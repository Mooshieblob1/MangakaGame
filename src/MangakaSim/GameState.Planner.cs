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

    internal Chapter CreateNextChapter(Series series)
    {
        var previous = series.Chapters.LastOrDefault();
        var number = previous is null ? 1 : previous.Number + 1;
        var from = previous?.DueDate ?? series.StartDate;
        var chapter = new Chapter
        {
            Id = AllocateId(),
            Number = number,
            DueDate = CadenceRules.NextDue(from, series.Cadence),
            Stages = StageOrder.All.Select(stage => new StageWork
            {
                Stage = stage,
                HoursRequired = Settings.Balance.HoursRequired(stage, series.PagesPerChapter),
            }).ToList(),
        };
        series.Chapters.Add(chapter);
        Emit(EventType.ChapterCreated,
            $"{series.Title} ch.{number} created, due {chapter.DueDate:ddd d MMM HH:mm}.",
            seriesId: series.Id, chapterNumber: number);
        return chapter;
    }

    private void AssignStages()
    {
        // Sub-project 1: the single person gets everything. Sub-project 3 replaces this.
        var assignee = People[0].Id;
        foreach (var chapter in Series.Where(s => s.Status == SeriesStatus.Active).SelectMany(s => s.Chapters))
        {
            foreach (var stage in chapter.Stages.Where(s => !s.IsDone)) stage.AssignedTo = assignee;
        }
    }

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
        return chapter.Stages.TakeWhile(s => s.Stage != r.Stage).All(s => s.IsDone);
    }
}
