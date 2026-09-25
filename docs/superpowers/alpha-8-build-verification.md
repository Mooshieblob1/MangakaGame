# Alpha.8 Windows build verification

2026-09-25: user explicitly authorized compilation and packaging.

Package: `builds/MangakaStudio-0.8.0-private-alpha.8-Windows.zip` (141,200,336 bytes).
Executable: `builds/MangakaStudio-0.8.0-private-alpha.8-Windows/MangakaStudio.exe`.
SHA256: `CE39966144C62CB064952977F69EFEDA12D956A56EC16805A6080B3BE9E52AD3`.

Includes the approved animated Helper-Chan and parents, customizable modular employees and creator preview, floating office interface and neighborhood, and the 60-second default working day at 8x. Overnight still uses its separate brief 32x presentation. Previous packages were retained.

## Executed checks

- Solution and final game compilation: zero warnings/errors with warnings treated as errors.
- Simulation suite: 550 passed, none failed or skipped.
- Godot asset import: successful, empty error log.
- Rendered office-life checks: 737 passed, including imported Helper, modular staff, writing, pause, break visits, following, doors and returns.
- Rendered atmosphere checks: 765 passed, including imported parents, creator appearance, 16:9/21:9 layouts, camera bounds, day duration and overnight speed restoration.
- Standalone executable: 21 opening/save/report checks, 733 office-life checks and 750 atmosphere checks passed.
- Inspected AVIF captures of Helper at her desk, the shared break, creator setup at 1920x1080 and the residential office at 2560x1080. Temporary JPEG viewing copies were removed; retained screenshots are AVIF.

NuGet's vulnerability service was unavailable even with network permission. Builds used installed packages with NuGetAudit=false for this invocation only; no project-wide audit setting was changed. This is not a completed online dependency vulnerability scan.

The rendered break test initially reset its manually selected speed during screenshot refresh; its setup now sets the actual speed control as well. The packaged opening test also used the obsolete Direction button label; it now uses What should I do?. Neither required a gameplay behavior change.

One standalone atmosphere run reported two ObjectDB objects remaining at shutdown. An immediate verbose rerun passed all 750 checks with no warning; the intermittent shutdown warning remains recorded rather than claimed fixed. Main packaging/opening and final rendered checks have empty error logs.

Evidence: `TestResults/alpha8-build` and `TestResults/package-check`. The initial export is retained under `TestResults/alpha8-build/first-export`. Clean-machine and extended human playtesting remain separate. Nothing was uploaded or published.
