using System.Text.Json.Nodes;
using MangakaSim;
using Xunit;

namespace MangakaSim.Tests;

public class CatalogTests
{
    private static string PublishersJson() =>
        typeof(GameState).Assembly.GetManifestResourceStream("MangakaSim.Data.publishers.json") is { } s
            ? new StreamReader(s).ReadToEnd()
            : throw new InvalidOperationException("resource missing");

    private static string TrendsJson() =>
        typeof(GameState).Assembly.GetManifestResourceStream("MangakaSim.Data.trends.json") is { } s
            ? new StreamReader(s).ReadToEnd()
            : throw new InvalidOperationException("resource missing");

    [Fact]
    public void Default_publisher_catalog_has_six_magazines_and_three_publishers()
    {
        var catalog = PublisherCatalog.LoadDefault();
        Assert.Equal(3, catalog.Publishers.Count);
        Assert.Equal(new[] { "tokiwa-jump", "tokiwa-square", "kaidan-magazine", "kaidan-afternoon", "hoshigaku-sunday", "hoshigaku-flowers" },
            catalog.Magazines.Select(m => m.Id));
        Assert.Same(catalog, PublisherCatalog.LoadDefault());
    }

    [Fact]
    public void Tokiwa_jump_matches_the_spec_table()
    {
        var jump = PublisherCatalog.LoadDefault().Require("tokiwa-jump");
        Assert.Equal("Weekly Tokiwa Jump", jump.Name);
        Assert.Equal(1, jump.Tier);
        Assert.Equal(Cadence.Weekly, jump.Cadence);
        Assert.Equal(Demographic.Shonen, jump.Demographic);
        Assert.Equal(20, jump.RosterSize);
        Assert.Equal(15, jump.CancellationRank);
        Assert.Equal(9000, jump.FeePerPageMin);
        Assert.Equal(20000, jump.FeePerPageMax);
        Assert.Equal(DayOfWeek.Thursday, jump.IssueCloseDay);
        Assert.Equal(18, jump.IssueCloseHour);
        Assert.Equal(9, jump.ChaptersPerVolume);
        Assert.Equal(7, jump.CadenceDays);
        Assert.Equal(1.2, jump.Affinity("action"));
        Assert.Equal(0.8, jump.Affinity("romance"));
        Assert.Equal(1.0, jump.Affinity("horror"));
    }

    [Fact]
    public void Every_magazine_has_affinities_in_range_and_roster_above_line()
    {
        foreach (var m in PublisherCatalog.LoadDefault().Magazines)
        {
            Assert.All(m.GenreAffinities.Values, a => Assert.InRange(a, 0.75, 1.25));
            Assert.True(m.RosterSize > m.CancellationRank, m.Id);
            Assert.True(m.FeePerPageMin < m.FeePerPageMax, m.Id);
        }
        Assert.Equal(28, PublisherCatalog.LoadDefault().Require("hoshigaku-flowers").CadenceDays);
        Assert.Equal(14, PublisherCatalog.LoadDefault().Require("tokiwa-square").CadenceDays);
    }

    [Fact]
    public void First_close_at_or_after_finds_the_next_close_day()
    {
        var jump = PublisherCatalog.LoadDefault().Require("tokiwa-jump");
        Assert.Equal(new DateTime(1996, 4, 4, 18, 0, 0), jump.FirstCloseAtOrAfter(GameClock.Start));
        Assert.Equal(new DateTime(1996, 4, 4, 18, 0, 0), jump.FirstCloseAtOrAfter(new DateTime(1996, 4, 4, 18, 0, 0)));
        Assert.Equal(new DateTime(1996, 4, 11, 18, 0, 0), jump.FirstCloseAtOrAfter(new DateTime(1996, 4, 4, 19, 0, 0)));
    }

    [Fact]
    public void Find_and_Require_behave()
    {
        var catalog = PublisherCatalog.LoadDefault();
        Assert.Null(catalog.Find("nope"));
        Assert.Throws<InvalidDataException>(() => catalog.Require("nope"));
    }

    [Theory]
    [InlineData("$.magazines[1].id", "tokiwa-jump", "duplicate")]
    [InlineData("$.magazines[0].publisherId", "nobody", "unknown publisher")]
    [InlineData("$.magazines[0].rosterSize", 15, "cancellation line")]
    [InlineData("$.magazines[0].feePerPageMin", 20000, "fee range")]
    [InlineData("$.magazines[0].genreAffinities.action", 1.3, "0.75..1.25")]
    [InlineData("$.magazines[0].tier", 4, "tier")]
    [InlineData("$.magazines[0].issueCloseHour", 24, "hour")]
    public void Publisher_catalog_validation_rejects_bad_data(string path, object value, string expectedText)
    {
        var json = JsonNode.Parse(PublishersJson())!;
        SetPath(json, path, value);
        var ex = Assert.Throws<InvalidDataException>(() => PublisherCatalog.FromJson(json.ToJsonString()));
        Assert.Contains(expectedText, ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Default_trend_catalog_has_twelve_genres_and_curves()
    {
        var catalog = TrendCatalog.LoadDefault();
        Assert.Equal(12, catalog.Genres.Count);
        Assert.Contains("other", catalog.Genres);
        Assert.Contains("slice of life", catalog.Genres);
        Assert.Equal(1.00, catalog.PriceIndex[1996]);
        Assert.Equal(1.18, catalog.PriceIndex[2026]);
        Assert.Equal(1.0, catalog.InternetReach[2005]);
        Assert.Equal(0.25, catalog.Keyframes["slice of life"][1996]);
        Assert.Equal(1.50, catalog.Keyframes["fantasy"][2020]);
        Assert.Same(catalog, TrendCatalog.LoadDefault());
    }

    [Fact]
    public void Shipped_baseline_satisfies_the_spread_rule_in_every_keyframe_year()
    {
        var catalog = TrendCatalog.LoadDefault();
        var years = catalog.Keyframes.Values.SelectMany(c => c.Keys).Distinct().ToList();
        Assert.Equal(new[] { 1996, 2000, 2005, 2010, 2015, 2020 }, years.OrderBy(y => y));
        foreach (var year in years)
        {
            var values = catalog.Genres.Where(g => g != "other").Select(g => catalog.Keyframes[g][year]).ToList();
            Assert.True(values.Count(v => v <= 0.70) >= 2, $"{year} low end");
            Assert.True(values.Count(v => v >= 1.20) >= 2, $"{year} high end");
            Assert.InRange(values.Average(), 0.90, 1.10);
        }
    }

    [Fact]
    public void Trend_catalog_validation_rejects_bad_data()
    {
        // Missing "other".
        var json = JsonNode.Parse(TrendsJson())!;
        var genres = json["genres"]!.AsArray();
        genres.RemoveAt(genres.Count - 1);
        Assert.Contains("other", Assert.Throws<InvalidDataException>(() => TrendCatalog.FromJson(json.ToJsonString())).Message);

        // Keyframes for an unknown genre.
        json = JsonNode.Parse(TrendsJson())!;
        json["baseline"]!["isekai"] = JsonNode.Parse("{\"1996\": 1.0}");
        Assert.Contains("unknown genre", Assert.Throws<InvalidDataException>(() => TrendCatalog.FromJson(json.ToJsonString())).Message);

        // Flattening every genre breaks the spread rule.
        json = JsonNode.Parse(TrendsJson())!;
        foreach (var genre in TrendCatalog.LoadDefault().Genres)
            json["baseline"]![genre]!["1996"] = 1.0;
        Assert.Contains("spread rule", Assert.Throws<InvalidDataException>(() => TrendCatalog.FromJson(json.ToJsonString())).Message);

        // Missing curve.
        json = JsonNode.Parse(TrendsJson())!;
        json.AsObject().Remove("priceIndex");
        Assert.Contains("price index", Assert.Throws<InvalidDataException>(() => TrendCatalog.FromJson(json.ToJsonString())).Message);
    }

    [Fact]
    public void Curves_interpolate_hold_and_grow()
    {
        var curve = new SortedDictionary<int, double> { [2000] = 1.0, [2010] = 2.0 };
        Assert.Equal(1.0, Curves.Interpolate(curve, 1990.0, false));
        Assert.Equal(1.5, Curves.Interpolate(curve, 2005.0, false));
        Assert.Equal(2.0, Curves.Interpolate(curve, 2030.0, false));
        Assert.Equal(2.0 * Math.Pow(1.02, 3), Curves.Interpolate(curve, 2013.0, true), 12);
        Assert.Equal(2002.5, Curves.FractionalYear(new DateTime(2002, 7, 2, 12, 0, 0)), 12);
    }

    private static void SetPath(JsonNode root, string path, object value)
    {
        // Tiny path helper: "$.a[0].b" style.
        var node = root;
        var parts = path.TrimStart('$', '.').Split('.');
        for (var i = 0; i < parts.Length; i++)
        {
            var part = parts[i];
            string name;
            int? index = null;
            var bracket = part.IndexOf('[');
            if (bracket >= 0)
            {
                name = part[..bracket];
                index = int.Parse(part[(bracket + 1)..^1]);
            }
            else name = part;

            var last = i == parts.Length - 1;
            if (index is { } idx)
            {
                var array = node![name]!.AsArray();
                if (last) array[idx] = JsonValue.Create(value);
                else node = array[idx];
            }
            else
            {
                if (last) node![name] = JsonValue.Create(value);
                else node = node![name];
            }
        }
    }
}
