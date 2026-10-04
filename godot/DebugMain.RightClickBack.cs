using System.Linq;
using Godot;

namespace MangakaGame;

public partial class DebugMain
{
    // Right-click back (approved 2026-10-03): a right click without a drag steps back one level, popup, menu sub-page,
    // menu, then page, until the 3D office, where it does nothing. A right drag still rotates the camera, text boxes
    // keep their copy and paste menu, dialogs keep their own buttons, and Escape is unchanged.
    private const float RightClickSlop = 6;
    private Vector2? _rightPress;

    private void HandleRightClick(InputEventMouseButton click)
    {
        if (click.Pressed) { _rightPress = TextBoxUnderMouse() ? null : click.Position; return; }
        if (_rightPress is not { } start) return;
        _rightPress = null;
        if (start.DistanceTo(click.Position) > RightClickSlop) return; // that was a camera drag
        Callable.From(StepBack).CallDeferred(); // after this release reaches the controls under the mouse
    }

    private bool TextBoxUnderMouse()
    {
        for (Node? node = GetViewport().GuiGetHoveredControl(); node is not null; node = node.GetParent())
            if (node is LineEdit or TextEdit or SpinBox) return true;
        return false;
    }

    private void StepBack()
    {
        if (_startup is not null || Fading) return;
        if (GetChildren().OfType<Window>().Any(w => w.Visible)) return; // a dialog answers with its own buttons
        if (TitleOpen) { if (_titlePage is { Visible: true }) ShowTitleMenu(); return; }
        if (!_managementReady) return;
        if (_storyOpen) { Notify("Choose an answer, Read later or Skip to close this conversation."); return; }
        if (_helperPopup.Visible) { _helperPopup.Hide(); return; }
        if (OfficeEditing) { Notify("Apply or discard your furniture changes first."); return; }
        if (_menu.Visible) { if (_menuCompact) { _menu.Hide(); _inMenu = false; } else ShowPauseMenu(); return; }
        if (_report.Visible || _side.Visible) GoBack();
    }
}
