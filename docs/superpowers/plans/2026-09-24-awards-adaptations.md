# Sub-project 7 implementation plan

Date: 2026-09-24.
Status: implemented locally. Results, final tuning and release limitations are in
the [delivery record](../sub-project-7-completion.md). The sequence below is the
original implementation plan, retained for traceability.
Design: [awards, adaptations and career progression](../specs/2026-09-24-awards-adaptations-design.md).
Requirements: [28 confirmed choices](../specs/2026-09-24-awards-adaptations-considerations.md).
Sources: [research notes](../specs/2026-09-24-awards-adaptations-research.md).

## Starting point and delivery boundary

The game launches `godot/ManagementMain.tscn`, backed by the `DebugMain`
partials. `MangakaSim` owns command-driven simulation and version-6 saves.
`CareerStore` provides career storage, `Career` owns narrative/report state,
and `TimelineWorld` handles dated industry behavior. Significant prior work is
uncommitted. Inspect and preserve it; stage or commit only when requested.

This milestone delivers the game systems, presentation, achievement catalog and
eligibility/adapter boundary. A real Steam application, published dashboard
definitions and live account verification are release prerequisites, not facts
already established by local simulation tests. Do not block engine work on a
missing Steam app ID, and do not claim a mock unlock proves platform delivery.

## 1. Inventory, catalogs and baseline

Inspect current repository guidance, diffs and completed SP3–6 evidence. Establish
a fresh build and simulation baseline once implementation begins. Reuse existing
test helpers and rendered smoke entry points; avoid rewriting earlier systems.

Trace one-shot creation/pitch resolution, chapter completion, rights handover,
royalty accrual, every affordability check, deadline consequences, construction
timers, era gates, career export and replay. Produce a short action-to-handler
inventory for the new controls. In particular, existing `IsOneShot` queries must
not mistake contest manuscripts for publishing pitch samples.

Create a versioned catalog for fictional awards/editions, partner profiles,
offer templates, milestones, achievement IDs and difficulty tuning. Validate
references, calendar consistency, bounds and era validity. Record evidence for
historically named content using the research policy. Current Japanese rules
must not silently become 1996 rules.

Gate: every new player action has a simulation owner, and all provisional
numbers are identifiable as game tuning. No runtime AI service is required.

## 2. State, commands, save migration and eligibility

Add the new models and typed commands in the engine-free project. Extend command
serialization, event context and validation. Add independent saved award and
licensing RNG streams; queries remain read-only. Introduce version 7 and an
explicit v6 import, composing earlier supported imports and preserving originals.

Implement immutable manuscript/entry snapshots, license beneficiaries and
settlement identities before payout logic. Specify chronological tick ordering.
Add difficulty settings and monotonic Sandbox eligibility before any assists
are reachable. Persist settings history, first activation and import provenance.
Update `CareerStore` manifests/import/export where necessary without duplicating
simulation ownership of eligibility.

Checks: missing roots, invalid values, bad references, duplicate records, future
dates, tampered active states, legacy conversion, preservation of original files,
saved RNG, command rejection with no mutation, export round trip and replay.
Missing eligibility is never interpreted as proof that a save is ordinary.

Gate: an empty SP7 state round-trips and imported ordinary play continues with
its previous finances, rights, pending scenes and timeline intact.

## 3. Contests and annual awards

Extend production with a distinct contest-manuscript purpose and revision
history while reusing stage work, staff costs and quality calculation. Add
create/revise/submit commands and previews. Freeze submitted work and prohibit
conflicting publication or active entries. Eligibility belongs to the edition,
not a single global assumption about all newcomer contests.

Build stable simulated fields, jury profiles, bounded variation, placement
floors, tiered rewards and contextual feedback. Add publisher-interest follow-ups
through the existing publishing offer flow. Settle personal prize payments once.
Then implement automatic annual consideration and frozen nomination/results.

Route persistent honors, temporary discovery/sales effects and bounded cultural
impact through explicit effect records. Reuse `CheckIconic`, avoiding duplicate
milestone bonuses or changes to its existing thresholds/protection.

Checks: small excellent work versus popular weak work, award-specific fit,
deadline boundaries, no prize below a floor, no guaranteed serial contract,
revision required for retry, changed-after-submission immunity, duplicate payout,
annual eligibility, repeat-win restrictions and effect expiry after reload.

Gate: a complete contest cycle and an annual award resolve deterministically
with reconcilable receipts and meaningful feedback.

## 4. Licensing, authority and counteroffers

Implement shared offers, expiration, finite negotiation rounds, signatures,
scope/exclusivity and contract settlement. Expose forecasts separately from
promised terms. Incoming opportunities and player pitches share cooldown and
capacity accounting. Require creator time for pitches where configured.

Add simple counteroffers for payment, rights and schedule. Store response state;
no re-opening dialog or repeated invalid commands may reroll. Respect owner,
employed-lead and rights-holder authority separately. Preview beneficiary splits
and record them at agreement. Define creator-share accrual without changing the
existing publishing royalty contract or double-counting business expenses.

Checks: stale offers, expired terms, unaffordable optional costs, invalid employer
authority, concessions/withdrawals, rounding, partial obligations, multiple
beneficiaries, overlapping rights, creator departure and old-business settlement.

Gate: a valid deal survives a studio move and its receipts reconcile with both
the business and creator accounts.

## 5. Anime and merchandise lifecycles

Add anime pre-production, production, release and completion, plus delays,
creative disputes and cancellation. Reserve consultation/supervision hours
through the normal schedule so they actually compete with manga work. Provide
warnings and contract-permitted choices without automatic unauthorized spending.

Track source material and supported stopping points. Implement wait/end-season,
original side stories and original continuation/ending only when feasible.
Separate anime reception from manga quality. Apply bounded discovery/backlist
effects and recoverable spillover; existing iconic status and authored chapter
quality remain intact. Follow-up seasons create new proposals and respect prior
licenses and source use.

Merchandise uses the common licensing flow with partner-controlled inventory,
category demand, receipt settlement and term expiry. Ensure manga-only licenses
work and license income is not represented as full retail turnover.

Checks: workload tradeoffs, partner capacity, all catch-up routes, unanswered
decisions, cancellation settlement, poor-but-visible anime outcomes, successful
renewal without automatic sequel, partner changes, term expiry and no infinite
fans/cash through repeated deals.

Gate: at least one successful and one troubled anime project and one manga-only
merchandise deal are playable through the complete lifecycle.

## 6. Difficulty and configurable Sandbox

Implement presets and bounded Custom settings with Standard preserving the
baseline. Keep starting funds and title ownership separate from ongoing options.
Difficulty must not alter protagonist starting skills or prodigy potential.
Define exactly which fictional costs and recovery windows each modifier affects;
signed obligations cannot be retroactively changed by switching difficulty.

Implement Sandbox toggles through a common policy layer, not scattered UI-only
shortcuts. Inventory every affordability and completion path. Unlimited accounts
receive labeled, finite subsidies that preserve ledger reconciliation and are
excluded from operating income/profit. Instant completion uses normal finish
hooks once and has finite work limits. Do not simulate impossible hours or award
XP repeatedly when there is no actual labor.

Separate progression unlocks from future-era unlocks; gate restoration preserves
owned assets and existing contracts but controls new acquisitions. Stress and
deadline assistance are independent. Protect calendar events, authority, rights,
debt records, contests and the parents' zero rent.

Checks: each toggle alone, interacting pairs, starting Sandbox with no toggles,
activation after normal play, deactivation, old obligations, finite integer
arithmetic, operating-income reports, empty queues, instant completion loops,
home/employed/multi-studio control and future technology without future news.

Gate: every toggle has a visible effect, a reversible ongoing setting and an
irreversible eligibility consequence; financial and historical invariants hold.

## 7. Milestones and achievement delivery boundary

Add milestone predicates based on typed facts and stable IDs. Separate creator,
series and studio histories. Award milestones once; batch celebrations and
contextual unlocks. Add natural-progress and optional mastery achievements with
fully specified evidence windows. No rare-RNG grinding requirements.

Implement a platform-neutral achievement service plus a Godot-side no-op adapter.
Use a recording adapter for tests. Centralize eligibility before both stat
progress and unlock requests; reject ineligible pending/backfilled awards. Keep
the native Steam implementation optional until an app ID and integration path
are available. Prepare stable API names and an eventual platform smoke checklist.

Checks: zero adapter calls on every Sandbox path, mode selected without assists,
disable/re-enable, save/export/import, clean pre-activation snapshot, separate
clean career, offline queue provenance, retry/deduplication and account switches.
In-game milestones must still work in Sandbox. Already earned Steam achievements
must never be reset as a consequence of enabling it.

Gate: local achievement policy is proven independently of Steam availability;
delivery notes distinguish adapter tests from any future live account checks.

## 8. Management UI and illustrated conversation

Add Awards and Adaptations/Licenses views within the existing navigation, with
contextual Series actions and Inbox decisions. Offer comparison shows money,
rights, creator time and likely schedule consequences. Extend reports with
personal prizes, license receipts, creator distributions and separate Sandbox
subsidies. Add setup/settings controls with clear achievement eligibility status.

Implement the new natural-only everyday scene through `HelperStories` and
`CareerNarrativeStep`. Reuse the original `desk-conversation.png`. Add an optional
illustration key to scene presentation, including journal replay, rather than
using a temporary absolute path. Keep ordinary portrait-only scenes working.
The new scene has no desk interaction and no management-stat effects.

Verify the complete illustration, responsive dialogue layout, selected answer
callback, defer/skip, urgent-decision priority, quiet-period scheduling and replay
without mutation. Retain chibi actors, doorway despawn and hyperlapse visuals.

Gate: both headless action checks and real rendered views demonstrate the new
flows; an attractive screenshot alone is insufficient for state correctness.

## 9. Integration, balance and delivery

Run the focused tests during each implementation phase. At integration, run the
full simulation suite, build with warnings treated as errors, and the existing
and new Godot smoke paths. Compare varied short-career seeds across ordinary
difficulties and test a forty-year career. Measure elapsed time, save size and
queue growth using the same machine/build settings when comparing a baseline.

Prove viable home/doujin and employed routes without lucky awards or adaptations.
Exercise multi-location studios, a creator departure with running deals, return
home, simultaneous deadlines and a cancelled sequel. Inspect reward distributions
for quality-first behavior, repeat-award farming and runaway license feedback.
Tune catalog values, recording changes and rationale.

Keep a requirement-to-check matrix for all 28 decisions. Capture representative
award, offer, project, finance, Sandbox and illustrated-event screens. Update the
roadmap, action inventory, asset provenance and a new SP7 delivery record with
actual results and limitations. Do not label the milestone complete with missing
required gameplay paths; do not label Steam release integration complete without
the configured application and live checks.

## Review and implementation checkpoints

The new specification and this plan are the reviewable result of the design
discussion. Numerical tables and dialogue remain tunable implementation proposals.
On approval to implement, execute phases in dependency order without asking for
permission again for routine reversible changes. Ask one focused question only
when a material gameplay decision cannot be resolved from the confirmed choices.
