# Game design showcase

The showcase edition of the Mangaka Days game design document: a one-page web
version and an A4 print copy.

- Web version: the claude.ai artifact https://claude.ai/artifact/1RzY8Bp37TiZP2E3nXMAk8
  (source `index.html` with `img/`).
- Print copy: `MangakaDays-GDD.pdf`, 23 A4 pages.
- Edition: 4 October 2026. It describes private alpha 12 plus the work since
  (career goals, progressive disclosure, streaming sales, work feedback, studio
  island, display settings, quick start and the Tier 1 sign-off). It replaces
  the 27 September "Mangaka Studio" edition based on alpha.11.

## Rebuilding the PDF

`build_pdf.py` turns `index.html` into an A4 print page (`print.html`) and prints
it with headless Microsoft Edge in two passes, so the contents page gets real
page numbers. It needs Python with `pypdf`. `raster.py` renders the pages to
PNG contact sheets in `pages/` for checking (needs `pypdfium2` and Pillow).
`print.html` and `pages/` are working files and are not kept.

```powershell
python docs/superpowers/showcase/build_pdf.py
```

Screenshots come from `TestResults` captures (display sweep, studio island,
selling and work feedback checks, 2026-10-03 and 2026-10-04) converted to JPEG.
The studio preview images are older captures cropped to the 3D view.
