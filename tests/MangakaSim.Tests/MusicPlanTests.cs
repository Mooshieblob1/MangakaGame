using Xunit;
namespace MangakaSim.Tests;

public class MusicPlanTests
{
    static MusicContext At(int hour, int day = 1, bool menu = false, bool moved = false, bool overnight = false) =>
        new(menu, new DateTime(1996, 4, day, hour, 0, 0), moved, overnight);

    /// <summary>Runs quiet time until the plan starts a track, returning the command and the seconds waited.</summary>
    static (MusicCommand Command, double Waited) UntilStart(MusicPlan plan, MusicContext context, double step = 1, double limit = 1000)
    {
        for (double t = step; t <= limit; t += step)
            if (plan.Update(step, context, false) is { Transition: not MusicTransition.None } command) return (command, t);
        throw new Xunit.Sdk.XunitException("No track started.");
    }
    static MusicCommand Finish(MusicPlan plan, MusicContext context) => plan.Update(0, context, true);

    [Fact] public void Career_starts_with_a_day_track_after_a_short_delay()
    {
        var plan = new MusicPlan(["day-01", "night-01"], 1);
        Assert.Equal(MusicTransition.None, plan.Update(1, At(10), false).Transition);
        var (start, waited) = UntilStart(plan, At(10));
        Assert.Equal(new MusicCommand(MusicTransition.FadeIn, "day-01"), start);
        Assert.InRange(waited, MusicPlan.FirstTrackDelay - 1, MusicPlan.FirstTrackDelay + 1);
    }

    [Fact] public void Night_hours_use_the_night_pool()
    {
        var plan = new MusicPlan(["day-01", "night-01"], 1);
        Assert.Equal("night-01", UntilStart(plan, At(22)).Command.TrackId);
    }

    [Fact] public void Studio_tracks_join_the_day_pool_after_moving_out()
    {
        var home = new MusicPlan(["day-01", "day-02", "studio-01"], 3);
        var moved = new MusicPlan(["day-01", "day-02", "studio-01"], 3);
        var atHome = new HashSet<string>(); var afterMove = new HashSet<string>();
        for (int i = 0; i < 30; i++)
        {
            atHome.Add(UntilStart(home, At(10)).Command.TrackId!); Finish(home, At(10));
            afterMove.Add(UntilStart(moved, At(10, moved: true)).Command.TrackId!); Finish(moved, At(10, moved: true));
        }
        Assert.DoesNotContain("studio-01", atHome);
        Assert.Contains("studio-01", afterMove);
    }

    [Fact] public void The_same_track_never_plays_twice_in_a_row()
    {
        var plan = new MusicPlan(["day-01", "day-02"], 5);
        string? last = null;
        for (int i = 0; i < 20; i++)
        {
            var track = UntilStart(plan, At(10)).Command.TrackId;
            Assert.NotEqual(last, track); last = track; Finish(plan, At(10));
        }
    }

    // Q58 (2026-10-03): the quiet between tracks shortened from 60 to 120 seconds, which players heard as the music stopping.
    [Fact] public void Quiet_gaps_last_15_to_30_seconds()
    {
        var plan = new MusicPlan(["day-01", "day-02"], 7);
        UntilStart(plan, At(10));
        for (int i = 0; i < 10; i++)
        {
            Assert.Equal(MusicTransition.None, Finish(plan, At(10)).Transition);
            Assert.InRange(UntilStart(plan, At(10), 0.5).Waited, 15, 30.5);
        }
    }

    [Fact] public void Time_of_day_is_read_only_when_a_track_starts()
    {
        var plan = new MusicPlan(["day-01", "night-01"], 1);
        Assert.Equal("day-01", UntilStart(plan, At(10)).Command.TrackId);
        // At 32x the game flips to night within one track; nothing switches.
        for (int i = 0; i < 50; i++) Assert.Equal(MusicTransition.None, plan.Update(0.2, At(i % 2 == 0 ? 22 : 10), false).Transition);
        Finish(plan, At(22));
        Assert.Equal("night-01", UntilStart(plan, At(22)).Command.TrackId);
    }

    [Fact] public void A_big_moment_crossfades_in_once_per_game_day()
    {
        var plan = new MusicPlan(["day-01", "good-news-01"], 1);
        UntilStart(plan, At(10));
        plan.Notice([MusicMoment.GoodNews], At(11));
        Assert.Equal(new MusicCommand(MusicTransition.Crossfade, "good-news-01"), plan.Update(0.1, At(11), false));
        Assert.Equal(MusicTransition.None, Finish(plan, At(12)).Transition);
        plan.Notice([MusicMoment.GoodNews], At(13));
        Assert.Equal(MusicTransition.None, plan.Update(0.1, At(13), false).Transition);
        plan.Notice([MusicMoment.GoodNews], At(9, day: 2));
        Assert.Equal("good-news-01", plan.Update(0.1, At(9, day: 2), false).TrackId);
    }

    [Fact] public void A_setback_wins_when_moments_arrive_together()
    {
        var plan = new MusicPlan(["day-01", "good-news-01", "setback-01", "deadline-01"], 1);
        UntilStart(plan, At(10));
        plan.Notice([MusicMoment.GoodNews, MusicMoment.Deadline, MusicMoment.Setback], At(10));
        Assert.Equal("setback-01", plan.Update(0.1, At(10), false).TrackId);
    }

    [Fact] public void A_lower_moment_does_not_interrupt_a_higher_one()
    {
        var plan = new MusicPlan(["day-01", "good-news-01", "setback-01"], 1);
        UntilStart(plan, At(10));
        plan.Notice([MusicMoment.Setback], At(10)); plan.Update(0.1, At(10), false);
        plan.Notice([MusicMoment.GoodNews], At(11));
        Assert.Equal(MusicTransition.None, plan.Update(0.1, At(11), false).Transition);
        Assert.Equal("setback-01", plan.Current);
    }

    [Fact] public void Nothing_interrupts_during_the_overnight_skip()
    {
        var plan = new MusicPlan(["night-01", "setback-01"], 1);
        UntilStart(plan, At(22));
        plan.Notice([MusicMoment.Setback], At(23, overnight: true));
        Assert.Equal(MusicTransition.None, plan.Update(0.1, At(23, overnight: true), false).Transition);
    }

    [Fact] public void Menus_play_the_title_and_leaving_returns_to_the_rotation()
    {
        var plan = new MusicPlan(["title-01", "day-01"], 1);
        Assert.Equal(new MusicCommand(MusicTransition.FadeIn, "title-01"), plan.Update(0.1, At(10, menu: true), false));
        Assert.Equal(MusicTransition.None, plan.Update(0.1, At(10, menu: true), false).Transition);
        Assert.Equal(new MusicCommand(MusicTransition.FadeIn, "title-01"), Finish(plan, At(10, menu: true)));
        Assert.Equal(new MusicCommand(MusicTransition.Crossfade, "day-01"), plan.Update(0.1, At(10), false));
    }

    [Fact] public void No_tracks_means_quiet_everywhere()
    {
        var plan = new MusicPlan([], 1);
        plan.Notice([MusicMoment.Setback], At(10));
        for (int i = 0; i < 500; i++) Assert.Equal(MusicTransition.None, plan.Update(1, At(i % 24, menu: i % 7 == 0), false).Transition);
        Assert.Null(plan.Current);
    }

    [Fact] public void A_moment_without_tracks_does_not_interrupt()
    {
        var plan = new MusicPlan(["day-01"], 1);
        UntilStart(plan, At(10));
        plan.Notice([MusicMoment.GoodNews], At(10));
        Assert.Equal(MusicTransition.None, plan.Update(0.1, At(10), false).Transition);
        Assert.Equal("day-01", plan.Current);
    }

    [Fact] public void Removing_every_track_leaves_quiet_without_retrying_every_frame()
    {
        var plan = new MusicPlan(["day-01"], 1);
        UntilStart(plan, At(10));
        plan.Remove("day-01");
        Assert.Null(plan.Current);
        for (int i = 0; i < 300; i++) Assert.Equal(MusicTransition.None, plan.Update(1, At(10), false).Transition);
    }

    [Fact] public void Unknown_names_are_not_music()
    {
        Assert.Null(MusicPlan.PoolOf("README"));
        Assert.Null(MusicPlan.PoolOf("boss-01"));
        Assert.Equal(MusicPool.GoodNews, MusicPlan.PoolOf("good-news-02"));
    }

    [Fact] public void Discover_strips_export_suffixes_and_ignores_other_files()
    {
        var found = MusicPlan.Discover(["day-01.ogg.import", "night-01.ogg", "README.md", "boss-01.ogg", "title-01.mp3.import", "day-01.ogg"], "res://Assets/Music");
        Assert.Equal(3, found.Count);
        Assert.Equal("res://Assets/Music/day-01.ogg", found["day-01"]);
        Assert.Equal("res://Assets/Music/title-01.mp3", found["title-01"]);
        Assert.True(found.ContainsKey("night-01"));
    }
}
