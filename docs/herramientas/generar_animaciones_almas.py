"""Genera los clips de animacion de las tres almas de AYNI.

Los dibujos originales de AlmaOro, AlmaRosa y AlmaAzul son tandas de imagenes
generadas por IA: cada fotograma es una pose distinta, de otro tamano y a veces
hasta girada. Reproducirlas en orden produce un parpadeo, no una animacion.

Por eso este script elige UNA pose limpia de cada alma y construye todos los
clips moviendo ese mismo dibujo con transformaciones suaves (respiracion,
balanceo, estiramiento, inclinacion, brillo y estelas). Asi el personaje se
mantiene reconocible y el movimiento se lee.

Se reescriben Quieto y Caminar; los originales quedan en
docs/backup-almas-20261028. Los PNG se guardan sin .meta nuevo: se conservan
los .meta existentes para no romper referencias y Unity crea los que falten.
"""

from __future__ import annotations

import math
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw, ImageFilter

ROOT = Path(r"C:\Users\Eduardo\AYNI-Unity\Assets\Resources\Battle\Personajes")
PREVIEW = Path(r"C:\Users\Eduardo\AYNI-Unity\docs\previas-almas-20261028")
BACKUP = Path(r"C:\Users\Eduardo\AYNI-Unity\docs\backup-almas-20261028")

# Pose base elegida a mano tras revisar las hojas de contacto originales:
# cuerpo entero, de pie, centrado y con aire alrededor para poder estirarlo.
ALMAS = {
    "AlmaOro": {"base": 20, "glow": (255, 205, 70), "spark": (255, 240, 160)},
    "AlmaRosa": {"base": 19, "glow": (255, 120, 190), "spark": (255, 205, 235)},
    "AlmaAzul": {"base": 15, "glow": (110, 165, 255), "spark": (200, 230, 255)},
}

BOTTOM_MARGIN = 6
FILL = 0.90  # alto visible respecto al lienzo, deja aire para estirar


# ---------------------------------------------------------------- utilidades

def load_frames(folder: Path) -> list[Image.Image]:
    files = sorted(folder.glob("*.png"), key=lambda p: int(p.stem))
    return [Image.open(f).convert("RGBA") for f in files]


def save_frames(folder: Path, frames: list[Image.Image]) -> None:
    folder.mkdir(parents=True, exist_ok=True)
    for i, frame in enumerate(frames):
        frame.save(folder / f"{i:02d}.png", optimize=True)

    # Borra fotogramas y .meta sobrantes de la version anterior.
    for png in sorted(folder.glob("*.png")):
        if int(png.stem) >= len(frames):
            png.unlink()
    for meta in folder.glob("*.png.meta"):
        try:
            idx = int(meta.name.split(".")[0])
        except ValueError:
            continue
        if idx >= len(frames):
            meta.unlink()


def canonical(frame: Image.Image) -> Image.Image:
    """Recorta la pose, la centra y la apoya en el borde inferior."""
    w, h = frame.size
    bbox = frame.split()[3].getbbox()
    if not bbox:
        raise ValueError("Pose base vacia")
    pose = frame.crop(bbox)

    target_h = max(1, int(round(h * FILL)))
    target_w = max(1, int(round(pose.width * target_h / pose.height)))
    pose = pose.resize((target_w, target_h), Image.LANCZOS)

    canvas = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    canvas.alpha_composite(pose, ((w - target_w) // 2, h - BOTTOM_MARGIN - target_h))
    return canvas


def anchor(im: Image.Image, sx: float, sy: float) -> Image.Image:
    """Escala manteniendo el borde inferior-centro fijo (los pies)."""
    w, h = im.size
    nw = max(1, int(round(w * sx)))
    nh = max(1, int(round(h * sy)))
    scaled = im.resize((nw, nh), Image.LANCZOS)
    left = (nw - w) // 2
    top = nh - h
    canvas = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    x0, y0 = max(0, left), max(0, top)
    x1, y1 = min(nw, left + w), min(nh, top + h)
    if x1 > x0 and y1 > y0:
        canvas.alpha_composite(scaled.crop((x0, y0, x1, y1)), (x0 - left, y0 - top))
    return canvas


def bend(im: Image.Image, lean: float = 0.0, wave: float = 0.0,
         phase: float = 0.0, power: float = 1.35) -> Image.Image:
    """Inclina la parte alta del cuerpo; la base queda clavada."""
    arr = np.array(im)
    h, w = arr.shape[:2]
    out = np.zeros_like(arr)
    for y in range(h):
        t = (h - 1 - y) / max(1, h - 1)
        dx = int(round(lean * t + wave * math.sin(phase + t * math.pi * 1.25) * t ** power))
        if dx == 0:
            out[y] = arr[y]
        elif dx > 0:
            out[y, : max(0, w - dx)] = arr[y, dx:]
        else:
            out[y, -dx:] = arr[y, : max(0, w + dx)]
    return Image.fromarray(out)


def bob(im: Image.Image, dy: int) -> Image.Image:
    if dy == 0:
        return im
    w, h = im.size
    canvas = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    canvas.alpha_composite(im, (0, dy))
    return canvas


def tint(im: Image.Image, color: tuple[int, int, int], amount: float) -> Image.Image:
    if amount <= 0:
        return im
    arr = np.array(im).astype(np.float32)
    rgb = arr[..., :3]
    alpha = arr[..., 3:] / 255.0
    col = np.array(color, dtype=np.float32)
    arr[..., :3] = np.clip(rgb * (1 - amount * alpha) + col * amount * alpha, 0, 255)
    return Image.fromarray(arr.astype(np.uint8))


def fade(im: Image.Image, amount: float) -> Image.Image:
    if amount >= 1:
        return im
    arr = np.array(im)
    arr[..., 3] = (arr[..., 3].astype(np.float32) * amount).astype(np.uint8)
    return Image.fromarray(arr)


def shift_x(im: Image.Image, dx: int) -> Image.Image:
    if dx == 0:
        return im
    w, h = im.size
    canvas = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    canvas.alpha_composite(im, (dx, 0))
    return canvas


def smear(im: Image.Image, dx: int, strength: float = 0.26, copies: int = 2) -> Image.Image:
    """Estela corta hacia atras; da peso al golpe sin ensuciar el dibujo."""
    ghosts = Image.new("RGBA", im.size, (0, 0, 0, 0))
    for i in range(1, copies + 1):
        ghost = fade(shift_x(im, -dx * i), strength * (1 - (i - 1) / (copies + 1)))
        ghosts = Image.alpha_composite(ghosts, ghost)
    return Image.alpha_composite(ghosts, im)


def glow(im: Image.Image, color: tuple[int, int, int], strength: float = 0.35,
         radius: float = 12.0) -> Image.Image:
    mask = im.split()[3].filter(ImageFilter.GaussianBlur(radius))
    mask = mask.point(lambda v: int(v * strength))
    halo = Image.new("RGBA", im.size, color + (0,))
    halo.putalpha(mask)
    return Image.alpha_composite(halo, im)


def sparkles(im: Image.Image, color: tuple[int, int, int], phase: float,
             count: int = 3) -> Image.Image:
    w, h = im.size
    overlay = Image.new("RGBA", im.size, (0, 0, 0, 0))
    draw = ImageDraw.Draw(overlay)
    bbox = im.split()[3].getbbox()
    if not bbox:
        return im
    x0, y0, x1, y1 = bbox
    spots = (
        (x0 + 0.18 * (x1 - x0), y0 + 0.24 * (y1 - y0)),
        (x1 - 0.12 * (x1 - x0), y0 + 0.42 * (y1 - y0)),
        (x0 + 0.34 * (x1 - x0), y0 + 0.08 * (y1 - y0)),
        (x1 - 0.20 * (x1 - x0), y0 + 0.70 * (y1 - y0)),
    )
    for i, (x, y) in enumerate(spots[:count]):
        pulse = 0.5 + 0.5 * math.sin(phase + i * 2.1)
        r = 1.6 + 2.2 * pulse
        a = int(90 + 100 * pulse)
        draw.ellipse((x - r, y - r, x + r, y + r), fill=color + (a,))
    return Image.alpha_composite(im, overlay)


# ------------------------------------------------------------------- clips

def make_idle(base: Image.Image, palette: dict) -> list[Image.Image]:
    """Respiracion apenas perceptible: el peso lo pone SpriteMotion."""
    out = []
    for i in range(8):
        phase = 2 * math.pi * i / 8
        wave = math.sin(phase)
        im = anchor(base, 1.0 - 0.004 * wave, 1.0 + 0.008 * wave)
        im = bend(im, lean=0.6 * wave, wave=0.8 * math.sin(phase + 1.1), phase=phase, power=1.4)
        im = bob(im, int(round(-0.5 - 0.5 * wave)))
        im = glow(im, palette["glow"], 0.05 + 0.02 * (0.5 + 0.5 * wave), 12)
        out.append(im)
    return out


def make_walk(base: Image.Image, palette: dict) -> list[Image.Image]:
    """Flotar con dos impulsos por ciclo; nada de poses nuevas."""
    out = []
    for i in range(12):
        phase = 2 * math.pi * i / 12
        step = math.cos(2 * phase)
        im = anchor(base, 1.0 - 0.008 * step, 1.0 + 0.012 * step)
        im = bend(im, lean=2.2 + 1.1 * math.sin(phase),
                  wave=1.8 * math.sin(phase), phase=phase, power=1.5)
        im = bob(im, int(round(-2.0 + 2.0 * step)))
        im = glow(im, palette["glow"], 0.05, 10)
        out.append(im)
    return out


def make_attack(base: Image.Image, palette: dict) -> list[Image.Image]:
    params = (
        # sx,   sy,   lean, wave, smear, tint, halo
        (1.02, 0.98, -4.0, -2.0, 0, 0.00, 0.00),
        (0.97, 1.04, -7.0, -4.0, 0, 0.06, 0.12),
        (1.08, 0.97, 8.0, 5.0, 9, 0.12, 0.26),
        (1.10, 0.95, 11.0, 7.0, 13, 0.20, 0.24),
        (1.03, 1.00, 5.0, 3.0, 5, 0.08, 0.12),
        (1.00, 1.00, 1.0, 1.0, 0, 0.00, 0.04),
    )
    out = []
    for sx, sy, lean, wave, blur, amt, halo in params:
        im = anchor(base, sx, sy)
        im = bend(im, lean=lean, wave=wave, phase=0.6, power=1.6)
        if blur:
            im = smear(im, blur, 0.24)
        im = tint(im, palette["glow"], amt)
        if halo:
            im = glow(im, palette["glow"], halo, 13)
        out.append(im)
    return out


def make_hurt(base: Image.Image) -> list[Image.Image]:
    params = ((1.06, 0.92, -5.0, -3.0, 0.18), (1.02, 0.96, -7.0, -2.0, 0.26), (1.00, 1.00, -1.5, 0.0, 0.06))
    out = []
    for sx, sy, lean, wave, flash in params:
        im = anchor(base, sx, sy)
        im = bend(im, lean=lean, wave=wave, phase=0.4, power=1.5)
        im = tint(im, (255, 235, 220), flash)
        out.append(im)
    return out


def make_fall(base: Image.Image, palette: dict) -> list[Image.Image]:
    """Las almas no se desploman: se apagan y se deshacen hacia arriba.

    Ocho frames a 14 fps dan ~0.57 s de disolucion; con cuatro se sentia
    demasiado brusco para una muerte.
    """
    count = 8
    out = []
    for i in range(count):
        t = i / (count - 1)
        ease = t * t * (3 - 2 * t)
        sx = 1.0 + 0.08 * ease
        sy = 1.0 - 0.42 * ease
        lean = -10.0 * ease
        alpha = 1.0 - 0.68 * (ease ** 1.2)
        rise = int(round(28 * ease))
        halo = 0.05 + 0.13 * math.sin(math.pi * t)

        im = anchor(base, sx, sy)
        im = bend(im, lean=lean, wave=-2.5 * math.sin(math.pi * t), phase=0.9, power=1.4)
        im = bob(im, -rise)
        im = glow(im, palette["glow"], halo, 14)
        im = tint(im, palette["glow"], 0.10)
        out.append(fade(im, alpha))
    return out


def make_jump(base: Image.Image, palette: dict) -> list[Image.Image]:
    """PlayJump se llama cuando el cuerpo ya sube: arranca estirado, no agachado."""
    params = ((0.96, 1.07, 1.0, 0.08), (1.00, 1.03, 0.0, 0.05), (1.02, 0.99, -1.0, 0.02))
    out = []
    for sx, sy, lean, halo in params:
        im = anchor(base, sx, sy)
        im = bend(im, lean=lean, wave=1.2, phase=0.8, power=1.3)
        if halo:
            im = glow(im, palette["glow"], halo, 10)
        out.append(im)
    return out


def make_swim(base: Image.Image, palette: dict) -> list[Image.Image]:
    out = []
    for i in range(4):
        phase = 2 * math.pi * i / 4
        im = anchor(base, 1.02, 0.98)
        im = bend(im, lean=7.0, wave=2.5 * math.sin(phase), phase=phase, power=1.5)
        im = bob(im, int(round(-3.0 - 3.0 * math.cos(phase))))
        im = tint(im, palette["glow"], 0.05)
        im = glow(im, palette["glow"], 0.12 + 0.06 * math.sin(phase), 12)
        out.append(im)
    return out


def make_happy(base: Image.Image, palette: dict) -> list[Image.Image]:
    params = ((1.02, 1.03, 5), (0.98, 0.97, 0), (1.03, 1.04, 6), (1.00, 0.99, 1))
    out = []
    for i, (sx, sy, dy) in enumerate(params):
        im = anchor(base, sx, sy)
        im = bob(im, -dy)
        im = sparkles(im, palette["spark"], i * 1.7)
        im = glow(im, palette["glow"], 0.10, 10)
        out.append(im)
    return out


def make_special(base: Image.Image, palette: dict) -> list[Image.Image]:
    out = []
    for i in range(6):
        t = i / 5.0
        pulse = 1.0 + 0.05 * math.sin(t * math.pi)
        im = anchor(base, pulse * 1.02, 1.0 + 0.03 * math.sin(t * math.pi))
        im = bend(im, lean=2.5 * math.sin(t * math.pi * 2), wave=3.5,
                  phase=t * math.pi * 2, power=1.6)
        if 1 <= i <= 3:
            im = smear(im, 7, 0.18)
        im = glow(im, palette["glow"], 0.16 + 0.20 * math.sin(t * math.pi), 16)
        im = tint(im, palette["glow"], 0.08)
        im = sparkles(im, palette["spark"], t * 6.0)
        out.append(im)
    return out


# --------------------------------------------------------------------- main

def strip(frames: list[Image.Image], path: Path, cell: int = 150) -> None:
    sheet = Image.new("RGB", (cell * len(frames), cell), (30, 28, 34))
    for i, frame in enumerate(frames):
        thumb = frame.copy()
        thumb.thumbnail((cell - 6, cell - 6), Image.LANCZOS)
        bg = Image.new("RGBA", (cell, cell), (30, 28, 34, 255))
        bg.alpha_composite(thumb, ((cell - thumb.width) // 2, cell - thumb.height - 3))
        sheet.paste(bg.convert("RGB"), (i * cell, 0))
    sheet.save(path)


def contact(char: str, clips: dict[str, list[Image.Image]]) -> None:
    cell = 170
    cols = max(len(v) for v in clips.values())
    sheet = Image.new("RGB", (cell * (cols + 1), cell * len(clips)), (30, 28, 34))
    draw = ImageDraw.Draw(sheet)
    for r, (name, frames) in enumerate(clips.items()):
        draw.text((6, r * cell + 4), name, fill=(255, 255, 255))
        for c, frame in enumerate(frames):
            thumb = frame.copy()
            thumb.thumbnail((cell - 8, cell - 8), Image.LANCZOS)
            bg = Image.new("RGBA", (cell, cell), (30, 28, 34, 255))
            bg.alpha_composite(thumb, ((cell - thumb.width) // 2, cell - thumb.height - 4))
            sheet.paste(bg.convert("RGB"), ((c + 1) * cell, r * cell))
    sheet.save(PREVIEW / f"{char}.png")


def build(char: str, palette: dict) -> None:
    root = ROOT / char
    # La pose base sale del respaldo original: asi el script se puede repetir
    # aunque el Quieto actual ya sea el generado (8 frames).
    backup = BACKUP / f"{char}-Quieto"
    source_dir = backup if backup.is_dir() else root / "Quieto"
    source = load_frames(source_dir)
    if not source:
        raise SystemExit(f"Sin frames Quieto para {char}")

    index = palette["base"]
    if index >= len(source):
        raise SystemExit(f"{char}: no existe el frame base {index}")
    base = canonical(source[index])
    print(f"{char}: base {index:02d} de {source[index].size[0]}x{source[index].size[1]}")

    clips = {
        "Quieto": make_idle(base, palette),
        "Caminar": make_walk(base, palette),
        "Ataque": make_attack(base, palette),
        "Golpe": make_hurt(base),
        "Caida": make_fall(base, palette),
        "Salto": make_jump(base, palette),
        "Nadar": make_swim(base, palette),
        "Feliz": make_happy(base, palette),
        "Especial": make_special(base, palette),
    }
    for name, frames in clips.items():
        save_frames(root / name, frames)
        print(f"  {name}: {len(frames)} frames")

    PREVIEW.mkdir(parents=True, exist_ok=True)
    contact(char, clips)
    strip(clips["Caminar"], PREVIEW / f"{char}-Caminar.png")
    strip(clips["Quieto"], PREVIEW / f"{char}-Quieto.png")


def main() -> None:
    for char, palette in ALMAS.items():
        build(char, palette)


if __name__ == "__main__":
    main()
