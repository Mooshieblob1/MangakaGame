# Tier 1 fix 1: career guidance and Helper-Chan's phone, completion

Date: 2026-09-26. Design: [considerations](specs/2026-09-26-career-guidance-considerations.md)
(Q12 to Q15). Plan: [implementation plan](plans/2026-09-26-career-guidance.md).
Not committed yet.

## What changed

- **One career path.** Guidance no longer stops after the first sale. It
  continues through turning the doujin into a series, pitching to the magazine
  with the best shown chance, waiting for the editor with the answer date,
  recovering from a rejection (weakest factor, cooldown date, what to do
  meanwhile), accepting an offer before it expires, the first deadline and a
  first hire. Old "opening" and "doujin" saves move onto this path. Contest and
  employment remain side routes on the Help page.
- **Pitch chance.** The Publishing page shows the estimated acceptance chance
  used by guidance.
- **Helper-Chan's phone.** Her guidance and notifications arrive as texts
  with her portrait, the in-game date and time, and a "Show me" reply button.
  A green-screen PHS handset is used until the end of 2009 and a smartphone
  with chat bubbles from 2010; the thread carries over. Each text is 140
  characters or fewer.
- **Pop-up behaviour.** The phone slides up with a buzz on a new message and
  stays until dismissed or the step is done, then shrinks to an icon with an
  unread count. The icon reopens the thread. Hiding guidance stops pop-ups but
  keeps the icon.
- **Layout.** The phone sits in the bottom-right corner below the header,
  sized from its real content, and grows with the text size setting.
- **Playtest.** The career playtest bot follows guidance targets. Results are
  in the [playtest findings](full-career-playtest-findings.md).
- **Research.** PHS messaging history and the accepted period exceptions are
  recorded in the considerations document.

Guidance is read-only: it never changes game state, so saves, replays and
determinism are unaffected.

## Verification

Run under the implementation go-ahead of 2026-09-26. These are automated and
rendered checks on the development machine, not human playtesting.

- `dotnet build MangakaGame.sln -warnaserror`: no warnings or errors.
- `dotnet test --filter Category!=Playtest`: 557 passed, including the new
  `GuidanceTests`.
- Career playtest (`Category=Playtest`): guidance reached every step to a
  first hire on Standard, Challenging and Relaxed.
- Headless Godot smoke checks passed: management (99 checks), usability (48),
  convenience (25), production (21), progression (15), series status (18),
  atmosphere (885), office life (734), family home (75) and alpha (33, or 46
  rendered with `--capture`).
- Rendered captures in `TestResults/guidance-phone`:
  `alpha-phone-phs.avif` (1600x900) and
  `alpha-phone-{phs,smartphone}-{1080,2560x1080,3440x1440,720-150}.avif`.
  Inspected by eye: PHS at 1600x900, 2560x1080 and 1280x720 with 150% text;
  smartphone at 1920x1080 and 1280x720 with 150% text. All fit below the header
  and are legible.

## Known issues

- `--smoke-test` fails at "1x advances one hour in 2.5 seconds". This also
  fails on the previous commit `242240c`; the check predates the current day
  pacing and is not caused by this work. Fixed later on 2026-09-26: the check
  now uses the driver's real rate of 36 seconds per hour at 1x, and the smoke
  test passes with 823 checks.
- The open phone covers part of the page behind it, most at 1280x720 with 150%
  text. "Show me" closes the phone first, as designed in Q15.
- The 1997 growth hire can be suggested before a free desk exists, and
  Challenging recruitment cooldowns are unexplained. Both resolved by fix 2
  ([first-hire safety](first-hire-safety-completion.md)): guidance now needs a
  free desk, and the search button and guidance show the next search date.
