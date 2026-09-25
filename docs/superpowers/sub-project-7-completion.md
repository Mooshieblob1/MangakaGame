# Sub-project 7 delivery record

Implemented locally on 24 September 2026 following the user's “implement”.
Earlier uncommitted sub-project work was preserved. No commit or push was requested.

## Playable entry points

- **Industry → Awards & contests:** produce a 32-page Story or Comedy one-shot,
  adopt eligible unpublished work, submit, read results, revise, or release it
  for ordinary publishing. A completed released entry becomes a standalone
  print-ready doujin master. Active entries remain reserved until judging.
- **Industry → Adaptations & merchandise:** pitch partners, compare incoming
  offers, counter twice, seek employer approval where required, sign, choose
  creator involvement, respond to production problems and inspect receipts.
  Series details link to both views. Pending license decisions remain in Inbox
  even when their notification has been read.
- **Industry → Career journal:** permanent honors, readership and publication
  milestones, with Helper-Chan celebrations and visible achievement eligibility.
- **New Career / Settings:** Relaxed, Standard, Challenging, bounded Custom,
  Sandbox and nine independent assists. Starting funds are configurable only at
  setup; ongoing costs/grace and assists can change later.
- **Helper-Chan:** the original supplied desk image now accompanies a natural
  everyday conversation. Both answers, remembered callbacks, defer, skip and
  read-only illustrated journal replay work. There is no manual desk trigger or
  gameplay bonus. The chibi office cast retains its existing proportions.

## Final prototype rules

These are fictional balancing choices, not asserted Japanese prices or contract
standards. The [research notes](specs/2026-09-24-awards-adaptations-research.md)
explain the official Japanese references and historical evidence limits.

Contests close March 31 and September 30; results arrive two months later.
Each category/edition freezes 24 simulated competitor scores and every player
entry. Judging is 60% craft, 25% originality, 15% fit, with at most five points
of jury variation. Winner, runner-up and honorable-mention floors are 80/72/65;
their personal prizes are ¥300,000/¥100,000/¥30,000. Prior winners cannot reuse
the work. Failed entries require a newly produced revision and a later edition.
Winning recognition adds ten percentage points to the ordinary publisher-pitch
chance for a year, capped at 95%; acceptance and production still use the
existing editorial workflow.

Published work from the previous year is considered each January 1, with a
quality floor of 65 and results January 15. The annual winner receives ¥500,000.
Honors persist while discovery effects expire. Award sales lift is capped at
25%, all recognition/adaptation sales lift at 50%, and new cultural impact at
10 per series/year. Repeat wins and later anime seasons have diminishing returns.
Iconic status still requires 90 impact and a million readers.

Three animation and three merchandise partner profiles have specialties and
different reliability. Offers last 30 days, pitching takes two creator hours,
and retries wait 90 days. A partner can operate three active projects. Each
series/category has one active license. Signing pays 20%, release pays 80%, and
the creator's frozen 20–40% share becomes a monthly business obligation.
The business and creator named in the signed deal keep those entitlements after
a move. Negotiations have two rounds with explicit concessions and possible
withdrawal. Merchandise partners own production and inventory; the game's
monthly receipts represent license income, not total retail sales.

Anime pre-production takes eight weeks; initial production quotes are 26, 39
or 52 weeks. Consultation consumes actual scheduled manga time: Monday/Thursday
for occasional consultation, or up to six first work-hours per week for close
supervision. Staff are not double-booked. Source shortages permit a smaller
faithful season, side stories or an original ending where approval rights allow.
Original endings have wider reception variation; shorter seasons reach fewer
viewers. Unanswered decisions use conservative defaults after 14 days. Delays
and rare cancellations preserve the signing payment but cannot pay the release
balance twice. Follow-up seasons need a new agreement and new published source.
Anime reception never overwrites manga quality. Severe spillover is capped at
five outstanding reputation points per creator and recovers after 182 days.

Merchandise preparation takes four weeks, production initially twelve weeks,
followed by a one-year licensing term with monthly receipts. The three categories
are figures, clothing and stationery, with no anime prerequisite.

Difficulty scales only new paid recruitment and digital/overseas setup costs
(0.85/1/1.15) and unpaid-rent closure grace (1.5/1/0.75). Prices shown in controls
come from the same quote functions as charges. Signed salaries, rent, loans,
ownership, protagonist skills and the parents' free house stay intact. Custom
opening funds independently choose half/standard/double the normal cushion.

Sandbox funds supply exact shortfalls through labeled ledger entries. Financial
charts and the profitability challenge exclude subsidies, loans and capital
transfers, while including creator-share payments. Instant production marks
synthetic completion explicitly, preserving real prior hours without inventing
labor or XP. Editor review and award dates still run normally. The delivery
assist covers print timers; leases and office construction were already immediate.
Progression unlocks retain price, capacity, layout and authority checks. Future
technology is independent: it grants access without firing dated news or changing
the global historical market. Signed early digital agreements keep operating
when the option is later disabled. Stress assistance protects morale while
needs and unpaid obligations remain; deadline assistance prevents missed-issue
penalties, not poor-ranking cancellation.

## Achievement and save boundary

Version-7 saves require progression state, settings history and Sandbox
provenance. Supported v3–v6 imports preserve originals and establish a new
deterministic replay checkpoint. Unknown legacy tuning is not taken as verified
ordinary achievement provenance.

Selecting Sandbox with no assists, or activating any assist in any mode,
permanently disables new platform achievements for that snapshot and its
descendants. Pending platform evidence is cleared, in-game honors remain, and
disabling assists cannot restore eligibility. Clean earlier snapshots and
independent ordinary careers remain eligible. Nothing resets existing account
achievements. This enforces the game's policy; it is not an anti-tamper system
for externally edited save files.

`ProgressionCatalog.Achievements` defines ten stable `MKG_*` API names and exact
predicates. `AchievementDelivery` checks the originating state before every
delivery. Session deduplication is separate from persisted evidence; a future
platform adapter must support idempotent retries and reset session deduplication
when the signed-in account changes. The Godot build is wired to the no-op sink.
There is no raw stat-upload path that could bypass eligibility.

**Steam release work remains:** configure a real app ID and achievement dashboard,
choose/install the native adapter, supply achievement icons, and verify actual
unlock, offline retry, restart and account-switch behavior. Local recording-sink
tests establish policy, not live Steam delivery.

## Requirement-to-check map

The [52 progression cases](../../tests/MangakaSim.Tests/ProgressionTests.cs) extend
the earlier simulation and Godot regression checks.

| Confirmed choices | Implementation and evidence |
| --- | --- |
| 1–2: award types and judging | Separate newcomer/annual editions; quality-first scoring and calendar tests. |
| 3: production/adoption | Ordinary stages; immutable submission; pitch-sample separation; instant-work provenance. |
| 4–5: prizes and feedback | Personal one-time payments, editorial comments, publisher pitch interest; winner/duplicate payout checks. |
| 6: retry | Actual revision work; one active entry; later-edition and prior-prize guards. |
| 7–8: annual honors | Published-period snapshots; automatic January cycle; capped/expiring effects. |
| 9: opportunities | Incoming monthly interest and player pitches share authority, capacity, cooldown and source checks. |
| 10: involvement | Scheduled creator time; paired manga-work test proves the opportunity cost. |
| 11: offer comparison | Rendered offer screen; quoted payment, creator share, rights, fit and schedule. |
| 12–13: setbacks/catch-up | Delay/default/cancellation settlement; contractual-rights test and actual UI catch-up controls. |
| 14: separate reception | Manga quality unchanged; actual limited reputation loss recovered once after 182 days. |
| 15: follow-ups | New season/terms, source-use accounting, performance-influenced interest and exclusive active category. |
| 16: merchandise | Manga-only release and complete-term receipt/expiry tests. |
| 17–18: milestones/impact | Persistent records, bounded effects, existing iconic thresholds; Sandbox journal UI verification. |
| 19–20: difficulty | Same prodigy skills; one-time independent cushion; prospective costs/grace; six preset/seed cases covering both career routes. |
| 21–22: assists/era | All nine toggles; account separation, price/authority, synthetic work, print delivery, morale and deadline tests; early digital contract continuation. |
| 23–24: achievements | Mode-only and every-toggle permanence, reload/import, clean snapshot, delivery deduplication and originating-state gating. |
| 25: finances | Personal prizes, business receipts, creator obligations, frozen parties across a home move and full merchandise term. |
| 26–27: illustration | Natural scheduler test; full supplied image at 1600×900 and 1280×720; real choice/defer/replay/save controls. |
| 28: counteroffers | Two rounds, concessions, saved responses, rejected-command atomicity and employer approval. |

## Verification

- 496 short simulation tests passed, including all 52 new progression cases.
- Forty-year test passed separately: 145.85 simulated-loop seconds, 65,235,860
  JSON characters, 25,799 events, 10 people and 225 timeline news items. The prior
  SP6 record was 158.41 seconds and 65,234,968 characters; timings are observations,
  not a controlled performance improvement claim. Long-career save size remains
  a future optimization concern, primarily from pre-existing historical ledgers.
- Godot build passed with warnings treated as errors.
- Existing full Godot harness: 758 headless / 823 rendered checks passed.
- Existing management harness: 56 headless / 70 rendered checks passed.
- New progression harness: 15 headless / 24 rendered checks passed. It verifies
  actual controls and supplies screenshots in
  `TestResults/progression-*.avif`, including the complete illustration at 720p.
- RTX 5070 rendered management fixture, 32 staff plus six ambient actors and
  Helper-Chan at 8×: median 7.92 ms, p95 9.77 ms. Full office harness: 6.84/8.84 ms.

The screenshots and rendered checks use controlled fixtures to expose distant
award/production states. Calendar behavior, normal production, money, migration,
replay and no-luck career routes are verified separately in simulation tests.
Balance remains prototype tuning rather than evidence from extended human play.

Logs are under `TestResults/progression-*.log`; the long-run measurement is in
`tests/MangakaSim.Tests/bin/Debug/net8.0/TestResults/timeline-long-run.txt`.
Run the new scene check with Godot `--path godot -- --progression-smoke --capture`
or `--headless --path godot -- --progression-smoke`.
