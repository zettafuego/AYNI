"""
Convierte las referencias de Cajamarca en texturas de juego tileables.

Las fotos no se recortan tal cual: se miden color, proporción de hiladas,
junta y desgaste, y se reconstruye el material con esas medidas. Así el tile
no arrastra ventanas, autos, avisos ni pasto, y no se nota la repetición.

Escala real: el juego mapea 2 m por tile en muros y techos, y 3.2 m por tile
en pisos (PlanFloorTile). Las proporciones de abajo respetan esa escala.

Entrada : docs/referencias/cajamarca/  (fotos CC BY-SA, ver CREDITOS.md)
Salida  : docs/herramientas/out/       (PNG sin costuras, listos para Unity)

Uso:  python docs/herramientas/texturas_cajamarca.py
"""
from PIL import Image, ImageFilter
import numpy as np
import os
from scipy.ndimage import gaussian_filter
from scipy.spatial import cKDTree

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
REF = os.path.join(ROOT, "docs", "referencias", "cajamarca")
OUT = os.path.join(ROOT, "docs", "herramientas", "out")
os.makedirs(OUT, exist_ok=True)


# ---------------------------------------------------------------- utilidades
def to_arr(im):
    return np.asarray(im).astype(np.float32) / 255.0


def to_img(a):
    return Image.fromarray(np.clip(a * 255.0, 0, 255).astype(np.uint8), "RGB")


def grain(h, w, sigma=1.4, amp=0.03, seed=0):
    r = np.random.default_rng(seed).normal(0, 1, (h, w)).astype(np.float32)
    r = gaussian_filter(r, sigma, mode="wrap")
    r = r - r.mean()
    return (r * (amp / max(r.std(), 1e-4)))[..., None]


def pnoise(h, w, sigma, seed):
    """Ruido periódico: se filtra con borde envuelto, así el tile no corta."""
    r = np.random.default_rng(seed).normal(0, 1, (h, w)).astype(np.float32)
    r = gaussian_filter(r, sigma, mode="wrap")
    r = r - r.mean()
    return r / max(r.std(), 1e-4)


def grade(a, mean, sat=1.0, contrast=1.0):
    lum = a @ np.array([0.299, 0.587, 0.114], dtype=np.float32)
    a = lum[..., None] + (a - lum[..., None]) * sat
    a = (a - 0.5) * contrast + 0.5
    cur = a.reshape(-1, 3).mean(0)
    target = np.array(mean, dtype=np.float32) / 255.0
    return np.clip(a * (target / np.maximum(cur, 1e-3)), 0, 1)


def masonry(w, h, rows, base, seed, joint=3, bevel=0.14, tone=0.05,
            block_min=0.7, block_max=1.7, per_row=6, stagger=0.0,
            pillow=0.0, pillow_px=12.0):
    """Hiladas de bloques que envuelven el tile (sin costura ni huecos)."""
    rr = np.random.default_rng(seed)
    a = np.ones((h, w, 3), np.float32) * np.asarray(base, np.float32) * 0.5
    yy, xx = np.mgrid[0:h, 0:w].astype(np.float32)
    rh = h / rows
    for r in range(rows):
        y0, y1 = r * rh, (r + 1) * rh
        ws = rr.uniform(block_min, block_max, per_row)
        ws = ws / ws.sum() * w
        edges = np.concatenate([[0.0], np.cumsum(ws)])
        off = rr.uniform(0, w)
        if stagger > 0.0:
            off = (off + (r % 2) * w * stagger) % w
        for b in range(per_row):
            xa0, xb0 = edges[b] + off, edges[b + 1] + off
            col = np.asarray(base, np.float32) * (1.0 + rr.normal(0, tone))
            col = col * (1.0 + rr.normal(0, tone * 0.35, 3))
            for dx in (-w, 0.0, w):
                for dy in (-h, 0.0, h):
                    xa, xb = xa0 + dx, xb0 + dx
                    ya, yb = y0 + dy, y1 + dy
                    if xb <= 0 or xa >= w or yb <= 0 or ya >= h:
                        continue
                    xa, xb = max(xa, 0.0), min(xb, float(w))
                    ya, yb = max(ya, 0.0), min(yb, float(h))
                    m = (xx >= xa) & (xx < xb) & (yy >= ya) & (yy < yb)
                    if not m.any():
                        continue
                    # bisel: luz arriba, sombra abajo, cara plana al centro
                    t = (yy - ya) / (yb - ya)
                    shade = 1.0 + bevel * (0.5 - t)
                    if pillow > 0.0:
                        dist = np.minimum(xx - xa, xb - xx)
                        shade = shade * (1.0 - pillow * (1.0 - np.clip(dist / pillow_px, 0, 1)))
                    a[m] = (col[None, None, :] * shade[..., None])[m]
                    # junta vertical
                    if joint > 0:
                        jl = m & (xx < xa + joint)
                        jr = m & (xx >= xb - joint)
                        a[jl] *= 1.0 - bevel * 2.6
                        a[jr] *= 1.0 - bevel * 2.2
        if joint > 0:
            hj = (yy >= y0) & (yy < y0 + joint)
            a[hj] *= 1.0 - bevel * 3.0
    return a


def load_square(rel, box, size):
    im = Image.open(os.path.join(REF, rel)).convert("RGB")
    w, h = im.size
    x0, y0, x1, y1 = [int(round(v * d)) for v, d in zip(box, (w, h, w, h))]
    c = im.crop((x0, y0, x1, y1))
    s = min(c.size)
    c = c.crop(((c.width - s) // 2, (c.height - s) // 2,
                (c.width + s) // 2, (c.height + s) // 2))
    return c.resize((size, size), Image.LANCZOS)


# ---------------------------------------------------------------- cal / revoco
def cal(size=512, seed=41):
    """Muro de cal barrida a mano. 2 m por tile: el grano queda fino."""
    n1 = pnoise(size, size, size / 6.0, seed)
    n2 = pnoise(size, size, size / 26.0, seed + 1)
    n3 = grain(size, size, 1.1, 1.0, seed + 2)[..., 0]
    yy = np.mgrid[0:size, 0:size][0].astype(np.float32)
    # pasadas de llana: bandas anchas y muy suaves
    trowel = 0.5 + 0.5 * np.sin(2 * np.pi * (yy / size * 2.0 + n1 * 0.30))
    tone = 1.0 + 0.020 * n1 + 0.014 * n2 + 0.018 * n3 + 0.008 * (trowel - 0.5)
    base = np.array([0.855, 0.838, 0.792], np.float32)
    a = base[None, None, :] * tone[..., None]
    # manchas de humedad y cal faltante
    stain = np.clip(n1 - 1.20, 0, None) * 0.45
    a = a * (1 - stain[..., None] * 0.14) + np.array([0.68, 0.66, 0.59]) * stain[..., None] * 0.14
    chip = np.clip(n2 - 1.60, 0, None) * 0.5
    a = a * (1 - chip[..., None] * 0.18) + np.array([0.62, 0.59, 0.52]) * chip[..., None] * 0.18
    return to_img(np.clip(a, 0, 1))


# ---------------------------------------------------------------- sillería
def ashlar(size=512, rows=6, seed=51):
    """Cantería de traquita gris: hiladas de 33 cm, bloques de 40-90 cm."""
    a = masonry(size, size, rows, (0.600, 0.598, 0.580), seed,
                joint=3, bevel=0.16, tone=0.040, block_min=0.55, block_max=1.6,
                per_row=7, stagger=0.25)
    # desgaste: manchas amplias y poros
    a = a * (1 + pnoise(size, size, size / 70.0, seed + 1)[..., None] * 0.040)
    pits = np.clip(pnoise(size, size, 1.6, seed + 4) - 1.9, 0, None)
    a = a * (1 - pits[..., None] * 0.45)
    a = a + grain(size, size, 1.0, 0.024, seed + 2)
    # moho tenue en juntas
    moss = np.clip(pnoise(size, size, size / 60.0, seed + 3) - 1.35, 0, None)
    a = a * (1 - moss[..., None] * 0.12) + np.array([0.40, 0.43, 0.34]) * moss[..., None] * 0.12
    return to_img(np.clip(a, 0, 1))


# ---------------------------------------------------------------- teja árabe
def teja(size=512, cols=8, rows=6, seed=61):
    """Teja árabe: 25 cm de canal por 33 cm de exposición (2 m = 1 tile)."""
    cw, rh = size / cols, size / rows
    yy, xx = np.mgrid[0:size, 0:size].astype(np.float32)
    row = (yy // rh).astype(np.int32) % rows
    r2 = np.random.default_rng(seed)
    tone_tab = 1.0 + r2.normal(0, 0.070, (rows, cols))
    warm_tab = r2.uniform(-1, 1, (rows, cols))

    shift = 0.5 * (row % 2)
    fu = xx / cw + shift
    u = fu % 1.0
    col = np.floor(fu).astype(np.int32) % cols
    v = (yy / rh) % 1.0

    band = tone_tab[row, col]
    warm = warm_tab[row, col]
    # canal: media caña, sombra en los bordes y luz en el centro
    canal = 0.46 + 0.62 * np.sin(np.pi * u) ** 0.60
    # exposición: sombra bajo la hilada de arriba, luz al soltar la teja
    overlap = np.where(v < 0.32, 0.46 + 0.54 * (v / 0.32) ** 0.7,
                       1.0 - 0.08 * (v - 0.32) / 0.68)
    base = np.array([0.520, 0.300, 0.222], np.float32)
    tint = np.stack([base[0] * (1 + 0.09 * warm), base[1] * (1 + 0.07 * warm),
                     base[2] * (1 + 0.03 * warm)], axis=-1)
    a = (tint * band[..., None] * canal[..., None] * overlap[..., None])
    # juntas y borde levantado de cada teja
    a[u < 0.045] *= 0.55
    a[u > 0.955] *= 0.68
    a[v < 0.030] *= 0.55
    # vejez: líquenes, cal y alguna teja gris
    n = pnoise(size, size, size / 90.0, seed + 3)[..., None]
    moss = np.clip(n - 1.35, 0, None) * 0.55
    a = a * (1 - moss * 0.30) + np.array([0.34, 0.39, 0.26], np.float32) * moss * 0.30
    lime = np.clip(-n - 1.50, 0, None) * 0.55
    a = a * (1 - lime * 0.26) + np.array([0.72, 0.68, 0.60], np.float32) * lime * 0.26
    a = a + grain(size, size, 1.1, 0.018, seed + 4)
    return to_img(np.clip(a, 0, 1))


# ---------------------------------------------------------------- lajas piedra azul
def lajas(w=768, h=1024, cols=3, rows=4, seed=71):
    """Losa azul de plaza. 3.2 m por tile: 1.07 x 0.80 m por losa."""
    a = masonry(w, h, rows, (0.455, 0.470, 0.500), seed,
                joint=2, bevel=0.10, tone=0.045, block_min=0.85, block_max=1.15,
                per_row=cols, stagger=0.5, pillow=0.10, pillow_px=6.0)
    # pulido del centro de la losa
    sw, sh = w / cols, h / rows
    u = (np.mgrid[0:h, 0:w][1] % sw) / sw
    v = (np.mgrid[0:h, 0:w][0] % sh) / sh
    edge = np.minimum(np.minimum(u, 1 - u), np.minimum(v, 1 - v))
    a *= (0.955 + 0.045 * np.clip(edge * 8, 0, 1))[..., None]
    # manchas de humedad y pulido desparejo
    a = a * (1 + pnoise(h, w, 6.0, seed + 5)[..., None] * 0.065)
    a = a * (1 - np.clip(pnoise(h, w, 40.0, seed + 7) - 1.8, 0, None)[..., None] * 0.20)
    a = a + grain(h, w, 1.2, 0.013, seed + 6)
    return to_img(np.clip(a, 0, 1))


# ---------------------------------------------------------------- sillería inca
def inca(size=512, rows=5, seed=111):
    """Cantería inca: bloques grandes, junta casi invisible, cara almohadillada."""
    a = masonry(size, size, rows, (0.685, 0.650, 0.596), seed,
                joint=1, bevel=0.04, tone=0.018, block_min=0.70, block_max=1.35,
                per_row=4, stagger=0.22, pillow=0.22, pillow_px=15.0)
    # poros, humedad y liquen
    a = a * (1 + pnoise(size, size, 7.0, seed + 1)[..., None] * 0.05)
    a = a * (1 + pnoise(size, size, size / 9.0, seed + 2)[..., None] * 0.035)
    pits = np.clip(pnoise(size, size, 1.5, seed + 3) - 2.1, 0, None)
    a = a * (1 - pits[..., None] * 0.5)
    a = a + grain(size, size, 1.0, 0.020, seed + 4)
    return to_img(np.clip(a, 0, 1))


# ---------------------------------------------------------------- empedrado de río
def empedrado(size=512, cells=13, seed=81):
    r2 = np.random.default_rng(seed)
    pts = []
    for j in range(cells):
        for i in range(cells):
            cx = (i + 0.5 + r2.uniform(-0.30, 0.30)) * size / cells
            cy = (j + 0.5 + r2.uniform(-0.30, 0.30)) * size / cells
            for dx in (-size, 0, size):
                for dy in (-size, 0, size):
                    pts.append((cx + dx, cy + dy))
    pts = np.array(pts, np.float32)
    yy, xx = np.mgrid[0:size, 0:size].astype(np.float32)
    d, idx = cKDTree(pts).query(np.stack([xx.ravel(), yy.ravel()], 1), k=2)
    d1, d2 = d[:, 0].reshape(size, size), d[:, 1].reshape(size, size)
    edge = np.clip((d2 - d1) / (size / cells * 0.20), 0, 1)
    tone = 1.0 + r2.normal(0, 0.09, len(pts)).astype(np.float32)
    warm = r2.uniform(0, 1, len(pts)).astype(np.float32)
    col = (np.array([0.44, 0.43, 0.41])[None, :] * (1 - warm[:, None] * 0.35)
           + np.array([0.50, 0.44, 0.36])[None, :] * (warm[:, None] * 0.35))
    cell = col[idx[:, 0]].reshape(size, size, 3) * tone[idx[:, 0]].reshape(size, size, 1)
    a = cell * (0.72 + 0.38 * edge[..., None] ** 0.55)
    a = a * (0.55 + 0.45 * edge[..., None])
    a = a * (1 + pnoise(size, size, 2.0, seed + 1)[..., None] * 0.10)
    a = a + grain(size, size, 1.1, 0.020, seed + 2)
    return to_img(np.clip(a, 0, 1))


# ---------------------------------------------------------------- madera
def madera(size=512, planks=4, seed=91):
    yy, xx = np.mgrid[0:size, 0:size].astype(np.float32)
    r2 = np.random.default_rng(seed)
    pw = size / planks
    plank = (xx // pw).astype(np.int32) % planks
    px = (xx % pw) / pw
    # veta: dos senos deformados, no una trama regular
    warp = pnoise(size, size, 12.0, seed) * 7.0 + pnoise(size, size, 3.5, seed + 1) * 2.0
    g = (np.sin((xx + warp) * (2 * np.pi / 19.0)) * 0.5 + 0.5) ** 1.6
    g2 = (np.sin((xx + warp * 1.7) * (2 * np.pi / 47.0)) * 0.5 + 0.5)
    grain_tone = 0.55 * g + 0.45 * g2
    base = np.array([0.300, 0.212, 0.158], np.float32)
    tone = 1.0 + r2.normal(0, 0.09, planks)[plank]
    grain_tone = grain_tone * (0.88 + 0.24 * r2.uniform(0, 1, planks)[plank])
    a = base[None, None, :] * (tone[..., None] * (0.80 + 0.34 * grain_tone[..., None]))
    # junta de tabla: sombra centrada en el borde, así el tile no duplica la línea
    a[(px < 0.012) | (px > 0.988)] *= 0.24
    a[((px >= 0.012) & (px < 0.040)) | ((px > 0.960) & (px <= 0.988))] *= 1.18
    # nudos pequeños y vetas oscuras
    for _ in range(3):
        kx, ky = r2.uniform(0, size), r2.uniform(0, size)
        for ox in (-size, 0, size):
            for oy in (-size, 0, size):
                r = np.sqrt((xx - kx - ox) ** 2 + ((yy - ky - oy) * 1.8) ** 2)
                a *= (1 - (np.exp(-(r / 13.0) ** 2) * 0.30)[..., None])
    a = a + grain(size, size, 0.8, 0.016, seed + 2)
    return to_img(np.clip(a, 0, 1))


# ---------------------------------------------------------------- catálogo
plaster = to_arr(cal(512, 41))
ocre = grade(plaster.copy(), (188, 160, 112), sat=1.0, contrast=1.0)

jobs = [
    # reemplazos: mismo nombre y tamaño, mismo .meta (no cambia ningún GUID)
    ("muro_adobe.png", cal(512, 41)),
    ("piedra.png", ashlar(512, 6, 51)),
    ("teja.png", teja(512, 8, 6, 61)),
    ("piso_plaza.png", lajas(768, 1024, 3, 4, 71)),
    # biblioteca nueva
    ("empedrado_rio.png", empedrado(512, 13, 81)),
    ("cal_ocre.png", to_img(ocre)),
    ("piedra_inca.png", inca(512, 5, 111)),
    ("madera_oscura.png", madera(512, 4, 91)),
]

for name, im in jobs:
    im.save(os.path.join(OUT, name), optimize=True)
    print(name, im.size, os.path.getsize(os.path.join(OUT, name)) // 1024, "KB")
