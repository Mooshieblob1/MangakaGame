using System;
using System.IO;
using System.Linq;
using Godot;
using MangakaSim;

namespace MangakaGame;

public partial class DebugMain
{
    // Opt-in checks for the menu and overnight presentation; no production shortcuts.
    private async void RunAtmosphereSmoke()
    {
        SetProcess(false);_homeOffice.SetProcess(false);_officeView.SetProcess(false);
        try
        {
            Directory.CreateDirectory(SmokeOutput);
            _careers=new CareerStore(Path.Combine(SmokeOutput,"atmosphere-"+Guid.NewGuid().ToString("N")));
            foreach(var size in new[]{new Vector2I(1280,720),new Vector2I(1920,1080),new Vector2I(2560,1080)})
            {
                // New careers start from the title screen (spec 2026-09-28, Q40), not the in-game menu.
                GetWindow().Size=size;OpenTitle();await SettleUi();
                Check(ButtonNamed("New Career").IsVisibleInTree(),"Main menu exposes career actions");
                Check(_homeOffice.GetGlobalRect().IsEqualApprox(GetViewportRect()),"Office fills the whole canvas behind floating panels");
                Check(_floatingUi!.FindChildren("*","Button",true,false).OfType<Button>().Count(b=>b.Text=="Menu")==1,"Floating controls contain one Menu action");
                await CaptureSmokeImage($"opening-{size.X}x{size.Y}");
                NewCareerMenu();await SettleUi();
                Check(!ButtonNamed("Show assist options").IsVisibleInTree(),"Character setup starts without sandbox clutter");
                var name=_menuContent.FindChildren("CreatorName","LineEdit",true,false).OfType<LineEdit>().Single();
                var preview=_menuContent.FindChildren("CreatorPreview","SubViewportContainer",true,false).OfType<CreatorPreview>().Single();
                Check(name.Text=="Aki"&&preview.IsVisibleInTree(),"Creator starts with Aki and a visible 3D preview");
                var original=_state.ToJson();name.Text="  Haruka  ";
                var picks=new[]{2,1,4,2,2,1};
                for(var i=0;i<picks.Length;i++)
                {
                    var option=_menuContent.FindChildren($"CreatorAppearance{i}","OptionButton",true,false).OfType<OptionButton>().Single();
                    option.Select(picks[i]);option.EmitSignal(OptionButton.SignalName.ItemSelected,picks[i]);
                }
                _menuContent.FindChildren("CreatorGlasses","CheckBox",true,false).OfType<CheckBox>().Single().ButtonPressed=true;
                Check(preview.Recipe==new AppearanceRecipe(2,1,4,2,true,1,2)&&_state.ToJson()==original,"Modular appearance preview updates clothing and build without changing the current career");
                await CaptureSmokeImage($"creator-preview-{size.X}x{size.Y}");
                Press("Career rules");await SettleUi();
                Check(ButtonNamed("Show assist options").IsVisibleInTree(),"Career rules expose optional assists");
                await CaptureSmokeImage($"career-rules-{size.X}x{size.Y}");
                var selectedLook=preview.Recipe;
                Press("Begin career");await SettleUi();
                Check(_state.Protagonist.Name=="Haruka"&&_state.ControlledBusiness.Name=="Haruka Studio"&&_state.Protagonist.Appearance==selectedLook,"Starting career uses the displayed identity and appearance");
                Check(_homeOffice.StaffActors[_state.ProtagonistPersonId].UsingModularKit&&_homeOffice.StaffActors[_state.ProtagonistPersonId].DisplayedAppearance==selectedLook,"Office imports the modular kit and uses the exact saved recipe");
                Check(_officeDashboard.Visible&&GetViewportRect().Encloses(_officeDashboard.GetGlobalRect()),"Office dashboard fits the reference layout");
                await CaptureSmokeImage($"residential-office-{size.X}x{size.Y}");
                var cameraOnlyState=_state.ToJson();
                foreach(var zoom in new[]{4f,40f})foreach(var angle in new[]{0f,.7f,1.6f,2.8f,4f,5.5f})
                {
                    var prefs=_homeOffice.CapturePreferences();
                    prefs.Cameras[prefs.Location]=new[]{angle,zoom,30f,-30f,20f};
                    _homeOffice.RestorePreferences(prefs);
                    Check(_homeOffice.NeighborhoodEdgesHidden(),"Restored camera keeps all world edges outside the full viewport");
                    foreach(var drag in new[]{new Vector2(100000,100000),new Vector2(-100000,100000),new Vector2(-100000,-100000),new Vector2(100000,-100000)})
                    {
                        _homeOffice.PanScreenPixels(drag);
                        Check(_homeOffice.NeighborhoodEdgesHidden(),"Hard panning cannot reveal neighborhood edges at any supported zoom or aspect");
                    }
                }
                _homeOffice.FocusCompanion();Check(_homeOffice.NeighborhoodEdgesHidden(),"Helper focus preserves world bounds");
                _homeOffice.ResetCamera();Check(_state.ToJson()==cameraOnlyState,"Neighborhood navigation does not alter simulation or RNG");
            }
            GetWindow().Size=new(1920,1080);
            {
                // A recap held behind a Helper-Chan notice follows it after Escape; resuming instead still skips the night.
                _presentation=new(){Page="Office"};
                _state=GameState.NewGame(2);_state.Apply(new CreateDoujinCommand("Evening pages","drama",64));_state.Advance(1);
                ResetManagementSession();ShowOffice();_helperPopup.Hide();_popupEvents.Clear();_recapDialog.Hide();await SettleUi();
                _scanIndex=_state.Events.Count;SetSpeed(8);
                _state.Events.Add(new(){Time=_state.Clock.Now,ActivityDate=_state.Clock.Now.Date,Type=EventType.StaffNotice,Message="Held recap fixture notice"});
                _state.Advance(9);ScanEvents();
                var held=_pendingRecap;
                Check(held is not null&&!_recapDialog.Visible,"A recap that arrives with a notice waits for it");
                await SettleUi();
                Check(_helperPopup.Visible&&!_recapDialog.Visible,"The notice shows first, without the recap on top");
                _UnhandledKeyInput(new InputEventKey{Pressed=true,Keycode=Key.Escape});await SettleUi();
                Check(!_helperPopup.Visible&&_recapDialog.Visible,"Closing the notice with Escape brings up the held recap");
                _recapDialog.Hide();_pendingRecap=held;SetSpeed(8);RefreshManagement();
                Check(_pendingRecap is null&&_overnightTarget is not null,"Resuming with a held recap still begins the overnight skip");
                CancelOvernight();Pause();
            }
            foreach(var previous in new[]{1d,2d,4d,8d})
            {
                _presentation=new(){Page="Office"};
                _state=GameState.NewGame(2);_state.Apply(new CreateDoujinCommand("Evening pages","drama",64));_state.Advance(1);
                ResetManagementSession();ShowOffice();_helperPopup.Hide();_popupEvents.Clear();
                var lead=_homeOffice.StaffActors[_state.ProtagonistPersonId];var helper=_homeOffice.Companion!;
                _state.Advance(9);_scanIndex=_state.Events.Count;_dirty=true;Refresh();
                SetSpeed(previous);Pause();var before=_state.Clock.Now;
                var expected=before.AddHours(_state.HoursUntilNextWork());
                var logBefore=string.Join("",_timeline!.Files().Select(f=>f.Text)).Length;
                OnRecapContinue();
                _speedTween?.Kill();_speedFlash?.Hide();
                Check(_speed==32&&_resumeSpeed==previous&&_state.Clock.Now==before,"Continue starts a temporary 32x transition without jumping the clock");
                Check(lead.Visible&&helper.Visible&&lead.Moving&&helper.Moving,"Both characters begin a visible closing-time walk");
                _homeOffice.GrabFocus();
                var pausedTarget=_overnightTarget;var leadPosition=lead.Position;var helperPosition=helper.Position;
                _speedButtons[0].EmitSignal(BaseButton.SignalName.Pressed);
                _Process(.5);_homeOffice._Process(.5);
                Check(_speed==0&&_overnightTarget==pausedTarget&&_overnightDeparting&&_state.Clock.Now==before&&
                    lead.Position==leadPosition&&helper.Position==helperPosition&&_resumeSpeed==previous,
                    "The pause button freezes closing-time departures without cancelling the night or replacing daytime speed");
                Check(_speedButtons.Where(p=>p.Key>0).All(p=>p.Value.Disabled)&&_overnightSpeedBadge!.Text.Contains("PAUSED"),
                    "Paused night is explicit and daytime speed buttons remain unavailable");
                _speedButtons[0].EmitSignal(BaseButton.SignalName.Pressed);
                Check(_speed==32&&_overnightTarget==pausedTarget&&_resumeSpeed==previous,
                    "The pause button resumes the same overnight transition at 32x");
                _state.Settings.AutoPause[EventType.DayStarted]=true;
                _state.Events.Add(new(){Time=before,ActivityDate=before.Date,Type=EventType.StaffNotice,Message="Overnight fixture notice"});
                Check(!ScanEvents()&&_speed==32,"Overnight notices do not interrupt the transition");
                var dark=false;var empty=false;var dawn=false;var midnight=false;var elapsed=0d;
                for(var frame=0;frame<3000&&_overnightTarget is not null;frame++)
                {
                    _Process(1d/60);_homeOffice._Process(1d/60);
                    elapsed+=1d/60;
                    if(_overnightTarget is null)break;
                    Check(!_helperPopup.Visible&&!_recapDialog.Visible,"No notifications appear during the overnight skip");
                    if(_overnightDeparting)Check(_state.Clock.Now==before,"Clock waits for doorway departures");
                    empty|=!lead.Visible&&!helper.Visible;
                    if(_homeOffice.RoomLightLevel<.05f&&!dark)
                    {dark=true;if(previous==1)await CaptureSmokeImage("overnight-lights-out");}
                    if(_state.Clock.Hour==6&&!dawn)
                    {dawn=true;if(previous==1)await CaptureSmokeImage("overnight-dawn");}
                    if(_state.Clock.Hour==0&&!midnight)
                    {
                        var pausedClock=_state.Clock.Now;var fraction=_accumulator;var hold=_overnightHold;
                        HandleTimeShortcut(Key.Space);_Process(.5);_homeOffice._Process(.5);
                        Check(_speed==0&&_state.Clock.Now==pausedClock&&_accumulator==fraction&&_overnightHold==hold&&
                            _overnightTarget==pausedTarget&&_homeOffice.ClosedForNight,
                            "Space freezes the night clock and keeps the room closed at midnight");
                        HandleTimeShortcut(Key.Space);
                        Check(_speed==32&&_resumeSpeed==previous,"Space resumes at 32x without losing the daytime choice");
                        HandleTimeShortcut(Key.Key1);Check(_speed==0,"The slower shortcut pauses the dedicated night transition");
                        HandleTimeShortcut(Key.Key2);Check(_speed==32,"The faster shortcut resumes the dedicated night transition");
                        SetSpeed(8);Check(_speed==32&&_resumeSpeed==previous&&_overnightTarget==pausedTarget,
                            "A playback request cannot downgrade overnight to ordinary 8x");
                        midnight=true;if(previous==1)await CaptureSmokeImage("overnight-midnight");
                        Check(_homeOffice.GetGlobalRect().Encloses(OvernightPanel!.GetGlobalRect()),"Overnight caption stays inside the office viewport");
                    }
                }
                Check(_overnightTarget is null&&_state.Clock.Now==expected,"Overnight ends at the next scheduled work hour without overshooting");
                var night=string.Join("",_timeline!.Files().Select(f=>f.Text))[logBefore..];
                Check(night.Contains("| overnight")&&night.Contains("| morning speed")&&!night.Contains("speed 32x")&&!night.Contains("screen Office"),
                    "The timeline logs the overnight skip as overnight and morning, not as a 32x choice or a screen change");
                Check(_speedButtons.All(p=>!p.Value.Disabled),"Daytime speed buttons are available again after sunrise");
                Check(dark&&empty&&dawn,"Empty room, lights out and dawn are all visible phases");
                Check(elapsed<(expected-before).TotalHours*SecondsPerHourAt1x/8,"Complete overnight presentation is faster than ordinary 8x playback");
                Check(_resumeSpeed==previous&&(_speed==previous||_speed==0&&_helperPopup.Visible),"Previous speed is restored; morning notices may use the ordinary pause");
                Check(_helperPopup.Visible,"Queued overnight notice is delivered in the morning");
                _helperPopup.Hide();_popupEvents.Clear();SetSpeed(_resumeSpeed);
                Check(_speed==previous,"Resume never leaves the game at 32x");
            }
            Check(_homeOffice.AmbientTraffic.Select(t=>t.Actor.ResidentRole).OrderBy(r=>r).SequenceEqual(new[]{"Father","Mother"}),"Home has two fixed parent models");
            Check(_homeOffice.AmbientTraffic.All(t=>t.Actor.UsingImportedModel),"Both approved parent rigs replace the procedural placeholders");
            Check(Math.Abs(10*SecondsPerHourAt1x/8-45)<.001,"Default ten-hour workday lasts 45 seconds at 8x");
            Check(OvernightSpeed==32&&OvernightSecondsPerHourAt1x==2.5,"Longer days retain the brief dedicated 32x overnight transition");
            var parent=_homeOffice.AmbientTraffic[0];parent.Reset();parent.Update(1,1,0,true);
            Check(!parent.Actor.Visible,"Parents have a quiet initial interval");
            parent.Update(12,1,0,true);
            for(var i=0;i<1200&&parent.NeedsNightReturn;i++)parent.Update(1d/60,1,0,false);
            var visits=parent.CompletedCrossings;
            parent.Update(20,1,0,true);Check(parent.CompletedCrossings==visits&&!parent.Actor.Visible,"Parents wait much longer between destination visits");
            await CheckParentMeals();
            GD.Print($"ATMOSPHERE SMOKE PASSED: {_smokeChecks} checks.");GetTree().Quit();
        }
        catch(Exception ex){GD.PrintErr("ATMOSPHERE SMOKE FAILED: "+ex);GetTree().Quit(1);}
    }
}
