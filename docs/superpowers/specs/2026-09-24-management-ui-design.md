# Sub-project 6: Management UI and charts

Date: 2026-09-24.
Status: approved and implemented locally on 2026-09-24. See the
[delivery record](../sub-project-6-completion.md) for final implementation choices and verification.
Requirements: [twenty confirmed choices](2026-09-24-management-ui-considerations.md).
Delivery: [implementation plan](../plans/2026-09-24-management-ui.md).
Baseline: [Sub-project 5 completion](../sub-project-5-completion.md).

The confirmed choices govern the design. Exact colors, layout dimensions, asset
counts, scene scripts, scheduling and save-retention numbers below are proposed
implementation defaults, not additional user-confirmed decisions.

## Player experience

The office becomes the home screen. A compact status bar shows date, fixed speed
controls, viewed studio, distinct business and personal balances, and alerts.
One management panel opens beside the office; Back restores its previous view
and filters. Reports can expand, then return without losing context. Opening
ordinary panels does not pause time or reset the camera.

Use a warm editorial theme: cream surfaces, dark ink-like text, muted blue/teal
accents, restrained rules and manga-page details. Decoration must not compete
with numbers or charts. Proposed baseline is the existing 1600×900 viewport,
with responsive behavior down to 1280×720 and scalable text. At large text sizes,
allow the panel to occupy the full content area instead of clipping controls.
Keep readable focus states, text labels beside icons, and symbols/text alongside
color-coded alerts and chart lines. Support keyboard focus, Escape/Back and
existing time controls without stealing shortcuts while typing.

Navigation offers Inbox, Series, Staff, Finances, Studios, Industry and Help.
The office itself remains accessible by closing the panel. Studios contains
locations, the Tokyo map, moves, branches and furniture editing. Production and
publishing move into series details rather than remaining separate giant forms.
The debug harness remains available as a developer entry point.

Default panel scope is the whole business within the player's authority.
Explicit location filters remain visible and independent of the viewed office.
Employed play shows the authorized team and budget; the UI must not expose owner
actions or imply that the employer's whole account is freely spendable. Label
account balance, available funds and team spending limit separately. Career
moves refresh scope and clear invalid selections, while old releases retain
their actual business ownership in historical reports.

## Management workflows

| Area | Default presentation | Details and actions |
| --- | --- | --- |
| Series | Compact cards: title, lead, current progress, deadline, warnings | Chapter stages, queue overrides, schedules, assignments, cadence/pages, publishing offers, contracts, print orders, promotion, distribution and showcase |
| Staff | Groups by assigned series/team, plus unassigned; studio filter | Skills, wellbeing, pay, availability, stage/team assignments, recruitment, retention and departures |
| Finances | Cash, available spending funds, wages, bills and income/spending charts | Business/personal account views, obligations, ledger, credit, contributions and incorporation |
| Studios | Current and other authorized locations | Tokyo map, property quotes, move/branch flows, furniture editor and camera focus |
| Industry | Current rankings, news and discovered opportunities | Magazine comparisons, rival scouting/recruitment, temporary assistants and channel proposals linked to their series |
| Help | Contextual guidance and completed tutorial topics | Revisit tips and Helper-Chan conversation history |

Retain every currently implemented player command through a deliberate route.
Shared staff may appear under more than one relevant assignment but remain one
person; headcount and wage totals must not double-count them. Show why an action
is unavailable. Revalidate quotes on submission; never mutate money or gameplay
state merely by selecting a row, opening a panel or previewing an action.

Keep existing furniture Apply/Discard behavior and pauses. If an urgent event
arrives while editing, preserve the draft and route to a safe decision point.
Do not lose an office layout because the player opened an inbox item.

## Inbox, notifications and tutorials

One inbox orders unresolved urgent matters and decisions first, then milestones
and routine news. Each actionable item links to its current person, series,
offer or workplace. Read state is distinct from resolved state: dismissing a
popup must not accept a contract, decline an offer or clear a real problem.
Expired offers display their outcome instead of leaving a live action button.

Helper-Chan presents decisions, urgent problems and major milestones. Routine
sales, rankings and ordinary historical news stay in the quieter feed. Coalesce
duplicate messages about the same cause; show one popup at a time. Urgent matters
take precedence over tutorials and story scenes. Keep stable event identities
and persisted acknowledgments so loading does not replay dismissed popups.
Existing configurable auto-pause rules remain authoritative. Non-actionable
milestones can announce themselves without silently changing pause preferences.

Daily recaps retain their existing end-of-day and next-arrival semantics, using
the new presentation and links into work that needs attention. Distinguish a
recap continuation from accepting an unrelated pending offer.

Tutorials are short first-use explanations with Skip, disable and revisit
controls. Disabling tutorials does not disable gameplay notifications. Proposed
first topics are time/recaps, creating a series, stage work, pitching, hiring,
cash reservations, printing, studio moves, the furniture editor and Industry.
Only explain features actually available at the current date; do not reveal
future unlocks, assistants or acceptance rolls.

## Helper-Chan's identity and physical presence

She is an implied employee, the protagonist's number-one fan, and always follows
their career. She is practical and capable when explaining work, delighted by
success and supportive through setbacks. Dialogue choices influence remembered
conversations, never whether she stays.

Use the supplied [image](../references/helper-chan.png) directly as the base
portrait and as the model reference. Preserve her female appearance, blonde
twintails, blue eyes, glasses, collared shirt, pencil skirt and clipboard.
Her absurdly long, enormous hair and cascading curls are the defining silhouette.
Keep the reference's proportions and outfit; a recolored generic staff model
does not satisfy this requirement. Use a dedicated 3D character, with hair
geometry and modest secondary motion that preserve its volume at office scale.
Inspect front, side, seated and office-camera views against the reference.

Proposed portrait expressions are neutral, happy, excited, concerned, surprised
and determined. The user offered to supply variants; neutral can support the
initial integration, but expressive portrait delivery remains part of the scope.
Keep consistent framing and preserve the original source image. Review generated
or supplied expressions for identity consistency before treating the set as done.

She arrives with her own equipped desk wherever the protagonist works, usually
beside their desk. No wages, upkeep, productivity effects, business inventory,
sale value, needs management or ordinary headcount are attached to her setup.
She is not a recruitable or dismissible Person and cannot occupy a production
assignment. Her chair, clipboard and equipment belong to the companion scene.

Provide a dedicated companion footprint for every home, rental and employer
floor-plan family, including fully occupied layouts. Prefer a valid footprint
beside the protagonist; if it does not fit, use a predefined companion alcove
next to the working area. The alcove belongs to the same interior, not an
unrelated hallway. It must not add productive staff capacity or change rent.
Do not shrink her hair, steal a paid desk, move the user's furniture or block a
door to fit her in. Extend visual bounds and camera framing where necessary;
keep ordinary placement coordinates and saved furniture valid. Test the actual
footprints before committing to a layout implementation.

Only the protagonist's workplace contains her active setup; visiting a branch
does not teleport her away from the protagonist. A career move relocates her
without a follower roll, fee or duplicated desk. Notifications remain available
while viewing another office. A closed workplace requires a defined visual
fallback to the protagonist's current valid location without creating free rent
or a new business. Preserve existing ambient door transitions and hyperlapse
effects, including clearing trails on scene changes and loads.

## Connected story and everyday scenes

The user chose a connected storyline with dialogue choices and remembered
responses, plus occasional everyday scenes. Proposed initial scope: a six-scene
arc called **The First Reader**, supported by four everyday scenes. This is a
bounded first arc that remains compatible with endless play.

The central thread is her clipboard archive of the protagonist's journey. Each
choice writes an explicit narrative flag, which changes a later line or keepsake.
There are no hidden loyalty scores, production perks or financial rewards.
Story scenes are optional, can be deferred or skipped, and remain accessible
through a journal. Skipping has a neutral continuation and does not pretend the
player selected an answer. No single career route is required to finish the arc.

| Scene / eligibility | Proposed dialogue and choices | Later consequence |
| --- | --- | --- |
| 1. A place beside yours; first introduction | “The desk is ready. So am I. What should I remind you of when things get difficult?” Choose **Make something I'm proud of** or **Reach someone who needs it**. | Save craft/readers emphasis for scenes 3 and 6. |
| 2. The first page worth keeping; first completed chapter after scene 1 | “I kept a copy of the first finished page. Official archival duties, obviously.” Choose **Keep the rough draft too** or **Start with the finished page**. | The archive uses process/presentation emphasis in later scenes. |
| 3. Your first reader; first released book, first accepted serialization, or 90 active career days after scene 2 | “I was here before the reviews. I'd still like to hear what you think of it.” Choose **I'm proud of the work** or **I want to do better next time**. Use a work-in-progress variant for the time fallback. | Record pride/ambition; acknowledge scene 1 without claiming nonexistent sales or release. |
| 4. Same clipboard, different day; a career move or setback after scene 3, otherwise 60 days later | On moving: “Different room. Same place beside you.” On a setback: “The clipboard has room for a difficult chapter, too.” Choose **Talk it through** or **Just stay beside me for a bit**. Quiet-period variant asks about the next chapter. | Remember direct reassurance/quiet company, with matching future responses. |
| 5. An archive of ordinary days; at least 30 days after scene 4 | “It was supposed to be a work log. Somehow, the ordinary days took up most of it.” Choose **Keep the little moments** or **Make room for what comes next**. | Everyday callbacks emphasize memories/future plans. |
| 6. The first reader, still; at least 30 days after scene 5 and a release/move milestone, or 60 days regardless | “I kept the first page. There's plenty of room after it.” Recall the player's craft/readers answer and draft choice. Choose **Let's keep going** or **Thank you for staying**. | Complete the first arc; keep her available and allow everyday scenes to continue. |

“Active career days” means elapsed in-game days in this career, not real-world
play time. Triggers use facts with dates and stable IDs, not message-text parsing.
Previous milestones can satisfy eligibility after import, but old scenes are
not all queued at once. The introduction acknowledges an established career;
subsequent scenes require a fresh scheduling interval.

Everyday scripts proposed for the initial set:

- **Clipboard margin:** “These are notes. The tiny cheering figure is also a
  note.” Ask to see it or let her keep it; a later line recalls the answer.
- **Reference shelf:** “I've organized the references. The favorites were harder
  to put back.” Talk about craft or readers, calling back to the opening answer.
- **Tea beside the draft:** “A pause is allowed. I checked.” Chat or enjoy the
  quiet; this is flavor and does not alter simulated needs or work hours.
- **Desk migration:** after a genuine move, “The important equipment survived.
  Clipboard, glasses, and an unreasonable amount of hair.” Ask about the archive
  or the new view. No relocation fee, move event or company action is generated.

Proposed scheduling: one optional scene offered per in-game day at most; arc
scenes have priority and at least seven days between offers. Everyday scenes
have a 14-day global cooldown, a 90-day same-scene cooldown and a 25% weekly
eligibility check using a dedicated saved narrative RNG. Queue at most one
optional scene; unresolved urgent matters postpone it. Offers remain in the
journal rather than disappearing at a timed response deadline. Selecting **Read
now** pauses for dialogue and restores the prior speed only if no other pause
reason exists. No unsolicited dialogue stack during idle skip or 8x play.

## Reports, history and artwork

Focused income/spending, physical/digital/overseas sales and ranking charts have
month, quarter, year and all-time presets. Show units, period, scope and exact
values on selection. Account transfers and loan proceeds are not publishing
income; debt payments, setup fees and owner contributions need distinct labels.
Show available cash after existing reservations without subtracting liabilities
twice. Do not allocate unrecorded shared expenses to a series to invent profit.

Current code stores ledgers, chapter ranks, channel receipts and latest magazine
rankings, but not every historical competitor rank or weekly physical unit
sample. Reconstruct only facts actually supported by those records. Add compact
weekly sales and per-issue ranking samples for future play, retaining identities
across endings and creator moves. Missing pre-import data is labeled unavailable,
not zero or a smooth estimated curve. Financial charts can reconstruct opening
balances from full ledgers. Mark an incomplete current period explicitly.

The showcase contains a cover, editable short synopsis, representative panels
and factual chapter/volume history. Use a bundled original art library with
stable genre-based selection and title typography. Proposed minimum: two cover
variants and one three-panel set per supported genre, plus a generic fallback.
Art repeats are acceptable and must not masquerade as individually illustrated
chapters. Synopsis templates describe a fictional premise, not simulated plot
events that never occurred. The player can edit that description.

Allow PNG/JPEG imports for the cover and individual panel slots, crop/fit preview,
and reset to bundled content. Proposed limits: 20 MiB input and 4096 pixels per
side after preparation, with decoded-image bounds enforced before allocation.
Copy accepted assets into the career's managed storage using content hashes;
never depend on the original temporary file path. Preserve aspect ratio by
default and ask for a crop in the image editor rather than silently distorting.
Old snapshots keep their referenced assets. Replacing artwork changes no quality,
fanbase, reputation, finances or gameplay RNG. No runtime generation service is
required. Helper-Chan's reference is a separate, explicitly authorized asset.

## Persistence and menus

Main menu: Continue, New Career, Load, Settings and Quit. New Career exposes the
existing supported seed, ownership rule and starting appearance; retain the
1996 start and rent-free parents' home. Unsupported difficulty systems and
alternate start dates remain outside this milestone. Proposed settings include
UI scale, tutorial/story presentation, existing auto-pause options and motion
effect level. Disabling optional stories does not disable urgent notifications.

Use an engine-free career state for narrative choices and report sampling,
separate from purely visual navigation/camera preferences. Proposed simulation
schema is version 6, with explicit validated v5 conversion and existing v3/v4
conversion composed through that boundary. Conversion preserves IDs, financial
history, furniture, gameplay RNG and pending decisions; starts a new replay
checkpoint; and records reporting availability. Do not trigger retrospective
income, art costs or a flood of old notifications.

A career save package contains simulation state, presentation state, a manifest
and referenced imported assets. Presentation state includes read acknowledgments,
tutorial completion, showcase overrides, selected filters and camera preferences.
Keep narrative choices in logged deterministic commands so headless replay can
reproduce them without UI participation. Rendering does not consume narrative
or gameplay randomness.

Proposed save defaults: named manual slots and three rotating daily autosaves
per career. Snapshot only at a completed tick and outside an uncommitted office
edit or partially submitted dialogue choice; coalesce pending autosaves. Use
temporary generation directories and a validated manifest switch so interruption
cannot mix old simulation with new UI/story/assets. Preserve the last good save
on write failure, check asset references before load, and load paused. Continue
selects the most recent valid career snapshot. Missing custom art can fall back
visibly to bundled art; invalid simulation state must not silently start anew.
No automatic deletion of manual saves or source imports. Provide a portable
career export containing all referenced assets, so moving saves works offline.

## Delivery boundary

The implementation must include the new player-facing shell, complete routes for
existing actions, reports, showcase/imports, Helper-Chan's portrait/model/desk,
connected story and everyday scenes, and menus/save management. Placeholder
character art or a shell with missing actions is not completion.

The test plan covers deterministic state and imports, authority, chart accounting,
missing history, safe save interruption, dialogue branches, office footprints,
notification timing, keyboard/text scaling and rendered asset fidelity. Existing
production, historical, office and motion-effect regressions remain required.
Tokyo-specific new facts need dated Japanese sources; this design introduces no
new rent, geography or historical factual claims. Helper-Chan's story and art
are fictional. Wider awards/adaptation systems remain Sub-project 7.
