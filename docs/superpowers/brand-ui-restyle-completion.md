# In-game UI restyle around the logo, completion

Date: 2026-09-29. Design: [spec](specs/2026-09-29-brand-ui-restyle-design.md)
(Q44 brand pass, Q45 both themes kept, Q46 Lilita One, Q47 rework the
code-built theme). Plan: [implementation plan](plans/2026-09-29-brand-ui-restyle.md).
Not yet committed; it sits on top of the uncommitted title screen work.

## What changed

- **Palette** (`src/MangakaSim/BrandPalette.cs`). Both themes now come from the
  logo: dark is "evening studio" (warm brown surfaces, cream text), light is
  "manuscript paper" (cream paper, brown ink). The file is engine-free, with a
  contrast calculator and unit tests. The theme properties in
  `godot/DebugMain.Usability.cs` and `DebugMain.UiPolish.cs` read from it, and
  money gained and lost use the palette's gain and loss colours everywhere
  (header, transaction rows, money feedback).
- **Slab buttons** (`godot/SlabStyleBox.cs`). Every button is drawn like the
  logo's buttons: a face on a darker extruded base inside a brown outline.
  Ordinary buttons are quiet slabs; main actions and the selected left rail item
  are gold; pressed or ticked buttons and the selected tab are mint ("where you
  are"); pressing sinks the face onto its base. Gold and mint slabs use the
  logo's brown ink in both themes. Panels, text fields, tables and charts stay
  flat with a soft hairline.
- **Fonts.** Lilita One (`godot/Assets/Fonts`, SIL Open Font License) for
  buttons, tabs and headings of 20 px and above. Body text, small capital
  labels, drop-downs, check boxes and the large money and count figures keep the
  reading font. If the font file is missing the theme falls back to the reading
  font and the timeline records it. The packaging script ships the licence.
- **Hand-styled parts.** The pause menu now uses the title screen's full sticker
  slabs (gold Resume, mint others, white edge). The volume screen's Continue is
  a gold main action. Helper-Chan's smartphone screen (from 2010) follows the
  palette; the period PHS keeps its LCD. The chapter progress bar keeps five
  distinct stages, retuned (blue, lilac, gold, mint, pink) with ink outlines.
  Progress bars fill with the palette's progress colour.
- **Unchanged:** layout, spacing, text size, compact mode, the saved theme
  choice, the 3D office and its name tags, the start-up disclaimer.

## Final palette

| Role | Evening studio | Manuscript paper |
|---|---|---|
| Page wash | `#221a1c` | `#efe4cf` |
| Card and panel | `#2e2426` | `#fbf5e8` |
| Hover | `#43363a` | `#f3e8d2` |
| Pressed or selected | `#5a4644` | `#e6d3ae` |
| Hairline outline | `#5a4644` | `#d8c7a8` |
| Text | `#f6ead2` | `#4b2c2a` |
| Muted text | `#c9b79a` | `#6b5343` |
| Accent text | `#a8f8e0` | `#226658` |
| Progress fill | `#a8f8e0` | `#50c0b0` |
| Money gained | `#7fe0c0` | `#17644a` |
| Money lost | `#ff9a8a` | `#98372e` |
| Quiet slab face / base | `#43363a` / `#241c1e` | `#fbf5e8` / `#d8c7a8` |

Both themes: gold `#f0c878` (lit `#fbe0a6`) on `#b88048`, mint `#a8f8e0` (lit
`#d2fff2`) on `#50c0b0`, ink `#4b2c2a`. Paper muted, accent, gain and loss are
darker than the spec's starting values so every text colour reaches 4.5:1.

## Verification

- **Automated, this computer:** warning-free build; 675 xUnit tests pass
  (671 before plus 4 palette contrast tests); all 18 Godot checks pass after
  the review fixes: smoke test 823, management 99, progression 16, alpha 45,
  usability 50, production 21, office life 734, convenience 25, series status
  23, atmosphere 893, family home 75, quiet speed 1177, display sweep 26 (520
  screen checks at 5 sizes, 2 text sizes and both themes, 0 flagged), journey
  53, music 13, start-up 16, title 74 and the new `--brand-smoke` 29 (palette in
  both themes, money colours, progress fill, slab styles and states, ink on gold
  and mint, Lilita One on buttons and headings, reading font on drop-downs,
  check boxes and number tiles, font fallback, pause menu sticker slabs, volume
  Continue, phone screen, chapter bar stages, and the review fixes below).
- **Rendered review, by eye:** 125 AVIF captures in
  `TestResults/brand-restyle` (office, production, publishing, finances, staff,
  inbox, settings, pause menu, phone and title at 1920 x 1080, 1280 x 800,
  3440 x 1440 and 1280 x 720 with 150% text; light and dark). Slabs read as the
  logo's buttons, gold marks one main action per card, panels stay calm, nothing
  is cut off. The review found three problems (headings of 20 to 21 px in the
  reading font, dark teal progress fill in the light theme, money figures in
  Lilita One); each was fixed with a check that failed first.
- **Independent review:** a fresh reviewer on the most capable model read the
  whole change (source only) and found no critical problems and five important
  ones, all fixed in one pass, each with a check that failed first: a ticked
  check box was unreadable in the evening theme (cream on mint); hovering a
  pressed header button turned it mint and larger; a missing font was not
  logged at launch; Lilita One had no fallback for symbols it lacks (pause bars,
  arrows); charts, floor plans, the overnight badge and caption and your own
  ranking rows still used the old blue and teal (two failed 4.5:1 in the light
  theme). Two rent figures still in Lilita One were fixed as well.

## Not verified

- Whether the new look feels right over a long session. That needs the user,
  then testers.
- Clean-machine rendering of the bundled font (only this computer).

## Rulings made during the work

- Built on `main`, uncommitted, like the earlier work; the user commits.
- The chapter bar's section dividers became ink as well as the percentage
  outline.
- All seven title menu calls were renamed (the plan counted six).
- Heading threshold lowered from 22 px to 20 px after the rendered review: the
  rail title, the office series title and three page headings sit at 20 to
  21 px.
- Progress bars use the palette's progress colour, not the accent; the paper
  accent made the ink percentage hard to read. The plan missed this line.
- Large number tiles keep the reading font (`Figure`), as the spec requires;
  the heading rule had put them in Lilita One.
- `README.md` keeps its LF line endings.

## Deferred minors

- The keyboard focus ring (mint) is hard to see on a focused mint slab in the
  evening theme.
- In the light theme the hover face is a shade darker than the resting face;
  the spec says hover lightens.
- A slab forced below 8 px tall would draw an inverted face (not reachable
  today).
- Hovered tabs use Godot's default near-white text (older than this work).
- Most slab checks read the theme directly and could miss inheritance problems.
- The title and pause sticker buttons have no disabled style (older, no visible
  effect).

## Next step

The user looks at the game in both themes and says what feels off. Then commit
the title screen and restyle work together, and package alpha.13 for tester B
when approved.
