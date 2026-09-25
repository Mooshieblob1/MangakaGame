# Sub-project 3: Staff and Studio — Design

Date: 2026-09-23
Status: implementation started 2026-09-23; foundation/staff slice delivered,
remaining scope tracked in the [implementation plan](../plans/2026-09-23-staff-studio.md).
Decision record: [Staff and Studio considerations](2026-09-23-staff-studio-considerations.md)
Roadmap: [Mangaka Studio](2026-09-22-roadmap.md)
Baseline: [Sub-project 2 completion](../sub-project-2-completion.md), commit `0962f26`.

The player-approved choices in the decision record are requirements. Numeric
values, formulas, catalogs and resolutions of previously open details in this
document are **proposed implementation defaults**, subject to balance validation.
Use real Japanese and Tokyo data wherever it is relevant, with the game's
1996 start date respected. The companion [Tokyo research ledger](2026-09-23-tokyo-research.md)
separates verified historical facts, modern comparison points and deliberate
game abstractions. Proposed numbers are not historical quotations unless
explicitly identified as such. This draft specifies the complete milestone;
implementation tasks and estimates belong in a subsequent plan.

## 1. Goal and boundaries

Turn the solo publishing simulation into a management game about a prodigy
mangaka, their colleagues and the businesses they work in. The player directs
teams and money. The continuing viewpoint follows the starting mangaka, not
permanent ownership of the first business.

The completed milestone supports:

- Hiring, paid recruitment, special introductions, skill development, needs,
  happiness, loyalty, overtime, mentoring and moonlighting.
- Several lead creators, staff proposals, shared assistance and overlapping
  chapter production with bounded work in progress.
- Personal and business finances, salaries, creator royalties, arrears,
  voluntary investment, borrowing and incorporation.
- A common starter house, a random outer Tokyo ward, tiered rental properties,
  additional locations, local conventions, printing stock and marketing.
- Player-directed departure to another employer, a new studio or the starter
  house; old businesses continue independently, with historical rights intact.
- Rare creative breakthroughs, including a stronger chance for prodigies.
- Version 3 saves, deterministic replay and a usable Godot debug interface.

Keep the five stages: Name, Pencils, Inks, Backgrounds, Tones. Keep whole-hour
ticks, fixed speeds, editor gates, publishing calendars and cancellation.
Needs remain hunger, thirst, comfort and happiness. No relationship or housing
life sim, tax returns, stock market, detailed banking simulator, mortgage,
custom office construction, pathfinding or finished management presentation.

Sub-project 4 owns the 3D office. Sub-project 5 owns historical arrivals,
scripted rival careers and era events; ordinary employers and former studios
in this milestone are functional businesses, not those historical rivals.
Additional book formats, anime and awards remain deferred. The ownership
difficulty choice is implemented now despite broader difficulty tuning being
scheduled later.

## 2. Identity, control and new-game setup

Use three distinct concepts:

| Concept | Meaning |
|---|---|
| Person | A persistent mangaka or employee, with personal funds and history |
| Business | An employer, balance, obligations, reputation and operating policy |
| Location | A physical workplace leased or used by a business |

`ProtagonistPersonId` never changes. `ControlledBusinessId` and control mode
can change. Modes are `OwnerDirector` and `EmployedLead`. Person records are
retained after departure; IDs are never reused. No control logic uses `People[0]`.

New-game defaults:

| Item | Proposed default |
|---|---|
| Date and protagonist | 1 April 1996, 08:00; existing Aki identity |
| Skills and trait | 80 in every stage; Prodigy; common skill ceiling 100 |
| Schedule | 08:00–18:00, Sunday off, one automatic break normally needed |
| Personal funds | ¥500,000 opening endowment, then a recorded ¥300,000 founding contribution |
| Resulting balances | ¥200,000 personal; ¥300,000 business; no debt |
| Founder compensation | Salary ¥0 while directing their own business; creator royalties apply |
| Workspace | Same two-seat workroom at the mangaka's parents' house; ¥0 rent |
| Random home district | Uniformly choose Nerima, Itabashi, Adachi, Katsushika, Edogawa or Ota |
| Starting facilities | Two basic desks, basic food/drinks and comfort provision |
| Ownership difficulty | Explicit new-game choice, Studio Retention preselected |

One seat belongs to the protagonist. The house's capacity, equipment and
operating rules are identical across seeds; its map position affects travel.
Record the location once. No initial-location reroll when loading.

Ownership settings, fixed for the save:

- **Creator Retention:** departing leads retain future rights to their titles.
- **Studio Retention:** hired leads' titles stay with the business unless the
  player releases them; a replacement lead can then be appointed.

The protagonist retains their lead titles on departure in both modes. The UI
explains the asymmetry in Studio Retention. Existing books retain their
business entitlement and creator splits under either setting. Section 14
defines title handovers and the publication cutoff.

## 3. Persistent model and authority

Retain the engine-free C# library and existing partial `GameState` organization.
Introduce focused model/rule files; keep all Godot nodes outside simulation data.

| Record | Essential additions |
|---|---|
| GameState | Protagonist/control IDs, businesses, locations, candidates, pending decisions, named RNG streams, ownership setting, catalog version |
| Person | Employment intervals, base location, personal account, salary terms, Prodigy/reveal state, stage XP, needs, happiness, loyalty to protagonist, departure warnings |
| Business | Account, incorporation state, locations, active staff, policies, arrears, loans, track record, internet service |
| Location | Property ID, district/point, lease, facilities, provision budget, seat reservations, stock, activities |
| Series | Current operating business, rights lead, production lead, lead tenures, main team, priority, buffer policy, internal-pitch origin |
| Chapter/StageWork | Frozen lead and business attribution, work/quality samples, editor-attempt history, breakthrough state and payout marker |
| Volume | Immutable release entitlement, frozen creator splits, print batches and release stock, channel sales and conversion counters |
| Money records | Typed journal entries, transfer IDs, obligations, deposits, loan contracts and interest remainders |
| Work records | Production, breaks, mentoring, promotion, convention travel/attendance, moving and recovery commissions |
| World records | Event instances, recruitment searches, employer offers, departure plans, AI business policies |

Use the global ID allocator for people, businesses, locations, series,
chapters, books, contracts, loans, offers and events. Stable catalog IDs are
strings and never allocated dynamically.

Business authority checks are independent of which screen is selected.
OwnerDirector can act only for their controlled business. EmployedLead can
act only for their contracted team, permitted series and delegated spending.
Neither can edit former employers, historical entitlements or another
person's wallet. Queries return the scope appropriate to the current mode.

## 4. Teams and production

### 4.1 Leads, teams and priorities

A lead is an assignment, not a permanent employee class. Any employed artist
can lead a series. Each active series needs one employed lead and a home
location; Name is reserved for that lead. Leads can draw other stages when
free. Each employee has at most one main series team; a lead may lead two
series, but receives a workload warning. Do not silently promise simultaneous
hours to both projects.

Priorities are Low/Normal/High. Within a priority, earliest issue/due date wins,
then chapter number, stage order and stable ID. Manual task pins and explicit
stage assignments outrank automatic choices, subject to eligibility and needs.
An impossible manual assignment remains visible as blocked with a reason.

At each hour, retain the current unfinished stage unless it becomes invalid,
the worker takes a break, or the player orders a handover. For unclaimed work,
process ready tasks in priority order and choose the available eligible worker
with the highest effective stage skill; break ties by oldest assignment then
person ID. The lead is the only candidate for Name. Respect existing manual
locks and allocate each person and stage at most once.

Workers are free to help another team when their main team has no ready work
for them at that hour. Borrowed workers finish their current stage before
returning automatically. Main-team requests and delays are visible. Cross-site
help needs an explicit temporary transfer because local travel is not free
time: no automatic hourly shuttling between branches. Sharing at the same
location is automatic; a transferred worker follows the destination schedule
for at least one whole workday. The transfer uses the local travel rules.

### 4.2 Chapter pipeline and buffers

Default unfinished-chapter cap is **2**, configurable to 1–3 per series.
Serialized series may additionally hold **2 complete, unpublished chapters**,
configurable to 0–4. Stop opening work when either limit would be exceeded;
already started work is allowed to finish even if a limit is lowered.

Create the next Name after the previous Name is editor-approved, or completed
for doujin. The next chapter still has its own sequential stage dependencies.
One-shots remain a single chapter while Pitching/Offered. Editor review of one
chapter does not unlock another chapter before that Name is approved.

Every serialized chapter reserves a unique future magazine issue. Allocate
after the last reserved slot, using the magazine's existing issue calendar.
If an issue is missed, shift the entire affected unpublished sequence by one
issue as today, preserving order and distinct slots. Doujin chapters retain
cadence-based target dates. A new series starts with no backdated deadlines.

Separate doujin WIP from sales: the first five completed eligible chapters
form a printable tankobon master. Completion does not create physical stock.
Default doujin target is one unprinted master beyond the last released book;
stop new chapters at that target unless the player raises it to 2–3 books.

### 4.3 Hour semantics, handovers and quality

Plan all people's activities from the same start-of-hour snapshot. Apply the
whole hour, then stage completions, editor submissions and planner updates.
Nobody can work a newly unlocked downstream stage inside that same hour.
Repeated calls with `Advance(1)` and one call with `Advance(n)` must agree.

Keep the existing skill-speed curve. Needs modify speed as in section 6.
For each productive hour record useful progress `u`, pre-hour skill factor
`q`, actual credited person-hours and whether it was overtime. Cap `u` at
remaining work; do not credit overshoot as useful work or learning.

Stage quality replaces the final-worker rule:

```
meanSkillFactor = sum(u * (.2 + .008 * skillAtWork)) / sum(u)
stageContribution = stageWeight * 100
                  * min(1, meanSkillFactor + NameRedoBonus)
                  * max(.7, 1 - .5 * overtimeHours / requiredWork)
NameRedoBonus = .05 * acceptedRedoCount, for Name only
```

Use existing weights and final rounding. Skipped stages contribute zero.
Keep the existing overtime quality factor; needs reduce speed rather than
also adding an identical quality penalty. Redo resets the current Name
attempt's progress/quality samples, but preserves previous attempts' payroll,
learning, overtime, lifetime contribution and audit records. Stage quality
uses the accepted attempt; attempt history cannot be erased for free overtime.
Chapter royalty attribution and breakthrough lead freeze on the first Name
work. Changing a lead takes effect at the next untouched chapter.

### 4.4 Deadline forecast

Replace the first-worker calendar calculation with a deterministic shadow
schedule over the assigned teams, all competing work, fixed appointments,
planned breaks, current pipeline and editor gates. Clone the relevant state;
never consume live RNG, learn skills, award money or emit live events.
Assume the current Name attempt is approved at its due review time; explain
that further editor redos are a forecast uncertainty.

Forecast regular hours first, then allowed overtime to show both estimates.
Simulate at most 12 weeks, using the actual allocation rules and a stable
cached result invalidated by relevant commands, work completion or day change.
For deadlines beyond the horizon show "outside forecast" instead of a false
safe/late result. Already overdue work is at risk. Overtime uses the prior
completed forecast, preventing recursive planning and feedback within a tick.

## 5. Hiring, development and retention

### 5.1 Candidate pool and recruitment

The open pool has six candidates, refreshed every first and third Monday at
08:00. Each offer lasts 14 days. No manual refresh. Pool generation uses:

| Profile | Pool chance | Paid-search chance | Starting stage skills |
|---|---:|---:|---|
| Junior | 55% | 20% | Each 25–45; chosen specialty +10, capped 100 |
| Generalist | 30% | 40% | Each 40–60; specialty +10 |
| Specialist | 14.5% | 38% | Target specialty 70–85; others 35–60 |
| Prodigy | 0.5% | 2% | Each 75–90; faster learning and breakthrough trait |

All integer ranges are inclusive. Draw profile, specialty, five skills, name
and reputation in catalog order. Starting reputation is 5/10/20/25 by profile;
the original protagonist keeps the existing 10. Name lists use fictional
people; future historical candidates enter through a separate catalog hook.

A paid search costs ¥20,000, takes seven days and returns three candidates;
choose a target stage or generalist. One pending search per controlled scope,
with a 14-day cooldown from request to next request. No refund after requesting;
show the expected result date and better odds, never promise a prodigy.

Expected full-time monthly pay is
`max(ceil1000(700 * 40 * 52 / 12), round1000(60000 + 1400 * maxStageSkill))`
at hire-date game prices: a minimum of ¥122,000. This proposed ¥700/hour
benchmark sits above Tokyo's recorded annual minima of ¥650 in 1995 and ¥664
in 1996; it is not evidence of typical manga-assistant pay. See research R2.
Offers permit 80–150% of expectation, but never below the hourly benchmark;
salary at least expectation is
accepted, below it has acceptance `clamp((ratio-.8)/.2,0,1)`. One negotiation
roll per candidate/offer; increased offers may be reconsidered, repeated
identical or lower offers reuse the result. Jobs begin next scheduled morning.
Reserve seat and first seven days' pay on acceptance. Expected pay drifts with
current skill and inflation, but contracted salary does not silently change.

Ordinary candidates expose skills, expected pay and reputation. The special
uncle introduction displays skills as unknown, not fabricated low numbers.

### 5.2 Special events and proposals

Check family introductions once per controlled business per calendar month,
not once per employee. Eligible when an employed non-protagonist has 90 days'
tenure, loyalty >=60, and no unpaid wages. Chance **0.5% per eligible month**;
choose an eligible sponsor by ID-stable random draw. The resulting uncle is
always a Prodigy. Maximum two such offers in one world, at least three years
apart, including declined or expired offers. Offer lasts seven days; rejecting
it has no happiness penalty. Its ordinary quoted salary is fixed for 90 days.
Reveal talent after eight productive hours, with a staff event and visible
trait; then normal pay expectations apply after the protected period.

Other initial event: an employee recommends a former colleague, 4% per
eligible month, generalist/specialist 50/50, seven-day offer and 90-day business
cooldown. These events create an offer, never an automatic hire or payroll bill.
Save eligibility cooldowns, generated people and outcomes across business moves.

Every 28 days an employed non-lead with Name >=50 and happiness >=50 has a
10% chance to propose a series. At most one unresolved internal proposal per
person and three per controlled scope. Proposals contain title, genre, lead,
pages and suggested cadence, expire after 28 days, and cost no work or money
until approved. Approval uses ordinary CreateSeries validation and budget;
decline is harmless. Publisher pitching remains a separate action.

### 5.3 Learning and mentoring

Each useful production hour earns stage XP proportional to the useful fraction
of that hour. Next skill point costs `40 + 2 * currentSkill` XP. Prodigies earn
2x XP; everyone stops at 100. Apply increases after the hour's work and quality
sample. No XP from idle time, skipped stages or instant command handovers.

Assign a willing mentor whose relevant stage skill is at least 15 higher.
One mentor/learner pairing each, same location. A one-hour weekly session costs
both people's time, pays regular wages and adds 4 XP to the learner's selected
stage, doubled for a prodigy. Automatic sessions use Friday's first mutual
free regular hour, yield to at-risk production, and expire if no hour exists.
No off-screen training courses or separate personality tree in this milestone.

### 5.4 Happiness, loyalty and departure

Needs/happiness/loyalty are 0–100. Initial happiness 70, loyalty to protagonist
50 for hires and 100 for the protagonist. Recompute a daily happiness target:

```
target = clamp(70 + clamp(40*(salary/expectedSalary-1), -20, 10)
               + locationAtmosphere
               - 2*overtimeHoursLast7Days
               - 4*daysWithUnpaidWagesCappedAt7
               - 3*lowNeedWorkHoursLast7DaysCappedAt10
               + moonlightingModifier, 0, 100)
happiness += clamp(target-happiness, -3, 2)
```

Founder salary zero in their own business uses neutral pay satisfaction rather
than division by zero. Pay expectations and targets are visible. Loyalty moves
+1 weekly at happiness >=75, -2 at happiness <40; mentoring completion adds
.25 to the learner, capped at +1/week; moonlighting adds its specified modifier.

Happiness <30 for seven consecutive days triggers a warning. Another seven
days below 30 triggers seven days' resignation notice. Raising happiness to
45 before the notice expires cancels an ordinary dissatisfaction resignation.
Wages unpaid for 14 days cause warning and refusal of discretionary tasks;
at 21 days the worker stops production, at 28 days gives seven days' notice.
Paying the full overdue wage balance cancels that notice. The protagonist
never autonomously leaves; these conditions remain warnings for the player.

Dismissal stops new assignments immediately and offers 30 calendar days of
paid notice; immediate release owes the remaining notice pay. This borrows the
notice structure described by MHLW, not a complete employment-law simulation
(research R3). No deleting a person, their arrears or prior royalties.
Reassignment preserves work samples.

## 6. Needs, facilities, overtime and moonlighting

Use satisfaction meters: high hunger meter means well-fed, high thirst meter
means hydrated. Label them "Food" and "Drink" in UI to avoid reversed scales.

| Activity per hour | Food | Drink | Comfort |
|---|---:|---:|---:|
| Work, mentoring, marketing or attending | -5 | -7 | -4 |
| Travel or moving | -4 | -5 | -3 |
| On-site idle | -2 | -3 | -1 |
| Combined provisioned break | +45 | +60 | +35 |
| Away from work | +10 | +15 | +10 |

Clamp after each tick. Default arrival starts full after sufficient time off.
Below 35 in any need, take a combined one-hour break before ordinary work.
If break capacity is occupied, wait/rest with +10 comfort and retry next hour.
Order queues by lowest need then longest wait then person ID. Basic provision
serves two people per break hour; one extra service module per two further
seats costs ¥10,000. There is no minute-by-minute bathroom simulation.

Daily provision spending is a proposed ¥150/person tea/snack subsidy, not a
claim that a full Tokyo lunch costs ¥150. It is paid from a
location's player-set daily cap (default capacity * ¥150). Prepare at first
arrival, not for absent staff. A funded place restores the table's amounts.
If underfunded, allocate by low need/ID and show the shortage. Staff still take
a basic off-site break restoring all needs to at least 50, with no extra
business debit; underprovision gives atmosphere -5 for that day. This prevents
a zero-cash break loop while making adequate facilities valuable.

Effective work speed multiplier for unmet needs is 1 at minimum need >=35,
.9 at 20–34, .75 below 20. No work below 10; a break is compulsory. These
speed effects combine once with the existing skill multiplier. No extra
fatigue meter and no second overtime quality multiplier.

Overtime is automatic only for at-risk production, never marketing, mentoring
or moonlighting. Defaults: two extra hours/day, six/week; player ranges 0–2
and 0–12 respectively, plus per-person consent toggle. Hired staff default to
09:00–18:00 Monday–Friday with one unpaid break. Regular schedules are capped
at eight paid hours/day and 40/week; editing a schedule cannot relabel overtime.
All assigned work, including convention attendance and travel, counts toward
these limits. Additional hours pay 1.25x the agreed base hourly rate; a booked
rest-day convention uses 1.35x, without stacking both multipliers. These are
simplified scheduling/pay rules; exceptions and legal procedures are outside
scope. The self-employed protagonist can retain the opening six-day routine;
extra hours still affect quality and wellbeing, even with no founder salary.
An employed protagonist uses the employee schedule and pay rules. Needs and
happiness costs persist across midnight.
Warn before a forecast first uses overtime and at four overtime hours/week;
critical need breaks outrank overtime permission.

Moonlighting policy is Allowed, Limited or Prohibited, with person overrides.
Once every 28 days each non-protagonist employed artist has a 20% chance of an
outside project. Project runs two weeks: Allowed uses two evenings/week,
Limited one, Prohibited none. Each evening uses two post-work hours, disallows
studio overtime that evening, and skips away-from-work need recovery for those
hours. Add 2 stage XP per evening and ¥2,000 personal income on project finish.
No studio wage/royalty share or fully simulated outside series is created.

Active Allowed project gives happiness target +5 and loyalty +1/week; Limited
+3 and +.5/week. Prohibiting an offered project gives -3 target for two weeks,
then expires. Staff skip an evening if minimum need <35; it is not replaced
with production overtime. Save offers/outcomes; no player micromanagement of
each employee's private spending. The protagonist's own doujin is handled by
their normal series/workplace rather than the employee moonlighting abstraction.

## 7. Personal and business finance

### 7.1 Accounts, pay and royalties

Both balances are yen: separate accounts, not exchangeable game currencies.
Locations share business cash; journal entries carry location/series/person
tags for reporting. Personal funds follow their owner, never the camera alone.
Transfers have matched debit/credit entries and one transaction ID. Principal
is financing, not revenue; refundable deposits are assets, not rent expense.
Separate cash flow from operating profit. Carry fractional accrual remainders
and round at settlement, not once per copy or hour.

Monthly salary accrues daily by employed calendar days/month and is due on the
last day. Overtime is paid with salary. Lack of work does not erase contracted
pay. Founder salary is optional; an employed protagonist receives their offer's
salary. OwnerDirector may voluntarily contribute personal savings to their own
business, with an amount preview; shortages never trigger automatic transfers.
No unrestricted business-to-personal withdrawal: salary and royalties are the
ordinary personal income routes.

Creator royalty defaults to **20% of publishing contribution**, a proposed
game contract. For magazine chapter fees, use 20% of the business receipt.
For a volume, contribution is cumulative business receipts minus its print
costs, channel fees and allocated event/campaign costs. General rent/payroll
is excluded. Commercial receipts are the existing publisher royalty, not full
cover-price revenue. Doujin receipts are actual sales. Use:

```
royaltyDue = max(0, .20 * cumulativeContribution - royaltyAlreadyAccrued)
```

Carry early direct losses forward before awarding another royalty; never claw
back personal cash when later costs reduce contribution. Accrue on settlement,
pay monthly. Allocate shared event/campaign costs by participating volumes'
sold copies, equally if none sell. A mixed-lead volume splits the creator share
by frozen chapter leads; assistants get salary. Historical creator shares do
not change on employment/title transfers. Reserve accrued compensation when
evaluating optional budgets. Cost ledgers must prevent deducting printing twice.

### 7.2 Obligations and arrears

Optional spending needs available cash after reservations. Reject unaffordable
commands before mutation. Mandatory bills can become obligations without cash
going negative. Pay oldest first within priority: wages/royalties, essential
premises/utilities, debt service, other bills; stable ID breaks ties. Partial
payments retain original due date and remaining amount. Show next payroll and
a seven-day cash forecast before discretionary purchases.

Wages use section 5 consequences. Proposed unpaid-service rules: internet
suspends after seven days; rent warns after seven and premises close after 30,
with a seven-day closure warning. Closed offices preserve people, stock and
equipment in unassigned/storage state; terminated leases stop future rent but
retain arrears. These are game timings, not Japanese eviction-law claims.

### 7.3 Credit and incorporation

Use fictional lenders offering consumer loans and card cash advances, both
established product categories before 1996 (research R9). Terms below are
**game estimates**, not historical quotes, legal ceilings or prevalence claims:

| Product | Proposed terms | Eligibility |
|---|---|---|
| Personal instalment loan | ¥100k–500k; 24% APR; 12 monthly equal-principal payments plus interest | Protagonist; combined initial personal credit ceiling ¥500k |
| Card cash advance | Up to ¥100k within the same ceiling; 24% APR; monthly larger of ¥5k or 10% principal, plus interest | Protagonist; no new endowment |
| Business loan | ¥500k–3m; 8% APR; 24 equal-principal payments plus interest | Incorporated; 90 days of receipts; no overdue wages/rent/debt |

Cap business borrowing at three months' average settled external publishing
receipts, within the product limits. Personal ceiling may grow up to ¥1m after
90 days, capped at ¥500k plus three times average monthly earned income.
Exclude transfers, loans and contributions from qualifying receipts. Freeze
new credit while that borrower has overdue debt. Eligibility is a visible
formula, not a repeatable approval roll; quotes expire after seven days.

Daily interest is outstanding principal * APR/365, retaining fractional yen;
settle monthly. No interest on unpaid interest this milestone. Cap final
payments at the outstanding amount. Borrowing and early repayment are explicit
player actions; accepting a loan authorizes its scheduled instalments. Missed
instalments become arrears, never an automatic refinance or cash injection.

Incorporation is optional. At the 1996 start, use a small limited-company
analogue with **¥3m capital**, grounded in the former yugen-kaisha requirement
(R10). Capital stays in the business, not a destroyed fee. Require ¥3m net
business assets after liabilities and enough liquid cash for a proposed ¥200k
setup fee. Record capital and fee separately; proposed administration ¥20k/month.
Existing staff, money, contracts and business IDs remain continuous.

Business credit unlocking here is the requested gameplay progression, not a
claim that Japanese sole traders cannot obtain business finance. Personal
loans remain payable after incorporation, and personal borrowing/contributions
remain available. Do not label a 1996 lender Japan Finance Corporation (2008)
or a 1996 company godo-kaisha (2006). An effective-date rule removes the old
minimum capital from 1 May 2006 and uses a generic small-company label; broader
era/tax systems remain Sub-project 5. Fees stay disclosed game estimates.

### 7.4 No-money recovery

The starting workroom is at the mangaka's parents' home, with **¥0 rent** and
two seats. It remains a fallback for home doujin work; no eviction through the
business rent system. Do not pretend the business owns the family property.
Its basic fixed equipment is attached to that home, not a duplicable asset.
Proposed ¥5k/month home operating costs cover business utilities/materials,
not hidden rent; unpaid costs become arrears but never block basic drawing.

Add a small recovery commission: four productive Name hours earn ¥3,000 business
receipts, with the normal 20% creator share, no upfront fee, stock requirement,
XP, fans or impact. At most one completion/week/protagonist across all employer
changes. This is a proposed game safety valve, not a Tokyo market-rate claim.
It lets a cashless home creator save for a tiny print run. Personal debt still
accrues and old obligations persist. Joining an employer is another voluntary
recovery route; no forced departure, debt or fresh starting endowment.

## 8. Tokyo properties and multiple studios

### 8.1 Scope and distances

Core map: the real 23 wards, plus selected local metropolitan nodes Musashino,
Mitaka, Tachikawa, Kawasaki, Yokohama, Ichikawa, Chiba, Urawa and Omiya. Greater
Tokyo comprises Tokyo, Saitama, Chiba and Kanagawa in the reference definition
(R1); this is a playable subset, not every municipality. Urawa/Omiya use their
1996 names. No Osaka, flights or overnight intercity convention trips.

Each property/venue has a geographic anchor, district and approximate route
distance/time with provenance. A shared ward does not guarantee free travel;
crossing a ward boundary does not automatically require a fare. Mark distances
estimated from coordinates as estimates, not measured railway routes. Start
with a readable location list and travel preview, not a full 3D Tokyo map.

### 8.2 Proposed property catalog

Arakawa's official audit records residential rent around ¥2,000–4,000/m²/month
in 1992 (R4). This is an order-of-magnitude benchmark, not a verified 1996
commercial lease for every ward. Fictional workplace proposals:

| Tier | Four area alternatives | Area / seats | Monthly rent | Main tradeoff |
|---|---|---|---|---|
| Parents' home | Seeded outer ward | Fixed workroom / 2 | ¥0 | Limited room/storage; travel varies |
| Small | Nerima, Adachi, Itabashi, Katsushika | 25–35m² / 4 | ¥60k–90k | Cheap, fewer major nearby venues |
| Growing | Nakano, Suginami, Arakawa, Ota | 45–65m² / 8 | ¥120k–190k | More staff; comfort/storage varies |
| Established | Toshima, Taito, Sumida, Koto | 90–120m² / 16 | ¥250k–380k | Different access and overhead |
| Large | Shinjuku, Bunkyo, Chiyoda, Koto | 160–220m² / 32 | ¥500k–800k | Capacity and access, substantial fixed cost |

Author four predetermined properties per paid tier, each with exact area,
rent, seats, stock capacity and atmosphere (-3 to +5). No universal best site:
quiet outer offices can beat central ones on cost/comfort; a Koto option can
win on Ariake access. Location does not automatically increase artistic skill
or magazine rank. All tiers require affordability only, not reputation.

Proposed leases: two-month refundable deposit plus first month's rent; annual
renewal without a fee this milestone. Utilities ¥10k/20k/35k/60k by paid tier.
These are simplified game contracts, not universal Japanese rental customs.
Apply deposits to lease arrears, returning the balance after seven days.
Moving costs ¥20k + ¥2k/person, one workday and local staff travel. Carry stock
and furniture, never duplicate them. Old rent stops at the stated handover.
Closing a branch needs destinations or explicit storage/unassigned status;
never silently dismiss its staff.

First additional branch requires six active staff and two active series; each
further branch requires four more staff than the previous threshold. These
are proposed definitions of "big enough", not rental-tier locks. Branches use
one business account with local reporting. Cross-site assistance consumes
travel time; no production bonus merely for opening another address.

## 9. Doujin printing and stock

### 9.1 Quotes and batches

Commercial books retain publisher-funded printing and existing royalties.
Doujin masters need physical stock. A master reserves its five chapters; no
duplicate master can consume them. First batch delivery starts its release
and sales age. Let `P = 4 * ceil((bodyPages + 4) / 4)` including four cover
pages; five 19-page chapters make P=100.

| Tier | Quantity | Proposed yen cost | Delivery | Presentation |
|---|---|---|---|---:|
| 7-Twelve copy book | 1–100 | `quantity * (10*ceil((P-4)/2) + 40)` | Next day | .25 |
| Local printer | 50–1,000 | `6000 + quantity*(60 + 2*P)` | 4 days | .70 |
| Professional bulk | 300–5,000 | `18000 + quantity*(30 + .8*P)` | 7 days | 1.00 |

Copy pricing uses two A5 pages per A4 printed side plus a game allowance for
cover/assembly. ¥10/side is a **modern Seven-Eleven proxy**, not a verified
1996 quote (R6). Professional sources support page/run-size pricing structure;
these formulas and delivery times are game estimates. Use paper originals,
not smartphones, USB or modern network printing in 1996. Assembly is included
in the quote abstraction.

At P=100, ten copies cost ¥5,200, 100 local copies ¥32,000, 300 professional
copies ¥51,000. Proposed retail is `max(300, ceil100(10*P))`, or ¥1,000 here;
the former flat ¥500 price needs revision to accommodate actual page costs.
Player-set retail pricing is deferred. Preview total, cost/copy, delivery,
margin and break-even sales. Pay upfront and reserve storage; freeze accepted
quote and batch cost/presentation. Reprints cannot change historical quality.

### 9.2 Manual and automatic orders

Manual printing is always available. Automation defaults Off per series.
Player sets a 28-day spending cap, allowed tiers and target stock (default 50).
At 08:00, if stock plus incoming orders is below half target, choose the cheapest
eligible order reaching target, respecting minimum run, storage, expected
demand and reserved pay. If none fits, warn and do nothing. One outstanding
automated order/volume. Manual orders count toward the same cap/incoming stock;
switching mode resets neither commitments nor spending. Explain blocked bulk
options rather than exceeding the budget.

### 9.3 Demand, cost and presentation

Preserve the existing doujin demand formula's content quality, fans, genre and
online terms, then constrain fulfilment by channel demand and stock. Replace
its implicit 40% cost deduction: direct convention receipts are full price,
passive distribution takes a proposed 30% channel fee. Printing is already
paid; expense the relevant batch's cost against sold copies for profit, never
charge it twice. Unused stock persists after the launch window.

At sale time:

```
E = max(clamp(publishedChapterCount/500, 0, 1),
        clamp(seriesFans/500000, 0, 1))
presentationMultiplier = 1 - (1-batchPresentation)*(.02 + .58*E*E)
```

Fresh tiny doujin copy books lose roughly 1.5–2% of potential sales; established
copy-book series can lose 45%. The 500-chapter example reaches the high end
gradually. Physical quality never changes chapter quality/editor approval or
past ranks. Apply per batch's allocation of buyers, without retrying rejected
buyers against every other batch. Sell oldest batches first unless reserved
for an event.

Keep one weekly demand ledger/volume: 70% convention opportunities, 30% passive
distribution. Unused opportunities expire rather than producing another full
sales roll. The existing four-week launch window (eight for online release)
still bounds passive distribution. Afterwards conventions reach a weekly
backlist pool equal to 10% of ordinary post-launch demand; passive stays closed.
Reprints and ownership moves never restart age or demand. Multiple events
consume the same pool; fulfilled copies cannot exceed stock.

## 10. Conventions and local travel

Catalog: monthly free community show, quarterly paid medium event, major
summer/winter event. Small shows use fictional names/dates. Anchor the 1996
summer event at Ariake on 3–4 August, reflecting Comic Market 50's documented
Tokyo Big Sight move (R7). Total historic visitors describe scale, not guaranteed
customers at one booth. Never transplant today's admission rules into 1996.

Proposed exhibitor fees are ¥0/¥5,000/¥8,000 per business booking. These are
table/participation fees, not spectator tickets. Modern COMITIA pricing is only
a comparison (R8). Each event declares allowed business/exhibitor types; real
doujin rules need not match our fictional corporate-studio-friendly events.

Booking opens 28 days ahead and closes seven days ahead. Reserve one or two
attendants, one booth/business, and named stock; 100 books/person/day carrying
capacity. Show schedule/deadline conflicts and total cost. Proposed show hours
11:00–16:00 plus travel; two-day bookings reserve both days. Cancel before
booking closes for fee refund, otherwise lose it. Release people/stock either
way. A departure prompts replace-or-cancel, never phantom attendance.

Travel from each attendant's assigned studio:

- Route up to 3km: walk/bike, **¥0 fare**, minimum one hour each way. Threshold
  and whole-hour rounding are game choices.
- Further: local transit, estimated minutes rounded up to hours, minimum one
  each way. Each traveller pays both legs. Proposed unified game bands use
  the 1995 Toei scale: ¥170 up to 4km, ¥210 up to 9km, ¥250 up to 15km, then
  +¥50 per additional 6km (R5; archival verification limitation recorded).
- This is an operator-grounded game tariff, not an exact fare for every Tokyo
  rail/bus combination. No IC-card discounts at the 1996 start.

Revalidate quotes when people/location/dates change. Booked rest-day attendance
requires consent and disclosed premium pay. Travel and selling consume time
and needs regardless of free entry/fare. Stagger breaks; a solo attendant
temporarily closes their table. Proposed booth visitor caps/day are 50/150/400.
Sales are bounded by weekly demand, booth cap, stock and staffed opening
fraction, then presentation. Award at most one new fan/five fulfilled copies,
once; no attendance-only fan farming. Result shows receipts, costs and lost
production time.

## 11. Ongoing marketing and release campaigns

Assign one promoter/series, using spare hours after production by default;
player priority overrides allowed. No concurrent marketing/drawing. Skill is
mean(Name, Pencils), without a sixth skill this milestone.

- Ongoing: default cap four hours and ¥2,000/week; ¥500 per worked hour gives
  `.5 + skill/100` reach, maximum 20.
- Campaign: 14-day window, maximum eight hours/¥5,000; ¥1,000 setup plus
  ¥500/hour, double ordinary reach/hour within the same 20-point ceiling.
- Reach decays 25% weekly. Total demand multiplier `1 + reach/100`, max +20%.
  No simultaneous campaigns for one series; repeated setup creates no reach
  and cannot reset a book's release window.

Offline means flyers, shop notices and event promotion. Online means an
era-appropriate homepage/mailing-list abstraction, not social-media ads.
Keep the existing internet setup purchase; add proposed ¥3,000 monthly service.
Unpaid service suspends after seven days; paying reconnects next morning
without another setup fee. Current online demand benefits suspend while offline;
a book's already frozen launch-window length never restarts or shrinks.
Provider fees/reach are gameplay estimates pending period-specific research,
not an asserted real 1996 subscription.

## 12. Prodigies and iconic potential

Everyone can produce an iconic series. Prodigies get high starting skill,
faster learning and more creative breakthroughs, with the same ceiling 100.
No passive impact multiplier or automatic hit.

At first Name completion roll once/chapter: ordinary lead 1%, Prodigy 3%,
provided no breakthrough triggered in the previous 12 chapters of that series.
Freeze the lead/result; editor redo, reassignment, reload or repitch cannot
reroll. Present it as a standout chapter; every fifth chapter may use an arc
climax description, without an additional reward roll.

Award proposed +2 cultural impact only on first successful publication with
final quality >=70. Serialized chapters use magazine release; doujin use first
stocked-volume release containing that chapter. One award across reprints,
formats and businesses. Unpublished/low-quality work gets none. Keep existing
iconic thresholds and normal publication/ranking effects; better odds do not
bypass audience or quality requirements.

## 13. Employers and colleagues following

Three ordinary fictional employers provide progressively larger team offers.
Joining is always the player's choice. One entry offer is always available,
including after studio failure: proposed salary ¥150k/month, two total team
seats and ¥30k/month discretionary budget. Higher offers depend on reputation
and settled publishing performance, with four/eight seats and ¥250k/¥500k
discretionary budgets. Profiles are game content, not actual Japanese offers.

Offers specify salary, royalties, permitted series, seats, workload and budget.
Employer separately funds contracted salaries, approved travel and premises;
discretionary allowance covers printing/promotion. Show total commitment.
Unused allowance is not personal money. Player controls only the negotiated
team/series, not another department or the employer's bank account. More
scope needs another offer.

Invite named colleagues in a departure preview. Proposed willingness is
`clamp((loyalty-40)/60 + .25*(newPay/oldPay-1), 0, .95)`, rolled once per
person/destination offer and saved. The new employer separately checks its
quoted pay/skill/seat limits. Own-studio/home followers need available desks
and affordable accepted contracts. High loyalty does not guarantee admission;
people can remain, find other employment or become independent. Never move
the entire old payroll implicitly.

## 14. Departure and title rights

Apply a departure atomically at the next day boundary. Preview destination,
personal balance/debt, titles, accepted followers, historical income, pending
events and obligations left behind. Cancellation before the boundary changes
nothing. Three destinations:

1. Existing employer: accepted offer, EmployedLead authority and team budget.
2. New studio: explicit funding plus affordable lease, OwnerDirector authority.
3. Parents' home: new business with any explicit contribution, including zero;
   no rent and no new starting-money grant.

Identity, skills, reputation, personal money/debt and historical creator shares
follow the protagonist. Old-business cash, loans, furniture, stock, leases and
non-following staff remain there. Family home access/fixed basic furnishings
return to the protagonist's operation; old occupants relocate or become
unassigned. Two businesses cannot occupy the same free family workroom.

### 14.1 Prospective rights

Track rights lead separately from production lead. Artwork assistance never
changes either. At creation the named lead receives future title entitlement,
subject to difficulty. A permanent lead replacement requires an explicit
prospective rights handover, recorded with a new tenure; temporary assistance
does not transfer ownership. This resolves an open detail: the **current rights
lead**, not every historical lead simultaneously, carries the future title.
Automatic employer reassignment cannot surrender the protagonist's protected
title; a player-approved permanent handover must disclose that consequence.

Creator Retention transfers future rights with the departing rights lead.
Studio Retention keeps hired leads' future titles unless the player releases
them; protagonist titles follow in both modes. Release remains available during
the departure preview or later for an already-departed creator. One effective
timestamp, title and destination; never duplicate the series or fanbase.

### 14.2 Publication cutoff and obligations

| Item | Outcome |
|---|---|
| Released books and remaining stock | Old publishing business retains entitlement/receipts; original creator shares continue |
| Published magazine chapters | Historical fees/ranks/business/creator attribution unchanged |
| Unreleased master, unpublished chapters and WIP of transferred title | Follow future title, preserving work/editor state; sunk wages/costs remain with original payer |
| Future work/book release | New operating business; frozen chapter authorship still determines creator splits |
| Accrued wage/royalty arrears | Same debtor and recipient |
| Personal debt | Same person, regardless of incorporation or employer |
| Business loans, lease, print order | Same business; explicit cancellation/settlement where supported |

For a prepaid undelivered print order of a transferred title, either wait for
delivery before leaving, or preview a special cancellation returning 50% of
the quote to its payer. The remainder is sunk cost. No double delivery to both
businesses. Delivered/released books remain old-business assets. Cancel or
reassign event reservations under ordinary booking rules, without duplicate
stock. These supplier terms are proposed game rules.

Publisher contracts use a simplified consented novation: retain issue slots,
deadlines, strikes, editorial state, genre and terms, change future payee.
This is not a claim about real manga IP contracts. Moving cannot reset sales
age, publisher cooldown, a poor launch or cancellation risk.

## 15. Independent previous businesses

Former studios stay in the world, using normal production, staff, publishing,
cash and debt rules under an AI director. Player loses remote authority, even
through stale screen references. Initial deterministic weekly policy:

- Preserve contracts, wages and assigned leads; meet needs and pay obligations.
- Hire a replacement from the ordinary pool only with a seat and two months'
  projected payroll/rent reserve. No automatic loans or personal contributions.
- Cap discretionary printing/promotion at surplus beyond that reserve.
- Assign the best eligible Name worker to a vacant studio-retained title;
  otherwise pause it and allow normal deadline/cancellation consequences.
- Downsize unaffordable premises after warnings, retaining history/stock.
  No staff means dormant operations; existing book receipts, royalties and
  debts still settle. Dormancy is not deletion or a player game over.

AI businesses do not chain new branches or acquire player titles. Departing
creator-retention staff may form an independent business under the same bounded
policy. Limit new independent projects to one/business/quarter, funded from
existing resources. Historical rivals belong to Sub-project 5. Profile repeated
departures/long games before batching off-screen work; never silently freeze
creditors or past books for performance.

## 16. Deterministic tick and command rules

`Advance(hours)` is the only simulation clock. An interval is `[current hour,
next hour)`: boundary actions happen once, planning uses one snapshot, then
work consumes the next interval. Persist boundary markers to prevent repeat
processing after load. At each boundary in stable ID order:

1. Complete previous-hour activities and useful-work/XP records.
2. Resolve stage/editor completions, publication, market/sales due there,
   freezing entitlements and settling receipts.
3. Accrue/settle due pay, interest and bills; apply arrears consequences.
4. Resolve expiries, departures, lease moves, print deliveries and offers;
   run scheduled recruitment/AI updates.
5. Apply queued player commands effective now, revalidating all references,
   authority and affordability before mutation.
6. Prepare daily provisions; reserve appointments/breaks, then allocate work,
   mentoring, promotion and permitted overtime for the next interval.
7. Emit scoped alerts/recap and record the boundary marker.

Use saved named RNG streams for hiring, events, negotiation and breakthroughs,
with explicit stable seed derivation, not runtime `GetHashCode`. Document draw
order. Queries, forecasts and panel opening consume no RNG. Persist generated
offers/results so load/camera changes cannot reroll. `Advance(n)` must equal
n calls of `Advance(1)`.

Validate compound commands before any writes. Transfers, hiring, loans, printing
and departures use transaction IDs against repeated submission. Failure returns
a clear reason and changes neither state nor RNG. No Godot UI direct edits of
domain records; all commands enforce current employer/control authority.

## 17. Usable debug UI

Extend the existing UI without waiting for the later full management screens:

- New game: ownership choice with protagonist exception; seeded ward revealed.
- Header: protagonist/employer/control mode, personal and business cash, next
  payroll/arrears, no ambiguous single Money counter.
- Staff: candidates/search/events/pay, skills/XP, needs/happiness explanations,
  schedules, mentoring, moonlighting and departure/dismissal consequences.
- Work: leads/teams, priority/pins, pipeline, forecast, borrowed assistance
  and conflicting appointments.
- Studio: property/move previews, facilities, provisions, branch reports and
  storage against shared cash.
- Publishing: print quotes/margins, automation caps, stock, local event
  cost/time previews, bookings and promotion.
- Finance/career: ledgers, contributions, debt, incorporation capital versus
  fees, employer offers and complete departure preview.

In-game confirmations make player spending and career changes legible; they
are not additional development approval gates. Scope queries to controlled
teams/businesses. Do not recompute expensive forecasts every rendered frame.

Recaps show current-scope work, costs, stockouts and decisions. A break or
convention is not everyone going home. Idle skipping advances every business
through equivalent ticks, stopping at relevant arrivals/deadlines/decisions.
Keep pause and 1x/2x/4x/8x controls, headless operation and readable panels.

## 18. Saves and validation

Version 3 requires a new game; no migration. Reject older formats before
replacing live state, preserve the old file, and prevent silent overwrite
through new-game/save-as. Save records, RNG, policies, quotes, appointments,
remainders, outcomes, rights history and one-time payment/demand counters.
Pin catalog version or reject incompatibility; new prices must not rewrite
historical contracts. Rebuild only caches that cannot alter outcomes.

Validate unique IDs; one protagonist/current employment; valid control scope;
retained historical contributors; one future title right; consistent teams,
issue slots and leases; no double person/seat/stage/stock reservations;
nonnegative cash/stock/principal; finite bounded skills/needs; correct shares
and balanced transfers; valid repayment times and one-time awards. Distressed
states with arrears, unassigned staff, overdue work or negative net worth are
valid gameplay, not automatically corrupted saves.

## 19. Acceptance and balance work

This milestone is substantially larger than hiring alone. Its implementation
plan needs dependency-sized slices, each retaining working controls and saves,
before connecting career transfers across all systems.

| Scenario | Required evidence |
|---|---|
| Teams | Spare assistance, competing leads, only lead does Name; no concurrent bookings or same-hour downstream work |
| Quality/pipeline | Weighted handover prevents last-hour expert exploit; redos keep costs; unique issues and bounded buffers |
| Hiring | Salary floor, saved negotiation, better recruitment odds, rare uncle guaranteed prodigy after reveal, cap100 |
| Wellbeing | Break queues/recovery, overtime pay/consequences, actual mentoring/moonlighting time |
| Money | Arrears priority, voluntary-only investment/credit; principal versus profit; no duplicate print deductions |
| Credit/company | Interest/repayment, no transfer-based credit farming, capital retained, 2006 boundary, debt persistence |
| Locations | Identical rent-free parents' home across seeds; tier affordability, branch shared cash, no asset duplication |
| Printing/events | Tiny runs versus bulk economics; caps across mode changes; stock-limited sales; free entry/travel separately exercised |
| Presentation | Small doujin negligible penalty; 500-chapter series material penalty; history unchanged |
| Career/rights | All destinations, employer refusal, both ownership modes and voluntary release; publication cutoff and lead succession |
| Old studios | Production and debts continue; stale commands cannot control old business |
| Breakthrough | Higher prodigy chance; once-only roll/reward; no unpublished impact |
| Persistence/UI | Continuous equals save/load/replay; old saves preserved; actual controls/layout and idle skipping verified |

Run at least 100 deterministic seeds for early doujin cash flow, hiring and
one-year retention, plus targeted longer runs for rare events/iconic progression.
Report dated cash/workload and uncertainty, not realism inferred from a unit
test. Never require a rare event in every playthrough.

Existing work rules give 19 pages 98.8 base work units; skill80 yields 1.6x
speed, about 61.75 productive hours before stage rounding (63 after separately
rounding the five stages). A roughly 54-hour
productive solo week cannot comfortably sustain weekly serialization. Hiring
and overlap must make a measured difference instead of silently speeding up
drawing. Five solo chapters need roughly six weeks before edits/break
variation; show that runway to the player.

At P=100, a ten-copy run risks ¥5,200 for at most ¥10,000 gross; a 300-copy
professional run risks ¥51,000 upfront. Test production time, launch demand,
convention access, royalties and running costs together. Demonstrate recovery
after a failed print run with zero cash, no forced loan and no renewed opening
grant. At least one sustainable small-studio route must not depend on rare RNG.

This document claims no gameplay validation. Sub-project 2 checks describe
that baseline only. Implementation requires C# tests/build, headless Godot
checks and rendered interaction/layout verification, with limitations reported.

## 20. Research requirements and proposed defaults

Use the [Tokyo research ledger](2026-09-23-tokyo-research.md) for primary evidence.
Each grounded catalog value needs source, observation date, geographic scope,
unit and classification: historical fact, modern proxy or game estimate.
Inflation scaling is a simulation assumption, not historical evidence. Preserve
units such as m², printed sides and per-person/per-leg fares.

For relevant future work, prefer Japanese official statistics, Tokyo/ward
government, transport operators, organisers and printers. Verify venue/service
opening dates; avoid retroactive company names and payment technology. When
1996 evidence is unavailable, retain a replaceable, disclosed provisional value.

Specifically proposed, rather than earlier voted, details include opening funds,
salary floor/notice/pay shares, branch threshold, permanent lead succession,
recovery commissions, loan terms, printing prices, walk/bike radius and RNG
probabilities. They make the design reviewable while preserving the confirmed
choices. Review with the implementation plan before calling them approved
balance targets.
