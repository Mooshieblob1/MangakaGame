# Alpha.11 Windows build verification

2026-09-26: user authorized compilation and packaging of the accumulated changes.

- Executable: `builds/MangakaStudio-0.8.0-private-alpha.11-Windows/MangakaStudio.exe`.
- Package: `builds/MangakaStudio-0.8.0-private-alpha.11-Windows.zip`, 141,242,903 bytes.
- SHA256: `C7839858695975AEBE5C72A98CDA63E3C11E5EBDEE92F51D77737DE11FC8D224`.
- Executable file/product version: `0.8.0.11`. Previous alpha packages were retained.

## Included changes

The header uses shorter speed, unread, Save and account controls. The studio selector shows the full current location name, with its ward beneath it. Chapter progress is a single weighted total with coloured stage sections and a readable legend, retaining completed work when the next stage begins.

Room labels and identifiable character names render above the scene at a readable screen size, including Mom and Dad. The south walls are continuous around the intended door openings. WC occupants remain visibly seated under a localized blur while Helper-Chan stays at her desk. Characters finish movement exactly at their seat or doorway.

The default ten-hour working day lasts about 45 seconds at 8x, retaining the selected movement and gesture speed. Pausing the overnight transition preserves its target and progress; resuming uses 32x until morning, then restores the previous daytime speed.

## Executed checks

- Solution compilation with warnings treated as errors: zero warnings/errors. Asset import completed with an empty error log.
- Simulation regression suite: 550 passed, none failed or skipped; TRX retained.
- Final rendered management pass: 122 checks, including 1920x1080, 2560x1080, 3440x1440, and 1280x720 at 150% text size. The occupied studio performance sample measured 9.05 ms median / 10.35 ms p95 with 32 staff, six passers-by and Helper-Chan at 8x on the local RTX 5070.
- Production: 22 checks with a focused convention screenshot, covering printing, delivery, booking/reservation information and confirmation visibility at 720p.
- Usability: 48 headless checks and 49 with a focused progress screenshot. Covers weighted total chapter progress, stage changes, camera controls, shortcuts and publishing navigation.
- Office-life: 734 headless checks after the final-position correction.
- Atmosphere: 885 headless checks, including each previous daytime speed, pausing/resuming overnight with buttons and keys, restored morning speed, 45-second daytime pacing, staff and parent WC visits, family outings, meals and selected-speed gestures.
- Family home: an initial comprehensive rendered pass of 85 checks, followed by 76 checks with a focused occupied-WC capture after reducing the blur strength.
- Final standalone executable: 21 opening/save/report checks, 885 atmosphere/family checks and 49 rendered usability checks passed directly against the packaged game.
- Inspected rendered AVIF screenshots of the compact header, stage legend, full studio selector, 16:9 and 21:9 layouts, large text, convention booking, house walls, names and occupied WC. Temporary JPEG inspection copies were removed; retained screenshots use AVIF.
- Verified archive contents include the executable, PCK, standalone runtime, START-HERE and engine/runtime notices, without development logs or source files at archive level.

## Corrections during verification

The arrival tolerance allowed a character to stop about 0.027 scene units short of the WC seat; completed movement now snaps to its exact destination before seating. The headless convention check now waits for the 720p layout to settle, independently of whether screenshots are enabled.

Rendered inspection caught letter-by-letter wrapping in the new stage legend. Labels now keep stage names intact and the flow container wraps complete entries. A general option-button setup was overriding the studio selector's no-clipping setting; the selector now retains its intended setting. The rail title also keeps its two intended lines at large text sizes. WC blur was narrowed and softened enough to preserve the seated silhouette.

One standalone atmosphere run emitted a shutdown warning about two remaining ObjectDB instances after all 885 checks passed. A verbose diagnostic rerun of the same executable also passed all 885 checks and exited without the warning. The other final rendered checks and export error logs were empty. The intermittent shutdown warning was not reproduced or established as a gameplay issue.

Evidence: `TestResults/alpha11-build`, including a copy of the final export and opening-check logs in `package-check`. No external release was uploaded. These are local automated and rendered checks, not clean-machine or extended human playtesting.

The initial build used invocation-only `NuGetAudit=false`, following earlier build runs; this is not a fresh vulnerability scan. No repository-wide audit setting changed.
