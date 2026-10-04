# Display settings: interface size, fullscreen and window size, design

Date: 2026-10-04. Status: Q64 decided in conversation; the rest are routine
decisions made under the user's autonomous working style; this file awaits
review.

## Why

Tester C (alpha.12, findings C2 and C3 in `fresh-player-test-findings.md`):
text is too small on a laptop even at the largest text size, and there is no
resolution or full-screen setting. Causes in the code: the text slider stops at
150% and lives only in a career's Settings; the window opens at a fixed
1600 x 900 and the interface draws at raw pixels, ignoring Windows display
scaling (most 13 to 15 inch laptops run at 125 to 150%), so 100% in the game
looks like about 70% of a normal application there.

## Decisions

### Q64. One "Interface size" that scales everything (decided 2026-10-04)

Buttons, panels, text, the phone and the office labels grow together, 80% to
200%. Default "Automatic" follows Windows display scaling. It replaces the
text-only slider. Rejected: raising only the text slider (buttons and panels
stay small, text crowds and clips) and two controls (interface plus text).

### Routine decisions

- **Per computer, not per career.** Stored in `user://display-settings.json`
  next to the audio settings. The old per-career text scale is no longer
  applied (careers that saved 150% open at the computer's interface size).
- **Window modes:** Fullscreen (borderless, at the screen's resolution) and
  Windowed. Default on a new install: Fullscreen with Automatic interface size,
  which also fixes the 1600 x 900 window not fitting 1366 x 768 laptops.
  Existing installs get the same default once (there is no display file yet).
- **Window size (Windowed only):** standard sizes that fit the current screen:
  1280 x 720, 1600 x 900, 1920 x 1080, 2560 x 1080, 2560 x 1440, 3440 x 1440.
  The window is centred; dragging its edge still works and is remembered.
- **Resolution in fullscreen** is the screen's own; no lower render resolution
  option (Tier 2 if performance testing needs one).
- **Shortcut:** F11 or Alt+Enter toggles Fullscreen and Windowed anywhere.
- **Where:** the first-launch screen gains a "Screen" card (window mode and
  interface size) beside the volume sliders; the title screen's Settings and
  the in-game Settings both show the same controls ("Display" card). Changes
  apply at once and save at once.
- **Automatic size:** Windows reports the screen's DPI; automatic is that DPI
  divided by 96 (100%), rounded to 5%, then limited to what fits (below).
- **What fits:** the interface is laid out for at least 1280 x 720 logical
  pixels, so the largest size offered is the one that keeps the window at
  1280 x 720 or more (a 1920 x 1080 screen allows up to 150%; 2560 x 1440 up to
  200%). The slider's top is that limit, labelled "largest for this screen";
  a saved size above the limit (a smaller monitor) is shown and used at the
  limit without changing the saved value.
- **The 3D office stays sharp:** it renders at the window's real pixels, not
  the scaled interface size.

## How it works

- Godot's window content scale factor scales every 2D control; the layout
  code already reads the logical viewport size, so panels, rail, phone and
  16:9 / 21:9 layouts adapt without per-control changes. Font sizes stop
  multiplying by the old text scale (it stays 1).
- The office `SubViewportContainer`s keep their logical size, while their
  `SubViewport` renders at logical size times the scale factor.
- `DisplaySettings` (engine-free, `src/MangakaSim/DisplaySettings.cs`) holds
  mode, window size, interface size (or Automatic) and the pure maths:
  automatic size from DPI, the largest size that fits a screen, the window
  sizes that fit, load and save with clamping (like `AudioSettings`).

## Testing

- **Unit tests** (`DisplaySettingsTests`): automatic size from DPI (96 to
  100%, 144 to 150%, 192 to 200%); the fit limit for 1366 x 768, 1920 x 1080,
  2560 x 1440 and 3440 x 1440; window sizes offered per screen; load with a
  missing or corrupt file gives defaults; out-of-range values are clamped.
- **Godot:** a new `--display-smoke`: Fullscreen and Windowed switch; F11 and
  Alt+Enter toggle; the window size list fits the screen; interface size 150%
  makes the logical viewport two thirds of the window and text physically
  larger; the office SubViewport renders at full window pixels; the settings
  appear on the first-launch screen, the title Settings and the in-game
  Settings; the saved file round-trips.
- **Display sweep** (`--display-sweep-smoke`) replaces its "150% text" pass
  with 150% interface size at 1920 x 1080 (logical 1280 x 720), keeps 100% at
  every size, and adds 200% at 2560 x 1440; same 24 screens, both themes.
- Existing checks that set the old text scale are updated (rulings).
- Rendered review at 1280 x 720 100%, 1920 x 1080 150% and 2560 x 1440 200%.

## Implementation steps

1. `DisplaySettings` and its unit tests (engine-free).
2. Apply on start-up: load, window mode, window size, content scale, F11 and
   Alt+Enter; remember edge-dragged window sizes.
3. Retire the per-career text scale (stays 1; slider removed) and keep the
   office SubViewports at full pixel resolution.
4. The "Display" controls on the first-launch screen, title Settings and
   in-game Settings.
5. `--display-smoke`; update the display sweep and checks that used the old
   text scale; full suite; rendered review; completion record.

## Out of scope

- Opening flow trimming (C1): re-checked after this work against the current
  opening.
- Lower render resolution, VSync, frame-rate cap, multiple-monitor choice
  (Tier 2 hardware testing).
- Controller and Steam Deck specific layout (Tier 2).
