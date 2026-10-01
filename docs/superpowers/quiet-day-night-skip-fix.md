# Night skip on days with no work, fix

Date: 2026-09-29. Reported by the user from a local problem report (career at
17 April 1996): "sometimes when it hits night without me doing anything it
doesn't do the timeskip, make it always timeskip at night". Not yet committed.

## Cause

The night skip began only from the day's recap: after "Day done" at 32x, or
after Continue on the recap at 8x and below. The simulation writes a recap only
when someone worked that day (`DayEndStep` in `GameState.Recap.cs`). In the
report, the first doujin finished on 13 April and nothing new was queued, so Aki
worked 0 hours on 15 and 16 April: no recap, so no night skip, and those nights
ran at ordinary speed. The timeline shows "overnight" every night up to
13 April and none after.

## Fix

- `GameState.IsQuietDayOver()` (engine-free): true once a scheduled day's
  regular hours are over, nobody worked and no recap fired. Days off are not
  scheduled days (the night before already skips to the next working morning).
- The real-time driver (`ScanEvents` in `godot/DebugMain.cs`) begins the same
  night skip when that is true, at any speed, keeping the open page. There is
  no recap to show, so it does not pause first. Days with work are unchanged.
- No save, balance or simulation change: the new method only reads state.

## Verification

- xUnit: three new tests in `RecapTests` (quiet day over after the last
  scheduled hour, not on a working day, not on a day off), written to fail
  first.
- Godot: `--quiet-speed-smoke` now plays a career with nothing to draw through
  its day at 8x and at 32x; it failed first (the day ran to 20:00) and now
  passes, with the next morning at 08:00 at the same speed.
- Full suite on this computer: warning-free build, 678 xUnit tests and all 18
  Godot checks pass (display sweep 520 screen checks, 0 flagged).
- Not yet seen by the user in play.
