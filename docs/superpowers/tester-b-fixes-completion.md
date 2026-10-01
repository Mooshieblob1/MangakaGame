# Tester B fixes, completion

Date: 2026-10-01. Findings and triage (approved 2026-10-01):
[fresh-player test findings](fresh-player-test-findings.md), tester B. Not yet
committed.

## What changed

- **B1, the hire works** (Q52). Assistants take Backgrounds and Tones by
  default, as in real studios; the mangaka keeps Name, Pencils and Inks unless
  nobody else is available. Team assignments and manual stage choices still
  override. Someone off duty (part-time job, travel, convention) no longer holds
  their stage, so a hire covers the mangaka's absences. The hire's status now
  names whose stage they wait for. Helper-Chan's existing texts ("the planner
  assigns the work automatically", "an assistant can take Backgrounds or
  Tones") are now true and stay. `GameState.Planner.cs`.
- **B4, pitching during a cooldown.** Pitch is disabled while the selected
  magazine is on cooldown, and its tooltip says when it reopens and to choose
  another magazine. Helper-Chan's route to Publishing selects the open magazine
  she suggests. `godot/DebugMain.Publishing.cs`, `DebugMain.Alpha.cs`.
- **B5, Continue.** An imported career keeps its own save time instead of being
  stamped newest, so Continue opens the career last played.
  `src/MangakaSim/CareerStore.cs`.
- **B7, furnishing.** While furnishing, the left rail and the speed buttons are
  locked, and they resync afterwards. While a workspace is open its own notice
  line shows notices, and the bottom bar steps aside instead of repeating them.
  `godot/DebugMain.Office.cs`, `DebugMain.SpeedFeedback.cs`,
  `DebugMain.Management.cs`.

## Verification

- Unit tests, written to fail first: `HireWorkTests` (the hire takes
  Backgrounds and Tones; the hire's waiting status; a team choice still
  overrides; the hire covers the mangaka's stage while they are away) and
  `ContinueAfterImportTests`. Full suite: 683 pass.
- Godot: new `--tester-b-smoke` (6 checks: the suggested magazine is selected,
  Pitch disabled with the reopening date on a magazine on cooldown, Continue
  after an import, rail and speed buttons locked while furnishing, a notice
  shown once, everything restored afterwards). The cooldown check was seen to
  fail without its rule. All 19 Godot checks pass; display sweep 520 screen
  checks, 0 flagged. The full run first caught a crash in the debug interface
  (no left rail there), fixed by resyncing only in the management interface.
- Playtest harness (seed 0, Standard, three years): runs cleanly, money in the
  same range as earlier runs, the first cancellation on the same date, the
  guided first hire later (August instead of May). Not an isolated comparison:
  other changes landed between the runs.

## Not verified

- How the default split changes the feel and balance of a solo studio's first
  months beyond the automated runs; needs play.
