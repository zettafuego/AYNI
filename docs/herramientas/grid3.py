from PIL import Image, ImageDraw, ImageFont
import os

BASE = r"C:\Users\Eduardo\AYNI-Unity\docs\referencias\cajamarca"
files = [
    r"fachadas\puerta-cajamarca-b.jpg",
    r"calles\calle-amalia-puga-b.jpg",
    r"monumentos\catedral-fachada.jpg",
    r"monumentos\cuarto-rescate.jpg",
    r"monumentos\belen-pileta.jpg",
    r"fachadas\puerta-edificio-2mayo-a.jpg",
]
f = ImageFont.truetype(r"C:\Windows\Fonts\segoeui.ttf", 15)
cells = []
for rel in files:
    im = Image.open(os.path.join(BASE, rel)).convert("RGB")
    im.thumbnail((520, 520))
    d = ImageDraw.Draw(im)
    for k in range(1, 10):
        x, y = im.width * k / 10, im.height * k / 10
        d.line([(x, 0), (x, im.height)], fill=(255, 0, 0))
        d.line([(0, y), (im.width, y)], fill=(255, 0, 0))
        d.text((x + 2, 2), str(k), font=f, fill=(255, 0, 0))
        d.text((2, y + 1), str(k), font=f, fill=(255, 0, 0))
    cells.append((rel, im))

W, H = 2 * 540 + 10, 3 * 560 + 10
s = Image.new("RGB", (W, H), (250, 250, 250))
d = ImageDraw.Draw(s)
for i, (rel, im) in enumerate(cells):
    r, c = divmod(i, 2)
    x, y = 10 + c * 540, 10 + r * 560
    s.paste(im, (x, y))
    d.text((x, y + im.height + 4), rel, font=f, fill=(0, 0, 0))
out = r"C:\Users\Eduardo\AYNI-Unity\docs\herramientas\grid3.png"
s.save(out)
print(out, s.size)
