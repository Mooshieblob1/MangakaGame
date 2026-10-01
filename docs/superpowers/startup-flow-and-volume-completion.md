# Start-up disclaimer and first-launch volume, completion

Date: 2026-09-28. Design: [spec](specs/2026-09-28-startup-flow-and-volume-design.md)
(Q37 when the screens appear, Q38 sliders, Q39 wording). Plan:
[implementation plan](plans/2026-09-28-startup-flow-and-volume.md). Not yet
committed; the 13 Suno tracks from the music step are included in the same
change.

## What changed

- **Disclaimer on every launch.** A black screen with the Q39 wording in white,
  fading in over about 1 second, holding for about 3 and fading out over about
  1. A click, key or controller button skips it. It is silent: the Master bus is
  muted while it shows, without changing the saved Master level
  (`godot/DebugMain.Startup.cs`).
- **Volume screen on the first launch only.** "Set your volume" with Master
  (starting at 0), Music (50%) and Sound effects (60%). The title music plays at
  Master times Music, so it fades in as Master rises; moving Sound effects plays
  a pencil scratch. "Continue" saves the levels and ends first-launch setup, with
  the line "You can change this any time in Settings." `--first-launch` shows it
  again.
- **Per-computer settings** in `user://audio-settings.json`
  (`src/MangakaSim/AudioSettings.cs`): Master, Music, Sound effects, "Play sound
  even while unfocused" and whether setup is done. A missing or damaged file
  gives the defaults and shows the volume screen again; out-of-range values are
  clamped; saving never interrupts play.
- **Audio buses** (`godot/DebugMain.Audio.cs`): Music and Effects feed Master,
  so Master caps both. The music player sends to Music; the room tone, activity,
  effects and buzz send to Effects, with the ambience keeping its old balance
  against the effects.
- **Settings** shows Master, Music and Sound effects, and the "Play sound even
  while unfocused" checkbox (off by default), with the line "These settings apply
  to every career on this computer." When the box is off, sound fades and pauses
  in another window, as before.
- **Careers** keep their old music, ambience and effects fields so they still
  load; the game no longer uses them.
- Automated checks skip both screens and use in-memory settings, so they never
  read or write the player's file.
- **Fixes from the final review:** the Sound effects preview is now audible on
  the volume screen and in Settings (menus used to cut it off at once); the
  start-up screens use the game's theme; music waits for the disclaimer to end
  and then fades in, instead of jumping in at full level on later launches; the
  volume screen starts with keyboard focus on Master and no longer swallows keys,
  so keyboard and controller players can reach Continue. A separate reviewer
  found no critical problems; deferred minor points are listed in the session
  summary (per-step saving, a Master-0 hint, disclaimer text size, and others).
- No simulation, balance or save-format change.

## Verification

### Automated

- `AudioSettingsTests` (6): defaults, round trip, missing and damaged files,
  clamping, an unwritable path. Run failing first.
- `--startup-smoke` (16, including the four review fixes): buses feed Master; each slider drives its bus; smoke
  runs never touch the player's file; the disclaimer shows in silence; a key
  skips it once to the volume screen on a first launch; Master starts at 0;
  default levels; raising Master makes sound audible; Continue saves and ends
  setup; later launches show only the disclaimer and sound returns; focus
  behaviour follows the checkbox.
- Full suite and the other Godot checks: see the ledger line for the final run.

### Not verified

- How the fades, the 3-second hold and the default levels feel on a real screen
  and speakers. That needs the user, then testers.

## Next step

The user tries it with `--first-launch`; then commit, and include it with the
music in the next tester build (alpha.13) when approved.
