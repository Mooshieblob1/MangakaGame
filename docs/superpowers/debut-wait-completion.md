# Tier 1 fix 5: the wait before a series debuts, completion

Date: 2026-09-26. Design: [considerations](specs/2026-09-26-debut-wait-considerations.md)
(Q24 and Q25). Plan: [implementation plan](plans/2026-09-26-debut-wait.md).
Not committed yet.

## What changed

- **The four-issue lead time stays.** No save change and no balance change.
- **Helper-Chan explains the wait.** Until the stock is ready, her texts give
  the magazine, the date the first issue closes, "x of y chapters ready", why
  editors want chapters early (a missed issue is covered) and that page fees
  only arrive after chapters publish.
- **Chapters ready ahead.** `GameState.ChaptersReadyAhead(series)` counts
  finished, unpublished magazine chapters against the buffer, using the
  planner's own rule. Production's series status shows "Chapters ready ahead:
  x of y" before and after the debut.
- **One suggestion once the stock is ready**, plus a list of the other ideas
  and a tip to raise the buffer to 4. The first match wins:
  1. Early hire, only when the runway is safe and cash alone lasts until the
     debut plus one month (at least 3 months). Wages fall due before the first
     page fee, so future fees do not count here. With no free desk the text
     points to Furniture first.
  2. Convention, when doujin copies are unsold, nothing is booked and the
     business can pay the booth and return travel now. A major event is
     named if it falls before the debut, otherwise the next regional one,
     with the date booking opens.
  3. Short doujin or one-shot, once per wait: starting one after signing
     stops this suggestion even after it is finished.
  4. Part-time job, or a reminder that the current one keeps money coming in.
- **Texts fit the phone.** Each text stays within 140 characters, and each
  suggestion has its own step id so the phone sends a new text only when the
  situation changes.
- **Playtest harness.** The report records "Pre-debut stock ready" and what
  the bot did with the wait. The bot follows each suggestion as a player would
  and, like a player reading the storage line, does not order copies it has no
  room for.

## Verification

Automated and rendered checks only; no human playtesting.

- `dotnet build MangakaGame.sln -warnaserror`: no warnings or errors.
- `dotnet test --filter Category!=Playtest`: 583 tests pass, including 7 in
  `DebutWaitTests.cs`: the ready count and a changed buffer, the explanation
  texts, the suggestion order, the convention affordability check, the early
  hire cash check, the card ending at the first chapter, and read-only
  guidance.
- Headless Godot checks: `--smoke-test` 823, `--management-smoke` 99 and
  `--series-status-smoke` 23 checks pass.
- Rendered `--series-status-smoke --capture`: 28 checks pass.
  `TestResults/series-status-debut-wait-1080.avif` and
  `series-status-debut-wait-720-150.avif` viewed: the phone texts and the ready
  line fit at 1920x1080 and at 1280x720 with 150% text.
- Career playtest re-run (5 runs, to April 1999). A first attempt suggested an
  early hire on future page fees and conventions the business could not pay
  for; both checks above were added and the runs repeated:

| Run | Stock ready | Hire | Business in August 1996, fix 4 then now | Unpaid wages or rejected bookings |
|---|---|---|---|---|
| Seed 0 Standard | Day 49 | After the debut, as before | 283,018 to 276,026 yen | None |
| Seed 1 Standard | Day 79 | After the debut, as before | 314,522 to 307,498 yen | None |
| Seed 42 Standard | Day 49 | After the debut, as before | 283,186 to 276,362 yen | None |
| Seed 7 Relaxed | Day 51 | Day 51, before the debut | 582,682 to 288,971 yen | None |
| Seed 7 Challenging | Day 79 | After the debut, as before | 163,346 to 156,890 yen | None |

  Debut dates are unchanged (day 142 or 172). Each run now books a convention,
  draws a side doujin and takes the part-time job during the wait, so personal
  savings in April 1999 are 1.1 to 2.3 million yen higher than before. Business
  money in April 1999 is within about 0.6 million yen of the fix 4 runs, except
  seed 7 Challenging, which ends at about 4.0 million yen instead of 0.18
  million. Seed 7 Relaxed has less business money at the debut because it
  hires early, as intended. The first attempt had left seed 0 and seed 42 with
  about 5,000 yen and seed 7 Challenging with nothing.
  The first attempt's 33 rejected bookings, rejected print orders and unpaid
  wages are gone; only pitch rejections remain.

## Known issues

- **Convention and job texts alternate.** In the last weeks before the debut
  the suggestion switches between a convention and the part-time job about
  once a week, as each regional event passes with copies still unsold (8 of
  each in seeds 1 and 7). Each text is correct, but it may feel like nagging.
  Review with fresh players (T1.10) before changing it.
- **Seed 0 now meets pitch rejections.** The extra activity during the wait
  changed the random order, and three pitches were rejected in 1997 (none in the fix 4
  run). Its cancellation date is unchanged. This is normal variation, not a regression.
- **The generic hiring runway still counts page fees before the debut.** The
  debut guidance now checks cash, but the first-hire runway warning in
  Recruitment does not. Seed 7 Relaxed hires before the debut with enough cash,
  so no run shows a problem; revisit if fresh players hit unpaid wages.
