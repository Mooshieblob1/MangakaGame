using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using MangakaSim;

namespace MangakaGame;

public partial class DebugMain
{
    // Career goals board (spec 2026-10-01).
    private async void RunGoalsSmoke()
    {
        SetProcess(false);
        try
        {
            GetWindow().Size = new(1920, 1080); Directory.CreateDirectory(SmokeOutput);
            _careers = new CareerStore(Path.Combine(SmokeOutput, "goals-" + Guid.NewGuid().ToString("N")));
            NewCareerMenu(); Press("Begin career"); await SettleUi(); _helperPopup.Hide();
            await CheckGoalsBoard();
            await CheckGoalCelebrations();
            GD.Print($"GOALS SMOKE PASSED: {_smokeChecks} checks.");
            var tree = GetTree(); tree.CreateTimer(.1).Timeout += () => QuitTree(tree); QueueFree();
        }
        catch (Exception ex) { GD.PushError($"GOALS SMOKE FAILED: {ex.Message}\n{ex.StackTrace}"); GetTree().Quit(1); }
    }

    private async Task CheckGoalsBoard()
    {
        Check(_navigation.ContainsKey("Goals") && _navigation["Goals"].Text.EndsWith("0/5"), $"The rail shows Goals with this chapter's count ({(_navigation.TryGetValue("Goals", out var g) ? g.Text : "missing")})");
        Press("Goals"); await SettleUi();
        Check(_page == "Goals" && _sideContent.FindChildren("*", "Label", true, false).OfType<Label>().Any(l => l.Text == "Doujin Days")
            && _sideContent.FindChildren("GoalHow-*", "Button", true, false).Count == 5, "The Goals page shows the chapter and its five goals with How? buttons");
        // A career past its first sale: two goals done.
        _presentation = new() { Page = "Office" };
        var s = GameState.NewGame(0); s.Apply(new CreateDoujinCommand("First pages", "adventure"));
        for (var d = 0; d < 180 && s.Series[0].Volumes.Count == 0; d++) s.Advance(24);
        s.Apply(new StudioActionCommand(StudioAction.Print, s.Series[0].Volumes.Single().Id, Amount: 10, Value: (int)PrintTier.CopyShop));
        for (var d = 0; d < 30 && !s.GoalDone("first-copy"); d++) s.Advance(24);
        _state = s; ResetManagementSession(); ShowOffice(); _helperPopup.Hide(); _popupEvents.Clear(); await SettleUi();
        Check(_navigation["Goals"].Text.EndsWith("2/5") && _dashboardGoalsTitle.Text == "Doujin Days" && _dashboardGoalsList.GetChildCount() > 0,
            $"The badge and the office's This chapter card follow progress ({_navigation["Goals"].Text}, {_dashboardGoalsTitle.Text})");
        Press("Goals"); await SettleUi();
        ((Button)_sideContent.FindChild("GoalHow-convention", true, false)!).EmitSignal(BaseButton.SignalName.Pressed); await SettleUi();
        Check(_page == "Conventions" && _presentation.Guidance.Thread.Last().Texts.SequenceEqual(new[] { GoalCatalog.Get("convention").Tip }),
            $"How? opens the right page with Helper-Chan's tip (page {_page})");
        foreach (var (size, name, scale) in new (Vector2I, string, double)[] { (new(1920, 1080), "1080", 1), (new(1280, 720), "720-150", 1.5) })
        {
            SmokeLayout(size,scale); Press("Goals"); await SettleUi(); await SettleUi();
            await CaptureSmokeImage($"goals-{name}");
        }
        SmokeLayout(new(1920, 1080),1); await SettleUi();
    }

    private async Task CheckGoalCelebrations()
    {
        var prefs = _presentation.Guidance;
        ShowOffice(); SetSpeed(4); ChooseSpeed(QuietSpeed); _popupEvents.Clear(); _helperPopup.Hide(); CareerGuidance.MarkRead(prefs); await SettleUi();
        _state.Goals!.Completed.Add(new("convention", _state.Clock.Now, false));
        _state.Events.Add(new() { Time = _state.Clock.Now, ActivityDate = _state.Clock.Now.Date, Type = EventType.GoalCompleted, Message = "Goal complete: Attend a convention. Reward: ¥10,000." });
        Check(!ScanEvents() && _speed == QuietSpeed && prefs.Thread.Last().Step == CareerGuidance.GoalStep && _notice.Text.Contains("Attend a convention"),
            $"A finished goal texts and notifies without stopping 32x (speed {_speed})");
        _state.Events.Add(new() { Time = _state.Clock.Now, ActivityDate = _state.Clock.Now.Date, Type = EventType.GoalChapterCompleted, Message = "Chapter complete: Doujin Days! Reward: a trophy shelf." });
        Check(ScanEvents() && _speed == 0 && _popupEvents.Count > 0, "A finished chapter stops 32x and queues its celebration");
        ShowEvent(_popupEvents.Dequeue()); await SettleUi(); Press("Open relevant controls"); await SettleUi();
        Check(_page == "Goals", "The chapter celebration opens the Goals page");
        Check(_state.Furniture.Any(f => f.Kind == "print" && f.Paid == 0 && f.Owner == FurnitureOwner.Business), "A decoration reward waits in office storage");
        RefreshOffice(); await SettleUi();
        Check(Enumerable.Range(0, _officeCatalog.ItemCount).All(i => !_officeCatalog.GetItemText(i).StartsWith("Trophy")), "Goal rewards are not for sale in the catalogue");
        var desk = Array.FindIndex(OfficeCatalog.Furniture, f => f.Id == "desk-studio");
        Check(_officeCatalog.IsItemDisabled(desk) && _officeCatalog.GetItemText(desk).Contains("Unlocked by: Rookie chapter"), $"The studio desk names the chapter that unlocks it ({_officeCatalog.GetItemText(desk)})");
        // Final review: the free table is shown where the booking is quoted, and is not charged.
        _state.Goals.FreeConventionTables = 1; Navigate("Conventions", _state.Series[0].Id); await SettleUi();
        var events = _sideContent.FindChildren("*", "OptionButton", true, false).OfType<OptionButton>().First(o => o.ItemCount == 3 && o.GetItemText(0) == "Free neighbourhood event");
        events.Select(1); events.EmitSignal(OptionButton.SignalName.ItemSelected, 1); await SettleUi();
        Check(_sideContent.FindChildren("*", "Label", true, false).OfType<Label>().Any(l => l.Text.Contains("Booth free (Doujin Days reward)")), "The Conventions page shows the free table from Doujin Days");
    }
}
