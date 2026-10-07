using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using MangakaSim;

namespace MangakaGame;

/// <summary>
/// The ring around the control a controller or keyboard player is on (controller support, 2026-10-06). It draws on
/// its own layer above every panel, glides from one control to the next, and is clipped to the scrolling area the
/// control sits in, so it never frames a button hidden below the fold.
/// </summary>
public partial class FocusRing : Control
{
    private Rect2 _shown, _target;
    private bool _visible;
    public bool Showing => _visible;
    private float _pulse;
    public bool ReducedMotion { get; set; }

    public FocusRing() { MouseFilter = MouseFilterEnum.Ignore; FocusMode = FocusModeEnum.None; }

    /// <summary>Moves the ring to a rectangle in canvas units, or hides it with null.</summary>
    public void Track(Rect2? target, double delta)
    {
        if (target is not { } rect) { if (_visible) { _visible = false; QueueRedraw(); } return; }
        _target = rect.Grow(4);
        if (!_visible || ReducedMotion) _shown = _target;
        else
        {
            var t = (float)(1 - Math.Exp(-delta * 22));
            _shown = new Rect2(_shown.Position.Lerp(_target.Position, t), _shown.Size.Lerp(_target.Size, t));
            if (_shown.Position.DistanceTo(_target.Position) < .5f && _shown.Size.DistanceTo(_target.Size) < .5f) _shown = _target;
        }
        _visible = true;
        _pulse = ReducedMotion ? 0 : (_pulse + (float)delta) % 1.6f;
        QueueRedraw();
    }

    public override void _Draw()
    {
        if (!_visible) return;
        // A dark edge outside a gold one reads on both the evening and the manuscript paper themes.
        var glow = ReducedMotion ? .5f : .35f + .25f * Mathf.Sin(_pulse / 1.6f * Mathf.Tau);
        DrawStyleBox(Box(new Color(BrandPalette.Gold) with { A = glow * .5f }, 9, 8), _shown.Grow(3));
        DrawStyleBox(Box(new Color(BrandPalette.Ink), 4, 9), _shown.Grow(1));
        DrawStyleBox(Box(new Color(BrandPalette.Gold), 3, 8), _shown);
    }

    private static StyleBoxFlat Box(Color colour, int width, int radius)
    {
        var box = new StyleBoxFlat { DrawCenter = false, BorderColor = colour, AntiAliasing = true };
        box.SetBorderWidthAll(width); box.SetCornerRadiusAll(radius);
        return box;
    }
}

/// <summary>One controller button drawn the way the prompts show it: Xbox colours for A, B, X and Y.</summary>
public partial class PadGlyphIcon : Control
{
    private readonly PadGlyph _glyph;
    private readonly float _size;

    public PadGlyphIcon() : this(PadGlyph.A, 22) { }

    public PadGlyphIcon(PadGlyph glyph, float size)
    {
        _glyph = glyph; _size = size; MouseFilter = MouseFilterEnum.Ignore;
        var wide = glyph is PadGlyph.LB or PadGlyph.RB or PadGlyph.LT or PadGlyph.RT or PadGlyph.Menu or PadGlyph.View;
        CustomMinimumSize = new Vector2(wide ? size * 1.45f : size, size);
        SizeFlagsVertical = SizeFlags.ShrinkCenter;
    }

    public override void _Draw()
    {
        var size = CustomMinimumSize; var centre = size / 2; var r = _size / 2;
        var ink = new Color("1b1416"); var light = new Color("f6ead2");
        var font = ThemeDB.FallbackFont; var fontSize = (int)(_size * .58f);
        void Text(string text, Color colour)
        {
            var measured = font.GetStringSize(text, HorizontalAlignment.Left, -1, fontSize);
            DrawString(font, new Vector2(centre.X - measured.X / 2, centre.Y + font.GetAscent(fontSize) / 2 - 1), text, HorizontalAlignment.Left, -1, fontSize, colour);
        }
        void Pill(Color fill)
        {
            var box = new StyleBoxFlat { BgColor = fill, AntiAliasing = true, BorderColor = light, }; box.SetBorderWidthAll(1); box.SetCornerRadiusAll((int)(r * .7f));
            DrawStyleBox(box, new Rect2(Vector2.Zero, size));
        }
        switch (_glyph)
        {
            case PadGlyph.A or PadGlyph.B or PadGlyph.X or PadGlyph.Y:
                var face = _glyph switch { PadGlyph.A => new Color("5bb543"), PadGlyph.B => new Color("e0463c"), PadGlyph.X => new Color("3f7fe0"), _ => new Color("f2c230") };
                DrawCircle(centre, r, face, true, -1, true);
                DrawCircle(centre, r - .5f, light with { A = .7f }, false, 1, true);
                Text(_glyph.ToString(), _glyph == PadGlyph.Y ? ink : Colors.White);
                break;
            case PadGlyph.LB or PadGlyph.RB or PadGlyph.LT or PadGlyph.RT:
                Pill(new Color("3a3034")); Text(_glyph.ToString(), light); break;
            case PadGlyph.Menu:
                Pill(new Color("3a3034"));
                for (var i = -1; i <= 1; i++) DrawLine(centre + new Vector2(-r * .45f, i * r * .32f), centre + new Vector2(r * .45f, i * r * .32f), light, 2, true);
                break;
            case PadGlyph.View:
                Pill(new Color("3a3034"));
                DrawRect(new Rect2(centre + new Vector2(-r * .5f, -r * .4f), new Vector2(r * .62f, r * .52f)), light, false, 1.5f);
                DrawRect(new Rect2(centre + new Vector2(-r * .12f, -r * .12f), new Vector2(r * .62f, r * .52f)), light, false, 1.5f);
                break;
            case PadGlyph.LeftStick or PadGlyph.RightStick:
                DrawCircle(centre, r, new Color("3a3034"), true, -1, true);
                DrawCircle(centre, r * .62f, light, false, 1.5f, true);
                Text(_glyph == PadGlyph.LeftStick ? "L" : "R", light); break;
            case PadGlyph.DPad:
                var arm = r * .36f;
                DrawRect(new Rect2(centre - new Vector2(arm, r * .95f), new Vector2(arm * 2, r * 1.9f)), new Color("3a3034"));
                DrawRect(new Rect2(centre - new Vector2(r * .95f, arm), new Vector2(r * 1.9f, arm * 2)), new Color("3a3034"));
                DrawRect(new Rect2(centre - new Vector2(arm, r * .95f), new Vector2(arm * 2, r * 1.9f)), light, false, 1);
                DrawRect(new Rect2(centre - new Vector2(r * .95f, arm), new Vector2(r * 1.9f, arm * 2)), light, false, 1);
                break;
        }
    }
}

/// <summary>The prompt bar: buttons and what they do, plus the focused control's tooltip, which a controller cannot
/// hover to read. Rebuilt only when its content changes.</summary>
public partial class ControllerHints : VBoxContainer
{
    private readonly HFlowContainer _row = new();
    private readonly Label _tip = new() { AutowrapMode = TextServer.AutowrapMode.WordSmart, Visible = false };
    private string _key = "";
    public float UiScale { get; set; } = 1;
    public Color TextColour { get; set; } = new("f6ead2");

    public ControllerHints()
    {
        MouseFilter = MouseFilterEnum.Ignore; Name = "ControllerHints";
        AddThemeConstantOverride("separation", 2);
        _row.AddThemeConstantOverride("h_separation", 14); _row.AddThemeConstantOverride("v_separation", 2); _row.MouseFilter = MouseFilterEnum.Ignore;
        AddChild(_row); AddChild(_tip);
        _tip.MouseFilter = MouseFilterEnum.Ignore;
    }

    /// <summary>The width the prompts take on one line, so a floating bar can be no wider than it needs.</summary>
    public float NaturalWidth()
    {
        var groups = _row.GetChildren().OfType<Control>().ToArray();
        var width = groups.Sum(g => g.GetCombinedMinimumSize().X) + Math.Max(0, groups.Length - 1) * 14f;
        if (_tip.Visible) width = Math.Max(width, Math.Min(640 * UiScale, _tip.GetThemeFont("font").GetStringSize(_tip.Text, HorizontalAlignment.Left, -1, (int)(13 * UiScale)).X));
        return width;
    }

    public void SetPrompts(IReadOnlyList<(PadGlyph Glyph, string Label)> prompts, string tip)
    {
        var key = $"{UiScale}|{TextColour.ToHtml()}|{tip}|{string.Join(",", prompts)}";
        if (key == _key) return;
        _key = key;
        foreach (var child in _row.GetChildren()) { _row.RemoveChild(child); child.QueueFree(); }
        HBoxContainer? group = null;
        foreach (var (glyph, label) in prompts)
        {
            group ??= new HBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
            group.AddThemeConstantOverride("separation", (int)(4 * UiScale));
            group.AddChild(new PadGlyphIcon(glyph, 22 * UiScale));
            if (label.Length == 0) continue; // joined to the next button
            var text = new Label { Text = label, MouseFilter = MouseFilterEnum.Ignore, SizeFlagsVertical = SizeFlags.ShrinkCenter };
            text.AddThemeFontSizeOverride("font_size", (int)(14 * UiScale)); text.AddThemeColorOverride("font_color", TextColour);
            group.AddChild(text); _row.AddChild(group); group = null;
        }
        if (group is not null) _row.AddChild(group);
        _tip.Text = tip; _tip.Visible = tip.Length > 0;
        _tip.AddThemeFontSizeOverride("font_size", (int)(13 * UiScale)); _tip.AddThemeColorOverride("font_color", TextColour with { A = .85f });
    }
}
