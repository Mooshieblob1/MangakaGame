using MangakaSim;
using Xunit;

namespace MangakaSim.Tests;

public class NeedsTests
{
    /// <summary>A 30-page weekly is at risk from the start, so overtime happens every day.</summary>
    private static GameState LongDays()
    {
        var state = GameState.NewGame();
        state.Apply(new CreateSeriesCommand("Rush", "action", Cadence.Weekly, 30));
        state.Apply(new SetScheduleCommand(state.People[0].Id, 8, 20, new HashSet<DayOfWeek> { DayOfWeek.Sunday }));
        return state;
    }

    [Fact]
    public void A_ten_hour_day_takes_no_break()
    {
        var state = GameState.NewGame();
        state.Apply(new CreateSeriesCommand("Calm", "slice of life", Cadence.Monthly, 19));
        var aki = state.People[0];
        state.Advance(10);
        Assert.Equal(50, aki.Needs.Hunger, 6);
        Assert.Equal(40, aki.Needs.Thirst, 6);
        Assert.Equal(70, aki.Needs.Comfort, 6);
        Assert.Equal(0, aki.BreaksToday);
        Assert.Equal(10, aki.HoursWorkedToday);
        Assert.DoesNotContain(state.Events, e => e.Type == EventType.TookBreak);
    }

    [Fact]
    public void The_second_overtime_hour_of_a_twelve_hour_day_is_a_break()
    {
        var state = LongDays();
        var aki = state.People[0];
        state.Advance(12); // 08:00 -> 20:00, twelve regular hours
        Assert.Equal(28, aki.Needs.Thirst, 6);
        Assert.Equal(12, aki.HoursWorkedToday);
        state.Advance(1); // first overtime hour
        Assert.Equal(19, aki.Needs.Thirst, 6);
        Assert.Equal(13, aki.HoursWorkedToday);
        Assert.Equal(1, aki.OvertimeHoursToday);
        state.Advance(1); // second overtime hour: thirst is below 25, so it becomes a break
        Assert.True(aki.OnBreak);
        Assert.Equal(1, aki.BreaksToday);
        Assert.Equal(13, aki.HoursWorkedToday);        // no work this hour
        Assert.Equal(2, aki.OvertimeHoursToday);        // but the hour still counts against the cap
        Assert.Equal(19 + 20, aki.Needs.Thirst, 6);     // bare recovery without a kettle
        Assert.Equal(aki.Needs.Hunger, 100 - 12 * 5 - 8 + 10, 6);
        var took = Assert.Single(state.Events, e => e.Type == EventType.TookBreak);
        Assert.Equal(aki.Id, took.PersonId);
        Assert.Contains("thirst", took.Message);
        var name = state.Series[0].Chapters[0].StageWork(Stage.Name);
        Assert.Equal(13 * 1.6, name.HoursDone, 6);
    }

    [Fact]
    public void Needs_reset_on_the_first_working_hour_of_the_next_day()
    {
        var state = LongDays();
        var aki = state.People[0];
        state.Advance(14);
        Assert.True(aki.Needs.Min < 60);
        state.Advance(10); // 22:00 -> 08:00 next day, nothing worked overnight
        Assert.True(aki.Needs.Min < 60);
        state.Advance(1); // first working hour: reset then one hour of depletion
        Assert.Equal(95, aki.Needs.Hunger, 6);
        Assert.Equal(94, aki.Needs.Thirst, 6);
        Assert.Equal(0, aki.BreaksToday);
    }

    [Fact]
    public void Amenities_change_recovery_and_comfort_depletion()
    {
        var state = GameState.NewGame();
        state.Apply(new CreateSeriesCommand("Calm", "slice of life", Cadence.Monthly, 19));
        state.Studio.Amenities.Add("fridge");
        state.Studio.Amenities.Add("office-chairs");
        var aki = state.People[0];
        state.Advance(1);
        Assert.Equal(99, aki.Needs.Comfort, 6); // office chairs: comfort -1 per hour
        aki.Needs.Hunger = 20;
        state.Advance(1);
        Assert.True(aki.OnBreak);
        Assert.Equal(90, aki.Needs.Hunger, 6);   // fridge: +70
        Assert.Equal(1, aki.BreaksToday);
        state.Advance(1);
        Assert.False(aki.OnBreak);
        Assert.Equal(85, aki.Needs.Hunger, 6);
        Assert.Single(state.Events, e => e.Type == EventType.TookBreak);
    }

    [Fact]
    public void A_need_below_the_threshold_always_forces_a_break_before_it_can_reach_zero()
    {
        var state = GameState.NewGame();
        state.Apply(new CreateSeriesCommand("Calm", "slice of life", Cadence.Monthly, 19));
        var aki = state.People[0];
        state.Advance(1);
        aki.Needs = new Needs { Hunger = 100, Thirst = 3, Comfort = 100 };
        var hoursBefore = state.Series[0].Chapters[0].StageWork(Stage.Name).HoursDone;
        state.Advance(1);
        Assert.True(aki.OnBreak);
        Assert.Equal(23, aki.Needs.Thirst, 6);
        Assert.Equal(hoursBefore, state.Series[0].Chapters[0].StageWork(Stage.Name).HoursDone, 6);
        state.Advance(1); // still below 25: another break
        Assert.True(aki.OnBreak);
        Assert.Equal(43, aki.Needs.Thirst, 6);
        Assert.Equal(2, aki.BreaksToday);
        Assert.DoesNotContain(state.Events, e => e.Type == EventType.NeedCritical);
        Assert.Single(state.Events, e => e.Type == EventType.TookBreak);
    }

    [Fact]
    public void Fatigue_builds_under_overtime_and_lowers_quality_then_rest_restores_it()
    {
        var state = LongDays();
        var aki = state.People[0];
        state.Advance(24 * 6); // Monday to Sunday morning: six days of 12 regular hours, one overtime hour worked and one break
        Assert.Equal(6 * (2.0 * 1 + 1.0 * 4) - 6 * 2, aki.Fatigue, 6); // +6 a day, -2 recovery
        Assert.Equal(6, aki.RecentOvertime.Count);
        Assert.Equal(1, aki.RecentOvertime.Last());
        Assert.Equal(12, aki.RecentRegular.Last());
        Assert.Equal(1, aki.RecentBreaks.Last());
        var fatigueAfterWeek = aki.Fatigue;
        state.Advance(24); // Sunday off: -6, clamped at zero
        Assert.Equal(Math.Max(0, fatigueAfterWeek - 6), aki.Fatigue, 6);
        Assert.True(fatigueAfterWeek > 0);

        state.Advance(24 * 7); // the 30-page chapter needs about 98 person-hours: done in the second week
        var chapter = state.Series[0].Chapters.First(c => c.Status == ChapterStatus.Complete);
        Assert.True(chapter.Stages.Sum(w => w.Contribution) < 84, $"contributions {chapter.Stages.Sum(w => w.Contribution)}");

        state.Apply(new PauseSeriesCommand(state.Series[0].Id));
        state.Advance(24 * 14);
        Assert.Equal(0, aki.Fatigue);
        Assert.Equal(7, aki.RecentRegular.Count);
    }

    [Fact]
    public void Daily_recap_lists_moods_and_yen_spent()
    {
        var state = GameState.NewGame();
        state.Apply(new CreateSeriesCommand("Calm", "slice of life", Cadence.Monthly, 19));
        state.Apply(new GetOnlineCommand());
        state.Advance(10);
        var recap = state.Events.Last(e => e.Type == EventType.DailyRecap).Recap!;
        var mood = Assert.Single(recap.Moods);
        Assert.Equal("Aki", mood.Name);
        Assert.Equal(70, mood.Happiness, 6);
        Assert.Equal(0, mood.Breaks);
        Assert.Equal(120_000, recap.YenSpent);
        Assert.Equal(0, recap.YenEarned);
        GameState.FromJson(state.ToJson());
    }
}
