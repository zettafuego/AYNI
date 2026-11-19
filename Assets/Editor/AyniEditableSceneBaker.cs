#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using AYNI.Battle;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace AYNI.Editor
{
    /// <summary>
    /// Convierte el mundo procedural de la Laguna en GameObjects reales dentro de una escena.
    /// El resultado se puede mover, borrar, duplicar y pintar desde el editor.
    /// </summary>
    public static class AyniEditableSceneBaker
    {
        const string SourceScene = "Assets/Scenes/AYNI_LagunaPelea.unity";
        const string TargetScene = "Assets/Scenes/AYNI_LagunaPelea_EDITABLE.unity";
        const string GeneratedFolder = "Assets/Generated/LagunaPelea";

        [MenuItem("AYNI/Escenas/Generar escena editable — Laguna", priority = 10)]
        public static void BakeLaguna()
        {
            try
            {
                BakeInternal();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                Debug.LogError("[AYNI] No se pudo generar la escena editable.");
                if (Application.isBatchMode) EditorApplication.Exit(1);
            }
        }

        [MenuItem("AYNI/Escenas/Abrir escena editable — Laguna", priority = 11)]
        public static void OpenLaguna()
        {
            if (!File.Exists(TargetScene))
            {
                EditorUtility.DisplayDialog(
                    "AYNI",
                    "Todavía no existe la escena editable.\n\n" +
                    "Usa primero: AYNI > Escenas > Generar escena editable — Laguna.",
                    "OK");
                return;
            }

            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                EditorSceneManager.OpenScene(TargetScene, OpenSceneMode.Single);
        }

        [MenuItem("AYNI/Escenas/Validar escena editable — Laguna", priority = 12)]
        public static void ValidateLaguna()
        {
            if (!File.Exists(TargetScene))
            {
                Debug.LogWarning("[AYNI] No existe " + TargetScene);
                return;
            }

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            var scene = EditorSceneManager.OpenScene(TargetScene, OpenSceneMode.Single);
            var root = GameObject.Find(AyniBakedWorld.RootName);
            if (root == null)
            {
                Debug.LogError("[AYNI] La escena existe pero no tiene " + AyniBakedWorld.RootName);
                return;
            }

            int spritesMissing = 0;
            int meshesMissing = 0;
            int materialsMissing = 0;

            foreach (var sr in root.GetComponentsInChildren<SpriteRenderer>(true))
                if (sr.sprite == null) spritesMissing++;
            foreach (var mf in root.GetComponentsInChildren<MeshFilter>(true))
                if (mf.sharedMesh == null) meshesMissing++;
            foreach (var mr in root.GetComponentsInChildren<MeshRenderer>(true))
                if (mr.sharedMaterial == null) materialsMissing++;

            var manager = UnityEngine.Object.FindFirstObjectByType<LagunaBattleManager>();
            bool detected = manager != null;
            bool baked = GameObject.Find(AyniBakedWorld.RootName) != null;

            Debug.Log(
                $"[AYNI] Validación Laguna EDITABLE | escena={scene.name}" +
                $" | objetos={Count(root)}" +
                $" | plataformas={root.GetComponentsInChildren<BattlePlatform>(true).Length}" +
                $" | coleccionables={root.GetComponentsInChildren<BattleCollectible>(true).Length}" +
                $" | spritesSinRef={spritesMissing}" +
                $" | mallasSinRef={meshesMissing}" +
                $" | materialesSinRef={materialsMissing}" +
                $" | manager={detected} | mundoHorneado={baked}");
        }

        /// <summary>Entrada para Unity en batch mode (-executeMethod).</summary>
        public static void BakeLagunaBatch()
        {
            BakeLaguna();
        }

        static void BakeInternal()
        {
            if (!File.Exists(SourceScene))
                throw new FileNotFoundException("No existe la escena base: " + SourceScene);

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            bool existing = File.Exists(TargetScene);

            if (existing && !Application.isBatchMode)
            {
                bool overwrite = EditorUtility.DisplayDialog(
                    "AYNI — Regenerar Laguna",
                    "Esto reconstruirá la Laguna dentro de la escena editable.\n\n" +
                    "Se perderán los cambios manuales hechos dentro de " +
                    AyniBakedWorld.RootName + " y se recreará la carpeta " +
                    "Assets/Generated/LagunaPelea.\n\n" +
                    "Cajamarca (" + AyniBakedCajamarca.RootName + ") se conserva " +
                    "tal como está.\n\n¿Continuar?",
                    "Regenerar",
                    "Cancelar");

                if (!overwrite) return;
            }

            if (!existing)
            {
                // Primera vez: se parte de la escena original intacta.
                if (!AssetDatabase.CopyAsset(SourceScene, TargetScene))
                    throw new Exception("AssetDatabase.CopyAsset devolvió false.");

                AssetDatabase.Refresh();
            }

            // Si la escena ya existía, se abre tal cual y solo se rehacen
            // los roots de la laguna. Así los demás distritos horneados
            // (por ejemplo Cajamarca) sobreviven al re-horneado.
            var scene = EditorSceneManager.OpenScene(TargetScene, OpenSceneMode.Single);
            var manager = UnityEngine.Object.FindFirstObjectByType<LagunaBattleManager>();
            if (manager == null)
                throw new Exception("La escena no contiene un LagunaBattleManager.");

            // Limpieza por si se vuelve a hornear sobre la misma escena.
            var oldRoot = GameObject.Find(AyniBakedWorld.RootName);
            if (oldRoot != null) UnityEngine.Object.DestroyImmediate(oldRoot);
            var oldParallax = GameObject.Find("ParallaxRoot");
            if (oldParallax != null) UnityEngine.Object.DestroyImmediate(oldParallax);
            var oldProps = GameObject.Find("BattleProps");
            if (oldProps != null) UnityEngine.Object.DestroyImmediate(oldProps);
            var oldRiver = GameObject.Find("RioDeEntrada");
            if (oldRiver != null) UnityEngine.Object.DestroyImmediate(oldRiver);
            var oldGround = manager.transform.Find("SueloYMonte");
            if (oldGround != null) UnityEngine.Object.DestroyImmediate(oldGround.gameObject);
            var oldFront = manager.transform.Find("IchuDelante");
            if (oldFront != null) UnityEngine.Object.DestroyImmediate(oldFront.gameObject);

            if (AssetDatabase.IsValidFolder(GeneratedFolder))
                AssetDatabase.DeleteAsset(GeneratedFolder);
            EnsureFolder(GeneratedFolder);
            EnsureFolder(GeneratedFolder + "/Meshes");
            EnsureFolder(GeneratedFolder + "/Materials");

            var root = new GameObject(AyniBakedWorld.RootName);
            root.AddComponent<AyniBakedWorld>();

            float floor = manager.FloorY;
            float minX = manager.ArenaMinX;
            float maxX = manager.ArenaMaxX;

            // 1) Fondo, cielo y capas de parallax.
            //    Se quedan como hijas del manager para que BattleParallaxSystem
            //    siga encontrándolas con GetComponentsInChildren.
            var parallax = manager.GetComponent<BattleParallaxSystem>();
            if (parallax == null) parallax = manager.gameObject.AddComponent<BattleParallaxSystem>();
            parallax.Build(floor, maxX);

            // 2) Suelo, montañas, ichu, rocas y cantutas (mallas combinadas).
            var dressing = MonteDressing.Ensure(manager.transform);
            dressing.BuildField(floor, minX, maxX);
            Reparent(manager.transform.Find("SueloYMonte")?.gameObject, root.transform);
            Reparent(manager.transform.Find("IchuDelante")?.gameObject, root.transform);

            // 3) Río de entrada.
            var river = UnityEngine.Object.FindFirstObjectByType<RioDeEntrada>();
            if (river == null) river = RioDeEntrada.Ensure(manager.transform, floor);
            Reparent(river.gameObject, root.transform);

            // 4) Plataformas y coleccionables con semilla fija.
            //    Antes eran aleatorios en cada partida; ahora son piezas editables.
            UnityEngine.Random.InitState(710);
            var spawner = manager.GetComponent<BattlePropSpawner>();
            if (spawner == null) spawner = manager.gameObject.AddComponent<BattlePropSpawner>();
            spawner.SpawnAll(floor, minX, maxX);
            Reparent(GameObject.Find("BattleProps"), root.transform);

            // 5) Guardar mallas y materiales como assets para que la escena no sea gigante
            //    y para que se puedan reutilizar/editar por separado.
            PersistMeshes(root);
            PersistMaterials(root);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, TargetScene);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log(
                $"[AYNI] Escena editable generada: {TargetScene}" +
                $" | objetos={Count(root)}" +
                $" | plataformas={root.GetComponentsInChildren<BattlePlatform>(true).Length}" +
                $" | coleccionables={root.GetComponentsInChildren<BattleCollectible>(true).Length}" +
                $" | mallas={root.GetComponentsInChildren<MeshFilter>(true).Length}");
        }

        static void Reparent(GameObject go, Transform parent)
        {
            if (go == null || parent == null) return;
            go.transform.SetParent(parent, true);
        }

        static void PersistMeshes(GameObject root)
        {
            var seen = new Dictionary<Mesh, Mesh>();
            int index = 0;

            foreach (var filter in root.GetComponentsInChildren<MeshFilter>(true))
            {
                var mesh = filter.sharedMesh;
                if (mesh == null || AssetDatabase.Contains(mesh)) continue;

                if (!seen.TryGetValue(mesh, out var asset))
                {
                    string path = $"{GeneratedFolder}/Meshes/{Sanitize(mesh.name)}_{index++}.asset";
                    AssetDatabase.CreateAsset(mesh, path);
                    asset = AssetDatabase.LoadAssetAtPath<Mesh>(path);
                    seen[mesh] = asset;
                }

                filter.sharedMesh = asset;
            }

            AssetDatabase.SaveAssets();
        }

        static void PersistMaterials(GameObject root)
        {
            var seen = new Dictionary<Material, Material>();
            int index = 0;

            foreach (var renderer in root.GetComponentsInChildren<MeshRenderer>(true))
            {
                var material = renderer.sharedMaterial;
                if (material == null || AssetDatabase.Contains(material)) continue;

                if (!seen.TryGetValue(material, out var asset))
                {
                    string path = $"{GeneratedFolder}/Materials/{Sanitize(material.name)}_{index++}.mat";
                    AssetDatabase.CreateAsset(material, path);
                    asset = AssetDatabase.LoadAssetAtPath<Material>(path);
                    seen[material] = asset;
                }

                renderer.sharedMaterial = asset;
            }

            AssetDatabase.SaveAssets();
        }

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;

            string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            string name = Path.GetFileName(path);
            if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
                EnsureFolder(parent);

            AssetDatabase.CreateFolder(parent, name);
        }

        static string Sanitize(string name)
        {
            if (string.IsNullOrEmpty(name)) return "sin_nombre";

            foreach (char c in Path.GetInvalidFileNameChars())
                name = name.Replace(c, '_');

            return name.Replace(' ', '_');
        }

        static int Count(GameObject root)
        {
            if (root == null) return 0;
            return root.GetComponentsInChildren<Transform>(true).Length;
        }
    }
}
#endif
