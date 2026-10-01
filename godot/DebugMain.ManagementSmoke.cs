using System;
using System.IO;
using System.Linq;
using Godot;
using MangakaSim;

namespace MangakaGame;

public partial class DebugMain
{
    private async void RunManagementSmoke()
    {
        SetProcess(false);
        try
        {
            Check(ManagementInterface&&_managementReady&&TitleOpen,"Management entry point and menu");
            _careers=new CareerStore(ProjectSettings.GlobalizePath("res://../TestResults/careers-"+Guid.NewGuid().ToString("N")));
            await CaptureSmokeImage("management-menu");
            NewCareerMenu();Press("Begin career");await SettleUi();
            Check(!_menu.Visible&&!TitleOpen&&_homeOffice.CompanionVisible,"New career with companion");
            CheckMoneyFeedback();
            CheckUiPolishNavigation();
            var baseline=_state.ToJson();var desks=_state.Offices.Sum(l=>l.Placements.Count);
            Navigate("Staff");Navigate("Finances");Navigate("Studios");Navigate("Help");GoBack();
            Check(_page=="Studios"&&_state.ToJson()==baseline,"Read-only navigation and Back");
            Check(_state.Offices.Sum(l=>l.Placements.Count)==desks,"Helper desk does not consume furniture");
            OpenOfficeSidebar("Inbox");await CaptureSmokeImage("management-office");
            _homeOffice.FocusCompanion();await CaptureSmokeImage("management-helper-model");_homeOffice.ResetCamera();
            ShowStory("beside");await CaptureSmokeImage("management-helper");
            Press(HelperStories.Describe(_state,"beside").First);await SettleUi();
            Check(_state.Career.Journal.Count==1&&!_storyOpen,"Story choice through actual controls");
            Navigate("New series");((LineEdit)_sideContent.FindChild("OngoingTitle",true,false)).Text="Paper Lantern";Press("Create ongoing series");await SettleUi();OpenWorkspace("Production");
            Check(_state.Series.Count==1,"Focused series form creates manga");
            await CaptureSmokeImage("management-workbench");_report.Hide();
            var series=_state.Series.Single();Navigate("Showcase",series.Id);await SettleUi();
            Check(ShowcaseTexture(series.Id,series.Genre,0) is AtlasTexture,"Bundled cover loads");
            await CaptureSmokeImage("management-showcase");
            var custom=Image.CreateEmpty(8,8,false,Image.Format.Rgba8);custom.Fill(Colors.Coral);
            _presentation.Artwork[$"{series.Id}:0"]=_careers.ImportAsset(_careerId,custom.SavePngToBuffer());
            Check(ShowcaseTexture(series.Id,series.Genre,0) is ImageTexture,"Imported cover replaces fallback");
            var save=SaveCareer("Smoke checkpoint");var json=_state.ToJson();
            _state.Advance(24*35);LoadCareer(save);await SettleUi();
            Check(_state.ToJson()==json&&_presentation.Artwork.Count==1,"Career restores state and custom art");
            var imported=_careers.Import(_careers.Export(save));LoadCareer(imported);
            Check(_careerId!=save.Career&&ShowcaseTexture(series.Id,series.Genre,0) is ImageTexture,"Portable package restores artwork in a new career");
            _presentation.Artwork.Clear();_state.Advance(24*45);_scanIndex=_state.Events.Count;_dirty=true;await SettleUi();
            Navigate("Finances");await CaptureSmokeImage("management-finances");
            if(OS.GetCmdlineUserArgs().Contains("--capture"))
            {
                var previousSize=GetWindow().Size;
                foreach(var size in new[]{new Vector2I(1920,1080),new Vector2I(2560,1080)})
                {
                    GetWindow().Size=size;
                    foreach(var page in new[]{"Finances","Books","Staff"})
                    {Navigate(page);await CaptureSmokeImage($"polished-{page.ToLowerInvariant()}-{size.X}x{size.Y}");}
                }
                GetWindow().Size=previousSize;Navigate("Finances");
            }
            Check(_state.Career.Rankings.Count>0,"Ranking history recorded");
            _state.Events.Add(new GameEvent{Time=_state.Clock.Now,ActivityDate=_state.Clock.Now.Date,Type=EventType.DeadlineMissed,Message="Notification smoke fixture: publisher deadline missed.",SeriesId=series.Id});
            var urgentId=_state.Events.Count-1;var urgent=_state.Events[urgentId];
            _presentation.Stories=false;QueueImportantEvent(urgent,urgentId);QueueImportantEvent(urgent,urgentId);
            Check(_popupEvents.Count==1,"Duplicate urgent messages coalesce even with optional stories off");
            SetSpeed(2);ShowEvent(_popupEvents.Dequeue());Check(_speed==0&&_helperPopup.Visible,"Urgent notice pauses with Helper-Chan");Press("Keep in inbox");_presentation.Stories=true;
            foreach(var page in new[]{"Inbox","Series","Staff","Finances","Studios","Industry","Help"}){Navigate(page);await SettleUi();Check(_sideContent.GetChildCount()>0,page+" populated");}
            GetWindow().Size=new(1280,720);Navigate("Series");await CaptureSmokeImage("management-1280");
            Check(_shell.GetGlobalRect().End.X<=GetViewportRect().Size.X+1,"Shell fits 1280 width");
            foreach(var size in new[]{new Vector2I(1920,1080),new Vector2I(2560,1080),new Vector2I(3440,1440)})
            {
                GetWindow().Size=size;Navigate("Studios");await SettleUi();
                Check(_shell.GetGlobalRect().End.X<=GetViewportRect().Size.X+1,$"Management shell fits {size.X}x{size.Y}");
                OpenWorkspace("Production");await SettleUi();
                Check(!_mainTabs.TabsVisible&&_report.Visible&&_rail.IsVisibleInTree()&&_currentProgress.IsVisibleInTree(),"Action workspace retains navigation and HUD");
                OpenOfficeSidebar("Series");await SettleUi();
                Check(_homeOffice.Visible&&_side.Visible,"Office and explicit Series sidebar remain available");
                var camera=_homeOffice.CapturePreferences();Navigate("Series details",series.Id);GoBack();await SettleUi();
                Check(_officeSidebar&&_homeOffice.Visible&&System.Text.Json.JsonSerializer.Serialize(camera)==System.Text.Json.JsonSerializer.Serialize(_homeOffice.CapturePreferences()),"Back restores sidebar and office camera");
                await CaptureSmokeImage($"gui-{size.X}x{size.Y}");
                Check(_sideTitle.GetLineCount()==1,"Sidebar heading stays on one readable line");
                Check(!_viewOffice.ClipText&&_viewOffice.Size.X>=_viewOffice.GetCombinedMinimumSize().X,"Studio selector keeps its full current name visible");
                foreach(var legend in _sideContent.FindChildren("ChapterStageLegend","HFlowContainer",true,false))
                    Check(legend.FindChildren("*","Label",true,false).OfType<Label>().All(label=>label.GetLineCount()==1),"Progress legend wraps whole stage names, not letters");
            }
            _presentation.CompactUi=true;_presentation.ReducedUiMotion=true;ApplyUiTheme();
            var guiSave=SaveCareer("GUI preferences");LoadCareer(guiSave);await SettleUi();
            Check(_presentation.CompactUi&&_presentation.ReducedUiMotion&&_officeSidebar,"GUI preferences and sidebar survive career load");
            _presentation.CompactUi=false;_presentation.ReducedUiMotion=false;ApplyUiTheme();
            GetWindow().Size=new(1280,720);
            _presentation.UiScale=1.5;ApplyTextScale();Navigate("Inbox");await CaptureSmokeImage("management-large-text");
            Check(_sideScroll.Size.Y>=140,"Large text retains a usable scrolling page at 720p");
            _popupEvents.Clear();ShowStory(_state.Career.PendingScene!);await CaptureSmokeImage("management-large-dialogue");Press("Read later");
            _presentation.UiScale=1;ApplyTextScale();GetWindow().Size=new(1600,900);
            foreach(var offerId in Enumerable.Range(1,16))
            {
                _state=GameState.NewGame(33);_state.ControlledBusiness.Account.OpeningBalance+=100000000;_state.Money+=100000000;
                _state.Apply(new StudioActionCommand(StudioAction.Move,offerId));_state.Advance(24);
                var location=_state.Protagonist.Employment!.LocationId;var arrangement=_state.ArrangeOffice(location,true,true);
                _state.Apply(new ApplyOfficeLayoutCommand(location,_state.OfficeRevision,arrangement.Placements,arrangement.Purchases,[]));
                // Explicitly put the fixture's mangaka at work: relocation/off-duty
                // activity correctly hides both the mangaka and their companion.
                _state.OfficeActivities=_state.ControlledStaff.Select(p=>new OfficeActivity(p.Id,p.Employment!.LocationId,OfficeActivityKind.Work,_state.Clock.Now,Stage.Inks)).ToList();
                var before=_state.ToJson();var capacity=_state.UsableWorkspaces(location);_homeOffice.Bind(_state,location);
                Check(_homeOffice.CompanionVisible&&capacity==_state.UsableWorkspaces(location)&&_state.ToJson()==before,$"Companion preserves filled property {offerId}");
                _scanIndex=_state.Events.Count;_viewLocation=location;_lastAutosave=_state.Clock.Now.Date;_dirty=true;await SettleUi();
                if(offerId is 1 or 16)await CaptureSmokeImage($"management-property-{offerId}");
                _homeOffice.Bind(_state,_state.Locations.First(l=>l.BusinessId==_state.World.RivalBusinesses[0]).Id);Check(!_homeOffice.CompanionVisible,"Helper appears only beside protagonist");
            }
            var fullLocation=_state.Protagonist.Employment!.LocationId;
            for(var i=1;i<32;i++)
            {
                var p=new Person{Id=_state.AllocateId(),Name=$"Assistant {i}",ExpectedSalary=StudioRules.MinimumMonthlySalary,Skills=StageOrder.All.ToDictionary(s=>s,_=>60)};
                p.EmploymentHistory.Add(new(){BusinessId=_state.ControlledBusinessId,LocationId=fullLocation,StartsAt=_state.Clock.Now,MonthlySalary=StudioRules.MinimumMonthlySalary});_state.People.Add(p);
            }
            _state.Apply(new StudioActionCommand(StudioAction.SetFounderSalary));
            _state.OfficeActivities=_state.ControlledStaff.Select(p=>new OfficeActivity(p.Id,fullLocation,OfficeActivityKind.Work,_state.Clock.Now,Stage.Inks)).ToList();
            _scanIndex=_state.Events.Count;_dirty=true;await SettleUi();Check(_homeOffice.ActorCount==32&&_homeOffice.CompanionVisible,"Full workforce plus Helper-Chan");
            var visualState=_state.ToJson();SetSpeed(8);_homeOffice.Speed=8;
            if(DisplayServer.GetName()!="headless")
            {
                var samples=new System.Collections.Generic.List<double>();var last=Time.GetTicksUsec();
                for(var frame=0;frame<150;frame++){await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);var now=Time.GetTicksUsec();if(frame>=30)samples.Add((now-last)/1000d);last=now;}
                samples.Sort();GD.Print($"MANAGEMENT PERFORMANCE: 32 staff, 6 ambient, Helper-Chan, 8x; median {samples[samples.Count/2]:F2}ms; p95 {samples[(int)(samples.Count*.95)]:F2}ms.");
            }
            Check(_state.ToJson()==visualState,"Companion poses and full office rendering do not tick the simulation");Pause();
            await CaptureSmokeImage("management-full-studio");
            _state=GameState.NewGame();_state.Apply(new StudioActionCommand(StudioAction.CareerEmployer,Value:1));_state.Advance(24);ResetManagementSession();await SettleUi();
            Check(_state.Control==ControlMode.EmployedLead&&_homeOffice.Companion is not null,"Helper follows career into an employer, including while the mangaka is off duty");await CaptureSmokeImage("management-employer");
            GD.Print($"MANAGEMENT PASS: {_smokeChecks} checks");GetTree().Quit(0);
        }
        catch(Exception ex){GD.PrintErr("MANAGEMENT FAIL: "+ex);GetTree().Quit(1);}
    }
}

