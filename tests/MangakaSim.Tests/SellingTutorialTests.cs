using MangakaSim.Rules;
using Xunit;
namespace MangakaSim.Tests;

// The selling tutorial (spec 2026-10-03, Q61): show selling as it happens.
public class SellingTutorialTests
{
    [Fact] public void Helper_Chan_says_books_sell_by_themselves_once_copies_reach_the_shops()
    {
        var s = PublishingTests.Started();
        PublishingTests.Until(s, () => s.Series[0].Volumes.Count == 1);
        s.Apply(new PauseSeriesCommand(s.Series[0].Id));
        var prefs = new GuidancePreferences();
        Assert.NotEqual("sell", CareerGuidance.Evaluate(s, prefs).Id);
        s.Apply(new StudioActionCommand(StudioAction.Print, s.Series[0].Volumes[0].Id, Amount: 10));
        Assert.Equal("delivery", CareerGuidance.Evaluate(s, prefs).Id);
        PublishingTests.Until(s, () => s.Stock(s.Series[0].Volumes[0].Id) > 0);
        var step = CareerGuidance.Evaluate(s, prefs);
        Assert.Equal("sell", step.Id);
        Assert.Equal("sold", step.Target);
        Assert.Contains("sell by themselves", step.Text);
        Assert.DoesNotContain("Monday", step.Text);
    }

    [Fact] public void A_first_copy_selling_on_the_arrival_tick_still_gets_the_sell_text_first()
    {
        var s = PublishingTests.Started();
        PublishingTests.Until(s, () => s.Series[0].Volumes.Count == 1);
        s.Apply(new PauseSeriesCommand(s.Series[0].Id));
        s.Apply(new StudioActionCommand(StudioAction.Print, s.Series[0].Volumes[0].Id, Amount: 10));
        PublishingTests.Until(s, () => s.Stock(s.Series[0].Volumes[0].Id) > 0);
        Assert.True(s.Series[0].Volumes[0].CopiesSold > 0); // the first copy sold on the arrival tick itself
        var prefs = new GuidancePreferences();
        CareerGuidance.Observe(s, prefs);
        CareerGuidance.Observe(s, prefs);
        Assert.Equal(["sell", "sell-more"], prefs.Thread.Select(m => m.Step).SkipWhile(x => x != "sell"));
    }

    [Fact] public void An_online_only_first_release_says_the_download_is_on_sale_now()
    {
        var s = PublishingTests.Started();
        PublishingTests.Until(s, () => s.Series[0].Volumes.Count == 1);
        s.Apply(new PauseSeriesCommand(s.Series[0].Id));
        s.Apply(new PublishDoujinOnlineCommand(s.Series[0].Volumes[0].Id));
        var step = CareerGuidance.Evaluate(s, new GuidancePreferences());
        Assert.Equal("sell-online", step.Id);
        Assert.Equal("sold", step.Target);
        Assert.Contains("on sale now", step.Text);
        Assert.DoesNotContain("Monday", step.Text);
    }

    [Fact] public void After_the_first_sale_she_suggests_online_and_conventions_then_moves_on()
    {
        var (s, book) = StreamingSalesTests.PrintedBook();
        var prefs = new GuidancePreferences();
        CareerGuidance.Observe(s, prefs); // the player sees the "sell" text while the copies are in the shops
        PublishingTests.Until(s, () => book.CopiesSold > 0);
        var step = CareerGuidance.Evaluate(s, prefs);
        Assert.Equal("sell-more", step.Id);
        Assert.Equal("sell-more", step.Target);
        CareerGuidance.Observe(s, prefs); // writes the message to the thread
        Assert.Contains(prefs.Thread, m => m.Step == "sell-more");
        s.Advance(24 * 4);
        Assert.NotEqual("sell-more", CareerGuidance.Evaluate(s, prefs).Id);
    }

    // Final review fix 4: decided from game history, not only the thread, so older saves and trimmed threads do not repeat it.
    [Fact] public void An_older_save_long_past_its_first_sale_does_not_hear_about_first_readers_again()
    {
        var (s, book) = StreamingSalesTests.PrintedBook();
        PublishingTests.Until(s, () => book.CopiesSold > 0);
        Assert.Contains(CareerGuidance.Evaluate(s, new GuidancePreferences()).Id, new[] { "sell", "sell-more" }); // a fresh first sale still does
        s.Advance(24 * 30); // never listed online, no convention, not serialized
        var loaded = new GuidancePreferences(); // an older save's thread has no selling steps
        Assert.DoesNotContain(CareerGuidance.Evaluate(s, loaded).Id, new[] { "sell", "sell-more" });
    }

    [Fact] public void A_trimmed_thread_does_not_bring_the_sell_more_suggestion_back()
    {
        var (s, book) = StreamingSalesTests.PrintedBook();
        var prefs = new GuidancePreferences();
        CareerGuidance.Observe(s, prefs);
        PublishingTests.Until(s, () => book.CopiesSold > 0);
        CareerGuidance.Observe(s, prefs);
        Assert.Contains(prefs.Thread, m => m.Step == "sell-more");
        s.Advance(24 * 15);
        prefs.Thread.RemoveAll(m => m.Step is "sell" or "sell-more"); // trimmed away by the thread limit
        Assert.DoesNotContain(CareerGuidance.Evaluate(s, prefs).Id, new[] { "sell", "sell-more" });
    }

    [Fact] public void Listing_online_ends_the_sell_more_suggestion()
    {
        var (s, book) = StreamingSalesTests.PrintedBook();
        var prefs = new GuidancePreferences();
        CareerGuidance.Observe(s, prefs); // the player sees the "sell" text while the copies are in the shops
        PublishingTests.Until(s, () => book.CopiesSold > 0);
        Assert.Equal("sell-more", CareerGuidance.Evaluate(s, prefs).Id);
        s.Apply(new PublishDoujinOnlineCommand(book.Id));
        Assert.NotEqual("sell-more", CareerGuidance.Evaluate(s, prefs).Id);
    }

    [Fact] public void No_selling_text_promises_a_Monday_settlement()
    {
        Assert.DoesNotContain(GoalCatalog.Goals, g => g.Tip.Contains("Monday"));
        var (s, book) = StreamingSalesTests.PrintedBook();
        var status = s.DescribeDoujin(book.Id);
        Assert.DoesNotContain("Monday", status.LocalSales);
        Assert.Contains("10:00 to 20:00", status.LocalSales);
        Assert.True(SalesRules.IsShopHour(status.NextCheck!.Value));
    }
}
