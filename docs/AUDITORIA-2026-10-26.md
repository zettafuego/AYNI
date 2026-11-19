# AYNI — Auditoría técnica: escalado, multijugador y authoring

**Fecha:** 26/10/2026  
**Proyecto:** `C:\Users\Eduardo\AYNI-Unity`  
**Motor:** Unity `6000.5.0f1`  
**Build revisado:** `C:\Users\Eduardo\Desktop\Juego` (Windows x64)

> **Actualización (misma fecha):** ya existe una escena editable horneada,
> `Assets/Scenes/AYNI_LagunaPelea_EDITABLE.unity`, y es la primera escena de build.
> El mundo de la laguna, el distrito de Cajamarca y la arboleda del bosque ya no se
> generan al pulsar Play: están guardados como GameObjects reales dentro de los roots
> `__MUNDO_EDITABLE__`, `__CAJAMARCA_EDITABLE__` y `__BOSQUE_EDITABLE__`. Ver
> [`ESCENAS-EDITABLES.md`](ESCENAS-EDITABLES.md).
>
> **Cajamarca tiene relieve real.** El distrito se drapea sobre un DEM de AWS
> Terrain Tiles (z15, ~4,7 m/píxel): la plaza queda a 0 m y el valle sube hasta
> +347 m. Las calles y plazas siguen la pendiente; las casas se agrupan en
> terrazas rígidas. El campo de altura vive en
> `Assets/Resources/Calle/cajamarca_altura.bytes` y el runtime lo lee con
> `CajamarcaAltura`.

---

## 1. Resumen ejecutivo

AYNI hoy es **un prototipo funcional con muy buena cantidad de contenido**, pero está construido con un patrón que no escala: **casi todo el juego se genera por código en tiempo de ejecución**. Las escenas están prácticamente vacías (38–42 GameObjects) y el nivel, los personajes, el HUD, los textos y las reglas aparecen al pulsar Play.

Eso explica exactamente el síntoma que describes: *“hay cosas que no puedo modificar si no es por código”*. No es un problema de Unity ni de permisos: **no hay nada que editar en la escena**, porque el contenido se crea desde scripts.

Consecuencias principales:

| Área | Estado | Riesgo |
|---|---|---|
| Editar mapas | Generados por código, con `Random` sin semilla | **Crítico** |
| Multijugador | Sin capa de red; estado global y un solo jugador cableado | **Crítico** |
| Escalar contenido | Textos, stats y niveles viven en C# | Alto |
| Escalar equipo | Monolitos de 2.357 líneas, sin asmdefs, sin git | Alto |
| Rendimiento | `Resources`, sprites sin atlas, mallas y texturas en runtime | Alto |
| Presentación | Build de desarrollo, UI con Arial legacy, shaders planos | Medio |

**Conclusión:** se puede llevar a un producto comercial y multijugador, pero **no conviene “parchear” el código actual**. Hay que separar tres cosas que hoy están mezcladas: **contenido (datos)**, **simulación (reglas)** y **presentación (escena/UI)**. La buena noticia es que el contenido ya existe y se puede migrar por fases sin tirar el trabajo.

---

## 2. Cómo está construido hoy

### 2.1 Ficha técnica

- **Escenas de build:** solo `AYNI_LagunaPelea` está habilitada. `AYNI_Prototipo` está deshabilitada.
- **Escena de pelea:** 38 GameObjects, 46 MonoBehaviour. Solo contiene cámara, canvas, dos fighters y el manager. El nivel no está en la escena.
- **Escena prototipo:** 42 GameObjects, hecha con primitivas (cápsula, cubos, plano). Es otro juego distinto al de la pelea.
- **Código:** 53 scripts de runtime + 3 de editor, ~11.194 líneas, 72 tipos. **No hay ningún `.asmdef` propio**: todo compila en `Assembly-CSharp`.
- **Assets:** 436 MB. 268 PNG de personajes (frames de ~650×529), 558 assets dentro de `Resources`.
- **Build:** 257 MB; `resources.assets.resS` = 111 MB.
- **Render:** Built-in Render Pipeline (`m_CustomRenderPipeline: {fileID: 0}`), shaders propios con iluminación falsa por vértice.
- **Input:** `activeInputHandler: 2` (ambos), pero el gameplay usa `Input.GetAxisRaw`/`Input.GetKeyDown` legacy. El paquete Input System está instalado pero sin usar.
- **Netcode:** no hay Netcode for GameObjects, Mirror, Fusion, Photon ni transporte. `com.unity.multiplayer.center` es solo el asistente de configuración.
- **Persistencia:** no hay `PlayerPrefs`, ni save system, ni perfiles, ni ajustes.
- **Control de versiones:** **no hay `.git`** en el proyecto.
- **Build de desarrollo:** contiene `AYNI_BurstDebugInformation_DoNotShip`, señal de que se compiló como Development Build.

### 2.2 Dónde vive cada cosa

```
Assets/
  Editor/
    AyniSceneBootstrap.cs       Crea la escena 3D desde cero (primitivas)
    LagunaBattleBootstrap.cs    Crea la escena de pelea desde cero
    AyniCompile.cs              Check de compilación
    AyniCajamarcaDem.cs         DEM real (AWS Terrain Tiles) → campo de altura
    CajamarcaRelieve.cs         Drapea calles/plazas, terrazas de casas y malla Terreno
    AyniCajamarcaSceneBaker.cs  Hornea el distrito de Cajamarca editable
  Scripts/
    Core/                       GameManager, AyniBalanceSystem (singletons)
    Player/                     PlayerController 3D, transformaciones
    Battle/                     El juego real: 2D fighter + ciudad + bosque
      CajamarcaCalles.cs        2.357 líneas — genera la ciudad entera
      CajamarcaAltura.cs        Lee el heightfield del DEM (altura en runtime)
      BosqueCapitulo.cs         1.086 líneas — genera el bosque
      LagunaBattleManager.cs    Orquesta pelea, UI, score, noche, historia
      VizcachaFighter.cs        Movimiento, daño, input directo
      ToroEnemyAI.cs            IA del enemigo
      MonteDressing.cs          Genera vegetación/rocas por mallas
      BattlePropSpawner.cs      Plataformas y coleccionables aleatorios
      RutaDelDia.cs             Noche/día, batán, cartilla
      StorybookScreens.cs       Pantallas de historia
    Narrative/                  Textos y decisiones morales
  Resources/                    Todo el contenido cargado en runtime
  Shaders/                      4 shaders propios
```

**Dato clave:** `AyniSceneBootstrap` y `LagunaBattleBootstrap` son herramientas de editor que **construyen la escena con código**. Si abres la escena y pulsas Play, el nivel se vuelve a generar. Si mueves algo en el editor, el runtime lo sobrescribe o lo ignora.

### 2.3 El caso de los mapas

- `Assets/Resources/Calle/calles.json` **solo guarda nombres y posiciones de calles para las etiquetas del HUD**. No define el nivel.
- La ciudad se construye con constantes en C#: `PlazaR = 14f`, `Lot = 15f`, `StreetOuter = 19f`, paleta de colores en campos `static readonly`, etc.
- `BattlePropSpawner` usa `Random.Range` **sin semilla**: las plataformas y coleccionables cambian en cada partida. Dos clientes de una partida multijugador verían niveles distintos.
- `MonteDressing` sí usa `new System.Random(710)` (buen patrón), pero `CajamarcaCalles` usa `UnityEngine.Random` sin semilla en varios sitios.
- `BattleRuntimeAssets.LoadClip` hace `Resources.LoadAll<Texture2D>` **cada vez que se llama** y crea `Sprite` nuevos sin caché.

Esto significa que hoy el “mapa” **no es un asset**: es el resultado de ejecutar código.

---

## 3. Problemas para escalar

### 3.1 Arquitectura y equipo

1. **Monolitos.** `CajamarcaCalles.cs` tiene 2.357 líneas y mezcla generación de malla, texturas procedurales, HUD, cámara, IA de peatones, colisiones y layout urbano. Cualquier cambio de mapa obliga a tocar ese archivo.
2. **Sin assemblies.** Todo en `Assembly-CSharp`. Cada cambio recompila todo; no se pueden aislar tests ni módulos.
3. **Sin git.** No hay historial, ramas, ni posibilidad de trabajar con otra persona sin pisarse. **Esto es lo primero que hay que resolver**, antes que cualquier refactor.
4. **Sin tests.** Ni unitarios ni de play mode. Los cambios de balance no se pueden verificar.
5. **Dos juegos en uno.** `AYNI_Prototipo` (3D, `PlayerController`, primitivas) y `AYNI_LagunaPelea` (2D fighter). Comparten poco. Mantener ambos duplica el costo; el prototipo 3D además se ve a “programmer art”.

### 3.2 Rendimiento y memoria

1. **`Resources/` para todo.** 558 assets y 111 MB en el build. `Resources` impide descarga selectiva, alarga el arranque y no permite parchear contenido. Migrar a **Addressables**.
2. **Generación en runtime.** Ciudad, bosque, vegetación, plataformas y texturas (fachadas de 512×512, etc.) se crean en el mismo frame. Sin batching estático, sin occlusion culling, sin LOD.
3. **Sin pooling.** Plataformas, coleccionables, hojas y proyectiles se crean/destruyen. En una pelea con más jugadores o efectos, el GC hará picos.
4. **Sin atlas de sprites.** 268 PNG sueltos, cada frame una textura independiente. Muchos draw calls y memoria de textura alta.
5. **Sin caché de assets.** `LoadClip` recarga y recrea sprites en cada llamada.
6. **Sin escenas aditivas ni streaming.** Todo el contenido de la pelea, la ciudad y el bosque vive en una sola escena.

### 3.3 Producto

- No hay **menú principal**, **pausa**, **ajustes** (volumen, resolución, calidad, remapeo), **guardado**, **créditos** ni **localización**.
- El texto narrativo está en constantes de C# (`LeyDelMonte.cs`, 12,7 KB) y en `StorybookScreens.cs` (31 KB). Cambiar una línea obliga a recompilar.
- No hay **telemetría** ni **dificultad configurable**; el balance está en campos `[SerializeField]` de componentes creados por código, así que tampoco se ajusta desde el Inspector.
- Build de **desarrollo**, con splash de Unity y sin icono propio.

---

## 4. Problemas para multijugador

### 4.1 Bloqueantes reales

| # | Bloqueante | Dónde | Por qué |
|---|---|---|---|
| 1 | No hay capa de red | `Packages/manifest.json` | Solo está `multiplayer.center` (asistente). Falta NGO/Fusion/Mirror + transporte + relay/lobby. |
| 2 | Estado global de un solo jugador | `AyniBalanceSystem.Instance`, `GameManager.Instance`, `JungleCanopy.Instance`, `Jornada.current`, `CajamarcaCalles.current`, `BatanFiambre.current` | Los singletons y `static` no representan “estado por jugador”. |
| 3 | Búsquedas globales | `FindFirstObjectByType<VizcachaFighter>()`, `Camera.main`, `FindFirstObjectByType<AnimalTransformationController>()` | La cámara, el HUD y la IA asumen **un** jugador. |
| 4 | Autoridad en el cliente | `VizcachaFighter.Update` lee `Input` y mueve `transform`; `ToroEnemyAI` ataca y aplica daño local | Cualquier cliente puede mover/atacar; no hay validación del servidor. |
| 5 | Nivel no determinista | `BattlePropSpawner` (`Random.Range` sin semilla), `CajamarcaCalles` | Cada cliente genera un mapa distinto. |
| 6 | Simulación en `Update` | `VizcachaFighter`, `ToroEnemyAI`, `LagunaBattleManager` | No hay tick fijo, ni input buffer, ni predicción/reconciliación. |
| 7 | Estado y vista mezclados | `Health`, `scoreAyni`, `IsPlaying`, `IsNightMode` en MonoBehaviours | Difícil serializar y sincronizar. |
| 8 | Historia y decisiones locales | `StorySequence`, `MoralChoiceSystem`, `StorybookScreens` | Habría que decidir si la historia es compartida o por jugador. |
| 9 | UI de un jugador | HUD con dos barras fijas y `Camera.main` | Falta HUD por jugador o HUD compartido. |
| 10 | Sin identidad ni lobby | No hay login, perfiles, salas | Falta todo el flujo online. |

### 4.2 Qué tipo de multijugador conviene

| Modo | Complejidad | Netcode sugerido | Comentario |
|---|---|---|---|
| **Co-op 2–4** (el más coherente con “ayni”) | Media | NGO + Relay + Lobby, servidor autoritativo | Dos vizcachas/animales cooperando; el estado es compartido y la historia puede ser común. |
| **PvP 1v1** (pelea) | Alta | Fusion (tick/rollback) o FishNet | Requiere simulación determinista o rollback; es un proyecto aparte. |
| **Asíncrono/social** | Baja | Backend + leaderboard | Ofrendas compartidas, ranking de “ayni”, fantasmas/replays. Funciona sin red en tiempo real y encaja con la temática. |

**Recomendación:** si el objetivo es “que se pueda jugar con alguien”, empieza por **co-op 2 jugadores** o por **social asíncrono**. El PvP competitivo real es el camino más caro y el que más se nota cuando está mal hecho.

### 4.3 Refactor mínimo para red (sin escribir netcode todavía)

1. **Separar input de simulación.**
   ```csharp
   public struct PlayerIntent
   {
       public float move;      // -1..1
       public bool jump;
       public bool attack;
       public bool heal;
       public bool interact;
   }
   ```
   `VizcachaFighter` deja de leer `Input` y consume un `PlayerIntent` que puede venir de teclado, gamepad, bot o red.

2. **Tick fijo.** Mover la simulación a `FixedUpdate` (o un `TickSystem` a 60 Hz) y que la vista interpole. Esto es requisito para cualquier netcode.

3. **Registro de jugadores.**
   ```csharp
   public sealed class PlayerRegistry : MonoBehaviour
   {
       readonly List<FighterAgent> agents = new();
       public IReadOnlyList<FighterAgent> Agents => agents;
       public FighterAgent Local { get; private set; }
   }
   ```
   La cámara y el HUD se enlazan con `registry.Local`, no con `FindFirstObjectByType`.

4. **Semilla de partida.**
   ```csharp
   Random.InitState(matchSeed); // y pasar System.Random a los generadores
   ```
   El servidor manda `matchSeed`; todos generan lo mismo. Mejor aún: **hornear el nivel** y no generarlo en runtime.

5. **Estado serializable.**
   ```csharp
   public struct FighterState
   {
       public float x, y, velocityY;
       public float health;
       public byte form;
       public ushort tick;
   }
   ```
   La vista lee `FighterState`; la red lo replica.

6. **Input System.** Ya está instalado. Crear `AYNI.inputactions` con esquemas `Keyboard` y `Gamepad` y usar `PlayerInput`. Así también sale gratis el juego local a 2 jugadores.

---

## 5. Cómo hacer los mapas editables (la solución concreta)

### 5.1 Estrategia recomendada: datos + “hornear” la escena

No hay que reescribir el generador de golpe. Se puede hacer una transición en 3 pasos:

**Paso A — El nivel como asset**

```csharp
using UnityEngine;

[CreateAssetMenu(menuName = "AYNI/Nivel", fileName = "Nivel_")]
public sealed class LevelDefinition : ScriptableObject
{
    [Header("Identidad")]
    public string id = "laguna_del_perol";
    public string displayName = "Laguna del Perol";

    [Header("Arena")]
    public float floorY = -2.5f;
    public float arenaMinX = -28f;
    public float arenaMaxX = 28f;

    [Header("Generación")]
    public int seed = 710;
    public bool generateAtRuntime = true;
    public GameObject groundPrefab;
    public PropEntry[] props;
    public SpawnPoint[] spawns;

    [Header("Presentación")]
    public AudioClip music;
    public Color skyColor = new Color(0.04f, 0.11f, 0.07f);
}

[System.Serializable]
public struct PropEntry
{
    public GameObject prefab;
    public int count;
    public Vector2 scaleRange;
    public Vector2 xRange;
    public int sortingOrder;
}

[System.Serializable]
public struct SpawnPoint
{
    public string id;
    public Vector3 position;
    public bool isPlayer;
}
```

**Paso B — Herramienta de editor “Hornear nivel”**

```csharp
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class AyniLevelBaker
{
    [MenuItem("AYNI/Niveles/Hornear nivel en escena")]
    public static void Bake()
    {
        var level = Selection.activeObject as LevelDefinition;
        if (level == null)
        {
            EditorUtility.DisplayDialog("AYNI", "Selecciona un LevelDefinition.", "OK");
            return;
        }

        var root = GameObject.Find("__NIVEL_HORNEADO__");
        if (root != null) Object.DestroyImmediate(root);
        root = new GameObject("__NIVEL_HORNEADO__");

        // Aquí se llama al generador actual, pero una sola vez.
        // El resultado queda como GameObjects reales, editables a mano.
        AyniLevelGenerator.Generate(level, root.transform);

        Undo.RegisterCreatedObjectUndo(root, "Hornear nivel AYNI");
        EditorUtility.SetDirty(root);
    }
}
#endif
```

**Paso C — El runtime deja de generar**

```csharp
public sealed class LevelRuntime : MonoBehaviour
{
    [SerializeField] LevelDefinition level;

    void Awake()
    {
        if (level == null) return;
        if (level.generateAtRuntime)
            AyniLevelGenerator.Generate(level, transform); // modo procedural
        // Si no, la escena ya contiene el nivel horneado.
    }
}
```

Con esto:
- El nivel se puede **ver, mover, pintar y borrar en el editor**.
- El generador sigue existiendo para producir variantes.
- Se puede decidir por nivel si es procedural o autorado.
- Los artistas dejan de depender de un programador para mover una roca.

### 5.2 Lo que hay que convertir en datos

| Hoy | Debería ser |
|---|---|
| `LeyDelMonte.cs` (textos) | `DialogueDatabase` (ScriptableObject o JSON) + Unity Localization |
| `StorybookScreens.cs` (capítulos) | `ChapterDefinition` assets |
| `BattleCast.cs` (stats/frames) | `FighterDefinition` assets |
| `AyniBalanceSystem` (valores iniciales) | `BalanceConfig` asset |
| `CajamarcaCalles` (layout, colores) | `CityDefinition` + prefabs + seed |
| `BattlePropSpawner` (cantidades/rangos) | `ArenaDefinition` |
| `LagunaBattleManager` (reglas) | `BattleRules` asset |

### 5.3 Para el caso 2D: Tilemap

Para la pelea, además de hornear, conviene mover el suelo/plataformas a un **Tilemap** o a prefabs colocados en la escena. Así se edita con el pincel de Unity, se ve en el editor y se puede iterar sin recompilar.

---

## 6. Por qué se ve “amateur” (y cómo corregirlo)

No es solo arte. Hay señales técnicas muy concretas:

| Señal | Evidencia | Corrección |
|---|---|---|
| Texto con Arial legacy | `Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")` en los dos bootstraps | Migrar a **TextMeshPro** + una fuente con personalidad + design tokens. |
| UI sin sistema de diseño | Canvas generado por código, anchors sueltos, `CanvasScaler` sin `matchWidthOrHeight` | Prefabs de UI, layout responsive, 9-slice, escala por aspecto. |
| Shaders planos | `AYNI_Calle.shader`: `Lighting Off`, luz falsa por vértice | URP 2D + luces 2D, o al menos sombras/rim light/color grading. |
| Sin post-proceso | No hay URP ni post-processing | Bloom, viñeta, gradación, grano, depth of field sutil. |
| Sprites sin import correcto | `textureType: 0` (Default), `alphaIsTransparency: 0`, 650×529 NPOT, `textureCompression: 1` | Import como Sprite, alpha transparency, atlas, compresión por plataforma. |
| Sin atlas | 268 PNG, `Sprite.Create` en runtime | **Sprite Atlas** + `Sprite` assets reales. |
| Sin “juice” | No hay hit-stop, shake, partículas de impacto, transiciones | Añadir cámara con shake, freeze frames, polvo, hojas, flashes. |
| Build de desarrollo | `AYNI_BurstDebugInformation_DoNotShip` | Release build, sin debug info, con icono y splash propio. |
| Splash de Unity | `m_ShowUnitySplashScreen: 1` | Desactivar si la licencia lo permite; si no, asumirlo. |
| Mezcla 3D/2D | Escena prototipo con cápsulas y cubos | Elegir un lenguaje visual; sacar el prototipo del build. |
| Sin menú/pausa/ajustes | No existen | Main menu, pausa, opciones, créditos, guardado. |
| Audio sin mezcla | Clips sueltos, sin `AudioMixer` | Buses Music/SFX/Voice, ducking, compresión. |

**Prioridad visual realista:** primero **UI + fuentes + atlas + release build** (se nota muchísimo y es barato), después **post-proceso/URP**, y por último arte nuevo.

---

## 7. Plan por fases

### Fase 0 — Higiene (1–2 días)

- [ ] `git init`, `.gitignore` de Unity, primer commit.
- [ ] Build **Release** (no Development), icono, nombre, versión.
- [ ] Separar `Assets/` de `Library/` en el control de versiones.
- [ ] Sacar `AYNI_Prototipo` del build o marcarlo como sandbox.
- [ ] Documento de decisión: ¿co-op, PvP o social?

### Fase 1 — Contenido editable (1–2 semanas)

- [ ] `LevelDefinition` + `LevelRuntime` + `AyniLevelBaker`.
- [ ] Migrar `BattlePropSpawner` a `ArenaDefinition` con semilla.
- [ ] Mover textos de `LeyDelMonte` a `DialogueDatabase`.
- [ ] Mover stats de `BattleCast` a `FighterDefinition`.
- [ ] Herramienta de editor: “Hornear nivel”, “Validar contenido”.
- [ ] Primer nivel jugable 100% editado en escena (sin generación).

**Resultado:** puedes mover el mapa, cambiar diálogos y ajustar stats sin tocar C#.

### Fase 2 — Rendimiento y presentación (2–4 semanas)

- [ ] Addressables en lugar de `Resources` para personajes/audio/escenarios.
- [ ] Sprite Atlas + import settings + caché de sprites.
- [ ] Object pooling para plataformas, hojas, proyectiles y coleccionables.
- [ ] UI con TextMeshPro + prefabs + escalado responsive.
- [ ] AudioMixer + opciones de volumen.
- [ ] Menú principal, pausa, ajustes, guardado.
- [ ] (Opcional) URP 2D + post-proceso.

### Fase 3 — Multijugador (4–8 semanas)

- [ ] `PlayerIntent` + tick fijo + `PlayerRegistry`.
- [ ] `FighterState` serializable + autoridad del host.
- [ ] NGO + Relay + Lobby (co-op 2–4) **o** Fusion (PvP 1v1).
- [ ] Semilla de partida sincronizada.
- [ ] HUD/cámara por jugador.
- [ ] Pruebas con 2 clientes + latencia simulada.

### Fase 4 — Producto (continuo)

- [ ] Localización (es/en/qu).
- [ ] Accesibilidad y remapeo.
- [ ] Telemetría y dificultad.
- [ ] Créditos, licencias de assets (Kenney, AllSky, etc.).
- [ ] Pipeline de build/CI.

---

## 8. Los 5 cambios con más impacto

1. **`git init` + build Release.** Sin esto, cualquier mejora es reversible por accidente.
2. **Hornear el nivel a la escena** y crear `LevelDefinition`. Es lo que desbloquea “modificar mapas”.
3. **Semilla determinista** en todo lo que use `Random`. Sin esto no hay multijugador ni pruebas reproducibles.
4. **Separar input → intención → simulación.** Es el prerrequisito de red y también mejora el gameplay local.
5. **TextMeshPro + atlas + menú/pausa/ajustes.** Es lo que más cambia la percepción de “amateur” por menos esfuerzo.

---

## 9. Notas finales

- El proyecto **no tiene errores de compilación** en los logs revisados.
- El build actual **funciona**, pero es de desarrollo y no tiene paracaídas de producto (menú, guardado, ajustes).
- La mayor fortaleza es la cantidad de contenido narrativo y visual ya producido. La mayor debilidad es que ese contenido está **atrapado en código**.
- No recomiendo añadir netcode antes de la Fase 1: si el nivel cambia en cada cliente y el estado es global, el multijugador será inestable por diseño.
