# QA de fallos visibles — protocolo y estado

Este doc es la fuente de verdad del revisor (`qa`). Cada sistema con fallos
recurrentes suma una fila a la tabla de trampas y un caso a la lista.

## Herramientas

| Qué | Dónde | Qué prueba |
| --- | --- | --- |
| Sonda de pies | `AYNI/Depurar/Revisar pies` (Play) | Ancla de cada caminante contra el suelo visible y pivote del sprite |
| Salida | `Logs/pies.txt` | `delta = ancla - suelo`; `pivote` 0 = pies, 1 = cabeza |
| Renders de control | `docs/previas-*/` | Confirmación visual, nunca única prueba |
| Renders de NPCs | `AYNI/Depurar/Renders de NPCs de la calle` | Puestos, taxis y feria |
| Pivotes por clip | `python docs/herramientas/pivotes_clips.py` | Regenera `SpriteMetrics.byClip` desde los PNG |

La sonda usa `CajamarcaCalles.SueloVisible(world)` → `SueloDeLaCalle.Altura`, que es
`CajamarcaAltura.Sample + CapaPlaza`. La capa de plaza ya incluye el peldaño de
vereda (0.16), así que sumar otro lift duplica el escalón. Ese es el contrato.

## El bug de los pivotes (el que hundía a los jugables)

Síntoma: "los jugadores están por debajo".

`SpriteMetrics` mide los pies **desde el borde superior** del lienzo
(`FeetFromTop`), porque así lo necesita la ficha del roster:
`drop = (1 - FeetFromTop) * alto`. `Sprite.Create` espera el pivote **desde el
borde inferior**. Al pasar el valor sin invertir, el ancla quedaba en la cabeza
y el cuerpo entero colgaba bajo el suelo.

Ejemplo medido (campesina, Quieto): lienzo 931 px, pies a 47 px del borde inferior.

| Dato | Valor |
| --- | ---: |
| `FeetFromTop` en la tabla | 0.9495 |
| Pivote de Unity correcto | 0.0505 |
| Hundimiento con 0.9495 | ≈ 5.4 u (el cuerpo mide 4.7 u) |

Regla: `SpriteMetrics.PivotFor` es el único que convierte, y siempre devuelve el
convenio de Unity (`1 - FeetFromTop`). Ninguna tabla se pasa cruda a
`Sprite.Create`, y `Measure` ya no se llama `PivotY` para que nadie repita el error.

Además el lienzo cambia por clip, así que un pivote único por personaje desalinea
los pies entre animaciones:

| Personaje | Quieto | Caminar | Salto |
| --- | ---: | ---: | ---: |
| campesina | 47 px | 49 px | **94 px** |
| Vizcacha | 6 px | **13 px** | 6 px |
| Toro | 54 px | **41 px** | 6 px |

`byClip` guarda las excepciones (> 3 px) con clave `"Personaje/Clip"` y tiene
prioridad sobre `byFolder`. Con eso los pies no bailan al cambiar de animación.

## El bug del suelo

Síntoma: "la vendedora se hunde hasta la rodilla", "el héroe flota en la vereda".

Antes convivían tres lifts distintos sobre el mismo relieve:

| Sistema | Fórmula vieja | Lift |
| --- | --- | ---: |
| `Ground(walker)` | `Sample + CapaPlaza + lift` | 0.16 |
| `Pisar()` | `Sample` | 0.20 |
| `GenteDeLaCalle.Dress` | `Sample` | 0.16 |
| Músicos sobre la tarima | `Sample` | 0.55 |

Hoy todo pasa por `SueloDeLaCalle.Altura` (relieve + capa de plaza, sin lift
extra). Lo que sí se apoya sobre un objeto —la tarima del concierto— usa
`Walker.PisoExtra`, no un lift de suelo.

### Cómo se lee la sonda

- `pivote > 0.5` → el dibujo está anclado en la cabeza: enterrado. Siempre es bug.
- `delta < -0.03` → el ancla quedó bajo el suelo. Siempre es bug.
- `delta` entre `-0.03` y `0.06` → sano.
- `delta > 0.06` → flotando **o saltando**. Confirmar con el estado del actor.

### Causas típicas, en orden de frecuencia

1. Pivote de tabla pasado a `Sprite.Create` sin invertir (cabeza por pies).
2. Clip nuevo o arte regenerado sin volver a correr `pivotes_clips.py`.
3. Un actor nuevo apoya con `+0.2` (convención vieja de `Pisar`) o con `0`.
4. `Present` distinto entre el horneado y el runtime → el error crece con la
   pendiente y es cero en la plaza llana.
5. Personaje escalado desde un pivote que no está en los pies.

## Regla de arreglo

Un solo contrato de suelo y un solo conversor de pivote:

- suelo: `SueloDeLaCalle.Altura()`; si algo necesita otro apoyo, va en
  `Walker.PisoExtra` o como parámetro explícito documentado;
- pies: `SpriteMetrics.PivotFor()`; ninguna tabla de medidas se usa directo.

Prohibido "subir todo 5 cm".

## Estado

| Fecha | Hallazgo | Medida | Estado |
| --- | --- | --- | --- |
| 2026-11-17 | Pivote medido pasado sin invertir a `Sprite.Create` | `PivotFor` invierte; sonda reporta `pivote` | Corregido |
| 2026-11-17 | El lienzo cambia por clip y los pies bailan | `byClip` (86 excepciones medidas) | Corregido |
| 2026-11-17 | Tres lifts distintos para el mismo suelo | `SueloDeLaCalle` sin lift extra | Corregido |
| 2026-11-17 | Toro con `footDrop` encima del pivote | `footDrop` eliminado | Corregido |
| 2026-11-17 | Falta confirmar en Play | `Logs/pies.txt` con CALLES abierto | Abierto |

## Alta de un hallazgo

```
Síntoma:
Medida (log/render):
Causa:
Dueño: unity | arte | narrativa
Archivo:línea:
```
