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
        Assert.Equal(0, volume.WeeksOnSale);
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
        var second = new Volume { Id = state.AllocateId(), Number = series.Volumes.Count + 1, IsDoujin = false,
            AverageQuality = 70, ReleaseDate = first.ReleaseDate, ReleasedAt = first.ReleasedAt, SalesWindowWeeks = 52 };
        series.Volumes.Add(second);
        first.WeeksOnSale = 0;
        first.CopiesSold = 99990;
        first.AverageQuality = 70;
        series.Fanbase = 3000000;
        var monday = state.Clock.Now.Date.AddDays(((int)DayOfWeek.Monday - (int)state.Clock.DayOfWeek + 7) % 7);
        if (monday <= state.Clock.Now) monday = monday.AddDays(7);
        state.Clock.Now = monday;
        var copies = SalesRules.CommercialCopies(3000000, 70, state.GenrePopularity(series.Genre), 1);
        var record = state.StudioTrackRecord;
        state.SalesStep();
        Assert.Equal(copies + 99990, first.CopiesSold);
        Assert.Equal(copies, second.CopiesSold);
        Assert.True(series.MillionCopyInfluenceAwarded);
        Assert.Equal(Math.Min(100, record + 26), state.StudioTrackRecord);
        Assert.Equal(4, state.Events.Count(e => e.Type == EventType.VolumeMilestone));
        Assert.Equal(.25, state.Trends.Single(t => t.Genre == "drama").PlayerInfluence, 10);
        state.Clock.Now = monday.AddDays(7);
        state.SalesStep();
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
        var volume = state.Series[0].Volumes.Single();
        PublishingTests.Until(state, () => volume.SalesClosed);
        Assert.Equal(0, volume.CopiesSold);
        Assert.Equal(4, volume.WeeksOnSale);
        Assert.Equal(500000, state.Money);
        Assert.DoesNotContain(state.Events, e => e.Type == EventType.ConventionRecap);
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
