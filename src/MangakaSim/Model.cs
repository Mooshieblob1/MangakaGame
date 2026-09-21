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

    [JsonIgnore]
    public bool IsDone => Status is StageStatus.Complete or StageStatus.Skipped;
}

public class Chapter
{
    public int Id { get; set; }
    public int Number { get; set; }
    public DateTime DueDate { get; set; }
    public ChapterStatus Status { get; set; } = ChapterStatus.NotStarted;
    public DateTime? CompletedAt { get; set; }
    public bool IsLate { get; set; }
    public int HoursOverdue { get; set; }
    public bool IsAtRisk { get; set; }
    public List<StageWork> Stages { get; set; } = new();

    [JsonIgnore]
    public bool IsFinished => Stages.All(s => s.IsDone);

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
}

public class Person
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public Dictionary<Stage, int> Skills { get; set; } = new();
    public Schedule Schedule { get; set; } = new();
    public bool OvertimeAllowed { get; set; }
    public List<QueueRef> Queue { get; set; } = new();
    public List<QueueRef> Pins { get; set; } = new();
    /// <summary>Explicit order from ReorderQueue; cleared at the next DayStarted.</summary>
    public List<QueueRef>? ManualOrder { get; set; }
    public QueueRef? CurrentTask { get; set; }
    public int HoursWorkedToday { get; set; }
    public int OvertimeHoursToday { get; set; }

    public int Skill(Stage stage) => Skills.TryGetValue(stage, out var v) ? v : 0;
}
