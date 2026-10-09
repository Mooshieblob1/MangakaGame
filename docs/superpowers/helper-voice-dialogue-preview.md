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

## Official Helper-Chan dub list (Blob, 2026-10-10)

Decided, but not done yet: Blob asked for this to be recorded as the official
list of dubs and not produced for now. In the game today: English (Australian)
and Japanese, for six scenes only.

| Dub | Status |
| --- | --- |
| English, Australian (default) | In game (six scenes) |
| Japanese, English subtitles | In game (six scenes); translation needs a native check |
| English, American | Planned |
| English, English (southern) | Planned |
| English, Scottish | Planned |
| English, Irish | Planned |
| English, Northern Irish (Belfast) | Planned |
| English, Welsh (Valleys) | Planned |
| English, South African | Planned |
| English, New Zealand | Planned |
| English, Indian | Planned |
| English, Singlish (Singlish wording) | Planned |
| Tagalog | Planned, later (replaces a Filipino English accent) |

- All English dubs come from the same designed voice, with only the accent changed.
- Rule: change pronunciation only, never add words to sound stereotypical.
  Singlish is the exception: words are swapped for Singlish ones, nothing added, so
  its text box needs the Singlish wording.
- Not included: Canadian (too close to American; only for lines with Canadian
  words such as zed, washroom, toque or runners), Filipino (a Tagalog dub later),
  Nigerian, Jamaican, Yorkshire and Southern US (not different enough).
- Prompt that worked: `[mood, speaking English with a thick <X> accent] <line>`
  with a mood cue per sentence. Very strong wording ("unmistakable") made v4
  repeat whole phrases. Accent takes are outside the repo in
  `%LOCALAPPDATA%\MangakaGame\voice-tests\2026-10-10\round5`, `round7` and
  `round8`; only English (Australian) and Japanese are in the game so far.

## i18n setup (Blob, 2026-10-10)

- Text languages: English (source and fallback), Japanese and Singlish
  (`src/MangakaSim/Languages.cs`). Settings > Screen has a Language picker:
  Automatic (follows Windows; Singlish only by hand), English, 日本語, Singlish,
  saved per computer as `Language` in `display-settings.json`.
- gettext catalogues in `godot/Localization/ja.po` and `en_SG.po`, registered in
  `project.godot`. The msgid is the exact English text, so Godot controls translate
  themselves and anything untranslated stays English. Code that measures its text
  (the typed dialogue line) calls `Tr` itself.
- Translated so far: Helper-Chan's six voiced conversations (titles, lines,
  answers, Read later, Skip), her name and the new settings labels. The rest of the
  interface is still English: extracting every string is its own sub-project.
- Adding a language: one entry in `Languages.Supported` plus one `.po` file.
- Japanese text uses the Windows system font as a fallback; a bundled CJK font
  (for Steam Deck and Linux) is still needed. Translations are drafts until a
  native speaker checks them.
- Verified: `--helper-voice-smoke` (45 checks, including the scene in Japanese and
  Singlish, captured), 823 simulation tests including new `LanguagesTests`.

## Voice language test (2026-10-10)

Lines 1 to 4 in Mandarin, Cantonese, Korean, Tagalog, Indonesian, Hindi, Arabic,
Russian, German, French, Spanish, European Portuguese, Italian and Polish (56
takes, 669 credits), outside the repo in
`%LOCALAPPDATA%\MangakaGame\voice-tests\2026-10-10\round9-languages`. Scribe heard
every language as intended. Cantonese is written in Cantonese, but Scribe
transcribes it as standard Chinese, so only listening can confirm it. ElevenLabs
refused the Tagalog code `tl`, so Tagalog ran without a language code and was
still heard as Filipino.

## More text languages (Blob, 2026-10-10)

- Added Blob's "group 1" from the voice test as text languages: Spanish (Spain),
  French, German, Italian and Portuguese (Portugal). The Language picker now lists
  English, 日本語, Singlish, Español, Français, Deutsch, Italiano and Português
  (Portugal). Automatic matches the Windows language; European Portuguese only
  matches Portugal, not Brazil.
- Same scope as before: Helper-Chan's six conversations and the settings labels.
  I wrote the translations as drafts, and answers are kept gender-neutral where the
  language allows, since the player character's gender varies.
- Verified: `--helper-voice-smoke` 61 checks with a capture per language (accents
  render in Lilita One), language tests pass.

## Groups 2 and 3 as text, more voice tests (Blob, 2026-10-10)

- Added Russian, Polish, Korean and Indonesian (group 2) and Hindi, Arabic and
  Tagalog (group 3, text only, no dubbing) as text languages. Fifteen languages in
  the picker. Windows' "Filipino" locale picks Tagalog automatically.
- Arabic: the text reads right to left inside each label, but the layout is not
  mirrored. Godot would mirror the whole interface for an RTL locale; the window
  is kept left to right and the hand-placed dialogue controls are pinned left to
  right, since built before joining the tree they would follow the RTL locale and
  land off screen. A proper RTL layout (and right-aligned Arabic text) is later work.
- Cyrillic, Hangul, Devanagari and Arabic fall back to the reading or system font,
  so headings lose Lilita One in those languages; bundled fonts are still needed.
- Voice test, lines 1 to 3: Brazilian Portuguese, Latin American Spanish, Dutch,
  Swedish, Danish, Norwegian and Catalan (21 takes, 217 credits) in
  `round10-languages`. Scribe heard each as intended except two Norwegian takes it
  labelled Danish (close relatives; listen to judge).
- Verified: 82 voice and language checks with a capture per language, language tests.
