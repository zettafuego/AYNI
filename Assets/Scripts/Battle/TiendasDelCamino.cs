using System.Collections.Generic;
using AYNI.Narrative;
using UnityEngine;

namespace AYNI.Battle
{
    /// <summary>
    /// Las tiendas de la feria y una casa de la esquina se entran con ENTER.
    /// Adentro hay un alma, o la calavera, y la puerta no abre hasta enfrentarla.
    /// </summary>
    public class TiendasDelCamino : MonoBehaviour
    {
        const float Alcance = 3.1f;
        const float Paso = 4.4f;

        sealed class Local
        {
            public string Rotulo;
            public string Entrada;
            public string PremioTexto;
            public CharacterSheet RivalHoja;
            public int Premio;
            public bool Clara;
            public readonly List<Vector3> Puertas = new List<Vector3>();
        }

        readonly List<Local> locales = new List<Local>();
        Vector3 origen;
        Vector3 regreso;
        Local abierta;
        GameObject rival;
        SpriteFrameAnimator rivalAnim;
        int vida = 3;
        int vidaRival = 3;
        float golpeHeroe;
        float golpeRival;
        bool rivalVivo;
        bool pagado;
        bool aviso;
        string dicho = "";

        public static bool Adentro { get; private set; }
        public static bool JuntoAPuerta { get; private set; }
        public static bool HayQuePelear => actual != null && actual.rivalVivo;

        Local mira;

        public static void PulsarEntrar()
        {
            if (CajamarcaCalles.Editando) return;
            if (actual == null || Adentro || actual.mira == null) return;
            actual.Entrar(actual.mira);
        }

        public static void PulsarGolpe()
        {
            if (actual == null || !Adentro) return;
            if (!CajamarcaCalles.LeerHeroe(out _, out Transform cuerpo, out SpriteFrameAnimator anim)) return;
            actual.Golpear(cuerpo, anim);
        }

        public static void PulsarSalir()
        {
            if (actual == null || !Adentro) return;
            actual.Salir(false);
        }

        static TiendasDelCamino actual;

        public static void Preparar(Transform parent)
        {
            if (parent == null || parent.Find("Tiendas") != null) return;
            var go = new GameObject("Tiendas");
            go.transform.SetParent(parent, false);
            go.AddComponent<TiendasDelCamino>().Armar(parent);
        }

        public static bool ConsumeEscape()
        {
            if (!Adentro || actual == null) return false;
            actual.Salir(false);
            return true;
        }

        void OnEnable()
        {
            actual = this;
        }

        void Armar(Transform parent)
        {
            Sumar(parent, "flores", "FLORES", BattleCast.AlmaRosa, 0, LeyDelMonte.TiendaAlma, LeyDelMonte.TiendaVida);
            Sumar(parent, "frutas", "FRUTAS", BattleCast.AlmaAzul, 1, LeyDelMonte.TiendaAlma, LeyDelMonte.TiendaAyni);
            Sumar(parent, "comida", "COMIDA", BattleCast.AlmaOro, 2, LeyDelMonte.TiendaAlma, LeyDelMonte.TiendaPan);
            PonerCasa(parent);
            origen = new Vector3(-920f, 24f, -920f);
            ConstruirSala();
        }

        void Sumar(Transform parent, string kind, string rotulo, CharacterSheet hoja, int premio, string entrada, string premioTexto)
        {
            var local = new Local
            {
                Rotulo = rotulo,
                RivalHoja = hoja,
                Premio = premio,
                Entrada = entrada,
                PremioTexto = premioTexto,
            };
            for (int i = 0; i < parent.childCount; i++)
            {
                var child = parent.GetChild(i);
                if (child.name != "Puesto_" + kind) continue;
                Transform piso = parent.parent != null ? parent.parent : parent;
                local.Puertas.Add(piso.InverseTransformPoint(child.position));
                if (local.Puertas.Count == 1) Letrero(child, rotulo);
            }

            if (local.Puertas.Count > 0) locales.Add(local);
        }

        void PonerCasa(Transform parent)
        {
            Vector3 at = locales.Count > 0 ? locales[0].Puertas[0] + new Vector3(7.2f, 0f, 1.4f) : new Vector3(8f, 0f, -8f);
            at.y = CajamarcaAltura.Sample(at.x, at.z);
            var casa = new GameObject("CasaEsquina");
            casa.transform.SetParent(parent, false);
            casa.transform.localPosition = at;

            Caja(casa.transform, "Suelo", new Vector3(0f, 0.08f, 0f), new Vector3(4.6f, 0.16f, 4.2f), "Calle/piso_empedrado");
            Caja(casa.transform, "MuroNorte", new Vector3(0f, 1.45f, 1.95f), new Vector3(4.6f, 2.9f, 0.28f), "Calle/muro_adobe");
            Caja(casa.transform, "MuroOeste", new Vector3(-2.16f, 1.45f, 0f), new Vector3(0.28f, 2.9f, 4.2f), "Calle/muro_adobe");
            Caja(casa.transform, "MuroEste", new Vector3(2.16f, 1.45f, 0f), new Vector3(0.28f, 2.9f, 4.2f), "Calle/muro_adobe");
            Caja(casa.transform, "JambaIzq", new Vector3(-1.45f, 1.45f, -1.95f), new Vector3(1.7f, 2.9f, 0.28f), "Calle/muro_adobe");
            Caja(casa.transform, "JambaDer", new Vector3(1.45f, 1.45f, -1.95f), new Vector3(1.7f, 2.9f, 0.28f), "Calle/muro_adobe");
            Caja(casa.transform, "Dintel", new Vector3(0f, 2.55f, -1.95f), new Vector3(1.4f, 0.7f, 0.28f), "Calle/muro_adobe");
            Caja(casa.transform, "Techo", new Vector3(0f, 3.05f, 0f), new Vector3(5f, 0.22f, 4.6f), "Calle/teja");
            Caja(casa.transform, "Hoja", new Vector3(0f, 1.05f, -1.82f), new Vector3(1.15f, 2.1f, 0.08f), "Calle/madera_oscura");
            Letrero(casa.transform, "CASA");

            var local = new Local
            {
                Rotulo = "CASA",
                RivalHoja = BattleCast.Calavera,
                Premio = 2,
                Entrada = LeyDelMonte.TiendaCalavera,
                PremioTexto = LeyDelMonte.TiendaCasa,
            };
            local.Puertas.Add(at + new Vector3(0f, 0f, -2.8f));
            locales.Add(local);
        }

        void ConstruirSala()
        {
            var sala = new GameObject("Sala");
            sala.transform.SetParent(transform, false);
            sala.transform.localPosition = origen;
            Caja(sala.transform, "Piso", new Vector3(0f, -0.1f, 0.3f), new Vector3(8.2f, 0.2f, 7.4f), "Calle/piso_plaza");
            Caja(sala.transform, "Norte", new Vector3(0f, 1.6f, 3.85f), new Vector3(8.2f, 3.4f, 0.28f), "Calle/muro_adobe");
            Caja(sala.transform, "Oeste", new Vector3(-4f, 1.6f, 0.3f), new Vector3(0.28f, 3.4f, 7.4f), "Calle/muro_adobe");
            Caja(sala.transform, "Este", new Vector3(4f, 1.6f, 0.3f), new Vector3(0.28f, 3.4f, 7.4f), "Calle/muro_adobe");
            Caja(sala.transform, "SurIzq", new Vector3(-2.55f, 1.6f, -3.25f), new Vector3(3.1f, 3.4f, 0.28f), "Calle/muro_adobe");
            Caja(sala.transform, "SurDer", new Vector3(2.55f, 1.6f, -3.25f), new Vector3(3.1f, 3.4f, 0.28f), "Calle/muro_adobe");
            Caja(sala.transform, "Techo", new Vector3(0f, 3.35f, 0.3f), new Vector3(8.6f, 0.2f, 7.8f), "Calle/teja");
            Caja(sala.transform, "Mostrador", new Vector3(0f, 0.5f, 2.35f), new Vector3(3.4f, 1f, 0.7f), "Calle/madera_oscura");
        }

        static Transform Caja(Transform parent, string name, Vector3 pos, Vector3 size, string tex)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localScale = size;
            var renderer = go.GetComponent<Renderer>();
            var shader = Shader.Find("AYNI/Calle") ?? Shader.Find("Sprites/Default");
            var mat = new Material(shader);
            var texture = Resources.Load<Texture2D>(tex);
            if (texture != null)
            {
                texture.wrapMode = TextureWrapMode.Repeat;
                mat.mainTexture = texture;
                mat.mainTextureScale = new Vector2(Mathf.Max(1f, size.x), Mathf.Max(1f, size.y));
            }

            renderer.sharedMaterial = mat;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return go.transform;
        }

        static void Letrero(Transform parent, string texto)
        {
            var board = Caja(parent, "Letrero", new Vector3(0f, 2.45f, -0.2f), new Vector3(1.35f, 0.42f, 0.08f), "Calle/madera_oscura");
            var go = new GameObject("Nombre");
            go.transform.SetParent(board, false);
            go.transform.localPosition = new Vector3(0f, 0f, -0.62f);
            var mesh = go.AddComponent<TextMesh>();
            mesh.text = texto;
            mesh.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            mesh.characterSize = 0.07f;
            mesh.fontSize = 64;
            mesh.anchor = TextAnchor.MiddleCenter;
            mesh.alignment = TextAlignment.Center;
            mesh.color = new Color(0.96f, 0.91f, 0.78f);
            go.AddComponent<Billboard>();
        }

        void Update()
        {
            if (!CajamarcaCalles.IsOpen || CaminoRelato.Reading)
            {
                JuntoAPuerta = false;
                return;
            }

            if (!CajamarcaCalles.LeerHeroe(out Vector3 pies, out Transform cuerpo, out SpriteFrameAnimator anim))
            {
                JuntoAPuerta = false;
                return;
            }

            if (CasonasDelCamino.Adentro || CasonasDelCamino.JuntoAPuerta
                || RutaHaciaBanos.Abordo || RutaHaciaBanos.JuntoAlParadero)
            {
                JuntoAPuerta = false;
                return;
            }

            if (!Adentro)
            {
                Vigilar(pies);
                return;
            }

            Mover(cuerpo, anim);
            Rival(cuerpo, anim);
        }

        void Vigilar(Vector3 pies)
        {
            Local cerca = null;
            float mejor = Alcance * Alcance;
            for (int i = 0; i < locales.Count; i++)
            {
                var local = locales[i];
                for (int p = 0; p < local.Puertas.Count; p++)
                {
                    Vector3 gap = local.Puertas[p] - pies;
                    gap.y = 0f;
                    float d = gap.sqrMagnitude;
                    if (d < mejor)
                    {
                        mejor = d;
                        cerca = local;
                    }
                }
            }

            mira = cerca;
            JuntoAPuerta = cerca != null;
            if (cerca == null)
            {
                aviso = false;
                return;
            }

            if (!aviso)
            {
                aviso = true;
                CajamarcaCalles.Decir(LeyDelMonte.TiendaCerca);
            }

            if (CajamarcaCalles.Editando) return;
            if (!Input.GetKeyDown(KeyCode.Return) && !Input.GetKeyDown(KeyCode.KeypadEnter)) return;
            Entrar(cerca);
        }

        void Entrar(Local local)
        {
            if (!CajamarcaCalles.LeerHeroe(out Vector3 pies, out _, out _)) return;
            abierta = local;
            regreso = pies;
            Adentro = true;
            JuntoAPuerta = false;
            pagado = local.Clara;
            vida = 3;
            aviso = false;
            CajamarcaCalles.FijarTienda(true);
            CajamarcaCalles.AsegurarInclinacion();
            CajamarcaCalles.Acercar(true);
            LluviaDeAlmas.Pausa(true);
            CajamarcaCalles.PonerHeroe(origen + new Vector3(0f, 0f, -2.15f));
            if (rival != null) Destroy(rival);
            rival = null;
            rivalVivo = false;
            if (!local.Clara) SoltarRival(local);
            else Decir(LeyDelMonte.TiendaClara);
            if (!local.Clara) Decir(local.Entrada + "  Tú 3 · " + local.RivalHoja.Name + " 3");
        }

        void SoltarRival(Local local)
        {
            rival = new GameObject("RivalDeTienda");
            rival.transform.SetParent(transform, false);
            float scale = BattleCast.BodyScale(local.RivalHoja) * 0.35f;
            rival.transform.localScale = new Vector3(Mathf.Abs(scale), Mathf.Abs(scale), 1f);
            var ink = rival.AddComponent<SpriteRenderer>();
            ink.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            ink.sortingOrder = 30;
            rivalAnim = rival.AddComponent<SpriteFrameAnimator>();
            BattleSprites.Apply(rivalAnim, local.RivalHoja);
            rival.AddComponent<Billboard>();
            vidaRival = 3;
            rivalVivo = true;
            rival.transform.localPosition = origen + new Vector3(0f, 0f, 1.15f);
            rivalAnim.PlayStep(Vector3.zero, false);
        }

        void Mover(Transform cuerpo, SpriteFrameAnimator anim)
        {
            Vector3 deseo = CajamarcaCalles.DeseoLocal();
            Vector3 mundo = CajamarcaCalles.DeseoMundo();
            if (!CajamarcaCalles.LeerHeroe(out Vector3 pies, out _, out _)) return;
            Vector3 next = pies + deseo * (Paso * Time.deltaTime);
            next.y = origen.y;
            float minZ = rivalVivo ? -1.55f : -2.7f;
            next.x = Mathf.Clamp(next.x, origen.x - 3.45f, origen.x + 3.45f);
            next.z = Mathf.Clamp(next.z, origen.z + minZ, origen.z + 1.85f);
            bool moving = (next - pies).sqrMagnitude > 0.0001f;
            CajamarcaCalles.PonerHeroe(next);
            anim?.PlayStep(mundo, moving && deseo.sqrMagnitude > 0.01f);
            if (!rivalVivo && (abierta == null || abierta.Clara) && next.z <= origen.z - 2.45f)
                Salir(true);
        }

        void Rival(Transform cuerpo, SpriteFrameAnimator anim)
        {
            if (abierta != null && abierta.Clara && !rivalVivo)
            {
                if (Input.GetKeyDown(KeyCode.F)) anim?.PlayAction();
                return;
            }

            if (rival == null || cuerpo == null) return;
            Vector3 a = cuerpo.position;
            Vector3 b = rival.transform.position;
            Vector3 gap = a - b;
            gap.y = 0f;
            float dist = gap.magnitude;
            var cam = Camera.main;
            if (cam != null && rivalAnim != null)
            {
                Vector3 right = cam.transform.right;
                right.y = 0f;
                rivalAnim.SetFacing(Vector3.Dot(gap, right) > 0f);
            }

            if (rivalVivo && rivalAnim != null)
            {
                Vector3 step = dist > 1.15f ? gap.normalized * (1.65f * Time.deltaTime) : Vector3.zero;
                Vector3 pos = rival.transform.localPosition + step;
                pos.x = Mathf.Clamp(pos.x, origen.x - 3.1f, origen.x + 3.1f);
                pos.z = Mathf.Clamp(pos.z, origen.z - 1.2f, origen.z + 1.7f);
                pos.y = origen.y;
                rival.transform.localPosition = pos;
                rivalAnim.PlayStep(step, step.sqrMagnitude > 0.0001f);
            }

            if (Input.GetKeyDown(KeyCode.F)) Golpear(cuerpo, anim);

            if (rivalVivo && dist < 1.2f && Time.time >= golpeHeroe)
            {
                golpeHeroe = Time.time + 1.05f;
                vida--;
                anim?.PlayHurt();
                if (vida <= 0)
                {
                    Decir(LeyDelMonte.TiendaPierdes);
                    Salir(false);
                    return;
                }

                Decir(abierta.Entrada + "  Tú " + vida + " · " + abierta.RivalHoja.Name + " " + vidaRival);
            }
        }

        void Golpear(Transform cuerpo, SpriteFrameAnimator anim)
        {
            anim?.PlayAction();
            if (rival == null || cuerpo == null || abierta == null || !rivalVivo) return;
            Vector3 gap = cuerpo.position - rival.transform.position;
            gap.y = 0f;
            float dist = gap.magnitude;
            if (dist >= 1.85f || Time.time < golpeRival) return;
            golpeRival = Time.time + 0.48f;
            vidaRival--;
            if (vidaRival <= 0) Caer();
            else rivalAnim?.PlayHurt();
            Decir(abierta.Entrada + "  Tú " + vida + " · " + abierta.RivalHoja.Name + " " + Mathf.Max(0, vidaRival));
        }

        void Caer()
        {
            rivalVivo = false;
            rivalAnim?.PlayDeath();
            abierta.Clara = true;
            if (!pagado)
            {
                pagado = true;
                if (abierta.Premio == 0)
                {
                    ReservaDelCamino.SumarVida();
                    AlforjaDelCamino.Guardar("flor", "Flor", LeyDelMonte.AlforjaFlor);
                }
                else if (abierta.Premio == 1)
                {
                    ReservaDelCamino.SumarAyni();
                    AlforjaDelCamino.Guardar("fruta", "Fruta", LeyDelMonte.AlforjaFruta);
                }
                else
                {
                    ReservaDelCamino.SumarReputacion();
                    AlforjaDelCamino.Guardar("pan", "Pan", LeyDelMonte.AlforjaPan);
                }
            }

            Decir(LeyDelMonte.TiendaGanas + " " + abierta.PremioTexto);
        }

        void Decir(string texto)
        {
            if (texto == dicho) return;
            dicho = texto;
            CajamarcaCalles.Decir(texto);
        }

        void Salir(bool limpia)
        {
            bool huir = !limpia && vida > 0 && abierta != null && !abierta.Clara;
            Adentro = false;
            dicho = "";
            if (rival != null) Destroy(rival);
            rival = null;
            rivalVivo = false;
            LluviaDeAlmas.Pausa(false);
            CajamarcaCalles.Acercar(false);
            CajamarcaCalles.FijarTienda(false);
            Vector3 volver = regreso;
            volver.y = SueloDeLaCalle.Altura(volver.x, volver.z);
            CajamarcaCalles.PonerHeroe(volver);
            if (huir)
                CajamarcaCalles.Decir(LeyDelMonte.TiendaHuyes);
            abierta = null;
            mira = null;
            aviso = false;
            JuntoAPuerta = false;
        }

        void OnDestroy()
        {
            if (actual == this) actual = null;
            Adentro = false;
            CajamarcaCalles.FijarTienda(false);
            LluviaDeAlmas.Pausa(false);
        }
    }
}
