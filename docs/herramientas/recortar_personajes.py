#!/usr/bin/env python3
"""Limpia el recorte alfa de los PNG de personajes de AYNI.

Dos problemas frecuentes en las hojas de personajes:

* ``soften``: el PNG se recorto con umbral duro (alfa 0 o 255). Se ve
  escalonado en el juego. Suaviza el borde con un desenfoque pequeno y
  rellena el color de los pixeles nuevos con el opaco mas cercano.
* ``defringe``: el PNG conserva un halo blanco en los pixeles
  semitransparentes (porque se recorto sobre fondo blanco). Sustituye el
  color de esos pixeles por el opaco mas cercano sin tocar el alfa.

En ambos modos se hace "alpha bleed": los pixeles transparentes cercanos
al borde reciben el color del opaco mas cercano. Asi el filtrado de la
textura en Unity no arrastra negro ni blanco hacia la silueta.

Uso:
    python recortar_personajes.py --mode soften  --folder <carpeta>
    python recortar_personajes.py --mode defringe --folder <carpeta>
    python recortar_personajes.py --mode soften --folder <carpeta> --dry-run

Los originales se guardan en docs/backup-corte-personajes-<fecha>/.
"""

from __future__ import annotations

import argparse
import shutil
from datetime import date
from pathlib import Path

import numpy as np
from PIL import Image, ImageFilter
from scipy import ndimage

ROOT = Path(__file__).resolve().parents[2]
BACKUP_ROOT = ROOT / "docs" / f"backup-corte-personajes-{date.today():%Y%m%d}"


def stats(rgba: np.ndarray) -> str:
    alpha = rgba[..., 3]
    semi = int(((alpha > 0) & (alpha < 250)).sum())
    semi_white = int(
        ((alpha > 0) & (alpha < 250) & (rgba[..., 0] > 235) & (rgba[..., 1] > 235) & (rgba[..., 2] > 235)).sum()
    )
    opaque = int((alpha >= 250).sum())
    return f"opaque={opaque} semi={semi} semiWhite={semi_white}"


def nearest_opaque_colors(rgba: np.ndarray) -> np.ndarray:
    """Devuelve una copia RGB donde cada pixel toma el color opaco mas cercano."""
    alpha = rgba[..., 3]
    opaque = alpha >= 250
    if not opaque.any():
        return rgba[..., :3].copy()
    # distance_transform_edt con indices entrega, para cada pixel, la
    # coordenada del pixel de fondo (aqui los opacos) mas cercano.
    _, indices = ndimage.distance_transform_edt(~opaque, return_distances=True, return_indices=True)
    return rgba[..., :3][tuple(indices)]


def bleed(rgba: np.ndarray, radius: int = 4) -> np.ndarray:
    """Rellena el RGB de los pixeles transparentes cercanos al borde.

    El alfa no cambia: solo se evita que el filtro bilineal de Unity
    interpole hacia el color (negro/blanco) del fondo transparente.
    """
    alpha = rgba[..., 3]
    opaque = alpha >= 250
    if not opaque.any():
        return rgba
    _, indices = ndimage.distance_transform_edt(~opaque, return_distances=True, return_indices=True)
    distance = ndimage.distance_transform_edt(~opaque)
    fill = distance <= radius
    out = rgba.copy()
    nearest = rgba[..., :3][tuple(indices)]
    out[..., :3][fill] = nearest[fill]
    return out


def soften(rgba: np.ndarray, radius: float = 0.65) -> np.ndarray:
    """Convierte un alfa binario en un corte suave de ~1 px."""
    alpha = rgba[..., 3].astype(np.float32) / 255.0
    mask = Image.fromarray((alpha * 255.0).astype(np.uint8), mode="L")
    blurred = np.asarray(mask.filter(ImageFilter.GaussianBlur(radius=radius)), dtype=np.float32) / 255.0
    # No erosionar: el alfa original manda; el desenfoque solo agrega borde.
    new_alpha = np.maximum(alpha, blurred)
    out = rgba.copy()
    out[..., 3] = np.clip(np.rint(new_alpha * 255.0), 0, 255).astype(np.uint8)
    # Los pixeles que antes eran transparentes y ahora son semitransparentes
    # no tienen color propio: toman el del opaco mas cercano.
    new_semi = (alpha <= 0.0) & (new_alpha > 0.0)
    if new_semi.any():
        nearest = nearest_opaque_colors(rgba)
        out[..., :3][new_semi] = nearest[new_semi]
    return out


def defringe(rgba: np.ndarray) -> np.ndarray:
    """Quita el halo blanco de los pixeles semitransparentes.

    Solo se corrige el borde exterior (a menos de 4 px de un pixel opaco
    que no sea blanco). Asi no se destruyen blancos propios del dibujo
    (por ejemplo, el brillo del sombrero o de una manta).
    """
    alpha = rgba[..., 3]
    semi = (alpha > 0) & (alpha < 250)
    white = semi & (rgba[..., 0] > 225) & (rgba[..., 1] > 225) & (rgba[..., 2] > 225)
    if not white.any():
        return rgba

    opaque = alpha >= 250
    opaque_white = opaque & (rgba[..., 0] > 225) & (rgba[..., 1] > 225) & (rgba[..., 2] > 225)
    source = opaque & ~opaque_white
    if not source.any():
        return rgba

    distance, indices = ndimage.distance_transform_edt(~source, return_distances=True, return_indices=True)
    fix = white & (distance <= 4.0)
    if not fix.any():
        return rgba

    out = rgba.copy()
    nearest = rgba[..., :3][tuple(indices)]
    out[..., :3][fix] = nearest[fix]
    return out


def process(path: Path, mode: str, dry_run: bool) -> None:
    with Image.open(path) as image:
        image = image.convert("RGBA")
        rgba = np.asarray(image, dtype=np.uint8).copy()

        before = stats(rgba)
        if mode == "soften":
            rgba = soften(rgba)
        elif mode == "defringe":
            rgba = defringe(rgba)
        else:
            raise ValueError(mode)
        rgba = bleed(rgba)
        after = stats(rgba)

    label = path.relative_to(ROOT) if path.is_relative_to(ROOT) else path
    print(f"{label} | {before} -> {after}")
    if dry_run:
        return

    if path.is_relative_to(ROOT):
        backup = BACKUP_ROOT / path.relative_to(ROOT)
        backup.parent.mkdir(parents=True, exist_ok=True)
        if not backup.exists():
            shutil.copy2(path, backup)

    Image.fromarray(rgba, mode="RGBA").save(path, format="PNG", optimize=True)


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--mode", choices=("soften", "defringe"), required=True)
    parser.add_argument("--folder", required=True, help="Carpeta con los PNG a procesar.")
    parser.add_argument("--dry-run", action="store_true", help="Solo muestra las estadisticas.")
    args = parser.parse_args()

    folder = Path(args.folder)
    if not folder.is_absolute():
        folder = ROOT / folder
    files = sorted(folder.rglob("*.png"))
    if not files:
        raise SystemExit(f"No hay PNG en {folder}")
    for path in files:
        process(path, args.mode, args.dry_run)


if __name__ == "__main__":
    main()
