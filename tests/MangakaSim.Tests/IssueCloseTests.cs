using MangakaSim;
using Xunit;

namespace MangakaSim.Tests;

public class IssueCloseTests
{
    private const string Jump = "tokiwa-jump";

    /// <summary>A weekly action series forced into serialization at Jump with its open chapter due at the next Thursday close.</summary>
    internal static GameState SerializedAtJump(int seed = 0, int fee = 10_000)
    {
        var state = GameState.NewGame(seed);
        state.Apply(new CreateSeriesCommand("Rush", "action", Cadence.Weekly, 19));
        var series = state.Series[0];
        series.Publishing = PublishingStatus.Serialized;
        series.Contract = new Contract { MagazineId = Jump, FeePerPage = fee, SignedAt = state.Clock.Now };
        series.OpenChapter!.DueDate = state.MarketOf(Jump).NextIssueClose;
        return state;
    }

    /// <summary>Marks the chapter finished and approved with a clean quality, as if Aki had drawn it perfectly.</summary>
    internal static void ForceComplete(GameState state, Chapter chapter, int quality = 84)
    {
        foreach (var work in chapter.Stages)
        {
            work.HoursDone = work.HoursRequired;
            work.Status = StageStatus.Complete;
            work.Contribution = QualityRules.Weight(work.Stage) * quality;
        }
        chapter.Status = ChapterStatus.Complete;
        chapter.CompletedAt = state.Clock.Now;
        chapter.Quality = quality;
        chapter.Editor = EditorStatus.Approved;
        chapter.IsAtRisk = false;
        state.RunPlanner();
    }

    private static void AdvanceToNextClose(GameState state, string magazineId)
    {
        state.Advance(state.Clock.HoursUntil(state.MarketOf(magazineId).NextIssueClose));
    }

    [Fact]
    public void A_ready_chapter_is_published_paid_and_ranked()
    {
        var state = SerializedAtJump();
        var series = state.Series[0];
        var chapter = series.Chapters[0];
        ForceComplete(state, chapter);
        Assert.Equal(2, series.Chapters.Count);
        Assert.Equal(new DateTime(1996, 4, 11, 18, 0, 0), series.Chapters[1].DueDate); // strictly after the first close

        AdvanceToNextClose(state, Jump);
        Assert.Equal(new DateTime(1996, 4, 4, 18, 0, 0), state.Clock.Now);
        Assert.Equal(state.Clock.Now, chapter.PublishedAt);
        Assert.Equal(1, series.ChaptersPublished);
        Assert.Equal(1, series.Contract!.ChaptersPublished);
        var fee = Assert.Single(state.Ledger);
        Assert.Equal("chapter fee", fee.Reason);
        Assert.Equal(19 * 10_000, fee.Amount);
        Assert.Equal(500_000 + 190_000, state.Money);
        var published = Assert.Single(state.Events, e => e.Type == EventType.ChapterPublished);
        Assert.Equal(190_000, published.Amount);
        Assert.Equal(Jump, published.MagazineId);

        var market = state.MarketOf(Jump);
        Assert.Equal(20, market.LastRanking.Count);
        Assert.Equal(Enumerable.Range(1, 20), market.LastRanking.Select(r => r.Rank));
        var row = Assert.Single(market.LastRanking, r => r.SeriesId == series.Id);
        Assert.Equal(row.Rank, chapter.Rank);
        Assert.Equal(row.Rank, series.LastRank);
        Assert.True(market.LastRanking.Zip(market.LastRanking.Skip(1)).All(p => p.First.Score >= p.Second.Score));
        var ranking = Assert.Single(state.Events, e => e.Type == EventType.RankingPublished);
        Assert.Equal(row.Rank, ranking.Rank);
        Assert.Contains($"#{row.Rank} of 20", ranking.Message);

        Assert.True(series.Fanbase > 0);
        var expectedGain = FanbaseRules.IssueGain(1, FanbaseRules.RankFactor(row.Rank, 15, 20), 84);
        Assert.Equal(expectedGain, series.Fanbase, 6); // 0 x 0.995 + gain
        Assert.Equal(0.05 + (row.Rank <= 3 ? 0.1 : 0), series.CulturalImpact, 9);
        Assert.Equal(1, market.IssuesClosed);
        Assert.Equal(new DateTime(1996, 4, 11, 18, 0, 0), market.NextIssueClose);
    }

    [Fact]
    public void Score_uses_quality_fanbase_affinity_trend_and_crowding()
    {
        var state = SerializedAtJump();
        var series = state.Series[0];
        series.Fanbase = 200_000;
        ForceComplete(state, series.Chapters[0], quality: 90);
        AdvanceToNextClose(state, Jump);
        var market = state.MarketOf(Jump);
        var row = Assert.Single(market.LastRanking, r => r.SeriesId == series.Id);
        var sameGenre = market.LastRanking.Count(r => r.FillerId is { } id && market.Fillers.Any(f => f.Id == id && f.Genre == "action"));
        // Fillers drifted after scoring, but genres did not, so the crowding count is reproducible. The trend is
        // the baseline at the close, before the top-3 finish added its 0.01 player influence.
        var trendAtClose = TrendRules.Baseline(TrendCatalog.LoadDefault(), "action", state.Clock.Now);
        var expected = RankingRules.PlayerScore(90, 200_000, 1, 1.2, trendAtClose, TrendRules.Crowding(sameGenre));
        Assert.Equal(expected, row.Score, 6);
    }

    [Fact]
    public void An_unready_chapter_misses_the_issue()
    {
        var state = SerializedAtJump();
        var series = state.Series[0];
        series.Contract!.ChaptersPublished = 8; // grace period over
        series.Fanbase = 1000;
        state.StudioTrackRecord = 5;
        var chapter = series.Chapters[0];
        state.People[0].OvertimeAllowed = false;

        AdvanceToNextClose(state, Jump);
        Assert.Null(chapter.PublishedAt);
        var missed = Assert.Single(state.Events, e => e.Type == EventType.IssueMissed);
        Assert.Contains("Strike 1", missed.Message);
        Assert.Equal(new DateTime(1996, 4, 4, 18, 0, 0), Assert.Single(series.Strikes));
        Assert.Equal(new DateTime(1996, 4, 11, 18, 0, 0), chapter.DueDate);
        Assert.Equal(970, series.Fanbase, 6);
        Assert.Equal(4, state.StudioTrackRecord);
        Assert.Equal(19, state.MarketOf(Jump).LastRanking.Count);
        Assert.DoesNotContain(state.MarketOf(Jump).LastRanking, r => r.SeriesId == series.Id);
        Assert.DoesNotContain(state.Events, e => e.Type == EventType.RankingPublished);
        Assert.Empty(state.Ledger);
    }

    [Fact]
    public void Misses_during_the_grace_period_cost_no_strike()
    {
        var state = SerializedAtJump();
        state.People[0].OvertimeAllowed = false;
        AdvanceToNextClose(state, Jump);
        Assert.Empty(state.Series[0].Strikes);
        var missed = Assert.Single(state.Events, e => e.Type == EventType.IssueMissed);
        Assert.Contains("No strike", missed.Message);
    }

    [Fact]
    public void A_late_chapter_publishes_at_the_following_close_and_later_chapters_stay_in_order()
    {
        var state = SerializedAtJump();
        var series = state.Series[0];
        var first = series.Chapters[0];
        state.People[0].OvertimeAllowed = false;
        AdvanceToNextClose(state, Jump); // miss 4 April
        ForceComplete(state, first);      // now ready; planner creates chapter 2 due after 11 April
        Assert.Equal(new DateTime(1996, 4, 11, 18, 0, 0), first.DueDate);
        Assert.Equal(new DateTime(1996, 4, 18, 18, 0, 0), series.Chapters[1].DueDate);
        AdvanceToNextClose(state, Jump);
        Assert.Equal(new DateTime(1996, 4, 11, 18, 0, 0), first.PublishedAt);
        Assert.Equal(1, series.ChaptersPublished);
    }

    [Fact]
    public void Fillers_drift_by_small_steps_and_stay_in_range()
    {
        var state = GameState.NewGame(3);
        var before = state.MarketOf(Jump).Fillers.ToDictionary(f => f.Id, f => f.Popularity);
        AdvanceToNextClose(state, Jump);
        var market = state.MarketOf(Jump);
        Assert.Equal(19, market.Fillers.Count);
        foreach (var filler in market.Fillers)
        {
            Assert.InRange(filler.Popularity - before[filler.Id], -3, 3);
            Assert.InRange(filler.Popularity, 5, 100);
        }
        Assert.Contains(market.Fillers, f => f.Popularity != before[f.Id]);
    }

    [Fact]
    public void A_bottom_filler_is_replaced_after_twelve_issues_but_an_iconic_one_survives()
    {
        var state = GameState.NewGame(4);
        var market = state.MarketOf(Jump);
        var weak = market.Fillers.First(f => !f.IsIconic);
        var star = market.Fillers.Single(f => f.IsIconic);
        var weakId = weak.Id;
        var starId = star.Id;
        var nextId = market.NextFillerId;
        for (var issue = 1; issue <= 12; issue++)
        {
            weak.Popularity = 5;
            star.Popularity = 5;
            AdvanceToNextClose(state, Jump);
            if (issue < 12)
            {
                Assert.Contains(market.Fillers, f => f.Id == weakId);
                Assert.Equal(issue, market.Fillers.First(f => f.Id == weakId).IssuesBelowLine);
            }
        }
        Assert.DoesNotContain(market.Fillers, f => f.Id == weakId);
        var replacement = market.Fillers.Single(f => f.Id == nextId);
        Assert.InRange(replacement.Popularity, 45, 65);
        Assert.Equal(0, replacement.IssuesBelowLine);
        Assert.Equal(19, market.Fillers.Count);
        Assert.Contains(market.Fillers, f => f.Id == starId && f.IsIconic);
        Assert.Equal(12, star.IssuesBelowLine);
    }

    [Fact]
    public void Monthly_trend_update_runs_once_per_month_across_magazines()
    {
        var state = GameState.NewGame(8);
        state.Advance(state.Clock.HoursUntil(new DateTime(1996, 4, 30, 23, 0, 0)));
        Assert.All(state.Trends, t => Assert.Equal(0, t.Noise));
        Assert.Equal(new DateTime(1996, 4, 1), state.LastTrendUpdateMonth);

        state.Advance(state.Clock.HoursUntil(new DateTime(1996, 5, 1, 18, 0, 0))); // Kaidan Magazine and Flowers close
        Assert.Equal(new DateTime(1996, 5, 1), state.LastTrendUpdateMonth);
        Assert.Contains(state.Trends, t => t.Genre != "other" && t.Noise != 0);
        Assert.Equal(0, state.TrendOf("other").Noise);
        var snapshot = state.Trends.Select(t => (t.Genre, t.Noise, t.Boom, t.PlayerInfluence)).ToList();

        state.Advance(state.Clock.HoursUntil(new DateTime(1996, 5, 3, 18, 0, 0))); // Jump, Square and Afternoon close
        Assert.Equal(snapshot, state.Trends.Select(t => (t.Genre, t.Noise, t.Boom, t.PlayerInfluence)).ToList());
        Assert.All(state.Trends, t => Assert.InRange(t.Noise, -0.02, 0.02));

        state.Advance(state.Clock.HoursUntil(new DateTime(1996, 6, 5, 18, 0, 0)));
        Assert.Equal(new DateTime(1996, 6, 1), state.LastTrendUpdateMonth);
        Assert.NotEqual(snapshot, state.Trends.Select(t => (t.Genre, t.Noise, t.Boom, t.PlayerInfluence)).ToList());
    }

    [Fact]
    public void A_boom_fades_toward_its_floor_and_clears()
    {
        var state = GameState.NewGame();
        var fantasy = state.TrendOf("fantasy");
        fantasy.Boom = 0.4;
        fantasy.BoomPeak = 0.4;
        fantasy.BoomEndsAt = new DateTime(1996, 4, 1);
        fantasy.BoomFadeEndsAt = new DateTime(1997, 4, 1);

        state.Advance(state.Clock.HoursUntil(new DateTime(1996, 5, 1, 18, 0, 0)));
        Assert.True(fantasy.BoomFading);
        var shifted = Assert.Single(state.Events, e => e.Type == EventType.GenreTrendShifted);
        Assert.Contains("fantasy boom is cooling", shifted.Message);
        Assert.True(fantasy.Boom < 0.4);
        Assert.True(fantasy.Boom >= fantasy.BoomFloor);
        Assert.True(fantasy.BoomFloor == 0 || Math.Abs(fantasy.BoomFloor - 0.2) < 1e-9);

        state.Advance(state.Clock.HoursUntil(new DateTime(1997, 4, 3, 18, 0, 0)));
        Assert.Null(fantasy.BoomEndsAt);
        Assert.Null(fantasy.BoomFadeEndsAt);
        Assert.False(fantasy.BoomFading);
        Assert.Equal(0, fantasy.BoomPeak);
        Assert.True(fantasy.Boom == 0 || Math.Abs(fantasy.Boom - 0.2) < 1e-9);
        GameState.FromJson(state.ToJson());
    }

    [Fact]
    public void A_boom_holds_at_its_peak_until_it_ends()
    {
        var state = GameState.NewGame();
        var horror = state.TrendOf("horror");
        horror.Boom = 0.45;
        horror.BoomPeak = 0.45;
        horror.BoomEndsAt = new DateTime(1998, 1, 1);
        horror.BoomFadeEndsAt = new DateTime(1999, 1, 1);
        state.Advance(24 * 90);
        Assert.Equal(0.45, horror.Boom);
        Assert.False(horror.BoomFading);
        Assert.Equal(0.70 + 0.45 + horror.Noise, state.GenreTrendFor(new Series { Genre = "horror" }), 9);
    }

    [Fact]
    public void A_top_three_finish_with_high_quality_moves_influence_reputation_and_impact()
    {
        var state = SerializedAtJump();
        var series = state.Series[0];
        series.Fanbase = 5_000_000;
        ForceComplete(state, series.Chapters[0], quality: 100);
        var repBefore = state.People[0].Reputation;
        AdvanceToNextClose(state, Jump);
        Assert.Equal(1, series.LastRank);
        Assert.Equal(0.01, state.TrendOf("action").PlayerInfluence, 9);
        Assert.Equal(0.5, state.StudioTrackRecord, 9);
        Assert.Equal(repBefore + 0.5, state.People[0].Reputation, 9);
        Assert.Equal(0.15, series.CulturalImpact, 9);
        Assert.DoesNotContain(state.Events, e => e.Type == EventType.GenreTrendShifted);
    }

    [Fact]
    public void Player_influence_announces_each_tenth_and_decays_monthly_without_a_serialized_series()
    {
        var state = GameState.NewGame();
        state.AddPlayerInfluence("Sports", 0.09);
        Assert.DoesNotContain(state.Events, e => e.Type == EventType.GenreTrendShifted);
        state.AddPlayerInfluence("sports", 0.01);
        var shifted = Assert.Single(state.Events, e => e.Type == EventType.GenreTrendShifted);
        Assert.Contains("Sports is catching on", shifted.Message);
        state.AddPlayerInfluence("sports", 1.0);
        Assert.Equal(0.5, state.TrendOf("sports").PlayerInfluence);
        state.Advance(state.Clock.HoursUntil(new DateTime(1996, 5, 1, 18, 0, 0)));
        Assert.Equal(0.495, state.TrendOf("sports").PlayerInfluence, 9);
    }

    [Fact]
    public void A_series_becomes_iconic_once_and_then_takes_no_strikes()
    {
        var state = SerializedAtJump();
        var series = state.Series[0];
        series.Contract!.ChaptersPublished = 20;
        series.CulturalImpact = 89.95;
        series.Fanbase = 1_000_000;
        ForceComplete(state, series.Chapters[0]);
        AdvanceToNextClose(state, Jump);
        Assert.True(series.IsIconic);
        Assert.True(series.CulturalImpact >= 90);
        Assert.Single(state.Events, e => e.Type == EventType.SeriesBecameIconic);

        var fansBefore = series.Fanbase;
        var influenceBefore = state.TrendOf("action").PlayerInfluence; // earned by the rank before the transition
        var next = series.OpenChapter!;
        state.Apply(new PauseSeriesCommand(series.Id)); // hiatus: the next issue is missed
        AdvanceToNextClose(state, Jump);
        Assert.Contains(state.Events, e => e.Type == EventType.IssueMissed);
        Assert.Empty(series.Strikes);
        Assert.Equal(fansBefore, series.Fanbase);
        Assert.Single(state.Events, e => e.Type == EventType.SeriesBecameIconic);

        state.Apply(new ResumeSeriesCommand(series.Id));
        ForceComplete(state, next);
        AdvanceToNextClose(state, Jump);
        Assert.Equal(fansBefore + FanbaseRules.IssueGain(1, 1.5, 84), series.Fanbase, 6);
        Assert.Equal(influenceBefore, state.TrendOf("action").PlayerInfluence); // an Iconic series adds no influence
    }

    [Fact]
    public void Nine_published_chapters_schedule_a_tankobon()
    {
        var state = SerializedAtJump();
        var series = state.Series[0];
        for (var i = 0; i < 9; i++)
        {
            ForceComplete(state, series.OpenChapter!);
            AdvanceToNextClose(state, Jump);
        }
        Assert.Equal(9, series.ChaptersPublished);
        var volume = Assert.Single(series.Volumes);
        Assert.False(volume.IsDoujin);
        Assert.False(volume.IsReleased);
        Assert.Equal(1, volume.FirstChapter);
        Assert.Equal(9, volume.LastChapter);
        Assert.Equal(84, volume.AverageQuality, 6);
        Assert.Equal(state.Clock.Now.AddDays(42), volume.ReleaseDate);
        var scheduled = Assert.Single(state.Events, e => e.Type == EventType.VolumeScheduled);
        Assert.Equal(volume.Id, scheduled.VolumeId);
        GameState.FromJson(state.ToJson());
    }

    [Fact]
    public void Issue_close_state_round_trips()
    {
        var state = SerializedAtJump(2);
        ForceComplete(state, state.Series[0].Chapters[0]);
        AdvanceToNextClose(state, Jump);
        var loaded = GameState.FromJson(state.ToJson());
        Assert.Equal(state.ToJson(), loaded.ToJson());
        state.Advance(24 * 30);
        loaded.Advance(24 * 30);
        Assert.Equal(state.ToJson(), loaded.ToJson());
    }
}
