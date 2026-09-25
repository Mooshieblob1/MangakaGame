# Sub-project 8: Private alpha — design

Date: 2026-09-24.
Status: implemented locally; see the [delivery record](../sub-project-8-completion.md).
The original design below is retained for traceability; the delivery record
describes final choices, measured results and qualification limits.
Requirements: [15 confirmed decisions](2026-09-24-private-alpha-considerations.md).
Plan: [implementation sequence](../plans/2026-09-24-private-alpha.md).
Baseline: [Sub-project 7 delivery](../sub-project-7-completion.md).

## Outcome and scope

Deliver a polished standalone Windows build for the user and a small group of
private testers. Add optional opening guidance, a short standalone doujin path,
natural ambience and effects, local feedback export, lossless save improvements
and targeted fixes supported by playtesting.

Music, native Steam integration, public distribution, Linux/macOS qualification
and a broad economic redesign are outside this milestone. Keep existing
achievement eligibility tracking, including the permanent restriction after any
Sandbox use. An in-game milestone is distinct from a platform achievement.

The sections below implement the confirmed choices. Suggested page counts,
storage formats and interface details are proposals to validate during
implementation; they are not additional user-approved design decisions.

## Helper-Chan guidance

Present one compact next-step card on the management screen. It names a concrete
objective, explains why it matters and offers “Show me.” The player can hide
guidance and resume it later. No action is locked behind completing a lesson.
Routine progress updates do not pause the simulation or queue repeated popups.
Existing important-event popups retain their priority.

“Show me” opens the appropriate panel and highlights its relevant control. It
does not purchase, submit, hire, advance time or change an assignment. Resolve
targets through stable control identifiers, with keyboard focus and readable
text as well as highlighting. Respect modal dialogs and unfinished edits; do
not discard an office layout draft to navigate to a lesson.

The initial suggested sequence is:

1. Create a short standalone doujin and choose its creative direction.
2. Assign and finish its ordinary production work.
3. Review the actual printing quote, choose an affordable batch and order it.
4. Receive stock and use an available sales route.
5. Record the first actual sale, then choose a guidance direction.

Offer three directions after that sale: grow the doujin business, enter a contest
or seek studio employment. These affect suggestions only. A player may change
direction or pursue several activities independently. Contest suggestions must
explain unpublished-work requirements rather than suggest entering a book already
sold when it is ineligible.

Use stable objective identities, an explicitly selected project where needed,
and typed evidence from production, printing, sales and career state. Queries
must not consume simulation randomness. Record completed objectives idempotently;
selling out or changing employer must not erase their completion. Guidance grants
no money, productivity, talent, awards or achievement eligibility.

Apply guidance to existing careers on load. Recognize evidence already present,
skip completed introductory steps and suggest a useful next step for the current
situation. A published career need not create another introductory book. Where
old saves lack evidence, do not invent history or treat missing records as proof
of failure. Offer a relevant route or let the player select a current project.
Do not replay historical completion notifications.

Keep guidance preferences and progress in the career export so they survive
save/load and protagonist moves. They must not affect authoritative financial
or production outcomes. Helper-Chan remains the same loyal implied employee,
with her own equipped desk, no wage and no company-performance bonus.

## Standalone doujin production

Provide an explicit standalone format independent of contest entry and publisher
pitch samples. Reuse existing production, quality, printing, stock and sales
systems. Completing the one-shot creates one print-ready book; it does not wait
for the usual multi-chapter collection or silently schedule an ongoing series.

A 16-page starting suggestion is a provisional tuning candidate, with the actual
supported range established against production and book rules. Display page
count, remaining work, printer costs, delivery time and relevant sales costs
before commitment. Use the correct personal/business account and existing
affordability checks. Do not add a special tutorial subsidy or free print run.

Keep ordinary ongoing-series behavior and contest eligibility intact. Any later
expansion of a standalone title must follow an explicit supported action; this
milestone does not require a new sequel system. Migrate older projects to their
existing format without reclassifying their history or rights.

Aim for 15–20 minutes of real playtime from guided creation through first sale,
using normal speed controls. Measure reading, control discovery, production,
printing and sales waits separately. This is a playtest target, not a timer,
guaranteed sale, altered RNG result or promise for every difficulty and route.
Use ordinary sales outcomes; diagnose excessive waiting before adjusting tuning.

## Ambience and sound effects

Add a restrained office ambience layer and clear effects for deliberate actions
and meaningful notifications. Keep pitch and playback in real time at every
simulation speed. Coalesce repeated activity cues, cap concurrent effects and
space them using real elapsed time so accelerated simulation stays calm.

Separate ambience and effects volume controls, including mute, are proposed
interface defaults. Keep essential information visible when muted. Suppress
historical sounds on load/replay and avoid a burst of queued cues after a pause
or modal closes. Define pause, background-window and menu behavior consistently
with the existing settings; audio must not continue accumulating a backlog.

Audio reads presentation events and never changes the simulation or its RNG.
Document the provenance and redistribution terms of bundled sounds. Procedural
or original effects are acceptable. Music and voice acting are deferred.

Preserve the chibi cast, exaggerated Helper-Chan hair, proper ambient doorways
and existing hyperlapse motion effects. Audio acceleration must not undermine
the visual treatment already selected.

## Local tester feedback

Add “Report a problem” in the in-game menu. The tester writes a note and can
optionally include a screenshot and a career snapshot. Both attachment options
start unchecked. Show what the report will contain before exporting a local
archive, then show its saved location for manual review and sharing.

Include a build identifier, game/save format versions and a bounded diagnostic
summary from an explicit allowlist. Avoid wholesale environment dumps, account
details, arbitrary filesystem contents and username-bearing absolute paths.
Clearly describe that an attached save includes the career's recorded history
and player-entered content. Capture attachments consistently without altering
the live career, clearing Sandbox provenance or marking pending events resolved.

Cancellation and write failure leave the career intact. Report generation must
not upload files, open an external submission service or send messages. The
exported report is a convenience for manual sharing, not an automatic crash
telemetry service. Bound attachment/archive sizes and validate archive paths
where existing career import/export code is reused.

## Lossless saves and responsive menus

Retain all recorded transactions, events, commands, career records and narrative
choices. Existing long-career evidence includes roughly 65 MB of serialized
state; prioritize measured improvements to save size, listing and load/save
responsiveness. Do not replace older records with lossy summaries. Migration
cannot reconstruct information absent from an imported save.

The current career envelope and simulation have separate versions. Preserve
backward reading of supported saves and introduce explicit versioned changes
only where needed. Proposed first steps are lossless compression, avoiding
unnecessary nested JSON copies and storing compact listing metadata so opening
the career menu does not parse every complete history.

Treat listing metadata as recoverable derived data. A missing or stale index
must not hide otherwise valid careers. Preserve atomic replacement, original
saves during migration, custom artwork, replay/checkpoints and achievement
provenance. Validate truncated, corrupt and oversized inputs before replacing
anything. Export/import must retain the same information as local saves.

Measure capture, encoding, compression, disk write, list and load separately.
If compression/writing moves off the main thread, first take a consistent
snapshot; workers must not traverse mutable game state or scene nodes. Do not
claim background saving removes capture stalls without measuring them.

## Targeted playtesting and packaging

Exercise a fresh standard career, an established imported career, a long career
and Sandbox. Cover the opening sale, all three guidance routes, career moves,
recovery from financial pressure, awards/licensing and save recovery. Fix
reproduced waiting, unclear quotes, navigation problems and progression blocks;
record each balance adjustment and the observation behind it.

Preserve separate personal/business finances, prospective title ownership and
the rent-free parents' house. When changes depend on actual Japanese costs,
travel or industry practices, verify relevant primary Japanese/Tokyo sources and
distinguish historical data from present-day data and gameplay tuning.

Prepare a versioned Windows folder archive with its executable, required runtime
files, credits and short tester instructions. Keep writable careers/settings in
the application's user-data location. Exclude developer caches, personal saves
and internal test artifacts. Play must not require Steam or the source checkout.
No installer or automatic updater is required for this milestone.

Test the actual exported executable, including saving after extraction to a
different location. Record which machine and dependencies were used; launching
on the development machine alone is not proof of a clean-machine install.
Provide a playtest checklist and known limitations. Automated smoke tests and
scripted fixtures do not establish that a human opening session meets its target.

## Acceptance criteria

- Fresh and existing careers receive optional, relevant guidance without repeated
  completed lessons or restricted actions (decisions 2, 6, 7 and 15).
- A normal standalone doujin can progress from creation to an actual first sale,
  with measured opening pacing and clear real costs (decisions 3–5 and 13).
- Ambience/effects remain natural under high simulation speed; music is absent
  from this delivery scope (decisions 8–9).
- A tester can review and export a local report, selecting optional attachments,
  without any automatic transmission (decision 10).
- Supported saves retain full recorded history and permanent Sandbox eligibility
  restrictions across migration, compression and export/import (decisions 12, 14).
- A standalone Windows candidate is packaged and tested, with actual playtest
  evidence and remaining qualification clearly recorded (decisions 1, 11–13).
