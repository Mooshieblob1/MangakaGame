using System.Linq;
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
    private bool _disclaimerDone, _startupShowVolume, _startupEnding;

    private void BeginStartup(bool showVolume)
    {
        _startupShowVolume = showVolume; _disclaimerDone = false; _volumeSetup = null; _startupEnding = false;
        _startupSilent = true; ApplyAudioSettings();
        _startup = new CanvasLayer { Layer = 100 }; AddChild(_startup);
        // A CanvasLayer does not inherit the game's theme, so pass it on explicitly (final review).
        _startupRoot = new Control { MouseFilter = MouseFilterEnum.Stop, FocusMode = FocusModeEnum.All, Theme = Theme };
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
            // Only the disclaimer is skipped this way; afterwards keys must reach the sliders and Continue (final review).
            if (_disclaimerDone) return;
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
        // Quick start (Q65): Master and the interface size only; music, sound effects and the window wait in Settings.
        Words(box, "Set your volume and size", 28);
        AudioSliders(box, masterOnly: true);
        DisplayControls(box, sizeOnly: true);
        var continueButton = ActionButton(box, "Continue", () =>
        {
            if (_startupEnding) return;
            _startupEnding = true; _audioSettings.SetupDone = true; SaveAudioSettings(); FadeOutStartup();
        });
        continueButton.ThemeTypeVariation = "PrimaryAction";
        Words(box, "Music, sound effects and fullscreen are in Settings.", 14);
        // Keyboard and controller players start on Master; Tab or the arrow keys move on to Continue.
        box.FindChildren("*", "HSlider", true, false).OfType<HSlider>().FirstOrDefault()?.GrabFocus();
    }

    // The volume screen fades to black; the curtain beneath then fades up on the title screen (spec 2026-09-28).
    private void FadeOutStartup()
    {
        var seconds = FadeSeconds(false);
        if (seconds <= 0 || _startupRoot is null) { EndStartup(); return; }
        var tween = CreateTween();
        tween.TweenProperty(_startupRoot, "modulate:a", 0f, seconds);
        tween.TweenCallback(Callable.From(EndStartup));
    }

    private void EndStartup()
    {
        _startupSilent = false; ApplyAudioSettings();
        _startup?.QueueFree(); _startup = null; _startupRoot = null;
        FadeIn(); FocusTitle();
    }
}
