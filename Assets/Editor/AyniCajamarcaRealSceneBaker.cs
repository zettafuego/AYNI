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
    /// Genera la copia fidedigna de Cajamarca con topografía real,
    /// Cerro Santa Apolonia, mirador, escalinatas y la paleta violeta en iluminación
    /// con cielo azul andino como un capítulo independiente.
    /// </summary>
    public static class AyniCajamarcaRealSceneBaker
    {
        const string ScenePath = "Assets/Scenes/AYNI_LagunaPelea_EDITABLE.unity";
        const string GeneratedFolder = "Assets/Generated/CajamarcaReal";
        const string RootName = AyniBakedCajamarcaReal.RootName;

        [MenuItem("AYNI/Escenas/Generar Cajamarca Real editable", priority = 30)]
        public static void BakeCajamarcaReal()
        {
            try
            {
                BakeInternal();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                Debug.LogError("[AYNI] No se pudo generar Cajamarca Real editable.");
                if (Application.isBatchMode) EditorApplication.Exit(1);
            }
        }

        [MenuItem("AYNI/Escenas/Enfocar Cajamarca Real en la escena", priority = 31)]
        public static void FocusCajamarcaReal()
        {
            var marker = FindMarker();
            if (marker == null)
            {
                EditorUtility.DisplayDialog(
                    "AYNI",
                    "Todavía no existe Cajamarca Real editable.\n\n" +
                    "Usa primero: AYNI > Escenas > Generar Cajamarca Real editable.",
                    "OK");
                return;
            }

            Selection.activeGameObject = marker.gameObject;
            var district = marker.transform.Find(AyniBakedCajamarcaReal.DistrictName);
            var apolonia = district != null ? district.Find(AyniBakedCajamarcaReal.SantaApoloniaName) : null;
            if (apolonia != null) Selection.activeGameObject = apolonia.gameObject;
            SceneView.lastActiveSceneView?.FrameSelected();
        }

        [MenuItem("AYNI/Escenas/Validar Cajamarca Real editable", priority = 32)]
        public static void ValidateCajamarcaReal()
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

            var district = root.transform.Find(AyniBakedCajamarcaReal.DistrictName);
            var plan = district != null ? district.Find(AyniBakedCajamarcaReal.PlanName) : null;
            var apolonia = district != null ? district.Find(AyniBakedCajamarcaReal.SantaApoloniaName) : null;
            var terreno = district != null ? district.Find(AyniBakedCajamarcaReal.TerrainName) : null;

            Debug.Log(
                $"[AYNI] Validación Cajamarca REAL | escena={scene.name}" +
                $" | objetos={root.GetComponentsInChildren<Transform>(true).Length}" +
                $" | mallas={root.GetComponentsInChildren<MeshFilter>(true).Length}" +
                $" | renderers={root.GetComponentsInChildren<Renderer>(true).Length}" +
                $" | distrito={district != null} | plano={plan != null}" +
                $" | terreno={terreno != null} | apolonia={apolonia != null}" +
                $" | mallasSinRef={meshesMissing} | materialesSinRef={materialsMissing}");
        }

        public static void BakeCajamarcaRealBatch()
        {
            BakeCajamarcaReal();
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
                    "AYNI — Regenerar Cajamarca Real",
                    "Esto reemplazará el distrito de Cajamarca Real actual.\n\n" +
                    "Se recreará la carpeta Assets/Generated/CajamarcaReal.\n\n" +
                    "La Laguna y el primer distrito se conservan tal como están.\n\n¿Continuar?",
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
            // Ubicación aislada (4500m este): no colisiona con Laguna (0), Bosque (-2000) ni Calles (2200)
            root.transform.position = new Vector3(4500f, 0f, 0f);
            root.AddComponent<AyniBakedCajamarcaReal>();

            var district = new GameObject(AyniBakedCajamarcaReal.DistrictName);
            district.transform.SetParent(root.transform, false);

            var host = new GameObject("__CAJAMARCA_REAL_BAKE_HOST__");
            var city = host.AddComponent<CajamarcaCalles>();
            SetField(city, "district", district.transform);

            var realShader = Shader.Find("AYNI/CajamarcaReal")
                             ?? Shader.Find("AYNI/Calle")
                             ?? Shader.Find("Standard");

            Call(city, "PlacePlan", realShader);
            var plan = GetField(city, "plan") as GameObject;
            if (plan == null)
                throw new Exception("No se pudo instanciar Resources/Calle/PlanoCajamarca.");

            // Relieve real con DEM fidedigno y Cerro Santa Apolonia
            var heights = AyniCajamarcaDem.Build();
            CajamarcaRealRelieve.Apply(district.transform, plan, heights, realShader);

            Call(city, "BindPlanDoor");
            Call(city, "BakeWalk");
            Call(city, "FindMainPlaza");
            SetField(city, "blockRadius", 6f);
            Call(city, "BuildPlanFountain", realShader);
            Call(city, "PlaceCrate");
            Call(city, "DressPlaza");

            // Luz direccional violeta/lilas para el distrito
            var sunGo = new GameObject("LuzLilasDistrito");
            sunGo.transform.SetParent(district.transform, false);
            sunGo.transform.localPosition = new Vector3(0f, 150f, 0f);
            sunGo.transform.rotation = Quaternion.Euler(48f, 215f, 0f);
            var sun = sunGo.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.color = CajamarcaRealRelieve.ColorHighlight;
            sun.intensity = 1.35f;
            sun.shadows = LightShadows.None;

            CajamarcaRealRelieve.SnapDistrict(district.transform, plan, heights);

            PersistMeshes(root);
            PersistMaterials(root);

            UnityEngine.Object.DestroyImmediate(host);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log(
                $"[AYNI] Cajamarca REAL editable generada: {ScenePath}" +
                $" | objetos={root.GetComponentsInChildren<Transform>(true).Length}" +
                $" | mallas={root.GetComponentsInChildren<MeshFilter>(true).Length}" +
                $" | renderers={root.GetComponentsInChildren<Renderer>(true).Length}" +
                $" | plan={plan.name}");
        }

        static void RemoveOldRoot()
        {
            var all = UnityEngine.Object.FindObjectsByType<AyniBakedCajamarcaReal>(
                FindObjectsInactive.Include);
            for (int i = all.Length - 1; i >= 0; i--)
                if (all[i] != null)
                    UnityEngine.Object.DestroyImmediate(all[i].gameObject);
        }

        static AyniBakedCajamarcaReal FindMarker()
        {
            var all = UnityEngine.Object.FindObjectsByType<AyniBakedCajamarcaReal>(
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

        static object GetField(object target, string field)
        {
            var info = target.GetType().GetField(
                field, BindingFlags.Instance | BindingFlags.NonPublic);
            if (info == null)
                throw new MissingFieldException(target.GetType().Name, field);
            return info.GetValue(target);
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
                }

                renderer.sharedMaterials = materials;
            }

            AssetDatabase.SaveAssets();
        }

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            string child = Path.GetFileName(path);
            if (!AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, child);
        }

        static string Sanitize(string name)
        {
            if (string.IsNullOrEmpty(name)) return "asset";
            var clean = new char[name.Length];
            for (int i = 0; i < name.Length; i++)
            {
                char c = name[i];
                clean[i] = char.IsLetterOrDigit(c) || c == '_' || c == '-' ? c : '_';
            }

            return new string(clean);
        }
    }
}
#endif
