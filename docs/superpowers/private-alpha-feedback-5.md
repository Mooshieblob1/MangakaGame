# Creator setup, floating panels and clearer overnight speed

## Money header follow-up after alpha.9

- Before incorporation the header shows Personal savings and Doujin budget as separate balances, as selected by the player. The production account is labelled Business funds after incorporation, or Employer funds when playing as an employed lead. Its tooltip explains protected spending availability.
- Each balance has its own green +yen and red negative-yen amounts beneath it, sourced from fresh ledger entries. Income and expenses remain separate even if their net change is zero. Contributions show the corresponding personal expense and production receipt.
- Recent changes accumulate within a four-second display interval, with a final one-second fade. Timing uses real UI seconds, so amounts remain readable at 8x and while paused. Reduced UI motion removes the fade. The feedback line keeps its space to prevent layout jumps.
- Loading, account changes and rewinds establish a new baseline rather than showing existing savings as income. Feedback does not alter the simulation, balances or save format.
- Source syntax checks passed. Added management smoke coverage for transaction grouping, duplicate refreshes, expiry, contributions, incorporation/employer captions and loading. Compilation, runtime layout checks and those smoke checks await the next authorized build.

Implemented in source after alpha.7. The alpha.7 Windows package remains unchanged; these changes are pending the next authorized compilation and build.

## Starting prodigy

- New Career includes a name field defaulting to Aki. Blank/whitespace input uses Aki; surrounding whitespace is trimmed. Names support Japanese characters, have a 40-character limit, and reject control characters.
- The name is established before career initialization, including the starting studio name. Saving/loading and timeline replay retain it. No save-format change is required.
- A live, isolated 3D viewport uses the actual office chibi model. Skin, hair colour, outfit, hairstyle and glasses update it immediately. Dragging or turn buttons show the model from other angles. Previewing does not change the active career or its random state.

## Office layout

- The 3D scene fills the canvas. Navigation, top status/progress, bottom guidance, and right-side Inbox/Series panels float above it. Full management forms remain large overlays; the scene remains behind them.
- Empty overlay space passes mouse input through to the office; controls retain their own input handling.
- Menu is a single left-navigation entry. Removed the duplicate top Menu entry and bottom Inbox/Series shortcuts. The requested clickable unread count remains, as do full-page/side-panel switches.
- Implemented in the existing Godot UI. No HTML/CSS runtime or web UI migration was introduced.

## Overnight feedback

- The simulation already advanced at 32x; 2x departures and two 0.65-second holds made the overall presentation feel slower. Closing-time departures now use 8x, the two visual holds are 0.2 seconds each, and the actual overnight clock remains 32x.
- The transition has a persistent highlighted 32x header badge, a larger overnight/dawn banner, current/destination times, and a progress bar. The speed flash repeats when closing is complete and the overnight skip begins.
- Overnight remains uninterrupted. Morning restores the previous selected speed; deferred notices then use the normal notification rules.

## Validation boundary

169 non-generated C# source files passed a syntax parse. No compilation, game execution or new package has been performed for these changes.

Added identity/save/replay and unchanged-world-generation tests. Expanded the atmosphere smoke to cover the live creator preview, name/appearance application, full-canvas office and single Menu action at 16:9/21:9 sizes, and total overnight duration versus ordinary 8x playback. Updated production smoke expectations for management overlays above the visible office. These tests and new AVIF captures still need to run after compilation is authorized.

## Reference-led UI and residential surroundings

- Adopted the supplied image's dark navy panels, fine outlines, blue selected controls, left rail, right series dashboard and larger bottom Helper-Chan portrait. Existing transparent character artwork remains the source. The reference is design direction, not a replacement screenshot or an imported apartment asset.
- The Office view now includes a scrollable right dashboard with current title, publishing status, numbered production progress, copies sold/stock/reservations, business totals and creation/publishing/convention actions. Full management forms retain their existing routes. Inbox remains available beside the scene.
- Funds have a separate header tile. There is still only one Menu entry, on the left. Floating panels leave the 3D scene full-canvas, with the overnight banner positioned in the unobstructed area.
- Added a ground-level foundation, entrance approach, roofed private wing, modest garden boundaries and planting for the family home. Detached neighboring houses, pitched roofs, windows, residential lanes, greenery and utility poles fill the surroundings. Geometry is decorative, deterministic and cached independently of furniture selection; no extra residents or simulation costs are introduced.
- Residential visual reference: [Nerima's official English guide](https://www.city.nerima.tokyo.jp/gaikokunohitomuke/bunka.files/kaigaimukejouhoushi2019.pdf), describing its detached-house residential neighborhoods and greenery. This guides the broad setting, not an exact address or a historical reconstruction. Modern subsidy amounts are not used.
- Camera limits account for the full orthographic ground footprint, zoom, rotation and aspect ratio, including pixels behind the UI. Pan ends at a hard stop before scenery edges; extreme aspect ratios also constrain zoom. Restored cameras and Helper focus use the same bounds. Screen-aligned dragging is preserved using equivalent ground-plane translation, preventing vertical camera drift.
- Added unrun smoke coverage for viewport corners at both zoom limits, six rotations, extreme panning, restored vertical offsets and 1280x720 / 1920x1080 / 2560x1080 layouts. Runtime appearance and performance remain unverified until compilation is authorized.

## Tripo experiment

- Prepared `scripts/tripo-chibi-trial.ps1` using the [v3 quick start](https://developers.tripo3d.ai/en/docs/quick-start). Reads TRIPO_API_KEY from the Windows user environment without printing or persisting it. Generates one short-legged, 2.5-head-tall mangaka with an 8,000-face target and standard textures; no rigging or paid add-ons requested.
- Listed base generation cost: 20 API credits. Checks available credits before submission and preserves task IDs to avoid accidental duplicate charges.
- The initial request was blocked by zero API credits. After the user's top-up, the mangaka trial succeeded for 20 credits, followed by the requested reference-based Helper-Chan trial for 30 credits. Both downloaded and rendered in a standalone browser preview. Helper-Chan passed the free biped riggability check; 450 credits remain. See [asset trial results](tripo-asset-trial.md). No purchase, subscription, game asset replacement or compilation performed by the agent.
