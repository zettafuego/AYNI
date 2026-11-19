#if UNITY_EDITOR
using System;
using System.IO;
using AYNI.Battle;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace AYNI.Editor
{
    /// <summary>
    /// Prueba del capítulo del bosque horneado: abrir, cerrar y reabrir.
    /// Comprueba que la arboleda real se reutiliza, que el piso tiene textura
    /// y que lo dinámico se limpia.
    /// </summary>
    [InitializeOnLoad]
    public static class AyniBosquePlayTest
    {
        const string ScenePath = "Assets/Scenes/AYNI_LagunaPelea_EDITABLE.unity";
        const string ResultPath = "Logs/playtest-bosque-result.txt";

        const string PendingKey = "AYNI.BosqueTest.Pending";
        const string StepKey = "AYNI.BosqueTest.Step";
        const string StartedKey = "AYNI.BosqueTest.Started";
        const string MeshesKey = "AYNI.BosqueTest.Meshes";

        static AyniBosquePlayTest()
        {
            Resume();
        }

        [MenuItem("AYNI/Escenas/Probar bosque editable", priority = 33)]
        public static void Run()
        {
            try
            {
                if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                    return;

                var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
                if (!scene.IsValid()) throw new Exception("No se pudo abrir " + ScenePath);

                var marker = FindMarker();
                if (marker == null) throw new Exception("No existe el bosque horneado en la escena.");

                SessionState.SetInt(MeshesKey, CountStaticMeshes(marker.transform));
                SessionState.SetInt(StepKey, 0);
                SessionState.SetBool(PendingKey, true);
                SessionState.SetString(StartedKey, EditorApplication.timeSinceStartup.ToString("R"));

                WriteResult("iniciando play mode…");
                EditorApplication.isPlaying = true;
            }
            catch (Exception e)
            {
                Fail(e);
            }
        }

        static void Resume()
        {
            if (!SessionState.GetBool(PendingKey, false)) return;

            EditorApplication.update -= Tick;
            EditorApplication.update += Tick;
        }

        static void Tick()
        {
            if (!SessionState.GetBool(PendingKey, false))
            {
                EditorApplication.update -= Tick;
                return;
            }

            if (!EditorApplication.isPlaying) return;

            double started = ParseStarted();
            double elapsed = EditorApplication.timeSinceStartup - started;
            int step = SessionState.GetInt(StepKey, 0);

            try
            {
                if (step == 0)
                {
                    if (elapsed < 2.0) return;

                    var manager = UnityEngine.Object.FindAnyObjectByType<LagunaBattleManager>();
                    if (manager == null) throw new Exception("No hay LagunaBattleManager en Play.");

                    manager.StartBattle();
                    BosqueCapitulo.Begin(manager);
                    NextStep(1);
                    return;
                }

                if (step == 1)
                {
                    if (elapsed < 2.5) return;

                    var marker = RequireMarker();
                    var grove = marker.transform.Find(AyniBakedBosque.GroveName);
                    var dynamicRoot = grove != null ? grove.Find("__DINAMICO__") : null;
                    int walkers = dynamicRoot != null ? dynamicRoot.childCount : 0;
                    int meshes = CountStaticMeshes(marker.transform);
                    int expected = SessionState.GetInt(MeshesKey, -1);

                    if (!BosqueCapitulo.IsOpen)
                        throw new Exception("El bosque no se abrió.");
                    if (!marker.gameObject.activeSelf)
                        throw new Exception("La arboleda horneada no se activó.");
                    if (dynamicRoot == null || walkers < 3)
                        throw new Exception("No se crearon los caminantes dinámicos.");
                    if (meshes != expected)
                        throw new Exception($"Mallas estáticas cambiaron al abrir: {expected}->{meshes}");
                    if (!FloorHasTexture(marker.transform))
                        throw new Exception("El suelo no tiene textura.");

                    BosqueCapitulo.CloseCurrent();
                    NextStep(2);
                    return;
                }

                if (step == 2)
                {
                    if (elapsed < 1.5) return;

                    var marker = RequireMarker();
                    var grove = marker.transform.Find(AyniBakedBosque.GroveName);
                    var dynamicRoot = grove != null ? grove.Find("__DINAMICO__") : null;
                    int meshes = CountStaticMeshes(marker.transform);
                    int expected = SessionState.GetInt(MeshesKey, -1);

                    if (BosqueCapitulo.IsOpen)
                        throw new Exception("El bosque no se cerró.");
                    if (marker.gameObject.activeSelf)
                        throw new Exception("La arboleda horneada no se ocultó al cerrar.");
                    if (dynamicRoot != null)
                        throw new Exception("Quedaron objetos dinámicos tras cerrar.");
                    if (meshes != expected)
                        throw new Exception($"Mallas estáticas cambiaron al cerrar: {expected}->{meshes}");

                    var manager = UnityEngine.Object.FindAnyObjectByType<LagunaBattleManager>();
                    if (manager == null) throw new Exception("No hay manager para reabrir.");
                    BosqueCapitulo.Begin(manager);
                    NextStep(3);
                    return;
                }

                if (elapsed < 2.5) return;

                var reopened = RequireMarker();
                var reopenedGrove = reopened.transform.Find(AyniBakedBosque.GroveName);
                var reopenedDynamic = reopenedGrove != null
                    ? reopenedGrove.Find("__DINAMICO__")
                    : null;
                int reopenedWalkers = reopenedDynamic != null ? reopenedDynamic.childCount : 0;
                int reopenedMeshes = CountStaticMeshes(reopened.transform);
                int expectedMeshes = SessionState.GetInt(MeshesKey, -1);

                string line =
                    "[AYNI] PlayTest BOSQUE" +
                    $" | abrir={BosqueCapitulo.IsOpen}" +
                    $" | arboledaActiva={reopened.gameObject.activeSelf}" +
                    $" | caminantes={reopenedWalkers}" +
                    $" | mallas={reopenedMeshes}" +
                    $" | texturaSuelo={FloorHasTexture(reopened.transform)}" +
                    $" | sinDuplicar={reopenedMeshes == expectedMeshes}";

                WriteResult(line);
                Debug.Log(line);

                bool ok = BosqueCapitulo.IsOpen
                          && reopened.gameObject.activeSelf
                          && reopenedWalkers >= 3
                          && reopenedMeshes == expectedMeshes
                          && FloorHasTexture(reopened.transform);
                Finish(ok ? 0 : 1);
            }
            catch (Exception e)
            {
                Fail(e);
            }
        }

        static void NextStep(int step)
        {
            SessionState.SetInt(StepKey, step);
            SessionState.SetString(StartedKey, EditorApplication.timeSinceStartup.ToString("R"));
        }

        static AyniBakedBosque RequireMarker()
        {
            var marker = FindMarker();
            if (marker == null) throw new Exception("Desapareció " + AyniBakedBosque.RootName);
            return marker;
        }

        static AyniBakedBosque FindMarker()
        {
            var all = UnityEngine.Object.FindObjectsByType<AyniBakedBosque>(
                FindObjectsInactive.Include);
            return all.Length > 0 ? all[0] : null;
        }

        static bool FloorHasTexture(Transform root)
        {
            var grove = root.Find(AyniBakedBosque.GroveName);
            var floor = grove != null ? grove.Find("Suelo") : null;
            var renderer = floor != null ? floor.GetComponent<Renderer>() : null;
            return renderer != null
                   && renderer.sharedMaterial != null
                   && renderer.sharedMaterial.mainTexture != null;
        }

        static int CountStaticMeshes(Transform root)
        {
            int count = 0;
            foreach (var filter in root.GetComponentsInChildren<MeshFilter>(true))
            {
                var t = filter.transform;
                bool runtime = false;
                while (t != null && t != root)
                {
                    if (t.name == "__DINAMICO__")
                    {
                        runtime = true;
                        break;
                    }

                    t = t.parent;
                }

                if (!runtime) count++;
            }

            return count;
        }

        static void Fail(Exception e)
        {
            WriteResult("ERROR: " + e);
            Debug.LogException(e);
            Finish(1);
        }

        static void Finish(int code)
        {
            SessionState.SetBool(PendingKey, false);
            EditorApplication.update -= Tick;
            EditorApplication.isPlaying = false;

            if (Application.isBatchMode)
                EditorApplication.Exit(code);
        }

        static double ParseStarted()
        {
            string raw = SessionState.GetString(StartedKey, "0");
            return double.TryParse(raw, out double value) ? value : 0.0;
        }

        static void WriteResult(string line)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(ResultPath) ?? ".");
                File.WriteAllText(ResultPath, line);
            }
            catch (Exception e)
            {
                Debug.LogWarning("[AYNI] No se pudo escribir el resultado: " + e.Message);
            }
        }
    }
}
#endif
