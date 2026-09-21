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
        var assignee = AssigneeOf(chapter);
        var remaining = RemainingPersonHours(chapter);
        if (remaining <= 0) return false;
        return remaining > RegularHoursBefore(assignee, Clock.Now, chapter.DueDate);
    }

    /// <summary>Person assigned to the first unfinished stage; falls back to the first person.</summary>
    internal Person AssigneeOf(Chapter chapter)
    {
        var firstOpen = chapter.Stages.FirstOrDefault(s => !s.IsDone);
        return (firstOpen?.AssignedTo is { } id ? FindPerson(id) : null) ?? People[0];
    }

    internal double RemainingPersonHours(Chapter chapter)
    {
        double total = 0;
        foreach (var work in chapter.Stages.Where(s => !s.IsDone))
        {
            var person = (work.AssignedTo is { } id ? FindPerson(id) : null) ?? People[0];
            var multiplier = Settings.Balance.SkillMultiplier(person.Skill(work.Stage));
            total += Math.Max(0, work.HoursRequired - work.HoursDone) / multiplier;
        }
        return total;
    }

    /// <summary>Regular scheduled hours whose start lies in [from, until). Overtime is not counted.</summary>
    internal int RegularHoursBefore(Person person, DateTime from, DateTime until)
    {
        var count = 0;
        for (var hour = from; hour < until; hour = hour.AddHours(1))
        {
            if (person.Schedule.IsRegularHour(hour)) count++;
        }
        return count;
    }
}
