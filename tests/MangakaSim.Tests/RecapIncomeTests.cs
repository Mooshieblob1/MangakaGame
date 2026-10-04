using Xunit;
namespace MangakaSim.Tests;

// Streaming sales (spec 2026-10-03): shops sell until 20:00, after the day's recap has fired, and income merges into one
// ledger line per day. Each recap counts what the one before left out, so no evening sale is lost or counted twice.
public class RecapIncomeTests
{
    private static long TotalEarned(GameState s) => s.Ledger.Where(e => e.Amount > 0 && e.Kind == AccountEntryKind.Publishing).Sum(e => e.Amount);
    private static GameEvent NextRecap(GameState s)
    {
        var count = s.Events.Count(e => e.Type == EventType.DailyRecap);
        for (var h = 0; h < 48 && s.Events.Count(e => e.Type == EventType.DailyRecap) == count; h++) s.Advance(1);
        Assert.True(s.Events.Count(e => e.Type == EventType.DailyRecap) > count, "no recap within two days");
        return s.Events.Last(e => e.Type == EventType.DailyRecap);
    }

    [Fact] public void Sales_after_the_recap_fires_appear_in_the_next_days_recap_and_nothing_is_counted_twice()
    {
        var s = PublishingTests.Started();
        PublishingTests.Until(s, () => s.Series[0].Volumes.Count == 1);
        s.Apply(new StudioActionCommand(StudioAction.Print, s.Series[0].Volumes[0].Id, Amount: 100));
        s.Series[0].Fanbase = 2000; // enough demand for sales in most shop hours
        PublishingTests.Until(s, () => s.Stock(s.Series[0].Volumes[0].Id) > 0);
        long shown = 0;
        var eveningSales = false;
        // Find a day whose recap fires before closing time and leaves evening sales behind it.
        for (var day = 0; day < 30 && !eveningSales; day++)
        {
            var recap = NextRecap(s);
            shown += recap.Recap!.YenEarned;
            Assert.Equal(TotalEarned(s), shown); // the recap itself accounts for everything earned so far
            if (s.Clock.Now.Date != recap.ActivityDate || s.Clock.Hour >= 20) continue;
            s.Advance(20 - s.Clock.Hour); // on to the last shop hour of the day
            eveningSales = TotalEarned(s) > shown;
        }
        Assert.True(eveningSales, "precondition: evening sales after a recap");
        shown += NextRecap(s).Recap!.YenEarned;
        Assert.Equal(TotalEarned(s), shown); // the evening sales appear in the next recap, once
    }
}
