# Sub-project 6 implementation plan

Date: 2026-09-24.
Status: implemented and verified locally on 2026-09-24. See the
[delivery record](../sub-project-6-completion.md) for final evidence and implementation choices.
Design: [management UI and charts](../specs/2026-09-24-management-ui-design.md).
Requirements: [confirmed choices](../specs/2026-09-24-management-ui-considerations.md).

## Starting point

The current application launches `godot/DebugMain.tscn`. Its partial C# classes
build the working management controls; `Office/OfficeView.cs` renders the office
and routes input. `MangakaSim` owns gameplay and version-5 persistence. The tree
also contains substantial uncommitted publishing, studio, office and historical
work. Preserve it and inspect current diffs before implementation.

The previous delivery recorded 422 simulation tests, 758 headless checks and
823 rendered checks. These are historical baseline evidence, not results of this
plan. Establish fresh results and record the final counts at delivery. Follow
current repository guidance and applicable asset skills. Do not require runtime
AI services for bundled artwork or dialogue.

## Delivery checklist

- [x] 1. Audit actions, data and assets; prove the companion desk layout.
- [x] 2. Add report history, narrative state and explicit save migration.
- [x] 3. Implement career save storage, imports, artwork persistence and menus.
- [x] 4. Build the warm editorial shell and migrate all management actions.
- [x] 5. Integrate inbox, Helper-Chan notifications, tutorials and recaps.
- [x] 6. Complete the Helper-Chan model, portrait expressions and equipped desk.
- [x] 7. Deliver the connected story and everyday conversations.
- [x] 8. Deliver charts, rankings and the manga showcase with image imports.
- [x] 9. Verify integrated play, accessibility, performance and save recovery.
- [x] 10. Record completion, limitations and final evidence; update documentation.

## 1. Inventory and visual feasibility

Read existing debug controls and command types. Produce an action-to-screen
inventory covering production overrides, pitching/contracts, staffing, finance,
printing, promotion/conventions, properties/careers, furniture, Industry and
legacy imports. Map every action to an authorized view and an error state.
Identify which histories are recoverable from current saves and which require
new samples. Do not assume latest rankings or lifetime copy totals are time series.

Inspect the supplied Helper-Chan source image and preserve it. Prepare an asset
manifest for her source portrait, expression variants, model/materials, equipment
and the original genre artwork sets. The user's offer to supply expressions
should inform asset work; no extra upload is needed for the reference portrait.
Use image-generation tools for raster generation/edits when appropriate, and
actual mesh/model work for the 3D character. Keep source and runtime files distinct.

Prototype her dedicated desk footprint against the starting 16×16 logical home
and every supported rental/employer floor-plan family. Prove a fully staffed,
furnished layout retains all ordinary workspaces and door routes. Work out the
companion alcove, visual bounds and camera framing before migrating layouts.
Do not silently charge for floor space, rearrange saved furniture or add a usable
worker slot. Specify the visual fallback when the protagonist has no open office;
do not create an economic location as a side effect of displaying the companion.

Gate: the inventory is complete and companion placement has a feasible, reviewed
layout for home, shared office and largest studio. Save changes must not be used
to conceal an unresolved desk/capacity conflict.

## 2. Engine-free reporting and narrative state

Add typed, validated report samples for physical/channel sales and per-issue
rankings. Capture at the actual settlement/issue boundary, once only, with stable
business, series, edition and magazine identities. Bound UI projections and
aggregation work without discarding financial/ownership evidence. Build pure
query projections for cards, finance, charts and permitted rival information.

Add an authored dialogue catalog and career narrative state: scene revision,
eligibility, completion/skip status, selected answer IDs, remembered flags,
offer queue, cooldowns and independent RNG. Use typed facts/events for career
changes and publishing milestones rather than parsing display strings. Add
commands for accepting, deferring, skipping and responding to scenes with
validation of scene state and available answers. Choices cannot mutate payroll,
needs, production, finances or Helper-Chan's permanent availability.

Introduce required version-6 fields and an explicit validated v5 conversion.
Compose existing v3/v4 imports through v5; preserve original files and create a
replay checkpoint. Do not reset existing gameplay RNG or pending offers. Mark
historical sample availability and initialize old careers without retroactive
story spam. Ensure opening a UI query or rendering a portrait cannot advance RNG.

Checks: batched/hourly equivalence, duplicate settlement prevention, stable IDs,
missing/corrupt state, scene branch prerequisites, independent story randomness,
skipping/deferred scenes, career transitions and post-import replay/continuation.
Compare gameplay outcomes with narrative responses varied to prove no business
effect. Verify known historical data is preserved and unknown history is absent.

## 3. Career storage and main menu

Extract save operations from the debug screen into a testable storage service.
Use career IDs and snapshot IDs independent of display names. Implement the
package manifest, simulation/presentation snapshots and content-addressed artwork
storage. Save through a temporary generation, validate it, then publish it as one
complete snapshot. Never overwrite a good manual slot before its replacement is
durable. Keep old asset references reachable by older snapshots.

Build Continue/New Career/Load/Settings/Quit and a readable save browser with
career, date, protagonist and studio context. New Career exposes supported options
only. Continue handles missing or damaged latest saves by offering a valid prior
snapshot, without silently discarding the current session. Imports use the existing
explicit version boundaries and leave sources untouched. Package export/import
must include custom art and reject paths outside the package's own storage.

Implement three rotating daily autosaves as a proposed default. Coalesce saves
across idle skip and wait for a settled tick and no uncommitted editing/dialogue
transaction. Manual saves preserve pending decisions. Restore read state, story
progress and camera preferences consistently; load paused. A failed save produces
an actionable notice without claiming success or destroying a prior snapshot.

Checks: multiple careers, slot naming, empty/invalid packages, interrupted writes
at each publish stage, rotation, full disk/write failure, old-save imports, moved
exports, missing artwork fallback, and deletion/replacement of original image
files after import. Perform failure injection in temporary test directories.

## 4. Shell and management migration

Create a new player-facing scene and reusable theme, navigation, card, inspector,
dialog, table and chart controls. Keep the debug scene as an explicit development
route. Extract a shared session/clock controller so the new shell and debug
entry point cannot diverge in tick, pause, idle-skip or event handling.

Implement the persistent bar and one-panel navigation stack. Retain selection,
scroll position and explicit filters on Back. Expanded reports restore their
prior panel on close. Office switching changes the scene without silently changing
business scope. Selecting a staff member in 3D opens the same inspector used by
Staff; Helper-Chan opens her guide/journal rather than employee management.

Migrate all inventoried actions using existing commands and quotes. Add action
availability explanations, validation feedback and retained form drafts. Preserve
the furniture editor's transactional Apply/Discard semantics. Test employed and
owner modes, branches, protagonist moves, former titles and expired offers.
Do not expose raw IDs, hidden rolls or technical schema labels in normal play.

## 5. Inbox and assistant presentation

Build a stable event-to-inbox projection with urgency, category, target and
resolution status. Distinguish unread, acknowledged and still-actionable items.
Queue one popup, coalesce repeated causes and prioritize urgent decisions over
optional tutorials/story scenes. Opening old messages must revalidate actions.

Use Helper-Chan's portrait with concise text and relevant action buttons. Include
the first-use tips and Help index, with saved seen/disabled state independent of
gameplay alert preferences. Restyle daily recaps without changing when they end
the day, pause or continue to the next scheduled arrival. Handle simultaneous
recaps, decisions and story offers without resuming against another pause reason.

Checks: 1x/8x and idle skip, final-day retention warning, multiple simultaneous
messages, save/load while open, read versus resolve, disabled tutorials, keyboard
focus, stale links and preserved furniture edits. Routine news must stay quiet.

## 6. Helper-Chan art and office integration

Integrate the supplied portrait and consistent expressions. Create a dedicated
3D model matching the reference's face, clothes, proportions and distinctive huge
curling twintails. Provide idle, seated/clipboard and simple movement poses with
controlled hair motion; keep sufficient volume in the office camera silhouette.
Do not treat a generic procedural employee as the finished character.

Implement the proven companion desk/alcove layouts and equipment. Bind presence
to the protagonist's workplace; do not instantiate copies across branches or
consume a Person ID, payroll entry or production desk assignment. Define the
non-economic setup explicitly in the renderer/layout adapters so inventory,
Auto-arrange, resale and hire-capacity queries cannot treat it as business stock.
Verify closure, relocation, careers, camera reset, saves and full staff occupancy.

Gate: inspect reference/model comparisons, neutral and expressive portraits, and
home/shared/large-office captures. Her hair must remain recognizable at 1x and
8x. Re-run ambient door exit/entry and hyperlapse checks with her present. Profile
the extra model and trails on the same local hardware used for the baseline.

## 7. Story content and remembered dialogue

Implement the proposed six-scene First Reader arc and four everyday scenes from
the design, adjusting prose without changing confirmed character requirements.
Every selectable answer must either have a later visible callback or be clearly
local flavor; never advertise remembered consequences that are not implemented.
Add conditional lines for independent, employed, successful and setback careers.
Keep fallback triggers so publishing or changing jobs is not mandatory.

Use the proposed daily, weekly and cooldown limits initially. Offer optional
scenes in the inbox/journal; **Read now** owns its pause while the conversation is
open. Defer and Skip are real actions, with neutral branch continuations. The
opening for imported careers acknowledges current progress instead of pretending
the career just started. Completed scenes remain readable with recorded choices.

Checks: every branch, each everyday callback, both ownership modes, no release
for months, repeated career moves, return home, imported late-career save, disabled
story prompts, urgent interruption, save/reload at every choice, and once-only
completion. Confirm no money, needs, performance or loyalty side effects.

## 8. Charts, rankings and showcase

Implement readable fixed-period charts and matching accessible tables. Show
scope, units, observed-versus-missing periods, incomplete current periods and
linked records. Reconcile cash, publishing income, salary, loans and transfers
against account entries. Keep physical units separate from channel receipts and
preserve old-edition owners. Mark rank 1 as best; do not connect absence/hiatus
as a fictitious last-place result. Bound drawing work for forty-year saves.

Produce the original bundled art sets, stable genre selection, editable synopsis,
title typography and representative panel viewer. Label panels as a showcase,
not a reconstruction of every simulated chapter. Support per-slot custom import,
fit/crop preview and reset. Validate decoded images, copy into managed career
storage and preserve all art references across saves/export. Imports do not use
gameplay RNG or alter series outcomes. Test missing and corrupt files gracefully.

Gate: render a populated early doujin career and a later multi-series career;
inspect data, covers, panel scaling, long titles and financial totals. No unknown
history is drawn as measured data, and no future historical cast is revealed.

## 9. Integrated validation

Run warning-as-error builds and all existing domain tests, plus new focused
tests for the introduced state and boundaries. Godot checks must exercise the
new entry point as well as retained office behavior; passing the old debug scene
alone is insufficient. Run editor import, headless smoke and a rendered walkthrough.

Test at 1280×720, the 1600×900 baseline and a larger window, with raised text scale
and keyboard-only navigation. Inspect long names, large yen totals, empty careers,
many teams, dense inboxes and reports with unavailable history. Check focus does
not trigger time shortcuts while entering titles or naming saves.

Use controlled date fixtures for historical unlocks and run representative long
careers to measure save growth, chart queries and narrative scheduling. Compare
long-run business results with optional narrative content ignored versus answered.
Record frame-time distributions and asset memory on the actual tested machine;
do not equate compile success with reference fidelity or universal performance.

## 10. Delivery

Create `docs/superpowers/sub-project-6-completion.md` with implemented scope,
asset inventory, final defaults, migration behavior, exact test results, rendered
captures, performance and remaining limits. Update README run/save instructions,
roadmap and this checklist to the state actually reached. Separate approved
requirements from proposed defaults that changed during implementation.

Do not mark this milestone complete with missing expressions, a placeholder
Helper-Chan model, absent story branches, unimplemented existing commands or
untested career saves. No commit, push or external publication is included unless
the user requests it.
