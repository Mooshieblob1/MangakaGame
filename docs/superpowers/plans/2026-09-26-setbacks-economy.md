# Tier 1 fix 3: setbacks and economy balance, implementation plan

Date: 2026-09-26. Design: [considerations](../specs/2026-09-26-setbacks-economy-considerations.md)
(Q19 to Q21). Authorized: code edits, build, tests, Godot smoke checks, AVIF
captures and playtest re-runs. No packaging, no commit.

## Approach

Stateless formula changes in `src/MangakaSim/Rules` and the market code, so
the save format (version 10) does not change. Draw order of the simulation
random number generator is kept; only ranges and thresholds change.

## Steps

1. **Pitch odds.** `PitchRules.Chance` base rates 12%, 24%, 40%.
2. **Cancellation pressure.**
   - Grace period 6 published chapters instead of 8 (`GameState.Market.cs`).
   - Earlier warning clock in `CancellationRules.Clocks`.
   - Stronger tier 2 and 3 filler series at market start.
3. **Sales by magazine size.** `SalesRules.CommercialCopies` gains a tier
   factor. Fan gain from commercial copies is weakened.
4. **Print-run royalties.** A stateless `SalesRules.Rung(copiesSold)` gives the
   copies paid so far for a volume. The first rung is paid at
   `ReleaseVolume`; each week the weekly sales step pays only the difference
   when sales pass a new rung. The 20% creator share is billed as before
   through `VolumeContribution`. Digital and overseas stay per unit.
5. **Newcomer page fees.** `ReputationRules.Fee` treats reputation below
   `NewcomerFeeReputation` (50 after tuning; 30 was planned) as that value.
6. **Guidance.**
   - `cancellation-warning`: rank against the line, issues left, the weakest
     factor and fixes, the option to end the series.
   - `series-cancelled`: what is kept, the 52-week wait, where to go next.
   - Playtest bot cases for both steps.
7. **Phone layout.** When Helper-Chan's phone is open, the floating page
   narrows to leave room for it when the screen is wide enough. As built, on a
   cramped screen the phone folds to its icon while a page is open instead of
   the page stopping above it, since the page would be too short to use. Alpha smoke check: the runway line is not covered.
8. **Tests.** Unit tests for the rule changes, the ladder, the fee floor and
   both guidance steps. Update existing expectations that depend on changed
   values.

## Verification

- `dotnet build MangakaGame.sln -warnaserror`.
- `dotnet test tests/MangakaSim.Tests --filter Category!=Playtest`.
- Godot `--smoke-test`, `--management-smoke`, `--alpha-smoke` and usability.
- Rendered captures in `TestResults/setbacks-economy/`, including 1280x720 at
  150% text.
- Career playtest on all five seeds and presets, tuned until: no drain after
  the first hire, ¥2 million to ¥5 million in the business by 1999 on
  Standard, and a rejection or warning in most runs.

## Outcome

Done 2026-09-26. See the [completion record](../setbacks-economy-completion.md).
Tuning during playtesting also lowered the ranking weight of craft to 40% and
raised the newcomer fee floor to 50.
