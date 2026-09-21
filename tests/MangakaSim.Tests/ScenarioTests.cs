using MangakaSim;
using Xunit;
using Xunit.Abstractions;

namespace MangakaSim.Tests;

public class ScenarioTests
{
    private readonly ITestOutputHelper _output;
    public ScenarioTests(ITestOutputHelper output) => _output = output;
    private static GameState Weekly(int skill)
    {
        var state = GameState.NewGame(42);
        foreach (var stage in StageOrder.All) state.People[0].Skills[stage] = skill;
        state.Apply(new CreateSeriesCommand("Weekly", "shonen", Cadence.Weekly, 19));
        return state;
    }

    [Fact]
    public void Prodigy_on_a_weekly_hits_at_least_45_of_52_deadlines()
    {
        var state = Weekly(80);
        state.Advance(52 * 24 * 7);

        var completed = state.Series[0].Chapters.Where(c => c.Status == ChapterStatus.Complete).ToList();
        Assert.True(completed.Count >= 45, $"only {completed.Count} chapters completed in 52 weeks");
        var onTime = completed.Take(52).Count(c => !c.IsLate);
        _output.WriteLine($"Skill 80, 52 weeks: {completed.Count} completed; {onTime} of first 52 on time.");
        Assert.True(onTime >= 45, $"only {onTime} of the first {Math.Min(52, completed.Count)} chapters were on time");
    }

    [Fact]
    public void Average_artist_on_a_weekly_is_mostly_late()
    {
        var state = Weekly(50);
        state.Advance(52 * 24 * 7);

        var completed = state.Series[0].Chapters.Where(c => c.Status == ChapterStatus.Complete).ToList();
        Assert.NotEmpty(completed);
        var late = completed.Count(c => c.IsLate);
        _output.WriteLine($"Skill 50, 52 weeks: {completed.Count} completed; {late} late.");
        Assert.True(late > completed.Count / 2, $"{late} late of {completed.Count}");
        Assert.Contains(state.Events, e => e.Type == EventType.DeadlineMissed);
    }

    [Fact]
    public void Prodigy_on_a_monthly_is_never_late_and_never_works_overtime()
    {
        var state = GameState.NewGame(42);
        state.Apply(new CreateSeriesCommand("Monthly", "seinen", Cadence.Monthly, 19));
        state.Advance(12 * 24 * 31);

        var completed = state.Series[0].Chapters.Where(c => c.Status == ChapterStatus.Complete).ToList();
        Assert.True(completed.Count >= 11, $"only {completed.Count} chapters completed in a year");
        Assert.All(completed, c => Assert.False(c.IsLate));
        Assert.DoesNotContain(state.Events, e => e.Type == EventType.ChapterAtRisk);
    }

    [Fact]
    public void A_year_of_weekly_play_emits_one_recap_per_working_day()
    {
        var state = Weekly(80);
        state.Advance(52 * 24 * 7);

        var recaps = state.Events.Count(e => e.Type == EventType.DailyRecap);
        Assert.Equal(52 * 6, recaps); // six working days a week, Sunday off
    }

    [Fact]
    public void Idle_skip_after_recap_lands_on_next_working_hour()
    {
        var state = Weekly(80);
        state.Advance(12); // Monday 20:00 after 10 regular + 2 overtime hours (weekly is at risk)
        Assert.Contains(state.Events, e => e.Type == EventType.DailyRecap);

        var skip = state.HoursUntilNextWork();
        Assert.Equal(12, skip); // 20:00 -> 08:00 Tuesday
        state.Advance(skip);
        Assert.Equal(new DateTime(1996, 4, 2, 8, 0, 0), state.Clock.Now);
    }
}
