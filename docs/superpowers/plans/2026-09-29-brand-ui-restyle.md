# Brand UI Restyle Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Carry the logo's palette, slab buttons and lettering into every in-game screen, in an "evening studio" dark theme and a "manuscript paper" light theme, while dense panels stay calm and readable.

**Architecture:** An engine-free `BrandPalette` in `src/MangakaSim` holds both themes' colours as hex strings with a WCAG contrast calculator and xUnit tests. The existing code-built theme (`EditorialTheme`, `PolishTheme`, `PolishHeaderTheme`) reads its colours from that palette and draws buttons with a new `SlabStyleBox` (face, extruded base, brown outline). Lilita One is registered for buttons and headings. A new `--brand-smoke` Godot check covers the theme; the display sweep covers every screen.

**Tech Stack:** C# (.NET 8), Godot 4.7.2 .NET, xUnit.

**Spec:** `docs/superpowers/specs/2026-09-29-brand-ui-restyle-design.md`

## Global Constraints

- Brand pass (Q44): palette everywhere; slabs for things you press and frames; dense panels calm and flat with a soft hairline, never the thick outline.
- Both themes kept (Q45): dark "evening studio", light "manuscript paper"; the theme preference file keeps its format.
- Fonts (Q46): Lilita One for buttons, headings (labels at 22 px and above) and titles; the current font for body text, small capital labels, numbers, drop-downs and check boxes. Download approved: `LilitaOne-Regular.ttf` and its `OFL.txt` from `https://raw.githubusercontent.com/google/fonts/main/ofl/lilitaone/`.
- Logo colours, both themes: gold `#f0c878` (lit `#fbe0a6`) on base `#b88048`; mint `#a8f8e0` (lit `#d2fff2`) on base `#50c0b0`; slab ink `#4b2c2a`.
- Evening studio: wash `#221a1c`, card `#2e2426`, hover `#43363a`, pressed `#5a4644`, outline `#5a4644`, text `#f6ead2`, muted `#c9b79a`, accent `#a8f8e0`, progress `#a8f8e0`, gain `#7fe0c0`, loss `#ff9a8a`, quiet face `#43363a`, quiet base `#241c1e`.
- Manuscript paper: wash `#efe4cf`, card `#fbf5e8`, hover `#f3e8d2`, pressed `#e6d3ae`, outline `#d8c7a8`, text `#4b2c2a`, muted `#6b5343`, accent `#226658`, progress `#50c0b0`, gain `#17644a`, loss `#98372e`, quiet face `#fbf5e8`, quiet base `#d8c7a8`. (Muted, accent, gain and loss are darker than the spec's starting values so they meet 4.5:1 on the page and hover surfaces; recorded as a ruling.)
- Rules: main actions and the selected rail item are gold slabs; ordinary buttons quiet slabs; selected tabs and pressed or ticked buttons mint; gold and mint slabs use ink text in both themes; in-game slabs have no white sticker edge; the pause menu uses the title screen's full sticker slabs.
- Contrast: every text colour at least 4.5:1 on card, wash and hover; text at least 4.5:1 on pressed and quiet face; ink at least 4.5:1 on gold, lit gold, mint and lit mint.
- Missing font: fall back to the reading font and log to the timeline.
- Presentation only: no simulation, balance or save change. Docs CRLF, code LF, metric, no em dashes. Do not commit: the user commits. Packaging is not approved.
- Build approval: this plan's handoff asks the user to confirm builds and Godot checks for this work (spec, Build approval).
- Commands (PowerShell, repository root):
  ```powershell
  $godot = Join-Path $env:LOCALAPPDATA 'Microsoft\WinGet\Packages\GodotEngine.GodotEngine.Mono_Microsoft.Winget.Source_8wekyb3d8bbwe\Godot_v4.7.2-stable_mono_win64\Godot_v4.7.2-stable_mono_win64_console.exe'
  dotnet build MangakaGame.sln -warnaserror
  & $godot --headless --editor --path godot --import --quit
  & $godot --headless --path godot -- --brand-smoke
  ```

## Review Focus

1. Lilita One is wider than the reading font, so button labels may cut off or push layouts at 1280 x 720 with 150% text: the display sweep's text-cut check at all 5 sizes and both themes (Task 6) must stay at 0 flagged.
2. A toggle button that stays pressed (a ticked choice, a selected filter) must still read clearly: it becomes a mint slab with ink text, pinned by the "pressed Button face is mint" and "ink font on pressed" checks (Task 3).
3. Switching theme while a page is open must restyle it without a restart: `ApplyUiTheme` rebuilds the theme, and each check toggles `SetDarkMode` both ways before asserting (Tasks 2, 3).
4. A missing or broken font file must not break the game: pinned by the fallback check (Task 4).
5. Colours outside the theme (money amounts, the phone screen, the chapter bar) must follow the palette in both themes: pinned by Tasks 2 and 5.

---

### Task 1: BrandPalette (engine-free)

**Files:**
- Create: `src/MangakaSim/BrandPalette.cs`
- Test: `tests/MangakaSim.Tests/BrandPaletteTests.cs`

**Interfaces:**
- Produces: `sealed record BrandTheme(string Wash, string Card, string Hover, string Pressed, string Outline, string Text, string Muted, string Accent, string Progress, string Gain, string Loss, string QuietFace, string QuietBase)`; `static class BrandPalette` with consts `Gold`, `GoldLit`, `GoldBase`, `Mint`, `MintLit`, `MintBase`, `Ink` (hex without `#`), `static BrandTheme Evening`, `static BrandTheme Paper`, `static BrandTheme For(bool dark)`, `static double Contrast(string a, string b)`.

- [ ] **Step 1: Write the failing tests**

`tests/MangakaSim.Tests/BrandPaletteTests.cs`:

```csharp
using Xunit;
namespace MangakaSim.Tests;

public class BrandPaletteTests
{
    [Fact] public void Contrast_follows_the_standard_formula()
    {
        Assert.Equal(21, BrandPalette.Contrast("ffffff", "000000"), 2);
        Assert.Equal(1, BrandPalette.Contrast("4b2c2a", "4b2c2a"), 3);
    }

    [Theory, InlineData(true), InlineData(false)]
    public void Every_text_colour_reads_on_every_surface(bool dark)
    {
        var t = BrandPalette.For(dark);
        foreach (var (fg, name) in new[] { (t.Text, "text"), (t.Muted, "muted"), (t.Accent, "accent"), (t.Gain, "gain"), (t.Loss, "loss") })
            foreach (var bg in new[] { t.Card, t.Wash, t.Hover })
                Assert.True(BrandPalette.Contrast(fg, bg) >= 4.5, $"{(dark ? "evening" : "paper")} {name} #{fg} on #{bg}: {BrandPalette.Contrast(fg, bg):0.00}");
        Assert.True(BrandPalette.Contrast(t.Text, t.Pressed) >= 4.5, "text on pressed");
        Assert.True(BrandPalette.Contrast(t.Text, t.QuietFace) >= 4.5, "text on quiet slab face");
    }

    [Fact] public void Slab_ink_reads_on_gold_and_mint_faces()
    {
        foreach (var face in new[] { BrandPalette.Gold, BrandPalette.GoldLit, BrandPalette.Mint, BrandPalette.MintLit })
            Assert.True(BrandPalette.Contrast(BrandPalette.Ink, face) >= 4.5, face);
    }
}
```

- [ ] **Step 2: Run the tests to verify they fail**

Run: `dotnet test tests/MangakaSim.Tests --filter FullyQualifiedName~BrandPaletteTests`
Expected: build FAILS with `CS0103: The name 'BrandPalette' does not exist`.

- [ ] **Step 3: Write the implementation**

`src/MangakaSim/BrandPalette.cs`:

```csharp
namespace MangakaSim;

/// <summary>The logo palette for the in-game UI (spec 2026-09-29): hex strings without '#', engine-free so contrast can be unit tested.</summary>
public sealed record BrandTheme(string Wash, string Card, string Hover, string Pressed, string Outline, string Text, string Muted,
    string Accent, string Progress, string Gain, string Loss, string QuietFace, string QuietBase);

public static class BrandPalette
{
    public const string Gold = "f0c878", GoldLit = "fbe0a6", GoldBase = "b88048", Mint = "a8f8e0", MintLit = "d2fff2", MintBase = "50c0b0", Ink = "4b2c2a";

    public static readonly BrandTheme Evening = new("221a1c", "2e2426", "43363a", "5a4644", "5a4644", "f6ead2", "c9b79a",
        "a8f8e0", "a8f8e0", "7fe0c0", "ff9a8a", "43363a", "241c1e");

    // Muted, accent, gain and loss are a shade darker than the mockup so they meet 4.5:1 on the page and hover surfaces.
    public static readonly BrandTheme Paper = new("efe4cf", "fbf5e8", "f3e8d2", "e6d3ae", "d8c7a8", "4b2c2a", "6b5343",
        "226658", "50c0b0", "17644a", "98372e", "fbf5e8", "d8c7a8");

    public static BrandTheme For(bool dark) => dark ? Evening : Paper;

    // WCAG 2 contrast ratio between two colours.
    public static double Contrast(string a, string b)
    {
        var (x, y) = (Luminance(a), Luminance(b));
        return (Math.Max(x, y) + .05) / (Math.Min(x, y) + .05);
    }

    private static double Luminance(string hex)
    {
        double Channel(int at)
        {
            var c = Convert.ToInt32(hex.Substring(at, 2), 16) / 255.0;
            return c <= .03928 ? c / 12.92 : Math.Pow((c + .055) / 1.055, 2.4);
        }
        return .2126 * Channel(0) + .7152 * Channel(2) + .0722 * Channel(4);
    }
}
```

- [ ] **Step 4: Run the tests to verify they pass**

Run: `dotnet test tests/MangakaSim.Tests --filter FullyQualifiedName~BrandPaletteTests`
Expected: PASS, 4 tests.

---

### Task 2: The palette in the Godot theme

**Files:**
- Create: `godot/DebugMain.BrandSmoke.cs`
- Modify: `godot/DebugMain.cs` (dispatch `--brand-smoke`)
- Modify: `godot/DebugMain.Usability.cs:19-25` (palette colours), `:49-50` (money recolour)
- Modify: `godot/DebugMain.UiPolish.cs:11-12` (muted and outline ink), `:105` (money colour)
- Modify: `godot/DebugMain.MoneyFeedback.cs:127-128` (money colour)
- Modify: `godot/DebugMain.UsabilitySmoke.cs:90` (light wash value)

**Interfaces:**
- Consumes: `BrandPalette`, `BrandTheme` (Task 1).
- Produces: `BrandTheme Brand` (current theme), `Color GainColour`, `Color LossColour`; `async void RunBrandSmoke()` calling `CheckBrandPalette()`.

- [ ] **Step 1: Write the failing smoke check**

`godot/DebugMain.BrandSmoke.cs`:

```csharp
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
        }
        SetDarkMode(true); await SettleUi();
    }
}
```

In `godot/DebugMain.cs`, after the `--title-smoke` dispatch line, add:

```csharp
        if (OS.GetCmdlineUserArgs().Contains("--brand-smoke")) CallDeferred(nameof(RunBrandSmoke));
```

- [ ] **Step 2: Build to verify it fails**

Run: `dotnet build MangakaGame.sln -warnaserror`
Expected: FAIL with `CS0103` for `GainColour` and `LossColour`.

- [ ] **Step 3: Point the theme colours at the palette**

In `godot/DebugMain.Usability.cs`, replace the seven colour properties (`Paper`, `Ink`, `Accent`, `Wash`, `Hover`, `SelectedSurface`, `CardSurface`) with:

```csharp
    // The logo palette (spec 2026-09-29): evening studio when dark, manuscript paper when light.
    private BrandTheme Brand=>BrandPalette.For(_darkMode);
    private Color Paper=>new(Brand.Card);
    private Color Ink=>new(Brand.Text);
    private Color Accent=>new(Brand.Accent);
    private Color Wash=>new(Brand.Wash);
    private Color Hover=>new(Brand.Hover);
    private Color SelectedSurface=>new(Brand.Pressed);
    private Color CardSurface=>new(Brand.Card);
    private Color GainColour=>new(Brand.Gain);
    private Color LossColour=>new(Brand.Loss);
```

and in `ApplyUiTheme`, replace the money recolour line with:

```csharp
            amount.AddThemeColorOverride("font_color",(int)amount.GetMeta("cash_sign")<0?LossColour:GainColour);
```

In `godot/DebugMain.UiPolish.cs`, replace lines 11-12 with:

```csharp
    private Color MutedInk=>new(Brand.Muted);
    private Color OutlineInk=>new(Brand.Outline);
```

and in `CashEntryRow` replace the amount colour line with:

```csharp
        amount.AddThemeColorOverride("font_color",entry.Amount<0?LossColour:GainColour);
```

In `godot/DebugMain.MoneyFeedback.cs`, replace lines 127-128 with:

```csharp
                badge.Gain.AddThemeColorOverride("font_color",GainColour);
                badge.Loss.AddThemeColorOverride("font_color",LossColour);
```

In `godot/DebugMain.UsabilitySmoke.cs`, line 90, replace `new Color("e6e1d5")` with `new Color(BrandPalette.Paper.Wash)` (add `using MangakaSim;` if the file lacks it).

- [ ] **Step 4: Build and run the checks**

Run:
```powershell
dotnet build MangakaGame.sln -warnaserror
& $godot --headless --path godot -- --brand-smoke
& $godot --headless --path godot -- --usability-smoke
```
Expected: 0 warnings; `BRAND SMOKE PASSED: 4 checks.`; usability smoke passes.

---

### Task 3: Slab buttons in the theme

**Files:**
- Create: `godot/SlabStyleBox.cs`
- Modify: `godot/DebugMain.UiPolish.cs` (`PolishTheme`, `PolishHeaderTheme`)
- Modify: `godot/DebugMain.Management.cs:45-63` (button font colours, tabs)
- Modify: `godot/DebugMain.BrandSmoke.cs` (add `CheckSlabStyles`)

**Interfaces:**
- Consumes: `Brand`, `BrandPalette` (Tasks 1, 2).
- Produces: `partial class SlabStyleBox : StyleBox` with fields `Color Face, Base, Outline`, `float Border, Depth, Radius` and `static SlabStyleBox Create(Color face, Color base, Color outline, float depth, float padX, float padY)`; `SlabStyleBox SlabStyle(string face, string base, float depth, float padX, float padY)` in `DebugMain`.

- [ ] **Step 1: Write the failing smoke check**

In `godot/DebugMain.BrandSmoke.cs`, add `await CheckSlabStyles();` after `await CheckBrandPalette();`, and add:

```csharp
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
```

- [ ] **Step 2: Build to verify it fails**

Run: `dotnet build MangakaGame.sln -warnaserror`
Expected: FAIL with `CS0246: The type or namespace name 'SlabStyleBox' could not be found`.

- [ ] **Step 3: Write the slab style**

`godot/SlabStyleBox.cs`:

```csharp
using Godot;

namespace MangakaGame;

/// <summary>A button face in the logo's style (spec 2026-09-29): a coloured face on a darker extruded base inside a
/// brown outline, drawn as three rounded boxes. Content margins keep the label on the face; a pressed slab (less
/// depth) moves its face down onto the base.</summary>
public partial class SlabStyleBox : StyleBox
{
    public Color Face, Base, Outline;
    public float Border = 2, Depth = 4, Radius = 8;
    private readonly StyleBoxFlat _box = new();

    public static SlabStyleBox Create(Color face, Color @base, Color outline, float depth, float padX, float padY)
    {
        var slab = new SlabStyleBox { Face = face, Base = @base, Outline = outline, Depth = depth };
        slab.ContentMarginLeft = slab.ContentMarginRight = padX + slab.Border;
        slab.ContentMarginTop = padY + slab.Border + (4 - depth);
        slab.ContentMarginBottom = padY + slab.Border + depth;
        return slab;
    }

    private void Fill(Rid canvas, Rect2 rect, Color colour, float radius)
    {
        _box.BgColor = colour; _box.SetCornerRadiusAll((int)radius); _box.Draw(canvas, rect);
    }

    public override void _Draw(Rid toCanvasItem, Rect2 rect)
    {
        Fill(toCanvasItem, rect, Outline, Radius + Border);
        var inner = rect.Grow(-Border);
        Fill(toCanvasItem, inner, Base, Radius);
        var sink = 4 - Depth;
        Fill(toCanvasItem, new Rect2(inner.Position.X, inner.Position.Y + sink, inner.Size.X, inner.Size.Y - Depth - sink), Face, Radius);
    }
}
```

- [ ] **Step 4: Use slabs in the theme**

In `godot/DebugMain.UiPolish.cs`, add inside the class:

```csharp
    private SlabStyleBox SlabStyle(string face,string @base,float depth,float padX,float padY)=>
        SlabStyleBox.Create(new Color(face),new Color(@base),new Color(BrandPalette.Ink),depth,padX,padY);
```

In `PolishTheme`, replace the `foreach(var kind in new[]{"Button","OptionButton","LineEdit","TextEdit"})` block with:

```csharp
        // Buttons are logo slabs (spec 2026-09-29): quiet face normally, mint when pressed or ticked; text fields stay flat.
        var slabY=_presentation.CompactUi?5f:8f;
        foreach(var kind in new[]{"Button","OptionButton"})
        {
            theme.SetStylebox("normal",kind,SlabStyle(Brand.QuietFace,Brand.QuietBase,4,14,slabY));
            theme.SetStylebox("hover",kind,SlabStyle(Brand.Pressed,Brand.QuietBase,4,14,slabY));
            theme.SetStylebox("pressed",kind,SlabStyle(BrandPalette.Mint,BrandPalette.MintBase,1,14,slabY));
            theme.SetStylebox("hover_pressed",kind,SlabStyle(BrandPalette.MintLit,BrandPalette.MintBase,1,14,slabY));
            theme.SetStylebox("disabled",kind,SlabStyle(Brand.Wash,Brand.QuietBase,4,14,slabY));
            theme.SetColor("font_hover_color",kind,Ink);
            theme.SetColor("font_pressed_color",kind,new Color(BrandPalette.Ink));theme.SetColor("font_hover_pressed_color",kind,new Color(BrandPalette.Ink));
            theme.SetColor("font_disabled_color",kind,MutedInk);
        }
        foreach(var kind in new[]{"LineEdit","TextEdit"})
        {
            theme.SetStylebox("normal",kind,ControlSurface(Paper));theme.SetStylebox("hover",kind,ControlSurface(Hover));
            theme.SetStylebox("disabled",kind,ControlSurface(Wash));
            theme.SetColor("font_disabled_color",kind,MutedInk);theme.SetColor("font_placeholder_color",kind,MutedInk);
        }
```

Replace the `PrimaryAction` block (the `primary` colour and its `foreach` lines, and the white font colours) with:

```csharp
        // Main actions are gold slabs with the logo's ink.
        foreach(var (state,face,depth) in new[]{("normal",BrandPalette.Gold,4f),("hover",BrandPalette.GoldLit,4f),("pressed",BrandPalette.Gold,1f),("hover_pressed",BrandPalette.GoldLit,1f)})
            theme.SetStylebox(state,"PrimaryAction",SlabStyle(face,BrandPalette.GoldBase,depth,14,slabY));
        foreach(var state in new[]{"font_color","font_hover_color","font_pressed_color","font_hover_pressed_color","font_focus_color"})theme.SetColor(state,"PrimaryAction",new Color(BrandPalette.Ink));
```

Replace the `NavigationButton` pressed line with:

```csharp
        theme.SetStylebox("pressed","NavigationButton",SlabStyle(BrandPalette.Gold,BrandPalette.GoldBase,3,12,6)); // the selected rail item
        theme.SetStylebox("hover_pressed","NavigationButton",SlabStyle(BrandPalette.GoldLit,BrandPalette.GoldBase,3,12,6));
        theme.SetStylebox("hover","NavigationButton",Surface(Hover,12));
```

In `PolishHeaderTheme`, replace the two `foreach` style blocks and the `active` colour block with:

```csharp
        foreach(var (kind,basis) in new[]{("HeaderButton","Button"),("HeaderOption","OptionButton")})
        {
            theme.SetTypeVariation(kind,basis);
            theme.SetStylebox("normal",kind,SlabStyle(Brand.QuietFace,Brand.QuietBase,3,10,3));
            theme.SetStylebox("hover",kind,SlabStyle(Brand.Pressed,Brand.QuietBase,3,10,3));
            theme.SetStylebox("pressed",kind,SlabStyle(BrandPalette.Mint,BrandPalette.MintBase,1,10,3));
            theme.SetStylebox("disabled",kind,SlabStyle(Brand.Wash,Brand.QuietBase,3,10,3));
        }
        theme.SetTypeVariation("HeaderActiveButton","HeaderButton");
        foreach(var (state,face,depth) in new[]{("normal",BrandPalette.Gold,3f),("hover",BrandPalette.GoldLit,3f),("pressed",BrandPalette.Gold,1f)})
            theme.SetStylebox(state,"HeaderActiveButton",SlabStyle(face,BrandPalette.GoldBase,depth,10,3));
        foreach(var state in new[]{"font_color","font_hover_color","font_pressed_color","font_hover_pressed_color","font_focus_color"})theme.SetColor(state,"HeaderActiveButton",new Color(BrandPalette.Ink));
```

In `godot/DebugMain.Management.cs`, `EditorialTheme`, replace the tab styles line (`foreach(var state in new[]{"tab_selected","tab_unselected","tab_hovered"})...`) with:

```csharp
        theme.SetStylebox("tab_selected","TabContainer",SlabStyle(BrandPalette.Mint,BrandPalette.MintBase,3,12,4)); // where you are
        theme.SetStylebox("tab_unselected","TabContainer",Surface(Wash,10));theme.SetStylebox("tab_hovered","TabContainer",Surface(Hover,10));
```

and change `theme.SetColor("font_selected_color","TabContainer",Ink);` to `theme.SetColor("font_selected_color","TabContainer",new Color(BrandPalette.Ink));`.

- [ ] **Step 5: Build and run the checks**

Run:
```powershell
dotnet build MangakaGame.sln -warnaserror
& $godot --headless --path godot -- --brand-smoke
& $godot --headless --path godot -- --display-sweep-smoke
```
Expected: 0 warnings; `BRAND SMOKE PASSED: 8 checks.`; display sweep `520 screen checks, 0 flagged` (fix any flag in this task, with a ruling if a layout value changes).

---

### Task 4: Lilita One for buttons and headings

**Files:**
- Create: `godot/Assets/Fonts/LilitaOne-Regular.ttf`, `godot/Assets/Fonts/OFL-LilitaOne.txt` (downloaded)
- Modify: `godot/DebugMain.Management.cs` (`EditorialTheme`, `Words`)
- Modify: `godot/DebugMain.BrandSmoke.cs` (add `CheckFonts`)
- Modify: `scripts/package-alpha.ps1` (ship the font licence)

**Interfaces:**
- Consumes: `ApplyUiTheme` (existing).
- Produces: `static Font? HeadingFont`, `string _fontPath`, `Font? LoadHeadingFont()`.

- [ ] **Step 1: Download the font and its licence**

```powershell
New-Item -ItemType Directory -Force godot/Assets/Fonts | Out-Null
Invoke-WebRequest https://raw.githubusercontent.com/google/fonts/main/ofl/lilitaone/LilitaOne-Regular.ttf -OutFile godot/Assets/Fonts/LilitaOne-Regular.ttf
Invoke-WebRequest https://raw.githubusercontent.com/google/fonts/main/ofl/lilitaone/OFL.txt -OutFile godot/Assets/Fonts/OFL-LilitaOne.txt
Get-Item godot/Assets/Fonts/* | Select-Object Name, Length
Get-Content godot/Assets/Fonts/OFL-LilitaOne.txt -TotalCount 3
```
Expected: the TTF (about 30 KB) and the licence exist; the licence's first line names the copyright holder (quote it in the credits in Task 7). Then import: `& $godot --headless --editor --path godot --import --quit`.

- [ ] **Step 2: Write the failing smoke check**

In `godot/DebugMain.BrandSmoke.cs`, add `await CheckFonts();` after `await CheckSlabStyles();`, and add:

```csharp
    private async Task CheckFonts()
    {
        Check(Theme.GetFont("font", "Button") is FontFile lilita && lilita.ResourcePath.EndsWith("LilitaOne-Regular.ttf")
            && Theme.GetFont("font", "OptionButton") != Theme.GetFont("font", "Button") && Theme.GetFont("font", "CheckBox") != Theme.GetFont("font", "Button"),
            "Buttons use Lilita One; drop-downs and check boxes keep the reading font");
        var heading = Words(this, "Heading check", 24); var body = Words(this, "Body check", 16); await SettleUi();
        Check(heading.GetThemeFont("font") == HeadingFont && body.GetThemeFont("font") != HeadingFont, "Headings use Lilita One and body text keeps the reading font");
        RemoveChild(heading); heading.QueueFree(); RemoveChild(body); body.QueueFree();
        _fontPath = "res://Assets/Fonts/Missing.ttf"; ApplyUiTheme(); await SettleUi();
        Check(HeadingFont is null && Theme.GetFont("font", "Button") == ThemeDB.FallbackFont, "A missing heading font falls back to the reading font");
        _fontPath = "res://Assets/Fonts/LilitaOne-Regular.ttf"; ApplyUiTheme(); await SettleUi();
    }
```

- [ ] **Step 3: Build to verify it fails**

Run: `dotnet build MangakaGame.sln -warnaserror`
Expected: FAIL with `CS0103` for `HeadingFont` and `_fontPath`.

- [ ] **Step 4: Register the font**

In `godot/DebugMain.Management.cs`, add fields next to `Surface`:

```csharp
    // Lilita One for buttons and headings (spec 2026-09-29, Q46); body text and numbers keep the reading font.
    private string _fontPath="res://Assets/Fonts/LilitaOne-Regular.ttf";
    private static Font? HeadingFont;
    private Font? LoadHeadingFont()
    {
        if(ResourceLoader.Exists(_fontPath)&&ResourceLoader.Load<Font>(_fontPath) is {} font)return font;
        LogTimeline("error heading font missing: "+_fontPath);return null;
    }
```

In `EditorialTheme`, directly after `var theme=new Theme{DefaultFontSize=16};`, add:

```csharp
        HeadingFont=LoadHeadingFont();
        if(HeadingFont is not null)
        {
            theme.SetFont("font","Button",HeadingFont);theme.SetFont("font","TabContainer",HeadingFont);
            // Drop-downs show values and check boxes read as options, so they keep the reading font.
            foreach(var kind in new[]{"OptionButton","CheckBox","CheckButton"})theme.SetFont("font",kind,ThemeDB.FallbackFont);
        }
```

In `Words`, before `return label;`, add:

```csharp
        if(size>=22&&HeadingFont is not null)label.AddThemeFontOverride("font",HeadingFont); // headings
```

In `scripts/package-alpha.ps1`, after the line that copies `private-alpha-credits.txt` to `CREDITS.txt`, add:

```powershell
Copy-Item -LiteralPath (Join-Path $repo 'godot/Assets/Fonts/OFL-LilitaOne.txt') -Destination (Join-Path $output 'OFL-LilitaOne.txt')
```

- [ ] **Step 5: Build and run the checks**

Run:
```powershell
dotnet build MangakaGame.sln -warnaserror
& $godot --headless --path godot -- --brand-smoke
& $godot --headless --path godot -- --display-sweep-smoke
& $godot --headless --path godot -- --title-smoke
```
Expected: 0 warnings; `BRAND SMOKE PASSED: 11 checks.`; display sweep 0 flagged (the wider font is the likeliest cause of a flag; fix in this task with a ruling); title smoke passes.

---

### Task 5: Hand-styled parts

**Files:**
- Modify: `godot/DebugMain.Title.cs` (`TitleButton` becomes `StickerButton(Control parent, …)`)
- Modify: `godot/DebugMain.ManagementMenus.cs` (`ShowPauseMenu` uses sticker slabs)
- Modify: `godot/DebugMain.Startup.cs` (volume Continue is a main action)
- Modify: `godot/HelperPhone.cs` (modern screen colours, icon screen)
- Modify: `godot/ChapterProgressBar.cs` (stage colours, outline)
- Modify: `godot/DebugMain.TitleSmoke.cs` (pause items found recursively)
- Modify: `godot/DebugMain.BrandSmoke.cs` (add `CheckHandStyled`)

**Interfaces:**
- Consumes: `BrandPalette` (Task 1); the title screen's slab code.
- Produces: `Button StickerButton(Control parent, string text, Action action, bool main)`; `static Color PhoneFrame.ModernScreen(bool dark)`, `ModernText(bool dark)`, `ModernBubble(bool dark)`, `ModernEdge(bool dark)`.

- [ ] **Step 1: Write the failing smoke check**

In `godot/DebugMain.BrandSmoke.cs`, add `await CheckHandStyled();` after `await CheckFonts();`, and add:

```csharp
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
```

In `godot/DebugMain.TitleSmoke.cs`, in `CheckPauseMenu`, replace `var items = _menuContent.GetChildren().OfType<Button>().Select(b => b.Text).ToArray();` with:

```csharp
        var items = _menuContent.FindChildren("*", "Button", true, false).OfType<Button>().Select(b => b.Text).ToArray();
```

- [ ] **Step 2: Build to verify it fails**

Run: `dotnet build MangakaGame.sln -warnaserror`
Expected: FAIL with `CS0117: 'PhoneFrame' does not contain a definition for 'ModernScreen'`.

- [ ] **Step 3: Sticker slabs for the pause menu**

In `godot/DebugMain.Title.cs`, rename `TitleButton(string text, Action action, bool main)` to `StickerButton(Control parent, string text, Action action, bool main)`, and in its body replace `_titleMenuBox!.AddChild(edge);` with `parent.AddChild(edge);`. Update the six calls in `ShowTitleMenu` to pass `_titleMenuBox!` first, for example `TitleButton("Continue", ContinueLatest, true)` becomes `StickerButton(_titleMenuBox!, "Continue", ContinueLatest, true)`.

In `godot/DebugMain.ManagementMenus.cs`, `ShowPauseMenu`, replace the six `ActionButton(_menuContent, …)` lines and the `FocusLater(...)` line with:

```csharp
        // The pause menu frames the game like the title screen, so it uses the same sticker slabs (spec 2026-09-29).
        var resume=StickerButton(_menuContent,"Resume",()=>{_menu.Hide();_inMenu=false;},true);resume.ThemeTypeVariation="PrimaryAction";
        StickerButton(_menuContent,"Save",OpenSaveMenu,false);
        StickerButton(_menuContent,"Load Career",LoadCareerMenu,false);
        StickerButton(_menuContent,"Settings",SettingsMenu,false);
        StickerButton(_menuContent,"Report a problem",ReportProblem,false);
        StickerButton(_menuContent,"Quit to title",QuitToTitle,false);
        foreach(var slab in _menuContent.FindChildren("*","Button",true,false).OfType<Button>())slab.CustomMinimumSize=new(300*(float)_presentation.UiScale,0);
```

and keep the build line after them, then add `FocusLater(resume);` at the end of the method.

In `godot/DebugMain.Startup.cs`, change the volume screen's `ActionButton(box, "Continue", () => { … });` statement to assign it and mark it: `var continueButton = ActionButton(box, "Continue", () => { … }); continueButton.ThemeTypeVariation = "PrimaryAction";` (the body unchanged).

- [ ] **Step 4: Phone and chapter bar colours**

In `godot/HelperPhone.cs`, add `using MangakaSim;` and replace lines 13-16 with:

```csharp
    // The period PHS keeps its LCD; the smartphone's screen follows the logo palette (spec 2026-09-29).
    public static Color ModernScreen(bool dark)=>new(BrandPalette.For(dark).Card);
    public static Color ModernText(bool dark)=>new(BrandPalette.For(dark).Text);
    public static Color ModernBubble(bool dark)=>new(BrandPalette.For(dark).Hover);
    public static Color ModernEdge(bool dark)=>new(BrandPalette.For(dark).Outline);
    public Color ScreenColor=>Modern?ModernScreen(Dark):LcdScreen;
    public Color TextColor=>Modern?ModernText(Dark):LcdText;
    public Color BubbleColor=>Modern?ModernBubble(Dark):new Color("aebe93");
    public Color BubbleEdge=>Modern?ModernEdge(Dark):LcdLine;
```

and in `PhoneIcon`, replace `new Color("85d8ca")` with `new Color(BrandPalette.Mint)`.

In `godot/ChapterProgressBar.cs`, add `using MangakaSim;`, replace the five stage colours with:

```csharp
        (Stage.Name,"Storyboard",new("8fb8de")),
        (Stage.Pencils,"Pencils",new("c3a3e6")),
        (Stage.Inks,"Inks",new(BrandPalette.Gold)),
        (Stage.Backgrounds,"Backgrounds",new(BrandPalette.Mint)),
        (Stage.Tones,"Tones",new("f0a0b8"))
```

and replace `new("17222d")` (the percentage outline) with `new(BrandPalette.Ink)`.

- [ ] **Step 5: Build and run the checks**

Run:
```powershell
dotnet build MangakaGame.sln -warnaserror
& $godot --headless --path godot -- --brand-smoke
& $godot --headless --path godot -- --title-smoke
& $godot --headless --path godot -- --alpha-smoke
```
Expected: 0 warnings; `BRAND SMOKE PASSED: 15 checks.`; title and alpha smokes pass.

---

### Task 6: Full verification and rendered review

**Files:**
- Modify only where a check or the review finds a problem, with a `Ruling:` line for any change to a check's expectation or a layout value.

- [ ] **Step 1: Run the full automated suite**

Run:
```powershell
dotnet test tests/MangakaSim.Tests
dotnet build MangakaGame.sln -warnaserror
& $godot --headless --editor --path godot --import --quit
foreach($s in 'smoke-test','management-smoke','progression-smoke','alpha-smoke','usability-smoke','production-smoke','office-life-smoke','convenience-smoke','series-status-smoke','atmosphere-smoke','family-home-smoke','quiet-speed-smoke','display-sweep-smoke','journey-smoke','music-smoke','startup-smoke','title-smoke','brand-smoke'){ & $godot --headless --path godot -- "--$s" 2>&1 | Select-String 'PASS|FAIL|flagged' }
```
Expected: all xUnit tests pass (671 + 4 = 675); 0 warnings; every smoke prints its PASSED line; display sweep `0 flagged`.

- [ ] **Step 2: Capture and review**

Run:
```powershell
& $godot --path godot -- --display-sweep-smoke --capture
& $godot --path godot -- --title-smoke --capture
```
Look at, in both themes: the office, production, publishing, finances, staff, inbox, settings, the pause menu, the phone and the title screen, at 1920 x 1080, 3440 x 1440 and 1280 x 720 with 150% text. Check: slabs read as the logo's buttons, panels stay calm, text is readable, nothing is cut off, gold marks the one main action per card. Fix what looks wrong and record what was seen.

---

### Task 7: Records

**Files:**
- Modify: `godot/Office/ASSETS.md` (font provenance), `docs/superpowers/private-alpha-credits.txt` (font credit)
- Create: `docs/superpowers/brand-ui-restyle-completion.md`
- Modify: `CLAUDE.md` section 5, `README.md` (add `--brand-smoke` to the list of checks)

- [ ] **Step 1: Provenance and credits**

Append to `godot/Office/ASSETS.md`:

```markdown

## Interface font

`../Assets/Fonts/LilitaOne-Regular.ttf` (Lilita One) is used for buttons and
headings. Downloaded 2026-09-29 from Google Fonts' repository
(`google/fonts`, `ofl/lilitaone`); SIL Open Font License 1.1, licence text in
`../Assets/Fonts/OFL-LilitaOne.txt`, shipped beside the game by
`scripts/package-alpha.ps1`.
```

Add to `docs/superpowers/private-alpha-credits.txt`, after the logo line, the copyright line quoted from `OFL-LilitaOne.txt` followed by: `Lilita One, used under the SIL Open Font License 1.1 (see OFL-LilitaOne.txt).`

- [ ] **Step 2: Completion record, CLAUDE.md and README**

Write `docs/superpowers/brand-ui-restyle-completion.md` in the style of `title-screen-and-pause-menu-completion.md`: what changed (palette, slabs, fonts, hand-styled parts), the final palette values, verification (xUnit count, each smoke's count, display sweep, what the captures showed), not verified (feel over a long session), rulings, next step.

Add to `CLAUDE.md` section 5:

```markdown
- **In-game UI restyle (2026-09-29):** the logo's palette in an "evening
  studio" dark theme and a "manuscript paper" light theme, logo slab buttons
  (gold main actions, quiet ordinary buttons, mint for where you are), Lilita
  One for buttons and headings (Q44 to Q47). Spec
  `docs/superpowers/specs/2026-09-29-brand-ui-restyle-design.md`; record
  `docs/superpowers/brand-ui-restyle-completion.md`.
```

In `README.md`, add `--brand-smoke` to the list of other checks after `--title-smoke`. Convert every edited doc to CRLF and confirm no LF-only lines remain.
