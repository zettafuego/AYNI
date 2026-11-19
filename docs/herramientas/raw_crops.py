from PIL import Image
import os

REF = r"C:\Users\Eduardo\AYNI-Unity\docs\referencias\cajamarca"
OUT = r"C:\Users\Eduardo\AYNI-Unity\docs\herramientas\out"
crops = {
    "raw_muro.png": (r"fachadas\puerta-cajamarca-b.jpg", (0.03, 0.32, 0.23, 0.80)),
    "raw_piedra.png": (r"monumentos\belen-hospital.jpg", (0.35, 0.24, 0.66, 0.60)),
    "raw_inca.png": (r"monumentos\cuarto-rescate.jpg", (0.50, 0.32, 0.86, 0.62)),
}
for name, (rel, box) in crops.items():
    im = Image.open(os.path.join(REF, rel)).convert("RGB")
    w, h = im.size
    x0, y0, x1, y1 = [int(round(v * d)) for v, d in zip(box, (w, h, w, h))]
    c = im.crop((x0, y0, x1, y1))
    c.save(os.path.join(OUT, name))
    print(name, c.size)

# hoja de contactos
ims = [Image.open(os.path.join(OUT, n)) for n in crops]
W = sum(i.width for i in ims) + 20
H = max(i.height for i in ims) + 20
s = Image.new("RGB", (W, H), (240, 240, 240))
x = 10
for i in ims:
    s.paste(i, (x, 10))
    x += i.width + 10
s.save(os.path.join(OUT, "raw_crops.png"))
print("sheet", s.size)
