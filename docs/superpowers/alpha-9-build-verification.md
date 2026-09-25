# Alpha.9 Windows build verification

2026-09-25: user authorized compilation, packaging and showing the result.

- Package: `builds/MangakaStudio-0.8.0-private-alpha.9-Windows.zip`, 141,214,942 bytes.
- Executable: `builds/MangakaStudio-0.8.0-private-alpha.9-Windows/MangakaStudio.exe`.
- SHA256: `FA2A1B502BC935BD0C7063F3889D8F19C9DBC6341B8E67A8DC64835B4D0DFAA4`.

Includes staggered parent meals, the rear staircase and upstairs retirement, the lowered tiled genkan and shoe storage, and compact residential blocks. All earlier alpha packages were retained.

## Executed verification

- Solution and final game compilation: zero warnings/errors, warnings treated as errors.
- Asset import: successful, empty error log.
- Office-life walkthrough: 734 headless checks passed, including employee and companion arrivals/departures through the new entrance route.
- Atmosphere walkthrough: 786 headless and 809 rendered checks passed, including 16:9/21:9, camera bounds, overnight transitions, both parents' meals/stair traversal and non-overlap with creator breaks.
- Final targeted family-home rendered walkthrough: 20 checks passed after the stair-floor fix.
- Final standalone export: 21 opening/save/report checks and 12 family-home checks passed; separate packaged office-life run logged alongside these checks.
- Visually inspected house overview, stair/entrance detail, genkan, compact neighborhood and a parent at the table. Final refreshed screenshots are AVIF, captured from the compiled game with UI hidden for the detail views.

The meal check revealed that a passing employee could cancel dinner; occupancy now distinguishes passing from using/targeting the chair. Its fixture also needed an occupied work hour rather than the overnight fixture's pre-arrival hour. Visual review caught coplanar surfaces at the stair foot; the underlying foundation was lowered slightly and the final captures refreshed.

NuGetAudit=false was used for these build invocations, following the unavailable audit service during alpha.8 qualification. No repository-wide audit setting changed. This is not a fresh online vulnerability scan. Simulation gameplay was unchanged; its 550-test alpha.8 result was not rerun or represented as a new alpha.9 run.

Logs/screenshots: `TestResults/alpha9-build`; packaging logs: `TestResults/package-check`. Local checks do not replace clean-machine or extended human playtesting. Nothing was uploaded or published externally.
