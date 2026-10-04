using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using MangakaSim;

namespace MangakaGame;

public partial class DebugMain
{
    // Display settings (spec 2026-10-04, Q64): interface size, fullscreen and windowed, per computer.
    private async void RunDisplaySmoke()
    {
        SetProcess(false);
        try
        {
            Directory.CreateDirectory(SmokeOutput);
            _careers = new CareerStore(Path.Combine(SmokeOutput, "display-" + Guid.NewGuid().ToString("N")));
            GetWindow().Size = new(1920, 1080); await SettleUi();
            await CheckDisplayControlsEverywhere();
            NewCareerMenu(); Press("Begin career"); await SettleUi(); _helperPopup.Hide();
            await CheckInterfaceScaling();
            await CheckDisplayModeAndShortcuts();
            GD.Print($"DISPLAY SMOKE PASSED: {_smokeChecks} checks.");
            var tree = GetTree(); tree.CreateTimer(.1).Timeout += () => QuitTree(tree);
        }
        catch (Exception ex) { GD.PushError($"DISPLAY SMOKE FAILED: {ex.Message}\n{ex.StackTrace}"); GetTree().Quit(1); }
    }

    /// <summary>For checks: a layout of this size at this interface size (the window is the size times the scale).</summary>
    private void SmokeLayout(Vector2I size, double scale)
    {
        _display.InterfaceSize = scale;
        GetWindow().Size = new((int)Math.Round(size.X * scale), (int)Math.Round(size.Y * scale));
        ApplyInterfaceSize();
    }

    private bool HasDisplayControls(Node root) =>
        root.FindChildren("DisplayMode", "OptionButton", true, false).Any() && root.FindChildren("InterfaceSize", "OptionButton", true, false).Any();

    private async Task CheckDisplayControlsEverywhere()
    {
        BeginStartup(true); SkipDisclaimer(); await SettleUi();
        // Quick start (Q65): the first-launch screen offers the interface size; fullscreen and window sizes wait in Settings.
        Check(_startupRoot is not null && _startupRoot.FindChildren("InterfaceSize", "OptionButton", true, false).Any() && !_startupRoot.FindChildren("DisplayMode", "OptionButton", true, false).Any(),
            "The first-launch screen shows the interface size only");
        EndStartup(); await SettleUi();
        SettingsMenu(); await SettleUi();
        Check(HasDisplayControls(TitleOpen ? _titlePageContent! : _menuContent), "The title screen's Settings show the display choices");
        ShowMenu(); await SettleUi();
    }

    private async Task CheckInterfaceScaling()
    {
        GetWindow().Size = new(1920, 1080); SetInterfaceSize(1); await SettleUi();
        var label = _currentCopies; var before = label.GetGlobalRect().Size.Y * InterfaceScale;
        SetInterfaceSize(1.5); await SettleUi();
        var view = GetViewportRect().Size;
        Check(Mathf.IsEqualApprox(GetWindow().ContentScaleFactor, 1.5f) && Math.Abs(view.X - 1280) < 2 && Math.Abs(view.Y - 720) < 2,
            $"150% at 1920 x 1080 lays the interface out in 1280 x 720 ({view.X:0} x {view.Y:0})");
        var after = label.GetGlobalRect().Size.Y * InterfaceScale;
        Check(after > before * 1.3, $"Text is physically larger at 150% ({before:0} to {after:0} screen pixels)");
        var office = _homeOffice.RenderViewport;
        GD.Print($"OFFICE RENDER: container {_homeOffice.Size} logical, viewport {office?.Size}, window {GetWindow().Size}");
        Check(office is not null && office.Size.X >= _homeOffice.Size.X * 1.45f, $"The 3D office renders at full window pixels ({office?.Size} for {_homeOffice.Size} at 150%)");
        ShowOffice(); await SettleUi(); await SettleUi();
        // Clicks still land where they look at 150%, though the 3D image is 1.5 times the layout size (final review).
        // Where a point appears on screen, worked out here from the camera and the picture's real sizes, independently of
        // the office's own conversion, so a broken conversion cannot hide behind itself.
        Vector2 OnScreen(Vector3 world) => _homeOffice.RenderViewport.GetCamera3D().UnprojectPosition(world) * (_homeOffice.Size / (Vector2)_homeOffice.RenderViewport.Size);
        var lead = _homeOffice.StaffActors[_state.ProtagonistPersonId]; _homeOffice.SelectedPerson = 0;
        _homeOffice._GuiInput(new InputEventMouseButton { ButtonIndex = MouseButton.Left, Pressed = true, Position = OnScreen(lead.GlobalPosition + new Vector3(0, 1, 0)) });
        Check(_homeOffice.SelectedPerson == _state.ProtagonistPersonId, "At 150% a click on Aki selects Aki");
        ShowOffice(); await SettleUi();
        OfficeCell? clicked = null; void Ground(OfficeCell c) => clicked = c; _homeOffice.GroundClicked += Ground;
        var cell = new OfficeCell(8, 13);
        _homeOffice._GuiInput(new InputEventMouseButton { ButtonIndex = MouseButton.Left, Pressed = true, Position = OnScreen(new Vector3((cell.X + .5f) * .25f, 0, (cell.Z + .5f) * .25f)) });
        _homeOffice.GroundClicked -= Ground;
        Check(clicked == cell, $"At 150% a click on the floor finds the cell under the pointer ({clicked})");
        int CaptionSize() => _homeOffice.RenderViewport.FindChildren("*", "Label", true, false).OfType<Label>().Where(l => l.Visible && l.TopLevel)
            .Select(l => l.GetThemeFontSize("font_size")).DefaultIfEmpty(0).Max();
        var large = CaptionSize();
        await CaptureSmokeImage("display-150-1920x1080");
        SetInterfaceSize(1); await SettleUi(); await SettleUi(); var normal = CaptionSize();
        GD.Print($"LABELS: {normal} at 100%, {large} at 150% (office label scale {_homeOffice.LabelTextScale})");
        Check(large >= normal * 1.4, $"Office name tags grow with the interface size ({normal} to {large})");
        await CaptureSmokeImage("display-100-1920x1080"); SetInterfaceSize(1.5); await SettleUi();
        SetInterfaceSize(2); await SettleUi();
        Check(Mathf.IsEqualApprox((float)InterfaceScale, 1.5f) && _display.InterfaceSize == 2, "A size larger than fits is used at the limit without forgetting the choice");
        GetWindow().Size = new(2560, 1440); await SettleUi();
        Check(Mathf.IsEqualApprox((float)InterfaceScale, 2f), "A larger window allows 200%");
        GetWindow().Size = new(1920, 1080); SetInterfaceSize(null); await SettleUi();
        Check(Mathf.IsEqualApprox((float)InterfaceScale, (float)Math.Min(DisplaySettings.AutomaticSize(ScreenDpi), 1.5)), "Automatic follows the screen's scaling");
        SettingsMenu(); await SettleUi();
        var sizes = _menuContent.FindChildren("InterfaceSize", "OptionButton", true, false).OfType<OptionButton>().First();
        Check(HasDisplayControls(_menuContent) && Enumerable.Range(0, sizes.ItemCount).All(i => sizes.GetItemId(i) <= 150),
            "In-game Settings show the display choices, offering only sizes that fit the window");
        Check(!_menuContent.FindChildren("*", "HSlider", true, false).OfType<HSlider>().Any(s => s.MaxValue == 1.5 && s.MinValue == .8),
            "The old per-career text scale slider is gone");
        _menu.Hide(); _inMenu = false; SetInterfaceSize(1); await SettleUi();
    }

    private async Task CheckDisplayModeAndShortcuts()
    {
        var path = Path.Combine(SmokeOutput, "display-" + Guid.NewGuid().ToString("N"), "display-settings.json");
        _displayPath = path;
        var mode = _display.Mode;
        _Input(new InputEventKey { Keycode = Key.F11, Pressed = true }); await SettleUi();
        Check(_display.Mode != mode, "F11 switches between fullscreen and windowed");
        _Input(new InputEventKey { Keycode = Key.Enter, AltPressed = true, Pressed = true }); await SettleUi();
        Check(_display.Mode == mode, "Alt+Enter switches back");
        SetWindowSize(1600, 900); SetInterfaceSize(1.25);
        var loaded = DisplaySettings.Load(path);
        Check(loaded.Mode == _display.Mode && loaded.WindowWidth == 1600 && loaded.InterfaceSize == 1.25, "Choices are saved for this computer at once");
        _displayPath = null; _display = new() { Mode = DisplayMode.Windowed, InterfaceSize = 1 }; ApplyInterfaceSize(); await SettleUi();
    }
}
