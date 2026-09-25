# Sub-project 4: 3D Office

Date: 2026-09-23
Status: approved direction implemented on 2026-09-24. See the
[delivery record](../sub-project-4-completion.md) for actual structure, validation and limits.
Approved direction: [decision record](2026-09-23-3d-office-considerations.md).
Builds on: [Staff and Studio](2026-09-23-staff-studio-design.md).
Research: [office evidence ledger](2026-09-23-3d-office-research.md).
Execution: [implementation plan](../plans/2026-09-23-3d-office.md).

The decision record contains user-confirmed requirements. Concrete catalog
values, technical structures and defaults below are proposals to implement
that direction, not additional choices the user has already approved one by
one. All prices, dimensions and numeric visual settings are game estimates.

## 1. Outcome and scope

The player can watch their manga studio operate in a furnished 3D space,
select staff, arrange furniture and move between locations. The starting
workroom belongs to a visibly inhabited family home. A rented office can
share its building with unrelated people. Their presence adds atmosphere
without creating another group for the player to manage.

The office follows the existing simulation. Production, needs, deadlines,
cash, conventions, creator continuity and background businesses remain in
MangakaSim. Showing a room, hiding it, moving the camera or changing render
quality must not change those outcomes. Physical furniture introduces usable
workspaces and modest comfort/facility effects, but walking adds no work-time
penalty. This distinction applies equally to visible and hidden locations.

Included:

- Fixed property floor plans, grid furniture editing, assigned desks and
  local Auto-arrange with preview and locked placements.
- Optional furnishing packages, business-owned inventory, storage, resale,
  relocation and employer permissions.
- Overhead rotating/zooming camera, staff selection, attention indicators,
  location switching and persisted appearance choices.
- Stylised actual 3D rooms, furniture and characters; basic walking, sitting,
  drawing and break animations; supporting 2D portraits and decorative art.
- Accelerated movement with a character-only long-exposure timelapse effect.
- Ambient family members and shared-building occupants.
- Save/load, deterministic commands, compatibility handling, rendered checks
  and integration with the current management/debug screens.

Outside this milestone: editable walls, whole-house life simulation, family
relationships, direct interaction with background people, detailed plumbing,
new bathroom needs, a walkable 3D Tokyo, full building exploration, furniture
wear/repair/delivery systems, a large decorative catalog, new digital manga
progression, and the full management UI/manga viewer owned by Sub-project 6.

## 2. Current integration constraints

The current application is `DebugMain.tscn` with a code-built Control UI and
Production, Publishing, Staff and studio, Studio management and Tokyo map tabs.
`DebugMain._Process` advances whole hours at 2.5 real seconds/hour at 1x;
`AdvanceAndScan` stops on the exact tick of an auto-pause. Preserve this driver.

`StudioLocation.Seats` is currently a property ceiling, `BreakSeats` defaults
to two, and `Storage` is physical book capacity. The existing BreakRoom action
buys two extra break seats for ¥10,000. `Person.CurrentTask` is a planner
assignment; it alone cannot distinguish actual work, mentoring, a break,
travel or a convention hour. Do not animate every queued task as completed work.

Property moves currently relocate people, series and stock, charge moving
costs and set one day of downtime. Career moves preserve the old business's
assets and follow the protagonist to a different business. Extend these paths
to furniture rather than building a parallel move/accounting system.

The code's sixteen `TokyoProperties.All` offers are authoritative for current
districts, rents and capacities. Earlier proposed district lists in the
Sub-project 3 design differ from the delivered catalog; this milestone must
not silently rewrite them. No current source contains authored office scenes
or a furniture inventory.

## 3. Properties and the camera

### 3.1 Fixed floor plans

Give each paid offer an explicit floor-plan ID and context type. All sixteen
offers get a predetermined variation; reusable wall/door/window pieces keep
the asset count manageable. Persist the chosen offer/template identity on
the location instead of regenerating it from mutable labels.

| Property family | Existing maximum desks | Proposed usable-area band | Context |
|---|---:|---:|---|
| Parents' home | 2 | 12–16 m² workroom, additional visible shared areas | Family home |
| Small, four offers | 4 | 25–35 m² | Converted residential unit or small office suite |
| Growing, four offers | 8 | 45–65 m² | Office suite with neighbouring tenants |
| Established, four offers | 16 | 90–120 m² | Larger suite with shared building circulation |
| Large, four offers | 32 | 160–220 m² | Large suite/leased floor, not automatically a whole building |
| Employer team | 2/4/8 | Sized for the offered team capacity | Defined team area within a shared office |

Areas are authoring targets carried forward from the earlier design, not
surveyed property data. Each final template must actually fit its advertised
desks, basic break capacity, stock zone and valid circulation. Common building
areas do not consume the tenant's advertised floor area or add rentable desks.
Do not turn the family's shared kitchen into extra manga workstations.

Template data includes editable tenant cells, fixed walls, windows, door
openings and swing areas, entrance anchors, shared/private zones, fixed
facilities, service anchors, protected stock zones and example arrangements.
Stairs/lifts/closed doors are scene boundaries, not simulated extra floors.

The same family-house template appears in whichever outer ward is seeded.
Private family rooms stay closed. Give the physical home a stable site key
separate from business-specific location records so returning home reuses the
house and family furnishings rather than duplicating them.

### 3.2 Camera defaults

Use an orthographic camera tilted approximately 45 degrees, with continuous
horizontal rotation, optional 90-degree rotation buttons, pan, wheel zoom and
a reset-to-room control. Orthographic projection is an implementation default,
not an extra restriction on the approved angled view.

Hide roofs and lower or fade foreground walls based on camera direction;
retain doorframes and room outlines. Shared spaces remain visually distinct
from editable tenant space. Show the grid only while furnishing.

Keep a local camera bookmark per location. Switching views does not move the
protagonist or staff between workplaces. Remember selection by stable person
ID, and fall back to the protagonist's current location if a saved selection
is closed or outside the player's authority.

## 4. Furniture and usable capacity

### 4.1 Initial catalog

These are proposed **G** balance values, including fictional tax/fees in the
displayed total. Names are generic; no real brand model is implied.

| Item | Proposed purchase | Footprint / role | Effect |
|---|---:|---|---|
| Basic drawing desk | ¥8,000 | 1.25 × 0.75 m | One workstation when paired with a chair |
| Better drawing desk | ¥16,000 | Same footprint | Improved appearance/storage detail; no skill bonus |
| Basic work chair | ¥2,000 | 0.75 × 0.75 m seated zone | Completes a workstation |
| Supportive work chair | ¥8,000 | Same zone | Modestly reduces comfort loss while working |
| Two-seat break set | ¥10,000 | 1.5 × 1.5 m reserved module | Two usable break seats |
| Comfortable two-seat break set | ¥18,000 | Same module | Two seats with modest extra comfort recovery |
| Tea/food cabinet | ¥6,000 | 0.75 × 0.5 m plus access | Visual service point for existing provision routine |
| Book/reference shelf | ¥4,000 | 1 × 0.5 m plus access | Cosmetic organisation, not extra saleable-book capacity |
| Plant | ¥1,000 | 0.5 × 0.5 m | Small capped atmosphere contribution |
| Wall print or calendar | ¥1,000 | Approved wall slot | Small capped atmosphere contribution |

Paper, rulers, pens, tone sheets and desk lamps are workstation dressing,
not separate compulsory purchases. Fixed kitchen/bathroom fittings belong
to the property. The stock zone visually represents the existing `Storage`
value; decorative shelving never silently increases printing capacity.

Default basic package: one desk/chair pair per advertised workstation plus
one tea cabinet. Every paid property has two fixed, non-saleable basic break
places supplied with the tenancy, representing the existing baseline. The
package therefore costs `¥10,000 * Seats + ¥6,000`. It is optional and shown
separately from deposit, first rent, moving costs and any additional purchases.
For a move, Auto-arrange uses owned items first and can suggest only missing
items instead of buying a duplicate full package.

The starting home includes two family-owned desk/chair pairs and shared basic
break places at no cost. They have no resale value and remain with the home.
The family's kitchen supplies a fixed service anchor; it does not provide
free daily provisions. Rent remains exactly zero.

### 4.2 Capacity and assignment rules

Keep `Seats` as the property's hard ceiling. Derive `UsableWorkspaces` from
reachable, valid desk/chair pairs within the editable work area. A desk without
a paired chair is decoration until completed, not a hireable seat.

Reserve an available workstation when a hire is accepted, including staff
whose employment begins later. Automatically assign in stable person/item-ID
order. Manual reassignment can swap two occupied desks atomically. A person
has at most one assigned workplace desk; a desk has at most one person.

Count active employees and accepted future hires against capacity. An edit
cannot remove their last usable workspace: propose a reassignment or explain
the shortfall. Empty unstaffed branches may remain unfurnished. A move/transfer
must have a complete valid destination layout for its incoming team. Staff
leaving, transferring or being dismissed release reservations at the same
time their workplace assignment ends.

Production eligibility, hiring, employee transfers, career follower previews,
former-business hiring and forecast capacity must consult the same workspace
query. No debug or AI command may bypass furniture-backed capacity.

### 4.3 Small benefits, one accounting path

- A supportive chair reduces the work-step comfort loss from 3 to 2.5 for
  hours actually spent at that chair. Other need drains remain unchanged.
- A comfortable break place adds 3 comfort points to the existing provisioned
  break recovery, clamped to the existing maximum of 100.
- Two distinct placed decoration categories add at most +2 atmosphere total;
  repeats do not stack. Preserve the property's base atmosphere separately.
- Basic furniture recreates the existing baseline. No extra skill, quality,
  walking-distance or desk-position multiplier is introduced.

Derive effective break seats from fixed service capacity plus placed break
sets, capped at the property desk ceiling for new purchases. Fixed access
permissions prevent background tenants from consuming player break slots.
The existing BreakRoom purchase becomes an adapter to an explicit ¥10,000
two-seat set and layout validation. Remove the independent numeric increment;
disable that purchase with an explanation if no legal placement exists.

Where break places differ in comfort, reserve them deterministically using
the existing wellbeing queue order, then place ID. A disabled/offscreen view
uses identical assignments and effects. Preserve the existing fallback for
staff without paid provisions; ambient household food is not free stock.

## 5. Placement and Auto-arrange

### 5.1 Placement validity

Proposed logical grid: 0.25 m cells, independent of rendering meshes. Furniture
rotates only in 90-degree increments. Grid size and clearances are game
authoring defaults, not claims about Japanese construction standards.

Validate the entire proposed layout, not just the dragged object's origin:

- Footprints stay inside permitted zones and do not overlap solid furniture,
  walls, fixtures, protected stock cells or door-swing reservations.
- Paired chair zones are reserved behind desks and access anchors remain
  reachable from the tenant entrance.
- Use a common clearance-aware navigation mask for editing and visible walking.
  Start with 0.75 m usable passage clearance and 1 m protected main aisles.
- Every used workstation, break seat and required service/exit anchor must
  remain connected. Reject corner cutting and routes through private rooms.
- Wall decorations use valid slots and do not obstruct doors/windows.

Temporary drag previews can be invalid and show the exact obstruction. Applying
must pass all constraints. It must never strand a worker behind furniture.
Current animated positions are not permanent placement blockers; on apply,
rebind actors to valid anchors without charging time or moving simulation staff.

### 5.2 Search strategy

The planner is local, deterministic and bounded. It does not call an AI service
and does not promise the globally optimal layout.

1. Start from the floor-plan template, existing inventory, team assignments,
   fixed property assets and player-locked items. Validate locks first.
2. Reserve doors, main circulation and service/stock areas. Try authored
   arrangements and alternate desk-bank orientations appropriate to the room.
3. Place workstations for current staff and accepted hires first, preserving
   their desk assignments where possible. Group series teammates, then place
   spare workstations, break sets and optional decorative items.
4. Score feasible layouts lexicographically: required workspaces and services,
   use of existing furniture, preserved locks/assignments, team grouping,
   accessible shared facilities, then short routes and visual spacing.
5. Bound the search by a deterministic candidate count (initial target: 2,000
   complete/partial candidates), with stable item/coordinate tie breaks. A UI
   cancellation or elapsed-time limit must not apply a partial layout.

Required workstation/facility fit failures block apply. Optional surplus goes
to business storage with an itemised explanation. Invalid locks are reported;
the planner never silently moves them. An author-validated baseline supplies
a fallback when no locks prevent it. Extra purchases are suggestions in the
preview, never an automatic debit.

### 5.3 Editing transaction

Entering furnishing mode pauses the global clock and captures the previous
speed. Camera movement remains available. The draft is separate from live
inventory, balances, assignments and command history.

The preview lists purchases, sales, stored/transferred items, resulting usable
desks, break capacity and total cost. Apply validates the entire batch and
commits it as one command. Cancel changes nothing. Restore the prior speed
on exit only if no recap/other modal is holding a pause; previously paused
games stay paused. Disable conflicting management commands while editing.

Use a revision token to reject stale previews, including after loading a save,
changing location or changing the controlled business. Camera-only changes
must not invalidate a financial quote. Saving stores committed state only;
leaving the screen with edits requires Apply or Discard, not a hidden commit.

## 6. Ownership, cash and moving

Furniture instances have explicit owner kinds: business, family/site, or
landlord/site. Only business-owned movable assets can be moved between that
business's studios, stored or sold. Fixed assets and common-area decoration
are never sold. The family workroom's movable furniture can be rearranged
inside its permitted area but cannot leave the family home.

New purchases use business funds through the existing reserved-cash and
employer-budget checks. No automatic personal contribution, borrowing or
payment from a staff member occurs. Charge once at Apply; delivery is immediate.
Resale returns 50% of the item's recorded acquisition price, rounded down in
yen. Free/inherited starter assets have zero acquisition price and zero resale.
Selling an item removes it permanently; an item cannot be sold twice or sold
and moved in one batch. Record gross purchases and sale receipts separately.

Sales may fund purchases in the same validated batch, but final business cash
must retain wage/bill reserves. Employer discretionary spending counts gross
purchases; selling furniture cannot reset that budget or refund prior spending.
In employed-lead mode, the player may rearrange their allocated team area and
buy permitted upgrades within budget. Employer-provided baseline assets are
not saleable or movable out of that area. The wider office is ambient scenery.

Business storage is a simple offsite furniture inventory in this milestone,
with no rental charge, physical-book capacity consumption or wear. Explain
this game abstraction in the furniture screen. Storage creates no cash,
workspace or facility benefit. It does not erase book storage restrictions.

For an ordinary studio relocation:

1. Preview eligible existing furniture in the destination using Auto-arrange.
2. Offer missing furniture purchases and optional extras, preserving capacity
   for incoming staff. Include the existing lease and moving charges.
3. Validate and apply the move, furniture batch, employment/series/stock updates
   and cash entries atomically. A failure changes none of them.
4. Keep one day of existing relocation downtime. Storage overflow is furniture
   only; insufficient physical-book storage still blocks the move.

An empty additional branch can be leased without a package. Transferring staff
there is blocked until its workspaces exist. A closure stores business-owned
movable furniture and leaves fixed/family assets at their site. Do not silently
dismiss staff or grant a refund for their desks.

A creator changing employers or founding a different business takes no assets
from the former business. Old inventory and cash remain there, regardless of
title-retention mode. A newly founded rented studio must fund its incoming
team's desks from its new business balance. The career preview includes that
cost and capacity; revalidate at the scheduled midnight transition. Employers
provide their offered baseline workspaces. Returning home restores access to
the one existing set of family furnishings, without minting resaleable copies.

Background businesses retain their layouts and inventory. Any replacement
hire needs an available desk; adding a desk uses that business's own cash and
the existing conservative hiring reserve. Former-business automation may
choose a valid basic arrangement but may not acquire free assets or expand
beyond the existing property rules.

## 7. Characters, activities and ambient life

### 7.1 Appearance

Use a small modular 3D character kit with believable proportions, simplified
faces and distinguishable silhouettes. Initial customisation: body/build
preset, skin tone, hairstyle/colour, outfit palette and glasses. All combinations
are cosmetic; they cannot affect talent, salary, gendered work restrictions
or recruitment outcomes. The starting mangaka is customised at new game;
generated staff keep their looks when hired, moved or reloaded.

Generate appearances from an independent stable visual seed and save component
IDs. Appearance rerolls must not advance production, hiring, event or location
RNGs. Candidate looks carry into employment under their identity. Later famous
analogue arrivals can supply an authored appearance without using real likeness
assets. Aging, wardrobe economies and an editor for every employee are deferred.

Provide a matching small 2D portrait assembled from the same appearance recipe;
do not generate an unrelated face each time the UI opens. Use original small
2D wall/cover decoration appropriate to manga work. A full series-cover creator
and manga viewer remain later work. No runtime generative service is required.

### 7.2 Simulation-to-scene activity contract

Add a read-only, engine-free activity snapshot per person for the last resolved
hour: timestamp, person/location ID, activity reason, optional task/stage ID and
reserved desk/facility/partner ID. Record reasons at the existing wellbeing,
convention, spare-hours and work steps where the decision actually happens.
Do not infer all busy states from `BusyUntil`, which currently conflates them.

Activities cover work, break/wait, mentoring, promotion, recovery commission,
offsite convention/travel, relocation, off duty and idle. Scene-only walking
is a transition between activity anchors, not an extra simulation activity
that consumes a production hour. A blocked renderer route emits a diagnostic
and uses a valid anchor; it must not stop wages, deadlines or production.

Store the last bounded activity snapshot with the save to reconstruct a view
after load. It is observational, not a second planner; no unbounded hourly
animation history is needed. Validate references/times, including historical
locations immediately after a midnight career transition. During fast catch-up
or idle skip, show the latest relevant snapshot rather than replaying days of
stale walking. Labels show authoritative activity and identify offsite staff.

Use basic walk/idle/sit/draw/reach/eat-drink loops. Stage-specific drawing props
and poses can distinguish Name, pencils, inks and tones without five separate
complex rigs. Mentoring visibly pairs staff; online promotion need not imply
that the whole studio has converted to digital drawing.

### 7.3 Timelapse

At 2x/4x/8x, movement and working loops advance at 2/4/8 times their 1x rate.
Do not impose a slow-animation cap or shorten ordinary paths at high speed.
Calibrate the 1x visual pace against the existing 2.5-second hour in the first
rendered prototype, so ordinary journeys resolve promptly even in a large
office. This is an illustrative game pace, not a claim of literal walking speed.

Default visual proposal: no trail at 1x, then a subtle/medium/strong trail at
2x/4x/8x. Start with short-lived, depth-tested ghost silhouettes sampled along
the moving character's path/pose. Keep the current body readable. Do not blur
the camera, furniture, text, selection markers or the whole frame.

Prototype the effect before committing to a shader design. Test motion of
working hands as well as walking bodies. Trails must be occluded by walls and
must not cast shadows, intercept clicks, trigger animation sounds repeatedly
or become a queue of full character nodes. Clear trails on pause, teleport,
load, view changes and time skips. A stopped character must not leave a smear.
Include Off/Reduced/Full intensity controls; these affect rendering only.

Transparency/pose sampling cost is a known implementation risk. Begin with
pooled ghosts, a small per-character history and bounded visible population;
measure before considering a more complex velocity-buffer effect. A screenshot
alone cannot verify the selected timelapse behavior: capture a short sequence
or perform a recorded rendered walkthrough at every speed.

Implementation update, 24 September 2026: the initial four-ghost treatment
looked like fast forward in user review. Full now integrates closely overlapping
path samples with soft edges and a translucent moving body, producing a visible
long-exposure streak. It retains the readable stationary body, bounded pooling,
scene occlusion and simulation independence above. See the delivery record for
current shutter settings, sampling limits and measured performance.

### 7.4 Ambient residents and other tenants

Ambient actors are a separate presentation population, never `Person` staff
records. Suggested initial density: two recurring family residents, and up
to six background occupants at once in shared commercial circulation.
These numbers are presentation defaults, not demographic claims.

Give them modest time-of-day patterns: leaving/returning, passing through a
hall, pausing at a common kitchen or disappearing through a neighbour's door.
Use private-room/entrance anchors rather than spawning in the middle of the
workroom. Their routines use an independent visual seed and game time; view
switches reconstruct the current scene without consuming gameplay randomness.

They have no selectable management panel, dialogue, salary, needs, staff slots,
sales, break reservations or collision authority over workers. They stay in
their allowed zones; commercial neighbours do not wander through the tenant's
private work area. Household movement can use designated shared routes without
interrupting work. Apply the same accelerated-time visual language to them.

## 8. UI integration and assets

Add an Office tab to the current harness, sharing its global clock and account
state. Use a location selector, focus-protagonist button, Furnish/Auto-arrange
controls, usable desk count and compact selected-person/furniture panel.
No always-on nameplates: selected/hovered staff show details; attention markers
appear for actionable existing conditions and open the related management view.

Existing management tabs remain available. Location switching and alerts are
limited to the controlled business; a career move follows the protagonist and
removes former-business control. Do not make visual visiting a backdoor into
managing the old studio. Non-visible locations continue through normal ticks.

Author reusable local Godot scenes/meshes and materials. Simple procedural
geometry is acceptable when it produces intentional silhouettes and usable
scenes, rather than leaving all furniture as labelled placeholder boxes.
Separate collision/access footprints from art geometry. Repeated props may be
batched, but moving furniture and selectable actors retain stable identities.

Use subdued materials, readable hands/paper and restrained domestic clutter.
Fixed scenes communicate home versus commercial office through finishes,
lighting, shared-space dressing and signs. Day/night lighting is decorative;
no new weather or seasonal simulation is needed. Audio is optional polish,
not a prerequisite for functional completion.

Keep an asset manifest recording authoring method, source/license for any
external asset, dimensions, pivot, material and collision/access conventions.
Do not ship museum photographs, recognisable manga pages or borrowed brand
textures as decoration. Research evidence O1–O4 informs original art direction.

## 9. Data, commands and persistence

Proposed engine-free models:

| Model | Responsibilities |
|---|---|
| FloorPlanDefinition | Versioned template ID, zones, occupancy/access masks, fixed assets, layout examples |
| FurnitureDefinition | Stable type ID, footprint, chair/desk/service anchors, price, modest benefit, art key |
| FurnitureInstance | Unique ID, type ID, owner/site, acquisition price, placed/stored/sold state |
| OfficeLayout | Location/site, revision, item transforms, locks and workstation assignments |
| OfficeActivitySnapshot | Last resolved-hour activity per person, no visual animation progress |
| AppearanceRecipe | Stable saved components and palette for staff/candidates |
| OfficeViewPreferences | Camera/selected location/effect intensity; separate from authoritative simulation |

Logical coordinates, rotations and path masks live in MangakaSim. Godot reads
them; it is not the only validator. Prefer dedicated typed commands such as
`ApplyOfficeLayoutCommand`, `AssignDeskCommand` and `RelocateStudioCommand`
over expanding the generic StudioAction positional parameters indefinitely.
Register all new commands with the existing polymorphic command log.

Quotes contain a revision, explicit item changes and calculated totals. On
apply, clone incoming collections, recompute prices and validate IDs, ownership,
budgets, staffing, connectivity and stock constraints before committing.
Never trust quoted totals supplied by a caller. Rejected operations leave
cash, IDs, RNGs, placements and command history unchanged.

Use save version 4 for the new required state. Keep `debug-v3.json` intact and
write new saves to `debug-v4.json`. Proposed compatibility scope: an explicit
import of valid, completed Sub-project 3 version-3 saves, not an implicit
rewrite and not support for earlier incomplete version-3 formats.

The importer validates the old schema before conversion, maps paid locations
to the existing catalog by their stored terms, and selects employer/home
templates explicitly. For open workplaces, materialise the previously implicit
workspaces and break places with zero acquisition price, preserving capacities
without a new cash charge or resale windfall. Keep closed locations and their
liabilities; do not invent saleable furniture for empty historical records.
Reconcile all family-home records to one physical family asset set. Stock,
cash, wages, debt, rights and gameplay RNG states must survive unchanged.

Build deterministic layouts in stable ID order and validate them. Preserve
existing legitimate break capacity if it exceeds a new-purchase cap; block
further additions until within the new limit. Unsupported/custom legacy
location terms or unplaceable legacy capacities produce a clear import error
without writing a new file. Do not silently discard fixtures or truncate staff.

Imported games replay from the converted checkpoint; pre-import history is
retained as history, not claimed to reproduce identical version-4 assets when
replayed from the old NewGame constructor. New version-4 games must support
full command replay. Camera changes, visual seeds and ambient actors must not
consume global entity IDs used by gameplay.

## 10. Acceptance and implementation risks

Completion requires all of the following, with evidence in the plan:

1. Every catalog property and employer/home variant has an accessible baseline
   layout at its advertised capacity; the house stays rent-free.
2. Placement and Auto-arrange preserve locks, reject blocked access, assign
   staff once, explain overflow and preview without spending money.
3. Apply/cancel, resale, moving, branch transfers, closure, careers and employer
   permissions preserve ownership, cash reserves and the separate money model.
4. Basic furniture preserves the earlier production baseline; chair/break/decor
   benefits are modest, capped and identical with all office rendering disabled.
5. Staff activities reflect resolved simulation hours; movement and ambient
   residents create no extra productive time, wages or needs.
6. Camera controls, wall cutaways, selection, location switching and quiet
   alerts remain usable at the existing 1600×900 window and at 1280×720.
7. Actual rendered walking/working accelerates at each selected speed, with
   readable timelapse trails, correct occlusion and sharp UI/backgrounds.
8. New saves round-trip/replay; valid supported v3 imports preserve progression;
   corrupt/unsupported saves and stale previews cannot overwrite live state.
9. Real local assets, modular character variation and ambient shared spaces
   are delivered, not only architecture, mock screenshots or empty scenes.
10. Existing domain tests/build and Godot smoke paths pass alongside new office
    coverage; rendered verification is reported separately from headless checks.

Initial performance target, to measure rather than claim in advance: a 32-staff
office plus six ambient occupants at 1600×900 should sustain approximately
60 FPS on the current development machine with Full trails at 8x. Record
hardware, frame-time distribution and settings. Also test reduced effects;
this does not establish a minimum-spec claim for other computers. Keep dormant
locations out of the rendered scene and bound trail memory over long runs.

Highest risks: transparency trails, activity timing at fast speeds, property
capacity versus valid floor plans, migration of implicit facilities, and
atomic furniture/career transactions. Resolve these in early slices, before
spending time on decorative variation. The research ledger explicitly records
the remaining historical price and commercial-interior evidence gaps.
