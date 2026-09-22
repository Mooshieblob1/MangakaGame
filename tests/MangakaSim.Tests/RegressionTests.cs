using System.Text.Json.Nodes;
using MangakaSim;
using Xunit;

namespace MangakaSim.Tests;

public class RegressionTests
{
    private static GameState Started()
    {
        var state = GameState.NewGame(42);
        state.Apply(new CreateSeriesCommand("Rush", "action", Cadence.Weekly, 19));
        return state;
    }

    public static IEnumerable<object[]> InvalidCommands()
    {
        yield return new object[] { new CreateSeriesCommand("Invalid", "action", (Cadence)999, 19) };
        yield return new object[] { new CreateSeriesCommand("Invalid", null!, Cadence.Weekly, 19) };
        yield return new object[] { new SetScheduleCommand(1, 9, 17, null!) };
        yield return new object[] { new SetScheduleCommand(1, 9, 17, new() { (DayOfWeek)99 }) };
        yield return new object[] { new SetScheduleCommand(1, 8, int.MaxValue, new()) };
        yield return new object[] { new PinStageCommand(1, 3, (Stage)99) };
        yield return new object[] { new SkipStageCommand(3, (Stage)99) };
        yield return new object[] { new ReorderQueueCommand(1, null!) };
    }

    [Theory]
    [MemberData(nameof(InvalidCommands))]
    public void Invalid_commands_fail_without_any_state_change(ICommand command)
    {
        var state = Started();
        var before = state.ToJson();
        Assert.Throws<InvalidCommandException>(() => state.Apply(command));
        Assert.Equal(before, state.ToJson());
    }

    [Fact]
    public void Null_command_is_rejected_without_changes()
    {
        var state = Started();
        var before = state.ToJson();
        Assert.Throws<InvalidCommandException>(() => state.Apply(null!));
        Assert.Equal(before, state.ToJson());
    }

    [Fact]
    public void Skipping_partial_work_resets_stage_hours_to_zero()
    {
        var state = Started();
        state.Advance(2);
        var chapter = state.Series[0].Chapters[0];
        state.Apply(new SkipStageCommand(chapter.Id, Stage.Name));
        Assert.Equal(0, chapter.StageWork(Stage.Name).HoursDone);
        Assert.Equal(StageStatus.Skipped, chapter.StageWork(Stage.Name).Status);
    }

    [Fact]
    public void Commands_recompute_risk_immediately()
    {
        var state = Started();
        Assert.True(state.Series[0].Chapters[0].IsAtRisk);
        state.Apply(new SetScheduleCommand(1, 8, 20, new() { DayOfWeek.Sunday }));
        Assert.False(state.Series[0].Chapters[0].IsAtRisk);
    }

    [Fact]
    public void Pins_survive_a_series_pause_and_resume()
    {
        var state = Started();
        var series = state.Series[0];
        var pin = new QueueRef(series.Chapters[0].Id, Stage.Tones);
        state.Apply(new PinStageCommand(1, pin.ChapterId, pin.Stage));
        state.Apply(new PauseSeriesCommand(series.Id));
        Assert.Empty(state.People[0].Queue);
        state.Apply(new ResumeSeriesCommand(series.Id));
        Assert.Equal(pin, state.People[0].Queue[0]);
    }

    [Fact]
    public void Caller_cannot_mutate_the_saved_command_log_via_command_collections()
    {
        var state = Started();
        var days = new HashSet<DayOfWeek> { DayOfWeek.Sunday };
        state.Apply(new SetScheduleCommand(1, 9, 17, days));
        var before = state.ToJson();
        days.Clear();
        Assert.Equal(before, state.ToJson());
    }

    [Fact]
    public void Daily_recap_does_not_include_last_nights_commands()
    {
        var state = Started();
        state.Advance(12);
        var chapter = state.Series[0].Chapters[0];
        foreach (var stage in StageOrder.All) state.Apply(new SkipStageCommand(chapter.Id, stage));
        state.Advance(24);
        var recap = state.Events.Last(e => e.Type == EventType.DailyRecap).Recap!;
        Assert.DoesNotContain(new ChapterRef(state.Series[0].Id, chapter.Number), recap.ChaptersCompleted);
    }

    [Theory]
    [InlineData("null")]
    [InlineData("[]")]
    [InlineData("{\"Version\":null}")]
    [InlineData("{\"Version\":\"1\"}")]
    [InlineData("{\"Version\":0}")]
    [InlineData("{\"Version\":1}")]
    public void Incomplete_or_unsupported_saves_fail_clearly(string json)
    {
        Assert.Throws<InvalidDataException>(() => GameState.FromJson(json));
    }

    [Theory]
    [InlineData("People")]
    [InlineData("Clock")]
    [InlineData("Series")]
    [InlineData("Settings")]
    [InlineData("Events")]
    [InlineData("Rng")]
    [InlineData("CommandLog")]
    [InlineData("Markets")]
    [InlineData("Trends")]
    [InlineData("Ledger")]
    public void Saves_with_null_required_state_are_rejected(string property)
    {
        var json = JsonNode.Parse(Started().ToJson())!;
        json[property] = null;
        Assert.Throws<InvalidDataException>(() => GameState.FromJson(json.ToJsonString()));
    }

    [Fact]
    public void Changing_schedule_does_not_grant_more_than_the_daily_overtime_cap()
    {
        var state = Started();
        state.Apply(new SetPagesPerChapterCommand(2, 200));
        state.Series[0].Chapters[0].DueDate = state.Clock.Now;
        state.Advance(12);
        state.Apply(new SetScheduleCommand(1, 8, 20, new() { DayOfWeek.Sunday }));
        state.Advance(2);
        Assert.Equal(2, state.People[0].OvertimeHoursToday);
    }

    [Fact]
    public void Midnight_work_is_in_previous_days_recap_before_counters_reset()
    {
        var state = Started();
        state.Apply(new SetScheduleCommand(1, 20, 22, new()));
        state.Advance(16);
        var recap = Assert.Single(state.Events, e => e.Type == EventType.DailyRecap);
        Assert.Equal(new DateTime(1996, 4, 2), recap.Time);
        Assert.Equal(4, recap.Recap!.HoursPerPerson[0].Hours);
        Assert.Equal(2, recap.Recap.HoursPerPerson[0].OvertimeHours);
        Assert.True(state.Events.IndexOf(recap) < state.Events.FindIndex(e => e.Type == EventType.DayStarted));
        Assert.Equal(0, state.People[0].HoursWorkedToday);
    }

    [Fact]
    public void Saves_with_broken_recap_payload_are_rejected()
    {
        var state = Started();
        state.Advance(12);
        var json = JsonNode.Parse(state.ToJson())!;
        json["Events"]!.AsArray().Last()!["Recap"]!["HoursPerPerson"] = null;
        Assert.Throws<InvalidDataException>(() => GameState.FromJson(json.ToJsonString()));
    }
}
