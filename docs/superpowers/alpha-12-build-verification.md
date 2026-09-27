# Alpha.12 Windows build verification

2026-09-27: the user authorized the pre-package checks and packaging of alpha.12
for the fresh-player test (T1.10).

- Executable: `builds/MangakaStudio-0.8.0-private-alpha.12-Windows/MangakaStudio.exe`.
- Package: `builds/MangakaStudio-0.8.0-private-alpha.12-Windows.zip`, 141,407,451 bytes.
- SHA256: `7155C105257C4BE6508BBF1D07633A43FAB330138FC2A78A9EDE4E6F851391C8`.
- Executable file/product version: `0.8.0.12`. Build name in problem reports:
  `0.8.0-private-alpha.12`. Previous alpha packages were retained.

## Included changes

Tier 1 fixes 1 to 7 (career guidance, first-hire safety, setbacks and economy,
difficulty, the debut wait, mid-career pacing and the display sweep) and the
fresh-player test preparation: session timeline, journey milestones, the
"Attach session timeline" report option, the practice career, and the
walk-through fixes to the daily recap, the Publishing selection and the
Production controls. See the
[preparation record](fresh-player-test-preparation-completion.md) and the
earlier Tier 1 completion records.

## Package contents

- Game: `MangakaStudio.exe`, `MangakaStudio.pck` and the standalone .NET runtime
  in `data_MangakaGame_windows_x86_64`, with its licence and notices.
- `READ ME FIRST.txt` and `Questionnaire.txt` from `docs/superpowers/fresh-player-kit/`.
- `Practice - a struggling series.mangaka`, byte-identical to the test fixture
  (SHA256 compared).
- `CREDITS.txt` and `GODOT-LICENSES.txt`.
- No `START-HERE.md`. The user decided on 2026-09-27 to leave the step-by-step
  guide out while fresh players test the game's own guidance;
  `scripts/package-alpha.ps1` no longer copies it.

## Executed checks

### Automated, before packaging

- `dotnet test --filter Category!=Playtest`: 612 passed, none failed or skipped.
- `dotnet build MangakaGame.sln -warnaserror`: no warnings or errors.
- Godot asset import: no errors.
- Headless smokes: `--smoke-test` (823), `--management-smoke` (99),
  `--journey-smoke` (49) and `--display-sweep-smoke` (480 screen checks,
  0 flagged, 26 checks).
- Earlier in the same session, after the final review fixes:
  `--atmosphere-smoke` (893) and `--quiet-speed-smoke` (1,175).

### Automated, against the packaged game

- The packaging script ran `--alpha-smoke` on the exported executable: 45
  checks passed. Its exported problem report holds `report.json` and
  `timeline.log`, and the timeline starts with "session start" and records the
  new career, Helper-Chan steps, screens, saves and a load.
- Export error log empty. Evidence: `TestResults/package-check`.

### Rendered checks

- `--journey-smoke --capture` on the development build: 58 checks, nine
  1920x1080 captures reviewed (see the preparation record). Not repeated on
  the packaged executable.

### My own look at the package

- Listed the ZIP contents and checked the executable version.
- Unzipped the package to a temporary folder and started `MangakaStudio.exe`;
  it opened its window from that folder.
- I did not click through the practice career import in the packaged game:
  the desktop control tool cannot target an executable that is not installed.
  The import button uses the same `CareerStore.Import` as the four
  `PracticeCareerTests` and the journey walk-through, on a byte-identical file.

## Limits

These are local automated and rendered checks on the development machine, not
clean-machine, lower-end hardware or human playtesting. The window title of
the packaged game reads "MangakaGame". No external release was uploaded and no
tester was contacted.
