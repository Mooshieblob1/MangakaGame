# Steam store art brief

Date: 2026-09-26. Decision: Tier 2 question 7, see the
[considerations](specs/2026-09-26-early-access-considerations.md). The user
generates the art; this brief says what each piece must show and how big it
must be.

Update 2026-09-28 (later): change of plan again. The user will generate the
store art, logo artwork and title screen art with NovelAI Diffusion V5 and
disclose AI use openly; the commission briefs are shelved. Prompts come from the
user's `novelai-v5-prompter` skill.

Earlier update 2026-09-28: the user intended to commission these pieces, the logo and
the title screen art from a designer; see the
[art commission brief](logo-designer-brief.md). The rules and sizes below still
apply. Check the sizes against the Steamworks graphical assets page on the day
of upload, since Valve changes them from time to time.

## Direction

Helper-Chan is the featured mascot. Use her approved reference design
([helper-chan.png](references/helper-chan.png)): adult office worker, very long
blonde drill twintails, black glasses, white blouse, pencil skirt, lanyard and
clipboard. That design already carries the appeal; keep it confident,
playful and fully clothed.

Rules for every piece:

- Draw Helper-Chan in her full-proportion reference style, never as a
  sexualised chibi. The in-game chibi versions appear only in cute,
  non-suggestive roles.
- No nudity, underwear, see-through clothing or poses framed on the chest or
  hips. Art like that must be declared in Steam's content survey, puts the game
  behind the mature content filter for many shoppers and promises a game we
  are not making.
- Always show that this is a manga studio game: manga pages, pens, screentone,
  a drawing desk, the chibi office, or late-1990s Tokyo details (CRT monitor,
  fax machine, overhead wires, a train crossing).
- Capsules may contain only artwork and the game name. No review quotes, award
  text, prices or "Early Access" banners.
- Keep the key subject and logo inside the central 80% so crops still work.
- Deliver PNG at the exact size. Record the tool, date and prompt for each
  piece in the provenance table below.

## Pieces

| File | Size (px) | What it shows |
|---|---|---|
| `header-capsule` | 920x430 | Main shot: Helper-Chan in front with clipboard raised, an empty drawing desk behind (half-inked page, pen set down), logo readable. Most-seen image; the logo must read at a glance. |
| `small-capsule` | 462x174 | Mostly logo, with Helper-Chan's face and a twintail. Test it at actual size; tiny detail vanishes. |
| `main-capsule` | 1232x706 | Wider version of the header: Helper-Chan, the empty drawing desk, a glimpse of the Tokyo street through the window, logo. |
| `vertical-capsule` | 748x896 | Helper-Chan full figure with twintails filling the height, logo at top or bottom. |
| `library-capsule` | 600x900 | Same idea as the vertical capsule, logo included. |
| `library-header` | 920x430 | Can reuse the header capsule. |
| `library-hero` | 3840x1240 | Wide scene with **no logo and no text**: the studio at dusk, desks, manga pages, Tokyo skyline outside. Keep faces and key detail inside the central 860x380 safe area (Steamworks, checked 2026-09-28); the rest gets cropped as the client scales. |
| `library-logo` | 1280 wide or 720 high | "Mangaka Days" logo alone on a transparent background. |
| `page-background` | 1438x810 | Optional. Soft, dark, low-contrast studio or skyline texture; store text sits on top of it. |
| `community-icon` | 184x184 | Helper-Chan's face with glasses, simple and bold. |
| `client-icon` | 32x32 ICO and 256x256 PNG | Same face, simplified further. |
| `event-cover` | 800x450 | Template for update announcements: Helper-Chan holding the clipboard with space for text on one side. |
| `event-header` | 1920x622 | Wide version of the event cover. |

## Screenshots (made by me in-game, not generated)

At least five at 1920x1080, taken from the real game once Tier 1 art is final.
Steam requires screenshots to show actual gameplay, so these are captures, not
artwork. Planned set: the office home screen, the chapter pipeline mid-production,
a convention sale, a magazine ranking with a serialized series, and the studio
after a move with staff at work. A 21:9 capture can be added as extra.

## Provenance

| File | Tool | Date | Prompt | Notes |
|---|---|---|---|---|
| header-capsule | | | | |
