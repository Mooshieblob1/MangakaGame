using System;
using System.IO;
using System.Linq;
using Godot;
using MangakaSim;

namespace MangakaGame;

public partial class DebugMain
{
    private async void RunUsabilitySmoke()
    {
        SetProcess(false);
        try
        {
            Directory.CreateDirectory(SmokeOutput);_careers=new CareerStore(Path.Combine(SmokeOutput,"usability-careers-"+Guid.NewGuid().ToString("N")));
            NewCareerMenu();Press("Begin career");await SettleUi();_helperPopup.Hide();_side.Hide();_homeOffice.GrabFocus();
            Check(_darkMode,"Dark mode defaults on");
            var weightedChapter=new Chapter{Stages=[
                new(){Stage=Stage.Name,HoursRequired=10,HoursDone=10,Status=StageStatus.Complete},
                new(){Stage=Stage.Pencils,HoursRequired=30,HoursDone=0,Status=StageStatus.InProgress}]};
            Check(ChapterPercent(weightedChapter)==25,"A completed stage remains in total progress when the next stage starts");
            weightedChapter.Stages[1].HoursDone=15;
            Check(ChapterPercent(weightedChapter)==62.5,"Overall progress weights stages by work rather than giving them equal shares");
            weightedChapter.Stages[1].Status=StageStatus.Skipped;
            Check(ChapterPercent(weightedChapter)==100,"Skipped work leaves a finished chapter at 100 percent");
            Check(_currentProgress.Visible&&_progressSeries.Disabled,"Progress strip explains the no-series state");
            foreach(var angle in new[]{0f,90f,135f})
            {
                _homeOffice.ResetCamera();_homeOffice.RotateCamera(angle);
                var point=new Vector3(2,0,2);var before=_homeOffice.ProjectPoint(point);
                _homeOffice._GuiInput(new InputEventMouseButton{ButtonIndex=MouseButton.Middle,Pressed=true});
                _homeOffice._GuiInput(new InputEventMouseMotion{Relative=new(50,25),ButtonMask=MouseButtonMask.Middle});
                _homeOffice._GuiInput(new InputEventMouseButton{ButtonIndex=MouseButton.Middle,Pressed=false});
                var moved=_homeOffice.ProjectPoint(point)-before;
                Check(moved.DistanceTo(new(50,25))<2,$"Screen-aligned middle drag at {angle} degrees: {moved}");
            }
            var prefs=_homeOffice.CapturePreferences();var projected=_homeOffice.ProjectPoint(Vector3.Zero);
            Check(prefs.Cameras[prefs.Location].Length==5,"Camera preferences include vertical pan");
            _homeOffice.ResetCamera();_homeOffice.RestorePreferences(prefs);
            Check(_homeOffice.ProjectPoint(Vector3.Zero).DistanceTo(projected)<.1,"Camera pan restores without drifting");
            prefs.Cameras[prefs.Location]=prefs.Cameras[prefs.Location].Take(4).ToArray();_homeOffice.RestorePreferences(prefs);
            Check(_homeOffice.CapturePreferences().Cameras[prefs.Location][4]==0,"Legacy four-value camera preferences remain valid");
            var origin=_homeOffice.ProjectPoint(Vector3.Zero);PanOffice(Vector2.Right,.05);
            var shifted=_homeOffice.ProjectPoint(Vector3.Zero)-origin;Check(shifted.X<0&&Math.Abs(shifted.Y)<.1,"D pans right along screen axes");
            origin=_homeOffice.ProjectPoint(Vector3.Zero);PanOffice(Vector2.Up,.05);shifted=_homeOffice.ProjectPoint(Vector3.Zero)-origin;
            Check(shifted.Y>0&&Math.Abs(shifted.X)<.1,"W pans up along screen axes");_homeOffice.ResetCamera();
            SetSpeed(4);_Input(new InputEventKey{Keycode=Key.Space,Pressed=true});Check(_speed==0,"Space pauses through input handler");
            _Input(new InputEventKey{Keycode=Key.Space,Pressed=true});Check(_speed==4,"Space restores prior speed");
            foreach(var expected in new[]{2,1,0,0}){HandleTimeShortcut(Key.Key1);Check(_speed==expected,"1 decreases speed with pause boundary");}
            foreach(var expected in new[]{1,2,4,8,8}){HandleTimeShortcut(Key.Key2);Check(_speed==expected,"2 increases speed with maximum boundary");}
            _Input(new InputEventKey{Keycode=Key.Space,Pressed=true,Echo=true});Check(_speed==8,"Key repeat does not toggle pause repeatedly");
            ShowMenu();Check(!HandleTimeShortcut(Key.Space)&&!GameKeysAvailable(true),"Menu blocks time and movement shortcuts");
            _inMenu=false;_menu.Hide();_helperPopup.Show();Check(!HandleTimeShortcut(Key.Key2),"Helper popup blocks shortcuts");_helperPopup.Hide();
            Navigate("New doujin");var title=GetNode<LineEdit>("%DoujinTitle");title.GrabFocus();
            Check(!HandleTimeShortcut(Key.Space)&&!GameKeysAvailable(true),"Typing a title blocks shortcuts");title.Text="Finding the readers";title.ReleaseFocus();Press("Create one-shot doujin");await SettleUi();
            var first=_state.Series[0];var firstStage=first.Chapters[0].Stages[0];
            Check(_currentProgress.Visible&&_progressSeriesId==first.Id&&_currentStageBar.Value==0,"New current series shows initial chapter progress");
            _state.Advance(1);_dirty=true;await SettleUi();
            Check(firstStage.HoursDone>0&&Math.Abs(_currentStageBar.Value-Math.Round(ChapterPercent(first.Chapters[0]),1))<.11,"Overall chapter bar updates from work across all stages");
            await CaptureSmokeImage("current-series-progress");Workbench(0);await SettleUi();
            Check(_currentProgress.GetGlobalRect().End.Y<=_report.GetGlobalRect().Position.Y+1,"Current progress remains above the expanded workbench");_report.Hide();
            var untouched=_state.ToJson();RefreshCurrentProgress();Check(_state.ToJson()==untouched,"Progress display is read-only");
            _state.Apply(new PauseSeriesCommand(first.Id));RefreshCurrentProgress();Check(_progressDescription.Text.Contains("Series paused"),"Paused series is labeled explicitly");
            _state.Apply(new ResumeSeriesCommand(first.Id));
            for(var h=0;h<200&&!firstStage.IsDone;h++)_state.Advance(1);
            _dirty=true;await SettleUi();Check(firstStage.IsDone&&!_progressDescription.Text.Contains("Storyboard")&&
                _currentStageBar.Value>=100*firstStage.HoursRequired/first.Chapters[0].Stages.Sum(s=>s.HoursRequired)-.001,
                "Current-stage description follows production without resetting overall progress");
            _state.Apply(new CreateDoujinCommand("Another story","drama"));_dirty=true;await SettleUi();
            SelectSeriesForWorkbench(_state.Series[1].Id);Check(_progressSeriesId==_state.Series[1].Id&&_currentStageBar.Value==0,"Current progress follows selected series");
            _progressSeries.Select(_progressSeries.GetItemIndex(first.Id));_progressSeries.EmitSignal(OptionButton.SignalName.ItemSelected,(long)_progressSeries.Selected);
            Check(_progressSeriesId==first.Id&&_seriesOption.GetSelectedId()==first.Id,"Top series selector synchronizes the workbench");
            Navigate("Books");await SettleUi();Check(_sideContent.FindChildren("*","Label",true,false).OfType<Label>().Any(l=>l.Text.Contains("In production")),"Books explains unfinished production");
            for(int day=0;day<180&&_state.Series[0].Volumes.Count==0;day++)_state.Advance(24);
            _lastAutosave=_state.Clock.Now.Date;_scanIndex=_state.Events.Count;_popupEvents.Clear();_dirty=true;await SettleUi();Navigate("Books");Press("Print physical copies");await SettleUi();
            Check(_currentStageBar.Value==100&&_progressDescription.Text.Contains("Book ready"),"Finished doujin displays completion instead of a stale active stage");
            var label=(Label)_sideContent.FindChild("DistributionStatus",true,false);Check(label.Text=="Ready to print","Books opens selected book with clear distribution state");
            _alphaCopies.Value=7;Press("Order this print run");Check(label.Text.Contains("awaiting delivery"),"Order updates printing state immediately");
            Check(ButtonNamed("Order this print run").Disabled,"Duplicate order is visibly unavailable");
            _state.Advance(24);_lastAutosave=_state.Clock.Now.Date;_scanIndex=_state.Events.Count;_popupEvents.Clear();RefreshManagement();
            Check(label.Text=="On sale locally"&&_alphaCopies.Value==7,"Delivery refreshes live status without resetting the form");
            Check(!ButtonNamed("Order this print run").Disabled,"Delivered order permits another batch");
            _sideScroll.ScrollVertical=0;await CaptureSmokeImage("usability-dark-printing");
            var save=SaveCareer("Printing view");LoadCareer(save);await SettleUi();
            Check(((Label)_sideContent.FindChild("DistributionStatus",true,false)).Text=="On sale locally","Loading on printing page rebinds controls to the loaded career");
            ShowMenu();Press("Settings");await SettleUi();
            var themeToggle=_menuContent.FindChildren("*","CheckBox",true,false).OfType<CheckBox>().Single(c=>c.Text=="Dark mode");themeToggle.ButtonPressed=false;
            Check(!_darkMode&&_backdrop.Color==new Color("e6e1d5"),"Light mode applies immediately");
            _darkMode=true;LoadUiPreferences();Check(!_darkMode,"Light preference persists independently of career");await CaptureSmokeImage("usability-light-settings");
            themeToggle.ButtonPressed=true;_darkMode=false;LoadUiPreferences();Check(_darkMode,"Dark preference persists");
            _menu.Hide();_inMenu=false;Navigate("Finances");await CaptureSmokeImage("usability-dark-finances");
            GetWindow().Size=new(1280,720);Navigate("Books");await SettleUi();await CaptureSmokeImage("usability-dark-books-720");
            Check(_shell.GetGlobalRect().End.X<=GetViewportRect().Size.X+1,"Books navigation fits 720p");
            GD.Print($"USABILITY SMOKE PASSED: {_smokeChecks} checks.");var tree=GetTree();tree.CreateTimer(.1).Timeout+=()=>tree.Quit();QueueFree();
        }
        catch(Exception ex){GD.PrintErr("USABILITY SMOKE FAILED: "+ex);GetTree().Quit(1);}
    }
}
