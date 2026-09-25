# Alpha.10 Windows build verification

2026-09-25: user authorized compilation, packaging and showing the result.

- Executable: `builds/MangakaStudio-0.8.0-private-alpha.10-Windows/MangakaStudio.exe`.
- Package: `builds/MangakaStudio-0.8.0-private-alpha.10-Windows.zip`, 141,232,435 bytes.
- SHA256: `00176F26F195C2C7F9C4ACE684C6F10A18304A3BC2E8742455F41634F5908609`.
- Earlier alpha packages were retained. Only preliminary alpha.10 outputs from this build session were replaced.

## Included changes

The UI sweep adds clearer cards, primary actions, selected account/period controls, publishing and stock information, expandable details and corrected navigation. Personal savings and the doujin budget remain separate before incorporation, with green gains and red expenses beside the appropriate balance. Includes the family routines, visible usable WC, outside/inside continuity, clear sky-window setback, immediate seated facing and gestures that follow the selected speed.

## Executed checks

- Solution and final game compilation: zero warnings/errors with warnings treated as errors. Asset import completed with an empty error log.
- Simulation regression suite: 550 passed, none failed or skipped. TRX retained.
- Management: initial 89 headless checks; comprehensive rendered pass 115 checks. Final focused-capture run passed 94 checks after compact-header/guidance fixes. The count differs because screenshot existence checks count only captured images.
- Production: 25 rendered checks passed, including printing, delivery, direct convention booking, reservation quote and confirmation visibility at 1280x720.
- Usability: 45 headless and 50 rendered checks passed, including dark/light settings, camera controls and publishing shortcuts.
- Office-life: 734 headless checks passed. Atmosphere: 842 headless checks passed, including overnight timing and restored speed.
- Family home: 68 headless and 77 rendered checks passed. Covers exclusive WC use, both parents' outings and returns, staggered meals, same-door emergence, pause, selected-speed gestures and immediate desk facing.
- Final Windows export: 21 opening/save/report checks, 68 family-home checks and 45 usability checks passed directly against the packaged executable, with empty error logs.
- Inspected actual rendered AVIF captures for finances, Books, staff, office sidebar, convention booking, light settings, large text, house overview and the WC. 16:9 and 21:9 previews include 1920x1080 and 2560x1080; management also checks 3440x1440. Temporary JPEG inspection copies were deleted.

## Issues resolved during verification

The production test's ambiguous Series selector was corrected to explicitly use the navigation rail. The convention quote was shortened and its explanatory details made expandable so inputs and confirmation fit at 720p. Sidebar headings stay on one line with a compact refresh icon. At 720p and 150% text, compact guidance and header spacing retain the time controls, Save and a usable management scrolling area; full guidance remains in Help. The capture filter waits for layout changes even when skipping an image.

Logs/screenshots: `TestResults/alpha10-build`. Final packaging logs are copied into its `package-check` directory. No external release was uploaded. These are local automated and visual checks, not clean-machine or extended human playtesting.

NuGetAudit=false was invocation-only because the audit service was previously unavailable. No repository-wide setting changed and this is not a fresh vulnerability scan.
