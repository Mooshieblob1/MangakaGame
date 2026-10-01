# Start-up Disclaimer and First-launch Volume Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Show a silent AI-assets disclaimer on every launch and a first-launch volume screen (Master, Music, Sound effects), with per-computer volume settings applied through Godot audio buses.

**Architecture:** An engine-free `AudioSettings` class in `src/MangakaSim` loads and saves a small JSON file with defaults and clamping. The Godot side creates Music and Effects buses feeding Master, routes the music player and office audio to them, applies the settings every frame, and shows a start-up overlay before the main menu.

**Tech Stack:** C# (.NET 8), Godot 4.7.2 .NET, xUnit.

**Spec:** `docs/superpowers/specs/2026-09-28-startup-flow-and-volume-design.md`

## Global Constraints

- Disclaimer on every launch, volume screen only on first launch (Q37). Disclaimer wording (Q39), exactly:
  "This game was made with the help of generative AI." / "Some artwork, 3D models and music were created with AI tools," / "then chosen and edited for this game."
- Disclaimer: fade in about 1 s, hold about 3 s, fade out about 1 s; click, key or controller button skips; silent.
- Sliders Master, Music, Sound effects (Q38); defaults Master 0 until setup is done, Music 0.5, Sound effects 0.6; checkbox "Play sound even while unfocused", off by default.
- Volume is per computer (`user://audio-settings.json`); career volume fields stay in saves but are no longer used.
- Buses: Music and Effects send to Master; ambience keeps its balance of 0.35 to 0.6 against the effects.
- Automated checks skip both screens and never read or write the player's settings file; `--first-launch` shows the volume screen again.
- Presentation only: no simulation, balance or save-format change.
- Docs CRLF, metric, no em dashes; commit only when the user asks. Builds, tests and Godot checks are authorized; packaging is not.

## Review Focus

1. Escape and other keys while the start-up overlay is showing must not open or close menus underneath: pinned by the `_UnhandledKeyInput` guard (Task 3) and the startup smoke's skip check.
2. A second skip (double click or key repeat) must not run the end of the disclaimer twice: `EndDisclaimer` is guarded by `_disclaimerDone` (Task 3).
3. Dragging a slider saves the file many times; a locked or read-only user folder must never interrupt play: pinned by `Save_never_throws_on_an_unwritable_path` (Task 1).
4. A player who continues with Master at 0 hears nothing on later launches; the Settings line and heading must make the fix obvious: the Settings text "These settings apply to every career on this computer." sits beside the Master slider (Task 2).
5. Smoke runs must not touch the player's `audio-settings.json`: pinned by `SmokeRun` using in-memory settings (Task 2) and the startup smoke's temporary path (Task 3).

---

### Task 1: AudioSettings (engine-free)

**Files:**
- Create: `src/MangakaSim/AudioSettings.cs`
- Test: `tests/MangakaSim.Tests/AudioSettingsTests.cs`

**Interfaces:**
- Produces: `sealed class AudioSettings` with `double Master` (default 0), `double Music` (0.5), `double Effects` (0.6), `bool PlayWhileUnfocused` (false), `bool SetupDone` (false), `static AudioSettings Load(string path)`, `void Save(string path)`, `static double Clamp(double value)`.

- [ ] **Step 1: Write the failing tests**

`tests/MangakaSim.Tests/AudioSettingsTests.cs`:

```csharp
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

    [Fact] public void Save_never_throws_on_an_unwritable_path()
    {
        var blocker = Path.Combine(Path.GetTempPath(), "audio-blocker-" + Guid.NewGuid().ToString("N"));
        File.WriteAllText(blocker, "a file where a folder should be");
        new AudioSettings().Save(Path.Combine(blocker, "audio-settings.json"));
        File.Delete(blocker);
    }
}
```

- [ ] **Step 2: Run the tests to verify they fail**

Run: `dotnet test tests/MangakaSim.Tests --filter "FullyQualifiedName~AudioSettingsTests"`
Expected: build error, `AudioSettings` does not exist.

- [ ] **Step 3: Implement**

`src/MangakaSim/AudioSettings.cs`:

```csharp
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
```

- [ ] **Step 4: Run the tests to verify they pass**

Run: `dotnet test tests/MangakaSim.Tests --filter "FullyQualifiedName~AudioSettingsTests"`
Expected: PASS, 6 tests.

- [ ] **Step 5: Commit** (only if the user asked for commits)

```bash
git add src/MangakaSim/AudioSettings.cs tests/MangakaSim.Tests/AudioSettingsTests.cs
git commit -m "Audio: per-computer audio settings"
```

---

### Task 2: Buses, routing and the Settings sliders

**Files:**
- Create: `godot/DebugMain.Audio.cs`
- Modify: `godot/Office/OfficeAudio.cs` (`_Ready`, and a new `Preview` method)
- Modify: `godot/Office/MusicPlayer.cs` (`_Ready`)
- Modify: `godot/DebugMain.Alpha.cs` (`BuildAlpha` near line 30; the settings sliders near lines 251 to 260)
- Modify: `godot/DebugMain.cs` (`_Process` near lines 118 and 119)

**Interfaces:**
- Consumes: `AudioSettings` (Task 1).
- Produces: `DebugMain._audioSettings`, `_audioSettingsPath`, `_startupSilent`, `SmokeRun`, `LoadAudioSettings()`, `SaveAudioSettings()`, `ApplyAudioSettings()`, `AudioFocused`, `AudioSliders(Control parent)`; `OfficeAudio.Preview()`; buses named `Music` and `Effects`.

- [ ] **Step 1: Write the check first**

Create `godot/DebugMain.StartupSmoke.cs` with the bus and settings checks (Task 3 extends it with the screens):

```csharp
using System;
using System.IO;
using System.Linq;
using Godot;
using MangakaSim;

namespace MangakaGame;

public partial class DebugMain
{
    // Start-up disclaimer and first-launch volume (spec 2026-09-28). Uses a temporary settings file, never the player's.
    private async void RunStartupSmoke()
    {
        SetProcess(false);
        try
        {
            await SettleUi();
            int Bus(string name) => AudioServer.GetBusIndex(name);
            Check(Bus("Music") > 0 && Bus("Effects") > 0 && AudioServer.GetBusSend(Bus("Music")) == "Master" && AudioServer.GetBusSend(Bus("Effects")) == "Master",
                "Music and Sound effects feed the Master bus");
            _audioSettings = new() { Master = .5, Music = 0, Effects = 1, SetupDone = true }; ApplyAudioSettings();
            Check(AudioServer.IsBusMute(Bus("Music")) && !AudioServer.IsBusMute(Bus("Effects")) && AudioServer.GetBusVolumeDb(0) < -5,
                "Each slider drives its own bus, and Master caps both");
            Check(_audioSettingsPath is null, "Smoke runs never read or write the player's audio settings file");
            GD.Print($"STARTUP SMOKE PASSED: {_smokeChecks} checks.");
            var tree = GetTree(); tree.CreateTimer(.1).Timeout += () => tree.Quit(); QueueFree();
        }
        catch (Exception ex) { GD.PushError($"STARTUP SMOKE FAILED: {ex.Message}\n{ex.StackTrace}"); GetTree().Quit(1); }
    }
}
```

In `godot/DebugMain.cs`, beside the other smoke dispatch lines:

```csharp
        if (OS.GetCmdlineUserArgs().Contains("--startup-smoke")) CallDeferred(nameof(RunStartupSmoke));
```

- [ ] **Step 2: Build to verify it fails**

Run: `dotnet build MangakaGame.sln -warnaserror`
Expected: FAIL, `_audioSettings`, `ApplyAudioSettings` and `_audioSettingsPath` do not exist.

- [ ] **Step 3: Implement the audio settings side**

`godot/DebugMain.Audio.cs`:

```csharp
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
```

`godot/Office/OfficeAudio.cs`: at the end of `_Ready`, add

```csharp
        foreach (var player in new[] { _room, _activity, _effect, _buzz }) player.Bus = "Effects";
```

and add the method:

```csharp
    /// <summary>A pencil scratch at the current level, for the Sound effects slider.</summary>
    public void Preview() { if (!_effect.Playing) _effect.Play(); }
```

`godot/Office/MusicPlayer.cs`: change `_Ready` to

```csharp
    public override void _Ready() { foreach (var player in _players) { player.Bus = "Music"; AddChild(player); } }
```

`godot/DebugMain.Alpha.cs`: in `BuildAlpha`, make the first line

```csharp
        LoadAudioSettings();
```

(before `_audio=new OfficeAudio();AddChild(_audio);`). Replace the settings slider loop and the sentence after it (the `foreach(var (label,read,write) in new (string,Func<double>,Action<double>)[]{` block with the "Music · 0 mutes" entries, and the `Words(parent,"Sounds stay natural at every speed...` line) with:

```csharp
        AudioSliders(parent);
        var unfocused=new CheckBox{Text="Play sound even while unfocused",ButtonPressed=_audioSettings.PlayWhileUnfocused};parent.AddChild(unfocused);
        unfocused.Toggled+=on=>{_audioSettings.PlayWhileUnfocused=on;SaveAudioSettings();};
        Words(parent,"These settings apply to every career on this computer. Office ambience pauses in menus, where the title music plays.",14);
```

`godot/DebugMain.cs`, in `_Process`, replace the two audio lines (near 118 and 119):

```csharp
        if(_managementReady)_audio.Update(delta,!_inMenu&&GetWindow().HasFocus(),_speed>0&&!OfficeEditing,_presentation.AmbienceVolume,_presentation.EffectsVolume);
        if(_managementReady)_music.Update(delta,MusicNow(),_presentation.MusicVolume,GetWindow().HasFocus());
```

with:

```csharp
        ApplyAudioSettings();
        if(_managementReady)_audio.Update(delta,!_inMenu&&AudioFocused,_speed>0&&!OfficeEditing,AmbienceBalance,1);
        if(_managementReady)_music.Update(delta,MusicNow(),1,AudioFocused);
```

- [ ] **Step 4: Build and run the checks**

Run:
```powershell
dotnet build MangakaGame.sln -warnaserror
$godot = Join-Path $env:LOCALAPPDATA 'Microsoft\WinGet\Packages\GodotEngine.GodotEngine.Mono_Microsoft.Winget.Source_8wekyb3d8bbwe\Godot_v4.7.2-stable_mono_win64\Godot_v4.7.2-stable_mono_win64_console.exe'
& $godot --headless --path godot -- --startup-smoke
& $godot --headless --path godot -- --alpha-smoke
& $godot --headless --path godot -- --atmosphere-smoke
& $godot --headless --path godot -- --music-smoke
```
Expected: build clean; `STARTUP SMOKE PASSED: 3 checks.`; the others pass (the alpha smoke's "Volume controls and text scale present" still finds at least three sliders).

- [ ] **Step 5: Commit** (only if the user asked for commits)

```bash
git add godot/DebugMain.Audio.cs godot/DebugMain.StartupSmoke.cs godot/Office/OfficeAudio.cs godot/Office/MusicPlayer.cs godot/DebugMain.Alpha.cs godot/DebugMain.cs
git commit -m "Audio: Master, Music and Effects buses and per-computer sliders"
```

---

### Task 3: The start-up overlay

**Files:**
- Create: `godot/DebugMain.Startup.cs`
- Modify: `godot/DebugMain.StartupSmoke.cs` (add the screen checks)
- Modify: `godot/DebugMain.Management.cs` (start-up near line 165; `_UnhandledKeyInput` near line 168)

**Interfaces:**
- Consumes: Task 2 members.
- Produces: `DisclaimerText`, `BeginStartup(bool showVolume)`, fields `_startup`, `_startupRoot`, `_disclaimer`, `_volumeSetup`.

- [ ] **Step 1: Extend the check first**

In `godot/DebugMain.StartupSmoke.cs`, insert before the `GD.Print($"STARTUP SMOKE PASSED` line:

```csharp
            // A first launch: disclaimer in silence, then the volume screen.
            var path = Path.Combine(SmokeOutput, "audio-settings-" + Guid.NewGuid().ToString("N") + ".json");
            _audioSettings = new(); _audioSettingsPath = path;
            BeginStartup(!_audioSettings.SetupDone); await SettleUi();
            Check(_disclaimer!.Visible && _disclaimer.Text == DisclaimerText && AudioServer.IsBusMute(0), "The disclaimer shows in silence");
            var key = new InputEventKey { Pressed = true, Keycode = Key.Space };
            _startupRoot!.EmitSignal(Control.SignalName.GuiInput, key); await SettleUi();
            _startupRoot.EmitSignal(Control.SignalName.GuiInput, key); await SettleUi();
            Check(!_disclaimer.Visible && _volumeSetup is { Visible: true }, "A key skips the disclaimer, once, to the volume screen on a first launch");
            ApplyAudioSettings();
            Check(AudioServer.IsBusMute(0), "Master starts at 0, so nothing is heard");
            var sliders = _volumeSetup!.FindChildren("*", "HSlider", true, false).OfType<HSlider>().ToArray();
            Check(sliders.Length == 3 && sliders[0].Value == 0 && Math.Abs(sliders[1].Value - .5) < 1e-9 && Math.Abs(sliders[2].Value - .6) < 1e-9,
                "Master 0, Music 50%, Sound effects 60%");
            sliders[0].Value = .8; ApplyAudioSettings();
            Check(!AudioServer.IsBusMute(0) && AudioServer.GetBusVolumeDb(0) > -3, "Raising Master makes sound audible");
            Press("Continue"); await SettleUi();
            Check(_startup is null && _audioSettings.SetupDone && AudioSettings.Load(path) is { SetupDone: true, Master: > .79 },
                "Continue saves the levels and ends first-launch setup");
            // A later launch: only the disclaimer.
            BeginStartup(!_audioSettings.SetupDone); await SettleUi();
            _startupRoot!.EmitSignal(Control.SignalName.GuiInput, key); await SettleUi();
            Check(_startup is null && !AudioServer.IsBusMute(0), "Later launches show only the disclaimer, then sound returns");
            _audioSettings.PlayWhileUnfocused = false; Check(AudioFocused == GetWindow().HasFocus(), "Sound follows window focus by default");
            _audioSettings.PlayWhileUnfocused = true; Check(AudioFocused, "The checkbox keeps sound playing while unfocused");
            _audioSettingsPath = null;
```

- [ ] **Step 2: Build to verify it fails**

Run: `dotnet build MangakaGame.sln -warnaserror`
Expected: FAIL, `BeginStartup`, `DisclaimerText` and the overlay fields do not exist.

- [ ] **Step 3: Implement the overlay**

`godot/DebugMain.Startup.cs`:

```csharp
using Godot;

namespace MangakaGame;

public partial class DebugMain
{
    // Every launch: a silent disclaimer (Q37, Q39). First launch only: the volume screen, silent until Master rises.
    public const string DisclaimerText = "This game was made with the help of generative AI.\nSome artwork, 3D models and music were created with AI tools,\nthen chosen and edited for this game.";
    private CanvasLayer? _startup;
    private Control? _startupRoot, _volumeSetup;
    private Label? _disclaimer;
    private Tween? _disclaimerTween;
    private bool _disclaimerDone, _startupShowVolume;

    private void BeginStartup(bool showVolume)
    {
        _startupShowVolume = showVolume; _disclaimerDone = false; _volumeSetup = null;
        _startupSilent = true; ApplyAudioSettings();
        _startup = new CanvasLayer { Layer = 100 }; AddChild(_startup);
        _startupRoot = new Control { MouseFilter = MouseFilterEnum.Stop, FocusMode = FocusModeEnum.All };
        _startupRoot.SetAnchorsPreset(LayoutPreset.FullRect); _startup.AddChild(_startupRoot);
        var black = new ColorRect { Color = Colors.Black, MouseFilter = MouseFilterEnum.Ignore };
        black.SetAnchorsPreset(LayoutPreset.FullRect); _startupRoot.AddChild(black);
        _disclaimer = new Label { Text = DisclaimerText, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center, Modulate = new Color(1, 1, 1, 0) };
        _disclaimer.SetAnchorsPreset(LayoutPreset.FullRect);
        _disclaimer.AddThemeColorOverride("font_color", Colors.White);
        _disclaimer.AddThemeFontSizeOverride("font_size", (int)(26 * _presentation.UiScale));
        _startupRoot.AddChild(_disclaimer);
        _startupRoot.GuiInput += e =>
        {
            if (e is InputEventMouseButton { Pressed: true } or InputEventKey { Pressed: true } or InputEventJoypadButton { Pressed: true })
            { _startupRoot.AcceptEvent(); SkipDisclaimer(); }
        };
        _startupRoot.GrabFocus();
        _disclaimerTween = CreateTween();
        _disclaimerTween.TweenProperty(_disclaimer, "modulate:a", 1f, 1.0);
        _disclaimerTween.TweenInterval(3.0);
        _disclaimerTween.TweenProperty(_disclaimer, "modulate:a", 0f, 1.0);
        _disclaimerTween.Finished += EndDisclaimer;
    }

    private void SkipDisclaimer() { if (!_disclaimerDone) { _disclaimerTween?.Kill(); EndDisclaimer(); } }

    private void EndDisclaimer()
    {
        if (_disclaimerDone) return;
        _disclaimerDone = true; _disclaimer?.Hide();
        if (_startupShowVolume) ShowVolumeSetup(); else EndStartup();
    }

    private void ShowVolumeSetup()
    {
        // Master is 0 here, so lifting the silence lets the title music follow the Master slider.
        _startupSilent = false; ApplyAudioSettings();
        var centre = new CenterContainer(); centre.SetAnchorsPreset(LayoutPreset.FullRect); _startupRoot!.AddChild(centre);
        var box = new VBoxContainer { CustomMinimumSize = new Vector2(420, 0) }; centre.AddChild(box);
        _volumeSetup = centre;
        Words(box, "Set your volume", 28);
        AudioSliders(box);
        ActionButton(box, "Continue", () => { _audioSettings.SetupDone = true; SaveAudioSettings(); EndStartup(); });
        Words(box, "You can change this any time in Settings.", 14);
    }

    private void EndStartup()
    {
        _startupSilent = false; ApplyAudioSettings();
        _startup?.QueueFree(); _startup = null; _startupRoot = null;
    }
}
```

`godot/DebugMain.Management.cs`, on the start-up line near 165 (`..._viewLocation=_state.Protagonist.Employment!.LocationId;RefreshManagement();ShowMenu();`), append:

```csharp
        if(!SmokeRun)BeginStartup(!_audioSettings.SetupDone||OS.GetCmdlineUserArgs().Contains("--first-launch"));
```

At the top of `_UnhandledKeyInput` (near line 170), before the existing first line:

```csharp
        if(_startup is not null)return; // keys belong to the start-up screens
```

- [ ] **Step 4: Build and run the checks**

Run:
```powershell
dotnet build MangakaGame.sln -warnaserror
& $godot --headless --path godot -- --startup-smoke
& $godot --headless --path godot -- --management-smoke
& $godot --headless --path godot -- --display-sweep-smoke
```
Expected: build clean; `STARTUP SMOKE PASSED: 12 checks.`; the others pass unchanged (they skip both screens).

- [ ] **Step 5: Commit** (only if the user asked for commits)

```bash
git add godot/DebugMain.Startup.cs godot/DebugMain.StartupSmoke.cs godot/DebugMain.Management.cs
git commit -m "Start-up: AI disclaimer and first-launch volume screen"
```

---

### Task 4: Records and full verification

**Files:**
- Create: `docs/superpowers/startup-flow-and-volume-completion.md`
- Modify: `CLAUDE.md` (section 5), `docs/superpowers/music-system-completion.md` (volume now per computer)

- [ ] **Step 1: Write the records**

`startup-flow-and-volume-completion.md` in the style of `music-system-completion.md`: what changed (disclaimer, volume screen, per-computer settings, buses, Settings sliders and checkbox, career volume fields no longer used), what was verified (unit tests, startup smoke and other checks, stated separately), what was not (how the fades feel), and that the 13 Suno tracks are included. In `music-system-completion.md`, note that the Music slider is now per computer and capped by Master. CLAUDE.md section 5: one bullet. Convert changed Markdown to CRLF with `unix2dos`.

- [ ] **Step 2: Full verification**

Run:
```powershell
dotnet test tests/MangakaSim.Tests --filter "Category!=Playtest"
dotnet build MangakaGame.sln -warnaserror
foreach($f in 'smoke-test','management-smoke','alpha-smoke','atmosphere-smoke','journey-smoke','quiet-speed-smoke','music-smoke','startup-smoke','display-sweep-smoke'){ & $godot --headless --path godot -- "--$f" }
```
Expected: all pass.

- [ ] **Step 3: Commit** (only if the user asked for commits)

```bash
git add docs/superpowers CLAUDE.md
git commit -m "Start-up flow and volume: records"
```
