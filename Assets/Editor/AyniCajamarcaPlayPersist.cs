#if UNITY_EDITOR
using System.IO;
using AYNI.Battle;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace AYNI.Editor
{
    /// <summary>
    /// Al salir de Play guarda las piezas y las hornea en la escena
    /// para editarlas en el Inspector, sin volver a dar Play.
    /// </summary>
    [InitializeOnLoad]
    public static class AyniCajamarcaPlayPersist
    {
        static AyniCajamarcaPlayPersist()
        {
            EditorApplication.playModeStateChanged += OnPlay;
        }

        static void OnPlay(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingEditMode)
                CajamarcaCalles.RecogerDeLaEscena();
            if (state == PlayModeStateChange.ExitingPlayMode)
                CajamarcaCalles.VolcarPiezas();
            if (state == PlayModeStateChange.EnteredEditMode)
                HornearPiezasEnEscena(false);
        }

        [MenuItem("AYNI/Escenas/Hornear piezas de Cajamarca en la escena", priority = 24)]
        public static void HornearMenu()
        {
            HornearPiezasEnEscena(true);
        }

        public static void HornearPiezasEnEscena(bool avisar)
        {
            if (Application.isPlaying) return;
            var all = Object.FindObjectsByType<AyniBakedCajamarca>(FindObjectsInactive.Include);
            if (all == null || all.Length == 0)
            {
                if (avisar)
                    EditorUtility.DisplayDialog("AYNI",
                        "No hay Cajamarca editable en la escena.\n\nUsa primero: AYNI > Escenas > Generar Cajamarca editable.",
                        "OK");
                return;
            }

            var marker = all[0];
            var district = marker.transform.Find(AyniBakedCajamarca.DistrictName);
            if (district == null) return;

            string json = Path.Combine(Application.dataPath, "Generated/Cajamarca/piezas.json");
            if (!File.Exists(json))
            {
                if (avisar)
                    EditorUtility.DisplayDialog("AYNI",
                        "Todavía no hay piezas guardadas.\nEntrá a las calles, editá el mapa y salí de Play.",
                        "OK");
                return;
            }

            marker.gameObject.SetActive(true);
            var viejo = district.Find("ElementosDelMapa");
            if (viejo != null) Object.DestroyImmediate(viejo.gameObject);
            var plaza = district.Find("PlazaViva");
            if (plaza != null) Object.DestroyImmediate(plaza.gameObject);

            CajamarcaCalles.PonerPiezasEditables(district);
            AyniCajamarcaSceneBaker.PersistGenerated(marker.gameObject);

            var escena = marker.gameObject.scene;
            if (escena.IsValid())
            {
                EditorSceneManager.MarkSceneDirty(escena);
                EditorSceneManager.SaveScene(escena);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[AYNI] Piezas de Cajamarca horneadas en la escena. Ya se pueden mover en el Hierarchy.");
        }
    }
}
#endif
