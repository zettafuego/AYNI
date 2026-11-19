# AYNI — El camino de Atoq

Unity 6000.5.0f1, Built-in RP. Raíz: `C:\Users\Eduardo\AYNI-Unity`.
Responde en español. Estudio: Waffles Tech.

Este repo tiene cuatro agentes especialistas. En una tarea que toque más de un oficio, lánzalos en paralelo con `spawn_subagent` y el tipo que corresponda. En una tarea de un solo oficio, lanza solo ese.

| Tipo | Oficio | Cuándo |
| --- | --- | --- |
| `historia` | Storytelling | Copy, tono, capítulos, diálogos, hechos de promesa/río/oro |
| `ux-ui` | UX / UI | Menús, HUD, placas, iconos, layout, flujo de pantallas |
| `animacion` | Animación | Clips de sprites, ciclos, pivotes, motion |
| `programacion` | Programación | C#, sistemas, input, cámaras, compile, cableado |

El prompt del hijo debe ser autónomo: rutas, archivos, y qué se considera hecho. `cwd` = esta raíz. Unity MCP y `unity-cli` van con programación (y con animación/UI si hay Editor abierto).

No copies Disney. No edites `.unity` / `.prefab` / `.asset` a mano si `unity status` muestra un Editor `ready`. Copy de jugador en `LeyDelMonte`. Elenco en `BattleCast`. Pivote solo por `SpriteMetrics.PivotFor`. Compilar: `dotnet build Assembly-CSharp.csproj` — 0 errores.

Cerrar Play borra la sesión (`PartidaDelCamino`).
