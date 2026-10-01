using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using MangakaSim;

namespace MangakaGame;

public partial class DebugMain
{
    // In-game UI restyle around the logo (spec 2026-09-29). Uses a temporary career store and the smoke UI preferences file.
    private async void RunBrandSmoke()
    {
        SetProcess(false);
        try
        {
            GetWindow().Size = new(1920, 1080); Directory.CreateDirectory(SmokeOutput);
            _careers = new CareerStore(Path.Combine(SmokeOutput, "brand-" + Guid.NewGuid().ToString("N")));
            await SettleUi();
            await CheckBrandPalette();
            await CheckSlabStyles();
            await CheckFonts();
            await CheckHandStyled();
            await CheckReviewFixes();
            GD.Print($"BRAND SMOKE PASSED: {_smokeChecks} checks.");
            var tree = GetTree(); tree.CreateTimer(.1).Timeout += () => tree.Quit(); QueueFree();
        }
        catch (Exception ex) { GD.PushError($"BRAND SMOKE FAILED: {ex.Message}\n{ex.StackTrace}"); GetTree().Quit(1); }
    }

    private async Task CheckBrandPalette()
    {
        foreach (var dark in new[] { true, false })
        {
            SetDarkMode(dark); await SettleUi();
            var brand = BrandPalette.For(dark); var name = dark ? "evening studio" : "manuscript paper";
            Check(_backdrop.Color == new Color(brand.Wash) && Theme.GetColor("font_color", "Label") == new Color(brand.Text)
                && Theme.GetStylebox("panel", "StudioCard") is StyleBoxFlat card && card.BgColor == new Color(brand.Card) && card.BorderColor == new Color(brand.Outline),
                $"The {name} theme uses the logo palette");
            Check(GainColour == new Color(brand.Gain) && LossColour == new Color(brand.Loss), $"Money gained and lost use the {name} colours");
            Check(Theme.GetStylebox("fill", "ProgressBar") is StyleBoxFlat fill && fill.BgColor == new Color(brand.Progress),
                $"Progress bars fill with the {name} progress colour");
        }
        SetDarkMode(true); await SettleUi();
    }

    private async Task CheckSlabStyles()
    {
        foreach (var dark in new[] { true, false })
        {
            SetDarkMode(dark); await SettleUi();
            var brand = BrandPalette.For(dark); var name = dark ? "evening studio" : "manuscript paper";
            SlabStyleBox? S(string state, string kind) => Theme.GetStylebox(state, kind) as SlabStyleBox;
            Check(S("normal", "Button")?.Face == new Color(brand.QuietFace) && S("normal", "PrimaryAction")?.Face == new Color(BrandPalette.Gold)
                && S("pressed", "NavigationButton")?.Face == new Color(BrandPalette.Gold) && S("pressed", "Button")?.Face == new Color(BrandPalette.Mint)
                && S("tab_selected", "TabContainer")?.Face == new Color(BrandPalette.Mint) && S("normal", "HeaderActiveButton")?.Face == new Color(BrandPalette.Gold)
                && S("normal", "HeaderButton")?.Face == new Color(brand.QuietFace),
                $"Buttons, the selected rail item and tabs are logo slabs in the {name} theme");
            Check(S("pressed", "Button")!.Depth < S("normal", "Button")!.Depth && Theme.GetColor("font_color", "PrimaryAction") == new Color(BrandPalette.Ink)
                && Theme.GetColor("font_pressed_color", "Button") == new Color(BrandPalette.Ink),
                $"Pressed slabs sink onto their base, and gold and mint slabs use the logo's ink ({name})");
        }
        SetDarkMode(true); await SettleUi();
    }

    private async Task CheckFonts()
    {
        Check(Theme.GetFont("font", "Button") is FontFile lilita && lilita.ResourcePath.EndsWith("LilitaOne-Regular.ttf")
            && Theme.GetFont("font", "OptionButton") != Theme.GetFont("font", "Button") && Theme.GetFont("font", "CheckBox") != Theme.GetFont("font", "Button"),
            "Buttons use Lilita One; drop-downs and check boxes keep the reading font");
        var heading = Words(this, "Heading check", 24); var body = Words(this, "Body check", 16); await SettleUi();
        Check(heading.GetThemeFont("font") == HeadingFont && body.GetThemeFont("font") != HeadingFont, "Headings use Lilita One and body text keeps the reading font");
        RemoveChild(heading); heading.QueueFree(); RemoveChild(body); body.QueueFree();
        var railTitle = _rail.FindChildren("*", "Label", true, false).OfType<Label>().First(l => l.Text == "MANGAKA\nDAYS");
        Check(railTitle.GetThemeFont("font") == HeadingFont && _dashboardTitle.GetThemeFont("font") == HeadingFont,
            "The rail title and the office's series title (21 px headings) use Lilita One");
        var tiles = new VBoxContainer(); AddChild(tiles); Metric(tiles, "CHECK", () => "¥1,000");
        var figure = tiles.FindChildren("*", "Label", true, false).OfType<Label>().First(l => l.Text == "¥1,000");
        Check(figure.GetThemeFont("font") != HeadingFont && _dashboardFans.GetThemeFont("font") != HeadingFont,
            "Large money and count figures keep the reading font");
        RemoveChild(tiles); tiles.QueueFree();
        _fontPath = "res://Assets/Fonts/Missing.ttf"; ApplyUiTheme(); await SettleUi();
        Check(HeadingFont is null && Theme.GetFont("font", "Button") == ThemeDB.FallbackFont, "A missing heading font falls back to the reading font");
        _fontPath = "res://Assets/Fonts/LilitaOne-Regular.ttf"; ApplyUiTheme(); await SettleUi();
    }

    private async Task CheckHandStyled()
    {
        OpenTitle(); NewCareerMenu(); Press("Begin career"); await SettleUi(); _helperPopup.Hide();
        ShowMenu(); await SettleUi();
        var resume = ButtonNamed("Resume");
        Check(resume.ThemeTypeVariation == "PrimaryAction" && resume.GetParent()?.GetParent()?.GetParent() is PanelContainer edge
            && edge.GetThemeStylebox("panel") is StyleBoxFlat white && white.BgColor == Colors.White,
            "The pause menu uses the title screen's sticker slabs");
        _menu.Hide(); _inMenu = false;
        BeginStartup(true); await SettleUi(); SkipDisclaimer(); await SettleUi();
        Check(_volumeSetup!.FindChildren("*", "Button", true, false).OfType<Button>().Single(b => b.Text == "Continue").ThemeTypeVariation == "PrimaryAction",
            "The volume screen's Continue is a gold main action");
        EndStartup(); await SettleUi();
        Check(PhoneFrame.ModernScreen(true) == new Color(BrandPalette.Evening.Card) && PhoneFrame.ModernScreen(false) == new Color(BrandPalette.Paper.Card)
            && PhoneFrame.ModernText(false) == new Color(BrandPalette.Paper.Text), "Helper-Chan's smartphone screen uses the palette");
        Check(ChapterProgressBar.Stages.Select(s => s.Color).Distinct().Count() == 5 && ChapterProgressBar.Stages.Any(s => s.Color == new Color(BrandPalette.Gold))
            && ChapterProgressBar.Stages.Any(s => s.Color == new Color(BrandPalette.Mint)), "The chapter bar keeps five distinct stage colours from the palette");
    }

    // Final review fixes (2026-09-29), each written to fail first.
    private async Task CheckReviewFixes()
    {
        foreach (var dark in new[] { true, false })
        {
            SetDarkMode(dark); await SettleUi();
            var box = new CheckBox { Text = "Ticked check", ButtonPressed = true }; AddChild(box); await SettleUi();
            Check(BrandPalette.Contrast(box.GetThemeColor("font_pressed_color").ToHtml(false), BrandPalette.Mint) >= 4.5
                && BrandPalette.Contrast(box.GetThemeColor("font_hover_pressed_color").ToHtml(false), BrandPalette.MintLit) >= 4.5,
                $"A ticked check box's label reads on its mint face ({(dark ? "evening" : "paper")})");
            RemoveChild(box); box.QueueFree();
            var active = new Button { Text = "8×", ThemeTypeVariation = "HeaderActiveButton" }; var plain = new Button { Text = "1×", ThemeTypeVariation = "HeaderButton" };
            AddChild(active); AddChild(plain); await SettleUi();
            Check(active.GetThemeStylebox("hover_pressed") is SlabStyleBox litGold && litGold.Face == new Color(BrandPalette.GoldLit)
                && litGold.ContentMarginLeft == active.GetThemeStylebox("normal").ContentMarginLeft
                && plain.GetThemeStylebox("hover_pressed").ContentMarginLeft == plain.GetThemeStylebox("normal").ContentMarginLeft,
                $"Hovering a pressed header button keeps its colour and size ({(dark ? "evening" : "paper")})");
            RemoveChild(active); active.QueueFree(); RemoveChild(plain); plain.QueueFree();
        }
        SetDarkMode(true); await SettleUi();
        // At launch the theme is built before the timeline starts, as in BuildManagementShell.
        var earlier = _timeline; _timeline = null; _fontPath = "res://Assets/Fonts/Missing.ttf"; ApplyUiTheme(); StartTimeline(); earlier?.End();
        Check(string.Join("", _timeline!.Files().Select(f => f.Text)).Contains("heading font missing"), "A missing heading font is logged at launch");
        _fontPath = "res://Assets/Fonts/LilitaOne-Regular.ttf"; ApplyUiTheme(); await SettleUi();
        Check(HeadingFont!.Fallbacks.Contains(ThemeDB.FallbackFont), "Symbols Lilita One lacks (pause, arrows) fall back to the reading font");
        foreach (var dark in new[] { true, false })
            Check(ReportPlot.Background(dark) == new Color(BrandPalette.For(dark).Wash) && ReportPlot.Line(dark) == new Color(BrandPalette.For(dark).Accent)
                && StudioPlanPreview.Floor(dark) == new Color(BrandPalette.For(dark).Wash), $"Charts and floor plans use the palette ({(dark ? "evening" : "paper")})");
        Check(_overnightSpeedBadge!.ThemeTypeVariation == "SectionLabel" && !_overnightSpeedBadge!.HasThemeColorOverride("font_color"), "The overnight speed badge follows the theme's accent colour");
        _navigation["Studios"].EmitSignal(BaseButton.SignalName.Pressed); await SettleUi();
        var rents = FindChildren("*", "Label", true, false).OfType<Label>().Where(l => l.Text.EndsWith("/ month") && l.IsVisibleInTree()).ToArray();
        Check(rents.Length > 0 && rents.All(l => l.GetThemeFont("font") != HeadingFont), $"Rent figures keep the reading font ({rents.Length} on the Studios page)");
    }
}
