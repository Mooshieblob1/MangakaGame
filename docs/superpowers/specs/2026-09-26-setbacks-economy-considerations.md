# Tier 1 fix 3: setbacks and economy balance, considerations

Date: 2026-09-26.
Status: approved 2026-09-26; see the [implementation plan](../plans/2026-09-26-setbacks-economy.md)
and the [completion record](../setbacks-economy-completion.md). Source findings: no setbacks occur and
the economy is too generous once serialized ([full career playtest findings](../full-career-playtest-findings.md)).

## Problem

A sensible player never meets a setback. Across five three-year careers
there were no rejections after the first pitch, no cancellation warnings and
no cancellations. The first tier 3 pitch succeeded at a shown chance of 60%.
Once serialized, a debut series sold about 750,000 copies and the business
held about ¥18.7 million by April 1999 on seed 0 Standard, so money stops
mattering after the first year. At the other end, the five months after the
first hire are thin: the business falls from about ¥318,000 to ¥82,000 before
the first collected edition pays out.

A layout problem from fixes 1 and 2 is included: at 1280x720 with 150% text,
Helper-Chan's phone covers the right half of the Recruitment runway line.

## Decisions

### Q19. Setbacks: a risk you can see and respond to (decided 2026-09-26)

- The first pitch at debut quality is roughly a coin flip, so about half of
  careers see a rejection.
- After the protected opening chapters, average work slips below the
  cancellation line often enough that a warning is common in the first
  serialized year.
- A series is cancelled only if it does not improve after a warning.
- Tradeoff accepted: a setback is likely but not guaranteed.

### Q20. Economy: smaller, steadier income (decided 2026-09-26)

- Collected edition sales depend on the size of the magazine: a tier 3
  monthly sells far fewer copies than a tier 1 weekly. Readership grows more
  slowly from volume sales, weakening the snowball.
- Royalties are paid per print run: the first print run is paid at release
  and each reprint when sales pass it, as Japanese publishers traditionally
  do, rather than per copy each week.
- Newcomer page fees rise slightly, so the months after the first hire hold
  level instead of draining.
- Target: ¥2 million to ¥5 million in the business by 1999 for an average
  debut on Standard, with a better series clearly ahead of that.
- Rejected: simply scaling income down (keeps the snowball), and letting
  difficulty change demand (left for the "difficulty that matters" fix).

### Q21. Responding to a warning: use the tools the game already has (decided 2026-09-26)

- When a warning arrives, Helper-Chan explains the rank against the line,
  roughly how many issues remain before the editor decides, the weakest
  factor (quality, readership or genre fit) and a concrete fix: more time per
  chapter, a stronger assistant or fewer pages.
- The player can end the series on their own terms with the existing action.
- After a cancellation she explains what is kept (volumes keep selling), the
  52-week wait at that magazine, and the next options: another magazine or a
  new series.
- Nothing new is built. Rejected: a new editor meeting screen.

## Gameplay estimates

These values are gameplay estimates tuned by the automated playtest, not
historical figures.

- Pitch base chance by tier: 12%, 24% and 40% (was 15%, 30% and 50%).
- Protected opening: 6 published chapters (was 8). The warning comes sooner
  after slipping below the line.
- Tier 2 and 3 magazines start with stronger competing series.
- Collected edition demand scales by magazine tier; per-copy royalties are
  replaced by print-run royalties. Digital and overseas income stays per unit.
- Page fees are offered as if reputation were at least 50 (tuned up from
  the planned 30 during playtesting).
- Reader surveys weigh readership at 60% and craft at 40%.
- Standard magazine page counts per issue, used to scale collected edition
  demand: weekly 19, biweekly 24, monthly 32.
