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

## Re-run after the four fixes (2026-10-05)

The coordinator approved all four fixes (Blob gave it discretion), and they are
made on branch `claude/project-thread-kpkoc9`, not on `main`:

1. Fan gains shrink towards a 10 million readership ceiling
   (`FanbaseRules.Saturated`). A magazine series' old doujin downloads reach at
   most a 20,000-reader following and add fans at the collected-volume rate.
2. A newly accepted magazine series starts with 30% of the readers of the
   creator's other titles, without lowering or stacking readerships.
3. After about 70 months a series joins readers more slowly (down to a quarter
   of the usual gain) and loses up to 0.6% more a month, at the same pace per
   month for weekly and monthly magazines.
4. "Run two series at once" counts only titles running in magazines.

Verified by: 797 simulation tests passing (seven new ones in
`TenYearBalanceTests.cs`), and the same six ten-year careers re-run. Reports
are kept locally in `TestResults/career-playtest/ten-year-fixes-2026-10-05/`.
No save format change.

| Career | Business, April 2006 | Savings, April 2006 | Cancellations | Left the Serialized chapter |
|---|---|---|---|---|
| Seed 0 Standard | ¥1.04 million to ¥5.24 million | ¥11.4 million to ¥16.0 million | 7 to 5 | December 2005 to November 2003 |
| Seed 1 Standard | ¥63.3 million to ¥48.8 million | ¥32.5 million to ¥36.6 million | 1 to 1 | October 1998 to September 1998 |
| Seed 42 Standard | ¥48.6 trillion to ¥1.96 billion | ¥12.2 trillion to ¥518 million | 1 to 1 | February 1999 to July 1999 |
| Seed 7 Standard | ¥1.12 million to ¥4.63 million | ¥12.8 million to ¥18.9 million | 8 to 8 | Never to August 2005 |
| Seed 7 Relaxed | ¥121.6 million to ¥54.3 million | ¥49.1 million to ¥38.3 million | 0 to 1 | November 1997 to December 1997 |
| Seed 7 Challenging | ¥0.02 million to ¥5.20 million | ¥5.6 million to ¥10.9 million | 9 to 8 | Never to November 2004 |

All six careers pass the save round trip and full replay check.

### What changed

- **The runaway is gone.** Seed 42's top series levels off at about 1.6 million
  readers (1.55 million in 2004, 1.60 million in 2005) instead of 94 billion.
  It is still a weekly mega-hit earning about ¥500 million a year in reprint
  royalties, which is plausible for a hit of that size but leaves money
  meaningless for that career.
- **Hits now peak and fade.** Seed 7 Relaxed peaks at about 254,000 readers in
  2004 and slips to 245,000 in 2005, with yearly reprint royalties falling from
  ¥21.0 million to ¥17.0 million. Seed 1 flattens at about 230,000. Final
  business money roughly halves for both.
- **Every career now runs two magazine series** at some point (seed 7
  Relaxed from early 1998, seed 1 from late 1998), so that path is now tested.
- **Struggling careers can climb, but too late.** All three now escape the
  cancellation loop and reach the Legend goal chapter, but only after 7.5 to 9.5
  years, in November 2003, November 2004 and August 2005. Cancellations barely
  fall (5 to 8 instead of 7 to 9). Readers following the creator add up only as
  cancelled titles pile up, so the escape still depends on outlasting the loop.

### Is the middle game still empty?

Yes, for careers with a hit. From their fifth year seeds 1, 42 and 7 Relaxed
make 0 to 5 decisions a year, almost all accepting licence offers, with quiet
stretches of 90 to 285 days and 0 or 1 Helper-Chan texts a year.
Struggling careers stay busy, but with the same pitch, debut and cancellation
cycle until they break out.

Two gaps remain: there are few decisions with consequences once a studio
settles, and nothing large to spend money on.

### Which Tier 3 item would fill it best

Of the three parked items (research or know-how points, staff and Aki training,
Aki's story milestones), **Aki's story milestones** fit best, if each one asks
the player a real choice with a cost (time away from the desk, money, a staff
member's future), spread through the quiet years:

- Training does little while page quality is already 90 to 99 from the start
  (Aki begins at 95 in every stage), so it would add clicks, not decisions.
- Research points need something worth buying. Without a money sink they repeat
  the "nothing to spend on" problem.
- Story milestones reuse the existing story scenes and Helper-Chan texts
  (`StoryCommand`), so they are the cheapest of the three, and they put a moment
  that needs the player into each quiet year.

A smaller tuning question is also open: the struggling careers' climb could be
made earlier (for example a larger following share or a debut boost for a
creator's later series). That is a balance call, not a Tier 3 item, and is not
made.

## Re-run with the first three story milestones (2026-10-06)

Blob chose to implement Aki's story milestones (2026-10-05) and the first three
are built on branch `claude/project-thread-kpkoc9`: the final arc, a bigger
magazine calls, and an assistant who wants to debut (spec
`specs/2026-10-05-aki-story-milestones-design.md`). The balance fixes above were
merged to `main` first (`b335443`, after 805 simulation tests passed on the
combined code). With the milestones, 817 simulation tests pass and the whole
solution builds without warnings. The milestone card has not been checked on
screen yet.

The playtest answers each milestone with the first answer on even seeds (0, 42)
and the second on odd seeds (1, 7).

| Career | Milestones met | Decisions a year, years 5 to 10 (before) | Longest quiet stretch, years 5 to 10 (before) |
|---|---|---|---|
| Seed 1 Standard | debut (wait), final arc four times (keep running) | 2 to 7 (1 to 4) | 92 to 271 days (92 to 271) |
| Seed 42 Standard | debut (back it), final arc twice (plan the ending), bigger magazine (accept) | 2 to 5 (0 to 4) | 115 to 246 days (90 to 285) |
| Seed 7 Relaxed | bigger magazine (stay loyal), debut (wait), final arc three times (keep running) | 0 to 4 (0 to 3) | 150 to 275 days (151 to 275) |
| Seeds 0, 7 Standard | one debut each | unchanged in kind | unchanged in kind |
| Seed 7 Challenging | none (never settles until 2005) | unchanged | unchanged |

- Hit careers make a little more use of their middle years, and planning an
  ending works as intended: seed 42's runaway hit ends, and the business ends
  at ¥1.11 billion instead of ¥1.96 billion.
- The target (at least four decisions a year after year five and no quiet
  stretch past about 150 days) is **not met**. Only the final arc can recur, and
  the other two happen once per career, so the quiet years return once they
  are used.
- Struggling careers are not interrupted, as designed.
- Seed 42 was re-run alone to confirm its save round trip and replay check
  pass with the milestones (the six-career run stopped before recording its
  checks).


Recommended next step: build the remaining three milestones (teaching, an
overseas convention, the parents' house) and let some recur (a new assistant
debut every two years or so, the bigger magazine again after a loyalty
period), then re-run.
