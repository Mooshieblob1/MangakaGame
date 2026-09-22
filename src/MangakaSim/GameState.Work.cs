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
            var delivered = Math.Min(work.HoursRequired, work.HoursDone + multiplier) - work.HoursDone;
            work.HoursDone += delivered;
            work.HoursByPerson[person.Id] = work.HoursByPerson.GetValueOrDefault(person.Id) + delivered;
            person.HoursWorkedToday++;
            if (isOvertime)
            {
                person.OvertimeHoursToday++;
                work.OvertimeHours++;
            }

            if (work.HoursDone >= work.HoursRequired)
            {
                work.Status = StageStatus.Complete;
                work.Contribution = QualityRules.Contribution(task.Stage, WeightedSkill(work),
                    work.OvertimeHours, work.HoursRequired, chapter.RedoCount, WeightedFatigue(work));
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

    /// <summary>Hours-weighted mean skill of everyone who worked the stage; the current assignee's skill when nobody has.</summary>
    internal double WeightedSkill(StageWork work) =>
        QualityRules.WeightedSkill(work.HoursByPerson, id => FindAnyPerson(id)?.Skill(work.Stage) ?? 0,
            fallback: work.AssignedTo is { } a ? FindAnyPerson(a)?.Skill(work.Stage) ?? 0 : 0);

    /// <summary>Hours-weighted mean fatigue of the people who worked the stage.</summary>
    internal double WeightedFatigue(StageWork work) =>
        QualityRules.WeightedSkill(work.HoursByPerson, id => FindAnyPerson(id)?.Fatigue ?? 0,
            fallback: work.AssignedTo is { } a ? FindAnyPerson(a)?.Fatigue ?? 0 : 0);

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
