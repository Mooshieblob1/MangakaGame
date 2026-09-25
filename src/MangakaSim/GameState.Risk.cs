namespace MangakaSim;

public partial class GameState
{
    internal void RiskStep()
    {
        foreach (var series in Series.Where(s => s.Status == SeriesStatus.Active))
        {
            foreach (var chapter in series.Chapters.Where(c => c.Status != ChapterStatus.Complete))
            {
                var atRisk = ComputeAtRisk(chapter);
                if (atRisk && !chapter.IsAtRisk)
                {
                    Emit(EventType.ChapterAtRisk,
                        $"{series.Title} ch.{chapter.Number} is at risk of missing its deadline ({chapter.DueDate:ddd d MMM HH:mm}).",
                        seriesId: series.Id, chapterNumber: chapter.Number);
                }
                chapter.IsAtRisk = atRisk;
            }
        }
    }

    internal bool ComputeAtRisk(Chapter chapter)
    {
        if(!HasPublisherDeadline(chapter))return false;
        var ready = Clock.Now;
        var available = People.ToDictionary(p => p.Id, _ => Clock.Now);
        // Earlier deadlines consume each worker's capacity before this chapter.
        foreach (var other in Series.Where(s => s.Status == SeriesStatus.Active).SelectMany(s => s.Chapters)
                     .Where(c => HasPublisherDeadline(c) && c.Status != ChapterStatus.Complete && (c.DueDate < chapter.DueDate || c.DueDate == chapter.DueDate && c.Id <= chapter.Id))
                     .OrderBy(c => c.DueDate).ThenBy(c => c.Id))
        {
            ready = Clock.Now;
            foreach (var work in other.Stages.Where(w => !w.IsDone))
            {
                var p = work.AssignedTo is { } id ? FindPerson(id) : null;
                if (p is null || !CanProduce(p)) { ready = other.DueDate.AddHours(1); break; }
                var start = available[p.Id] > ready ? available[p.Id] : ready;
                if (p.BusyUntil > start) start = p.BusyUntil.Value;
                if (work.Stage != Stage.Name && other.EditorDecisionAt > start) start = other.EditorDecisionAt.Value;
                var hours = (int)Math.Ceiling(Math.Max(0,work.HoursRequired-work.HoursDone)/Settings.Balance.SkillMultiplier(p.Skill(work.Stage)));
                var limit = start.AddDays(366);
                while (hours > 0 && start < limit)
                {
                    var booked = Bookings.Any(b => !b.Cancelled && !b.Settled && (b.PersonId == p.Id || b.AssistantId == p.Id) &&
                        start.Date >= b.Date && start.Date < b.Date.AddDays(b.Scale == 2 ? 2 : 1) && start.Hour >= 11-b.TravelHours && start.Hour < 16+b.TravelHours);
                    if (p.Schedule.IsRegularHour(start) && !AtOutsideJob(p,start) && start.Hour != p.Schedule.WorkStartHour+4 && !booked) hours--;
                    start = start.AddHours(1);
                }
                available[p.Id] = ready = start;
            }
            if (other.Id == chapter.Id) return ready > chapter.DueDate;
        }
        return false;
    }

    /// <summary>Person assigned to the first unfinished stage; falls back to the first person.</summary>
    internal Person AssigneeOf(Chapter chapter)
    {
        var firstOpen = chapter.Stages.FirstOrDefault(s => !s.IsDone);
        return (firstOpen?.AssignedTo is { } id ? FindPerson(id) : null) ?? FindPerson(SeriesOf(chapter).LeadPersonId)!;
    }

    internal double RemainingPersonHours(Chapter chapter)
    {
        double total = 0;
        foreach (var work in chapter.Stages.Where(s => !s.IsDone))
        {
            var person = (work.AssignedTo is { } id ? FindPerson(id) : null) ?? FindPerson(SeriesOf(chapter).LeadPersonId)!;
            var multiplier = Settings.Balance.SkillMultiplier(person.Skill(work.Stage));
            total += Math.Max(0, work.HoursRequired - work.HoursDone) / multiplier;
        }
        return total;
    }

    /// <summary>Regular scheduled hours whose start lies in [from, until). Overtime is not counted.</summary>
    internal int RegularHoursBefore(Person person, DateTime from, DateTime until)
    {
        if (until <= from) return 0;
        // Buffered serialized chapters can be months ahead. Whole weeks repeat
        // the same schedule; only the remaining partial week needs scanning.
        var weeks = (int)((until - from).TotalHours / (24 * 7));
        var count = weeks * (7 - person.Schedule.DaysOff.Count) *
            (person.Schedule.WorkEndHour - person.Schedule.WorkStartHour);
        for (var hour = from.AddDays(weeks * 7); hour < until; hour = hour.AddHours(1))
        {
            if (person.Schedule.IsRegularHour(hour)) count++;
        }
        return count;
    }
}
