using System.Text.Json.Serialization;

namespace MangakaSim;

public enum SeriesStatus { Active, Paused, Ended }
public enum ChapterStatus { NotStarted, InProgress, Complete }
public enum StageStatus { NotStarted, InProgress, Complete, Skipped }

/// <summary>Reference to one StageWork: the chapter's Id plus the stage.</summary>
public readonly record struct QueueRef(int ChapterId, Stage Stage);

public class Schedule
{
    public int WorkStartHour { get; set; } = 8;
    public int WorkEndHour { get; set; } = 18;
    public HashSet<DayOfWeek> DaysOff { get; set; } = new();

    public bool IsDayOff(DateTime hourStart) => DaysOff.Contains(hourStart.DayOfWeek);

    /// <summary>True when the hour starting at hourStart is inside the regular schedule.</summary>
    public bool IsRegularHour(DateTime hourStart) =>
        !IsDayOff(hourStart) && hourStart.Hour >= WorkStartHour && hourStart.Hour < WorkEndHour;
}

public class StageWork
{
    public Stage Stage { get; set; }
    public double HoursRequired { get; set; }
    public double HoursDone { get; set; }
    public int? AssignedTo { get; set; }
    public StageStatus Status { get; set; } = StageStatus.NotStarted;
    /// <summary>Hours logged per person id; sums to HoursDone.</summary>
    public Dictionary<int, double> HoursByPerson { get; set; } = new();
    /// <summary>A player-chosen assignee that wins over the planner until the stage is done.</summary>
    public int? ManualAssignee { get; set; }
    /// <summary>Hours worked outside the assignee's regular schedule.</summary>
    public double OvertimeHours { get; set; }
    /// <summary>Quality points, set when the stage completes (0 when skipped).</summary>
    public double Contribution { get; set; }

    [JsonIgnore]
    public bool IsDone => Status is StageStatus.Complete or StageStatus.Skipped;
}

public class Chapter
{
    public int Id { get; set; }
    public int Number { get; set; }
    /// <summary>Pages in this chapter; the series default or 31 for a one-shot.</summary>
    public int Pages { get; set; }
    public DateTime DueDate { get; set; }
    public ChapterStatus Status { get; set; } = ChapterStatus.NotStarted;
    public DateTime? CompletedAt { get; set; }
    public bool IsLate { get; set; }
    public int HoursOverdue { get; set; }
    public bool IsAtRisk { get; set; }
    public List<StageWork> Stages { get; set; } = new();

    /// <summary>0..100, set on completion.</summary>
    public int? Quality { get; set; }
    public EditorStatus Editor { get; set; } = EditorStatus.NotRequired;
    public DateTime? EditorDecisionAt { get; set; }
    public int RedoCount { get; set; }
    public bool IsOneShot { get; set; }
    /// <summary>One-shots only: the magazine the pitch targets.</summary>
    public string? PitchMagazineId { get; set; }
    public DateTime? PublishedAt { get; set; }
    public int? Rank { get; set; }

    [JsonIgnore]
    public bool IsFinished => Stages.All(s => s.IsDone);

    [JsonIgnore]
    public bool IsPublished => PublishedAt.HasValue;

    /// <summary>True when no stage has been started or skipped.</summary>
    [JsonIgnore]
    public bool IsUntouched => Stages.All(s => s.Status == StageStatus.NotStarted && s.HoursDone == 0);

    public StageWork StageWork(Stage stage) => Stages.First(s => s.Stage == stage);
}

public class Series
{
    public int Id { get; set; }
    public string Title { get; set; } = "";
    public string Genre { get; set; } = "";
    public Cadence Cadence { get; set; } = Cadence.Weekly;
    public int PagesPerChapter { get; set; } = 19;
    public SeriesStatus Status { get; set; } = SeriesStatus.Active;
    public DateTime StartDate { get; set; }
    public List<Chapter> Chapters { get; set; } = new();

    /// <summary>The person who writes the Name and pencils by default.</summary>
    public int LeadId { get; set; }
    public PublishingStatus Publishing { get; set; } = PublishingStatus.Unpublished;
    public Contract? Contract { get; set; }
    public SerializationOffer? PendingOffer { get; set; }
    /// <summary>Readers; unbounded, never negative.</summary>
    public double Fanbase { get; set; }
    /// <summary>0..100, never falls.</summary>
    public double CulturalImpact { get; set; }
    public bool IsIconic { get; set; }
    public bool MillionInfluenceGiven { get; set; }
    public List<DateTime> Strikes { get; set; } = new();
    public int WeeksBelowLine { get; set; }
    public DateTime? WarningIssuedAt { get; set; }
    /// <summary>Magazine id -> the time the cooldown ends.</summary>
    public Dictionary<string, DateTime> PitchCooldowns { get; set; } = new();
    public List<Volume> Volumes { get; set; } = new();
    /// <summary>Lifetime count of chapters published in a magazine.</summary>
    public int ChaptersPublished { get; set; }
    public int? LastRank { get; set; }

    [JsonIgnore]
    public bool IsSerialized => Publishing == PublishingStatus.Serialized;

    public Chapter? OpenChapter => Chapters.FirstOrDefault(c => c.Status != ChapterStatus.Complete);

    public bool IsInVolume(int chapterNumber) => Volumes.Any(v => v.Covers(chapterNumber));
}

public class Person
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public Dictionary<Stage, int> Skills { get; set; } = new();
    /// <summary>0..100, no decay.</summary>
    public double Reputation { get; set; }
    /// <summary>0..100; lowers the skill used for quality.</summary>
    public double Fatigue { get; set; }
    public PersonRole Role { get; set; } = PersonRole.Assistant;
    /// <summary>Nominal yen per month; 0 for the mangaka.</summary>
    public int Salary { get; set; }
    public DateTime? HiredAt { get; set; }
    public Needs Needs { get; set; } = new();
    /// <summary>0..100.</summary>
    public double Happiness { get; set; } = 60;
    public bool IsMoonlighting { get; set; }
    public int MonthsEmployed { get; set; }
    /// <summary>Stages the planner may hand this person.</summary>
    public HashSet<Stage> AllowedStages { get; set; } = new();
    /// <summary>This hour is a break rather than work.</summary>
    public bool OnBreak { get; set; }
    public int BreaksToday { get; set; }
    /// <summary>Last seven days, newest last: overtime hours, regular hours, breaks.</summary>
    public List<int> RecentOvertime { get; set; } = new();
    public List<int> RecentRegular { get; set; } = new();
    public List<int> RecentBreaks { get; set; } = new();
    /// <summary>Promotion duty in idle hours: null off, 0 the whole studio, otherwise a series id.</summary>
    public int? PromotionSeriesId { get; set; }

    [JsonIgnore]
    public bool IsMangaka => Role == PersonRole.Mangaka;

    public bool MayWork(Stage stage) => AllowedStages.Contains(stage);
    public Schedule Schedule { get; set; } = new();
    public bool OvertimeAllowed { get; set; }
    public List<QueueRef> Queue { get; set; } = new();
    public List<QueueRef> Pins { get; set; } = new();
    /// <summary>Explicit order from ReorderQueue; cleared at the next DayStarted.</summary>
    public List<QueueRef>? ManualOrder { get; set; }
    public QueueRef? CurrentTask { get; set; }
    public int HoursWorkedToday { get; set; }
    /// <summary>Overtime hours consumed today (breaks in overtime count against the cap).</summary>
    public int OvertimeHoursToday { get; set; }
    /// <summary>Regular scheduled hours actually worked today.</summary>
    public int RegularHoursToday { get; set; }

    public int Skill(Stage stage) => Skills.TryGetValue(stage, out var v) ? v : 0;
}
