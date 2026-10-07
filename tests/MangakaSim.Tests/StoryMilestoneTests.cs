using Xunit;
namespace MangakaSim.Tests;

// Aki's story milestones (spec docs/superpowers/specs/2026-10-05-aki-story-milestones-design.md): one-off choices with a
// stated cost, offered only in a quiet stretch of a settled career, with the safer answer when the window closes.
public class StoryMilestoneTests
{
    static void ToEight(GameState s)
    {
        var next = s.Clock.Now.Date.AddHours(8);
        if (next <= s.Clock.Now) next = next.AddDays(1);
        s.Advance(s.Clock.HoursUntil(next));
    }

    // A settled career whose series has run for five years. The contract's first issue lies ahead, so no issue closes
    // touch the made-up series during these short tests.
    static (GameState State, Series Hit) LongRunningHit(int months = 61)
    {
        var s = GameState.NewGame(0);
        s.Goals!.Chapter = 3;
        s.Milestones.OfferedAt = s.Clock.Now.AddDays(-200);
        s.Apply(new CreateSeriesCommand("Long hit", "adventure", Cadence.Monthly, 16));
        var hit = s.Series.Single();
        var magazine = s.PublisherCatalog.Magazines.First(m => m.Tier == 3);
        hit.Publishing = PublishingStatus.Serialized;
        hit.Contract = new Contract(9999, magazine.Id, 10_000, s.Clock.Now, s.Clock.Now.AddYears(2));
        hit.Chapters.First().PublishedAt = s.Clock.Now.AddMonths(-months);
        return (s, hit);
    }

    [Fact] public void A_five_year_series_brings_the_final_arc_decision()
    {
        var (s, hit) = LongRunningHit();
        ToEight(s);
        Assert.Equal(AkiMilestones.FinalArc, s.Milestones.Pending);
        Assert.Equal(hit.Id, s.Milestones.Target);
        Assert.Contains(s.Events, e => e.Type == EventType.MilestoneOffered);
        Assert.Contains(EventType.MilestoneOffered, CareerGuidance.FastSpeedStops);
        var card = s.PendingMilestone!;
        Assert.False(string.IsNullOrWhiteSpace(card.FirstCost));
        Assert.False(string.IsNullOrWhiteSpace(card.SecondCost));
        Assert.StartsWith("milestone:", CareerGuidance.Evaluate(s, new GuidancePreferences { Completed = ["first-sale"] }).Id);
    }

    [Fact] public void Nothing_arrives_before_the_career_settles_or_after_a_recent_stop()
    {
        var (s, _) = LongRunningHit();
        s.Goals!.Chapter = 2;
        ToEight(s);
        Assert.Null(s.Milestones.Pending);

        s.Goals.Chapter = 3;
        s.Emit(EventType.CancellationWarning, "A recent worry.");
        ToEight(s);
        Assert.Null(s.Milestones.Pending);
    }

    [Fact] public void A_younger_series_is_not_asked_about_its_ending()
    {
        var (s, _) = LongRunningHit(months: 30);
        ToEight(s);
        Assert.Null(s.Milestones.Pending);
    }

    [Fact] public void Planning_the_ending_counts_down_twelve_chapters()
    {
        var (s, hit) = LongRunningHit();
        ToEight(s);
        s.Apply(new MilestoneCommand(AkiMilestones.FinalArc, 0));
        Assert.Null(s.Milestones.Pending);
        Assert.Equal(hit.Id, s.Milestones.FinalArcSeries);
        Assert.Equal(AkiMilestones.FinalArcChapters, s.Milestones.FinalArcChaptersLeft);
        Assert.Contains(s.Career.Journal, e => e.Scene == AkiMilestones.FinalArc && e.Answer == 0 && e.Text.Contains("Plan the ending"));
        Assert.Equal("The final arc?", HelperStories.Describe(s, AkiMilestones.FinalArc).Title);
    }

    [Fact] public void Keeping_it_running_raises_the_fee_and_asks_again_two_years_later()
    {
        var (s, hit) = LongRunningHit();
        ToEight(s);
        var chapters = hit.Contract!.ChaptersPublished = 7;
        s.Apply(new MilestoneCommand(AkiMilestones.FinalArc, 1));
        Assert.Equal(11_000, hit.Contract!.FeePerPage);
        Assert.Equal(chapters, hit.Contract.ChaptersPublished);
        s.Milestones.OfferedAt = s.Clock.Now.AddDays(-200);
        s.Events.RemoveAll(e => e.Type == EventType.MilestoneOffered);
        ToEight(s);
        Assert.Null(s.Milestones.Pending);
        s.Milestones.Done[^1] = s.Milestones.Done[^1] with { At = s.Clock.Now.AddMonths(-25) };
        ToEight(s);
        Assert.Equal(AkiMilestones.FinalArc, s.Milestones.Pending);
    }

    [Fact] public void An_unanswered_decision_takes_the_safer_answer_after_thirty_days()
    {
        var (s, hit) = LongRunningHit();
        ToEight(s);
        var due = s.Milestones.DueAt;
        Assert.Throws<InvalidCommandException>(() => s.Apply(new MilestoneCommand(AkiMilestones.BiggerMagazine, 0)));
        Assert.Throws<InvalidCommandException>(() => s.Apply(new MilestoneCommand(AkiMilestones.FinalArc, 2)));
        s.Advance(s.Clock.HoursUntil(due));
        Assert.Null(s.Milestones.Pending);
        var record = Assert.Single(s.Milestones.Done);
        Assert.True(record.Defaulted);
        Assert.Equal(AkiMilestones.DefaultAnswer, record.Answer);
        Assert.Equal(11_000, hit.Contract!.FeePerPage);
        Assert.Contains("No answer in time", s.Career.Journal.Last().Text);
    }

    [Fact] public void A_milestone_whose_series_ends_is_withdrawn()
    {
        var (s, hit) = LongRunningHit();
        ToEight(s);
        hit.Publishing = PublishingStatus.Unpublished; hit.Contract = null;
        ToEight(s);
        Assert.Null(s.Milestones.Pending);
        Assert.Empty(s.Milestones.Done);
    }

    [Fact] public void Staying_loyal_closes_the_bigger_magazine_for_two_years()
    {
        var (s, hit) = LongRunningHit(months: 20);
        var tier1 = s.PublisherCatalog.Magazines.Where(m => m.Tier == 1).Select(m => m.Id).ToArray();
        s.Milestones.Pending = AkiMilestones.BiggerMagazine; s.Milestones.Target = hit.Id; s.Milestones.Magazine = tier1[0];
        s.Milestones.DueAt = s.Clock.Now.AddDays(30);
        var fans = hit.Fanbase = 100_000;
        s.Apply(new MilestoneCommand(AkiMilestones.BiggerMagazine, 1));
        Assert.Equal(11_000, hit.Contract!.FeePerPage);
        Assert.True(hit.Fanbase > fans);
        Assert.Equal((DateTime?)s.Clock.Now.AddYears(2), s.MilestoneClosed(tier1[0]));
        s.Apply(new CreateSeriesCommand("Next idea", "adventure", Cadence.Monthly, 16));
        var next = s.Series.Last();
        var error = Assert.Throws<InvalidCommandException>(() => s.Apply(new PitchSeriesCommand(next.Id, tier1[0])));
        Assert.Contains("stay loyal", error.Message);
        Assert.False(CareerGuidance.Outlook(s, next, tier1[0]).Open);
    }

    [Fact] public void Accepting_the_bigger_magazine_guarantees_one_offer()
    {
        var (s, hit) = LongRunningHit(months: 20);
        var tier1 = s.PublisherCatalog.Magazines.First(m => m.Tier == 1);
        s.Milestones.Pending = AkiMilestones.BiggerMagazine; s.Milestones.Target = hit.Id; s.Milestones.Magazine = tier1.Id;
        s.Milestones.DueAt = s.Clock.Now.AddDays(30);
        s.Apply(new MilestoneCommand(AkiMilestones.BiggerMagazine, 0));
        Assert.Equal(tier1.Id, s.Milestones.GuaranteedMagazine);
        s.Apply(new CreateSeriesCommand("Big launch", "adventure", Cadence.Monthly, 16));
        var launch = s.Series.Last();
        s.Apply(new PitchSeriesCommand(launch.Id, tier1.Id));
        for (var day = 0; day < 200 && launch.Publishing == PublishingStatus.Pitching; day++) s.Advance(24);
        Assert.Equal(PublishingStatus.Offered, launch.Publishing);
        Assert.Null(s.Milestones.GuaranteedMagazine);
    }

    [Fact] public void Backing_an_assistants_debut_gives_them_a_series()
    {
        var s = GameState.NewGame(17);
        var candidate = s.Candidates.First();
        s.Apply(new HireStaffCommand(candidate.Id, s.Locations.Single(l => l.BusinessId == s.ControlledBusinessId).Id, candidate.ExpectedSalary));
        var assistant = s.FindPerson(candidate.Id)!;
        s.Advance(s.Clock.HoursUntil(assistant.Employment!.StartsAt));
        s.Goals!.Chapter = 3;
        s.Milestones.OfferedAt = s.Clock.Now.AddDays(-200);
        assistant.Employment.StartsAt = s.Clock.Now.AddYears(-3);
        assistant.Skills[Stage.Pencils] = 80;
        ToEight(s);
        Assert.Equal(AkiMilestones.AssistantDebut, s.Milestones.Pending);
        Assert.Contains(assistant.Name, s.PendingMilestone!.Title);
        s.Apply(new MilestoneCommand(AkiMilestones.AssistantDebut, 0));
        var debut = s.Series.Single(x => x.LeadPersonId == assistant.Id);
        Assert.Equal(debut.Id, assistant.MainSeriesId);
        Assert.Equal(assistant.Id, debut.RightsLeadPersonId);
    }

    [Fact] public void Asking_an_assistant_to_wait_costs_happiness_and_loyalty()
    {
        var s = GameState.NewGame(17);
        var candidate = s.Candidates.First();
        s.Apply(new HireStaffCommand(candidate.Id, s.Locations.Single(l => l.BusinessId == s.ControlledBusinessId).Id, candidate.ExpectedSalary));
        var assistant = s.FindPerson(candidate.Id)!;
        s.Milestones.Pending = AkiMilestones.AssistantDebut; s.Milestones.Target = assistant.Id; s.Milestones.DueAt = s.Clock.Now.AddDays(30);
        var (happiness, loyalty) = (assistant.Happiness, assistant.Loyalty);
        s.Apply(new MilestoneCommand(AkiMilestones.AssistantDebut, 1));
        Assert.Equal(Math.Max(0, happiness - 30), assistant.Happiness);
        Assert.Equal(Math.Max(0, loyalty - 20), assistant.Loyalty);
    }

    [Fact] public void Milestone_state_survives_a_save_and_old_saves_start_with_none()
    {
        var s = GameState.NewGame(0);
        s.Milestones.Done.Add(new(AkiMilestones.FinalArc, 1, 1, s.Clock.Now, false));
        s.Milestones.ClosedMagazine = s.PublisherCatalog.Magazines.First(m => m.Tier == 1).Id; s.Milestones.ClosedUntil = s.Clock.Now.AddYears(2);
        var json = s.ToJson();
        var loaded = GameState.FromJson(json);
        Assert.Equal(json, loaded.ToJson());
        Assert.Single(loaded.Milestones.Done);

        var old = System.Text.Json.Nodes.JsonNode.Parse(GameState.NewGame(0).ToJson())!.AsObject();
        old.Remove("Milestones");
        var upgraded = GameState.FromJson(old.ToJsonString());
        Assert.Null(upgraded.Milestones.Pending);
        Assert.Empty(upgraded.Milestones.Done);
    }

    static void Offer(GameState s, string id, int target)
    {
        s.Milestones.Pending = id; s.Milestones.Target = target; s.Milestones.DueAt = s.Clock.Now.AddDays(30);
    }

    [Fact] public void A_top_ten_creator_is_asked_to_teach_and_teaching_takes_wednesday_afternoons()
    {
        var (s, hit) = LongRunningHit(months: 20);
        hit.Chapters.First().Rank = 5;
        ToEight(s);
        Assert.Equal(AkiMilestones.Teaching, s.Milestones.Pending);
        var track = s.ControlledBusiness.TrackRecord;
        s.Apply(new MilestoneCommand(AkiMilestones.Teaching, 0));
        Assert.Equal((DateTime?)s.Clock.Now.AddYears(1), s.Milestones.TeachingUntil);
        Assert.True(s.ControlledBusiness.TrackRecord > track);
        var wednesday = s.Clock.Now.Date.AddDays(((int)DayOfWeek.Wednesday - (int)s.Clock.Now.DayOfWeek + 7) % 7 + 7).AddHours(15);
        s.Advance(s.Clock.HoursUntil(wednesday));
        Assert.True(s.Protagonist.BusyUntil > s.Clock.Now);
        Assert.Contains(s.Protagonist.PersonalAccount.Entries, e => e.Reason == "manga school lecture" && e.Amount > 0);
    }

    [Fact] public void The_overseas_trip_needs_the_cash_and_takes_a_week()
    {
        var (s, hit) = LongRunningHit(months: 20);
        Offer(s, AkiMilestones.OverseasConvention, hit.Id);
        s.Money = 0;
        Assert.Throws<InvalidCommandException>(() => s.Apply(new MilestoneCommand(AkiMilestones.OverseasConvention, 0)));
        s.Money = 5_000_000;
        var fans = hit.Fanbase = 50_000;
        s.Apply(new MilestoneCommand(AkiMilestones.OverseasConvention, 0));
        Assert.True(s.Protagonist.BusyUntil >= s.Clock.Now.AddDays(AkiMilestones.TripDays));
        Assert.True(hit.Fanbase > fans * 1.02);
        Assert.Contains(s.Ledger, e => e.Reason == "overseas convention trip" && e.Amount == -s.MilestoneYen(AkiMilestones.TripCost));
    }

    [Fact] public void Helping_the_parents_costs_savings_and_lifts_aki()
    {
        var s = GameState.NewGame(0);
        Offer(s, AkiMilestones.ParentsHouse, s.ProtagonistPersonId);
        var cost = s.MilestoneYen(AkiMilestones.RepairCost);
        Assert.Throws<InvalidCommandException>(() => s.Apply(new MilestoneCommand(AkiMilestones.ParentsHouse, 0)));
        s.Protagonist.PersonalAccount.Balance = 6_000_000;
        s.Protagonist.Happiness = 40;
        s.Apply(new MilestoneCommand(AkiMilestones.ParentsHouse, 0));
        Assert.Equal(6_000_000 - cost, s.PersonalMoney);
        Assert.Equal(100, s.Protagonist.Happiness);
    }

    [Fact] public void Declined_parents_ask_once_more_a_year_later_and_then_stop()
    {
        var s = GameState.NewGame(0);
        s.Milestones.Done.Add(new(AkiMilestones.ParentsHouse, s.ProtagonistPersonId, 1, s.Clock.Now.AddMonths(-13), false));
        Assert.Equal(1, s.Milestones.Done.Count(r => r.Id == AkiMilestones.ParentsHouse));
        Offer(s, AkiMilestones.ParentsHouse, s.ProtagonistPersonId);
        s.Apply(new MilestoneCommand(AkiMilestones.ParentsHouse, 1));
        Assert.Equal(2, s.Milestones.Done.Count(r => r.Id == AkiMilestones.ParentsHouse));
        foreach (var id in AkiMilestones.All) Assert.False(string.IsNullOrWhiteSpace(AkiMilestones.Describe(s, id, s.ProtagonistPersonId, null).SecondCost));
    }
}
