# Mangaka Days

A real-time manga studio management simulation. Sub-projects 1 and 2 are
implemented: chapter production, doujin publishing, magazine pitches and
editor reviews, serialization, reader rankings, book sales, money, reputation,
genre trends, cancellation, daily recaps, and deterministic save/load.

Sub-project 3 adds staff wellbeing and development, teams and overlapping
production, separate personal/business finances, credit, Tokyo studios and
branches, physical doujin printing, conventions, promotion and creator-led
career changes. The parents' home starts rent-free. See the
[delivery record](docs/superpowers/plans/2026-09-23-staff-studio.md) for scope,
verification and prototype balance choices.

Sub-project 4 adds a working 3D office, furniture ownership and editing,
Auto-arrange, ambient residents and accelerated character trails. See the
[delivery record](docs/superpowers/sub-project-4-completion.md),
[design](docs/superpowers/specs/2026-09-23-3d-office-design.md) and
[implementation plan](docs/superpowers/plans/2026-09-23-3d-office.md).

Sub-project 6 adds the office home screen, management cards, inbox and charts,
career menus/saves, manga showcases and custom artwork. Helper-Chan has the
supplied portrait, expression variants, her own 3D character and equipped desk,
contextual help, important notices and a persistent optional story. See the
[delivery record](docs/superpowers/sub-project-6-completion.md).

Sub-project 7 adds newcomer contests and annual awards, anime and merchandise
licensing, career honors, difficulty presets and configurable Sandbox assists.
Any Sandbox use permanently disables new Steam achievements for that save.
Helper-Chan's supplied desk illustration appears in an optional everyday scene.
See the [delivery record](docs/superpowers/sub-project-7-completion.md).

## Run the game

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

1. Choose **New Career**, then Helper-Chan's **Show me** or **Series → Create a
   one-shot doujin**. Enter a title and genre; 16 story pages are suggested.
   For an ongoing title, use **Series → Create an ongoing series** instead.
2. Press **1x** or **8x** to watch Aki work through Name, Pencils, Inks,
   Backgrounds, and Tones. At 1x, an hour takes 48 seconds; the default workday takes about one minute at 8x.
3. Daily recaps pause the game. **Continue** skips to the next scheduled arrival
   and restores the previous speed. Chapter completion and missed deadlines
   also pause automatically; press a speed button to continue.
4. Pause time before editing the queue. Pins set priority while preserving
   stage prerequisites. Up/Down overrides expire at midnight; pins persist.
5. **Save** keeps named snapshots; **Menu → Load Career** restores them. Three
   daily autosaves rotate per career. Data lives in Godot's `user://careers`
   (normally `%APPDATA%\Godot\app_userdata\MangakaGame\careers` on Windows).
   **Export portable career** includes custom artwork in a `.mangaka` file.
   New snapshots use lossless compressed `.career` files; old snapshots remain
   readable. Full recorded history is retained, including in portable exports.

The [Windows private alpha guide](docs/superpowers/private-alpha-testing.md)
covers standalone packaging and tester feedback. Helper-Chan's optional next-step
card adapts to existing careers and offers doujin, contest and employment routes.
Settings include natural-speed ambience/effects and separate mute controls.
**Menu → Report a problem** exports a local report, with optional screenshot/save
attachments for manual sharing. Music and native Steam integration are deferred.

The office remains the home screen. Staff, Studios and Industry open the expanded
Studio workbench directly. Other management pages use the full area; Inbox and
Series remain sidebars with numbered progress bars. Cards open details. Header money and time
controls remain available there. Back returns to the previous detail/filter;
Escape closes the current layer. Use **Studios → Arrange furniture** for the
office editor. **Help** revisits guides and conversations. Menu settings control
text size, motion trails, tutorial prompts, optional stories and automatic pauses.

Open the **Publishing** tab to choose one of six magazines and **Pitch one-shot**.
An untouched next-chapter draft can be replaced by a pitch; started chapters are
preserved and put on hold while the sample is prepared. No collected book is required. The 31-page sample goes through Name review before
production continues. A successful pitch produces an offer to accept or decline.
Accepting starts a contract with a fixed fee per page and a first issue date.

The Publishing tab shows rankings, editor status, quality, fans, cancellation
warnings, books, transactions, and genre trends. **Get online** expands doujin
sales. A one-shot finishes after one story. Each chapter of a new ongoing doujin creates a printable numbered issue; five chapters also create an optional collected book. Order copies through **Show me** or in
**Studio management**; the first delivery starts its sales window. Commercial books collect published chapters and release six weeks later. Copies sell through shop hours,
10:00 to 20:00. Pausing a serialized series does not pause its magazine deadlines;
completed stock publishes first, then issues are missed. **Withdraw** returns
the series to doujin. **End series** closes it while released books keep selling.

Open **Staff and studio** to hire into the family's spare desk, commission a
targeted search, contribute personal savings, inspect pay and assign colleagues
to series or particular drawing stages. Only the lead can do Name. Select a
person in the staff table or Production dropdown to edit their schedule/queue.
Hiring reserves seven days' pay and starts the employee next weekday; monthly
wages go to their own wallet. A shortage becomes arrears, never an automatic
debit from the protagonist's savings. Dismissal gives 30 days of paid notice.

The protagonist starts with ¥200,000 personally and ¥300,000 in the business,
after a recorded founding contribution. The family's house has **¥0 rent**.
The new-game dialog selects title-retention difficulty. The protagonist keeps
future lead titles in either mode; hired creators follow that ownership policy.

Open **Studio management** for needs, overtime limits, personal-project policy,
salaries, loans, incorporation, recovery commissions, properties, branches,
printing, conventions and promotion. Select the employee/title in **Staff and
studio** first. A property move costs three rents upfront plus moving expenses;
two rents are a refundable deposit. Loan rates and property/printer prices are
labelled game estimates, with the supporting Japanese sources in the research
ledger. **Tokyo map** shows estimated distances and fares. Nearby walking or
cycling costs nothing but still uses time.

Career choices are returning home, establishing a new studio, or joining an
employer. Select colleagues to invite, review the destination, budget, titles,
followers and print cancellation costs, then confirm the midnight move. Former
businesses retain cash, debt, released books and stock and continue independently.
Creator shares remain payable to their original recipients. Employment gives
control of the lead's team and a limited budget; it does not grant ownership.

Open **Office** to watch the current workplace. Select a location to view a branch;
other workplaces keep operating. Click a person for their portrait and current
activity. Use the wheel to zoom, right drag to rotate, middle drag to pan, and
**Reset camera** to restore the room view. Timelapse has Full, Reduced and Off
settings; character movement follows the selected 1x/2x/4x/8x speed.

**Furnish** pauses the game. Add furniture, drag it on the grid, rotate with R,
lock arrangements, assign desks, store or sell owned items, and preview
Auto-arrange. A desk requires a paired chair before staff can use it. Basic
furniture preserves production speed; walking never costs productive hours.
Apply charges the business once; Discard restores the prior layout and pause
state. **Preview studio move** includes the lease, move and furniture in one
quote. An additional branch starts empty and needs workstations before transfers.
The parents' home retains its free family-owned desks when the creator leaves.
Starting appearance can be changed before time first advances.

Saves now use **version 7**, with camera, navigation and artwork preferences in
the career envelope. **Menu → Load Career → Import career or previous save**
accepts portable packages and completed v3/v4/v5/v6 JSON saves. Original files remain
untouched. Incomplete earlier v3, v1/v2, custom properties and legacy layouts that
cannot fit are rejected. Sales/ranking charts label missing pre-import history.
Imported games continue from the converted checkpoint; old commands remain
history rather than a promise of cross-version replay from a new game.

The **Industry** tab adds thirteen historical manga analogues, dated industry
news, rival studios, paid scouting, reciprocal recruitment and temporary assistant
opportunities. Hiring reserves a workstation and seven days of wages. Your own
career move always needs acceptance; ordinary staff may choose to leave.

Temporary assistants must first be discovered through recruitment, colleagues or
the pool. Their fixed departure is shown before hiring. They support production
and limited mentoring, and can remain professional contacts afterward.

Digital and overseas proposals unlock with the era. Publisher review takes seven
days, can refuse, and charges nothing on refusal. Direct doujin digital release
is immediate. Digital copies need no printed stock; channel receipts are shown
separately. Historical coverage ends in 2025; later news is labeled simulated.

**Import previous office save (v4)** in Industry converts `debug-v4.json` at its
current date, preserving accounts and earlier history. It establishes a replay
checkpoint without replaying earlier income. The original file is kept. The
Office tab's v3 importer now composes the conversions into v7. The main menu
provides the general file picker for all supported previous versions.

The office panels and management workbench are now the default player interface.
`DebugMain.tscn` and `--smoke-test` retain the original development harness for
regression checks. **Industry → Awards, Licenses or Career journal** opens the
new progression views. Contests use normal manuscript production. Licensing
offers explain payment, creator share, approval rights and time commitments.
**Menu → Settings** changes ongoing difficulty and individual Sandbox assists;
opening money and title ownership remain fixed after career setup.

Steam achievement definitions and the permanent Sandbox policy are implemented;
this build uses a no-op platform adapter. A Steam app ID, native integration and
live account checks are still required before release.

## Validate

```powershell
dotnet test tests/MangakaSim.Tests
dotnet build MangakaGame.sln -warnaserror
New-Item -ItemType Directory -Force TestResults | Out-Null
& $godot --headless --editor --path godot --import --quit
& $godot --headless --path godot -- --smoke-test
& $godot --headless --path godot -- --management-smoke
```

The other automated checks run the same way: `--progression-smoke`,
`--alpha-smoke`, `--usability-smoke`, `--production-smoke`,
`--office-life-smoke`, `--convenience-smoke`, `--series-status-smoke`,
`--atmosphere-smoke`, `--family-home-smoke`, `--quiet-speed-smoke`,
`--display-sweep-smoke`, `--journey-smoke`, `--music-smoke`,
`--startup-smoke`, `--title-smoke` (title screen, fades, pause menu and
safety saves) `--brand-smoke` (the logo palette, slab buttons and fonts) and
`--tester-b-smoke` (fixes from fresh-player tester B) `--goals-smoke`
(the career goals board), `--disclosure-smoke` (progressive disclosure),
`--work-feedback-smoke` (work sparkles, "done" bubbles and right-click back) and
`--selling-smoke` (streaming sales and the selling tutorial).

The automated Godot walkthrough tests the actual scene controls, timing,
automatic pauses, recaps, queue editing, offers and expiry, editor review,
publication, royalties, withdrawal, endings, and save/load. Its fixed-seed
publishing run uses real commands and ticks. It writes a separate test save
under `TestResults`, leaving the normal debug save alone.

To run with graphics and capture screenshots:

Screenshots use AVIF (quality 85, full 4:4:4 colour) to keep UI text readable with smaller files. Capture-only tooling requires Python with Pillow AVIF support; set `MANGAKA_SCREENSHOT_PYTHON` to its executable if it is not on PATH. Normal gameplay does not use Python.

Existing test screenshots can be converted with `python scripts/convert-test-screenshots.py`. It validates every AVIF before removing its PNG and preserves career artwork fixtures.

```powershell
& $godot --path godot -- --smoke-test --capture
& $godot --path godot -- --management-smoke --capture
```

This produces `TestResults/debug-main.avif`, `debug-recap.avif`,
`debug-publishing.avif`, `debug-books.avif`, `debug-trends.avif`, `debug-staff.avif`,
`debug-operations.avif`, `debug-operations-career.avif` and `debug-tokyo.avif`.
Office captures include `office-family.avif`, `office-editor.avif`,
`office-small-studio.avif`, `office-large.avif`, `office-1280.avif`, and a 45-frame
`office-motion-000.avif` through `office-motion-044.avif` sequence at 30 FPS.
The rendered run reports Full/Reduced large-office frame timings. Test outputs,
build products, and Godot caches are ignored by Git.

## Code and design

- `src/MangakaSim`: engine-free C# simulation. `GameState.Advance(hours)` drives
  time; `GameState.Apply(command)` handles player changes.
- `tests/MangakaSim.Tests`: unit, regression, replay, save/load, and balance tests.
- `godot`: management and debug scenes, real-time driver, and opt-in integration checks.
- [Management action inventory](docs/superpowers/management-ui-actions.md)
- [Sub-project 6 completion and verification](docs/superpowers/sub-project-6-completion.md)
- [Bundled artwork and Helper-Chan assets](godot/Assets/README.md)
- [Roadmap](docs/superpowers/specs/2026-09-22-roadmap.md)
- [Simulation design](docs/superpowers/specs/2026-09-22-sim-core-design.md)
- [Implementation plan](docs/superpowers/plans/2026-09-22-sim-core.md)
- [Completion notes and verification](docs/superpowers/sub-project-1-completion.md)
- [Publishing and market design](docs/superpowers/specs/2026-09-22-publishing-market-design.md)
- [Publishing implementation plan](docs/superpowers/plans/2026-09-22-publishing-market.md)
- [Sub-project 2 completion and verification](docs/superpowers/sub-project-2-completion.md)
- [Staff and studio design](docs/superpowers/specs/2026-09-23-staff-studio-design.md)
- [Staff and studio implementation plan](docs/superpowers/plans/2026-09-23-staff-studio.md)
- [Tokyo research and limits](docs/superpowers/specs/2026-09-23-tokyo-research.md)

- [Office asset provenance and conventions](godot/Office/ASSETS.md)
- [Sub-project 4 completion and verification](docs/superpowers/sub-project-4-completion.md)

### Private alpha controls and printing

The strip below navigation shows the current series, active production stage,
stage progress and overall chapter completion. It follows series details and
workbench selection, and its dropdown can switch the series being followed.

Middle drag and WASD pan along the screen axes. Space pauses/resumes; 1 slows
time and 2 speeds it up. Books shows finished and unfinished doujin, with direct
printing controls and live delivery, stock and sales status. Local distribution
starts automatically on delivery, and copies sell through shop hours, 10:00 to 20:00. Dark mode defaults on
and can be switched in Menu → Settings; the preference persists across careers.


### Production and management update (alpha.4)

Self-published dates are personal targets without missed-deadline penalties.
The prodigy starts with all production skills at 95; existing version-8 careers
receive the same minimum when loaded. **Finances → Part-time job** supplies
optional personal income during afternoon/evening shifts, blocking studio work
for those hours. Hired assistants retain their studio wages.

**Send to convention** on a series or book selects its title automatically. Pick
an attendee and event, review stock and the booth/travel total, then book. The
attendee goes automatically, promotes that title and sells its available stock.

The [alpha.4 change record](docs/superpowers/private-alpha-feedback-2.md) explains
balance choices, historical benchmarks and validation.

Alpha.5 adds explicit publishing status to Series cards/details, including a
direct offer-review action. Genre inputs are dropdowns in every creation form.
Escape releases text-entry focus first, preserving the text and current panel.

### Alpha.6: GUI redesign and office life

Alpha.6 adds series sales/stock/fan counters, a clickable unread count,
one-shot continuation, free direct online listings, convention stock reservations
and sooner local events, plus visible speed selection. Office changes add
door-based staff travel, Helper-Chan activities and following, decorative breaks,
and a furnished break room separated from the corridor by a wall and doorway.

The GUI now has persistent navigation, full overview pages, focused action/quote
cards, priority inbox views and transparent Helper-Chan guidance. It supports
16:9 and 21:9 layouts, compact spacing and reduced interface motion.

The [office-life change record](docs/superpowers/private-alpha-feedback-3.md)
covers these changes.

### Alpha.7 to alpha.11: characters, family home and pacing

Later alphas add creator name and appearance setup, animated Helper-Chan and
parents, modular chibi staff, family routines, a WC, genkan and rear stairs,
residential surroundings, a weighted chapter progress bar, screen-space
nametags, and a working day of about 45 seconds at 8x with a 32x overnight skip.
See [family home refinement](docs/superpowers/family-home-refinement.md),
[character integration and day length](docs/superpowers/character-integration-and-day-length.md)
and the [UI and UX sweep](docs/superpowers/ui-ux-sweep-2026-09-25.md).

### Alpha.12: Tier 1 fixes and the fresh-player test

Alpha.12 carries Tier 1 fixes 1 to 7 (career guidance, first-hire safety,
setbacks and economy, difficulty, the debut wait, 32x pacing and the display
sweep) and the fresh-player test kit: a session timeline attached to problem
reports, a practice career that reaches a cancellation warning, and tester
instructions and a questionnaire in the package. See the
[preparation record](docs/superpowers/fresh-player-test-preparation-completion.md).

The latest local Windows package is
`builds/MangakaStudio-0.8.0-private-alpha.12-Windows.zip`. See the
[alpha.12 build verification](docs/superpowers/alpha-12-build-verification.md).
The [release plan](docs/superpowers/specs/2026-09-22-roadmap.md#release-plan)
sets out what remains before Steam Early Access.
