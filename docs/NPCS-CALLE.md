# NPCs de la calle — Cajamarca

El capítulo **CALLES** ahora tiene más gente: taxis con conductor por las
calles que rodean la plaza, puestos de mercado con vendedoras y señoras que
caminan por la vereda.

## Qué se agregó

| NPC | Dónde | Script |
| --- | --- | --- |
| 4 gallinas-taxi | Una por cada calle que rodea la plaza (sur, norte, este, oeste) | `GallinaConductora` |
| 22 puestos con vendedora | Vereda de piedra de la plaza: 14 al sur, 4 al norte, 2 al este y 2 al oeste | `GenteDeLaCalle` |
| 3 vendedoras caminando | Vereda sur, cerca del arranque del capítulo | `CajamarcaCalles.SpawnWalkers` |

Los puestos usan tres variantes de arte: `frutas`, `flores` y `comida`.
La vendedora es el sprite de `campesina`, con su animación de reposo y su
respiración procedural.

## Cómo se colocan

- **Puestos:** se ubican sobre la vereda de piedra, a `plazaHalf - PlazaWalk *
  0.72` del centro (≈ 45.9 m en la plaza de Cajamarca, que mide 104 × 104 m),
  mirando al jardín. Se valida con `OnStreet` y `OnPlaza` para no dejarlos en
  la calzada ni dentro de una casa.
- **Feria del sur:** dos hileras de 7 puestos a los lados de un pasillo central
  de 6.8 m, medidas en **metros** (no en fracciones de la plaza) para que la
  feria se vea igual de tupida aunque la plaza sea enorme. La separación mínima
  entre puestos es de 2.29 m y hay un zigzag de ±0.35 m para que la hilera no
  parezca una reja.
- **Vendedoras:** van como hijas del puesto, en `local (0, -0.25, 0.55)`, para
  que queden detrás del tablero desde cualquier ángulo. El puesto es un
  *billboard* (`Billboard`), así que la vendedora también mira a la cámara.
- **Taxis:** `StreetLane(side)` busca la franja de calzada que bordea la plaza
  por ese lado, toma su centro y devuelve el eje de la calle. Cada taxi tiene
  sentido, velocidad y fase de traqueteo distintos.
- **Altura:** todos usan `CajamarcaAltura.Sample(x, z)`, igual que el héroe,
  para no flotar ni hundirse en la topografía.

## Árboles de la plaza

Los árboles horneados de la plaza venían de cuatro modelos de `Bosque`
(`tree_oak`, `tree_default`, `tree_fat`, `tree_oak_dark`), y `tree_fat` es
prácticamente una bola verde sin detalle. Ahora se usan seis variantes del
mismo paquete:

```
tree_detailed · tree_oak · tree_tall · tree_default · tree_simple · tree_oak_dark
```

Se dejaron afuera `tree_fat` (la bola) y los pinos, que no pegan con la plaza.
El cambio se aplica **en tiempo de ejecución**: `CajamarcaCalles.ReplantTrees()`
recorre el grupo `Arboles` de la escena horneada, guarda posición, giro y altura
de cada árbol, y lo vuelve a plantar con la paleta nueva. Así no hace falta
rehornear la escena completa. El baker del editor
(`CajamarcaRutaApolonia.PlantPlazaTrees`) usa la misma paleta para las próximas
hornadas.

## Césped enterrado

El césped horneado es un disco plano a 0,16 m y el piso de la plaza es una malla
ondulada: donde el piso baja, el césped asomaba en manchas verdes sobre el
empedrado (se veía como hojas sueltas tiradas en el suelo).
`CajamarcaCalles.HideBuriedLawn()` oculta ese césped al abrir el capítulo, sin
tocar la escena guardada.

## Archivos

- `Assets/Scripts/Battle/GenteDeLaCalle.cs`: puestos, vendedoras y billboard.
- `Assets/Scripts/Battle/GallinaConductora.cs`: carril, sentido, fase, tinte y
  seguimiento del suelo.
- `Assets/Scripts/Battle/CajamarcaCalles.cs`: `DressTown`, `DressMarket`,
  `StreetLane`, `SpawnWalkers` y el paseo de las vendedoras.
- `Assets/Resources/Calle/Puestos/{frutas,flores,comida}.png`: arte de los
  puestos (512 × 512, PPU 100).
- `docs/herramientas/generar_puestos_calle.py`: generador reproducible del
  arte, con vista previa en `docs/previas-calle-20261028/puestos.png`.

## Perillas de ajuste

- `GenteDeLaCalle.StallMeters`: alto del puesto (2.1 m).
- `GenteDeLaCalle.VendorStreetScale`: alto de la vendedora relativo al héroe
  (0.35, el mismo factor de calle).
- `CajamarcaCalles.DressMarket`: `span` (largo de la feria del sur, 16.5 m),
  `perSide` (7 puestos por hilera), `near` (boca del pasillo, 3.4 m) y el
  factor `0.72` de `PlazaWalk` que decide qué tan adentro de la vereda quedan.
- `CajamarcaCalles.TaxiSpeed` / `TaxiStart`: velocidad y posición inicial.
- `GallinaConductora`: `Speed`, `Phase`, `Bob`, `BobRate`, `FollowGround`.

## Renders de control

`AYNI/Depurar/Renders de NPCs de la calle` abre el capítulo en Play, deja correr
la escena y guarda cinco imágenes en `docs/previas-calle-20261028/`:

| Archivo | Qué muestra |
| --- | --- |
| `01-inicio.png` | Vista de arranque: taxis en la calle y feria a la izquierda |
| `02-vereda.png` | Héroe en el borde de la vereda, taxi pasando |
| `03-puestos.png` | La feria del sur completa, con el pasillo central |
| `04-calles.png` | Taxi en la calzada que rodea la plaza |
| `05-puesto-cerca.png` | Detalle de un puesto con su vendedora |

Además escribe `Logs/calle-npcs-shot.txt` con la posición de cada puesto, si cae
en calle/plaza, la separación mínima y los cuatro carriles de taxi.

## Regenerar el arte

```powershell
python docs/herramientas/generar_puestos_calle.py
```

El script dibuja a 1024 px y reduce a 512 px con LANCZOS; agrega grano y una
luz suave desde arriba a la izquierda. La vista previa incluye una prueba de
proporción con la campesina (puesto de 2.1 m, vendedora de ~1.64 m).
