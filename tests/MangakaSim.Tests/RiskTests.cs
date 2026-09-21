using MangakaSim;
using Xunit;

namespace MangakaSim.Tests;

public class RiskTests
{
    private static GameState WithSeries(Cadence cadence, bool overtimeAllowed = true)
    {
        var state = GameState.NewGame();
        state.People[0].OvertimeAllowed = overtimeAllowed;
        state.CreateSeries("S", "g", cadence, 19);
        state.RunPlanner();
        return state;
    }

    [Fact]
    public void RegularHoursBefore_counts_scheduled_hours_only()
    {
        var state = GameState.NewGame();
        var person = state.People[0];
        var monday8 = new DateTime(1996, 4, 1, 8, 0, 0);
        Assert.Equal(60, state.RegularHoursBefore(person, monday8, monday8.AddDays(7)));
        Assert.Equal(10, state.RegularHoursBefore(person, monday8, monday8.AddHours(12)));
        Assert.Equal(0, state.RegularHoursBefore(person, monday8, monday8));
        Assert.Equal(0, state.RegularHoursBefore(person, monday8, monday8.AddDays(-1)));
    }

    [Fact]
    public void RemainingPersonHours_uses_assignee_multiplier()
    {
        var state = WithSeries(Cadence.Weekly);
        var chapter = state.Series[0].Chapters[0];
        Assert.Equal(98.8 / 1.6, state.RemainingPersonHours(chapter), 6);
        chapter.StageWork(Stage.Name).HoursDone = 22.8;
        chapter.StageWork(Stage.Name).Status = StageStatus.Complete;
        Assert.Equal(76.0 / 1.6, state.RemainingPersonHours(chapter), 6);
        chapter.StageWork(Stage.Pencils).Status = StageStatus.Skipped;
        Assert.Equal(47.5 / 1.6, state.RemainingPersonHours(chapter), 6);
    }

    [Fact]
    public void Risk_event_fires_once_per_transition_back_to_at_risk()
    {
        var state = WithSeries(Cadence.Weekly);
        state.Advance(1);
        var chapter = state.Series[0].Chapters[0];
        Assert.True(chapter.IsAtRisk);
        var ev = Assert.Single(state.Events, e => e.Type == EventType.ChapterAtRisk);
        Assert.Equal(1, ev.ChapterNumber);
        state.Advance(9); // Still at risk throughout the regular workday.
        Assert.Single(state.Events, e => e.Type == EventType.ChapterAtRisk);
        state.Advance(2); // Overtime brings the chapter back within its time budget.
        Assert.False(chapter.IsAtRisk);
        state.Advance(15); // Finishing Name uses a whole tick, making the chapter at risk again.
        Assert.True(chapter.IsAtRisk);
        Assert.Equal(2, state.Events.Count(e => e.Type == EventType.ChapterAtRisk));
    }

    [Fact]
    public void Monthly_chapter_is_not_at_risk()
    {
        var state = WithSeries(Cadence.Monthly);
        state.Advance(48);
        Assert.False(state.Series[0].Chapters[0].IsAtRisk);
        Assert.DoesNotContain(state.Events, e => e.Type == EventType.ChapterAtRisk);
    }

    [Fact]
    public void Past_due_chapter_is_at_risk()
    {
        var state = WithSeries(Cadence.Monthly);
        state.Series[0].Chapters[0].DueDate = new DateTime(1996, 3, 1, 8, 0, 0);
        state.Advance(1);
        Assert.True(state.Series[0].Chapters[0].IsAtRisk);
    }

    [Fact]
    public void Overtime_is_worked_when_allowed_and_at_risk_up_to_cap()
    {
        var state = WithSeries(Cadence.Weekly);
        state.Advance(12); // 08:00 -> 20:00
        var person = state.People[0];
        Assert.Equal(12, person.HoursWorkedToday);
        Assert.Equal(2, person.OvertimeHoursToday);
        Assert.Equal(19.2, state.Series[0].Chapters[0].StageWork(Stage.Name).HoursDone, 6);
        state.Advance(1); // 21:00, past the cap
        Assert.Equal(12, person.HoursWorkedToday);
        Assert.Equal(19.2, state.Series[0].Chapters[0].StageWork(Stage.Name).HoursDone, 6);
    }

    [Fact]
    public void No_overtime_when_not_allowed()
    {
        var state = WithSeries(Cadence.Weekly, overtimeAllowed: false);
        state.Advance(12);
        Assert.Equal(10, state.People[0].HoursWorkedToday);
        Assert.Equal(0, state.People[0].OvertimeHoursToday);
    }

    [Fact]
    public void No_overtime_when_not_at_risk()
    {
        var state = WithSeries(Cadence.Monthly);
        state.Advance(12);
        Assert.Equal(10, state.People[0].HoursWorkedToday);
        Assert.Equal(0, state.People[0].OvertimeHoursToday);
    }

    [Fact]
    public void Paused_series_chapters_are_not_evaluated()
    {
        var state = WithSeries(Cadence.Weekly);
        state.Series[0].Status = SeriesStatus.Paused;
        state.RunPlanner();
        state.Advance(1);
        Assert.False(state.Series[0].Chapters[0].IsAtRisk);
    }
}
