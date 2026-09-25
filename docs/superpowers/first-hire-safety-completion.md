# Tier 1 fix 2: first-hire safety, completion

Date: 2026-09-26. Design: [considerations](specs/2026-09-26-first-hire-safety-considerations.md)
(Q16 to Q18). Plan: [implementation plan](plans/2026-09-26-first-hire-safety.md).
Not committed yet.

## What changed

- **Runway calculation.** A new read-only query, `GameState.HiringRunway`,
  adds business cash to confirmed magazine page fees for the next 90 days
  (after the 20% creator share) and divides by monthly costs: salaries, the
  wage being considered, rent, utilities and loan repayments. Three months or
  more counts as safe. Doujin, convention and licensing income is not counted.
  Nothing in the save changes.
- **Hiring screen.** The Recruitment page shows the chosen wage, monthly costs
  after hiring, confirmed page fees and the runway in months, updating as the
  candidate or offer changes. Below three months the line turns amber and the
  hire button asks first: "Business funds cover about N months of wages and
  costs..." with Hire and Cancel. It never blocks the hire.
- **Desk warning.** When the workplace has no free desk the page says so before
  a candidate is chosen, with a button to Furniture, or to Properties when the
  room is full.
- **Search cooldown.** The recruitment search button is disabled during the
  14-day cooldown and shows the next search date.
- **Guidance.** The first-hire step waits for a safe runway and a free desk.
  A new `first-hire-desk` step points to Furniture when the money is there but
  no desk is free. `serial-rhythm` explains the three-month target, and the
  first-hire text gives the cooldown date when no search can start.
- **Missed payday (Q18).** When a payday is missed, Helper-Chan texts at once:
  the amount owed, and the dates of the warning, the work stoppage and the
  notice. If personal savings cover it, a "Cover from savings (¥N)" reply moves
  exactly that amount into the business on tap only. Otherwise the text points
  to contributing part, cutting costs, Staff and borrowing. The text is sent
  once per missed payday.

## Verification

Automated and rendered checks only; no human playtesting.

- `dotnet build MangakaGame.sln -warnaserror`: no warnings or errors.
- `dotnet test --filter Category!=Playtest`: 564 tests pass, including the new
  `HiringRunwayTests` (runway with and without a contract, rent and loans,
  doujin income excluded, desk and runway gating of guidance, cooldown text,
  missed-payday texts with and without savings, cover clears the arrears).
- Headless Godot checks: `--smoke-test` 823, `--management-smoke` 99,
  `--alpha-smoke` 40 and usability 48 checks pass. New alpha checks cover the
  runway line, the desk warning, the short-runway confirmation, hiring anyway,
  the missed-payday text, the cover button and the cooldown date.
- Rendered alpha smoke with captures (56 checks at the time of capture, before
  the cooldown check was added) in `TestResults/first-hire-safety/`:
  `alpha-hiring-runway-1080`, `alpha-hiring-runway-720-150`,
  `alpha-hiring-runway-warning` and `alpha-wage-arrears` (AVIF). Viewed: the
  runway line and the warning read correctly at 1920x1080, and the arrears
  text, cover button and savings header agree.
- Career playtest re-run on all five seeds and presets; see the
  [findings](full-career-playtest-findings.md). First hire on day 143 on every
  preset, with no missed paydays.

## Known issues

- At 1280x720 with 150% text, Helper-Chan's phone covers the right half of the
  runway line when both are open. The phone can be closed, but the page should
  make room for it. Carried over from fix 1.
- The studio's cash falls from about ¥318,000 to ¥82,000 over the five months
  after the first hire before the first collected edition pays out. It never
  misses a payday, but the margin is thin. Fix 3 (economy) should look at this
  together with the much larger late surpluses.
- The playtest bot's scripted 1997 growth hire still meets "This workplace has
  no free desk" before moving to Nerima. The message is now clear and the bot
  recovers; guided players are sent to Furniture first.
