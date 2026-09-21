# Sub-project 1 completion

Completed 22 September 2026. Sub-project 2 has not been started.

## Delivered

- .NET 8 solution with an independent simulation library, xUnit project, and
  Godot 4.7.2 C# debug scene.
- Deterministic hourly clock, one starting mangaka, multiple series, weekly /
  biweekly / monthly cadences, and the five-stage chapter pipeline.
- Automatic planning, pins, temporary queue ordering, skip, pause/resume,
  schedule changes, overtime, and tracked deadlines.
- Daily recaps, overnight/day-off skipping, fixed speeds, and event-based pauses.
- Versioned JSON saves, saved RNG state, validated loading, and timestamped
  serializable commands for replay.
- A debug screen exposing all eleven commands, chapter progress and history,
  the worker's queue, an event log, recaps, and save/load.

## Verification

Verified locally on Windows with .NET SDK 8.0.425 and Godot
4.7.2.stable.mono.official.ed1daf0bf.

- `dotnet test tests/MangakaSim.Tests`: **148 passed**, none failed or skipped.
- `dotnet build MangakaGame.sln -warnaserror`: **0 warnings, 0 errors**.
- Godot headless editor import and scene startup: successful, no engine errors.
- Godot scene walkthrough: **29 checks passed** headless; **31 passed** when
  rendered, including two screenshot captures.
- Rendered on the local RTX 5070 using Vulkan / Forward+. Both screenshots
  were inspected for readable controls, chapter columns, queue, log, and recap.
- 52-week weekly-series scenario at skill 80: **52 chapters completed, all 52
  on time**. At skill 50: **37 completed, all 37 late**.
- Monthly scenario, weekly recap count, save/load continuation, command replay,
  midnight accounting, and invalid-command atomicity all passed.

UI verification used automated scene-control signals and rendered screenshots.
A separate human playthrough in the Godot editor was not performed. The owner
walkthrough remains available in the plan and README.

## Clarifications and corrections to the plan examples

The source files are the implemented version. The plan's embedded code remains
a record of the original approach, with these corrections:

- Overtime can resolve risk. A later transition back into risk emits another
  warning; it is not limited to one warning for the entire chapter. Recaps list
  risk remaining at day end. Two example test expectations were corrected.
- `CommandLog` entries hold `Time` and `Command`; mutable command collections
  are copied so subsequent caller edits cannot alter the saved log.
- Commands validate enum values, null collections, and schedule limits before
  mutation, then refresh risk immediately. Skipping clears partial stage hours.
- Pins survive pause/resume. Completed or otherwise invalid pins are removed.
- The daily overtime counter enforces the cap even if the schedule changes.
- Midnight closes the previous day's recap before resetting its counters. The
  recap event window resets each day so yesterday's commands are not reported
  as today's completions.
- Loads reject incomplete, unsupported, or inconsistent state before the UI
  replaces its current game.
- The Godot driver scans events after every simulated hour, including when
  catching up after a slow frame. Command-generated events also auto-pause.
  Recaps always pause, and idle skip respects other enabled auto-pause events.
- Loading at a recap restores its Continue action. The chapter table retains
  older chapters; the recap includes both started and completed stages.
- A new game starts at 08:00 with an empty event log. DayStarted and WeekStarted
  are clock-rollover events, not fabricated initial events. Pinning a blocked
  later stage changes its queue position without bypassing prerequisites.
- The unavailable Superpowers helper skills were replaced by direct execution
  of the task sequence. Task commits were consolidated into one milestone
  commit rather than committing intermediate scaffolds.

No publishing, commercial release, or deployment was performed. Later roadmap
milestones remain pending further instructions.
