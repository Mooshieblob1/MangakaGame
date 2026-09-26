# Tier 1 fix 4: difficulty that matters, completion

Date: 2026-09-26. Design: [considerations](specs/2026-09-26-difficulty-considerations.md)
(Q22 and Q23). Plan: [implementation plan](plans/2026-09-26-difficulty.md).
Not committed yet.

## What changed

- **Recovery grace reaches editors.** Warning and cancellation waits are
  x0.75, x1 or x1.5 of the standard clock. Shorter waits round down and longer
  waits round up, so a newcomer is warned after 1, 2 or 3 weak issues and
  cancelled after 2, 3 or 5. A new serialization is protected for 4, 6 or 9
  chapters. The rent grace keeps the scale it already had.
- **Business pressure reaches the market.** Rival series in reader surveys
  score x0.9, x1 or x1.1, pitch acceptance odds are x1.15, x1 or x0.85, and
  optional costs keep their x0.85, x1 or x1.15.
- **Scope.** Editor patience, protected chapters, pitch odds and costs apply
  only to the player's own business. Rival strength applies to every
  magazine's rival series. Standard is unchanged, the save format is
  unchanged, the random number order is unchanged, and a changed setting
  applies from then on.
- **Explained in the game.** The pitch chance on Helper-Chan's phone and the
  warning text include the preset. The setup summaries now say what each
  preset does: Relaxed has "weaker rivals, better pitch odds and more patient
  editors", Challenging has "stronger rivals, harder pitches and less patient
  editors". The Custom explanation lists every effect of both dials.
- **Rules in one place.** `src/MangakaSim/Rules/DifficultyRules.cs` holds the
  tables. `GameState` exposes `CancellationClocks`, `GraceChapters` and
  `PitchFactor` so the simulation and the guidance use the same numbers.

## Verification

Automated and rendered checks only; no human playtesting.

- `dotnet build MangakaGame.sln -warnaserror`: no warnings or errors.
- `dotnet test --filter Category!=Playtest`: 576 tests pass, including 8 new
  cases in `DifficultyTests.cs` covering the rule tables and rounding, warning
  timing by preset, protected chapters with a save round trip, independent
  Custom dials, the pitch chance shown by guidance, and rival scores with
  save, load and replay equality.
- Headless Godot checks: `--smoke-test` 823, `--management-smoke` 99 and
  `--alpha-smoke` 45 checks pass.
- Rendered `--progression-smoke --capture`: 26 checks pass. The new step opens
  Career rules, selects Challenging and checks the summary.
  `TestResults/progression-setup-challenging.avif` viewed at 1600x900: the
  summary fits on one line inside the Difficulty card.
- Career playtest re-run (5 runs). Standard seeds 0, 1 and 42 are identical to
  the reports before this fix. Seed 7 now differs by preset:

| Seed 7, to April 1999 | Before | Relaxed now | Challenging now |
|---|---|---|---|
| First pitch | Offer 1996-05-01 | Offer 1996-05-01 | Rejected, then offer 1996-05-31 |
| Pitch rejections | 0 | 0 | 7 |
| First warning | 1997-04-30 (both presets) | None | 1997-02-07 |
| Cancellations | 1 (both presets) | 0 | 3, each followed by a new serialization |
| Business, April 1999 | 0.44 million Relaxed, 1.55 million Challenging | 3.54 million | 0.18 million |

## Known issues

- **Relaxed may be too smooth.** Seed 7 Relaxed met no rejection or
  cancellation in three years. That fits a gentler preset, but a Relaxed
  player may never see the recovery guidance. Review with fresh players
  (T1.10) before changing it.
- **Challenging is lean.** Three cancellations leave the business near zero
  in April 1999, though personal savings stay above 2 million and every
  cancellation led to a new serialization. This matches "tougher but fair",
  and should be rechecked in the Tier 2 balance pass over 5 to 10 years.
