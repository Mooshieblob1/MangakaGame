namespace MangakaSim;

public partial class GameState
{
    /// <summary>Index into Events where the events for the next recap begin.</summary>
    public int RecapWindowStart { get; set; }

    /// <summary>Index into Ledger where the entries for the next recap begin.</summary>
    public int LedgerWindowStart { get; set; }

    internal void DayEndStep()
    {
        if (RecapFiredToday) return;
        if (!People.Any(p => p.HoursWorkedToday > 0)) return;
        if (People.Any(HasWorkingHourLeftToday)) return;
        EmitDailyRecap();
    }

    /// <summary>True if any hour from Clock.Now to the end of TickStart's date is a working hour.</summary>
    internal bool HasWorkingHourLeftToday(Person person)
    {
        var today = TickStart.Date;
        for (var hour = Clock.Now; hour.Date == today; hour = hour.AddHours(1))
        {
            if (IsWorkingHour(person, hour, out _)) return true;
        }
        return false;
    }

    internal GameEvent EmitDailyRecap()
    {
        var window = Events.Skip(RecapWindowStart).ToList();
        var payload = new DailyRecapPayload
        {
            StagesStarted = window.Where(e => e.Type == EventType.StageStarted)
                .Select(e => new StageRef(e.SeriesId!.Value, e.ChapterNumber!.Value, e.Stage!.Value)).ToList(),
            StagesCompleted = window.Where(e => e.Type == EventType.StageCompleted)
                .Select(e => new StageRef(e.SeriesId!.Value, e.ChapterNumber!.Value, e.Stage!.Value)).ToList(),
            HoursPerPerson = People.Select(p => new PersonHours(p.Id, p.Name, p.HoursWorkedToday, p.OvertimeHoursToday)).ToList(),
            ChaptersAtRisk = Series.Where(s => s.Status == SeriesStatus.Active)
                .SelectMany(s => s.Chapters.Where(c => c.IsAtRisk).Select(c => new ChapterRef(s.Id, c.Number))).ToList(),
            ChaptersCompleted = window.Where(e => e.Type == EventType.ChapterCompleted)
                .Select(e => new ChapterRef(e.SeriesId!.Value, e.ChapterNumber!.Value)).ToList(),
            DeadlinesMissed = window.Where(e => e.Type == EventType.DeadlineMissed)
                .Select(e => new ChapterRef(e.SeriesId!.Value, e.ChapterNumber!.Value)).ToList(),
            YenEarned = Ledger.Skip(LedgerWindowStart).Where(l => l.Amount > 0).Sum(l => l.Amount),
            YenSpent = -Ledger.Skip(LedgerWindowStart).Where(l => l.Amount < 0).Sum(l => l.Amount),
            Moods = People.Select(p => new PersonMood(p.Id, p.Name, p.Happiness, p.Fatigue, p.BreaksToday, p.IsMoonlighting)).ToList(),
            ChaptersPublished = window.Count(e => e.Type == EventType.ChapterPublished),
            IssuesMissed = window.Count(e => e.Type == EventType.IssueMissed),
        };

        var hours = string.Join(", ", payload.HoursPerPerson.Where(h => h.Hours > 0)
            .Select(h => h.OvertimeHours > 0 ? $"{h.Name} {h.Hours}h ({h.OvertimeHours}h overtime)" : $"{h.Name} {h.Hours}h"));
        var message = $"Day done. {hours}. Stages completed: {payload.StagesCompleted.Count}. " +
                      $"Chapters completed: {payload.ChaptersCompleted.Count}. At risk: {payload.ChaptersAtRisk.Count}.";

        var ev = Emit(EventType.DailyRecap, message);
        ev.Recap = payload;
        RecapFiredToday = true;
        RecapWindowStart = Events.Count;
        LedgerWindowStart = Ledger.Count;
        return ev;
    }

    /// <summary>
    /// Hours until the next regular scheduled hour of any person, counting from Clock.Now.
    /// Returns 0 when the hour starting now is already scheduled, or when nobody has any scheduled day.
    /// Overtime is ignored.
    /// </summary>
    public int HoursUntilNextWork()
    {
        const int limit = 24 * 8;
        for (var n = 0; n < limit; n++)
        {
            var hour = Clock.Now.AddHours(n);
            if (People.Any(p => IsRegularHour(p, hour))) return n;
        }
        return 0;
    }
}
