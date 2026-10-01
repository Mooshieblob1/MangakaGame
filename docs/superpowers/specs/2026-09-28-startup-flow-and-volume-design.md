# Start-up disclaimer and first-launch volume: design

Date: 2026-09-28. Status: design approved in conversation (Q37 to Q39, approach
and two design sections); written spec awaiting the user's review.

Background: the music system and the 13 Suno tracks are in the game. The user
asked that, on launch, the game first shows a silent disclaimer about
generative-AI assets, then a volume screen where the music fades in as the
player raises a master slider, so nobody is startled by sound at full volume.

## Goal

An honest, gentle opening: the AI disclosure is seen every session, and a new
player sets a comfortable volume before any sound plays. Volume becomes a
per-computer setting with a master control that caps music and sound effects.

## Decisions

### Q37. When the screens appear (decided 2026-09-28)

The disclaimer appears on every launch and can be skipped. The volume screen
appears only on the first launch; afterwards the levels are remembered on that
computer and changed in Settings. Rejected: both every launch (tedious, and a
master default of 0 each time would mean silence every session) and both only
once (the disclosure would be seen once per computer).

### Q38. Which sliders (decided 2026-09-28)

Master, Music and Sound effects, on the volume screen and in Settings. Sound
effects covers the office ambience (room tone, the house, pencil activity) and
the interface effects. Settings also gains a "Play sound even while unfocused"
checkbox, off by default. Rejected: a separate ambience slider (a busier first
screen for a small soundscape) and different sliders on the two screens.

### Q39. Disclaimer wording (decided 2026-09-28)

> This game was made with the help of generative AI.
> Some artwork, 3D models and music were created with AI tools,
> then chosen and edited for this game.

The game's name is left out because it is tentative.

## Start-up flow

- **Every launch, the disclaimer.** A black screen with the wording in white,
  centred, at a readable size that follows the text-size setting. It fades in
  over about 1 second, holds for about 3 seconds and fades out over about 1
  second. A click, a key press or a controller button skips to the end of it.
  It is silent: no music or ambience plays until it has gone.
- **First launch only, the volume screen.** Silent until the player acts.
  - Heading "Set your volume".
  - Master starts at 0; Music at 50%; Sound effects at 60%.
  - The title music plays underneath at Master times Music, so it fades in as
    Master rises and follows the Music slider.
  - Moving the Sound effects slider plays a soft pencil scratch at the new
    level.
  - A "Continue" button leads to the main menu, with the line "You can change
    this any time in Settings." Continuing marks the first launch as done, even
    if Master was left at 0.
- **Then** the main menu as today, with the title music at the chosen levels.
- **Settings** shows the same three sliders and the "Play sound even while
  unfocused" checkbox (off by default), replacing today's three per-career
  sliders.
- **Automated checks** (`--*-smoke`, `--smoke-test`, the display sweep, the
  journey) skip both screens so they run unchanged. A command-line flag,
  `--first-launch`, shows the volume screen again for testing.

## Settings storage

- A small per-computer settings file in the game's user folder, next to the
  careers, holds: master, music and sound-effects levels (0 to 1), "play while
  unfocused" (true or false) and whether first-launch setup is done.
- Defaults: master 0 until setup is done, music 0.5, sound effects 0.6, play
  while unfocused off, setup not done.
- A missing or unreadable file uses the defaults and shows the volume screen
  again; it never shows an error. Values outside 0 to 1 are clamped.
- Existing careers keep their saved music, ambience and effects levels in the
  file so they still load, but the game no longer uses them. No career changes.

## Sound routing

- Three Godot audio buses: Master, with Music and Sound effects feeding it.
  Master caps the other two by construction.
- Music: the music player sends to Music.
- Sound effects: the room tone, activity, effects and buzz players send to Sound
  effects. Inside it, the ambience keeps today's default balance relative to the
  effects (about 0.35 to 0.6), so one slider scales both without changing the
  mix.
- Focus: with "play while unfocused" off, all sound fades out and pauses when
  the window loses focus and resumes when it returns, as music does today. With
  it on, sound keeps playing. Office ambience still pauses in menus, as today.

## Structure

- **`AudioSettings`** (engine-free, `src/MangakaSim`): the settings record,
  reading and writing its JSON with defaults and clamping. Testable without
  Godot.
- **Godot side:** bus setup at start-up; a start-up overlay (disclaimer, then
  the volume screen on first launch) shown before the main menu; the music
  player and office audio send to their buses; Settings uses the new sliders.

## Testing

- **Automated tests for `AudioSettings`:** defaults; round trip; a damaged or
  missing file gives defaults with setup not done; clamping; setup done
  survives a reload.
- **Godot check:** the disclaimer appears and a key skips it; the volume screen
  appears only when setup is not done (or with `--first-launch`); Master at 0 is
  silent; raising Master makes the music audible; Music and Sound effects never
  exceed Master; Continue marks setup done; the checkbox changes the focus
  behaviour; existing checks still skip both screens.
- **Not verifiable by me:** how the fades and levels feel. That needs the user,
  then testers.

## Out of scope

Separate ambience or interface sliders, per-career volume, subtitles or a
disclaimer for other topics, and a settings screen redesign.
