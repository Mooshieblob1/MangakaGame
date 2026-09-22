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

    private void ApplyWithdrawSeries(WithdrawSeriesCommand c) => throw new InvalidCommandException("WithdrawSeries is not available yet.");
    private void ApplyEndSeries(EndSeriesCommand c) => throw new InvalidCommandException("EndSeries is not available yet.");
}
