# Title Screen and Pause Menu Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace the menu panel over the office with a full-screen title screen (studio art, logo, menu column) that fades into and out of careers, add a small in-game pause menu with Quit to title, and keep a safety save whenever the player leaves a career.

**Architecture:** A full-screen `TitleScreen` Control is built in code as the last child of `DebugMain`, over the hidden office (Q43). A black curtain on its own CanvasLayer does every fade through one `Transition(change)` method. The existing sub-page builders (New Career, Load, Settings, Report) keep writing into `_menuContent`, which points at the title's page panel while the title is open and at the pause menu panel otherwise. Small engine-free helpers (fade lengths, the Continue caption, logo width) live in `src/MangakaSim/TitleScreen.cs` with xUnit tests; everything else is checked by a new `--title-smoke`.

**Tech Stack:** C# (.NET 8), Godot 4.7.2 .NET, xUnit.

**Spec:** `docs/superpowers/specs/2026-09-28-title-screen-and-pause-menu-design.md`

## Global Constraints

- Name: "Mangaka Days". Fallback logo text exactly `MANGAKA DAYS`; tagline exactly "A career told one page at a time."; footer "Private alpha · " + build, as now.
- Fades: to black about 0.6 s, from black about 1 s; Reduced interface motion makes both about 0.2 s. Clicks and keys are ignored while a fade runs.
- Title menu order: Continue (only when a readable save exists, with "Studio · d MMM yyyy" under it), New Career, Load Career, Settings, Report a problem, Quit. The first shown is the main button (`PrimaryAction`).
- Pause menu order: Resume, Save, Load Career, Settings, Report a problem, Quit to title.
- Title art slots: `res://Assets/Branding/title-background.(jpg|png|webp)` and `res://Assets/Branding/logo.png`; missing or unreadable falls back to the dark colour `#1d2a30` and the text logo, logged to the timeline.
- Art fills the window (cover), dark gradient on the left to clear at 55% width, slow push-in of 3% over 40 s and back, off with Reduced interface motion.
- Logo width `min(30% of window width, 32% of window height x logo aspect)`.
- Title music plays only while the title screen is open; the pause menu keeps the career music.
- Safety save ("Progress kept.") before Quit to title, loading another career from the pause menu, and the window close button, only while a career is in play. A failed save keeps the player in the career; a second close request quits anyway.
- Title-screen Settings: computer-wide only (Dark mode, sound sliders, "Play sound even while unfocused"). Per-career options only from the pause menu.
- Presentation only: no simulation, balance or save-format change. The save folder stays `app_userdata/MangakaGame`.
- Docs CRLF, code LF, metric, no em dashes. Do not commit: the user commits. Builds, tests and Godot checks are authorized (2026-09-28); packaging is not.
- Commands (PowerShell, repository root):
  ```powershell
  $godot = Join-Path $env:LOCALAPPDATA 'Microsoft\WinGet\Packages\GodotEngine.GodotEngine.Mono_Microsoft.Winget.Source_8wekyb3d8bbwe\Godot_v4.7.2-stable_mono_win64\Godot_v4.7.2-stable_mono_win64_console.exe'
  dotnet build MangakaGame.sln -warnaserror
  & $godot --headless --editor --path godot --import --quit
  & $godot --headless --path godot -- --title-smoke
  ```

## Review Focus

1. A player on 150% text at 1280 x 720 sees the whole menu column without clipping: pinned by the title smoke's `Inspect()` loop at that size (Task 3).
2. Pressing Enter on a focused title button during a fade must not start a second career or run a second save: pinned by the double Begin career check (Task 3) and `Transition` ignoring requests while `Fading` (Task 2).
3. A player with real saves who runs any automated check must never have a save loaded or written: the startup smoke presses its own Continue (Task 2), and the title smoke uses temporary stores (Tasks 3 to 5).
4. A load that fails behind the curtain (damaged save) must leave the player on the title screen with a message, not on a black screen: pinned by the curtain's `finally` in `Transition` (Task 2) and the damaged-Continue check (Task 3).
5. Leaving a career while a furniture draft is open must not silently discard it: `TrySafetySave` refuses with the furniture message (Task 4). No automated check sets up a furniture draft here; the menu already refuses to open during one, so this is verified by reading the code.

---

### Task 1: TitleScreen helpers (engine-free)

**Files:**
- Create: `src/MangakaSim/TitleScreen.cs`
- Test: `tests/MangakaSim.Tests/TitleScreenTests.cs`

**Interfaces:**
- Produces: `static class TitleScreen` with `const double FadeOutSeconds = .6`, `FadeInSeconds = 1.0`, `ReducedFadeSeconds = .2`, `PushInScale = 1.03`, `PushInSeconds = 40`; `static double FadeSeconds(bool fadeIn, bool reducedMotion)`; `static string ContinueLabel(CareerSaveInfo save)`; `static double LogoWidth(double width, double height, double logoAspect)`.

- [ ] **Step 1: Write the failing tests**

`tests/MangakaSim.Tests/TitleScreenTests.cs`:

```csharp
using Xunit;
namespace MangakaSim.Tests;

public class TitleScreenTests
{
    [Fact] public void Fades_are_short_out_and_gentle_in()
    {
        Assert.Equal(.6, TitleScreen.FadeSeconds(fadeIn: false, reducedMotion: false));
        Assert.Equal(1.0, TitleScreen.FadeSeconds(fadeIn: true, reducedMotion: false));
    }

    [Fact] public void Reduced_motion_shortens_both_fades()
    {
        Assert.Equal(.2, TitleScreen.FadeSeconds(false, true));
        Assert.Equal(.2, TitleScreen.FadeSeconds(true, true));
    }

    [Fact] public void Continue_caption_names_the_studio_and_game_date()
    {
        var save = new CareerSaveInfo("c", "s", "Daily autosave", true, new DateTime(2026, 9, 28), new DateTime(1997, 6, 3), "Haruka Studio");
        Assert.Equal("Haruka Studio · 3 Jun 1997", TitleScreen.ContinueLabel(save));
    }

    [Theory]
    [InlineData(1920, 1080, 552.96)]  // 16:9: height-limited
    [InlineData(3440, 1440, 737.28)]  // 21:9: height-limited, never swamps the screen
    [InlineData(1280, 720, 368.64)]
    [InlineData(1000, 1200, 300)]     // tall window: width-limited
    public void Logo_width_follows_the_window_not_the_text_size(double width, double height, double expected) =>
        Assert.Equal(expected, TitleScreen.LogoWidth(width, height, 1.6), 2);
}
```

- [ ] **Step 2: Run the tests to verify they fail**

Run: `dotnet test tests/MangakaSim.Tests --filter FullyQualifiedName~TitleScreenTests`
Expected: build FAILS with `CS0103: The name 'TitleScreen' does not exist`.

- [ ] **Step 3: Write the implementation**

`src/MangakaSim/TitleScreen.cs`:

```csharp
using System.Globalization;

namespace MangakaSim;

/// <summary>Title screen timings and sizes (spec 2026-09-28), kept engine-free so they can be unit tested.</summary>
public static class TitleScreen
{
    public const double FadeOutSeconds = .6, FadeInSeconds = 1.0, ReducedFadeSeconds = .2;
    public const double PushInScale = 1.03, PushInSeconds = 40;

    public static double FadeSeconds(bool fadeIn, bool reducedMotion) =>
        reducedMotion ? ReducedFadeSeconds : fadeIn ? FadeInSeconds : FadeOutSeconds;

    public static string ContinueLabel(CareerSaveInfo save) =>
        $"{save.Studio} · {save.GameDate.ToString("d MMM yyyy", CultureInfo.InvariantCulture)}";

    // About 30% of the window width, but never taller than about a third of the window, so wide and short screens keep room for the menu.
    public static double LogoWidth(double width, double height, double logoAspect) =>
        Math.Min(width * .30, height * .32 * logoAspect);
}
```

- [ ] **Step 4: Run the tests to verify they pass**

Run: `dotnet test tests/MangakaSim.Tests --filter FullyQualifiedName~TitleScreenTests`
Expected: PASS, 7 tests (2 facts + 1 fact + 4 theory rows).

---

### Task 2: The curtain and fades

**Files:**
- Create: `godot/DebugMain.Transitions.cs`
- Create: `godot/DebugMain.TitleSmoke.cs`
- Modify: `godot/DebugMain.Startup.cs` (Continue fades out; EndStartup lifts the curtain)
- Modify: `godot/DebugMain.Management.cs:165-166` (build the curtain at launch) and `:171` (keys wait for fades)
- Modify: `godot/DebugMain.StartupSmoke.cs:50` (press the volume screen's own Continue)
- Modify: `godot/DebugMain.cs:107` (dispatch `--title-smoke`)

**Interfaces:**
- Consumes: `TitleScreen.FadeSeconds(bool, bool)` (Task 1).
- Produces: `void BuildCurtain(bool startBlack)`, `void FadeIn()`, `bool Transition(Action change)` (false when ignored), `bool Fading`, `bool _instantFades` (true in smoke runs), `double FadeSeconds(bool fadeIn)`, `Task WaitSeconds(double)`, `async void RunTitleSmoke()` calling `CheckTitleFades()`.

- [ ] **Step 1: Write the failing smoke check**

`godot/DebugMain.TitleSmoke.cs`:

```csharp
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using MangakaSim;

namespace MangakaGame;

public partial class DebugMain
{
    // Title screen and pause menu (spec 2026-09-28). Uses temporary career stores, never the player's saves.
    private async void RunTitleSmoke()
    {
        SetProcess(false);
        try
        {
            GetWindow().Size = new(1920, 1080); Directory.CreateDirectory(SmokeOutput);
            await SettleUi();
            await CheckTitleFades();
            GD.Print($"TITLE SMOKE PASSED: {_smokeChecks} checks.");
            var tree = GetTree(); tree.CreateTimer(.1).Timeout += () => tree.Quit(); QueueFree();
        }
        catch (Exception ex) { GD.PushError($"TITLE SMOKE FAILED: {ex.Message}\n{ex.StackTrace}"); GetTree().Quit(1); }
    }

    private async Task WaitSeconds(double seconds) => await ToSignal(GetTree().CreateTimer(seconds), SceneTreeTimer.SignalName.Timeout);

    private async Task CheckTitleFades()
    {
        _instantFades = false; _presentation.ReducedUiMotion = false;
        var ran = 0;
        Check(Transition(() => ran++) && Fading && ran == 0, "A transition fades to black before changing the screen");
        Check(!Transition(() => ran += 10), "A second request during a fade is ignored");
        await WaitSeconds(.8);
        Check(ran == 1 && Fading, "The change happens behind the curtain, then the picture fades back in");
        await WaitSeconds(1.2);
        Check(!Fading, "The fade finishes and the screen takes input again");
        _presentation.ReducedUiMotion = true;
        Transition(() => ran++); await WaitSeconds(.6);
        Check(ran == 2 && !Fading, "Reduced interface motion shortens both fades to about 0.2 seconds");
        _presentation.ReducedUiMotion = false;
        // A first launch: the volume screen fades to black, then the screen beneath fades in.
        _curtain!.Modulate = Colors.White; _curtain.Show();
        BeginStartup(true); await SettleUi(); SkipDisclaimer(); await SettleUi();
        var cont = _volumeSetup!.FindChildren("*", "Button", true, false).OfType<Button>().Single(b => b.Text == "Continue");
        cont.EmitSignal(BaseButton.SignalName.Pressed); cont.EmitSignal(BaseButton.SignalName.Pressed);
        Check(_startup is not null, "The volume screen fades out instead of vanishing");
        await WaitSeconds(.8);
        Check(_startup is null && Fading, "Then the screen beneath fades in from black");
        await WaitSeconds(1.2);
        Check(!Fading, "The fade in from the start-up screens finishes");
        _instantFades = true;
    }
}
```

In `godot/DebugMain.cs`, after the `--startup-smoke` line (line 107), add:

```csharp
        if (OS.GetCmdlineUserArgs().Contains("--title-smoke")) CallDeferred(nameof(RunTitleSmoke));
```

- [ ] **Step 2: Build to verify it fails**

Run: `dotnet build MangakaGame.sln -warnaserror`
Expected: FAIL with `CS0103` for `_instantFades`, `Transition`, `Fading` and `_curtain`.

- [ ] **Step 3: Write the curtain**

`godot/DebugMain.Transitions.cs`:

```csharp
using System;
using Godot;
using MangakaSim;

namespace MangakaGame;

public partial class DebugMain
{
    // Fades between the start-up screens, the title screen and the office (spec 2026-09-28).
    private CanvasLayer? _curtainLayer;
    private ColorRect? _curtain;
    private Tween? _curtainTween;
    // Automated checks skip the fades unless a check is timing them.
    private bool _instantFades = SmokeRun;
    private bool Fading => _curtain is { Visible: true };

    private void BuildCurtain(bool startBlack)
    {
        // Above the office, the title screen and the speed flash; below the start-up screens (layer 100).
        _curtainLayer = new CanvasLayer { Layer = 90 }; AddChild(_curtainLayer);
        _curtain = new ColorRect { Color = Colors.Black, MouseFilter = MouseFilterEnum.Stop, Visible = startBlack, Modulate = new Color(1, 1, 1, startBlack ? 1 : 0) };
        _curtain.SetAnchorsPreset(LayoutPreset.FullRect); _curtainLayer.AddChild(_curtain);
    }

    private double FadeSeconds(bool fadeIn) => _instantFades ? 0 : TitleScreen.FadeSeconds(fadeIn, _presentation.ReducedUiMotion);

    // Lifts the curtain from whatever it covers now.
    private void FadeIn()
    {
        if (_curtain is null) return;
        _curtainTween?.Kill();
        var seconds = FadeSeconds(true);
        if (seconds <= 0) { _curtain.Modulate = new Color(1, 1, 1, 0); _curtain.Hide(); return; }
        _curtain.Show();
        _curtainTween = CreateTween();
        _curtainTween.TweenProperty(_curtain, "modulate:a", 0f, seconds);
        _curtainTween.TweenCallback(Callable.From(_curtain.Hide));
    }

    // Fades to black, makes the change out of sight, then fades back in. A request while a fade runs is ignored,
    // so a double click or a held key cannot start two careers. The curtain always lifts, even if the change fails.
    private bool Transition(Action change)
    {
        if (_curtain is null) { change(); return true; }
        if (Fading) return false;
        void Behind() { try { change(); } finally { FadeIn(); } }
        var seconds = FadeSeconds(false);
        _curtainTween?.Kill(); _curtain.Show();
        if (seconds <= 0) { _curtain.Modulate = Colors.White; Behind(); return true; }
        _curtain.Modulate = new Color(1, 1, 1, 0);
        _curtainTween = CreateTween();
        _curtainTween.TweenProperty(_curtain, "modulate:a", 1f, seconds);
        _curtainTween.TweenCallback(Callable.From(Behind));
        return true;
    }
}
```

- [ ] **Step 4: Fade the start-up screens**

In `godot/DebugMain.Startup.cs`:

Change the field line `private bool _disclaimerDone, _startupShowVolume;` to:

```csharp
    private bool _disclaimerDone, _startupShowVolume, _startupEnding;
```

In `BeginStartup`, change the first line to reset the new flag:

```csharp
        _startupShowVolume = showVolume; _disclaimerDone = false; _volumeSetup = null; _startupEnding = false;
```

Replace the Continue line in `ShowVolumeSetup` with:

```csharp
        ActionButton(box, "Continue", () =>
        {
            if (_startupEnding) return;
            _startupEnding = true; _audioSettings.SetupDone = true; SaveAudioSettings(); FadeOutStartup();
        });
```

Add after `ShowVolumeSetup`:

```csharp
    // The volume screen fades to black; the curtain beneath then fades up on the title screen (spec 2026-09-28).
    private void FadeOutStartup()
    {
        var seconds = FadeSeconds(false);
        if (seconds <= 0 || _startupRoot is null) { EndStartup(); return; }
        var tween = CreateTween();
        tween.TweenProperty(_startupRoot, "modulate:a", 0f, seconds);
        tween.TweenCallback(Callable.From(EndStartup));
    }
```

Replace `EndStartup` with:

```csharp
    private void EndStartup()
    {
        _startupSilent = false; ApplyAudioSettings();
        _startup?.QueueFree(); _startup = null; _startupRoot = null;
        FadeIn();
    }
```

- [ ] **Step 5: Build the curtain at launch and hold keys during fades**

In `godot/DebugMain.Management.cs`, replace lines 165-166:

```csharp
        _viewLocation=_state.Protagonist.Employment!.LocationId;RefreshManagement();ShowMenu();
        if(!SmokeRun)BeginStartup(!_audioSettings.SetupDone||OS.GetCmdlineUserArgs().Contains("--first-launch"));
```

with:

```csharp
        _viewLocation=_state.Protagonist.Employment!.LocationId;RefreshManagement();ShowMenu();
        // A real launch starts black behind the start-up screens; the title screen fades up when they end.
        BuildCurtain(startBlack:!SmokeRun);
        if(!SmokeRun)BeginStartup(!_audioSettings.SetupDone||OS.GetCmdlineUserArgs().Contains("--first-launch"));
```

and replace line 171:

```csharp
        if(_startup is not null)return; // keys belong to the start-up screens
```

with:

```csharp
        if(_startup is not null||Fading)return; // keys belong to the start-up screens, and wait for fades
```

- [ ] **Step 6: Keep the startup smoke off any title button named Continue**

In `godot/DebugMain.StartupSmoke.cs`, replace line 50 `Press("Continue"); await SettleUi();` with:

```csharp
            // Press the volume screen's own Continue: the title screen may also show one when the player has saves.
            _volumeSetup!.FindChildren("*", "Button", true, false).OfType<Button>().Single(b => b.Text == "Continue").EmitSignal(BaseButton.SignalName.Pressed); await SettleUi();
```

- [ ] **Step 7: Build and run the checks**

Run:
```powershell
dotnet build MangakaGame.sln -warnaserror
& $godot --headless --path godot -- --title-smoke
& $godot --headless --path godot -- --startup-smoke
```
Expected: build succeeds with 0 warnings; `TITLE SMOKE PASSED: 8 checks.`; `STARTUP SMOKE PASSED: 16 checks.`

---

### Task 3: The title screen

**Files:**
- Create: `godot/DebugMain.Title.cs`
- Modify: `godot/DebugMain.cs:110-112` (title music follows the title screen)
- Modify: `godot/DebugMain.Management.cs:13-17` fields, `:157` (keep the pause menu container), `:165` (open the title at launch), `:168` (notices reach the title), `:171-172` (Escape does nothing on the title)
- Modify: `godot/DebugMain.ManagementMenus.cs` (`ShowMenu` dispatch, `NewCareerMenu`, `SettingsMenu`, `OpenSaveMenu`, `LoadCareerMenu`, `ResetManagementSession`)
- Modify: `godot/DebugMain.Alpha.cs` (`AlphaSettings` career flag, `ReportProblem` panel and attachment)
- Modify: `godot/DebugMain.Gui.cs:107` (lay out the title on resize)
- Modify: `godot/DebugMain.DisplaySweepSmoke.cs` (`Inspect` skips the moving art)
- Modify: `godot/DebugMain.MusicSmoke.cs:66-71`, `godot/DebugMain.ManagementSmoke.cs:16,20`
- Modify: `godot/DebugMain.TitleSmoke.cs` (add `CheckTitleScreen`)

**Interfaces:**
- Consumes: `Transition(Action)`, `_instantFades`, `Fading`, `WaitSeconds` (Task 2); `TitleScreen.ContinueLabel`, `TitleScreen.LogoWidth`, `TitleScreen.PushInScale`, `TitleScreen.PushInSeconds` (Task 1).
- Produces: `bool TitleOpen`, `void OpenTitle()`, `void CloseTitle()`, `void ShowTitleMenu()`, `void OpenMenuPanel()`, `void LayoutTitle()`, `void EnterCareer(Action load)`, `void ContinueLatest()`, fields `_titleArt`, `_titleLogo`, `_titleColumn`, `_titlePage`, `_titleShade`, `_titleMenuBox`, `_titlePageContent`, `_titleNotice`, `_titlePushIn`, `_brandingFolder`, `_pauseMenuContent`; `string[] TitleButtons()`.

- [ ] **Step 1: Write the failing smoke checks**

In `godot/DebugMain.TitleSmoke.cs`, add `await CheckTitleScreen();` on the line after `await CheckTitleFades();`, and add these methods to the class:

```csharp
    private string[] TitleButtons() => _titleMenuBox!.GetChildren().OfType<Button>().Select(b => b.Text).ToArray();
    private bool TitleShows(string text) => _title!.FindChildren("*", "Label", true, false).OfType<Label>().Any(l => l.Text == text);

    private async Task CheckTitleScreen()
    {
        Check(TitleOpen && _inMenu && MusicNow().InMenu, "The game opens on the title screen, paused, on the title music");
        _careers = new CareerStore(Path.Combine(SmokeOutput, "title-" + Guid.NewGuid().ToString("N")));
        OpenTitle(); await SettleUi();
        Check(TitleButtons().SequenceEqual(new[] { "New Career", "Load Career", "Settings", "Report a problem", "Quit" })
            && ButtonNamed("New Career").ThemeTypeVariation == "PrimaryAction", "Without saves, Continue is hidden and New Career leads");
        Check(_titleArt?.Texture is not null && _titleLogo?.Texture is not null, "The placeholder art and logo load from Assets/Branding");
        Check(TitleShows("A career told one page at a time.") && TitleShows("Private alpha · " + ProblemReport.Build), "Tagline and build line show");
        foreach (var (size, scale) in new[] { (new Vector2I(1920, 1080), 1d), (new Vector2I(2560, 1080), 1d), (new Vector2I(3440, 1440), 1d), (new Vector2I(1680, 1050), 1d), (new Vector2I(1280, 720), 1.5) })
        {
            await Resize(size, scale); OpenTitle(); await SettleUi(); await SettleUi();
            var label = $"{size.X}x{size.Y} at {scale * 100}% text";
            Check(_titleArt!.StretchMode == TextureRect.StretchModeEnum.KeepAspectCovered && _titleArt.Size.IsEqualApprox(GetViewportRect().Size), $"The studio art covers the whole window at {label}");
            Check(_titleColumn!.GetGlobalRect().End.X <= size.X * .56f, $"The logo and menu stay on the dark side at {label}");
            var problems = Inspect();
            Check(problems.Count == 0, $"The title screen fits at {label}: {string.Join("; ", problems)}");
            await CaptureSmokeImage($"title-{size.X}x{size.Y}-{scale * 100:0}");
        }
        await Resize(new(1920, 1080), 1);
        // Sub-pages open in the page panel; title Settings are this computer's only.
        OpenTitle(); Press("Settings"); await SettleUi();
        Check(_titlePage!.Visible && !_titleColumn!.Visible && _menuContent == _titlePageContent, "Settings opens in the page panel over the title art");
        var boxes = _menuContent.FindChildren("*", "CheckBox", true, false).OfType<CheckBox>().Select(c => c.Text).ToArray();
        Check(boxes.Contains("Dark mode") && boxes.Contains("Play sound even while unfocused") && !boxes.Any(t => t.StartsWith("Helper-Chan"))
            && !_menuContent.FindChildren("*", "Button", true, false).OfType<Button>().Any(b => b.Text == "Difficulty & Sandbox"), "Title settings show only this computer's settings");
        await CaptureSmokeImage("title-settings");
        Press("Back"); await SettleUi();
        Check(_titleColumn.Visible && !_titlePage.Visible, "Back returns to the menu column");
        Press("Report a problem"); await SettleUi();
        Check(_menuContent.GetChildren().OfType<CheckBox>().Single(c => c.Text.StartsWith("Attach career")).Disabled, "A report from the title screen cannot attach a career");
        Press("Cancel"); await SettleUi();
        Input.ParseInputEvent(new InputEventKey { Keycode = Key.Escape, Pressed = true }); await SettleUi();
        Check(TitleOpen && _titleColumn!.Visible && !_menu.Visible, "Escape does nothing on the title screen");
        // Motion and fallbacks.
        Check(_titlePushIn is { } push && push.IsRunning(), "The studio art slowly pushes in");
        _presentation.ReducedUiMotion = true; OpenTitle(); await SettleUi();
        Check(_titlePushIn is null && _titleArt!.Scale == Vector2.One, "Reduced interface motion keeps the art still");
        _presentation.ReducedUiMotion = false;
        _brandingFolder = "res://Assets/NoSuchFolder"; OpenTitle(); await SettleUi();
        Check(_titleArt is null && _titleLogo is null && TitleShows("MANGAKA DAYS"), "Missing art falls back to the plain backdrop and a text logo");
        _brandingFolder = "res://Assets/Branding";
        // Into a career and back: a double press starts only one career.
        OpenTitle(); NewCareerMenu(); _instantFades = false;
        Press("Begin career"); Press("Begin career");
        await WaitSeconds(2.0);
        Check(!TitleOpen && !Fading && !_inMenu && !MusicNow().InMenu && _careers.List().Select(s => s.Career).Distinct().Count() == 1,
            "Begin career fades into the office once, even when pressed twice, and starts the career music");
        _instantFades = true; _helperPopup.Hide();
        OpenTitle(); await SettleUi();
        var latest = _careers.List()[0];
        Check(TitleButtons()[0] == "Continue" && ButtonNamed("Continue").ThemeTypeVariation == "PrimaryAction" && TitleShows(TitleScreen.ContinueLabel(latest)),
            "With a save, Continue leads and names the latest career");
        await CaptureSmokeImage("title-continue");
        var loaded = _careerId; _careerId = Guid.NewGuid().ToString("N");
        Press("Continue"); await SettleUi();
        Check(!TitleOpen && _careerId == loaded, "Continue loads the latest career");
        // A load that fails behind the curtain (here, nothing left to load): the player stays on the title screen
        // with the reason, not on a black screen.
        _careers = new CareerStore(Path.Combine(SmokeOutput, "title-empty-" + Guid.NewGuid().ToString("N"))); OpenTitle();
        ContinueLatest(); await SettleUi();
        Check(TitleOpen && !Fading && _titleNotice!.Text.StartsWith("No saved careers yet"), "A career that cannot be loaded leaves the player on the title screen with the reason");
        _helperPopup.Hide();
    }
```

In `godot/DebugMain.DisplaySweepSmoke.cs`, in `Inspect()`, after `if(_speedFlash is not null)excluded.Add(_speedFlash);` add:

```csharp
        if(_titleArt is not null)excluded.Add(_titleArt); // scaled a little past the window by the slow push-in, by design
```

- [ ] **Step 2: Build to verify it fails**

Run: `dotnet build MangakaGame.sln -warnaserror`
Expected: FAIL with `CS0103` for `TitleOpen`, `OpenTitle`, `_titleArt` and the other title fields.

- [ ] **Step 3: Write the title screen**

`godot/DebugMain.Title.cs`:

```csharp
using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using Godot;
using MangakaSim;

namespace MangakaGame;

public partial class DebugMain
{
    // The title screen: a full-screen layer over the hidden office (spec 2026-09-28, Q43). Rebuilt each time it opens.
    private Control? _title;
    private TextureRect? _titleArt, _titleLogo;
    private ScrollContainer? _titleColumn;
    private PanelContainer? _titlePage;
    private ColorRect? _titleShade;
    private VBoxContainer? _titleMenuBox, _titlePageContent;
    private Label? _titleNotice;
    private Tween? _titlePushIn;
    private float _titleLogoAspect = 1840f / 1152f;
    // Commissioned art replaces the files in this folder with no code change.
    private string _brandingFolder = "res://Assets/Branding";
    private VBoxContainer _pauseMenuContent = null!;
    private bool TitleOpen => _title is not null;

    private Texture2D? LoadBranding(string name, params string[] extensions)
    {
        foreach (var extension in extensions)
        {
            var path = $"{_brandingFolder}/{name}.{extension}";
            if (ResourceLoader.Exists(path) && ResourceLoader.Load<Texture2D>(path) is { } texture) return texture;
        }
        LogTimeline("error title art missing: " + name);
        return null;
    }

    private void OpenTitle()
    {
        CloseTitle();
        Pause(); _inMenu = true; _menu.Hide(); _helperPopup.Hide(); _recapDialog.Hide(); ClosePhone(); _speedFlash?.Hide();
        // ZIndex keeps it above the office nametags, which draw with z 50.
        _title = new Control { Name = "TitleScreen", MouseFilter = MouseFilterEnum.Stop, ZIndex = 60 };
        _title.SetAnchorsPreset(LayoutPreset.FullRect); AddChild(_title);
        var wash = new ColorRect { Color = new Color("1d2a30"), MouseFilter = MouseFilterEnum.Ignore };
        wash.SetAnchorsPreset(LayoutPreset.FullRect); _title.AddChild(wash);
        if (LoadBranding("title-background", "jpg", "png", "webp") is { } art)
        {
            _titleArt = new TextureRect { Texture = art, ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize, StretchMode = TextureRect.StretchModeEnum.KeepAspectCovered, MouseFilter = MouseFilterEnum.Ignore };
            _titleArt.SetAnchorsPreset(LayoutPreset.FullRect); _title.AddChild(_titleArt);
            _titleArt.Resized += () => { if (_titleArt is not null) _titleArt.PivotOffset = _titleArt.Size / 2; };
            StartPushIn();
        }
        var gradient = new TextureRect
        {
            Texture = new GradientTexture2D { Gradient = new Gradient { Offsets = [0f, .55f], Colors = [new Color(0, 0, 0, .78f), new Color(0, 0, 0, 0)] }, Width = 256, Height = 4 },
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize, StretchMode = TextureRect.StretchModeEnum.Scale, MouseFilter = MouseFilterEnum.Ignore,
        };
        gradient.SetAnchorsPreset(LayoutPreset.FullRect); _title.AddChild(gradient);
        _titleShade = new ColorRect { Color = new Color(0, 0, 0, .45f), MouseFilter = MouseFilterEnum.Ignore, Visible = false };
        _titleShade.SetAnchorsPreset(LayoutPreset.FullRect); _title.AddChild(_titleShade);
        _titleColumn = new ScrollContainer { HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled }; _title.AddChild(_titleColumn);
        var box = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill }; _titleColumn.AddChild(box);
        if (LoadBranding("logo", "png") is { } logo)
        {
            _titleLogoAspect = logo.GetWidth() / (float)logo.GetHeight();
            _titleLogo = new TextureRect { Texture = logo, ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize, StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered, SizeFlagsHorizontal = SizeFlags.ShrinkBegin };
            box.AddChild(_titleLogo);
        }
        else Words(box, "MANGAKA DAYS", 44);
        Words(box, "A career told one page at a time.", 18);
        _titleMenuBox = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill }; box.AddChild(_titleMenuBox);
        _titleNotice = Words(box, "", 15);
        var footer = Words(_title, "Private alpha · " + ProblemReport.Build, 13);
        footer.AutowrapMode = TextServer.AutowrapMode.Off; footer.SetAnchorsAndOffsetsPreset(LayoutPreset.BottomLeft, LayoutPresetMode.Minsize, 24);
        _titlePage = new PanelContainer { Visible = false }; _titlePage.SetAnchorsPreset(LayoutPreset.FullRect); _title.AddChild(_titlePage);
        var pageScroll = new ScrollContainer { HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled }; _titlePage.AddChild(pageScroll);
        _titlePageContent = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill }; pageScroll.AddChild(_titlePageContent);
        // New Career, Load, Settings and Report keep writing into _menuContent; on the title screen that is the page panel.
        _menuContent = _titlePageContent;
        ShowTitleMenu(); LayoutTitle();
    }

    private void CloseTitle()
    {
        if (_title is null) return;
        RemoveChild(_title); _title.QueueFree();
        _title = null; _titleArt = _titleLogo = null; _titleColumn = null; _titlePage = null; _titleShade = null;
        _titleMenuBox = _titlePageContent = null; _titleNotice = null; _titlePushIn = null;
        _menuContent = _pauseMenuContent;
    }

    private void StartPushIn()
    {
        _titlePushIn = null;
        if (_titleArt is null) return;
        _titleArt.Scale = Vector2.One;
        if (_presentation.ReducedUiMotion) return;
        _titlePushIn = _titleArt.CreateTween().SetLoops();
        _titlePushIn.TweenProperty(_titleArt, "scale", Vector2.One * (float)TitleScreen.PushInScale, TitleScreen.PushInSeconds).SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.InOut);
        _titlePushIn.TweenProperty(_titleArt, "scale", Vector2.One, TitleScreen.PushInSeconds).SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.InOut);
    }

    // The menu column: Continue only when a readable save exists; the first button shown is the main one.
    private void ShowTitleMenu()
    {
        if (_title is null) return;
        _titlePage!.Hide(); _titleShade!.Hide(); _titleColumn!.Show();
        Empty(_titlePageContent!); Empty(_titleMenuBox!);
        Pause(); _inMenu = true;
        var latest = _careers.List().FirstOrDefault();
        Button? main = null;
        if (latest is not null) { main = ActionButton(_titleMenuBox!, "Continue", ContinueLatest); Words(_titleMenuBox!, TitleScreen.ContinueLabel(latest), 14); }
        var start = ActionButton(_titleMenuBox!, "New Career", NewCareerMenu); main ??= start;
        ActionButton(_titleMenuBox!, "Load Career", LoadCareerMenu);
        ActionButton(_titleMenuBox!, "Settings", SettingsMenu);
        ActionButton(_titleMenuBox!, "Report a problem", ReportProblem);
        ActionButton(_titleMenuBox!, "Quit", () => { _timeline?.End(); GetTree().Quit(); });
        main.ThemeTypeVariation = "PrimaryAction"; main.CallDeferred(Control.MethodName.GrabFocus);
    }

    // Shows the panel that the sub-page builders fill: the title's page panel, or the pause menu panel in a career.
    private void OpenMenuPanel()
    {
        Pause(); _inMenu = true;
        if (_title is not null) { _titleColumn!.Hide(); _titleShade!.Show(); _titlePage!.Show(); return; }
        _menu.Show();
    }

    private void LayoutTitle()
    {
        if (_title is null || _titleColumn is null) return;
        var size = GetViewportRect().Size;
        var logoWidth = (float)TitleScreen.LogoWidth(size.X, size.Y, _titleLogoAspect);
        if (_titleLogo is not null) _titleLogo.CustomMinimumSize = new Vector2(logoWidth, logoWidth / _titleLogoAspect);
        var left = Math.Max(32, size.X * .06f); var top = Math.Max(24, size.Y * .06f);
        _titleColumn.Position = new Vector2(left, top);
        _titleColumn.Size = new Vector2(Math.Max(logoWidth, 360 * (float)_presentation.UiScale), Math.Max(200, size.Y - top - 56));
        if (_titlePage is null) return;
        var inset = Math.Max(24, (size.X - 1040) / 2);
        _titlePage.OffsetLeft = inset; _titlePage.OffsetRight = -inset; _titlePage.OffsetTop = 40; _titlePage.OffsetBottom = -40;
    }

    // From the title screen, starting, continuing or loading a career fades through black. A load that fails
    // leaves the player where they were, with the reason.
    private void EnterCareer(Action load)
    {
        void Load()
        {
            try { load(); }
            catch (Exception ex) when (ex is IOException or InvalidDataException or JsonException or InvalidCommandException or UnauthorizedAccessException)
            { LogTimeline("error " + TimelineRedactor.Clean(ex.Message, _state)); Notify(ex.Message); }
        }
        Transition(Load);
    }

    private void ContinueLatest() => EnterCareer(() =>
    {
        string? failure = null;
        foreach (var save in _careers.List())
            try { LoadCareer(save); if (failure is not null) Notify("Loaded an earlier valid snapshot. The latest could not be read: " + failure); return; }
            catch (Exception ex) when (ex is IOException or InvalidDataException or JsonException) { failure = ex.Message; }
        throw new InvalidDataException(failure ?? "No saved careers yet. Choose New Career to begin.");
    });
}
```

- [ ] **Step 4: Wire the title into the management screen**

In `godot/DebugMain.Management.cs`:

Replace line 157:

```csharp
        var menuScroll=new ScrollContainer{HorizontalScrollMode=ScrollContainer.ScrollMode.Disabled};_menu.AddChild(menuScroll);_menuContent=new VBoxContainer{SizeFlagsHorizontal=SizeFlags.ExpandFill};menuScroll.AddChild(_menuContent);
```

with:

```csharp
        var menuScroll=new ScrollContainer{HorizontalScrollMode=ScrollContainer.ScrollMode.Disabled};_menu.AddChild(menuScroll);_menuContent=new VBoxContainer{SizeFlagsHorizontal=SizeFlags.ExpandFill};menuScroll.AddChild(_menuContent);
        _pauseMenuContent=_menuContent;
```

Replace the launch line `_viewLocation=_state.Protagonist.Employment!.LocationId;RefreshManagement();ShowMenu();` with:

```csharp
        _viewLocation=_state.Protagonist.Employment!.LocationId;RefreshManagement();OpenTitle();
```

Replace `Notify` (line 168) with:

```csharp
    private void Notify(string text)
    {
        if(!_managementReady){LogLine(text);return;}
        _notice.Text=text;_workbenchNotice.Text=text;_workbenchNotice.Show();
        if(_titleNotice is not null)_titleNotice.Text=text;
    }
```

After the start-up guard in `_UnhandledKeyInput` (the `if(_startup is not null||Fading)return;` line), add:

```csharp
        if(TitleOpen){if(ev is InputEventKey{Keycode:Key.Escape})GetViewport().SetInputAsHandled();return;} // nothing to go back to
```

In `godot/DebugMain.cs`, replace lines 110-112:

```csharp
    // Only the main menu plays the title music; the Save screen keeps the career rotation (final review).
    private bool _titleMenu=true;
    private MusicContext MusicNow()=>new(_inMenu&&_titleMenu,_state.Clock.Now,
```

with:

```csharp
    // Only the title screen plays the title music; the pause menu keeps the career rotation (spec 2026-09-28).
    private MusicContext MusicNow()=>new(TitleOpen,_state.Clock.Now,
```

In `godot/DebugMain.Gui.cs`, after the `_menu` resize line (line 107), add:

```csharp
        LayoutTitle();
```

- [ ] **Step 5: Route the menus through the title**

In `godot/DebugMain.ManagementMenus.cs`:

At the top of `ShowMenu()`, before the `OfficeEditing` line, add:

```csharp
        if(TitleOpen){ShowTitleMenu();return;}
```

and in the same method change `Pause();_inMenu=true;_titleMenu=true;_menu.Show();Empty(_menuContent);` to:

```csharp
        OpenMenuPanel();Empty(_menuContent);
```

At the top of `NewCareerMenu()` change `Empty(_menuContent);Words(_menuContent,"A new career",30);` to:

```csharp
        OpenMenuPanel();Empty(_menuContent);Words(_menuContent,"A new career",30);
```

In `NewCareerMenu`, wrap the Begin career action so it fades from the title:

```csharp
        var begin=ActionButton(actions,"Begin career",()=>EnterCareer(()=>
        {
            _state=GameState.NewGame((int)seed.Value,rights.Selected==0?OwnershipMode.StudioRetention:OwnershipMode.CreatorRetention,name.Text);
            _state.Apply(difficulty());
            _state.Apply(new SetAppearanceCommand(SelectedLook()));
            _presentation=new(){Page="Office"};_careerId=Guid.NewGuid().ToString("N");
            LogTimeline($"new-career {CareerCode} {_state.Progression.Difficulty} sandbox={_state.Progression.EverSandbox}");
            ResetManagementSession();SaveCareer("The first page");ShowOffice();
        }));begin.ThemeTypeVariation="PrimaryAction";
```

Replace the start of `SettingsMenu()`:

```csharp
        Empty(_menuContent);Words(_menuContent,"Settings",30);
```

with:

```csharp
        OpenMenuPanel();Empty(_menuContent);Words(_menuContent,"Settings",30);
        // On the title screen no career is loaded, so only this computer's settings show (spec 2026-09-28).
        if(TitleOpen){var computer=StudioCard(_menuContent,"DISPLAY & SOUND");AlphaSettings(computer,career:false);ActionButton(_menuContent,"Back",ShowMenu);return;}
```

In `OpenSaveMenu()` change `Pause();_inMenu=true;_titleMenu=false;_menu.Show();Empty(_menuContent);` to:

```csharp
        OpenMenuPanel();Empty(_menuContent);
```

In `LoadCareerMenu()` change the first line to `OpenMenuPanel();Empty(_menuContent);Words(_menuContent,"Your careers",30);`, and wrap both ways in:

```csharp
        ActionButton(_menuContent,"Import career or previous save",()=>ChooseFile("Import career",FileDialog.FileModeEnum.OpenFile,["*.mangaka ; Portable career","*.json ; Previous save"],path=>EnterCareer(()=>
        {
            if(path.EndsWith(".mangaka",StringComparison.OrdinalIgnoreCase)){LoadCareer(_careers.Import(System.IO.File.ReadAllBytes(path)));LogTimeline($"import {CareerCode}");}
            else{_state=GameState.ImportSupported(System.IO.File.ReadAllText(path));_careerId=Guid.NewGuid().ToString("N");_presentation=new();LogTimeline($"import {CareerCode}");ResetManagementSession();SaveCareer("Imported career");}
        })));
        foreach(var save in _careers.List().Take(80)){var entry=save;ActionButton(_menuContent,$"{save.Name}   ·   {save.GameDate:d MMM yyyy}   ·   {save.Studio}"+(save.Auto?"   [auto]":""),()=>EnterCareer(()=>LoadCareer(entry)));}
```

At the start of `ResetManagementSession()` add:

```csharp
        CloseTitle(); // every way into a career (new, continue, load, import) passes through here
```

In `godot/DebugMain.Alpha.cs`:

Change `private void AlphaSettings(Control parent)` to `private void AlphaSettings(Control parent,bool career=true)`, wrap the guidance checkbox line in `if(career){ ... }`, and change the last line's text to:

```csharp
        Words(parent,"These settings apply to every career on this computer. Office ambience pauses in menus.",14);
```

In `ReportProblem()`, change `Pause();_inMenu=true;_menu.Show();Empty(_menuContent);` to `OpenMenuPanel();Empty(_menuContent);`, and after the `save` checkbox line add:

```csharp
        if(TitleOpen){save.Disabled=true;save.TooltipText="No career is open on the title screen.";}
```

- [ ] **Step 6: Update the checks that expected the old launch menu**

In `godot/DebugMain.ManagementSmoke.cs`, line 16: replace `_menu.Visible` with `TitleOpen`; line 20: replace `!_menu.Visible` with `!_menu.Visible&&!TitleOpen`.

In `godot/DebugMain.MusicSmoke.cs`, replace lines 66-71 (from the `// Final review:` comment through `_menu.Hide();_inMenu=false;`) with:

```csharp
            // Final review, updated for the title screen: only the title screen plays the title track.
            CloseTitle();_managementReady=true;OpenSaveMenu();await SettleUi();
            Check(!MusicNow().InMenu, "The Save screen keeps the career music");
            _menu.Hide();_inMenu=false;OpenTitle();await SettleUi();
            Check(MusicNow().InMenu, "The title screen plays the title music");
            CloseTitle();
```

- [ ] **Step 7: Import the art, build and run the checks**

Run:
```powershell
& $godot --headless --editor --path godot --import --quit
dotnet build MangakaGame.sln -warnaserror
& $godot --headless --path godot -- --title-smoke
& $godot --headless --path godot -- --management-smoke
& $godot --headless --path godot -- --music-smoke
& $godot --headless --path godot -- --startup-smoke
```
Expected: build succeeds with 0 warnings; `TITLE SMOKE PASSED: 45 checks.` (8 fade checks, 31 title checks and 6 window-size checks from `Resize`); management, music (13) and startup (16) smokes pass.

---

### Task 4: Pause menu, Quit to title and safety saves

**Files:**
- Modify: `godot/DebugMain.ManagementMenus.cs` (`ShowMenu` becomes the pause menu)
- Modify: `godot/DebugMain.Title.cs` (`EnterCareer` saves before leaving a career)
- Modify: `godot/DebugMain.Transitions.cs` (safety saves, Quit to title)
- Modify: `godot/DebugMain.Management.cs` (`Notify` reaches the pause menu; field)
- Modify: `godot/DebugMain.Gui.cs:107` (small pause panel)
- Modify: `godot/DebugMain.ManagementPanels.cs:282` (Helper-Chan settings link)
- Modify: `godot/DebugMain.AlphaSmoke.cs:86`, `godot/DebugMain.DisplaySweepSmoke.cs` (screen list, Reset)
- Modify: `godot/DebugMain.TitleSmoke.cs` (add `CheckPauseMenu`)

**Interfaces:**
- Consumes: `OpenTitle`, `CloseTitle`, `OpenMenuPanel`, `EnterCareer`, `TitleOpen` (Task 3); `Transition` (Task 2).
- Produces: `bool _safetySaves` (off in smoke runs), `bool CareerInPlay`, `bool TrySafetySave()`, `void QuitToTitle()`, `void ShowPauseMenu()`, `bool _menuCompact`, `Label? _pauseNotice`.

- [ ] **Step 1: Write the failing smoke checks**

In `godot/DebugMain.TitleSmoke.cs`, add `await CheckPauseMenu();` after `await CheckTitleScreen();`, and add:

```csharp
    private async Task CheckPauseMenu()
    {
        // In a career with safety saves switched on, as for a player.
        _careers = new CareerStore(Path.Combine(SmokeOutput, "title-pause-" + Guid.NewGuid().ToString("N")));
        OpenTitle(); NewCareerMenu(); Press("Begin career"); await SettleUi(); _helperPopup.Hide();
        _safetySaves = true;
        int Saves() => _careers.List().Count;
        Input.ParseInputEvent(new InputEventKey { Keycode = Key.Escape, Pressed = true }); await SettleUi();
        var items = _menuContent.GetChildren().OfType<Button>().Select(b => b.Text).ToArray();
        Check(_menu.Visible && items.SequenceEqual(new[] { "Resume", "Save", "Load Career", "Settings", "Report a problem", "Quit to title" }), "Escape opens the pause menu with its six items");
        Check(_inMenu && !MusicNow().InMenu && _menu.Size.X < 700, "The pause menu is a small panel that pauses the game and keeps its music");
        await CaptureSmokeImage("pause-menu");
        Input.ParseInputEvent(new InputEventKey { Keycode = Key.Escape, Pressed = true }); await SettleUi();
        Check(!_menu.Visible && !_inMenu, "Escape again resumes");
        ShowMenu(); Press("Settings"); await SettleUi();
        Check(_menuContent.FindChildren("*", "Button", true, false).OfType<Button>().Any(b => b.Text == "Difficulty & Sandbox") && _menu.Size.X > 700, "Settings from the pause menu use the full panel and include this career's options");
        Press("Back"); await SettleUi();
        var saves = Saves();
        Press("Quit to title"); await SettleUi();
        Check(TitleOpen && Saves() == saves + 1 && _careers.List()[0].Auto && _titleNotice!.Text == "Progress kept.", "Quit to title keeps a safety save, then shows the title screen");
        Press("Continue"); await SettleUi();
        saves = Saves(); ShowMenu(); Press("Load Career"); await SettleUi();
        _menuContent.GetChildren().OfType<Button>().First(b => b.Text.Contains('·')).EmitSignal(BaseButton.SignalName.Pressed); await SettleUi();
        Check(Saves() == saves + 1 && !_menu.Visible && !TitleOpen, "Loading a career from the pause menu keeps a safety save first");
        // A save that fails keeps the player in the career (an artwork reference with no file makes saving fail).
        _presentation.Artwork["broken"] = new string('a', 64);
        ShowMenu(); Press("Quit to title"); await SettleUi();
        Check(!TitleOpen && _menu.Visible && _notice.Text.StartsWith("Your progress could not be kept"), "A failed safety save keeps the player in the career with the reason");
        _presentation.Artwork.Remove("broken"); _menu.Hide(); _inMenu = false;
    }
```

- [ ] **Step 2: Build to verify it fails**

Run: `dotnet build MangakaGame.sln -warnaserror`
Expected: FAIL with `CS0103: The name '_safetySaves' does not exist`.

- [ ] **Step 3: Write safety saves and Quit to title**

Add to `godot/DebugMain.Transitions.cs`, inside the class:

```csharp
    // Leaving a career keeps an autosave first (spec 2026-09-28). Off in automated checks unless one switches it on.
    private bool _safetySaves = !SmokeRun;
    private bool CareerInPlay => _managementReady && !TitleOpen && _startup is null;

    // True when it is safe to leave: nothing to keep, or the save worked. A failed save names the reason and keeps the player in.
    private bool TrySafetySave()
    {
        if (!CareerInPlay || !_safetySaves) return true;
        if (OfficeEditing) { Notify("Apply or discard your furniture changes first."); return false; }
        try { SaveCareer("Progress kept", true); Notify("Progress kept."); return true; }
        catch (Exception ex) when (ex is System.IO.IOException or System.IO.InvalidDataException or UnauthorizedAccessException)
        {
            LogTimeline("error " + TimelineRedactor.Clean(ex.Message, _state));
            Notify("Your progress could not be kept: " + ex.Message + " You are still in your career.");
            return false;
        }
    }

    private void QuitToTitle()
    {
        if (!TrySafetySave()) return;
        var kept = _safetySaves;
        Transition(() => { OpenTitle(); if (kept) Notify("Progress kept."); });
    }
```

In `godot/DebugMain.Title.cs`, replace the last line of `EnterCareer` (`Transition(Load);`) with:

```csharp
        if (!TitleOpen && !TrySafetySave()) return; // loading another career from the pause menu
        Transition(Load);
```

- [ ] **Step 4: Replace the in-game menu with the pause menu**

In `godot/DebugMain.ManagementMenus.cs`, replace the whole `ShowMenu()` method with:

```csharp
    private void ShowMenu()
    {
        if(TitleOpen){ShowTitleMenu();return;}
        ShowPauseMenu();
    }
    // The in-game menu (Q40): a small panel over the paused game. New careers start from the title screen.
    private void ShowPauseMenu()
    {
        if(OfficeEditing){Notify("Apply or discard furniture changes before opening the menu.");return;}
        if(!_inMenu)CaptureReportScreen();
        OpenMenuPanel();_menuCompact=true;ResizeGui();Empty(_menuContent);
        Words(_menuContent,"Paused",30);
        Words(_menuContent,$"{_state.ControlledBusiness.Name} · {_state.Clock.Now:d MMM yyyy}",15);
        ActionButton(_menuContent,"Resume",()=>{_menu.Hide();_inMenu=false;}).ThemeTypeVariation="PrimaryAction";
        ActionButton(_menuContent,"Save",OpenSaveMenu);
        ActionButton(_menuContent,"Load Career",LoadCareerMenu);
        ActionButton(_menuContent,"Settings",SettingsMenu);
        ActionButton(_menuContent,"Report a problem",ReportProblem);
        ActionButton(_menuContent,"Quit to title",QuitToTitle);
        _pauseNotice=Words(_menuContent,"",14);
        Words(_menuContent,"Private alpha · "+ProblemReport.Build,13);
        _menuContent.GetChildren().OfType<Button>().First().CallDeferred(Control.MethodName.GrabFocus);
    }
```

In `godot/DebugMain.Title.cs`, in `OpenMenuPanel`, replace `_menu.Show();` with:

```csharp
        _menuCompact = false; _menu.Show(); ResizeGui(); // sub-pages use the full panel; the pause menu narrows it again
```

In `godot/DebugMain.Management.cs`, add the fields next to `_managementReady` (line 29):

```csharp
    private bool _menuCompact;
    private Label? _pauseNotice;
```

and in `Notify`, after the `_titleNotice` line, add:

```csharp
        if(_menu.Visible&&_pauseNotice is not null&&IsInstanceValid(_pauseNotice))_pauseNotice.Text=text;
```

In `godot/DebugMain.Gui.cs`, replace line 107:

```csharp
        if(_menu is not null){var inset=Math.Max(24,(width-1040)/2);_menu.OffsetLeft=inset;_menu.OffsetRight=-inset;_menu.OffsetTop=40;_menu.OffsetBottom=-40;}
```

with:

```csharp
        if(_menu is not null)
        {
            // The pause menu is a small centred panel; its sub-pages use the full panel.
            var ui=(float)_presentation.UiScale;var height=GetViewportRect().Size.Y;
            var insetX=Math.Max(24,(width-(_menuCompact?460*ui:1040))/2);var insetY=_menuCompact?Math.Max(40,(height-600*ui)/2):40;
            _menu.OffsetLeft=insetX;_menu.OffsetRight=-insetX;_menu.OffsetTop=insetY;_menu.OffsetBottom=-insetY;
        }
```

In `godot/DebugMain.ManagementPanels.cs`, line 282, replace `()=>{Pause();_inMenu=true;_menu.Show();SettingsMenu();}` with `SettingsMenu`.

- [ ] **Step 5: Update the checks that used the old in-game menu**

In `godot/DebugMain.AlphaSmoke.cs`, line 86: replace `Press("Return to this studio");` with `Press("Resume");`.

In `godot/DebugMain.DisplaySweepSmoke.cs`:

In `Reset()`, change `_menu.Hide();_inMenu=false;` to `CloseTitle();_menu.Hide();_inMenu=false;`.

Replace these entries in `screens`:

```csharp
                ("new-career",()=>{ShowMenu();NewCareerMenu();}),
                ("new-career-rules",()=>{ShowMenu();NewCareerMenu();Press("Career rules");}),
```

with:

```csharp
                ("title",OpenTitle),
                ("title-settings",()=>{OpenTitle();SettingsMenu();}),
                ("new-career",()=>{OpenTitle();NewCareerMenu();}),
                ("new-career-rules",()=>{OpenTitle();NewCareerMenu();Press("Career rules");}),
```

and replace `("menu",ShowMenu),` with `("pause-menu",ShowMenu),`.

- [ ] **Step 6: Build and run the checks**

Run:
```powershell
dotnet build MangakaGame.sln -warnaserror
& $godot --headless --path godot -- --title-smoke
& $godot --headless --path godot -- --alpha-smoke
& $godot --headless --path godot -- --display-sweep-smoke
```
Expected: build succeeds with 0 warnings; `TITLE SMOKE PASSED: 52 checks.`; alpha smoke passes; display sweep reports `520 screen checks, 0 flagged` (26 screens x 20 combinations).

---

### Task 5: The window close button

**Files:**
- Modify: `godot/DebugMain.Timeline.cs:53-57` (`_Notification`)
- Modify: `godot/DebugMain.Transitions.cs` (`HandleCloseRequest`)
- Modify: `godot/DebugMain.Management.cs` (take over quitting when the management screen is built)
- Modify: `godot/DebugMain.TitleSmoke.cs` (add `CheckCloseRequest`)

**Interfaces:**
- Consumes: `TrySafetySave`, `_safetySaves` (Task 4); `OpenTitle` (Task 3).
- Produces: `bool HandleCloseRequest()` (true when the game may close), `bool _closeWarned`.

- [ ] **Step 1: Write the failing smoke checks**

In `godot/DebugMain.TitleSmoke.cs`, add `await CheckCloseRequest();` after `await CheckPauseMenu();`, and add:

```csharp
    private async Task CheckCloseRequest()
    {
        // Still in the career from CheckPauseMenu, with safety saves on.
        Check(!GetTree().AutoAcceptQuit, "The game decides itself when the window may close");
        var saves = _careers.List().Count;
        Check(HandleCloseRequest() && _careers.List().Count == saves + 1, "Closing the window keeps a safety save first");
        _presentation.Artwork["broken"] = new string('a', 64);
        Check(!HandleCloseRequest() && _notice.Text.Contains("Close the window again"), "If that save fails, the first close keeps the game open with a message");
        Check(HandleCloseRequest(), "A second close quits without saving");
        _presentation.Artwork.Remove("broken"); _closeWarned = false;
        OpenTitle(); await SettleUi();
        Check(HandleCloseRequest() && _careers.List().Count == saves + 1, "On the title screen the window closes at once, with nothing to save");
        _safetySaves = false;
    }
```

- [ ] **Step 2: Build to verify it fails**

Run: `dotnet build MangakaGame.sln -warnaserror`
Expected: FAIL with `CS0103: The name 'HandleCloseRequest' does not exist`.

- [ ] **Step 3: Write the close handling**

Add to `godot/DebugMain.Transitions.cs`, inside the class:

```csharp
    private bool _closeWarned;

    // The window's close button (spec 2026-09-28): keep a safety save first. If it fails, stay open once with the
    // reason; a second close quits anyway, so a broken disk can never trap the player in the game.
    private bool HandleCloseRequest()
    {
        if (_closeWarned || TrySafetySave()) return true;
        _closeWarned = true;
        Notify(_notice.Text + " Close the window again to quit without saving.");
        return false;
    }
```

In `godot/DebugMain.Timeline.cs`, replace:

```csharp
        if(what==NotificationWMCloseRequest){DrainUnexpectedErrors();_timeline?.End();}
```

with:

```csharp
        if(what==NotificationWMCloseRequest)
        {
            if(_managementReady&&!HandleCloseRequest())return;
            DrainUnexpectedErrors();_timeline?.End();
            if(_managementReady)GetTree().Quit();
        }
```

In `godot/DebugMain.Management.cs`, directly after `_managementReady=true;ResizeGui();` (line 164), add:

```csharp
        GetTree().AutoAcceptQuit=false; // the close button keeps a safety save first (HandleCloseRequest)
```

- [ ] **Step 4: Build and run the check**

Run:
```powershell
dotnet build MangakaGame.sln -warnaserror
& $godot --headless --path godot -- --title-smoke
```
Expected: build succeeds with 0 warnings; `TITLE SMOKE PASSED: 57 checks.`

---

### Task 6: Full verification and rendered review

**Files:**
- Modify only if a check fails: the file the failure points to, with a `Ruling:` line in the ledger for any change to a check's expectation.

**Interfaces:**
- Consumes: everything above.
- Produces: the verification numbers for the completion record.

- [ ] **Step 1: Run the full automated suite**

Run:
```powershell
dotnet test tests/MangakaSim.Tests
dotnet build MangakaGame.sln -warnaserror
& $godot --headless --editor --path godot --import --quit
foreach($s in 'smoke-test','management-smoke','progression-smoke','alpha-smoke','usability-smoke','production-smoke','office-life-smoke','convenience-smoke','series-status-smoke','atmosphere-smoke','family-home-smoke','quiet-speed-smoke','display-sweep-smoke','journey-smoke','music-smoke','startup-smoke','title-smoke'){ & $godot --headless --path godot -- "--$s" 2>&1 | Select-String 'PASS|FAIL|flagged' }
```
Expected: all xUnit tests pass (657 + 7 = 664); build 0 warnings; every smoke prints its PASSED line; the display sweep prints `0 flagged`.

- [ ] **Step 2: Capture and review the rendered screens**

Run:
```powershell
& $godot --path godot -- --title-smoke --capture
& $godot --path godot -- --display-sweep-smoke --capture
```
Expected: AVIF captures in `TestResults` named `title-1920x1080-100`, `title-2560x1080-100`, `title-3440x1440-100`, `title-1680x1050-100`, `title-1280x720-150`, `title-settings`, `title-continue`, `pause-menu`, and the display sweep review set. Look at each: art fills the window with no bars, the logo and menu are readable over the gradient and clear of the lit desk, nothing is cut off at 1280 x 720 with 150% text, and the pause menu is a small centred panel. Record what was seen, by eye, in the completion record.

---

### Task 7: Records

**Files:**
- Modify: `godot/Office/ASSETS.md` (title screen art provenance)
- Modify: `docs/superpowers/private-alpha-credits.txt` (placeholder art credit)
- Modify: `README.md` (Validate list gains `--title-smoke`)
- Create: `docs/superpowers/title-screen-and-pause-menu-completion.md`
- Modify: `CLAUDE.md` section 5 (one bullet), `docs/superpowers/specs/2026-09-22-roadmap.md` (matching line if the start-up work is listed there)

**Interfaces:**
- Consumes: the verification numbers from Task 6.

- [ ] **Step 1: Provenance and credits**

Append to `godot/Office/ASSETS.md`:

```markdown

## Title screen branding (placeholders)

`../Assets/Branding/title-background.jpg` (2752 x 1536, a dusk mangaka studio) and
`../Assets/Branding/logo.png` (1840 x 1152, "MANGAKA DAYS" with a chibi Helper-Chan)
were generated by the project owner with Google Gemini on 2026-09-28. Real manga
titles and logos on the bookshelf were removed in a Gemini edit and checked at full
size; the logo was cut out of its grey background locally. They are placeholders
until the commissioned art arrives (`docs/superpowers/logo-designer-brief.md`), which
replaces these two files with no code change.
```

Add to `docs/superpowers/private-alpha-credits.txt`, after the Music paragraph:

```text
Title screen art and logo (placeholders): AI-generated with Google Gemini from
prompts by the project owner, then edited for this game.
```

- [ ] **Step 2: README and completion record**

In `README.md`, add `--title-smoke` to the Validate list next to `--startup-smoke`, with the description "title screen, fades, pause menu and safety saves".

Write `docs/superpowers/title-screen-and-pause-menu-completion.md` in the style of `startup-flow-and-volume-completion.md`: what changed (title screen, fades, pause menu, safety saves, close button, title Settings, records), verification (xUnit count, each smoke's check count, display sweep numbers, what the captures showed), not verified (feel of the fades and push-in on a real screen; the user, then testers), rulings, and next step (the user tries it; then commit, and include it in alpha.13 when approved).

- [ ] **Step 3: CLAUDE.md and roadmap**

Add to `CLAUDE.md` section 5, after the start-up bullet:

```markdown
- **Title screen and pause menu (2026-09-28):** full-screen title screen with
  the placeholder studio art and logo (Q42, Gemini), fades into and out of
  careers, a small pause menu with Quit to title (Q40), and a safety save
  before leaving a career or closing the window. Name decided: Mangaka Days
  (Q41). Spec `docs/superpowers/specs/2026-09-28-title-screen-and-pause-menu-design.md`;
  record `docs/superpowers/title-screen-and-pause-menu-completion.md`.
```

If `docs/superpowers/specs/2026-09-22-roadmap.md` lists the start-up work, add the matching line after it. Convert every edited doc to CRLF (`unix2dos`) and confirm no LF-only lines remain.
