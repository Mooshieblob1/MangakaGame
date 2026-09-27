# Tier 1: display sweep (T1.9), considerations

Date: 2026-09-27.
Status: design agreed 2026-09-27 (Q29, Q30); implemented 2026-09-27; see the [completion record](../display-sweep-completion.md).
Plan: [implementation plan](../plans/2026-09-27-display-sweep.md). Checklist
item: T1.9 in the roadmap's Release plan section.

## Problem

T1.9 asks that the core screens work at 16:9 and 21:9, with 150% text, in the
dark and light themes, with nothing clipped or overlapping. Earlier checks
covered a few screens at a few sizes each, spread over 17 smoke files. Nobody
has looked at every core screen in every combination, and fixes since the last
UI sweep (2026-09-25) added new controls: the 32x speed button, Helper-Chan's
phone, runway and debut texts.

## Q29: how thorough should the sweep be?

Answer: **1**. A new automated check visits every core screen at 5 window
sizes, 2 text sizes and 2 themes, and flags anything off-screen, overlapping or
with cut-off text. I then look at about 40 screenshots myself:

- every screen at the tightest sizes (1280x720 and 1280x800 with 150% text),
- every screen at 3440x1440,
- every screen in the light theme,
- anything the check flags.

Window sizes: 1280x720, 1280x800 (16:10, Steam Deck), 1920x1080, 2560x1080
(21:9), 3440x1440 (21:9). Text: 100% and 150%. Themes: dark and light.

Core screens: new-career setup, office home and header, Helper-Chan's phone,
Production and the day recap, Publishing and the pitch, Books, printing and
conventions, Finance, Staff and hiring, Inbox and decision cards, Settings,
Save and Load.

## Q30: how strict at the smallest screen with the largest text?

Answer: **1**. Everything must be fully usable at 1280x720 and 1280x800 with
150% text. Things may wrap, scroll inside their panel or switch to a compact
layout. Nothing may be cut off, overlap or become impossible to reach. Any
problem the sweep finds is fixed in the layout.

## Routine decisions (made without a question)

- **What counts as a problem.** A visible control or label whose rectangle
  leaves the window; two visible sibling controls in the same container whose
  rectangles overlap; a label or button whose text needs more room than it has
  and is not set to wrap or scroll. Floating panels over the office are allowed
  to overlap the 3D view and each other only where the design intends it (the
  phone, Helper-Chan's card and pop-ups sit on top by design); content inside a
  scroll container counts as reachable when the scroll container itself is on
  screen.
- **Evidence.** Retained screenshots are AVIF, in the usual `TestResults`
  capture folder. The check prints a short list of flagged items per
  combination so the result is readable without images.
- **Scope.** Layout fixes only. No new screens, features or save changes.
  Non-core screens (awards, licensing, branches, industry, legacy) are captured
  once at 1280x720 with 150% text for information; problems there are recorded
  and fixed only if cheap, otherwise listed for Tier 2.
