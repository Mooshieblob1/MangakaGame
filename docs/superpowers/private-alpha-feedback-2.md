# Production, income and management clarity — alpha.4

Implemented 24 September 2026, continuing the private alpha feedback.

## Player-facing changes

- Staff, Studios and Industry open their actual full management controls on the
  first click. Finances, Books and task forms fill the management area. Inbox and
  Series keep the office alongside them. Series cards and details have percentage
  numbers inside their progress bars; details put current progress above actions.
- Creation distinguishes a one-shot (one story, then stop) from an ongoing
  series. New ongoing titles produce a printable numbered issue per chapter and
  an optional collection every five chapters. Collections and issues have
  separate stock. Unprinted optional collections do not block the next issue.
- Self-published work and pitch samples have personal targets, without deadline
  risk, automatic overtime or late-completion penalties. Contract issue deadlines
  remain binding. Normal queue ordering prioritizes contract deadlines; explicit
  manual ordering and pins remain available.
- A pitch replaces untouched drafts but preserves started chapters, holding them
  while the separate 31-page sample is prepared. No collected book is required.
- The protagonist starts at skill 95 instead of 80 in all five production stages.
  Base hourly work rises from 1.6 to 1.9 before needs modifiers, an 18.75% increase.
  Ordinary learning, needs, review and production rules still apply.
- Finances offers no outside job, afternoon shifts, or evening shifts. Shifts run
  four hours on Monday/Wednesday/Friday, credit personal savings each worked hour,
  and prevent simultaneous manga work. Convention/career travel takes precedence.
  Hired employees keep their studio wages. Joining an employer suspends the
  protagonist's outside shifts. Personal contributions remain explicit transfers.
- Printing quotes show estimated printing break-even copies at the local net
  sale price, excluding wages, overhead and creator share. Delivery starts local
  sales automatically; prices and quantities remain player choices.
- “Send to convention” is available from series, books and printing. The compact
  form shows title, attendee, event, stock and total booth/return-travel expense
  before confirmation. Booked attendance happens automatically, prevents drawing
  while away, and sells only that title's available printed stock subject to
  demand/capacity. Promotion also reaches a small number of readers without stock.
  Records show completed sales. Nearby/free event costs retain existing rules.

## Save compatibility

Simulation format 9 adds optional fields for outside jobs, short issues and
targeted convention bookings. Loading format 8 preserves money, work, history,
Sandbox provenance and saved commands; it raises protagonist skills to at least
95 and enables short issues for ongoing unpublished titles. Higher learned skills
remain intact. Old personal-target risk/late flags are cleared. A new replay
checkpoint marks the rule change; subsequent actions remain deterministic.
Previously saved snapshots are not overwritten merely by loading them.

## Research and authored balance

- **Historical benchmark:** MHLW lists Tokyo hourly minimum-wage figures of ¥650
  for fiscal 1995, ¥664 for 1996 and ¥679 for 1997. These annual values do not
  establish each year's 1 April effective rate. [MHLW historical table](https://www.mhlw.go.jp/shingi/2003/12/s1202-3h.html).
- **Game choice:** outside pay uses a ¥700 baseline times the game's existing
  price index, rounded upward to ¥10. The April 1996 opening pays ¥710/hour,
  or ¥8,520 for twelve completed shift hours. It is not a claim about a typical
  historical job, exact statutory wage progression or take-home tax treatment.
- **Historical context, later than the opening:** Comiket's 2011 circle survey
  reports losses for roughly two-thirds of respondents. It supports keeping a
  distinction between hobby income and reliable living expenses, rather than
  promising printing profit. [Comiket 35th-anniversary survey](https://www.comiket.co.jp/info-a/C81/C81Ctlg35AnqReprot.pdf).
- **Modern proxies:** [Melonbooks' circle terms](https://circle.melonbooks.co.jp/terms/)
  illustrate specialist consignment fees; [BOOTH's guide](https://booth.pm/guide)
  distinguishes physical and download products. Neither is presented as a 1996
  platform or exact historical contract. The five-chapter collection threshold,
  issue-per-chapter release model and convention promotion increment are game
  rules, not universal Japanese publishing requirements.

The starting parents' house still has zero rent, as requested.

## Verification

- Warning-free solution build and 526 passing simulation tests.
- Eight new gameplay checks cover individual issues and collections, continued
  production without printing collections, soft targets, paid time away, optional
  schedules, employer suspension, targeted convention stock/promotion, migration,
  save/load and deterministic replay.
- 25 rendered production checks cover actual navigation/creation/printing/job/
  booking controls, selection, duplicate booking, saved views, numbers inside
  progress bars, and a complete quote plus confirmation button at 1280×720.
- 50 rendered usability checks preserve screen-plane camera panning, keyboard
  speed/pause, live printing, current progress, dark/light themes and save/load.
- The exported Windows build passes 21 opening-to-first-sale checks. A fresh
  extraction of the final ZIP also passes all 25 rendered production checks.
  Both packaged runtime error logs are empty.
- Normal simulation produced the opening 16-page issue in approximately eight
  game days with afternoon shifts. This is a fixture result, not a guaranteed
  pace or profit for every workload or play style.

Package and rendered checks run on the development Windows/RTX 5070 machine.
Clean-machine and broader hardware qualification remain separate. No public
release, commit or push is included.

Package: `builds/MangakaStudio-0.8.0-private-alpha.4-Windows.zip`.

SHA256: `B3F42602D66D964CCEDDE347D18BE1F6C943DDC2FE95BA123EC37EADA00A7EFB`.
