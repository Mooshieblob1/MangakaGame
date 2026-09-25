# Sub-project 5: Historical timeline and rivals — considerations

Date: 2026-09-24
Status: this design-question pass is complete; twenty decisions confirmed.
Design and implementation plan are consolidated for review. Numerical defaults
and unconfirmed implementation details remain proposals.
Implementation has not started.
Design: [historical timeline](2026-09-24-historical-timeline-design.md).
Plan: [delivery steps](../plans/2026-09-24-historical-timeline.md).
Evidence: [research ledger](2026-09-24-historical-timeline-research.md).
Source: [roadmap](2026-09-22-roadmap.md) and prior milestone decisions.

## Existing direction

- The current game starts on 1 April 1996. Additional starting eras remain optional.
- Manga, creators, magazines and publishers use fictional analogue names.
- Historical rivals launch on a scripted schedule and retain their historical
  milestones. The player can outperform them without cancelling their history.
- Relevant Japanese/Tokyo details must be researched, distinguishing dated
  historical sources, modern proxies and deliberate game estimates.
- This milestone owns scheduled famous-analogue arrivals, historical rivals,
  era events and relevant rival offers/career interactions.
- The player still follows the starting prodigy. Existing business ownership,
  employment, title rights and separate personal/business money must be preserved.
- Full management UI/charts remain Sub-project 6. Player awards, anime adaptation
  and broader balance/polish remain Sub-project 7; rivals' historical milestones
  must not silently expand this milestone into those full player systems.

## Integration observed in the current source

Magazine rosters already contain generated filler series with popularity and
rankings. The current market also accounts for genre crowding. Historical titles
can therefore become named, scheduled competitors within an existing system.
The design still needs to decide how much additional sales/market pressure they
create, how they occupy magazine slots and how player success coexists with
protected historical outcomes. The confirmed decisions below narrow these rules.

## Decision 1 — active historical competitors

Confirmed by the user: option 1. Historical rivals participate in magazine
rankings and influence reader demand, creating pressure and opportunities for
the player's studio. Their scripted historical milestones remain protected.
The exact strength and direction of market effects are still to be designed.

## Decision 2 — curated historical roster

Confirmed by the user: option 1. Include major historical titles across several
genres, using fictional analogues. Generated smaller series fill the remaining
magazine slots. A near-complete reconstruction of historical magazine lineups
is outside the chosen scope. Specific titles and timeline coverage remain open.

## Decision 3 — blockbuster competition and opportunity

Confirmed by the user: option 1. A major hit increases reader interest in its
genre while increasing competition for attention and high magazine rankings.
Strong player series can benefit from that larger audience. This is the chosen
gameplay model, not a claim about measured historical manga sales. Effect sizes,
caps, duration and genre overlap remain to be designed, with research where
historical evidence is applicable.

## Decision 4 — time-limited famous-analogue assistants

Confirmed by the user: option 1. Future famous creator analogues can join the
employee roster during early-career availability windows. Their departure dates
are disclosed before recruitment, and they leave in time for their scripted
historical career milestones. Named analogues and specific windows require
historical research before implementation. Ordinary generated employees and
prodigies retain the existing employment and career rules.

## Decision 5 — supporting artists and mentors only

Confirmed by the user: option 1. Scripted temporary historical assistants may
contribute to player-created titles and mentor colleagues, but cannot become
series leads. Their scheduled departure therefore does not trigger lead-title
ownership transfers. Ordinary generated employees and prodigies retain the
existing leadership and ownership rules.

## Decision 6 — curated timeline from 1996 through 2025

Confirmed by the user: option 1. The initial historical content covers 1996–2025
with a selective set of major rival launches and industry changes. This is not
an exhaustive reconstruction of every year or magazine. Play continues beyond
the researched period without a forced ending. Additional starting dates remain
separate from this decision.

This is an approved content scope, not a claim that the dates or events have
already been researched. Ongoing historical titles beyond the researched cutoff
need explicit handling; invented future outcomes must not be presented as facts.

## Decision 7 — news and near-term announcements

Confirmed by the user: option 1. Reveal historical developments through news
as they happen and near-term public announcements. Do not provide a full future
calendar or reveal which newly announced titles will become major hits.
Temporary historical assistants still disclose their scheduled departure dates
before hiring, as previously agreed. Historical source notes must remain
separate from in-game information that would reveal future outcomes.

## Decision 8 — optional responses to industry changes

Confirmed by the user: option 1. Era developments unlock optional studio
responses, such as investing in digital releases or asking a publisher to
pursue a new channel. Choices have costs and tradeoffs, alongside broader
market effects that happen independently. Specific historical dates, services
and financial values still require research or explicit game-estimate labels.
Responses must respect existing publishing rights, owner versus employed-lead
authority and separate personal/business money. Full player anime-adaptation
and award systems remain Sub-project 7.

## Decision 9 — rival offers with a chance to retain staff

Confirmed by the user: option 1. Rival studios can approach ordinary generated
employees and prodigies. The player receives advance warning and can attempt
to retain the employee before a decision. Pay, loyalty and working conditions
influence acceptance. Specific offer frequency, response windows and retention
mechanics remain to be designed.

Temporary historical assistants retain their disclosed fixed departure dates.
Departing leads use the existing selected title-ownership rules. Offers to the
starting protagonist require a player career decision, not automatic acceptance.

## Decision 10 — reciprocal recruitment of ordinary rival staff

Confirmed by the user: option 1. The player can approach eligible ordinary
employees of rival studios. Salary expectations, loyalty and willingness affect
whether an offer succeeds. Historical creator analogues remain available only
during their agreed temporary recruitment windows; protected historical
milestones remain intact.

Successful hires must respect workstation capacity, wage reserves, employer
authority and existing title-ownership rules. Offer costs, information visibility,
response timing and safeguards against repeated offers remain design details.

## Decision 11 — a simulated future after 2025

Confirmed by the user: option 1. From 1 January 2026, generated rivals and market
trends continue evolving. Historical analogues still active at the cutoff can
receive fictional future outcomes. The game clearly distinguishes this period
from researched history, without implying knowledge of actual future events.
No additional starting date is implied.

The linked design supplies proposed numerical defaults and implementation
details. Those proposals are distinct from the user-confirmed choices here.

## Decision 12 — discovering historical assistants

Confirmed by the user: option 1. Targeted recruitment and connections, including
staff introductions, are the main ways to discover future famous mangaka during
their temporary availability windows. They can occasionally appear in the
ordinary candidate pool too. Discovery does not reveal future fame or guarantee
a successful hire. They are not automatically listed when their window opens.
Discovery probabilities and eligibility remain design details.

The user requested that detailed questions continue one at a time, as in the
previous sub-projects. The earlier drafts do not make their unconfirmed defaults
approved decisions, and implementation has not been requested.

## Decision 13 — promising but uneven historical assistants

Confirmed by the user: option 1. Future famous assistants have strong specialties
and room to develop when recruited, rather than exceptional ability in every
stage. Historical identity does not automatically grant the prodigy trait.
Specific skill profiles and growth values remain proposed balance choices, not
measurements of real creators. Their protected historical milestones remain intact.

## Decision 14 — contacts after historical assistants leave

Confirmed by the user: option 1. Historical assistants who leave on good terms
can remain useful professional contacts, occasionally providing staff introductions
or industry opportunities. Benefits depend on the relationship; they are not
guaranteed merely because the person was hired. Their historical career
milestones remain unchanged. Event frequency, eligibility and relationship
tracking remain design details.

## Decision 15 — public profiles plus scouting

Confirmed by the user: option 1. Ordinary rival employees have visible public
reputation and specialties. Scouting reveals a clearer picture of their current
skills and salary expectations, while loyalty and willingness to leave remain
uncertain. This does not authorize revealing exact hidden acceptance chances.
Scouting costs, duration, report precision and freshness remain design details.

## Decision 16 — mild relationship consequences for poaching

Confirmed by the user: option 1. Repeated successful poaching can mildly strain
relations between studios. Rivals may respond with stronger retention offers or
attempts to recruit from the player's team. There is no sabotage or guaranteed
retaliation. Relationship impact, recovery and response frequency remain design
details; failed approaches do not count as successful hires.

## Decision 17 — publisher approval depends on commercial prospects

Confirmed by the user: option 1. Publishers can refuse a digital or overseas
release even when the player can afford it. Audience demand, title performance
and publisher priorities influence approval. Explain refusals and allow a later
retry when circumstances improve. Rights, channel availability, funding and
employer authority still apply. Decision weights and retry timing remain
proposed balance details. This publisher-approval rule does not add a publisher
approval requirement to direct releases of owner-controlled doujin.

## Decision 18 — one combined overseas market initially

Confirmed by the user: option 1. This sub-project uses one combined overseas
market, tracking overseas interest, licensed sales and income separately from
Japan. Leave room to split this market into regions later. Regional preferences
and separate regional licensing economies are not part of the current scope.

## Decision 19 — focused initial historical roster

Confirmed by the user: option 1. Use the draft's thirteen major historical rival
series across several genres and two temporary famous-assistant opportunities
for the first implementation. Expand the historical cast after the systems are
working. Required historical-date and terminal-state verification still applies;
this scope choice does not turn provisional evidence into verified history.

## Decision 20 — pause for decisions and urgent staff deadlines

Confirmed by the user: option 1. Timeline decisions and urgent staff deadlines
pause the game by default. Ordinary industry news, including historical
milestones without a decision, stays in the chronological feed and daily recap.
Use the existing configurable auto-pause system. Idle skip must respect the
actionable warnings so that the player has time to respond before expiry.

## Review boundary

All twenty choices above are reflected in the consolidated design and delivery
plan. The user subsequently approved implementation with “looks good, implement”.
Sub-project 5 is implemented and locally verified. Costs, probabilities, durations
and exact skill profiles are initial gameplay estimates rather than separately
confirmed choices. The [delivery record](../sub-project-5-completion.md) records
implemented defaults, source verification, test results and remaining limits.
