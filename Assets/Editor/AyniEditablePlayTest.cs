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
    /// Prueba automática de la escena horneada: entra en Play, arranca la batalla
    /// y comprueba que el mundo editado no se regenera ni se duplica.
    /// El estado vive en SessionState para sobrevivir a la recarga de dominio
    /// que Unity hace al entrar en Play Mode.
    /// </summary>
    [InitializeOnLoad]
    public static class AyniEditablePlayTest
    {
        const string ScenePath = "Assets/Scenes/AYNI_LagunaPelea_EDITABLE.unity";
        const string ResultPath = "Logs/playtest-editable-result.txt";

        const string PendingKey = "AYNI.PlayTest.Pending";
        const string StepKey = "AYNI.PlayTest.Step";
        const string StartedKey = "AYNI.PlayTest.Started";
        const string PlatformsKey = "AYNI.PlayTest.Platforms";
        const string CollectiblesKey = "AYNI.PlayTest.Collectibles";
        const string MovedNameKey = "AYNI.PlayTest.MovedName";
        const string MovedXKey = "AYNI.PlayTest.MovedX";

        static AyniEditablePlayTest()
        {
            Resume();
        }

        [MenuItem("AYNI/Escenas/Probar escena editable — Laguna", priority = 13)]
        public static void Run()
        {
            try
            {
                if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                    return;

                var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
                if (!scene.IsValid()) throw new Exception("No se pudo abrir " + ScenePath);

                SessionState.SetInt(PlatformsKey, Count<BattlePlatform>());
                SessionState.SetInt(CollectiblesKey, Count<BattleCollectible>());

                // Simula el trabajo normal en el editor: mover un coleccionable
                // con la herramienta Move, sin tocar startPos en el Inspector.
                var sample = UnityEngine.Object.FindAnyObjectByType<BattleCollectible>();
                if (sample != null)
                {
                    float targetX = sample.transform.position.x + 0.75f;
                    sample.transform.position += Vector3.right * 0.75f;
                    SessionState.SetString(MovedNameKey, sample.name);
                    SessionState.SetString(MovedXKey, targetX.ToString("R"));
                }
                else
                {
                    SessionState.SetString(MovedNameKey, string.Empty);
                }

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

        /// <summary>Se ejecuta tras cada recarga de dominio, incluida la de entrar en Play.</summary>
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
                    // La escena ya lleva un momento en Play: comprobamos que el
                    // mundo horneado sobrevivió y arrancamos la batalla.
                    if (elapsed < 2.0) return;

                    var manager = UnityEngine.Object.FindAnyObjectByType<LagunaBattleManager>();
                    if (manager == null) throw new Exception("No hay LagunaBattleManager en Play.");

                    var root = GameObject.Find(AyniBakedWorld.RootName);
                    if (root == null) throw new Exception("El mundo horneado desapareció en Play.");

                    SessionState.SetInt(StepKey, 1);
                    SessionState.SetString(StartedKey, EditorApplication.timeSinceStartup.ToString("R"));
                    manager.StartBattle();
                    return;
                }

                if (elapsed < 2.0) return;

                int beforePlatforms = SessionState.GetInt(PlatformsKey, -1);
                int beforeCollectibles = SessionState.GetInt(CollectiblesKey, -1);
                int platforms = Count<BattlePlatform>();
                int collectibles = Count<BattleCollectible>();
                bool rootAlive = GameObject.Find(AyniBakedWorld.RootName) != null;
                bool duplicated = platforms != beforePlatforms || collectibles != beforeCollectibles;
                bool moveRespected = CheckMovedCollectible();

                string line =
                    "[AYNI] PlayTest EDITABLE" +
                    $" | plataformas={beforePlatforms}->{platforms}" +
                    $" | coleccionables={beforeCollectibles}->{collectibles}" +
                    $" | duplicado={duplicated}" +
                    $" | mundo={rootAlive}" +
                    $" | moverColeccionable={moveRespected}";

                WriteResult(line);
                Debug.Log(line);
                Finish(duplicated || !rootAlive || !moveRespected ? 1 : 0);
            }
            catch (Exception e)
            {
                Fail(e);
            }
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

            // En batch cerramos Unity con el código de salida.
            // Desde el menú, volvemos al editor sin cerrarlo.
            if (Application.isBatchMode)
                EditorApplication.Exit(code);
        }

        static int Count<T>() where T : UnityEngine.Object
        {
            return UnityEngine.Object.FindObjectsByType<T>(FindObjectsInactive.Include).Length;
        }

        static bool CheckMovedCollectible()
        {
            string name = SessionState.GetString(MovedNameKey, string.Empty);
            if (string.IsNullOrEmpty(name)) return true;

            string raw = SessionState.GetString(MovedXKey, "0");
            if (!double.TryParse(raw, out double expected)) return false;

            var go = GameObject.Find(name);
            if (go == null) return false;

            var collectible = go.GetComponent<BattleCollectible>();
            if (collectible == null) return false;

            return Mathf.Abs(collectible.transform.position.x - (float)expected) < 0.05f;
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
