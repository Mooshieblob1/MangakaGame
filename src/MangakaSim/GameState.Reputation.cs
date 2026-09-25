using MangakaSim.Catalog;
using MangakaSim.Rules;

namespace MangakaSim;

public partial class GameState
{
    private void ChangeTrackRecord(double delta, int? businessId = null)
    { var b = BusinessOf(businessId ?? ControlledBusinessId); b.TrackRecord = Math.Clamp(b.TrackRecord + delta, 0, 100); }
    internal double BusinessReputation(int business) => ReputationRules.Effective(BusinessOf(business).TrackRecord, People.Where(p => p.Employment?.BusinessId == business).Select(p => p.Reputation));
    private static void ChangeReputation(Person person, double delta) => person.Reputation = Math.Clamp(person.Reputation + delta, 0, 100);
    private static Dictionary<int, long> ChapterHours(Chapter chapter) => chapter.Stages.SelectMany(w => w.HoursByPerson)
        .GroupBy(p => p.Key).ToDictionary(g => g.Key, g => g.Sum(p => p.Value));
    private void AwardShares(IReadOnlyDictionary<int, long> hours, double award)
    {
        var total = hours.Values.Sum();
        if (total == 0) return;
        foreach (var pair in hours.OrderBy(p => p.Key)) ChangeReputation(FindPerson(pair.Key)!, award * pair.Value / total);
    }
    private void AwardCompletion(Series series, Chapter chapter) => AwardShares(ChapterHours(chapter),
        ReputationRules.Completion(chapter.Quality!.Value, 1, !chapter.IsOneShot && series.Publishing == PublishingStatus.Unpublished));
    private void CheckIconic(Series series)
    {
        if (series.IsIconic || series.CulturalImpact < 90 || series.Fanbase < 1000000) return;
        series.IsIconic = true;
        ClearCancellation(series);
        Emit(EventType.SeriesBecameIconic, $"{series.Title} has become iconic.", series.Id);
    }
    private void AddInfluence(Series series, double delta)
    {
        if (series.IsIconic) return;
        var trend = Trends.Single(t => t.Genre == TrendRules.Normalise(series.Genre, TrendCatalog));
        var old = trend.PlayerInfluence;
        trend.PlayerInfluence = Math.Clamp(old + delta, 0, .5);
        for (var step = (int)Math.Floor(old * 10 + 1e-9) + 1; step <= (int)Math.Floor(trend.PlayerInfluence * 10 + 1e-9); step++)
            Emit(EventType.GenreTrendShifted, $"{trend.Genre} is catching on; critics point at the studio ({step * 10}%).", series.Id);
    }
    private static void ClearCancellation(Series series)
    {
        series.Strikes.Clear();
        series.WeeksBelowLine = 0;
        series.WarningIssuedAt = null;
    }
    private static void ArchiveContract(Series series)
    {
        if (series.Contract is { } contract) series.PastContracts.Add(contract);
        series.Contract = null;
    }
    private void ApplyWithdraw(WithdrawSeriesCommand command)
    {
        var series = RequireSeries(command.SeriesId);
        if (series.Publishing != PublishingStatus.Serialized || series.Contract is null)
            throw new InvalidCommandException("Only a serialized series can withdraw.");
        var magazine = series.Contract.MagazineId;
        ArchiveContract(series);
        series.Publishing = PublishingStatus.Unpublished;
        series.Cadence = series.DoujinCadence;
        var due = CadenceRules.NextDue(Clock.Now, series.DoujinCadence);
        series.NextChapterDueOverride = due;
        if (!series.IsIconic) series.Fanbase *= .9;
        ClearCancellation(series);
        series.PitchCooldowns[magazine] = Clock.Now.AddDays(52 * 7);
        ChangeTrackRecord(ReputationRules.Withdraw(series.ChaptersPublished), series.BusinessId);
        foreach (var chapter in series.Chapters.Where(c => !c.IsOneShot && c.PublishedAt is null).ToArray())
        {
            chapter.DoujinEligible = true;
            if (chapter.Status == ChapterStatus.Complete) continue;
            chapter.Editor = EditorStatus.NotRequired;
            chapter.EditorMagazineId = null;
            chapter.EditorDecisionAt = null;
            chapter.DueDate = due;
            series.NextChapterDueOverride = null;
            CompleteChapterIfDone(chapter);
        }
        CollectDoujin(series);
        Emit(EventType.SeriesWithdrawn, $"{series.Title} leaves its magazine and returns to doujin.", series.Id, context: new(MagazineId: magazine));
    }
    private void ApplyEnd(EndSeriesCommand command)
    {
        var series = RequireSeries(command.SeriesId);
        if (series.Status == SeriesStatus.Ended) throw new InvalidCommandException("This series has already ended.");
        if (Progression.Manuscripts.Any(m => m.SeriesId == series.Id && !m.Released))
            throw new InvalidCommandException("Release the contest manuscript before ending its title. Active submissions must finish judging first.");
        if (series.Contract is { } contract)
        {
            if (series.ChaptersPublished >= 12)
            {
                var ranks = series.Chapters.Where(c => c.PublishedAt is not null && c.Rank is not null).Select(c => c.Rank!.Value).ToArray();
                var bonus = ReputationRules.Ending(PublisherCatalog.Get(contract.MagazineId).CancellationRank,
                    ranks.Average(), series.Volumes.Sum(v => v.CopiesSold));
                ChangeTrackRecord(bonus, series.BusinessId);
                AwardShares(series.LifetimeHoursByPerson, bonus);
            }
            else ChangeTrackRecord(ReputationRules.Withdraw(series.ChaptersPublished), series.BusinessId);
        }
        FinishSeries(series);
        Emit(EventType.SeriesEnded, $"{series.Title} ended.", series.Id);
    }
    private void FinishSeries(Series series)
    {
        var liveSample = series.Publishing is PublishingStatus.Pitching or PublishingStatus.Offered
            ? series.Chapters.LastOrDefault(c => c.IsOneShot) : null;
        series.Status = SeriesStatus.Ended;
        series.Publishing = PublishingStatus.Unpublished;
        series.PendingOffer = null;
        ArchiveContract(series);
        series.NextChapterDueOverride = null;
        ClearCancellation(series);
        foreach (var chapter in series.Chapters.Where(c => c.Status != ChapterStatus.Complete || c == liveSample).ToArray()) DropChapter(series, chapter);
        CollectCommercial(series, 0, true);
        RunPlanner();
    }
    internal void CancellationStep(Series series, Magazine magazine, bool grace)
    {
        if (series.IsIconic || grace) return;
        var clocks = CancellationRules.Clocks(Protection(series));
        series.Strikes.RemoveAll(time => CancellationRules.StrikeExpired(time, Clock.Now, magazine.Cadence, clocks.StrikeLifetime));
        if (series.LastRank is { } rank)
        {
            if (rank <= magazine.CancellationRank)
            {
                series.WeeksBelowLine = 0;
                if (series.WarningIssuedAt is not null)
                    Emit(EventType.CancellationWarningLifted, $"{series.Title}: cancellation warning lifted.", series.Id, context: new(MagazineId: magazine.Id));
                series.WarningIssuedAt = null;
            }
            else
            {
                series.WeeksBelowLine++;
                if (series.WeeksBelowLine >= clocks.Warning && series.WarningIssuedAt is null)
                {
                    series.WarningIssuedAt = Clock.Now;
                    Emit(EventType.CancellationWarning, $"{series.Title}: rankings put serialization at risk.", series.Id, context: new(MagazineId: magazine.Id));
                }
            }
        }
        if ((DeadlineProtected(series) || series.Strikes.Count < 3) && (series.WarningIssuedAt is not { } warning ||
            CancellationRules.IssueAge(warning, Clock.Now, magazine.Cadence) < clocks.Cancel)) return;
        if (Rng.NextDouble() < CancellationRules.Chance(BusinessReputation(series.BusinessId)))
        {
            ChangeTrackRecord(-8, series.BusinessId);
            foreach (var pair in series.LifetimeHoursByPerson.Where(p => p.Value > 0)) ChangeReputation(FindPerson(pair.Key)!, -2);
            series.PitchCooldowns[magazine.Id] = Clock.Now.AddDays(52 * 7);
            FinishSeries(series);
            Emit(EventType.SeriesCancelled, $"{series.Title} was cancelled.", series.Id, context: new(MagazineId: magazine.Id));
        }
        else
        {
            series.WeeksBelowLine /= 2;
            series.Strikes = series.Strikes.Order().TakeLast(series.Strikes.Count / 2).ToList();
            Emit(EventType.CancellationSurvived, $"{series.Title} gets another chance.", series.Id, context: new(MagazineId: magazine.Id));
        }
    }
}
