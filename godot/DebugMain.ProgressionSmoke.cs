using System;
using System.Linq;
using Godot;
using MangakaSim;

namespace MangakaGame;

public partial class DebugMain
{
    private async void RunProgressionSmoke()
    {
        SetProcess(false);
        try
        {
            _careers=new CareerStore(ProjectSettings.GlobalizePath("res://../TestResults/progression-careers-"+Guid.NewGuid().ToString("N")));
            NewCareerMenu();await SettleUi();
            FindChildren("*","CheckBox",true,false).OfType<CheckBox>().First(c=>c.Text=="Instant manga production"&&!c.IsQueuedForDeletion()).ButtonPressed=true;
            await CaptureSmokeImage("progression-setup");Press("Begin career");await SettleUi();
            Check(_state.Assist(SandboxAssist.InstantProduction)&&!AchievementDelivery.Eligible(_state),"New-career Sandbox controls reach policy");
            _state.Apply(new DifficultyCommand(CareerDifficulty.Custom,SandboxAssist.InstantProduction,Pressure:0));_dirty=true;await SettleUi();
            Check(_recruitButton.Text.Contains("17,000")&&_digitalProposal.Text.Contains("25,500"),"Displayed quotes follow selected business difficulty");
            _presentation.Stories=false;_popupEvents.Clear();
            Navigate("Awards");await SettleUi();
            _sideContent.FindChildren("*","LineEdit",true,false).OfType<LineEdit>().First(e=>e.PlaceholderText=="One-shot title"&&e.IsVisibleInTree()).Text="The Paper Sky";
            Press("Start contest one-shot");await SettleUi();
            _state.Advance(1);_scanIndex=_state.Events.Count;Navigate("Awards");await SettleUi();
            Press("Submit eligible manuscript");await SettleUi();
            Check(_state.Progression.Awards.Count==1,"Contest submission through visible controls");
            var award=_state.Progression.Awards.Single();award.Quality=95;award.Originality=95;award.Jury=5;award.Competitors=[30,40];award.ResolvesAt=_state.Clock.Now.AddHours(1);
            _state.Advance(24);_scanIndex=_state.Events.Count;_popupEvents.Clear();Navigate("Awards");await CaptureSmokeImage("progression-awards");
            Check(award.Prize==300000,"Award result visible and paid");
            Press("Release for normal publishing");await SettleUi();
            var title=_state.Series.Single();title.Fanbase=20000;
            var project=new LicenseProject{Id=_state.AllocateId(),SeriesId=title.Id,BusinessId=title.BusinessId,CreatorId=title.RightsLeadPersonId,
                Kind=LicenseKind.Anime,Partner="Paper Lantern Animation",CreatedAt=_state.Clock.Now,DueAt=_state.Clock.Now.AddDays(30),Payment=400000,
                CreatorPercent=30,Control=2,Fit=85,Reliability=.9,Roll=.5,Weeks=26,SourceChapters=24};
            _state.Progression.Projects.Add(project);Navigate("Inbox");await SettleUi();Press("Mark displayed messages read");await SettleUi();
            Check(ButtonNamed("Review license decision") is not null,"Pending licenses remain actionable after reading notifications");
            Press("Review license decision");await CaptureSmokeImage("progression-offer");
            Press("Accept agreement");await SettleUi();Press("Consult occasionally");await SettleUi();
            Check(project.Phase==LicensePhase.PreProduction&&project.Involvement==1,"Deal and creator workload through controls");
            project.DueAt=_state.Clock.Now.AddHours(1);_state.Advance(24);_scanIndex=_state.Events.Count;_popupEvents.Clear();Navigate("Licenses");await SettleUi();
            Press("Approve original side stories · 4 more weeks");await SettleUi();
            Check(project.Original&&!project.OriginalEnding,"Catch-up choice recorded");await CaptureSmokeImage("progression-production");
            project.DueAt=_state.Clock.Now.AddHours(1);project.WarningChecked=true;_state.Advance(24);_scanIndex=_state.Events.Count;_popupEvents.Clear();Navigate("Licenses");await CaptureSmokeImage("progression-release");
            Check(project.Phase==LicensePhase.Released&&_state.Progression.Receipts.Count==2,"Anime release and separate receipts");
            Navigate("Finances");await CaptureSmokeImage("progression-finances");Navigate("Legacy");await CaptureSmokeImage("progression-journal");
            Check(_state.Progression.Milestones.Count>0&&_state.Progression.Achievements.Count==0,"Sandbox retains career honors without platform evidence");
            if(_state.Career.PendingScene=="beside")_state.Apply(new StoryCommand("beside",0));
            _state.Career.PendingScene="desk_moment";ShowStory("desk_moment");await CaptureSmokeImage("progression-illustration");
            Check(FindChildren("*","TextureRect",true,false).OfType<TextureRect>().Any(t=>t.IsVisibleInTree()&&t.Texture?.ResourcePath.EndsWith("desk-conversation.png")==true),"Original illustration loaded");
            GetWindow().Size=new(1280,720);await SettleUi();_helperPopup.Hide();ShowStory("desk_moment");await SettleUi();await CaptureSmokeImage("progression-illustration-720");
            var bounds=_helperPopup.GetGlobalRect();
            Check(bounds.Position.X>=0&&bounds.Position.Y>=0&&bounds.End.X<=GetViewportRect().Size.X+1&&bounds.End.Y<=GetViewportRect().Size.Y+1,"Entire illustrated dialogue fits 720p");
            Press("Read later");Check(_state.Career.DeferredUntil is not null,"Illustrated scene can be deferred");
            ShowStory("desk_moment");Press(HelperStories.Describe(_state,"desk_moment").Second);await SettleUi();
            Navigate("Help");await SettleUi();var before=_state.ToJson();Press("Revisit illustrated conversation");await SettleUi();
            Check(_state.ToJson()==before,"Illustrated journal replay is read-only");Press("Close");
            var save=SaveCareer("Progression smoke");LoadCareer(save);await SettleUi();
            Check(_state.ToJson()==before,"Progression state reloads through career menu");
            _state=GameState.FromJson(_state.ToJson());Check(_state.Progression.EverSandbox,"Achievement ineligibility survives save round trip");
            GD.Print($"PROGRESSION PASS: {_smokeChecks} checks");GetTree().Quit(0);
        }
        catch(Exception ex){GD.PrintErr("PROGRESSION FAIL: "+ex);GetTree().Quit(1);}
    }
}
