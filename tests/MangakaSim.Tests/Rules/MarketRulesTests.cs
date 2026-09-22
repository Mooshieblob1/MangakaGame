using MangakaSim;
using Xunit;

namespace MangakaSim.Tests.Rules;

public class MarketRulesTests
{
    [Fact]
    public void Ranking_scores()
    {
        Assert.Equal(200_000, RankingRules.K(1));
        Assert.Equal(50_000, RankingRules.K(3));
        Assert.Equal(50, RankingRules.FanScore(200_000, 1), 9);
        Assert.Equal(0, RankingRules.FanScore(0, 1), 9);
        Assert.Equal(50.4 * 1.56, RankingRules.PlayerScore(84, 0, 1, 1.2, 1.3, 1.0), 9);
        Assert.Equal(70 * 0.97, RankingRules.FillerScore(70, 0.97), 9);
    }

    [Fact]
    public void Rank_factor_is_piecewise_between_first_line_and_roster()
    {
        Assert.Equal(3.0, FanbaseRules.RankFactor(1, 15, 20), 9);
        Assert.Equal(0.5, FanbaseRules.RankFactor(15, 15, 20), 9);
        Assert.Equal(0.2, FanbaseRules.RankFactor(20, 15, 20), 9);
        Assert.Equal(0.2, FanbaseRules.RankFactor(25, 15, 20), 9);
        Assert.Equal(1.75, FanbaseRules.RankFactor(8, 15, 20), 9);
        Assert.Equal(0.35, FanbaseRules.RankFactor(17, 15, 19), 9); // halfway from the line to the roster
    }

    [Fact]
    public void Issue_gain_and_base_readers()
    {
        Assert.Equal(3000, FanbaseRules.BaseReaders(1));
        Assert.Equal(800, FanbaseRules.BaseReaders(3));
        Assert.Equal(10800, FanbaseRules.IssueGain(1, 3.0, 84), 9);
    }

    [Fact]
    public void Tankobon_weekly_copies()
    {
        Assert.Equal(7200, SalesRules.TankobonCopies(1, 10_000, 84, 1.0));
        Assert.Equal(386, SalesRules.TankobonCopies(5, 10_000, 84, 1.0));
        Assert.Equal(480, SalesRules.TankobonCopies(2, 10_000, 84, 1.0));
        Assert.Equal(9360, SalesRules.TankobonCopies(1, 10_000, 84, 1.3));
    }

    [Fact]
    public void Doujin_weekly_copies_with_and_without_internet()
    {
        Assert.Equal(240, SalesRules.DoujinCopies(1, 0, 84, 1.0, 0));
        Assert.Equal(360, SalesRules.DoujinCopies(1, 0, 84, 1.0, 1.0));
        Assert.Equal(96, SalesRules.DoujinCopies(2, 0, 84, 1.0, 0));
        Assert.Equal(144, SalesRules.DoujinCopies(3, 0, 84, 1.0, 1.0));
        Assert.Equal(4, SalesRules.DoujinWindow(false));
        Assert.Equal(8, SalesRules.DoujinWindow(true));
    }

    [Fact]
    public void Covers_and_royalties()
    {
        Assert.Equal(400, SalesRules.TankobonCover(1.0), 9);
        Assert.Equal(590, SalesRules.DoujinCover(1.18), 9);
        Assert.Equal(288_000, SalesRules.Royalty(7200, 400));
        Assert.Equal(43_200, SalesRules.DoujinIncome(240, 500, 120)); // 180 yen per copy after printing
        Assert.Equal(180, SalesRules.DoujinIncome(1, 500, SalesRules.PrintCost(1.0)));
    }

    [Fact]
    public void Reputation_terms()
    {
        Assert.Equal(10, ReputationRules.StaffTerm(new[] { 10.0 }), 9);
        Assert.Equal(66, ReputationRules.StaffTerm(new[] { 50.0, 30, 10, 90 }), 9);
        Assert.Equal(0.5 * 40 + 0.5 * 10, ReputationRules.Effective(40, 10), 9);
        Assert.Equal(0.7, ReputationRules.Protection(300, 500_000, 50), 9);
        Assert.Equal(0.0, ReputationRules.Protection(0, 0, 0), 9);
        Assert.Equal(3.7, ReputationRules.EndingBonus(15, 5, 500_000), 9);
        Assert.Equal(1.0, ReputationRules.EndingBonus(15, 20, 500_000), 9);
        Assert.Equal(-4, ReputationRules.WithdrawPenalty(20), 9);
        Assert.Equal(-10, ReputationRules.WithdrawPenalty(200), 9);
        Assert.Equal(1.2, ReputationRules.ChapterCompletionDelta(84, 1.0, false), 9);
        Assert.Equal(0.6, ReputationRules.ChapterCompletionDelta(84, 1.0, true), 9);
    }

    [Fact]
    public void Cancellation_clocks_and_strikes()
    {
        Assert.Equal(3, CancellationRules.WarningClock(0));
        Assert.Equal(3, CancellationRules.CancelClock(0));
        Assert.Equal(8, CancellationRules.StrikeLifetime(0));
        Assert.Equal(12, CancellationRules.WarningClock(1));
        Assert.Equal(26, CancellationRules.CancelClock(1));
        Assert.Equal(2, CancellationRules.StrikeLifetime(1));
        Assert.Equal(0.8, CancellationRules.CancelChance(50), 9);

        var now = new DateTime(1996, 6, 1, 18, 0, 0);
        var strikes = new[] { now.AddDays(-7 * 8), now.AddDays(-7 * 7), now.AddDays(-1) };
        var live = CancellationRules.LiveStrikes(strikes, now, 7, 8);
        Assert.Equal(2, live.Count);
        Assert.DoesNotContain(now.AddDays(-56), live);
    }

    [Fact]
    public void Filler_word_lists_and_ranges()
    {
        Assert.True(FillerRules.Adjectives.Distinct().Count() >= 40);
        Assert.True(FillerRules.Nouns.Distinct().Count() >= 40);
        Assert.Equal((40, 95), FillerRules.PopularityRange(1));
        Assert.Equal((35, 85), FillerRules.PopularityRange(2));
        Assert.Equal((30, 75), FillerRules.PopularityRange(3));
        Assert.Equal(5, FillerRules.Drift(6, -3));
        Assert.Equal(100, FillerRules.Drift(99, 3));
    }
}
