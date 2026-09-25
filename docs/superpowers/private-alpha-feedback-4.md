# Opening menus, overnight passage and family-home atmosphere

Delivered in `0.8.0-private-alpha.7` after the user authorized compilation and packaging. The previous alpha.6 executable and ZIP remain available separately.

## Agreed behaviour

- Separate opening-menu actions visually. Character setup, career rules, display/audio, Helper-Chan preferences and difficulty settings have distinct sections. Custom rules and sandbox assists do not fill the initial screen; the permanent achievement warning stays visible in their section.
- Continuous, stylized sunrise/day/sunset/night lighting follows the simulation clock. Windows show changing sky colour and a moving sun or moon. Warm wall lights have visible illuminated fittings.
- Daily recap **Continue** starts the overnight presentation. Aki, Helper-Chan and any residents already crossing finish their doorway routes before the lights switch off. An empty-room beat and a midnight beat make the elapsed time visible.
- **32× is temporary**, only for the overnight skip. Morning restores the previous player-selected speed (1×, 2×, 4× or 8×). Pausing or choosing a speed cancels the transition; keyboard speed changes use the previous normal speed as their starting point.
- There are no overnight interruptions. Simulation bookkeeping continues, with notices queued for morning. Normal morning notification rules can then pause the game without losing the previous speed.
- The family home has two fixed older chibi residents: a father with silver temples, glasses and a cardigan; a mother with a silver bun, glasses and an apron. Visits are much less frequent than shared-office traffic. Residents already walking finish at a door when visiting hours end.
- None of these visual actors, lighting changes or departures change staffing, wages, production or the simulation's saved clock rules.

## Validation boundary

The source-only phase passed a 166-file syntax parse. After compilation was authorized, the Godot project built with zero warnings and errors, and all 538 simulation tests passed (3 minutes 14 seconds).

The rendered `--atmosphere-smoke --capture` run passed 1,105 checks, including opening/career menus at 1280×720, 1920×1080 and 2560×1080; closing-time departure; darkness/dawn; notification deferral; restoration of every normal running speed; and quieter parent traffic. AVIF images were visually inspected. That review caught an off-screen overnight caption, which was corrected and checked again. The speed-loop fixture now resets its read-message state between independent careers.

Additional checks passed: full Godot regression 823, office-life 735, management 69, usability 45, and alpha 21. The exported executable passed its 21-check alpha walkthrough and a separate 1,096-check atmosphere run with empty error logs. A first sandboxed package run could not write normal Godot logs or read Windows certificates; the authorized normal-permission rerun passed cleanly. Lighting timings remain presentation choices, not an astronomical Tokyo calendar.

Logs and AVIF captures: `TestResults/alpha7-build/`. Export and packaged alpha logs: `TestResults/package-check/`.

Windows archive: `builds/MangakaStudio-0.8.0-private-alpha.7-Windows.zip` (109,626,482 bytes, 194 entries). Executable file/product version: `0.8.0.7`. The archive contains the runtime, game data, alpha.7 instructions and required license notices.

SHA256: `47A49F7652A3405621257565FE1F9B2DAB95700BA34B8FA5AC3DC391C8F8499B`.

## Screenshot storage

Converted and fully decoded/verified 173 existing test screenshots to AVIF: 32,163,974 bytes became 10,895,039 bytes (66.1% smaller). Original PNG screenshots were removed only after successful conversion; 30 saved-career artwork fixtures remain in their original format because saves reference them. The per-file conversion record is `TestResults/screenshot-avif-conversion.json`.

Smoke/prototype captures use a shared embedded Python/Pillow encoder at quality 85 with full 4:4:4 colour. No PNG screenshot is written to disk. Both the isolated stdin workflow and the compiled C# capture path passed; converted GUI captures were visually inspected for readable text. Normal gameplay, artwork imports and report attachments do not acquire a Python dependency.
