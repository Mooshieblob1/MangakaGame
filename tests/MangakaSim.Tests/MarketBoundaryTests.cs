using MangakaSim.Rules;
using Xunit;

namespace MangakaSim.Tests;

public class MarketBoundaryTests
{
    [Theory]
    [InlineData(0)] [InlineData(1)]
    public void Pitch_resolution_uses_actual_close_snapshot_even_after_calendar_moves(int lateHours)
    {
        var state = PublishingTests.Started();
        var series = state.Series[0];
        state.Apply(new PitchSeriesCommand(series.Id, "hoshigaku-flowers"));
        var sample = series.Chapters.Last();
        foreach (var stage in StageOrder.All) state.Apply(new SkipStageCommand(sample.Id, stage));
        sample.RedoCount = 2;
        var due = sample.DueDate;
        sample.EditorDecisionAt = due.AddHours(lateHours);
        state.Advance(state.Clock.HoursUntil(due));
        if (lateHours > 0)
        {
            Assert.False(sample.PitchResolved);
            state.Advance(1);
            Assert.Equal(ChapterStatus.Complete, sample.Status);
            Assert.False(sample.PitchResolved);
            Assert.Equal(state.ToJson(), GameState.FromJson(state.ToJson()).ToJson());
            state.Advance(state.Clock.HoursUntil(due.AddDays(28)));
        }
        Assert.True(sample.PitchResolved);
        Assert.True(state.Markets[5].NextIssueClose > state.Clock.Now);
        Assert.DoesNotContain(state.Events, e => e.Type == EventType.DeadlineMissed && e.ChapterNumber == sample.Number);
        Assert.Empty(series.Strikes);
    }
    [Theory]
    [InlineData("tokiwa-jump", 48)] [InlineData("tokiwa-square", 36)] [InlineData("hoshigaku-flowers", 24)]
    public void Editor_gate_resolves_at_exact_delay_and_third_submission_uses_no_rng(string magazine, int delay)
    {
        var state = PublishingTests.Started();
        state.Apply(new PitchSeriesCommand(state.Series[0].Id, magazine));
        var chapter = state.Series[0].Chapters.Last();
        chapter.RedoCount = 2;
        foreach (var stage in StageOrder.All) state.Apply(new SkipStageCommand(chapter.Id, stage));
        Assert.Equal(state.Clock.Now.AddHours(delay), chapter.EditorDecisionAt);
        state.Advance(delay - 1);
        Assert.Equal(EditorStatus.AwaitingReview, chapter.Editor);
        // Isolate the editor step from unrelated magazine draws in this hour.
        state.Clock.Advance();
        var rng = state.Rng.State;
        state.EditorStep();
        Assert.Equal(rng, state.Rng.State);
        Assert.Equal(EditorStatus.Approved, chapter.Editor);
        Assert.Equal(ChapterStatus.Complete, chapter.Status);
        Assert.Equal(0, chapter.Quality);
        Assert.Null(chapter.EditorDecisionAt);
    }

    [Fact]
    public void Redo_preserves_actual_contributor_hours_and_resets_attempt_quality()
    {
        var state = PublishingTests.Started();
        state.Apply(new PitchSeriesCommand(state.Series[0].Id, "tokiwa-jump"));
        var chapter = state.Series[0].Chapters.Last();
        PublishingTests.Until(state, () => chapter.Editor == EditorStatus.AwaitingReview);
        var name = chapter.StageWork(Stage.Name);
        var hours = name.HoursByPerson.Values.Sum();
        // A known high sample rejects the first submission at this reputation.
        state.Rng = Rng.FromSeed(3);
        state.Clock.Now = chapter.EditorDecisionAt!.Value;
        state.EditorStep();
        Assert.Equal(EditorStatus.RedoRequested, chapter.Editor);
        Assert.Equal(1, chapter.RedoCount);
        Assert.Equal(hours, name.HoursByPerson.Values.Sum());
        Assert.Equal(0, name.HoursDone);
        Assert.Equal(0, name.Contribution);
        Assert.Equal(0, name.OvertimeHours);
        Assert.Equal(9.5, state.People[0].Reputation);
    }

    [Theory]
    [InlineData(0, false)] [InlineData(1, true)]
    public void Combined_cancellation_triggers_draw_once_and_survival_keeps_warning_age(int seed, bool cancelled)
    {
        var state = SimulationFixture.EightPublished();
        var series = state.Series[0];
        var magazine = state.PublisherCatalog.Get(series.Contract!.MagazineId);
        state.StudioTrackRecord = state.People[0].Reputation = 100;
        series.LastRank = null;
        series.WeeksBelowLine = 7;
        var clocks = CancellationRules.Clocks(state.Protection(series));
        series.WarningIssuedAt = state.Clock.Now.AddDays(-clocks.Cancel * IssueSchedule.Days(magazine.Cadence));
        var warning = series.WarningIssuedAt;
        series.Strikes = new() { state.Clock.Now.AddDays(-2), state.Clock.Now.AddDays(-1), state.Clock.Now };
        state.Rng = Rng.FromSeed(seed);
        var expected = Rng.FromSeed(seed);
        expected.NextDouble();
        state.CancellationStep(series, magazine, false);
        Assert.Equal(expected.State, state.Rng.State);
        Assert.Equal(cancelled, series.Status == SeriesStatus.Ended);
        if (!cancelled)
        {
            Assert.Equal(3, series.WeeksBelowLine);
            Assert.Single(series.Strikes);
            Assert.Equal(warning, series.WarningIssuedAt);
            Assert.Contains(state.Events, e => e.Type == EventType.CancellationSurvived);
        }
    }

    [Fact]
    public void Safe_rank_lifts_warning_before_any_warning_roll()
    {
        var state = SimulationFixture.EightPublished();
        var series = state.Series[0];
        var magazine = state.PublisherCatalog.Get(series.Contract!.MagazineId);
        series.WarningIssuedAt = GameClock.Start;
        series.WeeksBelowLine = 20;
        series.LastRank = magazine.CancellationRank;
        var rng = state.Rng.State;
        state.CancellationStep(series, magazine, false);
        Assert.Null(series.WarningIssuedAt);
        Assert.Equal(0, series.WeeksBelowLine);
        Assert.Equal(rng, state.Rng.State);
        Assert.Contains(state.Events, e => e.Type == EventType.CancellationWarningLifted);
    }

    [Fact]
    public void Grace_misses_shift_due_slots_without_strikes_and_preserve_saveability()
    {
        var state = SimulationFixture.Serialized();
        var series = state.Series[0];
        state.Apply(new PauseSeriesCommand(series.Id));
        var first = series.Contract!.FirstIssueClose;
        state.Advance(state.Clock.HoursUntil(first.AddHours(-1)));
        Assert.DoesNotContain(state.Events, e => e.Type == EventType.IssueMissed);
        state.Advance(1);
        Assert.Empty(series.Strikes);
        Assert.Equal(first.AddDays(28), series.Chapters.Last().DueDate);
        Assert.DoesNotContain(state.Markets[5].LastRanking, r => r.SeriesId == series.Id);
        Assert.Equal(state.ToJson(), GameState.FromJson(state.ToJson()).ToJson());
    }

    [Fact]
    public void Fillers_retire_after_twelve_lows_and_last_table_references_the_archive()
    {
        var state = GameState.NewGame(3);
        var market = state.Markets[0];
        var filler = market.Fillers.First(f => !f.IsIconic);
        filler.Popularity = 5;
        filler.IssuesBelowLine = 11;
        state.Advance(state.Clock.HoursUntil(market.NextIssueClose));
        Assert.DoesNotContain(filler, market.Fillers);
        Assert.Contains(filler, market.RetiredFillers);
        Assert.Contains(market.LastRanking, r => r.FillerId == filler.Id);
        Assert.Equal(19, market.Fillers.Count);
        Assert.Equal(state.ToJson(), GameState.FromJson(state.ToJson()).ToJson());
    }

    [Fact]
    public void Monthly_update_runs_once_and_retained_boom_survives_load_and_later_booms()
    {
        var state = GameState.NewGame(6);
        var trend = state.Trends.Single(t => t.Genre == "fantasy");
        trend.PermanentBoom = .15;
        trend.BoomPeak = .4;
        trend.Boom = .55;
        trend.BoomEndsAt = GameClock.Start.AddYears(1);
        trend.BoomFadeEndsAt = trend.BoomEndsAt.Value.AddYears(1);
        state.Advance(24 * 370);
        Assert.True(trend.BoomFloorChosen);
        var loaded = GameState.FromJson(state.ToJson());
        var rng = state.Rng.State;
        state.MonthlyTrends();
        Assert.Equal(rng, state.Rng.State);
        state.Advance(24 * 370);
        loaded.Advance(24 * 370);
        Assert.Equal(state.ToJson(), loaded.ToJson());
        Assert.True(trend.PermanentBoom >= .15);
        Assert.True(trend.Boom >= trend.PermanentBoom);
        Assert.Equal(0, state.Trends.Single(t => t.Genre == "other").Noise);
    }

    [Fact]
    public void Midnight_sales_belong_to_mondays_recap_while_work_belongs_to_sunday()
    {
        var state = PublishingTests.Started();
        var series = state.Series[0];
        PublishingTests.Until(state, () => series.Volumes.Count == 1);
        state.Apply(new StudioActionCommand(StudioAction.Print,series.Volumes[0].Id,Amount:100));
        state.Apply(new SetPagesPerChapterCommand(series.Id, 200));
        var shortChapter = series.Chapters.Last();
        PublishingTests.Until(state, () => shortChapter.Status == ChapterStatus.Complete);
        var person = state.People[0];
        state.Apply(new SetScheduleCommand(person.Id, 20, 22, Enum.GetValues<DayOfWeek>().Where(d => d != DayOfWeek.Sunday).ToHashSet()));
        var sunday = state.Clock.Now.Date.AddDays((7 - (int)state.Clock.DayOfWeek) % 7).AddHours(20);
        if (sunday < state.Clock.Now) sunday = sunday.AddDays(7);
        state.Advance(state.Clock.HoursUntil(sunday));
        series.Chapters.Last().DueDate = sunday;
        state.Apply(new SetOvertimeAllowedCommand(person.Id, true));
        state.Advance(4);
        Assert.Equal(DayOfWeek.Monday, state.Clock.DayOfWeek);
        var recap = state.Events.Last(e => e.Type == EventType.DailyRecap);
        Assert.Equal(sunday.Date, recap.ActivityDate);
        Assert.Equal(0, recap.Recap!.YenEarned);
        var earned = state.Ledger.Where(e => e.Time == state.Clock.Now && e.Amount > 0).Sum(e => e.Amount);
        Assert.True(earned > 0);
        state = GameState.FromJson(state.ToJson());
        state.Apply(new SetScheduleCommand(person.Id, 8, 18, new() { DayOfWeek.Sunday }));
        state.Advance(24);
        Assert.Equal(earned, state.Events.Last(e => e.Type == EventType.DailyRecap).Recap!.YenEarned);
    }
}
