# Progressive disclosure (A3), completion

Date: 2026-10-03. Design: [spec](specs/2026-10-02-progressive-disclosure-design.md)
(Q53 to Q57). Plan: [implementation plan](plans/2026-10-03-progressive-disclosure.md).
Fixes findings A3 and B6 from the fresh-player test. Not yet committed.

## What changed

- **Parts that open as the career reaches them** (`src/MangakaSim/Disclosure.cs`,
  `GameState.Disclosure.cs`). A new career shows Office, Goals, Inbox, Series,
  Finances (accounts and the part-time job), Help and speeds up to 8x. Books,
  32x, Publishing, Contests, Staff, Studios, Industry and Business money
  (loans, card advances, incorporation, buyouts) open at their own moment or
  when their chapter begins, and never close again. What has opened is saved;
  opening a part from navigation is a recorded action, so replays stay exact.
- **Nothing points at a hidden part.** Going to any page opens its part first:
  Helper-Chan's "Show me", a goal's How?, inbox and pop-up routes and page
  links. Choosing the contest route opens Contests and the employment route
  opens Studios. Clicking your own character and the pay screens open nothing.
  Hidden parts are absent, not greyed out.
- **Announcements.** A newly opened part gets a "New" tag on its rail item (or
  on the Finances and Series details buttons) until visited, and one line from
  Helper-Chan; these texts never stop 32x. The rail refreshes at once, even
  while paused.
- **Experienced player: show every screen.** A tick box on the New Career page
  and a switch in Settings; turning it off returns to the parts already opened.
- **Rival job offers to Aki** wait until Industry has opened (serialization, a
  contest result, or show every screen). Offers to staff are unchanged.
- **Older saves** open every part they have already reached, silently.

## Order parts opened in the playtests

| Part | Seed 0 Standard | Seed 1 Standard | Seed 7 Relaxed |
|---|---|---|---|
| Books | 6 Apr 1996 | 6 Apr 1996 | 6 Apr 1996 |
| 32x and Publishing | 8 Apr 1996 | 8 Apr 1996 | 8 Apr 1996 |
| Industry (serialization) | 2 May 1996 | 1 Jun 1996 | 2 May 1996 |
| Staff and Studios (first hire) | 22 Aug 1996 | 5 Aug 1996 | 22 May 1996 |
| Contests (Rookie chapter) | 4 Aug 1996 | 4 Aug 1996 | 4 Aug 1996 |
| Business money | 6 Oct 1997 | 1 Mar 1998 | 12 Nov 1997 |

## Verification

- Unit tests: 730 pass, including the parts catalogue, opening at moments and
  chapters, never closing, show every screen, older saves (a doujin career and a
  mid-career save), rival offers to Aki and to staff, Helper-Chan's
  announcements and the final review fixes, each written to fail first.
- Godot: new `--disclosure-smoke` (13 checks: the day-one rail and no 32x, the
  32x shortcut before the first sale, Books with a New tag and Helper-Chan's
  line, the tag clearing on a visit, loans hidden in Business actions, your own
  character not opening Staff, the print page hiding digital deals, the contest
  route opening Contests, a pop-up route opening Industry with the rail
  refreshed, Finances and Series details hiding their parts, the New Career
  choice and the Settings switch). All 21 Godot checks pass, warning-free
  build; the display sweep runs with every screen shown plus a day-one view
  (560 screen checks, 0 flagged).
- Playtest harness: all seven playtests pass, with the order parts opened in
  their reports (table above).
- Rendered review: the day-one view at 1280 x 720 with 150% text
  and in both themes shows only Office, Goals, Inbox, Series, Finances and Help
  with no 32x button, nothing cut off; after the smoke's walk Books and
  Industry appear in the rail with Helper-Chan's neutral line.
- Independent review (most capable model, source only): no critical problems;
  three important findings fixed (buttons opening parts early, routed openings
  announcing moments that never happened, missing and vacuous tests), plus three
  raised from minor (the rail after a route, the contest route, an older save
  with a leased studio).

## Not verified

- Whether new players now find the start calm and the menus findable. That
  needs outside testers (Tier 2).

## Rulings

- Built on `main`, uncommitted, on top of the uncommitted goals board work.
- The 32x part opens with the first sale (the simulation's measure of the
  spec's "after the first sale").
- The day-one sweep view closes Helper-Chan's phone; the phone has its own
  screen and every other screen closes it.
- Existing checks updated for intended behaviour: the UI polish, usability and
  management smokes open a part before asserting that browsing changes nothing,
  and expect 8x as the top speed before 32x opens; two AlphaTests assertions
  were rewritten to the xUnit analyzer's forms.
- Final review: three findings raised from minor to important (rail refresh
  after a route, the contest route, an older save with a leased studio).
- Rival offers use "shown" rather than "opened", so show every screen counts,
  as Q57 states.

## Deferred minors

- Session restore can show a page whose part is closed (an older save left on a
  page whose part was not reached).
- New tags gained before turning show every screen on still show while it is
  on; with guidance hidden, parts open untagged and their texts wait.
- The sweep does not render a "New" tag at 1280 x 720 with 150% text.
- Turning show every screen off at 32x before the first sale leaves 32x running
  with its button hidden.

## Next step

The Tier 1 closeout (Q53): tick checklist items only with evidence, record what
is still unproven, and move the fresh-player test into Tier 2.
