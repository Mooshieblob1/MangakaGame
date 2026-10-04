using System;
using System.Linq;
using Godot;
using MangakaSim;

namespace MangakaGame;

public partial class DebugMain
{
    // Display settings (spec 2026-10-04, Q64): fullscreen or windowed, the window size, and one interface size that
    // scales every panel, button and text through the window's content scale. Per computer, like the sound settings.
    private static readonly double[] InterfaceSteps = [.8, .9, 1, 1.1, 1.25, 1.5, 1.75, 2];
    private DisplaySettings _display = new();
    private string? _displayPath;
    private bool _applyingDisplay, _displaySavePending;
    internal double InterfaceScale { get; private set; } = 1;
    private static bool Headless => DisplayServer.GetName() == "headless";

    private void LoadDisplaySettings()
    {
        // Automated checks keep their own window sizes at 100% and never touch the player's file.
        if (SmokeRun) { _display = new() { Mode = DisplayMode.Windowed, InterfaceSize = 1 }; _displayPath = null; }
        else { _displayPath = ProjectSettings.GlobalizePath("user://display-settings.json"); _display = DisplaySettings.Load(_displayPath); ApplyWindowMode(true); }
        GetWindow().SizeChanged += OnWindowSizeChanged;
        ApplyInterfaceSize();
    }

    private void ApplyWindowMode(bool placeWindow)
    {
        if (!Headless)
        {
            var window = GetWindow(); _applyingDisplay = true;
            try
            {
                if (_display.Mode == DisplayMode.Fullscreen) { if (window.Mode != Window.ModeEnum.Fullscreen) window.Mode = Window.ModeEnum.Fullscreen; }
                else
                {
                    if (window.Mode is not (Window.ModeEnum.Windowed or Window.ModeEnum.Maximized)) { window.Mode = Window.ModeEnum.Windowed; placeWindow = true; }
                    if (placeWindow) { if (window.Mode != Window.ModeEnum.Windowed) window.Mode = Window.ModeEnum.Windowed; PlaceWindow(new(_display.WindowWidth, _display.WindowHeight)); }
                    if (_display.Maximized && window.Mode != Window.ModeEnum.Maximized) window.Mode = Window.ModeEnum.Maximized;
                }
            }
            finally { _applyingDisplay = false; }
        }
        ApplyInterfaceSize();
    }

    // The window, title bar included, fits the screen's usable area and opens centred on it (final review: a window as
    // large as the screen used to open with its title bar off the top). Window.Position is the client area's corner.
    private Vector2I Decorations => Headless ? Vector2I.Zero : GetWindow().GetSizeWithDecorations() - GetWindow().Size;
    private Vector2I UsableClientSize => Headless ? GetWindow().Size : DisplayServer.ScreenGetUsableRect(GetWindow().CurrentScreen).Size - Decorations;
    private void PlaceWindow(Vector2I size)
    {
        var window = GetWindow(); var usable = DisplayServer.ScreenGetUsableRect(window.CurrentScreen); var frame = Decorations;
        size = new(Math.Min(size.X, usable.Size.X - frame.X), Math.Min(size.Y, usable.Size.Y - frame.Y));
        var border = frame.X / 2; var titleBar = Math.Max(0, frame.Y - border);
        var outer = size + frame;
        _applyingDisplay = true;
        try { window.Size = size; window.Position = usable.Position + (usable.Size - outer) / 2 + new Vector2I(border, titleBar); }
        finally { _applyingDisplay = false; }
    }

    private Vector2I ScreenSize => Headless ? GetWindow().Size : DisplayServer.ScreenGetSize(GetWindow().CurrentScreen);
    private double ScreenDpi => Headless ? 96 : DisplayServer.ScreenGetDpi(GetWindow().CurrentScreen);

    // The size in use is the choice (or Automatic) within what still leaves the layout 1280 x 720 of room.
    private void ApplyInterfaceSize()
    {
        var window = GetWindow();
        InterfaceScale = _display.Effective(ScreenDpi, window.Size.X, window.Size.Y);
        if (!Mathf.IsEqualApprox(window.ContentScaleFactor, (float)InterfaceScale)) window.ContentScaleFactor = (float)InterfaceScale;
        foreach (var view in new[] { _homeOffice, _officeView }.Where(v => v is not null)) { view.RenderScale = (float)InterfaceScale; view.LabelTextScale = InterfaceScale; }
        if (_managementReady) ResizeGui();
    }

    private void OnWindowSizeChanged()
    {
        ApplyInterfaceSize();
        // A window resized by dragging its edge opens at that size next time; a maximised one reopens maximised.
        var window = GetWindow();
        if (_applyingDisplay || _displayPath is null || _display.Mode != DisplayMode.Windowed || window.Mode is not (Window.ModeEnum.Windowed or Window.ModeEnum.Maximized)) return;
        // A window dragged smaller than 960 x 540 springs back (Godot's own minimum size upsets the scaled layout).
        if (window.Mode == Window.ModeEnum.Windowed && (window.Size.X < DisplaySettings.MinWindowWidth || window.Size.Y < DisplaySettings.MinWindowHeight))
        { Callable.From(() => { _applyingDisplay = true; try { window.Size = new(Math.Max(window.Size.X, DisplaySettings.MinWindowWidth), Math.Max(window.Size.Y, DisplaySettings.MinWindowHeight)); } finally { _applyingDisplay = false; } ApplyInterfaceSize(); }).CallDeferred(); return; }
        _display.Maximized = window.Mode == Window.ModeEnum.Maximized;
        if (!_display.Maximized) { var size = window.Size; _display.WindowWidth = size.X; _display.WindowHeight = size.Y; }
        if (!_displaySavePending) { _displaySavePending = true; Callable.From(SaveDisplaySettings).CallDeferred(); }
    }

    private void SaveDisplaySettings() { _displaySavePending = false; if (_displayPath is not null) _display.Save(_displayPath); }

    private void SetDisplayMode(DisplayMode mode) { _display.Mode = mode; SaveDisplaySettings(); ApplyWindowMode(true); }
    private void ToggleFullscreen() => SetDisplayMode(_display.Mode == DisplayMode.Fullscreen ? DisplayMode.Windowed : DisplayMode.Fullscreen);

    private void SetWindowSize(int width, int height)
    {
        _display.WindowWidth = width; _display.WindowHeight = height; SaveDisplaySettings();
        if (_display.Mode == DisplayMode.Windowed) ApplyWindowMode(true);
    }

    private void SetInterfaceSize(double? size)
    {
        _display.InterfaceSize = size is { } chosen ? DisplaySettings.Normalise(chosen) : null;
        SaveDisplaySettings(); ApplyInterfaceSize();
    }

    /// <summary>The Screen controls, shared by the first-launch screen, the title Settings and the in-game Settings.
    /// The first-launch screen shows the interface size only; fullscreen and window sizes stay in Settings (quick start, Q65).</summary>
    private void DisplayControls(Control parent, bool sizeOnly = false)
    {
        OptionButton? mode = null, sizes = null;
        if (!sizeOnly)
        {
            Words(parent, "Screen", 14);
            var modeButton = new OptionButton { Name = "DisplayMode" }; modeButton.AddItem("Fullscreen", (int)DisplayMode.Fullscreen); modeButton.AddItem("Windowed", (int)DisplayMode.Windowed);
            modeButton.Select(modeButton.GetItemIndex((int)_display.Mode)); parent.AddChild(modeButton);
            var sizeButton = new OptionButton { Name = "WindowSize", TooltipText = "Window size, used when windowed" }; parent.AddChild(sizeButton);
            var room = UsableClientSize; var offered = DisplaySettings.SizesFor(room.X, room.Y);
            foreach (var (w, h) in offered) sizeButton.AddItem($"Window {w} x {h}");
            var current = offered.Select((s, i) => (s, i)).OrderBy(x => Math.Abs(x.s.Width - _display.WindowWidth) + Math.Abs(x.s.Height - _display.WindowHeight)).First().i;
            sizeButton.Select(current); sizeButton.Disabled = _display.Mode == DisplayMode.Fullscreen;
            modeButton.ItemSelected += index => { SetDisplayMode((DisplayMode)modeButton.GetItemId((int)index)); sizeButton.Disabled = _display.Mode == DisplayMode.Fullscreen; };
            sizeButton.ItemSelected += index => SetWindowSize(offered[(int)index].Width, offered[(int)index].Height);
            mode = modeButton; sizes = sizeButton;
        }

        Words(parent, "Interface size · buttons, panels and text", 14);
        var size = new OptionButton { Name = "InterfaceSize" }; parent.AddChild(size);
        var note = Words(parent, "", 13); note.ThemeTypeVariation = "QuietLabel";
        void Fill()
        {
            size.Clear();
            var window = GetWindow().Size; var largest = DisplaySettings.LargestFit(window.X, window.Y);
            size.AddItem($"Automatic ({DisplaySettings.AutomaticSize(ScreenDpi) * 100:0}% from Windows scaling)", 0);
            foreach (var step in InterfaceSteps.Where(s => s <= largest + 1e-9)) size.AddItem($"{step * 100:0}%", (int)Math.Round(step * 100));
            var chosen = _display.InterfaceSize is { } c ? (int)Math.Round(c * 100) : 0;
            // A saved size larger than this window allows stays listed, so the choice never looks lost (final review).
            if (chosen > 0 && size.GetItemIndex(chosen) < 0) size.AddItem($"{chosen}% (limited to {InterfaceScale * 100:0}% here)", chosen);
            size.Select(Math.Max(0, size.GetItemIndex(chosen)));
            if (mode is not null && sizes is not null) { mode.Select(mode.GetItemIndex((int)_display.Mode)); sizes.Disabled = _display.Mode == DisplayMode.Fullscreen; }
            note.Text = $"In use: {InterfaceScale * 100:0}%" + (InterfaceScale >= largest - 1e-9 ? " · the largest for this window" : "") +
                ". F11 or Alt+Enter switches fullscreen.";
        }
        size.ItemSelected += index => { var id = size.GetItemId((int)index); SetInterfaceSize(id == 0 ? null : id / 100d); Fill(); };
        if (mode is not null) mode.ItemSelected += _ => Callable.From(Fill).CallDeferred();
        if (sizes is not null) sizes.ItemSelected += _ => Callable.From(Fill).CallDeferred();
        // F11, Alt+Enter or dragging the window edge while Settings is open keeps these controls current (final review).
        void Refresh() { if (GodotObject.IsInstanceValid(size) && size.IsInsideTree()) Callable.From(Fill).CallDeferred(); }
        var window = GetWindow(); window.SizeChanged += Refresh; size.TreeExiting += () => window.SizeChanged -= Refresh;
        Fill();
    }
}
