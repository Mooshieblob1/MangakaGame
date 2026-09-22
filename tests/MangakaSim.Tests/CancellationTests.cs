using MangakaSim;
using Xunit;
using static MangakaSim.Tests.IssueCloseTests;

namespace MangakaSim.Tests;

public class CancellationTests
{
    private const string Jump = "tokiwa-jump";

    /// <summary>Gives the chapter the next close will judge the given quality, then runs to that close.</summary>
    private static void CloseWith(GameState state, int quality, double fanbase)
    {
        var series = state.Series[0];
        series.Fanbase = fanbase;
        var judged = state.ContractChapters(series).Where(c => !c.IsPublished).MinBy(c => c.Number)!;
        if (judged.Status == ChapterStatus.Complete)
        {
            // Aki finished it herself under the serialized pipeline; rewrite its score.
            foreach (var work in judged.Stages) work.Contribution = QualityRules.Weight(work.Stage) * quality;
            judged.Quality = quality;
            judged.Editor = EditorStatus.Approved;
        }
        else ForceComplete(state, judged, quality);
        state.Advance(state.Clock.HoursUntil(state.MarketOf(Jump).NextIssueClose));
    }

    [Fact]
    public void Issues_below_the_line_earn_a_warning_and_climbing_back_lifts_it()
    {
        var state = SerializedAtJump();
        var series = state.Series[0];
        series.Contract!.ChaptersPublished = 8; // grace ends with this issue's publish

        for (var i = 1; i <= 3; i++)
        {
            CloseWith(state, quality: 1, fanbase: 0); // bottom of the table
            Assert.True(series.LastRank > 15, $"rank {series.LastRank}");
            Assert.Equal(i, series.WeeksBelowLine);
        }
        Assert.Equal(3, CancellationRules.WarningClock(state.ProtectionOf(series)));
        var warning = Assert.Single(state.Events, e => e.Type == EventType.CancellationWarning);
        Assert.Equal(state.Clock.Now, series.WarningIssuedAt);
        Assert.Equal(Jump, warning.MagazineId);

        CloseWith(state, quality: 1, fanbase: 0);
        Assert.Equal(4, series.WeeksBelowLine);
        Assert.Single(state.Events, e => e.Type == EventType.CancellationWarning); // not repeated

        CloseWith(state, quality: 100, fanbase: 5_000_000); // back on top
        Assert.Equal(1, series.LastRank);
        Assert.Equal(0, series.WeeksBelowLine);
        Assert.Null(series.WarningIssuedAt);
        Assert.Single(state.Events, e => e.Type == EventType.CancellationWarningLifted);
        Assert.Equal(PublishingStatus.Serialized, series.Publishing);
    }

    [Fact]
    public void Below_line_issues_during_the_grace_period_do_not_count()
    {
        var state = SerializedAtJump();
        var series = state.Series[0];
        for (var i = 0; i < 4; i++) CloseWith(state, quality: 1, fanbase: 0);
        Assert.Equal(0, series.WeeksBelowLine);
        Assert.DoesNotContain(state.Events, e => e.Type == EventType.CancellationWarning);
    }

    [Fact]
    public void Old_strikes_expire()
    {
        var state = SerializedAtJump();
        var series = state.Series[0];
        series.Contract!.ChaptersPublished = 9;
        series.Strikes.Add(state.Clock.Now.AddDays(-7 * 9)); // older than the 8-issue lifetime at P ~ 0
        series.Strikes.Add(state.Clock.Now.AddDays(-7 * 2));
        CloseWith(state, quality: 84, fanbase: 1000);
        var strike = Assert.Single(series.Strikes);
        Assert.Equal(GameClock.Start.AddDays(-14), strike);
    }

    /// <summary>Ten chapters in, Aki works a day, then the series goes on hiatus and misses three issues.</summary>
    private static GameState ThreeMisses(int seed)
    {
        var state = SerializedAtJump(seed);
        var series = state.Series[0];
        series.Contract!.ChaptersPublished = 10;
        state.StudioTrackRecord = 20;
        state.Advance(5);
        state.Apply(new PauseSeriesCommand(series.Id));
        for (var i = 0; i < 3; i++) state.Advance(state.Clock.HoursUntil(state.MarketOf(Jump).NextIssueClose));
        return state;
    }

    [Fact]
    public void Three_strikes_trigger_exactly_one_roll_that_can_be_survived()
    {
        var seed = EditorTests.FindSeed(ThreeMisses, s => s.Events.Any(e => e.Type == EventType.CancellationSurvived), 2000);
        var state = ThreeMisses(seed);
        var series = state.Series[0];
        Assert.Equal(3, state.Events.Count(e => e.Type == EventType.IssueMissed));
        Assert.Single(state.Events, e => e.Type == EventType.CancellationSurvived);
        Assert.DoesNotContain(state.Events, e => e.Type == EventType.SeriesCancelled);
        Assert.Equal(PublishingStatus.Serialized, series.Publishing);
        Assert.Single(series.Strikes); // halved, oldest dropped
        Assert.Equal(state.Clock.Now, series.Strikes[0]);
        Assert.Equal(17, state.StudioTrackRecord); // three misses only

        // Another miss brings the count to two, not three: no second roll yet.
        state.Advance(state.Clock.HoursUntil(state.MarketOf(Jump).NextIssueClose));
        Assert.Equal(2, series.Strikes.Count);
        Assert.Single(state.Events, e => e.Type == EventType.CancellationSurvived);
    }

    [Fact]
    public void Three_strikes_can_cancel_the_series()
    {
        var seed = EditorTests.FindSeed(ThreeMisses, s => s.Events.Any(e => e.Type == EventType.SeriesCancelled));
        var state = ThreeMisses(seed);
        var series = state.Series[0];
        var cancelled = Assert.Single(state.Events, e => e.Type == EventType.SeriesCancelled);
        Assert.Equal(Jump, cancelled.MagazineId);
        Assert.Equal(SeriesStatus.Ended, series.Status);
        Assert.Equal(PublishingStatus.Unpublished, series.Publishing);
        Assert.Null(series.Contract);
        Assert.DoesNotContain(series.Chapters, ch => ch.Status != ChapterStatus.Complete); // open chapter dropped
        Assert.Equal(state.Clock.Now.AddDays(7 * 52), series.PitchCooldowns[Jump]);
        Assert.Equal(10 - 2, state.People[0].Reputation, 6);      // Aki worked on it
        Assert.Equal(20 - 3 - 8, state.StudioTrackRecord, 6);     // three misses, then the cancellation
        Assert.Empty(series.Strikes);
        Assert.Null(state.People[0].CurrentTask);
        GameState.FromJson(state.ToJson());

        // A cancelled series can still be ended? No: it is already Ended.
        Assert.Throws<InvalidCommandException>(() => state.Apply(new EndSeriesCommand(series.Id)));
        Assert.Throws<InvalidCommandException>(() => state.Apply(new WithdrawSeriesCommand(series.Id)));
    }

    [Fact]
    public void Withdrawing_returns_the_series_to_doujin_with_penalties()
    {
        var state = SerializedAtJump();
        var series = state.Series[0];
        state.StudioTrackRecord = 10;
        CloseWith(state, 84, 1000);
        CloseWith(state, 84, series.Fanbase);
        Assert.Equal(2, series.ChaptersPublished);
        var open = series.OpenChapter!;
        var fans = series.Fanbase;

        state.Apply(new WithdrawSeriesCommand(series.Id));

        Assert.Equal(PublishingStatus.Unpublished, series.Publishing);
        Assert.Null(series.Contract);
        Assert.Equal(SeriesStatus.Active, series.Status);
        Assert.Equal(fans * 0.9, series.Fanbase, 6);
        Assert.Contains(open, series.Chapters);
        Assert.Equal(EditorStatus.NotRequired, open.Editor);
        Assert.Equal(state.Clock.Now.AddDays(7), open.DueDate);
        Assert.Equal(state.Clock.Now.AddDays(7 * 52), series.PitchCooldowns[Jump]);
        Assert.Equal(10 - 3.1, state.StudioTrackRecord, 6);
        Assert.Equal(2, series.ChaptersPublished); // lifetime count kept
        var withdrawn = Assert.Single(state.Events, e => e.Type == EventType.SeriesWithdrawn);
        Assert.Equal(Jump, withdrawn.MagazineId);
        Assert.Throws<InvalidCommandException>(() => state.Apply(new WithdrawSeriesCommand(series.Id)));
        GameState.FromJson(state.ToJson());

        // Back at work as doujin: the next chapter needs no editor.
        state.Advance(24 * 3);
        Assert.Equal(EditorStatus.NotRequired, open.Editor);
        Assert.True(open.StageWork(Stage.Pencils).HoursDone > 0 || open.StageWork(Stage.Name).HoursDone > 0);
    }

    [Fact]
    public void A_proper_ending_pays_a_bonus_and_schedules_the_final_volume()
    {
        var state = SerializedAtJump();
        var series = state.Series[0];
        for (var i = 0; i < 12; i++) CloseWith(state, 90, 300_000);
        Assert.Equal(12, series.ChaptersPublished);
        var firstVolume = Assert.Single(series.Volumes);
        firstVolume.CopiesSold = 250_000;
        var trackBefore = state.StudioTrackRecord;
        var repBefore = state.People[0].Reputation;
        var averageRank = series.Chapters.Where(ch => ch.Rank is not null).Average(ch => ch.Rank!.Value);
        var bonus = ReputationRules.EndingBonus(15, averageRank, 250_000);
        Assert.True(bonus > 1.0 && bonus <= 3.0, $"bonus {bonus}");

        state.Apply(new EndSeriesCommand(series.Id));

        Assert.Equal(SeriesStatus.Ended, series.Status);
        Assert.Equal(PublishingStatus.Unpublished, series.Publishing);
        Assert.Null(series.Contract);
        Assert.Equal(trackBefore + bonus, state.StudioTrackRecord, 6);
        Assert.Equal(repBefore + bonus, state.People[0].Reputation, 6);
        Assert.All(series.Chapters, ch => Assert.Equal(ChapterStatus.Complete, ch.Status));
        Assert.Equal(2, series.Volumes.Count);
        Assert.Equal(10, series.Volumes[1].FirstChapter);
        Assert.Equal(12, series.Volumes[1].LastChapter);
        Assert.Equal(state.Clock.Now.AddDays(42), series.Volumes[1].ReleaseDate);
        var ended = Assert.Single(state.Events, e => e.Type == EventType.SeriesEnded);
        Assert.Contains("proper ending", ended.Message);
        GameState.FromJson(state.ToJson());
    }

    [Fact]
    public void Ending_early_costs_the_withdraw_penalty_and_ending_a_doujin_is_free()
    {
        var state = SerializedAtJump();
        var series = state.Series[0];
        state.StudioTrackRecord = 10;
        for (var i = 0; i < 3; i++) CloseWith(state, 84, 1000);
        state.Apply(new EndSeriesCommand(series.Id));
        Assert.Equal(10 - 3.15, state.StudioTrackRecord, 6);
        Assert.Equal(SeriesStatus.Ended, series.Status);
        var final = Assert.Single(series.Volumes); // 3 leftover published chapters make a final volume
        Assert.Equal(1, final.FirstChapter);
        Assert.Equal(3, final.LastChapter);
        Assert.Single(state.Events, e => e.Type == EventType.VolumeScheduled);

        state.Apply(new CreateSeriesCommand("Calm", "slice of life", Cadence.Monthly, 19));
        var doujin = state.Series[1];
        state.Advance(30);
        state.Apply(new EndSeriesCommand(doujin.Id));
        Assert.Equal(SeriesStatus.Ended, doujin.Status);
        Assert.Equal(10 - 3.15, state.StudioTrackRecord, 6);
        Assert.Empty(doujin.Chapters);
        Assert.Null(state.People[0].CurrentTask);
    }

    [Fact]
    public void Ending_a_pitching_or_offered_series_drops_the_pitch()
    {
        var state = GameState.NewGame();
        state.Apply(new CreateSeriesCommand("Petals", "romance", Cadence.Monthly, 19));
        var series = state.Series[0];
        state.Apply(new PitchSeriesCommand(series.Id, "hoshigaku-flowers"));
        state.Apply(new EndSeriesCommand(series.Id));
        Assert.Equal(PublishingStatus.Unpublished, series.Publishing);
        Assert.Empty(series.Chapters);

        var offered = GameState.NewGame();
        offered.Apply(new CreateSeriesCommand("Petals", "romance", Cadence.Monthly, 19));
        var s2 = offered.Series[0];
        s2.Publishing = PublishingStatus.Offered;
        s2.PendingOffer = new SerializationOffer { MagazineId = "hoshigaku-flowers", FeePerPage = 7000, FirstIssueClose = offered.Clock.Now.AddDays(60), ExpiresAt = offered.Clock.Now.AddDays(60) };
        offered.Apply(new EndSeriesCommand(s2.Id));
        Assert.Null(s2.PendingOffer);
        Assert.Equal(PublishingStatus.Unpublished, s2.Publishing);
        GameState.FromJson(offered.ToJson());
    }
}
