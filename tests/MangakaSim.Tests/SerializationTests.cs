using MangakaSim;
using Xunit;

namespace MangakaSim.Tests;

public class SerializationTests
{
    private static GameState Played(int seed = 5)
    {
        var state = GameState.NewGame(seed);
        state.Apply(new CreateSeriesCommand("Rush", "action", Cadence.Weekly, 19));
        state.Apply(new CreateSeriesCommand("Calm", "slice of life", Cadence.Monthly, 24));
        state.Advance(30);
        var person = state.People[0];
        var calm = state.Series[1].Chapters[0];
        state.Apply(new PinStageCommand(person.Id, calm.Id, Stage.Name));
        state.Advance(40);
        state.Apply(new SkipStageCommand(calm.Id, Stage.Pencils));
        state.Rng.NextUInt64();
        state.Rng.NextUInt64();
        return state;
    }

    [Fact]
    public void Round_trip_produces_identical_json()
    {
        var state = Played();
        var json = state.ToJson();
        var loaded = GameState.FromJson(json);
        Assert.Equal(json, loaded.ToJson());
    }

    [Fact]
    public void Round_trip_preserves_key_fields()
    {
        var state = Played();
        var loaded = GameState.FromJson(state.ToJson());

        Assert.Equal(state.Clock.Now, loaded.Clock.Now);
        Assert.Equal(state.NextId, loaded.NextId);
        Assert.Equal(state.RngSeed, loaded.RngSeed);
        Assert.Equal(state.Rng.State, loaded.Rng.State);
        Assert.Equal(state.Rng.NextUInt64(), loaded.Rng.NextUInt64());
        Assert.Equal(state.Events.Count, loaded.Events.Count);
        Assert.Equal(state.RecapWindowStart, loaded.RecapWindowStart);
        Assert.Equal(state.People[0].CurrentTask, loaded.People[0].CurrentTask);
        Assert.Equal(state.People[0].Pins, loaded.People[0].Pins);
        Assert.Equal(state.People[0].Skills, loaded.People[0].Skills);
        Assert.Equal(state.People[0].Schedule.DaysOff, loaded.People[0].Schedule.DaysOff);
        Assert.Equal(state.Settings.AutoPause, loaded.Settings.AutoPause);
        Assert.Equal(StageStatus.Skipped, loaded.Series[1].Chapters[0].StageWork(Stage.Pencils).Status);

        Assert.Equal(4, loaded.CommandLog.Count);
        Assert.IsType<CreateSeriesCommand>(loaded.CommandLog[0].Command);
        Assert.IsType<PinStageCommand>(loaded.CommandLog[2].Command);
        Assert.Equal(state.CommandLog, loaded.CommandLog);
    }

    [Fact]
    public void Continuing_after_load_matches_continuing_without_load()
    {
        var original = Played();
        var loaded = GameState.FromJson(original.ToJson());

        original.Advance(24 * 30);
        loaded.Advance(24 * 30);

        Assert.Equal(original.ToJson(), loaded.ToJson());
    }

    [Fact]
    public void Same_seed_and_same_commands_at_same_times_are_deterministic()
    {
        Assert.Equal(Played(11).ToJson(), Played(11).ToJson());
    }

    [Fact]
    public void Saved_command_log_replays_actions_at_their_original_times()
    {
        var original = GameState.NewGame(11);
        original.Apply(new CreateSeriesCommand("Rush", "action", Cadence.Weekly, 19));
        original.Advance(30);
        original.Apply(new SetScheduleCommand(1, 9, 17, new() { DayOfWeek.Sunday }));
        original.Advance(40);
        original.Apply(new SkipStageCommand(3, Stage.Tones));
        original.Advance(100);

        var replay = GameState.NewGame(original.RngSeed);
        var saved = GameState.FromJson(original.ToJson());
        foreach (var entry in saved.CommandLog)
        {
            replay.Advance(replay.Clock.HoursUntil(entry.Time));
            replay.Apply(entry.Command);
        }
        replay.Advance(replay.Clock.HoursUntil(original.Clock.Now));
        Assert.Equal(original.ToJson(), replay.ToJson());
    }

    [Fact]
    public void Json_contains_version_and_string_enums()
    {
        var json = Played().ToJson();
        Assert.Contains("\"Version\": 3", json);
        Assert.Contains("\"Weekly\"", json);
        Assert.Contains("\"type\": \"CreateSeries\"", json);
    }

    [Fact]
    public void Loading_newer_version_throws_clear_error()
    {
        var json = Played().ToJson().Replace("\"Version\": 3", "\"Version\": 4");
        var ex = Assert.Throws<InvalidDataException>(() => GameState.FromJson(json));
        Assert.Contains("version 4", ex.Message);
        Assert.Contains("newer", ex.Message);
    }

    [Fact]
    public void Loading_older_saves_is_rejected_as_unsupported()
    {
        foreach (var old in new[] { 1, 2 })
        {
            var json = Played().ToJson().Replace("\"Version\": 3", $"\"Version\": {old}");
            var ex = Assert.Throws<InvalidDataException>(() => GameState.FromJson(json));
            Assert.Contains("not supported", ex.Message);
        }
    }

    [Fact]
    public void Loading_version_1_save_is_rejected_as_unsupported()
    {
        var json = Played().ToJson().Replace("\"Version\": 3", "\"Version\": 1");
        var ex = Assert.Throws<InvalidDataException>(() => GameState.FromJson(json));
        Assert.Contains("not supported", ex.Message);
    }

    [Fact]
    public void Loading_json_without_version_throws()
    {
        Assert.Throws<InvalidDataException>(() => GameState.FromJson("{}"));
    }
}
