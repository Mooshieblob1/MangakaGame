"""Mangaka Days social banners: title studio art, logo and a short tagline.

Usage: python scripts/make-social-banners.py OUT_DIR [--guides]
Needs Pillow and NumPy. --guides also writes copies with the safe areas drawn.
"""
import os, sys
import numpy as np
from PIL import Image, ImageDraw, ImageFilter, ImageFont

ROOT = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..")
ART = os.path.join(ROOT, "godot", "Assets", "Branding", "title-background.png")
LOGO = os.path.join(ROOT, "godot", "Assets", "Branding", "logo.png")
FONT = os.path.join(ROOT, "godot", "Assets", "Fonts", "LilitaOne-Regular.ttf")

WASH = (0x22, 0x1A, 0x1C)
TEXT = (0xF6, 0xEA, 0xD2)
MINT = (0xA8, 0xF8, 0xE0)
INK = (0x4B, 0x2C, 0x2A)

TAGLINE = "A manga career sim. Tokyo, 1996."
SUBLINE = "Coming to Steam"

# name: size, focus of the art crop (0..1 of art height), safe box (x0, y0, x1, y1)
# as fractions of the banner, and covered boxes (avatars) drawn in guide copies.
BANNERS = {
    # X header: avatar covers the lower left; top and bottom crop a little on phones.
    "x-header-1500x500": dict(size=(1500, 500), focus=0.52,
                              safe=(0.30, 0.12, 0.97, 0.88),
                              covered=[(0.0, 0.62, 0.24, 1.0)]),
    # Bluesky banner: 3:1, avatar overlaps the lower left.
    "bluesky-banner-3000x1000": dict(size=(3000, 1000), focus=0.52,
                                     safe=(0.28, 0.10, 0.97, 0.90),
                                     covered=[(0.0, 0.66, 0.16, 1.0)]),
    # YouTube: 2560 x 1440 master, only the centre 1546 x 423 shows on every device.
    "youtube-banner-2560x1440": dict(size=(2560, 1440), focus=0.55,
                                     safe=(0.198, 0.353, 0.802, 0.647),
                                     covered=[]),
    # Discord server banner (needs Boost level 2) and invite splash (level 1).
    "discord-banner-960x540": dict(size=(960, 540), focus=0.55,
                                   safe=(0.08, 0.14, 0.92, 0.86), covered=[]),
    "discord-invite-splash-1920x1080": dict(size=(1920, 1080), focus=0.55,
                                            safe=(0.20, 0.22, 0.80, 0.78), covered=[]),
}


def cover(art, size, focus):
    w, h = size
    scale = max(w / art.width, h / art.height)
    a = art.resize((round(art.width * scale), round(art.height * scale)), Image.LANCZOS)
    top = int(np.clip(focus * a.height - h / 2, 0, a.height - h))
    left = (a.width - w) // 2
    return a.crop((left, top, left + w, top + h))


def shade(bg, safe):
    """Soften and darken the art, darkest behind the logo so it reads."""
    w, h = bg.size
    bg = bg.filter(ImageFilter.GaussianBlur(max(1.5, w / 1400)))
    rgb = np.asarray(bg.convert("RGB"), np.float32)
    cx = (safe[0] + safe[2]) / 2 * w
    cy = (safe[1] + safe[3]) / 2 * h
    rx = (safe[2] - safe[0]) / 2 * w * 1.15
    ry = (safe[3] - safe[1]) / 2 * h * 1.6
    yy, xx = np.mgrid[0:h, 0:w].astype(np.float32)
    d = np.sqrt(((xx - cx) / rx) ** 2 + ((yy - cy) / ry) ** 2)
    k = (0.38 + 0.30 * np.clip(1.2 - d, 0, 1))[..., None]  # 0.38 everywhere, up to 0.68 at centre
    rgb = rgb * (1 - k) + np.array(WASH, np.float32) * k
    return Image.fromarray(rgb.clip(0, 255).astype(np.uint8), "RGB").convert("RGBA")


def shadowed_text(img, xy, text, font, fill, anchor="mm"):
    sh = Image.new("RGBA", img.size, (0, 0, 0, 0))
    d = ImageDraw.Draw(sh)
    sw = max(2, font.size // 9)
    d.text(xy, text, font=font, fill=INK + (255,), anchor=anchor, stroke_width=sw, stroke_fill=INK + (255,))
    off = max(2, font.size // 14)
    sh2 = Image.new("RGBA", img.size, (0, 0, 0, 0))
    sh2.alpha_composite(sh, (0, off))
    img.alpha_composite(sh2)
    img.alpha_composite(sh)
    ImageDraw.Draw(img).text(xy, text, font=font, fill=fill + (255,), anchor=anchor)


def banner(art, logo, size, focus, safe, **_):
    w, h = size
    img = shade(cover(art, size, focus), safe)
    sx0, sy0, sx1, sy1 = safe[0] * w, safe[1] * h, safe[2] * w, safe[3] * h
    sw, shh = sx1 - sx0, sy1 - sy0
    # logo takes ~68% of the safe height, text the rest
    lh = shh * 0.66
    lw = lh * logo.width / logo.height
    if lw > sw * 0.96:
        lw = sw * 0.96
        lh = lw * logo.height / logo.width
    lg = logo.resize((round(lw), round(lh)), Image.LANCZOS)
    t1 = ImageFont.truetype(FONT, max(14, round(shh * 0.115)))
    t2 = ImageFont.truetype(FONT, max(12, round(shh * 0.085)))
    gap = shh * 0.045
    total = lh + gap + t1.size + gap * 0.7 + t2.size
    y = sy0 + (shh - total) / 2
    cx = (sx0 + sx1) / 2
    img.alpha_composite(lg, (round(cx - lw / 2), round(y)))
    y += lh + gap
    shadowed_text(img, (cx, y + t1.size / 2), TAGLINE, t1, TEXT)
    y += t1.size + gap * 0.7
    shadowed_text(img, (cx, y + t2.size / 2), SUBLINE, t2, MINT)
    return img.convert("RGB")


def guides(img, safe, covered):
    g = img.convert("RGBA")
    o = Image.new("RGBA", g.size, (0, 0, 0, 0))
    d = ImageDraw.Draw(o)
    w, h = g.size
    lw = max(2, w // 500)
    d.rectangle([safe[0] * w, safe[1] * h, safe[2] * w, safe[3] * h], outline=(80, 255, 120, 255), width=lw)
    for c in covered:
        d.ellipse([c[0] * w + lw * 4, c[1] * h, c[2] * w, c[1] * h + (c[2] - c[0]) * w],
                  fill=(255, 60, 60, 110), outline=(255, 60, 60, 255), width=lw)
    g.alpha_composite(o)
    return g.convert("RGB")


if __name__ == "__main__":
    out = sys.argv[1] if len(sys.argv) > 1 else "."
    os.makedirs(out, exist_ok=True)
    art = Image.open(ART).convert("RGB")
    logo = Image.open(LOGO).convert("RGBA")
    logo = logo.crop(logo.getbbox())
    for name, spec in BANNERS.items():
        img = banner(art, logo, **spec)
        img.save(os.path.join(out, name + ".png"), optimize=True)
        if "--guides" in sys.argv:
            guides(img, spec["safe"], spec["covered"]).save(os.path.join(out, name + "-guides.png"))
        print("done", name)
