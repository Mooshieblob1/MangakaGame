using System.Text.Json;

namespace MangakaSim;

public enum DisplayMode { Fullscreen, Windowed }

/// <summary>
/// Per-computer display settings (spec 2026-10-04, Q64): fullscreen or windowed, the window size, and one interface size
/// that scales every panel, button and text (Automatic follows Windows display scaling). The interface is laid out for at
/// least 1280 x 720, so the size in use never goes above what keeps that much room. A missing or damaged file gives the
/// defaults; saving never interrupts play.
/// </summary>
public sealed class DisplaySettings
{
    private static readonly JsonSerializerOptions Json = new() { WriteIndented = true };
    public const double MinSize = .8, MaxSize = 2, Step = .05;
    public const int LayoutWidth = 1280, LayoutHeight = 720, MinWindowWidth = 960, MinWindowHeight = 540, MaxWindowWidth = 7680, MaxWindowHeight = 4320;
    public static readonly (int Width, int Height)[] WindowSizes = [(1280, 720), (1600, 900), (1920, 1080), (2560, 1080), (2560, 1440), (3440, 1440)];

    public DisplayMode Mode { get; set; } = DisplayMode.Fullscreen;
    public int WindowWidth { get; set; } = 1600;
    public int WindowHeight { get; set; } = 900;
    /// <summary>Windowed and maximised: reopens maximised, keeping the window size for when it is restored.</summary>
    public bool Maximized { get; set; }
    /// <summary>The chosen interface size, or null for Automatic.</summary>
    public double? InterfaceSize { get; set; }
    /// <summary>The text language (a <see cref="Languages"/> code), or null for Automatic.</summary>
    public string? Language { get; set; }

    /// <summary>A size in five per cent steps within 80% to 200%.</summary>
    public static double Normalise(double size) =>
        double.IsFinite(size) ? Math.Clamp(Math.Round(size / Step) * Step, MinSize, MaxSize) : 1;

    /// <summary>Windows reports 96 DPI at 100% display scaling.</summary>
    public static double AutomaticSize(double dpi) => dpi > 0 ? Normalise(dpi / 96) : 1;

    /// <summary>
    /// The largest size that still leaves the interface 1280 x 720 or more of room. A few pixels of slack: Windows
    /// fullscreen windows can lose a pixel or two to a border, and 150% must still fit a 1080p screen (final review).
    /// </summary>
    public const int FitSlack = 4;
    public static double LargestFit(int width, int height) =>
        Math.Clamp(Math.Floor(Math.Min((double)(width + FitSlack) / LayoutWidth, (double)(height + FitSlack) / LayoutHeight) / Step + 1e-9) * Step, MinSize, MaxSize);

    /// <summary>The size in use: the choice (or Automatic), shown at most at what fits this window, without forgetting the choice.</summary>
    public double Effective(double dpi, int width, int height) =>
        Math.Max(MinSize, Math.Min(InterfaceSize ?? AutomaticSize(dpi), LargestFit(width, height)));

    /// <summary>The standard window sizes that fit this screen, or the screen itself when none does.</summary>
    public static IReadOnlyList<(int Width, int Height)> SizesFor(int screenWidth, int screenHeight)
    {
        var sizes = WindowSizes.Where(s => s.Width <= screenWidth && s.Height <= screenHeight).ToList();
        return sizes.Count > 0 ? sizes : [(screenWidth, screenHeight)];
    }

    public static DisplaySettings Load(string path)
    {
        try
        {
            if (!File.Exists(path)) return new();
            var settings = JsonSerializer.Deserialize<DisplaySettings>(File.ReadAllText(path)) ?? new();
            if (!Enum.IsDefined(settings.Mode)) settings.Mode = DisplayMode.Fullscreen;
            settings.WindowWidth = Math.Clamp(settings.WindowWidth, MinWindowWidth, MaxWindowWidth);
            settings.WindowHeight = Math.Clamp(settings.WindowHeight, MinWindowHeight, MaxWindowHeight);
            if (settings.InterfaceSize is { } size) settings.InterfaceSize = Normalise(size);
            if (!Languages.Known(settings.Language)) settings.Language = null;
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
