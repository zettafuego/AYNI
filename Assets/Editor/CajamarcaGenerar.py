"""
╔══════════════════════════════════════════════════════════════════╗
║  CajamarcaGenerar.py  —  AYNI Unity Project                     ║
║                                                                  ║
║  Genera TODA la ciudad en un solo FBX (Opción A):                ║
║    suelo, calles, plazas, agua, muros, piedra, techos,           ║
║    puertas, portadas y marcadores.                               ║
║  Unity sólo pinta y decora; no inventa casas en runtime.         ║
║                                                                  ║
║  INSTRUCCIONES:                                                  ║
║  1. Abre Blender 3.x+ (probado en 5.1)                          ║
║  2. Workspace "Scripting" → New → pega este archivo             ║
║  3. ▶ Run Script  (descarga OSM la primera vez)                 ║
║  4. El FBX se exporta a:                                        ║
║     Assets/Resources/Calle/PlanoCajamarca.fbx                   ║
║                                                                  ║
║  Datos: © OpenStreetMap contributors (ODbL).                     ║
║  El centro y la orientación se toman de la Plaza de Armas real.  ║
╚══════════════════════════════════════════════════════════════════╝
"""

import bpy
import math
import json
import os
import random
import time
import unicodedata
import urllib.error
import urllib.parse
import urllib.request

# ══════════════════════════════════════════════════════════════════
#  CONFIGURACIÓN
# ══════════════════════════════════════════════════════════════════

EXPORT_PATH = r"C:\Users\Eduardo\AYNI-Unity\Assets\Resources\Calle\PlanoCajamarca.fbx"
LABEL_PATH = r"C:\Users\Eduardo\AYNI-Unity\Assets\Resources\Calle\calles.json"

# Centro aproximado de la Plaza de Armas de Cajamarca (sólo para el bbox
# de descarga; luego el script centra el mapa en la plaza real de OSM).
CENTER_LAT = -7.15695
CENTER_LON = -78.51740

# Bbox de descarga amplio: cubre el rectángulo del mapa después de girarlo.
# Se descarga en 2×2 tiles para que Overpass no sature.
DELTA_LAT = 0.0095   # ~1.06 km
DELTA_LON = 0.0115   # ~1.27 km
TILES = 2

# Rectángulo final del mapa (metros). Se recorta a este tamaño.
MAP_HALF_X = 780.0
MAP_HALF_Y = 650.0

# Suelo base: grande para que no se vea el borde al alejar la cámara.
SUELO_HALF_X = 1450.0
SUELO_HALF_Y = 1450.0

# Alturas (Z en Blender → Y en Unity).
Z_SUELO   = -0.30
Z_CALZADA =  0.00
Z_VEREDA  =  0.075
Z_PLAZA   =  0.110
Z_AGUA    =  0.030
Z_PUERTA  =  0.100
Z_PORTADA =  0.140
# Banda que cose la plaza con la calle: apenas bajo la plaza para no pelear
# contra su superficie, y sobre la acera para que no la tape.
Z_BANDA   = Z_PLAZA - 0.005

# Franja pavimentada alrededor de plazas/parques para que no queden islas.
APRON_WIDTH   = 13.0
APRON_CELL    = 1.6
APRON_MIN_GAP = 2.0

# Nombres de calles sobre el plano (Unity los convierte en texto 3D).
LABEL_LIMIT   = 22
LABEL_NEAR    = 130.0     # calles que rozan la plaza: siempre con nombre
LABEL_SIZES   = {0: 6.5, 1: 6.5, 2: 5.5, 3: 4.6, 4: 4.6}
LABEL_PRIORITY = {"primary": 0, "secondary": 1, "tertiary": 2, "pedestrian": 3}

# Anchos de calle (semiancho de calzada, semiancho de vereda). Las veredas
# van anchas: en Cajamarca ocupan un tercio del ancho total de la vía.
ROAD_STYLE = {
    "primary":      (5.5, 3.4),
    "secondary":    (4.8, 3.2),
    "tertiary":     (4.0, 3.0),
    "residential":  (3.4, 2.6),
    "living_street":(2.8, 2.2),
    "unclassified": (3.2, 2.4),
    "service":      (2.5, 1.9),
    "pedestrian":   (4.5, 0.0),
    "footway":      (1.4, 0.0),
    "path":         (1.2, 0.0),
    "steps":        (1.6, 0.0),
    "track":        (2.0, 0.0),
}

# Atlas del piso de calle (Resources/Calle/piso_empedrado.png).
# La imagen es un corte transversal de la vía: vereda, cuneta, asfalto, cuneta
# y vereda otra vez. Cada franja del mesh toma su trozo de la imagen por U, y
# la V corre a lo largo de la calle repitiendo la textura sin deformarla.
ATLAS_W       = 1536.0
ATLAS_H       = 2736.0
BAND_CALZADA  = (0.262, 0.748)    # cuneta + asfalto + cuneta
BAND_VEREDA   = (0.006, 0.256)    # losas de la acera + sardinel
BAND_SARDINEL = (0.2510, 0.2565)  # cara vertical del sardinel

# Tipos de vía que reciben fachadas (viviendas de relleno).
FRONTAGE_ROADS = {
    "primary", "secondary", "tertiary", "residential",
    "living_street", "unclassified", "pedestrian",
}

OVERPASS_ENDPOINTS = [
    "https://overpass-api.de/api/interpreter",
    "https://overpass.kumi.systems/api/interpreter",
]

CACHE_PATH = os.path.join(
    os.environ.get("TEMP", os.environ.get("TMP", ".")),
    "ayni_osm_cajamarca_v3.json",
)
CACHE_DAYS = 7.0

SEED = 7319
random.seed(SEED)

R_EARTH = 6371000.0

SOUTH = CENTER_LAT - DELTA_LAT
NORTH = CENTER_LAT + DELTA_LAT
WEST  = CENTER_LON - DELTA_LON
EAST  = CENTER_LON + DELTA_LON


def osm_query(bbox):
    return f"""
[out:json][timeout:150];
(
  way["highway"~"^(primary|secondary|tertiary|residential|unclassified|service|living_street|pedestrian|footway|path|steps|track)$"]({bbox});
  way["building"]({bbox});
  way["amenity"="place_of_worship"]({bbox});
  way["leisure"]({bbox});
  way["place"]({bbox});
  way["landuse"]({bbox});
  way["natural"]({bbox});
  way["waterway"]({bbox});
  relation["building"]({bbox});
  relation["leisure"]({bbox});
  relation["place"]({bbox});
  relation["natural"]({bbox});
  relation["landuse"]({bbox});
);
(._;>;);
out body;
"""

# ══════════════════════════════════════════════════════════════════
#  COORDENADAS Y ORIENTACIÓN
#  x = este, y = norte (en Blender) → Unity x = este, z = norte.
#  El mapa se gira para que la Plaza de Armas quede alineada a los ejes.
# ══════════════════════════════════════════════════════════════════

FRAME_PHI = 0.0
FRAME_COS = 1.0
FRAME_SIN = 0.0
FRAME_TX = 0.0
FRAME_TY = 0.0


def raw_xy(lat, lon):
    """Lat/lon → (x este, y) en metros, sin girar.

    OJO: el FBX se importa en Unity como (x, z, -y). Para que en Unity el
    norte quede en +Z (y la vista de planta salga con el norte arriba),
    el eje y de Blender debe apuntar al SUR. Por eso se niega aquí.
    """
    dlat = math.radians(lat - CENTER_LAT)
    dlon = math.radians(lon - CENTER_LON)
    x = dlon * R_EARTH * math.cos(math.radians(CENTER_LAT))
    y = -dlat * R_EARTH
    return x, y


def map_xy(lat, lon):
    """Lat/lon → coordenadas finales del mapa (plaza centrada y alineada)."""
    x, y = raw_xy(lat, lon)
    rx = x * FRAME_COS - y * FRAME_SIN
    ry = x * FRAME_SIN + y * FRAME_COS
    return rx + FRAME_TX, ry + FRAME_TY


def rotate_xy(p, ang):
    c, s = math.cos(ang), math.sin(ang)
    return p[0] * c - p[1] * s, p[0] * s + p[1] * c


def configure_frame(plaza_pts):
    """Centra el mapa en la plaza y lo deja con el norte arriba.

    El plano de referencia (Google Maps) va con el norte arriba: la plaza y
    el damero del centro quedan en diagonal, tal como se ven en la realidad.
    Girar la ciudad para cuadrar la plaza hacía que las calles no coincidieran
    con la referencia, así que el marco no gira.
    """
    global FRAME_PHI, FRAME_COS, FRAME_SIN, FRAME_TX, FRAME_TY
    FRAME_PHI = 0.0
    FRAME_COS, FRAME_SIN = 1.0, 0.0
    FRAME_TX = FRAME_TY = 0.0
    if len(plaza_pts) < 3:
        print("[FRAME] Sin plaza: se usa el centro aproximado, sin giro.")
        return

    xs = [p[0] for p in plaza_pts]
    ys = [p[1] for p in plaza_pts]
    FRAME_TX = -(min(xs) + max(xs)) * 0.5
    FRAME_TY = -(min(ys) + max(ys)) * 0.5
    print(f"[FRAME] Norte arriba (sin giro); plaza centrada en (0,0); "
          f"tamaño {max(xs)-min(xs):.0f} × {max(ys)-min(ys):.0f} m")


def clip_segment(ax, ay, bx, by, hx, hy):
    """Recorta un segmento al rectángulo [-hx,hx]×[-hy,hy] (Liang-Barsky)."""
    dx, dy = bx - ax, by - ay
    t0, t1 = 0.0, 1.0
    for p, q in ((-dx, ax + hx), (dx, hx - ax), (-dy, ay + hy), (dy, hy - ay)):
        if abs(p) < 1e-12:
            if q < 0:
                return None
            continue
        r = q / p
        if p < 0:
            if r > t1:
                return None
            if r > t0:
                t0 = r
        else:
            if r < t0:
                return None
            if r < t1:
                t1 = r
    if t1 - t0 < 1e-6:
        return None
    return (ax + dx * t0, ay + dy * t0, ax + dx * t1, ay + dy * t1)


def clip_polyline(pts, hx, hy):
    """Devuelve una lista de polilíneas recortadas al rectángulo."""
    out = []
    cur = []
    for i in range(len(pts) - 1):
        seg = clip_segment(pts[i][0], pts[i][1], pts[i + 1][0], pts[i + 1][1], hx, hy)
        if seg is None:
            if len(cur) >= 2:
                out.append(cur)
            cur = []
            continue
        a = (seg[0], seg[1])
        b = (seg[2], seg[3])
        if not cur or math.hypot(cur[-1][0] - a[0], cur[-1][1] - a[1]) > 0.05:
            if len(cur) >= 2:
                out.append(cur)
            cur = [a]
        cur.append(b)
    if len(cur) >= 2:
        out.append(cur)
    return out


# ══════════════════════════════════════════════════════════════════
#  DESCARGA OSM
# ══════════════════════════════════════════════════════════════════

def osm_cache_fresh():
    try:
        return time.time() - os.path.getmtime(CACHE_PATH) < CACHE_DAYS * 86400.0
    except OSError:
        return False


def tile_cache_path(index):
    stem, ext = os.path.splitext(CACHE_PATH)
    return f"{stem}_tile{index}{ext}"


def read_json(path, max_age_days=None):
    if not os.path.exists(path):
        return None
    try:
        if max_age_days is not None:
            age = time.time() - os.path.getmtime(path)
            if age >= max_age_days * 86400.0:
                return None
        with open(path, "r", encoding="utf-8") as fh:
            return json.load(fh)
    except (OSError, ValueError) as exc:
        print(f"[OSM] Cache ilegible {os.path.basename(path)}: {exc}")
        return None


def fetch_tile(bbox, label):
    body = urllib.parse.urlencode({"data": osm_query(bbox)}).encode("utf-8")
    headers = {
        "Content-Type": "application/x-www-form-urlencoded",
        "User-Agent": "AYNI-Unity-Game/1.0 (educational project; OSM ODbL)",
        "Accept": "application/json",
    }
    for attempt in range(2):
        for endpoint in OVERPASS_ENDPOINTS:
            print(f"[OSM] {label}: {endpoint} (intento {attempt + 1}) ...")
            try:
                req = urllib.request.Request(endpoint, data=body, headers=headers)
                with urllib.request.urlopen(req, timeout=190) as resp:
                    data = json.loads(resp.read().decode("utf-8"))
                if "elements" not in data:
                    raise ValueError("respuesta sin 'elements'")
                print(f"[OSM] {label}: {len(data['elements'])} elementos")
                return data
            except Exception as exc:
                print(f"[OSM] {label}: error {exc}")
            time.sleep(2.0)
    return None


def merge_elements(tiles):
    merged = {}
    for data in tiles:
        for el in data.get("elements", []):
            key = (el.get("type"), el.get("id"))
            if key not in merged:
                merged[key] = el
    return {"elements": list(merged.values())}


def download_osm():
    cached = read_json(CACHE_PATH, CACHE_DAYS)
    if cached is not None:
        print(f"[OSM] Cache: {len(cached.get('elements', []))} elementos ({CACHE_PATH})")
        return cached

    lat_step = (NORTH - SOUTH) / TILES
    lon_step = (EAST - WEST) / TILES
    tiles = []
    index = 0
    for row in range(TILES):
        for col in range(TILES):
            index += 1
            s = SOUTH + lat_step * row
            n = s + lat_step
            w = WEST + lon_step * col
            e = w + lon_step
            bbox = f"{s:.7f},{w:.7f},{n:.7f},{e:.7f}"
            label = f"tile {index}/{TILES * TILES}"
            path = tile_cache_path(index)

            data = read_json(path, CACHE_DAYS)
            if data is not None:
                print(f"[OSM] {label}: cache {len(data.get('elements', []))} elementos")
            else:
                data = fetch_tile(bbox, label)
                if data is None:
                    data = read_json(path)
                    if data is None:
                        print(f"[OSM] {label}: sin datos; se aborta la descarga.")
                        return None
                    print(f"[OSM] {label}: se usa cache antigua.")
                else:
                    try:
                        with open(path, "w", encoding="utf-8") as fh:
                            json.dump(data, fh)
                    except OSError as exc:
                        print(f"[OSM] {label}: no se pudo guardar cache ({exc})")
            tiles.append(data)

    data = merge_elements(tiles)
    print(f"[OSM] Total: {len(data['elements'])} elementos únicos")
    try:
        with open(CACHE_PATH, "w", encoding="utf-8") as fh:
            json.dump(data, fh)
        print(f"[OSM] Guardado en cache: {CACHE_PATH}")
    except OSError as exc:
        print(f"[OSM] No se pudo guardar la cache: {exc}")
    return data


def parse_osm(data):
    nodes = {}
    ways = {}
    rels = {}
    for el in data.get("elements", []):
        kind = el.get("type")
        if kind == "node":
            nodes[el["id"]] = (el["lat"], el["lon"])
        elif kind == "way":
            ways[el["id"]] = el
        elif kind == "relation":
            rels[el["id"]] = el
    return nodes, ways, rels


def way_points(way, nodes, transform=map_xy):
    pts = []
    for nid in way.get("nodes", []):
        if nid not in nodes:
            continue
        lat, lon = nodes[nid]
        pts.append(transform(lat, lon))
    return pts


def stitch_rings(lines, close_tol=1.2):
    """Une polilíneas sueltas (miembros de relación) en anillos cerrados."""
    pending = [list(line) for line in lines if len(line) >= 2]
    rings = []
    while pending:
        cur = pending.pop(0)
        if math.hypot(cur[0][0] - cur[-1][0], cur[0][1] - cur[-1][1]) <= close_tol:
            rings.append(clean_ring(cur))
            continue
        changed = True
        while changed:
            changed = False
            if math.hypot(cur[0][0] - cur[-1][0], cur[0][1] - cur[-1][1]) <= close_tol:
                rings.append(clean_ring(cur))
                cur = None
                break
            for i, line in enumerate(pending):
                if math.hypot(cur[-1][0] - line[0][0], cur[-1][1] - line[0][1]) <= close_tol:
                    cur.extend(line[1:])
                    pending.pop(i)
                    changed = True
                    break
                if math.hypot(cur[-1][0] - line[-1][0], cur[-1][1] - line[-1][1]) <= close_tol:
                    cur.extend(reversed(line[:-1]))
                    pending.pop(i)
                    changed = True
                    break
                if math.hypot(cur[0][0] - line[-1][0], cur[0][1] - line[-1][1]) <= close_tol:
                    cur = line[:-1] + cur
                    pending.pop(i)
                    changed = True
                    break
        if cur is not None and len(cur) >= 4:
            rings.append(clean_ring(cur))
    return rings


def clean_ring(ring):
    out = []
    for p in ring:
        if not out or math.hypot(p[0] - out[-1][0], p[1] - out[-1][1]) > 0.03:
            out.append(p)
    if len(out) >= 2 and math.hypot(out[0][0] - out[-1][0], out[0][1] - out[-1][1]) < 0.03:
        out.pop()
    return out


def relation_rings(rel, ways, nodes, transform=map_xy):
    outer = []
    inner = []
    for member in rel.get("members", []):
        if member.get("type") != "way":
            continue
        way = ways.get(member.get("ref"))
        if way is None:
            continue
        pts = way_points(way, nodes, transform)
        if len(pts) < 2:
            continue
        role = member.get("role", "")
        if role == "inner":
            inner.append(pts)
        else:
            outer.append(pts)
    return stitch_rings(outer), stitch_rings(inner)


def slug(text):
    norm = unicodedata.normalize("NFKD", text or "")
    return norm.encode("ascii", "ignore").decode("ascii").lower()


# ══════════════════════════════════════════════════════════════════
#  GEOMETRÍA BÁSICA
# ══════════════════════════════════════════════════════════════════

def signed_area(ring):
    area = 0.0
    n = len(ring)
    for i in range(n):
        x0, y0 = ring[i]
        x1, y1 = ring[(i + 1) % n]
        area += x0 * y1 - x1 * y0
    return area * 0.5


def ensure_ccw(ring):
    if signed_area(ring) < 0.0:
        return list(reversed(ring))
    return list(ring)


def ring_center(ring):
    if not ring:
        return 0.0, 0.0
    # Centro de masa aproximado del polígono (más estable que el promedio).
    a = signed_area(ring)
    if abs(a) < 1e-6:
        return (sum(p[0] for p in ring) / len(ring),
                sum(p[1] for p in ring) / len(ring))
    cx = cy = 0.0
    n = len(ring)
    for i in range(n):
        x0, y0 = ring[i]
        x1, y1 = ring[(i + 1) % n]
        cross = x0 * y1 - x1 * y0
        cx += (x0 + x1) * cross
        cy += (y0 + y1) * cross
    return cx / (6.0 * a), cy / (6.0 * a)


def ring_scale(ring, factor):
    cx, cy = ring_center(ring)
    return [(cx + (p[0] - cx) * factor, cy + (p[1] - cy) * factor) for p in ring]


def avg_radius(ring):
    cx, cy = ring_center(ring)
    if not ring:
        return 1.0
    return sum(math.hypot(p[0] - cx, p[1] - cy) for p in ring) / len(ring)


def point_in_tri(p, a, b, c):
    d1 = (p[0] - b[0]) * (a[1] - b[1]) - (a[0] - b[0]) * (p[1] - b[1])
    d2 = (p[0] - c[0]) * (b[1] - c[1]) - (b[0] - c[0]) * (p[1] - c[1])
    d3 = (p[0] - a[0]) * (c[1] - a[1]) - (c[0] - a[0]) * (p[1] - a[1])
    has_neg = d1 < -1e-9 or d2 < -1e-9 or d3 < -1e-9
    has_pos = d1 > 1e-9 or d2 > 1e-9 or d3 > 1e-9
    return not (has_neg and has_pos)


def triangulate(ring):
    """Ear clipping para un anillo simple. Devuelve índices del anillo."""
    if len(ring) < 3:
        return []
    if signed_area(ring) < 0:
        ring = list(reversed(ring))
    idx = list(range(len(ring)))
    tris = []
    guard = 0
    while len(idx) > 3 and guard < len(ring) * len(ring) + 16:
        guard += 1
        ear = False
        for k in range(len(idx)):
            i0 = idx[(k - 1) % len(idx)]
            i1 = idx[k]
            i2 = idx[(k + 1) % len(idx)]
            ax, ay = ring[i0]
            bx, by = ring[i1]
            cx, cy = ring[i2]
            cross = (bx - ax) * (cy - ay) - (by - ay) * (cx - ax)
            if cross <= 1e-8:
                continue
            inside = False
            for j in idx:
                if j in (i0, i1, i2):
                    continue
                if point_in_tri(ring[j], ring[i0], ring[i1], ring[i2]):
                    inside = True
                    break
            if inside:
                continue
            tris.append((i0, i1, i2))
            idx.pop(k)
            ear = True
            break
        if not ear:
            break
    if len(idx) == 3:
        tris.append((idx[0], idx[1], idx[2]))
    elif not tris:
        # Fallback: abanico (para anillos raros de OSM).
        for i in range(1, len(ring) - 1):
            tris.append((0, i, i + 1))
    return tris


def point_segment_dist(px, py, ax, ay, bx, by):
    dx, dy = bx - ax, by - ay
    l2 = dx * dx + dy * dy
    if l2 < 1e-12:
        return math.hypot(px - ax, py - ay), ax, ay
    t = ((px - ax) * dx + (py - ay) * dy) / l2
    t = max(0.0, min(1.0, t))
    qx, qy = ax + dx * t, ay + dy * t
    return math.hypot(px - qx, py - qy), qx, qy


def point_in_ring(p, ring):
    inside = False
    n = len(ring)
    x, y = p
    for i in range(n):
        x0, y0 = ring[i]
        x1, y1 = ring[(i + 1) % n]
        if (y0 > y) != (y1 > y):
            xin = (x1 - x0) * (y - y0) / ((y1 - y0) or 1e-12) + x0
            if x < xin:
                inside = not inside
    return inside

# ══════════════════════════════════════════════════════════════════
#  ACUMULADOR DE MALLAS
# ══════════════════════════════════════════════════════════════════

class MeshAcc:
    def __init__(self):
        self.verts = []
        self.faces = []
        self.uvs = []
        self.cols = []

    @property
    def empty(self):
        return not self.faces

    def _vert(self, p, uv):
        self.verts.append((float(p[0]), float(p[1]), float(p[2])))
        self.uvs.append((float(uv[0]), float(uv[1])))
        self.cols.append((1.0, 1.0, 1.0, 1.0))
        return len(self.verts) - 1

    def tri(self, p0, p1, p2, uv0, uv1, uv2):
        if (p0 == p1) or (p1 == p2) or (p0 == p2):
            return
        i0 = self._vert(p0, uv0)
        i1 = self._vert(p1, uv1)
        i2 = self._vert(p2, uv2)
        self.faces.append((i0, i1, i2))

    def quad(self, p0, p1, p2, p3, uv0, uv1, uv2, uv3):
        if p0 == p1 or p1 == p2 or p2 == p3:
            return
        i0 = self._vert(p0, uv0)
        i1 = self._vert(p1, uv1)
        i2 = self._vert(p2, uv2)
        i3 = self._vert(p3, uv3)
        self.faces.append((i0, i1, i2, i3))


def add_flat_poly(acc, ring, z, uv_scale):
    ring = ensure_ccw(clean_ring(ring))
    if len(ring) < 3:
        return
    for i0, i1, i2 in triangulate(ring):
        p0 = (ring[i0][0], ring[i0][1], z)
        p1 = (ring[i1][0], ring[i1][1], z)
        p2 = (ring[i2][0], ring[i2][1], z)
        acc.tri(
            p0, p1, p2,
            (p0[0] / uv_scale, p0[1] / uv_scale),
            (p1[0] / uv_scale, p1[1] / uv_scale),
            (p2[0] / uv_scale, p2[1] / uv_scale),
        )


def area_gap(ring, roads, step=4.0):
    """Holgura máxima entre el borde del área y el pavimento de calle más cercano.

    Positivo = hay terreno sin pavimentar entre el área y la calle (isla).
    """
    worst = 0.0
    n = len(ring)
    for i in range(n):
        ax, ay = ring[i]
        bx, by = ring[(i + 1) % n]
        length = math.hypot(bx - ax, by - ay)
        count = max(1, int(length / step))
        for k in range(count + 1):
            t = k / count
            x = ax + (bx - ax) * t
            y = ay + (by - ay) * t
            gap = float("inf")
            for rx0, ry0, rx1, ry1, half, side in roads:
                dist, _, _ = point_segment_dist(x, y, rx0, ry0, rx1, ry1)
                gap = min(gap, dist - half - side)
                if gap <= -1.0:
                    break
            worst = max(worst, gap)
    return worst


def add_apron(acc, ring, width, z, roads, water_rings=(), water_lines=(),
              footprints=(), cell=APRON_CELL, uv_scale=2.0):
    """Pavimenta la franja entre un área walkable y las calles cercanas.

    Se rasteriza en celdas y se descartan las que caen sobre la calzada, la
    acera, el agua, una acequia o un edificio: así el hueco se rellena sin
    tapar nada ni abrir paso a través de las casas. Devuelve cuántas celdas
    se pintaron.
    """
    ring = ensure_ccw(clean_ring(ring))
    if len(ring) < 3 or width <= 0.0:
        return 0

    minx = min(p[0] for p in ring) - width - 4.0
    maxx = max(p[0] for p in ring) + width + 4.0
    miny = min(p[1] for p in ring) - width - 4.0
    maxy = max(p[1] for p in ring) + width + 4.0

    near = [r for r in roads
            if max(r[0], r[2]) >= minx and min(r[0], r[2]) <= maxx
            and max(r[1], r[3]) >= miny and min(r[1], r[3]) <= maxy]
    near_water = [r for r in water_lines
                  if max(r[0], r[2]) >= minx and min(r[0], r[2]) <= maxx
                  and max(r[1], r[3]) >= miny and min(r[1], r[3]) <= maxy]
    near_houses = [h for h in footprints
                   if h[1][2] >= minx and h[1][0] <= maxx
                   and h[1][3] >= miny and h[1][1] <= maxy]

    n = len(ring)
    added = 0
    for ix in range(int(math.floor(minx / cell)), int(math.ceil(maxx / cell))):
        for iy in range(int(math.floor(miny / cell)), int(math.ceil(maxy / cell))):
            x0, y0 = ix * cell, iy * cell
            x1, y1 = x0 + cell, y0 + cell
            cx, cy = x0 + cell * 0.5, y0 + cell * 0.5

            if point_in_ring((cx, cy), ring):
                continue
            edge = min(point_segment_dist(cx, cy, ring[i][0], ring[i][1],
                                          ring[(i + 1) % n][0],
                                          ring[(i + 1) % n][1])[0]
                       for i in range(n))
            if edge > width:
                continue

            corners = ((x0, y0), (x1, y0), (x1, y1), (x0, y1))
            blocked = False
            for wx in water_rings:
                if any(point_in_ring(c, wx) for c in corners):
                    blocked = True
                    break
            if not blocked:
                for house_ring, _box in near_houses:
                    if any(point_in_ring(c, house_ring) for c in corners):
                        blocked = True
                        break
            if not blocked:
                for wx0, wy0, wx1, wy1, half, _side in near_water:
                    if any(point_segment_dist(c[0], c[1], wx0, wy0,
                                              wx1, wy1)[0] < half + 0.6
                           for c in corners):
                        blocked = True
                        break
            if not blocked:
                # Basta con que una esquina quede fuera del pavimento para
                # incluir la celda; así la banda toca la calle sin rendijas.
                covered = 0
                for c in corners:
                    if any(point_segment_dist(c[0], c[1], rx0, ry0,
                                              rx1, ry1)[0] < half + side
                           for rx0, ry0, rx1, ry1, half, side in near):
                        covered += 1
                blocked = covered == 4
            if blocked:
                continue

            acc.quad(
                (x0, y0, z), (x1, y0, z), (x1, y1, z), (x0, y1, z),
                (x0 / uv_scale, y0 / uv_scale),
                (x1 / uv_scale, y0 / uv_scale),
                (x1 / uv_scale, y1 / uv_scale),
                (x0 / uv_scale, y1 / uv_scale),
            )
            added += 1
    return added


def add_wall_ring(acc, ring, height, stone=False, u_offset=0.0, base_z=0.0):
    """Muros verticales de un anillo. UV.x = metros de recorrido."""
    ring = ensure_ccw(clean_ring(ring))
    n = len(ring)
    if n < 3:
        return
    u = u_offset
    for i in range(n):
        a = ring[i]
        b = ring[(i + 1) % n]
        dx, dy = b[0] - a[0], b[1] - a[1]
        length = math.hypot(dx, dy)
        if length < 0.02:
            continue
        z0 = base_z
        z1 = base_z + height
        if stone:
            u0, u1 = u / 3.0, (u + length) / 3.0
            v0, v1 = z0 / 3.0, z1 / 3.0
        else:
            # Fachada: el ancho corre en metros (Unity lo divide por casa) y
            # el alto va normalizado, así una casa = una textura completa por
            # más que cambie la altura del muro.
            u0, u1 = u, u + length
            v0, v1 = 0.0, 1.0
        acc.quad(
            (a[0], a[1], z0),
            (b[0], b[1], z0),
            (b[0], b[1], z1),
            (a[0], a[1], z1),
            (u0, v0), (u1, v0), (u1, v1), (u0, v1),
        )
        u += length


def add_hip_roof(acc, ring, height, uv_scale=3.0):
    """Tejado a cuatro aguas (truncado) para cualquier huella."""
    ring = ensure_ccw(clean_ring(ring))
    if len(ring) < 3:
        return
    r = max(1.2, avg_radius(ring))
    eave = ring_scale(ring, 1.0 + 0.45 / r)
    inner = ring_scale(ring, 1.0 - min(0.32, max(0.10, 2.3 / r)))
    rise = min(2.2, max(0.7, 0.16 * r))
    z0 = height
    z1 = height + rise
    n = len(ring)
    for i in range(n):
        a = eave[i]
        b = eave[(i + 1) % n]
        c = inner[(i + 1) % n]
        d = inner[i]
        acc.quad(
            (a[0], a[1], z0),
            (b[0], b[1], z0),
            (c[0], c[1], z1),
            (d[0], d[1], z1),
            (a[0] / uv_scale, a[1] / uv_scale),
            (b[0] / uv_scale, b[1] / uv_scale),
            (c[0] / uv_scale, c[1] / uv_scale),
            (d[0] / uv_scale, d[1] / uv_scale),
        )
    add_flat_poly(acc, inner, z1, uv_scale)


def add_door(acc, center, tangent, outward, width, height, base=Z_PUERTA):
    hx = width * 0.5
    ox = outward[0] * 0.045
    oy = outward[1] * 0.045
    ax = center[0] - tangent[0] * hx + ox
    ay = center[1] - tangent[1] * hx + oy
    bx = center[0] + tangent[0] * hx + ox
    by = center[1] + tangent[1] * hx + oy
    z0, z1 = base, base + height
    acc.quad(
        (ax, ay, z0), (bx, by, z0), (bx, by, z1), (ax, ay, z1),
        (0.0, 0.0), (1.0, 0.0), (1.0, 1.0), (0.0, 1.0),
    )


def add_billboard(acc, center, tangent, outward, width, height, base=Z_PORTADA):
    hx = width * 0.5
    ox = outward[0] * 0.075
    oy = outward[1] * 0.075
    ax = center[0] - tangent[0] * hx + ox
    ay = center[1] - tangent[1] * hx + oy
    bx = center[0] + tangent[0] * hx + ox
    by = center[1] + tangent[1] * hx + oy
    z0, z1 = base, base + height
    acc.quad(
        (ax, ay, z0), (bx, by, z0), (bx, by, z1), (ax, ay, z1),
        (0.0, 0.0), (1.0, 0.0), (1.0, 1.0), (0.0, 1.0),
    )


def add_gable_house(acc_wall, acc_roof, cx, cy, front, side, width, depth,
                    height, u_offset, overhang=0.45):
    """Casa rectangular; 'front' mira a la calle y 'side' recorre la fachada."""
    hw, hd = width * 0.5, depth * 0.5

    def local(u, v):
        return (cx + side[0] * u + front[0] * v,
                cy + side[1] * u + front[1] * v)

    def p3(u, v, z):
        a, b = local(u, v)
        return (a, b, z)

    ring = [
        local(-hw, -hd), local(hw, -hd), local(hw, hd), local(-hw, hd),
    ]
    add_wall_ring(acc_wall, ring, height, stone=False, u_offset=u_offset)

    rise = min(1.8, max(0.7, width * 0.10))
    ez = height
    rz = height + rise
    ew = hw + overhang
    ed = hd + overhang
    # Pendientes (frente y fondo).
    f0 = p3(-ew, ed, ez)
    f1 = p3(ew, ed, ez)
    f2 = p3(ew, 0.0, rz)
    f3 = p3(-ew, 0.0, rz)
    acc_roof.quad(
        f0, f3, f2, f1,
        (f0[0] / 3.0, f0[1] / 3.0), (f3[0] / 3.0, f3[1] / 3.0),
        (f2[0] / 3.0, f2[1] / 3.0), (f1[0] / 3.0, f1[1] / 3.0),
    )
    b0 = p3(ew, -ed, ez)
    b1 = p3(-ew, -ed, ez)
    b2 = p3(-ew, 0.0, rz)
    b3 = p3(ew, 0.0, rz)
    acc_roof.quad(
        b0, b3, b2, b1,
        (b0[0] / 3.0, b0[1] / 3.0), (b3[0] / 3.0, b3[1] / 3.0),
        (b2[0] / 3.0, b2[1] / 3.0), (b1[0] / 3.0, b1[1] / 3.0),
    )
    # Triángulos del hastial (muro) a izquierda y derecha.
    for sgn in (-1.0, 1.0):
        u = hw * sgn
        p0 = p3(u, -hd, ez)
        p1 = p3(u, hd, ez)
        p2 = p3(u, 0.0, rz)
        if sgn > 0.0:
            acc_wall.tri(
                p0, p1, p2,
                (u_offset, 0.82),
                (u_offset + depth, 0.82),
                (u_offset + depth * 0.5, 0.82),
            )
        else:
            acc_wall.tri(
                p0, p2, p1,
                (u_offset, 0.82),
                (u_offset + depth, 0.82),
                (u_offset + depth * 0.5, 0.82),
            )


# ══════════════════════════════════════════════════════════════════
#  OBJETOS BLENDER
# ══════════════════════════════════════════════════════════════════

def create_obj(name, acc):
    if acc.empty:
        print(f"  [skip] {name:22s} sin geometría")
        return None
    me = bpy.data.meshes.new(name)
    me.from_pydata(acc.verts, [], acc.faces)
    me.validate(verbose=False)
    me.update()
    if acc.uvs:
        layer = me.uv_layers.new(name="UVMap")
        for poly in me.polygons:
            for li in poly.loop_indices:
                vi = me.loops[li].vertex_index
                if vi < len(acc.uvs):
                    layer.data[li].uv = acc.uvs[vi]
    if acc.cols:
        colors = me.color_attributes.new(name="Color", type="FLOAT_COLOR", domain="POINT")
        for i, col in enumerate(acc.cols):
            if i < len(colors.data):
                colors.data[i].color = col
    ob = bpy.data.objects.new(name, me)
    bpy.context.scene.collection.objects.link(ob)
    print(f"  ✅ {name:22s} {len(acc.verts):7d} verts  {len(acc.faces):7d} caras")
    return ob


def create_empty(name, x, y, z=0.0):
    ob = bpy.data.objects.new(name, None)
    ob.location = (float(x), float(y), float(z))
    ob.empty_display_type = "ARROWS"
    ob.empty_display_size = 6.0
    bpy.context.scene.collection.objects.link(ob)
    print(f"  📍 {name:22s} pos=({x:.1f}, {y:.1f}, {z:.1f})")
    return ob


# ══════════════════════════════════════════════════════════════════
#  CALLES
# ══════════════════════════════════════════════════════════════════

def unit_perp(ax, ay, bx, by):
    dx, dy = bx - ax, by - ay
    length = math.hypot(dx, dy)
    if length < 1e-9:
        return 0.0, 0.0
    return -dy / length, dx / length


def perp_at(pts, i):
    n = len(pts)
    if i == 0:
        return unit_perp(pts[0][0], pts[0][1], pts[1][0], pts[1][1])
    if i == n - 1:
        return unit_perp(pts[-2][0], pts[-2][1], pts[-1][0], pts[-1][1])
    ax, ay = unit_perp(pts[i - 1][0], pts[i - 1][1], pts[i][0], pts[i][1])
    bx, by = unit_perp(pts[i][0], pts[i][1], pts[i + 1][0], pts[i + 1][1])
    x, y = ax + bx, ay + by
    length = math.hypot(x, y)
    if length < 1e-9:
        return ax, ay
    return x / length, y / length


def add_strip(acc, pts, half_w, z, offset=0.0, uv_scale=2.0, band=None,
              curb_from=None):
    """Franja de pavimento a lo largo de una polilínea.

    Con ``band`` la franja toma una franja de U del atlas de calle y avanza la
    V con la distancia recorrida. ``curb_from`` añade la cara vertical del
    sardinel entre esa altura y ``z``.
    """
    if len(pts) < 2 or half_w <= 0:
        return
    verts = []
    for i, (px, py) in enumerate(pts):
        rx, ry = perp_at(pts, i)
        if abs(rx) + abs(ry) < 1e-9:
            continue
        lx = px + rx * (offset - half_w)
        ly = py + ry * (offset - half_w)
        rr = px + rx * (offset + half_w)
        rt = py + ry * (offset + half_w)
        verts.append((lx, ly, rr, rt))
    if len(verts) < 2:
        return

    if band is None:
        for i in range(len(verts) - 1):
            ax0, ay0, bx0, by0 = verts[i]
            ax1, ay1, bx1, by1 = verts[i + 1]
            acc.quad(
                (ax0, ay0, z), (ax1, ay1, z), (bx1, by1, z), (bx0, by0, z),
                (ax0 / uv_scale, ay0 / uv_scale),
                (ax1 / uv_scale, ay1 / uv_scale),
                (bx1 / uv_scale, by1 / uv_scale),
                (bx0 / uv_scale, by0 / uv_scale),
            )
        return

    u0, u1 = band
    # Misma densidad de texeles en ambos ejes: una altura completa de la
    # imagen cubre este largo de calle para este ancho de franja.
    tile = (2.0 * half_w) * ATLAS_H / (max(1e-6, u1 - u0) * ATLAS_W)
    # En la acera izquierda el sardinel queda del lado del eje (ax); en la
    # derecha, del lado exterior (bx).
    left_curb = offset > 0.0
    ua, ub = (u1, u0) if left_curb else (u0, u1)
    s0, s1 = BAND_SARDINEL

    along = 0.0
    for i in range(len(verts) - 1):
        ax0, ay0, bx0, by0 = verts[i]
        ax1, ay1, bx1, by1 = verts[i + 1]
        seg = math.hypot((ax1 + bx1 - ax0 - bx0) * 0.5,
                         (ay1 + by1 - ay0 - by0) * 0.5)
        v0 = along / tile
        v1 = (along + seg) / tile
        acc.quad(
            (ax0, ay0, z), (ax1, ay1, z), (bx1, by1, z), (bx0, by0, z),
            (ua, v0), (ua, v1), (ub, v1), (ub, v0),
        )
        if curb_from is not None:
            cx0, cy0 = (ax0, ay0) if left_curb else (bx0, by0)
            cx1, cy1 = (ax1, ay1) if left_curb else (bx1, by1)
            cb, ct = (s1, s0) if left_curb else (s0, s1)
            acc.quad(
                (cx0, cy0, curb_from), (cx1, cy1, curb_from),
                (cx1, cy1, z), (cx0, cy0, z),
                (cb, v0), (cb, v1), (ct, v1), (ct, v0),
            )
        along += seg


# ══════════════════════════════════════════════════════════════════
#  ALTURAS Y CLASES
# ══════════════════════════════════════════════════════════════════

def parse_float(text):
    if text is None:
        return None
    text = str(text).strip().lower().replace(",", ".")
    if "ft" in text or "feet" in text or "'" in text:
        return None
    text = text.replace("metros", "").replace("meter", "").replace("m", "")
    try:
        return float(text.strip())
    except ValueError:
        return None


def building_height(tags, name_slug):
    if "catedral" in name_slug:
        return 16.0
    if "san francisco" in name_slug:
        return 13.0
    if "belen" in name_slug or "museo belen" in name_slug:
        return 9.0
    stone = is_stone(tags, name_slug)
    h = parse_float(tags.get("height"))
    if h is None:
        h = parse_float(tags.get("building:height"))
    if h is None:
        levels = parse_float(tags.get("building:levels"))
        if levels is not None and levels > 0:
            h = levels * 3.0 + 0.8
    if h is None:
        h = 8.5 if stone else 7.0
    if stone:
        return max(5.0, min(16.0, h))
    return max(3.5, min(18.0, h))


def is_stone(tags, name_slug):
    kind = (tags.get("building") or "").lower()
    if kind in ("church", "cathedral", "chapel", "convent", "monastery", "temple"):
        return True
    if tags.get("amenity") == "place_of_worship":
        return True
    for word in ("catedral", "iglesia", "convento", "capilla", "belen", "santuario"):
        if word in name_slug:
            return True
    return False


def is_cathedral(name_slug):
    return "catedral" in name_slug


def is_francisco(name_slug):
    return "san francisco" in name_slug


# ══════════════════════════════════════════════════════════════════
#  RECTÁNGULOS ORIENTADOS (para no solapar casas)
# ══════════════════════════════════════════════════════════════════

def ring_obb(ring):
    if len(ring) < 3:
        return None
    best = None
    best_area = float("inf")
    n = len(ring)
    for i in range(n):
        ax, ay = ring[i]
        bx, by = ring[(i + 1) % n]
        ang = math.atan2(by - ay, bx - ax)
        c, s = math.cos(-ang), math.sin(-ang)
        us = [p[0] * c - p[1] * s for p in ring]
        vs = [p[0] * s + p[1] * c for p in ring]
        w = max(us) - min(us)
        h = max(vs) - min(vs)
        area = w * h
        if area < best_area:
            best_area = area
            cu = (max(us) + min(us)) * 0.5
            cv = (max(vs) + min(vs)) * 0.5
            cc, ss = math.cos(ang), math.sin(ang)
            cx = cu * cc - cv * ss
            cy = cu * ss + cv * cc
            best = (cx, cy, w * 0.5, h * 0.5, ang)
    return best


def obb_corners(obb, grow=0.0):
    cx, cy, hw, hd, ang = obb
    c, s = math.cos(ang), math.sin(ang)
    out = []
    for sx, sy in ((-1, -1), (1, -1), (1, 1), (-1, 1)):
        u = sx * (hw + grow)
        v = sy * (hd + grow)
        out.append((cx + u * c - v * s, cy + u * s + v * c))
    return out


def obb_aabb(obb, grow=0.0):
    pts = obb_corners(obb, grow)
    xs = [p[0] for p in pts]
    ys = [p[1] for p in pts]
    return min(xs), min(ys), max(xs), max(ys)


def obb_overlap(a, b, margin=0.0):
    """SAT entre dos rectángulos orientados."""
    ca, sa = math.cos(a[4]), math.sin(a[4])
    cb, sb = math.cos(b[4]), math.sin(b[4])
    axes = [(ca, sa), (-sa, ca), (cb, sb), (-sb, cb)]
    pa = obb_corners(a, margin)
    pb = obb_corners(b, margin)
    for ax, ay in axes:
        amin = min(p[0] * ax + p[1] * ay for p in pa)
        amax = max(p[0] * ax + p[1] * ay for p in pa)
        bmin = min(p[0] * ax + p[1] * ay for p in pb)
        bmax = max(p[0] * ax + p[1] * ay for p in pb)
        if amax < bmin or bmax < amin:
            return False
    return True


def find_plaza_raw(ways, rels, nodes):
    """Devuelve el anillo de la Plaza de Armas en coordenadas sin girar."""
    candidates = []
    for way in ways.values():
        tags = way.get("tags", {})
        if "plaza de armas" in slug(tags.get("name", "")):
            pts = way_points(way, nodes, raw_xy)
            if len(pts) >= 3:
                candidates.append(pts)
    for rel in rels.values():
        tags = rel.get("tags", {})
        if "plaza de armas" in slug(tags.get("name", "")):
            outers, _ = relation_rings(rel, ways, nodes, raw_xy)
            for ring in outers:
                if len(ring) >= 3:
                    candidates.append(ring)
    if not candidates:
        # Fallback: el parque/plaza más grande cerca del centro aproximado.
        for way in ways.values():
            tags = way.get("tags", {})
            if not (tags.get("leisure") in ("park", "garden", "recreation_ground")
                    or tags.get("place") in ("square", "plaza")):
                continue
            pts = way_points(way, nodes, raw_xy)
            if len(pts) < 3:
                continue
            cx = sum(p[0] for p in pts) / len(pts)
            cy = sum(p[1] for p in pts) / len(pts)
            if math.hypot(cx, cy) < 500.0:
                candidates.append(pts)
    if not candidates:
        return []
    return max(candidates, key=lambda ring: abs(signed_area(ring)))


def is_plaza_tags(tags):
    if tags.get("leisure") in ("park", "garden", "recreation_ground"):
        return True
    if tags.get("place") in ("square", "plaza"):
        return True
    if tags.get("landuse") in ("grass", "village_green"):
        return True
    return False


def is_water_area_tags(tags):
    if tags.get("natural") == "water":
        return True
    if tags.get("waterway") in ("riverbank", "dock", "basin"):
        return True
    return False


def nearest_road_target(x, y, roads):
    best = None
    best_dist = float("inf")
    for ax, ay, bx, by, half, side in roads:
        dist, qx, qy = point_segment_dist(x, y, ax, ay, bx, by)
        if dist < best_dist:
            best_dist = dist
            best = (qx, qy)
    return best if best is not None else (x, y)


# ══════════════════════════════════════════════════════════════════
#  NOMBRES DE CALLES
# ══════════════════════════════════════════════════════════════════

def street_key(name):
    key = name.strip().lower()
    for pre in ("jirón ", "jiron ", "avenida ", "av. ", "av ", "pasaje ",
                "jr. ", "jr "):
        if key.startswith(pre):
            key = key[len(pre):]
            break
    return key.strip()


def point_at(pts, dist):
    """Punto y dirección a cierta distancia de recorrido sobre una polilínea."""
    left = dist
    for a, b in zip(pts, pts[1:]):
        seg = math.hypot(b[0] - a[0], b[1] - a[1])
        if seg < 1e-6:
            continue
        if left <= seg:
            t = left / seg
            return (a[0] + (b[0] - a[0]) * t, a[1] + (b[1] - a[1]) * t,
                    (b[0] - a[0]) / seg, (b[1] - a[1]) / seg)
        left -= seg
    a, b = pts[-2], pts[-1]
    seg = max(1e-6, math.hypot(b[0] - a[0], b[1] - a[1]))
    return (b[0], b[1], (b[0] - a[0]) / seg, (b[1] - a[1]) / seg)


def street_labels(ways, nodes, limit=LABEL_LIMIT):
    """Nombres de calles principales con posición y ángulo (coords Unity)."""
    streets = {}
    for way in ways.values():
        tags = way.get("tags", {})
        kind = tags.get("highway")
        name = (tags.get("name") or "").strip()
        if not name or kind not in ROAD_STYLE:
            continue
        pts = clean_ring(way_points(way, nodes))
        if len(pts) < 2:
            continue
        cx = sum(p[0] for p in pts) / len(pts)
        cy = sum(p[1] for p in pts) / len(pts)
        if abs(cx) > MAP_HALF_X or abs(cy) > MAP_HALF_Y:
            continue
        length = sum(math.hypot(b[0] - a[0], b[1] - a[1])
                     for a, b in zip(pts, pts[1:]))
        if length < 20.0:
            continue
        key = street_key(name)
        item = streets.get(key)
        if item is None:
            item = streets[key] = {"name": name, "len": 0.0,
                                   "near": 1e9, "rank": 4, "ways": []}
        item["len"] += length
        item["near"] = min(item["near"], min(math.hypot(p[0], p[1]) for p in pts))
        item["rank"] = min(item["rank"], LABEL_PRIORITY.get(kind, 4))
        if len(name) > len(item["name"]):
            item["name"] = name
        item["ways"].append((length, pts))

    # Primero las calles que bordean la plaza (aunque sean jirones menores);
    # luego se completan por largo, con ventaja para las vías de rango alto.
    core = [it for it in streets.values()
            if it["near"] <= LABEL_NEAR and it["len"] >= 200.0]
    core.sort(key=lambda it: (it["near"], -it["len"]))
    picked = core[:limit]

    taken = {id(it) for it in picked}
    rest = [it for it in streets.values() if id(it) not in taken]
    rest.sort(key=lambda it: (-(it["len"] + (350.0 if it["rank"] <= 2 else 0.0)),
                              it["near"]))
    picked += rest[:max(0, limit - len(picked))]

    labels = []
    for item in picked:
        best = None
        for length, pts in item["ways"]:
            if length < 24.0:
                continue
            px, py, dx, dy = point_at(pts, length * 0.5)
            dist = math.hypot(px, py)
            if best is None or dist < best[0]:
                best = (dist, px, py, dx, dy)
        if best is None:
            length, pts = max(item["ways"], key=lambda w: w[0])
            px, py, dx, dy = point_at(pts, length * 0.5)
        else:
            _, px, py, dx, dy = best
        angle = -math.degrees(math.atan2(dy, dx))     # y Blender → z Unity = -y
        if dx < 0.0:
            angle += 180.0
        labels.append({
            "name": item["name"],
            "x": round(px, 2),
            "z": round(-py, 2),
            "angle": round(angle, 2),
            "size": LABEL_SIZES.get(item["rank"], 5.2),
        })
    labels.sort(key=lambda lab: lab["name"])
    return labels


def write_street_labels(labels):
    os.makedirs(os.path.dirname(LABEL_PATH), exist_ok=True)
    with open(LABEL_PATH, "w", encoding="utf-8") as fh:
        json.dump({"streets": labels}, fh, ensure_ascii=False, indent=1)
    print(f"  🏷  {len(labels)} nombres de calle → {LABEL_PATH}")


def pick_facade_edge(ring, target, min_len=3.2):
    """Elige la arista más adecuada para puerta/portada."""
    ring = ensure_ccw(clean_ring(ring))
    if len(ring) < 3:
        return None
    best = None
    best_score = float("inf")
    n = len(ring)
    for i in range(n):
        a = ring[i]
        b = ring[(i + 1) % n]
        dx, dy = b[0] - a[0], b[1] - a[1]
        length = math.hypot(dx, dy)
        if length < min_len:
            continue
        dist, qx, qy = point_segment_dist(target[0], target[1], a[0], a[1], b[0], b[1])
        score = dist - min(length, 30.0) * 0.08
        if score < best_score:
            best_score = score
            best = (a, b, length)
    if best is None:
        return None
    a, b, length = best
    dx, dy = b[0] - a[0], b[1] - a[1]
    tx, ty = dx / length, dy / length
    raw_t = ((target[0] - a[0]) * dx + (target[1] - a[1]) * dy) / (length * length)
    t = max(0.26, min(0.74, raw_t))
    center = (a[0] + dx * t, a[1] + dy * t)
    outward = (dy / length, -dx / length)
    return {
        "a": a, "b": b, "length": length, "center": center,
        "tangent": (tx, ty), "outward": outward,
    }


def add_osm_building(accs, ring, tags, name, target):
    ring = clean_ring(ring)
    if len(ring) < 3 or abs(signed_area(ring)) < 12.0:
        return None
    name_slug = slug(name)
    height = building_height(tags, name_slug)
    stone = is_stone(tags, name_slug)
    uoff = random.uniform(0.0, 24.0)
    if stone:
        add_wall_ring(accs["piedra"], ring, height, stone=True, u_offset=uoff)
    else:
        add_wall_ring(accs["muros"], ring, height, stone=False, u_offset=uoff)
    add_hip_roof(accs["techos"], ring, height)

    edge = pick_facade_edge(ring, target)
    info = {
        "ring": ring, "height": height, "stone": stone,
        "name": name, "slug": name_slug, "edge": edge,
    }
    if edge is None:
        return info
    door_w = max(0.9, min(1.7, edge["length"] * 0.30))
    door_h = max(2.2, min(3.2, height * 0.50))
    add_door(accs["puertas"], edge["center"], edge["tangent"],
             edge["outward"], door_w, door_h)
    return info


def build_city(data):
    nodes, ways, rels = parse_osm(data)
    print(f"\n  Nodos: {len(nodes):,}  Ways: {len(ways):,}  Relaciones: {len(rels):,}")

    plaza_raw = find_plaza_raw(ways, rels, nodes)
    configure_frame(plaza_raw)

    accs = {
        "suelo": MeshAcc(), "calles": MeshAcc(), "banda": MeshAcc(),
        "plazas": MeshAcc(), "agua": MeshAcc(), "muros": MeshAcc(),
        "piedra": MeshAcc(), "techos": MeshAcc(), "puertas": MeshAcc(),
        "port_cat": MeshAcc(), "port_fra": MeshAcc(),
    }

    area_boxes = []
    road_segments = []
    water_segments = []
    road_polylines = []
    plaza_rings = []
    water_rings = []

    # ── Plazas, parques y agua (áreas) ─────────────────────────────
    n_plaza = n_water = 0
    for way in ways.values():
        tags = way.get("tags", {})
        plaza = is_plaza_tags(tags)
        water = is_water_area_tags(tags)
        if not plaza and not water:
            continue
        ring = clean_ring(way_points(way, nodes))
        if len(ring) < 3 or abs(signed_area(ring)) < 20.0:
            continue
        cx = sum(p[0] for p in ring) / len(ring)
        cy = sum(p[1] for p in ring) / len(ring)
        if abs(cx) > MAP_HALF_X + 80 or abs(cy) > MAP_HALF_Y + 80:
            continue
        xs = [p[0] for p in ring]
        ys = [p[1] for p in ring]
        if plaza:
            add_flat_poly(accs["plazas"], ring, Z_PLAZA, 3.2)
            plaza_rings.append(ring)
            n_plaza += 1
        else:
            add_flat_poly(accs["agua"], ring, Z_AGUA, 3.0)
            water_rings.append(ring)
            n_water += 1
        area_boxes.append((ring, (min(xs), min(ys), max(xs), max(ys))))

    for rel in rels.values():
        tags = rel.get("tags", {})
        plaza = is_plaza_tags(tags)
        water = is_water_area_tags(tags)
        if not plaza and not water:
            continue
        outers, _ = relation_rings(rel, ways, nodes)
        for ring in outers:
            ring = clean_ring(ring)
            if len(ring) < 3 or abs(signed_area(ring)) < 20.0:
                continue
            cx = sum(p[0] for p in ring) / len(ring)
            cy = sum(p[1] for p in ring) / len(ring)
            if abs(cx) > MAP_HALF_X + 80 or abs(cy) > MAP_HALF_Y + 80:
                continue
            xs = [p[0] for p in ring]
            ys = [p[1] for p in ring]
            if plaza:
                add_flat_poly(accs["plazas"], ring, Z_PLAZA, 3.2)
                plaza_rings.append(ring)
                n_plaza += 1
            else:
                add_flat_poly(accs["agua"], ring, Z_AGUA, 3.0)
                water_rings.append(ring)
                n_water += 1
            area_boxes.append((ring, (min(xs), min(ys), max(xs), max(ys))))

    # ── Calles ─────────────────────────────────────────────────────
    n_road = 0
    for way in ways.values():
        tags = way.get("tags", {})
        kind = tags.get("highway")
        if kind not in ROAD_STYLE:
            continue
        pts = clean_ring(way_points(way, nodes))
        if len(pts) < 2:
            continue
        half, sidewalk = ROAD_STYLE[kind]
        polylines = clip_polyline(pts, MAP_HALF_X + 25.0, MAP_HALF_Y + 25.0)
        for poly in polylines:
            if len(poly) < 2:
                continue
            if sidewalk > 0.01:
                add_strip(accs["calles"], poly, half, Z_CALZADA, 0.0, 2.0,
                          BAND_CALZADA)
                add_strip(accs["calles"], poly, sidewalk, Z_VEREDA,
                          half + sidewalk, 2.0, BAND_VEREDA,
                          curb_from=Z_CALZADA)
                add_strip(accs["calles"], poly, sidewalk, Z_VEREDA,
                          -(half + sidewalk), 2.0, BAND_VEREDA,
                          curb_from=Z_CALZADA)
            else:
                # Sin acera: la calzada (o el sendero) se pavimenta entero.
                band = BAND_CALZADA if half >= 2.0 else BAND_VEREDA
                add_strip(accs["calles"], poly, half, Z_VEREDA, 0.0, 2.0, band)
            for i in range(len(poly) - 1):
                ax, ay = poly[i]
                bx, by = poly[i + 1]
                if math.hypot(bx - ax, by - ay) > 1.0:
                    road_segments.append((ax, ay, bx, by, half, sidewalk))
            road_polylines.append((poly, kind))
            n_road += 1

    # ── Acequias / ríos (líneas) ────────────────────────────────────
    for way in ways.values():
        tags = way.get("tags", {})
        ww = tags.get("waterway")
        if ww not in ("river", "stream", "canal", "drain"):
            continue
        if tags.get("tunnel") or tags.get("covered") == "yes":
            continue
        pts = clean_ring(way_points(way, nodes))
        if len(pts) < 2:
            continue
        half = 2.4 if ww in ("river", "canal") else 1.4
        for poly in clip_polyline(pts, MAP_HALF_X + 25.0, MAP_HALF_Y + 25.0):
            if len(poly) < 2:
                continue
            add_strip(accs["agua"], poly, half, Z_AGUA, 0.0, 3.0)
            for i in range(len(poly) - 1):
                ax, ay = poly[i]
                bx, by = poly[i + 1]
                if math.hypot(bx - ax, by - ay) > 1.0:
                    water_segments.append((ax, ay, bx, by, half, 0.0))

    # ── Edificios OSM ──────────────────────────────────────────────
    buildings = []
    for way in ways.values():
        tags = way.get("tags", {})
        if "building" not in tags:
            continue
        ring = clean_ring(way_points(way, nodes))
        if len(ring) < 3:
            continue
        buildings.append((ring, tags, tags.get("name", "")))
    for rel in rels.values():
        tags = rel.get("tags", {})
        if "building" not in tags:
            continue
        outers, _ = relation_rings(rel, ways, nodes)
        for ring in outers:
            ring = clean_ring(ring)
            if len(ring) >= 3:
                buildings.append((ring, tags, tags.get("name", "")))

    def b_area(item):
        return abs(signed_area(item[0]))

    def b_center(item):
        ring = item[0]
        return (sum(p[0] for p in ring) / len(ring),
                sum(p[1] for p in ring) / len(ring))

    buildings = [b for b in buildings
                 if b_area(b) >= 12.0
                 and abs(b_center(b)[0]) <= MAP_HALF_X + 40
                 and abs(b_center(b)[1]) <= MAP_HALF_Y + 40]
    buildings.sort(key=b_area, reverse=True)
    print(f"  Áreas: {n_plaza} plazas/parques, {n_water} de agua; "
          f"{n_road} tramos de calle; {len(buildings)} edificios OSM")

    cathedral = None
    francisco = None
    for item in buildings:
        name = slug(item[2])
        if cathedral is None and is_cathedral(name):
            cathedral = item
        if francisco is None and is_francisco(name):
            francisco = item
    print(f"  Catedral: {cathedral[2] if cathedral else '—'}  |  "
          f"San Francisco: {francisco[2] if francisco else '—'}")

    # ── Banda que cose la Plaza de Armas con las calles ────────────
    # El polígono de OSM es sólo el jardín central: entre él y la calzada
    # quedan huecos de hasta 10 m. Se pavimenta esa franja sin tapar
    # calles, agua ni edificios, para que el héroe pueda salir por
    # cualquier lado de la plaza.
    main_plaza = None
    for ring in plaza_rings:
        area = abs(signed_area(ring))
        if area < 3000.0:
            continue
        cx = sum(p[0] for p in ring) / len(ring)
        cy = sum(p[1] for p in ring) / len(ring)
        if math.hypot(cx, cy) > 45.0:
            continue
        if main_plaza is None or area > abs(signed_area(main_plaza)):
            main_plaza = ring
    n_apron = 0
    if main_plaza is not None:
        gap = area_gap(main_plaza, road_segments)
        width = min(APRON_WIDTH, max(APRON_MIN_GAP, gap) + 1.2)
        footprints = [(ring, (min(p[0] for p in ring), min(p[1] for p in ring),
                              max(p[0] for p in ring), max(p[1] for p in ring)))
                      for ring, _tags, _name in buildings]
        n_apron = add_apron(accs["banda"], main_plaza, width, Z_BANDA,
                            road_segments, water_rings, water_segments,
                            footprints, uv_scale=3.2)
        r = max(6.0, avg_radius(main_plaza))
        wide = ring_scale(main_plaza, 1.0 + width / r)
        xs = [p[0] for p in wide]
        ys = [p[1] for p in wide]
        area_boxes.append((wide, (min(xs), min(ys), max(xs), max(ys))))
        print(f"  Banda plaza ↔ calle: hueco {gap:.1f} m, ancho {width:.1f} m, "
              f"{n_apron} celdas")

    # ── Rejilla de ocupación y de calles ───────────────────────────
    occ = {}
    cell_occ = 18.0

    def occ_key(ix, iy):
        return ix, iy

    def occ_add(obb):
        minx, miny, maxx, maxy = obb_aabb(obb, 1.0)
        for ix in range(int(math.floor(minx / cell_occ)), int(math.floor(maxx / cell_occ)) + 1):
            for iy in range(int(math.floor(miny / cell_occ)), int(math.floor(maxy / cell_occ)) + 1):
                occ.setdefault(occ_key(ix, iy), []).append(obb)

    def occ_hit(obb, margin=0.28):
        minx, miny, maxx, maxy = obb_aabb(obb, 1.0)
        seen = set()
        for ix in range(int(math.floor(minx / cell_occ)), int(math.floor(maxx / cell_occ)) + 1):
            for iy in range(int(math.floor(miny / cell_occ)), int(math.floor(maxy / cell_occ)) + 1):
                for other in occ.get(occ_key(ix, iy), ()):
                    if id(other) in seen:
                        continue
                    seen.add(id(other))
                    if obb_overlap(other, obb, margin):
                        return True
        return False

    road_grid = {}
    cell_road = 32.0

    def grid_add(seg):
        ax, ay, bx, by, half, side = seg
        pad = half + side + 3.0
        minx, maxx = min(ax, bx) - pad, max(ax, bx) + pad
        miny, maxy = min(ay, by) - pad, max(ay, by) + pad
        for ix in range(int(math.floor(minx / cell_road)), int(math.floor(maxx / cell_road)) + 1):
            for iy in range(int(math.floor(miny / cell_road)), int(math.floor(maxy / cell_road)) + 1):
                road_grid.setdefault((ix, iy), []).append(seg)

    for seg in road_segments:
        grid_add(seg)
    for seg in water_segments:
        grid_add(seg)

    def clear_of_roads(points):
        cands = {}
        for x, y in points:
            key = (int(math.floor(x / cell_road)), int(math.floor(y / cell_road)))
            for seg in road_grid.get(key, ()):
                cands[id(seg)] = seg
        for seg in cands.values():
            ax, ay, bx, by, half, side = seg
            need = half + side + 0.55
            for x, y in points:
                dist, _, _ = point_segment_dist(x, y, ax, ay, bx, by)
                if dist < need:
                    return False
        return True

    def area_hit(x, y):
        for ring, (minx, miny, maxx, maxy) in area_boxes:
            if x < minx or x > maxx or y < miny or y > maxy:
                continue
            if point_in_ring((x, y), ring):
                return True
        return False

    # ── Colocar edificios OSM, puertas y portadas ──────────────────
    stone_infos = []
    cat_info = None
    fra_info = None
    house_ids = 0

    for item in buildings:
        ring, tags, name = item
        name_slug = slug(name)
        cx, cy = b_center(item)
        if item is cathedral:
            target = (0.0, 0.0)
        else:
            target = nearest_road_target(cx, cy, road_segments)
        info = add_osm_building(accs, ring, tags, name, target)
        if info is None:
            continue
        obb = ring_obb(ring)
        if obb is not None:
            occ_add(obb)
        if info["stone"]:
            stone_infos.append(info)
        if item is cathedral:
            cat_info = info
        if item is francisco:
            fra_info = info

    def add_portada(info, acc, height, max_width):
        edge = info.get("edge") if info else None
        if not edge:
            return None
        width = min(max_width, max(4.4, edge["length"] * 0.72))
        width = min(width, edge["length"] * 0.90)
        add_billboard(acc, edge["center"], edge["tangent"],
                      edge["outward"], width, height)
        return edge["center"]

    # Fallback si los nombres de OSM no aparecen.
    if cat_info is None and stone_infos:
        cat_info = min(stone_infos,
                       key=lambda i: math.hypot(i["ring"][0][0], i["ring"][0][1]))
    if fra_info is None:
        others = [i for i in stone_infos if i is not cat_info]
        if others:
            fra_info = min(others, key=lambda i: math.hypot(
                i["edge"]["center"][0] if i["edge"] else i["ring"][0][0],
                i["edge"]["center"][1] if i["edge"] else i["ring"][0][1]))

    cat_door = add_portada(cat_info, accs["port_cat"], 8.6, 10.2)
    fra_door = add_portada(fra_info, accs["port_fra"], 7.2, 8.4)
    print(f"  Portada catedral: {'sí' if cat_door else 'no'}  |  "
          f"Portada San Francisco: {'sí' if fra_door else 'no'}")

    labels = street_labels(ways, nodes)
    if labels:
        print("  Calles rotuladas: " + ", ".join(lab["name"] for lab in labels))

    return {
        "accs": accs, "road_polylines": road_polylines,
        "road_segments": road_segments, "occ": occ, "occ_add": occ_add,
        "occ_hit": occ_hit, "clear_of_roads": clear_of_roads,
        "area_hit": area_hit, "fra_door": fra_door, "cat_door": cat_door,
        "labels": labels,
    }


def add_infill(ctx):
    """Rellena las fachadas que OSM no tiene mapeadas (siempre en Blender)."""
    accs = ctx["accs"]
    occ_add = ctx["occ_add"]
    occ_hit = ctx["occ_hit"]
    clear_of_roads = ctx["clear_of_roads"]
    area_hit = ctx["area_hit"]
    count = 0
    limit = 6000

    for poly, kind in ctx["road_polylines"]:
        if kind not in FRONTAGE_ROADS:
            continue
        half, sidewalk = ROAD_STYLE[kind]
        for i in range(len(poly) - 1):
            ax, ay = poly[i]
            bx, by = poly[i + 1]
            length = math.hypot(bx - ax, by - ay)
            if length < 9.0:
                continue
            dx, dy = (bx - ax) / length, (by - ay) / length
            for sign in (-1.0, 1.0):
                ox, oy = -dy * sign, dx * sign       # del eje hacia la fachada
                front = (-ox, -oy)                  # de la casa hacia la calle
                side = (front[1], -front[0])        # recorrido de la fachada
                t = random.uniform(3.5, 6.0)
                while t < length - 4.0 and count < limit:
                    w = random.uniform(7.0, 10.8)
                    d = random.uniform(8.0, 11.8)
                    tc = t + w * 0.5
                    advance = tc + w * 0.5 + random.uniform(0.9, 2.6)
                    if tc > length - 3.5:
                        break
                    px, py = ax + dx * tc, ay + dy * tc
                    setback = max(0.9, sidewalk * 0.6 + 0.7)
                    off = half + sidewalk + setback + d * 0.5
                    cx, cy = px + ox * off, py + oy * off
                    yaw = math.atan2(side[1], side[0])
                    obb = (cx, cy, w * 0.5, d * 0.5, yaw)
                    pts = obb_corners(obb, 0.0)
                    probes = pts + [(cx, cy)]

                    t = advance
                    if count >= limit:
                        break
                    if any(abs(p[0]) > MAP_HALF_X - 4.0 or abs(p[1]) > MAP_HALF_Y - 4.0
                           for p in pts):
                        continue
                    if random.random() < 0.07:
                        continue
                    if not clear_of_roads(probes):
                        continue
                    if any(area_hit(p[0], p[1]) for p in probes):
                        continue
                    if occ_hit(obb, 0.22):
                        continue

                    height = random.uniform(5.0, 8.3)
                    if random.random() < 0.10:
                        height = random.uniform(8.6, 10.2)
                    uoff = random.uniform(0.0, 24.0)
                    add_gable_house(accs["muros"], accs["techos"], cx, cy,
                                    front, side, w, d, height, uoff)
                    dc = (cx + front[0] * d * 0.5, cy + front[1] * d * 0.5)
                    add_door(accs["puertas"], dc, (-side[0], -side[1]), front,
                             1.5, min(3.0, height * 0.48))
                    occ_add(obb)
                    count += 1

    print(f"  🏠 Viviendas de relleno (Blender): {count}"
          + ("  (límite alcanzado)" if count >= limit else ""))
    return count


# ══════════════════════════════════════════════════════════════════
#  MAIN
# ══════════════════════════════════════════════════════════════════

def report_scene():
    """Comprueba caras, normales y altura de cada capa antes de exportar."""
    print("\n  Verificación de capas (coords Unity: x, y=alto, z):")
    for ob in bpy.context.scene.objects:
        if ob.type != "MESH":
            continue
        me = ob.data
        if not me.polygons:
            continue
        up = 0
        down = 0
        side = 0
        zs = [v.co.z for v in me.vertices]
        xs = [v.co.x for v in me.vertices]
        ys = [v.co.y for v in me.vertices]
        for poly in me.polygons:
            nz = poly.normal.z
            if nz > 0.7:
                up += 1
            elif nz < -0.7:
                down += 1
            else:
                side += 1
        total = float(len(me.polygons))
        print(f"    {ob.name:20s} caras {len(me.polygons):6d}  "
              f"arriba {up / total:4.0%}  abajo {down / total:4.0%}  "
              f"lado {side / total:4.0%}  alto {min(zs):.2f}..{max(zs):.2f}  "
              f"x {min(xs):.0f}..{max(xs):.0f}  y {min(ys):.0f}..{max(ys):.0f}")


def clear_scene():
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete(use_global=False)
    for mesh in list(bpy.data.meshes):
        bpy.data.meshes.remove(mesh)
    for curve in list(bpy.data.curves):
        bpy.data.curves.remove(curve)


def main():
    print()
    print("═" * 66)
    print("  AYNI — Generador de PlanoCajamarca.fbx (Opción A)")
    print("  Plaza de Armas de Cajamarca · datos © OpenStreetMap (ODbL)")
    print("═" * 66)

    clear_scene()
    data = download_osm()
    if data is None:
        print("\n❌ No se pudo descargar OSM. Revisa internet o la cache.")
        return

    ctx = build_city(data)
    add_infill(ctx)

    write_street_labels(ctx["labels"])

    # Suelo base (debajo de todo, sin coplanaridad).
    sx, sy = SUELO_HALF_X, SUELO_HALF_Y
    add_flat_poly(ctx["accs"]["suelo"], [
        (-sx, -sy), (sx, -sy), (sx, sy), (-sx, sy),
    ], Z_SUELO, 8.0)

    print("\n  Creando objetos Blender:")
    create_obj("PlanoSuelo", ctx["accs"]["suelo"])
    create_obj("PlanoCalles", ctx["accs"]["calles"])
    create_obj("PlanoBanda", ctx["accs"]["banda"])
    create_obj("PlanoPlazas", ctx["accs"]["plazas"])
    create_obj("PlanoAgua", ctx["accs"]["agua"])
    create_obj("PlanoMuros", ctx["accs"]["muros"])
    create_obj("PlanoPiedra", ctx["accs"]["piedra"])
    create_obj("PlanoTechos", ctx["accs"]["techos"])
    create_obj("PlanoPuertas", ctx["accs"]["puertas"])
    create_obj("PlanoPortadaCatedral", ctx["accs"]["port_cat"])
    create_obj("PlanoPortadaFrancisco", ctx["accs"]["port_fra"])

    door = ctx["fra_door"] or (0.0, -20.0)
    create_empty("PlanoMarca", 62.0, 0.0, 0.4)
    create_empty("PlanoPuerta", door[0], door[1], 0.3)

    if not ctx["fra_door"]:
        print("  ⚠ No se encontró San Francisco: PlanoPuerta queda en (0, -20).")

    report_scene()

    print("\n  Exportando FBX ...")
    os.makedirs(os.path.dirname(EXPORT_PATH), exist_ok=True)
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.export_scene.fbx(
        filepath=EXPORT_PATH,
        use_selection=True,
        global_scale=1.0,
        apply_unit_scale=True,
        apply_scale_options="FBX_SCALE_NONE",
        axis_forward="-Z",
        axis_up="Y",
        object_types={"EMPTY", "MESH"},
        mesh_smooth_type="FACE",
        use_mesh_modifiers=False,
        add_leaf_bones=False,
        bake_anim=False,
        path_mode="AUTO",
    )

    size = os.path.getsize(EXPORT_PATH) if os.path.exists(EXPORT_PATH) else 0
    print()
    print("═" * 66)
    print(f"  ✅ Exportado: {EXPORT_PATH}")
    print(f"     {size/1024.0:.0f} KB")
    print()
    print("  En Unity: selecciona PlanoCajamarca → Model →")
    print("  ✅ Read/Write Enabled → Apply.")
    print("  El mapa ya trae muros, puertas, portadas y alturas:")
    print("  Unity sólo pinta y decora (no inventa casas).")
    print("═" * 66)


if __name__ == "__main__":
    main()





