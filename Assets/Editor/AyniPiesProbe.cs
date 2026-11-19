#if UNITY_EDITOR
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using AYNI.Battle;
using UnityEditor;
using UnityEngine;

namespace AYNI.Editor
{
    /// <summary>
    /// Sonda de apoyo: mide los pies de cada caminante contra el suelo visible
    /// del capítulo CALLES y lista los que quedan hundidos o flotando.
    /// Se lanza en Play, con el capítulo abierto.
    /// </summary>
    public static class AyniPiesProbe
    {
        /// <summary>Debajo de esto el personaje está enterrado.</summary>
        const float Hundido = -0.03f;

        /// <summary>Encima de esto el personaje flota (un salto también cae aquí).</summary>
        const float Flotando = 0.06f;

        /// <summary>
        /// Un pivote alto ancla el dibujo en la cabeza: el cuerpo cuelga por
        /// debajo del suelo. Los pivotes buenos quedan cerca de 0 (los pies).
        /// </summary>
        const float PivoteAlto = 0.5f;

        struct Fila
        {
            public string Nombre;
            public string Padre;
            public float Delta;
            public float Pies;
            public float Suelo;
            public float Pivote;
            public bool Brinca;
            public Vector3 Local;
        }

        [MenuItem("AYNI/Depurar/Revisar pies")]
        public static void Run()
        {
            var calles = Object.FindAnyObjectByType<CajamarcaCalles>();
            if (!Application.isPlaying || calles == null || calles.Distrito == null)
            {
                Debug.LogWarning("[PIES] Abre el capítulo CALLES en Play y vuelve a lanzar la revisión.");
                return;
            }

            var cuerpos = Object.FindObjectsByType<SpriteFrameAnimator>(
                FindObjectsInactive.Exclude);

            var filas = new List<Fila>(cuerpos.Length);
            for (int i = 0; i < cuerpos.Length; i++)
            {
                var anim = cuerpos[i];
                if (anim == null) continue;

                var sr = anim.GetComponent<SpriteRenderer>();
                if (sr == null || sr.sprite == null) continue;

                Transform t = sr.transform;
                if (!t.IsChildOf(calles.Distrito)) continue;

                float pies = t.position.y;
                calles.SueloDelCuerpo(t, out float suelo, out bool brinca);
                float alto = sr.sprite.rect.height;

                filas.Add(new Fila
                {
                    Nombre = t.name,
                    Padre = t.parent != null ? t.parent.name : "-",
                    Delta = pies - suelo,
                    Pies = pies,
                    Suelo = suelo,
                    Pivote = alto > 0f ? sr.sprite.pivot.y / alto : 0f,
                    Brinca = brinca,
                    Local = calles.Distrito.InverseTransformPoint(t.position),
                });
            }

            // Primero lo más hundido: es lo que se ve peor en pantalla.
            filas.Sort((a, b) => a.Delta.CompareTo(b.Delta));

            int hundidos = 0;
            int flotando = 0;
            int brincando = 0;
            int invertidos = 0;
            for (int i = 0; i < filas.Count; i++)
            {
                if (filas[i].Delta < Hundido) hundidos++;
                else if (filas[i].Delta > Flotando)
                {
                    if (filas[i].Brinca) brincando++;
                    else flotando++;
                }
                if (filas[i].Pivote > PivoteAlto) invertidos++;
            }

            var log = new StringBuilder();
            log.AppendLine("PIES · CALLES · " + filas.Count + " cuerpos · "
                + hundidos + " hundidos · " + flotando + " flotando · "
                + brincando + " en salto · "
                + invertidos + " con pivote alto");
            log.AppendLine("delta = ancla - suelo (0 = pies en el piso). Negativo = enterrado.");
            log.AppendLine("pivote = ancla dentro del lienzo, 0 abajo y 1 arriba; "
                + "arriba de " + PivoteAlto + " el dibujo cuelga bajo el suelo.");
            log.AppendLine("nombre\tdelta\tpies\tsuelo\tpivote\tpadre\tlocal");
            for (int i = 0; i < filas.Count; i++)
            {
                Fila f = filas[i];
                log.AppendLine(string.Format(CultureInfo.InvariantCulture,
                    "{0}\t{1:F3}\t{2:F3}\t{3:F3}\t{4:F3}\t{5}\t({6:F1}, {7:F1})",
                    f.Nombre, f.Delta, f.Pies, f.Suelo, f.Pivote, f.Padre,
                    f.Local.x, f.Local.z));
            }

            string dir = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Logs");
            Directory.CreateDirectory(dir);
            string archivo = Path.Combine(dir, "pies.txt");
            File.WriteAllText(archivo, log.ToString());

            Debug.Log("[PIES] " + filas.Count + " cuerpos · " + hundidos
                + " hundidos · " + flotando + " flotando · " + invertidos
                + " con pivote alto → " + archivo);

            for (int i = 0; i < filas.Count && i < 12; i++)
            {
                Fila f = filas[i];
                Debug.Log(string.Format(CultureInfo.InvariantCulture,
                    "[PIES] {0,7:F3} m  {1,-22} local ({2:F1}, {3:F1})  pivote {4:F3}",
                    f.Delta, f.Nombre, f.Local.x, f.Local.z, f.Pivote));
            }
        }
    }
}
#endif
