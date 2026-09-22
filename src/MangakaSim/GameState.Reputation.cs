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
            foreach (var (id, h) in work.HoursByPerson)
            {
                if (h > 0) hours[id] = hours.GetValueOrDefault(id) + h;
            }
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
    public List<DateTime> LiveStrikes(Series series, Magazine magazine) =>
        CancellationRules.LiveStrikes(series.Strikes, Clock.Now, magazine.CadenceDays,
            CancellationRules.StrikeLifetime(ProtectionOf(series)));

    /// <summary>Runs at issue close for a non-Iconic Serialized series; rank is null when it missed the issue.</summary>
    private void ApplyCancellationRule(Series series, Magazine magazine, int? rank)
    {
        var protection = ProtectionOf(series);
        var inGrace = series.Contract!.ChaptersPublished <= ReputationRules.GraceChapters;
        series.Strikes = LiveStrikes(series, magazine);

        if (rank is null)
        {
            // A missed issue neither counts below the line nor clears the streak.
        }
        else if (rank <= magazine.CancellationRank)
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

    // ---------------------------------------------------------------- withdraw and end

    private void ApplyWithdrawSeries(WithdrawSeriesCommand c)
    {
        var series = RequireSeries(c.SeriesId);
        if (!series.IsSerialized)
            throw new InvalidCommandException($"Series '{series.Title}' is not serialized; there is nothing to withdraw from.");
        var magazine = Publishers.Require(series.Contract!.MagazineId);

        EndSerialization(series, dropOpenChapter: false);
        if (!series.IsIconic) series.Fanbase *= FanbaseRules.WithdrawChurn;
        foreach (var open in series.Chapters.Where(ch => ch.Status != ChapterStatus.Complete))
        {
            open.Editor = EditorStatus.NotRequired;
            open.EditorDecisionAt = null;
            open.DueDate = CadenceRules.NextDue(Clock.Now, series.Cadence);
        }
        series.PitchCooldowns[magazine.Id] = Clock.Now.AddDays(7 * 52);
        var penalty = ReputationRules.WithdrawPenalty(series.ChaptersPublished);
        AdjustTrackRecord(penalty);
        Emit(EventType.SeriesWithdrawn,
            $"{series.Title} leaves {magazine.Name} after {series.ChaptersPublished} chapters and goes back to doujin work.",
            new EventContext(SeriesId: series.Id, MagazineId: magazine.Id));
        TryCreateDoujinVolume(series);
    }

    private void ApplyEndSeries(EndSeriesCommand c)
    {
        var series = RequireSeries(c.SeriesId);
        if (series.Status == SeriesStatus.Ended)
            throw new InvalidCommandException($"Series '{series.Title}' has already ended.");

        string outcome;
        if (series.IsSerialized)
        {
            var magazine = Publishers.Require(series.Contract!.MagazineId);
            if (series.ChaptersPublished >= ReputationRules.ProperEndingChapters)
            {
                var ranked = series.Chapters.Where(ch => ch.Rank is not null).Select(ch => (double)ch.Rank!.Value).ToList();
                var averageRank = ranked.Count > 0 ? ranked.Average() : magazine.CancellationRank;
                var totalCopies = series.Volumes.Sum(v => v.CopiesSold);
                var bonus = ReputationRules.EndingBonus(magazine.CancellationRank, averageRank, totalCopies);
                AdjustTrackRecord(bonus);
                ApplyToContributors(series, bonus);
                outcome = $"a proper ending after {series.ChaptersPublished} chapters (+{bonus:0.0} track record)";
            }
            else
            {
                AdjustTrackRecord(ReputationRules.WithdrawPenalty(series.ChaptersPublished));
                outcome = $"an early end after {series.ChaptersPublished} chapters; {magazine.Name} is not pleased";
            }
            EndSerialization(series, dropOpenChapter: true);
            series.Status = SeriesStatus.Ended;
            ScheduleFinalVolume(series, magazine);
        }
        else
        {
            EndSerialization(series, dropOpenChapter: true);
            series.Status = SeriesStatus.Ended;
            outcome = "the doujin run ends quietly";
        }
        Emit(EventType.SeriesEnded, $"{series.Title} ends: {outcome}.", new EventContext(SeriesId: series.Id));
    }
}
