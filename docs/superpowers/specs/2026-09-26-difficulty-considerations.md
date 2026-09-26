# Tier 1 fix 4: difficulty that matters, considerations

Date: 2026-09-26.
Status: approved 2026-09-26; see the [implementation plan](../plans/2026-09-26-difficulty.md)
and the [completion record](../difficulty-completion.md). Source finding: difficulty only
changes money ([full career playtest findings](../full-career-playtest-findings.md)).

## Problem

The difficulty presets changed opening funds, a few optional costs and the
grace before unpaid rent closes a workplace. Nothing that decides whether a
series survives was affected. After fix 3, seed 7 was warned and cancelled on
the same days on Relaxed and Challenging, so the choice on the setup screen
promised a gentler or tougher career that the game did not deliver.

## Decisions

### Q22. Widen the two existing dials (decided 2026-09-26)

- Keep the two Custom settings the game already saves, Business pressure and
  Recovery grace, and make them reach the systems that create setbacks.
- Recovery grace also sets how long editors wait before warning and
  cancelling, and how many chapters protect a new serialization.
- Business pressure also sets how strong rival series are in reader surveys
  and how often editors accept pitches.
- Relaxed and Challenging set both dials, as before. No new setting, no save
  change.
- Rejected: new separate settings (more to explain on the setup screen), and
  changing sales demand (fix 3 has just been balanced around it).

### Q23. Noticeable but fair magnitudes (decided 2026-09-26)

| Effect | Low / short | Standard | High / long |
|---|---|---|---|
| Editor warning and cancel waits (Recovery) | x0.75 | x1 | x1.5 |
| Protected opening chapters (Recovery) | 4 | 6 | 9 |
| Rent grace (Recovery) | as before | as before | as before |
| Rival series strength (Pressure) | x0.9 | x1 | x1.1 |
| Pitch acceptance odds (Pressure) | x1.15 | x1 | x0.85 |
| Optional costs (Pressure) | x0.85 | x1 | x1.15 |

- Relaxed is low pressure and long recovery; Challenging is high pressure and
  short recovery.
- Standard is unchanged.
- Editor patience, protected chapters and pitch odds apply only to the
  player's own business. Rival strength applies to the magazines' rival
  series, which compete with everyone.
- A changed setting applies from then on. Warnings already issued keep their
  date.
- Tradeoff accepted: a skilled player still wins on Challenging; the presets
  shift how much room there is for mistakes, not whether success is possible.

## Gameplay estimates

These are gameplay estimates checked with the automated playtest, not
historical figures.

- Editor waits are counted in issues. A shorter wait rounds down and a longer
  wait rounds up, so even the two-issue newcomer warning changes: one issue on
  Challenging, three on Relaxed. The cancel wait for a newcomer is two, three
  or five issues.
- The pitch chance shown on Helper-Chan's phone includes the preset.
