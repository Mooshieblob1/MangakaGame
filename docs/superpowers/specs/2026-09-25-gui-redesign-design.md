# Sub-project 9: Overall GUI redesign

Date: 2026-09-25.
Status: visual direction and 16:9/21:9 review layouts approved by the user on
2026-09-25. Source implementation delivered after the user instructed implementation.
The user subsequently authorized compilation and packaging. Local checks and
Windows alpha.6 packaging passed; see [build verification](../sub-project-9-build-verification.md) and the
[source implementation record](../sub-project-9-source-implementation.md).
Requirements: [17 confirmed decisions](2026-09-25-gui-redesign-considerations.md).
Delivery sequence: [implementation plan](../plans/2026-09-25-gui-redesign.md).
Visual review: [clickable mockup](../mockups/gui-redesign.html).

## Outcome

Make Mangaka Studio feel like one coherent manga-management game. The player
should identify current work, notice decisions, understand money and release
status, and reach an action without navigating a second set of workbench tabs.
Keep the simulation, balance, rights, saved careers and current capabilities.

The accepted visual direction is polished and manga-inspired: readable controls,
quiet editorial detail, illustrated accents and clear data. Exact spacing, colors,
font choices and component geometry in the mockup are review proposals, not
additional confirmed requirements. No new mechanics or balance changes belong
in this milestone.

## Application shell

A persistent left rail contains Office, Series, Books, Staff, Finances, Studios,
Industry, Inbox and Help. Settings/save/menu access remains consistently reachable.
Each management area opens a full page inside this shell; remove duplicate
workbench navigation from the player interface. Keep the developer harness.

Office is always one navigation action away. Inbox and Series can also appear
beside the office through explicit local shortcuts. The unread control opens
the inbox sidebar when viewing the office and the full Inbox elsewhere. A sidebar
can open the relevant full details page. Returning restores the office camera,
selection and management context. Merely visiting a page neither changes scope
nor pauses the simulation.

Use two compact HUD rows:

| Row | Contents | Rules |
| --- | --- | --- |
| Global | Date/time, selected speed, business and personal funds, fans, unread shortcut | Clearly label account ownership and money that is actually available to spend; maintain current speed highlights and brief speed-change overlay. |
| Selected series | Title selector, current work, numeric progress bar, publishing status, total sold, physical stock, reserved copies | Resolve every figure from the selected title. Provide download/physical breakdown and available stock in details. No series gets a clear empty state. |

The current fans query and its scope must be retained or explicitly labeled;
do not silently sum duplicate readers into a new unique-fan statistic. The mockup
uses an illustrative count. Explain global versus series-scoped values in the
implemented HUD and its tooltips.

On narrow windows, wrap groups without truncating the meaning of costs or statuses.
Do not shrink text to force a single line. Start with the game's current 1280×720
minimum review size and 1600×900 default; the browser mockup also reflows below
those sizes for conversation use. Its small-screen arrangement is not a new
mobile platform commitment.

Support both **16:9 and 21:9** as first-class desktop layouts. Review at 1920×1080
and 2560×1080, and verify 3440×1440 during authorized game checks. The left rail,
text and controls retain sensible sizes. Wider monitors provide more office
viewport space and room beside a contextual sidebar; they do not stretch the
office image, portraits or text. In Godot, preserve the intended vertical office
framing and allow the camera to show additional horizontal space.

Management pages use a centered, bounded reading area. Focused forms keep a
bounded input column and adjacent quote/details column; extra monitor width is
not a reason to make quantity fields or paragraphs arbitrarily long. Longer
pages scroll within their content area while the HUD/navigation remain available.
Height and UI scale matter as well as aspect ratio: do not assume ultrawide means
more vertical space. Office plus an open sidebar must retain accessible controls
and guidance at both ratios. Full-monitor browser captures use the companion
`gui-desktop-preview.css`; its layout dimensions remain review proposals.

## Page pattern and navigation state

Each page begins with a short title, useful summary values and a primary action.
Cards or compact rows open focused details. Advanced controls expand locally.
Preserve selected IDs, filter, period, scroll and back history when moving between
overview, detail and actions. Reset invalid context on save load, title removal
or career moves. Revalidate authority against current state before submitting.

Do not keep an undocumented selected employee/title in another workbench tab.
Action forms display their target and provide an appropriate selector where the
target can change. Opening a quote or choosing a row does not spend money,
consume RNG, apply a command or alter production. Forms retain inputs after
validation failure and put the error beside the responsible field.

## Series and Books

Series overview presents cover/title, lead, genre, format, work progress and
publication status. A visual workflow connects production, release and readers.
This is a navigation aid, not a compulsory sequence: ongoing chapters, online
sales and print stock can exist at the same time.

Differentiate one-shot, short numbered issue and collected edition everywhere.
One-shot completion means one completed story. A short-issue series can release
each chapter; a collected book requires five completed chapters. State readiness
against the chosen edition rather than an ambiguous general "remaining chapters."

Use existing publishing outcomes: preparation, being pitched, editor work,
awaiting decision, offer received, signed serialization, and the applicable
rejected/declined/expired/withdrawn/cancelled outcome. Offer received is distinct
from a signed contract. Show real publisher deadlines only where applicable.

The primary action is derived from a known available action, such as printing a
finished issue or reviewing an offer. Never invent eligibility or predicted
acceptance. Keep alternate routes and ordinary actions accessible.

Books collects edition-level printing, online listings and distribution. Print
forms show edition, pages, tier, copies, full quote, account paying, expected
delivery and the effect on stock. Orders do not count as delivered copies.
Online listings state no upfront fee, sale price and deductions on actual sales.
Convention forms show date, attendees, travel, booth cost and available/reserved
stock together. Preserve eligible future delivery reservations and cancellation
release semantics. One-shot continuation retains the existing title and readers.

## Staff, finances, studios and industry

| Area | First view | Deeper controls |
| --- | --- | --- |
| Staff | Portrait cards with role, current work, strongest known skills and wellbeing | Compact comparison view for staff/recruits; schedules, roles, assignments, pay, recruitment and departures in focused details. Do not expose hidden candidate talent. |
| Finances | Separate Business and Personal summaries, income/spending, obligations and cash flow | Account-specific ledger, financing, investment, outside work and incorporation. Distinguish operating flow from transfers; retain wage reservations and employer budgets. Forecasts, if any, are labeled estimates. |
| Studios | Location cards, actual floor-plan previews, rent, staff/usable desks, storage, View/Furnish | Compare property tradeoffs and full move quotes; Tokyo map, branches, staff moves, closure and career routes. Preserve furniture draft, Apply/Discard and navigation guards. |
| Industry | Magazine-inspired sections with covers, current rankings, publishers, awards, adaptations and news | Readable comparisons and eligibility, scouting/recruitment, distribution and licensing. Show only era-appropriate unlocked/discovered information. |

The recent source-only Studios dashboard is reusable groundwork, not an already
validated replacement for all these pages. Its existing operations must be mapped
to deliberate destinations rather than copied wholesale into Studios again.

## Inbox and Helper-Chan

Inbox starts with Needs attention, then Results and milestones, then Routine
updates. Include an All messages chronological view. Group repeated routine
updates while retaining their individual history. Unread is a reading state;
unresolved is a decision state. Marking read never accepts or resolves anything.
Refresh expired or already-resolved items before offering their actions.

Use the existing transparent Helper-Chan neutral artwork with a non-destructive
portrait crop for compact guidance. Preserve her glasses, face and recognizable
twintails; larger important-message panels can show more of the original image.
No new character art is required. Keep the portrait and dialogue separate from
the choice buttons so larger text can reflow without covering her face.

Routine tips use a compact edge card. Important decisions and story events use a
larger illustrated panel with a short summary, consequences and choices. Preserve
existing notification priorities, tutorial/story preferences and auto-pause
settings. Do not add extra popup interruptions to routine inbox news. "Keep in
inbox" defers the message without resolving its decision. Furniture drafts remain
protected when a message arrives.

## Shared visual system

- Charcoal-blue dark surfaces, warm cream text and restrained teal accents;
  matching warm-paper light surfaces with dark ink text.
- Readable body type and tabular numeric alignment; manga-inspired headings and
  restrained rules/page motifs. Confirm any bundled font license before adding it.
- Icons paired with visible labels; status text or symbols alongside color.
- Comfortable spacing by default, optional compact density. Text size is
  independent of density. Controls and headings remain legible at larger text.
- Short, nonblocking transitions for selection, panels and feedback. Reduced
  motion disables nonessential movement. Office hyperlapse remains independent.
- Accessible focus, stable tab order, appropriate disabled reasons, clear errors,
  and sufficient contrast in both themes. Avoid hover-only essential information.
- Escape first releases text entry; subsequent Escape follows existing safe
  dismissal/back behavior. WASD, middle-drag, Space and speed shortcuts retain
  their typing/modal guards.

## Implementation boundary

Use Godot-native controls and the current simulation command/query interfaces.
The browser mockup is a review tool, not an embedded browser replacing the game.
Separate shell/navigation, common visual components and page controllers from
the existing large DebugMain partials incrementally. Keep IDs stable for guidance
links and preserve a traceable route for every action in the
[action inventory](../management-ui-actions.md).

Presentation preferences may evolve compatibly without rewriting game outcomes.
Selection and preview reads must remain deterministic and side-effect-free.
Avoid rebuilding whole pages every simulation tick: update values in place to
preserve focus, dropdown state, form entries and scroll. A career/state replacement
must invalidate cached references and pending quotes.

## What this first mockup demonstrates

- Left rail, two-row HUD, Office and its Inbox/Series sidebars.
- Series workflow with self-published and offer-received sample states.
- Studios cards and an illustrative property comparison.
- Editable sample print quote, validation, a pending delivery after ordering,
  and sample convention reservation updates.
- The actual transparent Helper-Chan asset in a small portrait and larger panel.
- Theme, density and text-size switches; reduced motion through review controls.

All figures, titles, offers and property comparisons are illustrative. The office
background is an existing development screenshot, not a newly compiled render;
it does not react to prototype time controls. The portrait is losslessly encoded
for embedding and cropped only by layout. Mockup transactions affect local sample
state only. Staff, Finances and Industry are explicitly disabled in this first
prototype. Offer terms and furniture editing use explanatory previews; they are
not implemented game interactions here. Full production settings, map, main menu,
all save routes and all career variants remain implementation work.

## Acceptance before completion

Visual approval is followed by implementation and, only when authorized, checks
in the actual Godot application. Review both themes/densities, larger text,
keyboard navigation and empty/large catalogs. Exercise complete create → produce
→ release → sell paths, offer review, hire/assign/pay, property/furniture flows
and save/load/career transitions. Check financial quotes and both authority modes.
No simulation rebalance or historical data changes are planned. If factual Tokyo
content needs changing, use the existing research ledger or verify the new claim.

Compilation and packaged builds remain separately gated by the user's instruction.
Mockup interaction checks do not qualify the game build or pending dashboard code.
