from PIL import Image, ImageDraw, ImageFont
import os

OUT = r"C:\Users\Eduardo\AYNI-Unity\docs\herramientas\out"
PREV = r"C:\Users\Eduardo\AYNI-Unity\docs\img\calle-texturas-nuevas.png"

names = [
    "muro_adobe.png", "piedra.png", "teja.png", "piso_plaza.png",
    "empedrado_rio.png", "cal_ocre.png", "piedra_inca.png", "madera_oscura.png",
]

def font(size):
    return ImageFont.truetype(r"C:\Windows\Fonts\segoeui.ttf", size)

cell = 270
label = 26
cols = 4
rows = (len(names) + cols - 1) // cols
W = cols * (cell + 14) + 14
H = 58 + rows * (cell + label + 14) + 14
sheet = Image.new("RGB", (W, H), (238, 236, 230))
d = ImageDraw.Draw(sheet)
d.text((14, 12), "CALLES - texturas de juego (cada celda = tile 3x3)", font=font(28), fill=(25, 25, 25))

for i, n in enumerate(names):
    im = Image.open(os.path.join(OUT, n)).convert("RGB")
    t = im.resize((cell // 3, cell // 3), Image.LANCZOS)
    tile = Image.new("RGB", (cell, cell))
    for y in range(3):
        for x in range(3):
            tile.paste(t, (x * cell // 3, y * cell // 3))
    r, c = divmod(i, cols)
    x = 14 + c * (cell + 14)
    y = 58 + r * (cell + label + 14)
    sheet.paste(tile, (x, y))
    d.rectangle([x - 1, y - 1, x + cell, y + cell], outline=(190, 186, 178))
    d.text((x, y + cell + 4), "%s  %dx%d" % (n, im.width, im.height), font=font(16), fill=(60, 60, 60))

sheet.save(PREV)
print(PREV, sheet.size)
