# Alpha.13 Windows build verification

2026-10-05 (GMT+8): the user approved the recommended first Tier 2 step
("do recommended then"): a small cleanup, the A6 capture and an alpha.13
package for one new tester to re-check C1, the opening.

- Executable: `builds/MangakaDays-0.8.0-private-alpha.13-Windows/MangakaDays.exe`.
- Package: `builds/MangakaDays-0.8.0-private-alpha.13-Windows.zip`, 180,339,924 bytes, 197 entries.
- SHA256: `BDFA469BCF8FC9CB938A853B5164DAFBA9F481E636DD711C01E9746DDFF98D4C`.
- Executable file and product version: `0.8.0.13`, product name "Mangaka Days
  Private Alpha". Build name in problem reports: `0.8.0-private-alpha.13`.
  Previous alpha packages were retained.

## Included changes

Everything on `main` since alpha.12: tester A and B fixes, the career goals
board, progressive disclosure (A3), work feedback and right-click back, the
studio island, streaming sales and the selling tutorial, display settings (C2,
C3), the quick start (C1, Q65), the music system, start-up flow and volume,
title screen and pause menu, the brand restyle and the Tier 1 closeout. See the
completion records in `docs/superpowers`.

Changes in this step:

- The left rail's Japanese subtitle now reads マンガカ・デイズ (Mangaka Days in
  katakana) instead of the old マンガスタジオ (`godot/DebugMain.Management.cs`).
  My default; no Japanese title had been decided.
- The roadmap's T1.3 and T1.4 boxes are ticked, matching the closeout record.
- Tester kit: renamed to Mangaka Days, `MangakaDays.exe`, the title screen and
  in-game menu routes, known gaps updated (music is in; sound effects are
  placeholders, no Steam features). The questionnaire adds a question on the
  first few minutes, for C1.
- Version bumped to alpha.13 (`godot/export_presets.cfg`,
  `src/MangakaSim/ProblemReport.cs`, `AlphaTests`).

## Executed checks

### Automated, before packaging

- `dotnet build MangakaGame.sln -warnaserror`: no warnings or errors.
- `dotnet test --filter Category!=Playtest`: 790 passed, none failed or skipped.
- Godot asset import: no errors.
- Headless smokes: `--smoke-test` (823), `--management-smoke` (99),
  `--journey-smoke` (53), `--progression-smoke` (16) and
  `--display-sweep-smoke` (616 screen checks, 0 flagged, 28 checks).

### Rendered checks

- `--progression-smoke --capture` (windowed, 26 checks), ten 1600 x 900
  captures in `TestResults/progression-*.avif`. I looked at
  `progression-offer.avif`: the A6 licence card shows the guaranteed payment
  split, Aki's share, fit, reliability and approval, a "No royalties" line and
  a plain explanation of fit, reliability and approval. Nothing clipped; the
  explanation runs below the fold inside the scrolling panel, so Accept is a
  scroll away at this size.
- The rail subtitle renders as マンガカ・デイズ (zoomed crop of the same capture).
- Not done: the offer card at 1280 x 720 with a large interface size. The
  display sweep visits the Licenses page at that size, but without an offer.

### Automated, against the packaged game

- The packaging script ran `--alpha-smoke` on the exported executable: 47
  checks passed. Its problem report holds `report.json` and `timeline.log`.
- Export error log empty. Evidence: `TestResults/package-check`.
- The practice career in the package is byte-identical to the test fixture
  (SHA256 compared). Executable version info checked.

## Limits

These are local automated and rendered checks on the development machine, not
clean-machine, lower-end hardware or human playtesting. I did not start the
packaged game in a window or click through it. No external release was
uploaded and no tester was contacted.
