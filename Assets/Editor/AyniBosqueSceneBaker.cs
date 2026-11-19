#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using AYNI.Battle;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace AYNI.Editor
{
    /// <summary>
    /// Convierte la arboleda procedural del capítulo del bosque en un distrito
    /// real dentro de la escena editable. Laguna y Cajamarca se conservan.
    /// </summary>
    public static class AyniBosqueSceneBaker
    {
        const string ScenePath = "Assets/Scenes/AYNI_LagunaPelea_EDITABLE.unity";
        const string GeneratedFolder = "Assets/Generated/Bosque";
        const string RootName = AyniBakedBosque.RootName;
        const float GroveX = -360f;

        [MenuItem("AYNI/Escenas/Generar bosque editable", priority = 30)]
        public static void BakeBosque()
        {
            try
            {
                BakeInternal();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                Debug.LogError("[AYNI] No se pudo generar el bosque editable.");
                if (Application.isBatchMode) EditorApplication.Exit(1);
            }
        }

        [MenuItem("AYNI/Escenas/Enfocar bosque en la escena", priority = 31)]
        public static void FocusBosque()
        {
            var marker = FindMarker();
            if (marker == null)
            {
                EditorUtility.DisplayDialog(
                    "AYNI",
                    "Todavía no existe el bosque editable.\n\n" +
                    "Usa primero: AYNI > Escenas > Generar bosque editable.",
                    "OK");
                return;
            }

            Selection.activeGameObject = marker.gameObject;
            var grove = marker.transform.Find(AyniBakedBosque.GroveName);
            var path = grove != null ? grove.Find("Sendero") : null;
            if (path != null) Selection.activeGameObject = path.gameObject;
            SceneView.lastActiveSceneView?.FrameSelected();
        }

        [MenuItem("AYNI/Escenas/Validar bosque editable", priority = 32)]
        public static void ValidateBosque()
        {
            if (!File.Exists(ScenePath))
            {
                Debug.LogWarning("[AYNI] No existe " + ScenePath);
                return;
            }

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var marker = FindMarker();
            if (marker == null)
            {
                Debug.LogError("[AYNI] La escena no contiene " + RootName);
                return;
            }

            var root = marker.gameObject;
            int meshesMissing = 0;
            int materialsMissing = 0;
            foreach (var filter in root.GetComponentsInChildren<MeshFilter>(true))
                if (filter.sharedMesh == null) meshesMissing++;
            foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
                foreach (var material in renderer.sharedMaterials)
                    if (material == null) materialsMissing++;

            var grove = root.transform.Find(AyniBakedBosque.GroveName);
            var floor = grove != null ? grove.Find("Suelo") : null;
            var floorRenderer = floor != null ? floor.GetComponent<Renderer>() : null;
            bool floorTexture = floorRenderer != null
                                && floorRenderer.sharedMaterial != null
                                && floorRenderer.sharedMaterial.mainTexture != null;
            int trees = CountByPrefix(grove, "Bosque/tree")
                        + CountByPrefix(grove, "Coniferas/Prefabs/trees");
            int tiles = CountNamed(grove, "Bosque/grass")
                        + CountNamed(grove, "Bosque/grass_large")
                        + CountNamed(grove, "Bosque/grass_leafs");
            int berries = CountNamed(grove, "BayaDeAura");
            var river = grove != null ? grove.Find("Rio") : null;
            var path = grove != null ? grove.Find("Sendero") : null;

            Debug.Log(
                $"[AYNI] Validación BOSQUE EDITABLE | escena={scene.name}" +
                $" | objetos={root.GetComponentsInChildren<Transform>(true).Length}" +
                $" | mallas={root.GetComponentsInChildren<MeshFilter>(true).Length}" +
                $" | renderers={root.GetComponentsInChildren<Renderer>(true).Length}" +
                $" | arboleda={grove != null} | suelo={floor != null}" +
                $" | texturaSuelo={floorTexture} | cesped={tiles} | arboles={trees}" +
                $" | rio={river != null} | sendero={path != null} | bayas={berries}" +
                $" | mallasSinRef={meshesMissing} | materialesSinRef={materialsMissing}");
        }

        public static void BakeBosqueBatch()
        {
            BakeBosque();
        }

        static int CountNamed(Transform root, string name)
        {
            if (root == null) return 0;
            int count = 0;
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
                if (t.name == name) count++;
            return count;
        }

        static int CountByPrefix(Transform root, string prefix)
        {
            if (root == null) return 0;
            int count = 0;
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
                if (t.parent == root && t.name.StartsWith(prefix, StringComparison.Ordinal))
                    count++;
            return count;
        }

        static void BakeInternal()
        {
            if (!File.Exists(ScenePath))
                throw new FileNotFoundException("No existe la escena editable: " + ScenePath);

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            if (AssetDatabase.IsValidFolder(GeneratedFolder) && !Application.isBatchMode)
            {
                bool overwrite = EditorUtility.DisplayDialog(
                    "AYNI — Regenerar bosque",
                    "Esto reemplazará la arboleda actual.\n\n" +
                    "Se perderán los cambios manuales hechos dentro de " + RootName +
                    " y se recreará la carpeta Assets/Generated/Bosque.\n\n" +
                    "La Laguna y Cajamarca se conservan tal como están.\n\n¿Continuar?",
                    "Regenerar",
                    "Cancelar");

                if (!overwrite) return;
            }

            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            if (GameObject.Find(AyniBakedWorld.RootName) == null)
                throw new Exception("Abre o genera primero la escena editable de la laguna.");

            RemoveOldRoot();
            if (AssetDatabase.IsValidFolder(GeneratedFolder))
                AssetDatabase.DeleteAsset(GeneratedFolder);
            EnsureFolder(GeneratedFolder);
            EnsureFolder(GeneratedFolder + "/Meshes");
            EnsureFolder(GeneratedFolder + "/Materials");
            EnsureFolder(GeneratedFolder + "/Textures");

            var root = new GameObject(RootName);
            root.transform.position = new Vector3(GroveX, 0f, 0f);
            root.AddComponent<AyniBakedBosque>();

            var grove = new GameObject(AyniBakedBosque.GroveName);
            grove.transform.SetParent(root.transform, false);

            var host = new GameObject("__BOSQUE_BAKE_HOST__");
            var bosque = host.AddComponent<BosqueCapitulo>();
            SetField(bosque, "grove", grove.transform);
            SetField(bosque, "rng", new System.Random(11));

            Call(bosque, "BuildGrove");

            PersistMeshes(root);
            PersistMaterials(root);

            UnityEngine.Object.DestroyImmediate(host);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log(
                $"[AYNI] Bosque editable generado: {ScenePath}" +
                $" | objetos={root.GetComponentsInChildren<Transform>(true).Length}" +
                $" | mallas={root.GetComponentsInChildren<MeshFilter>(true).Length}" +
                $" | renderers={root.GetComponentsInChildren<Renderer>(true).Length}" +
                $" | arboles={CountByPrefix(grove.transform, "Bosque/tree") + CountByPrefix(grove.transform, "Coniferas/Prefabs/trees")}" +
                $" | cesped={CountNamed(grove.transform, "Bosque/grass") + CountNamed(grove.transform, "Bosque/grass_large") + CountNamed(grove.transform, "Bosque/grass_leafs")}");
        }

        static void RemoveOldRoot()
        {
            var all = UnityEngine.Object.FindObjectsByType<AyniBakedBosque>(
                FindObjectsInactive.Include);
            for (int i = all.Length - 1; i >= 0; i--)
                if (all[i] != null)
                    UnityEngine.Object.DestroyImmediate(all[i].gameObject);
        }

        static AyniBakedBosque FindMarker()
        {
            var all = UnityEngine.Object.FindObjectsByType<AyniBakedBosque>(
                FindObjectsInactive.Include);
            return all.Length > 0 ? all[0] : null;
        }

        static object Call(object target, string method, params object[] args)
        {
            var info = target.GetType().GetMethod(
                method, BindingFlags.Instance | BindingFlags.NonPublic);
            if (info == null)
                throw new MissingMethodException(target.GetType().Name, method);
            return info.Invoke(target, args);
        }

        static void SetField(object target, string field, object value)
        {
            var info = target.GetType().GetField(
                field, BindingFlags.Instance | BindingFlags.NonPublic);
            if (info == null)
                throw new MissingFieldException(target.GetType().Name, field);
            info.SetValue(target, value);
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
            var seenMaterials = new Dictionary<Material, Material>();
            var seenTextures = new Dictionary<Texture, Texture>();
            int materialIndex = 0;
            int textureIndex = 0;

            foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                var materials = renderer.sharedMaterials;
                bool changed = false;

                for (int i = 0; i < materials.Length; i++)
                {
                    var material = materials[i];
                    if (material == null || AssetDatabase.Contains(material)) continue;

                    var texture = material.mainTexture;
                    if (texture != null && !AssetDatabase.Contains(texture))
                    {
                        if (!seenTextures.TryGetValue(texture, out var textureAsset))
                        {
                            string path =
                                $"{GeneratedFolder}/Textures/{Sanitize(texture.name)}_{textureIndex++}.asset";
                            AssetDatabase.CreateAsset(texture, path);
                            textureAsset = AssetDatabase.LoadAssetAtPath<Texture>(path);
                            seenTextures[texture] = textureAsset;
                        }

                        material.mainTexture = textureAsset;
                    }

                    if (!seenMaterials.TryGetValue(material, out var materialAsset))
                    {
                        string path =
                            $"{GeneratedFolder}/Materials/{Sanitize(material.name)}_{materialIndex++}.mat";
                        AssetDatabase.CreateAsset(material, path);
                        materialAsset = AssetDatabase.LoadAssetAtPath<Material>(path);
                        seenMaterials[material] = materialAsset;
                    }

                    materials[i] = materialAsset;
                    changed = true;
                }

                if (changed)
                    renderer.sharedMaterials = materials;
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
    }
}
#endif
