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
        state.Apply(new StudioActionCommand(StudioAction.AutoPrint,state.Series[0].Id,1,10000,10,true));
        return state;
    }

    [Fact]
    public void Solo_weekly_prodigy_has_material_time_and_cash_pressure()
    {
        var state = Weekly(80);
        state.Advance(52 * 24 * 7);

        var completed = state.Series[0].Chapters.Where(c => c.Status == ChapterStatus.Complete).ToList();
        Assert.True(completed.Count >= 35, $"only {completed.Count} chapters completed in 52 weeks");
        var onTime = completed.Take(52).Count(c => !c.IsLate);
        _output.WriteLine($"Skill 80, 52 weeks: {completed.Count} completed; {onTime} of first 52 on time.");
        Assert.True(onTime < 45, $"only {onTime} of the first {Math.Min(52, completed.Count)} chapters were on time");
    }

    [Fact]
    public void Average_self_publisher_misses_personal_targets_without_penalties()
    {
        var state = Weekly(50);
        state.Advance(52 * 24 * 7);

        var completed = state.Series[0].Chapters.Where(c => c.Status == ChapterStatus.Complete).ToList();
        Assert.NotEmpty(completed);
        var late = completed.Count(c => c.CompletedAt > c.DueDate);
        _output.WriteLine($"Skill 50, 52 weeks: {completed.Count} completed; {late} late.");
        Assert.True(late > completed.Count / 2, $"{late} late of {completed.Count}");
        Assert.DoesNotContain(state.Events, e => e.Type == EventType.DeadlineMissed);
        Assert.All(completed,c=>Assert.False(c.IsLate));
    }

    [Fact]
    public void Prodigy_on_a_monthly_is_never_late_and_never_works_overtime()
    {
        var state = GameState.NewGame(42);
        state.Apply(new CreateSeriesCommand("Monthly", "seinen", Cadence.Monthly, 19));
        state.Apply(new StudioActionCommand(StudioAction.AutoPrint,state.Series[0].Id,1,10000,10,true));
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
        var workedDates = new HashSet<DateTime>();
        for(var hour=0;hour<52*24*7;hour++)
        {
            var before=state.Protagonist.ProductiveHours;
            state.Advance(1);
            if(state.Protagonist.ProductiveHours>before) workedDates.Add(state.TickStart.Date);
        }
        var recaps=state.Events.Where(e=>e.Type==EventType.DailyRecap).ToArray();
        Assert.Equal(workedDates.Order(),recaps.Select(e=>e.ActivityDate).Order());
        Assert.Equal(recaps.Length,recaps.Select(e=>e.ActivityDate).Distinct().Count());
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
