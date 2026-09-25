# Approved character integration and longer playable days

## Runtime changes

- Helper-Chan, Mom and Dad now load their approved animated GLBs through a shared `CharacterModel` visual. The generated text-only mangaka experiment is not used; customizable Aki, employees and generic visitors keep the shared modular kit and saved appearance recipe.
- Imported instances use independent materials and animation libraries, explicit looping clips and manual advancement so pause/editing do not let animations run on their own. Mesh bounds include seated poses and wide hair.
- Existing door routes, companion following, break visits, ambient roles, selection and parent hyperlapse exposure remain in their owners. Helper retains her existing +Z-facing convention and movement/gesture speed; ordinary actors face -Z. All imported assets use the reviewed 1.2 scale.
- Helper writes and checks her clipboard at her desk, walks while following, and uses the seated clip on breaks. No unsupported hair-touch animation is advertised. Her chair/anchor and paper were aligned in source to the short-arm writing pose. Exact engine clearance remains unverified.
- Parent surface materials are included in the existing hyperlapse opacity path without assuming MaterialOverride. Separate instances do not change one another's materials. Import failure retains existing procedural fallback; smoke checks require the real imported models.

## Day pacing

The user's latest pacing choice is about 45 seconds for a working day at 8x, replacing the earlier one-minute choice. The default schedule is 08:00-18:00 (ten simulated hours). The daytime clock now uses 36 real seconds per simulated hour at 1x, giving 360 seconds (6 minutes) at 1x and 45 seconds at 8x. Pauses, alternative schedules, overtime and recap reading can change total elapsed play time.

Speed selections remain 1x/2x/4x/8x. Movement, animation multipliers, simulation work per hour, deadlines, wages and the 24-hour calendar are unchanged. The dedicated overnight sequence keeps 32x and its previous 2.5-second baseline independently, including lights-out, dawn, no overnight interruptions and restoration of the selected speed.

## Verification boundary

Update: the subsequent user-authorized compilation and packaging completed as alpha.8. See [executed build verification](alpha-8-build-verification.md). The notes below describe the earlier source-only handoff.

All 13 changed C# files passed a Tree-sitter syntax parse. Required skin/clip names were checked directly in all three GLBs; day-length arithmetic and separate overnight timing were checked. Godot API names were checked against the installed 4.7.2 XML reference. No compiler, Godot process, tests requiring compilation, screenshot capture or build was run.

A broader parse flags the untouched `GameState.TimelineValidation.cs` around `location?[field]`; this does not affect the changed-file syntax result and was not treated as compiler evidence or edited as part of this task.

Office-life smoke now requires modular employees, imported Helper clips and paused animation time. Atmosphere smoke requires both imported parents and the 60-second daytime/separate overnight contract. These checks are added but unrun. Next authorized engine pass must verify actual GLB import, facing, texture lighting, pause/resume, desk and break-chair clearance, follower door traversal, parent exposure and speed restoration, before packaging.

## Source follow-up after alpha.10

The daytime baseline is now 36 seconds per simulated hour, giving the requested 45-second default workday at 8x. Overnight pause no longer cancels the transition: it freezes closing-time departures, elapsed night progress and the dawn target. The pause button and Space resume at 32x; keyboard 1 pauses the transition and 2 resumes it. Daytime speed buttons are disabled until morning, when the original speed is restored. Paused-night captions and the header badge explain the current state. Loading or starting another career explicitly clears the previous transition.

Atmosphere regression checks now cover pausing during departure and at midnight, mouse and keyboard resume, prevention of ordinary 8x overnight playback, restored controls and the 45-second pacing. These updated checks await compilation and an authorized engine run.
