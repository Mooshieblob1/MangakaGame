# Work feedback and right-click back, completion

Date: 2026-10-03. Bounded change, design approved in chat (no spec or plan file).
Not yet committed.

## What changed

- **Work sparkles.** While the 3D office is the main view, small sparkles in the
  stage's colour arc from each person drawing the header series into that
  stage's part of the header progress bar, landing where its fill ends. About 3
  a second per worker at 8x, growing gently with speed and capped at 32x; none
  while paused, overnight, with a page or menu open, or while furnishing.
  Pacing lives in `src/MangakaSim/WorkFeedback.cs` (`WorkFeedbackPlan`,
  presentation only, no simulation change); drawing in
  `godot/DebugMain.WorkFeedback.cs`.
- **"Done" bubbles.** When a stage of the header series finishes, a bubble in
  its colour ("Pencils done!") drops below the progress bar and fades. At most
  one a second; loading a career, switching series or a chapter that arrives
  already drawn never shows one.
- **Reduced interface motion** turns sparkles and bubbles off.
- **Right-click back** (`godot/DebugMain.RightClickBack.cs`). A right click
  without a drag steps back one level: Helper-Chan's pop-up, a menu sub-page
  (back to the pause menu), the pause menu, then pages through the Back
  history until the 3D office, where it does nothing. On the title screen it
  backs out of a page. A right drag still rotates the camera; text boxes keep
  their copy and paste menu; dialogs keep their own buttons; while furnishing
  it explains instead of leaving; Escape is unchanged.

## Verification

- Unit tests: 736 pass, including 6 new `WorkFeedbackTests` written to fail
  first (rates, pause, the 32x cap, bubbles, no false bubbles, spacing).
- Godot: new `--work-feedback-smoke` (17 checks headless, 19 with captures):
  sparkles at 8x and none paused, under a page or with reduced motion; the
  bubble; right drag versus right click; stepping back to the office and
  stopping there; menu sub-page to menu to closed; the pop-up; a text box; the
  furnishing notice. All 22 Godot checks pass, warning-free build; display
  sweep 560 screen checks, 0 flagged.
- Rendered review at 1920 x 1080: sparkles with trails visible between Aki and
  the bar; the "Storyboard done!" bubble below the bar. The first rate (1.6 a
  second) was too faint in the capture and was raised to 3 with a short trail.

## Music quiet gaps (Q58, same day)

- A problem report ("music stopped playing") matched the planned 60 to 120
  second quiet between tracks, not a fault. The gap is now 15 to 30 seconds,
  and track starts, ends and fade outs go to the problem report timeline.
- Verified: `Quiet_gaps_last_15_to_30_seconds` failed first, then passed; 736
  unit tests; music smoke 14 checks (new: track changes reach the timeline),
  startup 16 and title 74 checks; warning-free build.

## Desk-only sparkles (same day, user follow-up)

- Sparkles now flow only while the worker sits at their desk working; none from
  toilet trips, break-room visits or walking. The simulation already counts
  whole work hours and never leaves the desk; the trips are office decoration.
- The maths: the office shows a break-room visit about once a game hour (about
  5 of every 38 one-speed seconds away) and a toilet trip every 3 to 4 game
  hours (about 20 of every 141), so about 98 of a 10-hour day's 360 seconds are
  away and 73% at the desk. The seated rate is divided by 0.73 (`DeskShare`),
  about 4.1 sparkles a second at 8x instead of 3, so a day's total is unchanged.
- Measured in the smoke over a working day at 8x: 73% seated, 118 sparkles
  against 122 for the old always-on flow. Progress, deadlines and balance are
  unchanged (presentation only).
- Sparkles are about 30% larger with a dark outline, so they read against
  both the pale floor and the dark panels.
- Verified: `Seated_rate_makes_up_for_the_time_away_from_the_desk` failed
  first, then passed; 765 unit tests; `--work-feedback-smoke` 24 checks (27
  with captures: no sparkles on a toilet trip or a break-room visit, the worker
  returns, the measured desk share and the day's count); selling 31 and alpha
  47 checks; warning-free build; rendered capture reviewed.

## Not verified

- How the sparkles feel in motion over a long session (captures are stills).
- Right-click inside drop-down lists and native dialogs (left to their own
  behaviour).
