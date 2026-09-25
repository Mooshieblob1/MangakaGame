using System.Text.Json.Nodes;
using Xunit;

namespace MangakaSim.Tests;

public class StudioTests
{
    private static Person Hire(GameState state)
    {
        var candidate = state.Candidates.First();
        state.Apply(new HireStaffCommand(candidate.Id, state.Locations.Single(l => l.BusinessId == state.ControlledBusinessId).Id, candidate.ExpectedSalary));
        return state.FindPerson(candidate.Id)!;
    }

    [Fact]
    public void New_game_has_separate_reconciled_accounts_and_rent_free_family_home()
    {
        var state = GameState.NewGame(17, OwnershipMode.CreatorRetention);
        Assert.Equal(200000, state.PersonalMoney);
        Assert.Equal(300000, state.Money);
        Assert.Equal(OwnershipMode.CreatorRetention, state.Ownership);
        Assert.Equal(0, state.Locations.Single(l => l.BusinessId == state.ControlledBusinessId).MonthlyRent);
        Assert.Equal(2, state.Locations.Single(l => l.BusinessId == state.ControlledBusinessId).Seats);
        Assert.True(state.Protagonist.IsProdigy);
        Assert.Equal(state.ToJson(), GameState.FromJson(state.ToJson()).ToJson());
        Assert.Equal(0, state.Ledger.Single().Amount + state.Protagonist.PersonalAccount.Entries.Single().Amount);
    }

    [Fact]
    public void Location_is_seeded_and_does_not_change_home_capacity_or_market_rng()
    {
        var wards = Enumerable.Range(0, 100).Select(seed => GameState.NewGame(seed).Locations.Single(l => l.IsFamilyHome).District).ToHashSet();
        Assert.Equal(6, wards.Count);
        var a = GameState.NewGame(5); var b = GameState.NewGame(5);
        a.Apply(new RecruitStaffCommand(Stage.Inks));
        Assert.Equal(a.Rng.State, b.Rng.State);
        Assert.Equal(a.LocationRng.State, b.LocationRng.State);
    }

    [Fact]
    public void Contribution_is_paired_and_rejected_transfer_changes_nothing()
    {
        var state = GameState.NewGame();
        state.Apply(new ContributeFundsCommand(10000));
        Assert.Equal(190000, state.PersonalMoney);
        Assert.Equal(310000, state.Money);
        Assert.Equal(state.Ledger.Last().TransferId, state.Protagonist.PersonalAccount.Entries.Last().TransferId);
        var before = state.ToJson();
        foreach (var amount in new[] { 0L, -1, 190001, long.MaxValue })
        {
            Assert.Throws<InvalidCommandException>(() => state.Apply(new ContributeFundsCommand(amount)));
            Assert.Equal(before, state.ToJson());
        }
        Assert.Equal(before, GameState.FromJson(before).ToJson());
    }

    [Fact]
    public void Hiring_reserves_a_desk_and_starts_next_weekday_without_spending_personal_savings()
    {
        var state = GameState.NewGame(); var employee = Hire(state);
        Assert.Equal(new DateTime(1996, 4, 2, 9, 0, 0), employee.Employment!.StartsAt);
        Assert.Equal(200000, state.PersonalMoney);
        Assert.Equal(300000, state.Money);
        Assert.True(state.ReservedWages > 0);
        var before = state.ToJson();
        Assert.Throws<InvalidCommandException>(() => Hire(state));
        Assert.Equal(before, state.ToJson());
        Assert.Equal(before, GameState.FromJson(before).ToJson());
    }

    [Fact]
    public void Low_salary_and_duplicate_offer_do_not_reroll_or_mutate()
    {
        var state = GameState.NewGame(); var candidate = state.Candidates.First(); var before = state.ToJson();
        for (var i = 0; i < 3; i++)
        {
            Assert.Throws<InvalidCommandException>(() => state.Apply(new HireStaffCommand(candidate.Id, state.Locations.Single(l => l.BusinessId == state.ControlledBusinessId).Id, 1)));
            Assert.Equal(before, state.ToJson());
        }
        state.Apply(new HireStaffCommand(candidate.Id, state.Locations.Single(l => l.BusinessId == state.ControlledBusinessId).Id, candidate.ExpectedSalary));
        before = state.ToJson();
        Assert.Throws<InvalidCommandException>(() => state.Apply(new HireStaffCommand(candidate.Id, state.Locations.Single(l => l.BusinessId == state.ControlledBusinessId).Id, candidate.ExpectedSalary)));
        Assert.Equal(before, state.ToJson());
    }

    [Fact]
    public void Search_cost_cooldown_results_and_mid_search_save_are_deterministic()
    {
        var state = GameState.NewGame(24);
        state.Apply(new RecruitStaffCommand(Stage.Backgrounds));
        Assert.Equal(280000, state.Money);
        var before = state.ToJson();
        Assert.Throws<InvalidCommandException>(() => state.Apply(new RecruitStaffCommand()));
        Assert.Equal(before, state.ToJson());
        var loaded = GameState.FromJson(before);
        state.Advance(7 * 24); loaded.Advance(7 * 24);
        Assert.Equal(state.ToJson(), loaded.ToJson());
        Assert.Null(state.Recruitment);
        Assert.Equal(3, state.Candidates.Count(c => c.Recruited));
        Assert.Throws<InvalidCommandException>(() => state.Apply(new RecruitStaffCommand()));
    }

    [Fact]
    public void Salary_accrues_by_calendar_days_and_pays_personal_account_once()
    {
        var state = GameState.NewGame(); var employee = Hire(state);
        var salary = employee.Employment!.MonthlySalary;
        state.Advance(state.Clock.HoursUntil(new DateTime(1996, 5, 1)));
        var amount = (long)decimal.Floor(salary * 29m / 30m);
        Assert.Equal(amount, employee.PersonalAccount.Balance);
        Assert.Equal(300000 - amount + state.Ledger.Where(e => e.Kind == AccountEntryKind.Expense).Sum(e => e.Amount), state.Money);
        Assert.Equal(200000, state.PersonalMoney);
        Assert.All(state.WageObligations, o => Assert.Equal(0, o.Remaining));
        var loaded = GameState.FromJson(state.ToJson()); loaded.Advance(1);
        Assert.Equal(amount, loaded.FindPerson(employee.Id)!.PersonalAccount.Balance);
    }

    [Fact]
    public void Shortfall_becomes_arrears_not_a_personal_debit_and_can_be_paid_after_contribution()
    {
        var state = GameState.NewGame(); var employee = Hire(state);
        state.Advance(state.Clock.HoursUntil(new DateTime(1996, 7, 1)));
        Assert.True(state.WageArrears > 0);
        Assert.Equal(0, state.Money);
        Assert.Equal(200000, state.PersonalMoney);
        var arrears = state.WageArrears;
        state.Apply(new ContributeFundsCommand(arrears)); state.Advance(1);
        Assert.Equal(0, state.WageArrears);
        Assert.Equal(200000 - arrears, state.PersonalMoney);
        Assert.Equal(state.ToJson(), GameState.FromJson(state.ToJson()).ToJson());
    }

    [Fact]
    public void Full_month_salary_is_not_lost_to_daily_decimal_division()
    {
        var state = GameState.NewGame();
        state.Advance(state.Clock.HoursUntil(new DateTime(1996, 4, 23, 10, 0, 0)));
        state.Apply(new RecruitStaffCommand());
        state.Advance(state.Clock.HoursUntil(new DateTime(1996, 4, 30, 10, 0, 0)));
        var employee = Hire(state);
        var salary = employee.Employment!.MonthlySalary;
        state.Advance(state.Clock.HoursUntil(new DateTime(1996, 6, 1)));
        Assert.Equal(salary, employee.PersonalAccount.Balance);
        Assert.Equal(300000 - salary + state.Ledger.Where(e => e.Kind == AccountEntryKind.Expense).Sum(e => e.Amount), state.Money);
    }

    [Theory]
    [InlineData("EmploymentHistory")][InlineData("PersonalAccount")]
    public void Null_person_collections_produce_a_clear_load_error(string field)
    {
        var json = JsonNode.Parse(GameState.NewGame().ToJson())!;
        json["People"]![0]![field] = null;
        Assert.Throws<InvalidDataException>(() => GameState.FromJson(json.ToJsonString()));
    }

    [Fact]
    public void Dismissal_keeps_history_and_thirty_days_pay_and_cannot_remove_protagonist()
    {
        var state = GameState.NewGame(); var employee = Hire(state);
        state.Advance(25);
        state.Apply(new DismissStaffCommand(employee.Id));
        var end = employee.Employment!.NoticeEndsAt!.Value;
        Assert.Equal(state.Clock.Now.Date.AddDays(30), end);
        state.Advance(state.Clock.HoursUntil(end));
        Assert.Null(employee.Employment);
        Assert.Contains(employee, state.People);
        Assert.True(employee.PersonalAccount.Balance > 0);
        Assert.Throws<InvalidCommandException>(() => state.Apply(new DismissStaffCommand(state.ProtagonistPersonId)));
        Assert.Equal(state.ToJson(), GameState.FromJson(state.ToJson()).ToJson());
    }

    [Fact]
    public void Name_is_reserved_for_lead_and_employee_schedule_cannot_hide_overtime()
    {
        var state = GameState.NewGame(); var employee = Hire(state);
        state.Apply(new CreateSeriesCommand("Team", "action", Cadence.Monthly, 19));
        var chapter = state.Series.Single().Chapters.Single();
        Assert.Throws<InvalidCommandException>(() => state.Apply(new AssignStageCommand(chapter.Id, Stage.Name, employee.Id)));
        Assert.Throws<InvalidCommandException>(() => state.Apply(new SetScheduleCommand(employee.Id, 8, 18, [DayOfWeek.Sunday])));
        state.Apply(new AssignStageCommand(chapter.Id, Stage.Inks, employee.Id));
        state.Advance(25);
        Assert.Equal(employee.Id, chapter.StageWork(Stage.Inks).AssignedTo);
        Assert.Equal(state.ProtagonistPersonId, chapter.StageWork(Stage.Name).AssignedTo);
    }

    [Fact]
    public void Expert_handover_uses_actual_work_quality_not_last_worker()
    {
        var state = GameState.NewGame(); var employee = Hire(state);
        employee.Skills[Stage.Pencils] = 20;
        state.Advance(25);
        state.Apply(new CreateSeriesCommand("Team", "action", Cadence.Monthly, 19));
        var chapter = state.Series.Single().Chapters.Single();
        state.Apply(new SkipStageCommand(chapter.Id, Stage.Name));
        state.Apply(new AssignStageCommand(chapter.Id, Stage.Pencils, employee.Id));
        var pencils = chapter.StageWork(Stage.Pencils);
        PublishingTests.Until(state, () => pencils.HoursDone >= 20);
        state.Apply(new AssignStageCommand(chapter.Id, Stage.Pencils, state.ProtagonistPersonId));
        PublishingTests.Until(state, () => pencils.IsDone);
        Assert.True(pencils.Contribution < 20); // full skill-80 pencils would contribute 25.2
        Assert.True(pencils.HoursByPerson.ContainsKey(employee.Id));
        Assert.True(pencils.HoursByPerson.ContainsKey(state.ProtagonistPersonId));
        Assert.Equal(state.ToJson(), GameState.FromJson(state.ToJson()).ToJson());
    }

    [Fact]
    public void Completion_does_not_unlock_another_workers_stage_in_same_hour()
    {
        var state = GameState.NewGame(); var employee = Hire(state);
        state.Advance(25);
        state.Apply(new CreateSeriesCommand("Short", "action", Cadence.Monthly, 1));
        var chapter = state.Series.Single().Chapters.Single();
        state.Apply(new AssignStageCommand(chapter.Id, Stage.Pencils, employee.Id));
        state.Advance(1);
        Assert.True(chapter.StageWork(Stage.Name).IsDone);
        Assert.Equal(0, chapter.StageWork(Stage.Pencils).HoursDone);
        state.Advance(1);
        Assert.True(chapter.StageWork(Stage.Pencils).HoursDone > 0);
    }

    [Fact]
    public void Staff_commands_round_trip_and_replay_with_ownership_setting()
    {
        var state = GameState.NewGame(25, OwnershipMode.CreatorRetention);
        state.Apply(new ContributeFundsCommand(50000));
        var employee = Hire(state);
        state.Apply(new RecruitStaffCommand(Stage.Name));
        state.Advance(48);
        state.Apply(new CreateSeriesCommand("Team", "action", Cadence.Monthly, 19));
        state.Apply(new AssignStaffCommand(employee.Id, state.Series.Single().Id));
        state.Advance(24 * 40);
        var loaded = GameState.FromJson(state.ToJson());
        var replay = GameState.NewGame(25, OwnershipMode.CreatorRetention);
        foreach (var entry in loaded.CommandLog)
        { replay.Advance(replay.Clock.HoursUntil(entry.Time)); replay.Apply(entry.Command); }
        replay.Advance(replay.Clock.HoursUntil(state.Clock.Now));
        Assert.Equal(state.ToJson(), replay.ToJson());
    }

    [Theory]
    [InlineData("ProtagonistPersonId")][InlineData("Ownership")][InlineData("Businesses")]
    [InlineData("Locations")][InlineData("StaffRng")][InlineData("WageObligations")]
    public void New_required_state_cannot_silently_default(string field)
    {
        var json = JsonNode.Parse(GameState.NewGame().ToJson())!.AsObject(); json.Remove(field);
        Assert.Throws<InvalidDataException>(() => GameState.FromJson(json.ToJsonString()));
    }

    [Theory]
    [InlineData("balance")][InlineData("transfer")][InlineData("rent")][InlineData("id")]
    [InlineData("candidate")][InlineData("employment")]
    public void Invalid_financial_or_staff_state_is_rejected(string mutation)
    {
        var json = JsonNode.Parse(GameState.NewGame().ToJson())!;
        switch (mutation)
        {
            case "balance": json["Businesses"]![0]!["Account"]!["Balance"] = 1; break;
            case "transfer": json["People"]![0]!["PersonalAccount"]!["Entries"]![0]!["TransferId"] = 999; break;
            case "rent": json["Locations"]![0]!["MonthlyRent"] = 1; break;
            case "id": json["Businesses"]![0]!["Id"] = 1; break;
            case "candidate": json["Candidates"]![0]!["AcceptanceRoll"] = 2; break;
            case "employment": json["People"]![0]!["EmploymentHistory"]![0]!["BusinessId"] = 999; break;
        }
        Assert.Throws<InvalidDataException>(() => GameState.FromJson(json.ToJsonString()));
    }

    [Fact]
    public void Version_two_is_explicitly_rejected_with_new_game_explanation()
    {
        var json = JsonNode.Parse(GameState.NewGame().ToJson())!; json["Version"] = 2;
        Assert.Contains("new game", Assert.Throws<InvalidDataException>(() => GameState.FromJson(json.ToJsonString())).Message);
    }
}
