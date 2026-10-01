using System;
using System.Linq;
using Godot;
using MangakaSim;

namespace MangakaGame;

public partial class DebugMain
{
    // Per-computer sound settings and the Music and Effects buses feeding Master (spec 2026-09-28).
    // Inside Effects the office ambience keeps its old default balance against the interface sounds.
    private const float AmbienceBalance = .35f / .6f;
    private AudioSettings _audioSettings = new();
    private string? _audioSettingsPath;
    private bool _startupSilent;
    private static bool SmokeRun => OS.GetCmdlineUserArgs().Any(a => a.EndsWith("-smoke") || a == "--smoke-test");
    private bool AudioFocused => _audioSettings.PlayWhileUnfocused || GetWindow().HasFocus();

    private void LoadAudioSettings()
    {
        EnsureBus("Music"); EnsureBus("Effects");
        // Automated checks use full, in-memory levels and never touch the player's file.
        if (SmokeRun) { _audioSettings = new() { Master = 1, SetupDone = true }; _audioSettingsPath = null; }
        else { _audioSettingsPath = ProjectSettings.GlobalizePath("user://audio-settings.json"); _audioSettings = AudioSettings.Load(_audioSettingsPath); }
        ApplyAudioSettings();
    }

    // Music waits for the disclaimer to end, then fades in normally instead of jumping in at full level (final review).
    private void UpdateMusic(double delta) { if (_managementReady && !_startupSilent) _music.Update(delta, MusicNow(), 1, AudioFocused); }

    private void SaveAudioSettings() { if (_audioSettingsPath is not null) _audioSettings.Save(_audioSettingsPath); }

    private void ApplyAudioSettings()
    {
        SetBus("Master", _startupSilent ? 0 : _audioSettings.Master);
        SetBus("Music", _audioSettings.Music);
        SetBus("Effects", _audioSettings.Effects);
    }

    private static void EnsureBus(string name)
    {
        if (AudioServer.GetBusIndex(name) >= 0) return;
        AudioServer.AddBus();
        var index = AudioServer.BusCount - 1;
        AudioServer.SetBusName(index, name); AudioServer.SetBusSend(index, "Master");
    }

    private static void SetBus(string name, double level)
    {
        var index = AudioServer.GetBusIndex(name);
        if (index < 0) return;
        AudioServer.SetBusMute(index, level <= 0);
        AudioServer.SetBusVolumeDb(index, Mathf.LinearToDb((float)Math.Max(1e-5, level)));
    }

    /// <summary>The Master, Music and Sound effects sliders, shared by Settings and the first-launch screen.</summary>
    private void AudioSliders(Control parent)
    {
        foreach (var (label, read, write) in new (string, Func<double>, Action<double>)[]
        {
            ("Master volume", () => _audioSettings.Master, v => _audioSettings.Master = v),
            ("Music · within Master", () => _audioSettings.Music, v => _audioSettings.Music = v),
            ("Sound effects · within Master", () => _audioSettings.Effects, v => { _audioSettings.Effects = v; _audio.Preview(); }),
        })
        {
            Words(parent, label);
            var slider = new HSlider { MinValue = 0, MaxValue = 1, Step = .05, Value = read() }; parent.AddChild(slider);
            slider.ValueChanged += v => { write(v); ApplyAudioSettings(); SaveAudioSettings(); };
        }
    }
}
