using MangakaSim;
using Xunit;

namespace MangakaSim.Tests;

public class HiringTests
{
    private static GameState AtFirstRefresh(int seed = 0)
    {
        var state = GameState.NewGame(seed);
        state.Advance(state.Clock.HoursUntil(new DateTime(1996, 5, 1, 18, 0, 0))); // first close in May
        return state;
    }

    internal static Candidate Cheapest(GameState state) => state.Candidates.Where(c => !c.IsScheduled).MinBy(c => c.AskingSalary)!;

    [Fact]
    public void The_pool_is_empty_until_the_first_monthly_refresh()
    {
        var state = GameState.NewGame();
        state.Advance(24 * 29);
        Assert.Empty(state.Candidates);
        Assert.DoesNotContain(state.Events, e => e.Type == EventType.CandidatePoolRefreshed);
    }

    [Fact]
    public void The_first_refresh_fills_four_generated_candidates()
    {
        var state = AtFirstRefresh(3);
        Assert.Equal(4, state.Candidates.Count);
        Assert.Equal(new DateTime(1996, 5, 1), state.LastPoolRefreshMonth);
        var catalog = StaffCatalog.LoadDefault();
        foreach (var c in state.Candidates)
        {
            var parts = c.Name.Split(' ');
            Assert.Equal(2, parts.Length);
            Assert.Contains(parts[0], catalog.GivenNames);
            Assert.Contains(parts[1], catalog.FamilyNames);
            Assert.All(StageOrder.All, s => Assert.InRange(c.Skill(s), 5, 95));
            var market = PayRules.MarketSalary(c.Skills.Values, 1.0);
            Assert.InRange(c.AskingSalary, market * 0.9 - 500, market * 1.15 + 500);
            Assert.InRange(c.AvailableUntil, new DateTime(1996, 6, 1, 18, 0, 0), new DateTime(1996, 7, 1, 18, 0, 0));
            Assert.False(c.IsScheduled);
            Assert.True(c.Id > 1);
        }
        Assert.Equal(4, state.Candidates.Select(c => c.Name).Distinct().Count());
        Assert.Single(state.Events, e => e.Type == EventType.CandidatePoolRefreshed);
        Assert.True(state.Candidates.Average(c => c.Skill(Stage.Name)) < state.Candidates.Average(c => c.Skill(Stage.Inks)) + 5);
        GameState.FromJson(state.ToJson());
    }

    [Fact]
    public void Later_refreshes_drop_expired_candidates_and_top_the_pool_up()
    {
        var state = AtFirstRefresh(4);
        var first = state.Candidates.Select(c => c.Id).ToList();
        state.Advance(state.Clock.HoursUntil(new DateTime(1996, 8, 5, 18, 0, 0)));
        Assert.Equal(4, state.Candidates.Count);
        Assert.All(state.Candidates, c => Assert.True(c.AvailableUntil > new DateTime(1996, 8, 1, 18, 0, 0))); // alive at the August refresh
        Assert.Empty(first.Intersect(state.Candidates.Select(c => c.Id))); // May's candidates have all left
        Assert.Equal(4, state.Events.Count(e => e.Type == EventType.CandidatePoolRefreshed)); // May, June, July, August
    }

    [Fact]
    public void Scheduled_candidates_appear_once_at_their_moment()
    {
        var state = GameState.NewGame(1);
        state.Advance(state.Clock.HoursUntil(new DateTime(1997, 6, 30, 23, 0, 0)));
        Assert.DoesNotContain(state.Candidates, c => c.IsScheduled);
        state.Advance(state.Clock.HoursUntil(new DateTime(1997, 7, 3, 18, 0, 0)));
        var oga = Assert.Single(state.Candidates, c => c.IsScheduled);
        Assert.Equal("Eiichido Oga", oga.Name);
        Assert.Equal(75, oga.Skill(Stage.Inks));
        Assert.Equal(PayRules.MarketSalary(oga.Skills.Values, state.PriceIndexNow), oga.AskingSalary);
        Assert.Equal(5, state.Candidates.Count); // four generated plus the star
        var appeared = Assert.Single(state.Events, e => e.Type == EventType.CandidateAppeared);
        Assert.Contains("Eiichido Oga", appeared.Message);
        Assert.Contains("Eiichido Oga", state.ScheduledCandidatesShown);

        state.Advance(24 * 120);
        Assert.DoesNotContain(state.Candidates, c => c.IsScheduled);
        Assert.Single(state.Events, e => e.Type == EventType.CandidateAppeared);
        GameState.FromJson(state.ToJson());
    }

    [Fact]
    public void Hiring_at_the_asking_salary_adds_an_assistant()
    {
        var state = AtFirstRefresh();
        var candidate = Cheapest(state);
        state.Apply(new HireCommand(candidate.Id, candidate.AskingSalary));
        Assert.Equal(2, state.People.Count);
        var hired = state.People[1];
        Assert.Equal(candidate.Id, hired.Id);
        Assert.Equal(candidate.Name, hired.Name);
        Assert.Equal(PersonRole.Assistant, hired.Role);
        Assert.Equal(candidate.AskingSalary, hired.Salary);
        Assert.Equal(state.Clock.Now, hired.HiredAt);
        Assert.Equal(60, hired.Happiness, 6);
        Assert.Equal(100, hired.Needs.Min);
        Assert.Equal(0, hired.Fatigue);
        Assert.DoesNotContain(Stage.Name, hired.AllowedStages);
        Assert.Equal(4, hired.AllowedStages.Count);
        Assert.Equal(candidate.Skills, hired.Skills);
        Assert.DoesNotContain(state.Candidates, c => c.Id == candidate.Id);
        var hiredEvent = Assert.Single(state.Events, e => e.Type == EventType.StaffHired);
        Assert.Equal(hired.Id, hiredEvent.PersonId);
        Assert.Equal(candidate.AskingSalary, hiredEvent.Amount);
        GameState.FromJson(state.ToJson());
    }

    [Fact]
    public void Hiring_validates_salary_capacity_and_candidate()
    {
        var state = AtFirstRefresh();
        var candidate = Cheapest(state);
        var before = state.ToJson();
        Assert.Throws<InvalidCommandException>(() => state.Apply(new HireCommand(999, 200_000)));
        Assert.Throws<InvalidCommandException>(() => state.Apply(new HireCommand(candidate.Id, -1)));
        var refused = Assert.Throws<InvalidCommandException>(() => state.Apply(new HireCommand(candidate.Id, (int)(candidate.AskingSalary * 0.79))));
        Assert.Contains("won't work", refused.Message);
        Assert.Equal(before, state.ToJson());

        state.Apply(new HireCommand(candidate.Id, (int)(candidate.AskingSalary * 0.8) + 1));
        Assert.InRange(state.People[1].Happiness, 55.9, 56.1);

        var second = Cheapest(state);
        var noDesk = Assert.Throws<InvalidCommandException>(() => state.Apply(new HireCommand(second.Id, second.AskingSalary)));
        Assert.Contains("No desk", noDesk.Message); // the garage holds two
        Assert.Equal(2, state.People.Count);
    }

    [Fact]
    public void Firing_pays_severance_and_keeps_the_record()
    {
        var state = AtFirstRefresh();
        var candidate = Cheapest(state);
        state.Apply(new HireCommand(candidate.Id, candidate.AskingSalary));
        var hired = state.People[1];
        state.Advance(24 * 3);
        var akiBefore = state.People[0].Happiness;
        var moneyBefore = state.Money;

        state.Apply(new FireCommand(hired.Id));

        Assert.Single(state.People);
        Assert.Contains(hired, state.FormerPeople);
        Assert.Same(hired, state.FindAnyPerson(hired.Id));
        Assert.Null(state.FindPerson(hired.Id));
        var severance = Assert.Single(state.Ledger, l => l.Reason == "severance");
        Assert.Equal(-hired.Salary, severance.Amount);
        Assert.Equal(hired.Id, severance.PersonId);
        Assert.Equal(moneyBefore - hired.Salary, state.Money);
        Assert.Equal(akiBefore - 3, state.People[0].Happiness, 6);
        var note = Assert.Single(state.Departures);
        Assert.False(note.Quit);
        Assert.Equal(hired.Name, note.Name);
        Assert.Single(state.Events, e => e.Type == EventType.StaffFired);
        Assert.Empty(hired.Queue);
        Assert.Null(hired.CurrentTask);
        GameState.FromJson(state.ToJson());

        Assert.Throws<InvalidCommandException>(() => state.Apply(new FireCommand(hired.Id)));
        Assert.Throws<InvalidCommandException>(() => state.Apply(new FireCommand(state.People[0].Id)));
    }

    [Fact]
    public void Salary_changes_shock_happiness()
    {
        var state = AtFirstRefresh();
        var candidate = Cheapest(state);
        state.Apply(new HireCommand(candidate.Id, candidate.AskingSalary));
        var hired = state.People[1];
        var salary = hired.Salary;
        state.Apply(new SetSalaryCommand(hired.Id, salary + salary / 10 + 1000));
        Assert.Equal(70, hired.Happiness, 6);
        state.Apply(new SetSalaryCommand(hired.Id, salary));
        Assert.Equal(55, hired.Happiness, 6);
        state.Apply(new SetSalaryCommand(hired.Id, salary + 1000)); // under 10%: no shock
        Assert.Equal(55, hired.Happiness, 6);
        Assert.Equal(3, state.Events.Count(e => e.Type == EventType.SalaryChanged));
        Assert.Throws<InvalidCommandException>(() => state.Apply(new SetSalaryCommand(hired.Id, -5)));
        Assert.Throws<InvalidCommandException>(() => state.Apply(new SetSalaryCommand(state.People[0].Id, 100_000)));
    }

    [Fact]
    public void Allowed_stages_leads_and_manual_assignments_validate()
    {
        var state = AtFirstRefresh();
        state.Apply(new CreateSeriesCommand("Rush", "action", Cadence.Weekly, 19));
        var candidate = Cheapest(state);
        state.Apply(new HireCommand(candidate.Id, candidate.AskingSalary));
        var aki = state.People[0];
        var hired = state.People[1];
        var series = state.Series[0];
        var chapter = series.Chapters[0];

        Assert.Throws<InvalidCommandException>(() => state.Apply(new SetAllowedStagesCommand(hired.Id, new HashSet<Stage>())));
        Assert.Throws<InvalidCommandException>(() => state.Apply(new SetAllowedStagesCommand(aki.Id, new HashSet<Stage> { Stage.Inks })));
        Assert.Throws<InvalidCommandException>(() => state.Apply(new SetSeriesLeadCommand(series.Id, hired.Id)));
        Assert.Equal(aki.Id, series.LeadId);

        state.Apply(new SetAllowedStagesCommand(hired.Id, new HashSet<Stage> { Stage.Name, Stage.Inks }));
        Assert.Equal(new HashSet<Stage> { Stage.Name, Stage.Inks }, hired.AllowedStages);
        state.Apply(new SetSeriesLeadCommand(series.Id, hired.Id));
        Assert.Equal(hired.Id, series.LeadId);
        Assert.Throws<InvalidCommandException>(() => state.Apply(new SetAllowedStagesCommand(hired.Id, new HashSet<Stage> { Stage.Inks })));

        Assert.Throws<InvalidCommandException>(() => state.Apply(new AssignStageCommand(chapter.Id, Stage.Tones, hired.Id)));
        state.Apply(new AssignStageCommand(chapter.Id, Stage.Inks, hired.Id));
        Assert.Equal(hired.Id, chapter.StageWork(Stage.Inks).ManualAssignee);
        state.Apply(new AssignStageCommand(chapter.Id, Stage.Inks, null));
        Assert.Null(chapter.StageWork(Stage.Inks).ManualAssignee);
        state.Apply(new SkipStageCommand(chapter.Id, Stage.Tones));
        Assert.Throws<InvalidCommandException>(() => state.Apply(new AssignStageCommand(chapter.Id, Stage.Tones, aki.Id)));
        GameState.FromJson(state.ToJson());
    }
}
