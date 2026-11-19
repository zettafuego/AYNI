"""Genera los puestos de mercado de las calles de Cajamarca.

Tres puestos (frutas, flores y comida) dibujados en 2D para que acompañen a
las vendedoras, que son sprites de la campesina. El puesto se dibuja con el
centro transparente: la vendedora se coloca detras y se le ve el torso y la
cara entre la mesa y el toldo.

Salida: Assets/Resources/Calle/Puestos/{frutas,flores,comida}.png
Vista previa: docs/previas-calle-20261028/puestos.png
"""

from __future__ import annotations

import math
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw, ImageFilter

ROOT = Path(r"C:\Users\Eduardo\AYNI-Unity")
OUT = ROOT / "Assets/Resources/Calle/Puestos"
PREVIEW = ROOT / "docs/previas-calle-20261028"
CAMPESINA = ROOT / "Assets/Resources/Battle/Personajes/campesina/Quieto/00.png"

S = 1024  # se dibuja al doble y se reduce para suavizar bordes

OUTLINE = (52, 30, 18, 255)
WOOD = (156, 96, 48, 255)
WOOD_DARK = (92, 52, 24, 255)
WOOD_LIGHT = (198, 142, 82, 255)
CLOTH_RED = (186, 48, 42, 255)
CLOTH_CREAM = (244, 230, 198, 255)
CLOTH_YELLOW = (238, 186, 52, 255)
CLOTH_GREEN = (52, 118, 72, 255)
CLOTH_BLUE = (52, 102, 158, 255)
CLOTH_PURPLE = (118, 62, 148, 255)
METAL = (150, 156, 158, 255)
METAL_DARK = (92, 100, 104, 255)
TERRACOTTA = (176, 92, 52, 255)
BURLAP = (190, 158, 108, 255)
BURLAP_DARK = (136, 106, 66, 255)


def wood_post(d: ImageDraw.ImageDraw, x0: int, y0: int, x1: int, y1: int) -> None:
    d.rectangle([x0, y0, x1, y1], fill=WOOD, outline=OUTLINE, width=7)
    d.line([x0 + 12, y0 + 10, x0 + 12, y1 - 8], fill=WOOD_LIGHT, width=6)
    d.line([x1 - 10, y0 + 14, x1 - 10, y1 - 12], fill=WOOD_DARK, width=6)


def wood_panel(d: ImageDraw.ImageDraw, x0: int, y0: int, x1: int, y1: int) -> None:
    d.rectangle([x0, y0, x1, y1], fill=WOOD, outline=OUTLINE, width=9)
    step = 118
    for x in range(x0 + step, x1, step):
        d.line([x, y0 + 8, x, y1 - 8], fill=WOOD_DARK, width=6)
    for i in range(14):
        x = x0 + 20 + (i * 71) % (x1 - x0 - 40)
        y = y0 + 24 + (i * 53) % (y1 - y0 - 48)
        d.line([x, y, x + 26, y + 4], fill=WOOD_DARK, width=3)


def canopy(d: ImageDraw.ImageDraw, accent: tuple[int, int, int, int]) -> None:
    # Toldo con dos aguas y valance festoneado.
    d.polygon([(6, 18), (S - 6, 18), (S, 120), (0, 120)], fill=accent, outline=OUTLINE)
    d.line([(0, 120), (S, 120)], fill=OUTLINE, width=9)
    for i in range(0, S, 128):
        d.polygon([(i, 18), (i + 64, 18), (i + 58, 120), (i + 6, 120)],
                  fill=CLOTH_CREAM if (i // 128) % 2 == 0 else accent)
        d.line([(i, 18), (i + 6, 120)], fill=OUTLINE, width=5)
    # Festones
    for i in range(0, S, 128):
        color = CLOTH_CREAM if (i // 128) % 2 == 0 else accent
        d.ellipse([i - 4, 96, i + 124, 176], fill=color, outline=OUTLINE, width=7)
    # Remate superior
    d.line([(6, 18), (S - 6, 18)], fill=OUTLINE, width=10)


def table(d: ImageDraw.ImageDraw) -> None:
    # Sombra
    d.ellipse([40, 950, 984, 1022], fill=(18, 10, 6, 100))
    # Patas traseras
    wood_post(d, 210, 700, 266, 986)
    wood_post(d, 758, 700, 814, 986)
    # Panel frontal
    wood_panel(d, 136, 690, 888, 980)
    # Tablón
    d.rectangle([104, 636, 920, 706], fill=WOOD_LIGHT, outline=OUTLINE, width=10)
    for i in range(7):
        y = 648 + i * 9
        d.line([120 + i * 12, y, 900 - i * 8, y], fill=WOOD, width=4)
    # Mantel andino sobre el canto
    d.rectangle([104, 690, 920, 742], fill=CLOTH_RED, outline=OUTLINE, width=7)
    for i in range(8):
        x = 150 + i * 96
        d.polygon([(x, 706), (x + 18, 724), (x, 742), (x - 18, 724)],
                  fill=CLOTH_YELLOW if i % 2 == 0 else CLOTH_GREEN)
    d.line([(104, 742), (920, 742)], fill=OUTLINE, width=6)


def basket(d: ImageDraw.ImageDraw, cx: int, top: int, w: int, h: int,
           fruits: list[tuple[int, int, int, int]]) -> None:
    d.ellipse([cx - w // 2, top, cx + w // 2, top + 34], fill=BURLAP, outline=OUTLINE, width=6)
    d.polygon([(cx - w // 2, top + 12), (cx + w // 2, top + 12),
               (cx + w // 2 - 12, top + h), (cx - w // 2 + 12, top + h)],
              fill=BURLAP, outline=OUTLINE)
    for i in range(4):
        y = top + 26 + i * (h - 30) // 4
        d.line([(cx - w // 2 + 8, y), (cx + w // 2 - 8, y)], fill=BURLAP_DARK, width=4)
    for fx, fy, r, color in fruits:
        d.ellipse([cx + fx - r, top + fy - r, cx + fx + r, top + fy + r],
                  fill=color, outline=OUTLINE, width=4)
        d.arc([cx + fx - r + 4, top + fy - r + 4, cx + fx + r - 4, top + fy + r - 4],
              200, 330, fill=(255, 255, 255, 110), width=4)


def fruit_stand(d: ImageDraw.ImageDraw) -> None:
    basket(d, 270, 548, 190, 96, [(-38, 12, 26, (236, 128, 34, 255)),
                                   (10, 4, 27, (232, 112, 26, 255)),
                                   (52, 16, 25, (240, 140, 40, 255)),
                                   (-8, 40, 24, (212, 92, 20, 255))])
    basket(d, 520, 540, 200, 104, [(-44, 14, 28, (188, 36, 40, 255)),
                                    (4, 4, 30, (204, 44, 46, 255)),
                                    (52, 16, 27, (170, 28, 34, 255)),
                                    (-12, 42, 26, (150, 24, 30, 255))])
    basket(d, 770, 552, 176, 92, [(-30, 10, 24, (150, 190, 54, 255)),
                                   (14, 6, 26, (132, 176, 44, 255)),
                                   (46, 22, 22, (166, 200, 62, 255))])
    # Sandía en rodajas
    d.ellipse([610, 584, 706, 680], fill=(58, 132, 62, 255), outline=OUTLINE, width=6)
    d.ellipse([622, 596, 694, 668], fill=(226, 70, 74, 255))
    for i in range(6):
        a = math.radians(20 + i * 26)
        x = 658 + math.cos(a) * 24
        y = 632 + math.sin(a) * 24
        d.ellipse([x - 5, y - 5, x + 5, y + 5], fill=(40, 28, 18, 255))
    # Plátanos colgando
    for i in range(4):
        d.arc([860, 560 + i * 12, 944, 640 + i * 12], 200, 340,
              fill=(242, 206, 62, 255), width=15)
        d.arc([860, 560 + i * 12, 944, 640 + i * 12], 200, 340, fill=OUTLINE, width=3)
    # Sacos al pie
    for x in (160, 830):
        d.ellipse([x - 60, 900, x + 60, 990], fill=BURLAP, outline=OUTLINE, width=7)
        d.line([x - 30, 918, x + 30, 918], fill=BURLAP_DARK, width=6)


def flower_stand(d: ImageDraw.ImageDraw) -> None:
    for i, (cx, color) in enumerate(((270, (206, 52, 58, 255)), (520, (238, 178, 52, 255)),
                                     (770, (140, 74, 168, 255)))):
        d.rectangle([cx - 54, 592, cx + 54, 706], fill=METAL, outline=OUTLINE, width=6)
        d.ellipse([cx - 54, 576, cx + 54, 620], fill=METAL_DARK, outline=OUTLINE, width=6)
        for k in range(7):
            a = -1.35 + k * 0.45
            x2 = cx + math.cos(a) * 54
            y2 = 540 - abs(math.sin(a)) * 60
            d.line([cx, 600, x2, y2], fill=(52, 118, 62, 255), width=6)
            r = 15 + (k % 3) * 4
            d.ellipse([x2 - r, y2 - r, x2 + r, y2 + r], fill=color, outline=OUTLINE, width=4)
            d.ellipse([x2 - r // 2, y2 - r // 2, x2 + r // 2, y2 + r // 2],
                      fill=CLOTH_YELLOW)
    # Manojo sobre la mesa
    for k in range(9):
        x = 380 + k * 30
        y = 630 - (k % 3) * 12
        d.line([x, 660, x + 8, y - 30], fill=(52, 118, 62, 255), width=5)
        d.ellipse([x - 10, y - 46, x + 16, y - 20], fill=CLOTH_RED if k % 2 else CLOTH_YELLOW,
                  outline=OUTLINE, width=3)
    # Macetas
    for x in (160, 840):
        d.polygon([(x - 46, 906), (x + 46, 906), (x + 34, 986), (x - 34, 986)],
                  fill=TERRACOTTA, outline=OUTLINE, width=7)
        for k in range(4):
            d.line([x - 18 + k * 12, 906, x - 26 + k * 18, 858], fill=(52, 118, 62, 255), width=5)
            d.ellipse([x - 34 + k * 18, 846, x - 10 + k * 18, 870], fill=CLOTH_PURPLE)


def food_stand(d: ImageDraw.ImageDraw) -> None:
    # Olla grande humeante
    d.ellipse([300, 560, 560, 700], fill=(72, 64, 60, 255), outline=OUTLINE, width=8)
    d.ellipse([316, 548, 544, 620], fill=(96, 86, 80, 255), outline=OUTLINE, width=7)
    d.ellipse([330, 560, 530, 604], fill=(206, 120, 58, 255))
    for i in range(4):
        x = 380 + i * 34
        d.arc([x - 30, 430 - i * 10, x + 30, 560 - i * 10], 200, 340,
              fill=(246, 240, 224, 120), width=10)
    # Choclos
    for i in range(4):
        x = 610 + i * 40
        d.ellipse([x, 596 + (i % 2) * 8, x + 34, 690 + (i % 2) * 8],
                  fill=(238, 200, 62, 255), outline=OUTLINE, width=5)
        for k in range(4):
            d.line([x + 8, 610 + k * 18, x + 26, 610 + k * 18], fill=(196, 156, 36, 255), width=3)
        d.polygon([(x + 6, 690), (x + 28, 690), (x + 22, 716), (x + 12, 716)],
                  fill=(78, 128, 62, 255), outline=OUTLINE)
    # Papas en un canasto
    basket(d, 820, 566, 170, 96, [(-34, 16, 26, (196, 160, 104, 255)),
                                   (8, 6, 28, (176, 138, 84, 255)),
                                   (46, 22, 24, (208, 172, 116, 255))])
    # Saco de papas
    d.ellipse([150, 890, 330, 1000], fill=BURLAP, outline=OUTLINE, width=8)
    for i in range(5):
        d.ellipse([180 + i * 28, 930, 222 + i * 28, 966], fill=(176, 138, 84, 255),
                  outline=OUTLINE, width=3)


def grain(img: Image.Image) -> Image.Image:
    arr = np.array(img).astype(np.float32)
    h, w = arr.shape[:2]
    # Luz suave desde arriba a la izquierda: el puesto deja de verse plano.
    yy = np.linspace(0.0, 1.0, h)[:, None]
    xx = np.linspace(0.0, 1.0, w)[None, :]
    light = (1.04 - 0.16 * yy) * (1.02 - 0.04 * xx)
    arr[..., :3] = np.clip(arr[..., :3] * light[..., None], 0, 255)
    rng = np.random.default_rng(7)
    noise = rng.normal(0.0, 5.5, (h, w, 1))
    arr[..., :3] = np.clip(arr[..., :3] + noise, 0, 255)
    return Image.fromarray(arr.astype(np.uint8))


def build(variant: str, accent: tuple[int, int, int, int]) -> Image.Image:
    img = Image.new("RGBA", (S, S), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    canopy(d, accent)
    wood_post(d, 82, 128, 142, 700)
    wood_post(d, 882, 128, 942, 700)
    table(d)
    if variant == "frutas":
        fruit_stand(d)
    elif variant == "flores":
        flower_stand(d)
    else:
        food_stand(d)
    img = img.resize((S // 2, S // 2), Image.LANCZOS)
    return grain(img)


def preview(stalls: dict[str, Image.Image]) -> None:
    PREVIEW.mkdir(parents=True, exist_ok=True)
    cell = 420
    columns = 4 if CAMPESINA.exists() else 3
    sheet = Image.new("RGB", (cell * columns, cell + 40), (32, 28, 30))
    draw = ImageDraw.Draw(sheet)
    for i, (name, stall) in enumerate(stalls.items()):
        thumb = stall.copy()
        thumb.thumbnail((cell - 20, cell - 30), Image.LANCZOS)
        bg = Image.new("RGBA", (cell, cell), (32, 28, 30, 255))
        bg.alpha_composite(thumb, ((cell - thumb.width) // 2, cell - thumb.height - 6))
        sheet.paste(bg.convert("RGB"), (i * cell, 0))
        draw.text((i * cell + 10, cell + 8), name, fill=(255, 255, 255))

    # Prueba de proporcion: la vendedora detras del puesto de frutas.
    if CAMPESINA.exists():
        stall = stalls["frutas"].copy()
        vendor = Image.open(CAMPESINA).convert("RGBA")
        alpha = np.array(vendor)[..., 3]
        ys, _ = np.nonzero(alpha > 12)
        visible = ys.max() - ys.min() + 1
        # El puesto mide 2.1 m; la campesina, ~1.64 m.
        target_visible = int(round(stall.height * (1.64 / 2.1)))
        scale = target_visible / visible
        vendor = vendor.resize((max(1, int(vendor.width * scale)), max(1, int(vendor.height * scale))),
                               Image.LANCZOS)
        bg = Image.new("RGBA", (cell, cell), (32, 28, 30, 255))
        s = stall.copy()
        s.thumbnail((cell - 20, cell - 20), Image.LANCZOS)
        v = vendor.copy()
        v.thumbnail((cell, cell), Image.LANCZOS)
        bg.alpha_composite(v, ((cell - v.width) // 2, cell - v.height + 2))
        bg.alpha_composite(s, ((cell - s.width) // 2, cell - s.height))
        sheet.paste(bg.convert("RGB"), (cell * 3, 0))
        draw.text((cell * 3 + 10, cell + 8), "frutas + vendedora", fill=(255, 255, 255))

    sheet.save(PREVIEW / "puestos.png")


def main() -> None:
    OUT.mkdir(parents=True, exist_ok=True)
    stalls = {
        "frutas": build("frutas", CLOTH_RED),
        "flores": build("flores", CLOTH_GREEN),
        "comida": build("comida", CLOTH_BLUE),
    }
    for name, img in stalls.items():
        img.save(OUT / f"{name}.png", optimize=True)
        print(f"{name}: {img.size[0]}x{img.size[1]}")
    preview(stalls)


if __name__ == "__main__":
    main()
