# In-game UI restyle around the logo: design

Date: 2026-09-29. Status: design approved in conversation (Q44 to Q47 and three
design sections); written spec awaiting the user's review.

Background: the title screen now has the NovelAI logo and "logo slab" buttons
(gold and mint faces on a darker extruded base, a brown outline and a white
sticker edge; see the
[title screen completion record](../title-screen-and-pause-menu-completion.md)).
The in-game management UI still uses a generic dark or light theme with teal and
blue accents, so the title screen and the game look like two products. The user
asked to restyle the in-game UI around the logo and the menu buttons.

## Goal

One visual identity from the title screen into the game: the logo's palette,
button shape and lettering carried into every screen, while the dense management
screens (tables, charts, forms, money) stay calm and easy to read.

## Decisions

### Q44. How far the logo style reaches (decided 2026-09-28)

A brand pass. The logo's palette everywhere; the slab treatment for things you
press and things that frame the game (buttons, the header and left rail, the
pause menu, dialogs, Helper-Chan's phone); dense panels become calm flat
surfaces in the new palette. Rejected: the full sticker look on every panel
(heavy on busy screens, tight at 1280 x 720 with 150% text) and menus only (the
game would still look different from the title screen).

### Q45. Dark and light themes (decided 2026-09-28)

Keep both, recoloured from the logo: dark becomes **evening studio** (warm brown
surfaces, cream text) and light becomes **manuscript paper** (cream paper, brown
ink), both with gold and mint for actions. The existing setting and the saved
choice keep working. Rejected: dark only, light only.

### Q46. Fonts (decided 2026-09-28)

**Lilita One** for buttons, headings and titles; the current font for body text,
small capital labels and all numbers. The user's choice approves downloading
`LilitaOne-Regular.ttf` (about 30 KB, Google Fonts' official repository, SIL
Open Font License, which allows commercial use and bundling). Rejected: a
rounded body font as well (a second font, less density on tables), and no new
font (would not echo the logo).

### Q47. Approach (decided 2026-09-28)

Rework the existing code-built theme around a logo palette. Today the look comes
from about nine named colours with dark and light values
(`godot/DebugMain.Usability.cs`), two theme builders (`EditorialTheme` in
`godot/DebugMain.Management.cs`, `PolishTheme` and `PolishHeaderTheme` in
`godot/DebugMain.UiPolish.cs`), and a few components with their own colours.
Changing those changes every screen at once. Rejected: a theme file built in
Godot's editor (a second source that would drift from the code and could not
follow text size or compact spacing), and restyling screen by screen (slow,
inconsistent in between).

## Palette

Starting values from the approved mockup; the contrast tests (below) may adjust
any text or surface colour that fails, and the final values are recorded in the
completion record.

| Role | Evening studio | Manuscript paper |
|---|---|---|
| Page wash (behind panels) | `#221a1c` | `#efe4cf` |
| Card and panel surface | `#2e2426` | `#fbf5e8` |
| Hover surface | `#43363a` | `#f3e8d2` |
| Pressed or selected surface (text selection, pressed rows) | `#5a4644` | `#e6d3ae` |
| Hairline outline | `#5a4644` | `#d8c7a8` |
| Text | `#f6ead2` | `#4b2c2a` |
| Muted text | `#c9b79a` | `#7a6150` |
| Accent text (section labels, links) | `#a8f8e0` | `#2a7a6b` |
| Progress fill | `#a8f8e0` | `#50c0b0` |
| Money gained | `#7fe0c0` | `#1f7a5c` |
| Money lost | `#ff9a8a` | `#b0453a` |
| Quiet slab face / base | `#43363a` / `#241c1e` | `#fbf5e8` / `#d8c7a8` |

Both themes share the logo colours: gold face `#f0c878` on base `#b88048`, mint
face `#a8f8e0` on base `#50c0b0`, slab ink `#4b2c2a` (outline and text on gold
and mint).

## Rules

- **Main actions** (the `PrimaryAction` buttons, one per card) and the **selected
  item in the left rail** are gold slabs: gold face, extruded base, brown outline.
- **Ordinary buttons** are quiet slabs: the same outline and extruded base, with
  the quiet face. Hover lightens the face; pressing sinks it onto the base.
- **Selected tabs and ticked choices** are small mint slabs ("where you are"),
  distinct from gold ("the thing to do").
- **Mint** also marks progress bars and good news; money lost uses coral.
- **In-game slabs have no white sticker edge.** The full sticker (with the white
  edge) is kept for the title screen and the pause menu, the two menus that frame
  the game.
- **Panels** get a soft hairline and rounded corners, never the thick outline, so
  tables and charts stay calm.
- **Headings and buttons** use Lilita One; numbers, body text and small capital
  labels keep the current font.

## Coverage

- **Automatic through the theme:** panels, cards, lists, inputs, tabs, tooltips,
  dialogs, the header and the left rail, and every button style
  (`Button`, `PrimaryAction`, `NavigationButton`, `HeaderButton`,
  `HeaderActiveButton`, `HeaderOption`, `OptionButton`).
- **By hand:**
  - the pause menu's buttons become full sticker slabs, like the title screen's;
  - the volume screen and Helper-Chan's pop-ups use slab buttons;
  - Helper-Chan's phone keeps its period handset (PHS to 2009, then a
    smartphone) with its screen, bubbles and buttons on the palette;
  - the chapter progress bar keeps five distinct stage colours, retuned to sit
    with the palette;
  - money amounts use the gain and loss colours.
- **Unchanged:** layout, spacing, text size, the compact setting and every
  screen's structure; the 3D office and its name tags; the start-up disclaimer;
  the title screen (already styled).

## Readability and safety

- The palette lives in a small engine-free file in `src/MangakaSim` with a
  contrast calculator and xUnit tests. Every text colour meets the standard
  contrast ratio of 4.5:1 against its surface in both themes, including muted
  text, slab ink on gold and mint, accent text and the gain and loss colours;
  headings at 22 px and above need at least 3:1.
- If the Lilita One file is missing or unreadable, the theme falls back to the
  current font and the timeline log records it.
- No save or settings change: the theme preference file keeps its format, so the
  player's existing dark or light choice carries over.

## Testing

- **Unit tests:** palette contrast in both themes.
- **Godot checks:** the display sweep (24 core screens at 5 sizes, 2 text sizes
  and both themes, 520 screen checks) must stay at 0 flagged, including cut-off
  button text from the wider font; new checks confirm the slab styles and fonts
  are in the theme, the pause menu uses sticker slabs, the fallback font works
  and the saved theme choice still loads; the full suite and every other check.
- **Rendered review:** captures of the key screens in both themes at 1920 x 1080,
  3440 x 1440 and 1280 x 720 with 150% text, reviewed by eye, fixed before done.
- **Not verifiable automatically:** whether the new look feels right over a long
  session. That needs the user, then testers.

## Out of scope

- New screens, layout changes or new features.
- The 3D office, characters and name tags.
- A second (body) font.

## Build approval

Building and running checks for this restyle is new work after the 2026-09-28
approval; the plan handoff asks the user to confirm it. Packaging a tester build
still needs its own go-ahead.
