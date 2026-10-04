# NovelAI V5 prompts: store art, title screen and logo

Date: 2026-09-28. Built with the user's `novelai-v5-prompter` skill. Target: V5 Full,
kept safe with `rating:general` in the prompt and `nsfw` in Undesired Content.
Helper-Chan is read from `references/helper-chan.png`. Vibe Transfer and Precise
Reference are not on V5 yet, so the text carries her whole look; keep the
Character Block identical everywhere so she stays consistent.

Commercial use was checked on 2026-10-04: NovelAI's terms let us use these
images on the Steam store page and title screen, with Steam's AI disclosure.
See `specs/2026-10-04-novelai-commercial-terms-research.md`.

## How the pieces fit

| # | Image | Used for |
|---|---|---|
| 1 | Studio background, 16:9 | Title screen, key art backdrop, library hero, every capsule background, event cover, store page background (blurred and darkened in an editor) |
| 2 | Helper-Chan, full body, transparent | Key art focal layer, vertical and library capsules |
| 3 | Helper-Chan, waist up, transparent | Header, main capsule, event cover |
| 4 | Chibi Helper-Chan mark, transparent | Logo mark next to the typeset wordmark, small capsule |
| 5 | Helper-Chan face icon | Community icon 184 x 184, app icon 256 and 32 |

The "MANGAKA DAYS" lettering is set in a commercially licensed font, not
generated. Generate at normal size, keep the best of 4 to 8, then Enhance or
Upscale 4x before cropping.

## Shared blocks

Style Block (in every base prompt except the chibi and the icon):

```
best quality, very aesthetic, official art, anime coloring, clean lineart, soft shading, high complexity, year 2026, -2::realistic::, rating:general
```

Character Block (Helper-Chan):

```
girl, office lady, blonde hair, absurdly long hair, twintails, twin drills, drill hair, black hair tie, blunt bangs, sidelocks, blue eyes, black-framed eyewear, rectangular eyewear, white shirt, collared shirt, sleeves rolled up, red lanyard, white id card, black skirt, pencil skirt, high-waist skirt, black pantyhose, black footwear, loafers. She is a woman in her mid-twenties. Her huge drill curls hang almost to her ankles.
```

## 1. Studio background

Base Prompt

```
no humans, scenery, best quality, very aesthetic, official art, visual novel bg, anime coloring, clean lineart, warm lighting, high complexity, year 2026, -2::realistic::, rating:general, indoors, location, cluttered room, desk, desk lamp, chair, paper, pen, ink, bookshelf, book stack, computer, monitor, cork board, calendar (object), window, sunset, evening, orange sky, purple sky, power lines, utility pole, building, cityscape, wide shot. A small, lived-in 1990s Tokyo manga artist's workroom at dusk. A tilted wooden drawing desk sits right of centre under a glowing desk lamp, covered in half-inked manga pages with pale blue guide lines, dip pens, an open ink bottle and loose sheets of dotted tone paper. Its chair is pulled out and empty. Behind the desk a large window shows a purple and orange evening sky over overhead power lines and apartment blocks. A beige CRT computer and a fax machine sit on a side desk. The room continues to the left: a dim wall with a cork board, a tall bookshelf and a side desk, all in soft evening shadow.
```

Undesired Content

```
nsfw, 1girl, 1boy, person, text, english text, watermark, signature, smartphone, flat screen monitor, laptop, blurry, border, white border, split screen, blank space, empty panel
```

Settings: Quality Tags Standard (its `no text` keeps book spines unreadable) · UC
Heavy · 1344 x 768 (16:9) · 28 / 5 / Euler Ancestral · Transparent BG off ·
Positions AI's Choice.

Notes: Heavy bans `screentone` as a drawing style, so the tone sheets are
described in words. Check the shelves for anything resembling a real manga cover.
Revised 2026-09-28: the first version asked for "open space" on the left and got a
blank brown panel with a white border, so the room now fills the frame. The title
screen's own dark gradient keeps the menu readable.

## 2. Helper-Chan, full body

Base Prompt

```
1girl, solo, best quality, very aesthetic, official art, anime coloring, clean lineart, soft shading, high complexity, year 2026, -2::realistic::, rating:general, transparent background, full body, standing, three quarter view, looking at viewer. A confident, friendly office worker stands facing the viewer, holding a clipboard up beside her face with one hand and a pen in the other, as if about to read out the day's schedule.
```

Character 1 (position about x 0.5, y 0.35)

```
girl, office lady, blonde hair, absurdly long hair, twintails, twin drills, drill hair, black hair tie, blunt bangs, sidelocks, blue eyes, black-framed eyewear, rectangular eyewear, white shirt, collared shirt, sleeves rolled up, red lanyard, white id card, black skirt, pencil skirt, high-waist skirt, black pantyhose, black footwear, loafers, confident smile, holding clipboard, holding pen. She is a woman in her mid-twenties. Her huge drill curls hang almost to her ankles.
```

Undesired Content

```
nsfw, cleavage, breast focus, ass focus, upskirt, child, aged down, heterochromia, white background, border
```

Settings: Quality Tags Standard · UC Human Focus · 768 x 1344 (tall, so the hair
fits) · 28 / 5 / Euler Ancestral · Transparent BG on · Positions Custom (as above).

Notes: if the drill curls come out short, try `1.2::twin drills, absurdly long
hair::`. If her age reads young, strengthen the mid-twenties sentence rather than
adding body tags.

## 3. Helper-Chan, waist up

Base Prompt

```
1girl, solo, best quality, very aesthetic, official art, anime coloring, clean lineart, soft shading, high complexity, year 2026, -2::realistic::, rating:general, transparent background, cowboy shot, three quarter view, looking at viewer. A cheerful office worker raises her clipboard with one hand and points her pen toward the left side of the picture with the other, inviting the viewer in.
```

Character 1 (position about x 0.55, y 0.3)

```
girl, office lady, blonde hair, absurdly long hair, twintails, twin drills, drill hair, black hair tie, blunt bangs, sidelocks, blue eyes, black-framed eyewear, rectangular eyewear, white shirt, collared shirt, sleeves rolled up, red lanyard, white id card, black skirt, pencil skirt, high-waist skirt, smile, open mouth, holding clipboard, holding pen, pointing. She is a woman in her mid-twenties.
```

Undesired Content

```
nsfw, cleavage, breast focus, child, aged down, heterochromia, white background, border
```

Settings: Quality Tags Standard · UC Human Focus · 1024 x 1024 · 28 / 5 / Euler
Ancestral · Transparent BG on · Positions Custom.

Notes: the pantyhose and shoe tags are left out so the framing stays at the
waist; everything else matches the Character Block.

## 4. Chibi logo mark

Base Prompt

```
1girl, solo, best quality, very aesthetic, chibi, visual novel chibi, flat color, thick lineart, outline, white outline, low complexity, year 2026, rating:general, transparent background, upper body, looking at viewer. A cute chibi mascot of a blonde office worker with black glasses, winking and holding up a clipboard, drawn as a bold sticker with a thick white outline, simple enough to read at small sizes.
```

Character 1 (position about x 0.5, y 0.4)

```
girl, blonde hair, twintails, twin drills, drill hair, black hair tie, blunt bangs, sidelocks, blue eyes, black-framed eyewear, rectangular eyewear, white shirt, collared shirt, red lanyard, white id card, smile, one eye closed, holding clipboard
```

Undesired Content

```
nsfw, scenery, detailed background, realistic, heterochromia
```

Settings: Quality Tags Standard · UC Heavy · 1024 x 1024 · 28 / 5 / Euler
Ancestral · Transparent BG on · Positions Custom.

Notes: the mark sits beside the wordmark, so leave the lettering out here. If it
comes out too detailed for icon sizes, add `-1::shading::` to the base prompt.

## 5. Face icon

Base Prompt

```
1girl, solo, best quality, very aesthetic, flat color, thick lineart, low complexity, year 2026, rating:general, portrait, close-up, looking at viewer, simple background, teal background, centered. A bold, simple app icon of a blonde woman's face with black glasses and a friendly smile, her twin drill curls framing both sides of the picture.
```

Character 1 (position about x 0.5, y 0.45)

```
girl, blonde hair, twintails, twin drills, blunt bangs, sidelocks, blue eyes, black-framed eyewear, rectangular eyewear, smile, white shirt, collared shirt
```

Undesired Content

```
nsfw, text, detailed background, heterochromia
```

Settings: Quality Tags Standard · UC Heavy · 1024 x 1024 · 28 / 5 / Euler
Ancestral · Transparent BG off · Positions Custom.

Notes: check it shrunk to 32 x 32; the glasses and blonde silhouette should still
read.

## 6. Logo with lettering (wide and stacked)

V5 renders English text, so this tries the whole logo in NovelAI. Quality Tags
must be Off (Standard adds `no text`), and UC Light, since Heavy bans `logo`.
Check every letter; if the lettering garbles after a few tries, keep the mark and
typeset the words instead.

Wide, Base Prompt

```
1girl, solo, logo, best quality, very aesthetic, masterpiece, chibi, flat color, thick lineart, outline, white outline, low complexity, year 2026, rating:general, transparent background, text, english text, sticker. A game title logo that reads "MANGAKA DAYS" on one line in big, chunky, rounded bold letters with a thick dark outline and a white sticker border around the whole design. "MANGAKA" is warm golden yellow and "DAYS" is mint teal. A small chibi mascot peeks out from the left end of the lettering, winking and holding up a clipboard. Text: MANGAKA DAYS
```

Stacked, Base Prompt

```
1girl, solo, logo, best quality, very aesthetic, masterpiece, chibi, flat color, thick lineart, outline, white outline, low complexity, year 2026, rating:general, transparent background, text, english text, sticker. A square game title logo with "MANGAKA" on the top line and "DAYS" on the line below, in big, chunky, rounded bold letters with a thick dark outline and a white sticker border around the whole design. "MANGAKA" is warm golden yellow and "DAYS" is mint teal. A small chibi mascot stands to the left of both lines, winking and holding up a clipboard. Text: MANGAKA

DAYS
```

Character 1 (both; wide about x 0.15, y 0.5; stacked about x 0.22, y 0.55)

```
girl, chibi, blonde hair, twintails, twin drills, drill hair, black hair tie, blunt bangs, sidelocks, blue eyes, black-framed eyewear, rectangular eyewear, white shirt, collared shirt, red lanyard, white id card, smile, one eye closed, holding clipboard
```

Undesired Content (both)

```
nsfw, watermark, signature, scenery, detailed background, realistic, heterochromia, blurry, lowercase
```

Settings: Quality Tags Off · UC Light · wide 1344 x 768, stacked 1024 x 1024 ·
28 / 5 / Euler Ancestral · Transparent BG on · Positions Custom.

Notes: the dark outline plus white border lets one version work on light and
dark backgrounds. In the stacked prompt the blank line after `Text: MANGAKA` is a
Shift+Enter, which makes "DAYS" a separate piece of text.

## Variants

- **1990s anime look** (matches the 1996 setting): replace `year 2026` with
  `year 1997, retro artstyle, 1990s (style)` in every base prompt.
- **V5 Curated:** remove `-2::realistic::` and `location`, keep the tags lean,
  and expect a steadier Helper-Chan with a little less background detail.

## Results log

- 2026-09-28, first round: full body (#2), waist up (#3), chibi mark (#4) and
  face icon (#5) kept; real alpha on the cutouts, clean hair edges on dark. The
  background (#1) came out as a lovely 1990s cel-style room on the right with a
  blank panel on the left; regenerate with the revised prompt.
- 2026-09-28, second round (revised background prompt): five rooms the user
  liked, all filling the frame with no border. Checked at full size: no readable
  text or real covers. Strongest: the tilted drafting board version (most clearly
  a manga workroom) and the fax-and-cork-board version (richest period detail).
  Title screen: the drafting board room, chosen by the user 2026-09-28. Waiting
  for the 4x upscaled PNG export to replace the Gemini placeholder.
- 2026-09-28: the 1344 x 768 drafting board room installed as
  `godot/Assets/Branding/title-background.png`, replacing the Gemini placeholder
  (the user asked for their NovelAI image). Replaced the same day by the user's NovelAI upscale, 2688 x 1536.
- 2026-09-28, logo (prompt 6): six stacked results, all spelled correctly. The
  user chose the one with clean bold letters and Helper-Chan clear of the
  lettering; upscaled to 2000 x 2000, faint alpha noise cleared and cropped to
  1712 x 581, now `godot/Assets/Branding/logo.png`.
