using System;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using MangakaSim;

namespace MangakaGame;

// Opt-in integration checks against the actual scene and its controls.
// Run with: Godot --headless --path godot -- --smoke-test
public partial class DebugMain
{
    private int _smokeChecks;

    private void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
        _smokeChecks++;
    }

    private Button ButtonNamed(string text) => FindChildren("*", "Button", true, false)
        .OfType<Button>().Where(button => button.Text == text && !button.IsQueuedForDeletion()).OrderByDescending(button=>button.IsVisibleInTree()).FirstOrDefault()
        ?? (_navigation.TryGetValue(text,out var navigation)?navigation:throw new InvalidOperationException("No action named "+text));

    private void Press(string text) => ButtonNamed(text).EmitSignal(BaseButton.SignalName.Pressed);

    private async Task SettleUi()
    {
        if (_dirty) Refresh();
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
    }

    private async Task CaptureSmokeImage(string name)
    {
        if (!OS.GetCmdlineUserArgs().Contains("--capture")) return;
        const string filterPrefix="--capture-only=";
        var filter=OS.GetCmdlineUserArgs().FirstOrDefault(arg=>arg.StartsWith(filterPrefix));
        if(filter is not null&&name!=filter[filterPrefix.Length..]){await SettleUi();return;}
        await SettleUi();
        await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
        var output = System.IO.Path.Combine(SmokeOutput,name+".avif");
        using var captured = GetViewport().GetTexture().GetImage();
        ScreenshotCapture.SaveAvif(captured,output);
        Check(System.IO.File.Exists(output), $"Screenshot missing: {output}");
    }

    private async void RunSmokeTest()
    {
        SetProcess(false);
        _savePath = "res://../TestResults/godot-smoke-save.json";
        try
        {
            System.IO.Directory.CreateDirectory(ProjectSettings.GlobalizePath("res://../TestResults"));
            Check(_state.Clock.Now == GameClock.Start && _speed == 0, "Initial clock and pause");
            Check(_state.Series.Count == 0 && _state.People[0].CurrentTask is null, "Initial empty studio");
            var beforeInvalid = _state.ToJson();
            Press("Create");
            Check(_state.ToJson() == beforeInvalid && _log.GetParsedText().Contains("Command rejected"), "Blank title validation");

            _titleEdit.Text = "Rush";
            SelectGenre(_genreOption,"action");
            Press("Create");
            await SettleUi();
            var series = _state.Series.Single();
            var chapter = series.Chapters.Single();
            Check(chapter.DueDate == GameClock.Start.AddDays(7) && chapter.Stages.Count == 5, "Create series and chapter");
            Check(_state.People[0].Queue.Count == 5 && !chapter.IsAtRisk, "Self-published queue has no publisher deadline risk");
            Press("1x");
            _Process(1.25);
            Check(_state.Clock.Now == GameClock.Start, "Fractional time stays in driver");
            _Process(1.25);
            Check(_state.Clock.Now == GameClock.Start.AddHours(1), "1x advances one hour in 2.5 seconds");
            Check(Math.Abs(chapter.StageWork(Stage.Name).HoursDone - 1.9) < 0.0001, "Work bar progress");

            AdvanceAndScan(200);
            Check(_state.Clock.Now == GameClock.Start.AddHours(10), "Large advance stops exactly at first recap");
            Check(_speed == 0 && _recapDialog.Visible && _state.People[0].OvertimeHoursToday == 0, "Self-publishing recap pauses without deadline overtime");
            await CaptureSmokeImage("debug-recap");
            _recapDialog.GetOkButton().EmitSignal(BaseButton.SignalName.Pressed);
            Check(_state.Clock.Now == new DateTime(1996, 4, 2, 8, 0, 0) && _speed == 1, "Continue skips night and restores speed");
            Pause();
            await SettleUi();

            // Pin a blocked later stage: queue priority must not bypass prerequisites.
            var tonesRow = _queueBox.GetChildren().OfType<Container>().Last();
            tonesRow.GetChildren().OfType<Button>().First(b => b.Text == "Pin").EmitSignal(BaseButton.SignalName.Pressed);
            await SettleUi();
            Check(_state.People[0].Queue[0].Stage == Stage.Tones && _state.People[0].CurrentTask?.Stage == Stage.Name, "Pin respects prerequisites");
            Press("Unpin");
            await SettleUi();
            Check(_state.People[0].Pins.Count == 0, "Unpin");
            Press("Down");
            await SettleUi();
            Check(_state.People[0].ManualOrder is not null && _state.People[0].Queue[0].Stage == Stage.Pencils, "Queue reorder");
            Press("Skip");
            await SettleUi();
            Check(chapter.StageWork(Stage.Pencils).Status == StageStatus.Skipped, "Queue skip");

            Press("Pause"); // Speed pause is first; the series button is selected explicitly below.
            var seriesPause = FindChildren("*", "Button", true, false).OfType<Button>().Last(b => b.Text == "Pause");
            seriesPause.EmitSignal(BaseButton.SignalName.Pressed);
            Check(series.Status == SeriesStatus.Paused && _state.People[0].CurrentTask is null, "Pause series");
            Press("Resume");
            Check(series.Status == SeriesStatus.Active && _state.People[0].CurrentTask is not null, "Resume series");

            _startSpin.Value = 9;
            _endSpin.Value = 17;
            _dayOffBoxes[DayOfWeek.Saturday].ButtonPressed = true;
            Press("Apply schedule");
            _overtimeBox.ButtonPressed = false;
            Check(_state.People[0].Schedule.WorkStartHour == 9 && _state.People[0].Schedule.DaysOff.Contains(DayOfWeek.Saturday), "Schedule controls");
            Check(!_state.People[0].OvertimeAllowed, "Overtime toggle");

            _setCadenceOption.Selected = (int)Cadence.Monthly;
            Press("Set cadence");
            _setPagesSpin.Value = 30;
            Press("Set pages");
            Check(series.Cadence == Cadence.Monthly && series.PagesPerChapter == 30, "Series controls");
            Check(chapter.DueDate == GameClock.Start.AddDays(7), "Existing chapter keeps its due date");
            SetSpeed(8);
            foreach (var work in chapter.Stages.Where(w => !w.IsDone).ToArray())
                TryApply(new SkipStageCommand(chapter.Id, work.Stage));
            Check(chapter.Status == ChapterStatus.Complete && _speed == 0, "Command completion auto-pauses");
            Check(series.Chapters[1].DueDate == chapter.DueDate.AddMonths(1) &&
                series.Chapters[1].StageWork(Stage.Name).HoursRequired == 36, "Next chapter takes new cadence and pages");

            Press("Save");
            var saved = _state.ToJson();
            AdvanceAndScan(3);
            Press("Load");
            Check(_state.ToJson() == saved && _speed == 0, "Save/load restores exact state and pauses");
            Check(_startSpin.Value == 9 && !_overtimeBox.ButtonPressed, "Load refreshes controls");
            using (var invalid = FileAccess.Open(_savePath, FileAccess.ModeFlags.Write)) invalid.StoreString("{\"Version\":1}");
            Press("Load");
            Check(_state.ToJson() == saved && _log.GetParsedText().Contains("Load failed"), "Invalid load keeps current game");

            _state = GameState.NewGame();
            _scanIndex = 0;
            _log.Clear();
            ResetPersonInputs();
            TryApply(new CreateSeriesCommand("Rush", "action", Cadence.Weekly, 19));
            SetSpeed(8);
            _Process(1000); // Simulated slow frame: it must not skip through multiple days.
            Check(_state.Clock.Hour == 18 && _speed == 0, "Slow frame respects recap boundary");
            Save();
            var recapSave = _state.ToJson();
            OnRecapContinue();
            Load();
            Check(_state.ToJson() == recapSave && _recapDialog.Visible, "Loading a recap restores its Continue action");

            _state.Settings.AutoPause[EventType.DayStarted] = true;
            OnRecapContinue();
            Check(_state.Clock.Hour == 0 && _speed == 0, "Idle skip preserves other auto-pause events");
            _state.Settings.AutoPause[EventType.DayStarted] = false;
            AdvanceAndScan(_state.HoursUntilNextWork());
            Pause();
            await SettleUi();
            await CaptureSmokeImage("debug-main");

            await RunPublishingSmoke();
            await RunStaffSmoke();
            await RunOperationsSmoke();
            await RunOfficeSmoke();
            await RunIndustrySmoke();

            GD.Print($"GODOT SMOKE PASS: {_smokeChecks} checks");
            GetTree().Quit(0);
        }
        catch (Exception ex)
        {
            GD.PrintErr($"GODOT SMOKE FAIL: {ex}");
            GetTree().Quit(1);
        }
    }
}
