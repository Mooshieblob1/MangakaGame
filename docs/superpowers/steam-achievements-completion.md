# Steam achievements, completion

Date: 2026-10-05. Tier 2 item 1 (native Steam integration and about 25
achievements), done for free against Valve's test app 480. Decisions: Q3
(2026-09-26, about 25 career achievements), Q68 (Steamworks.NET) and Q69 (the
Q3 draft with two gentler targets).

## Decisions

- **Q68, library: Steamworks.NET.** Free, MIT licensed, a thin C# wrapper that
  fits the C# code directly. GodotSteam (GDScript first) and
  Facepunch.Steamworks (little maintenance since 2022) were the alternatives.
  Version 2025.164.1 (Steamworks SDK 1.64) is kept in
  `godot/ThirdParty/Steamworks.NET/` with Valve's `steam_api64.dll`, so builds
  never depend on a download. The NuGet package was not used: it is older (SDK
  1.60) and has no `steam_api64.dll`. Source and checksum: `SOURCE.txt` there.
- **Q69, the list: the Q3 draft, two targets gentler.** Ten magazine chapters
  on time in a row instead of twelve, and 1,000,000 career copies instead of
  10,000,000, because balance past year five is untested.

## What changed

- **25 achievements** in `ProgressionCatalog.Achievements`, each with a Steam
  API name, a display name and a player-facing description (the ten older
  descriptions were rewritten from developer wording). Table below.
- **The 15 new ones are daily checks on the save**
  (`src/MangakaSim/GameState.Achievements.cs`), so an older save earns what it
  has already reached on its next in-game day. They are recorded as career
  milestones without an inbox message (`MarkMilestone(..., announce: false)`),
  because the goals board and Steam's own pop-up already mark these moments.
  Sandbox saves keep the milestones but earn no achievements, as before.
- **Unlocks retry.** `IAchievementSink.Unlock` now returns whether the platform
  took the unlock, and `AchievementSession` retries the ones it did not (Steam
  loads the player's achievements a moment after start).
- **`SteamAchievements`** (`godot/Platform/SteamAchievements.cs`) is the real
  sink. It never stops the game: with Steam off, missing or not running, every
  unlock stays in the save and reaches Steam on a later run.
  - Development: `--steam` connects with test app 480 (no `steam_appid.txt`
    needed; the app ID is set in the environment before start). Achievement
    names the app does not have, which is all of ours on 480, are skipped once
    instead of retried.
  - Release: set `SteamAchievements.ReleaseAppId` once the Steam Direct fee is
    paid. Exported builds then connect by default (`--no-steam` turns it off)
    and relaunch through Steam when started outside it. While it is 0, packaged
    builds for testers never touch Steam.
  - Steam callbacks run every frame; new unlocks are stored in one request,
    retried every 2 seconds on failure, and flushed on exit.
- **Career journal** shows achievement names instead of keys, and says whether
  Steam achievements are on, waiting for Steam, or disabled for a Sandbox save.
- **Packaging** copies `steam_api64.dll` and the Steamworks.NET licence
  (`LICENSE-Steamworks.NET.txt`) beside the executable.
- **`--steam-smoke`** checks a real round trip on test app 480: connect, load
  achievements, unlock Spacewar's `ACH_WIN_ONE_GAME` through the game's
  session, skip our unknown names, then put the Spacewar achievement back as it
  was. Needs Steam open and signed in.

## The 25 achievements

These are the values to enter in Steamworks when the app exists (API name,
display name, description). Each also needs two 256 x 256 icons (unlocked and
locked).

| API name | Name | Description |
|---|---|---|
| MKG_FIRST_PUBLICATION | In Print | Publish a chapter or release a book. |
| MKG_CONTEST_PLACEMENT | Judges Noticed | Place in a newcomer contest. |
| MKG_ANNUAL_AWARD | Manga Craft Award | Win the annual Manga Craft Award. |
| MKG_ANIME_RELEASE | On Screen | Release a first anime adaptation. |
| MKG_ANIME_FOLLOWUP | Second Season | Release a follow-up anime with reception of 75 or more. |
| MKG_MERCHANDISE | In Their Hands | Release licensed merchandise for one of your series. |
| MKG_READERS_10000 | A Real Hit | Reach 10,000 readers on one of your series. |
| MKG_READERS_MILLION | National Hit | Reach 1,000,000 readers on one of your series. |
| MKG_ICONIC_SERIES | Manga History | Make one of your series iconic. |
| MKG_SUSTAINABLE_STUDIO | Books Balanced | Earn over ¥1,000,000 operating profit in a year with every bill and wage paid on time. |
| MKG_FIRST_SALE | First Reader | Sell your first copy. |
| MKG_FIRST_CONVENTION | Behind the Table | Sell copies at a convention. |
| MKG_SERIALIZATION | Serialized | Accept a magazine serialization offer. |
| MKG_RANKING_TOP3 | Top Three | Reach the top three in a magazine ranking. |
| MKG_DEADLINE_STREAK | Never Late | Deliver ten magazine chapters in a row without a missed deadline or issue. |
| MKG_COMEBACK | Comeback | Win a new serialization after a series is cancelled. |
| MKG_FIRST_HIRE | First Assistant | Hire your first assistant. |
| MKG_FULL_TEAM | Full Team | Employ four assistants at once. |
| MKG_MOVE_OUT | A Studio of My Own | Move into a studio outside the family home. |
| MKG_INCORPORATE | Incorporated | Incorporate your business. |
| MKG_SECOND_STUDIO | Two Studios | Run two studios at once. |
| MKG_COPIES_100K | 100,000 Copies | Sell 100,000 copies of one series. |
| MKG_COPIES_1M | A Million Copies | Sell 1,000,000 copies across your career. |
| MKG_OVERSEAS_DEAL | Beyond the Shop | Sign a digital or overseas deal. |
| MKG_TEN_YEARS | Ten Years at the Desk | Still creating manga ten years after your start date. |

## Verification

Authorized by Blob on 2026-10-05 (GMT+8 evening). Automated checks only, no
human playtest and no package built.

- `dotnet build MangakaGame.sln -warnaserror`: no warnings.
- Unit tests (`Category!=Playtest`): 798 passed, including the new
  `AchievementTests` (catalog shape, quiet daily checks, older saves, Sandbox,
  convention, on-time run, comeback) and the retry test in `ProgressionTests`.
- `--progression-smoke` 16 checks, `--journey-smoke` 53 checks and
  `--management-smoke` 99 checks passed; none of them touch Steam.
- `--steam-smoke` passed (8 checks) on Blob's PC with Steam signed in: it
  connected to app 480, unlocked and restored `ACH_WIN_ONE_GAME`, and skipped
  `MKG_FIRST_PUBLICATION` as unknown to app 480.
- Found and fixed on the way: Godot loads game code in its own load context,
  which did not find `steam_api64.dll` in the build folder. The adapter now
  points .NET at the executable folder, the game assembly folder and, in
  development, `godot/ThirdParty/Steamworks.NET`. Before the fix the game
  still started normally without Steam, as designed.
- Not yet checked: an exported package with Steam (the release app ID is 0, so
  packages do not connect), and the overlay pop-up.

## Still open

- Steam Direct fee and the real app ID (planned for about 16 Nov, see the
  marketing plan), then the 25 achievements and 50 icons in Steamworks.
- Achievement icon art.
- The Steam overlay pop-up only appears when Steam launches the game, so it is
  first seen once the real app exists.
