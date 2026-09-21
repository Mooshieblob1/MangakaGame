# Mangaka Studio

A real-time manga studio management simulation. Sub-project 1 is implemented:
one mangaka, chapter production, schedules and overtime, queue overrides,
deadline tracking, daily recaps, save/load, and a Godot debug screen.

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

The screen is a functional debug harness. Publishing, money, quality, hiring,
the 3D office, and the finished management UI belong to later sub-projects.

## Validate

```powershell
dotnet test tests/MangakaSim.Tests
dotnet build MangakaGame.sln -warnaserror
New-Item -ItemType Directory -Force TestResults | Out-Null
& $godot --headless --editor --path godot --import --quit
& $godot --headless --path godot -- --smoke-test
```

The automated Godot walkthrough tests the actual scene controls, timing,
automatic pauses, recaps, queue editing, and save/load. It writes a separate
test save under `TestResults`, leaving the normal debug save alone.

To run with graphics and capture screenshots:

```powershell
& $godot --path godot -- --smoke-test --capture
```

This produces `TestResults/debug-main.png` and `TestResults/debug-recap.png`.
Test outputs, build products, and Godot caches are ignored by Git.

## Code and design

- `src/MangakaSim`: engine-free C# simulation. `GameState.Advance(hours)` drives
  time; `GameState.Apply(command)` handles player changes.
- `tests/MangakaSim.Tests`: unit, regression, replay, save/load, and balance tests.
- `godot`: debug scene, real-time driver, and opt-in scene integration checks.
- [Roadmap](docs/superpowers/specs/2026-09-22-roadmap.md)
- [Simulation design](docs/superpowers/specs/2026-09-22-sim-core-design.md)
- [Implementation plan](docs/superpowers/plans/2026-09-22-sim-core.md)
- [Completion notes and verification](docs/superpowers/sub-project-1-completion.md)
