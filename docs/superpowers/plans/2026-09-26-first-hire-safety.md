# Tier 1 fix 2: first-hire safety, implementation plan

Date: 2026-09-26. Design: [first-hire safety considerations](../specs/2026-09-26-first-hire-safety-considerations.md)
(Q16 to Q18). Finding addressed: the first hire is a cash trap
([full career playtest findings](../full-career-playtest-findings.md)).
Status: implemented 2026-09-26, not committed. See the
[completion record](../first-hire-safety-completion.md).

## Part A. Runway calculation (simulation, read-only)

New read-only query `GameState.HiringRunway(long extraMonthlySalary)` in
`src/MangakaSim/GameState.Studio.cs`, returning a small record:

- `Cash`: `AvailableBusinessCash` for the controlled business (already nets
  out arrears and the seven-day hiring reserves of recent hires).
- `ConfirmedIncome`: page fees for signed serializations of the controlled
  business whose issue closes fall in the next 90 days, counted at chapter
  pages times `Contract.FeePerPage`, less the 20% creator share bill that
  publication already raises. Doujin, convention, online, licensing and other
  irregular income is excluded.
- `MonthlyCosts`: existing staff salaries, plus the extra salary being
  considered, plus monthly rent of the business's locations, plus recurring
  bills and loan repayments averaged to a month.
- `Months`: `(Cash + ConfirmedIncome) / MonthlyCosts`, or "no costs" when
  costs are zero.
- `Safe`: `Months >= 3` (constant `StudioRules.SafeRunwayMonths = 3`).

No state is changed, so saves, replays and determinism are unaffected. The
AI affordability rule in `GameState.Properties.cs` stays as it is.

## Part B. Hiring screen (Godot, `godot/DebugMain.Staff.cs` and the Recruitment page)

1. Beside the salary offer, show live: this wage per month, total monthly
   costs after hiring, confirmed income over three months, and the runway in
   months, updating as the salary or candidate changes.
2. When the runway is under three months, the hire button opens a
   confirmation: "Business funds cover about N months of wages and costs.
   If a payday is missed, this assistant may leave. Hire anyway?" with Hire
   and Cancel. It never blocks; the existing simulation checks (free desk,
   salary range, seven-day reserve, acceptance) are unchanged.
3. When the workplace has no free desk, say so before the player picks a
   candidate, with a button to Furniture (or Properties if the room is full).
4. The recruitment search button shows its cooldown date when it is
   unavailable: "Next search available from 14 Jun 1997."

## Part C. Guidance (`src/MangakaSim/Guidance.cs`)

1. The `first-hire` step uses `HiringRunway` with the cheapest expected wage
   instead of the plain "three months of cash" check, and also requires a
   free desk. Texts stay within the 140-character limit.
2. New `first-hire-desk` variant: the money is there but no desk is free.
   Target Furniture.
3. `serial-rhythm` states the runway target as "about three months of wages
   and costs, counting confirmed page fees".
4. If a recruitment search is cooling down, the hire step says when the
   next search opens.

## Part D. Missed payday text (Q18)

1. When `EventType.WageArrears` fires for the controlled business, the
   driver adds a Helper-Chan phone text at once, and opens the phone with a
   buzz: amount owed, to whom, and days left before the warning (14 days)
   and notice (28 days).
2. A contextual reply button "Cover from savings (¥N)" appears while arrears
   remain and personal savings cover them. On tap only, it applies
   `ContributeFundsCommand(WageArrears)`; settlement then pays the arrears
   normally. Savings are never moved automatically.
3. If savings are short, the text explains the other routes: contribute part,
   reduce costs, dismiss staff in Staff, or borrow in Finances. A "Show me"
   reply opens Staff.
4. Presentation only: the text lives in the guidance thread (`Say`), and the
   reply issues an ordinary command, so replays stay deterministic. The
   message uses a new step id `wage-arrears` so it is not duplicated on
   repeated events for the same obligation.

## Part E. Tests and verification

- Unit tests (`tests/MangakaSim.Tests/GuidanceTests.cs` and a new
  `HiringRunwayTests.cs`): runway with no contract, with a signed contract,
  with rent and loans, and excluding doujin income; guidance suggests a hire
  only with runway and a free desk; the desk variant; the cooldown text;
  contributing the owed amount clears arrears at the next settlement.
- Smoke checks: runway figures on the Recruitment page, the confirmation
  under three months, the cooldown date, the arrears text and the "Cover
  from savings" reply.
- Rendered captures (AVIF) of the Recruitment page and the arrears text at
  1920x1080 and 1280x720 with 150% text.
- Re-run the career playtest (`--filter Category=Playtest`) on all difficulty
  presets and record the first-hire day and any arrears in the findings.
- Completion record: `docs/superpowers/first-hire-safety-completion.md`.

## Out of scope

Rebalancing salaries, fees or the economy (that is fix 3, economy and
setbacks). The unused approach-fee rules in Rivals stay as they are.
