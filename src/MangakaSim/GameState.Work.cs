using MangakaSim.Rules;

namespace MangakaSim;

public partial class GameState
{
    /// <summary>Applies one hour of work per person for the hour starting at TickStart.</summary>
    internal void WorkStep()
    {
        // Freeze all choices before applying progress: one worker completing a stage
        // cannot unlock work for another inside this same simulated hour.
        ShareSpareCapacity();
        var assignments = People.Where(p => p.CurrentTask is { } task && IsStartable(task) && IsWorkingHour(p, TickStart, out _)).Select(p => (Person: p, Task: p.CurrentTask!.Value)).ToArray();
        foreach (var (person, task) in assignments)
        {
            if (!IsWorkingHour(person, TickStart, out var isOvertime)) continue;
            var chapter = FindChapter(task.ChapterId);
            if (chapter is null) continue;
            var series = SeriesOf(chapter);
            var work = chapter.StageWork(task.Stage);

            if (work.Status == StageStatus.NotStarted)
            {
                work.Status = StageStatus.InProgress;
                if (chapter.Status == ChapterStatus.NotStarted) chapter.Status = ChapterStatus.InProgress;
                Emit(EventType.StageStarted,
                    $"{person.Name} started {task.Stage} on {series.Title} ch.{chapter.Number}.",
                    seriesId: series.Id, chapterNumber: chapter.Number, personId: person.Id, stage: task.Stage,
                    context: new(ActivityDate: TickStart.Date));
            }

            var multiplier = Settings.Balance.SkillMultiplier(person.Skill(task.Stage)) * (person.LowestNeed < 20 ? .75 : person.LowestNeed < 35 ? .9 : 1);
            if (chapter.CreatorPersonId is null) chapter.CreatorPersonId = series.LeadPersonId;
            var useful = Math.Min(multiplier, work.HoursRequired - work.HoursDone);
            work.QualityWeightedWork += useful * QualityRules.SkillFactor(person.Skill(task.Stage));
            work.HoursDone = Math.Min(work.HoursRequired, work.HoursDone + multiplier);
            Observe(person, OfficeActivityKind.Work, task.Stage);
            person.HoursWorkedToday++;
            person.ProductiveHours++;
            if (person.HiddenTalent && person.ProductiveHours >= 8) { person.HiddenTalent = false; StudioMessage($"{person.Name} is a prodigy!", person.Id); }
            Learn(person,task.Stage,useful / Math.Max(.01,multiplier),true);
            NeedsChange(person,-3,-4,-3 + ChairComfort(person));
            work.HoursByPerson[person.Id] = checked(work.HoursByPerson.GetValueOrDefault(person.Id) + 1);
            series.LifetimeHoursByPerson[person.Id] = checked(series.LifetimeHoursByPerson.GetValueOrDefault(person.Id) + 1);
            if (isOvertime)
            {
                person.OvertimeHoursToday++; person.WeeklyOvertime++; work.OvertimeHours++;
                if (person.Employment is { } job) job.AccruedPay += job.MonthlySalary / (40m * 52m / 12m) * 1.25m;
            }

            if (work.HoursDone >= work.HoursRequired)
            {
                work.Status = StageStatus.Complete;
                work.Contribution = QualityRules.Weight(task.Stage) * 100 * Math.Min(1,
                    work.QualityWeightedWork / work.HoursRequired + (task.Stage == Stage.Name ? .05 * chapter.RedoCount : 0)) *
                    QualityRules.RushFactor(work.OvertimeHours, work.HoursRequired);
                Emit(EventType.StageCompleted,
                    $"{person.Name} finished {task.Stage} on {series.Title} ch.{chapter.Number}.",
                    seriesId: series.Id, chapterNumber: chapter.Number, personId: person.Id, stage: task.Stage,
                    context: new(ActivityDate: TickStart.Date));
                SubmitNameIfReady(chapter);
                CompleteChapterIfDone(chapter, TickStart.Date);
            }
        }
        // Rebuild after the frozen allocation has finished applying this hour.
        RunPlanner();
    }

    internal bool IsWorkingHour(Person person, DateTime hourStart, out bool isOvertime)
    {
        if(AtOutsideJob(person,hourStart)){isOvertime=false;return false;}
        isOvertime = false;
        if (!person.Schedule.IsRegularHour(hourStart) && !IsOvertimeHour(person,hourStart)) return false;
        if (!CanProduce(person) || person.Employment!.StartsAt > hourStart || person.BusyUntil > hourStart || (hourStart <= TickStart && person.LowestNeed < 10)) return false;
        // Full-time hired staff have a protected lunch hour; the founder keeps the
        // original schedule until the needs/provision slice replaces fixed breaks.
        if (person.Id != ProtagonistPersonId && hourStart.Hour == person.Schedule.WorkStartHour + 4) return false;
        if (person.Schedule.IsRegularHour(hourStart)) return true;
        if (IsOvertimeHour(person, hourStart))
        {
            isOvertime = true;
            return true;
        }
        return false;
    }

    internal bool IsOvertimeHour(Person person, DateTime hourStart)
    {
        if (!person.OvertimeAllowed || person.WeeklyOvertime >= person.WeeklyOvertimeLimit || person.OvertimeHoursToday >= person.DailyOvertimeLimit) return false;
        if (person.OvertimeHoursToday >= Settings.Balance.OvertimeCap) return false;
        var schedule = person.Schedule;
        if (schedule.IsDayOff(hourStart)) return false;
        var hour = hourStart.Hour;
        if (hour < schedule.WorkEndHour || hour >= schedule.WorkEndHour + Settings.Balance.OvertimeCap) return false;
        if (person.CurrentTask is not { } task) return false;
        var chapter = FindChapter(task.ChapterId);
        return chapter is { IsAtRisk: true };
    }

    /// <summary>If every stage is Complete or Skipped, closes the chapter and records lateness.</summary>
    internal void CompleteChapterIfDone(Chapter chapter, DateTime? activityDate = null)
    {
        if (chapter.Status == ChapterStatus.Complete || !chapter.IsFinished) return;
        if (chapter.EditorMagazineId is not null && chapter.Editor != EditorStatus.Approved) return;
        var series = SeriesOf(chapter);
        chapter.Status = ChapterStatus.Complete;
        chapter.CompletedAt = Clock.Now;
        chapter.IsAtRisk = false;
        chapter.Quality = QualityRules.Total(chapter.Stages.Select(s => s.Contribution));
        if (HasPublisherDeadline(chapter) && Clock.Now > chapter.DueDate)
        {
            chapter.IsLate = true;
            chapter.HoursOverdue = (int)Math.Ceiling((Clock.Now - chapter.DueDate).TotalHours);
        }
        Emit(EventType.ChapterCompleted,
            $"{series.Title} ch.{chapter.Number} complete, quality {chapter.Quality}" + (chapter.IsLate ? $" ({chapter.HoursOverdue}h late)." : HasPublisherDeadline(chapter)?" on time.":"."),
            seriesId: series.Id, chapterNumber: chapter.Number, context: new(ActivityDate: activityDate));
        if (chapter.IsLate && HasPublisherDeadline(chapter) && !DeadlineProtected(series))
        {
            Emit(EventType.DeadlineMissed,
                $"{series.Title} ch.{chapter.Number} missed its deadline by {chapter.HoursOverdue}h.",
                seriesId: series.Id, chapterNumber: chapter.Number, context: new(ActivityDate: activityDate));
        }
        AwardCompletion(series, chapter);
        CollectDoujin(series);
        RunPlanner();
    }
}
