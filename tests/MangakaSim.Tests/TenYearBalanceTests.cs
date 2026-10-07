using MangakaSim.Rules;
using Xunit;
namespace MangakaSim.Tests;

// Ten-year balance pass (2026-10-05, docs/superpowers/ten-year-balance-findings.md): a readership ceiling,
// tiring long runs, readers who follow their creator and a stricter "Run two series at once".
public class TenYearBalanceTests
{
    [Fact] public void Fan_gains_shrink_towards_the_readership_ceiling()
    {
        Assert.Equal(100, FanbaseRules.Saturated(0, 100));
        Assert.Equal(50, FanbaseRules.Saturated(FanbaseRules.ReadershipCeiling / 2, 100), 6);
        Assert.Equal(0, FanbaseRules.Saturated(FanbaseRules.ReadershipCeiling, 100));
        Assert.Equal(0, FanbaseRules.Saturated(FanbaseRules.ReadershipCeiling * 3, 100));
        Assert.Equal(-100, FanbaseRules.Saturated(FanbaseRules.ReadershipCeiling / 2, -100));
    }

    [Fact] public void A_series_never_passes_the_ceiling_however_long_it_runs()
    {
        var fans = FanbaseRules.ReadershipCeiling * .99;
        for (var issue = 0; issue < 500; issue++) fans = FanbaseRules.AfterPublication(fans, 1, 100, 1, 10, 20, true);
        Assert.True(fans <= FanbaseRules.ReadershipCeiling, $"{fans:N0}");
    }

    [Fact] public void Young_series_publish_as_before()
    {
        // Only the ceiling's tiny trim (0.2% at 20,000 readers) separates a young series from the old formula.
        var before = 20_000 * .995 + FanbaseRules.Gain(3, 80, FanbaseRules.RankFactor(5, 10, 14)) * (1 - 20_000 / FanbaseRules.ReadershipCeiling);
        var after = FanbaseRules.AfterPublication(20_000, 3, 80, 5, 10, 14, false, runMonths: 24);
        Assert.Equal(before, after, 0);
    }

    [Fact] public void Long_runs_join_readers_more_slowly_and_lose_them_faster()
    {
        Assert.Equal(1, FanbaseRules.FatigueGain(FanbaseRules.FatigueStartMonths));
        Assert.Equal(0, FanbaseRules.FatigueDecay(FanbaseRules.FatigueStartMonths));
        Assert.True(FanbaseRules.FatigueGain(100) < 1 && FanbaseRules.FatigueGain(1000) == .25);
        Assert.True(FanbaseRules.FatigueDecay(100) > 0 && FanbaseRules.FatigueDecay(1000) == .006);
        var fresh = FanbaseRules.AfterPublication(300_000, 3, 95, 1, 10, 14, false, runMonths: 12);
        var tired = FanbaseRules.AfterPublication(300_000, 3, 95, 1, 10, 14, false, runMonths: 110);
        Assert.True(tired < fresh);
        Assert.True(tired < 300_000, "A rank-1 hit of 300,000 readers in its tenth year should be past its peak.");
    }

    [Fact] public void Weekly_and_monthly_series_tire_at_the_same_pace_per_month()
    {
        var monthly = FanbaseRules.AfterPublication(1_000_000, 1, 0, 20, 10, 20, true, 110, 1);
        var weekly = 1_000_000d;
        var perMonth = FanbaseRules.ChaptersPerMonth(Cadence.Weekly);
        for (var i = 0; i < 13; i++) weekly = FanbaseRules.AfterPublication(weekly, 1, 0, 20, 10, 20, true, 110, perMonth);
        // Three months of weekly issues lose about three times a month of monthly issues (gains are zero at quality 0).
        Assert.Equal(1 - Math.Pow(monthly / 1_000_000, 3), 1 - weekly / 1_000_000, 3);
    }

    [Fact] public void A_side_doujin_is_not_a_second_series()
    {
        var s = GameState.NewGame(0);
        s.Apply(new CreateDoujinCommand("Side story", "comedy"));
        s.Apply(new CreateSeriesCommand("Main story", "adventure", Cadence.Monthly, 16));
        Assert.Equal(0, GoalCatalog.Get("two-series").Measure(s).Have);
        s.Series.Single(x => x.Title == "Main story").Publishing = PublishingStatus.Serialized;
        Assert.Equal(1, GoalCatalog.Get("two-series").Measure(s).Have);
    }

    [Fact] public void A_new_magazine_series_starts_with_part_of_the_creators_readers()
    {
        var s = GameState.NewGame(0);
        s.Apply(new CreateSeriesCommand("Earlier hit", "adventure", Cadence.Monthly, 16));
        s.Apply(new CreateSeriesCommand("New story", "adventure", Cadence.Monthly, 16));
        var earlier = s.Series.Single(x => x.Title == "Earlier hit");
        var series = s.Series.Single(x => x.Title == "New story");
        s.Apply(new PauseSeriesCommand(earlier.Id));
        earlier.Fanbase = 200_000;
        for (var day = 0; day < 700 && series.Publishing != PublishingStatus.Offered; day++)
        {
            if (series.Publishing == PublishingStatus.Unpublished && CareerGuidance.PitchOutlooks(s, series).FirstOrDefault(o => o.Open) is { } best)
                s.Apply(new PitchSeriesCommand(series.Id, best.Magazine.Id));
            s.Advance(24);
        }
        Assert.Equal(PublishingStatus.Offered, series.Publishing);
        Assert.True(series.Fanbase < 60_000);
        s.Apply(new AcceptOfferCommand(series.Id));
        Assert.Equal(earlier.Fanbase * FanbaseRules.FollowingShare, series.Fanbase, 0);
        Assert.Equal(200_000, earlier.Fanbase);
        var json = s.ToJson();
        Assert.Equal(json, GameState.FromJson(json).ToJson());
    }
}
