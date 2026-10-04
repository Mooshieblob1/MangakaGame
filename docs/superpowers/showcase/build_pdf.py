"""Builds an A4 print copy of the GDD page and renders it to PDF with headless Edge.

Pass 1 renders with placeholder page numbers, reads which page each chapter starts
on, then pass 2 renders again with the real numbers in the contents page.
"""
import json, re, subprocess, sys, pathlib

HERE = pathlib.Path(__file__).parent
sys.path.insert(0, str(HERE.parent / "pylib"))
from pypdf import PdfReader  # noqa: E402

EDGE = r"C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe"
SRC = HERE / "index.html"
OUT_HTML = HERE / "print.html"
OUT_PDF = HERE / "MangakaDays-GDD.pdf"

CHAPTERS = [
    ("vision", "構想", "Vision"),
    ("helper", "相棒", "Companion"),
    ("journey", "道のり", "Journey"),
    ("goals", "目標", "Goals"),
    ("production", "制作", "Production"),
    ("publishing", "出版", "Publishing"),
    ("money", "資金", "Money"),
    ("studio", "仕事場", "Studio"),
    ("era", "時代", "Era"),
    ("screens", "画面", "Interface"),
    ("sound", "音", "Sound"),
    ("difficulty", "難易度", "Difficulty"),
    ("tech", "技術", "Technology"),
    ("roadmap", "発売", "Roadmap"),
]

PRINT_CSS = r"""
/* ================= A4 print edition ================= */
@page{
  size:A4; margin:14mm 16mm 17mm; background:#F3EEE3;
  @bottom-left{content:"Mangaka Days  /  Game Design Document";font:500 7pt "IBM Plex Mono",monospace;letter-spacing:.08em;text-transform:uppercase;color:#7A7C83;vertical-align:top;padding-top:5mm}
  @bottom-right{content:counter(page) " / " counter(pages);font:500 7pt "IBM Plex Mono",monospace;color:#15171C;vertical-align:top;padding-top:5mm}
}
@page cover{margin:0;background:#0F1116;
  @bottom-left{content:none} @bottom-right{content:none}}
*{-webkit-print-color-adjust:exact;print-color-adjust:exact}
html,body{background:#F3EEE3}
body{overflow:visible}
.wrap{max-width:none;padding:0}

/* ---------- cover ---------- */
.hero{page:cover;height:296mm;padding:22mm 17mm 0;break-after:page;display:flex;flex-direction:column}
.hero .wrap{flex:1;display:flex;flex-direction:column}
.hero h1{font-size:104px}
.jp-v{font-size:34px}
.lede{font-size:16.5px;margin:24px 0 30px;max-width:none}
.hero-shot img{display:block;height:auto;aspect-ratio:2200/928}
.hero-shot figcaption{bottom:-24px;font-size:10.5px}
.facts{grid-template-columns:repeat(3,minmax(0,1fr));margin-top:auto;margin-bottom:14mm}
.facts dd{font-size:13.5px}
.cover-foot{position:absolute;left:17mm;right:17mm;bottom:9mm;display:flex;justify-content:space-between;
  font:500 10px "IBM Plex Mono",monospace;letter-spacing:.1em;text-transform:uppercase;color:#56606d}

/* ---------- interior scale ---------- */
.toc,main{zoom:.71}
.chapter{padding:0;break-before:page}
.chapter + .chapter{border-top:0}
.spine .jp{position:static}
.block{margin-top:40px}
.cap,.panel figcaption{font-size:12.5px}
.note{font-size:14.5px}
h2{font-size:48px;break-after:avoid}
h3,h4,.eyebrow{break-after:avoid}
.cap{break-before:avoid}
.panel,figure,.tbl,tr,.pillars,.rules,.tombo,.phone-row,.arc,.journey li,.speed,.pipe,.math,.covers,
.formula,.pockets,.pocket,.callout,.timeline,.checks,.jcard,.tiers,.stats,.cols>div,.cols-3>div,
.helper-top>div,.checklist li,ul.plain li,.chips,footer{break-inside:avoid}
.formula{white-space:normal}
.tbl{overflow:visible}
.journey{margin-top:30px}
.journey li{padding-bottom:24px}
.covers img{transition:none}
.panel img{display:block;height:auto}
.pocket .amt{font-size:29px}

#roadmap .callout{margin-top:18px}
#roadmap .block{margin-top:22px}
#roadmap .tiers{margin-top:24px}
#roadmap .checklist li{padding:7px 0}

/* ---------- contents ---------- */
.toc{break-after:page}
.toc .ch-grid{min-height:0}
.toc ol{list-style:none;margin:34px 0 0;padding:0;border-top:3px solid var(--rule)}
.toc li{border-bottom:1.5px solid var(--rule)}
.toc a{display:grid;grid-template-columns:44px 96px minmax(0,1fr) 54px;align-items:center;gap:0 18px;padding:13px 0;text-decoration:none}
.toc .num{font:500 12px "IBM Plex Mono",monospace;color:var(--red);letter-spacing:.06em}
.toc .kj{font-family:"Dela Gothic One";font-size:25px;line-height:1}
.toc .t b{display:block;font-size:17px;line-height:1.3}
.toc .t span{font-size:14px;color:var(--ink-2)}
.toc .pg{font-family:"Dela Gothic One";font-size:24px;text-align:right}
.toc .lead{margin-top:30px;display:grid;grid-template-columns:repeat(3,minmax(0,1fr));border:3px solid var(--rule);background:var(--card)}
.toc .lead div{padding:16px 18px;border-right:1.5px solid var(--rule)}
.toc .lead div:last-child{border-right:0}
.toc .lead b{display:block;font:500 11px "IBM Plex Mono",monospace;letter-spacing:.08em;text-transform:uppercase;color:var(--ink-3);margin-bottom:4px}
.toc .lead span{font-size:14.5px}

/* ---------- back cover ---------- */
footer{page:cover;break-before:page;margin:0;border:0;height:296mm;padding:0 17mm 20mm;
  background:radial-gradient(rgba(241,233,214,.07) 1.2px,transparent 1.4px) 0 0/7px 7px,#0F1116;
  display:flex;flex-direction:column;justify-content:flex-end}
footer .wrap{display:block}
footer .big{font-size:88px;color:#F1E9D6;line-height:.95}
footer .big::after{content:"マンガカ・デイズ";display:block;font-size:22px;letter-spacing:.3em;color:#E3B341;margin-top:18px}
footer p{font-size:13px;color:#8B93A0;max-width:520px;margin-top:26px;padding-top:20px;border-top:1.5px solid #3A3F4A}
"""


def strip_width_media_queries(css: str) -> str:
    """Removes @media (max-width: ...) blocks so the desktop layout is kept on paper."""
    out, i = [], 0
    pattern = re.compile(r"@media\s*\(max-width:[^)]*\)\s*\{")
    while True:
        m = pattern.search(css, i)
        if not m:
            out.append(css[i:])
            break
        out.append(css[i:m.start()])
        depth, j = 1, m.end()
        while depth:
            if css[j] == "{":
                depth += 1
            elif css[j] == "}":
                depth -= 1
            j += 1
        i = j
    return "".join(out)


def h2_texts(html: str) -> dict:
    result = {}
    for cid, _, _ in CHAPTERS:
        m = re.search(r'<section class="chapter" id="%s">.*?<h2>(.*?)</h2>' % cid, html, re.S)
        result[cid] = re.sub(r"<[^>]+>", "", m.group(1)).strip()
    return result


def toc_html(titles: dict, pages: dict) -> str:
    items = []
    for n, (cid, kj, en) in enumerate(CHAPTERS, 1):
        items.append(
            f'<li><a href="#{cid}"><span class="num">{n:02d}</span>'
            f'<span class="kj" lang="ja">{kj}</span>'
            f'<span class="t"><b>{en}</b><span>{titles[cid]}</span></span>'
            f'<span class="pg">{pages.get(cid, "00")}</span></a></li>'
        )
    return f"""
<section class="toc">
  <div class="wrap ch-grid">
    <div class="spine"><div class="jp" lang="ja">目次<small lang="en">Contents</small></div></div>
    <div>
      <p class="eyebrow">Game Design Document, showcase edition</p>
      <h2>Contents</h2>
      <p class="intro">Fourteen chapters, from the pitch to the road to Steam Early Access. Each chapter opens on a new page.</p>
      <ol>{''.join(items)}</ol>
      <div class="lead">
        <div><b>Edition</b><span>4 October 2026</span></div>
        <div><b>Build</b><span>0.8.0 private alpha 12 and later work</span></div>
        <div><b>Status</b><span>Tier 1 complete</span></div>
      </div>
    </div>
  </div>
</section>
"""


def build(pages: dict) -> None:
    html = SRC.read_text(encoding="utf-8")
    titles = h2_texts(html)
    style_end = html.index("</style>")
    css = strip_width_media_queries(html[:style_end])
    html = css + PRINT_CSS + html[style_end:]
    html = re.sub(r'<header class="bar">.*?</header>\s*', "", html, flags=re.S)
    html = re.sub(r"<script>.*?</script>\s*", "", html, flags=re.S)
    html = html.replace(' loading="lazy"', "")
    html = html.replace("<title>", '<html lang="en" data-theme="light"><title>', 1)
    html = html.replace(
        "  </div>\n</section>\n\n<main>",
        '  </div>\n  <div class="cover-foot"><span>Game Design Document</span><span>Showcase edition</span></div>\n</section>\n'
        + toc_html(titles, pages) + "\n<main>",
        1,
    )
    OUT_HTML.write_text(html, encoding="utf-8")


def render() -> None:
    OUT_PDF.unlink(missing_ok=True)
    subprocess.run([
        EDGE, "--headless=new", "--disable-gpu", "--no-pdf-header-footer",
        "--virtual-time-budget=15000", "--run-all-compositor-stages-before-draw",
        f"--print-to-pdf={OUT_PDF}", OUT_HTML.as_uri(),
    ], check=True, timeout=180, capture_output=True)


def chapter_pages() -> dict:
    html = SRC.read_text(encoding="utf-8")
    titles = h2_texts(html)
    reader = PdfReader(str(OUT_PDF))
    texts = [re.sub(r"\s+", " ", p.extract_text() or "") for p in reader.pages]
    pages = {}
    for cid, _, _ in CHAPTERS:
        probe = re.sub(r"\s+", " ", titles[cid])[:18]
        for idx, t in enumerate(texts):
            if idx >= 2 and probe in t:
                pages[cid] = f"{idx + 1:02d}"
                break
    return pages, len(texts)


if __name__ == "__main__":
    build({})
    render()
    pages, count = chapter_pages()
    build(pages)
    render()
    pages2, count2 = chapter_pages()
    print(json.dumps({"pages": count2, "chapters": pages2, "stable": pages == pages2}, indent=1))
