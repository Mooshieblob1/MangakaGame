using System.Text.Json;
using System.Text.Json.Nodes;
using MangakaSim.Catalog;
using MangakaSim.Rules;
using Xunit;

namespace MangakaSim.Tests;

public class CatalogTests
{
    [Theory]
    [InlineData("Tier", 0)] [InlineData("Tier", 4)]
    [InlineData("RosterSize", 15)] [InlineData("CancellationRank", 0)]
    [InlineData("FeePerPageMin", 20000)] [InlineData("FeePerPageMax", 1)]
    [InlineData("ChaptersPerVolume", 0)] [InlineData("IssueCloseHour", 24)]
    [InlineData("IssueCloseDay", 9)] [InlineData("Cadence", 99)] [InlineData("Demographic", 99)]
    public void Magazine_rejects_invalid_parameters(string field, int value)
    {
        var json = JsonNode.Parse(JsonSerializer.Serialize(PublisherCatalog.LoadDefault()))!;
        json["Magazines"]![0]![field] = value;
        Assert.Throws<InvalidDataException>(() => PublisherCatalog.FromJson(json.ToJsonString()));
    }
    [Theory]
    [InlineData("duplicate")] [InlineData("publisher")] [InlineData("affinity")]
    [InlineData("genre")] [InlineData("words")] [InlineData("null")]
    public void Magazine_rejects_broken_references_and_lists(string mutation)
    {
        var json = JsonNode.Parse(JsonSerializer.Serialize(PublisherCatalog.LoadDefault()))!;
        var magazines = json["Magazines"]!;
        switch (mutation)
        {
            case "duplicate": magazines[1]!["Id"] = magazines[0]!["Id"]!.GetValue<string>(); break;
            case "publisher": magazines[0]!["PublisherId"] = "missing"; break;
            case "affinity": magazines[0]!["GenreAffinities"]!["action"] = 1.3; break;
            case "genre": magazines[0]!["GenreAffinities"]!["missing"] = 1; break;
            case "words": json["Nouns"] = new JsonArray(); break;
            case "null": magazines[0] = null; break;
        }
        Assert.Throws<InvalidDataException>(() => PublisherCatalog.FromJson(json.ToJsonString()));
    }
    [Theory]
    [InlineData("other")] [InlineData("order")] [InlineData("spread")] [InlineData("empty")]
    public void Trend_catalog_rejects_missing_or_flat_data(string mutation)
    {
        var json = JsonNode.Parse(JsonSerializer.Serialize(TrendCatalog.LoadDefault()))!;
        switch (mutation)
        {
            case "other": json["Genres"]!.AsArray().RemoveAt(11); break;
            case "order": json["PriceIndex"]![1]!["Year"] = 1995; break;
            case "spread": json["Baselines"]!["horror"]![0]!["Value"] = 1; break;
            case "empty": json["InternetReach"] = new JsonArray(); break;
        }
        Assert.Throws<InvalidDataException>(() => TrendCatalog.FromJson(json.ToJsonString()));
    }
    [Theory]
    [InlineData("tokiwa-jump", 7)] [InlineData("tokiwa-square", 14)]
    [InlineData("kaidan-magazine", 7)] [InlineData("kaidan-afternoon", 28)]
    [InlineData("hoshigaku-sunday", 7)] [InlineData("hoshigaku-flowers", 28)]
    public void Every_calendar_retains_its_anchor_across_years(string id, int days)
    {
        var m = PublisherCatalog.LoadDefault().Get(id);
        var anchor = IssueSchedule.Anchor(m);
        var close = IssueSchedule.FirstCloseAtOrAfter(m, new DateTime(1997, 1, 1));
        Assert.Equal(0, (close - anchor).TotalDays % days);
        Assert.Equal(m.IssueCloseDay, close.DayOfWeek);
        Assert.Equal(close.AddDays(days), IssueSchedule.FirstCloseAfter(m, close));
        Assert.Equal(close, IssueSchedule.FirstCloseAtOrAfter(m, close.AddHours(-1)));
    }
    [Fact]
    public void Pure_rank_reputation_and_curve_boundaries()
    {
        Assert.Equal(3, FanbaseRules.RankFactor(1, 15, 20));
        Assert.Equal(.5, FanbaseRules.RankFactor(15, 15, 20));
        Assert.Equal(.2, FanbaseRules.RankFactor(21, 15, 20));
        Assert.Equal(0, RankingRules.FanScore(0, 1));
        Assert.Equal(50, RankingRules.FanScore(100000, 2));
        Assert.Equal(10, ReputationRules.StaffTerm(new[] { 10.0 }));
        Assert.Equal(81.25, ReputationRules.StaffTerm(new[] { 50.0, 100 }));
        Assert.Equal(23, ReputationRules.StaffTerm(new[] { 0.0, 10, 20, 30 }));
        Assert.Equal(-10, ReputationRules.Withdraw(1000));
        Assert.Equal(5, ReputationRules.Ending(15, 0, 500000));
        Assert.Equal(1, CancellationRules.Chance(0));
        Assert.Equal(.6, CancellationRules.Chance(100));
        foreach (var cadence in Enum.GetValues<Cadence>())
        {
            var expiry = GameClock.Start.AddDays(8 * IssueSchedule.Days(cadence));
            Assert.False(CancellationRules.StrikeExpired(GameClock.Start, expiry, cadence, 8));
            Assert.True(CancellationRules.StrikeExpired(GameClock.Start, expiry.AddHours(1), cadence, 8));
        }
    }
}
