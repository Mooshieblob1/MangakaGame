using Xunit;

namespace MangakaSim.Tests;

/// <summary>Tier 1 fix 2: hiring runway, desk gating and missed-payday guidance.</summary>
public class HiringRunwayTests
{
    private static GameState Sold(int seed = 0)
    {
        var s = GameState.NewGame(seed); s.Apply(new CreateDoujinCommand("First pages", "adventure"));
        for (int day = 0; day < 180 && s.Series[0].Volumes.Count == 0; day++) s.Advance(24);
        s.Apply(new StudioActionCommand(StudioAction.Print, s.Series[0].Volumes.Single().Id, Amount: 10, Value: (int)PrintTier.CopyShop));
        s.Advance(24 * 8);
        return s;
    }

    /// <summary>A career whose first series has signed a serialization contract and, if requested, published its debut.</summary>
    private static GameState Serialized(bool debuted = true)
    {
        for (int seed = 0; seed < 40; seed++)
        {
            var s = Sold(seed); var series = s.Series[0];
            s.Apply(new ContinueOneShotCommand(series.Id));
            var best = CareerGuidance.PitchOutlooks(s, series).First(o => o.Open);
            s.Apply(new PitchSeriesCommand(series.Id, best.Magazine.Id));
            for (int day = 0; day < 200 && series.Publishing == PublishingStatus.Pitching; day++) s.Advance(24);
            if (series.Publishing != PublishingStatus.Offered) continue;
            s.Apply(new AcceptOfferCommand(series.Id));
            if (!debuted) return s;
            for (int day = 0; day < 200 && series.ChaptersPublished == 0; day++) s.Advance(24);
            if (series.ChaptersPublished > 0 && series.Publishing == PublishingStatus.Serialized) return s;
        }
        throw new InvalidOperationException("No seed produced a first offer and debut.");
    }

    private static Person Hire(GameState s)
    {
        var c = s.Candidates.First();
        s.Apply(new HireStaffCommand(c.Id, s.Locations.Single(l => l.BusinessId == s.ControlledBusinessId).Id, c.ExpectedSalary));
        return s.FindPerson(c.Id)!;
    }

    private static void FurnishDesk(GameState s)
    {
        var location = s.Protagonist.Employment!.LocationId;
        var arrangement = s.ArrangeOffice(location, true, true);
        if (arrangement.Purchases.Count > 0) s.Apply(new ApplyOfficeLayoutCommand(location, s.OfficeRevision, arrangement.Placements, arrangement.Purchases, []));
    }

    [Fact]
    public void New_career_runway_counts_cash_against_utilities_and_a_proposed_salary()
    {
        var s = GameState.NewGame();
        var alone = s.HiringRunway();
        Assert.Equal(s.AvailableBusinessCash, alone.Cash);
        Assert.Equal(0, alone.ConfirmedIncome);
        Assert.Equal(5000, alone.MonthlyCosts);
        Assert.True(alone.Safe);
        var hired = s.HiringRunway(StudioRules.MinimumMonthlySalary);
        Assert.Equal(5000 + StudioRules.MinimumMonthlySalary, hired.MonthlyCosts);
        Assert.False(hired.Safe);
        Assert.True(hired.Months < StudioRules.SafeRunwayMonths);
        Assert.True(new HiringRunway(0, 0, 0).Safe);
        Assert.Null(new HiringRunway(0, 0, 0).Months);
    }

    [Fact]
    public void Rent_loans_and_existing_staff_count_as_monthly_costs()
    {
        var s = GameState.NewGame(); var baseline = s.HiringRunway().MonthlyCosts;
        s.Locations.Single(l => l.BusinessId == s.ControlledBusinessId).MonthlyRent = 50000;
        Assert.Equal(baseline + 50000, s.HiringRunway().MonthlyCosts);
        s.Loans.Add(new Loan { Id = 9999, BusinessId = s.ControlledBusinessId, PersonId = s.ProtagonistPersonId, Principal = 100000,
            Original = 100000, Apr = .12m, Term = 10, Card = true, NextPayment = s.Clock.Now.AddDays(30) });
        Assert.Equal(baseline + 50000 + 10000 + 1000, s.HiringRunway().MonthlyCosts);
        var employee = Hire(s);
        Assert.Equal(baseline + 61000 + employee.Employment!.MonthlySalary, s.HiringRunway().MonthlyCosts);
        Assert.True(s.HiringRunway().Cash < s.Money);
    }

    [Fact]
    public void Doujin_sales_are_not_confirmed_income_but_a_signed_contract_is()
    {
        var sold = Sold();
        Assert.True(sold.Series[0].Volumes.Single().CopiesSold > 0);
        Assert.Equal(0, sold.HiringRunway().ConfirmedIncome);

        var signed = Serialized(debuted: false); var pending = signed.Series.Single(x => x.Publishing == PublishingStatus.Serialized);
        var expected = pending.Contract!.FirstIssueClose <= signed.Clock.Now.AddDays(StudioRules.ConfirmedIncomeDays);
        Assert.Equal(expected, signed.HiringRunway().ConfirmedIncome > 0);

        var s = Serialized(); var series = s.Series.Single(x => x.Publishing == PublishingStatus.Serialized);
        var fee = (long)series.PagesPerChapter * series.Contract!.FeePerPage; var net = fee - (long)(fee * .2m);
        var income = s.HiringRunway().ConfirmedIncome;
        Assert.True(income > 0);
        Assert.Equal(0, income % net);
        Assert.True(income / net <= StudioRules.ConfirmedIncomeDays / 7 + 1);
        var json = s.ToJson(); s.HiringRunway(StudioRules.MinimumMonthlySalary); Assert.Equal(json, s.ToJson());
    }

    [Fact]
    public void Guidance_waits_for_a_safe_runway_and_a_free_desk_before_the_first_hire()
    {
        var s = Serialized(); var p = new GuidancePreferences();
        s.ControlledBusiness.Account.Balance = 0;
        // Standard magazine fees cover a first salary, so use thin chapters to exercise the wait.
        var pages = s.Series[0].PagesPerChapter;
        s.Series[0].PagesPerChapter = 4;
        var step = CareerGuidance.Evaluate(s, p);
        Assert.Equal("serial-rhythm", step.Id);
        Assert.Contains("three months", step.Text);

        s.ControlledBusiness.Account.Balance = 5_000_000;
        s.Series[0].PagesPerChapter = pages;
        if (s.WorkplaceWithFreeDesk is null)
        {
            step = CareerGuidance.Evaluate(s, p);
            Assert.Equal("first-hire-desk", step.Id); Assert.Equal("furniture", step.Target);
            FurnishDesk(s);
        }
        Assert.NotNull(s.WorkplaceWithFreeDesk);
        step = CareerGuidance.Evaluate(s, p);
        Assert.Equal("first-hire", step.Id); Assert.Equal("recruitment", step.Target);
        Assert.All(step.Texts, t => Assert.True(t.Length <= CareerGuidance.TextLimit, t));
    }

    [Fact]
    public void Recruitment_cooldown_date_is_shown_when_no_search_can_start()
    {
        var s = Serialized(); var p = new GuidancePreferences();
        s.ControlledBusiness.Account.Balance = 5_000_000;
        FurnishDesk(s);
        foreach (var c in s.Candidates) c.ExpiresAt = s.Clock.Now;
        s.Recruitment = null; s.LastRecruitmentAt = s.Clock.Now.AddDays(-3);
        Assert.Equal(s.Clock.Now.AddDays(11), s.NextRecruitmentAt);
        var step = CareerGuidance.Evaluate(s, p);
        Assert.Equal("first-hire", step.Id);
        Assert.Contains($"{s.NextRecruitmentAt:d MMM yyyy}", step.Text);
        s.LastRecruitmentAt = s.Clock.Now.AddDays(-14);
        Assert.Null(s.NextRecruitmentAt);
        Assert.Contains("Start a candidate search", CareerGuidance.Evaluate(s, p).Text);
    }

    private static (GameState State, GameEvent Arrears) Missed(long savings)
    {
        var s = GameState.NewGame(); Hire(s);
        s.Advance(s.Clock.HoursUntil(new DateTime(1996, 7, 1)));
        s.Protagonist.PersonalAccount.Balance = savings;
        Assert.True(s.WageArrears > 0);
        return (s, s.Events.Last(e => e.Type == EventType.WageArrears));
    }

    [Fact]
    public void Missed_payday_is_texted_once_with_dates_and_a_cover_offer()
    {
        var (s, ev) = Missed(1_000_000); var p = new GuidancePreferences();
        CareerGuidance.Observe(s, p); var before = p.Thread.Count;
        var json = s.ToJson();
        Assert.True(CareerGuidance.ReportArrears(s, p, ev));
        Assert.False(CareerGuidance.ReportArrears(s, p, ev));
        Assert.Equal(json, s.ToJson());
        Assert.Equal(before + 1, p.Thread.Count);
        var texts = p.Thread[^1].Texts;
        Assert.Equal(CareerGuidance.ArrearsStep, p.Thread[^1].Step);
        Assert.Contains($"¥{s.WageArrears:N0}", texts[0]);
        Assert.Contains($"{ev.Time.Date.AddDays(21):d MMM}", texts[1]);
        Assert.Contains("Cover from savings", texts[2]);
        Assert.All(texts, t => Assert.True(t.Length <= CareerGuidance.TextLimit, t));
        CareerGuidance.Observe(s, p);
        Assert.Equal(before + 1, p.Thread.Count);

        s.Apply(new ContributeFundsCommand(s.WageArrears)); s.Advance(1);
        Assert.Equal(0, s.WageArrears);
        Assert.False(CareerGuidance.ReportArrears(s, p, ev));
    }

    [Fact]
    public void Missed_payday_without_savings_points_to_other_fixes()
    {
        var (s, ev) = Missed(0); var p = new GuidancePreferences();
        Assert.True(CareerGuidance.ReportArrears(s, p, ev));
        var texts = p.Thread[^1].Texts;
        Assert.DoesNotContain(texts, t => t.Contains("Cover from savings"));
        Assert.Contains("borrow", texts[2]);
        Assert.All(texts, t => Assert.True(t.Length <= CareerGuidance.TextLimit, t));
    }
}
