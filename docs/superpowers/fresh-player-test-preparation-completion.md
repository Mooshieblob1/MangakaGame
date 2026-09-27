# T1.10 fresh-player test preparation, completion

Date: 2026-09-27. Design: [considerations](specs/2026-09-27-fresh-player-test-considerations.md)
(Q31 to Q34). Plan: [implementation plan](plans/2026-09-27-fresh-player-test.md).
Not yet committed. alpha.12 packaged on 2026-09-27, see the
[build verification](alpha-12-build-verification.md).

## What changed

- **Session timeline** (`src/MangakaSim/SessionTimeline.cs`,
  `godot/DebugMain.Timeline.cs`). A private `timeline.log` next to the saves,
  one line per moment: real time, minutes into the session, game date and the
  event. It records session start and end, window size, text size and theme,
  new careers with difficulty and Sandbox, each new Helper-Chan message
  (`src/MangakaSim/TimelineGuidance.cs`), journey milestones, speed changes and
  pauses, screens and phone opens, saves, loads, imports and error messages.
  It never records names, typed text, file paths or hardware; a career appears
  as a 6-character code and error text is cleaned of paths and titles. The file
  rolls over at about 1 MB into `timeline.old.log`, so at most about 2 MB are
  kept. A write failure never interrupts play.
- **Journey milestones** (`src/MangakaSim/JourneyMilestones.cs`). The game's own
  events mark each T1.1 step in the timeline: first doujin, first sale, later
  releases that sell, first pitch and its answer, serialization, first magazine
  chapter, first missed deadline, cancellation warning, cancellation, first hire
  and missed payday. Milestones a loaded career already reached are not logged
  again. No simulation change.
- **Problem report** (`src/MangakaSim/ProblemReport.cs`, `godot/DebugMain.Alpha.cs`).
  A third box, "Attach session timeline", ticked by default. When ticked both
  timeline files go into the report ZIP as readable text. The build name is now
  `0.8.0-private-alpha.12`.
- **Practice career** (`tests/MangakaSim.Tests/Fixtures/Practice - a struggling series.mangaka`,
  `src/MangakaSim/CareerStore.cs`, `tests/MangakaSim.Tests/CareerPlaytest.cs`).
  Made by the automated playtest from seed 0 on Standard, about two in-game
  weeks before its April 1997 cancellation warning. It keeps a fixed
  identifier, so its code is recognisable in a timeline, and testers open it
  with the existing "Import career or previous save" button.
- **Journey walk-through** (`--journey-smoke`, `godot/DebugMain.JourneySmoke.cs`).
  Drives the real screens through every T1.1 step on Standard, saving, loading
  and checking the career continues identically for 48 hours at each step. It
  reaches the setback through the practice career and tries the oldest save
  fixture and, when present, an alpha.11 career.
- **Tester kit** (`docs/superpowers/fresh-player-kit/`). `READ ME FIRST.txt` and
  `Questionnaire.txt`, copied with the practice career into the package by
  `scripts/package-alpha.ps1`. Menu labels in the text were checked against the
  game ("Load Career", "Export local report").
- **Player-facing fixes found by the walk-through:**
  - **The daily recap covered Helper-Chan's notices.** When a serialization
    offer or a cancellation warning arrived on the same day as the recap, the
    recap sat on top of it, and "Continue" started the night with the notice
    still open. The recap now waits until the notice is dealt with, then
    appears. If the player resumes time instead, that day's recap is skipped
    (`DebugMain.cs`, `DebugMain.Management.cs`, with small guards in
    `DebugMain.ManagementMenus.cs`, `DebugMain.ManagementPanels.cs` and
    `DebugMain.Office.cs`).
  - **Publishing kept the previous title selected** after a new series was
    created, while the header already showed the new one. It now follows the
    current series (`DebugMain.Publishing.cs`).
  - **Production controls showed "Weekly" and 19 pages** whatever title was
    selected. They now show the selected title's own cadence and page count
    (`DebugMain.ManagementPanels.cs`).
  - **The Production explanation only described ongoing series.** It now starts
    "A one-shot is one complete book." (`DebugMain.cs`).
- **Fixes from the final review:**
  - Resuming time while a recap is held back now still starts the overnight
    skip, instead of playing the night at the day speed.
  - The timeline records the overnight skip as "overnight" and "morning speed
    8x" rather than as the player choosing 32x and opening the Office, so the
    speed shares in the findings stay honest.
  - Errors from the import, export and report file dialogs now reach the
    timeline.
  - `READ ME FIRST.txt` now says the attached career save includes the creator
    name and titles; only the timeline is free of typed names.
- No simulation, balance or save format change.

## Verification

### Automated

- `dotnet build MangakaGame.sln -warnaserror`: no warnings or errors.
- `dotnet test --filter Category!=Playtest`: 612 tests pass, including the new
  `SessionTimelineTests`, `JourneyMilestonesTests`, `TimelineGuidanceTests`,
  `PracticeCareerTests` and the report tests in `AlphaTests`.
- Headless smokes: `--smoke-test` (823), `--management-smoke` (99),
  `--alpha-smoke` (45, run before the walk-through fixes),
  `--quiet-speed-smoke` (1,175), `--atmosphere-smoke` (893, with new checks for
  the held recap and the overnight timeline lines) and `--journey-smoke` (49).
- `--journey-smoke --capture` (rendered, windowed): 58 checks pass. The oldest
  save fixture is refused with a clear message. No alpha.11 career file exists
  yet, so that load was not tried.

### Rendered captures and my review

Nine captures, `TestResults/journey-*.avif` at 1920x1080, one per step: new
career, first doujin, first sale, later sale, first pitch, serialization offer,
first magazine chapter, first hire and the practice career's cancellation
warning. I looked at every capture over three runs. The first run showed the
recap covering the serialization offer and the cancellation warning, stale
numbers on the Print page, a window that grew to 1118 px and the wrong series
on Publishing. The stale numbers, the window size and one wrong-series case
came from the walk-through itself and were fixed there. The recap and the
Publishing selection were real player problems and were fixed in the game (see
above). In the final run every step shows the right series, current numbers
and one notice at a time.

This is an automated walk-through plus my review of its captures. It is not
human play.

### Final review

A separate reviewer read the whole change. It found no critical problems and
four important ones, all fixed above. Four minor points are left for later:
the error-text cleaner can garble words that contain a name, a corrupted
multi-gigabyte timeline is read before the size check, one stray "step" line
can appear before the first career, and running the playtest tests rewrites
the practice career file.

## Known limitations

- First hire is judged by current staff, so a career whose only hire has left
  may log "first-hire" again.
- There is no crash handler. A session without an end line only suggests a
  crash or a forced close.
- The walk-through creates its two doujin with the game command, not the
  one-shot form, so the form's own defaults are not checked there.
- Still visible in the captures and left for the testers to judge: Publishing
  labels its pitch button "Pitch one-shot" on a serialized series, and the
  inbox holds 159 unread items by the first hire.

## Next step

alpha.12 is packaged. The user recruits two testers who have
never seen the game and sends them alpha.12. Their reports go in
`TestResults/fresh-player/`, and the results go in
[fresh-player-test-findings.md](fresh-player-test-findings.md).
