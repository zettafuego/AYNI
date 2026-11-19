#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using UnityEditor;
using UnityEngine;

namespace AYNI.Editor
{
    /// <summary>
    /// Campo de altura del valle de Cajamarca en coordenadas del plano.
    /// x = este (metros), z = norte (metros), y = altura respecto a la plaza.
    /// </summary>
    public sealed class CajamarcaHeightField
    {
        public const int Version = 1;
        public const int HeaderBytes = 24;
        public const float DefaultExtent = 1600f;
        public const int DefaultCount = 321; // 1600 m a cada lado, paso de 10 m

        public int Count;
        public float Extent;
        public float Step;
        public float Datum;
        public float[] Heights;

        public float Min => MinRelative();
        public float Max => MaxRelative();

        public float MinRelative()
        {
            float value = float.MaxValue;
            for (int i = 0; i < Heights.Length; i++)
                if (Heights[i] < value) value = Heights[i];
            return Heights.Length > 0 ? value : 0f;
        }

        public float MaxRelative()
        {
            float value = float.MinValue;
            for (int i = 0; i < Heights.Length; i++)
                if (Heights[i] > value) value = Heights[i];
            return Heights.Length > 0 ? value : 0f;
        }

        public float Sample(float x, float z)
        {
            if (Heights == null || Count < 2) return 0f;
            float fx = (x + Extent) / Step;
            float fz = (z + Extent) / Step;
            int i0 = Mathf.Clamp(Mathf.FloorToInt(fx), 0, Count - 2);
            int j0 = Mathf.Clamp(Mathf.FloorToInt(fz), 0, Count - 2);
            float tx = Mathf.Clamp01(fx - i0);
            float tz = Mathf.Clamp01(fz - j0);
            int a = j0 * Count + i0;
            int b = a + 1;
            int c = a + Count;
            int d = c + 1;
            float h0 = Mathf.Lerp(Heights[a], Heights[b], tx);
            float h1 = Mathf.Lerp(Heights[c], Heights[d], tx);
            return Mathf.Lerp(h0, h1, tz);
        }

        public void Save(string path)
        {
            string dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) Directory.CreateDirectory(dir);

            using var stream = new FileStream(path, FileMode.Create, FileAccess.Write);
            using var writer = new BinaryWriter(stream);
            writer.Write(new[] { 'A', 'Y', 'N', 'H' });
            writer.Write(Version);
            writer.Write(Count);
            writer.Write(Extent);
            writer.Write(Datum);
            writer.Write(Step);
            for (int i = 0; i < Heights.Length; i++) writer.Write(Heights[i]);
        }

        public static CajamarcaHeightField Load(string path)
        {
            if (!File.Exists(path)) return null;
            using var stream = new FileStream(path, FileMode.Open, FileAccess.Read);
            using var reader = new BinaryReader(stream);
            if (reader.ReadByte() != 'A' || reader.ReadByte() != 'Y' ||
                reader.ReadByte() != 'N' || reader.ReadByte() != 'H')
                return null;

            int version = reader.ReadInt32();
            int count = reader.ReadInt32();
            var field = new CajamarcaHeightField
            {
                Count = count,
                Extent = reader.ReadSingle(),
                Datum = reader.ReadSingle(),
                Step = reader.ReadSingle(),
            };
            if (version != Version || count < 2) return null;

            field.Heights = new float[count * count];
            for (int i = 0; i < field.Heights.Length; i++) field.Heights[i] = reader.ReadSingle();
            return field;
        }
    }

    /// <summary>
    /// Descarga el modelo de elevación real (AWS Terrain Tiles, SRTM) del valle
    /// de Cajamarca y lo reproyecta a las coordenadas del plano de OSM.
    /// </summary>
    public static class AyniCajamarcaDem
    {
        public const string AssetPath = "Assets/Resources/Calle/cajamarca_altura.bytes";
        const string TileUrl = "https://s3.amazonaws.com/elevation-tiles-prod/terrarium/{0}/{1}/{2}.png";
        const int Zoom = 15;               // ~4.7 m por píxel en esta latitud
        const double EarthRadius = 6378137.0;
        const double TileSize = 256.0;
        const double ElevationOffset = 32768.0;

        // Centro usado para descargar el mapa (Plaza de Armas aproximada).
        const double CenterLat = -7.15695;
        const double CenterLon = -78.51740;
        static readonly double CenterCos = Math.Cos(CenterLat * Math.PI / 180.0);

        // Centro real (caja envolvente) de la Plaza de Armas según OSM: es el
        // origen del plano, porque CajamarcaGenerar.py centra el mapa en ella.
        const double PlazaLat = -7.1570001;
        const double PlazaLon = -78.5174367;
        static readonly double PlazaEast = (PlazaLon - CenterLon) * Math.PI / 180.0 * EarthRadius * CenterCos;
        static readonly double PlazaSouth = -(PlazaLat - CenterLat) * Math.PI / 180.0 * EarthRadius;

        [MenuItem("AYNI/Escenas/Descargar alturas de Cajamarca", priority = 23)]
        public static void DownloadMenu()
        {
            try
            {
                var field = Build(true);
                Debug.Log($"[AYNI] Alturas de Cajamarca: {field.Count}×{field.Count} " +
                          $"| datum={field.Datum:F1} m | rango={field.Min:F1}..{field.Max:F1} m " +
                          $"| archivo={AssetPath}");
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        public static CajamarcaHeightField Build(bool force = false)
        {
            if (!force)
            {
                var cached = CajamarcaHeightField.Load(AssetPath);
                if (cached != null) return cached;
            }

            float extent = CajamarcaHeightField.DefaultExtent;
            int count = CajamarcaHeightField.DefaultCount;
            float step = 2f * extent / (count - 1);

            // Caja en píxeles del mosaico, con un tile de margen.
            GetTile(-extent, extent, out int ax, out int ay);   // noroeste
            GetTile(extent, -extent, out int bx, out int by);   // sureste
            int tx0 = Math.Min(ax, bx) - 1;
            int ty0 = Math.Min(ay, by) - 1;
            int tx1 = Math.Max(ax, bx) + 1;
            int ty1 = Math.Max(ay, by) + 1;

            int cols = tx1 - tx0 + 1;
            int rows = ty1 - ty0 + 1;
            int mosaicW = cols * 256;
            int mosaicH = rows * 256;
            var mosaic = new float[mosaicW * mosaicH];

            string cacheDir = Path.Combine(Path.GetTempPath(), "ayni_dem");
            Directory.CreateDirectory(cacheDir);

            for (int ty = ty0; ty <= ty1; ty++)
            {
                for (int tx = tx0; tx <= tx1; tx++)
                {
                    string path = Path.Combine(cacheDir, $"{Zoom}_{tx}_{ty}.png");
                    if (!File.Exists(path))
                    {
                        byte[] data = Download(string.Format(TileUrl, Zoom, tx, ty));
                        if (data == null) continue;
                        File.WriteAllBytes(path, data);
                    }

                    var pixels = DecodeTile(path);
                    if (pixels == null) continue;

                    int ox = (tx - tx0) * 256;
                    int oy = (ty - ty0) * 256;
                    for (int row = 0; row < 256; row++)
                    {
                        // Terrarium: fila 0 = norte. En la textura de Unity la
                        // fila 0 es el sur, así que se invierte.
                        int src = (255 - row) * 256;
                        int dst = (oy + row) * mosaicW + ox;
                        for (int col = 0; col < 256; col++)
                            mosaic[dst + col] = pixels[src + col];
                    }
                }
            }

            var field = new CajamarcaHeightField
            {
                Count = count,
                Extent = extent,
                Step = step,
                Heights = new float[count * count],
            };

            for (int j = 0; j < count; j++)
            {
                float z = -extent + j * step;
                for (int i = 0; i < count; i++)
                {
                    float x = -extent + i * step;
                    PlanToLatLon(x, z, out double lat, out double lon);
                    Slippy(lat, lon, out double px, out double py);
                    field.Heights[j * count + i] = SampleMosaic(
                        mosaic, mosaicW, mosaicH,
                        (float)(px - tx0 * TileSize),
                        (float)(py - ty0 * TileSize));
                }
            }

            // Referencia: la plaza queda en y = 0 (media de la zona central).
            double sum = 0.0;
            int hits = 0;
            for (int dz = -25; dz <= 25; dz += 10)
            {
                for (int dx = -25; dx <= 25; dx += 10)
                {
                    sum += field.Sample(dx, dz);
                    hits++;
                }
            }

            field.Datum = (float)(sum / Mathf.Max(1, hits));
            for (int i = 0; i < field.Heights.Length; i++)
                field.Heights[i] -= field.Datum;

            field.Save(AssetPath);
            AssetDatabase.ImportAsset(AssetPath, ImportAssetOptions.ForceUpdate);
            Debug.Log($"[AYNI] DEM Cajamarca descargado: datum={field.Datum:F1} m | " +
                      $"relieve={field.MinRelative():F1}..{field.MaxRelative():F1} m");
            return field;
        }

        static void PlanToLatLon(float x, float z, out double lat, out double lon)
        {
            // Plano: x = este, z = norte. Blender usa y hacia el sur.
            double rawX = x + PlazaEast;
            double rawY = -z + PlazaSouth;
            lat = CenterLat - rawY / EarthRadius * 180.0 / Math.PI;
            lon = CenterLon + rawX / (EarthRadius * CenterCos) * 180.0 / Math.PI;
        }

        static void Slippy(double lat, double lon, out double px, out double py)
        {
            double n = Math.Pow(2.0, Zoom);
            px = (lon + 180.0) / 360.0 * n * TileSize;
            double rad = lat * Math.PI / 180.0;
            py = (1.0 - Math.Log(Math.Tan(rad) + 1.0 / Math.Cos(rad)) / Math.PI) / 2.0 * n * TileSize;
        }

        static void GetTile(float x, float z, out int tx, out int ty)
        {
            PlanToLatLon(x, z, out double lat, out double lon);
            Slippy(lat, lon, out double px, out double py);
            tx = (int)Math.Floor(px / TileSize);
            ty = (int)Math.Floor(py / TileSize);
        }

        static float[] DecodeTile(string path)
        {
            byte[] bytes;
            try
            {
                bytes = File.ReadAllBytes(path);
            }
            catch (IOException)
            {
                return null;
            }

            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            try
            {
                if (!tex.LoadImage(bytes)) return null;
                Color32[] pixels = tex.GetPixels32();
                if (pixels.Length != 256 * 256)
                {
                    Debug.LogWarning("[AYNI] Tile con tamaño inesperado: " + tex.width + "×" + tex.height);
                    return null;
                }

                var heights = new float[pixels.Length];
                for (int i = 0; i < pixels.Length; i++)
                {
                    Color32 c = pixels[i];
                    heights[i] = c.r * 256.0f + c.g + c.b / 256.0f - (float)ElevationOffset;
                }

                return heights;
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(tex);
            }
        }

        static float SampleMosaic(float[] mosaic, int width, int height, float px, float py)
        {
            px = Mathf.Clamp(px, 0f, width - 1.001f);
            py = Mathf.Clamp(py, 0f, height - 1.001f);
            int x0 = (int)px;
            int y0 = (int)py;
            int x1 = Mathf.Min(x0 + 1, width - 1);
            int y1 = Mathf.Min(y0 + 1, height - 1);
            float tx = px - x0;
            float ty = py - y0;
            float a = mosaic[y0 * width + x0];
            float b = mosaic[y0 * width + x1];
            float c = mosaic[y1 * width + x0];
            float d = mosaic[y1 * width + x1];
            return Mathf.Lerp(Mathf.Lerp(a, b, tx), Mathf.Lerp(c, d, tx), ty);
        }

        static byte[] Download(string url)
        {
            try
            {
                var request = (HttpWebRequest)WebRequest.Create(url);
                request.UserAgent = "AYNI-Unity-Game/1.0 (educational project)";
                request.Timeout = 60000;
                request.ReadWriteTimeout = 60000;
                using var response = (HttpWebResponse)request.GetResponse();
                using var stream = response.GetResponseStream();
                using var memory = new MemoryStream();
                stream.CopyTo(memory);
                return memory.ToArray();
            }
            catch (Exception e)
            {
                Debug.LogWarning("[AYNI] No se pudo descargar " + url + " :: " + e.Message);
                return null;
            }
        }
    }
}
#endif
