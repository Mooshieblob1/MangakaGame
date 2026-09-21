# Sub-project 1: Simulation Core — Design

Date: 2026-09-22
Status: implemented and verified on 2026-09-22; see ../sub-project-1-completion.md
Roadmap: `2026-09-22-roadmap.md`

## Goal

Build the engine-free simulation core that every later sub-project plugs into:
a game clock, one mangaka, one series, a staged chapter production pipeline, an
auto-planner with player overrides, due-date tracking, a daily recap, save/load,
and a deliberately ugly Godot debug scene to drive it.

At the end of this sub-project the owner can open the Godot debug scene, create
a series, watch the prodigy mangaka work through chapters day by day, override
the queue, see daily recaps, save, and reload. All logic is covered by headless
tests.

## Non-goals (deferred to later sub-projects)

- Publishers, magazines, contracts, rankings, sales, income, cancellation (2).
- Editor check stage and chapter/series quality (2).
- Consequences for late or skipped chapters beyond tracking (2).
- Hiring, needs, happiness, pay, overtime costs (3).
- 3D office (4). Historical rivals and era events (5). Real UI and charts (6).

## Stack and project layout

One solution, `MangakaGame.sln`, with three projects:

| Project | Type | Purpose |
|---|---|---|
| `src/MangakaSim` | C# class library, no Godot reference | All simulation logic and data |
| `tests/MangakaSim.Tests` | xUnit | Unit, scenario, save/load, determinism tests |
| `godot/` | Godot 4.x C# project, references `MangakaSim` | Debug scene, real-time driver |

Target the .NET version that the installed Godot 4.x C# build requires
(net8.0 at time of writing; confirm during planning).

Rule: `MangakaSim` never references Godot. Godot only calls into the sim
through `GameState`, `Advance`, commands, and read-only queries.

## Data model

All plain C# types. `GameState` is the whole save file.

### GameState
- `Version` (int): save format version, starts at 1.
- `Clock` (GameClock).
- `Series` (list of Series).
- `People` (list of Person).
- `Events` (list of GameEvent): append-only log.
- `RngSeed` (int) and RNG state, so a loaded game continues deterministically.
- `Settings` (Settings): auto-pause flags per event type, and balance constants
  (base hours per page per stage, skill multiplier curve, overtime cap).

### GameClock
- Game time as year, month, day, hour (0 to 23). Starts **1 April 1996, 08:00**.
- One tick = one hour. Provides day-of-week and "hours until" helpers.
- Backed by `System.DateTime` at hour precision. Godot never touches it.

### Series
- `Id`, `Title`, `Genre` (string tag for now).
- `Cadence`: Weekly, Biweekly, Monthly. Determines due-date spacing.
- `PagesPerChapter` (int, default 19).
- `Status`: Active, Paused, Ended.
- `StartDate`: first due date is StartDate + cadence interval.
- `Chapters` (list of Chapter).

### Chapter
- `Number` (int, 1-based).
- `DueDate` (game time).
- `Status`: NotStarted, InProgress, Complete.
- `CompletedAt` (nullable game time).
- `IsLate` (bool) and `HoursOverdue` (int): set at completion, for sub-project 2
  to punish later.
- `Stages` (ordered list of StageWork).

### StageWork
- `Stage`: Name, Pencils, Inks, Backgrounds, Tones (in that order).
- `HoursRequired` (double), `HoursDone` (double).
- `AssignedTo` (Person id, nullable).
- `Status`: NotStarted, InProgress, Complete, Skipped.

`HoursRequired = PagesPerChapter * BaseHoursPerPage[Stage]`. Skill does not
change hours required; it changes how many hours of work a person delivers per
tick (see Ticking). Base hours per page (balance constants, tunable):

| Stage | Hours/page | 19-page chapter |
|---|---|---|
| Name | 1.2 | 22.8 |
| Pencils | 1.5 | 28.5 |
| Inks | 1.0 | 19.0 |
| Backgrounds | 1.0 | 19.0 |
| Tones | 0.5 | 9.5 |
| **Total** | | **98.8** |

### Person
- `Id`, `Name`.
- `Skills`: value 0 to 100 per Stage.
- `Schedule`: `WorkStartHour`, `WorkEndHour` (e.g. 08 to 18), `DaysOff` (set of
  day-of-week).
- `OvertimeAllowed` (bool).
- `Queue`: ordered list of references (chapter id + stage) to StageWork.
- `CurrentTask`: the queue item being worked, or null.
- `HoursWorkedToday`, `OvertimeHoursToday`: reset at day start; used by the
  recap and the overtime cap.

Skill multiplier (speed): skill 0 gives 0.5x, 50 gives 1.0x, 100 gives 2.0x.
Piecewise linear between those points.

**Starting mangaka** is a prodigy: skill 80 in every stage (about 1.6x), so a
98.8-hour chapter takes about 62 person-hours. At 10 hours a day, six days a
week (60 hours), a weekly chapter is achievable solo with occasional overtime,
and falls behind if anything slips. Difficulty settings later scale these
starting skills.

### GameEvent
- `Time` (game time), `Type` (enum), `Message` (string), and typed payload
  fields as needed (series id, chapter number, person id, stage).
- Types in this sub-project: DayStarted, WeekStarted, StageStarted,
  StageCompleted, StageSkipped, ChapterCreated, ChapterCompleted,
  DeadlineMissed, ChapterAtRisk, DailyRecap, CommandApplied.

## Ticking

Single entry point: `GameState.Advance(int hours)`. Runs `Tick()` that many
times. Each tick, in fixed order:

1. **Clock.** Advance one hour; roll day, month, year. On hour 0 of a new day
   emit DayStarted and reset per-day counters. On Monday at hour 0 also emit
   WeekStarted.
2. **Planner.** Runs on DayStarted (and after every command, outside the
   tick). See Planner.
3. **Work.** For every person, if this hour is a working hour for them and they
   have a current task, add `1.0 * SkillMultiplier(stage)` to the task's
   `HoursDone`. If `HoursDone >= HoursRequired`, mark the stage Complete, emit
   StageCompleted, advance to the next startable queue item and emit
   StageStarted. If the completed stage was the last in its chapter, mark the
   chapter Complete and emit ChapterCompleted.
4. **Deadlines.** For each chapter completed this tick, set `IsLate` and
   `HoursOverdue` if `CompletedAt > DueDate`, and emit DeadlineMissed. For each
   in-progress chapter, recompute at-risk status (see Overtime) and emit
   ChapterAtRisk on the transition from not-at-risk to at-risk.
5. **Day end.** If after this tick no person will work again until a later
   day, emit DailyRecap.

A working hour for a person is: not a day off, and `WorkStartHour <= hour <
WorkEndHour`, or an overtime hour (see Overtime).

Sequencing rule: stages within a chapter complete in order. A stage is
startable only when every earlier stage in the same chapter is Complete or
Skipped.

## Planner

Runs at DayStarted and after every command. Idempotent: running it twice
changes nothing.

1. **Ensure next chapter.** For each Active series, if there is no chapter with
   status NotStarted or InProgress, create the next chapter: number = last + 1,
   due date = previous due date + cadence interval (or StartDate + interval
   for chapter 1), five StageWork entries with hours from the balance table.
   Emit ChapterCreated. Only one unstarted chapter exists at a time in this
   sub-project.
2. **Assign.** Every unfinished stage of every active series is assigned to the
   single Person. Sub-project 3 replaces this rule.
3. **Order queues.** Each person's queue = all their unfinished, non-skipped
   stages, ordered by chapter due date ascending, then stage order. Pinned
   items (see Commands) stay at the top in pin order. A ReorderQueue order is
   respected until the next DayStarted. CurrentTask is the first startable
   item in the queue; the queue is not reordered to find it.

Paused series contribute nothing to queues. Their chapters keep their due
dates; consequences are a sub-project 2 concern.

## Overtime and at-risk

A chapter is **at risk** when, for its assignee, the remaining person-hours of
work (sum over unfinished stages of `(HoursRequired - HoursDone) /
SkillMultiplier(stage)`) exceed the assignee's remaining scheduled working
hours before the due date.

When a person has `OvertimeAllowed` and the chapter of their CurrentTask is at
risk, the hours from `WorkEndHour` to `WorkEndHour + OvertimeCap` (cap = 2, a
balance constant) count as working hours for them that day and increment
`OvertimeHoursToday`. Overtime has no cost in this sub-project.

## Commands (player overrides)

All player changes go through `GameState.Apply(ICommand)`. A command
validates fully, then mutates state, emits CommandApplied plus any specific
events, and runs the Planner. The debug UI and the future real UI use the
same commands. Commands are serializable so tests can replay a command log.

| Command | Effect |
|---|---|
| CreateSeries(title, genre, cadence, pagesPerChapter) | Adds an Active series starting now; planner creates chapter 1 |
| PauseSeries(id) / ResumeSeries(id) | Toggles Active and Paused |
| SetCadence(seriesId, cadence) | Applies to chapters created after this point |
| SetPagesPerChapter(seriesId, pages) | Applies to chapters created after this point |
| PinStage(personId, chapterId, stage) / UnpinStage(...) | Pinned items sit at the top of the queue in pin order |
| ReorderQueue(personId, orderedRefs) | Sets explicit order until the next DayStarted; pinned items still win |
| SkipStage(chapterId, stage) | Stage becomes Skipped at zero hours; emits StageSkipped. Cannot skip a Complete stage |
| SetSchedule(personId, start, end, daysOff) | Takes effect immediately |
| SetOvertimeAllowed(personId, bool) | Takes effect immediately |

Undo is out of scope for this sub-project. The command log makes it possible
later.

## Daily rhythm: recap and idle skip

- **Recap.** DailyRecap fires when the last working person finishes for the
  day: 18:00 on a normal day, 20:00 if someone worked full overtime. Payload:
  stages started and completed today, hours worked per person with overtime
  called out, chapters at risk, chapters completed today, deadlines missed
  today. Later sub-projects append their own lines to the same event.
- **Idle skip (Godot side).** When Godot receives DailyRecap it shows the
  recap and pauses. On dismiss, it asks the sim `HoursUntilNextWork()` and
  calls `Advance` with that number in one batch. The clock then reads the next
  arrival time. The sim processes the skipped hours as ordinary ticks, so day
  and week rollovers and DayStarted events happen correctly. If nobody is
  scheduled for several days, the skip covers the whole span.

## Speed and real time (Godot side)

- Speeds: Pause, 1x, 2x, 4x, 8x. At 1x one game day is 60 real seconds, so
  one game hour is 2.5 s. At 8x one hour is about 0.31 s.
- Godot accumulates fractional hours from `delta * speed / 2.5` and calls
  `Advance(wholeHours)` whenever the accumulator reaches 1 or more. The sim
  never sees real time or fractions.
- **Auto-pause**: `Settings` holds a flag per event type. After each Advance,
  Godot scans new events; if any flagged type appeared, it sets speed to
  Pause. Defaults: DailyRecap on (always pauses), ChapterCompleted on,
  DeadlineMissed on, all others off.
- No uncapped speed. This is a deliberate decision: the rhythm is daily.

## Save and load

- `GameState.ToJson()` and `GameState.FromJson(string)` using
  System.Text.Json.
- `Version` is written. Loading a newer version than supported fails with a
  clear error. No migrations yet.
- RNG state is saved so a loaded game continues identically. No system uses
  the RNG in this sub-project, but the plumbing exists so later systems do not
  retrofit it.
- Debug scene has Save and Load buttons writing to `user://debug.json`.

## Debug scene (Godot)

One scene, `DebugMain.tscn`, with a C# script. Ugly on purpose. Contains:

- Clock label (date, time, day of week) and speed buttons.
- Event log panel, newest at bottom, auto-scroll.
- Chapter table: one row per chapter with five per-stage progress bars, due
  date, status, late flag.
- Person panel: name, schedule fields, overtime toggle, queue list with pin,
  reorder, and skip buttons.
- Buttons: New Series (small form), Pause/Resume Series, Set Cadence, Set
  Pages, Save, Load.
- Recap popup on DailyRecap with a Continue button that performs the idle
  skip.

## Error handling

- Commands validate before mutating and throw `InvalidCommandException` with a
  message. State is never left partially mutated. The debug UI shows the
  message in the log.
- `Advance` with negative hours throws. `Advance(0)` is a no-op.
- Save failures surface as a log line in the debug UI. The sim itself only
  throws.

## Testing

All in `MangakaSim.Tests`, headless, no Godot.

- **Clock**: rollovers at day, month (including February and leap years),
  year; DayStarted and WeekStarted emission.
- **Work**: skill multiplier at 0, 50, 80, 100; hours accrue only in working
  hours; stage sequencing; stage and chapter completion.
- **Planner**: next chapter creation and due dates for each cadence;
  idempotence; queue ordering by due date then stage; pinned items; reorder
  expiry at DayStarted; paused series excluded.
- **Overtime and at-risk**: detection; overtime only when allowed and at risk;
  cap respected.
- **Commands**: each command's effect and validation failures; state unchanged
  on an invalid command.
- **Recap**: fires at the right hour on normal and overtime days; payload
  contents.
- **Idle skip query**: `HoursUntilNextWork()` across nights, days off, and
  multi-day gaps.
- **Save and load**: round-trip equality; continuing after a load matches an
  unloaded run.
- **Determinism**: two runs from the same seed and command log produce equal
  state.
- **Scenario and balance**: prodigy mangaka (skill 80), weekly series, 52
  weeks, 10-hour days, one day off, overtime allowed: assert at least 45 of 52
  chapters are on time. Same scenario at skill 50: assert most chapters are
  late. These are the balance guardrails for the constants above; adjust the
  thresholds only alongside a deliberate constant change.

## Decisions made during brainstorming

- Staged pipeline without quality; quality is deferred to sub-project 2.
- One game day per real minute at 1x; fixed speeds only, max 8x.
- Start date 1 April 1996.
- Goal-level control with overrides, Software Inc. style.
- Due dates tracked only; no consequences yet.
- Idle time after the recap is skipped entirely, not accelerated.
- Starting mangaka is a prodigy so solo weekly serialization is possible.
