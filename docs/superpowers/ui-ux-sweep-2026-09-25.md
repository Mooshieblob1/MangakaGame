# UI clarity and usability sweep

Source implementation after alpha.9. The user subsequently authorized compilation and packaging; see [alpha.10 verification](alpha-10-build-verification.md) for executed checks and the final package.

## Review

Reviewed the approved GUI direction, current Godot screens and prior captured office/finance views. The principal issues were weak distinction between primary actions and secondary controls, repeated series details, ambiguous physical/online sales figures, unhighlighted finance filters, and long histories ahead of useful actions. One office sales shortcut incorrectly led to magazine publishing instead of the Books overview.

## Changes

- Shared cards have consistent outlines and padding, smaller section labels, quieter secondary text and larger metric values. Primary actions have a distinct blue treatment, including hover/pressed states. Navigation is left aligned. Styles follow dark/light themes and comfortable/compact settings.
- The office dashboard puts the selected title's next action in its current-series card. Self-publishing sales opens Books; magazine pitches retain their separate route and an explanatory workspace heading. Empty careers hide unused progress/sales figures and explain why convention booking needs a title. Aggregate fan totals explicitly describe their scope. Dashboard buttons wrap at narrower widths and larger text sizes.
- Series details show publication status, current work and release/readership figures without repeating the same production bar. Stage breakdowns, older chapters/rankings and promotion extras expand on demand. One-shot continuation and short-issue options remain reachable. Expanded sections survive refreshes and are reset with a career session.
- Books distinguishes delivered stock, stock available after reservations, physical sales, downloads, pending print deliveries and online listing status. Pending reservations are not presented as delivered stock. Counts update in place; distribution explanations expand locally. Printing quotes name the paying account and show the order total first.
- Finance account/period controls show the active choice. Account naming matches the HUD's Personal savings / Doujin budget / Business funds / Employer funds rules. Period totals and eight recent transactions precede optional charts and outside-work controls. Signed amounts have dedicated aligned columns and theme-aware colours. Full ledger access is preserved. Snapshot period figures display their refresh time.
- Each HUD money total opens its own finance account by click or Enter, with visible keyboard-focus colour and the existing furniture-edit guard.
- Staff details group the portrait/activity/pay, actions, wellbeing bars and known skills. Hidden talent stays hidden. Staff comparison is expandable, and activity text distinguishes off-duty and other scheduled states from available work.
- Inbox reading controls explain their scope and disable when there is nothing unread. Reading a historical message is labelled separately from the live decision cards. Back and close controls have clearer labels or tooltips.

## Original source-only verification boundary

- Checked C# source syntax with the local parser and relevant controls against the installed Godot API documentation.
- Updated production/usability smoke routes for renamed printing actions and expandable funding controls. Added management checks for the Books shortcut, empty states, selected account/period, preserved disclosure state and unchanged simulation state.
- No compiler, Godot runtime, new screenshot capture or packaging was run. Existing screenshots informed the review; they are not previews of this revision.
- Next authorized build should run management, production, usability, money feedback and family-home checks. Visually review dark/light themes, normal/large text, compact/comfortable density, and 1280x720, 1920x1080, 2560x1080 and 3440x1440 layouts. Alpha.9 remains the last built package.

## Fixes found during authorized engine checks

- Convention booking keeps all four inputs, the date/cost/stock quote and confirmation together at 1280x720. The longer attendance and reservation explanation expands below.
- Sidebar headings stay on one line; the compact sidebar uses a refresh icon with its existing explanatory tooltip.
- On short windows with large text, Helper-Chan's persistent guidance card becomes compact to preserve room for the management page. The next-step action remains visible and full guidance stays in Help.

## Follow-up after alpha.10: studio selector

The navigation selector no longer clips the active studio name or combines it with the ward in a narrow fixed-width control. Its minimum width follows the selected name and chosen text scale; the ward is displayed underneath and included in the tooltip. Changes to the rail's minimum width reposition the floating panels. Source review and whitespace checks passed; compilation, fresh screenshots and packaging are deferred until requested. The existing alpha.10 package is unchanged.

## Follow-up after alpha.10: total production progress

The series sidebar, office dashboard and current-work strip now share a total chapter progress bar. Storyboard, pencils, inks, backgrounds and tones each have a distinct colour. Segment widths reflect required work; completed work remains filled when the next stage begins. A centred total percentage, compact stage legend and detailed tooltip explain the colours and remaining work. Empty dashboards hide the legend along with the bar. Completed or skipped stages count toward completion.

Source syntax and the installed Godot control APIs were checked without compilation. Updated usability and production smoke checks await the next authorized engine run. No new build or screenshots were produced.

## Follow-up after alpha.10: shorter top bar

The HUD now has dedicated compact styles for speed controls, unread messages, Save and the series selector. Buttons use a 32-pixel minimum height and stay vertically centred instead of stretching to the money cards' height. Hover, selected speed, keyboard focus, disabled overnight controls and light/dark themes retain their own states.

Account names sit beside their balances, with the existing green income and red spending figures on one reserved line underneath. Captions and balances retain their previous font sizes and finance shortcuts. Account-card vertical padding is reduced to 3-4 pixels and the outer header padding to 6 pixels. Tighter row spacing, a shorter date/time block and compact progress-strip controls reduce the full header height while allowing narrow windows and larger text to wrap. Year, clock, current speed, fan/copy counts and production progress remain available.

Source syntax, whitespace and installed Godot API checks only. Exact rendered heights, narrow-window wrapping and large-text layouts need the next authorized compilation and visual run; no new build was made.
