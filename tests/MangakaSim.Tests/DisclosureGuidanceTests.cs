using Xunit;
namespace MangakaSim.Tests;

// Progressive disclosure (spec 2026-10-02): each opened part is announced once, with a "New" tag, and never stops 32x.
public class DisclosureGuidanceTests
{
    [Fact] public void An_opened_part_is_announced_once_and_tagged_new()
    {
        var s = GoalProgressTests.FirstSale(); var prefs = new GuidancePreferences();
        CareerGuidance.ObserveParts(s, prefs); CareerGuidance.ObserveParts(s, prefs);
        Assert.Single(prefs.Thread, m => m.Texts.Single() == DisclosureCatalog.Get("books").Announcement);
        Assert.Contains("books", prefs.NewParts);
        Assert.DoesNotContain("quiet-speed", prefs.NewParts);
        Assert.All(prefs.Thread, m => Assert.Equal(CareerGuidance.GoalStep, m.Step));
        Assert.Equal(0, CareerGuidance.UnreadStopping(prefs));
    }

    [Fact] public void Parts_from_an_older_save_and_under_show_every_screen_pass_silently()
    {
        var old = GameState.NewGame(0); old.Disclosure = null;
        old.Apply(new CreateDoujinCommand("First pages", "adventure"));
        for (var d = 0; d < 180 && old.Series[0].Volumes.Count == 0; d++) old.Advance(24);
        var prefs = new GuidancePreferences();
        CareerGuidance.ObserveParts(GameState.FromJson(old.ToJson()), prefs);
        Assert.Empty(prefs.Thread); Assert.Empty(prefs.NewParts);
        var every = GameState.NewGame(0); every.Apply(new ShowEveryScreenCommand(true)); every.Apply(new OpenPartCommand("staff"));
        CareerGuidance.ObserveParts(every, prefs);
        Assert.Empty(prefs.Thread); Assert.Empty(prefs.NewParts);
    }

    [Fact] public void Every_announcement_fits_a_phone_bubble()
    {
        foreach (var p in DisclosureCatalog.Parts.Where(p => p.Announcement is not null))
            Assert.True(p.Announcement!.Length <= CareerGuidance.TextLimit, p.Id);
    }
}
