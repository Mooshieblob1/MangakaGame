using Xunit;
namespace MangakaSim.Tests;

// Career goals board (spec 2026-10-01): the catalogue, its measures and the reward furniture.
public class GoalCatalogTests
{
    [Fact] public void Five_named_chapters_then_mastery_with_four_to_six_goals_each()
    {
        Assert.Equal(new[] { "Doujin Days", "Rookie", "Serialized", "Studio Head", "Legend", "Mastery" }, GoalCatalog.Chapters.Select(c => c.Name));
        foreach (var c in GoalCatalog.Chapters.Take(5)) Assert.InRange(GoalCatalog.In(c.Index).Count(), 4, 6);
        Assert.Equal(GoalCatalog.Goals.Length, GoalCatalog.Goals.Select(g => g.Id).Distinct().Count());
    }

    [Fact] public void Tips_fit_a_phone_bubble_and_every_goal_names_its_reward()
    {
        foreach (var g in GoalCatalog.Goals)
        {
            Assert.True(g.Tip.Length <= CareerGuidance.TextLimit, $"{g.Id}: {g.Tip.Length}");
            Assert.False(string.IsNullOrWhiteSpace(g.Reward.Describe()), g.Id);
        }
    }

    [Fact] public void Reward_furniture_exists_cannot_be_bought_and_sits_at_the_end_of_the_catalogue()
    {
        foreach (var r in GoalCatalog.Goals.Select(g => g.Reward).Concat(GoalCatalog.Chapters.Select(c => c.Reward)))
        {
            if (r.Furniture is { } f) OfficeCatalog.Get(f);
            if (r.Unlock is { } u) Assert.False(OfficeCatalog.Get(u).RewardOnly);
        }
        Assert.True(OfficeCatalog.Furniture.SkipWhile(f => !f.RewardOnly).All(f => f.RewardOnly), "reward-only items come last, so the buy list keeps its indexes");
        Assert.Equal("Rookie", GoalCatalog.UnlockedBy("desk-studio"));
        Assert.Null(GoalCatalog.UnlockedBy("desk"));
    }

    [Fact] public void A_new_career_measures_from_zero()
    {
        var s = GameState.NewGame(0);
        Assert.All(GoalCatalog.In(0), g => Assert.False(g.Measure(s).Done));
        Assert.Equal("¥0 of ¥50,000", GoalCatalog.Get("sales-50000").Measure(s).Text);
        Assert.Equal("0 of 100", GoalCatalog.Get("fans-100").Measure(s).Text);
    }

    [Fact] public void A_first_doujin_and_its_first_sale_meet_the_first_two_goals()
    {
        var s = GameState.NewGame(0); s.Apply(new CreateDoujinCommand("First pages", "adventure"));
        for (var d = 0; d < 180 && s.Series[0].Volumes.Count == 0; d++) s.Advance(24);
        Assert.True(GoalCatalog.Get("doujin-finished").Measure(s).Done);
        s.Apply(new StudioActionCommand(StudioAction.Print, s.Series[0].Volumes.Single().Id, Amount: 10, Value: (int)PrintTier.CopyShop));
        for (var d = 0; d < 30 && !GoalCatalog.Get("first-copy").Measure(s).Done; d++) s.Advance(24);
        Assert.True(GoalCatalog.Get("first-copy").Measure(s).Done);
    }

    [Fact] public void Reward_only_furniture_cannot_be_bought()
    {
        var s = GameState.NewGame(0); var l = s.Locations.Single(x => x.BusinessId == s.ControlledBusinessId);
        var ex = Assert.Throws<InvalidCommandException>(() => s.Apply(new ApplyOfficeLayoutCommand(l.Id, s.OfficeRevision,
            s.OfficeAt(l.Id).Placements.ToList(), [new(-1, "trophy-shelf")], [])));
        Assert.Contains("goal reward", ex.Message);
    }
}
