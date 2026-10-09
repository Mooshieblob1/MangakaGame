using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using MangakaSim;

namespace MangakaGame;

// Visual novel conversations with Helper-Chan (voice preview, 2026-10-10): her portrait stands over a full-width
// text box, the words type out alongside her recorded voice, and the answers appear once she has finished.
// Presentation only: the scenes, answers and saves are unchanged. Illustrated scenes keep the framed popup.
public partial class DebugMain
{
    // The exact English text each recording belongs to (the Japanese recordings say the same thing, with this text as
    // the subtitle). A scene whose text has changed (follow-ups added by earlier answers) still opens as a visual novel
    // scene, just without the voice. Voice language is a per-computer setting: English, Japanese or off (2026-10-10).
    private static readonly Dictionary<string, string> HelperVoiceLines = new()
    {
        ["beside"] = "The desk is ready. So am I. What should I remind you of when things get difficult?",
        ["page"] = "I kept a copy of the first finished page. Official archival duties, obviously.",
        ["same"] = "The clipboard has room for a difficult chapter, too. What would help right now?",
        ["tea"] = "A pause is allowed. I checked. I'll stay beside you. We don't have to fill the silence.",
        ["migration"] = "The important equipment survived. Clipboard, glasses, and an unreasonable amount of hair.",
        ["goal-legend"] = "Anime, merchandise, a million readers. People will remember these pages. I kept the first one, you know.",
    };
    private const double DialogueTypingRate = 42; // characters per second when a line has no recording
    private AudioStreamPlayer? _voice;
    private Label? _dialogueLine;
    private Control? _dialogueChoices;
    private double _dialogueRate, _dialogueShown;
    private float _musicDuck = 1;

    private bool DialogueTyping => _dialogueLine is not null && _helperPopup.Visible && _dialogueShown < _dialogueLine.Text.Length;
    private bool HelperSpeaking => _voice is { Playing: true };

    private AudioStream? HelperVoice(StoryScene scene)
    {
        if (_audioSettings.HelperVoice is not ("en" or "ja")) return null;
        var path = $"res://Assets/Voice/Helper/{_audioSettings.HelperVoice}/{scene.Id}.ogg";
        return HelperVoiceLines.TryGetValue(scene.Id, out var recorded) && recorded == scene.Text.Trim() && ResourceLoader.Exists(path)
            ? ResourceLoader.Load<AudioStream>(path) : null;
    }

    private void HelperVoiceChoice(Control parent)
    {
        Words(parent, "Helper-Chan's voice");
        var choice = new OptionButton { Name = "HelperVoice" }; parent.AddChild(choice);
        foreach (var (label, id) in new[] { ("English", "en"), ("Japanese", "ja"), ("Off, text only", "off") })
        { choice.AddItem(label); if (id == _audioSettings.HelperVoice) choice.Select(choice.ItemCount - 1); }
        choice.ItemSelected += i => { _audioSettings.HelperVoice = AudioSettings.HelperVoices[i]; SaveAudioSettings(); };
    }

    private void EnsureVoicePlayer()
    {
        if (_voice is not null) return;
        _voice = new AudioStreamPlayer { Bus = "Voice" }; AddChild(_voice);
        _helperPopup.VisibilityChanged += () => { if (!_helperPopup.Visible) _voice.Stop(); };
    }

    /// <summary>The framed popups (notices, illustrated scenes) undo the full-screen dialogue layout.</summary>
    private void ResetHelperPopup()
    {
        _helperPopup.RemoveThemeStyleboxOverride("panel");
        _dialogueLine = null; _dialogueChoices = null; _voice?.Stop();
    }

    /// <summary>Lays out the scene and starts her line. Returns the box the answer buttons go in; it appears when she finishes.</summary>
    private VBoxContainer DialogueBody(StoryScene scene)
    {
        EnsureVoicePlayer(); Empty(_helperPopup);
        var view = GetViewportRect().Size;
        _helperPopup.AddThemeStyleboxOverride("panel", new StyleBoxEmpty());
        _helperPopup.Position = Vector2.Zero; _helperPopup.Size = view;
        var stage = new Control { MouseFilter = MouseFilterEnum.Ignore }; _helperPopup.AddChild(stage);
        // A deeper dim than the usual popup shade, so the scene reads over the busy office and panels.
        var dim = new ColorRect { Color = new Color(0, 0, 0, .38f), Size = view, MouseFilter = MouseFilterEnum.Ignore }; stage.AddChild(dim);

        // Her full-length portrait, framed from the head to the hips; the text box covers the rest.
        var texture = GD.Load<Texture2D>($"res://Assets/Helper/{scene.Expression}.png");
        var height = view.Y * 1.5f; var width = height * texture.GetWidth() / texture.GetHeight();
        var portrait = new TextureRect { Texture = texture, ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize, StretchMode = TextureRect.StretchModeEnum.Scale,
            Size = new(width, height), Position = new(view.X * .24f - width / 2, view.Y * .03f), MouseFilter = MouseFilterEnum.Ignore };
        stage.AddChild(portrait);

        var boxRect = new Rect2(view.X * .04f, view.Y * .755f, view.X * .92f, view.Y * .205f);
        var box = new PanelContainer { Position = boxRect.Position, Size = boxRect.Size, MouseFilter = MouseFilterEnum.Stop };
        var face = new Color(Brand.Card) { A = .95f };
        box.AddThemeStyleboxOverride("panel", new StyleBoxFlat { BgColor = face, BorderColor = new Color(BrandPalette.Gold), BorderWidthLeft = 3, BorderWidthTop = 3,
            BorderWidthRight = 3, BorderWidthBottom = 3, CornerRadiusTopLeft = 18, CornerRadiusTopRight = 18, CornerRadiusBottomLeft = 18, CornerRadiusBottomRight = 18,
            ContentMarginLeft = 44, ContentMarginRight = 44, ContentMarginTop = 40, ContentMarginBottom = 24, ShadowColor = new Color(0, 0, 0, .35f), ShadowSize = 12 });
        stage.AddChild(box);
        var copy = new VBoxContainer(); box.AddChild(copy);
        var title = Words(copy, scene.Title, 15); title.Modulate = new Color(1, 1, 1, .7f);
        _dialogueLine = Words(copy, Tr(scene.Text.Trim()), 30); _dialogueLine.AutoTranslateMode = AutoTranslateModeEnum.Disabled;
        _dialogueLine.VisibleCharactersBehavior = TextServer.VisibleCharactersBehavior.CharsAfterShaping;
        _dialogueLine.VisibleCharacters = 0; _dialogueShown = 0;
        box.GuiInput += e => { if (e is InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left } && DialogueTyping) FinishDialogueLine(); };

        // Name plate on the box's top edge, in the logo's gold slab.
        var plate = new PanelContainer { Position = boxRect.Position + new Vector2(36, -26), MouseFilter = MouseFilterEnum.Ignore };
        plate.AddThemeStyleboxOverride("panel", SlabStyle(BrandPalette.Gold, BrandPalette.GoldBase, 4, 22, 6));
        var name = Words(plate, "Helper-Chan", 26); name.AutowrapMode = TextServer.AutowrapMode.Off; name.AddThemeColorOverride("font_color", new Color(BrandPalette.Ink));
        stage.AddChild(plate);

        // Answers stack on the right, above the box, once the line is complete.
        var choices = new VBoxContainer { Position = new(view.X * .60f, 0), CustomMinimumSize = new(view.X * .34f, 0), Visible = false };
        choices.AddThemeConstantOverride("separation", 10);
        choices.Resized += () => choices.Position = new(view.X * .60f, boxRect.Position.Y - choices.Size.Y - 34);
        stage.AddChild(choices); _dialogueChoices = choices;

        var voice = HelperVoice(scene);
        _dialogueRate = voice is null ? DialogueTypingRate : _dialogueLine.Text.Length / Math.Max(.5, voice.GetLength() * .9);
        if (voice is not null) { _voice!.Stream = voice; _voice.Play(); }
        if (_presentation.ReducedUiMotion) FinishDialogueLine();
        else
        {
            portrait.Modulate = new Color(1, 1, 1, 0); box.Modulate = new Color(1, 1, 1, 0); plate.Modulate = box.Modulate;
            var enter = CreateTween().SetParallel();
            enter.TweenProperty(portrait, "modulate:a", 1f, .35);
            enter.TweenProperty(portrait, "position:x", portrait.Position.X, .45).From(portrait.Position.X - view.X * .05f).SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.Out);
            enter.TweenProperty(box, "modulate:a", 1f, .25); enter.TweenProperty(plate, "modulate:a", 1f, .25);
        }
        _helperPopup.Show();
        return choices;
    }

    private void FinishDialogueLine()
    {
        if (_dialogueLine is null) return;
        _dialogueShown = _dialogueLine.Text.Length; _dialogueLine.VisibleCharacters = -1;
        if (_dialogueChoices is { Visible: false } choices)
        {
            choices.Show();
            if (Input.GetConnectedJoypads().Count > 0) choices.FindChildren("*", "Button", true, false).OfType<Button>().FirstOrDefault()?.GrabFocus();
        }
    }

    /// <summary>Types the line out at the pace of her voice; a click or the accept key shows the rest at once.</summary>
    private void UpdateDialogue(double delta)
    {
        if (!DialogueTyping) return;
        if (Input.IsActionJustPressed("ui_accept")) { FinishDialogueLine(); return; }
        _dialogueShown += delta * _dialogueRate;
        if (_dialogueShown >= _dialogueLine!.Text.Length) FinishDialogueLine();
        else _dialogueLine.VisibleCharacters = (int)_dialogueShown;
    }

    /// <summary>Music steps back while she speaks.</summary>
    private float MusicDuck(double delta) => _musicDuck = Mathf.MoveToward(_musicDuck, HelperSpeaking ? .35f : 1f, (float)delta * 1.5f);
}
