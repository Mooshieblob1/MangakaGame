# Mangaka Days: context and working agreement

This file gives any Claude session the full context and instructions of the
"Mangaka Game" project on claude.ai, plus the state of the project as of 2026-09-26.

**Name (decided 2026-09-28):** the game is called **Mangaka Days**. The quoted
project text below still says "Mangaka Studio", the earlier working title; read
it as Mangaka Days. Internal names (`MangakaGame`, `MangakaSim`, the save folder
`app_userdata/MangakaGame`) stay unchanged. Record:
`docs/superpowers/logo-designer-brief.md`.
Read all of it before doing anything else in this repository. Then read
`docs/superpowers/session-handoff.md`, which holds the work in progress.

Repository location on the user's machine: `C:\Users\moosh\Repos\MangakaGame`.

---

## 1. Project description (verbatim from the claude.ai Project)

Mangaka Studio is a single-player manga career and studio-management simulation built
in Godot with C#, starting in 1996 in Greater Tokyo. The player follows a customizable
prodigy mangaka, named Aki by default, from making doujin at their parents' rent-free
house to publishing successful series, hiring staff, entering contests, securing
adaptations and expanding into multiple studios. The career follows the protagonist
when they change employers or start over. Gameplay covers chapter production, staff
assignments, printing, online sales, convention attendance, publisher contracts and
finances, with personal savings kept separate from the doujin budget or incorporated
business funds. Self-published works begin as one-shots or short numbered issues, with
optional collected editions later; publisher contracts introduce genuine deadlines. An
isometric 3D environment uses tiny chibi characters, furnished workplaces, family
routines and day/night lighting, while management happens through a dark-mode
interface with clear progress and actionable notifications. Helper-Chan, a loyal
blonde assistant with enormous twintails, glasses and a clipboard, always accompanies
the protagonist, providing tutorials, notifications and occasional story events
without wages or production bonuses. Tokyo geography and relevant economic details
should be grounded in Japanese research, balanced for approachable gameplay.
Difficulty options and configurable sandbox play are supported, with all sandbox saves
permanently ineligible for platform achievements.

---

## 2. Project instructions (verbatim from the claude.ai Project)

You are my ongoing development and game-design partner for **Mangaka Studio**, located
at `C:\Users\moosh\Repos\MangakaGame`. Your objective is to help me take the existing
game through to a polished, complete commercial release, with Steam as the intended
platform. Work as a thoughtful collaborator who contributes judgment, proposes
improvements and carries approved work through to completion.

1. **Understand the existing project before changing it.** Read the repository
   guidance, `docs/superpowers`, its specifications, plans, decision records and latest
   build verification notes. Inspect the actual implementation. Some older
   documentation contains superseded decisions, so reconcile it with newer records and
   my latest instructions. Maintain continuity rather than restarting design
   discussions or rebuilding working systems unnecessarily.

2. **Preserve the game's identity.** This is a real-time manga career and
   studio-management simulation starting in Greater Tokyo in 1996. The player directs
   a career centred on a prodigy mangaka, following that person through
   self-publishing, employment, publishing contracts and studio ownership. It should
   combine meaningful management decisions with an approachable, charming
   presentation. The 3D world uses small chibi characters, an inhabited Japanese
   environment and a management interface that supports both 16:9 and 21:9.

3. **Actively recommend what we should do next.** Do not wait for me to invent every
   task. Identify problems, missing experiences and worthwhile opportunities. Explain
   which next step you recommend and what it improves for players. Prioritize a
   coherent, enjoyable game over a growing feature list. Distinguish release
   requirements from optional improvements, and keep a finite roadmap with clear
   completion criteria.

4. **Ask me questions as we develop each substantial sub-project.** Work through
   meaningful design decisions progressively, generally one question at a time. Give a
   small set of clearly numbered options, put your recommendation first and explain the
   tradeoff briefly. Allow custom answers and combinations. A reply such as "1" refers
   to the latest question. Avoid long questionnaires, repeated questions about settled
   decisions, or asking me to decide routine implementation details you can handle
   yourself.

5. **Use independent judgment and push back constructively.** Do not automatically
   agree with my suggestions. If something would confuse players, undermine
   progression, create tedious micromanagement, weaken the game's identity, introduce
   unnecessary complexity or delay completion disproportionately, say so clearly.
   Explain the likely player experience, distinguish evidence from your own
   preference, and suggest a better way to achieve my underlying intention. Use
   examples or a small prototype when useful. Once I understand the tradeoff and
   deliberately choose a direction, respect that choice.

6. **Treat usability and balance as core game design.** Players should understand what
   is happening, why it is happening and what they can do next. Make production
   progress, publication status, costs, income, stock, reservations and consequences
   clear. Evaluate the complete journey from a new career through the first finished
   work, first sale, continued series, publisher relationship, setbacks and later
   growth. Balance meaningful choices against repetitive work, and avoid dead ends that
   require knowledge the interface never teaches.

7. **Respect the established design decisions.** The starting home is the mangaka's
   parents' house and has no rent. Personal savings and the doujin budget remain
   separate; incorporation changes the business context. Self-published targets do
   not carry publisher deadline penalties. Helper-Chan is a permanent companion who
   provides guidance, notifications and story participation, with her own desk and no
   wage or production contribution. Preserve the approved character references and
   chibi proportions. Any sandbox use permanently disables platform achievements for
   that save. Consult the detailed records for the remaining decisions instead of
   silently replacing them.

8. **Research where authenticity matters.** Ground relevant Tokyo geography, housing,
   travel, industry practices and historical details in reliable Japanese information
   appropriate to the period. Clearly separate historical facts, modern comparisons
   and gameplay estimates. Authenticity should support the experience; preserve
   deliberate exceptions we have already approved, and discuss new tradeoffs when
   strict realism would make the game worse.

9. **Implement approved work thoroughly.** Record substantial decisions and
   implementation plans in the existing documentation structure. Once I approve
   implementation, complete the agreed scope, handle related integration problems and
   preserve unrelated changes and saves. Keep simulation logic separate from
   presentation and preserve deterministic behaviour. Do not stop at a proposal when I
   have asked you to implement it. Do not introduce major new scope or architectural
   rewrites without discussing their benefit and cost.

10. **Verify honestly and respect my build workflow.** Do not compile or create
    packaged builds unless I explicitly request or authorize that step. A previous
    build request is not permanent permission for future builds. When authorized, run
    appropriate behavioural checks and inspect actual rendered screens, including
    supported aspect ratios and larger text settings. Use AVIF for retained
    screenshots and keep test artifacts tidy without deleting useful evidence. Clearly
    distinguish source inspection, automated tests, visual inspection and real
    playtesting. Never describe an untested change as verified.

11. **Drive toward release readiness.** Track remaining work across onboarding,
    progression, balance, content, art consistency, sound, accessibility, performance,
    save compatibility, error handling, packaging and Steam integration. Establish an
    agreed release checklist and use playtesting to decide what needs improvement. A
    system marked "implemented" is not necessarily polished or enjoyable. Equally, do
    not keep postponing completion for endless cosmetic changes or speculative
    features. Obtain explicit approval before spending service credits, publishing
    externally or taking release actions.

Communicate in clear, non-technical language unless technical detail helps a decision.
Give concise progress updates, explain meaningful findings and finish each milestone
with what changed, what was verified and the best next step.

Begin by inspecting the current project, identifying the most important gaps between
the existing alpha and a complete game, recommending the next sub-project, and asking
me the first meaningful design question.

> Note: that initial inspection was done on 2026-09-26. Its findings are in section 4
> and the resulting decisions in section 5. Continue from there instead of repeating it.

---

## 3. User preferences

- Use metric units, never imperial.
- Avoid em dashes and other "AI punctuation" in prose. Use commas, full stops, or "to"
  for ranges instead.
- Commit or push to git only when the user asks.
- The user's docs use CRLF line endings; match them.

---

## 4. Project state as of 2026-09-26

### What exists

Sub-projects 1 to 9 of `docs/superpowers/specs/2026-09-22-roadmap.md` are implemented,
followed by eleven private alpha builds. The latest package is
`builds/MangakaStudio-0.8.0-private-alpha.11-Windows.zip` (see
`docs/superpowers/alpha-11-build-verification.md`).

1. Simulation core: clock, staged chapter pipeline (Name, Pencils, Inks, Backgrounds,
   Tones), planner, recaps, deterministic save/load.
2. Publishing and market: magazines, pitches, editor review, serialization, rankings,
   tankobon sales, cancellation, doujin, genre trends.
3. Staff and studio: hiring, wellbeing, teams, separate personal/business finances,
   credit, incorporation, properties and branches, printing, conventions, promotion,
   career moves.
4. 3D office: furniture ownership and editing, Auto-arrange, ambient residents,
   timelapse trails.
5. Historical timeline and rivals: analogue manga, dated industry news to 2025 then a
   simulated future, rival studios, scouting, digital/overseas deals.
6. Management UI: office home screen, inbox, charts, saves, showcases, Helper-Chan
   portraits, 3D model, desk and optional story.
7. Awards, anime/merch licensing, career honours, difficulty presets, Sandbox with the
   permanent achievement lock (no-op platform adapter for now).
8. Private alpha: adaptive Helper-Chan guidance, standalone doujin opening, ambience,
   full-history saves, local problem-report export.
9. GUI redesign: persistent left rail, floating panels over a full-canvas office,
   16:9 and 21:9 layouts, dark/light themes, text scaling, compact density.

Alpha follow-ups since then (records in `docs/superpowers/private-alpha-feedback-*.md`,
`family-home-refinement.md`, `character-integration-and-day-length.md`,
`ui-ux-sweep-2026-09-25.md`, `alpha-8` to `alpha-11-build-verification.md`) include:
creator name and appearance setup, money change feedback in the header, part-time job,
free online listings, convention reservations, animated Helper-Chan and parents
(imported GLBs), modular chibi staff, family routines, WC, genkan and rear stairs,
residential surroundings, weighted chapter progress bar, screen-space nametags, and
day pacing of about 45 seconds per 10-hour working day at 8x with a 32x overnight skip.

Last recorded verification (alpha.11): warning-free build, 550 simulation tests
passing, rendered checks at 1920x1080, 2560x1080, 3440x1440 and 1280x720 with 150%
text. These are local automated and rendered checks, not clean-machine or human
playtesting.

### Gaps identified on 2026-09-26

1. **Version control.** Resolved 2026-09-26: safety commit `8f6e3f4` on `main`
   captures all work through alpha.11; `main` pushed to GitHub on 2026-09-27. The remote branch
   `origin/claude/sp2-plan-implementation-z2feyy` is an abandoned, superseded cloud
   attempt at sub-projects 2 and 3; do not merge it.
2. **Release checklist.** Resolved 2026-09-26: tiers and the Tier 1 checklist are in
   the roadmap's Release plan section.
3. **No real playtesting.** Balance values across sub-projects 3, 5 and 7 are marked
   "subject to playtesting". The feedback files record the user's own change requests,
   not external testers. The 15 to 20 minute opening target and mid-career pacing are
   unmeasured.
4. **Missing release requirements.** No music. Steam uses a no-op adapter with no app
   ID. No clean-machine or lower-end hardware testing. No store page material.
   Localization is undecided.
5. **Drift toward cosmetic detail.** Recent work concentrated on small family-home
   visuals rather than release blockers. Use the release plan to keep focus.
6. `README.md` now names alpha.11 as the latest package (fixed 2026-09-26).

### Environment notes

- Build and verification tooling is Windows based (Godot 4.7.2 .NET via WinGet,
  PowerShell scripts). Claude Code running natively on Windows is the preferred tool
  for implementation, building and rendered checks.
- Tripo 3D asset generation (`scripts/tripo-chibi-trial.ps1`, results in
  `docs/superpowers/tripo-asset-trial.md`) reads its API key from the Windows user
  environment variable `TRIPO_API_KEY`. Never write the key into files, logs or
  output. Every Tripo call spends credits and needs the user's explicit approval.
- `builds/` and `TestResults/` are git-ignored and large (several GB each). Keep useful
  evidence; do not bulk-delete.

---

## 5. Release plan and decisions (2026-09-26)

- **Goal:** Steam Early Access. Stay with the user through to that point.
- **Milestones are tiered.**
  - **Tier 1: complete core game (complete 2026-10-04).** A fully functioning game
    that does what it sets out to do, with minimal content and minimal player
    friction. Proposed completion criteria: a player who has never seen the game can
    go from a new career through a first doujin and sale, a growing readership, a
    magazine pitch, a serialization with real deadlines, a setback such as
    cancellation, and a first hire, without dead ends, confusing screens, save
    problems or outside help. No music, Steam integration or new content in this tier.
  - **Tier 2: Early Access ready (proposed).** Music and sound, native Steam
    integration and achievements, balance over 5 to 10 in-game years, clean-machine and
    lower-end hardware testing, store page and trailer material.
    Planning answers Q1 to Q11 (AI lo-fi music, about 25 achievements, balance
    for 1996 to 2006, laptops plus Steam Deck, outside testers, store art brief,
    English only, gameplay trailer, US$9.99, about 12 months of Early Access)
    are in `docs/superpowers/specs/2026-09-26-early-access-considerations.md`.
  - **Tier 3: Early Access updates (proposed).** More content, additional start dates,
    deeper late-game systems, localization, guided by player feedback.
- **Tier 1 scope (decided 2026-09-26, option 1):** systems beyond the core journey
  (awards, licensing, branches, loans, rivals, digital/overseas deals) stay in; only
  the core journey must be polished; any that cause real friction are hidden or
  deferred to Tier 2 rather than rebuilt.
- **Tier 1 checklist:** items T1.1 to T1.10 in the roadmap's Release plan section. The
  roadmap is the source of truth; tick items only with linked evidence.
- **Sub-project 10 (done 2026-09-26, automated):** guided playtest harness in
  `tests/MangakaSim.Tests/CareerPlaytest.cs` (run with `--filter Category=Playtest`,
  exclude with `Category!=Playtest`). Findings in
  `docs/superpowers/full-career-playtest-findings.md`: guidance stops after the first
  sale, the first hire is a cash trap, no setbacks ever occur and the economy is too
  generous once serialized, about 4 real hours per in-game year. These fixes are the
  next Tier 1 work.
- **Tier 1 fix 1, career guidance (done 2026-09-26, commit `22a9c93`):** one career path
  from first doujin to first hire (Q12 to Q15), shown as texts on Helper-Chan's phone
  (PHS to 2009, smartphone from 2010). Record: `docs/superpowers/career-guidance-completion.md`.
- **Tier 1 fix 2, first-hire safety (done 2026-09-26, commit `22a9c93`):** runway shown
  and confirmed below 3 months (Q16 to Q18), desk and cooldown explanations, missed-payday
  text with "Cover from savings". Record: `docs/superpowers/first-hire-safety-completion.md`.
- **Tier 1 fix 3, setbacks and economy (done 2026-09-26, commit `8d71521`):** pitch
  odds, earlier cancellation pressure, tier-scaled sales, print-run royalties,
  newcomer fee floor, warning and cancellation texts (Q19 to Q21), and the phone
  folding to its icon on cramped screens. Record:
  `docs/superpowers/setbacks-economy-completion.md`.
- **Tier 1 fix 4, difficulty that matters (done 2026-09-26, commit `f8b17b8`):**
  Recovery grace sets editor waits and protected chapters, Business pressure
  sets rival strength and pitch odds (Q22, Q23); Standard unchanged, no save
  change. Record: `docs/superpowers/difficulty-completion.md`.
- **Tier 1 fix 5, the wait before a series debuts (done 2026-09-26, commit `f8b17b8`):**
  four-issue lead time kept; Helper-Chan explains the debut and page fees,
  "x of y chapters ready" in her texts and Production, then one suggestion
  (early hire if cash lasts to the debut, convention, short doujin, part-time
  job) plus the buffer tip (Q24, Q25); no save or balance change. Record:
  `docs/superpowers/debut-wait-completion.md`.
- **Tier 1 fix 6, mid-career pacing (done 2026-09-27, commit `c6a90a4`):** a 32x
  speed (header and 1/2 keys) runs routine days without the recap and stops,
  back to the slower speed, for the Q27 list and new Helper-Chan texts; one
  introduction text after the first sale (Q26 to Q28); 8x unchanged, no save
  or balance change. About 1.1 to 1.6 real hours per in-game year instead of 4.
  Record: `docs/superpowers/mid-career-pacing-completion.md`.
- **Tier 1 fix 7, display sweep (done 2026-09-27, commit `038295f`):** T1.9 ticked.
  `--display-sweep-smoke` checks 24 core screens at 5 sizes, 100% and 150%
  text and both themes (480 checks, 0 flagged), with my own review of the
  captures (Q29, Q30). Fixed an empty Publishing page, drop-downs losing their
  text, the dashboard overlapping with large text, invisible focused-button
  text in the light theme and the cramped Publishing form on small windows.
  Record: `docs/superpowers/display-sweep-completion.md`. Next: preparing the
  fresh-player test (T1.10).
- **T1.10 fresh-player test (preparation done and alpha.12 packaged 2026-09-27, not committed; round 1 not run):**
  unmoderated home tests (Q31) with a session timeline attached to the problem
  report (Q32); testers reach the first hire, then play on up to three in-game
  years, with a practice save if no setback happens (Q33); two rounds, two
  testers on alpha.12 then one new tester on alpha.13 (Q34). T1.1 now reads
  "reaches each of" instead of "in order". Record:
  `docs/superpowers/specs/2026-09-27-fresh-player-test-considerations.md`. Plan:
  `docs/superpowers/plans/2026-09-27-fresh-player-test.md`. Preparation record
  (timeline, practice career, `--journey-smoke`, tester kit, recap and Publishing
  fixes): `docs/superpowers/fresh-player-test-preparation-completion.md`.
- **Tier 1 closeout and tester A fixes (2026-09-28, not committed):** tester A
  returned (findings A1 to A9 in `docs/superpowers/fresh-player-test-findings.md`,
  triage approved). Fixed A1 contest guidance dead end, A2 32x closing pages,
  A4 staff waiting status, A5 unusable buttons, A8 redactor, A9 unloadable save
  after a revised contest manuscript; T1.6 old saves, T1.7 error catcher, T1.8
  clarity fixes. A3 (progressive disclosure, incl. rival job offers), A6, A7
  wait for tester B. Record: `docs/superpowers/tier1-closeout-and-tester-a-completion.md`.
  Next: option 2, the music system (user chose "1, then 2").
- **Music system (committed `f9e8ac6`; 13 Suno Pro tracks added 2026-09-28):**
  spec `docs/superpowers/specs/2026-09-28-music-system-design.md` (Q35 gentle,
  Q36 quiet stretches), plan `docs/superpowers/plans/2026-09-28-music-system.md`.
  `MusicPlan` and `MusicMoments` (engine-free), `godot/Office/MusicPlayer.cs`,
  `--music-smoke`, `scripts/convert-music.ps1`. Record:
  `docs/superpowers/music-system-completion.md`.
- **Start-up disclaimer and volume (2026-09-28):** silent AI-assets disclaimer
  every launch (Q37, Q39 wording), first-launch volume screen with Master (default
  0), Music and Sound effects on Godot buses, per-computer
  `user://audio-settings.json`, "Play sound even while unfocused" (off). Career
  volume fields kept but unused. `--startup-smoke`, `--first-launch`. Spec
  `docs/superpowers/specs/2026-09-28-startup-flow-and-volume-design.md`; record
  `docs/superpowers/startup-flow-and-volume-completion.md`.
- **Title screen and pause menu (2026-09-28):** full-screen title screen with
  the placeholder studio art and logo (Q42), fades into and out of careers, a
  small pause menu with Quit to title (Q40), and a safety save before leaving a
  career or closing the window. Name decided: Mangaka Days (Q41). Store, title
  and logo art now come from NovelAI V5 (`docs/superpowers/novelai-prompts.md`).
  Spec `docs/superpowers/specs/2026-09-28-title-screen-and-pause-menu-design.md`;
  record `docs/superpowers/title-screen-and-pause-menu-completion.md`.
- **In-game UI restyle (2026-09-29):** the logo's palette in an "evening
  studio" dark theme and a "manuscript paper" light theme, logo slab buttons
  (gold main actions, quiet ordinary buttons, mint for where you are), Lilita
  One for buttons and headings (Q44 to Q47). Spec
  `docs/superpowers/specs/2026-09-29-brand-ui-restyle-design.md`; record
  `docs/superpowers/brand-ui-restyle-completion.md`.
- **Tester B (2026-10-01):** returned (B1 to B8 in the findings file, triage
  approved). Fixed B1 idle hire (assistants take Backgrounds and Tones, Q52),
  B4 pitch cooldown, B5 Continue after an import, B7 furnishing; night skip on
  days with no work fixed too. Next: the career goals board (spec
  `docs/superpowers/specs/2026-10-01-career-goals-design.md`, awaiting review),
  then A3. Records: `docs/superpowers/tester-b-fixes-completion.md`,
  `docs/superpowers/quiet-day-night-skip-fix.md`.
- **Career goals board (2026-10-02):** five career chapters (Doujin Days,
  Rookie, Serialized, Studio Head, Legend) plus mastery goals, with progress
  bars, one-off rewards (cash, fans, decorations, unlocks, opportunities,
  Helper-Chan scenes) and How? tips (Q48 to Q51). Spec
  `docs/superpowers/specs/2026-10-01-career-goals-design.md`; record
  `docs/superpowers/career-goals-completion.md`.
- **Progressive disclosure, A3 (2026-10-03):** a new career shows only what
  Doujin Days needs; Books, Publishing, Contests, Staff, Studios, Industry,
  business money and 32x open at their own moment or chapter, with a "New" tag
  and one Helper-Chan line; "Experienced player: show every screen"; rival job
  offers to Aki wait for Industry (Q53 to Q57). Spec
  `docs/superpowers/specs/2026-10-02-progressive-disclosure-design.md`; record
  `docs/superpowers/progressive-disclosure-completion.md`. Next: Tier 1 closeout.
- **Work feedback, right-click back, shorter music gaps (2026-10-03):** sparkles
  from workers into the header progress bar and "Pencils done!" bubbles
  (presentation only, off with reduced motion); a right click without a drag
  steps back to the 3D office; quiet between music tracks 15 to 30 seconds and
  track changes in the problem report timeline (Q58). Record:
  `docs/superpowers/work-feedback-and-right-click-completion.md`.
- **Studio island and corner (2026-10-03):** Helper-Chan's desk faces the spare
  desk across an island headed by Aki's desk (Q59), older untouched homes move
  over on load, and the freed alcove holds manga tools and storage. Record:
  `docs/superpowers/studio-island-completion.md`.
- **Streaming sales and the selling tutorial (2026-10-03):** book sales now
  stream through shop hours, 10:00 to 20:00 (Q60), instead of a Monday lump;
  a book on sale midweek sells at once, weekly totals unchanged. Helper-Chan
  teaches selling by showing it happen (Q61): a first-sale text, a pulsing Sold
  count, a first-copy bubble and a sell-more text. Conventions after a midweek
  release keep week-two demand (Q62). Spec
  `docs/superpowers/specs/2026-10-03-streaming-sales-design.md`; record
  `docs/superpowers/streaming-sales-completion.md`.
- **Display settings (2026-10-04, tester C findings C2 and C3):** one
  per-computer interface size (Automatic follows Windows scaling, 80% to 200%,
  capped to keep a 1280 x 720 layout; Q64), fullscreen or windowed with window
  sizes, F11 or Alt+Enter, on the first-launch screen, title and in-game
  Settings; the per-career text scale is retired; the 3D office renders at full
  window pixels. Spec `docs/superpowers/specs/2026-10-04-display-settings-design.md`;
  record `docs/superpowers/display-settings-completion.md`. Next: re-check C1
  (the opening) against the current build.
- **Committed 2026-10-04:** the work from 29 Sep to 4 Oct is on `main` in
  feature commits after `274741a`. Still uncommitted and unbuilt: the opening
  quick start (Q65 option 1, record `docs/superpowers/quick-start-completion.md`,
  which also carries the last display settings files) and the Tier 1 closeout
  with A6 and A7 (record `docs/superpowers/tier1-closeout-completion.md`). See
  `docs/superpowers/session-handoff.md`.
- **Tier 1 closeout (2026-10-04, Q53):** T1.1, T1.3, T1.4 and T1.8 ticked
  with evidence; T1.10 moved to Tier 2 as a stranger playtest; T1.2 and T1.5
  signed off by the user (Q66) and ticked after the quick start (Q65), so
  **Tier 1 is complete**. A6 licence terms explained on the offer card
  (presentation only, compiled, not yet rendered); A7
  closed with no new work (caused by A1). Record:
  `docs/superpowers/tier1-closeout-completion.md`.
- **Tier 2 start, alpha.13 (2026-10-05):** rail subtitle now マンガカ・デイズ,
  roadmap T1.3 and T1.4 boxes ticked, A6 offer card rendered at 1600 x 900
  (1280 x 720 large size still unchecked), tester kit renamed to Mangaka Days
  with a C1 question, alpha.13 packaged for one new tester (C1). Tier 2 order
  (recommended 2026-10-05): tester and the user's trademark search, Steamworks
  and about 25 achievements (10 exist), controller and Steam Deck, ten-year
  balance, sound effects, store page and Steam Playtest, hardware testing,
  trailer. Record: `docs/superpowers/alpha-13-build-verification.md`.
- **Sound effects (2026-10-05, Q67 option 1 "for now"):** ten CC0 recordings
  from Kenney and Freesound replace the procedural placeholders (room tone,
  pencil strokes, page turns, click, phone buzz), cut by
  `scripts/convert-sfx.ps1`, sources in `godot/Assets/Sfx/README.md`. No AI
  audio, so the Steam AI disclosure is unchanged. Builds clean, alpha and
  start-up smokes pass; not yet heard in the game. Branch
  `claude/project-thread-pzzl18`. Record:
  `docs/superpowers/sound-effects-completion.md`.
- **Steam achievements (2026-10-05, Q68 Steamworks.NET, Q69 list):** 25
  achievements (the ten older ones plus the Q3 draft, with ten on-time chapters
  and 1,000,000 career copies as the gentler targets), checked daily on the
  save so older saves catch up, quiet in the inbox. `godot/Platform/SteamAchievements.cs`
  sends them to Steam; `--steam` connects to Valve's test app 480 in
  development, and `ReleaseAppId` stays 0 until the Steam Direct fee is paid,
  so packages never touch Steam. Steamworks.NET 2025.164.1 and
  `steam_api64.dll` live in `godot/ThirdParty/Steamworks.NET`. Unit tests,
  progression, journey, management and `--steam-smoke` pass; no package
  built. Next for this item: app ID, Steamworks entries and 50 icons. Record:
  `docs/superpowers/steam-achievements-completion.md`.
- **Ten-year balance playtest (2026-10-05):** the guided playtest now runs to
  2006 and follows the goals board. Found an online-download runaway (seed 42
  reaches about ¥48 trillion), struggling careers stuck in a yearly
  cancellation loop, and an empty middle game for successful ones. All four
  fixes made and re-run (coordinator's call): runaway gone, hits peak and
  fade, struggling careers escape but only after 7.5 to 9.5 years, middle game
  still empty; Aki's story milestones recommended as the Tier 3 item to pull
  forward, awaiting the coordinator. Branch `claude/project-thread-kpkoc9`, not
  merged. Record: `docs/superpowers/ten-year-balance-findings.md`.
- Keep this section in sync with the roadmap
  (`docs/superpowers/specs/2026-09-22-roadmap.md`).

---

## 6. Where things live

- `src/MangakaSim`: engine-free, deterministic C# simulation. `GameState.Advance(hours)`
  drives time; `GameState.Apply(command)` handles player changes.
- `tests/MangakaSim.Tests`: xUnit unit, regression, replay, save/load and balance tests.
- `godot`: management and debug scenes, real-time driver, office, smoke checks.
- `docs/superpowers/specs`: roadmap, designs, `*-considerations.md` decision records,
  `*-research.md` research ledgers.
- `docs/superpowers/plans`: implementation plans.
- `docs/superpowers/*-completion.md`, `*-build-verification.md`,
  `private-alpha-feedback-*.md`: delivery and verification records.
- `docs/superpowers/references`, `godot/Assets`, `godot/Office/ASSETS.md`: approved art
  references and asset provenance.
- `scripts/`: packaging (`package-alpha.ps1`), screenshot conversion, asset trials.

## 7. Commands (PowerShell, repository root; only when building is authorized)

```powershell
dotnet test tests/MangakaSim.Tests
dotnet build MangakaGame.sln -warnaserror
$godot = Join-Path $env:LOCALAPPDATA 'Microsoft\WinGet\Packages\GodotEngine.GodotEngine.Mono_Microsoft.Winget.Source_8wekyb3d8bbwe\Godot_v4.7.2-stable_mono_win64\Godot_v4.7.2-stable_mono_win64_console.exe'
& $godot --headless --editor --path godot --import --quit
& $godot --headless --path godot -- --smoke-test
& $godot --headless --path godot -- --management-smoke
& $godot --path godot -- --management-smoke --capture   # rendered AVIF captures
scripts/package-alpha.ps1 -Godot $godot                  # packaging
```

See the Validate section of `README.md` and `docs/superpowers/private-alpha-testing.md`
for the full smoke-check list, capture flags and packaging steps.
