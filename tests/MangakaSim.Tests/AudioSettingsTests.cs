using Xunit;
namespace MangakaSim.Tests;

public class AudioSettingsTests
{
    static string TempFile() => Path.Combine(Path.GetTempPath(), "audio-" + Guid.NewGuid().ToString("N"), "audio-settings.json");

    [Fact] public void Defaults_start_silent_with_setup_not_done()
    {
        var s = new AudioSettings();
        Assert.Equal(0, s.Master); Assert.Equal(0.5, s.Music); Assert.Equal(0.6, s.Effects);
        Assert.False(s.PlayWhileUnfocused); Assert.False(s.SetupDone);
    }

    [Fact] public void Settings_survive_a_save_and_load()
    {
        var path = TempFile();
        new AudioSettings { Master = 0.8, Music = 0.3, Effects = 0.9, PlayWhileUnfocused = true, SetupDone = true }.Save(path);
        var loaded = AudioSettings.Load(path);
        Assert.Equal(0.8, loaded.Master); Assert.Equal(0.3, loaded.Music); Assert.Equal(0.9, loaded.Effects);
        Assert.True(loaded.PlayWhileUnfocused); Assert.True(loaded.SetupDone);
    }

    [Fact] public void A_missing_file_gives_defaults()
    {
        var loaded = AudioSettings.Load(TempFile());
        Assert.False(loaded.SetupDone); Assert.Equal(0, loaded.Master);
    }

    [Fact] public void A_damaged_file_gives_defaults_so_setup_shows_again()
    {
        var path = TempFile(); Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, "{ \"Master\": \"loud\", ");
        var loaded = AudioSettings.Load(path);
        Assert.False(loaded.SetupDone); Assert.Equal(0.5, loaded.Music);
    }

    [Fact] public void Levels_outside_0_to_1_are_clamped()
    {
        var path = TempFile(); Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, "{\"Master\":3,\"Music\":-1,\"Effects\":0.4,\"SetupDone\":true}");
        var loaded = AudioSettings.Load(path);
        Assert.Equal(1, loaded.Master); Assert.Equal(0, loaded.Music); Assert.Equal(0.4, loaded.Effects);
        Assert.True(loaded.SetupDone);
        Assert.Equal(0, AudioSettings.Clamp(double.NaN));
    }

    [Fact] public void Helper_voice_defaults_to_English_and_survives_a_save()
    {
        Assert.Equal("en", new AudioSettings().HelperVoice);
        var path = TempFile();
        new AudioSettings { HelperVoice = "ja" }.Save(path);
        Assert.Equal("ja", AudioSettings.Load(path).HelperVoice);
    }

    [Fact] public void An_older_or_unknown_helper_voice_falls_back_to_English()
    {
        var path = TempFile(); Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, "{\"Master\":0.5,\"SetupDone\":true}");
        Assert.Equal("en", AudioSettings.Load(path).HelperVoice);
        File.WriteAllText(path, "{\"HelperVoice\":\"klingon\",\"SetupDone\":true}");
        var loaded = AudioSettings.Load(path);
        Assert.Equal("en", loaded.HelperVoice); Assert.True(loaded.SetupDone);
    }

    [Fact] public void Save_never_throws_on_an_unwritable_path()
    {
        var blocker = Path.Combine(Path.GetTempPath(), "audio-blocker-" + Guid.NewGuid().ToString("N"));
        File.WriteAllText(blocker, "a file where a folder should be");
        new AudioSettings().Save(Path.Combine(blocker, "audio-settings.json"));
        File.Delete(blocker);
    }
}
