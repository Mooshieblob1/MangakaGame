using MangakaSim;
using Xunit;
using static MangakaSim.Tests.AssignmentTests;

namespace MangakaSim.Tests;

public class CostTests
{
    private static void AdvanceTo(GameState state, DateTime when) => state.Advance(state.Clock.HoursUntil(when));

    [Fact]
    public void Payroll_runs_on_the_25th_at_nine()
    {
        var state = GameState.NewGame();
        var assistant = AddAssistant(state, "Ren Ogawa", 60, salary: 220_000);
        AdvanceTo(state, new DateTime(1996, 5, 25, 9, 0, 0));
        Assert.DoesNotContain(state.Ledger, l => l.Reason == "salary");
        state.Advance(1);
        var salary = Assert.Single(state.Ledger, l => l.Reason == "salary");
        Assert.Equal(-220_000, salary.Amount);
        Assert.Equal(assistant.Id, salary.PersonId);
        Assert.Equal(new DateTime(1996, 5, 25, 10, 0, 0), salary.Time);
        Assert.Equal(1, assistant.MonthsEmployed);
        var paid = Assert.Single(state.Events, e => e.Type == EventType.PayrollPaid);
        Assert.Equal(-220_000, paid.Amount);
        Assert.Equal(new DateTime(1996, 5, 1), state.LastPayrollMonth);
        state.Advance(24);
        Assert.Single(state.Ledger, l => l.Reason == "salary");
        AdvanceTo(state, new DateTime(1996, 6, 25, 10, 0, 0));
        Assert.Equal(2, state.Ledger.Count(l => l.Reason == "salary"));
        Assert.Equal(2, assistant.MonthsEmployed);
        Assert.Equal(500_000 - 440_000, state.Money);
        GameState.FromJson(state.ToJson());
    }

    [Fact]
    public void A_missed_payroll_pays_nobody_and_hurts()
    {
        var state = GameState.NewGame();
        var assistant = AddAssistant(state, "Ren Ogawa", 60, salary: 220_000);
        state.AddLedger(-400_000, "test");
        AdvanceTo(state, new DateTime(1996, 5, 25, 10, 0, 0));
        Assert.DoesNotContain(state.Ledger, l => l.Reason == "salary");
        Assert.Single(state.Events, e => e.Type == EventType.PayrollMissed);
        Assert.Equal(1, state.Studio.MissedPayrolls);
        Assert.Equal(40, assistant.Happiness, 6);
        Assert.Equal(1, assistant.MonthsEmployed);

        state.AddLedger(1_000_000, "test");
        AdvanceTo(state, new DateTime(1996, 6, 25, 10, 0, 0));
        Assert.Single(state.Ledger, l => l.Reason == "salary");
        Assert.Equal(0, state.Studio.MissedPayrolls);
    }

    [Fact]
    public void Rent_upkeep_and_provider_are_charged_on_the_first()
    {
        var state = GameState.NewGame();
        AdvanceTo(state, new DateTime(1996, 5, 1, 10, 0, 0));
        Assert.DoesNotContain(state.Events, e => e.Type == EventType.MonthlyCostsPaid); // the garage is free

        state.Apply(new MovePremisesCommand("apartment"));
        state.Apply(new BuyAmenityCommand("fridge"));
        state.Apply(new GetOnlineCommand());
        AdvanceTo(state, new DateTime(1996, 6, 1, 9, 0, 0));
        Assert.DoesNotContain(state.Ledger, l => l.Reason == "rent");
        state.Advance(1);
        Assert.Equal(-80_000, Assert.Single(state.Ledger, l => l.Reason == "rent").Amount);
        Assert.Equal(-4_000, Assert.Single(state.Ledger, l => l.Reason == "upkeep").Amount);
        Assert.Equal(-3_000, Assert.Single(state.Ledger, l => l.Reason == "internet provider").Amount);
        var costs = Assert.Single(state.Events, e => e.Type == EventType.MonthlyCostsPaid);
        Assert.Equal(-87_000, costs.Amount);
        Assert.Equal(new DateTime(1996, 6, 1), state.LastCostsMonth);
        AdvanceTo(state, new DateTime(1996, 7, 1, 10, 0, 0));
        Assert.Equal(2, state.Ledger.Count(l => l.Reason == "rent"));
        GameState.FromJson(state.ToJson());
    }

    [Fact]
    public void Moving_validates_and_charges_a_month_up_front()
    {
        var state = GameState.NewGame();
        AddAssistant(state, "Ren Ogawa", 60);
        AddAssistant(state, "Mio Sakai", 60); // the helper moves the studio to the apartment for the third desk
        Assert.Equal("apartment", state.Studio.PremisesId);
        Assert.Throws<InvalidCommandException>(() => state.Apply(new MovePremisesCommand("penthouse")));
        Assert.Throws<InvalidCommandException>(() => state.Apply(new MovePremisesCommand("apartment")));
        Assert.Contains("holds 2", Assert.Throws<InvalidCommandException>(() => state.Apply(new MovePremisesCommand("garage"))).Message);
        state.AddLedger(-300_000, "test");
        Assert.Contains("up front", Assert.Throws<InvalidCommandException>(() => state.Apply(new MovePremisesCommand("office"))).Message);
        state.AddLedger(300_000, "test");

        var moods = state.People.Select(p => p.Happiness).ToList();
        state.Apply(new MovePremisesCommand("office"));
        Assert.Equal("office", state.Studio.PremisesId);
        Assert.Equal(state.Clock.Now, state.Studio.MovedInAt);
        Assert.Equal(-250_000, Assert.Single(state.Ledger, l => l.Reason == "moving").Amount);
        Assert.Equal(moods.Select(m => m + 5), state.People.Select(p => p.Happiness));
        var moved = Assert.Single(state.Events, e => e.Type == EventType.PremisesMoved);
        Assert.Equal(-250_000, moved.Amount);
        Assert.Equal(10, state.Atmosphere, 6);

        state.Apply(new FireCommand(state.People[2].Id));
        state.Apply(new MovePremisesCommand("garage")); // down again: free, no shock
        Assert.Equal(moods.Select(m => m + 5 - 3).Take(2), state.People.Select(p => p.Happiness));
        Assert.Single(state.Ledger, l => l.Reason == "moving");
    }

    [Fact]
    public void Buying_amenities_validates_and_charges()
    {
        var state = GameState.NewGame();
        Assert.Throws<InvalidCommandException>(() => state.Apply(new BuyAmenityCommand("jacuzzi")));
        state.Apply(new BuyAmenityCommand("fridge"));
        Assert.Equal(new[] { "fridge" }, state.Studio.Amenities);
        Assert.Equal(-90_000, Assert.Single(state.Ledger, l => l.Reason == "amenity").Amount);
        Assert.Single(state.Events, e => e.Type == EventType.AmenityBought);
        Assert.Contains("already", Assert.Throws<InvalidCommandException>(() => state.Apply(new BuyAmenityCommand("fridge"))).Message);
        state.AddLedger(-400_000, "test");
        Assert.Contains("costs", Assert.Throws<InvalidCommandException>(() => state.Apply(new BuyAmenityCommand("air-conditioner"))).Message);
        state.Apply(new BuyAmenityCommand("kettle"));
        Assert.Equal(2, state.Studio.Amenities.Count);
        GameState.FromJson(state.ToJson());
    }

    [Fact]
    public void The_convention_table_is_paid_with_the_recap()
    {
        var state = GameState.NewGame();
        state.Apply(new CreateSeriesCommand("Petals", "romance", Cadence.Monthly, 19));
        for (var i = 0; i < 24 * 60 && state.Series[0].Volumes.Count == 0; i++) state.Advance(1);
        state.Advance(24 * 45);
        var recap = state.Events.First(e => e.Type == EventType.ConventionRecap);
        Assert.Contains("table cost 20,000", recap.Message);
        var table = state.Ledger.First(l => l.Reason == "convention table");
        Assert.Equal(-20_000, table.Amount);
        Assert.Equal(recap.Time, table.Time);
    }

    [Fact]
    public void Idle_staff_online_promote_doujin_series()
    {
        var state = GameState.NewGame();
        state.Apply(new CreateSeriesCommand("Petals", "romance", Cadence.Monthly, 19));
        var idle = AddAssistant(state, "Ren Ogawa", 60, allowed: Stage.Tones); // nothing to do until the tones
        Assert.Null(idle.CurrentTask);
        var series = state.Series[0];
        Assert.Throws<InvalidCommandException>(() => state.Apply(new SetPromotionCommand(idle.Id, series.Id))); // offline
        state.Apply(new SetPromotionCommand(idle.Id, null)); // clearing never needs the internet
        state.Apply(new GetOnlineCommand());
        state.Apply(new SetPromotionCommand(idle.Id, series.Id));
        Assert.Equal(series.Id, idle.PromotionSeriesId);
        state.Advance(1);
        Assert.Equal(PromotionRules.FansPerHour(60, 0.1), series.Fanbase, 9); // 0.32 fans an hour in 1996
        state.Advance(9);
        Assert.Equal(10 * PromotionRules.FansPerHour(60, 0.1), series.Fanbase, 9);
        Assert.Equal(0, state.People[0].Skill(Stage.Name) == 80 ? 0 : 1); // Aki kept drawing
        state.Advance(14);
        Assert.Equal(10 * PromotionRules.FansPerHour(60, 0.1), series.Fanbase, 9); // nothing overnight

        state.Apply(new CreateSeriesCommand("Side", "comedy", Cadence.Monthly, 19));
        state.Apply(new SetPromotionCommand(idle.Id, PromotionRules.WholeStudio));
        var before = series.Fanbase;
        state.Advance(1);
        Assert.Equal(before + PromotionRules.FansPerHour(60, 0.1) / 2, series.Fanbase, 9);
        Assert.Equal(PromotionRules.FansPerHour(60, 0.1) / 2, state.Series[1].Fanbase, 9);

        series.Publishing = PublishingStatus.Serialized;
        series.Contract = new Contract { MagazineId = "hoshigaku-flowers", FeePerPage = 6000, SignedAt = state.Clock.Now };
        Assert.Throws<InvalidCommandException>(() => state.Apply(new SetPromotionCommand(idle.Id, series.Id)));
        GameState.FromJson(state.ToJson());
    }
}
