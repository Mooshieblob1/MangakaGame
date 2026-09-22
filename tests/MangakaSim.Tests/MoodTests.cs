using MangakaSim;
using Xunit;
using static MangakaSim.Tests.AssignmentTests;

namespace MangakaSim.Tests;

public class MoodTests
{
    [Fact]
    public void Happiness_drifts_toward_the_equilibrium_every_midnight()
    {
        var state = GameState.NewGame();
        var aki = state.People[0];
        Assert.Equal(45, state.EquilibriumOf(aki), 9); // garage atmosphere -10, nothing else
        state.Advance(16); // to midnight
        Assert.Equal(70 + 0.1 * (45 - 70), aki.Happiness, 9);
        state.Advance(24 * 30);
        Assert.InRange(aki.Happiness, 45, 46.5);
    }

    [Fact]
    public void Pay_below_the_market_rate_drags_an_assistant_down()
    {
        var state = GameState.NewGame();
        var assistant = AddAssistant(state, "Ren Ogawa", 60, salary: 220_000); // market 240,000
        Assert.Equal(220_000.0 / 240_000, state.PayFactorOf(assistant), 9);
        var expected = HappinessRules.Equilibrium(220_000.0 / 240_000, -10, 0, 0, 0);
        Assert.Equal(expected, state.EquilibriumOf(assistant), 9);
        state.Advance(16);
        Assert.Equal(60 + 0.1 * (expected - 60), assistant.Happiness, 9);
    }

    [Fact]
    public void Overtime_and_breaks_lower_the_equilibrium_and_amenities_raise_it()
    {
        var state = GameState.NewGame();
        state.Apply(new CreateSeriesCommand("Rush", "action", Cadence.Weekly, 30));
        state.Apply(new SetScheduleCommand(state.People[0].Id, 8, 20, new HashSet<DayOfWeek> { DayOfWeek.Sunday }));
        var aki = state.People[0];
        state.Advance(24 * 7);
        Assert.True(GameState.OvertimeShare(aki) > 0);
        Assert.True(GameState.BreaksPerDay(aki) > 0);
        Assert.True(state.EquilibriumOf(aki) < 45);
        var tired = state.EquilibriumOf(aki);
        state.Studio.PremisesId = "apartment";
        state.Studio.Amenities.AddRange(new[] { "fridge", "office-chairs", "air-conditioner" });
        Assert.Equal(tired + 10.0 / 2 + 11.0 / 2, state.EquilibriumOf(aki), 9);
    }

    [Fact]
    public void A_top_three_finish_cheers_the_contributors_and_a_cancellation_hurts_them()
    {
        var state = IssueCloseTests.SerializedAtJump();
        var series = state.Series[0];
        series.Fanbase = 5_000_000;
        IssueCloseTests.ForceComplete(state, series.Chapters[0], quality: 100);
        state.Advance(state.Clock.HoursUntil(state.MarketOf("tokiwa-jump").NextIssueClose) - 1);
        var before = state.People[0].Happiness; // captured after the last midnight drift
        state.Advance(1);
        Assert.Equal(1, series.LastRank);
        Assert.Equal(before + 2, state.People[0].Happiness, 6);
    }

    private static GameState UnhappyAssistant(int seed, out Person assistant)
    {
        var state = GameState.NewGame(seed);
        state.Apply(new CreateSeriesCommand("Rush", "action", Cadence.Weekly, 19));
        assistant = AddAssistant(state, "Ren Ogawa", 60, salary: 100_000); // pay factor 0.6 in the garage
        assistant.Happiness = 20;
        return state;
    }

    [Fact]
    public void An_unhappy_assistant_starts_moonlighting_and_a_better_studio_stops_it()
    {
        var state = UnhappyAssistant(2, out var assistant);
        state.Advance(24 * 60);
        Assert.True(assistant.IsMoonlighting, $"happiness {assistant.Happiness}");
        var started = Assert.Single(state.Events, e => e.Type == EventType.MoonlightingStarted);
        Assert.Equal(assistant.Id, started.PersonId);
        Assert.Equal(16, state.EffectiveWorkEndHour(assistant));
        Assert.DoesNotContain(state.Events, e => e.Type == EventType.StaffQuit && e.PersonId == assistant.Id);

        state.Apply(new MovePremisesCommand("apartment"));
        state.Apply(new BuyAmenityCommand("office-chairs"));
        state.Apply(new BuyAmenityCommand("fridge"));
        state.Apply(new SetSalaryCommand(assistant.Id, 340_000)); // pay factor 1.4, plus the raise shock
        Assert.True(state.EquilibriumOf(assistant) > 55, $"equilibrium {state.EquilibriumOf(assistant)}");
        state.Advance(24 * 60);
        Assert.False(assistant.IsMoonlighting);
        Assert.Single(state.Events, e => e.Type == EventType.MoonlightingStopped);
        Assert.Equal(18, state.EffectiveWorkEndHour(assistant));
        GameState.FromJson(state.ToJson());
    }

    private static GameState AtFirstQuitRoll(int seed)
    {
        var state = UnhappyAssistant(seed, out var assistant);
        state.Advance(state.Clock.HoursUntil(new DateTime(1996, 4, 30, 23, 0, 0)));
        assistant.Happiness = 5;
        state.Advance(1); // May 1st midnight: the roll
        return state;
    }

    [Fact]
    public void A_miserable_assistant_can_quit_and_returns_to_the_pool_six_months_later()
    {
        var seed = EditorTests.FindSeed(AtFirstQuitRoll, s => s.Events.Any(e => e.Type == EventType.StaffQuit));
        var state = AtFirstQuitRoll(seed);
        var quit = Assert.Single(state.Events, e => e.Type == EventType.StaffQuit);
        var former = Assert.Single(state.FormerPeople);
        Assert.Equal(former.Id, quit.PersonId);
        Assert.Single(state.People);
        var note = Assert.Single(state.Departures);
        Assert.True(note.Quit);
        Assert.Equal(new DateTime(1996, 5, 1), note.LeftAt);
        var open = state.Series[0].OpenChapter!;
        Assert.All(open.Stages.Where(w => !w.IsDone), w => Assert.Equal(state.People[0].Id, w.AssignedTo));
        Assert.Contains(state.Series[0].Chapters.SelectMany(c => c.Stages), w => w.IsDone && w.AssignedTo == former.Id); // history kept
        GameState.FromJson(state.ToJson());

        state.Advance(state.Clock.HoursUntil(new DateTime(1996, 10, 5, 18, 0, 0)));
        Assert.DoesNotContain(state.Candidates, c => c.IsReturning);
        state.Advance(state.Clock.HoursUntil(new DateTime(1996, 11, 7, 18, 0, 0)));
        var back = Assert.Single(state.Candidates, c => c.IsReturning);
        Assert.Equal(former.Name, back.Name);
        Assert.Equal(130_000, back.AskingSalary);
        Assert.Contains("left the studio in May 1996", back.Note);
        Assert.Equal(former.Skills, back.Skills);
        Assert.True(note.Returned);
        Assert.Equal(5, state.Candidates.Count);
        state.Advance(24 * 90);
        Assert.DoesNotContain(state.Candidates, c => c.IsReturning); // gone again, and never twice
        GameState.FromJson(state.ToJson());
    }

    [Fact]
    public void A_happy_assistant_never_rolls_and_missed_payrolls_double_the_odds()
    {
        var state = GameState.NewGame();
        var assistant = AddAssistant(state, "Ren Ogawa", 60);
        assistant.Happiness = 60;
        Assert.Equal(0, state.QuitChanceFor(assistant));
        assistant.Happiness = 10;
        Assert.Equal(0.2, state.QuitChanceFor(assistant), 9);
        state.Studio.MissedPayrolls = 2;
        Assert.Equal(0.4, state.QuitChanceFor(assistant), 9);
        assistant.MonthsEmployed = 20;
        Assert.Equal(0.12, state.QuitChanceFor(assistant), 9);
    }
}
