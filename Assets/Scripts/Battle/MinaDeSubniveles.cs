using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace AYNI.Battle
{
    /// <summary>
    /// Corte de la mina bajo la laguna. Cinco subniveles a 50 m, labor de 3.5 x 3.5 m
    /// y una rampa en hélice: tres vueltas por tramo, radio de eje 17.9 m, pendiente 15 %.
    /// Cada boca tiene 4 m de piso plano para salir del crucero antes de bajar.
    /// El pique del agua entra por el este del subnivel 0. El pique de izaje queda al este,
    /// con castillo en la orilla. El tajo está al oeste de la rampa.
    /// </summary>
    public static class MinaDeSubniveles
    {
        public const float Seccion = 3.5f;
        public const float EntreNivel = 50f;
        public const int Subniveles = 5;
        public const float RadioEje = 17.9f;
        public const float Vueltas = 3f;
        public const float PortalX = -36f;
        public const float TorreX = -22f;
        public const float TorreZ = 9.2f;
        const float Aterrizaje = 4f;
        const float Libre = 0.95f;
        const float Grosor = 0.58f;
        const float PasoRampa = 2.25f;

        static readonly Color Roca = new Color(0.92f, 0.90f, 0.86f);
        static readonly Color TechoC = new Color(0.70f, 0.68f, 0.64f);
        static readonly Color PisoC = new Color(0.94f, 0.91f, 0.82f);
        static readonly Color Madera = new Color(0.98f, 0.94f, 0.86f);
        static readonly Color Relleno = new Color(0.46f, 0.32f, 0.16f);
        static readonly Color Mineral = new Color(0.34f, 0.30f, 0.27f);
        static readonly Color Veta = new Color(0.78f, 0.62f, 0.24f);
        static readonly Color Fierro = new Color(0.45f, 0.48f, 0.52f);
        static readonly Color Agua = new Color(0.12f, 0.22f, 0.28f);
        static readonly Color Ducto = new Color(0.96f, 0.72f, 0.08f);
        static readonly Color TuboRojo = new Color(0.72f, 0.16f, 0.12f);
        static readonly Color TuboGris = new Color(0.62f, 0.66f, 0.7f);
        static readonly Color Oxido = new Color(0.55f, 0.32f, 0.16f);
        static readonly Color Carbon = new Color(0.1f, 0.1f, 0.11f);
        static readonly Color Luz = new Color(1f, 0.94f, 0.68f);

        static readonly string[] Nombres =
        {
            "Subnivel 0 m  ·  acceso",
            "Subnivel -50 m  ·  relleno",
            "Subnivel -100 m  ·  tajo",
            "Subnivel -150 m  ·  exploracion",
            "Subnivel -200 m  ·  acarreo",
        };

        struct Tramo
        {
            public Vector3 A, B;
            public float Holgura;
        }

        struct Marco
        {
            public Vector3 P;
            public Vector3 T;
            public float Ancho;
            public float Alto;
            public Color Tinte;
            public bool Izquierda;
            public bool Derecha;
            public bool Rampa;
        }

        sealed class Lienzo
        {
            readonly List<Vector3> v = new List<Vector3>(16000);
            readonly List<Vector2> uv = new List<Vector2>(16000);
            readonly List<Color> c = new List<Color>(16000);
            readonly List<int> t = new List<int>(24000);

            public void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Color col)
            {
                if (Vector3.Cross(b - a, d - a).sqrMagnitude < 1e-5f) return;
                int i = v.Count;
                float u = Vector3.Distance(a, b) / 3.5f;
                float w = Vector3.Distance(a, d) / 3.5f;
                v.Add(a); v.Add(b); v.Add(c); v.Add(d);
                uv.Add(Vector2.zero);
                uv.Add(new Vector2(u, 0f));
                uv.Add(new Vector2(u, w));
                uv.Add(new Vector2(0f, w));
                this.c.Add(col); this.c.Add(col); this.c.Add(col); this.c.Add(col);
                t.Add(i); t.Add(i + 1); t.Add(i + 2);
                t.Add(i); t.Add(i + 2); t.Add(i + 3);
            }

            public Mesh Soltar(string nombre)
            {
                var mesh = new Mesh { name = nombre, indexFormat = IndexFormat.UInt32 };
                mesh.SetVertices(v);
                mesh.SetUVs(0, uv);
                mesh.SetColors(c);
                mesh.SetTriangles(t, 0);
                mesh.RecalculateNormals();
                mesh.RecalculateBounds();
                return mesh;
            }
        }

        static readonly List<Tramo> tramos = new List<Tramo>(900);
        static float piso0;
        static Material rocaMat;
        static Material gravaMat;
        static Material maderaMat;
        static Material ductoMat;
        static Material metalMat;
        static Material oxidoMat;
        static Material carbonMat;
        static Material vetaMat;
        static Material aguaMat;
        static Material lisa;

        public struct Hallazgo
        {
            public Vector3 OroA, OroB, OroC, Muqui;
            public bool Bien;
        }

        public static Hallazgo Construir(Transform raiz, float piso, float torreY)
        {
            piso0 = piso;
            tramos.Clear();
            rocaMat = Textura("Mina/roca", new Vector2(1.15f, 1.15f));
            gravaMat = Textura("Mina/grava", new Vector2(2.4f, 2.4f));
            maderaMat = Textura("Mina/madera", new Vector2(1f, 2.2f));
            ductoMat = Textura("Mina/ducto", new Vector2(14f, 2f));
            metalMat = Textura("Mina/metal", new Vector2(2.4f, 2.4f));
            oxidoMat = Textura("Mina/oxido", new Vector2(1.8f, 1.8f));
            carbonMat = Textura("Mina/carbon", new Vector2(1.6f, 1.6f));
            vetaMat = Textura("Mina/veta", new Vector2(1.3f, 1.3f));
            aguaMat = PielDeAgua();
            lisa = Lisa();
            var suelo = new Lienzo();
            var muro = new Lienzo();
            for (int n = 0; n < Subniveles; n++)
                Nivel(raiz, n, suelo, muro);
            Rampa(suelo, muro);
            Poner(raiz, "PisoMina", suelo.Soltar("PisoMina"), false, gravaMat);
            Poner(raiz, "MuroTunel", muro.Soltar("MuroTunel"), true, rocaMat);
            Vestir(raiz, torreY);
            float fondo = PisoDe(Subniveles - 1);
            var hall = new Hallazgo
            {
                OroA = new Vector3(-88.4f, fondo + 0.22f, -0.42f),
                OroB = new Vector3(-87.5f, fondo + 0.22f, 0.08f),
                OroC = new Vector3(-88.1f, fondo + 0.22f, 0.5f),
                Muqui = new Vector3(-90.4f, fondo, 0.55f),
                Bien = Revisar(),
            };
            return hall;
        }

        public static string Rotulo(float y)
        {
            int n = Indice(y);
            if (n < 0) return "Rampa 15 %    ·    seccion 3.5 x 3.5 m    ·    cada 50 m";
            return Nombres[n] + "    ·    3.5 x 3.5 m";
        }

        public static string Lugar(Vector3 p)
        {
            int n = Indice(p.y);
            float bajada = Mathf.Max(0f, piso0 - p.y);
            if (n < 0)
                return "Rampa 15 %   ·   " + bajada.ToString("0") + " m bajo el acceso   ·   flechas giran la vista";
            string lado;
            float dx = p.x - PortalX;
            if (Mathf.Abs(p.x - TorreX) < 3f && p.z > 1.2f) lado = "estocada al pique";
            else if (Mathf.Abs(dx) < 3f && Mathf.Abs(p.z) < 2.2f) lado = "boca de la rampa";
            else if (dx < 0f) lado = Mathf.Abs(dx).ToString("0") + " m al oeste del portal";
            else lado = Mathf.Abs(dx).ToString("0") + " m al este, hacia la salida";
            return Nombres[n] + "   ·   " + lado;
        }

        /// <summary>
        /// 0 is the west end of the drift and 1 is the mouth at the water.
        /// On the ramp the mark stays on the portal, because the helix swings in x.
        /// </summary>
        public static float Marca(Vector3 p)
        {
            float x = Indice(p.y) < 0 ? PortalX : p.x;
            return Mathf.InverseLerp(-96f, -2f, x);
        }

        public static bool Pisar(Vector3 actual, Vector3 destino, bool luz, out Vector3 puesto)
        {
            puesto = actual;
            if (!Proyectar(destino, actual.y, out var q)) return false;
            if (!luz && q.y < actual.y - 0.04f) return false;
            float dx = q.x - actual.x;
            float dz = q.z - actual.z;
            if (dx * dx + dz * dz > 9f) return false;
            puesto = q;
            return true;
        }

        public static Vector3 HaciaSalida(Vector3 p)
        {
            float mejor = 2.2f;
            Vector3 dir = Vector3.zero;
            for (int i = 0; i < tramos.Count; i++)
            {
                Tramo t = tramos[i];
                float dy = t.B.y - t.A.y;
                float largo = Vector3.Distance(t.A, t.B);
                if (largo < 0.2f || Mathf.Abs(dy) / largo < 0.05f) continue;
                float ymin = Mathf.Min(t.A.y, t.B.y);
                float ymax = Mathf.Max(t.A.y, t.B.y);
                if (p.y < ymin - 1.6f || p.y > ymax + 1.6f) continue;
                Vector3 ab = t.B - t.A;
                float u = Mathf.Clamp01(Vector3.Dot(p - t.A, ab) / ab.sqrMagnitude);
                Vector3 q = t.A + ab * u;
                float dx = p.x - q.x;
                float dz = p.z - q.z;
                float d = Mathf.Sqrt(dx * dx + dz * dz);
                if (d > t.Holgura || d > mejor) continue;
                mejor = d;
                dir = dy >= 0f ? ab : -ab;
            }

            if (dir.sqrMagnitude > 0.01f) return dir.normalized;
            if (p.y > piso0 - 4f) return Vector3.right;
            float hacia = PortalX - p.x;
            if (Mathf.Abs(hacia) < 1.6f && Mathf.Abs(p.z) < 1.4f) return Vector3.forward;
            return new Vector3(hacia >= 0f ? 1f : -1f, 0f, 0f);
        }

        static float PisoDe(int n)
        {
            return piso0 - n * EntreNivel;
        }

        static int Indice(float y)
        {
            float bajada = piso0 - y;
            int n = Mathf.RoundToInt(bajada / EntreNivel);
            if (n < 0 || n >= Subniveles) return -1;
            if (Mathf.Abs(bajada - n * EntreNivel) > 4f) return -1;
            return n;
        }

        static float Arco()
        {
            return Vueltas * Mathf.PI * 2f * RadioEje;
        }

        static float Theta0()
        {
            return Mathf.PI * 0.5f;
        }

        static Vector3 Helice(float th)
        {
            float s = (th - Theta0()) * RadioEje;
            float arco = Arco();
            int k = Mathf.FloorToInt(s / arco + 1e-5f);
            if (k < 0) k = 0;
            if (k > Subniveles - 2) k = Subniveles - 2;
            float local = s - k * arco;
            if (local < 0f) local = 0f;
            if (local > arco) local = arco;
            float y = Cota(local, PisoDe(k));
            float cx = PortalX - RadioEje;
            return new Vector3(cx + RadioEje * Mathf.Sin(th), y, RadioEje * Mathf.Cos(th));
        }

        static float Cota(float s, float yArriba)
        {
            float arco = Arco();
            float cuerpo = arco - 2f * Aterrizaje;
            if (s <= Aterrizaje) return yArriba;
            if (s >= arco - Aterrizaje) return yArriba - EntreNivel;
            float u = (s - Aterrizaje) / cuerpo;
            return yArriba - u * EntreNivel;
        }

        static void Recta(float x0, float x1, float y, float z0, float z1, float holgura)
        {
            tramos.Add(new Tramo
            {
                A = new Vector3(x0, y, z0),
                B = new Vector3(x1, y, z1),
                Holgura = holgura,
            });
        }

        static float XEste(int n)
        {
            return n == 0 ? -2.05f : -16f;
        }

        static float XOeste(int n)
        {
            return n == 1 ? -84f : -94f;
        }

        static void Nivel(Transform raiz, int n, Lienzo suelo, Lienzo muro)
        {
            float y = PisoDe(n);
            float oeste = XOeste(n);
            float este = XEste(n);
            Recta(oeste, este, y, 0f, 0f, Libre);

            Recta(TorreX, TorreX, y, 0f, 4.15f, Libre);

            var marcas = new List<float>();
            for (float x = oeste; x < este - 0.2f; x += 1.8f) Meter(marcas, x, oeste, este);
            Meter(marcas, este, oeste, este);
            Meter(marcas, PortalX - 1.85f, oeste, este);
            Meter(marcas, PortalX + 1.85f, oeste, este);
            Meter(marcas, TorreX - 1.85f, oeste, este);
            Meter(marcas, TorreX + 1.85f, oeste, este);
            marcas.Sort();
            var marcos = new List<Marco>(marcas.Count);
            for (int i = 0; i < marcas.Count; i++)
                marcos.Add(MarcoDe(n, marcas[i]));

            for (int i = 0; i < marcos.Count - 1; i++)
            {
                Marco a = marcos[i];
                Marco b = marcos[i + 1];
                float xm = (a.P.x + b.P.x) * 0.5f;
                a.Derecha = true;
                a.Izquierda = true;
                if (Mathf.Abs(xm - PortalX) < 1.98f)
                {
                    if (n < Subniveles - 1) a.Derecha = false;
                    if (n > 0) a.Izquierda = false;
                }

                if (Mathf.Abs(xm - TorreX) < 1.98f) a.Izquierda = false;
                Unir(suelo, muro, a, b);
            }

            if (marcos.Count > 0) Tapon(muro, marcos[0], true);
            if (n != 0 && marcos.Count > 1) Tapon(muro, marcos[marcos.Count - 1], false);
            Estocada(y, suelo, muro);
            Letrero(raiz, n, y);
            Jambas(raiz, n, y);
        }

        static void Meter(List<float> marcas, float x, float oeste, float este)
        {
            if (x < oeste - 0.01f || x > este + 0.01f) return;
            for (int i = 0; i < marcas.Count; i++)
                if (Mathf.Abs(marcas[i] - x) < 0.04f) return;
            marcas.Add(x);
        }

        static Marco MarcoDe(int n, float x)
        {
            float ancho = Seccion;
            float alto = Seccion;
            Color tinte = Roca;
            if (n == 1 && x < -76f) tinte = Relleno;
            if (n == 2 && x < -80f)
            {
                float u = Mathf.Clamp01((-80f - x) / 12f);
                ancho = Mathf.Lerp(Seccion, 5.6f, u);
                alto = Mathf.Lerp(Seccion, 7.4f, u);
                tinte = Mineral;
            }

            return new Marco
            {
                P = new Vector3(x, PisoDe(n), 0f),
                T = Vector3.right,
                Ancho = ancho,
                Alto = alto,
                Tinte = tinte,
                Izquierda = true,
                Derecha = true,
            };
        }

        static void Estocada(float y, Lienzo suelo, Lienzo muro)
        {
            var a = new Marco
            {
                P = new Vector3(TorreX, y, 1.55f),
                T = Vector3.forward,
                Ancho = Seccion,
                Alto = Seccion,
                Tinte = Roca,
                Izquierda = true,
                Derecha = true,
            };
            var b = a;
            b.P.z = 6.15f;
            Unir(suelo, muro, a, b);
        }

        static void Rampa(Lienzo suelo, Lienzo muro)
        {
            float th = Theta0();
            float fin = Theta0() + (Subniveles - 1) * Vueltas * Mathf.PI * 2f;
            float dTh = PasoRampa / RadioEje;
            Vector3 anterior = Helice(th);
            while (th < fin - 1e-4f)
            {
                float next = Mathf.Min(th + dTh, fin);
                for (int k = 1; k < Subniveles; k++)
                {
                    float tk = Theta0() + k * Vueltas * Mathf.PI * 2f;
                    if (tk > th + 1e-4f && tk < next - 1e-4f) next = tk;
                }

                Vector3 p = Helice(next);
                tramos.Add(new Tramo { A = anterior, B = p, Holgura = Libre });
                var a = new Marco
                {
                    P = anterior,
                    T = p - anterior,
                    Ancho = Seccion,
                    Alto = Seccion,
                    Tinte = Roca,
                    Izquierda = true,
                    Derecha = true,
                    Rampa = true,
                };
                var b = a;
                b.P = p;
                Unir(suelo, muro, a, b);
                anterior = p;
                th = next;
            }
        }

        static void Unir(Lienzo suelo, Lienzo muro, Marco a, Marco b)
        {
            if (a.Rampa && !SaleDeLaBoca(ref a.P, ref b.P)) return;
            if (Vector3.Distance(a.P, b.P) < 0.12f) return;
            a.T = b.P - a.P;
            b.T = a.T;
            Esquinas(a, true, out var fl, out var fr, out var cl, out var cr, out _, out _);
            Esquinas(b, true, out var fl2, out var fr2, out var cl2, out var cr2, out _, out _);
            Esquinas(a, false, out var ofl, out var ofr, out var ocl, out var ocr, out _, out _);
            Esquinas(b, false, out var ofl2, out var ofr2, out var ocl2, out var ocr2, out _, out _);

            suelo.Quad(fl, fr, fr2, fl2, PisoC);
            muro.Quad(cl, cr, cr2, cl2, TechoC);
            if (a.Izquierda)
            {
                muro.Quad(fl, cl, cl2, fl2, a.Tinte);
                muro.Quad(ofl, ocl, ocl2, ofl2, a.Tinte * 0.82f);
            }

            if (a.Derecha)
            {
                muro.Quad(fr, cr, cr2, fr2, a.Tinte);
                muro.Quad(ofr, ocr, ocr2, ofr2, a.Tinte * 0.82f);
            }

            muro.Quad(ofl, ofr, ofr2, ofl2, a.Tinte * 0.7f);
            muro.Quad(ocl, ocr, ocr2, ocl2, TechoC * 0.8f);
        }

        static void Tapon(Lienzo muro, Marco m, bool oeste)
        {
            Esquinas(m, true, out var fl, out var fr, out var cl, out var cr, out _, out _);
            if (oeste) muro.Quad(fr, fl, cl, cr, m.Tinte);
            else muro.Quad(fl, fr, cr, cl, m.Tinte);
        }

        static void Esquinas(Marco m, bool interior, out Vector3 fl, out Vector3 fr, out Vector3 cl, out Vector3 cr, out Vector3 lado, out Vector3 arriba)
        {
            Vector3 t = m.T.sqrMagnitude > 1e-6f ? m.T.normalized : Vector3.right;
            lado = Vector3.Cross(Vector3.up, t);
            if (lado.sqrMagnitude < 1e-6f) lado = Vector3.forward;
            lado.Normalize();
            arriba = Vector3.Cross(t, lado);
            if (arriba.y < 0f)
            {
                arriba = -arriba;
                lado = -lado;
            }

            arriba.Normalize();
            float medio = m.Ancho * 0.5f;
            float alto = m.Alto;
            if (!interior)
            {
                medio += Grosor;
                fl = m.P - lado * medio - arriba * 0.42f;
                fr = m.P + lado * medio - arriba * 0.42f;
                cl = m.P - lado * medio + arriba * (alto + 0.36f);
                cr = m.P + lado * medio + arriba * (alto + 0.36f);
                return;
            }

            fl = m.P - lado * medio;
            fr = m.P + lado * medio;
            cl = fl + arriba * alto;
            cr = fr + arriba * alto;
        }

        static bool Boca(Vector3 p)
        {
            if (Mathf.Abs(p.x - PortalX) > 2.5f || Mathf.Abs(p.z) > 1.62f) return false;
            float bajada = piso0 - p.y;
            int n = Mathf.RoundToInt(bajada / EntreNivel);
            if (n < 0 || n >= Subniveles) return false;
            return Mathf.Abs(bajada - n * EntreNivel) < 1.1f;
        }

        static bool SaleDeLaBoca(ref Vector3 a, ref Vector3 b)
        {
            bool ia = Boca(a);
            bool ib = Boca(b);
            if (!ia && !ib) return true;
            if (ia && ib) return false;
            Vector3 adentro = ia ? a : b;
            Vector3 afuera = ia ? b : a;
            for (int i = 0; i < 12; i++)
            {
                Vector3 m = (adentro + afuera) * 0.5f;
                if (Boca(m)) adentro = m;
                else afuera = m;
            }

            if (ia) a = afuera;
            else b = afuera;
            return Vector3.Distance(a, b) > 0.15f;
        }

        static bool Proyectar(Vector3 destino, float yRef, out Vector3 puesto)
        {
            float best = 1e9f;
            bool ok = false;
            puesto = destino;
            for (int i = 0; i < tramos.Count; i++)
            {
                Tramo t = tramos[i];
                float ymin = t.A.y < t.B.y ? t.A.y : t.B.y;
                float ymax = t.A.y > t.B.y ? t.A.y : t.B.y;
                if (yRef < ymin - 2f || yRef > ymax + 2f) continue;
                Vector3 ab = t.B - t.A;
                float ab2 = ab.sqrMagnitude;
                if (ab2 < 1e-6f) continue;
                float u = Vector3.Dot(destino - t.A, ab) / ab2;
                if (u < 0f) u = 0f;
                else if (u > 1f) u = 1f;
                Vector3 q = t.A + ab * u;
                float dx = destino.x - q.x;
                float dz = destino.z - q.z;
                float d = Mathf.Sqrt(dx * dx + dz * dz);
                if (d > t.Holgura) continue;
                float borde = (u < 0.02f || u > 0.98f) ? 0.2f : 0f;
                float score = d + Mathf.Abs(q.y - yRef) * 0.25f + borde;
                if (score >= best) continue;
                best = score;
                puesto = q;
                ok = true;
            }

            return ok;
        }

        static bool Corta(float x)
        {
            return Mathf.Abs(x - PortalX) < 3.2f || Mathf.Abs(x - TorreX) < 2.6f;
        }

        static void Interior(Transform raiz)
        {
            for (int n = 0; n < Subniveles; n++)
                Galeria(raiz, n, PisoDe(n));
            RampaViva(raiz);
        }

        static void Galeria(Transform raiz, int n, float y)
        {
            float a = XOeste(n) + 2.2f;
            float b = n == Subniveles - 1 ? -28f : XEste(n) - 1.2f;
            float inicio = a;
            bool vivo = false;
            const float paso = 4.2f;
            for (float x = a; x <= b + 0.2f; x += paso)
            {
                bool sigue = x <= b && !Corta(x);
                if (sigue)
                {
                    if (!vivo) inicio = x;
                    vivo = true;
                    float zPost = 1.48f;
                    if (n == 2 && x < -80f)
                    {
                        float u = Mathf.Clamp01((-80f - x) / 12f);
                        zPost = Mathf.Lerp(1.48f, 2.4f, u);
                    }

                    Cuadro(raiz, new Vector3(x, y, 0f), Vector3.right, zPost, true);
                }
                else if (vivo)
                {
                    Servicios(raiz, y, inicio, x - paso);
                    vivo = false;
                }
            }

            if (vivo) Servicios(raiz, y, inicio, b);
        }

        static void Servicios(Transform raiz, float y, float x0, float x1)
        {
            if (x1 - x0 < 4f) return;
            float medio = (x0 + x1) * 0.5f;
            float largo = x1 - x0;
            Pieza(raiz, "AdemeDucto", new Vector3(medio, y + 2.55f, 0.82f), new Vector3(largo, 0.5f, 0.5f), Ducto, Quaternion.identity, true);
            Pieza(raiz, "AdemeTubo", new Vector3(medio, y + 1.42f, -1.42f), new Vector3(largo, 0.1f, 0.1f), TuboRojo, Quaternion.identity, true);
            Pieza(raiz, "AdemeTubo", new Vector3(medio, y + 1.68f, -1.42f), new Vector3(largo, 0.08f, 0.08f), TuboGris, Quaternion.identity, true);
            Pieza(raiz, "AdemeRiel", new Vector3(medio, y + 0.08f, 0.98f), new Vector3(largo, 0.06f, 0.05f), Fierro, Quaternion.identity, true);
            Pieza(raiz, "AdemeRiel", new Vector3(medio, y + 0.08f, 1.32f), new Vector3(largo, 0.06f, 0.05f), Fierro, Quaternion.identity, true);
            var durmiente = Madera;
            for (float x = x0 + 0.6f; x < x1 - 0.4f; x += 2.4f)
                Pieza(raiz, "AdemeDurmiente", new Vector3(x, y + 0.04f, 1.15f), new Vector3(0.16f, 0.05f, 0.64f), durmiente, Quaternion.identity, true);
            for (float x = x0 + 5f; x < x1 - 3f; x += 18f)
                Vagon(raiz, x, y);
        }

        static void Vagon(Transform raiz, float x, float y)
        {
            const float z = 1.14f;
            Pieza(raiz, "AdemeVagon", new Vector3(x, y + 0.58f, z), new Vector3(1.45f, 0.62f, 0.7f), Oxido, Quaternion.identity, true);
            Pieza(raiz, "AdemeVagon", new Vector3(x, y + 0.96f, z), new Vector3(1.12f, 0.18f, 0.48f), Carbon, Quaternion.identity, true);
            Pieza(raiz, "AdemeVagon", new Vector3(x - 0.48f, y + 0.16f, z - 0.24f), new Vector3(0.2f, 0.2f, 0.2f), new Color(0.16f, 0.16f, 0.16f), Quaternion.identity, true);
            Pieza(raiz, "AdemeVagon", new Vector3(x + 0.48f, y + 0.16f, z - 0.24f), new Vector3(0.2f, 0.2f, 0.2f), new Color(0.16f, 0.16f, 0.16f), Quaternion.identity, true);
            Pieza(raiz, "AdemeVagon", new Vector3(x - 0.48f, y + 0.16f, z + 0.24f), new Vector3(0.2f, 0.2f, 0.2f), new Color(0.16f, 0.16f, 0.16f), Quaternion.identity, true);
            Pieza(raiz, "AdemeVagon", new Vector3(x + 0.48f, y + 0.16f, z + 0.24f), new Vector3(0.2f, 0.2f, 0.2f), new Color(0.16f, 0.16f, 0.16f), Quaternion.identity, true);
        }

        static void RampaViva(Transform raiz)
        {
            float th = Theta0() + 10f / RadioEje;
            float fin = Theta0() + (Subniveles - 1) * Vueltas * Mathf.PI * 2f - 10f / RadioEje;
            float paso = 22f / RadioEje;
            for (float t = th; t < fin; t += paso)
            {
                Vector3 p = Helice(t);
                if (Boca(p)) continue;
                Vector3 q = Helice(Mathf.Min(t + 0.08f, fin));
                Cuadro(raiz, p, q - p, 1.42f, true);
            }
        }

        static void Cuadro(Transform raiz, Vector3 piso, Vector3 eje, float zPost, bool lampara)
        {
            Vector3 t = eje.sqrMagnitude > 1e-6f ? eje.normalized : Vector3.right;
            Vector3 lado = Vector3.Cross(Vector3.up, t);
            if (lado.sqrMagnitude < 1e-6f) lado = Vector3.forward;
            lado.Normalize();
            const float yPost = 2.15f;
            const float sube = 0.97f;
            Pieza(raiz, "AdemePoste", piso - lado * zPost + Vector3.up * (yPost * 0.5f), new Vector3(0.18f, yPost, 0.18f), Madera, Quaternion.identity, true);
            Pieza(raiz, "AdemePoste", piso + lado * zPost + Vector3.up * (yPost * 0.5f), new Vector3(0.18f, yPost, 0.18f), Madera, Quaternion.identity, true);
            const int n = 5;
            for (int i = 0; i < n; i++)
            {
                Vector3 p0 = PuntoArco(piso, lado, zPost, yPost, sube, i / (float)n);
                Vector3 p1 = PuntoArco(piso, lado, zPost, yPost, sube, (i + 1) / (float)n);
                Vector3 dir = p1 - p0;
                float largo = dir.magnitude;
                if (largo < 0.05f) continue;
                Vector3 hacia = dir / largo;
                Quaternion rot = Mathf.Abs(Vector3.Dot(hacia, Vector3.up)) > 0.96f
                    ? Quaternion.LookRotation(Vector3.forward, hacia)
                    : Quaternion.LookRotation(hacia, Vector3.up);
                Pieza(raiz, "AdemeArco", (p0 + p1) * 0.5f, new Vector3(0.16f, 0.14f, largo + 0.05f), Madera, rot, true);
            }

            if (!lampara) return;
            Vector3 clave = piso + Vector3.up * (yPost + sube);
            Pieza(raiz, "AdemeLuz", clave - Vector3.up * 0.22f, new Vector3(0.07f, 0.28f, 0.07f), new Color(0.32f, 0.28f, 0.24f), Quaternion.identity, true);
            Pieza(raiz, "AdemeLuz", clave - Vector3.up * 0.42f, new Vector3(0.46f, 0.1f, 0.22f), Luz, Quaternion.identity, true);
        }

        static Vector3 PuntoArco(Vector3 piso, Vector3 lado, float zPost, float yPost, float sube, float u)
        {
            float th = Mathf.Lerp(Mathf.PI, 0f, u);
            return piso + lado * (Mathf.Cos(th) * zPost) + Vector3.up * (yPost + Mathf.Sin(th) * sube);
        }

        static void Jambas(Transform raiz, int n, float y)
        {
            if (n < Subniveles - 1)
            {
                Cubo(raiz, "AdemeJamba", new Vector3(PortalX - 1.72f, y + 1.75f, -1.62f), new Vector3(0.22f, Seccion, 0.22f), Madera);
                Cubo(raiz, "AdemeJamba", new Vector3(PortalX + 1.72f, y + 1.75f, -1.62f), new Vector3(0.22f, Seccion, 0.22f), Madera);
            }

            if (n > 0)
            {
                Cubo(raiz, "AdemeJamba", new Vector3(PortalX - 1.72f, y + 1.75f, 1.62f), new Vector3(0.22f, Seccion, 0.22f), Madera);
                Cubo(raiz, "AdemeJamba", new Vector3(PortalX + 1.72f, y + 1.75f, 1.62f), new Vector3(0.22f, Seccion, 0.22f), Madera);
            }
        }

        static void Letrero(Transform raiz, int n, float y)
        {
            var pos = new Vector3(PortalX + 4.6f, y + 2.15f, 1.38f);
            Texto(raiz, pos, Nombres[n] + "\n3.5 x 3.5 m", 180f);
            if (n != 0) return;
            Texto(raiz, new Vector3(PortalX + 4.6f, y + 2.15f, -1.38f), "Rampa 15 %\ncada 50 m", 0f);
        }

        static void Vestir(Transform raiz, float torreY)
        {
            float bajo = PisoDe(Subniveles - 1);
            float alto = piso0 + Seccion;
            float medio = (bajo + alto) * 0.5f;
            float largo = alto - bajo;
            const float jaula = 1.15f;
            Cubo(raiz, "MuroPique", new Vector3(TorreX - jaula, medio, 6.35f), new Vector3(0.28f, largo, 0.28f), Madera);
            Cubo(raiz, "MuroPique", new Vector3(TorreX + jaula, medio, 6.35f), new Vector3(0.28f, largo, 0.28f), Madera);
            Cubo(raiz, "MuroPique", new Vector3(TorreX - jaula, medio, 8.15f), new Vector3(0.28f, largo, 0.28f), Madera);
            Cubo(raiz, "MuroPique", new Vector3(TorreX + jaula, medio, 8.15f), new Vector3(0.28f, largo, 0.28f), Madera);
            Cubo(raiz, "MuroPique", new Vector3(TorreX, medio, 8.95f), new Vector3(4.6f, largo, 0.45f), Roca);
            for (int n = 0; n < Subniveles; n++)
            {
                float y = PisoDe(n) + 3.15f;
                Cubo(raiz, "MuroPique", new Vector3(TorreX, y, 7.25f), new Vector3(jaula * 2f + 0.2f, 0.18f, 0.18f), Madera);
                Cubo(raiz, "MuroPique", new Vector3(TorreX, y, 7.25f), new Vector3(0.18f, 0.18f, 2.1f), Madera);
            }

            Interior(raiz);

            float y4 = bajo;
            Cubo(raiz, "AdemeTolva", new Vector3(-19.2f, y4 + 1.15f, 1.15f), new Vector3(2.4f, 2.2f, 1.05f), new Color(0.42f, 0.36f, 0.28f));
            Cubo(raiz, "AdemeTolva", new Vector3(-19.2f, y4 + 2.5f, 1.15f), new Vector3(1.1f, 0.7f, 0.7f), Fierro);
            Cubo(raiz, "AdemeChancadora", new Vector3(-19.4f, y4 + 0.7f, -1.15f), new Vector3(2.6f, 1.3f, 1.1f), Fierro);
            Cubo(raiz, "AdemeBomba", new Vector3(-16.8f, y4 + 0.45f, -1.05f), new Vector3(0.7f, 0.8f, 0.55f), new Color(0.25f, 0.38f, 0.55f));
            Cubo(raiz, "PisoSump", new Vector3(-17.2f, y4 + 0.04f, -0.15f), new Vector3(2.2f, 0.06f, 1.3f), Agua);

            float y2 = PisoDe(2);
            Cubo(raiz, "AdemeVeta", new Vector3(-93.1f, y2 + 1.4f, 0.4f), new Vector3(0.35f, 1.6f, 0.8f), Veta);
            Cubo(raiz, "AdemeVeta", new Vector3(-93.2f, y2 + 2.3f, -0.7f), new Vector3(0.28f, 0.9f, 0.55f), Veta);
            float y3 = PisoDe(3);
            for (int i = -1; i <= 1; i++)
                Cubo(raiz, "AdemeBarreno", new Vector3(-93.3f, y3 + 1.5f, i * 0.7f), new Vector3(1.1f, 0.1f, 0.1f), new Color(0.08f, 0.08f, 0.08f));

            Castillo(raiz, torreY);
        }

        static void Castillo(Transform raiz, float suelo)
        {
            float pie = suelo;
            const float h = 11f;
            float x = TorreX;
            float z = 7.25f;
            Cubo(raiz, "MuroTorre", new Vector3(x - 1.5f, pie + h * 0.5f, z - 1.3f), new Vector3(0.28f, h, 0.28f), Madera);
            Cubo(raiz, "MuroTorre", new Vector3(x + 1.5f, pie + h * 0.5f, z - 1.3f), new Vector3(0.28f, h, 0.28f), Madera);
            Cubo(raiz, "MuroTorre", new Vector3(x - 1.5f, pie + h * 0.5f, z + 1.3f), new Vector3(0.28f, h, 0.28f), Madera);
            Cubo(raiz, "MuroTorre", new Vector3(x + 1.5f, pie + h * 0.5f, z + 1.3f), new Vector3(0.28f, h, 0.28f), Madera);
            Cubo(raiz, "MuroTorre", new Vector3(x, pie + h - 0.2f, z), new Vector3(3.6f, 0.28f, 3.2f), Madera);
            Cubo(raiz, "MuroTorre", new Vector3(x, pie + h + 0.7f, z), new Vector3(0.7f, 1.5f, 0.18f), Fierro);
            Cubo(raiz, "AdemePolea", new Vector3(x, pie + h + 1.5f, z), new Vector3(1.3f, 1.3f, 0.18f), Fierro);
            Cubo(raiz, "MuroTorre", new Vector3(x, pie + 0.35f, z), new Vector3(3.2f, 0.7f, 2.8f), new Color(0.32f, 0.3f, 0.28f));
        }

        static void Cubo(Transform padre, string nombre, Vector3 pos, Vector3 tam, Color tinte)
        {
            Pieza(padre, nombre, pos, tam, tinte, Quaternion.identity, false);
        }

        static void Pieza(Transform padre, string nombre, Vector3 pos, Vector3 tam, Color tinte, Quaternion giro, bool liso)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = nombre;
            var col = go.GetComponent<Collider>();
            if (col != null) Object.DestroyImmediate(col);
            go.transform.SetParent(padre, false);
            go.transform.localPosition = pos;
            go.transform.localRotation = giro;
            go.transform.localScale = tam;
            var rend = go.GetComponent<Renderer>();
            Material mat = Elegir(nombre, tinte, liso);
            rend.sharedMaterial = mat;
            rend.shadowCastingMode = ShadowCastingMode.Off;
            rend.receiveShadows = false;
            var mesh = go.GetComponent<MeshFilter>().mesh;
            Color tintaMalla = tinte;
            if (mat == ductoMat || mat == oxidoMat || mat == carbonMat || mat == vetaMat)
                tintaMalla = new Color(0.94f, 0.94f, 0.94f);
            else if (mat == aguaMat)
                tintaMalla = new Color(0.55f, 0.78f, 0.74f);
            else if (mat == metalMat)
                tintaMalla = Color.Lerp(Color.white, tinte, 0.82f);
            var cols = new Color[mesh.vertexCount];
            for (int i = 0; i < cols.Length; i++) cols[i] = tintaMalla;
            mesh.colors = cols;
        }

        static void Poner(Transform raiz, string nombre, Mesh mesh, bool solido, Material mat)
        {
            var go = new GameObject(nombre);
            go.transform.SetParent(raiz, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var rend = go.AddComponent<MeshRenderer>();
            rend.sharedMaterial = mat != null ? mat : rocaMat;
            rend.shadowCastingMode = ShadowCastingMode.Off;
            rend.receiveShadows = false;
            if (!solido) return;
            var col = go.AddComponent<MeshCollider>();
            col.sharedMesh = mesh;
            col.convex = false;
        }

        static void Texto(Transform raiz, Vector3 pos, string texto, float yaw)
        {
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font == null) font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            if (font == null) return;
            var go = new GameObject("AdemeLetrero");
            go.transform.SetParent(raiz, false);
            go.transform.localPosition = pos;
            go.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
            var tm = go.AddComponent<TextMesh>();
            tm.text = texto;
            tm.font = font;
            tm.fontSize = 48;
            tm.characterSize = 0.14f;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            tm.color = new Color(0.95f, 0.88f, 0.66f);
            var rend = go.GetComponent<MeshRenderer>();
            rend.sharedMaterial = font.material;
            rend.shadowCastingMode = ShadowCastingMode.Off;
            rend.receiveShadows = false;
        }

        static Material Elegir(string nombre, Color tinte, bool liso)
        {
            if (tinte == Madera) return maderaMat;
            if (nombre.Contains("Ducto")) return ductoMat;
            if (nombre.Contains("Sump")) return aguaMat;
            if (nombre.Contains("Veta")) return vetaMat;
            if (nombre.Contains("Barreno")) return carbonMat;
            if (nombre.Contains("Vagon"))
            {
                if (tinte == Oxido) return oxidoMat;
                if (tinte == Carbon) return carbonMat;
                return metalMat;
            }

            if (nombre.Contains("Tolva")) return tinte == Fierro ? metalMat : oxidoMat;
            if (nombre.Contains("Tubo") || nombre.Contains("Riel") || nombre.Contains("Chancadora")
                || nombre.Contains("Polea") || nombre.Contains("Bomba") || nombre.Contains("Luz"))
                return metalMat;
            if (liso) return lisa;
            return rocaMat;
        }

        static Material PielDeAgua()
        {
            var shader = Shader.Find("AYNI/Laguna") ?? Shader.Find("AYNI/Rio") ?? Shader.Find("AYNI/Mina");
            var mat = new Material(shader);
            var tex = Resources.Load<Texture2D>("Agua/laguna");
            if (tex == null) tex = Resources.Load<Texture2D>("Mina/laguna");
            if (tex != null)
            {
                tex.wrapMode = TextureWrapMode.Repeat;
                mat.mainTexture = tex;
                mat.mainTextureScale = new Vector2(1.6f, 1.6f);
            }

            if (mat.HasProperty("_Color"))
                mat.SetColor("_Color", new Color(0.55f, 0.78f, 0.84f, 0.92f));
            return mat;
        }

        static Material Textura(string ruta, Vector2 escala)
        {
            var shader = Shader.Find("AYNI/Mina") ?? Shader.Find("AYNI/Calle") ?? Shader.Find("Sprites/Default");
            var mat = new Material(shader);
            var tex = Resources.Load<Texture2D>(ruta);
            if (tex == null) tex = Resources.Load<Texture2D>("Calle/piedra");
            if (tex != null)
            {
                tex.wrapMode = TextureWrapMode.Repeat;
                mat.mainTexture = tex;
                mat.mainTextureScale = escala;
            }

            return mat;
        }

        static Material Lisa()
        {
            var shader = Shader.Find("AYNI/Mina") ?? Shader.Find("AYNI/Calle") ?? Shader.Find("Sprites/Default");
            return new Material(shader);
        }

        static bool Revisar()
        {
            var fallos = new List<string>();
            float arco = Arco();
            float cuerpo = arco - 2f * Aterrizaje;
            float grado = EntreNivel / Mathf.Sqrt(cuerpo * cuerpo + EntreNivel * EntreNivel);
            if (Mathf.Abs(grado - 0.15f) > 0.004f)
                fallos.Add("pendiente " + grado.ToString("0.000"));
            if (Mathf.Abs(Seccion - 3.5f) > 0.001f || Mathf.Abs(EntreNivel - 50f) > 0.001f)
                fallos.Add("medidas");

            for (int n = 0; n < Subniveles; n++)
            {
                var boca = new Vector3(PortalX, PisoDe(n), 0f);
                if (!Proyectar(boca, boca.y, out var q) || Mathf.Abs(q.y - boca.y) > 0.05f || Mathf.Abs(q.x - PortalX) > 0.05f)
                    fallos.Add("portal " + n);
                if (n == 0)
                {
                    var sur = boca + new Vector3(0f, 0f, -1.2f);
                    if (!Proyectar(sur, boca.y, out q) || q.y < boca.y - 0.08f || q.z > -0.4f)
                        fallos.Add("bajada plana");
                    var hondo = Helice(Theta0() + 8f / RadioEje);
                    if (!Proyectar(hondo, boca.y, out q) || q.y > boca.y - 0.25f)
                        fallos.Add("no baja");
                }

                if (n == 2)
                {
                    var norte = boca + new Vector3(0f, 0f, 1.2f);
                    if (!Proyectar(norte, boca.y, out q) || q.y < boca.y - 0.08f || q.z < 0.4f)
                        fallos.Add("subida plana");
                    var oeste = boca + new Vector3(-4f, 0f, 0f);
                    if (!Proyectar(oeste, boca.y, out q) || Mathf.Abs(q.y - boca.y) > 0.05f || Mathf.Abs(q.z) > 0.05f)
                        fallos.Add("crucero");
                }
            }

            float xBajo = PortalX - 2f * RadioEje;
            var cruce = new Vector3(xBajo, piso0, 0f);
            if (!Proyectar(cruce, piso0, out var queda) || Mathf.Abs(queda.y - piso0) > 0.08f)
                fallos.Add("paso bajo la labor");

            if (fallos.Count > 0)
            {
                Debug.LogError("[AYNI] Mina incompleta: " + string.Join(", ", fallos));
                return false;
            }

            Debug.Log("[AYNI] Mina OK seccion=3.5 entre=50 grado=" + grado.ToString("0.000") + " tramos=" + tramos.Count);
            return true;
        }
    }
}
