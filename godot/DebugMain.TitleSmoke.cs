using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using MangakaSim;

namespace MangakaGame;

public partial class DebugMain
{
    // Title screen and pause menu (spec 2026-09-28). Uses temporary career stores, never the player's saves.
    private async void RunTitleSmoke()
    {
        SetProcess(false);
        try
        {
            GetWindow().Size = new(1920, 1080); Directory.CreateDirectory(SmokeOutput);
            await SettleUi();
            await CheckTitleFades();
            await CheckTitleScreen();
            await CheckPauseMenu();
            await CheckLoadOldestAutosave();
            await CheckFadeInput();
            await CheckCloseRequest();
            GD.Print($"TITLE SMOKE PASSED: {_smokeChecks} checks.");
            var tree = GetTree(); tree.CreateTimer(.1).Timeout += () => QuitTree(tree); QueueFree();
        }
        catch (Exception ex) { GD.PushError($"TITLE SMOKE FAILED: {ex.Message}\n{ex.StackTrace}"); GetTree().Quit(1); }
    }

    private async Task WaitSeconds(double seconds) => await ToSignal(GetTree().CreateTimer(seconds), SceneTreeTimer.SignalName.Timeout);

    private async Task CheckTitleFades()
    {
        _instantFades = false; _presentation.ReducedUiMotion = false;
        var ran = 0;
        Check(Transition(() => ran++) && Fading && ran == 0, "A transition fades to black before changing the screen");
        Check(!Transition(() => ran += 10), "A second request during a fade is ignored");
        await WaitSeconds(.8);
        Check(ran == 1 && Fading, "The change happens behind the curtain, then the picture fades back in");
        await WaitSeconds(1.2);
        Check(!Fading, "The fade finishes and the screen takes input again");
        _presentation.ReducedUiMotion = true;
        Transition(() => ran++); await WaitSeconds(.6);
        Check(ran == 2 && !Fading, "Reduced interface motion shortens both fades to about 0.2 seconds");
        _presentation.ReducedUiMotion = false;
        // A first launch: the volume screen fades to black, then the screen beneath fades in.
        _curtain!.Modulate = Colors.White; _curtain.Show();
        BeginStartup(true); await SettleUi(); SkipDisclaimer(); await SettleUi();
        var cont = _volumeSetup!.FindChildren("*", "Button", true, false).OfType<Button>().Single(b => b.Text == "Continue");
        cont.EmitSignal(BaseButton.SignalName.Pressed); cont.EmitSignal(BaseButton.SignalName.Pressed);
        Check(_startup is not null, "The volume screen fades out instead of vanishing");
        await WaitSeconds(.8);
        Check(_startup is null && Fading, "Then the screen beneath fades in from black");
        await WaitSeconds(1.2);
        Check(!Fading, "The fade in from the start-up screens finishes");
        _instantFades = true;
        // Final review: at launch the title is built first, then the disclaimer, and the disclaimer must keep the keyboard.
        _careers = new CareerStore(Path.Combine(SmokeOutput, "title-focus-" + Guid.NewGuid().ToString("N")));
        OpenTitle(); BeginStartup(false); await SettleUi();
        Check(GetViewport().GuiGetFocusOwner() == _startupRoot, "The disclaimer keeps keyboard focus over the title screen");
        SkipDisclaimer(); await SettleUi();
        Check(GetViewport().GuiGetFocusOwner() is Button main && main.ThemeTypeVariation == "PrimaryAction" && TitleOpen, "After the start-up screens, the title's main button takes focus");
    }

    private string[] TitleButtons() => _titleMenuBox!.FindChildren("*", "Button", true, false).OfType<Button>().Select(b => b.Text).ToArray();
    private bool TitleShows(string text) => _title!.FindChildren("*", "Label", true, false).OfType<Label>().Any(l => l.Text == text);

    private async Task CheckTitleScreen()
    {
        Check(TitleOpen && _inMenu && MusicNow().InMenu, "The game opens on the title screen, paused, on the title music");
        var titleRoot = Path.Combine(SmokeOutput, "title-" + Guid.NewGuid().ToString("N"));
        _careers = new CareerStore(titleRoot);
        OpenTitle(); await SettleUi();
        Check(TitleButtons().SequenceEqual(new[] { "New Career", "Load Career", "Settings", "Report a problem", "Quit" })
            && ButtonNamed("New Career").ThemeTypeVariation == "PrimaryAction", "Without saves, Continue is hidden and New Career leads");
        Check(_titleArt?.Texture is not null && _titleLogo?.Texture is not null, "The placeholder art and logo load from Assets/Branding");
        // User request: title buttons styled like the logo (option A slabs), and compact instead of full-width bars.
        StyleBoxFlat? Face(string name) => ButtonNamed(name).GetThemeStylebox("normal") as StyleBoxFlat;
        Check(Face("New Career")?.BgColor.IsEqualApprox(new Color("f0c878")) == true && Face("Load Career")?.BgColor.IsEqualApprox(new Color("a8f8e0")) == true
            && ButtonNamed("Load Career").GetParent() is PanelContainer && ButtonNamed("Load Career").Size.X < _titleColumn!.Size.X * .9f,
            "Title buttons are logo slabs: gold main, mint others, compact, on an extruded base");
        Check(TitleShows("A career told one page at a time.") && TitleShows("Private alpha · " + ProblemReport.Build), "Tagline and build line show");
        var aiNote = _title!.FindChildren("*", "Label", true, false).OfType<Label>().FirstOrDefault(l => l.Text == TitleArtNote);
        Check(aiNote is not null && aiNote.GetGlobalRect().Position.X > GetViewportRect().Size.X / 2 && aiNote.GetGlobalRect().Position.Y > GetViewportRect().Size.Y * .8f && aiNote.Modulate.A < 1,
            "A subtle note in the bottom-right corner says the title image was AI-generated");
        // Rendered review: the light theme's dark ink vanished over the dark art, so title text is always light.
        SetDarkMode(false); OpenTitle(); await SettleUi();
        var readable = _title!.FindChildren("*", "Label", true, false).OfType<Label>().Where(l => l.Text.Length > 0 && !l.GetParent().IsClass("Button")).All(l => l.GetThemeColor("font_color").Luminance > .7f);
        SetDarkMode(true); OpenTitle(); await SettleUi();
        Check(readable, "Title text stays light over the art in the light theme too");
        foreach (var (size, scale) in new[] { (new Vector2I(1920, 1080), 1d), (new Vector2I(2560, 1080), 1d), (new Vector2I(3440, 1440), 1d), (new Vector2I(1680, 1050), 1d), (new Vector2I(1280, 720), 1.5) })
        {
            await Resize(size, scale); OpenTitle(); await SettleUi(); await SettleUi();
            var label = $"{size.X}x{size.Y} at {scale * 100}% text";
            Check(_titleArt!.StretchMode == TextureRect.StretchModeEnum.KeepAspectCovered && _titleArt.Size.IsEqualApprox(GetViewportRect().Size), $"The studio art covers the whole window at {label}");
            Check(_titleColumn!.GetGlobalRect().End.X <= size.X * .56f, $"The logo and menu stay on the dark side at {label}");
            var problems = Inspect();
            Check(problems.Count == 0, $"The title screen fits at {label}: {string.Join("; ", problems)}");
            await CaptureSmokeImage($"title-{size.X}x{size.Y}-{scale * 100:0}");
        }
        await Resize(new(1920, 1080), 1);
        // Sub-pages open in the page panel; title Settings are this computer's only.
        OpenTitle(); Press("Settings"); await SettleUi();
        Check(_titlePage!.Visible && !_titleColumn!.Visible && _menuContent == _titlePageContent, "Settings opens in the page panel over the title art");
        var boxes = _menuContent.FindChildren("*", "CheckBox", true, false).OfType<CheckBox>().Select(c => c.Text).ToArray();
        Check(boxes.Contains("Dark mode") && boxes.Contains("Play sound even while unfocused") && !boxes.Any(t => t.StartsWith("Helper-Chan"))
            && !_menuContent.FindChildren("*", "Button", true, false).OfType<Button>().Any(b => b.Text == "Difficulty & Sandbox"), "Title settings show only this computer's settings");
        await CaptureSmokeImage("title-settings");
        Press("Back"); await SettleUi();
        Check(_titleColumn!.Visible && !_titlePage!.Visible, "Back returns to the menu column");
        Press("Report a problem"); await SettleUi();
        Check(_menuContent.GetChildren().OfType<CheckBox>().Single(c => c.Text.StartsWith("Attach career")).Disabled, "A report from the title screen cannot attach a career");
        Press("Cancel"); await SettleUi();
        Input.ParseInputEvent(new InputEventKey { Keycode = Key.Escape, Pressed = true }); await SettleUi();
        Check(TitleOpen && _titleColumn!.Visible && !_menu.Visible, "Escape does nothing on the title screen");
        // Motion and fallbacks.
        Check(_titlePushIn is { } push && push.IsRunning(), "The studio art slowly pushes in");
        // User report: the push-in stepped visibly on large screens because controls snap to whole pixels.
        // The zoom now happens inside the picture (a shader), so the frame itself never moves or scales.
        await WaitSeconds(.5);
        Check(_titleArt!.Scale == Vector2.One && _titleArt.Material is ShaderMaterial zoomMaterial && (float)zoomMaterial.GetShaderParameter("zoom") > 1f,
            "The push-in zooms the picture inside a fixed frame, so it glides without pixel steps");
        _presentation.ReducedUiMotion = true; OpenTitle(); await SettleUi();
        Check(_titlePushIn is null && _titleArt!.Scale == Vector2.One && _titleArt.Material is ShaderMaterial still && (float)still.GetShaderParameter("zoom") == 1f, "Reduced interface motion keeps the art still");
        _presentation.ReducedUiMotion = false;
        _brandingFolder = "res://Assets/NoSuchFolder"; OpenTitle(); await SettleUi();
        Check(_titleArt is null && _titleLogo is null && TitleShows("MANGAKA DAYS") && !TitleShows(TitleArtNote), "Missing art falls back to the plain backdrop and a text logo, without the AI note");
        _brandingFolder = "res://Assets/Branding";
        // Into a career and back: a double press starts only one career.
        OpenTitle(); NewCareerMenu(); _instantFades = false;
        Press("Begin career"); Press("Begin career");
        await WaitSeconds(2.0);
        Check(!TitleOpen && !Fading && !_inMenu && !MusicNow().InMenu && _careers.List().Select(s => s.Career).Distinct().Count() == 1,
            "Begin career fades into the office once, even when pressed twice, and starts the career music");
        _instantFades = true; _helperPopup.Hide();
        OpenTitle(); await SettleUi();
        var latest = _careers.List()[0];
        Check(TitleButtons()[0] == "Continue" && ButtonNamed("Continue").ThemeTypeVariation == "PrimaryAction" && TitleShows(TitleScreen.ContinueLabel(latest)),
            "With a save, Continue leads and names the latest career");
        await CaptureSmokeImage("title-continue");
        // Final review: with a save (Continue plus its caption) the column must still fit at 1280 x 720 with 150% text.
        await Resize(new(1280, 720), 1.5); OpenTitle(); await SettleUi(); await SettleUi();
        var view = GetViewportRect();
        Check(_titleMenuBox!.FindChildren("*", "Button", true, false).OfType<Button>().All(b => view.Encloses(b.GetGlobalRect())) && _titleColumn!.FollowFocus
            && _titleColumn.ScrollVertical == 0 && view.Encloses(_titleLogo!.GetGlobalRect()),
            $"With a save, the logo and every title button fit at 1280x720 with 150% text, without scrolling, and the menu follows keyboard focus (column {_titleColumn.Size.Y:0}, content {((Control)_titleColumn.GetChild(0)).GetCombinedMinimumSize().Y:0}, logo {_titleLogo!.GetGlobalRect()}, scroll {_titleColumn.ScrollVertical})");
        await CaptureSmokeImage("title-continue-1280x720-150");
        await Resize(new(1920, 1080), 1); OpenTitle(); await SettleUi();
        var loaded = _careerId; _careerId = Guid.NewGuid().ToString("N");
        Press("Continue"); await SettleUi();
        Check(!TitleOpen && _careerId == loaded, "Continue loads the latest career");
        // Final review: a save that fails from the title's Load list must say so on that page, not in a hidden label.
        var titleSave = _careers.List()[0];
        OpenTitle(); Press("Load Career"); await SettleUi();
        File.Delete(Path.Combine(titleRoot, titleSave.Career, titleSave.Snapshot + ".career"));
        _menuContent.GetChildren().OfType<Button>().First(b => b.Text.Contains('·')).EmitSignal(BaseButton.SignalName.Pressed); await SettleUi();
        Check(TitleOpen && _titlePageNotice is { } pageNote && pageNote.IsVisibleInTree() && pageNote.Text.Length > 0, "A save that cannot be loaded from the title's Load list says so on that page");
        // A load that fails behind the curtain (here, nothing left to load): the player stays on the title screen
        // with the reason, not on a black screen.
        _careers = new CareerStore(Path.Combine(SmokeOutput, "title-empty-" + Guid.NewGuid().ToString("N"))); OpenTitle();
        ContinueLatest(); await SettleUi();
        Check(TitleOpen && !Fading && _titleNotice!.Text.StartsWith("No saved careers yet"), "A career that cannot be loaded leaves the player on the title screen with the reason");
        _helperPopup.Hide();
    }

    private async Task CheckPauseMenu()
    {
        // In a career with safety saves switched on, as for a player.
        _careers = new CareerStore(Path.Combine(SmokeOutput, "title-pause-" + Guid.NewGuid().ToString("N")));
        OpenTitle(); NewCareerMenu(); Press("Begin career"); await SettleUi(); _helperPopup.Hide();
        _safetySaves = true;
        int Saves() => _careers.List().Count;
        Input.ParseInputEvent(new InputEventKey { Keycode = Key.Escape, Pressed = true }); await SettleUi();
        var items = _menuContent.FindChildren("*", "Button", true, false).OfType<Button>().Select(b => b.Text).ToArray();
        Check(_menu.Visible && items.SequenceEqual(new[] { "Resume", "Save", "Load Career", "Settings", "Report a problem", "Quit to title" }), "Escape opens the pause menu with its six items");
        Check(_inMenu && !MusicNow().InMenu && _menu.Size.X < 700, "The pause menu is a small panel that pauses the game and keeps its music");
        await CaptureSmokeImage("pause-menu");
        Input.ParseInputEvent(new InputEventKey { Keycode = Key.Escape, Pressed = true }); await SettleUi();
        Check(!_menu.Visible && !_inMenu, "Escape again resumes");
        ShowMenu(); Press("Settings"); await SettleUi();
        Check(_menuContent.FindChildren("*", "Button", true, false).OfType<Button>().Any(b => b.Text == "Difficulty & Sandbox") && _menu.Size.X > 700, "Settings from the pause menu use the full panel and include this career's options");
        Press("Back"); await SettleUi();
        var saves = Saves();
        Press("Quit to title"); await SettleUi();
        Check(TitleOpen && Saves() == saves + 1 && _careers.List()[0].Auto && _titleNotice!.Text == "Progress kept.", "Quit to title keeps a safety save, then shows the title screen");
        Press("Continue"); await SettleUi();
        saves = Saves(); ShowMenu(); Press("Load Career"); await SettleUi();
        _menuContent.GetChildren().OfType<Button>().First(b => b.Text.Contains('·')).EmitSignal(BaseButton.SignalName.Pressed); await SettleUi();
        Check(Saves() == saves + 1 && !_menu.Visible && !TitleOpen, "Loading a career from the pause menu keeps a safety save first");
        // A save that fails keeps the player in the career (an artwork reference with no file makes saving fail).
        _presentation.Artwork["broken"] = new string('a', 64);
        ShowMenu(); Press("Quit to title"); await SettleUi();
        Check(!TitleOpen && _menu.Visible && _notice.Text.StartsWith("Your progress could not be kept"), "A failed safety save keeps the player in the career with the reason");
        Press("Load Career"); await SettleUi();
        _menuContent.GetChildren().OfType<Button>().First(b => b.Text.Contains('·')).EmitSignal(BaseButton.SignalName.Pressed); await SettleUi();
        Check(!TitleOpen && _menuNotice is { } menuNote && menuNote.IsVisibleInTree() && menuNote.Text.StartsWith("Your progress could not be kept"), "A failed safety save from the pause menu's Load list says so on that page");
        _presentation.Artwork.Remove("broken"); _menu.Hide(); _inMenu = false;
    }

    private string LatestSnapshot() => _careers.List()[0].Snapshot;

    // Final review: keeping the safety save trims autosaves to three, which used to delete the one being loaded.
    private async Task CheckLoadOldestAutosave()
    {
        var career = _careerId;
        for (var i = 0; i < 3; i++) { _state.Advance(24); SaveCareer("Daily autosave", true); }
        var oldest = _careers.List().Where(s => s.Career == career && s.Auto).OrderBy(s => s.SavedAt).First();
        ShowMenu(); Press("Load Career"); await SettleUi();
        var label = $"{oldest.Name}   ·   {oldest.GameDate:d MMM yyyy}   ·   {oldest.Studio}   [auto]";
        _menuContent.GetChildren().OfType<Button>().Single(b => b.Text == label).EmitSignal(BaseButton.SignalName.Pressed); await SettleUi();
        Check(_state.Clock.Now == oldest.GameDate && !_menu.Visible && !TitleOpen, "Loading the oldest autosave from the pause menu loads it, instead of deleting it first");
    }

    // Final review: the curtain blocks the mouse; keys and repeat presses must not act behind it either.
    private async Task CheckFadeInput()
    {
        _instantFades = false;
        Transition(() => { });
        Input.ParseInputEvent(new InputEventKey { Keycode = Key.Space, Pressed = true }); await SettleUi();
        Check(_speed == 0, "Keys during a fade do not reach the game behind the curtain");
        await WaitSeconds(2);
        var before = LatestSnapshot();
        ShowMenu(); await SettleUi(); QuitToTitle();
        Check(GetViewport().GuiGetFocusOwner() is null, "Starting a fade lets go of keyboard focus");
        var first = LatestSnapshot(); QuitToTitle();
        Check(first != before && LatestSnapshot() == first, "A second Quit to title during the fade runs no second safety save");
        await WaitSeconds(2);
        Check(TitleOpen && !Fading, "The fade to the title screen still completes");
        _instantFades = true; Press("Continue"); await SettleUi(); _helperPopup.Hide();
    }

    private async Task CheckCloseRequest()
    {
        // Still in the career from CheckPauseMenu, with safety saves on.
        // Autosaves are trimmed to three per career, so a new save shows as a new newest snapshot, not a higher count.
        Check(!GetTree().AutoAcceptQuit, "The game decides itself when the window may close");
        var before = LatestSnapshot();
        Check(HandleCloseRequest() && LatestSnapshot() != before, "Closing the window keeps a safety save first");
        _presentation.Artwork["broken"] = new string('a', 64);
        Check(!HandleCloseRequest() && _notice.Text.Contains("Close the window again"), "If that save fails, the first close keeps the game open with a message");
        Check(HandleCloseRequest(), "A second close quits without saving");
        // Final review: once the problem is fixed, a later close must save again rather than quit straight away.
        _presentation.Artwork.Remove("broken"); before = LatestSnapshot();
        Check(HandleCloseRequest() && LatestSnapshot() != before, "After a refused close, a later close keeps a safety save again");
        OpenTitle(); await SettleUi(); before = LatestSnapshot();
        Check(HandleCloseRequest() && LatestSnapshot() == before, "On the title screen the window closes at once, with nothing to save");
        _safetySaves = false;
    }
}
