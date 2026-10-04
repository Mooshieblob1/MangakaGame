using System.Text.Json.Nodes;
using MangakaSim.Catalog;
using MangakaSim.Rules;
using Xunit;

namespace MangakaSim.Tests;

public class TimelineTests
{
    private static void RoundTrip(GameState s) => Assert.Equal(s.ToJson(), GameState.FromJson(s.ToJson()).ToJson());
    private static int Home(GameState s) => s.Protagonist.Employment!.LocationId;
    private static void Capital(GameState s, long yen = 10000000)
    { s.ControlledBusiness.Account.OpeningBalance += yen; s.Money += yen; }
    private static Person Rival(GameState s) => s.People.First(p => s.World.RivalBusinesses.Contains(p.Employment!.BusinessId));
    private static StaffNegotiation Inbound(GameState s, Person person, double roll = 0)
    {
        var destination = s.Locations.First(l => s.World.RivalBusinesses.Contains(l.BusinessId));
        var o = new StaffNegotiation { Id = s.NextId++, PersonId = person.Id, FromBusiness = s.ControlledBusinessId,
            ToBusiness = destination.BusinessId, LocationId = destination.Id, Salary = 250000, CreatedAt = s.Clock.Now,
            EndsAt = s.Clock.Now.AddDays(7), Roll = roll };
        s.World.Offers.Add(o); return o;
    }
    // Domain boundary tests use an explicit clock jump; integration/replay tests below use normal ticks.
    private static void Jump(GameState s, DateTime date)
    {
        s.Clock.Now = date;
        s.Candidates.Clear();s.StaffProposals.Clear();
        foreach(var series in s.Series.Where(t=>t.Contract is not null))
        { var magazine=s.PublisherCatalog.Get(series.Contract!.MagazineId);var close=IssueSchedule.FirstCloseAfter(magazine,date);
          foreach(var chapter in series.Chapters.Where(c=>!c.IsOneShot&&!c.DoujinEligible&&c.PublishedAt is null).OrderBy(c=>c.Number))
          {chapter.DueDate=close;close=IssueSchedule.AddIssues(magazine,close,1);}
        }
        foreach (var market in s.Markets)
        {
            var magazine = s.PublisherCatalog.Get(market.MagazineId);
            market.NextIssueClose = IssueSchedule.FirstCloseAfter(magazine, date);
            market.IssuesClosed = (int)((market.NextIssueClose - IssueSchedule.Anchor(magazine)).TotalDays / IssueSchedule.Days(magazine.Cadence));
            market.LastRanking = market.Fillers.OrderByDescending(f => f.Popularity).Select((f,i) => new RankEntry(i+1,f.Title,null,f.Id,f.Popularity)).ToList();
        }
        s.TimelineMarketStep();
    }
    [Fact] public void Initial_world_has_real_funded_workplaces_and_no_future_spoilers()
    {
        var s = GameState.NewGame();
        Assert.Single(s.World.Rivals); Assert.Equal("conan", s.World.Rivals[0].Key);
        Assert.Equal(3, s.World.RivalBusinesses.Count);
        foreach (var id in s.World.RivalBusinesses)
        {
            var location = s.Locations.Single(l => l.BusinessId == id);
            Assert.Equal(4, s.UsableWorkspaces(location.Id));
            Assert.Equal(3, s.People.Count(p => p.Employment?.BusinessId == id));
            Assert.Equal(3000000, s.Businesses.Single(b => b.Id == id).Account.Balance);
        }
        Assert.All(s.World.Assistants, a => Assert.Null(a.DiscoveredBy));
        Assert.Empty(s.World.News); Assert.Equal(0, s.Locations.Single(l => l.IsFamilyHome).MonthlyRent); RoundTrip(s);
    }
    [Fact] public void Historical_launch_and_end_are_exact_once_and_preserve_capacity()
    {
        var s = GameState.NewGame(); var d = TimelineCatalog.Default.Rivals.Single(r => r.Id == "bleach");
        Jump(s, d.Start.AddHours(-1)); Assert.DoesNotContain(s.World.Rivals,r => r.Key == d.Id);
        Jump(s, d.Start); var rival = Assert.Single(s.World.Rivals,r => r.Key == d.Id);
        var id = rival.FillerId; var news = s.World.News.Count;
        s.TimelineMarketStep(); Assert.Equal(news,s.World.News.Count);
        Jump(s,d.End!.Value.AddHours(-1)); Assert.Equal(RivalPhase.Active,rival.Phase);
        Jump(s,d.End.Value); Assert.Equal(RivalPhase.Ended,rival.Phase);
        Assert.Contains(s.Markets.Single(m=>m.MagazineId==d.Magazine).RetiredFillers,f=>f.Id==id);
        foreach(var market in s.Markets) Assert.Equal(s.PublisherCatalog.Get(market.MagazineId).RosterSize-1,market.Fillers.Count);
        RoundTrip(s);
    }
    [Fact] public void Historical_low_ranking_cannot_randomly_end_a_protected_title()
    {
        var s=GameState.NewGame();var rival=s.World.Rivals.Single();
        var filler=s.Markets.SelectMany(m=>m.Fillers).Single(f=>f.Id==rival.FillerId);filler.IssuesBelowLine=100;
        s.Advance(24*100);Assert.Equal(RivalPhase.Active,rival.Phase);
        Assert.Contains(s.Markets.SelectMany(m=>m.Fillers),f=>f.Id==filler.Id);RoundTrip(s);
    }
    [Fact] public void Rival_demand_rewards_quality_but_never_escapes_caps()
    {
        var s=GameState.NewGame();Jump(s,new(2020,1,1));
        Assert.True(s.RivalDemand("action",90)>s.RivalDemand("action",30));
        foreach(var genre in s.TrendCatalog.Genres)foreach(var quality in new[]{0d,40,60,80,100}) Assert.InRange(s.RivalDemand(genre,quality),.85,1.25);
    }
    [Fact] public void News_and_decisions_have_different_default_pause_policies()
    {
        var s=GameState.NewGame();Assert.False(s.Settings.AutoPause[EventType.IndustryNews]);Assert.True(s.Settings.AutoPause[EventType.IndustryDecision]);
    }
    [Fact] public void Scouting_cost_delay_privacy_and_read_only_views()
    {
        var s=GameState.NewGame();var p=Rival(s);var before=s.ToJson();
        Assert.DoesNotContain("loyalty",s.RivalStaffProfile(p.Id),StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("expected salary ~",s.RivalStaffProfile(p.Id));Assert.Equal(before,s.ToJson());
        s.Apply(new TimelineCommand(TimelineAction.Scout,p.Id));Assert.Equal(290000,s.Money);
        var saved=s.ToJson();Assert.Throws<InvalidCommandException>(()=>s.Apply(new TimelineCommand(TimelineAction.Scout,p.Id)));Assert.Equal(saved,s.ToJson());
        s.Advance(71);Assert.Null(s.World.Reports.Single().ObservedAt);
        s.Advance(1);Assert.NotNull(s.World.Reports.Single().ObservedAt);Assert.Contains("Willingness unknown",s.RivalStaffProfile(p.Id));
        RoundTrip(s);Assert.Equal(s.ToJson(),s.ReplayTimeline().ToJson());
    }
    [Fact] public void Approach_reserves_desk_and_wages_and_cancellation_releases_them()
    {
        var s=GameState.NewGame();var p=Rival(s);
        s.Apply(new TimelineCommand(TimelineAction.Approach,p.Id,Home(s),p.ExpectedSalary));
        Assert.Equal(280000,s.Money);Assert.Equal(280000-StudioRules.HiringReserve(p.ExpectedSalary),s.AvailableBusinessCash);
        var before=s.ToJson();var c=s.Candidates[0];
        Assert.Throws<InvalidCommandException>(()=>s.Apply(new HireStaffCommand(c.Id,Home(s),c.ExpectedSalary)));Assert.Equal(before,s.ToJson());
        var o=s.World.Offers.Single();s.Apply(new TimelineCommand(TimelineAction.CancelOffer,o.Id));Assert.Equal(s.Money,s.AvailableBusinessCash);
        before=s.ToJson();Assert.Throws<InvalidCommandException>(()=>s.Apply(new TimelineCommand(TimelineAction.Approach,p.Id,Home(s),p.ExpectedSalary)));Assert.Equal(before,s.ToJson());RoundTrip(s);
    }
    [Theory] [InlineData(0,true)] [InlineData(.999,false)]
    public void Rival_approach_uses_saved_draw_and_keeps_personal_savings(double draw,bool accepted)
    {
        var s=GameState.NewGame();Capital(s);var p=Rival(s);var old=p.Employment!.BusinessId;
        s.Apply(new TimelineCommand(TimelineAction.Approach,p.Id,Home(s),p.ExpectedSalary));s.World.Offers.Single().Roll=draw;
        var copy=GameState.FromJson(s.ToJson());s.Advance(24*7);copy.Advance(24*7);Assert.Equal(s.ToJson(),copy.ToJson());
        Assert.Equal(accepted?s.ControlledBusinessId:old,p.Employment!.BusinessId);Assert.Equal(200000,s.PersonalMoney);
        Assert.Equal(accepted?NegotiationStatus.Accepted:NegotiationStatus.Declined,s.World.Offers.Single().Status);RoundTrip(s);
    }
    [Fact] public void Destination_closure_lapses_offer_without_moving_the_person()
    {
        var s=GameState.NewGame();var p=Rival(s);s.Apply(new TimelineCommand(TimelineAction.Approach,p.Id,Home(s),p.ExpectedSalary));
        s.Apply(new StudioActionCommand(StudioAction.Move,1));s.Advance(1);
        Assert.Equal(NegotiationStatus.Lapsed,s.World.Offers.Single().Status);Assert.NotEqual(s.ControlledBusinessId,p.Employment!.BusinessId);RoundTrip(s);
    }
    [Theory] [InlineData(false)] [InlineData(true)]
    public void Career_move_releases_former_studio_recruitment_reservations(bool rivalEmployer)
    {
        var s=GameState.NewGame();Capital(s);var person=Rival(s);var old=s.ControlledBusinessId;
        s.Apply(new TimelineCommand(TimelineAction.Approach,person.Id,Home(s),person.ExpectedSalary));
        var approach=s.World.Offers.Single();
        if(rivalEmployer){var career=Inbound(s,s.Protagonist);s.Apply(new TimelineCommand(TimelineAction.AcceptCareer,career.Id));}
        else{s.Apply(new StudioActionCommand(StudioAction.CareerHome));s.Advance(s.Clock.HoursUntil(s.Clock.Now.Date.AddDays(1)));}
        Assert.NotEqual(old,s.ControlledBusinessId);Assert.Equal(NegotiationStatus.Cancelled,approach.Status);
        s.Advance(24*7);Assert.NotEqual(old,person.Employment!.BusinessId);RoundTrip(s);
    }
    [Fact] public void Protagonist_offer_never_auto_accepts_and_warns_before_expiry()
    {
        var s=GameState.NewGame();var old=s.ControlledBusinessId;var o=Inbound(s,s.Protagonist);
        s.Advance(24*6);Assert.True(o.Warned);Assert.Contains(s.Events,e=>e.Type==EventType.IndustryDecision&&e.Message.Contains("Last day"));
        s.Advance(24);Assert.Equal(old,s.ControlledBusinessId);Assert.Equal(NegotiationStatus.Declined,o.Status);RoundTrip(s);
    }
    [Fact] public void Career_offer_uses_actual_employer_and_keeps_old_edition_ownership()
    {
        var s=PublishingTests.Started();PublishingTests.Until(s,()=>s.Series[0].Volumes.Count>0);
        var title=s.Series[0];s.Apply(new StudioActionCommand(StudioAction.Print,title.Volumes[0].Id,Amount:10));s.Advance(24);
        var old=s.ControlledBusinessId;var money=s.Money;var personal=s.PersonalMoney;var o=Inbound(s,s.Protagonist);
        s.Apply(new TimelineCommand(TimelineAction.AcceptCareer,o.Id));
        Assert.Equal(o.ToBusiness,s.ControlledBusinessId);Assert.Equal(o.ToBusiness,title.BusinessId);
        Assert.Equal(old,title.Volumes[0].BusinessId);Assert.Equal(money,s.Businesses.Single(b=>b.Id==old).Account.Balance);Assert.Equal(personal,s.PersonalMoney);
        Assert.Equal(ControlMode.EmployedLead,s.Control);RoundTrip(s);
    }
    [Theory] [InlineData(OwnershipMode.CreatorRetention,true)] [InlineData(OwnershipMode.StudioRetention,false)]
    public void Departing_ordinary_lead_respects_selected_rights(OwnershipMode rights,bool follows)
    {
        var s=GameState.NewGame(0,rights);Capital(s);var c=s.Candidates[0];s.Apply(new HireStaffCommand(c.Id,Home(s),c.ExpectedSalary));s.Advance(25);
        s.Apply(new CreateSeriesCommand("Their work","drama",Cadence.Monthly,19));s.Apply(new AssignStaffCommand(c.Id,s.Series[0].Id,true));
        var p=s.FindPerson(c.Id)!;var old=s.ControlledBusinessId;var o=Inbound(s,p);s.Advance(24*7);
        Assert.Equal(NegotiationStatus.Accepted,o.Status);Assert.Equal(o.ToBusiness,p.Employment!.BusinessId);
        Assert.Equal(follows?o.ToBusiness:old,s.Series[0].BusinessId);RoundTrip(s);
    }
    [Fact] public void Retention_changes_salary_without_rerolling_or_forcing_the_employee()
    {
        var s=GameState.NewGame();Capital(s);var c=s.Candidates[0];s.Apply(new HireStaffCommand(c.Id,Home(s),c.ExpectedSalary));s.Advance(25);
        var p=s.FindPerson(c.Id)!;var o=Inbound(s,p,.8);var rng=s.World.HiringRng.State;
        s.Apply(new TimelineCommand(TimelineAction.Retain,o.Id,Salary:c.ExpectedSalary*2));Assert.Equal(.8,o.Roll);Assert.Equal(rng,s.World.HiringRng.State);
        s.Advance(24*7);Assert.Equal(NegotiationStatus.Declined,o.Status);Assert.Equal(s.ControlledBusinessId,p.Employment!.BusinessId);RoundTrip(s);
    }
    [Fact] public void Assistant_discovery_and_support_contract_expire_without_duplicate_identity()
    {
        var s=GameState.NewGame();Capital(s);var a=s.World.Assistants[0];a.DiscoveredBy=s.ControlledBusinessId;
        var d=TimelineCatalog.Default.Creators.Single(c=>c.Id==a.Key);var salary=StudioRules.ExpectedSalary(d.Skills.Max());
        s.Apply(new TimelineCommand(TimelineAction.HireAssistant,0,Home(s),salary));var p=s.FindPerson(a.PersonId!.Value)!;
        Assert.False(p.IsProdigy);s.Advance(25);s.Apply(new CreateSeriesCommand("Support","drama",Cadence.Monthly,19));
        var before=s.ToJson();Assert.Throws<InvalidCommandException>(()=>s.Apply(new AssignStaffCommand(p.Id,s.Series[0].Id,true)));Assert.Equal(before,s.ToJson());
        s.Apply(new AssignStaffCommand(p.Id,s.Series[0].Id));
        s.Advance(s.Clock.HoursUntil(d.Departure));Assert.True(a.Departed);Assert.Null(p.Employment);Assert.True(p.PersonalAccount.Balance>0);
        before=s.ToJson();Assert.Throws<InvalidCommandException>(()=>s.Apply(new TimelineCommand(TimelineAction.HireAssistant,0,Home(s),salary)));Assert.Equal(before,s.ToJson());RoundTrip(s);
    }
    [Fact] public void Hiring_that_would_start_at_the_fixed_departure_is_atomic()
    {
        var s=GameState.NewGame();Jump(s,new(1997,3,31,12,0,0));s.World.Assistants[0].DiscoveredBy=s.ControlledBusinessId;
        var before=s.ToJson();Assert.Throws<InvalidCommandException>(()=>s.Apply(new TimelineCommand(TimelineAction.HireAssistant,0,Home(s),200000)));Assert.Equal(before,s.ToJson());
    }
    [Fact] public void New_channels_are_gated_and_rejected_commands_preserve_everything()
    {
        var s=PublishingTests.Started();var before=s.ToJson();
        foreach(var action in new[]{TimelineAction.RequestDigital,TimelineAction.RequestOverseas})
        {Assert.Throws<InvalidCommandException>(()=>s.Apply(new TimelineCommand(action,s.Series[0].Id)));Assert.Equal(before,s.ToJson());}
    }
    [Fact] public void Digital_doujin_uses_no_stock_and_settles_only_once_per_week()
    {
        var s=PublishingTests.Started();Capital(s);PublishingTests.Until(s,()=>s.Series[0].Volumes.Count>0);
        Jump(s,new(2006,5,1));s.Apply(new GetOnlineCommand());var title=s.Series[0];var v=title.Volumes[0];title.Fanbase=10000;
        var money=s.Money;s.Apply(new TimelineCommand(TimelineAction.RequestDigital,title.Id));Assert.Equal(money-30000,s.Money);Assert.NotNull(v.ReleasedAt);
        // Streaming sales (2026-10-03): the release plans the week's units and its one receipt; shop hours fill it.
        s.SalesStep();var receipt=Assert.Single(s.World.Receipts);Assert.Equal(0,receipt.Units);
        var plan=Assert.Single(v.SalesPlans!,p=>p.Kind==SaleKind.Channel);Assert.True(plan.Total>0);
        for(var h=1;h<24*7;h++)
        {
            s.Advance(1);
            if(h==11){Assert.Equal(1,plan.HoursDone);var hour=s.ToJson();s.SalesStep();Assert.Equal(hour,s.ToJson());}
        }
        Assert.Equal(receipt,Assert.Single(s.World.Receipts,r=>r.VolumeId==v.Id));Assert.Equal(plan.Total,receipt.Units);Assert.Equal(0,v.CopiesSold);Assert.Equal(0,s.Stock(v.Id));
        var before=s.ToJson();s.SalesStep();Assert.Equal(before,s.ToJson());RoundTrip(s);
    }
    [Theory] [InlineData(0,true)] [InlineData(.999,false)]
    public void Publisher_channels_can_approve_or_refuse_affordable_proposals(double draw,bool accepted)
    {
        var s=SimulationFixture.EightPublished();Capital(s);Jump(s,new(2019,2,1));var title=s.Series[0];
        // Existing scheduled editions stay eligible; the request must not revive closed books.
        Assert.NotEmpty(title.Volumes);s.Apply(new TimelineCommand(TimelineAction.RequestOverseas,title.Id));var a=s.World.Channels.Single();a.Roll=draw;
        var money=s.Money;Assert.Equal(money-100000-s.ReservedWages-s.Bills.Where(b=>b.BusinessId==s.ControlledBusinessId).Sum(b=>b.Remaining),s.AvailableBusinessCash);
        Jump(s,a.ResolvesAt);s.TimelineStaffStep();Assert.Equal(accepted?NegotiationStatus.Accepted:NegotiationStatus.Declined,a.Status);
        Assert.Equal(money-(accepted?100000:0),s.Money);Assert.NotEmpty(a.Reason);RoundTrip(s);
    }
    [Fact] public void Career_move_withdraws_pending_publisher_proposals_without_charging_setup()
    {
        var s=SimulationFixture.EightPublished();Capital(s);Jump(s,new(2019,2,1));
        s.Apply(new TimelineCommand(TimelineAction.RequestOverseas,s.Series[0].Id));var channel=s.World.Channels.Single();
        var old=s.ControlledBusiness;var balance=s.Money;var career=Inbound(s,s.Protagonist);
        s.Apply(new TimelineCommand(TimelineAction.AcceptCareer,career.Id));
        Assert.Equal(NegotiationStatus.Cancelled,channel.Status);Assert.Equal(balance,old.Account.Balance);
        Jump(s,channel.ResolvesAt);s.TimelineStaffStep();Assert.Equal(NegotiationStatus.Cancelled,channel.Status);
        Assert.Empty(s.World.Receipts);RoundTrip(s);
    }
    [Fact] public void Future_transition_is_once_only_and_never_resurrects_ended_history()
    {
        var s=GameState.NewGame();Jump(s,new(2025,12,31,23,0,0));Assert.False(s.World.SimulatedFuture);
        var ended=s.World.Rivals.Where(r=>r.Phase==RivalPhase.Ended).Select(r=>r.Key).ToArray();
        Jump(s,new(2026,1,1));s.TimelineStaffStep();var count=s.World.News.Count(n=>n.Message.Contains("Researched history has ended"));
        Assert.Equal(1,count);Assert.True(s.World.SimulatedFuture);
        for(var q=1;q<=12;q++){Jump(s,new DateTime(2026,1,1).AddMonths(q*3));s.TimelineStaffStep();}
        Assert.All(ended,id=>Assert.Equal(RivalPhase.Ended,s.World.Rivals.Single(r=>r.Key==id).Phase));
        Assert.All(s.World.News.Where(n=>n.Time>=TimelineCatalog.Cutoff),n=>Assert.True(n.Simulated));Assert.InRange(s.World.News.Count,1,500);RoundTrip(s);
    }
    [Fact] public void Batched_ticks_save_load_and_new_game_replay_are_identical()
    {
        var a=GameState.NewGame(37);a.Apply(new RecruitStaffCommand(Stage.Pencils));var b=GameState.FromJson(a.ToJson());
        a.Advance(24*35);for(var i=0;i<24*35;i++)b.Advance(1);
        Assert.Equal(a.ToJson(),b.ToJson());Assert.Equal(a.ToJson(),a.ReplayTimeline().ToJson());
    }
    [Theory] [InlineData("World")] [InlineData("HiringRng")] [InlineData("Rivals")] [InlineData("Mentoring")]
    public void Required_timeline_fields_do_not_silently_default(string field)
    {
        var json=JsonNode.Parse(GameState.NewGame().ToJson())!;
        if(field=="World")json.AsObject().Remove(field);else json["World"]!.AsObject().Remove(field);
        Assert.Throws<InvalidDataException>(()=>GameState.FromJson(json.ToJsonString()));
    }
    [Fact] public void V4_import_preserves_money_history_and_replays_from_checkpoint()
    {
        var s=GameState.NewGame();s.Advance(24*20);var old=JsonNode.Parse(s.ToJson())!;old["Version"]=4;old.AsObject().Remove("World");
        var imported=GameState.ImportTimelineV4(old.ToJsonString());Assert.Equal(s.Money,imported.Money);Assert.Equal(s.PersonalMoney,imported.PersonalMoney);
        Assert.Equal(s.Rng.State,imported.Rng.State);Assert.Equal(s.StaffRng.State,imported.StaffRng.State);Assert.Equal(s.CommandLog,imported.CommandLog);
        imported.Apply(new TimelineCommand(TimelineAction.Scout,Rival(imported).Id));imported.Advance(96);
        RoundTrip(imported);Assert.Equal(imported.ToJson(),imported.ReplayTimeline().ToJson());
        old.AsObject().Remove("Clock");Assert.Throws<InvalidDataException>(()=>GameState.ImportTimelineV4(old.ToJsonString()));
    }
    [Fact] public void Historical_mentoring_requires_productive_shared_attendance_and_caps_one_learner()
    {
        var s=GameState.NewGame();Capital(s);s.Apply(new StudioActionCommand(StudioAction.Move,1));OfficeTests.Furnish(s,Home(s));
        s.World.Assistants[0].DiscoveredBy=s.ControlledBusinessId;s.Apply(new TimelineCommand(TimelineAction.HireAssistant,0,Home(s),200000));
        var candidate=s.Candidates[0];s.Apply(new HireStaffCommand(candidate.Id,Home(s),candidate.ExpectedSalary));s.Advance(28);
        var mentor=s.FindPerson(s.World.Assistants[0].PersonId!.Value)!;var learner=s.Protagonist;var second=s.FindPerson(candidate.Id)!;
        s.Apply(new CreateSeriesCommand("Lessons","drama",Cadence.Monthly,19));var task=new QueueRef(s.Series[0].Chapters[0].Id,Stage.Pencils);
        foreach(var p in new[]{mentor,learner,second}){p.CurrentTask=task;p.BusyUntil=null;p.Experience[Stage.Pencils]=0;}
        learner.Skills[Stage.Pencils]=second.Skills[Stage.Pencils]=10;mentor.HoursWorkedToday=1;
        s.Learn(learner,Stage.Pencils,10);Assert.Equal(20,learner.Experience[Stage.Pencils]);Assert.Empty(s.World.Mentoring);
        learner.Experience[Stage.Pencils]=0;s.Learn(learner,Stage.Pencils,10,true);Assert.Equal(22,learner.Experience[Stage.Pencils]);
        s.Learn(second,Stage.Pencils,10,true);Assert.Equal(10*(second.IsProdigy?2:1),second.Experience[Stage.Pencils]);
        mentor.BusyUntil=s.Clock.Now.AddHours(1);learner.Experience[Stage.Pencils]=0;s.Learn(learner,Stage.Pencils,10,true);Assert.Equal(20,learner.Experience[Stage.Pencils]);
    }
    [Theory] [InlineData("null-world")] [InlineData("null-assistant")] [InlineData("early-unlock")] [InlineData("wrong-future")] [InlineData("duplicate-rival")] [InlineData("unknown-revision")]
    public void Corrupt_timeline_state_fails_with_a_save_error(string mutation)
    {
        var node=JsonNode.Parse(GameState.NewGame().ToJson())!;var w=node["World"]!;
        switch(mutation)
        {
            case "null-world":node["World"]=null;break;
            case "null-assistant":w["Assistants"]![0]=null;break;
            case "early-unlock":w["Processed"]!.AsArray().Add("industry:mobile");break;
            case "wrong-future":w["SimulatedFuture"]=true;break;
            case "duplicate-rival":w["Rivals"]!.AsArray().Add(w["Rivals"]![0]!.DeepClone());break;
            case "unknown-revision":w["Revision"]=99;break;
        }
        Assert.Throws<InvalidDataException>(()=>GameState.FromJson(node.ToJsonString()));
    }
    [Fact] public void Repeated_successful_poaching_mildly_changes_relations_and_failure_does_not()
    {
        var s=GameState.NewGame();Capital(s);s.Apply(new StudioActionCommand(StudioAction.Move,1));OfficeTests.Furnish(s,Home(s));
        var rivals=s.People.Where(p=>p.Employment?.BusinessId==s.World.RivalBusinesses[0]).ToArray();
        foreach(var p in rivals.Take(2))
        {s.Apply(new TimelineCommand(TimelineAction.Approach,p.Id,Home(s),p.ExpectedSalary));s.World.Offers.Last().Roll=0;s.Advance(24*7);}
        Assert.Equal(45,Assert.Single(s.World.Relations).Value);
        s.Apply(new TimelineCommand(TimelineAction.Approach,rivals[2].Id,Home(s),rivals[2].ExpectedSalary));s.World.Offers.Last().Roll=.999;s.Advance(24*7);
        Assert.Equal(45,Assert.Single(s.World.Relations).Value);RoundTrip(s);
    }
    [Fact] public void New_digital_volumes_release_without_buying_stock_and_old_receipts_stay_put_after_move()
    {
        var s=PublishingTests.Started(0,1);Capital(s);Jump(s,new(2006,5,1,8,0,0));s.Apply(new GetOnlineCommand());
        PublishingTests.Until(s,()=>s.Series[0].Volumes.Count>0);s.Apply(new TimelineCommand(TimelineAction.RequestDigital,s.Series[0].Id));
        PublishingTests.Until(s,()=>s.Series[0].Volumes.Count==2);s.Advance(1);
        Assert.All(s.Series[0].Volumes,v=>Assert.NotNull(v.ReleasedAt));var a=s.World.Channels.Single();Assert.Equal(2,a.VolumeIds.Count);
        s.Series[0].Fanbase=10000;s.Advance(24*7);var old=s.ControlledBusinessId;var units=s.World.Receipts.Sum(r=>r.Units);
        s.Apply(new StudioActionCommand(StudioAction.CareerHome));s.Advance(24*7);
        Assert.Equal(old,a.BusinessId);Assert.All(a.VolumeIds,id=>Assert.Equal(old,s.Series[0].Volumes.Single(v=>v.Id==id).BusinessId));
        Assert.True(s.World.Receipts.Sum(r=>r.Units)>=units);RoundTrip(s);
    }
    [Fact] [Trait("Category","LongRun")]
    public void Forty_year_run_remains_bounded_and_saves_a_continuable_world()
    {
        var s=GameState.NewGame(31);var timer=System.Diagnostics.Stopwatch.StartNew();
        System.IO.Directory.CreateDirectory("TestResults");
        for(var year=1997;year<=2036;year++)
        {s.Advance(s.Clock.HoursUntil(new DateTime(year,4,1,8,0,0)));System.IO.File.WriteAllText("TestResults/timeline-long-run-progress.txt",$"{year}: {timer.Elapsed.TotalSeconds:F2}s");}
        var elapsed=timer.Elapsed;
        Assert.Equal(13,s.World.Rivals.Count);Assert.InRange(s.World.News.Count,1,500);Assert.Equal(10,s.People.Count);
        Assert.All(s.Markets,m=>Assert.Equal(s.PublisherCatalog.Get(m.MagazineId).RosterSize-1,m.Fillers.Count));
        var json=s.ToJson();var copy=GameState.FromJson(json);s.Advance(48);copy.Advance(48);Assert.Equal(s.ToJson(),copy.ToJson());
        var report=$"1996-04-01 to 2036-04-01: {elapsed.TotalSeconds:F2}s; {json.Length:N0} JSON characters; {s.Events.Count:N0} events; {s.People.Count} people; {s.World.News.Count} news items.";
        System.IO.Directory.CreateDirectory("TestResults");System.IO.File.WriteAllText("TestResults/timeline-long-run.txt",report);
        var archiveRoot=System.IO.Path.Combine(System.IO.Path.GetTempPath(),"mangaka-long-"+Guid.NewGuid().ToString("N"));
        try
        {
            var store=new CareerStore(archiveRoot);var career=Guid.NewGuid().ToString("N");var full=s.ToJson();
            timer.Restart();var saved=store.Save(career,"Forty years",s,new());var saveMs=timer.Elapsed.TotalMilliseconds;
            var bytes=new System.IO.FileInfo(System.IO.Path.Combine(archiveRoot,career,saved.Snapshot+".career")).Length;
            timer.Restart();Assert.Single(store.List());var listMs=timer.Elapsed.TotalMilliseconds;
            timer.Restart();var loaded=store.Load(saved);var loadMs=timer.Elapsed.TotalMilliseconds;
            Assert.Equal(full,loaded.State.ToJson());
            var exported=store.Export(saved);Assert.Equal(full,store.Load(store.Import(exported)).State.ToJson());
            System.IO.File.WriteAllText("TestResults/alpha-long-storage.txt",$"Full history: {full.Length:N0} JSON chars; {bytes:N0} stored bytes; save {saveMs:F1} ms; list {listMs:F1} ms; load {loadMs:F1} ms. Local and portable roundtrips exact.");
        }
        finally{if(System.IO.Directory.Exists(archiveRoot))System.IO.Directory.Delete(archiveRoot,true);}
    }
}
