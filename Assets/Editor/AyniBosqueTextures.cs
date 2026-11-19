#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace AYNI.Editor
{
    /// <summary>
    /// Pinta las texturas del piso del bosque (césped, tierra, barro y piedra)
    /// y las guarda como PNG en Assets/Resources/Bosque, para que el suelo
    /// deje de ser color plano y se pueda repintar desde el editor.
    /// </summary>
    public static class AyniBosqueTextures
    {
        const string Folder = "Assets/Resources/Bosque";
        const int Size = 512;

        [MenuItem("AYNI/Escenas/Generar texturas del bosque", priority = 25)]
        public static void Generate()
        {
            try
            {
                Directory.CreateDirectory(Folder);

                Save("piso_cesped", Grass());
                Save("piso_tierra", Dirt());
                Save("piso_barro", Mud());
                Save("piso_piedra", Rock());

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Debug.Log("[AYNI] Texturas del bosque generadas en " + Folder);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                Debug.LogError("[AYNI] No se pudieron generar las texturas del bosque.");
                if (Application.isBatchMode) EditorApplication.Exit(1);
            }
        }

        public static void GenerateBatch()
        {
            Generate();
        }

        static Texture2D Grass()
        {
            var tex = Base(
                1101,
                new Color(0.15f, 0.26f, 0.10f),
                new Color(0.25f, 0.41f, 0.16f),
                new Color(0.38f, 0.56f, 0.23f));

            var rng = new System.Random(1103);
            var px = tex.GetPixels();
            int blades = 2600;
            for (int i = 0; i < blades; i++)
            {
                int x = rng.Next(Size);
                int y = rng.Next(Size);
                int len = 3 + rng.Next(6);
                float drift = (float)rng.NextDouble() * 1.4f - 0.7f;
                float alpha = 0.16f + (float)rng.NextDouble() * 0.30f;
                var tip = Color.Lerp(
                    new Color(0.34f, 0.55f, 0.20f),
                    new Color(0.66f, 0.78f, 0.34f),
                    (float)rng.NextDouble());
                for (int s = 0; s < len; s++)
                {
                    var shade = Color.Lerp(tip * 0.75f, tip, s / (float)len);
                    Paint(px, x + Mathf.RoundToInt(drift * s * 0.45f), y + s, shade, alpha);
                }
            }

            // Tréboles y florecillas mínimas para romper la repetición.
            for (int i = 0; i < 220; i++)
            {
                int x = rng.Next(Size);
                int y = rng.Next(Size);
                var dot = rng.Next(4) == 0
                    ? new Color(0.78f, 0.82f, 0.46f)
                    : new Color(0.30f, 0.46f, 0.18f);
                Paint(px, x, y, dot, 0.25f + (float)rng.NextDouble() * 0.2f);
            }

            tex.SetPixels(px);
            tex.Apply(true, false);
            return tex;
        }

        static Texture2D Dirt()
        {
            var tex = Base(
                1201,
                new Color(0.27f, 0.18f, 0.09f),
                new Color(0.42f, 0.30f, 0.16f),
                new Color(0.56f, 0.43f, 0.25f));

            var rng = new System.Random(1203);
            var px = tex.GetPixels();

            // Motas de tierra y piedritas.
            for (int i = 0; i < 1500; i++)
            {
                int x = rng.Next(Size);
                int y = rng.Next(Size);
                int r = rng.Next(10) == 0 ? 2 : 1;
                var color = rng.Next(3) == 0
                    ? new Color(0.62f, 0.56f, 0.44f)
                    : new Color(0.33f, 0.23f, 0.12f);
                float alpha = 0.18f + (float)rng.NextDouble() * 0.25f;
                for (int dy = -r; dy <= r; dy++)
                    for (int dx = -r; dx <= r; dx++)
                        if (dx * dx + dy * dy <= r * r)
                            Paint(px, x + dx, y + dy, color, alpha);
            }

            // Briznas secas.
            for (int i = 0; i < 420; i++)
            {
                int x = rng.Next(Size);
                int y = rng.Next(Size);
                int len = 2 + rng.Next(4);
                var dry = new Color(0.55f, 0.50f, 0.28f);
                for (int s = 0; s < len; s++)
                    Paint(px, x + Mathf.RoundToInt(s * 0.4f), y + s, dry, 0.16f);
            }

            tex.SetPixels(px);
            tex.Apply(true, false);
            return tex;
        }

        static Texture2D Mud()
        {
            var tex = Base(
                1301,
                new Color(0.19f, 0.13f, 0.07f),
                new Color(0.32f, 0.22f, 0.12f),
                new Color(0.45f, 0.33f, 0.18f));

            var rng = new System.Random(1303);
            var px = tex.GetPixels();

            // Charquitos oscuros.
            for (int i = 0; i < 260; i++)
            {
                int x = rng.Next(Size);
                int y = rng.Next(Size);
                int r = 2 + rng.Next(6);
                float alpha = 0.10f + (float)rng.NextDouble() * 0.16f;
                for (int dy = -r; dy <= r; dy++)
                {
                    for (int dx = -r; dx <= r; dx++)
                    {
                        float d = (dx * dx + dy * dy) / (float)(r * r);
                        if (d > 1f) continue;
                        Paint(px, x + dx, y + dy, new Color(0.14f, 0.10f, 0.06f), alpha * (1f - d));
                    }
                }
            }

            for (int i = 0; i < 700; i++)
            {
                int x = rng.Next(Size);
                int y = rng.Next(Size);
                var stone = new Color(0.48f, 0.45f, 0.36f);
                Paint(px, x, y, stone, 0.2f + (float)rng.NextDouble() * 0.2f);
            }

            tex.SetPixels(px);
            tex.Apply(true, false);
            return tex;
        }

        static Texture2D Rock()
        {
            var tex = Base(
                1401,
                new Color(0.28f, 0.27f, 0.24f),
                new Color(0.47f, 0.46f, 0.41f),
                new Color(0.63f, 0.62f, 0.55f));

            var rng = new System.Random(1403);
            var px = tex.GetPixels();

            // Grietas cortas.
            for (int i = 0; i < 320; i++)
            {
                int x = rng.Next(Size);
                int y = rng.Next(Size);
                int len = 3 + rng.Next(7);
                float dx = (float)rng.NextDouble() * 1.2f - 0.6f;
                for (int s = 0; s < len; s++)
                    Paint(px, x + Mathf.RoundToInt(dx * s), y + s,
                        new Color(0.22f, 0.21f, 0.19f), 0.25f);
            }

            for (int i = 0; i < 900; i++)
            {
                int x = rng.Next(Size);
                int y = rng.Next(Size);
                var grit = rng.Next(2) == 0
                    ? new Color(0.70f, 0.68f, 0.60f)
                    : new Color(0.34f, 0.33f, 0.29f);
                Paint(px, x, y, grit, 0.16f + (float)rng.NextDouble() * 0.2f);
            }

            tex.SetPixels(px);
            tex.Apply(true, false);
            return tex;
        }

        static Texture2D Base(int seed, Color dark, Color mid, Color light)
        {
            var tex = new Texture2D(Size, Size, TextureFormat.RGBA32, true)
            {
                name = "piso_bosque",
                wrapMode = TextureWrapMode.Repeat,
                filterMode = FilterMode.Bilinear,
                anisoLevel = 4,
            };

            var px = new Color[Size * Size];
            for (int y = 0; y < Size; y++)
            {
                float v = y / (float)Size;
                for (int x = 0; x < Size; x++)
                {
                    float u = x / (float)Size;
                    float n = Octaves(u, v, seed);
                    px[y * Size + x] = n < 0.5f
                        ? Color.Lerp(dark, mid, n * 2f)
                        : Color.Lerp(mid, light, (n - 0.5f) * 2f);
                }
            }

            tex.SetPixels(px);
            tex.Apply(true, false);
            return tex;
        }

        static float Octaves(float u, float v, int seed)
        {
            float n = 0f;
            n += Tile(u, v, 2, seed + 1) * 0.12f;
            n += Tile(u, v, 4, seed + 2) * 0.22f;
            n += Tile(u, v, 8, seed + 3) * 0.28f;
            n += Tile(u, v, 16, seed + 4) * 0.22f;
            n += Tile(u, v, 32, seed + 5) * 0.16f;
            return Mathf.Clamp01(n);
        }

        static float Tile(float u, float v, int period, int seed)
        {
            float x = u * period;
            float y = v * period;
            int x0 = Mathf.FloorToInt(x);
            int y0 = Mathf.FloorToInt(y);
            float fx = x - x0;
            float fy = y - y0;
            float sx = fx * fx * (3f - 2f * fx);
            float sy = fy * fy * (3f - 2f * fy);

            int xa = Wrap(x0, period), xb = Wrap(x0 + 1, period);
            int ya = Wrap(y0, period), yb = Wrap(y0 + 1, period);

            float a = Hash(xa, ya, seed);
            float b = Hash(xb, ya, seed);
            float c = Hash(xa, yb, seed);
            float d = Hash(xb, yb, seed);
            return Mathf.Lerp(Mathf.Lerp(a, b, sx), Mathf.Lerp(c, d, sx), sy);
        }

        static int Wrap(int value, int period)
        {
            int r = value % period;
            return r < 0 ? r + period : r;
        }

        static float Hash(int x, int y, int seed)
        {
            unchecked
            {
                int h = x * 374761393 + y * 668265263 + seed * 1274126177;
                h = (h ^ (h >> 13)) * 1274126177;
                h ^= h >> 16;
                return (h & 0x7fffffff) / (float)int.MaxValue;
            }
        }

        static void Paint(Color[] pixels, int x, int y, Color color, float alpha)
        {
            x = Wrap(x, Size);
            y = Wrap(y, Size);
            int i = y * Size + x;
            pixels[i] = Color.Lerp(pixels[i], color, Mathf.Clamp01(alpha));
        }

        static void Save(string file, Texture2D tex)
        {
            string path = Folder + "/" + file + ".png";
            File.WriteAllBytes(path, tex.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(tex);

            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) return;

            importer.textureType = TextureImporterType.Default;
            importer.wrapMode = TextureWrapMode.Repeat;
            importer.filterMode = FilterMode.Bilinear;
            importer.mipmapEnabled = true;
            importer.anisoLevel = 4;
            importer.maxTextureSize = Size;
            importer.alphaIsTransparency = false;
            importer.SaveAndReimport();
        }
    }
}
#endif
