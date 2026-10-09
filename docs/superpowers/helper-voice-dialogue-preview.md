# Helper-Chan voiced visual novel dialogue (preview, 2026-10-10)

Blob asked for Helper-Chan's conversations to play as visual novel scenes with her
ElevenLabs voice, using the "quoted IPA phrase blocks" text format Blob chose after
listening tests, and for a recording to show people.

## What changed

- Non-illustrated Helper-Chan story scenes now open as a full-screen visual novel
  scene: her large portrait (with the scene's expression) slides in on the left, a
  full-width text box with a gold "Helper-Chan" name plate sits along the bottom,
  the line types out at the pace of her voice, and the two answers (gold) plus
  Read later and Skip appear above the box once she finishes. A click or the
  accept key shows the whole line at once. Reduced motion shows it immediately.
- Six scenes are voiced (`godot/Assets/Voice/Helper/`, see its README). A file
  plays only while the scene text still matches what was recorded, so scenes with
  answer-dependent follow-ups stay silent rather than mismatched.
- Voice plays on a new Voice bus that follows the Sound effects slider for now;
  music ducks to about 35% while she speaks.
- The illustrated `desk_moment` scene and the notice popups keep the framed popup.
- Presentation only: no simulation, save or balance change.

## Verified

- `dotnet build MangakaGame.sln -warnaserror`: 0 warnings, 0 errors.
- New `--helper-voice-smoke` (37 checks): six voiced scenes on a new career; voice
  starts, text types out, answers wait for her, the answer reaches the journal,
  Skip stops the voice. Paced in real time so it doubles as the recording.
- `--management-smoke` (99 checks, includes a story choice through the real
  controls), `--gamepad-smoke` (132) and `--music-smoke` (14) pass.
- Rendered check: frames from the recording reviewed at 1600 x 900. Not yet checked
  at 21:9, 1280 x 720 or larger interface sizes. Not playtested by a person.

## Recording

`Godot --path godot --write-movie <file>.avi --fixed-fps 30 -- --helper-voice-smoke`,
then ffmpeg to H.264/AAC MP4 (75 s, 1600 x 900). The preview is outside the repo:
`%LOCALAPPDATA%\MangakaGame\voice-tests\2026-10-10\preview\helper-chan-dialogue.mp4`.

## Open

- Voice for the remaining scenes and answer follow-ups (needs IPA lines and credits).
- A Voice volume slider, and a "voice on/off" option.
- Window resizes while a scene is open are not re-laid out.
