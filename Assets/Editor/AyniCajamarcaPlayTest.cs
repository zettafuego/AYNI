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
    /// Prueba del capítulo de Cajamarca horneado: abrir, cerrar y reabrir.
    /// Comprueba que el distrito real se reutiliza y que lo dinámico se limpia.
    /// </summary>
    [InitializeOnLoad]
    public static class AyniCajamarcaPlayTest
    {
        const string ScenePath = "Assets/Scenes/AYNI_LagunaPelea_EDITABLE.unity";
        const string ResultPath = "Logs/playtest-cajamarca-result.txt";

        const string PendingKey = "AYNI.CajamarcaTest.Pending";
        const string StepKey = "AYNI.CajamarcaTest.Step";
        const string StartedKey = "AYNI.CajamarcaTest.Started";
        const string MeshesKey = "AYNI.CajamarcaTest.Meshes";

        static AyniCajamarcaPlayTest()
        {
            Resume();
        }

        [MenuItem("AYNI/Escenas/Probar Cajamarca editable", priority = 23)]
        public static void Run()
        {
            try
            {
                if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                    return;

                var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
                if (!scene.IsValid()) throw new Exception("No se pudo abrir " + ScenePath);

                var marker = FindMarker();
                if (marker == null) throw new Exception("No existe Cajamarca horneada en la escena.");

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
                    CajamarcaCalles.Begin(manager);
                    NextStep(1);
                    return;
                }

                if (step == 1)
                {
                    if (elapsed < 2.5) return;

                    var marker = RequireMarker();
                    var district = marker.transform.Find(AyniBakedCajamarca.DistrictName);
                    var dynamicRoot = district != null ? district.Find("__DINAMICO__") : null;
                    int walkers = dynamicRoot != null ? dynamicRoot.childCount : 0;
                    int meshes = CountStaticMeshes(marker.transform);
                    int expected = SessionState.GetInt(MeshesKey, -1);

                    if (!CajamarcaCalles.IsOpen)
                        throw new Exception("Cajamarca no se abrió.");
                    if (!marker.gameObject.activeSelf)
                        throw new Exception("El distrito horneado no se activó.");
                    if (dynamicRoot == null || walkers < 3)
                        throw new Exception("No se crearon los caminantes dinámicos.");
                    if (meshes != expected)
                        throw new Exception($"Mallas estáticas cambiaron al abrir: {expected}->{meshes}");

                    CajamarcaCalles.CloseCurrent();
                    NextStep(2);
                    return;
                }

                if (step == 2)
                {
                    if (elapsed < 1.5) return;

                    var marker = RequireMarker();
                    var district = marker.transform.Find(AyniBakedCajamarca.DistrictName);
                    var dynamicRoot = district != null ? district.Find("__DINAMICO__") : null;
                    int meshes = CountStaticMeshes(marker.transform);
                    int expected = SessionState.GetInt(MeshesKey, -1);

                    if (CajamarcaCalles.IsOpen)
                        throw new Exception("Cajamarca no se cerró.");
                    if (marker.gameObject.activeSelf)
                        throw new Exception("El distrito horneado no se ocultó al cerrar.");
                    if (dynamicRoot != null)
                        throw new Exception("Quedaron objetos dinámicos tras cerrar.");
                    if (meshes != expected)
                        throw new Exception($"Mallas estáticas cambiaron al cerrar: {expected}->{meshes}");

                    var manager = UnityEngine.Object.FindAnyObjectByType<LagunaBattleManager>();
                    if (manager == null) throw new Exception("No hay manager para reabrir.");
                    CajamarcaCalles.Begin(manager);
                    NextStep(3);
                    return;
                }

                if (elapsed < 2.5) return;

                var reopened = RequireMarker();
                var reopenedDistrict = reopened.transform.Find(AyniBakedCajamarca.DistrictName);
                var reopenedDynamic = reopenedDistrict != null
                    ? reopenedDistrict.Find("__DINAMICO__")
                    : null;
                int reopenedWalkers = reopenedDynamic != null ? reopenedDynamic.childCount : 0;
                int reopenedMeshes = CountStaticMeshes(reopened.transform);
                int expectedMeshes = SessionState.GetInt(MeshesKey, -1);

                string line =
                    "[AYNI] PlayTest CAJAMARCA" +
                    $" | abrir={CajamarcaCalles.IsOpen}" +
                    $" | distritoActivo={reopened.gameObject.activeSelf}" +
                    $" | caminantes={reopenedWalkers}" +
                    $" | mallas={reopenedMeshes}" +
                    $" | sinDuplicar={reopenedMeshes == expectedMeshes}";

                WriteResult(line);
                Debug.Log(line);

                bool ok = CajamarcaCalles.IsOpen
                          && reopened.gameObject.activeSelf
                          && reopenedWalkers >= 3
                          && reopenedMeshes == expectedMeshes;
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

        static AyniBakedCajamarca RequireMarker()
        {
            var marker = FindMarker();
            if (marker == null) throw new Exception("Desapareció " + AyniBakedCajamarca.RootName);
            return marker;
        }

        static AyniBakedCajamarca FindMarker()
        {
            var all = UnityEngine.Object.FindObjectsByType<AyniBakedCajamarca>(
                FindObjectsInactive.Include);
            return all.Length > 0 ? all[0] : null;
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
                    if (t.name == "NombresCalles" || t.name == "__DINAMICO__")
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
