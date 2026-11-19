using System.Collections.Generic;
using AYNI.Narrative;
using UnityEngine;
using UnityEngine.Rendering;

namespace AYNI.Battle
{
    /// <summary>
    /// Las casonas principales de la plaza se entran con ENTER.
    /// Adentro hay un patio colonial: corredor, jardín y pileta.
    /// </summary>
    public class CasonasDelCamino : MonoBehaviour
    {
        const float Alcance = 3.4f;
        const float Paso = 5.2f;

        public struct Puerta
        {
            public Vector3 Local;
            public string Nombre;
            public Color Tinta;
            public string Fachada;
        }

        sealed class Casa
        {
            public Puerta Puerta;
            public Vector3 Origen;
            public Vector3 Regreso;
        }

        static readonly List<Puerta> pendientes = new List<Puerta>();
        static CasonasDelCamino actual;

        readonly List<Casa> casas = new List<Casa>();
        Casa abierta;
        bool aviso;

        public static bool Adentro { get; private set; }
        public static bool JuntoAPuerta { get; private set; }

        public static void Registrar(Vector3 local, string nombre, Color tinta, string fachada)
        {
            pendientes.Add(new Puerta
            {
                Local = local,
                Nombre = nombre,
                Tinta = tinta,
                Fachada = fachada,
            });
        }

        public static void Preparar(Transform district)
        {
            if (district == null) return;
            if (district.Find("Casonas") != null)
            {
                pendientes.Clear();
                return;
            }

            var go = new GameObject("Casonas");
            go.transform.SetParent(district, false);
            go.AddComponent<CasonasDelCamino>().Armar();
        }

        public static bool ConsumeEscape()
        {
            if (!Adentro || actual == null) return false;
            actual.Salir();
            return true;
        }

        void OnEnable()
        {
            actual = this;
        }

        void Armar()
        {
            for (int i = 0; i < pendientes.Count; i++)
            {
                var puerta = pendientes[i];
                var origen = new Vector3(-2100f, 4.2f, -2100f - i * 48f);
                ConstruirPatio(i, origen, puerta);
                casas.Add(new Casa { Puerta = puerta, Origen = origen });
            }

            pendientes.Clear();
        }

        void ConstruirPatio(int id, Vector3 origen, Puerta puerta)
        {
            var root = new GameObject("Patio_" + id);
            root.transform.SetParent(transform, false);
            root.transform.localPosition = origen;

            const float patio = 9.2f;
            const float corredor = 2.6f;
            const float alto = 4.4f;
            float total = patio + corredor * 2f;
            var tinta = puerta.Tinta;

            Caja(root.transform, "Piso", new Vector3(0f, -0.06f, 0f), new Vector3(total, 0.14f, total), "Calle/piedra", Color.white);
            Caja(root.transform, "Jardin", new Vector3(0f, 0.04f, 0f), new Vector3(patio - 1.2f, 0.08f, patio - 1.2f), "Bosque/piso_cesped", new Color(0.82f, 0.94f, 0.66f));
            Caja(root.transform, "Pileta", new Vector3(0f, 0.28f, 0f), new Vector3(2.4f, 0.42f, 2.4f), "Calle/piedra", Color.white);
            Caja(root.transform, "Agua", new Vector3(0f, 0.52f, 0f), new Vector3(1.55f, 0.08f, 1.55f), null, new Color(0.32f, 0.52f, 0.62f));

            float muro = total * 0.5f;
            Caja(root.transform, "Norte", new Vector3(0f, alto * 0.5f, muro), new Vector3(total, alto, 0.32f), "Calle/muro_adobe", tinta);
            Caja(root.transform, "Este", new Vector3(muro, alto * 0.5f, 0f), new Vector3(0.32f, alto, total), "Calle/muro_adobe", tinta);
            Caja(root.transform, "Oeste", new Vector3(-muro, alto * 0.5f, 0f), new Vector3(0.32f, alto, total), "Calle/muro_adobe", tinta);
            Caja(root.transform, "SurIzq", new Vector3(-(muro + 1.4f) * 0.5f, alto * 0.5f, -muro), new Vector3(total * 0.5f - 1.4f, alto, 0.32f), "Calle/muro_adobe", tinta);
            Caja(root.transform, "SurDer", new Vector3((muro + 1.4f) * 0.5f, alto * 0.5f, -muro), new Vector3(total * 0.5f - 1.4f, alto, 0.32f), "Calle/muro_adobe", tinta);
            Caja(root.transform, "Dintel", new Vector3(0f, alto - 0.55f, -muro), new Vector3(2.9f, 1.1f, 0.32f), "Calle/muro_adobe", tinta);
            Caja(root.transform, "Techo", new Vector3(0f, alto + 0.18f, 0f), new Vector3(total + 0.8f, 0.28f, total + 0.8f), "Calle/teja", Color.white);

            for (int i = 0; i < 8; i++)
            {
                float a = (i + 0.5f) * Mathf.PI * 0.25f;
                float r = patio * 0.5f + 0.35f;
                Caja(root.transform, "Columna", new Vector3(Mathf.Cos(a) * r, 1.7f, Mathf.Sin(a) * r),
                    new Vector3(0.32f, 3.4f, 0.32f), "Calle/piedra", Color.white);
            }

            Caja(root.transform, "BarandaN", new Vector3(0f, 3.55f, patio * 0.5f + 0.2f), new Vector3(patio, 0.12f, 0.12f), "Calle/madera_oscura", Color.white);
            Caja(root.transform, "BarandaS", new Vector3(0f, 3.55f, -patio * 0.5f - 0.2f), new Vector3(patio, 0.12f, 0.12f), "Calle/madera_oscura", Color.white);
            Caja(root.transform, "BarandaE", new Vector3(patio * 0.5f + 0.2f, 3.55f, 0f), new Vector3(0.12f, 0.12f, patio), "Calle/madera_oscura", Color.white);
            Caja(root.transform, "BarandaO", new Vector3(-patio * 0.5f - 0.2f, 3.55f, 0f), new Vector3(0.12f, 0.12f, patio), "Calle/madera_oscura", Color.white);

            Maceta(root.transform, new Vector3(2.1f, 0.22f, 1.6f));
            Maceta(root.transform, new Vector3(-2.1f, 0.22f, 1.6f));
            Maceta(root.transform, new Vector3(2.1f, 0.22f, -1.4f));
            Maceta(root.transform, new Vector3(-2.1f, 0.22f, -1.4f));

            Caja(root.transform, "FotoPatio", new Vector3(0f, 2.05f, muro - 0.22f),
                new Vector3(patio + 1.2f, 3.6f, 0.08f), "Calle/Fachadas/patio", Color.white);

            var letrero = new GameObject("Nombre");
            letrero.transform.SetParent(root.transform, false);
            letrero.transform.localPosition = new Vector3(0f, 3.9f, -muro + 0.4f);
            var mesh = letrero.AddComponent<TextMesh>();
            mesh.text = puerta.Nombre;
            mesh.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            mesh.characterSize = 0.09f;
            mesh.fontSize = 48;
            mesh.anchor = TextAnchor.MiddleCenter;
            mesh.color = new Color(0.96f, 0.90f, 0.74f);
            letrero.AddComponent<Billboard>();

            SolidoDelCamino.Armar(root.transform);
        }

        static void Maceta(Transform parent, Vector3 at)
        {
            Caja(parent, "Maceta", at, new Vector3(0.55f, 0.42f, 0.55f), "Calle/piedra", new Color(0.55f, 0.32f, 0.18f));
            Caja(parent, "Planta", at + Vector3.up * 0.45f, new Vector3(0.28f, 0.55f, 0.28f), "Bosque/piso_cesped", new Color(0.28f, 0.52f, 0.22f));
        }

        static Transform Caja(Transform parent, string name, Vector3 pos, Vector3 size, string tex, Color tinta)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localScale = size;
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

        void Update()
        {
            if (!CajamarcaCalles.IsOpen || CaminoRelato.Reading || RutaHaciaBanos.Abordo)
            {
                JuntoAPuerta = false;
                return;
            }

            if (!CajamarcaCalles.LeerHeroe(out Vector3 pies, out Transform cuerpo, out SpriteFrameAnimator anim))
            {
                JuntoAPuerta = false;
                return;
            }

            if (Adentro)
            {
                Mover(cuerpo, anim);
                return;
            }

            if (RutaHaciaBanos.JuntoAlParadero)
            {
                JuntoAPuerta = false;
                return;
            }

            Vigilar(pies);
        }

        void Vigilar(Vector3 pies)
        {
            Casa cerca = null;
            float mejor = Alcance * Alcance;
            for (int i = 0; i < casas.Count; i++)
            {
                Vector3 gap = casas[i].Puerta.Local - pies;
                gap.y = 0f;
                float d = gap.sqrMagnitude;
                if (d < mejor)
                {
                    mejor = d;
                    cerca = casas[i];
                }
            }

            JuntoAPuerta = cerca != null;
            if (cerca == null)
            {
                aviso = false;
                return;
            }

            if (!aviso)
            {
                aviso = true;
                CajamarcaCalles.Decir(LeyDelMonte.CasonaCerca);
            }

            if (CajamarcaCalles.Editando) return;
            if (!Input.GetKeyDown(KeyCode.Return) && !Input.GetKeyDown(KeyCode.KeypadEnter)) return;
            Entrar(cerca);
        }

        void Entrar(Casa casa)
        {
            if (!CajamarcaCalles.LeerHeroe(out Vector3 pies, out _, out _)) return;
            abierta = casa;
            casa.Regreso = pies;
            Adentro = true;
            JuntoAPuerta = false;
            aviso = false;
            CajamarcaCalles.FijarTienda(true);
            CajamarcaCalles.AsegurarInclinacion();
            CajamarcaCalles.Acercar(true);
            LluviaDeAlmas.Pausa(true);
            float muro = (9.2f + 2.6f * 2f) * 0.5f;
            CajamarcaCalles.PonerHeroe(casa.Origen + new Vector3(0f, 0f, -muro + 1.6f));
            CajamarcaCalles.Decir(LeyDelMonte.CasonaPatio);
        }

        void Mover(Transform cuerpo, SpriteFrameAnimator anim)
        {
            if (abierta == null) return;
            Vector3 deseo = CajamarcaCalles.DeseoLocal();
            Vector3 mundo = CajamarcaCalles.DeseoMundo();
            if (!CajamarcaCalles.LeerHeroe(out Vector3 pies, out _, out _)) return;
            Vector3 next = pies + deseo * (Paso * Time.deltaTime);
            Vector3 o = abierta.Origen;
            const float patio = 9.2f;
            const float corredor = 2.6f;
            float half = (patio + corredor * 2f) * 0.5f - 0.7f;
            next.x = Mathf.Clamp(next.x, o.x - half, o.x + half);
            next.z = Mathf.Clamp(next.z, o.z - half, o.z + half);
            next.y = o.y;
            bool moving = (next - pies).sqrMagnitude > 0.0001f;
            CajamarcaCalles.PonerHeroe(next);
            anim?.PlayStep(mundo, moving && deseo.sqrMagnitude > 0.01f);
        }

        void Salir()
        {
            if (abierta == null) return;
            Vector3 puerta = abierta.Puerta.Local;
            abierta = null;
            Adentro = false;
            aviso = false;
            CajamarcaCalles.PonerHeroe(puerta);
            CajamarcaCalles.FijarTienda(false);
            CajamarcaCalles.Acercar(false);
            LluviaDeAlmas.Pausa(false);
            CajamarcaCalles.Decir(LeyDelMonte.CasonaSale);
        }
    }
}
