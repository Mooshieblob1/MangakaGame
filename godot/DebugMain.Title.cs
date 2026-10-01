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
    private Label? _titleNotice, _titlePageNotice;
    private Tween? _titlePushIn;
    private float _titleLogoAspect = 1840f / 1152f;
    // Commissioned or regenerated art replaces the files in this folder with no code change.
    private string _brandingFolder = "res://Assets/Branding";
    private VBoxContainer _pauseMenuContent = null!;
    private bool TitleOpen => _title is not null;
    // Shown only over the title art itself, since the plain fallback backdrop is not AI-generated.
    public const string TitleArtNote = "This image was AI-generated";

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
        _titleColumn = new ScrollContainer { HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled, FollowFocus = true }; _title.AddChild(_titleColumn);
        var box = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill }; _titleColumn.AddChild(box);
        if (LoadBranding("logo", "png") is { } logo)
        {
            _titleLogoAspect = logo.GetWidth() / (float)logo.GetHeight();
            _titleLogo = new TextureRect { Texture = logo, ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize, StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered, SizeFlagsHorizontal = SizeFlags.ShrinkBegin };
            box.AddChild(_titleLogo);
        }
        else TitleWords(box, "MANGAKA DAYS", 44);
        TitleWords(box, "A career told one page at a time.", 18);
        _titleMenuBox = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill }; box.AddChild(_titleMenuBox);
        _titleMenuBox.AddThemeConstantOverride("separation", 8); // slabs sit close, so small screens keep the logo in view
        _titleNotice = TitleWords(box, "", 15); _titleNotice.Hide(); // takes no room until there is something to say
        var footer = TitleWords(_title, "Private alpha · " + ProblemReport.Build, 13);
        footer.AutowrapMode = TextServer.AutowrapMode.Off; footer.SetAnchorsAndOffsetsPreset(LayoutPreset.BottomLeft, LayoutPresetMode.Minsize, 24);
        if (_titleArt is not null)
        {
            var note = TitleWords(_title, TitleArtNote, 12);
            note.AutowrapMode = TextServer.AutowrapMode.Off; note.Modulate = new Color(1, 1, 1, .55f);
            note.GrowHorizontal = GrowDirection.Begin; note.GrowVertical = GrowDirection.Begin;
            note.SetAnchorsAndOffsetsPreset(LayoutPreset.BottomRight, LayoutPresetMode.Minsize, 16);
        }
        _titlePage = new PanelContainer { Visible = false }; _titlePage.SetAnchorsPreset(LayoutPreset.FullRect); _title.AddChild(_titlePage);
        // The notice sits outside the rebuilt page, so a failed load or import on a sub-page is seen (final review).
        var pageBox = new VBoxContainer(); _titlePage.AddChild(pageBox);
        _titlePageNotice = Words(pageBox, "", 15); _titlePageNotice.Hide();
        var pageScroll = new ScrollContainer { HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled, SizeFlagsVertical = SizeFlags.ExpandFill }; pageBox.AddChild(pageScroll);
        _titlePageContent = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill }; pageScroll.AddChild(_titlePageContent);
        // New Career, Load, Settings and Report keep writing into _menuContent; on the title screen that is the page panel.
        _menuContent = _titlePageContent;
        ShowTitleMenu(); LayoutTitle();
    }

    // The title screen always sits over dark art, so its words stay light in both themes (rendered review).
    private static Label TitleWords(Control parent, string text, int size)
    {
        var label = Words(parent, text, size);
        label.AddThemeColorOverride("font_color", new Color("e3ecee"));
        label.AddThemeColorOverride("font_shadow_color", new Color(0, 0, 0, .6f));
        label.AddThemeConstantOverride("shadow_offset_x", 1); label.AddThemeConstantOverride("shadow_offset_y", 1);
        return label;
    }

    private void CloseTitle()
    {
        if (_title is null) return;
        RemoveChild(_title); _title.QueueFree();
        _title = null; _titleArt = _titleLogo = null; _titleColumn = null; _titlePage = null; _titleShade = null;
        _titleMenuBox = _titlePageContent = null; _titleNotice = _titlePageNotice = null; _titlePushIn = null; _titleMainButton = null;
        _menuContent = _pauseMenuContent;
    }

    // Zooms the picture inside its fixed frame. Scaling the control itself stepped visibly on large screens, because
    // controls snap to whole pixels; sampling the texture closer to its centre glides smoothly (user report).
    private static Shader? _pushInShader;
    private void StartPushIn()
    {
        _titlePushIn = null;
        if (_titleArt is null) return;
        _pushInShader ??= new Shader { Code = "shader_type canvas_item;\nuniform float zoom = 1.0;\nvoid vertex() { UV = (UV - vec2(0.5)) / zoom + vec2(0.5); }\n" };
        var material = new ShaderMaterial { Shader = _pushInShader }; material.SetShaderParameter("zoom", 1f);
        _titleArt.Material = material;
        if (_presentation.ReducedUiMotion) return;
        _titlePushIn = _titleArt.CreateTween().SetLoops();
        _titlePushIn.TweenProperty(material, "shader_parameter/zoom", (float)TitleScreen.PushInScale, TitleScreen.PushInSeconds).SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.InOut);
        _titlePushIn.TweenProperty(material, "shader_parameter/zoom", 1f, TitleScreen.PushInSeconds).SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.InOut);
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
        if (latest is not null) { main = StickerButton(_titleMenuBox!, "Continue", ContinueLatest, true); TitleWords(_titleMenuBox!, TitleScreen.ContinueLabel(latest), 14); }
        main ??= StickerButton(_titleMenuBox!, "New Career", NewCareerMenu, true);
        if (main.Text != "New Career") StickerButton(_titleMenuBox!, "New Career", NewCareerMenu, false);
        StickerButton(_titleMenuBox!, "Load Career", LoadCareerMenu, false);
        StickerButton(_titleMenuBox!, "Settings", SettingsMenu, false);
        StickerButton(_titleMenuBox!, "Report a problem", ReportProblem, false);
        StickerButton(_titleMenuBox!, "Quit", () => { _timeline?.End(); GetTree().Quit(); }, false);
        main.ThemeTypeVariation = "PrimaryAction"; _titleMainButton = main;
        FocusLater(main);
        Callable.From(LayoutTitle).CallDeferred(); // the logo fits around the finished menu
    }

    private Button? _titleMainButton;

    // Title buttons drawn like pieces of the logo (user choice A): a gold (main) or mint face on a darker extruded
    // base, a brown outline and a white sticker edge. Colours are sampled from the logo. Pressing sinks the face.
    private static readonly Color SlabInk = new("4b2c2a");
    private static StyleBoxFlat Slab(Color colour, int radius, float margin)
    {
        var box = new StyleBoxFlat { BgColor = colour }; box.SetCornerRadiusAll(radius); box.SetContentMarginAll(margin); return box;
    }
    private Button StickerButton(Control parent, string text, Action action, bool main)
    {
        Color face = new(main ? "f0c878" : "a8f8e0"), lit = new(main ? "fbe0a6" : "d2fff2"), depth = new(main ? "b88048" : "50c0b0");
        var edge = new PanelContainer { SizeFlagsHorizontal = SizeFlags.ShrinkBegin }; edge.AddThemeStyleboxOverride("panel", Slab(Colors.White, 16, 3)); parent.AddChild(edge);
        var outline = new PanelContainer(); outline.AddThemeStyleboxOverride("panel", Slab(SlabInk, 13, 3)); edge.AddChild(outline);
        var baseBox = Slab(depth, 11, 0); baseBox.ContentMarginBottom = 5;
        var slabBase = new PanelContainer(); slabBase.AddThemeStyleboxOverride("panel", baseBox); outline.AddChild(slabBase);
        var button = ActionButton(slabBase, text, action);
        var ui = (float)_presentation.UiScale;
        StyleBoxFlat Face(Color colour) { var box = Slab(colour, 11, 0); box.ContentMarginTop = box.ContentMarginBottom = 4 * ui; box.ContentMarginLeft = box.ContentMarginRight = 16 * ui; return box; }
        button.AddThemeStyleboxOverride("normal", Face(face)); button.AddThemeStyleboxOverride("pressed", Face(face));
        button.AddThemeStyleboxOverride("hover", Face(lit)); button.AddThemeStyleboxOverride("hover_pressed", Face(lit));
        button.AddThemeStyleboxOverride("focus", Face(lit));
        foreach (var name in new[] { "font_color", "font_hover_color", "font_focus_color", "font_pressed_color", "font_hover_pressed_color" }) button.AddThemeColorOverride(name, SlabInk);
        button.AddThemeFontSizeOverride("font_size", (int)((main ? 22 : 18) * ui));
        button.ButtonDown += () => { baseBox.ContentMarginTop = 3; baseBox.ContentMarginBottom = 2; };
        button.ButtonUp += () => { baseBox.ContentMarginTop = 0; baseBox.ContentMarginBottom = 5; };
        return button;
    }

    // Deferred focus, decided when it runs: skipped while the start-up screens hold the keyboard (EndStartup
    // focuses the title then), and if the control has gone (a title can close in the same frame). Final review.
    private void FocusLater(Control control) =>
        Callable.From(() => { if (_startup is null && IsInstanceValid(control) && control.IsInsideTree() && control.IsVisibleInTree()) control.GrabFocus(); }).CallDeferred();

    private void FocusTitle() { if (_titleMainButton is not null && TitleOpen) FocusLater(_titleMainButton); }

    // Shows the panel that the sub-page builders fill: the title's page panel, or the pause menu panel in a career.
    private void OpenMenuPanel()
    {
        Pause(); _inMenu = true;
        // A new page starts without the previous page's message.
        if (_title is not null) { _titlePageNotice!.Hide(); _titleColumn!.Hide(); _titleShade!.Show(); _titlePage!.Show(); return; }
        _menuNotice?.Hide(); _menuCompact = false; _menu.Show(); ResizeGui(); // sub-pages use the full panel; the pause menu narrows it again
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
        // Compact slabs rather than full-width bars (user request); all the same width so the column stays tidy.
        var slabWidth = Math.Max(280 * (float)_presentation.UiScale, logoWidth * .62f);
        foreach (var slab in _titleMenuBox?.FindChildren("*", "Button", true, false).OfType<Button>() ?? []) slab.CustomMinimumSize = new Vector2(slabWidth, 0);
        // Short windows with large text: the logo gives up height so every button stays on screen (final review).
        if (_titleLogo?.GetParent() is Control box)
        {
            var rest = box.GetCombinedMinimumSize().Y - _titleLogo.CustomMinimumSize.Y;
            // At least 64 px tall (the wide logo stays legible), never taller than the width rule allows (tiny windows included).
            var height = Math.Min(Math.Max(_titleColumn.Size.Y - rest - 8, 64f), logoWidth / _titleLogoAspect);
            _titleLogo.CustomMinimumSize = new Vector2(height * _titleLogoAspect, height);
        }
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
        if (Fading) return; // a repeat press during the fade must not save or load again
        if (!TitleOpen && !TrySafetySave()) return; // loading another career from the pause menu
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
