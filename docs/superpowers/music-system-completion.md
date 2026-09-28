# Music system, completion

Date: 2026-09-28. Design: [spec](specs/2026-09-28-music-system-design.md) (Q35
gentle reactions, Q36 quiet stretches). Plan:
[implementation plan](plans/2026-09-28-music-system.md). Not yet committed.
Built before any track exists, as agreed; the game sounds exactly as before
until music files are added.

## What changed

- **`MusicPlan`** (`src/MangakaSim/MusicPlan.cs`, engine-free). Decides what
  plays and when: the title pool in menus; in a career the day pool from 06:00
  to 18:00 and the night pool otherwise, with studio tracks joining the day pool
  after the first move out of the parents' house; the time of day read only when
  a track starts, so 32x and the overnight skip never cause switching; no
  immediate repeats; 60 to 120 seconds of quiet after each rotation track; big
  moments crossfading in once per game day, never overnight or in menus, with a
  setback outranking good news, then a deadline, then a convention. It has its
  own random generator, so music cannot affect play or save replays. It also
  finds music files by name, including the renamed files in exported builds.
- **`MusicMoments`** (`src/MangakaSim/MusicMoments.cs`). Good news: a
  serialization offer or acceptance, an award with a prize, the first sale.
  Setback: a cancellation or a rejected pitch. Deadline: a magazine chapter at
  risk. Convention: a convention the mangaka attends today. Only the player's
  own studio counts.
- **`MusicPlayer`** (`godot/Office/MusicPlayer.cs`). Two players for 3-second
  crossfades, 2-second fade-ins, a fade-out, and a 1-second fade and pause when
  the window loses focus; unfocused time does not count toward the quiet gap.
  A file that fails to load is skipped for the session and written once to the
  session timeline.
- **Settings.** A "Music · 0 mutes" slider above the ambience and effects
  sliders, default 50%, saved with the career; older careers get the default.
- **Files.** `godot/Assets/Music/` with a README on naming; the prefix picks the
  pool, so new tracks need no code change.
- **`scripts/convert-music.ps1`** converts Suno WAV or MP3 downloads to
  loudness-matched OGG (ffmpeg loudnorm to -16 LUFS) and skips files whose names
  do not start with a known prefix. "Adding tracks to the game" in
  `music-suno-prompts.md` gives the steps.
- The credits line ("Music: AI-generated with Suno") is added with the first
  real track, not now, because this build has no music.
- **Fixes from the final review:** only the main menu plays the title music, so
  saving from the header keeps the career music; the conversion script now also
  runs in Windows PowerShell 5.1; the new scripts' Godot `.uid` files are
  generated. A separate reviewer found no critical or important problems;
  deferred minor points are in the ledger (a failed moment file loses the
  plan's place, two transitions within 3 seconds cut a track abruptly).
- No simulation, balance or save change beyond the optional music volume.

## Verification

### Automated

- `dotnet test --filter Category!=Playtest`: full suite passes (count in the
  ledger), including `MusicPlanTests` (16) and `MusicMomentsTests` (9), each run
  failing first.
- `dotnet build MangakaGame.sln -warnaserror`: no warnings or errors.
- `--music-smoke`: 10 checks with generated tones standing in for tracks
  (silence with no files, a day track after the opening delay, volume 0 and
  raising it, good news crossfading in and completing, focus pause and resume
  on the same track, a failing file skipped and reported once, title music in
  menus).
- `--alpha-smoke` (45), `--atmosphere-smoke` (893), `--journey-smoke` (53),
  `--display-sweep-smoke` (480 screen checks, 0 flagged), `--smoke-test` and
  `--management-smoke` pass with the player wired in.
- The conversion script, tried on generated tones: a correctly named file
  converted, a wrongly named one skipped with a warning.

### Not verified

- Natural track ends inside Godot: the headless audio driver may not advance
  playback, so this is covered by the plan's unit tests and code review, not a
  live run.
- How the music sounds and feels in play: no real tracks exist yet. That needs
  the user's ears, then testers.

## Next step

When the user is ready: generate the tracks in Suno (plan with commercial
rights) following `music-suno-prompts.md`, save the downloads, then I convert
and import them and we listen together.
