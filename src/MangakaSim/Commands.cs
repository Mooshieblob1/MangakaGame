using System.Text.Json.Serialization;

namespace MangakaSim;

/// <summary>
/// A player action. Commands are plain records so they can be logged and serialized for replay.
/// The JSON discriminator is written as "type": "CreateSeries" and so on.
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(CreateSeriesCommand), "CreateSeries")]
[JsonDerivedType(typeof(PauseSeriesCommand), "PauseSeries")]
[JsonDerivedType(typeof(ResumeSeriesCommand), "ResumeSeries")]
[JsonDerivedType(typeof(SetCadenceCommand), "SetCadence")]
[JsonDerivedType(typeof(SetPagesPerChapterCommand), "SetPagesPerChapter")]
[JsonDerivedType(typeof(PinStageCommand), "PinStage")]
[JsonDerivedType(typeof(UnpinStageCommand), "UnpinStage")]
[JsonDerivedType(typeof(ReorderQueueCommand), "ReorderQueue")]
[JsonDerivedType(typeof(SkipStageCommand), "SkipStage")]
[JsonDerivedType(typeof(SetScheduleCommand), "SetSchedule")]
[JsonDerivedType(typeof(SetOvertimeAllowedCommand), "SetOvertimeAllowed")]
public interface ICommand
{
}

/// <summary>The game time is recorded so commands can be replayed between ticks.</summary>
public record CommandEntry(DateTime Time, ICommand Command);

public record CreateSeriesCommand(string Title, string Genre, Cadence Cadence, int PagesPerChapter) : ICommand;
public record PauseSeriesCommand(int SeriesId) : ICommand;
public record ResumeSeriesCommand(int SeriesId) : ICommand;
public record SetCadenceCommand(int SeriesId, Cadence Cadence) : ICommand;
public record SetPagesPerChapterCommand(int SeriesId, int Pages) : ICommand;


public class InvalidCommandException : Exception
{
    public InvalidCommandException(string message) : base(message)
    {
    }
}
public record PinStageCommand(int PersonId, int ChapterId, Stage Stage) : ICommand;
public record UnpinStageCommand(int PersonId, int ChapterId, Stage Stage) : ICommand;
public record ReorderQueueCommand(int PersonId, List<QueueRef> OrderedRefs) : ICommand;
public record SkipStageCommand(int ChapterId, Stage Stage) : ICommand;
public record SetScheduleCommand(int PersonId, int WorkStartHour, int WorkEndHour, HashSet<DayOfWeek> DaysOff) : ICommand;
public record SetOvertimeAllowedCommand(int PersonId, bool Allowed) : ICommand;
