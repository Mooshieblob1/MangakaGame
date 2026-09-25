# Sub-project 4: 3D Office — Considerations

Date: 2026-09-23
Status: decision record supporting the [formal design draft](2026-09-23-3d-office-design.md)
and [implementation plan](../plans/2026-09-23-3d-office.md).
Implementation has not started. Research: [office evidence ledger](2026-09-23-3d-office-research.md).
Roadmap: [Mangaka Studio](2026-09-22-roadmap.md)
Builds on: [Staff and studio design](2026-09-23-staff-studio-design.md)

Confirmed choices are requirements. Recommendations and unanswered questions
remain open until agreed.

## Confirmed choices

- Fixed rooms, movable furniture: each property has a predefined floor plan;
  players arrange desks and amenities within it. Player-built walls and room
  layouts are outside this choice.
- Angled overhead camera with rotation and zoom, supporting furniture
  arrangement and observation of staff activity.
- Stylised visuals with believable proportions: simple 3D characters and
  furniture, with detail in manga tools and studio clutter.
- Use actual 3D models for the office, furniture and simple characters, with
  basic walking, sitting and drawing animations. Use 2D artwork for portraits,
  manga covers and decorative details. This presentation direction does not
  move the full manga viewer or management UI into this milestone.
- Furniture snaps to a grid and rotates in 90-degree steps. Reject placements
  that block doors or make a workstation unreachable.
- Furniture has modest practical benefits: better chairs improve comfort,
  amenities support breaks, and each usable desk provides a workspace within
  the property's capacity. Integrate these benefits with the existing needs
  and facilities model; exact values remain to be designed.
- Walking distance inside a studio adds no productivity or working-time
  penalty. Keep the agreed furniture and facility benefits, but do not deduct
  production time for crossing the room. Short routes remain a visual layout
  preference, not an additional productivity bonus.
- Staff have assigned desks. Automatically allocate an available desk to each
  employee, with manual reassignment so the player can group teams together.
- Initially show the family home's workroom, entrance, shared kitchen and
  bathroom, with private family rooms closed off. Leave room to expand the
  presentation later without committing to a whole-house view now.
- Make the family home visibly inhabited through passive household details
  and ambient residents walking through shared areas independently of studio
  work. These people have no direct player interactions or management; they
  provide background atmosphere rather than additional staff or a life sim.
- Extend this ambient activity to studios renting part of a larger office
  building: other tenants and visitors use visible common areas independently
  of the player's studio. Reflect the property's shared-building context,
  without direct player interaction or management of these background people.
- New studio leases offer an optional, separately priced basic furnishing
  package. Players may buy the package or furnish the space themselves,
  including bringing existing furniture where ownership permits.
- Offer an Auto-arrange preview when furnishing or moving. Players can accept
  the proposal, adjust individual items, or lock placements and arrange the
  remaining furniture around them.
- The local, rule-based layout planner uses predefined property zones and
  example arrangements. Keep doors, corridors and workstation access clear;
  pair desks with chairs, group series teammates, keep shared equipment
  accessible and group break facilities. Compare valid arrangements for
  usable space and short walking routes rather than filling every square.
- Fit existing eligible furniture first and suggest purchases for remaining
  needs. Put furniture that cannot fit into storage with a clear explanation.
  The planner does not require an external AI service.
- Walking and working animations accelerate with the selected game speed,
  rather than simplifying or shortening journeys to keep movement slow.
  At accelerated speeds, give moving people a timelapse/hyperlapse appearance
  through motion blur or brief ghosted trails, like long-exposure footage.
  Keep stationary office geometry and UI sharp; the visual effect must not
  alter simulation outcomes. Rendering technique and intensity need visual
  prototyping before the formal implementation choices are finalised.
- Keep staff information quiet by default: clicking a staff member opens their
  details, while overhead indicators appear only when they need attention.
  Do not display everyone's name and activity continuously.
- View one studio location at a time, with quick switching and alerts that
  jump to the relevant managed location. All studios continue operating in
  the simulation regardless of which location is visible.
- Let the player customise the starting mangaka's appearance. Everyone else,
  including hired staff, receives a distinct generated look rather than a
  player-editable appearance.
- Automatically pause the game while arranging furniture. Preview the layout
  and total cost, then apply or cancel the changes before returning to play.
- Start with a focused furniture catalogue: essential desks, chairs, storage
  and break furniture, with basic and improved options plus a small selection
  of decorations. A large catalogue of cosmetic variants is outside the
  initial scope.
- Purchased furniture arrives immediately when the player applies the layout
  and pays the displayed cost. There is no separate furniture delivery delay;
  existing studio relocation downtime still applies.

## Existing requirements carried forward

- The office is a Godot scene driven by the existing simulation, showing
  characters working, resting and moving between desks and facilities.
- Remain readable at 1x, 2x, 4x and 8x, with pause available.
- Preserve the rent-free parents' house, predetermined property tiers and
  additional studio locations established in Sub-project 3.
- Ground relevant Tokyo buildings, furnishings and services in Japanese
  evidence appropriate to the setting, starting in April 1996. Distinguish
  historical evidence, modern comparisons and gameplay estimates.
- Keep needs limited to the existing model; this milestone does not introduce
  a wider life simulation or minute-by-minute bathroom needs.

## Proposed defaults supplied by the formal design

- Specific appearance options within the agreed character style.
- Furniture prices, storage and resale rules, and ownership handling during
  relocation and career changes, consistent with Sub-project 3.

The formal design proposes concrete defaults for these details, including
catalog prices, modest benefits, appearance components, ownership/storage,
version-3 save import and the rendering approach. They are distinct from the
confirmed choices above. The implementation plan orders the build and required
verification. The subsequent [delivery record](../sub-project-4-completion.md)
records the implementation and evidence separately from these decisions.
