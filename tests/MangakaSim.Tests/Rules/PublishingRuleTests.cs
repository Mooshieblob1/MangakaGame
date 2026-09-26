using MangakaSim.Catalog;
using MangakaSim.Rules;
using Xunit;

namespace MangakaSim.Tests.Rules;

public class PublishingRuleTests
{
    [Fact]
    public void Catalogs_ship_all_magazines_and_spread_out_genres()
    {
        var publishers = PublisherCatalog.LoadDefault();
        var trends = TrendCatalog.LoadDefault();
        Assert.Equal(6, publishers.Magazines.Count);
        Assert.Equal(12, trends.Genres.Count);
        Assert.Same(publishers, PublisherCatalog.LoadDefault());
        foreach (var year in new[] { 1996, 2000, 2005, 2010, 2015, 2020 })
        {
            var values = trends.Genres.Where(g => g != "other")
                .Select(g => TrendRules.Baseline(trends, g, new DateTime(year, 1, 1))).ToArray();
            Assert.True(values.Count(v => v <= .7) >= 2);
            Assert.True(values.Count(v => v >= 1.2) >= 2);
            Assert.InRange(values.Average(), .9, 1.1);
        }
    }

    [Fact]
    public void Clean_skipped_and_rushed_quality_match_the_formula()
    {
        var contributions = StageOrder.All.Select(s => QualityRules.Contribution(s, 80, 0, 10, 0)).ToArray();
        Assert.Equal(84, QualityRules.Total(contributions));
        Assert.Equal(76, QualityRules.Total(contributions.Take(4)));
        Assert.Equal(22.68, QualityRules.Contribution(Stage.Pencils, 80, 2, 10, 0), 10);
        Assert.Equal(35, QualityRules.Contribution(Stage.Name, 100, 0, 10, 2));
        Assert.Equal(.7, QualityRules.RushFactor(100, 10));
    }

    [Fact]
    public void Pitch_editor_and_fees_have_exact_boundaries()
    {
        Assert.Equal(.19858176, PitchRules.Chance(1, 84, 25, 1.2, 1.3), 10);
        Assert.Equal(.3072, PitchRules.Chance(1, 100, 100, 1, 1), 10);
        Assert.Equal(.02, PitchRules.Chance(1, 0, 0, .8, .2));
        Assert.Equal(.95, PitchRules.Chance(3, 100, 100, 1.2, 1.8));
        Assert.Equal(65, EditorRules.Threshold(1, 0));
        Assert.Equal(.95, EditorRules.Chance(1, 85, 0), 10);
        Assert.Equal(.05, EditorRules.Chance(1, 45, 0), 10);
        Assert.Equal(14500, ReputationRules.Fee(9000, 20000, 50, 1));
        Assert.Equal(ReputationRules.Fee(9000, 20000, 50, 1), ReputationRules.Fee(9000, 20000, 0, 1));
        Assert.Equal(16700, ReputationRules.Fee(9000, 20000, 70, 1));
        Assert.Equal(.12 * .5 * .6, PitchRules.Chance(1, 50, 0, 1, 1), 10);
        Assert.Equal(.24 * .5 * .6, PitchRules.Chance(2, 50, 0, 1, 1), 10);
        Assert.Equal(.40 * .5 * .6, PitchRules.Chance(3, 50, 0, 1, 1), 10);
    }

    [Fact]
    public void Sales_use_live_fans_and_floor_copy_counts()
    {
        Assert.Equal(7200, SalesRules.CommercialCopies(10000, 84, 1, 1));
        Assert.Equal(386, SalesRules.CommercialCopies(10000, 84, 1, 5));
        Assert.Equal(3840, SalesRules.DoujinCopies(10000, 84, 1, 0, 1));
        Assert.Equal(4032, SalesRules.DoujinCopies(10000, 84, 1, .1, 1));
        Assert.Equal(0, SalesRules.CommercialCopies(10000, 84, 1, 53));
    }

    [Fact]
    public void Curves_crowding_protection_and_clocks_use_plan_boundaries()
    {
        var c = TrendCatalog.LoadDefault();
        Assert.Equal(1, Economy.PriceIndex(c, GameClock.Start));
        Assert.Equal(120000, Economy.InternetCost(c, GameClock.Start));
        Assert.Equal(1.18 * Math.Pow(1.02, 4), Economy.PriceIndex(c, new DateTime(2030, 1, 1)), 10);
        Assert.Equal("other", TrendRules.Normalise("unknown", c));
        Assert.Equal("action", TrendRules.Normalise(" ACTION ", c));
        Assert.Equal(1, TrendRules.Crowding(2));
        Assert.Equal(.97, TrendRules.Crowding(3));
        Assert.Equal(.85, TrendRules.Crowding(7));
        Assert.Equal(.7, ReputationRules.Protection(300, 500000, 50), 10);
        Assert.Equal((2, 3, 8), CancellationRules.Clocks(0));
        Assert.Equal((8, 26, 2), CancellationRules.Clocks(1));
    }

    [Fact]
    public void Monthly_issues_are_28_days_and_exact_close_is_inclusive()
    {
        var m = PublisherCatalog.LoadDefault().Get("hoshigaku-flowers");
        var first = IssueSchedule.FirstCloseAtOrAfter(m, GameClock.Start);
        Assert.Equal(new DateTime(1996, 4, 3, 18, 0, 0), first);
        Assert.Equal(first, IssueSchedule.FirstCloseAtOrAfter(m, first));
        Assert.Equal(first.AddDays(28), IssueSchedule.FirstCloseAfter(m, first));
        Assert.Equal(first.AddDays(112), IssueSchedule.AddIssues(m, first, 4));
    }

    [Fact]
    public void Invalid_rng_bounds_do_not_consume_a_draw()
    {
        var rng = Rng.FromSeed(12);
        var before = rng.State;
        Assert.Throws<ArgumentOutOfRangeException>(() => rng.NextInt(3, 3));
        Assert.Throws<ArgumentOutOfRangeException>(() => rng.NextDouble(double.NaN, 1));
        Assert.Equal(before, rng.State);
        for (var i = 0; i < 1000; i++)
        {
            Assert.InRange(rng.NextInt(-3, 4), -3, 3);
            Assert.True(rng.NextDouble() is >= 0 and < 1);
        }
    }
}
