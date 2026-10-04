using Xunit;
namespace MangakaSim.Tests;

// Career goals board, final review fixes (2026-10-02), each written to fail first.
public class GoalReviewFixTests
{
    [Fact] public void An_older_save_counts_goals_already_met_in_later_chapters_too()
    {
        var old = GameState.NewGame(17); old.Goals = null;
        var candidate = old.Candidates.First();
        old.Apply(new HireStaffCommand(candidate.Id, old.Locations.Single(l => l.BusinessId == old.ControlledBusinessId).Id, candidate.ExpectedSalary));
        old.Advance(old.Clock.HoursUntil(old.FindPerson(candidate.Id)!.Employment!.StartsAt) + 1);
        var loaded = GameState.FromJson(old.ToJson());
        Assert.Equal(0, loaded.Goals!.Chapter);
        Assert.True(loaded.GoalDone("first-hire"), "a later chapter's goal already met counts as done");
        Assert.True(loaded.Goals.Completed.Single(r => r.Id == "first-hire").Backfilled);
        Assert.Contains(loaded.Furniture, f => f.Kind == "chair-support" && f.Paid == 0);
        Assert.DoesNotContain(loaded.Ledger, e => e.Kind == AccountEntryKind.GoalReward);
    }

    [Fact] public void A_new_stopping_text_is_seen_even_when_the_thread_is_full()
    {
        var prefs = new GuidancePreferences();
        for (var i = 0; i < CareerGuidance.ThreadLimit; i++) CareerGuidance.Say(prefs, GameClock.Start, $"Old notice {i}");
        var before = CareerGuidance.LastStopping(prefs);
        CareerGuidance.ObserveGoals(GoalProgressTests.FirstSale(), prefs);
        Assert.Same(before, CareerGuidance.LastStopping(prefs));
        CareerGuidance.Say(prefs, GameClock.Start, "A new stopping notice.");
        Assert.Equal(CareerGuidance.ThreadLimit, prefs.Thread.Count);
        Assert.NotSame(before, CareerGuidance.LastStopping(prefs));
    }

    [Fact] public void A_free_table_makes_the_booth_free_wherever_the_fee_is_quoted()
    {
        var s = GameState.NewGame(0);
        Assert.Equal(5000, s.ConventionBoothFee(1));
        s.Goals!.FreeConventionTables = 1;
        Assert.Equal(0, s.ConventionBoothFee(1)); Assert.Equal(0, s.ConventionBoothFee(2)); Assert.Equal(0, s.ConventionBoothFee(0));
    }

    [Fact] public void The_last_path_message_skips_notices_and_goal_texts()
    {
        var prefs = new GuidancePreferences();
        prefs.Thread.Add(new() { Step = CareerGuidance.ArrearsStep, Texts = ["Payday missed!"] });
        CareerGuidance.ObserveGoals(GoalProgressTests.FirstSale(), prefs);
        CareerGuidance.Say(prefs, GameClock.Start, "A notice.");
        Assert.Equal(CareerGuidance.ArrearsStep, CareerGuidance.LastPathMessage(prefs)?.Step);
    }
}
