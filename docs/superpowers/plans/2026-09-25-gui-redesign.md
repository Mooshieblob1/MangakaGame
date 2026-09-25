# Sub-project 9: Overall GUI redesign — implementation plan

Date: 2026-09-25.
Status: source implementation authorized and delivered on 2026-09-25.
The subsequent explicit build request authorized compilation and packaging.
Local checks passed and alpha.6 is packaged; see [build verification](../sub-project-9-build-verification.md).
Wider human acceptance and hardware checks remain separate.
Implementation record: [source delivery](../sub-project-9-source-implementation.md).
Design: [GUI redesign](../specs/2026-09-25-gui-redesign-design.md).
Decisions: [17 confirmed choices](../specs/2026-09-25-gui-redesign-considerations.md).

## 1. Review the representative design

- Deliver the clickable mockup with real transparent Helper-Chan artwork.
- Check Office, both sidebars, Series, Studios, print form, reservation form and
  important-message presentation. Expose theme, density and larger text.
- Clearly identify sample prices/states and routes outside this prototype.
- Review full-monitor layouts at 16:9 (1920×1080) and 21:9 (2560×1080), with
  bounded forms and wider office space; preserve image proportions and text sizes.
- Record requested revisions in the design before propagating its style.

The visual review is approved. The subsequent instruction to implement authorized
game source changes. The existing hold on all compilation remains in force.

## 2. Establish components and route coverage

- Inventory current player actions from management-ui-actions.md and the current
  source, including feedback-era online sales and convention reservation commands.
- Map each to its new destination, selection context, authority and empty state.
- Introduce a shared Godot theme/token layer: light/dark surfaces, typography,
  spacing/density, icons, focus, disabled states, progress, cards and forms.
- Add consistent shell/page interfaces and presentation preferences with defaults
  for existing saves. Avoid a wholesale replacement of simulation/controller code.
- Preserve the debug scene and source-only dashboard work while moving reusable
  location cards, floor-plan previews and selectors into the new components.

Reviewable outcome: shared components and action coverage map, with no lost route.

## 3. Replace player-facing navigation and HUD

- Persistent left rail and one full management workspace; remove duplicate
  workbench tabs from the player shell.
- Two-row HUD using existing queries, explicit account/title scope, readable
  numeric progress, publishing status, fans, sales, stock and reservations.
- Office stays one click away; office-side Inbox/Series and full-detail routing
  preserve camera, selection, filters and back history.
- Retain speed highlighting/feedback, time controls and all focus/modal guards.
- Replace hard-coded tab-index navigation and guidance targets with stable routes.

Reviewable outcome: coherent shell and navigation, including no-series/empty states.

## 4. Deliver Series and Books as the first complete workflow

- Series cards/details, lifecycle strip and state-derived primary action.
- Make one-shot/issue/collection and pitch/offer/contract states explicit.
- Focused create, production, pitch, offer, print, online and convention forms.
- Actual pricing and eligibility, explicit paying account, delivery vs stock,
  reservation protection and clear error recovery.
- Advanced controls remain available, with remembered selection and inputs.
- Preserve cover/showcase/import routes and one-shot continuation behavior.

Reviewable outcome: complete start-to-first-sale and publisher-offer paths through
the new pages using existing commands, without old workbench detours.

## 5. Staff, finances and Studios

- Portrait-led staff and recruit views, comparison table, skill visibility,
  individual details, assignments, schedules, pay and recruitment actions.
- Account-separated finance overview, true ledger charts, obligations, reserved
  wages, team budget limits, loans, repayments, investment and outside jobs.
- Studio cards, actual floor-plan previews, map and property comparisons.
- Furniture integration, full move quote, branches, location closure and career
  transitions with their existing confirmations and draft guards.
- Keep Helper-Chan out of capacity, staff totals, payroll and simulation output.

Reviewable outcome: routine management can be completed without hidden selections.

## 6. Industry, Inbox, Helper-Chan and remaining menus

- Magazine-inspired Industry overview connected to all existing rankings,
  publishers, scouting, awards, adaptations, licensing and channels.
- Priority inbox plus chronological view; group routine updates without losing
  history or confusing acknowledgement with decision resolution.
- Compact transparent portrait guidance and larger important/story panels.
- Preserve preferences, pending narrative choices, journal, guidance targets and
  important-event pause behavior.
- Bring main menu, career setup, save/load, settings and feedback export into the
  same visual system. Maintain Sandbox achievement restrictions and difficulty.

Reviewable outcome: consistent styling and routing across all player surfaces.

## 7. Validate when compilation is authorized

Run focused checks appropriate to the changed routes. Reuse existing smoke flows
and stable action IDs; add only missing behavior coverage, not implementation-
mirroring tests. Track source-only versus executed checks explicitly.

| Area | Required checks |
| --- | --- |
| Navigation | Full page and office sidebar entry; back, camera and filters; guidance shortcuts; furniture Apply/Discard guard. |
| Inputs | Escape releases entry, later Escape closes safely; Space/1/2 and WASD cannot affect gameplay while typing or in blocking dialogs. |
| Forms | Correct person/title/account; quote refresh; insufficient funds, expiry and authority rejection retain entries; no mutation on preview. |
| Publishing | Self-published vs publisher deadlines; one-shot/issue/collection; offer received vs signed; stock, downloads, pending deliveries and reservations. |
| Career | Owner and employed-lead authority; career move/save-load invalidates stale IDs; historical book ownership and budgets remain correct. |
| Inbox | Read vs resolved; deferral; expired offers; grouped messages; no duplicate popup replay after load. |
| Layout | 16:9 at 1280×720, 1600×900 and 1920×1080; 21:9 at 2560×1080 and 3440×1440. Both themes/densities, larger text, long names, empty/large catalogs, office with sidebar, bounded form widths, and no stretched art or clipped actions. |
| Motion/focus | Reduced motion; no animation input delay; stable focus/dropdowns/scroll during running simulation. |
| Persistence | Old presentation defaults, restored valid context, corrupted/missing optional artwork fallback; saves retain authoritative history. |

Use rendered screenshots of representative states and live input checks after
development compilation is permitted. Run relevant simulation regressions if
command wiring, save presentation or lifecycle changes justify them. Do not call
the HTML prototype evidence of Godot rendering or simulation correctness.

## 8. Complete and package only on instruction

- Update the action inventory and record actual checks, defects and limits.
- Obtain feedback on the implemented GUI and resolve defects in agreed scope.
- Produce a completion record linking the approved design, final pages and evidence.
- Export/package only when explicitly requested. No new release is implied by a
  source implementation, mockup review or development screenshot.
