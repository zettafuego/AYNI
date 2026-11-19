"""Mide el lienzo y el cuerpo visible de un personaje.

Uso:
    python medir_lienzo.py "C:\\ruta\\Personajes\\campesina"

Imprime el tamano del lienzo, el cuerpo visible en reposo, el margen de los
pies y los valores que espera AYNI.Battle.SpriteMetrics (PivotY, StandWidth,
StandHeight, CanvasWidth, CanvasHeight). Sirve para regenerar la tabla cuando
cambie el arte.
"""

import os
import sys

import numpy as np
from PIL import Image

# Un pixel cuenta como cuerpo cuando esta al menos medio opaco. Los PNG del
# elenco traen motas sueltas casi transparentes (alfa 20-60) que estiraban la
# caja visible: Juan de Oso medía 1183 px de alto cuando su cuerpo real mide
# 989. Ese aire de mas hacia que la ficha lo encogiera un 20 %.
ALPHA = 128


def bbox(path):
    im = Image.open(path).convert("RGBA")
    alpha = np.asarray(im.getchannel("A"))
    ys, xs = np.nonzero(alpha > ALPHA)
    if len(ys) == 0:
        return im.size, (0, 0, 0, 0)
    return im.size, (int(xs.min()), int(ys.min()), int(xs.max()) + 1, int(ys.max()) + 1)


def main(folder):
    name = os.path.basename(os.path.normpath(folder))
    quieto = os.path.join(folder, "Quieto")
    caminar = os.path.join(folder, "Caminar")
    if not os.path.isdir(quieto):
        raise SystemExit("No existe " + quieto)

    frames = [os.path.join(quieto, f) for f in sorted(os.listdir(quieto)) if f.endswith(".png")]
    (w, h), bb = bbox(frames[0])
    stand_w = bb[2] - bb[0]
    stand_h = bb[3] - bb[1]

    # Los pies se apoyan en el cuadro de caminar mas bajo, no en el fondo del lienzo.
    lows = []
    for clip in (quieto, caminar):
        if not os.path.isdir(clip):
            continue
        for f in sorted(os.listdir(clip)):
            if not f.endswith(".png"):
                continue
            _, box = bbox(os.path.join(clip, f))
            lows.append(h - box[3])
    foot = min(lows) if lows else h - bb[3]

    print(f"personaje : {name}")
    print(f"lienzo    : {w}x{h}")
    print(f"cuerpo    : {stand_w}x{stand_h}  (alto visible {100 * stand_h / h:.1f}% del lienzo)")
    print(f"pies      : {foot} px sobre el borde inferior")
    print()
    print("SpriteMetrics.Measure:")
    print(f'    new Measure({1 - foot / h:.4f}f, {stand_w}f, {stand_h}f, {w}f, {h}f),')
    print()
    print("El primer valor son los pies medidos desde el borde SUPERIOR.")
    print("SpriteMetrics.PivotFor lo invierte al crear el sprite; no lo uses tal cual.")


if __name__ == "__main__":
    if len(sys.argv) != 2:
        raise SystemExit(__doc__)
    main(sys.argv[1])
