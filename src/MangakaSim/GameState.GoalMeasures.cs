namespace MangakaSim;

// What each career goal measures (spec 2026-10-01). Internal, so saves never serialize them.
public partial class GameState
{
    internal IEnumerable<Series> GoalTitles => Series.Where(s => s.BusinessId == ControlledBusinessId);
    internal bool GoalTitle(int? seriesId) => seriesId is { } id && FindSeries(id)?.BusinessId == ControlledBusinessId;

    internal static GoalProgress GoalCount(double have, double need) => new(have, need, $"{Math.Min(have, need):N0} of {need:N0}");
    internal static GoalProgress GoalYen(double have, double need) => new(have, need, $"¥{Math.Min(have, need):N0} of ¥{need:N0}");
    internal static GoalProgress GoalFlag(bool done, string waiting) => new(done ? 1 : 0, 1, done ? "Done" : waiting);

    internal bool GoalDoujinFinished => GoalTitles.Any(s => s.Volumes.Any(v => v.IsDoujin));
    internal long GoalCopiesSold => GoalTitles.Sum(s => SeriesCopiesSold(s.Id));
    internal int GoalConventions => Bookings.Count(b => b.BusinessId == ControlledBusinessId && b.Settled && !b.Cancelled && b.StaffedHours > 0);
    internal double GoalFans => Math.Floor(GoalTitles.Sum(s => s.Fanbase));
    internal long GoalOwnSales => Ledger.Where(e => e.Amount > 0 && e.Kind == AccountEntryKind.Publishing &&
        (e.Reason == "doujin sales" || e.Reason == "domestic digital receipts" && e.SeriesId is { } id && FindSeries(id)?.Volumes.Any(v => v.IsDoujin) == true)).Sum(e => e.Amount);
    internal bool GoalPitchedOrEntered => Events.Any(e => e.Type == EventType.PitchSubmitted && GoalTitle(e.SeriesId)) ||
        Progression.Awards.Any(a => a.ManuscriptId > 0 && GoalTitle(a.SeriesId));
    internal bool GoalVerdict => Events.Any(e => e.Type is EventType.PitchRejected or EventType.SerializationOffered && GoalTitle(e.SeriesId));
    internal bool GoalMilestone(string key) => Progression.Milestones.Any(m => m.Key == key);
    internal bool GoalSerializedOrPlaced => GoalTitles.Any(s => s.Publishing == PublishingStatus.Serialized || s.PastContracts.Count > 0) || GoalMilestone("contest_placement");
    internal int GoalMagazineChapters => GoalTitles.Sum(s => s.ChaptersPublished);
    internal GoalProgress GoalTopRank(int target)
    {
        var best = GoalTitles.SelectMany(s => s.Chapters).Where(c => c.Rank is not null).Select(c => c.Rank!.Value).DefaultIfEmpty(0).Min();
        return best == 0 ? new(0, 1, "No ranking yet") : new(best <= target ? 1 : 0, 1, $"Best rank so far: #{best}");
    }
    internal bool GoalCollectedVolume => GoalTitles.Any(s => s.Volumes.Any(v => !v.IsDoujin && v.ReleasedAt is not null));
    internal int GoalStaff => ControlledStaff.Count(p => p.Id != ProtagonistPersonId);
    internal double GoalTopReaders => Math.Floor(GoalTitles.Where(s => s.LeadPersonId == ProtagonistPersonId).Select(s => s.Fanbase).DefaultIfEmpty(0).Max());
    internal bool GoalWorksFromStudio => Protagonist.Employment is { } e &&
        Locations.Any(l => l.Id == e.LocationId && l.BusinessId == ControlledBusinessId && !l.IsFamilyHome && l.PropertyOfferId > 0);
    internal int GoalActiveTitles => GoalTitles.Count(s => s.Status == SeriesStatus.Active);
    internal bool GoalShortlisted => Progression.Awards.Any(a => a.Award.StartsWith("annual:", StringComparison.Ordinal) && GoalTitle(a.SeriesId));
    internal int GoalReleasedBooks => GoalTitles.Sum(s => s.Volumes.Count(v => v.ReleasedAt is not null));
    internal GoalProgress GoalMilestoneOr(string key, GoalProgress progress) => GoalMilestone(key) ? new(1, 1, "Done") : progress;
}
