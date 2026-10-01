# Title screen and pause menu, completion

Date: 2026-09-28. Design: [spec](specs/2026-09-28-title-screen-and-pause-menu-design.md)
(Q40 pause menu, Q41 the name Mangaka Days, Q42 placeholder art, Q43 a title
layer inside the management screen). Plan:
[implementation plan](plans/2026-09-28-title-screen-and-pause-menu.md). Not yet
committed; it sits on top of the uncommitted start-up and music work.

## What changed

- **Title screen** (`godot/DebugMain.Title.cs`). The game opens on a full-screen
  title layer over the hidden office: the studio art filling the window, a dark
  gradient on the left, the logo top left, the tagline, and the menu column
  (Continue with the latest career under it when a save exists, New Career, Load
  Career, Settings, Report a problem, Quit). The art pushes in by 3% over 40
  seconds and back. New Career, Load, Settings and Report open in a page panel
  over the dimmed art. Escape does nothing there. Title text stays light in both
  themes.
  A small, semi-transparent note in the bottom-right corner reads "This image
  was AI-generated" whenever the title art is shown (user request).
  After the user's first look: the push-in now zooms inside the picture with a
  small shader (scaling the control stepped visibly on large screens, because
  controls snap to whole pixels), and the menu buttons are compact "logo slabs"
  (user choice A): a gold main button and mint others, on a darker extruded
  base with a brown outline and white sticker edge, colours sampled from the
  logo, pressing down when clicked. The logo may shrink to 64 px tall on
  1280 x 720 with 150% text so nothing scrolls.
- **Fades** (`godot/DebugMain.Transitions.cs`). One black curtain does every
  fade: about 0.6 seconds to black and 1 second back, about 0.2 seconds with
  Reduced interface motion. The volume screen's Continue now fades out, and the
  title screen fades up after the start-up screens. A second click or key during
  a fade is ignored, and the curtain always lifts even if a load fails.
- **Into and out of careers.** Begin career, Continue, Load and Import fade
  through black into the office. A load that fails leaves the player on the title
  screen with the reason. The title track plays only on the title screen.
- **Pause menu.** The Menu button and Escape open a small centred panel: Resume,
  Save, Load Career, Settings, Report a problem, Quit to title. Its sub-pages use
  the full panel. The career keeps its own music.
- **Safety saves.** Quit to title, loading another career from the pause menu and
  the window's close button first keep an autosave ("Progress kept."). A failed
  save names the reason and keeps the player in the career; a second close
  request quits anyway.
- **Title-screen Settings** show only this computer's settings (Dark mode, the
  sound sliders, "Play sound even while unfocused"); per-career options appear
  from the pause menu.
- **Report a problem** from the title screen cannot attach a career.
- **Art slots:** `godot/Assets/Branding/title-background.*` and `logo.png`, with
  a plain backdrop and a text logo if either is missing. Both are now the
  user's NovelAI V5 images: the logo (1712 x 581) and the drafting-board room
  (2688 x 1536, upscaled in NovelAI)
  ([novelai-prompts.md](novelai-prompts.md)). Provenance in
  `godot/Office/ASSETS.md`; credits updated.
- **Fixes from the final review** (a fresh reviewer on the strongest model; each
  fix has a check that failed first):
  - Loading an older autosave from the pause menu used to delete it: the safety
    save trims autosaves to three, and ran before the load. The chosen save is now
    read first.
  - Keys and controller buttons no longer act behind the curtain during a fade,
    and a repeat press cannot run a second safety save.
  - At launch the disclaimer keeps the keyboard; the title's main button takes it
    once the start-up screens end.
  - With a save present, the whole menu fits at 1280 x 720 with 150% text: the
    logo gives up height, the empty message line takes no room, and the menu
    follows keyboard focus.
  - After one refused close, later closes save normally again.
  - Failed loads and saves on the Load and Import pages now show their message on
    that page.
- No simulation, balance or save-format change.

## Verification

### Automated

- `TitleScreenTests` (7): fade lengths, reduced motion, the Continue caption,
  logo width. Run failing first.
- `--title-smoke` (71): fades and the ignored second request, keys held during
  fades, launch focus staying on the disclaimer, the start-up fade, launch on the title screen, menu order and main
  button, art and logo loading, cover fit and layout at 1920 x 1080, 2560 x 1080,
  3440 x 1440, 1680 x 1050 and 1280 x 720 at 150% text with the display sweep's
  inspector, title Settings, Report, Escape, push-in and reduced motion, missing
  art fallbacks, a double Begin career, Continue, a failed load, the pause menu
  and its music, Quit to title and load from pause with safety saves, a failed
  safety save, loading the oldest autosave, messages on the Load pages, and the
  close button including a later close after a refused one.
- Full run: xUnit 671/671; build with warnings as errors, 0 warnings; all 17
  Godot checks pass (smoke-test 823, management 99, progression 16, alpha 45,
  usability 50, production 21, office-life 734, convenience 25, series-status 23,
  atmosphere 893, family-home 75, quiet-speed 1177, display sweep 520 screen
  checks with 0 flagged, journey 53, music 13, startup 16, title 71), after the
  review fixes.
- An intermittent Godot .NET error at shutdown (objects still alive as the
  engine exits) appeared after the title check had passed in 2 of about 20 runs,
  and in no other check; the final run was clean. Cause not established.

### Rendered review (by eye)

Captures in `TestResults` (`title-*`, `pause-menu`, `display-sweep-*title*`):
the art fills 16:9, 21:9 and 16:10 with no bars; the logo and menu are readable
over the gradient and clear of the lit desk; the pause menu is a small centred
panel. The review found the light theme's dark text vanishing over the art,
which is now fixed and checked. The 1280 x 720 at 150% scroll bar is gone with the
logo fitting fix; empty space under the pause menu's buttons remains.

### Not verified

How the fades, the push-in and the final NovelAI art feel on a real screen. That
needs the user, then testers. The window close button was checked through its
handler, not by closing a real window.

## Next step

The user tries it, drops in the NovelAI background and logo, then commits and
includes it in the next tester build (alpha.13) when approved.
