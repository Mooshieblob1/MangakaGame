"""Mangaka Days "MD" monogram, drawn in the logo's sticker lettering.

Usage: python scripts/make-md-monogram.py OUT_DIR [a b c]
Needs Pillow and NumPy. Uses the game's Lilita One font and the logo palette
from docs/superpowers/specs/2026-09-29-brand-ui-restyle-design.md.
"""
import sys, os
import numpy as np
from PIL import Image, ImageDraw, ImageFont

FONT = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "godot", "Assets", "Fonts", "LilitaOne-Regular.ttf")
OUT = sys.argv[1] if len(sys.argv) > 1 else "."

GOLD, GOLD_BASE = (0xF0, 0xC8, 0x78), (0xB8, 0x80, 0x48)
MINT, MINT_BASE = (0xA8, 0xF8, 0xE0), (0x50, 0xC0, 0xB0)
INK, WHITE = (0x4B, 0x2C, 0x2A), (0xFA, 0xFA, 0xF8)
WASH, SURFACE = (0x22, 0x1A, 0x1C), (0x2E, 0x24, 0x26)

W = 3200  # working canvas


def glyph(ch, size, x, y, stroke=0):
    m = Image.new("L", (W, W), 0)
    ImageDraw.Draw(m).text((x, y), ch, font=ImageFont.truetype(FONT, size), fill=255,
                           stroke_width=stroke, stroke_fill=255, anchor="ls")
    return np.asarray(m, dtype=np.uint8)


def extrude(ch, size, x, y, stroke, depth, dx=0, step=6):
    acc = np.zeros((W, W), np.uint8)
    n = max(1, depth // step)
    for i in range(n + 1):
        t = depth * i / n
        acc = np.maximum(acc, glyph(ch, size, x + dx * i / n, y + t, stroke))
    return acc


def letter(ch, size, x, y, face, base, rim, depth, ink, white=34, bold=48):
    """Masks for one sticker letter: face, base (rim + extrusion), ink outline."""
    F = glyph(ch, size, x, y, bold)
    rim += bold
    B = extrude(ch, size, x, y, rim, depth)
    O = extrude(ch, size, x, y, rim + ink, depth)
    S = extrude(ch, size, x, y, rim + ink + white, depth)
    return dict(F=F, B=B, O=O, S=S, face=face, base=base)


def compose(letters):
    """Back-to-front letters, one white sticker outline around the whole mark."""
    img = np.zeros((W, W, 4), np.float32)

    def paint(mask, col):
        a = mask.astype(np.float32)[..., None] / 255.0
        src = np.concatenate([np.broadcast_to(np.array(col, np.float32), (W, W, 3)),
                              np.full((W, W, 1), 255.0, np.float32)], axis=2)
        img[:] = src * a + img * (1 - a)

    union = np.zeros((W, W), np.uint8)
    for L in letters:
        union = np.maximum(union, L["S"])
    paint(union, WHITE)
    for L in letters:
        paint(L["O"], INK)
        paint(L["B"], L["base"])
        paint(L["F"], L["face"])
    return Image.fromarray(img.clip(0, 255).astype(np.uint8), "RGBA")


def layout(name):
    if name == "a":  # side by side, sharing one sticker like the logo's words
        return [letter("M", 1500, 300, 1950, GOLD, GOLD_BASE, 30, 90, 30),
                letter("D", 1500, 1450, 1950, MINT, MINT_BASE, 20, 110, 30)]
    if name == "b":  # stacked like the logo: gold M over a big mint D
        return [letter("M", 1100, 860, 1330, GOLD, GOLD_BASE, 28, 70, 28),
                letter("D", 1650, 790, 2600, MINT, MINT_BASE, 22, 120, 32)]
    if name == "c":  # interlocked: M behind and raised, D overlapping its lower right
        return [letter("M", 1500, 300, 1750, GOLD, GOLD_BASE, 30, 90, 30),
                letter("D", 1500, 1230, 2180, MINT, MINT_BASE, 20, 110, 30)]
    raise ValueError(name)


def fit(mark, side, frac, circle=False):
    bbox = mark.getbbox()
    mark = mark.crop(bbox)
    extent = (mark.width ** 2 + mark.height ** 2) ** 0.5 if circle else max(mark.size)
    scale = frac * side / extent
    mark = mark.resize((round(mark.width * scale), round(mark.height * scale)), Image.LANCZOS)
    out = Image.new("RGBA", (side, side), (0, 0, 0, 0))
    out.alpha_composite(mark, ((side - mark.width) // 2, (side - mark.height) // 2))
    return out


def background(side):
    yy, xx = np.mgrid[0:side, 0:side].astype(np.float32)
    d = np.sqrt((xx - side / 2) ** 2 + (yy - side * 0.45) ** 2) / (side * 0.7)
    t = np.clip(d, 0, 1)[..., None]
    rgb = np.array(SURFACE, np.float32) * (1 - t) + np.array(WASH, np.float32) * t
    return Image.fromarray(np.concatenate([rgb, np.full((side, side, 1), 255.0)], 2)
                           .astype(np.uint8), "RGBA")


if __name__ == "__main__":
    names = sys.argv[2:] or ["a", "b", "c"]  # "a" is the chosen layout
    for n in names:
        mark = compose(layout(n))
        fit(mark, 1024, 0.92).save(os.path.join(OUT, f"md-{n}-transparent.png"))
        bg = background(1024)
        bg.alpha_composite(fit(mark, 1024, 0.88, circle=True))
        bg.convert("RGB").save(os.path.join(OUT, f"md-{n}-dark.png"))
        print("done", n)
