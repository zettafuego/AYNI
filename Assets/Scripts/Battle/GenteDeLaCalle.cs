using System.Collections.Generic;
using UnityEngine;

namespace AYNI.Battle
{
    /// <summary>
    /// Puestos de mercado. El puesto es un billboard: siempre mira a la cámara.
    /// En las calles no hay vendedora: solo caminan la vizcacha, Atoq y el conejo.
    /// </summary>
    public static class GenteDeLaCalle
    {
        /// <summary>Alto del puesto en metros (toldo incluido).</summary>
        const float StallMeters = 2.1f;

        const string StallRoot = "Calle/Puestos/";
        static readonly string[] Kinds = { "frutas", "flores", "comida" };

        /// <summary>
        /// Levanta un puesto en cada punto, sin persona. Los puntos vienen en
        /// coordenadas del distrito; la altura se toma del DEM.
        /// </summary>
        public static void Dress(Transform parent, IList<Vector3> spots)
        {
            if (parent == null || spots == null || spots.Count == 0) return;

            for (int i = 0; i < spots.Count; i++)
            {
                string kind = Kinds[i % Kinds.Length];
                var tex = Resources.Load<Texture2D>(StallRoot + kind);
                if (tex == null) continue;

                Vector3 spot = spots[i];
                spot.y = SueloDeLaCalle.Altura(spot.x, spot.z);

                var stall = new GameObject("Puesto_" + kind);
                stall.transform.SetParent(parent, false);
                stall.transform.localPosition = spot;
                float aspecto = tex.height > 0 ? tex.width / (float)tex.height : 1f;
                float ancho = Mathf.Clamp(StallMeters * aspecto * 0.72f, 0.8f, 2.2f);
                var tope = stall.AddComponent<BoxCollider>();
                tope.center = new Vector3(0f, StallMeters * 0.5f, 0f);
                tope.size = new Vector3(ancho, StallMeters, 0.85f);

                var cara = new GameObject("Cara");
                cara.transform.SetParent(stall.transform, false);
                float native = tex.height / 100f;
                float size = (native > 0.01f ? StallMeters / native : 0.41f) * (0.97f + 0.03f * (i % 3));
                cara.transform.localScale = new Vector3(size, size, 1f);

                var ink = cara.AddComponent<SpriteRenderer>();
                ink.sprite = Sprite.Create(tex, new Rect(0f, 0f, tex.width, tex.height),
                    new Vector2(0.5f, 0f), 100f);
                ink.sortingOrder = 16;
                cara.AddComponent<Billboard>();
            }
        }
    }

    /// <summary>
    /// Sitio vacío de una persona. Se mueve en el editor y Play arranca ahí.
    /// </summary>
    public sealed class MarcaDePuesto : MonoBehaviour
    {
        void OnDrawGizmos()
        {
            Gizmos.color = new Color(0.25f, 0.72f, 0.38f, 0.95f);
            Gizmos.DrawWireSphere(transform.position + Vector3.up * 0.9f, 0.45f);
        }
    }

    /// <summary>Mantiene un plano vertical mirando a la camara, como los personajes.</summary>
    public class Billboard : MonoBehaviour
    {
        void LateUpdate()
        {
            var cam = Camera.main;
            if (cam == null) return;

            if (cam.orthographic)
            {
                transform.rotation = cam.transform.rotation;
                return;
            }

            Vector3 flat = cam.transform.forward;
            flat.y = 0f;
            if (flat.sqrMagnitude < 0.0001f) return;
            transform.rotation = Quaternion.LookRotation(flat, Vector3.up);
        }
    }
}
