using MangakaSim;
using Xunit;

namespace MangakaSim.Tests;

public class AssignmentTests
{
    /// <summary>Adds an assistant straight to the studio (the pool is tested elsewhere).</summary>
    internal static Person AddAssistant(GameState state, string name, int skill, int salary = 220_000, params Stage[] allowed)
    {
        var person = new Person
        {
            Id = state.AllocateId(),
            Name = name,
            Role = PersonRole.Assistant,
            Salary = salary,
            HiredAt = state.Clock.Now,
            Reputation = 5,
            Happiness = 60,
            Schedule = new Schedule { WorkStartHour = 8, WorkEndHour = 18, DaysOff = { DayOfWeek.Sunday } },
            OvertimeAllowed = true,
            AllowedStages = (allowed.Length > 0 ? allowed : StageOrder.All.Where(s => s != Stage.Name)).ToHashSet(),
        };
        foreach (var stage in StageOrder.All) person.Skills[stage] = skill;
        if (state.People.Count >= state.CurrentPremises.Capacity) state.Studio.PremisesId = "apartment";
        state.People.Add(person);
        state.RunPlanner();
        state.RiskStep();
        return person;
    }

    private static GameState WithInker(out Person inker)
    {
        var state = GameState.NewGame();
        state.Apply(new CreateSeriesCommand("Rush", "action", Cadence.Weekly, 19));
        inker = AddAssistant(state, "Ren Ogawa", 60);
        return state;
    }

    [Fact]
    public void The_lead_keeps_the_name_and_the_pencils_and_the_assistant_takes_backgrounds()
    {
        var state = WithInker(out var inker);
        var aki = state.People[0];
        var chapter = state.Series[0].Chapters[0];
        Assert.Equal(aki.Id, chapter.StageWork(Stage.Name).AssignedTo);
        Assert.Equal(aki.Id, chapter.StageWork(Stage.Pencils).AssignedTo);
        // Aki already carries 32 queued hours (Name and Pencils): 1.6 / 2.6 = 0.62 against the assistant's 1.2.
        Assert.Equal(inker.Id, chapter.StageWork(Stage.Inks).AssignedTo);
        Assert.Equal(inker.Id, chapter.StageWork(Stage.Backgrounds).AssignedTo); // 1.2 / 1.79 = 0.67 still beats 0.62
        Assert.Equal(aki.Id, chapter.StageWork(Stage.Tones).AssignedTo);          // the assistant is now at 0.46
        Assert.Equal(new QueueRef(chapter.Id, Stage.Name), aki.CurrentTask);
        Assert.Null(inker.CurrentTask); // backgrounds wait for the pencils
        Assert.Contains(new QueueRef(chapter.Id, Stage.Backgrounds), inker.Queue);
        GameState.FromJson(state.ToJson());
    }

    [Fact]
    public void Inks_and_backgrounds_run_in_the_same_hour_on_two_desks()
    {
        var state = WithInker(out var inker);
        var chapter = state.Series[0].Chapters[0];
        for (var i = 0; i < 24 * 7 && chapter.StageWork(Stage.Pencils).Status != StageStatus.Complete; i++) state.Advance(1);
        Assert.Equal(StageStatus.Complete, chapter.StageWork(Stage.Pencils).Status);
        // With the pencils done the planner re-splits the rest: Aki (free) inks, the assistant does backgrounds.
        Assert.Equal(state.People[0].Id, chapter.StageWork(Stage.Inks).AssignedTo);
        Assert.Equal(inker.Id, chapter.StageWork(Stage.Backgrounds).AssignedTo);
        Assert.Equal(state.People[0].Id, chapter.StageWork(Stage.Tones).AssignedTo);
        state.Advance(state.HoursUntilNextWork() + 1);
        Assert.Equal(StageStatus.InProgress, chapter.StageWork(Stage.Inks).Status);
        Assert.Equal(StageStatus.InProgress, chapter.StageWork(Stage.Backgrounds).Status);
        Assert.True(chapter.StageWork(Stage.Backgrounds).HoursByPerson[inker.Id] >= 1.2); // started the hour the pencils finished
        Assert.True(chapter.StageWork(Stage.Inks).HoursByPerson[state.People[0].Id] >= 1.6);
        Assert.Equal(2, state.Events.Count(e => e.Type == EventType.StageStarted && e.Stage is Stage.Inks or Stage.Backgrounds));
    }

    [Fact]
    public void A_buried_lead_hands_pencils_and_inks_to_the_assistant()
    {
        var state = GameState.NewGame();
        state.Apply(new CreateSeriesCommand("Rush", "action", Cadence.Weekly, 19));
        state.Apply(new CreateSeriesCommand("Dash", "sports", Cadence.Weekly, 19));
        var assistant = AddAssistant(state, "Mio Sakai", 60);
        var aki = state.People[0];
        var rush = state.Series[0].Chapters[0];
        var dash = state.Series[1].Chapters[0];
        Assert.Equal(aki.Id, rush.StageWork(Stage.Name).AssignedTo);
        Assert.Equal(aki.Id, dash.StageWork(Stage.Name).AssignedTo);
        Assert.Equal(aki.Id, rush.StageWork(Stage.Pencils).AssignedTo);       // queued 32h at the time: under 40
        Assert.Equal(assistant.Id, dash.StageWork(Stage.Pencils).AssignedTo); // queued 46h of names and pencils: buried
        Assert.Equal(assistant.Id, rush.StageWork(Stage.Inks).AssignedTo);
    }

    [Fact]
    public void A_manual_assignment_wins_and_a_disallowed_stage_never_goes_to_the_assistant()
    {
        var state = WithInker(out var inker);
        var chapter = state.Series[0].Chapters[0];
        state.Apply(new AssignStageCommand(chapter.Id, Stage.Backgrounds, state.People[0].Id));
        Assert.Equal(state.People[0].Id, chapter.StageWork(Stage.Backgrounds).AssignedTo);
        state.Apply(new AssignStageCommand(chapter.Id, Stage.Inks, inker.Id));
        Assert.Equal(inker.Id, chapter.StageWork(Stage.Inks).AssignedTo);
        state.Apply(new SetAllowedStagesCommand(inker.Id, new HashSet<Stage> { Stage.Tones }));
        Assert.Null(chapter.StageWork(Stage.Inks).ManualAssignee);
        Assert.Equal(state.People[0].Id, chapter.StageWork(Stage.Inks).AssignedTo);
        Assert.Equal(inker.Id, chapter.StageWork(Stage.Tones).AssignedTo);
    }

    [Fact]
    public void A_leaver_stage_is_reassigned_with_its_hours_kept()
    {
        var state = WithInker(out var inker);
        var chapter = state.Series[0].Chapters[0];
        state.Apply(new AssignStageCommand(chapter.Id, Stage.Inks, inker.Id));
        for (var i = 0; i < 24 * 7 && chapter.StageWork(Stage.Inks).HoursDone < 3; i++) state.Advance(1);
        var inks = chapter.StageWork(Stage.Inks);
        Assert.Equal(StageStatus.InProgress, inks.Status);
        var hours = inks.HoursDone;
        Assert.True(hours >= 3);

        state.Apply(new FireCommand(inker.Id));
        Assert.Equal(state.People[0].Id, inks.AssignedTo);
        Assert.Equal(hours, inks.HoursDone, 6);
        Assert.Equal(hours, inks.HoursByPerson[inker.Id], 6);
        Assert.Null(inks.ManualAssignee);
        for (var i = 0; i < 24 * 7 && inks.Status != StageStatus.Complete; i++) state.Advance(1);
        var weighted = (60 * hours + 80 * inks.HoursByPerson[state.People[0].Id]) / inks.HoursDone;
        Assert.Equal(weighted, state.WeightedSkill(inks), 6);
        GameState.FromJson(state.ToJson());
    }

    [Fact]
    public void Risk_is_judged_per_assignee()
    {
        var solo = GameState.NewGame();
        solo.Apply(new CreateSeriesCommand("Rush", "action", Cadence.Weekly, 19));
        Assert.True(solo.Series[0].Chapters[0].IsAtRisk); // 62 person-hours against 60 regular hours

        var staffed = WithInker(out _);
        Assert.False(staffed.Series[0].Chapters[0].IsAtRisk); // Aki's share is under 60 hours
        var shares = staffed.RemainingPersonHoursByPerson(staffed.Series[0].Chapters[0]);
        Assert.Equal(2, shares.Count);
        Assert.True(shares[staffed.People[0]] < 60);
    }

    [Fact]
    public void A_moonlighting_assistant_leaves_two_hours_early_and_never_works_overtime()
    {
        var state = WithInker(out var inker);
        inker.IsMoonlighting = true;
        state.RunPlanner();
        Assert.Equal(16, state.EffectiveWorkEndHour(inker));
        Assert.True(state.IsRegularHour(inker, new DateTime(1996, 4, 1, 15, 0, 0)));
        Assert.False(state.IsRegularHour(inker, new DateTime(1996, 4, 1, 16, 0, 0)));
        Assert.False(state.IsOvertimeHour(inker, new DateTime(1996, 4, 1, 18, 0, 0)));
        Assert.Equal(8, state.RegularHoursBefore(inker, new DateTime(1996, 4, 1, 8, 0, 0), new DateTime(1996, 4, 2, 0, 0, 0)));
        Assert.Equal(10, state.RegularHoursBefore(state.People[0], new DateTime(1996, 4, 1, 8, 0, 0), new DateTime(1996, 4, 2, 0, 0, 0)));
    }
}
