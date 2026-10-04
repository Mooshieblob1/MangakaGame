namespace MangakaSim;

/// <summary>Turns new game events into the big moments that may cut into the music (spec 2026-09-28).</summary>
public static class MusicMoments
{
    public static IReadOnlyList<MusicMoment> Classify(GameState state, IEnumerable<GameEvent> fresh, IEnumerable<string> milestones)
    {
        var moments = new HashSet<MusicMoment>();
        Series? Owned(GameEvent e) => e.SeriesId is { } id && state.FindSeries(id) is { } s && s.BusinessId == state.ControlledBusinessId ? s : null;
        foreach (var e in fresh)
            switch (e.Type)
            {
                case EventType.SerializationOffered or EventType.OfferAccepted when Owned(e) is not null:
                    moments.Add(MusicMoment.GoodNews); break;
                case EventType.AwardResult when (Owned(e) is not null || e.PersonId == state.ProtagonistPersonId) &&
                    state.Progression.Awards.Any(a => a.SeriesId == e.SeriesId && a.ResolvedAt == e.Time && a.Prize > 0):
                    moments.Add(MusicMoment.GoodNews); break;
                case EventType.SeriesCancelled or EventType.PitchRejected when Owned(e) is not null:
                    moments.Add(MusicMoment.Setback); break;
                case EventType.GoalChapterCompleted:
                    moments.Add(MusicMoment.GoodNews); break;
                case EventType.ChapterAtRisk when Owned(e)?.Contract is not null:
                    moments.Add(MusicMoment.Deadline); break;
            }
        if (milestones.Contains("first-sale")) moments.Add(MusicMoment.GoodNews);
        if (state.Bookings.Any(b => !b.Cancelled && b.BusinessId == state.ControlledBusinessId &&
                b.PersonId == state.ProtagonistPersonId && b.Date == state.Clock.Now.Date))
            moments.Add(MusicMoment.Convention);
        return moments.OrderByDescending(m => m).ToList();
    }
}
