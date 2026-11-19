using AYNI.UI;
using UnityEngine;
using UnityEngine.UI;

namespace AYNI.Battle
{
    /// <summary>
    /// En las calles, la webcam lee dónde está la persona de arriba a abajo.
    /// Arriba del cuadro sube. Abajo baja. El centro deja caminar.
    /// </summary>
    public static class VueloDeCamara
    {
        public static bool Activo { get; private set; }
        public static bool Ve { get; private set; }
        public static float Subir { get; private set; }

        static WebCamTexture ojo;
        static Image marco;
        static RawImage vista;
        static RectTransform marca;
        static float suave = 0.5f;
        static float proxima;

        public static bool Abrir(Transform hud, out string aviso)
        {
            aviso = "";
            var dispositivos = WebCamTexture.devices;
            if (dispositivos == null || dispositivos.Length == 0)
            {
                aviso = "No encuentro una cámara.";
                return false;
            }

            PararOjo();
            ojo = new WebCamTexture(dispositivos[0].name, 160, 120, 12);
            ojo.Play();
            AsegurarVista(hud);
            if (marco != null) marco.gameObject.SetActive(true);
            Activo = true;
            Ve = false;
            Subir = 0f;
            suave = 0.5f;
            proxima = 0f;
            return true;
        }

        public static void Cerrar()
        {
            Activo = false;
            Ve = false;
            Subir = 0f;
            if (marco != null) marco.gameObject.SetActive(false);
            PararOjo();
        }

        public static void Paso()
        {
            if (!Activo || ojo == null) return;
            if (vista != null)
            {
                vista.texture = ojo;
                PintarUv();
            }

            if (!ojo.isPlaying || !ojo.didUpdateThisFrame || ojo.width < 8) return;
            if (Time.unscaledTime < proxima) return;
            proxima = Time.unscaledTime + 0.08f;
            Medir();
        }

        static void PararOjo()
        {
            if (ojo == null) return;
            if (vista != null) vista.texture = null;
            ojo.Stop();
            Object.Destroy(ojo);
            ojo = null;
        }

        static void AsegurarVista(Transform hud)
        {
            if (marco != null || hud == null) return;
            marco = Storybook.Plate(hud, "Ojo",
                new Vector2(0.76f, 0.50f), new Vector2(0.88f, 0.66f),
                new Color(0.08f, 0.05f, 0.04f, 0.92f));
            marco.raycastTarget = false;

            var go = new GameObject("Imagen");
            go.transform.SetParent(marco.transform, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.06f, 0.08f);
            rect.anchorMax = new Vector2(0.94f, 0.92f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            vista = go.AddComponent<RawImage>();
            vista.raycastTarget = false;

            var linea = new GameObject("Altura");
            linea.transform.SetParent(go.transform, false);
            marca = linea.AddComponent<RectTransform>();
            marca.anchorMin = new Vector2(0f, 0.5f);
            marca.anchorMax = new Vector2(1f, 0.53f);
            marca.offsetMin = Vector2.zero;
            marca.offsetMax = Vector2.zero;
            var tinta = linea.AddComponent<Image>();
            tinta.color = new Color(0.95f, 0.86f, 0.35f, 0.95f);
            tinta.raycastTarget = false;
            marco.gameObject.SetActive(false);
        }

        static void PintarUv()
        {
            if (vista == null || ojo == null) return;
            bool abajo = ojo.videoVerticallyMirrored;
            int ang = ojo.videoRotationAngle % 360;
            if (ang < 0) ang += 360;
            if (ang == 180) abajo = !abajo;
            float uy = abajo ? 1f : 0f;
            float uh = abajo ? -1f : 1f;
            vista.uvRect = new Rect(1f, uy, -1f, uh);
        }

        static void Medir()
        {
            int w = ojo.width;
            int h = ojo.height;
            var pix = ojo.GetPixels32();
            if (pix == null || pix.Length < w * h) return;

            int ang = ojo.videoRotationAngle % 360;
            if (ang < 0) ang += 360;
            bool vflip = ojo.videoVerticallyMirrored;
            if (ang == 180) vflip = !vflip;
            bool deLado = ang == 90 || ang == 270;

            long suma = 0;
            int cuenta = 0;
            int min = deLado ? w : h;
            int max = 0;
            int limite = deLado ? w : h;
            for (int y = 0; y < h; y++)
            {
                int row = y * w;
                for (int x = 0; x < w; x++)
                {
                    if (!Piel(pix[row + x])) continue;
                    int eje = deLado ? x : y;
                    cuenta++;
                    suma += eje;
                    if (eje < min) min = eje;
                    if (eje > max) max = eje;
                }
            }

            float tex = 0.5f;
            bool ve = false;
            if (cuenta > w * h / 50 && max - min > limite * 0.12f)
            {
                tex = (float)suma / cuenta / limite;
                ve = true;
            }
            else if (Contraste(pix, w, h, deLado, out float centro))
            {
                tex = centro;
                ve = true;
            }

            float arriba = tex;
            if (deLado && ang == 270) arriba = 1f - tex;
            else if (!deLado) arriba = vflip ? 1f - tex : tex;

            if (!ve) arriba = suave;
            suave = Mathf.Lerp(suave, arriba, ve ? 0.45f : 0.2f);
            Ve = ve;
            float banda = 0.12f;
            float d = suave - 0.5f;
            float objetivo = Mathf.Abs(d) < banda
                ? 0f
                : Mathf.Clamp((d - Mathf.Sign(d) * banda) / (0.5f - banda), -1f, 1f);
            Subir = Mathf.Lerp(Subir, ve ? objetivo : 0f, 0.55f);
            if (marca != null)
            {
                float y = Mathf.Clamp01(suave);
                marca.anchorMin = new Vector2(0f, y);
                marca.anchorMax = new Vector2(1f, Mathf.Min(1f, y + 0.035f));
            }
        }

        static bool Contraste(Color32[] pix, int w, int h, bool deLado, out float centro)
        {
            centro = 0.5f;
            int largo = deLado ? w : h;
            int cruce = deLado ? h : w;
            int third = Mathf.Max(1, cruce / 3);
            float sum = 0f;
            float peso = 0f;
            int filas = 0;
            int y0 = largo;
            int y1 = 0;
            for (int eje = 0; eje < largo; eje++)
            {
                int centroL = 0;
                int ladoL = 0;
                int nc = 0;
                int nl = 0;
                for (int cruza = 0; cruza < cruce; cruza++)
                {
                    int x = deLado ? eje : cruza;
                    int y = deLado ? cruza : eje;
                    var c = pix[y * w + x];
                    int luma = (c.r * 3 + c.g * 4 + c.b) >> 3;
                    if (cruza >= third && cruza < cruce - third)
                    {
                        centroL += luma;
                        nc++;
                    }
                    else
                    {
                        ladoL += luma;
                        nl++;
                    }
                }

                if (nc < 1 || nl < 1) continue;
                int diff = Mathf.Abs(centroL / nc - ladoL / nl);
                if (diff < 16) continue;
                filas++;
                if (eje < y0) y0 = eje;
                if (eje > y1) y1 = eje;
                sum += eje * diff;
                peso += diff;
            }

            if (filas <= largo / 10 || y1 - y0 <= largo * 0.12f || peso < 1f) return false;
            centro = (sum / peso) / largo;
            return true;
        }

        static bool Piel(Color32 c)
        {
            int r = c.r;
            int g = c.g;
            int b = c.b;
            int max = r > g ? (r > b ? r : b) : (g > b ? g : b);
            int min = r < g ? (r < b ? r : b) : (g < b ? g : b);
            if (max < 48 || max - min < 12) return false;
            if (r + 10 < g || r + 6 < b) return false;
            return true;
        }
    }
}
