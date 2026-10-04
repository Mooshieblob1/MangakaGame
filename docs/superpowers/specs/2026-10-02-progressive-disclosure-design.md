# Progressive disclosure (A3): design

Date: 2026-10-02. Status: design approved in conversation (Q53 to Q57 and
three design sections); written spec approved by the user on 2026-10-03.

Background: both fresh-player testers said the game shows too much at once and
that it is hard to find where things are (findings A3 and B6 in
`docs/superpowers/fresh-player-test-findings.md`). An inventory of day one
(2026-10-02) found nine rail items of which about four matter at first; loans,
incorporation, licence pitches, rival scouting and studio moves all offered
from the start; and a rival-studio job offer that can pause a solo doujin
artist's game from about day 90. The career goals board
(`specs/2026-10-01-career-goals-design.md`) answers "what should I aim for";
this design answers "too much on screen".

## Goal

A new player sees only what the Doujin Days chapter needs. Everything else
appears when the career reaches it, each arrival is announced, and nothing a
player has reached ever disappears. Experienced players can show everything.

## Decisions

### Q53. What comes next (decided 2026-10-02)

A3 progressive disclosure, then a Tier 1 closeout: tick checklist items only
with evidence and move the fresh-player test into Tier 2 as a playtest with
strangers from the target audience (Steam Playtest once a store page exists).
No more testers are expected for Tier 1.

### Q54. Not yet relevant means hidden (decided 2026-10-02)

Hidden until it opens, then announced: a "New" tag and one Helper-Chan text.
Rejected: visible but locked (the clutter stays), a "More" group (finding B6
gets worse).

### Q55. What opens a part (decided 2026-10-02)

Each part opens at its own moment, with the chapter that needs it as a
backstop, so side routes (contests, employment) never miss a screen. Rejected:
chapters only (screens arrive too early or late), moments only (a career that
skips a moment could miss a screen).

### Q56. Experienced players (decided 2026-10-02)

An "Experienced player: show every screen" tick box on the New Career page
(off by default) and in Settings, switchable mid-career. Rejected: no option
(returning players unlock their way back), automatic (guesses wrong on shared
computers).

### Q57. Rival job offers to Aki (decided 2026-10-02)

They wait until Industry has opened. Offers to the player's staff are
unchanged. Rejected: today's timing (tester A's interruption), never (loses a
story moment).

## What opens when

Always present: Office, Goals, Inbox, Series, Help, the menu, the header's
date, money and speeds up to 8x, and Finances with the accounts and the
part-time job.

| Part | What appears | Opens at its moment | Backstop chapter |
|---|---|---|---|
| Books | Books in the rail, print, online sales, conventions, the dashboard's book and convention buttons | The first finished book | Rookie |
| 32x speed | The 32x button | Helper-Chan's 32x introduction after the first sale | Rookie |
| Publishing | The Publishing page and the magazine section of Series details | The first ongoing series | Rookie |
| Contests | Awards (newcomer contests) | Choosing the contest route in Help (Helper-Chan's routing opens it) | Rookie |
| Staff | Staff in the rail, recruitment, team and overtime controls | Helper-Chan suggesting a hire (her routing opens it) | Serialized |
| Studios | Studios in the rail, desks and furnishing, properties, branches, career moves | The first hire | Studio Head |
| Industry | Industry in the rail, rankings, rival studios, scouting, licences, adaptations, legacy; rival job offers to Aki | Serialization or a contest result | Serialized |
| Business money | Loans, card advances, incorporation and buyouts in Finances | A missed payday | Studio Head |

A backstop chapter opens its part when that chapter begins. A page that is the
only way to reach its own moment (Awards for a contest entry, Staff for a
hire) opens through Helper-Chan's routing instead, so no moment is circular.

## Rules

- **Nothing points at a hidden part.** Helper-Chan's "Show me", a goal's How?,
  and an inbox message's or pop-up's "Open relevant controls" open the part
  they lead to before showing it (her early-hire suggestion opens Staff, the
  employment route opens Studios, a licence offer opens Industry, a missed
  payday opens Business money).
- **Nothing closes again** once opened, even if its condition stops being true.
- **Hidden means absent.** No greyed-out buttons for hidden parts.
- **Announcements.** When a part opens, its rail item (or, for parts inside a
  page, its section heading) shows a "New" tag until visited, and Helper-Chan
  texts one line saying what opened and why. These texts light the phone and
  never stop 32x, like goal texts.
- **Show every screen.** Shows every part at once with no tags or texts.
  Turning it off returns to the parts already opened. It is recorded as a
  player action so replays stay exact.
- **Older saves** open every part whose moment has already happened or whose
  backstop chapter has passed, silently (no tags, no texts).
- **Saves.** The save gains a small section: the parts opened and the show
  every screen choice. Older saves keep loading.
- **Simulation.** The only behaviour change is the timing of rival job offers
  to Aki (Q57). Balance and the economy are unchanged. Every difficulty and
  Sandbox behave the same.

## Testing

- **Unit tests:** each part opens at its moment and at its backstop chapter;
  nothing closes; show every screen on and off; older saves backfill silently;
  rival offers to Aki wait for Industry while offers to staff do not; routing
  opens a part before leading to it.
- **Playtest harness:** records when each part opened, to check the order
  across the six test careers.
- **Godot checks:** a new `--disclosure-smoke` (a new career shows only the
  day-one rail; Books appears with "New" after the first book; Helper-Chan's
  line; the tag clears on visit; a pop-up route opens Industry; the New Career
  tick box and the Settings switch). The display sweep runs with show every
  screen on, so every screen is still checked at every size and theme, and
  adds a day-one view.
- **Rendered review** of a day-one career and a fully opened one.
- **Not verifiable automatically:** whether new players now find the start
  calm and the menus findable. That needs testers (Tier 2).

## Out of scope

Header changes beyond hiding 32x at first; the duplicated zero totals on the
dashboard and header; the nightly recap and the auto-opening phone.

## Build approval

Building and running checks needs the user's confirmation at the plan handoff.
Packaging a tester build needs its own go-ahead.
