# Sub-project 3 implementation plan

Started 2026-09-23 following the user's instruction to start work.
Design: [Staff and Studio](../specs/2026-09-23-staff-studio-design.md).
Research: [Tokyo/Japan evidence](../specs/2026-09-23-tokyo-research.md).

Implement in coherent, tested slices. Preserve the existing publishing and
market simulation while replacing its single-person/business assumptions.
Proposed numbers in the design are initial implementation values, subject to
documented balance findings. The parents' home always has zero rent.

## Delivery sequence

- [x] Identity, business/location accounts, personal contributions, version 3
  validation, new-game ownership choice and safe save filename.
- [x] Candidate pool/paid recruitment, hiring, salary accrual/arrears,
  dismissal/history, staff selection and team/stage assignments.
- [x] Needs, provisions, overtime limits/pay, weighted work quality,
  learning/mentoring, moonlighting, rare introductions and staff proposals.
- [x] Overlapping chapter pipeline, buffers and team-aware deadline forecast.
- [x] Credit, incorporation, royalty compensation and cashless recovery.
- [x] Sourced Tokyo property catalog, leases, moves and additional locations.
- [x] Page-based printing, stock, manual/budgeted automation and presentation.
- [x] Local conventions/travel, channel demand accounting and promotion.
- [x] Creative breakthroughs, employer offers, followers, prospective rights
  transfers and independently operating former studios.
- [x] Complete UI, cross-system replay/save validation, balance scenarios,
  rendered/headless Godot checks and milestone completion report.

## Verification

Each slice gets focused domain tests for meaningful invariants, command
atomicity and save/load/replay. Keep the full existing suite useful: change
assertions only when the approved design intentionally changes the behavior.
Do not bypass the new systems with a legacy game mode just to keep tests green.
Run the warning-as-error solution build and actual Godot scene checks after
UI integration. Record delivered scope and unfinished work here; do not call
the milestone complete until every required slice is verified.

## Delivery record — 23 September 2026

The simulation and debug UI milestone is implemented. Production, publishing,
staff, finance, property, printing, outreach and career operations share the
same tick, command log and save model. No legacy simulation mode was added.

Delivered systems:

- Separate personal/business accounts, contributions, monthly wages and creator
  shares, reserved cash, arrears, paid notice and immediate notice buyout.
- Needs/provisions/break seating, happiness/loyalty, resignation and work refusal,
  daily/weekly overtime, weighted work quality, capped skill learning, mentoring,
  personal projects and policy overrides. Paid recruitment and ordinary pools,
  rare sponsor introductions with a guaranteed hidden prodigy, staff proposals.
- Configurable unfinished pipeline, serialized buffers and unprinted-master
  limits; local team assignment, manual overrides, spare-worker assistance and
  deadline forecasts that include prerequisites, worker calendars, existing
  workload, protected breaks, travel and editor waits.
- Personal instalment/card credit, simple interest, repayment, incorporation,
  business credit, separate enduring debtor identities, four-hour recovery work.
- Six possible starting outer wards, the same two-seat parents' home at zero
  rent, sixteen rental choices, deposits/refunds, anniversary rent, moving,
  branch unlocks, staff travel, workplace closure and retained stock/liabilities.
- Print-ready masters, page-based quotes, three printer tiers, lead times,
  storage, physical stock, manual orders and bounded automatic replenishment.
  Presentation affects established titles more strongly. Creator contribution
  carries direct losses forward; book/creator entitlements are frozen.
- Free/local, paid/regional and summer/winter conventions, real time reservations,
  attendance capacity and shared weekly demand; bounded promotion/campaigns.
  Tokyo schematic map covers the 23 wards and selected nearby places, with a
  separate Ariake venue point and period-appropriate Urawa label.
- Publication-only creative breakthroughs and cooldown; employer tiers and team
  budgets; named follower invitations, saved acceptance outcomes and a seven-day
  career preview; midnight transitions to employer/new studio/family home.
  Future title rights follow the selected difficulty, contracts retain their
  terms, and historical publications/books retain their original business.
  Former studios continue production, payroll, selling and debt settlement,
  with bounded hiring/new projects and no automatic borrowing or expansion.
- Actual debug controls for the above, safe rejected commands, strict version-3
  state validation, stock/loan/account reconciliation and deterministic replay.

## Verification

- 332 simulation tests, including 43 operation cases beyond the previous
  289-test foundation suite. Coverage includes the two-year public-command
  publishing replay, both title-retention modes, credit/interest, paid stock,
  budgets, chapter pipelines, branches, rare talent, publication breakthroughs,
  monthly creator payments, historic payees, career previews and malformed saves.
- Warning-as-error solution build: zero warnings/errors.
- Godot 4.7.2 .NET editor import and actual control walkthroughs, plus a rendered
  Vulkan walkthrough on the local RTX 5070. Nine screenshots cover the existing
  production/publishing screens, staff, both management scroll positions and
  the Tokyo map. Final results: 90 headless checks and 99 rendered checks;
  no engine errors in the final runs. A headless-only popup-position fix avoids
  trying to centre a recap in a nonexistent native desktop rectangle.
- Rendered management controls and geographic view inspected for readable text,
  complete scrolling, selected destination consistency and separated balances.

## Initial implementation choices and evidence boundaries

These are prototype balance choices, not claims that the Japanese market was
priced exactly this way in 1996. The research ledger distinguishes historical
facts, modern proxies and game estimates. The accessible 2022 Tokyo transport
statistics now verifies the historical fare table previously available only
through an indexed older PDF. Rent-free starting housing remains explicit.

The forecast is conservative calendar/capacity scheduling, not a prediction of
future random editor decisions, hires or skill gains. Unfinished chapters may
finish after a buffer setting is reduced; the planner stops further creation.
A zero finished-buffer setting permits the current issue's necessary chapter.

Automatic print runs prioritise newer books and stop replenishing expired
passive-sales windows; players can manually reprint event backlist. Property
rent renews on the lease anniversary, so a late-month move cannot charge two
full rents on consecutive days. Workplace closures retain their assets and
liabilities. Returning home never grants a new cash endowment.

Credit is quoted and executed together against current balances, without a
persistent stale credit offer. Career quotes persist for seven days, and
reopening a destination never rerolls colleague acceptance. An employee who
retains a title but cannot afford a new workplace keeps its prospective rights
paused until they can establish one; the former studio cannot keep producing it.

The solo 19-page weekly scenario now has meaningful cash/time pressure: breaks,
printing and running costs replace the old free-printing, uninterrupted-work
assumptions. The regression checks continued production, pressure and one recap
per actual working day. Monthly production and the two-year serialization run
remain covered. Numbers still need broader player-driven tuning.

The UI is the existing debug harness, expanded with management and a schematic
Tokyo map. Final art, a 3D office, historical rivals, route-accurate rail travel
and exhaustive period-specific market prices are later work, not implied here.
Earlier foundation-only version-3 saves lack required fields and need a fresh
game; files are not migrated or overwritten by a failed load.
