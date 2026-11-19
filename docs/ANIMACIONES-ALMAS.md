# Animaciones de las almas — 28/10/2026

Los tres últimos personajes del roster (`AlmaOro`, `AlmaRosa`, `AlmaAzul`) tenían
solo dos carpetas: `Quieto` y `Caminar`. **`Caminar` era una copia exacta de
`Quieto`**, así que no había caminata ni respuestas de combate.

Además, al revisar el arte se encontró el problema de fondo: los `Quieto`
originales eran tandas de 30/28/20 imágenes generadas por IA y **cada fotograma
era una pose distinta**, de otro tamaño y a veces hasta girada. Reproducirlos en
orden producía un parpadeo, no una animación. Cualquier clip derivado de esa
secuencia heredaba el mismo caos.

## Qué se hizo

### Una pose base por alma

Se eligió a mano un fotograma limpio de cada `Quieto` original (cuerpo entero,
de pie, centrado y con aire alrededor). Todos los clips se construyen moviendo
**ese mismo dibujo** con transformaciones suaves, así que el personaje se
mantiene reconocible y el movimiento se lee.

| Alma | Frame base | Lienzo |
| --- | ---: | --- |
| `AlmaOro` | `20` | 254 × 361 |
| `AlmaRosa` | `19` | 205 × 273 |
| `AlmaAzul` | `15` | 252 × 267 |

La pose se recorta, se centra y se apoya en el borde inferior, ocupando el 90 %
del alto del lienzo. Ese margen deja aire para estirar sin que se corte la
cabeza. El tamaño del lienzo no cambió, así que `CastScale` sigue calculando la
misma escala en el mundo.

### Clips de sprites

| Clip | Frames | Uso |
| --- | ---: | --- |
| `Quieto` | 8 | Respiración suave; reemplaza la tanda incoherente |
| `Caminar` | 12 | Flotar con dos impulsos por ciclo, distinto del idle |
| `Ataque` | 6 | Anticipación, estocada, brillo y estela |
| `Golpe` | 3 | Contragolpe con destello |
| `Caida` | 8 | El alma se apaga y se deshace hacia arriba (~0.57 s) |
| `Salto` | 3 | Compresión, estiramiento y flotación |
| `Nadar` | 4 | Planeo ondulante |
| `Feliz` | 4 | Rebote con chispas (tarjeta del roster) |
| `Especial` | 6 | Pulso espectral con estela |

`Quieto` y `Caminar` se reescribieron. Las carpetas originales completas están
en `docs/backup-almas-20261028/` (`<Alma>-Quieto`, `<Alma>-Caminar`).

### Generador reproducible

`docs/herramientas/generar_animaciones_almas.py` (Pillow + NumPy) vuelve a
generar todos los clips. Toma la pose base del respaldo original en
`docs/backup-almas-20261028/`, así que se puede ejecutar las veces que haga
falta aunque el `Quieto` actual ya sea el generado.

Conserva los `.meta` existentes y borra los sobrantes cuando un clip tiene menos
fotogramas que antes, para no dejar metadatos huérfanos ni romper referencias.

### Pulido en tiempo de ejecución

`SpriteFrameAnimator` distingue velocidades de idle, caminata, aire, nado y
acción. Si un personaje no tiene un clip de acción, la acción igual se siente:
se marca como acción durante un instante para que los controladores no la corten.

`SpriteMotion` (nuevo) agrega sin mover el objeto:

- respiración y balanceo en reposo;
- paso con rebote y giro;
- estiramiento al saltar y compresión al aterrizar;
- anticipación, estocada y destello al atacar;
- contragolpe y flash al recibir daño;
- desvanecimiento al morir;
- estelas en ataque y especial;
- aura flotante para las almas.

El aura y el movimiento etéreo se activan desde `BattleSprites.Apply` solo para
`AlmaOro`, `AlmaRosa` y `AlmaAzul`; el resto del elenco recibe el pulido básico.
El brillo de los frames es deliberadamente bajo para no duplicar el aura.

## Dónde tocar

- Ritmos: campos `idleRate`, `walkRate`, `airRate`, `swimRate`, `actionRate` en
  `SpriteFrameAnimator`.
- Intensidad del movimiento: `breathe` y `sway` en `SpriteMotion`.
- Amplitud de los frames: constantes dentro de `make_idle`, `make_walk`,
  `make_attack`, etc. en el generador.
- Colores de aura: `BattleSprites.Aura`.
- Frames: carpetas dentro de `Assets/Resources/Battle/Personajes/<Alma>/`.

## Revisión visual

En `docs/previas-almas-20261028/`:

- `<Alma>.png`: hoja de contacto, una fila por clip.
- `<Alma>-Quieto.png` y `<Alma>-Caminar.png`: tiras con todos los frames.

Los bucles de `Quieto` y `Caminar` se midieron: la diferencia entre el último y
el primer fotograma es del mismo orden que la diferencia entre frames
consecutivos, así que el ciclo no salta.

## Estado

- Unity reimportó los PNG y compiló `SpriteMotion.cs` sin errores.
- Falta la prueba en juego: elegir ORO/ROSA/AZUL en el roster y revisar idle,
  caminata, ataque, golpe, muerte, salto, nado, especial, aura y estelas.
