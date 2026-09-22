using MangakaSim.Catalog;
using MangakaSim.Rules;

namespace MangakaSim;

public partial class GameState
{
    private void InitializeMarket()
    {
        Trends = TrendCatalog.Genres.Select(g => new GenreTrend { Genre = g }).ToList();
        foreach (var magazine in PublisherCatalog.Magazines)
        {
            var market = new MagazineState { MagazineId = magazine.Id, NextIssueClose = IssueSchedule.Anchor(magazine) };
            var (min, max) = magazine.Tier switch { 1 => (40, 95), 2 => (35, 85), _ => (30, 75) };
            for (var slot = 0; slot < magazine.RosterSize - 1; slot++) market.Fillers.Add(NewFiller(magazine, min, max));
            if (magazine.Tier == 1)
            {
                var iconic = market.Fillers.OrderByDescending(f => f.Popularity).ThenBy(f => f.Id).First();
                iconic.IsIconic = true;
                iconic.Popularity = Rng.NextInt(85, 96);
            }
            Markets.Add(market);
        }
    }
    private FillerSeries NewFiller(Magazine magazine, int min, int max)
    {
        // Draw order: adjective, noun, genre, popularity; magazines and slots use catalog order.
        var title = PublisherCatalog.Adjectives[Rng.NextInt(PublisherCatalog.Adjectives.Count)] + " " +
            PublisherCatalog.Nouns[Rng.NextInt(PublisherCatalog.Nouns.Count)];
        var sample = Rng.NextDouble() * TrendCatalog.Genres.Sum(magazine.Affinity);
        var genre = TrendCatalog.Genres[^1];
        foreach (var candidate in TrendCatalog.Genres)
        {
            sample -= magazine.Affinity(candidate);
            if (sample < 0) { genre = candidate; break; }
        }
        return new() { Id = AllocateId(), Title = title, Genre = genre, Popularity = Rng.NextInt(min, max + 1) };
    }
    private void IssueCloseStep(IReadOnlyList<IssueCloseContext> closes)
    {
        foreach (var close in closes)
        {
            var magazine = PublisherCatalog.Get(close.MagazineId);
            var market = Markets.Single(m => m.MagazineId == magazine.Id);
            var competitors = Series.Where(s => s.Publishing == PublishingStatus.Serialized &&
                s.Contract!.MagazineId == magazine.Id && s.Contract.FirstIssueClose <= close.CloseTime).OrderBy(s => s.Id).ToArray();
            var grace = competitors.ToDictionary(s => s.Id, s => s.Contract!.ChaptersPublished < 8);
            var published = new Dictionary<int, Chapter>();
            foreach (var series in competitors)
            {
                var chapter = series.Chapters.FirstOrDefault(c => !c.IsOneShot && !c.DoujinEligible &&
                    c.PublishedAt is null && c.DueDate == close.CloseTime);
                series.LastRank = null;
                if (chapter is { Status: ChapterStatus.Complete, Editor: EditorStatus.Approved })
                {
                    var contract = series.Contract!;
                    chapter.PublishedAt = Clock.Now;
                    chapter.PublishedMagazineId = magazine.Id;
                    chapter.PublishedContractId = contract.Id;
                    series.ChaptersPublished++;
                    contract.ChaptersPublished++;
                    var amount = checked(chapter.Pages * contract.FeePerPage);
                    PostLedger(amount, "chapter fee", series.Id);
                    Emit(EventType.ChapterPublished, $"{series.Title} ch.{chapter.Number} published in {magazine.Name}.", series.Id, chapter.Number,
                        context: new(MagazineId: magazine.Id, Amount: amount));
                    published[series.Id] = chapter;
                    CollectCommercial(series, magazine.ChaptersPerVolume);
                }
                else
                {
                    if (!grace[series.Id] && !series.IsIconic) series.Strikes.Add(Clock.Now);
                    if (!series.IsIconic) series.Fanbase *= .97;
                    ChangeTrackRecord(-1);
                    var waiting = series.Chapters.Where(c => !c.IsOneShot && !c.DoujinEligible && c.PublishedAt is null &&
                        c.DueDate >= close.CloseTime).OrderBy(c => c.DueDate).ToArray();
                    foreach (var buffered in waiting) buffered.DueDate = IssueSchedule.AddIssues(magazine, buffered.DueDate, 1);
                    if (waiting.Length == 0) series.NextChapterDueOverride = IssueSchedule.AddIssues(magazine, close.CloseTime, 1);
                    Emit(EventType.IssueMissed, $"{series.Title} missed {magazine.Name}'s issue.", series.Id, chapter?.Number,
                        context: new(MagazineId: magazine.Id));
                }
            }
            var genres = market.Fillers.Select(f => f.Genre).Concat(competitors.Where(s => published.ContainsKey(s.Id))
                .Select(s => TrendRules.Normalise(s.Genre, TrendCatalog))).GroupBy(g => g).ToDictionary(g => g.Key, g => g.Count());
            var rows = market.Fillers.Select(f => new RankEntry(0, f.Title, null, f.Id,
                f.Popularity * TrendRules.Crowding(genres[f.Genre] - 1))).ToList();
            foreach (var series in competitors.Where(s => published.ContainsKey(s.Id)))
            {
                var genre = TrendRules.Normalise(series.Genre, TrendCatalog);
                rows.Add(new(0, series.Title, series.Id, null, RankingRules.Score(published[series.Id].Quality!.Value,
                    series.Fanbase, magazine.Tier, series.IsIconic ? 1 : magazine.Affinity(genre),
                    series.IsIconic ? 1 : GenrePopularity(genre), genres[genre] - 1)));
            }
            market.LastRanking = rows.OrderByDescending(r => r.Score).ThenBy(r => r.FillerId is null ? 1 : 0)
                .ThenBy(r => r.FillerId ?? r.SeriesId).Select((r, i) => r with { Rank = i + 1 }).ToList();
            foreach (var row in market.LastRanking.Where(r => r.SeriesId is not null))
            {
                var series = FindSeries(row.SeriesId!.Value)!;
                var chapter = published[series.Id];
                series.LastRank = chapter.Rank = row.Rank;
                series.Fanbase = FanbaseRules.AfterPublication(series.Fanbase, magazine.Tier, chapter.Quality!.Value,
                    row.Rank, magazine.CancellationRank, magazine.RosterSize, series.IsIconic);
                CheckIconic(series);
                series.CulturalImpact = Math.Min(100, series.CulturalImpact + .05);
                CheckIconic(series);
                if (row.Rank <= 3 && !series.IsIconic)
                {
                    series.CulturalImpact = Math.Min(100, series.CulturalImpact + .1);
                    CheckIconic(series);
                    if (!series.IsIconic)
                    {
                        ChangeTrackRecord(row.Rank == 1 ? .5 : .3);
                        AwardShares(ChapterHours(chapter), .5);
                        if (chapter.Quality >= 80) AddInfluence(series, .01);
                    }
                }
            }
            var best = market.LastRanking.Where(r => r.SeriesId is not null).Select(r => (int?)r.Rank).Min();
            Emit(EventType.RankingPublished, $"{magazine.Name} rankings published" + (best is null ? "." : $"; studio best #{best}."),
                context: new(MagazineId: magazine.Id, Rank: best));
            foreach (var series in competitors) CancellationStep(series, magazine, grace[series.Id]);
            foreach (var filler in market.Fillers.OrderBy(f => f.Id).ToArray())
            {
                var rank = market.LastRanking.Single(r => r.FillerId == filler.Id).Rank;
                filler.IssuesBelowLine = rank > magazine.CancellationRank ? filler.IssuesBelowLine + 1 : 0;
                filler.Popularity = Math.Clamp(filler.Popularity + Rng.NextInt(-3, 4), 5, 100);
                if (filler.IsIconic || filler.IssuesBelowLine < 12) continue;
                var index = market.Fillers.IndexOf(filler);
                market.RetiredFillers.Add(filler);
                market.Fillers[index] = NewFiller(magazine, 45, 65);
            }
            market.NextIssueClose = IssueSchedule.AddIssues(magazine, close.CloseTime, 1);
            market.IssuesClosed++;
            MonthlyTrends();
        }
        if (closes.Count > 0) RunPlanner();
    }
    internal void MonthlyTrends()
    {
        var month = new DateTime(Clock.Now.Year, Clock.Now.Month, 1);
        if (LastTrendUpdateMonth == month) return;
        LastTrendUpdateMonth = month;
        foreach (var trend in Trends)
        {
            if (trend.Genre != "other")
            {
                trend.Noise = TrendRules.Noise(trend.Noise, Rng.NextDouble(-.02, .02));
                if (trend.BoomEndsAt is { } ends)
                {
                    if (Clock.Now >= ends && !trend.BoomFloorChosen)
                    {
                        trend.BoomFloorChosen = true;
                        trend.BoomFloor = Rng.NextDouble() < .25 ? trend.BoomPeak / 2 : 0;
                        Emit(EventType.GenreTrendShifted, $"The {trend.Genre} boom is cooling.");
                    }
                    trend.Boom = Math.Min(1, trend.PermanentBoom + (Clock.Now < ends ? trend.BoomPeak :
                        TrendRules.Fade(trend.BoomPeak, trend.BoomFloor, ends, trend.BoomFadeEndsAt!.Value, Clock.Now)));
                    if (Clock.Now >= trend.BoomFadeEndsAt)
                    {
                        trend.PermanentBoom = Math.Min(1, trend.PermanentBoom + trend.BoomFloor);
                        trend.Boom = trend.PermanentBoom;
                        trend.BoomEndsAt = trend.BoomFadeEndsAt = null;
                        trend.BoomPeak = trend.BoomFloor = 0;
                        trend.BoomFloorChosen = false;
                    }
                }
                else if (Rng.NextDouble() < .01)
                {
                    trend.BoomPeak = Rng.NextDouble(.3, .5);
                    trend.BoomEndsAt = Clock.Now.AddYears(Rng.NextInt(1, 3));
                    trend.BoomFadeEndsAt = trend.BoomEndsAt.Value.AddYears(1);
                    trend.Boom = Math.Min(1, trend.PermanentBoom + trend.BoomPeak);
                    Emit(EventType.GenreTrendShifted, $"{trend.Genre} is having a moment.");
                }
            }
            if (!Series.Any(s => s.Publishing == PublishingStatus.Serialized && TrendRules.Normalise(s.Genre, TrendCatalog) == trend.Genre))
                trend.PlayerInfluence = Math.Max(0, trend.PlayerInfluence - .005);
        }
    }
}
