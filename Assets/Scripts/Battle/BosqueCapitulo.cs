using System.Collections.Generic;
using AYNI.Narrative;
using AYNI.UI;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace AYNI.Battle
{
    /// <summary>
    /// A 3D walk in a forest. The near trees are Kenney Nature Kit (CC0).
    /// Past the river, the pine grove is poly coniferous forest (SQUID) and holds the aura berries.
    /// Built at runtime, west of the lagoon. The scene file stays as it is.
    /// </summary>
    public class BosqueCapitulo : MonoBehaviour
    {
        public const int KindCount = 8;

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

        struct Air
        {
            public bool Fog;
            public FogMode Mode;
            public Color Color;
            public float Start;
            public float End;
            public float Density;
            public Color Ambient;
            public float AmbientIntensity;
            public AmbientMode AmbientMode;
        }

        struct HudBit
        {
            public GameObject Go;
            public bool WasOn;
        }

        sealed class Walker
        {
            public Transform Body;
            public SpriteFrameAnimator Anim;
            public Vector3 Local;
            public Vector3 Goal;
            public float Wait;
            public bool Hero;
            public bool Chase;
            public float Detour;
            public Vector3 Home;
            public float Vy;
            public Vector3 Push;
            public bool Grounded = true;
            public bool Swimming;
            public bool Fell;
        }

        sealed class Ripple
        {
            public Transform Body;
            public float Age;
        }

        struct Trunk
        {
            public float X;
            public float Z;
            public float Radius;
        }

        static readonly Color Sod = new Color(0.23f, 0.40f, 0.18f);
        static readonly Color Path = new Color(0.42f, 0.30f, 0.16f);
        static readonly Color Sky = new Color(0.55f, 0.72f, 0.78f);
        static readonly Color Mist = new Color(0.62f, 0.74f, 0.66f);

        static readonly string[] Pines =
        {
            "tree_pineTallA", "tree_pineTallB", "tree_pineTallC", "tree_pineTallD",
            "tree_pineDefaultA", "tree_pineDefaultB", "tree_pineRoundA", "tree_pineRoundC",
            "tree_pineSmallA", "tree_pineSmallC",
        };

        static readonly string[] Broad =
        {
            "tree_oak", "tree_oak_dark", "tree_default", "tree_default_dark",
            "tree_tall", "tree_detailed", "tree_simple", "tree_small",
        };

        static readonly string[] Brush =
        {
            "plant_bush", "plant_bushLarge", "plant_bushSmall", "plant_bushDetailed",
        };

        static readonly string[] Stones =
        {
            "rock_largeA", "rock_largeB", "rock_largeD", "rock_smallA", "rock_smallF", "rock_tallA", "rock_tallB",
        };

        static BosqueCapitulo current;

        public static bool IsOpen => current != null;

        readonly List<Walker> walkers = new List<Walker>();
        readonly List<HudBit> hudBits = new List<HudBit>();
        readonly List<Trunk> trunks = new List<Trunk>();
        readonly Dictionary<string, float> heightAtOne = new Dictionary<string, float>();
        LagunaBattleManager battle;
        AyniBakedBosque bakedGrove;
        Transform dynamicRoot;
        Transform grove;
        GameObject hud;
        Camera cam;
        Pose saved;
        Air savedAir;
        bool savedPose;
        bool restored;
        bool snapped;
        int planted;
        Text line;
        TituloFugaz lineFade;
        enum Vista
        {
            Inclinacion,
            Planta,
        }

        readonly CamaraTercera camara = new CamaraTercera
        {
            Pitch = 20f,
            Distance = 14f,
            MinDistance = 6f,
            MaxDistance = 32f,
            PivotHeight = 2.8f,
        };
        BotonesDeMovimiento movimientos;
        Vista vista = Vista.Inclinacion;
        float planSize = 42f;
        bool planSuelto;
        bool planArrastre;
        Vector2 planMira;
        Vector3 planCursor;
        readonly List<Ripple> ripples = new List<Ripple>();
        readonly List<Transform> berries = new List<Transform>();
        int berriesFound;
        bool toldGrove;
        bool toldWord;
        bool allowJump = true;
        bool jumpLatch = true;
        float safeUntil;

        static readonly string[] ConiferTrees =
        {
            "Coniferas/Prefabs/trees/SM_large_spruce",
            "Coniferas/Prefabs/trees/SM_large_spruce_1",
            "Coniferas/Prefabs/trees/SM_medium_spruce",
            "Coniferas/Prefabs/trees/SM_medium_spruce_1",
            "Coniferas/Prefabs/trees/SM_pine",
            "Coniferas/Prefabs/trees/SM_pine_1",
            "Coniferas/Prefabs/trees/SM_pine_2",
            "Coniferas/Prefabs/trees/SM_pine_3",
            "Coniferas/Prefabs/trees/SM_pine_4",
            "Coniferas/Prefabs/trees/SM_small_spruce",
            "Coniferas/Prefabs/trees/SM_small_spruce_1",
        };

        static readonly string[] ConiferBrush =
        {
            "Coniferas/Prefabs/plants/SM_bush",
            "Coniferas/Prefabs/plants/SM_bush_1",
            "Coniferas/Prefabs/plants/SM_long_grass",
            "Coniferas/Prefabs/plants/SM_grass",
            "Coniferas/Prefabs/plants/SM_grass_1",
            "Coniferas/Prefabs/plants/SM_tree_sprout",
        };

        const float MapHalfX = 72f;
        const float MapMinZ = -32f;
        const float MapMaxZ = 96f;
        const float RiverMinZ = 16.4f;
        const float RiverMaxZ = 25.2f;
        const float RiverHalfX = 72f;
        const float WaterY = 0.2f;
        System.Random rng;

        public static void Begin(LagunaBattleManager battle)
        {
            if (current != null || battle == null || CajamarcaCalles.IsOpen) return;
            var go = new GameObject("Bosque");
            current = go.AddComponent<BosqueCapitulo>();
            current.Open(battle);
        }

        public static void CloseCurrent()
        {
            if (current != null) current.Close();
        }

        Transform RuntimeParent => dynamicRoot != null ? dynamicRoot : grove;

        void Open(LagunaBattleManager battle)
        {
            this.battle = battle;
            PalabrasCartilla.Close();
            rng = new System.Random(11);
            if (battle.startScreen != null) battle.startScreen.SetActive(false);
            HideBattleHud();
            cam = Camera.main;
            SaveCamera();
            SaveAir();

            // Si la arboleda ya está horneada en la escena, se usa tal cual.
            bakedGrove = FindBakedGrove();
            grove = bakedGrove != null
                ? bakedGrove.transform.Find(AyniBakedBosque.GroveName)
                : null;

            if (grove != null)
            {
                bakedGrove.gameObject.SetActive(true);
                dynamicRoot = new GameObject("__DINAMICO__").transform;
                dynamicRoot.SetParent(grove, false);
                BindBakedGrove();
            }
            else
            {
                bakedGrove = null;
                grove = new GameObject("Arboleda").transform;
                grove.SetParent(transform, false);
                grove.position = new Vector3(-360f, 0f, 0f);
                BuildGrove();
            }

            SpawnWalkers();
            LluviaDeAlmas.Begin(RuntimeParent, walkers.Count > 0 ? walkers[0].Body : null,
                (x, z) => 0.2f, 1f);
            BuildHud();
            FigurasDelCamino.DressGrove(grove);
            SolidoDelCamino.Armar(grove);
            Jornada.BeginPlay();
            snapped = false;
            battle?.Contar(LeyDelMonte.ForestKicker, LeyDelMonte.ForestLie);
        }

        static AyniBakedBosque FindBakedGrove()
        {
            var all = Object.FindObjectsByType<AyniBakedBosque>(FindObjectsInactive.Include);
            return all.Length > 0 ? all[0] : null;
        }

        /// <summary>
        /// Reconstruye los datos de navegación (troncos, número de árboles y
        /// bayas) a partir de la arboleda horneada, sin volver a generarla.
        /// </summary>
        void BindBakedGrove()
        {
            trunks.Clear();
            berries.Clear();
            planted = 0;

            var all = grove.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < all.Length; i++)
            {
                var t = all[i];
                if (t.name == "BayaDeAura")
                {
                    berries.Add(t);
                    continue;
                }

                if (t.parent != grove) continue;

                string n = t.name;
                float radius = 0f;
                if (n.StartsWith("Bosque/tree", System.StringComparison.Ordinal)
                    || n.StartsWith("Coniferas/Prefabs/trees", System.StringComparison.Ordinal))
                {
                    radius = Mathf.Clamp(Span(t.gameObject) * 0.16f, 0.55f, 2.4f);
                    planted++;
                }
                else if (n.StartsWith("Bosque/rock", System.StringComparison.Ordinal))
                {
                    radius = Mathf.Clamp(Span(t.gameObject) * 0.34f, 0.45f, 1.8f);
                }

                if (radius <= 0f) continue;
                trunks.Add(new Trunk
                {
                    X = t.localPosition.x,
                    Z = t.localPosition.z,
                    Radius = radius,
                });
            }
        }

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
                Clear = cam.clearFlags,
            };
            savedPose = true;
        }

        void SaveAir()
        {
            savedAir = new Air
            {
                Fog = RenderSettings.fog,
                Mode = RenderSettings.fogMode,
                Color = RenderSettings.fogColor,
                Start = RenderSettings.fogStartDistance,
                End = RenderSettings.fogEndDistance,
                Density = RenderSettings.fogDensity,
                Ambient = RenderSettings.ambientLight,
                AmbientIntensity = RenderSettings.ambientIntensity,
                AmbientMode = RenderSettings.ambientMode,
            };
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = Sky;
            RenderSettings.fogStartDistance = 42f;
            RenderSettings.fogEndDistance = 180f;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.78f, 0.80f, 0.74f);
            RenderSettings.ambientIntensity = 1f;
        }

        void RestoreCamera()
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
            RenderSettings.fog = savedAir.Fog;
            RenderSettings.fogMode = savedAir.Mode;
            RenderSettings.fogColor = savedAir.Color;
            RenderSettings.fogStartDistance = savedAir.Start;
            RenderSettings.fogEndDistance = savedAir.End;
            RenderSettings.fogDensity = savedAir.Density;
            RenderSettings.ambientMode = savedAir.AmbientMode;
            RenderSettings.ambientLight = savedAir.Ambient;
            RenderSettings.ambientIntensity = savedAir.AmbientIntensity;
        }

        void Close()
        {
            CamaraTercera.Release();
            RestoreCamera();
            RestoreBattleHud();
            if (hud != null) Destroy(hud);

            // Lo dinámico (caminantes, salpicaduras, serpiente) se va;
            // la arboleda horneada se queda en la escena, solo se oculta.
            if (dynamicRoot != null)
            {
                Destroy(dynamicRoot.gameObject);
                dynamicRoot = null;
            }

            if (bakedGrove != null)
                bakedGrove.gameObject.SetActive(false);

            if (battle != null && battle.startScreen != null)
                battle.startScreen.SetActive(true);
            Destroy(gameObject);
        }

        void OnDestroy()
        {
            CamaraTercera.Release();
            if (current == this) current = null;
            RestoreCamera();
        }

        void Update()
        {
            if (CaminoRelato.Reading)
            {
                jumpLatch = false;
                CamaraTercera.ShowCursor();
                return;
            }

            if (PartidaDelCamino.RioCruzado) SeguirElCamino.Tramo();

            allowJump = jumpLatch;
            jumpLatch = true;
            if (Input.GetKeyDown(KeyCode.Escape))
                Close();
            if (Input.GetKeyDown(KeyCode.V))
                ElegirVista(vista != Vista.Planta);
            if (Input.GetKeyDown(KeyCode.C))
            {
                camara.Yaw = 0f;
                CamaraTercera.PlanYaw = 0f;
                planSuelto = false;
                snapped = false;
            }

            if (CamaraTercera.TakeNorth())
            {
                camara.Yaw = 0f;
                CamaraTercera.PlanYaw = 0f;
                planSuelto = false;
                snapped = false;
            }

            var lead = Hero();
            bool nada = lead != null && lead.Swimming;
            if (nada)
                CamaraTercera.Release();
            else if (vista == Vista.Planta)
            {
                CamaraTercera.Release();
                CamaraTercera.ReadPlanTurn();
                Vector2 aqui = lead != null ? new Vector2(lead.Local.x, lead.Local.z) : Vector2.zero;
                BotonesDeMovimiento.LeerMapa(cam, grove, ref planSize, 14f, 150f,
                    ref planSuelto, ref planArrastre, ref planMira, ref planCursor, aqui);
            }
            else
                camara.ReadMouse();
            BrujulaDelMapa.Rumbo = vista == Vista.Planta ? CamaraTercera.PlanYaw : camara.Yaw;
            movimientos?.Marcar(vista == Vista.Planta, vista == Vista.Inclinacion);
            BrujulaDelMapa.Mostrar = lead == null || !lead.Swimming;

            for (int i = 0; i < walkers.Count; i++)
            {
                var walker = walkers[i];
                if (walker.Hero) Steer(walker);
                else Pace(walker);
            }

            Hunt();

            BobBerries();
            GatherBerries();
            var here = Hero();
            if (here != null && !toldWord && here.Local.z > 3f && here.Local.z < 12f)
            {
                toldWord = true;
                battle?.ShowRoadLine("PALABRAS", LeyDelMonte.WordForest, LeyDelMonte.Voice);
            }

            if (here != null) FigurasDelCamino.TouchGrove(here.Local);

            for (int i = ripples.Count - 1; i >= 0; i--)
            {
                var ripple = ripples[i];
                ripple.Age += Time.deltaTime;
                if (ripple.Body == null || ripple.Age > 0.7f)
                {
                    if (ripple.Body != null) Destroy(ripple.Body.gameObject);
                    ripples.RemoveAt(i);
                    continue;
                }

                float u = ripple.Age / 0.7f;
                ripple.Body.localScale = Vector3.one * Mathf.Lerp(0.4f, 3.2f, u);
            }
        }

        void LateUpdate()
        {
            var hero = Hero();
            if (cam == null || hero == null || hero.Body == null) return;

            Vector3 feet = hero.Body.position;
            bool mapa = vista == Vista.Planta && !hero.Swimming;
            cam.orthographic = mapa;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.fieldOfView = 50f;
            cam.nearClipPlane = mapa ? 0.3f : 0.2f;
            cam.farClipPlane = mapa ? 420f : 280f;
            Jornada.TintSky(cam, Sky);
            Jornada.TintAir(Sky, new Color(0.78f, 0.80f, 0.74f));
            cam.backgroundColor = RenderSettings.fogColor;
            Vector3 position;
            Quaternion rotation;
            if (hero.Swimming)
            {
                position = feet + new Vector3(-7.4f, 3.8f, -1.2f);
                float minZ = grove.position.z + RiverMinZ + 0.8f;
                float maxZ = grove.position.z + RiverMaxZ - 0.3f;
                position.z = Mathf.Clamp(position.z, minZ, maxZ);
                Vector3 look = feet + Vector3.up * 0.85f;
                rotation = Quaternion.LookRotation(look - position, Vector3.up);
            }
            else if (mapa)
            {
                Vector3 focus = feet;
                if (planSuelto && grove != null)
                    focus = grove.TransformPoint(new Vector3(planMira.x, 0f, planMira.y));
                position = focus + Vector3.up * 200f;
                rotation = Quaternion.Euler(90f, CamaraTercera.PlanYaw, 0f);
                cam.orthographicSize = planSize;
            }
            else
            {
                camara.Pose(feet, out position, out rotation);
                if (position.y < feet.y + 1.8f)
                    position.y = feet.y + 1.8f;
            }

            if (mapa)
            {
                if (!snapped)
                    cam.transform.SetPositionAndRotation(position, rotation);
                else
                {
                    float t = 1f - Mathf.Exp(-6f * Time.deltaTime);
                    cam.transform.position = Vector3.Lerp(cam.transform.position, position, t);
                    cam.transform.rotation = rotation;
                }

                snapped = true;
            }
            else
            {
                Vector3 pivot = feet + Vector3.up * (hero.Swimming ? 1.1f : camara.PivotHeight);
                SolidoDelCamino.Encuadrar(cam, pivot, position, !snapped);
                snapped = true;
            }

            for (int i = 0; i < walkers.Count; i++)
            {
                if (walkers[i].Body == null) continue;
                if (mapa)
                {
                    walkers[i].Body.rotation = cam.transform.rotation;
                    continue;
                }

                Vector3 flat = cam.transform.forward;
                flat.y = 0f;
                if (flat.sqrMagnitude < 0.001f) continue;
                walkers[i].Body.rotation = Quaternion.LookRotation(flat, Vector3.up);
            }
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

        Walker Hero()
        {
            for (int i = 0; i < walkers.Count; i++)
                if (walkers[i].Hero) return walkers[i];
            return null;
        }

        void Steer(Walker walker)
        {
            if (walker.Swimming)
            {
                Swim(walker);
                return;
            }

            if (!walker.Grounded)
            {
                Fly(walker);
                return;
            }

            if (allowJump && Input.GetKeyDown(KeyCode.Space))
            {
                Jump(walker);
                return;
            }

            Vector3 worldMove;
            Vector3 localMove;
            if (vista == Vista.Planta)
            {
                localMove = camara.PlanLocal(grove);
                worldMove = CamaraTercera.WorldDir(grove, localMove);
            }
            else
            {
                worldMove = camara.Wish();
                localMove = camara.ToLocal(grove, worldMove);
            }

            bool moving = Slide(walker, localMove, 7.2f);
            walker.Local.y = BankY(walker.Local.z);
            walker.Anim?.PlayStep(worldMove, moving);
            walker.Body.localPosition = walker.Local;
            if (walker.Hero && walker.Local.z > 13.2f && walker.Local.z < RiverMinZ)
                Note(LeyDelMonte.ForestLip);
        }

        void Jump(Walker walker)
        {
            walker.Grounded = false;
            walker.Fell = false;
            walker.Vy = 7.6f;
            Vector3 worldMove = camara.Wish();
            if (worldMove.sqrMagnitude < 0.04f)
                worldMove = camara.FlatForward();
            Vector3 push = camara.ToLocal(grove, worldMove);
            if (push.sqrMagnitude < 0.01f) push = Vector3.forward;
            walker.Push = push.normalized * 5.1f;
            walker.Anim?.PlayJump();
        }

        void Fly(Walker walker)
        {
            walker.Vy -= 20f * Time.deltaTime;
            Vector3 next = walker.Local + walker.Push * Time.deltaTime;
            next.y += walker.Vy * Time.deltaTime;
            next.x = Mathf.Clamp(next.x, -MapHalfX, MapHalfX);
            next.z = Mathf.Clamp(next.z, MapMinZ, MapMaxZ);
            if (!Cabe(walker.Local, next))
            {
                next.x = walker.Local.x;
                next.z = walker.Local.z;
            }

            if (InRiver(next.x, next.z) && next.y < 0.55f)
            {
                EnterWater(walker, next);
                return;
            }

            float ground = BankY(next.z);
            if (!InRiver(next.x, next.z) && next.y <= ground && walker.Vy <= 0f)
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

        void EnterWater(Walker walker, Vector3 at)
        {
            walker.Swimming = true;
            walker.Grounded = false;
            walker.Fell = false;
            walker.Vy = 0f;
            walker.Push = Vector3.zero;
            at.y = WaterY;
            at.x = Mathf.Clamp(at.x, -RiverHalfX + 1f, RiverHalfX - 1f);
            at.z = Mathf.Clamp(at.z, RiverMinZ + 0.5f, RiverMaxZ - 0.6f);
            walker.Local = at;
            walker.Body.localPosition = at;
            Splash(at);
            walker.Anim?.PlaySwim();
            if (walker.Hero)
                Note(LeyDelMonte.ForestFall);
        }

        void Swim(Walker walker)
        {
            float h = Input.GetAxisRaw("Horizontal");
            float v = Input.GetAxisRaw("Vertical");
            bool stroke = h * h + v * v > 0.04f;
            float current = stroke ? 0.45f : 2.7f;
            var move = new Vector3(h, 0f, v);
            if (move.sqrMagnitude > 1f) move.Normalize();
            Vector3 next = walker.Local + (move * 4.5f + Vector3.right * current) * Time.deltaTime;
            next.x = Mathf.Clamp(next.x, -RiverHalfX + 0.5f, RiverHalfX - 0.5f);
            next.y = WaterY + Mathf.Sin(Time.time * 5.2f + walker.Local.x) * 0.07f;

            float bankX = Mathf.Clamp(next.x, -MapHalfX + 2f, MapHalfX - 2f);
            if (next.z > RiverMaxZ - 0.3f && v > 0.2f)
            {
                LeaveWater(walker, new Vector3(bankX, 0.2f, RiverMaxZ + 1.4f));
                return;
            }

            if (next.z < RiverMinZ + 0.35f && v < -0.2f)
            {
                LeaveWater(walker, new Vector3(bankX, BankY(RiverMinZ - 1.15f), RiverMinZ - 1.15f));
                return;
            }

            next.z = Mathf.Clamp(next.z, RiverMinZ + 0.35f, RiverMaxZ - 0.2f);
            walker.Local = next;
            if (Mathf.Abs(h) > 0.2f)
                walker.Anim?.SetFacing(h > 0f);
            else if (!stroke)
                walker.Anim?.SetFacing(true);
            walker.Anim?.PlaySwim();
            walker.Body.localPosition = walker.Local;
            if (walker.Hero)
                Note(stroke ? LeyDelMonte.ForestSwim : LeyDelMonte.ForestDrift);
        }

        void LeaveWater(Walker walker, Vector3 spot)
        {
            walker.Swimming = false;
            walker.Grounded = true;
            walker.Vy = 0f;
            walker.Push = Vector3.zero;
            walker.Local = spot;
            walker.Body.localPosition = spot;
            walker.Anim?.PlayStep(Vector3.zero, false);
            if (walker.Hero)
            {
                bool far = spot.z > RiverMaxZ;
                if (far) PartidaDelCamino.CruzarRio();
                if (far && berriesFound < berries.Count)
                    Note(LeyDelMonte.ForestGrove);
                else if (far && berries.Count > 0 && berriesFound >= berries.Count)
                    Note(LeyDelMonte.ForestBerriesDone);
                else
                    Note(LeyDelMonte.ForestOut);
            }
        }

        void Splash(Vector3 at)
        {
            var host = new GameObject("Salpicadura");
            host.transform.SetParent(RuntimeParent, false);
            host.transform.localPosition = at + Vector3.up * 0.15f;
            host.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            var filter = host.AddComponent<MeshFilter>();
            filter.sharedMesh = Flat("Salpicadura", 1.4f, 1.4f, new Color(0.75f, 0.92f, 0.9f, 0.9f));
            var renderer = host.AddComponent<MeshRenderer>();
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            var shader = Shader.Find("AYNI/Calle") ?? Shader.Find("Sprites/Default") ?? Shader.Find("Standard");
            if (shader != null)
            {
                var mat = new Material(shader);
                renderer.sharedMaterial = mat;
            }

            ripples.Add(new Ripple { Body = host.transform });
        }

        void Pace(Walker walker)
        {
            if (walker.Chase)
            {
                walker.Detour -= Time.deltaTime;
                var hero = Hero();
                if (hero != null && !hero.Swimming && walker.Detour <= 0f)
                    walker.Goal = hero.Local;
            }

            walker.Wait -= Time.deltaTime;
            Vector3 to = walker.Goal - walker.Local;
            to.y = 0f;
            if (!walker.Chase && (to.magnitude < 0.45f || walker.Wait < -2f))
            {
                walker.Goal = RandomPoint();
                walker.Wait = Range(0.3f, 1.4f);
                walker.Anim?.PlayStep(Vector3.zero, false);
                return;
            }

            if (to.sqrMagnitude < 0.04f)
            {
                walker.Anim?.PlayStep(Vector3.zero, false);
                return;
            }

            float speed = walker.Chase ? 5.1f : 2.4f;
            bool moving = Slide(walker, to.normalized, speed);
            walker.Anim?.PlayStep(CamaraTercera.WorldDir(grove, to.normalized), moving);
            walker.Local.y = BankY(walker.Local.z);
            walker.Body.localPosition = walker.Local;
            if (!moving)
            {
                walker.Goal = RandomPoint();
                if (walker.Chase) walker.Detour = 0.7f;
            }
        }

        void Hunt()
        {
            if (Time.time < safeUntil) return;
            var hero = Hero();
            if (hero == null || hero.Swimming || !hero.Grounded) return;
            for (int i = 0; i < walkers.Count; i++)
            {
                var hunter = walkers[i];
                if (!hunter.Chase || hunter.Body == null) continue;
                Vector3 gap = hunter.Local - hero.Local;
                gap.y = 0f;
                if (gap.sqrMagnitude > 2.4f * 2.4f) continue;
                hero.Local = new Vector3(0f, 0.2f, 0f);
                hero.Body.localPosition = hero.Local;
                if (gap.sqrMagnitude > 0.01f)
                {
                    hunter.Local += gap.normalized * 10f;
                    hunter.Local.y = BankY(hunter.Local.z);
                    hunter.Body.localPosition = hunter.Local;
                }

                hunter.Detour = 0.9f;
                safeUntil = Time.time + 1.5f;
                Note(LeyDelMonte.TeAlcanzo);
                return;
            }
        }

        bool Slide(Walker walker, Vector3 move, float speed)
        {
            if (move.sqrMagnitude < 0.0001f) return false;
            Vector3 step = move * speed * Time.deltaTime;
            Vector3 next = walker.Local + step;
            next.y = walker.Local.y;
            if (OpenGround(next.x, next.z) && Cabe(walker.Local, next))
            {
                walker.Local = next;
                return true;
            }

            var alongX = walker.Local + new Vector3(step.x, 0f, 0f);
            if (OpenGround(alongX.x, alongX.z) && Cabe(walker.Local, alongX))
            {
                walker.Local = alongX;
                return true;
            }

            var alongZ = walker.Local + new Vector3(0f, 0f, step.z);
            if (OpenGround(alongZ.x, alongZ.z) && Cabe(walker.Local, alongZ))
            {
                walker.Local = alongZ;
                return true;
            }

            return false;
        }

        bool Cabe(Vector3 desde, Vector3 hasta)
        {
            var space = RuntimeParent;
            if (space == null) return true;
            return SolidoDelCamino.Puede(space.TransformPoint(desde), space.TransformPoint(hasta), 0.72f, 2.3f);
        }

        bool OpenGround(float x, float z)
        {
            if (x < -MapHalfX || x > MapHalfX || z < MapMinZ || z > MapMaxZ) return false;
            if (InRiver(x, z)) return false;
            for (int i = 0; i < trunks.Count; i++)
            {
                float dx = x - trunks[i].X;
                float dz = z - trunks[i].Z;
                if (dx * dx + dz * dz < trunks[i].Radius * trunks[i].Radius)
                    return false;
            }

            return true;
        }

        Vector3 RandomPoint()
        {
            for (int i = 0; i < 24; i++)
            {
                float x = Range(-MapHalfX + 8f, MapHalfX - 8f);
                float z = Range(MapMinZ + 6f, MapMaxZ - 8f);
                if (OpenGround(x, z)) return new Vector3(x, 0.2f, z);
            }

            return new Vector3(0f, 0.2f, -8f);
        }

        void SpawnWalkers()
        {
            var hero = FighterRoster.Current;
            Vector3 casa = PartidaDelCamino.RioCruzado
                ? new Vector3(0f, 0.2f, RiverMaxZ + 1.4f)
                : new Vector3(0f, 0.2f, 0f);
            var lead = Spawn("BosqueHero", hero.Sheet, WalkerScale(hero.Scale), hero.FacesRight, casa, true);
            lead.Home = lead.Local;
            walkers.Add(lead);

            var trio = new[] { BattleCast.Vizcacha, BattleCast.Atoq, BattleCast.Conejo };
            int side = 0;
            for (int i = 0; i < trio.Length; i++)
            {
                if (trio[i] == hero.Sheet) continue;
                side++;
                float x = side == 1 ? -2.6f : 2.8f;
                var pal = Spawn("Bosque" + trio[i].Name, trio[i], WalkerScale(BattleCast.BodyScale(trio[i])), true,
                    new Vector3(x, 0.2f, 1f), false);
                pal.Home = pal.Local;
                pal.Goal = RandomPoint();
                walkers.Add(pal);
            }

            var vecinos = BattleCast.Vecinos;
            for (int i = 0; i < vecinos.Length; i++)
            {
                Vector3 at = RandomPoint();
                var pal = Spawn("BosqueVecino" + (i + 1), vecinos[i], WalkerScale(BattleCast.BodyScale(vecinos[i])), true, at, false);
                pal.Home = at;
                pal.Goal = RandomPoint();
                walkers.Add(pal);
            }

            var cazas = BattleCast.Cazadores;
            for (int i = 0; i < cazas.Length; i++)
            {
                Vector3 at = RandomPoint();
                for (int n = 0; n < 8 && at.sqrMagnitude < 18f * 18f; n++)
                    at = RandomPoint();
                var hunter = Spawn("BosqueCaza" + (i + 1), cazas[i], WalkerScale(BattleCast.BodyScale(cazas[i])), true, at, false);
                hunter.Home = at;
                hunter.Goal = at;
                hunter.Chase = true;
                walkers.Add(hunter);
            }
        }

        static float WalkerScale(float scale) => scale;

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
                Hero = hero,
            };
        }

        void BuildHud()
        {
            var canvas = battle != null && battle.startScreen != null
                ? battle.startScreen.transform.parent
                : null;
            if (canvas == null) return;

            var plate = Storybook.Plate(canvas, "BosqueHUD", Vector2.zero, Vector2.one, new Color(0f, 0f, 0f, 0f));
            hud = plate.gameObject;

            var kicker = AddText(hud.transform, "Kicker", new Vector2(0.16f, 0.88f), new Vector2(0.84f, 0.97f));
            Storybook.DressText(kicker, LeyDelMonte.ForestKicker,
                Storybook.DisplayFont, 26, Color.white, FontStyle.Bold);
            Storybook.Stamp(kicker);

            line = AddText(hud.transform, "Linea", new Vector2(0.10f, 0.78f), new Vector2(0.90f, 0.88f));
            string walk = planted > 0 ? LeyDelMonte.ForestWalk : LeyDelMonte.ForestMissing;
            Storybook.DressText(line, walk, Storybook.DisplayFont, 18, Color.white, FontStyle.Bold);
            Storybook.Stamp(line);

            var credit = AddText(hud.transform, "Credito", new Vector2(0.28f, 0.035f), new Vector2(0.62f, 0.09f));
            Storybook.DressText(credit, LeyDelMonte.ForestCredit,
                Storybook.BodyFont, 14, new Color(1f, 1f, 1f, 0.85f), FontStyle.Bold);
            TituloFugaz.Begin(kicker.gameObject);
            lineFade = TituloFugaz.Begin(line.gameObject);
            TituloFugaz.Begin(credit.gameObject);

            Storybook.IconButton(hud.transform, "Volver", Storybook.Marca.Volver,
                new Vector2(0.908f, 0.02f), new Vector2(0.982f, 0.15f),
                Close);
            Storybook.IconButton(hud.transform, "Historia", Storybook.Marca.Historia,
                new Vector2(0.018f, 0.83f), new Vector2(0.092f, 0.96f),
                () => battle?.AbrirHistoria());
            movimientos = BotonesDeMovimiento.Armar(
                hud.transform,
                () => ElegirVista(true),
                () => ElegirVista(false),
                () => Zoom(0.72f),
                () => Zoom(1.38f));
            Storybook.Brujula(hud.transform);
            hud.transform.SetAsLastSibling();
        }

        void ElegirVista(bool mapa)
        {
            Vista next = mapa ? Vista.Planta : Vista.Inclinacion;
            if (vista == next) return;
            if (mapa) CamaraTercera.PlanYaw = camara.Yaw;
            else camara.Yaw = CamaraTercera.PlanYaw;
            vista = next;
            snapped = false;
        }

        void Zoom(float mul)
        {
            if (vista == Vista.Planta)
                planSize = Mathf.Clamp(planSize * mul, 14f, 150f);
            else
                camara.Distance = Mathf.Clamp(camara.Distance * mul, camara.MinDistance, camara.MaxDistance);
            snapped = false;
        }

        void Note(string text)
        {
            if (line == null || line.text == text) return;
            line.text = text;
            lineFade?.ShowAgain();
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

        void BuildGrove()
        {
            Slab("Suelo", new Vector3(0f, -0.06f, 90f), 720f, 580f, Sod, "piso_cesped", 6f);
            Slab("Sendero", new Vector3(0f, 0.02f, 2f), 6.2f, 28f, Path, "piso_tierra", 3.4f);
            Slab("SenderoLejos", new Vector3(0f, 0.02f, 62f), 6.2f, 60f, Path, "piso_tierra", 3.4f);
            BuildRiver();

            for (float z = 2f; z <= 44f; z += 9.2f)
            {
                for (float x = -64f; x <= 64f; x += 8.8f)
                {
                    if (Mathf.Abs(x) < 5.6f) continue;
                    if (z > 29f) continue;
                    if (InRiver(x, z) || (z > 12f && z < 28f)) continue;
                    float jx = x + Range(-0.8f, 0.8f);
                    float jz = z + Range(-0.8f, 0.8f);
                    bool pine = Next() < 0.62f;
                    string kind = pine ? Pick(Pines) : Pick(Broad);
                    float height = pine ? Range(13f, 19f) : Range(10f, 15f);
                    var tree = Place(kind, new Vector3(jx, 0f, jz), height, pine ? 1.2f : 1.3f, Range(0f, 360f));
                    if (tree == null) continue;
                    planted++;
                    float width = Span(tree);
                    trunks.Add(new Trunk
                    {
                        X = jx,
                        Z = jz,
                        Radius = Mathf.Clamp(width * 0.16f, 0.55f, 2.1f),
                    });
                }
            }

            for (int i = 0; i < 70; i++)
            {
                float x = Range(-66f, 66f);
                float z = Range(MapMinZ + 4f, MapMaxZ - 4f);
                if (Mathf.Abs(x) < 4.2f) x += x < 0f ? -4.4f : 4.4f;
                if (z > 12f && z < 28f) continue;
                Place(Pick(Brush), new Vector3(x, 0f, z), Range(1.7f, 2.7f), 1.35f, Range(0f, 360f));
            }

            for (int i = 0; i < 36; i++)
            {
                float x = Range(-64f, 64f);
                float z = Range(MapMinZ + 4f, MapMaxZ - 6f);
                if (Mathf.Abs(x) < 2.2f) continue;
                if (z > 12f && z < 28f) continue;
                var rock = Place(Pick(Stones), new Vector3(x, 0f, z), Range(1.3f, 3.4f), 1.25f, Range(0f, 360f));
                if (rock == null) continue;
                trunks.Add(new Trunk { X = x, Z = z, Radius = Mathf.Clamp(Span(rock) * 0.34f, 0.45f, 1.8f) });
            }

            for (int i = 0; i < 140; i++)
            {
                float tx = Range(-68f, 68f);
                float tz = Range(MapMinZ + 2f, MapMaxZ - 4f);
                if (tz > 12f && tz < 28f) continue;
                string tuft = Next() < 0.5f ? "grass" : (Next() < 0.5f ? "grass_large" : "grass_leafs");
                Place(tuft, new Vector3(tx, 0f, tz), Range(0.8f, 1.5f), 1.2f, Range(0f, 360f));
            }

            string[] blooms = { "flower_redA", "flower_redC", "flower_yellowA", "flower_purpleA", "mushroom_red" };
            for (int i = 0; i < 28; i++)
            {
                float bx = Range(-40f, 40f);
                float bz = Range(MapMinZ + 6f, 14f);
                if (bz > 12f && bz < 28f) continue;
                Place(Pick(blooms), new Vector3(bx, 0f, bz), Range(0.7f, 1.15f), 1f, Range(0f, 360f));
            }

            PlantPines();
            PlantBerries();

            Place("log", new Vector3(-4.6f, 0f, 4f), 0.7f, 1f, 70f);
            Place("log_large", new Vector3(5.2f, 0f, 8f), 0.9f, 1f, -20f);
            Place("stump_round", new Vector3(-6.4f, 0f, 31f), 1.1f, 1f, 15f);
            Place("stump_old", new Vector3(6.8f, 0f, -2f), 1.3f, 1f, 40f);
        }

        static bool InRiver(float x, float z)
        {
            return Mathf.Abs(x) <= RiverHalfX && z >= RiverMinZ && z <= RiverMaxZ;
        }

        static float BankY(float z)
        {
            if (z >= 11f && z < RiverMinZ)
                return Mathf.Lerp(0.2f, 1.62f, Mathf.InverseLerp(11f, 15.2f, Mathf.Clamp(z, 11f, 15.2f)));
            return 0.2f;
        }

        static void Kill(Object target)
        {
            if (target == null) return;
            if (Application.isPlaying) Destroy(target);
            else DestroyImmediate(target);
        }

        void BuildRiver()
        {
            var mud = new Color(0.40f, 0.28f, 0.16f);
            var rock = new Color(0.55f, 0.50f, 0.42f);
            float span = 720f;
            Slab("RampaBaja", new Vector3(0f, 0.45f, 12.1f), span, 2.2f, mud, "piso_barro", 4f);
            Slab("RampaMedia", new Vector3(0f, 0.95f, 13.6f), span, 2f, mud, "piso_barro", 4f);
            Slab("Orilla", new Vector3(0f, 1.62f, 15.15f), span, 2.4f, mud, "piso_barro", 4f);
            Slab("Ribera", new Vector3(0f, -0.2f, 15.55f), span, 1.1f, rock, "piso_piedra", 3f);
            Slab("RiberaLejos", new Vector3(0f, -0.15f, 25.7f), span, 1.2f, rock, "piso_piedra", 3f);

            var host = new GameObject("Rio");
            host.transform.SetParent(grove, false);
            host.transform.localPosition = new Vector3(0f, WaterY, (RiverMinZ + RiverMaxZ) * 0.5f);
            var filter = host.AddComponent<MeshFilter>();
            filter.sharedMesh = RiverSheet(span, RiverMaxZ - RiverMinZ + 0.4f, 320, 8);
            var renderer = host.AddComponent<MeshRenderer>();
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            var shader = Shader.Find("AYNI/Rio") ?? Shader.Find("Standard") ?? Shader.Find("Sprites/Default");
            if (shader != null)
            {
                var mat = new Material(shader);
                mat.color = new Color(0.92f, 1f, 0.96f, 1f);
                var tex = Resources.Load<Texture2D>("Agua/rio");
                if (tex == null) tex = Resources.Load<Texture2D>("Bosque/rio");
                if (tex != null)
                {
                    tex.wrapMode = TextureWrapMode.Repeat;
                    mat.mainTexture = tex;
                    mat.mainTextureScale = new Vector2(span / 18f, 2.2f);
                }

                renderer.sharedMaterial = mat;
            }
        }

        static Mesh RiverSheet(float width, float depth, int columns, int rows)
        {
            var mesh = new Mesh { name = "Rio" };
            int vx = columns + 1;
            int vz = rows + 1;
            var verts = new Vector3[vx * vz];
            var uvs = new Vector2[verts.Length];
            var tris = new int[columns * rows * 6];
            float hx = width * 0.5f;
            float hz = depth * 0.5f;
            for (int z = 0; z < vz; z++)
            {
                for (int x = 0; x < vx; x++)
                {
                    int i = z * vx + x;
                    float u = x / (float)columns;
                    float v = z / (float)rows;
                    verts[i] = new Vector3(Mathf.Lerp(-hx, hx, u), 0f, Mathf.Lerp(-hz, hz, v));
                    uvs[i] = new Vector2(u, v);
                }
            }

            int t = 0;
            for (int z = 0; z < rows; z++)
            {
                for (int x = 0; x < columns; x++)
                {
                    int i = z * vx + x;
                    tris[t++] = i;
                    tris[t++] = i + vx;
                    tris[t++] = i + 1;
                    tris[t++] = i + 1;
                    tris[t++] = i + vx;
                    tris[t++] = i + vx + 1;
                }
            }

            mesh.vertices = verts;
            mesh.uv = uvs;
            mesh.triangles = tris;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        void Slab(string name, Vector3 local, float width, float depth, Color color, string texName = null, float tile = 6f)
        {
            var host = new GameObject(name);
            host.transform.SetParent(grove, false);
            host.transform.localPosition = local;
            var tex = string.IsNullOrEmpty(texName)
                ? null
                : Resources.Load<Texture2D>("Bosque/" + texName);
            var tint = tex != null ? Color.white : color;
            var filter = host.AddComponent<MeshFilter>();
            filter.sharedMesh = Flat(name, width, depth, tint);
            var renderer = host.AddComponent<MeshRenderer>();
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            var shader = Shader.Find("AYNI/Calle") ?? Shader.Find("Standard") ?? Shader.Find("Sprites/Default");
            if (shader != null)
            {
                var mat = new Material(shader);
                if (tex != null)
                {
                    // El suelo es enorme: se repite la textura cada "tile" metros
                    // en vez de estirarla sobre toda la pradera.
                    mat.mainTexture = tex;
                    mat.mainTextureScale = new Vector2(width / Mathf.Max(0.5f, tile), depth / Mathf.Max(0.5f, tile));
                }

                renderer.sharedMaterial = mat;
            }
        }

        static Mesh Flat(string meshName, float width, float depth, Color color)
        {
            var mesh = new Mesh { name = meshName };
            float hx = width * 0.5f;
            float hz = depth * 0.5f;
            mesh.vertices = new[]
            {
                new Vector3(-hx, 0f, -hz),
                new Vector3(-hx, 0f, hz),
                new Vector3(hx, 0f, hz),
                new Vector3(hx, 0f, -hz),
            };
            mesh.colors = new[] { color, color, color, color };
            mesh.normals = new[] { Vector3.up, Vector3.up, Vector3.up, Vector3.up };
            mesh.uv = new[]
            {
                new Vector2(0f, 0f),
                new Vector2(0f, 1f),
                new Vector2(1f, 1f),
                new Vector2(1f, 0f),
            };
            mesh.triangles = new[] { 0, 1, 2, 0, 2, 3 };
            mesh.RecalculateBounds();
            return mesh;
        }

        void PlantPines()
        {
            var clearing = new Vector3(0f, 0f, 64f);
            for (float z = 30.5f; z <= 90f; z += 8.2f)
            {
                for (float x = -64f; x <= 64f; x += 8.4f)
                {
                    float jx = x + Range(-0.7f, 0.7f);
                    float jz = z + Range(-0.7f, 0.7f);
                    if (Mathf.Abs(jx) < 3.6f) continue;
                    float dx = jx - clearing.x;
                    float dz = jz - clearing.z;
                    if (dx * dx + dz * dz < 6.4f * 6.4f) continue;
                    string kind = Pick(ConiferTrees);
                    float height = kind.Contains("large") ? Range(16f, 22f)
                        : kind.Contains("small") ? Range(6.5f, 9f)
                        : kind.Contains("medium") ? Range(11f, 15f)
                        : Range(13f, 17f);
                    var tree = PlaceResource(kind, new Vector3(jx, 0f, jz), height, 1.15f, Range(0f, 360f));
                    if (tree == null) continue;
                    planted++;
                    float width = Span(tree);
                    trunks.Add(new Trunk
                    {
                        X = jx,
                        Z = jz,
                        Radius = Mathf.Clamp(width * 0.16f, 0.7f, 2.4f),
                    });
                }
            }

            for (int i = 0; i < 48; i++)
            {
                float x = Range(-64f, 64f);
                float z = Range(31f, 90f);
                if (Mathf.Abs(x) < 2.4f) continue;
                PlaceResource(Pick(ConiferBrush), new Vector3(x, 0f, z), Range(0.9f, 1.8f), 1.3f, Range(0f, 360f));
            }
        }

        void PlantBerries()
        {
            Vector3[] spots =
            {
                new Vector3(-16f, 0.35f, 46f),
                new Vector3(18f, 0.35f, 52f),
                new Vector3(-34f, 0.35f, 64f),
                new Vector3(36f, 0.35f, 70f),
                new Vector3(-12f, 0.35f, 80f),
                new Vector3(14f, 0.35f, 86f),
            };
            var shader = Shader.Find("Standard") ?? Shader.Find("AYNI/Bosque") ?? Shader.Find("Sprites/Default");
            var glow = new Color(0.62f, 0.16f, 0.95f);
            for (int s = 0; s < spots.Length; s++)
            {
                Vector3 spot = ClearOfTrunks(spots[s]);
                var host = new GameObject("BayaDeAura");
                host.transform.SetParent(grove, false);
                host.transform.localPosition = spot;
                for (int i = 0; i < 3; i++)
                {
                    var ball = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    ball.name = "Fruto";
                    var body = ball.GetComponent<Collider>();
                    if (body != null) Kill(body);
                    ball.transform.SetParent(host.transform, false);
                    ball.transform.localPosition = new Vector3((i - 1) * 0.16f, 0.2f + i * 0.04f, i == 1 ? 0.06f : 0f);
                    ball.transform.localScale = Vector3.one * 0.22f;
                    var renderer = ball.GetComponent<Renderer>();
                    if (shader == null || renderer == null) continue;
                    var mat = new Material(shader);
                    if (mat.HasProperty("_Color")) mat.SetColor("_Color", glow);
                    if (mat.HasProperty("_EmissionColor"))
                    {
                        mat.EnableKeyword("_EMISSION");
                        mat.SetColor("_EmissionColor", glow * 1.35f);
                    }
                    renderer.sharedMaterial = mat;
                    renderer.shadowCastingMode = ShadowCastingMode.Off;
                }

                PlaceResource(Pick(ConiferBrush), spot + new Vector3(0.35f, 0f, 0.2f), 1.1f, 1.2f, Range(0f, 360f));
                berries.Add(host.transform);
            }
        }

        Vector3 ClearOfTrunks(Vector3 spot)
        {
            var home = new Vector3(0f, spot.y, 64f);
            for (int n = 0; n < 8; n++)
            {
                if (OpenGround(spot.x, spot.z)) return spot;
                spot = Vector3.Lerp(spot, home, 0.4f);
            }

            return home;
        }

        void BobBerries()
        {
            for (int i = 0; i < berries.Count; i++)
            {
                var berry = berries[i];
                if (berry == null) continue;
                var at = berry.localPosition;
                at.y = 0.35f + Mathf.Sin(Time.time * 2.4f + i) * 0.05f;
                berry.localPosition = at;
            }
        }

        void GatherBerries()
        {
            var hero = Hero();
            if (hero == null || line == null) return;
            if (!toldGrove && hero.Local.z > 29f && berriesFound < berries.Count)
            {
                toldGrove = true;
                Note(LeyDelMonte.ForestGrove);
                battle?.Contar("LA MINA", LeyDelMonte.ForestMuqui);
            }

            for (int i = 0; i < berries.Count; i++)
            {
                var berry = berries[i];
                if (berry == null) continue;
                float dx = hero.Local.x - berry.localPosition.x;
                float dz = hero.Local.z - berry.localPosition.z;
                if (dx * dx + dz * dz > 1.35f * 1.35f) continue;
                berriesFound++;
                Destroy(berry.gameObject);
                berries[i] = null;
                battle?.TasteAuraBerry();
                Note(berriesFound >= berries.Count
                    ? LeyDelMonte.ForestBerriesDone
                    : LeyDelMonte.ForestBerry + "  " + berriesFound + " de " + berries.Count);
            }
        }

        GameObject Place(string name, Vector3 local, float targetHeight, float widen, float yaw)
        {
            return PlaceResource("Bosque/" + name, local, targetHeight, widen, yaw);
        }

        GameObject PlaceResource(string resourcePath, Vector3 local, float targetHeight, float widen, float yaw)
        {
            var prefab = Resources.Load<GameObject>(resourcePath);
            if (prefab == null) return null;
            var go = Instantiate(prefab, grove);
            go.name = resourcePath;
            go.transform.localPosition = local;
            go.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
            go.transform.localScale = Vector3.one;
            float native = NativeHeight(resourcePath, go);
            float mul = targetHeight / Mathf.Max(0.05f, native);
            go.transform.localScale = new Vector3(mul * widen, mul, mul * widen);
            Wake(go);
            return go;
        }

        float NativeHeight(string name, GameObject go)
        {
            if (heightAtOne.TryGetValue(name, out float cached)) return cached;
            float h = 1f;
            var renders = go.GetComponentsInChildren<Renderer>();
            if (renders.Length > 0)
            {
                Bounds box = renders[0].bounds;
                for (int i = 1; i < renders.Length; i++) box.Encapsulate(renders[i].bounds);
                h = Mathf.Max(0.05f, box.size.y);
            }

            heightAtOne[name] = h;
            return h;
        }

        static float Span(GameObject go)
        {
            var renders = go.GetComponentsInChildren<Renderer>();
            if (renders.Length == 0) return 1f;
            Bounds box = renders[0].bounds;
            for (int i = 1; i < renders.Length; i++) box.Encapsulate(renders[i].bounds);
            return Mathf.Max(box.size.x, box.size.z);
        }

        static readonly Dictionary<string, Color> Palette = new Dictionary<string, Color>
        {
            // Los FBX de los kits traen colores incrustados que salen cian
            // (hojas) y rosados (troncos). Se corrigen a verde y madera.
            { "leafsGreen", new Color(0.30f, 0.52f, 0.22f) },
            { "leafsDark", new Color(0.22f, 0.40f, 0.16f) },
            { "grass", new Color(0.38f, 0.62f, 0.26f) },
            { "woodBark", new Color(0.52f, 0.36f, 0.22f) },
            { "woodBarkDark", new Color(0.42f, 0.28f, 0.17f) },
            { "woodInner", new Color(0.80f, 0.66f, 0.48f) },
            { "dirt", new Color(0.45f, 0.32f, 0.19f) },
        };

        static readonly Dictionary<Material, Material> paletteMaterials = new Dictionary<Material, Material>();

        static Material Repaint(Material source, Color color)
        {
            if (paletteMaterials.TryGetValue(source, out var painted)) return painted;

            painted = new Material(source)
            {
                name = source.name,
                color = color,
            };
            paletteMaterials[source] = painted;
            return painted;
        }

        static void Wake(GameObject go)
        {
            var fallback = Shader.Find("Standard") ?? Shader.Find("AYNI/Bosque") ?? Shader.Find("Sprites/Default");
            var renders = go.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renders.Length; i++)
            {
                renders[i].shadowCastingMode = ShadowCastingMode.Off;
                renders[i].receiveShadows = false;
                var mats = renders[i].sharedMaterials;
                var copy = new Material[mats.Length];
                bool changed = false;
                for (int m = 0; m < mats.Length; m++)
                {
                    var mat = mats[m];
                    copy[m] = mat;
                    if (mat == null) continue;

                    if (Palette.TryGetValue(mat.name, out var paletteTint))
                    {
                        copy[m] = Repaint(mat, paletteTint);
                        changed = true;
                        continue;
                    }

                    bool broken = mat.shader == null
                        || mat.shader.name.IndexOf("Error", System.StringComparison.Ordinal) >= 0
                        || mat.shader.name.IndexOf("Universal", System.StringComparison.Ordinal) >= 0;
                    if (!broken || fallback == null) continue;

                    var neu = new Material(fallback);
                    Texture tex = mat.mainTexture;
                    if (tex == null && mat.HasProperty("_BaseMap")) tex = mat.GetTexture("_BaseMap");
                    if (tex != null && neu.HasProperty("_MainTex")) neu.mainTexture = tex;

                    Color tint = mat.HasProperty("_Color") ? mat.GetColor("_Color") : Color.white;
                    if (mat.HasProperty("_BaseColor")) tint = mat.GetColor("_BaseColor");
                    if (neu.HasProperty("_Color")) neu.SetColor("_Color", tint);

                    if (mat.HasProperty("_Cutoff") && neu.HasProperty("_Cutoff"))
                        neu.SetFloat("_Cutoff", mat.GetFloat("_Cutoff"));

                    copy[m] = neu;
                    changed = true;
                }

                if (changed) renders[i].sharedMaterials = copy;
            }
        }

        string Pick(string[] names) => names[(int)Next(names.Length)];

        float Range(float a, float b) => a + (float)rng.NextDouble() * (b - a);

        double Next() => rng.NextDouble();

        double Next(int count) => rng.Next(count);
    }
}
