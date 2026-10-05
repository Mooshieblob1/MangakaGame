namespace MangakaSim;

// Career achievements added for Steam (Q69, 2026-10-05). Each is a daily check on the save itself, so older saves earn
// what they already reached on their next day. They are quiet in the inbox: the goals board and Steam's own pop-up
// already mark these moments.
public partial class GameState
{
    private void CareerAchievementStep()
    {
        var mine = Series.Where(s => s.LeadPersonId == ProtagonistPersonId).ToArray();
        var me = ProtagonistPersonId;
        void Career(string key, Func<bool> reached, string text) { if (!Marked(key, me) && reached()) MarkMilestone(key, me, text, announce: false); }

        Career("first_sale", () => mine.Any(s => SeriesCopiesSold(s.Id) > 0), "First copy sold.");
        Career("first_convention", () => Bookings.Any(b => b.BusinessId == ControlledBusinessId && b.Settled && !b.Cancelled && b.CopiesSold > 0),
            "First copies sold at a convention.");
        Career("serialization", () => mine.Any(s => s.Contract is not null || s.PastContracts.Count > 0), "First magazine serialization.");
        Career("deadline_streak", () => LongestOnTimeRun(mine) >= 10, "Ten magazine chapters on time in a row.");
        Career("comeback", () => Comeback(mine), "A new serialization after a cancellation.");
        Career("first_hire", () => GoalStaff >= 1, "First assistant hired.");
        Career("full_team", () => GoalStaff >= 4, "A team of four assistants.");
        Career("move_out", () => GoalWorksFromStudio, "Moved into a studio of your own.");
        Career("incorporate", () => ControlledBusiness.Incorporated, "Business incorporated.");
        Career("second_studio", () => Locations.Count(l => l.BusinessId == ControlledBusinessId && !l.Closed && !l.IsFamilyHome) >= 2, "Two studios at once.");
        Career("copies_1m", () => mine.Sum(s => SeriesCopiesSold(s.Id)) >= 1_000_000, "A million copies sold across your career.");
        Career("overseas_deal", () => World.Channels.Any(a => a.Status == NegotiationStatus.Accepted && mine.Any(s => s.Id == a.SeriesId)),
            "First digital or overseas deal.");
        Career("ten_years", () => Clock.Now >= GameClock.Start.AddYears(10), "Ten years of making manga.");
        foreach (var s in mine)
        {
            if (!Marked("ranking_top3", s.Id) && s.Chapters.Any(c => c.Rank is <= 3)) MarkMilestone("ranking_top3", s.Id, $"{s.Title} reached the top three.", announce: false);
            if (!Marked("copies_100k", s.Id) && SeriesCopiesSold(s.Id) >= 100_000) MarkMilestone("copies_100k", s.Id, $"{s.Title} has sold 100,000 copies.", announce: false);
        }
    }

    private bool Marked(string key, int entity) => Progression.Milestones.Any(m => m.Key == key && m.Entity == entity);

    /// <summary>Longest run of magazine chapters published without a missed deadline or a missed issue, across the creator's series.</summary>
    internal int LongestOnTimeRun(IReadOnlyCollection<Series> mine)
    {
        int run = 0, best = 0;
        foreach (var e in Events)
        {
            if (e.Type is not (EventType.ChapterPublished or EventType.IssueMissed) || mine.FirstOrDefault(s => s.Id == e.SeriesId) is not { } s) continue;
            var onTime = e.Type == EventType.ChapterPublished && s.Chapters.FirstOrDefault(c => c.Number == e.ChapterNumber) is { IsLate: false };
            run = onTime ? run + 1 : 0;
            best = Math.Max(best, run);
        }
        return best;
    }

    private bool Comeback(IReadOnlyCollection<Series> mine)
    {
        var cancelled = Events.Where(e => e.Type == EventType.SeriesCancelled && mine.Any(s => s.Id == e.SeriesId)).Select(e => e.Time).DefaultIfEmpty(DateTime.MaxValue).Min();
        return mine.SelectMany(s => s.PastContracts.Append(s.Contract)).Any(c => c is not null && c.SignedAt > cancelled);
    }
}
