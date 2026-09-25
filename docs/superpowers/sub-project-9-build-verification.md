# GUI redesign: alpha.6 build verification

Date: 2026-09-25. The user explicitly authorized compilation and packaging.

Delivered Windows x64 package: **0.8.0-private-alpha.6**.

- ZIP: `builds/MangakaStudio-0.8.0-private-alpha.6-Windows.zip`
- Executable: `builds/MangakaStudio-0.8.0-private-alpha.6-Windows/MangakaStudio.exe`
- SHA256: `881D08FA1E1EBCA0C298CB87BD7557706FC8B76536AD9B442B7557B1F2F5F402`
- Standalone Godot .NET export with runtime, artwork, START-HERE guide, credits
  and engine/runtime third-party notices. Earlier packages were retained.

## Executed checks

| Check | Result |
| --- | --- |
| Godot C# project compilation | Passed, 0 warnings and 0 errors |
| Simulation test suite | 538 passed, 0 failed, 0 skipped |
| Full developer scene regression | 755 checks passed, empty final error log |
| Rendered management smoke | 86 checks passed |
| Rendered production and convention smoke | 25 checks passed |
| Rendered online sales, reserves and convenience smoke | 30 checks passed |
| Usability / keyboard / themes | 45 checks passed |
| Publishing status / text focus / genre selection | 18 checks passed |
| Alpha opening / guidance / save and report export | 21 checks passed |
| Awards / licensing / progression | 15 checks passed |
| Office life / doors / companion behavior | 667 checks passed |
| Exported Windows executable | 21 alpha smoke checks passed; reports alpha.6 |

Logs and screenshots are under `TestResults/gui9-build`; packaged-executable
checks and export logs are under `TestResults/package-check`. The final runs
have no Godot error output. Rendered captures include 1280×720, 1920×1080,
2560×1080 and 3440×1440, plus larger text and important-message presentation.
The 1920×1080 and 2560×1080 office/sidebar captures and the corrected 720p
convention form were visually inspected.

The local 32-staff rendered management run recorded a 5.82 ms median frame and
6.08 ms p95 at 8×. This is one development machine, not a hardware guarantee.

## Fixes found during qualification

- Avoid calling `GetTree()` while constructing labels before their parent enters
  the scene. This removed startup engine warnings.
- Put the convention confirmation beside its quote so it remains fully visible
  with the reservation fields at 720p.
- Update old harness expectations for the already-approved 95-skill prodigy,
  personal self-publishing targets, no deadline-driven overtime and wrapping
  queue rows. Office fixtures now distinguish a companion following an employed
  mangaka from both characters correctly being absent during off-duty/relocation.
- Dismiss the regression fixture's recap before opening its career dialog;
  the harness must respect the same modal ordering as normal input.
- Export directly through Godot's main executable, avoiding a console-forwarder
  process that remained open after writing the export. Packaging now resolves a
  supplied console path to its sibling engine and bounds the export wait.
  The first attempt was retained under `TestResults/gui9-build/export-console-attempt`.

This verifies the local private-alpha candidate. Clean-machine, wider hardware,
extended human playtesting and exhaustive combinations of all page/density/text
settings remain separate. Nothing was uploaded or published externally.
