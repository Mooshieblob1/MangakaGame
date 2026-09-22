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
[JsonDerivedType(typeof(PitchSeriesCommand), "PitchSeries")]
[JsonDerivedType(typeof(AcceptOfferCommand), "AcceptOffer")]
[JsonDerivedType(typeof(DeclineOfferCommand), "DeclineOffer")]
[JsonDerivedType(typeof(WithdrawSeriesCommand), "WithdrawSeries")]
[JsonDerivedType(typeof(EndSeriesCommand), "EndSeries")]
[JsonDerivedType(typeof(GetOnlineCommand), "GetOnline")]
[JsonDerivedType(typeof(HireCommand), "Hire")]
[JsonDerivedType(typeof(FireCommand), "Fire")]
[JsonDerivedType(typeof(SetSalaryCommand), "SetSalary")]
[JsonDerivedType(typeof(SetAllowedStagesCommand), "SetAllowedStages")]
[JsonDerivedType(typeof(SetSeriesLeadCommand), "SetSeriesLead")]
[JsonDerivedType(typeof(AssignStageCommand), "AssignStage")]
[JsonDerivedType(typeof(SetPromotionCommand), "SetPromotion")]
[JsonDerivedType(typeof(MovePremisesCommand), "MovePremises")]
[JsonDerivedType(typeof(BuyAmenityCommand), "BuyAmenity")]
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

// Sub-project 2: publishing and market.
public record PitchSeriesCommand(int SeriesId, string MagazineId) : ICommand;
public record AcceptOfferCommand(int SeriesId) : ICommand;
public record DeclineOfferCommand(int SeriesId) : ICommand;
public record WithdrawSeriesCommand(int SeriesId) : ICommand;
public record EndSeriesCommand(int SeriesId) : ICommand;
public record GetOnlineCommand : ICommand;

// Sub-project 3: staff and studio.
public record HireCommand(int CandidateId, int Salary) : ICommand;
public record FireCommand(int PersonId) : ICommand;
public record SetSalaryCommand(int PersonId, int Salary) : ICommand;
public record SetAllowedStagesCommand(int PersonId, HashSet<Stage> Stages) : ICommand;
public record SetSeriesLeadCommand(int SeriesId, int PersonId) : ICommand;
/// <summary>PersonId null clears the manual assignment.</summary>
public record AssignStageCommand(int ChapterId, Stage Stage, int? PersonId) : ICommand;
/// <summary>SeriesId null turns promotion off; 0 promotes the whole studio.</summary>
public record SetPromotionCommand(int PersonId, int? SeriesId) : ICommand;
public record MovePremisesCommand(string PremisesId) : ICommand;
public record BuyAmenityCommand(string AmenityId) : ICommand;
