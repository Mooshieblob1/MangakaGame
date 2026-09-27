# Tier 1 fix 7: display sweep (T1.9), completion

Date: 2026-09-27. Design: [considerations](specs/2026-09-27-display-sweep-considerations.md)
(Q29, Q30). Plan: [implementation plan](plans/2026-09-27-display-sweep.md).
Not yet committed.

## What changed

- **An automated display sweep** (`--display-sweep-smoke`, new files
  `godot/DisplaySweep.cs` and `godot/DebugMain.DisplaySweepSmoke.cs`). It builds
  one career through real player commands, with a sold doujin, a serialized
  series waiting for its debut, a convention booking, one hire and unread
  inbox items. It then opens 24 core screens at 5 window sizes (1280x720,
  1280x800, 1920x1080, 2560x1080, 3440x1440), 2 text sizes (100% and 150%) and
  both themes: 480 checks. Each check reports controls outside the window,
  overlapping neighbours in the same row or column, floating panels covering
  each other, and button text that is cut off. The smoke fails if any core
  screen is flagged. With `--capture` it keeps AVIF captures of the review set
  and of anything flagged.
- **Fixes found by the sweep:**
  - **The Publishing page could open empty.** Its content was still hidden
    from its earlier life as a tab. This was a real player bug, not a layout
    detail (`DebugMain.Gui.cs`).
  - **Drop-down choices lost their text.** Fixed-width choices now size to
    their longest item, and only choices that stretch across a row may trim.
    The convention event picker and the header's series picker (now at least
    230 px wide, with an ellipsis) were the visible cases.
  - **The office dashboard overlapped the side panel** with 150% text. It is
    now placed at the width it really needs and re-placed when its content
    grows (`DebugMain.FloatingOffice.cs`).
  - **The camera hint** ("Wheel: zoom ...") showed briefly on screens where it
    is switched off (`Office/OfficeView.cs`).
  - **Invisible button text in the light theme.** A focused button, such as
    "Continue" on the day recap, fell back to near-white text. Every button
    style now sets its focused and pressed-hover text colours
    (`DebugMain.Management.cs`, `DebugMain.UiPolish.cs`).
  - **The standing hint above workbench forms** ("Choose the named person..."
    and the Publishing hint) is hidden when the window is short and text is
    large (below 560 px of scaled height, so 1280x720 and 1280x800 at 150%).
    The form gets that space back. Action results still appear there at every
    size (`DebugMain.Gui.cs`, `DebugMain.Management.cs`).
- No simulation, balance, save or text change beyond the above.

## Verification

### Automated

- `dotnet build MangakaGame.sln -warnaserror`: no warnings or errors.
- `dotnet test --filter Category!=Playtest`: 590 tests pass.
- Godot import and all headless smokes pass: `--smoke-test` (823),
  `--management-smoke` (99), `--progression-smoke` (16), `--alpha-smoke` (45),
  `--usability-smoke` (50), `--production-smoke` (21), `--office-life-smoke`
  (734), `--convenience-smoke` (25), `--series-status-smoke` (23),
  `--atmosphere-smoke` (885), `--family-home-smoke` (75) and
  `--quiet-speed-smoke` (1,175).
- `--display-sweep-smoke --capture` (rendered, windowed): "480 screen checks,
  0 flagged", 127 checks passed, no rejected commands while building the
  career.
- Flagged count over the runs: 226 on the first run, then 0, 28, 12 and 0
  as fixes and harness corrections went in, and 0 on the final run. The first
  zero was misleading: the Publishing page was empty, so it had nothing to
  flag. The later runs fixed that and added the light-theme and Sell online
  cases.
- **Limits of the checker.** It measures position, overlap and button text.
  It cannot judge colour contrast, and it treats scrolling content as
  reachable. Those needed the visual review below.

### Visual review (my own inspection of the captures)

119 retained captures in `TestResults/display-sweep-*.avif`: every core screen
at 1280x720 and 1280x800 with 150% text (dark), at 3440x1440 (dark) and at
1920x1080 (light), plus the non-core pages.

- **All core screens are usable** at 1280x720 and 1280x800 with 150% text.
  The side rail, menus, settings and longer forms scroll; nothing needed is
  off screen or hidden behind another panel.
- **Acceptable by design:** the day recap and pop-ups sit over the header;
  Helper-Chan's phone covers part of the dashboard while open; long series
  names and status lines in the top bar end with an ellipsis; at 720 and 800
  with 150% text the speed buttons wrap to a second header row.
- **3440x1440:** panels keep sensible widths and do not stretch across the
  whole screen. 2560x1080 was checked automatically only; it had no flags, so
  it has no retained captures.
- **Light theme at 1920x1080:** readable throughout after the button colour
  fix, including "Continue" on the recap.
- **Rechecked after the last fixes:** the recap button text, the roomier
  Publishing page at 1280x720 and 1280x800 with 150% text, and Sell online
  showing the selected book.
- **Non-core pages** (Awards, Licenses, Studios, Industry, Legacy) at
  1280x720 with 150% text, dark: no problems seen, and the checker reported
  none.

No human playtesting and no clean-machine testing.

## Known issues

- **Minor:** the "100%" label on the chapter progress bar has low contrast
  over the pale segments in the light theme. It is readable, so it is left for
  a later polish pass.
- At 1280x720 with 150% text the Publishing and Sell online forms show only
  a few lines before scrolling. They are usable, but a player on a small
  laptop with large text will scroll often.

## Next step

Preparing the fresh-player test (T1.10).
