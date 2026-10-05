using System.Text.RegularExpressions;
using Xunit;

namespace MangakaSim.Tests;

// Steam achievements (Q3, Q69): about 25 career milestones, recorded in the save and sent to Steam by the Godot layer.
public class AchievementTests
{
    private static GameState Doujin()
    {
        var s = GameState.NewGame(3); s.Apply(new CreateDoujinCommand("First pages", "adventure")); return s;
    }
    private static bool Earned(GameState s, string key) => s.Progression.Achievements.Any(a => a.Key == key);
    private static void NextDay(GameState s) => s.Advance(24);

    [Fact] public void Catalog_has_25_unique_steam_ready_achievements()
    {
        var all = ProgressionCatalog.Achievements;
        Assert.Equal(25, all.Length);
        Assert.Equal(all.Length, all.Select(a => a.Key).Distinct().Count());
        Assert.Equal(all.Length, all.Select(a => a.ApiName).Distinct().Count());
        Assert.All(all, a =>
        {
            Assert.Matches(new Regex("^MKG_[A-Z0-9_]+$"), a.ApiName);
            Assert.InRange(a.Name.Length, 1, 40);
            Assert.InRange(a.Requirement.Length, 1, 120);
            Assert.DoesNotContain("—", a.Name + a.Requirement);
        });
    }

    [Fact] public void Career_achievements_are_earned_from_the_save_and_stay_quiet()
    {
        var s = Doujin();
        s.ControlledBusiness.Incorporated = true;
        NextDay(s);
        Assert.True(Earned(s, "incorporate"));
        Assert.DoesNotContain(s.Events, e => e.Type == EventType.CareerMilestone && e.Message.Contains("incorporated"));
        Assert.Equal(s.ToJson(), GameState.FromJson(s.ToJson()).ToJson());
    }

    [Fact] public void Older_saves_earn_what_they_already_reached_on_their_next_day()
    {
        var s = Doujin(); NextDay(s);
        s.ControlledBusiness.Incorporated = true;
        var loaded = GameState.FromJson(s.ToJson());
        Assert.False(Earned(loaded, "incorporate"));
        NextDay(loaded);
        Assert.True(Earned(loaded, "incorporate"));
    }

    [Fact] public void Sandbox_saves_keep_the_milestone_but_earn_no_achievement()
    {
        var s = Doujin();
        s.Apply(new DifficultyCommand(CareerDifficulty.Sandbox));
        s.ControlledBusiness.Incorporated = true;
        NextDay(s);
        Assert.Contains(s.Progression.Milestones, m => m.Key == "incorporate");
        Assert.Empty(s.Progression.Achievements);
    }

    [Fact] public void A_convention_counts_only_once_copies_sell_there()
    {
        var s = Doujin();
        var booking = new ConventionBooking { Id = s.AllocateId(), BusinessId = s.ControlledBusinessId, PersonId = s.ProtagonistPersonId,
            Date = s.Clock.Now, Settled = true };
        s.Bookings.Add(booking);
        NextDay(s);
        Assert.False(Earned(s, "first_convention"));
        booking.CopiesSold = 4;
        NextDay(s);
        Assert.True(Earned(s, "first_convention"));
    }

    [Fact] public void On_time_run_resets_at_a_late_chapter_or_a_missed_issue()
    {
        var s = Doujin(); var title = s.Series[0];
        var late = new HashSet<int> { 4 };
        for (var n = 1; n <= 16; n++) title.Chapters.Add(new Chapter { Id = s.AllocateId(), Number = n, IsLate = late.Contains(n) });
        void Published(int n) => s.Events.Add(new GameEvent { Type = EventType.ChapterPublished, SeriesId = title.Id, ChapterNumber = n, Time = s.Clock.Now });
        for (var n = 1; n <= 9; n++) Published(n);
        Assert.Equal(5, s.LongestOnTimeRun([title]));
        s.Events.Add(new GameEvent { Type = EventType.IssueMissed, SeriesId = title.Id, Time = s.Clock.Now });
        for (var n = 10; n <= 16; n++) Published(n);
        Assert.Equal(7, s.LongestOnTimeRun([title]));
        title.Chapters.Add(new Chapter { Id = s.AllocateId(), Number = 17 }); Published(17);
        title.Chapters.Add(new Chapter { Id = s.AllocateId(), Number = 18 }); Published(18);
        title.Chapters.Add(new Chapter { Id = s.AllocateId(), Number = 19 }); Published(19);
        Assert.Equal(10, s.LongestOnTimeRun([title]));
    }

    [Fact] public void A_comeback_needs_a_contract_signed_after_a_cancellation()
    {
        var s = Doujin(); var title = s.Series[0];
        title.PastContracts.Add(new Contract(s.AllocateId(), "tokiwa-jump", 5000, s.Clock.Now, s.Clock.Now.AddDays(28)));
        NextDay(s);
        Assert.True(Earned(s, "serialization"));
        Assert.False(Earned(s, "comeback"));
        s.Events.Add(new GameEvent { Type = EventType.SeriesCancelled, SeriesId = title.Id, Time = s.Clock.Now });
        NextDay(s);
        Assert.False(Earned(s, "comeback"));
        title.PastContracts.Add(new Contract(s.AllocateId(), "tokiwa-jump", 5000, s.Clock.Now, s.Clock.Now.AddDays(28)));
        NextDay(s);
        Assert.True(Earned(s, "comeback"));
    }
}
