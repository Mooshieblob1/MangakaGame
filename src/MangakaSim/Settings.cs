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
        settings.AutoPause[EventType.DailyRecap] = true;
        settings.AutoPause[EventType.ChapterCompleted] = true;
        settings.AutoPause[EventType.DeadlineMissed] = true;
        return settings;
    }
}
