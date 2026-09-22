namespace MangakaSim;

public partial class GameState
{
    /// <summary>Applies one hour of work per person for the hour starting at TickStart.</summary>
    internal void WorkStep()
    {
        foreach (var person in People)
        {
            if (!IsWorkingHour(person, TickStart, out var isOvertime)) continue;
            if (person.CurrentTask is not { } task) continue;
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
                    seriesId: series.Id, chapterNumber: chapter.Number, personId: person.Id, stage: task.Stage);
            }

            var multiplier = Settings.Balance.SkillMultiplier(person.Skill(task.Stage));
            work.HoursDone = Math.Min(work.HoursRequired, work.HoursDone + multiplier);
            person.HoursWorkedToday++;
            if (isOvertime)
            {
                person.OvertimeHoursToday++;
                work.OvertimeHours++;
            }

            if (work.HoursDone >= work.HoursRequired)
            {
                work.Status = StageStatus.Complete;
                work.Contribution = QualityRules.Contribution(task.Stage, person.Skill(task.Stage),
                    work.OvertimeHours, work.HoursRequired, chapter.RedoCount);
                Emit(EventType.StageCompleted,
                    $"{person.Name} finished {task.Stage} on {series.Title} ch.{chapter.Number}.",
                    seriesId: series.Id, chapterNumber: chapter.Number, personId: person.Id, stage: task.Stage);
                OnStageFinished(chapter, work);
                CompleteChapterIfDone(chapter);
                // Rebuild queues so CurrentTask moves to the next startable item.
                RunPlanner();
            }
        }
    }

    internal bool IsWorkingHour(Person person, DateTime hourStart, out bool isOvertime)
    {
        isOvertime = false;
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
        if (!person.OvertimeAllowed) return false;
        if (person.OvertimeHoursToday >= Settings.Balance.OvertimeCap) return false;
        var schedule = person.Schedule;
        if (schedule.IsDayOff(hourStart)) return false;
        var hour = hourStart.Hour;
        if (hour < schedule.WorkEndHour || hour >= schedule.WorkEndHour + Settings.Balance.OvertimeCap) return false;
        if (person.CurrentTask is not { } task) return false;
        var chapter = FindChapter(task.ChapterId);
        return chapter is { IsAtRisk: true };
    }

    /// <summary>Called after a stage completes or is skipped: hooks that depend on which stage finished.</summary>
    private void OnStageFinished(Chapter chapter, StageWork work)
    {
        if (work.Stage == Stage.Name) SubmitForReviewIfRequired(chapter);
    }

    /// <summary>If every stage is Complete or Skipped, closes the chapter, scores it and records lateness.</summary>
    internal void CompleteChapterIfDone(Chapter chapter)
    {
        if (chapter.Status == ChapterStatus.Complete || !chapter.IsFinished) return;
        var series = SeriesOf(chapter);
        chapter.Status = ChapterStatus.Complete;
        chapter.CompletedAt = Clock.Now;
        chapter.IsAtRisk = false;
        chapter.Quality = QualityRules.Quality(chapter.Stages.Select(s => s.Contribution));
        if (Clock.Now > chapter.DueDate)
        {
            chapter.IsLate = true;
            chapter.HoursOverdue = (int)Math.Ceiling((Clock.Now - chapter.DueDate).TotalHours);
        }
        Emit(EventType.ChapterCompleted,
            $"{series.Title} ch.{chapter.Number} finished, quality {chapter.Quality}" +
            (chapter.IsLate ? $" ({chapter.HoursOverdue}h late)." : " (on time)."),
            seriesId: series.Id, chapterNumber: chapter.Number);
        // Serialized chapters are judged at the issue close (IssueMissed), not by the doujin deadline rule.
        if (chapter.IsLate && !series.IsSerialized)
        {
            Emit(EventType.DeadlineMissed,
                $"{series.Title} ch.{chapter.Number} missed its deadline by {chapter.HoursOverdue}h.",
                seriesId: series.Id, chapterNumber: chapter.Number);
        }
        ApplyChapterCompletionReputation(series, chapter);
        TryCreateDoujinVolume(series);
        RunPlanner();
    }
}
