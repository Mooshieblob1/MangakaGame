namespace MangakaSim;

public record StageRef(int SeriesId, int ChapterNumber, Stage Stage);
public record ChapterRef(int SeriesId, int ChapterNumber);
public record PersonHours(int PersonId, string Name, int Hours, int OvertimeHours);

public class DailyRecapPayload
{
    public List<StageRef> StagesStarted { get; set; } = new();
    public List<StageRef> StagesCompleted { get; set; } = new();
    public List<PersonHours> HoursPerPerson { get; set; } = new();
    public List<ChapterRef> ChaptersAtRisk { get; set; } = new();
    public List<ChapterRef> ChaptersCompleted { get; set; } = new();
    public List<ChapterRef> DeadlinesMissed { get; set; } = new();
}

public class GameEvent
{
    public DateTime Time { get; set; }
    public EventType Type { get; set; }
    public string Message { get; set; } = "";
    public int? SeriesId { get; set; }
    public int? ChapterNumber { get; set; }
    public int? PersonId { get; set; }
    public Stage? Stage { get; set; }
    public DailyRecapPayload? Recap { get; set; }
}
