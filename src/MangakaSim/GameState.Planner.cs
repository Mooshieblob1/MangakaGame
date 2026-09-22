namespace MangakaSim;

public partial class GameState
{
    internal Series CreateSeries(string title, string genre, Cadence cadence, int pagesPerChapter)
    {
        var series = new Series
        {
            Id = AllocateId(),
            Title = title,
            Genre = genre,
            Cadence = cadence,
            PagesPerChapter = pagesPerChapter,
            Status = SeriesStatus.Active,
            StartDate = Clock.Now,
            LeadId = Mangaka.Id,
        };
        Series.Add(series);
        return series;
    }

    /// <summary>Idempotent. Runs at DayStarted, after every command, and when a chapter completes.</summary>
    internal void RunPlanner()
    {
        EnsureNextChapters();
        AssignStages();
        foreach (var person in People) OrderQueue(person);
    }

    private void EnsureNextChapters()
    {
        foreach (var series in Series.Where(s => s.Status == SeriesStatus.Active))
        {
            var hasOpenChapter = series.Chapters.Any(c => c.Status != ChapterStatus.Complete);
            if (!hasOpenChapter) CreateNextChapter(series);
        }
    }

    internal Chapter CreateNextChapter(Series series) => CreateNextChapter(series, pitchMagazine: null, dueOverride: null);

    /// <summary>
    /// Creates the next chapter. A pitched one-shot has 31 pages and is due at the magazine's first close at least
    /// 14 days out; a Serialized chapter is due at the first close strictly after the previous chapter's due date;
    /// doujin chapters follow the cadence rule.
    /// </summary>
    internal Chapter CreateNextChapter(Series series, Magazine? pitchMagazine, DateTime? dueOverride)
    {
        var previous = series.Chapters.LastOrDefault();
        var number = previous is null ? 1 : previous.Number + 1;
        var oneShot = pitchMagazine is not null;
        var pages = oneShot ? PitchRules.OneShotPages : series.PagesPerChapter;

        DateTime due;
        if (dueOverride is { } forced) due = forced;
        else if (pitchMagazine is { } target)
            due = NextCloseAtOrAfter(target, Clock.Now.AddDays(PitchRules.MinDaysBeforeDue));
        else if (series.Contract is { } contract)
        {
            var from = previous is null ? Clock.Now : (previous.DueDate > Clock.Now ? previous.DueDate : Clock.Now);
            due = NextCloseAfter(Publishers.Require(contract.MagazineId), from);
        }
        else due = CadenceRules.NextDue(previous?.DueDate ?? series.StartDate, series.Cadence);

        var chapter = new Chapter
        {
            Id = AllocateId(),
            Number = number,
            Pages = pages,
            DueDate = due,
            IsOneShot = oneShot,
            PitchMagazineId = pitchMagazine?.Id,
            Stages = StageOrder.All.Select(stage => new StageWork
            {
                Stage = stage,
                HoursRequired = Settings.Balance.HoursRequired(stage, pages),
            }).ToList(),
        };
        series.Chapters.Add(chapter);
        Emit(EventType.ChapterCreated,
            oneShot
                ? $"{series.Title} one-shot (ch.{number}) for {pitchMagazine!.Name} created, due {chapter.DueDate:ddd d MMM HH:mm}."
                : $"{series.Title} ch.{number} created, due {chapter.DueDate:ddd d MMM HH:mm}.",
            new EventContext(SeriesId: series.Id, ChapterNumber: number, MagazineId: pitchMagazine?.Id));
        return chapter;
    }

    /// <summary>The magazine's first issue close strictly after the given time.</summary>
    internal DateTime NextCloseAfter(Magazine magazine, DateTime after)
    {
        var close = MarketOf(magazine.Id).NextIssueClose;
        while (close <= after) close = close.AddDays(magazine.CadenceDays);
        return close;
    }

    /// <summary>The magazine's first issue close at or after the given time.</summary>
    internal DateTime NextCloseAtOrAfter(Magazine magazine, DateTime atOrAfter)
    {
        var close = MarketOf(magazine.Id).NextIssueClose;
        while (close < atOrAfter) close = close.AddDays(magazine.CadenceDays);
        return close;
    }

    /// <summary>
    /// Hands every unfinished stage of every active series to a person: manual assignments win, a stage in
    /// progress keeps its assignee, the lead writes the Name and pencils unless buried, and everything else goes
    /// to the best available person by skill discounted by the work already queued for them.
    /// </summary>
    private void AssignStages()
    {
        var queued = People.ToDictionary(p => p.Id, _ => 0.0);
        var mangaka = Mangaka;
        foreach (var series in Series.Where(s => s.Status == SeriesStatus.Active))
        {
            var lead = FindPerson(series.LeadId) ?? mangaka;
            foreach (var chapter in series.Chapters.OrderBy(c => c.DueDate).ThenBy(c => c.Number))
            {
                foreach (var work in chapter.Stages.Where(w => !w.IsDone))
                {
                    var chosen = ChooseAssignee(work, lead, queued) ?? mangaka;
                    work.AssignedTo = chosen.Id;
                    var multiplier = Settings.Balance.SkillMultiplier(chosen.Skill(work.Stage));
                    queued[chosen.Id] = queued.GetValueOrDefault(chosen.Id) + Math.Max(0, work.HoursRequired - work.HoursDone) / multiplier;
                }
            }
        }
    }

    private Person? ChooseAssignee(StageWork work, Person lead, Dictionary<int, double> queued)
    {
        if (work.ManualAssignee is { } manual && FindPerson(manual) is { } chosen && chosen.MayWork(work.Stage)) return chosen;
        if (work.Status == StageStatus.InProgress && work.AssignedTo is { } current && FindPerson(current) is { } keeper) return keeper;
        if (work.Stage == Stage.Name) return lead;
        if (work.Stage == Stage.Pencils && lead.MayWork(Stage.Pencils) &&
            queued.GetValueOrDefault(lead.Id) <= AssignmentRules.LeadPencilsBacklogHours) return lead;
        var candidates = People.Where(p => p.MayWork(work.Stage))
            .Select(p => (p.Id, AssignmentRules.Score(Settings.Balance.SkillMultiplier(p.Skill(work.Stage)), queued.GetValueOrDefault(p.Id))));
        var best = AssignmentRules.Best(candidates);
        return best is { } id ? FindPerson(id) : null;
    }

    /// <summary>The person's working window, shortened by two hours while moonlighting.</summary>
    internal int EffectiveWorkEndHour(Person person) =>
        person.IsMoonlighting
            ? Math.Max(person.Schedule.WorkStartHour + 1, person.Schedule.WorkEndHour - MoonlightRules.EarlyLeaveHours)
            : person.Schedule.WorkEndHour;

    /// <summary>True when the hour starting at hourStart is inside the person's (possibly shortened) regular schedule.</summary>
    internal bool IsRegularHour(Person person, DateTime hourStart) =>
        !person.Schedule.IsDayOff(hourStart) && hourStart.Hour >= person.Schedule.WorkStartHour &&
        hourStart.Hour < EffectiveWorkEndHour(person);

    internal IEnumerable<QueueRef> UnfinishedRefs(Person person) =>
        Series.Where(s => s.Status == SeriesStatus.Active)
            .SelectMany(s => s.Chapters)
            .SelectMany(c => c.Stages.Where(w => !w.IsDone && w.AssignedTo == person.Id)
                .Select(w => new QueueRef(c.Id, w.Stage)));

    private void OrderQueue(Person person)
    {
        var refs = UnfinishedRefs(person).ToHashSet();

        var ordered = new List<QueueRef>();
        foreach (var pin in person.Pins.Where(refs.Contains)) ordered.Add(pin);

        IEnumerable<QueueRef> rest = refs.Except(ordered);
        if (person.ManualOrder is not null)
        {
            var manual = person.ManualOrder.Where(refs.Contains).Except(ordered).ToList();
            ordered.AddRange(manual);
            rest = rest.Except(manual);
        }

        ordered.AddRange(rest
            .OrderBy(r => FindChapter(r.ChapterId)!.DueDate)
            .ThenBy(r => (int)r.Stage)
            .ThenBy(r => r.ChapterId));

        person.Queue = ordered;
        person.Pins.RemoveAll(pin => FindChapter(pin.ChapterId) is not { } chapter ||
            chapter.StageWork(pin.Stage).IsDone || chapter.StageWork(pin.Stage).AssignedTo != person.Id);
        person.CurrentTask = ordered.Cast<QueueRef?>().FirstOrDefault(r => IsStartable(r!.Value));
    }

    internal bool IsStartable(QueueRef r)
    {
        var chapter = FindChapter(r.ChapterId);
        if (chapter is null) return false;
        if (SeriesOf(chapter).Status != SeriesStatus.Active) return false;
        var work = chapter.StageWork(r.Stage);
        if (work.IsDone) return false;
        // The editor holds Pencils until the Name is approved; later stages wait through the dependency graph.
        if (r.Stage == Stage.Pencils && chapter.Editor == EditorStatus.AwaitingReview) return false;
        return StageOrder.Prerequisites(r.Stage).All(p => chapter.StageWork(p).IsDone);
    }
}
