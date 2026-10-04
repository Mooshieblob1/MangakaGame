using Xunit;
namespace MangakaSim.Tests;

// Career goals board (spec 2026-10-01, Q51): Helper-Chan says how; goal texts never stop 32x.
public class GoalGuidanceTests
{
    [Fact] public void Helper_chan_congratulates_each_goal_once()
    {
        var s = GoalProgressTests.FirstSale(); var prefs = new GuidancePreferences();
        CareerGuidance.ObserveGoals(s, prefs); CareerGuidance.ObserveGoals(s, prefs);
        Assert.Single(prefs.Thread, m => m.Texts.Single() == CareerGuidance.GoalDoneText(GoalCatalog.Get("first-copy")));
        Assert.Single(prefs.Thread, m => m.Texts.Single() == CareerGuidance.ChapterOpenText(GoalCatalog.Chapters[0]));
    }

    [Fact] public void Goals_counted_from_an_older_save_pass_silently()
    {
        var loaded = GameState.FromJson(GoalProgressTests.FirstSale(goals: false).ToJson()); var prefs = new GuidancePreferences();
        CareerGuidance.ObserveGoals(loaded, prefs);
        Assert.DoesNotContain(prefs.Thread, m => m.Texts.Any(t => t.StartsWith("Goal done")));
    }

    [Fact] public void Every_goal_text_fits_a_phone_bubble()
    {
        foreach (var g in GoalCatalog.Goals) Assert.True(CareerGuidance.GoalDoneText(g).Length <= CareerGuidance.TextLimit, g.Id);
        foreach (var c in GoalCatalog.Chapters) Assert.True(CareerGuidance.ChapterOpenText(c).Length <= CareerGuidance.TextLimit, c.Name);
    }

    [Fact] public void Goal_texts_do_not_count_as_stopping_but_her_other_texts_still_do()
    {
        var prefs = new GuidancePreferences();
        var before = CareerGuidance.StoppingMessages(prefs);
        CareerGuidance.ObserveGoals(GoalProgressTests.FirstSale(), prefs);
        Assert.Equal(before, CareerGuidance.StoppingMessages(prefs)); Assert.Equal(0, CareerGuidance.UnreadStopping(prefs));
        CareerGuidance.Say(prefs, GameClock.Start, "A stopping notice.");
        Assert.Equal(before + 1, CareerGuidance.StoppingMessages(prefs)); Assert.Equal(1, CareerGuidance.UnreadStopping(prefs));
    }

    [Fact] public void A_goal_text_does_not_make_her_repeat_the_current_step()
    {
        var s = GoalProgressTests.FirstSale(); var prefs = new GuidancePreferences();
        CareerGuidance.Observe(s, prefs); var step = CareerGuidance.Evaluate(s, prefs).Id;
        CareerGuidance.Observe(s, prefs);
        Assert.Single(prefs.Thread, m => m.Step == step);
    }

    [Fact] public void The_next_goal_is_the_first_unfinished_in_catalogue_order()
    {
        var s = GoalProgressTests.FirstSale();
        Assert.Equal("convention", CareerGuidance.NextGoal(s)!.Id);
    }

    [Fact] public void A_finished_chapter_stops_32x_and_plays_good_news_but_a_goal_does_not()
    {
        Assert.Contains(EventType.GoalChapterCompleted, CareerGuidance.FastSpeedStops);
        Assert.DoesNotContain(EventType.GoalCompleted, CareerGuidance.FastSpeedStops);
        var s = GameState.NewGame(0);
        var chapter = new GameEvent { Time = s.Clock.Now, Type = EventType.GoalChapterCompleted, Message = "Chapter complete" };
        Assert.Contains(MusicMoment.GoodNews, MusicMoments.Classify(s, [chapter], []));
    }
}
