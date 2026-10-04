using Xunit;
namespace MangakaSim.Tests;

// Career goals board (spec 2026-10-01): goals complete once, pay once, and chapters open in order.
public class GoalProgressTests
{
    internal static GameState FirstSale(int seed = 0, bool goals = true)
    {
        var s = GameState.NewGame(seed); if (!goals) s.Goals = null;
        s.Apply(new CreateDoujinCommand("First pages", "adventure"));
        for (var d = 0; d < 180 && s.Series[0].Volumes.Count == 0; d++) s.Advance(24);
        s.Apply(new StudioActionCommand(StudioAction.Print, s.Series[0].Volumes.Single().Id, Amount: 10, Value: (int)PrintTier.CopyShop));
        for (var d = 0; d < 30 && s.Series[0].Volumes.Single().CopiesSold == 0; d++) s.Advance(24);
        s.Advance(2);
        return s;
    }

    [Fact] public void Goals_complete_with_their_reward_exactly_once()
    {
        var prints = GameState.NewGame(0).Furniture.Count(f => f.Kind == "print");
        var s = FirstSale();
        Assert.True(s.GoalDone("doujin-finished") && s.GoalDone("first-copy"));
        s.Advance(24 * 7);
        Assert.Single(s.Ledger, e => e.Reason == "Goal reward: Finish your first doujin" && e.Amount == 5_000 && e.Kind == AccountEntryKind.GoalReward);
        Assert.Single(s.Events, e => e.Type == EventType.GoalCompleted && e.Message.StartsWith("Goal complete: Sell your first copy"));
        Assert.Equal(prints + 1, s.Furniture.Count(f => f.Kind == "print"));
    }

    [Fact] public void Finishing_a_chapter_opens_the_next_with_the_chapter_reward_and_scene()
    {
        var s = FirstSale();
        foreach (var id in new[] { "convention", "fans-100", "sales-50000" }) s.Goals!.Completed.Add(new(id, s.Clock.Now, false));
        s.Advance(1);
        Assert.Equal(1, s.Goals!.Chapter);
        Assert.Single(s.Events, e => e.Type == EventType.GoalChapterCompleted && e.Message.StartsWith("Chapter complete: Doujin Days"));
        Assert.Equal(1, s.Goals.FreeConventionTables);
        Assert.Contains(s.Furniture, f => f.Kind == "trophy-shelf" && f.Paid == 0);
        Assert.True(s.Career.PendingScene == "goal-doujin-days" || s.Goals.PendingScenes.Contains("goal-doujin-days"));
    }

    [Fact] public void A_queued_chapter_scene_follows_the_scene_in_front_of_it()
    {
        var s = FirstSale();
        s.Career.PendingScene = "beside"; s.Goals!.PendingScenes.Add("goal-doujin-days");
        s.Apply(new StoryCommand("beside", 0));
        Assert.Equal("goal-doujin-days", s.Career.PendingScene);
        Assert.Empty(s.Goals.PendingScenes);
    }

    [Fact] public void Goals_survive_a_save_and_load_and_play_the_same_twice()
    {
        var a = FirstSale(7); var b = FirstSale(7);
        Assert.Equal(a.ToJson(), b.ToJson());
        var loaded = GameState.FromJson(a.ToJson());
        Assert.Equal(a.Goals!.Completed.Count, loaded.Goals!.Completed.Count);
        Assert.Contains(loaded.Furniture, f => f.Kind == "print" && f.Paid == 0);
    }

    [Fact] public void A_save_naming_an_unknown_goal_is_refused()
    {
        var s = FirstSale(); s.Goals!.Completed.Add(new("not-a-goal", s.Clock.Now, false));
        Assert.Throws<InvalidDataException>(() => GameState.FromJson(s.ToJson()));
    }
}
