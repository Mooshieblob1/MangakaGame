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

    // A printed doujin with stock, paused at the Monday planning tick. The run is delivered on that tick: delivered
    // earlier, the book would start selling at once (Task 4) and the 100 copies would be gone by Monday.
    internal static (GameState State, Volume Book) PrintedBook()
    {
        var s = PublishingTests.Started();
        PublishingTests.Until(s, () => s.Series[0].Volumes.Count == 1);
        s.Apply(new PauseSeriesCommand(s.Series[0].Id));
        s.Apply(new StudioActionCommand(StudioAction.Print, s.Series[0].Volumes[0].Id, Amount: 100));
        s.Series[0].Fanbase = 2000; // enough demand to see several copies a week
        var monday = s.Clock.Now.Date.AddDays(((int)DayOfWeek.Monday - (int)s.Clock.DayOfWeek + 7) % 7);
        if (monday <= s.Clock.Now) monday = monday.AddDays(7);
        s.PrintRuns.Last().DueAt = monday;
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
        var (s, book) = PrintedBook();
        Assert.Single(book.SalesPlans!, p => p.Kind == SaleKind.Shop).Total = 70; // about a copy every shop hour
        var account = s.ControlledBusiness.Account;
        var day = s.Clock.Now.Date;
        long streamed = 0; var hoursWithSales = 0;
        for (var h = 0; h < 24; h++)
        {
            var copies = book.CopiesSold;
            s.Advance(1);
            if (book.CopiesSold == copies) continue;
            hoursWithSales++;
            streamed += (long)Math.Floor((book.CopiesSold - copies) * book.Price * .7);
        }
        Assert.True(hoursWithSales >= 2, $"sales in {hoursWithSales} shop hours");
        Assert.True(streamed > 0);
        var line = Assert.Single(account.Entries, e => e.Reason == "doujin sales" && e.SeriesId == s.Series[0].Id && e.Time.Date == day);
        Assert.Equal(streamed, line.Amount);
        Assert.Equal(s.ToJson(), GameState.FromJson(s.ToJson()).ToJson());
    }

    [Fact] public void Copy_shop_penalty_carries_across_hours_so_the_week_adds_up()
    {
        var (s, book) = PrintedBook();
        var plan = Assert.Single(book.SalesPlans!, p => p.Kind == SaleKind.Shop);
        plan.Total = 30; // far fewer copies than shop hours: most hours are worth less than one copy
        var established = Math.Clamp(Math.Max(s.Series[0].ChaptersPublished / 500d, s.Series[0].Fanbase / 500000), 0, 1);
        var factor = 1 - (1 - .25) * (.02 + .58 * established * established);
        var expected = (long)Math.Floor(plan.Total * factor);
        var sold = book.CopiesSold;
        s.Advance(24 * 7 - 4); // to Sunday 20:00
        Assert.Equal(plan.Total, plan.Released);
        Assert.True(Math.Abs(book.CopiesSold - sold - expected) <= 1, $"sold {book.CopiesSold - sold}, one weekly call would sell {expected}");
        Assert.True(book.CopiesSold - sold > 0);
        Assert.InRange(plan.Carry, 0, .999999);
    }

    [Fact] public void Creator_share_bills_merge_per_person_and_month_until_part_paid()
    {
        var s = GameState.NewGame(7);
        var add = typeof(GameState).GetMethod("AddBill", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
        var (business, person) = (s.ControlledBusinessId, s.ProtagonistPersonId);
        void Add(long amount) => add.Invoke(s, new object?[] { business, amount, "creator share", person, null });
        IEnumerable<Bill> Shares() => s.Bills.Where(b => b.PersonId == person && b.Reason == "creator share");
        Add(1000); Add(500);
        var bill = Assert.Single(Shares());
        Assert.Equal((1500L, 1500L), (bill.Original, bill.Remaining));
        bill.Remaining -= 200; // partly paid: a new accrual must not rewrite what was already settled
        Add(300);
        Assert.Equal(2, Shares().Count());
        Assert.Equal((1500L, 1300L), (bill.Original, bill.Remaining));
        Assert.Equal(300, Shares().Last().Original);
    }

    [Fact] public void Copies_due_with_no_stock_are_missed_and_a_midweek_reprint_sells_again()
    {
        var (s, book) = PrintedBook();
        var plan = Assert.Single(book.SalesPlans!, p => p.Kind == SaleKind.Shop);
        var before = book.CopiesSold;
        foreach (var run in s.PrintRuns.Where(r => r.VolumeId == book.Id)) run.Remaining = 0;
        s.Advance(24 * 2);
        var during = book.CopiesSold;
        Assert.Equal(before, during); // nothing sells while out of stock
        Assert.True(plan.Released > 0); // though copies fell due
        var dueBefore = plan.Released;
        s.Apply(new StudioActionCommand(StudioAction.Print, book.Id, Amount: 50));
        s.Advance(24 * 4);
        Assert.True(book.CopiesSold > during);
        Assert.True(book.CopiesSold - during <= plan.Total - dueBefore, "no catch-up: only copies due after the reprint sell");
        Assert.True(book.WeeklyDemand >= 0);
    }

    [Fact] public void An_older_save_loaded_midweek_is_not_paid_again()
    {
        var (s, book) = PrintedBook();
        book.SalesPlans!.Single().Total = 60; // a slow week, so the 100 printed copies last into the next one
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

    [Fact] public void Download_receipts_fill_through_shop_hours()
    {
        var (s, book) = PrintedBook();
        s.Apply(new PublishDoujinOnlineCommand(book.Id));
        s.Advance(s.Clock.HoursUntil(s.Clock.Now.Date.AddDays(7))); // the next Monday 00:00 plans the downloads
        var receipt = s.World.Receipts.Last(r => r.VolumeId == book.Id);
        var plan = book.SalesPlans!.Single(p => p.Kind == SaleKind.Download);
        Assert.True(plan.Total > 0);
        Assert.Equal(0, receipt.Units); // planned, nothing released yet
        s.Advance(10);
        Assert.Equal(0, receipt.Units); // shops are closed until 10:00 too
        s.Advance(24 * 7 - 10); // to the next Monday 00:00: every shop hour of the week has run
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

    [Fact] public void A_download_plan_without_its_receipt_is_rejected_on_load()
    {
        var (s, book) = PrintedBook();
        s.Apply(new PublishDoujinOnlineCommand(book.Id));
        s.Advance(24 * 7); // the next Monday plans the downloads
        Assert.Single(book.SalesPlans!, p => p.Kind == SaleKind.Download);
        Assert.Equal(s.ToJson(), GameState.FromJson(s.ToJson()).ToJson());
        s.World.Receipts.RemoveAll(r => r.VolumeId == book.Id);
        Assert.Throws<InvalidDataException>(() => GameState.FromJson(s.ToJson()));
    }

    [Fact] public void A_channel_receipt_week_off_midnight_is_rejected_on_load()
    {
        var (s, book) = PrintedBook();
        s.Apply(new PublishDoujinOnlineCommand(book.Id));
        s.Advance(24 * 14);
        var first = s.World.Receipts.Where(r => r.VolumeId == book.Id).MinBy(r => r.Week)!;
        Assert.DoesNotContain(book.SalesPlans ?? new(), p => p.AgreementId == first.AgreementId && p.Week == first.Week); // its plan has finished
        Assert.Equal(s.ToJson(), GameState.FromJson(s.ToJson()).ToJson());
        first.Week = first.Week.AddHours(1); // still a Monday, but not the week's 00:00 key
        Assert.Throws<InvalidDataException>(() => GameState.FromJson(s.ToJson()));
    }

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

    // Final review fix 1: a Sunday 19:00 delivery plans 30 shop hours, so 28 of them spill into the next week. With and
    // without that spillover, paused at the next Monday's planning tick, with stock to spare and a Saturday convention.
    private static (GameState State, Volume Book, ConventionBooking Booking) SpilloverWeek(bool keepSpillover)
    {
        var sunday = NextWeekday(DayOfWeek.Sunday).AddHours(19);
        var s = BookAwaitingDelivery(sunday);
        var book = s.Series[0].Volumes[0];
        s.PrintRuns.Single(r => r.VolumeId == book.Id).Remaining = 1000;
        s.Advance(s.Clock.HoursUntil(sunday.Date.AddDays(1)));
        if (!keepSpillover) book.SalesPlans!.RemoveAll(p => p.Week < s.Clock.Now);
        var booking = new ConventionBooking { Id = 99998, BusinessId = s.ControlledBusinessId, PersonId = s.ProtagonistPersonId, Date = s.Clock.Now.Date.AddDays(5), Scale = 1, StaffedHours = 100 };
        s.Bookings.Add(booking);
        return (s, book, booking);
    }

    [Fact] public void A_first_week_spilling_past_monday_sells_without_draining_the_next_weeks_demand()
    {
        var (s, book, booking) = SpilloverWeek(true);
        var (control, controlBook, controlBooking) = SpilloverWeek(false);
        var spill = Assert.Single(book.SalesPlans!, p => p.Week < s.Clock.Now);
        Assert.Equal(28, spill.Hours - spill.HoursDone); // the 19:00 and 20:00 hours ran on Sunday
        var pool = book.WeeklyDemand;
        Assert.Equal(pool, controlBook.WeeklyDemand);
        var friday = s.Clock.Now.AddDays(4).AddHours(23); // Friday 23:00, the spillover done, the convention still ahead
        s.Advance(s.Clock.HoursUntil(friday)); control.Advance(control.Clock.HoursUntil(friday));
        Assert.Equal(spill.Total, spill.Released);
        Assert.True(book.CopiesSold > controlBook.CopiesSold, "the spillover hours still sell"); // (a)
        Assert.Equal(controlBook.WeeklyDemand, book.WeeklyDemand); // (b) only the week's own shop sales draw it down
        Assert.True(book.WeeklyDemand > pool / 2, $"{book.WeeklyDemand} of {pool} left for conventions");
        s.Advance(24); control.Advance(24); // the Saturday convention settles
        Assert.True(booking.Settled && controlBooking.Settled);
        Assert.True(booking.CopiesSold > 0, $"{booking.CopiesSold} sold, cancelled {booking.Cancelled}");
        Assert.Equal(controlBooking.CopiesSold, booking.CopiesSold); // (c) the convention takes its usual share
    }

    [Fact] public void A_release_on_the_monday_planning_tick_is_planned_once()
    {
        var monday = NextWeekday(DayOfWeek.Monday);
        var s = BookAwaitingDelivery(monday);
        var book = s.Series[0].Volumes[0];
        Assert.Single(book.SalesPlans!, p => p.Kind == SaleKind.Shop);
        Assert.Equal(1, book.WeeksOnSale);
    }

    [Fact] public void Listing_an_already_selling_book_online_midweek_starts_its_downloads_at_once()
    {
        var (s, book) = PrintedBook();
        s.Advance(24 * 2 + 12); // Wednesday 12:00, the book already on sale since Monday
        var nextMonday = s.Clock.Now.Date.AddDays(5);
        var weeks = book.WeeksOnSale;
        s.Apply(new PublishDoujinOnlineCommand(book.Id));
        var plan = Assert.Single(book.SalesPlans!, p => p.Kind == SaleKind.Download);
        Assert.Equal(Math.Max(SalesRules.MinimumFirstWeekHours, SalesRules.ShopHoursUntil(s.Clock.Now, nextMonday)), plan.Hours);
        Assert.True(plan.Total > 0);
        Assert.Equal(weeks, book.WeeksOnSale); // listing is not another week on sale
        Assert.Single(book.SalesPlans!, p => p.Kind == SaleKind.Shop); // and adds no second shop plan
        var receipt = Assert.Single(s.World.Receipts, r => r.VolumeId == book.Id);
        Assert.Equal(0, receipt.Units);
        s.Advance(s.Clock.HoursUntil(nextMonday) - 1); // to Sunday 23:00: every shop hour has run
        Assert.Equal(plan.Total, receipt.Units);
        Assert.DoesNotContain(book.SalesPlans!, p => p.Kind == SaleKind.Download);
        s.Advance(1); // Monday plans the following week once
        Assert.Equal(2, s.World.Receipts.Count(r => r.VolumeId == book.Id));
        var next = Assert.Single(book.SalesPlans!, p => p.Kind == SaleKind.Download);
        Assert.Equal(SalesRules.ShopHoursPerWeek, next.Hours);
    }

    [Fact] public void The_monday_after_a_midweek_listing_plans_the_second_download_week()
    {
        var (s, book) = PrintedBook();
        s.Advance(24 * 2 + 12); // Wednesday 12:00
        var nextMonday = s.Clock.Now.Date.AddDays(5);
        s.Apply(new PublishDoujinOnlineCommand(book.Id));
        var first = Assert.Single(book.SalesPlans!, p => p.Kind == SaleKind.Download).Total;
        s.Advance(s.Clock.HoursUntil(nextMonday));
        var second = Assert.Single(book.SalesPlans!, p => p.Kind == SaleKind.Download).Total;
        // The listing planned download week 1 at once, so this Monday plans week 2 (40% demand), not week 1 again.
        Assert.InRange(second, 1, (long)Math.Ceiling(first * .45));
    }

    private static DateTime NextWeekday(DayOfWeek day)
    {
        var start = GameState.NewGame(0).Clock.Now.Date.AddDays(14);
        return start.AddDays(((int)day - (int)start.DayOfWeek + 7) % 7);
    }
}
