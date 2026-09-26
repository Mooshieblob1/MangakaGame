using System;
using System.IO;
using System.Linq;
using Godot;
using MangakaSim;

namespace MangakaGame;

public partial class DebugMain
{
    private async void RunSeriesStatusSmoke()
    {
        SetProcess(false);
        try
        {
            Directory.CreateDirectory(SmokeOutput);
            _careers=new CareerStore(Path.Combine(SmokeOutput,"status-careers-"+Guid.NewGuid().ToString("N")));
            NewCareerMenu();Press("Begin career");await SettleUi();_helperPopup.Hide();
            _state=GameState.NewGame(2);ResetManagementSession();await SettleUi();
            Navigate("New series");await SettleUi();
            var title=_sideContent.FindChildren("OngoingTitle","LineEdit",true,false).OfType<LineEdit>().Single();
            title.Text="Paper Garden";title.GrabFocus();
            Input.ParseInputEvent(new InputEventKey{Keycode=Key.Escape,Pressed=true});await SettleUi();
            Check(!title.HasFocus()&&title.Text=="Paper Garden"&&_side.Visible&&_page=="New series","Escape releases title focus without closing its form or changing text");
            Check(GameKeysAvailable(),"Gameplay shortcuts resume after leaving text entry");
            var genres=_sideContent.FindChildren("GenreChoice","OptionButton",true,false).OfType<OptionButton>().Single();
            Check(Enumerable.Range(0,genres.ItemCount).Select(genres.GetItemText).SequenceEqual(_state.TrendCatalog.Genres),"Dropdown contains exactly the supported genres");
            SelectGenre(genres,"drama");Press("Create ongoing series");await SettleUi();var series=_state.Series.Single();
            Check(series.Genre=="drama","Ongoing creation submits selected genre");
            Navigate("Series");Check(PublishingStatusText(series).Contains("Self-published"),"Sidebar distinguishes unpitched work");
            _state.Advance(1);_state.Apply(new PitchSeriesCommand(series.Id,"hoshigaku-flowers"));Navigate("Series");await SettleUi();
            Check(_sideContent.FindChildren("*","Label",true,false).OfType<Label>().Any(l=>l.Text.Contains("Being pitched")),"Series list shows pitch state");
            Check(SidebarChapter(series)!.IsOneShot,"Sidebar progress follows pitch sample rather than held doujin draft");
            await CaptureSmokeImage("series-status-pitch");
            for(var hour=0;hour<24*240&&series.Publishing==PublishingStatus.Pitching;hour++)_state.Advance(1);
            Check(series.Publishing==PublishingStatus.Offered,"Normal sample production receives a serialization offer");
            _scanIndex=_state.Events.Count;_lastAutosave=_state.Clock.Now.Date;_popupEvents.Clear();_dirty=true;Navigate("Series");await SettleUi();
            var snapshot=_state.ToJson();
            Check(PublishingStatusText(series).Contains("your decision needed")&&PublishingStatusText(series).Contains("Accept or decline by"),"Accepted pitch clearly requires the player's contract decision");
            Check(_state.ToJson()==snapshot,"Status queries do not alter simulation state");
            await CaptureSmokeImage("series-status-offer");
            Press("Review publishing offer");await SettleUi();Check(_report.Visible&&_mainTabs.CurrentTab==1&&_publishingSeries.GetSelectedId()==series.Id,"Sidebar action selects the correct offer");
            _state.Apply(new AcceptOfferCommand(series.Id));Navigate("Series details",series.Id);await SettleUi();
            Check(PublishingStatusText(series).Contains("Serialization accepted")&&!PublishingStatusText(series).Contains("your decision needed"),"Signed contract replaces pending decision text");
            await CaptureSmokeImage("series-status-accepted");
            var accepted=_state.ToJson();
            Check(PublishingStatusText(series).Contains($"Chapters ready ahead: 0 of {_state.ChaptersReadyAhead(series).Target}"),"Signed series shows chapters ready ahead");
            // This fixture skips the doujin sale, so record it as a career past its first sale would have.
            _presentation.Guidance.Completed.Add("first-sale");RefreshGuidance();await SettleUi();
            Check(CareerGuidance.Evaluate(_state,_presentation.Guidance).Id=="first-deadline"&&PhoneSays("chapters ready")&&PhoneSays("Page fees only arrive"),$"Helper-Chan explains the debut wait and counts ready chapters ({CareerGuidance.Evaluate(_state,_presentation.Guidance).Id}: {string.Join(" | ",_phone.FindChildren("*","Label",true,false).OfType<Label>().Select(l=>l.Text).TakeLast(4))})");
            for(var day=0;day<120&&_state.ChaptersReadyAhead(series) is var a&&a.Ready<a.Target;day++){_state.Advance(24);CareerGuidance.Observe(_state,_presentation.Guidance);}
            _scanIndex=_state.Events.Count;_dirty=true;RefreshGuidance();Navigate("Series details",series.Id);await SettleUi();
            var wait=CareerGuidance.Evaluate(_state,_presentation.Guidance);
            Check(series.ChaptersPublished==0&&wait.Id.StartsWith("debut-wait-")&&PhoneSays("Other ideas")&&PhoneSays("buffer to 4"),"Stock ready: one suggestion, the others listed and the buffer tip");
            Check(_presentation.Guidance.Thread.SelectMany(m=>m.Texts).All(t=>t.Length<=CareerGuidance.TextLimit),"Debut wait texts fit the 140 character limit");
            Check(PublishingStatusText(series).Contains($"Chapters ready ahead: {_state.ChaptersReadyAhead(series).Ready} of"),"Series status counts the finished stock");
            var windowSize=GetWindow().Size;
            foreach(var (size,name,scale) in new(Vector2I,string,double)[]{(new(1920,1080),"1080",1),(new(1280,720),"720-150",1.5)})
            {
                _presentation.UiScale=scale;ApplyTextScale();GetWindow().Size=size;await SettleUi();
                if(!_phoneOpen)OpenPhone(false);_phoneTween?.Kill();_phoneSlide=0;await SettleUi();await SettleUi();
                await CaptureSmokeImage($"series-status-debut-wait-{name}");
            }
            _presentation.UiScale=1;ApplyTextScale();GetWindow().Size=windowSize;await SettleUi();
            _state=GameState.FromJson(accepted);series=_state.Series.Single();
            var signed=_state.ToJson();_state=GameState.FromJson(snapshot);series=_state.Series.Single();
            _state.Apply(new DeclineOfferCommand(series.Id));Navigate("Series");Check(PublishingStatusText(series).Contains("Offer declined"),"Declined offer retains a clear outcome");
            _state=GameState.FromJson(snapshot);series=_state.Series.Single();_state.Advance(_state.Clock.HoursUntil(series.PendingOffer!.ExpiresAt));
            Check(PublishingStatusText(series).Contains("Offer expired"),"Expired offer no longer appears accepted or pending");
            _state=GameState.FromJson(signed);series=_state.Series.Single();_scanIndex=_state.Events.Count;_lastAutosave=_state.Clock.Now.Date;
            Navigate("New doujin");await SettleUi();var oneShot=GetNode<LineEdit>("%DoujinTitle");oneShot.Text="Small romance";
            genres=_sideContent.FindChildren("GenreChoice","OptionButton",true,false).OfType<OptionButton>().Single();SelectGenre(genres,"romance");Press("Create one-shot doujin");
            Check(_state.Series.Last().Genre=="romance","One-shot creation submits dropdown genre");
            Navigate("New series");SelectGenre(_sideContent.FindChildren("GenreChoice","OptionButton",true,false).OfType<OptionButton>().Single(),"mystery");((LineEdit)_sideContent.FindChild("OngoingTitle",true,false)).Text="Mystery club";Press("Create ongoing series");Check(_state.Series.Last().Genre=="mystery","Ongoing creation submits dropdown genre");
            ShowMenu();ReportProblem();await SettleUi();var note=_menuContent.GetChildren().OfType<TextEdit>().Single();note.Text="Keep this note";note.GrabFocus();
            Input.ParseInputEvent(new InputEventKey{Keycode=Key.Escape,Pressed=true});await SettleUi();
            Check(!note.HasFocus()&&note.Text=="Keep this note"&&_menu.Visible,"Escape also releases multiline entry without closing menu");
            Input.ParseInputEvent(new InputEventKey{Keycode=Key.Escape,Pressed=true});await SettleUi();Check(!_menu.Visible,"Second Escape retains normal menu closing");
            GD.Print($"SERIES STATUS SMOKE PASSED: {_smokeChecks} checks.");var tree=GetTree();tree.CreateTimer(.1).Timeout+=()=>tree.Quit();QueueFree();
        }
        catch(Exception ex){GD.PrintErr("SERIES STATUS SMOKE FAILED: "+ex);GetTree().Quit(1);}
    }
}
