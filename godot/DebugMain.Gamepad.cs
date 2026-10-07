using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using MangakaSim;

namespace MangakaGame;

public partial class DebugMain
{
    // Controller support (2026-10-06): every screen by gamepad. The stick or D-pad moves the focus (held, it repeats),
    // A presses, B steps back, LB and RB flip through the rail, Y pauses, LT and RT change speed, Start opens the menu,
    // View the inbox, X Helper-Chan's phone, the right stick scrolls a page or looks around the office, R3 zooms.
    // A ring marks the focused control and a prompt bar shows the buttons, both only while the controller (or, for the
    // ring, the keyboard) is in use. Popups and menus keep the focus inside them. Nothing here touches the simulation.
    private enum InputKind { Mouse, Keyboard, Pad }
    private InputKind _lastInput = InputKind.Mouse;
    private bool PadActive => _lastInput == InputKind.Pad;
    private readonly PadStick _padStick = new();
    private readonly PadRepeat _padRepeat = new();
    private readonly PadTrigger _padSlower = new(), _padFaster = new();
    private Vector2 _leftStick;
    private CanvasLayer? _padLayer;
    private FocusRing? _focusRing;
    private ControllerHints? _padHintsGame, _padHintsOverlay;
    private PanelContainer? _padHintsPanel;
    private Control? _focusTrap;
    private bool _focusTrapSet, _onSteamDeck;
    private Control? _phoneReturn;
    // The last control focused in each area (the game, the menu, a popup), so closing a popup or rebuilding a page
    // puts the controller back where it was rather than at the top.
    private readonly Dictionary<Control, (Control Control, string Label)> _padMemory = new();

    private void BuildGamepad()
    {
        PrepareInputMap();
        _padLayer = new CanvasLayer { Layer = 110, Name = "ControllerLayer" }; AddChild(_padLayer);
        _focusRing = new FocusRing { Name = "FocusRing" }; _padLayer.AddChild(_focusRing); _focusRing.SetAnchorsPreset(LayoutPreset.FullRect);
        var panelStyle = Surface(new Color(BrandPalette.Evening.Wash) with { A = .92f }, 8);
        panelStyle.BorderColor = new Color(BrandPalette.Evening.Outline); panelStyle.SetBorderWidthAll(1);
        _padHintsPanel = new PanelContainer { Name = "ControllerHintsPanel", MouseFilter = MouseFilterEnum.Ignore, Visible = false };
        _padHintsPanel.AddThemeStyleboxOverride("panel", panelStyle); _padLayer.AddChild(_padHintsPanel);
        _padHintsOverlay = new ControllerHints(); _padHintsPanel.AddChild(_padHintsOverlay);
        // In a career the prompts join the notice strip, so the floating layout makes room for them.
        _padHintsGame = new ControllerHints { Visible = false }; _notice.GetParent().AddChild(_padHintsGame);
        _padHintsGame.Resized += ResizeFloatingOffice;
        GetTree().NodeAdded += OnPadNodeAdded;
        TreeExiting += () => GetTree().NodeAdded -= OnPadNodeAdded;
        foreach (var dialog in FindChildren("*", "AcceptDialog", true, false).OfType<AcceptDialog>()) WatchDialog(dialog);
        _menu.VisibilityChanged += UpdateFocusTrap; _helperPopup.VisibilityChanged += UpdateFocusTrap;
        // A Steam Deck starts ready for its controls; any mouse or trackpad movement switches back.
        _onSteamDeck = _steam.OnSteamDeck;
        if (_onSteamDeck) SetInputKind(InputKind.Pad);
    }

    // Godot's own menus and dialogs (drop-down lists, confirmations) answer to the ui actions, so those also take the
    // D-pad, the left stick, A and B. Events are added for every controller, not just the first.
    private static void PrepareInputMap()
    {
        void Add(string action, InputEvent ev) { ev.Device = -1; if (InputMap.HasAction(action) && !InputMap.ActionHasEvent(action, ev)) InputMap.ActionAddEvent(action, ev); }
        Add("ui_accept", new InputEventJoypadButton { ButtonIndex = JoyButton.A });
        Add("ui_cancel", new InputEventJoypadButton { ButtonIndex = JoyButton.B });
        foreach (var (action, button, axis, sign) in new[] { ("ui_up", JoyButton.DpadUp, JoyAxis.LeftY, -1f), ("ui_down", JoyButton.DpadDown, JoyAxis.LeftY, 1f),
            ("ui_left", JoyButton.DpadLeft, JoyAxis.LeftX, -1f), ("ui_right", JoyButton.DpadRight, JoyAxis.LeftX, 1f) })
        {
            Add(action, new InputEventJoypadButton { ButtonIndex = button });
            Add(action, new InputEventJoypadMotion { Axis = axis, AxisValue = sign });
        }
    }

    private void OnPadNodeAdded(Node node) { if (node is AcceptDialog dialog) WatchDialog(dialog); }

    // A dialog opens with a button focused so the controller can answer it. A confirmation starts on Cancel, so a
    // quick A never confirms something by accident.
    private void WatchDialog(AcceptDialog dialog)
    {
        dialog.AboutToPopup += () => Callable.From(() =>
        {
            if (!IsInstanceValid(dialog) || !dialog.Visible) return;
            var button = dialog is ConfirmationDialog confirm ? confirm.GetCancelButton() : dialog.GetOkButton();
            button.GrabFocus(_lastInput == InputKind.Mouse);
        }).CallDeferred();
    }

    private void SetInputKind(InputKind kind)
    {
        if (_lastInput == kind) return;
        var wasPad = _lastInput == InputKind.Pad;
        _lastInput = kind;
        if (Headless) return;
        if (kind == InputKind.Pad) Input.MouseMode = Input.MouseModeEnum.Hidden;
        else if (wasPad) Input.MouseMode = Input.MouseModeEnum.Visible;
    }

    /// <summary>Called first from _Input. True when the controller event was used here.</summary>
    private bool HandlePadInput(InputEvent ev)
    {
        switch (ev)
        {
            case InputEventMouseMotion motion when motion.Relative.LengthSquared() > 9: SetInputKind(InputKind.Mouse); return false;
            case InputEventMouseButton { Pressed: true }: SetInputKind(InputKind.Mouse); return false;
            case InputEventKey { Pressed: true }: SetInputKind(InputKind.Keyboard); return false;
            case InputEventJoypadButton button: return PadButton(button);
            case InputEventJoypadMotion motion: return PadMotion(motion);
            default: return false;
        }
    }

    private static PadDirection DpadDirection(JoyButton button) => button switch
    {
        JoyButton.DpadUp => PadDirection.Up, JoyButton.DpadDown => PadDirection.Down,
        JoyButton.DpadLeft => PadDirection.Left, JoyButton.DpadRight => PadDirection.Right, _ => PadDirection.None,
    };

    private bool PadButton(InputEventJoypadButton button)
    {
        // Any button skips the start-up disclaimer; that screen listens for itself.
        if (_startup is not null && !_disclaimerDone) { SetInputKind(InputKind.Pad); return false; }
        var direction = DpadDirection(button.ButtonIndex);
        if (!button.Pressed) { if (direction != PadDirection.None) _padRepeat.Release(direction); return direction != PadDirection.None && !WindowOpen; }
        SetInputKind(InputKind.Pad);
        if (WindowOpen) { _padRepeat.Clear(); return false; } // a dialog or drop-down list answers the ui actions itself
        if (direction != PadDirection.None) { PadMove(direction); _padRepeat.Press(direction); return true; }
        switch (button.ButtonIndex)
        {
            case JoyButton.A: return PadAccept();
            case JoyButton.B: PadBack(); return true;
            case JoyButton.Start: PadStart(); return true;
            case JoyButton.Back: PadInbox(); return true;
            case JoyButton.X: PadPhone(); return true;
            case JoyButton.Y: if (PadGameAvailable) TogglePause(); return true;
            case JoyButton.LeftShoulder: PadSection(-1); return true;
            case JoyButton.RightShoulder: PadSection(1); return true;
            case JoyButton.RightStick: PadZoom(); return true;
            default: return false;
        }
    }

    private bool PadMotion(InputEventJoypadMotion motion)
    {
        if (Fading) return true;
        switch (motion.Axis)
        {
            case JoyAxis.LeftX: _leftStick.X = motion.AxisValue; break;
            case JoyAxis.LeftY: _leftStick.Y = motion.AxisValue; break;
            case JoyAxis.TriggerLeft: if (_padSlower.Update(motion.AxisValue)) { SetInputKind(InputKind.Pad); PadSpeed(false); } return true;
            case JoyAxis.TriggerRight: if (_padFaster.Update(motion.AxisValue)) { SetInputKind(InputKind.Pad); PadSpeed(true); } return true;
            default: if (Math.Abs(motion.AxisValue) > .5f) SetInputKind(InputKind.Pad); return true; // the right stick is read each frame
        }
        var before = _padStick.Direction;
        var entered = _padStick.Update(_leftStick.X, _leftStick.Y);
        if (_padStick.Direction == PadDirection.None && before != PadDirection.None) _padRepeat.Release(before);
        if (entered == PadDirection.None) return true;
        SetInputKind(InputKind.Pad);
        if (_startup is not null && !_disclaimerDone) return true;
        if (WindowOpen) return false;
        PadMove(entered); _padRepeat.Press(entered);
        return true;
    }

    /// <summary>Each frame: held directions repeat, the right stick scrolls or looks, focus stays somewhere useful,
    /// and the ring and prompts follow.</summary>
    private void UpdatePad(double delta)
    {
        if (!_managementReady || _focusRing is null) return;
        UpdateFocusTrap();
        if (!Fading && _padRepeat.Held != PadDirection.None)
            for (var steps = _padRepeat.Update(delta); steps > 0; steps--) PadMove(_padRepeat.Held);
        PadRightStick(delta);
        if (PadActive) EnsurePadFocus();
        UpdatePadOverlay(delta);
    }

    private bool WindowOpen => GetViewport().GetEmbeddedSubwindows().Any(w => w.Visible);

    // Time, sections, the inbox and the phone wait while a menu, popup, conversation, dialog or fade is up.
    private bool PadGameAvailable => _managementReady && _startup is null && !TitleOpen && !Fading && !_inMenu && !_helperPopup.Visible &&
        !_storyOpen && !OfficeEditing && !GetChildren().OfType<Window>().Any(w => w.Visible);

    /// <summary>The area the controller is working in: the topmost of start-up, title, popup, menu, then the game.</summary>
    private Control? PadRoot()
    {
        if (_startup is not null) return _startupRoot;
        if (TitleOpen) return _titlePage is { Visible: true } ? _titlePage : _titleColumn;
        if (_helperPopup.Visible) return _helperPopup;
        if (_menu.Visible) return _menu;
        return _floatingUi;
    }

    // A menu, popup or the title keeps the focus inside it: everything behind stops taking focus until it closes.
    // Runs on their visibility changes, so code that focuses a control right after closing one finds it enabled.
    private void UpdateFocusTrap()
    {
        if (!_managementReady) return;
        Control? modal = TitleOpen ? _title : _helperPopup.Visible ? _helperPopup : _menu.Visible ? _menu : null;
        if (_focusTrapSet && modal == _focusTrap) return;
        _focusTrapSet = true; _focusTrap = modal;
        foreach (var child in GetChildren().OfType<Control>())
            child.FocusBehaviorRecursive = modal is null || child == modal || child.IsAncestorOf(modal)
                ? FocusBehaviorRecursiveEnum.Inherited : FocusBehaviorRecursiveEnum.Disabled;
    }

    private static bool Focusable(Control control) =>
        IsInstanceValid(control) && control.IsInsideTree() && control.IsVisibleInTree() && control.GetFocusModeWithOverride() == FocusModeEnum.All;

    private static bool PadFocusValid(Control? control, Control? root) =>
        control is not null && root is not null && Focusable(control) && (control == root || root.IsAncestorOf(control));

    /// <summary>The first control a controller can land on, in reading order; an enabled one before a disabled one.</summary>
    private static Control? FirstFocusable(Control? root)
    {
        if (root is null || !root.IsVisibleInTree()) return null;
        Control? disabled = null;
        foreach (var control in Descendants(root))
        {
            if (!Focusable(control)) continue;
            if (control is BaseButton { Disabled: true }) { disabled ??= control; continue; }
            return control;
        }
        return disabled;
    }

    private static IEnumerable<Control> Descendants(Control root)
    {
        foreach (var child in root.GetChildren().OfType<Control>())
        {
            if (!child.Visible) continue;
            yield return child;
            foreach (var inner in Descendants(child)) yield return inner;
        }
    }

    private static string FocusLabel(Control control) => control switch { Button b => b.Text, LineEdit e => "edit:" + e.PlaceholderText + e.Name, _ => control.Name };

    /// <summary>Where the controller lands in an area with nothing focused.</summary>
    private Control? PadDefault(Control root)
    {
        if (root == _startupRoot) return _volumeSetup is { } volume ? FirstFocusable(volume) : _startupRoot;
        if (root == _titleColumn) return _titleMainButton is { } main && Focusable(main) ? main : FirstFocusable(_titleColumn);
        if (root == _titlePage) return FirstFocusable(_titlePageContent);
        if (root == _menu) return FirstFocusable(_pauseMenuContent);
        if (root == _helperPopup) return FirstFocusable(_helperPopup);
        return GameDefault();
    }

    // In a career: the open page's first control, else the highlighted rail button.
    private Control? GameDefault()
    {
        if (_report.Visible) return FirstFocusable(_mainTabs.GetCurrentTabControl()) ?? FirstFocusable(_report);
        if (_side.Visible) return FirstFocusable(_sideContent) ?? FirstFocusable(_side);
        return _navigation.Values.FirstOrDefault(b => b.ButtonPressed && Focusable(b)) ?? FirstFocusable(_floatingRail);
    }

    private void EnsurePadFocus()
    {
        var root = PadRoot();
        if (root is null || Fading || GetViewport().GetEmbeddedSubwindows().Any(w => w.Visible)) return;
        if (root == _startupRoot && !_disclaimerDone) return;
        var owner = GetViewport().GuiGetFocusOwner();
        if (PadFocusValid(owner, root)) { _padMemory[root] = (owner!, FocusLabel(owner!)); return; }
        RestorePadFocus(root);
    }

    // The same control if it is still there, else one with the same label (a rebuilt page), else the area's default.
    private void RestorePadFocus(Control root)
    {
        Control? target = null;
        if (_padMemory.TryGetValue(root, out var memory))
        {
            if (PadFocusValid(memory.Control, root)) target = memory.Control;
            else if (memory.Label.Length > 0) target = Descendants(root).FirstOrDefault(c => Focusable(c) && FocusLabel(c) == memory.Label);
        }
        target ??= PadDefault(root) ?? FirstFocusable(root);
        if (target is not null) FocusPad(target);
    }

    private void FocusPad(Control control)
    {
        control.GrabFocus();
        // Scroll every surrounding page so the control is fully in view.
        for (var parent = control.GetParent(); parent is not null; parent = parent.GetParent())
            if (parent is ScrollContainer scroll) scroll.CallDeferred(ScrollContainer.MethodName.EnsureControlVisible, control);
    }

    private void PadMove(PadDirection direction)
    {
        if (Fading || direction == PadDirection.None) return;
        var root = PadRoot();
        if (root is null) return;
        var owner = GetViewport().GuiGetFocusOwner();
        if (!PadFocusValid(owner, root)) { RestorePadFocus(root); return; }
        var sign = direction is PadDirection.Right or PadDirection.Down ? 1 : -1;
        // Left and right change sliders and number boxes; up and down always move on.
        if (direction is PadDirection.Left or PadDirection.Right)
        {
            if (owner is Slider slider) { slider.Value += sign * Math.Max(slider.Step, (slider.MaxValue - slider.MinValue) / 20); return; }
            if (owner!.GetParent() is SpinBox spin) { spin.Value += sign * (spin.CustomArrowStep > 0 ? spin.CustomArrowStep : spin.Step > 0 ? spin.Step : 1); return; }
        }
        else if (owner is ItemList list && list.ItemCount > 0)
        {
            var selected = list.GetSelectedItems();
            var next = selected.Length == 0 ? (sign > 0 ? 0 : -1) : selected[0] + sign;
            if (next >= 0 && next < list.ItemCount) { list.Select(next); list.EmitSignal(ItemList.SignalName.ItemSelected, next); list.EnsureCurrentIsVisible(); return; }
        }
        if (PadNeighbour(owner!, direction, root) is { } neighbour) FocusPad(neighbour);
    }

    /// <summary>The control a direction leads to inside the area, or null. Nothing that way falls back to the
    /// reading order, so every control stays reachable.</summary>
    private static Control? PadNeighbour(Control owner, PadDirection direction, Control root)
    {
        var neighbour = PadSpatial(owner, direction, root);
        if (!PadFocusValid(neighbour, root))
            neighbour = direction is PadDirection.Right or PadDirection.Down ? owner.FindNextValidFocus() : owner.FindPrevValidFocus();
        return PadFocusValid(neighbour, root) && neighbour != owner ? neighbour : null;
    }

    // Godot's own neighbour search skipped whole groups of buttons in the floating panels (the controller smoke found
    // 107 unreachable), so the controller uses its own: the nearest control that lies that way, preferring ones in
    // line with the current control, then the one whose edge lines up (reading order). Controls scrolled out of a page
    // still count by where they sit, so moving towards them scrolls them in.
    private static Control? PadSpatial(Control owner, PadDirection direction, Control root)
    {
        var a = owner.GetGlobalRect();
        Control? best = null; var bestScore = float.MaxValue;
        foreach (var candidate in Descendants(root))
        {
            if (candidate == owner || !Focusable(candidate)) continue;
            var b = candidate.GetGlobalRect();
            if (b.Size.X <= 0 || b.Size.Y <= 0) continue;
            var vertical = direction is PadDirection.Up or PadDirection.Down;
            var sign = direction is PadDirection.Down or PadDirection.Right ? 1 : -1;
            // Along the direction: the candidate's near edge must be past the middle of the current control's extent.
            float aStart = vertical ? a.Position.Y : a.Position.X, aEnd = vertical ? a.End.Y : a.End.X;
            float bStart = vertical ? b.Position.Y : b.Position.X, bEnd = vertical ? b.End.Y : b.End.X;
            var half = Math.Min(aEnd - aStart, bEnd - bStart) * .5f;
            if (sign > 0 ? !(bStart >= aStart + half && (bStart + bEnd) > (aStart + aEnd) + 2) : !(bEnd <= aEnd - half && (bStart + bEnd) < (aStart + aEnd) - 2)) continue;
            var primary = Math.Max(0, sign > 0 ? bStart - aEnd : aStart - bEnd);
            // Across the direction: the gap between the two (zero when they overlap) and how well their edges line up.
            float cStart = vertical ? a.Position.X : a.Position.Y, cEnd = vertical ? a.End.X : a.End.Y;
            float dStart = vertical ? b.Position.X : b.Position.Y, dEnd = vertical ? b.End.X : b.End.Y;
            var gap = Math.Max(0, Math.Max(cStart - dEnd, dStart - cEnd));
            var align = vertical ? Math.Abs(dStart - cStart) : Math.Abs((dStart + dEnd) - (cStart + cEnd)) / 2;
            var score = primary + gap * 3 + align * .05f;
            if (score < bestScore) { bestScore = score; best = candidate; }
        }
        return best;
    }

    private bool PadAccept()
    {
        if (Fading) return true;
        var root = PadRoot();
        if (root is null) return false;
        if (GetViewport().GetEmbeddedSubwindows().Any(w => w.Visible)) return false;
        var owner = GetViewport().GuiGetFocusOwner();
        if (!PadFocusValid(owner, root)) { RestorePadFocus(root); return true; }
        if (owner is LineEdit edit)
        {
            if (!edit.Editable) return true;
            edit.Edit();
            // On a Steam Deck or in Big Picture, Steam's own keyboard types into the box.
            var area = edit.GetGlobalRect(); var scale = GetWindow().ContentScaleFactor;
            _steam.ShowKeyboard(new Rect2I((int)(area.Position.X * scale), (int)(area.Position.Y * scale), (int)(area.Size.X * scale), (int)(area.Size.Y * scale)), edit.GetParent() is SpinBox);
            return true;
        }
        return false; // Godot presses the focused button, ticks the box or opens the list
    }

    private void PadBack()
    {
        if (_startup is not null || Fading) return;
        if (GetViewport().GuiGetFocusOwner() is LineEdit edit && edit.IsEditing()) { edit.Unedit(); return; }
        var inPhone = _phoneOpen && GetViewport().GuiGetFocusOwner() is { } owner && _phone.IsAncestorOf(owner);
        if (_phoneOpen && PadGameAvailable && (inPhone || !PageOpen)) { PadClosePhone(); return; }
        StepBack();
    }

    private void PadStart()
    {
        if (_startup is not null || Fading || TitleOpen || !_managementReady) return;
        if (GetChildren().OfType<Window>().Any(w => w.Visible)) return;
        if (_storyOpen) { Notify("Choose an answer, Read later or Skip to close this conversation."); return; }
        if (_helperPopup.Visible) { _helperPopup.Hide(); return; }
        if (OfficeEditing) { Notify("Apply or discard your furniture changes first."); return; }
        if (_menu.Visible) { _menu.Hide(); _inMenu = false; return; }
        ShowMenu();
    }

    private void PadInbox()
    {
        if (!PadGameAvailable) return;
        _inboxShortcut.EmitSignal(BaseButton.SignalName.Pressed);
        FocusPageLater();
    }

    private void PadPhone()
    {
        if (!PadGameAvailable) return;
        if (_phoneOpen) { PadClosePhone(); return; }
        _phoneReturn = GetViewport().GuiGetFocusOwner();
        OpenPhone(false);
        Callable.From(() =>
        {
            if (!_phoneOpen) return;
            var replies = _phone.FindChildren("*", "Button", true, false).OfType<Button>().Where(Focusable).ToArray();
            if ((replies.FirstOrDefault(b => b.Name.ToString().StartsWith("Guidance")) ?? replies.LastOrDefault()) is { } reply) FocusPad(reply);
        }).CallDeferred();
    }

    private void PadClosePhone()
    {
        ClosePhone();
        if (_phoneReturn is { } back && PadFocusValid(back, _floatingUi)) FocusPad(back);
        _phoneReturn = null;
    }

    private void PadSection(int step)
    {
        if (!PadGameAvailable) return;
        var sections = _rail.GetChildren().OfType<Button>().Where(b => _navigation.ContainsValue(b) && b.IsVisibleInTree()).ToList();
        if (sections.Count == 0) return;
        var current = sections.FindIndex(b => b.ButtonPressed);
        var next = current < 0 ? (step > 0 ? 0 : sections.Count - 1) : (current + step + sections.Count) % sections.Count;
        sections[next].EmitSignal(BaseButton.SignalName.Pressed);
        FocusPageLater();
    }

    // After a section or the inbox opens, the controller lands on the new page's first control.
    private void FocusPageLater() => Callable.From(() => { if (PadRoot() == _floatingUi && GameDefault() is { } target) FocusPad(target); }).CallDeferred();

    private void PadSpeed(bool faster) { if (PadGameAvailable) StepSpeed(faster); }

    private OfficeView PadOfficeView => _report.Visible ? _officeView : _homeOffice;

    private void PadZoom()
    {
        if (!(PadGameAvailable || OfficeEditing && !_inMenu)) return;
        if (PadOfficeView.IsVisibleInTree()) PadOfficeView.CycleZoom();
    }

    private void PadRightStick(double delta)
    {
        var stick = Vector2.Zero;
        foreach (var id in Input.GetConnectedJoypads())
        {
            var reading = new Vector2(Input.GetJoyAxis(id, JoyAxis.RightX), Input.GetJoyAxis(id, JoyAxis.RightY));
            if (reading.Length() > stick.Length()) stick = reading;
        }
        const float deadzone = .2f;
        if (stick.Length() < deadzone || Fading || _startup is not null) return;
        stick = stick.Normalized() * (stick.Length() - deadzone) / (1 - deadzone);
        if (!OfficeEditing && PadScrollTarget() is { } scroll)
        {
            scroll.ScrollVertical += (int)Math.Round(stick.Y * delta * 1400);
            return;
        }
        if (!(PadGameAvailable || OfficeEditing && !_inMenu)) return;
        var view = PadOfficeView;
        if (view.IsVisibleInTree()) view.PanScreenPixels(-stick * (float)Math.Min(delta, .1) * view.Size.Y * .65f);
    }

    // The scrolling area the right stick moves: the open menu, popup or page, or the phone while it has the focus.
    private ScrollContainer? PadScrollTarget()
    {
        if (TitleOpen) return _titlePage is { Visible: true } ? _titlePageContent?.GetParent() as ScrollContainer : _titleColumn;
        if (_helperPopup.Visible) return _helperPopup.FindChildren("*", "ScrollContainer", true, false).OfType<ScrollContainer>().FirstOrDefault(s => s.IsVisibleInTree());
        if (_menu.Visible) return _pauseMenuContent.GetParent() as ScrollContainer;
        if (_phoneOpen && GetViewport().GuiGetFocusOwner() is { } owner && _phone.IsAncestorOf(owner)) return _phoneScroll;
        if (_report.Visible) return _mainTabs.GetCurrentTabControl() as ScrollContainer;
        return _side.Visible ? _sideScroll : null;
    }

    private void UpdatePadOverlay(double delta)
    {
        if (_focusRing is null || _padHintsGame is null || _padHintsOverlay is null || _padHintsPanel is null) return;
        _focusRing.ReducedMotion = _presentation.ReducedUiMotion;
        var owner = GetViewport().GuiGetFocusOwner();
        var dialogOpen = GetViewport().GetEmbeddedSubwindows().Any(w => w.Visible);
        Rect2? ring = null;
        if (_lastInput != InputKind.Mouse && !dialogOpen && !Fading && owner is not null && owner != _startupRoot && owner.IsVisibleInTree() && owner.HasFocus(true))
            ring = VisibleArea(owner.HasMeta("focus_frame") && owner.GetMeta("focus_frame").As<Control>() is { } frame && IsInstanceValid(frame) ? frame : owner);
        _focusRing.Track(ring, delta);

        var show = PadActive && !Fading && !(_startup is not null && !_disclaimerDone);
        var context = PadContextNow(owner);
        var inGame = context.Screen == PadScreen.Game;
        var tip = owner is not null && owner.IsVisibleInTree() ? PadTip(owner) : "";
        _padHintsGame.Visible = show && inGame;
        _padHintsPanel.Visible = show && !inGame;
        if (_padHintsGame.Visible) { _padHintsGame.TextColour = Ink; _padHintsGame.SetPrompts(PadPrompts.For(context), tip); }
        if (_padHintsPanel.Visible)
        {
            _padHintsOverlay.SetPrompts(PadPrompts.For(context), tip);
            var window = GetViewportRect().Size;
            var width = Math.Min(window.X - 32, _padHintsOverlay.NaturalWidth() + 20);
            _padHintsPanel.Size = new Vector2(width, 0);
            _padHintsPanel.Position = new Vector2((window.X - _padHintsPanel.Size.X) / 2, window.Y - _padHintsPanel.Size.Y - 12);
        }
    }

    // The control's rectangle, cut to the scrolling areas around it; null once it has scrolled out of sight.
    private static Rect2? VisibleArea(Control control)
    {
        var area = control.GetGlobalRect();
        for (var parent = control.GetParent(); parent is not null; parent = parent.GetParent())
            if (parent is ScrollContainer scroll) area = area.Intersection(scroll.GetGlobalRect());
        return area.Size.X >= 2 && area.Size.Y >= 2 ? area : null;
    }

    private PadContext PadContextNow(Control? owner)
    {
        var screen = _startup is not null ? PadScreen.Startup : TitleOpen ? (_titlePage is { Visible: true } ? PadScreen.TitlePage : PadScreen.Title) :
            _storyOpen ? PadScreen.Story : _helperPopup.Visible ? PadScreen.Popup : _menu.Visible ? (_menuCompact ? PadScreen.PauseMenu : PadScreen.MenuPage) : PadScreen.Game;
        return new(screen, PageOpen, _phoneOpen, owner is Slider || owner?.GetParent() is SpinBox, owner is LineEdit, _onSteamDeck, OfficeEditing);
    }

    // A controller cannot hover, so the focused control's tooltip (or its container's) shows under the prompts.
    private static string PadTip(Control control)
    {
        Node? node = control;
        for (var depth = 0; depth < 3 && node is Control c; depth++, node = c.GetParent())
            if (c.TooltipText.Length > 0) return c.TooltipText.ReplaceLineEndings(" ");
        return "";
    }
}
