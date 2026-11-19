# AYNI — Escenas editables

**Fecha:** 26/10/2026  
**Escena principal:** `Assets/Scenes/AYNI_LagunaPelea_EDITABLE.unity`  
**Escena original (sin tocar):** `Assets/Scenes/AYNI_LagunaPelea.unity`  
**Distritos horneados:** Laguna, Cajamarca y bosque

---

## 1. Qué cambió

Antes, el nivel de la laguna se construía desde cero al pulsar **Play**. En la escena solo había cámara, UI, los dos luchadores y el manager. Por eso no se podía mover una plataforma, cambiar un arbusto ni recolocar un coleccionable sin editar C#.

Ahora existe una escena **horneada**: el mismo mundo, pero con **GameObjects reales** guardados en la escena.

- **84 objetos** de mundo dentro de `__MUNDO_EDITABLE__`.
- **15 plataformas** caminables (`Platform_0` … `Platform_14`, `Platform_Center`).
- **18 coleccionables** (`Collectible_0` … `Collectible_17`).
- **32 mallas** de suelo, montañas, piedra, ichu y río.
- Fondo y parallax ya colocados en la jerarquía.
- La escena está **primera en Build Settings**, así que el juego arranca con ella.
- La escena original `AYNI_LagunaPelea.unity` sigue existiendo como respaldo.

El runtime detecta el componente `AyniBakedWorld` y **ya no regenera el mundo**. Lo que ves en el editor es lo que se juega.

También se horneó **Cajamarca** dentro de la misma escena, en el root `__CAJAMARCA_EDITABLE__` (colocado a `x = 2200`, lejos de la laguna y del bosque):

- Plano completo de la ciudad (calles, plazas, muros, techos, puertas, agua).
- **Relieve real**: `Distrito/Terreno` es una malla de 321×321 vértices generada con el DEM real de Cajamarca (AWS Terrain Tiles, z15). La plaza queda a 0 m y el valle sube hasta +347 m en los cerros; las calles y plazas siguen la pendiente y las casas se apoyan en terrazas.
- **Fuente** de la plaza, **césped**, **senderos**, **jardines**, **36 árboles** y **caja de madera**.
- Componente marcador `AyniBakedCajamarca` para que el capítulo use esta copia.
- Los caminantes, la gallina, el HUD y los rótulos de calles siguen siendo dinámicos.

La ciudad se abre con el capítulo de Cajamarca desde la historia, o directamente con el test automático.

También se horneó el **bosque** en el root `__BOSQUE_EDITABLE__` (a `x = -360`, al oeste de la laguna):

- **502 mallas** reales: suelo, senderos, rampas, orilla, río, 121 matas de césped 3D, 151 árboles, arbustos, rocas, flores, troncos y 6 bayas de aura.
- Componente marcador `AyniBakedBosque` para que el capítulo use esta copia.
- Los caminantes, la serpiente, las zarzas, las salpicaduras y el HUD siguen siendo dinámicos.
- **El piso tiene textura de césped pintada** (`Assets/Resources/Bosque/piso_cesped.png`), más tierra en el sendero, barro en las rampas y piedra en las orillas.
- **Árboles y arbustos** usan paleta verde con troncos de madera. Los FBX del kit traían colores incrustados cian/rosa que se corrigen al hornear y al abrir el capítulo.

---

## 2. Cómo abrirla

En Unity:

1. Menú **AYNI > Escenas > Abrir escena editable — Laguna**.
2. O abre directamente `Assets/Scenes/AYNI_LagunaPelea_EDITABLE.unity` desde el Project.

Al pulsar **Play** debería arrancar la pantalla de inicio normal y, al comenzar la pelea, usar el mundo editado.

---

## 3. Jerarquía de la escena

```
AYNI_LagunaPelea_EDITABLE
├── __MUNDO_EDITABLE__            (AyniBakedWorld — NO borrar)
│   ├── SueloYMonte               (mallas: Suelo, Camino, Ichu, Piedra…)
│   ├── IchuDelante               (vegetación en primer plano)
│   ├── RioDeEntrada              (Agua, orillas)
│   └── BattleProps
│       ├── Platform_0 … Platform_14
│       ├── Platform_Center
│       └── Collectible_0 … Collectible_17
├── __CAJAMARCA_EDITABLE__        (AyniBakedCajamarca — NO borrar)
│   └── Distrito
│       ├── Terreno               (malla del valle con el DEM real)
│       ├── Plano                 (FBX de la ciudad, drapeado sobre el terreno)
│       │   ├── PlanoCalles
│       │   ├── PlanoMuros
│       │   ├── PlanoTechos
│       │   └── PlanoPlazas…
│       ├── Fuente
│       ├── Cesped
│       ├── Senderos
│       ├── Jardines
│       ├── CajaMadera
│       └── Arbol × 36
├── __BOSQUE_EDITABLE__           (AyniBakedBosque — NO borrar)
│   └── Arboleda
│       ├── Suelo                 (textura piso_cesped)
│       ├── Sendero / SenderoLejos
│       ├── RampaBaja / RampaMedia / Orilla
│       ├── Ribera / RiberaLejos
│       ├── Rio                   (textura Bosque/rio)
│       ├── Bosque/grass* × 121    (matas 3D de césped)
│       ├── Bosque/tree_* y Coniferas/Prefabs/trees × 151
│       ├── Bosque/rock_*, plant_*, flower_*, log_*
│       └── BayaDeAura × 6
├── _LagunaBattleManager          (LagunaBattleManager, BattlePropSpawner…)
│   └── ParallaxRoot              (Background_*, Middleground_*, Foreground_*)
├── Main Camera
├── BattleCanvas                  (HUD, puntajes, pantallas)
├── Vizcacha                      (jugador)
├── Toro                          (rival)
└── …
```

> El root de Cajamarca está a `x = 2200` para no mezclarse con la laguna ni con el bosque. Usa **AYNI > Escenas > Enfocar Cajamarca en la escena** para saltar directamente a él.

> El root del bosque está a `x = -360`. Usa **AYNI > Escenas > Enfocar bosque en la escena** para saltar directamente a él.

> El parallax se queda **dentro** de `_LagunaBattleManager` a propósito: `BattleParallaxSystem` busca sus capas con `GetComponentsInChildren`. Si lo sacas de ahí, el fondo deja de seguir a la cámara.

---

## 4. Cómo editar

### 4.1 Plataformas

Selecciona cualquier `Platform_*` dentro de `BattleProps`.

- **Mover:** herramienta Move (tecla `W`).
- **Tamaño:** herramienta Scale (tecla `R`). La **X** es el ancho y la **Y** es el alto.
- La colisión y la altura del suelo leen directamente la escala del Transform, así que lo que ves es lo que se pisa.
- El hijo `MonteDePiedra` (piedra + ichu) se escala junto con la plataforma.
- El gizmo verde muestra el área caminable.

Para duplicar una plataforma: `Ctrl+D`. El sistema la reconoce automáticamente al iniciar la pelea.

### 4.2 Coleccionables

Selecciona un `Collectible_*`.

- **Mover:** herramienta Move. La posición se respeta al jugar (el `Awake` sincroniza el punto de origen).
- **Tipo:** en el Inspector, campo **Type**:
  - `SpiritOrb` — orbe de ánima (azul).
  - `CocaLeaf` — hoja de coca (verde).
  - `GoldenFlower` — flor dorada (amarilla).
- Al cambiar el tipo, el color se actualiza solo en el editor.
- **Bob Speed / Bob Amount:** velocidad y amplitud del flotado.

### 4.3 Fondo y parallax

Dentro de `_LagunaBattleManager > ParallaxRoot` están las capas `Background`, `Middleground_A/B/C`, `Foreground`, etc. Cada una tiene un componente `ParallaxLayer` con:

- **Parallax Factor:** cuánto se desplaza respecto al jugador. Más bajo = más lejos.
- **Segment Width:** ancho de cada segmento repetido.

Puedes mover, escalar o desactivar capas completas. También puedes cambiar los `SpriteRenderer` hijos.

### 4.4 Suelo, río y vegetación

`SueloYMonte`, `IchuDelante` y `RioDeEntrada` son **mallas combinadas** guardadas como assets en `Assets/Generated/LagunaPelea/`.

- Se pueden mover, escalar, desactivar o cambiar de material.
- **No** son tiles individuales: para cambiar la silueta del terreno hay que volver a hornear o sustituirlas por tus propios sprites/prefabs.

### 4.5 UI, cámara y luchadores

Ya eran objetos normales de la escena. Puedes seguir editando `BattleCanvas`, `Main Camera`, `Vizcacha` y `Toro` como siempre.

### 4.6 Cajamarca

Selecciona **AYNI > Escenas > Enfocar Cajamarca en la escena** y edita `__CAJAMARCA_EDITABLE__ > Distrito`.

- **Ciudad:** `Plano` contiene las mallas de calles, plazas, muros, techos, puertas y agua. Puedes mover, rotar, escalar, desactivar o cambiar materiales de cada parte. (`PlanoSuelo` ya no existe: lo sustituye `Terreno`.)
- **Terreno:** `Distrito/Terreno` es la malla del valle, con dos sub-mallas: tierra del casco urbano (material heredado del antiguo `PlanoSuelo`) y pajonal de las laderas. Mide 3,2 × 3,2 km y tiene 103 041 vértices. Se puede mover, cambiar de material o desactivar, pero **las alturas del gameplay salen del heightfield**, no de esta malla.
- **Alturas:** `Assets/Resources/Calle/cajamarca_altura.bytes` guarda el campo de altura (cabecera `AYNH` + 321×321 `float`). El runtime lo lee con `CajamarcaAltura`; si lo borras, el héroe y los caminantes vuelven a y = 0.
- **Plaza:** `Fuente`, `Cesped`, `Senderos` y `Jardines` son objetos independientes, apoyados en la altura real de la plaza.
- **Árboles:** cada `Arbol` es una copia editable; puedes moverlo, escalarlo o borrarlo. Usan la paleta verde (hojas) y marrón (tronco) para corregir los colores cian/rosa del FBX.
- **Caja:** `CajaMadera` se puede mover o sustituir.
- **Materiales:** están en `Assets/Generated/Cajamarca/` y se pueden cambiar sin tocar código.
- Los **caminantes** no están en la escena: se crean al abrir el capítulo. Se pueden cambiar desde los scripts de personajes.

> Si mueves la ciudad entera, mueve `__CAJAMARCA_EDITABLE__` (no el `Distrito` por separado) para que las posiciones internas sigan cuadrando.

### 4.7 Bosque

Selecciona **AYNI > Escenas > Enfocar bosque en la escena** y edita `__BOSQUE_EDITABLE__ > Arboleda`.

- **Piso:** `Suelo` usa la textura `piso_cesped` repetida cada 6 metros. Para repintarlo, cambia `Assets/Resources/Bosque/piso_cesped.png` o genera otras con **AYNI > Escenas > Generar texturas del bosque**.
- **Sendero:** `Sendero` y `SenderoLejos` usan `piso_tierra`.
- **Río:** `RampaBaja`, `RampaMedia` y `Orilla` usan `piso_barro`; `Ribera` y `RiberaLejos`, `piso_piedra`. El agua (`Rio`) usa `Bosque/rio`.
- **Césped 3D:** las 121 matas `Bosque/grass*` (`grass`, `grass_large`, `grass_leafs`) son objetos independientes; puedes moverlos, escalarlos o borrarlos. El suelo ya no lleva planos verdes encima: se ve la textura pintada.
- **Árboles, arbustos, rocas y flores:** cada objeto conserva el nombre del recurso (`Bosque/tree_oak`, `Coniferas/Prefabs/trees/SM_pine`…). Mueve, escala, duplica o borra libremente.
- **Bayas:** `BayaDeAura` × 6 son objetos de juego: se pueden recoger y desaparecen. Si las borras, el capítulo tendrá menos bayas.
- **Troncos de colisión:** no están guardados como objetos. El runtime los reconstruye al abrir el capítulo a partir de los árboles y rocas horneados.
- Los **caminantes, la serpiente, las zarzas y las salpicaduras** se crean al abrir el capítulo.

> Si mueves el bosque entero, mueve `__BOSQUE_EDITABLE__`, no `Arboleda` por separado.

---

## 5. Qué sigue siendo dinámico

Estos sistemas todavía se crean por código al jugar, pero **no regeneran el mapa**:

- Luchadores, animaciones y combate.
- Lianas (`JungleCanopy`).
- Pantallas de historia, capítulos y textos.
- Compañeros y figuras del camino.
- Ciclo día/noche.
- En Cajamarca: caminantes, gallina, HUD y rótulos de calles. La ciudad en sí ya está horneada.
- En el bosque: caminantes, serpiente, zarzas, salpicaduras y HUD. La arboleda y el piso ya están horneados.

Es decir: los **mapas base** de la laguna, Cajamarca y el bosque ya son editables; la lógica y el contenido narrativo siguen en C#.

---

## 6. Reglas para no romper la escena

1. **No borres `__MUNDO_EDITABLE__` ni el componente `AyniBakedWorld`.**
   Si desaparecen, el manager vuelve a generar el nivel en runtime y aparecerán objetos duplicados o distintos a los que editaste.
2. **No borres `__CAJAMARCA_EDITABLE__` ni el componente `AyniBakedCajamarca`.**
   Si desaparecen, el capítulo vuelve a generar la ciudad desde código.
3. **No borres `__BOSQUE_EDITABLE__` ni el componente `AyniBakedBosque`.**
   Si desaparecen, el capítulo vuelve a generar la arboleda desde código.
4. **No saques `ParallaxRoot` de `_LagunaBattleManager`.**
5. **No edites a mano `Assets/Generated/LagunaPelea/`, `Assets/Generated/Cajamarca/` ni `Assets/Generated/Bosque/`** si vas a volver a hornear: el proceso borra y recrea esas carpetas.
6. Si mueves un coleccionable con el gizmo, no hace falta tocar `startPos`: se sincroniza solo.
7. Si escalas una plataforma, usa la escala del Transform, no solo el hijo `MonteDePiedra`.
8. No escales `BattleProps` ni `__MUNDO_EDITABLE__` completos: mueve o escala cada plataforma por separado. El cálculo de suelo usa la escala local de cada plataforma.
9. Mueve Cajamarca desde `__CAJAMARCA_EDITABLE__`, no desde `Distrito`, para conservar las coordenadas internas.
10. Mueve el bosque desde `__BOSQUE_EDITABLE__`, no desde `Arboleda`.

---

## 7. Volver a hornear

**Laguna:** **AYNI > Escenas > Generar escena editable — Laguna**.

**Cajamarca:** **AYNI > Escenas > Generar Cajamarca editable**.

El horneado de Cajamarca **descarga el DEM real** la primera vez (16 tiles de AWS Terrain Tiles, ~4,7 m/píxel) y lo reproyecta a las coordenadas del plano. La descarga queda cacheada en `%TEMP%\ayni_dem\` y el campo resultante en `Assets/Resources/Calle/cajamarca_altura.bytes`. Para forzar una descarga nueva: **AYNI > Escenas > Descargar alturas de Cajamarca**.

Qué hace el horneado con el relieve:

- **Suelo, calles, plazas y agua** se drapean vértice a vértice sobre el DEM.
- **Capas del suelo:** el terreno del casco se rebaja 45 cm (`CoreDrop`) respecto al DEM. En el plano original `PlanoSuelo` ya estaba 30 cm por debajo de calles y plazas; al poner el terreno nuevo a la cota exacta del DEM quedaba coplanar con ellas y las tapaba (z-fighting y cuerdas de la rejilla de 10 m en los tramos cóncavos). Las aceras y calzadas llevan una falda vertical de 90 cm (`SkirtDepth`) para que se vea el canto y no un hueco.
- **Casas** (muros, piedra y techos) se mantienen rígidas: cada edificio se agrupa con sus vecinos, se apoya en la altura mediana de su huella y la base del muro se hunde 70 cm (`Plinth`): 45 del rebaje + 25 de enterrado en la ladera.
- **Puertas y portadas** se apoyan en su propio tramo de fachada.
- **Objetos de la plaza** (fuente, caja, árboles, césped y senderos) se crean después del relieve y se ajustan a la altura del terreno.
- El **quad plano** `PlanoSuelo` se elimina y en su lugar queda `Distrito/Terreno`.

**Bosque:** **AYNI > Escenas > Generar bosque editable**.

**Texturas del piso:** **AYNI > Escenas > Generar texturas del bosque** (regenera los cuatro PNG de `Assets/Resources/Bosque/`). No hace falta volver a hornear para que el suelo use las texturas nuevas.

Después valida con **Validar escena editable — Laguna**, **Validar Cajamarca editable** y **Validar bosque editable**.

Cada horneado **solo reconstruye su propio distrito**:

- **Generar escena editable — Laguna** rehace `__MUNDO_EDITABLE__` y las capas de la laguna. Cajamarca y el bosque se conservan intactos.
- **Generar Cajamarca editable** rehace `__CAJAMARCA_EDITABLE__`. La laguna y el bosque se conservan intactos.
- **Generar bosque editable** rehace `__BOSQUE_EDITABLE__`. La laguna y Cajamarca se conservan intactos.
- Cada uno borra y recrea únicamente su carpeta: `Assets/Generated/LagunaPelea/`, `Assets/Generated/Cajamarca/` o `Assets/Generated/Bosque/`.

**Atención:** dentro del distrito que estás rehorneando **sí se pierden los cambios manuales** hechos en su root.

Para conservar esos cambios: duplica la escena antes de re-hornear ese distrito, o mueve tus objetos editados fuera del root correspondiente.

---

## 8. Verificación automática

Menú **AYNI > Escenas > Probar escena editable — Laguna**.

El test:

- abre la escena,
- entra en Play,
- arranca la batalla,
- comprueba que no se duplican plataformas ni coleccionables,
- comprueba que el mundo horneado sigue vivo,
- mueve un coleccionable antes de Play y verifica que la posición se respeta.

Resultado esperado:

```
[AYNI] PlayTest EDITABLE | plataformas=15->15 | coleccionables=18->18 | duplicado=False | mundo=True | moverColeccionable=True
```

**Cajamarca:** menú **AYNI > Escenas > Probar Cajamarca editable**. El test abre el capítulo, lo cierra y lo vuelve a abrir.

Resultado esperado:

```
[AYNI] PlayTest CAJAMARCA | abrir=True | distritoActivo=True | caminantes=4 | mallas=55 | sinDuplicar=True
```

**Bosque:** menú **AYNI > Escenas > Probar bosque editable**. El test abre el capítulo, comprueba la textura del suelo, lo cierra y lo vuelve a abrir.

Resultado esperado:

```
[AYNI] PlayTest BOSQUE | abrir=True | arboledaActiva=True | caminantes=3 | mallas=502 | texturaSuelo=True | sinDuplicar=True
```

---

## 9. Pendiente

- **Cajamarca:** afinar el borde del casco de tierra (constantes `CoreX` / `CoreZ` en `CajamarcaRelieve`) y revisar los bloques que OSM deja vacíos.
- Revisar la escena `AYNI_Prototipo` (ya es editable, pero es un prototipo 3D distinto).
- Migrar textos, stats y configuración a datos (ScriptableObjects) para no depender de C#.
- Añadir multijugador: primero intención de jugador y tick fijo, después netcode.

Ver también: [`AUDITORIA-2026-10-26.md`](AUDITORIA-2026-10-26.md).
