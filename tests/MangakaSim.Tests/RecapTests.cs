using MangakaSim;
using Xunit;

namespace MangakaSim.Tests;

public class RecapTests
{
    private static GameState WithSeries(Cadence cadence, bool overtimeAllowed = true)
    {
        var state = GameState.NewGame();
        state.People[0].OvertimeAllowed = overtimeAllowed;
        state.CreateSeries("S", "g", cadence, 19);
        state.RunPlanner();
        return state;
    }

    private static List<GameEvent> Recaps(GameState state) =>
        state.Events.Where(e => e.Type == EventType.DailyRecap).ToList();

    [Fact]
    public void Recap_fires_at_18_on_a_normal_day_and_only_once()
    {
        var state = WithSeries(Cadence.Monthly);
        state.Advance(9);
        Assert.Empty(Recaps(state));
        state.Advance(1);
        var recap = Assert.Single(Recaps(state));
        Assert.Equal(new DateTime(1996, 4, 1, 18, 0, 0), recap.Time);
        Assert.Same(recap, state.Events.Last());
        state.Advance(6);
        Assert.Single(Recaps(state));
    }

    [Fact]
    public void Recap_payload_on_first_day()
    {
        var state = WithSeries(Cadence.Monthly);
        state.Advance(10);
        var payload = Recaps(state)[0].Recap!;
        var person = state.People[0];
        var seriesId = state.Series[0].Id;
        Assert.Equal(new[] { new StageRef(seriesId, 1, Stage.Name) }, payload.StagesStarted);
        Assert.Empty(payload.StagesCompleted);
        Assert.Equal(new[] { new PersonHours(person.Id, person.Name, 10, 0) }, payload.HoursPerPerson);
        Assert.Empty(payload.ChaptersAtRisk);
        Assert.Empty(payload.ChaptersCompleted);
        Assert.Empty(payload.DeadlinesMissed);
    }

    [Fact]
    public void Recap_fires_at_20_after_full_overtime_and_omits_resolved_risk()
    {
        var state = WithSeries(Cadence.Weekly);
        state.Advance(10);
        Assert.Empty(Recaps(state));
        state.Advance(2);
        var recap = Assert.Single(Recaps(state));
        Assert.Equal(new DateTime(1996, 4, 1, 20, 0, 0), recap.Time);
        var payload = recap.Recap!;
        Assert.Equal(new[] { new PersonHours(state.People[0].Id, state.People[0].Name, 12, 2) }, payload.HoursPerPerson);
        Assert.Empty(payload.ChaptersAtRisk);
    }

    [Fact]
    public void Recap_lists_risk_that_remains_after_work_ends()
    {
        var state = WithSeries(Cadence.Weekly, overtimeAllowed: false);
        state.Advance(10);
        var payload = Assert.Single(Recaps(state)).Recap!;
        Assert.Equal(new[] { new ChapterRef(state.Series[0].Id, 1) }, payload.ChaptersAtRisk);
    }

    [Fact]
    public void No_recap_on_a_day_off()
    {
        var state = WithSeries(Cadence.Monthly);
        state.Advance(7 * 24); // Monday 08:00 to next Monday 08:00
        Assert.Equal(6, Recaps(state).Count);
        Assert.DoesNotContain(Recaps(state), r => r.Time.DayOfWeek == DayOfWeek.Sunday);
    }

    [Fact]
    public void Recap_lists_stages_completed_and_started_that_day()
    {
        var state = WithSeries(Cadence.Monthly);
        state.Advance(24 + 10); // through Tuesday 18:00; Name completes Tuesday 13:00
        var tuesday = Recaps(state)[1].Recap!;
        var seriesId = state.Series[0].Id;
        Assert.Equal(new[] { new StageRef(seriesId, 1, Stage.Name) }, tuesday.StagesCompleted);
        Assert.Equal(new[] { new StageRef(seriesId, 1, Stage.Pencils) }, tuesday.StagesStarted);
    }

    [Fact]
    public void Recap_lists_chapters_completed_and_deadlines_missed()
    {
        var state = WithSeries(Cadence.Weekly, overtimeAllowed: false);
        state.Advance(7 * 24 + 10); // chapter 1 completes Monday 8 April 11:00, recap 18:00
        var recap = Recaps(state).Last().Recap!;
        var seriesId = state.Series[0].Id;
        Assert.Equal(new[] { new ChapterRef(seriesId, 1) }, recap.ChaptersCompleted);
        Assert.Equal(new[] { new ChapterRef(seriesId, 1) }, recap.DeadlinesMissed);
    }

    [Fact]
    public void Events_are_not_double_counted_across_days()
    {
        var state = WithSeries(Cadence.Monthly);
        state.Advance(24 + 10);
        var monday = Recaps(state)[0].Recap!;
        var tuesday = Recaps(state)[1].Recap!;
        Assert.Single(monday.StagesStarted);
        Assert.DoesNotContain(monday.StagesStarted[0], tuesday.StagesStarted);
    }

    [Fact]
    public void HoursUntilNextWork_now_when_in_working_hour()
    {
        var state = GameState.NewGame();
        Assert.Equal(0, state.HoursUntilNextWork());
    }

    [Fact]
    public void HoursUntilNextWork_overnight()
    {
        var state = GameState.NewGame();
        state.Advance(10); // 18:00
        Assert.Equal(14, state.HoursUntilNextWork());
        state.Advance(14);
        Assert.Equal(new DateTime(1996, 4, 2, 8, 0, 0), state.Clock.Now);
        Assert.Equal(0, state.HoursUntilNextWork());
    }

    [Fact]
    public void HoursUntilNextWork_skips_day_off()
    {
        var state = GameState.NewGame();
        state.Advance(5 * 24 + 10); // Saturday 18:00
        Assert.Equal(38, state.HoursUntilNextWork());
    }

    [Fact]
    public void HoursUntilNextWork_skips_multi_day_gap()
    {
        var state = GameState.NewGame();
        state.People[0].Schedule.DaysOff = new HashSet<DayOfWeek> { DayOfWeek.Saturday, DayOfWeek.Sunday };
        state.Advance(4 * 24 + 10); // Friday 18:00
        Assert.Equal(62, state.HoursUntilNextWork());
    }

    [Fact]
    public void Idle_skip_then_work_resumes_correctly()
    {
        var state = WithSeries(Cadence.Monthly);
        state.Advance(10);
        state.Advance(state.HoursUntilNextWork());
        Assert.Equal(1, state.Events.Count(e => e.Type == EventType.DayStarted));
        state.Advance(1);
        Assert.Equal(17.6, state.Series[0].Chapters[0].StageWork(Stage.Name).HoursDone, 6);
    }
}
