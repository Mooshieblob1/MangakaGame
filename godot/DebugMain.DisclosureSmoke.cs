using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using MangakaSim;

namespace MangakaGame;

public partial class DebugMain
{
    // Progressive disclosure (spec 2026-10-02).
    private async void RunDisclosureSmoke()
    {
        SetProcess(false);
        try
        {
            GetWindow().Size = new(1920, 1080); Directory.CreateDirectory(SmokeOutput);
            _careers = new CareerStore(Path.Combine(SmokeOutput, "disclosure-" + Guid.NewGuid().ToString("N")));
            NewCareerMenu(); Press("Begin career"); await SettleUi(); _helperPopup.Hide();
            await CheckDayOne();
            await CheckInPageAndChoices();
            GD.Print($"DISCLOSURE SMOKE PASSED: {_smokeChecks} checks.");
            var tree = GetTree(); tree.CreateTimer(.1).Timeout += () => QuitTree(tree); QueueFree();
        }
        catch (Exception ex) { GD.PushError($"DISCLOSURE SMOKE FAILED: {ex.Message}\n{ex.StackTrace}"); GetTree().Quit(1); }
    }

    private async Task CheckDayOne()
    {
        string[] dayOne = ["Office", "Goals", "Inbox", "Series", "Finances", "Help"], later = ["Books", "Staff", "Studios", "Industry"];
        Check(dayOne.All(p => _navigation[p].Visible) && later.All(p => !_navigation[p].Visible) && !_speedButtons[32].Visible,
            "A new career shows only the day-one rail and no 32x");
        var speed = _speed; ChooseSpeed(QuietSpeed); Check(_speed == speed, "The 32x shortcut does nothing before the first sale");
        _state.Apply(new CreateDoujinCommand("First pages", "adventure"));
        for (var d = 0; d < 180 && _state.Series[0].Volumes.Count == 0; d++) _state.Advance(24);
        RefreshManagement(); await SettleUi();
        Check(_navigation["Books"].Visible && _navigation["Books"].Text.EndsWith("· New")
            && _presentation.Guidance.Thread.Any(m => m.Texts.Contains(DisclosureCatalog.Get("books").Announcement!)),
            $"Books appears with a New tag and Helper-Chan's line after the first book ({_navigation["Books"].Text})");
        Press("Books"); await SettleUi(); RefreshManagement();
        Check(!_navigation["Books"].Text.EndsWith("· New"), "Visiting Books clears its New tag");
        // Final review fixes: visible buttons never open a part early, and choosing the contest route opens Contests.
        OpenWorkspace("Business actions"); await SettleUi(); RefreshManagement(); await SettleUi();
        Check(!_state.PartOpened("money") && !FindChildren("*", "Button", true, false).OfType<Button>().Any(b => b.IsVisibleInTree() && b.Text == "Personal loan"),
            "Business actions keeps loans and incorporation hidden until they open");
        Navigate("Person", _state.ProtagonistPersonId); await SettleUi();
        Check(!_state.PartOpened("staff"), "Opening your own character does not open Staff");
        Navigate("Print doujin", _state.Series[0].Id); await SettleUi();
        Check(!_sideContent.FindChildren("*", "Button", true, false).OfType<Button>().Any(b => b.Visible && b.Text.StartsWith("Digital & overseas")),
            "The print page hides digital and overseas deals until Industry opens");
        Navigate("Guidance"); await SettleUi(); Press("Enter a contest"); await SettleUi();
        Check(_state.PartOpened("contests"), "Choosing the contest route opens Contests");
        _presentation.Guidance.Route = "career";
        _state.Events.Add(new() { Time = _state.Clock.Now, ActivityDate = _state.Clock.Now.Date, Type = EventType.LicenseOffered, Message = "Staged licence offer" });
        ShowEvent(_state.Events.Count - 1); await SettleUi(); Press("Open relevant controls"); await SettleUi();
        Check(_navigation["Industry"].Visible && _state.PartOpened("industry"), "A pop-up's route opens the hidden part it leads to");
        await CaptureSmokeImage("disclosure-day-one");
    }

    private async Task CheckInPageAndChoices()
    {
        Navigate("Finances"); await SettleUi();
        Check(!_sideContent.FindChildren("*", "Button", true, false).OfType<Button>().Any(b => b.Visible && b.Text.StartsWith("Funding, loans")),
            "Finances hides loans and incorporation until they open");
        Navigate("Series details", _state.Series[0].Id); await SettleUi();
        Check(!_sideContent.FindChildren("*", "Button", true, false).OfType<Button>().Any(b => b.Visible && b.Text.StartsWith("Magazine pitch")),
            "Series details hides the magazine section before an ongoing series");
        // The New Career choice shows every screen with no tags.
        _presentation = new() { Page = "Office" }; OpenTitle(); NewCareerMenu(); await SettleUi();
        var every = (CheckBox)_menuContent.FindChild("ShowEveryScreen", true, false)!; every.ButtonPressed = true;
        Press("Begin career"); await SettleUi(); _helperPopup.Hide(); RefreshManagement(); await SettleUi();
        Check(new[] { "Books", "Staff", "Studios", "Industry" }.All(p => _navigation[p].Visible && !_navigation[p].Text.EndsWith("· New")) && _speedButtons[32].Visible,
            "Experienced player: every screen shows from the start, with no New tags");
        // The Settings switch turns it off again, back to the parts already opened.
        ShowMenu(); Press("Settings"); await SettleUi();
        var toggle = (CheckBox)_menuContent.FindChild("ShowEveryScreen", true, false)!; toggle.ButtonPressed = false; await SettleUi();
        _menu.Hide(); _inMenu = false; RefreshManagement(); await SettleUi();
        Check(!_navigation["Industry"].Visible && !_state.Disclosure!.ShowAll, "Turning it off in Settings returns to the parts already opened");
    }
}
