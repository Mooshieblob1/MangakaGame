# Sub-project 8: Private alpha — considerations

Date: 2026-09-24.
Status: decisions 1–15 implemented locally. See the
[delivery and verification record](../sub-project-8-completion.md).
The decisions below remain the confirmed requirements; the delivery record
identifies final tuning and qualification limits.
Design: [private alpha](2026-09-24-private-alpha-design.md).
Plan: [implementation sequence](../plans/2026-09-24-private-alpha.md).
Roadmap: [Mangaka Studio](2026-09-22-roadmap.md).
Builds on: [Sub-project 7 delivery](../sub-project-7-completion.md).

## Decision 1 — delivery target

Confirmed: a polished private alpha, providing a complete playable build for the
user and a small group of testers. Public-demo scope and Steam Early Access
release readiness are later targets, not requirements established by this choice.

## Proposed areas to discuss

- Opening-hour guidance and pacing.
- Full-career playtesting and economic/progression balance.
- Presentation, feedback, audio and menu usability.
- Long-career save size, load responsiveness and stability.
- Build distribution and a practical tester feedback workflow.

The decisions below record which proposals are confirmed. Native Steam
integration remains outstanding from SP7 and is deferred beyond this phase
under decision 12.

## Carried-forward constraints

Preserve the protagonist-following career, separate personal/business finances,
prospective title rights, free parents' home and researched Japanese/Tokyo context
where relevant. Preserve Helper-Chan's identity and role without wages or business
bonuses, the chibi office cast, ambient doorways and hyperlapse effects. Any
Sandbox use permanently disables new platform achievements for that save while
retaining in-game milestones and stories.

## Decision 2 — opening experience

Confirmed: optional guided objectives delivered by Helper-Chan. She suggests
manageable next steps and explains the relevant controls while players remain
free to explore and pursue other actions at their own pace. This is guidance,
not a mandatory introductory sequence that locks access to the wider game.

## Decision 3 — first recommended objective

Confirmed: Helper-Chan initially recommends publishing a small doujin. Teach
creation, production, printing and selling through one manageable project.
The objectives remain optional; players can pursue contests or other available
routes instead. Decisions 4–5 set the format and pacing target; length remains open.

## Decision 4 — introductory doujin format

Confirmed: a short standalone one-shot. Players finish one story and can print
and sell it as a small book without first producing several chapters for a
collected volume. Exact page count remains open; decision 5 sets the pacing target.

## Decision 5 — opening-loop pacing

Confirmed: aim for roughly 15–20 minutes of real playtime from guided creation
through the first sale, using normal speed controls. Provide an early payoff
while teaching the essentials. This is a playtesting target, not a guaranteed
sale, fixed timer or authorization for hidden financial bonuses.

## Decision 6 — guidance presentation

Confirmed: present the current suggested objective in a compact next-step card.
An optional “Show me” button highlights the relevant control. Routine objective
guidance does not require a sequence of pausing popups; retain Helper-Chan's
existing popups for important events.

## Decision 7 — guidance after the first sale

Confirmed: after the first sale, Helper-Chan offers a choice of direction:
grow the doujin business, enter a contest or seek studio employment. Tailor her
subsequent suggestions to the chosen route. These are optional guidance choices,
not restrictions on the player's available career actions.

## Decision 8 — private-alpha audio scope

Confirmed: include office ambience and sound effects in the private alpha;
defer music until later. Establish the studio atmosphere and provide audible
feedback for actions and notifications. Exact sounds and playback behavior
remain to be designed.

## Decision 9 — audio at accelerated simulation speeds

Confirmed: keep office ambience calm and natural at every simulation speed.
Play ambience in real time and limit repeated activity cues rather than making
them denser or faster as time accelerates. Preserve natural pitch.

## Decision 10 — private-tester feedback

Confirmed: provide an in-game “Report a problem” tool. Testers can write a note
and export a local report to review and share manually. Screenshots and saves
are optional attachments. This does not authorize automatic uploads or sending
reports to an external service.

## Decision 11 — private-alpha platforms

Confirmed: target Windows first for the private alpha. Focus packaging and
testing on the platform already verified locally. Linux and macOS support are
outside this phase's delivery requirements.

## Decision 12 — Steam integration timing

Confirmed: deliver a standalone Windows build that testers can play without
Steam. Retain the existing achievement eligibility tracking and permanent Sandbox
restriction. Defer the native Steam adapter, live achievement delivery and Steam
distribution setup until a later phase.

## Decision 13 — balance-pass scope

Confirmed: make targeted adjustments supported by playtesting. Address excessive
waiting, unclear costs and progression bottlenecks while preserving the current
systems. A broad redesign of income, expenses and career progression is outside
this phase's agreed scope.

## Decision 14 — long-career history retention

Confirmed: retain the full recorded history. Optimize storage and loading while
keeping old transactions, events and career records available. Do not replace
older detailed records with lossy summaries to reduce save size.

## Decision 15 — guidance in existing careers

Confirmed: make optional objectives available in existing careers too. Recognize
completed progress and suggest relevant next steps without requiring players to
repeat the opening. Adapt to the career's recorded evidence and current situation;
do not replay a backlog of introductory notifications.
