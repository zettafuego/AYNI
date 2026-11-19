#if UNITY_EDITOR
using System;
using System.Collections;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using AYNI.Battle;
using UnityEditor;
using UnityEngine;

namespace AYNI.Editor
{
    /// <summary>
    /// Abre el capítulo CALLES, deja correr la escena y guarda renders de los
    /// NPCs nuevos (puestos, vendedoras y taxis) más un informe de colocación.
    /// Se dispara con Temp/ayni-calle-shot.flag o con el menú AYNI/Depurar.
    /// </summary>
    [InitializeOnLoad]
    public static class AyniCalleNpcShot
    {
        const string FlagPath = "Temp/ayni-calle-shot.flag";
        const string StatePath = "Temp/ayni-calle-shot.state";
        const string ShotDir = "docs/previas-calle-20261028";
        const string ResultPath = "Logs/calle-npcs-shot.txt";
        const int W = 1600, H = 900;

        static AyniCalleNpcShot()
        {
            // El flag se vigila siempre: así se puede lanzar la corrida aunque el
            // archivo aparezca después de que el editor termine de cargar.
            EditorApplication.update -= Vigilar;
            EditorApplication.update += Vigilar;
            Resume();
        }

        static void Vigilar()
        {
            if (!File.Exists(FlagPath)) return;
            try { File.Delete(FlagPath); }
            catch { }
            Arm();
            StartPlay();
        }

        [MenuItem("AYNI/Depurar/Renders de NPCs de la calle", priority = 71)]
        public static void Run()
        {
            Arm();
            StartPlay();
        }

        // El paso vive en un archivo, no en SessionState: el dominio se recarga
        // al entrar a Play y SessionState puede llegar vacío, y entonces la
        // corrida se abandonaba en silencio.
        static void Arm() => Escribir(0, Now, DateTime.UtcNow.AddMinutes(30).Ticks);

        static void StartPlay()
        {
            if (!File.Exists(StatePath)) return;
            EditorApplication.update -= Tick;
            EditorApplication.update += Tick;
            if (!EditorApplication.isPlaying) EditorApplication.isPlaying = true;
        }

        static void Resume()
        {
            if (!File.Exists(StatePath)) return;
            if (Vencido())
            {
                try { File.Delete(StatePath); } catch { }
                return;
            }

            // Al entrar a Play se recarga el dominio: el reloj del paso 0
            // arranca aquí, no cuando se pulsó el menú.
            Leer(out int step, out double mark);
            if (EditorApplication.isPlaying && step == 0) Escribir(0, Now, Limite);

            EditorApplication.update -= Tick;
            EditorApplication.update += Tick;
        }

        static void Escribir(int step, double mark, long limite)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(StatePath)));
            File.WriteAllText(StatePath,
                step.ToString(CultureInfo.InvariantCulture) + "\n"
                + mark.ToString("R", CultureInfo.InvariantCulture) + "\n"
                + limite.ToString(CultureInfo.InvariantCulture));
        }

        static void Leer(out int step, out double mark)
        {
            step = 0;
            mark = 0.0;
            if (!File.Exists(StatePath)) return;
            string[] lineas = File.ReadAllLines(StatePath);
            if (lineas.Length > 0)
                int.TryParse(lineas[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out step);
            if (lineas.Length > 1)
                double.TryParse(lineas[1], NumberStyles.Float, CultureInfo.InvariantCulture, out mark);
        }

        static long Limite
        {
            get
            {
                if (!File.Exists(StatePath)) return 0L;
                string[] lineas = File.ReadAllLines(StatePath);
                return lineas.Length > 2
                    && long.TryParse(lineas[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out long v)
                    ? v : 0L;
            }
        }

        static bool Vencido()
        {
            long limite = Limite;
            return limite > 0L && DateTime.UtcNow.Ticks > limite;
        }

        static double Now => EditorApplication.timeSinceStartup;

        static void Next(int step)
        {
            long limite = Limite;
            if (limite == 0L) limite = DateTime.UtcNow.AddMinutes(30).Ticks;
            Escribir(step, Now, limite);
        }

        static void Tick()
        {
            if (!File.Exists(StatePath) || Vencido())
            {
                try { File.Delete(StatePath); } catch { }
                EditorApplication.update -= Tick;
                return;
            }

            Leer(out int step, out double mark);
            if (mark > Now) mark = Now;
            double elapsed = Now - mark;
            var calle = UnityEngine.Object.FindAnyObjectByType<CajamarcaCalles>();

            try
            {
                // El delayCall se pierde en cada recarga de dominio: el arranque
                // de Play se hace aquí, que sí sobrevive.
                if (!EditorApplication.isPlaying)
                {
                    // La primera apertura del proyecto puede tardar varios
                    // minutos en cargar la escena; 15 min es margen sano.
                    if (Now - mark > 900.0) throw new Exception("No se pudo entrar a Play Mode.");
                    EditorApplication.isPlaying = true;
                    return;
                }

                switch (step)
                {
                    case 0:
                        if (elapsed < 2.5) return;
                        if (!CajamarcaCalles.IsOpen)
                        {
                            var manager = UnityEngine.Object.FindAnyObjectByType<LagunaBattleManager>();
                            if (manager == null) throw new Exception("No hay LagunaBattleManager en Play.");
                            manager.StartBattle();
                            CajamarcaCalles.Begin(manager);
                        }

                        Next(1);
                        return;

                    case 1:
                        if (elapsed < 1.8) return;
                        if (calle == null) throw new Exception("Cajamarca no se abrió.");
                        Snap("01-inicio");
                        Next(2);
                        return;

                    case 2:
                        if (elapsed < 0.2) return;
                        MoveHero(calle, 0f, -F(calle, "plazaHalfV") + 1.2f);
                        Next(3);
                        return;

                    case 3:
                        if (elapsed < 1.1) return;
                        Snap("02-vereda");
                        Next(4);
                        return;

                    case 4:
                        ShotStalls(calle);
                        Next(5);
                        return;

                    case 5:
                        ShotStreet(calle);
                        Next(6);
                        return;

                    case 6:
                        ShotClose(calle);
                        Next(7);
                        return;

                    default:
                        string report = Dump(calle);
                        File.WriteAllText(ResultPath, report);
                        Debug.Log("[AYNI] " + report.Replace("\n", " | "));
                        // Misma corrida: apoyo de cada caminante contra el suelo.
                        AyniPiesProbe.Run();
                        CajamarcaCalles.CloseCurrent();
                        Finish();
                        return;
                }
            }
            catch (Exception e)
            {
                try { File.WriteAllText(ResultPath, "ERROR: " + e); }
                catch { }
                Debug.LogException(e);
                Finish();
            }
        }

        static void Finish()
        {
            try { File.Delete(StatePath); } catch { }
            EditorApplication.update -= Tick;
            EditorApplication.isPlaying = false;
        }

        // ---------------------------------------------------------------- vistas

        static void ShotStalls(CajamarcaCalles calle)
        {
            float halfV = F(calle, "plazaHalfV");
            Vector2 from = Plaza(calle, 0f, -halfV + 23f);
            Vector2 to = Plaza(calle, 0f, -halfV + 6.4f);
            Free(calle, "03-puestos",
                At(calle, from, 4.6f), At(calle, to, 1.1f), 45f);
        }

        static void ShotStreet(CajamarcaCalles calle)
        {
            var method = typeof(CajamarcaCalles).GetMethod("StreetLane",
                BindingFlags.NonPublic | BindingFlags.Instance);
            if (method == null) return;

            object[] args = { -1, Vector2.zero, Vector2.right, 0f };
            if (!(bool)method.Invoke(calle, args)) return;

            var lane = (Vector2)args[1];
            var axis = (Vector2)args[2];
            float reach = (float)args[3];
            float back = Mathf.Min(30f, Mathf.Max(8f, reach));

            Vector2 from = lane - axis * back;
            Vector2 to = lane + axis * Mathf.Min(18f, reach);
            Free(calle, "04-calles", At(calle, from, 3.6f), At(calle, to, 1.2f), 50f);
        }

        static void ShotClose(CajamarcaCalles calle)
        {
            var root = DynamicRoot(calle);
            var district = root != null ? root.parent : null;
            if (root == null) return;

            Transform stall = null;
            float best = float.MaxValue;
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
            {
                if (!t.name.StartsWith("Puesto_", StringComparison.Ordinal)) continue;
                float d = Mathf.Abs(PlazaLocal(calle, t.position).x);
                if (d < best)
                {
                    best = d;
                    stall = t;
                }
            }

            if (stall == null) return;

            Vector3 center = new Vector3(F(calle, "fountainX"), 0f, F(calle, "fountainZ"));
            if (district != null) center = district.TransformPoint(center);

            Vector3 dir = center - stall.position;
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.01f) dir = Vector3.forward;
            dir.Normalize();

            Free(calle, "05-puesto-cerca",
                stall.position + dir * 4.2f + Vector3.up * 1.55f,
                stall.position + Vector3.up * 1.05f, 40f);
        }

        static void Free(CajamarcaCalles calle, string file, Vector3 pos, Vector3 target, float fov)
        {
            var cam = Camera.main;
            if (cam == null) return;

            bool wasOn = calle.enabled;
            Vector3 p0 = cam.transform.position;
            Quaternion r0 = cam.transform.rotation;
            float f0 = cam.fieldOfView;
            bool o0 = cam.orthographic;

            calle.enabled = false;
            cam.orthographic = false;
            cam.fieldOfView = fov;
            cam.transform.position = pos;
            cam.transform.LookAt(target);
            AimAll(cam);
            Render(cam, file);

            cam.transform.position = p0;
            cam.transform.rotation = r0;
            cam.fieldOfView = f0;
            cam.orthographic = o0;
            calle.enabled = wasOn;
        }

        static void Snap(string file)
        {
            var cam = Camera.main;
            if (cam != null) Render(cam, file);
        }

        static void Render(Camera cam, string file)
        {
            var rt = new RenderTexture(W, H, 24, RenderTextureFormat.ARGB32);
            var prev = cam.targetTexture;
            cam.targetTexture = rt;
            cam.Render();

            RenderTexture.active = rt;
            var tex = new Texture2D(W, H, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0f, 0f, W, H), 0, 0);
            tex.Apply();

            Directory.CreateDirectory(ShotDir);
            File.WriteAllBytes(Path.Combine(ShotDir, file + ".png"), tex.EncodeToPNG());

            RenderTexture.active = null;
            cam.targetTexture = prev;
            UnityEngine.Object.DestroyImmediate(rt);
            UnityEngine.Object.DestroyImmediate(tex);
            Debug.Log("[SHOT] " + file);
        }

        /// <summary>Todos los sprites de la calle miran a la cámara, como en juego.</summary>
        static void AimAll(Camera cam)
        {
            var root = GameObject.Find("__DINAMICO__");
            if (root == null) return;
            foreach (var ink in root.GetComponentsInChildren<SpriteRenderer>(true))
                ink.transform.rotation = cam.transform.rotation;
        }

        // ------------------------------------------------------------- utilidades

        static Transform DynamicRoot(CajamarcaCalles calle)
        {
            var go = GameObject.Find("__DINAMICO__");
            return go != null ? go.transform : null;
        }

        static float F(object target, string name)
        {
            var field = target.GetType().GetField(name,
                BindingFlags.NonPublic | BindingFlags.Instance);
            return field != null ? (float)field.GetValue(target) : 0f;
        }

        static Vector2 Plaza(CajamarcaCalles calle, float u, float v)
        {
            var method = typeof(CajamarcaCalles).GetMethod("PlazaWorld",
                BindingFlags.NonPublic | BindingFlags.Instance);
            return method != null
                ? (Vector2)method.Invoke(calle, new object[] { u, v })
                : new Vector2(u, v);
        }

        static Vector2 PlazaLocal(CajamarcaCalles calle, Vector3 world)
        {
            var district = DynamicRoot(calle)?.parent;
            Vector3 local = district != null ? district.InverseTransformPoint(world) : world;
            var method = typeof(CajamarcaCalles).GetMethod("PlazaLocal",
                BindingFlags.NonPublic | BindingFlags.Instance);
            return method != null
                ? (Vector2)method.Invoke(calle, new object[] { local.x, local.z })
                : new Vector2(local.x, local.z);
        }

        static Vector3 At(CajamarcaCalles calle, Vector2 local, float height)
        {
            var district = DynamicRoot(calle)?.parent;
            float ground = CajamarcaAltura.Sample(local.x, local.y);
            var p = new Vector3(local.x, ground + height, local.y);
            return district != null ? district.TransformPoint(p) : p;
        }

        /// <summary>Mundo → coordenadas locales del distrito (las que usan OnStreet/OnPlaza).</summary>
        static Vector2 LocalXZ(CajamarcaCalles calle, Vector3 world)
        {
            var district = DynamicRoot(calle)?.parent;
            Vector3 local = district != null ? district.InverseTransformPoint(world) : world;
            return new Vector2(local.x, local.z);
        }

        static object HeroWalker(CajamarcaCalles calle)
        {
            var field = typeof(CajamarcaCalles).GetField("walkers",
                BindingFlags.NonPublic | BindingFlags.Instance);
            if (field == null) return null;

            var list = (IList)field.GetValue(calle);
            if (list == null) return null;
            foreach (var w in list)
            {
                var hero = w.GetType().GetField("Hero");
                if (hero != null && (bool)hero.GetValue(w)) return w;
            }

            return null;
        }

        static void MoveHero(CajamarcaCalles calle, float u, float v)
        {
            var hero = HeroWalker(calle);
            if (hero == null) return;

            Vector2 p = Plaza(calle, u, v);
            var local = new Vector3(p.x, SueloDeLaCalle.Altura(p.x, p.y), p.y);
            var type = hero.GetType();
            type.GetField("Local")?.SetValue(hero, local);
            var body = (Transform)type.GetField("Body")?.GetValue(hero);
            if (body != null) body.localPosition = local;
        }

        // --------------------------------------------------------------- informe

        static string Dump(CajamarcaCalles calle)
        {
            var sb = new StringBuilder();
            var root = DynamicRoot(calle);
            float halfU = F(calle, "plazaHalfU");
            float halfV = F(calle, "plazaHalfV");

            sb.AppendLine("[AYNI] CALLE NPCs");
            sb.AppendLine($"plaza: halfU={halfU:F2} halfV={halfV:F2} " +
                          $"cos={F(calle, "plazaCos"):F3} sin={F(calle, "plazaSin"):F3}");
            sb.AppendLine($"heroe local={HeroWalker(calle)?.GetType().GetField("Local")?.GetValue(HeroWalker(calle))}");

            var onStreet = typeof(CajamarcaCalles).GetMethod("OnStreet",
                BindingFlags.NonPublic | BindingFlags.Instance);
            var onPlaza = typeof(CajamarcaCalles).GetMethod("OnPlaza",
                BindingFlags.NonPublic | BindingFlags.Instance);

            var stalls = new System.Collections.Generic.List<Transform>();
            int vendors = 0;
            if (root != null)
            {
                foreach (var t in root.GetComponentsInChildren<Transform>(true))
                {
                    if (t.name.StartsWith("Puesto_", StringComparison.Ordinal)) stalls.Add(t);
                    if (t.name == "Vendedora") vendors++;
                }
            }

            sb.AppendLine($"puestos={stalls.Count} vendedoras={vendors}");

            float minGap = float.MaxValue;
            for (int i = 0; i < stalls.Count; i++)
            {
                Vector3 w = stalls[i].position;
                Vector2 l = PlazaLocal(calle, w);
                Vector2 lz = LocalXZ(calle, w);
                bool street = onStreet != null && (bool)onStreet.Invoke(calle, new object[] { lz.x, lz.y });
                bool plaza = onPlaza != null && (bool)onPlaza.Invoke(calle, new object[] { lz.x, lz.y });
                sb.AppendLine($"  {stalls[i].name} u={l.x:F1} v={l.y:F1} " +
                              $"calle={street} plaza={plaza} escala={stalls[i].localScale.x:F3}");

                for (int j = i + 1; j < stalls.Count; j++)
                {
                    float d = Vector3.Distance(w, stalls[j].position);
                    if (d < minGap) minGap = d;
                }
            }

            sb.AppendLine($"separacion minima entre puestos={minGap:F2} m");

            int cars = 0;
            if (root != null)
            {
                foreach (var drive in root.GetComponentsInChildren<GallinaConductora>(true))
                {
                    cars++;
                    Vector2 l = PlazaLocal(calle, drive.transform.position);
                    sb.AppendLine($"  {drive.name} u={l.x:F1} v={l.y:F1} " +
                                  $"vel={drive.Speed:F2} izq={drive.Left:F1} der={drive.Right:F1}");
                }
            }

            sb.AppendLine($"taxis={cars}");

            var walkersField = typeof(CajamarcaCalles).GetField("walkers",
                BindingFlags.NonPublic | BindingFlags.Instance);
            var list = walkersField != null ? (IList)walkersField.GetValue(calle) : null;
            sb.AppendLine($"caminantes={list?.Count ?? 0}");

            return sb.ToString();
        }
    }
}
#endif
