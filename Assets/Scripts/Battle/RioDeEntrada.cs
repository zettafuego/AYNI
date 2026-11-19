using UnityEngine;

namespace AYNI.Battle
{
    /// <summary>
    /// The cut in the mountain path. Jump it. Falling in returns you to the near bank.
    /// </summary>
    public class RioDeEntrada : MonoBehaviour
    {
        public const float Left = -12.4f;
        public const float Right = -6.8f;
        public const float ReturnX = -15.6f;
        public const float SpawnX = -16.5f;
        public const float Pit = 6f;
        public const float FarBank = 2.2f;

        public static RioDeEntrada Instance { get; private set; }

        public static bool Over(float x) => x > Left + 0.15f && x < Right - 0.15f;

        public static bool Blocks(float x, float width)
        {
            float half = width * 0.5f;
            return x + half > Left - 0.2f && x - half < Right + 0.2f;
        }

        public static bool Swallows(float x, float y, float floorY) => Over(x) && y < floorY - 1.35f;

        public static RioDeEntrada Ensure(Transform parent, float floorY)
        {
            if (Instance != null) return Instance;
            var go = new GameObject("RioDeEntrada");
            go.transform.SetParent(parent, false);
            var river = go.AddComponent<RioDeEntrada>();
            river.Paint(floorY);
            return river;
        }

        void Awake() => Instance = this;

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        void Paint(float floorY)
        {
            if (PintarFoto(floorY)) return;
            const int w = 180;
            const int h = 72;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;
            for (int y = 0; y < h; y++)
            {
                float fromTop = 1f - y / (float)(h - 1);
                for (int x = 0; x < w; x++)
                {
                    float wave = Mathf.Sin(x * 0.33f + fromTop * 5f) * 0.5f + 0.5f;
                    var deep = new Color(0.03f, 0.10f, 0.28f, 1f);
                    var mid = new Color(0.08f, 0.24f, 0.55f, 1f);
                    var foam = new Color(0.78f, 0.88f, 0.96f, 1f);
                    var c = Color.Lerp(deep, mid, wave * (1f - fromTop * 0.65f));
                    float crest = 0.07f + wave * 0.045f;
                    if (fromTop < crest) c = Color.Lerp(mid, foam, 1f - fromTop / crest);
                    c.a = Mathf.Lerp(0.72f, 0.94f, 1f - fromTop);
                    tex.SetPixel(x, y, c);
                }
            }
            tex.Apply();

            float span = Right - Left;
            var sprite = Sprite.Create(tex, new Rect(0f, 0f, w, h), new Vector2(0.5f, 1f), w / span);
            var agua = new GameObject("Agua");
            agua.transform.SetParent(transform, false);
            agua.transform.position = new Vector3((Left + Right) * 0.5f, floorY + 0.04f, 0f);
            var ink = agua.AddComponent<SpriteRenderer>();
            ink.sprite = sprite;
            ink.sortingOrder = 6;
        }

        /// <summary>
        /// Side view of the gap. One horizontal band of the seamless tile, so the
        /// pixels stay square across the cut.
        /// </summary>
        bool PintarFoto(float floorY)
        {
            var foto = Resources.Load<Texture2D>("Agua/rio");
            if (foto == null || foto.width < 8 || foto.height < 8) return false;
            foto.wrapMode = TextureWrapMode.Repeat;

            float span = Right - Left;
            const float alto = 1.35f;
            float band = Mathf.Clamp(foto.width * (alto / span), 8f, foto.height);
            var rect = new Rect(0f, (foto.height - band) * 0.5f, foto.width, band);
            var sprite = Sprite.Create(foto, rect, new Vector2(0.5f, 1f), foto.width / span, 0, SpriteMeshType.FullRect);
            var agua = new GameObject("Agua");
            agua.transform.SetParent(transform, false);
            agua.transform.position = new Vector3((Left + Right) * 0.5f, floorY + 0.04f, 0f);
            var ink = agua.AddComponent<SpriteRenderer>();
            ink.sprite = sprite;
            ink.color = new Color(0.82f, 0.95f, 1f, 0.95f);
            ink.sortingOrder = 6;
            return true;
        }
    }

    /// <summary>
    /// Laguna El Perol de noche: cielo con luna, loma verde, peña oscura,
    /// agua honda y la columna de luz. No usa la foto; la sigue.
    /// </summary>
    public static class LagunaDelPerol
    {
        public static void Pintar(Transform parent, float floorY, float minX, float maxX)
        {
            if (parent == null || parent.Find("LagunaElPerol") != null) return;

            const int w = 960;
            const int h = 220;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;
            var pix = new Color32[w * h];
            for (int y = 0; y < h; y++)
            {
                float v = y / (float)(h - 1);
                for (int x = 0; x < w; x++)
                {
                    float u = x / (float)(w - 1);
                    pix[y * w + x] = Tinta(u, v, x, y);
                }
            }

            tex.SetPixels32(pix);
            tex.Apply(false);

            float span = Mathf.Max(24f, maxX - minX + 8f);
            var sprite = Sprite.Create(tex, new Rect(0f, 0f, w, h), new Vector2(0.5f, 0.1f), w / span);
            var go = new GameObject("LagunaElPerol");
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3((minX + maxX) * 0.5f, floorY, 0f);
            var ink = go.AddComponent<SpriteRenderer>();
            ink.sprite = sprite;
            ink.sortingOrder = 7;
        }

        static Color32 Tinta(float u, float v, int x, int y)
        {
            float cerro = u < 0.58f
                ? 0.72f - u * 0.28f + 0.035f * Mathf.Sin(u * 36f)
                : 0.50f + 0.22f * Mathf.Abs(Mathf.Sin((u - 0.55f) * 8.5f));
            float orilla = 0.16f + 0.015f * Mathf.Sin(u * 18f);
            float agua = 0.44f + 0.03f * Mathf.Sin(u * 5f);

            if (v < orilla)
            {
                bool mata = ((x * 17 + y * 3) % 23) < 6;
                if (mata) return new Color32(28, 48, 24, 255);
                byte g = (byte)(92 + ((x * 5 + y * 9) % 28));
                return new Color32((byte)(g - 12), g, (byte)(g - 20), 255);
            }

            if (v < agua)
            {
                float deep = Mathf.InverseLerp(orilla, agua, v);
                var fondo = Color.Lerp(new Color(0.02f, 0.08f, 0.22f), new Color(0.06f, 0.18f, 0.48f), deep);
                float columna = Mathf.Abs(u - 0.63f);
                if (columna < 0.018f)
                {
                    float brillo = (1f - columna / 0.018f) * (0.45f + 0.55f * Mathf.Sin(v * 70f));
                    fondo = Color.Lerp(fondo, new Color(0.85f, 0.92f, 0.98f), brillo);
                }
                else if (((x * 13 + y * 7) % 47) == 0)
                    fondo = Color.Lerp(fondo, new Color(0.75f, 0.85f, 0.95f), 0.35f);
                return fondo;
            }

            if (v < cerro)
            {
                bool pena = u > 0.58f;
                if (pena)
                {
                    float n = 0.35f + 0.25f * Mathf.Sin(u * 50f + v * 20f);
                    return Color.Lerp(new Color(0.10f, 0.14f, 0.12f), new Color(0.22f, 0.28f, 0.18f), n);
                }

                float pasto = 0.45f + 0.35f * Mathf.Sin(u * 28f + v * 12f);
                return Color.Lerp(new Color(0.12f, 0.32f, 0.14f), new Color(0.28f, 0.55f, 0.18f), pasto);
            }

            var cielo = Color.Lerp(new Color(0.05f, 0.12f, 0.32f), new Color(0.02f, 0.05f, 0.16f), Mathf.InverseLerp(cerro, 1f, v));
            float dx = u - 0.63f;
            float dy = v - 0.84f;
            float luna = Mathf.Sqrt(dx * dx + dy * dy * 2.2f);
            if (luna < 0.045f)
                cielo = Color.Lerp(new Color(1f, 0.96f, 0.82f), cielo, luna / 0.045f);
            else if (luna < 0.09f)
                cielo = Color.Lerp(new Color(0.95f, 0.92f, 0.7f), cielo, (luna - 0.045f) / 0.045f);
            else if (((x * 29 + y * 17) % 173) == 0)
                cielo = Color.Lerp(cielo, Color.white, 0.85f);
            return cielo;
        }
    }
}
