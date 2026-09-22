using MangakaSim;
using Xunit;

namespace MangakaSim.Tests;

public class GameStateTickTests
{
    [Fact]
    public void NewGame_has_prodigy_mangaka_and_default_settings()
    {
        var state = GameState.NewGame(seed: 5);
        Assert.Equal(2, state.Version);
        Assert.Equal(GameClock.Start, state.Clock.Now);
        Assert.Equal(5, state.RngSeed);
        var person = Assert.Single(state.People);
        Assert.All(StageOrder.All, s => Assert.Equal(80, person.Skill(s)));
        Assert.Equal(8, person.Schedule.WorkStartHour);
        Assert.Equal(18, person.Schedule.WorkEndHour);
        Assert.Equal(new HashSet<DayOfWeek> { DayOfWeek.Sunday }, person.Schedule.DaysOff);
        Assert.True(person.OvertimeAllowed);
        Assert.Empty(state.Series);
        Assert.True(state.Settings.AutoPause[EventType.DailyRecap]);
        Assert.Equal(10, person.Reputation);
        Assert.Equal(500_000, state.Money);
        Assert.Empty(state.Ledger);
        Assert.Equal(0, state.StudioTrackRecord);
        Assert.False(state.HasInternet);
        Assert.Equal(new DateTime(1996, 4, 1), state.LastTrendUpdateMonth);
    }

    [Fact]
    public void Advance_negative_throws_and_zero_is_noop()
    {
        var state = GameState.NewGame();
        Assert.Throws<ArgumentOutOfRangeException>(() => state.Advance(-1));
        state.Advance(0);
        Assert.Equal(GameClock.Start, state.Clock.Now);
        Assert.Empty(state.Events);
    }

    [Fact]
    public void Advance_moves_clock_by_hours()
    {
        var state = GameState.NewGame();
        state.Advance(30);
        Assert.Equal(new DateTime(1996, 4, 2, 14, 0, 0), state.Clock.Now);
    }

    [Fact]
    public void DayStarted_fires_at_hour_zero_with_new_date()
    {
        var state = GameState.NewGame();
        state.Advance(16); // 08:00 -> 00:00 next day
        var ev = Assert.Single(state.Events, e => e.Type == EventType.DayStarted);
        Assert.Equal(new DateTime(1996, 4, 2, 0, 0, 0), ev.Time);
    }

    [Fact]
    public void WeekStarted_fires_on_monday_hour_zero_only()
    {
        var state = GameState.NewGame();
        state.Advance(16 + 24 * 6); // Monday 08:00 -> next Monday 00:00
        var weeks = state.Events.Where(e => e.Type == EventType.WeekStarted).ToList();
        var week = Assert.Single(weeks);
        Assert.Equal(new DateTime(1996, 4, 8, 0, 0, 0), week.Time);
        Assert.Equal(7, state.Events.Count(e => e.Type == EventType.DayStarted));
    }

    [Fact]
    public void DayStarted_resets_daily_counters_and_manual_order()
    {
        var state = GameState.NewGame();
        var person = state.People[0];
        person.HoursWorkedToday = 5;
        person.OvertimeHoursToday = 1;
        person.ManualOrder = new List<QueueRef>();
        state.RecapFiredToday = true;
        state.Advance(16);
        Assert.Equal(0, person.HoursWorkedToday);
        Assert.Equal(0, person.OvertimeHoursToday);
        Assert.Null(person.ManualOrder);
        Assert.False(state.RecapFiredToday);
    }

    [Fact]
    public void TickStart_is_the_hour_just_elapsed()
    {
        var state = GameState.NewGame();
        state.Advance(1);
        Assert.Equal(new DateTime(1996, 4, 1, 8, 0, 0), state.TickStart);
    }

    [Fact]
    public void AllocateId_is_sequential()
    {
        var state = GameState.NewGame();
        var first = state.AllocateId();
        Assert.Equal(first + 1, state.AllocateId());
    }
}
