using MangakaSim;
using Xunit;
using static MangakaSim.Tests.IssueCloseTests;

namespace MangakaSim.Tests;

public class SalesTests
{
    private const string Jump = "tokiwa-jump";

    private static void AdvanceTo(GameState state, DateTime when) => state.Advance(state.Clock.HoursUntil(when));

    private static DateTime NextMondayMidnight(DateTime from)
    {
        var day = from.Date.AddDays(1);
        while (day.DayOfWeek != DayOfWeek.Monday) day = day.AddDays(1);
        return day;
    }

    /// <summary>Nine chapters published at Jump: a tankobon is scheduled six weeks after the ninth close.</summary>
    private static GameState WithScheduledTankobon()
    {
        var state = SerializedAtJump();
        var series = state.Series[0];
        for (var i = 0; i < 9; i++)
        {
            ForceComplete(state, series.OpenChapter!);
            AdvanceTo(state, state.MarketOf(Jump).NextIssueClose);
        }
        state.Apply(new PauseSeriesCommand(series.Id)); // hiatus keeps the fanbase quiet apart from misses
        return state;
    }

    [Fact]
    public void A_scheduled_tankobon_is_released_once_and_sells_on_mondays()
    {
        var state = WithScheduledTankobon();
        var series = state.Series[0];
        var volume = series.Volumes[0];
        Assert.False(volume.IsReleased);

        AdvanceTo(state, volume.ReleaseDate.AddHours(-1));
        Assert.False(volume.IsReleased);
        state.Advance(1);
        Assert.True(volume.IsReleased);
        Assert.Single(state.Events, e => e.Type == EventType.VolumeReleased);
        Assert.Equal(0, volume.WeeksOnSale);

        var monday = NextMondayMidnight(state.Clock.Now);
        AdvanceTo(state, monday.AddHours(-1));
        var fans = series.Fanbase;
        var trend = state.GenreTrendFor(series);
        var ledgerBefore = state.Ledger.Count;
        state.Advance(1);
        Assert.Equal(monday, state.Clock.Now);
        var copies = SalesRules.TankobonCopies(1, fans, 84, trend);
        Assert.True(copies > 0);
        Assert.Equal(copies, volume.CopiesSold);
        Assert.Equal(1, volume.WeeksOnSale);
        var royalty = state.Ledger.Skip(ledgerBefore).Single(l => l.Reason == "royalties");
        Assert.Equal(SalesRules.Royalty(copies, 400), royalty.Amount);
        Assert.Equal(series.Id, royalty.SeriesId);
        Assert.Equal(monday, royalty.Time);
        Assert.Equal(fans + copies * 0.05, series.Fanbase, 6);
        Assert.Single(state.Events, e => e.Type == EventType.VolumeReleased);

        // Week two on the following Monday.
        AdvanceTo(state, monday.AddDays(7).AddHours(-1));
        fans = series.Fanbase;
        state.Advance(1);
        Assert.Equal(copies + SalesRules.TankobonCopies(2, fans, 84, state.GenreTrendFor(series)), volume.CopiesSold);
        Assert.Equal(2, volume.WeeksOnSale);
    }

    [Fact]
    public void A_tankobon_stops_after_52_weeks()
    {
        var state = WithScheduledTankobon();
        var volume = state.Series[0].Volumes[0];
        volume.WeeksOnSale = 51;
        volume.IsReleased = true;
        volume.ReleaseDate = state.Clock.Now;
        var monday = NextMondayMidnight(state.Clock.Now);
        AdvanceTo(state, monday);
        Assert.Equal(52, volume.WeeksOnSale);
        var copies = volume.CopiesSold;
        AdvanceTo(state, monday.AddDays(14));
        Assert.Equal(52, volume.WeeksOnSale);
        Assert.Equal(copies, volume.CopiesSold);
        GameState.FromJson(state.ToJson());
    }

    private static GameState DoujinRomance()
    {
        var state = GameState.NewGame();
        state.Apply(new CreateSeriesCommand("Petals", "romance", Cadence.Monthly, 19)); // romance trend is 1.00 in 1996
        for (var i = 0; i < 24 * 60 && state.Series[0].Volumes.Count == 0; i++) state.Advance(1);
        Assert.Single(state.Series[0].Volumes);
        return state;
    }

    [Fact]
    public void A_doujin_volume_sells_for_four_weeks_offline()
    {
        var state = DoujinRomance();
        var series = state.Series[0];
        var volume = series.Volumes[0];
        Assert.Equal(0, series.Fanbase);
        var monday = NextMondayMidnight(state.Clock.Now);
        var moneyBefore = state.Money;
        // Romance's baseline is 1.00 through the 1990s, but the May trend update adds a little noise.
        var trend = state.GenreTrendFor(series);
        Assert.InRange(trend, 0.98, 1.02);
        var week1 = SalesRules.DoujinCopies(1, 0, 84, trend, 0); // 240 at a trend of exactly 1.00

        AdvanceTo(state, monday);
        Assert.Equal(week1, volume.CopiesSold);
        Assert.InRange(week1, 235, 245);
        Assert.Equal(1, volume.WeeksOnSale);
        var sale = Assert.Single(state.Ledger, l => l.Reason == "doujin sales");
        Assert.Equal(week1 * (500 * 0.6 - 120), sale.Amount, 6); // printing costs 120 per copy
        Assert.Equal(moneyBefore + sale.Amount, state.Money);
        Assert.Equal(week1 * 0.3, series.Fanbase, 6);
        Assert.Equal(week1, state.DoujinCopiesThisMonth);
        Assert.Equal(week1 * 0.3, state.DoujinFansThisMonth, 6);

        AdvanceTo(state, monday.AddDays(7));
        var week2 = SalesRules.DoujinCopies(2, week1 * 0.3, 84, trend, 0); // (200 + fans x 0.3) x 1.2 x trend x 0.4
        Assert.Equal(week1 + week2, volume.CopiesSold);
        Assert.Equal(2, volume.WeeksOnSale);

        AdvanceTo(state, monday.AddDays(35));
        Assert.Equal(4, volume.WeeksOnSale);
        var total = volume.CopiesSold;
        AdvanceTo(state, monday.AddDays(42));
        Assert.Equal(total, volume.CopiesSold);
        // Four Mondays of sales for this volume; a second doujin volume starts selling later in the window.
        Assert.Equal(4, state.Ledger.Count(l => l.Reason == "doujin sales" && l.Time <= monday.AddDays(21)));
    }

    [Fact]
    public void Get_online_debits_the_ledger_once_and_validates()
    {
        var state = GameState.NewGame();
        Assert.Equal(120_000, state.InternetCostNow);
        state.Apply(new GetOnlineCommand());
        Assert.True(state.HasInternet);
        Assert.Equal(380_000, state.Money);
        var entry = Assert.Single(state.Ledger);
        Assert.Equal("internet", entry.Reason);
        Assert.Equal(-120_000, entry.Amount);
        var online = Assert.Single(state.Events, e => e.Type == EventType.WentOnline);
        Assert.Equal(-120_000, online.Amount);
        Assert.Throws<InvalidCommandException>(() => state.Apply(new GetOnlineCommand()));

        var poor = GameState.NewGame();
        poor.AddLedger(-450_000, "test");
        var ex = Assert.Throws<InvalidCommandException>(() => poor.Apply(new GetOnlineCommand()));
        Assert.Contains("120,000", ex.Message);
        Assert.False(poor.HasInternet);
        GameState.FromJson(state.ToJson());
    }

    [Fact]
    public void Online_widens_the_doujin_window_and_spreads_word_of_mouth()
    {
        var state = DoujinRomance();
        var series = state.Series[0];
        var volume = series.Volumes[0];
        state.Apply(new GetOnlineCommand());
        var monday = NextMondayMidnight(state.Clock.Now);

        var trend = state.GenreTrendFor(series);
        AdvanceTo(state, monday);
        var week1 = SalesRules.DoujinCopies(1, 0, 84, trend, 0.1); // 240 x 1.05 = 252 at a trend of exactly 1.00
        Assert.Equal(week1, volume.CopiesSold);
        Assert.True(week1 > SalesRules.DoujinCopies(1, 0, 84, trend, 0));
        var fansAfterSale = week1 * 0.3;
        Assert.Equal(fansAfterSale + fansAfterSale * 0.01 * 0.1, series.Fanbase, 6); // word of mouth on top

        AdvanceTo(state, monday.AddDays(7 * 7));
        Assert.Equal(8, volume.WeeksOnSale);
        var total = volume.CopiesSold;
        AdvanceTo(state, monday.AddDays(7 * 8));
        Assert.Equal(total, volume.CopiesSold);
        GameState.FromJson(state.ToJson());
    }

    [Fact]
    public void Convention_recap_sums_the_previous_month_on_its_first_monday()
    {
        var state = DoujinRomance();
        var firstSale = NextMondayMidnight(state.Clock.Now);
        AdvanceTo(state, firstSale);
        Assert.DoesNotContain(state.Events, e => e.Type == EventType.ConventionRecap);

        var firstMonday = new DateTime(firstSale.Year, firstSale.Month, 1).AddMonths(1);
        while (firstMonday.DayOfWeek != DayOfWeek.Monday) firstMonday = firstMonday.AddDays(1);
        AdvanceTo(state, firstMonday.AddHours(-1));
        var copies = state.DoujinCopiesThisMonth;
        var fans = state.DoujinFansThisMonth;
        Assert.True(copies > 0);
        state.Advance(1);
        var recap = Assert.Single(state.Events, e => e.Type == EventType.ConventionRecap);
        Assert.Equal(firstMonday, recap.Time);
        Assert.Equal(copies, recap.Amount);
        Assert.Contains($"{copies:N0} doujin copies", recap.Message);
        Assert.Contains($"{fans:N0} new fans", recap.Message);
        // The recap ran before this Monday's sale, so only this week's copies remain in the counter.
        var soldThisMonday = state.Series[0].Volumes.Sum(v => v.CopiesSold) - copies;
        Assert.Equal(soldThisMonday, state.DoujinCopiesThisMonth);
    }

    [Fact]
    public void No_convention_recap_when_nothing_sold()
    {
        var state = GameState.NewGame();
        state.Advance(24 * 40);
        Assert.DoesNotContain(state.Events, e => e.Type == EventType.ConventionRecap);
    }

    [Fact]
    public void Passing_100k_copies_is_a_milestone()
    {
        var state = WithScheduledTankobon();
        var series = state.Series[0];
        var volume = series.Volumes[0];
        volume.IsReleased = true;
        volume.ReleaseDate = state.Clock.Now;
        volume.CopiesSold = 99_000;
        volume.WeeksOnSale = 3;
        series.Fanbase = 500_000;
        var trackBefore = state.StudioTrackRecord;
        var influenceBefore = state.TrendOf("action").PlayerInfluence;
        AdvanceTo(state, NextMondayMidnight(state.Clock.Now));
        Assert.True(volume.CopiesSold >= 100_000);
        var milestone = Assert.Single(state.Events, e => e.Type == EventType.VolumeMilestone);
        Assert.Equal(volume.Id, milestone.VolumeId);
        Assert.Contains("100,000", milestone.Message);
        Assert.Equal(trackBefore + 3, state.StudioTrackRecord, 6);
        Assert.Equal(influenceBefore + 0.05, state.TrendOf("action").PlayerInfluence, 9);
        AdvanceTo(state, NextMondayMidnight(state.Clock.Now));
        Assert.Single(state.Events, e => e.Type == EventType.VolumeMilestone); // once
    }

    [Fact]
    public void Passing_a_million_gives_influence_once_per_series()
    {
        var state = WithScheduledTankobon();
        var series = state.Series[0];
        var volume = series.Volumes[0];
        volume.IsReleased = true;
        volume.ReleaseDate = state.Clock.Now;
        volume.CopiesSold = 999_000;
        volume.WeeksOnSale = 3;
        series.Fanbase = 5_000_000;
        var influenceBefore = state.TrendOf("action").PlayerInfluence;
        AdvanceTo(state, NextMondayMidnight(state.Clock.Now));
        Assert.Single(state.Events, e => e.Type == EventType.VolumeMilestone && e.Message.Contains("1,000,000"));
        Assert.True(series.MillionInfluenceGiven);
        Assert.Equal(influenceBefore + 0.15, state.TrendOf("action").PlayerInfluence, 9);
        Assert.True(state.Events.Count(e => e.Type == EventType.GenreTrendShifted) >= 1);
    }

    [Fact]
    public void Daily_recap_reports_yen_and_chapters_published()
    {
        var state = SerializedAtJump();
        ForceComplete(state, state.Series[0].OpenChapter!);
        AdvanceTo(state, new DateTime(1996, 4, 4, 23, 0, 0));
        var recap = state.Events.Last(e => e.Type == EventType.DailyRecap).Recap!;
        Assert.Equal(190_000, recap.YenEarned);
        Assert.Equal(1, recap.ChaptersPublished);
        Assert.Equal(0, recap.IssuesMissed);
        var next = state.Events.Last(e => e.Type == EventType.DailyRecap);
        Assert.Equal(new DateTime(1996, 4, 4).Date, next.Time.Date);

        state.Advance(24);
        var friday = state.Events.Last(e => e.Type == EventType.DailyRecap).Recap!;
        Assert.Equal(0, friday.YenEarned);
        Assert.Equal(0, friday.ChaptersPublished);
    }
}
