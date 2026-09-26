using Xunit;

namespace MangakaSim.Tests;

/// <summary>Tier 1 mid-career pacing: Helper-Chan's one-time 32x introduction and the fast-day stop list (Q27, Q28).</summary>
public class MidCareerPacingTests
{
    private static GameState Sold(int seed)
    {
        var s = GameState.NewGame(seed); s.Apply(new CreateDoujinCommand("First pages", "adventure"));
        for (int day = 0; day < 180 && s.Series[0].Volumes.Count == 0; day++) s.Advance(24);
        s.Apply(new StudioActionCommand(StudioAction.Print, s.Series[0].Volumes.Single().Id, Amount: 10, Value: (int)PrintTier.CopyShop));
        s.Advance(24 * 8);
        return s;
    }

    /// <summary>Guidance that has caught up with the career and has nothing unread.</summary>
    private static GuidancePreferences Caught(GameState s)
    {
        var p = new GuidancePreferences(); CareerGuidance.Observe(s, p); CareerGuidance.MarkRead(p);
        return p;
    }

    [Fact]
    public void Quiet_speed_text_appears_once_after_a_quiet_day_following_the_first_sale()
    {
        var s = Sold(0); var p = Caught(s);
        Assert.Contains("first-sale", p.Completed);
        var dayStart = s.Clock.Now; s.Advance(24);
        Assert.True(CareerGuidance.OfferQuietSpeed(s, p, dayStart));
        var text = p.Thread.Last();
        Assert.Equal(CareerGuidance.QuietSpeedStep, text.Step);
        Assert.False(text.Read);
        Assert.Contains("32×", text.Texts.Single());
        Assert.All(text.Texts, t => Assert.True(t.Length <= CareerGuidance.TextLimit, t));
        Assert.Contains(CareerGuidance.QuietSpeedStep, p.Completed);

        // Never twice, even after the text is read and another quiet day passes.
        CareerGuidance.MarkRead(p); dayStart = s.Clock.Now; s.Advance(24);
        Assert.False(CareerGuidance.OfferQuietSpeed(s, p, dayStart));
        Assert.Single(p.Thread, m => m.Step == CareerGuidance.QuietSpeedStep);

    }

    [Fact]
    public void Quiet_speed_text_does_not_make_the_career_step_repeat()
    {
        var s = Sold(0); var dayStart = s.Clock.Now; s.Advance(24);
        var p = Caught(s);
        Assert.True(CareerGuidance.OfferQuietSpeed(s, p, dayStart));
        var count = p.Thread.Count; CareerGuidance.Observe(s, p);
        Assert.Equal(count, p.Thread.Count);
    }

    [Fact]
    public void Quiet_speed_text_waits_for_the_first_sale()
    {
        var s = GameState.NewGame(0); s.Apply(new CreateDoujinCommand("First pages", "adventure"));
        var p = Caught(s);
        var dayStart = s.Clock.Now; s.Advance(24);
        Assert.False(CareerGuidance.OfferQuietSpeed(s, p, dayStart));
        Assert.DoesNotContain(CareerGuidance.QuietSpeedStep, p.Completed);
    }

    [Fact]
    public void Quiet_speed_text_waits_for_unread_texts_and_days_with_a_stop()
    {
        var s = Sold(0); var p = Caught(s);
        CareerGuidance.Say(p, s.Clock.Now, "Unread notice");
        var dayStart = s.Clock.Now; s.Advance(24);
        Assert.False(CareerGuidance.OfferQuietSpeed(s, p, dayStart));

        CareerGuidance.MarkRead(p); dayStart = s.Clock.Now; s.Advance(24);
        s.Events.Add(new GameEvent { Time = s.Clock.Now, Type = EventType.CancellationWarning, Message = "Staged stop" });
        Assert.False(CareerGuidance.OfferQuietSpeed(s, p, dayStart));
        Assert.DoesNotContain(CareerGuidance.QuietSpeedStep, p.Completed);
    }

    [Fact]
    public void Choosing_32x_first_counts_as_introduced()
    {
        var s = Sold(0); var p = Caught(s);
        p.Completed.Add(CareerGuidance.QuietSpeedStep);
        var dayStart = s.Clock.Now; s.Advance(24);
        Assert.False(CareerGuidance.OfferQuietSpeed(s, p, dayStart));
    }

    [Fact]
    public void Stop_list_matches_Q27_and_leaves_routine_events_out()
    {
        Assert.Equal(10, CareerGuidance.FastSpeedStops.Count);
        Assert.DoesNotContain(EventType.DailyRecap, CareerGuidance.FastSpeedStops);
        Assert.DoesNotContain(EventType.ChapterCompleted, CareerGuidance.FastSpeedStops);
        Assert.Contains(EventType.SerializationOffered, CareerGuidance.FastSpeedStops);
        Assert.Contains(EventType.WageArrears, CareerGuidance.FastSpeedStops);
    }

    [Fact]
    public void Quiet_speed_offer_is_read_only_toward_the_simulation()
    {
        var s = Sold(0); var p = Caught(s);
        var dayStart = s.Clock.Now; s.Advance(24);
        var json = s.ToJson();
        CareerGuidance.OfferQuietSpeed(s, p, dayStart);
        Assert.Equal(json, s.ToJson());
    }
}
