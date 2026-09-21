using MangakaSim;
using Xunit;

namespace MangakaSim.Tests;

public class PlannerTests
{
    private static GameState NewStateWithSeries(Cadence cadence = Cadence.Weekly, int pages = 19)
    {
        var state = GameState.NewGame();
        state.CreateSeries("Blade Saga", "action", cadence, pages);
        state.RunPlanner();
        return state;
    }

    [Theory]
    [InlineData(Cadence.Weekly, "1996-04-08T08:00:00")]
    [InlineData(Cadence.Biweekly, "1996-04-15T08:00:00")]
    [InlineData(Cadence.Monthly, "1996-05-01T08:00:00")]
    public void Creates_chapter_one_due_at_start_plus_interval(Cadence cadence, string due)
    {
        var state = NewStateWithSeries(cadence);
        var chapter = Assert.Single(state.Series[0].Chapters);
        Assert.Equal(1, chapter.Number);
        Assert.Equal(DateTime.Parse(due), chapter.DueDate);
        Assert.Equal(ChapterStatus.NotStarted, chapter.Status);
        Assert.Single(state.Events, e => e.Type == EventType.ChapterCreated);
    }

    [Fact]
    public void Chapter_stages_have_hours_from_balance_table()
    {
        var state = NewStateWithSeries();
        var chapter = state.Series[0].Chapters[0];
        Assert.Equal(StageOrder.All, chapter.Stages.Select(s => s.Stage));
        Assert.Equal(22.8, chapter.StageWork(Stage.Name).HoursRequired, 6);
        Assert.Equal(9.5, chapter.StageWork(Stage.Tones).HoursRequired, 6);
        Assert.All(chapter.Stages, s => Assert.Equal(state.People[0].Id, s.AssignedTo));
    }

    [Fact]
    public void Is_idempotent()
    {
        var state = NewStateWithSeries();
        var eventsBefore = state.Events.Count;
        var queueBefore = state.People[0].Queue.ToList();
        state.RunPlanner();
        state.RunPlanner();
        Assert.Single(state.Series[0].Chapters);
        Assert.Equal(eventsBefore, state.Events.Count);
        Assert.Equal(queueBefore, state.People[0].Queue);
    }

    [Fact]
    public void Queue_is_all_five_stages_in_order_and_current_task_is_first()
    {
        var state = NewStateWithSeries();
        var person = state.People[0];
        var chapterId = state.Series[0].Chapters[0].Id;
        Assert.Equal(StageOrder.All.Select(s => new QueueRef(chapterId, s)), person.Queue);
        Assert.Equal(new QueueRef(chapterId, Stage.Name), person.CurrentTask);
    }

    [Fact]
    public void Next_chapter_created_when_previous_is_complete_with_due_from_previous_due()
    {
        var state = NewStateWithSeries();
        var first = state.Series[0].Chapters[0];
        foreach (var s in first.Stages) s.Status = StageStatus.Complete;
        first.Status = ChapterStatus.Complete;
        state.RunPlanner();
        Assert.Equal(2, state.Series[0].Chapters.Count);
        var second = state.Series[0].Chapters[1];
        Assert.Equal(2, second.Number);
        Assert.Equal(first.DueDate.AddDays(7), second.DueDate);
        Assert.Equal(new QueueRef(second.Id, Stage.Name), state.People[0].CurrentTask);
    }

    [Fact]
    public void Queue_orders_by_due_date_then_stage_across_series()
    {
        var state = GameState.NewGame();
        state.CreateSeries("Late", "a", Cadence.Monthly, 19);
        state.CreateSeries("Soon", "b", Cadence.Weekly, 19);
        state.RunPlanner();
        var soon = state.Series[1].Chapters[0];
        var late = state.Series[0].Chapters[0];
        var queue = state.People[0].Queue;
        Assert.Equal(10, queue.Count);
        Assert.All(queue.Take(5), r => Assert.Equal(soon.Id, r.ChapterId));
        Assert.All(queue.Skip(5), r => Assert.Equal(late.Id, r.ChapterId));
        Assert.Equal(StageOrder.All, queue.Take(5).Select(r => r.Stage));
    }

    [Fact]
    public void Pinned_items_come_first_in_pin_order()
    {
        var state = NewStateWithSeries();
        var person = state.People[0];
        var chapterId = state.Series[0].Chapters[0].Id;
        person.Pins.Add(new QueueRef(chapterId, Stage.Tones));
        person.Pins.Add(new QueueRef(chapterId, Stage.Inks));
        state.RunPlanner();
        Assert.Equal(new QueueRef(chapterId, Stage.Tones), person.Queue[0]);
        Assert.Equal(new QueueRef(chapterId, Stage.Inks), person.Queue[1]);
        Assert.Equal(new QueueRef(chapterId, Stage.Name), person.Queue[2]);
        // Tones is not startable (earlier stages not done), so CurrentTask is Name.
        Assert.Equal(new QueueRef(chapterId, Stage.Name), person.CurrentTask);
    }

    [Fact]
    public void Manual_order_is_respected_and_pins_still_win()
    {
        var state = NewStateWithSeries();
        var person = state.People[0];
        var chapterId = state.Series[0].Chapters[0].Id;
        person.ManualOrder = new List<QueueRef>
        {
            new(chapterId, Stage.Backgrounds), new(chapterId, Stage.Name), new(chapterId, Stage.Tones),
            new(chapterId, Stage.Inks), new(chapterId, Stage.Pencils),
        };
        person.Pins.Add(new QueueRef(chapterId, Stage.Inks));
        state.RunPlanner();
        Assert.Equal(new[]
        {
            new QueueRef(chapterId, Stage.Inks), new QueueRef(chapterId, Stage.Backgrounds),
            new QueueRef(chapterId, Stage.Name), new QueueRef(chapterId, Stage.Tones),
            new QueueRef(chapterId, Stage.Pencils),
        }, person.Queue);
    }

    [Fact]
    public void Paused_series_contribute_nothing()
    {
        var state = NewStateWithSeries();
        state.Series[0].Status = SeriesStatus.Paused;
        state.RunPlanner();
        Assert.Empty(state.People[0].Queue);
        Assert.Null(state.People[0].CurrentTask);
    }

    [Fact]
    public void Skipped_and_complete_stages_leave_the_queue()
    {
        var state = NewStateWithSeries();
        var chapter = state.Series[0].Chapters[0];
        chapter.StageWork(Stage.Name).Status = StageStatus.Complete;
        chapter.StageWork(Stage.Pencils).Status = StageStatus.Skipped;
        state.RunPlanner();
        Assert.Equal(3, state.People[0].Queue.Count);
        Assert.Equal(new QueueRef(chapter.Id, Stage.Inks), state.People[0].CurrentTask);
    }
}
