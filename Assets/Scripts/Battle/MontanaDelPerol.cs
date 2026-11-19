using System.Collections.Generic;
using AYNI.Narrative;
using AYNI.UI;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace AYNI.Battle
{
    /// <summary>
    /// La montaña, la Laguna El Perol y la mina, en un paseo en 3D.
    /// El agua se cruza nadando. Con la lámpara se entra a la labor:
    /// subniveles cada 50 m, sección 3.5 x 3.5 m y rampa al 15 %.
    /// El toro de la orilla abre la pelea de lado.
    /// </summary>
    public class MontanaDelPerol : MonoBehaviour
    {
        enum Zona
        {
            Tierra,
            Superficie,
            Pozo,
            Galeria,
        }

        enum AireMina
        {
            Quieto,
            Cuenta,
            Gases,
            Limpio,
        }

        struct Pose
        {
            public Vector3 Position;
            public Quaternion Rotation;
            public bool Orthographic;
            public float Size;
            public float Field;
            public float Near;
            public float Far;
            public Color Background;
            public CameraClearFlags Clear;
        }

        const float LagoR = 17f;
        const float PozoR = 3.35f;
        const float AguaY = 0.22f;
        const float CuerpoAlto = 1.85f;
        const float Hundido = 1.05f;
        const float Fondo = -15.8f;
        const float MundoX = 6800f;

        static readonly Color Cielo = new Color(0.18f, 0.32f, 0.42f);
        static readonly Color Pasto = new Color(0.34f, 0.48f, 0.22f);
        static readonly Color RocaTinta = new Color(0.55f, 0.52f, 0.46f);
        static readonly Color Piedra = new Color(0.42f, 0.4f, 0.36f);
        static readonly Color Madera = new Color(0.62f, 0.46f, 0.28f);
        static readonly Color Oro = new Color(1f, 0.82f, 0.22f);

        static readonly string[] Arboles =
        {
            "Bosque/tree_tall",
            "Bosque/tree_oak",
            "Bosque/tree_detailed",
            "Bosque/tree_pineTallA",
            "Bosque/tree_default",
            "Bosque/tree_pineTallB",
            "Bosque/tree_oak_dark",
            "Bosque/tree_pineTallC",
            "Bosque/tree_simple",
            "Bosque/tree_pineDefaultA",
            "Bosque/tree_default_dark",
            "Bosque/tree_pineTallD",
            "Bosque/tree_small",
            "Bosque/tree_pineSmallA",
        };

        static readonly string[] Rocas =
        {
            "Bosque/rock_largeA",
            "Bosque/rock_largeB",
            "Bosque/rock_largeD",
            "Bosque/rock_tallA",
            "Bosque/rock_tallB",
            "Bosque/rock_smallA",
        };

        static readonly Dictionary<string, Color> Hojas = new Dictionary<string, Color>
        {
            { "leafsGreen", new Color(0.30f, 0.52f, 0.22f) },
            { "leafsDark", new Color(0.22f, 0.40f, 0.16f) },
            { "woodBark", new Color(0.52f, 0.36f, 0.22f) },
            { "woodBarkDark", new Color(0.42f, 0.28f, 0.17f) },
        };

        static readonly Dictionary<Material, Material> pintadas = new Dictionary<Material, Material>();

        static MontanaDelPerol actual;

        public static bool IsOpen => actual != null;
        public static bool Junto => actual != null && actual.junto;
        public static bool Ocupa => actual != null && actual.zona != Zona.Tierra;
        public static bool EnGaleria => actual != null && actual.zona == Zona.Galeria;
        public static bool Encendida => actual != null && actual.encendida;
        public static bool CargaOro => actual != null && actual.carga;
        public static bool JuntoAlToro => actual != null && actual.juntoToro;

        LagunaBattleManager battle;
        Transform raiz;
        Transform cuerpo;
        SpriteFrameAnimator anim;
        Camera cam;
        Pose saved;
        bool savedPose;
        bool restored;
        bool snapped;
        bool vivo = true;
        Vector3 local;
        float vy;
        Vector3 empuje;
        bool enSuelo = true;
        bool cayo;
        bool allowJump = true;
        bool jumpLatch = true;
        Zona zona = Zona.Tierra;
        BotonesDeMovimiento movimientos;
        bool planta;
        float planSize = 46f;
        bool planSuelto;
        bool planArrastre;
        Vector2 planMira;
        Vector3 planCursor;
        bool junto;
        bool juntoToro;
        bool tiene;
        bool encendida;
        bool carga;
        bool cobro;
        bool devuelto;
        bool dijoNada;
        bool dijoSinLuz;
        bool dijoAire;
        bool dijoLibre;
        bool dijoQueda;
        bool dijoLejos;
        bool dijoToro;
        float impulso;
        float impulsoX;
        bool haciaBoca;
        bool recienSalio;
        float muquiBaja;
        bool muquiVivo = true;
        AireMina aire = AireMina.Quieto;
        float reloj;
        float sacudida;
        float boloEspera;
        int boloPaso = -1;
        bool tieneReloj;
        bool dijoTope;
        Vector3 lamparaEn;
        Vector3 toroEn;
        Vector3 orillaSalida;

        GameObject lamparaOrilla;
        GameObject fanal;
        GameObject muqui;
        Image velo;
        Image planoMina;
        RectTransform puntoMapa;
        Text linea;
        Text cota;
        Text relojTexto;
        TituloFugaz lineaFade;
        readonly List<Obrero> obreros = new List<Obrero>();
        readonly List<Transform> sentados = new List<Transform>();
        readonly List<Vector3> sentadoEn = new List<Vector3>();
        readonly List<Transform> oros = new List<Transform>();
        readonly List<Vector3> casas = new List<Vector3>();
        readonly CamaraTercera camara = new CamaraTercera
        {
            Pitch = 18f,
            Distance = 8f,
            MinDistance = 3.2f,
            MaxDistance = 22f,
            PivotHeight = 1.05f,
        };

        SpriteRenderer tinta;

        readonly List<GameObject> hudBits = new List<GameObject>();
        readonly List<bool> hudWas = new List<bool>();

        public static void Begin(LagunaBattleManager host)
        {
            if (actual != null || host == null) return;
            if (CajamarcaCalles.IsOpen || BosqueCapitulo.IsOpen || CajamarcaRealCapitulo.IsOpen) return;
            var go = new GameObject("MontanaDelPerol");
            actual = go.AddComponent<MontanaDelPerol>();
            actual.Open(host);
        }

        public static void CloseCurrent()
        {
            if (actual != null) actual.Close();
        }

        public static void PulsarLampara()
        {
            if (actual == null) return;
            if (!actual.tiene)
            {
                if (actual.zona != Zona.Tierra)
                {
                    actual.Lejos();
                    return;
                }

                if (actual.CercaDeLampara()) actual.Tomar();
                return;
            }

            actual.Encender();
        }

        public static void PulsarBajar()
        {
            if (actual == null || actual.zona == Zona.Galeria) return;
            if (actual.zona == Zona.Tierra)
            {
                if (!actual.junto) return;
                actual.haciaBoca = true;
                actual.EntrarSuperficie(actual.local);
                return;
            }

            if (actual.zona == Zona.Superficie)
            {
                actual.haciaBoca = true;
                return;
            }

            actual.impulso = Fondo - actual.local.y - 0.05f;
        }

        public static void PulsarSubir()
        {
            if (actual == null || actual.zona == Zona.Tierra) return;
            if (actual.zona == Zona.Superficie) actual.SalirOrilla();
            else if (actual.zona == Zona.Galeria) actual.impulsoX = 6.5f;
            else actual.impulso = 3.6f;
        }

        public static void PulsarSoltar()
        {
            if (actual == null || !actual.carga) return;
            actual.Soltar();
        }

        public static void EmpezarPelea()
        {
            if (actual == null || actual.zona != Zona.Tierra || !actual.juntoToro) return;
            var host = actual.battle;
            actual.Close();
            host?.StartBattle();
        }

        readonly Vector4[] luzPos = new Vector4[12];
        readonly Vector4[] luzDir = new Vector4[12];
        int luzN;

        void Open(LagunaBattleManager host)
        {
            Shader.SetGlobalFloat("_MinaAmb", 1f);
            Shader.SetGlobalInt("_MinaLuzN", 0);
            battle = host;
            PalabrasCartilla.Close();
            if (battle.startScreen != null) battle.startScreen.SetActive(false);
            OcultarPelea();
            cam = Camera.main;
            GuardarCamara();

            raiz = new GameObject("Perol").transform;
            raiz.SetParent(transform, false);
            raiz.position = new Vector3(MundoX, 0f, 0f);

            local = new Vector3(0f, 0f, -34f);
            Terreno();
            Laguna();
            Socavon();
            Lampara();
            Toro();
            Sembrar();
            Heroe();
            RecordarOro();
            Fanal();
            LluviaDeAlmas.Begin(raiz, cuerpo, (x, z) => Relieve(x, z) + 0.12f, 0.35f);
            Hud();
            SolidoDelCamino.Armar(raiz);
            Jornada.BeginPlay();
            snapped = false;
            battle?.Contar(LeyDelMonte.SceneLine, LeyDelMonte.MinaOrilla);
        }

        void OnDestroy()
        {
            Shader.SetGlobalFloat("_MinaAmb", 1f);
            Shader.SetGlobalInt("_MinaLuzN", 0);
            CamaraTercera.Release();
            if (actual == this) actual = null;
            RestaurarCamara();
        }

        void Close()
        {
            if (!vivo) return;
            vivo = false;
            Shader.SetGlobalFloat("_MinaAmb", 1f);
            Shader.SetGlobalInt("_MinaLuzN", 0);
            CamaraTercera.Release();
            RestaurarCamara();
            RestaurarPelea();
            LluviaDeAlmas.Pausa(false);
            if (carga) PartidaDelCamino.GuardarOro();
            if (battle != null && battle.startScreen != null)
                battle.startScreen.SetActive(true);
            Destroy(gameObject);
        }

        void Update()
        {
            if (!vivo) return;
            if (CaminoRelato.Reading)
            {
                jumpLatch = false;
                CamaraTercera.ShowCursor();
                return;
            }

            if (PartidaDelCamino.EleccionOro == PartidaDelCamino.Oro.Soltado) SeguirElCamino.Tramo();

            allowJump = jumpLatch;
            jumpLatch = true;

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (zona != Zona.Tierra) SalirOrilla();
                else Close();
                return;
            }

            if (zona != Zona.Tierra) planta = false;
            if (Input.GetKeyDown(KeyCode.V)) PonerPlanta(!planta);
            if (Input.GetKeyDown(KeyCode.C) || CamaraTercera.TakeNorth())
            {
                camara.Yaw = 0f;
                CamaraTercera.PlanYaw = 0f;
                planSuelto = false;
                snapped = false;
            }

            if (planta && zona == Zona.Tierra)
            {
                CamaraTercera.Release();
                CamaraTercera.ReadPlanTurn();
                BotonesDeMovimiento.LeerMapa(cam, raiz, ref planSize, 16f, 120f,
                    ref planSuelto, ref planArrastre, ref planMira, ref planCursor,
                    new Vector2(local.x, local.z));
            }
            else
            {
                camara.ReadMouse();
                camara.ReadArrows();
            }
            if (zona == Zona.Galeria)
                camara.Pitch = Mathf.Clamp(camara.Pitch, 5f, 34f);
            else if (zona == Zona.Pozo)
                camara.Pitch = Mathf.Clamp(camara.Pitch, -18f, 68f);
            else
                camara.Pitch = Mathf.Clamp(camara.Pitch, 6f, 72f);

            BrujulaDelMapa.Rumbo = planta && zona == Zona.Tierra ? CamaraTercera.PlanYaw : camara.Yaw;
            movimientos?.Marcar(planta && zona == Zona.Tierra, !(planta && zona == Zona.Tierra));
            BrujulaDelMapa.Mostrar = true;
            MoverObreros();
            PasoBolo();
            TickReloj();
            LluviaDeAlmas.Pausa(zona != Zona.Tierra);

            if (zona == Zona.Tierra) Caminar();
            else Bucear();
            if (!vivo) return;

            Medir();
        }

        void LateUpdate()
        {
            if (!vivo || cam == null || cuerpo == null) return;
            bool mapa = planta && zona == Zona.Tierra;
            cam.orthographic = mapa;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.fieldOfView = zona == Zona.Pozo ? 56f : 50f;
            cam.nearClipPlane = 0.15f;
            cam.farClipPlane = 420f;
            Jornada.TintSky(cam, Cielo);
            Jornada.TintAir(Cielo, new Color(0.72f, 0.76f, 0.74f));
            if (zona == Zona.Galeria)
            {
                RenderSettings.fogColor = new Color(0.012f, 0.011f, 0.01f);
                RenderSettings.fogStartDistance = encendida ? 7.5f : 1f;
                RenderSettings.fogEndDistance = encendida ? 16f : 7f;
                if (aire == AireMina.Gases)
                {
                    RenderSettings.fogColor = new Color(0.28f, 0.2f, 0.1f);
                    RenderSettings.fogStartDistance = 0.8f;
                    RenderSettings.fogEndDistance = 8f;
                }
                cam.farClipPlane = 520f;
            }
            else if (zona == Zona.Pozo)
            {
                RenderSettings.fogColor = new Color(0.03f, 0.24f, 0.34f);
                RenderSettings.fogStartDistance = 0.4f;
                RenderSettings.fogEndDistance = 9f;
                RenderSettings.ambientLight = new Color(0.04f, 0.14f, 0.2f);
                cam.farClipPlane = 48f;
            }
            else
            {
                RenderSettings.fogStartDistance = zona == Zona.Tierra ? 40f : 18f;
                RenderSettings.fogEndDistance = zona == Zona.Tierra ? 180f : 80f;
                cam.farClipPlane = 420f;
            }

            cam.backgroundColor = RenderSettings.fogColor;
            MarcarCota();

            Vector3 pies = cuerpo.position;
            if (mapa)
            {
                Vector3 focus = pies;
                if (planSuelto && raiz != null)
                    focus = raiz.TransformPoint(new Vector3(planMira.x, Relieve(planMira.x, planMira.y), planMira.y));
                cam.orthographicSize = planSize;
                cam.nearClipPlane = 0.3f;
                cam.farClipPlane = 420f;
                Vector3 arriba = focus + Vector3.up * 220f;
                Quaternion giro = Quaternion.Euler(90f, CamaraTercera.PlanYaw, 0f);
                if (!snapped)
                    cam.transform.SetPositionAndRotation(arriba, giro);
                else
                {
                    float t = 1f - Mathf.Exp(-6f * Time.deltaTime);
                    cam.transform.position = Vector3.Lerp(cam.transform.position, arriba, t);
                    cam.transform.rotation = giro;
                }

                snapped = true;
                cuerpo.rotation = giro;
            }
            else
            {
                camara.Pose(pies, out Vector3 position, out Quaternion rotation);
                if (zona == Zona.Pozo)
                    position.y = Mathf.Max(position.y, pies.y + 0.4f);
                else if (zona == Zona.Galeria)
                {
                    if (position.y < pies.y + 0.35f) position.y = pies.y + 0.35f;
                    if (position.y > pies.y + 3.05f) position.y = pies.y + 3.05f;
                }
                else if (position.y < pies.y + 0.95f)
                    position.y = pies.y + 0.95f;
                if (sacudida > 0f)
                {
                    sacudida -= Time.deltaTime;
                    float k = Mathf.Clamp01(sacudida);
                    position.x += Mathf.Sin(Time.time * 48f) * 0.09f * k;
                    position.y += Mathf.Sin(Time.time * 71f) * 0.05f * k;
                }

                Vector3 pivot = pies + Vector3.up * camara.PivotHeight;
                SolidoDelCamino.Encuadrar(cam, pivot, position, !snapped);
                snapped = true;

                Vector3 flat = cam.transform.forward;
                flat.y = 0f;
                if (flat.sqrMagnitude > 0.001f)
                    cuerpo.rotation = Quaternion.LookRotation(flat, Vector3.up);
            }

            Luces();
            if (toro != null)
                toro.rotation = cuerpo.rotation;
            for (int i = 0; i < obreros.Count; i++)
            {
                if (obreros[i].cuerpo == null) continue;
                obreros[i].cuerpo.rotation = cuerpo.rotation;
                var ink = obreros[i].cuerpo.GetComponent<SpriteRenderer>();
                if (ink != null && zona == Zona.Galeria)
                    ink.color = TinteDeLuz(obreros[i].cuerpo.position, Color.white);
            }
            for (int i = 0; i < sentados.Count; i++)
            {
                if (sentados[i] == null) continue;
                sentados[i].rotation = cuerpo.rotation;
                if (i < sentadoEn.Count)
                    sentados[i].localPosition = sentadoEn[i] + Vector3.up * Mathf.Sin(Time.time * 1.3f + i) * 0.012f;
                if (zona == Zona.Galeria)
                {
                    var ink = sentados[i].GetComponent<SpriteRenderer>();
                    if (ink != null) ink.color = TinteDeLuz(sentados[i].position, Color.white);
                }
            }
            if (muqui != null && muqui.activeSelf)
            {
                muqui.transform.rotation = cuerpo.rotation;
                var tintaMuki = muqui.GetComponent<SpriteRenderer>();
                if (tintaMuki != null)
                    tintaMuki.color = zona == Zona.Galeria
                        ? TinteDeLuz(muqui.transform.position, new Color(0.95f, 0.78f, 0.42f))
                        : new Color(0.95f, 0.78f, 0.42f);
            }

            Velar();
            if (tinta != null)
                tinta.color = zona == Zona.Pozo
                    ? new Color(0.4f, 0.72f, 0.8f, 1f)
                    : zona == Zona.Galeria
                        ? TinteDeLuz(cuerpo.position, Color.white)
                        : Color.white;
            if (fanal != null)
                fanal.SetActive(tiene && encendida);
        }

        Transform toro;

        void Caminar()
        {
            if (Input.GetKeyDown(KeyCode.L)) PulsarLampara();
            if (Input.GetKeyDown(KeyCode.G) && carga) Soltar();
            if ((Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.F)) && juntoToro)
            {
                EmpezarPelea();
                return;
            }

            if (!enSuelo)
            {
                Volar();
                return;
            }

            if (junto && Input.GetKeyDown(KeyCode.S))
            {
                EntrarSuperficie(local);
                return;
            }

            if (allowJump && Input.GetKeyDown(KeyCode.Space))
            {
                Saltar();
                return;
            }

            Vector3 wish = planta && zona == Zona.Tierra
                ? camara.PlanLocal(raiz)
                : camara.Wasd();
            bool anda = wish.sqrMagnitude > 0.04f;
            Vector3 next = local + wish * 7.2f * Time.deltaTime;
            next.y = Relieve(next.x, next.z);
            if (!Dentro(next) || !Cabe(local, next))
                anda = false;
            else
            {
                if (Radio(next) < LagoR)
                {
                    EntrarSuperficie(next);
                    return;
                }

                local = next;
            }

            cuerpo.localPosition = local;
            Mirar(wish, anda, false);
        }

        void Saltar()
        {
            enSuelo = false;
            cayo = false;
            vy = 7.6f;
            Vector3 wish = camara.Wasd();
            if (wish.sqrMagnitude < 0.04f) wish = camara.FlatForward();
            empuje = wish.sqrMagnitude > 0.01f ? wish.normalized * 5.1f : Vector3.forward * 5.1f;
            anim?.PlayJump();
        }

        void Volar()
        {
            vy -= 20f * Time.deltaTime;
            Vector3 next = local + empuje * Time.deltaTime;
            next.y += vy * Time.deltaTime;
            if (!Cabe(local, next))
            {
                next.x = local.x;
                next.z = local.z;
            }

            if (Radio(next) < LagoR && next.y < AguaY + 0.8f)
            {
                EntrarSuperficie(next);
                return;
            }

            float suelo = Relieve(next.x, next.z);
            if (Radio(next) >= LagoR && next.y <= suelo && vy <= 0f)
            {
                local = new Vector3(next.x, suelo, next.z);
                enSuelo = true;
                vy = 0f;
                empuje = Vector3.zero;
                cayo = false;
                anim?.PlayLocomotion(false, false);
            }
            else
            {
                local = next;
                if (vy < -1.4f && !cayo)
                {
                    cayo = true;
                    anim?.PlayFall();
                }
            }

            cuerpo.localPosition = local;
        }

        void Bucear()
        {
            if (Input.GetKeyDown(KeyCode.L)) PulsarLampara();
            if (Input.GetKeyDown(KeyCode.G) && carga) Soltar();

            if (zona == Zona.Superficie) NadarSuperficie();
            else if (zona == Zona.Pozo) NadarPozo();
            else NadarGaleria();

            ProbarOro();
            HundirMuqui();
        }

        void NadarSuperficie()
        {
            Vector3 wish = camara.Wasd();
            float avance = TomarImpulso(ref impulso);
            bool sube = avance > 0.01f || Input.GetKey(KeyCode.W);
            if (sube) haciaBoca = false;
            if (avance > 0.01f)
            {
                SalirOrilla();
                return;
            }

            bool baja = haciaBoca || avance < -0.01f || Input.GetKey(KeyCode.S);
            if (baja && Radio(local) > PozoR)
            {
                Vector3 alCentro = new Vector3(-local.x, 0f, -local.z);
                if (alCentro.sqrMagnitude > 0.01f) wish = alCentro.normalized;
            }

            Vector3 next = local + wish * 4.4f * Time.deltaTime;
            float r = Radio(next);
            float limite = LagoR - 0.3f;
            if (r > limite)
            {
                Vector3 radial = new Vector3(next.x, 0f, next.z);
                if (Vector3.Dot(wish, radial) > 0.05f)
                {
                    SalirOrilla();
                    return;
                }

                if (r > 0.01f)
                {
                    float k = limite / r;
                    next.x *= k;
                    next.z *= k;
                }
            }

            next.y = AguaY - Hundido;
            if (Cabe(local, next)) local = next;
            cuerpo.localPosition = local;

            if (baja && Radio(local) <= PozoR)
            {
                haciaBoca = true;
                EntrarPozo();
                return;
            }

            Mirar(wish, wish.sqrMagnitude > 0.01f, true);
        }

        void NadarPozo()
        {
            Vector3 wish = camara.Wasd();
            float vertical = 0f;
            if (Input.GetKey(KeyCode.S)) vertical -= 1f;
            if (Input.GetKey(KeyCode.W)) vertical += 1f;
            float dy = TomarImpulso(ref impulso);
            float dx = TomarImpulso(ref impulsoX);
            if (vertical > 0f && dy < 0f)
            {
                impulso = 0f;
                dy = 0f;
            }

            if (vertical < 0f || dy < -0.01f) wish = Vector3.zero;

            Vector3 next = local;
            next.x += wish.x * 3.1f * Time.deltaTime + dx;
            next.z += wish.z * 3.1f * Time.deltaTime;
            next.y += vertical * 4.2f * Time.deltaTime + dy;
            next.x = Mathf.Clamp(next.x, -0.9f, 1.05f);
            next.z = Mathf.Clamp(next.z, -0.95f, 0.95f);
            if (!encendida) next.x = Mathf.Max(next.x, -0.85f);

            if (next.y > AguaY - 0.4f && (vertical > 0f || dy > 0.01f))
            {
                EntrarSuperficie(next);
                return;
            }

            next.y = Mathf.Clamp(next.y, Fondo, AguaY - 0.35f);
            Vector3 paso = new Vector3(next.x, local.y, next.z);
            if (!Cabe(local, paso)) paso = local;
            Vector3 bajo = new Vector3(paso.x, next.y, paso.z);
            if (!Cabe(paso, bajo)) bajo = paso;

            if (bajo.y > Fondo + 2.2f) recienSalio = false;
            if ((vertical < 0f || dy < -0.01f) && bajo.y <= Fondo + 1.2f) recienSalio = false;
            if (bajo.y <= Fondo + 0.35f && encendida && !recienSalio)
            {
                EntrarGaleria();
                return;
            }

            if (bajo.y <= Fondo + 0.35f && !encendida && !dijoSinLuz)
            {
                dijoSinLuz = true;
                Decir("EL SOCAVÓN", LeyDelMonte.MinaSinLuz, "El cuy");
            }

            local = bajo;
            cuerpo.localPosition = local;
            Mirar(wish, wish.sqrMagnitude > 0.01f || Mathf.Abs(vertical) > 0.01f, true);
        }

        void NadarGaleria()
        {
            Vector3 wish = camara.Wasd();
            float emp = TomarImpulso(ref impulsoX);
            if (emp > 0.001f)
                wish += MinaDeSubniveles.HaciaSalida(local) * Mathf.Clamp(emp * 4f, 0f, 1f);
            if (wish.sqrMagnitude > 1f) wish.Normalize();

            float paso = 6.2f * Time.deltaTime;
            Vector3 plano = new Vector3(wish.x, 0f, wish.z);
            if (!MinaDeSubniveles.Pisar(local, local + plano * paso, encendida, out var puesto))
            {
                Vector3 soloX = local + new Vector3(plano.x, 0f, 0f) * paso;
                Vector3 soloZ = local + new Vector3(0f, 0f, plano.z) * paso;
                if (!MinaDeSubniveles.Pisar(local, soloX, encendida, out puesto)
                    && !MinaDeSubniveles.Pisar(local, soloZ, encendida, out puesto))
                    puesto = local;
            }

            if (!Cabe(local, puesto)) puesto = local;
            if (aire == AireMina.Gases && Hondo(puesto) > Hondo(local) + 0.03f)
            {
                puesto = local;
                if (!dijoTope)
                {
                    dijoTope = true;
                    Decir("EL AIRE", LeyDelMonte.MinaGases, "El reloj");
                }
            }
            if (puesto.y > Fondo - 1.2f && puesto.x > -2.4f && Mathf.Abs(puesto.z) < 1.25f)
            {
                local = new Vector3(-0.55f, Fondo, Mathf.Clamp(puesto.z, -0.7f, 0.7f));
                recienSalio = true;
                EntrarPozo();
                cuerpo.localPosition = local;
                return;
            }

            bool anda = (puesto - local).sqrMagnitude > 0.000001f;
            local = puesto;
            cuerpo.localPosition = local;
            Mirar(wish, anda, false);
            if (!dijoAire)
            {
                dijoAire = true;
                Decir("LA MINA", LeyDelMonte.MinaAire, "La mina");
            }
        }

        void EntrarSuperficie(Vector3 desde)
        {
            float r = Radio(desde);
            if (r > LagoR - 0.4f && r > 0.01f)
            {
                float k = (LagoR - 0.8f) / r;
                desde.x *= k;
                desde.z *= k;
            }

            desde.y = AguaY - Hundido;
            local = desde;
            zona = Zona.Superficie;
            enSuelo = true;
            vy = 0f;
            empuje = Vector3.zero;
            impulso = 0f;
            camara.Distance = 6.2f;
            camara.MinDistance = 3.4f;
            camara.MaxDistance = 12f;
            camara.Pitch = 28f;
            camara.PivotHeight = 0.85f;
            cuerpo.localPosition = local;
            if (!dijoNada)
            {
                dijoNada = true;
                Decir("EL AGUA", LeyDelMonte.MinaNada, "El cuy");
            }

            anim?.PlaySwim();
        }

        void EntrarPozo()
        {
            bool hundir = haciaBoca;
            haciaBoca = false;
            zona = Zona.Pozo;
            local.x = Mathf.Clamp(local.x, -1f, 1f);
            local.z = Mathf.Clamp(local.z, -1f, 1f);
            if (local.y > AguaY - 0.2f) local.y = AguaY - 0.6f;
            if (hundir) impulso = Fondo - local.y - 0.05f;
            camara.Distance = 1.7f;
            camara.MinDistance = 1.2f;
            camara.MaxDistance = 2.2f;
            camara.Pitch = 24f;
            camara.PivotHeight = 1.05f;
            snapped = false;
            cuerpo.localPosition = local;
        }

        void EntrarGaleria()
        {
            zona = Zona.Galeria;
            impulso = 0f;
            impulsoX = 0f;
            haciaBoca = false;
            local = new Vector3(-2.7f, Fondo, 0f);
            camara.Distance = 2.45f;
            camara.MinDistance = 1.55f;
            camara.MaxDistance = 3.15f;
            camara.Pitch = 14f;
            camara.PivotHeight = 1.15f;
            camara.Yaw = -90f;
            snapped = false;
            cuerpo.localPosition = local;
        }

        void SalirOrilla()
        {
            zona = Zona.Tierra;
            local = orillaSalida;
            enSuelo = true;
            vy = 0f;
            empuje = Vector3.zero;
            impulso = 0f;
            impulsoX = 0f;
            haciaBoca = false;
            recienSalio = false;
            camara.Distance = 8f;
            camara.MinDistance = 3.2f;
            camara.MaxDistance = 22f;
            camara.Pitch = 18f;
            camara.PivotHeight = 1.05f;
            snapped = false;
            if (cuerpo != null) cuerpo.localPosition = local;
            anim?.PlayLocomotion(false, false);
        }

        void Medir()
        {
            float r = Radio(local);
            junto = zona == Zona.Tierra && r > LagoR - 0.4f && r < LagoR + 3.6f;
            juntoToro = zona == Zona.Tierra && Plano(local, toroEn) < 6.5f * 6.5f;
            if (juntoToro && !dijoToro)
            {
                dijoToro = true;
                Decir("LA MONTAÑA", LeyDelMonte.MontanaEntra, "La vizcacha");
            }
        }

        bool CercaDeLampara()
        {
            return zona == Zona.Tierra && Plano(local, lamparaEn) < 2.1f * 2.1f;
        }

        void Tomar()
        {
            tiene = true;
            encendida = true;
            if (lamparaOrilla != null) lamparaOrilla.SetActive(false);
            Decir("LA LÁMPARA", LeyDelMonte.MinaToma, "El cuy");
        }

        void Encender()
        {
            encendida = !encendida;
            if (encendida) dijoSinLuz = false;
            Decir("LA LÁMPARA", encendida ? LeyDelMonte.MinaLuz : LeyDelMonte.MinaApaga, "El cuy");
        }

        void Lejos()
        {
            if (dijoLejos) return;
            dijoLejos = true;
            Decir("LA LÁMPARA", LeyDelMonte.MinaEnOrilla, "El cuy");
        }

        void ProbarOro()
        {
            if (!muquiVivo || carga || zona != Zona.Galeria) return;
            Vector3 pies = cuerpo != null ? cuerpo.position : raiz.TransformPoint(local);
            for (int i = 0; i < oros.Count; i++)
            {
                if (oros[i] == null) continue;
                if (Vector3.Distance(pies, oros[i].position) > 1.35f) continue;
                if (devuelto)
                {
                    if (!dijoQueda)
                    {
                        dijoQueda = true;
                        Decir("EL ORO", LeyDelMonte.MinaQueda, "El cuy");
                    }

                    return;
                }

                carga = true;
                PartidaDelCamino.GuardarOro();
                if (!cobro)
                {
                    cobro = true;
                    battle?.NoteCodicia();
                }

                for (int k = 0; k < oros.Count; k++)
                {
                    if (oros[k] == null) continue;
                    oros[k].SetParent(cuerpo, true);
                    oros[k].localPosition = new Vector3(-0.35f + k * 0.28f, 1.15f, 0f);
                    oros[k].localScale = new Vector3(0.28f, 0.22f, 0.28f);
                }

                if (!dijoLibre)
                {
                    dijoLibre = true;
                    Decir("EL ORO", LeyDelMonte.MinaLibre, "La vizcacha");
                }

                return;
            }
        }

        void Soltar()
        {
            carga = false;
            for (int i = 0; i < oros.Count; i++)
            {
                if (oros[i] == null) continue;
                oros[i].SetParent(raiz, false);
                oros[i].localPosition = casas[i];
                oros[i].localScale = new Vector3(0.42f, 0.32f, 0.42f);
            }

            if (cobro && !devuelto)
            {
                devuelto = true;
                battle?.DevolverOro();
            }

            PartidaDelCamino.SoltarOro();
            muquiBaja = 1.6f;
            muquiVivo = false;
            Decir("EL MUQUI", LeyDelMonte.MinaSuelta, "El cuy");
        }

        void HundirMuqui()
        {
            if (muquiBaja <= 0f || muqui == null) return;
            muquiBaja -= Time.deltaTime;
            muqui.transform.position += Vector3.down * Time.deltaTime * 0.8f;
            if (muquiBaja <= 0f) muqui.SetActive(false);
        }

        void Decir(string titulo, string cuerpoLinea, string voz)
        {
            battle?.ShowRoadLine(titulo, cuerpoLinea, voz);
            if (linea == null || linea.text == cuerpoLinea) return;
            linea.text = cuerpoLinea;
            lineaFade?.ShowAgain();
        }

        void Mirar(Vector3 worldMove, bool moving, bool nadar)
        {
            if (anim == null) return;
            if (nadar && zona != Zona.Galeria)
            {
                anim.PlaySwim();
                return;
            }

            if (enSuelo == false && zona == Zona.Tierra)
            {
                anim.PlayLocomotion(moving, true);
                return;
            }

            anim.PlayStep(worldMove, moving);
        }

        bool Cabe(Vector3 desde, Vector3 hasta)
        {
            if (raiz == null) return true;
            return SolidoDelCamino.Puede(raiz.TransformPoint(desde), raiz.TransformPoint(hasta), 0.72f, 2.1f);
        }

        bool Dentro(Vector3 p)
        {
            return Radio(p) < 63f;
        }

        static float Radio(Vector3 p)
        {
            return Mathf.Sqrt(p.x * p.x + p.z * p.z);
        }

        static float Plano(Vector3 a, Vector3 b)
        {
            float x = a.x - b.x;
            float z = a.z - b.z;
            return x * x + z * z;
        }

        static float TomarImpulso(ref float queda)
        {
            if (Mathf.Abs(queda) < 0.02f)
            {
                queda = 0f;
                return 0f;
            }

            float paso = Mathf.Sign(queda) * Mathf.Min(Mathf.Abs(queda), 9f * Time.deltaTime);
            queda -= paso;
            return paso;
        }

        static float Relieve(float x, float z)
        {
            float r = Mathf.Sqrt(x * x + z * z);
            float onda = Mathf.Sin(x * 0.11f + 1.2f) * Mathf.Cos(z * 0.08f) * 0.7f;
            if (r < LagoR - 0.4f) return -1.5f;
            if (r < 23f)
            {
                float u = (r - (LagoR - 0.4f)) / (23f - (LagoR - 0.4f));
                return Mathf.Lerp(-0.15f, 0.55f, u) + onda * 0.12f;
            }

            float t = Mathf.Clamp01((r - 23f) / 44f);
            float cerro = Mathf.Lerp(0.55f, 18f, t * t);
            float px = x * x;
            float pz = z - 48f;
            float pico = Mathf.Exp(-(px + pz * pz) / 320f) * 14f;
            return cerro + onda * t + pico;
        }

        void Terreno()
        {
            const int n = 52;
            const float half = 68f;
            int lado = n + 1;
            var verts = new Vector3[lado * lado];
            var cols = new Color[verts.Length];
            var uvs = new Vector2[verts.Length];
            for (int iz = 0; iz <= n; iz++)
            {
                for (int ix = 0; ix <= n; ix++)
                {
                    float x = Mathf.Lerp(-half, half, ix / (float)n);
                    float z = Mathf.Lerp(-half, half, iz / (float)n);
                    float y = Relieve(x, z);
                    int i = iz * lado + ix;
                    verts[i] = new Vector3(x, y, z);
                    uvs[i] = new Vector2(x / 7f, z / 7f);
                    cols[i] = y < 2.2f
                        ? Color.white
                        : Color.Lerp(Color.white, RocaTinta, Mathf.Clamp01((y - 2.2f) / 10f));
                }
            }

            var tris = new int[n * n * 6];
            int t = 0;
            for (int iz = 0; iz < n; iz++)
            {
                for (int ix = 0; ix < n; ix++)
                {
                    int i = iz * lado + ix;
                    tris[t++] = i;
                    tris[t++] = i + lado;
                    tris[t++] = i + 1;
                    tris[t++] = i + 1;
                    tris[t++] = i + lado;
                    tris[t++] = i + lado + 1;
                }
            }

            var mesh = new Mesh { name = "Terreno" };
            mesh.vertices = verts;
            mesh.colors = cols;
            mesh.uv = uvs;
            mesh.triangles = tris;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            var go = new GameObject("Terreno");
            go.transform.SetParent(raiz, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var rend = go.AddComponent<MeshRenderer>();
            rend.sharedMaterial = Malla("Bosque/piso_cesped", new Vector2(8f, 8f), Pasto, false);
            rend.shadowCastingMode = ShadowCastingMode.Off;
            rend.receiveShadows = false;
        }

        void Laguna()
        {
            Anillo("AguaLaguna", PozoR * 0.92f, LagoR + 0.4f, AguaY, new Color(0.82f, 0.93f, 0.9f, 0.64f), 48);
            Disco("AguaBoca", PozoR, AguaY + 0.04f, new Color(0.02f, 0.05f, 0.08f), 24);
        }

        void Anillo(string nombre, float radioIn, float radioOut, float y, Color tinte, int seg)
        {
            var verts = new Vector3[seg * 2];
            var uvs = new Vector2[seg * 2];
            var tris = new int[seg * 6];
            for (int i = 0; i < seg; i++)
            {
                float a = i / (float)seg * Mathf.PI * 2f;
                float c = Mathf.Cos(a);
                float s = Mathf.Sin(a);
                verts[i] = new Vector3(c * radioIn, y, s * radioIn);
                verts[i + seg] = new Vector3(c * radioOut, y, s * radioOut);
                uvs[i] = new Vector2(verts[i].x / 14f, verts[i].z / 14f);
                uvs[i + seg] = new Vector2(verts[i + seg].x / 14f, verts[i + seg].z / 14f);
                int n = (i + 1) % seg;
                int t = i * 6;
                tris[t] = i;
                tris[t + 1] = i + seg;
                tris[t + 2] = n + seg;
                tris[t + 3] = i;
                tris[t + 4] = n + seg;
                tris[t + 5] = n;
            }

            var mesh = new Mesh { name = nombre };
            mesh.vertices = verts;
            mesh.uv = uvs;
            mesh.triangles = tris;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            var go = new GameObject(nombre);
            go.transform.SetParent(raiz, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var rend = go.AddComponent<MeshRenderer>();
            rend.sharedMaterial = MallaLaguna(AguaTex(), tinte);
            rend.shadowCastingMode = ShadowCastingMode.Off;
            rend.receiveShadows = false;
        }

        void Disco(string nombre, float radio, float y, Color tinte, int seg)
        {
            var verts = new Vector3[seg + 1];
            var uvs = new Vector2[seg + 1];
            var tris = new int[seg * 3];
            verts[0] = new Vector3(0f, y, 0f);
            uvs[0] = new Vector2(0.5f, 0.5f);
            for (int i = 0; i < seg; i++)
            {
                float a = i / (float)seg * Mathf.PI * 2f;
                verts[i + 1] = new Vector3(Mathf.Cos(a) * radio, y, Mathf.Sin(a) * radio);
                uvs[i + 1] = new Vector2(0.5f + Mathf.Cos(a) * 0.5f, 0.5f + Mathf.Sin(a) * 0.5f);
                tris[i * 3] = 0;
                tris[i * 3 + 1] = i + 1;
                tris[i * 3 + 2] = i == seg - 1 ? 1 : i + 2;
            }

            var mesh = new Mesh { name = nombre };
            mesh.vertices = verts;
            mesh.uv = uvs;
            mesh.triangles = tris;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            var go = new GameObject(nombre);
            go.transform.SetParent(raiz, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var rend = go.AddComponent<MeshRenderer>();
            rend.sharedMaterial = Malla("Bosque/rio", new Vector2(3f, 3f), tinte, true);
            rend.shadowCastingMode = ShadowCastingMode.Off;
            rend.receiveShadows = false;
        }

        void Socavon()
        {
            const float medio = (Fondo - 0.6f + -0.15f) * 0.5f;
            const float alto = (-0.15f) - (Fondo - 0.6f);
            Caja("MuroEste", new Vector3(2.15f, medio, 0f), new Vector3(0.6f, alto, 4.6f), Piedra);
            Caja("MuroNorte", new Vector3(0f, medio, 2.15f), new Vector3(4.6f, alto, 0.6f), Piedra);
            Caja("MuroSur", new Vector3(0f, medio, -2.15f), new Vector3(4.6f, alto, 0.6f), Piedra);
            float oesteAlto = -0.15f - (-12.15f);
            float oesteMedio = (-12.15f + -0.15f) * 0.5f;
            Caja("MuroOeste", new Vector3(-2.15f, oesteMedio, 0f), new Vector3(0.6f, oesteAlto, 4.6f), Piedra);

            float torreY = Relieve(MinaDeSubniveles.TorreX, MinaDeSubniveles.TorreZ);
            var hall = MinaDeSubniveles.Construir(raiz, Fondo, torreY);
            Vector3[] pepas = { hall.OroA, hall.OroB, hall.OroC };
            for (int i = 0; i < pepas.Length; i++)
            {
                var pepa = Caja("Oro" + i, pepas[i], new Vector3(0.42f, 0.32f, 0.42f), Oro);
                oros.Add(pepa);
                casas.Add(pepa.localPosition);
            }

            Gente(hall.Muqui);
        }

        void Gente(Vector3 mukiEn)
        {
            float y0 = Fondo;
            ObreroDe(BattleCast.Campesina, -14f, -8f, y0, -1.32f);
            ObreroDe(BattleCast.JuanDeOso, -62f, -50f, y0 - 50f, -1.32f);
            ObreroDe(BattleCast.Brayan, -70f, -58f, y0 - 100f, -1.32f);
            ObreroDe(BattleCast.Campesina, -78f, -66f, y0 - 150f, -1.32f);
            ObreroDe(BattleCast.JuanDeOso, -70f, -58f, y0 - 200f, -1.32f);
            var mukiGo = Persona("Muki", BattleCast.JuanDeOso, mukiEn, 1.12f, true);
            muqui = mukiGo != null ? mukiGo.gameObject : null;
            Descanso();
        }

        void Descanso()
        {
            float[] xs = { -17.6f, -20.3f, -23f };
            for (int i = 0; i < xs.Length; i++)
            {
                var pos = new Vector3(xs[i], Fondo, -1.18f);
                var cuerpoMina = Sentado("MineroSentado", i, pos, 1.28f);
                if (cuerpoMina == null) continue;
                sentados.Add(cuerpoMina);
                sentadoEn.Add(pos);
                Caja("AdemeBanco", pos + new Vector3(0f, 0.12f, -0.28f), new Vector3(0.7f, 0.16f, 0.36f), Madera);
                var farol = Caja("AdemeLuz", pos + new Vector3(0.16f, 0.78f, 0.08f), new Vector3(0.1f, 0.1f, 0.1f), new Color(1f, 0.9f, 0.45f));
                farol.SetParent(cuerpoMina, true);
            }
        }

        Transform Sentado(string nombre, int hoja, Vector3 pos, float alto)
        {
            var tex = Resources.Load<Texture2D>("Battle/Personajes/Mineros/Sentado/" + hoja.ToString("00"));
            if (tex == null || !tex.isReadable) return null;
            var sprite = Sprite.Create(tex, new Rect(0f, 0f, tex.width, tex.height), new Vector2(0.5f, 0f), 100f);
            var go = new GameObject(nombre);
            go.transform.SetParent(raiz, false);
            go.transform.localPosition = pos;
            var ink = go.AddComponent<SpriteRenderer>();
            ink.sprite = sprite;
            ink.sortingOrder = 22;
            float nativo = sprite.rect.height / sprite.pixelsPerUnit;
            if (nativo < 0.2f) nativo = 1f;
            float escala = alto / nativo;
            go.transform.localScale = new Vector3(escala, escala, 1f);
            return go.transform;
        }

        void PasoBolo()
        {
            if (zona != Zona.Galeria || boloPaso >= 3) return;
            if (boloPaso < 0)
            {
                if (Plano(local, new Vector3(-20.3f, Fondo, -1.18f)) > 4.6f * 4.6f) return;
                if (Mathf.Abs(local.y - Fondo) > 3f) return;
                boloPaso = 0;
                aire = AireMina.Cuenta;
                reloj = 600f;
                DecirBolo();
                return;
            }

            boloEspera -= Time.deltaTime;
            if (boloEspera > 0f) return;
            boloPaso++;
            if (boloPaso == 2) tieneReloj = true;
            if (boloPaso < 3) DecirBolo();
        }

        void DecirBolo()
        {
            string lineaBolo = boloPaso == 0
                ? LeyDelMonte.MinaBolo1
                : boloPaso == 1 ? LeyDelMonte.MinaBolo2 : LeyDelMonte.MinaBolo3;
            Decir("LA CUADRILLA", lineaBolo, "Los mineros");
            boloEspera = 7.4f;
        }

        void TickReloj()
        {
            if (aire != AireMina.Cuenta && aire != AireMina.Gases) return;
            reloj -= Time.deltaTime;
            if (reloj > 0f) return;
            if (aire == AireMina.Cuenta)
            {
                aire = AireMina.Gases;
                reloj = 120f;
                sacudida = 1.5f;
                dijoTope = false;
                Decir("LA VOLADURA", LeyDelMonte.MinaVoladura, "La mina");
                return;
            }

            aire = AireMina.Limpio;
            reloj = 0f;
            Decir("EL AIRE", LeyDelMonte.MinaAireLimpio, "El reloj");
        }

        static float Hondo(Vector3 p)
        {
            return (Fondo - p.y) * 20f - p.x;
        }

        void PintarReloj()
        {
            if (relojTexto == null) return;
            bool ver = tieneReloj && aire != AireMina.Quieto;
            if (relojTexto.gameObject.activeSelf != ver) relojTexto.gameObject.SetActive(ver);
            if (!ver) return;
            int s = Mathf.CeilToInt(Mathf.Max(0f, reloj));
            string marca = (s / 60).ToString("00") + ":" + (s % 60).ToString("00");
            string texto = aire == AireMina.Gases
                ? "Reloj  " + marca + "   ·   gases sobre el límite"
                : aire == AireMina.Limpio
                    ? "Reloj   ·   aire dentro del límite"
                    : "Reloj  " + marca + "   ·   voladura";
            if (relojTexto.text != texto) relojTexto.text = texto;
        }

        void ObreroDe(CharacterSheet hoja, float a, float b, float y, float z)
        {
            float x = (a + b) * 0.5f;
            var cuerpoMina = Persona("Minero", hoja, new Vector3(x, y, z), CuerpoAlto, false);
            if (cuerpoMina == null) return;
            obreros.Add(new Obrero
            {
                cuerpo = cuerpoMina,
                anim = cuerpoMina.GetComponent<SpriteFrameAnimator>(),
                a = a,
                b = b,
                y = y,
                z = z,
                x = x,
            });
        }

        Transform Persona(string nombre, CharacterSheet hoja, Vector3 pos, float alto, bool muki)
        {
            var go = new GameObject(nombre);
            go.transform.SetParent(raiz, false);
            go.transform.localPosition = pos;
            var ink = go.AddComponent<SpriteRenderer>();
            ink.sortingOrder = 22;
            var clip = go.AddComponent<SpriteFrameAnimator>();
            if (hoja != null && hoja.HasSprites)
                BattleSprites.Apply(clip, hoja);
            float nativo = hoja != null ? CastScale.Native(hoja) : 1f;
            if (nativo < 0.2f) nativo = 1f;
            float escala = alto / nativo;
            go.transform.localScale = new Vector3(escala, escala, 1f);
            clip.PlayLocomotion(false, false);
            if (muki) ink.color = new Color(0.95f, 0.78f, 0.42f);
            float pecho = nativo * 0.62f;
            CajaEn(go.transform, "AdemeCasco", new Vector3(0f, nativo * 0.9f, 0f), new Vector3(nativo * 0.34f, nativo * 0.12f, nativo * 0.28f), Madera);
            CajaEn(go.transform, "AdemeLuz", new Vector3(nativo * 0.22f, pecho, 0f), new Vector3(nativo * 0.1f, nativo * 0.1f, nativo * 0.1f), new Color(1f, 0.9f, 0.45f));
            return go.transform;
        }

        void MoverObreros()
        {
            for (int i = 0; i < obreros.Count; i++)
            {
                Obrero o = obreros[i];
                if (o.cuerpo == null) continue;
                o.x += o.dir * 0.85f * Time.deltaTime;
                if (o.x > o.b) { o.x = o.b; o.dir = -1; }
                if (o.x < o.a) { o.x = o.a; o.dir = 1; }
                o.cuerpo.localPosition = new Vector3(o.x, o.y, o.z);
                o.anim?.PlayStep(new Vector3(o.dir, 0f, 0f), true);
            }

            if (muqui != null && muqui.activeSelf)
                muqui.GetComponent<SpriteFrameAnimator>()?.PlayStep(Vector3.forward, false);
        }

        void Lampara()
        {
            lamparaEn = new Vector3(0f, 0f, -19.4f);
            lamparaEn.y = Relieve(lamparaEn.x, lamparaEn.z);
            lamparaOrilla = new GameObject("LamparaOrilla");
            lamparaOrilla.transform.SetParent(raiz, false);
            lamparaOrilla.transform.localPosition = lamparaEn;
            CajaEn(lamparaOrilla.transform, "Pie", new Vector3(0f, 0.4f, 0f), new Vector3(0.16f, 0.8f, 0.16f), Madera);
            CajaEn(lamparaOrilla.transform, "FanalPie", new Vector3(0f, 0.95f, 0f), new Vector3(0.32f, 0.32f, 0.32f), Oro);
        }

        void Fanal()
        {
            fanal = new GameObject("Fanal");
            fanal.transform.SetParent(cuerpo, false);
            fanal.transform.localPosition = new Vector3(0.35f, 1.15f, 0f);
            CajaEn(fanal.transform, "Llama", Vector3.zero, new Vector3(0.22f, 0.22f, 0.22f), new Color(1f, 0.9f, 0.45f));
            fanal.SetActive(false);
        }

        void Heroe()
        {
            var ficha = FighterRoster.Current;
            var go = new GameObject("MontanaHero");
            go.transform.SetParent(raiz, false);
            var ink = go.AddComponent<SpriteRenderer>();
            ink.sortingOrder = 24;
            anim = go.AddComponent<SpriteFrameAnimator>();
            BattleSprites.Apply(anim, ficha.Sheet);
            float escala = ficha.Scale * (CuerpoAlto / CastScale.Stand);
            go.transform.localScale = new Vector3(Mathf.Abs(escala), Mathf.Abs(escala), 1f);
            tinta = ink;
            local.y = Relieve(local.x, local.z);
            go.transform.localPosition = local;
            orillaSalida = new Vector3(0f, Relieve(0f, -20f), -20f);
            anim.SetFacing(ficha.FacesRight);
            anim.PlayLocomotion(false, false);
            cuerpo = go.transform;
        }

        void RecordarOro()
        {
            if (PartidaDelCamino.EleccionOro == PartidaDelCamino.Oro.Soltado)
            {
                devuelto = true;
                cobro = true;
                carga = false;
                muquiVivo = false;
                if (muqui != null) muqui.SetActive(false);
                return;
            }

            if (PartidaDelCamino.EleccionOro != PartidaDelCamino.Oro.Guardado || cuerpo == null) return;
            carga = true;
            cobro = true;
            for (int k = 0; k < oros.Count; k++)
            {
                if (oros[k] == null) continue;
                oros[k].SetParent(cuerpo, true);
                oros[k].localPosition = new Vector3(-0.35f + k * 0.28f, 1.15f, 0f);
                oros[k].localScale = new Vector3(0.28f, 0.22f, 0.28f);
            }
        }

        void Toro()
        {
            toroEn = new Vector3(18f, 0f, -12f);
            toroEn.y = Relieve(toroEn.x, toroEn.z);
            var go = new GameObject("ToroDeLaOrilla");
            go.transform.SetParent(raiz, false);
            var ink = go.AddComponent<SpriteRenderer>();
            ink.sortingOrder = 22;
            var clip = go.AddComponent<SpriteFrameAnimator>();
            BattleSprites.Apply(clip, BattleCast.Toro);
            float escala = CastScale.Of(BattleCast.Toro) * (CuerpoAlto / CastScale.Stand);
            go.transform.localScale = new Vector3(Mathf.Abs(escala), Mathf.Abs(escala), 1f);
            go.transform.localPosition = toroEn;
            clip.SetFacing(false);
            clip.PlayLocomotion(false, false);
            toro = go.transform;
        }

        void Sembrar()
        {
            int puestos = 0;
            for (int salt = 0; salt < 96 && puestos < 48; salt++)
            {
                float ang = salt * 2.399963f;
                float rad = 26f + (salt % 18) * 1.9f;
                if (rad > 60f) continue;
                float x = Mathf.Cos(ang) * rad;
                float z = Mathf.Sin(ang) * rad;
                var p = new Vector3(x, 0f, z);
                if (Plano(p, local) < 8f * 8f) continue;
                if (Plano(p, lamparaEn) < 5f * 5f) continue;
                if (Plano(p, toroEn) < 8f * 8f) continue;
                if (Plano(p, new Vector3(MinaDeSubniveles.TorreX, 0f, MinaDeSubniveles.TorreZ)) < 9f * 9f) continue;
                float alto = 8.2f + (salt % 5) * 1.45f;
                if (salt % 8 == 0) alto = 5.4f;
                Plantar(Arboles[salt % Arboles.Length], x, z, alto, salt * 37f, true);
                puestos++;
            }

            for (int salt = 0; salt < 16; salt++)
            {
                float ang = salt * 2.1f + 0.4f;
                float rad = 28f + (salt % 7) * 4.2f;
                float x = Mathf.Cos(ang) * rad;
                float z = Mathf.Sin(ang) * rad;
                if (Plano(new Vector3(x, 0f, z), new Vector3(MinaDeSubniveles.TorreX, 0f, MinaDeSubniveles.TorreZ)) < 9f * 9f)
                    continue;
                Plantar(Rocas[salt % Rocas.Length], x, z, 1.6f + (salt % 3) * 0.7f, salt * 20f, false);
            }
        }

        void Plantar(string recurso, float x, float z, float alto, float yaw, bool arbol)
        {
            var prefab = Resources.Load<GameObject>(recurso);
            if (prefab == null) return;
            var pieza = Instantiate(prefab, raiz);
            pieza.name = arbol ? "Arbol" : "Roca";
            foreach (var col in pieza.GetComponentsInChildren<Collider>(true))
                DestroyImmediate(col);
            pieza.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
            pieza.transform.localScale = Vector3.one;
            float nativo = AltoDe(pieza);
            float escala = alto / Mathf.Max(0.2f, nativo);
            pieza.transform.localScale = Vector3.one * escala;
            float suelo = Relieve(x, z);
            pieza.transform.localPosition = new Vector3(x, suelo, z);
            float minY = MinY(pieza);
            float pie = raiz.TransformPoint(new Vector3(x, suelo, z)).y;
            pieza.transform.position += new Vector3(0f, pie + 0.08f - minY, 0f);
            if (!arbol) return;

            foreach (var renderer in pieza.GetComponentsInChildren<Renderer>(true))
            {
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = false;
                var mats = renderer.sharedMaterials;
                bool changed = false;
                for (int m = 0; m < mats.Length; m++)
                {
                    var mat = mats[m];
                    if (mat == null || !Hojas.TryGetValue(mat.name, out var tinta)) continue;
                    mats[m] = Repintar(mat, tinta);
                    changed = true;
                }

                if (changed) renderer.sharedMaterials = mats;
            }
        }

        static Material Repintar(Material source, Color color)
        {
            if (pintadas.TryGetValue(source, out var lista)) return lista;
            var painted = new Material(source)
            {
                name = source.name,
                color = color,
                mainTexture = null,
            };
            pintadas[source] = painted;
            return painted;
        }

        static float AltoDe(GameObject go)
        {
            float min = float.MaxValue;
            float max = float.MinValue;
            var all = go.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < all.Length; i++)
            {
                var b = all[i].bounds;
                if (b.min.y < min) min = b.min.y;
                if (b.max.y > max) max = b.max.y;
            }

            return max > min ? max - min : 1f;
        }

        static float MinY(GameObject go)
        {
            float min = float.MaxValue;
            var all = go.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < all.Length; i++)
                if (all[i].bounds.min.y < min) min = all[i].bounds.min.y;
            return min == float.MaxValue ? 0f : min;
        }

        Transform Caja(string nombre, Vector3 pos, Vector3 tam, Color tinta)
        {
            return CajaEn(raiz, nombre, pos, tam, tinta);
        }

        Transform CajaEn(Transform padre, string nombre, Vector3 pos, Vector3 tam, Color tinta)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = nombre;
            var col = go.GetComponent<Collider>();
            if (col != null) DestroyImmediate(col);
            go.transform.SetParent(padre, false);
            go.transform.localPosition = pos;
            go.transform.localScale = tam;
            var rend = go.GetComponent<Renderer>();
            string tex = nombre.StartsWith("Muro") ? "Mina/roca" : "Calle/piedra";
            Color tintaMalla = nombre.StartsWith("Muro") ? Color.Lerp(tinta, Color.white, 0.72f) : tinta;
            rend.sharedMaterial = Malla(tex, new Vector2(Mathf.Max(1f, tam.x), Mathf.Max(1f, tam.y)), Color.white, false);
            rend.shadowCastingMode = ShadowCastingMode.Off;
            rend.receiveShadows = false;
            var mesh = go.GetComponent<MeshFilter>().mesh;
            var cols = new Color[mesh.vertexCount];
            for (int i = 0; i < cols.Length; i++) cols[i] = tintaMalla;
            mesh.colors = cols;
            return go.transform;
        }

        readonly Dictionary<string, Material> materiales = new Dictionary<string, Material>();

        Material Malla(string tex, Vector2 escala, Color tinte, bool rio)
        {
            string clave = tex + (rio ? ":rio:" : ":calle:") + tinte;
            if (materiales.TryGetValue(clave, out var lista)) return lista;
            var shader = rio
                ? Shader.Find("AYNI/Rio") ?? Shader.Find("AYNI/Calle")
                : Shader.Find("AYNI/Mina") ?? Shader.Find("AYNI/Calle");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            var mat = new Material(shader);
            var texture = Resources.Load<Texture2D>(tex);
            if (texture != null)
            {
                texture.wrapMode = TextureWrapMode.Repeat;
                mat.mainTexture = texture;
                mat.mainTextureScale = escala;
            }

            if (mat.HasProperty("_Color")) mat.SetColor("_Color", tinte);
            else mat.color = tinte;
            materiales[clave] = mat;
            return mat;
        }

        static string AguaTex()
        {
            return Resources.Load<Texture2D>("Agua/laguna") != null ? "Agua/laguna" : "Mina/laguna";
        }

        Material MallaLaguna(string tex, Color tinte)
        {
            var shader = Shader.Find("AYNI/Laguna") ?? Shader.Find("AYNI/Rio") ?? Shader.Find("AYNI/Calle");
            var mat = new Material(shader);
            var texture = Resources.Load<Texture2D>(tex);
            if (texture != null)
            {
                texture.wrapMode = TextureWrapMode.Repeat;
                mat.mainTexture = texture;
            }

            if (mat.HasProperty("_Color")) mat.SetColor("_Color", tinte);
            return mat;
        }

        void Luces()
        {
            if (zona != Zona.Galeria)
            {
                Shader.SetGlobalFloat("_MinaAmb", 1f);
                Shader.SetGlobalInt("_MinaLuzN", 0);
                luzN = 0;
                return;
            }

            Shader.SetGlobalFloat("_MinaAmb", encendida ? 0.04f : 0.012f);
            luzN = 0;
            if (encendida && cuerpo != null)
            {
                Vector3 origen = fanal != null && fanal.activeSelf
                    ? fanal.transform.position
                    : cuerpo.position + Vector3.up * 1.25f;
                Vector3 haz = cuerpo.forward;
                haz.y = -0.28f;
                if (haz.sqrMagnitude < 0.01f) haz = Vector3.forward;
                haz.Normalize();
                PonerLuz(origen, 6.8f, haz, 0.55f);
                PonerLuz(origen, 2.1f, Vector3.zero, -1f);
            }

            for (int i = 0; i < obreros.Count; i++)
            {
                if (obreros[i].cuerpo == null) continue;
                PonerLuz(obreros[i].cuerpo.position + Vector3.up * 1.15f, 4.6f, Vector3.zero, -1f);
            }

            for (int i = 0; i < sentados.Count; i++)
            {
                if (sentados[i] == null) continue;
                PonerLuz(sentados[i].position + Vector3.up * 0.7f, 3.4f, Vector3.zero, -1f);
            }

            Shader.SetGlobalVectorArray("_MinaLuzPos", luzPos);
            Shader.SetGlobalVectorArray("_MinaLuzDir", luzDir);
            Shader.SetGlobalInt("_MinaLuzN", luzN);
        }

        void PonerLuz(Vector3 p, float alcance, Vector3 dir, float cono)
        {
            if (luzN >= luzPos.Length) return;
            luzPos[luzN] = new Vector4(p.x, p.y, p.z, alcance);
            luzDir[luzN] = cono < 0f
                ? new Vector4(0f, 0f, 0f, -1f)
                : new Vector4(dir.x, dir.y, dir.z, cono);
            luzN++;
        }

        float FarolEn(Vector3 p)
        {
            float sum = 0f;
            for (int i = 0; i < luzN; i++)
            {
                Vector3 from = p - new Vector3(luzPos[i].x, luzPos[i].y, luzPos[i].z);
                float d = from.magnitude;
                float att = Mathf.Clamp01(1f - d / Mathf.Max(0.2f, luzPos[i].w));
                att *= att;
                if (luzDir[i].w >= 0f && d > 0.05f)
                {
                    float facing = Vector3.Dot(from / d, new Vector3(luzDir[i].x, luzDir[i].y, luzDir[i].z));
                    att *= Mathf.Clamp01((facing - luzDir[i].w) / Mathf.Max(0.08f, 1f - luzDir[i].w));
                }

                sum += att;
            }

            return Mathf.Clamp01(sum);
        }

        Color TinteDeLuz(Vector3 p, Color baseColor)
        {
            return Color.Lerp(new Color(0.04f, 0.04f, 0.05f), baseColor, FarolEn(p + Vector3.up * 0.9f));
        }

        void MarcarCota()
        {
            if (cota == null) return;
            if (!cota.gameObject.activeSelf) cota.gameObject.SetActive(true);
            bool galeria = zona == Zona.Galeria;
            if (planoMina != null && planoMina.gameObject.activeSelf != galeria)
                planoMina.gameObject.SetActive(galeria);
            string s = galeria
                ? MinaDeSubniveles.Lugar(local)
                : zona == Zona.Pozo
                    ? "Pozo de agua   ·   S baja a la mina   ·   W sube"
                    : zona == Zona.Superficie
                        ? "Laguna El Perol   ·   círculo oscuro al centro   ·   S baja"
                        : "Orilla sur   ·   la mina está bajo el círculo oscuro";
            if (cota.text != s) cota.text = s;
            if (!galeria || puntoMapa == null) return;
            float u = MinaDeSubniveles.Marca(local);
            puntoMapa.anchorMin = new Vector2(u, 0.5f);
            puntoMapa.anchorMax = new Vector2(u, 0.5f);
            puntoMapa.anchoredPosition = Vector2.zero;
            PintarReloj();
        }

        void Velar()
        {
            if (velo == null) return;
            bool galeria = zona == Zona.Galeria;
            bool pozo = zona == Zona.Pozo;
            bool superficie = zona == Zona.Superficie;
            if (!galeria && !pozo && !superficie)
            {
                if (velo.gameObject.activeSelf) velo.gameObject.SetActive(false);
                return;
            }

            if (!velo.gameObject.activeSelf) velo.gameObject.SetActive(true);
            if (galeria && carga) velo.color = new Color(0.55f, 0.08f, 0.05f, 0.4f);
            else if (galeria && aire == AireMina.Gases) velo.color = new Color(0.32f, 0.2f, 0.06f, 0.42f);
            else if (galeria && !encendida) velo.color = new Color(0.02f, 0.02f, 0.03f, 0.93f);
            else if (galeria) velo.color = new Color(0.01f, 0.01f, 0.012f, 0.02f);
            else if (pozo) velo.color = new Color(0.03f, 0.32f, 0.42f, 0.46f);
            else velo.color = new Color(0.04f, 0.2f, 0.3f, 0.1f);
        }

        void Hud()
        {
            var canvas = battle != null && battle.startScreen != null
                ? battle.startScreen.transform.parent
                : null;
            if (canvas == null) return;

            var plate = Storybook.Plate(canvas, "MontanaHUD", Vector2.zero, Vector2.one, new Color(0f, 0f, 0f, 0f));
            plate.raycastTarget = false;

            var cortina = new GameObject("VeloMina");
            cortina.transform.SetParent(plate.transform, false);
            var rect = cortina.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            velo = cortina.AddComponent<Image>();
            velo.color = new Color(0f, 0f, 0f, 0.9f);
            velo.raycastTarget = false;
            cortina.SetActive(false);

            var kicker = Texto(plate.transform, "Kicker", new Vector2(0.16f, 0.88f), new Vector2(0.84f, 0.97f));
            Storybook.DressText(kicker, LeyDelMonte.SceneLine, Storybook.DisplayFont, 26, Color.white, FontStyle.Bold);
            Storybook.Stamp(kicker);
            linea = Texto(plate.transform, "Linea", new Vector2(0.10f, 0.78f), new Vector2(0.90f, 0.88f));
            Storybook.DressText(linea, LeyDelMonte.MinaOrilla, Storybook.DisplayFont, 18, Color.white, FontStyle.Bold);
            Storybook.Stamp(linea);
            cota = Texto(plate.transform, "Cota", new Vector2(0.28f, 0.012f), new Vector2(0.84f, 0.062f));
            Storybook.DressText(cota, "", Storybook.DisplayFont, 16, new Color(0.95f, 0.88f, 0.68f), FontStyle.Bold);
            cota.alignment = TextAnchor.MiddleCenter;
            relojTexto = Texto(plate.transform, "Reloj", new Vector2(0.58f, 0.90f), new Vector2(0.98f, 0.97f));
            Storybook.DressText(relojTexto, "", Storybook.DisplayFont, 18, new Color(0.98f, 0.86f, 0.42f), FontStyle.Bold);
            relojTexto.alignment = TextAnchor.MiddleRight;
            relojTexto.gameObject.SetActive(false);

            var barra = new GameObject("PlanoMina");
            barra.transform.SetParent(plate.transform, false);
            var barraRect = barra.AddComponent<RectTransform>();
            barraRect.anchorMin = new Vector2(0.22f, 0.066f);
            barraRect.anchorMax = new Vector2(0.78f, 0.084f);
            barraRect.offsetMin = Vector2.zero;
            barraRect.offsetMax = Vector2.zero;
            planoMina = barra.AddComponent<Image>();
            planoMina.color = new Color(0.08f, 0.06f, 0.04f, 0.82f);
            planoMina.raycastTarget = false;
            barra.SetActive(false);

            float portalU = Mathf.InverseLerp(-96f, -2f, MinaDeSubniveles.PortalX);
            var marca = new GameObject("Portal");
            marca.transform.SetParent(barra.transform, false);
            var marcaRect = marca.AddComponent<RectTransform>();
            marcaRect.anchorMin = new Vector2(portalU, 0f);
            marcaRect.anchorMax = new Vector2(portalU, 1f);
            marcaRect.sizeDelta = new Vector2(3f, 0f);
            var marcaImg = marca.AddComponent<Image>();
            marcaImg.color = new Color(0.95f, 0.78f, 0.28f, 0.95f);
            marcaImg.raycastTarget = false;

            var punto = new GameObject("Aqui");
            punto.transform.SetParent(barra.transform, false);
            puntoMapa = punto.AddComponent<RectTransform>();
            puntoMapa.anchorMin = new Vector2(0.5f, 0.5f);
            puntoMapa.anchorMax = new Vector2(0.5f, 0.5f);
            puntoMapa.sizeDelta = new Vector2(12f, 12f);
            var puntoImg = punto.AddComponent<Image>();
            puntoImg.color = new Color(1f, 0.95f, 0.75f, 1f);
            puntoImg.raycastTarget = false;

            var oeste = Texto(barra.transform, "Oeste", new Vector2(0.01f, 0f), new Vector2(0.2f, 1f));
            Storybook.DressText(oeste, "O", Storybook.DisplayFont, 12, new Color(0.9f, 0.82f, 0.6f), FontStyle.Bold);
            oeste.alignment = TextAnchor.MiddleLeft;
            var este = Texto(barra.transform, "Este", new Vector2(0.8f, 0f), new Vector2(0.99f, 1f));
            Storybook.DressText(este, "E salida", Storybook.DisplayFont, 12, new Color(0.9f, 0.82f, 0.6f), FontStyle.Bold);
            este.alignment = TextAnchor.MiddleRight;
            TituloFugaz.Begin(kicker.gameObject);
            lineaFade = TituloFugaz.Begin(linea.gameObject);
            Storybook.IconButton(plate.transform, "Volver", Storybook.Marca.Volver,
                new Vector2(0.908f, 0.02f), new Vector2(0.982f, 0.15f), Close);
            Storybook.IconButton(plate.transform, "Historia", Storybook.Marca.Historia,
                new Vector2(0.018f, 0.83f), new Vector2(0.092f, 0.96f),
                () => battle?.AbrirHistoria());
            movimientos = BotonesDeMovimiento.Armar(
                plate.transform,
                () => PonerPlanta(true),
                () => PonerPlanta(false),
                () => Zoom(0.72f),
                () => Zoom(1.38f));
            Storybook.Brujula(plate.transform);
            plate.transform.SetAsLastSibling();
        }

        void PonerPlanta(bool on)
        {
            if (on && zona != Zona.Tierra) return;
            if (planta == on) return;
            if (on) CamaraTercera.PlanYaw = camara.Yaw;
            else camara.Yaw = CamaraTercera.PlanYaw;
            planta = on;
            snapped = false;
        }

        void Zoom(float mul)
        {
            if (planta && zona == Zona.Tierra)
                planSize = Mathf.Clamp(planSize * mul, 16f, 120f);
            else
                camara.Distance = Mathf.Clamp(camara.Distance * mul, camara.MinDistance, camara.MaxDistance);
            snapped = false;
        }

        class Obrero
        {
            public Transform cuerpo;
            public SpriteFrameAnimator anim;
            public float a;
            public float b;
            public float y;
            public float z;
            public float x;
            public int dir = 1;
        }

        static Text Texto(Transform parent, string nombre, Vector2 min, Vector2 max)
        {
            var go = new GameObject(nombre);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return go.AddComponent<Text>();
        }

        void OcultarPelea()
        {
            if (battle == null || battle.startScreen == null) return;
            var canvas = battle.startScreen.transform.parent;
            if (canvas == null) return;
            string[] names =
            {
                "PlayerHealth_Panel", "EnemyHealth_Panel",
                "AYNI", "Espiritualidad", "Respeto", "Dinero", "Instructions",
            };
            for (int i = 0; i < names.Length; i++)
            {
                var child = canvas.Find(names[i]);
                if (child == null) continue;
                hudBits.Add(child.gameObject);
                hudWas.Add(child.gameObject.activeSelf);
                child.gameObject.SetActive(false);
            }
        }

        void RestaurarPelea()
        {
            for (int i = 0; i < hudBits.Count; i++)
            {
                if (hudBits[i] != null) hudBits[i].SetActive(hudWas[i]);
            }

            hudBits.Clear();
            hudWas.Clear();
        }

        void GuardarCamara()
        {
            if (cam == null) return;
            saved = new Pose
            {
                Position = cam.transform.position,
                Rotation = cam.transform.rotation,
                Orthographic = cam.orthographic,
                Size = cam.orthographicSize,
                Field = cam.fieldOfView,
                Near = cam.nearClipPlane,
                Far = cam.farClipPlane,
                Background = cam.backgroundColor,
                Clear = cam.clearFlags,
            };
            savedPose = true;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = Cielo;
            RenderSettings.fogStartDistance = 40f;
            RenderSettings.fogEndDistance = 180f;
        }

        void RestaurarCamara()
        {
            if (restored || !savedPose || cam == null) return;
            restored = true;
            cam.orthographic = saved.Orthographic;
            cam.orthographicSize = saved.Size;
            cam.fieldOfView = saved.Field;
            cam.nearClipPlane = saved.Near;
            cam.farClipPlane = saved.Far;
            cam.backgroundColor = saved.Background;
            cam.clearFlags = saved.Clear;
            cam.transform.SetPositionAndRotation(saved.Position, saved.Rotation);
        }
    }
}
