# Tier 1: mid-career pacing, considerations

Date: 2026-09-27.
Status: design agreed 2026-09-27 (Q26 to Q28); implemented 2026-09-27; see the
[completion record](../mid-career-pacing-completion.md).
Plan: [implementation plan](../plans/2026-09-27-mid-career-pacing.md). Source
finding: item 6, "Mid-career pacing", in the
[full career playtest findings](../full-career-playtest-findings.md).

## Problem

The playtest harness measured about 4 real hours per in-game year at the
fastest speed, 8x. Once a series is running with a buffer, most working days
need nothing from the player: the studio draws, the recap appears, the player
clicks Continue, the night passes. On top of the 4 hours, the daily recap
pauses the game about 300 times a year, and the harness does not count that
clicking. A Tier 2 balance target of 5 to 10 in-game years would mean 20 to 40
real hours of mostly waiting.

The 45-second working day at 8x was the user's deliberate choice
([character integration and day length](../character-integration-and-day-length.md))
and suits the opening, when every day matters. The problem is the stretch in
the middle of a career where days are routine.

## Numbers

From the real-time driver (`SecondsPerHourAt1x = 36`, 10-hour working day,
overnight skip at 32x with its own 2.5-second baseline, about 1 second):

| Speed | Working day | In-game year (about) |
|---|---|---|
| 8x (today's fastest) | 45 s | 4 h, plus about 300 recap clicks |
| 32x (new) | about 11 s | 1 h, no routine clicks |

These are estimates from the constants, not measurements. The implementation
measures them.

## Decisions

### Q26. A fifth speed, 32x, for quiet stretches (decided 2026-09-27)

- Add 32x after 8x in the header speed buttons and the 1 and 2 keys.
- Anything that needs the player drops the game back to the speed they used
  before 32x (or pauses, as today) so they are never rushed through a decision.
- 8x and below behave exactly as today, keeping the 45-second day.
- Rejected: making 8x faster (changes the approved day length for everyone),
  and an automatic "skip to next event" button (hides the living office and
  gives less control over how fast time runs).

### Q27. At 32x, routine stops do not pause (decided 2026-09-27)

- The daily recap and "chapter completed" go to the inbox instead of pausing,
  and the working day rolls straight into the overnight skip, then the next
  morning continues at 32x.
- It still stops, and returns to the previous speed, for: industry decisions,
  serialization offers, pitch rejections, editor redo requests, cancellation
  warnings, series cancelled, missed deadlines, missed issues, chapters at
  risk, missed paydays and new Helper-Chan texts. Other events keep their
  Auto-pause setting.
- 8x and below still pause on the daily recap as today.
- Rejected: pausing on the recap at 32x too (keeps the 300 clicks a year), and
  a separate setting to choose which events interrupt (more settings for a
  new player to learn; the existing Auto-pause list stays for other events).

### Q28. Available from the start, introduced once (decided 2026-09-27)

- 32x is available from the first day, like the other speeds.
- The first time a whole working day passes with nothing needing the player,
  Helper-Chan sends one text, for example: "Nothing needs you right now. Try
  32× to skip ahead; I'll stop you if anything comes up."
- Rejected: unlocking 32x later (a hidden rule the interface would have to
  teach), and no introduction (players would not learn that 32x is safe).

### Office animation at 32x (implementation choice, 2026-09-27)

During 32x working days the office animation is capped at 8x motion, as the
overnight skip already does, so characters do not jitter across the room.

## Scope limits

- Presentation only. No simulation, balance or save change. Simulation
  results for a given sequence of commands are unchanged, so determinism is
  kept; the playtest harness is unaffected except for the new measurement.
- The Helper-Chan text is read-only guidance in `CareerGuidance`, stored in
  the existing guidance thread, which already saves.
- No new settings.
