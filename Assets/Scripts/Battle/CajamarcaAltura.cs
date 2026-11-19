using UnityEngine;

namespace AYNI.Battle
{
    /// <summary>
    /// Altura real del valle de Cajamarca (DEM) en coordenadas del distrito.
    /// La genera AYNI > Escenas > Generar Cajamarca editable y se guarda en
    /// Resources/Calle/cajamarca_altura.bytes.
    /// </summary>
    public static class CajamarcaAltura
    {
        public const string ResourcePath = "Calle/cajamarca_altura";
        const int HeaderBytes = 24;
        const int Version = 1;

        /// <summary>
        /// Factor con el que se hornearon calles, casas y terreno.
        /// No subirlo: un rebake volvería a usar este número.
        /// </summary>
        public const float RelieveCalles = 1.6f;

        /// <summary>
        /// Relieve que se ve en CALLES. Un poco más marcado que el horneado:
        /// los 76 m de la plaza al mirador quedan en unos 156 m.
        /// El capítulo real deja Present en 1.
        /// </summary>
        public const float RelieveMapa = 2.05f;

        static float[] heights;
        static int count;
        static float extent;
        static float step;
        static bool ready;
        static bool failed;
        static float present = 1f;

        public static float Present
        {
            get { return present; }
            set { present = value < 0.5f ? 1f : value; }
        }

        public static bool Ready
        {
            get
            {
                if (!ready && !failed) Load();
                return ready;
            }
        }

        public static float Extent
        {
            get
            {
                if (!ready && !failed) Load();
                return extent;
            }
        }

        public static bool Load()
        {
            if (ready) return true;
            if (failed) return false;

            var asset = Resources.Load<TextAsset>(ResourcePath);
            if (asset == null)
            {
                failed = true;
                return false;
            }

            byte[] bytes = asset.bytes;
            if (bytes == null || bytes.Length < HeaderBytes)
            {
                failed = true;
                return false;
            }

            if (bytes[0] != (byte)'A' || bytes[1] != (byte)'Y' ||
                bytes[2] != (byte)'N' || bytes[3] != (byte)'H')
            {
                failed = true;
                return false;
            }

            int version = System.BitConverter.ToInt32(bytes, 4);
            int nodes = System.BitConverter.ToInt32(bytes, 8);
            if (version != Version || nodes < 2 || bytes.Length < HeaderBytes + nodes * nodes * 4)
            {
                failed = true;
                return false;
            }

            extent = System.BitConverter.ToSingle(bytes, 12);
            step = System.BitConverter.ToSingle(bytes, 20);
            var data = new float[nodes * nodes];
            System.Buffer.BlockCopy(bytes, HeaderBytes, data, 0, data.Length * 4);

            heights = data;
            count = nodes;
            if (step <= 0.01f && count > 1) step = 2f * extent / (count - 1);
            ready = true;
            return true;
        }

        /// <summary>Altura en metros respecto a la plaza. 0 si no hay datos.</summary>
        public static float Sample(float x, float z)
        {
            if (!ready && !failed) Load();
            if (!ready) return 0f;

            float fx = (x + extent) / step;
            float fz = (z + extent) / step;
            int i0 = Mathf.Clamp(Mathf.FloorToInt(fx), 0, count - 2);
            int j0 = Mathf.Clamp(Mathf.FloorToInt(fz), 0, count - 2);
            float tx = Mathf.Clamp01(fx - i0);
            float tz = Mathf.Clamp01(fz - j0);

            int a = j0 * count + i0;
            int b = a + 1;
            int c = a + count;
            int d = c + 1;
            float h0 = Mathf.Lerp(heights[a], heights[b], tx);
            float h1 = Mathf.Lerp(heights[c], heights[d], tx);
            float raw = Mathf.Lerp(h0, h1, tz);
            return present == 1f ? raw : raw * present;
        }

        /// <summary>Altura en coordenadas de mundo para un distrito dado.</summary>
        public static float SampleWorld(Transform district, Vector3 world)
        {
            if (district == null) return Sample(world.x, world.z);
            Vector3 local = district.InverseTransformPoint(world);
            return Sample(local.x, local.z);
        }
    }
}
