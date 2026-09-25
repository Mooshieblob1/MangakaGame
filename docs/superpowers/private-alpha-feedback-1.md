# Private alpha feedback: controls, printing and themes

Date: 2026-09-24. Build `0.8.0-private-alpha.2`. Implemented locally.

## Changes

- Middle-mouse drag moves the scene along the camera's screen axes at every
  rotation, including vertical movement. WASD pans in screen directions.
  Camera saves preserve the extra vertical component and accept older cameras.
- Space pauses/resumes the previous speed. 1 decreases and 2 increases speed
  through paused, 1x, 2x, 4x and 8x. Typing, menus, dialogs, Helper-Chan popups
  and furniture editing protect against unintended time shortcuts. WASD remains
  available in the furniture editor when no text input is focused.
- Books is a primary navigation destination, with unfinished production and
  finished books, stock, physical copies sold and next local sales check.
  Series details also link directly to printing. Current navigation is highlighted
  and new pages start at the top; Back retains the previous scroll position.
- Printing has a prominent order action, price quote and live status. It preserves
  the player's printer/quantity while time advances, prevents duplicate orders,
  and explains insufficient cash/storage. Loading a career rebinds the form.
- Local physical distribution starts on delivery and sales settle on Mondays.
  Closed sales windows, sold-out stock, digital agreements and overseas agreements
  are explicitly distinguished. A digital edition with no print history is not
  described as physically sold out. These are read-only explanations of existing
  rules, not changes to simulation demand or economics.
- Dark mode defaults on across management panels, workbench, menus, cards and
  plots. Menu → Settings offers light mode. The preference is application-wide,
  independent of careers, and is written atomically to user data. Smoke runs use
  isolated preferences and careers.

## Verification

- Warning-free solution build.
- 518 simulation tests passed; the four new distribution checks also passed after
  the final copy change. They cover ordering/delivery/sales, Monday-midnight
  delivery, sold-out/closed windows, and read-only channel queries.
- 56 existing management checks passed.
- 39 rendered usability checks passed in the editor build and an exported build:
  camera projections at three rotations, old/new camera preferences, direction
  movement, time shortcut boundaries, input guards, Books, live printing,
  load/rebind, theme persistence and 1280x720 layout.
- Packaged opening-to-first-sale checks passed, including local reporting,
  compressed saves and bounded audio.
- Captured dark printing, dark Books at 720p, dark finance charts and light Settings
  were inspected. Runtime error logs were empty.

The Windows package remains a private local test build. These checks ran on the
development Windows machine with an RTX 5070; they are not clean-machine or broad
hardware qualification. No commit, push or public release was requested.

## Package

`builds/MangakaStudio-0.8.0-private-alpha.2-Windows.zip`.
SHA256: `43EE0352F3F584FAD705DE900A022A9C0853E8998D3263384B3EA58B972123C7`.
Extract the entire folder and launch `MangakaStudio.exe`. Existing career saves
remain in user data. The earlier alpha.1 archive is retained.
