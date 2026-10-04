using System.Collections.Generic;
using MangakaSim;

namespace MangakaGame;

// Progressive disclosure (spec 2026-10-02): hidden parts, opening on navigation, and "New" tags.
public partial class DebugMain
{
    private static readonly (string Page, string Part)[] RailParts = [("Books", "books"), ("Staff", "staff"), ("Studios", "studios"), ("Industry", "industry")];
    private readonly Dictionary<string, string> _railText = new();
    private Godot.Button _dashboardBooks = null!;
    private readonly List<Godot.Control> _moneyControls = new();

    private bool PartShown(string part) => _state.PartShown(part);
    private bool PageShown(string page) => DisclosureCatalog.PartFor(page) is not { } part || _state.PartShown(part);
    private bool PartIsNew(string part) => _presentation.Guidance.NewParts.Contains(part);

    /// <summary>Nothing points at a hidden part: going to a page opens its part first, then clears its New tag.</summary>
    private void RevealPage(string page)
    {
        if (!_managementReady || DisclosureCatalog.PartFor(page) is not { } part) return;
        if (!_state.PartOpened(part)) _state.Apply(new OpenPartCommand(part));
        CareerGuidance.ObserveParts(_state, _presentation.Guidance);
        _presentation.Guidance.NewParts.Remove(part);
        RefreshDisclosure(); RefreshNavigation(); // the rail shows the opened part at once, even while paused
    }

    private void RefreshDisclosure()
    {
        foreach (var (page, part) in RailParts)
            if (_navigation.TryGetValue(page, out var button))
            {
                _railText.TryAdd(page, button.Text);
                button.Visible = PartShown(part);
                button.Text = _railText[page] + (PartIsNew(part) ? "  · New" : "");
            }
        if (_speedButtons.TryGetValue(32, out var quiet)) quiet.Visible = PartShown("quiet-speed");
        foreach (var control in _moneyControls) control.Visible = PartShown("money");
    }
}
