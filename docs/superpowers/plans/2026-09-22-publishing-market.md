# Publishing and Market Implementation Plan

Date: 2026-09-22
Status: implemented and verified on 2026-09-23
Completion: [results and implementation notes](../sub-project-2-completion.md)
Design: [Sub-project 2: Publishing and Market](../specs/2026-09-22-publishing-market-design.md)
Roadmap: [Mangaka Studio](../specs/2026-09-22-roadmap.md)
Baseline: simulation implementation `e9662d1`, design document `4143b9e`

## Goal and scope

Extend the existing simulation and Godot debug scene through the complete
doujin -> one-shot -> serialization -> rankings -> volumes/sales lifecycle.
Implement the six magazines, editor decisions, quality, money, reputation,
genre trends, cancellation, withdrawal, and endings described by the design.
Keep every simulation rule deterministic and independently testable.

Execution was authorized after review. The twenty tasks are delivered; see
the completion notes for concrete validation and implementation adjustments.
The source code and Sub-project 1 completion notes, rather than the old plan's
embedded snippets, were the starting point. Tests were consolidated by feature
instead of creating every separately suggested test filename.

Do not add hiring, running costs beyond getting online, fatigue, international
interest, alternate book formats, named historical rivals, awards, anime,
the 3D office, or finished management screens. In particular, the specific
Sub-project 2 design defers international interest despite the older roadmap.

## Constraints and working method

- Keep .NET 8, C# 12, xUnit, Godot 4.7.2 .NET, and the existing solution.
  No new third-party packages are needed.
- `MangakaSim` has no Godot dependency. Godot continues to use `GameState`,
  commands, read-only display queries, and whole-hour `Advance` calls.
- Preserve elapsed-hour work accounting and midnight rollover at tick end.
- Keep exactly one person and exactly five production stages. Editor review
  is a gate, not a sixth stage. The existing pending stage enum value is
  `StageStatus.NotStarted`, not the design's informal `Pending`.
- All player changes use `Apply`. Validate before changing state, consuming
  RNG, allocating ids, emitting events, or appending `CommandLog`.
- Retain `CommandEntry(Time, Command)` and defensive copying of command
  collections. Never replace the timestamped log with `List<ICommand>`.
- Keep per-hour event scanning, compulsory recap pauses, restored recap
  dialogs after loading, and respect for auto-pause during idle skipping.
- Put numerical formulas in pure rule classes. Pass random samples into
  rules; draw them only from `GameState.Rng` in orchestration code.
- Iterate catalogs in JSON order, dynamic entities in ascending id order,
  and dictionaries only via explicit sorted keys when order affects results.
- Use the checked-in design's curves as game balance data. This task does
  not independently certify its historical CPI or manga-market claims.
- Implement in task order. Write focused failing tests, implement, run the
  relevant tests, then run the full suite at the stated integration points.
  Keep commits coherent and buildable. Do not push or publish as part of this
  milestone unless separately requested.

## Reconciliations needed before coding

The approved design contains several conflicts with the running simulation
and a few inconsistent examples. The following are explicit plan decisions,
not assertions that these details were already present in the design. Keep
them visible when reviewing or changing this plan; do not silently weaken
tests to make an incompatible interpretation pass.

### 1. Pitching must be reachable without discarding work

`CompleteChapterIfDone` currently runs the planner immediately, which creates
the next chapter. Consequently a strict requirement for no pending chapter
would reject every pitch through the public API.

Allow `PitchSeries` when there is no unfinished chapter **or** the sole
unfinished chapter is an untouched automatic draft: all stages NotStarted,
zero work and overtime, and no recorded contributor hours. A queue selection
or pin does not mean work has started. Replace that untouched draft with the
31-page one-shot, cleaning its queue, pin, and manual-order references. Retain
completed chapters. Reject any partially worked or skipped draft unchanged.
This also permits pitching immediately after creating a series.

Suppress automatic ordinary chapter creation while Pitching or Offered.
Keep ordinary doujin production continuous while Unpublished. After rejection,
decline, or expiry, resume planning. A completed one-shot awaiting its issue
close remains a pending pitch even though its chapter status is Complete;
validation must require one **unresolved** one-shot, not one unfinished one.

### 2. Freeze chapter facts and distinguish production from publication

Store `Chapter.Pages` at creation; stage costs and chapter fees use it after
`SetPagesPerChapter` changes the series. One-shots always store 31 pages.
Store the editor's magazine on the chapter and publication magazine/contract
identity when published, so old chapters remain meaningful after withdrawal,
ending, or signing with a different magazine.
Archive an outgoing contract before clearing it; published chapter contract
ids must still resolve to that historical contract.

Completing a chapter does not publish it. Ready chapters may accumulate ahead
of their issue dates. Publish each once at its assigned close. On a miss,
move the waiting chapter and any later unpublished regular chapters forward
in order so two chapters never acquire the same contract issue slot. A paused
series can publish already completed stock, then miss subsequent issues.
There is no miss before a contract's first scheduled issue.

Retain the pre-contract doujin cadence. During serialization the contract's
magazine determines publication cadence; reject `SetCadence` on Serialized
series rather than allowing it to alter the contract. Withdrawal restores
the saved doujin cadence and sets the open chapter's due date to
`CadenceRules.NextDue(now, doujinCadence)`. Persist a next-due override when
the series is paused or has no open chapter to receive this date yet.

### 3. Editor approval must not be bypassed by skip or completion

When Name completes or is skipped, enter review before rebuilding queues or
completing the chapter. While review is pending, later work cannot proceed,
even if the player skipped Pencils. A chapter requiring approval cannot be
marked Complete until Approved, even if all five stages are Complete/Skipped.
Permit stage-skip commands during review, but hold chapter completion; skipping
does not waive editorial approval. Re-evaluate completion after approval.

Approval/redo rebuilds queues immediately; work on the newly available task
starts on the next elapsed hour. Redo resets Name to NotStarted with zero
current-attempt work, contribution and overtime, while preserving lifetime
contributor-hour accounting. The third submission is approved without a
random draw. Name quality zero from a skip still follows the same review flow.

### 4. Correct formula examples, rounding, and curve anchors

- With the specified weights and skill factor, a clean Aki chapter is 84.
  Skipping Tones yields `round(84 - 8.4) = 76`, not the design test's 74.
- Redo adds `0.05 * RedoCount` inside Name's skill factor, capped at 1;
  do not also multiply by a separate redo factor.
- Crowding is `max(0.85, 1 - 0.03 * max(0, sameGenreOthers - 2))`:
  0, 1, 2 others -> 1; 3 -> .97; 7+ -> .85.
- Use explicit `MidpointRounding.AwayFromZero` for quality, whole-yen amounts,
  fees rounded to the nearest 100 yen, and ending bonuses to one decimal.
  Copy counts use floor. Clamp reputation, impact and influence after deltas.
- Trend baselines use fractional calendar year with actual year length;
  hold the first/last value outside the keyframes.
- Anchor the economy and internet curves' first 1996 values at
  `GameClock.Start` (1996-04-01 08:00), with later year anchors on January 1.
  This satisfies the design's exact starting index 1.00 and internet cost
  120,000, which January-to-January interpolation would not. Interpolate by
  elapsed time between anchors. After 2026-01-01 the price index is
  `1.18 * 1.02^(fractionalYear - 2026)`; other curves hold their last value.
- Preserve the written iconic pitch formula, using affinity/trend 1.0.
  Iconic status does not directly add fanbase/impact to pitch odds. The prose
  claim that an iconic pitch approaches .95 in any magazine is not true of
  that formula: tier 1 at quality 100 and effective reputation 100 is .384.
  Do not invent an iconic acceptance bonus to make that example true.

### 5. Same-tick close handoff and stable ordering

Capture due issue closes immediately after advancing the clock, before
`IssueCloseStep` advances the magazine calendars. Pass that immutable local
snapshot to both `IssueCloseStep` and `PitchStep`. Do not test equality
against the already-advanced `NextIssueClose` in `PitchStep`.

A one-shot complete and approved at a close can resolve that tick; a late
one-shot waits for the next actual close. It gets neither a strike nor the
old `DeadlineMissed` penalty/event. Offers start on the fourth **future**
close, excluding the close that resolved the pitch, and expire at that time.

At a close, rank before drifting or replacing fillers. Compute all scores
from one pre-award snapshot. Ties use filler rows before player rows, then
ascending id within the kind. Crowding counts the actual competing rows;
a missing player chapter contributes no row. Clamp rank factors beyond the
configured roster size to the bottom factor .2.

Keep retired fillers in a saved archive so `LastRanking` can still reference
the filler that competed in that issue. Only active fillers take part in
future rankings. All generated ids use the existing global `NextId` counter.

### 6. Contracts, grace, cancellation clocks, and cleanup

Add a per-contract published-chapter count; lifetime `ChaptersPublished`
cannot implement the first-eight-chapters grace period after a new contract.
Capture grace before incrementing the count, so publication number eight is
also protected. Misses during grace still lose fans and track record; they
do not add strikes or below-line counts. Grace ends after eight publications,
not after eight calendar issues.

Cancellation clocks are whole issue intervals, using ceiling on `3+9P`,
`3+23P`, and `8-6P`. Weekly/biweekly/monthly intervals are 7/14/28 days.
Drop a strike when its age is strictly greater than the current lifetime.
Misses do not improve or clear below-line history; the warning ages with
magazine closes even when no player row appears.

Roll at most once per series per issue if either trigger applies. Survival
halves counts with floor, drops the oldest strikes, and retains the warning
and its original age, as specified. An overdue warning can therefore trigger
another roll at the next issue. A safe rank clears the warning first.

Use one cleanup helper when dropping an unfinished chapter: remove references
from Queue, Pins, ManualOrder and CurrentTask before replanning. Preserve
historical events/commands and finished chapters, except the live pitch
sample explicitly abandoned by EndSeries, even if that sample is Complete.
An accepted or previously rejected sample is historical, not a live pitch.
Retain contributor-hour history when dropping a sample. Number validation must
allow strictly increasing positive chapter numbers with gaps after dropped
work; do not renumber old chapters or event references.

### 7. Volumes need membership and one-time release state

Add an explicit chapter-id list to each volume. FirstChapter/LastChapter are
display bounds derived from its members, not an instruction to include every
chapter in that numeric range. Old doujin leftovers and newer published
chapters can interleave after switching channels; validate non-overlapping
**membership**, not non-overlapping numeric bounds. Never sell the same
chapter in both a doujin volume and a tankobon in this sub-project.

Collect uncollected published chapters in publication order within a series,
including across contracts. Use the publishing magazine's ChaptersPerVolume
at the publication that triggers a full group. At ending/cancellation, collect
all remaining published chapters if there are at least three. The design
adds no publisher-rights restriction that would strand old-contract leftovers.

Add `ReleasedAt`; WeeksOnSale=0 alone cannot distinguish an unreleased volume
from one released on Thursday awaiting its first Monday sale. Detect release
every tick, emit once, and count selling weeks only on Mondays. Ending or
cancellation does not stop existing volume sales.

Rejected, declined or expired completed one-shots become doujin eligible.
Accepted one-shots remain samples, excluded from regular publication and
volume membership. Re-check the five-chapter doujin threshold whenever a
one-shot becomes eligible, not only inside completion.

Persist a doujin sales-window limit (4 or 8) and a closed flag. Getting online
extends still-open doujin windows to eight sales weeks; it does not restart
an already exhausted four-week run. Commercial volumes close after 52 sales
weeks. Zero-copy weeks still advance the window. All copy/income arithmetic
must be finite and checked before conversion/addition to a `long`.

For each Monday take one fanbase snapshot per series for all that series'
volume calculations, then aggregate fan gains. This avoids changing this
week's sales merely by reordering volumes. Apply online word of mouth after
sales fan gains. Pay royalties using the index at each volume's ReleaseDate.

### 8. Milestones, reputation, and persistent booms

Track actual person-hours per stage and lifetime person-hours per series,
including redo and discarded work. Shares use those hours, not skill-scaled
HoursDone. A chapter with no worked hours gives no personal completion bonus.
Retain lifetime accounting when dropping an open chapter.

Volume thresholds mean crossing from below to at least 100,000 or 1,000,000.
They fire once per commercial volume; the one-million genre-influence reward
also has a once-per-series flag. Doujin sales have the specified release
track-record reward, but no commercial volume milestone/impact rewards.
No extra cultural-impact increment is invented for volume milestones.

On becoming iconic, clear active strikes/warning state once and keep normal
fees, editor reviews and sales. Suppress all rank-driven benefits, including
the top-three impact bonus, because the design says its rank feeds nothing;
the ordinary +.05 publication impact still applies. Completion reputation
and non-rank rewards still apply. Already iconic series never add influence.
Evaluate iconic transition after fanbase or impact changes and before later
side effects for that entity at the same boundary.

Boom state needs a permanent retained component and a flag recording that
the fade-floor roll happened, including when that roll produced zero. Keep
the total saved `Boom` within 0..1. Monthly updates sample the specified
linear fade; retained gains survive clearing an active boom and a later boom
adds on top. Genre `other` gets no noise or booms, but can receive/lose player
influence normally. A monthly transition never rolls twice after save/load.

### 9. Monthly counters, recaps, and history-aware validation

On the first Monday, emit/reset the previous month's convention counters
**before** recording the new Monday's sales. Count doujin sale fan gains and
online doujin word of mouth; do not include commercial-volume fan gains.
Store a last-processed sales timestamp to guard a Monday batch.

Compute `DailyRecap.YenEarned` from that day's positive ledger entries, with
publication/miss lists from events for that worked day. Ordinary sales at
Monday 00:00 belong to Monday, not Sunday's hour-23 work recap. Save an
`ActivityDate` on each event: work-generated events use TickStart.Date;
commands, editor and market events use Clock.Now.Date. Pass the work date
through chapter-completion helpers rather than inferring it from event type,
because a midnight SkipStage command also can complete a chapter. Event.Time
keeps its existing emission timestamp. Filter recap events by activity date
and ledger entries by Time.Date. Retain already-emitted new-day events in the
next recap window when StartNewDay runs; its current reset would lose them.

Save validation must admit histories reachable through normal commands:
completed unresolved one-shots while Pitching; skipped Name under review;
all stages done but chapter awaiting approval; previously edited/published
chapters after withdrawal; retired fillers in historical rankings; chapter
number gaps; and interleaved volume display ranges. Preserve the design's
intent by checking the appropriate historical identity and membership.

## File and state contracts

Use the design's new files and existing partial-class pattern. Additional
small files below make the orchestration seams explicit; splitting a growing
partial file by responsibility is acceptable without adding a framework.

| File | Responsibility |
|---|---|
| `Catalog/PublisherCatalog.cs`, `Catalog/TrendCatalog.cs` | Immutable validated catalog objects, cached embedded defaults, FromJson test entry points |
| `Data/publishers.json`, `Data/trends.json` | Exact design tables plus filler title word lists |
| `Rules/QualityRules.cs`, `PitchRules.cs`, `EditorRules.cs` | Chapter contributions, chance and weakest factor, review delay/probability |
| `Rules/RankingRules.cs`, `FanbaseRules.cs` | Score, fan score, rank factors and fan gains |
| `Rules/ReputationRules.cs`, `CancellationRules.cs` | Reputation/fee/protection/ending math, issue-clock rules |
| `Rules/SalesRules.cs`, `TrendRules.cs`, `Economy.cs` | Copies/income, curves and trends, prices |
| `Rules/IssueSchedule.cs` | Anchored 7/14/28-day magazine schedules, separate from doujin AddMonths |
| `Market.cs` | Dynamic types, enums and records listed in the design plus supporting state below |
| `GameState.Publishing.cs` | Editor and pitch steps, review gate, offers and new publishing commands |
| `GameState.Market.cs` | Close snapshots, publication/rankings, filler lifecycle, monthly trends |
| `GameState.Sales.cs` | Ledger helper, volume scheduling/releases/sales, online purchase and convention counters |
| `GameState.Reputation.cs` | Award application, iconic transitions, cancellation and series lifecycle |
| `GameState.Serialization.cs` | Required v2 fields and semantic validation; no migration |
| `tests/MangakaSim.Tests/Rules/` | Exact-input rule tests |
| `tests/MangakaSim.Tests/Fixtures/` | Small checked-in v1 save and test scenario helpers |
| `godot/DebugMain.Publishing.cs` (optional partial) | Publishing/market controls and refresh methods |

Paths without a `tests/` or `godot/` prefix are relative to `src/MangakaSim`.
Rule filenames grouped after a `Rules/` path also belong in `Rules/`.

Add every field in design section 1 and these supporting fields together in
the v2 model. Use public serializable properties, enum strings, and JsonIgnore
for computed views/catalogs. Do not depend on a UI variable for any of them.

| Owner | Supporting saved state |
|---|---|
| GameState | `DateTime? LastTrendUpdateMonth`, `long DoujinCopiesThisMonth`, `double DoujinFansThisMonth`, `DateTime? LastSalesAt` |
| Contract | `int Id`, `DateTime FirstIssueClose`, `int ChaptersPublished` in addition to magazine, fee and signing time |
| Series | `Cadence DoujinCadence`, `DateTime? NextChapterDueOverride`, `int NextChapterNumber`, `bool MillionCopyInfluenceAwarded`, `Dictionary<int,long> LifetimeHoursByPerson`, `List<Contract> PastContracts` |
| Chapter | `int Pages`, `string? EditorMagazineId`, `string? PublishedMagazineId`, `int? PublishedContractId`, `bool PitchResolved`, `bool DoujinEligible` |
| StageWork | `Dictionary<int,long> HoursByPerson` in addition to design OvertimeHours and Contribution |
| Volume | `List<int> ChapterIds`, `DateTime? ReleasedAt`, `int SalesWindowWeeks`, `bool SalesClosed` |
| GenreTrend | `double PermanentBoom`, `bool BoomFloorChosen` in addition to the design's active-boom fields |
| MagazineState | `List<FillerSeries> RetiredFillers`; these are historical records, not competitors |
| GameEvent | `DateTime ActivityDate` at date precision for recap attribution |

`PublishingStatus`: Unpublished, Pitching, Offered, Serialized.
`EditorStatus`: NotRequired, AwaitingReview, Approved, RedoRequested.
`VolumeFormat`: Tankobon. `Demographic`: Shonen, Shojo, Seinen, Josei.

An ephemeral `IssueCloseContext(MagazineId, CloseTime, IssueNumber)` list is
local to a tick and is never saved. Existing event fields and the existing
Emit signature remain usable; append an optional `EventContext` carrying
MagazineId, VolumeId, Rank, Amount and an optional ActivityDate override
without creating ambiguous overloads.

The command record names follow the existing `...Command` convention:
`PitchSeriesCommand`, `AcceptOfferCommand`, `DeclineOfferCommand`,
`WithdrawSeriesCommand`, `EndSeriesCommand`, `GetOnlineCommand`.

## Task sequence

Each task lists its prerequisites, files, implementation steps and acceptance
checks. A task is complete only when its checks pass, not merely when its
files exist. Keep test fixture changes tied to an intentional behavior change.

### Task 1: Capture the baseline and remove brittle test identities

**Depends on:** nothing.
**Files:** existing tests; add `Fixtures/SimulationFixture.cs` and
`Fixtures/v1-minimal.json` under the test project.

- [x] Read the design, this plan, current source and Sub-project 1 completion
  notes. Check repository status and preserve unrelated edits.
- [x] Run `dotnet test tests/MangakaSim.Tests` and
  `dotnet build MangakaGame.sln -warnaserror`. The recorded baseline is 148
  passing tests, not a fixed required count for the expanded project.
- [x] Save one real, minimal v1 `NewGame().ToJson()` as a fixture for rejection
  testing after the version bump.
- [x] Replace hardcoded series/chapter ids such as 2 and 3 in regression and
  replay tests with ids obtained from their setup state. Keep tests that
  intentionally assert global id allocation separate.
- [x] Preserve existing pure production scenarios as doujin scenarios; do
  not reinterpret their weekly deadlines as magazine publication deadlines.
  Run the suite before proceeding.

### Task 2: Embedded catalogs, schedule math, and RNG primitives

**Depends on:** Task 1.
**Files:** both `Catalog/*.cs`, both `Data/*.json`, `Rules/IssueSchedule.cs`,
`Rng.cs`, `MangakaSim.csproj`; new `CatalogTests.cs`, `IssueScheduleTests.cs`,
and additional RNG tests.

- [x] Add `<EmbeddedResource Include="Data/*.json" />`. Load resources via
  the assembly, never the current directory or a Godot path.
- [x] Transcribe all six magazines, affinities, twelve genres, baseline,
  price and reach tables exactly. Add at least forty adjectives and forty
  nouns for fictional filler titles. Preserve JSON catalog order.
- [x] Implement `LoadDefault()` with cached immutable results and `FromJson`
  for tests. Reject duplicate/empty ids, unknown publisher/genre references,
  invalid enum/cadence/hour/day/tier, nonpositive volume size, invalid roster
  and fee bounds, nonfinite numbers, malformed curves and missing `other`.
  Enforce the spread rule for every shipped keyframe year.
- [x] Implement anchored `FirstCloseAtOrAfter`, `FirstCloseAfter` and
  `AddIssues`. Monthly magazine periods are 28 days; doujin Monthly remains
  calendar-month `AddMonths(1)`. Test 1996 starts, exact close equality,
  biweekly alignment, year rollover and the fourth future close.
- [x] Add `Rng.NextDouble()` using the high 53 bits of one `NextUInt64()`;
  add finite ordered-range `NextDouble(min,max)` and
  `NextInt(minInclusive,maxExclusive)`. Validate arguments before drawing.
  Retain existing RNG sequence and `NextInt(maxExclusive)` behavior.
- [x] Test range endpoints, copied-state continuation and unchanged state on
  invalid bounds. In this plan `NextInt(1,3)` means one or two years;
  inclusive popularity ranges use max+1.

### Task 3: Quality, pitching, and editor rules

**Depends on:** Task 2.
**Files:** `Rules/QualityRules.cs`, `PitchRules.cs`, `EditorRules.cs`;
corresponding files in `tests/MangakaSim.Tests/Rules/`.

- [x] Implement skill factor, weight, rush factor, Name redo bonus, per-stage
  contribution and rounded total quality from design section 5.
- [x] Test clean skill-80 total 84; skipped Tones 76; Pencils with 20% overtime
  contribution 22.68 instead of 25.2; rush floor .7; skipped contribution
  zero; skill/redo boundary cases and rounding ties.
- [x] Implement pitch factor calculation, `Chance`, and `WeakestFactor`.
  On tied weakest factors choose quality, reputation, affinity, then trend
  in that order. Unknown genres use other; affinities never hard-reject.
- [x] Assert tier 1 / quality 84 / effective rep 25 / affinity 1.2 /
  trend 1.3 gives .2482272. Test .02/.95 clamps and iconic neutral factors.
- [x] Implement review delays 48/36/24 hours, thresholds and approval odds.
  Test tier 1 rep 0 threshold 65; threshold +/-20 gives .95/.05; third
  submission is a separate guaranteed-approval branch in the state layer.
- [x] Run the rule tests; no GameState or RNG reference belongs in these
  three rule classes.

### Task 4: Ranking, fanbase, reputation, and cancellation rules

**Depends on:** Task 2.
**Files:** `Rules/RankingRules.cs`, `FanbaseRules.cs`, `ReputationRules.cs`,
`CancellationRules.cs`; matching rule tests.

- [x] Implement section 6 scores, K by tier, rank-factor interpolation and
  fan gains/churn. Test rank 1=3, line=.5, configured bottom=.2, ranks beyond
  bottom, zero fanbase, iconic fixed factor 1.5, and neutral iconic affinity.
- [x] Implement weighted top-three staff reputation with deterministic tie
  ordering and renormalization for one/two staff inputs. NewGame still has
  only Aki. Test effective reputation from track record and staff term.
- [x] Implement fees, protection, chapter contributor gains, penalties and
  ending bonus as pure helpers. Assert the 1996 Tokiwa fee at rep 50 is
  14,500; protection at 300 chapters/500k fans/50 impact is .7.
- [x] Implement warning/cancel/strike clocks, issue-age conversion, strike
  expiry, survival count reductions and cancellation chance. Test P=0 gives
  3/3/8; P=1 gives 12/26/2; intermediate ceiling; exact expiry boundary;
  cancel chance at rep 0=1 and rep 100=.6.
- [x] Keep contextual decisions such as grace and iconic immunity in state
  orchestration, with explicit inputs to these pure helpers.

### Task 5: Sales, trends, and price rules

**Depends on:** Task 2.
**Files:** `Rules/SalesRules.cs`, `TrendRules.cs`, `Economy.cs`; matching tests.

- [x] Implement the exact commercial and doujin copy formulas, separate
  copy flooring from nominal income rounding, and make week/window limits
  explicit inputs. Reject nonfinite/out-of-range conversion inputs.
- [x] Assert 10,000 fans, quality 84, trend 1: commercial week 1 is 7,200
  copies; week 5 is 386. Offline doujin week 1 is 3,840; online at reach .1
  is 4,032. Test subsequent weeks, zero fans, zero quality and exhausted runs.
- [x] Implement normalized genre lookup, baseline interpolation, effective
  clamp .2..1.8, crowding, noise update, fade interpolation and influence
  threshold crossings. Pass sampled noise/floor choices into pure functions.
- [x] Validate all year anchors, before/after curve bounds, leap-year
  fractions, noise clamp, influence clamp and retained-boom addition.
- [x] Assert economy index at GameClock.Start is 1, internet cost 120,000,
  post-2026 growth at 2030-01-01 is `1.18 * 1.02^4`, and release-date pricing
  differs from current-sale-date pricing as intended.
- [x] Run all pure rule tests and the full existing suite.

### Task 6: Version 2 model, initialization, and tick seams

**Depends on:** Tasks 2-5.
**Files:** `Market.cs`, `Model.cs`, `GameState.cs`, `GameState.Serialization.cs`,
`GameEvent.cs`, `EventType.cs`, `Settings.cs`, `Commands.cs`, the four new
GameState partials; initialization/model/event/serialization tests.

- [x] Add the design fields and supporting state table above, using finite
  doubles for game metrics and long for yen/copies/hour aggregates.
- [x] Bump CurrentVersion to 2. Reject v1 with a clear unsupported-version
  error, retain the newer-version error, and update tests to use
  CurrentVersion or CurrentVersion+1 instead of string-replacing 1 -> 2.
- [x] Initialize money to 500,000 with an empty ledger, Aki reputation to 10,
  track record/fans/impact/influence to zero, no internet, one trend per
  genre, and one calendar/roster per magazine. No opening-balance ledger
  entry; reconciliation is `500000 + Sum(Amount)`.
- [x] Generate `RosterSize-1` fillers with globally unique ids, weighted
  genre selection, initial tier ranges and the tier-1 iconic reroll. Consume
  draws in documented catalog/slot/field order. Never use System.Random.
- [x] Append the 24 EventType members in the exact design section 10 order.
  Add the eight pause defaults and nullable event context fields, keeping
  existing Emit call sites valid and missing pause keys off.
- [x] Register all six new command records in JSON polymorphism. Add tick
  seam methods with buildable bodies; filling them is owned by later tasks,
  not considered finished behavior at this checkpoint. The initial market
  seam only captures/advances due calendars for pitch handoff.
- [x] Adopt this order, with the close snapshot passed locally:

  ```text
  Clock.Advance -> capture due closes -> WorkStep -> EditorStep
  -> IssueCloseStep(closes) -> SalesStep -> PitchStep(closes)
  -> RiskStep -> DayEndStep -> midnight StartNewDay
  ```

- [x] Add required-root-field checks and round-trip coverage for new state.
  Full cross-field validation is Task 17, after the state transitions exist.
  Test equal seed initialization, different seeds, resource independence
  from working directory, all id uniqueness, and unchanged RNG on load.

### Task 7: Frozen pages, contributions, and contributor hours

**Depends on:** Task 6.
**Files:** `GameState.Planner.cs`, `GameState.Work.cs`, `GameState.Commands.cs`,
`Model.cs`; `QualityIntegrationTests.cs` and existing work/queue tests.

- [x] Extend CreateNextChapter with explicit optional page/due overrides;
  store Pages and derive HoursRequired once at creation. Do not infer a fee
  from the series' current page setting or divide hours by a balance value.
- [x] Count actual elapsed person-hours in StageWork.HoursByPerson and
  Series.LifetimeHoursByPerson on every worked tick, including overtime.
  Add 1 to stage OvertimeHours when IsWorkingHour reports overtime.
- [x] Freeze contribution when a stage completes. Skip clears current work
  and contribution while preserving already-worked lifetime hours.
- [x] CompleteChapterIfDone computes Quality once, includes it in the event,
  and calls explicit completion hooks owned by Tasks 10-11. Preserve no-op
  behavior on repeat completion calls.
- [x] Test changed page settings affect only future chapters; captured
  contributions survive later skill changes; skipped/redo work accounting;
  and all old pure production time/sequence assertions still hold.

### Task 8: Planner lifecycle and editor gate

**Depends on:** Tasks 6-7.
**Files:** `GameState.Planner.cs`, `GameState.Work.cs`, `GameState.Publishing.cs`,
`GameState.Commands.cs`; `EditorTests.cs`, `PublishingPlannerTests.cs`.

- [x] Implement untouched-draft detection/replacement and reference cleanup.
  Suppress ordinary auto-drafts during Pitching/Offered, keep doujin cadence
  behavior, and implement serialized issue-slot assignment and due overrides.
- [x] Implement editor requirement/context selection and Name submission
  for both normal completion and SkipStage. Resolve NotStarted terminology.
- [x] Make IsStartable and CompleteChapterIfDone honor the gate even when
  Pencils or every stage was skipped. Do not let a blocked cached CurrentTask
  continue working. Rebuild planner state after review transitions.
- [x] Implement EditorStep: due reviews in chapter-id order, one probability
  draw for submissions 1/2, no draw for guaranteed third approval, redo
  reset and events. Connect redo penalties through Task 10's award helpers.
- [x] Test 24/36/48-hour gates, exact-boundary ordering, automatic and skipped
  Name, all-stage skips, max two redos, other-series work while waiting,
  late review causing a missed issue later, and paused series behavior.
- [x] Test accepted existing progress semantics: unfinished Name submits
  later; finished/skipped Name with untouched Pencils submits now; Pencils
  already worked/completed means Approved. Completed historical chapters
  are not retroactively sent to an editor.

### Task 9: Pitches, offers, acceptance, decline, expiry

**Depends on:** Task 8.
**Files:** `GameState.Publishing.cs`, `GameState.Commands.cs`, planner helpers;
`PublishingTests.cs`, `PublishingCommandTests.cs`.

- [x] Add Apply handlers for Pitch/Accept/Decline. Fully validate all state,
  magazine ids and cooldowns before replacing a draft or consuming RNG.
- [x] Create exactly one 31-page one-shot, with its magazine and first
  aligned close >= now+14 days. Pitching/Offered block another pitch.
- [x] Use close contexts to resolve complete, approved one-shots only once,
  at/after their due close. Suppress one-shot DeadlineMissed and strikes.
- [x] Roll chance once; generate a fixed nominal fee offer for the fourth
  future close or set a 26-week rejection cooldown and weakest-factor reason.
  Connect qualifying rejection reputation via Task 10.
- [x] Accept sets Contract including its own id/count/first close, preserves
  lifetime publications, saves doujin cadence, changes cadence, resets
  cancellation state, and sets the next regular chapter's editorial state.
  Decline/expiry marks the sample doujin eligible and resumes planning.
- [x] Expire at `now >= ExpiresAt`; accept at an expired timestamp is invalid.
  Define one-shot archival flags consistently on every exit path.
- [x] Test public-command reachability both immediately after creation and
  after a completion; partial-work rejection; all cooldown boundaries;
  same-tick close handoff; late sample; seeds for success/rejection; pending
  complete pitch save; no duplicate offer after load; and command atomicity
  including RNG, NextId, events, money and timestamped logs.

### Task 10: Reputation application and iconic transitions

**Depends on:** Tasks 7-9.
**Files:** `GameState.Reputation.cs`; completion/editor/pitch hooks;
`ReputationTests.cs`, `IconicTests.cs`.

- [x] Add small helpers to apply/clamp personal, track-record, fanbase,
  impact and influence changes with reasons and typed event context.
- [x] Connect completion shares (half for ordinary doujin, full for pitched
  samples/serialized work), zero-hour behavior, redo penalties and qualifying
  rejected-pitch rewards. Do not award twice during replay or repeated hooks.
- [x] Expose read-only staff term, effective reputation and protection
  queries for later issue handling and Godot; keep their formulas pure.
- [x] Check iconic thresholds after fanbase/impact changes, emit once, and
  clear cancellation state. Gate all later rank/influence effects as defined
  above. Retain non-rank benefits and the normal editor path.
- [x] Test reputation clamps, person-hour shares including redos, zero-hour
  skips, exact threshold crossing in either order, permanence, neutral iconic
  factors and lack of unwanted immunity for merely high-reputation series.

### Task 11: Ledger, doujin volumes, internet, and weekly sales

**Depends on:** Tasks 5-10.
**Files:** `GameState.Sales.cs`, command dispatcher; `LedgerTests.cs`,
`DoujinTests.cs`, `InternetTests.cs`.

- [x] Implement one checked `PostLedger` helper; all money changes pass
  through it. Reasons are exactly chapter fee, royalties, doujin sales,
  internet. Refuse an invalid debit without mutating money or ledger.
- [x] Complete five eligible doujin chapters into one immediate-release
  volume using explicit membership and frozen average quality; repeat for
  additional full groups if eligibility changes release a backlog. Include
  newly rejected/declined/expired samples. Never reuse volume members.
- [x] Apply the quality>=75 release track-record reward exactly once.
  One-shot resolution and withdrawal must invoke the eligibility check too.
- [x] Implement release bookkeeping and Monday doujin sales with live
  fanbase snapshots, 4/8-week windows, nominal covers, 60% income and 30%
  fan gains. Advance zero-copy weeks and honor exhausted windows.
- [x] Add GetOnline command: cost from current price index, once-only
  validation, debit and event, eligible window extension, then weekly word
  of mouth on Unpublished series with a released volume.
- [x] Before first-Monday sales, emit the nonempty previous-month convention
  recap and reset counters. Guard the weekly batch with LastSalesAt.
- [x] Test fifth completion, stock released after pitch rejection, exact
  money reconciliation, no double release on non-Monday ticks, online cost
  and unaffordability, window extension vs exhausted volumes, first-Monday
  ordering, zero copies, and continuation after a save between release/sale.

### Task 12: Issue publication, rankings, fanbase, and filler turnover

**Depends on:** Tasks 8-11.
**Files:** `GameState.Market.cs`, planner/work hooks; `MarketTests.cs`,
`FillerTests.cs`, `PublicationTests.cs`.

- [x] Expand the calendar seam into IssueCloseStep using the captured close
  contexts in catalog order. Process serialized series in ascending id order,
  including paused ones, after their first issue has arrived.
- [x] Publish eligible chapters once, freeze publication identity/time,
  increment lifetime and contract counts, post `Chapter.Pages * FeePerPage`,
  emit ChapterPublished and invoke the volume scheduling hook for Task 14.
- [x] On an unready due chapter emit IssueMissed, shift its issue slot and
  later unpublished slots, apply .97 fan retention and -1 track record;
  record strikes only outside grace and for non-iconic series. Do not also
  emit the old DeadlineMissed event for serialized work.
- [x] Build all ranking scores first, sort deterministically, record chapter
  and series ranks plus LastRanking, and emit one RankingPublished per close.
  A miss clears the current LastRank display rather than showing it as this
  issue's result; historical chapter ranks remain intact.
- [x] Apply fanbase, impact and top-three reputation/influence in the defined
  order, preserving iconic exceptions and first-eight publication grace.
  Cancellation processing is connected in Task 13.
- [x] Drift fillers after rankings. Count below-line appearances, retire at
  12, archive the old record, and replace with 45..65 popularity, a new id,
  title and weighted genre. Iconic fillers drift but never retire.
- [x] Advance calendars/counts once. The monthly trend hook is completed by
  Task 15; do not update it once per player series.
- [x] Test fees use frozen pages/offer price, close-time review outcomes,
  buffering and missed-slot uniqueness, no publication twice, multiple
  player series, absent missed rows, ties/crowding, old rank references after
  retirement, all six schedules, and save/load immediately after a close.

### Task 13: Cancellation, withdrawal, and endings

**Depends on:** Task 12.
**Files:** `GameState.Reputation.cs`, command dispatcher, shared cleanup;
`CancellationTests.cs`, `SeriesLifecycleTests.cs`.

- [x] Apply grace from the pre-publication contract count, age/expire strikes
  in issue intervals, lift safe warnings, and increment below-line clocks.
  A missing row neither lifts a warning nor counts as a ranked safe issue.
- [x] Evaluate combined strike/warning triggers once per issue. Handle
  survival floor-halving and retained warning age, or cancellation cleanup,
  -8 track record, contributor penalties and 52-week magazine cooldown.
- [x] Implement WithdrawSeries: preserve work, restore doujin cadence/editor
  context, turn eligible uncollected work into doujin stock, retain iconic
  fans, otherwise multiply by .9, clear cancellation state and apply the
  clamped table penalty. Preserve completed publication history and volumes.
- [x] Archive contracts on withdrawal, cancellation and serialized ending.
  Current and past contracts retain unique ids; published chapter references
  must resolve to their original contract after switching magazines.
- [x] Implement EndSeries for every non-Ended state, including Paused,
  Pitching and Offered. Clear pitch/offer/contract state, drop unfinished
  work and the current live pitch sample, clean references and emit once.
  This includes a completed sample awaiting its close or offer acceptance.
  Ended is always Unpublished.
- [x] For serialized proper endings (lifetime publications>=12), use the
  current magazine's line, mean of lifetime published chapter ranks, and
  lifetime copies in the stated bonus; distribute personal bonus by lifetime
  hours. Earlier serialized endings use the penalty. Doujin endings are free.
- [x] Invoke final-commercial-volume scheduling with >=3 leftover published
  chapters; Task 14 supplies its implementation. Do not invent a short
  final-doujin-volume rule.
- [x] Test grace on chapter eight vs nine and after a new contract, all three
  magazine cadences, exact clock boundaries, both seeded cancellation
  outcomes, both triggers on one close, warning lift/survival recurrence,
  iconic immunity, cleanup of pins/manual order, repeated-command failures,
  withdrawal/re-pitch, historical contract lookup, and every EndSeries
  publishing state, including completed Pitching and Offered samples.

### Task 14: Tankobon, commercial royalties, and milestones

**Depends on:** Tasks 11-13.
**Files:** `GameState.Sales.cs`, reputation/influence hooks;
`VolumeTests.cs`, `SalesTests.cs`, `MilestoneTests.cs`.

- [x] Collect uncollected published chapters in series publication order,
  using the triggering publication's magazine ChaptersPerVolume for a full
  group. Include earlier-contract leftovers and preserve exact membership.
- [x] Schedule full groups six weeks after their last publication and final
  groups of at least three six weeks after ending/cancellation. Number all
  volumes monotonically within their series, regardless of sales channel.
- [x] Release on/after ReleaseDate exactly once on any tick, then sell on
  Mondays until 52 sales weeks. Apply frozen release-date cover price,
  10% royalty, live per-series fanbase snapshot, and 5% aggregate fan gains.
- [x] Detect threshold crossings from old/new CopiesSold. Emit both events
  if one sale crosses both thresholds; grant +3/+10 track record and
  +.05/+.15 influence as specified, with million influence once per series
  and no iconic influence. Check iconic transitions after metric updates.
- [x] Keep commercial and doujin paths separate. Existing volumes continue
  selling after withdrawal, cancellation or ending; ordinary publication
  cannot collect chapters already in a doujin volume.
- [x] Test six/nine-chapter thresholds, remaining 2 vs 3 chapters, delayed
  release crossing a year/price boundary, Monday release selling that same
  tick, week 52 cutoff, ended back catalog, multiple-volume order invariance,
  collection after a magazine switch, both thresholds in one sale and
  save/load around release/milestone events.

### Task 15: Monthly trends, persistent booms, and influence

**Depends on:** Tasks 10, 12, 14.
**Files:** `GameState.Market.cs`, relevant award hooks;
`TrendStateTests.cs`, `TrendInfluenceTests.cs`.

- [x] At the first catalog-ordered close of a calendar month, update
  LastTrendUpdateMonth once. Later magazines that tick/month do not repeat it.
- [x] In catalog genre order draw monthly noise; draw an inactive boom roll;
  on success draw peak and duration. At fade start draw the floor once and
  emit once; at fade end transfer retained gain and clear the active phase.
  An existing active/fading boom does not also start another in that update.
- [x] Keep saved total Boom in 0..1, effective multiplier in .2..1.8, and
  other noise/boom exactly zero. Apply influence decay only when no series
  in that genre has Publishing==Serialized, including paused contracts.
- [x] Wire top-three quality>=80 and commercial copy milestones through one
  influence helper. Emit a threshold event per upward .1 boundary crossed,
  with a small tolerance for floating-point equality; never emit on decay.
- [x] Test two magazines closing together, missed months in scripted
  advances, known-seed noise/booms, zero-floor fade resuming after load,
  retained gains under later booms, normalization, iconic exclusion and
  repeat upward crossings after decay. No unseeded probabilistic assertion.

### Task 16: Daily recap and boundary integration

**Depends on:** Tasks 9-15.
**Files:** `GameState.cs`, `GameState.Recap.cs`, `GameEvent.cs`, affected
helpers; `PublishingTickTests.cs`, `PublishingRecapTests.cs`.

- [x] Remove every temporary seam body and verify the final tick ordering
  exactly matches the order in Task 6 and design section 10.
- [x] Add YenEarned, ChaptersPublished and IssuesMissed to recap payloads.
  Populate event ActivityDate explicitly from work and command entry points;
  use it with ledger dates so the hour-23 production recap and new-day sales
  cannot be mixed or lost. Adjust both recap and StartNewDay window resets
  to retain new-day boundary events. Preserve started/completed-work behavior.
- [x] Confirm a review, issue close, pitch result, release or sale never
  depends on somebody being at work. Advance(hours) and UI idle skipping
  must process these steps even on days off and while every series is paused.
- [x] Keep HoursUntilNextWork as a schedule query; it is not a replacement
  for processing intervening market events. Do not jump the clock directly.
- [x] Test final Name hour exactly at a close, approval exactly at a close,
  pitch resolution after a moved calendar, sales at Monday midnight after
  hour-23 work, a chapter completed by a midnight command, save/load between
  midnight and the next recap, previous-month convention totals, event pause
  flags, and equivalence of batched vs one-hour-at-a-time Advance.
- [x] Run the full suite. Old scenario deadlines remain doujin production
  checks; add magazine-specific assertions separately rather than reducing
  their thresholds to accommodate unrelated systems.

### Task 17: Complete version 2 save validation and replay

**Depends on:** Task 16.
**Files:** `GameState.Serialization.cs`, optionally a separate validation
partial; `PublishingSerializationTests.cs`, fixture files, old serializers.

- [x] Require every v2 root field plus supporting state. Nullable scheduling
  fields must have a key but may be null when their state allows it.
  Catalog objects and tick-local close contexts are excluded from JSON.
- [x] Validate money with checked long reconciliation; nominal entries have
  valid times/reasons/series ids. Reject nonfinite metrics, duplicate ids,
  missing catalogs/genres, invalid curves/state bounds, and NextId not above
  every current or archived generated id.
- [x] Validate exactly one market/trend per catalog entry, aligned calendars,
  active filler count, archive separation and historical ranking targets.
  Do not reject a just-retired filler still present in LastRanking.
- [x] Validate publishing state, current contract/offer exclusivity, cooldown
  ids, unresolved one-shot count, frozen pages, historical magazine identities,
  archived contract ownership and chapter-to-contract references,
  editor states/times and quality iff chapter Complete. Review requires Name
  done (Complete **or Skipped**), with no subsequent production bypass.
- [x] Adapt the old Complete==IsFinished check: all stages done can still be
  InProgress awaiting required approval. Completion still requires all stages
  done and approval if applicable. Validate chapter numbers as increasing,
  not necessarily contiguous, and clean all references to removed drafts.
- [x] Validate volume membership, frozen mean quality, chapter bounds,
  release/sales-window state, copies and one-time awards;
  validate all counters, contributor references, boom phases and timestamps.
- [x] Validate event ActivityDate at date precision, allowing the preceding
  date for hour-23 work emitted at midnight and the current date for commands
  and market events. Preserve pending recap attribution across load.
- [x] Ensure load validation is read-only: no RNG draws, replanning, events,
  automatic awards or repairs. A bad file never replaces the Godot state.
- [x] Round-trip saves in every publishing/editor phase, including completed
  waiting pitches, all-skipped review, paused serialization, post-retirement,
  interleaved doujin/tankobon membership, and ended series with selling books.
  Mutate each invariant in a valid JSON copy and assert a clear load failure.
- [x] Reject the captured v1 fixture explicitly. Replay all seventeen command
  kinds at saved Time values; test that loading/continuing preserves future
  RNG decisions, ranks, ledger entries, events and resulting JSON.

### Task 18: Two-year scenarios and regression evidence

**Depends on:** Task 17.
**Files:** `PublishingScenarioTests.cs`, `PublishingDeterminismTests.cs`,
test scenario helpers; extend rather than replace `ScenarioTests.cs`.

- [x] Use a bounded test-only seed search once to find representative success,
  rejection, editor-redo and cancellation cases; commit the resulting fixed
  seeds and their command/tick schedules. Do not search seeds every test run
  or introduce production cheats for the Godot demonstration.
- [x] Test a garage doujin studio, purchase internet, produce/sell a doujin
  volume, pitch a 31-page sample to hoshigaku-flowers, accept, publish through
  a first tankobon release and a nonzero royalty. Include another command-
  driven scenario for withdrawal/re-pitch/ending and an adverse cancellation.
- [x] Run two in-game years with intervening save/load checkpoints. Compare
  to uninterrupted and timestamped-command replay runs, including Rng.State,
  catalogs by reference id, all events and full JSON. Verify monetary sums.
- [x] Assert useful scenario outcomes instead of an exact global event count:
  fees and royalties occur, chapters have quality, offers/reviews resolve,
  ranking rows have unique ranks, trends update once/month, and old saves fail.
- [x] Keep the Sub-project 1 skill-80 and skill-50 production guardrails.
  Log diagnostics for failures without relying on date formatting/culture.
- [x] Run `dotnet test tests/MangakaSim.Tests` and a warning-as-error solution
  build. Record actual counts/timings; do not claim success from source review.

### Task 19: Godot publishing and market controls

**Depends on:** Task 18.
**Files:** `godot/DebugMain.cs`, optional `DebugMain.Publishing.cs`, scene
configuration only if needed for layout; no simulation formulas in Godot.

- [x] Add catalog-backed magazine selection and Pitch/Accept/Decline/
  Withdraw/End buttons using commands. Show useful validation messages;
  command errors must leave the existing UI/game usable.
- [x] Show series publishing/contract state, frozen fee, editor wait/redo,
  last quality and rank/line, fanbase, impact, strikes and warning.
- [x] Show the selected magazine's ranking with highlighted player rows,
  volumes and sales counts, ledger tail/balance/index, and Get Online.
  Add studio track record/staff/effective reputation and each genre's
  effective trend/influence using read-only sim queries.
- [x] Use scrollable panels or tabs rather than squeezing another fixed
  column into the current 1600x900 layout. Keep current form inputs stable
  while time advances and preserve selected series/magazine when refreshing.
- [x] Extend recap formatting and event context display. Refresh all new
  controls after loading, including a pending offer/editor decision.
- [x] Keep shared ScanEvents for commands and ticks, exact-hour auto-pause,
  and the restored Continue action. UI disabling may help discovery, but
  simulation validation remains authoritative.
- [x] Build with zero warnings. Do not claim a rendered test from a headless
  import; the actual scene checks and screenshot are the next task.

### Task 20: Godot walkthrough, final verification, and handoff

**Depends on:** Task 19.
**Files:** `godot/DebugMain.SmokeTest.cs` (optional second partial for the new
scenario), `README.md`, roadmap/design/plan status, new
`docs/superpowers/sub-project-2-completion.md`.

- [x] Retain the old control walkthrough with ids/version expectations
  adapted to v2. Add a fixed-seed public-command publishing walkthrough:
  first doujin completion -> pitch -> offer -> accept -> publication ->
  released commercial volume with nonzero sales -> save/load validation.
  Assert ledger reasons include chapter fee and royalties.
- [x] Drive editor/pitch results through real ticks and UI event scanning.
  Bound the run (e.g. two years plus a fixed allowance), fail with the last
  relevant events on timeout, and handle recap/offer pauses deliberately.
  Never set a finished volume or successful pitch directly in this walkthrough.
- [x] Add focused UI checks for each new button, failed commands, offer
  expiry, reload while review is pending, exact auto-pause boundaries,
  overnight market events and version-1 load rejection preserving the game.
- [x] Use separate test saves under TestResults. Run headless import/startup,
  the automated controls, then a rendered capture showing the new market
  data and inspect text, scrolling, columns, selection and clipping.
- [x] Run the full simulation suite and warning-as-error solution build.
  Review the diff and run `git diff --check`. Confirm no Godot dependency in
  the simulation and no build caches/screenshots/test saves staged.
- [x] Update README launch/validation instructions, state actual evidence
  and any limits in completion notes, and mark only verified tasks complete.
  Record which plan decisions changed, if any, with their regression tests.
- [x] Commit the reviewed milestone locally if implementation was authorized
  with the plan's commit workflow. Stop after Sub-project 2; do not begin
  staffing/studio work or install tools for later milestones.

## Verification commands

Run from the repository root in PowerShell. Use filtered tests during each
task; run the complete suite at the integration and final checkpoints.

```powershell
dotnet test tests/MangakaSim.Tests
dotnet build MangakaGame.sln -warnaserror

$godot = Join-Path $env:LOCALAPPDATA 'Microsoft\WinGet\Packages\GodotEngine.GodotEngine.Mono_Microsoft.Winget.Source_8wekyb3d8bbwe\Godot_v4.7.2-stable_mono_win64\Godot_v4.7.2-stable_mono_win64_console.exe'
& $godot --headless --editor --path godot --import --quit
& $godot --headless --path godot -- --smoke-test
& $godot --path godot -- --smoke-test --capture

git diff --check
```

Confirm the editor path/version at implementation time. Dependency restore
may need network permission; report genuine environment failures distinctly
from failing game behavior. An editor import alone is not proof that the
new commands or simulation loop work.

## Coverage map

| Design section | Owning tasks |
|---|---|
| 1. Data model | 6-7, 17 |
| 2. Catalog, fillers and issue calendars | 2, 6, 12 |
| 3. Genre trends | 5, 10, 12, 14-15 |
| 4. Pitching, offers, iconic status | 8-10 |
| 5. Editor gate and quality | 3, 7-8 |
| 6. Publication, ranking, fans | 4, 12 |
| 7. Volumes, commercial sales, ledger | 5, 11, 14 |
| 8. Doujin and internet | 5, 11 |
| 9. Reputation, cancellation, endings | 4, 10, 13 |
| 10. Events, pauses and tick order | 6, 16, 19-20 |
| 11. Commands, v2 saves and prices | 5-6, 9, 11, 13, 17 |
| 12. Debug scene | 19-20 |
| 13. Tests and smoke verification | 1-20, with full scenarios in 18 |

The milestone is complete only when the user can follow the doujin-to-
serialization flow in the debug scene, money and rankings have a tested
source, v2 saves continue deterministically, and the evidence above has
actually been collected. Writing this plan does not satisfy those checks.
