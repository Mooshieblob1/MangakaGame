using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using MangakaSim;

namespace MangakaGame;

public partial class DebugMain
{
    // Work feedback and right-click back (approved 2026-10-03).
    private async void RunWorkFeedbackSmoke()
    {
        SetProcess(false);
        try
        {
            GetWindow().Size = new(1920, 1080); Directory.CreateDirectory(SmokeOutput);
            _careers = new CareerStore(Path.Combine(SmokeOutput, "work-feedback-" + Guid.NewGuid().ToString("N")));
            NewCareerMenu(); Press("Begin career"); await SettleUi(); _helperPopup.Hide();
            await CheckStudioIsland();
            await CheckWorkSparkles();
            await CheckRightClickBack();
            GD.Print($"WORK FEEDBACK SMOKE PASSED: {_smokeChecks} checks.");
            var tree = GetTree(); tree.CreateTimer(.1).Timeout += () => QuitTree(tree); QueueFree();
        }
        catch (Exception ex) { GD.PushError($"WORK FEEDBACK SMOKE FAILED: {ex.Message}\n{ex.StackTrace}"); GetTree().Quit(1); }
    }

    // Studio island (Q59): a new career seats Helper-Chan at her island desk, facing the spare desk.
    private async Task CheckStudioIsland()
    {
        ShowOffice(); RefreshManagement(); await SettleUi();
        var home = _state.Locations.Single(l => l.BusinessId == _state.ControlledBusinessId);
        Check(home.StudioIsland && _homeOffice.HelperDesk.X > 0, "A new career uses the studio island, with Helper-Chan's seat in the room");
        for (var h = 0; h < 48 && _homeOffice.Companion is not { AtDesk: true, Moving: false }; h++) { _state.Advance(1); RefreshManagement(); await SettleUi(); }
        for (var i = 0; i < 30; i++) await SettleUi();
        var helper = _homeOffice.Companion;
        Check(helper is { AtDesk: true } && helper.Destination.DistanceTo(_homeOffice.HelperDesk) < .01f && Mathf.IsEqualApprox(helper.Rotation.Y, Mathf.Pi / 2, .05f),
            $"Helper-Chan sits at her island desk facing the spare desk (at {helper?.Position}, facing {helper?.Rotation.Y:0.00})");
        await CaptureSmokeImage("studio-island");
    }

    private void FeedbackFor(double seconds) { for (var t = 0.0; t < seconds; t += .1) UpdateWorkFeedback(.1); }

    private async Task CheckWorkSparkles()
    {
        _state.Apply(new CreateDoujinCommand("Sparkle pages", "adventure"));
        ShowOffice(); RefreshManagement(); await SettleUi();
        for (var h = 0; h < 72 && (_currentStageBar.Chapter is not { } c || WorkingOn(c).Count == 0); h++) { _state.Advance(1); RefreshManagement(); }
        await SettleUi(); await SettleUi();
        var chapter = _currentStageBar.Chapter!; var working = WorkingOn(chapter);
        Check(working.Count > 0, "Someone is drawing the header series");
        var worker = working.Keys.First();
        Check(_homeOffice.PersonScreenPoint(worker) is not null && _currentStageBar.StagePoint(working[worker]) is not null,
            "The worker and the stage's place on the progress bar are both on screen");

        _speed = 8; var before = _sparkleLayer.Launched; FeedbackFor(3);
        Check(_sparkleLayer.Launched > before && _sparkleLayer.Count > 0, $"Sparkles flow from the worker at 8x ({_sparkleLayer.Launched - before} in 3 s)");
        await CaptureSmokeImage("work-feedback-sparkles");

        _speed = 0; before = _sparkleLayer.Launched; FeedbackFor(3);
        Check(_sparkleLayer.Launched == before, "No sparkles while paused");
        await CheckDeskOnlySparkles(worker);
        _speed = 8; Navigate("Inbox"); await SettleUi(); before = _sparkleLayer.Launched; FeedbackFor(3);
        Check(_sparkleLayer.Launched == before, "No sparkles while a page covers the office");
        ShowOffice(); await SettleUi();
        _presentation.ReducedUiMotion = true; before = _sparkleLayer.Launched; FeedbackFor(3);
        Check(_sparkleLayer.Launched == before, "Reduced interface motion turns sparkles off");
        _presentation.ReducedUiMotion = false;

        // A stage finishing shows one bubble near the bar, never more than one a second.
        var stage = chapter.StageWork(working[worker]); FeedbackFor(.2);
        var shown = WorkBubblesShown;
        stage.Status = StageStatus.Complete; RefreshManagement(); FeedbackFor(1.2); await SettleUi();
        Check(WorkBubblesShown == shown + 1 && GetChildren().OfType<Label>().Any(l => l.Name.ToString().StartsWith("StageBubble") && l.Text == StageLabel(stage.Stage) + " done!"),
            $"Finishing {StageLabel(stage.Stage)} shows a \"done\" bubble");
        await CaptureSmokeImage("work-feedback-bubble");
        _speed = 0;
    }

    // Desk-only sparkles (2026-10-03): nothing flows from the toilet or the break room, and over a working day the
    // count stays close to the old always-on flow because the seated rate makes up for the measured time away.
    private async Task CheckDeskOnlySparkles(int worker)
    {
        _speed = 8; _homeOffice.Speed = 8; // the check drives frames itself, so the office clock needs the speed too
        _homeOffice.SendToToilet(worker); for (var i = 0; i < 10; i++) { _homeOffice._Process(.1); await SettleUi(); }
        var before = _sparkleLayer.Launched; FeedbackFor(2);
        Check(!_homeOffice.AtDeskWorking(worker) && _sparkleLayer.Launched == before, "No sparkles while the worker is on a toilet trip");
        for (var i = 0; i < 600 && !_homeOffice.AtDeskWorking(worker); i++) { _homeOffice._Process(.1); await SettleUi(); }
        _homeOffice.SendToBreakRoom(worker); for (var i = 0; i < 4; i++) { _homeOffice._Process(.1); await SettleUi(); }
        before = _sparkleLayer.Launched; FeedbackFor(1);
        Check(!_homeOffice.AtDeskWorking(worker) && _sparkleLayer.Launched == before, "No sparkles while the worker is on a break-room visit");
        for (var i = 0; i < 600 && !_homeOffice.AtDeskWorking(worker); i++) { _homeOffice._Process(.1); await SettleUi(); }
        Check(_homeOffice.AtDeskWorking(worker), "The worker returns to the desk");
        // Measure a working stretch at 8x: the share of work time seated, and the sparkles against the always-on rate.
        double work = 0, seated = 0; before = _sparkleLayer.Launched; const double frame = 1d / 30;
        for (var t = 0.0; t < 45 && _currentStageBar.Chapter is { } c && WorkingOn(c).ContainsKey(worker); t += frame)
        {
            _Process(frame); _homeOffice._Process(frame);
            work += frame; if (_homeOffice.AtDeskWorking(worker)) seated += frame;
        }
        var share = work > 0 ? seated / work : 0; var expected = work * WorkFeedbackPlan.SparklesPerSecondAt8x;
        var launched = _sparkleLayer.Launched - before;
        GD.Print($"DESK SHARE: {share:P0} of {work:0.0} s at 8x; sparkles {launched} vs always-on {expected:0}");
        Check(work >= 20 && share is > .55 and < .9, $"The worker sits at the desk for most of the working time ({share:P0} of {work:0.0} s)");
        Check(Math.Abs(launched - expected) <= expected * .25, $"Seated-only sparkles keep about the day's always-on count ({launched} vs {expected:0})");
        _speed = 0;
        // The working day ends with the evening recap and pop-ups; close them so later checks start from the office.
        foreach (var window in GetChildren().OfType<Window>().Where(w => w.Visible)) window.Hide();
        _popupEvents.Clear(); _helperPopup.Hide(); _pendingRecap = null; await SettleUi();
    }

    private async Task RightClick(float drag = 0)
    {
        var at = new Vector2(900, 500);
        _Input(new InputEventMouseButton { ButtonIndex = MouseButton.Right, Pressed = true, Position = at });
        _Input(new InputEventMouseButton { ButtonIndex = MouseButton.Right, Pressed = false, Position = at + new Vector2(drag, 0) });
        await SettleUi();
    }

    private async Task CheckRightClickBack()
    {
        ShowOffice(); Navigate("Inbox"); await SettleUi(); Navigate("Series"); await SettleUi();
        await RightClick(40);
        Check(_page == "Series" && _side.Visible, "A right drag does not go back (it rotates the camera)");
        await RightClick();
        Check(_page == "Inbox" && _side.Visible, $"A right click steps back one page ({_page})");
        await RightClick();
        Check(_page == "Office" && !_side.Visible, $"Stepping back ends on the 3D office ({_page})");
        await RightClick();
        Check(_page == "Office" && !_menu.Visible && !_inMenu, "A right click on the office does nothing");

        ShowPauseMenu(); SettingsMenu(); await SettleUi();
        await RightClick();
        Check(_menu.Visible && _menuCompact, "From a menu sub-page, a right click returns to the pause menu");
        await RightClick();
        Check(!_menu.Visible && !_inMenu, "A right click closes the pause menu");

        _helperPopup.Show(); await RightClick();
        Check(!_helperPopup.Visible, "A right click closes Helper-Chan's pop-up");

        OpenSaveMenu(); await SettleUi();
        var box = _menuContent.FindChildren("*", "LineEdit", true, false).OfType<LineEdit>().First(l => l.IsVisibleInTree());
        GetViewport().PushInput(new InputEventMouseMotion { Position = box.GetGlobalRect().GetCenter() }); await SettleUi();
        Check(TextBoxUnderMouse(), "The pointer over a text box is recognised");
        _Input(new InputEventMouseButton { ButtonIndex = MouseButton.Right, Pressed = true, Position = box.GetGlobalRect().GetCenter() });
        _Input(new InputEventMouseButton { ButtonIndex = MouseButton.Right, Pressed = false, Position = box.GetGlobalRect().GetCenter() });
        await SettleUi();
        Check(_menu.Visible && !_menuCompact, "A right click on a text box keeps its copy and paste menu instead of going back");
        _menu.Hide(); _inMenu = false; GetViewport().PushInput(new InputEventMouseMotion { Position = new(900, 500) }); await SettleUi();

        OpenWorkspace("Furniture"); BeginOfficeEditor(); await SettleUi();
        await RightClick();
        Check(OfficeEditing && _notice.Text.Contains("furniture"), "While furnishing, a right click explains instead of leaving");
        EndOfficeEditor(); ShowOffice(); await SettleUi();
    }
}
