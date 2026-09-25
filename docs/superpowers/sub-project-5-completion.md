# Sub-project 5 delivery record

Implemented locally on 24 September 2026. The user's “looks good, implement”
approved the consolidated design. Prior publishing, studio and office changes
were preserved. No commit or push was requested.

## Delivered

The **Industry** tab exposes dated news, current historical rivals, rival staff
profiles, paid scouting, recruitment offers, retention responses, temporary
assistant opportunities, career offers and digital/overseas release proposals.
It uses the simulation's actual people, workplaces, money, rights and time.
Ordinary news does not pause by default; decisions and urgent staff deadlines do.
Undiscovered assistants and future historical entries are not revealed early.

Thirteen fictional manga analogues enter the six existing magazine markets on
researched dates. Historical launches replace generated filler slots without
evicting player contracts. Scripted endings and the modeled late-2025 hiatus
are protected; the player can still outrank these series. Genre opportunity and
competition produce one bounded demand modifier. From 1 January 2026, clearly
labeled simulated events allow ongoing rivals to resume or end and generated
competition to continue. Ended history and assistant windows never restart.

Three funded Tokyo rival studios have real employees, salary obligations, rent,
furniture and commission income. Scouting gives rounded skills and salary
expectations without exposing loyalty or acceptance rolls. Reciprocal offers
reserve actual desks and wages. Employees can stay, leave or accept a lasting
raise; repeated successful poaching mildly affects studio relations. Protagonist
offers require an explicit choice and use the actual offering employer. Selected
followers face the destination's skill, space, funding and saved willingness
checks. A career move cancels outstanding recruitment and publisher proposals
from the former studio, releasing their reservations without setup charges.

Two temporary famous-analogue assistants can be discovered through recruitment,
staff connections or the ordinary candidate pool. Their fixed departure is shown
before hiring. They support production and offer small attendance-dependent
mentoring, but cannot lead series or automatically count as prodigies. Departure
warnings, accrued wages and contribution history survive their departure.
Good relationships can later produce occasional professional introductions.

Researched industry milestones unlock optional distribution routes. Direct
owner-controlled digital doujin can launch immediately; publisher-backed
proposals take seven days and may be refused for work quality, readership or
publisher priorities. Domestic demand splits between physical and digital;
digital copies require no printed stock. One combined overseas market tracks
interest, licensed units and net income separately. Setup is charged only on
approval. Edition ownership and contributor payments remain attached to the
original business and creators when future titles move.

Version-5 saves include scheduled state, scouting knowledge, stored decisions,
reservations, relationships, receipts and independent random streams. Explicit
version-4 import preserves the old file and starts a replay checkpoint without
retroactive historical income. The existing version-3 import composes through
the version-4 boundary. New-game and imported-checkpoint replay are tested.

## Evidence and initial balance

The [production research ledger](specs/2026-09-24-historical-timeline-research.md)
records primary sources, date precision, cutoff status, magazine mappings and
fictional interpretations. Year-only events use 1 July and month-only events
use the first day, with approximate dates labeled. Four adaptation anchors are
individually sourced. Other rivals use a fictional one-year growth delay.
Real creator biographies inform the two analogues; their availability contracts
and skill profiles are invented. National manga-market figures are not claimed
to measure Tokyo alone. The existing rent-free parents' home is unchanged.

These are implemented starting values for playtesting, not measured Japanese
prices or probabilities and not additional user-confirmed design decisions:

- Rival studios start with three employees and **four usable desks**, rather
  than the draft's six-seat upper bound. Monthly commissions fund this simplified
  background economy; these studios do not run full automatic chapter production
  or unlimited recruitment/expansion.
- Scouting costs ¥10,000, takes three days and rounds skills to the nearest five.
  Reports have a 30-day refresh window. Approaches cost ¥20,000 and take seven
  days, with a seven-day wage reserve and a 30-day per-person approach cooldown.
- Temporary discovery chances are 35% after targeted recruitment, 2% during a
  candidate-pool refresh and 3% at an eligible weekly staff-connection check.
  Productive mentoring adds 10% to eligible experience for one learner per mentor
  per day. Former contacts need relationship 60, a three-month cooldown and a
  successful 5% monthly check before introducing a candidate.
- Repeated successful poaching between the same studios within 90 days costs
  five relationship points; relations recover one point monthly. Hostile studios
  can fund a modest retention raise. Willingness always uses the stored offer roll.
- Domestic digital setup costs ¥30,000 and overseas setup ¥100,000. Refused
  proposals have a 90-day retry window. Digital preference rises from 5% to a
  50% cap across 19 years; non-adopters lose half of that potential share. Overseas
  interest starts at ten and remains bounded. These are fictional demand rules;
  no quantitative piracy-to-lost-sales penalty is claimed.
- Future lifecycle checks are quarterly. Hiatus resumption has a 25% check;
  eligible endings have a 5% check after the initial future year and a one-year
  title cooldown. New generated competition can add small capped genre noise.

## Verification

- Warning-as-error solution build: **zero warnings and zero errors**.
- Domain suite: **421 regular tests passed**, plus the separate **one long-run
  test passed** (422 total). No skipped tests in these two runs.
- Godot 4.7.2 .NET editor import: exited successfully with no errors.
- Headless Godot smoke: **758 checks passed**, no errors.
- Rendered Godot smoke: **823 checks passed**, no errors. Screenshots were
  inspected for readable recruitment and distribution controls.
- The 1996-04-01 to 2036-04-01 hourly simulation took **163.46 seconds** locally
  (about 0.47 ms per simulated hour). Its save contained **38,472,364 JSON
  characters**, **25,799 events**, **ten people** and **225 retained news items**.
  Loading that save and advancing both copies another 48 hours produced identical
  state. This run includes an insolvent idle player business with growing unpaid
  utility bills, so it exercises arrears as well as the funded rival studios.
- The stress run exposed repeated wage-history scans for every unpaid bill.
  Computing the set of businesses owing wages once per hourly settlement removed
  that repeated work without changing payment priority. Earlier unfinished runs
  were stopped and replaced; they are not counted as passes.
- Rendered office performance on the local NVIDIA GeForce RTX 5070 with 32 staff
  and six ambient occupants at 8x: Full median **7.52 ms**, p95 **9.71 ms**;
  Reduced median **6.56 ms**, p95 **7.73 ms**. Capture frames were included.
- Final whitespace/diff validation passed. Local logs and screenshots are under
  ignored `TestResults/`; the domain TRX reports are under the test project's
  `TestResults/`.

The tests cover chronology, historical capacity and protection, demand caps,
once-only settlement, scouting privacy, atomic command failures, funded offers,
closure/career cancellation, retention, actual-employer transfers, both rights
settings, temporary contract expiry, productive mentoring, publisher refusals,
stock-free digital releases, old-edition attribution, corrupt saves, imports,
deterministic continuation and the simulated-future boundary.

The rendered walkthrough exercises the Industry controls and the existing office
at normal and 8x speed, including urgent-warning pause/resume, scouting, hiring,
retention, digital approval and income, save/load and the future transition.
Captures live under `TestResults/industry-*.avif`. The office's hyperlapse exposure
trails and explicit ambient entry/exit doors remain covered by the rendered
office smoke checks.

## Limits

This remains the development management harness; polished management screens
and charts belong to Sub-project 6. Earlier digital-service and simultaneous
release milestones appear as dated news around the combined channel system,
not as separate storefront economies. Regional overseas markets, a larger
historical cast and additional starting dates remain deferred.

The 1996–2036 stress fixture runs every hourly step with the initial ten people
and no player-created series. It measures background-world behavior and save
continuation, not every possible forty-year player portfolio. Active market
rosters and news are bounded (500 news items); financial ledgers, archived market
identities and the event history deliberately grow to preserve ownership and
audit history. Real-time performance evidence is local to the tested machine.
