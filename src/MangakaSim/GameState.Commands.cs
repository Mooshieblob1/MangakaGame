namespace MangakaSim;

public partial class GameState
{
    /// <summary>Every command applied so far, in order. Serialized for replay.</summary>
    public List<CommandEntry> CommandLog { get; set; } = new();

    /// <summary>
    /// Validates the command fully, then mutates state, logs it, emits CommandApplied and runs the planner.
    /// Throws InvalidCommandException without changing anything when validation fails.
    /// </summary>
    public void Apply(ICommand command)
    {
        if (command is null) throw new InvalidCommandException("Command must not be null.");
        // Keep caller-owned collections out of the state and the replay log.
        command = command switch
        {
            ReorderQueueCommand c when c.OrderedRefs is not null => c with { OrderedRefs = c.OrderedRefs.ToList() },
            SetScheduleCommand c when c.DaysOff is not null => c with { DaysOff = new(c.DaysOff) },
            _ => command,
        };
        switch (command)
        {
            case CreateSeriesCommand c: ApplyCreateSeries(c); break;
            case PauseSeriesCommand c: ApplyPauseSeries(c); break;
            case ResumeSeriesCommand c: ApplyResumeSeries(c); break;
            case SetCadenceCommand c: ApplySetCadence(c); break;
            case SetPagesPerChapterCommand c: ApplySetPagesPerChapter(c); break;
            case PinStageCommand c: ApplyPinStage(c); break;
            case UnpinStageCommand c: ApplyUnpinStage(c); break;
            case ReorderQueueCommand c: ApplyReorderQueue(c); break;
            case SkipStageCommand c: ApplySkipStage(c); break;
            case SetScheduleCommand c: ApplySetSchedule(c); break;
            case SetOvertimeAllowedCommand c: ApplySetOvertimeAllowed(c); break;
            case PitchSeriesCommand c: ApplyPitchSeries(c); break;
            case AcceptOfferCommand c: ApplyAcceptOffer(c); break;
            case DeclineOfferCommand c: ApplyDeclineOffer(c); break;
            case WithdrawSeriesCommand c: ApplyWithdrawSeries(c); break;
            case EndSeriesCommand c: ApplyEndSeries(c); break;
            case GetOnlineCommand c: ApplyGetOnline(c); break;
            default:
                throw new InvalidCommandException($"Unsupported command {command.GetType().Name}.");
        }

        CommandLog.Add(new CommandEntry(Clock.Now, command));
        Emit(EventType.CommandApplied, command.ToString() ?? command.GetType().Name);
        RunPlanner();
        RiskStep();
    }

    private Series RequireSeries(int seriesId) =>
        FindSeries(seriesId) ?? throw new InvalidCommandException($"No series with id {seriesId}.");

    private void ApplyCreateSeries(CreateSeriesCommand c)
    {
        if (string.IsNullOrWhiteSpace(c.Title)) throw new InvalidCommandException("Series title must not be blank.");
        if (c.Genre is null) throw new InvalidCommandException("Genre must not be null.");
        if (!Enum.IsDefined(c.Cadence)) throw new InvalidCommandException($"Unknown cadence {c.Cadence}.");
        if (c.PagesPerChapter < 1) throw new InvalidCommandException("Pages per chapter must be at least 1.");
        CreateSeries(c.Title.Trim(), c.Genre.Trim(), c.Cadence, c.PagesPerChapter);
    }

    private void ApplyPauseSeries(PauseSeriesCommand c)
    {
        var series = RequireSeries(c.SeriesId);
        if (series.Status != SeriesStatus.Active)
            throw new InvalidCommandException($"Series '{series.Title}' is {series.Status}, only Active series can be paused.");
        series.Status = SeriesStatus.Paused;
    }

    private void ApplyResumeSeries(ResumeSeriesCommand c)
    {
        var series = RequireSeries(c.SeriesId);
        if (series.Status != SeriesStatus.Paused)
            throw new InvalidCommandException($"Series '{series.Title}' is {series.Status}, only Paused series can be resumed.");
        series.Status = SeriesStatus.Active;
    }

    private void ApplySetCadence(SetCadenceCommand c)
    {
        var series = RequireSeries(c.SeriesId);
        if (!Enum.IsDefined(c.Cadence)) throw new InvalidCommandException($"Unknown cadence {c.Cadence}.");
        series.Cadence = c.Cadence;
    }

    private void ApplySetPagesPerChapter(SetPagesPerChapterCommand c)
    {
        var series = RequireSeries(c.SeriesId);
        if (c.Pages < 1) throw new InvalidCommandException("Pages per chapter must be at least 1.");
        series.PagesPerChapter = c.Pages;
    }

    private Person RequirePerson(int personId) =>
        FindPerson(personId) ?? throw new InvalidCommandException($"No person with id {personId}.");

    private Chapter RequireChapter(int chapterId) =>
        FindChapter(chapterId) ?? throw new InvalidCommandException($"No chapter with id {chapterId}.");

    private void ApplyPinStage(PinStageCommand c)
    {
        var person = RequirePerson(c.PersonId);
        var chapter = RequireChapter(c.ChapterId);
        RequireStage(c.Stage);
        var work = chapter.StageWork(c.Stage);
        if (work.AssignedTo != person.Id || SeriesOf(chapter).Status != SeriesStatus.Active)
            throw new InvalidCommandException("Only an assigned stage of an active series can be pinned.");
        if (work.IsDone) throw new InvalidCommandException($"{c.Stage} of chapter {chapter.Number} is already {work.Status}.");
        var reference = new QueueRef(c.ChapterId, c.Stage);
        if (person.Pins.Contains(reference)) throw new InvalidCommandException($"{c.Stage} of chapter {chapter.Number} is already pinned.");
        person.Pins.Add(reference);
    }

    private void ApplyUnpinStage(UnpinStageCommand c)
    {
        var person = RequirePerson(c.PersonId);
        var reference = new QueueRef(c.ChapterId, c.Stage);
        if (!person.Pins.Remove(reference)) throw new InvalidCommandException($"{c.Stage} of chapter id {c.ChapterId} is not pinned.");
    }

    private void ApplyReorderQueue(ReorderQueueCommand c)
    {
        var person = RequirePerson(c.PersonId);
        var current = person.Queue;
        if (c.OrderedRefs is null) throw new InvalidCommandException("Queue order must not be null.");
        if (c.OrderedRefs.Count != current.Count)
            throw new InvalidCommandException($"Reorder must list all {current.Count} queue items, got {c.OrderedRefs.Count}.");
        if (c.OrderedRefs.Distinct().Count() != c.OrderedRefs.Count)
            throw new InvalidCommandException("Reorder contains duplicate queue items.");
        var missing = c.OrderedRefs.FirstOrDefault(r => !current.Contains(r));
        if (c.OrderedRefs.Any(r => !current.Contains(r)))
            throw new InvalidCommandException($"Reorder contains an item not in the queue: chapter id {missing.ChapterId} {missing.Stage}.");
        person.ManualOrder = c.OrderedRefs.ToList();
    }

    private void ApplySkipStage(SkipStageCommand c)
    {
        var chapter = RequireChapter(c.ChapterId);
        RequireStage(c.Stage);
        var work = chapter.StageWork(c.Stage);
        if (work.IsDone) throw new InvalidCommandException($"{c.Stage} of chapter {chapter.Number} is already {work.Status} and cannot be skipped.");
        var series = SeriesOf(chapter);
        work.Status = StageStatus.Skipped;
        work.HoursDone = 0;
        work.HoursByPerson.Clear();
        work.OvertimeHours = 0;
        work.Contribution = 0;
        if (chapter.Status == ChapterStatus.NotStarted) chapter.Status = ChapterStatus.InProgress;
        Emit(EventType.StageSkipped,
            $"{c.Stage} on {series.Title} ch.{chapter.Number} skipped.",
            seriesId: series.Id, chapterNumber: chapter.Number, stage: c.Stage);
        OnStageFinished(chapter, work);
        CompleteChapterIfDone(chapter);
    }

    private void ApplySetSchedule(SetScheduleCommand c)
    {
        var person = RequirePerson(c.PersonId);
        var cap = Settings.Balance.OvertimeCap;
        if (c.WorkStartHour < 0 || c.WorkStartHour >= c.WorkEndHour)
            throw new InvalidCommandException($"Work start {c.WorkStartHour} must be >= 0 and before work end {c.WorkEndHour}.");
        if (c.WorkEndHour > 24 - cap)
            throw new InvalidCommandException($"Work end {c.WorkEndHour} plus overtime cap {cap} must not pass midnight.");
        if (c.DaysOff is null || c.DaysOff.Any(day => !Enum.IsDefined(day)))
            throw new InvalidCommandException("Days off must contain valid weekdays.");
        person.Schedule.WorkStartHour = c.WorkStartHour;
        person.Schedule.WorkEndHour = c.WorkEndHour;
        person.Schedule.DaysOff = new HashSet<DayOfWeek>(c.DaysOff);
    }

    private void ApplySetOvertimeAllowed(SetOvertimeAllowedCommand c)
    {
        var person = RequirePerson(c.PersonId);
        person.OvertimeAllowed = c.Allowed;
    }

    private static void RequireStage(Stage stage)
    {
        if (!Enum.IsDefined(stage)) throw new InvalidCommandException($"Unknown stage {stage}.");
    }
}
