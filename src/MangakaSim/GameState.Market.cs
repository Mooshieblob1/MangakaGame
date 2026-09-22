namespace MangakaSim;

public partial class GameState
{
    // ---------------------------------------------------------------- new game setup

    private void InitialiseTrends()
    {
        Trends = TrendData.Genres.Select(g => new GenreTrend { Genre = g }).ToList();
    }

    private void InitialiseMarkets()
    {
        Markets = new List<MagazineState>();
        foreach (var magazine in Publishers.Magazines)
        {
            var market = new MagazineState
            {
                MagazineId = magazine.Id,
                NextIssueClose = magazine.FirstCloseAtOrAfter(Clock.Now),
            };
            var (min, max) = FillerRules.PopularityRange(magazine.Tier);
            for (var i = 0; i < magazine.RosterSize - 1; i++)
                market.Fillers.Add(NewFiller(market, magazine, min, max));
            if (magazine.Tier == 1)
            {
                var star = market.Fillers.MaxBy(f => f.Popularity)!;
                star.IsIconic = true;
                star.Popularity = Rng.NextInt(FillerRules.IconicRange.Min, FillerRules.IconicRange.Max);
            }
            Markets.Add(market);
        }
    }

    private FillerSeries NewFiller(MagazineState market, Magazine magazine, int minPopularity, int maxPopularity)
    {
        var title = FillerRules.Adjectives[Rng.NextInt(FillerRules.Adjectives.Length)] + " " +
                    FillerRules.Nouns[Rng.NextInt(FillerRules.Nouns.Length)];
        return new FillerSeries
        {
            Id = market.NextFillerId++,
            Title = title,
            Genre = DrawGenre(magazine),
            Popularity = Rng.NextInt(minPopularity, maxPopularity),
        };
    }

    /// <summary>Catalog genre weighted by the magazine's affinities.</summary>
    private string DrawGenre(Magazine magazine)
    {
        var genres = TrendData.Genres;
        var weights = genres.Select(magazine.Affinity).ToList();
        var roll = Rng.NextDouble() * weights.Sum();
        for (var i = 0; i < genres.Count; i++)
        {
            roll -= weights[i];
            if (roll < 0) return genres[i];
        }
        return genres[^1];
    }

    // ---------------------------------------------------------------- issue close

    /// <summary>Closes every magazine whose issue is due now. Filled in by the market tasks.</summary>
    internal void IssueCloseStep()
    {
        foreach (var market in Markets)
        {
            if (market.NextIssueClose > Clock.Now) continue;
            var magazine = Publishers.Require(market.MagazineId);
            CloseIssue(market, magazine);
        }
    }

    private void CloseIssue(MagazineState market, Magazine magazine)
    {
        market.LastIssueClose = market.NextIssueClose;
        market.NextIssueClose = market.NextIssueClose.AddDays(magazine.CadenceDays);
        market.IssuesClosed++;
    }
}
