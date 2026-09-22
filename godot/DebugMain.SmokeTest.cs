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
        .OfType<Button>().First(button => button.Text == text && !button.IsQueuedForDeletion());

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
        await SettleUi();
        await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
        var output = ProjectSettings.GlobalizePath($"res://../TestResults/{name}.png");
        var error = GetViewport().GetTexture().GetImage().SavePng(output);
        Check(error == Error.Ok, $"Screenshot failed: {error}");
    }

    /// <summary>Drives the scene's own advance loop, scanning events as the real driver does, until the condition holds.</summary>
    private void RunUntil(Func<bool> condition, int maxHours, string what)
    {
        for (var i = 0; i < maxHours; i++)
        {
            if (condition()) return;
            AdvanceAndScan(1);
        }
        Check(condition(), $"Timed out waiting for {what}");
    }

    /// <summary>A seed whose first Flowers pitch, made after the first doujin chapter, ends in an offer.</summary>
    private static int FindSeedThatSucceeds()
    {
        for (var seed = 0; seed < 500; seed++)
        {
            var state = GameState.NewGame(seed);
            state.Apply(new CreateSeriesCommand("Petals", "romance", Cadence.Monthly, 19));
            for (var i = 0; i < 24 * 30 && state.Series[0].Chapters[0].Status != ChapterStatus.Complete; i++) state.Advance(1);
            state.Apply(new PitchSeriesCommand(state.Series[0].Id, "hoshigaku-flowers"));
            for (var i = 0; i < 24 * 90 && state.Series[0].Publishing == PublishingStatus.Pitching; i++) state.Advance(1);
            if (state.Series[0].Publishing == PublishingStatus.Offered) return seed;
        }
        throw new InvalidOperationException("no seed produced an offer");
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
            Check(_state.Markets.Count == 6 && _magazineOption.ItemCount == 6, "Markets and magazine dropdown");
            var beforeInvalid = _state.ToJson();
            Press("Create");
            Check(_state.ToJson() == beforeInvalid && _log.GetParsedText().Contains("Command rejected"), "Blank title validation");

            _titleEdit.Text = "Rush";
            _genreEdit.Text = "action";
            Press("Create");
            await SettleUi();
            var series = _state.Series.Single();
            var chapter = series.Chapters.Single();
            Check(chapter.DueDate == GameClock.Start.AddDays(7) && chapter.Stages.Count == 5, "Create series and chapter");
            Check(_state.People[0].Queue.Count == 5 && chapter.IsAtRisk, "Queue and initial risk");
            Press("1x");
            _Process(1.25);
            Check(_state.Clock.Now == GameClock.Start, "Fractional time stays in driver");
            _Process(1.25);
            Check(_state.Clock.Now == GameClock.Start.AddHours(1), "1x advances one hour in 2.5 seconds");
            Check(Math.Abs(chapter.StageWork(Stage.Name).HoursDone - 1.6) < 0.0001, "Work bar progress");

            AdvanceAndScan(200);
            Check(_state.Clock.Now == GameClock.Start.AddHours(12), "Large advance stops exactly at first recap");
            Check(_speed == 0 && _recapDialog.Visible && _state.People[0].OvertimeHoursToday == 2, "Overtime recap pauses");
            await CaptureSmokeImage("debug-recap");
            _recapDialog.GetOkButton().EmitSignal(BaseButton.SignalName.Pressed);
            Check(_state.Clock.Now == new DateTime(1996, 4, 2, 8, 0, 0) && _speed == 1, "Continue skips night and restores speed");
            Pause();
            await SettleUi();

            // Pin a blocked later stage: queue priority must not bypass prerequisites.
            var tonesRow = _queueBox.GetChildren().OfType<HBoxContainer>().Last();
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
            Check(chapter.Quality == 0, "A fully skipped chapter scores zero");
            Check(series.Chapters[1].DueDate == chapter.DueDate.AddMonths(1) &&
                series.Chapters[1].StageWork(Stage.Name).HoursRequired == 36, "Next chapter takes new cadence and pages");

            Press("Save");
            var saved = _state.ToJson();
            AdvanceAndScan(3);
            Press("Load");
            Check(_state.ToJson() == saved && _speed == 0, "Save/load restores exact state and pauses");
            Check(_startSpin.Value == 9 && !_overtimeBox.ButtonPressed, "Load refreshes controls");
            using (var invalid = FileAccess.Open(_savePath, FileAccess.ModeFlags.Write)) invalid.StoreString("{\"Version\":2}");
            Press("Load");
            Check(_state.ToJson() == saved && _log.GetParsedText().Contains("Load failed"), "Invalid load keeps current game");

            _state = GameState.NewGame();
            _scanIndex = 0;
            _log.Clear();
            ResetPersonInputs();
            TryApply(new CreateSeriesCommand("Rush", "action", Cadence.Weekly, 19));
            SetSpeed(8);
            _Process(1000); // Simulated slow frame: it must not skip through multiple days.
            Check(_state.Clock.Hour == 20 && _speed == 0, "Slow frame respects recap boundary");
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

            GD.Print($"GODOT SMOKE PASS: {_smokeChecks} checks");
            GetTree().Quit(0);
        }
        catch (Exception ex)
        {
            GD.PrintErr($"GODOT SMOKE FAIL: {ex}");
            GetTree().Quit(1);
        }
    }

    /// <summary>Sub-project 2: doujin chapter, pitch to Flowers, accept, publish, sell, save and reload.</summary>
    private async Task RunPublishingSmoke()
    {
        var seed = FindSeedThatSucceeds();
        _state = GameState.NewGame(seed);
        _scanIndex = 0;
        _log.Clear();
        _recapDialog.Hide();
        ResetPersonInputs();
        _titleEdit.Text = "Petals";
        _genreEdit.Text = "romance";
        _cadenceOption.Selected = (int)Cadence.Monthly;
        _pagesSpin.Value = 19;
        Press("Create");
        await SettleUi();
        var series = _state.Series.Single();

        RunUntil(() => series.Chapters[0].Status == ChapterStatus.Complete, 24 * 30, "the first doujin chapter");
        Check(series.Chapters[0].Quality == 84 && series.Publishing == PublishingStatus.Unpublished, "Doujin chapter finished with quality 84");
        Check(series.OpenChapter is { IsUntouched: true }, "The next chapter is untouched and ready to be replaced by a one-shot");

        _magazineOption.Selected = _state.Publishers.Magazines.ToList().FindIndex(m => m.Id == "hoshigaku-flowers");
        await SettleUi();
        Check(_rankingTitle.Text.StartsWith("Monthly Hoshigaku Flowers"), "Market column follows the magazine dropdown");
        Press("Pitch");
        await SettleUi();
        Check(series.Publishing == PublishingStatus.Pitching && series.OpenChapter is { IsOneShot: true, Pages: 31 }, "Pitch creates a 31-page one-shot");
        Check(_seriesStatusLabel.Text.Contains("Pitching"), "Status block shows the publishing state");

        RunUntil(() => series.Publishing != PublishingStatus.Pitching, 24 * 120, "the pitch to resolve");
        Check(series.Publishing == PublishingStatus.Offered && _speed == 0, "Offer arrives and auto-pauses");
        Check(_state.Events.Any(e => e.Type == EventType.EditorApproved), "The one-shot went through the editor");
        Press("Accept");
        await SettleUi();
        Check(series.Publishing == PublishingStatus.Serialized && series.Contract?.MagazineId == "hoshigaku-flowers", "Accept signs the contract");
        Check(_seriesStatusLabel.Text.Contains("Monthly Hoshigaku Flowers"), "Status block shows the contract");

        RunUntil(() => series.ChaptersPublished >= 1, 24 * 200, "the first published chapter");
        Check(_state.Ledger.Any(l => l.Reason == "chapter fee"), "Chapter fee in the ledger");
        Check(series.LastRank is not null && _state.MarketOf("hoshigaku-flowers").LastRanking.Any(r => r.SeriesId == series.Id), "Ranked in Flowers");
        await SettleUi();
        Check(_rankingText.GetParsedText().Contains(">>") && _rankingText.GetParsedText().Contains("Petals"), "Ranking marks the player's row");

        RunUntil(() => _state.Ledger.Any(l => l.Reason == "royalties"), 24 * 400, "the first royalty payment");
        Check(series.Volumes.Any(v => !v.IsDoujin && v.CopiesSold > 0), "A tankobon sold copies");
        Check(series.Volumes.All(v => !v.IsDoujin), "Serialized before five doujin chapters: no doujin volume");
        Check(_state.Money > 500_000, "The studio is in profit");
        await SettleUi();
        Check(_ledgerText.GetParsedText().Contains("royalties") && _volumesText.GetParsedText().Contains("Petals vol."), "Ledger and volumes shown");

        Press("Get Online");
        await SettleUi();
        Check(_state.HasInternet && _state.Ledger.Last().Reason == "internet", "Get Online debits the ledger");
        Check(_studioLabel.Text.StartsWith("Studio:") && _trendsLabel.Text.Contains("romance"), "Studio and trend status lines");

        Press("Save");
        var saved = _state.ToJson();
        AdvanceAndScan(24 * 3);
        Press("Load");
        Check(_state.ToJson() == saved, "Publishing state survives save and load");
        Check(_state.Series[0].Publishing == PublishingStatus.Serialized, "Loaded series is still serialized");
        await SettleUi();
        await CaptureSmokeImage("debug-market");
    }
}
