using MangakaSim;
using Xunit;

namespace MangakaSim.Tests;

public class QueueCommandTests
{
    /// <summary>Weekly "A" (due first) and monthly "B". Default queue order puts every A stage before every B stage.</summary>
    private static (GameState state, Person person, Chapter a, Chapter b) TwoSeries()
    {
        var state = GameState.NewGame();
        state.Apply(new CreateSeriesCommand("A", "action", Cadence.Weekly, 19));
        state.Apply(new CreateSeriesCommand("B", "drama", Cadence.Monthly, 19));
        var person = state.People[0];
        return (state, person, state.Series[0].Chapters[0], state.Series[1].Chapters[0]);
    }

    [Fact]
    public void Default_queue_orders_by_due_date_then_stage()
    {
        var (_, person, a, b) = TwoSeries();
        Assert.Equal(10, person.Queue.Count);
        Assert.Equal(new QueueRef(a.Id, Stage.Name), person.Queue[0]);
        Assert.Equal(new QueueRef(a.Id, Stage.Tones), person.Queue[4]);
        Assert.Equal(new QueueRef(b.Id, Stage.Name), person.Queue[5]);
    }

    [Fact]
    public void PinStage_moves_ref_to_front_and_becomes_current_task()
    {
        var (state, person, _, b) = TwoSeries();
        state.Apply(new PinStageCommand(person.Id, b.Id, Stage.Name));

        Assert.Contains(new QueueRef(b.Id, Stage.Name), person.Pins);
        Assert.Equal(new QueueRef(b.Id, Stage.Name), person.Queue[0]);
        Assert.Equal(new QueueRef(b.Id, Stage.Name), person.CurrentTask);
    }

    [Fact]
    public void UnpinStage_restores_default_order()
    {
        var (state, person, a, b) = TwoSeries();
        state.Apply(new PinStageCommand(person.Id, b.Id, Stage.Name));
        state.Apply(new UnpinStageCommand(person.Id, b.Id, Stage.Name));

        Assert.Empty(person.Pins);
        Assert.Equal(new QueueRef(a.Id, Stage.Name), person.Queue[0]);
    }

    [Fact]
    public void Pin_and_unpin_validate()
    {
        var (state, person, a, b) = TwoSeries();
        Assert.Throws<InvalidCommandException>(() => state.Apply(new PinStageCommand(999, b.Id, Stage.Name)));
        Assert.Throws<InvalidCommandException>(() => state.Apply(new PinStageCommand(person.Id, 999, Stage.Name)));
        Assert.Throws<InvalidCommandException>(() => state.Apply(new UnpinStageCommand(person.Id, b.Id, Stage.Name))); // not pinned

        state.Apply(new PinStageCommand(person.Id, b.Id, Stage.Name));
        Assert.Throws<InvalidCommandException>(() => state.Apply(new PinStageCommand(person.Id, b.Id, Stage.Name))); // twice

        state.Apply(new SkipStageCommand(a.Id, Stage.Name));
        Assert.Throws<InvalidCommandException>(() => state.Apply(new PinStageCommand(person.Id, a.Id, Stage.Name))); // done
        Assert.Single(person.Pins);
    }

    [Fact]
    public void ReorderQueue_applies_given_order()
    {
        var (state, person, a, b) = TwoSeries();
        var reversed = person.Queue.AsEnumerable().Reverse().ToList();
        state.Apply(new ReorderQueueCommand(person.Id, reversed));

        Assert.Equal(reversed, person.Queue);
        Assert.Equal(new QueueRef(b.Id, Stage.Tones), person.Queue[0]);
        Assert.Equal(new QueueRef(b.Id, Stage.Name), person.CurrentTask); // first startable in the new order
        Assert.NotNull(person.ManualOrder);
        _ = a;
    }

    [Fact]
    public void ReorderQueue_pins_still_win()
    {
        var (state, person, a, b) = TwoSeries();
        state.Apply(new PinStageCommand(person.Id, a.Id, Stage.Name));
        var bFirst = person.Queue.Where(r => r.ChapterId == b.Id).Concat(person.Queue.Where(r => r.ChapterId == a.Id)).ToList();
        state.Apply(new ReorderQueueCommand(person.Id, bFirst));

        Assert.Equal(new QueueRef(a.Id, Stage.Name), person.Queue[0]);
        Assert.Equal(new QueueRef(b.Id, Stage.Name), person.Queue[1]);
    }

    [Fact]
    public void ReorderQueue_expires_at_next_DayStarted()
    {
        var (state, person, a, b) = TwoSeries();
        var bFirst = person.Queue.Where(r => r.ChapterId == b.Id).Concat(person.Queue.Where(r => r.ChapterId == a.Id)).ToList();
        state.Apply(new ReorderQueueCommand(person.Id, bFirst));
        Assert.Equal(b.Id, person.Queue[0].ChapterId);

        state.Advance(16); // Monday 08:00 -> Tuesday 00:00, DayStarted fires

        Assert.Null(person.ManualOrder);
        Assert.Equal(a.Id, person.Queue[0].ChapterId);
    }

    [Fact]
    public void ReorderQueue_rejects_non_permutations()
    {
        var (state, person, a, b) = TwoSeries();
        var before = person.Queue.ToList();

        var missingOne = person.Queue.Skip(1).ToList();
        Assert.Throws<InvalidCommandException>(() => state.Apply(new ReorderQueueCommand(person.Id, missingOne)));

        var duplicate = person.Queue.ToList();
        duplicate[1] = duplicate[0];
        Assert.Throws<InvalidCommandException>(() => state.Apply(new ReorderQueueCommand(person.Id, duplicate)));

        var foreign = person.Queue.ToList();
        foreign[0] = new QueueRef(999, Stage.Name);
        Assert.Throws<InvalidCommandException>(() => state.Apply(new ReorderQueueCommand(person.Id, foreign)));

        Assert.Equal(before, person.Queue);
        Assert.Null(person.ManualOrder);
        _ = a; _ = b;
    }

    [Fact]
    public void SkipStage_marks_skipped_emits_event_and_unblocks_next_stage()
    {
        var (state, person, a, _) = TwoSeries();
        state.Apply(new SkipStageCommand(a.Id, Stage.Name));

        var name = a.StageWork(Stage.Name);
        Assert.Equal(StageStatus.Skipped, name.Status);
        Assert.Equal(0.0, name.HoursDone);
        Assert.Contains(state.Events, e => e.Type == EventType.StageSkipped && e.ChapterNumber == 1 && e.Stage == Stage.Name);
        Assert.Equal(new QueueRef(a.Id, Stage.Pencils), person.CurrentTask);
        Assert.DoesNotContain(new QueueRef(a.Id, Stage.Name), person.Queue);
    }

    [Fact]
    public void Skipping_every_stage_completes_chapter_and_creates_next()
    {
        var (state, _, a, _) = TwoSeries();
        var series = state.Series[0];
        foreach (var stage in StageOrder.All) state.Apply(new SkipStageCommand(a.Id, stage));

        Assert.Equal(ChapterStatus.Complete, a.Status);
        Assert.False(a.IsLate);
        Assert.Contains(state.Events, e => e.Type == EventType.ChapterCompleted && e.SeriesId == series.Id);
        Assert.Equal(2, series.Chapters.Count);
        Assert.Equal(2, series.Chapters[1].Number);
    }

    [Fact]
    public void SkipStage_cannot_skip_complete_or_skipped_or_unknown()
    {
        var (state, _, a, _) = TwoSeries();
        state.Apply(new SkipStageCommand(a.Id, Stage.Name));
        Assert.Throws<InvalidCommandException>(() => state.Apply(new SkipStageCommand(a.Id, Stage.Name)));
        Assert.Throws<InvalidCommandException>(() => state.Apply(new SkipStageCommand(999, Stage.Name)));

        state.Apply(new SkipStageCommand(a.Id, Stage.Pencils));
        state.Advance(30); // Inks: 19h at 1.6/h = 12 worked hours; 10 on Monday + 6 on Tuesday by 14:00 (no overtime, chapter is not at risk after two skips)
        Assert.Equal(StageStatus.Complete, a.StageWork(Stage.Inks).Status);
        Assert.Throws<InvalidCommandException>(() => state.Apply(new SkipStageCommand(a.Id, Stage.Inks)));
    }

    [Fact]
    public void SetSchedule_updates_schedule_and_validates()
    {
        var (state, person, _, _) = TwoSeries();
        state.Apply(new SetScheduleCommand(person.Id, 9, 17, new HashSet<DayOfWeek> { DayOfWeek.Saturday, DayOfWeek.Sunday }));

        Assert.Equal(9, person.Schedule.WorkStartHour);
        Assert.Equal(17, person.Schedule.WorkEndHour);
        Assert.Equal(new HashSet<DayOfWeek> { DayOfWeek.Saturday, DayOfWeek.Sunday }, person.Schedule.DaysOff);

        Assert.Throws<InvalidCommandException>(() => state.Apply(new SetScheduleCommand(person.Id, 17, 9, new HashSet<DayOfWeek>())));
        Assert.Throws<InvalidCommandException>(() => state.Apply(new SetScheduleCommand(person.Id, -1, 9, new HashSet<DayOfWeek>())));
        Assert.Throws<InvalidCommandException>(() => state.Apply(new SetScheduleCommand(person.Id, 8, 23, new HashSet<DayOfWeek>()))); // 23 + 2 overtime > 24
        Assert.Throws<InvalidCommandException>(() => state.Apply(new SetScheduleCommand(999, 8, 18, new HashSet<DayOfWeek>())));
        Assert.Equal(9, person.Schedule.WorkStartHour);
    }

    [Fact]
    public void SetOvertimeAllowed_toggles_flag()
    {
        var (state, person, _, _) = TwoSeries();
        state.Apply(new SetOvertimeAllowedCommand(person.Id, false));
        Assert.False(person.OvertimeAllowed);
        state.Apply(new SetOvertimeAllowedCommand(person.Id, true));
        Assert.True(person.OvertimeAllowed);
        Assert.Throws<InvalidCommandException>(() => state.Apply(new SetOvertimeAllowedCommand(999, true)));
    }
}
