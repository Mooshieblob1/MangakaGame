using System.Text.Json.Nodes;
using Xunit;

namespace MangakaSim.Tests;

public class StudioOperationsTests
{
    private static GameState Master()
    {
        var s=PublishingTests.Started();
        PublishingTests.Until(s,()=>s.Series[0].Volumes.Count>0);
        return s;
    }
    private static void RoundTrip(GameState s)
    {
        var copy=GameState.FromJson(s.ToJson()); Assert.Equal(s.ToJson(),copy.ToJson());
        s.Advance(48); copy.Advance(48); Assert.Equal(s.ToJson(),copy.ToJson());
    }
    [Fact] public void Manuscript_is_not_stock_and_print_delivery_starts_sales_window()
    {
        var s=Master(); var v=s.Series[0].Volumes[0];
        Assert.Null(v.ReleasedAt); Assert.Equal(0,s.Stock(v.Id));
        s.Advance(24*14); Assert.Equal(0,v.CopiesSold); Assert.Equal(0,v.WeeksOnSale);
        s.Apply(new StudioActionCommand(StudioAction.Print,v.Id,Amount:50));
        Assert.Equal(0,s.Stock(v.Id)); s.Advance(24); Assert.Equal(50,s.Stock(v.Id)); Assert.NotNull(v.ReleasedAt);
        RoundTrip(s);
    }
    [Theory] [InlineData(PrintTier.CopyShop,52000)] [InlineData(PrintTier.LocalPrinter,32000)] [InlineData(PrintTier.BulkPrinter,29000)]
    public void Page_costs_follow_documented_estimates(PrintTier tier,long expected) => Assert.Equal(expected,GameState.PrintingCost(tier,100,100));
    [Fact] public void Sales_cannot_exceed_paid_stock_and_creator_share_uses_net_contribution()
    {
        var s=Master(); var v=s.Series[0].Volumes[0];
        s.Apply(new StudioActionCommand(StudioAction.Print,v.Id,Amount:10)); s.Advance(24*40);
        Assert.InRange(v.CopiesSold,1,10); Assert.Equal(10,v.CopiesSold+s.Stock(v.Id));
        Assert.Equal(Math.Max(0,(long)Math.Floor(v.Contribution*.2)),v.CreatorAccrued);
        RoundTrip(s);
    }
    [Fact] public void Automatic_printing_respects_budget_pending_order_and_storage()
    {
        var s=Master();var title=s.Series[0];
        s.Apply(new StudioActionCommand(StudioAction.AutoPrint,title.Id,1,1000,50,true)); s.Advance(48); Assert.Empty(s.PrintRuns);
        s.Apply(new StudioActionCommand(StudioAction.AutoPrint,title.Id,1,10000,50,true)); s.Advance(24);
        Assert.Single(s.PrintRuns); Assert.True(s.PrintRuns.Sum(r=>r.Cost)<=10000); RoundTrip(s);
    }
    [Fact] public void Paid_move_has_deposit_capacity_travel_and_retains_zero_rent_home_history()
    {
        var s=PublishingTests.Started(); var old=s.Locations.Single(l=>l.BusinessId==s.ControlledBusinessId);
        s.Apply(new StudioActionCommand(StudioAction.Move,1));
        var place=s.Locations.Single(l=>!l.Closed&&l.BusinessId==s.ControlledBusinessId); Assert.Equal(4,place.Seats); Assert.Equal(120000,place.Deposit);
        Assert.True(old.Closed); Assert.Equal(0,old.MonthlyRent); Assert.Equal(place.Id,s.Protagonist.Employment!.LocationId);
        Assert.Equal(place.Id,s.Series[0].LocationId); Assert.True(s.Protagonist.BusyUntil>s.Clock.Now);
        RoundTrip(s);
    }
    [Fact] public void Branch_requires_scale_and_failure_is_atomic()
    {
        var s=GameState.NewGame(); var before=s.ToJson();
        Assert.Throws<InvalidCommandException>(()=>s.Apply(new StudioActionCommand(StudioAction.Lease,1))); Assert.Equal(before,s.ToJson());
    }
    [Fact] public void Personal_credit_is_not_business_cash_and_repayment_preserves_ledger()
    {
        var s=GameState.NewGame(); s.Apply(new StudioActionCommand(StudioAction.BorrowPersonal,Amount:100000));
        Assert.Equal(300000,s.PersonalMoney); Assert.Equal(300000,s.Money);
        s.Advance(24); Assert.True(s.Loans[0].Interest>0);
        s.Apply(new StudioActionCommand(StudioAction.RepayLoan,s.Loans[0].Id,Amount:10000));
        Assert.True(s.Loans[0].Principal<100000); RoundTrip(s);
    }
    [Fact] public void Card_and_personal_loan_share_ceiling_and_rejection_does_not_draw_money()
    {
        var s=GameState.NewGame();s.Apply(new StudioActionCommand(StudioAction.BorrowPersonal,Amount:450000));
        var before=s.ToJson(); Assert.Throws<InvalidCommandException>(()=>s.Apply(new StudioActionCommand(StudioAction.BorrowCard,Amount:100000)));Assert.Equal(before,s.ToJson());
    }
    [Fact] public void Cashless_commission_consumes_four_hours_and_earns_once_without_skill_or_fans()
    {
        var s=PublishingTests.Started();var p=s.Protagonist;var skill=p.Skill(Stage.Name);var progress=s.Series[0].Chapters[0].Stages.Sum(w=>w.HoursDone);
        s.Apply(new StudioActionCommand(StudioAction.RecoveryCommission));s.Advance(4);
        Assert.Equal(progress,s.Series[0].Chapters[0].Stages.Sum(w=>w.HoursDone));Assert.Equal(skill,p.Skill(Stage.Name));
        Assert.Single(s.Ledger,e=>e.Reason=="recovery commission");Assert.Equal(0,s.Series[0].Fanbase);
        Assert.Throws<InvalidCommandException>(()=>s.Apply(new StudioActionCommand(StudioAction.RecoveryCommission)));RoundTrip(s);
    }
    [Fact] public void Needs_force_breaks_and_learning_does_not_rewrite_completed_work()
    {
        var s=PublishingTests.Started(pages:19);s.Advance(10);
        Assert.Equal(9,s.Protagonist.HoursWorkedToday);Assert.InRange(s.Protagonist.Drink,1,100);Assert.True(s.Protagonist.Experience[Stage.Name]>0);
        RoundTrip(s);
    }
    [Fact] public void Weekly_overtime_cap_holds_and_resets_on_monday()
    {
        var s=GameState.NewGame();s.Apply(new CreateSeriesCommand("Hard","drama",Cadence.Weekly,100));DeadlineFixture.Attach(s);
        s.Apply(new StudioActionCommand(StudioAction.SetWellbeing,s.ProtagonistPersonId,3,Value:2));s.Advance(24*5);
        Assert.InRange(s.Protagonist.WeeklyOvertime,1,3);s.Advance(s.Clock.HoursUntil(new DateTime(1996,4,8)));Assert.Equal(0,s.Protagonist.WeeklyOvertime);
    }
    [Fact] public void Pipeline_allows_overlap_then_stops_at_unprinted_master_limit()
    {
        var s=PublishingTests.Started(pages:19);var series=s.Series[0];
        PublishingTests.Until(s,()=>series.Chapters.Count==2);Assert.NotEqual(ChapterStatus.Complete,series.Chapters[0].Status);
        PublishingTests.Until(s,()=>series.Volumes.Count==1);s.Advance(24*60);
        Assert.Single(series.Volumes);Assert.InRange(series.Chapters.Count,5,7);RoundTrip(s);
    }
    [Fact] public void Nearby_free_convention_still_reserves_time_and_cannot_double_book()
    {
        var s=Master();s.Apply(new StudioActionCommand(StudioAction.Print,s.Series[0].Volumes[0].Id,Amount:100));
        s.Apply(new StudioActionCommand(StudioAction.BookConvention,s.ProtagonistPersonId));var booking=s.Bookings.Single();
        Assert.Equal(0,booking.Fee);Assert.Equal(0,booking.TravelCost);Assert.Equal(1,booking.TravelHours);
        var before=s.ToJson();Assert.Throws<InvalidCommandException>(()=>s.Apply(new StudioActionCommand(StudioAction.BookConvention,s.ProtagonistPersonId)));Assert.Equal(before,s.ToJson());
        s.Advance(s.Clock.HoursUntil(booking.Date.AddHours(18)));Assert.True(booking.Settled);Assert.True(booking.StaffedHours>0);RoundTrip(s);
    }
    [Fact] public void Travel_is_free_locally_and_scales_across_greater_tokyo()
    {
        var local=TokyoProperties.Travel("Nerima","Nerima");var far=TokyoProperties.Travel("Nerima","Chiba");
        Assert.Equal(0,local.Fare);Assert.True(local.Hours>0);Assert.True(far.Fare>local.Fare);Assert.True(far.Hours>local.Hours);
    }
    [Fact] public void Priority_promotion_and_campaign_use_worker_hours_and_bounded_cash()
    {
        var s=PublishingTests.Started(pages:19);var title=s.Series[0];
        s.Apply(new StudioActionCommand(StudioAction.Promote,title.Id,s.ProtagonistPersonId,Enabled:true));
        s.Apply(new StudioActionCommand(StudioAction.Campaign,title.Id));s.Advance(24*14);
        Assert.Equal(8,title.CampaignHours);Assert.InRange(title.Reach,0,20);Assert.Equal(-1000,s.Ledger.Single(e=>e.Reason=="campaign setup").Amount);RoundTrip(s);
    }
    [Theory] [InlineData(OwnershipMode.StudioRetention)] [InlineData(OwnershipMode.CreatorRetention)]
    public void Career_follows_protagonist_and_future_title_but_existing_books_and_debt_stay(OwnershipMode ownership)
    {
        var s=Master();s.Ownership=ownership;var title=s.Series[0];var old=s.ControlledBusinessId;
        s.Apply(new StudioActionCommand(StudioAction.Print,title.Volumes[0].Id,Amount:100));s.Advance(24);
        s.Apply(new StudioActionCommand(StudioAction.BorrowPersonal,Amount:100000));var loan=s.Loans.Single();
        s.Apply(new StudioActionCommand(StudioAction.CareerHome,Amount:10000));s.Advance(24);
        Assert.NotEqual(old,s.ControlledBusinessId);Assert.Equal(s.ControlledBusinessId,title.BusinessId);Assert.Equal(SeriesStatus.Active,title.Status);
        Assert.Equal(old,title.Volumes[0].BusinessId);Assert.Null(loan.BusinessId);Assert.True(s.Businesses.Single(b=>b.Id==old).Independent);
        Assert.Equal(0,s.Locations.Single(l=>l.Id==s.Protagonist.Employment!.LocationId).MonthlyRent);RoundTrip(s);
    }
    [Fact] public void Employer_route_is_always_available_but_does_not_grant_owner_authority()
    {
        var s=GameState.NewGame();s.Apply(new StudioActionCommand(StudioAction.CareerEmployer,Value:1));s.Advance(24);
        Assert.Equal(ControlMode.EmployedLead,s.Control);Assert.Equal(150000,s.Protagonist.Employment!.MonthlySalary);
        Assert.Throws<InvalidCommandException>(()=>s.Apply(new ContributeFundsCommand(1000)));RoundTrip(s);
    }
    [Theory] [InlineData("Food",-1)] [InlineData("Happiness",101)] [InlineData("WeeklyOvertimeLimit",100)]
    public void Corrupted_wellbeing_is_rejected(string field,int value)
    {
        var json=JsonNode.Parse(GameState.NewGame().ToJson())!;json["People"]![0]![field]=value;
        Assert.Throws<InvalidDataException>(()=>GameState.FromJson(json.ToJsonString()));
    }
    [Fact] public void Rejected_compound_purchase_preserves_rng_ids_money_and_command_history()
    {
        var s=GameState.NewGame();var before=s.ToJson();
        Assert.Throws<InvalidCommandException>(()=>s.Apply(new StudioActionCommand(StudioAction.Move,16)));Assert.Equal(before,s.ToJson());
    }
    [Fact] public void Operations_commands_replay_across_career_change()
    {
        var s=GameState.NewGame(3);s.Apply(new StudioActionCommand(StudioAction.BorrowPersonal,Amount:100000));
        s.Apply(new StudioActionCommand(StudioAction.RecoveryCommission));s.Advance(24);
        s.Apply(new StudioActionCommand(StudioAction.Move,1));s.Advance(48);
        s.Apply(new StudioActionCommand(StudioAction.CareerHome,Amount:10000));s.Advance(72);
        var replay=GameState.NewGame(3);
        foreach(var c in s.CommandLog){replay.Advance(replay.Clock.HoursUntil(c.Time));replay.Apply(c.Command);}
        replay.Advance(replay.Clock.HoursUntil(s.Clock.Now));Assert.Equal(s.ToJson(),replay.ToJson());RoundTrip(s);
    }
    [Fact] public void Rare_uncle_introduction_guarantees_prodigy_but_reveals_only_after_work()
    {
        var s=GameState.NewGame();s.Apply(new StudioActionCommand(StudioAction.BorrowPersonal,Amount:500000));s.Apply(new ContributeFundsCommand(700000));
        s.Apply(new StudioActionCommand(StudioAction.Move,1));
        OfficeTests.Furnish(s,s.Protagonist.Employment!.LocationId);
        var candidate=s.Candidates[0];s.Apply(new HireStaffCommand(candidate.Id,s.Protagonist.Employment!.LocationId,candidate.ExpectedSalary));
        s.Advance(s.Clock.HoursUntil(new DateTime(1996,8,1).AddHours(-1)));
        var sponsor=s.FindPerson(candidate.Id)!;sponsor.Loyalty=80;
        var seed=Enumerable.Range(0,100000).First(i=>Rng.FromSeed(i).NextDouble()<.005);s.StaffRng=Rng.FromSeed(seed);
        s.Advance(1);var uncle=Assert.Single(s.Candidates,c=>c.HiddenTalent);
        Assert.Equal(CandidateProfile.Prodigy,uncle.Profile);
        s.ControlledBusiness.Account.OpeningBalance+=200000;s.Money+=200000; // Isolate the talent event from startup cash scarcity.
        s.Apply(new HireStaffCommand(uncle.Id,s.Protagonist.Employment.LocationId,uncle.ExpectedSalary));
        var person=s.FindPerson(uncle.Id)!;
        s.Apply(new CreateSeriesCommand("Uncle's story","drama",Cadence.Monthly,19));s.Apply(new AssignStaffCommand(person.Id,s.Series[0].Id,true));
        PublishingTests.Until(s,()=>person.ProductiveHours>=8);Assert.False(person.HiddenTalent);Assert.True(person.IsProdigy);RoundTrip(s);
    }
    [Fact] public void Creator_retention_and_studio_retention_differ_for_hired_leads()
    {
        foreach(var ownership in Enum.GetValues<OwnershipMode>())
        {
            var s=GameState.NewGame(0,ownership);s.Apply(new StudioActionCommand(StudioAction.BorrowPersonal,Amount:500000));s.Apply(new ContributeFundsCommand(600000));
            var c=s.Candidates[0];s.Apply(new HireStaffCommand(c.Id,s.Locations[0].Id,c.ExpectedSalary));s.Advance(25);
            s.Apply(new CreateSeriesCommand("Employee title","drama",Cadence.Monthly,19));s.Apply(new AssignStaffCommand(c.Id,s.Series[0].Id,true));
            s.Advance(s.Clock.HoursUntil(new DateTime(1996,6,1)));
            var old=s.ControlledBusinessId;s.Apply(new StudioActionCommand(StudioAction.DismissBuyout,c.Id));
            if(ownership==OwnershipMode.CreatorRetention) {Assert.NotEqual(old,s.Series[0].BusinessId);Assert.True(s.Businesses.Single(b=>b.Id==s.Series[0].BusinessId).Independent);}
            else Assert.Equal(old,s.Series[0].BusinessId);
            Assert.Equal(c.Id,s.Series[0].RightsLeadPersonId);RoundTrip(s);
        }
    }
    [Fact] public void Quotes_do_not_reroll_followers_when_reopened_or_loaded()
    {
        var s=GameState.NewGame();var c=s.Candidates[0];s.Apply(new HireStaffCommand(c.Id,s.Locations[0].Id,c.ExpectedSalary));s.Advance(25);
        var quote=new StudioActionCommand(StudioAction.QuoteCareer,(int)StudioAction.CareerEmployer,Value:1);
        s.Apply(quote);var roll=s.FollowerRolls[c.Id];var preview=s.CareerPreview();
        s.Apply(new StudioActionCommand(StudioAction.QuoteCareer,(int)StudioAction.CareerHome));s.Apply(quote);
        Assert.Equal(roll,s.FollowerRolls[c.Id]);Assert.Equal(preview,s.CareerPreview());RoundTrip(s);
    }
    [Fact] public void Incorporation_keeps_capital_and_existing_personal_debt()
    {
        var s=GameState.NewGame();s.Apply(new StudioActionCommand(StudioAction.BorrowPersonal,Amount:100000));
        // A reconciled publisher advance is a controlled late-game finance fixture.
        s.ControlledBusiness.Account.OpeningBalance+=4000000;s.Money+=4000000;
        var balance=s.Money;s.Apply(new StudioActionCommand(StudioAction.Incorporate));
        Assert.Equal(balance-200000,s.Money);Assert.Null(s.Loans[0].BusinessId);Assert.True(s.ControlledBusiness.Incorporated);
        Assert.Throws<InvalidCommandException>(()=>s.Apply(new StudioActionCommand(StudioAction.BorrowPersonal,Amount:100000)));RoundTrip(s);
    }
    [Fact] public void Loan_interest_is_simple_and_missed_instalments_do_not_compound_interest()
    {
        var s=GameState.NewGame();s.Apply(new StudioActionCommand(StudioAction.BorrowPersonal,Amount:100000));s.Apply(new ContributeFundsCommand(s.PersonalMoney));
        var loan=s.Loans[0];s.Advance(24*60);
        Assert.Equal(100000,loan.Principal);Assert.InRange(loan.Interest,3900m,4000m);Assert.True(loan.Arrears>0);RoundTrip(s);
    }
    [Fact] public void A_lease_charges_its_next_rent_on_anniversary_not_next_calendar_day()
    {
        var s=GameState.NewGame();s.Advance(s.Clock.HoursUntil(new DateTime(1996,4,30,8,0,0)));
        s.Apply(new StudioActionCommand(StudioAction.Move,1));s.Advance(24);
        Assert.DoesNotContain(s.Bills,b=>b.Reason=="rent"&&b.BusinessId==s.ControlledBusinessId);Assert.Equal(new DateTime(1996,5,30),s.Locations.Single(l=>!l.Closed&&l.BusinessId==s.ControlledBusinessId).NextRentAt);RoundTrip(s);
    }
    [Theory] [InlineData("Loans")] [InlineData("Bookings")] [InlineData("PrintRuns")] [InlineData("Bills")] [InlineData("FollowerDecisionHistory")]
    public void Null_operation_collections_are_rejected(string field)
    {
        var json=JsonNode.Parse(GameState.NewGame().ToJson())!;json[field]=null;Assert.Throws<InvalidDataException>(()=>GameState.FromJson(json.ToJsonString()));
    }
    [Fact] public void Branch_unlock_and_cross_site_lead_transfer_use_real_capacity_and_travel()
    {
        var s=GameState.NewGame();s.ControlledBusiness.Account.OpeningBalance+=3000000;s.Money+=3000000;
        s.Apply(new StudioActionCommand(StudioAction.Move,5));
        OfficeTests.Furnish(s,s.Protagonist.Employment!.LocationId);
        foreach(var c in s.Candidates.Take(5).ToArray())s.Apply(new HireStaffCommand(c.Id,s.Protagonist.Employment!.LocationId,c.ExpectedSalary));
        s.Apply(new CreateSeriesCommand("One","drama",Cadence.Monthly,19));s.Apply(new CreateSeriesCommand("Two","drama",Cadence.Monthly,19));
        s.Apply(new StudioActionCommand(StudioAction.Lease,1));var branch=s.Locations.Last(); OfficeTests.Furnish(s,branch.Id); var lead=s.ControlledStaff.First(p=>p.Id!=s.ProtagonistPersonId);
        s.Apply(new AssignStaffCommand(lead.Id,s.Series[1].Id,true));s.Apply(new StudioActionCommand(StudioAction.TransferStaff,branch.Id,lead.Id));
        Assert.Equal(branch.Id,s.Series[1].LocationId);Assert.True(lead.BusyUntil>s.Clock.Now);Assert.Equal(2,s.Locations.Count(l=>!l.Closed&&l.BusinessId==s.ControlledBusinessId));RoundTrip(s);
    }
    [Fact] public void Career_novation_preserves_contract_and_published_business_history()
    {
        var s=SimulationFixture.EightPublished();var title=s.Series[0];var old=s.ControlledBusinessId;var contract=title.Contract!;
        var nextClose=s.Markets.Single(m=>m.MagazineId==contract.MagazineId).NextIssueClose;
        var published=title.Chapters.Where(c=>c.PublishedAt is not null).ToArray();
        s.Apply(new StudioActionCommand(StudioAction.CareerEmployer,Value:1));s.Advance(24);
        Assert.Same(contract,title.Contract);Assert.Equal(nextClose,s.Markets.Single(m=>m.MagazineId==contract.MagazineId).NextIssueClose);
        Assert.All(published,c=>Assert.Equal(old,c.PublishedBusinessId));Assert.Equal(s.ControlledBusinessId,title.BusinessId);RoundTrip(s);
    }
    [Fact] public void Released_stock_receipts_go_to_old_business_after_move()
    {
        var s=Master();var book=s.Series[0].Volumes[0];s.Apply(new StudioActionCommand(StudioAction.Print,book.Id,Amount:100));s.Advance(24);
        var old=s.ControlledBusiness;s.Apply(new StudioActionCommand(StudioAction.CareerHome));s.Advance(24*14);
        Assert.Contains(old.Account.Entries,e=>e.Reason=="doujin sales"&&e.Amount>0);
        Assert.DoesNotContain(s.Ledger,e=>e.Reason=="doujin sales");Assert.Equal(100,book.CopiesSold+s.Stock(book.Id));RoundTrip(s);
    }
    [Fact] public void Creator_royalties_settle_monthly_after_actual_receipts()
    {
        var s=SimulationFixture.Serialized();PublishingTests.Until(s,()=>s.Bills.Any(b=>b.Reason=="creator share"));
        var royalty=s.Bills.First(b=>b.Reason=="creator share");Assert.True(royalty.DueAt>s.Clock.Now);Assert.Equal(royalty.Original,royalty.Remaining);
        s.Advance(s.Clock.HoursUntil(royalty.DueAt));Assert.Equal(0,royalty.Remaining);
        Assert.Contains(s.Protagonist.PersonalAccount.Entries,e=>e.Reason=="creator share"&&e.Amount>0);RoundTrip(s);
    }
    [Fact] public void Breakthrough_is_publication_gated_with_twelve_chapter_cooldown()
    {
        var s=SimulationFixture.Serialized();var title=s.Series[0];
        PublishingTests.Until(s,()=>title.Chapters.Any(c=>!c.IsOneShot&&!c.DoujinEligible&&c.Status==ChapterStatus.Complete));
        Assert.Equal(0,title.CulturalImpact);
        s.Advance(s.Clock.HoursUntil(title.Contract!.FirstIssueClose.AddHours(-1)));
        var seed=Enumerable.Range(0,100000).First(i=>Rng.FromSeed(i).NextDouble()<.01);s.StaffRng=Rng.FromSeed(seed);
        s.Advance(1);Assert.True(title.CulturalImpact>=2);Assert.Equal(1,title.LastBreakthroughChapter);
        var before=title.CulturalImpact;s.StaffRng=Rng.FromSeed(seed);SimulationFixture.NextIssue(s);
        Assert.True(title.CulturalImpact-before<1);RoundTrip(s);
    }
    [Fact] public void Declining_all_follower_invitations_and_mutating_input_cannot_change_saved_move()
    {
        var s=GameState.NewGame();var candidate=s.Candidates[0];s.Apply(new HireStaffCommand(candidate.Id,s.Locations[0].Id,candidate.ExpectedSalary));
        s.Advance(25);var invited=new List<int>();s.Apply(new StudioActionCommand(StudioAction.CareerEmployer,Value:1,Followers:invited));invited.Add(candidate.Id);
        var old=s.ControlledBusinessId;s.Advance(24);Assert.Equal(old,s.FindPerson(candidate.Id)!.Employment!.BusinessId);RoundTrip(s);
    }
    [Fact] public void Exhausted_employer_budget_stops_optional_work_without_crashing_provisions()
    {
        var s=GameState.NewGame();s.Apply(new StudioActionCommand(StudioAction.CareerEmployer,Value:1));s.Advance(24);
        s.Apply(new CreateSeriesCommand("Team title","drama",Cadence.Monthly,19));s.Apply(new StudioActionCommand(StudioAction.Promote,s.Series[0].Id,s.ProtagonistPersonId,Enabled:true));
        s.ControlledBusiness.DiscretionarySpent=30000;s.Advance(24*5);
        Assert.Equal(0,s.Series[0].PromotionHours);Assert.True(s.Protagonist.HasProvisions);RoundTrip(s);
    }
}
