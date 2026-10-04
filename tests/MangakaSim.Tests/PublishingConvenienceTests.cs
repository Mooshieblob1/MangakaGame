using System.Text.Json.Nodes;
using Xunit;

namespace MangakaSim.Tests;

public class PublishingConvenienceTests
{
    private static GameState Book()
    {
        var s=GameState.NewGame(2);s.Apply(new CreateDoujinCommand("Paper Garden","drama",8));
        PublishingTests.Until(s,()=>s.Series[0].Volumes.Count>0);return s;
    }
    private static void Replay(GameState s)
    {Assert.Equal(s.ToJson(),GameState.FromJson(s.ToJson()).ToJson());Assert.Equal(s.ToJson(),s.ReplayTimeline().ToJson());}
    [Fact]public void Continue_keeps_identity_readers_stock_money_and_completed_book_then_produces_issue_two()
    {
        var s=Book();var title=s.Series[0];var book=title.Volumes[0];
        s.Apply(new StudioActionCommand(StudioAction.Print,book.Id,Amount:100));s.Advance(7*24);
        var readers=title.Fanbase;var money=s.Money;var stock=s.Stock(book.Id);var sold=book.CopiesSold;var history=s.Career.Sales.Count;
        s.Apply(new ContinueOneShotCommand(title.Id));
        Assert.Same(title,s.Series[0]);Assert.Equal("Paper Garden",title.Title);Assert.Equal("drama",title.Genre);
        Assert.Equal(readers,title.Fanbase);Assert.Equal(money,s.Money);Assert.Equal(stock,s.Stock(book.Id));Assert.Equal(sold,book.CopiesSold);
        Assert.Equal(history,s.Career.Sales.Count);Assert.Same(book,title.Volumes[0]);Assert.False(title.StandaloneDoujin);Assert.True(title.ReleaseShortIssues);
        PublishingTests.Until(s,()=>title.Volumes.Count>1);
        Assert.Equal(VolumeFormat.DoujinIssue,title.Volumes[1].Format);Assert.Equal(2,title.Volumes[1].FirstChapter);Replay(s);
    }
    [Fact]public void Unfinished_conversion_keeps_progress_and_repeat_conversion_is_atomic()
    {
        var s=GameState.NewGame(2);s.Apply(new CreateDoujinCommand("In progress","drama",8));s.Advance(3);
        var title=s.Series[0];var chapter=title.Chapters[0];var hours=chapter.Stages.Sum(w=>w.HoursDone);
        s.Apply(new ContinueOneShotCommand(title.Id));Assert.Equal(hours,chapter.Stages.Sum(w=>w.HoursDone));Assert.Same(chapter,title.Chapters[0]);
        var before=s.ToJson();Assert.Throws<InvalidCommandException>(()=>s.Apply(new ContinueOneShotCommand(title.Id)));Assert.Equal(before,s.ToJson());Replay(s);
    }
    [Fact]public void Direct_shop_is_free_without_internet_and_pays_only_actual_downloads_without_printing()
    {
        var s=Book();var title=s.Series[0];var book=title.Volumes[0];var cash=s.Money;
        Assert.False(s.ControlledBusiness.HasInternet);s.Apply(new PublishDoujinOnlineCommand(book.Id));
        // Streaming sales (2026-10-03): listing plans the first download week at once; its receipt fills through shop hours.
        var listed=Assert.Single(s.World.Receipts);Assert.Equal((0L,0L),(listed.Units,listed.NetYen));
        Assert.Equal(cash,s.Money);Assert.Empty(s.PrintRuns);Assert.True(s.DoujinOnlineListed(book.Id));
        Assert.True(GameState.DoujinDownloadPrice(book)<book.Price);Assert.Equal(0,s.World.Channels.Single().Cost);
        var planned=Assert.Single(book.SalesPlans!,p=>p.Kind==SaleKind.Download).Total;
        s.Advance(7*24);Assert.Equal(planned,listed.Units);Assert.True(listed.Units>0);
        Assert.All(s.World.Receipts,r=>Assert.Equal(r.Units*GameState.DoujinDownloadNet(book),r.NetYen));
        var units=s.World.Receipts.Sum(r=>r.Units);var net=s.World.Receipts.Sum(r=>r.NetYen);
        Assert.Equal(units,s.SeriesCopiesSold(title.Id));Assert.Equal(units,s.DoujinDownloadsSold(book.Id));
        Assert.Equal(0,book.CopiesSold);Assert.Equal(0,s.Stock(book.Id));Assert.True(title.Fanbase>0);
        Assert.Equal(net,s.ControlledBusiness.Account.Entries.Where(e=>e.Reason=="domestic digital receipts").Sum(e=>e.Amount));
        var before=s.ToJson();Assert.Throws<InvalidCommandException>(()=>s.Apply(new PublishDoujinOnlineCommand(book.Id)));Assert.Equal(before,s.ToJson());Replay(s);
    }
    [Fact]public void Backlist_downloads_continue_after_local_window_and_each_new_issue_needs_listing()
    {
        var s=Book();var title=s.Series[0];var book=title.Volumes[0];s.Apply(new PublishDoujinOnlineCommand(book.Id));s.Advance(24*70);
        Assert.True(book.SalesClosed);var downloads=s.DoujinDownloadsSold(book.Id);s.Advance(24*7);Assert.True(s.DoujinDownloadsSold(book.Id)>downloads);
        s.Apply(new ContinueOneShotCommand(title.Id));PublishingTests.Until(s,()=>title.Volumes.Count>1);
        Assert.False(s.DoujinOnlineListed(title.Volumes[1].Id));Replay(s);
    }
    [Fact]public void Zero_demand_has_no_payment()
    {
        // Streaming sales (2026-10-03): listing plans the first week at once, so the demand must be zero before listing.
        var s=Book();var book=s.Series[0].Volumes[0];book.AverageQuality=0;s.Apply(new PublishDoujinOnlineCommand(book.Id));
        s.Advance(24*7);Assert.NotEmpty(s.World.Receipts);
        Assert.All(s.World.Receipts,r=>{Assert.Equal(0,r.Units);Assert.Equal(0,r.NetYen);});
        Assert.DoesNotContain(s.ControlledBusiness.Account.Entries,e=>e.Reason=="domestic digital receipts");
    }
    [Fact]public void More_popular_genres_sell_more_downloads_for_the_same_edition()
    {
        var lower=Book();var book=lower.Series[0].Volumes[0];lower.Apply(new PublishDoujinOnlineCommand(book.Id));
        var higher=GameState.FromJson(lower.ToJson());
        lower.Trends.Single(t=>t.Genre=="drama").Noise=-.15;
        higher.Trends.Single(t=>t.Genre=="drama").Noise=.15;
        lower.Advance(24*7);higher.Advance(24*7);
        Assert.True(higher.DoujinDownloadsSold(book.Id)>lower.DoujinDownloadsSold(book.Id));
    }
    [Fact]public void Two_events_cannot_reserve_the_same_copies_and_late_delivery_is_ineligible()
    {
        var s=Book();var title=s.Series[0];var book=title.Volumes[0];
        while(s.Clock.DayOfWeek!=DayOfWeek.Sunday)s.Advance(24);
        s.Apply(new StudioActionCommand(StudioAction.Print,book.Id,Amount:200,Value:(int)PrintTier.LocalPrinter));
        var before=s.ToJson();
        Assert.Throws<InvalidCommandException>(()=>s.Apply(new StudioActionCommand(StudioAction.BookConvention,s.ProtagonistPersonId,Amount:title.Id,ReservedCopies:50)));
        Assert.Equal(before,s.ToJson()); // Wednesday event is before Thursday delivery.
        s.Advance(7*24); // Sunday again, after Thursday's delivery.
        // Streaming sales (2026-10-03): shops sold some delivered copies since Thursday, so claim all but 10 of what is left.
        var claim=s.Stock(book.Id)-10;Assert.True(claim>=30,$"Stock {s.Stock(book.Id)}");
        s.Apply(new StudioActionCommand(StudioAction.BookConvention,s.ProtagonistPersonId,Amount:title.Id,ReservedCopies:claim));
        // Wednesday local and the following Sunday regional are different events; only 10 copies stay unclaimed.
        Assert.NotEqual(s.NextConvention(0),s.NextConvention(1));Assert.Equal(10,s.ConventionReservable(title.Id,s.NextConvention(1)));
        before=s.ToJson();Assert.Throws<InvalidCommandException>(()=>s.Apply(new StudioActionCommand(StudioAction.BookConvention,s.ProtagonistPersonId,Amount:title.Id,Value:1,ReservedCopies:20)));
        Assert.Equal(before,s.ToJson());
        Assert.Equal(claim,s.ConventionReserved(book.Id));
    }
    [Fact]public void Reserved_print_orders_survive_local_sales_and_online_sales_then_release_after_event()
    {
        var s=Book();var title=s.Series[0];var book=title.Volumes[0];
        // Regional Sunday leaves a Monday between booking and the event.
        while(s.Clock.DayOfWeek!=DayOfWeek.Sunday)s.Advance(24);
        s.Apply(new StudioActionCommand(StudioAction.Print,book.Id,Amount:100));s.Apply(new PublishDoujinOnlineCommand(book.Id));
        s.Apply(new StudioActionCommand(StudioAction.BookConvention,s.ProtagonistPersonId,Amount:title.Id,Value:1,ReservedCopies:100));
        var booking=Assert.Single(s.Bookings);Assert.Equal(100,s.ConventionReserved(book.Id));Replay(s);
        s.Advance(s.Clock.HoursUntil(booking.Date.AddHours(10)));
        Assert.Equal(100,s.Stock(book.Id));Assert.Equal(0,book.CopiesSold);Assert.True(s.DoujinDownloadsSold(book.Id)>0);
        s.Advance(s.Clock.HoursUntil(booking.Date.AddHours(16+booking.TravelHours)));
        // Streaming sales (2026-10-03): settlement runs before the shop hour of the same tick, so released leftovers may sell at once (at most that hour's share).
        Assert.True(booking.Settled);Assert.True(booking.CopiesSold>0);Assert.InRange(book.CopiesSold-booking.CopiesSold,0,1);
        Assert.Equal(100-book.CopiesSold,s.Stock(book.Id));Assert.Equal(0,s.ConventionReserved(book.Id));Replay(s);
    }
    [Fact]public void Reservation_updates_cancellation_and_overbooking_are_atomic()
    {
        var s=Book();var title=s.Series[0];var book=title.Volumes[0];s.Apply(new StudioActionCommand(StudioAction.Print,book.Id,Amount:50));
        s.Apply(new StudioActionCommand(StudioAction.BookConvention,s.ProtagonistPersonId,Amount:title.Id,ReservedCopies:40));var b=s.Bookings.Single();
        Assert.Equal(10,s.ConventionReservable(title.Id,b.Date));s.Apply(new ReserveConventionStockCommand(b.Id,25));Assert.Equal(25,s.ConventionReserved(book.Id));
        foreach(var invalid in new[]{-1,51}){var before=s.ToJson();Assert.Throws<InvalidCommandException>(()=>s.Apply(new ReserveConventionStockCommand(b.Id,invalid)));Assert.Equal(before,s.ToJson());}
        s.Apply(new StudioActionCommand(StudioAction.CancelConvention,b.Id));Assert.Equal(0,s.ConventionReserved(book.Id));Assert.Equal(50,s.ConventionReservable(title.Id,b.Date));Replay(s);
    }
    [Fact]public void Nearby_events_are_soon_and_major_calendar_is_retained()
    {
        var s=GameState.NewGame();for(var day=0;day<7;day++)
        {Assert.InRange((s.NextConvention(0)-s.Clock.Now.Date).Days,2,5);Assert.InRange((s.NextConvention(1)-s.Clock.Now.Date).Days,2,8);s.Advance(24);}
        Assert.Equal(new DateTime(1996,8,3),s.NextConvention(2));
    }
    [Fact]public void Version9_import_preserves_existing_booking_dates_and_replays_new_actions()
    {
        var s=Book();s.Apply(new StudioActionCommand(StudioAction.BookConvention,s.ProtagonistPersonId,Amount:s.Series[0].Id));
        s.Bookings[0].Date=s.Bookings[0].Date.AddDays(21);var date=s.Bookings[0].Date;
        var json=JsonNode.Parse(s.ToJson())!;json["Version"]=9;var restored=GameState.ImportSupported(json.ToJsonString());
        Assert.Equal(date,restored.Bookings[0].Date);Assert.Equal(s.Money,restored.Money);
        restored.Apply(new PublishDoujinOnlineCommand(restored.Series[0].Volumes[0].Id));restored.Advance(24*7);Replay(restored);
    }
}
