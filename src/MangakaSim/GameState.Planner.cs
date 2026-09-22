using MangakaSim.Rules;

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
            DoujinCadence = cadence,
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
        foreach (var series in Series.Where(s => s.Status == SeriesStatus.Active &&
                     s.Publishing is not (PublishingStatus.Pitching or PublishingStatus.Offered)))
        {
            var hasOpenChapter = series.Chapters.Any(c => c.Status != ChapterStatus.Complete);
            if (!hasOpenChapter) CreateNextChapter(series);
        }
    }

    internal Chapter CreateNextChapter(Series series, int? pages = null, DateTime? due = null)
    {
        var previous = series.Chapters.LastOrDefault();
        var number = series.NextChapterNumber++;
        var from = previous?.DueDate ?? series.StartDate;
        var chapterPages = pages ?? series.PagesPerChapter;
        var deadline = due ?? series.NextChapterDueOverride;
        if (deadline is null && series.Contract is { } contract)
        {
            var lastSlot = series.Chapters.Where(c => !c.IsOneShot && !c.DoujinEligible).OrderBy(c => c.DueDate).LastOrDefault();
            deadline = lastSlot is null ? contract.FirstIssueClose : IssueSchedule.FirstCloseAfter(PublisherCatalog.Get(contract.MagazineId), lastSlot.DueDate);
        }
        series.NextChapterDueOverride = null;
        var chapter = new Chapter
        {
            Id = AllocateId(),
            Number = number,
            Pages = chapterPages,
            DueDate = deadline ?? CadenceRules.NextDue(from, series.Cadence),
            DoujinEligible = series.Publishing == PublishingStatus.Unpublished,
            EditorMagazineId = series.Contract?.MagazineId,
            Stages = StageOrder.All.Select(stage => new StageWork
            {
                Stage = stage,
                HoursRequired = Settings.Balance.HoursRequired(stage, chapterPages),
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
        if (r.Stage != Stage.Name && chapter.EditorMagazineId is not null && chapter.Editor != EditorStatus.Approved) return false;
        return chapter.Stages.TakeWhile(s => s.Stage != r.Stage).All(s => s.IsDone);
    }
}
