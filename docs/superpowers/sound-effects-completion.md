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
- Not yet done: the Godot import of the new files, a compiled build, the
  `--alpha-smoke` and `--startup-smoke` checks, and hearing them in the game.
  These need the user's OK to build (project instruction 10).

## Next

The user listens and approves or swaps individual sounds; then, with an OK to
build, import, build, run the audio smokes and listen in the game.
