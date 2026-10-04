using System;
using System.IO;
using System.Linq;
using Godot;
using MangakaSim;

namespace MangakaGame;

public partial class DebugMain
{
    // Tier 1 mid-career pacing (Q26 to Q28): 32x routine days, the stop list and Helper-Chan's introduction.
    private async void RunQuietSpeedSmoke()
    {
        SetProcess(false);_homeOffice.SetProcess(false);_officeView.SetProcess(false);
        try
        {
            GetWindow().Size=new(1920,1080);
            Directory.CreateDirectory(SmokeOutput);
            _careers=new CareerStore(Path.Combine(SmokeOutput,"quiet-speed-"+Guid.NewGuid().ToString("N")));
            NewCareerMenu();Press("Begin career");await SettleUi();_helperPopup.Hide();

            // A career just past its first sale, with guidance caught up.
            _presentation=new(){Page="Office"};
            _state=GameState.NewGame(0);_state.Apply(new CreateDoujinCommand("First pages","adventure"));
            for(var d=0;d<180&&_state.Series[0].Volumes.Count==0;d++)_state.Advance(24);
            _state.Apply(new StudioActionCommand(StudioAction.Print,_state.Series[0].Volumes.Single().Id,Amount:10,Value:(int)PrintTier.CopyShop));
            _state.Advance(24*8);
            ResetManagementSession();ShowOffice();_helperPopup.Hide();_popupEvents.Clear();
            var prefs=_presentation.Guidance;
            CareerGuidance.Observe(_state,prefs);
            // A second doujin keeps the creator drawing, so working days end with a recap.
            _state.Apply(new CreateDoujinCommand("Second pages","romance"));
            CareerGuidance.Observe(_state,prefs);CareerGuidance.MarkRead(prefs);RefreshGuidance();await SettleUi();
            Check(prefs.Completed.Contains("first-sale")&&!prefs.Completed.Contains(CareerGuidance.QuietSpeedStep),"Fixture career is past its first sale and has not met 32x yet");

            // Frames of the real-time driver; ordinary pop-ups are dismissed and play resumes at the given speed.
            double Run(double speed,Func<bool> until,int limit=20000)
            {
                var elapsed=0d;
                for(var frame=0;frame<limit&&!until();frame++)
                {
                    if(_speed==0&&_overnightTarget is null)
                    {
                        if(_recapDialog.Visible)break;
                        _helperPopup.Hide();_popupEvents.Clear();CareerGuidance.MarkRead(prefs);SetSpeed(speed);
                    }
                    _Process(1d/30);_homeOffice._Process(1d/30);elapsed+=1d/30;
                    if(_overnightTarget is null&&_speed>=QuietSpeed)
                        Check(OfficePlaybackSpeed<=8&&_homeOffice.Speed<=8,"Office motion is capped at 8x during 32x days");
                }
                return elapsed;
            }

            // At 8x the day still ends with the recap, and a quiet day brings Helper-Chan's one-time text.
            SetSpeed(8);Check(_daySpeed==8,"8x is remembered as the daytime speed");
            var t8=Run(8,()=>_recapDialog.Visible);GD.Print($"QUIET SPEED: one 8x working day took {t8:0.0} real seconds.");
            Check(_recapDialog.Visible&&_speed==0,$"At 8x the working day still ends with the recap dialog (speed {_speed}, now {_state.Clock.Now})");
            Check(prefs.Thread.LastOrDefault()?.Step==CareerGuidance.QuietSpeedStep&&prefs.Completed.Contains(CareerGuidance.QuietSpeedStep),"A quiet 8x day brings the 32x introduction once");
            Check(prefs.Thread.SelectMany(m=>m.Texts).All(t=>t.Length<=CareerGuidance.TextLimit),"The 32x text fits the 140 character limit");
            _recapDialog.Hide();RefreshGuidance();await SettleUi();
            Check(PhoneSays("32×")&&PhoneSays("I'll stop you"),"Helper-Chan's phone shows the 32x introduction");
            var windowSize=GetWindow().Size;
            foreach(var (size,name,scale) in new(Vector2I,string,double)[]{(new(1920,1080),"1080",1),(new(1280,720),"720-150",1.5)})
            {
                SmokeLayout(size,scale);await SettleUi();for(var f=0;f<60&&GetViewport().GetVisibleRect().Size!=(Vector2)size;f++)await SettleUi();Check(GetViewport().GetVisibleRect().Size==(Vector2)size,$"Window reached {size}");
                OpenPhone(false);_phoneTween?.Kill();_phoneSlide=0;await SettleUi();await SettleUi();
                await CaptureSmokeImage($"quiet-speed-phone-{name}");
            }
            SmokeLayout(windowSize,1);await SettleUi();

            // Choosing 32x yourself also counts as the introduction.
            prefs.Completed.Remove(CareerGuidance.QuietSpeedStep);SetSpeed(4);ChooseSpeed(QuietSpeed);
            Check(_speed==32&&_daySpeed==4&&prefs.Completed.Contains(CareerGuidance.QuietSpeedStep),"Choosing 32x marks the introduction complete and keeps 4x as the return speed");
            Check(_speedButtons[32].TooltipText.Contains("routine days skip ahead")&&_speedButtons[32].TooltipText.Contains("Helper-Chan stops you"),"The 32x button explains what it does");

            // At 32x the day rolls into the night without the recap, and the next morning continues at 32x.
            var overnights=0;
            // Fresh-player finding A2: the page the player is using stays open, at its scroll position, through the night.
            Navigate("Finances");await SettleUi();_sideScroll.ScrollVertical=60;await SettleUi();var scrolled=_sideScroll.ScrollVertical;
            Run(QuietSpeed,()=>_overnightTarget is not null);
            Check(_side.Visible&&_page=="Finances"&&_sideScroll.ScrollVertical==scrolled,$"At 32x the night keeps the open page and its scroll position (page {_page}, scroll {_sideScroll.ScrollVertical} of {scrolled})");
            Check(_overnightTarget is not null&&!_recapDialog.Visible,"At 32x the working day ends without the recap dialog and the night begins");
            Check(_state.Events.Last(e=>e.Type==EventType.DailyRecap).Time<=_state.Clock.Now,"The day's recap is still recorded for the inbox");
            Run(QuietSpeed,()=>_overnightTarget is null);overnights++;
            Check(_overnightTarget is null&&_speed==32&&_resumeSpeed==32,"The next morning continues at 32x");
            Check(_side.Visible&&_page=="Finances"&&_sideScroll.ScrollVertical==scrolled,$"The open page and scroll position survive into the next 32x morning (page {_page}, scroll {_sideScroll.ScrollVertical})");
            _speedTween?.Kill();_speedFlash?.Hide();
            foreach(var (size,name,scale) in new(Vector2I,string,double)[]{(new(1920,1080),"1080",1),(new(1280,720),"720-150",1.5)})
            {
                SmokeLayout(size,scale);ClosePhone();await SettleUi();await SettleUi();for(var f=0;f<60&&GetViewport().GetVisibleRect().Size!=(Vector2)size;f++)await SettleUi();Check(GetViewport().GetVisibleRect().Size==(Vector2)size,$"Window reached {size}");
                Check(_speedButtons[32].ButtonPressed&&_speedButtons[32].IsVisibleInTree(),$"32x header button is visible and active at {name}");
                await CaptureSmokeImage($"quiet-speed-header-{name}");
            }
            SmokeLayout(windowSize,1);await SettleUi();

            // One whole routine working day at 32x, measured in real seconds.
            var day=Run(QuietSpeed,()=>_overnightTarget is not null);
            GD.Print($"QUIET SPEED: one 32x working day took {day:0.0} real seconds.");
            Check(day is >5 and <15,$"A 32x working day takes about 11 real seconds ({day:0.0} s)");
            Run(QuietSpeed,()=>_overnightTarget is null);overnights++;
            Check(overnights==2&&_speed==32,"Consecutive routine days need no clicks at 32x");

            // Anything that needs the player stops the game and returns it to the slower speed.
            foreach(var type in new[]{EventType.SerializationOffered,EventType.CancellationWarning,EventType.DeadlineMissed})
            {
                SetSpeed(4);ChooseSpeed(QuietSpeed);_popupEvents.Clear();_helperPopup.Hide();
                _state.Events.Add(new(){Time=_state.Clock.Now,ActivityDate=_state.Clock.Now.Date,Type=type,Message="Staged "+type});
                Check(ScanEvents()&&_speed==0&&_resumeSpeed==4,$"{type} at 32x stops the game and resumes at 4x");
                TogglePause();Check(_speed==4,$"Resuming after {type} returns to 4x");
            }
            // Routine events at 32x do not stop the game.
            SetSpeed(4);ChooseSpeed(QuietSpeed);_popupEvents.Clear();_helperPopup.Hide();
            _state.Events.Add(new(){Time=_state.Clock.Now,ActivityDate=_state.Clock.Now.Date,Type=EventType.ChapterCompleted,Message="Staged chapter"});
            Check(!ScanEvents()&&_speed==32,"A completed chapter at 32x does not stop the game");
            // At 8x and below the stop list changes nothing.
            SetSpeed(8);_state.Settings.AutoPause[EventType.ChapterCompleted]=true;
            _state.Events.Add(new(){Time=_state.Clock.Now,ActivityDate=_state.Clock.Now.Date,Type=EventType.ChapterCompleted,Message="Staged chapter"});
            Check(ScanEvents()&&_speed==0&&_resumeSpeed==8,"At 8x Auto-pause still applies as before");

            // A day with no work has no recap, but the night still begins, at any speed (user report 2026-09-29).
            foreach(var speed in new double[]{8,QuietSpeed})
            {
                _state=GameState.NewGame(0);ResetManagementSession();ShowOffice();_helperPopup.Hide();_popupEvents.Clear();
                CareerGuidance.Observe(_state,prefs);CareerGuidance.MarkRead(prefs);SetSpeed(speed);
                Run(speed,()=>_overnightTarget is not null||_state.Clock.Now.Hour>=20);
                Check(_overnightTarget is not null&&!_recapDialog.Visible&&!_state.Events.Any(e=>e.Type==EventType.DailyRecap),
                    $"A day with no work still ends in the night skip at {speed}x (now {_state.Clock.Now:HH:mm})");
                Run(speed,()=>_overnightTarget is null);
                Check(_state.Clock.Now==new DateTime(1996,4,2,8,0,0)&&_speed==speed,$"After a quiet day's night the morning continues at {speed}x (now {_state.Clock.Now}, speed {_speed})");
            }

            GD.Print($"QUIET SPEED SMOKE PASSED: {_smokeChecks} checks.");var tree=GetTree();tree.CreateTimer(.1).Timeout+=()=>QuitTree(tree);QueueFree();
        }
        catch(Exception ex)
        {
            GD.PushError($"QUIET SPEED SMOKE FAILED: {ex.Message}");GetTree().Quit(1);
        }
    }
}
