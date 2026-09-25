# Tier 1 fix 2: first-hire safety, considerations

Date: 2026-09-26.
Status: implemented 2026-09-26; see the [implementation plan](../plans/2026-09-26-first-hire-safety.md)
and the [completion record](../first-hire-safety-completion.md). Source finding: the first hire is a cash
trap ([full career playtest findings](../full-career-playtest-findings.md)).

## Problem

The game lets the player hire a ¥144,000 to ¥175,000 a month assistant with
about ¥300,000 in the business account and no income for about four months.
Wages go unpaid and the assistant quits in four of five playtest runs. The
recovery tools already exist (contributing personal savings to the business,
dismissing staff, credit), but nothing points to them. The guidance re-run
added two smaller problems: the 1997 growth hire can be suggested before a
free desk exists, and on Challenging, recruitment cooldowns are unexplained.

## Decisions

### Q16. Protection: warn and confirm (decided 2026-09-26)

- The hiring screen shows the monthly wage against current income and how many
  months the business cash will last with the new hire.
- Below about three months, a clear warning appears and the hire needs a
  confirmation. Hiring is never blocked by this check.
- Helper-Chan suggests a hire only once there is a free desk and the money to
  cover it.
- If wages go unpaid, she explains how to recover: contribute personal
  savings, reduce costs or let someone go, and what happens if nothing changes.

### Q17. Runway income: cash plus confirmed magazine income (decided 2026-09-26)

- Months of runway count the business account plus page fees from a signed
  serialization due in the next three months.
- Doujin sales, conventions and other irregular income are not counted.
- Costs are the new wage plus existing wages, rent and other recurring bills.
- Helper-Chan's hire suggestion uses the same rule, so her advice and the
  hiring warning never disagree.

### Q18. Missed payday: a text with a one-tap fix (decided 2026-09-26)

- When wages go unpaid, Helper-Chan texts at once with the amount owed and the
  days left before the staff member warns and then leaves.
- Her reply button "Cover from savings" moves exactly the owed amount from
  personal savings to the business, only when the player taps it.
- If savings cannot cover it, she points to letting someone go or borrowing.
- Personal savings are never used automatically, keeping the approved
  separation of personal and business money.
