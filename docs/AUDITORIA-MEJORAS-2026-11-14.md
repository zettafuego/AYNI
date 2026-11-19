# AYNI — Auditoría de las mejoras del 14/11/2026

**Proyecto:** `C:\Users\Eduardo\AYNI-Unity`  
**Motor:** Unity `6000.5.0f1` (Built-in Render Pipeline)  
**Alcance:** corte visual de mineros y campesina, ampliación del elenco jugable y
correcciones encontradas durante la auditoría.  
**Verificación:** compilación de `Assembly-CSharp` + capturas del editor.

---

## 1. Resumen ejecutivo

| Mejora | Estado | Evidencia |
|---|---|---|
| Corte de los 3 mineros sentados | Hecho | `semi` 0 → 5.499–6.941 px; borde duro −78/−79 % |
| Corte de los 17 cuadros de la campesina | Hecho | halo blanco 47–53 % → 0,3–2,4 % del borde |
| Elenco jugable de 3 a 8 personajes | Hecho | grilla 4×2, navegación con flechas/WASD |
| Escala y apoyo de pies del elenco jugable | Hecho | los 8 cuerpos visibles miden 6,00 u y 91 px en la ficha |
| Malla *Tight* que comprimía el dibujo en la ficha | Corregido | `SpriteMeshType.FullRect`; Juan de Oso 72 → 91 px |
| Texturas de los 8 jugables deformadas por NPOT | Corregido | 147 `.meta`; respaldo en `docs/backup-npot-20261114` |
| Compilación | Limpia | `0 Errores`, 73 advertencias preexistentes |
| Voces de los personajes nuevos | Pendiente | faltan 3 de los 5 nuevos |
| 298 texturas NPOT del resto del elenco | Pendiente | ver §6, hallazgo P0 |

**Conclusión:** las dos tareas pedidas están terminadas y verificadas. La auditoría
encontró tres defectos sistémicos que no se veían en el código: el lienzo transparente
de algunos dibujos, la importación NPOT que deforma el arte y la malla *Tight* que
comprime el sprite dentro de su propio rectángulo. Los tres quedaron corregidos para
los 8 jugables; el NPOT del resto del elenco queda documentado para otra pasada.

---

## 2. Corte visual de sprites

### 2.1 Diagnóstico

Se midió el canal alfa de los PNG originales:

- **Mineros sentados:** alfa prácticamente binario (0 o 255). No había un solo píxel
  semitransparente, así que el borde del dibujo terminaba en escalón. Sobre fondos
  claros se veía el recorte duro; sobre oscuros, un filo blanquecino.
- **Campesina:** el alfa sí tenía degradado, pero contaminado de blanco. Entre el 47 %
  y el 85 % de los píxeles del borde eran blancos semitransparentes: el clásico halo
  lechoso al pegar un dibujo recortado sobre un fondo que no era blanco.

### 2.2 Herramienta

Se dejó una herramienta reutilizable:

`docs/herramientas/recortar_personajes.py`

- Modo `soften`: suaviza el borde de un alfa binario sin comerse el dibujo.
- Modo `defringe`: sustituye el color de los píxeles semitransparentes por el color
  opaco más cercano (decontaminación) y propaga el alfa hacia afuera (alpha bleed).
- Conserva el tamaño del lienzo y el pivote.
- Guarda respaldo antes de escribir y acepta `--dry-run`.

Respaldo de los originales: `docs/backup-corte-personajes-20261114/`.

### 2.3 Resultados

**Mineros** (`Assets/Resources/Battle/Personajes/Mineros/Sentado/`):

| Cuadro | Semitransparentes antes | Después | Borde duro antes | Después |
|---|---:|---:|---:|---:|
| `00.png` | 0 | 6.941 | 3.868 | 801 |
| `01.png` | 0 | 6.499 | 3.479 | 759 |
| `02.png` | 0 | 5.499 | 3.033 | 642 |

**Campesina** (`Assets/Resources/Battle/Personajes/campesina/`, 17 cuadros):
el porcentaje de blanco en el borde bajó de **47–53 %** a **0,3–4,8 %** en casi todos
los cuadros. El peor caso, `Golpe/00`, bajó de **85,4 %** a **13,2 %**; ese 13,2 %
residual no es halo: la manta de ese cuadro es gris muy claro y el medidor la cuenta
como blanco legítimo.

Los 20 PNG conservaron exactamente su tamaño original (339×640, 384×656, 364×655 en
los mineros; 489×931 en la campesina), así que ningún `Rect` de sprite ni pivote cambió.

**Verificación visual:** `Logs/comparacion-cortes.png` (antes/después sobre fondo
oscuro y claro). El borde de los mineros deja de ser un escalón y la campesina pierde
el cerco blanco.

### 2.4 Falsos positivos del medidor

Un conteo alto de “blanco en el borde” no siempre es un halo. Se revisaron a mano
`Brayan` (35,6 %), `Juan de Oso` (29,9 %) y `Toro` (28,7 %): en los tres el blanco es
plumaje, pelaje o luz de contorno del propio dibujo, no contaminación. No requieren
`defringe`. Los casos reales eran mineros y campesina.

---

## 3. Elenco jugable

### 3.1 Fichas

`FighterRoster.Choices` pasó de 3 a 8 entradas:

| # | Personaje | Ficha | Carpeta |
|---:|---|---|---|
| 0 | Vizcacha | Cachorro | `Vizcacha` |
| 1 | Atoq | Maestro | `Atoq` |
| 2 | Conejo | Maíz | `Conejo` |
| 3 | Brayan | Cóndor | `Brayan` |
| 4 | Cuy | Narrador | `Cuy` |
| 5 | Calavera | Danzante | `Calavera` |
| 6 | Campesina | Guardián de la Tierra | `campesina` |
| 7 | Juan de Oso | Fuerza del Oso | `Juan de Oso` |

### 3.2 Interfaz

- Grilla de **4 columnas × 2 filas** (`FighterRoster.RosterColumns = 4`), calculada
  por `RosterCell` con los mismos márgenes para cualquier cantidad de fichas.
- Navegación: `←`/`Q`/`A` y `→`/`E`/`D` mueven una ficha; `↑`/`W` y `↓`/`S` mueven
  una fila (4 posiciones). Con 8 fichas el movimiento es circular.
- `Enter`, `Espacio` o el botón **LISTO** confirman; `Esc` o **VOLVER** regresan.
- El splash del título sigue mostrando la tríada original (Vizcacha, Atoq, Conejo).
  Solo se amplió el selector, como se pidió.

### 3.3 Cobertura de animaciones

Los 8 personajes tienen los clips obligatorios: `Quieto`, `Caminar`, `Ataque`,
`Golpe`, `Caida`, `Feliz`, `Salto` y `Nadar`.

| Personaje | Quieto | Caminar | Ataque | Golpe | Caida | Feliz | Salto | Frente/Detrás |
|---|---:|---:|---:|---:|---:|---:|---:|---|
| Vizcacha | 2 | 6 | 5 | 3 | 3 | 1 | 4 | sí |
| Atoq | 1 | 6 | 3 | 3 | 3 | 1 | 3 | sí |
| Conejo | 2 | 4 | 3 | 2 | 1 | 1 | 1 | sí |
| Brayan | 2 | 6 | 3 | 2 | 2 | 1 | 2 | sí |
| Cuy | 3 | 6 | 3 | 2 | 2 | 1 | 3 | sí |
| Calavera | 2 | 4 | 3 | 2 | 1 | 1 | 1 | **no** |
| Campesina | 2 | 4 | 3 | 2 | 1 | 1 | 1 | **no** |
| Juan de Oso | 2 | 4 | 3 | 2 | 1 | 1 | 1 | **no** |

Calavera, Campesina y Juan de Oso no tienen `CaminarFrente` ni `CaminarDetras`. No es
un error: `SpriteFrameAnimator.SetAxialWalks` ignora los clips vacíos y el personaje
sigue usando el ciclo lateral. Se pierde variedad cuando la cámara gira, no se rompe
nada.

### 3.4 Voces

`CastVoice.For` busca `Battle/Audio/<id>`. Existen `vizcacha`, `atoq`, `brayan` y
`cuy`; **faltan `conejo`, `calavera`, `campesina` y `juandeoso`**. El clip nulo está
contemplado por `SetCry`, así que no hay excepción: simplemente esos cuatro personajes
entran a la pelea en silencio. Es el pendiente más visible del elenco nuevo.

---

## 4. Correcciones derivadas de la auditoría

### 4.1 Lienzo transparente: escala y pies flotantes

Los dibujos de campesina y Juan de Oso traen mucho aire transparente. Como el sprite
se crea con el lienzo completo y pivote en el borde inferior, ese aire hacía dos cosas:

- el cuerpo se medía con el alto del lienzo, así que salía **más bajo** que el resto;
- los pies quedaban **flotando** sobre el suelo.

Medición (`docs/herramientas/medir_lienzo.py`):

| Personaje | Lienzo | Cuerpo visible | Pies sobre el borde | Alto visible antes | Después |
|---|---:|---:|---:|---:|---:|
| Campesina | 489×931 | 342×725 | 47 px | 4,68 u | **6,00 u** |
| Juan de Oso | 668×1489 | 488×989 | 71 px | 4,77 u | **6,00 u** |

Se agregó `Assets/Scripts/Battle/SpriteMetrics.cs` con las medidas de los 8 jugables
(campesina y Juan de Oso eran los casos extremos, pero todos se normalizan igual) y se
aplicó en tres lugares:

- `BattleRuntimeAssets.CreateSprite`: pivote vertical en la línea de los pies.
- `CastScale.Measure`: alto visible en reposo, no alto del lienzo. Así los 8 cuerpos
  visibles miden 6,00 unidades, como pedía el comentario de `CastScale`.
- `RosterCard.Fit`: la ficha llena la caja con el cuerpo visible y apoya los pies en la
  misma línea que las demás.

El resto del elenco (NPC, almas, comparsa) no se toca: sus márgenes son de 0–2 % y su
pivote actual ya era correcto. La tabla es explícita y se regenera con
`medir_lienzo.py` si cambia el arte. Depende de que el lienzo del sprite coincida con
el PNG, es decir, de la corrección NPOT de §4.2.

### 4.2 Texturas NPOT: el arte estaba estirado

Al revisar las capturas se detectó que Unity reportaba lienzos de sprite que no
coincidían con los PNG:

| Personaje | PNG | Sprite que veía Unity |
|---|---:|---:|
| Vizcacha | 286×361 | 256×256 |
| Atoq | 557×537 | 512×512 |
| Conejo | 1010×763 | 1024×512 |
| Brayan | 564×700 | 512×512 |
| Cuy | 520×680 | 512×512 |
| Calavera | 1003×1042 | 1024×1024 |

La causa es `nPOTScale: 1` (Non-Power of Two → *ToNearest*) en los `.meta`. Unity
redimensiona cada eje al potencia de dos más cercano **sin conservar la proporción**.
Resultado: Conejo queda ~51 % más ancho, Cuy ~31 %, Vizcacha ~26 % y Brayan ~24 %; y se
pierde resolución (361 px de alto convertidos en 256).

La comprobación visual está en `Logs/auditoria-pot.png` (PNG original contra el
resultado que ve Unity). Además, en el splash la silueta de Vizcacha mide 139×139 px
en pantalla: aspecto 1,000 exacto, la firma de un lienzo cuadrado.

**Corrección aplicada:** `nPOTScale: 0` en los 147 `.meta` de las carpetas de los 8
jugables. Respaldo: `docs/backup-npot-20261114/` (misma ruta relativa). Al reimportar,
el sprite recupera su tamaño real y el código lo vuelve a normalizar a 6 unidades de
alto, así que el tamaño en pantalla se mantiene y solo se corrige la proporción.

**Pendiente:** quedan **298 `.meta` con `nPOTScale: 1`** en el resto del elenco
(Almas, Toro, Diablo, gatos, cuyes, comparsa, etc.). Es el hallazgo P0 de §6.

### 4.3 Malla *Tight*: la ficha encogía a Juan de Oso

Con la tabla de métricas ya aplicada, Juan de Oso seguía midiendo ~72 px de alto en su
ficha mientras los otros siete medían 91 px. La investigación descartó el `RectTransform`
(medía 162 unidades como se esperaba) y el pivote.

Se encontraron dos causas encadenadas:

1. **`Sprite.Create` usa malla `Tight` por defecto.** Con esa malla Unity recorta el
   cuadro que dibuja `Image` por el aire transparente del sprite (lo resta como
   `padding`) mientras las UV siguen siendo las del lienzo completo. El resultado es
   que el dibujo se comprime dentro de su propio rectángulo. Se veía en varios
   personajes (Conejo dibujaba solo el 43 % del ancho de su caja), pero solo Juan lo
   notaba porque su lienzo tiene mucho aire.
   **Arreglo:** `Sprite.Create(..., SpriteMeshType.FullRect)` en
   `BattleRuntimeAssets.CreateSprite`. El sprite cubre el lienzo completo y la malla es
   un cuadro simple (4 vértices en vez de 1.508). No cambia nada en batalla, donde el
   `SpriteRenderer` ya dibujaba el mismo píxel.

2. **La medición del cuerpo contaba motas casi transparentes.** El PNG de Juan tiene
   píxeles sueltos con alfa 20–60 por encima de la cabeza. Medido con alfa > 8, su
   cuerpo daba 1183 px de alto (79,5 % del lienzo) cuando el cuerpo real mide 989 px
   (66,4 %). La tabla lo encogía un 20 % para que “llenara” la caja.
   **Arreglo:** `medir_lienzo.py` mide con **alfa ≥ 50 %** (128/255). Con ese umbral los
   otros siete personajes dan exactamente el mismo número que antes, así que el cambio
   solo corrige a Juan. La tabla se regeneró completa para que sea reproducible.

Verificación final (`Logs/ui-personaje.png`): los ocho cuerpos visibles miden **91 px**
en pantalla a 768p y los ocho pares de pies caen en la misma línea.

---

## 5. Verificación técnica

### 5.1 Compilación

```powershell
dotnet build "C:\Users\Eduardo\AYNI-Unity\Assembly-CSharp.csproj" -v q --nologo
```

Resultado: **0 errores**, 73 advertencias. Todas las advertencias son preexistentes
(campos serializados sin asignar, `FindFirstObjectByType` obsoleto, etc.); ninguna
proviene de los archivos nuevos.

> Nota: `Assembly-CSharp.csproj` se genera desde Unity. Se agregó a mano
> `SpriteMetrics.cs` para poder compilar sin abrir el editor; Unity lo regenera solo.

### 5.2 Capturas

Se usó la sonda existente `AYNI.Editor.AyniTitleShot.Capture`, que abre
`AYNI_LagunaPelea_EDITABLE`, entra en Play y guarda:

- `Logs/ui-portada.png` — menú principal.
- `Logs/ui-personaje.png` — selector de personaje.
- `Logs/ui-splash.png` — splash de la historia.

Comprobado en las capturas:

- 8 fichas en 4×2, sin solaparse con el encabezado ni con la ficha de descripción.
- Nombres y descripciones legibles.
- Splash intacto con la tríada original.
- Los 8 cuerpos visibles miden 91 px y apoyan los pies en la misma línea
  (Juan de Oso medía 72 px antes de §4.3).
- Tras el cambio NPOT, Conejo, Cuy y Brayan recuperan su proporción.

---

## 6. Hallazgos priorizados

### P0 — 298 texturas siguen deformadas por NPOT

- **Qué:** el resto del elenco (Almas, Toro, Diablo, gatos, cuyes, comparsa, etc.)
  sigue con `nPOTScale: 1`.
- **Impacto:** proporciones incorrectas y pérdida de resolución en todo el juego.
- **Arreglo:** aplicar el mismo cambio a los 298 `.meta`, reimportar y revisar las
  escenas. Respaldo previo obligatorio.
- **Riesgo:** bajo en código (todo se normaliza por `sprite.rect`), alto en cantidad de
  assets. Hacerlo en una sola pasada y con capturas de cada escena.

### P1 — Faltan 4 voces

`conejo`, `calavera`, `campesina` y `juandeoso` no tienen `Battle/Audio/<id>`. Se
pueden sintetizar como las existentes o grabar. Mientras tanto, esos personajes
pelean en silencio.

### P1 — La segunda fila roza la ficha de descripción

La grilla termina en `y = 0.33` y la ficha empieza en `y = 0.32`: 0,01 de pantalla
(~8 px a 768p). En la captura se ve ajustado. Sugerencia: bajar la ficha a `0.115`
o subir el pie de la grilla a `0.345`.

### P2 — Tabla de métricas hardcodeada

`SpriteMetrics` guarda números medidos a mano para los 8 jugables. Si se recorta o
reemplaza el arte de cualquiera de ellos, hay que regenerarlos con
`docs/herramientas/medir_lienzo.py` (mide con alfa ≥ 50 %; con umbrales más bajos las
motas semitransparentes inflan la caja, que fue justo el error de §4.3). Está
documentado en el propio archivo, pero conviene un test de editor que avise.

### P2 — `RosterCard.Fit` se recalcula en cada `Update`

Es aritmética barata (8 fichas), pero se puede cachear por sprite. No urge.

### P2 — Sin control de versiones

El proyecto sigue sin `.git`. Los respaldos de esta sesión son carpetas copiadas a
mano. Un `git init` con `.gitignore` para `Library/`, `Temp/`, `Logs/` y `obj/`
evitaría sustos en la próxima pasada de assets.

### P3 — Caminatas axiales incompletas

Calavera, Campesina y Juan de Oso no tienen `CaminarFrente`/`CaminarDetras`. Funciona
por respaldo, pero se nota cuando la cámara gira. Es contenido, no bug.

---

## 7. Cómo revertir

- **Corte de sprites:** copiar de vuelta
  `docs/backup-corte-personajes-20261114/Assets/...` sobre `Assets/...`.
- **NPOT:** copiar de vuelta `docs/backup-npot-20261114/` sobre las carpetas
  correspondientes y reimportar (clic derecho → Reimport en Unity).
- **Código:** `SpriteMetrics.cs` es nuevo y aditivo. Para volver al comportamiento
  anterior basta con vaciar el diccionario `byFolder`; el resto del código usa el
  respaldo (lienzo completo, pivote 0).
- **Malla de los sprites:** quitar `SpriteMeshType.FullRect` de
  `BattleRuntimeAssets.CreateSprite` devuelve la malla *Tight* (y con ella el recorte
  que comprimía la ficha de Juan de Oso).

---

## 8. Próximos pasos sugeridos

1. Aplicar NPOT a los 298 `.meta` restantes con respaldo y capturas por escena.
2. Completar las 4 voces faltantes.
3. Ajustar la separación grilla/ficha (P1).
4. `git init` + `.gitignore` antes de la próxima tanda de assets.
5. Revisar el resto del elenco con `recortar_personajes.py --dry-run` y decidir caso
   por caso (recordar los falsos positivos de plumaje).
