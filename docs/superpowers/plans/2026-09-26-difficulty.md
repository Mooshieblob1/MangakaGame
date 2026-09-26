# Tier 1 fix 4: difficulty that matters, implementation plan

Date: 2026-09-26. Design: [considerations](../specs/2026-09-26-difficulty-considerations.md)
(Q22 and Q23). Authorized: code edits, build, tests, Godot smoke checks, AVIF
captures and playtest re-runs. No packaging, no commit.

## Approach

The existing saved dials `Progression.Pressure` and `Progression.Recovery`
(0 to 2) are read through a new stateless `Rules/DifficultyRules.cs`. The save
format and the random number draw order do not change; only thresholds and
scores do.

## Steps

1. **Rules.** `DifficultyRules` holds every table: recovery factor, grace
   chapters, cost factor, rival strength, pitch factor and the scaled
   cancellation clocks.
2. **State helpers** in `GameState.Progression.cs`: `CancellationClocks`,
   `GraceChapters` and `PitchFactor` apply the dials to the player's own
   business only; `RivalStrength` scales filler series.
3. **Use sites.**
   - `CancellationStep` uses `CancellationClocks`.
   - The market grace test uses `GraceChapters`; filler scores use
     `RivalStrength`.
   - The pitch roll and the phone's pitch outlook use `PitchFactor`.
   - The cancellation warning text uses `CancellationClocks` for issues left.
4. **Setup text.** Preset summaries and the Custom explanation name the new
   effects.
5. **Tests** in `DifficultyTests.cs`: rule tables, warning timing per preset,
   grace chapters with a save round trip, independent Custom dials, pitch
   outlook factors and filler scores with a replay check.

## Verification

- `dotnet build MangakaGame.sln -warnaserror`.
- `dotnet test tests/MangakaSim.Tests --filter Category!=Playtest`.
- Godot `--smoke-test`, `--management-smoke` and `--alpha-smoke`.
- Rendered capture of the setup screen text.
- Career playtest on all five runs; seed 7 must now differ between Relaxed
  and Challenging.
