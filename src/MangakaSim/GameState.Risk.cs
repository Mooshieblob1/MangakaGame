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

    /// <summary>At risk when any assignee's share of the remaining work exceeds their regular hours before the due date.</summary>
    internal bool ComputeAtRisk(Chapter chapter)
    {
        foreach (var (person, remaining) in RemainingPersonHoursByPerson(chapter))
        {
            if (remaining <= 0) continue;
            if (remaining > RegularHoursBefore(person, Clock.Now, chapter.DueDate)) return true;
        }
        return false;
    }

    /// <summary>Person assigned to the first unfinished stage; falls back to the series lead.</summary>
    internal Person AssigneeOf(Chapter chapter)
    {
        var firstOpen = chapter.Stages.FirstOrDefault(s => !s.IsDone);
        return (firstOpen?.AssignedTo is { } id ? FindPerson(id) : null) ?? LeadOf(chapter);
    }

    internal Person LeadOf(Chapter chapter) => FindPerson(SeriesOf(chapter).LeadId) ?? Mangaka;

    internal double RemainingPersonHours(Chapter chapter) => RemainingPersonHoursByPerson(chapter).Sum(kv => kv.Value);

    /// <summary>Remaining person-hours per assignee; unassigned stages fall to the lead.</summary>
    internal Dictionary<Person, double> RemainingPersonHoursByPerson(Chapter chapter)
    {
        var lead = LeadOf(chapter);
        var result = new Dictionary<Person, double>();
        foreach (var work in chapter.Stages.Where(s => !s.IsDone))
        {
            var person = (work.AssignedTo is { } id ? FindPerson(id) : null) ?? lead;
            var multiplier = Settings.Balance.SkillMultiplier(person.Skill(work.Stage));
            result[person] = result.GetValueOrDefault(person) + Math.Max(0, work.HoursRequired - work.HoursDone) / multiplier;
        }
        return result;
    }

    /// <summary>Regular scheduled hours whose start lies in [from, until). Overtime is not counted.</summary>
    internal int RegularHoursBefore(Person person, DateTime from, DateTime until)
    {
        var count = 0;
        for (var hour = from; hour < until; hour = hour.AddHours(1))
        {
            if (IsRegularHour(person, hour)) count++;
        }
        return count;
    }
}
