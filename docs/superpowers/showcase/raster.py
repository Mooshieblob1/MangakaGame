import sys, pathlib
sys.path.insert(0, str(pathlib.Path(__file__).parent.parent / "pylib"))
import pypdfium2 as pdfium
from PIL import Image
pdf = pdfium.PdfDocument("MangakaDays-GDD.pdf")
imgs = [p.render(scale=1.1).to_pil() for p in pdf]
for i, im in enumerate(imgs, 1):
    im.save(f"pages/p{i:02d}.png")
# contact sheets of 4 pages side by side
w, h = imgs[0].size
for s in range(0, len(imgs), 4):
    sheet = Image.new("RGB", (w*4 + 30, h), "white")
    for k, im in enumerate(imgs[s:s+4]):
        sheet.paste(im, (k*(w+10), 0))
    sheet.save(f"pages/sheet{s//4+1}.png")
print(len(imgs), w, h)
