# Brand assets

## MD monogram (2026-10-05)

A small "MD" mark for the Discord server icon and social avatars, drawn in the
logo's sticker lettering: gold M (`#f0c878` on `#b88048`) and mint D (`#a8f8e0`
on `#50c0b0`), slab ink outline `#4b2c2a` and a white sticker edge, in Lilita
One thickened to match the logo's heavy letters.

- `md-monogram-1024-transparent.png`: 1024 x 1024, transparent background.
- `md-monogram-1024-dark.png`: 1024 x 1024 on the "evening studio" background
  (`#2e2426` to `#221a1c`), sized to stay whole when cropped to a circle.
- `md-monogram-512/256/128/32-dark.png`: smaller copies of the dark version.
- `md-monogram-options.png`: the three layouts offered (A side by side, chosen
  by Blob on 2026-10-05; B stacked like the logo; C interlocked), circle-cropped at 512,
  128, 64 and 32 px.

Regenerate with `python scripts/make-md-monogram.py OUT_DIR a` (Pillow and
NumPy). This is a stand-in until the commissioned logo and icons exist (see
`docs/superpowers/logo-designer-brief.md`); it has not been uploaded anywhere.

## Social banners (2026-10-05)

The title screen studio art, softened and darkened, with the full logo, the
line "A manga career sim. Tokyo, 1996." and "Coming to Steam" in mint. All in
`banners/`:

- `x-header-1500x500.png`: X header. The text sits right of the profile
  picture and clear of the strips phones crop.
- `bluesky-banner-3000x1000.png`, plus a `.jpg` under 1 MB for Bluesky's
  upload limit. Same layout as X.
- `youtube-banner-2560x1440.png`: logo and text inside the 1546 x 423 centre
  that every device shows; TVs show the whole image.
- `discord-banner-960x540.png`: server banner (needs Boost level 2).
- `discord-invite-splash-1920x1080.png`: invite background (Boost level 1).

TikTok and Instagram have no banners; use the MD monogram as the avatar.
Regenerate with `python scripts/make-social-banners.py OUT_DIR --guides`
(the guides copies show the safe areas). Both pictures are NovelAI art, so the
AI disclosure in the marketing plan applies; swap in the commissioned art when
it exists.
