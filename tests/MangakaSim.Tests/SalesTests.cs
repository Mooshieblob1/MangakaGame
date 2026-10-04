using MangakaSim.Rules;
using Xunit;

namespace MangakaSim.Tests;

public class SalesTests
{
    private static GameState Doujin()
    {
        var state = PublishingTests.Started();
        PublishingTests.Until(state, () => state.Series[0].Volumes.Count == 1);
        state.Apply(new PauseSeriesCommand(state.Series[0].Id));
        state.Apply(new StudioActionCommand(StudioAction.Print,state.Series[0].Volumes[0].Id,Amount:100));
        state.Advance(24);
        return state;
    }
    [Theory]
    [InlineData(false, 4)] [InlineData(true, 8)]
    public void Doujin_windows_close_once_and_online_does_not_restart_exhausted_books(bool online, int window)
    {
        var state = Doujin();
        var series = state.Series[0];
        var volume = series.Volumes[0];
        if (online) state.Apply(new GetOnlineCommand());
        PublishingTests.Until(state, () => volume.SalesClosed);
        Assert.Equal(window, volume.WeeksOnSale);
        Assert.Single(state.Events, e => e.Type == EventType.VolumeReleased && e.VolumeId == volume.Id);
        var copies = volume.CopiesSold;
        if (!online) state.Apply(new GetOnlineCommand());
        state.Advance(24 * 30);
        Assert.Equal(copies, volume.CopiesSold);
        Assert.Equal(window, volume.WeeksOnSale);
        Assert.Equal(state.ToJson(), GameState.FromJson(state.ToJson()).ToJson());
    }
    [Fact]
    public void Weekly_batch_is_idempotent_and_convention_precedes_current_month_sales()
    {
        var state = Doujin();
        // Streaming sales (2026-10-03): a repeated step in the same shop hour releases nothing more.
        state.Advance(state.Clock.HoursUntil(SalesRules.NextShopHour(state.Clock.Now)));
        Assert.Contains(state.Series[0].Volumes[0].SalesPlans!, p => p.HoursDone > 0);
        var hour = state.ToJson();
        state.SalesStep();
        Assert.Equal(hour, state.ToJson());
        var firstMonday = new DateTime(1996, 5, 6);
        state.Advance(state.Clock.HoursUntil(firstMonday.AddHours(-1)));
        var previous = state.DoujinCopiesThisMonth;
        Assert.True(previous > 0);
        state.Advance(1);
        var recap = state.Events.Last(e => e.Type == EventType.ConventionRecap);
        Assert.Contains(previous.ToString("N0"), recap.Message);
        Assert.Equal(firstMonday, recap.Time);
        var before = state.ToJson();
        state.SalesStep();
        Assert.Equal(before, state.ToJson());
    }
    [Fact]
    public void Commercial_release_is_six_weeks_later_and_stops_after_week_52()
    {
        var state = SimulationFixture.Serialized();
        var series = state.Series[0];
        PublishingTests.Until(state, () => series.Volumes.Any(v => !v.IsDoujin));
        var volume = series.Volumes.First(v => !v.IsDoujin);
        var last = series.Chapters.Single(c => c.Id == volume.ChapterIds.Last());
        Assert.Equal(last.PublishedAt!.Value.AddDays(42), volume.ReleaseDate);
        Assert.Null(volume.ReleasedAt);
        state.Apply(new EndSeriesCommand(series.Id));
        state.Advance(state.Clock.HoursUntil(volume.ReleaseDate));
        Assert.Equal(volume.ReleaseDate, volume.ReleasedAt);
        // Streaming sales (2026-10-03): the release plans its first week at once.
        Assert.Equal(1, volume.WeeksOnSale);
        Assert.Single(volume.SalesPlans!, p => p.Kind == SaleKind.Commercial && p.Total > 0);
        PublishingTests.Until(state, () => volume.SalesClosed);
        Assert.Equal(52, volume.WeeksOnSale);
        var copies = volume.CopiesSold;
        state.Advance(24 * 14);
        Assert.Equal(copies, volume.CopiesSold);
        Assert.Equal(state.ToJson(), GameState.FromJson(state.ToJson()).ToJson());
    }
    [Fact]
    public void Multiple_volumes_use_one_fanbase_snapshot_and_milestones_fire_once()
    {
        var state = SimulationFixture.EightPublished();
        var series = state.Series[0];
        state.Apply(new EndSeriesCommand(series.Id));
        var first = series.Volumes.First(v => !v.IsDoujin);
        // Controlled sales fixture: two already-released commercial editions.
        // Edition overlap is intentionally not loaded as a save; production
        // collection and membership validation are covered by separate tests.
        var second = new Volume { BusinessId = state.ControlledBusinessId, Id = state.AllocateId(), Number = series.Volumes.Count + 1, IsDoujin = false,
            AverageQuality = 70, ReleaseDate = first.ReleaseDate, ReleasedAt = first.ReleasedAt, SalesWindowWeeks = 52 };
        series.Volumes.Add(second);
        first.WeeksOnSale = 0;
        first.SalesPlans = null;
        first.CopiesSold = 99990;
        first.AverageQuality = 70;
        var monday = state.Clock.Now.Date.AddDays(((int)DayOfWeek.Monday - (int)state.Clock.DayOfWeek + 7) % 7);
        if (monday <= state.Clock.Now) monday = monday.AddDays(7);
        state.Clock.Now = monday;
        var tier = state.PublisherCatalog.Get(series.PastContracts.Last().MagazineId).Tier;
        // Enough readers for a million-copy week whatever the magazine size.
        series.Fanbase = 3000000 / SalesRules.TierDemand(tier);
        var copies = SalesRules.CommercialCopies(series.Fanbase, 70, state.GenrePopularity(series.Genre), 1, tier);
        var record = state.StudioTrackRecord;
        var influence=state.Trends.Single(t=>t.Genre=="drama").PlayerInfluence;
        // Streaming sales (2026-10-03): Monday 00:00 plans the week; its shop hours release it.
        void Week(DateTime start) { state.Clock.Now = start; state.SalesStep(); for (var h = 1; h < 24 * 7; h++) { state.Clock.Now = start.AddHours(h); state.SalesStep(); } }
        Week(monday);
        Assert.Equal(copies + 99990, first.CopiesSold);
        Assert.Equal(copies, second.CopiesSold);
        Assert.True(series.MillionCopyInfluenceAwarded);
        Assert.Equal(Math.Min(100, record + 26), state.StudioTrackRecord);
        Assert.Equal(4, state.Events.Count(e => e.Type == EventType.VolumeMilestone));
        Assert.Equal(influence+.25, state.Trends.Single(t => t.Genre == "drama").PlayerInfluence, 10);
        Week(monday.AddDays(7));
        Assert.True(first.CopiesSold > copies + 99990);
        Assert.Equal(4, state.Events.Count(e => e.Type == EventType.VolumeMilestone));
    }
    [Fact]
    public void Zero_quality_still_advances_sales_window_without_income()
    {
        var state = PublishingTests.Started();
        for (var n = 0; n < 5; n++)
        {
            var chapter = state.Series[0].Chapters.Last();
            foreach (var stage in StageOrder.All) state.Apply(new SkipStageCommand(chapter.Id, stage));
        }
        state.Apply(new PauseSeriesCommand(state.Series[0].Id));
        state.Apply(new StudioActionCommand(StudioAction.Print,state.Series[0].Volumes[0].Id,Amount:100));
        state.Advance(24);
        var volume = state.Series[0].Volumes.Single();
        PublishingTests.Until(state, () => volume.SalesClosed);
        Assert.Equal(0, volume.CopiesSold);
        Assert.Equal(4, volume.WeeksOnSale);
        // Goal rewards (career goals spec 2026-10-01) are income, but not sales income.
        Assert.Equal(300000 + state.Ledger.Where(e => e.Kind is AccountEntryKind.Expense or AccountEntryKind.GoalReward).Sum(e => e.Amount), state.Money);
        Assert.DoesNotContain(state.Events, e => e.Type == EventType.ConventionRecap);
    }
    [Fact]
    public void Tier_demand_and_print_run_ladder_have_exact_boundaries()
    {
        Assert.Equal(1, SalesRules.TierDemand(1));
        Assert.Equal(.6, SalesRules.TierDemand(2));
        Assert.Equal(.35, SalesRules.TierDemand(3));
        Assert.Throws<ArgumentOutOfRangeException>(() => SalesRules.TierDemand(4));
        Assert.Equal(21000, SalesRules.CommercialCopies(100000, 70, 1, 1, 3));
        Assert.Equal(10000, SalesRules.Rung(0));
        Assert.Equal(10000, SalesRules.Rung(10000));
        Assert.Equal(15000, SalesRules.Rung(10001));
        Assert.Equal(100000, SalesRules.Rung(70001));
        Assert.Equal(150000, SalesRules.Rung(100001));
        Assert.Equal(500000, SalesRules.Rung(500000));
        Assert.Equal(600000, SalesRules.Rung(500001));
        Assert.Throws<ArgumentOutOfRangeException>(() => SalesRules.Rung(-1));
    }
    [Fact]
    public void Royalties_are_paid_per_print_run_at_release_and_each_reprint()
    {
        var state = SimulationFixture.Serialized();
        var series = state.Series[0];
        PublishingTests.Until(state, () => series.Volumes.Any(v => !v.IsDoujin));
        var volume = series.Volumes.First(v => !v.IsDoujin);
        state.Apply(new EndSeriesCommand(series.Id));
        state.Advance(state.Clock.HoursUntil(volume.ReleaseDate));
        var index = Economy.PriceIndex(state.TrendCatalog, volume.ReleaseDate);
        var first = Assert.Single(state.Ledger, e => e.Reason == "royalties, first print run" && e.SeriesId == series.Id);
        Assert.Equal(volume.ReleaseDate, first.Time);
        Assert.Equal(SalesRules.Income(SalesRules.FirstPrintRun, index, false), first.Amount);
        PublishingTests.Until(state, () => volume.SalesClosed);
        var reprints = state.Ledger.Where(e => e.Reason == "royalties, reprint" && e.SeriesId == series.Id).ToList();
        var paid = first.Amount + reprints.Sum(e => e.Amount);
        Assert.InRange(paid, SalesRules.Income(SalesRules.Rung(volume.CopiesSold), index, false) - reprints.Count,
            SalesRules.Income(SalesRules.Rung(volume.CopiesSold), index, false) + reprints.Count);
        Assert.All(reprints, e => Assert.Contains(state.Events, m => m.Time == e.Time && m.Message.Contains("goes back to press")));
    }
    [Fact]
    public void Internet_unaffordability_and_numeric_overflow_are_explicit()
    {
        var state = GameState.NewGame();
        state.Money = 1;
        var before = state.ToJson();
        Assert.Throws<InvalidCommandException>(() => state.Apply(new GetOnlineCommand()));
        Assert.Equal(before, state.ToJson());
        Assert.Throws<OverflowException>(() => SalesRules.Copies(double.PositiveInfinity));
        Assert.Throws<OverflowException>(() => SalesRules.Copies(1e20));
        Assert.Throws<OverflowException>(() => Economy.Yen(double.NaN));
    }
}
