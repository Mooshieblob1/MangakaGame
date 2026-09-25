# Sub-project 5: Historical timeline and rivals

Date: 2026-09-24.
Status: implemented and verified locally on 2026-09-24. Numerical balance
values are initial gameplay estimates, subject to playtesting. See the
[delivery record](../sub-project-5-completion.md) for final defaults and evidence.
Basis: [confirmed decisions](2026-09-24-historical-timeline-considerations.md).
Evidence: [primary-source ledger](2026-09-24-historical-timeline-research.md).
Delivery: [implementation plan](../plans/2026-09-24-historical-timeline.md).

## Experience and scope

The player builds a manga career in an industry that changes around them.
Familiar fictional rivals arrive, compete for readers, inspire new interest,
reach protected historical milestones and eventually finish or change status.
A player series can rank above them. A famous rival's scripted success does not
take away the player's success or force its cancellation.

Start remains 1 April 1996. Curated history ends on 31 December 2025. From
1 January 2026 onward, the world evolves through explicitly fictional events.
There is no ending screen or new start-date selector in this milestone.

The user confirmed the focused first roster: the thirteen historical rival
analogues listed below and two temporary creator opportunities. Expand the
historical cast after the systems work. Also deliver a small population of
ordinary rival studios, industry news, channel decisions and reciprocal staff
recruitment. Keep the existing office,
production, printing, conventions, property tiers and protagonist continuity.
Provide usable controls in the existing Godot harness; the full management
screen redesign belongs to Sub-project 6. Player anime production and awards
remain Sub-project 7. Historical rivals can receive adaptation news here.

Confirmed direction, including initial roster scope, is in the decision record.
Unconfirmed balance numbers, working names and detailed mechanics below remain
proposed implementation defaults, not further user approvals or historical
measurements. Historical content still requires the stated evidence checks.

The completed question pass establishes these player-facing priorities:

- Famous assistants are discovered through recruitment, connections and rare
  pool appearances. They have uneven strengths, disclosed departures and can
  remain useful contacts when the relationship is good.
- Rival staff have public profiles; scouting improves knowledge without exposing
  loyalty or exact willingness. Repeated successful poaching has mild studio
  relationship consequences, with funded responses rather than sabotage.
- Publishers can decline new channels for commercial reasons and explain why.
  Overseas activity initially uses one combined market, separate from Japan.
- Decisions and urgent staff deadlines pause by default. Ordinary historical
  news stays in the feed and recap. Future history is not revealed in advance.

The remaining numerical tuning and technical work do not constitute additional
unanswered design questions. The implementation plan records what must be
resolved and verified before delivery; further material scope changes still
need discussion.

## Historical catalog and chronology

An embedded, versioned catalog contains stable string IDs for rivals, creators,
events, sources and channels. Each event includes its reference identity,
fictional copy, evidence classification, date precision, source IDs, effective
game hour, optional public announcement hour and typed effects. Validate all
references, dates, supported genres and overlapping creator obligations.

Keep source date/precision distinct from the effective game timestamp:

- Exact dates take effect at 00:00 Japanese game time; the title first appears
  in the next relevant magazine ranking. No timezone conversion from the PC.
- Month-only evidence uses the first day of that month as an explicitly
  approximate game anchor. Year-only evidence uses 1 July of that year.
- Issue-only evidence must acquire a source-backed release date or an explicit
  approximate month/year before shipping. Never parse an issue number as a date.
- Preserve causal ordering: launch precedes ending; employment ends before
  conflicting career commitments. If coarse dates conflict, refine the evidence
  or omit the subsidiary event rather than inventing an exact historical date.
- Do not announce a future event without a supported public announcement. The
  default is news at occurrence. Assistant contract end dates are disclosed.

Series active before the game starts are seeded at their then-current phase.
Completed pre-start series are not inserted as active serializations. Current
rankings may vary; launches, sourced milestones, hiatuses and endings through
2025 do not depend on the player's rank or staff decisions.

Initial working roster (reference names are development documentation only):

| Reference | Fictional working title | Primary game genre | Game magazine |
| --- | --- | --- | --- |
| Detective Conan | Casebook Kento | mystery | hoshigaku-sunday |
| One Piece | Two Piece | adventure | tokiwa-jump |
| Fruits Basket | Zodiac House | romance | hoshigaku-flowers |
| Naruto | Naruka | action | tokiwa-jump |
| Mushishi | Spirit Wanderer | drama | kaidan-afternoon |
| Bleach | Blanch | action | tokiwa-jump |
| Chihayafuru | Hundred Verse Hearts | sports | hoshigaku-flowers |
| Attack on Titan | Beyond the Walls | horror | kaidan-magazine |
| Haikyu!! | High Serve!! | sports | tokiwa-jump |
| My Hero Academia | Hero Homeroom | action | tokiwa-jump |
| Demon Slayer | Demon Lantern | fantasy | tokiwa-jump |
| Spy x Family | Secret Household | comedy | tokiwa-square |
| Frieren | After the Hero | fantasy | hoshigaku-sunday |

These are gameplay groupings into the existing six magazine templates, not
historical publisher/cadence claims. For example, an original web title appearing
in Tokiwa Square's rankings is an explicit abstraction. Catalog metadata retains
its original medium, and news must not falsely call that reference a print debut.
Do not add new magazine economies merely to achieve an exact historical mapping.
Complete each entry's pre-cutoff terminal-state evidence before shipping content.

## Ranking and demand

Extend a filler entry with an optional historical rival ID. Keep rank row IDs
stable across issues. A launch replaces a generated filler, never a contracted
player series. Select the lowest-popularity eligible generated filler, breaking
ties by ID; archive it normally. A historical ending frees that filler slot for
a new generated title. A hiatus retains identity but contributes no active
serialization score until return. Historical titles bypass generated-filler
cancellation and random replacement through the cutoff.

Preserve the existing roster policy and player contract capacity. Validate peak
historical occupancy against every magazine's available filler slots across the
whole catalog. If no replaceable slot exists, fail content validation; do not
silently expand the roster, evict the player or postpone a scripted launch.

Historical rivals use designed launch/growth/peak/tail popularity curves on the
existing score scale, with small deterministic issue variation. These are not
historical reader surveys. There is no forced rank-one override. Rival scores
use the same genre crowding input as other fillers; current player cancellation
grace and iconic protection remain intact.

Model increased interest separately from limited attention. Proposed defaults:

- For each active hit, normalized intensity rises from 0 to 1 over six months
  after its configured breakthrough; ends fade over twelve months.
- Same-genre expansion is `min(0.25, 0.12 * sum(intensity))`.
- Same-genre attention pressure is `min(0.15, 0.08 * sum(intensity))`.
- A volume's quality factor is `clamp((quality - 40) / 40, 0, 1)`.
- Demand multiplier is `clamp(1 + expansion * qualityFactor - pressure,
  0.85, 1.25)`. With one full hit, quality 80 yields 1.04; quality 40 yields 0.92.

These deliberately modest numbers make strong books benefit while weaker books
feel pressure. Apply once to pre-stock unit demand, before sales channels split.
Do not apply it again through `GenrePopularity`, ranking score or fan growth.
Physical doujin sales still require printed stock. Existing campaign and print
quality effects remain separate, with combined-factor bounds covered by balance
scenarios. Historical success does not guarantee a player's commercial success.

## Temporary historical assistants

Use the two researched career anchors and explicitly fictional opportunity
windows in the research ledger. Discover them through targeted recruitment and
connections such as staff introductions, with occasional ordinary-pool appearances.
An opening window does not automatically reveal the candidate. After discovery,
a separate recruitment card preserves the opportunity through normal pool
refreshes, subject to its availability deadline. The card shows current skills,
salary expectation, start constraints and fixed departure date, but no promise
of future fame or future hit title. There is one identity per creator.

Confirmed starting-ability direction: promising but uneven, with strong
specialties and room to develop. Historical identity does not automatically
grant the prodigy trait or exceptional skill in every production stage.
Numerical profiles remain fictional balance choices.

Confirmed relationship direction: assistants who leave on good terms can remain
professional contacts, occasionally providing staff introductions or industry
opportunities. Benefits depend on the relationship and are not guaranteed by
hiring alone. Preserve the contact after employment ends without keeping them
on payroll or occupying a desk. Historical career milestones remain protected.
Relationship tracking, event eligibility and frequency remain proposed details.

Ordinary hiring checks apply: owner authority, free usable desk, salary offer,
wage reserves and next-business-day start. Reject starts on/after departure.
They can do supporting production stages and use existing skill-development
mechanics. Mentoring adds at most 10% to existing earned skill growth for one
less-skilled colleague in the same studio on days both work; never grants free
skill while absent or stacks multiple bonuses on one learner.

They cannot lead, acquire future title rights, propose a lead project, join the
ordinary poaching pool or be retained beyond the disclosed departure. Dismissal
remains possible under normal earned-pay rules. Fixed departure removes work
and desk reservations, cancels pending offers, settles or records earned wages,
and leaves completed contributions and their original creator attribution intact.
Warn thirty days and seven days beforehand. At the departure boundary they do
no further work. Staff welfare can still cause an earlier departure; the future
historical launch remains protected. Departure never transfers a player title.

## Ordinary rival studios and staff

Historical series are lightweight market records. Do not simulate every chapter,
author's daily needs or thirty years of historical personal finances. Their
protected creators are not ordinary staff who can be poached.

Separately seed three fictional rival businesses with one location and three
ordinary staff each; cap initial expansion at six staff per rival. Reuse the
existing business, account, employment, property, person and office-assignment
models so recruits keep identity, skills, loyalty, wages and contribution history.
These nine initial people are real sim people, unlike market-only historical
authors. Reuse background-business scheduling and add only missing decisions.

Give each new rival an explicit opening capital entry and a small generated
commission portfolio. Proposed opening capital is ¥3 million; monthly gross
commission income is ¥1 million plus ¥10,000 times its mean staff skill (0–100),
posted once with a distinct reason. This is a simplified fictional studio-income
model, not income attributed to the curated historical titles. Charge wages,
rent and operating expenses normally. If a studio gains fully simulated player
titles, their real receipts are additional identifiable entries, never duplicate
commission royalties on the same books. Offers require actual cash and payroll
capacity; indebted rivals reduce hiring rather than receiving hidden bailouts.

Business/personal balances remain separate. The protagonist's former business
keeps its existing background behavior; do not reseed or replace it with one of
these three rival templates. A rival cannot spend another business's cash.

### Offers and retention

Proposed inbound offer cadence: weekly check, 5% chance per eligible employee,
at most one new offer per player-controlled business that week. Eligibility
requires ninety days' tenure and no unresolved offer, historical obligation or
pending departure. Choose a solvent rival and a funded role before rolling.
Set a seven-day response deadline; one offer per person per ninety days.

Player receives the competing salary, role, destination and deadline. Options:
let the offer stand, raise salary, or decline retention efforts. A salary raise
is an ordinary lasting wage change, with reserve checks, not a free promise.
Existing welfare policies affect the outcome. In employer mode, salary changes
must remain within the team's existing authority; otherwise request employer
approval using a bounded, quoted response, not a direct company-wide edit.

At expiry, compare actual offers and current conditions. Proposed acceptance
probability is clamped to 5–90%, starting at 30%, adding up to 25 percentage
points for a rival's pay premium and up to 20 for dissatisfaction, subtracting
up to 30 for loyalty. Store one random draw at offer creation; no reroll from
opening a panel, resaving or revising terms. Resolve staff, payroll, assignments
and future rights atomically. No transfer if destination capacity or funding
has disappeared; report the lapsed offer and preserve the current employment.

Ordinary leads use the selected title-rights difficulty policy. Reuse and factor
the existing departure/transfer logic so this destination is used instead of
accidentally creating a new studio. Past chapters, stock, volume ownership and
cash never move retroactively. Unreleased rights remain with the former owner;
they cannot be sold twice. The protagonist receives a career opportunity and
only moves through an explicit player choice, with a quoted destination, budget
and follower eligibility. Never auto-accept on their behalf.

### Player recruitment from rivals

Show ordinary rival staff's public reputation and specialties. Scouting provides
a clearer picture of current skills and salary expectations, while loyalty and
willingness to leave remain uncertain. Exact hidden acceptance chances and
future development are not revealed. Scouting costs, duration, report precision
and freshness remain design details. Historical creators do not appear here.
A ¥20,000 approach fee and seven-day negotiation are proposed defaults; a failed
approach has a thirty-day
cooldown for that target. One active negotiation per person worldwide.

Confirmed studio-relationship direction: repeated successful poaching can mildly
strain relations. Rivals may respond through stronger retention offers or
recruitment attempts against the player's staff, without sabotage or guaranteed
retaliation. Response strength, frequency and relationship recovery remain design
details. Responses must respect rival cash, payroll capacity and the existing
offer safeguards; they cannot bypass staff willingness or protagonist choice.

The same pay, loyalty and conditions model applies in the other direction,
with one stored draw. Reserve the proposed desk and seven days of wages when
the offer is made; account for the reservation in all other hiring/spending
checks. On resolution, revalidate the desk, funds, authority and notice period,
then use one shared transfer transaction. Reserve release must occur on failure,
expiry, cancellation, career change or business closure. No negative cash,
duplicate person, duplicated title or simultaneous employment.

## Industry changes and player responses

Use the dated anchors in the research ledger: overseas print opportunity,
early mobile distribution, overseas digital, simultaneous release, domestic app
distribution, broader international distribution and late-era digital news.
The three fictional publishers need explicit channel eligibility; a real service
launch is not an automatic agreement with every fictional publisher.

Separate capabilities from adoption. Existing internet access and online physical
doujin orders remain valid before smartphone platforms; purchasing internet does
not grant overseas publishing rights or modern app access in 1996.

Owner-controlled doujin can opt into an available domestic digital channel.
Serialized titles request publisher participation; employed leads request within
their team's authority. A proposal shows eligible volumes, rights owner, total
cost, revenue terms, decision date and reason for possible refusal. Proposed
setup costs are ¥30,000 for direct domestic digital and ¥100,000 for overseas
localization per title, payable by the authorized business. Seven-day publisher
responses use a stored draw and a ninety-day retry cooldown. No automatic
debit simply because a platform launch occurred. Owners can defer or decline.

Confirmed approval direction: affordability alone does not guarantee publisher
approval. Audience demand, title performance and publisher priorities influence
the decision. Give a refusal reason that reflects the actual decision factors
and allow later reconsideration when circumstances improve. Numerical weights,
decision delay and retry cooldown remain proposed defaults. Direct releases of
owner-controlled doujin do not require this publisher approval.

For the first implementation, domestic digital is a second edition of an
eligible volume, not a whole new production pipeline. Calculate total potential
domestic units once, then split by the era's configured digital preference only
if the title has adopted. Proposed digital share rises gradually from 5% to a
maximum 50% across enabled eras. Non-adopters retain physical demand multiplied
by `1 - 0.5 * digitalShare`; this is a game estimate, not a claim about migration
measured in Japan. Digital units consume no stock; physical units still do.
Only adopters receive digital receipts. Keep format/channel sales totals distinct.

Reuse commercial net-royalty accounting for digital commercial editions; direct
doujin digital receipts use displayed price less a proposed 30% service share.
Quote those game terms before spending. Each edition retains the original
volume's business and contribution ownership. Transferring a future title does
not authorize rereleasing someone else's back catalog. Start with volumes whose
existing sales window is still open; no infinite reopening of closed books.

Confirmed market scope: one combined overseas market for this sub-project,
with interest, licensed units and income tracked separately from Japan. Give
the market a stable identity so regional markets can be added later without
conflating old domestic and overseas records. Regional preferences and separate
regional licensing economies are deferred.

Overseas licensing unlocks a separate, bounded demand pool after acceptance:
proposed ceiling 20% of equivalent domestic demand, scaled by international
interest (0–100). Add the interest state explicitly; it is not present in the
current source despite the old roadmap placeholder. Interest grows from actual
licensed readership and configured exposure, not from simply advancing the year.
Seed accepted titles at 10 interest to avoid a zero-demand deadlock. Store units,
net yen receipts, fees and contributor payments by channel, volume and period.
No foreign exchange trading or full country-by-country market in this milestone.

Piracy-related news can affect the perceived value of timely official access,
but the default numerical piracy-loss modifier is zero pending a defensible
model. Neither illegal-read estimates nor simultaneous-release dates justify
claiming a specific recovered-sales percentage. Adaptation news can trigger
modest genre interest through the single rival-demand mechanism above; avoid a
second global anime/streaming multiplier. Full player adaptations remain later.

## News, responses and the office

Add a chronological industry feed and an action inbox to the existing harness.
Show current rival profiles, magazine rankings, discovered assistants, channel
offers and recruitment negotiations. Each actionable item shows its expiry,
price, authority and consequences; invalid actions explain the missing condition.
No developer catalog IDs or future hit markers in ordinary player-facing copy.

Historical outcomes are revealed at their occurrence. Near-term announcements
require their own dates; there is no omniscient calendar. Historical research
and reference identities live in development documentation, not a spoiler panel.
Contract departure dates remain visible. Staff changes flow through ordinary
office assignments and appearance identities, including arrivals and exits.

Confirmed default: pause for new actionable decisions and urgent staff deadline
warnings, using the existing configurable auto-pause system. Ordinary industry
news and historical milestones without a decision appear in the feed and daily
recap without pausing. Group simultaneous alerts into one pause rather than
repeatedly stopping when the player resumes.

Issue deadline warnings while there is still time to act. Idle skip must stop
at these actionable warnings even when no worker is present, rather than after
an offer has already expired. Godot owns pausing and the driver's skip boundary;
the engine-free simulation continues to advance deterministically when asked.
Acknowledging or filtering news cannot change the simulation, redraw offers,
spend money or continually retrigger an acknowledged pause.

## Simulation order, persistence and future

Use whole-hour ticks and three new saved RNG streams: rival markets, rival
staffing and future events. Seed them with fixed documented salts; do not draw
their seeds from existing production, staff or location RNG streams.

At a tick boundary, process due timeline effects in `(effective hour, priority,
stable ID)` order before issue rankings, sales and new work. Priority is:
fixed departures/window closure, historical status changes, channel unlocks,
then announcements. Apply changes at the boundary without retroactively deleting
the previous hour's earned pay or work; align this explicitly with `TickStart`.
Resolve ordinary offers after payroll/needs updates with current validation.
Never let a departing person work an interval starting at their departure time.
Test these interval semantics rather than relying only on method order.

Persist catalog revision, processed event IDs, active rival phases, source-date
precision, channel adoption/contracts, editions, offer draws/deadlines/reserves,
assistant identity/departure, former-assistant contacts, scouting knowledge,
studio relationships and response cooldowns, acknowledged pause alerts, future
state and all RNG states. Bump save version
to 5. Unknown revisions and missing required data fail clearly instead of being
silently defaulted. Loading, multi-hour advance and idle skip produce identical
results to equivalent single ticks. UI snapshots are read-only.

Provide an explicit version-4 checkpoint import, preserving the original file,
clock, IDs, existing RNGs, money, titles, staff and furniture. Seed historical
state as of that date, replace only eligible filler slots, mark earlier events
processed without replaying rewards/news, and announce that history has been
initialized. No past digital revenue or expired assistant hires. Keep old command
history as provenance; replay starts at the converted checkpoint, not a fresh
version-5 game. Preserve the explicit completed-version-3 importer by converting
to a validated version-4 shape first, then the new checkpoint format.

At 2026-01-01 emit one clear message: the researched timeline has ended and
subsequent developments are simulated. Keep this status visible in the date/feed.
Ongoing analogues retain their current identities and popularity; freeze no
unrecorded historical destiny. Quarterly future checks can create rival launches,
genre shifts and endings, with one-year minimum time between major events for
the same title. Proposed ending chance is 5% per quarter after one year in the
future; an ending replaces a filler slot as usual. Generated rivals continue
normal turnover. News is labeled simulated, never attributed to real events.
No new permanent channel stack above the design caps; old historical events do
not repeat. Archive finished records and bound active staff/offers/news snapshots
so decades of play do not expand hourly work without limit.

## Acceptance

The milestone is complete when all catalog entries have validated historical
states through the cutoff; rivals rank without overriding player success;
temporary hires leave safely; both recruiting directions respect money, desks,
authority and title rights; channel choices produce attributable sales; all
timed actions survive save/load and idle skip; and an explicitly simulated future
continues. Validate a rendered player walkthrough as well as deterministic tests.
The implementation plan defines the evidence to record. No runtime changes or
new test results are claimed by this design document.
