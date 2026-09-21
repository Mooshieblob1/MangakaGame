using System.Text.Json.Serialization;

namespace MangakaSim;

public class GameClock
{
    public static readonly DateTime Start = new(1996, 4, 1, 8, 0, 0);

    public DateTime Now { get; set; } = Start;

    [JsonIgnore]
    public DayOfWeek DayOfWeek => Now.DayOfWeek;
    [JsonIgnore]
    public int Hour => Now.Hour;

    public static GameClock AtStart() => new() { Now = Start };

    public void Advance() => Now = Now.AddHours(1);

    /// <summary>Whole hours from Now until target; 0 if target is now or in the past.</summary>
    public int HoursUntil(DateTime target)
    {
        var hours = (target - Now).TotalHours;
        return hours <= 0 ? 0 : (int)Math.Ceiling(hours);
    }
}
