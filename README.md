# Mangaka Studio

A real-time manga studio management simulation. Sub-projects 1 to 3 are
implemented: one mangaka, chapter production, schedules and overtime, queue
overrides, deadline tracking, daily recaps, save/load, and a Godot debug
screen; six magazines with generated rosters, pitching and serialization, an
editor gate on the Name stage, chapter quality, weekly rankings, fanbase and
cultural impact, tankobon and doujin sales into a ledger, genre trends,
reputation, warnings and cancellation, withdraw and end, going online; and a
studio with a monthly candidate pool, hiring, salaries and payroll, premises
and amenities, needs and breaks, fatigue, happiness, moonlighting, quitting,
and a planner that shares a chapter across several desks.

## Run the debug screen

Requires **.NET 8 SDK** and **Godot 4.7.2 .NET** (the C# edition).

From the repository root in PowerShell:

```powershell
dotnet build MangakaGame.sln
```

Import `godot/project.godot` into Godot, open it, and press **F5**.
The solution is at the repository root; Godot is configured to use it.

Alternatively, with the current Windows WinGet installation:

```powershell
$godot = Join-Path $env:LOCALAPPDATA 'Microsoft\WinGet\Packages\GodotEngine.GodotEngine.Mono_Microsoft.Winget.Source_8wekyb3d8bbwe\Godot_v4.7.2-stable_mono_win64\Godot_v4.7.2-stable_mono_win64_console.exe'
& $godot --path godot
```

1. Enter a title and genre, choose a cadence and page count, and click **Create**.
2. Press **1x** or **8x** to watch Aki work through Name, Pencils, Inks,
   Backgrounds, and Tones. At 1x, an hour takes 2.5 seconds.
3. Daily recaps pause the game. **Continue** skips to the next scheduled arrival
   and restores the previous speed. Chapter completion and missed deadlines
   also pause automatically; press a speed button to continue.
4. Pause time before editing the queue. Pins set priority while preserving
   stage prerequisites. Up/Down overrides expire at midnight; pins persist.
5. **Save** and **Load** use Godot's `user://debug.json`. On Windows this is
   normally `%APPDATA%\Godot\app_userdata\MangakaGame\debug.json`.
6. Publishing row: pick a magazine, then **Pitch** while the selected series'
   open chapter is untouched. The chapter becomes a 31-page one-shot; when it
   is finished and approved it is judged at the magazine's next issue close.
   **Accept** or **Decline** the offer that follows a successful pitch.
   **Withdraw** leaves a magazine (penalties apply); **End series** ends a
   run, with a bonus after twelve published chapters.
7. The status line under the controls shows the selected series' publishing
   state, contract, editor state, last quality and rank, fanbase, impact,
   strikes and warnings. The chapter table gains quality (**Q**) and **Rank**
   columns; one-shots are marked `*`.
8. The **Market** column follows the magazine dropdown: the latest ranking
   (player rows marked `>>`), the ledger tail with balance and price index,
   a **Get Online** button, and every volume with copies sold.
9. The bottom lines show the studio's track record, staff term and effective
   reputation, and every genre's effective trend multiplier (`+` is the
   studio's own influence, `!` marks a boom).

10. The **Staff** column has a dropdown over everyone in the studio. The
    schedule controls, salary field, allowed-stage boxes, promotion dropdown
    and **Fire** button act on the selected person; the mood line shows
    happiness (and its equilibrium), fatigue, needs and moonlighting. Below
    the queue, the **Candidates** list shows the monthly pool with skills
    (Name/Pencils/Inks/Backgrounds/Tones) and asking salary; adjust the offer
    and press **Hire** (offers under 80% of the asking salary are refused;
    the garage holds two people).
11. The **Studio** line at the top of the market column shows the premises,
    desks, atmosphere and monthly charges, with **Move** and **Buy** buttons
    for premises and amenities. Rent and upkeep are charged on the 1st,
    payroll on the 25th, both at 09:00. The stage bars carry the assignee's
    initials (`!` marks a manual assignment).

The screen is a functional debug harness. The 3D office, the historical
timeline and the finished management UI belong to later sub-projects.

## Validate

```powershell
dotnet test tests/MangakaSim.Tests
dotnet build MangakaGame.sln -warnaserror
New-Item -ItemType Directory -Force TestResults | Out-Null
& $godot --headless --editor --path godot --import --quit
& $godot --headless --path godot -- --smoke-test
```

The automated Godot walkthrough tests the actual scene controls, timing,
automatic pauses, recaps, queue editing, and save/load, then runs a doujin
chapter, pitches to Monthly Hoshigaku Flowers under a seed that succeeds,
accepts, publishes, sells a tankobon, gets online, and reloads; then it
pitches a weekly series to Tokiwa Jump, hires two assistants from the pool,
moves to the apartment, buys a fridge and chairs, publishes twelve chapters
with at most one miss, cuts a salary, and reloads. It writes a separate test
save under `TestResults`, leaving the normal debug save alone.

To run with graphics and capture screenshots:

```powershell
& $godot --path godot -- --smoke-test --capture
```

This produces `TestResults/debug-main.png`, `TestResults/debug-recap.png`,
`TestResults/debug-market.png` and `TestResults/debug-studio.png`.

On Linux without a display, the same run works under a virtual X server with
software OpenGL:

```bash
LIBGL_ALWAYS_SOFTWARE=1 xvfb-run -a -s "-screen 0 1600x900x24" \
  godot --rendering-driver opengl3 --path godot -- --smoke-test --capture
```
Test outputs, build products, and Godot caches are ignored by Git.

## Code and design

- `src/MangakaSim`: engine-free C# simulation. `GameState.Advance(hours)` drives
  time; `GameState.Apply(command)` handles player changes. `Rules/` holds the
  stateless formula modules, `Catalog/` the embedded publisher and trend data.
- `tests/MangakaSim.Tests`: unit, regression, replay, save/load, and balance tests.
- `godot`: debug scene, real-time driver, and opt-in scene integration checks.
- [Roadmap](docs/superpowers/specs/2026-09-22-roadmap.md)
- Sub-project 1: [design](docs/superpowers/specs/2026-09-22-sim-core-design.md),
  [plan](docs/superpowers/plans/2026-09-22-sim-core.md),
  [completion notes](docs/superpowers/sub-project-1-completion.md)
- Sub-project 2: [design](docs/superpowers/specs/2026-09-22-publishing-market-design.md),
  [plan](docs/superpowers/plans/2026-09-22-publishing-market.md),
  [completion notes](docs/superpowers/sub-project-2-completion.md)
- Sub-project 3: [design](docs/superpowers/specs/2026-09-22-staff-studio-design.md),
  [plan](docs/superpowers/plans/2026-09-22-staff-studio.md),
  [completion notes](docs/superpowers/sub-project-3-completion.md)
