using MangakaSim;
using Xunit;

namespace MangakaSim.Tests;

public class WorkTests
{
    // Monthly series is never at risk for the prodigy, so no overtime interferes.
    private static GameState Monthly()
    {
        var state = GameState.NewGame();
        state.CreateSeries("Calm", "slice of life", Cadence.Monthly, 19);
        state.RunPlanner();
        return state;
    }

    private static GameState WeeklyNoOvertime()
    {
        var state = GameState.NewGame();
        state.People[0].OvertimeAllowed = false;
        state.CreateSeries("Rush", "action", Cadence.Weekly, 19);
        state.RunPlanner();
        return state;
    }

    [Fact]
    public void First_working_hour_starts_stage_and_chapter_and_accrues_multiplier()
    {
        var state = Monthly();
        state.Advance(1);
        var chapter = state.Series[0].Chapters[0];
        var name = chapter.StageWork(Stage.Name);
        Assert.Equal(1.6, name.HoursDone, 6);
        Assert.Equal(StageStatus.InProgress, name.Status);
        Assert.Equal(ChapterStatus.InProgress, chapter.Status);
        var started = Assert.Single(state.Events, e => e.Type == EventType.StageStarted);
        Assert.Equal(Stage.Name, started.Stage);
        Assert.Equal(1, started.ChapterNumber);
        Assert.Equal(state.People[0].Id, started.PersonId);
        Assert.Equal(1, state.People[0].HoursWorkedToday);
    }

    [Fact]
    public void Hours_accrue_only_in_regular_hours_when_not_at_risk()
    {
        var state = Monthly();
        state.Advance(10); // 08:00 -> 18:00
        var name = state.Series[0].Chapters[0].StageWork(Stage.Name);
        Assert.Equal(16.0, name.HoursDone, 6);
        state.Advance(14); // 18:00 -> 08:00 next day, nothing worked overnight
        Assert.Equal(16.0, name.HoursDone, 6);
        Assert.Equal(0, state.People[0].HoursWorkedToday);
    }

    [Fact]
    public void Nothing_accrues_on_a_day_off()
    {
        var state = Monthly();
        state.Advance(5 * 24); // Saturday 08:00
        var before = state.Series[0].Chapters[0].Stages.Sum(s => s.HoursDone);
        state.Advance(24);     // Saturday worked, now Sunday 08:00
        var afterSaturday = state.Series[0].Chapters[0].Stages.Sum(s => s.HoursDone);
        Assert.True(afterSaturday > before);
        state.Advance(24);     // Sunday: day off
        Assert.Equal(afterSaturday, state.Series[0].Chapters[0].Stages.Sum(s => s.HoursDone), 6);
    }

    [Fact]
    public void Stage_completes_clamped_and_next_stage_becomes_current()
    {
        var state = Monthly();
        // Name needs 22.8h at 1.6/h: 15 working hours. Monday 10h + Tuesday 5h.
        state.Advance(24 + 5); // Tuesday 13:00
        var chapter = state.Series[0].Chapters[0];
        var name = chapter.StageWork(Stage.Name);
        Assert.Equal(StageStatus.Complete, name.Status);
        Assert.Equal(22.8, name.HoursDone, 6);
        Assert.Single(state.Events, e => e.Type == EventType.StageCompleted);
        Assert.Equal(new QueueRef(chapter.Id, Stage.Pencils), state.People[0].CurrentTask);
        Assert.Equal(StageStatus.NotStarted, chapter.StageWork(Stage.Pencils).Status);
        Assert.DoesNotContain(new QueueRef(chapter.Id, Stage.Name), state.People[0].Queue);
    }

    [Fact]
    public void Stage_starts_only_when_earlier_stages_done()
    {
        var state = Monthly();
        state.Advance(24 + 6); // one hour after Name completes
        var chapter = state.Series[0].Chapters[0];
        Assert.Equal(StageStatus.InProgress, chapter.StageWork(Stage.Pencils).Status);
        Assert.Equal(1.6, chapter.StageWork(Stage.Pencils).HoursDone, 6);
        Assert.Equal(StageStatus.NotStarted, chapter.StageWork(Stage.Inks).Status);
        Assert.Equal(2, state.Events.Count(e => e.Type == EventType.StageStarted));
    }

    [Fact]
    public void Chapter_completes_on_time_and_next_chapter_starts_same_hour()
    {
        var state = Monthly();
        // Stages need 15 + 18 + 12 + 12 + 6 = 63 working hours: Mon-Sat 60h, then Mon 08-11.
        state.Advance(7 * 24 + 3); // Monday 8 April 11:00
        var series = state.Series[0];
        var first = series.Chapters[0];
        Assert.Equal(ChapterStatus.Complete, first.Status);
        Assert.Equal(new DateTime(1996, 4, 8, 11, 0, 0), first.CompletedAt);
        Assert.False(first.IsLate);
        Assert.Equal(0, first.HoursOverdue);
        Assert.All(first.Stages, s => Assert.Equal(StageStatus.Complete, s.Status));
        Assert.Single(state.Events, e => e.Type == EventType.ChapterCompleted);
        Assert.DoesNotContain(state.Events, e => e.Type == EventType.DeadlineMissed);
        Assert.Equal(2, series.Chapters.Count);
        Assert.Equal(new QueueRef(series.Chapters[1].Id, Stage.Name), state.People[0].CurrentTask);
    }

    [Fact]
    public void Late_chapter_is_flagged_with_hours_overdue()
    {
        var state = WeeklyNoOvertime();
        state.Advance(7 * 24 + 3); // due Monday 08:00, done Monday 11:00
        var first = state.Series[0].Chapters[0];
        Assert.Equal(ChapterStatus.Complete, first.Status);
        Assert.True(first.IsLate);
        Assert.Equal(3, first.HoursOverdue);
        var missed = Assert.Single(state.Events, e => e.Type == EventType.DeadlineMissed);
        Assert.Equal(1, missed.ChapterNumber);
        Assert.Equal(state.Series[0].Id, missed.SeriesId);
    }

    [Fact]
    public void Paused_series_gets_no_work()
    {
        var state = Monthly();
        state.Series[0].Status = SeriesStatus.Paused;
        state.RunPlanner();
        state.Advance(10);
        Assert.Equal(0, state.Series[0].Chapters[0].Stages.Sum(s => s.HoursDone));
        Assert.Equal(0, state.People[0].HoursWorkedToday);
    }

    [Fact]
    public void IsWorkingHour_regular_and_overtime_rules()
    {
        var state = WeeklyNoOvertime();
        var person = state.People[0];
        var chapter = state.Series[0].Chapters[0];
        var monday18 = new DateTime(1996, 4, 1, 18, 0, 0);
        Assert.True(state.IsWorkingHour(person, new DateTime(1996, 4, 1, 8, 0, 0), out var ot));
        Assert.False(ot);
        Assert.False(state.IsWorkingHour(person, monday18, out _));

        person.OvertimeAllowed = true;
        Assert.False(state.IsWorkingHour(person, monday18, out _)); // not at risk yet
        chapter.IsAtRisk = true;
        Assert.True(state.IsWorkingHour(person, monday18, out ot));
        Assert.True(ot);
        Assert.True(state.IsWorkingHour(person, monday18.AddHours(1), out _));
        Assert.False(state.IsWorkingHour(person, monday18.AddHours(2), out _)); // cap of 2
        Assert.False(state.IsWorkingHour(person, new DateTime(1996, 4, 7, 18, 0, 0), out _)); // Sunday
    }

    [Fact]
    public void Backgrounds_is_startable_while_inks_is_in_progress_but_tones_waits_for_both()
    {
        var state = Monthly();
        state.Advance(76); // Name done Tuesday 13:00, Pencils (17.8 working hours) done Thursday 11:00; now Thursday 12:00
        var chapter = state.Series[0].Chapters[0];
        Assert.Equal(StageStatus.Complete, chapter.StageWork(Stage.Pencils).Status);
        Assert.Equal(StageStatus.InProgress, chapter.StageWork(Stage.Inks).Status);
        Assert.True(state.IsStartable(new QueueRef(chapter.Id, Stage.Backgrounds)));
        Assert.False(state.IsStartable(new QueueRef(chapter.Id, Stage.Tones)));
        Assert.Equal(new QueueRef(chapter.Id, Stage.Inks), state.People[0].CurrentTask); // one person still goes in stage order
    }

    [Fact]
    public void Hours_by_person_track_who_worked_each_stage()
    {
        var state = Monthly();
        state.Advance(24 + 5);
        var name = state.Series[0].Chapters[0].StageWork(Stage.Name);
        Assert.Equal(StageStatus.Complete, name.Status);
        var (id, hours) = Assert.Single(name.HoursByPerson);
        Assert.Equal(state.People[0].Id, id);
        Assert.Equal(name.HoursDone, hours, 6);
        Assert.Equal(1.0, state.HourShares(state.Series[0].Chapters[0])[state.People[0].Id], 6);
    }
}
