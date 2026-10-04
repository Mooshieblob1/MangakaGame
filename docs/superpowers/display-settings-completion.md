# Display settings, completion

Date: 2026-10-04. Design: [spec](specs/2026-10-04-display-settings-design.md)
(Q64). Fixes tester C findings C2 (text too small on a laptop even at the
largest size) and C3 (no resolution or full-screen setting). Not yet committed.

## What changed

- **Interface size** (per computer, `user://display-settings.json`): one
  setting scales every panel, button, text, the phone and the office name
  tags, from 80% to 200%. "Automatic" (the default) follows Windows display
  scaling, so a 1920 x 1080 laptop at 150% opens at 150%. The size in use never
  goes above what keeps a 1280 x 720 layout (150% on 1080p, 200% on 1440p),
  with a few pixels of slack so fullscreen windows that lose a border pixel
  still reach those sizes. A saved size larger than a window allows stays
  listed as "150% (limited to 125% here)". Engine-free maths in
  `src/MangakaSim/DisplaySettings.cs`; applied through Godot's window content
  scale in `godot/DebugMain.Display.cs`.
- **Fullscreen or Windowed** (Fullscreen by default), window sizes that fit
  the screen's usable area with the title bar on screen, a remembered dragged
  size, a remembered maximised window, a minimum window of 960 x 540 (a smaller dragged window springs back), and
  **F11 or Alt+Enter** to switch anywhere.
- **Where:** the first-launch screen ("Set your volume and screen"; since the
  [quick start](quick-start-completion.md) it shows the interface size only), the title
  screen's Settings and the in-game Settings; the controls refresh live when
  the window changes.
- **The old per-career text scale is retired** (always 1; older careers that
  saved 150% use the computer's interface size).
- **The 3D office and the character preview stay sharp:** they render at the
  window's real pixels behind a scaled layout (the 3D view is held outside its
  container's sizing and shown through a full-size picture), with screen and
  3D pixel conversions for clicks, name tags and projected points; click
  radii, camera turning speed and name-tag spacing keep their physical feel.
- The display sweep now tests interface sizes (each layout size at 100% and at
  150%, plus 200%) instead of the old text scale, and the other checks that set
  large text now set the interface size.

## Verification

- Unit tests: 22 `DisplaySettingsTests` (automatic size from DPI, the fit
  limit including fullscreen border pixels, window sizes per screen, save and
  load, clamping, the maximised window), written before the code.
- Godot: new `--display-smoke` (16 checks): the first-launch screen, title and
  in-game Settings show the controls; 150% at 1920 x 1080 lays out in 1280 x
  720 with physically larger text; the office renders at full window pixels
  (1920 x 1080 behind a 1280 x 720 layout); name tags grow (20 to 30 px); a
  size above the limit is used at the limit without being forgotten; 200% on
  2560 x 1440; Automatic; only fitting sizes offered; the old slider gone; F11
  and Alt+Enter; saved at once; at 150% a click on Aki selects Aki and a click
  on the floor finds the right cell. The click checks were shown to fail with
  the screen-to-3D conversion deliberately broken.
- Full suite before the review fixes: 792 unit tests, all 24 Godot checks,
  display sweep 616 screen checks, 0 flagged. After the review fixes: 794 unit tests;
  22 of 24 Godot checks passed and the sweep had 616 screen checks, 0 flagged;
  production and convenience failed their 720p fit checks because Godot's own
  window minimum size upset the scaled layout. The minimum (960 x 540) is now
  enforced by springing a dragged window back instead, and both checks, plus
  the display check, pass again (21, 25 and 16 checks).
- Rendered review at 1920 x 1080: 100% and 150% captures; at 150% panels and
  text are larger and the office is as sharp as at 100%.
- Independent review (most capable model, source only): no critical issues;
  two important (fullscreen border pixels stopping 150% on 1080p; a
  screen-sized window opening with its title bar off screen) and ten minor
  findings; both important and eight minor fixed (see Rulings).

## Not verified

- Real fullscreen on Windows (automated checks run windowed or headless; the
  fullscreen border-pixel slack comes from Godot's documentation).
- A real 125% or 150% laptop, a second monitor with different scaling
  (Automatic is re-checked only when the window resizes), and 3D performance
  at full resolution on lower-end hardware (Tier 2).
- Whether tester C now finds the text large enough; C1 (the opening feeling
  like an installation document) is still to be re-checked.

## Rulings

- Interface size is a dropdown of steps (Automatic, 80, 90, 100, 110, 125,
  150, 175, 200) rather than the spec's slider: rescaling while dragging moves
  the slider under the mouse. Cost if wrong: a slider later.
- Checks that set 150% text now set 150% interface size on a 1.5 times larger
  window, so they keep testing the same layout room; the alpha check runs one
  fewer check because the phone no longer folds at that layout (the layout is
  the same as 100% at 1280 x 720, where it has room).
- Automatic does not follow a move to a monitor with different scaling until
  the window resizes (Godot gives no DPI-change signal on Windows); acceptable
  for Tier 1.

## Deferred minors

- `Normalise` uses banker's rounding at exact halves (no practical effect).
- The "Automatic follows the screen's scaling" and F11 checks cannot exercise
  real DPI or window modes in headless or windowed automated runs.
