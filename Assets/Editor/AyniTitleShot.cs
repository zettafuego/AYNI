#if UNITY_EDITOR
using System;
using System.IO;
using System.Reflection;
using AYNI.Battle;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace AYNI.Editor
{
    /// <summary>Sonda temporal: captura portada, selector de personaje y splash.</summary>
    [InitializeOnLoad]
    public static class AyniTitleShot
    {
        const string ScenePath = "Assets/Scenes/AYNI_LagunaPelea_EDITABLE.unity";
        const string Running = "AYNI.TitleShot.Running";
        const string StageKey = "AYNI.TitleShot.Stage";
        const string WaitKey = "AYNI.TitleShot.Wait";

        static string Dir => Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Logs");

        static AyniTitleShot()
        {
            if (SessionState.GetBool(Running, false))
                EditorApplication.update += Tick;
        }

        public static void Capture()
        {
            SessionState.SetBool(Running, true);
            SessionState.SetInt(StageKey, 0);
            SessionState.SetFloat(WaitKey, 0f);

            var scene = EditorSceneManager.OpenScene(ScenePath);
            Debug.Log("[SHOT] escena abierta " + scene.name);

            EditorApplication.update += Tick;
            EditorApplication.isPlaying = true;
        }

        static void Tick()
        {
            if (!SessionState.GetBool(Running, false)) return;
            if (!EditorApplication.isPlaying) return;

            int stage = SessionState.GetInt(StageKey, 0);
            float wait = SessionState.GetFloat(WaitKey, 0f);
            float t = Time.unscaledTime;

            if (stage == 0 && t > 3.6f)
            {
                FitScale();
                SessionState.SetInt(StageKey, 1);
            }
            else if (stage == 1)
            {
                Shoot("portada");
                SessionState.SetInt(StageKey, 2);
                SessionState.SetFloat(WaitKey, t);
            }
            else if (stage == 2 && t > wait + 0.8f)
            {
                AbrirPersonaje();
                FitScale();
                SessionState.SetInt(StageKey, 3);
                SessionState.SetFloat(WaitKey, t);
            }
            else if (stage == 3 && t > wait + 0.8f)
            {
                Shoot("personaje");
                SessionState.SetInt(StageKey, 4);
                SessionState.SetFloat(WaitKey, t);
            }
            else if (stage == 4 && t > wait + 0.8f)
            {
                MostrarSplash();
                FitScale();
                SessionState.SetInt(StageKey, 5);
                SessionState.SetFloat(WaitKey, t);
            }
            else if (stage == 5 && t > wait + 0.8f)
            {
                Shoot("splash");
                SessionState.SetInt(StageKey, 6);
                SessionState.SetFloat(WaitKey, t);
            }
            else if (stage == 6 && t > wait + 1.0f)
            {
                Finish();
            }
        }

        static void FitScale()
        {
            var canvas = UnityEngine.Object.FindFirstObjectByType<Canvas>();
            if (canvas == null) return;
            var scaler = canvas.GetComponent<CanvasScaler>();
            if (scaler == null) return;
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            scaler.scaleFactor = Mathf.Max(0.1f, Screen.height / 1080f);
            Canvas.ForceUpdateCanvases();
            Debug.Log($"[SHOT] escala forzada screen={Screen.width}x{Screen.height} factor={scaler.scaleFactor:0.000}");
        }

        static void Shoot(string label)
        {
            string path = Path.Combine(Dir, "ui-" + label + ".png");
            ScreenCapture.CaptureScreenshot(path);
            Debug.Log("[SHOT] " + path + " t=" + Time.unscaledTime.ToString("0.0"));
        }

        static void AbrirPersonaje()
        {
            var screens = UnityEngine.Object.FindFirstObjectByType<StorybookScreens>();
            if (screens == null) { Debug.LogError("[SHOT] sin StorybookScreens"); return; }
            var m = typeof(StorybookScreens).GetMethod("PedirPersonaje",
                BindingFlags.Instance | BindingFlags.NonPublic);
            if (m == null) { Debug.LogError("[SHOT] sin PedirPersonaje"); return; }
            m.Invoke(screens, new object[] { (Action)delegate { } });
            Debug.Log("[SHOT] personaje abierto");
        }

        static void MostrarSplash()
        {
            var relato = UnityEngine.Object.FindFirstObjectByType<CaminoRelato>();
            if (relato == null) { Debug.LogError("[SHOT] sin CaminoRelato"); return; }
            var type = typeof(CaminoRelato);
            var field = type.GetField("splash", BindingFlags.Instance | BindingFlags.NonPublic);
            var time = type.GetField("splashTime", BindingFlags.Instance | BindingFlags.NonPublic);
            var go = field != null ? field.GetValue(relato) as GameObject : null;
            if (go == null) { Debug.LogError("[SHOT] sin splash"); return; }
            go.SetActive(true);
            go.transform.SetAsLastSibling();
            if (time != null) time.SetValue(relato, 1.02f);
            Debug.Log("[SHOT] splash mostrado");
        }

        static void Finish()
        {
            SessionState.EraseBool(Running);
            EditorApplication.update -= Tick;
            EditorApplication.isPlaying = false;
            Debug.Log("[SHOT] fin");
            EditorApplication.Exit(0);
        }
    }
}
#endif
