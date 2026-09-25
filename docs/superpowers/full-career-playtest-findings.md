# Full-career playtest findings

Date: 2026-09-26. Source: automated guided playtest
(`tests/MangakaSim.Tests/CareerPlaytest.cs`), five careers of three in-game
years each (seeds 0, 1 and 42 on Standard; seed 7 on Relaxed and Challenging).

What this is: simulation runs driven by a scripted careful player. What it is
not: human playtesting, rendered screen checks or clean-machine testing.

## Summary

The core journey works end to end and saves are sound, but four problems stand
between the alpha and a finished Tier 1: guidance stops after the first sale,
the first hire is a trap, nothing ever goes wrong once serialized, and the
middle of the career is slow in real time.

## Findings by checklist item

### T1.1 Core journey: mostly met

Every run reached the first doujin, first sale, pitch, serialization, first
magazine chapter and first hire. No run ever saw a setback: zero cancellations,
cancellation warnings or missed deadlines. The "setback such as cancellation"
step of the journey is never experienced by a sensible player.

### T1.2 Taught in the game: not met

Guidance moves create, produce, print, direction, then settles on "Plan your
next book or print run" from day 8 and never changes for three years. It never
mentions pitching, magazines, serialization, deadlines or hiring. A player who
follows Helper-Chan alone would never pitch.

### T1.4 Economy sense: not met

- **First-hire trap.** The game lets the player hire a ¥144,000 to ¥175,000 a
  month assistant with about ¥300,000 in the business account and no income for
  about four months. The account reaches ¥0, wages go unpaid for months and the
  assistant quits in four of five runs. Rehiring then fails 19 to 96 times with
  "Reserve seven days' salary before hiring", and recruiting fails with
  "Recruitment needs ¥20,000 after reserved wages". No warning is given before
  the hire.
- **Too generous once serialized.** The first series sold 522,000 to 841,000
  copies in about 2.5 years. Final balances were ¥10M to ¥18M business and ¥5M
  to ¥8M personal. After the first year, money stops mattering.
- **Easy first pitch.** A tier 3 pitch at about 31% estimated chance succeeded
  first time in four of five runs.
- **Weak doujin sales.** 80 copies across three print runs. Low sales are
  realistic, but the doujin path gives little sense of growth.
- **Difficulty has no market effect.** Relaxed and Challenging ended with the
  same 724,717 copies sold. Difficulty only changes starting cash, some
  discretionary costs and recovery time.

### T1.5 Pacing: partly met

- Opening: first sale on day 7, about 4 real minutes at the fastest speed. This
  is well inside the 15 to 20 minute target once reading and menus are added.
- Three years take about 12.1 real hours at the fastest speeds, about 4 hours a
  year, almost all during working hours. The overnight skip saves little.
- Dead zone: about 110 in-game days (about 1.2 real hours) between accepting
  serialization and the first magazine chapter, with little to do.

### T1.6 Saves: met in simulation

All five final states survive a JSON round trip and a full replay of the
command timeline unchanged. Loading at each journey step inside the game still
needs checking on screen.

### T1.7 Stability: met in simulation

No exceptions across 15 in-game years of play.

### T1.8 Extra systems: no blockers seen

Awards (January 1997), licence offers (January to May 1997), editor redos and a
studio move to Nerima (¥60,000 a month) all occurred without blocking the core
journey.

### Other observations

- Seeds 1, 7 and 42 produced nearly identical careers: same genre, same
  magazine, similar dates. Early market variety is low.
- A second hire needs a move or more furniture ("This workplace has no free
  desk"). This worked and felt like a sensible growth step.

T1.3, T1.9 and T1.10 need rendered checks and human players; this playtest
cannot measure them.

## Recommended Tier 1 fixes, in priority order

1. **Guidance after the first sale.** Add Helper-Chan steps for pitching,
   waiting for an answer, accepting serialization, the first deadline, and
   considering a first hire, including what to do after a rejection.
2. **First-hire safety.** Show the monthly cost against current income and cash
   runway before hiring, warn clearly when the studio cannot cover about three
   months, and make the unpaid-wage state explain how to recover.
3. **Setbacks and economy balance.** Tune serialized sales and income so a debut
   series can struggle, make cancellation warnings reachable for middling work,
   and lower the first-pitch success rate slightly.
4. **Pre-publication gap.** Give the player useful things to do between
   acceptance and the first issue, or shorten the gap, and explain the wait.
5. **Difficulty that matters.** Let difficulty adjust market demand or
   cancellation pressure, not only costs.
6. **Mid-career pacing.** Consider a faster speed for quiet stretches when no
   decision is pending.

Each fix should be followed by a re-run of the playtest to confirm the change.

## Re-run after fix 1, career guidance (2026-09-26)

The playtest bot now follows Helper-Chan's targets after the first sale
instead of a scripted route. Results on the Standard preset:

- Guidance moved on at every point: continue as a series on day 7, pitch on
  day 8 (Monthly Hoshigaku Flowers, shown chance 60%), serialization offer on
  day 31, first hire on day 171.
- First hire day by difficulty: Challenging day 227, Relaxed day 143.
- T1.2 is now met for the steps up to a first hire in simulation. It still
  needs a fresh player (T1.10) to confirm.

New friction found:

- The 1997 growth hire has no free desk; guidance points to recruitment
  before the player can seat anyone. Fix 2 (first-hire safety) should cover
  this.
- On Challenging, repeated recruitment attempts hit the recruitment cooldown
  with no explanation of when to try again.

Unchanged: early careers are nearly identical across seeds, and the studio
holds about ¥18.6 million by 1999, so fixes 2 and 3 are still needed.

## Re-run after fix 2, first-hire safety (2026-09-26)

All five runs pass (seeds 0, 1 and 42 on Standard, seed 7 on Challenging and
Relaxed). Guidance now waits for a safe runway, counting confirmed page fees,
and a free desk.

- First hire on day 143 on every preset, the day after the first magazine
  chapter is published (day 142). Before fix 2 it was day 171 on Standard,
  227 on Challenging and 143 on Relaxed.
- No missed paydays in any run, so the new missed-payday text never fired in
  the playtest. It is covered by unit and smoke checks instead.
- After the first hire, business cash on seed 0 falls from ¥317,685
  (September 1996) to ¥81,851 (February 1997) before the first collected
  edition lifts it to ¥796,777 in March. Safe, but thin.
- The recruitment cooldown is now explained; no run hit it.
- The bot's scripted 1997 growth hire still meets "This workplace has no free
  desk" once before moving to Nerima.

Unchanged: early careers are nearly identical across seeds, no setbacks
occur, and the studio holds about ¥18.7 million by April 1999. Fix 3
(setbacks and economy balance) is next.

