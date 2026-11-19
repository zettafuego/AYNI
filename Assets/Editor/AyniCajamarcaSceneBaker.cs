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
    /// Convierte la ciudad procedural de Cajamarca en un distrito real
    /// dentro de la escena editable de la laguna.
    /// </summary>
    public static class AyniCajamarcaSceneBaker
    {
        const string ScenePath = "Assets/Scenes/AYNI_LagunaPelea_EDITABLE.unity";
        const string GeneratedFolder = "Assets/Generated/Cajamarca";
        const string RootName = AyniBakedCajamarca.RootName;
        static bool skipConfirm;

        [MenuItem("AYNI/Escenas/Generar Cajamarca editable", priority = 20)]
        public static void BakeCajamarca()
        {
            try
            {
                BakeInternal();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                Debug.LogError("[AYNI] No se pudo generar Cajamarca editable.");
                if (Application.isBatchMode) EditorApplication.Exit(1);
            }
        }

        /// <summary>
        /// Regenera el distrito sin el cuadro de confirmación.
        /// El usuario ya pidió rehacer __CAJAMARCA_EDITABLE__.
        /// </summary>
        public static void BakeCajamarcaForced()
        {
            skipConfirm = true;
            try
            {
                BakeCajamarca();
            }
            finally
            {
                skipConfirm = false;
            }
        }

        [MenuItem("AYNI/Escenas/Enfocar Cajamarca en la escena", priority = 21)]
        public static void FocusCajamarca()
        {
            var marker = FindMarker();
            if (marker == null)
            {
                EditorUtility.DisplayDialog(
                    "AYNI",
                    "Todavía no existe Cajamarca editable.\n\n" +
                    "Usa primero: AYNI > Escenas > Generar Cajamarca editable.",
                    "OK");
                return;
            }

            marker.gameObject.SetActive(true);
            Selection.activeGameObject = marker.gameObject;
            var district = marker.transform.Find(AyniBakedCajamarca.DistrictName);
            // La ruta va de la plaza al mirador de Santa Apolonia
            // (184 m al oeste, 386 m al sur). El relieve de CALLES exagera
            // esos 76 m por RelieveCalles, hasta unos 121 m. El encuadre
            // se sienta a mitad de esa subida.
            Vector3 pivot = marker.transform.position + new Vector3(-92f, 58f, -190f);
            var view = SceneView.lastActiveSceneView;
            if (view == null && SceneView.sceneViews.Count > 0)
                view = SceneView.sceneViews[0] as SceneView;
            if (view == null) return;
            view.LookAt(pivot, Quaternion.Euler(24f, 16f, 0f), 300f, false, true);
            view.Repaint();
        }

        [MenuItem("AYNI/Escenas/Validar Cajamarca editable", priority = 22)]
        public static void ValidateCajamarca()
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

            var district = root.transform.Find(AyniBakedCajamarca.DistrictName);
            var plan = district != null ? district.Find(AyniBakedCajamarca.PlanName) : null;
            Transform fountain = district != null ? district.Find("Fuente") : null;
            Transform crate = district != null ? district.Find("CajaMadera") : null;
            Transform lawn = district != null ? district.Find("Cesped") : null;
            Transform paths = district != null ? district.Find("Senderos") : null;

            Debug.Log(
                $"[AYNI] Validación Cajamarca EDITABLE | escena={scene.name}" +
                $" | objetos={root.GetComponentsInChildren<Transform>(true).Length}" +
                $" | mallas={root.GetComponentsInChildren<MeshFilter>(true).Length}" +
                $" | renderers={root.GetComponentsInChildren<Renderer>(true).Length}" +
                $" | distrito={district != null} | plano={plan != null}" +
                $" | fuente={fountain != null} | caja={crate != null}" +
                $" | cesped={lawn != null} | senderos={paths != null}" +
                $" | mallasSinRef={meshesMissing} | materialesSinRef={materialsMissing}");
        }

        public static void BakeCajamarcaBatch()
        {
            BakeCajamarca();
        }

        static void BakeInternal()
        {
            if (!File.Exists(ScenePath))
                throw new FileNotFoundException("No existe la escena editable: " + ScenePath);

            if (skipConfirm || Application.isBatchMode)
                EditorSceneManager.SaveOpenScenes();
            else if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            if (AssetDatabase.IsValidFolder(GeneratedFolder) && !Application.isBatchMode && !skipConfirm)
            {
                bool overwrite = EditorUtility.DisplayDialog(
                    "AYNI — Regenerar Cajamarca",
                    "Esto reemplazará el distrito de Cajamarca actual.\n\n" +
                    "Se perderán los cambios manuales hechos dentro de " + RootName +
                    " y se recreará la carpeta Assets/Generated/Cajamarca.\n\n" +
                    "La Laguna (" + AyniBakedWorld.RootName + ") se conserva " +
                    "tal como está.\n\n¿Continuar?",
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
            root.transform.position = new Vector3(2200f, 0f, 0f);
            root.AddComponent<AyniBakedCajamarca>();

            var district = new GameObject(AyniBakedCajamarca.DistrictName);
            district.transform.SetParent(root.transform, false);

            var host = new GameObject("__CAJAMARCA_BAKE_HOST__");
            var city = host.AddComponent<CajamarcaCalles>();
            SetField(city, "district", district.transform);

            var shader = Shader.Find("AYNI/Calle")
                         ?? Shader.Find("Standard")
                         ?? Shader.Find("Sprites/Default");

            // Mapa recortado: plaza → Santa Apolonia. Solo terreno, rutas y
            // casas sueltas. No se instancia el FBX ni la fuente ni el césped.
            var heights = AyniCajamarcaDem.Build();
            var plan = CajamarcaRutaApolonia.Build(district.transform, heights, shader);
            SetField(city, "plan", plan);
            SetField(city, "blockRadius", 0f);
            Call(city, "BindPlanDoor");
            Call(city, "BakeWalk");
            bool plazaOk = (bool)Call(city, "FindMainPlaza");
            bool walkReady = (bool)GetField(city, "walkReady");
            if (!plazaOk || !walkReady)
                throw new Exception(
                    "La ruta no quedó caminable. plaza=" + plazaOk + " calles=" + walkReady);

            PersistGenerated(root);

            UnityEngine.Object.DestroyImmediate(host);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            CajamarcaRutaApolonia.Capture(root.transform);
            if (!Application.isBatchMode) FocusCajamarca();

            Debug.Log(
                $"[AYNI] Cajamarca editable generada: {ScenePath}" +
                $" | objetos={root.GetComponentsInChildren<Transform>(true).Length}" +
                $" | mallas={root.GetComponentsInChildren<MeshFilter>(true).Length}" +
                $" | renderers={root.GetComponentsInChildren<Renderer>(true).Length}" +
                $" | plan={plan.name} | {CajamarcaRutaApolonia.LastReport}");
        }

        static void RemoveOldRoot()
        {
            var all = UnityEngine.Object.FindObjectsByType<AyniBakedCajamarca>(
                FindObjectsInactive.Include);
            for (int i = all.Length - 1; i >= 0; i--)
                if (all[i] != null)
                    UnityEngine.Object.DestroyImmediate(all[i].gameObject);
        }

        static AyniBakedCajamarca FindMarker()
        {
            var all = UnityEngine.Object.FindObjectsByType<AyniBakedCajamarca>(
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

        public static void PersistGenerated(GameObject root)
        {
            if (root == null) return;
            EnsureFolder(GeneratedFolder);
            EnsureFolder(GeneratedFolder + "/Meshes");
            EnsureFolder(GeneratedFolder + "/Materials");
            EnsureFolder(GeneratedFolder + "/Textures");
            PersistMeshes(root);
            PersistMaterials(root);
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
