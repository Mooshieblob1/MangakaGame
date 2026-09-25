# Sales clarity, convention reservations and office life

Originally source-only work, 25 September 2026. The user subsequently requested
compilation and packaging. These changes are included in alpha.6, with local
validation recorded in [build verification](sub-project-9-build-verification.md).
The earlier compilation hold applied to the source-development phase.

## Sales and navigation

- The selected series' top strip shows fans, total copies sold, delivered
  physical stock and reserved convention copies. The Series list and details
  distinguish downloads and stock available for ordinary sales. Reservations
  include paid orders arriving before the event; the stock total includes only
  delivered copies.
- The unread-count button opens Inbox without marking messages read.
- Continue as ongoing series retains the same title record, genre, readership,
  artwork references, published one-shot, stock and sales history. New finished
  chapters become numbered issues. A pitch remains a separate action.
- Books → Sell online publishes a finished doujin edition immediately, without
  printing or setup fees and without buying a studio internet connection.
  Download price is half the printed cover price. A 30% shop fee is deducted per
  purchase; normal creator-share rules still apply to net contribution.
- Monday settlements depend on actual units bought, quality and current genre
  popularity. An older listing keeps a small backlist audience after its physical
  sales window ends. No sale means no payment. Each new issue must be listed;
  a one-shot listing does not silently include future chapters.

Day-one access, the small initial online audience, price and fee are deliberate
gameplay choices, not claims about April 1996 commerce. A first-person EISYS
executive interview dates DLsite's predecessor Soft Island to July 1996:
[4Gamer interview, 2015](https://www.4gamer.net/games/291/G029176/20150514103/).
The user's explicit decision was to make this route available from day one.

## Conventions

Booking can reserve a chosen quantity from delivered stock or paid print orders
due before opening. Local sales and other bookings cannot consume those copies.
Reservations can be edited until the attendee departs. Cancellation or completion
releases remaining stock; reservations do not guarantee demand or booth capacity.
Stock from reserved editions is offered first at the chosen convention.

Fictional neighbourhood events occur on Wednesdays/Sundays with two days' notice
(2–5 days away). Regional events use Sundays (2–8 days away). Major summer/winter
dates retain the existing calendar. These more frequent local events are pacing
choices, not historical event listings. Existing saved bookings keep their dates.
Save version 10 imports version 9 with a replay checkpoint so past bookings are
not reconstructed using the new calendar.

## Speed feedback and office presentation

The selected time control stays highlighted. Speed changes, including keyboard
shortcuts, briefly display a large translucent icon which fades and does not
intercept input.

Staff departure/arrival paths lead through the entry doorway. Departing characters
remain visible until reaching the threshold, and their exposure trail is cleared
in the same update. Returning staff wait for the door to open. Pause freezes
movement. Loading or changing location restores current presence instead of
replaying historical travel.

Helper-Chan has walking and seated poses, accompanies the protagonist's trips and
breaks, and cycles through writing notes, adjusting her hair and checking her
clipboard at her own desk. Her name label moves with her.

The shared break room has a counter, sink, kettle, microwave, fridge, shelves,
table and chairs. A wall with an opening separates it from the corridor. Both
outer doors use a rigid 1.2 m leaf that rotates into its recess, opens for traffic
and closes when clear. Routes use the corridor and openings rather than walls.

Occasional decorative breaks belong entirely to the office renderer. They do not
advance the simulation, consume work hours, alter production progress, charge
wages or affect needs. Existing simulation-managed breaks still operate normally.

## Verification boundary

Before the no-compilation restriction, the sales/convention changes passed 535
simulation tests, a focused repeat of 9 new simulation tests, and 30 rendered
checks covering online sales, continuation, reservations, Inbox, the counters,
speed feedback, saved UI restoration and 1280×720 layouts. Screenshots are in
`TestResults/convenience-*.avif`.

The later office movement, gestures, break-room and door work has not completed
live verification. Additional genre-demand/reservation tests and the immediate
release of reservations on career transfer are also unrun. Automatic approval
review rejected the planned development compilation under the user's no-build
instruction; the user then confirmed that all compilation should wait.

Prepared checks for a future authorized run:

- `PublishingConvenienceTests`, then the normal non-LongRun simulation suite.
- `--office-life-smoke --capture`: desk gestures, decorative break invariance,
  companion visits, paused/1×/8× departure and return, door dimensions and ambient
  circulation. This check is written but has not run.
- `--convenience-smoke --capture` and the existing production/usability checks.
- Inspect the new room at both supported test window sizes and multiple office
  sizes, including uninterrupted paths around the table and corridor wall.

Do not export or package a new candidate without a separate user instruction.

## Follow-up: chibi legs and seating

Shortened both segments of the ordinary cast's legs, lowered their standing torso
and head to keep feet grounded, and matched the hyperlapse silhouette. Helper-Chan
now has compact standing legs and a separate short bent-leg seated mesh. Seated
pelvis/skirt placement derives from the cushion surface rather than sinking the
torso toward the floor. A forward pose offset keeps the short calves clear of the
cushion edge; feet dangle. Desk and break-room seating use their respective
heights, and a companion standing beside another activity does not sit in midair.

Source review only. No compilation, render checks or build were run for this
follow-up. Verify front/side seated views, writing and hair gestures, walking,
both chair types and 8× exposure when the user authorizes checks.


## Approved development preview: chibi seating

The user subsequently authorized development compilation and screenshots (no
package). Debug compilation succeeded and `--office-life-smoke --capture` passed
671 checks. Captures include `TestResults/office-life-chibi-seated.avif` and
`TestResults/office-life-shared-break.avif`. The user accepted the proportions and
mouthless appearance. This permission applied to that preview.

## Follow-up: studio management dashboard

Replaced the single operations list with Overview, Locations, Team, Money,
Publishing, and Career sections. Overview shows business cash, monthly rent,
staff versus usable desks, unpaid bills, and location cards. Each location has a
small floor plan drawn from its actual furniture placements, capacity indicator,
and direct viewing/furnishing buttons. Team has need and morale bars. Relevant
sections expose employee and series selection directly, with named input fields,
property costs, print quotes, guided book/convention shortcuts, and disabled
empty-state actions with explanations. Existing simulation commands and career
confirmation are retained. Cards use the existing light/dark palette and wrap on
smaller widths; changes/errors also appear in the workbench footer.

Source review and whitespace checks only for this dashboard change. No further
compilation, in-game verification, export, or packaging was performed. On the
next authorized preview, inspect all six sections at 1280x720 and larger text,
light/dark themes, multiple locations, empty books/debts, and employee/series
selection before running the existing operations smoke flow.
