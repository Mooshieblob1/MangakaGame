using System.Text.Json.Nodes;
using Xunit;

namespace MangakaSim.Tests;

public class PublishingSerializationTests
{
    [Theory]
    [InlineData("Series", "LifetimeHoursByPerson")]
    [InlineData("Series", "NextChapterNumber")]
    [InlineData("People", "Reputation")]
    [InlineData("Trends", "BoomFloorChosen")]
    [InlineData("Markets", "RetiredFillers")]
    public void Nested_v2_fields_cannot_silently_default(string collection, string field)
    {
        var json = JsonNode.Parse(SimulationFixture.Serialized().ToJson())!;
        json[collection]![0]!.AsObject().Remove(field);
        Assert.Throws<InvalidDataException>(() => GameState.FromJson(json.ToJsonString()));
    }
    [Fact]
    public void Captured_version_one_is_explicitly_unsupported()
    {
        var json = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", "v1-minimal.json"));
        Assert.Contains("version 1 is not supported", Assert.Throws<InvalidDataException>(() => GameState.FromJson(json)).Message);
    }

    [Theory]
    [InlineData("Money")] [InlineData("Ledger")] [InlineData("StudioTrackRecord")]
    [InlineData("Markets")] [InlineData("Trends")] [InlineData("HasInternet")]
    [InlineData("LastTrendUpdateMonth")] [InlineData("LastSalesAt")]
    [InlineData("DoujinCopiesThisMonth")] [InlineData("DoujinFansThisMonth")]
    public void Every_new_root_field_is_required(string field)
    {
        var json = JsonNode.Parse(GameState.NewGame().ToJson())!.AsObject();
        json.Remove(field);
        Assert.Throws<InvalidDataException>(() => GameState.FromJson(json.ToJsonString()));
    }

    [Theory]
    [InlineData("money")] [InlineData("calendar")] [InlineData("filler-id")] [InlineData("rank-target")]
    [InlineData("contract")] [InlineData("chapter-contract")] [InlineData("quality")] [InlineData("editor")]
    [InlineData("volume-members")] [InlineData("volume-mean")] [InlineData("volume-weeks")]
    [InlineData("trend")] [InlineData("reputation")] [InlineData("activity-date")]
    [InlineData("next-id")] [InlineData("chapter-number")] [InlineData("contributor")]
    [InlineData("frozen-quality")] [InlineData("publication-slot")] [InlineData("lifetime-hours")]
    [InlineData("overflow-hours")]
    public void Corrupt_dynamic_state_is_rejected_without_repair(string mutation)
    {
        var json = JsonNode.Parse(SimulationFixture.EightPublished().ToJson())!;
        var s = json["Series"]![0]!;
        var chapters = s["Chapters"]!.AsArray();
        var published = chapters.First(c => c!["PublishedAt"] is not null)!;
        switch (mutation)
        {
            case "money": json["Money"] = 1; break;
            case "calendar": json["Markets"]![0]!["NextIssueClose"] = GameClock.Start; break;
            case "filler-id": json["Markets"]![0]!["Fillers"]![0]!["Id"] = 1; break;
            case "rank-target": json["Markets"]![0]!["LastRanking"]![0]!["FillerId"] = -7; break;
            case "contract": s["Contract"] = null; break;
            case "chapter-contract": published["PublishedContractId"] = -1; break;
            case "quality": published["Quality"] = null; break;
            case "editor": published["Editor"] = "AwaitingReview"; break;
            case "volume-members": s["Volumes"]![0]!["ChapterIds"]![0] = published["Id"]!.GetValue<int>(); break;
            case "volume-mean": s["Volumes"]![0]!["AverageQuality"] = 0; break;
            case "volume-weeks": s["Volumes"]![0]!["WeeksOnSale"] = 53; break;
            case "trend": json["Trends"]![0]!["Noise"] = .16; break;
            case "reputation": json["People"]![0]!["Reputation"] = 101; break;
            case "activity-date": json["Events"]![0]!["ActivityDate"] = GameClock.Start.AddDays(-3); break;
            case "next-id": json["NextId"] = 1; break;
            case "chapter-number": s["NextChapterNumber"] = 1; break;
            case "contributor": published["Stages"]![0]!["HoursByPerson"]!["99999"] = 3; break;
            case "frozen-quality": published["Quality"] = 1; break;
            case "publication-slot": chapters.Last()!["DueDate"] = GameClock.Start; break;
            case "lifetime-hours": s["LifetimeHoursByPerson"] = new JsonObject(); break;
            case "overflow-hours":
                published["Stages"]![0]!["HoursByPerson"]!["1"] = long.MaxValue;
                published["Stages"]![1]!["HoursByPerson"]!["1"] = long.MaxValue;
                break;
        }
        Assert.Throws<InvalidDataException>(() => GameState.FromJson(json.ToJsonString()));
    }
}
