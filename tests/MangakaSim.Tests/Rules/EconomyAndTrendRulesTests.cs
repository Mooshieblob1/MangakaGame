using MangakaSim;
using Xunit;

namespace MangakaSim.Tests.Rules;

public class EconomyAndTrendRulesTests
{
    private static readonly TrendCatalog Catalog = TrendCatalog.LoadDefault();

    [Fact]
    public void Price_index_interpolates_and_grows_after_last_keyframe()
    {
        Assert.Equal(1.00, Economy.PriceIndex(Catalog, new DateTime(1996, 4, 1, 8, 0, 0)), 9);
        Assert.Equal(1.00, Economy.PriceIndex(Catalog, new DateTime(1996, 12, 31, 23, 0, 0)), 9);
        Assert.Equal(1.01, Economy.PriceIndex(Catalog, new DateTime(1997, 6, 1)), 9);
        Assert.Equal(1.015, Economy.PriceIndex(Catalog, new DateTime(1999, 7, 1)), 9);
        Assert.Equal(1.18 * Math.Pow(1.02, 4), Economy.PriceIndex(Catalog, new DateTime(2030, 1, 1)), 9);
        Assert.Equal(120_000, Economy.Inflate(Economy.InternetCost1996, Economy.PriceIndex(Catalog, GameClock.Start)));
        Assert.Equal(141_600, Economy.Inflate(120_000, 1.18));
    }

    [Fact]
    public void Internet_reach_follows_its_curve_and_holds_after_2015()
    {
        Assert.Equal(0.1, Economy.InternetReach(Catalog, new DateTime(1996, 4, 1)), 9);
        Assert.Equal(0.15, Economy.InternetReach(Catalog, new DateTime(1997, 9, 1)), 9);
        Assert.Equal(1.0, Economy.InternetReach(Catalog, new DateTime(2005, 8, 1)), 9);
        Assert.Equal(2.0, Economy.InternetReach(Catalog, new DateTime(2040, 1, 1)), 9);
    }

    [Fact]
    public void Baseline_interpolates_between_keyframes_and_holds_after_2020()
    {
        Assert.Equal(0.60, TrendRules.Baseline(Catalog, "slice of life", new DateTime(2002, 7, 2, 12, 0, 0)), 9);
        Assert.Equal(1.00, TrendRules.Baseline(Catalog, "slice of life", new DateTime(2025, 1, 1)), 9);
        Assert.Equal(0.25, TrendRules.Baseline(Catalog, "Slice Of Life ", new DateTime(1996, 1, 1)), 9);
        Assert.Equal(0.90, TrendRules.Baseline(Catalog, "isekai", new DateTime(2010, 1, 1)), 9);
    }

    [Fact]
    public void Effective_adds_layers_and_clamps()
    {
        var trend = new GenreTrend { Genre = "fantasy", Noise = 0.05, Boom = 0.1, PlayerInfluence = 0.1 };
        var now = new DateTime(2020, 1, 1);
        Assert.Equal(1.5 + 0.05 + 0.1 + 0.1, TrendRules.Effective(trend, Catalog, now), 9);
        trend.Boom = 1.0;
        Assert.Equal(1.8, TrendRules.Effective(trend, Catalog, now), 9);
        var cool = new GenreTrend { Genre = "fantasy", Noise = -0.15 };
        Assert.Equal(0.80 - 0.15, TrendRules.Effective(cool, Catalog, new DateTime(1996, 1, 1)), 9);
        var cold = new GenreTrend { Genre = "slice of life", Noise = -0.15 };
        Assert.Equal(0.2, TrendRules.Effective(cold, Catalog, new DateTime(1996, 1, 1)), 9); // 0.10 clamps up
    }

    [Theory]
    [InlineData(0, 1.0)]
    [InlineData(1, 1.0)]
    [InlineData(2, 1.0)]
    [InlineData(3, 0.97)]
    [InlineData(4, 0.94)]
    [InlineData(5, 0.91)]
    [InlineData(6, 0.88)]
    [InlineData(7, 0.85)]
    [InlineData(8, 0.85)]
    public void Crowding_table(int others, double expected)
    {
        Assert.Equal(expected, TrendRules.Crowding(others), 9);
    }

    [Fact]
    public void Normalise_trims_lowercases_and_maps_unknown_to_other()
    {
        Assert.Equal("sci-fi", TrendRules.Normalise("  Sci-Fi ", Catalog));
        Assert.Equal("other", TrendRules.Normalise("isekai", Catalog));
        Assert.Equal("other", TrendRules.Normalise(null, Catalog));
        Assert.Equal("slice of life", TrendRules.Normalise("SLICE OF LIFE", Catalog));
    }
}
