# Tier 2: Early Access ready, considerations

Date: 2026-09-26.
Status: planning questions Q1 to Q11 answered on 2026-09-26. Tier 2 work starts
after Tier 1 is signed off; these answers only plan it ahead.

## Decisions

### Q1. Music source: AI-generated (decided 2026-09-26)

The user chose AI-generated music. The game already uses AI-generated art
assets, so the music keeps the same production approach.

Conditions that come with this choice:

- Use a music service whose plan grants commercial use of the output, and check
  its terms on the day the tracks are generated. Tracks made on a plan without
  commercial rights are not used in the game.
- Record the service, plan, date and prompt for every track in the asset
  provenance notes, as is done for art in `godot/Office/ASSETS.md`.
- The Steam store page must disclose AI-generated content in Steam's content
  survey. This covers art and music together.
- Every generation that spends credits or needs a paid plan needs the user's
  explicit approval first.
- Keep tracks swappable, so a single cue can be replaced later without code
  changes if a track turns out weak or its rights are unclear.

### Q2. Music style: modern lo-fi study beats (decided 2026-09-26)

The user chose option 2 over the recommended late-1990s Japanese pop sound,
accepting that lo-fi beats are a 2010s style. The user generates the tracks in
Suno from prompts that lean towards dusty late-1990s jazz-hop textures, which
keeps some period feel. Prompts, cue list and provenance table:
[music-suno-prompts.md](../music-suno-prompts.md). Wiring music into the game
is Tier 2 work and waits until Tier 1 is signed off.

### Q3. Achievements: 20 to 30 career milestones (decided 2026-09-26)

About 25 achievements tied to the career journey, no challenge or oddity
achievements at launch. Sandbox saves stay permanently ineligible.

Ten already exist in `ProgressionCatalog.Achievements` and are kept:
first publication, contest placement, annual award, anime release, anime
follow-up, merchandise licence, 10,000 readers, 1,000,000 readers, iconic
series and sustainable studio.

Proposed additions (draft, to confirm when Tier 2 starts):

| Key | Requirement |
|---|---|
| first_sale | Sell your first copy. |
| first_convention | Sell copies at a convention. |
| serialization | Accept a magazine serialization offer. |
| ranking_top3 | Reach the top three in a magazine ranking. |
| deadline_streak | Deliver twelve magazine chapters in a row without a missed deadline. |
| comeback | Win a new serialization after a series is cancelled. |
| first_hire | Hire your first assistant. |
| full_team | Employ four assistants at once. |
| move_out | Move into a studio outside the family home. |
| incorporate | Incorporate your business. |
| second_studio | Run two studios at once. |
| copies_100k | Sell 100,000 copies of one series. |
| copies_10m | Sell 10,000,000 copies across your career. |
| overseas_deal | Sign a digital or overseas deal. |
| ten_years | Still creating manga ten years after your start date. |

`comeback` depends on cancellation being reachable, which is a Tier 1 balance
fix from the playtest findings.

**Q69 (decided 2026-10-05): the draft above with two gentler targets.**
`deadline_streak` needs ten chapters in a row instead of twelve, and
`copies_10m` became `copies_1m` (1,000,000 career copies), since balance past
year five is untested. Steamworks.NET was chosen as the library (Q68). Record:
[steam-achievements-completion.md](../steam-achievements-completion.md).

### Q4. Career length: open-ended, balanced for 1996 to 2006 (decided 2026-09-26)

The career stays open-ended through the dated timeline to 2025 and the simulated
future. Balance and playtesting effort targets the first ten in-game years,
about 25 to 40 hours of play at current pacing. Later years remain playable and
are described as less tuned during Early Access. The automated career playtest
extends to ten years for this work, and mid-career pacing fixes from Tier 1
change the hour estimate.

### Q5. Minimum hardware: integrated-graphics laptops plus Steam Deck (decided 2026-09-26)

Target a recent integrated-graphics laptop (Intel Iris Xe or AMD Radeon
integrated class), 8 GB memory, Windows 10 or 11 64-bit, 1080p at a steady
30 frames per second or better, and Steam Deck at its native 1280x800. Add a
low-quality graphics setting if needed (shadows and effects off).

The user chose Steam Deck for Tier 2 knowing it adds substantial work:

- Full controller support for every management screen, including focus
  order, a visible focus highlight, on-screen button prompts and text entry
  through the Steam on-screen keyboard.
- A 16:10 layout at 1280x800 with readable text (Valve's guidance is text no
  smaller than about 9 pixels high at that resolution), in addition to 16:9
  and 21:9.
- Running on Proton (the Windows build) and passing Valve's Deck
  compatibility review, aiming for Verified and accepting Playable.
- Testing on a real Steam Deck; how to obtain one is still to be arranged,
  as is a real low-end laptop.

### Q6. Test hardware: outside testers (decided 2026-09-26)

No hardware is bought. Low-end laptop and Steam Deck testing relies on outside
testers who already own that hardware (a small friends-and-family beta), with
Valve's Deck compatibility review as the final Deck check.

Consequences:

- The game needs a performance and system report testers can export in one
  step, building on the existing local problem-report export: frame rate over
  time, graphics device, memory, resolution and settings.
- Testers get a short written test script so reports are comparable.
- The same testers can serve as fresh players for Tier 1 item T1.10.
- Every build shared with testers is an external release and needs the user's
  approval each time. Once a Steam app ID exists, Steam's playtest or beta
  branch is the preferred way to distribute.

### Q7. Store art: generated by the user, Helper-Chan featured (decided 2026-09-26)

The user generates the store art, with Helper-Chan as the featured mascot and an
intentionally eye-catching style. Agreed limits: she appears in her adult
reference design and stays fully clothed; no nudity or sexualised chibi art,
which would trigger Steam's mature content filter and misrepresent a cosy
management game; every piece shows the manga studio setting. Screenshots are
real in-game captures made by Claude. Sizes, contents and provenance table:
[store-art-brief.md](../store-art-brief.md). Store art counts toward the AI
content disclosure from Q1.

### Q8. Languages: English only at launch (decided 2026-09-26)

Early Access launches in English only. Japanese, and possibly Simplified
Chinese, follow during Early Access once the game's text has settled, so
nothing is translated twice. Localization stays in Tier 3 as the roadmap
already says.

To keep later translation cheap, new player-facing text should go through one
central place rather than being scattered through code. Moving existing text
into a string table is a Tier 3 task, sized when localization is scheduled.
The store page lists English as the only supported language until then.

### Q9. Trailer: 60 to 90 seconds of real gameplay (decided 2026-09-26)

Claude captures the trailer in-game once Tier 1 visuals are final: a
Helper-Chan opening, then a career from the first doujin through a convention
sale, a magazine ranking and a busy studio, cut to one of the Suno tracks.
It is remade cheaply whenever the game changes noticeably. A produced trailer
with generated art is left for a later update or the full release.

### Q10. Early Access price: US$9.99 (decided 2026-09-26)

The user chose the low price over the recommended US$14.99, favouring an easy
purchase decision while the game is still rough. Accepted tradeoffs: lower
revenue per sale, less room for launch discounts and a "small game" signal.
Whether and by how much the price rises at full release is decided later; any
planned rise should be stated on the store page from the start. The price can
still change before launch.

### Q11. Early Access length: about 12 months (decided 2026-09-26)

The store page states about 12 months. That window covers the Tier 3 work
(Japanese, more start dates, deeper late-game systems) plus changes driven by
player feedback. If the plan changes, tell players in an update well before
the date passes.
