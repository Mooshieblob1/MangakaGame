using System.Text.Json.Nodes;
using Xunit;
namespace MangakaSim.Tests;

// Career goals board (spec 2026-10-01): saves written before the board count goals already met, without a windfall.
public class OldSaveGoalsTests
{
    static void AssertBackfilled(GameState before, GameState loaded)
    {
        Assert.True(loaded.GoalDone("doujin-finished") && loaded.GoalDone("first-copy"));
        Assert.All(loaded.Goals!.Completed, r => Assert.True(r.Backfilled));
        Assert.DoesNotContain(loaded.Ledger, e => e.Kind == AccountEntryKind.GoalReward);
        Assert.DoesNotContain(loaded.Events, e => e.Type is EventType.GoalCompleted or EventType.GoalChapterCompleted);
        Assert.Equal(before.Furniture.Count(f => f.Kind == "print") + 1, loaded.Furniture.Count(f => f.Kind == "print"));
        Assert.Equal(before.Money, loaded.Money);
        Assert.Equal(loaded.ToJson(), loaded.ReplayTimeline().ToJson());
    }

    [Fact] public void A_save_with_no_goals_counts_goals_already_met_without_cash_fans_or_events()
    {
        var old = GoalProgressTests.FirstSale(goals: false);
        AssertBackfilled(old, GameState.FromJson(old.ToJson()));
    }

    [Fact] public void A_save_written_before_the_goals_property_existed_loads_the_same_way()
    {
        var old = GoalProgressTests.FirstSale(goals: false);
        var node = JsonNode.Parse(old.ToJson())!.AsObject(); node.Remove("Goals");
        AssertBackfilled(old, GameState.FromJson(node.ToJsonString()));
    }

    [Fact] public void Play_after_a_backfill_completes_new_goals_with_their_rewards()
    {
        var loaded = GameState.FromJson(GoalProgressTests.FirstSale(goals: false).ToJson());
        loaded.Goals!.Completed.RemoveAll(r => r.Id == "first-copy");
        loaded.Advance(1);
        Assert.Contains(loaded.Events, e => e.Type == EventType.GoalCompleted && e.Message.StartsWith("Goal complete: Sell your first copy"));
    }
}
