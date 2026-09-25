# Sub-project 5 implementation plan

Date: 2026-09-24.
Status: implemented and verified locally on 2026-09-24. Numerical balance
values are initial gameplay estimates, subject to playtesting. See the
[delivery record](../sub-project-5-completion.md) for final defaults and evidence.
Design: [historical timeline and rivals](../specs/2026-09-24-historical-timeline-design.md).
Decisions: [confirmed choices](../specs/2026-09-24-historical-timeline-considerations.md).
Evidence: [research ledger](../specs/2026-09-24-historical-timeline-research.md).

## Starting point (before implementation)

The working tree already contains implemented Sub-projects 3 and 4 alongside
earlier publishing changes. Preserve them. `GameState.CurrentVersion` is 4;
magazine filler rankings, creator departures, separate accounts, office desk
reservations, background businesses and explicit version-3 import already exist.
The roadmap's international-interest placeholder has no corresponding current
state field. Do not assume it is an implemented feature.

Use engine-free simulation changes in `src/MangakaSim`, meaningful domain tests
in `tests/MangakaSim.Tests`, and the existing Godot harness for usable controls.
No external runtime service or new package is required. Build sequential slices
and preserve the user's confirmed choices when adjusting technical details.
Record any material balance change from the draft as a proposed default change.

## Delivery checklist

- [x] 1. Establish baseline and complete the production content ledger.
- [x] 2. Add validated timeline data, state and deterministic dispatch.
- [x] 3. Integrate historical rivals with rankings and demand.
- [x] 4. Add temporary historical assistants and mentoring.
- [x] 5. Add funded ordinary rival studios and shared transfer rules.
- [x] 6. Add reciprocal recruitment, retention and protagonist offers.
- [x] 7. Add industry unlocks, channel decisions and attributed receipts.
- [x] 8. Add the simulated future and long-run limits.
- [x] 9. Complete version-5 persistence and explicit checkpoint import.
- [x] 10. Deliver usable controls, integration checks and completion notes.

## 1. Baseline and content acceptance

Read current repository guidance and inspect changed files before edits. Run
the existing domain suite and warning-as-error build once; record new evidence
rather than copying Sub-project 4's counts. Verify the installed Godot .NET
engine and existing smoke-test entry points when beginning UI work.

Use the confirmed focused roster of thirteen historical rivals and two temporary
creator opportunities; defer expansion until the systems are working. Turn the
initial roster into a reviewed content table. For every
entry record launch, relevant protected milestones, pre-2026 terminal state,
historical medium, fictional magazine mapping, supported date precision and
direct primary-source URLs. Complete the missing ending evidence called out in
the ledger. Do not infer a serialization ending from a final volume release.
Confirm cutoff status for titles with no ending. Coarse supported dates are
acceptable under the design's labeled approximation policy.

Check both assistant opportunities against earlier career commitments; preserve
the distinction between a researched career anchor and an invented job opening.
Do not imply that a real creator was available for hire by the player. Keep
adaptation news to individually sourced events. Validate proposed genre keys
against `trends.json`, and historical peak occupancy against `publishers.json`.

Gate: no accepted event lacks a source/precision or explicit fictional label;
no title with a known historical ending continues through 2025 by omission.
The research ledger itself remains a record of evidence, not runtime game data.

## 2. Timeline catalog and hourly scheduler

Likely additions: `Catalog/TimelineCatalog.cs`, `Data/timeline.json`,
`Timeline.cs`, `GameState.Timeline.cs`, `Rules/TimelineRules.cs` and focused
timeline tests. Use the existing embedded JSON conventions.

Add typed events, stable source/content IDs, catalog revision, phase records,
processed-event tracking and saved dedicated RNG streams. Implement historical
initialization as of a supplied clock without historical money transactions.
Define event priorities and exact work-interval semantics at tick boundaries.
Dispatch from normal whole-hour advance, including hours advanced by idle skip.
No separate Godot timer for historical or hiring events.

Checks: before/at/after boundaries; simultaneous events; leap years; coarse-date
ordering; once-only processing; late checkpoint initialization; invalid catalog
references and impossible creator overlap. Compare N single ticks with one
`Advance(N)` call. Prove that opening a snapshot consumes no random draws.

## 3. Rivals, ranking and demand

Extend `FillerSeries` with explicit rival linkage and lifecycle state. Update
`GameState.Market.cs`, market validation and ranking snapshots. Reuse existing
IDs and generated replacement rules; protect historical records only for their
scripted period. Archived entries retain identity for news and past rankings.

Add pure rivalry-demand functions and the one-time application point in
commercial and doujin demand. Inspect `GameState.Printing.cs` and stock sales
as well as `GameState.Sales.cs`; the current doujin branch recalculates demand,
so changing an unused preliminary copy count is insufficient. Avoid duplicating
the effect through genre popularity, rankings or fan growth.

Checks: historical launch replaces a filler without changing player contracts;
all six magazine templates preserve capacity; strongest player can rank first;
historical title survives poor rank then ends at the scripted boundary; hiatus
returns the same identity; demand examples in the design hold; all stacked
effects stay bounded; physical copies never exceed stock. Verify multiple rival
launches at the same hour and stable ordering on save/load.

## 4. Temporary assistants

Implement discovery through targeted recruitment, staff connections and occasional
ordinary-pool appearances, without automatically revealing every open window.
Integrate creator opportunities with studio recruitment, person identity,
employment, teams, skill development and office reservations. Reject all lead
assignment paths, including automatic project proposals and planner fallbacks.
Apply mentoring only to eligible earned skill increments, with a daily cap.
Use promising but uneven starting profiles; historical identity alone must not
grant the prodigy trait. Keep specific skill values labeled as game estimates.
Preserve relationship-dependent professional contacts after departure, enabling
occasional staff introductions or industry opportunities. Define and document
bounded event frequency and eligibility before implementing this contact layer.
Persist contact identity and relationship state independently of employment;
verify that contact events neither rehire the creator nor alter protected history.
Implement hiring expiry, departure warnings, pending-start cancellation and
fixed departure before conflicting historical obligations.

Checks: no early discovery/future fame leak; deadline shown before hiring;
no duplicate creator after pool refresh or load; required wages/desks/authority;
no start on departure date; no post-departure work; no lead/title transfer;
contribution records survive; mentoring requires shared attendance and does not
stack; early dismissal or welfare departure does not cancel the later milestone.

## 5. Rival businesses and transfer foundation

Seed three fictional ordinary studios using existing accounts, properties,
people, employment and office state. Preserve the original generated location
stream for the player. Add bounded monthly commission receipts, genuine expenses
and cash-backed hiring decisions. Record rival capital and commission income
separately from commercial royalties and player transfers.

Factor current creator departure and future-title transfer code into a shared
transaction that accepts a known destination. Do not call the branch that
creates a fresh studio when a rival has already agreed to hire someone. Inspect
rights-release commands and follower movement for equivalent assumptions.

Checks: unique IDs/locations, valid usable desks, no invisible salary subsidy,
exactly-once monthly income and expenses, cash conservation for transfers,
both rights settings, pending royalties and arrears, released versus withheld
future rights, and unchanged past chapter/stock/volume ownership. Rival closure
and the protagonist leaving a business must release affected reservations.

## 6. Staff approaches and retention

Add public rival-staff profiles and scouting reports: public reputation and
specialties first, clearer current skills and salary expectations after scouting.
Keep actual loyalty, willingness and acceptance draws out of player-facing
snapshots. Define report timing, cost and freshness before implementation;
persist discovered information and verify that viewing it consumes no RNG.

Add saved offers, target/cooldown indexes, quoted terms, stored decision draws,
deadlines and explicit reservation lifecycles. Implement weekly inbound checks,
the seven-day retention window, salary responses, player approaches and ordinary
staff transfers using step 5's shared operation. Factor a pure acceptance rule
with documented normalization for salary, happiness and loyalty.

Track mild studio-relationship consequences of repeated successful poaching.
Support optional rival retention or recruitment responses with bounded frequency
and real funding. Define relationship recovery and response strength as balance
defaults. Verify that failed approaches do not count as hires, responses cannot
create automatic staff transfers, and save/load does not duplicate consequences.

Protagonist offers produce a career quote using the actual destination business;
do not silently generate a different employer. Continue only when the player
chooses. Revalidate follower seats and destination acceptance through existing
career rules. Employer-controlled salary/recruitment proposals need a bounded
approval outcome and quoted scope, never unrestricted business control.

Checks: bidirectional success/failure, salary and loyalty effects, single global
negotiation per person, cooldowns, stored roll unchanged after save/load or term
changes, desk removed during negotiation, funds spent elsewhere, business or
career change, destination closure, deadline crossing in idle skip, no player
auto-departure, and no free repeat approaches. Every failed transaction leaves
staff, rights, funds and reservations consistent.

## 7. Industry choices and channel economics

Use one combined overseas market, with distinct interest, licensed sales and
income records. Keep a stable market identifier to support later regional
expansion. Verify domestic/overseas separation through sales reports, save/load
and contributor accounting; do not add regional preferences or licensing
economies in this milestone.

Implement publisher approval based on audience demand, title performance and
publisher priorities in addition to basic rights, channel and funding checks.
Return actionable refusal reasons derived from the actual decision, and support
later retries as circumstances improve. Verify both affordable-but-refused and
improved-prospects cases without exposing the stored decision draw. Keep direct
owner-controlled doujin releases separate from publisher approval.

Add dated capability unlocks and fictional-publisher eligibility. Add explicit
title/channel proposals, seven-day outcomes, retry cooldowns, adoption state,
edition ownership and international interest. Retain existing early internet
and physical doujin behavior. Use the design's costs and caps as initial game
estimates, visible in quotes; do not present them as historical Japanese fees.

Introduce one demand calculation followed by domestic physical/digital splitting
and a separate bounded overseas demand pool. Digital units do not use printed
stock. Preserve per-volume business and contributor attribution, royalty rules,
sales-window closure and idempotent weekly processing. New channels cannot
resurrect closed volumes or grant rights to old editions after a creator moves.

Checks: before-unlock rejection, eligible publisher only, owner versus employed
authority, setup fee once, denial/no adoption, localization and first readership,
non-adopter tradeoff, physical stock limit, distinct digital/overseas receipts,
no duplicated units, no double contributor payments, retired/cancelled titles,
transferred future rights with old stock still owned by the original business,
and no channel replay on load. Piracy numeric loss stays zero by default.

## 8. Simulated future

Add a once-only cutoff transition and visibly fictional future news. Convert
only ongoing historical market records to future lifecycle eligibility; do not
resurrect ended titles or replay assistant openings. Implement bounded quarterly
launch/end/trend events and per-title cooldowns using the dedicated future RNG.
Keep staff and active rival populations capped. Archive inactive state without
breaking IDs referenced by past events, contracts, rights or ledgers.

Checks: 2025-12-31 to 2026-01-01 boundary, game already beyond cutoff on import,
no actual 2026 facts admitted as history, same-seed reproducibility, ongoing
analogue can end fictionally, continued player play without forced completion,
finite/capped modifiers and no repeated permanent unlock accumulation.

Run a representative 1996–2036 simulation or equivalent controlled long-run
fixture. Measure hourly workload and save growth rather than assuming that
market-only records and nine initial rival staff are cheap enough. Optimize
only if measured growth or runtime warrants it.

## 9. Persistence and explicit import

Introduce version-5 required fields and validation while each stateful slice is
built; finish end-to-end coverage here. Include pending proposal reservations,
decision draws, historical linkage, former-assistant contacts, scouting knowledge,
studio relationships and response cooldowns, news visibility, acknowledged pause
alerts, future flags, channel ownership and all RNGs. Validate enum values,
ID uniqueness, event provenance,
finite bounded factors, legal phase transitions and supported catalog revision.

Implement an explicit version-4 checkpoint converter from a validated old save.
Retain a validated old-schema boundary rather than deserializing directly into
new required fields with silent defaults. Preserve the original save, previous
RNG state and historical commands; establish a new replay checkpoint. Compose
the existing completed-version-3 conversion through a validated version-4
intermediate. Do not make its current `CurrentVersion` assignment accidentally
skip timeline initialization after the version constant changes.

Checks: fresh round trip; pending hire/poach/channel decisions; fixed departure;
just before and after cutoff; missing fields, bad IDs and future schema rejection;
v4 conversion during early and late history; v3 office conversion still valid;
money/stock/title/furniture unchanged on import; no retroactive income; old news
not replayed; and checkpoint replay equals continuing the converted state.

## 10. Controls, integrated verification and delivery

Likely UI additions: `godot/DebugMain.Timeline.cs` and timeline smoke scenarios.
Provide current industry news, rival rankings/profile, assistant cards, staff
approach/retention actions and channel proposals. Reuse existing command error,
selection, save/load, pause and recap handling. Keep all future knowledge out of
ordinary cards and give clear unavailable-action reasons.

Integrate actionable events with configurable auto-pause and idle-skip stop
conditions. Default to pausing for decisions and urgent staff deadline warnings;
ordinary industry news and historical milestones without decisions belong in
the feed and daily recap without pausing. Verify warnings stop idle skip before
the response expires, simultaneous alerts produce one pause, and acknowledged
alerts do not repeatedly pause on resume or reload. Preserve deterministic
headless advance and keep real-time pausing in the Godot driver. Confirm staff moves
appear in the office through the established activity/appearance snapshots;
no separate visual employee or reset of the approved hyperlapse/door behavior.

Run focused tests while implementing, then the full domain suite, solution
warning-as-error build, Godot import, headless smoke and rendered walkthrough.
Use controlled dated fixtures for visual checks rather than making the user
wait thirty game years. Exercise a launch, temporary hire/departure, retention,
rival recruitment, digital proposal, save/load and cutoff at normal and 8x
speed. Inspect text layout and disabled actions as well as command outcomes.

Record the exact checks and any limitations in
`docs/superpowers/sub-project-5-completion.md`. Update roadmap, README, decisions
and this checklist only to the status actually reached. Keep researched dates,
fictional assumptions, automated results and rendered observations distinct.
No completion claim based only on compiling or on an unchecked content table.
