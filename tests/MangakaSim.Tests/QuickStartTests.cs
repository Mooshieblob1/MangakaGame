using Xunit;

namespace MangakaSim.Tests;

/// <summary>Quick start (Q65, tester C's C1): a new career opens with one short text and Show me; her other tips wait until relevant.</summary>
public class QuickStartTests
{
    [Fact]
    public void A_new_career_opens_with_one_short_text()
    {
        var s = GameState.NewGame(); var p = new GuidancePreferences();
        CareerGuidance.Observe(s, p); CareerGuidance.Observe(s, p);
        var message = Assert.Single(p.Thread);
        Assert.Equal("create", message.Step);
        Assert.Equal([CareerGuidance.FirstText], message.Texts);
        Assert.Equal("create", CareerGuidance.Evaluate(s, p).Target); // Show me opens the New doujin page
    }

    [Fact]
    public void The_goals_chapter_is_introduced_with_the_first_doujin()
    {
        var s = GameState.NewGame(); var p = new GuidancePreferences();
        CareerGuidance.Observe(s, p);
        Assert.DoesNotContain(p.Thread, m => m.Texts.Contains(CareerGuidance.ChapterOpenText(GoalCatalog.Chapters[0])));
        s.Apply(new CreateDoujinCommand("Small", "drama")); CareerGuidance.Observe(s, p); CareerGuidance.Observe(s, p);
        Assert.Single(p.Thread, m => m.Texts.Contains(CareerGuidance.ChapterOpenText(GoalCatalog.Chapters[0])));
        Assert.Equal("produce", p.Thread[^1].Step);
    }

    [Fact]
    public void The_opening_text_never_changes_the_career()
    {
        var s = GameState.NewGame(); var before = s.ToJson();
        CareerGuidance.Observe(s, new GuidancePreferences());
        Assert.Equal(before, s.ToJson());
        Assert.True(CareerGuidance.FirstText.Length <= CareerGuidance.TextLimit);
    }
}
