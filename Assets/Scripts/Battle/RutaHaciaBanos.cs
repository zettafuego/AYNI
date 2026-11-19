using System.Collections.Generic;
using AYNI.Narrative;
using UnityEngine;
using UnityEngine.Rendering;

namespace AYNI.Battle
{
    /// <summary>
    /// Av. Atahualpa hacia la Universidad Nacional de Cajamarca y Baños del Inca.
    /// El bus y la mototaxi recorren plaza → UNC → Baños. ENTER sube y baja.
    /// </summary>
    [DefaultExecutionOrder(-50)]
    public class RutaHaciaBanos : MonoBehaviour
    {
        public const float UncX = 880f;
        public const float UncZ = -210f;
        public const float BanosX = 1300f;
        public const float BanosZ = -55f;

        const float Alcance = 4.2f;
        const float Espera = 3.2f;

        static RutaHaciaBanos actual;
        static bool sembrado;

        public static bool Abordo { get; private set; }
        public static bool JuntoAlParadero { get; private set; }

        struct Parada
        {
            public string Nombre;
            public float Dist;
            public Vector2 Piso;
        }

        sealed class Carro
        {
            public Transform Cuerpo;
            public bool Bus;
            public float Along;
            public float Dir = 1f;
            public float Speed;
            public float Espera;
            public int Parada;
            public Vector3 Asiento;
            public bool QuiereBajar;
        }

        readonly List<Carro> carros = new List<Carro>();
        Vector2[] via;
        float viaLargo;
        Parada[] paradas;
        Carro montado;
        bool aviso;
        string avisoTexto;

        public static void Preparar(Transform parent)
        {
            if (parent == null) return;
            if (parent.Find("RutaBanosActores") != null) return;
            var go = new GameObject("RutaBanosActores");
            go.transform.SetParent(parent, false);
            go.AddComponent<RutaHaciaBanos>().Armar();
        }

        public static bool ConsumeEscape()
        {
            if (!Abordo || actual == null) return false;
            actual.Bajar();
            return true;
        }

        void OnEnable()
        {
            actual = this;
        }

        void OnDisable()
        {
            if (actual != this) return;
            if (Abordo) CajamarcaCalles.FijarTienda(false);
            Abordo = false;
            JuntoAlParadero = false;
            actual = null;
        }

        void Armar()
        {
            via = ViaCompleta();
            viaLargo = Largo(via);
            paradas = new[]
            {
                new Parada { Nombre = "Plaza de Armas", Dist = 0f, Piso = via[0] },
                new Parada { Nombre = "Universidad Nacional de Cajamarca", Dist = DistHasta(via, UncX, UncZ), Piso = new Vector2(UncX, UncZ) },
                new Parada { Nombre = "Baños del Inca", Dist = viaLargo, Piso = new Vector2(BanosX, BanosZ) },
            };

            VestirUniversidad();
            VestirBanos();
            if (!sembrado)
            {
                CasasDeLaAvenida();
                CajamarcaCalles.SembrarEdificio(UncX - 16f, UncZ + 8f, 180f, 0, "Pabellón A");
                CajamarcaCalles.SembrarEdificio(UncX + 16f, UncZ + 6f, 180f, 1, "Pabellón B");
                CajamarcaCalles.SembrarEdificio(UncX, UncZ - 14f, 0f, 2, "Pabellón C");
                CajamarcaCalles.SembrarEdificio(BanosX - 14f, BanosZ - 8f, 0f, 3, "Vestuarios");
                CajamarcaCalles.SembrarEdificio(BanosX + 14f, BanosZ + 8f, 180f, 4, "Hostal");
                CajamarcaCalles.SembrarEdificio(via[0].x, via[0].y, 0f, 5, "Paradero  Plaza de Armas");
                CajamarcaCalles.SembrarEdificio(UncX, UncZ + 22f, 0f, 5, "Paradero  UNC");
                CajamarcaCalles.SembrarEdificio(BanosX, BanosZ + 16f, 0f, 5, "Paradero  Baños del Inca");
                sembrado = true;
            }

            var bus = HacerCarro(true, 0f, 1f, 28f, new Vector3(-1.35f, 0.55f, -0.6f), 0);
            var moto = HacerCarro(false, paradas[1].Dist, -1f, 22f, new Vector3(-0.95f, 0.42f, 0.15f), 1);
            carros.Add(bus);
            carros.Add(moto);
        }

        static Vector2[] ViaCompleta()
        {
            var plaza = CajamarcaCalles.PuntoPlazaEste();
            return new[]
            {
                new Vector2(plaza.x, plaza.z),
                new Vector2(132.8f, 56.3f),
                new Vector2(248.8f, -40.8f),
                new Vector2(307.3f, -91.2f),
                new Vector2(420f, -130f),
                new Vector2(540f, -165f),
                new Vector2(680f, -190f),
                new Vector2(800f, -205f),
                new Vector2(UncX, UncZ),
                new Vector2(1000f, -190f),
                new Vector2(1120f, -140f),
                new Vector2(1220f, -90f),
                new Vector2(BanosX, BanosZ),
            };
        }

        Carro HacerCarro(bool bus, float along, float dir, float speed, Vector3 asiento, int parada)
        {
            var go = new GameObject(bus ? "BusBanos" : "MototaxiBanos");
            go.transform.SetParent(transform, false);
            PonerEnVia(go.transform, along, dir);
            if (bus) CuerpoBus(go.transform);
            else CuerpoMoto(go.transform);
            GallinaAlVolante(go.transform, bus);
            return new Carro
            {
                Cuerpo = go.transform,
                Bus = bus,
                Along = along,
                Dir = dir,
                Speed = speed,
                Asiento = asiento,
                Parada = parada,
                Espera = bus ? 1.6f : 0.8f,
            };
        }

        void CuerpoBus(Transform root)
        {
            var crema = new Color(0.96f, 0.86f, 0.28f);
            var blanco = new Color(0.94f, 0.93f, 0.88f);
            var vidrio = new Color(0.42f, 0.58f, 0.72f, 1f);
            Caja(root, "Chasis", new Vector3(0f, 1.15f, 0f), new Vector3(2.4f, 2.1f, 7.4f), null, crema, false);
            Caja(root, "Techo", new Vector3(0f, 2.28f, 0f), new Vector3(2.2f, 0.18f, 7.2f), null, blanco, false);
            Caja(root, "VidrioFrente", new Vector3(0f, 1.55f, 3.55f), new Vector3(2.0f, 0.85f, 0.08f), null, vidrio, false);
            Caja(root, "VidrioLado", new Vector3(1.22f, 1.55f, 0.4f), new Vector3(0.06f, 0.7f, 5.4f), null, vidrio, false);
            Caja(root, "RuedaA", new Vector3(0.95f, 0.38f, 2.4f), new Vector3(0.28f, 0.76f, 0.76f), null, Color.black, false);
            Caja(root, "RuedaB", new Vector3(-0.95f, 0.38f, 2.4f), new Vector3(0.28f, 0.76f, 0.76f), null, Color.black, false);
            Caja(root, "RuedaC", new Vector3(0.95f, 0.38f, -2.5f), new Vector3(0.28f, 0.76f, 0.76f), null, Color.black, false);
            Caja(root, "RuedaD", new Vector3(-0.95f, 0.38f, -2.5f), new Vector3(0.28f, 0.76f, 0.76f), null, Color.black, false);
            Texto(root, "LetreroBus", new Vector3(0f, 2.55f, 0f), "BAÑOS DEL INCA", 0.08f);
        }

        void CuerpoMoto(Transform root)
        {
            var amarillo = new Color(1f, 0.82f, 0.12f);
            var lona = new Color(0.18f, 0.42f, 0.22f);
            Caja(root, "Base", new Vector3(0f, 0.55f, 0.15f), new Vector3(1.35f, 0.55f, 2.5f), null, amarillo, false);
            Caja(root, "Cabina", new Vector3(0f, 1.35f, -0.35f), new Vector3(1.28f, 1.15f, 1.55f), null, amarillo, false);
            Caja(root, "Lona", new Vector3(0f, 1.95f, -0.2f), new Vector3(1.4f, 0.08f, 1.9f), null, lona, false);
            Caja(root, "RuedaF", new Vector3(0f, 0.32f, 1.15f), new Vector3(0.18f, 0.64f, 0.64f), null, Color.black, false);
            Caja(root, "RuedaI", new Vector3(0.58f, 0.32f, -0.85f), new Vector3(0.18f, 0.64f, 0.64f), null, Color.black, false);
            Caja(root, "RuedaD", new Vector3(-0.58f, 0.32f, -0.85f), new Vector3(0.18f, 0.64f, 0.64f), null, Color.black, false);
            Texto(root, "LetreroMoto", new Vector3(0f, 2.15f, -0.2f), "MOTOTAXI", 0.055f);
        }

        void GallinaAlVolante(Transform root, bool bus)
        {
            var frames = BattleRuntimeAssets.LoadClip("Calle/Gallina/Conducir");
            if (frames == null || frames.Length == 0) return;
            var hen = new GameObject("Conductora");
            hen.transform.SetParent(root, false);
            hen.transform.localPosition = bus ? new Vector3(0.55f, 1.55f, 2.6f) : new Vector3(0.15f, 1.15f, 0.55f);
            float height = frames[0].rect.height / frames[0].pixelsPerUnit;
            float scale = height > 0.01f ? 1.15f / height : 0.35f;
            hen.transform.localScale = new Vector3(scale, scale, 1f);
            var ink = hen.AddComponent<SpriteRenderer>();
            ink.sortingOrder = 14;
            ink.sprite = frames[0];
            var anim = hen.AddComponent<SpriteFrameAnimator>();
            anim.SetFrames(frames);
            anim.PlayLocomotion(true, false);
            hen.AddComponent<Billboard>();
        }

        void CasasDeLaAvenida()
        {
            for (int i = 0; i < 14; i++)
            {
                float t = (i + 0.35f) / 14f;
                float x = Mathf.Lerp(380f, 760f, t);
                float z = Mathf.Lerp(-118f, -200f, t);
                float dx = 760f - 380f;
                float dz = -200f + 118f;
                float mag = Mathf.Sqrt(dx * dx + dz * dz);
                float lado = (i % 2 == 0) ? 11.5f : -11.5f;
                float px = -dz / mag * lado;
                float pz = dx / mag * lado;
                float cx = x + px;
                float cz = z + pz;
                float yaw = Mathf.Atan2(-px, -pz) * Mathf.Rad2Deg;
                CajamarcaCalles.SembrarCasona(cx, cz, yaw, i);
            }
        }

        void VestirUniversidad()
        {
            var root = new GameObject("CampusUNC");
            root.transform.SetParent(transform, false);
            root.transform.localPosition = new Vector3(UncX, CajamarcaAltura.Sample(UncX, UncZ), UncZ);
            Caja(root.transform, "PorticoI", new Vector3(-4.2f, 2.4f, 18f), new Vector3(0.7f, 4.8f, 0.7f), "Calle/piedra", Color.white);
            Caja(root.transform, "PorticoD", new Vector3(4.2f, 2.4f, 18f), new Vector3(0.7f, 4.8f, 0.7f), "Calle/piedra", Color.white);
            Caja(root.transform, "DintelUNC", new Vector3(0f, 4.7f, 18f), new Vector3(9.2f, 0.7f, 0.7f), "Calle/piedra", Color.white);
            Texto(root.transform, "NombreUNC", new Vector3(0f, 6.4f, 18.4f), "UNIVERSIDAD NACIONAL\nDE CAJAMARCA", 0.11f);
        }

        void VestirBanos()
        {
            var root = new GameObject("BanosDelInca");
            root.transform.SetParent(transform, false);
            root.transform.localPosition = new Vector3(BanosX, CajamarcaAltura.Sample(BanosX, BanosZ), BanosZ);
            var piedra = new Color(0.82f, 0.74f, 0.58f);
            var agua = new Color(0.28f, 0.62f, 0.68f);
            Caja(root.transform, "BordeA", new Vector3(-6f, 0.28f, 2f), new Vector3(12.4f, 0.55f, 8.4f), "Calle/piedra_inca", piedra, false);
            Caja(root.transform, "AguaA", new Vector3(-6f, 0.52f, 2f), new Vector3(10.6f, 0.18f, 6.6f), null, agua, false);
            Caja(root.transform, "BordeB", new Vector3(7f, 0.28f, -3f), new Vector3(9.2f, 0.5f, 7.2f), "Calle/piedra_inca", piedra, false);
            Caja(root.transform, "AguaB", new Vector3(7f, 0.5f, -3f), new Vector3(7.6f, 0.16f, 5.6f), null, agua, false);
            Texto(root.transform, "NombreBanos", new Vector3(0f, 4.6f, 12f), "BAÑOS DEL INCA", 0.13f);
        }

        void Update()
        {
            if (!CajamarcaCalles.IsOpen || CaminoRelato.Reading)
            {
                JuntoAlParadero = false;
                return;
            }

            for (int i = 0; i < carros.Count; i++)
                MoverCarro(carros[i]);

            if (CasonasDelCamino.Adentro || CajamarcaCalles.Editando)
            {
                JuntoAlParadero = false;
                return;
            }

            if (!CajamarcaCalles.LeerHeroe(out Vector3 pies, out _, out var anim))
            {
                JuntoAlParadero = false;
                return;
            }

            if (Abordo)
            {
                SeguirAsiento(anim);
                return;
            }

            Vigilar(pies);
        }

        void MoverCarro(Carro carro)
        {
            if (carro.Cuerpo == null) return;
            if (carro.Espera > 0f)
            {
                carro.Espera -= Time.deltaTime;
                PonerEnVia(carro.Cuerpo, carro.Along, carro.Dir);
                return;
            }

            carro.Along += carro.Dir * carro.Speed * Time.deltaTime;
            if (carro.Along >= viaLargo)
            {
                carro.Along = viaLargo;
                carro.Dir = -1f;
                carro.Parada = paradas.Length - 1;
                carro.Espera = Espera;
            }
            else if (carro.Along <= 0f)
            {
                carro.Along = 0f;
                carro.Dir = 1f;
                carro.Parada = 0;
                carro.Espera = Espera;
            }
            else
            {
                int toca = ParadaEn(carro.Along, carro.Dir);
                if (toca >= 0 && toca != carro.Parada)
                {
                    carro.Parada = toca;
                    carro.Along = paradas[toca].Dist;
                    carro.Espera = Espera;
                    if (montado == carro && carro.QuiereBajar)
                        Bajar();
                }
            }

            PonerEnVia(carro.Cuerpo, carro.Along, carro.Dir);
        }

        void PonerEnVia(Transform cuerpo, float along, float dir)
        {
            Punto(via, Mathf.Clamp(along, 0f, viaLargo), out float x, out float z, out float tx, out float tz);
            if (dir < 0f)
            {
                tx = -tx;
                tz = -tz;
            }

            float y = SueloDeLaCalle.Altura(x, z);
            cuerpo.localPosition = new Vector3(x, y, z);
            if (tx * tx + tz * tz > 0.0001f)
                cuerpo.localRotation = Quaternion.LookRotation(new Vector3(tx, 0f, tz), Vector3.up);
        }

        int ParadaEn(float along, float dir)
        {
            for (int i = 0; i < paradas.Length; i++)
            {
                float d = paradas[i].Dist - along;
                if (dir > 0f && d >= 0f && d < 8f) return i;
                if (dir < 0f && d <= 0f && d > -8f) return i;
            }

            return -1;
        }

        void Vigilar(Vector3 pies)
        {
            Carro cerca = null;
            float mejor = Alcance * Alcance;
            for (int i = 0; i < carros.Count; i++)
            {
                var carro = carros[i];
                if (carro.Cuerpo == null) continue;
                Vector3 gap = carro.Cuerpo.localPosition - pies;
                gap.y = 0f;
                float d = gap.sqrMagnitude;
                if (d < mejor)
                {
                    mejor = d;
                    cerca = carro;
                }
            }

            JuntoAlParadero = cerca != null;
            if (cerca == null)
            {
                aviso = false;
                return;
            }

            string linea = cerca.Bus ? LeyDelMonte.ParaderoBus : LeyDelMonte.ParaderoMoto;
            if (!aviso || avisoTexto != linea)
            {
                aviso = true;
                avisoTexto = linea;
                CajamarcaCalles.Decir(linea);
            }

            if (CajamarcaCalles.EnTienda) return;
            if (!Input.GetKeyDown(KeyCode.Return) && !Input.GetKeyDown(KeyCode.KeypadEnter)) return;
            Subir(cerca);
        }

        void Subir(Carro carro)
        {
            if (!CajamarcaCalles.LeerHeroe(out _, out _, out _)) return;
            montado = carro;
            carro.QuiereBajar = false;
            Abordo = true;
            JuntoAlParadero = false;
            aviso = false;
            CajamarcaCalles.FijarTienda(true);
            CajamarcaCalles.AsegurarInclinacion();
            LluviaDeAlmas.Pausa(true);
            string linea = carro.Bus ? LeyDelMonte.SubeBus : LeyDelMonte.SubeMoto;
            CajamarcaCalles.Decir(linea);
        }

        void SeguirAsiento(SpriteFrameAnimator anim)
        {
            if (montado == null || montado.Cuerpo == null)
            {
                Bajar();
                return;
            }

            Vector3 seat = montado.Cuerpo.localPosition + montado.Cuerpo.localRotation * montado.Asiento;
            seat.y = SueloDeLaCalle.Altura(seat.x, seat.z) + 0.55f;
            CajamarcaCalles.PonerHeroe(seat);
            anim?.PlayStep(Vector3.zero, false);

            int ahora = ParadaCercana(montado.Along);
            if (montado.Espera > 0f && ahora >= 0)
            {
                string linea = LineaParada(ahora);
                if (avisoTexto != linea)
                {
                    avisoTexto = linea;
                    CajamarcaCalles.Decir(linea);
                }
            }

            if (!Input.GetKeyDown(KeyCode.Return) && !Input.GetKeyDown(KeyCode.KeypadEnter)) return;
            if (montado.Espera > 0f)
                Bajar();
            else
            {
                montado.QuiereBajar = true;
                CajamarcaCalles.Decir(LeyDelMonte.BajaProxima);
            }
        }

        void Bajar()
        {
            if (montado == null || montado.Cuerpo == null)
            {
                Soltar();
                return;
            }

            Vector3 lado = montado.Cuerpo.localRotation * new Vector3(-2.4f, 0f, 0f);
            Vector3 pies = montado.Cuerpo.localPosition + lado;
            pies.y = SueloDeLaCalle.Altura(pies.x, pies.z);
            int parada = ParadaCercana(montado.Along);
            montado.QuiereBajar = false;
            Soltar();
            CajamarcaCalles.PonerHeroe(pies);
            if (parada == 1) CajamarcaCalles.Decir(LeyDelMonte.BajaUnc);
            else if (parada == 2) CajamarcaCalles.Decir(LeyDelMonte.BajaBanos);
            else CajamarcaCalles.Decir(LeyDelMonte.BajaPlaza);
        }

        void Soltar()
        {
            Abordo = false;
            montado = null;
            aviso = false;
            avisoTexto = null;
            CajamarcaCalles.FijarTienda(false);
            LluviaDeAlmas.Pausa(false);
        }

        int ParadaCercana(float along)
        {
            int best = 0;
            float mejor = float.MaxValue;
            for (int i = 0; i < paradas.Length; i++)
            {
                float d = Mathf.Abs(paradas[i].Dist - along);
                if (d < mejor)
                {
                    mejor = d;
                    best = i;
                }
            }

            return mejor < 14f ? best : -1;
        }

        string LineaParada(int i)
        {
            if (i == 1) return LeyDelMonte.ParadaUnc;
            if (i == 2) return LeyDelMonte.ParadaBanos;
            return LeyDelMonte.ParadaPlaza;
        }

        static Transform Caja(Transform parent, string name, Vector3 pos, Vector3 size, string tex, Color tinta, bool solido = true)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localScale = size;
            var col = go.GetComponent<Collider>();
            if (col != null && !solido) Object.Destroy(col);
            var renderer = go.GetComponent<Renderer>();
            var shader = Shader.Find("AYNI/Calle") ?? Shader.Find("Sprites/Default");
            var mat = new Material(shader);
            if (!string.IsNullOrEmpty(tex))
            {
                var texture = Resources.Load<Texture2D>(tex);
                if (texture != null)
                {
                    texture.wrapMode = TextureWrapMode.Repeat;
                    mat.mainTexture = texture;
                }
            }

            mat.color = tinta;
            renderer.sharedMaterial = mat;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            return go.transform;
        }

        static void Texto(Transform parent, string name, Vector3 pos, string linea, float size)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            var mesh = go.AddComponent<TextMesh>();
            mesh.text = linea;
            mesh.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            mesh.characterSize = size;
            mesh.fontSize = 48;
            mesh.anchor = TextAnchor.MiddleCenter;
            mesh.alignment = TextAlignment.Center;
            mesh.color = new Color(0.97f, 0.94f, 0.82f);
            go.AddComponent<Billboard>();
        }

        static float Largo(Vector2[] pts)
        {
            float s = 0f;
            for (int i = 1; i < pts.Length; i++)
                s += Vector2.Distance(pts[i - 1], pts[i]);
            return s;
        }

        static float DistHasta(Vector2[] pts, float x, float z)
        {
            float acc = 0f;
            var meta = new Vector2(x, z);
            float mejor = float.MaxValue;
            float at = 0f;
            for (int i = 1; i < pts.Length; i++)
            {
                float len = Vector2.Distance(pts[i - 1], pts[i]);
                for (int s = 0; s <= 8; s++)
                {
                    float t = s / 8f;
                    var p = Vector2.Lerp(pts[i - 1], pts[i], t);
                    float d = (p - meta).sqrMagnitude;
                    if (d < mejor)
                    {
                        mejor = d;
                        at = acc + len * t;
                    }
                }

                acc += len;
            }

            return at;
        }

        static void Punto(Vector2[] pts, float dist, out float x, out float z, out float tx, out float tz)
        {
            x = pts[0].x;
            z = pts[0].y;
            tx = 1f;
            tz = 0f;
            if (pts.Length < 2) return;
            float acc = 0f;
            for (int i = 1; i < pts.Length; i++)
            {
                float dx = pts[i].x - pts[i - 1].x;
                float dz = pts[i].y - pts[i - 1].y;
                float len = Mathf.Sqrt(dx * dx + dz * dz);
                if (len < 0.001f) continue;
                if (acc + len >= dist || i == pts.Length - 1)
                {
                    float t = Mathf.Clamp01((dist - acc) / Mathf.Max(0.001f, len));
                    x = pts[i - 1].x + dx * t;
                    z = pts[i - 1].y + dz * t;
                    tx = dx / len;
                    tz = dz / len;
                    return;
                }

                acc += len;
            }
        }
    }
}
