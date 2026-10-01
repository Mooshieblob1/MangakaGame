using System.Text.Json;

namespace MangakaSim;

/// <summary>
/// Per-computer sound settings (spec 2026-09-28, Q37 and Q38): Master caps Music and Sound effects. A missing or damaged
/// file gives the defaults, so first-launch setup shows again; saving never interrupts play.
/// </summary>
public sealed class AudioSettings
{
    private static readonly JsonSerializerOptions Json = new() { WriteIndented = true };

    public double Master { get; set; }
    public double Music { get; set; } = .5;
    public double Effects { get; set; } = .6;
    public bool PlayWhileUnfocused { get; set; }
    public bool SetupDone { get; set; }

    public static double Clamp(double value) => double.IsFinite(value) ? Math.Clamp(value, 0, 1) : 0;

    public static AudioSettings Load(string path)
    {
        try
        {
            if (!File.Exists(path)) return new();
            var settings = JsonSerializer.Deserialize<AudioSettings>(File.ReadAllText(path)) ?? new();
            settings.Master = Clamp(settings.Master); settings.Music = Clamp(settings.Music); settings.Effects = Clamp(settings.Effects);
            return settings;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException or NotSupportedException) { return new(); }
    }

    public void Save(string path)
    {
        try
        {
            if (Path.GetDirectoryName(path) is { Length: > 0 } folder) Directory.CreateDirectory(folder);
            File.WriteAllText(path, JsonSerializer.Serialize(this, Json));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or NotSupportedException) { }
    }
}
