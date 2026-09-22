using System.Text.Json.Nodes;
using MangakaSim;
using Xunit;
using static MangakaSim.Tests.IssueCloseTests;

namespace MangakaSim.Tests;

public class SaveV2Tests
{
    private const string Flowers = "hoshigaku-flowers";

    /// <summary>Two in-game years: get online, pitch, accept, serialize, run a second doujin series alongside.</summary>
    private static GameState TwoYears(int seed)
    {
        var state = GameState.NewGame(seed);
        state.Apply(new GetOnlineCommand());
        state.Apply(new CreateSeriesCommand("Petals", "romance", Cadence.Monthly, 19));
        state.Apply(new PitchSeriesCommand(state.Series[0].Id, Flowers));
        for (var i = 0; i < 24 * 60 && state.Series[0].Publishing == PublishingStatus.Pitching; i++) state.Advance(1);
        if (state.Series[0].Publishing == PublishingStatus.Offered) state.Apply(new AcceptOfferCommand(state.Series[0].Id));
        state.Advance(24 * 30);
        state.Apply(new CreateSeriesCommand("Side Dish", "comedy", Cadence.Monthly, 16));
        state.Advance(24 * 365 * 2 - state.Clock.HoursUntil(GameClock.Start));
        return state;
    }

    private static int SerializedSeed() =>
        EditorTests.FindSeed(s =>
        {
            var g = GameState.NewGame(s);
            g.Apply(new CreateSeriesCommand("Petals", "romance", Cadence.Monthly, 19));
            g.Apply(new PitchSeriesCommand(g.Series[0].Id, Flowers));
            for (var i = 0; i < 24 * 60 && g.Series[0].Publishing == PublishingStatus.Pitching; i++) g.Advance(1);
            return g;
        }, g => g.Series[0].Publishing == PublishingStatus.Offered);

    [Fact]
    public void Two_years_of_play_round_trip_and_continue_identically()
    {
        var state = TwoYears(SerializedSeed());
        var series = state.Series[0];
        Assert.Equal(PublishingStatus.Serialized, series.Publishing);
        Assert.True(series.ChaptersPublished >= 10, $"{series.ChaptersPublished} published");
        Assert.True(series.Volumes.Count >= 1);
        Assert.True(state.Series[1].Volumes.Count >= 1);
        Assert.Contains(state.Ledger, l => l.Reason == "chapter fee");
        Assert.Contains(state.Ledger, l => l.Reason == "royalties");
        Assert.Contains(state.Ledger, l => l.Reason == "doujin sales");
        Assert.Contains(state.Ledger, l => l.Reason == "internet");
        Assert.True(state.Money > 500_000, $"money {state.Money}");
        Assert.Contains(state.Events, e => e.Type == EventType.ConventionRecap);
        Assert.All(state.Markets, m => Assert.True(m.IssuesClosed >= 24));

        var json = state.ToJson();
        var loaded = GameState.FromJson(json);
        Assert.Equal(json, loaded.ToJson());
        state.Advance(24 * 90);
        loaded.Advance(24 * 90);
        Assert.Equal(state.ToJson(), loaded.ToJson());
        Assert.Equal(TwoYears(SerializedSeed()).ToJson(), json);
    }

    [Fact]
    public void Saved_command_log_replays_the_publishing_commands()
    {
        var original = TwoYears(SerializedSeed());
        var replay = GameState.NewGame(original.RngSeed);
        foreach (var entry in GameState.FromJson(original.ToJson()).CommandLog)
        {
            replay.Advance(replay.Clock.HoursUntil(entry.Time));
            replay.Apply(entry.Command);
        }
        replay.Advance(replay.Clock.HoursUntil(original.Clock.Now));
        Assert.Equal(original.ToJson(), replay.ToJson());
        Assert.Contains(replay.CommandLog, e => e.Command is PitchSeriesCommand);
        Assert.Contains(replay.CommandLog, e => e.Command is AcceptOfferCommand);
        Assert.Contains(replay.CommandLog, e => e.Command is GetOnlineCommand);
    }

    /// <summary>A small deterministic state that holds every publishing shape at once.</summary>
    private static GameState Mixed()
    {
        var state = SerializedAtJump(seed: 1);
        ForceComplete(state, state.Series[0].OpenChapter!);
        state.Advance(state.Clock.HoursUntil(state.MarketOf("tokiwa-jump").NextIssueClose)); // one publish, ranked
        state.Apply(new CreateSeriesCommand("Doujin", "romance", Cadence.Monthly, 19));
        var doujin = state.Series[1];
        for (var i = 0; i < 5; i++)
        {
            ForceComplete(state, doujin.OpenChapter!);
        }
        Assert.Single(doujin.Volumes);
        state.Apply(new CreateSeriesCommand("Offered", "comedy", Cadence.Monthly, 19));
        var offered = state.Series[2];
        offered.Publishing = PublishingStatus.Offered;
        offered.PendingOffer = new SerializationOffer { MagazineId = Flowers, FeePerPage = 7000, FirstIssueClose = state.Clock.Now.AddDays(60), ExpiresAt = state.Clock.Now.AddDays(60) };
        state.Apply(new CreateSeriesCommand("Pitching", "drama", Cadence.Monthly, 19));
        state.Apply(new PitchSeriesCommand(state.Series[3].Id, Flowers));
        state.Series[0].Strikes.Add(state.Clock.Now.AddDays(-7));
        state.Advance(2);
        GameState.FromJson(state.ToJson()); // the fixture itself is valid
        return state;
    }

    private static void AssertRejected(string field, Action<JsonNode> mutate)
    {
        var json = JsonNode.Parse(Mixed().ToJson())!;
        mutate(json);
        var ex = Assert.Throws<InvalidDataException>(() => GameState.FromJson(json.ToJsonString()));
        Assert.Contains(field, ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    private static JsonNode Series(JsonNode root, int index) => root["Series"]![index]!;
    private static JsonNode Chapter(JsonNode root, int series, int index) => Series(root, series)["Chapters"]![index]!;
    private static JsonNode Market(JsonNode root, int index) => root["Markets"]![index]!;

    [Fact]
    public void Money_must_equal_the_ledger_sum() => AssertRejected("money", j => j["Money"] = 1);

    [Fact]
    public void Ledger_window_and_entries_are_checked()
    {
        AssertRejected("ledger window", j => j["LedgerWindowStart"] = 99);
        AssertRejected("ledger", j => j["Ledger"]![0]!["SeriesId"] = 999);
    }

    [Fact]
    public void Markets_must_match_the_catalog()
    {
        AssertRejected("markets", j => j["Markets"]!.AsArray().RemoveAt(0));
        AssertRejected("markets", j => Market(j, 0)["MagazineId"] = "tokiwa-square");
        AssertRejected("next issue close", j => Market(j, 0)["NextIssueClose"] = "1996-04-04T18:00:00");
        AssertRejected("next issue close", j => Market(j, 0)["NextIssueClose"] = "1996-04-18T18:30:00");
        AssertRejected("filler id", j => Market(j, 0)["Fillers"]![1]!["Id"] = Market(j, 0)["Fillers"]![0]!["Id"]!.GetValue<int>());
        AssertRejected("filler details", j => Market(j, 0)["Fillers"]![0]!["Popularity"] = 101);
        AssertRejected("filler details", j => Market(j, 0)["Fillers"]![0]!["Genre"] = "isekai");
        AssertRejected("iconic fillers", j => { foreach (var f in Market(j, 0)["Fillers"]!.AsArray()) f!["IsIconic"] = true; });
        AssertRejected("rank entry", j => Market(j, 0)["LastRanking"]![0]!["Rank"] = 5);
        AssertRejected("rank entry target", j => Market(j, 0)["LastRanking"]![0]!["FillerId"] = 999);
        AssertRejected("rank entry target", j =>
        {
            var row = Market(j, 0)["LastRanking"]!.AsArray().First(r => r!["SeriesId"] is not null)!;
            row["SeriesId"] = 999;
        });
    }

    [Fact]
    public void Trends_must_match_the_catalog_and_stay_in_range()
    {
        AssertRejected("trends", j => j["Trends"]!.AsArray().RemoveAt(0));
        AssertRejected("trend values", j => j["Trends"]![0]!["Noise"] = 0.2);
        AssertRejected("trend values", j => j["Trends"]![0]!["Boom"] = 1.5);
        AssertRejected("trend values", j => j["Trends"]![0]!["PlayerInfluence"] = 0.6);
        AssertRejected("boom fields", j => j["Trends"]![0]!["BoomEndsAt"] = "1997-01-01T00:00:00");
        AssertRejected("trend update month", j => j["LastTrendUpdateMonth"] = "1996-04-02T00:00:00");
    }

    [Fact]
    public void Reputations_and_series_stats_stay_in_range()
    {
        AssertRejected("studio track record", j => j["StudioTrackRecord"] = 101);
        AssertRejected("person reputation", j => j["People"]![0]!["Reputation"] = -1);
        AssertRejected("series publishing details", j => Series(j, 0)["Fanbase"] = -5);
        AssertRejected("series publishing details", j => Series(j, 0)["CulturalImpact"] = 101);
        AssertRejected("iconic series impact", j => Series(j, 0)["IsIconic"] = true);
        AssertRejected("strikes in the future", j => Series(j, 0)["Strikes"]![0] = "2001-01-01T00:00:00");
        AssertRejected("cooldown magazine ids", j => Series(j, 0)["PitchCooldowns"] = JsonNode.Parse("{\"nowhere\":\"1997-01-01T00:00:00\"}"));
    }

    [Fact]
    public void Publishing_status_must_agree_with_contract_and_offer()
    {
        AssertRejected("serialized series must hold a contract", j => Series(j, 0)["Contract"] = null);
        AssertRejected("serialized series must hold a contract", j => Series(j, 0)["PendingOffer"] = Series(j, 2)["PendingOffer"]!.DeepClone());
        AssertRejected("offered series must hold an offer", j => Series(j, 2)["PendingOffer"] = null);
        AssertRejected("pitching series must have a pitched one-shot", j => Chapter(j, 3, 0)["IsOneShot"] = false);
        AssertRejected("unpublished series must hold no contract", j => Series(j, 1)["Contract"] = Series(j, 0)["Contract"]!.DeepClone());
        AssertRejected("ended series must be unpublished", j =>
        {
            Series(j, 2)["Status"] = "Ended"; // an Offered series that somehow ended
            j["People"]![0]!["Queue"] = new JsonArray();
            j["People"]![0]!["Pins"] = new JsonArray();
            j["People"]![0]!["CurrentTask"] = null;
        });
        AssertRejected("contract", j => Series(j, 0)["Contract"]!["MagazineId"] = "nowhere");
        AssertRejected("contract", j => Series(j, 0)["Contract"]!["ChaptersPublished"] = 5);
        AssertRejected("pending offer", j => Series(j, 2)["PendingOffer"]!["ExpiresAt"] = "1990-01-01T00:00:00");
    }

    [Fact]
    public void Chapter_editor_and_quality_invariants()
    {
        AssertRejected("editor status on a doujin chapter", j => Chapter(j, 1, 0)["Editor"] = "Approved");
        AssertRejected("chapter quality", j => Chapter(j, 0, 0)["Quality"] = null);
        AssertRejected("chapter quality", j => Chapter(j, 0, 1)["Quality"] = 50);
        AssertRejected("awaiting review state", j =>
        {
            var published = Chapter(j, 0, 0); // pencils are done, so it cannot be waiting on the name
            published["Editor"] = "AwaitingReview";
            published["EditorDecisionAt"] = "1996-04-05T18:00:00";
        });
        AssertRejected("pitched one-shot", j => Chapter(j, 3, 0)["PitchMagazineId"] = "nowhere"); // the pitching series loses its valid one-shot
        AssertRejected("published chapter must be complete", j => Chapter(j, 0, 1)["PublishedAt"] = "1996-04-04T18:00:00");
        AssertRejected("stage quality data", j => Chapter(j, 0, 0)["Stages"]![0]!["OvertimeHours"] = -1);
    }

    [Fact]
    public void Volume_invariants()
    {
        AssertRejected("volume chapter range", j => Series(j, 1)["Volumes"]![0]!["LastChapter"] = 99);
        AssertRejected("volume chapter range", j => Series(j, 1)["Volumes"]![0]!["Number"] = 2);
        AssertRejected("volume chapter range", j =>
        {
            var volumes = Series(j, 1)["Volumes"]!.AsArray();
            var copy = volumes[0]!.DeepClone();
            copy["Number"] = 2;
            copy["Id"] = 9999;
            volumes.Add(copy); // overlaps chapters 1-5
        });
        AssertRejected("volume sales data", j => Series(j, 1)["Volumes"]![0]!["CopiesSold"] = -1);
        AssertRejected("volume sales data", j => Series(j, 1)["Volumes"]![0]!["WeeksOnSale"] = 9);
        AssertRejected("volume sales data", j => Series(j, 1)["Volumes"]![0]!["ReleaseDate"] = "2001-01-01T00:00:00"); // released before its date
        AssertRejected("unique id", j => Series(j, 1)["Volumes"]![0]!["Id"] = Series(j, 0)["Id"]!.GetValue<int>());
    }

    [Fact]
    public void Doujin_month_counters_and_internet_flag_are_checked()
    {
        AssertRejected("doujin month counters", j => j["DoujinCopiesThisMonth"] = -1);
        AssertRejected("missing HasInternet", j => j.AsObject().Remove("HasInternet"));
    }
}
