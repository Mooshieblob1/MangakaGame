# Sound effects: CC0 recordings replace the procedural placeholders

Date: 2026-10-05. Tier 2 item 5 (sound effects pass).

## Decision

Q67, where the real sound effects come from. Options were free CC0 and
royalty-free libraries, recording our own, AI-generated (ElevenLabs, paid
Starter plan for commercial rights and a Steam AI disclosure) or paid packs.
The user chose option 1, free libraries, "for now". Nothing was spent and no
AI audio is used, so the Steam AI disclosure list is unchanged.

## What changed

- `godot/Assets/Sfx/` holds ten CC0 sounds: a 30-second looping room tone, five
  pencil strokes, two page turns, the interface click and the phone buzz.
  Sources, links and licences are in `godot/Assets/Sfx/README.md`.
- `scripts/convert-sfx.ps1` rebuilds them from the downloaded sources: cuts,
  short fades, hiss reduction on the quiet pencil recordings, levelling to a
  target loudness per sound, and a cross-faded loop for the room tone.
- `godot/Office/OfficeAudio.cs` plays the files. While people work, every 7.5
  seconds it plays a pencil stroke, or about one time in five a page turn,
  never the same sound twice running. The choice uses its own random numbers,
  so the simulation is untouched. The room tone loops. The click plays on
  buttons and the Sound effects slider; the phone buzz plays for new texts.
  If a file is missing or not yet imported, the old procedural sound plays.
- Timing, budgets, volume buses and settings are unchanged. No save change.
- Credits (`docs/superpowers/private-alpha-credits.txt`) and
  `godot/Assets/README.md` now describe the recordings.

## Verification

- Source inspection and measurement of the ten files with ffmpeg (length, mean
  and peak loudness).
- A listening page for the user: https://claude.ai/artifact/3VnAyqDwdG5GMxwvNu4Rfa
- Build authorized by the user 2026-10-05. Because other threads had unfinished
  work in the shared checkout (Steamworks files without their package), the
  branch was cloned into a clean folder and checked there: Godot import of the
  ten files, `dotnet build MangakaGame.sln -warnaserror` with 0 warnings and
  0 errors, `--alpha-smoke` 48 checks passed (including the new "Recorded CC0
  sound effects load, room tone loops" check) and `--startup-smoke` 17 checks
  passed. These are automated headless checks.
- Not yet done: hearing the sounds inside the running game. The `.ogg.import`
  files are git-ignored like the music's, so each checkout runs the Godot import
  step before playing or packaging.

## Next

The user listens (listening page, then in the game) and approves or swaps
individual sounds; swaps go through `scripts/convert-sfx.ps1`. Merge the branch
into main when approved.
