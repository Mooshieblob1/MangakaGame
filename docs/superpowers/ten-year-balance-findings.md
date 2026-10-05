# Ten-year balance findings, 1996 to 2006

Date: 2026-10-05. Tier 2 item: balance over 5 to 10 in-game years (early access
considerations Q3, balance for 1996 to 2006).

Source: the guided playtest (`tests/MangakaSim.Tests/CareerPlaytest.cs`),
extended from three to ten in-game years. Six careers: seeds 0, 1, 42 and 7 on
Standard, seed 7 on Relaxed and Challenging. Run on a clean copy of `main`
(`e46b309`) plus the playtest change only. Reports are kept locally in
`TestResults/career-playtest/ten-year-2026-10-05/`.

What this is: simulation runs driven by a scripted careful player. What it is
not: human playtesting, rendered screen checks or clean-machine testing.

## Changes to the playtest

- Runs to April 2006 instead of April 1999. The practice career fixture still
  looks at the first three years only.
- After the first hire the bot works on the goals board, as Helper-Chan points
  it there: it moves out once a year of rent is in the account, hires up to four
  people (six with two serialized series), moves to a bigger studio when desks
  run out, incorporates once the capital rule allows, starts and pitches a
  second series, accepts licence offers, answers licence production questions
  and pitches licences for the anime and merchandise goals.
- New report sections: a ten-year summary (money, flows, staff, fans, chapters
  and goal chapter per career year) and middle game activity (player decisions,
  32x stops, goals done and the longest quiet stretch each year).
- Helper-Chan texts are now counted from the last message seen, because the
  phone keeps only its newest 200 messages, and goal texts no longer count as
  32x stops (they never stop 32x in the game).

## Summary

Saves, replays and stability hold up over ten years, but the economy splits
careers into two kinds and neither is fun after about 2000:

1. **A runaway hit breaks the numbers.** On seed 42 the business holds about
   ¥48.6 trillion by 2006 and one series has about 94 billion fans, more people
   than live on Earth. This is a bug, not a balance taste question.
2. **Struggling careers never escape.** On three of six careers every new series
   debuts at or near the bottom of its magazine, stays there whatever its
   quality, and is cancelled about a year later. This repeats seven to nine
   times in ten years.
3. **Successful careers stop needing the player.** Once a series reaches rank 1
   it stays there for years. Money piles up (¥63 million to ¥122 million in the
   business and ¥32 million to ¥49 million in savings by 2006) with nothing to
   spend it on, and the player makes 0 to 3 decisions a year.

Yes, the middle game feels empty, in different ways for both kinds of career.

## Results by career

| Career | Business, April 2006 | Savings, April 2006 | Cancellations | Goal chapter reached |
|---|---|---|---|---|
| Seed 0 Standard | ¥1.04 million | ¥11.4 million | 7 | Studio Head (December 2005) |
| Seed 1 Standard | ¥63.3 million | ¥32.5 million | 1 | Legend (from July 2000) |
| Seed 42 Standard | ¥48.6 trillion | ¥12.2 trillion | 1 | Everything, by August 2005 |
| Seed 7 Standard | ¥1.12 million | ¥12.8 million | 8 | Serialized (all ten years) |
| Seed 7 Relaxed | ¥121.6 million | ¥49.1 million | 0 | Legend (from June 1999) |
| Seed 7 Challenging | ¥0.02 million | ¥5.6 million | 9 | Serialized (all ten years) |

All six careers pass the save round trip and full replay check. No exceptions.

## Findings

### 1. Runaway growth from online doujin downloads (release blocker)

On seed 42 a side doujin was listed online, continued as a series, spent ten
months being pitched while its chapters were collected into doujin volumes, and
was then serialized in a weekly magazine. The bot listed each doujin volume
online, 26 listings that year. Fans and money then multiply by about five to
eight every year:

| Year | Top fanbase | Business yen |
|---|---|---|
| 1998 | 108,842 | 25.8 million |
| 1999 | 1.47 million | 625 million |
| 2001 | 45.3 million | 24.6 billion |
| 2005 | 93.9 billion | 48.6 trillion |

Cause, from the source: an online doujin listing never closes, its downloads
are worked out from the series' current fanbase (magazine readers included),
and every download adds 0.3 fans (`DoujinOnline.cs` `DirectDownloadUnits`,
`GameState.Sales.cs` `ApplySales`). Once the series is popular, each listing
turns fans into downloads and downloads into more fans, so growth compounds.
Nothing caps the fanbase. The amounts are still below the point where the
game's checked arithmetic would crash, but a longer career could get there.

For scale, the best-selling magazine of the period, Weekly Shonen Jump, peaked
at about 6.5 million copies a week in 1995 (historical figure). A series with
more than about 10 million readers is beyond anything realistic.

### 2. Struggling careers are a treadmill

Seeds 0, 7 Standard and 7 Challenging follow the same loop every year:
continue a side doujin as a series, pitch it, win a tier 3 magazine (usually
Monthly Hoshigaku Flowers), debut near the bottom (13th to 18th), stay there
for about 13 issues and get cancelled. Seed 7 Challenging ranks 14th in almost every
issue of nine series. Chapter quality is 90 to 99 throughout, so the pages are
not the problem and the player has nothing left to improve.

Cause, from the source: the ranking score is 40% page quality and 60% fanbase
(`RankingRules.Score`), and each new series starts with only its own small doujin
readership. A creator's earlier readers do not follow them to a new title, and
fan gains below the cancellation line (about 380 per chapter at a tier 3
magazine) cannot close the gap before cancellation. Pitching, meanwhile, stays
easy, because reputation keeps rising.

Effects a player would notice:

- Helper-Chan shows "Reach the top 10" for 224 days at a time, five times over,
  on seeds 0 and 7.
- Seed 7 Standard and Challenging never leave the Serialized goal chapter in
  ten years; seed 0 leaves it in December 2005.
- The business sits between ¥0 and ¥2.5 million for a decade and is topped up
  from savings 10 to 40 times a year, while savings grow to ¥5 million to
  ¥13 million. (The bot moves only ¥50,000 at a time, so some of the top-ups
  are its own habit, but the studio never pays its own way.)

### 3. Successful careers compound with nothing to spend on

Seeds 1 and 7 Relaxed reach rank 1 (seed 7 Relaxed by its 27th issue, seed 1
by its 56th) and hold it for every remaining issue, 55 to 100 issues in a row. Yearly business income rises to
¥33 million to ¥46 million, almost all reprint royalties, while costs stay flat
at about ¥6.6 million (four staff, rent, provisions). Licence payments are
capped low (an anime pays at most ¥2.2 million up front) and barely register.

There is no reader fatigue for long series, nothing large to buy, and the bot
never needed a second serialized series (see finding 5). Money stops mattering
by about 2001, as the three-year run already hinted with seed 1.

### 4. The middle game is empty

| Career | Decisions per year, 2001 to 2006 | Longest quiet stretch in those years |
|---|---|---|
| Seed 1 Standard | 0 to 2 | 184 to 365 days |
| Seed 7 Relaxed | 0 to 3 | 97 to 339 days |
| Seed 42 Standard | 0 to 3 | 92 to 286 days |
| Seeds 0 and 7 (struggling) | 8 to 31 | 47 to 227 days |

Successful careers get no Helper-Chan texts at all after their third year and
wait 2,000 to 2,400 days on "A million readers" or "An iconic series", which no
action speeds up. Struggling careers have more to click, but it is the same
pitch, debut and cancellation loop each year, plus savings top-ups.

The real time is reasonable: about 1.1 real hours per in-game year at 32x
(4.0 at 8x), so ten years is about 11 to 16 hours. The problem is what fills
those hours, not their length.

### 5. Smaller findings

- **"Run two series at once" is met by a side doujin.** The goal counts every
  active title, including an unfinished side doujin, so it completes without a
  second magazine series. As a result the playtest never ran two serialized
  series, and that path is still untested over ten years.
- **Difficulty decides which kind of career you get.** Seed 7 is a ¥122 million
  hit on Relaxed and a nine-cancellation treadmill on Standard and Challenging.
  Difficulty should change how hard the climb is, not whether one is possible.
- **The second hire at the parents' house meets "This workplace has no free
  desk"** once in every career before the bot moves out. Known since the
  three-year runs; the move then works.
- **No missed issues, missed deadlines or staff departures** in any career.
  Staff wellbeing and deadlines never come into play once a studio settles,
  which also contributes to finding 4.
- **Missed paydays come with the treadmill.** Seed 7 Challenging misses 37
  paydays in ten years, seed 0 misses 7 and seed 7 Standard 5, each covered
  from savings before anyone leaves. Seed 1 misses one in 1998, before its hit.

## Proposed fixes, for review before any balance change

In priority order. None is made yet.

1. **Stop the runaway (bug, needed for release).** Work out download demand
   from the doujin's own readers rather than the whole magazine readership,
   give back-catalogue downloads the same small fan gain as collected volumes
   (0.03 instead of 0.3 per copy), and add a soft readership ceiling so fan
   gains shrink as a series nears about 10 million readers. Expected effect:
   seed 42 lands near the other hits instead of in the trillions.
2. **Let readers follow the creator.** A new series starts with part of the
   creator's earlier readership (for example a quarter of their best series'
   peak fans), so a second or third series debuts mid-table rather than last.
   Expected effect: struggling careers can climb after one or two tries, and
   each cancellation still costs momentum.
3. **Long series tire.** After a long run (about 60 to 80 chapters) readers
   slowly drift away unless the series is refreshed or ended. Ending a series
   on a high note already pays a reputation bonus; with fix 2 it also hands its
   readers to the next title. This gives successful careers a recurring
   decision: when to end the hit and what to launch next.
4. **Count only magazine series for "Run two series at once".** Small goal
   fix, so the board actually leads players into running two titles.

Fixes 2 and 3 together give the middle game a loop built from existing
systems (launch, climb, peak, finale, next series), which is cheaper than
pulling research points or training forward from Tier 3. Recommendation: make
fixes 1 to 4, re-run the ten-year playtest, and only then decide whether Aki's
story milestones (the cheapest Tier 3 item, which would fill the quiet years
with texts) are still needed.
