# Streaming Sales and the Selling Tutorial Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Sales planned each Monday are released across shop hours (10:00 to 20:00 daily) instead of in one Monday lump, and a new player is shown that printed copies sell by themselves.

**Architecture:** Monday planning keeps today's formulas and order but turns each volume's week into `SalesPlan` quotas stored on the volume; every shop hour `SalesStep` releases the whole copies due so far. A book that goes on sale midweek plans its first week at release. Presentation adds a Sold pulse, a first-sale bubble, Helper-Chan texts and a two-route "sell more" step.

**Tech Stack:** C# (.NET 8) engine-free simulation `src/MangakaSim`, xUnit `tests/MangakaSim.Tests`, Godot 4.7.2 .NET presentation `godot/`.

**Spec:** `docs/superpowers/specs/2026-10-03-streaming-sales-design.md`

## Global Constraints

- Shop hours: 10:00 to 20:00 every day, 70 shop hours a week; a "shop hour" is a tick whose clock (after `Clock.Advance`) reads 11:00 to 20:00 inclusive.
- Weekly totals per book stay the same; no sales formula, price or royalty rule changes.
- A midweek first week spreads over the shop hours left that week, or at least 30 shop hours.
- Ledger: one line per title, reason and day for streamed income.
- New save data is optional (older saves load); sales use no random draws; replays stay exact.
- Texts: no em dashes, metric units, "Helper-Chan", "Sold" as in the header.
- Docs use CRLF line endings; commit only when the user asks (plan commit steps are skipped unless asked; record that in the ledger).
- Building and running checks is authorised for this plan (user approved implementation); packaging is not.

## Review Focus

1. Calling `SalesStep()` twice in the same shop hour (tests and replays do this) must not sell twice: guarded by `LastShopHourAt`; pinned in Task 2.
2. A book released at Monday 00:00 (print delivery or release date on the planning tick) must not get both a release plan and a Monday plan: pinned in Task 4.
3. A volume whose stock runs out mid-week, then is reprinted mid-week, sells again from the next shop hour without exceeding the week's demand: pinned in Task 2.
4. An older save loaded mid-week (Monday lump already paid, no plans) must not pay that week again: pinned in Task 1.
5. Direct downloads keep a plan every week after the shop window closes; the window must still close: pinned in Task 3.

---

### Task 1: Sales plan model, shop-hour rules and save checks

**Files:**
- Modify: `src/MangakaSim/Market.cs` (Volume, GameState sales fields)
- Modify: `src/MangakaSim/Rules/SalesRules.cs`
- Modify: `src/MangakaSim/GameState.MarketValidation.cs`
- Test: `tests/MangakaSim.Tests/StreamingSalesTests.cs` (create)

**Interfaces:**
- Produces: `enum SaleKind { Shop, Commercial, Channel, Download }`; `class SalesPlan { SaleKind Kind; int AgreementId; DateTime Week; long Total; long Released; int Hours; int HoursDone; }`; `Volume.SalesPlans` (`List<SalesPlan>?`, optional); `GameState.LastShopHourAt` (`DateTime?`, optional); `SalesRules.ShopOpens=10`, `ShopCloses=20`, `ShopHoursPerWeek=70`, `MinimumFirstWeekHours=30`, `bool IsShopHour(DateTime)`, `long DueBy(long total,int done,int hours)`, `int ShopHoursUntil(DateTime from,DateTime until)`, `DateTime NextShopHour(DateTime from)`.

- [ ] **Step 1: Write the failing tests**

```csharp
using MangakaSim.Rules;
using Xunit;
namespace MangakaSim.Tests;

// Streaming sales (spec 2026-10-03, Q60): weekly plans released through shop hours, 10:00 to 20:00.
public class StreamingSalesTests
{
    [Fact] public void Shop_hours_are_the_ten_hours_from_ten_to_twenty()
    {
        var day = new DateTime(1996, 4, 8);
        Assert.Equal(10, Enumerable.Range(0, 24).Count(h => SalesRules.IsShopHour(day.AddHours(h))));
        Assert.False(SalesRules.IsShopHour(day.AddHours(10)));
        Assert.True(SalesRules.IsShopHour(day.AddHours(11)) && SalesRules.IsShopHour(day.AddHours(20)));
        Assert.Equal(70, SalesRules.ShopHoursUntil(day, day.AddDays(7)));
        Assert.Equal(day.AddHours(11), SalesRules.NextShopHour(day));
        Assert.Equal(day.AddDays(1).AddHours(11), SalesRules.NextShopHour(day.AddHours(20)));
    }

    [Fact] public void Hourly_shares_add_up_exactly_and_carry_fractions()
    {
        long released = 0;
        for (var h = 1; h <= 70; h++) released = SalesRules.DueBy(7, h, 70);
        Assert.Equal(7, released);
        Assert.Equal(0, SalesRules.DueBy(7, 9, 70));
        Assert.Equal(1, SalesRules.DueBy(7, 10, 70));
        Assert.Equal(long.MaxValue / 2, SalesRules.DueBy(long.MaxValue / 2, 70, 70));
    }

    [Fact] public void An_older_save_without_plans_loads_and_a_bad_plan_is_rejected()
    {
        var s = GameState.NewGame(0);
        var json = s.ToJson();
        Assert.DoesNotContain("SalesPlans\":[", json); // absent until a book is planned
        var loaded = GameState.FromJson(json);
        Assert.Null(loaded.LastShopHourAt);
        loaded.LastShopHourAt = new DateTime(1996, 4, 1, 9, 0, 0); // not a shop hour
        Assert.Throws<InvalidDataException>(() => GameState.FromJson(loaded.ToJson()));
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test tests/MangakaSim.Tests --filter "FullyQualifiedName~StreamingSalesTests"`
Expected: FAIL to compile ("SalesRules does not contain a definition for IsShopHour", "LastShopHourAt").

- [ ] **Step 3: Implement the model and rules**

In `src/MangakaSim/Market.cs`, after the `Volume` class's `AverageQuality` property add:

```csharp
    /// <summary>Open weekly sales plans released through shop hours (streaming sales, 2026-10-03). Absent in older saves.</summary>
    public List<SalesPlan>? SalesPlans { get; set; }
```

and add next to `LastSalesAt`:

```csharp
    /// <summary>The last shop hour whose sales were released, so a repeated step never sells twice. Absent in older saves.</summary>
    public DateTime? LastShopHourAt { get; set; }
```

and at file end:

```csharp
public enum SaleKind { Shop, Commercial, Channel, Download }
/// <summary>A week's quota of one kind of sale, released evenly over shop hours (streaming sales, 2026-10-03).</summary>
public sealed class SalesPlan
{
    [JsonRequired] public SaleKind Kind { get; set; }
    [JsonRequired] public int AgreementId { get; set; }
    [JsonRequired] public DateTime Week { get; set; }
    [JsonRequired] public long Total { get; set; }
    [JsonRequired] public long Released { get; set; }
    [JsonRequired] public int Hours { get; set; }
    [JsonRequired] public int HoursDone { get; set; }
}
```

In `src/MangakaSim/Rules/SalesRules.cs` add:

```csharp
    // Streaming sales (spec 2026-10-03, Q60): shops sell from 10:00 to 20:00 every day.
    public const int ShopOpens = 10, ShopCloses = 20, ShopHoursPerWeek = 70, MinimumFirstWeekHours = 30;
    /// <summary>True on the tick that ends a shop hour (the clock reads 11:00 to 20:00).</summary>
    public static bool IsShopHour(DateTime now) => now.Minute == 0 && now.Second == 0 && now.Hour > ShopOpens && now.Hour <= ShopCloses;
    /// <summary>Whole units due after <paramref name="done"/> of <paramref name="hours"/> shop hours.</summary>
    public static long DueBy(long total, int done, int hours) =>
        hours <= 0 || done >= hours ? total : (long)(new System.Numerics.BigInteger(total) * Math.Max(0, done) / hours);
    public static int ShopHoursUntil(DateTime from, DateTime until)
    {
        var count = 0;
        for (var t = from.Date.AddHours(from.Hour + 1); t <= until; t = t.AddHours(1)) if (IsShopHour(t)) count++;
        return count;
    }
    public static DateTime NextShopHour(DateTime from)
    {
        var t = from.Date.AddHours(from.Hour + 1);
        while (!IsShopHour(t)) t = t.AddHours(1);
        return t;
    }
```

In `src/MangakaSim/GameState.MarketValidation.cs`, after the `"sales timestamp"` check add:

```csharp
        Check(LastShopHourAt is null || (Time(LastShopHourAt.Value) && SalesRules.IsShopHour(LastShopHourAt.Value) && LastShopHourAt <= Clock.Now), "shop hour timestamp");
```

and, inside the per-volume loop after the `"volume release"` check:

```csharp
                Check(volume.SalesPlans is null || volume.SalesPlans.All(p => p is not null && Enum.IsDefined(p.Kind) && p.Total >= 0 &&
                    p.Released >= 0 && p.Released <= p.Total && p.Hours >= 1 && p.HoursDone >= 0 && p.HoursDone < p.Hours &&
                    Time(p.Week) && p.Week.TimeOfDay == TimeSpan.Zero && p.Week.DayOfWeek == DayOfWeek.Monday && p.Week <= Clock.Now &&
                    volume.ReleasedAt is not null && (p.Kind is SaleKind.Shop or SaleKind.Commercial) == (p.AgreementId == 0)), "sales plan");
```

Change the window part of `"volume release"` from `volume.SalesClosed == (volume.WeeksOnSale == volume.SalesWindowWeeks)` to:

```csharp
                    volume.SalesClosed == (volume.WeeksOnSale == volume.SalesWindowWeeks && !(volume.SalesPlans ?? []).Any(p => p.Kind != SaleKind.Download)) &&
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test tests/MangakaSim.Tests --filter "FullyQualifiedName~StreamingSalesTests"`
Expected: PASS (3 tests).

- [ ] **Step 5: Commit** (only if the user asks; otherwise ledger "not committed")

```bash
git add src/MangakaSim/Market.cs src/MangakaSim/Rules/SalesRules.cs src/MangakaSim/GameState.MarketValidation.cs tests/MangakaSim.Tests/StreamingSalesTests.cs
git commit -m "Streaming sales: plan model, shop hours and save checks"
```

---

### Task 2: Monday planning and hourly shop release for printed doujin

**Files:**
- Modify: `src/MangakaSim/GameState.Sales.cs` (`SalesStep`)
- Modify: `src/MangakaSim/GameState.Printing.cs` (`SellStock` daily ledger line)
- Modify: `src/MangakaSim/GameState.Operations.cs` (`AccountPostDaily`, creator share bill merge)
- Modify: `src/MangakaSim/GameState.Career.cs` (`RecordSales` weekly key)
- Test: `tests/MangakaSim.Tests/StreamingSalesTests.cs`

**Interfaces:**
- Consumes: Task 1 `SalesPlan`, `SalesRules.IsShopHour`, `DueBy`, `ShopHoursPerWeek`, `LastShopHourAt`.
- Produces: `private void PlanSalesWeek()`, `private void PlanVolumeWeek(Series series, Volume volume, double fans, double trend, int hours)`, `private void ReleaseSalesHour()`, `private void ApplySales(Series s, Volume v, long copies, long units, List<(Volume Volume,long Threshold)> milestones)`, `private void AwardMilestones(Series s, List<(Volume Volume,long Threshold)> milestones)`, `private void AccountPostDaily(CashAccount account, long amount, string reason, AccountEntryKind kind, int? seriesId)`, `SellStock(Series, Volume, long requested, bool convention, bool daily = false)`.

- [ ] **Step 1: Write the failing tests** (append to `StreamingSalesTests`)

```csharp
    // A printed doujin with stock, paused at the Monday planning tick.
    internal static (GameState State, Volume Book) PrintedBook()
    {
        var s = PublishingTests.Started();
        PublishingTests.Until(s, () => s.Series[0].Volumes.Count == 1);
        s.Apply(new PauseSeriesCommand(s.Series[0].Id));
        s.Apply(new StudioActionCommand(StudioAction.Print, s.Series[0].Volumes[0].Id, Amount: 100));
        s.Series[0].Fanbase = 2000; // enough demand to see several copies a week
        var monday = s.Clock.Now.Date.AddDays(((int)DayOfWeek.Monday - (int)s.Clock.DayOfWeek + 7) % 7);
        if (monday <= s.Clock.Now) monday = monday.AddDays(7);
        s.Advance(s.Clock.HoursUntil(monday));
        return (s, s.Series[0].Volumes[0]);
    }

    [Fact] public void Monday_plans_the_week_and_shops_sell_only_during_shop_hours()
    {
        var (s, book) = PrintedBook();
        var plan = Assert.Single(book.SalesPlans!, p => p.Kind == SaleKind.Shop);
        Assert.Equal(SalesRules.ShopHoursPerWeek, plan.Hours);
        var sold = book.CopiesSold; var stock = s.Stock(book.Id); var demand = book.WeeklyDemand;
        s.Advance(10); // 00:00 to 10:00: shops closed
        Assert.Equal(sold, book.CopiesSold);
        s.Advance(24 * 7 - 14); // to Sunday 20:00, the week's last shop hour
        Assert.Equal(plan.Released, plan.Total);
        Assert.Equal(stock - (book.CopiesSold - sold), s.Stock(book.Id));
        Assert.True(book.CopiesSold > sold);
        Assert.Equal(demand - (book.CopiesSold - sold), book.WeeklyDemand); // shop sales draw down the demand conventions share
    }

    [Fact] public void A_repeated_step_in_the_same_shop_hour_sells_nothing_more()
    {
        var (s, book) = PrintedBook();
        s.Advance(15);
        var json = s.ToJson();
        s.SalesStep();
        Assert.Equal(json, s.ToJson());
    }

    [Fact] public void Streamed_income_keeps_one_ledger_line_per_title_and_day()
    {
        var (s, _) = PrintedBook();
        s.Advance(24);
        var lines = s.ControlledBusiness.Account.Entries.Where(e => e.Reason == "doujin sales" && e.Time.Date == s.Clock.Now.Date.AddDays(-1)).ToList();
        Assert.True(lines.Count <= 1);
        Assert.Equal(s.ToJson(), GameState.FromJson(s.ToJson()).ToJson());
    }

    [Fact] public void Copies_due_with_no_stock_are_missed_and_a_midweek_reprint_sells_again()
    {
        var (s, book) = PrintedBook();
        foreach (var run in s.PrintRuns.Where(r => r.VolumeId == book.Id)) run.Remaining = 0;
        s.Advance(24 * 2);
        var during = book.CopiesSold;
        s.Apply(new StudioActionCommand(StudioAction.Print, book.Id, Amount: 50));
        s.Advance(24 * 4);
        Assert.True(book.CopiesSold > during);
        Assert.True(book.WeeklyDemand >= 0);
    }

    [Fact] public void An_older_save_loaded_midweek_is_not_paid_again()
    {
        var (s, book) = PrintedBook();
        s.Advance(24 * 3);
        book.SalesPlans = null; // the old rules already paid this week on Monday
        s.World.ReplayCheckpoint = null; s.World.ReplayLogStart = s.CommandLog.Count; s.World.ReplayCheckpoint = s.ToJson();
        var loaded = GameState.FromJson(s.ToJson());
        var lifted = loaded.Series[0].Volumes[0];
        var sold = lifted.CopiesSold;
        var nextMonday = loaded.Clock.Now.Date.AddDays(((int)DayOfWeek.Monday - (int)loaded.Clock.DayOfWeek + 7) % 7);
        loaded.Advance(loaded.Clock.HoursUntil(nextMonday) - 1); // up to Sunday 23:00: nothing planned, nothing paid
        Assert.Equal(sold, lifted.CopiesSold);
        loaded.Advance(24 * 7);
        Assert.True(lifted.CopiesSold > sold); // streaming picks up from that Monday
    }
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test tests/MangakaSim.Tests --filter "FullyQualifiedName~StreamingSalesTests"`
Expected: FAIL (no Shop plan is created; sales still arrive at Monday 00:00).

- [ ] **Step 3: Implement planning and release**

Replace `SalesStep` in `src/MangakaSim/GameState.Sales.cs` with:

```csharp
    internal void SalesStep()
    {
        foreach (var series in Series.OrderBy(s => s.Id))
            foreach (var volume in series.Volumes.OrderBy(v => v.Id).Where(v => v.ReleasedAt is null && v.ReleaseDate <= Clock.Now &&
                (!v.IsDoujin || ActiveChannel(series, v, ReleaseChannel.DomesticDigital) is not null))) ReleaseVolume(series, volume);
        if (Clock.DayOfWeek == DayOfWeek.Monday && Clock.Hour == 0 && LastSalesAt != Clock.Now) PlanSalesWeek();
        // Streaming sales (spec 2026-10-03): each shop hour releases the share of every plan due so far.
        if (SalesRules.IsShopHour(Clock.Now) && LastShopHourAt != Clock.Now) { LastShopHourAt = Clock.Now; ReleaseSalesHour(); }
    }
    private void PlanSalesWeek()
    {
        LastSalesAt = Clock.Now;
        if (Clock.Now.Day <= 7)
        {
            if (DoujinCopiesThisMonth > 0) Emit(EventType.ConventionRecap,
                $"Last month: {DoujinCopiesThisMonth:N0} doujin copies, {DoujinFansThisMonth:N0} new fans.");
            DoujinCopiesThisMonth = 0;
            DoujinFansThisMonth = 0;
        }
        foreach (var series in Series.OrderBy(s => s.Id))
        {
            var fans = series.Fanbase;
            var trend = series.IsIconic ? 1 : GenrePopularity(series.Genre);
            // A book that went on sale on this very tick was planned at release (Task 4).
            foreach (var volume in series.Volumes.OrderBy(v => v.Id).Where(v => v.ReleasedAt is not null && v.ReleasedAt != Clock.Now &&
                (!v.SalesClosed || DirectDownload(v) is not null)))
                PlanVolumeWeek(series, volume, fans, trend, SalesRules.ShopHoursPerWeek);
            if (BusinessOf(series.BusinessId).HasInternet && series.Publishing == PublishingStatus.Unpublished && series.Volumes.Any(v => v.ReleasedAt is not null))
            {
                var mouth = series.Fanbase * .01 * Economy.InternetReach(TrendCatalog, Clock.Now);
                series.Fanbase += mouth;
                DoujinFansThisMonth += mouth;
            }
            CheckIconic(series);
        }
    }
    // The week's demand, with today's formulas, becomes plans released through shop hours.
    private void PlanVolumeWeek(Series series, Volume volume, double fans, double trend, int hours)
    {
        var copies = volume.IsDoujin ? SalesRules.DoujinCopies(fans, volume.AverageQuality, trend,
            BusinessOf(volume.BusinessId).HasInternet ? Economy.InternetReach(TrendCatalog, Clock.Now) : 0, volume.WeeksOnSale + 1) :
            SalesRules.CommercialCopies(fans, volume.AverageQuality, trend, volume.WeeksOnSale + 1, SeriesTier(series));
        var potential = (long)Math.Floor(copies*(1+series.Reach/100)*RivalDemand(series.Genre,volume.AverageQuality)*RecognitionLift(series.Id));
        volume.SalesPlans ??= new();
        PlanChannelDemand(series, volume, potential, hours);
        RecordSales(series, volume, 0, weekly: true);
        if (volume.SalesClosed) return;
        long total;
        if (volume.IsDoujin) { DemandFor(series, volume); total = (long)Math.Floor(volume.WeeklyDemand * .3); }
        else total = ChannelPhysicalDemand(series, volume, potential);
        volume.SalesPlans.Add(new() { Kind = volume.IsDoujin ? SaleKind.Shop : SaleKind.Commercial, Week = Monday(Clock.Now), Total = total, Hours = hours });
        volume.WeeksOnSale++;
    }
    private void ReleaseSalesHour()
    {
        foreach (var series in Series.OrderBy(s => s.Id))
        {
            var milestones = new List<(Volume Volume, long Threshold)>();
            foreach (var volume in series.Volumes.OrderBy(v => v.Id).Where(v => v.SalesPlans is { Count: > 0 }))
            {
                long copies = 0, units = 0;
                foreach (var plan in volume.SalesPlans!.ToList())
                {
                    plan.HoursDone++;
                    var due = SalesRules.DueBy(plan.Total, plan.HoursDone, plan.Hours) - plan.Released;
                    plan.Released += due;
                    if (due > 0)
                        switch (plan.Kind)
                        {
                            case SaleKind.Shop: copies += SellStock(series, volume, due, false, daily: true); break;
                            case SaleKind.Commercial: copies += due; break;
                            default: units += ReleaseChannelUnits(series, volume, plan, due); break;
                        }
                    if (plan.HoursDone >= plan.Hours) volume.SalesPlans.Remove(plan);
                }
                ApplySales(series, volume, copies, units, milestones);
                if (volume.WeeksOnSale >= volume.SalesWindowWeeks && !volume.SalesPlans.Any(p => p.Kind != SaleKind.Download)) volume.SalesClosed = true;
            }
            AwardMilestones(series, milestones);
            CheckIconic(series);
        }
    }
    private void ApplySales(Series series, Volume volume, long copies, long units, List<(Volume Volume, long Threshold)> milestones)
    {
        if (copies == 0 && units == 0) return;
        var old = volume.CopiesSold;
        volume.CopiesSold = checked(old + copies);
        RecordSales(series, volume, copies, weekly: true);
        if (!volume.IsDoujin)
        {
            var (printed, reprint) = (SalesRules.Rung(old), SalesRules.Rung(volume.CopiesSold));
            if (reprint > printed && PayPrintRun(series, volume, printed, reprint, "royalties, reprint") > 0)
                StudioMessage($"{series.Title} volume {volume.Number} goes back to press: {reprint:N0} copies in print.");
            foreach (var threshold in new[] { 100000L, 1000000L })
                if (old < threshold && volume.CopiesSold >= threshold) milestones.Add((volume, threshold));
        }
        var fanGain = (copies + units) * (volume.IsDoujin ? .3 : SalesRules.CommercialFanGain);
        series.Fanbase += fanGain;
        if (volume.IsDoujin) { DoujinCopiesThisMonth = checked(DoujinCopiesThisMonth + copies); DoujinFansThisMonth += fanGain; }
    }
    private void AwardMilestones(Series series, List<(Volume Volume, long Threshold)> milestones)
    {
        foreach (var (volume, threshold) in milestones)
        {
            ChangeTrackRecord(threshold == 100000 ? 3 : 10, volume.BusinessId);
            if (threshold == 100000) AddInfluence(series, .05);
            else if (!series.MillionCopyInfluenceAwarded) { AddInfluence(series, .15); series.MillionCopyInfluenceAwarded = true; }
            Emit(EventType.VolumeMilestone, $"{series.Title} volume {volume.Number} reached {threshold:N0} copies.", series.Id,
                context: new(VolumeId: volume.Id, Amount: threshold));
        }
    }
```

(Task 3 adds `PlanChannelDemand` and `ReleaseChannelUnits`; for this task add them as the Task 3 code below so the file compiles, or implement Task 3 in the same edit and run both tasks' tests.)

In `src/MangakaSim/GameState.Printing.cs` change the `SellStock` signature to `private long SellStock(Series s, Volume v, long requested, bool convention, bool daily = false)` and its posting line to:

```csharp
            if (daily) AccountPostDaily(BusinessOf(v.BusinessId).Account,revenue,"doujin sales",AccountEntryKind.Publishing,s.Id);
            else AccountPost(BusinessOf(v.BusinessId).Account,revenue,"doujin sales",AccountEntryKind.Publishing,s.Id);
```

In `src/MangakaSim/GameState.Operations.cs` add after `AccountPost`:

```csharp
    // Streaming sales (spec 2026-10-03): hourly income adds to today's line for the same title and reason.
    private void AccountPostDaily(CashAccount account, long amount, string reason, AccountEntryKind kind, int? seriesId)
    {
        if (amount <= 0) { if (amount < 0) AccountPost(account, amount, reason, kind, seriesId); return; }
        for (var i = account.Entries.Count - 1; i >= 0 && account.Entries[i].Time.Date == Clock.Now.Date; i--)
        {
            var entry = account.Entries[i];
            if (entry.Reason != reason || entry.SeriesId != seriesId || entry.Kind != kind || entry.TransferId is not null) continue;
            account.Balance = checked(account.Balance + amount);
            account.Entries[i] = entry with { Amount = checked(entry.Amount + amount) };
            return;
        }
        AccountPost(account, amount, reason, kind, seriesId);
    }
```

and in `AddBill`, before `Bills.Add`:

```csharp
        // Hourly sales accrue creator shares often; one unpaid bill per person and due date keeps the ledger short.
        if (reason == "creator share" && person is not null && Bills.LastOrDefault(b => b.BusinessId == business && b.PersonId == person &&
            b.Reason == reason && b.Remaining == b.Original && b.DueAt == new DateTime(Clock.Now.Year, Clock.Now.Month, 1).AddMonths(1)) is { } open)
        { open.Original = checked(open.Original + amount); open.Remaining = checked(open.Remaining + amount); return; }
```

In `src/MangakaSim/GameState.Career.cs` change `RecordSales` to take `bool weekly = false` and key weekly samples by week:

```csharp
    private void RecordSales(Series s,Volume v,long copies,bool channels=true,bool weekly=false)
    {
        var week=Monday(Clock.Now);
        var at=weekly?(week<Career.AvailableFrom?Career.AvailableFrom:week):Clock.Now;
        var receipts=World.Receipts.Where(r=>r.VolumeId==v.Id&&r.Week==week).ToArray();
        long Units(ReleaseChannel channel)=>channels?receipts.Where(r=>World.Channels.Any(a=>a.Id==r.AgreementId&&a.Channel==channel)).Sum(r=>r.Units):0;
        var existing=Career.Sales.FindIndex(x=>x.At==at&&x.Volume==v.Id);
        var sample=new SalesSample(at,v.BusinessId,s.Id,v.Id,copies,Units(ReleaseChannel.DomesticDigital),Units(ReleaseChannel.Overseas));
        if(existing<0)Career.Sales.Add(sample);
        else{var old=Career.Sales[existing];Career.Sales[existing]=sample with{Physical=old.Physical+copies,Digital=Math.Max(old.Digital,sample.Digital),Overseas=Math.Max(old.Overseas,sample.Overseas)};}
    }
```

(`Monday` is the private static helper in `GameState.Printing.cs`; it is visible across the partial class.)

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test tests/MangakaSim.Tests --filter "FullyQualifiedName~StreamingSalesTests"`
Expected: PASS (8 tests).

- [ ] **Step 5: Commit** (only if asked)

```bash
git add src/MangakaSim tests/MangakaSim.Tests/StreamingSalesTests.cs
git commit -m "Streaming sales: weekly plans released through shop hours"
```

---

### Task 3: Downloads, publisher deals and publisher volumes stream too

**Files:**
- Modify: `src/MangakaSim/GameState.Channels.cs` (replace `SettleChannelDemand`)
- Test: `tests/MangakaSim.Tests/StreamingSalesTests.cs`

**Interfaces:**
- Consumes: Task 2 `PlanVolumeWeek`, `ReleaseSalesHour`, `AccountPostDaily`.
- Produces: `private void PlanChannelDemand(Series s, Volume v, long domesticPotential, int hours)`, `private long ReleaseChannelUnits(Series s, Volume v, SalesPlan plan, long units)`.

- [ ] **Step 1: Write the failing tests**

```csharp
    [Fact] public void Download_receipts_fill_through_shop_hours()
    {
        var (s, book) = PrintedBook();
        s.Apply(new PublishDoujinOnlineCommand(book.Id));
        s.Advance(s.Clock.HoursUntil(s.Clock.Now.Date.AddDays(7)));
        var receipt = s.World.Receipts.Last(r => r.VolumeId == book.Id);
        var plan = book.SalesPlans!.Single(p => p.Kind == SaleKind.Download);
        Assert.Equal(0, receipt.Units);
        s.Advance(24 * 7);
        Assert.Equal(plan.Total, receipt.Units);
        Assert.Equal(s.DoujinDownloadsSold(book.Id), s.World.Receipts.Where(r => r.VolumeId == book.Id).Sum(r => r.Units));
    }

    [Fact] public void The_shop_window_closes_while_downloads_keep_selling()
    {
        var (s, book) = PrintedBook();
        s.Apply(new PublishDoujinOnlineCommand(book.Id));
        PublishingTests.Until(s, () => book.SalesClosed);
        Assert.Equal(book.SalesWindowWeeks, book.WeeksOnSale);
        Assert.DoesNotContain(book.SalesPlans!, p => p.Kind == SaleKind.Shop);
        var receipts = s.World.Receipts.Count(r => r.VolumeId == book.Id);
        s.Advance(24 * 14);
        Assert.True(s.World.Receipts.Count(r => r.VolumeId == book.Id) > receipts);
        Assert.True(book.SalesClosed);
    }

    [Fact] public void Publisher_volume_copies_and_milestones_arrive_through_the_week_once()
    {
        var s = SimulationFixture.EightPublished();
        var series = s.Series[0];
        s.Apply(new EndSeriesCommand(series.Id));
        var volume = series.Volumes.First(v => !v.IsDoujin);
        volume.CopiesSold = 99990; series.Fanbase = 3_000_000;
        var monday = s.Clock.Now.Date.AddDays(((int)DayOfWeek.Monday - (int)s.Clock.DayOfWeek + 7) % 7);
        if (monday <= s.Clock.Now) monday = monday.AddDays(7);
        s.Advance(s.Clock.HoursUntil(monday));
        var before = volume.CopiesSold;
        s.Advance(12);
        Assert.InRange(volume.CopiesSold, before + 1, long.MaxValue);
        s.Advance(24 * 7);
        Assert.Single(s.Events, e => e.Type == EventType.VolumeMilestone && e.VolumeId == volume.Id && e.Message.Contains("100,000"));
    }
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test tests/MangakaSim.Tests --filter "FullyQualifiedName~StreamingSalesTests"`
Expected: FAIL (receipts are settled whole at planning).

- [ ] **Step 3: Implement channel planning and release**

Replace `SettleChannelDemand` in `src/MangakaSim/GameState.Channels.cs` with:

```csharp
    // Streaming sales (spec 2026-10-03): a week's channel units become plans; the receipt fills as they are released.
    private void PlanChannelDemand(Series s,Volume v,long domesticPotential,int hours)
    {
        foreach(var channel in Enum.GetValues<ReleaseChannel>())
        {
            var a=ActiveChannel(s,v,channel);if(a is null)continue;
            if(v.SalesClosed&&!a.DirectDoujin)continue;
            var week=Monday(Clock.Now);
            if(World.Receipts.Any(r=>r.AgreementId==a.Id&&r.VolumeId==v.Id&&r.Week==week))continue;
            var units=a.DirectDoujin?DirectDownloadUnits(s,v,a):(long)Math.Floor(domesticPotential*(channel==ReleaseChannel.DomesticDigital?Math.Max(.05,DigitalPreference):.2*a.InternationalInterest/100));
            World.Receipts.Add(new(){AgreementId=a.Id,VolumeId=v.Id,Week=week,Units=0,NetYen=0});
            (v.SalesPlans??=new()).Add(new(){Kind=a.DirectDoujin?SaleKind.Download:SaleKind.Channel,AgreementId=a.Id,Week=week,Total=units,Hours=hours});
        }
    }
    private long ReleaseChannelUnits(Series s,Volume v,SalesPlan plan,long units)
    {
        var a=World.Channels.Single(x=>x.Id==plan.AgreementId);
        var income=a.DirectDoujin?checked(units*DoujinDownloadNet(v)):v.IsDoujin?(long)Math.Floor(units*v.Price*.7):SalesRules.Income(units,Economy.PriceIndex(TrendCatalog,v.ReleaseDate),false);
        if(income>0){AccountPostDaily(BusinessOf(v.BusinessId).Account,income,a.Channel==ReleaseChannel.DomesticDigital?"domestic digital receipts":"overseas licensed receipts",AccountEntryKind.Publishing,s.Id);VolumeContribution(v,income);}
        var receipt=World.Receipts.Single(r=>r.AgreementId==a.Id&&r.VolumeId==v.Id&&r.Week==plan.Week);
        receipt.Units=checked(receipt.Units+units);receipt.NetYen=checked(receipt.NetYen+income);
        if(a.Channel==ReleaseChannel.Overseas)a.InternationalInterest=Math.Clamp(a.InternationalInterest+units/1000d,0,100);
        return units;
    }
```

Add to the `"sales plan"` validation: Channel and Download plans reference an existing receipt:

```csharp
                Check((volume.SalesPlans ?? []).Where(p => p.AgreementId != 0).All(p => World.Receipts.Any(r => r.AgreementId == p.AgreementId && r.VolumeId == volume.Id && r.Week == p.Week)), "sales plan receipt");
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test tests/MangakaSim.Tests --filter "FullyQualifiedName~StreamingSalesTests"`
Expected: PASS (11 tests).

- [ ] **Step 5: Commit** (only if asked)

---

### Task 4: A book on sale midweek starts selling at once

**Files:**
- Modify: `src/MangakaSim/GameState.Sales.cs` (`ReleaseVolume`)
- Test: `tests/MangakaSim.Tests/StreamingSalesTests.cs`

**Interfaces:**
- Consumes: `PlanVolumeWeek`, `SalesRules.ShopHoursUntil`, `MinimumFirstWeekHours`.

- [ ] **Step 1: Write the failing tests**

```csharp
    private static GameState BookAwaitingDelivery(DateTime deliveredAt)
    {
        var s = PublishingTests.Started();
        PublishingTests.Until(s, () => s.Series[0].Volumes.Count == 1);
        s.Apply(new PauseSeriesCommand(s.Series[0].Id));
        s.Series[0].Fanbase = 2000;
        s.Advance(s.Clock.HoursUntil(deliveredAt.AddHours(-1)));
        s.Apply(new StudioActionCommand(StudioAction.Print, s.Series[0].Volumes[0].Id, Amount: 100));
        s.PrintRuns.Last().DueAt = deliveredAt;
        s.Advance(1);
        return s;
    }

    [Fact] public void A_midweek_release_sells_its_full_first_week_starting_at_once()
    {
        var thursday = NextWeekday(DayOfWeek.Thursday).AddHours(9);
        var s = BookAwaitingDelivery(thursday);
        var book = s.Series[0].Volumes[0];
        var plan = Assert.Single(book.SalesPlans!, p => p.Kind == SaleKind.Shop);
        Assert.Equal(1, book.WeeksOnSale);
        Assert.Equal(Math.Max(SalesRules.MinimumFirstWeekHours, SalesRules.ShopHoursUntil(thursday, thursday.Date.AddDays(4))), plan.Hours);
        s.Advance(3);
        Assert.True(book.CopiesSold > 0 || plan.Total < 3); // selling the same day
    }

    [Fact] public void A_release_late_on_sunday_still_spreads_over_thirty_shop_hours()
    {
        var sunday = NextWeekday(DayOfWeek.Sunday).AddHours(19);
        var s = BookAwaitingDelivery(sunday);
        var plan = Assert.Single(s.Series[0].Volumes[0].SalesPlans!, p => p.Kind == SaleKind.Shop);
        Assert.Equal(SalesRules.MinimumFirstWeekHours, plan.Hours);
    }

    [Fact] public void A_release_on_the_monday_planning_tick_is_planned_once()
    {
        var monday = NextWeekday(DayOfWeek.Monday);
        var s = BookAwaitingDelivery(monday);
        var book = s.Series[0].Volumes[0];
        Assert.Single(book.SalesPlans!, p => p.Kind == SaleKind.Shop);
        Assert.Equal(1, book.WeeksOnSale);
    }

    private static DateTime NextWeekday(DayOfWeek day)
    {
        var start = GameState.NewGame(0).Clock.Now.Date.AddDays(14);
        return start.AddDays(((int)day - (int)start.DayOfWeek + 7) % 7);
    }
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test tests/MangakaSim.Tests --filter "FullyQualifiedName~StreamingSalesTests"`
Expected: FAIL (no plan until Monday; `WeeksOnSale` is 0).

- [ ] **Step 3: Implement**

At the end of `ReleaseVolume` in `src/MangakaSim/GameState.Sales.cs` add:

```csharp
        // Streaming sales (spec 2026-10-03): a book on sale midweek starts selling now. Its first week (the same
        // first-week demand the next Monday would give) spreads over the shop hours left, at least 30.
        var hours = Math.Max(SalesRules.MinimumFirstWeekHours, SalesRules.ShopHoursUntil(Clock.Now, Monday(Clock.Now).AddDays(7)));
        PlanVolumeWeek(series, volume, series.Fanbase, series.IsIconic ? 1 : GenrePopularity(series.Genre), hours);
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test tests/MangakaSim.Tests --filter "FullyQualifiedName~StreamingSalesTests"`
Expected: PASS (14 tests).

- [ ] **Step 5: Commit** (only if asked)

---

### Task 5: Existing tests and playtests under streaming sales

**Files:**
- Modify: `tests/MangakaSim.Tests/SalesTests.cs`, `TimelineTests.cs`, `DoujinDistributionTests.cs`, `ProductionClarityTests.cs`, `PublishingTests.cs`, `StudioOperationsTests.cs`, `CareerTests.cs`, `AlphaTests.cs`, `GuidanceTests.cs` and any other test the full run shows failing
- Modify: `docs/superpowers/full-career-playtest-findings.md` only if a table is regenerated there (otherwise record shifts in the completion record)

**Interfaces:**
- Consumes: everything from Tasks 1 to 4.

- [ ] **Step 1: Run the full suite and record every failure**

Run: `dotnet test tests/MangakaSim.Tests --filter "Category!=Playtest" > .superpowers/sdd/2026-10-03-streaming-sales/t5-before.log 2>&1`
Expected: failures limited to tests that assert sales arrive at Monday 00:00 or in one `SalesStep()` call.

- [ ] **Step 2: Rewrite each Monday-lump assertion to the streaming equivalent**

For each failing test decide: intended change (rewrite, ledger a ruling) or a defect (fix the code with systematic debugging). The known rewrites:

- `SalesTests.Weekly_batch_is_idempotent_and_convention_precedes_current_month_sales`: keep the recap assertions; replace the idempotence check with a repeated `SalesStep()` at a shop hour:
```csharp
        state.Advance(state.Clock.HoursUntil(firstMonday.AddHours(11)));
        var before = state.ToJson();
        state.SalesStep();
        Assert.Equal(before, state.ToJson());
```
- `SalesTests.Multiple_volumes_use_one_fanbase_snapshot_and_milestones_fire_once`: after the planning `state.SalesStep()` at `monday`, release the week by stepping the clock through it, then assert the same totals:
```csharp
        state.SalesStep();
        for (var h = 1; h <= 24 * 7 - 1; h++) { state.Clock.Now = monday.AddHours(h); state.SalesStep(); }
        Assert.Equal(copies + 99990, first.CopiesSold);
        Assert.Equal(copies, second.CopiesSold);
```
  and the second-week check becomes `state.Clock.Now = monday.AddDays(7); state.SalesStep();` followed by stepping that week, still expecting 4 milestone events.
- `TimelineTests.Digital_doujin_uses_no_stock_and_settles_only_once_per_week`: after the planning `SalesStep()`, step the clock through the week before asserting `Receipts.Sum(r => r.Units) > 0`; the idempotence check moves to a shop hour as above.
- `DoujinDistributionTests`: `NextCheck` is now the next shop hour (Task 6); assert `SalesRules.IsShopHour(pending.NextCheck!.Value)` and advance a full week before asserting `CopiesSold > 0`.
- Others: replace "advance to the next Monday, then assert sales" with "advance to the end of the week (Sunday 20:00) or past it".

- [ ] **Step 3: Run the full suite**

Run: `dotnet test tests/MangakaSim.Tests --filter "Category!=Playtest"`
Expected: PASS.

- [ ] **Step 4: Run the playtest harness and compare**

Run: `dotnet test tests/MangakaSim.Tests --filter "Category=Playtest" > .superpowers/sdd/2026-10-03-streaming-sales/playtest.log 2>&1`
Expected: PASS. Compare the money and copies tables against the last recorded run (`docs/superpowers/progressive-disclosure-completion.md` lists the previous run's state; the harness prints tables) and record any shift (copies sold up to a week earlier, totals within a few per cent) in the completion record.

- [ ] **Step 5: Commit** (only if asked)

---

### Task 6: Helper-Chan's selling texts, the "sell more" step and reworded texts

**Files:**
- Modify: `src/MangakaSim/Guidance.cs`
- Modify: `src/MangakaSim/Goals.cs:52`
- Modify: `src/MangakaSim/DoujinOnline.cs:31`
- Modify: `src/MangakaSim/DoujinDistribution.cs`
- Test: `tests/MangakaSim.Tests/SellingTutorialTests.cs` (create)

**Interfaces:**
- Produces: guidance steps `sell` and `sell-online` with target `"sold"`; step `sell-more` with target `"sell-more"` (added to `CalmCareerSteps`); `DoujinStatus.NextCheck` now the next shop hour.

- [ ] **Step 1: Write the failing tests**

```csharp
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

    [Fact] public void After_the_first_sale_she_suggests_online_and_conventions_then_moves_on()
    {
        var (s, book) = StreamingSalesTests.PrintedBook();
        var prefs = new GuidancePreferences();
        PublishingTests.Until(s, () => book.CopiesSold > 0);
        var step = CareerGuidance.Evaluate(s, prefs);
        Assert.Equal("sell-more", step.Id);
        CareerGuidance.Observe(s, prefs); // writes the message to the thread
        s.Advance(24 * 4);
        Assert.NotEqual("sell-more", CareerGuidance.Evaluate(s, prefs).Id);
    }

    [Fact] public void Listing_online_ends_the_sell_more_suggestion()
    {
        var (s, book) = StreamingSalesTests.PrintedBook();
        var prefs = new GuidancePreferences();
        PublishingTests.Until(s, () => book.CopiesSold > 0);
        s.Apply(new PublishDoujinOnlineCommand(book.Id));
        Assert.NotEqual("sell-more", CareerGuidance.Evaluate(s, prefs).Id);
    }

    [Fact] public void No_selling_text_promises_a_Monday_settlement()
    {
        Assert.DoesNotContain(GoalCatalog.Goals, g => g.Tip.Contains("Monday"));
        var (s, book) = StreamingSalesTests.PrintedBook();
        var status = s.DescribeDoujin(book.Id);
        Assert.DoesNotContain("Monday", status.Local);
        Assert.Contains("10:00 to 20:00", status.Local);
        Assert.True(SalesRules.IsShopHour(status.NextCheck!.Value));
    }
}
```

(If `CareerGuidance.Observe` or `GoalCatalog.Goals`/`Tip` have different names in the code, use the existing method that appends the current step to `GuidancePreferences.Thread` and the goal definition's tip property; record the name in the ledger.)

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test tests/MangakaSim.Tests --filter "FullyQualifiedName~SellingTutorialTests"`
Expected: FAIL (texts still mention Mondays; no `sell-more` step).

- [ ] **Step 3: Implement the texts and the step**

In `src/MangakaSim/Guidance.cs`:

```csharp
        if(state.DoujinOnlineListed(book.Id))return new("sell-online","Find your online readers","Your download is on sale now!\nDownloads sell through the day, so watch Sold in the top bar. No printed stock is needed.","sold",project.Id);
```
```csharp
        return new("sell", "Find your first readers", "Your books are in local shops now, and they sell by themselves!\nShops are open 10:00 to 20:00, so watch Sold in the top bar.", "sold", project.Id);
```

Add `"sell-more"` to `CalmCareerSteps`. In `Career(...)`, immediately before `var project = active.FirstOrDefault(...)`, insert:

```csharp
        // The selling tutorial (spec 2026-10-03): after the first sale, show the two ways to sell more, for three days at most.
        var seller = owned.LastOrDefault(s => s.Volumes.Any(v => v.IsDoujin && v.CopiesSold > 0));
        if (seller is not null && !owned.Any(s => s.Chapters.Any(c => c.PublishedAt is not null)) &&
            !seller.Volumes.Any(v => state.DoujinOnlineListed(v.Id) || state.ConventionReserved(v.Id) > 0) &&
            !preferences.Thread.Any(m => m.Step == "sell-more" && m.Time <= state.Clock.Now.AddDays(-3)))
            return new("sell-more", "Sell more copies", $"{seller.Title} has its first readers! Want more?\n" +
                "List it online for free, or bring copies to a convention. Pick one below.", "sell-more", seller.Id);
```

`src/MangakaSim/Goals.cs:52` tip becomes: `"Print ten copies at the copy shop, or list the book online for free. Copies sell through shop hours once they arrive."`

`src/MangakaSim/DoujinOnline.cs:31` news ends: `"No upfront charge. Downloads sell through the day."`

In `src/MangakaSim/DoujinDistribution.cs` replace the `next` computation and texts:

```csharp
        var from=pending is not null&&stock==0&&pending.DueAt>Clock.Now?pending.DueAt:Clock.Now;
        var next=SalesRules.NextShopHour(from);
```
```csharp
            $"{(stock>0?"Local distribution is active":"Local distribution starts when copies arrive")}. Shops sell through the day, 10:00 to 20:00. Demand determines how many sell; a sale is not guaranteed.";
```
```csharp
        if(!book.SalesClosed)local+=$"\nSales weeks remaining: {Math.Max(0,book.SalesWindowWeeks-book.WeeksOnSale)}.";
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test tests/MangakaSim.Tests --filter "FullyQualifiedName~SellingTutorialTests|FullyQualifiedName~GuidanceTests|FullyQualifiedName~DoujinDistributionTests"`
Expected: PASS (update any guidance test that pinned the old "sell" text or target, with a ruling).

- [ ] **Step 5: Commit** (only if asked)

---

### Task 7: The Sold pulse, the first-sale bubble, the routes and header money

**Files:**
- Modify: `godot/DebugMain.MoneyFeedback.cs` (balance changes without a new line)
- Modify: `godot/DebugMain.WorkFeedback.cs` (shared bubble helper, Sold watcher)
- Modify: `godot/DebugMain.Alpha.cs` (routes `sold`, `online`, `sell-more` replies)
- Modify: `godot/DebugMain.DoujinOnline.cs:34`, `godot/DebugMain.Usability.cs:113`, `godot/DebugMain.Management.cs:319` (texts)
- Create: `godot/DebugMain.SellingSmoke.cs`; Modify: `godot/DebugMain.cs` (flag), `README.md` (smoke list)

**Interfaces:**
- Consumes: Task 6 step ids and targets; `GameState.SeriesCopiesSold`.
- Produces: `--selling-smoke`; `internal int SoldPulses`, `internal int FirstSaleBubbles`.

- [ ] **Step 1: Write the failing smoke** (`godot/DebugMain.SellingSmoke.cs`)

```csharp
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using MangakaSim;

namespace MangakaGame;

public partial class DebugMain
{
    // Streaming sales and the selling tutorial (spec 2026-10-03).
    private async void RunSellingSmoke()
    {
        SetProcess(false);
        try
        {
            GetWindow().Size = new(1920, 1080); Directory.CreateDirectory(SmokeOutput);
            _careers = new CareerStore(Path.Combine(SmokeOutput, "selling-" + Guid.NewGuid().ToString("N")));
            NewCareerMenu(); Press("Begin career"); await SettleUi(); _helperPopup.Hide();
            _state.Apply(new CreateDoujinCommand("Selling pages", "adventure"));
            for (var d = 0; d < 180 && _state.Series[0].Volumes.Count == 0; d++) _state.Advance(24);
            var book = _state.Series[0].Volumes[0]; _state.Series[0].Fanbase = 3000;
            ShowOffice(); RefreshManagement(); UpdateWorkFeedback(.1); await SettleUi();
            _state.Apply(new StudioActionCommand(StudioAction.Print, book.Id, Amount: 100));
            var pulses = SoldPulses;
            for (var h = 0; h < 48 && _state.Stock(book.Id) == 0; h++) { _state.Advance(1); RefreshManagement(); UpdateWorkFeedback(.1); }
            Check(SoldPulses == pulses + 1, "The Sold count pulses when the first copies reach the shops");
            var step = CareerGuidance.Evaluate(_state, _presentation.Guidance);
            Check(step.Id == "sell" && step.Text.Contains("sell by themselves"), $"Helper-Chan says the books sell by themselves ({step.Id})");
            ShowGuidance(); await SettleUi();
            Check(_page == "Office" && !_side.Visible, "Her Show me highlights Sold on the office view");
            var stock = _state.Stock(book.Id); var bubbles = FirstSaleBubbles; var money = _state.Money;
            for (var h = 0; h < 24 * 7 && book.CopiesSold == 0; h++) { _state.Advance(1); RefreshManagement(); UpdateWorkFeedback(.1); }
            Check(book.CopiesSold > 0 && _state.Stock(book.Id) < stock && _state.Money > money && SalesRules.IsShopHour(_state.Clock.Now),
                $"Copies sell during shop hours ({book.CopiesSold} sold at {_state.Clock.Now:HH:mm})");
            Check(FirstSaleBubbles == bubbles + 1, "The first copy sold gets its bubble");
            await CaptureSmokeImage("selling-first-sale");
            step = CareerGuidance.Evaluate(_state, _presentation.Guidance);
            Check(step.Id == "sell-more", $"Helper-Chan then suggests selling more ({step.Id})");
            OpenPhone(false); await SettleUi();
            Press("Sell online"); await SettleUi();
            Check(_page == "Sell online", "The Sell online reply opens Sell online");
            OpenPhone(false); await SettleUi();
            Press("Conventions"); await SettleUi();
            Check(_page == "Conventions", "The Conventions reply opens Conventions");
            _presentation.ReducedUiMotion = true; ShowOffice(); await SettleUi();
            PulseSold();
            Check(_currentCopies.Modulate.IsEqualApprox(new Color(BrandPalette.Gold)), "With reduced motion the Sold count is highlighted, not pulsed");
            _hadSale = false; ObserveSales(); await SettleUi();
            Check(_notice.Text.Contains("First copy sold!"), "With reduced motion the first sale is a notice line, not a bubble");
            _presentation.ReducedUiMotion = false;
            GD.Print($"SELLING SMOKE PASSED: {_smokeChecks} checks.");
            var tree = GetTree(); tree.CreateTimer(.1).Timeout += () => tree.Quit(); QueueFree();
        }
        catch (Exception ex) { GD.PushError($"SELLING SMOKE FAILED: {ex.Message}\n{ex.StackTrace}"); GetTree().Quit(1); }
    }
}
```

Register in `godot/DebugMain.cs` after the work feedback flag:

```csharp
        if (OS.GetCmdlineUserArgs().Contains("--selling-smoke")) CallDeferred(nameof(RunSellingSmoke));
```

- [ ] **Step 2: Build and run it to verify it fails**

Run: `dotnet build MangakaGame.sln -warnaserror` then `pwsh -NoProfile -Command "& '.superpowers/sdd/2026-09-29-brand-ui-restyle/smoke.ps1' -Flags @('--selling-smoke') -Timeout 600"`
Expected: build FAILS ("SoldPulses does not exist") until Step 3.

- [ ] **Step 3: Implement presentation**

In `godot/DebugMain.WorkFeedback.cs`, extract the bubble body of `ShowStageBubble` into `private void ShowBubble(Vector2 point, string text, Color colour, float below)` and call it from `ShowStageBubble`. Add the Sold watcher, called at the end of `UpdateWorkFeedback` (before the series and chapter checks, after the state reset):

```csharp
    internal int SoldPulses { get; private set; }
    internal int FirstSaleBubbles { get; private set; }
    private System.Collections.Generic.HashSet<int>? _onSale;
    private bool? _hadSale;

    // The selling tutorial (spec 2026-10-03): the Sold count pulses when a book's first copies go on sale, and the
    // career's first copy sold gets a bubble. Loading a career is the starting point, never a new event.
    private void ObserveSales()
    {
        var mine = _state.Series.Where(s => s.BusinessId == _state.ControlledBusinessId).SelectMany(s => s.Volumes)
            .Where(v => v.ReleasedAt is not null && (_state.Stock(v.Id) > 0 || _state.DoujinOnlineListed(v.Id) || !v.IsDoujin)).Select(v => v.Id).ToHashSet();
        var sale = _state.Series.Where(s => s.BusinessId == _state.ControlledBusinessId).Any(s => _state.SeriesCopiesSold(s.Id) > 0);
        if (_onSale is not null && mine.Except(_onSale).Any()) PulseSold();
        if (_hadSale == false && sale) FirstSale();
        _onSale = mine; _hadSale = sale;
    }
    private void PulseSold()
    {
        SoldPulses++;
        var label = _currentCopies; var gold = new Color(BrandPalette.Gold);
        if (_presentation.ReducedUiMotion)
        { label.Modulate = gold; GetTree().CreateTimer(3).Timeout += () => { if (IsInstanceValid(label)) label.Modulate = Colors.White; }; return; }
        var tween = label.CreateTween().SetLoops(3);
        tween.TweenProperty(label, "modulate", gold, .25); tween.TweenProperty(label, "modulate", Colors.White, .25);
    }
    private void FirstSale()
    {
        FirstSaleBubbles++;
        if (_presentation.ReducedUiMotion || !_currentCopies.IsVisibleInTree()) { Notify("Helper-Chan: First copy sold!"); return; }
        var rect = _currentCopies.GetGlobalRect();
        ShowBubble(new(rect.GetCenter().X, rect.End.Y), "First copy sold!", new(BrandPalette.Gold), 6);
    }
```

Reset `_onSale = null; _hadSale = null;` wherever `_workFeedback.Reset()` runs on a state change, and set `_hadSale` from the loaded state on the first observation (null means "first look": record without events).

In `godot/DebugMain.MoneyFeedback.cs` track the balance so a merged line still flashes:

```csharp
        private long _balance;
        // In Reset: _balance=0 is replaced by the account balance in Observe's first branch.
        public void Observe(CashAccount account)
        {
            if(!ReferenceEquals(_account,account)||account.Entries.Count<_entries)
            {Reset();_account=account;_entries=account.Entries.Count;_balance=account.Balance;return;}
            long added=0;
            for(;_entries<account.Entries.Count;_entries++)
            {
                var entry=account.Entries[_entries];added+=entry.Amount;
                if(entry.Amount>0){Gains+=entry.Amount;_gainAge=0;GainReason=entry.Reason;}
                else if(entry.Amount<0){Losses-=(decimal)entry.Amount;_lossAge=0;LossReason=entry.Reason;}
            }
            // Streaming sales add to today's line instead of adding one (spec 2026-10-03).
            var merged=account.Balance-_balance-added;
            if(merged>0){Gains+=merged;_gainAge=0;GainReason="sales";}
            else if(merged<0){Losses-=merged;_lossAge=0;LossReason="adjustment";}
            _balance=account.Balance;
        }
```

In `godot/DebugMain.Alpha.cs` `RouteGuidance` add cases:

```csharp
            case "sold":ShowOffice();focus=_currentCopies;
                hint="Sold counts up here as the shops sell your copies, from 10:00 to 20:00.";break;
            case "online":if(_state.FindSeries(project)?.Volumes.LastOrDefault(v=>v.IsDoujin) is {} listing)OpenOnline(project,listing.Id);else Navigate("Books");
                hint="List the book online here. It costs nothing up front.";break;
```

In the phone reply builder replace the single "Show me" with:

```csharp
        var current=CareerGuidance.Evaluate(_state,_presentation.Guidance);
        if(current.Id=="sell-more")
        {
            Reply(replies,"Sell online",()=>{ClosePhone();HighlightGuidance(RouteGuidance("online",current.Project).Focus);}).Name="GuidanceSellOnline";
            Reply(replies,"Conventions",()=>{ClosePhone();HighlightGuidance(RouteGuidance("conventions",current.Project).Focus);}).Name="GuidanceConventions";
            _showGuidance=Reply(replies,"Show me",ShowGuidance);_showGuidance.Visible=false;
        }
        else{_showGuidance=Reply(replies,"Show me",ShowGuidance);_showGuidance.Name="GuidanceShowMe";}
```

`HighlightGuidance` must accept a `Label` (`_currentCopies`): set `FocusMode=All` on `_currentCopies` when it is built so `GrabFocus` works.

Texts:
- `godot/DebugMain.DoujinOnline.cs:34`: `"Downloads sell through the day as buyers find them. Quality and current genre popularity determine demand; older editions retain a small backlist audience. No purchase means no income. This route does not require a studio internet upgrade."`
- `godot/DebugMain.Usability.cs:113`: `"Physical copies need printing and delivery. Downloads need no stock or upfront payment. Copies sell through the day, 10:00 to 20:00."`
- `godot/DebugMain.Management.cs:319`: `"Choose a finished book to print. Delivery puts copies in local shops, where they sell through the day."`

README: add `--selling-smoke` (streaming sales and the selling tutorial) to the smoke list.

- [ ] **Step 4: Build and run the smoke**

Run: `dotnet build MangakaGame.sln -warnaserror` then the smoke command from Step 2.
Expected: `SELLING SMOKE PASSED`, 0 warnings. Then the windowed capture run (`--selling-smoke --capture`, `-Windowed`) and look at `TestResults/selling-first-sale.avif`.

- [ ] **Step 5: Commit** (only if asked)

---

### Task 8: Full verification and records

**Files:**
- Create: `docs/superpowers/streaming-sales-completion.md` (CRLF)
- Modify: `CLAUDE.md` section 5, `docs/superpowers/session-handoff.md`, `.superpowers/sdd/work-feedback/suite.ps1` (add `--selling-smoke`)

- [ ] **Step 1: Run everything**

Run: `pwsh -NoProfile -File .superpowers/sdd/work-feedback/suite.ps1` (with `--selling-smoke` added to its flag list)
Expected: all unit tests pass; all 23 Godot checks pass; display sweep 0 flagged; `--journey-smoke` passes end to end.

- [ ] **Step 2: Rendered review**

Look at `selling-first-sale` and a mid-week office capture at 1920 x 1080 and 1280 x 720 with 150% text: Sold readable, bubble not clipped.

- [ ] **Step 3: Write the completion record and update CLAUDE.md**

Record: what changed, verification (exact counts), playtest shifts, rulings, deferred minors, what was not verified (how the trickle feels to a new player).

- [ ] **Step 4: Final review** (executing-plans final review on the most capable model), one fix pass, then report to the user.
