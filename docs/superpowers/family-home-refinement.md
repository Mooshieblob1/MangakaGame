# Family home: staggered meals, rear stairs, genkan and compact surroundings

## Follow-up after alpha.9: routines, WC and seated gestures

Implemented and checked in the authorized alpha.10 build. See [alpha.10 verification](alpha-10-build-verification.md).

- Parents now choose actual WC, meal and outing destinations instead of walking from the stair landing back to itself. They wait inside the WC or outside before returning through the corresponding door. Exit/entry counters and residence state prevent consecutive exits or entrances. Outside status survives overnight and switching the viewed studio during the same game session; these remain cosmetic routines, not serialized simulation state.
- A furnished cutaway WC has a toilet, hand basin, mirror, towel, tiled floor and a narrow hinged door. Staff and parents reserve it exclusively, walk to its door, spend a short interval inside with an occupied indicator, and leave from that same door. Helper waits in the hall during the protagonist's visit. WC visits do not change production or needs. Closing time lets occupants finish leaving before the overnight transition.
- The decorative building directly behind the sky windows is removed, leaving their sky-facing setback clear. The surrounding compact neighborhood remains.
- Eating, seated work and Helper's desk gestures follow the selected game speed, including 8x. Helper's previous 2x animation cap is removed. Walking also retains the selected speed; pausing freezes the gestures.
- Actors apply their assigned chair-facing direction on the arrival frame, so the final walking direction cannot leave Aki facing sideways until the next scene refresh.
- Extended the family-home smoke checks for both parents completing WC visits and outings, exclusive occupancy including staff, off-hours continuity, immediate chair-facing on all four rotations, Eat/Write/Sit playback at the selected 1x/2x/4x/8x/32x speeds, and paused animation. These checks passed in the engine and Windows export. Source syntax parsing also passed; alpha.10 includes this follow-up.

Source changes after alpha.8, 2026-09-25.

- Parents' meals and the creator/Helper's decorative breaks take separate turns. Eight visual seconds separate their visits, including return walks. A due creator break waits for a parent's current visit to finish, and parents avoid starting just before the next break. Scheduled activity retains priority. No production or simulation timing is changed.
- The old private door is removed at home. Parents emerge on a covered upstairs landing, walk down ten physical treads at the rear end of the corridor, and return upstairs after their visits. The staircase is separated from the front entrance, with a dedicated rear opening. Non-home office doors retain their existing routes.
- The genkan has a tiled floor 0.14 metres below the interior, a wooden step, shoe cabinet, shoes, indoor slippers and umbrella stand. Staff and Helper routes cross the floor-height change and avoid the cabinet; parents also use the same corridor routing.
- Neighboring homes now use facing rows on connected narrow lanes. Approximate 8.2-by-8.7-metre lots replace the old 23-metre house spacing. Modest two-storey homes, small front aprons, low boundary walls, mailboxes, air-conditioners, small plants and utility poles replace large empty gardens. Repeated boxes are batched by material colour to limit rendering overhead. Existing camera limits and the world ground bounds remain unchanged.

## References and interpretation

[Nakano Ward's district-plan lot-size page](https://www.city.tokyo-nakano.lg.jp/machizukuri/machizukuri/chikukeikaku/chikushisetsudoro/shikichimenseki.html) describes a 60-square-metre minimum in the specified district-plan areas. This supports a compact-lot art reference; it is not an average lot size, a universal Tokyo rule or a claim about every 1996 neighborhood. The game uses stylized approximately 71-square-metre surrounding plots and varied house silhouettes.

[Web Japan's explanation of the genkan](https://web-japan.org/kidsweb/explore/housing/q1.html) describes the lower entrance floor and changing from shoes to slippers. The exact game step height and furniture positions are artistic choices, not building-code advice.

## Verification boundary

Update: alpha.9 was subsequently compiled, visually reviewed and packaged at the user's request. See [executed build verification](alpha-9-build-verification.md). The paragraph below records the earlier source-only handoff.

Source syntax and installed Godot API references checked; no new compiler, Godot launch or packaged build in this turn. Alpha.8 remains unchanged. The updated atmosphere/parent-meal checks cover staggering, both parents receiving meals, stair traversal, upstairs retirement, pause and simulation-state preservation. They await an authorized engine run, along with visual checks of stair occlusion, genkan floor clearance and dense-neighborhood framing/performance.

## Source follow-up after alpha.10: WC privacy and south wall

- WC visitors now walk from the doorway to the toilet, sit inside the visible room and walk back through the same doorway. A screen-space blur follows the rendered occupant; characters remain fully clothed. Staff and parents use the same visit sequence, preserve single occupancy, respect pause and selected speed, and wait for the door to open before leaving. Closing time finishes the trip before returning upstairs or leaving work.
- Helper-Chan stays at her own desk during the protagonist's WC visit. Regular scene refreshes retain the occupant's WC activity and seated facing.
- The office's south partition now uses the actual walking entrance rather than leaving unintended corner gaps. A continuous exterior cutaway wall encloses the front walkway, Helper's alcove, break room and lowered genkan. Wall scaling stays anchored to the appropriate floor height; the walking route and east entry remain open.

C# source parsing and whitespace review passed. Family-home checks were updated to cover visible seated occupants, privacy, Helper's desk, pause, refresh continuity and WC-door exits, including an occupied-WC capture on the next authorized run. Compilation, shader rendering and in-game visual checks remain pending; the existing alpha.10 package is unchanged.

## Source follow-up after alpha.10: foreground labels

Room and doorway labels, including Upstairs and Break Room, use a shared camera-facing style that ignores scene depth and renders above wall, furniture and trail materials. A dark outline keeps text readable over either bright or dark surfaces. Matching nametags identify the chosen protagonist name, every employed character, Helper-Chan, Mom and Dad. Generic office passers-by receive no nametag. Tags follow their actor's visibility on entry/exit and are recreated after appearance changes. Nametags sit above the characters, with attention markers higher to avoid overlap.

Installed Godot label properties were checked in the local API reference. Visual review across camera angles remains pending the next authorized compilation and run.

## Source follow-up after alpha.10: sharper names

The shared labels now project their world anchors into ordinary 2D UI text, replacing the small 3D glyph textures. Names use 20-pixel text at the default setting; room labels use 18 pixels. Camera zoom and window size no longer shrink the letters. Labels respect the text-size preference, retain a minimum 16-pixel font, align their position to whole pixels and keep a dark outline. They render above the scene and WC blur, while the surrounding game UI remains above the office viewport. Hidden or removed actors lose their tags, and anchors behind the camera or outside the viewport are hidden.

Source syntax and installed Godot API checks only. The next authorized run should visually check near/far zoom, camera rotation, window resizing, text scaling and character entry/exit; no compilation or fresh screenshots were produced for this update.
