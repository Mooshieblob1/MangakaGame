using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using MangakaSim;

namespace MangakaGame;

public partial class DebugMain
{
    // Controller support (2026-10-06): every core screen can be fully reached with the D-pad from where the controller
    // lands, focus never leaves an open menu or popup, the buttons do what the prompts say, and the screens still fit
    // at Steam Deck (1280 x 800) and 1280 x 720 with the prompts showing. Includes the A6 licence card at small sizes.
    private async void RunGamepadSmoke()
    {
        SetProcess(false); _homeOffice.SetProcess(false); _officeView.SetProcess(false);
        try
        {
            Directory.CreateDirectory(SmokeOutput);
            _careers = new CareerStore(Path.Combine(SmokeOutput, "gamepad-" + Guid.NewGuid().ToString("N")));
            await Resize(new(1280, 800), 1);
            NewCareerMenu(); Press("Begin career"); await SettleUi(); _helperPopup.Hide();
            _presentation = new() { Page = "Office" };
            _state = SweepCareer(out var serial, out var doujin, out var fixture);
            var smokeState = _state;
            GD.Print("GAMEPAD fixture: " + fixture);
            ResetManagementSession(); ShowOffice(); _helperPopup.Hide(); _popupEvents.Clear();
            CareerGuidance.Observe(_state, _presentation.Guidance); CareerGuidance.MarkRead(_presentation.Guidance); RefreshGuidance();
            SetInputKind(InputKind.Pad); await PadFrame();

            void Reset()
            {
                if (_state != smokeState) { _state = smokeState; ResetManagementSession(); }
                CloseTitle(); _menu.Hide(); _inMenu = false; ClosePhone(); _recapDialog.Hide(); _helperPopup.Hide(); _storyOpen = false; _speedFlash?.Hide();
                if (OfficeEditing) { _officeDraft = null; LockOfficeControls(false); }
                ShowOffice(); GetViewport().GuiReleaseFocus();
            }
            var screens = new List<(string Name, Action Open)>
            {
                ("title", OpenTitle),
                ("title-settings", () => { OpenTitle(); SettingsMenu(); }),
                ("new-career", () => { OpenTitle(); NewCareerMenu(); }),
                ("office", () => { }),
                ("goals", () => Navigate("Goals")),
                ("pause-menu", ShowMenu),
                ("settings", () => { ShowMenu(); SettingsMenu(); }),
                ("save", OpenSaveMenu),
                ("load", () => { ShowMenu(); LoadCareerMenu(); }),
                ("production", () => OpenWorkspace("Production")),
                ("publishing", () => OpenWorkspace("Publishing")),
                ("series", () => Navigate("Series")),
                ("series-details", () => Navigate("Series details", serial.Id)),
                ("new-series", () => Navigate("New series")),
                ("books", () => Navigate("Books")),
                ("printing", () => OpenPrinting(doujin.Id)),
                ("conventions", () => Navigate("Conventions")),
                ("sell-online", () => OpenOnline(doujin.Id, doujin.Volumes[0].Id)),
                ("finances", () => Navigate("Finances")),
                ("staff", () => Navigate("Staff")),
                ("recruitment", () => OpenWorkspace("Recruitment")),
                ("inbox", () => Navigate("Inbox")),
                ("studios", () => Navigate("Studios")),
                ("industry", () => Navigate("Industry")),
                ("help", () => Navigate("Help")),
                ("furniture", () => { OpenWorkspace("Furniture"); BeginOfficeEditor(); }),
                ("event-popup", () => ShowEvent(PadSmokeEvent())),
            };

            // 1. Reachability: from where the controller lands, the four directions reach every control in the area.
            var unreachable = new List<string>();
            foreach (var (name, open) in screens)
            {
                Reset(); await SettleUi(); open(); await SettleUi(); await PadFrame();
                var root = PadRoot()!;
                var start = GetViewport().GuiGetFocusOwner();
                Check(PadFocusValid(start, root), $"{name}: the controller lands inside the open area ({(start is null ? "nothing" : DisplaySweep.Describe(start))})");
                Check(start is not OfficeView, $"{name}: the controller never lands on the office picture");
                var reached = new HashSet<Control> { start! };
                var queue = new Queue<Control>(reached);
                while (queue.Count > 0)
                {
                    var at = queue.Dequeue();
                    foreach (var direction in new[] { PadDirection.Up, PadDirection.Down, PadDirection.Left, PadDirection.Right })
                        if (PadNeighbour(at, direction, root) is { } next && reached.Add(next)) queue.Enqueue(next);
                }
                var all = Descendants(root).Where(Focusable).ToList();
                var missed = all.Where(c => !reached.Contains(c)).ToList();
                GD.Print($"GAMEPAD {name}: {reached.Count} of {all.Count} controls reachable from {DisplaySweep.Describe(start!)}");
                foreach (var c in missed) { var line = $"{name}: unreachable {DisplaySweep.Describe(c)}"; unreachable.Add(line); GD.Print("GAMEPAD FLAG " + line); }
                Check(reached.All(c => root == c || root.IsAncestorOf(c)), $"{name}: focus stays inside the open area");
                if (name == "furniture") Check(reached.Any(c => c is Button { Text: "Move →" }), "furniture: the editor is on screen and its move buttons are reachable");
            }
            Check(unreachable.Count == 0, $"Every control on every core screen is reachable by D-pad ({unreachable.Count} not)");

            // 2. The buttons.
            Reset(); await SettleUi(); await PadFrame();
            Check(GetViewport().GuiGetFocusOwner() == _navigation["Office"], "In the office the controller lands on the rail");
            var before = _page;
            await Pad(JoyButton.RightShoulder);
            Check(_page != before && _side.Visible && PadFocusValid(GetViewport().GuiGetFocusOwner(), _side), $"RB opens the next rail section with focus on its page ({_page})");
            await Pad(JoyButton.LeftShoulder);
            Check(_page == "Office" && !PageOpen, "LB goes back to the office");
            await Pad(JoyButton.RightShoulder); await Pad(JoyButton.B);
            Check(!PageOpen, "B steps back from a page to the office");

            await Pad(JoyButton.Start);
            Check(_menu.Visible && _inMenu && GetViewport().GuiGetFocusOwner() is Button { Text: "Resume" }, "Start opens the pause menu on Resume");
            for (var i = 0; i < 12; i++) await Pad(JoyButton.DpadDown);
            Check(_menu.IsAncestorOf(GetViewport().GuiGetFocusOwner()), "Holding down never leaves the pause menu");
            for (var i = 0; i < 12; i++) await Pad(JoyButton.DpadUp);
            Check(GetViewport().GuiGetFocusOwner() is Button { Text: "Resume" }, "Up returns to Resume");
            await Pad(JoyButton.A);
            Check(!_menu.Visible && !_inMenu, "A presses the focused button (Resume)");
            await Pad(JoyButton.Start); await Pad(JoyButton.B);
            Check(!_menu.Visible, "B resumes from the pause menu");

            var speed = _speed;
            await Pad(JoyButton.Y);
            Check(_speed != speed, "Y pauses or resumes");
            Pause(); await PadTrigger(JoyAxis.TriggerRight);
            Check(_speed == 1, $"RT goes faster (now {_speed}x)");
            await PadTrigger(JoyAxis.TriggerRight);
            Check(_speed == 2, "RT again, faster still");
            await PadTrigger(JoyAxis.TriggerLeft);
            Check(_speed == 1, "LT goes slower");
            Pause();

            await Pad(JoyButton.Back);
            Check(_page == "Inbox" && _side.Visible, "View opens the inbox");
            Reset(); await SettleUi(); await PadFrame();
            await Pad(JoyButton.X);
            Check(_phoneOpen && _phone.IsAncestorOf(GetViewport().GuiGetFocusOwner()), "X opens Helper-Chan's phone with focus on a reply");
            await Pad(JoyButton.B);
            Check(!_phoneOpen && GetViewport().GuiGetFocusOwner() == _navigation["Office"], "B puts the phone away and returns focus");

            // The left stick moves too, once per push.
            var stickFrom = GetViewport().GuiGetFocusOwner();
            await PadStickPush(0, .9f);
            Check(GetViewport().GuiGetFocusOwner() != stickFrom, "The left stick moves the focus");

            // Sliders take left and right; up and down move on.
            Reset(); ShowMenu(); SettingsMenu(); await SettleUi(); await PadFrame();
            var slider = _menuContent.FindChildren("*", "HSlider", true, false).OfType<HSlider>().First(s => s.IsVisibleInTree());
            FocusPad(slider); await PadFrame();
            var level = slider.Value;
            await Pad(slider.Value >= slider.MaxValue ? JoyButton.DpadLeft : JoyButton.DpadRight);
            Check(slider.Value != level && GetViewport().GuiGetFocusOwner() == slider, "Left and right adjust a slider in place");
            slider.Value = level;
            await Pad(JoyButton.DpadDown);
            Check(GetViewport().GuiGetFocusOwner() != slider, "Down moves on from a slider");

            // A popup keeps the controller inside it; closing it puts focus back.
            Reset(); await SettleUi(); await PadFrame();
            ShowEvent(PadSmokeEvent()); await PadFrame();
            Check(_helperPopup.IsAncestorOf(GetViewport().GuiGetFocusOwner()), "A Helper-Chan popup takes the focus");
            await Pad(JoyButton.B); await PadFrame();
            Check(!_helperPopup.Visible && PadFocusValid(GetViewport().GuiGetFocusOwner(), _floatingUi), "B closes the popup and focus returns to the game");

            // A dialog opens with a button focused; a confirmation starts on Cancel.
            Reset(); await SettleUi();
            ShowRecap(_state.Events.Last(e => e.Type == EventType.DailyRecap)); await SettleUi(); await SettleUi();
            Check(_recapDialog.GetOkButton().HasFocus(), "The day recap opens with Continue focused");
            _recapDialog.Hide();

            // Furniture moves without the mouse.
            Reset(); OpenWorkspace("Furniture"); BeginOfficeEditor(); await SettleUi();
            var placed = _officeDraft!.Placements.First(p => !p.Locked && p.DeskId is null);
            _officeItem = placed.ItemId; _officeView.SelectedFurniture = placed.ItemId;
            Press("Move →"); await SettleUi();
            var moved = _officeDraft.Placements.First(p => p.ItemId == placed.ItemId);
            Check(Math.Abs(moved.X - placed.X) + Math.Abs(moved.Z - placed.Z) == 1, "Move buttons shift furniture one grid square");
            _officeDraft = null; LockOfficeControls(false);

            // 3. Layout with the prompts showing, at Steam Deck size and 1280 x 720, plus captures with the ring.
            var flagged = new List<string>();
            foreach (var (size, label) in new[] { (new Vector2I(1280, 800), "1280x800"), (new Vector2I(1280, 720), "1280x720") })
            {
                await Resize(size, 1);
                foreach (var (name, open) in screens)
                {
                    Reset(); await SettleUi(); open(); await SettleUi(); await PadFrame(); await PadFrame();
                    foreach (var p in Inspect()) { var line = $"{label} {name}: {p}"; flagged.Add(line); GD.Print("GAMEPAD LAYOUT FLAG " + line); }
                    // Valve asks for text about 9 px tall at 1280 x 800, so nothing is smaller than 13 px (Q70).
                    if (label == "1280x800")
                        foreach (var small in FindChildren("*", "Label", true, false).OfType<Label>().Where(l => l.IsVisibleInTree() && l.Text.Length > 0 && l.GetThemeFontSize("font_size") < MinTextSize))
                        {
                            var line = $"{label} {name}: text {small.GetThemeFontSize("font_size")} px {DisplaySweep.Describe(small)}";
                            flagged.Add(line); GD.Print("GAMEPAD TEXT FLAG " + line);
                        }
                    if (label == "1280x800" && name is "office" or "pause-menu" or "series-details" or "books" or "furniture" or "event-popup" or "title" or "settings")
                    {
                        var focused = GetViewport().GuiGetFocusOwner();
                        Check(_focusRing!.Showing, $"{label} {name}: the focus ring shows (focus {(focused is null ? "none" : DisplaySweep.Describe(focused))}, visible {focused?.HasFocus(true)})");
                        await CaptureSmokeImage($"gamepad-{label}-{name}");
                    }
                }
            }
            Reset(); await SettleUi(); await PadFrame();
            await Resize(new(1280, 800), 1); Reset(); await SettleUi(); await PadFrame();
            await Pad(JoyButton.X); _phoneTween?.Kill(); _phoneSlide = 0; ResizeFloatingOffice(); await PadFrame(); await CaptureSmokeImage("gamepad-1280x800-phone"); ClosePhone();
            Check(flagged.Count == 0, $"Core screens fit at 1280 x 800 and 1280 x 720 with the prompts showing ({flagged.Count} flagged)");

            // 4. The A6 licence card at small sizes and a large interface (tester A's A6).
            Reset(); await SettleUi();
            var title = _state.Series.First(s => s.BusinessId == _state.ControlledBusinessId);
            var project = new LicenseProject { Id = _state.AllocateId(), SeriesId = title.Id, BusinessId = title.BusinessId, CreatorId = title.RightsLeadPersonId,
                Kind = LicenseKind.Anime, Partner = "Paper Lantern Animation", CreatedAt = _state.Clock.Now, DueAt = _state.Clock.Now.AddDays(30), Payment = 400000,
                CreatorPercent = 30, Control = 2, Fit = 85, Reliability = .9, Roll = .5, Weeks = 26, SourceChapters = 24 };
            _state.Progression.Projects.Add(project);
            foreach (var (size, scale, label) in new[] { (new Vector2I(1280, 720), 1d, "1280x720-100"), (new Vector2I(1280, 800), 1d, "1280x800-100"), (new Vector2I(1280, 720), 1.5, "1920x1080-150") })
            {
                await Resize(size, scale);
                Reset(); await SettleUi(); Navigate("Licenses"); await SettleUi(); await PadFrame();
                var accept = ButtonNamed("Accept agreement");
                Check(accept.IsVisibleInTree(), $"{label}: the licence offer shows its Accept button");
                FocusPad(accept); await SettleUi(); await PadFrame();
                var problems = Inspect();
                foreach (var p in problems) GD.Print($"GAMEPAD A6 FLAG {label}: {p}");
                Check(problems.Count == 0, $"{label}: the licence card fits ({problems.Count} flagged)");
                Check(VisibleArea(accept) is not null, $"{label}: the controller scrolls Accept into view");
                await CaptureSmokeImage($"gamepad-a6-{label}");
            }
            _state.Progression.Projects.Remove(project);
            await Resize(new(1920, 1080), 1);
            GD.Print($"GAMEPAD SMOKE PASSED: {_smokeChecks} checks."); var tree = GetTree(); tree.CreateTimer(.1).Timeout += () => QuitTree(tree); QueueFree();
        }
        catch (Exception ex)
        {
            GD.PushError($"GAMEPAD SMOKE FAILED: {ex.Message}\n{ex.StackTrace}"); GetTree().Quit(1);
        }
    }

    private int PadSmokeEvent() { var important = _state.Events.FindLastIndex(ImportantEvent); return important >= 0 ? important : _state.Events.Count - 1; }

    // _Process is off during smokes, so each step runs the controller's per-frame work by hand.
    private async Task PadFrame() { await SettleUi(); UpdatePad(1); await SettleUi(); UpdatePad(1); }

    private async Task Pad(JoyButton button)
    {
        GetViewport().PushInput(new InputEventJoypadButton { ButtonIndex = button, Pressed = true });
        GetViewport().PushInput(new InputEventJoypadButton { ButtonIndex = button, Pressed = false });
        await PadFrame();
    }

    private async Task PadTrigger(JoyAxis axis)
    {
        GetViewport().PushInput(new InputEventJoypadMotion { Axis = axis, AxisValue = 1 });
        GetViewport().PushInput(new InputEventJoypadMotion { Axis = axis, AxisValue = 0 });
        await PadFrame();
    }

    private async Task PadStickPush(float x, float y)
    {
        GetViewport().PushInput(new InputEventJoypadMotion { Axis = JoyAxis.LeftX, AxisValue = x });
        GetViewport().PushInput(new InputEventJoypadMotion { Axis = JoyAxis.LeftY, AxisValue = y });
        GetViewport().PushInput(new InputEventJoypadMotion { Axis = JoyAxis.LeftX, AxisValue = 0 });
        GetViewport().PushInput(new InputEventJoypadMotion { Axis = JoyAxis.LeftY, AxisValue = 0 });
        await PadFrame();
    }
}
