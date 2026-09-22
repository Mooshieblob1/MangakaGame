namespace MangakaSim;

public partial class GameState
{
    // ---------------------------------------------------------------- reputation bookkeeping

    internal void AdjustTrackRecord(double delta) =>
        StudioTrackRecord = ReputationRules.Clamp(StudioTrackRecord + delta);

    internal void AdjustReputation(Person person, double delta) =>
        person.Reputation = ReputationRules.Clamp(person.Reputation + delta);

    /// <summary>0.5 x studio track record + 0.5 x the weighted top-three staff reputation.</summary>
    public double EffectiveReputation =>
        ReputationRules.Effective(StudioTrackRecord, ReputationRules.StaffTerm(People.Select(p => p.Reputation)));

    public double StaffTerm => ReputationRules.StaffTerm(People.Select(p => p.Reputation));

    /// <summary>Share of a chapter's worked hours per person, by stage assignee. Empty when nothing was worked.</summary>
    internal Dictionary<int, double> HourShares(Chapter chapter) => HourShares(new[] { chapter });

    internal Dictionary<int, double> HourShares(IEnumerable<Chapter> chapters)
    {
        var hours = new Dictionary<int, double>();
        foreach (var work in chapters.SelectMany(c => c.Stages))
        {
            if (work.AssignedTo is not { } id || work.HoursDone <= 0) continue;
            hours[id] = hours.GetValueOrDefault(id) + work.HoursDone;
        }
        var total = hours.Values.Sum();
        return total <= 0
            ? new Dictionary<int, double>()
            : hours.ToDictionary(kv => kv.Key, kv => kv.Value / total);
    }

    /// <summary>Contributors gain or lose (quality - 60) / 20 x hour share; doujin chapters count half.</summary>
    private void ApplyChapterCompletionReputation(Series series, Chapter chapter)
    {
        var doujin = !series.IsSerialized && !chapter.IsOneShot;
        foreach (var (personId, share) in HourShares(chapter))
        {
            if (FindPerson(personId) is { } person)
                AdjustReputation(person, ReputationRules.ChapterCompletionDelta(chapter.Quality ?? 0, share, doujin));
        }
    }

    /// <summary>Applies a delta to everyone who worked on the series, split by lifetime hours.</summary>
    internal void ApplyToContributors(Series series, double totalDelta)
    {
        foreach (var (personId, share) in HourShares(series.Chapters))
        {
            if (FindPerson(personId) is { } person) AdjustReputation(person, totalDelta * share);
        }
    }

    /// <summary>Applies a flat delta to every person who worked on the series.</summary>
    internal void ApplyFlatToContributors(Series series, double delta)
    {
        foreach (var personId in HourShares(series.Chapters).Keys)
        {
            if (FindPerson(personId) is { } person) AdjustReputation(person, delta);
        }
    }

    // ---------------------------------------------------------------- protection and cancellation

    internal double ProtectionOf(Series series) =>
        ReputationRules.Protection(series.ChaptersPublished, series.Fanbase, series.CulturalImpact);

    /// <summary>Strikes still inside their lifetime for the series' magazine, oldest first.</summary>
    internal List<DateTime> LiveStrikes(Series series, Magazine magazine) =>
        CancellationRules.LiveStrikes(series.Strikes, Clock.Now, magazine.CadenceDays,
            CancellationRules.StrikeLifetime(ProtectionOf(series)));

    /// <summary>Runs at issue close for a ranked, non-Iconic Serialized series.</summary>
    private void ApplyCancellationRule(Series series, Magazine magazine, int rank)
    {
        var protection = ProtectionOf(series);
        var inGrace = series.Contract!.ChaptersPublished <= ReputationRules.GraceChapters;
        series.Strikes = LiveStrikes(series, magazine);

        if (rank <= magazine.CancellationRank)
        {
            series.WeeksBelowLine = 0;
            if (series.WarningIssuedAt is not null)
            {
                series.WarningIssuedAt = null;
                Emit(EventType.CancellationWarningLifted,
                    $"{series.Title} climbs back above the line at #{rank}; {magazine.Name} withdraws its warning.",
                    new EventContext(SeriesId: series.Id, MagazineId: magazine.Id, Rank: rank));
            }
        }
        else if (!inGrace)
        {
            series.WeeksBelowLine++;
            if (series.WarningIssuedAt is null && series.WeeksBelowLine >= CancellationRules.WarningClock(protection))
            {
                series.WarningIssuedAt = Clock.Now;
                Emit(EventType.CancellationWarning,
                    $"{magazine.Name} warns {series.Title}: {series.WeeksBelowLine} issues below the line at #{rank}. Climb back or face cancellation.",
                    new EventContext(SeriesId: series.Id, MagazineId: magazine.Id, Rank: rank));
            }
        }

        var issuesUnderWarning = series.WarningIssuedAt is { } warned
            ? (int)Math.Floor((Clock.Now - warned).TotalDays / magazine.CadenceDays)
            : -1;
        var rollDue = (series.WarningIssuedAt is not null && issuesUnderWarning >= CancellationRules.CancelClock(protection)) ||
                      series.Strikes.Count >= CancellationRules.StrikesForRoll;
        if (!rollDue) return;

        var chance = CancellationRules.CancelChance(EffectiveReputation);
        if (Rng.NextDouble() < chance)
        {
            CancelSeries(series, magazine);
            return;
        }
        series.WeeksBelowLine /= 2;
        var keep = series.Strikes.Count / 2;
        series.Strikes = series.Strikes.OrderBy(s => s).TakeLast(keep).ToList();
        if (series.WarningIssuedAt is not null) series.WarningIssuedAt = Clock.Now;
        Emit(EventType.CancellationSurvived,
            $"{magazine.Name} keeps {series.Title} on for now, but the editors are watching.",
            new EventContext(SeriesId: series.Id, MagazineId: magazine.Id, Rank: rank));
    }

    private void CancelSeries(Series series, Magazine magazine)
    {
        var contract = series.Contract!;
        Emit(EventType.SeriesCancelled,
            $"{magazine.Name} cancels {series.Title} after {contract.ChaptersPublished} chapters.",
            new EventContext(SeriesId: series.Id, MagazineId: magazine.Id));
        ApplyFlatToContributors(series, ReputationRules.PersonCancellation);
        AdjustTrackRecord(ReputationRules.Cancellation);
        series.PitchCooldowns[magazine.Id] = Clock.Now.AddDays(7 * 52);
        EndSerialization(series, dropOpenChapter: true);
        series.Status = SeriesStatus.Ended;
        ScheduleFinalVolume(series, magazine);
        RunPlanner();
    }

    /// <summary>Clears the contract and publishing state; optionally drops the open chapter.</summary>
    private void EndSerialization(Series series, bool dropOpenChapter)
    {
        series.Contract = null;
        series.PendingOffer = null;
        series.Publishing = PublishingStatus.Unpublished;
        series.Strikes.Clear();
        series.WeeksBelowLine = 0;
        series.WarningIssuedAt = null;
        if (dropOpenChapter)
        {
            foreach (var open in series.Chapters.Where(c => c.Status != ChapterStatus.Complete).ToList())
                series.Chapters.Remove(open);
        }
    }

    private void ApplyWithdrawSeries(WithdrawSeriesCommand c) => throw new InvalidCommandException("WithdrawSeries is not available yet.");
    private void ApplyEndSeries(EndSeriesCommand c) => throw new InvalidCommandException("EndSeries is not available yet.");
}
