using MangakaSim;
using Xunit;

namespace MangakaSim.Tests;

public class CommandTests
{
    private static GameState WithWeekly()
    {
        var state = GameState.NewGame();
        state.Apply(new CreateSeriesCommand("Rush", "action", Cadence.Weekly, 19));
        return state;
    }

    [Fact]
    public void CreateSeries_adds_series_first_chapter_and_events()
    {
        var state = GameState.NewGame();
        state.Apply(new CreateSeriesCommand("Rush", "action", Cadence.Weekly, 19));

        var series = Assert.Single(state.Series);
        Assert.Equal("Rush", series.Title);
        Assert.Equal("action", series.Genre);
        Assert.Equal(Cadence.Weekly, series.Cadence);
        Assert.Equal(19, series.PagesPerChapter);
        Assert.Equal(SeriesStatus.Active, series.Status);
        Assert.Equal(GameClock.Start, series.StartDate);

        var chapter = Assert.Single(series.Chapters);
        Assert.Equal(1, chapter.Number);
        Assert.Equal(GameClock.Start.AddDays(7), chapter.DueDate);
        Assert.Equal(new QueueRef(chapter.Id, Stage.Name), state.People[0].CurrentTask);

        Assert.Contains(state.Events, e => e.Type == EventType.CommandApplied);
        Assert.Contains(state.Events, e => e.Type == EventType.ChapterCreated);
        Assert.Single(state.CommandLog);
    }

    [Fact]
    public void CreateSeries_rejects_blank_title_and_bad_pages_without_changing_state()
    {
        var state = GameState.NewGame();
        var eventsBefore = state.Events.Count;

        var ex1 = Assert.Throws<InvalidCommandException>(() =>
            state.Apply(new CreateSeriesCommand("   ", "action", Cadence.Weekly, 19)));
        Assert.Contains("title", ex1.Message, StringComparison.OrdinalIgnoreCase);

        var ex2 = Assert.Throws<InvalidCommandException>(() =>
            state.Apply(new CreateSeriesCommand("Rush", "action", Cadence.Weekly, 0)));
        Assert.Contains("pages", ex2.Message, StringComparison.OrdinalIgnoreCase);

        Assert.Empty(state.Series);
        Assert.Empty(state.CommandLog);
        Assert.Equal(eventsBefore, state.Events.Count);
    }

    [Fact]
    public void PauseSeries_stops_work_and_ResumeSeries_restarts_it()
    {
        var state = WithWeekly();
        var series = state.Series[0];

        state.Apply(new PauseSeriesCommand(series.Id));
        Assert.Equal(SeriesStatus.Paused, series.Status);
        Assert.Null(state.People[0].CurrentTask);

        state.Advance(3);
        Assert.Equal(0.0, series.Chapters[0].StageWork(Stage.Name).HoursDone);

        state.Apply(new ResumeSeriesCommand(series.Id));
        Assert.Equal(SeriesStatus.Active, series.Status);
        Assert.NotNull(state.People[0].CurrentTask);

        state.Advance(1);
        Assert.True(series.Chapters[0].StageWork(Stage.Name).HoursDone > 0);
    }

    [Fact]
    public void Pause_and_resume_validate_status_and_existence()
    {
        var state = WithWeekly();
        var series = state.Series[0];
        var eventsBefore = state.Events.Count;

        Assert.Throws<InvalidCommandException>(() => state.Apply(new ResumeSeriesCommand(series.Id))); // already active
        Assert.Throws<InvalidCommandException>(() => state.Apply(new PauseSeriesCommand(999)));         // unknown id

        state.Apply(new PauseSeriesCommand(series.Id));
        Assert.Throws<InvalidCommandException>(() => state.Apply(new PauseSeriesCommand(series.Id)));   // already paused

        Assert.Equal(eventsBefore + 1, state.Events.Count); // only the one valid pause emitted CommandApplied
        Assert.Equal(2, state.CommandLog.Count);
    }

    [Fact]
    public void SetCadence_applies_to_chapters_created_after()
    {
        var state = WithWeekly();
        var series = state.Series[0];
        var firstDue = series.Chapters[0].DueDate;

        state.Apply(new SetCadenceCommand(series.Id, Cadence.Monthly));

        Assert.Equal(Cadence.Monthly, series.Cadence);
        Assert.Equal(firstDue, series.Chapters[0].DueDate); // existing chapter untouched

        var next = state.CreateNextChapter(series);
        Assert.Equal(firstDue.AddMonths(1), next.DueDate);
    }

    [Fact]
    public void SetPagesPerChapter_applies_to_chapters_created_after()
    {
        var state = WithWeekly();
        var series = state.Series[0];

        state.Apply(new SetPagesPerChapterCommand(series.Id, 30));

        Assert.Equal(30, series.PagesPerChapter);
        Assert.Equal(22.8, series.Chapters[0].StageWork(Stage.Name).HoursRequired, precision: 6); // 19 pages, unchanged

        var next = state.CreateNextChapter(series);
        Assert.Equal(36.0, next.StageWork(Stage.Name).HoursRequired, precision: 6); // 30 * 1.2
    }

    [Fact]
    public void SetCadence_and_SetPages_validate()
    {
        var state = WithWeekly();
        var series = state.Series[0];
        Assert.Throws<InvalidCommandException>(() => state.Apply(new SetCadenceCommand(999, Cadence.Monthly)));
        Assert.Throws<InvalidCommandException>(() => state.Apply(new SetPagesPerChapterCommand(series.Id, 0)));
        Assert.Equal(19, series.PagesPerChapter);
    }

    [Fact]
    public void Apply_emits_CommandApplied_with_command_text()
    {
        var state = GameState.NewGame();
        var before = state.Events.Count;
        state.Apply(new CreateSeriesCommand("Rush", "action", Cadence.Weekly, 19));
        var applied = state.Events.Skip(before).First(e => e.Type == EventType.CommandApplied);
        Assert.Contains("CreateSeriesCommand", applied.Message);
        Assert.Contains("Rush", applied.Message);
    }
}
