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
    /// Capítulo fidedigno del Valle de Cajamarca con la topografía real,
    /// Cerro Santa Apolonia, mirador panorámico, escalinatas de piedra y
    /// la paleta de iluminación violeta con cielo azul andino.
    /// </summary>
    public class CajamarcaRealCapitulo : MonoBehaviour
    {
        public enum Vista
        {
            Inclinacion,
            Mirador,
            Planta,
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

        struct Air
        {
            public bool Fog;
            public FogMode Mode;
            public Color FogColor;
            public float FogStart;
            public float FogEnd;
            public float FogDensity;
            public AmbientMode AmbientMode;
            public Color AmbientSky;
            public Color AmbientEquator;
            public Color AmbientGround;
            public Color AmbientLight;
        }

        sealed class Walker
        {
            public Transform Body;
            public SpriteFrameAnimator Anim;
            public Vector3 Local;
            public Vector3 Goal;
            public Vector3 Home;
            public float Wait;
            public float Detour;
            public bool Hero;
            public bool Chase;
            public float Vy;
            public Vector3 Push;
            public bool Grounded = true;
            public bool Fell;
        }

        struct HudBit
        {
            public GameObject Go;
            public bool WasOn;
        }

        // Paleta de iluminación solicitada por el usuario
        public static readonly Color ColorHighlight = new Color(0.886f, 0.808f, 0.965f); // #E2CEF6
        public static readonly Color ColorOrchid    = new Color(0.851f, 0.502f, 0.976f); // #D980F9
        public static readonly Color ColorPurple    = new Color(0.686f, 0.314f, 0.898f); // #AF50E5
        public static readonly Color ColorViolet    = new Color(0.541f, 0.043f, 0.824f); // #8A0BD2
        public static readonly Color ColorShadow    = new Color(0.357f, 0.024f, 0.447f); // #5B0672
        public static readonly Color SkyBlue        = new Color(0.12f, 0.44f, 0.86f);    // Cielo azul andino

        static CajamarcaRealCapitulo current;
        public static bool IsOpen => current != null;

        readonly List<Walker> walkers = new List<Walker>();
        readonly List<HudBit> hudBits = new List<HudBit>();
        LagunaBattleManager battle;
        Transform district;
        AyniBakedCajamarcaReal bakedCity;
        Transform dynamicRoot;
        GameObject plan;
        Transform gallinaBody;
        GameObject hud;
        Camera cam;
        Pose savedPose;
        Air savedAir;
        bool hasSaved;
        Vista vista = Vista.Inclinacion;
        bool snapped;
        bool toldSummit;
        bool allowJump = true;
        bool jumpLatch = true;
        float safeUntil;
        Text aviso;
        TituloFugaz avisoFade;
        float planSize = 120f;
        BotonesDeMovimiento movimientos;
        bool planDetached;
        bool planDrag;
        Vector2 planLook;
        Vector3 planCursor;
        readonly CamaraTercera camara = new CamaraTercera
        {
            Pitch = 22f,
            Distance = 15f,
            MinDistance = 7f,
            MaxDistance = 36f,
            PivotHeight = 2.4f,
        };

        // Puntos geográficos reales
        readonly Vector3 PlazaCenter = Vector3.zero;
        readonly Vector3 SantaApoloniaSummit = new Vector3(-184f, 76f, -386f);

        public static void Begin(LagunaBattleManager battle)
        {
            if (current != null || battle == null || CajamarcaCalles.IsOpen || BosqueCapitulo.IsOpen)
                return;

            var go = new GameObject("CajamarcaRealCapitulo");
            current = go.AddComponent<CajamarcaRealCapitulo>();
            current.Open(battle);
        }

        public static void CloseCurrent()
        {
            if (current != null) current.Close();
        }

        void Open(LagunaBattleManager battle)
        {
            this.battle = battle;
            PalabrasCartilla.Close();
            if (battle.startScreen != null) battle.startScreen.SetActive(false);
            HideBattleHud();
            cam = Camera.main;
            SaveEnvironment();

            bakedCity = FindBakedCity();
            district = bakedCity != null
                ? bakedCity.transform.Find(AyniBakedCajamarcaReal.DistrictName)
                : null;

            if (district != null)
            {
                bakedCity.gameObject.SetActive(true);
                plan = district.Find(AyniBakedCajamarcaReal.PlanName)?.gameObject;
            }

            if (district == null)
            {
                // Fallback runtime si no estuviera horneado
                district = new GameObject("Distrito").transform;
                district.SetParent(transform, false);
                district.position = new Vector3(4500f, 0f, 0f);
            }

            dynamicRoot = new GameObject("__DINAMICO__").transform;
            dynamicRoot.SetParent(district, false);

            ApplyAtmosphere();
            var cerro = district.Find(AyniBakedCajamarcaReal.SantaApoloniaName);
            var capillaReal = cerro != null ? cerro.Find(AyniBakedCajamarcaReal.CapillaName) : null;
            if (capillaReal != null) CajamarcaCalles.CambiarCapilla(capillaReal, 12.6f, 6.05f);
            SpawnActors();
            LluviaDeAlmas.Begin(dynamicRoot, walkers.Count > 0 ? walkers[0].Body : null,
                (x, z) => SampleHeight(x, z) + 0.15f, 0.35f);
            SolidoDelCamino.Armar(district);
            BuildHud();
            snapped = false;
            toldSummit = false;
            battle?.Contar(LeyDelMonte.CajamarcaRealKicker, LeyDelMonte.ValleArrive);
        }

        void SaveEnvironment()
        {
            if (cam == null || hasSaved) return;

            savedPose = new Pose
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

            savedAir = new Air
            {
                Fog = RenderSettings.fog,
                Mode = RenderSettings.fogMode,
                FogColor = RenderSettings.fogColor,
                FogStart = RenderSettings.fogStartDistance,
                FogEnd = RenderSettings.fogEndDistance,
                FogDensity = RenderSettings.fogDensity,
                AmbientMode = RenderSettings.ambientMode,
                AmbientSky = RenderSettings.ambientSkyColor,
                AmbientEquator = RenderSettings.ambientEquatorColor,
                AmbientGround = RenderSettings.ambientGroundColor,
                AmbientLight = RenderSettings.ambientLight,
            };

            hasSaved = true;
        }

        void ApplyAtmosphere()
        {
            if (cam != null)
            {
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = SkyBlue;
            }

            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = Color.Lerp(SkyBlue, ColorPurple, 0.40f);
            RenderSettings.fogStartDistance = 140f;
            RenderSettings.fogEndDistance = 2200f;

            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = ColorOrchid;
            RenderSettings.ambientEquatorColor = ColorPurple;
            RenderSettings.ambientGroundColor = ColorShadow;
        }

        void RestoreEnvironment()
        {
            if (!hasSaved || cam == null) return;

            cam.transform.position = savedPose.Position;
            cam.transform.rotation = savedPose.Rotation;
            cam.orthographic = savedPose.Orthographic;
            cam.orthographicSize = savedPose.Size;
            cam.fieldOfView = savedPose.Field;
            cam.nearClipPlane = savedPose.Near;
            cam.farClipPlane = savedPose.Far;
            cam.backgroundColor = savedPose.Background;
            cam.clearFlags = savedPose.Clear;

            RenderSettings.fog = savedAir.Fog;
            RenderSettings.fogMode = savedAir.Mode;
            RenderSettings.fogColor = savedAir.FogColor;
            RenderSettings.fogStartDistance = savedAir.FogStart;
            RenderSettings.fogEndDistance = savedAir.FogEnd;
            RenderSettings.fogDensity = savedAir.FogDensity;
            RenderSettings.ambientMode = savedAir.AmbientMode;
            RenderSettings.ambientSkyColor = savedAir.AmbientSky;
            RenderSettings.ambientEquatorColor = savedAir.AmbientEquator;
            RenderSettings.ambientGroundColor = savedAir.AmbientGround;
            RenderSettings.ambientLight = savedAir.AmbientLight;

            hasSaved = false;
        }

        void Close()
        {
            if (current != this) return;
            CamaraTercera.Release();

            RestoreEnvironment();
            RestoreBattleHud();

            if (dynamicRoot != null) Destroy(dynamicRoot.gameObject);
            if (bakedCity != null) bakedCity.gameObject.SetActive(false);
            if (hud != null) Destroy(hud);

            current = null;
            if (battle != null && battle.startScreen != null)
                battle.startScreen.SetActive(true);

            Destroy(gameObject);
        }

        void Update()
        {
            if (CaminoRelato.Reading)
            {
                jumpLatch = false;
                CamaraTercera.ShowCursor();
                return;
            }

            allowJump = jumpLatch;
            jumpLatch = true;
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                Close();
                return;
            }

            if (Input.GetKeyDown(KeyCode.V))
                CycleVista();
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
                ReadPlan();
            }
            else if (vista == Vista.Inclinacion)
                camara.ReadMouse();
            else
                CamaraTercera.Release();

            DriveActors();
            WatchSummit();
            UpdateCamera();
            BrujulaDelMapa.Rumbo = vista == Vista.Planta ? CamaraTercera.PlanYaw : camara.Yaw;
            BrujulaDelMapa.Mostrar = vista != Vista.Mirador;
            movimientos?.Marcar(vista == Vista.Planta, vista == Vista.Inclinacion);
            for (int i = 0; i < walkers.Count; i++)
                Aim(walkers[i].Body);
        }

        void CycleVista()
        {
            ElegirVista((Vista)(((int)vista + 1) % 3));
        }

        void ElegirVista(Vista next)
        {
            if (vista == next) return;
            if (next == Vista.Planta)
                CamaraTercera.PlanYaw = camara.Yaw;
            else if (vista == Vista.Planta)
                camara.Yaw = CamaraTercera.PlanYaw;
            vista = next;
            snapped = false;
            if (aviso == null) return;
            aviso.text = vista == Vista.Planta ? LeyDelMonte.MapaAyuda : LeyDelMonte.CajamarcaRealWalk;
            avisoFade?.ShowAgain();
        }

        void ReadPlan()
        {
            if (cam == null || district == null) return;
            float wheel = Input.mouseScrollDelta.y;
            if (Input.GetKey(KeyCode.Equals) || Input.GetKey(KeyCode.KeypadPlus)) wheel += 0.8f * Time.deltaTime * 8f;
            if (Input.GetKey(KeyCode.Minus) || Input.GetKey(KeyCode.KeypadMinus)) wheel -= 0.8f * Time.deltaTime * 8f;
            if (wheel > 0.01f) planSize = Mathf.Max(18f, planSize * (1f - 0.18f * Mathf.Min(wheel, 3f)));
            if (wheel < -0.01f) planSize = Mathf.Min(480f, planSize / (1f - 0.18f * Mathf.Min(-wheel, 3f)));

            bool overUi = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
            if (Input.GetMouseButtonDown(1) && !overUi)
            {
                if (!planDetached && walkers.Count > 0)
                    planLook = new Vector2(walkers[0].Local.x, walkers[0].Local.z);
                planDetached = true;
                planDrag = true;
                planCursor = Input.mousePosition;
            }

            if (!Input.GetMouseButton(1)) planDrag = false;
            if (!planDrag) return;
            Vector3 now = Input.mousePosition;
            Vector3 delta = now - planCursor;
            planCursor = now;
            Vector3 right = cam.transform.right;
            Vector3 up = cam.transform.up;
            right.y = 0f;
            up.y = 0f;
            if (right.sqrMagnitude < 0.001f || up.sqrMagnitude < 0.001f) return;
            right.Normalize();
            up.Normalize();
            float units = (planSize * 2f) / Mathf.Max(1f, Screen.height);
            Vector3 move = district.InverseTransformDirection((-delta.x * right - delta.y * up) * units);
            planLook.x += move.x;
            planLook.y += move.z;
        }

        void ZoomPlan(float mul)
        {
            if (vista != Vista.Planta)
            {
                CamaraTercera.PlanYaw = camara.Yaw;
                vista = Vista.Planta;
                if (aviso != null) aviso.text = LeyDelMonte.MapaAyuda;
                avisoFade?.ShowAgain();
            }

            planSize = Mathf.Clamp(planSize * mul, 18f, 480f);
            snapped = false;
        }

        void DriveActors()
        {
            if (walkers.Count == 0) return;
            var hero = walkers[0];
            if (!hero.Grounded)
                FlyValley(hero);
            else
                WalkValley(hero);

            for (int i = 1; i < walkers.Count; i++)
                PaceValley(walkers[i], hero);
            HuntValley(hero);
        }

        void WalkValley(Walker hero)
        {
            Vector3 localMove;
            Vector3 worldMove;
            if (vista == Vista.Inclinacion)
            {
                worldMove = camara.Wish();
                localMove = camara.ToLocal(district, worldMove);
            }
            else if (vista == Vista.Planta)
            {
                localMove = camara.PlanLocal(district);
                worldMove = CamaraTercera.WorldDir(district, localMove);
            }
            else
            {
                localMove = Vector3.zero;
                if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) localMove.x -= 1f;
                if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) localMove.x += 1f;
                if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) localMove.z += 1f;
                if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) localMove.z -= 1f;
                if (localMove.sqrMagnitude > 1f) localMove.Normalize();
                worldMove = CamaraTercera.WorldDir(district, localMove);
            }

            if (allowJump && Input.GetKeyDown(KeyCode.Space))
            {
                hero.Grounded = false;
                hero.Fell = false;
                hero.Vy = 12f;
                hero.Push = localMove.sqrMagnitude > 0.01f ? localMove.normalized * 16f : Vector3.zero;
                hero.Anim?.PlayJump();
                return;
            }

            if (localMove.sqrMagnitude > 0.01f)
            {
                Vector3 step = localMove * (16f * Time.deltaTime);
                Vector3 next = hero.Local + step;
                if (!Sale(hero.Local, next))
                {
                    var ejeX = hero.Local + new Vector3(step.x, 0f, 0f);
                    var ejeZ = hero.Local + new Vector3(0f, 0f, step.z);
                    if (Sale(hero.Local, ejeX)) next = ejeX;
                    else if (Sale(hero.Local, ejeZ)) next = ejeZ;
                    else next = hero.Local;
                }

                bool moved = (next - hero.Local).sqrMagnitude > 0.0001f;
                hero.Local = next;
                hero.Anim?.PlayStep(worldMove, moved);
            }
            else
            {
                hero.Anim?.PlayStep(Vector3.zero, false);
            }

            hero.Local.y = SampleHeight(hero.Local.x, hero.Local.z);
            if (hero.Body != null)
                hero.Body.localPosition = hero.Local;
        }

        void FlyValley(Walker hero)
        {
            hero.Vy -= 26f * Time.deltaTime;
            Vector3 next = hero.Local + hero.Push * Time.deltaTime;
            if (!Sale(hero.Local, new Vector3(next.x, hero.Local.y, next.z)))
            {
                next.x = hero.Local.x;
                next.z = hero.Local.z;
            }

            next.y = hero.Local.y + hero.Vy * Time.deltaTime;
            float ground = SampleHeight(next.x, next.z);
            bool hit = next.y <= ground && (hero.Vy <= 0f || ground > hero.Local.y + 0.05f);
            if (hit)
            {
                hero.Local = new Vector3(next.x, ground, next.z);
                hero.Grounded = true;
                hero.Vy = 0f;
                hero.Push = Vector3.zero;
                hero.Fell = false;
                hero.Anim?.PlayStep(Vector3.zero, false);
            }
            else
            {
                hero.Local = next;
                if (hero.Vy < -1.4f && !hero.Fell)
                {
                    hero.Fell = true;
                    hero.Anim?.PlayFall();
                }
                else if (!hero.Fell)
                    hero.Anim?.PlayLocomotion(false, true);
            }

            if (hero.Body != null)
                hero.Body.localPosition = hero.Local;
        }

        void PaceValley(Walker walker, Walker hero)
        {
            if (walker.Body == null) return;
            if (walker.Chase)
            {
                walker.Detour -= Time.deltaTime;
                if (walker.Detour <= 0f)
                    walker.Goal = hero.Local;
            }

            walker.Wait -= Time.deltaTime;
            Vector3 to = walker.Goal - walker.Local;
            to.y = 0f;
            if (!walker.Chase && (to.magnitude < 1.2f || walker.Wait < -2f))
            {
                walker.Goal = ValleyPoint(walker.Home);
                walker.Wait = Random.Range(0.4f, 1.4f);
                walker.Anim?.PlayStep(Vector3.zero, false);
                return;
            }

            if (to.sqrMagnitude < 0.05f)
            {
                walker.Anim?.PlayStep(Vector3.zero, false);
                return;
            }

            float speed = walker.Chase ? 9f : 3.4f;
            Vector3 step = to.normalized * speed * Time.deltaTime;
            if (step.magnitude > to.magnitude) step = to;
            Vector3 next = walker.Local + step;
            if (!Sale(walker.Local, next))
            {
                var ejeX = walker.Local + new Vector3(step.x, 0f, 0f);
                var ejeZ = walker.Local + new Vector3(0f, 0f, step.z);
                if (Sale(walker.Local, ejeX)) next = ejeX;
                else if (Sale(walker.Local, ejeZ)) next = ejeZ;
                else next = walker.Local;
            }

            walker.Local = next;
            walker.Local.y = SampleHeight(walker.Local.x, walker.Local.z) + 0.15f;
            walker.Body.localPosition = walker.Local;
            walker.Anim?.PlayStep(CamaraTercera.WorldDir(district, to.normalized), true);
        }

        void HuntValley(Walker hero)
        {
            if (Time.time < safeUntil || hero.Body == null || !hero.Grounded) return;
            for (int i = 1; i < walkers.Count; i++)
            {
                var hunter = walkers[i];
                if (!hunter.Chase) continue;
                Vector3 gap = hunter.Local - hero.Local;
                gap.y = 0f;
                if (gap.sqrMagnitude > 2.6f * 2.6f) continue;
                hero.Grounded = true;
                hero.Vy = 0f;
                hero.Push = Vector3.zero;
                hero.Fell = false;
                hero.Local = new Vector3(0f, SampleHeight(0f, 0f), 0f);
                hero.Body.localPosition = hero.Local;
                if (gap.sqrMagnitude > 0.01f)
                {
                    hunter.Local += gap.normalized * 14f;
                    hunter.Local.y = SampleHeight(hunter.Local.x, hunter.Local.z) + 0.15f;
                    if (hunter.Body != null) hunter.Body.localPosition = hunter.Local;
                }

                hunter.Detour = 0.9f;
                safeUntil = Time.time + 1.5f;
                if (aviso != null) aviso.text = LeyDelMonte.TeAlcanzo;
                return;
            }
        }

        Vector3 ValleyPoint(Vector3 around)
        {
            for (int i = 0; i < 8; i++)
            {
                float x = around.x + Random.Range(-28f, 28f);
                float z = around.z + Random.Range(-28f, 28f);
                if (x < -220f || x > 180f || z < -460f || z > 160f) continue;
                return new Vector3(x, 0f, z);
            }

            return around;
        }

        void SpawnCrowd()
        {
            var vecinos = BattleCast.Vecinos;
            for (int i = 0; i < vecinos.Length; i++)
            {
                float ang = i * 1.25f;
                var at = new Vector3(Mathf.Cos(ang) * (22f + i * 6f), 0f, Mathf.Sin(ang) * (18f + i * 5f));
                SpawnPerson("ValleVecino" + (i + 1), vecinos[i], at, false);
            }

            var cazas = BattleCast.Cazadores;
            for (int i = 0; i < cazas.Length; i++)
            {
                float ang = i * 1.3f + 0.6f;
                var at = new Vector3(Mathf.Cos(ang) * (48f + i * 8f), 0f, Mathf.Sin(ang) * (40f + i * 7f));
                SpawnPerson("ValleCaza" + (i + 1), cazas[i], at, true);
            }
        }

        void SpawnPerson(string name, CharacterSheet sheet, Vector3 at, bool chase)
        {
            at.y = SampleHeight(at.x, at.z) + 0.15f;
            var go = new GameObject(name);
            go.transform.SetParent(dynamicRoot, false);
            var ink = go.AddComponent<SpriteRenderer>();
            ink.shadowCastingMode = ShadowCastingMode.Off;
            ink.receiveShadows = false;
            ink.sortingOrder = 18;
            var anim = go.AddComponent<SpriteFrameAnimator>();
            BattleSprites.Apply(anim, sheet);
            float scale = BattleCast.BodyScale(sheet) * 0.35f;
            go.transform.localScale = new Vector3(Mathf.Abs(scale), Mathf.Abs(scale), 1f);
            go.transform.localPosition = at;
            anim.SetFacing(true);
            anim.PlayLocomotion(false, false);
            walkers.Add(new Walker
            {
                Body = go.transform,
                Anim = anim,
                Local = at,
                Goal = chase ? at : ValleyPoint(at),
                Home = at,
                Hero = false,
                Chase = chase,
            });
        }

        void UpdateCamera()
        {
            if (cam == null || district == null) return;

            Vector3 heroWorld = district.TransformPoint(walkers.Count > 0 ? walkers[0].Local : PlazaCenter);
            Vector3 targetPos;
            Quaternion targetRot;

            switch (vista)
            {
                case Vista.Mirador:
                    // Cámara panorámica fija en la cumbre del Mirador de Santa Apolonia
                    Vector3 miradorWorld = district.TransformPoint(SantaApoloniaSummit + new Vector3(0f, 6.5f, 0f));
                    targetPos = miradorWorld;
                    targetRot = Quaternion.LookRotation(heroWorld - miradorWorld, Vector3.up);
                    cam.orthographic = false;
                    cam.fieldOfView = 55f;
                    cam.nearClipPlane = 0.5f;
                    cam.farClipPlane = 3500f;
                    break;

                case Vista.Planta:
                    Vector3 focus = heroWorld;
                    if (planDetached)
                        focus = district.TransformPoint(new Vector3(planLook.x, SampleHeight(planLook.x, planLook.y), planLook.y));
                    targetPos = focus + Vector3.up * 450f;
                    targetRot = Quaternion.Euler(90f, CamaraTercera.PlanYaw, 0f);
                    cam.orthographic = true;
                    cam.orthographicSize = planSize;
                    cam.nearClipPlane = 1f;
                    cam.farClipPlane = 3500f;
                    break;

                case Vista.Inclinacion:
                default:
                    cam.orthographic = false;
                    cam.fieldOfView = 50f;
                    cam.nearClipPlane = 0.3f;
                    cam.farClipPlane = 2600f;
                    camara.Pose(heroWorld, out targetPos, out targetRot);
                    float ground = SampleHeightWorld(targetPos);
                    if (targetPos.y < ground + 2.2f)
                    {
                        targetPos.y = ground + 2.2f;
                        Vector3 pivot = heroWorld + Vector3.up * camara.PivotHeight;
                        Vector3 to = pivot - targetPos;
                        if (to.sqrMagnitude > 0.001f)
                            targetRot = Quaternion.LookRotation(to, Vector3.up);
                    }
                    break;
            }

            if (vista == Vista.Inclinacion)
            {
                Vector3 pivot = heroWorld + Vector3.up * camara.PivotHeight;
                SolidoDelCamino.Encuadrar(cam, pivot, targetPos, !snapped);
                snapped = true;
                return;
            }

            if (!snapped)
            {
                cam.transform.SetPositionAndRotation(targetPos, targetRot);
                snapped = true;
            }
            else if (vista == Vista.Planta)
            {
                float t = 1f - Mathf.Exp(-6f * Time.deltaTime);
                cam.transform.position = Vector3.Lerp(cam.transform.position, targetPos, t);
                cam.transform.rotation = targetRot;
            }
            else
            {
                float t = 1f - Mathf.Exp(-6f * Time.deltaTime);
                cam.transform.position = Vector3.Lerp(cam.transform.position, targetPos, t);
                cam.transform.rotation = Quaternion.Slerp(cam.transform.rotation, targetRot, t);
            }
        }

        bool Sale(Vector3 desde, Vector3 hasta)
        {
            if (district == null) return true;
            return SolidoDelCamino.Puede(district.TransformPoint(desde), district.TransformPoint(hasta), 0.55f, 1.55f);
        }

        void Aim(Transform body)
        {
            if (body == null || cam == null) return;
            if (vista == Vista.Planta)
            {
                body.rotation = cam.transform.rotation;
                return;
            }
            Vector3 forward = cam.transform.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude > 0.001f)
                body.rotation = Quaternion.LookRotation(forward.normalized, Vector3.up);
        }

        float SampleHeight(float lx, float lz)
        {
            return CajamarcaAltura.Sample(lx, lz);
        }

        float SampleHeightWorld(Vector3 worldPos)
        {
            if (district == null) return 0f;
            Vector3 local = district.InverseTransformPoint(worldPos);
            return district.TransformPoint(new Vector3(local.x, CajamarcaAltura.Sample(local.x, local.z), local.z)).y;
        }

        void SpawnActors()
        {
            walkers.Clear();
            var heroGo = new GameObject("AtoqGallina");
            heroGo.transform.SetParent(dynamicRoot, false);
            heroGo.transform.localPosition = new Vector3(0f, SampleHeight(0f, 0f) + 0.1f, 0f);

            var spr = heroGo.AddComponent<SpriteRenderer>();
            spr.shadowCastingMode = ShadowCastingMode.Off;
            spr.receiveShadows = false;

            var anim = heroGo.AddComponent<SpriteFrameAnimator>();

            var textures = new[]
            {
                Resources.Load<Sprite>("Calle/Gallina/gallina_0"),
                Resources.Load<Sprite>("Calle/Gallina/gallina_1"),
            };

            var valid = new List<Sprite>();
            for (int i = 0; i < textures.Length; i++)
                if (textures[i] != null) valid.Add(textures[i]);

            if (valid.Count > 0)
            {
                anim.SetFrames(valid.ToArray());
                spr.sprite = valid[0];
            }

            heroGo.transform.localScale = Vector3.one * 1.8f;
            gallinaBody = heroGo.transform;

            walkers.Add(new Walker
            {
                Body = heroGo.transform,
                Anim = anim,
                Local = heroGo.transform.localPosition,
                Goal = heroGo.transform.localPosition,
                Home = heroGo.transform.localPosition,
                Hero = true,
            });
            SpawnCrowd();
        }

        void BuildHud()
        {
            var canvasGo = new GameObject("CajamarcaRealHUD");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGo.AddComponent<CanvasScaler>();
            canvasGo.AddComponent<GraphicRaycaster>();
            hud = canvasGo;

            // Panel superior con título
            var panel = new GameObject("Panel");
            panel.transform.SetParent(canvasGo.transform, false);
            var rect = panel.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.12f, 0.90f);
            rect.anchorMax = new Vector2(0.88f, 1f);
            rect.offsetMin = rect.offsetMax = Vector2.zero;

            var titleGo = new GameObject("Titulo");
            titleGo.transform.SetParent(panel.transform, false);
            var title = titleGo.AddComponent<Text>();
            Storybook.DressText(title, LeyDelMonte.CajamarcaRealKicker, Storybook.DisplayFont, 24, ColorHighlight, FontStyle.Bold);
            title.alignment = TextAnchor.MiddleCenter;
            var tRect = titleGo.GetComponent<RectTransform>();
            tRect.anchorMin = Vector2.zero;
            tRect.anchorMax = Vector2.one;
            tRect.offsetMin = tRect.offsetMax = Vector2.zero;

            // Franja inferior con instrucciones
            var subGo = new GameObject("Instrucciones");
            subGo.transform.SetParent(canvasGo.transform, false);
            var sub = subGo.AddComponent<Text>();
            Storybook.DressText(sub, LeyDelMonte.CajamarcaRealWalk, Storybook.BodyFont, 16, Color.white, FontStyle.Bold);
            aviso = sub;
            sub.alignment = TextAnchor.MiddleCenter;
            var sRect = subGo.GetComponent<RectTransform>();
            sRect.anchorMin = new Vector2(0.30f, 0.14f);
            sRect.anchorMax = new Vector2(0.70f, 0.22f);
            sRect.offsetMin = sRect.offsetMax = Vector2.zero;
            TituloFugaz.Begin(panel);
            avisoFade = TituloFugaz.Begin(subGo);
            Storybook.IconButton(canvasGo.transform, "Historia", Storybook.Marca.Historia,
                new Vector2(0.018f, 0.83f), new Vector2(0.092f, 0.96f),
                () => battle?.AbrirHistoria());
            Storybook.IconButton(canvasGo.transform, "Volver", Storybook.Marca.Volver,
                new Vector2(0.908f, 0.02f), new Vector2(0.982f, 0.15f),
                Close);
            movimientos = BotonesDeMovimiento.Armar(
                canvasGo.transform,
                () => ElegirVista(Vista.Planta),
                () => ElegirVista(Vista.Inclinacion),
                () => ZoomPlan(0.72f),
                () => ZoomPlan(1.38f));
            Storybook.Brujula(canvasGo.transform);
        }

        void WatchSummit()
        {
            if (toldSummit || walkers.Count == 0) return;
            Vector3 here = walkers[0].Local - SantaApoloniaSummit;
            here.y = 0f;
            if (here.sqrMagnitude > 70f * 70f) return;
            toldSummit = true;
            battle?.Contar("SANTA APOLONIA", LeyDelMonte.ValleSilencio);
        }

        void HideBattleHud()
        {
            hudBits.Clear();
            Transform story = battle != null && battle.startScreen != null
                ? battle.startScreen.transform.parent
                : null;
            var huds = FindObjectsByType<Canvas>(FindObjectsInactive.Exclude);
            for (int i = 0; i < huds.Length; i++)
            {
                if (huds[i].name == "StorybookCanvas" || huds[i].name == "PalabrasCanvas") continue;
                if (story != null && huds[i].transform == story)
                {
                    HideNamed(story);
                    continue;
                }

                hudBits.Add(new HudBit { Go = huds[i].gameObject, WasOn = huds[i].gameObject.activeSelf });
                huds[i].gameObject.SetActive(false);
            }
        }

        void HideNamed(Transform canvas)
        {
            string[] names =
            {
                "PlayerHealth_Panel", "EnemyHealth_Panel",
                "AYNI", "Espiritualidad", "Respeto", "Dinero", "Instructions",
            };
            for (int i = 0; i < names.Length; i++)
            {
                var child = canvas.Find(names[i]);
                if (child == null || !child.gameObject.activeSelf) continue;
                hudBits.Add(new HudBit { Go = child.gameObject, WasOn = true });
                child.gameObject.SetActive(false);
            }
        }

        void RestoreBattleHud()
        {
            for (int i = 0; i < hudBits.Count; i++)
                if (hudBits[i].Go != null)
                    hudBits[i].Go.SetActive(hudBits[i].WasOn);
            hudBits.Clear();
        }

        static AyniBakedCajamarcaReal FindBakedCity()
        {
            var all = FindObjectsByType<AyniBakedCajamarcaReal>(FindObjectsInactive.Include);
            return all.Length > 0 ? all[0] : null;
        }
    }
}
