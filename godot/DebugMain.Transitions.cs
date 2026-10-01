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
        GetViewport().GuiReleaseFocus(); // nothing behind the curtain keeps the keyboard
        _curtainTween?.Kill(); _curtain.Show();
        if (seconds <= 0) { _curtain.Modulate = Colors.White; Behind(); return true; }
        _curtain.Modulate = new Color(1, 1, 1, 0);
        _curtainTween = CreateTween();
        _curtainTween.TweenProperty(_curtain, "modulate:a", 1f, seconds);
        _curtainTween.TweenCallback(Callable.From(Behind));
        return true;
    }

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

    private bool _closeWarned;

    // The window's close button (spec 2026-09-28): keep a safety save first. If it fails, stay open once with the
    // reason; a second close quits anyway, so a broken disk can never trap the player in the game.
    // Every close tries the save first, so a problem fixed since an earlier refused close is saved normally (final review).
    private bool HandleCloseRequest()
    {
        if (TrySafetySave()) { _closeWarned = false; return true; }
        if (_closeWarned) return true;
        _closeWarned = true;
        Notify(_notice.Text + " Close the window again to quit without saving.");
        return false;
    }

    private void QuitToTitle()
    {
        if (Fading || !TrySafetySave()) return; // a repeat press during the fade must not save again
        var kept = _safetySaves;
        Transition(() => { OpenTitle(); if (kept) Notify("Progress kept."); });
    }
}
