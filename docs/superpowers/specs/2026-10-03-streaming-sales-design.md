# Streaming sales and the selling tutorial, design

Date: 2026-10-03. Status: design approved in conversation (Q60, Q61 and two
design sections); this file awaits review.

## Why

A fresh player could not find the button to sell printed copies. There is
none: printed doujin sell by themselves in local shops, but only as one lump
each Monday at midnight, so for up to a week nothing visibly happens. The user
asked for sales to stream in real time and for a new player tutorial that
takes a manga from printing to selling.

## Decisions

### Q60. How sales arrive: through shop hours (decided 2026-10-03)

Each week's sales are spread across shop hours, 10:00 to 20:00 every day (70
shop hours a week), so copies tick down and money arrives in small amounts.
Weekly totals per book stay the same. Rejected: one payout each evening (still
a wait, less to watch) and around-the-clock hourly sales (the overnight skip
would hide them, and shops would look like downloads).

### Q61. Teaching selling: show it happening (decided 2026-10-03)

No new button. When the first copies go on sale, Helper-Chan says so, the Sold
count pulses and the first copy sold is celebrated; a follow-up text shows the
two ways to sell more, each with "Show me". Rejected: an explicit "Put on sale"
step every print run (a choice nearly everyone makes the same way) and adding
a "Sell more" area to the print page as well.

### Q62. Conventions the week after a release: keep week-two demand (decided 2026-10-03)

A book released midweek sells its first week in the shops at once, so a
convention the following week sees the smaller week-two demand (about 40% of
week one). Kept: the first week's buyers already bought in shops, and new
players are usually limited by stock rather than demand. Rejected: treating a
short first week as not counting (stronger early conventions, slightly higher
lifetime sales).

### Approach: plan the week, sell by the hour

Rejected: recomputing demand every hour (fan numbers shift within the week, so
totals drift from the tuned balance, and every book's formula runs every
hour), and animating the Monday lump on screen only (money and stock would be
shown wrongly for most of the week).

## What changes in the simulation

- **Planning stays on Monday at 00:00**, with the same formulas in the same
  order as today: each released book's potential, its channel split, the
  weekly shop demand (`WeeklyDemand`, 30% of it for local shops) and its
  sales week count. What used to be sold at once becomes a **sales plan**: a
  quota to release evenly over a number of shop hours.
- **Every shop hour** (hours starting 10:00 to 19:00, every day) each plan
  releases its share: the whole copies due so far (total times hours done,
  divided by hours planned, rounded down) minus what it has already released,
  so fractions carry forward and nothing is lost to rounding.
  - **Local shops:** due copies sell from delivered stock exactly as today
    (oldest print run first, the presentation penalty, reserved convention
    copies untouched), limited by stock on hand and by what is left of the
    week's demand. Copies due while there is no stock are missed customers, as
    a Monday with no stock is today. Stock that arrives midweek starts selling
    in the next shop hour.
  - **Online downloads and publisher digital and overseas deals:** due units
    are sold and paid as they are released; that week's receipt is created at
    planning time and fills up hour by hour.
  - **Publisher volumes:** copies sold, reprint milestones ("goes back to
    press"), the 100,000 and 1,000,000 milestones and fan growth update
    hourly. Royalty money stays tied to print runs, as today.
  - **Fans** grow per copy or unit as each share is released.
- **A book that goes on sale midweek starts selling at once.** Its first week
  (the same full first-week demand today's first Monday would give) is planned
  at release and spread over the shop hours left that week, or over at least
  30 shop hours, continuing into the next week if needed. Its later weeks are
  planned on Mondays as today, so its total is unchanged and its sales simply
  start up to a week earlier. More than one plan can run for a book at once.
- **The sales window** (4, 8 or 52 weeks) closes only when its last planned
  week has finished releasing.
- **Conventions** still settle when the event ends and still draw on the same
  weekly demand; copies the shops have already sold that week are no longer
  available to the event, and the other way round.
- **Unchanged:** the monthly "Last month: X copies" recap, convention recaps,
  promotion reach (it still decays on Monday before planning) and all random
  draws (sales use none).

## Money records

- The account ledger keeps **one line per title, reason and day**: an hourly
  sale adds to that day's line instead of adding a line, so saves do not grow
  by thousands of entries a year.
- The header's money change still flashes "+" for each hourly sale: the header
  feedback reads changes to the latest line as well as new lines.

## Saves and replays

- Each book gains its open sales plans (total, released, hours planned, hours
  done, and which kind of sale they release). The new data is optional in the
  save, so older saves load.
- An older save picks up streaming at its next Monday: books whose Monday lump
  has already been paid have nothing planned until then, and a book released
  but not yet sold under the old rules gets its first week at that Monday, as
  it would have before. Nothing is lost or paid twice.
- Sales use no random draws, so replays stay exact. The save checks that read
  "settled on Monday" (the receipt week, the last sales time) keep their
  Monday keys.

## The selling tutorial and texts

- **When a book's first copies go on sale** (first print run delivered and in
  shops, or the first online release), Helper-Chan texts: "Your books are in
  local shops now, and they sell by themselves! Shops are open 10:00 to 20:00,
  so watch Sold in the top bar." For an online-only first release: "Your
  download is on sale now!" Her "Show me" highlights the Sold count in the top
  bar and on the series. The texts appear when stock reaches the shops, not
  before.
- **The Sold count pulses** gently when a book's first copies go on sale.
- **The first copy ever sold** gets a "First copy sold!" bubble by the Sold
  count, in the style of the work feedback bubbles, alongside the money flash
  and the first-sale good-news music.
- **A follow-up text** shows how to sell more: online downloads (no upfront
  cost, "Show me" opens Sell online) and conventions ("Show me" opens
  Conventions). Neither link opens a part the career has not reached (the
  progressive disclosure rules apply).
- **Texts that say sales settle on Mondays are reworded**: Helper-Chan's sell
  and online texts, the goal tips, the Sell online page and the distribution
  summary ("Next sales check: Monday" becomes "Selling now · shops open 10:00
  to 20:00").
- **Reduced interface motion:** the pulse becomes a steady highlight and the
  bubble becomes a line in the notice bar.

## Testing

- **Unit tests:** hourly release adds up exactly to the weekly plan; nothing
  sells outside shop hours; stock limits and missed customers; midweek stock
  arriving; a midweek release starting at once with an unchanged total; the
  minimum 30-hour spread; sales windows closing after their last week;
  conventions and shops sharing weekly demand; download receipts filling
  hourly; reprint and copy milestones firing once; daily ledger lines; older
  saves picking up at Monday without double payment; replays staying exact;
  the tutorial texts' timing and the reworded texts.
- **Existing tests** that expect one Monday lump (sales, timeline, doujin
  distribution, production clarity and others) are rewritten for hourly
  sales; each change is recorded as a ruling.
- **Playtest harness:** the seven playtests rerun and their money and copies
  tables are compared with today's; any shift is recorded.
- **Godot:** a smoke check for live Sold and stock changes during shop hours,
  the pulse, the first-sale bubble, both "Show me" routes and reduced motion;
  the journey smoke rerun end to end; the full suite and the display sweep.

## Out of scope

- Busier weekends or evenings (all shop hours sell equally for now).
- Changing any sales formula, price or royalty rule.
- Per-shop stock or choosing which shops stock a book.
