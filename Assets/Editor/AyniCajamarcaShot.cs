#if UNITY_EDITOR
using System.IO;
using AYNI.Battle;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace AYNI.Editor
{
    /// <summary>Sonda temporal: renders de control de Cajamarca.</summary>
    public static class AyniCajamarcaShot
    {
        const string ScenePath = "Assets/Scenes/AYNI_LagunaPelea_EDITABLE.unity";
        const int W = 1600, H = 900;

        [MenuItem("AYNI/Depurar/Renders de Cajamarca")]
        public static void Run()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath);
            var root = GameObject.Find(AyniBakedCajamarca.RootName);
            if (root == null) { Debug.LogError("[SHOT] sin root"); return; }

            string dir = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Logs");
            float ox = root.transform.position.x;

            Shot("cajamarca-aerea.png", dir,
                new Vector3(ox + 620f, 780f, -780f), new Vector3(ox, 0f, 0f), 45f);
            Shot("cajamarca-plaza.png", dir,
                new Vector3(ox - 150f, 55f, -170f), new Vector3(ox, 0f, -10f), 50f);
            Shot("cajamarca-pardo.png", dir,
                new Vector3(ox + 4f, 14f, -190f), new Vector3(ox, 2f, 20f), 55f);
            Shot("cajamarca-peru.png", dir,
                new Vector3(ox - 240f, 78f, -620f), new Vector3(ox - 120f, 20f, -400f), 60f);
            Shot("cajamarca-calles.png", dir,
                new Vector3(ox + 260f, 320f, 260f), new Vector3(ox, 0f, 0f), 55f);

            EditorSceneManager.CloseScene(scene, true);
        }

        static void Shot(string file, string dir, Vector3 pos, Vector3 target, float fov)
        {
            var go = new GameObject("ShotCam");
            var cam = go.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.62f, 0.75f, 0.88f);
            cam.fieldOfView = fov;
            cam.nearClipPlane = 0.3f;
            cam.farClipPlane = 4200f;
            go.transform.position = pos;
            go.transform.LookAt(target);

            var rt = new RenderTexture(W, H, 24, RenderTextureFormat.ARGB32);
            cam.targetTexture = rt;
            cam.Render();
            RenderTexture.active = rt;

            var tex = new Texture2D(W, H, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, W, H), 0, 0);
            tex.Apply();
            File.WriteAllBytes(Path.Combine(dir, file), tex.EncodeToPNG());

            RenderTexture.active = null;
            cam.targetTexture = null;
            Object.DestroyImmediate(rt);
            Object.DestroyImmediate(tex);
            Object.DestroyImmediate(go);
            Debug.Log("[SHOT] " + file);
        }
    }
}
#endif
