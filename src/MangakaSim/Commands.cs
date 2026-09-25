using System.Text.Json.Serialization;

namespace MangakaSim;

/// <summary>
/// A player action. Commands are plain records so they can be logged and serialized for replay.
/// The JSON discriminator is written as "type": "CreateSeries" and so on.
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(DifficultyCommand), "Difficulty")]
[JsonDerivedType(typeof(RecognitionCommand), "Recognition")]
[JsonDerivedType(typeof(AdoptManuscriptCommand), "AdoptManuscript")]
[JsonDerivedType(typeof(LicenseCommand), "License")]
[JsonDerivedType(typeof(StoryCommand), "Story")]
[JsonDerivedType(typeof(TimelineCommand), "Timeline")]
[JsonDerivedType(typeof(StudioActionCommand), "StudioAction")]
[JsonDerivedType(typeof(CreateSeriesCommand), "CreateSeries")]
[JsonDerivedType(typeof(CreateDoujinCommand), "CreateDoujin")]
[JsonDerivedType(typeof(SetOutsideJobCommand), "OutsideJob")]
[JsonDerivedType(typeof(SetDoujinIssuesCommand), "DoujinIssues")]
[JsonDerivedType(typeof(ContinueOneShotCommand), "ContinueOneShot")]
[JsonDerivedType(typeof(ReserveConventionStockCommand), "ReserveConventionStock")]
[JsonDerivedType(typeof(PublishDoujinOnlineCommand), "PublishDoujinOnline")]
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
[JsonDerivedType(typeof(ContributeFundsCommand), "ContributeFunds")]
[JsonDerivedType(typeof(RecruitStaffCommand), "RecruitStaff")]
[JsonDerivedType(typeof(HireStaffCommand), "HireStaff")]
[JsonDerivedType(typeof(DismissStaffCommand), "DismissStaff")]
[JsonDerivedType(typeof(AssignStaffCommand), "AssignStaff")]
[JsonDerivedType(typeof(AssignStageCommand), "AssignStage")]
[JsonDerivedType(typeof(ApplyOfficeLayoutCommand), "OfficeLayout")]
[JsonDerivedType(typeof(AssignDeskCommand), "AssignDesk")]
[JsonDerivedType(typeof(SetAppearanceCommand), "Appearance")]
[JsonDerivedType(typeof(RelocateOfficeCommand), "RelocateOffice")]
public interface ICommand
{
}

/// <summary>The game time is recorded so commands can be replayed between ticks.</summary>
public record CommandEntry(DateTime Time, ICommand Command);

public record CreateSeriesCommand(string Title, string Genre, Cadence Cadence, int PagesPerChapter, bool ShortIssues = false) : ICommand;
public record CreateDoujinCommand(string Title, string Genre, int Pages = 16) : ICommand;
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
public record PitchSeriesCommand(int SeriesId, string MagazineId) : ICommand;
public record AcceptOfferCommand(int SeriesId) : ICommand;
public record DeclineOfferCommand(int SeriesId) : ICommand;
public record WithdrawSeriesCommand(int SeriesId) : ICommand;
public record EndSeriesCommand(int SeriesId) : ICommand;
public record GetOnlineCommand() : ICommand;
public record ContributeFundsCommand(long Amount) : ICommand;
public record RecruitStaffCommand(Stage? Specialty = null) : ICommand;
public record HireStaffCommand(int CandidateId, int LocationId, long MonthlySalary) : ICommand;
public record DismissStaffCommand(int PersonId) : ICommand;
public record AssignStaffCommand(int PersonId, int SeriesId, bool Lead = false) : ICommand;
public record AssignStageCommand(int ChapterId, Stage Stage, int? PersonId) : ICommand;
