# Sub-project 7 research notes

Date checked: 2026-09-24. These notes distinguish contemporary source evidence,
historical uncertainty and proposed game mechanics. No source below establishes
a universal 1996 prize amount, adaptation fee, creator share or production time.

## R1 — newcomer contests

[Shueisha's official Tezuka/Akatsuka requirements](https://www.jump-mangasho.com/award/tezuka-akatsuka/)
describe separate story and gag categories, multiple prize levels and publication
opportunities for some placements. Current rules specify unpublished original
work and restrict simultaneous submissions and previously awarded work. The
story category currently requires 31 pages; professional and amateur entrants
are both accepted. Thus a blanket rule equating every newcomer contest with
"never published professionally" would be inaccurate.

Use this evidence for distinct eligibility policies and tiered results. Do not
copy current web submission methods, prize figures, judges or license terms into
1996. Feedback for every entry, meaningful revision before retrying and the
fictional game's automatic administration are player-facing design choices.
The live page contains mixed future ceremony wording; it is not a historical
calendar source. Use versioned fictional schedules unless a period-specific
primary source supports the actual event being represented.

## R2 — awards for published work

[Shogakukan's official Manga Award page](https://shogakukan-comic.jp/shogakukan-mangasho)
describes a 2026 published-work eligibility window, recommendations from industry
participants and readers, and successive selection stages ending in committee
judging. It supports distinguishing published-work awards from manuscript-entry
contests. Automatic consideration in the game abstracts this process. Current
categories, prize amounts and web eligibility are not evidence of 1996 rules.

## R3 — licensing scope

[METI, Trade White Paper 2025, section I-2-3-4](https://www.meti.go.jp/report/tsuhaku2025/2025honbun/i2340000.html)
has an indexed official excerpt explaining that licensed IP scope can be narrow,
such as animation or merchandise, or broader. The full-page fetch returned HTTP
403 in this session, so only that limited indexed statement was checked. It
does not support a standard royalty percentage, guaranteed approval right or
universal production-committee arrangement. Model the rights granted by each
deal explicitly; numerical contract terms remain fictional game tuning.

## R4 — Steam achievement delivery

[Steamworks ISteamUserStats](https://partner.steamgames.com/doc/api/ISteamUserStats)
documents that `SetAchievement` changes cached achievement state and `StoreStats`
submits changes. Achievement API names must be configured and published for the
application. Sandbox eligibility therefore must be checked before issuing an
unlock, rather than relying on suppressing only the later upload.

[Steamworks stats and achievements](https://partner.steamgames.com/doc/features/achievements)
documents offline caching and later synchronization. Prevent Sandbox-origin
progress from entering the platform path, including stat-driven unlocks.
Permanent save ineligibility is this game's policy, requested by the user, not
a Steam requirement. No application ID, dashboard configuration or live Steam
achievement test has been established by this research.

## Existing Tokyo and historical grounding

Retain the [Tokyo research](2026-09-23-tokyo-research.md) and
[historical timeline research](2026-09-24-historical-timeline-research.md).
This design introduces no new ward distances, fares, rents or compulsory awards
ceremony travel. The parents' home remains rent-free by the user's instruction.
Future technology in Sandbox changes access, not the historical calendar.

## Evidence still required for historically named content

Before shipping an actual named award or partner with date-specific claims,
verify its existence and the relevant period's rules from primary sources.
Label simulated outcomes as simulated. Prefer fictional, explicitly tuned
content where period evidence is missing. Do not present contemporary economic
values or fictional fees, probabilities and durations as measured Japanese data.
