# Sub-project 10: full-career playtest design

Date: 2026-09-26. Status: implemented as an automated guided playtest; human
playtesting (T1.10) remains separate.

## Purpose

Measure the core journey of Tier 1 (T1.1) over about three in-game years before
any more Tier 1 work is chosen. The playtest answers four questions:

1. Can a sensible player reach every step of the core journey without dead ends?
2. How long does it take in real time at the fastest speeds?
3. Does the in-game guidance lead the player through each step?
4. Does the economy make sense, with meaningful risk and no traps?

## Approach

A scripted "guided player" drives the real simulation through `GameState.Apply`
and `GameState.Advance`, one hour at a time, making decisions at 09:00 each day.
It plays the way a careful new player would: follow Helper-Chan, make a doujin,
sell it, pitch to the most promising magazine, accept serialization, then hire
once the studio can afford it. It never uses knowledge a player could not have
from the interface.

This is not a substitute for a person playing. It tests the rules, pacing and
guidance flow, not whether screens are readable or fun.

## Runs

| Seed | Difficulty |
|---|---|
| 0 | Standard |
| 1 | Standard |
| 42 | Standard |
| 7 | Relaxed |
| 7 | Challenging |

Each run starts 1996-04-01 and ends 1999-04-01.

## Measurements

- Milestone dates and estimated real time at the fastest speeds (4.5 seconds per
  game hour while anyone controlled is working, 0.078 seconds otherwise).
- Helper-Chan guidance step changes.
- Monthly personal and business money, serialized series, staff and guidance.
- Every rejected player action, grouped by message, as a friction signal.
- Series results and event counts.
- Save integrity: the final state must survive a JSON round trip and a full
  replay of the command timeline unchanged.

## Output

One Markdown report per run in `TestResults/career-playtest/`, and a findings
record in `docs/superpowers/full-career-playtest-findings.md` that maps results to
the Tier 1 checklist.
