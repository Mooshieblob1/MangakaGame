# Sub-project 6: Management UI and charts — considerations

Date: 2026-09-24.
Status: twenty choices confirmed; design approved and implementation verified
locally on 2026-09-24. See the [delivery record](../sub-project-6-completion.md).
Design: [management UI](2026-09-24-management-ui-design.md).
Plan: [implementation](../plans/2026-09-24-management-ui.md).
Roadmap: [Mangaka Studio](2026-09-22-roadmap.md).
Builds on: [Sub-project 5 delivery](../sub-project-5-completion.md).

The user asked to move on after Sub-project 5. Continue the established
question-by-question design discussion and record answers as they arrive.
Recommendations below are not confirmed requirements.

## Existing scope and constraints

The roadmap assigns full management screens, graphs, rankings and an abridged
manga viewer to this milestone. The current Godot interface is a development
harness with Production, Publishing, Staff and studio, Studio management,
Tokyo map, Office and Industry tabs. The implemented simulation supplies their
actions and data; it remains the authority for gameplay.

Preserve the protagonist-following perspective, separate personal and business
money, employer authority, historical discovery rules, configurable auto-pause,
save continuity and the existing office camera, furniture and hyperlapse effects.
The starting parents' home remains rent-free. Any new Tokyo-specific factual
content should follow the established research policy. Awards, anime adaptation
systems and broader balance work remain assigned to Sub-project 7.

## Decision 1 — the main play screen

1. **Office as the home screen, with management panels** (recommended): keep
   the studio visible during ordinary play; open staff, series and financial
   details alongside it, with full-screen reports available for dense information.
2. **Management dashboard as the home screen**: prioritize deadlines, finances
   and decisions; open the office as a separate view.
3. **Equal office and management modes**: provide two primary workspaces with
   an explicit switch between them.

The first option builds on the user's interest in ambient occupants and the
office's visual life while retaining room for detailed management.

Confirmed by the user: option 1. The office is the home screen, with management
panels alongside it and expandable reports for dense information.

## Decision 2 — management panel behavior

1. **One main panel at a time** (recommended): selecting staff, series or finances
   replaces the current side panel. Back returns to the previous view. Detailed
   reports can expand when needed, keeping ordinary play uncluttered.
2. **Several movable windows**: keep multiple management views open and arrange
   them freely over the office, with more flexibility and more window management.
3. **Two fixed panels**: allow two management views side by side for comparisons,
   leaving less room for the office while both are open.

Confirmed by the user: option 1. Use one main management panel at a time,
with Back navigation and expandable reports.

## Decision 3 — information always visible

1. **Compact status bar** (recommended): always show date and speed controls,
   current studio, separate business and personal balances, and an alert count.
   Deadlines, trends and breakdowns appear in the relevant panel.
2. **Detailed status strip**: also keep the next deadline, monthly profit and
   team wellbeing visible, taking more screen space.
3. **Minimal overlay**: show only date, speed and alerts; open a panel to see
   finances and studio details.

Confirmed by the user: option 1. Keep a compact status bar with date, speed,
current studio, separate business and personal balances, and alerts.

## Decision 4 — finding decisions and problems

1. **One priority inbox** (recommended): collect decisions and problems in one
   place, with urgent deadlines first. Each item opens the relevant person,
   series or action. Routine news sits in a quieter section of the same inbox.
2. **Separate department queues**: show pending matters within Staff, Series,
   Finances and Industry, with badges on their navigation buttons.
3. **One chronological feed**: show decisions and news in time order, with
   filters for urgent or unresolved items and links to relevant actions.

This choice concerns organization and navigation. Preserve the existing
configurable auto-pause rules and the agreed handling of urgent staff decisions.

Confirmed by the user: option 1, with an addition. Use one priority inbox and
also present important matters through popups from a cute assistant named
**Helper-Chan**. Keep those matters accessible in the inbox after the popup.
Her role, presentation and important-popup categories are specified below.
These choices do not change auto-pause settings by themselves.

## Decision 5 — Helper-Chan's role and appearance

The user specified a custom direction: Helper-Chan is an **implied employee**
and the protagonist's number-one fan. She always follows the player, regardless
of career or studio changes. Her continued presence is unconditional; ordinary
employee departure or follower-acceptance rules must not remove her. Her desk
and relationship to staff mechanics are specified in Decision 7.

Her confirmed character design is always female, with blonde twintails, blue
eyes, glasses, a collared shirt, a pencil skirt and a clipboard. Her defining
feature is **absurdly long, enormous hair**. Preserve its exaggerated length and
volume, rather than reducing it to ordinary long twintails. The supplied reference
shows the twin tails widening into large cascading curls and a silhouette much
wider than her body.

The user's [visual reference](../references/helper-chan.png) is preserved in the
repository. Decision 6 authorizes using it directly as the base portrait as well
as the reference for her 3D appearance; runtime integration is still pending.

## Decision 6 — Helper-Chan's presence and reference fidelity

1. **Expressive 2D portraits and a 3D office presence** (recommended): portraits
   accompany her popups, while a matching stylized character can be seen in the
   current office. Preserve her distinctive hair in both representations.
2. **Expressive 2D portraits only**: she appears through popups and guidance,
   with her employment implied rather than represented by an office character.

Office presence would not by itself grant production skills or other employee
mechanics; those remain separate from this presentation choice.

Confirmed by the user: option 1, with strong reference fidelity. Include both
expressive 2D portraits and a 3D office character, making her as close to the
supplied image as possible. Direct use of the supplied image is authorized.
Use it as the base portrait; preserve its face, glasses, outfit, proportions,
clipboard and especially the length, volume and cascading curls of the twintails
when translating her into 3D. Review the model visually against the reference;
do not substitute a generic office character or simplify away her defining hair.

The user offered to supply expression variants. A proposed starting set is the
existing neutral portrait plus happy, excited, concerned, surprised and determined
expressions. The exact set is not yet confirmed. Preserve matching appearance
and framing across variants; no additional images are needed to continue design.

## Decision 7 — Helper-Chan's desk and gameplay role

The user specified a custom direction. Wherever she follows the protagonist,
Helper-Chan has her own already-equipped desk by default, usually beside the
protagonist's desk. The player does not need to hire her or purchase her setup.
She receives no wage and does not affect company operations. Her functional
roles are tutorials and notifications, and she participates in the plots of a
few events. Those events' specific stories and outcomes remain to be designed.

Her presence and equipment should therefore not consume the ordinary workforce's
productive capacity, grant bonuses, create upkeep or require needs management.
Treat her workstation as a dedicated companion setup rather than a free source
of sellable business furniture. These are implementation consequences of the
confirmed non-economic role, not additional staff-management features.

Placement must accommodate her at the parents' home and later workplaces while
preserving room access and usable staff desks. Prefer a position beside the
protagonist; exact placement and any necessary companion space in fixed floor
plans must be worked out in the implementation design. Do not silently omit her
desk when the ordinary staff capacity is full.

## Decision 8 — which matters trigger Helper-Chan popups

1. **Decisions, urgent problems and major milestones** (recommended): use popups
   for matters such as a contract offer, imminent staff departure, first
   serialization or a Helper-Chan story event. Routine sales and rankings stay
   in the inbox. Contextual tutorial prompts remain separately available.
2. **Action required and tutorials only**: successes and other non-actionable
   events stay in the inbox, minimizing interruptions.
3. **All notable updates**: she also announces ordinary releases, ranking changes
   and industry developments, producing a more talkative experience.

This choice determines popup eligibility, not a new blanket pause rule. Retain
the existing configurable auto-pause behavior and keep messages in the inbox.

Confirmed by the user: option 1. Helper-Chan presents decisions, urgent problems
and major milestones; routine sales, rankings and ordinary news stay in the inbox.

## Decision 9 — tutorial structure

1. **Short contextual guidance** (recommended): Helper-Chan explains features
   when first encountered. Tips can be skipped, disabled and revisited through
   Help; the player is free to choose their own next action.
2. **Guided opening sequence**: an optional step-by-step introduction takes the
   player through creating a first series, managing work and releasing a book,
   before handing over to ordinary play.
3. **Help only when requested**: Helper-Chan explains the current screen when
   asked, with no automatic tutorial prompts.

Tutorial preferences are separate from important gameplay notifications.

Confirmed by the user: option 1. Give short contextual tips on first encountering
a feature, with skip, disable and revisit controls. Disabling tutorials does not
disable important gameplay notifications.

## Decision 10 — the series overview

1. **Compact series cards with expandable details** (recommended): each card
   shows the title, lead creator, current chapter's progress, next deadline and
   warnings. Selecting a card opens production, team and publishing details.
   Keep dense comparisons available in an expanded report.
2. **Spreadsheet-style list**: sortable rows and columns show more series and
   performance figures together, with details opened from a selected row.
3. **Production board**: organize chapter cards by production stage so the player
   can see work across series and identify bottlenecks. This presentation would
   not itself change scheduling or stage prerequisites.

Confirmed by the user: option 1. Use compact series cards with expandable
production, team and publishing details, and expanded reports for comparisons.

## Decision 11 — organizing staff management

1. **By series and team** (recommended): show creators and assistants around
   their assigned series, with a separate unassigned section and a studio filter.
   Selecting a person opens their skills, wellbeing, pay and work controls.
2. **By studio location**: start with each workplace and its staff, then inspect
   individuals and their assignments from that location.
3. **One sortable staff list**: show everyone within the player's management
   authority together, with filters for studio, specialty and assignment.

This is a default view choice, not a change to work allocation or employer
authority. Shared staff must remain identifiable across assignments; Helper-Chan
is a companion rather than an entry in the productive workforce.

Confirmed by the user: option 1. Organize staff by series and team, with a
separate unassigned section, a studio filter and individual detail views.

## Decision 12 — the finance overview

1. **Cash and upcoming costs first** (recommended): show the business balance,
   money available after reservations, wages and bills, plus income and spending
   charts. Keep personal finances separate; open detailed reports when needed.
2. **Series earnings first**: lead with each manga's revenue and attributable
   costs, with overall business cash and obligations in a secondary view.
3. **Detailed ledger first**: lead with a sortable transaction list, with summary
   charts and upcoming obligations available separately.

This determines the default finance view, not the accounting rules. Distinguish
balances, reserved funds, unpaid obligations and forecasts. Do not invent
per-series costs or historical chart samples that the simulation does not record.

Confirmed by the user: option 1. Lead with business cash, available spending
money and upcoming obligations, supported by income/spending charts. Keep
personal finances separate and provide detailed reports on demand.

## Decision 13 — the abridged manga viewer

1. **A series showcase** (recommended): an original cover, a short synopsis and
   a few representative illustrated panels convey the manga's identity. Chapter
   and volume history remains available alongside it, without illustrating every
   chapter separately.
2. **Short readable chapter excerpts**: provide a brief comic sequence for each
   completed chapter, requiring a substantially larger story and art system.
3. **Covers and written summaries**: present a book-style archive with cover art,
   descriptions and release history, without illustrated interior panels.

The selection determines content scope. How original art and text are authored,
varied and stored still needs an explicit design; the current simulation does
not already contain illustrated chapters or generated story content. Historical
analogues must not reproduce source manga pages or copied scenes.

Confirmed by the user: option 1. Provide a series showcase with an original
cover, short synopsis and a few representative illustrated panels, alongside
chapter and volume history. A comic sequence for every chapter is outside scope.

## Decision 14 — management interface art direction

1. **Warm editorial style** (recommended): cream panels, dark ink-like text,
   restrained accent colors and subtle manga-page details. Keep charts and
   controls clean and readable, with the office and character art providing life.
2. **Dark management style**: charcoal panels, crisp typography and restrained
   color accents, giving reports and charts a more technical presentation.
3. **Bright illustrated style**: colorful panels, rounded controls and more
   playful manga-inspired decoration throughout the interface.

This concerns the interface around the office and artwork. Helper-Chan's
confirmed reference appearance remains unchanged under any of these choices.

Confirmed by the user: option 1. Use a warm editorial interface with cream
panels, dark ink-like text, restrained colors and subtle manga-page details.

## Decision 15 — artwork for the series showcase

1. **Bundled original art library** (recommended): match reusable covers and
   representative panels to genre, customize cover titles and allow optional
   player image imports. Works offline; art can recur across different series.
2. **Generate new art during play**: create artwork for each series through an
   image-generation system. This adds model or service integration, generation
   delays and a need to design availability and failure handling.
3. **Player-supplied artwork**: use imported covers and panels, with a simple
   title-and-genre presentation until the player supplies images.

This is a choice about where game content comes from, separate from the
confirmed showcase layout and permission to use Helper-Chan's supplied image.
No runtime generation service or account requirement is assumed at this stage.

Confirmed by the user: combine options 1 and 3. Supply bundled original artwork
and support player-supplied covers and panels. Use the bundled genre-matched
artwork by default; imports can replace a series' cover or individual panels,
with bundled art retained for slots the player has not replaced. Support returning
to the bundled artwork. Runtime image generation is outside this selection.
Imported artwork is presentation content and must not alter series performance.

## Decision 16 — chart depth

1. **Focused charts with preset periods** (recommended): provide income/spending,
   book sales and ranking trends, with one-month, three-month, one-year and
   all-time views, readable values and links to the relevant detailed records.
2. **Customizable analytics dashboard**: also let players assemble their own
   chart layouts and choose metrics, comparisons and custom date ranges.

Only show historical values supported by recorded data. Mark unavailable history
clearly, keep forecasts separate, and respect each metric's actual sampling
frequency. Historical competitors' private financial or future information must
not become visible through charts.

Confirmed by the user: option 1. Provide focused income/spending, sales and
ranking charts with month, quarter, year and all-time views and links to details.

## Decision 17 — management scope across studios

1. **Whole business by default, with location filters** (recommended): management
   panels show all staff and series under the player's authority. Switching the
   visible office changes the scene, without silently filtering every report.
   Players can explicitly narrow a panel to a studio.
2. **Follow the visible office by default**: switching offices also filters staff
   and series panels to that workplace. A clear all-studios view remains available.

Under either choice, company-wide finances must be labeled as such, and employed
play remains limited to the protagonist's authorized team and budget. Viewing
another location cannot grant control over an employer or former business.

Confirmed by the user: option 1. Default to the whole business within the
player's authority, with explicit studio filters. Changing the office view does
not silently change the scope of management reports.

## Decision 18 — menus and career saves

1. **Full game entry and save flow now** (recommended): add a main menu with
   Continue, New Career, Load and Settings; provide separate career saves,
   named manual saves and rotating autosaves. New Career exposes the already
   supported starting options rather than inventing new difficulty mechanics.
2. **Management screens first**: retain the existing direct launch and simple
   Save/Load workflow for this milestone, deferring the main menu and career-save
   browser to the later polish milestone.

Existing saves must remain accessible through the supported import path.
Autosave frequency, retention and presentation of imported artwork should be
specified in the implementation design if the full save flow is selected.

Confirmed by the user: option 1. Include the full main menu, separate career
saves, named manual saves, rotating autosaves and supported existing-save imports.

## Decision 19 — Helper-Chan's dialogue tone

1. **Capable assistant with an enthusiastic fan side** (recommended): concise
   and clear when explaining decisions or problems, visibly delighted by the
   protagonist's successes, and supportive during setbacks.
2. **Bubbly cheerleader**: openly energetic and expressive in most conversations,
   while still explaining urgent information clearly.
3. **Dry, witty assistant**: composed delivery and gentle teasing, with loyalty
   and enthusiasm showing through occasional breaks in her professional manner.

All options preserve her established number-one-fan devotion and permanent
presence. Tone should not hide consequences, overwhelm actionable information
or imply unapproved romance or relationship mechanics.

Confirmed by the user: option 1. Write her as a capable assistant with an
enthusiastic fan side: practical guidance, delight at successes and support
during setbacks.

## Decision 20 — initial Helper-Chan story events

1. **A few milestone scenes** (recommended): short character moments for events
   such as the first released book, first serialization, a major career move or
   a return to the parents' home. Give each scene a once-only trigger and keep
   it distinct from routine notifications.
2. **Milestones plus occasional everyday scenes**: also include rare office
   conversations and small incidents between major achievements.
3. **A connected character storyline**: link her appearances into a longer
   sequence with dialogue choices and remembered responses, requiring a broader
   narrative system.

These scenes express her established role and personality. They do not grant
production or financial bonuses, alter her permanent loyalty, or replace the
existing consequences of the underlying career events. Exact scripts and trigger
rules should be included in the consolidated design for review.

Confirmed by the user: combine options 3 and 2. Include a connected character
storyline with dialogue choices and remembered responses, plus occasional
everyday office scenes. This expands the earlier tentative few-milestone-scenes
scope. Her loyalty remains unconditional, and narrative choices do not grant
business bonuses. The consolidated design proposes a bounded initial arc and
everyday scene set, with scripts, triggers and persistence rules.

## Consolidation and review

All twenty choices are now recorded. The linked design and plan consolidate them
with proposed scripts, layout details, save rules and verification work. These
implementation defaults are proposals rather than separately confirmed choices.

The consolidated design should also specify readable scaling, keyboard access,
color-independent chart/alert cues, historical-data availability, artwork import
persistence, and how existing production and publishing actions fit these views.

This completes the design-question pass. No implementation or asset-completion
claim is made by these documents.
