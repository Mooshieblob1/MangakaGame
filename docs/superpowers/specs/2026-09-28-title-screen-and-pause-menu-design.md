# Title screen and pause menu: design

Date: 2026-09-28. Status: design approved in conversation (Q40 to Q43 and four
design sections); written spec awaiting the user's review.

Background: after the start-up disclaimer and first-launch volume screen, the
game jumps straight to a menu drawn as a panel over the office, and the same
panel opens from the in-game Menu button. The user asked for the screens to fade
into the game instead of the menu appearing at once, and for the main menu to be
its own full-screen screen rather than a modal above the game. They also asked
for a logo on it, and supplied placeholder art until commissioned art arrives
(see the [art commission brief](../logo-designer-brief.md)).

## Goal

A proper front door: launching the game fades from the start-up screens into a
full-screen title screen with the game's art and logo, and starting, continuing
or loading a career fades into the office. Inside a career, a small pause menu
replaces the old full menu, and leaving a career never loses progress.

## Decisions

### Q40. What the in-game Menu button opens (decided 2026-09-28)

A quick pause menu over the game: Resume, Save, Load Career, Settings, Report a
problem, Quit to title. The full title screen appears at launch and after "Quit
to title". Rejected: sending the player to the full title screen from inside a
career (a long trip for a quick save or setting).

### Q41. The game's name (decided 2026-09-28)

**Mangaka Days**, replacing the working title "Mangaka Studio" (one letter from
"Manga Studio", the old Western name of Clip Studio Paint). Player-facing text,
the window title and the exe already use it; the save folder stays at
`app_userdata/MangakaGame`. Record: [art commission brief](../logo-designer-brief.md).

### Q42. Title screen art until the commissioned art arrives (decided 2026-09-28)

The user's placeholder images, made with Google Gemini on 2026-09-28:

- **Background:** a dusk mangaka studio with an empty drawing desk and chair,
  2752 x 1536 (`godot/Assets/Branding/title-background.jpg`). The user edited out
  real manga titles and logos from the bookshelf; checked at full size.
- **Logo:** "MANGAKA DAYS" with a chibi Helper-Chan, cut out to a transparent PNG,
  1840 x 1152 (`godot/Assets/Branding/logo.png`). Her outfit differs from the
  approved reference; acceptable for a placeholder, not a designer reference.

Rejected: the live 3D office dimmed behind the menu, a plain dark screen, and the
existing Helper-Chan desk illustration.

### Q43. How the title screen fits into the game (decided 2026-09-28)

A full-screen title layer inside the existing management screen. The office and
interface load hidden behind it; entering a career fades the layer away.
Rejected: a separate title scene, which would mean rebuilding the management
screen's one-time set-up, career loading and every automated check for the same
result on screen.

## Flow and fades

- **Launch:** the disclaimer, unchanged, ends on black. On the first launch the
  volume screen follows; its "Continue" now fades to black over about 0.6
  seconds instead of vanishing. The title screen then fades in from black over
  about 1 second, with the title track starting under it.
- **Title screen to career:** Continue, "Begin career" in New Career, or a
  career chosen in Load Career fades to black over about 0.6 seconds. The career
  loads during the black, the title layer is removed and the office fades in
  over about 1 second. The title track fades out with the picture, and the normal
  music rotation takes over with its usual quiet stretch first.
- **In a career:** the Menu button or Escape opens the pause menu over the game;
  Escape again, or Resume, closes it. The pause menu keeps the game's own music
  playing softly, as menus do now; it does not switch to the title track.
- **Quit to title:** a safety save first (see Pause menu), then a fade to black
  over about 0.6 seconds and the title screen fading in over about 1 second, back
  on the title track.
- **While a fade runs,** clicks and keys are ignored, so a double-click cannot
  start two careers or skip a save.
- **Reduced interface motion** (existing setting) shortens every fade to about
  0.2 seconds.
- **On the title screen, Escape does nothing**; Quit is on the menu.
- **Automated checks** skip the fades unless a check is testing them, as they
  already skip the start-up screens.

## Title screen layout

- **Background:** the studio illustration always fills the window, scaled to
  cover it and centred: 21:9 trims a little ceiling and floor, 16:10 a little of
  each side. A soft dark gradient over the left side keeps the menu readable and
  fades to clear before the lit drawing desk.
- **Slow push-in:** the illustration zooms in by about 3% over about 40 seconds,
  then back, repeating. Off with Reduced interface motion.
- **Top left:** the logo at about 30% of the window width, sized from the window
  rather than the text scale, so it never swamps small windows. Under it, small:
  "A career told one page at a time."
- **Menu column** under the logo:
  1. **Continue**, the main button, with the latest career beneath it (for
     example "Haruka Studio · 3 Jun 1997"). Shown only when a readable save
     exists; otherwise New Career becomes the main button. If the latest save
     cannot be read, Continue tries the next, as it does now, and says so.
  2. **New Career**
  3. **Load Career**
  4. **Settings**
  5. **Report a problem**
  6. **Quit** (closes the game; no career is in play, so nothing to save)
- **Bottom left, small:** "Private alpha · build", as now.
- **Sub-pages:** New Career, Load Career and Settings open in a large dark panel
  over the dimmed background, reusing the existing screens (character creator,
  save list, settings); Back returns to the menu column. Settings opened from the
  title screen shows the computer-wide settings only (sound, display, text); the
  per-career parts (Helper-Chan tips, automatic pauses, difficulty) appear only
  from the pause menu, since there is no career to change.
- **Keyboard and controller:** focus starts on the main button. The column stays
  readable and unclipped at 150% text and at 1280 x 720.
- **No starter career shows:** the blank career the game creates at launch stays
  hidden behind the title layer and is never shown or saved.

## Pause menu

A small centred panel over the paused, dimmed game:

1. **Resume** (main button; Escape does the same)
2. **Save** (the existing "Keep this chapter of your career" screen: snapshot
   name and portable export)
3. **Load Career** (the existing save list)
4. **Settings** (all settings, including difficulty and the career's Helper-Chan
   and pause options)
5. **Report a problem**
6. **Quit to title**

"Return to this studio" and "Continue latest career" are removed from the
in-game menu. The Helper-Chan page's "Tutorial and notification settings" link
opens Settings from the pause menu.

### Safety saves

Whenever the player leaves a career that is in play, the game first keeps an
autosave, the same kind as the daily autosave, and shows "Progress kept.":

- Quit to title;
- loading a different career from the pause menu;
- closing the window with its close button.

Not on the title screen, during start-up or in automated checks. If the save
fails (for example a full disk or a read-only folder), the player sees a message
naming the problem and stays in the career; for the window close button, the
game stays open with the message, and a second close request quits without
saving. Furniture edits in progress still block leaving, as they block the menu
today.

## Image slots and fallbacks

- The title screen loads `godot/Assets/Branding/title-background.*` (JPG, PNG or
  WebP) and `godot/Assets/Branding/logo.png`. Commissioned art replaces these
  files with no code change.
- If the background is missing or unreadable: the dark interface colour with a
  soft gradient. If the logo is missing or unreadable: "MANGAKA DAYS" in large
  type. Either case is written to the timeline log; the game always starts.
- Provenance: both images are added to `godot/Office/ASSETS.md`, and the credits
  gain "Title screen art and logo (placeholders): AI-generated with Google
  Gemini." The start-up disclaimer already covers AI artwork; its wording stays.

## Testing

- **New `--title-smoke`:**
  - after start-up the title screen shows, and the background covers the window
    with no gaps at 16:9, 21:9 and 16:10;
  - the logo and menu sit inside the gradient without overlapping each other, at
    100% and 150% text;
  - Continue is hidden with no saves and names the latest save when one exists;
  - Begin career, Continue and Load each fade into the office, remove the title
    layer, and a second click during the fade does nothing;
  - Escape opens the pause menu with its six items and closes it again;
  - Quit to title, loading from the pause menu and the window close request each
    keep a safety save, and a failed save keeps the player in the career;
  - Reduced interface motion shortens the fades and stops the push-in;
  - missing background and logo fall back without errors;
  - the title track plays on the title screen only, and the pause menu keeps the
    game's music.
- **Existing checks** that open today's menu (display sweep, usability, alpha,
  music, management, atmosphere, series status) move to the title screen or the
  pause menu. The display sweep adds both screens at all 5 sizes, both text
  scales and both themes.
- **Rendered review:** captures at 1920 x 1080, 2560 x 1080, 3440 x 1440, and
  1280 x 720 at 150% text, reviewed by eye.
- **Not verifiable automatically:** how the fades, the push-in and the art feel
  on a real screen. That needs the user, then testers.

## Out of scope

- Commissioned art (replaces the placeholder files later).
- Changes to the disclaimer and volume screens beyond the fade out of "Continue".
- Renaming internal names (`MangakaGame`, `MangakaSim`, the save folder).
- Updating the tester kit, which stays on alpha.12 for tester B.

## Build approval

Building and running the automated and rendered checks for this work is covered
by the user's 2026-09-28 approval. Packaging a tester build (alpha.13) still
needs a separate go-ahead.
