using System.Text.Json.Nodes;
using MangakaSim;
using Xunit;

namespace MangakaSim.Tests;

public class SaveV3Tests
{
    /// <summary>A command-only year: get online, run a doujin series, hire two from the pool, fire one, underpay the other.</summary>
    private static GameState CommandYear(int seed = 5)
    {
        var state = GameState.NewGame(seed);
        state.Apply(new GetOnlineCommand());
        state.Apply(new CreateSeriesCommand("Petals", "romance", Cadence.Weekly, 19));
        state.Advance(state.Clock.HoursUntil(new DateTime(1996, 5, 2, 8, 0, 0)));
        state.Apply(new MovePremisesCommand("apartment"));
        state.Apply(new BuyAmenityCommand("kettle"));
        var first = HiringTests.Cheapest(state);
        state.Apply(new HireCommand(first.Id, first.AskingSalary));
        var second = HiringTests.Cheapest(state);
        state.Apply(new HireCommand(second.Id, second.AskingSalary));
        state.Apply(new SetPromotionCommand(state.People[2].Id, PromotionRules.WholeStudio));
        state.Advance(24 * 60);
        state.Apply(new FireCommand(state.People[1].Id));
        state.Apply(new SetSalaryCommand(state.People[1].Id, 100_000));
        state.Advance(24 * 300);
        return state;
    }

    private static readonly Lazy<GameState> Year = new(() => CommandYear());

    [Fact]
    public void A_year_with_staff_round_trips_and_continues_identically()
    {
        var state = Year.Value;
        Assert.Equal(2, state.People.Count);
        Assert.Single(state.FormerPeople);
        Assert.Contains(state.Ledger, l => l.Reason == "salary");
        Assert.Contains(state.Ledger, l => l.Reason == "severance");
        Assert.Contains(state.Ledger, l => l.Reason == "rent");
        Assert.Contains(state.Ledger, l => l.Reason == "upkeep");
        Assert.Contains(state.Ledger, l => l.Reason == "internet provider");
        Assert.True(state.Candidates.Count >= 4);
        var json = state.ToJson();
        var loaded = GameState.FromJson(json);
        Assert.Equal(json, loaded.ToJson());
        var copy = GameState.FromJson(json);
        copy.Advance(24 * 30);
        loaded.Advance(24 * 30);
        Assert.Equal(copy.ToJson(), loaded.ToJson());
    }

    [Fact]
    public void The_command_log_replays_the_staff_commands()
    {
        var original = CommandYear();
        var replay = GameState.NewGame(original.RngSeed);
        foreach (var entry in GameState.FromJson(original.ToJson()).CommandLog)
        {
            replay.Advance(replay.Clock.HoursUntil(entry.Time));
            replay.Apply(entry.Command);
        }
        replay.Advance(replay.Clock.HoursUntil(original.Clock.Now));
        Assert.Equal(original.ToJson(), replay.ToJson());
        Assert.Contains(replay.CommandLog, e => e.Command is HireCommand);
        Assert.Contains(replay.CommandLog, e => e.Command is FireCommand);
        Assert.Contains(replay.CommandLog, e => e.Command is MovePremisesCommand);
    }

    private static void AssertRejected(string field, Action<JsonNode> mutate)
    {
        var json = JsonNode.Parse(Year.Value.ToJson())!;
        mutate(json);
        var ex = Assert.Throws<InvalidDataException>(() => GameState.FromJson(json.ToJsonString()));
        Assert.Contains(field, ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    private static JsonNode Person(JsonNode root, int index) => root["People"]![index]!;

    [Fact]
    public void People_invariants()
    {
        AssertRejected("exactly one mangaka", j => Person(j, 0)["Role"] = "Assistant");
        AssertRejected("salary", j => Person(j, 0)["Salary"] = 5);
        AssertRejected("salary", j => Person(j, 1)["Salary"] = -1);
        AssertRejected("needs", j => Person(j, 1)["Needs"]!["Hunger"] = 101);
        AssertRejected("mood", j => Person(j, 1)["Happiness"] = -1);
        AssertRejected("mood", j => Person(j, 1)["Fatigue"] = 101);
        AssertRejected("allowed stages", j => Person(j, 1)["AllowedStages"] = new JsonArray());
        AssertRejected("allowed stages", j => Person(j, 0)["AllowedStages"] = new JsonArray(JsonValue.Create("Inks")));
        AssertRejected("staff counters", j => Person(j, 1)["BreaksToday"] = -1);
        AssertRejected("promotion target", j => Person(j, 1)["PromotionSeriesId"] = 999);
        AssertRejected("work counters", j => Person(j, 0)["RegularHoursToday"] = 30);
    }

    [Fact]
    public void Series_and_stage_invariants()
    {
        AssertRejected("series lead", j => j["Series"]![0]!["LeadId"] = Person(j, 1)["Id"]!.GetValue<int>()); // assistant cannot write the Name
        AssertRejected("series lead", j => j["Series"]![0]!["LeadId"] = 999);
        AssertRejected("hours by person", j =>
        {
            var hours = j["Series"]![0]!["Chapters"]![0]!["Stages"]![0]!["HoursByPerson"]!.AsObject();
            var key = hours.First().Key;
            hours[key] = hours[key]!.GetValue<double>() + 1;
        });
        AssertRejected("hours by person", j => j["Series"]![0]!["Chapters"]![0]!["Stages"]![0]!["HoursByPerson"]!["999"] = 0);
        AssertRejected("manual assignee", j =>
        {
            var open = j["Series"]![0]!["Chapters"]!.AsArray().Last()!;
            open["Stages"]![4]!["ManualAssignee"] = 999;
        });
    }

    [Fact]
    public void Studio_candidate_and_ledger_invariants()
    {
        AssertRejected("studio", j => j["Studio"]!["PremisesId"] = "penthouse");
        AssertRejected("studio", j => j["Studio"]!["Amenities"] = new JsonArray(JsonValue.Create("kettle"), JsonValue.Create("kettle")));
        AssertRejected("studio", j => j["Studio"]!["MissedPayrolls"] = -1);
        AssertRejected("studio capacity", j =>
        {
            j["Studio"]!["PremisesId"] = "garage";
            var people = j["People"]!.AsArray();
            for (var i = 0; i < 3; i++)
            {
                var clone = people[1]!.DeepClone();
                clone["Id"] = 9000 + i;
                clone["Name"] = $"Extra {i}";
                clone["Queue"] = new JsonArray();
                clone["Pins"] = new JsonArray();
                clone["CurrentTask"] = null;
                clone["ManualOrder"] = null;
                people.Add(clone);
            }
            j["NextId"] = 9100;
        });
        AssertRejected("candidate details", j => j["Candidates"]![1]!["Name"] = j["Candidates"]![0]!["Name"]!.GetValue<string>());
        AssertRejected("candidate details", j => j["Candidates"]![0]!["AskingSalary"] = 0);
        AssertRejected("candidate details", j => j["Candidates"]![0]!["Skills"]!["Inks"] = 101);
        AssertRejected("ledger person ids", j =>
        {
            var salary = j["Ledger"]!.AsArray().First(l => l!["Reason"]!.GetValue<string>() == "salary")!;
            salary["PersonId"] = 999;
        });
        AssertRejected("staff month markers", j => j["LastPayrollMonth"] = "1996-05-02T00:00:00");
        AssertRejected("departures", j => j["Departures"]![0]!["LeftAt"] = "2099-01-01T00:00:00");
        AssertRejected("missing Studio", j => j.AsObject().Remove("Studio"));
    }
}
