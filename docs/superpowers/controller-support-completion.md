# Controller and Steam Deck support, completion

Date: 2026-10-06. Tier 2 item 3 (roadmap "Steam Deck support"; Q5 in the
[Early Access considerations](specs/2026-09-26-early-access-considerations.md)).
Branch `claude/project-thread-b9i0a0`, not yet on `main`.

## What changed

- **Every screen by controller.** The left stick or D-pad moves between
  controls (held, it repeats after a short pause), A presses, B steps back
  (leaves a text box, closes a popup or the phone, steps back a page, resumes
  from the pause menu). LB and RB flip through the left rail sections and land
  on the new page. Y pauses or resumes, LT and RT go slower and faster (like 1
  and 2). Start opens or closes the pause menu, View opens the inbox, X opens
  Helper-Chan's phone on her reply buttons. The right stick scrolls the open
  page, menu or popup, or looks around the office when no page is open; R3
  steps through three zoom levels.
- **Our own direction search.** Godot's built-in neighbour search skipped whole
  groups of buttons in the floating panels (the new smoke found 107 controls it
  could never reach), so the controller picks the nearest control that lies in
  the pressed direction, preferring ones in line, then by reading order. A dead
  end falls back to the reading order, so nothing is ever unreachable.
- **Focus stays where it belongs.** The title screen, pause menu and
  Helper-Chan popups stop everything behind them from taking focus while open.
  Each area remembers its last control, so closing a popup or rebuilding a
  page returns the controller to where it was. Godot dialogs open with a
  button focused; a confirmation starts on Cancel so a quick A never confirms
  by accident. The 3D office picture is click-focus only, so the stick never
  lands on it.
- **Sliders and number boxes** change with left and right in place; up and
  down move on. Lists (the furniture list) step through their rows.
- **Focus ring.** A gold ring with a dark edge (readable on both themes) glides
  to the focused control on its own layer, clipped to the scrolling page it sits
  in. It shows while a controller or the keyboard is in use, never after a
  mouse click. Sticker buttons (title and pause menu) are ringed whole.
- **Button prompts.** Xbox-style A, B, X and Y (accepted for Deck Verified)
  with LB/RB, LT/RT, View, Menu, D-pad and stick glyphs drawn in code. In a
  career they sit in the notice strip, elsewhere in a small bar at the bottom.
  They change with the moment (Back only when there is somewhere to go back
  to, Adjust on sliders, Edit or Type on text boxes) and show the focused
  control's tooltip, since a controller cannot hover. They show only while a
  controller is in use; the mouse pointer hides meanwhile and returns when the
  mouse or a trackpad moves.
- **Text entry on Steam Deck.** A on a text box starts editing and asks Steam
  for its floating on-screen keyboard (numeric for number boxes). Off a Deck or
  outside Big Picture nothing extra happens. A Steam Deck starts the game ready
  for its controls.
- **Furniture without a mouse.** Move ←, ↑, ↓, → buttons in the furnishing
  editor shift the selected piece one grid square in that screen direction,
  whichever way the camera faces. The furnishing workspace now scrolls, as it
  ran off a 1280 x 800 or 720 window.
- Smaller touches: the header money totals open Finances with A, the overnight
  pause caption says "Press Y" while a controller is in use.
- Code: `godot/DebugMain.Gamepad.cs` (input, navigation, focus, prompts),
  `godot/ControllerOverlay.cs` (ring, glyphs, prompt bar),
  `src/MangakaSim/PadNavigation.cs` (engine-free stick, repeat, trigger and
  prompt rules). No simulation or save change.

## Verification

All on this PC, authorized by Blob on 2026-10-06.

- `dotnet build MangakaGame.sln -warnaserror`: no warnings or errors.
- `dotnet test --filter Category!=Playtest`: 805 passed, none failed
  (7 new `PadNavigationTests`).
- New `--gamepad-smoke` (144 checks, headless and windowed with captures):
  - Reachability: on 27 core screens (title, its settings, new career, office,
    goals, pause menu, settings, save, load, production, publishing, series,
    series details, new series, books, printing, conventions, sell online,
    finances, staff, recruitment, inbox, studios, industry, help, furnishing,
    a Helper-Chan popup) every focusable control is reachable with the four
    directions from where the controller lands, and focus never leaves the
    open area.
  - Buttons: RB and LB change section, B steps back, Start opens the pause
    menu on Resume, holding down never leaves it, A presses Resume, Y pauses,
    RT and LT change speed, View opens the inbox, X opens and B puts away the
    phone, the stick moves once per push, a slider adjusts in place, a popup
    takes and returns focus, the day recap opens with Continue focused, the
    Move buttons shift furniture one square.
  - Layout with the prompts showing: the 27 screens at 1280 x 800 and
    1280 x 720 have nothing off the window, overlapping or cut off; the focus
    ring shows on each captured screen.
  - A6 licence card: at 1280 x 720 100%, 1280 x 800 100% and 1920 x 1080 at
    150% (a 1280 x 720 layout) the card fits and the controller scrolls Accept
    agreement into view.
- Captures reviewed by me (`TestResults/gamepad-*.avif`): office, pause menu,
  title, settings, series details, a popup, furnishing, the phone and the three
  A6 sizes. Ring, prompts and layout look right.
- Existing smokes rerun with the change, all passing: `--smoke-test`,
  `--management-smoke`, `--journey-smoke` (53), `--progression-smoke`,
  `--alpha-smoke` (48), `--startup-smoke` (17), `--title-smoke` (74),
  `--usability-smoke` (50), `--tester-b-smoke` (6), `--disclosure-smoke` (13),
  `--work-feedback-smoke` (24), `--display-smoke` (16), `--brand-smoke` (29),
  `--goals-smoke` (11), `--convenience-smoke` (25) and `--display-sweep-smoke`
  (616 screen checks, 0 flagged).
- Not done: a real controller in hand, a real Steam Deck, the Steam keyboard
  (needs Steam running in Big Picture or on a Deck), Valve's Deck review.

## Open

- **Small text on Steam Deck.** At 1280 x 800 the interface cannot grow past
  100% (it needs a 1280 x 720 layout), and some labels are 11 or 12 px (the
  stage legend under the progress bar, the rail subtitle, captions). Valve
  asks for text about 9 px tall; 12 px type is about 8 to 9 px tall. Raising
  the smallest sizes to 13 px is a design choice for Blob.
- The furnishing workspace scrolls at 1280 x 800 but its 3D view is small
  there; fine for a rare task, worth a look in a Deck playtest.
- Testing on a real Deck is still to be arranged (Q6: outside testers).
