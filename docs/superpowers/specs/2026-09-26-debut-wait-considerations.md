# Tier 1 fix 5: the wait before a series debuts, considerations

Date: 2026-09-26.
Status: design agreed 2026-09-26 (Q24, Q25); implemented 2026-09-26, see the
[completion record](../debut-wait-completion.md) and the
[implementation plan](../plans/2026-09-26-debut-wait.md). Source finding: item 4,
"Pre-publication gap", in the [full career playtest findings](../full-career-playtest-findings.md).

## Problem

Accepting a monthly serialization sets the first issue four issues ahead, about
110 in-game days. The studio then draws two chapters ahead (the default
finished-chapter buffer) and stops. From the production rules, a 32-page
chapter is about 166 working hours at average skill, so a prodigy working alone
finishes both within about 3 to 4 weeks. That leaves about 80 in-game days,
roughly 50 real minutes, with nothing queued. Helper-Chan repeats "Your first
deadline" the whole time, and the business balance slowly falls (300,100 yen to
283,018 yen in the seed 0 run) with no explanation of why no money arrives.

The 80-day figure is an estimate from the rules, not a measurement. The
implementation adds a measured "stock ready" date to the playtest report.

## Research

Historical fact and modern comparison, from Japanese sources:

- Today the gap from a serialization decision to the debut is often about six
  months, and creators commonly prepare several chapters, up to a volume of
  storyboards or finished pages, before debuting.
  [Mineral0120 on X](https://x.com/Mineral0120/status/1801736059606470973),
  [Hobonichi interview with editor Lin Shihei](https://www.1101.com/n/s/lin_shihei/2023-09-02.html).
- The same editor notes preparation periods have grown longer recently, so a
  1996 debut was probably prepared faster. The game's four issues ahead is
  plausible for the period.
- Page fees are paid after publication, commonly at the end of the following
  month and sometimes two to three months after the issue, so no page-fee
  income during the wait is realistic.
  [Chiebukuro](https://detail.chiebukuro.yahoo.co.jp/qa/question_detail/q10280721515),
  [note: shokonigaoe](https://note.com/shokonigaoe/n/n51c001f92946),
  [Ameblo: kito-kito](https://ameblo.jp/kito-kito-748/entry-11342193044.html),
  [tttombo](https://www.tttombo.com/chishiki-manga-genkouryo.html).

Gameplay estimate: keeping four issues of lead time is authentic; the problem is
presentation and having nothing to do, not the length.

## Decisions

### Q24. Keep the length, make the wait count (decided 2026-09-26)

- Keep the four-issue lead time.
- Helper-Chan explains the debut date and why editors want chapters finished
  first: stock ahead protects against a missed issue, and page fees only arrive
  after chapters publish.
- Show how many chapters are ready ahead, such as "2 of 2 ready", on Helper-Chan's
  texts and on the series status in Production.
- Once the stock is ready, Helper-Chan suggests side work.
- Real-time pacing of the wait is left to item 6, mid-career pacing.
- Rejected: shortening the gap (less authentic, and removes the natural moment
  to prepare), and doing nothing beyond text (the studio would still sit idle).

### Q25. One fitting suggestion, others listed (decided 2026-09-26)

Once the stock is ready, Helper-Chan names one suggestion with one reason, and
lists the other options in a shorter text. The first matching rule wins:

1. **Early hire**, when funds cover about three months with an assistant (the
   existing first-hire runway check). If every desk is taken, she points to
   Furniture first, as the existing desk card does.
2. **Convention**, when the player has unsold doujin stock. She names the next
   major event if it falls before the debut, otherwise the next regional one.
3. **Short doujin or one-shot**, when no side doujin is in progress.
4. **Part-time job**, as the fallback, for example while a side doujin is
   already under way.

Every version also mentions that Production can draw up to 4 chapters ahead for
extra safety, using the existing finished-chapter buffer setting.

- Rejected: an equal menu of all options (less direction for new players), and
  a short "you have free time" mention only (players who do not know the side
  activities would not find them).

## Scope limits

- No simulation balance change and no save format change. The ready count and
  the suggestion are read-only views of existing state.
- Work order is unchanged: chapters with a publisher deadline already come first
  in every queue, so side work cannot delay a magazine chapter.
