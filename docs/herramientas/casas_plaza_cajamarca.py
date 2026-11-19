# Casas de la Plaza de Armas de Cajamarca, para AYNI.
# Cada malla es un material del juego (muro, piedra, teja, madera, vidrio, reja).
# El UV es 0.5 por metro: un tile de Calle cubre 2 m, igual que los muros y techos.
# Blender: +X a la derecha de la fachada, -Y hacia la calle, +Z arriba.
# Al exportar (-Z forward, Y up) la fachada queda en +Z de Unity.
#
#   blender --background --python docs/herramientas/casas_plaza_cajamarca.py

import math
import os

import bpy

UV = 0.5
OUT_FBX = r"C:\Users\Eduardo\AYNI-Unity\Assets\Resources\Calle\Casas"
OUT_BLEND = r"C:\Users\Eduardo\AYNI-Unity\Casas\casas-plaza.blend"
OUT_IMG = r"C:\Users\Eduardo\AYNI-Unity\docs\img\casas-plaza"

# PisoPiedra y PisoMadera llevan "Piso" para no tener collider, y Piedra o Madera
# para que PintarCasona les ponga la textura de la calle.
COLORES = {
    "Muro": (0.94, 0.91, 0.84, 1),
    "Piedra": (0.55, 0.54, 0.50, 1),
    "Teja": (0.66, 0.27, 0.17, 1),
    "AdornoMadera": (0.42, 0.26, 0.14, 1),
    "AdornoVidrio": (0.10, 0.14, 0.16, 1),
    "AdornoReja": (0.08, 0.07, 0.06, 1),
    "PisoPiedra": (0.62, 0.60, 0.54, 1),
    "PisoMadera": (0.55, 0.36, 0.20, 1),
    # Puertas verdes de la catedral y de San Francisco. PintarCasona las pinta.
    "Porton": (0.16, 0.34, 0.22, 1),
}

# El vano se hunde hasta la cara interior. Así el jambón llega al muro de adentro.
GROSOR = 0.34


def vadd(a, b):
    return (a[0] + b[0], a[1] + b[1], a[2] + b[2])


def vsub(a, b):
    return (a[0] - b[0], a[1] - b[1], a[2] - b[2])


def vmul(a, s):
    return (a[0] * s, a[1] * s, a[2] * s)


def vdot(a, b):
    return a[0] * b[0] + a[1] * b[1] + a[2] * b[2]


def vcross(a, b):
    return (
        a[1] * b[2] - a[2] * b[1],
        a[2] * b[0] - a[0] * b[2],
        a[0] * b[1] - a[1] * b[0],
    )


def vnorm(a):
    L = math.sqrt(vdot(a, a)) or 1.0
    return (a[0] / L, a[1] / L, a[2] / L)


class Parte:
    def __init__(self):
        self.verts = []
        self.faces = []
        self.uvs = []

    def _push(self, pts, uvs, outward):
        a, b, c = pts[0], pts[1], pts[2]
        n = vcross(vsub(b, a), vsub(c, a))
        if vdot(n, outward) < 0:
            pts = list(reversed(pts))
            uvs = list(reversed(uvs))
        i = len(self.verts)
        self.verts.extend(pts)
        self.faces.append(tuple(i + k for k in range(len(pts))))
        self.uvs.append(uvs)

    def quad(self, pts, uvs, outward):
        self._push(pts, uvs, outward)

    def tri(self, pts, uvs, outward):
        self._push(pts, uvs, outward)


def uv2(p, q):
    return (p * UV, q * UV)


def caja(parte, x0, x1, y0, y1, z0, z1):
    caras = [
        ([(x0, y0, z0), (x0, y1, z0), (x1, y1, z0), (x1, y0, z0)], (0, 0, -1),
         [uv2(x0, y0), uv2(x0, y1), uv2(x1, y1), uv2(x1, y0)]),
        ([(x0, y0, z1), (x1, y0, z1), (x1, y1, z1), (x0, y1, z1)], (0, 0, 1),
         [uv2(x0, y0), uv2(x1, y0), uv2(x1, y1), uv2(x0, y1)]),
        ([(x0, y0, z0), (x1, y0, z0), (x1, y0, z1), (x0, y0, z1)], (0, -1, 0),
         [uv2(x0, z0), uv2(x1, z0), uv2(x1, z1), uv2(x0, z1)]),
        ([(x1, y1, z0), (x0, y1, z0), (x0, y1, z1), (x1, y1, z1)], (0, 1, 0),
         [uv2(x0, z0), uv2(x1, z0), uv2(x1, z1), uv2(x0, z1)]),
        ([(x0, y1, z0), (x0, y0, z0), (x0, y0, z1), (x0, y1, z1)], (-1, 0, 0),
         [uv2(y0, z0), uv2(y1, z0), uv2(y1, z1), uv2(y0, z1)]),
        ([(x1, y0, z0), (x1, y1, z0), (x1, y1, z1), (x1, y0, z1)], (1, 0, 0),
         [uv2(y0, z0), uv2(y1, z0), uv2(y1, z1), uv2(y0, z1)]),
    ]
    for pts, outward, uvs in caras:
        parte.quad(pts, uvs, outward)


def punto(origen, eje_u, eje_v, u, v):
    return vadd(origen, vadd(vmul(eje_u, u), vmul(eje_v, v)))


def arco_muestras(n, radio):
    pts = []
    for i in range(n + 1):
        a = math.pi - i * math.pi / n
        pts.append((math.cos(a) * radio, math.sin(a) * radio))
    return pts


def contorno_vano(vano, n=8):
    x0, x1 = vano["x0"], vano["x1"]
    z0 = vano["z0"]
    radio = vano.get("radio", 0.0)
    if radio <= 0.001:
        z1 = vano["z1"]
        return [(x0, z0), (x0, z1), (x1, z1), (x1, z0)]
    spring = vano["spring"]
    cx = (x0 + x1) * 0.5
    muestras = arco_muestras(n, radio)
    pts = [(x0, z0), (x0, spring)]
    for du, dz in muestras[1:]:
        pts.append((cx + du, spring + dz))
    pts.append((x1, z0))
    return pts


def dentro(vano, u, v):
    if u <= vano["x0"] + 1e-4 or u >= vano["x1"] - 1e-4:
        return False
    if v <= vano["z0"] + 1e-4:
        return False
    radio = vano.get("radio", 0.0)
    if radio <= 0.001:
        return v < vano["z1"] - 1e-4
    if v >= vano["spring"] + radio - 1e-4:
        return False
    if v <= vano["spring"]:
        return True
    cx = (vano["x0"] + vano["x1"]) * 0.5
    du = u - cx
    dv = v - vano["spring"]
    return du * du + dv * dv < (radio - 0.02) * (radio - 0.02)


def muro_con_vanos(muro, origen, eje_u, eje_v, u0, u1, v0, v1, vanos, hacia_fuera):
    us = {u0, u1}
    vs = {v0, v1}
    for vano in vanos:
        us.add(vano["x0"])
        us.add(vano["x1"])
        vs.add(vano["z0"])
        radio = vano.get("radio", 0.0)
        if radio > 0.001:
            vs.add(vano["spring"])
            vs.add(vano["spring"] + radio)
            cx = (vano["x0"] + vano["x1"]) * 0.5
            for du, dz in arco_muestras(8, radio):
                us.add(cx + du)
                vs.add(vano["spring"] + dz)
        else:
            vs.add(vano["z1"])
    us = sorted(x for x in us if u0 - 1e-4 <= x <= u1 + 1e-4)
    vs = sorted(z for z in vs if v0 - 1e-4 <= z <= v1 + 1e-4)
    for i in range(len(us) - 1):
        for j in range(len(vs) - 1):
            cu = (us[i] + us[i + 1]) * 0.5
            cv = (vs[j] + vs[j + 1]) * 0.5
            if any(dentro(vano, cu, cv) for vano in vanos):
                continue
            pts = [
                punto(origen, eje_u, eje_v, us[i], vs[j]),
                punto(origen, eje_u, eje_v, us[i], vs[j + 1]),
                punto(origen, eje_u, eje_v, us[i + 1], vs[j + 1]),
                punto(origen, eje_u, eje_v, us[i + 1], vs[j]),
            ]
            uvs = [uv2(us[i], vs[j]), uv2(us[i], vs[j + 1]), uv2(us[i + 1], vs[j + 1]), uv2(us[i + 1], vs[j])]
            muro.quad(pts, uvs, hacia_fuera)


def vano_hundido(muro, vidrio, origen, eje_u, eje_v, hacia_dentro, vano, fondo=0.36):
    borde = contorno_vano(vano)
    n = len(borde)
    cx = sum(p[0] for p in borde) / n
    cz = sum(p[1] for p in borde) / n
    for i in range(n):
        u0, v0 = borde[i]
        u1, v1 = borde[(i + 1) % n]
        mitad = ((u0 + u1) * 0.5, (v0 + v1) * 0.5)
        tangente = vnorm(vsub(punto(origen, eje_u, eje_v, u1, v1), punto(origen, eje_u, eje_v, u0, v0)))
        normal_boca = vnorm(vcross(tangente, hacia_dentro))
        al_centro = vsub(punto(origen, eje_u, eje_v, cx, cz), punto(origen, eje_u, eje_v, mitad[0], mitad[1]))
        if vdot(normal_boca, al_centro) < 0:
            normal_boca = vmul(normal_boca, -1)
        afuera0 = punto(origen, eje_u, eje_v, u0, v0)
        afuera1 = punto(origen, eje_u, eje_v, u1, v1)
        adentro0 = vadd(afuera0, vmul(hacia_dentro, fondo))
        adentro1 = vadd(afuera1, vmul(hacia_dentro, fondo))
        muro.quad(
            [afuera0, afuera1, adentro1, adentro0],
            [uv2(0, v0), uv2(0.2, v1), uv2(fondo, v1), uv2(fondo, v0)],
            normal_boca,
        )
    if vidrio is None:
        return
    atras = vadd(origen, vmul(hacia_dentro, fondo))
    pts = [punto(atras, eje_u, eje_v, u, v) for u, v in borde]
    uvs = [uv2(u, v) for u, v in borde]
    for i in range(1, n - 1):
        vidrio.tri([pts[0], pts[i], pts[i + 1]], [uvs[0], uvs[i], uvs[i + 1]], vmul(hacia_dentro, -1))


def archivolta(piedra, origen, eje_u, eje_v, hacia_fuera, vano, grosor=0.16, canto=0.20):
    radio = vano.get("radio", 0.0)
    if radio <= 0.001:
        return
    cx = (vano["x0"] + vano["x1"]) * 0.5
    spring = vano["spring"]
    muestras = arco_muestras(8, radio)
    for i in range(len(muestras) - 1):
        for paso, rr in ((0, radio),):
            du0, dz0 = muestras[i]
            du1, dz1 = muestras[i + 1]
            a = (cx + du0, spring + dz0)
            b = (cx + du1, spring + dz1)
            c = (cx + du1 * (radio + canto) / radio, spring + dz1 * (radio + canto) / radio)
            d = (cx + du0 * (radio + canto) / radio, spring + dz0 * (radio + canto) / radio)
            frente = vmul(hacia_fuera, grosor)
            pa = vadd(punto(origen, eje_u, eje_v, a[0], a[1]), frente)
            pb = vadd(punto(origen, eje_u, eje_v, b[0], b[1]), frente)
            pc = vadd(punto(origen, eje_u, eje_v, c[0], c[1]), frente)
            pd = vadd(punto(origen, eje_u, eje_v, d[0], d[1]), frente)
            piedra.quad([pa, pb, pc, pd], [uv2(a[0], a[1]), uv2(b[0], b[1]), uv2(c[0], c[1]), uv2(d[0], d[1])], hacia_fuera)
    for signo, xj in ((-1, vano["x0"]), (1, vano["x1"])):
        z0 = vano["z0"]
        z1 = spring
        frente = vmul(hacia_fuera, grosor)
        a = vadd(punto(origen, eje_u, eje_v, xj, z0), frente)
        b = vadd(punto(origen, eje_u, eje_v, xj, z1), frente)
        c = vadd(punto(origen, eje_u, eje_v, xj + signo * canto, z1), frente)
        d = vadd(punto(origen, eje_u, eje_v, xj + signo * canto, z0), frente)
        piedra.quad([a, b, c, d], [uv2(xj, z0), uv2(xj, z1), uv2(xj + canto, z1), uv2(xj + canto, z0)], hacia_fuera)


def reja(reja, origen, eje_u, eje_v, hacia_fuera, vano, saliente=-0.10):
    x0, x1 = vano["x0"] + 0.08, vano["x1"] - 0.08
    z0 = vano["z0"] + 0.06
    radio = vano.get("radio", 0.0)
    z1 = vano["spring"] - 0.04 if radio > 0.001 else vano["z1"] - 0.06
    if z1 - z0 < 0.3 or x1 - x0 < 0.3:
        return
    frente = vmul(hacia_fuera, saliente)
    x = x0
    while x <= x1 + 1e-4:
        a = vadd(punto(origen, eje_u, eje_v, x, z0), frente)
        caja_en(reja, a, eje_u, eje_v, hacia_fuera, 0.025, 0.02, z1 - z0)
        x += 0.16
    medio = (z0 + z1) * 0.5
    a = vadd(punto(origen, eje_u, eje_v, x0, medio), frente)
    caja_en(reja, a, eje_u, eje_v, hacia_fuera, x1 - x0, 0.02, 0.025)


def caja_en(parte, origen, eje_u, eje_v, hacia_fuera, ancho, profundo, alto):
    # origen es la esquina inferior izquierda, sobre la cara.
    eu = vnorm(eje_u)
    ev = vnorm(eje_v)
    ef = vnorm(hacia_fuera)
    x = origen
    y = vadd(origen, vmul(eu, ancho))
    z = vadd(y, vmul(ev, alto))
    w = vadd(origen, vmul(ev, alto))
    o2 = vadd(origen, vmul(ef, profundo))
    y2 = vadd(y, vmul(ef, profundo))
    z2 = vadd(z, vmul(ef, profundo))
    w2 = vadd(w, vmul(ef, profundo))
    parte.quad([x, y, z, w], [uv2(0, 0), uv2(ancho, 0), uv2(ancho, alto), uv2(0, alto)], ev)
    parte.quad([x, w, w2, o2], [uv2(0, 0), uv2(0, alto), uv2(profundo, alto), uv2(profundo, 0)], vmul(eu, -1))
    parte.quad([y, y2, z2, z], [uv2(0, 0), uv2(profundo, 0), uv2(profundo, alto), uv2(0, alto)], eu)
    parte.quad([o2, w2, z2, y2], [uv2(0, 0), uv2(0, alto), uv2(ancho, alto), uv2(ancho, 0)], ef)


def puerta_madera(madera, origen, eje_u, eje_v, hacia_fuera, vano, saliente=-0.20):
    x0, x1 = vano["x0"] + 0.08, vano["x1"] - 0.08
    z0 = vano["z0"] + 0.02
    radio = vano.get("radio", 0.0)
    z1 = (vano["spring"] - 0.04) if radio > 0.001 else (vano["z1"] - 0.08)
    # saliente negativo mete la hoja en el vano. El grosor sale hacia la calle.
    frente = vmul(hacia_fuera, saliente)
    medio = (x0 + x1) * 0.5
    for a, b in ((x0, medio - 0.02), (medio + 0.02, x1)):
        p = vadd(punto(origen, eje_u, eje_v, a, z0), frente)
        caja_en(madera, p, eje_u, eje_v, hacia_fuera, b - a, 0.07, z1 - z0)


def puerta_abierta(madera, origen, eje_u, eje_v, hacia_dentro, vano, signo=-1.0):
    # Una hoja, abierta hacia el cuarto, para que se vean los dos pisos.
    x0, x1 = vano["x0"] + 0.06, vano["x1"] - 0.06
    z0 = vano["z0"] + 0.02
    radio = vano.get("radio", 0.0)
    z1 = (vano["spring"] - 0.02) if radio > 0.001 else (vano["z1"] - 0.04)
    if z1 - z0 < 0.4 or x1 - x0 < 0.4:
        return
    hoja = min(0.72, (x1 - x0) * 0.55)
    angulo = 1.40
    eu = vnorm(eje_u)
    ed = vnorm(hacia_dentro)
    c, s = math.cos(angulo), math.sin(angulo)
    if signo >= 0:
        bisagra = x0
        eje_hoja = vnorm(vadd(vmul(eu, c), vmul(ed, s)))
    else:
        bisagra = x1
        eje_hoja = vnorm(vadd(vmul(eu, -c), vmul(ed, s)))
    normal = vnorm(vcross(eje_v, eje_hoja))
    p = vadd(punto(origen, eje_u, eje_v, bisagra, z0), vmul(ed, 0.10))
    caja_en(madera, p, eje_hoja, eje_v, normal, hoja, 0.06, z1 - z0)


def balaustre(madera, origen, eje_u, eje_v, hacia_fuera, alto=0.78):
    lados = 6
    radios = [(0.0, 0.045), (0.16, 0.030), (0.46, 0.058), (0.68, 0.028), (1.0, 0.040)]
    eu = vnorm(eje_u)
    ev = vnorm(eje_v)
    anillos = []
    for t, r in radios:
        anillo = []
        base = vadd(origen, vmul(ev, alto * t))
        for i in range(lados):
            a = i * math.tau / lados
            anillo.append(vadd(base, vadd(vmul(eu, math.cos(a) * r), vmul(hacia_fuera, math.sin(a) * r))))
        anillos.append(anillo)
    for k in range(len(anillos) - 1):
        a = anillos[k]
        b = anillos[k + 1]
        for i in range(lados):
            j = (i + 1) % lados
            mitad = vmul(vadd(vadd(a[i], a[j]), vadd(b[i], b[j])), 0.25)
            fuera = vnorm(vsub(mitad, vadd(origen, vmul(ev, alto * 0.5))))
            madera.quad(
                [a[i], a[j], b[j], b[i]],
                [uv2(i * 0.04, radios[k][0]), uv2((i + 1) * 0.04, radios[k][0]),
                 uv2((i + 1) * 0.04, radios[k + 1][0] * alto), uv2(i * 0.04, radios[k + 1][0] * alto)],
                fuera,
            )


def balcon(madera, origen_cara, eje_u, eje_v, hacia_fuera, u0, u1, z_piso, vuelo=1.05):
    # origen_cara está en la fachada. El balcón sale hacia la calle.
    eu = vnorm(eje_u)
    ev = vnorm(eje_v)
    ef = vnorm(hacia_fuera)
    z = z_piso
    # Losa
    o = vadd(punto(origen_cara, eu, ev, u0, z), vmul(ef, 0.04))
    caja_en(madera, o, eu, ev, ef, u1 - u0, vuelo, 0.10)
    # Canes
    n = max(2, int((u1 - u0) / 1.35))
    for i in range(n):
        t = (i + 0.5) / n
        u = u0 + (u1 - u0) * t - 0.07
        c = vadd(punto(origen_cara, eu, ev, u, z - 0.16), vmul(ef, 0.02))
        caja_en(madera, c, eu, ev, ef, 0.14, vuelo * 0.86, 0.14)
    # Baranda frontal
    frente = vadd(punto(origen_cara, eu, ev, u0, z + 0.10), vmul(ef, vuelo - 0.08))
    largo = u1 - u0
    # Pasamanos
    mano = vadd(frente, vmul(ev, 0.70))
    caja_en(madera, mano, eu, ev, ef, largo, 0.08, 0.07)
    # Zapata
    caja_en(madera, frente, eu, ev, ef, largo, 0.07, 0.08)
    # Balaustres
    u = u0 + 0.12
    while u < u1 - 0.08:
        p = vadd(punto(origen_cara, eu, ev, u, z + 0.18), vmul(ef, vuelo - 0.05))
        balaustre(madera, p, eu, ev, ef, 0.64)
        u += 0.20
    # Laterales de la baranda
    for u_lat, signo in ((u0, 1), (u1, -1)):
        p = vadd(punto(origen_cara, eu, ev, u_lat, z + 0.10), vmul(ef, 0.08))
        # eje a lo largo del vuelo: hacia_fuera. Reusamos caja_en con eu = hacia fuera.
        caja_en(madera, p, ef, ev, vmul(eu, signo), vuelo - 0.08, 0.06, 0.78)


def techo_dos_aguas(teja, muro, x0, x1, y_frente, y_fondo, z_alero, rise, vuelo_frente, vuelo_lado, vuelo_fondo):
    y_cumbrera = (y_frente + y_fondo) * 0.5
    z_cum = z_alero + rise
    xf0 = x0 - vuelo_lado
    xf1 = x1 + vuelo_lado
    yf = y_frente - vuelo_frente
    yb = y_fondo + vuelo_fondo
    # Vertiente a la calle
    largo = math.hypot(y_cumbrera - yf, rise)
    teja.quad(
        [(xf0, yf, z_alero), (xf1, yf, z_alero), (xf1, y_cumbrera, z_cum), (xf0, y_cumbrera, z_cum)],
        [uv2(xf0, 0), uv2(xf1, 0), uv2(xf1, largo), uv2(xf0, largo)],
        (0, -rise, y_cumbrera - yf),
    )
    largo_b = math.hypot(yb - y_cumbrera, rise)
    teja.quad(
        [(xf1, yb, z_alero), (xf0, yb, z_alero), (xf0, y_cumbrera, z_cum), (xf1, y_cumbrera, z_cum)],
        [uv2(xf0, 0), uv2(xf1, 0), uv2(xf1, largo_b), uv2(xf0, largo_b)],
        (0, rise, yb - y_cumbrera),
    )
    # Tímpanos
    for x, signo in ((x0, -1), (x1, 1)):
        muro.tri(
            [(x, y_frente, z_alero), (x, y_fondo, z_alero), (x, y_cumbrera, z_cum)],
            [uv2(y_frente, z_alero), uv2(y_fondo, z_alero), uv2(y_cumbrera, z_cum)],
            (signo, 0, 0),
        )
    # Canto del alero, para que la teja no sea una hoja
    canto = 0.10
    teja.quad(
        [(xf0, yf, z_alero), (xf0, yf, z_alero - canto), (xf1, yf, z_alero - canto), (xf1, yf, z_alero)],
        [uv2(xf0, 0), uv2(xf0, canto), uv2(xf1, canto), uv2(xf1, 0)],
        (0, -1, 0),
    )


def canes_alero(madera, x0, x1, y_frente, z_alero, vuelo):
    x = x0 + 0.35
    while x < x1 - 0.2:
        caja(madera, x, x + 0.14, y_frente - vuelo, y_frente + 0.15, z_alero - 0.22, z_alero - 0.06)
        x += 0.95


def techo_cuatro_aguas(teja, x0, x1, y0, y1, z_alero, rise, vuelo):
    xf0, xf1 = x0 - vuelo, x1 + vuelo
    yf0, yf1 = y0 - vuelo, y1 + vuelo
    cx = (x0 + x1) * 0.5
    cy = (y0 + y1) * 0.5
    zc = z_alero + rise
    cima = (cx, cy, zc)
    lados = [
        ([(xf0, yf0, z_alero), (xf1, yf0, z_alero), cima], (0, -1, 0.35)),
        ([(xf1, yf1, z_alero), (xf0, yf1, z_alero), cima], (0, 1, 0.35)),
        ([(xf0, yf1, z_alero), (xf0, yf0, z_alero), cima], (-1, 0, 0.35)),
        ([(xf1, yf0, z_alero), (xf1, yf1, z_alero), cima], (1, 0, 0.35)),
    ]
    for pts, outward in lados:
        a, b, c = pts
        du = math.hypot(b[0] - a[0], b[1] - a[1])
        dv = math.hypot(c[0] - (a[0] + b[0]) * 0.5, c[1] - (a[1] + b[1]) * 0.5, c[2] - z_alero)
        teja.tri(pts, [uv2(0, 0), uv2(du, 0), uv2(du * 0.5, dv)], outward)


def banda(piedra, a0, a1, b0, b1, z0, z1, cortes, eje):
    # eje "x": la banda corre en X y se corta en X. eje "y": corre en Y.
    cortes = sorted(cortes)
    cursor = a0
    for a, b in cortes:
        if a > cursor + 0.04:
            if eje == "x":
                caja(piedra, cursor, a, b0, b1, z0, z1)
            else:
                caja(piedra, b0, b1, cursor, a, z0, z1)
        cursor = max(cursor, b)
    if cursor < a1 - 0.04:
        if eje == "x":
            caja(piedra, cursor, a1, b0, b1, z0, z1)
        else:
            caja(piedra, b0, b1, cursor, a1, z0, z1)


def zocalo(piedra, x0, x1, y0, y1, alto, sale, puertas, puertas_lado=None):
    # Banda de piedra. Se abre en cada puerta para que el vano llegue al suelo.
    s = sale
    caja(piedra, x0 - s, x1 + s, y1, y1 + s, 0, alto)
    caja(piedra, x0 - s, x0, y0 - s, y1 + s, 0, alto)
    banda(piedra, y0 - s, y1 + s, x1, x1 + s, 0, alto, puertas_lado or [], "y")
    banda(piedra, x0 - s, x1 + s, y0 - s, y0, 0, alto, puertas, "x")
    banda(piedra, x0 - s, x1 + s, y0 - s, y0, alto, alto + 0.07, puertas, "x")
    if puertas_lado:
        banda(piedra, y0 - s, y1 + s, x1, x1 + s, alto, alto + 0.07, puertas_lado, "y")


def pilastras(piedra, x0, x1, y_frente, alto):
    for x in (x0, x1 - 0.28):
        caja(piedra, x, x + 0.28, y_frente - 0.14, y_frente + 0.02, 0.0, alto)


def cornisa(piedra, x0, x1, y_frente, z, alto=0.22, sale=0.16):
    caja(piedra, x0 - 0.06, x1 + 0.06, y_frente - sale, y_frente + 0.04, z, z + alto)


def cuerpo(partes, ancho, fondo, alto, vanos_frente, vanos_lado=None, lado="+X"):
    x0, x1 = -ancho * 0.5, ancho * 0.5
    y0, y1 = -fondo * 0.5, fondo * 0.5
    muro = partes["Muro"]
    vidrio = partes["AdornoVidrio"]
    piedra = partes["Piedra"]
    reja_m = partes["AdornoReja"]
    madera = partes["AdornoMadera"]
    # Fachada a la calle
    origen = (0, y0, 0)
    eje_u = (1, 0, 0)
    eje_v = (0, 0, 1)
    fuera = (0, -1, 0)
    dentro_v = (0, 1, 0)
    muro_con_vanos(muro, origen, eje_u, eje_v, x0, x1, 0, alto, vanos_frente, fuera)
    for vano in vanos_frente:
        tapa = None if (vano.get("puerta") or vano.get("abierto")) else vidrio
        vano_hundido(muro, tapa, origen, eje_u, eje_v, dentro_v, vano, GROSOR)
        archivolta(piedra, origen, eje_u, eje_v, fuera, vano)
        if vano.get("reja"):
            reja(reja_m, origen, eje_u, eje_v, fuera, vano, -0.10)
        if vano.get("puerta"):
            puerta_abierta(madera, origen, eje_u, eje_v, dentro_v, vano, signo=vano.get("hoja", -1))
    # Lados y fondo, cerrados. El de la esquina trae su propia segunda fachada.
    if vanos_lado:
        if lado == "+X":
            origen_l = (x1, 0, 0)
            eje_ul = (0, 1, 0)
            fuera_l = (1, 0, 0)
            dentro_l = (-1, 0, 0)
            u0, u1 = y0, y1
        else:
            origen_l = (x0, 0, 0)
            eje_ul = (0, -1, 0)
            fuera_l = (-1, 0, 0)
            dentro_l = (1, 0, 0)
            u0, u1 = -y1, -y0
        muro_con_vanos(muro, origen_l, eje_ul, eje_v, u0, u1, 0, alto, vanos_lado, fuera_l)
        for vano in vanos_lado:
            tapa = None if (vano.get("puerta") or vano.get("abierto")) else vidrio
            vano_hundido(muro, tapa, origen_l, eje_ul, eje_v, dentro_l, vano, GROSOR)
            archivolta(piedra, origen_l, eje_ul, eje_v, fuera_l, vano)
            if vano.get("reja"):
                reja(reja_m, origen_l, eje_ul, eje_v, fuera_l, vano, -0.10)
            if vano.get("puerta"):
                puerta_abierta(madera, origen_l, eje_ul, eje_v, dentro_l, vano, signo=vano.get("hoja", -1))
        # El otro lado y el fondo quedan lisos.
        muro.quad(
            [(x0, y0, 0), (x0, y0, alto), (x0, y1, alto), (x0, y1, 0)] if lado == "+X"
            else [(x1, y1, 0), (x1, y1, alto), (x1, y0, alto), (x1, y0, 0)],
            [uv2(y0, 0), uv2(y0, alto), uv2(y1, alto), uv2(y1, 0)],
            (-1, 0, 0) if lado == "+X" else (1, 0, 0),
        )
    else:
        muro.quad(
            [(x0, y1, 0), (x0, y0, 0), (x0, y0, alto), (x0, y1, alto)],
            [uv2(0, 0), uv2(fondo, 0), uv2(fondo, alto), uv2(0, alto)],
            (-1, 0, 0),
        )
        muro.quad(
            [(x1, y0, 0), (x1, y1, 0), (x1, y1, alto), (x1, y0, alto)],
            [uv2(0, 0), uv2(fondo, 0), uv2(fondo, alto), uv2(0, alto)],
            (1, 0, 0),
        )
    muro.quad(
        [(x1, y1, 0), (x0, y1, 0), (x0, y1, alto), (x1, y1, alto)],
        [uv2(x0, 0), uv2(x1, 0), uv2(x1, alto), uv2(x0, alto)],
        (0, 1, 0),
    )
    return x0, x1, y0, y1


def losa_con_hueco(parte, x0, x1, y0, y1, z0, z1, hueco):
    if y1 - y0 < 0.08 or x1 - x0 < 0.08:
        return
    if hueco is None:
        caja(parte, x0, x1, y0, y1, z0, z1)
        return
    hx0, hx1, hy0, hy1 = hueco
    hx0, hx1 = max(hx0, x0), min(hx1, x1)
    hy0, hy1 = max(hy0, y0), min(hy1, y1)
    if hx1 - hx0 < 0.08 or hy1 - hy0 < 0.08:
        caja(parte, x0, x1, y0, y1, z0, z1)
        return
    if hy0 - y0 > 0.04:
        caja(parte, x0, x1, y0, hy0, z0, z1)
    if y1 - hy1 > 0.04:
        caja(parte, x0, x1, hy1, y1, z0, z1)
    if hx0 - x0 > 0.04:
        caja(parte, x0, hx0, hy0, hy1, z0, z1)
    if x1 - hx1 > 0.04:
        caja(parte, hx1, x1, hy0, hy1, z0, z1)


def viga_cortada(parte, x, grueso, y0, y1, z0, z1, hueco):
    if hueco is None or x + grueso < hueco[0] or x > hueco[1]:
        caja(parte, x, x + grueso, y0, y1, z0, z1)
        return
    hy0, hy1 = hueco[2], hueco[3]
    if hy0 - y0 > 0.08:
        caja(parte, x, x + grueso, y0, min(hy0, y1), z0, z1)
    if y1 - hy1 > 0.08:
        caja(parte, x, x + grueso, max(hy1, y0), y1, z0, z1)


def escalera(madera, x0, x1, y0, y1, z0, z1, pasos=12):
    n = pasos
    dy = (y1 - y0) / n
    dz = (z1 - z0) / n
    for i in range(n):
        ya = y0 + dy * i
        yb = ya + dy
        zb = z0 + dz * (i + 1)
        caja(madera, x0, x1, ya, yb, z0, zb)
        caja(madera, x0 - 0.07, x0, ya, yb, zb, zb + 0.68)
        caja(madera, x0 - 0.07, x0 + 0.02, ya, yb, zb + 0.68, zb + 0.74)


def baranda_x(madera, x0, x1, y, z, gap=None):
    def tramo(a, b):
        if b - a < 0.18:
            return
        caja(madera, a, b, y, y + 0.06, z + 0.78, z + 0.86)
        u = a + 0.06
        while u < b - 0.06:
            caja(madera, u, u + 0.05, y - 0.01, y + 0.07, z, z + 0.80)
            u += 0.32

    if not gap:
        tramo(x0, x1)
        return
    tramo(x0, gap[0])
    tramo(gap[1], x1)


def baranda_y(madera, y0, y1, x, z, gap=None):
    def tramo(a, b):
        if b - a < 0.18:
            return
        caja(madera, x, x + 0.06, a, b, z + 0.78, z + 0.86)
        v = a + 0.06
        while v < b - 0.06:
            caja(madera, x - 0.01, x + 0.07, v, v + 0.05, z, z + 0.80)
            v += 0.32

    if not gap:
        tramo(y0, y1)
        return
    tramo(y0, gap[0])
    tramo(gap[1], y1)


def mesa(madera, x, y, z):
    caja(madera, x, x + 1.35, y, y + 0.72, z + 0.62, z + 0.70)
    for dx, dy in ((0.06, 0.06), (1.18, 0.06), (0.06, 0.56), (1.18, 0.56)):
        caja(madera, x + dx, x + dx + 0.08, y + dy, y + dy + 0.08, z, z + 0.62)


def baul(madera, x, y, z):
    caja(madera, x, x + 0.92, y, y + 0.48, z, z + 0.40)
    caja(madera, x + 0.08, x + 0.84, y - 0.04, y + 0.52, z + 0.18, z + 0.24)


def cascaron(muro, x0, x1, y0, y1, alto, vanos_frente, vanos_lado, lado):
    g = GROSOR
    ix0, ix1 = x0 + g, x1 - g
    iy0, iy1 = y0 + g, y1 - g
    tope = alto - 0.08
    muro_con_vanos(muro, (0, iy0, 0), (1, 0, 0), (0, 0, 1), ix0, ix1, 0, tope, vanos_frente or [], (0, 1, 0))
    muro_con_vanos(muro, (0, iy1, 0), (1, 0, 0), (0, 0, 1), ix0, ix1, 0, tope, [], (0, -1, 0))
    muro_con_vanos(muro, (ix0, 0, 0), (0, 1, 0), (0, 0, 1), iy0, iy1, 0, tope, [], (1, 0, 0))
    vanos_der = vanos_lado if lado == "+X" and vanos_lado else []
    muro_con_vanos(muro, (ix1, 0, 0), (0, 1, 0), (0, 0, 1), iy0, iy1, 0, tope, vanos_der, (-1, 0, 0))
    return ix0, ix1, iy0, iy1


def cielo_y_vigas(partes, ix0, ix1, iy0, iy1, alto):
    caja(partes["AdornoMadera"], ix0 + 0.04, ix1 - 0.04, iy0 + 0.04, iy1 - 0.04, alto - 0.22, alto - 0.08)
    x = ix0 + 0.55
    while x < ix1 - 0.45:
        caja(partes["AdornoMadera"], x, x + 0.16, iy0 + 0.1, iy1 - 0.1, alto - 0.40, alto - 0.22)
        x += 1.55


def interior_recta(partes, ancho, fondo, alto, z2, vanos_frente, y_losa, escalon, mesa_xy, baul_xy, vanos_lado=None, lado="+X"):
    # Zaguán a doble altura y entrepiso de madera al fondo. La escalera sube a la vista.
    x0, x1 = -ancho * 0.5, ancho * 0.5
    y0, y1 = -fondo * 0.5, fondo * 0.5
    ix0, ix1, iy0, iy1 = cascaron(partes["Muro"], x0, x1, y0, y1, alto, vanos_frente, vanos_lado, lado)
    caja(partes["PisoPiedra"], ix0 + 0.02, ix1 - 0.02, iy0 + 0.02, iy1 - 0.02, 0.03, 0.11)
    sx0, sx1, sy0, sy1 = escalon
    hueco = (sx0 - 0.16, sx1 + 0.08, y_losa, sy1 + 0.04)
    losa_con_hueco(partes["PisoMadera"], ix0 + 0.02, ix1 - 0.02, y_losa, iy1 - 0.02, z2 - 0.20, z2, hueco)
    x = ix0 + 0.35
    while x < ix1 - 0.4:
        viga_cortada(partes["AdornoMadera"], x, 0.14, y_losa, iy1 - 0.06, z2 - 0.36, z2 - 0.20, hueco)
        x += 1.15
    cielo_y_vigas(partes, ix0, ix1, iy0, iy1, alto)
    escalera(partes["AdornoMadera"], sx0, sx1, sy0, sy1, 0.11, z2)
    baranda_x(partes["AdornoMadera"], ix0 + 0.1, ix1 - 0.1, y_losa + 0.02, z2, (sx0 - 0.08, sx1 + 0.1))
    if mesa_xy:
        mesa(partes["AdornoMadera"], mesa_xy[0], mesa_xy[1], 0.11)
    if baul_xy:
        baul(partes["AdornoMadera"], baul_xy[0], baul_xy[1], z2)


def interior_esquina(partes, ancho, fondo, alto, z2, vanos_frente, vanos_lado, hueco, escalon):
    # El rincón de la plaza queda a doble altura. El entrepiso es una L.
    x0, x1 = -ancho * 0.5, ancho * 0.5
    y0, y1 = -fondo * 0.5, fondo * 0.5
    ix0, ix1, iy0, iy1 = cascaron(partes["Muro"], x0, x1, y0, y1, alto, vanos_frente, vanos_lado, "+X")
    caja(partes["PisoPiedra"], ix0 + 0.02, ix1 - 0.02, iy0 + 0.02, iy1 - 0.02, 0.03, 0.11)
    losa_con_hueco(partes["PisoMadera"], ix0 + 0.02, ix1 - 0.02, iy0 + 0.02, iy1 - 0.02, z2 - 0.20, z2, hueco)
    x = ix0 + 0.4
    while x < ix1 - 0.4:
        viga_cortada(partes["AdornoMadera"], x, 0.14, iy0 + 0.08, iy1 - 0.06, z2 - 0.36, z2 - 0.20, hueco)
        x += 1.2
    cielo_y_vigas(partes, ix0, ix1, iy0, iy1, alto)
    sx0, sx1, sy0, sy1 = escalon
    escalera(partes["AdornoMadera"], sx0, sx1, sy0, sy1, 0.11, z2)
    hx0, hx1, hy0, hy1 = hueco
    baranda_x(partes["AdornoMadera"], hx0, ix1 - 0.15, hy1 + 0.02, z2, (sx0 - 0.1, sx1 + 0.12))
    baranda_y(partes["AdornoMadera"], hy0 + 0.1, hy1, hx0 + 0.02, z2)
    caja(partes["AdornoMadera"], hx0 - 0.06, hx0 + 0.08, hy1 - 0.06, hy1 + 0.08, 0.11, z2)
    mesa(partes["AdornoMadera"], ix0 + 0.4, hy1 + 0.8, 0.11)
    baul(partes["AdornoMadera"], ix0 + 0.55, hy1 + 1.15, z2)


def casa_balcon(partes):
    ancho, fondo, alto = 10.40, 7.40, 7.05
    puerta = {"x0": -0.82, "x1": 0.82, "z0": 0.0, "spring": 2.15, "radio": 0.82, "puerta": True, "hoja": 1}
    izq = {"x0": -3.35, "x1": -2.15, "z0": 1.42, "spring": 2.55, "radio": 0.60, "reja": True}
    der = {"x0": 2.15, "x1": 3.35, "z0": 1.42, "spring": 2.55, "radio": 0.60, "reja": True}
    alta = {"x0": -0.78, "x1": 0.78, "z0": 4.55, "z1": 6.15, "radio": 0.0}
    x0, x1, y0, y1 = cuerpo(partes, ancho, fondo, alto, [puerta, izq, der, alta])
    zocalo(partes["Piedra"], x0, x1, y0, y1, 1.16, 0.10, [(-0.95, 0.95)])
    pilastras(partes["Piedra"], x0 + 0.08, x1 - 0.08, y0, alto - 0.18)
    cornisa(partes["Piedra"], x0, x1, y0, alto - 0.20)
    balcon(partes["AdornoMadera"], (0, y0, 0), (1, 0, 0), (0, 0, 1), (0, -1, 0), -3.15, 3.15, 4.08, 1.08)
    techo_dos_aguas(partes["Teja"], partes["Muro"], x0, x1, y0, y1, alto, 1.65, 0.82, 0.06, 0.40)
    canes_alero(partes["AdornoMadera"], x0 + 0.2, x1 - 0.2, y0, alto, 0.78)
    interior_recta(
        partes, ancho, fondo, alto, 3.98, [puerta, izq, der, alta],
        y0 + 2.20, (-0.05, 1.25, y0 + 0.48, y0 + 4.15),
        (-3.55, y0 + 4.55), (-2.4, y0 + 2.85),
    )
    return {"ancho": ancho, "fondo": fondo, "alto": alto, "puerta": 0.0}


def casa_dos_balcones(partes):
    ancho, fondo, alto = 11.20, 7.60, 7.15
    reja_izq = {"x0": -4.70, "x1": -3.25, "z0": 0.0, "z1": 2.55, "radio": 0.0, "reja": True, "abierto": True}
    ventana = {"x0": -1.15, "x1": 0.15, "z0": 1.38, "z1": 2.55, "radio": 0.0, "reja": True}
    puerta = {"x0": 3.05, "x1": 4.50, "z0": 0.0, "z1": 2.58, "radio": 0.0, "puerta": True}
    alta_izq = {"x0": -4.35, "x1": -2.85, "z0": 4.35, "z1": 5.85, "radio": 0.0}
    alta_der = {"x0": 0.55, "x1": 2.15, "z0": 4.35, "z1": 5.85, "radio": 0.0}
    x0, x1, y0, y1 = cuerpo(partes, ancho, fondo, alto, [reja_izq, ventana, puerta, alta_izq, alta_der])
    zocalo(partes["Piedra"], x0, x1, y0, y1, 1.20, 0.10, [(-4.85, -3.10), (2.90, 4.65)])
    pilastras(partes["Piedra"], x0 + 0.08, x1 - 0.08, y0, alto - 0.18)
    cornisa(partes["Piedra"], x0, x1, y0, alto - 0.20)
    balcon(partes["AdornoMadera"], (0, y0, 0), (1, 0, 0), (0, 0, 1), (0, -1, 0), -4.55, -2.65, 3.95, 0.92)
    balcon(partes["AdornoMadera"], (0, y0, 0), (1, 0, 0), (0, 0, 1), (0, -1, 0), 0.35, 2.35, 3.95, 0.92)
    techo_dos_aguas(partes["Teja"], partes["Muro"], x0, x1, y0, y1, alto, 1.72, 0.82, 0.06, 0.40)
    canes_alero(partes["AdornoMadera"], x0 + 0.2, x1 - 0.2, y0, alto, 0.78)
    interior_recta(
        partes, ancho, fondo, alto, 3.88, [reja_izq, ventana, puerta, alta_izq, alta_der],
        y0 + 2.25, (2.85, 4.05, y0 + 0.55, y0 + 4.20),
        (-4.15, y0 + 4.6), (-1.4, y0 + 2.95),
    )
    return {"ancho": ancho, "fondo": fondo, "alto": alto, "puerta": 3.78}


def casa_portales(partes):
    ancho, fondo, alto = 12.60, 8.00, 7.40
    pier = 0.62
    n = 4
    luz = (ancho - pier * (n + 1)) / n
    vanos = []
    x = -ancho * 0.5 + pier
    centros = []
    for i in range(n):
        vanos.append({
            "x0": x, "x1": x + luz, "z0": 0.0,
            "spring": 2.55, "radio": luz * 0.5, "abierto": True,
        })
        centros.append(x + luz * 0.5)
        x += luz + pier
    altas = []
    for c in centros:
        altas.append({"x0": c - 0.46, "x1": c + 0.46, "z0": 5.05, "z1": 6.35, "radio": 0.0})
    x0, x1, y0, y1 = cuerpo(partes, ancho, fondo, alto, vanos + altas)
    x = x0
    for _i in range(n + 1):
        caja(partes["Piedra"], x, x + pier, y0 - 0.10, y0 + 0.22, 0.0, 2.55)
        x += pier + luz
    zocalo(partes["Piedra"], x0, x1, y0, y1, 0.38, 0.08, [(v["x0"] - 0.02, v["x1"] + 0.02) for v in vanos])
    cornisa(partes["Piedra"], x0, x1, y0, alto - 0.18)
    balcon(partes["AdornoMadera"], (0, y0, 0), (1, 0, 0), (0, 0, 1), (0, -1, 0), x0 + 0.35, x1 - 0.35, 4.08, 1.05)
    techo_dos_aguas(partes["Teja"], partes["Muro"], x0, x1, y0, y1, alto, 1.80, 0.90, 0.06, 0.45)
    canes_alero(partes["AdornoMadera"], x0 + 0.2, x1 - 0.2, y0, alto, 0.86)
    interior_recta(
        partes, ancho, fondo, alto, 3.96, vanos + altas,
        y0 + 2.50, (-2.20, -0.90, y0 + 0.70, y0 + 4.35),
        (2.15, y0 + 4.7), (0.85, y0 + 2.75),
    )
    return {"ancho": ancho, "fondo": fondo, "alto": alto, "puerta": centros[1]}


def casa_esquina(partes):
    ancho, fondo, alto = 9.20, 9.20, 7.20
    # Fachada -Y (la que en Unity mira +Z) y fachada +X.
    puerta_f = {"x0": 1.35, "x1": 2.85, "z0": 0.0, "spring": 2.10, "radio": 0.75, "puerta": True, "hoja": 1}
    ventana_f = {"x0": -2.70, "x1": -1.45, "z0": 1.40, "spring": 2.50, "radio": 0.62, "reja": True}
    # En el muro +X, u crece hacia +Y. La esquina de la plaza está en y negativo.
    puerta_l = {"x0": -2.85, "x1": -1.35, "z0": 0.0, "spring": 2.10, "radio": 0.75, "puerta": True, "hoja": 1}
    ventana_l = {"x0": 1.45, "x1": 2.70, "z0": 1.40, "spring": 2.50, "radio": 0.62, "reja": True}
    x0, x1, y0, y1 = cuerpo(
        partes, ancho, fondo, alto,
        [puerta_f, ventana_f],
        [puerta_l, ventana_l],
        lado="+X",
    )
    zocalo(partes["Piedra"], x0, x1, y0, y1, 1.12, 0.10, [(1.20, 3.00)], [(-3.00, -1.20)])
    # Zócalo del lado +X, abierto en su puerta. El zócalo general ya cerró ese lado; se recorta con la puerta visual.
    pilastras(partes["Piedra"], x0 + 0.06, x1 - 0.40, y0, alto - 0.16)
    caja(partes["Piedra"], x1 - 0.08, x1 + 0.18, y0 - 0.16, y0 + 0.20, 0.0, alto - 0.16)
    cornisa(partes["Piedra"], x0, x1, y0, alto - 0.18)
    cornisa_x(partes["Piedra"], x1, y0, y1, alto - 0.18)
    balcon(partes["AdornoMadera"], (0, y0, 0), (1, 0, 0), (0, 0, 1), (0, -1, 0), 0.15, x1 - 0.05, 3.90, 1.00)
    balcon(partes["AdornoMadera"], (x1, 0, 0), (0, 1, 0), (0, 0, 1), (1, 0, 0), y0 + 0.05, -0.15, 3.90, 1.00)
    techo_cuatro_aguas(partes["Teja"], x0, x1, y0, y1, alto, 1.70, 0.62)
    # Hueco del rincón de la plaza: x positivo, y hacia la calle.
    interior_esquina(
        partes, ancho, fondo, alto, 3.90,
        [puerta_f, ventana_f], [puerta_l, ventana_l],
        (0.25, x1 - 0.45, y0 + 0.45, y0 + 5.85),
        (0.50, 1.65, y0 + 1.35, y0 + 5.70),
    )
    return {"ancho": ancho, "fondo": fondo, "alto": alto, "puerta": 2.10}


def columna(piedra, x, y, z0, z1, lado=0.28):
    # Cuatro tramos corridos, para que se lea como columna salomónica sin una espiral.
    n = 4
    h = (z1 - z0) / n
    for i in range(n):
        dx = 0.07 if i % 2 == 0 else -0.07
        caja(piedra, x - lado * 0.5 + dx, x + lado * 0.5 + dx, y - lado * 0.45, y + lado * 0.28, z0 + i * h, z0 + (i + 1) * h)
    caja(piedra, x - lado * 0.75, x + lado * 0.75, y - lado * 0.7, y + lado * 0.45, z0, z0 + 0.18)
    caja(piedra, x - lado * 0.8, x + lado * 0.8, y - lado * 0.75, y + lado * 0.5, z1 - 0.12, z1 + 0.08)


def disco(parte, cx, y, cz, radio, n=12):
    yf = y - 0.05
    centro = (cx, yf, cz)
    for i in range(n):
        a0 = i * 2.0 * math.pi / n
        a1 = (i + 1) * 2.0 * math.pi / n
        p0 = (cx + math.cos(a0) * radio, yf, cz + math.sin(a0) * radio)
        p1 = (cx + math.cos(a1) * radio, yf, cz + math.sin(a1) * radio)
        parte.tri([centro, p0, p1], [uv2(cx, cz), uv2(p0[0], p0[2]), uv2(p1[0], p1[2])], (0, -1, 0))


def cruz(piedra, x, y, z, alto=1.15):
    caja(piedra, x - 0.07, x + 0.07, y - 0.18, y - 0.02, z, z + alto)
    caja(piedra, x - 0.36, x + 0.36, y - 0.18, y - 0.02, z + alto * 0.62, z + alto * 0.62 + 0.14)


def fronton(piedra, x0, x1, y, z0, z1):
    cx = (x0 + x1) * 0.5
    yf = y - 0.10
    piedra.tri(
        [(x0, yf, z0), (x1, yf, z0), (cx, yf, z1)],
        [uv2(x0, z0), uv2(x1, z0), uv2(cx, z1)],
        (0, -1, 0),
    )
    piedra.tri(
        [(x1, yf + 0.16, z0), (x0, yf + 0.16, z0), (cx, yf + 0.16, z1)],
        [uv2(x1, z0), uv2(x0, z0), uv2(cx, z1)],
        (0, 1, 0),
    )


def torre(partes, x0, x1, y_frente, profundo, alto, coronar):
    piedra = partes["Piedra"]
    teja = partes["Teja"]
    y1 = y_frente + profundo
    cx = (x0 + x1) * 0.5
    campana = {
        "x0": cx - 0.52, "x1": cx + 0.52, "z0": alto - 3.15,
        "spring": alto - 2.05, "radio": 0.52, "abierto": True,
    }
    origen = (0, y_frente, 0)
    muro_con_vanos(piedra, origen, (1, 0, 0), (0, 0, 1), x0, x1, 0, alto, [campana], (0, -1, 0))
    vano_hundido(piedra, None, origen, (1, 0, 0), (0, 0, 1), (0, 1, 0), campana, 0.40)
    archivolta(piedra, origen, (1, 0, 0), (0, 0, 1), (0, -1, 0), campana, 0.10, 0.14)
    caja(piedra, x0, x0 + 0.32, y_frente, y1, 0, alto)
    caja(piedra, x1 - 0.32, x1, y_frente, y1, 0, alto)
    caja(piedra, x0, x1, y1 - 0.32, y1, 0, alto)
    if coronar == "teja":
        z0 = alto
        z1 = alto + 2.15
        cima = (cx, (y_frente + y1) * 0.5, z1)
        teja.tri(
            [(x0 - 0.12, y_frente - 0.10, z0), (x1 + 0.12, y_frente - 0.10, z0), cima],
            [uv2(x0, z0), uv2(x1, z0), uv2(cx, z1)],
            (0, -1, 0),
        )
        teja.tri(
            [(x1 + 0.12, y1 + 0.08, z0), (x0 - 0.12, y1 + 0.08, z0), cima],
            [uv2(x1, z0), uv2(x0, z0), uv2(cx, z1)],
            (0, 1, 0),
        )
        teja.tri(
            [(x0 - 0.12, y1 + 0.08, z0), (x0 - 0.12, y_frente - 0.10, z0), cima],
            [uv2(y1, z0), uv2(y_frente, z0), uv2(cx, z1)],
            (-1, 0, 0),
        )
        teja.tri(
            [(x1 + 0.12, y_frente - 0.10, z0), (x1 + 0.12, y1 + 0.08, z0), cima],
            [uv2(y_frente, z0), uv2(y1, z0), uv2(cx, z1)],
            (1, 0, 0),
        )
    else:
        caja(piedra, x0 - 0.10, x1 + 0.10, y_frente - 0.10, y1 + 0.08, alto, alto + 0.32)


def techo_nave(teja, x0, x1, y0, y1, z_alero, rise):
    cx = (x0 + x1) * 0.5
    zc = z_alero + rise
    largo = y1 - y0
    teja.quad(
        [(x0, y0, z_alero), (x0, y1, z_alero), (cx, y1, zc), (cx, y0, zc)],
        [uv2(0, 0), uv2(largo, 0), uv2(largo, zc - z_alero), uv2(0, zc - z_alero)],
        (-rise, 0, cx - x0),
    )
    teja.quad(
        [(x1, y1, z_alero), (x1, y0, z_alero), (cx, y0, zc), (cx, y1, zc)],
        [uv2(0, 0), uv2(largo, 0), uv2(largo, zc - z_alero), uv2(0, zc - z_alero)],
        (rise, 0, x1 - cx),
    )


def nave(partes, x0, x1, y0, y1, alto, z_coro):
    # Suelo, dos filas de columnas, altar al fondo y coro sobre la entrada.
    caja(partes["PisoPiedra"], x0 + 0.35, x1 - 0.35, y0 + 0.45, y1 - 0.45, 0.03, 0.12)
    y = y0 + 2.4
    while y < y1 - 2.2:
        columna(partes["Piedra"], x0 + 2.15, y, 0.12, alto - 1.6, 0.22)
        columna(partes["Piedra"], x1 - 2.15, y, 0.12, alto - 1.6, 0.22)
        y += 2.8
    caja(partes["AdornoMadera"], x0 + 1.4, x1 - 1.4, y1 - 1.7, y1 - 0.55, 0.12, 1.35)
    caja(partes["AdornoMadera"], (x0 + x1) * 0.5 - 0.9, (x0 + x1) * 0.5 + 0.9, y1 - 1.85, y1 - 0.45, 1.35, 3.4)
    cruz(partes["Piedra"], (x0 + x1) * 0.5, y1 - 0.35, 3.55, 0.9)
    caja(partes["PisoMadera"], x0 + 0.4, x1 - 0.4, y0 + 0.5, y0 + 3.1, z_coro - 0.16, z_coro)
    baranda_x(partes["AdornoMadera"], x0 + 0.45, x1 - 0.45, y0 + 3.05, z_coro)
    cielo_y_vigas(partes, x0 + 0.3, x1 - 0.3, y0 + 0.4, y1 - 0.4, alto - 0.15)


def templo(partes, ancho, fondo, alto_fachada, alto_torre, coronar, con_reloj, con_fronton):
    piedra = partes["Piedra"]
    porton = partes["Porton"]
    vidrio = partes["AdornoVidrio"]
    x0, x1 = -ancho * 0.5, ancho * 0.5
    y0, y1 = -fondo * 0.5, fondo * 0.5
    tw = 3.55
    nx0, nx1 = x0 + tw, x1 - tw
    # Cuerpo de la nave, más bajo que la fachada.
    alto_nave = 8.6
    origen = (0, y0, 0)
    eje_u = (1, 0, 0)
    eje_v = (0, 0, 1)
    fuera = (0, -1, 0)
    dentro = (0, 1, 0)
    principal = {"x0": -1.45, "x1": 1.45, "z0": 0.0, "spring": 3.35, "radio": 1.45, "puerta": True, "hoja": 1}
    lateral_i = {"x0": nx0 + 0.85, "x1": nx0 + 2.55, "z0": 0.0, "spring": 2.45, "radio": 0.85}
    lateral_d = {"x0": nx1 - 2.55, "x1": nx1 - 0.85, "z0": 0.0, "spring": 2.45, "radio": 0.85}
    nichos = []
    for cx in (nx0 + 1.7, -3.55, 3.55, nx1 - 1.7):
        nichos.append({"x0": cx - 0.38, "x1": cx + 0.38, "z0": 5.15, "spring": 6.15, "radio": 0.38})
    vanos = [principal, lateral_i, lateral_d] + nichos
    muro_con_vanos(piedra, origen, eje_u, eje_v, nx0, nx1, 0, alto_fachada, vanos, fuera)
    for vano in vanos:
        tapa = None if vano.get("puerta") else vidrio
        vano_hundido(piedra, tapa, origen, eje_u, eje_v, dentro, vano, 0.55)
        archivolta(piedra, origen, eje_u, eje_v, fuera, vano, 0.12, 0.16)
    puerta_abierta(porton, origen, eje_u, eje_v, dentro, principal, signo=1)
    for vano in (lateral_i, lateral_d):
        hoja = dict(vano)
        puerta_madera(porton, origen, eje_u, eje_v, fuera, hoja, -0.08)
    for cx, signo in ((-1.7, -1), (1.7, 1)):
        columna(piedra, cx, y0 - 0.15, 0.15, 4.15, 0.24)
    cornisa(piedra, nx0, nx1, y0, 4.35, 0.18, 0.14)
    cornisa(piedra, nx0, nx1, y0, alto_fachada - 0.28, 0.22, 0.18)
    if con_reloj:
        disco(piedra, 0, y0, 8.15, 0.92)
        disco(partes["AdornoReja"], 0, y0 - 0.02, 8.15, 0.68)
    if con_fronton:
        fronton(piedra, -2.4, 2.4, y0, alto_fachada - 0.05, alto_fachada + 1.55)
        cruz(piedra, 0, y0, alto_fachada + 1.45, 0.85)
    else:
        cruz(piedra, 0, y0, alto_fachada + 0.05, 1.05)
    torre(partes, x0, nx0 + 0.06, y0 - 0.48, 1.35, alto_torre, coronar)
    torre(partes, nx1 - 0.06, x1, y0 - 0.48, 1.35, alto_torre, coronar)
    # Lados de la nave, con ventanas altas.
    for x_muro, normal in ((nx0, (-1, 0, 0)), (nx1, (1, 0, 0))):
        ventanas = []
        yv = y0 + 2.2
        while yv < y1 - 1.6:
            ventanas.append({"x0": yv, "x1": yv + 0.72, "z0": 4.8, "z1": 6.35, "radio": 0.0})
            yv += 2.5
        muro_con_vanos(piedra, (x_muro, 0, 0), (0, 1, 0), (0, 0, 1), y0, y1, 0, alto_nave, ventanas, normal)
        hacia_dentro = (-normal[0], 0, 0)
        for vano in ventanas:
            vano_hundido(piedra, vidrio, (x_muro, 0, 0), (0, 1, 0), (0, 0, 1), hacia_dentro, vano, 0.40)
    muro_con_vanos(piedra, (0, y1, 0), (1, 0, 0), (0, 0, 1), nx0, nx1, 0, alto_nave, [], (0, 1, 0))
    techo_nave(partes["Teja"], nx0 - 0.15, nx1 + 0.15, y0 + 0.35, y1 + 0.25, alto_nave - 0.05, 2.05)
    yb = y0 + 1.6
    while yb < y1 - 0.8:
        caja(piedra, nx0 - 0.42, nx0, yb, yb + 0.55, 0.2, alto_nave * 0.72)
        caja(piedra, nx1, nx1 + 0.42, yb, yb + 0.55, 0.2, alto_nave * 0.72)
        yb += 3.1
    nave(partes, nx0, nx1, y0, y1, alto_nave, 5.15)
    return ancho, fondo, max(alto_torre, alto_fachada)


def casa_catedral(partes):
    # Santa Catalina, lado de la plaza: fachada de piedra, tres puertas,
    # torres truncas y el reloj de la fachada.
    ancho, fondo, alto = templo(partes, 24.0, 13.2, 11.8, 14.6, "plana", True, False)
    return {"ancho": ancho, "fondo": fondo, "alto": alto, "puerta": 0.0}


def casa_san_francisco(partes):
    # San Francisco, al frente: la misma cantería, con las torres ya terminadas en teja.
    ancho, fondo, alto = templo(partes, 20.5, 12.4, 11.2, 16.2, "teja", False, True)
    return {"ancho": ancho, "fondo": fondo, "alto": alto, "puerta": 0.0}


def casa_corrido(partes):
    # Balcón de lado a lado, como las casonas corridas de la plaza, y tiendas abajo.
    ancho, fondo, alto = 11.80, 7.50, 7.20
    tiendas = []
    for x0 in (-5.35, -3.35, 1.85, 3.85):
        tiendas.append({"x0": x0, "x1": x0 + 1.35, "z0": 0.0, "z1": 2.45, "radio": 0.0, "puerta": True, "hoja": 1})
    puerta = {"x0": -0.85, "x1": 0.85, "z0": 0.0, "spring": 2.55, "radio": 0.85, "puerta": True, "hoja": 1}
    altas = []
    for x0 in (-4.7, -2.5, -0.45, 1.6, 3.7):
        altas.append({"x0": x0, "x1": x0 + 0.95, "z0": 4.45, "z1": 6.15, "radio": 0.0})
    vanos = tiendas + [puerta] + altas
    x0, x1, y0, y1 = cuerpo(partes, ancho, fondo, alto, vanos)
    zocalo(partes["Piedra"], x0, x1, y0, y1, 0.85, 0.08, [(v["x0"] - 0.04, v["x1"] + 0.04) for v in tiendas + [puerta]])
    cornisa(partes["Piedra"], x0, x1, y0, alto - 0.18)
    balcon(partes["AdornoMadera"], (0, y0, 0), (1, 0, 0), (0, 0, 1), (0, -1, 0), x0 + 0.25, x1 - 0.25, 4.02, 1.05)
    techo_dos_aguas(partes["Teja"], partes["Muro"], x0, x1, y0, y1, alto, 1.70, 0.85, 0.06, 0.40)
    canes_alero(partes["AdornoMadera"], x0 + 0.2, x1 - 0.2, y0, alto, 0.80)
    interior_recta(
        partes, ancho, fondo, alto, 3.90, vanos,
        y0 + 2.15, (-0.15, 1.15, y0 + 0.50, y0 + 4.05),
        (-4.2, y0 + 4.4), (3.4, y0 + 2.7),
    )
    return {"ancho": ancho, "fondo": fondo, "alto": alto, "puerta": 0.0}


def casa_portada(partes):
    # Portada de piedra al centro, como la casona municipal y la de los Gálvez.
    ancho, fondo, alto = 10.80, 7.50, 7.30
    puerta = {"x0": -1.00, "x1": 1.00, "z0": 0.0, "spring": 2.70, "radio": 1.00, "puerta": True, "hoja": 1}
    bajas = []
    for x0 in (-4.55, -2.85, 1.70, 3.40):
        bajas.append({"x0": x0, "x1": x0 + 1.05, "z0": 1.15, "spring": 2.15, "radio": 0.52, "reja": True})
    altas = [
        {"x0": -3.55, "x1": -2.35, "z0": 4.70, "z1": 6.25, "radio": 0.0},
        {"x0": -0.55, "x1": 0.55, "z0": 4.85, "z1": 6.30, "radio": 0.0},
        {"x0": 2.35, "x1": 3.55, "z0": 4.70, "z1": 6.25, "radio": 0.0},
    ]
    x0, x1, y0, y1 = cuerpo(partes, ancho, fondo, alto, [puerta] + bajas + altas)
    zocalo(partes["Piedra"], x0, x1, y0, y1, 1.05, 0.10, [(-1.15, 1.15)])
    pilastras(partes["Piedra"], x0 + 0.06, x1 - 0.06, y0, alto - 0.16)
    cornisa(partes["Piedra"], x0, x1, y0, 3.85, 0.16, 0.12)
    cornisa(partes["Piedra"], x0, x1, y0, alto - 0.18)
    fronton(partes["Piedra"], -1.55, 1.55, y0, 3.72, 4.08)
    balcon(partes["AdornoMadera"], (0, y0, 0), (1, 0, 0), (0, 0, 1), (0, -1, 0), -1.45, 1.45, 4.15, 0.95)
    techo_dos_aguas(partes["Teja"], partes["Muro"], x0, x1, y0, y1, alto, 1.68, 0.80, 0.06, 0.38)
    canes_alero(partes["AdornoMadera"], x0 + 0.2, x1 - 0.2, y0, alto, 0.76)
    interior_recta(
        partes, ancho, fondo, alto, 3.95, [puerta] + bajas + altas,
        y0 + 2.20, (-0.10, 1.20, y0 + 0.55, y0 + 4.10),
        (-3.6, y0 + 4.5), (2.6, y0 + 2.8),
    )
    return {"ancho": ancho, "fondo": fondo, "alto": alto, "puerta": 0.0}


def casa_ajimez(partes):
    # Balcón de cajón, cerrado de madera, distinto del balcón abierto.
    ancho, fondo, alto = 10.40, 7.40, 7.15
    puerta = {"x0": -3.55, "x1": -2.05, "z0": 0.0, "spring": 2.35, "radio": 0.75, "puerta": True, "hoja": 1}
    ventana = {"x0": -0.55, "x1": 0.70, "z0": 1.25, "spring": 2.30, "radio": 0.55, "reja": True}
    ventana2 = {"x0": 2.15, "x1": 3.40, "z0": 1.25, "spring": 2.30, "radio": 0.55, "reja": True}
    galeria = {"x0": -2.15, "x1": 2.15, "z0": 4.15, "z1": 6.25, "radio": 0.0, "abierto": True}
    x0, x1, y0, y1 = cuerpo(partes, ancho, fondo, alto, [puerta, ventana, ventana2, galeria])
    zocalo(partes["Piedra"], x0, x1, y0, y1, 1.05, 0.08, [(-3.70, -1.90)])
    cornisa(partes["Piedra"], x0, x1, y0, alto - 0.18)
    # Cajón: sale a la calle y tapa la galería con tres luces.
    yb = y0 - 0.95
    caja(partes["AdornoMadera"], -2.35, 2.35, yb, y0 + 0.08, 4.05, 4.20)
    caja(partes["AdornoMadera"], -2.35, 2.35, yb, y0 + 0.08, 6.20, 6.40)
    caja(partes["AdornoMadera"], -2.35, -2.18, yb, y0 + 0.05, 4.20, 6.20)
    caja(partes["AdornoMadera"], 2.18, 2.35, yb, y0 + 0.05, 4.20, 6.20)
    luces = [
        {"x0": -1.85, "x1": -0.85, "z0": 4.55, "z1": 5.85, "radio": 0.0},
        {"x0": -0.45, "x1": 0.45, "z0": 4.55, "z1": 5.85, "radio": 0.0},
        {"x0": 0.85, "x1": 1.85, "z0": 4.55, "z1": 5.85, "radio": 0.0},
    ]
    muro_con_vanos(partes["AdornoMadera"], (0, yb, 0), (1, 0, 0), (0, 0, 1), -2.18, 2.18, 4.20, 6.20, luces, (0, -1, 0))
    for vano in luces:
        vano_hundido(partes["AdornoMadera"], partes["AdornoVidrio"], (0, yb, 0), (1, 0, 0), (0, 0, 1), (0, 1, 0), vano, 0.08)
    techo_dos_aguas(partes["Teja"], partes["Muro"], x0, x1, y0, y1, alto, 1.65, 0.78, 0.06, 0.38)
    canes_alero(partes["AdornoMadera"], x0 + 0.2, x1 - 0.2, y0, alto, 0.74)
    interior_recta(
        partes, ancho, fondo, alto, 3.85, [puerta, ventana, ventana2, galeria],
        y0 + 2.10, (-3.15, -1.95, y0 + 0.50, y0 + 4.00),
        (2.4, y0 + 4.3), (-0.2, y0 + 2.6),
    )
    return {"ancho": ancho, "fondo": fondo, "alto": alto, "puerta": -2.80}


def cornisa_x(piedra, x, y0, y1, z, alto=0.22, sale=0.16):
    caja(piedra, x - 0.04, x + sale, y0 - 0.06, y1 + 0.06, z, z + alto)


def nuevo_vacio():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    escena = bpy.context.scene
    escena.unit_settings.system = "METRIC"
    escena.unit_settings.scale_length = 1.0


def material(nombre):
    mat = bpy.data.materials.new(nombre)
    mat.diffuse_color = COLORES[nombre]
    return mat


def publicar(parte, nombre, padre):
    if not parte.faces:
        return None
    malla = bpy.data.meshes.new(nombre)
    malla.from_pydata(parte.verts, [], parte.faces)
    malla.validate(verbose=False)
    malla.update()
    uv = malla.uv_layers.new(name="UVMap")
    for poly in malla.polygons:
        coords = parte.uvs[poly.index]
        for i, loop in enumerate(poly.loop_indices):
            uv.data[loop].uv = coords[i]
    colores = malla.color_attributes.new(name="Col", type="BYTE_COLOR", domain="CORNER")
    for item in colores.data:
        item.color = (1.0, 1.0, 1.0, 1.0)
    malla.color_attributes.active_color = colores
    obj = bpy.data.objects.new(nombre, malla)
    bpy.context.scene.collection.objects.link(obj)
    obj.parent = padre
    obj.data.materials.append(material(nombre))
    malla.update()
    return obj


def construir(nombre, fabrica):
    partes = {clave: Parte() for clave in COLORES}
    medidas = fabrica(partes)
    raiz = bpy.data.objects.new(nombre, None)
    bpy.context.scene.collection.objects.link(raiz)
    for clave, parte in partes.items():
        publicar(parte, clave, raiz)
    caras = ", ".join(f"{k}:{len(p.faces)}" for k, p in partes.items() if p.faces)
    print(f"MOLDE {nombre} ancho={medidas['ancho']:.2f} fondo={medidas['fondo']:.2f} alto={medidas['alto']:.2f} puerta={medidas['puerta']:.2f} {caras}")
    return raiz, medidas


def hijos(raiz):
    return [raiz] + list(raiz.children_recursive)


def exportar(raiz, ruta):
    for obj in bpy.context.view_layer.objects:
        obj.select_set(False)
    for obj in hijos(raiz):
        obj.select_set(True)
    bpy.context.view_layer.objects.active = raiz
    bpy.ops.export_scene.fbx(
        filepath=ruta,
        use_selection=True,
        apply_scale_options="FBX_SCALE_ALL",
        axis_forward="-Z",
        axis_up="Y",
        object_types={"EMPTY", "MESH"},
        mesh_smooth_type="FACE",
        use_mesh_modifiers=True,
        add_leaf_bones=False,
        bake_anim=False,
        path_mode="STRIP",
        embed_textures=False,
        colors_type="SRGB",
    )


def mirar(cam, destino):
    direccion = destino - cam.location
    cam.rotation_euler = direccion.to_track_quat("-Z", "Y").to_euler()


def render(ruta, objetivo, ojo, lente=36):
    escena = bpy.context.scene
    escena.render.engine = "BLENDER_WORKBENCH"
    escena.render.resolution_x = 1400
    escena.render.resolution_y = 900
    escena.render.film_transparent = False
    escena.display.shading.light = "STUDIO"
    escena.display.shading.color_type = "MATERIAL"
    escena.display.shading.show_shadows = True
    escena.display.render_aa = "8"
    if escena.world is None:
        escena.world = bpy.data.worlds.new("Cielo")
    escena.world.color = (0.62, 0.74, 0.86)
    cam_data = bpy.data.cameras.new("Cam")
    cam_data.lens = lente
    cam = bpy.data.objects.new("Cam", cam_data)
    escena.collection.objects.link(cam)
    escena.camera = cam
    cam.location = ojo
    from mathutils import Vector
    mirar(cam, Vector(objetivo))
    escena.render.filepath = ruta
    bpy.ops.render.render(write_still=True)
    bpy.data.objects.remove(cam, do_unlink=True)


def guia(altura=1.70):
    parte = Parte()
    caja(parte, -0.22, 0.22, -0.16, 0.16, 0, altura)
    malla = bpy.data.meshes.new("Persona")
    malla.from_pydata(parte.verts, [], parte.faces)
    obj = bpy.data.objects.new("Guia", malla)
    obj.location = (-1.6, -6.4, 0)
    mat = bpy.data.materials.new("Guia")
    mat.diffuse_color = (0.85, 0.45, 0.22, 1)
    obj.data.materials.append(mat)
    bpy.context.scene.collection.objects.link(obj)
    suelo = Parte()
    caja(suelo, -16, 16, -16, 10, -0.04, 0)
    malla_s = bpy.data.meshes.new("Suelo")
    malla_s.from_pydata(suelo.verts, [], suelo.faces)
    piso = bpy.data.objects.new("SueloGuia", malla_s)
    mat_s = bpy.data.materials.new("SueloGuia")
    mat_s.diffuse_color = (0.55, 0.58, 0.56, 1)
    piso.data.materials.append(mat_s)
    bpy.context.scene.collection.objects.link(piso)
    return obj, piso


def main():
    os.makedirs(OUT_FBX, exist_ok=True)
    os.makedirs(os.path.dirname(OUT_BLEND), exist_ok=True)
    os.makedirs(OUT_IMG, exist_ok=True)
    nuevo_vacio()
    fabricas = [
        ("CasonaBalcon", casa_balcon, (-6.2, -18.0, 4.4), (0.4, -2.4, 3.2), (0.55, -4.95, 1.48), (1.05, 0.35, 3.5)),
        ("CasonaDosBalcones", casa_dos_balcones, (-6.6, -18.4, 4.5), (0.6, -2.6, 3.3), (3.45, -5.05, 1.48), (3.05, 0.15, 3.3)),
        ("CasonaPortales", casa_portales, (-7.4, -19.5, 4.8), (0.2, -2.8, 3.4), (-1.50, -5.40, 1.55), (-1.55, -0.15, 3.6)),
        ("CasonaEsquina", casa_esquina, (11.5, -15.5, 5.0), (1.4, -2.2, 3.3), (2.40, -5.90, 1.52), (1.15, 0.35, 3.4)),
        ("CasonaCorrido", casa_corrido, (-8.2, -20.0, 4.6), (0.2, -2.6, 3.3), (0.15, -5.1, 1.5), (0.2, 0.4, 3.4)),
        ("CasonaPortada", casa_portada, (-7.6, -19.2, 4.6), (0.1, -2.5, 3.3), (0.1, -5.0, 1.5), (0.15, 0.3, 3.4)),
        ("CasonaAjimez", casa_ajimez, (-7.4, -18.6, 4.5), (-0.4, -2.4, 3.2), (-2.6, -4.9, 1.48), (-1.2, 0.2, 3.3)),
        ("Catedral", casa_catedral, (-18.0, -32.0, 9.0), (0.0, -3.0, 6.5), (0.2, -5.6, 1.6), (0.1, 1.5, 4.2)),
        ("SanFrancisco", casa_san_francisco, (-16.0, -30.0, 9.5), (0.0, -2.8, 7.0), (0.2, -5.3, 1.55), (0.1, 1.2, 4.0)),
    ]
    casas = []
    for nombre, fabrica, _ojo, _mira, _ojo2, _mira2 in fabricas:
        raiz, medidas = construir(nombre, fabrica)
        casas.append((nombre, raiz, medidas))
    persona, piso = guia()
    from mathutils import Vector
    for nombre, raiz, _medidas in casas:
        visibles = set(hijos(raiz))
        visibles.add(persona)
        visibles.add(piso)
        for obj in list(bpy.context.view_layer.objects):
            if obj is None:
                continue
            obj.hide_render = obj not in visibles
        ojo, mira, ojo2, mira2 = next((o, m, o2, m2) for n, _f, o, m, o2, m2 in fabricas if n == nombre)
        render(os.path.join(OUT_IMG, nombre + ".png"), mira, Vector(ojo))
        render(os.path.join(OUT_IMG, nombre + "-dentro.png"), mira2, Vector(ojo2), 28)
    for obj in list(bpy.context.view_layer.objects):
        if obj is None:
            continue
        obj.hide_render = False
    bpy.data.objects.remove(persona, do_unlink=True)
    bpy.data.objects.remove(piso, do_unlink=True)
    for nombre, raiz, _medidas in casas:
        exportar(raiz, os.path.join(OUT_FBX, nombre + ".fbx"))
    for i, (_nombre, raiz, _medidas) in enumerate(casas):
        raiz.location.x = i * 16.0
    bpy.ops.wm.save_as_mainfile(filepath=OUT_BLEND)
    print("LISTO", OUT_BLEND)


if __name__ == "__main__":
    main()
