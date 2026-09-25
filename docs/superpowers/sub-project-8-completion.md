# Sub-project 8: private alpha delivery

Date: 2026-09-24. Implemented locally; no commit, push or public release.
Build: `0.8.0-private-alpha.1`.

Follow-up: [controls, printing clarity and dark mode](private-alpha-feedback-1.md)
are delivered in `0.8.0-private-alpha.2`; the original delivery evidence below
describes alpha.1.

Build `0.8.0-private-alpha.3` adds a live current-series progress strip below
navigation, visible above both the office and expanded workbench. It follows
series selection, shows current stage progress and overall chapter completion,
and explicitly labels paused series, completed books and pending chapter approval.
It reads existing work hours without changing simulation state. Old careers
require no migration. The updated package passed 21 opening-flow checks and
50 rendered usability checks from a fresh ZIP extraction, including progress
updates, stage transitions, series switching, read-only behavior and placement.
The solution build passed without warnings. Runtime error logs were empty.

Package: `builds/MangakaStudio-0.8.0-private-alpha.3-Windows.zip`.
SHA256: `EE6ACA8C3FC52669CCD32AFF87279CAA2E2DE7A3BA580819DE50002093B197A0`.

Design: [private alpha](specs/2026-09-24-private-alpha-design.md).
Plan: [implementation](plans/2026-09-24-private-alpha.md).
Tester guide: [playing and reporting problems](private-alpha-testing.md).

## Delivered

Helper-Chan offers an optional next-step card in both new and existing careers.
It recognizes recorded production and sales, remembers observed completion, can
be hidden/resumed and supports a selected project. Doujin growth, contests and
employment change suggestions without restricting career actions. “Show me”
opens and highlights controls while respecting dialogs and furniture drafts.

The normal standalone doujin format supports 8–64 story pages in multiples of
four, with a 16-page suggestion. Ordinary production creates one print-ready book
without five chapters or automatic continuation. It is separate from contest
manuscripts and serialization pitch samples. The focused printing screen shows
printer tiers, quantity, actual business cost, available funds, stock and delivery.
The existing workbench retains conventions, promotion and automatic printing.

Original procedural room tone, pencil strokes and soft action sounds use real-time
cue budgets and natural pitch. Ambience and effects each have volume/mute controls
saved with the career. Office ambience stops in menus/background windows; activity
does not queue up during pauses or accelerate with simulation speed. Music remains
deferred. No sound samples or external audio service are required.

The problem-report screen exports only a local archive. Notes, build/format
versions, game date, difficulty/Sandbox status and counts are explicitly
allowlisted. Screenshots and complete portable careers are optional and initially
unchecked. The screenshot is captured before opening the menu when available.
Reports never upload automatically. Save attachments preserve achievement
provenance, history and artwork without creating a new named gameplay snapshot.

## Saves and performance

Simulation version 8 adds standalone project behavior; career storage envelope
version 2 is separate. Existing local JSON snapshots and format-1 portable
archives remain readable. Version-7 imports preserve the complete command log,
RNG, progression and replay checkpoint rather than resetting their history.
Earlier supported imports still compose through their existing migrations.

New immutable snapshots are `.career` ZIP containers with a small manifest and
losslessly compressed `state.json`. Career listing reads manifests without
inflating state or maintaining an external index. Portable exports use the same
format with content-addressed artwork. Atomic publication preserves prior saves
on failures; corrupt archives are rejected/skipped so Continue can use an earlier
valid snapshot. Full recorded detail remains available.

Ledger validation now groups transfers with their account identity in one pass,
instead of rescanning all ledgers for every transfer. Both-leg balancing and
separate-account validation remain enforced.

Same forty-year fixture, local development machine, single observations:

| Measurement | Result |
| --- | --- |
| Serialized history | 65,248,690 JSON characters |
| Compressed snapshot | 2,720,687 bytes (about 2.7 MB) |
| Save | 477.4 ms |
| List | 10.6 ms |
| Load before transfer-validation change | 13,483.9 ms |
| Load after transfer-validation change | 1,603.0 ms |
| Local and portable round trips | Exact complete state equality |

These are fixture measurements, not guarantees on other machines. Capture and
save remain synchronous; a very long career can still have a short autosave
pause. No historical transactions or events were pruned to obtain these results.

## Opening pacing and targeted fixes

The normal seed-0 opening completed a 16-page story in six simulated days and
reached its first actual sale after fourteen days, using a ten-copy copy-shop
order. Clock-only time is fourteen minutes at 1× or 1.75 minutes at 8×. Reading,
interaction and automatic pauses are excluded; the 15–20 minute human-play target
remains a private-tester measurement, not an established result.

Other normal-seed tests also reached real sales without financial subsidies.
No broad economic retuning, RNG guarantee or fabricated tutorial purchase was
introduced. Existing Tokyo/printing costs and free parents' home are retained;
this milestone introduces no new factual Japanese price assumptions.

Screen checks led to a focused printing panel, protection against hourly updates
resetting an unfinished creation form, and a fix for an unattached settings
control that leaked its popup/resources when settings were reopened.

## Verification

- Baseline: 496 short simulation tests passed before implementation.
- Final short suite: 514 tests passed, including 18 new alpha cases.
- Forty-year simulation and full-history local/portable storage checks passed.
- Godot/.NET build passed with warnings treated as errors.
- Existing headless full harness: 758 checks; management: 56; progression: 15.
- New alpha harness: 21 headless / 26 rendered checks, including actual creation,
  print ordering, quote/payment agreement, ordinary sale, route choices,
  hide/resume, compressed reload, local-report action and sound-rate limits.
- Rendered screens checked at 1600×900 and 1280×720. Final rendered alpha exit
  has no resource-leak warnings after the settings-control fix.

Audio rate/PCM and settings checks are automated; subjective sound mixing still
benefits from human feedback. Existing visual geometry, chibi proportions,
ambient doorways and hyperlapse rendering were retained.

Logs and captures are under `TestResults/alpha-*`. Long-career observations are
under `tests/MangakaSim.Tests/bin/Debug/net8.0/TestResults/alpha-long-storage.txt`.

## Windows candidate

The deliverable is `builds/MangakaStudio-0.8.0-private-alpha.1-Windows.zip`.
Archive size: 109,536,309 bytes. SHA256:
`07A9DE0990F178BB0B9117CF4A3D55FADDA55B9F62A417C34F65EE999F33B54A`.
It contains the executable, PCK, self-contained .NET runtime, tester guide,
credits, complete Godot license records and matching .NET license/notices.
`scripts/package-alpha.ps1` and `godot/export_presets.cfg` reproduce the export.
Official Godot 4.7.2 .NET templates were verified against the release SHA512 sums.

The package script passed 21 exported headless checks without the editor or a
source-project path. A fresh extraction passed all 26 rendered checks with empty
error output. Final extraction/rendered qualification is recorded in
`TestResults/alpha-extracted/`. These checks use this Windows development machine
and RTX 5070; they do not establish clean-machine or broader GPU compatibility.
The game saves careers to its writable user-data folder, separate from the
extracted application. The test harness uses isolated synthetic careers.

No Steam login, platform adapter, installer, signing or automatic updater is
included. Human pacing/usability, subjective audio and clean-machine testing are
the next private-testing activities. Music and Linux/macOS remain later work.

## Confirmed-choice coverage

| Decisions | Delivery |
| --- | --- |
| 1, 11, 12 | Standalone Windows private-test candidate; Steam deferred; existing Sandbox restriction preserved. |
| 2, 6, 7, 15 | Optional adaptive card, safe Show me, route choice, saved preferences and existing-career evidence. |
| 3, 4, 5 | Normal standalone story-to-book-to-sale loop; 16-page default; pacing evidence with human boundary stated. |
| 8, 9 | Original ambience/effects, separate mute and real-time repetition limits; no music. |
| 10 | Local report preview/export, opt-in screenshot/career, no automatic transmission. |
| 13 | Focused usability/stability fixes and observed opening timing; existing economy preserved. |
| 14 | Lossless compression, fast manifest listing, improved transfer validation, exact full-history round trips. |
