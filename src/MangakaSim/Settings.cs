namespace MangakaSim;

public class Settings
{
    public Dictionary<EventType, bool> AutoPause { get; set; } = new();
    public Balance Balance { get; set; } = new();

    public static Settings Default()
    {
        var settings = new Settings();
        foreach (var type in Enum.GetValues<EventType>())
        {
            settings.AutoPause[type] = false;
        }
        settings.AutoPause[EventType.IndustryDecision] = true;
        settings.AutoPause[EventType.DailyRecap] = true;
        settings.AutoPause[EventType.ChapterCompleted] = true;
        settings.AutoPause[EventType.DeadlineMissed] = true;
        foreach (var type in new[] { EventType.SerializationOffered, EventType.PitchRejected,
            EventType.EditorRedoRequested, EventType.CancellationWarning, EventType.SeriesCancelled,
            EventType.VolumeMilestone, EventType.ConventionRecap, EventType.SeriesBecameIconic })
            settings.AutoPause[type] = true;
        return settings;
    }
}
