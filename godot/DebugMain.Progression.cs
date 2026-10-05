using System;
using System.Linq;
using Godot;
using MangakaSim;

namespace MangakaGame;

public partial class DebugMain
{
    private readonly SteamAchievements _steam = new();
    private AchievementSession? _achievements;
    private AchievementSession AchievementSink => _achievements ??= new(_steam);
    private void ProgressionAction(ICommand command)
    {
        _state.Apply(command); _dirty = true; BuildManagementPage(); RefreshManagement();
    }
    private void BuildAwards()
    {
        Words(_sideContent,"Awards & contests",26);
        Words(_sideContent,$"Next newcomer deadline: {GameState.ContestDeadline(_state.Clock.Now):d MMM yyyy}\nFictional Story and Comedy awards · 32-page new entries · existing manuscripts 16–64 pages. Judged on craft, originality and fit.");
        var title = new LineEdit { PlaceholderText = "One-shot title", MaxLength = 120 }; _sideContent.AddChild(title);
        var category = new OptionButton(); category.AddItem("Story"); category.AddItem("Comedy"); _sideContent.AddChild(category);
        ActionButton(_sideContent,"Start contest one-shot",()=>ProgressionAction(new RecognitionCommand(RecognitionAction.CreateManuscript, Text:title.Text, Category:category.Selected==0?"story":"comedy")));
        foreach (var m in _state.Progression.Manuscripts.Where(m => ManagedSeries.Any(s => s.Id == m.SeriesId)))
        {
            var s = _state.FindSeries(m.SeriesId)!; var c = _state.FindChapter(m.ChapterId)!;
            var box = Card(s.Title,$"Revision {m.Revision}"+(c is null?"":$" · {c.Status} · {c.Stages.Count(w=>w.IsDone)}/5 stages"));
            if (m.Released) { Words(box,"Released from contest use. Normal publishing controls apply."); continue; }
            var active = _state.Progression.Awards.FirstOrDefault(a => a.ManuscriptId == m.Id && a.ResolvedAt is null);
            if(active is not null){Words(box,$"Submitted · results {active.ResolvesAt:d MMM yyyy}");continue;}
            if(c?.Status == ChapterStatus.Complete)
            {
                // Offer only what the contest rules accept (fresh-player finding A5).
                var won=_state.Progression.Awards.Any(a=>a.ManuscriptId==m.Id&&a.Prize>0);
                var entered=_state.Progression.Awards.Any(a=>a.ManuscriptId==m.Id&&a.Revision==m.Revision);
                if(won)Words(box,"This manuscript already won a prize, so it cannot enter newcomer contests again. Release it for normal publishing.",14);
                else if(entered)Words(box,"This version has already been entered. Produce a revised version to enter again.",14);
                else ActionButton(box,"Submit eligible manuscript",()=>ProgressionAction(new RecognitionCommand(RecognitionAction.Submit,m.Id)));
                if(!won)ActionButton(box,"Produce a revised version",()=>ProgressionAction(new RecognitionCommand(RecognitionAction.Revise,m.Id)));
            }
            else Words(box,"Finish the manuscript's pages in Production first. They progress by themselves while time runs.",14);
            ActionButton(box,"Release for normal publishing",()=>ProgressionAction(new RecognitionCommand(RecognitionAction.ReleaseManuscript,m.Id)));
        }
        var adoptable=ManagedSeries.SelectMany(s=>s.Chapters.Where(_state.CanAdoptManuscript).TakeLast(1).Select(c=>(s,c))).ToArray();
        foreach(var (s,c) in adoptable)
            ActionButton(_sideContent,$"Use existing manuscript: {s.Title}",()=>ProgressionAction(new AdoptManuscriptCommand(c.Id)));
        if(adoptable.Length==0)QuietWords(_sideContent,"An existing title can enter when it is unpublished, has no unfinished chapters and has a finished 16 to 64 page chapter that is not in a book yet.");
        Words(_sideContent,"Honors & results",23);
        Words(_sideContent,"Published manga are considered automatically each January. Shortlisted work receives a smaller temporary discovery boost. Wins never guarantee serialization or an anime.",14);
        foreach(var a in _state.Progression.Awards.Where(a=>ManagedSeries.Any(s=>s.Id==a.SeriesId)||a.CreatorId==_state.ProtagonistPersonId).TakeLast(30).Reverse())
        {
            var box=Card(_state.FindSeries(a.SeriesId)!.Title,a.Award+" · "+a.Result);
            Words(box,a.ResolvedAt is null?$"Results: {a.ResolvesAt:d MMM yyyy}":$"{a.Feedback}\nPersonal prize: ¥{a.Prize:N0}");
            if(a.Prize>0)ActionButton(box,"Explore publishing interest",()=>OpenWorkspace("Publishing"));
        }
    }
    private void BuildLicenses()
    {
        Words(_sideContent,"Adaptations & merchandise",26);
        Words(_sideContent,"Partners make the anime and products. Compare guaranteed payment, creator share, fit, reliability and approval rights before signing.",14);
        foreach(var s in ManagedSeries)
        {
            var box=Card(s.Title,$"{s.Fanbase:N0} readers");
            var kinds=new OptionButton();foreach(var kind in Enum.GetValues<LicenseKind>())kinds.AddItem(kind.ToString());box.AddChild(kinds);
            ActionButton(box,"Pitch a licensing partner · 2 hours",()=>ProgressionAction(new LicenseCommand(LicenseAction.Pitch,s.Id,(LicenseKind)kinds.Selected)));
        }
        foreach(var p in _state.Progression.Projects.Where(p=>p.BusinessId==_state.ControlledBusinessId||p.CreatorId==_state.ProtagonistPersonId).TakeLast(35).Reverse())
        {
            var box=Card($"{_state.FindSeries(p.SeriesId)!.Title} · {p.Kind}"+(p.Kind==LicenseKind.Anime?$" · Season {p.Season}":""),$"{p.Partner} · {p.Phase}");
            var creator=_state.FindPerson(p.CreatorId)!.Name;
            var signing=p.Payment/5;
            bool anime=p.Kind==LicenseKind.Anime;
            Words(box,$"{p.Outcome}\nGuaranteed payment ¥{p.Payment:N0}: ¥{signing:N0} on signing, ¥{p.Payment-signing:N0} on release\n{creator} receives {p.CreatorPercent}% of every payment\nFit {p.Fit:0}/100 · reliability {p.Reliability:P0} · approval: {new[]{"delegated","consultation","close supervision"}[p.Control]}\nProduction {p.Weeks} weeks · next date {p.DueAt:d MMM yyyy}");
            Words(box,anime?"No royalties: an anime pays the guaranteed payment only. Its reward is new readers and a six-month lift for the manga."
                :$"Then royalties every month for a year, up to ¥{p.Payment*8/100:N0} a month, more when the products are well received.",13);
            if(p.Phase==LicensePhase.Offer)
                Words(box,$"Fit is how well {p.Partner} suits this series. Reliability is how likely they stay on schedule without disputes. Both raise the reception, and a well-received release brings more new readers"+
                    (anime?". Approval is your say over the story: with more of it you can approve original material if the anime catches up with the manga.":" and higher royalties."),13);
            Words(box,"Exclusive in its category during the term. If production is cancelled, the signing payment is kept.",13);
            Words(box,$"Paid to {_state.Businesses.Single(b=>b.Id==p.BusinessId).Name}; {creator}'s share follows on the next monthly settlement. These recipients stay with the signed deal.",13);
            var s=_state.FindSeries(p.SeriesId)!;
            bool permitted=p.BusinessId==_state.ControlledBusinessId&&s.BusinessId==_state.ControlledBusinessId&&
                ( _state.Control==ControlMode.OwnerDirector||s.LeadPersonId==_state.ProtagonistPersonId)&&p.CreatorId==s.RightsLeadPersonId;
            if(!permitted){Words(box,"Historical agreement: original parties and payment rights retained.");continue;}
            if(p.Phase==LicensePhase.Offer)
            {
                ActionButton(box,_state.Control==ControlMode.EmployedLead&&!p.EmployerApproved?"Request employer approval":"Accept agreement",()=>ProgressionAction(new LicenseCommand(LicenseAction.Accept,p.Id)));
                ActionButton(box,"Decline",()=>ProgressionAction(new LicenseCommand(LicenseAction.Decline,p.Id)));
                if(p.Rounds<2&&p.EmployerRequestedAt is null)
                {
                    Words(box,$"Counteroffers remaining: {2-p.Rounds}. Partners can refuse or withdraw.",13);
                    ActionButton(box,"Ask +10% payment · allow 4 more weeks",()=>ProgressionAction(new LicenseCommand(LicenseAction.Payment,p.Id)));
                    if(p.Kind==LicenseKind.Anime&&p.Control<2)ActionButton(box,"Ask more creative rights · concede 10% payment",()=>ProgressionAction(new LicenseCommand(LicenseAction.Control,p.Id)));
                    ActionButton(box,"Allow 8 more weeks · improve reliability",()=>ProgressionAction(new LicenseCommand(LicenseAction.Schedule,p.Id)));
                }
            }
            if(p.Kind==LicenseKind.Anime&&p.Phase is LicensePhase.PreProduction or LicensePhase.Production)
            {
                Words(box,$"Creator involvement: {new[]{"Hands-off","Consultation (2h/week)","Close supervision (up to 6h/week)"}[p.Involvement]}\nActual consultation so far: {p.ConsultationHours} hours",14);
                for(int i=0;i<=p.Control;i++){var level=i;ActionButton(box,new[]{"Hands-off","Consult occasionally","Supervise closely"}[i],()=>ProgressionAction(new LicenseCommand(LicenseAction.Involvement,p.Id,Value:level)));}
            }
            if(p.Decision=="Source material")
            {
                Words(box,"The anime is catching up. Choose within fourteen days; the default is a shorter season.");
                Words(box,"A shorter season reaches fewer viewers but stays faithful. Original stories depend more on partner fit; an original ending has the wider range of possible reactions.",13);
                ActionButton(box,"Finish a shorter season; wait for more manga",()=>ProgressionAction(new LicenseCommand(LicenseAction.Wait,p.Id)));
                if(p.Control>0)
                {
                    ActionButton(box,"Approve original side stories · 4 more weeks",()=>ProgressionAction(new LicenseCommand(LicenseAction.SideStories,p.Id)));
                    ActionButton(box,"Approve an original ending · 4 more weeks",()=>ProgressionAction(new LicenseCommand(LicenseAction.OriginalEnding,p.Id)));
                }
            }
            else if(p.Decision.Length>0)ActionButton(box,"Agree to an eight-week production delay",()=>ProgressionAction(new LicenseCommand(LicenseAction.RespondDelay,p.Id)));
            foreach(var n in p.Negotiations)Words(box,n,13);
            if(p.ReleasedAt is not null)Words(box,$"Reception {p.Reception:0}/100 · receipts ¥{_state.Progression.Receipts.Where(r=>r.ProjectId==p.Id).Sum(r=>r.Gross):N0}");
        }
    }
    private void BuildLegacy()
    {
        Words(_sideContent,"Career journal",26);
        Words(_sideContent,!AchievementDelivery.Eligible(_state)?"Steam achievements disabled for this save. Career milestones and stories remain available.":_steam.Connected?"Steam achievements are on for this save.":"This save can earn Steam achievements. They reach Steam the next time you play with Steam running.");
        foreach(var m in _state.Progression.Milestones.AsEnumerable().Reverse().Take(60))Words(Card(ProgressionCatalog.Achievements.FirstOrDefault(a=>a.Key==m.Key)?.Name??m.Key.Replace('_',' '),$"{m.At:d MMM yyyy}"),m.Text);
    }
    private Func<DifficultyCommand> DifficultyControls(Control parent, bool newGame)
    {
        var p=_state.Progression;
        var preset=StudioCard(parent,"DIFFICULTY");
        var mode=new OptionButton();foreach(var value in Enum.GetValues<CareerDifficulty>())mode.AddItem(value.ToString());
        mode.Select(newGame?(int)CareerDifficulty.Standard:(int)p.Difficulty);preset.AddChild(mode);
        var summary=Words(preset,"",14);
        var custom=StudioCard(parent,"CUSTOM RULES");
        var cushion=new OptionButton{Visible=newGame};foreach(var name in new[]{"Custom opening funds: lean (half)","Custom opening funds: standard","Custom opening funds: comfortable (double)"})cushion.AddItem(name);cushion.Select(1);custom.AddChild(cushion);
        var pressure=new OptionButton();foreach(var name in new[]{"Low business pressure","Standard business pressure","High business pressure"})pressure.AddItem(name);pressure.Select(newGame?1:p.Pressure);custom.AddChild(pressure);
        var recovery=new OptionButton();foreach(var name in new[]{"Short recovery grace","Standard recovery grace","Long recovery grace"})recovery.AddItem(name);recovery.Select(newGame?1:p.Recovery);custom.AddChild(recovery);
        Words(custom,"Business pressure sets how strong rival series are in reader surveys, how often editors accept pitches, and optional costs such as recruitment and advertising. Recovery grace sets how long editors wait before warning and cancelling, how many chapters protect a new series, and the grace before unpaid rent closes a workplace. Signed obligations stay intact.",13);
        var assists=StudioCard(parent,"SANDBOX ASSISTS");
        Words(assists,"Sandbox or any assist permanently disables Steam achievements for this save.",14);
        var assistList=new VBoxContainer{Visible=!newGame&&p.Assists!=SandboxAssist.None};assists.AddChild(assistList);
        var toggle=ActionButton(assists,assistList.Visible?"Hide assist options":"Show assist options",()=>{});
        assists.MoveChild(toggle,2);
        toggle.Pressed+=()=>{assistList.Visible=!assistList.Visible;toggle.Text=assistList.Visible?"Hide assist options":"Show assist options";};
        var options=Enum.GetValues<SandboxAssist>().Where(a=>a!=SandboxAssist.None).Select(a=>
        {
            var label=a switch {SandboxAssist.PersonalFunds=>"Unlimited personal funds",SandboxAssist.BusinessFunds=>"Unlimited authorized business funds",SandboxAssist.InstantProduction=>"Instant manga production",SandboxAssist.InstantDelivery=>"Instant print deliveries",SandboxAssist.UnlockLocations=>"Unlock additional studio locations",SandboxAssist.UnlockEquipment=>"Unlock equipment progression",SandboxAssist.NoStress=>"Disable stress effects",SandboxAssist.NoDeadlinePenalties=>"Disable deadline penalties",_=>"Unlock future equipment and publishing channels"};
            var check=new CheckBox{Text=label,ButtonPressed=!newGame&&(p.Assists&a)!=0};assistList.AddChild(check);return(Assist:a,Check:check);
        }).ToArray();
        void PresetChanged()
        {
            var selected=(CareerDifficulty)mode.Selected;
            custom.GetParent<Control>().Visible=selected==CareerDifficulty.Custom;
            summary.Text=newGame?selected switch
            {
                CareerDifficulty.Relaxed=>"A gentler career · ¥400,000 personal / ¥600,000 business · weaker rivals, better pitch odds and more patient editors",
                CareerDifficulty.Challenging=>"A tougher career · ¥100,000 personal / ¥150,000 business · stronger rivals, harder pitches and less patient editors",
                CareerDifficulty.Custom=>"Choose your opening funds, business pressure and recovery grace below.",
                CareerDifficulty.Sandbox=>"Play freely · achievements disabled · choose optional assists below.",
                _=>"The standard career · ¥200,000 personal / ¥300,000 business"
            }:"Changes apply going forward. Opening funds and title ownership stay fixed.";
            if(selected==CareerDifficulty.Sandbox){assistList.Show();toggle.Text="Hide assist options";}
        }
        mode.ItemSelected+=_=>PresetChanged();PresetChanged();
        return()=>new((CareerDifficulty)mode.Selected,options.Where(o=>o.Check.ButtonPressed).Aggregate(SandboxAssist.None,(v,o)=>v|o.Assist),pressure.Selected,recovery.Selected,cushion.Selected);
    }
}
