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

## Update, same day: voice choice

- Blob dropped the "English with a Japanese accent" lines. Helper-Chan's voice is
  now a per-computer setting under Settings: English (soft Australian accent, Blob's
  picks from three takes per line), Japanese with the English text as subtitles, or
  off. Stored as `HelperVoice` in `user://audio-settings.json`; older files and
  unknown values fall back to English (two new tests in `AudioSettingsTests`).
- Files are in `godot/Assets/Voice/Helper/en` and `ja`. The Japanese translations
  were written for the preview and still need a native speaker's check.
- `--helper-voice-smoke [--helper-voice=ja]` checks either language, plus Skip and
  voice off (38 checks). It now detects Movie Maker mode with `OS.HasFeature("movie")`
  so recordings end on the last answer.
- Blob is rating eleven other English accents (one take per line each) for a
  possible extra option.

## Accent list (Blob, 2026-10-10)

- Official English accents for Helper-Chan, all from the same designed voice with
  only the accent changed: Australian (default), American, English (southern),
  Scottish, Irish, Northern Irish (Belfast), Welsh (Valleys), South African, New
  Zealand, Indian and Singlish. Plus the Japanese voice with English subtitles.
- Rule: change pronunciation only, never add words to sound stereotypical.
  Singlish is the exception: words are swapped for Singlish ones, nothing added, so
  its text box needs the Singlish wording.
- Not included: Canadian (too close to American; only for lines with Canadian
  words such as zed, washroom, toque or runners), Filipino (a Tagalog dub later),
  Nigerian, Jamaican, Yorkshire and Southern US (not different enough).
- Prompt that worked: `[mood, speaking English with a thick <X> accent] <line>`
  with a mood cue per sentence. Very strong wording ("unmistakable") made v4
  repeat whole phrases. Accent takes are outside the repo in
  `%LOCALAPPDATA%\MangakaGameoice-tests‚6-10-10ound5`, `round7` and
  `round8`; only English (Australian) and Japanese are in the game so far.
