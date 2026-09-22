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

    /// <summary>Closes every magazine whose issue is due now, in catalog order, then runs the monthly trend update once.</summary>
    internal void IssueCloseStep()
    {
        foreach (var market in Markets)
        {
            if (market.NextIssueClose > Clock.Now) continue;
            var magazine = Publishers.Require(market.MagazineId);
            CloseIssue(market, magazine);
        }
    }

    private sealed record IssueRow(string Title, string Genre, Series? Series, Chapter? Chapter, FillerSeries? Filler)
    {
        public double Score { get; set; }
    }

    internal IEnumerable<Series> SerializedIn(string magazineId) =>
        Series.Where(s => s.IsSerialized && s.Contract!.MagazineId == magazineId);

    private void CloseIssue(MagazineState market, Magazine magazine)
    {
        var closeTime = market.NextIssueClose;
        var rows = new List<IssueRow>();

        // 1. Publish or miss. The earliest unpublished chapter of each series is the one this issue judges.
        foreach (var series in SerializedIn(magazine.Id).ToList())
        {
            var chapter = series.Chapters.Where(c => !c.IsPublished && !c.IsOneShot).MinBy(c => c.Number);
            if (chapter is null || chapter.DueDate > closeTime) continue;
            if (chapter.Status == ChapterStatus.Complete && chapter.Editor == EditorStatus.Approved)
            {
                PublishChapter(series, chapter, magazine, closeTime);
                rows.Add(new IssueRow(series.Title, TrendRules.Normalise(series.Genre, TrendData), series, chapter, null));
            }
            else
            {
                MissIssue(series, chapter, magazine, closeTime);
                if (!series.IsIconic) ApplyCancellationRule(series, magazine, rank: null);
            }
            if (series.IsSerialized) ResequenceDueDates(series, magazine, chapter);
        }
        foreach (var filler in market.Fillers)
            rows.Add(new IssueRow(filler.Title, filler.Genre, null, null, filler));

        // 2. Score.
        foreach (var row in rows)
        {
            var others = rows.Count(r => r != row && r.Genre == row.Genre);
            var crowding = TrendRules.Crowding(others);
            if (row.Filler is { } filler)
            {
                row.Score = RankingRules.FillerScore(filler.Popularity, crowding);
            }
            else
            {
                var series = row.Series!;
                var affinity = series.IsIconic ? 1.0 : magazine.Affinity(row.Genre);
                row.Score = RankingRules.PlayerScore(row.Chapter!.Quality ?? 0, series.Fanbase, magazine.Tier,
                    affinity, GenreTrendFor(series), crowding);
            }
        }
        var ordered = rows.OrderByDescending(r => r.Score)
            .ThenBy(r => r.Filler?.Id ?? int.MaxValue)
            .ThenBy(r => r.Series?.Id ?? int.MaxValue)
            .ToList();

        // 3. Record.
        market.LastRanking = ordered.Select((r, i) => new RankEntry
        {
            Rank = i + 1,
            Title = r.Title,
            SeriesId = r.Series?.Id,
            FillerId = r.Filler?.Id,
            Score = r.Score,
        }).ToList();
        var playerRows = new List<(IssueRow Row, int Rank)>();
        for (var i = 0; i < ordered.Count; i++)
        {
            var row = ordered[i];
            if (row.Series is null) continue;
            row.Chapter!.Rank = i + 1;
            row.Series.LastRank = i + 1;
            playerRows.Add((row, i + 1));
        }
        market.IssuesClosed++;
        if (playerRows.Count > 0)
        {
            var best = playerRows.MinBy(p => p.Rank);
            Emit(EventType.RankingPublished,
                $"{magazine.Name} issue {market.IssuesClosed}: {best.Row.Title} ranks #{best.Rank} of {ordered.Count} (line at #{magazine.CancellationRank}).",
                new EventContext(SeriesId: best.Row.Series!.Id, ChapterNumber: best.Row.Chapter!.Number, MagazineId: magazine.Id, Rank: best.Rank));
        }

        // 4-6. Fanbase, cultural impact, reputation and the cancellation rule.
        foreach (var (row, rank) in playerRows)
            ApplyIssueResult(row.Series!, row.Chapter!, magazine, rank);

        // 7. Fillers drift and retire.
        foreach (var filler in market.Fillers.ToList())
        {
            filler.Popularity = FillerRules.Drift(filler.Popularity, Rng.NextInt(-FillerRules.MaxDrift, FillerRules.MaxDrift));
            var rank = market.LastRanking.First(r => r.FillerId == filler.Id).Rank;
            filler.IssuesBelowLine = rank > magazine.CancellationRank ? filler.IssuesBelowLine + 1 : 0;
            if (!filler.IsIconic && filler.IssuesBelowLine >= FillerRules.RetireAfterIssuesBelowLine)
            {
                var index = market.Fillers.IndexOf(filler);
                market.Fillers[index] = NewFiller(market, magazine, FillerRules.ReplacementRange.Min, FillerRules.ReplacementRange.Max);
            }
        }

        // 8. Advance.
        market.LastIssueClose = closeTime;
        market.NextIssueClose = closeTime.AddDays(magazine.CadenceDays);

        // 9. Monthly trend update, once per calendar month across all magazines.
        var month = new DateTime(Clock.Now.Year, Clock.Now.Month, 1);
        if (month > LastTrendUpdateMonth)
        {
            LastTrendUpdateMonth = month;
            UpdateTrendsMonthly();
        }
    }

    private void PublishChapter(Series series, Chapter chapter, Magazine magazine, DateTime closeTime)
    {
        var contract = series.Contract!;
        chapter.PublishedAt = Clock.Now;
        series.ChaptersPublished++;
        contract.ChaptersPublished++;
        var fee = (long)chapter.Pages * contract.FeePerPage;
        AddLedger(fee, "chapter fee", series.Id);
        Emit(EventType.ChapterPublished,
            $"{series.Title} ch.{chapter.Number} runs in {magazine.Name} (quality {chapter.Quality}); fee {fee:N0} yen.",
            new EventContext(SeriesId: series.Id, ChapterNumber: chapter.Number, MagazineId: magazine.Id, Amount: fee));
        TryScheduleTankobon(series, magazine, closeTime);
    }

    private void MissIssue(Series series, Chapter chapter, Magazine magazine, DateTime closeTime)
    {
        var contract = series.Contract!;
        var inGrace = contract.ChaptersPublished < ReputationRules.GraceChapters;
        var reason = chapter.Status == ChapterStatus.Complete
            ? "the name is still with the editor"
            : chapter.Editor == EditorStatus.AwaitingReview ? "the name is still with the editor" : "the chapter is not finished";
        chapter.DueDate = closeTime.AddDays(magazine.CadenceDays);
        var strike = !series.IsIconic && !inGrace;
        if (strike) series.Strikes.Add(Clock.Now);
        if (!series.IsIconic) series.Fanbase *= FanbaseRules.MissChurn;
        AdjustTrackRecord(ReputationRules.MissedIssue);
        Emit(EventType.IssueMissed,
            $"{series.Title} misses {magazine.Name}'s issue: {reason}. " +
            (strike ? $"Strike {LiveStrikes(series, magazine).Count}. " : inGrace ? "No strike during the opening chapters. " : "") +
            $"Ch.{chapter.Number} is now due {chapter.DueDate:d MMM}.",
            new EventContext(SeriesId: series.Id, ChapterNumber: chapter.Number, MagazineId: magazine.Id));
    }

    /// <summary>Keeps later unpublished chapters due at strictly later closes than the one just judged.</summary>
    private void ResequenceDueDates(Series series, Magazine magazine, Chapter judged)
    {
        var previousDue = judged.DueDate;
        foreach (var later in series.Chapters.Where(c => c.Number > judged.Number && !c.IsPublished && !c.IsOneShot).OrderBy(c => c.Number))
        {
            if (later.DueDate <= previousDue) later.DueDate = NextCloseAfter(magazine, previousDue);
            previousDue = later.DueDate;
        }
    }

    /// <summary>Fanbase, cultural impact, reputation, player influence and the cancellation rule for one ranked chapter.</summary>
    private void ApplyIssueResult(Series series, Chapter chapter, Magazine magazine, int rank)
    {
        var quality = chapter.Quality ?? 0;
        var top3 = rank <= 3;

        if (series.IsIconic)
        {
            series.Fanbase += FanbaseRules.IssueGain(magazine.Tier, FanbaseRules.IconicRankFactor, quality);
        }
        else
        {
            series.Fanbase *= FanbaseRules.IssueChurn;
            series.Fanbase += FanbaseRules.IssueGain(magazine.Tier,
                FanbaseRules.RankFactor(rank, magazine.CancellationRank, magazine.RosterSize), quality);
        }

        series.CulturalImpact = Math.Min(100, series.CulturalImpact + 0.05 + (top3 ? 0.1 : 0));

        if (!series.IsIconic)
        {
            if (rank == 1) AdjustTrackRecord(ReputationRules.Rank1);
            else if (top3) AdjustTrackRecord(ReputationRules.Rank2To3);
            if (top3)
            {
                foreach (var (personId, share) in HourShares(chapter))
                    if (FindPerson(personId) is { } person) AdjustReputation(person, ReputationRules.PersonTop3 * share);
                if (quality >= 80) AddPlayerInfluence(series.Genre, 0.01);
            }
            ApplyCancellationRule(series, magazine, rank);
        }
        CheckIconic(series);
    }

    /// <summary>A series becomes Iconic once impact and fanbase both cross their thresholds; the flag never clears.</summary>
    internal void CheckIconic(Series series)
    {
        if (series.IsIconic) return;
        if (series.CulturalImpact < FanbaseRules.IconicImpact || series.Fanbase < FanbaseRules.IconicFanbase) return;
        series.IsIconic = true;
        Emit(EventType.SeriesBecameIconic,
            $"{series.Title} has become a cultural icon: {series.Fanbase:N0} readers and impact {series.CulturalImpact:0}.",
            new EventContext(SeriesId: series.Id));
    }

    // ---------------------------------------------------------------- trends

    internal static string GenreLabel(string genre) =>
        genre.Length == 0 ? genre : char.ToUpperInvariant(genre[0]) + genre[1..];

    /// <summary>Layer 3: moves a genre's player influence and announces every 0.1 crossed upward.</summary>
    internal void AddPlayerInfluence(string rawGenre, double delta)
    {
        var trend = TrendOf(rawGenre);
        var before = trend.PlayerInfluence;
        trend.PlayerInfluence = Math.Clamp(before + delta, 0, TrendRules.MaxPlayerInfluence);
        if ((int)Math.Floor(trend.PlayerInfluence * 10 + 1e-9) > (int)Math.Floor(before * 10 + 1e-9))
        {
            Emit(EventType.GenreTrendShifted,
                $"{GenreLabel(trend.Genre)} is catching on, and critics point at Studio Aki (influence {trend.PlayerInfluence:0.0}).");
        }
    }

    /// <summary>Layer 2 noise and booms plus the layer 3 monthly decay, once per calendar month.</summary>
    private void UpdateTrendsMonthly()
    {
        var now = Clock.Now;
        foreach (var trend in Trends)
        {
            if (trend.Genre != TrendCatalog.OtherGenre)
            {
                trend.Noise = Math.Clamp(trend.Noise + Rng.NextDouble(-0.02, 0.02) - 0.1 * trend.Noise,
                    -TrendRules.MaxNoise, TrendRules.MaxNoise);
                UpdateBoom(trend, now);
            }

            var serializedInGenre = Series.Any(s => s.IsSerialized && !s.IsIconic &&
                                                    TrendRules.Normalise(s.Genre, TrendData) == trend.Genre);
            if (!serializedInGenre) trend.PlayerInfluence = Math.Max(0, trend.PlayerInfluence - 0.005);
        }
    }

    private void UpdateBoom(GenreTrend trend, DateTime now)
    {
        var label = GenreLabel(trend.Genre);
        if (trend.BoomEndsAt is null)
        {
            if (Rng.NextDouble() >= 0.01) return;
            trend.BoomPeak = Math.Min(1.0, trend.Boom + Rng.NextDouble(0.3, 0.5));
            trend.BoomEndsAt = now.AddYears(Rng.NextInt(1, 3));
            trend.BoomFadeEndsAt = trend.BoomEndsAt.Value.AddYears(1);
            trend.BoomFading = false;
            trend.BoomFloor = 0;
            trend.Boom = trend.BoomPeak;
            Emit(EventType.GenreTrendShifted, $"{label} is having a moment.");
            return;
        }

        if (now < trend.BoomEndsAt)
        {
            trend.Boom = trend.BoomPeak;
        }
        else if (now < trend.BoomFadeEndsAt)
        {
            if (!trend.BoomFading)
            {
                trend.BoomFading = true;
                trend.BoomFloor = Rng.NextDouble() < 0.25 ? trend.BoomPeak / 2 : 0;
                Emit(EventType.GenreTrendShifted, $"The {trend.Genre} boom is cooling.");
            }
            var progress = (now - trend.BoomEndsAt.Value).TotalDays / (trend.BoomFadeEndsAt!.Value - trend.BoomEndsAt.Value).TotalDays;
            trend.Boom = trend.BoomPeak + (trend.BoomFloor - trend.BoomPeak) * Math.Clamp(progress, 0, 1);
        }
        else
        {
            trend.Boom = trend.BoomFloor;
            trend.BoomPeak = 0;
            trend.BoomFloor = 0;
            trend.BoomFading = false;
            trend.BoomEndsAt = null;
            trend.BoomFadeEndsAt = null;
        }
    }
}
