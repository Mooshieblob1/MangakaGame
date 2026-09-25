# Sub-project 7: awards, adaptations, milestones and polish

Date: 2026-09-24.
Status: implemented locally; see the [delivery record](../sub-project-7-completion.md)
for final tuning and verified scope. Proposal language below records the design
baseline and does not supersede that record.
Requirements: [28 confirmed choices](2026-09-24-awards-adaptations-considerations.md).
Research: [source notes and evidence limits](2026-09-24-awards-adaptations-research.md).
Execution: [implementation plan](../plans/2026-09-24-awards-adaptations.md).

Confirmed choices define behavior. New names, formulas, amounts, durations and
default values below are proposed implementation tuning, not separately approved
requirements or claims about real Japanese contracts. Keep them in versioned
catalogs so balancing does not require rewriting the simulation.

## 1. Outcome and boundaries

A career can begin with an unpublished contest one-shot, gain recognition through
publication and awards, attract anime and merchandise deals, and build a lasting
legacy. Players compare offers and make a few consequential creative decisions
while manga production remains the core activity. Failure leaves routes to revise,
recover and continue. No award, anime or rare event is necessary to sustain a studio.

Deliver newcomer contests, annual awards, anime projects and follow-up seasons,
lightweight merchandise licensing, career milestones, Steam achievement readiness,
business difficulty, configurable Sandbox, the supplied illustrated Helper-Chan
scene, and integrated balance/UI checks. Actual Steam publishing and a complete
anime-production or merchandise-inventory management game are outside this scope.

Preserve these existing rules:

- The player follows the starting prodigy, including after leaving a studio.
  Starting skills and prodigy potential do not decline on harder settings.
- Personal and business accounts remain separate yen balances. No automatic
  personal contribution covers a studio shortfall in ordinary play.
- Existing title ownership, prospective transfers, frozen authorship and
  historical income attribution remain authoritative.
- Employment permits management of the agreed team and budget, not unrestricted
  control of the employer. Helper-Chan is neither a paid nor productive worker.
- The parents' home has zero rent. Tokyo locations and historical availability
  retain their existing evidence and catalogs.
- The 3D cast remains chibi; supplied 2D artwork keeps its drawn proportions.
- Simulation queries, UI refreshes, artwork and platform availability cannot
  change random outcomes. Save/load and batched/hourly continuation agree.

## 2. Integration and saved state

Current code uses `GameState.CurrentVersion = 6`, command replay, independent
timeline/story randomness, account ledgers and `CareerStore`. Add required
version-7 roots for recognition/licensing state and difficulty/eligibility.
Suggested files are `Recognition.cs`, `Licensing.cs`, `Difficulty.cs`,
`Achievements.cs`, corresponding `GameState.*` partials and a versioned catalog.
These names are proposals; existing abstractions should be reused where sound.

| Record | Required meaning |
| --- | --- |
| Contest definition/edition | Stable ID, validity years, category, entry window, judging date, eligibility, page bounds, prizes, jury profile |
| Manuscript revision | Work/person IDs, production revision, frozen stage quality, originality/fit evidence, prior publication and contest history |
| Award entry/result | Frozen submitted revision, edition, entrants, placement, feedback, named recipients, settlement IDs |
| Annual nomination | Series/award/year identity, eligibility snapshot, jury outcome, notification state |
| Partner/offer | Role, track record, genre fit, capacity, expiry, available terms, negotiation history |
| License/project | Rights scope, term, covered work, beneficiaries, payment basis, state, milestones and obligations |
| Reception effect | Origin event, target series/editions, start/end, bounded discovery/sales and reputation effects |
| Career milestone | Stable definition/entity key, first occurrence, evidence, celebration status |
| Difficulty profile | Start choices, ongoing settings, ordered changes and optional Sandbox controls |
| Achievement eligibility | Ever-used-Sandbox flag, first activation reason/time, eligibility provenance |

Use independent saved RNG streams for awards and licensing, without reseeding
existing gameplay or narrative streams. Stable IDs and settlement keys prevent
repeat prizes, duplicate contracts or repeated achievements after reload.
Resolve due work in timestamp/ID order at explicit tick boundaries. Snapshot
eligibility before results and apply receipts before the relevant finance
settlement; document this ordering in tests. Reading an offer never consumes RNG.

Extend typed event context with award, entry, offer/project and milestone IDs
where needed. Never discover milestones by parsing localized event text.
Commands validate authority, current state, amount bounds and stale IDs before
mutation or random draws. Invalid commands leave the entire state unchanged.

## 3. Newcomer contests and manuscript production

Use normal Name, pencils, inks, backgrounds and tones, including lead authorship,
staff work, needs and payroll. Add an explicit manuscript purpose: a contest
one-shot must not be mistaken for the existing publisher-pitch sample. Current
publishing code searches for unresolved `IsOneShot` samples, so simply marking
another chapter `IsOneShot` is insufficient. Keep its scheduling, publication
reservation and pitch identity separate while sharing production routines.

An entry preview shows deadline, page/category requirements, named creator,
publication restrictions, award history restrictions and availability. An
eligible completed unpublished one-shot can be entered without remaking it.
Reject duplicate active submissions across contests. Do not publish or license
the reserved manuscript during a conflicting active entry. Missing a deadline
does not silently enroll it in another contest.

The submitted revision is immutable. Judging cannot see improvements made after
submission. After an unsuccessful result, the player may revise selected stages
through ordinary work and submit a new completed revision to a later eligible
edition. Retain earlier attempts and their costs. Require actual revision work;
renaming, changing a cover or resetting progress is not a qualifying revision.
Revision can improve quality but does not guarantee improvement or a prize.

Originality must have durable evidence: derive a bounded value from the Name
revision and existing creative-breakthrough state, with any variation sampled
once at revision creation. Do not redraw originality when viewing or submitting.
Explain strengths in prose; keep hidden jury scores out of the normal UI.

Initial fictional catalog proposal: a twice-yearly story contest, a twice-yearly
comedy contest and annual published-work awards. Use data-driven page bounds;
do not copy a current real contest's limits or dates into 1996 without evidence.
Each edition has an actual finite eligible field: player manuscripts plus stable
simulated competitor records. Rival outcomes are labeled simulated where they
depart from historical facts. A top prize may remain unawarded below its floor.

Proposed judging prototype, on a normalized 0–100 scale: 60% craft, 25%
originality and 15% award fit, plus a bounded jury adjustment of at most 5 points
either way. Jury profiles can vary these weights; reader awards explicitly
weight audience support. Rank against the field, then apply prize floors and
slot counts. Small excellent works must be competitive without large sales.

Every result supplies brief feedback based on actual submitted strengths,
weaknesses, fit and competition. Shortlisted/prize work receives fuller editorial
comments. Avoid presenting a hidden exact winning threshold as guaranteed advice.
Placements grant escalating recognition; named prizes credit the recipient's
personal account once. Invitations or publishing offers use normal authority
and acceptance workflows; a contest prize never guarantees serialization.

## 4. Published-work awards and lasting recognition

Automatically consider eligible published manga for each award/year. Freeze
the shortlist and evaluation period rather than rescoring continually. Include
eligible simulated competition, completed series when the category permits, and
the same quality-first philosophy. Awards specify first-win/repeat eligibility.
Consideration is silent; nominations and wins appear through Helper-Chan and Inbox.

A win creates permanent award history and career/series prestige. Reader
discovery and sales effects expire. Nominations have smaller effects. Proposed
starting envelope: nomination effects last 4–8 weeks, major wins 12–26 weeks;
combined award-driven sales uplift is capped at 25% before other marketing
effects. These are tuning limits to evaluate, not promised player outcomes.
Avoid paying the same discovery benefit through both an increased fanbase and
an uncapped sales multiplier without accounting for that overlap.

Major awards and successful adaptations add bounded cultural impact. Preserve
the existing iconic transition: impact at least 90 and fanbase at least
1,000,000, checked once and never cleared. Existing prodigy breakthroughs and
ordinary manga publication remain viable sources. Signing a contract adds no
impact. Repeated similar recognition has diminishing returns; provisional
annual awards/adaptation impact gains are capped at 10 points per series/year.
Career milestones record these achievements but do not pay a second impact bonus.

## 5. Offers, authority, negotiation and money

Incoming offers and player pitches are both supported. Interest depends on
readership, acclaim, adaptation suitability, source-material depth and partner
capacity. Pitching reserves creator time; repeat approaches have a cooldown.
Use existing relationship facts where available, with catalog-defined eligibility
for new contacts. No repeated free roll by cancelling and reissuing a pitch.

Compare payment, creator/business split, studio track record, genre fit, scope,
schedule, approval rights, license term and restrictions in a readable card.
Separate contract promises from forecast ranges. Offers expire, can be declined,
and may coexist; the player is not guaranteed multiple choices at once.

Simple counteroffers request better payment, greater creative approval or a
different schedule where relevant. Proposed maximum: two counteroffer rounds.
Each stores the prior offer and response. Partner interest and creator leverage
affect acceptance, concessions and withdrawal; the player sees foreseeable
tradeoffs without an exact hidden roll. Later presentation refreshes never reroll.

Only a party with authority may sign. An owner-director may approve within their
rights; an employed lead requests employer approval for business commitments,
while exercising any separately held creator approval rights. Both approvals
must exist where necessary. Sandbox financial assists do not grant employer
authority or ownership of unrelated titles.

Award prizes credit the named creator. License income first enters the named
business account, then accrues the contract's creator share using the existing
obligation/payment machinery. The creator share is shown before signing; do not
silently inherit publishing's 20% rule. Prototype contract shares may use an
explicit, fictional 20–40% range of the game's net license receipts, subject to
balance review. No percentage is asserted as a Japanese industry norm.

Define the receipt basis, due dates, beneficiary identities and any recoupment
unambiguously in each offer. Initial implementation should prefer fixed license
payments and explicit per-settlement receipts over opaque committee-profit
accounting. Never turn merchandise retail gross or an anime's production budget
into spendable studio income. Creator distributions cannot exceed accrued shares.

Departure preserves active license terms, historical beneficiaries and accrued
debts. A subsequent season or new merchandise license is a new agreement subject
to current title rights and unexpired exclusivity. A rights transfer cannot erase
existing licenses or retroactively redirect payments. Leaving an employer retains
visibility of the protagonist's interests without allowing remote business control.

## 6. Anime projects and reception

State progression: proposal, negotiation, agreed, pre-production, production,
release, completed; stalled and cancelled states retain their prior history.
An agreement freezes its term and initial milestones. Partner capacity blocks
implausible unlimited concurrent productions. Use era-appropriate catalog
availability and release formats, with fictional schedules clearly identified.
This milestone presents an anime project through cards, artwork and reports,
not generated full-length episodes.

Choose hands-off, consultation or close supervision per project within the
contract's rights. Prototype weekly creator allocations: 0, 2 or 6 work hours.
Reserve real eligible work time, reduce manga capacity accordingly and respect
rest, employment and existing workload. Record missed consultations without
inventing hours. More attention can improve creative fit and warning responses;
it does not guarantee successful production. Approval rights and time allocation
are distinct. Helper-Chan never supplies substitute work.

Production checks consider partner reliability, schedule pressure, scope and
source-material buffer. Present emerging problems before avoidable major
consequences. Important decisions go through Inbox/Helper-Chan with sensible
contract-defined defaults if unanswered. Do not charge new optional spending
or expand licensed rights without the required approval. Rare cancellation
settles only the signed terms; it is neither total automatic refund nor automatic
loss of unrelated cash.

Project scope identifies the manga chapters/arcs covered. If catching up becomes
likely, allow feasible choices: finish the season at a stopping point and wait,
add original side stories, or agree to an original continuation/ending. Production
stage, time and contractual rights constrain choices. Original material has its
own craft/fit outcome; it is not an automatic quality penalty. Display likely
schedule and fidelity consequences before choosing.

On release, calculate anime quality/reception separately from manga quality.
Success yields bounded discovery, backlist sales and retained new fans. Mixed
reception may still bring readers. Poor outcomes usually reduce the expected
benefit; severe problems can create limited, temporary reputational spillover.
Never overwrite chapter quality, remove iconic status or erase the existing
fanbase as a single bad-anime penalty. Proposed spillover ceiling: 5 reputation
points, recovered over at most 26 weeks absent further incidents.

Track lifetime reach and deduplicate sales/fan acquisition effects across
episodes, campaigns and later seasons. Follow-ups are separate proposals based
on demand, performance, source material, available partners and remaining rights.
They are neither automatic nor categorically blocked by a previous disappointment.
Existing successful adaptations stay in the journal even after a cancelled sequel.

## 7. Merchandise licensing

Use the shared offer/negotiation/contract machinery with product category,
territory/channel, term, exclusivity, partner suitability and payment basis.
Starter categories are figures, clothing and stationery. Manga popularity alone
can produce interest; no anime prerequisite. Production and inventory belong
to the partner, with no player stock-management subsystem.

Demand, product fit, partner delivery and quality influence settlement receipts
and bounded reputation effects. A poor partner can underperform; an expensive
or prestigious license is not a guaranteed success. Show payment history and
term expiry. Repeated deals cannot create unlimited fan growth, duplicate sales
of the same licensed category/territory or repeatedly harvest a signing bonus
through cancel/re-sign loops. Material changes require a new approved amendment.

## 8. Milestones and Steam readiness

Store creator, series and studio milestones separately with stable keys. The
protagonist's journal follows them to a new employer or home. Helper-Chan announces
important firsts once, with a recap when several occur together. Some milestones
open contextual events or opportunities, never a generic permanent stat bonus.

Proposed initial achievement catalog: first completed publication, first contest
placement, first major annual award, first anime released, first merchandise
settlement, a successful follow-up season, readership milestones, first iconic
series and a sustained profitable-studio challenge. Define exact predicates and
evidence windows before coding. Do not award one for the rare relative-prodigy
event, require random restart grinding or force optional Helper-Chan scenes.

All ordinary presets and bounded Custom difficulty are eligible. Any Sandbox
mode selection, even without active toggles, or activation of any Sandbox assist
sets a monotonic `EverSandbox` flag. That snapshot and every descendant remain
ineligible for new Steam achievements, permanently. Show this before activation.
Disabling options, renaming/exporting the career, moving studios or restoring
presentation settings cannot clear the flag. Clean pre-activation snapshots and
independent careers remain eligible. Already earned platform achievements stay.

In-game milestones, character scenes and the journal continue in Sandbox. The
platform path checks eligibility before progress/stat updates as well as unlocks;
do not let Sandbox progress trigger a stat-based platform achievement later.
Any local pending delivery retains its originating snapshot/provenance. Never
backfill Sandbox milestones after loading a clean career or going online.

Keep achievement predicates and eligibility in the engine-free simulation;
platform I/O uses a Godot-side adapter with a no-op implementation when Steam
is unavailable. Native Steam integration, a real app ID, dashboard definitions,
icons and account testing must be separately verified before a Steam release.
Do not add an assumed app ID or require Steam to play a local build. R4 explains
why filtering only upload is insufficient. Mock checks prove game policy, not
live platform delivery or tamper resistance against manually edited saves.

## 9. Difficulty and Sandbox details

Offer Relaxed, Standard, Challenging, Custom and Sandbox. Proposed default:
Standard, preserving the present opening balances and baseline rules. Title
ownership remains an independent new-game choice. New-game choices are recorded
once; switching presets later does not issue another starting grant.

| Setting | Relaxed proposal | Standard | Challenging proposal |
| --- | --- | --- | --- |
| Opening personal/business balances | ¥400k / ¥600k | ¥200k / ¥300k | ¥100k / ¥150k |
| Tunable business pressure | 0.85× | 1× | 1.15× |
| Recovery consequence grace | 1.5× | 1× | 0.75× |
| Protagonist skill/prodigy status | Unchanged | Unchanged | Unchanged |

These multipliers are game assists/challenges, not alternative Tokyo price data.
Specify exactly which fictional discretionary costs and consequence grace
periods they affect. Never silently scale a signed salary, loan, rent contract
or already accrued obligation. Ordinary calendar deadlines still matter. Custom
uses bounded catalog choices from these ranges, not arbitrary debug edits.

Ongoing pressure/recovery controls and Sandbox options can change mid-career
through validated, logged commands. Changes are prospective; old obligations,
ownership and work remain intact. Difficulty history accompanies any challenge
whose predicate depends on settings during a time window.

Sandbox controls are independent:

- Unlimited personal funds and unlimited business funds. Apply to the player's
  personal account and authorized operating funds respectively. Supply exact
  shortfalls through labeled Sandbox subsidy entries, keeping ledger arithmetic
  finite and excluding subsidies from earned-income, profitability and reputation.
  Do not create `long.MaxValue` balances, forgive debts silently or enrich every NPC.
- Instant manga production and instant construction/delivery. Complete approved
  work through normal once-only finish hooks, at most the finite work already
  authorized. Do not create infinite chapters, double XP, unearned worked hours,
  automatic offers or instant award results. Preserve selected workers/quality
  inputs. Define coverage for each construction/delivery timer during inventory.
- Unlock locations and equipment. Waive progression gates, retaining affordability,
  placement, capacity, leases and authority unless another option changes them.
- Disable stress effects and disable deadline penalties separately. Do not erase
  hunger, legal/financial obligations or past strikes. Deadline assistance does
  not reopen closed competitions, stop the calendar or create published chapters.
- Unlock future equipment and publishing channels early. Keep the calendar,
  dated historical events and real-world news timeline unchanged. Use channel
  behavior defined for the selected content, not future market conditions globally.

When assists are disabled, completed work, balances and owned equipment remain.
Existing early-access contracts continue on signed terms; new purchases/deals
obey restored availability gates. Previously unlocked empty locations do not stay
freely purchasable. Sandbox provenance remains permanent in either case.

## 10. Helper-Chan's illustrated everyday event

Use `godot/Assets/Helper/desk-conversation.png` unchanged. It is the user's supplied
image, already copied and hash-verified. Fit it without stretching or destructive
cropping, with dialogue in its own panel; keep the full hair silhouette visible.
Use a responsive layout with keyboard/controller focus and a text-size option.
Do not require animation or generate replacement artwork.

Add a stable everyday scene ID such as `desk_moment`, using existing quiet-period
offers, narrative RNG and cooldowns. It can appear early after the introduction;
it requires neither an award nor a new studio. Urgent gameplay decisions take
priority. There is no click-to-talk desk interaction. Defer, skip and journal
replay must work with artwork; replay cannot advance time, RNG or dialogue state.

Proposed script, editable during implementation:

Title: **A moment at her desk**.
Helper-Chan: "Oh, taking a break? I can make room. For you, anyway. The hair stays."
Responses: **Tell me about your day** / **Let's sit here for a while.**
Later callbacks remember conversation versus quiet companionship. Neither choice
changes stress, morale, production, pay, loyalty mechanics or her availability.
Presentation is a companion illustration, not a requirement that every house or
office physically match the pictured desk and cabinets.

## 11. UI, reports and persistence

Add Awards and Adaptations/Licenses sections within the existing management
panels, reusing Inbox for decisions rather than a second parallel queue. Series
pages show eligible contests, honors, active anime/merchandise and upcoming dates.
Finances separate prize income, license receipts, creator shares and Sandbox
subsidies. Preserve exact tables alongside charts. Important notices use
Helper-Chan; ordinary progress stays in project cards and recaps.

Import v6 explicitly into v7, preserving originals and establishing a validated
replay checkpoint. Compose the supported v3/v4/v5 routes without invoking a new
game to fabricate history. Seed only new streams. Initialize future catalog
editions without replaying missed award ceremonies or paying historic prizes.
Validated legacy saves with known ordinary configuration may be classified
eligible for future achievements; absent eligibility fields alone never prove a
clean save. Unknown/debug provenance must be explicitly represented and cannot
emit platform unlocks pending a supported classification. Do not synthesize past
achievement evidence that the old format did not record.

Validate references, finite values, chronological states, authoritative signatures,
exclusive scopes, receipt totals, monotonic Sandbox provenance and uniqueness.
Keep terminal results and financial evidence; bound presentation lists through
paging and aggregation. Save/load/export must retain illustrated scene choices,
pending negotiations, project warnings, staff time reservations and eligibility.

## 12. Acceptance and balance evidence

Behavioral checks must cover contest revision and reservation, quality-first
competition, all reward tiers, automatic annual nominations, temporary effects,
creator/business accounting, departure with active contracts, negotiation limits,
adaptation stages and cancellation, catch-up routes, mixed reception, renewals,
merchandise expiry, milestones and all Sandbox combinations that interact.

Run paired seeds through hourly and batched simulation, save/reload and replay.
Vary narrative choices and UI access to prove unrelated outcomes are unchanged.
Check career progression from home, as an employed lead, with multiple studios
and after returning home. Run short varied-seed balance scenarios and a forty-year
career with bounded queues/catalog scans and measured save growth.

Standard must support a sustainable small studio without award/adaptation RNG.
Relaxed must offer demonstrably more recovery room; Challenging must remain
recoverable without weakening the prodigy. A very good small manga can beat a
bestseller in a jury award; mediocre spam cannot reliably farm prizes. Repeat
licenses and exposure effects must not grow income/fanbase without bounds.

For achievement readiness, exercise every Sandbox toggle and activation path,
including a save made with no toggles but Sandbox mode selected. Require zero
platform-progress/unlock calls from ineligible state, even after option removal,
reconnect, import/export or a studio change. Confirm a clean career remains
independently eligible. Keep journal scenes operational in both cases.

Rendered checks cover award/offer comparisons, busy Inbox priority, personal and
business receipts, difficulty setup, Sandbox consequence display, the full desk
illustration, journal replay, and unchanged chibi/door/hyperlapse behavior. Record
actual checks at delivery; this design is not evidence that they have passed.
