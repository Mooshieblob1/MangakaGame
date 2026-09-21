using MangakaSim;
using Xunit;

namespace MangakaSim.Tests;

public class GameClockTests
{
    [Fact]
    public void Starts_on_monday_1_april_1996_at_0800()
    {
        var clock = GameClock.AtStart();
        Assert.Equal(new DateTime(1996, 4, 1, 8, 0, 0), clock.Now);
        Assert.Equal(DayOfWeek.Monday, clock.DayOfWeek);
        Assert.Equal(8, clock.Hour);
    }

    [Fact]
    public void Advance_moves_one_hour()
    {
        var clock = GameClock.AtStart();
        clock.Advance();
        Assert.Equal(new DateTime(1996, 4, 1, 9, 0, 0), clock.Now);
    }

    [Fact]
    public void Advance_rolls_over_day()
    {
        var clock = new GameClock { Now = new DateTime(1996, 4, 1, 23, 0, 0) };
        clock.Advance();
        Assert.Equal(new DateTime(1996, 4, 2, 0, 0, 0), clock.Now);
        Assert.Equal(DayOfWeek.Tuesday, clock.DayOfWeek);
    }

    [Fact]
    public void Advance_rolls_over_february_in_leap_year()
    {
        var clock = new GameClock { Now = new DateTime(1996, 2, 28, 23, 0, 0) };
        clock.Advance();
        Assert.Equal(new DateTime(1996, 2, 29, 0, 0, 0), clock.Now);
        for (var i = 0; i < 24; i++) clock.Advance();
        Assert.Equal(new DateTime(1996, 3, 1, 0, 0, 0), clock.Now);
    }

    [Fact]
    public void Advance_rolls_over_february_in_non_leap_year()
    {
        var clock = new GameClock { Now = new DateTime(1997, 2, 28, 23, 0, 0) };
        clock.Advance();
        Assert.Equal(new DateTime(1997, 3, 1, 0, 0, 0), clock.Now);
    }

    [Fact]
    public void Advance_rolls_over_year()
    {
        var clock = new GameClock { Now = new DateTime(1996, 12, 31, 23, 0, 0) };
        clock.Advance();
        Assert.Equal(new DateTime(1997, 1, 1, 0, 0, 0), clock.Now);
    }

    [Fact]
    public void HoursUntil_counts_whole_hours_and_clamps_past_to_zero()
    {
        var clock = GameClock.AtStart();
        Assert.Equal(10, clock.HoursUntil(new DateTime(1996, 4, 1, 18, 0, 0)));
        Assert.Equal(0, clock.HoursUntil(new DateTime(1996, 3, 31, 0, 0, 0)));
    }
}
