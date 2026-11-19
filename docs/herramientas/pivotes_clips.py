"""Genera los pivotes por clip de AYNI.

Un solo pivote por personaje no alcanza: cada animacion se dibuja en un lienzo
distinto y con otro aire abajo. Ej.: la campesina apoya a 47 px en Quieto, 49 en
Caminar y 94 en Salto. Con un pivote unico, al saltar se hunde 47 px.

Este script mide el borde visible mas bajo de cada clip y emite solo las
excepciones: los clips que se apartan mas de 3 px del pivote base del personaje.
El resto sigue usando SpriteMetrics.byFolder. Los personajes sin medida en
byFolder usan pivote 0 (borde inferior del lienzo), asi que sus clips cuentan
como excepcion en cuanto traen aire abajo.

IMPORTANTE: los valores se guardan como "pies medidos desde el borde superior"
(1 - aire_abajo / alto), igual que Measure.PivotY. NO son el pivote de Unity:
SpriteMetrics.PivotFor invierte ese valor antes de crear el sprite.

Uso:
    python pivotes_clips.py
"""

import glob
import os
import re

ALPHA = 128
TOLERANCIA_PX = 3

RAIZ = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
PERSONAJES = os.path.join(RAIZ, "Assets", "Resources", "Battle", "Personajes")
CALLE = os.path.join(RAIZ, "Assets", "Resources", "Calle")
SPRITE_METRICS = os.path.join(RAIZ, "Assets", "Scripts", "Battle", "SpriteMetrics.cs")


def pivote_base():
    """Lee la tabla byFolder de SpriteMetrics.cs: nombre -> pivot."""
    texto = open(SPRITE_METRICS, encoding="utf-8").read()
    base = {}
    for nombre, pivote in re.findall(r'\{\s*"([^"]+)",\s*new Measure\(([0-9.]+)f', texto):
        base[nombre] = float(pivote)
    return base


def medir_clip(carpeta):
    """Devuelve (alto, gap minimo) del clip, o None si no hay PNG."""
    from PIL import Image
    import numpy as np

    bajo = None
    alto = 0
    for archivo in glob.glob(os.path.join(carpeta, "*.png")):
        im = Image.open(archivo).convert("RGBA")
        alfa = np.asarray(im.getchannel("A"))
        ys, _ = np.nonzero(alfa > ALPHA)
        if len(ys) == 0:
            continue
        h = im.size[1]
        gap = h - (int(ys.max()) + 1)
        alto = max(alto, h)
        bajo = gap if bajo is None else min(bajo, gap)
    if bajo is None:
        return None
    return alto, bajo


def clips():
    for raiz in (PERSONAJES, CALLE):
        if not os.path.isdir(raiz):
            continue
        for personaje in sorted(os.listdir(raiz)):
            carpeta = os.path.join(raiz, personaje)
            if not os.path.isdir(carpeta):
                continue
            for clip in sorted(os.listdir(carpeta)):
                sub = os.path.join(carpeta, clip)
                if os.path.isdir(sub):
                    yield personaje, clip, sub


def main():
    base = pivote_base()
    filas = []
    for personaje, clip, sub in clips():
        medida = medir_clip(sub)
        if medida is None:
            continue
        alto, gap = medida
        pivote = 1.0 - gap / alto
        # Aire abajo que ya supone el pivote base: byFolder lo mide, y sin
        # entrada el sprite se crea con el borde inferior (aire 0).
        if personaje in base:
            base_gap = round((1.0 - base[personaje]) * alto)
        else:
            base_gap = 0
        if abs(gap - base_gap) <= TOLERANCIA_PX:
            continue
        filas.append((personaje, clip, pivote, gap, alto))

    print("Pivotes por clip (excepciones a byFolder):")
    print("    static readonly Dictionary<string, float> byClip = new Dictionary<string, float>")
    print("    {")
    for personaje, clip, pivote, gap, alto in filas:
        print(f'        {{ "{personaje}/{clip}", {pivote:.4f}f }},  // gap {gap} / {alto} px')
    print("    };")
    print()
    print(f"{len(filas)} clips. Pivote base por personaje: {len(base)} entradas.")


if __name__ == "__main__":
    main()
