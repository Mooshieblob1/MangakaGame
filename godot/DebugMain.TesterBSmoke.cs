using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using MangakaSim;

namespace MangakaGame;

public partial class DebugMain
{
    // Fresh-player tester B fixes (2026-10-01): B4 pitch cooldown, B5 Continue after an import, B7 furnishing.
    private async void RunTesterBSmoke()
    {
        SetProcess(false);
        try
        {
            GetWindow().Size = new(1920, 1080); Directory.CreateDirectory(SmokeOutput);
            _careers = new CareerStore(Path.Combine(SmokeOutput, "tester-b-" + Guid.NewGuid().ToString("N")));
            NewCareerMenu(); Press("Begin career"); await SettleUi(); _helperPopup.Hide();
            await CheckPitchCooldown();
            await CheckContinueAfterImport();
            await CheckFurnishingLock();
            GD.Print($"TESTER B SMOKE PASSED: {_smokeChecks} checks.");
            var tree = GetTree(); tree.CreateTimer(.1).Timeout += () => tree.Quit(); QueueFree();
        }
        catch (Exception ex) { GD.PushError($"TESTER B SMOKE FAILED: {ex.Message}\n{ex.StackTrace}"); GetTree().Quit(1); }
    }

    // B4: a magazine on cooldown cannot be pitched, says when it can, and Helper-Chan's route selects an open magazine.
    private async Task CheckPitchCooldown()
    {
        _state.Apply(new CreateSeriesCommand("Cooldown title", "adventure", Cadence.Monthly, 16));
        var series = _state.Series.Last(); SelectSeriesForWorkbench(series.Id);
        var best = CareerGuidance.PitchOutlooks(_state, series).First(o => o.Open).Magazine;
        var until = _state.Clock.Now.AddDays(90); series.PitchCooldowns[best.Id] = until;
        OpenWorkspace("Publishing"); await SettleUi();
        PreselectSuggestedMagazine(series); RefreshPublishing(); await SettleUi();
        var open = CareerGuidance.PitchOutlooks(_state, series).First(o => o.Open).Magazine;
        Check(SelectedMagazineId() == open.Id && open.Id != best.Id && !_pitchButton.Disabled,
            $"Helper-Chan's route selects the open magazine she suggests ({SelectedMagazineId()}, expected {open.Id})");
        _magazineOption.Select(_state.PublisherCatalog.Magazines.ToList().FindIndex(m => m.Id == best.Id)); RefreshPublishing(); await SettleUi();
        Check(_pitchButton.Disabled && _pitchButton.TooltipText.Contains(until.ToString("d MMM yyyy")),
            $"Pitch is disabled for a magazine on cooldown and says when it reopens (\"{_pitchButton.TooltipText}\")");
    }

    // B5: importing a career does not make Continue open it instead of the career just played.
    private async Task CheckContinueAfterImport()
    {
        var played = SaveCareer("Played today");
        var practice = _careers.Save(Guid.NewGuid().ToString("N"), "Practice", GameState.NewGame(3), new CareerPresentation());
        var package = _careers.Export(practice); await SettleUi();
        SaveCareer("Played later"); _careers.Import(package);
        Check(_careers.List().First().Career == played.Career, "Continue still opens the career just played after an import");
    }

    // B7: while furnishing, the rail and speed buttons cannot toggle, and a notice shows once.
    private async Task CheckFurnishingLock()
    {
        OpenWorkspace("Furniture"); BeginOfficeEditor(); await SettleUi();
        Check(_navigation.Values.All(b => b.Disabled) && _speedButtons.Values.All(b => b.Disabled), "While furnishing, the rail and speed buttons are locked");
        Notify("Furnishing notice"); await SettleUi();
        var shown = new[] { _notice, _workbenchNotice }.Count(l => l.IsVisibleInTree() && l.Text == "Furnishing notice");
        Check(shown == 1, $"A notice shows once while a workspace is open ({shown} copies)");
        EndOfficeEditor(); ShowOffice(); await SettleUi();
        Check(_navigation.Values.All(b => !b.Disabled) && _speedButtons.Values.All(b => !b.Disabled) && _navigation["Office"].ButtonPressed
            && _navigation.Count(n => n.Value.ButtonPressed) == 1 && _notice.IsVisibleInTree(), "After furnishing, the rail and speed buttons work again and only the open page is selected");
    }
}
