using MangakaSim;
using Xunit;

namespace MangakaSim.Tests;

public class MarketTests
{
    [Fact]
    public void New_game_has_one_market_per_catalog_magazine_with_first_closes()
    {
        var state = GameState.NewGame(5);
        Assert.Equal(PublisherCatalog.LoadDefault().Magazines.Select(m => m.Id), state.Markets.Select(m => m.MagazineId));
        Assert.Equal(new DateTime(1996, 4, 4, 18, 0, 0), state.MarketOf("tokiwa-jump").NextIssueClose);
        Assert.Equal(new DateTime(1996, 4, 2, 18, 0, 0), state.MarketOf("hoshigaku-sunday").NextIssueClose);
        Assert.Equal(new DateTime(1996, 4, 3, 18, 0, 0), state.MarketOf("kaidan-magazine").NextIssueClose);
        Assert.Equal(new DateTime(1996, 4, 5, 18, 0, 0), state.MarketOf("kaidan-afternoon").NextIssueClose);
        Assert.All(state.Markets, m => Assert.Equal(0, m.IssuesClosed));
        Assert.All(state.Markets, m => Assert.Null(m.LastIssueClose));
    }

    [Fact]
    public void Rosters_are_drawn_per_tier_with_one_iconic_star_in_each_flagship()
    {
        var state = GameState.NewGame(5);
        var catalog = PublisherCatalog.LoadDefault();
        foreach (var market in state.Markets)
        {
            var magazine = catalog.Require(market.MagazineId);
            Assert.Equal(magazine.RosterSize - 1, market.Fillers.Count);
            var (min, max) = FillerRules.PopularityRange(magazine.Tier);
            foreach (var filler in market.Fillers)
            {
                Assert.Contains(filler.Genre, TrendCatalog.LoadDefault().Genres);
                Assert.Matches("^[A-Z][a-z]+ [A-Z][a-z]+$", filler.Title);
                if (filler.IsIconic) Assert.InRange(filler.Popularity, 85, 95);
                else Assert.InRange(filler.Popularity, min, max);
                Assert.Equal(filler.Popularity, Math.Round(filler.Popularity));
            }
            Assert.Equal(magazine.Tier == 1 ? 1 : 0, market.Fillers.Count(f => f.IsIconic));
        }
        Assert.Equal(19, state.MarketOf("tokiwa-jump").Fillers.Count);
        Assert.Equal(13, state.MarketOf("hoshigaku-flowers").Fillers.Count);
        foreach (var market in state.Markets)
        {
            var ids = market.Fillers.Select(f => f.Id).ToList();
            Assert.Equal(ids.Count, ids.Distinct().Count());
            Assert.Equal(ids.Count + 1, market.NextFillerId);
        }
        Assert.Equal(2, state.NextId); // fillers do not consume the global id counter
    }

    [Fact]
    public void Rosters_are_deterministic_per_seed()
    {
        Assert.Equal(GameState.NewGame(9).ToJson(), GameState.NewGame(9).ToJson());
        var a = GameState.NewGame(9).MarketOf("tokiwa-jump").Fillers.Select(f => (f.Title, f.Popularity)).ToList();
        var b = GameState.NewGame(10).MarketOf("tokiwa-jump").Fillers.Select(f => (f.Title, f.Popularity)).ToList();
        Assert.NotEqual(a, b);
    }

    [Fact]
    public void Filler_genres_lean_toward_the_magazine_affinities()
    {
        // Over many seeds, Jump (action 1.2, slice of life 0.8) should draw more action than slice of life.
        int action = 0, slice = 0;
        for (var seed = 0; seed < 40; seed++)
        {
            var fillers = GameState.NewGame(seed).MarketOf("tokiwa-jump").Fillers;
            action += fillers.Count(f => f.Genre == "action");
            slice += fillers.Count(f => f.Genre == "slice of life");
        }
        Assert.True(action > slice, $"action {action} vs slice of life {slice}");
    }

    [Fact]
    public void Issue_close_advances_by_cadence_and_records_the_last_close()
    {
        var state = GameState.NewGame();
        state.Advance(24 * 7 * 5);
        var jump = state.MarketOf("tokiwa-jump");
        Assert.Equal(5, jump.IssuesClosed);
        Assert.Equal(new DateTime(1996, 5, 2, 18, 0, 0), jump.LastIssueClose);
        Assert.Equal(new DateTime(1996, 5, 9, 18, 0, 0), jump.NextIssueClose);
        var flowers = state.MarketOf("hoshigaku-flowers");
        Assert.Equal(2, flowers.IssuesClosed);
        Assert.Equal(new DateTime(1996, 5, 29, 18, 0, 0), flowers.NextIssueClose);
        Assert.All(state.Markets, m => Assert.True(m.NextIssueClose > state.Clock.Now));
    }

    [Fact]
    public void New_game_has_one_trend_per_genre_at_rest()
    {
        var state = GameState.NewGame();
        Assert.Equal(TrendCatalog.LoadDefault().Genres, state.Trends.Select(t => t.Genre));
        Assert.All(state.Trends, t => Assert.Equal(0, t.Noise + t.Boom + t.PlayerInfluence));
        Assert.Equal(TrendRules.Baseline(TrendCatalog.LoadDefault(), "action", state.Clock.Now), state.GenreTrendFor(new Series { Genre = "Action" }), 9);
        Assert.Equal(0.90, state.GenreTrendFor(new Series { Genre = "isekai" }), 9);
        Assert.Equal(1.0, state.GenreTrendFor(new Series { Genre = "action", IsIconic = true }), 9);
    }

    [Fact]
    public void Ledger_entries_update_money_and_round_trip()
    {
        var state = GameState.NewGame();
        state.Apply(new CreateSeriesCommand("Rush", "action", Cadence.Weekly, 19));
        state.AddLedger(-120_000, "internet");
        state.AddLedger(38_000, "chapter fee", state.Series[0].Id);
        Assert.Equal(418_000, state.Money);
        var loaded = GameState.FromJson(state.ToJson());
        Assert.Equal(418_000, loaded.Money);
        Assert.Equal(2, loaded.Ledger.Count);
        Assert.Equal("chapter fee", loaded.Ledger[1].Reason);
    }
}
