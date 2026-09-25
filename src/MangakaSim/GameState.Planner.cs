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
            BusinessId = ControlledBusinessId,
            LocationId = Protagonist.Employment!.LocationId,
            LeadPersonId = ProtagonistPersonId,
            RightsLeadPersonId = ProtagonistPersonId,
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
            if (series.StandaloneDoujin && series.Chapters.Count > 0) continue;
            if (Progression.Manuscripts.Any(m => m.SeriesId == series.Id && !m.Released)) continue;
            var open = series.Chapters.Count(c => c.Status != ChapterStatus.Complete);
            var completed = series.Chapters.Count(c => c.Status == ChapterStatus.Complete && c.PublishedAt is null && !c.IsOneShot && !c.DoujinEligible);
            if (series.Publishing == PublishingStatus.Serialized && completed >= Math.Max(1,series.BufferLimit)) continue;
            if (series.Publishing == PublishingStatus.Unpublished && series.Volumes.Count(v => v.IsDoujin && v.ReleasedAt is null && (!series.ReleaseShortIssues || v.Format==VolumeFormat.DoujinIssue)) >= series.MasterLimit) continue;
            if (open < series.PipelineLimit && (open == 0 || series.Chapters.Last().StageWork(Stage.Name).IsDone)) CreateNextChapter(series);
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
        foreach (var series in Series.Where(s => s.Status == SeriesStatus.Active))
        {
            var staff = People.Where(p => CanProduce(p) && p.Employment!.BusinessId == series.BusinessId &&
                p.Employment.LocationId == series.LocationId).ToArray();
            foreach (var chapter in series.Chapters)
            foreach (var stage in chapter.Stages.Where(s => !s.IsDone))
            {
                var leadId = chapter.CreatorPersonId ?? series.LeadPersonId;
                var eligible = staff.Where(p => stage.Stage != Stage.Name || p.Id == leadId).ToArray();
                if (stage.ManualAssignee is { } manual)
                { stage.AssignedTo = eligible.Any(p => p.Id == manual) ? manual : null; continue; }
                if (stage.Status == StageStatus.InProgress && eligible.Any(p => p.Id == stage.AssignedTo && p.Schedule.IsRegularHour(Clock.Now))) continue;
                stage.AssignedTo = eligible.OrderByDescending(p => p.MainSeriesId == series.Id)
                    .ThenByDescending(p => p.Skill(stage.Stage)).ThenBy(p => p.Id).FirstOrDefault()?.Id;
            }
        }
    }

    private void ShareSpareCapacity()
    {
        if (!Series.Any(s=>s.Status==SeriesStatus.Active)) return;
        var claimed = new HashSet<QueueRef>();
        foreach (var person in People.OrderBy(p => p.Id))
        {
            if (!IsWorkingHour(person, TickStart, out _) || person.CurrentTask is { } own && IsStartable(own))
            { if (person.CurrentTask is { } task) claimed.Add(task); continue; }
            var extra = Series.Where(s => s.Status == SeriesStatus.Active && s.BusinessId == person.Employment!.BusinessId && s.LocationId == person.Employment.LocationId)
                .SelectMany(s => s.Chapters.SelectMany(c => c.Stages.Where(w => !w.IsDone && w.ManualAssignee is null &&
                    (w.Stage != Stage.Name || (c.CreatorPersonId ?? s.LeadPersonId) == person.Id)).Select(w => new QueueRef(c.Id,w.Stage))))
                .Where(r => !claimed.Contains(r) && IsStartable(r) && !People.Any(p => p.Id != person.Id && p.CurrentTask == r && IsWorkingHour(p,TickStart,out _)))
                .OrderByDescending(r => HasPublisherDeadline(FindChapter(r.ChapterId)!)).ThenBy(r => FindChapter(r.ChapterId)!.DueDate).ThenByDescending(r => person.Skill(r.Stage)).Cast<QueueRef?>().FirstOrDefault();
            if (extra is not { } choice) continue;
            FindChapter(choice.ChapterId)!.StageWork(choice.Stage).AssignedTo = person.Id;
            person.CurrentTask = choice; claimed.Add(choice);
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
            .OrderByDescending(r => HasPublisherDeadline(FindChapter(r.ChapterId)!)).ThenBy(r => FindChapter(r.ChapterId)!.DueDate)
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
        if(SeriesOf(chapter).Publishing is PublishingStatus.Pitching or PublishingStatus.Offered&&!chapter.IsOneShot)return false;
        var work = chapter.StageWork(r.Stage);
        if (work.IsDone) return false;
        if (r.Stage != Stage.Name && chapter.EditorMagazineId is not null && chapter.Editor != EditorStatus.Approved) return false;
        return chapter.Stages.TakeWhile(s => s.Stage != r.Stage).All(s => s.IsDone);
    }
}
