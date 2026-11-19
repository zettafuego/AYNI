using System.Collections.Generic;
using AYNI.Narrative;
using AYNI.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace AYNI.Battle
{
    /// <summary>
    /// Chapter I as a walk across the Plaza de Armas.
    /// When the OSM plan loads, the square is that plaza: octagonal fountain,
    /// cross and diagonal paths, eight lawns, and the four streets around it.
    /// The diamond remains only if the plan is missing.
    /// Built at runtime, off to the side of the lagoon. The scene file stays as it is.
    /// </summary>
    [DefaultExecutionOrder(400)]
    public class CajamarcaCalles : MonoBehaviour
    {
        public const int VistaCount = 2;

        const float PlazaR = 14f;
        const float StreetOuter = 19f;

        enum Vista
        {
            Planta,
            Inclinacion,
        }

        enum Skin
        {
            Tierra,
            Empedrado,
            Plaza,
            Adobe,
            Piedra,
            Teja,
            Pintura,
            Catedral,
            Francisco,
            Count,
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
        }

        sealed class Walker
        {
            public Transform Body;
            public SpriteFrameAnimator Anim;
            public Vector3 Local;
            public Vector3 Goal;
            public Vector3 Home;
            public float Wait;
            public float Roam;
            public float Speed = 2.3f;
            public bool Hero;
            public bool Route;
            public bool Chase;
            public float Detour;
            public Vector3 There;
            public float Vy;
            public Vector3 Push;
            public bool Grounded = true;
            public bool Fell;
            public bool Canta;
            public bool Toca;
            public bool Chicha;
            public bool Pogo;
            public int Puesto;
            public bool Caido;
            public bool Llevado;
            public bool Policia;
            public bool Jefe;
            public bool Vende;
            public bool Baile;
            public bool Comparsa;
            public bool Lider;
            public bool Alumbra;
            public float Lado;
            public float Atras;
            /// <summary>Altura extra del piso propio (tarima, tablado, carro).</summary>
            public float PisoExtra;
            public bool Roba;
            public float RoboEn;
            public Color Tinte = Color.white;
            public float Fase;
            public Vector3 Talla;
        }

        [System.Serializable]
        sealed class StreetLabel
        {
            public string name;
            public float x;
            public float z;
            public float angle;
            public float size;
        }

        [System.Serializable]
        sealed class StreetLabelSet
        {
            public StreetLabel[] streets;
        }

#pragma warning disable CS0649
        [System.Serializable]
        sealed class TramoFila
        {
            public string name;
            public float x0;
            public float z0;
            public float x1;
            public float z1;
        }

        [System.Serializable]
        sealed class TramoSet
        {
            public TramoFila[] tramos;
            public StreetLabel[] rotulos;
        }
#pragma warning restore CS0649

        sealed class StreetLabelFit : MonoBehaviour
        {
            public float Target;

            void LateUpdate()
            {
                // El alto del texto depende de la fuente del sistema: se mide ya generado.
                var filter = GetComponent<MeshFilter>();
                if (filter == null || filter.sharedMesh == null) return;
                float tall = filter.sharedMesh.bounds.size.y;
                if (tall < 0.01f) return;
                transform.localScale = Vector3.one * (Mathf.Max(0.05f, Target) / tall);
                enabled = false;
            }
        }

        /// <summary>El letrero mira a la cámara. El palo se queda quieto.</summary>
        sealed class PosteEsquina : MonoBehaviour
        {
            void LateUpdate()
            {
                var cam = Camera.main;
                if (cam == null) return;
                Vector3 toCam = cam.transform.position - transform.position;
                if (toCam.sqrMagnitude < 0.0001f) return;
                Vector3 up = cam.transform.up;
                up -= Vector3.Project(up, toCam.normalized);
                if (up.sqrMagnitude < 0.0001f) up = Vector3.up;
                transform.rotation = Quaternion.LookRotation(toCam, up);
            }
        }

        public enum Pieza
        {
            Casa,
            Arbol,
            Banco,
            Farol,
            Flor,
            Casona,
            Quitar,
            Edificio,
            Colonial,
        }

        [System.Serializable]
        public struct PiezaDato
        {
            public Pieza Tipo;
            public float X;
            public float Z;
            public float Yaw;
            public int Variante;
            public string Nombre;
        }

        [System.Serializable]
        public class PiezasArchivo
        {
            public PiezaDato[] items;
        }

        struct RecetaCasona
        {
            public float Ancho;
            public float Alto;
            public float Fondo;
            public float Rise;
            public int Pisos;
            public int Balcones;
            public float Puerta;
            public int Ventanas;
            public Color Muro;
            public Color Zocalo;
        }

        /// <summary>Una pieza puesta a mano. Queda en la escena para editarla sin Play.</summary>
        public class PiezaDeMapa : MonoBehaviour
        {
            public Pieza Tipo;
            public float X;
            public float Z;
            public float Yaw;
            public int Variante;
            public string Nombre;
            public int Indice;

            void LateUpdate()
            {
                if (Application.isPlaying) return;
                Vector3 p = transform.localPosition;
                X = p.x;
                Z = p.z;
            }
        }

        /// <summary>El nombre de origen es el del plano. El texto visible se puede cambiar en la partida.</summary>
        sealed class CalleEditable : MonoBehaviour
        {
            public string Origen;
        }

        /// <summary>
        /// The name stays along the street. If the map turn would leave it upside down, flip it.
        /// </summary>
        sealed class CalleLegible : MonoBehaviour
        {
            public float Yaw;

            void LateUpdate()
            {
                var cam = Camera.main;
                if (cam == null) return;
                transform.localRotation = Quaternion.Euler(90f, Yaw, 0f);
                Vector3 up = transform.up;
                Vector3 camUp = cam.transform.up;
                up.y = 0f;
                camUp.y = 0f;
                if (up.sqrMagnitude < 0.0001f || camUp.sqrMagnitude < 0.0001f) return;
                if (Vector3.Dot(up, camUp) < 0f)
                    transform.localRotation = Quaternion.Euler(90f, Yaw + 180f, 0f);
            }
        }

        // Paleta calibrada a Cajamarca colonial real
        static readonly Color Losa  = new Color(0.93f, 0.90f, 0.82f);       // Piedra caliza local (beige-crema)
        static readonly Color Cerro = new Color(0.58f, 0.64f, 0.38f);       // Cerros con eucaliptos
        static readonly Color Door  = new Color(0.18f, 0.10f, 0.06f);       // Madera oscura colonial
        // Tinte para la puerta cuando lleva la textura de madera encima.
        static readonly Color DoorWood = new Color(0.82f, 0.74f, 0.68f);
        static readonly Color Candle= new Color(0.98f, 0.84f, 0.22f);       // Velas amarillo-oro
        static readonly Color Flower= new Color(0.88f, 0.20f, 0.28f);       // Flores rojas andinas
        static readonly Color Water = new Color(0.22f, 0.44f, 0.58f);       // Acequia El Cumbe
        static readonly Color Glass = new Color(0.30f, 0.36f, 0.42f);       // Vidrios coloniales
        static readonly Color Sky   = new Color(0.48f, 0.66f, 0.90f);       // Cielo andino

        static readonly string[] SkinTex =
        {
            "Bosque/piso_cesped",
            "Calle/piso_empedrado",
            "Calle/piso_plaza",
            "Calle/muro_adobe",
            "Calle/piedra",
            "Calle/teja",
            null,
            "Calle/portada_catedral",
            "Calle/portada_francisco",
        };

        struct HudBit
        {
            public GameObject Go;
            public bool WasOn;
        }

        static CajamarcaCalles current;
        static readonly Dictionary<string, string> NombresGuardados = new Dictionary<string, string>();
        static readonly Dictionary<string, string> CarasGuardadas = new Dictionary<string, string>();
        static readonly List<Vector2> LotesPuestos = new List<Vector2>();
        static readonly List<PiezaDato> piezasGuardadas = new List<PiezaDato>();
        static bool casonasDePlazaPuestas;
        public const string PiezasRuta = "Assets/Generated/Cajamarca/piezas.json";
        static readonly string[] CaraRecurso =
        {
            "Calle/muro_adobe",
            "Calle/piedra",
            "Calle/cal_ocre",
            "Calle/fachada_colonial",
        };
        static readonly string[] CaraNombre = { "adobe", "piedra", "cal", "fachada" };

        public static bool IsOpen => current != null;

        public static bool Editando => current != null && current.editandoNombre;

        public static bool EnTienda { get; private set; }

        public static void FijarTienda(bool dentro)
        {
            EnTienda = dentro;
        }

        public static bool LeerHeroe(out Vector3 local, out Transform body, out SpriteFrameAnimator anim)
        {
            local = Vector3.zero;
            body = null;
            anim = null;
            if (current == null) return false;
            var hero = current.Hero();
            if (hero == null || hero.Body == null) return false;
            local = hero.Local;
            body = hero.Body;
            anim = hero.Anim;
            return true;
        }

        public static void PonerHeroe(Vector3 local)
        {
            if (current == null) return;
            var hero = current.Hero();
            if (hero == null || hero.Body == null) return;
            hero.Local = local;
            hero.Grounded = true;
            hero.Vy = 0f;
            hero.Push = Vector3.zero;
            hero.Body.localPosition = local;
        }

        public static void Decir(string texto)
        {
            if (current == null || string.IsNullOrEmpty(texto)) return;
            current.Note(texto);
        }

        public static Vector3 DeseoMundo()
        {
            return current != null ? current.camara.Wish() : Vector3.zero;
        }

        public static Vector3 DeseoLocal()
        {
            if (current == null) return Vector3.zero;
            return current.camara.ToLocal(current.district, current.camara.Wish());
        }

        public static void AsegurarInclinacion()
        {
            if (current == null || current.vista != Vista.Planta) return;
            current.SetVista(Vista.Inclinacion);
        }

        public static void Acercar(bool cerca)
        {
            if (current == null) return;
            if (cerca)
            {
                current.camara.Distance = 6.4f;
                current.camara.MinDistance = 3.4f;
                current.camara.PivotHeight = 1.55f;
            }
            else
            {
                current.camara.Distance = 10.2f;
                current.camara.MinDistance = 4.2f;
                current.camara.PivotHeight = 2.3f;
            }

            current.snapped = false;
        }

        public static void SembrarCasona(float x, float z, float yaw, int variante)
        {
            if (current == null) return;
            current.GuardarPieza(Pieza.Casona, x, z, yaw, variante, null);
            var pieza = current.Construir(piezasGuardadas.Count - 1);
            if (pieza != null) SolidoDelCamino.Armar(pieza.transform);
        }

        public static void SembrarEdificio(float x, float z, float yaw, int variante, string nombre)
        {
            if (current == null) return;
            current.GuardarPieza(Pieza.Edificio, x, z, yaw, variante, nombre);
            var pieza = current.Construir(piezasGuardadas.Count - 1);
            if (pieza != null) SolidoDelCamino.Armar(pieza.transform);
        }

        /// <summary>Paradero de la plaza, en la calzada este, para el bus a Baños.</summary>
        public static Vector3 PuntoPlazaEste()
        {
            if (current == null) return new Vector3(78f, 0f, -4f);
            if (current.StreetLane(2, out Vector2 lane, out _, out _))
                return new Vector3(lane.x, 0f, lane.y);
            if (current.plazaFound)
            {
                Vector2 p = current.PlazaWorld(current.plazaHalfU + 6.2f, 0f);
                return new Vector3(p.x, 0f, p.y);
            }

            return new Vector3(78f, 0f, -4f);
        }

        readonly List<Walker> walkers = new List<Walker>();
        readonly List<Transform> streetCars = new List<Transform>();
        readonly List<HudBit> hudBits = new List<HudBit>();
        LagunaBattleManager battle;
        Transform district;
        AyniBakedCajamarca bakedCity;
        Transform dynamicRoot;
        bool bakedLabels;
        GameObject plan;
        Transform streetLabels;
        InputField nombreCampo;
        GameObject nombrePanel;
        string nombreOrigen;
        bool editandoNombre;
        bool editandoMapa;
        bool dijoEditar;
        Pieza herramienta = Pieza.Casa;
        int varianteColonial;
        PiezaDeMapa piezaElegida;
        Transform elementos;
        GameObject paleta;
        Button editarButton;
        Button[] herramientaBoton;
        Pieza[] herramientaTipos;
        List<EjeCalle> ejesCache;
        List<Vector2> portadas;
        bool hasDoor;
        bool walkReady;
        bool walkSpan;
        bool oyoComparsa;
        bool cercaChicha;
        Walker liderComparsa;
        int comparsaSentido = 1;
        float atascoComparsa;
        readonly Vector4[] farolMundo = new Vector4[36];
        int farolCuenta;
        bool oyoConcierto;
        bool oyoBanda;
        bool yendoConcierto;
        bool zetaEnEscenario;
        int fanTurno;
        float quedaFan = 2.2f;
        float atascoFan;
        bool dijoPasion;
        bool pasionShow;
        bool moshSuena;
        bool dijoMosh;
        bool dijoAtropello;
        float moshCompas;
        float moshGolpe;
        Walker atropellado;
        int ambulanciaPaso;
        float ambulanciaReloj;
        float ambulanciaAlong;
        float ambulanciaSide;
        Transform ambulancia;
        AudioSource sirena;
        readonly float[] ondaShow = new float[256];
        bool bandaSola;
        Walker zetaGuia;
        AudioSource temaBanda;
        Vector3 metaHeroe;
        Vector3 metaZeta;
        Vector3 bocaBanda;
        Vector3 pasoBanda;
        readonly List<Vector3> rutaZeta = new List<Vector3>();
        readonly List<Vector3> rutaHeroe = new List<Vector3>();
        int indiceZeta;
        int indiceHeroe;
        int atascoZeta;
        int atascoHeroe;
        bool dijoLadron;
        bool dijoHardcore;
        bool oyoMartin;
        bool batanCerrado;
        bool heroEnPista;
        bool pogoArriba;
        bool pogoGolpe;
        bool farmeoEsteFrame;
        bool dijoFarmeo;
        bool pidioGasolina;
        bool policiaHaciaVuelta;
        bool jefeHaciaVuelta;
        bool dijoJefe;
        bool dijoGolpe;
        bool dijoCorrer;
        bool dijoFlorCerro;
        bool anuncioComida;
        bool comioTurno;
        bool diaCompleto;
        bool juntoComida;
        bool puestoListo;
        int plata = 10;
        int comidasDia;
        int turnoComida;
        float relojDia;
        float auraFarmeo;
        float auraPiso;
        float atractivo;
        float vida = 100f;
        Vector3 puestoComida;
        float cobroLibre;
        float golpeLibre;
        float fotoLibre;
        Vector3 policiaIda;
        Vector3 policiaVuelta;
        Vector3 jefeIda;
        Vector3 jefeVuelta;
        Vector3 batanBoca;
        Vector3 batanEje;
        float walkMinX;
        float walkMaxX;
        float walkMinZ;
        float walkMaxZ;
        float blockRadius = 2.5f;
        bool plazaFound;
        float plazaMinX;
        float plazaMaxX;
        float plazaMinZ;
        float plazaMaxZ;
        float fountainX;
        float fountainZ;
        // La plaza del plano va girada respecto al norte: guardamos su marco
        // (u = lado largo, v = lado corto) para sembrar el jardín en su eje.
        float plazaCos = 1f;
        float plazaSin;
        float plazaHalfU;
        float plazaHalfV;
        bool pistaLista;
        bool calleMedida;
        bool ladoN, ladoS, ladoE, ladoW;
        float pistaInN, pistaOutN, pistaInS, pistaOutS;
        float pistaInE, pistaOutE, pistaInW, pistaOutW;
        float bocaCN, bocaHN, bocaCS, bocaHS, bocaCE, bocaHE, bocaCW, bocaHW;
        float jardinInner, jardinU1, jardinV1, altoJardin;
        readonly List<Vector2> casonas = new List<Vector2>();
        readonly List<Bounds> iglesias = new List<Bounds>();
        bool iglesiasListas;
        readonly List<Vector2> plazaWalk = new List<Vector2>();
        const float PlazaWalk = 8.5f;
        // La cruz de la plaza. Media anchura, para que el sendero se lea
        // entre los cuatro jardines y no se confunda con el paseo.
        const float RutaMedia = 3.15f;
        // Corte de piedra alrededor de la pileta. El cesped empieza afuera.
        const float CorteFuente = 8.6f;
        // Hueco de la taza. Ahí queda la fuente, no la losa del sendero.
        const float BocaFuente = 6.35f;
        // Corte de la plaza: la grada sube a la plana, la pista es un plano
        // inclinado y la vereda es un peldaño más arriba.
        const float Peldaño = 0.16f;
        const float Huella = 0.48f;
        const float VeredaAncho = 2.2f;
        const float PistaBaja = 0.03f;
        const float PistaAlta = 0.18f;
        // Piel para que el asfalto no pelee con el empedrado de abajo.
        const float PistaPiel = 0.04f;
        // Metros que cubre un tile de piso de plaza (igual que en el FBX).
        const float PlanFloorTile = 3.2f;
        float planSize = 96f;
        bool planDetached;
        bool planDrag;
        Vector2 planLook;
        Vector3 planCursor;
        readonly List<Vector2> walk = new List<Vector2>();
        readonly List<byte> walkKind = new List<byte>();
        const byte KindCalle = 1;
        const byte KindBanda = 2;
        const byte KindPlaza = 3;
        readonly List<Vector2> homes = new List<Vector2>();
        readonly HashSet<long> roofCells = new HashSet<long>();
        readonly HashSet<long> waterCells = new HashSet<long>();
        readonly Dictionary<long, List<int>> walkGrid = new Dictionary<long, List<int>>();
        const float Lot = 15f;
        GameObject hud;
        GameObject mountainPaint;
        bool mountainPaintWasOn;
        bool mountainPaintHidden;
        Text line;
        Text vidaTexto;
        Text auraTexto;
        Text atractivoTexto;
        Text comidaTexto;
        BotonesDeMovimiento movimientos;
        bool dijoVuelo;
        Camera cam;
        BattleCameraFollow fightFollow;
        bool fightFollowWasOn;
        bool savedOcclusion;
        bool savedPhysical;
        Pose saved;
        bool savedPose;
        bool restored;
        bool savedFog;
        FogMode savedFogMode;
        Color savedFogColor;
        float savedFogStart;
        float savedFogEnd;
        float savedFogDensity;
        Vista vista = Vista.Inclinacion;
        bool snapped;
        bool soloMarcas;
        bool promesaHecha;
        bool promesaDejoReserva;
        bool ofrendaCargada;
        bool ofrendaEntregada;
        bool colaQuemada;
        bool tropezo;
        bool estabaEnPuerta;
        int flores;
        int velas;
        float cargaMetros;
        Vector3 cargaDesde;
        Vector3 promesaPuerta;
        Text promesaTexto;
        Transform ofrenda;
        readonly List<Transform> floresEn = new List<Transform>();
        readonly List<Transform> floresCerro = new List<Transform>();
        readonly List<Vector4> farolLocal = new List<Vector4>();
        readonly List<Vector4> showLocal = new List<Vector4>();
        readonly List<Color> showColor = new List<Color>();
        readonly List<FocoBatam> focosShow = new List<FocoBatam>();
        readonly Vector4[] showMundo = new Vector4[8];
        readonly Vector4[] showGel = new Vector4[8];
        Transform showParent;
        Vector3 showLado;
        Vector3 showMira;

        struct FocoBatam
        {
            public Transform Can;
            public Transform Haz;
            public Transform Halo;
            public Light Luz;
            public Vector3 Montura;
            public float Fase;
            public float Vel;
        }
        readonly List<Transform> velasEn = new List<Transform>();
        readonly List<MariposaPaso> mariposas = new List<MariposaPaso>();
        readonly List<PiezaBasura> basura = new List<PiezaBasura>();
        readonly List<Transform> tachos = new List<Transform>();
        readonly List<int> tachoClase = new List<int>();
        PiezaBasura basuraEnMano;
        int basuraPuestas;
        int basuraTotal;
        bool basuraAviso;
        bool basuraHecha;
        string basuraDonde;
        float safeUntil;
        bool toldWord;
        bool allowJump = true;
        bool jumpLatch = true;
        TituloFugaz lineFade;
        AudioSource eternoEscenario;
        float eternoDesde = -1f;
        bool eternoYa;
        readonly CamaraTercera camara = new CamaraTercera
        {
            Pitch = 16f,
            Distance = 7.2f,
            MinDistance = 4f,
            MaxDistance = 22f,
            PivotHeight = 1.5f,
        };
        Vector3 sanFranciscoDoor;

        public static void Begin(LagunaBattleManager battle)
        {
            if (current != null || battle == null || BosqueCapitulo.IsOpen) return;
            var go = new GameObject("Cajamarca");
            current = go.AddComponent<CajamarcaCalles>();
            current.Open(battle);
        }

        public static void CloseCurrent()
        {
            if (current != null) current.Close();
        }

        public static void VolcarPiezas()
        {
            if (piezasGuardadas.Count == 0) return;
            try
            {
                string dir = System.IO.Path.Combine(Application.dataPath, "Generated/Cajamarca");
                if (!System.IO.Directory.Exists(dir)) System.IO.Directory.CreateDirectory(dir);
                var archivo = new PiezasArchivo { items = piezasGuardadas.ToArray() };
                string path = System.IO.Path.Combine(dir, "piezas.json");
                System.IO.File.WriteAllText(path, JsonUtility.ToJson(archivo, true));
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("[AYNI] No se pudieron guardar las piezas: " + e.Message);
            }
        }

        public static void RecogerDeLaEscena()
        {
            var todas = Object.FindObjectsByType<PiezaDeMapa>(FindObjectsInactive.Include);
            if (todas == null || todas.Length == 0) return;
            piezasGuardadas.Clear();
            for (int i = 0; i < todas.Length; i++)
            {
                var p = todas[i];
                if (p == null) continue;
                piezasGuardadas.Add(new PiezaDato
                {
                    Tipo = p.Tipo,
                    X = p.X,
                    Z = p.Z,
                    Yaw = p.Yaw,
                    Variante = p.Variante,
                    Nombre = p.Nombre,
                });
            }

            casonasDePlazaPuestas = piezasGuardadas.Count > 0;
            VolcarPiezas();
        }

        public static bool CargarPiezas()
        {
            string path = System.IO.Path.Combine(Application.dataPath, "Generated/Cajamarca/piezas.json");
            if (!System.IO.File.Exists(path)) return false;
            try
            {
                var archivo = JsonUtility.FromJson<PiezasArchivo>(System.IO.File.ReadAllText(path));
                if (archivo == null || archivo.items == null || archivo.items.Length == 0) return false;
                piezasGuardadas.Clear();
                piezasGuardadas.AddRange(archivo.items);
                casonasDePlazaPuestas = true;
                return true;
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("[AYNI] No se pudieron leer las piezas: " + e.Message);
                return false;
            }
        }

        /// <summary>
        /// Deja en el distrito las piezas que Play armaba y luego olvidaba.
        /// No borra lo que ya está. El menú del editor la llama una vez.
        /// </summary>
        public static void PonerPiezasEditables(Transform distrito)
        {
            if (distrito == null || Application.isPlaying) return;
            float relieve = CajamarcaAltura.Present;
            var previo = current;
            var host = new GameObject("__PIEZAS_HOST__");
            host.hideFlags = HideFlags.HideAndDontSave;
            var city = host.AddComponent<CajamarcaCalles>();
            current = city;
            try
            {
                city.district = distrito;
                city.bakedCity = distrito.GetComponentInParent<AyniBakedCajamarca>();
                city.plan = distrito.Find(AyniBakedCajamarca.PlanName)?.gameObject;
                city.soloMarcas = true;
                city.dynamicRoot = distrito;
                CargarPiezas();
                city.SembrarEditables();
                city.RestaurarPiezas();
            }
            finally
            {
                current = previo;
                DestroyImmediate(host);
                CajamarcaAltura.Present = relieve;
            }
        }

        // Altura del horneado, no la del mapa: Play sube el relieve después
        // y así la capilla nueva queda al mismo piso que las casas.
        void SembrarEditables()
        {
            if (district == null || plan == null) return;
            float antes = CajamarcaAltura.Present;
            try
            {
                CajamarcaAltura.Load();
                CajamarcaAltura.Present = CajamarcaAltura.RelieveCalles;
                blockRadius = district.Find("Fuente") != null ? 6f : 0f;
                BindPlanDoor();
                LayMoreStreets();
                DressSubida();
                DressEscalerasDelCerro();
                RaiseChapel();
                BakeWalk();
                if (CompletarCalles()) BakeWalk();
                if (TenderRutaBanos()) BakeWalk();
                BosqueDelCerro();
                SembrarFloresDelCerro();
                FindMainPlaza();
                streetLabels = plan.transform.Find("NombresCalles");
                bakedLabels = streetLabels != null;
                BuildStreetLabels();
                AddSpurLabels();
                AddRutaBanosLabels();
                DressGround();
                HideBuriedLawn();
                if (district.Find("CespedDecidido") == null)
                {
                    var decidido = new GameObject("CespedDecidido");
                    decidido.transform.SetParent(district, false);
                }

                VestirPlaza();
                AplicarCaras();
                if (district.Find("CarasDeFachada") == null)
                {
                    var caras = new GameObject("CarasDeFachada");
                    caras.transform.SetParent(district, false);
                }

                VestirPromesa();
                VestirBasura();
                RaiseCornerPosts();
                PonerFarolas();
                SembrarManto();
                DressMarket();
                PrepararBatan();
                if (Application.isPlaying) SpawnWalkers();
            }
            finally
            {
                CajamarcaAltura.Present = antes;
            }
        }

        // Una marca vacía guarda el sitio. Si ya está en la escena, manda ella.
        bool Colocar(string nombre, ref Vector3 at)
        {
            if (soloMarcas)
            {
                DejarMarca(nombre, at);
                return false;
            }

            if (district == null || string.IsNullOrEmpty(nombre)) return true;
            var root = district.Find("PuestosDeGente");
            var marca = root != null ? root.Find(nombre) : null;
            if (marca == null) return true;
            at = district.InverseTransformPoint(marca.position);
            return true;
        }

        void DejarMarca(string nombre, Vector3 local)
        {
            if (district == null || string.IsNullOrEmpty(nombre)) return;
            var root = district.Find("PuestosDeGente");
            if (root == null)
            {
                var go = new GameObject("PuestosDeGente");
                go.transform.SetParent(district, false);
                root = go.transform;
            }

            if (root.Find(nombre) != null) return;
            var marca = new GameObject(nombre);
            marca.transform.SetParent(root, false);
            marca.transform.localPosition = local;
            marca.AddComponent<MarcaDePuesto>();
        }

        void Open(LagunaBattleManager battle)
        {
            this.battle = battle;
            PalabrasCartilla.Close();
            if (battle.startScreen != null) battle.startScreen.SetActive(false);
            HideBattleHud();
            cam = Camera.main;
            SaveCamera();
            ParkFightCamera();

            // Si el distrito ya está horneado en la escena, se usa tal cual.
            // Solo se reconstruyen los datos de navegación y los actores.
            bakedCity = FindBakedCity();
            district = bakedCity != null
                ? bakedCity.transform.Find(AyniBakedCajamarca.DistrictName)
                : null;

            if (district != null)
            {
                bakedCity.gameObject.SetActive(true);
                plan = district.Find(AyniBakedCajamarca.PlanName)?.gameObject;
                CajamarcaAltura.Load();
                // El plano se horneó con RelieveCalles. En el mapa la subida
                // se marca un poco más. El valle sigue en escala real.
                CajamarcaAltura.Present = CajamarcaAltura.RelieveMapa;

                if (plan != null)
                {
                    blockRadius = district.Find("Fuente") != null ? 6f : 0f;
                    RenderSettings.fogStartDistance = 1600f;
                    RenderSettings.fogEndDistance = 3600f;
                    BindPlanDoor();
                    ExaggerateRelief();
                    LayMoreStreets();
                    DressSubida();
                    DressEscalerasDelCerro();
                    RaiseChapel();
                    BakeWalk();
                    if (CompletarCalles()) BakeWalk();
                    if (TenderRutaBanos()) BakeWalk();
                    BosqueDelCerro();
                    SembrarFloresDelCerro();
                    FindMainPlaza();
                    streetLabels = plan.transform.Find("NombresCalles")?.transform;
                    bakedLabels = streetLabels != null;
                    BuildStreetLabels();
                    AddSpurLabels();
                    AddRutaBanosLabels();
                }
                else
                {
                    bakedCity = null;
                    district = null;
                }
            }

            if (district == null)
            {
                LotesPuestos.Clear();
                bakedCity = null;
                district = new GameObject("Distrito").transform;
                district.SetParent(transform, false);
                district.position = new Vector3(2200f, 0f, 0f);
                BuildDistrict();
            }
            else
            {
                dynamicRoot = new GameObject("__DINAMICO__").transform;
                dynamicRoot.SetParent(district, false);
            }

            // Un solo contrato de suelo para caminantes, taxis y puestos.
            SueloDeLaCalle.Enlazar(CapaPlaza);

            // La arboleda del asset se queda. No se reemplaza al abrir las calles.
            HideBuriedLawn();
            DressGround();
            if (piezasGuardadas.Count == 0) CargarPiezas();
            VestirPlaza();
            AplicarCaras();
            RestaurarCaras();
            VestirPromesa();
            VestirBasura();
            RaiseCornerPosts();
            PonerFarolas();
            SembrarManto();
            SpawnWalkers();
            var lluviaHero = Hero();
            LluviaDeAlmas.Begin(RuntimeParent, lluviaHero != null ? lluviaHero.Body : null,
                (x, z) => SueloDeLaCalle.Altura(x, z), 0.16f);
            DressMarket();
            RestaurarPiezas();
            BuildHud();
            FigurasDelCamino.DressTown(district);
            SolidoDelCamino.Armar(district);
            if (plan != null) RutaHaciaBanos.Preparar(RuntimeParent);
            CasonasDelCamino.Preparar(RuntimeParent);
            HideMountainPaint();
            Jornada.Amanecer();
            Jornada.BeginPlay();
            snapped = false;
            toldWord = false;
            battle?.Contar(LeyDelMonte.StreetKicker, LeyDelMonte.StreetArrive);
            ReleaseCityFromOcclusion();
            // El mismo frame del clic: si se espera a LateUpdate, la vista
            // sigue en la laguna y el capítulo parece vacío.
            AimCamera(true);
        }

        static AyniBakedCajamarca FindBakedCity()
        {
            var all = Object.FindObjectsByType<AyniBakedCajamarca>(FindObjectsInactive.Include);
            return all.Length > 0 ? all[0] : null;
        }

        Transform RuntimeParent => dynamicRoot != null ? dynamicRoot : district;

        void SaveCamera()
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
            };
            savedPose = true;
            savedFog = RenderSettings.fog;
            savedFogMode = RenderSettings.fogMode;
            savedFogColor = RenderSettings.fogColor;
            savedFogStart = RenderSettings.fogStartDistance;
            savedFogEnd = RenderSettings.fogEndDistance;
            savedFogDensity = RenderSettings.fogDensity;
            savedOcclusion = cam.useOcclusionCulling;
            savedPhysical = cam.usePhysicalProperties;
            cam.useOcclusionCulling = false;
            cam.usePhysicalProperties = false;
            cam.ResetProjectionMatrix();
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = Sky;
            RenderSettings.fogStartDistance = 1600f;
            RenderSettings.fogEndDistance = 3600f;
        }

        void RestoreCamera()
        {
            ShowMountainPaint();
            if (fightFollow != null) fightFollow.enabled = fightFollowWasOn;
            if (restored || !savedPose || cam == null) return;
            restored = true;
            cam.useOcclusionCulling = savedOcclusion;
            cam.usePhysicalProperties = savedPhysical;
            cam.ResetProjectionMatrix();
            cam.orthographic = saved.Orthographic;
            cam.orthographicSize = saved.Size;
            cam.fieldOfView = saved.Field;
            cam.nearClipPlane = saved.Near;
            cam.farClipPlane = saved.Far;
            cam.backgroundColor = saved.Background;
            cam.transform.SetPositionAndRotation(saved.Position, saved.Rotation);
            RenderSettings.fog = savedFog;
            RenderSettings.fogMode = savedFogMode;
            RenderSettings.fogColor = savedFogColor;
            RenderSettings.fogStartDistance = savedFogStart;
            RenderSettings.fogEndDistance = savedFogEnd;
            RenderSettings.fogDensity = savedFogDensity;
        }

        static void Kill(Object target)
        {
            if (target == null) return;
            if (Application.isPlaying) Destroy(target);
            else DestroyImmediate(target);
        }

        void Close()
        {
            VolcarPiezas();
            SueloDeLaCalle.Soltar();
            CamaraTercera.Release();
            CajamarcaAltura.Present = 1f;
            RestoreCamera();
            RestoreBattleHud();
            if (hud != null) Destroy(hud);

            // Lo dinámico se va; el distrito horneado se queda en la escena,
            // solo se oculta para que no interfiera con la laguna.
            if (dynamicRoot != null)
            {
                Destroy(dynamicRoot.gameObject);
                dynamicRoot = null;
            }

            if (streetLabels != null && !bakedLabels)
                Destroy(streetLabels.gameObject);
            streetLabels = null;

            if (bakedCity != null)
                bakedCity.gameObject.SetActive(false);

            if (battle != null && battle.startScreen != null)
                battle.startScreen.SetActive(true);
            Destroy(gameObject);
        }

        void OnDestroy()
        {
            SueloDeLaCalle.Soltar();
            VueloDeCamara.Cerrar();
            CamaraTercera.Release();
            CajamarcaAltura.Present = 1f;
            if (current == this) current = null;
            Jornada.SilenciarCama(false);
            Shader.SetGlobalFloat("_FarolN", 0f);
            Shader.SetGlobalFloat("_ShowN", 0f);
            Shader.SetGlobalFloat("_Noche", 0f);
            RestoreCamera();
        }

        void Update()
        {
            Shader.SetGlobalFloat("_Noche", Jornada.Shade);
            MoverLucesShow();
            if (CaminoRelato.Reading)
            {
                jumpLatch = false;
                CamaraTercera.ShowCursor();
                return;
            }

            if (promesaHecha) SeguirElCamino.Tramo();

            if (editandoNombre)
            {
                CamaraTercera.ShowCursor();
                if (Input.GetKeyDown(KeyCode.Escape)) CerrarNombre(false);
                else if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
                    CerrarNombre(true);
                PaintVista();
                return;
            }

            if (Input.GetKeyDown(KeyCode.I) || Input.GetKeyDown(KeyCode.Tab))
                AlforjaDelCamino.Alternar();
            if (AlforjaDelCamino.Abierta)
            {
                CamaraTercera.ShowCursor();
                if (Input.GetKeyDown(KeyCode.Escape)) AlforjaDelCamino.Cerrar();
                PaintVista();
                return;
            }

            allowJump = jumpLatch;
            jumpLatch = true;
            if (!EnTienda && Input.GetKeyDown(KeyCode.V))
            {
                if (editandoMapa) DejarEdicion();
                SetVista(vista == Vista.Planta ? Vista.Inclinacion : Vista.Planta);
            }
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (piezaElegida != null) SoltarPieza();
                else if (editandoMapa) DejarEdicion();
                else if (!AlforjaDelCamino.ConsumeEscape() && !CasonasDelCamino.ConsumeEscape()
                    && !TiendasDelCamino.ConsumeEscape() && !RutaHaciaBanos.ConsumeEscape())
                    Close();
            }
            if (Input.GetKeyDown(KeyCode.C))
            {
                planDetached = false;
                snapped = false;
                CamaraTercera.PlanYaw = 0f;
                camara.Yaw = 0f;
            }

            if (CamaraTercera.TakeNorth())
            {
                camara.Yaw = 0f;
                snapped = false;
            }

            if (vista == Vista.Planta)
            {
                CamaraTercera.Release();
                CamaraTercera.ReadPlanTurn();
            }
            else if (vista == Vista.Inclinacion)
            {
                camara.ReadMouse();
                var lead = Hero();
                if (!EnTienda && lead != null && MiradaDeSubida(lead.Local, out _, out Vector3 uphill))
                {
                    // En la subida la pantalla mira desde abajo. El giro del mouse vuelve al salir.
                    CamaraTercera.ShowCursor();
                    float yaw = Mathf.Atan2(uphill.x, uphill.z) * Mathf.Rad2Deg;
                    camara.Yaw = yaw;
                    camara.Pitch = 24f;
                    CamaraTercera.VistaYaw = yaw;
                    CamaraTercera.UseYaw = true;
                }
            }
            else
                CamaraTercera.Release();

            VueloDeCamara.Paso();
            if (!EnTienda)
            {
                var enPista = Hero();
                heroEnPista = enPista != null && CalleCerrada(enPista.Local.x, enPista.Local.z);
                pasionShow = zetaEnEscenario && temaBanda != null && temaBanda.isPlaying;
                moshSuena = MedirCompas();
                MoverLuzComparsa();
                for (int i = 0; i < walkers.Count; i++)
                {
                    var walker = walkers[i];
                    if (walker.Hero) Steer(walker);
                    else
                    {
                        Pace(walker);
                        Ground(walker);
                    }

                    TeñirNoche(walker);
                }

                Hunt();
            }

            var hero = Hero();
            if (hero != null && !toldWord && hero.Local.sqrMagnitude < 36f)
            {
                toldWord = true;
                battle?.ShowRoadLine("PALABRAS", LeyDelMonte.WordStreet, LeyDelMonte.Voice);
            }

            if (hero != null) PasoPromesa(hero);
            if (hero != null) PasoFloresCerro(hero);
            if (hero != null) PasoComida(hero);
            PasoReloj();
            if (hero != null) PasoBasura(hero);
            if (hero != null) OirComparsa(hero);
            if (hero != null) OirChicha(hero);
            if (hero != null) OirConcierto(hero);
            if (hero != null) OirMartin(hero);
            if (hero != null) OirBanda(hero);
            if (hero != null) VigilarMosh(hero);
            PasoAmbulancia();
            MezclarShow(hero);
            if (hero != null) PasoLadrones(hero);
            if (hero != null) VerJefe(hero);
            if (hero != null && !EnTienda && Input.GetKeyDown(KeyCode.P))
                TomarFoto(hero);
            if (!farmeoEsteFrame)
                auraFarmeo = Mathf.Max(auraPiso, auraFarmeo - 4.2f * Time.deltaTime);
            farmeoEsteFrame = false;
            PintarSignos();
            Jornada.MarcarTema(oyoComparsa || oyoConcierto || oyoMartin || oyoBanda);

            ReadPlan();
            bool sobreUi = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
            if (vista != Vista.Planta) planDrag = false;
            if (Input.GetMouseButtonDown(0) && !sobreUi && !planDrag)
                TocarCiudad();
            PaintVista();
            BrujulaDelMapa.Rumbo = vista == Vista.Planta ? CamaraTercera.PlanYaw : camara.Yaw;
            BrujulaDelMapa.Mostrar = true;
        }

        void ParkFightCamera()
        {
            if (cam == null) return;
            fightFollow = cam.GetComponent<BattleCameraFollow>();
            if (fightFollow == null) return;
            fightFollowWasOn = fightFollow.enabled;
            fightFollow.enabled = false;
        }

        void ReleaseCityFromOcclusion()
        {
            if (district == null) return;
            var renderers = district.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                var renderer = renderers[i];
                if (!renderer.gameObject.activeInHierarchy) continue;
                renderer.forceRenderingOff = false;
                renderer.allowOcclusionWhenDynamic = false;
            }
        }

        void LateUpdate()
        {
            if (!mountainPaintHidden) HideMountainPaint();
            LlevarOfrenda();
            LlevarBasura();
            MoverMariposas();
            AimCamera(false);
            for (int i = 0; i < walkers.Count; i++)
            {
                var cuerpo = walkers[i].Body;
                if (cuerpo == null || walkers[i].Llevado) continue;
                Aim(cuerpo);
                if (walkers[i].Caido)
                    cuerpo.Rotate(0f, 0f, 74f, Space.Self);
            }
            for (int i = 0; i < streetCars.Count; i++)
                Aim(streetCars[i]);
            ShowCityVista();
        }

        void AimCamera(bool force)
        {
            if (cam == null) cam = Camera.main;
            var hero = Hero();
            if (cam == null || hero == null || hero.Body == null) return;

            Vector3 feet = PiesDeCamara(hero);
            Vector3 position;
            Quaternion rotation;
            float along = 0f;
            Vector3 uphill = Vector3.forward;
            bool subida = vista != Vista.Planta && MiradaDeSubida(hero.Local, out along, out uphill);
            if (vista == Vista.Planta)
            {
                cam.orthographic = true;
                cam.orthographicSize = plan != null ? planSize : 22f;
                cam.nearClipPlane = 0.3f;
                cam.farClipPlane = plan != null ? 3600f : 140f;
                Vector3 focus = feet;
                if (plan != null && planDetached)
                    focus = district.TransformPoint(new Vector3(planLook.x, 0f, planLook.y));
                focus.y = feet.y;
                // La cámara aérea tiene que quedar por encima de los cerros.
                // Euler(90, PlanYaw, 0): yaw 0 keeps screen-up on +Z. The turn is unbounded.
                position = focus + Vector3.up * 420f;
                rotation = Quaternion.Euler(90f, CamaraTercera.PlanYaw, 0f);
            }
            else if (subida)
            {
                cam.orthographic = false;
                cam.fieldOfView = 58f;
                cam.nearClipPlane = 0.25f;
                cam.farClipPlane = plan != null ? 2200f : 180f;
                PoseSubida(along, uphill, out position, out rotation);
            }
            else
            {
                cam.orthographic = false;
                cam.fieldOfView = 50f;
                cam.nearClipPlane = 0.3f;
                cam.farClipPlane = plan != null ? 2200f : 180f;
                camara.Pose(feet, out position, out rotation);
                if (!EnTienda)
                {
                    float floor = CajamarcaAltura.SampleWorld(district, position);
                    if (position.y < floor + 1.6f)
                    {
                        position.y = floor + 1.6f;
                        Vector3 pivot = feet + Vector3.up * camara.PivotHeight;
                        Vector3 to = pivot - position;
                        if (to.sqrMagnitude > 0.001f)
                            rotation = Quaternion.LookRotation(to, Vector3.up);
                    }
                }
            }

            Jornada.TintSky(cam, Sky);
            Jornada.TintAir(Sky, new Color(0.80f, 0.82f, 0.78f));
            cam.backgroundColor = RenderSettings.fogColor;
            cam.cullingMask = -1;
            cam.enabled = true;
            bool farFromHero = (cam.transform.position - position).sqrMagnitude > 80f * 80f;
            if (subida)
            {
                // Sin Encuadrar: esa mira al pecho y bajaría la vista. El eje de la escalera está libre.
                if (force || !snapped || farFromHero)
                    cam.transform.SetPositionAndRotation(position, rotation);
                else
                {
                    float t = 1f - Mathf.Exp(-6f * Time.deltaTime);
                    cam.transform.position = Vector3.Lerp(cam.transform.position, position, t);
                    cam.transform.rotation = Quaternion.Slerp(cam.transform.rotation, rotation, t);
                }

                snapped = true;
            }
            else if (vista == Vista.Inclinacion)
            {
                Vector3 pivot = feet + Vector3.up * camara.PivotHeight;
                if (EnTienda)
                {
                    if (force || !snapped || farFromHero)
                        cam.transform.SetPositionAndRotation(position, rotation);
                    else
                    {
                        float t = 1f - Mathf.Exp(-8f * Time.deltaTime);
                        cam.transform.position = Vector3.Lerp(cam.transform.position, position, t);
                        cam.transform.rotation = Quaternion.Slerp(cam.transform.rotation, rotation, t);
                    }
                }
                else
                    SolidoDelCamino.Encuadrar(cam, pivot, position, force || !snapped || farFromHero);
                snapped = true;
            }
            else if (force || !snapped || farFromHero)
            {
                cam.transform.SetPositionAndRotation(position, rotation);
                snapped = true;
            }
            else
            {
                float t = 1f - Mathf.Exp(-5f * Time.deltaTime);
                cam.transform.position = Vector3.Lerp(cam.transform.position, position, t);
                cam.transform.rotation = rotation;
            }
        }

        // En el concierto el héroe poguea. La cámara se queda en el suelo
        // para que la pantalla no suba y baje con cada salto.
        Vector3 PiesDeCamara(Walker hero)
        {
            Vector3 feet = hero.Body.position;
            if (!batanCerrado || !CalleCerrada(hero.Local.x, hero.Local.z)) return feet;
            float suelo = SueloDeLaCalle.Altura(hero.Local.x, hero.Local.z);
            if (hero.Local.y > suelo + 2.4f) return feet;
            var quieto = new Vector3(hero.Local.x, suelo, hero.Local.z);
            return district != null ? district.TransformPoint(quieto) : quieto;
        }

        void Aim(Transform body)
        {
            if (body == null || cam == null) return;
            if (vista == Vista.Planta)
            {
                body.rotation = cam.transform.rotation;
                return;
            }

            Vector3 flat = cam.transform.forward;
            flat.y = 0f;
            if (flat.sqrMagnitude < 0.001f) return;
            body.rotation = Quaternion.LookRotation(flat, Vector3.up);
        }

        void HideBattleHud()
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
                hudBits.Add(new HudBit { Go = child.gameObject, WasOn = child.gameObject.activeSelf });
                child.gameObject.SetActive(false);
            }
        }

        void RestoreBattleHud()
        {
            for (int i = 0; i < hudBits.Count; i++)
            {
                if (hudBits[i].Go != null)
                    hudBits[i].Go.SetActive(hudBits[i].WasOn);
            }
            hudBits.Clear();
        }

        void HideMountainPaint()
        {
            if (mountainPaintHidden) return;
            var root = GameObject.Find("ParallaxRoot");
            if (root == null) return;
            mountainPaint = root;
            mountainPaintWasOn = root.activeSelf;
            root.SetActive(false);
            mountainPaintHidden = true;
        }

        void ShowMountainPaint()
        {
            if (!mountainPaintHidden) return;
            mountainPaintHidden = false;
            if (mountainPaint != null)
                mountainPaint.SetActive(mountainPaintWasOn);
        }

        void SetVista(Vista next)
        {
            if (vista == next) return;
            if (next == Vista.Planta)
                CamaraTercera.PlanYaw = camara.Yaw;
            else if (vista == Vista.Planta)
                camara.Yaw = CamaraTercera.PlanYaw;
            vista = next;
            planDrag = false;
            snapped = false;
            if (line == null || plan == null) return;
            if (vista == Vista.Planta)
                Note(LeyDelMonte.MapaAyuda);
            else
                Note(promesaHecha ? LeyDelMonte.PromesaHecha : LeyDelMonte.StreetWalk);
        }

        Walker Hero()
        {
            for (int i = 0; i < walkers.Count; i++)
                if (walkers[i].Hero) return walkers[i];
            return null;
        }

        void Ground(Walker walker)
        {
            if (walker?.Body == null) return;
            Vector3 local = walker.Local;
            float piso = SueloDeLaCalle.Altura(local.x, local.z) + walker.PisoExtra;
            if (walker.Llevado) return;
            bool entrePublico = walker.Baile && zetaEnEscenario;
            if ((walker.Pogo && !walker.Caido) || entrePublico)
            {
                // Con el héroe en la pista, o con Cállate y Baila, el pogo va a doble tiempo.
                bool duro = heroEnPista || pasionShow || entrePublico;
                if (entrePublico && walker.Talla.sqrMagnitude < 0.01f && walker.Body != null)
                    walker.Talla = walker.Body.localScale;
                float pulso = PulsoPogo(duro, walker.Fase);
                float aire = pulso * pulso;
                float salto = entrePublico ? 1.65f : (duro ? 1.25f : 0.92f);
                piso += aire * salto;
                if (walker.Talla.sqrMagnitude > 0.01f)
                {
                    float bajo = entrePublico ? 0.72f : (duro ? 0.78f : 0.88f);
                    float cima = entrePublico ? 1.24f : (duro ? 1.16f : 1.08f);
                    float abierto = entrePublico ? 1.18f : (duro ? 1.14f : 1.06f);
                    float cerrado = entrePublico ? 0.8f : (duro ? 0.86f : 0.94f);
                    float alto = Mathf.Lerp(bajo, cima, aire);
                    float ancho = Mathf.Lerp(abierto, cerrado, aire);
                    walker.Body.localScale = new Vector3(
                        walker.Talla.x * ancho, walker.Talla.y * alto, walker.Talla.z);
                }
            }
            else if (walker.Toca)
            {
                // Saltan en el sitio. Cada uno entra en otro tiempo del compás.
                float fase = walker.Home.x * 0.71f + walker.Home.z * 0.33f;
                float pulso = Mathf.Max(0f, Mathf.Sin(Time.time * 5.4f + fase));
                piso += pulso * pulso * 0.62f;
            }
            else if (walker.Hero)
                PoguearHeroe(walker, ref piso);

            local.y = piso;
            walker.Local = local;
            walker.Body.localPosition = local;
        }

        void Steer(Walker walker)
        {
            if (VueloDeCamara.Activo && vista != Vista.Planta && !EnTienda)
            {
                Planear(walker);
                return;
            }

            if (!walker.Grounded)
            {
                Fly(walker);
                return;
            }

            if (yendoConcierto && CaminarHeroe(walker))
                return;

            Vector3 localMove;
            Vector3 worldMove;
            if (vista == Vista.Inclinacion)
            {
                worldMove = camara.Wish();
                localMove = camara.ToLocal(district, worldMove);
            }
            else
            {
                localMove = camara.PlanLocal(district);
                worldMove = CamaraTercera.WorldDir(district, localMove);
            }

            bool correr = (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
                && localMove.sqrMagnitude > 0.04f;
            walker.Anim?.Prisa(correr);
            if (correr && !dijoCorrer)
            {
                dijoCorrer = true;
                Note(LeyDelMonte.CorreCalle);
            }

            float paso = correr ? 9.6f : 5.6f;
            bool golpe = Input.GetKeyDown(KeyCode.F) && Golpear(walker);
            if (!golpe && allowJump && Input.GetKeyDown(KeyCode.Space))
            {
                Hop(walker, localMove, paso);
                return;
            }

            bool moving = Slide(walker, localMove, paso);
            Ground(walker);
            if (!golpe && pogoGolpe && localMove.sqrMagnitude < 0.04f)
                walker.Anim?.PlayJump();
            else if (!golpe)
                walker.Anim?.PlayStep(worldMove, moving);
            pogoGolpe = false;
            walker.Body.localPosition = walker.Local;
        }

        void Planear(Walker walker)
        {
            Vector3 localMove;
            Vector3 worldMove;
            if (vista == Vista.Inclinacion)
            {
                worldMove = camara.Wish();
                localMove = camara.ToLocal(district, worldMove);
            }
            else
            {
                localMove = camara.PlanLocal(district);
                worldMove = CamaraTercera.WorldDir(district, localMove);
            }

            bool correr = (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
                && localMove.sqrMagnitude > 0.04f;
            walker.Anim?.Prisa(correr);
            float paso = correr ? 9.6f : 5.6f;
            float subir = VueloDeCamara.Ve ? VueloDeCamara.Subir : 0f;
            float ground = SueloDeLaCalle.Altura(walker.Local.x, walker.Local.z);
            bool bajo = walker.Local.y <= ground + 0.45f;
            if (Mathf.Abs(subir) < 0.08f && !bajo) subir = -0.45f;
            if (Mathf.Abs(subir) < 0.08f && bajo)
            {
                walker.Grounded = true;
                walker.Vy = 0f;
                bool golpe = Input.GetKeyDown(KeyCode.F) && Golpear(walker);
                if (!golpe && allowJump && Input.GetKeyDown(KeyCode.Space))
                {
                    Hop(walker, localMove, paso);
                    return;
                }

                bool moving = Slide(walker, localMove, paso);
                Ground(walker);
                if (!golpe) walker.Anim?.PlayStep(worldMove, moving);
                walker.Body.localPosition = walker.Local;
                return;
            }

            walker.Grounded = false;
            Vector3 step = localMove * paso * Time.deltaTime;
            Vector3 next = walker.Local + new Vector3(step.x, 0f, step.z);
            if (localMove.sqrMagnitude > 0.0001f && !PisoHeroe(walker, next.x, next.z))
            {
                var alongX = new Vector3(walker.Local.x + step.x, walker.Local.y, walker.Local.z);
                var alongZ = new Vector3(walker.Local.x, walker.Local.y, walker.Local.z + step.z);
                if (PisoHeroe(walker, alongX.x, alongX.z)) next = alongX;
                else if (PisoHeroe(walker, alongZ.x, alongZ.z)) next = alongZ;
                else next = walker.Local;
            }

            float y = walker.Local.y + subir * 9.2f * Time.deltaTime;
            float piso = SueloDeLaCalle.Altura(next.x, next.z);
            if (y < piso) y = piso;
            if (y > piso + 46f) y = piso + 46f;
            var raised = new Vector3(next.x, y, next.z);
            if (!Cabe(walker, walker.Local, raised))
            {
                var solo = new Vector3(walker.Local.x, y, walker.Local.z);
                raised = Cabe(walker, walker.Local, solo) ? solo : walker.Local;
            }

            walker.Local = raised;
            walker.Vy = subir * 9.2f;
            walker.Grounded = raised.y <= piso + 0.08f;
            walker.Body.localPosition = raised;
            if (walker.Grounded)
                walker.Anim?.PlayStep(worldMove, localMove.sqrMagnitude > 0.01f);
            else if (subir < -0.2f)
                walker.Anim?.PlayFall();
            else
                walker.Anim?.PlayLocomotion(false, true);
        }

        void AlternarVuelo()
        {
            if (VueloDeCamara.Activo)
            {
                VueloDeCamara.Cerrar();
                PaintVista();
                return;
            }

            if (hud == null) return;
            if (!VueloDeCamara.Abrir(hud.transform, out string aviso))
            {
                Note(aviso);
                PaintVista();
                return;
            }

            if (vista == Vista.Planta) SetVista(Vista.Inclinacion);
            if (!dijoVuelo)
            {
                dijoVuelo = true;
                Note(LeyDelMonte.VueloCalle);
            }

            PaintVista();
        }

        // El golpe mira hacia donde mira la cámara y echa atrás a quien persigue.
        bool Golpear(Walker hero)
        {
            if (Time.time < golpeLibre) return false;
            golpeLibre = Time.time + 0.48f;
            hero.Anim?.PlayAction();
            Vector3 frente = cam != null ? cam.transform.forward : Vector3.forward;
            frente.y = 0f;
            if (frente.sqrMagnitude < 0.01f) frente = Vector3.forward;
            frente.Normalize();
            bool toco = false;
            for (int i = 0; i < walkers.Count; i++)
            {
                var otro = walkers[i];
                if (otro == null || otro.Body == null || (!otro.Chase && !otro.Roba)) continue;
                Vector3 gap = otro.Local - hero.Local;
                gap.y = 0f;
                if (gap.sqrMagnitude > 2.6f * 2.6f) continue;
                if (gap.sqrMagnitude > 0.25f && Vector3.Dot(gap.normalized, frente) < 0.2f) continue;
                Vector3 empuje = gap.sqrMagnitude > 0.04f ? gap.normalized : frente;
                otro.Local += empuje * 6.4f;
                otro.Goal = otro.Local;
                otro.Detour = 1.15f;
                Ground(otro);
                otro.Anim?.PlayHurt();
                toco = true;
            }

            if (toco && !dijoGolpe)
            {
                dijoGolpe = true;
                Note(LeyDelMonte.GolpeCalle);
            }

            return true;
        }

        // Un pulso por compás, o dos si el baile es hardcore. 128 BPM.
        static float PulsoPogo(bool duro, float fase)
        {
            float omega = duro ? 26.8f : 12.5f;
            return Mathf.Max(0f, Mathf.Sin(Time.time * omega + fase));
        }

        void Hop(Walker walker, Vector3 localMove, float speed)
        {
            walker.Grounded = false;
            walker.Fell = false;
            Vector3 push = localMove.sqrMagnitude > 0.01f ? localMove.normalized * speed : Vector3.zero;
            float vy = 7.6f;
            if (batanCerrado)
            {
                MedirBatan(walker.Local.x, walker.Local.z, out float along, out float side);
                bool dentro = CalleCerrada(walker.Local.x, walker.Local.z);
                if (dentro)
                {
                    vy = 11.2f;
                    if (along < 16.2f && Vector3.Dot(push, batanEje) < 3.2f)
                        push -= batanEje * 7.4f;
                    else if (along > 45.8f && Vector3.Dot(push, -batanEje) < 3.2f)
                        push += batanEje * 7.4f;
                }
                else if (EntradaDelBatan(along, side, out Vector3 hacia))
                {
                    vy = 11.2f;
                    if (Vector3.Dot(push, hacia) < 3.2f)
                        push += hacia * 7.4f;
                }
            }

            walker.Vy = vy;
            walker.Push = push;
            walker.Anim?.PlayJump();
        }

        void Fly(Walker walker)
        {
            walker.Vy -= 20f * Time.deltaTime;
            Vector3 step = walker.Push * Time.deltaTime;
            Vector3 next = walker.Local + new Vector3(step.x, 0f, step.z);
            if (!PisoHeroe(walker, next.x, next.z))
            {
                var alongX = new Vector3(walker.Local.x + step.x, walker.Local.y, walker.Local.z);
                var alongZ = new Vector3(walker.Local.x, walker.Local.y, walker.Local.z + step.z);
                if (PisoHeroe(walker, alongX.x, alongX.z)) next = alongX;
                else if (PisoHeroe(walker, alongZ.x, alongZ.z)) next = alongZ;
                else next = walker.Local;
            }

            next.y = walker.Local.y + walker.Vy * Time.deltaTime;
            if (!Cabe(walker, walker.Local, next))
            {
                next.x = walker.Local.x;
                next.z = walker.Local.z;
            }

            float ground = SueloDeLaCalle.Altura(next.x, next.z);
            bool hit = next.y <= ground && (walker.Vy <= 0f || ground > walker.Local.y + 0.05f);
            if (hit)
            {
                walker.Local = new Vector3(next.x, ground, next.z);
                walker.Grounded = true;
                walker.Vy = 0f;
                walker.Push = Vector3.zero;
                walker.Fell = false;
                walker.Anim?.PlayStep(Vector3.zero, false);
            }
            else
            {
                walker.Local = next;
                if (walker.Vy < -1.4f && !walker.Fell)
                {
                    walker.Fell = true;
                    walker.Anim?.PlayFall();
                }
                else if (!walker.Fell)
                    walker.Anim?.PlayLocomotion(false, true);
            }

            walker.Body.localPosition = walker.Local;
        }

        void Pace(Walker walker)
        {
            if (walker.Baile && zetaEnEscenario && temaBanda != null && temaBanda.isPlaying)
            {
                if (moshSuena)
                    CorrerMosh(walker);
                else
                    BailarEntrePublico(walker);
                return;
            }

            if (walker.Pogo)
            {
                if (walker.Caido)
                {
                    walker.Anim?.PlayLocomotion(false, false);
                    return;
                }

                if (moshSuena)
                    CorrerMosh(walker);
                else
                    walker.Anim?.PlayLocomotion(false, false);
                return;
            }

            if (walker.Baile && yendoConcierto && !zetaEnEscenario)
            {
                CaminarZeta(walker);
                return;
            }

            if (walker.Comparsa)
            {
                CaminarComparsa(walker);
                return;
            }

            if (walker.Canta || walker.Toca || walker.Chicha || walker.Vende || walker.Baile)
            {
                walker.Anim?.PlayLocomotion(false, false);
                return;
            }

            if (walker.Roba && plata > 0)
                Acechar(walker);
            if (walker.Policia)
                ElegirMetaPolicia(walker);
            else if (walker.Jefe)
                ElegirMetaJefe(walker);
            else if (walker.Chase)
            {
                walker.Detour -= Time.deltaTime;
                var hero = Hero();
                if (hero != null && walker.Detour <= 0f)
                    walker.Goal = FrenteDeAura(hero.Local, walker.Local, 7.5f, auraFarmeo >= 32f);
            }

            walker.Wait -= Time.deltaTime;
            Vector3 to = walker.Goal - walker.Local;
            to.y = 0f;
            if (!walker.Chase && !walker.Policia && !walker.Jefe && (to.magnitude < 0.6f || walker.Wait < -2f))
            {
                if (walker.Route) SwapRoute(walker);
                else walker.Goal = walker.Roam > 0.01f ? NearbyPoint(walker) : RandomPoint();
                walker.Wait = Random.Range(0.35f, 1.2f);
                walker.Anim?.PlayStep(Vector3.zero, false);
                return;
            }

            if (to.sqrMagnitude < 0.04f)
            {
                walker.Anim?.PlayStep(Vector3.zero, false);
                return;
            }

            bool moving = Slide(walker, to.normalized, walker.Speed);
            walker.Anim?.PlayStep(CamaraTercera.WorldDir(district, to.normalized), moving);
            walker.Body.localPosition = walker.Local;
            if (walker.Policia) CobroPolicia(walker);
            if (!moving)
            {
                if (walker.Chase)
                {
                    walker.Goal = RandomPoint();
                    walker.Detour = 0.7f;
                }
                else if (walker.Policia)
                    policiaHaciaVuelta = !policiaHaciaVuelta;
                else if (walker.Jefe)
                    jefeHaciaVuelta = !jefeHaciaVuelta;
                else if (walker.Route) SwapRoute(walker);
                else walker.Goal = walker.Roam > 0.01f ? NearbyPoint(walker) : RandomPoint();
            }
        }

        // Con aura el cazador llega hasta el borde y ahí se queda.
        Vector3 FrenteDeAura(Vector3 hero, Vector3 desde, float radio, bool escudo)
        {
            if (!escudo) return hero;
            Vector3 gap = desde - hero;
            gap.y = 0f;
            if (gap.sqrMagnitude < 0.04f) gap = Vector3.right;
            var meta = hero + gap.normalized * radio;
            meta.y = desde.y;
            return meta;
        }

        void ElegirMetaPolicia(Walker walker)
        {
            var hero = Hero();
            bool alto = auraFarmeo >= 70f;
            if (hero != null && alto)
            {
                Vector3 gap = walker.Local - hero.Local;
                gap.y = 0f;
                if (gap.sqrMagnitude < 11f * 11f)
                {
                    walker.Goal = FrenteDeAura(hero.Local, walker.Local, 11f, true);
                    return;
                }
            }
            else if (hero != null)
            {
                Vector3 gap = hero.Local - walker.Local;
                gap.y = 0f;
                if (gap.sqrMagnitude < 26f * 26f)
                {
                    walker.Goal = hero.Local;
                    if (!pidioGasolina && gap.sqrMagnitude < 16f * 16f)
                    {
                        pidioGasolina = true;
                        Note(LeyDelMonte.PoliciaPide);
                    }

                    return;
                }
            }

            Vector3 posta = policiaHaciaVuelta ? policiaVuelta : policiaIda;
            if ((posta - walker.Local).sqrMagnitude < 1.4f)
                policiaHaciaVuelta = !policiaHaciaVuelta;
            walker.Goal = policiaHaciaVuelta ? policiaVuelta : policiaIda;
        }

        void CobroPolicia(Walker walker)
        {
            if (auraFarmeo >= 70f) return;
            var hero = Hero();
            if (hero == null || Time.time < cobroLibre) return;
            Vector3 gap = hero.Local - walker.Local;
            gap.y = 0f;
            if (gap.sqrMagnitude > 2.3f * 2.3f) return;
            cobroLibre = Time.time + 16f;
            if (battle != null && battle.Cobrar(12f))
                Note(LeyDelMonte.PoliciaCobra);
            else
            {
                battle?.Multar(14f);
                Note(LeyDelMonte.PoliciaMulta);
            }
        }

        void ElegirMetaJefe(Walker walker)
        {
            Vector3 posta = jefeHaciaVuelta ? jefeVuelta : jefeIda;
            if ((posta - walker.Local).sqrMagnitude < 1.4f)
                jefeHaciaVuelta = !jefeHaciaVuelta;
            walker.Goal = jefeHaciaVuelta ? jefeVuelta : jefeIda;
        }

        void VerJefe(Walker hero)
        {
            if (dijoJefe) return;
            var jefe = BuscarJefe();
            if (jefe == null) return;
            Vector3 gap = jefe.Local - hero.Local;
            gap.y = 0f;
            if (gap.sqrMagnitude > 14f * 14f) return;
            dijoJefe = true;
            Note(LeyDelMonte.JefeHonesto);
        }

        // La foto es del jefe. El corrupto la anula si te tiene delante o al lado.
        void TomarFoto(Walker hero)
        {
            if (vista != Vista.Inclinacion)
            {
                Avisar(LeyDelMonte.FotoMapa);
                return;
            }

            if (Time.time < fotoLibre) return;
            var jefe = BuscarJefe();
            if (jefe == null || !Encuadre(hero, jefe, 3.2f, 16f, 0.55f))
            {
                fotoLibre = Time.time + 1.1f;
                Avisar(LeyDelMonte.FotoLejos);
                return;
            }

            if (CorruptoVe(hero))
            {
                fotoLibre = Time.time + 3.6f;
                Avisar(LeyDelMonte.FotoVista);
                return;
            }

            fotoLibre = Time.time + 7f;
            const int soles = 8;
            plata += soles;
            float antes = auraFarmeo;
            auraPiso = Mathf.Min(100f, auraPiso + 8f);
            auraFarmeo = Mathf.Min(100f, Mathf.Max(auraFarmeo, auraPiso));
            farmeoEsteFrame = true;
            atractivo = Mathf.Min(100f, atractivo + 10f);
            bool escudo = antes < 32f && auraFarmeo >= 32f;
            bool alta = antes < 70f && auraFarmeo >= 70f;
            Avisar(LeyDelMonte.FotoCuenta(soles, Mathf.RoundToInt(auraFarmeo), Mathf.RoundToInt(atractivo), escudo, alta));
            PintarSignos();
        }

        Walker BuscarJefe()
        {
            for (int i = 0; i < walkers.Count; i++)
            {
                var walker = walkers[i];
                if (walker != null && walker.Jefe) return walker;
            }

            return null;
        }

        bool Encuadre(Walker hero, Walker otro, float cerca, float lejos, float punto)
        {
            Vector3 gap = otro.Local - hero.Local;
            gap.y = 0f;
            float d = gap.magnitude;
            if (d < cerca || d > lejos) return false;
            return Vector3.Dot(gap / d, FrenteCamara()) >= punto;
        }

        Vector3 FrenteCamara()
        {
            Vector3 frente = cam != null ? cam.transform.forward : Vector3.forward;
            if (district != null) frente = district.InverseTransformDirection(frente);
            frente.y = 0f;
            if (frente.sqrMagnitude < 0.01f) frente = Vector3.forward;
            return frente.normalized;
        }

        bool CorruptoVe(Walker hero)
        {
            for (int i = 0; i < walkers.Count; i++)
            {
                var cop = walkers[i];
                if (cop == null || !cop.Policia) continue;
                Vector3 hacia = hero.Local - cop.Local;
                hacia.y = 0f;
                float d = hacia.magnitude;
                if (d < 4.8f) return true;
                if (d > 20f) return false;
                Vector3 mira = cop.Goal - cop.Local;
                mira.y = 0f;
                if (mira.sqrMagnitude < 0.16f) return d < 9f;
                float visto = Vector3.Dot(mira.normalized, hacia / d);
                if (visto > 0.42f) return true;
                if (visto > -0.2f && d <= 14f) return true;
            }

            return false;
        }

        void Avisar(string text)
        {
            if (line == null) return;
            line.text = text;
            lineFade?.ShowAgain(7.5f);
        }

        void FarmearAura()
        {
            farmeoEsteFrame = true;
            float antes = auraFarmeo;
            auraFarmeo = Mathf.Min(100f, auraFarmeo + 4f);
            battle?.SumarAura(1.2f);
            if (!dijoFarmeo)
            {
                dijoFarmeo = true;
                Note(LeyDelMonte.PogoAura);
            }

            if (antes < 32f && auraFarmeo >= 32f)
                Note(LeyDelMonte.AuraEscudo);
            if (antes < 70f && auraFarmeo >= 70f)
                Note(LeyDelMonte.AuraAlta);
        }

        void Hunt()
        {
            if (auraFarmeo >= 32f) return;
            if (Time.time < safeUntil) return;
            var hero = Hero();
            if (hero == null || !hero.Grounded) return;
            for (int i = 0; i < walkers.Count; i++)
            {
                var hunter = walkers[i];
                if (!hunter.Chase) continue;
                Vector3 gap = hunter.Local - hero.Local;
                gap.y = 0f;
                if (gap.sqrMagnitude > 2.1f * 2.1f) continue;
                hero.Local = hero.Home;
                hero.Grounded = true;
                hero.Vy = 0f;
                hero.Push = Vector3.zero;
                hero.Fell = false;
                Ground(hero);
                if (gap.sqrMagnitude > 0.01f)
                    hunter.Local += gap.normalized * 8f;
                Ground(hunter);
                hunter.Detour = 0.8f;
                safeUntil = Time.time + 1.5f;
                vida = Mathf.Max(0f, vida - 20f);
                bool vacia = vida <= 0f;
                if (vacia) vida = 100f;
                Note(vacia ? LeyDelMonte.VidaCero : LeyDelMonte.TeAlcanzo);
                PintarSignos();
                return;
            }
        }

        void SwapRoute(Walker walker)
        {
            Vector3 back = walker.There;
            walker.There = walker.Goal;
            walker.Goal = back;
            if ((walker.Goal - walker.Local).sqrMagnitude < 9f)
                walker.Goal = FarPoint(walker.Local);
        }

        Vector3 NearbyPoint(Walker walker)
        {
            for (int i = 0; i < 28; i++)
            {
                float a = Random.value * Mathf.PI * 2f;
                float r = Mathf.Sqrt(Random.value) * walker.Roam;
                float x = walker.Home.x + Mathf.Cos(a) * r;
                float z = walker.Home.z + Mathf.Sin(a) * r;
                if (PisoLibre(x, z)) return new Vector3(x, walker.Home.y, z);
            }

            return walker.Home;
        }

        bool Slide(Walker walker, Vector3 move, float speed)
        {
            if (move.sqrMagnitude < 0.0001f) return false;
            Vector3 step = move * speed * Time.deltaTime;
            Vector3 next = walker.Local + step;
            next.y = walker.Local.y;
            bool libre = PisoDe(walker, next.x, next.z);
            if (libre && Cabe(walker, walker.Local, next))
            {
                walker.Local = next;
                return true;
            }

            var alongX = walker.Local + new Vector3(step.x, 0f, 0f);
            libre = PisoDe(walker, alongX.x, alongX.z);
            if (libre && Cabe(walker, walker.Local, alongX))
            {
                walker.Local = alongX;
                return true;
            }

            var alongZ = walker.Local + new Vector3(0f, 0f, step.z);
            libre = PisoDe(walker, alongZ.x, alongZ.z);
            if (libre && Cabe(walker, walker.Local, alongZ))
            {
                walker.Local = alongZ;
                return true;
            }

            return false;
        }

        bool Cabe(Walker walker, Vector3 desde, Vector3 hasta)
        {
            if (walker.Hero && !walker.Grounded && CruzaElBatan(desde, hasta))
                return true;
            if (walker.Hero && EnRecintoPlaza(hasta.x, hasta.z) && !walker.Grounded)
                return true;
            var space = RuntimeParent;
            if (space == null) return true;
            return SolidoDelCamino.Puede(space.TransformPoint(desde), space.TransformPoint(hasta),
                walker.Hero ? 0.22f : 0.42f, walker.Hero ? 1.35f : 1.65f);
        }

        // Jr. del Batán, entre la boca de la plaza y el cruce con Amazonas.
        // La anilla de la plaza queda abierta. El héroe entra de un salto.
        bool CalleCerrada(float x, float z)
        {
            if (!batanCerrado) return false;
            MedirBatan(x, z, out float along, out float side);
            return along > 14f && along < 48f && Mathf.Abs(side) < 7.4f;
        }

        void MedirBatan(float x, float z, out float along, out float side)
        {
            float dx = x - batanBoca.x;
            float dz = z - batanBoca.z;
            along = dx * batanEje.x + dz * batanEje.z;
            side = dx * -batanEje.z + dz * batanEje.x;
        }

        bool EntradaDelBatan(float along, float side, out Vector3 haciaDentro)
        {
            haciaDentro = batanEje;
            if (!batanCerrado || Mathf.Abs(side) > 8.2f) return false;
            if (along >= 10f && along <= 14.6f) return true;
            if (along >= 47.4f && along <= 54f)
            {
                haciaDentro = -batanEje;
                return true;
            }

            return false;
        }

        bool CruzaElBatan(Vector3 desde, Vector3 hasta)
        {
            if (!batanCerrado) return false;
            return CalleCerrada(desde.x, desde.z) != CalleCerrada(hasta.x, hasta.z);
        }

        bool EnRecintoPlaza(float x, float z)
        {
            if (!plazaFound) return OnPlaza(x, z);
            Vector2 loc = PlazaLocal(x, z);
            return Mathf.Abs(loc.x) <= plazaHalfU + 2.4f && Mathf.Abs(loc.y) <= plazaHalfV + 2.4f;
        }

        // Toda la plaza se pisa: jardín, paseo y vereda. La pileta no.
        bool PisoHeroe(Walker walker, float x, float z)
        {
            float dx = x - fountainX;
            float dz = z - fountainZ;
            if (dx * dx + dz * dz < BocaFuente * BocaFuente) return false;
            if (EnRecintoPlaza(x, z) || OnPlaza(x, z)) return true;
            if (!OnStreet(x, z)) return false;
            if (yendoConcierto) return true;
            if (!CalleCerrada(x, z)) return true;
            if (CalleCerrada(walker.Local.x, walker.Local.z)) return true;
            return !walker.Grounded;
        }

        void PoguearHeroe(Walker walker, ref float piso)
        {
            // El pogo es del concierto en el Batán. En la plaza y las calles
            // el héroe camina pegado al piso.
            bool dentro = CalleCerrada(walker.Local.x, walker.Local.z);
            if (!dentro)
            {
                pogoArriba = false;
                if (walker.Talla.sqrMagnitude > 0.01f && walker.Body != null)
                {
                    walker.Body.localScale = walker.Talla;
                    walker.Talla = Vector3.zero;
                }

                return;
            }

            if (walker.Talla.sqrMagnitude < 0.01f && walker.Body != null)
                walker.Talla = walker.Body.localScale;
            bool duro = dentro;
            float pulso = PulsoPogo(duro, 0.4f);
            float aire = pulso * pulso;
            piso += aire * (duro ? 1.65f : 0.85f);
            bool arriba = aire > 0.55f;
            if (arriba && !pogoArriba)
                pogoGolpe = true;

            pogoArriba = arriba;
            if (!duro)
            {
                float altoSuave = Mathf.Lerp(0.9f, 1.08f, aire);
                float anchoSuave = Mathf.Lerp(1.06f, 0.94f, aire);
                if (walker.Talla.sqrMagnitude < 0.01f || walker.Body == null) return;
                walker.Body.localScale = new Vector3(
                    Mathf.Abs(walker.Talla.x) * anchoSuave, walker.Talla.y * altoSuave, walker.Talla.z);
                return;
            }
            if (walker.Talla.sqrMagnitude < 0.01f || walker.Body == null) return;
            float alto = Mathf.Lerp(0.72f, 1.24f, aire);
            float ancho = Mathf.Lerp(1.18f, 0.8f, aire);
            walker.Body.localScale = new Vector3(
                Mathf.Abs(walker.Talla.x) * ancho, walker.Talla.y * alto, walker.Talla.z);
        }

        bool PisoLibre(float x, float z) => OnStreet(x, z) && !CalleCerrada(x, z);

        // Durante la ida al concierto, Zetafuego cruza la valla caminando.
        bool PisoDe(Walker walker, float x, float z)
        {
            if (walker.Hero) return PisoHeroe(walker, x, z);
            if (!OnStreet(x, z)) return false;
            if (walker.Pogo)
                return !batanCerrado || CalleCerrada(x, z);
            if (walker.Baile && (yendoConcierto || zetaEnEscenario)) return true;
            return !CalleCerrada(x, z);
        }

        bool OnStreet(float x, float z)
        {
            float fx = x - fountainX;
            float fz = z - fountainZ;
            if (fx * fx + fz * fz < blockRadius * blockRadius) return false;
            if (walkReady) return OnPlan(x, z);
            float l1 = Mathf.Abs(x) + Mathf.Abs(z);
            if (l1 <= PlazaR - 0.35f) return true;
            if (l1 >= PlazaR + 0.45f && l1 <= StreetOuter - 0.35f) return true;
            if (Mathf.Abs(x) <= 2.05f && z >= PlazaR - 1f && z <= 32f) return true;
            if (Mathf.Abs(x) <= 2.05f && z <= -(PlazaR - 1f) && z >= -38.2f) return true;
            return false;
        }

        bool OnPlan(float x, float z)
        {
            int ix = Mathf.FloorToInt(x / 12f);
            int iz = Mathf.FloorToInt(z / 12f);
            if (!walkGrid.TryGetValue(WalkKey(ix, iz), out var hits)) return false;
            var p = new Vector2(x, z);
            // Tolerancia de 0.3 m: las aceras y calzadas del plano son franjas
            // separadas y en las esquinas quedan rendijas de pocos centímetros.
            const float pad = 0.3f;
            var px = new Vector2(x + pad, z);
            var nx = new Vector2(x - pad, z);
            var pz = new Vector2(x, z + pad);
            var nz = new Vector2(x, z - pad);
            for (int i = 0; i < hits.Count; i++)
            {
                int t = hits[i] * 3;
                var a = walk[t];
                var b = walk[t + 1];
                var c = walk[t + 2];
                if (Inside(p, a, b, c) || Inside(px, a, b, c) || Inside(nx, a, b, c)
                    || Inside(pz, a, b, c) || Inside(nz, a, b, c))
                    return true;
            }

            return false;
        }

        bool OnCalle(float x, float z)
        {
            return EnKind(x, z, KindCalle);
        }

        bool EnKind(float x, float z, byte kind)
        {
            if (!walkReady || walkKind.Count == 0) return false;
            int ix = Mathf.FloorToInt(x / 12f);
            int iz = Mathf.FloorToInt(z / 12f);
            if (!walkGrid.TryGetValue(WalkKey(ix, iz), out var hits)) return false;
            var p = new Vector2(x, z);
            const float pad = 0.35f;
            var px = new Vector2(x + pad, z);
            var nx = new Vector2(x - pad, z);
            var pz = new Vector2(x, z + pad);
            var nz = new Vector2(x, z - pad);
            for (int i = 0; i < hits.Count; i++)
            {
                int id = hits[i];
                if (id < 0 || id >= walkKind.Count || walkKind[id] != kind) continue;
                int t = id * 3;
                if (t + 2 >= walk.Count) continue;
                var a = walk[t];
                var b = walk[t + 1];
                var c = walk[t + 2];
                if (Inside(p, a, b, c) || Inside(px, a, b, c) || Inside(nx, a, b, c)
                    || Inside(pz, a, b, c) || Inside(nz, a, b, c))
                    return true;
            }

            return false;
        }

        // Asfalto sólo sobre la calzada del plano. Si el FBX no separó Calles,
        // se usa la calle caminable que no es plaza.
        bool EsPista(float x, float z)
        {
            if (OnCalle(x, z)) return true;
            if (calleMedida) return false;
            return OnStreet(x, z) && !OnPlaza(x, z);
        }

        /// <summary>Origen de las coordenadas locales del distrito de la calle.</summary>
        public Transform Distrito => district;

        /// <summary>
        /// Suelo visible de la calle: relieve + capa de plaza + elevación del pie.
        /// Es el contrato que deben compartir caminantes, puestos y la sonda de pies;
        /// si alguien apoya con otro lift, el personaje queda hundido o flotando.
        /// </summary>
        /// <summary>
        /// Suelo que apoya un cuerpo concreto: el contrato más la altura propia
        /// de su apoyo (la tarima del concierto, por ejemplo). La sonda de pies
        /// usa esto para no confundir un tablado con un personaje flotando.
        /// <paramref name="brinca"/> marca a quien salta por animación (pogo o
        /// músico tocando), donde estar sobre el piso es lo esperado.
        /// Devuelve false si el cuerpo no es un caminante del capítulo.
        /// </summary>
        public bool SueloDelCuerpo(Transform body, out float suelo, out bool brinca)
        {
            brinca = false;
            if (body != null)
            {
                for (int i = 0; i < walkers.Count; i++)
                {
                    var w = walkers[i];
                    if (w?.Body != body) continue;
                    suelo = SueloDeLaCalle.Altura(w.Local) + w.PisoExtra;
                    brinca = (w.Pogo && !w.Caido) || w.Toca;
                    return true;
                }
            }

            suelo = body != null ? SueloVisible(body.position) : 0f;
            return false;
        }

        public float SueloVisible(Vector3 world, float lift = 0f)
        {

            return SueloDeLaCalle.AlturaMundo(world, district) + lift;
        }

        // Lo que el pie suma encima del relieve: plana, jardín, calzada o vereda.
        float CapaPlaza(float x, float z)
        {
            if (!pistaLista || !plazaFound) return 0f;
            Vector2 loc = PlazaLocal(x, z);
            float u = loc.x;
            float v = loc.y;
            if (u * u + v * v < BocaFuente * BocaFuente) return 0f;
            if (EsPista(x, z)) return AlturaCalzada(u, v);

            float au = Mathf.Abs(u);
            float av = Mathf.Abs(v);
            float inU = plazaHalfU + (u >= 0f ? pistaInE : pistaInW);
            float inV = plazaHalfV + (v >= 0f ? pistaInN : pistaInS);
            if (au <= inU && av <= inV)
            {
                if (EnCesped(u, v)) return altoJardin;
                if (EnRuta(u, v)) return PistaBaja + Peldaño + 0.035f;
                return PistaBaja + Peldaño;
            }

            if (!calleMedida && EnBanda(u, v, inU, inV))
                return AlturaCalzada(u, v);
            if (!OnPlaza(x, z) && EnVereda(u, v))
                return PistaAlta + Peldaño;
            return 0f;
        }

        bool EnBanda(float u, float v, float inU, float inV)
        {
            float au = Mathf.Abs(u);
            float av = Mathf.Abs(v);
            float outU = plazaHalfU + (u >= 0f ? pistaOutE : pistaOutW);
            float outV = plazaHalfV + (v >= 0f ? pistaOutN : pistaOutS);
            bool norteSur = av >= inV && av <= outV && au <= outU;
            bool esteOeste = au >= inU && au <= outU && av <= outV;
            return norteSur || esteOeste;
        }

        bool EnVereda(float u, float v)
        {
            float au = Mathf.Abs(u);
            float av = Mathf.Abs(v);
            float outU = plazaHalfU + (u >= 0f ? pistaOutE : pistaOutW);
            float outV = plazaHalfV + (v >= 0f ? pistaOutN : pistaOutS);
            bool ladoV = av >= outV && av <= outV + VeredaAncho && au <= outU + VeredaAncho;
            bool ladoU = au >= outU && au <= outU + VeredaAncho && av <= outV + VeredaAncho;
            return ladoV || ladoU;
        }

        bool EnCesped(float u, float v)
        {
            float au = Mathf.Abs(u);
            float av = Mathf.Abs(v);
            if (u * u + v * v < (CorteFuente + 0.2f) * (CorteFuente + 0.2f)) return false;
            if (au > jardinU1 || av > jardinV1) return false;
            return !EnRuta(u, v);
        }

        bool EnDiagonal(float u, float v)
        {
            float tope = Mathf.Max(jardinU1, jardinV1) + 0.6f;
            if (Mathf.Abs(u) > tope || Mathf.Abs(v) > tope) return false;
            return Mathf.Abs(Mathf.Abs(u) - Mathf.Abs(v)) <= RutaMedia * 0.78f;
        }

        // Cruz, diagonales y anillo de la pileta: las ocho huertas de la Plaza de Armas.
        bool EnRuta(float u, float v)
        {
            float au = Mathf.Abs(u);
            float av = Mathf.Abs(v);
            if (au > plazaHalfU - 0.15f || av > plazaHalfV - 0.15f) return false;
            float r2 = u * u + v * v;
            if (r2 < BocaFuente * BocaFuente) return false;
            if (r2 < CorteFuente * CorteFuente) return true;
            if (au <= RutaMedia || av <= RutaMedia) return true;
            return EnDiagonal(u, v);
        }

        static bool Inside(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
        {
            float s1 = (b.x - a.x) * (p.y - a.y) - (b.y - a.y) * (p.x - a.x);
            float s2 = (c.x - b.x) * (p.y - b.y) - (c.y - b.y) * (p.x - b.x);
            float s3 = (a.x - c.x) * (p.y - c.y) - (a.y - c.y) * (p.x - c.x);
            bool hasNeg = s1 < 0f || s2 < 0f || s3 < 0f;
            bool hasPos = s1 > 0f || s2 > 0f || s3 > 0f;
            return !(hasNeg && hasPos);
        }

        static long WalkKey(int x, int z) => ((long)x << 32) | (uint)z;

        Vector3 SpreadPoint(int index, int count)
        {
            if (!walkSpan) return RandomPoint();
            float u = (index + 0.5f) / Mathf.Max(1, count);
            float x0 = Mathf.Lerp(walkMinX + 16f, walkMaxX - 16f, u);
            for (int i = 0; i < 40; i++)
            {
                float x = x0 + Random.Range(-36f, 36f);
                float z = Random.Range(walkMinZ + 12f, walkMaxZ - 12f);
                float dx = x - fountainX;
                float dz = z - fountainZ;
                if (dx * dx + dz * dz < 22f * 22f) continue;
                if (PisoLibre(x, z)) return new Vector3(x, 0.2f, z);
            }

            return RandomPoint();
        }

        Vector3 RandomPoint()
        {
            if (walkReady && walkSpan)
            {
                for (int i = 0; i < 64; i++)
                {
                    float x = Random.Range(walkMinX, walkMaxX);
                    float z = Random.Range(walkMinZ, walkMaxZ);
                    if (PisoLibre(x, z)) return new Vector3(x, 0.2f, z);
                }
            }

            if (walkReady)
            {
                for (int i = 0; i < 48; i++)
                {
                    float x = fountainX + Random.Range(-120f, 120f);
                    float z = fountainZ + Random.Range(-120f, 120f);
                    if (PisoLibre(x, z)) return new Vector3(x, 0.2f, z);
                }

                return new Vector3(fountainX, 0.2f, fountainZ - 20f);
            }

            for (int i = 0; i < 24; i++)
            {
                float x = Random.Range(-16f, 16f);
                float z = Random.Range(-30f, 26f);
                if (OnStreet(x, z)) return new Vector3(x, 0.2f, z);
            }

            return new Vector3(0f, 0.2f, -6f);
        }

        static readonly Color[] CarTint =
        {
            Color.white,
            new Color(0.95f, 0.97f, 1f),
            new Color(1f, 0.97f, 0.93f),
            new Color(0.96f, 1f, 0.96f),
        };

        void DressTown()
        {
            var frames = BattleRuntimeAssets.LoadClip("Calle/Gallina/Conducir");
            if (frames.Length == 0) return;

            float height = frames[0].rect.height / frames[0].pixelsPerUnit;
            float baseScale = height > 0.01f ? 3.4f / height : 0.72f;

            // Una gallina por cada calle que rodea la plaza. Cada una lleva su
            // propio carril, sentido y velocidad, asi que no se pisan.
            if (walkReady && plazaFound && plazaHalfU > 12f && plazaHalfV > 12f)
            {
                int made = 0;
                for (int side = -2; side <= 2; side++)
                {
                    if (side == 0) continue;
                    if (!StreetLane(side, out Vector2 lane, out Vector2 axis, out float reach)) continue;
                    SpawnTaxi(made, lane, axis, reach, baseScale, frames,
                        (made & 1) == 1, TaxiStart[made % TaxiStart.Length]);
                    made++;
                }

                if (made > 0) return;
            }

            Vector2 fallback = new Vector2(0f, walkReady ? -18f : -7f);
            var fallbackAxis = Vector2.right;
            float fallbackReach = walkReady ? 22f : 5.5f;
            SpawnTaxi(0, fallback, fallbackAxis, fallbackReach, baseScale, frames, false, -0.35f);
            SpawnTaxi(1, fallback, fallbackAxis, fallbackReach, baseScale, frames, true, 0.35f);
        }

        static readonly float[] TaxiStart = { -0.55f, 0.45f, -0.2f, 0.65f };

        void SpawnTaxi(int index, Vector2 lane, Vector2 axis, float reach, float baseScale,
            Sprite[] frames, bool rightward, float start)
        {
            Vector2 at = lane + axis * (reach * start);
            Vector3 place = new Vector3(
                at.x, SueloDeLaCalle.Altura(at.x, at.y), at.y);

            var car = new GameObject("GallinaConductora" + (index + 1));
            car.transform.SetParent(RuntimeParent, false);
            float scale = baseScale * (0.96f + 0.03f * (index % 3));
            car.transform.localScale = new Vector3(scale, scale, 1f);

            var ink = car.AddComponent<SpriteRenderer>();
            ink.sortingOrder = 11 + index;
            car.AddComponent<SpriteFrameAnimator>();

            var drive = car.AddComponent<GallinaConductora>();
            drive.Configure(place, axis, rightward);
            float center = Vector2.Dot(lane, axis);
            drive.Left = center - reach;
            drive.Right = center + reach;
            drive.Speed = TaxiSpeed[index % TaxiSpeed.Length];
            drive.Phase = index * 1.7f;
            drive.FollowGround = true;
            ink.color = CarTint[index % CarTint.Length];

            streetCars.Add(car.transform);
        }

        static readonly float[] TaxiSpeed = { 3.4f, 2.5f, 3.0f, 2.1f };

        /// <summary>
        /// Busca la franja de calzada que bordea la plaza por ese lado
        /// (-1 sur, +1 norte, -2 oeste, +2 este) y devuelve su centro.
        /// </summary>
        bool StreetLane(int side, out Vector2 lane, out Vector2 axis, out float reach)
        {
            lane = Vector2.zero;
            axis = Vector2.right;
            reach = 0f;

            float lo = 0f;
            float hi = 0f;
            bool found = false;
            for (float d = 3.2f; d <= 26f; d += 0.5f)
            {
                float u = 0f;
                float v = 0f;
                if (side == -1) v = -(plazaHalfV + d);
                else if (side == 1) v = plazaHalfV + d;
                else if (side == -2) u = -(plazaHalfU + d);
                else u = plazaHalfU + d;

                Vector2 p = PlazaWorld(u, v);
                bool street = OnStreet(p.x, p.y) && !OnPlaza(p.x, p.y);
                if (street)
                {
                    if (!found) lo = d;
                    hi = d;
                    found = true;
                }
                else if (found) break;
            }

            if (!found) return false;

            float mid = (lo + hi) * 0.5f;
            float mu = 0f;
            float mv = 0f;
            if (side == -1) mv = -(plazaHalfV + mid);
            else if (side == 1) mv = plazaHalfV + mid;
            else if (side == -2) mu = -(plazaHalfU + mid);
            else mu = plazaHalfU + mid;

            lane = PlazaWorld(mu, mv);
            axis = side == -1 || side == 1
                ? new Vector2(plazaCos, plazaSin)
                : new Vector2(-plazaSin, plazaCos);
            reach = (side == -1 || side == 1 ? plazaHalfU : plazaHalfV) * 0.85f;
            return true;
        }

        void DressMarket()
        {
            var spots = new List<Vector3>();
            if (plazaFound && plazaHalfU > 12f && plazaHalfV > 12f)
            {
                float u = plazaHalfU;
                float v = plazaHalfV;
                // Sobre la vereda de piedra, no sobre el jardin: el borde
                // dibujado de la vereda queda algo mas adentro que PlazaWalk,
                // asi que los puestos se corren hacia la calle.
                float inU = u - 2.4f;
                float inV = v - 2.4f;

                // Vereda sur: puestos contra la calle. El pasillo al jardín
                // queda libre para caminar.
                float span = Mathf.Min(20f, u - 4f);
                float near = 9f;
                const int perSide = 4;
                for (int side = -1; side <= 1; side += 2)
                {
                    for (int i = 0; i < perSide; i++)
                    {
                        float t = perSide > 1 ? i / (float)(perSide - 1) : 0f;
                        float su = side * Mathf.Lerp(near, span, t);
                        // Un pequeno zigzag para que la hilera no parezca una reja.
                        float wobble = (i % 2 == 0) ? 0.35f : -0.35f;
                        AddStallSpot(spots, PlazaWorld(su, -(inV - wobble)));
                    }
                }

                // Vereda norte: unos pocos, para que la plaza tenga vida cuando
                // el jugador la recorra.
                float[] north = { -0.55f, -0.20f, 0.20f, 0.55f };
                for (int i = 0; i < north.Length; i++)
                    AddStallSpot(spots, PlazaWorld(u * north[i], inV));

                // Veredas este y oeste.
                for (int i = 0; i < 2; i++)
                {
                    float f = (i == 0 ? -1f : 1f) * 0.28f;
                    AddStallSpot(spots, PlazaWorld(inU, v * f));
                    AddStallSpot(spots, PlazaWorld(-inU, v * f));
                }
            }
            else
            {
                // Plaza pequena del distrito procedural: cuatro esquinas del anillo.
                AddStallSpot(spots, new Vector2(-8.8f, -8.8f));
                AddStallSpot(spots, new Vector2(8.8f, -8.8f));
                AddStallSpot(spots, new Vector2(-8.8f, 8.8f));
                AddStallSpot(spots, new Vector2(8.8f, 8.8f));
            }

            Transform feria = district != null ? district.Find("Feria") : null;
            if (feria != null && TienePuestos(feria))
            {
                if (Application.isPlaying && feria.Find("Tiendas") == null)
                    TiendasDelCamino.Preparar(feria);
                return;
            }

            Transform ancla = feria;
            if (ancla == null)
            {
                var go = new GameObject("Feria");
                go.transform.SetParent(district != null ? district : RuntimeParent, false);
                ancla = go.transform;
            }

            GenteDeLaCalle.Dress(ancla, spots);
            if (Application.isPlaying) TiendasDelCamino.Preparar(ancla);
        }

        static bool TienePuestos(Transform feria)
        {
            for (int i = 0; i < feria.childCount; i++)
                if (feria.GetChild(i).name.StartsWith("Puesto_")) return true;
            return false;
        }

        void AddStallSpot(List<Vector3> spots, Vector2 at)
        {
            if (!OnStreet(at.x, at.y)) return;
            // En la plaza grande los puestos van sobre la vereda, no en la calzada.
            if (plazaFound && !OnPlaza(at.x, at.y)) return;
            spots.Add(new Vector3(at.x, SueloDeLaCalle.Altura(at.x, at.y), at.y));
        }

        void SpawnWalkers()
        {
            PrepararBatan();
            Vector3 center = new Vector3(0f, 0.2f, -6f);
            Vector3 left = new Vector3(-3.2f, 0.2f, -3f);
            Vector3 right = new Vector3(3.4f, 0.2f, 2f);
            if (walkReady && plazaFound && plazaHalfV > 0f)
            {
                // Vereda sur de la plaza, en su propio eje.
                float v = -plazaHalfV + PlazaWalk * 0.5f;
                Vector2 heroPos = PlazaWorld(0f, v);
                Vector2 a = PlazaWorld(-3f, v + 2.5f);
                Vector2 b = PlazaWorld(3f, v - 2.5f);
                center = new Vector3(heroPos.x, 0.2f, heroPos.y);
                left = new Vector3(a.x, 0.2f, a.y);
                right = new Vector3(b.x, 0.2f, b.y);
            }
            else if (walkReady)
            {
                center = new Vector3(0f, 0.2f, -20f);
                left = new Vector3(-16f, 0.2f, -18f);
                right = new Vector3(18f, 0.2f, -14f);
            }

            // El elegido se queda en la vereda sur. Los otros dos salen lejos
            // y recorren la ciudad, no dan vueltas al lado del héroe.
            if (walkSpan)
            {
                left = SpreadPoint(1, 8);
                right = SpreadPoint(6, 8);
            }
            center.y = SueloDeLaCalle.Altura(center.x, center.z);
            left.y = SueloDeLaCalle.Altura(left.x, left.z);
            right.y = SueloDeLaCalle.Altura(right.x, right.z);
            bool centroOk = Colocar("Centro", ref center);
            bool izquierdaOk = Colocar("Izquierda", ref left);
            bool derechaOk = Colocar("Derecha", ref right);
            if (centroOk && izquierdaOk && derechaOk)
            {
                var chosen = FighterRoster.Current.Sheet;
                bool viz = chosen == BattleCast.Vizcacha;
                bool conejo = chosen == BattleCast.Conejo;
                Vector3 vizAt = viz ? center : left;
                Vector3 atoqAt = conejo ? right : (viz ? left : center);
                Vector3 conejoAt = conejo ? center : right;
                AddStreet(BattleCast.Vizcacha, vizAt, viz);
                AddStreet(BattleCast.Atoq, atoqAt, !viz && !conejo);
                AddStreet(BattleCast.Conejo, conejoAt, conejo);
            }
            SpawnRoutes();
            SpawnHunters();
            SpawnPolicia();
            SpawnJefe();
            SpawnComparsa();
            SpawnConcierto();
            SpawnMartin();
            SpawnVendedores();
            SpawnBanda();
            SpawnLadrones();
        }

        // Tres cuyes de la raza del comedor. El cocinero y el pregón quedan
        // a los lados de la mesa. El mesero queda de frente a la subida.
        // El ciclo de Quieto es el trabajo: no caminan.
        void SpawnVendedores()
        {
            if (!puestoListo) return;
            var pts = SubidaPts();
            Vector2 cima = pts[pts.Length - 1];
            Vector3 desde = puestoComida - new Vector3(cima.x, 0f, cima.y);
            desde.y = 0f;
            if (desde.sqrMagnitude < 0.01f) desde = Vector3.forward;
            desde.Normalize();
            Vector3 lado = new Vector3(-desde.z, 0f, desde.x);
            var sheets = BattleCast.Puestos;
            Vector3[] sitio =
            {
                puestoComida + desde * 0.15f - lado * 2.35f,
                puestoComida - desde * 1.85f,
                puestoComida + desde * 0.15f + lado * 2.35f,
            };
            for (int i = 0; i < sheets.Length; i++)
            {
                Vector3 at = sitio[i];
                at.y = SueloDeLaCalle.Altura(at.x, at.z);
                if (!Colocar("Puesto" + sheets[i].Id, ref at)) continue;
                var walker = Spawn("Puesto" + sheets[i].Id, sheets[i], StreetScale(CastScale.Of(sheets[i])), true, at, false);
                walker.Home = at;
                walker.Goal = at;
                walker.There = at;
                walker.Roam = 0f;
                walker.Speed = 0f;
                walker.Vende = true;
                walkers.Add(walker);
                walker.Anim?.Desfasar(i);
            }
        }

        void Acechar(Walker walker)
        {
            var hero = Hero();
            if (hero == null) return;
            Vector3 gap = hero.Local - walker.Local;
            gap.y = 0f;
            if (gap.sqrMagnitude > 22f * 22f) return;
            walker.Goal = hero.Local;
        }

        void TeñirNoche(Walker walker)
        {
            if (walker.Body == null) return;
            var ink = walker.Body.GetComponent<SpriteRenderer>();
            if (ink == null) return;
            if (walker.Toca)
            {
                // En el estrado se quedan con su color. La noche no los apaga.
                ink.color = walker.Tinte;
                return;
            }

            float sombra = Jornada.Shade;
            float show = ShowCerca(walker.Local, out Color gel);
            float luz = sombra < 0.04f ? 1f : Mathf.Max(LuzCerca(walker.Local), show);
            Color apagado = new Color(0.2f, 0.22f, 0.32f);
            Color calido = new Color(1f, 0.84f, 0.48f);
            Color tono = Color.Lerp(Color.white, Color.Lerp(apagado, calido, luz), sombra);
            Color tinte = Color.Lerp(Color.white, gel, show * 0.85f);
            Color t = walker.Tinte;
            ink.color = new Color(t.r * tono.r * tinte.r, t.g * tono.g * tinte.g, t.b * tono.b * tinte.b, 1f);
        }

        float ShowCerca(Vector3 local, out Color gel)
        {
            gel = Color.white;
            Color sumaColor = Color.black;
            float suma = 0f;
            for (int i = 0; i < showLocal.Count; i++)
            {
                Vector4 f = showLocal[i];
                float dx = local.x - f.x;
                float dz = local.z - f.z;
                float d = Mathf.Sqrt(dx * dx + dz * dz);
                float k = Mathf.Clamp01(1f - d / Mathf.Max(0.5f, f.w));
                k *= k;
                if (k <= 0f) continue;
                sumaColor += showColor[i] * k;
                suma += k;
            }

            if (suma < 0.04f) return 0f;
            gel = sumaColor / suma;
            return Mathf.Clamp01(suma);
        }

        float LuzCerca(Vector3 local)
        {
            float mejor = 0f;
            for (int i = 0; i < farolLocal.Count; i++)
            {
                Vector4 f = farolLocal[i];
                float dx = local.x - f.x;
                float dz = local.z - f.z;
                float d = Mathf.Sqrt(dx * dx + dz * dz);
                float k = Mathf.Clamp01(1f - d / Mathf.Max(0.5f, f.w));
                k *= k;
                if (k > mejor) mejor = k;
            }

            return mejor;
        }

        void PasoLadrones(Walker hero)
        {
            for (int i = 0; i < walkers.Count; i++)
            {
                var ladron = walkers[i];
                if (!ladron.Roba || ladron.Body == null) continue;
                Vector3 gap = ladron.Local - hero.Local;
                gap.y = 0f;
                if (gap.sqrMagnitude > 1.7f * 1.7f) continue;
                if (Time.time < ladron.RoboEn) continue;
                ladron.RoboEn = Time.time + 8f;
                if (gap.sqrMagnitude > 0.04f)
                {
                    ladron.Local += gap.normalized * 4.5f;
                    ladron.Goal = ladron.Local;
                    Ground(ladron);
                }

                if (plata <= 0)
                {
                    if (!dijoLadron)
                    {
                        dijoLadron = true;
                        Note(LeyDelMonte.LadronSeco);
                    }

                    continue;
                }

                int toma = Mathf.Min(4, plata);
                plata -= toma;
                PintarSignos();
                Note(LeyDelMonte.LadronRoba);
            }
        }

        void PonerFarolas()
        {
            if (district == null) return;
            Transform root = district.Find("Faroles");
            if (root == null)
            {
                var go = new GameObject("Faroles");
                root = go.transform;
                root.SetParent(district, false);
                var puestos = new List<Vector2>();
                if (plazaFound && plazaHalfU > 10f && plazaHalfV > 10f)
                {
                    PonerFilaFarol(root, puestos, plazaHalfU - PlazaWalk - 1.1f, true);
                    PonerFilaFarol(root, puestos, -plazaHalfU + PlazaWalk + 1.1f, true);
                    PonerFilaFarol(root, puestos, plazaHalfV - PlazaWalk - 1.1f, false);
                    PonerFilaFarol(root, puestos, -plazaHalfV + PlazaWalk + 1.1f, false);
                }

                var pts = SubidaPts();
                for (int i = 1; i < pts.Length && puestos.Count < 36; i += 2)
                {
                    Vector2 eje = pts[i] - pts[i - 1];
                    if (eje.sqrMagnitude < 0.01f) continue;
                    eje.Normalize();
                    Vector2 lado = new Vector2(-eje.y, eje.x);
                    Vector2 p = pts[i] + lado * 3.6f;
                    IntentarFarol(root, puestos, p.x, p.y);
                }

                var ejes = Ejes();
                for (int i = 0; i < ejes.Count && puestos.Count < 36; i++)
                {
                    EjeCalle e = ejes[i];
                    float lx = -e.Tz;
                    float lz = e.Tx;
                    for (float d = 14f; d < e.Largo - 10f && puestos.Count < 36; d += 34f)
                    {
                        float x = e.X + e.Tx * d;
                        float z = e.Z + e.Tz * d;
                        if (walkReady && !OnStreet(x, z)) continue;
                        if (plazaFound && OnPlaza(x, z)) continue;
                        IntentarFarol(root, puestos, x + lx * 4.6f, z + lz * 4.6f);
                    }
                }
            }

            PublicarFaroles(root);
        }

        void PonerFilaFarol(Transform root, List<Vector2> puestos, float fijo, bool aLoLargo)
        {
            float largo = aLoLargo ? plazaHalfV : plazaHalfU;
            float tope = largo - PlazaWalk - 4f;
            for (float t = -tope; t <= tope && puestos.Count < 36; t += 20f)
            {
                Vector2 p = aLoLargo ? PlazaWorld(fijo, t) : PlazaWorld(t, fijo);
                IntentarFarol(root, puestos, p.x, p.y);
            }
        }

        void IntentarFarol(Transform root, List<Vector2> puestos, float x, float z)
        {
            if (puestos.Count >= 36) return;
            for (int i = 0; i < puestos.Count; i++)
            {
                float dx = puestos[i].x - x;
                float dy = puestos[i].y - z;
                if (dx * dx + dy * dy < 15f * 15f) return;
            }

            AlzarFarol(root, x, z);
            puestos.Add(new Vector2(x, z));
        }

        void AlzarFarol(Transform root, float x, float z)
        {
            float g = CajamarcaAltura.Sample(x, z);
            if (!PlantProp(root, "Farol", x, z, g, 3.3f, 0f,
                "Calle/Props/light", "Calle/Props/light-square", "Calle/Props/lamp-square"))
            {
                var hierro = new Color(0.16f, 0.15f, 0.14f);
                var vidrio = new Color(1f, 0.84f, 0.38f);
                Kill(BoxPart(root, "Farol", new Vector3(x, g + 1.65f, z), new Vector3(0.14f, 3.2f, 0.14f), null, hierro).GetComponent<Collider>());
                Kill(BoxPart(root, "Farol", new Vector3(x, g + 3.22f, z), new Vector3(0.46f, 0.1f, 0.46f), null, hierro).GetComponent<Collider>());
                Kill(BoxPart(root, "Farol", new Vector3(x, g + 3.02f, z), new Vector3(0.28f, 0.36f, 0.28f), null, vidrio).GetComponent<Collider>());
            }

            var luz = new GameObject("LuzFarol");
            luz.transform.SetParent(root, false);
            luz.transform.localPosition = new Vector3(x, g + 3.02f, z);
            var light = luz.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(1f, 0.78f, 0.32f);
            light.intensity = 2.6f;
            light.range = 16f;
            light.shadows = LightShadows.None;
        }

        void PublicarFaroles(Transform root)
        {
            farolLocal.Clear();
            for (int k = 0; k < farolMundo.Length; k++) farolMundo[k] = Vector4.zero;
            int n = 0;
            if (root != null)
            {
                for (int i = 0; i < root.childCount && n < 35; i++)
                {
                    Transform hijo = root.GetChild(i);
                    if (hijo.name != "LuzFarol") continue;
                    n = SumarFarol(hijo, n);
                }
            }

            if (elementos != null && n < 35)
            {
                var luces = elementos.GetComponentsInChildren<Transform>(true);
                for (int i = 0; i < luces.Length && n < 35; i++)
                {
                    if (luces[i].name != "LuzFarol") continue;
                    n = SumarFarol(luces[i], n);
                }
            }

            farolCuenta = n;
            Shader.SetGlobalVectorArray("_Faroles", farolMundo);
            Shader.SetGlobalFloat("_FarolN", n);
        }

        int SumarFarol(Transform hijo, int n)
        {
            if (hijo == null || n >= 35) return n;
            var dueño = hijo.GetComponentInParent<PiezaDeMapa>();
            if (dueño != null && dueño.Indice < 0) return n;
            Vector3 world = hijo.position;
            Vector3 local = district != null ? district.InverseTransformPoint(world) : world;
            farolLocal.Add(new Vector4(local.x, local.y, local.z, 16f));
            farolMundo[n] = new Vector4(world.x, world.y, world.z, 16f);
            return n + 1;
        }

        // El último cupo del shader sigue al gato del farol. La calle y los
        // cuerpos de la comparsa se encienden donde él camina.
        void MoverLuzComparsa()
        {
            while (farolLocal.Count > farolCuenta)
                farolLocal.RemoveAt(farolLocal.Count - 1);
            Walker luz = null;
            for (int i = 0; i < walkers.Count; i++)
            {
                if (!walkers[i].Alumbra || walkers[i].Body == null) continue;
                luz = walkers[i];
                break;
            }

            int n = farolCuenta;
            if (luz != null && n < farolMundo.Length)
            {
                Vector3 local = luz.Local + Vector3.up * 1.45f;
                Vector3 world = RuntimeParent != null ? RuntimeParent.TransformPoint(local) : local;
                farolMundo[n] = new Vector4(world.x, world.y, world.z, 9.5f);
                Vector3 enCalle = district != null ? district.InverseTransformPoint(world) : local;
                farolLocal.Add(new Vector4(enCalle.x, enCalle.y, enCalle.z, 9.5f));
                n++;
            }

            Shader.SetGlobalVectorArray("_Faroles", farolMundo);
            Shader.SetGlobalFloat("_FarolN", n);
        }

        void SembrarManto()
        {
            if (district == null || district.Find("Manto") != null) return;
            var root = new GameObject("Manto");
            root.transform.SetParent(district, false);
            int n = 0;
            if (plazaFound && plazaHalfU > 8f)
            {
                float paso = 7.2f;
                for (float u = -plazaHalfU + PlazaWalk; u <= plazaHalfU - PlazaWalk && n < 140; u += paso)
                {
                    for (float v = -plazaHalfV + PlazaWalk; v <= plazaHalfV - PlazaWalk && n < 140; v += paso)
                    {
                        if (!EnCesped(u, v)) continue;
                        Vector2 p = PlazaWorld(u, v);
                        if (OnGardenPath(p.x, p.y)) continue;
                        int salt = n++;
                        string pieza = MantoAsset[salt % MantoAsset.Length];
                        bool flor = pieza.IndexOf("flower", System.StringComparison.Ordinal) >= 0;
                        float suelo = CajamarcaAltura.Sample(p.x, p.y);
                        PlantTree(pieza, p.x, p.y, flor ? 0.48f : 0.72f, salt * 37f, suelo, root.transform, flor ? "flower" : "grass");
                        if (salt % 4 != 0 || CercaDeAlgunArbol(p.x, p.y, 8f)) continue;
                        var kinds = TreeKinds();
                        PlantTree(kinds[salt % kinds.Length], p.x, p.y, 9.2f + (salt % 3) * 1.4f, salt * 19f, suelo, root.transform);
                    }
                }
            }

            const float sx = -184f;
            const float sz = -386f;
            float cima = CajamarcaAltura.Sample(sx, sz);
            var casas = HuellasDeCasas();
            int c = 0;
            for (float z = sz - 56f; z <= sz + 32f && c < 80; z += 8.4f)
            {
                for (float x = sx - 52f; x <= sx + 32f && c < 80; x += 8.4f)
                {
                    int salt = c + 3;
                    float jx = x + ((salt * 3) % 5 - 2) * 0.55f;
                    float jz = z + ((salt * 5) % 5 - 2) * 0.55f;
                    if (!SitioDeArbol(jx, jz, sx, sz, cima, casas)) continue;
                    string pieza = MantoAsset[salt % MantoAsset.Length];
                    bool flor = pieza.IndexOf("flower", System.StringComparison.Ordinal) >= 0;
                    PlantTree(pieza, jx, jz, flor ? 0.5f : 0.78f, salt * 29f, CajamarcaAltura.Sample(jx, jz), root.transform, flor ? "flower" : "grass");
                    c++;
                }
            }
        }

        bool CercaDeAlgunArbol(float x, float z, float radio)
        {
            if (district == null) return false;
            float r2 = radio * radio;
            string[] grupos = { "Arboles", "BosqueDelCerro", "Manto" };
            for (int g = 0; g < grupos.Length; g++)
            {
                Transform padre = district.Find(grupos[g]);
                if (padre == null) continue;
                for (int i = 0; i < padre.childCount; i++)
                {
                    Transform hijo = padre.GetChild(i);
                    if (hijo.name.IndexOf("Arbol", System.StringComparison.OrdinalIgnoreCase) < 0
                        && hijo.name.IndexOf("tree", System.StringComparison.OrdinalIgnoreCase) < 0
                        && grupos[g] != "Arboles")
                        continue;
                    Vector3 p = district.InverseTransformPoint(hijo.position);
                    float dx = p.x - x;
                    float dz = p.z - z;
                    if (dx * dx + dz * dz < r2) return true;
                }
            }

            return false;
        }

        void SpawnBanda()
        {
            oyoBanda = false;
            Vector3 at = new Vector3(fountainX + 14f, 0.2f, fountainZ + 18f);
            if (plazaFound && plazaHalfU > 16f)
            {
                Vector2 puesto = PlazaWorld(plazaHalfU * 0.28f, plazaHalfV - PlazaWalk * 0.42f);
                at = new Vector3(puesto.x, 0.2f, puesto.y);
            }

            if (walkReady && !OnStreet(at.x, at.z))
                at = BuscarCalle(at, plazaFound ? new Vector3(plazaCos, 0f, plazaSin) : Vector3.forward);
            at.y = SueloDeLaCalle.Altura(at.x, at.z);
            if (!Colocar("Zetafuego", ref at)) return;
            var sheet = BattleCast.GatoZeta;
            var walker = Spawn("Calle" + sheet.Id, sheet, StreetScale(CastScale.Of(sheet)), true, at, false);
            walker.Home = at;
            walker.Goal = at;
            walker.There = at;
            walker.Roam = 0f;
            walker.Speed = 0f;
            walker.Baile = true;
            walker.Puesto = 8;
            walkers.Add(walker);
            walker.Anim?.Desfasar(1);
            temaBanda = PonerTema(walker.Body.gameObject, "Battle/Audio/quedate-un-rato-mas", ClipCajamarquina(), 0.72f, 6f, 36f, false);
        }

        void OirBanda(Walker hero)
        {
            Walker zeta = null;
            float mejor = 400f;
            for (int i = 0; i < walkers.Count; i++)
            {
                var walker = walkers[i];
                if (!walker.Baile || walker.Body == null) continue;
                float dist = (walker.Local - hero.Local).sqrMagnitude;
                if (dist < mejor)
                {
                    mejor = dist;
                    zeta = walker;
                }
            }

            if (zeta == null) return;
            if (!oyoBanda && mejor < 20f * 20f)
            {
                oyoBanda = true;
                Note(LeyDelMonte.BandaCajamarca);
            }

            if (yendoConcierto || zetaEnEscenario)
            {
                if (yendoConcierto && !zetaEnEscenario && Plano(zeta.Local, metaZeta) < 1.7f)
                {
                    zetaEnEscenario = true;
                    zeta.Speed = 3.2f;
                    if (EternoListo()) ArrancarTemaBanda();
                }

                if (yendoConcierto && zetaEnEscenario && CalleCerrada(hero.Local.x, hero.Local.z))
                    yendoConcierto = false;
                return;
            }

            if (mejor > 4.2f * 4.2f) return;
            if (!EmpezarCamino(zeta)) return;
            yendoConcierto = true;
            zetaGuia = zeta;
            Note(LeyDelMonte.ZetaLleva);
        }

        // Zetafuego no los teletransporta. La ruta pisa calle, da la vuelta a la plaza
        // si el recto se mete al jardín, y entra al Batán por la boca.
        bool EmpezarCamino(Walker zeta)
        {
            if (!batanCerrado) PrepararBatan();
            if (!batanCerrado || zeta == null) return false;
            bocaBanda = PuntoAndable(CrudoBatan(11.2f, 0.15f));
            pasoBanda = PuntoAndable(CrudoBatan(17.4f, 0.2f));
            metaHeroe = PuntoAndable(CrudoBatan(21.5f, -1.1f));
            metaZeta = PuntoAndable(CrudoBatan(31.2f, 1.3f));
            var hero = Hero();
            Vector3 salida = hero != null ? hero.Local : zeta.Local;
            ArmarRuta(rutaZeta, zeta.Local, bocaBanda, pasoBanda, metaZeta);
            ArmarRuta(rutaHeroe, salida, bocaBanda, pasoBanda, metaHeroe);
            indiceZeta = 0;
            indiceHeroe = 0;
            atascoZeta = 0;
            atascoHeroe = 0;
            zeta.Speed = 3.4f;
            zeta.Roam = 0f;
            zeta.Goal = bocaBanda;
            return rutaZeta.Count > 0;
        }

        void ArmarRuta(List<Vector3> ruta, Vector3 desde, Vector3 boca, Vector3 paso, Vector3 fin)
        {
            ruta.Clear();
            var tramo = new List<Vector3>();
            if (!TramoLibre(desde, boca))
            {
                var celdas = BuscarCeldas(desde, boca);
                if (celdas.Count == 0)
                    celdas = ArcoPlaza(desde, boca);
                tramo.AddRange(celdas);
            }

            tramo.Add(Pisar(boca));
            tramo.Add(Pisar(paso));
            tramo.Add(Pisar(fin));
            Acortar(desde, tramo);
            ruta.AddRange(tramo);
        }

        bool CaminarHeroe(Walker hero)
        {
            Vector3 lado = DeseoLateral();
            bool espera = false;
            if (zetaGuia != null && !zetaEnEscenario)
            {
                float yo = Plano(hero.Local, metaHeroe);
                float el = Plano(zetaGuia.Local, metaZeta);
                Vector3 gap = hero.Local - zetaGuia.Local;
                gap.y = 0f;
                if (yo + 4.5f < el && gap.sqrMagnitude > 3.4f * 3.4f)
                    espera = true;
            }

            Vector3 paso = SiguienteRuta(rutaHeroe, ref indiceHeroe, hero.Local, metaHeroe);
            Vector3 to = paso - hero.Local;
            to.y = 0f;
            bool cerca = to.sqrMagnitude <= 1.15f * 1.15f;
            bool moving = false;
            Vector3 cara = to.sqrMagnitude > 0.0001f ? to.normalized : lado;
            if (!espera && !cerca && to.sqrMagnitude > 0.0001f)
            {
                moving = Slide(hero, to.normalized, 4.6f);
                if (!moving && lado.sqrMagnitude < 0.01f)
                    moving = EmpujarLado(hero, to, 3.8f);
                if (!moving)
                {
                    atascoHeroe++;
                    if (atascoHeroe > 36 && indiceHeroe < rutaHeroe.Count - 1)
                    {
                        indiceHeroe++;
                        atascoHeroe = 0;
                    }
                }
                else
                    atascoHeroe = 0;
            }

            if (lado.sqrMagnitude > 0.01f && Slide(hero, lado, 5.6f))
            {
                moving = true;
                cara = lado;
            }

            Ground(hero);
            hero.Anim?.PlayStep(CamaraTercera.WorldDir(district, cara), moving);
            hero.Body.localPosition = hero.Local;
            return true;
        }

        void CaminarZeta(Walker zeta)
        {
            Vector3 lado = DeseoLateral();
            Vector3 paso = SiguienteRuta(rutaZeta, ref indiceZeta, zeta.Local, metaZeta);
            Vector3 to = paso - zeta.Local;
            to.y = 0f;
            bool moving = false;
            Vector3 cara = to.sqrMagnitude > 0.0001f ? to.normalized : lado;
            if (to.sqrMagnitude >= 0.35f)
            {
                moving = Slide(zeta, to.normalized, 3.4f);
                if (!moving && lado.sqrMagnitude < 0.01f)
                    moving = EmpujarLado(zeta, to, 2.8f);
                if (!moving)
                {
                    atascoZeta++;
                    if (atascoZeta > 36 && indiceZeta < rutaZeta.Count - 1)
                    {
                        indiceZeta++;
                        atascoZeta = 0;
                    }
                }
                else
                    atascoZeta = 0;
            }

            if (lado.sqrMagnitude > 0.01f && Slide(zeta, lado, 5.6f))
            {
                moving = true;
                cara = lado;
            }

            zeta.Anim?.PlayStep(CamaraTercera.WorldDir(district, cara), moving);
            if (zeta.Body != null) zeta.Body.localPosition = zeta.Local;
        }

        // A y D, respecto a la cámara. Los dos se corren juntos para no clavarse en el muro.
        Vector3 DeseoLateral()
        {
            float h = Input.GetAxisRaw("Horizontal");
            if (Mathf.Abs(h) < 0.2f) return Vector3.zero;
            float yaw = vista == Vista.Planta ? CamaraTercera.PlanYaw : camara.Yaw;
            Vector3 world = Quaternion.Euler(0f, yaw, 0f) * new Vector3(Mathf.Sign(h), 0f, 0f);
            return camara.ToLocal(district, world);
        }

        bool EmpujarLado(Walker walker, Vector3 to, float speed)
        {
            Vector3 lado = new Vector3(-to.z, 0f, to.x);
            if (lado.sqrMagnitude < 0.0001f) return false;
            lado.Normalize();
            if (Slide(walker, lado, speed)) return true;
            return Slide(walker, -lado, speed);
        }

        Vector3 SiguienteRuta(List<Vector3> ruta, ref int indice, Vector3 desde, Vector3 fin)
        {
            if (ruta == null || ruta.Count == 0) return fin;
            while (indice < ruta.Count && Plano(desde, ruta[indice]) < 2.3f)
                indice++;
            if (indice >= ruta.Count) return fin;
            return ruta[indice];
        }

        // Calle de verdad: fuera del Batán, el plano. Adentro, solo el centro de la calle.
        bool PisoGuia(float x, float z)
        {
            if (!OnStreet(x, z)) return false;
            if (!CalleCerrada(x, z)) return true;
            MedirBatan(x, z, out float along, out float side);
            return along > 11.5f && along < 44f && Mathf.Abs(side) < 3.4f;
        }

        bool TramoLibre(Vector3 a, Vector3 b)
        {
            float dist = Plano(a, b);
            if (dist < 0.25f) return true;
            int n = Mathf.Max(1, Mathf.CeilToInt(dist / 1.5f));
            for (int i = 1; i <= n; i++)
            {
                float t = i / (float)n;
                float x = Mathf.Lerp(a.x, b.x, t);
                float z = Mathf.Lerp(a.z, b.z, t);
                if (!PisoGuia(x, z)) return false;
            }

            var space = RuntimeParent;
            if (space == null) return true;
            return SolidoDelCamino.Puede(space.TransformPoint(a), space.TransformPoint(b), 0.42f, 1.65f);
        }

        List<Vector3> BuscarCeldas(Vector3 desde, Vector3 hasta)
        {
            var camino = new List<Vector3>();
            const float celda = 4f;
            int Ix(float v) => Mathf.FloorToInt(v / celda);
            int ax = Ix(desde.x);
            int az = Ix(desde.z);
            int bx = Ix(hasta.x);
            int bz = Ix(hasta.z);
            if (!CeldaAndable(bx, bz, out _))
            {
                if (!CercaAndable(hasta, out bx, out bz)) return camino;
            }

            int minX = Mathf.Min(ax, bx) - 18;
            int maxX = Mathf.Max(ax, bx) + 18;
            int minZ = Mathf.Min(az, bz) - 18;
            int maxZ = Mathf.Max(az, bz) + 18;
            var g = new Dictionary<long, float>();
            var prev = new Dictionary<long, long>();
            var open = new List<long>();
            long start = WalkKey(ax, az);
            long goal = WalkKey(bx, bz);
            g[start] = 0f;
            open.Add(start);
            int[] ox = { 1, -1, 0, 0, 1, 1, -1, -1 };
            int[] oz = { 0, 0, 1, -1, 1, -1, 1, -1 };
            int guard = 0;
            bool hallo = start == goal;
            while (open.Count > 0 && guard < 3500 && !hallo)
            {
                guard++;
                int best = 0;
                float bestF = float.MaxValue;
                for (int i = 0; i < open.Count; i++)
                {
                    long k = open[i];
                    int cx = (int)(k >> 32);
                    int cz = (int)k;
                    float score = g[k] + Mathf.Abs(cx - bx) + Mathf.Abs(cz - bz);
                    if (score < bestF)
                    {
                        bestF = score;
                        best = i;
                    }
                }

                long cur = open[best];
                open.RemoveAt(best);
                if (cur == goal)
                {
                    hallo = true;
                    break;
                }

                int x = (int)(cur >> 32);
                int z = (int)cur;
                Vector3 aqui = cur == start ? desde : default;
                if (cur != start && !CeldaAndable(x, z, out aqui)) continue;
                for (int n = 0; n < 8; n++)
                {
                    int nx = x + ox[n];
                    int nz = z + oz[n];
                    if (nx < minX || nx > maxX || nz < minZ || nz > maxZ) continue;
                    if (!CeldaAndable(nx, nz, out Vector3 alli)) continue;
                    if (!TramoLibre(aqui, alli)) continue;
                    long nk = WalkKey(nx, nz);
                    float step = n < 4 ? 1f : 1.42f;
                    float ng = g[cur] + step;
                    if (g.TryGetValue(nk, out float old) && ng >= old) continue;
                    g[nk] = ng;
                    prev[nk] = cur;
                    if (!open.Contains(nk)) open.Add(nk);
                }
            }

            if (start != goal && !prev.ContainsKey(goal)) return camino;
            var pila = new List<long>();
            long walk = goal;
            pila.Add(walk);
            int safe = 0;
            while (walk != start && prev.TryGetValue(walk, out long atras) && safe < 700)
            {
                walk = atras;
                pila.Add(walk);
                safe++;
            }

            pila.Reverse();
            for (int i = 1; i < pila.Count; i++)
            {
                int x = (int)(pila[i] >> 32);
                int z = (int)pila[i];
                if (CeldaAndable(x, z, out Vector3 p))
                    camino.Add(Pisar(p));
            }

            return camino;
        }

        bool CeldaAndable(int ix, int iz, out Vector3 p)
        {
            const float celda = 4f;
            float x = (ix + 0.5f) * celda;
            float z = (iz + 0.5f) * celda;
            if (PisoGuia(x, z))
            {
                p = new Vector3(x, 0f, z);
                return true;
            }

            for (int k = 0; k < 8; k++)
            {
                float a = k * 0.78539816f;
                float sx = x + Mathf.Cos(a) * 1.7f;
                float sz = z + Mathf.Sin(a) * 1.7f;
                if (!PisoGuia(sx, sz)) continue;
                p = new Vector3(sx, 0f, sz);
                return true;
            }

            p = default;
            return false;
        }

        bool CercaAndable(Vector3 hasta, out int ix, out int iz)
        {
            const float celda = 4f;
            int cx = Mathf.FloorToInt(hasta.x / celda);
            int cz = Mathf.FloorToInt(hasta.z / celda);
            for (int r = 0; r <= 6; r++)
            {
                for (int dz = -r; dz <= r; dz++)
                {
                    for (int dx = -r; dx <= r; dx++)
                    {
                        if (Mathf.Max(Mathf.Abs(dx), Mathf.Abs(dz)) != r) continue;
                        if (!CeldaAndable(cx + dx, cz + dz, out _)) continue;
                        ix = cx + dx;
                        iz = cz + dz;
                        return true;
                    }
                }
            }

            ix = cx;
            iz = cz;
            return false;
        }

        List<Vector3> ArcoPlaza(Vector3 desde, Vector3 hasta)
        {
            var vacio = new List<Vector3>();
            if (!plazaFound || plazaHalfU < 8f || plazaHalfV < 8f) return vacio;
            float ru = Mathf.Max(6f, plazaHalfU - PlazaWalk * 0.5f);
            float rv = Mathf.Max(6f, plazaHalfV - PlazaWalk * 0.5f);
            Vector2 a = PlazaLocal(desde.x, desde.z);
            Vector2 b = PlazaLocal(hasta.x, hasta.z);
            float angA = Mathf.Atan2(a.y / rv, a.x / ru);
            float angB = Mathf.Atan2(b.y / rv, b.x / ru);
            float corta = Mathf.DeltaAngle(angA * Mathf.Rad2Deg, angB * Mathf.Rad2Deg) * Mathf.Deg2Rad;
            var uno = MuestrearArco(angA, corta, ru, rv);
            var otro = MuestrearArco(angA, corta > 0f ? corta - Mathf.PI * 2f : corta + Mathf.PI * 2f, ru, rv);
            return otro.Count > uno.Count ? otro : uno;
        }

        List<Vector3> MuestrearArco(float angA, float delta, float ru, float rv)
        {
            var pts = new List<Vector3>();
            float largo = Mathf.Abs(delta) * Mathf.Max(ru, rv);
            int steps = Mathf.Clamp(Mathf.CeilToInt(largo / 6f), 3, 28);
            Vector3 previo = default;
            bool tiene = false;
            for (int i = 1; i <= steps; i++)
            {
                float ang = angA + delta * (i / (float)steps);
                Vector2 w = PlazaWorld(Mathf.Cos(ang) * ru, Mathf.Sin(ang) * rv);
                if (!PisoGuia(w.x, w.y)) continue;
                var p = new Vector3(w.x, 0f, w.y);
                if (tiene && !TramoLibre(previo, p)) continue;
                pts.Add(Pisar(p));
                previo = p;
                tiene = true;
            }

            return pts;
        }

        void Acortar(Vector3 desde, List<Vector3> pts)
        {
            if (pts.Count <= 1) return;
            var corto = new List<Vector3>();
            Vector3 cursor = desde;
            int i = 0;
            while (i < pts.Count)
            {
                int lejos = i;
                for (int j = pts.Count - 1; j > i; j--)
                {
                    if (!TramoLibre(cursor, pts[j])) continue;
                    lejos = j;
                    break;
                }

                corto.Add(pts[lejos]);
                cursor = pts[lejos];
                i = lejos + 1;
            }

            pts.Clear();
            pts.AddRange(corto);
        }

        static Vector3 Pisar(Vector3 p)
        {
            p.y = SueloDeLaCalle.Altura(p.x, p.z);
            return p;
        }

        Vector3 PuntoAndable(Vector3 p)
        {
            if (PisoGuia(p.x, p.z)) return Pisar(p);
            for (int r = 1; r <= 8; r++)
            {
                for (int k = 0; k < 8; k++)
                {
                    float a = k * 0.78539816f;
                    float x = p.x + Mathf.Cos(a) * r * 1.5f;
                    float z = p.z + Mathf.Sin(a) * r * 1.5f;
                    if (!PisoGuia(x, z)) continue;
                    return Pisar(new Vector3(x, 0f, z));
                }
            }

            return Pisar(p);
        }

        Vector3 CrudoBatan(float along, float side)
        {
            Vector3 lado = new Vector3(-batanEje.z, 0f, batanEje.x);
            if (lado.sqrMagnitude < 0.01f) lado = Vector3.right;
            else lado.Normalize();
            return batanBoca + batanEje * along + lado * side;
        }

        static float Plano(Vector3 a, Vector3 b)
        {
            float dx = a.x - b.x;
            float dz = a.z - b.z;
            return Mathf.Sqrt(dx * dx + dz * dz);
        }

        void SpawnLadrones()
        {
            var sheet = BattleCast.GatoLadron;
            const int n = 4;
            for (int i = 0; i < n; i++)
            {
                Vector3 at = SpreadPoint(i + 2, n + 4);
                if (CalleCerrada(at.x, at.z))
                    at = FarPoint(at);
                at.y = SueloDeLaCalle.Altura(at.x, at.z);
                if (!Colocar("Ladron" + i, ref at)) continue;
                var walker = Spawn("Ladron" + i, sheet, StreetScale(CastScale.Of(sheet)), true, at, false);
                walker.Home = at;
                walker.Goal = FarPoint(at);
                walker.There = at;
                walker.Roam = 20f;
                walker.Speed = 3.35f;
                walker.Roba = true;
                walker.RoboEn = 2f + i;
                walkers.Add(walker);
                walker.Anim?.Desfasar(i);
            }
        }

        // Motivo propio, pentatónico, para la banda. No es una grabación ajena.
        static AudioClip ClipCajamarquina()
        {
            const int rate = 22050;
            const float seconds = 8f;
            int n = Mathf.RoundToInt(rate * seconds);
            var data = new float[n];
            float[] nota =
            {
                440f, 523.25f, 659.25f, 587.33f,
                523.25f, 440f, 392f, 329.63f,
                392f, 440f, 523.25f, 440f,
                659.25f, 587.33f, 523.25f, 440f,
            };
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)rate;
                int b = Mathf.Min(nota.Length - 1, Mathf.FloorToInt(t / 0.5f));
                float local = t / 0.5f - b;
                float f = nota[b];
                float puerta = Mathf.Exp(-local * 3.6f);
                float quena = Mathf.Sin(Mathf.PI * 2f * f * t) * 0.32f * puerta;
                float cuerda = Mathf.Sin(Mathf.PI * 2f * (f * 0.5f) * t) * 0.22f;
                float bombo = 0f;
                if (local < 0.07f)
                    bombo = (1f - local / 0.07f) * Mathf.Sin(Mathf.PI * 2f * 70f * t) * 0.45f;
                data[i] = Mathf.Clamp((quena + cuerda + bombo) * 0.8f, -1f, 1f);
            }

            var clip = AudioClip.Create("bella-cajamarquina", n, 1, rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        // El chanchito da la vuelta a la fuente con el carnaval.
        // Los gatos caminan por las calles de la plaza, cada uno a un punto distinto.
        // El gato del farol alumbra y convida chicha.
        void SpawnComparsa()
        {
            oyoComparsa = false;
            cercaChicha = false;
            atascoComparsa = 0f;
            comparsaSentido = 1;
            liderComparsa = null;
            Vector3 anchor = AnclaDeComparsa();
            if (!Colocar("Comparsa", ref anchor)) return;
            Vector3 radio = new Vector3(anchor.x - fountainX, 0f, anchor.z - fountainZ);
            if (radio.sqrMagnitude < 1f) radio = Vector3.forward;
            Vector3 tangente = new Vector3(-radio.z, 0f, radio.x).normalized;
            PonerEnComparsa(BattleCast.ChanchoViolin, anchor, tangente, 0f, 0f, true, false, 0);
            PonerEnComparsa(BattleCast.GatoChicha, anchor, tangente, 0.7f, 1.25f, false, true, 1);
            PonerEnComparsa(BattleCast.GatoGuitarra, anchor, tangente, -0.55f, 2.45f, false, false, 2);
            PonerEnComparsa(BattleCast.GatoCaja, anchor, tangente, 0.55f, 2.45f, false, false, 3);
            PonerEnComparsa(BattleCast.GataPollera, anchor, tangente, -0.55f, 3.65f, false, false, 4);
            PonerEnComparsa(BattleCast.GatoBombo, anchor, tangente, 0.55f, 3.65f, false, false, 5);
        }

        void PonerEnComparsa(CharacterSheet sheet, Vector3 anchor, Vector3 tangente, float lado, float atras, bool lider, bool alumbra, int orden)
        {
            Vector3 ladoVec = new Vector3(-tangente.z, 0f, tangente.x);
            Vector3 columna = anchor - tangente * atras;
            Vector3 at = columna + ladoVec * lado;
            if (walkReady && !PisoLibre(at.x, at.z))
            {
                Vector3 respaldo = PisoLibre(columna.x, columna.z) ? columna : anchor;
                at = AcercarACalle(at, respaldo);
            }

            at.y = SueloDeLaCalle.Altura(at.x, at.z);
            var walker = Spawn("Calle" + sheet.Id, sheet, StreetScale(CastScale.Of(sheet)), true, at, false);
            walker.Home = at;
            walker.There = at;
            walker.Goal = at;
            walker.Roam = 0f;
            walker.Canta = true;
            walker.Comparsa = true;
            walker.Lider = lider;
            walker.Alumbra = alumbra;
            walker.Lado = lado;
            walker.Atras = atras;
            walker.Speed = lider ? 2.15f : Random.Range(1.9f, 2.4f);
            if (!lider) walker.Goal = PuntoDePlaza(at);
            walkers.Add(walker);
            walker.Anim?.Desfasar(orden * 2 + 1);
            var ink = walker.Body != null ? walker.Body.GetComponent<SpriteRenderer>() : null;
            if (ink != null) ink.sortingOrder = lider ? 22 : alumbra ? 21 : 20;
            if (lider)
            {
                liderComparsa = walker;
                walker.Goal = PasoDeAnillo(at);
                PonerCarnaval(walker.Body.gameObject);
            }

            if (alumbra) PonerFarolDeMano(walker);
        }

        void PonerFarolDeMano(Walker walker)
        {
            if (walker.Body == null) return;
            float escala = Mathf.Max(0.2f, walker.Body.localScale.y);
            var go = new GameObject("LuzChicha");
            go.transform.SetParent(walker.Body, false);
            go.transform.localPosition = new Vector3(0.45f, 1.35f / escala, 0f);
            var light = go.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(1f, 0.74f, 0.32f);
            light.intensity = 3.2f;
            light.range = 11f;
            light.shadows = LightShadows.None;
        }

        void CaminarComparsa(Walker walker)
        {
            if (walker.Lider || liderComparsa == null || liderComparsa.Body == null)
                ElegirMetaLider(walker);
            else
                ElegirMetaGato(walker);

            Vector3 to = walker.Goal - walker.Local;
            to.y = 0f;
            if (to.sqrMagnitude < 0.35f)
            {
                walker.Anim?.PlayStep(Vector3.zero, false);
                if (!walker.Lider) walker.Goal = PuntoDePlaza(walker.Local);
                return;
            }

            bool moving = Slide(walker, to.normalized, walker.Speed);
            walker.Anim?.PlayStep(CamaraTercera.WorldDir(district, to.normalized), moving);
            if (walker.Body != null) walker.Body.localPosition = walker.Local;
            if (walker.Lider)
            {
                if (moving)
                {
                    atascoComparsa = 0f;
                    return;
                }

                atascoComparsa += Time.deltaTime;
                if (atascoComparsa < 0.45f) return;
                atascoComparsa = 0f;
                comparsaSentido = -comparsaSentido;
                walker.Goal = PasoDeAnillo(walker.Local);
                return;
            }

            if (moving)
            {
                walker.Wait = 0f;
                return;
            }

            walker.Wait += Time.deltaTime;
            if (walker.Wait < 0.5f) return;
            walker.Wait = 0f;
            walker.Goal = PuntoDePlaza(walker.Local);
        }

        void ElegirMetaGato(Walker walker)
        {
            Vector3 to = walker.Goal - walker.Local;
            to.y = 0f;
            if (to.sqrMagnitude > 1.2f) return;
            walker.Goal = PuntoDePlaza(walker.Local);
        }

        // Calle de la plaza: la cruz, el paseo de piedra o el anillo. No sale a la ciudad.
        Vector3 PuntoDePlaza(Vector3 desde)
        {
            bool ejes = plazaFound && plazaHalfU > 12f && plazaHalfV > 12f;
            for (int i = 0; i < 28; i++)
            {
                float x;
                float z;
                if (ejes)
                {
                    int banda = Random.Range(0, 5);
                    float u;
                    float v;
                    if (banda == 0)
                    {
                        u = Random.Range(-RutaMedia + 0.45f, RutaMedia - 0.45f);
                        v = Random.Range(-plazaHalfV + 2f, plazaHalfV - 2f);
                    }
                    else if (banda == 1)
                    {
                        u = Random.Range(-plazaHalfU + 2f, plazaHalfU - 2f);
                        v = Random.Range(-RutaMedia + 0.45f, RutaMedia - 0.45f);
                    }
                    else if (banda == 2)
                    {
                        float borde = Random.Range(1.1f, PlazaWalk - 0.8f);
                        if (Random.value < 0.5f)
                        {
                            u = Random.Range(-plazaHalfU + 1.4f, plazaHalfU - 1.4f);
                            v = (Random.value < 0.5f ? 1f : -1f) * (plazaHalfV - borde);
                        }
                        else
                        {
                            v = Random.Range(-plazaHalfV + 1.4f, plazaHalfV - 1.4f);
                            u = (Random.value < 0.5f ? 1f : -1f) * (plazaHalfU - borde);
                        }
                    }
                    else
                    {
                        float fuera = Random.Range(1.4f, 5.2f);
                        if (Random.value < 0.5f)
                        {
                            u = Random.Range(-plazaHalfU - 3f, plazaHalfU + 3f);
                            v = (Random.value < 0.5f ? 1f : -1f) * (plazaHalfV + fuera);
                        }
                        else
                        {
                            v = Random.Range(-plazaHalfV - 3f, plazaHalfV + 3f);
                            u = (Random.value < 0.5f ? 1f : -1f) * (plazaHalfU + fuera);
                        }
                    }

                    if (u * u + v * v < (CorteFuente + 1.1f) * (CorteFuente + 1.1f)) continue;
                    Vector2 mundo = PlazaWorld(u, v);
                    x = mundo.x;
                    z = mundo.y;
                }
                else
                {
                    float ang = Random.value * Mathf.PI * 2f;
                    float rr = Random.Range(16f, 42f);
                    x = fountainX + Mathf.Cos(ang) * rr;
                    z = fountainZ + Mathf.Sin(ang) * rr;
                }

                if (!PisoLibre(x, z)) continue;
                float dx = x - desde.x;
                float dz = z - desde.z;
                float paso = dx * dx + dz * dz;
                float minimo = i < 18 ? 7f : 3.4f;
                if (paso < minimo * minimo || paso > 36f * 36f) continue;
                return new Vector3(x, desde.y, z);
            }

            Vector3 anillo = PasoDeAnillo(desde);
            anillo.y = desde.y;
            return anillo;
        }

        void ElegirMetaLider(Walker walker)
        {
            Vector3 to = walker.Goal - walker.Local;
            to.y = 0f;
            if (to.sqrMagnitude > 1.3f) return;
            Vector3 paso = PasoDeAnillo(walker.Local);
            if ((paso - walker.Local).sqrMagnitude < 2.5f)
            {
                comparsaSentido = -comparsaSentido;
                paso = PasoDeAnillo(walker.Local);
            }

            paso.y = walker.Local.y;
            walker.Goal = paso;
        }

        Vector3 PasoDeAnillo(Vector3 desde)
        {
            Vector3 radio = new Vector3(desde.x - fountainX, 0f, desde.z - fountainZ);
            float r = radio.magnitude;
            if (r < 10f) r = 24f;
            float ang = Mathf.Atan2(radio.z, radio.x);
            Vector3 mejor = desde;
            float mejorCosto = 999f;
            bool hallo = false;
            for (float da = 0.18f; da <= 0.62f; da += 0.08f)
            {
                for (float dr = -5f; dr <= 5f; dr += 1.25f)
                {
                    float a = ang + comparsaSentido * da;
                    float rr = Mathf.Clamp(r + dr, 14f, 48f);
                    float x = fountainX + Mathf.Cos(a) * rr;
                    float z = fountainZ + Mathf.Sin(a) * rr;
                    if (!PisoLibre(x, z)) continue;
                    float dx = x - desde.x;
                    float dz = z - desde.z;
                    float paso = Mathf.Sqrt(dx * dx + dz * dz);
                    if (paso < 5f || paso > 11f) continue;
                    float costo = Mathf.Abs(rr - r) * 1.4f + Mathf.Abs(paso - 7.2f);
                    if (costo >= mejorCosto) continue;
                    mejorCosto = costo;
                    mejor = new Vector3(x, desde.y, z);
                    hallo = true;
                }
            }

            return hallo ? mejor : desde;
        }

        void MetaDeFila(Walker walker)
        {
            Walker lider = liderComparsa;
            Vector3 rumbo = lider.Goal - lider.Local;
            rumbo.y = 0f;
            if (rumbo.sqrMagnitude < 0.25f)
            {
                rumbo = new Vector3(-(lider.Local.z - fountainZ), 0f, lider.Local.x - fountainX);
                rumbo *= comparsaSentido;
            }

            if (rumbo.sqrMagnitude < 0.01f) rumbo = Vector3.forward;
            rumbo.Normalize();
            Vector3 lado = new Vector3(-rumbo.z, 0f, rumbo.x);
            Vector3 desea = lider.Local - rumbo * walker.Atras + lado * walker.Lado;
            desea.y = walker.Local.y;
            walker.Goal = AcercarACalle(desea, lider.Local);
        }

        Vector3 AcercarACalle(Vector3 desea, Vector3 lider)
        {
            if (PisoLibre(desea.x, desea.z)) return desea;
            for (int i = 1; i <= 8; i++)
            {
                float t = i / 8f;
                Vector3 p = Vector3.Lerp(desea, lider, t);
                if (PisoLibre(p.x, p.z))
                {
                    p.y = desea.y;
                    return p;
                }
            }

            return lider;
        }

        Vector3 AnclaDeComparsa()
        {
            var lejos = new Vector3(0f, 0f, -20f);
            var mejor = new Vector3(fountainX, 0.2f, fountainZ + 22f);
            float mejorPuntaje = -1f;
            bool alguno = false;
            for (int k = 0; k < 24; k++)
            {
                float ang = k * 15f * Mathf.Deg2Rad;
                for (float rad = 16f; rad <= 40f; rad += 3f)
                {
                    float x = fountainX + Mathf.Cos(ang) * rad;
                    float z = fountainZ + Mathf.Sin(ang) * rad;
                    if (walkReady && !OnStreet(x, z)) continue;
                    float puntaje = (new Vector3(x, 0f, z) - lejos).sqrMagnitude + z * 30f;
                    if (puntaje <= mejorPuntaje) continue;
                    mejorPuntaje = puntaje;
                    mejor = new Vector3(x, 0.2f, z);
                    alguno = true;
                }
            }

            if (!alguno && walkSpan) mejor = SpreadPoint(2, 8);
            return mejor;
        }

        void OirComparsa(Walker hero)
        {
            bool cerca = false;
            for (int i = 0; i < walkers.Count; i++)
            {
                var walker = walkers[i];
                if (!walker.Canta || walker.Toca || walker.Body == null) continue;
                if ((walker.Local - hero.Local).sqrMagnitude < 16f * 16f)
                {
                    cerca = true;
                    break;
                }
            }

            if (cerca && !oyoComparsa)
                Note("Cantan el carnaval de Oxamarca.");
            oyoComparsa = cerca;
        }

        void OirChicha(Walker hero)
        {
            Walker luz = null;
            for (int i = 0; i < walkers.Count; i++)
            {
                if (!walkers[i].Alumbra || walkers[i].Body == null) continue;
                luz = walkers[i];
                break;
            }

            if (luz == null) return;
            Vector3 gap = luz.Local - hero.Local;
            gap.y = 0f;
            float lejos = gap.sqrMagnitude;
            if (lejos > 8.2f * 8.2f)
            {
                if (lejos > 14f * 14f) cercaChicha = false;
                return;
            }

            if (cercaChicha) return;
            cercaChicha = true;
            Note("El gato del farol invita un vaso de chicha.");
        }

        void PonerCarnaval(GameObject cuerpo)
        {
            PonerTema(cuerpo, "Battle/Audio/carnaval-oxamarca", ClipDeCarnaval(), 0.72f, 4f, 26f);
        }

        // Frase propia, en pentatónica, con pulso de bombo y caja.
        // No es una copla grabada.
        static AudioClip ClipDeCarnaval()
        {
            const int rate = 22050;
            const float seconds = 6.4f;
            int n = Mathf.RoundToInt(rate * seconds);
            var data = new float[n];
            float[] note =
            {
                392.00f, 329.63f, 293.66f, 329.63f,
                349.23f, 293.66f, 261.63f, 293.66f,
            };
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)rate;
                int b = Mathf.Min(7, Mathf.FloorToInt(t / 0.8f));
                float local = t / 0.8f - b;
                float boca = Mathf.Sin(Mathf.Clamp01(local) * Mathf.PI);
                boca *= boca;
                float f = note[b];
                float sing = Mathf.Sin(Mathf.PI * 2f * f * t) * 0.34f
                    + Mathf.Sin(Mathf.PI * 2f * f * 2f * t) * 0.11f;
                float cuerda = Mathf.Exp(-local * 8f) * Mathf.Sin(Mathf.PI * 2f * f * 0.5f * t);
                float drum = 0f;
                if ((b % 2) == 0 && local < 0.09f)
                    drum = (1f - local / 0.09f) * Mathf.Sin(Mathf.PI * 2f * 78f * t);
                if (local > 0.48f && local < 0.58f)
                {
                    float u = (local - 0.48f) / 0.1f;
                    int hash = (i * 1103515245 + 12345) & 0x7fffffff;
                    float ruido = hash / 2147483647f * 2f - 1f;
                    drum += (1f - u) * 0.28f * ruido;
                }

                data[i] = Mathf.Clamp((sing * boca + cuerda * 0.26f + drum * 0.62f) * 0.7f, -1f, 1f);
            }

            var clip = AudioClip.Create("carnaval", n, 1, rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        // El Batán sale de la plaza hacia Amazonas. Se cierra un tramo
        // para el concierto. La anilla y las cuatro bocas de la plaza siguen.
        void PrepararBatan()
        {
            batanBoca = new Vector3(2.1f, 0f, 90.2f);
            Vector3 eje = new Vector3(38f, 0f, 133.4f) - batanBoca;
            eje.y = 0f;
            batanCerrado = eje.sqrMagnitude > 4f;
            batanEje = batanCerrado ? eje.normalized : Vector3.forward;
        }

        // Tres cuyes. Guitarra, batería y bajo. No caminan.
        // Delante, los fanes poguean en círculo.
        void SpawnConcierto()
        {
            oyoConcierto = false;
            if (!batanCerrado) PrepararBatan();
            if (!batanCerrado) return;

            Vector3 lado = new Vector3(-batanEje.z, 0f, batanEje.x);
            Transform root = district != null ? district.Find("ConciertoBatan") : null;
            if (root == null && RuntimeParent != null) root = RuntimeParent.Find("ConciertoBatan");
            if (root == null)
            {
                var go = new GameObject("ConciertoBatan");
                go.transform.SetParent(district != null ? district : RuntimeParent, false);
                root = go.transform;
                Vector3 sur = batanBoca + batanEje * 14f;
                Vector3 norte = batanBoca + batanEje * 48f;
                PonerValla(root, sur, lado, -batanEje, true);
                PonerValla(root, norte, lado, batanEje, false);
                PonerEscenario(root, batanBoca + batanEje * 36f, lado, -batanEje);
                PonerFlyer(root, lado);
                SembrarConcierto(root);
            }

            // Fila en el estrado, de cara al público. El de la batería va un paso atrás.
            var sheets = BattleCast.Concierto;
            Vector3 centroBanda = batanBoca + batanEje * 36f;
            Vector3 miraBanda = -batanEje;
            if (miraBanda.sqrMagnitude < 0.01f) miraBanda = Vector3.back;
            miraBanda.Normalize();
            if (lado.sqrMagnitude < 0.01f) lado = Vector3.right;
            lado.Normalize();
            float[] fila = { -3.15f, 0f, 3.15f };
            float[] fondo = { 2.15f, 1.55f, 2.15f };
            for (int i = 0; i < sheets.Length && i < fila.Length; i++)
            {
                Vector3 at = centroBanda + lado * fila[i] + miraBanda * fondo[i];
                at.y = SueloDeLaCalle.Altura(at.x, at.z) + 0.10f;
                if (!Colocar("Musico" + i, ref at)) continue;
                var sheet = sheets[i];
                float talla = StreetScale(CastScale.Of(sheet)) * 1.45f;
                var walker = Spawn("Calle" + sheet.Id, sheet, talla, true, at, false);
                // La tarima mide 0.10 m: el alto del tablado, no un lift de suelo.
                walker.PisoExtra = 0.10f;
                walker.Home = at;
                walker.Goal = at;
                walker.There = at;
                walker.Roam = 0f;
                walker.Speed = 0f;
                walker.Toca = true;
                if (walker.Body != null)
                {
                    var tinta = walker.Body.GetComponent<SpriteRenderer>();
                    if (tinta != null) tinta.sortingOrder = 46;
                }

                walkers.Add(walker);
                if (i == 1)
                {
                    walker.Anim?.Compas(8f);
                    PonerRock(walker.Body.gameObject);
                }
            }

            SpawnFanes();
        }

        // Borde del concierto. Flores de los dos assets, y entre ellas
        // arbustos, hongos, pasto y piedras pequeñas. El centro queda libre.
        void SembrarConcierto(Transform parent)
        {
            if (!batanCerrado || parent == null) return;
            Vector3 lado = new Vector3(-batanEje.z, 0f, batanEje.x);
            if (lado.sqrMagnitude < 0.01f) lado = Vector3.right;
            lado.Normalize();
            string[] floresBosque =
            {
                "Bosque/flower_redA",
                "Bosque/flower_yellowA",
                "Bosque/flower_purpleA",
                "Bosque/flower_redC",
            };
            string[] cosas =
            {
                "Bosque/plant_bushSmall",
                "Bosque/mushroom_red",
                "Bosque/grass_leafs",
                "Bosque/rock_smallA",
                "Bosque/plant_flatTall",
                "Bosque/mushroom_tanGroup",
                "Bosque/rock_smallF",
                "Bosque/grass",
            };
            int n = 0;
            int puestos = 0;
            for (float along = 16.2f; along <= 46.4f && puestos < 72; along += 1.7f)
            {
                for (float ladoM = 2.6f; ladoM <= 6.6f && puestos < 72; ladoM += 1.45f)
                {
                    for (int s = -1; s <= 1 && puestos < 72; s += 2)
                    {
                        float side = s * (ladoM + (n % 3) * 0.12f);
                        if (along > 31.4f && along < 41.2f && Mathf.Abs(side) < 4.8f)
                        {
                            n++;
                            continue;
                        }

                        if (PegaFan(along, side))
                        {
                            n++;
                            continue;
                        }

                        Vector3 p = batanBoca + batanEje * (along + (((n * 3) % 5) - 2) * 0.1f) + lado * side;
                        if (walkReady && !OnStreet(p.x, p.z))
                        {
                            n++;
                            continue;
                        }

                        float yaw = n * 41f;
                        if (n % 5 == 4)
                        {
                            string cosa = cosas[n % cosas.Length];
                            string etiqueta = "Adorno";
                            float alto = 0.85f;
                            if (cosa.IndexOf("mushroom", System.StringComparison.Ordinal) >= 0)
                            {
                                etiqueta = "mushroom";
                                alto = 0.36f;
                            }
                            else if (cosa.IndexOf("grass", System.StringComparison.Ordinal) >= 0)
                            {
                                etiqueta = "grass";
                                alto = 0.58f;
                            }
                            else if (cosa.IndexOf("rock", System.StringComparison.Ordinal) >= 0)
                                alto = 0.38f;
                            VestirPieza(cosa, p.x, p.z, alto, yaw, parent, etiqueta);
                        }
                        else if (n % 2 == 0)
                            VestirPieza(floresBosque[n % floresBosque.Length], p.x, p.z, 0.46f, yaw, parent, "flower");
                        else
                            PlantarFlor(FloresDelAsset[n % FloresDelAsset.Length], p.x, p.z, yaw, parent, false);
                        puestos++;
                        n++;
                    }
                }
            }
        }

        static bool PegaFan(float along, float side)
        {
            if (along < 19.5f || along > 26.2f) return false;
            float[] laterales = { -5.1f, -1.7f, 1.7f, 5.1f };
            for (int i = 0; i < laterales.Length; i++)
                if (Mathf.Abs(side - laterales[i]) < 1.15f) return true;
            return false;
        }

        void VestirPieza(string resource, float x, float z, float alto, float yaw, Transform parent, string etiqueta)
        {
            int antes = parent.childCount;
            PlantTree(resource, x, z, alto, yaw, CajamarcaAltura.Sample(x, z), parent, etiqueta);
            if (parent.childCount == antes) return;
            var puesto = parent.GetChild(parent.childCount - 1);
            foreach (var pieza in puesto.GetComponentsInChildren<Transform>(true))
                pieza.name = etiqueta;
        }

        // Ocho fanes delante del escenario. Con la canción arman el mosh.
        void SpawnFanes()
        {
            var sheets = BattleCast.Fanes;
            if (sheets == null || sheets.Length == 0) return;

            Vector3 lado = new Vector3(-batanEje.z, 0f, batanEje.x);
            if (lado.sqrMagnitude < 0.01f) lado = Vector3.right;
            lado.Normalize();
            const int n = 8;
            for (int i = 0; i < n; i++)
            {
                int fila = i / 4;
                int puesto = i % 4;
                float lateral = (puesto - 1.5f) * 3.4f;
                float fondo = 21.2f + fila * 3.25f;
                Vector3 at = batanBoca + batanEje * fondo + lado * lateral;
                if (walkReady && !OnStreet(at.x, at.z))
                {
                    bool hallado = false;
                    float alcance = Mathf.Abs(lateral);
                    for (float cerca = 0.7f; cerca <= alcance && !hallado; cerca += 0.7f)
                    {
                        float queda = Mathf.MoveTowards(lateral, 0f, cerca);
                        Vector3 prueba = batanBoca + batanEje * fondo + lado * queda;
                        if (!OnStreet(prueba.x, prueba.z)) continue;
                        at = prueba;
                        hallado = true;
                    }

                    if (!hallado) continue;
                }

                at.y = SueloDeLaCalle.Altura(at.x, at.z);
                if (!Colocar("Fan" + i, ref at)) continue;
                var sheet = sheets[i % sheets.Length];
                var walker = Spawn("Calle" + sheet.Id, sheet, StreetScale(CastScale.Of(sheet)), true, at, false);
                walker.Home = at;
                walker.Goal = at;
                walker.There = at;
                walker.Roam = 0f;
                walker.Speed = 0f;
                walker.Pogo = true;
                walker.Puesto = i;
                walker.Fase = i * 0.91f + fila * 0.37f;
                walker.Talla = walker.Body != null ? walker.Body.localScale : Vector3.one;
                walker.Anim?.SetFacing(puesto % 2 == 0);
                walker.Anim?.Desfasar(i * 2 + 1);
                walkers.Add(walker);
            }
        }

        Vector3 BuscarCalle(Vector3 at, Vector3 eje)
        {
            Vector3 lado = new Vector3(-eje.z, 0f, eje.x);
            for (float n = 0f; n <= 8f; n += 1.1f)
            {
                for (int s = -1; s <= 1; s++)
                {
                    Vector3 p = at + eje * n + lado * (s * n * 0.65f);
                    if (OnStreet(p.x, p.z)) return p;
                    if (n <= 0f) continue;
                    p = at - eje * n + lado * (s * n * 0.65f);
                    if (OnStreet(p.x, p.z)) return p;
                }
            }

            return at;
        }

        void PonerValla(Transform parent, Vector3 centro, Vector3 lado, Vector3 mira, bool cartel)
        {
            float g = CajamarcaAltura.Sample(centro.x, centro.z);
            var giro = Quaternion.LookRotation(lado, Vector3.up);
            var tabla = BoxPart(parent, "VallaConcierto", new Vector3(centro.x, g + 0.78f, centro.z),
                new Vector3(0.34f, 1.15f, 13.2f), null, new Color(0.93f, 0.74f, 0.16f));
            tabla.transform.rotation = giro;
            var franja = BoxPart(parent, "VallaConcierto", new Vector3(centro.x, g + 0.78f, centro.z) + mira * 0.2f,
                new Vector3(0.08f, 0.28f, 12.2f), null, new Color(0.1f, 0.1f, 0.1f));
            franja.transform.rotation = giro;
            for (int i = -2; i <= 2; i++)
            {
                Vector3 p = centro + lado * (i * 3.1f);
                float pg = CajamarcaAltura.Sample(p.x, p.z);
                BoxPart(parent, "VallaConcierto", new Vector3(p.x, pg + 0.85f, p.z),
                    new Vector3(0.22f, 1.7f, 0.22f), null, new Color(0.36f, 0.22f, 0.1f));
            }

            var aviso = new GameObject("AvisoCierre");
            aviso.transform.SetParent(parent, false);
            aviso.transform.localPosition = new Vector3(centro.x, 0f, centro.z) + mira * 0.45f;
            aviso.transform.rotation = Quaternion.LookRotation(mira, Vector3.up);
            TextoCartel(aviso.transform, "CALLE CERRADA", new Vector3(0f, g + 0.82f, 0f), 0f, 0.36f);

            if (!cartel) return;
            PonerLetrero(parent, centro + mira * 0.9f, mira);
        }

        void PonerLetrero(Transform parent, Vector3 at, Vector3 mira)
        {
            float g = CajamarcaAltura.Sample(at.x, at.z);
            var root = new GameObject("CartelConcierto");
            root.transform.SetParent(parent, false);
            root.transform.localPosition = new Vector3(at.x, 0f, at.z);
            root.transform.rotation = Quaternion.LookRotation(mira, Vector3.up);
            BoxPart(root.transform, "PosteConcierto", new Vector3(0f, g + 1.45f, 0f),
                new Vector3(0.16f, 2.9f, 0.16f), null, new Color(0.36f, 0.22f, 0.1f));
            BoxPart(root.transform, "TablaConcierto", new Vector3(0f, g + 2.55f, 0f),
                new Vector3(3.6f, 1.4f, 0.08f), null, new Color(0.95f, 0.91f, 0.78f));
            TextoCartel(root.transform, "ETERNO\nMi Lugar", new Vector3(0f, g + 2.55f, 0.06f), 0f, 0.92f);
            TextoCartel(root.transform, "ETERNO\nMi Lugar", new Vector3(0f, g + 2.55f, -0.06f), 180f, 0.92f);
        }

        // Cartel grande, al lado de la boca, de cara a quien llega.
        void PonerFlyer(Transform parent, Vector3 lado)
        {
            if (!batanCerrado || parent == null) return;
            if (lado.sqrMagnitude < 0.01f) lado = Vector3.right;
            lado.Normalize();
            Vector3 cara = -lado;
            Vector3 at = batanBoca + batanEje * 9.2f + lado * 8.6f;
            float g = CajamarcaAltura.Sample(at.x, at.z);
            var root = new GameObject("FlyerConcierto");
            root.transform.SetParent(parent, false);
            root.transform.localPosition = new Vector3(at.x, 0f, at.z);
            root.transform.rotation = Quaternion.LookRotation(cara, Vector3.up);

            var papel = new Color(0.96f, 0.93f, 0.84f);
            var rojo = new Color(0.72f, 0.14f, 0.16f);
            BoxPart(root.transform, "Adorno", new Vector3(0f, g + 2.45f, 0f),
                new Vector3(6.4f, 4.7f, 0.08f), null, papel);
            BoxPart(root.transform, "Adorno", new Vector3(0f, g + 4.15f, 0.02f),
                new Vector3(6.4f, 0.95f, 0.04f), null, rojo);
            TextoCartel(root.transform, "GRAN CONCIERTO", new Vector3(0f, g + 4.15f, 0.08f), 0f, 0.62f,
                new Color(0.98f, 0.95f, 0.88f));
            TextoCartel(root.transform,
                "Carmencitha Larah\nCorazones Niños\nEterno\nEstrella de Noche\nPumitz\nCazadors",
                new Vector3(0f, g + 1.85f, 0.08f), 0f, 2.7f);
        }

        void TextoCartel(Transform parent, string title, Vector3 local, float yaw, float alto)
        {
            TextoCartel(parent, title, local, yaw, alto, new Color(0.16f, 0.09f, 0.05f));
        }

        void TextoCartel(Transform parent, string title, Vector3 local, float yaw, float alto, Color tinta)
        {
            var go = new GameObject("Letra");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = local;
            go.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
            var text = go.AddComponent<TextMesh>();
            text.text = title;
            text.font = Storybook.DisplayFont;
            text.fontSize = 48;
            text.characterSize = 0.055f;
            text.anchor = TextAnchor.MiddleCenter;
            text.alignment = TextAlignment.Center;
            text.fontStyle = FontStyle.Bold;
            text.richText = false;
            text.color = tinta;
            var fit = go.AddComponent<StreetLabelFit>();
            fit.Target = alto;
            var mesh = go.GetComponent<MeshRenderer>();
            if (mesh == null) return;
            if (text.font != null) mesh.sharedMaterial = text.font.material;
            mesh.shadowCastingMode = ShadowCastingMode.Off;
            mesh.receiveShadows = false;
        }

        void PonerEscenario(Transform parent, Vector3 centro, Vector3 lado, Vector3 mira)
        {
            float g = CajamarcaAltura.Sample(centro.x, centro.z);
            var giro = Quaternion.LookRotation(lado, Vector3.up);
            var tarima = BoxPart(parent, "Tarima", new Vector3(centro.x, g + 0.05f, centro.z),
                new Vector3(8.2f, 0.1f, 8.6f), "Calle/piedra", new Color(0.24f, 0.16f, 0.12f));
            tarima.transform.rotation = giro;

            Vector3 fondo = centro - mira * 4.2f;
            float gf = CajamarcaAltura.Sample(fondo.x, fondo.z);
            var telon = BoxPart(parent, "TelonConcierto", new Vector3(fondo.x, gf + 2.05f, fondo.z),
                new Vector3(0.18f, 4.1f, 8.4f), null, new Color(0.93f, 0.88f, 0.78f));
            telon.transform.rotation = giro;

            for (int s = -1; s <= 1; s += 2)
            {
                Vector3 p = centro + lado * (s * 4.15f) - mira * 1.2f;
                float pg = CajamarcaAltura.Sample(p.x, p.z);
                var caja = BoxPart(parent, "Parlante", new Vector3(p.x, pg + 0.85f, p.z),
                    new Vector3(0.72f, 1.7f, 0.55f), null, new Color(0.06f, 0.06f, 0.07f));
                caja.transform.rotation = Quaternion.LookRotation(mira, Vector3.up);
                var cono = BoxPart(parent, "Parlante", p + mira * 0.3f + Vector3.up * (pg + 1.15f - p.y),
                    new Vector3(0.28f, 0.28f, 0.08f), null, new Color(0.62f, 0.14f, 0.1f));
                cono.transform.rotation = caja.transform.rotation;
            }

            // El bombo queda al lado. No tapa al baterista.
            Vector3 bombo = centro + lado * 0.95f + mira * 0.15f;
            float gb = CajamarcaAltura.Sample(bombo.x, bombo.z);
            var cuerpo = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            cuerpo.name = "Bombo";
            Kill(cuerpo.GetComponent<Collider>());
            cuerpo.transform.SetParent(parent, false);
            cuerpo.transform.localPosition = new Vector3(bombo.x, gb + 0.34f, bombo.z);
            cuerpo.transform.localRotation = Quaternion.LookRotation(mira, Vector3.up) * Quaternion.Euler(90f, 0f, 0f);
            cuerpo.transform.localScale = new Vector3(0.7f, 0.32f, 0.7f);
            PaintPart(cuerpo, null, new Color(0.1f, 0.08f, 0.07f));
            PonerLucesDeColor(parent, centro, lado, mira);
        }

        // Cinco cabezas. El haz es un cono que se apaga, y barre el estrado y el pozo.
        void PonerLucesDeColor(Transform parent, Vector3 centro, Vector3 lado, Vector3 mira)
        {
            if (lado.sqrMagnitude < 0.01f) lado = Vector3.right;
            lado.Normalize();
            if (mira.sqrMagnitude < 0.01f) mira = Vector3.forward;
            mira.Normalize();
            Color[] gel =
            {
                new Color(1f, 0.16f, 0.18f),
                new Color(0.22f, 0.42f, 1f),
                new Color(0.28f, 0.95f, 0.42f),
                new Color(1f, 0.48f, 0.12f),
                new Color(0.78f, 0.28f, 1f),
            };
            float[] sitio = { -3.3f, -1.65f, 0f, 1.65f, 3.3f };
            float[] fase = { 0.2f, 1.45f, 2.7f, 3.9f, 5.15f };
            float[] vel = { 0.86f, 1.12f, 0.74f, 1.28f, 0.96f };
            Vector3 frente = centro + mira * 2.6f;
            float g = CajamarcaAltura.Sample(frente.x, frente.z);
            var hierro = new Color(0.14f, 0.13f, 0.14f);
            var barra = BoxPart(parent, "Farol", new Vector3(frente.x, g + 3.35f, frente.z), new Vector3(0.12f, 0.12f, 7.2f), null, hierro);
            barra.transform.rotation = Quaternion.LookRotation(lado, Vector3.up);

            showParent = parent;
            showLado = lado;
            showMira = mira;
            showLocal.Clear();
            showColor.Clear();
            focosShow.Clear();
            for (int i = 0; i < gel.Length; i++)
            {
                Vector3 p = frente + lado * sitio[i];
                float pg = CajamarcaAltura.Sample(p.x, p.z);
                if (i == 0 || i == gel.Length - 1)
                {
                    BoxPart(parent, "Farol", new Vector3(p.x, pg + 1.7f, p.z),
                        new Vector3(0.1f, 3.3f, 0.1f), null, hierro);
                }

                Vector3 desde = new Vector3(p.x, pg + 3.12f, p.z);
                Color nucleo = Color.Lerp(gel[i], Color.white, 0.35f);
                var hierroCan = new Color(0.08f, 0.08f, 0.09f);
                var can = BoxPart(parent, "Farol", desde, new Vector3(0.26f, 0.2f, 0.42f), null, hierroCan);
                var lente = BoxPart(parent, "Farol", desde, new Vector3(0.12f, 0.12f, 0.06f), null, nucleo);
                PintarGel(lente, nucleo);
                var haz = HazDeLuz(parent, gel[i]);

                var luz = new GameObject("LuzShow");
                luz.transform.SetParent(parent, false);
                luz.transform.position = parent.TransformPoint(desde);
                var light = luz.AddComponent<Light>();
                light.type = LightType.Spot;
                light.color = nucleo;
                light.intensity = 5.5f;
                light.range = 16f;
                light.spotAngle = 22f;
                light.innerSpotAngle = 8f;
                light.shadows = LightShadows.None;

                showColor.Add(gel[i]);
                showLocal.Add(Vector4.zero);
                focosShow.Add(new FocoBatam
                {
                    Can = can.transform,
                    Haz = haz.transform,
                    Halo = lente.transform,
                    Luz = light,
                    Montura = desde,
                    Fase = fase[i],
                    Vel = vel[i],
                });
            }

            MoverLucesShow();
        }

        // El haz sube al cielo y cruza el del vecino. Abajo, el color sigue en el piso.
        void MoverLucesShow()
        {
            int n = focosShow.Count;
            if (n == 0 || showParent == null) return;
            float t = Time.unscaledTime;
            for (int i = 0; i < 8; i++)
            {
                showMundo[i] = Vector4.zero;
                showGel[i] = Vector4.zero;
            }

            for (int i = 0; i < n && i < 8; i++)
            {
                FocoBatam foco = focosShow[i];
                if (foco.Can == null || foco.Haz == null || foco.Luz == null) continue;
                float u = t * foco.Vel + foco.Fase;
                float signo = (i % 2 == 0) ? 1f : -1f;
                float pan = Mathf.Lerp(-36f, 36f, Suave(u * 0.28f)) * signo;
                float tilt = Mathf.Lerp(12f, 42f, Suave(u * 0.17f + 0.25f));
                float panRad = pan * Mathf.Deg2Rad;
                float tiltRad = tilt * Mathf.Deg2Rad;
                Vector3 flat = showLado * Mathf.Sin(panRad) + showMira * Mathf.Cos(panRad);
                flat.y = 0f;
                if (flat.sqrMagnitude < 0.0001f) flat = showMira;
                flat.Normalize();
                Vector3 dir = flat * Mathf.Cos(tiltRad) + Vector3.up * Mathf.Sin(tiltRad);
                dir = showParent.TransformDirection(dir);
                if (dir.sqrMagnitude < 0.0001f) dir = Vector3.up;
                dir.Normalize();

                Vector3 worldDesde = showParent.TransformPoint(foco.Montura);
                var rot = Quaternion.LookRotation(dir, Vector3.up);
                foco.Can.rotation = rot;
                float cielo = Mathf.InverseLerp(12f, 42f, tilt);
                float largo = Mathf.Lerp(8.5f, 13.5f, cielo);
                float radio = Mathf.Lerp(0.42f, 0.72f, cielo);
                foco.Haz.position = worldDesde;
                foco.Haz.rotation = rot;
                foco.Haz.localScale = new Vector3(radio, radio, largo);
                if (foco.Halo != null)
                {
                    foco.Halo.position = worldDesde + dir * 0.22f;
                    foco.Halo.rotation = rot;
                    foco.Halo.localScale = new Vector3(0.14f, 0.14f, 0.05f);
                }

                var light = foco.Luz;
                light.transform.position = worldDesde;
                light.transform.rotation = rot;
                light.spotAngle = Mathf.Lerp(26f, 16f, cielo);
                light.innerSpotAngle = light.spotAngle * 0.35f;
                light.range = largo + 3f;
                light.intensity = 5.2f;

                float distancia = Mathf.Lerp(7.5f, 3.2f, cielo);
                Vector3 piso = foco.Montura + flat * distancia;
                float suelo = CajamarcaAltura.Sample(piso.x, piso.z);
                piso.y = suelo + 0.35f;
                float alcance = Mathf.Lerp(6.4f, 4.4f, cielo);
                Vector3 worldPiso = showParent.TransformPoint(piso);
                Vector3 local = district != null ? district.InverseTransformPoint(worldPiso) : piso;
                if (i < showLocal.Count)
                    showLocal[i] = new Vector4(local.x, local.y, local.z, alcance);
                showMundo[i] = new Vector4(worldPiso.x, worldPiso.y, worldPiso.z, alcance);
                if (i < showColor.Count) showGel[i] = showColor[i];
            }

            Shader.SetGlobalVectorArray("_ShowLuz", showMundo);
            Shader.SetGlobalVectorArray("_ShowColor", showGel);
            Shader.SetGlobalFloat("_ShowN", n);
        }

        static float Vaiven(float x)
        {
            float f = Mathf.Repeat(x, 2f);
            return f <= 1f ? f : 2f - f;
        }

        static float Suave(float x)
        {
            float p = Vaiven(x);
            return p * p * (3f - 2f * p);
        }

        static Mesh mallaHaz;

        static Mesh MallaDeHaz()
        {
            if (mallaHaz != null) return mallaHaz;
            const int lados = 8;
            var verts = new List<Vector3>(lados * 2);
            var cols = new List<Color>(lados * 2);
            var tris = new List<int>(lados * 6);
            for (int i = 0; i < lados; i++)
            {
                float a = i * Mathf.PI * 2f / lados;
                float c = Mathf.Cos(a);
                float s = Mathf.Sin(a);
                verts.Add(new Vector3(c * 0.16f, s * 0.16f, 0f));
                cols.Add(new Color(1f, 1f, 1f, 0.55f));
                verts.Add(new Vector3(c, s, 1f));
                cols.Add(new Color(1f, 1f, 1f, 0f));
            }

            for (int i = 0; i < lados; i++)
            {
                int n = (i + 1) % lados;
                int a0 = i * 2;
                int a1 = n * 2;
                int b0 = a0 + 1;
                int b1 = a1 + 1;
                tris.Add(a0);
                tris.Add(b0);
                tris.Add(a1);
                tris.Add(a1);
                tris.Add(b0);
                tris.Add(b1);
            }

            mallaHaz = new Mesh();
            mallaHaz.name = "HazShow";
            mallaHaz.SetVertices(verts);
            mallaHaz.SetColors(cols);
            mallaHaz.SetTriangles(tris, 0);
            return mallaHaz;
        }

        static GameObject HazDeLuz(Transform parent, Color gel)
        {
            var go = new GameObject("HazShow");
            go.transform.SetParent(parent, false);
            var filter = go.AddComponent<MeshFilter>();
            filter.sharedMesh = MallaDeHaz();
            var rend = go.AddComponent<MeshRenderer>();
            var shader = Shader.Find("AYNI/Haz") ?? Shader.Find("Sprites/Default");
            var mat = new Material(shader);
            mat.color = new Color(gel.r, gel.g, gel.b, 0.8f);
            rend.sharedMaterial = mat;
            rend.shadowCastingMode = ShadowCastingMode.Off;
            rend.receiveShadows = false;
            return go;
        }

        // El gel no pasa por la noche de la calle: el foco se ve de su color.
        static void PintarGel(GameObject go, Color gel)
        {
            if (go == null) return;
            var renderer = go.GetComponent<Renderer>();
            if (renderer == null) return;
            var shader = Shader.Find("Unlit/Color") ?? Shader.Find("Sprites/Default");
            if (shader == null) return;
            var mat = new Material(shader);
            mat.color = gel;
            renderer.sharedMaterial = mat;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }

        void OirConcierto(Walker hero)
        {
            bool cerca = false;
            for (int i = 0; i < walkers.Count; i++)
            {
                var walker = walkers[i];
                if (!walker.Toca || walker.Body == null) continue;
                if ((walker.Local - hero.Local).sqrMagnitude < 26f * 26f)
                {
                    cerca = true;
                    break;
                }
            }

            if (cerca && !oyoConcierto)
                Note("Eterno toca Mi Lugar. Salta la valla y poguea con los fanes.");
            if (heroEnPista && !dijoHardcore)
                Note("Modo hardcore. Pogueas al doble con Eterno.");
            dijoHardcore = heroEnPista;
            oyoConcierto = cerca;
        }

        void SpawnMartin()
        {
            oyoMartin = false;
            Vector3 at = new Vector3(fountainX - 22f, 0.2f, fountainZ + 4f);
            if (plazaFound && plazaHalfU > 20f)
            {
                Vector2 puesto = PlazaWorld(-plazaHalfU + PlazaWalk * 0.5f, -6f);
                at = new Vector3(puesto.x, 0.2f, puesto.y);
            }

            if (walkReady && !OnStreet(at.x, at.z))
                at = BuscarCalle(at, new Vector3(plazaCos, 0f, plazaSin));
            if (CalleCerrada(at.x, at.z) && plazaFound)
            {
                Vector2 sur = PlazaWorld(8f, -plazaHalfV + PlazaWalk * 0.5f);
                at = new Vector3(sur.x, 0.2f, sur.y);
            }

            at.y = SueloDeLaCalle.Altura(at.x, at.z);
            if (!Colocar("Martin", ref at)) return;
            var sheet = BattleCast.MartinVera;
            var walker = Spawn("Calle" + sheet.Id, sheet, StreetScale(CastScale.Of(sheet)), true, at, false);
            walker.Home = at;
            walker.Goal = at;
            walker.There = at;
            walker.Roam = 0f;
            walker.Speed = 0f;
            walker.Chicha = true;
            walkers.Add(walker);
            PonerTema(walker.Body.gameObject, "Battle/Audio/chicha-en-cacho", null, 0.72f, 4f, 26f);
        }

        void OirMartin(Walker hero)
        {
            bool cerca = false;
            for (int i = 0; i < walkers.Count; i++)
            {
                var walker = walkers[i];
                if (!walker.Chicha || walker.Body == null) continue;
                if ((walker.Local - hero.Local).sqrMagnitude < 20f * 20f)
                {
                    cerca = true;
                    break;
                }
            }

            if (cerca && !oyoMartin)
                Note("Martín Vera canta Chicha en Cacho.");
            oyoMartin = cerca;
        }

        AudioSource PonerTema(GameObject cuerpo, string recurso, AudioClip respaldo, float volumen, float minimo, float maximo, bool arrancar = true)
        {
            var clip = string.IsNullOrEmpty(recurso) ? null : BattleRuntimeAssets.LoadAudio(recurso);
            if (clip == null) clip = respaldo;
            if (clip == null || cuerpo == null) return null;
            var source = cuerpo.AddComponent<AudioSource>();
            source.clip = clip;
            source.loop = true;
            source.spatialBlend = 1f;
            source.minDistance = minimo;
            source.maxDistance = maximo;
            source.rolloffMode = AudioRolloffMode.Linear;
            source.volume = volumen;
            source.playOnAwake = false;
            if (arrancar) source.Play();
            return source;
        }

        void PonerRock(GameObject cuerpo)
        {
            eternoEscenario = PonerTema(cuerpo, "Battle/Audio/eterno-mi-lugar", ClipDeRock(), 0.72f, 6f, 48f, false);
            if (eternoEscenario != null) eternoEscenario.loop = false;
        }

        // Zetafuego no se queda en la tarima. Camina entre los fanes y poguea con cada uno.
        void BailarEntrePublico(Walker zeta)
        {
            if (zeta.Talla.sqrMagnitude < 0.01f && zeta.Body != null)
                zeta.Talla = zeta.Body.localScale;
            Walker fan = FanDelTurno();
            if (fan == null)
            {
                zeta.Fase = 0.4f;
                zeta.Anim?.PlayLocomotion(false, false);
                return;
            }

            zeta.Fase = fan.Fase;
            Vector3 sitio = AlLadoDelFan(fan);
            Vector3 to = sitio - zeta.Local;
            to.y = 0f;
            bool junto = to.sqrMagnitude <= 1.35f * 1.35f;
            if (!junto)
            {
                quedaFan = 2.2f;
                bool moving = false;
                if (to.sqrMagnitude > 0.0001f)
                {
                    moving = Slide(zeta, to.normalized, 3.2f);
                    if (!moving) moving = EmpujarLado(zeta, to, 2.6f);
                }

                if (moving)
                    atascoFan = 0f;
                else
                {
                    atascoFan += Time.deltaTime;
                    if (atascoFan > 0.85f)
                    {
                        atascoFan = 0f;
                        fanTurno++;
                        quedaFan = 2.2f;
                    }
                }

                Vector3 cara = to.sqrMagnitude > 0.0001f ? to.normalized : Vector3.forward;
                zeta.Anim?.PlayStep(CamaraTercera.WorldDir(district, cara), moving);
                return;
            }

            atascoFan = 0f;
            zeta.Anim?.PlayLocomotion(false, false);
            quedaFan -= Time.deltaTime;
            if (quedaFan > 0f) return;
            quedaFan = 2.2f;
            fanTurno++;
        }

        Walker FanDelTurno()
        {
            int n = 0;
            for (int i = 0; i < walkers.Count; i++)
            {
                var walker = walkers[i];
                if (walker.Pogo && !walker.Caido && !walker.Llevado && walker.Body != null) n++;
            }

            if (n == 0) return null;
            if (fanTurno < 0) fanTurno = 0;
            int turno = fanTurno % n;
            int vistos = 0;
            for (int i = 0; i < walkers.Count; i++)
            {
                var walker = walkers[i];
                if (!walker.Pogo || walker.Caido || walker.Llevado || walker.Body == null) continue;
                if (vistos == turno) return walker;
                vistos++;
            }

            return null;
        }

        Vector3 AlLadoDelFan(Walker fan)
        {
            MedirBatan(fan.Local.x, fan.Local.z, out float along, out float side);
            float empuje = 0f;
            if (Mathf.Abs(side) >= 1.5f)
                empuje = side > 0f ? -1.2f : 1.2f;
            float fondo = along + (Mathf.Abs(empuje) < 0.01f ? 1.25f : 0f);
            return CrudoBatan(fondo, side + empuje);
        }

        // El compás de la canción que está sonando. 128 BPM, el mismo del pogo.
        bool MedirCompas()
        {
            AudioSource src = null;
            if (temaBanda != null && temaBanda.isPlaying) src = temaBanda;
            else if (eternoEscenario != null && eternoEscenario.isPlaying) src = eternoEscenario;
            if (src == null) return false;
            moshCompas = src.time * (128f / 60f);
            moshGolpe = 0f;
            src.GetOutputData(ondaShow, 0);
            float suma = 0f;
            for (int i = 0; i < ondaShow.Length; i++)
                suma += ondaShow[i] * ondaShow[i];
            moshGolpe = Mathf.Clamp01(Mathf.Sqrt(suma / ondaShow.Length) * 6f);
            return true;
        }

        bool EnElPozo(Walker hero)
        {
            if (hero == null) return false;
            if (CalleCerrada(hero.Local.x, hero.Local.z)) return true;
            return Plano(hero.Local, CrudoBatan(23.2f, 0f)) < 24f;
        }

        void VigilarMosh(Walker hero)
        {
            if (!moshSuena || !EnElPozo(hero)) return;
            if (!dijoMosh && moshCompas > 2.2f)
            {
                dijoMosh = true;
                Note("El pogo arma el mosh. Cierran el círculo al compás.");
            }

            if (atropellado != null || ambulanciaPaso != 0) return;
            if (moshCompas < 14f) return;
            float ciclo = moshCompas % 16f;
            if (ciclo < 13.4f || ciclo > 14.8f) return;
            MarcarAtropello();
        }

        void MarcarAtropello()
        {
            Walker fan = null;
            for (int i = 0; i < walkers.Count; i++)
            {
                var walker = walkers[i];
                if (!walker.Pogo || walker.Caido || walker.Body == null) continue;
                if (walker.Puesto == 7) fan = walker;
                if (fan == null) fan = walker;
            }

            if (fan == null) return;
            fan.Caido = true;
            fan.Anim?.PlayFall();
            if (fan.Talla.sqrMagnitude < 0.01f && fan.Body != null)
                fan.Talla = fan.Body.localScale;
            if (fan.Body != null && fan.Talla.sqrMagnitude > 0.01f)
            {
                fan.Body.localScale = new Vector3(
                    Mathf.Abs(fan.Talla.x) * 1.25f, fan.Talla.y * 0.32f, fan.Talla.z);
            }

            atropellado = fan;
            if (!dijoAtropello)
            {
                dijoAtropello = true;
                Note("Lo atropellan en el círculo. Llega la ambulancia.");
            }

            SalirAmbulancia();
        }

        void CorrerMosh(Walker walker)
        {
            if (walker.Body == null) return;
            Vector3 meta = FormaMosh(walker);
            Vector3 to = meta - walker.Local;
            to.y = 0f;
            if (to.sqrMagnitude < 0.05f)
            {
                walker.Anim?.PlayLocomotion(false, false);
                return;
            }

            float vel = to.sqrMagnitude > 6f ? 5.6f : 4.3f;
            Vector3 paso = to.normalized * vel * Time.deltaTime;
            if (paso.sqrMagnitude > to.sqrMagnitude) paso = to;
            Vector3 next = walker.Local + paso;
            if (!PisoDe(walker, next.x, next.z))
            {
                walker.Anim?.PlayLocomotion(false, false);
                return;
            }

            walker.Local = new Vector3(next.x, walker.Local.y, next.z);
            walker.Anim?.PlayStep(CamaraTercera.WorldDir(district, to.normalized), true);
        }

        // Cada 16 pulsos cambia la figura: círculo, montón, círculo al revés, dos filas que chocan.
        // El volumen del tema abre o cierra el círculo.
        Vector3 FormaMosh(Walker walker)
        {
            float ciclo = moshCompas % 16f;
            if (ciclo < 0f) ciclo += 16f;
            float asiento = walker.Puesto + (walker.Baile ? 0.5f : 0f);
            const float centro = 23.2f;
            if (ciclo >= 12f)
            {
                float t = (ciclo - 12f) / 4f;
                float abre = Mathf.Abs(Mathf.Sin(t * Mathf.PI * 2f));
                float lado = (walker.Puesto % 2 == 0 ? -1f : 1f) * Mathf.Lerp(0.7f, 4.6f, abre);
                float fondo = 20.2f + (walker.Puesto / 2) * 1.15f;
                if (walker.Baile) fondo = centro;
                return CrudoBatan(fondo, lado);
            }

            bool apretado = ciclo >= 4f && ciclo < 8f;
            float dir = ciclo < 4f ? 1f : -1f;
            if (apretado) dir = 1f;
            float radioLargo = (apretado ? 1.55f : 4.1f) + moshGolpe * (apretado ? 0.45f : 1.15f);
            float radioAncho = (apretado ? 1.25f : 3.3f) + moshGolpe * 0.35f;
            float ang = asiento * (Mathf.PI * 2f / 8f) + moshCompas * (apretado ? 0.55f : 0.95f) * dir;
            return CrudoBatan(centro + Mathf.Cos(ang) * radioLargo, Mathf.Sin(ang) * radioAncho);
        }

        void SalirAmbulancia()
        {
            if (ambulanciaPaso != 0) return;
            ambulancia = ArmarAmbulancia();
            ambulanciaAlong = 15.4f;
            ambulanciaSide = -4.6f;
            ambulanciaPaso = 1;
            PonerAmbulancia();
        }

        void PasoAmbulancia()
        {
            if (ambulanciaPaso == 0 || ambulanciaPaso >= 4 || ambulancia == null) return;
            if (atropellado == null)
            {
                GuardarAmbulancia();
                return;
            }

            if (ambulanciaPaso == 1)
            {
                MedirBatan(atropellado.Local.x, atropellado.Local.z, out float metaAlong, out float metaSide);
                float vel = 8.2f * Time.deltaTime;
                ambulanciaAlong = Mathf.MoveTowards(ambulanciaAlong, metaAlong, vel);
                ambulanciaSide = Mathf.MoveTowards(ambulanciaSide, metaSide, vel);
                if (Mathf.Abs(ambulanciaAlong - metaAlong) < 1.15f
                    && Mathf.Abs(ambulanciaSide - metaSide) < 1.15f)
                {
                    ambulanciaPaso = 2;
                    ambulanciaReloj = 1.05f;
                }
            }
            else if (ambulanciaPaso == 2)
            {
                ambulanciaReloj -= Time.deltaTime;
                if (ambulanciaReloj <= 0f)
                {
                    SubirAtropellado();
                    ambulanciaPaso = 3;
                    Note("La ambulancia se lleva al atropellado.");
                }
            }
            else if (ambulanciaPaso == 3)
            {
                float vel = 9f * Time.deltaTime;
                ambulanciaAlong = Mathf.MoveTowards(ambulanciaAlong, 10.5f, vel);
                ambulanciaSide = Mathf.MoveTowards(ambulanciaSide, -4.2f, vel * 0.65f);
                if (ambulanciaAlong <= 10.8f)
                    GuardarAmbulancia();
            }

            if (ambulancia != null && ambulancia.gameObject.activeSelf)
                PonerAmbulancia();
        }

        void SubirAtropellado()
        {
            if (atropellado == null || atropellado.Body == null || ambulancia == null) return;
            var cuerpo = atropellado.Body;
            atropellado.Llevado = true;
            atropellado.Pogo = false;
            Vector3 talla = atropellado.Talla.sqrMagnitude > 0.01f ? atropellado.Talla : cuerpo.localScale;
            cuerpo.SetParent(ambulancia, true);
            cuerpo.localPosition = new Vector3(0f, 0.95f, -0.35f);
            cuerpo.localRotation = Quaternion.Euler(72f, 0f, 0f);
            cuerpo.localScale = new Vector3(Mathf.Abs(talla.x) * 1.1f, Mathf.Abs(talla.y) * 0.34f, Mathf.Abs(talla.z));
        }

        void PonerAmbulancia()
        {
            if (ambulancia == null) return;
            Vector3 p = CrudoBatan(ambulanciaAlong, ambulanciaSide);
            float g = SueloDeLaCalle.Altura(p.x, p.z);
            ambulancia.localPosition = new Vector3(p.x, g, p.z);
            Vector3 eje = ambulanciaPaso == 3 ? -batanEje : batanEje;
            if (eje.sqrMagnitude < 0.01f) eje = Vector3.forward;
            Vector3 mundo = RuntimeParent != null ? RuntimeParent.TransformDirection(eje) : eje;
            if (mundo.sqrMagnitude < 0.01f) mundo = Vector3.forward;
            ambulancia.rotation = Quaternion.LookRotation(mundo.normalized, Vector3.up);
        }

        void GuardarAmbulancia()
        {
            if (sirena != null) sirena.Stop();
            if (ambulancia != null) ambulancia.gameObject.SetActive(false);
            ambulanciaPaso = 4;
        }

        Transform ArmarAmbulancia()
        {
            var root = new GameObject("Ambulancia");
            root.transform.SetParent(RuntimeParent, false);
            var blanco = new Color(0.94f, 0.95f, 0.96f);
            var rojo = new Color(0.78f, 0.12f, 0.14f);
            var vidrio = new Color(0.16f, 0.22f, 0.28f);
            var rueda = new Color(0.1f, 0.1f, 0.11f);
            BoxPart(root.transform, "Adorno", new Vector3(0f, 0.95f, 0f), new Vector3(1.72f, 1.2f, 3.7f), null, blanco);
            BoxPart(root.transform, "Adorno", new Vector3(0f, 1.15f, 0f), new Vector3(1.78f, 0.22f, 3.74f), null, rojo);
            BoxPart(root.transform, "Adorno", new Vector3(0f, 1.7f, 0.85f), new Vector3(1.6f, 0.72f, 1.35f), null, blanco);
            BoxPart(root.transform, "Adorno", new Vector3(0f, 1.72f, 1.42f), new Vector3(1.4f, 0.48f, 0.08f), null, vidrio);
            BoxPart(root.transform, "Adorno", new Vector3(0f, 1.62f, -0.15f), new Vector3(0.62f, 0.1f, 0.16f), null, rojo);
            BoxPart(root.transform, "Adorno", new Vector3(0f, 1.62f, -0.15f), new Vector3(0.16f, 0.1f, 0.62f), null, rojo);
            float[] zx = { 1.15f, -1.15f };
            float[] xx = { 0.72f, -0.72f };
            for (int i = 0; i < 2; i++)
            {
                for (int j = 0; j < 2; j++)
                {
                    BoxPart(root.transform, "Adorno", new Vector3(xx[j], 0.32f, zx[i]),
                        new Vector3(0.28f, 0.56f, 0.56f), null, rueda);
                }
            }

            var fuente = root.AddComponent<AudioSource>();
            fuente.clip = ClipSirena();
            fuente.loop = true;
            fuente.spatialBlend = 1f;
            fuente.minDistance = 5f;
            fuente.maxDistance = 36f;
            fuente.rolloffMode = AudioRolloffMode.Linear;
            fuente.volume = 0.42f;
            fuente.playOnAwake = false;
            fuente.Play();
            sirena = fuente;
            return root.transform;
        }

        static AudioClip ClipSirena()
        {
            const int rate = 22050;
            const float seconds = 1.6f;
            int n = Mathf.RoundToInt(rate * seconds);
            var data = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)rate;
                bool alta = t >= 0.8f;
                float f = alta ? 860f : 640f;
                float local = alta ? (t - 0.8f) / 0.8f : t / 0.8f;
                float puerta = Mathf.Sin(local * Mathf.PI);
                data[i] = Mathf.Sin(Mathf.PI * 2f * f * t) * 0.35f * puerta;
            }

            var clip = AudioClip.Create("sirena", n, 1, rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        // Mientras se oye Quédate un rato más, esa es la única canción.
        // Antes, en el Batán, Eterno suena una sola vez.
        void MezclarShow(Walker hero)
        {
            EternoPrimero(hero);
            bool eternoSuena = eternoEscenario != null && eternoEscenario.isPlaying;
            bool solo = !eternoSuena && OyeSoloBanda(hero);
            Jornada.SilenciarCama(eternoSuena || solo);
            if (solo)
                CallarMenos(temaBanda);
            else if (eternoSuena)
                CallarConcierto();
            else if (eternoYa)
                SoltarConcierto();
        }

        bool EternoListo()
        {
            if (eternoEscenario == null) return true;
            return eternoYa && !eternoEscenario.isPlaying;
        }

        void ArrancarTemaBanda()
        {
            if (temaBanda == null || temaBanda.isPlaying) return;
            temaBanda.Play();
            if (dijoPasion) return;
            dijoPasion = true;
            Note("Cállate y Baila canta Quédate un rato más. Zetafuego poguea entre el público.");
        }

        bool OyeSoloBanda(Walker hero)
        {
            if (temaBanda == null || !temaBanda.isPlaying || hero == null)
            {
                bandaSola = false;
                return false;
            }

            Walker zeta = zetaGuia;
            if (zeta == null || zeta.Body == null)
            {
                zeta = null;
                for (int i = 0; i < walkers.Count; i++)
                {
                    if (!walkers[i].Baile || walkers[i].Body == null) continue;
                    zeta = walkers[i];
                    break;
                }
            }

            if (zeta == null) return bandaSola;
            float dist = Plano(hero.Local, zeta.Local);
            bandaSola = bandaSola ? dist < 40f : dist < 32f;
            return bandaSola;
        }

        void CallarMenos(AudioSource queda)
        {
            if (eternoEscenario != null && eternoEscenario != queda && eternoEscenario.isPlaying)
                eternoEscenario.Pause();
            for (int i = 0; i < walkers.Count; i++)
            {
                var walker = walkers[i];
                if (walker == null || walker.Body == null) continue;
                var fuentes = walker.Body.GetComponents<AudioSource>();
                for (int s = 0; s < fuentes.Length; s++)
                {
                    var fuente = fuentes[s];
                    if (fuente == null || fuente == queda) continue;
                    if (fuente.isPlaying) fuente.Pause();
                }
            }
        }

        // Al entrar al Batán, Eterno suena una vez. No vuelve a empezar.
        void EternoPrimero(Walker hero)
        {
            if (eternoEscenario == null)
            {
                if (zetaEnEscenario) ArrancarTemaBanda();
                return;
            }

            bool dentro = hero != null && CalleCerrada(hero.Local.x, hero.Local.z);
            if (!eternoYa && dentro)
            {
                eternoEscenario.loop = false;
                eternoEscenario.time = 0f;
                eternoEscenario.Play();
                eternoYa = true;
                eternoDesde = Time.time;
                Note("Eterno abre el Batán. Toca Mi Lugar una vez.");
            }

            if (eternoYa && !eternoEscenario.isPlaying && zetaEnEscenario)
                ArrancarTemaBanda();
        }

        void CallarConcierto()
        {
            for (int i = 0; i < walkers.Count; i++)
            {
                var walker = walkers[i];
                if (walker == null || walker.Body == null || walker.Toca) continue;
                if (!CalleCerrada(walker.Local.x, walker.Local.z)) continue;
                var fuentes = walker.Body.GetComponents<AudioSource>();
                for (int s = 0; s < fuentes.Length; s++)
                {
                    if (fuentes[s] == null || fuentes[s] == eternoEscenario) continue;
                    if (fuentes[s].isPlaying) fuentes[s].Pause();
                }
            }
        }

        void SoltarConcierto()
        {
            for (int i = 0; i < walkers.Count; i++)
            {
                var walker = walkers[i];
                if (walker == null || walker.Body == null) continue;
                var fuentes = walker.Body.GetComponents<AudioSource>();
                for (int s = 0; s < fuentes.Length; s++)
                {
                    if (fuentes[s] == null || fuentes[s] == eternoEscenario) continue;
                    if (!fuentes[s].isPlaying) fuentes[s].UnPause();
                }
            }
        }

        // Motivo propio en pentatónica, con guitarra, bajo y caja.
        // No es una canción de Eterno, Padme ni Corazones Niños.
        static AudioClip ClipDeRock()
        {
            const int rate = 22050;
            const float seconds = 6.4f;
            int n = Mathf.RoundToInt(rate * seconds);
            var data = new float[n];
            float[] nota =
            {
                220.00f, 261.63f, 293.66f, 329.63f,
                293.66f, 261.63f, 220.00f, 196.00f,
            };
            float[] bajo =
            {
                110.00f, 130.81f, 146.83f, 164.81f,
                146.83f, 130.81f, 110.00f, 98.00f,
            };
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)rate;
                int b = Mathf.Min(7, Mathf.FloorToInt(t / 0.8f));
                float local = t / 0.8f - b;
                float f = nota[b];
                float puerta = Mathf.Exp(-local * 3.2f);
                float guitarra = Mathf.Sin(Mathf.PI * 2f * f * t) * 0.34f
                    + Mathf.Sin(Mathf.PI * 2f * f * 2f * t) * 0.16f;
                guitarra *= puerta;
                float cuerda = Mathf.Sin(Mathf.PI * 2f * bajo[b] * t) * (0.28f + 0.08f * puerta);
                float drum = 0f;
                if (local < 0.08f)
                    drum += (1f - local / 0.08f) * Mathf.Sin(Mathf.PI * 2f * 62f * t);
                if (local > 0.46f && local < 0.58f)
                {
                    float u = (local - 0.46f) / 0.12f;
                    int hash = (i * 1103515245 + 12345) & 0x7fffffff;
                    float ruido = hash / 2147483647f * 2f - 1f;
                    drum += (1f - u) * 0.42f * ruido;
                }

                float mix = guitarra + cuerda + drum * 0.7f;
                data[i] = Mathf.Clamp(mix * 0.72f, -1f, 1f);
            }

            var clip = AudioClip.Create("rock-batan", n, 1, rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        // Recorre la vereda de la plaza. Pide para la gasolina o multa.
        // Con el farmeo alto no entra al aura.
        void SpawnPolicia()
        {
            Vector3 ida;
            Vector3 vuelta;
            if (plazaFound && plazaHalfU > 20f)
            {
                Vector2 a = PlazaWorld(plazaHalfU - PlazaWalk * 0.45f, plazaHalfV * 0.4f);
                Vector2 b = PlazaWorld(plazaHalfU - PlazaWalk * 0.45f, -plazaHalfV * 0.4f);
                ida = new Vector3(a.x, 0.2f, a.y);
                vuelta = new Vector3(b.x, 0.2f, b.y);
            }
            else
            {
                ida = new Vector3(16f, 0.2f, 8f);
                vuelta = new Vector3(16f, 0.2f, -8f);
            }

            Vector3 eje = plazaFound ? new Vector3(plazaCos, 0f, plazaSin) : Vector3.right;
            if (walkReady && !OnStreet(ida.x, ida.z)) ida = BuscarCalle(ida, eje);
            if (walkReady && !OnStreet(vuelta.x, vuelta.z)) vuelta = BuscarCalle(vuelta, eje);
            ida.y = SueloDeLaCalle.Altura(ida.x, ida.z);
            vuelta.y = SueloDeLaCalle.Altura(vuelta.x, vuelta.z);
            if (!Colocar("Policia", ref ida) || !Colocar("PoliciaVuelta", ref vuelta)) return;
            policiaIda = ida;
            policiaVuelta = vuelta;
            var walker = Spawn("Policia", BattleCast.Policia, StreetScale(CastScale.Of(BattleCast.Policia)), true, ida, false);
            walker.Policia = true;
            walker.Home = ida;
            walker.Goal = vuelta;
            walker.Speed = 3.15f;
            walker.Wait = 0.4f;
            var tinta = walker.Body != null ? walker.Body.GetComponent<SpriteRenderer>() : null;
            walker.Tinte = new Color(0.22f, 0.36f, 0.78f);
            if (tinta != null) tinta.color = walker.Tinte;
            walkers.Add(walker);
        }

        // Jefe del azul de la calle. Recorre el mismo lado, un paso hacia la fuente.
        // No pide gasolina. La foto se le toma con P.
        void SpawnJefe()
        {
            Vector3 ida;
            Vector3 vuelta;
            Vector3 eje = plazaFound ? new Vector3(plazaCos, 0f, plazaSin) : Vector3.right;
            if (plazaFound && plazaHalfU > 20f)
            {
                float u = plazaHalfU - PlazaWalk * 0.45f - 2.8f;
                Vector2 a = PlazaWorld(u, plazaHalfV * 0.05f);
                Vector2 b = PlazaWorld(u, plazaHalfV * 0.36f);
                ida = new Vector3(a.x, 0.2f, a.y);
                vuelta = new Vector3(b.x, 0.2f, b.y);
            }
            else
            {
                ida = new Vector3(13f, 0.2f, 2f);
                vuelta = new Vector3(13f, 0.2f, 10f);
            }

            if (walkReady && !OnStreet(ida.x, ida.z)) ida = BuscarCalle(ida, eje);
            if (walkReady && !OnStreet(vuelta.x, vuelta.z)) vuelta = BuscarCalle(vuelta, eje);
            ida.y = SueloDeLaCalle.Altura(ida.x, ida.z);
            vuelta.y = SueloDeLaCalle.Altura(vuelta.x, vuelta.z);
            if (!Colocar("Jefe", ref ida) || !Colocar("JefeVuelta", ref vuelta)) return;
            jefeIda = ida;
            jefeVuelta = vuelta;
            var walker = Spawn("Jefe", BattleCast.Policia, StreetScale(CastScale.Of(BattleCast.Policia)), true, ida, false);
            walker.Jefe = true;
            walker.Home = ida;
            walker.Goal = vuelta;
            walker.Speed = 1.15f;
            walker.Wait = 0.2f;
            var tinta = walker.Body != null ? walker.Body.GetComponent<SpriteRenderer>() : null;
            walker.Tinte = new Color(0.08f, 0.11f, 0.28f);
            if (tinta != null) tinta.color = walker.Tinte;
            walkers.Add(walker);
        }

        void SpawnRoutes()
        {
            var sheets = BattleCast.Vecinos;
            int n = sheets.Length * 2;
            for (int i = 0; i < n; i++)
            {
                Vector3 from = SpreadPoint(i, n);
                Vector3 to = FarPoint(from);
                if ((to - from).sqrMagnitude < 80f * 80f)
                    to = FarPoint(from);
                from.y = SueloDeLaCalle.Altura(from.x, from.z);
                to.y = SueloDeLaCalle.Altura(to.x, to.z);
                if (!Colocar("Vecino" + (i + 1), ref from)) continue;
                var sheet = sheets[i % sheets.Length];
                float scale = StreetScale(BattleCast.BodyScale(sheet));
                var walker = Spawn("CalleVecino" + (i + 1), sheet, scale, true, from, false);
                walker.Home = from;
                walker.Goal = to;
                walker.There = from;
                walker.Route = true;
                walker.Roam = 0f;
                walker.Speed = 1.2f + (i % 5) * 0.16f;
                walker.Wait = 0.2f * (i % 4);
                walkers.Add(walker);
            }
        }

        void SpawnHunters()
        {
            var hero = Hero();
            Vector3 origin = hero != null ? hero.Local : Vector3.zero;
            var sheets = BattleCast.Cazadores;
            for (int i = 0; i < sheets.Length; i++)
            {
                Vector3 from = FarPoint(origin);
                from.y = SueloDeLaCalle.Altura(from.x, from.z);
                if (!Colocar("Cazador" + (i + 1), ref from)) continue;
                var sheet = sheets[i];
                float scale = StreetScale(BattleCast.BodyScale(sheet));
                var walker = Spawn("CalleCaza" + (i + 1), sheet, scale, true, from, false);
                walker.Home = from;
                walker.Goal = from;
                walker.There = from;
                walker.Chase = true;
                walker.Roam = 0f;
                walker.Speed = 4.05f;
                walker.Wait = 0.15f * i;
                walkers.Add(walker);
            }
        }

        Vector3 FarPoint(Vector3 from)
        {
            Vector3 best = RandomPoint();
            float bestDist = 0f;
            for (int i = 0; i < 16; i++)
            {
                Vector3 tryAt = RandomPoint();
                float dist = (tryAt - from).sqrMagnitude;
                if (dist > bestDist)
                {
                    best = tryAt;
                    bestDist = dist;
                }

                if (dist > 36f * 36f) return tryAt;
            }

            return best;
        }

        void AddStreet(CharacterSheet sheet, Vector3 at, bool hero)
        {
            var walker = Spawn(hero ? "CalleHero" : "Calle" + sheet.Name,
                sheet, StreetScale(CastScale.Of(sheet)), true, at, hero);
            if (!hero)
            {
                walker.Route = true;
                walker.Roam = 0f;
                walker.Home = at;
                walker.There = at;
                walker.Goal = FarPoint(at);
                walker.Speed = 1.25f + (sheet.Name.Length % 4) * 0.15f;
            }

            walkers.Add(walker);
        }

        // Escala en calle: a escala 1 el cuerpo mide 6 unidades → 0.35 = ~2.1 m,
        // por debajo de las puertas del plano (2.2–3.2 m). Antes 0.55 (3.3 m)
        // los hacía más altos que las puertas.
        static float StreetScale(float scale) => scale * 0.35f;

        Walker Spawn(string name, CharacterSheet sheet, float scale, bool facesRight, Vector3 local, bool hero)
        {
            var go = new GameObject(name);
            go.transform.SetParent(RuntimeParent, false);
            var ink = go.AddComponent<SpriteRenderer>();
            ink.sortingOrder = hero ? 24 : 20;
            var anim = go.AddComponent<SpriteFrameAnimator>();
            BattleSprites.Apply(anim, sheet);
            go.transform.localScale = new Vector3(Mathf.Abs(scale), Mathf.Abs(scale), 1f);
            go.transform.localPosition = local;
            anim.SetFacing(facesRight);
            anim.PlayLocomotion(false, false);
            return new Walker
            {
                Body = go.transform,
                Anim = anim,
                Local = local,
                Goal = local,
                Home = local,
                Speed = hero ? 2.3f : 1.7f,
                Roam = hero ? 0f : 16f,
                Hero = hero,
            };
        }

        void BuildHud()
        {
            var canvas = battle != null && battle.startScreen != null
                ? battle.startScreen.transform.parent
                : null;
            if (canvas == null) return;

            var plate = Storybook.Plate(canvas, "CalleHUD", Vector2.zero, Vector2.one, new Color(0f, 0f, 0f, 0f));
            hud = plate.gameObject;

            var kicker = AddText(hud.transform, "Kicker", new Vector2(0.18f, 0.88f), new Vector2(0.82f, 0.97f));
            Storybook.DressText(kicker, LeyDelMonte.StreetKicker,
                Storybook.DisplayFont, 26, Color.white, FontStyle.Bold);
            Storybook.Stamp(kicker);

            TituloFugaz.Begin(kicker.gameObject);
            ArmarVineta();
            promesaTexto = AddText(hud.transform, "Promesa", new Vector2(0.36f, 0.012f), new Vector2(0.80f, 0.075f));
            Storybook.DressText(promesaTexto, "", Storybook.DisplayFont, 18, Color.white, FontStyle.Bold);
            Storybook.Stamp(promesaTexto);
            PintarPromesa();

            movimientos = BotonesDeMovimiento.Armar(
                hud.transform,
                () => SetVista(Vista.Planta),
                () => SetVista(Vista.Inclinacion),
                () => ZoomPlan(0.72f),
                () => ZoomPlan(1.38f),
                AlternarVuelo);
            editarButton = Storybook.IconButton(hud.transform, "Editar", Storybook.Marca.Editar,
                new Vector2(0.264f, 0.02f), new Vector2(0.338f, 0.15f),
                AlternarEdicion);
            ArmarPaleta();
            Storybook.IconButton(hud.transform, "Volver", Storybook.Marca.Volver,
                new Vector2(0.908f, 0.02f), new Vector2(0.982f, 0.15f),
                Close);
            var historia = Storybook.IconButton(hud.transform, "Historia", Storybook.Marca.Historia,
                new Vector2(0.018f, 0.83f), new Vector2(0.092f, 0.96f),
                () => battle?.AbrirHistoria());
            Storybook.Pista(historia, "Historia");
            Storybook.Pista(editarButton, "Editar mapa");
            AlforjaDelCamino.Preparar(hud.transform);
            Storybook.Brujula(hud.transform);
            vidaTexto = Signo(hud.transform, "Vida", Storybook.Marca.Vida,
                new Color(0.86f, 0.24f, 0.22f),
                new Vector2(0.018f, 0.66f), new Vector2(0.20f, 0.76f));
            auraTexto = Signo(hud.transform, "Aura", Storybook.Marca.Aura,
                new Color(0.95f, 0.78f, 0.28f),
                new Vector2(0.018f, 0.54f), new Vector2(0.20f, 0.64f));
            atractivoTexto = Signo(hud.transform, "Atractivo", Storybook.Marca.Atractivo,
                new Color(0.93f, 0.46f, 0.62f),
                new Vector2(0.018f, 0.42f), new Vector2(0.20f, 0.52f));
            var comidaPlaca = Storybook.Plate(hud.transform, "Comida",
                new Vector2(0.018f, 0.30f), new Vector2(0.24f, 0.40f),
                new Color(0.10f, 0.06f, 0.03f, 0.72f));
            comidaTexto = AddText(comidaPlaca.transform, "Cuenta", Vector2.zero, Vector2.one);
            Storybook.DressText(comidaTexto, "0/3   S/ 0", Storybook.DisplayFont, 18, Color.white, FontStyle.Bold);
            comidaTexto.alignment = TextAnchor.MiddleCenter;
            Storybook.Stamp(comidaTexto);
            PintarSignos();
            ArmarNombre();
            hud.transform.SetAsLastSibling();
            PaintVista();
        }

        void PaintVista()
        {
            movimientos?.Marcar(vista == Vista.Planta, vista == Vista.Inclinacion, VueloDeCamara.Activo);
            PintarBoton(editarButton, editandoMapa);
            if (paleta != null && paleta.activeSelf != editandoMapa)
                paleta.SetActive(editandoMapa);
            MarcarHerramientas();
            if (streetLabels != null
                && streetLabels.gameObject.activeSelf != (vista == Vista.Planta))
                streetLabels.gameObject.SetActive(vista == Vista.Planta);
        }

        void AlternarEdicion()
        {
            if (editandoNombre) return;
            if (editandoMapa) DejarEdicion();
            else EntrarEdicion();
        }

        void EntrarEdicion()
        {
            editandoMapa = true;
            if (vista != Vista.Planta) SetVista(Vista.Planta);
            if (!dijoEditar)
            {
                dijoEditar = true;
                Note(LeyDelMonte.EditarMapa);
            }

            PaintVista();
        }

        void DejarEdicion()
        {
            editandoMapa = false;
            SoltarPieza();
            PaintVista();
        }

        void SoltarPieza()
        {
            if (piezaElegida != null) PonMarco(piezaElegida, false);
            piezaElegida = null;
        }

        void ArmarPaleta()
        {
            if (hud == null) return;
            var root = new GameObject("Paleta", typeof(RectTransform));
            root.transform.SetParent(hud.transform, false);
            var rect = (RectTransform)root.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            paleta = root;
            var marcas = new[]
            {
                Storybook.Marca.Casa,
                Storybook.Marca.Arbol,
                Storybook.Marca.Banco,
                Storybook.Marca.Farol,
                Storybook.Marca.Flor,
                Storybook.Marca.Casa,
                Storybook.Marca.Quitar,
                Storybook.Marca.Casa,
            };
            herramientaTipos = new[]
            {
                Pieza.Casa,
                Pieza.Arbol,
                Pieza.Banco,
                Pieza.Farol,
                Pieza.Flor,
                Pieza.Casona,
                Pieza.Quitar,
                Pieza.Colonial,
            };
            herramientaBoton = new Button[marcas.Length];
            for (int i = 0; i < marcas.Length; i++)
            {
                float x0 = 0.018f + i * 0.074f;
                int capturado = i;
                herramientaBoton[i] = Storybook.IconButton(
                    root.transform,
                    herramientaTipos[i].ToString(),
                    marcas[i],
                    new Vector2(x0, 0.17f),
                    new Vector2(x0 + 0.068f, 0.29f),
                    () => ElegirHerramienta(herramientaTipos[capturado]));
            }

            paleta.SetActive(false);
        }

        void ElegirHerramienta(Pieza tipo)
        {
            if (tipo == Pieza.Colonial && herramienta == Pieza.Colonial)
            {
                PiezaColonial[] piezas = PiezasDelCatalogo();
                int n = piezas != null ? piezas.Length : 0;
                if (n > 0)
                {
                    varianteColonial = (varianteColonial + 1) % n;
                    PiezaColonial pieza = piezas[varianteColonial];
                    if (pieza != null && !string.IsNullOrEmpty(pieza.id))
                        Note(pieza.id);
                }
            }
            else if (herramienta == tipo)
                SoltarPieza();
            else
            {
                herramienta = tipo;
                if (tipo == Pieza.Colonial)
                    varianteColonial = 0;
            }

            MarcarHerramientas();
        }

        void MarcarHerramientas()
        {
            if (herramientaBoton == null || herramientaTipos == null) return;
            for (int i = 0; i < herramientaBoton.Length && i < herramientaTipos.Length; i++)
                PintarBoton(herramientaBoton[i], editandoMapa && herramientaTipos[i] == herramienta);
        }

        static void PintarBoton(Button button, bool on)
        {
            if (button == null) return;
            Storybook.LeafColors(button, on);
            var motion = button.GetComponent<MenuButtonMotion>();
            if (motion != null) motion.Chosen = on;
        }

        void RestaurarPiezas()
        {
            if (piezasGuardadas.Count == 0 || RuntimeParent == null) return;
            Transform raiz = RaizElementos();
            if (raiz.childCount > 0) return;
            for (int i = 0; i < piezasGuardadas.Count; i++)
                Construir(i);
            PublicarFaroles(district != null ? district.Find("Faroles") : null);
        }

        Transform RaizElementos()
        {
            if (elementos != null) return elementos;
            var go = new GameObject("ElementosDelMapa");
            go.transform.SetParent(RuntimeParent, false);
            elementos = go.transform;
            return elementos;
        }

        // La casa entra a homes al construirse. El lote de la partida solo se anota
        // al poner, mover o quitar: si se anotara aquí, cada reapertura lo duplicaría.
        PiezaDeMapa Construir(int indice)
        {
            if (indice < 0 || indice >= piezasGuardadas.Count) return null;
            PiezaDato dato = piezasGuardadas[indice];
            if (dato.Tipo == Pieza.Quitar) return null;
            var go = new GameObject("Pieza " + dato.Tipo);
            go.transform.SetParent(RaizElementos(), false);
            var pieza = go.AddComponent<PiezaDeMapa>();
            pieza.Tipo = dato.Tipo;
            pieza.X = dato.X;
            pieza.Z = dato.Z;
            pieza.Yaw = dato.Yaw;
            pieza.Variante = dato.Variante;
            pieza.Nombre = dato.Nombre;
            pieza.Indice = indice;
            int salt = SalDePieza(dato.X, dato.Z);
            switch (dato.Tipo)
            {
                case Pieza.Casa:
                    AlzarCasa(go.transform, dato.X, dato.Z, salt, dato.Yaw);
                    break;
                case Pieza.Casona:
                    AlzarCasona(go.transform, dato.X, dato.Z, dato.Yaw, dato.Variante, dato.Nombre);
                    break;
                case Pieza.Edificio:
                    AlzarEdificio(go.transform, dato.X, dato.Z, dato.Yaw, dato.Variante, dato.Nombre);
                    break;
                case Pieza.Arbol:
                    AlzarArbol(go.transform, dato.X, dato.Z, dato.Variante != 0 ? dato.Variante : salt);
                    break;
                case Pieza.Banco:
                    AlzarBanco(go.transform, dato.X, dato.Z);
                    break;
                case Pieza.Farol:
                    AlzarFarol(go.transform, dato.X, dato.Z);
                    break;
                case Pieza.Flor:
                    if (!PlantarFlor(FloresDelAsset[Mathf.Abs(salt) % FloresDelAsset.Length],
                            dato.X, dato.Z, salt * 47f, go.transform, false))
                        FlorCaja(go.transform, dato.X, dato.Z, salt, false);
                    break;
                case Pieza.Colonial:
                    AlzarColonial(go.transform, dato.X, dato.Z, dato.Yaw, dato.Variante);
                    break;
            }

            RecentrarPieza(go.transform, dato.X, dato.Z);
            return pieza;
        }

        static void RecentrarPieza(Transform go, float x, float z)
        {
            if (go == null) return;
            Vector3 at = new Vector3(x, CajamarcaAltura.Sample(x, z), z);
            go.localPosition = at;
            for (int i = 0; i < go.childCount; i++)
                go.GetChild(i).localPosition -= at;
        }

        static int SalDePieza(float x, float z)
        {
            return Mathf.Abs(Mathf.RoundToInt(x * 10f) * 17 + Mathf.RoundToInt(z * 13f));
        }

        void GuardarPieza(Pieza tipo, float x, float z, float yaw, int variante, string nombre)
        {
            piezasGuardadas.Add(new PiezaDato
            {
                Tipo = tipo,
                X = x,
                Z = z,
                Yaw = yaw,
                Variante = variante,
                Nombre = nombre,
            });
            VolcarPiezas();
        }

        void AlzarCasa(Transform root, float x, float z, int salt, float yaw)
        {
            float faceX;
            float faceZ;
            if (Mathf.Abs(yaw) > 0.01f)
            {
                float rad = yaw * Mathf.Deg2Rad;
                faceX = Mathf.Sin(rad);
                faceZ = Mathf.Cos(rad);
            }
            else if (!StreetFace(x, z, out faceX, out faceZ))
            {
                faceX = 0f;
                faceZ = 1f;
            }

            var walls = new Pen();
            var roofs = new Pen();
            var doors = new Pen();
            var faces = new Pen();
            RaiseHouse(walls, roofs, doors, faces, x, z, faceX, faceZ, salt);
            var shader = Shader.Find("AYNI/Calle") ?? Shader.Find("Standard") ?? Shader.Find("Sprites/Default");
            Publish(walls, "CasaMuro", "Calle/muro_adobe", shader, root);
            Publish(doors, "CasaPuerta", "Calle/madera_oscura", shader, root);
            PublishTex(faces, "CasaFrente", FacadeTex(), shader, root);
            var outward = new Vector3(faceX, 0f, faceZ);
            if (outward.sqrMagnitude < 0.01f) outward = Vector3.forward;
            outward.Normalize();
            Vector3 right = Vector3.Cross(Vector3.up, outward);
            if (right.sqrMagnitude < 0.01f) right = Vector3.right;
            right.Normalize();
            float g = CajamarcaAltura.Sample(x, z);
            float alto = 5.2f;
            PonerTechoADosAguas(root, new Vector3(x, g + alto, z), right, 8.2f, 7.6f, 1.85f);
        }

        static readonly Color[] MuroCasona =
        {
            new Color(0.93f, 0.78f, 0.38f),
            new Color(0.96f, 0.90f, 0.72f),
            new Color(0.88f, 0.62f, 0.32f),
            new Color(0.94f, 0.84f, 0.62f),
            new Color(0.97f, 0.95f, 0.88f),
            new Color(0.90f, 0.58f, 0.42f),
            new Color(0.92f, 0.74f, 0.48f),
            new Color(0.86f, 0.70f, 0.42f),
        };

        struct MoldeCasona
        {
            public string Recurso;
            public float Ancho;
            public float Fondo;
            public float Alto;
            public float Rise;
            public float Puerta;
        }

        // Medidas del FBX en Assets/Resources/Calle/Casas. Puerta es el
        // desplazamiento en metros sobre la fachada, no una fracción.
        const int MoldeCatedral = 7;
        const int MoldeSanFrancisco = 8;

        static readonly MoldeCasona[] MoldesCasona =
        {
            new MoldeCasona { Recurso = "Calle/Casas/CasonaBalcon", Ancho = 10.40f, Fondo = 7.40f, Alto = 7.05f, Rise = 1.65f, Puerta = 0f },
            new MoldeCasona { Recurso = "Calle/Casas/CasonaDosBalcones", Ancho = 11.20f, Fondo = 7.60f, Alto = 7.15f, Rise = 1.72f, Puerta = 3.78f },
            new MoldeCasona { Recurso = "Calle/Casas/CasonaPortales", Ancho = 12.60f, Fondo = 8.00f, Alto = 7.40f, Rise = 1.80f, Puerta = -1.50f },
            new MoldeCasona { Recurso = "Calle/Casas/CasonaEsquina", Ancho = 9.20f, Fondo = 9.20f, Alto = 7.20f, Rise = 1.70f, Puerta = 2.10f },
            new MoldeCasona { Recurso = "Calle/Casas/CasonaCorrido", Ancho = 11.80f, Fondo = 7.50f, Alto = 7.20f, Rise = 1.70f, Puerta = 0f },
            new MoldeCasona { Recurso = "Calle/Casas/CasonaPortada", Ancho = 10.80f, Fondo = 7.50f, Alto = 7.30f, Rise = 1.68f, Puerta = 0f },
            new MoldeCasona { Recurso = "Calle/Casas/CasonaAjimez", Ancho = 10.40f, Fondo = 7.40f, Alto = 7.15f, Rise = 1.65f, Puerta = -2.80f },
            new MoldeCasona { Recurso = "Calle/Casas/Catedral", Ancho = 24.00f, Fondo = 13.20f, Alto = 14.60f, Rise = 2.05f, Puerta = 0f },
            new MoldeCasona { Recurso = "Calle/Casas/SanFrancisco", Ancho = 20.50f, Fondo = 12.40f, Alto = 16.20f, Rise = 2.15f, Puerta = 0f },
        };

        // Menos de 100 queda en abs % 4: las esquinas (3, 7, 11, 15) siguen en CasonaEsquina.
        // 100 o más es un molde nuevo: (abs - 100) % largo.
        static int IndiceMolde(int variante)
        {
            int k = Mathf.Abs(variante);
            if (k >= 100) return (k - 100) % MoldesCasona.Length;
            return k % 4;
        }

        static RecetaCasona RecetaDe(int variante)
        {
            int k = Mathf.Abs(variante);
            MoldeCasona molde = MoldesCasona[IndiceMolde(variante)];
            return new RecetaCasona
            {
                Ancho = molde.Ancho,
                Pisos = 2,
                Alto = molde.Alto,
                Fondo = molde.Fondo,
                Rise = molde.Rise,
                Balcones = 1,
                Puerta = molde.Puerta,
                Ventanas = 3,
                Muro = MuroCasona[(k / 4) % MuroCasona.Length],
                Zocalo = new Color(0.72f, 0.66f, 0.54f),
            };
        }

        bool PonerCasonaMolde(Transform root, float x, float z, float g, Quaternion rot, Vector3 outward, Vector3 right, RecetaCasona rec, int variante, string nombre)
        {
            MoldeCasona molde = MoldesCasona[IndiceMolde(variante)];
            var prefab = Resources.Load<GameObject>(molde.Recurso);
            if (prefab == null) return false;
            var casa = Instantiate(prefab, root);
            casa.name = string.IsNullOrEmpty(nombre) ? "Casona" : nombre;
            casa.transform.localPosition = new Vector3(x, g, z);
            casa.transform.localRotation = rot;
            casa.transform.localScale = Vector3.one;
            PintarCasona(casa, rec.Muro);
            if (!string.IsNullOrEmpty(nombre))
            {
                Vector3 puerta = new Vector3(x, g, z) + right * rec.Puerta + outward * (rec.Fondo * 0.5f + 1.05f);
                CasonasDelCamino.Registrar(puerta, nombre, rec.Muro, "Calle/Fachadas/colonial");
                casonas.Add(new Vector2(x, z));
            }

            return true;
        }

        void PintarCasona(GameObject casa, Color muro)
        {
            var renders = casa.GetComponentsInChildren<MeshRenderer>(true);
            for (int i = 0; i < renders.Length; i++)
            {
                var go = renders[i].gameObject;
                string n = go.name;
                if (n.IndexOf("Vidrio", System.StringComparison.Ordinal) >= 0)
                    PaintPart(go, null, new Color(0.12f, 0.16f, 0.18f));
                else if (n.IndexOf("Reja", System.StringComparison.Ordinal) >= 0)
                    PaintPart(go, null, new Color(0.10f, 0.09f, 0.08f));
                else if (n.IndexOf("Porton", System.StringComparison.Ordinal) >= 0)
                    PaintPart(go, null, new Color(0.14f, 0.32f, 0.20f));
                else if (n.IndexOf("Madera", System.StringComparison.Ordinal) >= 0)
                    PaintPart(go, "Calle/madera_oscura", Color.white);
                else if (n.IndexOf("Teja", System.StringComparison.Ordinal) >= 0)
                    PaintPart(go, "Calle/teja", Color.white);
                else if (n.IndexOf("Piedra", System.StringComparison.Ordinal) >= 0)
                    PaintPart(go, "Calle/piedra", Color.white);
                else if (n.IndexOf("Muro", System.StringComparison.Ordinal) >= 0)
                    PaintPart(go, "Calle/muro_adobe", muro);
            }
        }

        void AlzarCasona(Transform root, float x, float z, float yawDeg, int variante, string nombre)
        {
            var rec = RecetaDe(variante);
            bool principal = !string.IsNullOrEmpty(nombre);
            if (principal)
            {
                rec.Ancho = Mathf.Max(rec.Ancho, 11.2f);
                rec.Alto = Mathf.Max(rec.Alto, 7.1f);
                rec.Pisos = 2;
                rec.Balcones = 1;
            }

            float rad = yawDeg * Mathf.Deg2Rad;
            Vector3 outward = new Vector3(Mathf.Sin(rad), 0f, Mathf.Cos(rad));
            if (outward.sqrMagnitude < 0.0001f) outward = Vector3.forward;
            outward.Normalize();
            Vector3 right = Vector3.Cross(Vector3.up, outward);
            if (right.sqrMagnitude < 0.0001f) right = Vector3.right;
            right.Normalize();
            Quaternion rot = Quaternion.LookRotation(outward, Vector3.up);
            var madera = new Color(0.38f, 0.22f, 0.12f);
            float g = CajamarcaAltura.Sample(x, z) + PistaAlta + Peldaño;
            if (PonerCasonaMolde(root, x, z, g, rot, outward, right, rec, variante, nombre))
                return;
            Vector3 cara = new Vector3(x, g, z) - outward * 0.35f;
            Vector3 cuerpo = cara - outward * (rec.Fondo * 0.5f);

            BoxPart(root, "Cuerpo", cuerpo + Vector3.up * (rec.Alto * 0.5f),
                new Vector3(rec.Ancho, rec.Alto, rec.Fondo), "Calle/muro_adobe", rec.Muro).transform.rotation = rot;
            SinTope(BoxPart(root, "AdornoZocalo", cuerpo + Vector3.up * 0.36f,
                new Vector3(rec.Ancho + 0.1f, 0.72f, rec.Fondo + 0.1f), "Calle/piedra", rec.Zocalo), rot);
            SinTope(BoxPart(root, "AdornoCornisa", cara + Vector3.up * (rec.Alto - 0.16f),
                new Vector3(rec.Ancho + 0.4f, 0.2f, 0.48f), "Calle/piedra", Color.white), rot);
            SinTope(BoxPart(root, "AdornoPilastraA", cara - right * (rec.Ancho * 0.48f) + Vector3.up * (rec.Alto * 0.5f),
                new Vector3(0.28f, rec.Alto, 0.22f), "Calle/piedra", Color.white), rot);
            SinTope(BoxPart(root, "AdornoPilastraB", cara + right * (rec.Ancho * 0.48f) + Vector3.up * (rec.Alto * 0.5f),
                new Vector3(0.28f, rec.Alto, 0.22f), "Calle/piedra", Color.white), rot);

            PonerTechoADosAguas(root, cuerpo + Vector3.up * rec.Alto, right, rec.Ancho, rec.Fondo, rec.Rise);

            float puertaX = rec.Puerta;
            Vector3 puerta = cara + right * puertaX + outward * 0.08f + Vector3.up * 1.28f;
            float puertaW = principal ? 1.7f : 1.35f;
            BoxPart(root, "Puerta", puerta, new Vector3(puertaW, 2.5f, 0.12f),
                "Calle/madera_oscura", madera).transform.rotation = rot;
            if (principal)
            {
                SinTope(BoxPart(root, "AdornoPortada", puerta + Vector3.up * 0.15f,
                    new Vector3(puertaW + 0.7f, 3.05f, 0.18f), "Calle/piedra", Color.white), rot);
                CasonasDelCamino.Registrar(cara + outward * 1.15f, nombre, rec.Muro, "Calle/Fachadas/colonial");
                casonas.Add(new Vector2(x, z));
            }

            int ventanas = rec.Ventanas;
            float yWin = rec.Pisos == 1 ? rec.Alto * 0.58f : rec.Alto * 0.28f;
            float span = rec.Ancho * 0.72f;
            for (int i = 0; i < ventanas; i++)
            {
                float t = ventanas == 1 ? 0f : (i / (float)(ventanas - 1) - 0.5f) * 2f;
                float wx = t * span * 0.5f;
                if (Mathf.Abs(wx - puertaX) < 1.5f && rec.Pisos == 1) continue;
                VentanaColonial(root, cara + right * wx + Vector3.up * yWin, rot, outward, 0.85f, 1.15f);
            }

            if (rec.Pisos == 2)
            {
                float y2 = rec.Alto * 0.72f;
                int n2 = 1 + (variante % 3);
                for (int i = 0; i < n2; i++)
                {
                    float t = n2 == 1 ? 0f : (i / (float)(n2 - 1) - 0.5f) * 2f;
                    VentanaColonial(root, cara + right * (t * span * 0.42f) + Vector3.up * y2, rot, outward, 0.78f, 1.05f);
                }
            }

            if (rec.Balcones > 0 && rec.Pisos == 2)
            {
                if (rec.Balcones == 1)
                    BalconColonial(root, cara + Vector3.up * (rec.Alto * 0.62f), rot, outward, right, rec.Ancho * 0.42f);
                else
                {
                    float side = rec.Ancho * 0.22f;
                    BalconColonial(root, cara - right * side + Vector3.up * (rec.Alto * 0.62f), rot, outward, right, 2.15f);
                    if (rec.Balcones >= 2)
                        BalconColonial(root, cara + right * side + Vector3.up * (rec.Alto * 0.62f), rot, outward, right, 2.15f);
                }
            }
        }

        void VentanaColonial(Transform root, Vector3 at, Quaternion rot, Vector3 outward, float w, float h)
        {
            var marco = new Color(0.92f, 0.90f, 0.84f);
            var vidrio = new Color(0.18f, 0.22f, 0.26f);
            SinTope(BoxPart(root, "AdornoMarco", at + outward * 0.04f, new Vector3(w + 0.16f, h + 0.16f, 0.1f),
                "Calle/piedra", marco), rot);
            SinTope(BoxPart(root, "AdornoVidrio", at + outward * 0.02f, new Vector3(w, h, 0.08f),
                null, vidrio), rot);
        }

        void BalconColonial(Transform root, Vector3 at, Quaternion rot, Vector3 outward, Vector3 right, float ancho)
        {
            var madera = new Color(0.36f, 0.20f, 0.11f);
            Vector3 piso = at + outward * 0.58f;
            SinTope(BoxPart(root, "AdornoBalcon", piso, new Vector3(ancho, 0.1f, 1.05f),
                "Calle/madera_oscura", madera), rot);
            SinTope(BoxPart(root, "AdornoBalcon", piso + outward * 0.46f + Vector3.up * 0.42f,
                new Vector3(ancho, 0.62f, 0.07f), "Calle/madera_oscura", madera), rot);
            SinTope(BoxPart(root, "AdornoBalcon", piso + right * (ancho * 0.48f) + Vector3.up * 0.36f,
                new Vector3(0.07f, 0.7f, 1.0f), "Calle/madera_oscura", madera), rot);
            SinTope(BoxPart(root, "AdornoBalcon", piso - right * (ancho * 0.48f) + Vector3.up * 0.36f,
                new Vector3(0.07f, 0.7f, 1.0f), "Calle/madera_oscura", madera), rot);
        }

        void SinTope(GameObject go, Quaternion rot)
        {
            if (go == null) return;
            go.transform.rotation = rot;
            var col = go.GetComponent<Collider>();
            if (col != null) Kill(col);
        }

        void AlzarEdificio(Transform root, float x, float z, float yawDeg, int variante, string nombre)
        {
            float rad = yawDeg * Mathf.Deg2Rad;
            Vector3 outward = new Vector3(Mathf.Sin(rad), 0f, Mathf.Cos(rad));
            if (outward.sqrMagnitude < 0.0001f) outward = Vector3.forward;
            outward.Normalize();
            Vector3 right = Vector3.Cross(Vector3.up, outward);
            if (right.sqrMagnitude < 0.0001f) right = Vector3.right;
            right.Normalize();
            Quaternion rot = Quaternion.LookRotation(outward, Vector3.up);
            float g = CajamarcaAltura.Sample(x, z);
            if (variante == 5)
            {
                BoxPart(root, "Palo", new Vector3(x, g + 1.35f, z), new Vector3(0.14f, 2.7f, 0.14f),
                    null, new Color(0.38f, 0.24f, 0.12f));
                float ancho = Mathf.Clamp(0.14f * (nombre != null ? nombre.Length : 8), 2.2f, 5.6f);
                BoxPart(root, "Tabla", new Vector3(x, g + 2.55f, z), new Vector3(ancho, 0.5f, 0.08f),
                    null, new Color(0.18f, 0.42f, 0.28f));
                var letrero = new GameObject("Texto");
                letrero.transform.SetParent(root, false);
                letrero.transform.localPosition = new Vector3(x, g + 2.55f, z);
                var mesh = letrero.AddComponent<TextMesh>();
                mesh.text = string.IsNullOrEmpty(nombre) ? "Paradero" : nombre;
                mesh.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                mesh.characterSize = 0.055f;
                mesh.fontSize = 48;
                mesh.anchor = TextAnchor.MiddleCenter;
                mesh.color = new Color(0.97f, 0.94f, 0.82f);
                letrero.AddComponent<Billboard>();
                return;
            }

            Vector3 size = variante == 0 ? new Vector3(22f, 8.4f, 10f)
                : variante == 1 ? new Vector3(18f, 7.2f, 9f)
                : variante == 2 ? new Vector3(26f, 6.2f, 8f)
                : variante == 3 ? new Vector3(10f, 3.4f, 6f)
                : new Vector3(12f, 4.4f, 7f);
            var muro = variante <= 2
                ? new Color(0.93f, 0.88f, 0.74f)
                : new Color(0.90f, 0.78f, 0.52f);
            Vector3 centro = new Vector3(x, g + size.y * 0.5f, z);
            BoxPart(root, "Pabellon", centro, size, "Calle/muro_adobe", muro).transform.rotation = rot;
            PonerTechoADosAguas(root, new Vector3(x, g + size.y, z), right, size.x, size.z, Mathf.Max(1.4f, size.z * 0.22f));
            if (!string.IsNullOrEmpty(nombre))
            {
                var letrero = new GameObject("Nombre");
                letrero.transform.SetParent(root, false);
                letrero.transform.localPosition = new Vector3(x, g + size.y + 1.6f, z);
                var mesh = letrero.AddComponent<TextMesh>();
                mesh.text = nombre;
                mesh.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                mesh.characterSize = 0.11f;
                mesh.fontSize = 48;
                mesh.anchor = TextAnchor.MiddleCenter;
                mesh.alignment = TextAlignment.Center;
                mesh.color = new Color(0.97f, 0.94f, 0.82f);
                letrero.AddComponent<Billboard>();
            }
        }

        void PonerTechoADosAguas(Transform parent, Vector3 centro, Vector3 along, float largoCalle, float fondo, float rise)
        {
            if (along.sqrMagnitude < 0.0001f) along = Vector3.right;
            along.Normalize();
            var go = new GameObject("TechoAguas");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = centro;
            go.transform.localRotation = Quaternion.LookRotation(along, Vector3.up);
            var filter = go.AddComponent<MeshFilter>();
            filter.sharedMesh = GableRoof(fondo + 1.5f, largoCalle + 1.2f, rise);
            go.AddComponent<MeshRenderer>();
            PaintPart(go, "Calle/teja", Color.white);
        }

        void AlzarArbol(Transform root, float x, float z, int salt)
        {
            var kinds = TreeKinds();
            string resource = kinds[Mathf.Abs(salt) % kinds.Length];
            if (Resources.Load<GameObject>(resource) != null)
            {
                PlantTree(resource, x, z, 9f + (Mathf.Abs(salt) % 3) * 1.6f, salt * 28f,
                    CajamarcaAltura.Sample(x, z), root);
                return;
            }

            float g = CajamarcaAltura.Sample(x, z);
            BoxPart(root, "Arbol", new Vector3(x, g + 1.6f, z), new Vector3(0.35f, 3.2f, 0.35f), null, new Color(0.36f, 0.24f, 0.12f));
            BoxPart(root, "Copa", new Vector3(x, g + 4.1f, z), new Vector3(2.2f, 2.4f, 2.2f), null, new Color(0.28f, 0.46f, 0.22f));
        }

        void AlzarBanco(Transform root, float x, float z)
        {
            float g = CajamarcaAltura.Sample(x, z);
            float yaw = 0f;
            if (StreetFace(x, z, out float faceX, out float faceZ))
                yaw = Mathf.Atan2(faceX, faceZ) * Mathf.Rad2Deg;
            if (PlantProp(root, "Banco", x, z, g, 1.25f, yaw,
                "Calle/Props/bench", "Calle/Props/bench-wood", "Calle/Props/bench-park"))
                return;

            var madera = new Color(0.45f, 0.28f, 0.14f);
            var face = Vector3.forward;
            if (StreetFace(x, z, out faceX, out faceZ))
                face = new Vector3(faceX, 0f, faceZ);
            if (face.sqrMagnitude < 0.01f) face = Vector3.forward;
            face.Normalize();
            var rot = Quaternion.LookRotation(face, Vector3.up);
            var seat = BoxPart(root, "Banco", new Vector3(x, g + 0.42f, z), new Vector3(1.25f, 0.1f, 0.46f), null, madera);
            seat.transform.localRotation = rot;
            var back = BoxPart(root, "Banco", new Vector3(x, g + 0.78f, z) - face * 0.18f, new Vector3(1.25f, 0.72f, 0.08f), null, madera);
            back.transform.localRotation = rot;
        }

        static CatalogoColonial catalogoColonial;
        static bool catalogoColonialListo;

        [System.Serializable]
        sealed class PiezaColonial
        {
            public string id;
            public string recurso;
            public float alto;
            public bool adorno;
        }

        [System.Serializable]
        sealed class CatalogoColonial
        {
            public PiezaColonial[] piezas;
        }

        static PiezaColonial[] PiezasDelCatalogo()
        {
            if (catalogoColonialListo)
                return catalogoColonial != null ? catalogoColonial.piezas : null;
            catalogoColonialListo = true;
            var asset = Resources.Load<TextAsset>("Calle/Colonial/catalogo");
            if (asset != null)
                catalogoColonial = JsonUtility.FromJson<CatalogoColonial>(asset.text);
            return catalogoColonial != null ? catalogoColonial.piezas : null;
        }

        // Adorno no lleva caja: un sólido taparía el vano. Muro sí, si mide 0.55 m o más.
        void AlzarColonial(Transform root, float x, float z, float yaw, int variante)
        {
            if (root == null) return;
            PiezaColonial[] piezas = PiezasDelCatalogo();
            if (piezas == null || piezas.Length == 0) return;
            int indice = Mathf.Abs(variante) % piezas.Length;
            PiezaColonial pieza = piezas[indice];
            if (pieza == null || string.IsNullOrEmpty(pieza.recurso)) return;
            var prefab = Resources.Load<GameObject>(pieza.recurso);
            if (prefab == null) return;
            var go = Instantiate(prefab, root);
            string nombre = pieza.adorno ? "Adorno" : "Muro";
            go.name = nombre;
            go.transform.localScale = Vector3.one;
            go.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
            go.transform.localPosition = new Vector3(x, CajamarcaAltura.Sample(x, z), z);
            var filtros = go.GetComponentsInChildren<MeshFilter>(true);
            for (int i = 0; i < filtros.Length; i++)
            {
                if (filtros[i] != null)
                    filtros[i].gameObject.name = nombre;
            }
        }

        void PonerNueva(Pieza tipo, float x, float z)
        {
            if (tipo == Pieza.Quitar) return;
            if (tipo == Pieza.Casona || tipo == Pieza.Edificio)
            {
                if (EnFuente(x, z))
                {
                    Note(LeyDelMonte.PiezaNoCabe);
                    return;
                }

                if (!CabePieza(tipo, x, z, null))
                {
                    Note(LeyDelMonte.PiezaNoCabe);
                    return;
                }

                float yaw = 0f;
                if (StreetFace(x, z, out float fx, out float fz))
                    yaw = Mathf.Atan2(fx, fz) * Mathf.Rad2Deg;
                int variante = SalDePieza(x, z);
                GuardarPieza(tipo, x, z, yaw, variante, null);
                var hechaCasona = Construir(piezasGuardadas.Count - 1);
                if (hechaCasona != null) SolidoDelCamino.Armar(hechaCasona.transform);
                Note(LeyDelMonte.PiezaPuesta);
                return;
            }

            if (tipo == Pieza.Colonial)
            {
                if (EnFuente(x, z))
                {
                    Note(LeyDelMonte.PiezaNoCabe);
                    return;
                }

                if (!CabePieza(tipo, x, z, null))
                {
                    Note(LeyDelMonte.PiezaNoCabe);
                    return;
                }

                float yaw = 0f;
                if (StreetFace(x, z, out float faceX, out float faceZ))
                    yaw = Mathf.Atan2(faceX, faceZ) * Mathf.Rad2Deg;
                GuardarPieza(tipo, x, z, yaw, varianteColonial, null);
                var hechaColonial = Construir(piezasGuardadas.Count - 1);
                if (hechaColonial != null) SolidoDelCamino.Armar(hechaColonial.transform);
                Note(LeyDelMonte.PiezaPuesta);
                return;
            }

            if (tipo == Pieza.Casa)
            {
                if (EnJardin(x, z) || OnPlaza(x, z))
                {
                    Note(LeyDelMonte.LoteCerrado);
                    return;
                }

                if (!TryLot(x, z, out float ox, out float oz, out _, out _)
                    || EnJardin(ox, oz) || OnPlaza(ox, oz))
                {
                    Note(LeyDelMonte.LoteCerrado);
                    return;
                }

                piezasGuardadas.Add(new PiezaDato { Tipo = Pieza.Casa, X = ox, Z = oz });
                LotesPuestos.Add(new Vector2(ox, oz));
                var casa = Construir(piezasGuardadas.Count - 1);
                if (casa != null) SolidoDelCamino.Armar(casa.transform);
                Note(editandoMapa ? LeyDelMonte.PiezaPuesta : LeyDelMonte.LotePuesto);
                return;
            }

            if (!CabePieza(tipo, x, z, null))
            {
                Note(LeyDelMonte.PiezaNoCabe);
                return;
            }

            piezasGuardadas.Add(new PiezaDato { Tipo = tipo, X = x, Z = z });
            var hecha = Construir(piezasGuardadas.Count - 1);
            if (hecha != null) SolidoDelCamino.Armar(hecha.transform);
            if (tipo == Pieza.Farol)
                PublicarFaroles(district != null ? district.Find("Faroles") : null);
            Note(LeyDelMonte.PiezaPuesta);
        }

        void MoverPieza(PiezaDeMapa pieza, float x, float z)
        {
            if (pieza == null || pieza.Indice < 0 || pieza.Indice >= piezasGuardadas.Count) return;
            PiezaDato viejo = piezasGuardadas[pieza.Indice];
            if (pieza.Tipo == Pieza.Casona || pieza.Tipo == Pieza.Edificio || pieza.Tipo == Pieza.Colonial)
            {
                if (EnFuente(x, z) || !CabePieza(pieza.Tipo, x, z, pieza))
                {
                    Note(LeyDelMonte.PiezaNoCabe);
                    return;
                }

                float yaw = viejo.Yaw;
                if (StreetFace(x, z, out float fx, out float fz))
                    yaw = Mathf.Atan2(fx, fz) * Mathf.Rad2Deg;
                piezasGuardadas[pieza.Indice] = new PiezaDato
                {
                    Tipo = pieza.Tipo,
                    X = x,
                    Z = z,
                    Yaw = yaw,
                    Variante = viejo.Variante,
                    Nombre = viejo.Nombre,
                };
                Reemplazar(pieza, x, z);
                return;
            }

            if (pieza.Tipo == Pieza.Casa)
            {
                if (EnJardin(x, z) || OnPlaza(x, z))
                {
                    Note(LeyDelMonte.LoteCerrado);
                    return;
                }

                Liberar(pieza);
                if (!TryLot(x, z, out float ox, out float oz, out _, out _)
                    || EnJardin(ox, oz) || OnPlaza(ox, oz))
                {
                    TomarLote(viejo.X, viejo.Z);
                    Note(LeyDelMonte.LoteCerrado);
                    return;
                }

                Reemplazar(pieza, ox, oz);
                return;
            }

            if (!CabePieza(pieza.Tipo, x, z, pieza))
            {
                Note(LeyDelMonte.PiezaNoCabe);
                return;
            }

            Reemplazar(pieza, x, z);
        }

        void Reemplazar(PiezaDeMapa pieza, float x, float z)
        {
            int indice = pieza.Indice;
            bool farol = pieza.Tipo == Pieza.Farol;
            PiezaDato previo = piezasGuardadas[indice];
            piezasGuardadas[indice] = new PiezaDato
            {
                Tipo = pieza.Tipo,
                X = x,
                Z = z,
                Yaw = previo.Yaw,
                Variante = previo.Variante,
                Nombre = previo.Nombre,
            };
            if (pieza.Tipo == Pieza.Casa) LotesPuestos.Add(new Vector2(x, z));
            pieza.Indice = -1;
            if (piezaElegida == pieza) piezaElegida = null;
            Kill(pieza.gameObject);
            var nueva = Construir(indice);
            if (nueva != null)
            {
                SolidoDelCamino.Armar(nueva.transform);
                piezaElegida = nueva;
                PonMarco(nueva, true);
            }

            if (farol)
                PublicarFaroles(district != null ? district.Find("Faroles") : null);
            VolcarPiezas();
            Note(LeyDelMonte.PiezaMovida);
        }

        void QuitarPieza(PiezaDeMapa pieza)
        {
            if (pieza == null || pieza.Indice < 0 || pieza.Indice >= piezasGuardadas.Count)
            {
                Note(LeyDelMonte.PiezaNada);
                return;
            }

            int indice = pieza.Indice;
            bool farol = pieza.Tipo == Pieza.Farol;
            if (pieza.Tipo == Pieza.Casa) Liberar(pieza);
            pieza.Indice = -1;
            if (piezaElegida == pieza) piezaElegida = null;
            Kill(pieza.gameObject);
            int last = piezasGuardadas.Count - 1;
            if (indice != last)
            {
                piezasGuardadas[indice] = piezasGuardadas[last];
                var movida = BuscarPieza(last);
                if (movida != null) movida.Indice = indice;
            }

            piezasGuardadas.RemoveAt(last);
            if (farol)
                PublicarFaroles(district != null ? district.Find("Faroles") : null);
            VolcarPiezas();
            Note(LeyDelMonte.PiezaQuitada);
        }

        void Liberar(PiezaDeMapa pieza)
        {
            if (pieza == null) return;
            QuitarCerca(homes, pieza.X, pieza.Z);
            roofCells.Remove(WalkKey(Mathf.FloorToInt(pieza.X / Lot), Mathf.FloorToInt(pieza.Z / Lot)));
            QuitarCerca(LotesPuestos, pieza.X, pieza.Z);
        }

        static void QuitarCerca(List<Vector2> puntos, float x, float z)
        {
            for (int i = puntos.Count - 1; i >= 0; i--)
            {
                float dx = puntos[i].x - x;
                float dz = puntos[i].y - z;
                if (dx * dx + dz * dz < 1f) puntos.RemoveAt(i);
            }
        }

        void TomarLote(float x, float z)
        {
            homes.Add(new Vector2(x, z));
            roofCells.Add(WalkKey(Mathf.FloorToInt(x / Lot), Mathf.FloorToInt(z / Lot)));
            LotesPuestos.Add(new Vector2(x, z));
        }

        bool EnFuente(float x, float z)
        {
            float dx = x - fountainX;
            float dz = z - fountainZ;
            return dx * dx + dz * dz < BocaFuente * BocaFuente;
        }

        bool CabePieza(Pieza tipo, float x, float z, PiezaDeMapa self)
        {
            if (tipo == Pieza.Casa) return true;
            if (EnFuente(x, z)) return false;
            bool plaza = EnRecintoPlaza(x, z) || OnPlaza(x, z);
            if (!plaza && tipo != Pieza.Casona && tipo != Pieza.Edificio && tipo != Pieza.Colonial && OnStreet(x, z)) return false;
            float radio = tipo == Pieza.Casona || tipo == Pieza.Edificio ? 6.2f
                : tipo == Pieza.Colonial ? 0.45f
                : 2.3f;
            float limite = radio * radio;
            for (int i = 0; i < piezasGuardadas.Count; i++)
            {
                if (self != null && self.Indice == i) continue;
                float dx = piezasGuardadas[i].X - x;
                float dz = piezasGuardadas[i].Z - z;
                if (dx * dx + dz * dz < limite) return false;
            }

            return true;
        }

        void ElegirPieza(PiezaDeMapa pieza)
        {
            if (pieza == null || pieza.Indice < 0) return;
            if (piezaElegida != null && piezaElegida != pieza) PonMarco(piezaElegida, false);
            piezaElegida = pieza;
            PonMarco(pieza, true);
            Note(LeyDelMonte.PiezaElegida);
        }

        PiezaDeMapa PiezaBajoCursor()
        {
            if (elementos == null || cam == null) return null;
            var todas = elementos.GetComponentsInChildren<PiezaDeMapa>(false);
            Vector2 mouse = Input.mousePosition;
            float mejor = float.PositiveInfinity;
            PiezaDeMapa ganadora = null;
            for (int i = 0; i < todas.Length; i++)
            {
                var pieza = todas[i];
                if (pieza == null || pieza.Indice < 0) continue;
                float dist = mejor;
                if (!Cubre(pieza.transform, mouse, ref dist)) continue;
                mejor = dist;
                ganadora = pieza;
            }

            return ganadora;
        }

        PiezaDeMapa BuscarPieza(int indice)
        {
            if (elementos == null) return null;
            var todas = elementos.GetComponentsInChildren<PiezaDeMapa>(true);
            for (int i = 0; i < todas.Length; i++)
            {
                if (todas[i] != null && todas[i].Indice == indice) return todas[i];
            }

            return null;
        }

        void PonMarco(PiezaDeMapa pieza, bool on)
        {
            if (pieza == null) return;
            Transform marco = pieza.transform.Find("Marco");
            if (!on)
            {
                if (marco != null) marco.gameObject.SetActive(false);
                return;
            }

            if (marco == null)
            {
                var placa = BoxPart(pieza.transform, "Marco", Vector3.zero, Vector3.one, null, new Color(0.95f, 0.78f, 0.22f));
                marco = placa.transform;
            }

            marco.gameObject.SetActive(true);
            EncajarMarco(pieza, marco);
        }

        static void EncajarMarco(PiezaDeMapa pieza, Transform marco)
        {
            var renders = pieza.GetComponentsInChildren<Renderer>();
            bool hay = false;
            Bounds caja = new Bounds(pieza.transform.position, Vector3.one);
            for (int i = 0; i < renders.Length; i++)
            {
                if (renders[i] == null || !renders[i].enabled) continue;
                if (renders[i].gameObject.name == "Marco") continue;
                if (!hay)
                {
                    caja = renders[i].bounds;
                    hay = true;
                }
                else caja.Encapsulate(renders[i].bounds);
            }

            if (!hay) return;
            Vector3 centro = pieza.transform.InverseTransformPoint(caja.center);
            Vector3 suelo = pieza.transform.InverseTransformPoint(new Vector3(caja.center.x, caja.min.y, caja.center.z));
            Vector3 lossy = pieza.transform.lossyScale;
            float sx = Mathf.Max(0.001f, Mathf.Abs(lossy.x));
            float sy = Mathf.Max(0.001f, Mathf.Abs(lossy.y));
            float sz = Mathf.Max(0.001f, Mathf.Abs(lossy.z));
            float ancho = Mathf.Clamp(caja.size.x + 0.45f, 1.2f, 14f);
            float largo = Mathf.Clamp(caja.size.z + 0.45f, 1.2f, 14f);
            marco.localRotation = Quaternion.identity;
            marco.localPosition = new Vector3(centro.x, suelo.y + 0.08f, centro.z);
            marco.localScale = new Vector3(ancho / sx, 0.08f / sy, largo / sz);
        }

        // Viñeta de cómic, arriba a la derecha. El texto queda en tinta sobre el papel.
        void ArmarVineta()
        {
            var borde = Storybook.Plate(hud.transform, "Vineta",
                new Vector2(0.58f, 0.68f), new Vector2(0.988f, 0.875f),
                new Color(0.08f, 0.05f, 0.04f, 1f));
            Storybook.Plate(borde.transform, "Papel",
                new Vector2(0.025f, 0.07f), new Vector2(0.975f, 0.95f),
                new Color(0.98f, 0.95f, 0.86f, 1f));
            var cola = Storybook.Plate(borde.transform, "Cola",
                new Vector2(0.04f, -0.01f), new Vector2(0.14f, 0.14f),
                new Color(0.08f, 0.05f, 0.04f, 1f));
            cola.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 36f);
            line = AddText(borde.transform, "Linea", new Vector2(0.05f, 0.12f), new Vector2(0.96f, 0.92f));
            Storybook.DressText(line, LeyDelMonte.StreetWalk,
                Storybook.DisplayFont, 36, new Color(0.14f, 0.08f, 0.05f), FontStyle.Bold);
            line.alignment = TextAnchor.UpperLeft;
            line.lineSpacing = 0.92f;
            lineFade = TituloFugaz.Begin(borde.gameObject, 7.5f);
        }

        void Note(string text)
        {
            if (line == null || line.text == text) return;
            line.text = text;
            lineFade?.ShowAgain(7.5f);
        }

        Text Signo(Transform parent, string name, Storybook.Marca marca, Color tinta, Vector2 min, Vector2 max)
        {
            var plate = Storybook.Plate(parent, name, min, max, new Color(0.10f, 0.06f, 0.03f, 0.72f));
            var iconGo = new GameObject("Icono");
            iconGo.transform.SetParent(plate.transform, false);
            var iconRect = iconGo.AddComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0.06f, 0.14f);
            iconRect.anchorMax = new Vector2(0.36f, 0.86f);
            iconRect.offsetMin = Vector2.zero;
            iconRect.offsetMax = Vector2.zero;
            var image = iconGo.AddComponent<Image>();
            image.sprite = Storybook.Icono(marca);
            image.color = tinta;
            image.preserveAspect = true;
            image.raycastTarget = false;
            var texto = AddText(plate.transform, "Porcentaje", new Vector2(0.36f, 0.08f), new Vector2(0.98f, 0.92f));
            Storybook.DressText(texto, "100%", Storybook.DisplayFont, 22, Color.white, FontStyle.Bold);
            texto.alignment = TextAnchor.MiddleLeft;
            Storybook.Stamp(texto);
            return texto;
        }

        void PintarSignos()
        {
            if (vidaTexto != null)
            {
                vidaTexto.text = Mathf.RoundToInt(vida) + "%";
                vidaTexto.color = vida > 20f ? Color.white : new Color(1f, 0.55f, 0.42f);
            }

            if (auraTexto != null)
                auraTexto.text = Mathf.RoundToInt(auraFarmeo) + "%";
            if (atractivoTexto != null)
                atractivoTexto.text = Mathf.RoundToInt(atractivo) + "%";
            if (comidaTexto != null)
                comidaTexto.text = comidasDia + "/3   S/ " + plata;
        }

        static Text AddText(Transform parent, string name, Vector2 min, Vector2 max)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return go.AddComponent<Text>();
        }

        void BuildDistrict()
        {
            var shader = Shader.Find("AYNI/Calle") ?? Shader.Find("Standard") ?? Shader.Find("Sprites/Default");
            PlacePlan(shader);
            if (plan != null)
            {
                BindPlanDoor();
                BakeWalk();
                FindMainPlaza();
                blockRadius = 6f;
                BuildPlanFountain(shader);
                RenderSettings.fogStartDistance = 1600f;
                RenderSettings.fogEndDistance = 3600f;
                PlaceCrate();
                DressPlaza();
                BuildStreetLabels();
                return;
            }

            var pens = new Pen[(int)Skin.Count];
            for (int i = 0; i < pens.Length; i++)
                pens[i] = new Pen();

            Box(pens[(int)Skin.Tierra], Vector3.zero + Vector3.down * 0.2f, new Vector3(560f, 0.4f, 560f), Quaternion.identity, Color.white);
            Diamond(pens[(int)Skin.Plaza], PlazaR, 0.10f, Losa);
            Ring(pens[(int)Skin.Empedrado], PlazaR, StreetOuter, 0.07f, Losa);
            Box(pens[(int)Skin.Empedrado], new Vector3(0f, 0.07f, 26f), new Vector3(4.4f, 0.1f, 16f), Quaternion.identity, Losa);
            Box(pens[(int)Skin.Empedrado], new Vector3(0f, 0.07f, -28.5f), new Vector3(4.4f, 0.1f, 21f), Quaternion.identity, Losa);

            Fountain(pens);
            sanFranciscoDoor = Church(pens, new Vector3(1f, 0f, -1f), false);
            Church(pens, new Vector3(-1f, 0f, 1f), true);
            Houses(pens, new Vector3(1f, 0f, 1f));
            Houses(pens, new Vector3(-1f, 0f, -1f));
            Belen(pens);
            Hill(pens[(int)Skin.Tierra], new Vector3(34f, 0f, 16f), 13f, 8.5f, Cerro);
            Hill(pens[(int)Skin.Tierra], new Vector3(40f, 0f, 22f), 9f, 6f, Cerro * 0.85f);
            Box(pens[(int)Skin.Piedra], new Vector3(34f, 8.7f, 16f), new Vector3(1.4f, 0.7f, 1.4f), Quaternion.identity, Losa);
            Box(pens[(int)Skin.Piedra], new Vector3(34f, 9.2f, 16f), new Vector3(0.7f, 0.45f, 0.7f), Quaternion.identity, Losa);

            Candles(pens, new Vector3(1f, 0f, -1f));

            string[] names =
            {
                "Tierra", "Empedrado", "Plaza", "Adobe", "Piedra", "Teja", "Pintura", "PortadaCatedral", "PortadaFrancisco",
            };
            for (int i = 0; i < pens.Length; i++)
                Publish(pens[i], names[i], SkinTex[i], shader);
            hasDoor = true;
            PlaceCrate();
        }

        void PlaceCrate()
        {
            if (district == null || district.Find("CajaMadera") != null) return;
            var prefab = Resources.Load<GameObject>("Madera/CajaMadera");
            if (prefab == null) return;

            var crate = Instantiate(prefab, district);
            crate.name = "CajaMadera";
            foreach (var col in crate.GetComponentsInChildren<Collider>(true))
                Kill(col);

            var tex = Resources.Load<Texture2D>("Madera/caja_diff");
            var wood = Shader.Find("Standard") ?? Shader.Find("AYNI/Calle") ?? Shader.Find("Sprites/Default");
            if (wood != null)
            {
                var mat = new Material(wood);
                if (tex != null) mat.mainTexture = tex;
                mat.color = Color.white;
                foreach (var renderer in crate.GetComponentsInChildren<Renderer>(true))
                {
                    renderer.sharedMaterial = mat;
                    renderer.shadowCastingMode = ShadowCastingMode.Off;
                    renderer.receiveShadows = false;
                }
            }

            crate.transform.localRotation = Quaternion.Euler(0f, 24f, 0f);
            if (plazaFound)
            {
                float a = -50f * Mathf.Deg2Rad;
                crate.transform.localPosition = new Vector3(
                    fountainX + Mathf.Cos(a) * 10.2f,
                    0f,
                    fountainZ + Mathf.Sin(a) * 10.2f);
            }
            else
                crate.transform.localPosition = new Vector3(9f, 0f, -8f);
            crate.transform.localScale = Vector3.one;
            float height = CrateSpan(crate, out float minY);
            float mul = 3.5f / Mathf.Max(0.05f, height);
            crate.transform.localScale = Vector3.one * mul;
            CrateSpan(crate, out minY);
            crate.transform.position += new Vector3(0f, 0.16f - minY, 0f);
        }

        static float CrateSpan(GameObject crate, out float minY)
        {
            bool any = false;
            var bounds = new Bounds(crate.transform.position, Vector3.zero);
            foreach (var renderer in crate.GetComponentsInChildren<Renderer>(true))
            {
                if (!any)
                {
                    bounds = renderer.bounds;
                    any = true;
                }
                else
                {
                    bounds.Encapsulate(renderer.bounds);
                }
            }

            minY = any ? bounds.min.y : crate.transform.position.y;
            return any ? bounds.size.y : 0.8f;
        }

        void DressPlaza()
        {
            if (!plazaFound)
            {
                DressPlazaCircle();
                return;
            }

            DressFormalPlaza();
        }

        void DressPlazaCircle()
        {
            var grass = new Pen();
            var shader = Shader.Find("AYNI/Calle") ?? Shader.Find("Standard") ?? Shader.Find("Sprites/Default");
            var lawn = new Color(0.36f, 0.58f, 0.28f);
            var deep = new Color(0.26f, 0.46f, 0.22f);
            for (int i = 0; i < 24; i++)
            {
                float a = i * 15f * Mathf.Deg2Rad;
                float radius = 18f + (i % 3) * 8f;
                float x = Mathf.Cos(a) * radius;
                float z = Mathf.Sin(a) * radius;
                if (Mathf.Abs(x) < 7f || Mathf.Abs(z) < 7f) continue;
                if (x * x + z * z < 11f * 11f) continue;
                if (!OnStreet(x, z)) continue;
                var tint = (i % 2) == 0 ? lawn : deep;
                StampCell(grass, x, z, 13f, 10f, 0.16f, tint);
            }

            Publish(grass, "Cesped", null, shader);
            PlantRing(18);
        }

        void DressFormalPlaza()
        {
            // Los senderos viejos iban a una altura fija y con la misma losa
            // que el plano, así que no se leían. VestirPlaza los separa.
            // Si el plano ya trae la arboleda del asset, no se planta otra encima.
            Transform grove = district != null ? district.Find("Arboles") : null;
            if (grove != null && grove.childCount > 0) return;
            PlantFormalTrees();
        }

        void PlantFormalTrees()
        {
            float margin = PlazaWalk + 4f;
            const float step = 15f;
            int n = 0;
            for (float u = -plazaHalfU + margin; u <= plazaHalfU - margin; u += step)
            {
                Vector2 a = PlazaWorld(u, -plazaHalfV + margin);
                TryTree(a.x, a.y, n++);
                Vector2 b = PlazaWorld(u, plazaHalfV - margin);
                TryTree(b.x, b.y, n++);
            }

            for (float v = -plazaHalfV + margin + step * 0.5f; v <= plazaHalfV - margin; v += step)
            {
                Vector2 a = PlazaWorld(-plazaHalfU + margin, v);
                TryTree(a.x, a.y, n++);
                Vector2 b = PlazaWorld(plazaHalfU - margin, v);
                TryTree(b.x, b.y, n++);
            }

            for (int k = 0; k < 8; k++)
            {
                float a = (22.5f + 45f * k) * Mathf.Deg2Rad;
                Vector2 p1 = PlazaWorld(Mathf.Cos(a) * plazaHalfU * 0.55f, Mathf.Sin(a) * plazaHalfV * 0.55f);
                TryTree(p1.x, p1.y, n++);
                Vector2 p2 = PlazaWorld(Mathf.Cos(a) * plazaHalfU * 0.78f, Mathf.Sin(a) * plazaHalfV * 0.78f);
                TryTree(p2.x, p2.y, n++);
            }
        }

        void PlantRing(int limit)
        {
            string[] kinds = TreeKinds();
            int planted = 0;
            for (int i = 0; i < 40 && planted < limit; i++)
            {
                float a = (i * 47f + 12f) * Mathf.Deg2Rad;
                float radius = 15f + (i % 5) * 5.5f;
                float x = Mathf.Cos(a) * radius;
                float z = Mathf.Sin(a) * radius;
                if (Mathf.Abs(x) < 8f || Mathf.Abs(z) < 8f) continue;
                if (x * x + z * z < 12f * 12f) continue;
                if (!OnStreet(x, z)) continue;
                PlantTree(kinds[i % kinds.Length], x, z, 9f + (i % 4) * 1.8f, i * 28f);
                planted++;
            }
        }

        void TryTree(float x, float z, int salt)
        {
            if (!OnPlaza(x, z) || OnGardenPath(x, z)) return;
            float dx = x - fountainX;
            float dz = z - fountainZ;
            if (dx * dx + dz * dz < (CorteFuente + 1.4f) * (CorteFuente + 1.4f)) return;
            string[] kinds = TreeKinds();
            PlantTree(kinds[Mathf.Abs(salt) % kinds.Length], x, z, 12f + (salt % 3) * 1.7f, salt * 28f);
        }

        static string[] TreeKinds()
        {
            return new[]
            {
                "Tree9/Tree9_2",
                "Tree9/Tree9_3",
                "Tree9/Tree9_4",
                "Tree9/Tree9_5",
            };
        }

        static readonly string[] MantoAsset =
        {
            "Bosque/grass",
            "Bosque/grass_large",
            "Bosque/grass_leafs",
            "Bosque/flower_redA",
            "Bosque/flower_yellowA",
            "Bosque/flower_purpleA",
            "Bosque/flower_redC",
        };

        bool OnGardenPath(float x, float z)
        {
            Vector2 local = PlazaLocal(x, z);
            return EnRuta(local.x, local.y)
                || Mathf.Min(plazaHalfU - Mathf.Abs(local.x), plazaHalfV - Mathf.Abs(local.y)) < PlazaWalk;
        }

        bool OnPlaza(float x, float z)
        {
            if (!plazaFound) return false;
            if (x < plazaMinX - 1f || x > plazaMaxX + 1f || z < plazaMinZ - 1f || z > plazaMaxZ + 1f) return false;
            var p = new Vector2(x, z);
            for (int i = 0; i + 2 < plazaWalk.Count; i += 3)
                if (Inside(p, plazaWalk[i], plazaWalk[i + 1], plazaWalk[i + 2])) return true;
            return false;
        }

        Vector2 PlazaWorld(float u, float v)
        {
            return new Vector2(fountainX + u * plazaCos - v * plazaSin,
                               fountainZ + u * plazaSin + v * plazaCos);
        }

        Vector2 PlazaLocal(float x, float z)
        {
            float dx = x - fountainX;
            float dz = z - fountainZ;
            return new Vector2(dx * plazaCos + dz * plazaSin,
                               -dx * plazaSin + dz * plazaCos);
        }

        bool FindMainPlaza()
        {
            plazaWalk.Clear();
            plazaFound = false;
            fountainX = 0f;
            fountainZ = 0f;
            plazaCos = 1f;
            plazaSin = 0f;
            plazaHalfU = 0f;
            plazaHalfV = 0f;
            if (plan == null) return false;
            MeshFilter filter = null;
            var filters = plan.GetComponentsInChildren<MeshFilter>(true);
            for (int i = 0; i < filters.Length; i++)
            {
                if (filters[i].gameObject.name.IndexOf("Plazas", System.StringComparison.Ordinal) < 0) continue;
                filter = filters[i];
                break;
            }

            if (filter == null || filter.sharedMesh == null || !filter.sharedMesh.isReadable) return false;
            var mesh = filter.sharedMesh;
            var verts = mesh.vertices;
            var tris = mesh.triangles;
            int count = tris.Length / 3;
            if (count == 0) return false;
            var parent = new int[count];
            for (int i = 0; i < count; i++) parent[i] = i;
            var owner = new Dictionary<long, int>();
            var poly = new Vector2[count * 3];
            var space = filter.transform;
            for (int t = 0; t < count; t++)
            {
                for (int k = 0; k < 3; k++)
                {
                    Vector3 world = district.InverseTransformPoint(space.TransformPoint(verts[tris[t * 3 + k]]));
                    poly[t * 3 + k] = new Vector2(world.x, world.z);
                    long key = ((long)Mathf.RoundToInt(world.x * 4f) << 32) | (uint)Mathf.RoundToInt(world.z * 4f);
                    if (owner.TryGetValue(key, out int other))
                    {
                        int ra = FindRoot(parent, t);
                        int rb = FindRoot(parent, other);
                        if (ra != rb) parent[rb] = ra;
                    }
                    else
                        owner[key] = t;
                }
            }

            for (int t = 0; t < count; t++) FindRoot(parent, t);
            var minX = new float[count];
            var maxX = new float[count];
            var minZ = new float[count];
            var maxZ = new float[count];
            var sumX = new float[count];
            var sumZ = new float[count];
            var num = new int[count];
            for (int i = 0; i < count; i++)
            {
                minX[i] = float.PositiveInfinity;
                minZ[i] = float.PositiveInfinity;
                maxX[i] = float.NegativeInfinity;
                maxZ[i] = float.NegativeInfinity;
            }

            for (int t = 0; t < count; t++)
            {
                int root = parent[t];
                for (int k = 0; k < 3; k++)
                {
                    var p = poly[t * 3 + k];
                    if (p.x < minX[root]) minX[root] = p.x;
                    if (p.x > maxX[root]) maxX[root] = p.x;
                    if (p.y < minZ[root]) minZ[root] = p.y;
                    if (p.y > maxZ[root]) maxZ[root] = p.y;
                    sumX[root] += p.x;
                    sumZ[root] += p.y;
                    num[root]++;
                }
            }

            int best = -1;
            float bestSpan = 0f;
            for (int t = 0; t < count; t++)
            {
                if (parent[t] != t || num[t] == 0) continue;
                float cx = sumX[t] / num[t];
                float cz = sumZ[t] / num[t];
                if (cx * cx + cz * cz > 40f * 40f) continue;
                float span = Mathf.Max(maxX[t] - minX[t], maxZ[t] - minZ[t]);
                if (span <= bestSpan) continue;
                bestSpan = span;
                best = t;
            }

            if (best < 0 || bestSpan < 40f) return false;
            plazaMinX = minX[best];
            plazaMaxX = maxX[best];
            plazaMinZ = minZ[best];
            plazaMaxZ = maxZ[best];
            fountainX = (plazaMinX + plazaMaxX) * 0.5f;
            fountainZ = (plazaMinZ + plazaMaxZ) * 0.5f;

            // Rectángulo envolvente mínimo de la plaza: su giro es el eje con
            // el que se dibujan jardines, caminos y el anillo central.
            var hull = new Vector2[count * 3];
            int hullN = 0;
            for (int t = 0; t < count; t++)
            {
                if (parent[t] != best) continue;
                hull[hullN++] = poly[t * 3];
                hull[hullN++] = poly[t * 3 + 1];
                hull[hullN++] = poly[t * 3 + 2];
            }

            float bestArea = float.PositiveInfinity;
            float bestAng = 0f;
            for (int i = 0; i < hullN; i++)
            {
                Vector2 a = hull[i];
                Vector2 b = hull[(i + 1) % hullN];
                float ang = Mathf.Atan2(b.y - a.y, b.x - a.x);
                float c0 = Mathf.Cos(-ang);
                float s0 = Mathf.Sin(-ang);
                float x0 = float.PositiveInfinity, x1 = float.NegativeInfinity;
                float z0 = float.PositiveInfinity, z1 = float.NegativeInfinity;
                for (int k = 0; k < hullN; k++)
                {
                    float px = hull[k].x * c0 - hull[k].y * s0;
                    float pz = hull[k].x * s0 + hull[k].y * c0;
                    if (px < x0) x0 = px;
                    if (px > x1) x1 = px;
                    if (pz < z0) z0 = pz;
                    if (pz > z1) z1 = pz;
                }

                float area = (x1 - x0) * (z1 - z0);
                if (area >= bestArea) continue;
                bestArea = area;
                bestAng = ang;
            }

            float c = Mathf.Cos(bestAng);
            float s = Mathf.Sin(bestAng);
            float u0 = float.PositiveInfinity, u1 = float.NegativeInfinity;
            float v0 = float.PositiveInfinity, v1 = float.NegativeInfinity;
            for (int k = 0; k < hullN; k++)
            {
                float u = hull[k].x * c + hull[k].y * s;
                float v = -hull[k].x * s + hull[k].y * c;
                if (u < u0) u0 = u;
                if (u > u1) u1 = u;
                if (v < v0) v0 = v;
                if (v > v1) v1 = v;
            }

            if (hullN >= 3 && u1 - u0 > 1f && v1 - v0 > 1f)
            {
                float uc = (u0 + u1) * 0.5f;
                float vc = (v0 + v1) * 0.5f;
                plazaCos = c;
                plazaSin = s;
                plazaHalfU = (u1 - u0) * 0.5f;
                plazaHalfV = (v1 - v0) * 0.5f;
                fountainX = uc * c - vc * s;
                fountainZ = uc * s + vc * c;
            }

            for (int t = 0; t < count; t++)
            {
                if (parent[t] != best) continue;
                plazaWalk.Add(poly[t * 3]);
                plazaWalk.Add(poly[t * 3 + 1]);
                plazaWalk.Add(poly[t * 3 + 2]);
            }

            plazaFound = plazaWalk.Count >= 3;
            return plazaFound;
        }

        static int FindRoot(int[] parent, int i)
        {
            int root = i;
            while (parent[root] != root) root = parent[root];
            while (parent[i] != root)
            {
                int next = parent[i];
                parent[i] = root;
                i = next;
            }

            return root;
        }

        static void StampCell(Pen pen, float x, float z, float width, float depth, float y, Color color)
        {
            float hx = width * 0.5f;
            float hz = depth * 0.5f;
            var c = new Vector3(x, y, z);
            pen.Quad(
                c + new Vector3(-hx, 0f, -hz),
                c + new Vector3(-hx, 0f, hz),
                c + new Vector3(hx, 0f, hz),
                c + new Vector3(hx, 0f, -hz),
                Vector3.up, color, false);
        }


        static bool lawnHidden;

        /// <summary>
        /// El terreno grande es pasto. La calzada usa el empedrado y la plaza
        /// su losa. Las UV de esas mallas se quedan: ya están medidas para
        /// la textura de la ruta.
        /// </summary>
        void DressGround()
        {
            var shader = Shader.Find("AYNI/Calle") ?? Shader.Find("Standard") ?? Shader.Find("Sprites/Default");
            if (shader == null) return;
            var grass = RepeatTex("Bosque/piso_cesped");
            var street = RepeatTex("Calle/piso_empedrado");
            var plaza = RepeatTex("Calle/piso_plaza");

            PaintTerrain(grass, shader);
            if (plan == null) return;
            var renderers = plan.GetComponentsInChildren<MeshRenderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                string name = renderers[i].gameObject.name;
                if (name.IndexOf("Nombres", System.StringComparison.Ordinal) >= 0) continue;
                if (name.IndexOf("Atrio", System.StringComparison.Ordinal) >= 0)
                    ApplyFloor(renderers[i], shader, plaza, false, Losa);
                else if (name.IndexOf("Calles", System.StringComparison.Ordinal) >= 0)
                    ApplyFloor(renderers[i], shader, street, false);
                else if (name.IndexOf("Plazas", System.StringComparison.Ordinal) >= 0
                    || name.IndexOf("Banda", System.StringComparison.Ordinal) >= 0)
                    ApplyFloor(renderers[i], shader, plaza, false, Losa);
                else if (name.IndexOf("Suelo", System.StringComparison.Ordinal) >= 0)
                    ApplyFloor(renderers[i], shader, grass, true);
            }
        }

        void PaintTerrain(Texture2D grass, Shader shader)
        {
            if (district == null) return;
            Transform terreno = district.Find("Terreno");
            if (terreno == null) return;
            var renderers = terreno.GetComponentsInChildren<MeshRenderer>(true);
            for (int i = 0; i < renderers.Length; i++)
                ApplyFloor(renderers[i], shader, grass, true);
            ShadeHeights(terreno);
        }

        /// <summary>
        /// En PLANTA el alto no se ve de canto. El pasto claro marca la cumbre
        /// y el pasto más verde el valle, sin tapar la textura.
        /// </summary>
        void ShadeHeights(Transform terreno)
        {
            if (terreno == null || district == null) return;
            var filters = terreno.GetComponentsInChildren<MeshFilter>(true);
            var low = new Color(0.58f, 0.84f, 0.52f);
            var high = new Color(1f, 0.88f, 0.58f);
            for (int f = 0; f < filters.Length; f++)
            {
                var filter = filters[f];
                if (filter == null || filter.sharedMesh == null || !filter.sharedMesh.isReadable) continue;
                var mesh = filter.mesh;
                var verts = mesh.vertices;
                if (verts == null || verts.Length == 0) continue;
                var colors = new Color[verts.Length];
                var space = filter.transform;
                for (int i = 0; i < verts.Length; i++)
                {
                    Vector3 local = district.InverseTransformPoint(space.TransformPoint(verts[i]));
                    float t = Mathf.InverseLerp(4f, 140f, local.y);
                    colors[i] = Color.Lerp(low, high, t);
                }

                mesh.colors = colors;
            }
        }

        /// <summary>
        /// Sube el relieve ya horneado hasta Present, sin estirar los muros:
        /// cada vértice gana la diferencia del suelo en su planta.
        /// </summary>
        void ExaggerateRelief()
        {
            if (district == null || district.Find("RelieveListo") != null) return;
            float baked = CajamarcaAltura.RelieveCalles;
            float shown = CajamarcaAltura.Present;
            if (shown < baked + 0.05f) return;

            float grow = shown / baked - 1f;
            CajamarcaAltura.Present = baked;
            var filters = district.GetComponentsInChildren<MeshFilter>(true);
            var carriers = new HashSet<Transform>();
            for (int i = 0; i < filters.Length; i++)
            {
                var filter = filters[i];
                if (filter == null || filter.sharedMesh == null || filter.sharedMesh.isReadable) continue;
                bool under = false;
                for (var p = filter.transform.parent; p != null && p != district; p = p.parent)
                {
                    if (carriers.Contains(p)) { under = true; break; }
                }

                if (!under) carriers.Add(filter.transform);
            }

            for (int i = 0; i < filters.Length; i++)
            {
                var filter = filters[i];
                if (filter == null || filter.sharedMesh == null) continue;
                bool carried = false;
                for (var p = filter.transform.parent; p != null && p != district; p = p.parent)
                {
                    if (carriers.Contains(p)) { carried = true; break; }
                }

                if (carried) continue;
                if (!filter.sharedMesh.isReadable)
                {
                    if (!carriers.Contains(filter.transform)) continue;
                    Vector3 at = district.InverseTransformPoint(filter.transform.position);
                    filter.transform.position += Vector3.up * (CajamarcaAltura.Sample(at.x, at.z) * grow);
                    continue;
                }

                var mesh = filter.mesh;
                var verts = mesh.vertices;
                var space = filter.transform;
                for (int v = 0; v < verts.Length; v++)
                {
                    Vector3 world = space.TransformPoint(verts[v]);
                    Vector3 local = district.InverseTransformPoint(world);
                    local.y += CajamarcaAltura.Sample(local.x, local.z) * grow;
                    verts[v] = space.InverseTransformPoint(district.TransformPoint(local));
                }

                mesh.vertices = verts;
                string name = filter.gameObject.name;
                bool ground = name.IndexOf("Terreno", System.StringComparison.Ordinal) >= 0
                    || name.IndexOf("Calles", System.StringComparison.Ordinal) >= 0
                    || name.IndexOf("Plazas", System.StringComparison.Ordinal) >= 0
                    || name.IndexOf("Banda", System.StringComparison.Ordinal) >= 0
                    || name.IndexOf("Suelo", System.StringComparison.Ordinal) >= 0
                    || name.IndexOf("Sendero", System.StringComparison.Ordinal) >= 0;
                if (ground) mesh.RecalculateNormals();
                mesh.RecalculateBounds();
            }

            CajamarcaAltura.Present = shown;
            var mark = new GameObject("RelieveListo");
            mark.transform.SetParent(district, false);
        }

        // Desde el cruce de Junín, Dos de Mayo trepa con jardín cercado
        // y después la escalera, hasta la capilla azul y blanca.
        static Vector2[] SubidaPts()
        {
            return new[]
            {
                new Vector2(-56.00f, -188.49f),
                new Vector2(-69.18f, -207.95f),
                new Vector2(-82.37f, -227.41f),
                new Vector2(-95.55f, -246.87f),
                new Vector2(-108.73f, -266.34f),
                new Vector2(-121.75f, -285.33f),
                new Vector2(-134.43f, -303.39f),
                new Vector2(-148.82f, -327.36f),
                new Vector2(-162.40f, -350.20f),
                new Vector2(-174.80f, -371.40f),
                new Vector2(-184.20f, -386.40f),
            };
        }

        static float PolyLen(Vector2[] pts)
        {
            float s = 0f;
            for (int i = 1; i < pts.Length; i++)
                s += Vector2.Distance(pts[i - 1], pts[i]);
            return s;
        }

        static void PointAt(Vector2[] pts, float dist, out float x, out float z, out float tx, out float tz)
        {
            x = pts[0].x;
            z = pts[0].y;
            tx = 0f;
            tz = 1f;
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
                    float t = Mathf.Clamp01((dist - acc) / len);
                    x = pts[i - 1].x + dx * t;
                    z = pts[i - 1].y + dz * t;
                    tx = dx / len;
                    tz = dz / len;
                    return;
                }

                acc += len;
            }
        }

        void DressSubida()
        {
            if (plan == null || plan.transform.Find("SubidaDosDeMayo") != null) return;
            var root = new GameObject("SubidaDosDeMayo");
            root.transform.SetParent(plan.transform, false);
            var pts = SubidaPts();
            float total = PolyLen(pts);
            float stairAt = StairMouth();
            const float half = 3.55f;
            const float garden = 60f;
            LayPoly(root.transform, "Calles Subida Dos de Mayo", pts, half, 0.1f, "Calle/piso_empedrado", stairAt + 2f);
            LayPoly(root.transform, "Cesped Mediana Dos de Mayo", pts, 1.05f, 0.24f, "Bosque/piso_cesped", garden);
            LayHuánucoStairs(root.transform, pts, stairAt, total - 6f);
            RaiseMedian(root.transform, pts, garden);
            RaiseCanyon(root.transform, pts, half, stairAt - 8f);
            FrameStairMouth(root.transform, pts, stairAt);
        }

        // Trazo original, sin invertir: el despeje y la malla no comparten el arreglo.
        static readonly List<Vector2[]> tramosEscalera = new List<Vector2[]>();

        void DressEscalerasDelCerro()
        {
            if (plan == null) return;
            var este = new[]
            {
                new Vector2(-60.75f, -315.56f),
                new Vector2(-86.40f, -348.61f),
                new Vector2(-98.51f, -362.87f),
            };
            var trasera = new[]
            {
                new Vector2(-288.93f, -458.67f),
                new Vector2(-285.83f, -456.45f),
                new Vector2(-219.67f, -410.65f),
            };
            var anden = new[]
            {
                new Vector2(-219.67f, -410.65f),
                new Vector2(-207.11f, -400.36f),
                new Vector2(-192.78f, -377.49f),
                new Vector2(-182.08f, -383.01f),
            };
            var sanMartin = new[]
            {
                new Vector2(-195.03f, -247.10f),
                new Vector2(-237.99f, -306.14f),
            };
            var petateros = new[]
            {
                new Vector2(-18.26f, -488.03f),
                new Vector2(-75.07f, -438.90f),
                new Vector2(-98.06f, -412.30f),
                new Vector2(-109.40f, -407.41f),
            };
            var argentina = new[]
            {
                new Vector2(-99.13f, -403.40f),
                new Vector2(-54.92f, -374.67f),
                new Vector2(-36.82f, -387.79f),
            };
            var belen = new[]
            {
                new Vector2(-42.62f, -394.74f),
                new Vector2(-36.82f, -387.79f),
                new Vector2(-8.55f, -353.90f),
            };

            tramosEscalera.Clear();
            tramosEscalera.Add(este);
            tramosEscalera.Add(trasera);
            tramosEscalera.Add(anden);
            tramosEscalera.Add(sanMartin);
            tramosEscalera.Add(petateros);
            tramosEscalera.Add(argentina);
            tramosEscalera.Add(belen);

            if (plan.transform.Find("EscalerasSantaApolonia") != null) return;
            var root = new GameObject("EscalerasSantaApolonia");
            root.transform.SetParent(plan.transform, false);
            LayEscalera(root.transform, "Calles Escalinata Este", este, 1.45f);
            LayEscalera(root.transform, "Calles Escalinata Trasera", trasera, 1.55f);
            LayPoly(root.transform, "Calles Sendero Anden", anden, 2.10f, 0.18f, "Calle/piedra", 0f);
            LayEscalera(root.transform, "Calles Escalinata San Martin", sanMartin, 2.05f);
            LayEscalera(root.transform, "Calles Escalinata Petateros", petateros, 1.85f);
            LayEscalera(root.transform, "Calles Escalinata Argentina", argentina, 1.55f);
            LayEscalera(root.transform, "Calles Escalinata Belen", belen, 1.55f);
        }

        void LayEscalera(Transform parent, string name, Vector2[] pts, float half)
        {
            if (pts == null || pts.Length < 2) return;
            var copia = (Vector2[])pts.Clone();
            float largo = PolyLen(copia);
            if (largo < 4f) return;
            float yInicio = CajamarcaAltura.Sample(copia[0].x, copia[0].y);
            float yFinal = CajamarcaAltura.Sample(copia[copia.Length - 1].x, copia[copia.Length - 1].y);
            if (yFinal < yInicio)
                System.Array.Reverse(copia);
            float rise = Mathf.Abs(yFinal - yInicio);
            if (rise < 0.8f) return;

            int n = Mathf.Clamp(Mathf.RoundToInt(rise / 0.16f), 4, 160);
            float step = largo / n;
            var verts = new List<Vector3>();
            var uv = new List<Vector2>();
            var tris = new List<int>();
            for (int i = 0; i < n; i++)
            {
                bool landing = (i % 9) == 8;
                float depth = step * (landing ? 1.12f : 1.02f);
                float halfW = half + (landing ? 0.30f : 0f);
                float d = (i + 0.5f) * step;
                PointAt(copia, d, out float x, out float z, out float tx, out float tz);
                float apoyo = CajamarcaAltura.Sample(x, z) + 0.22f;
                AddLevelTread(verts, uv, tris, x, z, tx, tz, halfW, depth, apoyo);
            }

            FinishStrip(parent, name, verts, uv, tris, "Calle/piedra", false);
            var piedra = new Color(0.86f, 0.84f, 0.78f);
            for (int i0 = 0; i0 < n; i0 += 9)
            {
                float desde = i0 * step;
                float hasta = Mathf.Min(largo, (i0 + 9) * step);
                float span = hasta - desde;
                if (span < 0.2f) continue;
                PointAt(copia, (desde + hasta) * 0.5f, out float x, out float z, out float tx, out float tz);
                var rot = Quaternion.LookRotation(new Vector3(tx, 0f, tz), Vector3.up);
                for (int lado = -1; lado <= 1; lado += 2)
                {
                    float borde = (half + 0.45f) * lado;
                    float wx = x + tz * borde;
                    float wz = z - tx * borde;
                    float g = CajamarcaAltura.Sample(wx, wz);
                    var muro = BoxPart(parent, "AndenPiedra", new Vector3(wx, g + 0.42f, wz), new Vector3(0.36f, 0.85f, span), "Calle/piedra", piedra);
                    muro.transform.localRotation = rot;
                }
            }
        }

        static float AlongNearest(Vector2[] pts, Vector2 mark)
        {
            float best = 0f;
            float bestD = float.MaxValue;
            float acc = 0f;
            for (int i = 1; i < pts.Length; i++)
            {
                Vector2 a = pts[i - 1];
                Vector2 b = pts[i];
                Vector2 ab = b - a;
                float len2 = ab.sqrMagnitude;
                float len = Mathf.Sqrt(len2);
                float t = len2 < 0.001f ? 0f : Mathf.Clamp01(Vector2.Dot(mark - a, ab) / len2);
                float along = acc + len * t;
                float d = (a + ab * t - mark).sqrMagnitude;
                if (d < bestD)
                {
                    bestD = d;
                    best = along;
                }

                acc += len;
            }

            return best;
        }

        // La boca de la escalera es el cruce con Huánuco, no el letrero (ese está al costado).
        static float StairMouth()
        {
            var pts = SubidaPts();
            float at = AlongNearest(pts, new Vector2(-132.47f, -273.27f));
            float total = PolyLen(pts);
            if (at < 72f) at = 72f;
            if (at > total - 30f) at = total - 30f;
            return at;
        }

        /// <summary>
        /// True while the hero is on Dos de Mayo. uphill is the climb in world XZ.
        /// Yaw 0 is +Z, so atan2(uphill.x, uphill.z) is the same yaw Wish() walks.
        /// </summary>
        bool MiradaDeSubida(Vector3 local, out float along, out Vector3 uphill)
        {
            along = 0f;
            uphill = Vector3.forward;
            if (district == null) return false;
            var pts = SubidaPts();
            along = AlongNearest(pts, new Vector2(local.x, local.z));
            PointAt(pts, along, out float x, out float z, out float tx, out float tz);
            float dx = local.x - x;
            float dz = local.z - z;
            if (dx * dx + dz * dz > 4.1f * 4.1f) return false;
            float total = PolyLen(pts);
            if (along < 2f || along > total + 8f) return false;
            uphill = district.TransformDirection(new Vector3(tx, 0f, tz));
            uphill.y = 0f;
            if (uphill.sqrMagnitude < 0.0001f) return false;
            uphill.Normalize();
            return true;
        }

        static void PlaceAlong(Vector2[] pts, float dist, out float x, out float z)
        {
            float total = PolyLen(pts);
            if (dist <= 0f)
            {
                PointAt(pts, 0f, out x, out z, out float tx, out float tz);
                x += tx * dist;
                z += tz * dist;
                return;
            }

            if (dist >= total)
            {
                PointAt(pts, total, out x, out z, out float tx, out float tz);
                float extra = dist - total;
                x += tx * extra;
                z += tz * extra;
                return;
            }

            PointAt(pts, dist, out x, out z, out _, out _);
        }

        // Cámara al pie de la cuesta, mirando hacia arriba. El personaje queda
        // abajo en la pantalla y las gradas suben hacia la capilla.
        // Check: tangent +Z → right offset is +X, and W walks +Z, away from the lens.
        void PoseSubida(float along, Vector3 uphill, out Vector3 position, out Quaternion rotation)
        {
            var pts = SubidaPts();
            float total = PolyLen(pts);
            float side = Mathf.Lerp(2.05f, 0f, Mathf.InverseLerp(StairMouth() - 18f, StairMouth() - 3f, along));
            PointAt(pts, Mathf.Clamp(along, 0f, total), out _, out _, out float tx, out float tz);
            float rx = tz * side;
            float rz = -tx * side;
            const float back = 8.2f;
            const float ahead = 15f;
            PlaceAlong(pts, along - back, out float cx, out float cz);
            PlaceAlong(pts, along + ahead, out float lx, out float lz);
            float camY = CajamarcaAltura.Sample(cx + rx, cz + rz) + 1.5f;
            float lookY = CajamarcaAltura.Sample(lx + rx, lz + rz) + 3.6f;
            position = district.TransformPoint(new Vector3(cx + rx, camY, cz + rz));
            Vector3 look = district.TransformPoint(new Vector3(lx + rx, lookY, lz + rz));
            Vector3 to = look - position;
            if (to.sqrMagnitude < 0.001f) to = uphill;
            rotation = Quaternion.LookRotation(to, Vector3.up);
            if (Physics.CheckSphere(position, 0.32f, 1, QueryTriggerInteraction.Ignore))
                position += Vector3.up * 1.2f;
        }

        void LayPoly(Transform parent, string name, Vector2[] pts, float half, float lift, string texture, float maxDist)
        {
            float total = PolyLen(pts);
            if (maxDist > 0f && maxDist < total) total = maxDist;
            if (total < 6f) return;
            int pieces = Mathf.Max(1, Mathf.CeilToInt(total / 4f));
            int n = pieces + 1;
            var verts = new Vector3[n * 2];
            var uv = new Vector2[n * 2];
            var colors = new Color[n * 2];
            float traveled = 0f;
            float prevX = 0f;
            float prevZ = 0f;
            for (int i = 0; i < n; i++)
            {
                PointAt(pts, total * i / pieces, out float x, out float z, out float tx, out float tz);
                float lx = -tz * half;
                float lz = tx * half;
                if (i > 0)
                    traveled += Mathf.Sqrt((x - prevX) * (x - prevX) + (z - prevZ) * (z - prevZ));
                prevX = x;
                prevZ = z;
                float yL = CajamarcaAltura.Sample(x + lx, z + lz) + lift;
                float yR = CajamarcaAltura.Sample(x - lx, z - lz) + lift;
                verts[i * 2] = new Vector3(x + lx, yL, z + lz);
                verts[i * 2 + 1] = new Vector3(x - lx, yR, z - lz);
                float v = traveled / Mathf.Max(1.5f, half * 2f);
                uv[i * 2] = new Vector2(0f, v);
                uv[i * 2 + 1] = new Vector2(1f, v);
                colors[i * 2] = Color.white;
                colors[i * 2 + 1] = Color.white;
            }

            var tris = new int[(n - 1) * 6];
            int cursor = 0;
            for (int i = 0; i < n - 1; i++)
            {
                int a = i * 2;
                int b = a + 1;
                int c = a + 2;
                int d = a + 3;
                tris[cursor++] = a; tris[cursor++] = b; tris[cursor++] = d;
                tris[cursor++] = a; tris[cursor++] = d; tris[cursor++] = c;
            }

            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var mesh = new Mesh { name = name };
            mesh.vertices = verts;
            mesh.uv = uv;
            mesh.colors = colors;
            mesh.triangles = tris;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            var filter = go.AddComponent<MeshFilter>();
            filter.sharedMesh = mesh;
            var renderer = go.AddComponent<MeshRenderer>();
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            bool lawn = texture != null && texture.IndexOf("cesped", System.StringComparison.OrdinalIgnoreCase) >= 0;
            var shader = Shader.Find("AYNI/Calle") ?? Shader.Find("Standard") ?? Shader.Find("Sprites/Default");
            if (shader != null) ApplyFloor(renderer, shader, RepeatTex(texture), lawn);
        }

        void LaySpan(Transform parent, string name, Vector2[] pts, float half, float lift, string texture, float from, float to)
        {
            if (to - from < 4f) return;
            int pieces = Mathf.Max(1, Mathf.CeilToInt((to - from) / 4f));
            int n = pieces + 1;
            var verts = new Vector3[n * 2];
            var uv = new Vector2[n * 2];
            var colors = new Color[n * 2];
            float traveled = 0f;
            float prevX = 0f;
            float prevZ = 0f;
            for (int i = 0; i < n; i++)
            {
                PointAt(pts, Mathf.Lerp(from, to, i / (float)pieces), out float x, out float z, out float tx, out float tz);
                float lx = -tz * half;
                float lz = tx * half;
                if (i > 0)
                    traveled += Mathf.Sqrt((x - prevX) * (x - prevX) + (z - prevZ) * (z - prevZ));
                prevX = x;
                prevZ = z;
                verts[i * 2] = new Vector3(x + lx, CajamarcaAltura.Sample(x + lx, z + lz) + lift, z + lz);
                verts[i * 2 + 1] = new Vector3(x - lx, CajamarcaAltura.Sample(x - lx, z - lz) + lift, z - lz);
                float v = traveled / Mathf.Max(1.5f, half * 2f);
                uv[i * 2] = new Vector2(0f, v);
                uv[i * 2 + 1] = new Vector2(1f, v);
                colors[i * 2] = Color.white;
                colors[i * 2 + 1] = Color.white;
            }

            var tris = new int[(n - 1) * 6];
            int cursor = 0;
            for (int i = 0; i < n - 1; i++)
            {
                int a = i * 2;
                int b = a + 1;
                int c = a + 2;
                int d = a + 3;
                tris[cursor++] = a; tris[cursor++] = b; tris[cursor++] = d;
                tris[cursor++] = a; tris[cursor++] = d; tris[cursor++] = c;
            }

            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var mesh = new Mesh { name = name };
            mesh.vertices = verts;
            mesh.uv = uv;
            mesh.colors = colors;
            mesh.triangles = tris;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            var filter = go.AddComponent<MeshFilter>();
            filter.sharedMesh = mesh;
            var renderer = go.AddComponent<MeshRenderer>();
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            var shader = Shader.Find("AYNI/Calle") ?? Shader.Find("Standard") ?? Shader.Find("Sprites/Default");
            if (shader != null) ApplyFloor(renderer, shader, RepeatTex(texture), false);
        }

        // Desde Huánuco, tramos de 9 gradas que pisan el cerro.
        // Arriba, dos escaleras circulares (la de abajo más ancha), un descanso
        // y la grada recta que llega a la puerta. El pasto queda a los lados.
        void LayHuánucoStairs(Transform parent, Vector2[] pts, float at, float end)
        {
            const int curves = 4;
            const float curveRad = 0.55f;
            const float curveOuter = 6.2f;
            const int perFlight = 9;
            const float tread = 0.42f;
            const float land = 1.7f;
            const int ringsA = 9;
            const float radA = 0.46f;
            const float outerA = 4.8f;
            const int ringsB = 7;
            const float radB = 0.42f;
            const float outerB = 3.6f;
            const float throat = 2.4f;
            const float plaza = 3.6f;
            const int lastFlight = 16;
            const float lastTread = 0.40f;
            float circleBlock = outerA * 2f + throat + outerB * 2f + plaza + lastFlight * lastTread;
            var stoneV = new List<Vector3>();
            var stoneU = new List<Vector2>();
            var stoneT = new List<int>();
            var grassV = new List<Vector3>();
            var grassU = new List<Vector2>();
            var grassT = new List<int>();

            PointAt(pts, at, out float x, out float z, out float tx, out float tz);
            float stepY = CajamarcaAltura.Sample(x, z) + 0.08f;
            var center = new Vector3(x + tx * curveOuter, 0f, z + tz * curveOuter);
            var down = new Vector3(-tx, 0f, -tz);
            var right = new Vector3(tz, 0f, -tx);
            for (int s = 0; s < curves; s++)
            {
                float r0 = curveOuter - s * curveRad;
                float r1 = r0 - curveRad;
                Vector3 front = center + down * ((r0 + r1) * 0.5f);
                stepY = Mathf.Max(CajamarcaAltura.Sample(front.x, front.z) + 0.06f, stepY + 0.14f);
                AddArc(stoneV, stoneU, stoneT, center, down, right, r0, r1, stepY, 88f, 16);
            }

            float d = at + curves * curveRad + 0.4f;
            float straightUntil = end - circleBlock;
            if (straightUntil < d + 8f) straightUntil = d + 8f;
            while (d + tread < straightUntil)
            {
                float flightStart = d;
                for (int i = 0; i < perFlight && d + tread < straightUntil; i++)
                {
                    PointAt(pts, d + tread * 0.5f, out x, out z, out tx, out tz);
                    float yUp = CajamarcaAltura.Sample(x + tx * tread * 0.5f, z + tz * tread * 0.5f);
                    stepY = Mathf.Max(yUp + 0.03f, stepY + 0.12f);
                    AddLevelTread(stoneV, stoneU, stoneT, x, z, tx, tz, 1.65f, tread, stepY);
                    d += tread;
                }

                if (d + land < straightUntil)
                {
                    PointAt(pts, d + land * 0.5f, out x, out z, out tx, out tz);
                    float yLand = CajamarcaAltura.Sample(x + tx * land * 0.5f, z + tz * land * 0.5f);
                    stepY = Mathf.Max(yLand + 0.03f, stepY);
                    AddLevelTread(stoneV, stoneU, stoneT, x, z, tx, tz, 2.15f, land, stepY);
                    AddTerrace(grassV, grassU, grassT, x, z, tx, tz);
                    d += land;
                }

                if (d <= flightStart + 0.01f) break;
            }

            if (straightUntil - d > 0.6f)
            {
                float gap = straightUntil - d;
                PointAt(pts, d + gap * 0.5f, out x, out z, out tx, out tz);
                AddLevelTread(stoneV, stoneU, stoneT, x, z, tx, tz, 2.15f, gap, stepY);
                d = straightUntil;
            }

            float bow = d;
            float topA = AddCircle(stoneV, stoneU, stoneT, pts, bow, ringsA, radA, outerA, stepY);
            float afterA = bow + outerA * 2f;
            AddSpanTread(stoneV, stoneU, stoneT, pts, afterA, throat, outerA * 0.7f, topA);
            float bowB = afterA + throat;
            float topB = AddCircle(stoneV, stoneU, stoneT, pts, bowB, ringsB, radB, outerB, topA);
            float afterB = bowB + outerB * 2f;
            AddSpanTread(stoneV, stoneU, stoneT, pts, afterB, plaza, outerB + 0.6f, topB);
            float flight = afterB + plaza;
            stepY = topB;
            for (int i = 0; i < lastFlight && flight + lastTread < end + 2f; i++)
            {
                PointAt(pts, flight + lastTread * 0.5f, out x, out z, out tx, out tz);
                float ground = CajamarcaAltura.Sample(x + tx * lastTread * 0.5f, z + tz * lastTread * 0.5f);
                stepY = Mathf.Max(ground + 0.04f, stepY + 0.14f);
                AddLevelTread(stoneV, stoneU, stoneT, x, z, tx, tz, 3.15f, lastTread, stepY);
                flight += lastTread;
            }

            AddBowlGarden(grassV, grassU, grassT, pts, bow, afterB, Mathf.Max(outerA, outerB));
            FinishStrip(parent, "Calles Escalinatas Dos de Mayo", stoneV, stoneU, stoneT, "Calle/piedra", false);
            FinishStrip(parent, "Cesped Terraza Dos de Mayo", grassV, grassU, grassT, "Bosque/piso_cesped", true);
            RaiseTerraceWalls(parent, pts, at + curves * curveRad + 1f, bow - 1f);
            FrameCircle(parent, pts, bow, outerA);
            FrameCircle(parent, pts, bowB, outerB);
        }

        // Anillos completos. El de afuera nace a la altura de llegada y el centro
        // alcanza el labio de arriba, así la escalera sube el cerro en redondo.
        static float AddCircle(List<Vector3> verts, List<Vector2> uv, List<int> tris,
            Vector2[] pts, float front, int rings, float radial, float outerR, float baseY)
        {
            PointAt(pts, front + outerR, out float cx, out float cz, out float tx, out float tz);
            var center = new Vector3(cx, 0f, cz);
            var down = new Vector3(-tx, 0f, -tz);
            var right = new Vector3(tz, 0f, -tx);
            Vector3 back = center - down * outerR;
            float yBack = CajamarcaAltura.Sample(back.x, back.z);
            float rise = Mathf.Max(0.15f, (yBack - baseY) / Mathf.Max(1, rings));
            for (int s = 0; s < rings; s++)
            {
                float r0 = outerR - s * radial;
                float r1 = Mathf.Max(0.28f, r0 - radial);
                float y = baseY + rise * (s + 1);
                AddArc(verts, uv, tris, center, down, right, r0, r1, y, 180f, 28);
            }

            float hub = Mathf.Max(0.45f, outerR - rings * radial);
            float top = baseY + rise * rings;
            AddDisk(verts, uv, tris, center, hub, top);
            return top;
        }

        static void AddDisk(List<Vector3> verts, List<Vector2> uv, List<int> tris,
            Vector3 center, float radius, float y)
        {
            const int seg = 24;
            for (int s = 0; s < seg; s++)
            {
                float u0 = s / (float)seg * Mathf.PI * 2f;
                float u1 = (s + 1) / (float)seg * Mathf.PI * 2f;
                var p0 = new Vector3(center.x + Mathf.Cos(u0) * radius, 0f, center.z + Mathf.Sin(u0) * radius);
                var p1 = new Vector3(center.x + Mathf.Cos(u1) * radius, 0f, center.z + Mathf.Sin(u1) * radius);
                AddWedge(verts, uv, tris, center, p0, p1, y, 0.16f);
            }
        }

        void FrameCircle(Transform parent, Vector2[] pts, float front, float outerR)
        {
            PointAt(pts, front + outerR, out float x, out float z, out float tx, out float tz);
            var stone = new Color(0.86f, 0.84f, 0.78f);
            var rot = Quaternion.LookRotation(new Vector3(tx, 0f, tz), Vector3.up);
            float length = outerR * 1.7f;
            for (int side = -1; side <= 1; side += 2)
            {
                float edge = (outerR + 1.15f) * side;
                float wx = x + tz * edge;
                float wz = z - tx * edge;
                float g = CajamarcaAltura.Sample(wx, wz);
                var wall = BoxPart(parent, "AndenPiedra", new Vector3(wx, g + 0.55f, wz), new Vector3(0.42f, 1.05f, length), "Calle/piedra", stone);
                wall.transform.localRotation = rot;
            }
        }

        // Semicírculo con el arco ancho hacia abajo y el centro hacia la capilla.
        // u = 0 mira cuesta abajo; u = ±90° cierra el diámetro.
        static float AddBowl(List<Vector3> verts, List<Vector2> uv, List<int> tris,
            Vector2[] pts, float front, int rings, float radial, float outerR)
        {
            PointAt(pts, front + outerR, out float cx, out float cz, out float tx, out float tz);
            var center = new Vector3(cx, 0f, cz);
            var down = new Vector3(-tx, 0f, -tz);
            var right = new Vector3(tz, 0f, -tx);
            Vector3 lip = center + down * outerR;
            float yFront = CajamarcaAltura.Sample(lip.x, lip.z);
            float yHub = CajamarcaAltura.Sample(cx, cz);
            float rise = Mathf.Max(0.15f, (yHub - yFront) / rings);
            for (int s = 0; s < rings; s++)
            {
                float r0 = outerR - s * radial;
                float r1 = r0 - radial;
                float y = yFront + rise * (s + 1);
                AddArc(verts, uv, tris, center, down, right, r0, r1, y, 90f, 22);
            }

            float hub = outerR - rings * radial;
            float top = yFront + rise * rings;
            AddHalfDisk(verts, uv, tris, center, down, right, hub, top);
            return top;
        }

        static void AddSpanTread(List<Vector3> verts, List<Vector2> uv, List<int> tris,
            Vector2[] pts, float from, float depth, float half, float minY)
        {
            PointAt(pts, from + depth * 0.5f, out float x, out float z, out float tx, out float tz);
            AddLevelTread(verts, uv, tris, x, z, tx, tz, half, depth * 0.92f, minY);
        }

        static void AddArc(List<Vector3> verts, List<Vector2> uv, List<int> tris,
            Vector3 center, Vector3 down, Vector3 right, float outerR, float innerR, float y, float sweep, int seg)
        {
            float a0 = -sweep * Mathf.Deg2Rad;
            float a1 = sweep * Mathf.Deg2Rad;
            for (int s = 0; s < seg; s++)
            {
                float u0 = Mathf.Lerp(a0, a1, s / (float)seg);
                float u1 = Mathf.Lerp(a0, a1, (s + 1) / (float)seg);
                Vector3 Ring(float u, float radius)
                {
                    return center + down * (Mathf.Cos(u) * radius) + right * (Mathf.Sin(u) * radius);
                }

                Vector3 p0 = Ring(u0, outerR);
                Vector3 p1 = Ring(u1, outerR);
                Vector3 p2 = Ring(u0, innerR);
                Vector3 p3 = Ring(u1, innerR);
                AddFlat(verts, uv, tris, p0, p1, p2, p3, y, 0.16f);
            }
        }

        static void AddHalfDisk(List<Vector3> verts, List<Vector2> uv, List<int> tris,
            Vector3 center, Vector3 down, Vector3 right, float radius, float y)
        {
            const int seg = 14;
            for (int s = 0; s < seg; s++)
            {
                float u0 = Mathf.Lerp(-90f, 90f, s / (float)seg) * Mathf.Deg2Rad;
                float u1 = Mathf.Lerp(-90f, 90f, (s + 1) / (float)seg) * Mathf.Deg2Rad;
                Vector3 p0 = center + down * (Mathf.Cos(u0) * radius) + right * (Mathf.Sin(u0) * radius);
                Vector3 p1 = center + down * (Mathf.Cos(u1) * radius) + right * (Mathf.Sin(u1) * radius);
                AddWedge(verts, uv, tris, center, p0, p1, y, 0.16f);
            }
        }

        static void AddWedge(List<Vector3> verts, List<Vector2> uv, List<int> tris,
            Vector3 center, Vector3 p0, Vector3 p1, float y, float thick)
        {
            int i = verts.Count;
            verts.Add(new Vector3(center.x, y, center.z));
            verts.Add(new Vector3(p0.x, y, p0.z));
            verts.Add(new Vector3(p1.x, y, p1.z));
            uv.Add(new Vector2(0.5f, 0f));
            uv.Add(new Vector2(0f, 1f));
            uv.Add(new Vector2(1f, 1f));
            tris.Add(i); tris.Add(i + 1); tris.Add(i + 2);
            tris.Add(i); tris.Add(i + 2); tris.Add(i + 1);
            if (thick < 0.02f) return;
            int r = verts.Count;
            verts.Add(new Vector3(p0.x, y, p0.z));
            verts.Add(new Vector3(p1.x, y, p1.z));
            verts.Add(new Vector3(p0.x, y - thick, p0.z));
            verts.Add(new Vector3(p1.x, y - thick, p1.z));
            uv.Add(new Vector2(0f, 1f));
            uv.Add(new Vector2(1f, 1f));
            uv.Add(new Vector2(0f, 0f));
            uv.Add(new Vector2(1f, 0f));
            tris.Add(r); tris.Add(r + 2); tris.Add(r + 3);
            tris.Add(r); tris.Add(r + 3); tris.Add(r + 1);
            tris.Add(r); tris.Add(r + 3); tris.Add(r + 2);
            tris.Add(r); tris.Add(r + 1); tris.Add(r + 3);
        }

        static void AddBowlGarden(List<Vector3> verts, List<Vector2> uv, List<int> tris,
            Vector2[] pts, float from, float to, float outerR)
        {
            float mid = (from + to) * 0.5f;
            float depth = (to - from) * 0.5f;
            PointAt(pts, mid, out float x, out float z, out float tx, out float tz);
            for (int side = -1; side <= 1; side += 2)
            {
                float edge = (outerR + 2.4f) * side;
                float ox = tz * edge;
                float oz = -tx * edge;
                float y = CajamarcaAltura.Sample(x + ox, z + oz) + 0.12f;
                float hx = tz * 1.15f * side;
                float hz = -tx * 1.15f * side;
                float dx = tx * depth;
                float dz = tz * depth;
                var a = new Vector3(x + ox - hx - dx, 0f, z + oz - hz - dz);
                var b = new Vector3(x + ox + hx - dx, 0f, z + oz + hz - dz);
                var c = new Vector3(x + ox - hx + dx, 0f, z + oz - hz + dz);
                var d = new Vector3(x + ox + hx + dx, 0f, z + oz + hz + dz);
                AddFlat(verts, uv, tris, a, b, c, d, y, 0.08f);
            }
        }

        static void AddLevelTread(List<Vector3> verts, List<Vector2> uv, List<int> tris,
            float x, float z, float tx, float tz, float half, float depth, float minY)
        {
            float hx = tz * half;
            float hz = -tx * half;
            float dx = tx * depth * 0.5f;
            float dz = tz * depth * 0.5f;
            float y0 = CajamarcaAltura.Sample(x - hx - dx, z - hz - dz);
            float y1 = CajamarcaAltura.Sample(x + hx - dx, z + hz - dz);
            float y2 = CajamarcaAltura.Sample(x - hx + dx, z - hz + dz);
            float y3 = CajamarcaAltura.Sample(x + hx + dx, z + hz + dz);
            float y = Mathf.Max(Mathf.Max(Mathf.Max(y0, y1), Mathf.Max(y2, y3)) + 0.06f, minY);
            var a = new Vector3(x - hx - dx, 0f, z - hz - dz);
            var b = new Vector3(x + hx - dx, 0f, z + hz - dz);
            var c = new Vector3(x - hx + dx, 0f, z - hz + dz);
            var d = new Vector3(x + hx + dx, 0f, z + hz + dz);
            AddFlat(verts, uv, tris, a, b, c, d, y, 0.22f);
        }

        static void AddTerrace(List<Vector3> verts, List<Vector2> uv, List<int> tris,
            float x, float z, float tx, float tz)
        {
            for (int side = -1; side <= 1; side += 2)
            {
                for (int band = 0; band < 2; band++)
                {
                    float inner = 2.5f + band * 1.7f;
                    float outer = inner + 1.45f;
                    float mid = (inner + outer) * 0.5f * side;
                    float ox = tz * mid;
                    float oz = -tx * mid;
                    float half = (outer - inner) * 0.5f;
                    float y = CajamarcaAltura.Sample(x + ox, z + oz) + 0.12f + band * 0.08f;
                    float hx = tz * half * side;
                    float hz = -tx * half * side;
                    float dx = tx * 2.6f;
                    float dz = tz * 2.6f;
                    var a = new Vector3(x + ox - hx - dx, 0f, z + oz - hz - dz);
                    var b = new Vector3(x + ox + hx - dx, 0f, z + oz + hz - dz);
                    var c = new Vector3(x + ox - hx + dx, 0f, z + oz - hz + dz);
                    var d = new Vector3(x + ox + hx + dx, 0f, z + oz + hz + dz);
                    AddFlat(verts, uv, tris, a, b, c, d, y, 0.08f);
                }
            }
        }

        static void AddFlat(List<Vector3> verts, List<Vector2> uv, List<int> tris,
            Vector3 a, Vector3 b, Vector3 c, Vector3 d, float y, float thick)
        {
            int i = verts.Count;
            verts.Add(new Vector3(a.x, y, a.z));
            verts.Add(new Vector3(b.x, y, b.z));
            verts.Add(new Vector3(c.x, y, c.z));
            verts.Add(new Vector3(d.x, y, d.z));
            uv.Add(new Vector2(0f, 0f));
            uv.Add(new Vector2(1f, 0f));
            uv.Add(new Vector2(0f, 1f));
            uv.Add(new Vector2(1f, 1f));
            tris.Add(i); tris.Add(i + 1); tris.Add(i + 3);
            tris.Add(i); tris.Add(i + 3); tris.Add(i + 2);
            tris.Add(i); tris.Add(i + 3); tris.Add(i + 1);
            tris.Add(i); tris.Add(i + 2); tris.Add(i + 3);
            if (thick < 0.02f) return;
            int r = verts.Count;
            verts.Add(new Vector3(a.x, y, a.z));
            verts.Add(new Vector3(b.x, y, b.z));
            verts.Add(new Vector3(a.x, y - thick, a.z));
            verts.Add(new Vector3(b.x, y - thick, b.z));
            uv.Add(new Vector2(0f, 1f));
            uv.Add(new Vector2(1f, 1f));
            uv.Add(new Vector2(0f, 0f));
            uv.Add(new Vector2(1f, 0f));
            tris.Add(r); tris.Add(r + 2); tris.Add(r + 3);
            tris.Add(r); tris.Add(r + 3); tris.Add(r + 1);
            tris.Add(r); tris.Add(r + 3); tris.Add(r + 2);
            tris.Add(r); tris.Add(r + 1); tris.Add(r + 3);
        }

        void FinishStrip(Transform parent, string name, List<Vector3> verts, List<Vector2> uv, List<int> tris, string texture, bool lawn)
        {
            if (verts.Count < 4) return;
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var mesh = new Mesh { name = name };
            mesh.SetVertices(verts);
            mesh.SetUVs(0, uv);
            var colors = new Color[verts.Count];
            for (int i = 0; i < colors.Length; i++) colors[i] = Color.white;
            mesh.SetColors(colors);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = go.AddComponent<MeshRenderer>();
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            var shader = Shader.Find("AYNI/Calle") ?? Shader.Find("Standard") ?? Shader.Find("Sprites/Default");
            if (shader != null) ApplyFloor(renderer, shader, RepeatTex(texture), lawn);
        }

        void RaiseMedian(Transform parent, Vector2[] pts, float garden)
        {
            var ink = new Color(0.08f, 0.08f, 0.09f);
            var plant = new Color(0.34f, 0.58f, 0.3f);
            const float edge = 1.12f;
            for (float d = 2.4f; d < garden - 1.2f; d += 4.6f)
            {
                PointAt(pts, d, out float x, out float z, out float tx, out float tz);
                PlaceRail(parent, x, z, tx, tz, edge, 4.4f, ink);
                PlaceRail(parent, x, z, tx, tz, -edge, 4.4f, ink);
                if (((int)(d / 4.6f) % 3) == 0)
                {
                    float g = CajamarcaAltura.Sample(x, z);
                    BoxPart(parent, "Cesped Mata", new Vector3(x, g + 0.42f, z), new Vector3(0.85f, 0.38f, 0.7f), "Bosque/piso_cesped", plant);
                }
            }

            PlaceCap(parent, pts, 1.6f, edge, ink);
            PlaceCap(parent, pts, garden - 0.6f, edge, ink);
            PointAt(pts, 16f, out float sx, out float sz, out float stx, out float stz);
            float sg = CajamarcaAltura.Sample(sx, sz);
            BoxPart(parent, "EstatuaPedestal", new Vector3(sx, sg + 0.62f, sz), new Vector3(0.72f, 1.15f, 0.72f), "Calle/piedra", Color.white);
            var figura = BoxPart(parent, "EstatuaFigura", new Vector3(sx, sg + 1.95f, sz), new Vector3(0.36f, 1.5f, 0.3f), null, new Color(0.94f, 0.92f, 0.86f));
            figura.transform.localRotation = Quaternion.LookRotation(new Vector3(stx, 0f, stz), Vector3.up);
        }

        void PlaceRail(Transform parent, float x, float z, float tx, float tz, float side, float length, Color ink)
        {
            float ox = tz * side;
            float oz = -tx * side;
            float wx = x + ox;
            float wz = z + oz;
            float g = CajamarcaAltura.Sample(wx, wz);
            var rail = BoxPart(parent, "CercoSubida", new Vector3(wx, g + 0.46f, wz), new Vector3(0.1f, 0.74f, length), null, ink);
            rail.transform.localRotation = Quaternion.LookRotation(new Vector3(tx, 0f, tz), Vector3.up);
            BoxPart(parent, "PosteCerco", new Vector3(wx, g + 0.48f, wz), new Vector3(0.14f, 0.9f, 0.14f), null, ink);
        }

        void PlaceCap(Transform parent, Vector2[] pts, float dist, float edge, Color ink)
        {
            PointAt(pts, dist, out float x, out float z, out float tx, out float tz);
            float g = CajamarcaAltura.Sample(x, z);
            var rail = BoxPart(parent, "CercoSubida", new Vector3(x, g + 0.46f, z), new Vector3(0.1f, 0.74f, edge * 2f), null, ink);
            rail.transform.localRotation = Quaternion.LookRotation(new Vector3(tz, 0f, -tx), Vector3.up);
        }

        void FrameStairMouth(Transform parent, Vector2[] pts, float at)
        {
            PointAt(pts, at + 1.2f, out float x, out float z, out float tx, out float tz);
            var rot = Quaternion.LookRotation(new Vector3(tx, 0f, tz), Vector3.up);
            float left = -9.6f;
            float lx = x + tz * left;
            float lz = z - tx * left;
            float g = CajamarcaAltura.Sample(lx, lz);
            var house = BoxPart(parent, "MuroSubida", new Vector3(lx, g + 1.7f, lz), new Vector3(0.7f, 3.4f, 6.2f), "Calle/muro_adobe", new Color(0.90f, 0.76f, 0.24f));
            house.transform.localRotation = rot;
            float right = 9.6f;
            float rx = x + tz * right;
            float rz = z - tx * right;
            g = CajamarcaAltura.Sample(rx, rz);
            var wall = BoxPart(parent, "AndenPiedra", new Vector3(rx, g + 0.85f, rz), new Vector3(0.55f, 1.7f, 6.4f), "Calle/piedra", new Color(0.86f, 0.84f, 0.78f));
            wall.transform.localRotation = rot;
        }

        void FrameBowls(Transform parent, Vector2[] pts, float from, float to, float edge)
        {
            var stone = new Color(0.86f, 0.84f, 0.78f);
            float mid = (from + to) * 0.5f;
            float length = to - from + 1.2f;
            PointAt(pts, mid, out float x, out float z, out float tx, out float tz);
            var rot = Quaternion.LookRotation(new Vector3(tx, 0f, tz), Vector3.up);
            for (int side = -1; side <= 1; side += 2)
            {
                float ox = tz * edge * side;
                float oz = -tx * edge * side;
                float g = CajamarcaAltura.Sample(x + ox, z + oz);
                var wall = BoxPart(parent, "AndenPiedra", new Vector3(x + ox, g + 0.42f, z + oz), new Vector3(0.4f, 0.78f, length), "Calle/piedra", stone);
                wall.transform.localRotation = rot;
            }
        }

        void RaiseTerraceWalls(Transform parent, Vector2[] pts, float from, float end)
        {
            var stone = new Color(0.72f, 0.70f, 0.64f);
            for (float d = from; d < end - 3f; d += 8.2f)
            {
                PointAt(pts, d, out float x, out float z, out float tx, out float tz);
                var rot = Quaternion.LookRotation(new Vector3(tx, 0f, tz), Vector3.up);
                for (int side = -1; side <= 1; side += 2)
                {
                    float edge = 5.6f * side;
                    float wx = x + tz * edge;
                    float wz = z - tx * edge;
                    float g = CajamarcaAltura.Sample(wx, wz);
                    var wall = BoxPart(parent, "AndenPiedra", new Vector3(wx, g + 0.48f, wz), new Vector3(0.42f, 0.9f, 6.4f), "Calle/piedra", stone);
                    wall.transform.localRotation = rot;
                }
            }
        }

        void RaiseCanyon(Transform parent, Vector2[] pts, float streetHalf, float until)
        {
            var white = new Color(0.97f, 0.95f, 0.92f);
            var wood = new Color(0.40f, 0.22f, 0.11f);
            var teal = new Color(0.2f, 0.5f, 0.55f);
            int index = 0;
            for (float d = 4.2f; d < until; d += 7.15f)
            {
                PointAt(pts, d, out float x, out float z, out float tx, out float tz);
                PlaceFacade(parent, x, z, tx, tz, 1f, index, streetHalf, white, wood, teal);
                PlaceFacade(parent, x, z, tx, tz, -1f, index, streetHalf, white, wood, teal);
                index++;
            }
        }

        void PlaceFacade(Transform parent, float x, float z, float tx, float tz, float side, int index,
            float streetHalf, Color white, Color wood, Color teal)
        {
            float ox = tz * side;
            float oz = -tx * side;
            const float thick = 0.58f;
            const float height = 5.15f;
            const float length = 7.4f;
            float offset = streetHalf + 0.2f + thick * 0.5f;
            float wx = x + ox * offset;
            float wz = z + oz * offset;
            float g = CajamarcaAltura.Sample(wx, wz);
            var rot = Quaternion.LookRotation(new Vector3(tx, 0f, tz), Vector3.up);
            var wall = BoxPart(parent, "MuroSubida", new Vector3(wx, g + 0.5f + height * 0.5f, wz), new Vector3(thick, height, length), "Calle/muro_adobe", white);
            wall.transform.localRotation = rot;
            var cimiento = BoxPart(parent, "CimientoSubida", new Vector3(wx, g + 0.36f, wz), new Vector3(thick + 0.06f, 0.68f, length), "Calle/piedra", Color.white);
            cimiento.transform.localRotation = rot;

            float eaveOff = offset - (thick * 0.5f + 0.42f);
            float ex = x + ox * eaveOff;
            float ez = z + oz * eaveOff;
            var alero = BoxPart(parent, "AleroSubida", new Vector3(ex, g + height + 0.28f, ez), new Vector3(0.95f, 0.2f, length + 0.1f), null, wood);
            alero.transform.localRotation = rot;
            for (int r = -1; r <= 1; r += 2)
            {
                var viga = BoxPart(parent, "VigaSubida", new Vector3(ex + tx * r * 1.7f, g + height + 0.16f, ez + tz * r * 1.7f), new Vector3(1.05f, 0.1f, 0.16f), null, wood);
                viga.transform.localRotation = rot;
            }

            var doorColor = side > 0f ? new Color(0.14f, 0.34f, 0.38f) : new Color(0.26f, 0.14f, 0.08f);
            float doorOff = offset - thick * 0.5f + 0.04f;
            var door = BoxPart(parent, "PuertaSubida", new Vector3(x + ox * doorOff, g + 1.25f, z + oz * doorOff), new Vector3(0.08f, 2.2f, 1.3f), null, doorColor);
            door.transform.localRotation = rot;
            if (side > 0f && (index % 2) == 0)
            {
                float balOff = offset - thick * 0.5f - 0.34f;
                var balcon = BoxPart(parent, "BalconSubida", new Vector3(x + ox * balOff, g + 3.25f, z + oz * balOff), new Vector3(0.62f, 0.26f, 1.65f), null, teal);
                balcon.transform.localRotation = rot;
            }
        }

        struct Spur
        {
            public string Name;
            public float X0, Z0, X1, Z1, Half;
        }

        static Spur[] MoreSpurs()
        {
            return new[]
            {
                new Spur { Name = "Jirón Belén", X0 = 67f, Z0 = -156f, X1 = 118f, Z1 = -248f, Half = 3.3f },
                new Spur { Name = "Jirón Apurímac", X0 = -120f, Z0 = 113f, X1 = -205f, Z1 = 172f, Half = 3.3f },
                new Spur { Name = "Jirón Lima", X0 = 164f, Z0 = 31f, X1 = 182f, Z1 = -42f, Half = 3.3f },
                new Spur { Name = "Jirón Angamos", X0 = 148f, Z0 = 74f, X1 = 182f, Z1 = 8f, Half = 3.3f },
                new Spur { Name = "Prolongación Junín", X0 = -12f, Z0 = -219f, X1 = 72f, Z1 = -286f, Half = 3.3f },
                new Spur { Name = "Jirón Chota", X0 = -167f, Z0 = 35f, X1 = -242f, Z1 = 102f, Half = 3.3f },
                new Spur { Name = "Jirón Miguel Iglesias", X0 = -150f, Z0 = -76f, X1 = -248f, Z1 = -8f, Half = 3.3f },
                new Spur { Name = "Jirón Tarapacá", X0 = 58f, Z0 = 157f, X1 = 118f, Z1 = 186f, Half = 3.3f },
                new Spur { Name = "Jirón José Sabogal", X0 = -209f, Z0 = -240f, X1 = -272f, Z1 = -158f, Half = 3.3f },
                new Spur { Name = "Avenida Mario Urteaga", X0 = 182f, Z0 = 8f, X1 = 270f, Z1 = -52f, Half = 3.8f },
                new Spur { Name = "Jirón Cinco Esquinas", X0 = 118f, Z0 = -248f, X1 = 198f, Z1 = -198f, Half = 3.3f },
                new Spur { Name = "Jirón Revilla Pérez", X0 = 118f, Z0 = 186f, X1 = 62f, Z1 = 258f, Half = 3.3f },
                new Spur { Name = "Jirón San Salvador", X0 = -205f, Z0 = 172f, X1 = -262f, Z1 = 246f, Half = 3.3f },
                new Spur { Name = "Jirón Guillermo Urrelo", X0 = -242f, Z0 = 102f, X1 = -310f, Z1 = 28f, Half = 3.3f },
                new Spur { Name = "Jirón Ayacucho", X0 = -272f, Z0 = -158f, X1 = -338f, Z1 = -220f, Half = 3.3f },
                new Spur { Name = "Jirón El Inca", X0 = -248f, Z0 = -8f, X1 = -304f, Z1 = -82f, Half = 3.3f },
                new Spur { Name = "Jirón Baños del Inca", X0 = 270f, Z0 = -52f, X1 = 322f, Z1 = 18f, Half = 3.3f },
                new Spur { Name = "Jirón José Olaya", X0 = 72f, Z0 = -286f, X1 = 16f, Z1 = -360f, Half = 3.3f },
                new Spur { Name = "Pasaje Argentina", X0 = -102f, Z0 = -408f, X1 = -48f, Z1 = -358f, Half = 2.6f },
            };
        }

        void LayMoreStreets()
        {
            if (plan == null || plan.transform.Find("CallesExtra") != null) return;
            var root = new GameObject("CallesExtra");
            root.transform.SetParent(plan.transform, false);
            LayAtrio(root.transform);
        }

        // Las vías del casco que el plano no trae se tienden aquí, con el
        // trazado de OpenStreetMap. Si el tramo ya es calle, no se duplica.
        bool CompletarCalles()
        {
            if (plan == null || !walkReady) return false;
            var set = LoadTramos();
            if (set == null || set.tramos == null || set.tramos.Length == 0) return false;
            Transform root = plan.transform.Find("CallesExtra");
            if (root == null)
            {
                var go = new GameObject("CallesExtra");
                go.transform.SetParent(plan.transform, false);
                root = go.transform;
            }

            var run = new List<Vector2>(16);
            string runName = null;
            int laid = 0;
            for (int i = 0; i < set.tramos.Length; i++)
            {
                var t = set.tramos[i];
                if (t == null || string.IsNullOrEmpty(t.name)) continue;
                bool cubre = TramoCubierto(t);
                bool sigue = runName == t.name && run.Count > 0
                    && (run[run.Count - 1] - new Vector2(t.x0, t.z0)).sqrMagnitude < 2.25f;
                if (cubre || !sigue)
                {
                    if (run.Count >= 2 && runName != null)
                    {
                        LayPoly(root, "Calles " + runName + " " + laid, run.ToArray(),
                            runName.StartsWith("Pasaje") ? 2.3f : 3.3f,
                            0.1f, "Calle/piso_empedrado", 0f);
                        laid++;
                    }

                    run.Clear();
                    runName = null;
                }

                if (cubre) continue;
                if (run.Count == 0)
                {
                    run.Add(new Vector2(t.x0, t.z0));
                    runName = t.name;
                }

                run.Add(new Vector2(t.x1, t.z1));
            }

            if (run.Count >= 2 && runName != null)
            {
                LayPoly(root, "Calles " + runName + " " + laid, run.ToArray(),
                    runName.StartsWith("Pasaje") ? 2.3f : 3.3f,
                    0.1f, "Calle/piso_empedrado", 0f);
                laid++;
            }

            return laid > 0;
        }

        // La avenida que el casco no trae: de Amazonas a la UNC y a Baños del Inca.
        bool TenderRutaBanos()
        {
            if (plan == null) return false;
            if (plan.transform.Find("RutaBanos") != null) return false;
            var root = new GameObject("RutaBanos");
            root.transform.SetParent(plan.transform, false);

            var via = new[]
            {
                new Vector2(320f, -95f),
                new Vector2(420f, -130f),
                new Vector2(540f, -165f),
                new Vector2(680f, -190f),
                new Vector2(800f, -205f),
                new Vector2(RutaHaciaBanos.UncX, RutaHaciaBanos.UncZ),
                new Vector2(1000f, -190f),
                new Vector2(1120f, -140f),
                new Vector2(1220f, -90f),
                new Vector2(RutaHaciaBanos.BanosX, RutaHaciaBanos.BanosZ),
            };

            LayPoly(root.transform, "Calles Avenida Atahualpa", via, 4.8f, 0.1f, "Calle/piso_empedrado", 0f);
            LayPoly(root.transform, "Banda Avenida Atahualpa", via, 7.4f, 0.08f, "Calle/piso_plaza", 0f);
            LayPad(root.transform, "Calles Campus UNC", RutaHaciaBanos.UncX, RutaHaciaBanos.UncZ,
                28f, 22f, 0.11f, "Calle/piso_empedrado", new Color(0.92f, 0.88f, 0.78f));
            LayPad(root.transform, "Cesped Campus UNC", RutaHaciaBanos.UncX, RutaHaciaBanos.UncZ - 2f,
                18f, 10f, 0.16f, "Bosque/piso_cesped", new Color(0.86f, 1f, 0.42f));
            LayPad(root.transform, "Calles Plaza Banos", RutaHaciaBanos.BanosX, RutaHaciaBanos.BanosZ,
                22f, 18f, 0.12f, "Calle/piso_plaza", new Color(0.94f, 0.86f, 0.68f));

            var acceso = new[]
            {
                new Vector2(RutaHaciaBanos.UncX, RutaHaciaBanos.UncZ + 18f),
                new Vector2(RutaHaciaBanos.UncX, RutaHaciaBanos.UncZ - 16f),
            };
            LayPoly(root.transform, "Calles Campus UNC Norte", acceso, 3.4f, 0.12f, "Calle/piso_empedrado", 0f);
            return true;
        }

        void LayPad(Transform parent, string name, float cx, float cz, float halfX, float halfZ,
            float lift, string texture, Color tinta)
        {
            int nx = Mathf.Max(2, Mathf.CeilToInt(halfX * 2f / 3.2f) + 1);
            int nz = Mathf.Max(2, Mathf.CeilToInt(halfZ * 2f / 3.2f) + 1);
            var verts = new Vector3[nx * nz];
            var uv = new Vector2[nx * nz];
            var colors = new Color[nx * nz];
            for (int z = 0; z < nz; z++)
            {
                float tz = nz == 1 ? 0.5f : z / (float)(nz - 1);
                float pz = cz + Mathf.Lerp(-halfZ, halfZ, tz);
                for (int x = 0; x < nx; x++)
                {
                    float tx = nx == 1 ? 0.5f : x / (float)(nx - 1);
                    float px = cx + Mathf.Lerp(-halfX, halfX, tx);
                    int i = z * nx + x;
                    verts[i] = new Vector3(px, CajamarcaAltura.Sample(px, pz) + lift, pz);
                    uv[i] = new Vector2(px / 4.2f, pz / 4.2f);
                    colors[i] = tinta;
                }
            }

            var tris = new int[(nx - 1) * (nz - 1) * 6];
            int cursor = 0;
            for (int z = 0; z < nz - 1; z++)
            {
                for (int x = 0; x < nx - 1; x++)
                {
                    int a = z * nx + x;
                    int b = a + 1;
                    int c = a + nx;
                    int d = c + 1;
                    tris[cursor++] = a; tris[cursor++] = c; tris[cursor++] = b;
                    tris[cursor++] = b; tris[cursor++] = c; tris[cursor++] = d;
                }
            }

            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var mesh = new Mesh { name = name };
            mesh.vertices = verts;
            mesh.uv = uv;
            mesh.colors = colors;
            mesh.triangles = tris;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            var filter = go.AddComponent<MeshFilter>();
            filter.sharedMesh = mesh;
            var renderer = go.AddComponent<MeshRenderer>();
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            bool lawn = texture != null && texture.IndexOf("cesped", System.StringComparison.OrdinalIgnoreCase) >= 0;
            var shader = Shader.Find("AYNI/Calle") ?? Shader.Find("Standard") ?? Shader.Find("Sprites/Default");
            if (shader != null) ApplyFloor(renderer, shader, RepeatTex(texture), lawn, tinta);
        }

        void AddRutaBanosLabels()
        {
            if (streetLabels == null) return;
            if (streetLabels.Find("Calle Avenida Atahualpa") != null) return;
            var ink = new Color(0.97f, 0.94f, 0.86f, 0.96f);
            AddStreetLabel(new StreetLabel
            {
                name = "Avenida Atahualpa",
                x = 620f,
                z = -178f,
                angle = -12f,
                size = 2.4f,
            }, ink);
            AddStreetLabel(new StreetLabel
            {
                name = "Universidad Nacional de Cajamarca",
                x = RutaHaciaBanos.UncX,
                z = RutaHaciaBanos.UncZ + 26f,
                angle = 0f,
                size = 2.8f,
            }, ink);
            AddStreetLabel(new StreetLabel
            {
                name = "Baños del Inca",
                x = RutaHaciaBanos.BanosX,
                z = RutaHaciaBanos.BanosZ + 22f,
                angle = 0f,
                size = 2.6f,
            }, ink);
        }

        static bool TramoCubierto(TramoFila t)
        {
            int hits = 0;
            for (int i = 0; i < 5; i++)
            {
                float u = (i + 0.5f) / 5f;
                float x = Mathf.Lerp(t.x0, t.x1, u);
                float z = Mathf.Lerp(t.z0, t.z1, u);
                if (current != null && current.OnStreet(x, z)) hits++;
            }

            return hits >= 4;
        }

        TramoSet LoadTramos()
        {
            var asset = Resources.Load<TextAsset>("Calle/tramos");
            if (asset == null) return null;
            try
            {
                return JsonUtility.FromJson<TramoSet>(asset.text);
            }
            catch (System.Exception)
            {
                return null;
            }
        }

        void LaySpur(Transform parent, Spur spur)
        {
            float dx = spur.X1 - spur.X0;
            float dz = spur.Z1 - spur.Z0;
            float len = Mathf.Sqrt(dx * dx + dz * dz);
            if (len < 8f) return;
            int pieces = Mathf.Max(1, Mathf.CeilToInt(len / 4f));
            int n = pieces + 1;
            var verts = new Vector3[n * 2];
            var uv = new Vector2[n * 2];
            var colors = new Color[n * 2];
            float traveled = 0f;
            float half = Mathf.Max(1.6f, spur.Half);
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)pieces;
                float x = spur.X0 + dx * t;
                float z = spur.Z0 + dz * t;
                float tm = Mathf.Max(0, i - 1) / (float)pieces;
                float tp = Mathf.Min(pieces, i + 1) / (float)pieces;
                float dirx = dx * (tp - tm);
                float dirz = dz * (tp - tm);
                float mag = Mathf.Sqrt(dirx * dirx + dirz * dirz);
                if (mag < 0.001f) mag = 1f;
                dirx /= mag;
                dirz /= mag;
                float sx = -dirz * half;
                float sz = dirx * half;
                if (i > 0)
                {
                    float px = spur.X0 + dx * ((i - 1) / (float)pieces);
                    float pz = spur.Z0 + dz * ((i - 1) / (float)pieces);
                    traveled += Mathf.Sqrt((x - px) * (x - px) + (z - pz) * (z - pz));
                }

                float yL = CajamarcaAltura.Sample(x + sx, z + sz) + 0.09f;
                float yR = CajamarcaAltura.Sample(x - sx, z - sz) + 0.09f;
                verts[i * 2] = new Vector3(x + sx, yL, z + sz);
                verts[i * 2 + 1] = new Vector3(x - sx, yR, z - sz);
                float v = traveled / Mathf.Max(1.5f, half * 2f);
                uv[i * 2] = new Vector2(0f, v);
                uv[i * 2 + 1] = new Vector2(1f, v);
                colors[i * 2] = Color.white;
                colors[i * 2 + 1] = Color.white;
            }

            var tris = new int[(n - 1) * 6];
            int cursor = 0;
            for (int i = 0; i < n - 1; i++)
            {
                int a = i * 2;
                int b = a + 1;
                int c = a + 2;
                int d = a + 3;
                tris[cursor++] = a; tris[cursor++] = b; tris[cursor++] = d;
                tris[cursor++] = a; tris[cursor++] = d; tris[cursor++] = c;
            }

            var go = new GameObject("Calles " + spur.Name);
            go.transform.SetParent(parent, false);
            var mesh = new Mesh { name = go.name };
            mesh.vertices = verts;
            mesh.uv = uv;
            mesh.colors = colors;
            mesh.triangles = tris;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            var filter = go.AddComponent<MeshFilter>();
            filter.sharedMesh = mesh;
            var renderer = go.AddComponent<MeshRenderer>();
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }

        void LayAtrio(Transform parent)
        {
            const float cx = -184f;
            const float cz = -386f;
            const int segments = 18;
            const float radius = 11f;
            var verts = new Vector3[segments + 2];
            var uv = new Vector2[verts.Length];
            var colors = new Color[verts.Length];
            float cy = CajamarcaAltura.Sample(cx, cz) + 0.14f;
            verts[0] = new Vector3(cx, cy, cz);
            uv[0] = new Vector2(0.5f, 0.5f);
            colors[0] = Color.white;
            for (int i = 0; i <= segments; i++)
            {
                float ang = i / (float)segments * Mathf.PI * 2f;
                float x = cx + Mathf.Cos(ang) * radius;
                float z = cz + Mathf.Sin(ang) * radius;
                verts[i + 1] = new Vector3(x, CajamarcaAltura.Sample(x, z) + 0.14f, z);
                uv[i + 1] = new Vector2(x / 4f, z / 4f);
                colors[i + 1] = Color.white;
            }

            var tris = new int[segments * 3];
            int t = 0;
            for (int i = 1; i <= segments; i++)
            {
                tris[t++] = 0;
                tris[t++] = i + 1;
                tris[t++] = i;
            }

            var go = new GameObject("Calles Atrio Santa Apolonia");
            go.transform.SetParent(parent, false);
            var mesh = new Mesh { name = go.name };
            mesh.vertices = verts;
            mesh.uv = uv;
            mesh.colors = colors;
            mesh.triangles = tris;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            var filter = go.AddComponent<MeshFilter>();
            filter.sharedMesh = mesh;
            var renderer = go.AddComponent<MeshRenderer>();
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }

        void AddSpurLabels()
        {
            if (plan == null) return;
            if (streetLabels == null)
            {
                var root = new GameObject("NombresCalles");
                root.transform.SetParent(plan.transform, false);
                streetLabels = root.transform;
            }

            var set = LoadTramos();
            if (set == null || set.rotulos == null) return;
            var ink = new Color(0.97f, 0.94f, 0.86f, 0.96f);
            for (int i = 0; i < set.rotulos.Length; i++)
            {
                var rotulo = set.rotulos[i];
                if (rotulo == null || string.IsNullOrEmpty(rotulo.name)) continue;
                if (RotuloCerca(rotulo)) continue;
                AddStreetLabel(rotulo, ink);
            }

            streetLabels.gameObject.SetActive(vista == Vista.Planta);
        }

        bool RotuloCerca(StreetLabel rotulo)
        {
            if (streetLabels == null) return false;
            for (int i = 0; i < streetLabels.childCount; i++)
            {
                var hijo = streetLabels.GetChild(i);
                if (hijo.name != "Calle " + rotulo.name) continue;
                Vector3 p = hijo.localPosition;
                float dx = p.x - rotulo.x;
                float dz = p.z - rotulo.z;
                if (dx * dx + dz * dz < 55f * 55f) return true;
            }

            return false;
        }

        void AddSpurPosts(Transform root)
        {
            if (root == null) return;
            var set = LoadTramos();
            if (set != null && set.rotulos != null)
            {
                for (int i = 0; i < set.rotulos.Length; i++)
                {
                    var rotulo = set.rotulos[i];
                    if (rotulo == null || string.IsNullOrEmpty(rotulo.name)) continue;
                    if (root.Find("Poste " + rotulo.name) != null) continue;
                    float rad = -rotulo.angle * Mathf.Deg2Rad;
                    float ox = -Mathf.Sin(rad) * 6.2f;
                    float oz = Mathf.Cos(rad) * 6.2f;
                    var corner = new Vector3(rotulo.x + ox, 0f, rotulo.z + oz);
                    corner.y = CajamarcaAltura.Sample(corner.x, corner.z);
                    BuildPost(root, corner, rotulo.name);
                }
            }

            if (root.Find("Poste Capilla Santa Apolonia") != null) return;
            var chapel = new Vector3(-174f, 0f, -374f);
            chapel.y = CajamarcaAltura.Sample(chapel.x, chapel.z);
            BuildPost(root, chapel, "Capilla Santa Apolonia");
        }

        static string[] CerroTrees()
        {
            return TreeKinds();
        }

        void BosqueDelCerro()
        {
            if (district == null || district.Find("BosqueDelCerro") != null) return;
            var root = new GameObject("BosqueDelCerro");
            root.transform.SetParent(district, false);
            var kinds = CerroTrees();
            const float sx = -184f;
            const float sz = -386f;
            float cima = CajamarcaAltura.Sample(sx, sz);
            var casas = HuellasDeCasas();
            int n = 0;
            int planted = 0;
            const float paso = 6.4f;
            for (float z = sz - 62f; z <= sz + 36f && planted < 130; z += paso)
            {
                for (float x = sx - 58f; x <= sx + 36f && planted < 130; x += paso)
                {
                    int salt = n++;
                    float jx = x + ((salt * 3) % 5 - 2) * 0.85f;
                    float jz = z + ((salt * 5) % 5 - 2) * 0.85f;
                    if (!SitioDeArbol(jx, jz, sx, sz, cima, casas)) continue;
                    float alto = 8.5f + (salt % 6) * 1.35f;
                    if (salt % 9 == 0) alto = 5.4f;
                    PlantTree(kinds[salt % kinds.Length], jx, jz, alto, salt * 41f,
                        CajamarcaAltura.Sample(jx, jz), root.transform);
                    planted++;
                }
            }
        }

        static readonly string[] FloresDelAsset =
        {
            "Coniferas/Prefabs/plants/SM_flower",
            "Coniferas/Prefabs/plants/SM_flower_2",
            "Coniferas/Prefabs/plants/SM_poppy",
            "Coniferas/Prefabs/plants/SM_chamomile",
            "Coniferas/Prefabs/plants/SM_dandelion",
            "Coniferas/Prefabs/plants/SM_field_bell",
            "Coniferas/Prefabs/plants/SM_forget-me-not",
        };

        void SembrarFloresDelCerro()
        {
            if (district == null) return;
            const float sx = -184f;
            const float sz = -386f;
            float cima = CajamarcaAltura.Sample(sx, sz);
            var pts = SubidaPts();
            Vector2 cimaPlano = pts[pts.Length - 1];
            Vector2 antes = pts[pts.Length - 2];
            Vector2 eje = cimaPlano - antes;
            if (eje.sqrMagnitude < 0.01f) eje = Vector2.up;
            eje.Normalize();
            Vector2 lado = new Vector2(-eje.y, eje.x);
            Vector2 puesto = cimaPlano + lado * 8.6f;
            puestoComida = new Vector3(puesto.x, 0f, puesto.y);
            if (district.Find("ComedorCerro") == null)
                PuestoDelCerro(puestoComida);
            else
                puestoListo = true;
            if (district.Find("FloresDelCerro") != null) return;
            var root = new GameObject("FloresDelCerro");
            root.transform.SetParent(district, false);
            int n = 0;
            int planted = 0;
            const float paso = 5.2f;
            for (float z = sz - 62f; z <= sz + 38f && planted < 240; z += paso)
            {
                for (float x = sx - 58f; x <= sx + 40f && planted < 240; x += paso)
                {
                    int salt = n++;
                    float jx = x + ((salt * 3) % 5 - 2) * 0.7f;
                    float jz = z + ((salt * 5) % 5 - 2) * 0.7f;
                    if (!SitioDeFlor(jx, jz, sx, sz, cima, puesto)) continue;
                    if (PlantarFlor(FloresDelAsset[salt % FloresDelAsset.Length], jx, jz, salt * 47f, root.transform))
                        planted++;
                }
            }

            if (planted > 0) return;
            for (float z = sz - 62f; z <= sz + 38f && floresCerro.Count < 240; z += paso)
            {
                for (float x = sx - 58f; x <= sx + 40f && floresCerro.Count < 240; x += paso)
                {
                    int salt = n++;
                    float jx = x + ((salt * 3) % 5 - 2) * 0.7f;
                    float jz = z + ((salt * 5) % 5 - 2) * 0.7f;
                    if (!SitioDeFlor(jx, jz, sx, sz, cima, puesto)) continue;
                    FlorCaja(root.transform, jx, jz, salt);
                }
            }
        }

        bool SitioDeFlor(float x, float z, float sx, float sz, float cima, Vector2 puesto)
        {
            float dx = x - sx;
            float dz = z - sz;
            if (dx * dx + dz * dz > 58f * 58f) return false;
            if (CajamarcaAltura.Sample(x, z) < cima - 70f) return false;
            if (OnStreet(x, z) || CercaDeLaSubida(x, z, 6.2f) || CercaDeEscalera(x, z, 3.2f)) return false;
            Vector3 face = new Vector3(-sx, 0f, -sz);
            if (face.sqrMagnitude < 1f) face = Vector3.forward;
            face.Normalize();
            float capX = sx - face.x * 7.5f;
            float capZ = sz - face.z * 7.5f;
            if ((x - capX) * (x - capX) + (z - capZ) * (z - capZ) < 12f * 12f) return false;
            float cruzX = sx - face.x * 26f;
            float cruzZ = sz - face.z * 26f;
            if ((x - cruzX) * (x - cruzX) + (z - cruzZ) * (z - cruzZ) < 5f * 5f) return false;
            if ((x + 200f) * (x + 200f) + (z + 376f) * (z + 376f) < 4.5f * 4.5f) return false;
            float px = puesto.x - x;
            float pz = puesto.y - z;
            return px * px + pz * pz >= 3.4f * 3.4f;
        }

        bool PlantarFlor(string resource, float x, float z, float yaw, Transform parent, bool recoger = true)
        {
            var prefab = Resources.Load<GameObject>(resource);
            if (prefab == null) return false;
            var flor = Instantiate(prefab, parent);
            flor.name = "flower";
            foreach (var col in flor.GetComponentsInChildren<Collider>(true))
                Kill(col);
            foreach (var pieza in flor.GetComponentsInChildren<Transform>(true))
            {
                if (pieza.name.IndexOf("flower", System.StringComparison.OrdinalIgnoreCase) < 0)
                    pieza.name = "flower";
            }

            flor.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
            float piso = CajamarcaAltura.Sample(x, z);
            flor.transform.localPosition = new Vector3(x, piso, z);
            flor.transform.localScale = Vector3.one;
            float native = CrateSpan(flor, out _);
            float alto = 0.40f + Mathf.Abs(yaw % 5f) * 0.025f;
            flor.transform.localScale = Vector3.one * (alto / Mathf.Max(0.05f, native));
            CrateSpan(flor, out float minY);
            Vector3 anchor = parent.TransformPoint(new Vector3(x, piso, z));
            flor.transform.position += new Vector3(0f, anchor.y + 0.02f - minY, 0f);
            foreach (var renderer in flor.GetComponentsInChildren<Renderer>(true))
            {
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = false;
            }

            if (recoger) floresCerro.Add(flor.transform);
            return true;
        }

        void FlorCaja(Transform parent, float x, float z, int salt, bool recoger = true)
        {
            var flor = new GameObject("flower");
            flor.transform.SetParent(parent, false);
            flor.transform.localPosition = new Vector3(x, CajamarcaAltura.Sample(x, z), z);
            Color[] petalos =
            {
                new Color(0.86f, 0.22f, 0.28f),
                new Color(0.95f, 0.78f, 0.22f),
                new Color(0.62f, 0.35f, 0.78f),
                new Color(0.95f, 0.55f, 0.25f),
            };
            BoxPart(flor.transform, "flower", new Vector3(0f, 0.22f, 0f), new Vector3(0.07f, 0.36f, 0.07f), null, new Color(0.22f, 0.42f, 0.16f));
            BoxPart(flor.transform, "flower", new Vector3(0f, 0.42f, 0f), new Vector3(0.4f, 0.1f, 0.4f), null, petalos[Mathf.Abs(salt) % petalos.Length]);
            if (recoger) floresCerro.Add(flor.transform);
        }

        void PuestoDelCerro(Vector3 at)
        {
            float g = CajamarcaAltura.Sample(at.x, at.z);
            var root = new GameObject("ComedorCerro");
            root.transform.SetParent(district, false);
            root.transform.position = new Vector3(at.x, g, at.z);
            var caja = root.AddComponent<BoxCollider>();
            caja.center = new Vector3(0f, 0.9f, 0f);
            caja.size = new Vector3(2.6f, 1.8f, 1.6f);
            var madera = new Color(0.45f, 0.28f, 0.14f);
            var tela = new Color(0.93f, 0.86f, 0.62f);
            var toldo = new Color(0.75f, 0.22f, 0.18f);
            BoxPart(root.transform, "MesaComida", new Vector3(0f, 0.78f, 0f), new Vector3(2.4f, 0.1f, 1.3f), null, madera);
            BoxPart(root.transform, "Mantel", new Vector3(0f, 0.86f, 0f), new Vector3(2.2f, 0.04f, 1.15f), null, tela);
            BoxPart(root.transform, "PataComida", new Vector3(-1.05f, 0.38f, -0.5f), new Vector3(0.1f, 0.76f, 0.1f), null, madera);
            BoxPart(root.transform, "PataComida", new Vector3(1.05f, 0.38f, -0.5f), new Vector3(0.1f, 0.76f, 0.1f), null, madera);
            BoxPart(root.transform, "PataComida", new Vector3(-1.05f, 0.38f, 0.5f), new Vector3(0.1f, 0.76f, 0.1f), null, madera);
            BoxPart(root.transform, "PataComida", new Vector3(1.05f, 0.38f, 0.5f), new Vector3(0.1f, 0.76f, 0.1f), null, madera);
            BoxPart(root.transform, "ToldoComida", new Vector3(0f, 1.9f, 0f), new Vector3(2.8f, 0.08f, 1.7f), null, toldo);
            BoxPart(root.transform, "PosteComedor", new Vector3(-1.25f, 1.05f, -0.72f), new Vector3(0.1f, 1.7f, 0.1f), null, madera);
            BoxPart(root.transform, "PosteComedor", new Vector3(1.25f, 1.05f, -0.72f), new Vector3(0.1f, 1.7f, 0.1f), null, madera);
            var letrero = new GameObject("LetreroComida");
            letrero.transform.SetParent(root.transform, false);
            letrero.transform.localPosition = new Vector3(0f, 1.55f, 0.72f);
            var mesh = letrero.AddComponent<TextMesh>();
            mesh.text = "COMIDA";
            mesh.fontSize = 64;
            mesh.characterSize = 0.045f;
            mesh.anchor = TextAnchor.MiddleCenter;
            mesh.color = new Color(0.98f, 0.94f, 0.86f);
            var fuente = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (fuente != null)
            {
                mesh.font = fuente;
                var tinta = letrero.GetComponent<MeshRenderer>();
                if (tinta != null) tinta.sharedMaterial = fuente.material;
            }

            puestoListo = true;
        }

        void PasoFloresCerro(Walker hero)
        {
            for (int i = 0; i < floresCerro.Count; i++)
            {
                var flor = floresCerro[i];
                if (flor == null || !flor.gameObject.activeSelf) continue;
                Vector3 sitio = district != null ? district.InverseTransformPoint(flor.position) : flor.localPosition;
                if (DistanciaPlana(hero.Local, sitio) > 1.35f) continue;
                flor.gameObject.SetActive(false);
                plata += 2;
                AlforjaDelCamino.Guardar("flor-cerro", "Flor del cerro", LeyDelMonte.AlforjaFlorCerro);
                PintarSignos();
                if (!dijoFlorCerro)
                {
                    dijoFlorCerro = true;
                    Note(LeyDelMonte.FlorCerro);
                }

                return;
            }
        }

        void PasoComida(Walker hero)
        {
            if (!puestoListo) return;
            Vector3 gap = hero.Local - puestoComida;
            gap.y = 0f;
            float cerca = gap.sqrMagnitude;
            bool junto = cerca < 7f * 7f;
            if (junto && !juntoComida)
                Note(LeyDelMonte.MenuCerro);
            juntoComida = junto;
            if (!junto && cerca > 14f * 14f) return;
            int cual = TeclaComida();
            if (cual < 0) return;
            if (cerca > 3.6f * 3.6f)
            {
                Note(LeyDelMonte.LejosComida);
                return;
            }

            if (comioTurno)
            {
                Note(LeyDelMonte.YaComiste);
                return;
            }

            int[] precios = { 10, 6, 14, 8 };
            string[] dichas =
            {
                LeyDelMonte.ComioFrito,
                LeyDelMonte.ComioTigre,
                LeyDelMonte.ComioCuy,
                LeyDelMonte.ComioCaldo,
            };
            if (plata < precios[cual])
            {
                Note(LeyDelMonte.FaltaPlata);
                return;
            }

            plata -= precios[cual];
            comioTurno = true;
            comidasDia = Mathf.Min(3, comidasDia + 1);
            vida = Mathf.Min(100f, vida + 8f);
            PintarSignos();
            if (comidasDia >= 3 && !diaCompleto)
            {
                diaCompleto = true;
                Note(dichas[cual] + " " + LeyDelMonte.DiaCompleto);
            }
            else
                Note(dichas[cual]);
        }

        static int TeclaComida()
        {
            if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1)) return 0;
            if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2)) return 1;
            if (Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Keypad3)) return 2;
            if (Input.GetKeyDown(KeyCode.Alpha4) || Input.GetKeyDown(KeyCode.Keypad4)) return 3;
            return -1;
        }

        void PasoReloj()
        {
            if (!puestoListo) return;
            const float tramo = 180f;
            if (!anuncioComida)
            {
                anuncioComida = true;
                Note(LeyDelMonte.TocaManana);
            }

            relojDia += Time.deltaTime;
            if (relojDia >= tramo * 3f)
            {
                bool fallo = !comioTurno;
                string castigo = fallo ? CastigoComida() : null;
                relojDia = 0f;
                turnoComida = 0;
                comidasDia = 0;
                comioTurno = false;
                diaCompleto = false;
                PintarSignos();
                Note(fallo ? castigo + " " + LeyDelMonte.TocaManana : LeyDelMonte.DiaNuevo);
                return;
            }

            int turno = Mathf.FloorToInt(relojDia / tramo);
            if (turno == turnoComida) return;
            string sigue = turno == 1 ? LeyDelMonte.TocaAlmuerzo : LeyDelMonte.TocaCena;
            string culpa = comioTurno ? null : CastigoComida();
            turnoComida = turno;
            comioTurno = false;
            Note(culpa == null ? sigue : culpa + " " + sigue);
        }

        string CastigoComida()
        {
            vida = Mathf.Max(0f, vida - 12f);
            bool vacia = vida <= 0f;
            if (vacia) vida = 100f;
            PintarSignos();
            return vacia ? LeyDelMonte.HambreCero : LeyDelMonte.SaltoComida;
        }

        bool SitioDeArbol(float x, float z, float sx, float sz, float cima, List<Vector2> casas)
        {
            float dx = x - sx;
            float dz = z - sz;
            float alcance = dx * dx + dz * dz;
            if (alcance < 18f * 18f || alcance > 60f * 60f) return false;
            if (CajamarcaAltura.Sample(x, z) < cima - 72f) return false;
            if (OnStreet(x, z) || CercaDeLaSubida(x, z, 12f) || CercaDeEscalera(x, z, 5.5f)) return false;
            float ax = x + 200f;
            float az = z + 376f;
            if (ax * ax + az * az < 7f * 7f) return false;
            for (int i = 0; i < casas.Count; i++)
            {
                float hx = casas[i].x - x;
                float hz = casas[i].y - z;
                if (hx * hx + hz * hz < 8f * 8f) return false;
            }

            return true;
        }

        List<Vector2> HuellasDeCasas()
        {
            var lista = new List<Vector2>();
            Transform casas = district != null ? district.Find("Casas") : null;
            if (casas == null) return lista;
            foreach (Transform casa in casas)
            {
                var rend = casa.GetComponentInChildren<Renderer>();
                if (rend == null) continue;
                Vector3 p = district.InverseTransformPoint(rend.bounds.center);
                lista.Add(new Vector2(p.x, p.z));
            }

            return lista;
        }

        static bool CercaDeLaSubida(float x, float z, float radio)
        {
            var pts = SubidaPts();
            if (pts.Length < 2) return false;
            float limite = radio * radio;
            var p = new Vector2(x, z);
            for (int i = 1; i < pts.Length; i++)
            {
                if (DistanciaAlSegmento(p, pts[i - 1], pts[i]) < limite) return true;
            }

            Vector2 end = pts[pts.Length - 1];
            Vector2 dir = end - pts[pts.Length - 2];
            if (dir.sqrMagnitude < 0.01f) return false;
            dir.Normalize();
            return DistanciaAlSegmento(p, end, end + dir * 42f) < limite;
        }

        static bool CercaDeEscalera(float x, float z, float radio)
        {
            if (tramosEscalera.Count == 0) return false;
            float limite = radio * radio;
            var p = new Vector2(x, z);
            for (int t = 0; t < tramosEscalera.Count; t++)
            {
                var pts = tramosEscalera[t];
                if (pts == null || pts.Length < 2) continue;
                for (int i = 1; i < pts.Length; i++)
                {
                    if (DistanciaAlSegmento(p, pts[i - 1], pts[i]) < limite) return true;
                }
            }

            return false;
        }

        static float DistanciaAlSegmento(Vector2 p, Vector2 a, Vector2 b)
        {
            Vector2 ab = b - a;
            float len2 = ab.sqrMagnitude;
            float t = len2 < 0.001f ? 0f : Mathf.Clamp01(Vector2.Dot(p - a, ab) / len2);
            return (a + ab * t - p).sqrMagnitude;
        }

        void RaiseChapel()
        {
            if (district == null) return;
            var existente = district.Find("SantaApolonia");
            if (existente != null)
            {
                var puesta = existente.Find("Capilla");
                if (puesta != null) CambiarCapilla(puesta, 12.6f, 7f);
                return;
            }

            if (PonerCapillaDelCerro()) return;

            const float sx = -184f;
            const float sz = -386f;
            float ground = CajamarcaAltura.Sample(sx, sz);
            var face = new Vector3(-sx, 0f, -sz);
            if (face.sqrMagnitude < 1f) face = Vector3.forward;
            face.Normalize();

            var root = new GameObject("SantaApolonia");
            root.transform.SetParent(district, false);

            var chapel = new GameObject("Capilla");
            chapel.transform.SetParent(root.transform, false);
            chapel.transform.localPosition = new Vector3(sx, ground + 0.28f, sz) - face * 7.5f;
            chapel.transform.localRotation = Quaternion.LookRotation(face, Vector3.up);

            const float w = 9.2f;
            const float len = 14f;
            const float h = 6.6f;
            var white = new Color(0.97f, 0.95f, 0.98f);
            BoxPart(chapel.transform, "Nave", new Vector3(0f, h * 0.5f, 0f), new Vector3(w, h, len), "Calle/muro_adobe", white);
            var roof = new GameObject("Techo");
            roof.transform.SetParent(chapel.transform, false);
            roof.transform.localPosition = new Vector3(0f, h, 0f);
            var roofMesh = GableRoof(w + 1.2f, len + 1.4f, 2.4f);
            roof.AddComponent<MeshFilter>().sharedMesh = roofMesh;
            roof.AddComponent<MeshRenderer>();
            PaintPart(roof, "Calle/teja", Color.white);
            BoxPart(chapel.transform, "Campanario", new Vector3(0f, h + 2.1f, len * 0.5f - 0.2f), new Vector3(3.2f, 3.8f, 0.6f), "Calle/muro_adobe", white);
            BoxPart(chapel.transform, "CruzTecho", new Vector3(0f, h + 1.35f, len * 0.5f - 0.35f), new Vector3(0.22f, 2.5f, 0.22f), "Calle/piedra", Color.white);
            BoxPart(chapel.transform, "CruzTechoBrazo", new Vector3(0f, h + 2.15f, len * 0.5f - 0.35f), new Vector3(1.35f, 0.18f, 0.18f), "Calle/piedra", Color.white);
            Vector3 cruz = new Vector3(sx, 0f, sz) - face * 26f;
            cruz.y = CajamarcaAltura.Sample(cruz.x, cruz.z);
            BoxPart(root.transform, "CruzCerro", cruz + Vector3.up * 9.2f, new Vector3(0.62f, 18.4f, 0.62f), "Calle/piedra", Color.white);
            BoxPart(root.transform, "CruzCerroBrazo", cruz + Vector3.up * 15.6f, new Vector3(7.4f, 0.48f, 0.48f), "Calle/piedra", Color.white);
            BoxPart(chapel.transform, "PuertaCapilla", new Vector3(0f, 1.7f, len * 0.5f + 0.06f), new Vector3(2.4f, 3.4f, 0.16f), null, new Color(0.22f, 0.16f, 0.12f));
            DressChapelCrown(chapel.transform, w, h, len);
            RaiseAntenna(root.transform);
        }

        // La puerta del modelo mira a +Z y el origen está en la base.
        // frenteZ es dónde queda la punta de la grada, en el espacio de la capilla.
        bool PonerCapillaDelCerro()
        {
            if (Resources.Load<GameObject>("Calle/Iglesia/Iglesia_LowPoly") == null) return false;

            var pts = SubidaPts();
            Vector2 arriba = pts[pts.Length - 1];
            Vector2 antes = pts[pts.Length - 2];
            var bajada = new Vector3(antes.x - arriba.x, 0f, antes.y - arriba.y);
            if (bajada.sqrMagnitude < 0.01f) bajada = Vector3.forward;
            bajada.Normalize();

            var root = new GameObject("SantaApolonia");
            root.transform.SetParent(district, false);
            var chapel = new GameObject("Capilla");
            chapel.transform.SetParent(root.transform, false);
            float suelo = CajamarcaAltura.Sample(arriba.x, arriba.y);
            chapel.transform.localPosition = new Vector3(arriba.x, suelo, arriba.y);
            chapel.transform.localRotation = Quaternion.LookRotation(bajada, Vector3.up);

            if (!ColocarIglesia(chapel.transform, 12.6f, 0f))
            {
                Kill(root);
                return false;
            }

            Vector3 cruz = new Vector3(arriba.x, 0f, arriba.y) - bajada * 22f;
            cruz.y = CajamarcaAltura.Sample(cruz.x, cruz.z);
            BoxPart(root.transform, "CruzCerro", cruz + Vector3.up * 9.2f, new Vector3(0.62f, 18.4f, 0.62f), "Calle/piedra", Color.white);
            BoxPart(root.transform, "CruzCerroBrazo", cruz + Vector3.up * 15.6f, new Vector3(7.4f, 0.48f, 0.48f), "Calle/piedra", Color.white);
            RaiseAntenna(root.transform);
            return true;
        }

        public static void CambiarCapilla(Transform capilla, float alto, float frenteZ)
        {
            if (capilla == null || capilla.Find("Iglesia") != null) return;
            if (!ColocarIglesia(capilla, alto, frenteZ)) return;
            for (int i = 0; i < capilla.childCount; i++)
            {
                var hijo = capilla.GetChild(i);
                if (hijo.name == "Iglesia") continue;
                hijo.gameObject.SetActive(false);
            }
        }

        static bool ColocarIglesia(Transform ancla, float alto, float frenteZ)
        {
            var prefab = Resources.Load<GameObject>("Calle/Iglesia/Iglesia_LowPoly");
            if (prefab == null || ancla == null) return false;

            var modelo = Object.Instantiate(prefab, ancla);
            modelo.name = "Iglesia";
            modelo.transform.localPosition = Vector3.zero;
            modelo.transform.localRotation = Quaternion.identity;
            modelo.transform.localScale = Vector3.one;

            var tex = Resources.Load<Texture2D>("Calle/Iglesia/Iglesia_LowPoly_BaseColor");
            var shader = Shader.Find("AYNI/Calle") ?? Shader.Find("Unlit/Texture") ?? Shader.Find("Standard");
            if (shader != null && tex != null)
            {
                tex.wrapMode = TextureWrapMode.Clamp;
                var mat = new Material(shader);
                mat.mainTexture = tex;
                var renders = modelo.GetComponentsInChildren<Renderer>(true);
                for (int i = 0; i < renders.Length; i++)
                {
                    renders[i].sharedMaterial = mat;
                    renders[i].shadowCastingMode = ShadowCastingMode.Off;
                    renders[i].receiveShadows = false;
                }
            }

            if (!LimitesIglesia(modelo.transform, ancla, out var min, out var max))
            {
                Object.Destroy(modelo);
                return false;
            }

            float mide = max.y - min.y;
            if (mide < 0.001f)
            {
                Object.Destroy(modelo);
                return false;
            }

            modelo.transform.localScale = Vector3.one * (alto / mide);
            if (!LimitesIglesia(modelo.transform, ancla, out min, out max))
            {
                Object.Destroy(modelo);
                return false;
            }

            modelo.transform.localPosition += new Vector3(
                -(min.x + max.x) * 0.5f,
                -min.y,
                frenteZ - max.z);
            return true;
        }

        static bool LimitesIglesia(Transform modelo, Transform ancla, out Vector3 min, out Vector3 max)
        {
            min = Vector3.zero;
            max = Vector3.zero;
            var filtros = modelo.GetComponentsInChildren<MeshFilter>(true);
            bool alguno = false;
            Vector3 lo = Vector3.zero;
            Vector3 hi = Vector3.zero;
            for (int i = 0; i < filtros.Length; i++)
            {
                var mesh = filtros[i].sharedMesh;
                if (mesh == null) continue;
                Vector3 c = mesh.bounds.center;
                Vector3 e = mesh.bounds.extents;
                for (int xi = -1; xi <= 1; xi += 2)
                for (int yi = -1; yi <= 1; yi += 2)
                for (int zi = -1; zi <= 1; zi += 2)
                {
                    Vector3 mundo = filtros[i].transform.TransformPoint(c + Vector3.Scale(e, new Vector3(xi, yi, zi)));
                    Vector3 local = ancla.InverseTransformPoint(mundo);
                    if (!alguno)
                    {
                        lo = hi = local;
                        alguno = true;
                    }
                    else
                    {
                        lo = Vector3.Min(lo, local);
                        hi = Vector3.Max(hi, local);
                    }
                }
            }

            min = lo;
            max = hi;
            return alguno;
        }

        void DressChapelCrown(Transform chapel, float w, float h, float len)
        {
            var white = new Color(0.97f, 0.95f, 0.98f);
            var blue = new Color(0.45f, 0.66f, 0.88f);
            var blueDeep = new Color(0.30f, 0.48f, 0.76f);
            float front = len * 0.5f;
            BoxPart(chapel, "FranjaFrente", new Vector3(0f, h * 0.78f, front + 0.08f), new Vector3(w + 0.35f, 0.5f, 0.12f), null, blue);
            BoxPart(chapel, "FranjaDer", new Vector3(w * 0.5f + 0.08f, h * 0.78f, 0f), new Vector3(0.12f, 0.5f, len), null, blue);
            BoxPart(chapel, "FranjaIzq", new Vector3(-w * 0.5f - 0.08f, h * 0.78f, 0f), new Vector3(0.12f, 0.5f, len), null, blue);

            float tw = 2.55f;
            float th = h + 7.2f;
            float tx = w * 0.5f - 0.15f;
            float tz = front - 1.15f;
            BoxPart(chapel, "TorreDer", new Vector3(tx, th * 0.5f, tz), new Vector3(tw, th, tw), "Calle/muro_adobe", white);
            BoxPart(chapel, "TorreIzq", new Vector3(-tx, th * 0.5f, tz), new Vector3(tw, th, tw), "Calle/muro_adobe", white);
            BoxPart(chapel, "CapitelDer", new Vector3(tx, th + 0.75f, tz), new Vector3(tw + 0.35f, 1.35f, tw + 0.35f), null, blue);
            BoxPart(chapel, "CapitelIzq", new Vector3(-tx, th + 0.75f, tz), new Vector3(tw + 0.35f, 1.35f, tw + 0.35f), null, blue);
            BoxPart(chapel, "ArcoDer", new Vector3(tx, 3.4f, tz + tw * 0.5f + 0.05f), new Vector3(1.15f, 2.6f, 0.12f), null, new Color(0.16f, 0.2f, 0.28f));
            BoxPart(chapel, "ArcoIzq", new Vector3(-tx, 3.4f, tz + tw * 0.5f + 0.05f), new Vector3(1.15f, 2.6f, 0.12f), null, new Color(0.16f, 0.2f, 0.28f));
            BoxPart(chapel, "Dintel", new Vector3(0f, 3.55f, front + 0.14f), new Vector3(3.2f, 0.42f, 0.16f), null, blueDeep);
            BoxPart(chapel, "JambaDer", new Vector3(1.4f, 1.7f, front + 0.14f), new Vector3(0.32f, 3.5f, 0.16f), null, blueDeep);
            BoxPart(chapel, "JambaIzq", new Vector3(-1.4f, 1.7f, front + 0.14f), new Vector3(0.32f, 3.5f, 0.16f), null, blueDeep);
            BoxPart(chapel, "TechoCampana", new Vector3(0f, h + 4.2f, front - 0.2f), new Vector3(3.6f, 0.42f, 0.95f), null, blue);

            var dome = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            dome.name = "Cupula";
            Kill(dome.GetComponent<Collider>());
            dome.transform.SetParent(chapel, false);
            dome.transform.localPosition = new Vector3(0f, h + 2.7f, 0.2f);
            dome.transform.localScale = new Vector3(4.6f, 3.2f, 4.6f);
            PaintPart(dome, null, blue);
            BoxPart(chapel, "Linterna", new Vector3(0f, h + 4.7f, 0.2f), new Vector3(0.7f, 1.35f, 0.7f), "Calle/muro_adobe", white);
            BoxPart(chapel, "LinternaAzul", new Vector3(0f, h + 5.5f, 0.2f), new Vector3(1.05f, 0.32f, 1.05f), null, blueDeep);
        }

        void RaiseAntenna(Transform parent)
        {
            const float x = -200f;
            const float z = -376f;
            float g = CajamarcaAltura.Sample(x, z);
            var steel = new Color(0.72f, 0.74f, 0.76f);
            BoxPart(parent, "AntenaMastil", new Vector3(x, g + 12f, z), new Vector3(0.28f, 24f, 0.28f), null, steel);
            BoxPart(parent, "AntenaMastilB", new Vector3(x + 0.85f, g + 9.5f, z), new Vector3(0.16f, 19f, 0.16f), null, steel);
            BoxPart(parent, "AntenaMastilC", new Vector3(x - 0.7f, g + 8.5f, z + 0.55f), new Vector3(0.16f, 17f, 0.16f), null, steel);
            for (int i = 0; i < 6; i++)
            {
                float y = g + 3.2f + i * 3.5f;
                BoxPart(parent, "AntenaCruz", new Vector3(x + 0.08f, y, z + 0.12f), new Vector3(2.1f, 0.1f, 0.1f), null, steel);
                BoxPart(parent, "AntenaCruzZ", new Vector3(x, y + 0.8f, z), new Vector3(0.1f, 0.1f, 1.6f), null, steel);
            }
        }

        GameObject BoxPart(Transform parent, string name, Vector3 localPos, Vector3 scale, string texture, Color tint)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            Kill(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = scale;
            PaintPart(go, texture, tint);
            return go;
        }

        void PaintPart(GameObject go, string resource, Color tint)
        {
            var renderer = go.GetComponent<Renderer>();
            if (renderer == null) return;
            var shader = Shader.Find("AYNI/Calle") ?? Shader.Find("Standard") ?? Shader.Find("Sprites/Default");
            if (shader == null) return;
            var tex = string.IsNullOrEmpty(resource) ? null : RepeatTex(resource);
            var mat = new Material(shader);
            mat.mainTexture = tex != null ? tex : Swatch(tint);
            renderer.sharedMaterial = mat;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            var filter = go.GetComponent<MeshFilter>();
            if (filter == null || filter.sharedMesh == null || !filter.sharedMesh.isReadable) return;
            var mesh = filter.mesh;
            var colors = new Color[mesh.vertexCount];
            Color ink = tex != null ? tint : Color.white;
            for (int i = 0; i < colors.Length; i++) colors[i] = ink;
            mesh.colors = colors;
        }

        static Mesh GableRoof(float width, float length, float rise)
        {
            float hx = width * 0.5f;
            float hz = length * 0.5f;
            var mesh = new Mesh { name = "TechoCapilla" };
            mesh.vertices = new[]
            {
                new Vector3(-hx, 0f, hz),
                new Vector3(hx, 0f, hz),
                new Vector3(hx, 0f, -hz),
                new Vector3(-hx, 0f, -hz),
                new Vector3(0f, rise, hz),
                new Vector3(0f, rise, -hz),
            };
            mesh.uv = new[]
            {
                new Vector2(0f, 0f),
                new Vector2(1f, 0f),
                new Vector2(1f, 1f),
                new Vector2(0f, 1f),
                new Vector2(0.5f, 0f),
                new Vector2(0.5f, 1f),
            };
            mesh.triangles = new[]
            {
                0, 4, 3, 3, 4, 5,
                1, 2, 4, 2, 5, 4,
                0, 1, 4,
                3, 5, 2,
            };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        void ApplyFloor(Renderer renderer, Shader shader, Texture2D tex, bool lawn, Color tint)
        {
            if (renderer == null) return;
            var mat = new Material(shader);
            mat.mainTexture = tex != null ? tex : Swatch(lawn ? new Color(0.42f, 0.68f, 0.28f) : Color.white);
            int slots = renderer.sharedMaterials != null ? renderer.sharedMaterials.Length : 1;
            if (slots < 1) slots = 1;
            var mats = new Material[slots];
            for (int i = 0; i < slots; i++) mats[i] = mat;
            renderer.sharedMaterials = mats;

            var filter = renderer.GetComponent<MeshFilter>();
            if (filter == null || filter.sharedMesh == null || !filter.sharedMesh.isReadable) return;
            var mesh = filter.mesh;
            int n = mesh.vertexCount;
            if (n == 0) return;
            var colors = new Color[n];
            Color wash = lawn ? new Color(0.55f, 0.82f, 0.32f) : (tint.a > 0.01f ? tint : Color.white);
            for (int i = 0; i < n; i++) colors[i] = wash;
            mesh.colors = colors;
        }

        void ApplyFloor(Renderer renderer, Shader shader, Texture2D tex, bool lawn)
        {
            ApplyFloor(renderer, shader, tex, lawn, Color.white);
        }

        static Texture2D RepeatTex(string resource)
        {
            var tex = CalleTex(resource);
            if (tex == null) return null;
            tex.wrapMode = resource == "Calle/piso_empedrado"
                ? TextureWrapMode.Mirror
                : TextureWrapMode.Repeat;
            tex.filterMode = FilterMode.Bilinear;
            return tex;
        }

        /// <summary>
        /// Fotogrametría CC0 en Calle/real/ si está importada; si no, el tile pintado.
        /// </summary>
        static Texture2D CalleTex(string resource)
        {
            if (string.IsNullOrEmpty(resource)) return null;
            if (resource.StartsWith("Calle/", System.StringComparison.Ordinal)
                && resource.IndexOf("/real/", System.StringComparison.Ordinal) < 0
                && resource.IndexOf("/Iglesia/", System.StringComparison.Ordinal) < 0
                && resource.IndexOf("/Puestos/", System.StringComparison.Ordinal) < 0
                && resource.IndexOf("/Gallina/", System.StringComparison.Ordinal) < 0
                && resource.IndexOf("/Props/", System.StringComparison.Ordinal) < 0)
            {
                int slash = resource.LastIndexOf('/');
                string file = slash >= 0 ? resource.Substring(slash + 1) : resource;
                // La plaza de Cajamarca es losa clara y sillería beige, no el
                // empedrado sucio ni la roca de acantilado de Poly Haven.
                if (file == "piso_plaza" || file == "piedra")
                    return Resources.Load<Texture2D>(resource);
                var real = Resources.Load<Texture2D>("Calle/real/" + file);
                if (real != null) return real;
            }

            return Resources.Load<Texture2D>(resource);
        }

        void RaiseCornerPosts()
        {
            if (plan == null || plan.transform.Find("Postes") != null) return;
            var set = LoadStreetNames();
            if (set == null || set.streets == null) return;

            var root = new GameObject("Postes");
            root.transform.SetParent(plan.transform, false);
            var seen = new Dictionary<string, Vector3>();
            for (int i = 0; i < set.streets.Length; i++)
            {
                var data = set.streets[i];
                if (data == null || string.IsNullOrEmpty(data.name)) continue;
                Vector3 corner = StreetCorner(data);
                if (seen.TryGetValue(data.name, out var prev)
                    && (prev - corner).sqrMagnitude < 22f * 22f)
                    continue;
                seen[data.name] = corner;
                corner.y = CajamarcaAltura.Sample(corner.x, corner.z);
                BuildPost(root.transform, corner, data.name);
            }

            AddSpurPosts(root.transform);
        }

        Vector3 StreetCorner(StreetLabel data)
        {
            float rad = -data.angle * Mathf.Deg2Rad;
            var along = new Vector3(Mathf.Cos(rad), 0f, Mathf.Sin(rad));
            var side = new Vector3(-along.z, 0f, along.x);
            var origin = new Vector3(data.x, 0f, data.z);
            var a = origin + side * 6.5f;
            var b = origin - side * 6.5f;
            return OnStreet(b.x, b.z) && !OnStreet(a.x, a.z) ? a : b;
        }

        void BuildPost(Transform parent, Vector3 local, string title)
        {
            string visible = NombreVisible(title);
            var root = new GameObject("Poste " + title);
            root.transform.SetParent(parent, false);
            root.transform.localPosition = local;
            float unit = plan != null ? plan.transform.lossyScale.x : 1f;
            if (unit < 0.05f) unit = 1f;
            root.transform.localScale = Vector3.one / unit;
            var marca = root.AddComponent<CalleEditable>();
            marca.Origen = title;

            var pole = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pole.name = "Palo";
            Kill(pole.GetComponent<Collider>());
            pole.transform.SetParent(root.transform, false);
            pole.transform.localPosition = new Vector3(0f, 1.3f, 0f);
            pole.transform.localScale = new Vector3(0.16f, 2.6f, 0.16f);
            TintMesh(pole, new Color(0.42f, 0.26f, 0.13f));

            var sign = new GameObject("Letrero");
            sign.transform.SetParent(root.transform, false);
            sign.transform.localPosition = new Vector3(0f, 2.45f, 0f);
            float width = Mathf.Clamp(0.16f * visible.Length, 1.8f, 5.6f);

            var board = GameObject.CreatePrimitive(PrimitiveType.Cube);
            board.name = "Tabla";
            Kill(board.GetComponent<Collider>());
            board.transform.SetParent(sign.transform, false);
            board.transform.localScale = new Vector3(width, 0.48f, 0.07f);
            TintMesh(board, new Color(0.95f, 0.91f, 0.78f));

            AddSignText(sign.transform, visible, -0.05f, 0f);
            AddSignText(sign.transform, visible, 0.05f, 180f);
            sign.AddComponent<PosteEsquina>();
        }

        static void AddSignText(Transform parent, string title, float z, float yaw)
        {
            var go = new GameObject(z < 0f ? "Nombre" : "NombreAtras");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3(0f, 0f, z);
            go.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
            var text = go.AddComponent<TextMesh>();
            text.text = title;
            text.font = Storybook.DisplayFont;
            text.fontSize = 48;
            text.characterSize = 0.055f;
            text.anchor = TextAnchor.MiddleCenter;
            text.alignment = TextAlignment.Center;
            text.fontStyle = FontStyle.Bold;
            text.richText = false;
            text.color = new Color(0.16f, 0.09f, 0.05f);
            var fit = go.AddComponent<StreetLabelFit>();
            fit.Target = 0.28f;
            var mesh = go.GetComponent<MeshRenderer>();
            if (mesh == null) return;
            if (text.font != null) mesh.sharedMaterial = text.font.material;
            mesh.shadowCastingMode = ShadowCastingMode.Off;
            mesh.receiveShadows = false;
        }

        void TintMesh(GameObject go, Color color)
        {
            var renderer = go.GetComponent<Renderer>();
            if (renderer == null) return;
            var shader = Shader.Find("AYNI/Calle") ?? Shader.Find("Standard") ?? Shader.Find("Sprites/Default");
            if (shader == null) return;
            var mat = new Material(shader);
            mat.mainTexture = Swatch(color);
            mat.color = color;
            renderer.sharedMaterial = mat;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }

        StreetLabelSet LoadStreetNames()
        {
            var asset = Resources.Load<TextAsset>("Calle/calles");
            if (asset == null) return null;
            try
            {
                return JsonUtility.FromJson<StreetLabelSet>(asset.text);
            }
            catch (System.Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// El cesped horneado es un disco plano a 0,16 m y el piso de la plaza es
        /// una malla ondulada: donde el piso baja, el cesped asoma en manchas
        /// verdes sobre el empedrado. Se oculta para dejar la plaza pareja.
        /// </summary>
        void HideBuriedLawn()
        {
            if (district == null || plan == null || lawnHidden) return;
            var lawn = district.Find("Cesped");
            if (lawn == null) return;
            var filter = lawn.GetComponent<MeshFilter>();
            if (filter == null || filter.sharedMesh == null) return;
            lawnHidden = true;
            var ink = lawn.GetComponent<MeshRenderer>();
            if (ink != null) ink.enabled = false;
        }

        // Capítulo I. La promesa se cumple en la plaza y en San Francisco:
        // tres flores, la ofrenda a cuestas y dos velas en la puerta.
        void VestirPromesa()
        {
            Transform parent = RuntimeParent;
            if (parent == null || parent.Find("Promesa") != null) return;
            var root = new GameObject("Promesa");
            root.transform.SetParent(parent, false);
            promesaPuerta = hasDoor ? sanFranciscoDoor : new Vector3(fountainX, 0f, fountainZ);

            if (plazaFound && plazaHalfV > 20f)
            {
                float sur = -(plazaHalfV - PlazaWalk - 4f);
                PonerFlor(root.transform, 9f, sur, new Color(0.82f, 0.22f, 0.28f));
                PonerFlor(root.transform, -13f, sur - 1.6f, new Color(0.95f, 0.78f, 0.22f));
                PonerFlor(root.transform, 18f, sur + 2.2f, new Color(0.93f, 0.48f, 0.68f));
                Vector2 carga = PlazaWorld(-5.5f, -plazaHalfV + PlazaWalk * 0.42f);
                PonerOfrenda(root.transform, carga.x, carga.y);
            }
            else
            {
                PonerFlorMundo(root.transform, 6f, -12f, new Color(0.82f, 0.22f, 0.28f));
                PonerFlorMundo(root.transform, -7f, -14f, new Color(0.95f, 0.78f, 0.22f));
                PonerFlorMundo(root.transform, 12f, -16f, new Color(0.93f, 0.48f, 0.68f));
                PonerOfrenda(root.transform, -4f, -8f);
            }

            PonerVelas(root.transform);
            RecordarPromesa();
        }

        void RecordarPromesa()
        {
            if (!PartidaDelCamino.PromesaHecha) return;
            promesaHecha = true;
            flores = 3;
            velas = 2;
            ofrendaEntregada = true;
            ofrendaCargada = false;
            for (int i = 0; i < floresEn.Count; i++)
                if (floresEn[i] != null) floresEn[i].gameObject.SetActive(false);
            for (int i = 0; i < velasEn.Count; i++)
            {
                var llama = velasEn[i] != null ? velasEn[i].Find("Llama") : null;
                if (llama != null) llama.gameObject.SetActive(true);
            }

            if (ofrenda != null) ofrenda.gameObject.SetActive(false);
            PintarPromesa();
        }

        void PonerFlor(Transform root, float u, float v, Color petal)
        {
            Vector2 sitio = SitioFlor(u, v);
            PonerFlorMundo(root, sitio.x, sitio.y, petal);
        }

        Vector2 SitioFlor(float u, float v)
        {
            for (int i = 0; i < 6; i++)
            {
                Vector2 mundo = PlazaWorld(u, v);
                if (OnPlaza(mundo.x, mundo.y) && !OnGardenPath(mundo.x, mundo.y)) return mundo;
                u += 3.4f;
            }

            return PlazaWorld(u, v);
        }

        void PonerFlorMundo(Transform root, float x, float z, Color petal)
        {
            float g = CajamarcaAltura.Sample(x, z);
            var flor = new GameObject("FlorPromesa");
            flor.transform.SetParent(root, false);
            flor.transform.localPosition = new Vector3(x, 0f, z);
            BoxPart(flor.transform, "Tallo", new Vector3(0f, g + 0.22f, 0f), new Vector3(0.08f, 0.4f, 0.08f), null, new Color(0.22f, 0.42f, 0.16f));
            BoxPart(flor.transform, "Petalo", new Vector3(0f, g + 0.46f, 0f), new Vector3(0.55f, 0.12f, 0.55f), null, petal);
            floresEn.Add(flor.transform);

            var ala = new GameObject("Mariposa");
            ala.transform.SetParent(root, false);
            BoxPart(ala.transform, "Ala", Vector3.zero, new Vector3(0.36f, 0.06f, 0.2f), null, new Color(0.96f, 0.86f, 0.42f));
            mariposas.Add(new MariposaPaso
            {
                Cuerpo = ala.transform,
                Casa = new Vector3(x, g + 0.95f, z),
            });
        }

        void PonerOfrenda(Transform root, float x, float z)
        {
            float g = CajamarcaAltura.Sample(x, z);
            ofrenda = BoxPart(root, "OfrendaPromesa", new Vector3(x, g + 0.2f, z), new Vector3(0.5f, 0.28f, 0.36f), null, new Color(0.55f, 0.24f, 0.16f)).transform;
        }

        void PonerVelas(Transform root)
        {
            Vector3 puerta = promesaPuerta;
            var hacia = new Vector3(fountainX - puerta.x, 0f, fountainZ - puerta.z);
            if (hacia.sqrMagnitude < 1f) hacia = new Vector3(0f, 0f, 1f);
            hacia.Normalize();
            var lado = new Vector3(hacia.z, 0f, -hacia.x);
            PonerVela(root, puerta + hacia * 3.5f + lado * 1.15f);
            PonerVela(root, puerta + hacia * 3.5f - lado * 1.15f);
        }

        void PonerVela(Transform root, Vector3 at)
        {
            float g = CajamarcaAltura.Sample(at.x, at.z);
            var vela = new GameObject("VelaPromesa");
            vela.transform.SetParent(root, false);
            vela.transform.localPosition = new Vector3(at.x, 0f, at.z);
            BoxPart(vela.transform, "Cera", new Vector3(0f, g + 0.24f, 0f), new Vector3(0.14f, 0.42f, 0.14f), null, new Color(0.93f, 0.9f, 0.78f));
            var llama = BoxPart(vela.transform, "Llama", new Vector3(0f, g + 0.52f, 0f), new Vector3(0.12f, 0.18f, 0.12f), null, new Color(0.98f, 0.72f, 0.22f));
            llama.SetActive(false);
            velasEn.Add(vela.transform);
        }

        void PasoPromesa(Walker hero)
        {
            if (promesaHecha || hero == null) return;
            string dicho = null;
            dicho = RecogerFlor(hero, dicho);
            dicho = EncenderVela(hero, dicho);
            dicho = TomarOfrenda(hero, dicho);
            dicho = SoltarOfrenda(hero, dicho);
            dicho = Tropezar(hero, dicho);

            float alcance = plan != null ? 16f : 9f;
            bool cerca = (hero.Local - promesaPuerta).sqrMagnitude < alcance * alcance;
            if (FloresListas && VelasListas && ofrendaEntregada)
            {
                promesaHecha = true;
                PartidaDelCamino.CumplirPromesa();
                promesaDejoReserva = ReservaDelCamino.PagarPromesa();
                PintarPromesa();
                Note(LeyDelMonte.PromesaHecha);
                battle?.Contar("SAN FRANCISCO", LeyDelMonte.PromesaHecha);
                estabaEnPuerta = cerca;
                return;
            }

            if (dicho != null) Note(dicho);
            else if (cerca && !estabaEnPuerta)
                Note(LeyDelMonte.PromesaPuerta + " " + FaltaPromesa());
            estabaEnPuerta = cerca;
            PintarPromesa();
        }

        bool FloresListas => flores >= 3 || floresEn.Count == 0;

        bool VelasListas => velas >= 2 || velasEn.Count == 0;

        string RecogerFlor(Walker hero, string dicho)
        {
            for (int i = 0; i < floresEn.Count; i++)
            {
                var flor = floresEn[i];
                if (flor == null || !flor.gameObject.activeSelf) continue;
                if (DistanciaPlana(hero.Local, flor.localPosition) > 1.45f) continue;
                flor.gameObject.SetActive(false);
                if (i < mariposas.Count && mariposas[i] != null)
                {
                    mariposas[i].SeFue = true;
                    mariposas[i].T = 0f;
                }
                flores++;
                AlforjaDelCamino.Guardar("flor", "Flor de la promesa", LeyDelMonte.AlforjaFlor);
                return flores >= 3 ? LeyDelMonte.PromesaFlores : LeyDelMonte.PromesaFlor;
            }

            return dicho;
        }

        string EncenderVela(Walker hero, string dicho)
        {
            if (dicho != null) return dicho;
            for (int i = 0; i < velasEn.Count; i++)
            {
                var vela = velasEn[i];
                if (vela == null) continue;
                var llama = vela.Find("Llama");
                if (llama != null && llama.gameObject.activeSelf) continue;
                if (DistanciaPlana(hero.Local, vela.localPosition) > 1.6f) continue;
                if (llama != null) llama.gameObject.SetActive(true);
                velas++;
                AlforjaDelCamino.Guardar("vela", "Vela", LeyDelMonte.AlforjaVela);
                if (!colaQuemada)
                {
                    colaQuemada = true;
                    if (hero.Grounded)
                    {
                        hero.Grounded = false;
                        hero.Vy = 5.4f;
                        hero.Push = Vector3.zero;
                        hero.Fell = false;
                        hero.Anim?.PlayJump();
                    }

                    return LeyDelMonte.PromesaVela;
                }

                return LeyDelMonte.PromesaVelas;
            }

            return dicho;
        }

        string TomarOfrenda(Walker hero, string dicho)
        {
            if (dicho != null || ofrenda == null || ofrendaCargada || ofrendaEntregada) return dicho;
            if (DistanciaPlana(hero.Local, ofrenda.localPosition) > 1.45f) return dicho;
            ofrendaCargada = true;
            cargaMetros = 0f;
            cargaDesde = hero.Local;
            AlforjaDelCamino.Guardar("ofrenda", "Ofrenda", LeyDelMonte.AlforjaOfrenda);
            return LeyDelMonte.PromesaCarga;
        }

        string SoltarOfrenda(Walker hero, string dicho)
        {
            if (dicho != null || !ofrendaCargada || ofrendaEntregada) return dicho;
            if (DistanciaPlana(hero.Local, promesaPuerta) > 8f) return dicho;
            ofrendaCargada = false;
            ofrendaEntregada = true;
            if (ofrenda != null) ofrenda.gameObject.SetActive(false);
            AlforjaDelCamino.Quitar("ofrenda");
            return LeyDelMonte.PromesaDeja;
        }

        string Tropezar(Walker hero, string dicho)
        {
            if (dicho != null || tropezo || !ofrendaCargada || !hero.Grounded) return dicho;
            Vector3 paso = hero.Local - cargaDesde;
            paso.y = 0f;
            cargaMetros += paso.magnitude;
            cargaDesde = hero.Local;
            if (cargaMetros < 8f) return dicho;
            tropezo = true;
            hero.Grounded = false;
            hero.Vy = 4.4f;
            hero.Push = Vector3.zero;
            hero.Fell = true;
            hero.Anim?.PlayFall();
            return LeyDelMonte.PromesaTropiezo;
        }

        string FaltaPromesa()
        {
            var partes = new List<string>();
            if (!FloresListas) partes.Add("las flores");
            if (!VelasListas) partes.Add("las velas");
            if (!ofrendaEntregada) partes.Add(ofrendaCargada ? "dejar la ofrenda" : "la ofrenda");
            if (partes.Count == 0) return "";
            if (partes.Count == 1) return "Falta " + partes[0] + ".";
            string ultima = partes[partes.Count - 1];
            partes.RemoveAt(partes.Count - 1);
            return "Faltan " + string.Join(", ", partes.ToArray()) + " y " + ultima + ".";
        }

        void PintarPromesa()
        {
            if (promesaTexto == null) return;
            if (promesaHecha)
            {
                promesaTexto.text = promesaDejoReserva
                    ? "Promesa cumplida. Vida, ayni y reputación +1"
                    : "Promesa cumplida";
                return;
            }

            string carga = ofrendaEntregada ? "en San Francisco" : ofrendaCargada ? "contigo" : "en la plaza";
            promesaTexto.text = "Flores " + flores + "/3    Velas " + velas + "/2    Ofrenda " + carga;
        }

        void LlevarOfrenda()
        {
            if (!ofrendaCargada || ofrenda == null) return;
            var hero = Hero();
            if (hero?.Body == null) return;
            Vector3 lado = cam != null ? cam.transform.right : Vector3.right;
            lado.y = 0f;
            if (lado.sqrMagnitude < 0.01f) lado = Vector3.right;
            ofrenda.position = hero.Body.position + Vector3.up * 1.05f + lado.normalized * 0.55f;
        }

        // Jirón Del Comercio, fuera de la plaza. Código municipal de la
        // NTP 900.058:2019: verde, marrón, negro y rojo.
        void VestirBasura()
        {
            if (RuntimeParent == null || RuntimeParent.Find("BasuraCalle") != null) return;
            basuraDonde = "Jirón Del Comercio";
            Vector3 a = new Vector3(-98f, 0f, -19.5f);
            Vector3 b = new Vector3(-147.7f, 0f, 21.1f);
            if (!HallarCalle(a, b, out Vector3 origen, out Vector3 eje))
            {
                basuraDonde = "Jirón Amazonas";
                a = new Vector3(38f, 0f, 133.4f);
                b = new Vector3(132.8f, 0f, 56.3f);
                if (!HallarCalle(a, b, out origen, out eje)) return;
            }

            Vector3 lado = new Vector3(-eje.z, 0f, eje.x);
            float borde = 0f;
            for (float d = 1.2f; d <= 2.8f; d += 0.8f)
            {
                if (!Pisable(origen + lado * d)) break;
                borde = d;
            }

            if (borde < 0.5f)
            {
                for (float d = 1.2f; d <= 2.8f; d += 0.8f)
                {
                    if (!Pisable(origen - lado * d)) break;
                    borde = -d;
                }
            }

            var root = new GameObject("BasuraCalle");
            root.transform.SetParent(RuntimeParent, false);
            string[] nombres = { "el papel", "la botella", "la cáscara", "la colilla", "la pila" };
            int[] clases = { 0, 0, 1, 2, 3 };
            for (int i = 0; i < nombres.Length; i++)
            {
                Vector3 at = origen + eje * (3.5f + i * 4.4f);
                if (!Pisable(at)) at += lado * Mathf.Sign(borde == 0f ? 1f : borde) * 1.2f;
                if (!Pisable(at)) continue;
                PonerPieza(root.transform, at, nombres[i], clases[i]);
            }

            if (basura.Count == 0)
            {
                Destroy(root);
                return;
            }

            basuraTotal = basura.Count;
            Vector3 fila = origen + eje * 28f + lado * borde;
            if (!Pisable(fila)) fila = origen + eje * 28f;
            string[] rotulos = { "VERDE\naprovechable", "MARRÓN\norgánico", "NEGRO\nno aprovechable", "ROJO\npeligroso" };
            var colores = new[]
            {
                new Color(0.15f, 0.48f, 0.22f),
                new Color(0.46f, 0.27f, 0.13f),
                new Color(0.08f, 0.08f, 0.08f),
                new Color(0.68f, 0.1f, 0.1f),
            };
            for (int i = 0; i < rotulos.Length; i++)
            {
                Vector3 at = fila + eje * (i * 1.85f);
                if (!Pisable(at)) at = origen + eje * (28f + i * 1.85f);
                PonerTacho(root.transform, at, i, rotulos[i], colores[i], eje);
            }
        }

        bool HallarCalle(Vector3 a, Vector3 b, out Vector3 origen, out Vector3 eje)
        {
            origen = a;
            eje = b - a;
            eje.y = 0f;
            if (eje.sqrMagnitude < 4f) return false;
            eje.Normalize();
            Vector3 lado = new Vector3(-eje.z, 0f, eje.x);
            for (float t = 12f; t <= 46f; t += 3f)
            {
                for (float s = -2.4f; s <= 2.4f; s += 1.2f)
                {
                    Vector3 p = a + eje * t + lado * s;
                    if (!Pisable(p)) continue;
                    origen = p;
                    return true;
                }
            }

            return false;
        }

        bool Pisable(Vector3 p)
        {
            return OnStreet(p.x, p.z) && !OnPlaza(p.x, p.z) && !CalleCerrada(p.x, p.z);
        }

        void PonerPieza(Transform root, Vector3 at, string nombre, int clase)
        {
            float g = CajamarcaAltura.Sample(at.x, at.z) + CapaPlaza(at.x, at.z);
            var go = new GameObject("Basura");
            go.transform.SetParent(root, false);
            go.transform.localPosition = new Vector3(at.x, 0f, at.z);
            if (clase == 0 && nombre.IndexOf("papel", System.StringComparison.Ordinal) >= 0)
                BoxPart(go.transform, "Hoja", new Vector3(0f, g + 0.03f, 0f), new Vector3(0.46f, 0.03f, 0.32f), null, new Color(0.93f, 0.9f, 0.8f));
            else if (clase == 0)
                BoxPart(go.transform, "Envase", new Vector3(0f, g + 0.2f, 0f), new Vector3(0.12f, 0.36f, 0.12f), null, new Color(0.55f, 0.84f, 0.9f));
            else if (clase == 1)
                BoxPart(go.transform, "Cascara", new Vector3(0f, g + 0.07f, 0f), new Vector3(0.28f, 0.1f, 0.22f), null, new Color(0.86f, 0.52f, 0.16f));
            else if (clase == 2)
            {
                BoxPart(go.transform, "Colilla", new Vector3(0f, g + 0.03f, 0f), new Vector3(0.16f, 0.035f, 0.05f), null, new Color(0.93f, 0.91f, 0.84f));
                BoxPart(go.transform, "Ceniza", new Vector3(0.1f, g + 0.03f, 0f), new Vector3(0.05f, 0.035f, 0.05f), null, new Color(0.9f, 0.42f, 0.14f));
            }
            else
            {
                BoxPart(go.transform, "Pila", new Vector3(0f, g + 0.08f, 0f), new Vector3(0.08f, 0.14f, 0.08f), null, new Color(0.12f, 0.12f, 0.13f));
                BoxPart(go.transform, "Polo", new Vector3(0f, g + 0.16f, 0f), new Vector3(0.09f, 0.035f, 0.09f), null, new Color(0.9f, 0.75f, 0.16f));
            }

            basura.Add(new PiezaBasura { Cuerpo = go.transform, Clase = clase, Nombre = nombre });
        }

        void PonerTacho(Transform root, Vector3 at, int clase, string rotulo, Color color, Vector3 eje)
        {
            float g = CajamarcaAltura.Sample(at.x, at.z) + CapaPlaza(at.x, at.z);
            var go = new GameObject("Tacho");
            go.transform.SetParent(root, false);
            go.transform.localPosition = new Vector3(at.x, 0f, at.z);
            Vector3 mira = new Vector3(-eje.z, 0f, eje.x);
            if (mira.sqrMagnitude < 0.01f) mira = Vector3.forward;
            go.transform.rotation = Quaternion.LookRotation(mira, Vector3.up);
            BoxPart(go.transform, "CuerpoTacho", new Vector3(0f, g + 0.55f, 0f), new Vector3(0.78f, 1.05f, 0.62f), null, color);
            BoxPart(go.transform, "BocaTacho", new Vector3(0f, g + 1.1f, 0f), new Vector3(0.68f, 0.08f, 0.52f), null, Color.Lerp(color, Color.black, 0.3f));
            BoxPart(go.transform, "PlacaTacho", new Vector3(0f, g + 0.62f, 0.36f), new Vector3(0.7f, 0.4f, 0.04f), null, new Color(0.95f, 0.91f, 0.8f));
            TextoCartel(go.transform, rotulo, new Vector3(0f, g + 0.62f, 0.4f), 0f, 0.32f);
            tachos.Add(go.transform);
            tachoClase.Add(clase);
        }

        void PasoBasura(Walker hero)
        {
            if (basuraHecha || hero == null || basura.Count == 0) return;
            if (basuraEnMano == null)
            {
                if (!basuraAviso && CercaDeBasura(hero.Local, 14f))
                {
                    basuraAviso = true;
                    Note(basuraDonde + ". " + LeyDelMonte.BasuraCalle);
                }

                for (int i = 0; i < basura.Count; i++)
                {
                    var pieza = basura[i];
                    if (pieza?.Cuerpo == null || !pieza.Cuerpo.gameObject.activeSelf) continue;
                    if (DistanciaPlana(hero.Local, pieza.Cuerpo.localPosition) > 1.45f) continue;
                    pieza.EnMano = true;
                    basuraEnMano = pieza;
                    Note("Recogiste " + pieza.Nombre + ". " + DondeBasura(pieza.Clase));
                    return;
                }

                return;
            }

            bool vioMal = false;
            for (int i = 0; i < tachos.Count; i++)
            {
                var tacho = tachos[i];
                if (tacho == null) continue;
                if (DistanciaPlana(hero.Local, tacho.localPosition) > 1.75f) continue;
                if (i >= tachoClase.Count || tachoClase[i] != basuraEnMano.Clase)
                {
                    vioMal = true;
                    continue;
                }

                basuraEnMano.EnMano = false;
                basuraEnMano.Cuerpo.gameObject.SetActive(false);
                basuraEnMano = null;
                basuraPuestas++;
                battle?.SumarAura();
                if (basuraPuestas >= basuraTotal)
                {
                    basuraHecha = true;
                    Note(LeyDelMonte.BasuraLista);
                }
                else
                    Note(LeyDelMonte.BasuraBien);
                return;
            }

            if (vioMal)
                Note(LeyDelMonte.BasuraMal + " " + DondeBasura(basuraEnMano.Clase));
        }

        bool CercaDeBasura(Vector3 local, float radio)
        {
            float r2 = radio * radio;
            for (int i = 0; i < basura.Count; i++)
            {
                var cuerpo = basura[i]?.Cuerpo;
                if (cuerpo == null) continue;
                if (DistanciaPlana(local, cuerpo.localPosition) <= radio) return true;
            }

            for (int i = 0; i < tachos.Count; i++)
            {
                if (tachos[i] == null) continue;
                Vector3 d = local - tachos[i].localPosition;
                d.y = 0f;
                if (d.sqrMagnitude <= r2) return true;
            }

            return false;
        }

        static string DondeBasura(int clase)
        {
            switch (clase)
            {
                case 1: return "Va al tacho marrón: orgánico.";
                case 2: return "Va al tacho negro: no aprovechable.";
                case 3: return "Va al tacho rojo: peligroso.";
                default: return "Va al tacho verde: aprovechable.";
            }
        }

        void LlevarBasura()
        {
            if (basuraEnMano?.Cuerpo == null || !basuraEnMano.Cuerpo.gameObject.activeSelf) return;
            var hero = Hero();
            if (hero?.Body == null) return;
            Vector3 lado = cam != null ? cam.transform.right : Vector3.right;
            lado.y = 0f;
            if (lado.sqrMagnitude < 0.01f) lado = Vector3.right;
            basuraEnMano.Cuerpo.position = hero.Body.position + Vector3.up * 1.05f + lado.normalized * 0.55f;
        }

        sealed class PiezaBasura
        {
            public Transform Cuerpo;
            public int Clase;
            public string Nombre;
            public bool EnMano;
        }

        void MoverMariposas()
        {
            float dt = Time.deltaTime;
            for (int i = 0; i < mariposas.Count; i++)
            {
                var ala = mariposas[i];
                if (ala?.Cuerpo == null) continue;
                ala.T += dt;
                if (ala.SeFue)
                {
                    ala.Cuerpo.localPosition += Vector3.up * dt * 1.6f;
                    if (ala.T > 1.8f) ala.Cuerpo.gameObject.SetActive(false);
                    continue;
                }

                float x = Mathf.Sin(ala.T * 2.1f) * 0.45f;
                float z = Mathf.Cos(ala.T * 1.5f) * 0.32f;
                float y = Mathf.Sin(ala.T * 6f) * 0.18f;
                ala.Cuerpo.localPosition = ala.Casa + new Vector3(x, y, z);
                if (ala.Cuerpo.childCount > 0)
                {
                    float aleteo = 0.04f + Mathf.Abs(Mathf.Sin(ala.T * 14f)) * 0.1f;
                    ala.Cuerpo.GetChild(0).localScale = new Vector3(0.36f, aleteo, 0.2f);
                }
            }
        }

        static float DistanciaPlana(Vector3 a, Vector3 b)
        {
            float dx = a.x - b.x;
            float dz = a.z - b.z;
            return Mathf.Sqrt(dx * dx + dz * dz);
        }

        sealed class MariposaPaso
        {
            public Transform Cuerpo;
            public Vector3 Casa;
            public bool SeFue;
            public float T;
        }

        // Jardines en los cuatro cuadrantes, fuera de la fuente y de los senderos.
        // La pista que rodea la plaza es la única calzada de doble vía.
        void VestirPlaza()
        {
            if (dynamicRoot == null || !plazaFound || plazaHalfU < 24f || plazaHalfV < 24f) return;
            if (dynamicRoot.Find("PlazaViva") != null) return;
            if (district != null && district.Find("PlazaViva") != null) return;
            MedirFrentes();
            // Cada cuadrante es jardin, desde el sendero hasta el paseo.
            // Antes el cesped empezaba en el anillo de la fuente y la cruz
            // quedaba un mar de la misma piedra que el paseo.
            float inner = RutaMedia;
            float u1 = plazaHalfU - PlazaWalk - 0.4f;
            float v1 = plazaHalfV - PlazaWalk - 0.4f;
            if (u1 - inner < 6f || v1 - inner < 6f) return;
            jardinInner = inner;
            jardinU1 = u1;
            jardinV1 = v1;
            altoJardin = PistaBaja + Peldaño + 0.06f;
            var root = new GameObject("PlazaViva");
            root.transform.SetParent(dynamicRoot, false);
            var shader = Shader.Find("AYNI/Calle") ?? Shader.Find("Standard") ?? Shader.Find("Sprites/Default");

            var grass = new Pen();
            var borde = new Pen();
            float bordeJardin = altoJardin + 0.02f;
            CespedEstrella(grass, u1, v1);
            Cuadrantes(inner, u1, v1, (a, b, c, d) => BordeCuadrante(borde, a, b, c, d, bordeJardin));
            BordeVereda(borde);
            var paseo = new Pen();
            PaseoDePlaza(paseo);
            Publish(grass, "Cesped Jardin", "Bosque/piso_cesped", shader, root.transform);
            Publish(borde, "Borde Plaza", "Calle/piedra", shader, root.transform);
            Publish(paseo, "Paseo Plaza", "Calle/piso_plaza", shader, root.transform);
            VestirPista(root.transform, shader);
            CanterosDeFlores(root.transform);
            CasasDeEsquina(root.transform);
            VestirManzanaDePlaza(root.transform);
            AclararArbolesDePlaza();
            PonerBancosPlaza(root.transform);
        }

        /// <summary>
        /// Cuatro frentes continuos, como la Plaza de Armas: casas de dos
        /// pisos alineadas, cada lado con una fachada real distinta.
        /// </summary>
        void VestirManzanaDePlaza(Transform root)
        {
            if (root == null || !plazaFound) return;
            float retiro = VeredaAncho + 0.4f;
            if (pistaLista)
                retiro += Mathf.Max(pistaOutN, pistaOutS, pistaOutE, pistaOutW);
            else
                retiro += PlazaWalk + 2.2f;

            const float boca = 8.4f;
            float uFace = plazaHalfU + retiro;
            float vFace = plazaHalfV + retiro;
            float uLen = plazaHalfU * 2f - boca * 2f;
            float vLen = plazaHalfV * 2f - boca * 2f;
            if (uLen < 12f || vLen < 12f) return;

            if (HayCasonasDePlaza())
            {
                casonasDePlazaPuestas = true;
                return;
            }

            int frenteFrancisco = FrenteSanFrancisco();
            int frenteCatedral = frenteFrancisco % 2 == 0 ? frenteFrancisco + 1 : frenteFrancisco - 1;
            SembrarFrentePlaza(0f, vFace, uLen, true, -1f, "Casona del Norte", MoldeDelFrente(0, frenteFrancisco, frenteCatedral));
            SembrarFrentePlaza(0f, -vFace, uLen, true, 1f, "Casona de la Cal", MoldeDelFrente(1, frenteFrancisco, frenteCatedral));
            SembrarFrentePlaza(uFace, 0f, vLen, false, -1f, "Casona de Puga", MoldeDelFrente(2, frenteFrancisco, frenteCatedral));
            SembrarFrentePlaza(-uFace, 0f, vLen, false, 1f, "Casona Turquesa", MoldeDelFrente(3, frenteFrancisco, frenteCatedral));
            SembrarEsquinaPlaza(1f, 1f, pistaOutE, pistaOutN, 12);
            SembrarEsquinaPlaza(-1f, 1f, pistaOutW, pistaOutN, 13);
            SembrarEsquinaPlaza(1f, -1f, pistaOutE, pistaOutS, 14);
            SembrarEsquinaPlaza(-1f, -1f, pistaOutW, pistaOutS, 15);
            SembrarArbolesDePlaza();
            SembrarBancosDePlaza();
            SembrarFloresDePlaza();
            casonasDePlazaPuestas = true;
        }

        bool HayCasonasDePlaza()
        {
            for (int i = 0; i < piezasGuardadas.Count; i++)
            {
                if (piezasGuardadas[i].Tipo != Pieza.Casona) continue;
                Vector2 loc = PlazaLocal(piezasGuardadas[i].X, piezasGuardadas[i].Z);
                if (Mathf.Abs(loc.x) <= plazaHalfU + 22f && Mathf.Abs(loc.y) <= plazaHalfV + 22f)
                    return true;
            }

            return false;
        }

        // 0 +V, 1 -V, 2 +U, 3 -U. Sin puerta, San Francisco cae en -V.
        int FrenteSanFrancisco()
        {
            if (!hasDoor) return 1;
            Vector2 loc = PlazaLocal(sanFranciscoDoor.x, sanFranciscoDoor.z);
            float cercaU = Mathf.Abs(loc.x) / plazaHalfU;
            float cercaV = Mathf.Abs(loc.y) / plazaHalfV;
            if (cercaU >= cercaV)
                return loc.x >= 0f ? 2 : 3;
            return loc.y >= 0f ? 0 : 1;
        }

        static int MoldeDelFrente(int frente, int francisco, int catedral)
        {
            if (frente == francisco) return MoldeSanFrancisco;
            if (frente == catedral) return MoldeCatedral;
            return -1;
        }

        static readonly int[] CicloCasasPlaza = { 4, 5, 6, 0, 1, 2 };

        void SembrarFrentePlaza(float u, float v, float largo, bool aLoLargoDeU, float mira, string casona, int iglesia)
        {
            float origen = (aLoLargoDeU ? u : v) - largo * 0.5f;
            float fin = origen + largo;
            Vector3 haciaPlaza = (aLoLargoDeU ? EjeV() : EjeU()) * mira;
            float yaw = Mathf.Atan2(haciaPlaza.x, haciaPlaza.z) * Mathf.Rad2Deg;
            float medio = (origen + fin) * 0.5f;
            bool hayIglesia = iglesia >= 0 && iglesia < MoldesCasona.Length;
            float hueco0 = 0f;
            float hueco1 = 0f;
            if (hayIglesia)
            {
                MoldeCasona molde = MoldesCasona[iglesia];
                bool sanFrancisco = iglesia == MoldeSanFrancisco;
                float centro = medio;
                if (sanFrancisco && hasDoor)
                {
                    Vector2 puerta = PlazaLocal(sanFranciscoDoor.x, sanFranciscoDoor.z);
                    centro = aLoLargoDeU ? puerta.x : puerta.y;
                }

                if (sanFrancisco)
                {
                    float mitad = molde.Ancho * 0.5f;
                    float minC = origen + 0.4f + mitad;
                    float maxC = fin - 0.4f - mitad;
                    if (minC <= maxC)
                        centro = Mathf.Clamp(centro, minC, maxC);
                }

                // Fachada del FBX a Fondo/2. Se corre el centro atrás para empatar el fondo 7.50 de las casas.
                float atras = (molde.Fondo - 7.50f) * 0.5f;
                float cu = aLoLargoDeU ? centro : u - mira * atras;
                float cv = aLoLargoDeU ? v - mira * atras : centro;
                Vector2 sitio = PlazaWorld(cu, cv);
                string nombre = sanFrancisco ? "San Francisco" : "Catedral";
                GuardarPieza(Pieza.Casona, sitio.x, sitio.y, yaw, 100 + iglesia, nombre);
                casonas.Add(sitio);
                hueco0 = centro - molde.Ancho * 0.5f - 0.35f;
                hueco1 = centro + molde.Ancho * 0.5f + 0.35f;
            }

            float cursor = origen + 0.3f;
            int i = 0;
            int principal = -1;
            float mejor = float.MaxValue;
            while (cursor < fin - 6.5f && i < 12)
            {
                int indice = CicloCasasPlaza[i % CicloCasasPlaza.Length];
                int variante = 100 + indice;
                var rec = RecetaDe(variante);
                if (hayIglesia && cursor < hueco1 && cursor + rec.Ancho > hueco0)
                {
                    cursor = hueco1;
                    i++;
                    continue;
                }

                if (cursor + rec.Ancho > fin) break;
                float mid = cursor + rec.Ancho * 0.5f;
                float uu = aLoLargoDeU ? mid : u;
                float vv = aLoLargoDeU ? v : mid;
                Vector2 wc = PlazaWorld(uu, vv);
                if (!TocaIglesia(wc.x, wc.y, 7f))
                {
                    int puesta = piezasGuardadas.Count;
                    GuardarPieza(Pieza.Casona, wc.x, wc.y, yaw, variante, null);
                    casonas.Add(wc);
                    float d = Mathf.Abs(mid - medio);
                    if (d < mejor)
                    {
                        mejor = d;
                        principal = puesta;
                    }
                }

                cursor += rec.Ancho + 0.20f;
                i++;
            }

            if (!hayIglesia && principal >= 0 && principal < piezasGuardadas.Count)
            {
                var dato = piezasGuardadas[principal];
                dato.Nombre = casona;
                piezasGuardadas[principal] = dato;
            }
        }

        void SembrarEsquinaPlaza(float su, float sv, float outU, float outV, int variante)
        {
            if (!pistaLista) return;
            float retiro = VeredaAncho + 0.06f;
            const float half = 4.60f;
            float u0 = su * (plazaHalfU + outU + retiro);
            float v0 = sv * (plazaHalfV + outV + retiro);
            float uc = u0 + su * half;
            float vc = v0 + sv * half;
            Vector2 centro = PlazaWorld(uc, vc);
            // +Z del molde mira una calle y +X la otra, hacia la plaza.
            Vector3 hacia = Mathf.Abs(su - sv) < 0.01f ? -sv * EjeV() : -su * EjeU();
            if (hacia.sqrMagnitude < 0.0001f) return;
            float yaw = Mathf.Atan2(hacia.x, hacia.z) * Mathf.Rad2Deg;
            if (TocaIglesia(centro.x, centro.y, 5f)) return;
            int molde = 3 + (Mathf.Abs(variante) % 4) * 4;
            GuardarPieza(Pieza.Casona, centro.x, centro.y, yaw, molde, null);
            casonas.Add(centro);
        }

        static readonly string[] CatalogoFachadas =
        {
            "Calle/Fachadas/amarilla",
            "Calle/Fachadas/cal",
            "Calle/Fachadas/puga",
            "Calle/Fachadas/turquesa",
            "Calle/Fachadas/colonial",
            "Calle/Fachadas/mayo",
            "Calle/Fachadas/portal",
            "Calle/Fachadas/blanca",
            "Calle/Fachadas/ocre",
            "Calle/Fachadas/rosa",
            "Calle/Fachadas/verde",
            "Calle/Fachadas/celeste",
            "Calle/Fachadas/salmon",
            "Calle/Fachadas/hueso",
            "Calle/Fachadas/porton",
            "Calle/Fachadas/canteria",
        };

        void AclararArbolesDePlaza()
        {
            Transform grove = district != null ? district.Find("Arboles") : null;
            if (grove != null)
            {
                for (int i = 0; i < grove.childCount; i++)
                {
                    Transform arbol = grove.GetChild(i);
                    Vector3 local = district.InverseTransformPoint(arbol.position);
                    if (OnPlaza(local.x, local.z)) arbol.gameObject.SetActive(false);
                }
            }

            if (!plazaFound) return;
        }

        void SembrarArbolesDePlaza()
        {
            if (jardinU1 < 4f || jardinV1 < 4f) return;
            float ru = jardinU1 * 0.58f;
            float rv = jardinV1 * 0.58f;
            for (int i = 0; i < 8; i++)
            {
                float ang = (i + 0.5f) * Mathf.PI * 0.25f;
                Vector2 p = PlazaWorld(Mathf.Cos(ang) * ru, Mathf.Sin(ang) * rv);
                GuardarPieza(Pieza.Arbol, p.x, p.y, 0f, i, null);
                float ang2 = (i + 0.32f) * Mathf.PI * 0.25f;
                Vector2 q = PlazaWorld(Mathf.Cos(ang2) * ru * 0.78f, Mathf.Sin(ang2) * rv * 0.78f);
                GuardarPieza(Pieza.Arbol, q.x, q.y, 0f, i + 8, null);
            }
        }

        void SembrarBancosDePlaza()
        {
            float u = plazaHalfU - PlazaWalk * 0.55f;
            float v = plazaHalfV - PlazaWalk * 0.55f;
            float[] t = { -0.62f, -0.22f, 0.22f, 0.62f };
            for (int i = 0; i < t.Length; i++)
            {
                Vector2 s = PlazaWorld(u * t[i], -v);
                GuardarPieza(Pieza.Banco, s.x, s.y, 0f, i, null);
                Vector2 n = PlazaWorld(u * t[i], v);
                GuardarPieza(Pieza.Banco, n.x, n.y, 0f, i + 4, null);
            }
        }

        void SembrarFloresDePlaza()
        {
            if (jardinU1 < 6f || jardinV1 < 6f) return;
            float ru = jardinU1 * 0.48f;
            float rv = jardinV1 * 0.48f;
            for (int i = 0; i < 4; i++)
            {
                float ang = (i * 2 + 0.5f) * Mathf.PI * 0.25f;
                Vector2 p = PlazaWorld(Mathf.Cos(ang) * ru, Mathf.Sin(ang) * rv);
                GuardarPieza(Pieza.Flor, p.x, p.y, 0f, i, null);
            }
        }

        void PonerBancosPlaza(Transform root)
        {
        }

        /// <summary>
        /// Paseo de losa que cierra la plaza: anillo entre jardines y calles,
        /// esquinas y todo hueco que no sea césped ni pileta.
        /// </summary>
        void PaseoDePlaza(Pen pen)
        {
            const float cell = 3.4f;
            const float solape = 0.18f;
            float lift = PistaBaja + Peldaño;
            float u0 = -plazaHalfU - 0.2f;
            float u1 = plazaHalfU + 0.2f;
            float v0 = -plazaHalfV - 0.2f;
            float v1 = plazaHalfV + 0.2f;
            for (float u = u0; u < u1 - 0.01f; u += cell - solape)
            {
                float ua = Mathf.Min(u + cell, u1);
                for (float v = v0; v < v1 - 0.01f; v += cell - solape)
                {
                    float va = Mathf.Min(v + cell, v1);
                    if (ua - u < 0.12f || va - v < 0.12f) continue;
                    float cu = (u + ua) * 0.5f;
                    float cv = (v + va) * 0.5f;
                    if (EnCesped(cu, cv)) continue;
                    if (cu * cu + cv * cv < BocaFuente * BocaFuente) continue;
                    LosaPlaza(pen, u, v, ua, va, lift, Losa);
                }
            }

            if (!pistaLista) return;
            float extraU = plazaHalfU + Mathf.Max(pistaOutE, pistaOutW) + VeredaAncho + 0.4f;
            float extraV = plazaHalfV + Mathf.Max(pistaOutN, pistaOutS) + VeredaAncho + 0.4f;
            for (float u = -extraU; u < extraU - 0.01f; u += cell - solape)
            {
                float ua = Mathf.Min(u + cell, extraU);
                for (float v = -extraV; v < extraV - 0.01f; v += cell - solape)
                {
                    float va = Mathf.Min(v + cell, extraV);
                    if (Mathf.Abs(u) <= plazaHalfU + 0.15f && Mathf.Abs(v) <= plazaHalfV + 0.15f) continue;
                    float cu = (u + ua) * 0.5f;
                    float cv = (v + va) * 0.5f;
                    Vector2 p = PlazaWorld(cu, cv);
                    if (EsPista(p.x, p.y)) continue;
                    if (OnStreet(p.x, p.y) && !OnPlaza(p.x, p.y)) continue;
                    LosaPlaza(pen, u, v, ua, va, PistaAlta + Peldaño, Losa);
                }
            }
        }

        void CanterosDeFlores(Transform parent)
        {
            if (parent == null || jardinU1 < 6f || jardinV1 < 6f) return;
            float ru = jardinU1 * 0.48f;
            float rv = jardinV1 * 0.48f;
            var colores = new[]
            {
                Flower,
                new Color(0.98f, 0.88f, 0.22f),
                new Color(0.92f, 0.28f, 0.42f),
                new Color(0.98f, 0.96f, 0.92f),
            };
            for (int i = 0; i < 4; i++)
            {
                float ang = (i * 2 + 0.5f) * Mathf.PI * 0.25f;
                Cantero(parent, Mathf.Cos(ang) * ru, Mathf.Sin(ang) * rv, colores[i]);
            }
        }

        void Cantero(Transform parent, float u, float v, Color flor)
        {
            var shader = Shader.Find("AYNI/Calle") ?? Shader.Find("Standard") ?? Shader.Find("Sprites/Default");
            Vector2 w = PlazaWorld(u, v);
            float y = CajamarcaAltura.Sample(w.x, w.y) + altoJardin + 0.05f;
            var tierra = new Pen();
            var flores = new Pen();
            var centro = new Vector3(w.x, y, w.y);
            Disc(tierra, centro, 3.4f, new Color(0.42f, 0.30f, 0.16f));
            Disc(flores, centro + Vector3.up * 0.04f, 2.7f, flor);
            Publish(tierra, "CanteroTierra", "Calle/piso_tierra", shader, parent);
            Publish(flores, "CanteroFlor", null, shader, parent);
        }

        void Cuadrantes(float inner, float u1, float v1, System.Action<float, float, float, float> cada)
        {
            cada(inner, u1, inner, v1);
            cada(-u1, -inner, inner, v1);
            cada(inner, u1, -v1, -inner);
            cada(-u1, -inner, -v1, -inner);
        }

        void CespedEstrella(Pen pen, float u1, float v1)
        {
            const float cell = 2.6f;
            var cesped = new Color(0.62f, 0.74f, 0.32f);
            for (float u = -u1; u < u1 - 0.04f; u += cell)
            {
                float u2 = Mathf.Min(u + cell, u1);
                for (float v = -v1; v < v1 - 0.04f; v += cell)
                {
                    float v2 = Mathf.Min(v + cell, v1);
                    float cu = (u + u2) * 0.5f;
                    float cv = (v + v2) * 0.5f;
                    if (!EnCesped(cu, cv)) continue;
                    LosaPlaza(pen, u, v, u2, v2, altoJardin, cesped);
                }
            }
        }

        void CespedEn(Pen pen, float u0, float u1, float v0, float v1)
        {
            const float cell = 4f;
            const float margen = 0.06f;
            float ua = Mathf.Min(u0, u1) + margen;
            float ub = Mathf.Max(u0, u1) - margen;
            float va = Mathf.Min(v0, v1) + margen;
            float vb = Mathf.Max(v0, v1) - margen;
            if (ub - ua < 2f || vb - va < 2f) return;
            var cesped = new Color(0.62f, 0.74f, 0.32f);
            for (float u = ua; u < ub - 0.04f; u += cell)
            {
                float u2 = Mathf.Min(u + cell, ub);
                for (float v = va; v < vb - 0.04f; v += cell)
                {
                    float v2 = Mathf.Min(v + cell, vb);
                    float cu = (u + u2) * 0.5f;
                    float cv = (v + v2) * 0.5f;
                    if (!EnCesped(cu, cv)) continue;
                    LosaPlaza(pen, u, v, u2, v2, altoJardin, cesped);
                }
            }
        }

        void BordeCuadrante(Pen pen, float u0, float u1, float v0, float v1, float lift)
        {
            FranjaLocal(pen, u0, v0, u1, v0, 0.22f, lift, Color.white);
            FranjaLocal(pen, u0, v1, u1, v1, 0.22f, lift, Color.white);
            FranjaLocal(pen, u0, v0, u0, v1, 0.22f, lift, Color.white);
            FranjaLocal(pen, u1, v0, u1, v1, 0.22f, lift, Color.white);
        }

        // El borde de piedra va sobre la plana y se abre en la boca de cada calle.
        void BordeVereda(Pen pen)
        {
            float u = plazaHalfU - 0.28f;
            float v = plazaHalfV - 0.28f;
            float lift = PistaBaja + Peldaño + 0.04f;
            BordeLado(pen, -u, u, -v, true, bocaCS, bocaHS, lift);
            BordeLado(pen, -u, u, v, true, bocaCN, bocaHN, lift);
            BordeLado(pen, -v, v, -u, false, bocaCW, bocaHW, lift);
            BordeLado(pen, -v, v, u, false, bocaCE, bocaHE, lift);
        }

        void BordeLado(Pen pen, float a0, float a1, float fijo, bool aLoLargoDeU, float bocaC, float bocaH, float lift)
        {
            float a = Mathf.Min(a0, a1);
            float b = Mathf.Max(a0, a1);
            float g0 = bocaC - bocaH;
            float g1 = bocaC + bocaH;
            if (aLoLargoDeU)
            {
                if (g0 > a + 0.8f)
                    FranjaLocal(pen, a, fijo, Mathf.Min(g0, b), fijo, 0.34f, lift, Color.white);
                if (g1 < b - 0.8f)
                    FranjaLocal(pen, Mathf.Max(g1, a), fijo, b, fijo, 0.34f, lift, Color.white);
            }
            else
            {
                if (g0 > a + 0.8f)
                    FranjaLocal(pen, fijo, a, fijo, Mathf.Min(g0, b), 0.34f, lift, Color.white);
                if (g1 < b - 0.8f)
                    FranjaLocal(pen, fijo, Mathf.Max(g1, a), fijo, b, 0.34f, lift, Color.white);
            }
        }

        void LosaPlaza(Pen pen, float u0, float v0, float u1, float v1, float lift, Color color)
        {
            if (u1 - u0 < 0.2f || v1 - v0 < 0.2f) return;
            Vector2 a = PlazaWorld(u0, v0);
            Vector2 b = PlazaWorld(u0, v1);
            Vector2 c = PlazaWorld(u1, v1);
            Vector2 d = PlazaWorld(u1, v0);
            pen.Quad(
                new Vector3(a.x, CajamarcaAltura.Sample(a.x, a.y) + lift, a.y),
                new Vector3(b.x, CajamarcaAltura.Sample(b.x, b.y) + lift, b.y),
                new Vector3(c.x, CajamarcaAltura.Sample(c.x, c.y) + lift, c.y),
                new Vector3(d.x, CajamarcaAltura.Sample(d.x, d.y) + lift, d.y),
                Vector3.up, color, false);
        }

        void FranjaLocal(Pen pen, float u0, float v0, float u1, float v1, float half, float lift, Color color, bool sobrePista = false)
        {
            float du = u1 - u0;
            float dv = v1 - v0;
            float len = Mathf.Sqrt(du * du + dv * dv);
            if (len < 0.4f) return;
            int n = Mathf.Max(1, Mathf.CeilToInt(len / 6f));
            float px = -dv / len;
            float pz = du / len;
            for (int i = 0; i < n; i++)
            {
                float t0 = i / (float)n;
                float t1 = (i + 1) / (float)n;
                float uu0 = Mathf.Lerp(u0, u1, t0);
                float vv0 = Mathf.Lerp(v0, v1, t0);
                float uu1 = Mathf.Lerp(u0, u1, t1);
                float vv1 = Mathf.Lerp(v0, v1, t1);
                float mu = (uu0 + uu1) * 0.5f;
                float mv = (vv0 + vv1) * 0.5f;
                if (mu * mu + mv * mv < CorteFuente * CorteFuente) continue;
                if (sobrePista)
                {
                    Vector2 medio = PlazaWorld(mu, mv);
                    if (!EsPista(medio.x, medio.y)) continue;
                }

                Vector2 a = PlazaWorld(uu0, vv0);
                Vector2 b = PlazaWorld(uu1, vv1);
                Vector2 right = new Vector2(plazaCos * px - plazaSin * pz, plazaSin * px + plazaCos * pz) * half;
                var a0 = a - right;
                var a1 = a + right;
                var b1 = b + right;
                var b0 = b - right;
                float ya0 = lift;
                float ya1 = lift;
                float yb1 = lift;
                float yb0 = lift;
                if (sobrePista)
                {
                    ya0 = AlturaCalzada(uu0 - px * half, vv0 - pz * half) + lift;
                    ya1 = AlturaCalzada(uu0 + px * half, vv0 + pz * half) + lift;
                    yb1 = AlturaCalzada(uu1 + px * half, vv1 + pz * half) + lift;
                    yb0 = AlturaCalzada(uu1 - px * half, vv1 - pz * half) + lift;
                }

                pen.Quad(
                    new Vector3(a0.x, CajamarcaAltura.Sample(a0.x, a0.y) + ya0, a0.y),
                    new Vector3(a1.x, CajamarcaAltura.Sample(a1.x, a1.y) + ya1, a1.y),
                    new Vector3(b1.x, CajamarcaAltura.Sample(b1.x, b1.y) + yb1, b1.y),
                    new Vector3(b0.x, CajamarcaAltura.Sample(b0.x, b0.y) + yb0, b0.y),
                    Vector3.up, color, false);
            }
        }

        void Cerco(Transform parent, float u0, float v0, float u1, float v1, float cu, float cv, float alzar = 0f)
        {
            float du = u1 - u0;
            float dv = v1 - v0;
            float len = Mathf.Sqrt(du * du + dv * dv);
            if (len < 5f) return;
            float px = -dv / len;
            float pz = du / len;
            float mx = (u0 + u1) * 0.5f;
            float mz = (v0 + v1) * 0.5f;
            if (px * (cu - mx) + pz * (cv - mz) < 0f)
            {
                px = -px;
                pz = -pz;
            }

            bool orilla = mx * mx + mz * mz > cu * cu + cv * cv + 1f;
            float uA = u0 + px * 0.7f;
            float vA = v0 + pz * 0.7f;
            float uB = u1 + px * 0.7f;
            float vB = v1 + pz * 0.7f;
            const float gap = 5.8f;
            float use = len - gap * 2f;
            if (use < 2f) return;
            int n = Mathf.Max(1, Mathf.CeilToInt(use / 3.4f));
            var piedra = new Color(0.76f, 0.72f, 0.64f);
            var madera = new Color(0.45f, 0.28f, 0.14f);
            for (int i = 0; i < n; i++)
            {
                float t0 = (gap + use * i / n) / len;
                float t1 = (gap + use * (i + 1) / n) / len;
                float mu = Mathf.Lerp(uA, uB, (t0 + t1) * 0.5f);
                float mv = Mathf.Lerp(vA, vB, (t0 + t1) * 0.5f);
                if (mu * mu + mv * mv < CorteFuente * CorteFuente) continue;
                Vector2 at = PlazaWorld(mu, mv);
                Vector2 dir = PlazaWorld(Mathf.Lerp(uA, uB, t1), Mathf.Lerp(vA, vB, t1)) - PlazaWorld(Mathf.Lerp(uA, uB, t0), Mathf.Lerp(vA, vB, t0));
                var tangent = new Vector3(dir.x, 0f, dir.y);
                if (tangent.sqrMagnitude < 0.01f) continue;
                float g = CajamarcaAltura.Sample(at.x, at.y) + alzar;
                var giro = Quaternion.LookRotation(tangent.normalized, Vector3.up);
                float largo = tangent.magnitude;
                var zocalo = BoxPart(parent, "AdornoBaranda", new Vector3(at.x, g + 0.16f, at.y), new Vector3(0.42f, 0.32f, largo), "Calle/piedra", piedra);
                zocalo.transform.localRotation = giro;
                Kill(zocalo.GetComponent<Collider>());
                var poste = BoxPart(parent, "AdornoBaranda", new Vector3(at.x, g + 0.46f, at.y), new Vector3(0.36f, 0.92f, 0.36f), "Calle/piedra", piedra);
                poste.transform.localRotation = giro;
                Kill(poste.GetComponent<Collider>());
                var pasamano = BoxPart(parent, "AdornoBaranda", new Vector3(at.x, g + 0.64f, at.y), new Vector3(0.28f, 0.62f, largo), "Calle/piedra", piedra);
                pasamano.transform.localRotation = giro;
                Kill(pasamano.GetComponent<Collider>());
            }

            if (!orilla) return;
            int seats = Mathf.Max(1, Mathf.FloorToInt(use / 8f));
            var hacia = new Vector3(plazaCos * px - plazaSin * pz, 0f, plazaSin * px + plazaCos * pz);
            if (hacia.sqrMagnitude < 0.01f) hacia = Vector3.forward;
            hacia.Normalize();
            for (int i = 0; i < seats; i++)
            {
                float t = (gap + use * (i + 0.5f) / seats) / len;
                float su = Mathf.Lerp(u0, u1, t) - px * 0.9f;
                float sv = Mathf.Lerp(v0, v1, t) - pz * 0.9f;
                Vector2 at = PlazaWorld(su, sv);
                float g = CajamarcaAltura.Sample(at.x, at.y) + alzar;
                var rot = Quaternion.LookRotation(hacia, Vector3.up);
                var seat = BoxPart(parent, "SillaPlaza", new Vector3(at.x, g + 0.42f, at.y), new Vector3(1.25f, 0.1f, 0.46f), null, madera);
                seat.transform.localRotation = rot;
                var back = BoxPart(parent, "SillaPlaza", new Vector3(at.x, g + 0.78f, at.y) - hacia * 0.18f, new Vector3(1.25f, 0.72f, 0.08f), null, madera);
                back.transform.localRotation = rot;
            }
        }

        void MedirFrentes()
        {
            calleMedida = false;
            for (int i = 0; i < walkKind.Count; i++)
            {
                if (walkKind[i] != KindCalle) continue;
                calleMedida = true;
                break;
            }

            MedirFrente(1, out pistaInN, out pistaOutN, out bocaCN, out bocaHN, out ladoN);
            MedirFrente(-1, out pistaInS, out pistaOutS, out bocaCS, out bocaHS, out ladoS);
            MedirFrente(2, out pistaInE, out pistaOutE, out bocaCE, out bocaHE, out ladoE);
            MedirFrente(-2, out pistaInW, out pistaOutW, out bocaCW, out bocaHW, out ladoW);
        }

        // Varios rayos a lo largo del frente. El ancho es el de la calzada
        // que bordea la plaza, no la calle que sigue de largo por la boca.
        void MedirFrente(int side, out float inner, out float outer, out float bocaC, out float bocaH, out bool ok)
        {
            inner = 2.2f;
            outer = 8.5f;
            bocaC = 0f;
            bocaH = 6f;
            ok = false;
            float halfAlong = (side == 1 || side == -1) ? plazaHalfU : plazaHalfV;
            var los = new List<float>();
            var his = new List<float>();
            var lats = new List<float>();
            for (float s = -halfAlong + 4f; s <= halfAlong - 4f; s += 3.2f)
            {
                if (!RayoCalle(side, s, out float lo, out float hi)) continue;
                if (lo > 14f) continue;
                los.Add(lo);
                his.Add(hi);
                lats.Add(s);
            }

            if (los.Count < 2)
            {
                if (!BandaPlaza(side, out float lo, out float hi)) return;
                inner = Mathf.Clamp(lo, 0.4f, 12f);
                outer = Mathf.Clamp(hi, inner + 3.8f, inner + 12f);
                ok = true;
                return;
            }

            inner = Mathf.Clamp(Percentil(los, 0.25f), 0.35f, 12f);
            var cortos = new List<float>();
            for (int i = 0; i < his.Count; i++)
            {
                if (his[i] < 18f) cortos.Add(his[i]);
            }

            float hiUse;
            if (cortos.Count >= 3)
                hiUse = Percentil(cortos, 0.5f);
            else
            {
                var copia = new List<float>(his);
                hiUse = Percentil(copia, 0.3f);
            }
            outer = Mathf.Clamp(hiUse, inner + 3.8f, inner + 12f);
            ok = outer > inner + 2f;

            var boca = new List<float>();
            for (int i = 0; i < his.Count; i++)
            {
                if (his[i] > outer + 2.4f) boca.Add(lats[i]);
            }

            if (boca.Count >= 2)
            {
                boca.Sort();
                float span = boca[boca.Count - 1] - boca[0];
                bocaC = (boca[0] + boca[boca.Count - 1]) * 0.5f;
                bocaH = span > 18f ? 6.5f : Mathf.Clamp(span * 0.5f + 0.8f, 3.2f, 8.5f);
            }
            else
            {
                bocaC = Percentil(lats, 0.5f);
                bocaH = Mathf.Clamp((outer - inner) * 0.55f, 3.4f, 7.5f);
            }
        }

        bool RayoCalle(int side, float lateral, out float lo, out float hi)
        {
            lo = 0f;
            hi = 0f;
            bool found = false;
            for (float d = 0.45f; d <= 22f; d += 0.7f)
            {
                float u = 0f;
                float v = 0f;
                if (side == 1) { u = lateral; v = plazaHalfV + d; }
                else if (side == -1) { u = lateral; v = -(plazaHalfV + d); }
                else if (side == 2) { v = lateral; u = plazaHalfU + d; }
                else { v = lateral; u = -(plazaHalfU + d); }
                Vector2 p = PlazaWorld(u, v);
                if (EsPista(p.x, p.y))
                {
                    if (!found) lo = d;
                    hi = d;
                    found = true;
                }
                else if (found)
                    break;
            }

            return found && hi - lo > 1.5f;
        }

        static float Percentil(List<float> vals, float p)
        {
            if (vals == null || vals.Count == 0) return 0f;
            vals.Sort();
            float t = Mathf.Clamp01(p) * (vals.Count - 1);
            int i = Mathf.FloorToInt(t);
            int j = Mathf.Min(i + 1, vals.Count - 1);
            return Mathf.Lerp(vals[i], vals[j], t - i);
        }

        void VestirPista(Transform parent, Shader shader)
        {
            casonas.Clear();
            pistaLista = ladoN || ladoS || ladoE || ladoW;
            if (!pistaLista) return;
            float midN = (pistaInN + pistaOutN) * 0.5f;
            float halfN = Mathf.Max(1.2f, (pistaOutN - pistaInN) * 0.5f);
            float midS = (pistaInS + pistaOutS) * 0.5f;
            float halfS = Mathf.Max(1.2f, (pistaOutS - pistaInS) * 0.5f);
            float midE = (pistaInE + pistaOutE) * 0.5f;
            float halfE = Mathf.Max(1.2f, (pistaOutE - pistaInE) * 0.5f);
            float midW = (pistaInW + pistaOutW) * 0.5f;
            float halfW = Mathf.Max(1.2f, (pistaOutW - pistaInW) * 0.5f);

            var asfalto = new Pen();
            var lineas = new Pen();
            var cebra = new Pen();
            var amarillo = new Color(0.95f, 0.82f, 0.18f);
            // La recta llega hasta el bordillo de adentro de la calle que cruza.
            // La esquina es otra losa. Las dos usan la misma altura en la junta.
            // Sólo se pinta donde el plano tiene calzada.
            float uInW = plazaHalfU + pistaInW;
            float uInE = plazaHalfU + pistaInE;
            float vInS = plazaHalfV + pistaInS;
            float vInN = plazaHalfV + pistaInN;
            float uOutW = plazaHalfU + pistaOutW;
            float uOutE = plazaHalfU + pistaOutE;
            float vOutS = plazaHalfV + pistaOutS;
            float vOutN = plazaHalfV + pistaOutN;
            if (ladoN)
                LosaAsfalto(asfalto, -uInW, vInN, uInE, vOutN);
            if (ladoS)
                LosaAsfalto(asfalto, -uInW, -vOutS, uInE, -vInS);
            if (ladoE)
                LosaAsfalto(asfalto, uInE, -vInS, uOutE, vInN);
            if (ladoW)
                LosaAsfalto(asfalto, -uOutW, -vInS, -uInW, vInN);
            if (ladoN && ladoE)
                LosaAsfalto(asfalto, uInE, vInN, uOutE, vOutN);
            if (ladoN && ladoW)
                LosaAsfalto(asfalto, -uOutW, vInN, -uInW, vOutN);
            if (ladoS && ladoE)
                LosaAsfalto(asfalto, uInE, -vOutS, uOutE, -vInS);
            if (ladoS && ladoW)
                LosaAsfalto(asfalto, -uOutW, -vOutS, -uInW, -vInS);

            // La doble línea se corta antes del cruce y se abre en la boca.
            float finE = plazaHalfU + Mathf.Max(0.8f, pistaInE - 1.8f);
            float finW = plazaHalfU + Mathf.Max(0.8f, pistaInW - 1.8f);
            float finN = plazaHalfV + Mathf.Max(0.8f, pistaInN - 1.8f);
            float finS = plazaHalfV + Mathf.Max(0.8f, pistaInS - 1.8f);
            if (ladoN)
                DobleLinea(lineas, -finW, finE, plazaHalfV + midN, true, amarillo, bocaCN, bocaHN);
            if (ladoS)
                DobleLinea(lineas, -finW, finE, -(plazaHalfV + midS), true, amarillo, bocaCS, bocaHS);
            if (ladoE)
                DobleLinea(lineas, -finS, finN, plazaHalfU + midE, false, amarillo, bocaCE, bocaHE);
            if (ladoW)
                DobleLinea(lineas, -finS, finN, -(plazaHalfU + midW), false, amarillo, bocaCW, bocaHW);

            // El borde blanco sigue el cordón, un poco adentro, y tampoco entra a la cebra.
            if (ladoN)
            {
                float m = MargenPista(pistaInN, pistaOutN);
                BordeCalzada(lineas, -finW, finE, plazaHalfV + pistaInN + m, true, bocaCN, bocaHN);
                BordeCalzada(lineas, -finW, finE, plazaHalfV + pistaOutN - m, true, bocaCN, bocaHN);
            }

            if (ladoS)
            {
                float m = MargenPista(pistaInS, pistaOutS);
                BordeCalzada(lineas, -finW, finE, -(plazaHalfV + pistaInS) - m, true, bocaCS, bocaHS);
                BordeCalzada(lineas, -finW, finE, -(plazaHalfV + pistaOutS) + m, true, bocaCS, bocaHS);
            }

            if (ladoE)
            {
                float m = MargenPista(pistaInE, pistaOutE);
                BordeCalzada(lineas, -finS, finN, plazaHalfU + pistaInE + m, false, bocaCE, bocaHE);
                BordeCalzada(lineas, -finS, finN, plazaHalfU + pistaOutE - m, false, bocaCE, bocaHE);
            }

            if (ladoW)
            {
                float m = MargenPista(pistaInW, pistaOutW);
                BordeCalzada(lineas, -finS, finN, -(plazaHalfU + pistaInW) - m, false, bocaCW, bocaHW);
                BordeCalzada(lineas, -finS, finN, -(plazaHalfU + pistaOutW) + m, false, bocaCW, bocaHW);
            }

            if (ladoN)
            {
                CebraSi(cebra, bocaCN, plazaHalfV + midN, halfN, true);
                TopeCebra(cebra, bocaCN, plazaHalfV + midN, halfN, true);
                CebraSi(cebra, -finW - 0.6f, plazaHalfV + midN, halfN, true);
                CebraSi(cebra, finE + 0.6f, plazaHalfV + midN, halfN, true);
            }

            if (ladoS)
            {
                CebraSi(cebra, bocaCS, -(plazaHalfV + midS), halfS, true);
                TopeCebra(cebra, bocaCS, -(plazaHalfV + midS), halfS, true);
                CebraSi(cebra, -finW - 0.6f, -(plazaHalfV + midS), halfS, true);
                CebraSi(cebra, finE + 0.6f, -(plazaHalfV + midS), halfS, true);
            }

            if (ladoE)
            {
                CebraSi(cebra, plazaHalfU + midE, bocaCE, halfE, false);
                TopeCebra(cebra, plazaHalfU + midE, bocaCE, halfE, false);
                CebraSi(cebra, plazaHalfU + midE, -finS - 0.6f, halfE, false);
                CebraSi(cebra, plazaHalfU + midE, finN + 0.6f, halfE, false);
            }

            if (ladoW)
            {
                CebraSi(cebra, -(plazaHalfU + midW), bocaCW, halfW, false);
                TopeCebra(cebra, -(plazaHalfU + midW), bocaCW, halfW, false);
                CebraSi(cebra, -(plazaHalfU + midW), -finS - 0.6f, halfW, false);
                CebraSi(cebra, -(plazaHalfU + midW), finN + 0.6f, halfW, false);
            }

            var grada = new Pen();
            var vereda = new Pen();
            var rutas = new Pen();
            VestirPlana(grada, vereda);
            VestirRutas(rutas);

            PublishTex(asfalto, "Pista Plaza", Asfalto(), shader, parent);
            Publish(lineas, "Lineas Pista", null, shader, parent);
            Publish(cebra, "Cebra Pista", null, shader, parent);
            Publish(grada, "Piso Grada", "Calle/piedra", shader, parent);
            Publish(vereda, "Piso Vereda", "Calle/piedra", shader, parent);
            Publish(rutas, "Rutas Plaza", "Calle/piso_plaza", shader, parent);
        }

        void Calzada(Pen pen, float desde, float hasta, float fijo, float half, bool horizontal)
        {
            float largo = hasta - desde;
            if (largo < 1f) return;
            float e0 = fijo - half;
            float e1 = fijo + half;
            bool e0Adentro = Mathf.Abs(e0) <= Mathf.Abs(e1);
            float h0 = e0Adentro ? PistaBaja : PistaAlta;
            float h1 = e0Adentro ? PistaAlta : PistaBaja;
            int n = Mathf.Max(1, Mathf.CeilToInt(largo / 6f));
            for (int i = 0; i < n; i++)
            {
                float t0 = Mathf.Lerp(desde, hasta, i / (float)n);
                float t1 = Mathf.Lerp(desde, hasta, (i + 1) / (float)n);
                if (horizontal)
                    LosaAlzada(pen, t0, e0, t1, e1, h0, h1, h1, h0, Color.white);
                else
                    LosaAlzada(pen, e0, t0, e1, t1, h0, h0, h1, h1, Color.white);
            }
        }

        // La plana es el piso de piedra dentro del bordillo. La vereda va
        // pegada a la calzada, del lado de las casas, y se abre en cada boca.
        void VestirPlana(Pen grada, Pen vereda)
        {
            var losa = Losa;
            var contra = new Color(0.62f, 0.58f, 0.52f);
            float altoPlaza = PistaBaja + Peldaño;
            float bajoPista = PistaBaja + PistaPiel;
            float bajoVereda = PistaAlta + PistaPiel;
            float uInW = ladoW ? plazaHalfU + pistaInW : plazaHalfU;
            float uInE = ladoE ? plazaHalfU + pistaInE : plazaHalfU;
            float vInS = ladoS ? plazaHalfV + pistaInS : plazaHalfV;
            float vInN = ladoN ? plazaHalfV + pistaInN : plazaHalfV;
            float uOutW = ladoW ? plazaHalfU + pistaOutW : plazaHalfU;
            float uOutE = ladoE ? plazaHalfU + pistaOutE : plazaHalfU;
            float vOutS = ladoS ? plazaHalfV + pistaOutS : plazaHalfV;
            float vOutN = ladoN ? plazaHalfV + pistaOutN : plazaHalfV;

            const float cell = 3.3f;
            for (float u = -uInW; u < uInE - 0.02f; u += cell)
            {
                float u2 = Mathf.Min(u + cell, uInE);
                for (float v = -vInS; v < vInN - 0.02f; v += cell)
                {
                    float v2 = Mathf.Min(v + cell, vInN);
                    if (u2 - u < 0.15f || v2 - v < 0.15f) continue;
                    float cu = (u + u2) * 0.5f;
                    float cv = (v + v2) * 0.5f;
                    if (TapaFuente(u, v, u2, v2) || EnCama(u, v, u2, v2) || EnRuta(cu, cv)) continue;
                    LosaAlzada(grada, u, v, u2, v2, altoPlaza, altoPlaza, altoPlaza, altoPlaza, losa);
                }
            }

            if (ladoN) Muro(grada, -uInW, vInN, uInE, vInN, bajoPista, altoPlaza, contra);
            if (ladoS) Muro(grada, -uInW, -vInS, uInE, -vInS, bajoPista, altoPlaza, contra);
            if (ladoE) Muro(grada, uInE, -vInS, uInE, vInN, bajoPista, altoPlaza, contra);
            if (ladoW) Muro(grada, -uInW, -vInS, -uInW, vInN, bajoPista, altoPlaza, contra);

            if (ladoN) VeredaEnGrilla(vereda, -uOutW, vOutN, uOutE, vOutN + VeredaAncho, losa);
            if (ladoS) VeredaEnGrilla(vereda, -uOutW, -vOutS - VeredaAncho, uOutE, -vOutS, losa);
            if (ladoE) VeredaEnGrilla(vereda, uOutE, -vOutS, uOutE + VeredaAncho, vOutN, losa);
            if (ladoW) VeredaEnGrilla(vereda, -uOutW - VeredaAncho, -vOutS, -uOutW, vOutN, losa);
            if (ladoN && ladoE) VeredaEnGrilla(vereda, uOutE, vOutN, uOutE + VeredaAncho, vOutN + VeredaAncho, losa);
            if (ladoN && ladoW) VeredaEnGrilla(vereda, -uOutW - VeredaAncho, vOutN, -uOutW, vOutN + VeredaAncho, losa);
            if (ladoS && ladoE) VeredaEnGrilla(vereda, uOutE, -vOutS - VeredaAncho, uOutE + VeredaAncho, -vOutS, losa);
            if (ladoS && ladoW) VeredaEnGrilla(vereda, -uOutW - VeredaAncho, -vOutS - VeredaAncho, -uOutW, -vOutS, losa);

            if (ladoN) MuroConBoca(vereda, -uOutW, vOutN, uOutE, vOutN, bajoVereda, PistaAlta + Peldaño, contra, true, bocaCN, bocaHN);
            if (ladoS) MuroConBoca(vereda, -uOutW, -vOutS, uOutE, -vOutS, bajoVereda, PistaAlta + Peldaño, contra, true, bocaCS, bocaHS);
            if (ladoE) MuroConBoca(vereda, uOutE, -vOutS, uOutE, vOutN, bajoVereda, PistaAlta + Peldaño, contra, false, bocaCE, bocaHE);
            if (ladoW) MuroConBoca(vereda, -uOutW, -vOutS, -uOutW, vOutN, bajoVereda, PistaAlta + Peldaño, contra, false, bocaCW, bocaHW);
        }

        // La cruz y el corte de la pileta. Losa de plaza, no la piedra del paseo.
        // Las UV van en el eje de la plaza para que la losa no se estire.
        void VestirRutas(Pen pen)
        {
            const float cell = 3.3f;
            const float tile = 2.35f;
            float lift = PistaBaja + Peldaño + 0.035f;
            float u0 = -plazaHalfU;
            float u1 = plazaHalfU;
            float v0 = -plazaHalfV;
            float v1 = plazaHalfV;
            for (float u = u0; u < u1 - 0.02f; u += cell)
            {
                float ua = Mathf.Min(u + cell, u1);
                for (float v = v0; v < v1 - 0.02f; v += cell)
                {
                    float va = Mathf.Min(v + cell, v1);
                    if (ua - u < 0.15f || va - v < 0.15f) continue;
                    float cu = (u + ua) * 0.5f;
                    float cv = (v + va) * 0.5f;
                    if (!EnRuta(cu, cv)) continue;
                    Vector2 a = PlazaWorld(u, v);
                    Vector2 b = PlazaWorld(u, va);
                    Vector2 c = PlazaWorld(ua, va);
                    Vector2 d = PlazaWorld(ua, v);
                    pen.Quad(
                        new Vector3(a.x, CajamarcaAltura.Sample(a.x, a.y) + lift, a.y),
                        new Vector3(b.x, CajamarcaAltura.Sample(b.x, b.y) + lift, b.y),
                        new Vector3(c.x, CajamarcaAltura.Sample(c.x, c.y) + lift, c.y),
                        new Vector3(d.x, CajamarcaAltura.Sample(d.x, d.y) + lift, d.y),
                        Vector3.up, Losa,
                        new Vector2(u / tile, v / tile),
                        new Vector2(u / tile, va / tile),
                        new Vector2(ua / tile, va / tile),
                        new Vector2(ua / tile, v / tile));
                }
            }
        }

        bool TapaFuente(float u0, float v0, float u1, float v1)
        {
            float r2 = BocaFuente * BocaFuente;
            float cu = (u0 + u1) * 0.5f;
            float cv = (v0 + v1) * 0.5f;
            if (cu * cu + cv * cv < r2) return true;
            if (u0 * u0 + v0 * v0 < r2) return true;
            if (u0 * u0 + v1 * v1 < r2) return true;
            if (u1 * u1 + v0 * v0 < r2) return true;
            return u1 * u1 + v1 * v1 < r2;
        }

        bool EnCama(float u0, float v0, float u1, float v1)
        {
            float cu = (u0 + u1) * 0.5f;
            float cv = (v0 + v1) * 0.5f;
            return EnCesped(cu, cv);
        }

        void VeredaEnGrilla(Pen pen, float u0, float v0, float u1, float v1, Color color)
        {
            if (u1 < u0) { float t = u0; u0 = u1; u1 = t; }
            if (v1 < v0) { float t = v0; v0 = v1; v1 = t; }
            const float cell = 3.3f;
            float alto = PistaAlta + Peldaño;
            for (float u = u0; u < u1 - 0.02f; u += cell)
            {
                float u2 = Mathf.Min(u + cell, u1);
                for (float v = v0; v < v1 - 0.02f; v += cell)
                {
                    float v2 = Mathf.Min(v + cell, v1);
                    if (u2 - u < 0.15f || v2 - v < 0.15f) continue;
                    float cu = (u + u2) * 0.5f;
                    float cv = (v + v2) * 0.5f;
                    Vector2 p = PlazaWorld(cu, cv);
                    if (OnPlaza(p.x, p.y) || EsPista(p.x, p.y)) continue;
                    if (!JuntoAPista(cu, cv) && !EsEsquinaVereda(cu, cv)) continue;
                    LosaAlzada(pen, u, v, u2, v2, alto, alto, alto, alto, color);
                }
            }
        }

        bool JuntoAPista(float u, float v)
        {
            float su = Mathf.Abs(u) < 0.01f ? 0f : -Mathf.Sign(u);
            float sv = Mathf.Abs(v) < 0.01f ? 0f : -Mathf.Sign(v);
            for (int i = 1; i <= 3; i++)
            {
                float d = i * 0.9f;
                Vector2 q = PlazaWorld(u + su * d, v + sv * d);
                if (EsPista(q.x, q.y)) return true;
            }

            return false;
        }

        bool EsEsquinaVereda(float u, float v)
        {
            float au = Mathf.Abs(u);
            float av = Mathf.Abs(v);
            float outU = plazaHalfU + (u >= 0f ? pistaOutE : pistaOutW);
            float outV = plazaHalfV + (v >= 0f ? pistaOutN : pistaOutS);
            return au >= outU - 0.4f && av >= outV - 0.4f
                && au <= outU + VeredaAncho + 0.4f
                && av <= outV + VeredaAncho + 0.4f;
        }

        void MuroConBoca(Pen pen, float u0, float v0, float u1, float v1, float low, float high, Color color, bool aLoLargoDeU, float bocaC, float bocaH)
        {
            float g0 = bocaC - bocaH;
            float g1 = bocaC + bocaH;
            if (aLoLargoDeU)
            {
                float a = Mathf.Min(u0, u1);
                float b = Mathf.Max(u0, u1);
                float v = (v0 + v1) * 0.5f;
                if (g0 > a + 0.4f) Muro(pen, a, v, Mathf.Min(g0, b), v, low, high, color);
                if (g1 < b - 0.4f) Muro(pen, Mathf.Max(g1, a), v, b, v, low, high, color);
            }
            else
            {
                float a = Mathf.Min(v0, v1);
                float b = Mathf.Max(v0, v1);
                float u = (u0 + u1) * 0.5f;
                if (g0 > a + 0.4f) Muro(pen, u, a, u, Mathf.Min(g0, b), low, high, color);
                if (g1 < b - 0.4f) Muro(pen, u, Mathf.Max(g1, a), u, b, low, high, color);
            }
        }

        void HuellaRecta(Pen pen, float u0, float v0, float u1, float v1, float alto, Color color)
        {
            if (u1 - u0 < 0.4f || v1 - v0 < 0.2f) return;
            LosaAlzada(pen, u0, v0, u1, v1, alto, alto, alto, alto, color);
        }

        void DobleLinea(Pen pen, float desde, float hasta, float fijo, bool horizontal, Color color, float bocaC, float bocaH)
        {
            float corte0 = bocaC - bocaH - 1.2f;
            float corte1 = bocaC + bocaH + 1.2f;
            TramoDoble(pen, desde, Mathf.Min(hasta, corte0), fijo, horizontal, color);
            TramoDoble(pen, Mathf.Max(desde, corte1), hasta, fijo, horizontal, color);
        }

        void TramoDoble(Pen pen, float desde, float hasta, float fijo, bool horizontal, Color color)
        {
            if (hasta - desde < 1.2f) return;
            if (horizontal)
            {
                FranjaLocal(pen, desde, fijo - 0.16f, hasta, fijo - 0.16f, 0.07f, 0.025f, color, true);
                FranjaLocal(pen, desde, fijo + 0.16f, hasta, fijo + 0.16f, 0.07f, 0.025f, color, true);
            }
            else
            {
                FranjaLocal(pen, fijo - 0.16f, desde, fijo - 0.16f, hasta, 0.07f, 0.025f, color, true);
                FranjaLocal(pen, fijo + 0.16f, desde, fijo + 0.16f, hasta, 0.07f, 0.025f, color, true);
            }
        }

        // Línea de borde, un poco adentro del cordón. Se corta en la boca.
        void BordeCalzada(Pen pen, float desde, float hasta, float fijo, bool horizontal, float bocaC, float bocaH)
        {
            var blanco = new Color(0.94f, 0.94f, 0.91f);
            float corte0 = bocaC - bocaH - 1.2f;
            float corte1 = bocaC + bocaH + 1.2f;
            TramoBorde(pen, desde, Mathf.Min(hasta, corte0), fijo, horizontal, blanco);
            TramoBorde(pen, Mathf.Max(desde, corte1), hasta, fijo, horizontal, blanco);
        }

        void TramoBorde(Pen pen, float desde, float hasta, float fijo, bool horizontal, Color color)
        {
            if (hasta - desde < 1.2f) return;
            if (horizontal)
                FranjaLocal(pen, desde, fijo, hasta, fijo, 0.09f, 0.04f, color, true);
            else
                FranjaLocal(pen, fijo, desde, fijo, hasta, 0.09f, 0.04f, color, true);
        }

        static float MargenPista(float inner, float outer)
        {
            return Mathf.Min(0.42f, Mathf.Max(0.28f, (outer - inner) * 0.16f));
        }

        // La casona mira al cruce. El ochavo da a la esquina y el cuerpo
        // queda en la manzana, detrás de las dos pistas.
        void CasasDeEsquina(Transform parent)
        {
            if (!pistaLista) return;
            QuitarCasasDelCruce();
        }

        void PonerEsquina(Transform parent, float su, float sv, float outU, float outV, int cual)
        {
            float retiro = VeredaAncho + 0.06f;
            const float ochavo = 3.5f;
            const float fondo = 7.2f;
            const float largo = 11.2f;
            const float alto = 6.8f;
            float u0 = su * (plazaHalfU + outU + retiro);
            float v0 = sv * (plazaHalfV + outV + retiro);
            float uc = u0 + su * (fondo * 0.65f);
            float vc = v0 + sv * (fondo * 0.65f);
            Vector2 centro = PlazaWorld(uc, vc);
            Vector2 ochavoPt = PlazaWorld(u0, v0);
            for (int n = 0; n < 4 && (OnPlaza(ochavoPt.x, ochavoPt.y) || EsPista(ochavoPt.x, ochavoPt.y)); n++)
            {
                uc += su * 0.45f;
                vc += sv * 0.45f;
                u0 += su * 0.45f;
                v0 += sv * 0.45f;
                centro = PlazaWorld(uc, vc);
                ochavoPt = PlazaWorld(u0, v0);
            }

            Vector2 esquina = PlazaWorld(u0, v0);
            Vector2 puntaU = PlazaWorld(u0 + su * (ochavo + largo), v0 + sv * (fondo * 0.35f));
            Vector2 puntaV = PlazaWorld(u0 + su * (fondo * 0.35f), v0 + sv * (ochavo + largo));
            if (TocaIglesia(centro.x, centro.y, 5f)
                || TocaIglesia(esquina.x, esquina.y, 4f)
                || TocaIglesia(puntaU.x, puntaU.y, 3.5f)
                || TocaIglesia(puntaV.x, puntaV.y, 3.5f))
                return;
            var tinta = AdobeTint[cual % AdobeTint.Length];
            var madera = new Color(0.42f, 0.26f, 0.14f);
            float ua = u0 + su * (ochavo - 0.25f);
            float ub = u0 + su * (ochavo + largo);
            float pisoMuro = PistaAlta + Peldaño;
            CajaPlaza(parent, "CasonaEsquina", (ua + ub) * 0.5f, v0 + sv * (fondo * 0.5f),
                Mathf.Abs(ub - ua), fondo, alto, "Calle/muro_adobe", tinta, true, pisoMuro);
            float va = v0 + sv * (ochavo - 0.25f);
            float vb = v0 + sv * (ochavo + largo);
            CajaPlaza(parent, "CasonaEsquina", u0 + su * (fondo * 0.5f), (va + vb) * 0.5f,
                fondo, Mathf.Abs(vb - va), alto, "Calle/muro_adobe", tinta, false, pisoMuro);
            CajaPlaza(parent, "CasonaEsquina", u0 + su * (ochavo * 0.55f + fondo * 0.45f), v0 + sv * (ochavo * 0.55f + fondo * 0.45f),
                fondo * 0.92f, fondo * 0.92f, alto, "Calle/muro_adobe", tinta, true, pisoMuro);

            float tu = u0 + su * (ochavo * 0.5f);
            float tv = v0 + sv * (ochavo * 0.5f);
            Vector3 hacia = (-su * EjeU() - sv * EjeV()).normalized;
            Vector2 boca = PlazaWorld(tu, tv);
            float pisoCasa = PistaAlta + Peldaño;
            float suelo = CajamarcaAltura.Sample(boca.x, boca.y) + pisoCasa;
            var muro = BoxPart(parent, "CasonaEsquina", new Vector3(boca.x, suelo + alto * 0.5f, boca.y),
                new Vector3(ochavo * 1.35f, alto, 0.42f), "Calle/muro_adobe", tinta);
            muro.transform.rotation = Mirar(hacia);
            var puerta = BoxPart(parent, "CasonaPuerta", new Vector3(boca.x, suelo + 1.35f, boca.y) + hacia * 0.28f,
                new Vector3(1.35f, 2.5f, 0.16f), "Calle/madera_oscura", Color.white);
            puerta.transform.rotation = Mirar(hacia);
            var balcon = BoxPart(parent, "CasonaBalcon", new Vector3(boca.x, suelo + 3.55f, boca.y) + hacia * 0.55f,
                new Vector3(2.6f, 0.12f, 0.7f), "Calle/madera_oscura", madera);
            balcon.transform.rotation = Mirar(hacia);
            var baranda = BoxPart(parent, "CasonaBalcon", new Vector3(boca.x, suelo + 4.05f, boca.y) + hacia * 0.82f,
                new Vector3(2.6f, 0.72f, 0.08f), "Calle/madera_oscura", madera);
            baranda.transform.rotation = Mirar(hacia);

            CajaPlaza(parent, "CasonaTecho", (ua + ub) * 0.5f, v0 + sv * (fondo * 0.5f),
                Mathf.Abs(ub - ua) + 0.7f, fondo + 0.8f, 0.42f, "Calle/teja", Color.white, true, alto + pisoCasa);
            CajaPlaza(parent, "CasonaTecho", u0 + su * (fondo * 0.5f), (va + vb) * 0.5f,
                fondo + 0.8f, Mathf.Abs(vb - va) + 0.7f, 0.42f, "Calle/teja", Color.white, false, alto + pisoCasa);
            casonas.Add(centro);
        }

        Vector3 EjeU()
        {
            return new Vector3(plazaCos, 0f, plazaSin);
        }

        Vector3 EjeV()
        {
            return new Vector3(-plazaSin, 0f, plazaCos);
        }

        Quaternion Mirar(Vector3 hacia)
        {
            if (hacia.sqrMagnitude < 0.0001f) hacia = Vector3.forward;
            Vector3 mundo = district != null ? district.TransformDirection(hacia) : hacia;
            Vector3 arriba = district != null ? district.up : Vector3.up;
            return Quaternion.LookRotation(mundo, arriba);
        }

        void CajaPlaza(Transform parent, string name, float u, float v, float largoU, float largoV, float alto,
            string tex, Color tinta, bool aLoLargoDeU, float yPiso = 0f)
        {
            Vector2 w = PlazaWorld(u, v);
            float g = CajamarcaAltura.Sample(w.x, w.y) + yPiso;
            Vector3 eje = aLoLargoDeU ? EjeU() : EjeV();
            var go = BoxPart(parent, name, new Vector3(w.x, g + alto * 0.5f, w.y),
                new Vector3(aLoLargoDeU ? largoV : largoU, alto, aLoLargoDeU ? largoU : largoV), tex, tinta);
            go.transform.rotation = Mirar(eje);
        }

        bool CasaSobrePista(float x, float z)
        {
            if (!pistaLista) return false;
            if (SobrePista(x, z, 2.4f)) return true;
            if (SobrePista(x + 3.2f, z, 0.4f) || SobrePista(x - 3.2f, z, 0.4f)) return true;
            if (SobrePista(x, z + 3.2f, 0.4f) || SobrePista(x, z - 3.2f, 0.4f)) return true;
            return false;
        }

        bool SobrePista(float x, float z, float holgura)
        {
            if (!pistaLista) return false;
            Vector2 loc = PlazaLocal(x, z);
            float au = Mathf.Abs(loc.x);
            float av = Mathf.Abs(loc.y);
            float vereda = VeredaAncho;
            bool norteSur = av >= plazaHalfV + Mathf.Min(pistaInN, pistaInS) - holgura
                && av <= plazaHalfV + Mathf.Max(pistaOutN, pistaOutS) + vereda + holgura
                && au <= plazaHalfU + Mathf.Max(pistaOutE, pistaOutW) + vereda + holgura;
            bool esteOeste = au >= plazaHalfU + Mathf.Min(pistaInE, pistaInW) - holgura
                && au <= plazaHalfU + Mathf.Max(pistaOutE, pistaOutW) + vereda + holgura
                && av <= plazaHalfV + Mathf.Max(pistaOutN, pistaOutS) + vereda + holgura;
            return norteSur || esteOeste;
        }

        bool CercaDeCasona(float x, float z)
        {
            for (int i = 0; i < casonas.Count; i++)
            {
                float dx = casonas[i].x - x;
                float dz = casonas[i].y - z;
                if (dx * dx + dz * dz < 13f * 13f) return true;
            }

            return false;
        }

        void ArmarIglesias()
        {
            if (iglesiasListas) return;
            iglesiasListas = true;
            iglesias.Clear();
            if (district == null) return;
            var renders = district.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renders.Length; i++)
            {
                if (!EsIglesia(renders[i].gameObject.name)) continue;
                Bounds caja = renders[i].bounds;
                if (caja.size.x < 2f && caja.size.z < 2f) continue;
                iglesias.Add(caja);
            }
        }

        static bool EsIglesia(string nombre)
        {
            if (string.IsNullOrEmpty(nombre)) return false;
            return nombre.IndexOf("Portada", System.StringComparison.Ordinal) >= 0
                || nombre.IndexOf("Catedral", System.StringComparison.Ordinal) >= 0
                || nombre.IndexOf("Francisco", System.StringComparison.Ordinal) >= 0
                || nombre.IndexOf("Capilla", System.StringComparison.Ordinal) >= 0
                || nombre.IndexOf("Iglesia", System.StringComparison.Ordinal) >= 0;
        }

        bool TocaIglesia(float x, float z, float radio)
        {
            if (district == null) return false;
            ArmarIglesias();
            Vector3 w = district.TransformPoint(new Vector3(x, 0f, z));
            float r2 = radio * radio;
            for (int i = 0; i < iglesias.Count; i++)
            {
                Bounds caja = iglesias[i];
                float cx = Mathf.Clamp(w.x, caja.min.x, caja.max.x);
                float cz = Mathf.Clamp(w.z, caja.min.z, caja.max.z);
                float dx = cx - w.x;
                float dz = cz - w.z;
                if (dx * dx + dz * dz <= r2) return true;
            }

            return false;
        }

        bool EnAnilloDePlaza(float x, float z)
        {
            if (!plazaFound) return false;
            if (OnPlaza(x, z) && !EsPista(x, z)) return true;
            Vector2 loc = PlazaLocal(x, z);
            float au = Mathf.Abs(loc.x);
            float av = Mathf.Abs(loc.y);
            float extra = pistaLista
                ? Mathf.Max(pistaOutN, pistaOutS, pistaOutE, pistaOutW)
                : PlazaWalk;
            float outU = plazaHalfU + VeredaAncho + extra + 12f;
            float outV = plazaHalfV + VeredaAncho + extra + 12f;
            if (au > outU || av > outV) return false;
            return au >= plazaHalfU - 1.2f || av >= plazaHalfV - 1.2f;
        }

        bool EnCruce(float x, float z)
        {
            if (!pistaLista) return false;
            Vector2 loc = PlazaLocal(x, z);
            float au = Mathf.Abs(loc.x);
            float av = Mathf.Abs(loc.y);
            float inU = plazaHalfU + Mathf.Min(pistaInE, pistaInW) - 0.6f;
            float outU = plazaHalfU + Mathf.Max(pistaOutE, pistaOutW) + 0.8f;
            float inV = plazaHalfV + Mathf.Min(pistaInN, pistaInS) - 0.6f;
            float outV = plazaHalfV + Mathf.Max(pistaOutN, pistaOutS) + 0.8f;
            return au >= inU && au <= outU && av >= inV && av <= outV;
        }

        void QuitarCasasDelCruce()
        {
            Transform casas = district != null ? district.Find("Casas") : null;
            if (casas == null) return;
            for (int i = 0; i < casas.childCount; i++)
            {
                Transform casa = casas.GetChild(i);
                if (EsIglesia(casa.name)) continue;
                var rend = casa.GetComponentInChildren<Renderer>();
                if (rend == null) continue;
                Vector3 tam = rend.bounds.size;
                if (Mathf.Max(tam.x, tam.z) > 36f) continue;
                Vector3 centro = district.InverseTransformPoint(rend.bounds.center);
                bool quitar = EnCruce(centro.x, centro.z)
                    || CercaDeCasona(centro.x, centro.z)
                    || EnAnilloDePlaza(centro.x, centro.z);
                if (!quitar)
                {
                    Vector3 c = rend.bounds.center;
                    Vector3 e = rend.bounds.extents;
                    var extras = new[]
                    {
                        c + new Vector3(e.x, 0f, e.z),
                        c + new Vector3(-e.x, 0f, e.z),
                        c + new Vector3(e.x, 0f, -e.z),
                        c + new Vector3(-e.x, 0f, -e.z),
                    };
                    for (int k = 0; k < extras.Length; k++)
                    {
                        Vector3 q = district.InverseTransformPoint(extras[k]);
                        if (!EnCruce(q.x, q.z)) continue;
                        quitar = true;
                        break;
                    }
                }

                if (quitar) casa.gameObject.SetActive(false);
            }
        }

        void CebraSi(Pen pen, float u, float v, float half, bool cruzaU)
        {
            Vector2 p = PlazaWorld(u, v);
            if (!EsPista(p.x, p.y)) return;
            Cebra(pen, u, v, half, cruzaU);
        }

        void Cebra(Pen pen, float u, float v, float half, bool cruzaU)
        {
            const int barras = 6;
            for (int i = 0; i < barras; i++)
            {
                float s = (i - (barras - 1) * 0.5f) * 0.85f;
                float u0;
                float v0;
                float u1;
                float v1;
                if (cruzaU)
                {
                    u0 = u + s - 0.22f;
                    u1 = u + s + 0.22f;
                    v0 = v - half;
                    v1 = v + half;
                }
                else
                {
                    u0 = u - half;
                    u1 = u + half;
                    v0 = v + s - 0.22f;
                    v1 = v + s + 0.22f;
                }

                LosaAlzada(pen, u0, v0, u1, v1,
                    AlturaCalzada(u0, v0) + 0.03f,
                    AlturaCalzada(u0, v1) + 0.03f,
                    AlturaCalzada(u1, v1) + 0.03f,
                    AlturaCalzada(u1, v0) + 0.03f,
                    Color.white);
            }
        }

        // Dos rayas de pare, una a cada lado de la cebra de la boca.
        // Las de la esquina se quedan sin raya: ahí el cruce ya es la esquina.
        void TopeCebra(Pen pen, float u, float v, float half, bool cruzaU)
        {
            Vector2 centro = PlazaWorld(u, v);
            if (!EsPista(centro.x, centro.y)) return;
            const float fuera = 2.7f;
            const float grueso = 0.28f;
            const float alza = 0.05f;
            for (int s = -1; s <= 1; s += 2)
            {
                float c = s * fuera;
                float u0;
                float v0;
                float u1;
                float v1;
                if (cruzaU)
                {
                    u0 = u + c - grueso * 0.5f;
                    u1 = u + c + grueso * 0.5f;
                    v0 = v - half;
                    v1 = v + half;
                }
                else
                {
                    u0 = u - half;
                    u1 = u + half;
                    v0 = v + c - grueso * 0.5f;
                    v1 = v + c + grueso * 0.5f;
                }

                float cu = (u0 + u1) * 0.5f;
                float cv = (v0 + v1) * 0.5f;
                if (!EsPistaUv(cu, cv)) continue;
                LosaAlzada(pen, u0, v0, u1, v1,
                    AlturaCalzada(u0, v0) + alza,
                    AlturaCalzada(u0, v1) + alza,
                    AlturaCalzada(u1, v1) + alza,
                    AlturaCalzada(u1, v0) + alza,
                    Color.white);
            }
        }

        float AlturaCalzada(float u, float v)
        {
            float du = Mathf.Abs(u) - plazaHalfU;
            float dv = Mathf.Abs(v) - plazaHalfV;
            float inU = u >= 0f ? pistaInE : pistaInW;
            float outU = u >= 0f ? pistaOutE : pistaOutW;
            float inV = v >= 0f ? pistaInN : pistaInS;
            float outV = v >= 0f ? pistaOutN : pistaOutS;
            float tU = Mathf.InverseLerp(inU, Mathf.Max(inU + 0.2f, outU), du);
            float tV = Mathf.InverseLerp(inV, Mathf.Max(inV + 0.2f, outV), dv);
            float t = Mathf.Max(Mathf.Clamp01(tU), Mathf.Clamp01(tV));
            return Mathf.Lerp(PistaBaja, PistaAlta, t) + PistaPiel;
        }

        // Asfalto en celdas chicas, con la veta en el eje de la plaza.
        // La losa de piedra sigue en el mundo: esta sólo cubre la calzada.
        void LosaAsfalto(Pen pen, float u0, float v0, float u1, float v1)
        {
            if (u1 - u0 < 0.15f || v1 - v0 < 0.15f) return;
            const float cell = 1.55f;
            const float tile = 2.6f;
            for (float u = u0; u < u1 - 0.02f; u += cell)
            {
                float u2 = Mathf.Min(u + cell, u1);
                for (float v = v0; v < v1 - 0.02f; v += cell)
                {
                    float v2 = Mathf.Min(v + cell, v1);
                    if (u2 - u < 0.15f || v2 - v < 0.15f) continue;
                    float cu = (u + u2) * 0.5f;
                    float cv = (v + v2) * 0.5f;
                    if (!EsPistaUv(cu, cv)) continue;
                    Vector2 a = PlazaWorld(u, v);
                    Vector2 b = PlazaWorld(u, v2);
                    Vector2 c = PlazaWorld(u2, v2);
                    Vector2 d = PlazaWorld(u2, v);
                    pen.Quad(
                        new Vector3(a.x, CajamarcaAltura.Sample(a.x, a.y) + AlturaCalzada(u, v), a.y),
                        new Vector3(b.x, CajamarcaAltura.Sample(b.x, b.y) + AlturaCalzada(u, v2), b.y),
                        new Vector3(c.x, CajamarcaAltura.Sample(c.x, c.y) + AlturaCalzada(u2, v2), c.y),
                        new Vector3(d.x, CajamarcaAltura.Sample(d.x, d.y) + AlturaCalzada(u2, v), d.y),
                        Vector3.up, Color.white,
                        new Vector2(u / tile, v / tile),
                        new Vector2(u / tile, v2 / tile),
                        new Vector2(u2 / tile, v2 / tile),
                        new Vector2(u2 / tile, v / tile));
                }
            }
        }

        bool EsPistaUv(float u, float v)
        {
            Vector2 p = PlazaWorld(u, v);
            return EsPista(p.x, p.y);
        }

        void LosaAlzada(Pen pen, float u0, float v0, float u1, float v1, float h00, float h01, float h11, float h10, Color color)
        {
            if (u1 - u0 < 0.15f || v1 - v0 < 0.15f) return;
            Vector2 a = PlazaWorld(u0, v0);
            Vector2 b = PlazaWorld(u0, v1);
            Vector2 c = PlazaWorld(u1, v1);
            Vector2 d = PlazaWorld(u1, v0);
            pen.Quad(
                new Vector3(a.x, CajamarcaAltura.Sample(a.x, a.y) + h00, a.y),
                new Vector3(b.x, CajamarcaAltura.Sample(b.x, b.y) + h01, b.y),
                new Vector3(c.x, CajamarcaAltura.Sample(c.x, c.y) + h11, c.y),
                new Vector3(d.x, CajamarcaAltura.Sample(d.x, d.y) + h10, d.y),
                Vector3.up, color, false);
        }

        void Muro(Pen pen, float u0, float v0, float u1, float v1, float low, float high, Color color)
        {
            float du = u1 - u0;
            float dv = v1 - v0;
            float len = Mathf.Sqrt(du * du + dv * dv);
            if (len < 0.3f || high - low < 0.04f) return;
            int n = Mathf.Max(1, Mathf.CeilToInt(len / 4f));
            for (int i = 0; i < n; i++)
            {
                float t0 = i / (float)n;
                float t1 = (i + 1) / (float)n;
                Vector2 a = PlazaWorld(Mathf.Lerp(u0, u1, t0), Mathf.Lerp(v0, v1, t0));
                Vector2 b = PlazaWorld(Mathf.Lerp(u0, u1, t1), Mathf.Lerp(v0, v1, t1));
                float ya = CajamarcaAltura.Sample(a.x, a.y);
                float yb = CajamarcaAltura.Sample(b.x, b.y);
                pen.Quad(
                    new Vector3(a.x, ya + low, a.y),
                    new Vector3(b.x, yb + low, b.y),
                    new Vector3(b.x, yb + high, b.y),
                    new Vector3(a.x, ya + high, a.y),
                    Vector3.up, color, false);
            }
        }

        bool BandaPlaza(int side, out float lo, out float hi)
        {
            lo = 0f;
            hi = 0f;
            bool found = false;
            for (float d = 2.4f; d <= 22f; d += 0.45f)
            {
                float u = 0f;
                float v = 0f;
                if (side == -1) v = -(plazaHalfV + d);
                else if (side == 1) v = plazaHalfV + d;
                else if (side == -2) u = -(plazaHalfU + d);
                else u = plazaHalfU + d;
                Vector2 p = PlazaWorld(u, v);
                bool street = EsPista(p.x, p.y);
                if (street)
                {
                    if (!found) lo = d;
                    hi = d;
                    found = true;
                }
                else if (found) break;
            }

            return found && hi - lo > 2f;
        }

        static Texture2D asfaltoTex;

        static int GranoAsfalto(int x, int y)
        {
            int h = x * 374761393 + y * 668265263;
            h = (h ^ (h >> 13)) * 1274126177;
            return h ^ (h >> 16);
        }

        static Texture2D Asfalto()
        {
            if (asfaltoTex != null) return asfaltoTex;
            const int n = 64;
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, true);
            var pix = new Color32[n * n];
            for (int y = 0; y < n; y++)
            {
                for (int x = 0; x < n; x++)
                {
                    int h = GranoAsfalto(x, y);
                    int g = 47 + (h & 11);
                    if ((h & 63) == 7) g += 16;
                    else if ((h & 63) == 19) g -= 9;
                    bool grieta = ((y & 15) == 3 && ((x / 3 + y) % 11) < 2)
                        || (x % 18 == 7 && (y / 3) % 9 < 2);
                    if (grieta) g -= 12;
                    g = Mathf.Clamp(g, 28, 74);
                    pix[y * n + x] = new Color32((byte)(g - 2), (byte)g, (byte)(g + 3), 255);
                }
            }

            tex.SetPixels32(pix);
            tex.wrapMode = TextureWrapMode.Repeat;
            tex.filterMode = FilterMode.Bilinear;
            tex.Apply(true);
            asfaltoTex = tex;
            return tex;
        }

        struct EjeCalle
        {
            public string Nombre;
            public float X, Z, Tx, Tz, Largo;
        }

        // caras.json dice qué textura lleva el frente de cada calle.
        // Si la cara es la de siempre, el muro horneado se queda.
        void AplicarCaras()
        {
            if (plan == null || dynamicRoot == null) return;
            var asset = Resources.Load<TextAsset>("Calle/caras");
            if (asset == null) return;
            CaraSet set = null;
            try
            {
                set = JsonUtility.FromJson<CaraSet>(asset.text);
            }
            catch (System.Exception)
            {
                set = null;
            }

            if (set == null || set.calles == null || set.calles.Length == 0) return;
            Transform muros = plan.transform.Find("PlanoMuros");
            var filter = muros != null ? muros.GetComponent<MeshFilter>() : null;
            if (filter == null || filter.sharedMesh == null || !filter.sharedMesh.isReadable)
            {
                PintarCarasDeLista(set);
                return;
            }
            var mesh = filter.sharedMesh;
            var verts = mesh.vertices;
            var uvs = mesh.uv;
            var cols = mesh.colors;
            bool hasUv = uvs != null && uvs.Length == verts.Length;
            bool hasCol = cols != null && cols.Length == verts.Length;
            var ejes = Ejes();
            if (ejes.Count == 0) return;
            var grupos = new Dictionary<string, List<int>>();
            var space = filter.transform;
            int sub = mesh.subMeshCount;
            for (int s = 0; s < sub; s++)
            {
                var tris = mesh.GetTriangles(s);
                for (int t = 0; t + 2 < tris.Length; t += 3)
                {
                    int i0 = tris[t];
                    int i1 = tris[t + 1];
                    int i2 = tris[t + 2];
                    if (i0 < 0 || i1 < 0 || i2 < 0 || i0 >= verts.Length || i1 >= verts.Length || i2 >= verts.Length) continue;
                    Vector3 a = district.InverseTransformPoint(space.TransformPoint(verts[i0]));
                    Vector3 b = district.InverseTransformPoint(space.TransformPoint(verts[i1]));
                    Vector3 c = district.InverseTransformPoint(space.TransformPoint(verts[i2]));
                    Vector3 n = Vector3.Cross(b - a, c - a);
                    if (Mathf.Abs(n.y) > Mathf.Abs(n.x) + Mathf.Abs(n.z)) continue;
                    n.y = 0f;
                    if (n.sqrMagnitude < 0.0001f) continue;
                    n.Normalize();
                    Vector3 mid = (a + b + c) / 3f;
                    string cara = CaraFrente(set, ejes, mid.x, mid.z, n.x, n.z);
                    if (cara == null) continue;
                    if (!grupos.TryGetValue(cara, out var lista))
                    {
                        lista = new List<int>();
                        grupos[cara] = lista;
                    }

                    lista.Add(i0);
                    lista.Add(i1);
                    lista.Add(i2);
                }
            }

            if (grupos.Count == 0) return;
            var shader = Shader.Find("AYNI/Calle") ?? Shader.Find("Standard") ?? Shader.Find("Sprites/Default");
            foreach (var par in grupos)
            {
                var tex = Resources.Load<Texture2D>(par.Key);
                if (tex == null) continue;
                var lista = par.Value;
                int count = lista.Count;
                var nv = new Vector3[count];
                var nu = new Vector2[count];
                var nc = new Color[count];
                var nt = new int[count];
                for (int k = 0; k < count; k++)
                {
                    int src = lista[k];
                    Vector3 p = district.InverseTransformPoint(space.TransformPoint(verts[src]));
                    nv[k] = p;
                    nu[k] = hasUv ? uvs[src] : Vector2.zero;
                    nc[k] = hasCol ? cols[src] : Color.white;
                    nt[k] = k;
                }

                for (int k = 0; k + 2 < count; k += 3)
                {
                    Vector3 n = Vector3.Cross(nv[k + 1] - nv[k], nv[k + 2] - nv[k]);
                    n.y = 0f;
                    if (n.sqrMagnitude < 0.0001f) continue;
                    n.Normalize();
                    nv[k] += n * 0.06f;
                    nv[k + 1] += n * 0.06f;
                    nv[k + 2] += n * 0.06f;
                }

                var host = new GameObject("Cara " + par.Key.Replace('/', ' '));
                host.transform.SetParent(dynamicRoot, false);
                var hecho = new Mesh { name = host.name };
                if (count > 65000) hecho.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
                hecho.vertices = nv;
                hecho.uv = nu;
                hecho.colors = nc;
                hecho.triangles = nt;
                hecho.RecalculateNormals();
                hecho.RecalculateBounds();
                host.AddComponent<MeshFilter>().sharedMesh = hecho;
                var renderer = host.AddComponent<MeshRenderer>();
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = false;
                if (shader != null)
                {
                    var mat = new Material(shader);
                    tex.wrapMode = TextureWrapMode.Repeat;
                    mat.mainTexture = tex;
                    renderer.sharedMaterial = mat;
                }
            }
        }

        string CaraFrente(CaraSet set, List<EjeCalle> ejes, float x, float z, float nx, float nz)
        {
            float best = 14f;
            string nombre = null;
            var normal = new Vector2(nx, nz);
            for (int i = 0; i < ejes.Count; i++)
            {
                EjeCalle e = ejes[i];
                float dx = x - e.X;
                float dz = z - e.Z;
                float along = dx * e.Tx + dz * e.Tz;
                if (along < -6f || along > e.Largo + 6f) continue;
                float lx = dx - e.Tx * along;
                float lz = dz - e.Tz * along;
                float lateral = Mathf.Sqrt(lx * lx + lz * lz);
                if (lateral < 2.8f || lateral > best) continue;
                var hacia = new Vector2(-lx, -lz);
                if (Vector2.Dot(normal, hacia) <= 0.2f * lateral) continue;
                best = lateral;
                nombre = e.Nombre;
            }

            if (nombre == null) return null;
            string defecto = string.IsNullOrEmpty(set.defecto) ? "Calle/fachada_colonial" : set.defecto;
            for (int i = 0; i < set.calles.Length; i++)
            {
                var fila = set.calles[i];
                if (fila == null || string.IsNullOrEmpty(fila.nombre)) continue;
                if (!string.Equals(fila.nombre, nombre, System.StringComparison.OrdinalIgnoreCase)) continue;
                if (string.IsNullOrEmpty(fila.cara) || string.Equals(fila.cara, defecto, System.StringComparison.OrdinalIgnoreCase))
                    return null;
                return fila.cara;
            }

            return null;
        }

        List<EjeCalle> Ejes()
        {
            if (ejesCache != null) return ejesCache;
            var lista = new List<EjeCalle>();
            var asset = Resources.Load<TextAsset>("Calle/calles");
            if (asset != null)
            {
                StreetLabelSet calles = null;
                try
                {
                    calles = JsonUtility.FromJson<StreetLabelSet>(asset.text);
                }
                catch (System.Exception)
                {
                    calles = null;
                }

                if (calles != null && calles.streets != null)
                {
                    for (int i = 0; i < calles.streets.Length; i++)
                    {
                        var s = calles.streets[i];
                        if (s == null || string.IsNullOrEmpty(s.name)) continue;
                        float rad = s.angle * Mathf.Deg2Rad;
                        lista.Add(new EjeCalle
                        {
                            Nombre = s.name,
                            X = s.x - Mathf.Cos(rad) * 110f,
                            Z = s.z - Mathf.Sin(rad) * 110f,
                            Tx = Mathf.Cos(rad),
                            Tz = Mathf.Sin(rad),
                            Largo = 220f,
                        });
                    }
                }
            }

            var tramos = LoadTramos();
            if (tramos != null && tramos.tramos != null)
            {
                for (int i = 0; i < tramos.tramos.Length; i++)
                {
                    var t = tramos.tramos[i];
                    if (t == null || string.IsNullOrEmpty(t.name)) continue;
                    float dx = t.x1 - t.x0;
                    float dz = t.z1 - t.z0;
                    float len = Mathf.Sqrt(dx * dx + dz * dz);
                    if (len < 6f) continue;
                    lista.Add(new EjeCalle
                    {
                        Nombre = t.name,
                        X = t.x0,
                        Z = t.z0,
                        Tx = dx / len,
                        Tz = dz / len,
                        Largo = len,
                    });
                }
            }

            ejesCache = lista;
            return lista;
        }

        [System.Serializable]
        sealed class CaraFila
        {
            public string nombre;
            public string cara;
        }

        [System.Serializable]
        sealed class CaraSet
        {
            public string defecto;
            public CaraFila[] calles;
        }

        bool PlantProp(Transform parent, string etiqueta, float x, float z, float ground, float height, float yaw, params string[] resources)
        {
            if (resources == null) return false;
            for (int i = 0; i < resources.Length; i++)
            {
                if (string.IsNullOrEmpty(resources[i])) continue;
                if (Resources.Load<GameObject>(resources[i]) == null) continue;
                PlantTree(resources[i], x, z, height, yaw, ground, parent, etiqueta);
                return true;
            }

            return false;
        }

        void PlantTree(string resource, float x, float z, float height, float yaw, float ground = 0f, Transform parent = null, string etiqueta = null)
        {
            var prefab = Resources.Load<GameObject>(resource);
            if (prefab == null) return;
            var host = parent != null ? parent : district;
            var tree = Instantiate(prefab, host);
            tree.name = string.IsNullOrEmpty(etiqueta) ? "Arbol" : etiqueta;
            foreach (var col in tree.GetComponentsInChildren<Collider>(true))
                Kill(col);
            tree.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
            tree.transform.localPosition = new Vector3(x, ground, z);
            tree.transform.localScale = Vector3.one;
            float native = CrateSpan(tree, out _);
            tree.transform.localScale = Vector3.one * (height / Mathf.Max(0.2f, native));
            CrateSpan(tree, out float minY);
            Vector3 anchor = host != null
                ? host.TransformPoint(new Vector3(x, ground, z))
                : new Vector3(x, ground, z);
            tree.transform.position += new Vector3(0f, anchor.y + 0.12f - minY, 0f);

            var fallback = Shader.Find("Standard") ?? Shader.Find("AYNI/Calle") ?? Shader.Find("Sprites/Default");
            foreach (var renderer in tree.GetComponentsInChildren<Renderer>(true))
            {
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = false;
                var mats = renderer.sharedMaterials;
                bool changed = false;
                for (int m = 0; m < mats.Length; m++)
                {
                    var mat = mats[m];
                    if (mat == null) continue;
                    if (TinteDeArbol(mat.name, out var tint))
                    {
                        mats[m] = RepaintTree(mat, tint);
                        changed = true;
                        continue;
                    }

                    bool broken = mat == null || mat.shader == null
                        || mat.shader.name.IndexOf("Error", System.StringComparison.Ordinal) >= 0
                        || mat.shader.name.IndexOf("Universal", System.StringComparison.Ordinal) >= 0;
                    if (!broken || fallback == null) continue;
                    var neu = new Material(fallback);
                    Texture mainTex = mat != null ? mat.mainTexture : null;
                    if (mainTex == null && mat != null && mat.HasProperty("_BaseMap"))
                        mainTex = mat.GetTexture("_BaseMap");
                    if (mainTex != null && neu.HasProperty("_MainTex")) neu.mainTexture = mainTex;
                    if (mat != null && mat.HasProperty("_Color")) neu.color = mat.color;
                    else if (mat != null && mat.HasProperty("_BaseColor")) neu.color = mat.GetColor("_BaseColor");
                    bool hoja = mat.name.IndexOf("leaf", System.StringComparison.OrdinalIgnoreCase) >= 0
                        || mat.name.IndexOf("Leaf", System.StringComparison.Ordinal) >= 0;
                    if (hoja && neu.HasProperty("_Mode"))
                    {
                        neu.SetFloat("_Mode", 1f);
                        neu.SetOverrideTag("RenderType", "TransparentCutout");
                        neu.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                        neu.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
                        neu.SetInt("_ZWrite", 1);
                        neu.EnableKeyword("_ALPHATEST_ON");
                        neu.DisableKeyword("_ALPHABLEND_ON");
                        neu.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                        neu.renderQueue = 2450;
                        if (neu.HasProperty("_Cutoff")) neu.SetFloat("_Cutoff", 0.35f);
                    }
                    else if (mat != null && mat.HasProperty("_Cutoff") && neu.HasProperty("_Cutoff"))
                        neu.SetFloat("_Cutoff", mat.GetFloat("_Cutoff"));
                    mats[m] = neu;
                    changed = true;
                }

                if (changed) renderer.sharedMaterials = mats;
            }
        }

        // Los FBX de árboles traen colores incrustados que salen cian (hojas)
        // y rosados (troncos). Se corrigen por nombre de material.
        static readonly Dictionary<string, Color> TreePalette = new Dictionary<string, Color>
        {
            { "leafsDark", new Color(0.22f, 0.40f, 0.16f) },
            { "leafsGreen", new Color(0.30f, 0.52f, 0.22f) },
            { "woodBarkDark", new Color(0.42f, 0.28f, 0.17f) },
            { "woodBark", new Color(0.52f, 0.36f, 0.22f) },
        };

        static bool TinteDeArbol(string nombre, out Color tinta)
        {
            tinta = Color.white;
            if (string.IsNullOrEmpty(nombre)) return false;
            if (nombre.StartsWith("leafsDark", System.StringComparison.Ordinal))
                tinta = TreePalette["leafsDark"];
            else if (nombre.StartsWith("leafsGreen", System.StringComparison.Ordinal))
                tinta = TreePalette["leafsGreen"];
            else if (nombre.StartsWith("woodBarkDark", System.StringComparison.Ordinal))
                tinta = TreePalette["woodBarkDark"];
            else if (nombre.StartsWith("woodBark", System.StringComparison.Ordinal))
                tinta = TreePalette["woodBark"];
            else
                return false;
            return true;
        }

        static readonly Dictionary<Material, Material> treePaints = new Dictionary<Material, Material>();

        static Material RepaintTree(Material source, Color color)
        {
            if (treePaints.TryGetValue(source, out var painted)) return painted;
            painted = new Material(source)
            {
                name = source.name,
                color = color,
                mainTexture = null,
            };
            treePaints[source] = painted;
            return painted;
        }

        static Texture2D facadeTex;

        // ── Fachada colonial de Cajamarca ────────────────────────────────
        // Un azulejo = una casa de 4.2 m de ancho. El alto va normalizado
        // (0 = vereda, 1 = alero), así la misma textura sirve para casas de
        // 5 a 8 m sin repetir pisos: muro encalado, zócalo de piedra, arcos
        // con marco blanco, balcón de madera con balaustres y alero de teja.
        static Color FacadeWash(Color tint)
        {
            return Color.Lerp(Color.white, tint, 0.4f);
        }

        static Texture2D FacadeTex()
        {
            if (facadeTex != null) return facadeTex;
            var photo = Resources.Load<Texture2D>("Calle/Fachadas/colonial")
                ?? Resources.Load<Texture2D>("Calle/fachada_colonial");
            if (photo != null)
            {
                photo.wrapMode = TextureWrapMode.Repeat;
                photo.filterMode = FilterMode.Bilinear;
                facadeTex = photo;
                return photo;
            }

            const int W = 512;
            const int H = 512;
            var tex = new Texture2D(W, H, TextureFormat.RGBA32, true);
            var pix = new Color32[W * H];
            for (int y = 0; y < H; y++)
            {
                float v = y / (float)(H - 1);
                for (int x = 0; x < W; x++)
                {
                    float u = x / (float)(W - 1);
                    float grain = 0.984f + ((x * 29 + y * 47) & 15) * 0.0017f;
                    pix[y * W + x] = FacadePixel(u, v, grain);
                }
            }

            tex.SetPixels32(pix);
            tex.wrapMode = TextureWrapMode.Repeat;
            tex.filterMode = FilterMode.Bilinear;
            tex.Apply(true);
            facadeTex = tex;
            return tex;
        }

        static Color FacadePixel(float u, float v, float grain)
        {
            var trim = new Color(0.975f, 0.955f, 0.925f);
            var c = new Color(0.930f, 0.895f, 0.855f) * grain;

            // Pilastras en los bordes: separan una casa de la siguiente.
            if (u < 0.045f || u > 0.955f) c = trim;
            else if (u < 0.058f || u > 0.942f) c *= 0.88f;

            // Zócalo de piedra.
            if (v < 0.030f) c = new Color(0.46f, 0.40f, 0.34f) * grain;
            else if (v < 0.038f) c = trim * 0.94f;

            // Puerta central: marco blanco y hoja de madera con tablas.
            if (InArch(u, v, 0.410f, 0.590f, 0.032f, 0.315f, 0.070f))
            {
                c = new Color(0.22f, 0.13f, 0.08f);
                if (Mathf.Repeat(u * 24f, 1f) < 0.16f) c *= 0.66f;
            }
            else if (InArch(u, v, 0.390f, 0.610f, 0.032f, 0.332f, 0.070f))
                c = trim;

            // Ventanas bajas: arco, vidrio oscuro y reja.
            c = FacadeWindow(u, v, c, 0.112f, 0.348f, 0.098f, 0.315f, 0.060f, trim);
            c = FacadeWindow(u, v, c, 0.652f, 0.888f, 0.098f, 0.315f, 0.060f, trim);

            // Balcón corrido de madera.
            if (u > 0.075f && u < 0.925f)
            {
                if (v > 0.348f && v < 0.372f) c = new Color(0.30f, 0.19f, 0.12f);
                else if (v >= 0.372f && v < 0.400f) c = new Color(0.42f, 0.27f, 0.16f);
                else if (v >= 0.400f && v < 0.414f) c = new Color(0.34f, 0.21f, 0.13f);
                else if (v >= 0.414f && v < 0.452f && Mathf.Repeat(u * 26f, 1f) < 0.30f)
                    c = new Color(0.40f, 0.25f, 0.15f);
                else if (v >= 0.452f && v < 0.468f) c = new Color(0.47f, 0.30f, 0.18f);
            }

            // Ventanas altas, detrás del balcón.
            c = FacadeWindow(u, v, c, 0.082f, 0.312f, 0.545f, 0.775f, 0.055f, trim);
            c = FacadeWindow(u, v, c, 0.385f, 0.615f, 0.545f, 0.775f, 0.055f, trim);
            c = FacadeWindow(u, v, c, 0.688f, 0.918f, 0.545f, 0.775f, 0.055f, trim);

            // Cornisa blanca y alero de teja.
            if (v >= 0.895f && v < 0.926f) c = trim;
            else if (v >= 0.926f && v < 0.936f) c *= 0.70f;
            else if (v >= 0.936f)
            {
                c = new Color(0.58f, 0.30f, 0.20f);
                if (Mathf.Repeat((v - 0.936f) * 14f, 1f) < 0.20f) c *= 0.72f;
                if (Mathf.Repeat(u * 18f, 1f) < 0.07f) c *= 0.82f;
            }

            return c;
        }

        static bool InArch(float u, float v, float u0, float u1, float v0, float v1, float rise)
        {
            if (u < u0 || u > u1 || v < v0 || v > v1) return false;
            float cx = (u0 + u1) * 0.5f;
            float rx = (u1 - u0) * 0.5f;
            float spring = v1 - rise;
            if (v <= spring) return true;
            float dx = (u - cx) / rx;
            float dy = (v - spring) / rise;
            return dx * dx + dy * dy <= 1f;
        }

        static Color FacadeWindow(float u, float v, Color c, float u0, float u1,
            float v0, float v1, float rise, Color trim)
        {
            if (InArch(u, v, u0, u1, v0, v1, rise))
            {
                float cx = (u0 + u1) * 0.5f;
                c = new Color(0.13f, 0.17f, 0.21f);
                if (v > v0 + (v1 - v0) * 0.58f) c += new Color(0.07f, 0.08f, 0.09f);
                if (Mathf.Abs(u - cx) < 0.008f
                    || Mathf.Abs(v - (v0 + (v1 - v0) * 0.58f)) < 0.006f)
                    c = trim * 0.92f;
            }
            else if (InArch(u, v, u0 - 0.020f, u1 + 0.020f, v0 - 0.012f, v1 + 0.016f, rise))
                c = trim;
            return c;
        }

        void PublishTex(Pen pen, string name, Texture2D tex, Shader shader, Transform parent = null)
        {
            if (pen.Empty || tex == null) return;
            if (shader == null) shader = Shader.Find("AYNI/Calle") ?? Shader.Find("Standard") ?? Shader.Find("Sprites/Default");
            if (shader == null) return;
            var host = new GameObject(name);
            host.transform.SetParent(parent != null ? parent : district, false);
            var filter = host.AddComponent<MeshFilter>();
            filter.sharedMesh = pen.Bake(name);
            var renderer = host.AddComponent<MeshRenderer>();
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            var mat = new Material(shader);
            tex.wrapMode = TextureWrapMode.Repeat;
            mat.mainTexture = tex;
            renderer.sharedMaterial = mat;
        }

        void BuildPlanFountain(Shader shader)
        {
            var pens = new Pen[(int)Skin.Count];
            for (int i = 0; i < pens.Length; i++)
                pens[i] = new Pen();
            Fountain(pens);
            var root = new GameObject("Fuente");
            root.transform.SetParent(district, false);
            root.transform.localPosition = new Vector3(fountainX, 0.14f, fountainZ);
            root.transform.localScale = new Vector3(2.4f, 2.4f, 2.4f);
            Publish(pens[(int)Skin.Piedra], "FuentePiedra", "Calle/piedra", shader, root.transform);
            Publish(pens[(int)Skin.Pintura], "FuenteAgua", null, shader, root.transform);
        }

        void Fountain(Pen[] pens)
        {
            var stone = pens[(int)Skin.Piedra];
            var paint = pens[(int)Skin.Pintura];
            Octagon(stone, Vector3.zero, 2.35f, 0.10f, 0.46f, Losa);
            OctaRing(stone, Vector3.zero, 1.55f, 2.15f, 0.58f, 0.28f, Losa);
            Disc(paint, new Vector3(0f, 0.72f, 0f), 1.45f, Water);
            Box(stone, new Vector3(0f, 1.15f, 0f), new Vector3(0.28f, 0.9f, 0.28f), Quaternion.identity, Losa);
            Box(stone, new Vector3(0f, 1.65f, 0f), new Vector3(0.7f, 0.16f, 0.7f), Quaternion.identity, Losa);
        }

        Vector3 Church(Pen[] pens, Vector3 outward, bool cathedral)
        {
            Vector3 normal = outward.normalized;
            Quaternion rot = Quaternion.LookRotation(normal, Vector3.up);
            float front = StreetOuter / Mathf.Sqrt(2f);
            float depth = cathedral ? 8.2f : 7.4f;
            float width = cathedral ? 10.2f : 8.6f;
            float height = cathedral ? 6.8f : 6f;
            float s = front + depth * 0.5f;
            Vector3 origin = normal * s;

            Box(pens[(int)Skin.Piedra], origin + rot * new Vector3(0f, height * 0.5f, 0.2f),
                new Vector3(width, height, depth), rot, Losa);
            Box(pens[(int)Skin.Teja], origin + rot * new Vector3(0f, height + 0.35f, 0.2f),
                new Vector3(width - 1.4f, 0.5f, depth + 0.4f), rot, Losa);

            float towerH = cathedral ? 12.2f : 9.4f;
            float towerH2 = cathedral ? 11.4f : 7.2f;
            Tower(pens, origin, rot, new Vector3(-(width * 0.5f + 1.35f), 0f, -depth * 0.5f + 1.5f), towerH, cathedral);
            Tower(pens, origin, rot, new Vector3(width * 0.5f + 1.35f, 0f, -depth * 0.5f + 1.5f), towerH2, cathedral);

            Portada(pens[cathedral ? (int)Skin.Catedral : (int)Skin.Francisco], origin, rot,
                new Vector3(0f, 3.15f, -depth * 0.5f - 0.08f), width - 2.8f, 6.1f);

            if (!cathedral)
            {
                Box(pens[(int)Skin.Piedra], origin + rot * new Vector3(width * 0.5f + 4.2f, 2.3f, 1.4f),
                    new Vector3(5.4f, 4.6f, 6.2f), rot, Losa);
                Box(pens[(int)Skin.Teja], origin + rot * new Vector3(width * 0.5f + 4.2f, 4.75f, 1.4f),
                    new Vector3(6f, 0.4f, 6.8f), rot, Losa);
            }

            Box(pens[(int)Skin.Piedra], origin + rot * new Vector3(0f, height + 1.15f, 0.2f),
                new Vector3(0.22f, 1.5f, 0.22f), rot, Losa);
            Box(pens[(int)Skin.Piedra], origin + rot * new Vector3(0f, height + 1.7f, 0.2f),
                new Vector3(1.05f, 0.18f, 0.18f), rot, Losa);

            return origin + rot * new Vector3(0f, 0f, -depth * 0.5f);
        }

        static void Tower(Pen[] pens, Vector3 origin, Quaternion rot, Vector3 local, float height, bool cathedral)
        {
            float w = cathedral ? 2.7f : 2.35f;
            Box(pens[(int)Skin.Piedra], origin + rot * (local + new Vector3(0f, height * 0.5f, 0f)),
                new Vector3(w, height, w + 0.4f), rot, Losa);
            Box(pens[(int)Skin.Teja], origin + rot * (local + new Vector3(0f, height + 0.25f, 0f)),
                new Vector3(w + 0.45f, 0.4f, w + 0.7f), rot, Losa);
            Box(pens[(int)Skin.Pintura], origin + rot * (local + new Vector3(0f, height * 0.72f, -(w + 0.4f) * 0.5f - 0.02f)),
                new Vector3(0.45f, 0.9f, 0.08f), rot, Glass);
        }

        void Houses(Pen[] pens, Vector3 outward)
        {
            Vector3 normal = outward.normalized;
            Vector3 tangent = Vector3.Cross(Vector3.up, normal);
            Quaternion rot = Quaternion.LookRotation(normal, Vector3.up);
            float front = StreetOuter / Mathf.Sqrt(2f);
            float depth = 6.2f;
            float s = front + depth * 0.5f;
            for (int i = -1; i <= 1; i += 2)
            {
                Vector3 origin = normal * s + tangent * (i * 4.6f);
                House(pens, origin, rot, depth);
            }
        }

        static void House(Pen[] pens, Vector3 origin, Quaternion rot, float depth)
        {
            Box(pens[(int)Skin.Adobe], origin + rot * new Vector3(0f, 2.1f, 0f),
                new Vector3(6.2f, 4.2f, depth), rot, Losa);
            Box(pens[(int)Skin.Teja], origin + rot * new Vector3(0f, 4.45f, 0f),
                new Vector3(7.1f, 0.48f, depth + 0.9f), rot, Losa);
            Box(pens[(int)Skin.Pintura], origin + rot * new Vector3(0f, 1.15f, -depth * 0.5f - 0.04f),
                new Vector3(1.15f, 2.2f, 0.12f), rot, Door);
            Box(pens[(int)Skin.Pintura], origin + rot * new Vector3(-1.55f, 2.7f, -depth * 0.5f - 0.04f),
                new Vector3(0.75f, 0.85f, 0.1f), rot, Glass);
            Box(pens[(int)Skin.Pintura], origin + rot * new Vector3(1.55f, 2.7f, -depth * 0.5f - 0.04f),
                new Vector3(0.75f, 0.85f, 0.1f), rot, Glass);
        }

        void Belen(Pen[] pens)
        {
            var origin = new Vector3(0f, 0f, -43f);
            var rot = Quaternion.LookRotation(Vector3.back, Vector3.up);
            Box(pens[(int)Skin.Piedra], origin + rot * new Vector3(0f, 3f, 0f),
                new Vector3(8f, 6f, 7f), rot, Losa);
            Box(pens[(int)Skin.Teja], origin + rot * new Vector3(0f, 6.25f, 0f),
                new Vector3(7.2f, 0.45f, 7.6f), rot, Losa);
            Tower(pens, origin, rot, new Vector3(-5.2f, 0f, -1.8f), 10f, false);
            Tower(pens, origin, rot, new Vector3(5.2f, 0f, -1.8f), 4.2f, false);
            Portada(pens[(int)Skin.Francisco], origin, rot, new Vector3(0f, 2.8f, -3.55f), 5.4f, 5.2f);
        }

        void Candles(Pen[] pens, Vector3 outward)
        {
            Vector3 normal = outward.normalized;
            Vector3 tangent = Vector3.Cross(Vector3.up, normal);
            float s = (PlazaR - 1.6f) / Mathf.Sqrt(2f);
            var paint = pens[(int)Skin.Pintura];
            for (int i = -3; i <= 3; i++)
            {
                if (i == 0) continue;
                Vector3 p = normal * s + tangent * (i * 1.35f);
                Box(paint, p + Vector3.up * 0.45f, new Vector3(0.16f, 0.7f, 0.16f), Quaternion.identity, Door);
                Box(paint, p + Vector3.up * 0.9f, new Vector3(0.28f, 0.24f, 0.28f), Quaternion.identity, i % 2 == 0 ? Candle : Flower);
            }

            for (int i = 0; i < 6; i++)
            {
                float a = i / 6f * Mathf.PI * 2f;
                var p = new Vector3(Mathf.Cos(a) * 6.2f, 0.28f, Mathf.Sin(a) * 6.2f);
                Box(paint, p, new Vector3(0.4f, 0.36f, 0.4f), Quaternion.identity, i % 2 == 0 ? Flower : Candle);
            }
        }

        static void Diamond(Pen pen, float radius, float y, Color color)
        {
            var n = new Vector3(0f, y, radius);
            var e = new Vector3(radius, y, 0f);
            var s = new Vector3(0f, y, -radius);
            var w = new Vector3(-radius, y, 0f);
            pen.Quad(n, e, s, w, Vector3.up, color, false);
        }

        static void Ring(Pen pen, float inner, float outer, float y, Color color)
        {
            Vector3 Inn(int k) => Corner(inner, y, k);
            Vector3 Out(int k) => Corner(outer, y, k);
            for (int k = 0; k < 4; k++)
            {
                int n = (k + 1) % 4;
                pen.Quad(Inn(k), Inn(n), Out(n), Out(k), Vector3.up, color, false);
            }
        }

        static Vector3 Corner(float radius, float y, int k)
        {
            if (k == 0) return new Vector3(0f, y, radius);
            if (k == 1) return new Vector3(radius, y, 0f);
            if (k == 2) return new Vector3(0f, y, -radius);
            return new Vector3(-radius, y, 0f);
        }

        static void Octagon(Pen pen, Vector3 center, float radius, float y0, float height, Color color)
        {
            int n = 8;
            var top = new Vector3[n];
            var bot = new Vector3[n];
            for (int i = 0; i < n; i++)
            {
                float a = i / (float)n * Mathf.PI * 2f;
                var xz = new Vector3(Mathf.Cos(a) * radius, 0f, Mathf.Sin(a) * radius);
                top[i] = center + xz + Vector3.up * (y0 + height);
                bot[i] = center + xz + Vector3.up * y0;
            }

            float yt = y0 + height;
            var cap = center + Vector3.up * yt;
            for (int i = 0; i < n; i++)
                pen.Tri(cap, top[i], top[(i + 1) % n], Vector3.up, color);
            for (int i = 0; i < n; i++)
            {
                int j = (i + 1) % n;
                Vector3 mid = (top[i] + top[j] + bot[i] + bot[j]) * 0.25f - center;
                mid.y = 0f;
                pen.Quad(bot[i], bot[j], top[j], top[i], mid.normalized, color, false);
            }
        }

        static void OctaRing(Pen pen, Vector3 center, float inner, float outer, float y, float height, Color color)
        {
            int n = 8;
            for (int i = 0; i < n; i++)
            {
                float a0 = i / (float)n * Mathf.PI * 2f;
                float a1 = (i + 1) / (float)n * Mathf.PI * 2f;
                Vector3 i0 = center + new Vector3(Mathf.Cos(a0) * inner, y + height, Mathf.Sin(a0) * inner);
                Vector3 i1 = center + new Vector3(Mathf.Cos(a1) * inner, y + height, Mathf.Sin(a1) * inner);
                Vector3 o0 = center + new Vector3(Mathf.Cos(a0) * outer, y + height, Mathf.Sin(a0) * outer);
                Vector3 o1 = center + new Vector3(Mathf.Cos(a1) * outer, y + height, Mathf.Sin(a1) * outer);
                pen.Quad(i0, i1, o1, o0, Vector3.up, color, false);
                Vector3 b0 = o0 - Vector3.up * height;
                Vector3 b1 = o1 - Vector3.up * height;
                Vector3 outN = ((o0 + o1) * 0.5f - center);
                outN.y = 0f;
                pen.Quad(b0, b1, o1, o0, outN.normalized, color, false);
            }
        }

        static void Disc(Pen pen, Vector3 center, float radius, Color color)
        {
            int n = 10;
            var prev = center + new Vector3(radius, 0f, 0f);
            for (int i = 1; i <= n; i++)
            {
                float a = i / (float)n * Mathf.PI * 2f;
                var p = center + new Vector3(Mathf.Cos(a) * radius, 0f, Mathf.Sin(a) * radius);
                pen.Tri(center, prev, p, Vector3.up, color);
                prev = p;
            }
        }

        static void Hill(Pen pen, Vector3 center, float radius, float height, Color color)
        {
            int n = 12;
            var top = center + Vector3.up * height;
            for (int i = 0; i < n; i++)
            {
                float a0 = i / (float)n * Mathf.PI * 2f;
                float a1 = (i + 1) / (float)n * Mathf.PI * 2f;
                var p0 = center + new Vector3(Mathf.Cos(a0) * radius, 0f, Mathf.Sin(a0) * radius);
                var p1 = center + new Vector3(Mathf.Cos(a1) * radius, 0f, Mathf.Sin(a1) * radius);
                Vector3 mid = (p0 + p1) * 0.5f - center;
                Vector3 normal = Vector3.Cross(p1 - top, p0 - top);
                if (Vector3.Dot(normal, mid) < 0f) normal = -normal;
                pen.Tri(top, p0, p1, normal.normalized, color);
            }
        }

        static void Portada(Pen pen, Vector3 origin, Quaternion rot, Vector3 localCenter, float width, float height)
        {
            Vector3 right = rot * Vector3.right;
            Vector3 c = origin + rot * localCenter;
            float hx = width * 0.5f;
            float hy = height * 0.5f;
            Vector3 bl = c - right * hx - Vector3.up * hy;
            Vector3 br = c + right * hx - Vector3.up * hy;
            Vector3 tr = c + right * hx + Vector3.up * hy;
            Vector3 tl = c - right * hx + Vector3.up * hy;
            pen.Quad(bl, br, tr, tl, rot * Vector3.back, Color.white, true);
        }

        static void Box(Pen pen, Vector3 center, Vector3 size, Quaternion rot, Color color)
        {
            Vector3 h = size * 0.5f;
            Vector3 P(float x, float y, float z) => center + rot * new Vector3(x, y, z);
            Vector3 N(Vector3 normal) => rot * normal;
            pen.Quad(P(-h.x, h.y, -h.z), P(-h.x, h.y, h.z), P(h.x, h.y, h.z), P(h.x, h.y, -h.z), N(Vector3.up), color, false);
            pen.Quad(P(-h.x, -h.y, h.z), P(-h.x, -h.y, -h.z), P(h.x, -h.y, -h.z), P(h.x, -h.y, h.z), N(Vector3.down), color, false);
            pen.Quad(P(-h.x, -h.y, -h.z), P(-h.x, h.y, -h.z), P(h.x, h.y, -h.z), P(h.x, -h.y, -h.z), N(Vector3.back), color, false);
            pen.Quad(P(h.x, -h.y, h.z), P(h.x, h.y, h.z), P(-h.x, h.y, h.z), P(-h.x, -h.y, h.z), N(Vector3.forward), color, false);
            pen.Quad(P(-h.x, -h.y, h.z), P(-h.x, h.y, h.z), P(-h.x, h.y, -h.z), P(-h.x, -h.y, -h.z), N(Vector3.left), color, false);
            pen.Quad(P(h.x, -h.y, -h.z), P(h.x, h.y, -h.z), P(h.x, h.y, h.z), P(h.x, -h.y, h.z), N(Vector3.right), color, false);
        }

        void PlacePlan(Shader shader)
        {
            var prefab = Resources.Load<GameObject>("Calle/PlanoCajamarca");
            if (prefab == null) return;
            plan = Instantiate(prefab, district);
            plan.name = "Plano";
            plan.transform.localPosition = new Vector3(0f, 0.03f, 0f);
            FitPlan();
            var renderers = plan.GetComponentsInChildren<MeshRenderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                var renderer = renderers[i];
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = false;
                if (shader == null) continue;
                var mat = new Material(shader);
                string texName = PlanTexture(renderer.gameObject.name);
                if (!string.IsNullOrEmpty(texName))
                {
                    var tex = CalleTex(texName);
                    bool wall = texName == "Calle/muro_adobe";
                    var face = wall ? FacadeTex() : tex;
                    if (face != null)
                    {
                        // El atlas de calle es un corte transversal que se repite
                        // a lo largo de la vía: en espejo no se nota la costura.
                        face.wrapMode = texName == "Calle/piso_empedrado"
                            ? TextureWrapMode.Mirror
                            : texName.IndexOf("portada", System.StringComparison.Ordinal) >= 0
                                ? TextureWrapMode.Clamp
                                : TextureWrapMode.Repeat;
                        mat.mainTexture = face;
                    }

                    if (wall) DressWallUvs(renderer);
                }
                else
                    mat.mainTexture = Swatch(PlanColor(renderer.gameObject.name));

                renderer.sharedMaterial = mat;
            }
        }

        void DressWallUvs(Renderer renderer)
        {
            var filter = renderer.GetComponent<MeshFilter>();
            if (filter == null || filter.sharedMesh == null) return;
            var mesh = filter.mesh;
            var srcUv = mesh.uv;
            if (srcUv == null || srcUv.Length != mesh.vertexCount) return;
            var srcVerts = mesh.vertices;
            var srcNormals = mesh.normals;
            var tris = mesh.triangles;
            if (tris.Length == 0) return;

            // El muro se reconstruye sin vértices compartidos: el tinte va por
            // casa y un vértice entre dos casas dejaría un degradado de color.
            var verts = new Vector3[tris.Length];
            var normals = new Vector3[tris.Length];
            var uv = new Vector2[tris.Length];
            var cols = new Color[tris.Length];
            var order = new int[tris.Length];
            for (int t = 0; t + 2 < tris.Length; t += 3)
            {
                // u: una casa cada 4.2 m. v: 0 en la vereda, 1 en el alero
                // (el generador ya lo deja normalizado por casa).
                float u = (srcUv[tris[t]].x + srcUv[tris[t + 1]].x + srcUv[tris[t + 2]].x)
                          / (3f * 4.2f);
                var tint = AdobeTint[Mathf.Abs(Mathf.FloorToInt(u)) % AdobeTint.Length];
                var wash = FacadeWash(tint);
                for (int k = 0; k < 3; k++)
                {
                    int src = tris[t + k];
                    int dst = t + k;
                    verts[dst] = srcVerts[src];
                    normals[dst] = srcNormals.Length == srcVerts.Length ? srcNormals[src] : Vector3.up;
                    uv[dst] = new Vector2(srcUv[src].x / 4.2f, srcUv[src].y);
                    cols[dst] = wash;
                    order[dst] = dst;
                }
            }

            mesh.Clear();
            mesh.indexFormat = IndexFormat.UInt32;
            mesh.vertices = verts;
            mesh.normals = normals;
            mesh.uv = uv;
            mesh.colors = cols;
            mesh.triangles = order;
            mesh.RecalculateBounds();
        }

        void FitPlan()
        {
            Transform marca = FindPlan("PlanoMarca");
            if (marca == null) return;
            Vector3 span = marca.position - plan.transform.position;
            float reach = Mathf.Max(Mathf.Abs(span.x), Mathf.Abs(span.z));
            if (reach < 0.01f) reach = Mathf.Abs(span.y);
            if (reach > 0.01f)
                plan.transform.localScale *= 62f / reach;
            marca.gameObject.SetActive(false);
        }

        void BindPlanDoor()
        {
            Transform puerta = FindPlan("PlanoPuerta");
            if (puerta == null) return;
            Vector3 local = district.InverseTransformPoint(puerta.position);
            sanFranciscoDoor = new Vector3(local.x, 0f, local.z);
            hasDoor = true;
            puerta.gameObject.SetActive(false);
        }

        void BuildStreetLabels()
        {
            if (plan == null || streetLabels != null) return;
            var asset = Resources.Load<TextAsset>("Calle/calles");
            if (asset == null) return;
            StreetLabelSet set = null;
            try
            {
                set = JsonUtility.FromJson<StreetLabelSet>(asset.text);
            }
            catch (System.Exception)
            {
                set = null;
            }

            if (set == null || set.streets == null || set.streets.Length == 0) return;
            var root = new GameObject("NombresCalles");
            root.transform.SetParent(plan.transform, false);
            root.transform.localPosition = new Vector3(0f, 0.22f, 0f);
            streetLabels = root.transform;
            var ink = new Color(0.97f, 0.94f, 0.86f, 0.96f);
            for (int i = 0; i < set.streets.Length; i++)
                AddStreetLabel(set.streets[i], ink);
            root.SetActive(vista == Vista.Planta);
        }

        void AddStreetLabel(StreetLabel data, Color ink)
        {
            if (data == null || string.IsNullOrEmpty(data.name)) return;
            var go = new GameObject("Calle " + data.name);
            go.transform.SetParent(streetLabels, false);
            go.transform.localPosition = new Vector3(
                data.x, CajamarcaAltura.Sample(data.x, data.z) + 0.22f, data.z);

            // Las calles verticales se rotulan siempre de abajo hacia arriba.
            float yaw = -data.angle;
            if (Mathf.Abs(Mathf.Cos(yaw * Mathf.Deg2Rad)) < 0.2f
                && Mathf.Sin(yaw * Mathf.Deg2Rad) > 0f)
                yaw += 180f;
            go.transform.localRotation = Quaternion.Euler(90f, yaw, 0f);
            var lectura = go.AddComponent<CalleLegible>();
            lectura.Yaw = yaw;

            var text = go.AddComponent<TextMesh>();
            text.text = NombreVisible(data.name);
            text.font = Storybook.DisplayFont;
            text.fontSize = 64;
            text.characterSize = 0.1f;
            text.anchor = TextAnchor.MiddleCenter;
            text.alignment = TextAlignment.Center;
            text.fontStyle = FontStyle.Bold;
            text.richText = false;
            text.color = ink;

            var mesh = go.GetComponent<MeshRenderer>();
            if (mesh != null)
            {
                if (text.font != null) mesh.sharedMaterial = text.font.material;
                mesh.shadowCastingMode = ShadowCastingMode.Off;
                mesh.receiveShadows = false;
            }

            // El "size" del JSON es la altura de letra buscada en metros del plano.
            var fit = go.AddComponent<StreetLabelFit>();
            fit.Target = data.size;
            var marca = go.AddComponent<CalleEditable>();
            marca.Origen = data.name;
        }

        void BakeWalk()
        {
            walk.Clear();
            walkKind.Clear();
            walkGrid.Clear();
            walkSpan = false;
            walkMinX = float.MaxValue;
            walkMaxX = float.MinValue;
            walkMinZ = float.MaxValue;
            walkMaxZ = float.MinValue;
            var filters = plan.GetComponentsInChildren<MeshFilter>(true);
            for (int i = 0; i < filters.Length; i++)
            {
                string name = filters[i].gameObject.name;
                bool calle = name.IndexOf("Calles", System.StringComparison.Ordinal) >= 0;
                bool banda = name.IndexOf("Banda", System.StringComparison.Ordinal) >= 0;
                bool plaza = name.IndexOf("Plazas", System.StringComparison.Ordinal) >= 0;
                if (!calle && !banda && !plaza) continue;
                byte kind = calle ? KindCalle : (banda ? KindBanda : KindPlaza);
                var mesh = filters[i].sharedMesh;
                if (mesh == null || !mesh.isReadable) continue;
                var verts = mesh.vertices;
                var tris = mesh.triangles;
                var space = filters[i].transform;
                for (int t = 0; t < tris.Length; t += 3)
                {
                    Vector3 a = district.InverseTransformPoint(space.TransformPoint(verts[tris[t]]));
                    Vector3 b = district.InverseTransformPoint(space.TransformPoint(verts[tris[t + 1]]));
                    Vector3 c = district.InverseTransformPoint(space.TransformPoint(verts[tris[t + 2]]));
                    int index = walk.Count / 3;
                    var pa = new Vector2(a.x, a.z);
                    var pb = new Vector2(b.x, b.z);
                    var pc = new Vector2(c.x, c.z);
                    walk.Add(pa);
                    walk.Add(pb);
                    walk.Add(pc);
                    walkKind.Add(kind);
                    CoverWalk(index, pa, pb, pc);
                    NoteWalk(pa);
                    NoteWalk(pb);
                    NoteWalk(pc);
                }
            }

            walkReady = walk.Count >= 3;
            walkSpan = walkReady && walkMaxX > walkMinX + 40f && walkMaxZ > walkMinZ + 40f;
        }

        void NoteWalk(Vector2 p)
        {
            if (p.x < walkMinX) walkMinX = p.x;
            if (p.x > walkMaxX) walkMaxX = p.x;
            if (p.y < walkMinZ) walkMinZ = p.y;
            if (p.y > walkMaxZ) walkMaxZ = p.y;
        }

        void ReadPlan()
        {
            if (vista != Vista.Planta || plan == null || cam == null) return;
            float wheel = Input.mouseScrollDelta.y;
            if (Input.GetKey(KeyCode.Equals) || Input.GetKey(KeyCode.KeypadPlus)) wheel += 0.8f * Time.deltaTime * 8f;
            if (Input.GetKey(KeyCode.Minus) || Input.GetKey(KeyCode.KeypadMinus)) wheel -= 0.8f * Time.deltaTime * 8f;
            if (wheel > 0.01f) planSize = Mathf.Max(10f, planSize * (1f - 0.18f * Mathf.Min(wheel, 3f)));
            if (wheel < -0.01f) planSize = Mathf.Min(1700f, planSize / (1f - 0.18f * Mathf.Min(-wheel, 3f)));

            bool overUi = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
            if (Input.GetMouseButtonDown(1) && !overUi)
            {
                var hero = Hero();
                if (!planDetached && hero != null)
                    planLook = new Vector2(hero.Local.x, hero.Local.z);
                planDetached = true;
                planDrag = true;
                planCursor = Input.mousePosition;
            }

            if (!Input.GetMouseButton(1)) planDrag = false;
            if (planDrag)
            {
                Vector3 now = Input.mousePosition;
                Vector3 delta = now - planCursor;
                planCursor = now;
                Vector3 right = cam.transform.right;
                Vector3 up = cam.transform.up;
                right.y = 0f;
                up.y = 0f;
                if (right.sqrMagnitude > 0.001f && up.sqrMagnitude > 0.001f)
                {
                    right.Normalize();
                    up.Normalize();
                    float units = (planSize * 2f) / Mathf.Max(1f, Screen.height);
                    Vector3 move = (-delta.x * right - delta.y * up) * units;
                    if (district != null) move = district.InverseTransformDirection(move);
                    planLook.x += move.x;
                    planLook.y += move.z;
                }
            }

        }

        void ZoomPlan(float mul)
        {
            if (plan != null)
            {
                if (vista != Vista.Planta) SetVista(Vista.Planta);
                planSize = Mathf.Clamp(planSize * mul, 10f, 1700f);
            }
            else
                camara.Distance = Mathf.Clamp(camara.Distance * mul, camara.MinDistance, camara.MaxDistance);
            snapped = false;
        }

        void ArmarNombre()
        {
            if (hud == null || nombrePanel != null) return;
            var fondo = Storybook.Plate(hud.transform, "EditarNombre",
                new Vector2(0.24f, 0.17f), new Vector2(0.76f, 0.31f),
                new Color(0.96f, 0.93f, 0.84f, 0.96f));
            fondo.raycastTarget = true;
            nombrePanel = fondo.gameObject;

            var aviso = AddText(nombrePanel.transform, "Aviso", new Vector2(0.04f, 0.62f), new Vector2(0.96f, 0.96f));
            Storybook.DressText(aviso, LeyDelMonte.EditarNombre,
                Storybook.BodyFont, 16, new Color(0.28f, 0.16f, 0.08f), FontStyle.Normal);

            var campo = new GameObject("Campo");
            campo.transform.SetParent(nombrePanel.transform, false);
            var rect = campo.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.04f, 0.12f);
            rect.anchorMax = new Vector2(0.96f, 0.58f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            var imagen = campo.AddComponent<Image>();
            imagen.color = new Color(1f, 0.99f, 0.96f, 1f);

            var textoGo = new GameObject("Texto");
            textoGo.transform.SetParent(campo.transform, false);
            var textoRect = textoGo.AddComponent<RectTransform>();
            textoRect.anchorMin = Vector2.zero;
            textoRect.anchorMax = Vector2.one;
            textoRect.offsetMin = new Vector2(10f, 2f);
            textoRect.offsetMax = new Vector2(-10f, -2f);
            var texto = textoGo.AddComponent<Text>();
            Storybook.DressText(texto, "", Storybook.BodyFont, 22, new Color(0.16f, 0.08f, 0.04f), FontStyle.Normal);
            texto.alignment = TextAnchor.MiddleLeft;
            texto.raycastTarget = true;

            nombreCampo = campo.AddComponent<InputField>();
            nombreCampo.textComponent = texto;
            nombreCampo.lineType = InputField.LineType.SingleLine;
            nombreCampo.characterLimit = 42;
            nombrePanel.SetActive(false);
        }

        void TocarCiudad()
        {
            if (cam == null || district == null || editandoNombre) return;
            if (TocarNombre(out string origen, out string visible))
            {
                AbrirNombre(origen, visible);
                return;
            }

            float x = 0f;
            float z = 0f;
            if (editandoMapa)
            {
                var bajo = PiezaBajoCursor();
                if (bajo != null)
                {
                    if (herramienta == Pieza.Quitar) QuitarPieza(bajo);
                    else ElegirPieza(bajo);
                    return;
                }

                if (!SueloBajoElCursor(out x, out z)) return;
                if (piezaElegida != null)
                {
                    MoverPieza(piezaElegida, x, z);
                    return;
                }

                if (herramienta == Pieza.Quitar)
                {
                    Note(LeyDelMonte.PiezaNada);
                    return;
                }

                PonerNueva(herramienta, x, z);
                return;
            }

            if (!SueloBajoElCursor(out x, out z)) return;

            if (CalleBajo(x, z, out EjeCalle calle))
            {
                if (vista != Vista.Planta && !CercaDelHeroe(x, z)) return;
                CiclarCara(calle.Nombre);
                return;
            }

            if (vista != Vista.Planta || EnJardin(x, z) || OnPlaza(x, z)) return;
            PonerNueva(Pieza.Casa, x, z);
        }

        bool TocarNombre(out string origen, out string visible)
        {
            origen = null;
            visible = null;
            if (cam == null) return false;
            Vector2 mouse = Input.mousePosition;
            float mejor = float.PositiveInfinity;
            bool hay = false;

            if (vista == Vista.Planta && streetLabels != null && streetLabels.gameObject.activeInHierarchy)
            {
                var textos = streetLabels.GetComponentsInChildren<TextMesh>(false);
                for (int i = 0; i < textos.Length; i++)
                {
                    if (!Cubre(textos[i].transform, mouse, ref mejor)) continue;
                    origen = OrigenDe(textos[i].transform, "Calle ", textos[i].text);
                    visible = textos[i].text;
                    hay = true;
                }
            }

            Transform postes = plan != null ? plan.transform.Find("Postes") : null;
            if (postes != null)
            {
                for (int i = 0; i < postes.childCount; i++)
                {
                    Transform poste = postes.GetChild(i);
                    Transform letrero = poste.Find("Letrero");
                    Transform marca = letrero != null ? letrero : poste;
                    if (!Cubre(marca, mouse, ref mejor)) continue;
                    var texto = poste.GetComponentInChildren<TextMesh>();
                    origen = OrigenDe(poste, "Poste ", texto != null ? texto.text : poste.name);
                    visible = texto != null ? texto.text : origen;
                    hay = true;
                }
            }

            return hay && !string.IsNullOrEmpty(origen);
        }

        bool Cubre(Transform marca, Vector2 mouse, ref float mejor)
        {
            if (marca == null || cam == null) return false;
            var render = marca.GetComponent<Renderer>();
            if (render == null) render = marca.GetComponentInChildren<Renderer>();
            Vector3 centro = render != null ? render.bounds.center : marca.position;
            Vector3 pantalla = cam.WorldToScreenPoint(centro);
            if (pantalla.z <= 0f) return false;
            float alcance = 46f;
            if (render != null)
            {
                Vector3 e = render.bounds.extents;
                float dx = Separacion(centro, new Vector3(e.x, 0f, 0f), pantalla);
                float dy = Separacion(centro, new Vector3(0f, e.y, 0f), pantalla);
                float dz = Separacion(centro, new Vector3(0f, 0f, e.z), pantalla);
                alcance = Mathf.Max(dx, Mathf.Max(dy, dz)) + 16f;
                if (alcance < 36f) alcance = 36f;
                if (alcance > 260f) alcance = 260f;
            }

            float dist = Vector2.Distance(mouse, new Vector2(pantalla.x, pantalla.y));
            if (dist > alcance || dist >= mejor) return false;
            mejor = dist;
            return true;
        }

        float Separacion(Vector3 centro, Vector3 eje, Vector3 centroPantalla)
        {
            Vector3 punta = cam.WorldToScreenPoint(centro + eje);
            if (punta.z <= 0f) return 0f;
            return Vector2.Distance(
                new Vector2(centroPantalla.x, centroPantalla.y),
                new Vector2(punta.x, punta.y));
        }

        string OrigenDe(Transform marca, string prefijo, string texto)
        {
            var dato = marca.GetComponent<CalleEditable>();
            if (dato != null && !string.IsNullOrEmpty(dato.Origen)) return dato.Origen;
            string nombre = marca.name;
            string origen = !string.IsNullOrEmpty(nombre) && nombre.StartsWith(prefijo)
                ? nombre.Substring(prefijo.Length)
                : texto;
            if (string.IsNullOrEmpty(origen)) origen = texto;
            dato = marca.gameObject.AddComponent<CalleEditable>();
            dato.Origen = origen;
            return origen;
        }

        void AbrirNombre(string origen, string visible)
        {
            if (nombrePanel == null || nombreCampo == null || string.IsNullOrEmpty(origen)) return;
            nombreOrigen = origen;
            editandoNombre = true;
            nombrePanel.SetActive(true);
            nombrePanel.transform.SetAsLastSibling();
            nombreCampo.text = string.IsNullOrEmpty(visible) ? origen : visible;
            nombreCampo.ActivateInputField();
            nombreCampo.Select();
        }

        void CerrarNombre(bool guardar)
        {
            string escrito = nombreCampo != null ? nombreCampo.text.Trim() : "";
            editandoNombre = false;
            if (nombreCampo != null) nombreCampo.DeactivateInputField();
            if (nombrePanel != null) nombrePanel.SetActive(false);
            if (!guardar || string.IsNullOrEmpty(escrito) || string.IsNullOrEmpty(nombreOrigen)) return;
            if (escrito == NombreVisible(nombreOrigen)) return;
            NombresGuardados[nombreOrigen] = escrito;
            PonerNombre(nombreOrigen, escrito);
            Note(escrito);
        }

        void PonerNombre(string origen, string visible)
        {
            var etiqueta = BuscarEtiqueta(origen);
            if (etiqueta != null) etiqueta.text = visible;
            var poste = BuscarPoste(origen);
            if (poste == null) return;
            var textos = poste.GetComponentsInChildren<TextMesh>(true);
            for (int i = 0; i < textos.Length; i++) textos[i].text = visible;
            Transform tabla = poste.Find("Letrero/Tabla");
            if (tabla == null) return;
            float ancho = Mathf.Clamp(0.16f * visible.Length, 1.8f, 5.6f);
            Vector3 escala = tabla.localScale;
            escala.x = ancho;
            tabla.localScale = escala;
        }

        TextMesh BuscarEtiqueta(string origen)
        {
            if (streetLabels == null || string.IsNullOrEmpty(origen)) return null;
            Transform hijo = streetLabels.Find("Calle " + origen);
            return hijo != null ? hijo.GetComponent<TextMesh>() : null;
        }

        Transform BuscarPoste(string origen)
        {
            if (plan == null || string.IsNullOrEmpty(origen)) return null;
            Transform postes = plan.transform.Find("Postes");
            return postes != null ? postes.Find("Poste " + origen) : null;
        }

        string NombreVisible(string origen)
        {
            if (!string.IsNullOrEmpty(origen)
                && NombresGuardados.TryGetValue(origen, out string guardado)
                && !string.IsNullOrEmpty(guardado))
                return guardado;
            return origen;
        }

        bool SueloBajoElCursor(out float x, out float z)
        {
            x = 0f;
            z = 0f;
            float y = district.position.y;
            if (vista != Vista.Planta)
            {
                var hero = Hero();
                if (hero != null && hero.Body != null) y = hero.Body.position.y;
            }

            Ray ray = cam.ScreenPointToRay(Input.mousePosition);
            var plane = new Plane(Vector3.up, new Vector3(0f, y, 0f));
            if (!plane.Raycast(ray, out float dist)) return false;
            Vector3 local = district.InverseTransformPoint(ray.GetPoint(dist));
            x = local.x;
            z = local.z;
            return true;
        }

        bool CalleBajo(float x, float z, out EjeCalle eje, float ancho = 8.5f)
        {
            eje = default;
            if (EnJardin(x, z)) return false;
            var ejes = Ejes();
            float mejor = ancho;
            bool hay = false;
            for (int i = 0; i < ejes.Count; i++)
            {
                EjeCalle e = ejes[i];
                float dx = x - e.X;
                float dz = z - e.Z;
                float along = dx * e.Tx + dz * e.Tz;
                if (along < -4f || along > e.Largo + 4f) continue;
                float lx = dx - e.Tx * along;
                float lz = dz - e.Tz * along;
                float lateral = Mathf.Sqrt(lx * lx + lz * lz);
                if (lateral >= mejor) continue;
                mejor = lateral;
                eje = e;
                hay = true;
            }

            return hay;
        }

        bool CercaDelHeroe(float x, float z)
        {
            var hero = Hero();
            if (hero == null) return false;
            float dx = hero.Local.x - x;
            float dz = hero.Local.z - z;
            return dx * dx + dz * dz < 26f * 26f;
        }

        bool EnJardin(float x, float z)
        {
            if (!plazaFound || plazaHalfU < 8f || plazaHalfV < 8f) return false;
            Vector2 local = PlazaLocal(x, z);
            return Mathf.Abs(local.x) < plazaHalfU - 2.5f && Mathf.Abs(local.y) < plazaHalfV - 2.5f;
        }

        void CiclarCara(string origen)
        {
            int indice = 0;
            if (CarasGuardadas.TryGetValue(origen, out string actual))
            {
                int puesto = System.Array.IndexOf(CaraRecurso, actual);
                indice = puesto < 0 ? 0 : (puesto + 1) % CaraRecurso.Length;
            }

            string recurso = CaraRecurso[indice];
            CarasGuardadas[origen] = recurso;
            PintarCara(origen, recurso);
            Note(NombreVisible(origen) + " · " + CaraNombre[indice]);
        }

        void RestaurarCaras()
        {
            if (CarasGuardadas.Count == 0) return;
            foreach (var par in CarasGuardadas)
                PintarCara(par.Key, par.Value);
        }

        void PintarCarasDeLista(CaraSet set)
        {
            if (set == null || set.calles == null) return;
            string defecto = string.IsNullOrEmpty(set.defecto) ? "Calle/fachada_colonial" : set.defecto;
            for (int i = 0; i < set.calles.Length; i++)
            {
                var fila = set.calles[i];
                if (fila == null || string.IsNullOrEmpty(fila.nombre) || string.IsNullOrEmpty(fila.cara)) continue;
                if (string.Equals(fila.cara, defecto, System.StringComparison.OrdinalIgnoreCase)) continue;
                if (CarasGuardadas.ContainsKey(fila.nombre)) continue;
                CarasGuardadas[fila.nombre] = fila.cara;
                PintarCara(fila.nombre, fila.cara);
            }
        }

        void PintarCara(string origen, string recurso)
        {
            if (string.IsNullOrEmpty(origen) || string.IsNullOrEmpty(recurso)) return;
            Transform parent = RuntimeParent;
            if (parent == null) return;
            var tex = CalleTex(recurso);
            if (tex == null) return;
            tex.wrapMode = TextureWrapMode.Repeat;

            var pen = new Pen();
            bool trazo = TieneTrazo(origen);
            var ejes = Ejes();
            for (int i = 0; i < ejes.Count; i++)
            {
                if (!MismaCalle(ejes[i].Nombre, origen)) continue;
                if (trazo && ejes[i].Largo >= 219f) continue;
                AgregarFachada(pen, ejes[i]);
            }

            Transform viejo = parent.Find("Fachada " + origen);
            if (viejo != null) Destroy(viejo.gameObject);
            if (pen.Empty) return;
            var shader = Shader.Find("AYNI/Calle") ?? Shader.Find("Standard") ?? Shader.Find("Sprites/Default");
            Publish(pen, "Fachada " + origen, recurso, shader, parent);
        }

        bool TieneTrazo(string origen)
        {
            var ejes = Ejes();
            for (int i = 0; i < ejes.Count; i++)
            {
                if (MismaCalle(ejes[i].Nombre, origen) && ejes[i].Largo < 219f) return true;
            }

            return false;
        }

        void AgregarFachada(Pen pen, EjeCalle e)
        {
            float tx = e.Tx;
            float tz = e.Tz;
            float mag = Mathf.Sqrt(tx * tx + tz * tz);
            if (mag < 0.001f) return;
            tx /= mag;
            tz /= mag;
            float nx = -tz;
            float nz = tx;
            const float paso = 8f;
            int n = Mathf.Max(1, Mathf.CeilToInt(e.Largo / paso));
            for (int i = 0; i < n; i++)
            {
                float a0 = i * paso;
                float a1 = Mathf.Min(e.Largo, a0 + paso);
                float mid = (a0 + a1) * 0.5f;
                if (EnJardin(e.X + tx * mid, e.Z + tz * mid)) continue;
                AgregarLado(pen, e.X, e.Z, tx, tz, a0, a1, nx, nz);
                AgregarLado(pen, e.X, e.Z, tx, tz, a0, a1, -nx, -nz);
            }
        }

        void AgregarLado(Pen pen, float ox, float oz, float tx, float tz, float a0, float a1, float nx, float nz)
        {
            float mid = (a0 + a1) * 0.5f;
            float cx = ox + tx * mid;
            float cz = oz + tz * mid;
            if (!Orilla(cx, cz, nx, nz, out float dist)) return;
            float seat = Mathf.Max(3.4f, dist - 0.25f);
            float x0 = ox + tx * a0 + nx * seat;
            float z0 = oz + tz * a0 + nz * seat;
            float x1 = ox + tx * a1 + nx * seat;
            float z1 = oz + tz * a1 + nz * seat;
            if (JuntoAPortada((x0 + x1) * 0.5f, (z0 + z1) * 0.5f)) return;
            const float alto = 6.8f;
            float y0 = CajamarcaAltura.Sample(x0, z0) + 0.08f;
            float y1 = CajamarcaAltura.Sample(x1, z1) + 0.08f;
            var a = new Vector3(x0, y0, z0);
            var b = new Vector3(x1, y1, z1);
            var c = new Vector3(x1, y1 + alto, z1);
            var d = new Vector3(x0, y0 + alto, z0);
            var normal = new Vector3(-nx, 0f, -nz);
            float span = Mathf.Sqrt((x1 - x0) * (x1 - x0) + (z1 - z0) * (z1 - z0));
            float tiles = span / (alto * (864f / 1152f));
            if (tiles < 0.35f) tiles = 0.35f;
            pen.Quad(a, b, c, d, normal, Color.white,
                new Vector2(0f, 0f),
                new Vector2(tiles, 0f),
                new Vector2(tiles, 1f),
                new Vector2(0f, 1f));
        }

        bool Orilla(float x, float z, float nx, float nz, out float dist)
        {
            dist = 0f;
            for (float d = 4.2f; d <= 14f; d += 1.15f)
            {
                float px = x + nx * d;
                float pz = z + nz * d;
                if (EnJardin(px, pz)) return false;
                if (OnStreet(px, pz)) continue;
                dist = d;
                return true;
            }

            return false;
        }

        bool JuntoAPortada(float x, float z)
        {
            ArmarPortadas();
            for (int i = 0; i < portadas.Count; i++)
            {
                float dx = portadas[i].x - x;
                float dz = portadas[i].y - z;
                if (dx * dx + dz * dz < 16f * 16f) return true;
            }

            return false;
        }

        void ArmarPortadas()
        {
            if (portadas != null) return;
            portadas = new List<Vector2>();
            if (hasDoor) portadas.Add(new Vector2(sanFranciscoDoor.x, sanFranciscoDoor.z));
            if (district == null) return;
            var renders = district.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renders.Length; i++)
            {
                string nombre = renders[i].gameObject.name;
                if (nombre.IndexOf("Portada", System.StringComparison.Ordinal) < 0
                    && nombre.IndexOf("Catedral", System.StringComparison.Ordinal) < 0
                    && nombre.IndexOf("Francisco", System.StringComparison.Ordinal) < 0
                    && nombre.IndexOf("Capilla", System.StringComparison.Ordinal) < 0
                    && nombre.IndexOf("Iglesia", System.StringComparison.Ordinal) < 0)
                    continue;
                Vector3 p = district.InverseTransformPoint(renders[i].bounds.center);
                portadas.Add(new Vector2(p.x, p.z));
            }
        }

        static bool MismaCalle(string a, string b)
        {
            return !string.IsNullOrEmpty(a)
                && string.Equals(a, b, System.StringComparison.OrdinalIgnoreCase);
        }

        bool TryLot(float x, float z, out float ox, out float oz, out float faceX, out float faceZ)
        {
            ox = x;
            oz = z;
            faceX = 0f;
            faceZ = 1f;
            if (LotOpen(x, z, out faceX, out faceZ))
            {
                ox = x;
                oz = z;
                return true;
            }

            for (int ring = 1; ring <= 3; ring++)
            {
                float step = 8f * ring;
                for (int i = 0; i < 8; i++)
                {
                    float a = i * 45f * Mathf.Deg2Rad;
                    float px = x + Mathf.Cos(a) * step;
                    float pz = z + Mathf.Sin(a) * step;
                    if (!LotOpen(px, pz, out faceX, out faceZ)) continue;
                    ox = px;
                    oz = pz;
                    return true;
                }
            }

            return false;
        }

        bool LotOpen(float x, float z, out float faceX, out float faceZ)
        {
            faceX = 0f;
            faceZ = 1f;
            float keepX = x - fountainX;
            float keepZ = z - fountainZ;
            if (keepX * keepX + keepZ * keepZ < (blockRadius + 4f) * (blockRadius + 4f)) return false;
            if (OnStreet(x, z) || Marked(roofCells, x, z) || Marked(waterCells, x, z)
                || CasaSobrePista(x, z) || CercaDeCasona(x, z)) return false;
            if (NearHouse(x, z, 12f)) return false;
            return StreetFace(x, z, out faceX, out faceZ);
        }

        bool StreetFace(float x, float z, out float faceX, out float faceZ)
        {
            faceX = 0f;
            faceZ = 1f;
            float best = 20f;
            bool found = false;
            for (int i = 0; i < 8; i++)
            {
                float a = i * 45f * Mathf.Deg2Rad;
                float dx = Mathf.Cos(a);
                float dz = Mathf.Sin(a);
                for (int step = 4; step <= 16; step += 4)
                {
                    if (!OnStreet(x + dx * step, z + dz * step)) continue;
                    if (step < best)
                    {
                        best = step;
                        faceX = dx;
                        faceZ = dz;
                        found = true;
                    }

                    break;
                }
            }

            return found;
        }

        bool NearHouse(float x, float z, float radius)
        {
            float limit = radius * radius;
            for (int i = 0; i < homes.Count; i++)
            {
                float dx = homes[i].x - x;
                float dz = homes[i].y - z;
                if (dx * dx + dz * dz < limit) return true;
            }

            for (int i = 0; i < LotesPuestos.Count; i++)
            {
                float dx = LotesPuestos[i].x - x;
                float dz = LotesPuestos[i].y - z;
                if (dx * dx + dz * dz < limit) return true;
            }

            return false;
        }

        static bool Marked(HashSet<long> cells, float x, float z)
        {
            return cells.Contains(WalkKey(Mathf.FloorToInt(x / Lot), Mathf.FloorToInt(z / Lot)));
        }

        void FillLots(Shader shader)
        {
            if (!walkReady || shader == null) return;
            var open = new HashSet<long>();
            for (int i = 0; i + 2 < walk.Count; i += 3)
                Stamp(open, walk[i], walk[i + 1], walk[i + 2]);
            RasterToken("Techos", roofCells);
            RasterToken("Piedra", roofCells);
            RasterToken("Agua", waterCells);

            var walls = new Pen();
            var roofs = new Pen();
            var doors = new Pen();
            var faces = new Pen();
            var taken = new HashSet<long>();
            int seeded = 0;
            foreach (long key in open)
            {
                int sx = (int)(key >> 32);
                int sz = (int)key;
                for (int dz = -1; dz <= 1; dz++)
                {
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        if (dx == 0 && dz == 0) continue;
                        if (dx != 0 && dz != 0) continue;
                        int nx = sx + dx;
                        int nz = sz + dz;
                        long lot = WalkKey(nx, nz);
                        if (open.Contains(lot) || roofCells.Contains(lot) || waterCells.Contains(lot) || taken.Contains(lot))
                            continue;
                        float cx = (nx + 0.5f) * Lot;
                        float cz = (nz + 0.5f) * Lot;
                        if (cx * cx + cz * cz > 340f * 340f) continue;
                        float awayX = cx - (sx + 0.5f) * Lot;
                        float awayZ = cz - (sz + 0.5f) * Lot;
                        float span = Mathf.Sqrt(awayX * awayX + awayZ * awayZ);
                        if (span < 0.1f) continue;
                        awayX /= span;
                        awayZ /= span;
                        float hx = cx + awayX * 1.8f;
                        float hz = cz + awayZ * 1.8f;
                        if (OnStreet(hx, hz))
                        {
                            hx += awayX * 3.2f;
                            hz += awayZ * 3.2f;
                        }

                        if (OnStreet(hx, hz) || Marked(roofCells, hx, hz) || Marked(waterCells, hx, hz)
                            || CasaSobrePista(hx, hz) || CercaDeCasona(hx, hz)) continue;
                        float keepX = hx - fountainX;
                        float keepZ = hz - fountainZ;
                        if (keepX * keepX + keepZ * keepZ < (blockRadius + 8f) * (blockRadius + 8f)) continue;
                        taken.Add(lot);
                        RaiseHouse(walls, roofs, doors, faces, hx, hz, -awayX, -awayZ, seeded);
                        seeded++;
                        if (seeded >= 640) break;
                    }

                    if (seeded >= 640) break;
                }

                if (seeded >= 640) break;
            }

            Publish(walls, "LotesMuros", "Calle/muro_adobe", shader);
            Publish(roofs, "LotesTechos", "Calle/teja", shader);
            Publish(doors, "LotesPuertas", "Calle/madera_oscura", shader);
            PublishTex(faces, "LotesFrentes", FacadeTex(), shader);
        }

        void RasterToken(string token, HashSet<long> cells)
        {
            var filters = plan.GetComponentsInChildren<MeshFilter>(true);
            for (int i = 0; i < filters.Length; i++)
            {
                if (filters[i].gameObject.name.IndexOf(token, System.StringComparison.Ordinal) < 0) continue;
                var mesh = filters[i].sharedMesh;
                if (mesh == null || !mesh.isReadable) continue;
                var verts = mesh.vertices;
                var tris = mesh.triangles;
                var space = filters[i].transform;
                for (int t = 0; t < tris.Length; t += 3)
                {
                    Vector3 a = district.InverseTransformPoint(space.TransformPoint(verts[tris[t]]));
                    Vector3 b = district.InverseTransformPoint(space.TransformPoint(verts[tris[t + 1]]));
                    Vector3 c = district.InverseTransformPoint(space.TransformPoint(verts[tris[t + 2]]));
                    Stamp(cells, new Vector2(a.x, a.z), new Vector2(b.x, b.z), new Vector2(c.x, c.z));
                }
            }
        }

        static void Stamp(HashSet<long> cells, Vector2 a, Vector2 b, Vector2 c)
        {
            cells.Add(WalkKey(Mathf.FloorToInt(a.x / Lot), Mathf.FloorToInt(a.y / Lot)));
            cells.Add(WalkKey(Mathf.FloorToInt(b.x / Lot), Mathf.FloorToInt(b.y / Lot)));
            cells.Add(WalkKey(Mathf.FloorToInt(c.x / Lot), Mathf.FloorToInt(c.y / Lot)));
            float minX = Mathf.Min(a.x, Mathf.Min(b.x, c.x));
            float maxX = Mathf.Max(a.x, Mathf.Max(b.x, c.x));
            float minZ = Mathf.Min(a.y, Mathf.Min(b.y, c.y));
            float maxZ = Mathf.Max(a.y, Mathf.Max(b.y, c.y));
            int x0 = Mathf.FloorToInt(minX / Lot);
            int x1 = Mathf.FloorToInt(maxX / Lot);
            int z0 = Mathf.FloorToInt(minZ / Lot);
            int z1 = Mathf.FloorToInt(maxZ / Lot);
            if (x1 - x0 > 40 || z1 - z0 > 40) return;
            for (int x = x0; x <= x1; x++)
            {
                for (int z = z0; z <= z1; z++)
                {
                    bool hit = false;
                    for (int s = 0; s < 4; s++)
                    {
                        float px = (x + 0.25f + (s % 2) * 0.5f) * Lot;
                        float pz = (z + 0.25f + (s / 2) * 0.5f) * Lot;
                        if (!Inside(new Vector2(px, pz), a, b, c)) continue;
                        hit = true;
                        break;
                    }

                    if (hit) cells.Add(WalkKey(x, z));
                }
            }
        }

        void RaiseHouse(Pen walls, Pen roofs, Pen doors, Pen faces, float x, float z, float faceX, float faceZ, int salt)
        {
            homes.Add(new Vector2(x, z));
            roofCells.Add(WalkKey(Mathf.FloorToInt(x / Lot), Mathf.FloorToInt(z / Lot)));
            var outward = new Vector3(faceX, 0f, faceZ);
            if (outward.sqrMagnitude < 0.01f) outward = Vector3.forward;
            outward.Normalize();
            Quaternion rot = Quaternion.LookRotation(outward, Vector3.up);
            int kind = Mathf.Abs(salt * 17 + 3) % 8;
            // Dimensiones de casas coloniales de Cajamarca — más bajas y angostas que antes.
            float height = kind == 7 ? 5.5f  : 3.5f + (kind % 5) * 0.4f;
            float width  = 7.5f;
            float depth  = 7.2f + (kind % 3) * 0.5f;
            var tint = AdobeTint[Mathf.Abs(salt) % AdobeTint.Length];
            var origin = new Vector3(x, 0.12f + height * 0.5f, z);
            Box(walls, origin, new Vector3(width, height, depth), rot, tint);
            Vector3 door = new Vector3(x, 1.85f, z) + rot * new Vector3(0f, 0f, depth * 0.5f + 0.1f);
            Vector3 right = rot * Vector3.right;
            doors.Quad(
                door - right * 0.75f - Vector3.up * 1.55f,
                door + right * 0.75f - Vector3.up * 1.55f,
                door + right * 0.75f + Vector3.up * 1.55f,
                door - right * 0.75f + Vector3.up * 1.55f,
                outward, DoorWood, true);
            float hw = width * 0.48f;
            float hh = height * 0.49f;
            Vector3 front = new Vector3(x, 0.12f + height * 0.5f, z) + rot * new Vector3(0f, 0f, depth * 0.5f + 0.08f);
            float faceW = hw * 2f;
            float faceH = hh * 2f;
            // La foto es más alta que ancha (864×1152). El alto del muro
            // ocupa la fachada entera y el ancho repite sin aplastar la puerta.
            float tiles = faceH > 0.01f ? faceW / (faceH * (864f / 1152f)) : 1f;
            var wash = FacadeWash(tint);
            faces.Quad(
                front - right * hw - Vector3.up * hh,
                front + right * hw - Vector3.up * hh,
                front + right * hw + Vector3.up * hh,
                front - right * hw + Vector3.up * hh,
                outward, wash,
                new Vector2(0f, 0f),
                new Vector2(tiles, 0f),
                new Vector2(tiles, 1f),
                new Vector2(0f, 1f));
        }

        static readonly Color[] AdobeTint =
        {
            new Color(0.97f, 0.95f, 0.91f),   // cal
            new Color(1.00f, 0.62f, 0.56f),   // salmón
            new Color(1.00f, 0.88f, 0.70f),   // crema
            new Color(1.00f, 0.55f, 0.55f),   // rosa colonial
            new Color(1.00f, 0.72f, 0.68f),   // rosa pálido
            new Color(1.00f, 0.84f, 0.48f),   // ocre
            new Color(0.90f, 0.88f, 0.84f),   // gris piedra
        };

        void CoverWalk(int index, Vector2 a, Vector2 b, Vector2 c)
        {
            float minX = Mathf.Min(a.x, Mathf.Min(b.x, c.x));
            float maxX = Mathf.Max(a.x, Mathf.Max(b.x, c.x));
            float minZ = Mathf.Min(a.y, Mathf.Min(b.y, c.y));
            float maxZ = Mathf.Max(a.y, Mathf.Max(b.y, c.y));
            int x0 = Mathf.FloorToInt(minX / 12f);
            int x1 = Mathf.FloorToInt(maxX / 12f);
            int z0 = Mathf.FloorToInt(minZ / 12f);
            int z1 = Mathf.FloorToInt(maxZ / 12f);
            for (int x = x0; x <= x1; x++)
            {
                for (int z = z0; z <= z1; z++)
                {
                    long key = WalkKey(x, z);
                    if (!walkGrid.TryGetValue(key, out var hits))
                    {
                        hits = new List<int>();
                        walkGrid.Add(key, hits);
                    }

                    hits.Add(index);
                }
            }
        }

        Transform FindPlan(string childName)
        {
            var all = plan.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < all.Length; i++)
                if (all[i].name == childName) return all[i];
            return null;
        }

        void ShowCityVista()
        {
            if (plan == null) return;
            if (!plan.activeSelf) plan.SetActive(true);
            RenderSettings.fog = vista != Vista.Planta;
        }

        static string PlanTexture(string name)
        {
            if (name.IndexOf("PortadaCatedral", System.StringComparison.Ordinal) >= 0) return "Calle/portada_catedral";
            if (name.IndexOf("PortadaFrancisco", System.StringComparison.Ordinal) >= 0) return "Calle/portada_francisco";
            // La banda que cose la plaza con la calle es pavimento de plaza.
            if (name.IndexOf("Banda", System.StringComparison.Ordinal) >= 0) return "Calle/piso_plaza";
            if (name.IndexOf("Calles", System.StringComparison.Ordinal) >= 0) return "Calle/piso_empedrado";
            if (name.IndexOf("Plazas", System.StringComparison.Ordinal) >= 0) return "Calle/piso_plaza";
            if (name.IndexOf("Muros", System.StringComparison.Ordinal) >= 0) return "Calle/muro_adobe";
            if (name.IndexOf("Techos", System.StringComparison.Ordinal) >= 0) return "Calle/teja";
            if (name.IndexOf("Piedra", System.StringComparison.Ordinal) >= 0) return "Calle/piedra";
            if (name.IndexOf("Suelo", System.StringComparison.Ordinal) >= 0) return "Bosque/piso_cesped";
            return null;
        }

        static Color PlanColor(string name)
        {
            if (name.IndexOf("Puertas", System.StringComparison.Ordinal) >= 0)
                return new Color(0.22f, 0.12f, 0.08f);
            if (name.IndexOf("Agua", System.StringComparison.Ordinal) >= 0)
                return new Color(0.28f, 0.48f, 0.62f);
            return new Color(0.62f, 0.56f, 0.40f);
        }

        static Texture2D Swatch(Color color)
        {
            var tex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            tex.SetPixel(0, 0, color);
            tex.Apply();
            tex.wrapMode = TextureWrapMode.Clamp;
            return tex;
        }

        void Publish(Pen pen, string name, string resource, Shader shader, Transform parent = null)
        {
            if (pen.Empty) return;
            if (shader == null) shader = Shader.Find("AYNI/Calle") ?? Shader.Find("Standard") ?? Shader.Find("Sprites/Default");
            if (shader == null) return;
            var host = new GameObject(name);
            host.transform.SetParent(parent != null ? parent : district, false);
            var filter = host.AddComponent<MeshFilter>();
            filter.sharedMesh = pen.Bake(name);
            var renderer = host.AddComponent<MeshRenderer>();
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            var mat = new Material(shader);
            if (!string.IsNullOrEmpty(resource))
            {
                var tex = CalleTex(resource);
                if (tex != null) mat.mainTexture = tex;
            }
            renderer.sharedMaterial = mat;
        }

        sealed class Pen
        {
            readonly List<Vector3> verts = new List<Vector3>();
            readonly List<Vector3> normals = new List<Vector3>();
            readonly List<Color> colors = new List<Color>();
            readonly List<Vector2> uvs = new List<Vector2>();
            readonly List<int> tris = new List<int>();

            public bool Empty => verts.Count == 0;

            public void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 normal, Color color, bool stretch)
            {
                Vector2 ua, ub, uc, ud;
                if (stretch)
                {
                    ua = new Vector2(0f, 0f);
                    ub = new Vector2(1f, 0f);
                    uc = new Vector2(1f, 1f);
                    ud = new Vector2(0f, 1f);
                }
                else
                {
                    ua = UV(a, normal);
                    ub = UV(b, normal);
                    uc = UV(c, normal);
                    ud = UV(d, normal);
                }

                Quad(a, b, c, d, normal, color, ua, ub, uc, ud);
            }

            public void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 normal, Color color,
                Vector2 ua, Vector2 ub, Vector2 uc, Vector2 ud)
            {
                int i = verts.Count;
                verts.Add(a);
                verts.Add(b);
                verts.Add(c);
                verts.Add(d);
                uvs.Add(ua);
                uvs.Add(ub);
                uvs.Add(uc);
                uvs.Add(ud);
                for (int n = 0; n < 4; n++)
                {
                    normals.Add(normal);
                    colors.Add(color);
                }

                tris.Add(i);
                tris.Add(i + 1);
                tris.Add(i + 2);
                tris.Add(i);
                tris.Add(i + 2);
                tris.Add(i + 3);
            }

            public void Tri(Vector3 a, Vector3 b, Vector3 c, Vector3 normal, Color color)
            {
                int i = verts.Count;
                verts.Add(a);
                verts.Add(b);
                verts.Add(c);
                uvs.Add(UV(a, normal));
                uvs.Add(UV(b, normal));
                uvs.Add(UV(c, normal));
                for (int n = 0; n < 3; n++)
                {
                    normals.Add(normal);
                    colors.Add(color);
                }

                tris.Add(i);
                tris.Add(i + 1);
                tris.Add(i + 2);
            }

            public void TransformXZ(float cx, float cz, float cos, float sin)
            {
                for (int i = 0; i < verts.Count; i++)
                {
                    Vector3 p = verts[i];
                    verts[i] = new Vector3(cx + p.x * cos - p.z * sin, p.y,
                                           cz + p.x * sin + p.z * cos);
                    Vector3 n = normals[i];
                    normals[i] = new Vector3(n.x * cos - n.z * sin, n.y,
                                             n.x * sin + n.z * cos);
                }
            }

            static Vector2 UV(Vector3 p, Vector3 normal)
            {
                Vector3 n = normal.sqrMagnitude > 0.0001f ? normal.normalized : Vector3.up;
                if (Mathf.Abs(n.y) > 0.6f) return new Vector2(p.x, p.z) / PlanFloorTile;
                if (Mathf.Abs(n.x) > Mathf.Abs(n.z)) return new Vector2(p.z, p.y) * 0.5f;
                return new Vector2(p.x, p.y) * 0.5f;
            }

            public Mesh Bake(string meshName)
            {
                var mesh = new Mesh { name = meshName };
                mesh.SetVertices(verts);
                mesh.SetNormals(normals);
                mesh.SetColors(colors);
                mesh.SetUVs(0, uvs);
                mesh.SetTriangles(tris, 0);
                mesh.RecalculateBounds();
                return mesh;
            }
        }
    }
}
