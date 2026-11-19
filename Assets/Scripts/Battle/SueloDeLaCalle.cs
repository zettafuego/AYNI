using System;

namespace AYNI.Battle
{
    /// <summary>
    /// Único contrato de suelo del capítulo CALLES.
    ///
    /// El piso visible es el relieve (DEM) más la capa que el constructor de la
    /// plaza dibuja encima: jardín, paseo, pista o calzada. Esa capa ya incluye
    /// el peldaño de la vereda, así que nadie debe sumarle un 0.16 extra.
    ///
    /// Fuera de CALLES la capa queda vacía y el suelo es solo el relieve, que es
    /// lo mismo que usan los demás capítulos.
    /// </summary>
    public static class SueloDeLaCalle
    {
        static Func<float, float, float> capa;

        /// <summary>La calle enlaza su capa de plaza al abrir el capítulo.</summary>
        public static void Enlazar(Func<float, float, float> capaPlaza)
        {
            capa = capaPlaza;
        }

        /// <summary>Al cerrar el capítulo nadie más aporta capa.</summary>
        public static void Soltar()
        {
            capa = null;
        }

        /// <summary>Altura del piso visible en coordenadas del distrito.</summary>
        public static float Altura(float x, float z)
        {
            float relieve = CajamarcaAltura.Sample(x, z);
            return capa != null ? relieve + capa(x, z) : relieve;
        }

        /// <summary>Altura del piso visible para un punto local del distrito.</summary>
        public static float Altura(UnityEngine.Vector3 local)
        {
            return Altura(local.x, local.z);
        }

        /// <summary>Altura del piso visible para un punto en coordenadas de mundo.</summary>
        public static float AlturaMundo(UnityEngine.Vector3 world, UnityEngine.Transform district)
        {
            if (district == null) return Altura(world.x, world.z);
            UnityEngine.Vector3 local = district.InverseTransformPoint(world);
            return Altura(local.x, local.z);
        }
    }
}
