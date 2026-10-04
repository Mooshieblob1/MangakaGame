# Tier 1 closeout

Date: 2026-10-04. Decision: Q53 (2026-10-02, in
[the progressive disclosure design](specs/2026-10-02-progressive-disclosure-design.md)):
tick checklist items only with evidence, record what is still unproven, and
move the fresh-player test into Tier 2 as a playtest with strangers from the
target audience (Steam Playtest once a store page exists). No more testers are
expected for Tier 1.

The checklist itself is in the
[roadmap's Release plan](specs/2026-09-22-roadmap.md#release-plan). Tester
evidence is in the [fresh-player findings](fresh-player-test-findings.md).

## Checklist

| Item | State | Evidence | Still unproven |
|---|---|---|---|
| T1.1 Core journey | Ticked | Tester A's own career reached every step without help (first sale 0:17, serialization 0:41, first hire 2:25, warning, rejection and cancellation by 3:22). `--journey-smoke` walks every step with a save and reload at each. The guided playtests (six careers to April 1999) reach each step. The guidance dead end tester A hit (A1) is fixed. | A target-audience player completing it on the current build. |
| T1.2 Taught in the game | Ticked (signed off by the user, Q66) | Guidance reaches every step in the guided playtests ([career guidance](career-guidance-completion.md)). Since A1, a contest or employment route no longer replaces career guidance. Each menu part is announced as it opens ([progressive disclosure](progressive-disclosure-completion.md)). The goals board has How? tips ([career goals](career-goals-completion.md)). Helper-Chan teaches selling as it happens ([streaming sales](streaming-sales-completion.md)). Testers A and B used no help or lookups. | Against it: tester A, on alpha.12 before these fixes, said the game did not help them learn it. Whether the fixes answer that needs a fresh player (Tier 2). |
| T1.3 Clear status | Ticked | Staff name what they wait for (A4). Buttons that cannot work are disabled with the reason (A5, B4). Money changes show in the header. Sales stream through shop hours with a Sold count. Licence terms are explained (A6, below). Display sweep 616 checks, 0 flagged ([display settings](display-settings-completion.md)). | Money legibility (B3) was observed rather than fixed; the goals board's money goals answer part of it. |
| T1.4 Economy sense | Ticked | Six guided careers stay solvent to April 1999, ending with 0.55 to 10.3 million yen personal savings ([streaming sales playtest comparison](streaming-sales-completion.md)). Pitch rejections and cancellations happen and are recoverable ([setbacks and economy](setbacks-economy-completion.md), [playtest findings](full-career-playtest-findings.md)). Tester A survived a cancellation and played on for ten more in-game months. | One career with a hit series (seed 42) is very rich by 1999. Balance over ten years is Tier 2. |
| T1.5 Pacing | Ticked after the quick start (signed off by the user, Q66) | First sale at 0:17 (tester A) and 0:23 (tester B) on alpha.12, against a 15 to 20 minute target. Three in-game years are estimated at 3.3 to 4.8 real hours with 32x ([mid-career pacing](mid-career-pacing-completion.md)). Tester A took about 2.6 real hours per in-game year, 35% of it paused. | The opening is being trimmed for tester C's C1 (Q65, separate work). The judgement of "acceptable" is the user's. |
| T1.6 Saves | Ticked 2026-09-28 | Unchanged. | |
| T1.7 Stability | Ticked 2026-09-28 | Unchanged. | |
| T1.8 Extra systems | Ticked | Extra systems stay hidden until their moment or chapter (A3), and rival job offers to Aki wait for Industry (Q57). Licence offers now explain their terms (A6). Clarity fixes for extra systems are in the [tester A record](tier1-closeout-and-tester-a-completion.md). | |
| T1.9 Displays | Ticked 2026-09-27 | Unchanged; the display settings work since then (C2, C3) extended the sweep to interface sizes, 616 checks, 0 flagged. | |
| T1.10 Fresh player | Moved to Tier 2 | Round 1 ran: tester A completed T1.1, tester B stopped after 1 h 26 min. All their approved findings are fixed or decided. | Becomes the Tier 2 stranger playtest. |

Tier 1 is complete once the user signs off T1.2 and T1.5 (and the whole tier).

**Q66 (decided 2026-10-04): sign off T1.2 and T1.5 once the quick start
(Q65) lands.** The Tier 2 stranger playtest confirms both on real players.
Rejected: T1.2 now and T1.5 after the user plays the trimmed opening; keeping
both open until the stranger playtest.

## A6 and A7 (tester B never reached them)

Both were approved on 2026-09-28 as "waits for tester B". Tester B stopped
before licensing and did not find their pitch rejection unfair, so neither was
confirmed. Calls made on 2026-10-04:

- **A6, licence terms unexplained: explained in place**, not hidden. Licensing
  already stays out of sight until Industry opens (A3), and the missing piece
  was words, not a system. The offer card on the Licenses page now says:
  - the guaranteed payment, split into the amount on signing (20%) and on
    release;
  - that the creator receives their percentage of every payment;
  - for products, that monthly royalties follow for a year, with the monthly
    maximum in yen, more when the products are well received; for an anime,
    that there are no royalties and the reward is new readers and a six-month
    lift for the manga;
  - on an open offer, what fit and reliability mean (how well the partner
    suits the series; how likely they stay on schedule without disputes) and
    that both raise the reception; for an anime, what approval rights allow.
  Presentation only (`godot/DebugMain.Progression.cs`): no simulation, balance
  or save change.
- **A7, setbacks felt random: no new work.** Tester A's guidance was stuck on
  the contest route from minute 31 (A1), so Helper-Chan's cancellation warning
  never reached them. That text already names the series' rank, the safe rank,
  the issues left before the editor decides and the weakest part (genre fit,
  page quality or readership) with what to do about it. Since the A1 fix,
  warnings, rejections and cancellations come before any side route. The
  Tier 2 stranger playtest watches for it; one more report reopens it.

## Verification

The A6 change compiled warning-free in the quick start's build (`2bb6d66`:
`dotnet build MangakaGame.sln -warnaserror`, 790 unit tests pass). It has not
been rendered. When building is next authorized, run
`--progression-smoke --capture` and a look at the Licenses page at 1280 x 720
with large interface size, since the card has two more lines.

Rendered 2026-10-05 for alpha.13: `progression-offer.avif` at 1600 x 900 shows
the explained terms with nothing clipped. The 1280 x 720 large-size look is
still to do. See the [alpha.13 build verification](alpha-13-build-verification.md).

## Next step

Done: T1.2 and T1.5 were ticked after the quick start (`2bb6d66`), so Tier 1
is complete (2026-10-04). Next is Tier 2, starting with the release items in
the roadmap.
