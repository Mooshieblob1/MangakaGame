using MangakaSim.Rules;
using Xunit;

namespace MangakaSim.Tests;

public class DifficultyTests
{
    [Fact]
    public void Standard_is_unchanged_and_the_dials_move_in_opposite_directions()
    {
        Assert.Equal((2, 3, 8), DifficultyRules.Clocks((2, 3, 8), 1));
        Assert.Equal((1, 2, 8), DifficultyRules.Clocks((2, 3, 8), 0));
        Assert.Equal((3, 5, 8), DifficultyRules.Clocks((2, 3, 8), 2));
        Assert.Equal((6, 19, 2), DifficultyRules.Clocks((8, 26, 2), 0));
        Assert.Equal((12, 39, 2), DifficultyRules.Clocks((8, 26, 2), 2));
        Assert.Equal([4, 6, 9], new[] { 0, 1, 2 }.Select(DifficultyRules.GraceChapters));
        Assert.Equal([.9, 1, 1.1], new[] { 0, 1, 2 }.Select(DifficultyRules.RivalStrength));
        Assert.Equal([1.15, 1, .85], new[] { 0, 1, 2 }.Select(DifficultyRules.PitchFactor));
    }

    private static GameState Preset(CareerDifficulty mode)
    {
        var state = SimulationFixture.EightPublished();
        state.Apply(new DifficultyCommand(mode));
        return state;
    }

    private static int IssuesUntilWarning(CareerDifficulty mode)
    {
        var state = Preset(mode);
        var series = state.Series[0];
        var magazine = state.PublisherCatalog.Get(series.Contract!.MagazineId);
        series.WeeksBelowLine = 0;
        series.WarningIssuedAt = null;
        series.Strikes.Clear();
        for (var issue = 1; issue <= 60; issue++)
        {
            series.LastRank = magazine.CancellationRank + 1;
            state.CancellationStep(series, magazine, false);
            if (series.WarningIssuedAt is not null) return issue;
        }
        throw new InvalidOperationException("No warning within 60 issues.");
    }

    [Fact]
    public void Editors_warn_later_on_relaxed_and_sooner_on_challenging()
    {
        var relaxed = IssuesUntilWarning(CareerDifficulty.Relaxed);
        var standard = IssuesUntilWarning(CareerDifficulty.Standard);
        var challenging = IssuesUntilWarning(CareerDifficulty.Challenging);
        Assert.True(relaxed > standard && standard > challenging, $"{relaxed} / {standard} / {challenging}");
        var state = SimulationFixture.EightPublished();
        Assert.Equal(CancellationRules.Clocks(state.Protection(state.Series[0])).Warning, standard);
    }

    [Theory]
    [InlineData(CareerDifficulty.Relaxed, 9)] [InlineData(CareerDifficulty.Standard, 6)] [InlineData(CareerDifficulty.Challenging, 4)]
    public void Newcomer_grace_follows_recovery(CareerDifficulty mode, int chapters)
    {
        var state = Preset(mode);
        Assert.Equal(chapters, state.GraceChapters(state.Series[0]));
        var copy = GameState.FromJson(state.ToJson());
        Assert.Equal(chapters, copy.GraceChapters(copy.Series[0]));
    }

    [Fact]
    public void Custom_dials_set_clocks_and_pitch_odds_independently()
    {
        var state = SimulationFixture.EightPublished();
        var series = state.Series[0];
        var baseClocks = CancellationRules.Clocks(state.Protection(series));
        state.Apply(new DifficultyCommand(CareerDifficulty.Custom, Pressure: 0, Recovery: 0));
        Assert.Equal(DifficultyRules.Clocks(baseClocks, 0), state.CancellationClocks(series));
        Assert.Equal(1.15, state.PitchFactor(series.BusinessId));
        Assert.Equal(1, state.PitchFactor(series.BusinessId + 1000));
    }

    [Fact]
    public void Pitch_odds_follow_the_preset()
    {
        double Chance(CareerDifficulty mode, string magazine)
        {
            var state = Preset(mode);
            return CareerGuidance.Outlook(state, state.Series[0], magazine).Chance;
        }
        var weakest = SimulationFixture.EightPublished();
        var magazine = CareerGuidance.PitchOutlooks(weakest, weakest.Series[0]).OrderBy(o => o.Chance).First().Magazine.Id;
        var baseline = Chance(CareerDifficulty.Standard, magazine);
        Assert.InRange(baseline, .01, .8);
        Assert.Equal(baseline * 1.15, Chance(CareerDifficulty.Relaxed, magazine), 10);
        Assert.Equal(baseline * .85, Chance(CareerDifficulty.Challenging, magazine), 10);
    }

    [Fact]
    public void Rival_series_score_higher_under_high_pressure_and_saves_replay()
    {
        IReadOnlyList<RankEntry> Ranking(CareerDifficulty mode)
        {
            var state = GameState.NewGame(3);
            state.Apply(new DifficultyCommand(mode));
            var market = state.Markets[0];
            state.Advance(state.Clock.HoursUntil(market.NextIssueClose));
            Assert.Equal(state.ToJson(), GameState.FromJson(state.ToJson()).ToJson());
            Assert.Equal(state.ToJson(), state.ReplayTimeline().ToJson());
            return market.LastRanking;
        }
        var standard = Ranking(CareerDifficulty.Standard).ToDictionary(r => r.FillerId!.Value, r => r.Score);
        var relaxed = Ranking(CareerDifficulty.Relaxed);
        var challenging = Ranking(CareerDifficulty.Challenging);
        Assert.Contains(standard.Values, v => v > 0);
        foreach (var row in relaxed) Assert.Equal(standard[row.FillerId!.Value] * .9, row.Score, 9);
        foreach (var row in challenging) Assert.Equal(standard[row.FillerId!.Value] * 1.1, row.Score, 9);
    }
}
