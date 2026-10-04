using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using MangakaSim;
using MangakaSim.Rules;

namespace MangakaGame;

public partial class DebugMain
{
    // T1.9 display sweep (Q29, Q30): every core screen at 5 sizes, 100% and 150% interface size (plus 200%) and 2 themes, checked for
    // controls off the window, overlapping siblings and cut-off button text, with a review set of captures.
    private async void RunDisplaySweepSmoke()
    {
        SetProcess(false);_homeOffice.SetProcess(false);_officeView.SetProcess(false);
        try
        {
            GetWindow().Size=new(1920,1080);
            Directory.CreateDirectory(SmokeOutput);
            _careers=new CareerStore(Path.Combine(SmokeOutput,"display-sweep-"+Guid.NewGuid().ToString("N")));
            NewCareerMenu();Press("Begin career");await SettleUi();_helperPopup.Hide();

            _presentation=new(){Page="Office"};
            _state=SweepCareer(out var serial,out var doujin,out var fixture);
            var sweepState=_state;
            GD.Print("DISPLAY SWEEP fixture: "+fixture);
            ResetManagementSession();ShowOffice();_helperPopup.Hide();_popupEvents.Clear();
            var prefs=_presentation.Guidance;
            CareerGuidance.Observe(_state,prefs);CareerGuidance.MarkRead(prefs);RefreshGuidance();await SettleUi();
            Check(serial.Contract is not null,"Fixture has a serialized series");
            Check(_state.Series.Any(s=>s.Volumes.Count>0),"Fixture has a finished book");
            var recap=_state.Events.LastOrDefault(e=>e.Type==EventType.DailyRecap);
            Check(recap is not null,"Fixture has a day recap");

            void Reset()
            {
                if(_state!=sweepState){_state=sweepState;ResetManagementSession();}
                CloseTitle();_menu.Hide();_inMenu=false;ClosePhone();_recapDialog.Hide();_helperPopup.Hide();_speedFlash?.Hide();ShowOffice();
            }
            var screens=new List<(string Name,Action Open)>
            {
                ("title",OpenTitle),
                ("title-settings",()=>{OpenTitle();SettingsMenu();}),
                ("new-career",()=>{OpenTitle();NewCareerMenu();}),
                ("new-career-rules",()=>{OpenTitle();NewCareerMenu();Press("Customise");Press("Career rules");}),
                ("office",()=>{}),
                ("goals",()=>Navigate("Goals")),
                ("day-one",()=>{_state=GameState.NewGame(0);ResetManagementSession();ShowOffice();ClosePhone();}), // the phone has its own screen; here it would be caught mid-slide
                ("phone",()=>{OpenPhone(false);_phoneTween?.Kill();_phoneSlide=0;ResizeFloatingOffice();}),
                ("pause-menu",ShowMenu),
                ("production",()=>OpenWorkspace("Production")),
                ("recap",()=>ShowRecap(recap!)),
                ("publishing",()=>OpenWorkspace("Publishing")),
                ("series",()=>Navigate("Series")),
                ("series-details",()=>Navigate("Series details",serial.Id)),
                ("new-series",()=>Navigate("New series")),
                ("books",()=>Navigate("Books")),
                ("printing",()=>OpenPrinting(doujin.Id)),
                ("conventions",()=>Navigate("Conventions")),
                ("sell-online",()=>OpenOnline(doujin.Id,doujin.Volumes[0].Id)),
                ("finances",()=>Navigate("Finances")),
                ("staff",()=>Navigate("Staff")),
                ("recruitment",()=>OpenWorkspace("Recruitment")),
                ("inbox",()=>Navigate("Inbox")),
                ("settings-display",()=>{ShowMenu();SettingsMenu();}),
                ("settings-helper",()=>{ShowMenu();SettingsMenu();Press("Helper & pauses");}),
                ("settings-difficulty",()=>{ShowMenu();SettingsMenu();Press("Difficulty & Sandbox");}),
                ("save",OpenSaveMenu),
                ("load",()=>{ShowMenu();LoadCareerMenu();}),
            };
            var sizes=new(Vector2I Size,string Name)[]{(new(1280,720),"1280x720"),(new(1280,800),"1280x800"),(new(1920,1080),"1920x1080"),(new(2560,1080),"2560x1080"),(new(3440,1440),"3440x1440")};
            var flagged=new List<string>();var combos=0;
            // Interface size (display settings, spec 2026-10-04): each layout size at 100% and at 150% (a window 1.5 times
            // larger with the same layout room), plus 200% on a 2560 x 1440 window (a 1280 x 720 layout).
            var passes=sizes.SelectMany(s=>new[]{(s.Size,s.Name,1d),(s.Size,s.Name,1.5)}).Append((new Vector2I(1280,720),"1280x720",2d)).ToArray();
            foreach(var dark in new[]{true,false})
            foreach(var (size,sizeName,scale) in passes)
            {
                SetDarkMode(dark);await Resize(size,scale);
                var combo=$"{sizeName}-{scale*100:0}-{(dark?"dark":"light")}";
                foreach(var (name,open) in screens)
                {
                    Reset();await SettleUi();open();await SettleUi();await SettleUi();
                    var problems=Inspect();combos++;
                    foreach(var p in problems){var line=$"{combo} {name}: {p}";flagged.Add(line);GD.Print("DISPLAY SWEEP FLAG "+line);}
                    // Review set: tightest sizes with large text, the widest screen, light theme at 1920x1080, and anything flagged.
                    var review=scale>1&&size.X==1280&&dark||size.X==3440&&scale==1&&dark||!dark&&scale==1&&size.X==1920||problems.Count>0;
                    if(review)await CaptureSmokeImage($"display-sweep-{combo}-{name}");
                }
                Reset();
            }
            GD.Print($"DISPLAY SWEEP: {combos} screen checks, {flagged.Count} flagged.");
            Check(flagged.Count==0,$"No core screen is off the window, overlapping or cut off ({flagged.Count} flagged)");

            // Non-core screens, informational only (Tier 1 scope option 1).
            SetDarkMode(true);await Resize(new(1280,720),1.5);
            foreach(var page in new[]{"Awards","Licenses","Studios","Industry","Legacy"})
            {
                Reset();await SettleUi();Navigate(page);await SettleUi();await SettleUi();
                foreach(var p in Inspect())GD.Print($"DISPLAY SWEEP NOTE 1280x720-150-dark {page}: {p}");
                await CaptureSmokeImage($"display-sweep-noncore-{page.ToLowerInvariant()}");
            }
            Reset();await Resize(new(1920,1080),1);

            GD.Print($"DISPLAY SWEEP SMOKE PASSED: {_smokeChecks} checks.");var tree=GetTree();tree.CreateTimer(.1).Timeout+=()=>QuitTree(tree);QueueFree();
        }
        catch(Exception ex)
        {
            GD.PushError($"DISPLAY SWEEP SMOKE FAILED: {ex.Message}\n{ex.StackTrace}");GetTree().Quit(1);
        }
    }

    // The window is the layout size times the interface size, so the interface keeps exactly that much room.
    private async Task Resize(Vector2I size,double scale)
    {
        _display.InterfaceSize=scale;GetWindow().Size=new((int)Math.Round(size.X*scale),(int)Math.Round(size.Y*scale));ApplyInterfaceSize();await SettleUi();
        for(var f=0;f<60&&(GetViewport().GetVisibleRect().Size-(Vector2)size).Length()>1;f++)await SettleUi();
        Check((GetViewport().GetVisibleRect().Size-(Vector2)size).Length()<=1,$"Window reached {size} (got {GetViewport().GetVisibleRect().Size}, scale {scale})");
    }

    private List<string> Inspect()
    {
        var excluded=new HashSet<Node>{_helperPopup,_homeOffice,_officeView,_backdrop};
        if(_speedFlash is not null)excluded.Add(_speedFlash);
        if(_titleArt is not null)excluded.Add(_titleArt); // scaled a little past the window by the slow push-in, by design
        var panels=new List<Control>{_side,_report,_officeDashboard};
        foreach(var panel in new Control?[]{_floatingRail,_floatingTop,_floatingBottom})if(panel is not null)panels.Add(panel);
        return DisplaySweep.Inspect(this,GetViewport().GetVisibleRect(),excluded,panels);
    }

    // A career with something on every core screen: a sold doujin with stock, a serialized series waiting for
    // its debut, a convention booking, one hire and a few inbox items. Only real commands, as a player would.
    private static GameState SweepCareer(out Series serial,out Series doujin,out string summary)
    {
        var state=GameState.NewGame(0);var notes=new List<string>();
        bool Try(ICommand command){try{state.Apply(command);return true;}catch(InvalidCommandException ex){notes.Add(ex.Message);return false;}}
        Try(new CreateDoujinCommand("First pages","adventure"));
        Try(new ShowEveryScreenCommand(true)); // every screen is checked; the day-one view is its own screen
        doujin=state.Series[0];
        for(var d=0;d<180&&doujin.Volumes.Count==0;d++)state.Advance(24);
        Try(new StudioActionCommand(StudioAction.Print,doujin.Volumes.Single().Id,Amount:30,Value:(int)PrintTier.CopyShop));
        state.Advance(24*8);

        var genre=state.PublisherCatalog.Magazines.SelectMany(m=>m.GenreAffinities.Keys.Select(g=>(m,g)))
            .OrderByDescending(p=>PitchRules.Chance(p.m.Tier,60,state.EffectiveReputation,p.m.Affinity(p.g),state.GenrePopularity(p.g)))
            .ThenBy(p=>p.m.Id).ThenBy(p=>p.g).First().g;
        Try(new CreateSeriesCommand("Ink and Thunder",genre,Cadence.Monthly,16));
        serial=state.Series[^1];
        for(var d=0;d<720&&serial.Contract is null;d++)
        {
            if(serial.Publishing==PublishingStatus.Offered){Try(new AcceptOfferCommand(serial.Id));continue;}
            if(serial.Publishing==PublishingStatus.Unpublished&&CareerGuidance.PitchOutlooks(state,serial).FirstOrDefault(o=>o.Open) is {} best)
                Try(new PitchSeriesCommand(serial.Id,best.Magazine.Id));
            state.Advance(24);
        }
        // A convention to show in Books and Conventions.
        for(var d=0;d<60&&state.NextConvention(1)>state.Clock.Now.Date.AddDays(28);d++)state.Advance(24);
        Try(new StudioActionCommand(StudioAction.BookConvention,state.ProtagonistPersonId,Amount:doujin.Id,Value:1));
        // One hire, furnishing the room first if there is no free desk.
        for(var d=0;d<60&&!state.Candidates.Any(Open);d++){if(state.Recruitment is null)Try(new RecruitStaffCommand());state.Advance(24);}
        if(state.Candidates.FirstOrDefault(Open) is {} candidate)
        {
            var location=state.Protagonist.Employment!.LocationId;var salary=Math.Max(StudioRules.MinimumMonthlySalary,candidate.ExpectedSalary);
            if(!Try(new HireStaffCommand(candidate.Id,location,salary)))
            {
                var arrangement=state.ArrangeOffice(location,true,true);
                if(Try(new ApplyOfficeLayoutCommand(location,state.OfficeRevision,arrangement.Placements,arrangement.Purchases,[])))Try(new HireStaffCommand(candidate.Id,location,salary));
            }
        }
        // End mid-morning of a working day so the office is busy and the last recap is recent.
        state.Advance(24);
        summary=$"{state.Clock.Now:d MMM yyyy}, series {serial.Publishing}, staff {state.ControlledStaff.Count()}, rejected commands: {string.Join(" | ",notes.Distinct())}";
        return state;
        bool Open(Candidate c)=>!c.Recruited&&c.ExpiresAt>state.Clock.Now&&(c.IntroductionBusinessId is null||c.IntroductionBusinessId==state.ControlledBusinessId);
    }
}
