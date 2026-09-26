# Tier 1 fix 6: mid-career pacing, completion

Date: 2026-09-27. Design: [considerations](specs/2026-09-27-mid-career-pacing-considerations.md)
(Q26 to Q28). Plan: [implementation plan](plans/2026-09-27-mid-career-pacing.md).
Not yet committed.

## What changed

- **A fifth speed, 32x.** The header has a "32×" button after 8x, and the 1
  and 2 keys step through 1, 2, 4, 8 and 32x. Its tooltip says routine days
  skip ahead and Helper-Chan stops you when anything needs you. The game
  remembers the last daytime speed below 32x.
- **Routine days run on at 32x.** The daily recap and "chapter completed" go
  to the inbox without pausing, and the working day rolls straight into the
  overnight skip; the next morning continues at 32x.
- **Anything that needs the player still stops the game** (Q27): industry
  decisions, serialization offers, pitch rejections, redo requests,
  cancellation warnings and cancellations, missed deadlines and issues,
  chapters at risk, missed paydays and new Helper-Chan texts. The game pauses
  and resumes at the remembered slower speed. Other events keep their
  Auto-pause setting. The list is `CareerGuidance.FastSpeedStops`.
- **8x and below are unchanged**, including the 45-second working day and the
  recap at the end of each day.
- **Office motion** is capped at 8x during 32x days, as during the overnight
  skip, so characters do not jitter.
- **Helper-Chan's introduction** (Q28). After the first sale, the first whole
  working day with no stop and no unread text brings one text suggesting 32×.
  It is sent once, and choosing 32x yourself first counts as the
  introduction. It stays within the 140 character limit.
- **Playtest harness.** Each report has a "Pacing by speed" table: working
  hours, estimated real time and recap clicks at 8x, and real time and stops
  at 32x, per career year. Seed 7 on Standard was added to the runs.
- No simulation, balance or save change. The guidance text is stored in the
  existing guidance thread.

## Verification

Automated and rendered checks only; no human playtesting.

- `dotnet build MangakaGame.sln -warnaserror`: no warnings or errors.
- `dotnet test --filter Category!=Playtest`: 590 tests pass, including 7 in
  `MidCareerPacingTests.cs`: the text appears once after a quiet day, waits for
  the first sale, waits for unread texts and days with a stop, does not repeat
  a career step, counts as seen when 32x is chosen first, the stop list, and
  read-only guidance.
- Headless Godot checks all pass: `--smoke-test`, `--management-smoke`,
  `--usability-smoke` (50), `--atmosphere-smoke` (885), the new
  `--quiet-speed-smoke` (1,175), `--convenience-smoke` (25), `--alpha-smoke`
  (45), `--family-home-smoke` (75), `--office-life-smoke` (734),
  `--production-smoke` (21), `--progression-smoke` and
  `--series-status-smoke` (23).
- The usability smoke found that the 1 and 2 keys did not reach 32x (plan task
  1 had only been done for the header). Fixed in `DebugMain.Usability.cs` and
  all smokes re-run.
- The quiet-speed smoke drives the real-time loop frame by frame: one 8x
  working day took 45.0 real seconds and ended with the recap; one 32x working
  day took 11.3 real seconds, rolled into the night without the recap, and the
  next morning continued at 32x with no clicks. Staged serialization offer,
  cancellation warning and missed deadline events stopped the game and resumed
  at 4x; a completed chapter did not stop it.
- Rendered `--quiet-speed-smoke --capture` viewed at 1920x1080 and at 1280x720
  with 150% text (`TestResults/quiet-speed-phone-*.avif`,
  `quiet-speed-header-*.avif`): Helper-Chan's 32x text is readable on the
  phone, and the 32× button is visible and highlighted. At 720 with 150% text
  the speed buttons wrap to a second header row, which is readable.
- Career playtest re-run (6 runs, to April 1999). Estimated real time per
  career year, excluding reading and menus:

| Run | 8x, hours per year | 8x recap clicks per year | 32x, hours per year | 32x stops per year |
|---|---|---|---|---|
| Seed 0 Standard | 4.0 | 119 to 154 | 1.1 | 25 to 52 |
| Seed 7 Standard | 4.0 | 114 to 141 | 1.1 | 16 to 40 |
| Seed 1 Standard | 4.0 | 127 to 146 | 1.1 | 10 to 41 |
| Seed 42 Standard | 4.0 | 126 to 146 | 1.1 | 6 to 26 |
| Seed 7 Relaxed | 4.0 | 131 to 150 | 1.1 | 8 to 28 |
| Seed 7 Challenging | 4.0 | 110 to 194 | 1.1 | 27 to 70 |

  Most stops are Helper-Chan texts, mainly in the first year. The 32x figure
  assumes every working day runs at 32x; days with a stop that the player
  finishes at 8x add up to about half an hour a year, so 1.1 to 1.6 hours per
  year is the realistic range. Three in-game years drop from about 12 hours to
  about 3.5 to 5 hours, and the recap clicks (about 130 a year, fewer than the
  300 estimated) disappear on routine days.

## Known issues

- **Challenging brings more stops.** Seed 7 Challenging stops about 70 times
  in its second year, 54 of them Helper-Chan texts. That is about one stop
  every five days, still far fewer than the daily recap. Left for the
  fresh-player test (T1.10).
- **The harness estimates real time from the driver's constants**, confirmed
  by the one-day measurements above, not by a full playthrough.

## Next step

T1.9 display sweep, then preparing the fresh-player test (T1.10).
