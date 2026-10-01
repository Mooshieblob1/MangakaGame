namespace MangakaSim;

public partial class GameState
{
    /// <summary>Index into Events where the events for the next recap begin.</summary>
    public int RecapWindowStart { get; set; }

    internal void DayEndStep()
    {
        if (RecapFiredToday) return;
        if (!ControlledStaff.Any(p => p.HoursWorkedToday > 0)) return;
        if (ControlledStaff.Any(HasWorkingHourLeftToday)) return;
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
        var window = Events.Skip(RecapWindowStart).Where(e => e.ActivityDate == TickStart.Date && (e.SeriesId is null || FindSeries(e.SeriesId.Value)?.BusinessId == ControlledBusinessId)).ToList();
        var payload = new DailyRecapPayload
        {
            YenEarned = Ledger.Where(e => e.Time.Date == TickStart.Date && e.Amount > 0 && e.Kind == AccountEntryKind.Publishing).Sum(e => e.Amount),
            ChaptersPublished = window.Where(e => e.Type == EventType.ChapterPublished)
                .Select(e => new ChapterRef(e.SeriesId!.Value, e.ChapterNumber!.Value)).ToList(),
            IssuesMissed = window.Where(e => e.Type == EventType.IssueMissed).Select(e => e.SeriesId!.Value).ToList(),
            StagesStarted = window.Where(e => e.Type == EventType.StageStarted)
                .Select(e => new StageRef(e.SeriesId!.Value, e.ChapterNumber!.Value, e.Stage!.Value)).ToList(),
            StagesCompleted = window.Where(e => e.Type == EventType.StageCompleted)
                .Select(e => new StageRef(e.SeriesId!.Value, e.ChapterNumber!.Value, e.Stage!.Value)).ToList(),
            HoursPerPerson = ControlledStaff.Select(p => new PersonHours(p.Id, p.Name, p.HoursWorkedToday, p.OvertimeHoursToday)).ToList(),
            ChaptersAtRisk = Series.Where(s => s.BusinessId == ControlledBusinessId && s.Status == SeriesStatus.Active)
                .SelectMany(s => s.Chapters.Where(c => c.IsAtRisk).Select(c => new ChapterRef(s.Id, c.Number))).ToList(),
            ChaptersCompleted = window.Where(e => e.Type == EventType.ChapterCompleted)
                .Select(e => new ChapterRef(e.SeriesId!.Value, e.ChapterNumber!.Value)).ToList(),
            DeadlinesMissed = window.Where(e => e.Type == EventType.DeadlineMissed)
                .Select(e => new ChapterRef(e.SeriesId!.Value, e.ChapterNumber!.Value)).ToList(),
        };

        var hours = string.Join(", ", payload.HoursPerPerson.Where(h => h.Hours > 0)
            .Select(h => h.OvertimeHours > 0 ? $"{h.Name} {h.Hours}h ({h.OvertimeHours}h overtime)" : $"{h.Name} {h.Hours}h"));
        var message = $"Day done. {hours}. Stages completed: {payload.StagesCompleted.Count}. " +
                      $"Chapters completed: {payload.ChaptersCompleted.Count}. At risk: {payload.ChaptersAtRisk.Count}.";

        var industry = window.Where(e => e.Type is EventType.IndustryNews or EventType.IndustryDecision).Select(e=>e.Message).ToArray();
        if(industry.Length>0) message += "\nIndustry: " + string.Join("\n",industry);
        var ev = Emit(EventType.DailyRecap, message, context: new(ActivityDate: TickStart.Date));
        ev.Recap = payload;
        RecapFiredToday = true;
        RecapWindowStart = Clock.Now.Date > TickStart.Date ? StartOfActivityDate(Clock.Now.Date) : Events.Count;
        return ev;
    }

    private int StartOfActivityDate(DateTime date)
    {
        // At midnight, market events can precede the previous day's recap.
        var index = Events.Count;
        while (index > 0 && Events[index - 1].Time.Date >= date) index--;
        return index;
    }

    /// <summary>
    /// True once a scheduled day's regular hours are over and nobody worked, so no recap will come to end the day.
    /// The presentation begins the night on this, as it does after a recap. Days off are not scheduled days.
    /// </summary>
    public bool IsQuietDayOver()
    {
        if (RecapFiredToday || ControlledStaff.Any(p => p.HoursWorkedToday > 0)) return false;
        var today = Clock.Now.Date;
        var scheduled = false;
        for (var hour = today; hour.Date == today; hour = hour.AddHours(1))
        {
            if (!ControlledStaff.Any(p => p.Schedule.IsRegularHour(hour))) continue;
            if (hour >= Clock.Now) return false;
            scheduled = true;
        }
        return scheduled;
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
            if (ControlledStaff.Any(p => p.Schedule.IsRegularHour(hour))) return n;
        }
        return 0;
    }
}
