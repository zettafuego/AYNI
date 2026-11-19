#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using AYNI.Battle;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace AYNI.Editor
{
    /// <summary>
    /// Sonda temporal: comprueba que calles, plazas y aceras quedan por encima
    /// de la malla del terreno (la misma triangulación que BuildTerrain).
    /// </summary>
    public static class AyniCajamarcaProbe
    {
        const string ScenePath = "Assets/Scenes/AYNI_LagunaPelea_EDITABLE.unity";

        [MenuItem("AYNI/Depurar/Comprobar despeje de calles")]
        public static void Run()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath);
            var root = GameObject.Find(AyniBakedCajamarca.RootName);
            if (root == null) { Debug.LogError("[SONDA] sin root"); return; }
            var district = root.transform.Find(AyniBakedCajamarca.DistrictName);
            var plan = district != null ? district.Find(AyniBakedCajamarca.PlanName) : null;
            if (plan == null) { Debug.LogError("[SONDA] sin plano"); return; }

            string data = Path.Combine(
                Directory.GetParent(Application.dataPath).FullName,
                "Assets/Resources/Calle/cajamarca_altura.bytes");
            var field = CajamarcaHeightField.Load(data);
            if (field == null) { Debug.LogError("[SONDA] sin heightfield"); return; }

            var names = new[] { "PlanoCalles", "PlanoBanda", "PlanoPlazas", "PlanoAgua" };
            var sb = new StringBuilder();
            foreach (var name in names)
            {
                var node = plan.Find(name);
                if (node == null) continue;
                var filter = node.GetComponent<MeshFilter>();
                if (filter == null || filter.sharedMesh == null) continue;
                var mesh = filter.sharedMesh;
                var raw = mesh.vertices;
                var normals = mesh.normals;
                var clear = new List<float>(raw.Length);
                float minY = float.MaxValue, maxY = float.MinValue;
                for (int i = 0; i < raw.Length; i++)
                {
                    // Las faldas son verticales: sus vértices inferiores quedan
                    // a propósito bajo el terreno. Solo se mide la superficie.
                    if (normals.Length == raw.Length && Mathf.Abs(normals[i].y) <= 0.5f) continue;
                    Vector3 d = district.InverseTransformPoint(node.TransformPoint(raw[i]));
                    float ground = TerrainAt(field, d.x, d.z);
                    clear.Add(d.y - ground);
                    if (d.y < minY) minY = d.y;
                    if (d.y > maxY) maxY = d.y;
                }

                clear.Sort();
                float p1 = clear[Mathf.Clamp(Mathf.RoundToInt(clear.Count * 0.01f), 0, clear.Count - 1)];
                float p50 = clear[clear.Count / 2];
                sb.AppendLine(
                    $"[SONDA] {name,-12} sup={clear.Count,6} tris={mesh.triangles.Length / 3,6} " +
                    $"y=[{minY,7:F2},{maxY,7:F2}] despeje min={clear[0],6:F2} p1={p1,6:F2} p50={p50,6:F2}");
            }

            Debug.Log(sb.ToString());

            // Terreno: comprobar que su malla es exactamente DEM - CoreDropAt.
            var terreno = district.Find(CajamarcaRelieve.TerrainName);
            if (terreno != null)
            {
                var mesh = terreno.GetComponent<MeshFilter>().sharedMesh;
                var raw = mesh.vertices;
                float maxErr = 0f;
                for (int i = 0; i < raw.Length; i += 7)
                {
                    Vector3 d = district.InverseTransformPoint(terreno.TransformPoint(raw[i]));
                    float expect = field.Sample(d.x, d.z) - CajamarcaRelieve.CoreDropAt(d.x, d.z);
                    maxErr = Mathf.Max(maxErr, Mathf.Abs(d.y - expect));
                }

                Debug.Log($"[SONDA] Terreno verts={raw.Length} error max vs DEM-rebaje={maxErr:F4} m");
            }

            // Muros: la base debe quedar enterrada.
            var muros = plan.Find("PlanoMuros");
            if (muros != null)
            {
                var mesh = muros.GetComponent<MeshFilter>().sharedMesh;
                var raw = mesh.vertices;
                var ys = new List<float>();
                for (int i = 0; i < raw.Length; i++)
                {
                    Vector3 d = district.InverseTransformPoint(muros.TransformPoint(raw[i]));
                    ys.Add(d.y);
                }

                ys.Sort();
                Debug.Log($"[SONDA] PlanoMuros y=[{ys[0]:F2},{ys[ys.Count - 1]:F2}]");
            }

            EditorSceneManager.CloseScene(scene, true);
        }

        /// <summary>Altura exacta de la malla del terreno (misma triangulación).</summary>
        static float TerrainAt(CajamarcaHeightField f, float x, float z)
        {
            float extent = f.Extent, step = f.Step;
            float fx = (x + extent) / step;
            float fz = (z + extent) / step;
            int i0 = Mathf.Clamp((int)Mathf.Floor(fx), 0, f.Count - 2);
            int j0 = Mathf.Clamp((int)Mathf.Floor(fz), 0, f.Count - 2);
            float tx = Mathf.Clamp01(fx - i0);
            float tz = Mathf.Clamp01(fz - j0);

            float a = Grid(f, i0, j0);
            float b = Grid(f, i0 + 1, j0);
            float c = Grid(f, i0, j0 + 1);
            float d = Grid(f, i0 + 1, j0 + 1);
            if (tx + tz <= 1f) return a + (b - a) * tx + (c - a) * tz;
            return d + (c - d) * (1f - tx) + (b - d) * (1f - tz);
        }

        static float Grid(CajamarcaHeightField f, int i, int j)
        {
            float x = -f.Extent + i * f.Step;
            float z = -f.Extent + j * f.Step;
            return f.Sample(x, z) - CajamarcaRelieve.CoreDropAt(x, z);
        }
    }
}
#endif
