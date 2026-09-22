# Sub-project 2 completion

Completed 23 September 2026. Sub-project 3 has not been started.

## Delivered

- Six magazines and three publishers, embedded immutable catalogs, generated
  filler rosters, anchored weekly/biweekly/four-week issue calendars.
- The complete doujin -> one-shot -> offer -> serialization -> rankings ->
  tankobon lifecycle, with editor review, up to two Name redos, and frozen
  chapter quality and page counts.
- Money and an append-only ledger, chapter fees, weekly royalties, four/eight
  week doujin sales, getting online, word of mouth, and convention recaps.
- Fanbase, personal and studio reputation, cultural impact, iconic series,
  copy milestones, genre curves, random booms, and player genre influence.
- Contract-specific grace periods, strikes, warnings, cancellation survival,
  withdrawal, endings, and continued back-catalog sales.
- Strict version 2 saves, required dynamic fields, historical contracts and
  filler references, explicit book membership, and deterministic command replay.
- Godot Production and Publishing tabs with all seventeen commands, rankings,
  books, ledger, trends, editor status, quality, reputation and cancellation
  information. Clock, speed, save and load remain available in either tab.

## Verification

Verified locally on Windows with .NET 8 and Godot
4.7.2.stable.mono.official.ed1daf0bf.

- Simulation suite: **260 passed**, none failed or skipped; approximately
  three seconds of reported test execution in the final run.
- Warning-as-error solution build: **zero warnings and zero errors**.
- Godot headless editor import: passed.
- Actual Godot scene walkthrough: **55 checks passed** headless.
- Rendered Godot walkthrough: **60 checks passed**, including five captures,
  on the local RTX 5070 using Vulkan / Forward+.
- Inspected production, recap, publishing rankings, books/ledger and genre
  screenshots for legible controls, selection, scrolling and clipping.
- A two-year public-command simulation matches uninterrupted execution,
  periodic save/load continuation, and timestamped command replay exactly.
  The tests require doujin revenue, chapter fees, royalties, published
  chapters, commercial books, and reconciled money.
- Both replay scenarios together cover all seventeen command types.
- Focused checks cover editor timing and forced third approval; late pitches;
  midnight work versus Monday sales; offer expiry; historical contract/filler
  references; cancellation and survival; sales windows; milestone crossings;
  retained booms; missing/corrupted save fields; and version 1 rejection.
- Existing Sub-project 1 production, scheduling, overtime and deadline
  guardrails continue to pass without lowering their thresholds.

Seed 2 drives the public-command Godot path: a 19-page monthly drama series,
internet purchase, its first doujin volume, a 31-page pitch to Hoshigaku Flowers,
offer acceptance, publication, and its first tankobon royalties. The displayed
March 1997 checkpoint has seven published chapters and commercial sales.
No finished chapters, successful offers, or sales were injected into this run.
Some focused rule/state tests use controlled fixtures to reach rare conditions.

Validation used automated scene controls and rendered screenshots. A separate
human playthrough and commercial balance qualification have not been performed.
Version 1 saves are deliberately unsupported, as specified in the design.

## Implementation notes

The reconciliations in the implementation plan were followed. Additional
details discovered during implementation:

- `Series.NextChapterNumber` is saved separately, so discarding an untouched
  draft or live pitch cannot reuse a number already present in event history.
- New dynamic model fields are required during JSON deserialization; invalid
  publication slots, frozen quality, historical references, lifetime hours,
  and monetary reconciliation are rejected before replacing a live game.
- Whole-week schedule counting replaces hour-by-hour scanning when computing
  distant chapter risk. A reference test verifies identical results; this
  keeps large production buffers and long scenarios inexpensive.
- Every Monday uses a fanbase and trend snapshot per series before volume
  sales, then applies fan gains and milestones. One volume cannot inflate
  another volume's same-week sales or change its iconic trend factor midway.
- Tests and implementation were grouped into coherent features rather than
  separate scaffold commits. Coverage is consolidated in `PublishingRuleTests`,
  `CatalogTests`, `PublishingTests`, `SeriesLifecycleTests`,
  `MarketBoundaryTests`, `SalesTests`, `PublishingSerializationTests`, and
  `PublishingScenarioTests`.
- The old roadmap's fatigue, additional book formats, and international
  interest remain deferred according to the approved Sub-project 2 design.

Generated screenshots, test saves, temporary probes, and build caches stay
under ignored output directories. The milestone is committed locally; no
push, deployment, release, or work on the next milestone is included.
