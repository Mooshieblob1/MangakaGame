namespace MangakaSim;

public enum EventType
{
    IndustryNews,
    IndustryDecision,
    DayStarted,
    WeekStarted,
    StageStarted,
    StageCompleted,
    StageSkipped,
    ChapterCreated,
    ChapterCompleted,
    DeadlineMissed,
    ChapterAtRisk,
    DailyRecap,
    CommandApplied,
    PitchSubmitted, PitchRejected, SerializationOffered, OfferAccepted,
    OfferDeclined, OfferExpired, EditorApproved, EditorRedoRequested,
    ChapterPublished, IssueMissed, RankingPublished, CancellationWarning,
    CancellationWarningLifted, CancellationSurvived, SeriesCancelled,
    SeriesWithdrawn, SeriesEnded, SeriesBecameIconic, VolumeScheduled,
    VolumeReleased, VolumeMilestone, ConventionRecap, GenreTrendShifted,
    WentOnline,
    StaffHired, StaffNotice, StaffDeparted, RecruitmentCompleted, WageArrears,
    AwardNomination, AwardResult, LicenseOffered, LicenseDecision, LicenseReleased, CareerMilestone,
}
