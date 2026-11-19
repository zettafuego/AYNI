# Texturas de CALLES desde las referencias de Cajamarca

Fecha: **2026-11-03**. Capítulo: **CALLES** (`Assets/Scripts/Battle/CajamarcaCalles.cs`).
Paquete de fotos: `docs/referencias/cajamarca/` (ver `docs/referencias/CREDITOS.md`).

## Qué se hizo

Las fotos no se pegan en el juego: se midió en ellas color, proporción de hiladas,
ancho de junta y tipo de desgaste, y con esos datos se generaron texturas
**tileables** (sin costura) del tamaño exacto que espera el juego. Así no entran
ventanas, autos, avisos ni pasto al material, y la repetición no se nota.

Generador: `docs/herramientas/texturas_cajamarca.py` (Pillow + numpy + scipy).
Salida intermedia: `docs/herramientas/out/`.
Hoja de control: `docs/img/calle-texturas-nuevas.png` (cada celda es el tile 3×3).
Verificación: `docs/herramientas/verifica_texturas.py`.

```powershell
python docs\herramientas\texturas_cajamarca.py   # regenera las 8 texturas
python docs\herramientas\verifica_texturas.py    # tamaño + continuidad de borde
python docs\herramientas\preview_texturas.py     # hoja de contactos 3×3
```

## Escala con la que se dibujaron

El shader `AYNI/Calle` multiplica textura × color de vértice y no usa `_MainTex_ST`,
así que el tamaño del tile lo fija el UV de cada malla:

| Superficie | UV del juego | Un tile cubre |
|---|---|---|
| Muros y techos (`UvOf`, `p::0.5`) | 0.5 por metro | **2 m × 2 m** |
| Pisos (`PlanFloorTile`) | 1 / 3.2 por metro | **3.2 m × 3.2 m** |

Por eso las hiladas de piedra son de 33 cm (6 hiladas por tile), las tejas de
25 cm de canal por 33 cm de exposición (8 × 6 por tile) y las losas de plaza
de 1.07 × 0.80 m (3 × 4 por tile).

Los muros llevan encima el tinte por vértice (`AdobeTint`: cal, salmón, crema,
rosa, ocre, gris), así que las texturas de muro se dejaron **claras y neutras**;
si fueran oscuras o muy saturadas, el tinte las ensucia.

## Texturas instaladas

### Reemplazos (mismo archivo, mismo nombre, mismo `.meta`, mismo GUID)

| Textura | Tamaño | Dónde se ve | Referencia medida |
|---|---|---|---|
| `muro_adobe.png` | 512² | muros laterales de casas y capilla (`Skin.Adobe`) | `fachadas/casona-2mayo-fachada.jpg` (cal), `fachadas/puerta-cajamarca-a.jpg` (revoco) |
| `piedra.png` | 512² | pileta, torres, cruz, iglesia (`Skin.Piedra`) | `materiales/detalle-catedral-derecha.jpg`, `muestras/canteria-belena.jpg` |
| `teja.png` | 512² | todos los techos (`Skin.Teja`) | `muestras/techos-teja.jpg`, `monumentos/plaza-armas.jpg` |
| `piso_plaza.png` | 768×1024 | Plaza de Armas, senderos y bandas (`Skin.Plaza`) | `monumentos/plaza-pileta.jpg`, `muestras/piedra-azul-labrada.jpg` |

### Biblioteca nueva (`Assets/Resources/Calle/`)

| Textura | Tamaño | Uso actual | Referencia medida |
|---|---|---|---|
| `madera_oscura.png` | 512² | **puertas de casas** (`CasaPuertas`, `LotesPuertas`) — sí está en escena | `fachadas/puerta-casa-2mayo.jpg`, `muestras/madera-puerta.jpg` |
| `empedrado_rio.png` | 512² | reservada (calle de canto rodado, capítulos siguientes) | `monumentos/belen-pileta.jpg`, `muestras/empedrado.jpg` |
| `cal_ocre.png` | 512² | reservada (muro ocre, variante de fachada) | `fachadas/casona-conde-uceda.jpg`, `muestras/cal-ocre.jpg` |
| `piedra_inca.png` | 512² | reservada (zócalos y muros incas) | `monumentos/cuarto-rescate.jpg`, `muestras/piedra-inca.jpg` |

Las tres reservadas ya están importadas con las mismas settings que el resto
(Repeat, mipmaps, sRGB, 2048) y se cargan con
`Resources.Load<Texture2D>("Calle/empedrado_rio")`, etc.

## Cambio de código (solo puertas)

`CajamarcaCalles.cs`:

- nueva constante `DoorWood = (0.82, 0.74, 0.68)`, el tinte que se aplica sobre
  la textura de madera (antes la puerta era color plano `Door`);
- `Publish(doors, "CasaPuerta" + n, "Calle/madera_oscura", shader)`;
- `Publish(doors, "LotesPuertas", "Calle/madera_oscura", shader)`;
- el quad de la puerta usa `DoorWood` en vez de `Door`.

No se tocó ninguna ruta `Resources.Load("Calle/...")`, ni nombres, ni GUIDs de
los assets existentes.

## Reversión

Los 4 PNG anteriores están en `docs/backup-texturas-calle-20261103/`.
Para volver atrás basta copiarlos de nuevo sobre `Assets/Resources/Calle/`
(los `.meta` no se modificaron) y revertir las 4 líneas de `CajamarcaCalles.cs`.

Para quitar la biblioteca nueva: borrar los 4 PNG y sus `.meta`
(`empedrado_rio`, `cal_ocre`, `piedra_inca`, `madera_oscura`) y volver a
`null` en las dos líneas `Publish(doors, ...)`.

## Licencia de estas texturas

Son **obras derivadas** de las fotos: heredan **CC BY-SA 4.0** (una fuente es
CC BY-SA 3.0 y otra de dominio público). Al distribuir el juego hay que:

1. acreditar a lxs autores de las fotos de origen;
2. publicar estas texturas derivadas bajo CC BY-SA;
3. no añadir restricciones extra sobre ellas.

La atribución concreta por textura está en `docs/referencias/CREDITOS.md`
(sección «Texturas derivadas del juego»).
