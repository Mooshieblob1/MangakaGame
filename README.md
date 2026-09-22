# Mangaka Studio

A real-time manga studio management simulation. Sub-projects 1 and 2 are
implemented: chapter production, doujin publishing, magazine pitches and
editor reviews, serialization, reader rankings, book sales, money, reputation,
genre trends, cancellation, daily recaps, and deterministic save/load.

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

Open the **Publishing** tab to choose one of six magazines and **Pitch one-shot**.
An untouched next-chapter draft can be replaced by a pitch; finish any work
already started first. The 31-page sample goes through Name review before
production continues. A successful pitch produces an offer to accept or decline.
Accepting starts a contract with a fixed fee per page and a first issue date.

The Publishing tab shows rankings, editor status, quality, fans, cancellation
warnings, books, transactions, and genre trends. **Get online** expands doujin
sales. Five completed doujin chapters release a book immediately; commercial
books collect published chapters and release six weeks later. Sales happen on
Mondays. Pausing a serialized series does not pause its magazine deadlines;
completed stock publishes first, then issues are missed. **Withdraw** returns
the series to doujin. **End series** closes it while released books keep selling.

Saves now use **version 2**. Version 1 saves are rejected; there is no migration.
Start a new game for this milestone. Save/load includes publishing history,
market state, contributor hours, the ledger, and the random generator state.

The screen remains a debug harness. Hiring, studio costs, the 3D office,
historical rivals, and finished management screens belong to later milestones.

## Validate

```powershell
dotnet test tests/MangakaSim.Tests
dotnet build MangakaGame.sln -warnaserror
New-Item -ItemType Directory -Force TestResults | Out-Null
& $godot --headless --editor --path godot --import --quit
& $godot --headless --path godot -- --smoke-test
```

The automated Godot walkthrough tests the actual scene controls, timing,
automatic pauses, recaps, queue editing, offers and expiry, editor review,
publication, royalties, withdrawal, endings, and save/load. Its fixed-seed
publishing run uses real commands and ticks. It writes a separate test save
under `TestResults`, leaving the normal debug save alone.

To run with graphics and capture screenshots:

```powershell
& $godot --path godot -- --smoke-test --capture
```

This produces `TestResults/debug-main.png`, `debug-recap.png`,
`debug-publishing.png`, `debug-books.png`, and `debug-trends.png`.
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
- [Publishing and market design](docs/superpowers/specs/2026-09-22-publishing-market-design.md)
- [Publishing implementation plan](docs/superpowers/plans/2026-09-22-publishing-market.md)
- [Sub-project 2 completion and verification](docs/superpowers/sub-project-2-completion.md)
