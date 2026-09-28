using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using MangakaSim;
using MangakaSim.Rules;

namespace MangakaGame;

public partial class DebugMain
{
    // T1.10 Part 4: every T1.1 step on Standard through the real screens, saving and reloading at each step, then the
    // setback through the practice career and the two oldest save shapes. Evidence for T1.6; not human play.
    // Steps whose form controls are not driven here apply the same command the screen's button sends, through TryApply.
    private async void RunJourneySmoke()
    {
        SetProcess(false);_homeOffice.SetProcess(false);_officeView.SetProcess(false);
        try
        {
            GetWindow().Size=new(1920,1080);Directory.CreateDirectory(SmokeOutput);
            _careers=new CareerStore(Path.Combine(SmokeOutput,"journey-"+Guid.NewGuid().ToString("N")));
            NewCareerMenu();Press("Begin career");await SettleUi();_helperPopup.Hide();
            Check(_state.Progression.Difficulty==CareerDifficulty.Standard&&!_state.Progression.EverSandbox,"Journey starts on Standard without Sandbox");
            await JourneyStep("new-career");

            // 1. First doujin.
            OpenWorkspace("Production");await SettleUi();
            TryApply(new CreateDoujinCommand("First pages","adventure"));
            var doujinId=_state.Series[^1].Id;
            for(var d=0;d<180&&Doujin().Volumes.Count==0;d++){_state.Advance(24);ScanEvents();}
            Check(Doujin().Volumes.Count>0,"First doujin is finished");
            await JourneyStep("first-doujin");

            // 2. First print run through the real printing screen, then the first sale.
            await Print(Doujin().Id,30);
            for(var d=0;d<60&&Doujin().Volumes[0].CopiesSold==0;d++){_state.Advance(24);ScanEvents();}
            Check(Doujin().Volumes[0].CopiesSold>0,"First doujin sells");
            await JourneyStep("first-sale");

            // 3. A later release that sells: a second doujin. A reprint of the first arrives after its 4 to 8 week shop window.
            OpenWorkspace("Production");await SettleUi();
            TryApply(new CreateDoujinCommand("Second pages","adventure"));
            var secondId=_state.Series[^1].Id;
            Series Second()=>_state.Series.First(s=>s.Id==secondId);
            for(var d=0;d<180&&Second().Volumes.Count==0;d++){_state.Advance(24);ScanEvents();}
            Check(secondId!=doujinId&&Second().Volumes.Count>0,"Second doujin is finished");
            await Print(secondId,30);
            for(var d=0;d<60&&Second().Volumes[0].CopiesSold==0;d++){_state.Advance(24);ScanEvents();}
            Check(Second().Volumes[0].CopiesSold>0,"Second doujin sells");
            await JourneyStep("later-sale");

            // 4. Pitch and serialization.
            Navigate("New series");await SettleUi();
            var genre=_state.PublisherCatalog.Magazines.SelectMany(m=>m.GenreAffinities.Keys.Select(g=>(m,g)))
                .OrderByDescending(p=>PitchRules.Chance(p.m.Tier,60,_state.EffectiveReputation,p.m.Affinity(p.g),_state.GenrePopularity(p.g)))
                .ThenBy(p=>p.m.Id).ThenBy(p=>p.g).First().g;
            SelectGenre(_sideContent.FindChildren("GenreChoice","OptionButton",true,false).OfType<OptionButton>().Single(),genre);
            ((LineEdit)_sideContent.FindChild("OngoingTitle",true,false)).Text="Ink and Thunder";
            Press("Create ongoing series");await SettleUi();
            Check(_state.Series[^1].Title=="Ink and Thunder"&&_page=="Series details","New series form creates the series and opens it");
            var serialId=_state.Series[^1].Id;
            var pitched=false;
            for(var d=0;d<720&&Serial().Contract is null;d++)
            {
                if(Serial().Publishing==PublishingStatus.Offered){OpenWorkspace("Publishing");await SettleUi();TryApply(new AcceptOfferCommand(serialId));continue;}
                if(Serial().Publishing==PublishingStatus.Unpublished&&CareerGuidance.PitchOutlooks(_state,Serial()).FirstOrDefault(o=>o.Open) is {} best)
                {
                    OpenWorkspace("Publishing");await SettleUi();
                    if(!pitched)Check(_publishingSeries.GetSelectedId()==serialId&&(int)_setPagesSpin.Value==Serial().PagesPerChapter,"Publishing and Production controls follow the new series");
                    TryApply(new PitchSeriesCommand(serialId,best.Magazine.Id));
                    if(!pitched){pitched=true;await JourneyStep("first-pitch");}
                }
                _state.Advance(24);ScanEvents();
            }
            Check(Serial().Contract is not null,"Series is serialized");
            await JourneyStep("serialized");
            for(var d=0;d<240&&Serial().ChaptersPublished==0;d++){_state.Advance(24);ScanEvents();}
            Check(Serial().ChaptersPublished>0,"First magazine chapter is published");
            await JourneyStep("first-chapter");

            // 5. First hire, furnishing the room first if there is no free desk.
            OpenWorkspace("Recruitment");await SettleUi();
            bool Open(Candidate c)=>!c.Recruited&&c.ExpiresAt>_state.Clock.Now&&(c.IntroductionBusinessId is null||c.IntroductionBusinessId==_state.ControlledBusinessId);
            for(var d=0;d<60&&!_state.Candidates.Any(Open);d++){if(_state.Recruitment is null)TryApply(new RecruitStaffCommand());_state.Advance(24);ScanEvents();}
            if(_state.Candidates.FirstOrDefault(Open) is {} candidate)
            {
                var location=_state.Protagonist.Employment!.LocationId;var salary=Math.Max(StudioRules.MinimumMonthlySalary,candidate.ExpectedSalary);
                var staff=_state.ControlledStaff.Count();
                TryApply(new HireStaffCommand(candidate.Id,location,salary));
                if(_state.ControlledStaff.Count()==staff)
                {
                    var arrangement=_state.ArrangeOffice(location,true,true);
                    TryApply(new ApplyOfficeLayoutCommand(location,_state.OfficeRevision,arrangement.Placements,arrangement.Purchases,[]));
                    TryApply(new HireStaffCommand(candidate.Id,location,salary));
                }
            }
            Check(_state.ControlledStaff.Any(p=>p.Id!=_state.ProtagonistPersonId),"First hire joined");
            await JourneyStep("first-hire");

            // 6. The setback, through the practice career.
            var practice=Path.Combine(ProjectSettings.GlobalizePath("res://"),"..","tests","MangakaSim.Tests","Fixtures","Practice - a struggling series.mangaka");
            var info=_careers.Import(File.ReadAllBytes(practice));LogTimeline($"import {SessionTimeline.ShortCareer(info.Career)}");
            Check(info.Career==CareerStore.PracticeCareer,"Practice career keeps its identifier");
            LoadCareer(info);await SettleUi();_helperPopup.Hide();
            bool Warned()=>_state.Events.Any(e=>e.Type==EventType.CancellationWarning&&_state.Series.Any(s=>s.Id==e.SeriesId&&s.BusinessId==_state.ControlledBusinessId));
            for(var d=0;d<45&&!Warned();d++){_state.Advance(24);ScanEvents();}
            Check(Warned(),"Practice career reaches the cancellation warning");
            await JourneyStep("setback");

            // 7. The oldest save fixture and an alpha.11 career (two in-game years, made by the alpha.11 simulation): each loads,
            // or is refused with a clear message.
            var v1=Path.Combine(ProjectSettings.GlobalizePath("res://"),"..","tests","MangakaSim.Tests","Fixtures","v1-minimal.json");
            Check(LoadsOrRefuses(()=>GameState.ImportSupported(File.ReadAllText(v1))),"Oldest save fixture loads or is refused clearly");
            var oldCareer=OS.GetCmdlineUserArgs().FirstOrDefault(a=>a.StartsWith("--old-career="))?["--old-career=".Length..]
                ??Path.Combine(ProjectSettings.GlobalizePath("res://"),"..","tests","MangakaSim.Tests","Fixtures","alpha11-year2.mangaka");
            if(File.Exists(oldCareer))
            {
                LoadCareer(_careers.Import(File.ReadAllBytes(oldCareer)));await SettleUi();_helperPopup.Hide();
                Check(_state.Series.Any(s=>s.BusinessId==_state.ControlledBusinessId&&s.Contract is not null),"alpha.11 career loads in the real screens");
                _state.Advance(48);ScanEvents();RefreshManagement();await SettleUi();
                Check(_presentation.Guidance.Route=="career","alpha.11 career plays on and its old guidance route moves to the career path");
            }
            else GD.Print("JOURNEY NOTE: no alpha.11 career at "+oldCareer+"; make one with the alpha.11 package and rerun.");

            // 8. An unexpected exception inside the game is noted in the timeline and told to the player once (T1.7).
            Callable.From(()=>throw new InvalidOperationException("Journey smoke deliberate exception")).CallDeferred();
            await SettleUi();await SettleUi();DrainUnexpectedErrors();
            var noted=string.Join("",_timeline!.Files().Select(f=>f.Text));
            Check(noted.Contains("error unexpected")&&noted.Contains("Journey smoke deliberate exception"),"An unexpected exception is written to the session timeline");
            Check(_workbenchNotice.Text.Contains("Something went wrong"),"The player is told once that something went wrong");

            // 9. The timeline recorded the journey.
            var log=string.Join("",_timeline!.Files().Select(f=>f.Text));
            foreach(var line in new[]{"session start","new-career","milestone first-doujin-completed","milestone first-sale","milestone later-sale",
                "milestone first-pitch","milestone serialization-accepted","milestone first-magazine-chapter","milestone first-hire",
                "milestone cancellation-warning","screen ","step ","save ","load ","import 70ac71"})
                Check(log.Contains(line),"Timeline records "+line.Trim());
            Check(!log.Contains("First pages")&&!log.Contains("Ink and Thunder")&&!log.Contains(SmokeOutput),"Timeline holds no titles or paths");
            File.WriteAllText(Path.Combine(SmokeOutput,"journey-timeline.log"),log);

            GD.Print($"JOURNEY SMOKE PASSED: {_smokeChecks} checks.");var tree=GetTree();tree.CreateTimer(.1).Timeout+=()=>tree.Quit();QueueFree();

            Series Doujin()=>_state.Series.First(s=>s.Id==doujinId);
            Series Serial()=>_state.Series.First(s=>s.Id==serialId);
        }
        catch(Exception ex)
        {
            GD.PushError($"JOURNEY SMOKE FAILED: {ex.Message}\n{ex.StackTrace}");GetTree().Quit(1);
        }
    }

    // Captures the step, then saves, reloads and checks the career continues identically for 48 hours.
    // LoadCareer replaces _state, so callers re-fetch series and people by id after every step.
    private async Task JourneyStep(string step)
    {
        // The live refresh is off during the smoke, so refresh the screen and restore the window size before capturing.
        // The day loops skip Continue, so a recap from an earlier day may still be open; a player would have dismissed it.
        _recapDialog.Hide();_pendingRecap=null;
        ScanEvents();GetWindow().Size=new(1920,1080);RefreshManagement();RefreshGuidance();await SettleUi();
        Check(!(_recapDialog.Visible&&_helperPopup.Visible),$"Journey {step}: the daily recap never covers a Helper-Chan notice");
        await CaptureSmokeImage("journey-"+step);
        var copy=GameState.FromJson(_state.ToJson());
        var info=SaveCareer("Journey "+step);LoadCareer(info);await SettleUi();_helperPopup.Hide();
        copy.Advance(48);_state.Advance(48);ScanEvents();
        Check(copy.ToJson()==_state.ToJson(),$"Journey {step}: saved and loaded career continues identically for 48 hours");
    }

    // Orders a print run through the real printing screen: copy shop, the given number of copies, then the order button.
    private async Task Print(int seriesId,int copies)
    {
        var runs=_state.PrintRuns.Count;
        OpenPrinting(seriesId);await SettleUi();
        _alphaPrinter.Select(0);_alphaCopies.Value=copies;await SettleUi();
        Press("Order this print run");await SettleUi();
        Check(_state.PrintRuns.Count==runs+1,$"Printing screen ordered {copies} copies");
        _state.Advance(24*8);ScanEvents();
    }

    private static bool LoadsOrRefuses(Action load)
    {
        try{load();return true;}
        catch(InvalidDataException ex){GD.Print("JOURNEY NOTE: refused with: "+ex.Message);return !string.IsNullOrWhiteSpace(ex.Message);}
    }
}
