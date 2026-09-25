# Sub-project 6 delivery record

Implemented locally on 24 September 2026 following the user's “do it”. Existing
uncommitted publishing, studio, office and historical work was preserved. No
commit or push was requested.

## Delivered

`ManagementMain.tscn` is the default entry point. The cream/ink/teal interface
opens with career menus and uses the office as its home screen. Series, staff,
finances, studios, Industry, inbox and Help occupy a single side panel, with Back
and expanded views. Detailed actions use the existing command-backed controls
inside the restyled Studio workbench. Its developer event stream is hidden and
command errors are visible in the footer. The date, speed and separate business
and personal balances remain accessible above the workbench. The complete
[action inventory](management-ui-actions.md) records the routes and authority.

Reports include cash balances, operating income/spending, physical copies,
domestic digital copies, overseas copies and title rankings. Period controls
offer month, quarter, year and all time; charts use real dates. Exact figures
and paginated ledgers remain readable without hovering. New physical sales and
issue samples retain edition/business identity. Imported careers explicitly
start this history at conversion rather than inventing earlier observations.

The inbox derives pending decision cards from current state, separately from
read acknowledgments. Important notices use Helper-Chan, pause play and coalesce
queued duplicates; routine results remain in the inbox. The first released book
receives a milestone popup, while later ordinary releases remain messages.
Optional first-use guidance can be disabled and revisited. Existing daily recap
and next-arrival behavior is retained.

Helper-Chan uses the supplied image as her neutral portrait plus three generated
expressions. Her dedicated stylized 3D mesh has enormous curling blonde twintails,
blue eyes, glasses, blouse, pencil skirt, stockings, shoes and clipboard. She sits
or stands beside her equipped desk, with small idle/hair movement. A visual alcove
preserves every ordinary furniture cell, usable workstation, door route and
economic rule. She follows the protagonist into rentals and employer workplaces
and does not appear as a worker, wage, usable desk or production bonus. The
[asset notes](../../godot/Assets/README.md) and
[generation manifest](../../godot/Assets/generation-manifest.json) preserve provenance.

The six-scene “The First Reader” arc and four recurring everyday scenes have
choices, remembered callbacks, defer/skip controls, cooldowns and a journal.
Saved narrative randomness is independent of gameplay randomness. All three
answer paths (first answer, second answer, skipping) complete and replay.

Showcases provide a cover, three representative panels, editable synopsis and
release history. Twelve original genre atlases supply twenty-four color cover
variants and monochrome panels. PNG/JPEG imports have fit/center-crop previews,
per-slot reset, size limits before decoding and immutable managed storage.
Artwork never changes gameplay statistics and no runtime AI service is required.

Version-6 career snapshots include simulation and presentation state. Named
manual snapshots are retained; three daily autosaves rotate per career. Atomic
rename publishes each immutable snapshot after flushing, with failure injection
tests proving earlier saves survive. Continue falls back to a valid earlier
snapshot; load pauses; corrupt simulation is rejected. Portable `.mangaka`
exports include referenced art, and imports create a separate career. Explicit
v3/v4/v5 imports preserve original files and establish a new replay boundary.

## Verification

- Solution build passes with warnings treated as errors.
- 444 regular simulation tests pass; the separate forty-year test also passes.
- Existing Godot regression suite: 758 headless checks and 823 rendered checks.
- New management walkthrough: 56 headless and 70 rendered checks, including real buttons,
  save/load, portable artwork, story answers, important notifications, page
  navigation, all sixteen furnished rentals, full workforce and employer moves.
- Screenshots inspected at 1600×900 and 1280×720, including 150% text, dialogue,
  model close-up, showcase, finances and workbench.
- Forty simulated years: 158.41 seconds; 65,234,968 JSON characters; 25,799 events;
  save/load continuation remains identical. This deliberately idle simulation
  fixture is not a maximal player-generated content stress test.
- RTX 5070 rendered management fixture with 32 staff, six ambient occupants and
  Helper-Chan at 8×: median 7.49 ms, p95 12.49 ms. These local measurements do not
  establish performance on other hardware.

Evidence is in ignored `TestResults/ui-*.log`, `ui-*.err` and
`management-*.png`, plus the long-run report under the test build's `TestResults`.
Use the README commands to reproduce the tests. Test saves live under isolated
`TestResults/careers-*` directories and do not replace real player saves.

## Concrete implementation choices and limits

The existing simulation driver and working management forms are shared by the
new scene and debug harness, rather than duplicated. Four portrait states ship;
six had been a proposed default. The companion is a stylized 3D interpretation
of the supplied illustration; her 2D neutral portrait is the supplied image.
The desk alcove is presentation-only and does not claim extra rented capacity.
The short authored story uses contextual lines and fixed choices, not generated
dialogue. Bundled manga art is reusable illustrative fiction, not bespoke art
for every simulated chapter. Current period charts may be incomplete.

Snapshot publication uses immutable files and atomic rename instead of swapping
whole directory manifests. Referenced artwork remains immutable across older
manual saves. Text scaling, focusable controls and figure tables are included;
there is no claim of screen-reader certification or a full accessibility audit.
Native system file-picker interaction is not automated; the decoding, preview,
managed asset and portable storage paths are exercised separately.

No new Japanese geography, rent, credit or historical claims were introduced.
Existing researched data and the free parents' home remain authoritative.
Awards, anime adaptations and the broader balance/polish pass remain Sub-project 7.
