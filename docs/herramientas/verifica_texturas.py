"""Comprueba tamaño y continuidad de borde de las texturas staged."""
from PIL import Image
import numpy as np
import os
import sys

OUT = os.path.join(os.path.dirname(__file__), "out")

expect = {
    "muro_adobe.png": (512, 512),
    "piedra.png": (512, 512),
    "teja.png": (512, 512),
    "piso_plaza.png": (768, 1024),
    "empedrado_rio.png": (512, 512),
    "cal_ocre.png": (512, 512),
    "piedra_inca.png": (512, 512),
    "madera_oscura.png": (512, 512),
}

bad = 0
for name, size in expect.items():
    a = np.asarray(Image.open(os.path.join(OUT, name)).convert("RGB")).astype(np.float32)
    h, w = a.shape[:2]
    ok = (w, h) == size
    # costura: el salto del borde debe parecerse a un salto normal del dibujo
    # (una junta también salta), no mayor que el percentil 99 interior.
    dx = np.abs(np.diff(a, axis=1)).mean(axis=(0, 2))
    dy = np.abs(np.diff(a, axis=0)).mean(axis=(1, 2))
    seam_x = np.abs(a[:, 0] - a[:, w - 1]).mean()
    seam_y = np.abs(a[0] - a[h - 1]).mean()
    ref_x = max(float(np.max(dx)) * 1.25, 6.0)
    ref_y = max(float(np.max(dy)) * 1.25, 6.0)
    ok = ok and seam_x <= ref_x and seam_y <= ref_y
    bad += 0 if ok else 1
    print("%-18s %sx%s  seamX %5.2f (p99 %5.2f)  seamY %5.2f (p99 %5.2f)  %s"
          % (name, w, h, seam_x, ref_x, seam_y, ref_y, "OK" if ok else "REVISAR"))

print("\n%d archivo(s) con problema" % bad)
sys.exit(1 if bad else 0)
