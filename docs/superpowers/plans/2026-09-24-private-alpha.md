# Sub-project 8 implementation plan

Date: 2026-09-24.
Status: implemented locally; see the [delivery and verification record](../sub-project-8-completion.md).
The original implementation sequence is retained below for traceability.
Design: [private alpha](../specs/2026-09-24-private-alpha-design.md).
Requirements: [15 confirmed decisions](../specs/2026-09-24-private-alpha-considerations.md).

## 1. Inventory and establish the baseline

Read current repository guidance and inspect the working tree, preserving prior
uncommitted work. Confirm the current simulation/career envelope versions,
management entry point, save locations and available Windows export tooling.
Run the relevant existing baseline once; record any pre-existing failures.

Trace production completion, collected volumes, contest manuscript release,
printing, sales and publisher one-shot behavior before adding a format. Inspect
`GameState.Awards.cs`, `GameState.Planner.cs`, `GameState.Sales.cs`, `CareerStore.cs`
and the management UI partials. Reuse their authority and validation boundaries.

Measure representative fresh, established and long-career saves: serialized and
stored size, save/list/load latency and main-thread stalls. Use the same machine
and fixtures for comparisons. Preserve original fixtures outside migration output.

Deliverable: a short baseline and action-to-handler inventory, with provisional
tuning values distinguished from confirmed requirements.

## 2. Add the normal standalone doujin path

Introduce an explicit project format and typed creation/completion behavior.
Reuse ordinary work stages and quality calculation. Complete one print-ready
book exactly once and prevent automatic continuation as an ongoing series.
Keep contest manuscripts and publisher pitch samples semantically distinct.

Expose the format through normal creation controls, with a provisional short
page-count default and valid bounds. Display actual production and print costs;
retain existing account, affordability, era and rights rules. Feed completed
books through current print orders, delivery, stock and sales systems.

Add an explicit simulation migration if the persisted model changes, composing
supported older imports. Default existing projects to their previous behavior.
Use one coherent new simulation version for this milestone as needed; do not
confuse it with any new career-storage envelope version.

Verification: standalone completion is idempotent; saves resume work correctly;
ordinary series still collect normally; contest eligibility and publisher
samples remain correct; rejected actions are atomic; actual sales use normal
stock and finances. Cover low funds and Sandbox without invented production XP
or restoration of achievement eligibility.

## 3. Implement adaptive objective state

Define stable objective/route IDs, completion evidence, selected project and
hide/resume preferences. Keep evaluation read-only and independent of RNG.
Persist the minimal guidance state needed across save/load and career moves;
derive current availability from authoritative engine state.

Implement the creation → production → printing → stock/sales → first-sale path,
then doujin growth, contest and employment suggestions. Choose relevant next
steps for established careers, retaining completed objectives and recognizing
existing evidence. Do not infer missing history or queue old completion alerts.

Verification: fresh and advanced careers, several simultaneous projects, sold-out
books, changed employers, dismissed guidance and legacy imports. Repeated queries
must leave state/RNG unchanged. Route changes must not change career actions,
money, awards, eligibility or previous completion records.

## 4. Integrate Helper-Chan's next-step card

Extend the management screen and existing Helper-Chan tutorial presentation.
Provide the compact objective, hide/resume, route selection and “Show me.” Map
targets to stable controls with readable explanations, focus and temporary visual
emphasis. Resolve disabled/unavailable targets with an explanation instead of
silently invoking an action.

Respect important popups, input focus and pending edits. Routine guidance must
not pause time or replace narrative notifications. Preserve Helper-Chan's desk,
visual identity, companion role and lack of economic effects.

Verification: rendered checks at the existing supported viewport sizes, keyboard
navigation, open modals, unfinished office layouts, hidden guidance, save/load
and existing careers. Confirm “Show me” never submits a financial or career action.

## 5. Add ambience and effects

Inventory existing audio infrastructure before adding buses/settings. Implement
separate ambience/effects volume and mute, real-time playback, bounded concurrent
cues and repetition limits. Keep UI confirmation and important notification
sounds distinguishable without overwhelming the office atmosphere.

Suppress load/replay history and cue backlogs. Set consistent pause, menu and
background-window behavior. Bundle only original or redistributable assets and
record their provenance. Keep music and voice acting out of this milestone.

Verification: listen at normal and maximum speed, under heavy office activity,
pause/resume, mute, menu transitions and save loading. Check natural pitch,
bounded cue rate, settings persistence and unchanged simulation outcomes.

## 6. Improve storage without losing history

Work from the baseline measurements. Prefer a simple versioned lossless format
and compact listing metadata before considering more complicated archival
structures. Reduce duplicate JSON buffers where practical. Retain backward
readers for existing local saves and exported career archives.

Keep atomic writes and validated consistent snapshots. Make listing metadata
recoverable, and measure whether background compression/write is helpful after
accounting for main-thread capture. Preserve artwork links, full history,
commands, RNG, checkpoints, narrative choices and achievement provenance.

Verification: compare complete pre/post state with only explicit migrations
allowed to differ; replay equivalent commands; import/export custom artwork;
recover a missing/corrupt index; reject truncated/oversized/corrupt files; retain
the previous save on interrupted writes. Recheck Sandbox descendants and older
supported versions. Compare size and timing on the same baseline fixtures.

## 7. Implement local problem reports

Add the menu entry, note field, unchecked screenshot/save options, contents
preview and local export result. Reuse safe career snapshot/export infrastructure
after storage changes. Include an explicit build identifier and a bounded,
allowlisted diagnostic summary; redact private paths rather than collecting the
whole environment.

Keep attachments internally consistent without mutating the active game. Handle
cancel, write failure and large careers gracefully. Add no networking or external
submission action.

Verification: report contents match chosen attachments; default reports contain
no save/screenshot; a distinctive synthetic private-path fixture is excluded;
attached careers retain history and Sandbox provenance; cancelled/failed reports
leave the active career and existing files intact.

## 8. Playtest and make targeted adjustments

Run the normal opening using real management controls and time it from creation
through first sale. Record time spent reading, finding controls, producing,
printing and selling, along with difficulty, speed use and seed. Use multiple
representative openings to identify variation; scripted instant fixtures are
not evidence of human pacing.

Exercise all three guidance directions and an existing career. Check financial
quotes, protagonist moves, recovery, contest entry and licensing for confusing
costs or dead ends. Adjust only reproduced bottlenecks and explain each change.
Verify Japanese/Tokyo source data when a factual cost or location assumption is
changed, clearly labeling game-tuned values.

Rerun affected tests after fixes, then the existing simulation and management
regression suite plus long-career coverage appropriate to storage changes.
Retain rendered verification of chibi scale, doorway movement, hyperlapse effects
and Helper-Chan layouts when their presentation code changes.

Deliverable: measured observations, tuning changes, known limitations and a
short private-tester checklist. Identify any human playtesting still outstanding.

## 9. Package and verify the Windows candidate

Add a reproducible Windows export configuration and build identifier, using the
actual Godot/.NET versions established during inventory. Produce a versioned
folder archive containing required runtime files, credits and tester instructions.
Exclude source checkout, private careers, caches and internal test artifacts.

Launch the exported executable from a separate extraction path. Verify offline
startup, creation, first-sale controls, guidance, audio settings, save/load,
career import/export and local reports. Check data goes to a writable user-data
location and survives replacing the extracted application folder. Preserve old
careers; use isolated synthetic data for package tests.

Record whether a clean machine was tested or only the development host, along
with any runtime prerequisites actually verified. Do not claim Steam delivery,
signing, other operating systems or clean-machine compatibility without evidence.

## 10. Delivery record and documentation

Update the roadmap and action inventory. Add the SP8 completion record with the
candidate path/build ID, all 15 requirements mapped to delivered behavior,
validation results, performance comparisons and remaining limitations. Link
tester instructions and explain manual report sharing and permanent Sandbox
achievement restrictions.

Stop at the private-alpha delivery boundary. Public release, native Steam
integration, music and broader platform support remain later work. Do not commit,
push or publish artifacts unless the user separately requests those actions.
