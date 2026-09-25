# Sub-project 9: Overall GUI redesign — considerations

Date: 2026-09-25.
Status: seventeen initial decisions confirmed; clickable mockup approved visually,
with an additional requirement to support both 16:9 and 21:9.
The user approved both full-monitor layout previews on 2026-09-25.
The user subsequently instructed implementation. Source changes are delivered;
a subsequent explicit build request authorized compilation and packaging.
See [local alpha.6 verification](../sub-project-9-build-verification.md).
See the [source implementation record](../sub-project-9-source-implementation.md).

## Confirmed brief

- The user likes how the game functions and wants a dedicated sub-project to
  improve the overall GUI. Preserve simulation behavior and existing capabilities.
- Continue the established process of asking design questions one at a time,
  recording answers, then producing a design and implementation plan for review.
- Dark mode is required. Retain the existing larger-text option and keyboard
  controls, including Escape leaving text entry before closing its containing UI.
- Preserve the user's earlier direction: management navigation opens the full
  management view; Inbox and Series can use the office sidebar. Any proposed
  revision to this arrangement needs to be presented as a new choice.
- Keep production progress and numbers readable, publishing status explicit,
  personal and business money distinct, and printing/distribution/convention
  workflows understandable. Preserve visible fans, sold copies, stock and
  convention reservations.
- Helper-Chan remains the established companion and guide. Her approved chibi
  proportions and appearance are not being redesigned by this GUI milestone.
- Leave compilation and packaging until authorized. The previous permission
  covered the chibi preview, not a standing exemption for future builds.

## Current baseline

- [Sub-project 6 design](2026-09-24-management-ui-design.md) defined an editorial
  interface, but the current player interface still exposes broad workbench forms.
- [Private alpha feedback](../private-alpha-feedback-3.md) records the newer HUD,
  distribution and studio-management changes. The studio dashboard source now has
  category navigation, location cards, floor-plan previews and grouped controls.
  That dashboard has not yet been compiled or visually checked.
- Existing workflows and action routes are recorded in
  [management UI actions](../management-ui-actions.md).

## Decisions to work through

The following are discussion topics, not approved requirements:

1. Overall visual direction and amount of manga-inspired decoration.
2. Main navigation and the relationship between the office and management views.
3. HUD priorities and how much information stays visible at once.
4. Common page structure: summaries, lists/cards, details and primary actions.
5. Production, publishing, printing and sales as clear connected workflows.
6. Staff, studio, finance and industry information density.
7. Inbox, alerts, Helper-Chan popups and contextual help.
8. Typography, icons, artwork, light/dark palettes and contrast.
9. Window sizes, text scaling, keyboard navigation and motion preferences.
10. Visual mockup review, implementation order and acceptance checks.

## Confirmed decision 1: visual direction

Suggested directions to discuss:

1. A polished manga-inspired management interface: clean cards, restrained ink
   details, illustrated accents and clear numbers.
2. A minimal management dashboard: understated styling and information density.
3. A playful cozy interface: rounder panels, larger illustrations and more decoration.

Selected option 1: a polished manga-inspired management interface, with clean
cards, restrained ink details, illustrated accents and clear numbers. Decoration
must support readability. The alternatives above are retained as discussion history.

## Confirmed decision 2: navigation

Recommended: one consistent full management workspace with a persistent left
navigation rail and a compact top status bar. Each area has its own designed page;
replace the overlapping navigation/workbench tabs. Keep Office one click away,
with Inbox and Series still available as sidebars while viewing the office.

Alternative: retain horizontal top navigation, consolidating the duplicate
workbench tabs into a single row and redesigning the pages beneath it.

Selected option 1: a persistent left navigation rail, one consistent full
management workspace, and a compact top status bar. Office remains one click
away; Inbox and Series retain their office sidebars. Replace duplicate navigation
and workbench tabs with clear pages. The alternative is retained as discussion history.

## Confirmed decision 3: persistent HUD

Recommended: use two compact rows. The global row shows date, speed, business and
personal money, fans, and the clickable inbox alert. The second row follows the
selected series and shows current work, a progress bar with numbers, publishing
status, sold copies, stock and reservations. Keep the series selection explicit.

Alternative: keep all those indicators in one dense row to maximize vertical
space for the office and management pages.

Selected option 1: two compact rows, separating global information from the
explicitly selected series. Preserve readable numbers inside the progress bar,
clear publishing status, and distinct sales, stock and reservation counts.

## Confirmed decision 4: management page structure

Recommended: summaries and useful actions first, with selectable cards or rows
opening focused details. Put advanced settings in expandable sections. Preserve
the current selection and filters when returning from details.

Alternative: show detailed tables and settings immediately, minimizing the need
to open individual items at the cost of a denser first view.

Selected option 1: overview-first pages with key numbers and useful actions at
the top, selectable cards or rows for focused details, and expandable advanced
settings. Preserve selection and filters when returning from details.

## Confirmed decision 5: series workflow

Recommended: each series has a visual lifecycle strip showing creation,
production, publishing/release and sales, with a prominent next useful action.
Represent self-publishing and publisher pitching as separate routes with actual
statuses. Keep ongoing production and sales visible together; do not imply a
series can only occupy one stage or force the player through a wizard.

Alternative: organize series details into conventional Production, Publishing
and Sales tabs without a lifecycle strip or suggested next action.

Selected option 1: a visual series workflow with clear production, pitching or
self-publishing, and sales statuses, plus a useful next-action button. Ongoing
production and sales remain visible together. This guides navigation without
forcing a sequence or performing an action automatically.

## Confirmed decision 6: staff presentation

Recommended: portrait cards showing each employee's name, role, current work,
strongest skills and wellbeing, with a compact comparison table available for
assignments and recruitment. Selecting an employee opens focused details.

Alternative: default to a sortable staff table with portraits as small accents,
prioritizing comparison across a large team over individual character presence.

Selected option 1: portrait cards by default, showing role, current work,
strongest skills and wellbeing, with a compact comparison table for assignments
and recruitment. Selecting an employee opens focused details.

## Confirmed decision 7: finances presentation

Recommended: clearly separated Business and Personal account summaries, with
income versus expenses, upcoming obligations and a cash-flow chart first. Keep
the detailed ledger and borrowing controls one click away. Distinguish available
business funds from reserved wages and show employer budget restrictions where
applicable. Label any forecasts as estimates with their time period.

Alternative: ledger-first presentation, prioritizing sortable transactions and
account balances, with charts and summaries as secondary views.

Selected option 1: financial overview first, with clearly separated Business and
Personal accounts, income versus expenses, upcoming bills and a cash-flow chart.
Transactions and borrowing controls remain one click away. Preserve available
funds, wage reservations and employer spending restrictions as distinct concepts.

## Confirmed decision 8: studios presentation

Recommended: location cards with floor-plan previews, rent, staff versus usable
desks and storage, plus direct View and Furnish actions. Browse potential moves
and branches in a comparison view showing costs, capacity and tradeoffs before
opening a preview. Keep the Tokyo map accessible from the same page.

Alternative: make the Tokyo map the primary studio screen, with current and
available locations opening their details when selected on the map.

The recent studio dashboard source is a starting point, not a visually verified
implementation of this milestone.

Selected option 1: location cards first, with floor-plan previews, rent, desk
capacity and storage, plus direct View and Furnish actions. Compare prospective
properties side by side and keep the Tokyo map accessible from the same page.

## Confirmed decision 9: industry presentation

Recommended: a manga-magazine-inspired overview with clear sections for current
rankings, publisher opportunities, awards, adaptations and industry news. Use
cover art and restrained editorial accents; open detailed comparisons and
eligibility requirements from the relevant cards. Show only information the
current career has unlocked or discovered.

Alternative: a compact dashboard of sortable lists, prioritizing comparison of
rankings, publishers and opportunities with less artwork.

Selected option 1: a manga-magazine-inspired overview using cover art and clear
sections for rankings, publishers, awards, adaptations and news. Cards open
detailed comparisons and eligibility requirements. Respect the career's current
unlocks and discovered information.

## Confirmed decision 10: inbox organization

Recommended: prioritize an actionable Needs attention section, followed by
Results and milestones, then Routine updates. Show a short summary, relevant
person or series, and a direct action where appropriate. Group repetitive updates
and offer an All messages chronological view. Keep unread state separate from
unresolved decisions; preserve the existing rules for important Helper-Chan popups.

Alternative: default to one chronological feed with clear category badges and
filters, leaving actionable decisions alongside the other messages.

Selected option 1: Needs attention first, followed by Results and milestones,
then Routine updates, with direct actions and an All messages chronological
view. Group repetitive updates. Unread state remains distinct from unresolved
decisions; existing important-event popup rules remain authoritative.

## Confirmed decision 11: Helper-Chan notification presentation

Recommended: a compact portrait card at the edge of the screen for routine tips
and acknowledgements, and a larger illustrated dialogue panel for important
decisions and story events. Clearly distinguish the summary, consequences and
choices. Respect existing notification preferences, priorities and pause rules;
do not introduce additional popups for routine inbox updates.

Alternative: use one consistent medium-sized portrait panel for all existing
Helper-Chan messages, changing its emphasis according to importance.

Selected option 1: compact portrait cards for guidance and larger illustrated
dialogue panels for important decisions and story events, with clear consequences
and choices. Existing notification and pause preferences remain authoritative.

Use the existing transparent Helper-Chan artwork. Crop it into a portrait where
needed, retaining her recognizable face, glasses and large twintails. Use a
non-destructive display crop so the original full artwork remains available for
larger panels. This does not call for a newly generated or redesigned character.

## Confirmed decision 12: interface palette

Recommended: charcoal-blue dark mode with warm cream text and restrained teal
accents; offer a matching warm-paper light mode with dark ink text. Use color
alongside labels and symbols for statuses, keeping decoration away from numbers.

Alternative: stronger black-and-white manga styling in both modes, with a single
bold accent color and more prominent panel borders.

Selected option 1: the soft editorial palette, with charcoal-blue dark surfaces,
warm cream text and restrained teal accents, plus a matching warm-paper light
mode. Status information uses labels and symbols alongside color.

## Confirmed decision 13: typography and icons

Recommended: highly readable type for controls, descriptions and numbers, with
manga-inspired lettering limited to section titles and special moments. Pair
navigation and action icons with visible text labels. Use consistent numeric
alignment in financial and comparison views.

Alternative: use more expressive manga lettering throughout and rely more on
icon-only controls, with labels provided through tooltips.

Selected option 1: readable type for controls, descriptions and numbers,
manga-inspired headings, and icons paired with visible text labels. Align
numbers consistently in financial and comparison views.

## Confirmed decision 14: interface motion

Recommended: subtle, short transitions for opening panels, changing selections
and acknowledging actions, with a reduced-motion setting. Controls respond
immediately; animation must not delay actions, steal focus or change simulation
timing. Preserve the separately controlled office hyperlapse effect.

Alternative: mostly instant interface changes, reserving animation for major
milestones and story moments. Keep office motion settings independent.

Selected option 1: subtle, short transitions and action feedback, with immediate
control response and a reduced-motion option. Animation does not delay actions,
steal focus or affect simulation timing. Office hyperlapse settings stay separate.

## Confirmed decision 15: layout density

Recommended: comfortable spacing and generously sized controls by default, with
an optional compact layout for players managing larger teams and catalogs.
Density remains separate from text size; both layouts adapt to the window and
retain visible labels, keyboard access and readable progress numbers.

Alternative: compact spacing by default, showing more rows and cards at once,
with a comfortable layout available as an option. Keep the same independent
text-size and responsive-layout support.

Selected option 1: comfortable spacing and clear, generously sized controls by
default, with an optional compact layout for larger studios. Text size stays
independently adjustable; both densities adapt to the window and preserve labels,
keyboard access and readable progress numbers.

## Confirmed decision 16: action forms

Recommended: use focused in-page forms for hiring, printing, pitching and similar
tasks. Show the relevant person or series, prerequisites, costs and expected
outcome together, with one clear primary action. Reserve confirmation dialogs
for consequential commitments such as career moves or dismissals. Retain entered
values after a validation error and do not make purchases through navigation.

Alternative: use short step-by-step flows for these tasks, showing one decision
at a time and a final review before committing. Preserve the same cost clarity,
validation and confirmation safeguards.

Selected option 1: focused in-page forms showing choices, prerequisites, costs
and expected outcomes together, with one clear primary action. Keep confirmation
for consequential commitments, preserve entries after validation errors, and
never perform purchases through navigation.

## Confirmed decision 17: first visual review

Recommended: a clickable mockup of the office HUD, series workflow and Studios
page, including a representative action form and Helper-Chan notification. Use
sample data explicitly labeled as a mockup, with dark/light mode and layout
controls so the user can evaluate the design before game implementation.
This preview does not compile the game or modify career saves.

Alternative: static mockup images of the same representative screens, focusing
the first review on appearance; review navigation during later implementation.

Selected option 1: a clickable mockup of the office HUD, series workflow and
Studios page, including a representative action form and Helper-Chan notification,
using clearly labeled sample data. Provide theme and density controls.

Review materials: [design](2026-09-25-gui-redesign-design.md),
[implementation plan](../plans/2026-09-25-gui-redesign.md), and
[mockup source](../mockups/gui-redesign.html).
Do not treat mockup approval as authorization for game compilation or packaged builds.

## Confirmed follow-up 18: widescreen and ultrawide

The user liked the clickable design and requested previews in both 16:9 and
21:9, explicitly requiring support for both aspect ratios. Provide full-monitor
review captures at 1920×1080 and 2560×1080. Preserve readable text and controls;
use extra horizontal space for the office and useful side-by-side context rather
than stretching forms across the monitor. Include 3440×1440 in the later game
verification matrix. These are design targets, not a claim of verified Godot support.

The user reviewed the full-monitor previews and replied "looks good". Both
layouts are accepted as the visual reference for implementation. This approval
does not constitute a request to compile or package the game.
