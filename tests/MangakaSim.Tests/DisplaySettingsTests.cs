using Xunit;
namespace MangakaSim.Tests;

// Display settings (spec 2026-10-04, Q64): one interface size, fullscreen or windowed, per computer.
public class DisplaySettingsTests
{
    static string TempFile() => Path.Combine(Path.GetTempPath(), "display-" + Guid.NewGuid().ToString("N"), "display-settings.json");

    [Fact] public void A_new_install_is_fullscreen_with_automatic_size()
    {
        var s = new DisplaySettings();
        Assert.Equal(DisplayMode.Fullscreen, s.Mode);
        Assert.Null(s.InterfaceSize);
        Assert.Equal((1600, 900), (s.WindowWidth, s.WindowHeight));
    }

    [Theory]
    [InlineData(96, 1.0)] [InlineData(120, 1.25)] [InlineData(144, 1.5)] [InlineData(192, 2.0)]
    [InlineData(100, 1.05)] [InlineData(0, 1.0)] [InlineData(400, 2.0)] [InlineData(60, .8)]
    public void Automatic_size_follows_windows_scaling_in_five_per_cent_steps(double dpi, double expected) =>
        Assert.Equal(expected, DisplaySettings.AutomaticSize(dpi), 10);

    [Theory]
    [InlineData(1366, 768, 1.05)] [InlineData(1920, 1080, 1.5)] [InlineData(2560, 1440, 2.0)] [InlineData(3440, 1440, 2.0)]
    [InlineData(1280, 720, 1.0)] [InlineData(1024, 600, .8)]
    // Windows fullscreen windows can lose a pixel or two to a border; 150% must still fit a 1080p screen (final review).
    [InlineData(1918, 1078, 1.5)] [InlineData(2558, 1438, 2.0)]
    public void The_largest_size_keeps_the_layout_at_least_1280_by_720(int width, int height, double expected) =>
        Assert.Equal(expected, DisplaySettings.LargestFit(width, height), 10);

    [Fact] public void The_size_used_is_the_chosen_or_automatic_size_within_what_fits()
    {
        var auto = new DisplaySettings();
        Assert.Equal(1.5, auto.Effective(144, 1920, 1080), 10);   // a 150% laptop
        Assert.Equal(1.05, auto.Effective(144, 1366, 768), 10);   // a small laptop cannot fit 150%
        var chosen = new DisplaySettings { InterfaceSize = 2 };
        Assert.Equal(1.5, chosen.Effective(96, 1920, 1080), 10);  // shown at the limit on a smaller screen
        Assert.Equal(2, chosen.InterfaceSize);                     // without forgetting the choice
        Assert.Equal(.9, new DisplaySettings { InterfaceSize = .9 }.Effective(144, 1920, 1080), 10);
    }

    [Fact] public void Window_sizes_offered_fit_the_screen()
    {
        Assert.Equal(new[] { (1280, 720), (1600, 900), (1920, 1080) }, DisplaySettings.SizesFor(1920, 1080));
        Assert.Contains((3440, 1440), DisplaySettings.SizesFor(3440, 1440));
        Assert.DoesNotContain((1600, 900), DisplaySettings.SizesFor(1366, 768));
        Assert.Equal(new[] { (1024, 600) }, DisplaySettings.SizesFor(1024, 600)); // nothing standard fits: the screen itself
    }

    [Fact] public void Settings_survive_a_save_and_load()
    {
        var path = TempFile();
        new DisplaySettings { Mode = DisplayMode.Windowed, WindowWidth = 1920, WindowHeight = 1080, InterfaceSize = 1.25, Maximized = true }.Save(path);
        var loaded = DisplaySettings.Load(path);
        Assert.True(loaded.Maximized);
        Assert.Equal(DisplayMode.Windowed, loaded.Mode);
        Assert.Equal((1920, 1080), (loaded.WindowWidth, loaded.WindowHeight));
        Assert.Equal(1.25, loaded.InterfaceSize);
    }

    [Fact] public void A_missing_or_damaged_file_gives_defaults()
    {
        Assert.Equal(DisplayMode.Fullscreen, DisplaySettings.Load(TempFile()).Mode);
        var path = TempFile(); Directory.CreateDirectory(Path.GetDirectoryName(path)!); File.WriteAllText(path, "{ not json");
        var loaded = DisplaySettings.Load(path);
        Assert.Equal(DisplayMode.Fullscreen, loaded.Mode); Assert.Null(loaded.InterfaceSize);
    }

    [Fact] public void Out_of_range_values_are_clamped_on_load()
    {
        var path = TempFile(); Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, "{\"Mode\":7,\"WindowWidth\":10,\"WindowHeight\":99999,\"InterfaceSize\":9.3}");
        var loaded = DisplaySettings.Load(path);
        Assert.Equal(DisplayMode.Fullscreen, loaded.Mode);
        Assert.Equal(DisplaySettings.MinWindowWidth, loaded.WindowWidth);
        Assert.Equal(DisplaySettings.MaxWindowHeight, loaded.WindowHeight);
        Assert.Equal(DisplaySettings.MaxSize, loaded.InterfaceSize);
        Assert.Equal(1.05, DisplaySettings.Normalise(1.07), 10); // five per cent steps
    }
}
