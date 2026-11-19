#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using AYNI.Battle;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace AYNI.Editor
{
    /// <summary>
    /// Arma la ruta Plaza de Armas → Cerro Santa Apolonia a partir del
    /// mapa descargado. Cada calle es un objeto. Cada casa es un solo
    /// objeto a escala colonial (muro de dos pisos), apoyado en el DEM
    /// con el relieve de CALLES un poco exagerado.
    /// </summary>
    public static class CajamarcaRutaApolonia
    {
        public const string JsonPath = "Assets/Resources/Calle/ruta_apolonia.json";
        public static string LastReport = "";

        const float StreetLift = 0.07f;
        const float PlazaLift = 0.02f;
        const float GroundDrop = 0.08f;
        const float SummitX = -184f;
        const float SummitZ = -386f;
        const float ColonialWall = 6.8f;
        const float PlazaHalf = 52f;
        const float PlazaAngleDeg = -44f;

        struct Span
        {
            public Vector2 a;
            public Vector2 b;
            public float half;
        }

        [Serializable]
        class Xz
        {
            public float x;
            public float z;
        }

        [Serializable]
        class RouteDto
        {
            public string name;
            public string kind;
            public float half;
            public Xz[] pts;
        }

        [Serializable]
        class HouseDto
        {
            public string name;
            public bool church;
            public float height;
            public int door;
            public Xz[] ring;
        }

        [Serializable]
        class MapDto
        {
            public RouteDto[] routes;
            public HouseDto[] houses;
        }

        public static GameObject Build(Transform district, CajamarcaHeightField field, Shader shader)
        {
            if (field == null) throw new Exception("No hay modelo de elevación de Cajamarca.");
            if (!File.Exists(JsonPath))
                throw new FileNotFoundException("No está el mapa de la ruta: " + JsonPath);

            var map = JsonUtility.FromJson<MapDto>(File.ReadAllText(JsonPath));
            if (map == null || map.routes == null || map.routes.Length == 0)
                throw new Exception("El mapa de la ruta no tiene calles.");

            shader = shader != null ? shader : Shader.Find("Standard");
            var cobble = MakeMat(shader, "RutaEmpedrado", "Calle/piso_empedrado", true);
            var stone = MakeMat(shader, "RutaPiedra", "Calle/piedra", true);
            var plazaMat = MakeMat(shader, "RutaPlaza", "Calle/piso_plaza", true);
            var groundMat = MakeMat(shader, "RutaTerreno", null, true);
            var adobe = MakeMat(shader, "RutaAdobe", "Calle/muro_adobe", true);
            var tile = MakeMat(shader, "RutaTeja", "Calle/teja", true);
            var facade = MakeMat(shader, "RutaFachada", "Calle/fachada_colonial", true);
            var portadaCatedral = MakeMat(shader, "RutaPortadaCatedral", "Calle/portada_catedral", false);
            var portadaFrancisco = MakeMat(shader, "RutaPortadaFrancisco", "Calle/portada_francisco", false);
            var garden = MakeMat(shader, "RutaJardin", null, true);
            var water = MakeMat(shader, "RutaAgua", null, true);

            var plano = new GameObject(AyniBakedCajamarca.PlanName);
            plano.transform.SetParent(district, false);
            var rutas = new GameObject("Rutas");
            rutas.transform.SetParent(plano.transform, false);
            var casas = new GameObject("Casas");
            casas.transform.SetParent(district, false);

            float minX = 0f, maxX = 0f, minZ = 0f, maxZ = 0f;
            bool boxed = false;
            int routesBuilt = 0;
            var spans = new List<Span>(512);
            for (int i = 0; i < map.routes.Length; i++)
            {
                if (!BuildRoute(rutas.transform, map.routes[i], field, cobble, stone, spans)) continue;
                routesBuilt++;
                var pts = map.routes[i].pts;
                for (int p = 0; p < pts.Length; p++)
                    Encapsulate(ref boxed, ref minX, ref maxX, ref minZ, ref maxZ, pts[p].x, pts[p].z);
            }

            int trees = BuildPlaza(plano.transform, district, field, shader, plazaMat, stone, garden, water, spans);
            trees += BuildMonumento(district, field, shader, stone, garden);

            int housesBuilt = 0;
            int churches = 0;
            var washes = new[]
            {
                new Color(1f, 0.97f, 0.92f),
                new Color(0.96f, 0.78f, 0.68f),
                new Color(0.98f, 0.88f, 0.78f),
                new Color(0.95f, 0.90f, 0.70f),
                new Color(0.90f, 0.84f, 0.74f),
            };
            var houses = map.houses ?? new HouseDto[0];
            for (int i = 0; i < houses.Length; i++)
            {
                bool church = houses[i].church || houses[i].height > 7f;
                var front = adobe;
                if (church && Has(houses[i].name, "catedral")) front = portadaCatedral;
                else if (church && Has(houses[i].name, "francisco")) front = portadaFrancisco;
                else if (!church) front = facade;
                Color wash = church
                    ? new Color(0.82f, 0.80f, 0.74f)
                    : Color.Lerp(Color.white, washes[i % washes.Length], 0.4f);
                if (!BuildHouse(casas.transform, houses[i], field, adobe, tile, front, wash, church))
                    continue;
                housesBuilt++;
                if (church) churches++;
                var ring = houses[i].ring;
                for (int p = 0; p < ring.Length; p++)
                    Encapsulate(ref boxed, ref minX, ref maxX, ref minZ, ref maxZ, ring[p].x, ring[p].z);
            }

            EncapsulatePlaza(ref boxed, ref minX, ref maxX, ref minZ, ref maxZ);
            Encapsulate(ref boxed, ref minX, ref maxX, ref minZ, ref maxZ, SummitX, SummitZ);
            BuildTerrain(district, field, groundMat, minX - 28f, maxX + 28f, minZ - 28f, maxZ + 28f);

            float stuck = NearestStreetGap(rutas.transform, field, out float streetY, out float demY, out float streetD);
            LastReport =
                "rutas=" + routesBuilt +
                " casas=" + housesBuilt +
                " iglesias=" + churches +
                " plaza=" + Height(field, 0f, 0f).ToString("F1") +
                " apolonia=" + Height(field, SummitX, SummitZ).ToString("F1") +
                " relieve=" + CajamarcaAltura.RelieveCalles.ToString("F2") +
                " crudoPlaza=" + field.Sample(0f, 0f).ToString("F1") +
                " crudoApolonia=" + field.Sample(SummitX, SummitZ).ToString("F1") +
                " calleCerca=" + streetD.ToString("F0") + "m y=" + streetY.ToString("F1") +
                " dem=" + demY.ToString("F1") +
                " holgura=" + stuck.ToString("F2") +
                " arboles=" + trees +
                " monumento=1";
            return plano;
        }

        public static void Capture(Transform root)
        {
            if (root == null) return;
            string dir = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Logs");
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            Vector3 origin = root.position;
            bool fog = RenderSettings.fog;
            RenderSettings.fog = false;
            try
            {
                Shoot(Path.Combine(dir, "cajamarca-ruta-plaza.png"),
                    origin + new Vector3(78f, 68f, 102f),
                    origin + new Vector3(-28f, 14f, -55f), 42f);
                Shoot(Path.Combine(dir, "cajamarca-ruta-planta.png"),
                    origin + new Vector3(28f, 210f, 78f),
                    origin + new Vector3(52f, 0f, -24f), 52f);
                Shoot(Path.Combine(dir, "cajamarca-ruta-subida.png"),
                    origin + new Vector3(-8f, 96f, -70f),
                    origin + new Vector3(SummitX, 118f, SummitZ), 42f);
            }
            finally
            {
                RenderSettings.fog = fog;
            }
        }

        static void Shoot(string path, Vector3 from, Vector3 to, float fov)
        {
            var go = new GameObject("ShotRuta");
            var cam = go.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.63f, 0.76f, 0.88f);
            cam.fieldOfView = fov;
            cam.nearClipPlane = 0.3f;
            cam.farClipPlane = 2500f;
            cam.transform.position = from;
            cam.transform.LookAt(to);
            var rt = new RenderTexture(1280, 720, 24, RenderTextureFormat.ARGB32);
            cam.targetTexture = rt;
            cam.Render();
            RenderTexture.active = rt;
            var tex = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
            tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            RenderTexture.active = null;
            cam.targetTexture = null;
            UnityEngine.Object.DestroyImmediate(rt);
            UnityEngine.Object.DestroyImmediate(tex);
            UnityEngine.Object.DestroyImmediate(go);
        }

        static bool BuildRoute(
            Transform parent, RouteDto route, CajamarcaHeightField field,
            Material cobble, Material stone, List<Span> spans)
        {
            if (route == null || route.pts == null || route.pts.Length < 2) return false;
            var line = Subdivide(route.pts, 4f);
            if (line.Count < 2) return false;

            float half = Mathf.Max(0.7f, route.half);
            if (spans != null)
            {
                for (int s = 1; s < route.pts.Length; s++)
                {
                    spans.Add(new Span
                    {
                        a = new Vector2(route.pts[s - 1].x, route.pts[s - 1].z),
                        b = new Vector2(route.pts[s].x, route.pts[s].z),
                        half = half,
                    });
                }
            }

            int n = line.Count;
            var verts = new Vector3[n * 2];
            var uv = new Vector2[n * 2];
            var colors = new Color[n * 2];
            float traveled = 0f;
            for (int i = 0; i < n; i++)
            {
                Vector2 prev = line[Mathf.Max(0, i - 1)];
                Vector2 next = line[Mathf.Min(n - 1, i + 1)];
                Vector2 dir = next - prev;
                if (dir.sqrMagnitude < 1e-6f) dir = Vector2.right;
                dir.Normalize();
                Vector2 side = new Vector2(-dir.y, dir.x);
                Vector2 p = line[i];
                if (i > 0) traveled += Vector2.Distance(line[i - 1], p);
                var left = new Vector3(p.x + side.x * half, 0f, p.y + side.y * half);
                var right = new Vector3(p.x - side.x * half, 0f, p.y - side.y * half);
                left.y = Height(field, left.x, left.z) + StreetLift;
                right.y = Height(field, right.x, right.z) + StreetLift;
                verts[i * 2] = left;
                verts[i * 2 + 1] = right;
                float v = traveled / Mathf.Max(1.5f, half * 2f);
                uv[i * 2] = new Vector2(0f, v);
                uv[i * 2 + 1] = new Vector2(1f, v);
                colors[i * 2] = Color.white;
                colors[i * 2 + 1] = Color.white;
            }

            var tris = new int[(n - 1) * 6];
            int t = 0;
            for (int i = 0; i < n - 1; i++)
            {
                int a = i * 2;
                int b = a + 1;
                int c = a + 2;
                int d = a + 3;
                tris[t++] = a; tris[t++] = b; tris[t++] = d;
                tris[t++] = a; tris[t++] = d; tris[t++] = c;
            }

            string label = string.IsNullOrEmpty(route.name) ? KindLabel(route.kind) : route.name.Trim();
            var go = new GameObject(Unique(parent, "Calles " + label));
            go.transform.SetParent(parent, false);
            var mesh = new Mesh { name = go.name };
            mesh.vertices = verts;
            mesh.uv = uv;
            mesh.colors = colors;
            mesh.triangles = tris;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            Finish(go, mesh, StoneKind(route.kind) ? stone : cobble);
            return true;
        }

        static int BuildPlaza(
            Transform plano, Transform district, CajamarcaHeightField field, Shader shader,
            Material plazaMat, Material stone, Material garden, Material water, List<Span> spans)
        {
            const float step = 4f;
            int cells = Mathf.RoundToInt(PlazaHalf * 2f / step);
            int side = cells + 1;
            var verts = new Vector3[side * side];
            var uv = new Vector2[side * side];
            var colors = new Color[side * side];
            for (int j = 0; j < side; j++)
            {
                for (int i = 0; i < side; i++)
                {
                    float u = -PlazaHalf + i * step;
                    float v = -PlazaHalf + j * step;
                    Vector2 w = PlazaToWorld(u, v);
                    int k = j * side + i;
                    verts[k] = new Vector3(w.x, Height(field, w.x, w.y) + PlazaLift, w.y);
                    uv[k] = new Vector2(u / 8f, v / 8f);
                    colors[k] = Color.white;
                }
            }

            var tris = new List<int>(cells * cells * 6);
            for (int j = 0; j < cells; j++)
            {
                for (int i = 0; i < cells; i++)
                {
                    int a = j * side + i;
                    int b = a + 1;
                    int c = a + side;
                    int d = c + 1;
                    tris.Add(a); tris.Add(b); tris.Add(d);
                    tris.Add(a); tris.Add(d); tris.Add(c);
                }
            }

            var go = new GameObject("PlanoPlazas");
            go.transform.SetParent(plano, false);
            var mesh = new Mesh { name = "PlanoPlazas" };
            mesh.vertices = verts;
            mesh.uv = uv;
            mesh.colors = colors;
            mesh.triangles = tris.ToArray();
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            Finish(go, mesh, plazaMat);

            DressPlazaGround(district, field, stone, garden, spans);
            BuildFountain(district, field, stone, water);
            return PlantPlazaTrees(district, field, shader, spans);
        }

        static void DressPlazaGround(
            Transform district, CajamarcaHeightField field,
            Material stone, Material garden, List<Span> spans)
        {
            var grassV = new List<Vector3>();
            var grassU = new List<Vector2>();
            var grassC = new List<Color>();
            var grassT = new List<int>();
            var pathV = new List<Vector3>();
            var pathU = new List<Vector2>();
            var pathC = new List<Color>();
            var pathT = new List<int>();

            // Por encima de las veredas OSM (StreetLift 0.07) para que no
            // recorten el jardín. Las calzadas de verdad se respetan.
            const float promenade = 5.15f;
            const float spokeHalf = 2.2f;
            const float liftGrass = PlazaLift + 0.105f;
            const float liftCourt = PlazaLift + 0.12f;
            const float liftPath = PlazaLift + 0.135f;
            var red = new Color(0.82f, 0.24f, 0.28f);
            var gold = new Color(0.94f, 0.78f, 0.28f);

            const int arc = 32;
            const float ringIn = 6.3f;
            const float ringOut = 11.2f;
            for (int i = 0; i < arc; i++)
            {
                float a0 = i * Mathf.PI * 2f / arc;
                float a1 = (i + 1) * Mathf.PI * 2f / arc;
                StampUvCorners(pathV, pathU, pathC, pathT, field,
                    Mathf.Cos(a0) * ringIn, Mathf.Sin(a0) * ringIn,
                    Mathf.Cos(a1) * ringIn, Mathf.Sin(a1) * ringIn,
                    Mathf.Cos(a1) * ringOut, Mathf.Sin(a1) * ringOut,
                    Mathf.Cos(a0) * ringOut, Mathf.Sin(a0) * ringOut,
                    liftCourt, Color.white);
            }

            for (int k = 0; k < 8; k++)
            {
                float a = k * 45f * Mathf.Deg2Rad;
                float du = Mathf.Cos(a);
                float dv = Mathf.Sin(a);
                float pu = -dv;
                float pv = du;
                float r0 = 6.5f;
                float r1 = SquareRadius(a) - promenade + 0.3f;
                int steps = Mathf.Max(1, Mathf.CeilToInt((r1 - r0) / 4f));
                for (int s = 0; s < steps; s++)
                {
                    float ra = Mathf.Lerp(r0, r1, s / (float)steps);
                    float rb = Mathf.Lerp(r0, r1, (s + 1) / (float)steps);
                    float mu = du * (ra + rb) * 0.5f;
                    float mv = dv * (ra + rb) * 0.5f;
                    Vector2 mid = PlazaToWorld(mu, mv);
                    if (RoadClearance(spans, mid.x, mid.y) < 0.9f) continue;
                    StampUvCorners(pathV, pathU, pathC, pathT, field,
                        du * ra + pu * spokeHalf, dv * ra + pv * spokeHalf,
                        du * ra - pu * spokeHalf, dv * ra - pv * spokeHalf,
                        du * rb - pu * spokeHalf, dv * rb - pv * spokeHalf,
                        du * rb + pu * spokeHalf, dv * rb + pv * spokeHalf,
                        liftPath, Color.white);
                }
            }

            float outer = PlazaHalf - 0.4f;
            float inner = PlazaHalf - promenade;
            StampBand(pathV, pathU, pathC, pathT, field, spans, inner, -outer, outer, outer, liftPath);
            StampBand(pathV, pathU, pathC, pathT, field, spans, -outer, -outer, -inner, outer, liftPath);
            StampBand(pathV, pathU, pathC, pathT, field, spans, -inner, inner, inner, outer, liftPath);
            StampBand(pathV, pathU, pathC, pathT, field, spans, -inner, -outer, inner, -inner, liftPath);

            for (int k = 0; k < 8; k++)
            {
                float a0 = k * 45f * Mathf.Deg2Rad;
                float a1 = (k + 1) * 45f * Mathf.Deg2Rad;
                Color deep = (k & 1) == 0
                    ? new Color(0.26f, 0.46f, 0.21f)
                    : new Color(0.34f, 0.56f, 0.27f);
                Color open = new Color(0.46f, 0.68f, 0.34f);
                for (float r = 11.5f; r < PlazaHalf * 1.5f; r += 4f)
                {
                    float r2 = r + 4f;
                    float guard = Mathf.Atan2(spokeHalf + 0.55f, r);
                    float left = a0 + guard;
                    float right = a1 - guard;
                    if (right <= left + 0.03f) continue;
                    int parts = right - left > 0.38f ? 2 : 1;
                    for (int p = 0; p < parts; p++)
                    {
                        float b0 = Mathf.Lerp(left, right, p / (float)parts);
                        float b1 = Mathf.Lerp(left, right, (p + 1) / (float)parts);
                        float o0 = GrassOuter(b0, promenade);
                        float o1 = GrassOuter(b1, promenade);
                        if (r >= o0 - 0.05f || r >= o1 - 0.05f) continue;
                        float ra = Mathf.Min(r2, o0);
                        float rb = Mathf.Min(r2, o1);
                        float mu = (Mathf.Cos(b0) + Mathf.Cos(b1)) * (r + ra) * 0.25f;
                        float mv = (Mathf.Sin(b0) + Mathf.Sin(b1)) * (r + rb) * 0.25f;
                        Vector2 mid = PlazaToWorld(mu, mv);
                        if (RoadClearance(spans, mid.x, mid.y) < 0.9f) continue;
                        Color tone = Color.Lerp(open, deep, Mathf.Clamp01((r - 12f) / 22f));
                        StampUvCorners(grassV, grassU, grassC, grassT, field,
                            Mathf.Cos(b0) * r, Mathf.Sin(b0) * r,
                            Mathf.Cos(b1) * r, Mathf.Sin(b1) * r,
                            Mathf.Cos(b1) * rb, Mathf.Sin(b1) * rb,
                            Mathf.Cos(b0) * ra, Mathf.Sin(b0) * ra,
                            liftGrass, tone);
                    }
                }

                float ba = (22.5f + 45f * k) * Mathf.Deg2Rad;
                float cu = Mathf.Cos(ba);
                float cv = Mathf.Sin(ba);
                float tu = -cv;
                float tv = cu;
                const float bed = 15.2f;
                StampUvCorners(grassV, grassU, grassC, grassT, field,
                    (bed - 0.8f) * cu + tu * 1.35f, (bed - 0.8f) * cv + tv * 1.35f,
                    (bed - 0.8f) * cu - tu * 1.35f, (bed - 0.8f) * cv - tv * 1.35f,
                    (bed + 0.8f) * cu - tu * 1.35f, (bed + 0.8f) * cv - tv * 1.35f,
                    (bed + 0.8f) * cu + tu * 1.35f, (bed + 0.8f) * cv + tv * 1.35f,
                    liftGrass + 0.03f, (k & 1) == 0 ? red : gold);
            }

            PublishMesh(district, "Cesped", grassV, grassU, grassC, grassT, garden);
            PublishMesh(district, "Senderos", pathV, pathU, pathC, pathT, stone);
        }

        static void BuildFountain(Transform district, CajamarcaHeightField field, Material stone, Material water)
        {
            float baseY = Height(field, 0f, 0f) + PlazaLift;
            var root = new GameObject("Fuente");
            root.transform.SetParent(district, false);
            root.transform.localPosition = new Vector3(0f, baseY, 0f);

            var stoneV = new List<Vector3>();
            var stoneU = new List<Vector2>();
            var stoneC = new List<Color>();
            var stoneT = new List<int>();
            var waterV = new List<Vector3>();
            var waterU = new List<Vector2>();
            var waterC = new List<Color>();
            var waterT = new List<int>();

            const int sides = 24;
            const float outer = 6.35f;
            const float inner = 5.05f;
            var basin = new Color(0.93f, 0.90f, 0.84f);
            for (int i = 0; i < sides; i++)
            {
                Vector3 ob0 = FountainRing(outer, 0.08f, i, sides);
                Vector3 ob1 = FountainRing(outer, 0.08f, i + 1, sides);
                Vector3 ot0 = FountainRing(outer, 1.02f, i, sides);
                Vector3 ot1 = FountainRing(outer, 1.02f, i + 1, sides);
                Vector3 it0 = FountainRing(inner, 0.86f, i, sides);
                Vector3 it1 = FountainRing(inner, 0.86f, i + 1, sides);
                Vector3 ib0 = FountainRing(inner, 0.2f, i, sides);
                Vector3 ib1 = FountainRing(inner, 0.2f, i + 1, sides);
                PushQuad(stoneV, stoneU, stoneC, stoneT, ob0, ob1, ot1, ot0,
                    Vector2.zero, Vector2.right, Vector2.one, Vector2.up, basin);
                PushQuad(stoneV, stoneU, stoneC, stoneT, ot0, ot1, it1, it0,
                    Vector2.zero, Vector2.right, Vector2.one, Vector2.up, Color.white);
                PushQuad(stoneV, stoneU, stoneC, stoneT, it0, it1, ib1, ib0,
                    Vector2.zero, Vector2.right, Vector2.one, Vector2.up, basin);
            }

            AddBox(stoneV, stoneU, stoneC, stoneT, new Vector3(0f, 1.7f, 0f), new Vector3(0.5f, 2.15f, 0.5f), basin);
            AddDisc(stoneV, stoneU, stoneC, stoneT, 1.45f, 2.85f, Color.white, 16);
            AddDisc(waterV, waterU, waterC, waterT, 4.85f, 0.64f, new Color(0.42f, 0.70f, 0.78f), 24);

            PublishMesh(root.transform, "FuentePiedra", stoneV, stoneU, stoneC, stoneT, stone);
            PublishMesh(root.transform, "FuenteAgua", waterV, waterU, waterC, waterT, water);
        }

        static int PlantPlazaTrees(Transform district, CajamarcaHeightField field, Shader shader, List<Span> spans)
        {
            var root = new GameObject("Arboles");
            root.transform.SetParent(district, false);
            string[] kinds =
            {
                "Bosque/tree_detailed",
                "Bosque/tree_oak",
                "Bosque/tree_tall",
                "Bosque/tree_default",
                "Bosque/tree_simple",
                "Bosque/tree_oak_dark",
            };
            int planted = 0;
            for (int k = 0; k < 8; k++)
            {
                float a = (22.5f + 45f * k) * Mathf.Deg2Rad;
                float cu = Mathf.Cos(a);
                float cv = Mathf.Sin(a);
                float pu = -cv;
                float pv = cu;
                planted += TryPlazaTree(root.transform, field, shader, spans, kinds, cu * 17.5f, cv * 17.5f, planted);
                planted += TryPlazaTree(root.transform, field, shader, spans, kinds,
                    cu * 27f + pu * 5.6f, cv * 27f + pv * 5.6f, planted + 1);
                planted += TryPlazaTree(root.transform, field, shader, spans, kinds,
                    cu * 27f - pu * 5.6f, cv * 27f - pv * 5.6f, planted + 2);
                planted += TryPlazaTree(root.transform, field, shader, spans, kinds,
                    cu * 36.5f + pu * 8.2f, cv * 36.5f + pv * 8.2f, planted + 3);
                planted += TryPlazaTree(root.transform, field, shader, spans, kinds,
                    cu * 36.5f - pu * 8.2f, cv * 36.5f - pv * 8.2f, planted + 4);
                planted += TryPlazaTree(root.transform, field, shader, spans, kinds, cu * 44f, cv * 44f, planted + 5);
            }

            if (planted == 0) UnityEngine.Object.DestroyImmediate(root);
            return planted;
        }

        static int TryPlazaTree(
            Transform parent, CajamarcaHeightField field, Shader shader, List<Span> spans,
            string[] kinds, float u, float v, int salt)
        {
            float inset = PlazaHalf - Mathf.Max(Mathf.Abs(u), Mathf.Abs(v));
            if (inset < 7.4f) return 0;
            if (u * u + v * v < 13.5f * 13.5f) return 0;
            if (Mathf.Abs(u) < 3.8f || Mathf.Abs(v) < 3.8f) return 0;
            if (Mathf.Abs(u - v) < 5.4f || Mathf.Abs(u + v) < 5.4f) return 0;
            Vector2 w = PlazaToWorld(u, v);
            if (RoadClearance(spans, w.x, w.y) < 2.4f) return 0;
            float ground = Height(field, w.x, w.y);
            float height = 8.6f + (Mathf.Abs(salt) % 6) * 0.9f;
            PlaceTree(parent, kinds[Mathf.Abs(salt) % kinds.Length], w.x, w.y, ground, height, salt * 28f, shader);
            return 1;
        }

        static int BuildMonumento(
            Transform district, CajamarcaHeightField field, Shader shader, Material stone, Material garden)
        {
            Vector2 center = new Vector2(102f, -36f);
            float rootY = Height(field, center.x, center.y) + PlazaLift;
            var root = new GameObject("MonumentoSanFrancisco");
            root.transform.SetParent(district, false);
            root.transform.localPosition = new Vector3(center.x, rootY, center.y);

            Vector2 toPlaza = (-center).normalized;
            Vector2 apex = toPlaza * 16f;
            Vector2 b1 = Rotate2(toPlaza, 115f) * 26f;
            Vector2 b2 = Rotate2(toPlaza, -115f) * 26f;

            var lawnV = new List<Vector3>();
            var lawnU = new List<Vector2>();
            var lawnC = new List<Color>();
            var lawnT = new List<int>();
            const int div = 5;
            Vector2 Corner(int i, int j)
            {
                float u = i / (float)div;
                float v = j / (float)div;
                return apex * (1f - u - v) + b1 * u + b2 * v;
            }

            Vector3 Lifted(Vector2 p, float extra)
            {
                float y = Height(field, center.x + p.x, center.y + p.y) + PlazaLift + extra - rootY;
                return new Vector3(p.x, y, p.y);
            }

            for (int j = 0; j < div; j++)
            {
                for (int i = 0; i < div - j; i++)
                {
                    Vector2 c0 = Corner(i, j);
                    Vector2 c1 = Corner(i + 1, j);
                    Vector2 c2 = Corner(i, j + 1);
                    if ((c0 + c1 + c2).magnitude / 3f > 7.3f)
                        PushTri(lawnV, lawnU, lawnC, lawnT, Lifted(c0, 0.1f), Lifted(c1, 0.1f), Lifted(c2, 0.1f), LawnTone(c0));
                    if (i + j < div - 1)
                    {
                        Vector2 d0 = Corner(i + 1, j);
                        Vector2 d1 = Corner(i + 1, j + 1);
                        Vector2 d2 = Corner(i, j + 1);
                        if ((d0 + d1 + d2).magnitude / 3f > 7.3f)
                            PushTri(lawnV, lawnU, lawnC, lawnT, Lifted(d0, 0.1f), Lifted(d1, 0.1f), Lifted(d2, 0.1f), LawnTone(d0));
                    }
                }
            }

            var courtV = new List<Vector3>();
            var courtU = new List<Vector2>();
            var courtC = new List<Color>();
            var courtT = new List<int>();
            const int sides = 28;
            const float courtR = 6.6f;
            int hub = courtV.Count;
            courtV.Add(new Vector3(0f, 0.16f, 0f));
            courtU.Add(new Vector2(0.5f, 0.5f));
            courtC.Add(Color.white);
            for (int i = 0; i <= sides; i++)
            {
                float a = i * Mathf.PI * 2f / sides;
                float lx = Mathf.Cos(a) * courtR;
                float lz = Mathf.Sin(a) * courtR;
                float y = Height(field, center.x + lx, center.y + lz) + PlazaLift + 0.16f - rootY;
                courtV.Add(new Vector3(lx, y, lz));
                courtU.Add(new Vector2(Mathf.Cos(a) * 0.5f + 0.5f, Mathf.Sin(a) * 0.5f + 0.5f));
                courtC.Add(Color.white);
            }

            for (int i = 0; i < sides; i++)
            {
                courtT.Add(hub);
                courtT.Add(hub + 1 + i);
                courtT.Add(hub + 2 + i);
                Vector3 e0 = courtV[hub + 1 + i];
                Vector3 e1 = courtV[hub + 2 + i];
                var cream = new Color(0.94f, 0.91f, 0.84f);
                PushQuad(courtV, courtU, courtC, courtT,
                    e0, e1, e1 + Vector3.up * 0.42f, e0 + Vector3.up * 0.42f,
                    Vector2.zero, Vector2.right, Vector2.one, Vector2.up, cream);
            }

            AddFrustum(courtV, courtU, courtC, courtT, Quaternion.identity,
                0.22f, 2.35f, 0.82f, 2.05f, 16, Color.white);

            var figureV = new List<Vector3>();
            var figureU = new List<Vector2>();
            var figureC = new List<Color>();
            var figureT = new List<int>();
            Quaternion face = Quaternion.LookRotation(new Vector3(toPlaza.x, 0f, toPlaza.y), Vector3.up);
            var robe = new Color(0.94f, 0.90f, 0.80f);
            var hood = new Color(0.62f, 0.42f, 0.24f);
            var skin = new Color(0.93f, 0.78f, 0.62f);
            var cross = new Color(0.92f, 0.74f, 0.28f);
            AddFrustum(figureV, figureU, figureC, figureT, face, 0.82f, 0.78f, 2.45f, 0.52f, 10, robe);
            AddFrustum(figureV, figureU, figureC, figureT, face, 2.15f, 0.66f, 2.7f, 0.4f, 8, hood);
            AddSphere(figureV, figureU, figureC, figureT, face * new Vector3(0f, 2.95f, 0.06f), 0.34f, skin);
            AddBox(figureV, figureU, figureC, figureT, face * new Vector3(0f, 1.85f, 0.62f), face, new Vector3(0.16f, 1.35f, 0.12f), cross);
            AddBox(figureV, figureU, figureC, figureT, face * new Vector3(0f, 2.15f, 0.62f), face, new Vector3(0.92f, 0.16f, 0.12f), cross);

            PublishMesh(root.transform, "Jardin", lawnV, lawnU, lawnC, lawnT, garden);
            PublishMesh(root.transform, "Plazoleta", courtV, courtU, courtC, courtT, stone);
            PublishMesh(root.transform, "Figura", figureV, figureU, figureC, figureT, garden);

            int planted = 0;
            Transform grove = district.Find("Arboles");
            float heading = Mathf.Atan2(toPlaza.y, toPlaza.x);
            for (int i = 0; i < 5; i++)
            {
                float a = heading + (72f + i * 52f) * Mathf.Deg2Rad;
                var spot = new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * 11.4f;
                if (!InsideTri(spot, apex, b1, b2, 0.08f)) continue;
                if (grove == null)
                {
                    var go = new GameObject("Arboles");
                    go.transform.SetParent(district, false);
                    grove = go.transform;
                }

                float wx = center.x + spot.x;
                float wz = center.y + spot.y;
                string[] kinds = { "Bosque/tree_detailed", "Bosque/tree_oak", "Bosque/tree_tall", "Bosque/tree_default", "Bosque/tree_simple", "Bosque/tree_oak_dark" };
                PlaceTree(grove, kinds[i % kinds.Length], wx, wz, Height(field, wx, wz), 8.4f + i * 0.7f, i * 40f, shader);
                planted++;
            }

            return planted;
        }

        static Color LawnTone(Vector2 local)
        {
            float t = Mathf.Clamp01(local.magnitude / 26f);
            return Color.Lerp(new Color(0.44f, 0.66f, 0.32f), new Color(0.24f, 0.44f, 0.20f), t);
        }

        static bool InsideTri(Vector2 p, Vector2 a, Vector2 b, Vector2 c, float margin)
        {
            float den = (b.y - c.y) * (a.x - c.x) + (c.x - b.x) * (a.y - c.y);
            if (Mathf.Abs(den) < 1e-4f) return false;
            float w0 = ((b.y - c.y) * (p.x - c.x) + (c.x - b.x) * (p.y - c.y)) / den;
            float w1 = ((c.y - a.y) * (p.x - c.x) + (a.x - c.x) * (p.y - c.y)) / den;
            float w2 = 1f - w0 - w1;
            return w0 > margin && w1 > margin && w2 > margin;
        }

        static Vector2 Rotate2(Vector2 v, float degrees)
        {
            float a = degrees * Mathf.Deg2Rad;
            float c = Mathf.Cos(a);
            float s = Mathf.Sin(a);
            return new Vector2(v.x * c - v.y * s, v.x * s + v.y * c);
        }

        static void PushTri(
            List<Vector3> v, List<Vector2> uv, List<Color> c, List<int> t,
            Vector3 a, Vector3 b, Vector3 c0, Color color)
        {
            int i = v.Count;
            v.Add(a); v.Add(b); v.Add(c0);
            uv.Add(new Vector2(a.x, a.z));
            uv.Add(new Vector2(b.x, b.z));
            uv.Add(new Vector2(c0.x, c0.z));
            c.Add(color); c.Add(color); c.Add(color);
            t.Add(i); t.Add(i + 1); t.Add(i + 2);
        }

        static void AddSphere(
            List<Vector3> v, List<Vector2> uv, List<Color> c, List<int> t,
            Vector3 center, float radius, Color color)
        {
            const int latN = 5;
            const int lonN = 8;
            int Columns = lonN + 1;
            int baseIndex = v.Count;
            for (int lat = 0; lat <= latN; lat++)
            {
                float phi = lat * Mathf.PI / latN;
                float y = Mathf.Cos(phi) * radius;
                float ring = Mathf.Sin(phi) * radius;
                for (int lon = 0; lon <= lonN; lon++)
                {
                    float a = lon * Mathf.PI * 2f / lonN;
                    v.Add(center + new Vector3(Mathf.Cos(a) * ring, y, Mathf.Sin(a) * ring));
                    uv.Add(new Vector2(lon / (float)lonN, lat / (float)latN));
                    c.Add(color);
                }
            }

            for (int lat = 0; lat < latN; lat++)
            {
                for (int lon = 0; lon < lonN; lon++)
                {
                    int a = baseIndex + lat * Columns + lon;
                    int b = a + 1;
                    int d = a + Columns;
                    int e = d + 1;
                    t.Add(a); t.Add(e); t.Add(b);
                    t.Add(a); t.Add(d); t.Add(e);
                }
            }
        }

        static void AddFrustum(
            List<Vector3> v, List<Vector2> uv, List<Color> c, List<int> t,
            Quaternion rotation, float y0, float r0, float y1, float r1, int sides, Color color)
        {
            for (int i = 0; i < sides; i++)
            {
                float a0 = i * Mathf.PI * 2f / sides;
                float a1 = (i + 1) * Mathf.PI * 2f / sides;
                Vector3 p0 = rotation * new Vector3(Mathf.Cos(a0) * r0, y0, Mathf.Sin(a0) * r0);
                Vector3 p1 = rotation * new Vector3(Mathf.Cos(a1) * r0, y0, Mathf.Sin(a1) * r0);
                Vector3 p2 = rotation * new Vector3(Mathf.Cos(a1) * r1, y1, Mathf.Sin(a1) * r1);
                Vector3 p3 = rotation * new Vector3(Mathf.Cos(a0) * r1, y1, Mathf.Sin(a0) * r1);
                PushQuad(v, uv, c, t, p0, p1, p2, p3, Vector2.zero, Vector2.right, Vector2.one, Vector2.up, color);
            }

            int hub = v.Count;
            v.Add(rotation * new Vector3(0f, y1, 0f));
            uv.Add(new Vector2(0.5f, 0.5f));
            c.Add(color);
            for (int i = 0; i <= sides; i++)
            {
                float a = i * Mathf.PI * 2f / sides;
                v.Add(rotation * new Vector3(Mathf.Cos(a) * r1, y1, Mathf.Sin(a) * r1));
                uv.Add(new Vector2(Mathf.Cos(a) * 0.5f + 0.5f, Mathf.Sin(a) * 0.5f + 0.5f));
                c.Add(color);
            }

            for (int i = 0; i < sides; i++)
            {
                t.Add(hub);
                t.Add(hub + 1 + i);
                t.Add(hub + 2 + i);
            }
        }

        static void PlaceTree(
            Transform parent, string resource, float x, float z, float ground,
            float height, float yaw, Shader shader)
        {
            var prefab = Resources.Load<GameObject>(resource);
            if (prefab == null) return;
            var tree = UnityEngine.Object.Instantiate(prefab, parent);
            tree.name = "Arbol";
            foreach (var col in tree.GetComponentsInChildren<Collider>(true))
                UnityEngine.Object.DestroyImmediate(col);
            tree.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
            tree.transform.localPosition = new Vector3(x, ground, z);
            tree.transform.localScale = Vector3.one;
            float native = TreeSpan(tree, out _);
            float scale = height / Mathf.Max(0.2f, native);
            tree.transform.localScale = new Vector3(scale, scale, scale);
            TreeSpan(tree, out float minY);
            Vector3 world = parent.TransformPoint(new Vector3(x, ground, z));
            tree.transform.position += new Vector3(0f, world.y + 0.12f - minY, 0f);
            PaintTree(tree, shader);
        }

        static float TreeSpan(GameObject tree, out float minY)
        {
            var renderers = tree.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
            {
                minY = tree.transform.position.y;
                return 1f;
            }

            Bounds box = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) box.Encapsulate(renderers[i].bounds);
            minY = box.min.y;
            return Mathf.Max(0.2f, box.size.y);
        }

        static void PaintTree(GameObject tree, Shader fallback)
        {
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
                    bool known = TreeTint(mat.name, out Color tint);
                    bool broken = mat.shader == null
                        || mat.shader.name.IndexOf("Error", StringComparison.Ordinal) >= 0
                        || mat.shader.name.IndexOf("Universal", StringComparison.Ordinal) >= 0;
                    if (!known && !broken) continue;
                    Shader use = broken && fallback != null ? fallback : mat.shader;
                    if (use == null) continue;
                    var neu = new Material(use) { name = mat.name };
                    if (known)
                    {
                        if (neu.HasProperty("_Color")) neu.color = tint;
                    }
                    else
                    {
                        Texture tex = mat.mainTexture;
                        if (tex == null && mat.HasProperty("_BaseMap")) tex = mat.GetTexture("_BaseMap");
                        if (tex != null && neu.HasProperty("_MainTex")) neu.mainTexture = tex;
                        if (mat.HasProperty("_Color")) neu.color = mat.color;
                    }

                    mats[m] = neu;
                    changed = true;
                }

                if (changed) renderer.sharedMaterials = mats;
            }
        }

        static bool TreeTint(string name, out Color color)
        {
            if (name.IndexOf("leafsDark", StringComparison.Ordinal) >= 0)
            {
                color = new Color(0.22f, 0.40f, 0.16f);
                return true;
            }

            if (name.IndexOf("leafsGreen", StringComparison.Ordinal) >= 0)
            {
                color = new Color(0.30f, 0.52f, 0.22f);
                return true;
            }

            if (name.IndexOf("woodBarkDark", StringComparison.Ordinal) >= 0)
            {
                color = new Color(0.42f, 0.28f, 0.17f);
                return true;
            }

            if (name.IndexOf("woodBark", StringComparison.Ordinal) >= 0)
            {
                color = new Color(0.52f, 0.36f, 0.22f);
                return true;
            }

            color = Color.white;
            return false;
        }

        static Vector3 FountainRing(float radius, float y, int index, int sides)
        {
            float a = index * Mathf.PI * 2f / Mathf.Max(3, sides);
            return new Vector3(Mathf.Cos(a) * radius, y, Mathf.Sin(a) * radius);
        }

        static void AddDisc(
            List<Vector3> v, List<Vector2> uv, List<Color> c, List<int> t,
            float radius, float y, Color color, int sides)
        {
            int center = v.Count;
            v.Add(new Vector3(0f, y, 0f));
            uv.Add(new Vector2(0.5f, 0.5f));
            c.Add(color);
            for (int i = 0; i <= sides; i++)
            {
                float a = i * Mathf.PI * 2f / sides;
                v.Add(new Vector3(Mathf.Cos(a) * radius, y, Mathf.Sin(a) * radius));
                uv.Add(new Vector2(Mathf.Cos(a) * 0.5f + 0.5f, Mathf.Sin(a) * 0.5f + 0.5f));
                c.Add(color);
            }

            for (int i = 0; i < sides; i++)
            {
                t.Add(center);
                t.Add(center + 1 + i);
                t.Add(center + 2 + i);
            }
        }

        static void AddBox(
            List<Vector3> v, List<Vector2> uv, List<Color> c, List<int> t,
            Vector3 center, Vector3 size, Color color)
        {
            Vector3 h = size * 0.5f;
            Vector3 p000 = center + new Vector3(-h.x, -h.y, -h.z);
            Vector3 p001 = center + new Vector3(-h.x, -h.y, h.z);
            Vector3 p010 = center + new Vector3(-h.x, h.y, -h.z);
            Vector3 p011 = center + new Vector3(-h.x, h.y, h.z);
            Vector3 p100 = center + new Vector3(h.x, -h.y, -h.z);
            Vector3 p101 = center + new Vector3(h.x, -h.y, h.z);
            Vector3 p110 = center + new Vector3(h.x, h.y, -h.z);
            Vector3 p111 = center + new Vector3(h.x, h.y, h.z);
            PushQuad(v, uv, c, t, p001, p101, p111, p011, Vector2.zero, Vector2.right, Vector2.one, Vector2.up, color);
            PushQuad(v, uv, c, t, p100, p000, p010, p110, Vector2.zero, Vector2.right, Vector2.one, Vector2.up, color);
            PushQuad(v, uv, c, t, p000, p001, p011, p010, Vector2.zero, Vector2.right, Vector2.one, Vector2.up, color);
            PushQuad(v, uv, c, t, p101, p100, p110, p111, Vector2.zero, Vector2.right, Vector2.one, Vector2.up, color);
            PushQuad(v, uv, c, t, p010, p011, p111, p110, Vector2.zero, Vector2.right, Vector2.one, Vector2.up, color);
        }

        static void AddBox(
            List<Vector3> v, List<Vector2> uv, List<Color> c, List<int> t,
            Vector3 center, Quaternion rotation, Vector3 size, Color color)
        {
            Vector3 h = size * 0.5f;
            Vector3 P(float x, float y, float z)
            {
                return center + rotation * new Vector3(x, y, z);
            }

            Vector3 p000 = P(-h.x, -h.y, -h.z);
            Vector3 p001 = P(-h.x, -h.y, h.z);
            Vector3 p010 = P(-h.x, h.y, -h.z);
            Vector3 p011 = P(-h.x, h.y, h.z);
            Vector3 p100 = P(h.x, -h.y, -h.z);
            Vector3 p101 = P(h.x, -h.y, h.z);
            Vector3 p110 = P(h.x, h.y, -h.z);
            Vector3 p111 = P(h.x, h.y, h.z);
            PushQuad(v, uv, c, t, p001, p101, p111, p011, Vector2.zero, Vector2.right, Vector2.one, Vector2.up, color);
            PushQuad(v, uv, c, t, p100, p000, p010, p110, Vector2.zero, Vector2.right, Vector2.one, Vector2.up, color);
            PushQuad(v, uv, c, t, p000, p001, p011, p010, Vector2.zero, Vector2.right, Vector2.one, Vector2.up, color);
            PushQuad(v, uv, c, t, p101, p100, p110, p111, Vector2.zero, Vector2.right, Vector2.one, Vector2.up, color);
            PushQuad(v, uv, c, t, p010, p011, p111, p110, Vector2.zero, Vector2.right, Vector2.one, Vector2.up, color);
        }

        static void StampUvQuad(
            List<Vector3> v, List<Vector2> uv, List<Color> c, List<int> t,
            CajamarcaHeightField field, float u0, float v0, float u1, float v1, float lift, Color color)
        {
            StampUvCorners(v, uv, c, t, field, u0, v0, u0, v1, u1, v1, u1, v0, lift, color);
        }

        static void StampUvCorners(
            List<Vector3> v, List<Vector2> uv, List<Color> c, List<int> t,
            CajamarcaHeightField field,
            float u0, float v0, float u1, float v1, float u2, float v2, float u3, float v3,
            float lift, Color color)
        {
            Vector2 a = PlazaToWorld(u0, v0);
            Vector2 b = PlazaToWorld(u1, v1);
            Vector2 d = PlazaToWorld(u2, v2);
            Vector2 e = PlazaToWorld(u3, v3);
            PushQuad(v, uv, c, t,
                new Vector3(a.x, Height(field, a.x, a.y) + lift, a.y),
                new Vector3(b.x, Height(field, b.x, b.y) + lift, b.y),
                new Vector3(d.x, Height(field, d.x, d.y) + lift, d.y),
                new Vector3(e.x, Height(field, e.x, e.y) + lift, e.y),
                new Vector2(u0 / 4f, v0 / 4f), new Vector2(u1 / 4f, v1 / 4f),
                new Vector2(u2 / 4f, v2 / 4f), new Vector2(u3 / 4f, v3 / 4f),
                color);
        }

        static void PublishMesh(
            Transform parent, string name,
            List<Vector3> verts, List<Vector2> uvs, List<Color> cols, List<int> tris, Material material)
        {
            if (verts.Count < 3 || tris.Count < 3 || material == null) return;
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var mesh = new Mesh { name = name };
            mesh.SetVertices(verts);
            mesh.SetUVs(0, uvs);
            mesh.SetColors(cols);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            Finish(go, mesh, material);
        }

        static float Height(CajamarcaHeightField field, float x, float z)
        {
            return field.Sample(x, z) * CajamarcaAltura.RelieveCalles;
        }

        static Vector2 PlazaToWorld(float u, float v)
        {
            float rad = PlazaAngleDeg * Mathf.Deg2Rad;
            float c = Mathf.Cos(rad);
            float s = Mathf.Sin(rad);
            return new Vector2(u * c - v * s, u * s + v * c);
        }

        static void EncapsulatePlaza(
            ref bool boxed, ref float minX, ref float maxX, ref float minZ, ref float maxZ)
        {
            Vector2 a = PlazaToWorld(-PlazaHalf, -PlazaHalf);
            Vector2 b = PlazaToWorld(-PlazaHalf, PlazaHalf);
            Vector2 c = PlazaToWorld(PlazaHalf, -PlazaHalf);
            Vector2 d = PlazaToWorld(PlazaHalf, PlazaHalf);
            Encapsulate(ref boxed, ref minX, ref maxX, ref minZ, ref maxZ, a.x, a.y);
            Encapsulate(ref boxed, ref minX, ref maxX, ref minZ, ref maxZ, b.x, b.y);
            Encapsulate(ref boxed, ref minX, ref maxX, ref minZ, ref maxZ, c.x, c.y);
            Encapsulate(ref boxed, ref minX, ref maxX, ref minZ, ref maxZ, d.x, d.y);
        }

        static float Clearance(List<Span> spans, float x, float z)
        {
            if (spans == null || spans.Count == 0) return 999f;
            float best = 999f;
            var p = new Vector2(x, z);
            for (int i = 0; i < spans.Count; i++)
            {
                float d = DistanceToSegment(p, spans[i].a, spans[i].b) - spans[i].half;
                if (d < best) best = d;
            }

            return best;
        }

        static float RoadClearance(List<Span> spans, float x, float z)
        {
            if (spans == null || spans.Count == 0) return 999f;
            float best = 999f;
            var p = new Vector2(x, z);
            for (int i = 0; i < spans.Count; i++)
            {
                if (spans[i].half < 2f) continue;
                float d = DistanceToSegment(p, spans[i].a, spans[i].b) - spans[i].half;
                if (d < best) best = d;
            }

            return best;
        }

        static float SquareRadius(float radians)
        {
            float m = Mathf.Max(Mathf.Abs(Mathf.Cos(radians)), Mathf.Abs(Mathf.Sin(radians)));
            return PlazaHalf / Mathf.Max(0.25f, m);
        }

        static float GrassOuter(float radians, float promenade)
        {
            return SquareRadius(radians) - promenade - 0.2f;
        }

        static void StampBand(
            List<Vector3> v, List<Vector2> uv, List<Color> c, List<int> t,
            CajamarcaHeightField field, List<Span> spans,
            float u0, float v0, float u1, float v1, float lift)
        {
            int nu = Mathf.Max(1, Mathf.CeilToInt(Mathf.Abs(u1 - u0) / 4f));
            int nv = Mathf.Max(1, Mathf.CeilToInt(Mathf.Abs(v1 - v0) / 4f));
            for (int j = 0; j < nv; j++)
            {
                float va = Mathf.Lerp(v0, v1, j / (float)nv);
                float vb = Mathf.Lerp(v0, v1, (j + 1) / (float)nv);
                for (int i = 0; i < nu; i++)
                {
                    float ua = Mathf.Lerp(u0, u1, i / (float)nu);
                    float ub = Mathf.Lerp(u0, u1, (i + 1) / (float)nu);
                    Vector2 mid = PlazaToWorld((ua + ub) * 0.5f, (va + vb) * 0.5f);
                    if (RoadClearance(spans, mid.x, mid.y) < 0.85f) continue;
                    StampUvCorners(v, uv, c, t, field, ua, va, ub, va, ub, vb, ua, vb, lift, Color.white);
                }
            }
        }

        static float DistanceToSegment(Vector2 p, Vector2 a, Vector2 b)
        {
            Vector2 ab = b - a;
            float len2 = ab.sqrMagnitude;
            if (len2 < 1e-6f) return Vector2.Distance(p, a);
            float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / len2);
            return Vector2.Distance(p, a + ab * t);
        }

        static bool BuildHouse(
            Transform parent, HouseDto house, CajamarcaHeightField field,
            Material adobe, Material tile, Material front, Color wash, bool church)
        {
            if (house == null || house.ring == null || house.ring.Length < 3) return false;
            int n = house.ring.Length;
            if (n > 64) return false;
            float area = RingArea(house.ring);
            if (Mathf.Abs(area) < 8f) return false;

            var ring = new Vector2[n];
            for (int i = 0; i < n; i++) ring[i] = new Vector2(house.ring[i].x, house.ring[i].z);
            int door = Mod(house.door, n);
            if (area < 0f)
            {
                Array.Reverse(ring);
                door = Mod(n - 2 - house.door, n);
            }

            var bottom = new float[n];
            float roofY = float.MinValue;
            for (int i = 0; i < n; i++)
            {
                bottom[i] = Height(field, ring[i].x, ring[i].y);
                if (bottom[i] > roofY) roofY = bottom[i];
            }

            float height = church
                ? (house.height > 2.5f ? house.height : 9f)
                : ColonialWall;
            roofY += height;

            var wallV = new List<Vector3>();
            var wallU = new List<Vector2>();
            var wallC = new List<Color>();
            var wallT = new List<int>();
            var roofV = new List<Vector3>();
            var roofU = new List<Vector2>();
            var roofC = new List<Color>();
            var roofT = new List<int>();
            var faceV = new List<Vector3>();
            var faceU = new List<Vector2>();
            var faceC = new List<Color>();
            var faceT = new List<int>();

            var roofRing = new List<Vector2>(n);
            Vector2 center = Vector2.zero;
            for (int i = 0; i < n; i++) center += ring[i];
            center /= n;
            float reach = 0f;
            for (int i = 0; i < n; i++) reach += Vector2.Distance(center, ring[i]);
            reach /= n;
            float scale = reach > 0.4f ? (reach + 0.35f) / reach : 1f;
            for (int i = 0; i < n; i++)
            {
                Vector2 p = center + (ring[i] - center) * scale;
                roofRing.Add(p);
                roofV.Add(new Vector3(p.x, roofY, p.y));
                roofU.Add(new Vector2(p.x / 2.2f, p.y / 2.2f));
                roofC.Add(Color.white);
            }

            var cap = EarClip(roofRing);
            if (cap.Count < 3)
            {
                for (int i = 1; i < n - 1; i++)
                {
                    cap.Add(0);
                    cap.Add(i);
                    cap.Add(i + 1);
                }
            }
            roofT.AddRange(cap);

            for (int i = 0; i < n; i++)
            {
                int j = (i + 1) % n;
                Vector2 a = roofRing[i];
                Vector2 b = roofRing[j];
                float dx = b.x - a.x;
                float dz = b.y - a.y;
                float len = Mathf.Sqrt(dx * dx + dz * dz);
                if (len < 0.05f) continue;
                var skirtA = new Vector3(a.x, roofY, a.y);
                var skirtB = new Vector3(b.x, roofY, b.y);
                var skirtC = new Vector3(b.x, roofY - 0.22f, b.y);
                var skirtD = new Vector3(a.x, roofY - 0.22f, a.y);
                PushQuad(roofV, roofU, roofC, roofT, skirtA, skirtB, skirtC, skirtD,
                    new Vector2(0f, 0.22f / 2.2f), new Vector2(len / 2.2f, 0.22f / 2.2f),
                    new Vector2(len / 2.2f, 0f), new Vector2(0f, 0f), Color.white);
            }

            Color wallWash = Color.Lerp(wash, new Color(0.86f, 0.72f, 0.55f), church ? 0.15f : 0.35f);
            for (int i = 0; i < n; i++)
            {
                int j = (i + 1) % n;
                Vector2 a = ring[i];
                Vector2 b = ring[j];
                float dx = b.x - a.x;
                float dz = b.y - a.y;
                float len = Mathf.Sqrt(dx * dx + dz * dz);
                if (len < 0.05f) continue;
                var bottomA = new Vector3(a.x, bottom[i], a.y);
                var bottomB = new Vector3(b.x, bottom[j], b.y);
                var topB = new Vector3(b.x, roofY, b.y);
                var topA = new Vector3(a.x, roofY, a.y);
                bool face = i == door && front != null && front != adobe;
                if (face)
                {
                    float bays = Mathf.Max(1f, Mathf.Round(len / 7.6f));
                    PushQuad(faceV, faceU, faceC, faceT, bottomA, bottomB, topB, topA,
                        Vector2.zero, new Vector2(bays, 0f), new Vector2(bays, 1f), Vector2.up, wash);
                }
                else
                {
                    float vA = (roofY - bottom[i]) / 2.4f;
                    float vB = (roofY - bottom[j]) / 2.4f;
                    PushQuad(wallV, wallU, wallC, wallT, bottomA, bottomB, topB, topA,
                        Vector2.zero, new Vector2(len / 2.4f, 0f),
                        new Vector2(len / 2.4f, vB), new Vector2(0f, vA), wallWash);
                    if (i == door)
                        AddDoor(wallV, wallU, wallC, wallT, bottomA, bottomB, roofY, church);
                }
            }

            if (wallT.Count < 3 || roofT.Count < 3) return false;

            string name = string.IsNullOrEmpty(house.name) ? "Casa" : house.name.Trim();
            var go = new GameObject(Unique(parent, name));
            go.transform.SetParent(parent, false);
            var mesh = new Mesh { name = go.name };
            var verts = new List<Vector3>(wallV.Count + roofV.Count + faceV.Count);
            var uvs = new List<Vector2>(verts.Capacity);
            var cols = new List<Color>(verts.Capacity);
            verts.AddRange(wallV); uvs.AddRange(wallU); cols.AddRange(wallC);
            int roofShift = verts.Count;
            verts.AddRange(roofV); uvs.AddRange(roofU); cols.AddRange(roofC);
            int faceShift = verts.Count;
            verts.AddRange(faceV); uvs.AddRange(faceU); cols.AddRange(faceC);
            Shift(roofT, roofShift);
            Shift(faceT, faceShift);
            mesh.SetVertices(verts);
            mesh.SetUVs(0, uvs);
            mesh.SetColors(cols);
            int subs = faceT.Count >= 3 ? 3 : 2;
            mesh.subMeshCount = subs;
            mesh.SetTriangles(wallT, 0);
            mesh.SetTriangles(roofT, 1);
            if (subs == 3) mesh.SetTriangles(faceT, 2);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            var filter = go.AddComponent<MeshFilter>();
            filter.sharedMesh = mesh;
            var renderer = go.AddComponent<MeshRenderer>();
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.sharedMaterials = subs == 3
                ? new[] { adobe, tile, front }
                : new[] { adobe, tile };
            return true;
        }

        static void AddDoor(
            List<Vector3> v, List<Vector2> uv, List<Color> c, List<int> t,
            Vector3 a, Vector3 b, float roofY, bool church)
        {
            Vector3 edge = b - a;
            edge.y = 0f;
            float len = edge.magnitude;
            if (len < 1.2f) return;
            Vector3 dir = edge / len;
            Vector3 outward = new Vector3(dir.z, 0f, -dir.x);
            float width = Mathf.Min(church ? 2.1f : 1.25f, len * 0.46f);
            float y0 = Mathf.Min(a.y, b.y);
            float doorH = Mathf.Min(church ? 3.5f : 2.25f, roofY - y0 - 0.35f);
            if (doorH < 1.4f) return;
            Vector3 mid = (a + b) * 0.5f;
            mid.y = 0f;
            Vector3 left = mid - dir * (width * 0.5f) + outward * 0.05f;
            Vector3 right = mid + dir * (width * 0.5f) + outward * 0.05f;
            var color = new Color(0.32f, 0.16f, 0.09f);
            PushQuad(v, uv, c, t,
                new Vector3(left.x, y0, left.z),
                new Vector3(right.x, y0, right.z),
                new Vector3(right.x, y0 + doorH, right.z),
                new Vector3(left.x, y0 + doorH, left.z),
                Vector2.zero, Vector2.right, Vector2.one, Vector2.up, color);
        }

        static void BuildTerrain(
            Transform district, CajamarcaHeightField field, Material material,
            float minX, float maxX, float minZ, float maxZ)
        {
            const float step = 8f;
            int nx = Mathf.Clamp(Mathf.CeilToInt((maxX - minX) / step), 2, 220);
            int nz = Mathf.Clamp(Mathf.CeilToInt((maxZ - minZ) / step), 2, 220);
            float sx = (maxX - minX) / nx;
            float sz = (maxZ - minZ) / nz;
            int vx = nx + 1;
            int vz = nz + 1;
            var verts = new Vector3[vx * vz];
            var uv = new Vector2[vx * vz];
            var colors = new Color[vx * vz];
            var earth = new Color(0.74f, 0.60f, 0.42f);
            var grass = new Color(0.48f, 0.64f, 0.34f);
            for (int j = 0; j < vz; j++)
            {
                for (int i = 0; i < vx; i++)
                {
                    float x = minX + i * sx;
                    float z = minZ + j * sz;
                    float h = Height(field, x, z);
                    int k = j * vx + i;
                    verts[k] = new Vector3(x, h - GroundDrop, z);
                    uv[k] = new Vector2(x / 16f, z / 16f);
                    colors[k] = Color.Lerp(earth, grass, Mathf.InverseLerp(6.4f, 108.8f, h));
                }
            }

            var tris = new int[nx * nz * 6];
            int t = 0;
            for (int j = 0; j < nz; j++)
            {
                for (int i = 0; i < nx; i++)
                {
                    int a = j * vx + i;
                    int b = a + 1;
                    int c = a + vx;
                    int d = c + 1;
                    tris[t++] = a; tris[t++] = b; tris[t++] = d;
                    tris[t++] = a; tris[t++] = d; tris[t++] = c;
                }
            }

            var go = new GameObject(CajamarcaRelieve.TerrainName);
            go.transform.SetParent(district, false);
            var mesh = new Mesh { name = "TerrenoRuta", indexFormat = IndexFormat.UInt32 };
            mesh.vertices = verts;
            mesh.uv = uv;
            mesh.colors = colors;
            mesh.triangles = tris;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            Finish(go, mesh, material);
        }

        static float NearestStreetGap(Transform rutas, CajamarcaHeightField field, out float streetY, out float demY, out float distance)
        {
            streetY = 0f;
            demY = Height(field, SummitX, SummitZ);
            distance = 999f;
            float worst = 0f;
            foreach (var filter in rutas.GetComponentsInChildren<MeshFilter>(true))
            {
                var mesh = filter.sharedMesh;
                if (mesh == null) continue;
                var verts = mesh.vertices;
                for (int i = 0; i < verts.Length; i++)
                {
                    Vector3 v = verts[i];
                    float gap = Mathf.Abs(v.y - (Height(field, v.x, v.z) + StreetLift));
                    if (gap > worst) worst = gap;
                    float dx = v.x - SummitX;
                    float dz = v.z - SummitZ;
                    float d = Mathf.Sqrt(dx * dx + dz * dz);
                    if (d < distance)
                    {
                        distance = d;
                        streetY = v.y;
                        demY = Height(field, v.x, v.z);
                    }
                }
            }

            return worst;
        }

        static List<Vector2> Subdivide(Xz[] pts, float step)
        {
            var line = new List<Vector2>(pts.Length * 2);
            line.Add(new Vector2(pts[0].x, pts[0].z));
            for (int i = 1; i < pts.Length; i++)
            {
                Vector2 a = new Vector2(pts[i - 1].x, pts[i - 1].z);
                Vector2 b = new Vector2(pts[i].x, pts[i].z);
                float len = Vector2.Distance(a, b);
                int pieces = Mathf.Max(1, Mathf.CeilToInt(len / step));
                for (int k = 1; k <= pieces; k++)
                    line.Add(Vector2.Lerp(a, b, k / (float)pieces));
            }

            return line;
        }

        static void PushQuad(
            List<Vector3> v, List<Vector2> uv, List<Color> c, List<int> t,
            Vector3 a, Vector3 b, Vector3 c0, Vector3 d,
            Vector2 ua, Vector2 ub, Vector2 uc, Vector2 ud, Color color)
        {
            int i = v.Count;
            v.Add(a); v.Add(b); v.Add(c0); v.Add(d);
            uv.Add(ua); uv.Add(ub); uv.Add(uc); uv.Add(ud);
            c.Add(color); c.Add(color); c.Add(color); c.Add(color);
            t.Add(i); t.Add(i + 1); t.Add(i + 2);
            t.Add(i); t.Add(i + 2); t.Add(i + 3);
        }

        static void Shift(List<int> tris, int shift)
        {
            for (int i = 0; i < tris.Count; i++) tris[i] += shift;
        }

        static List<int> EarClip(List<Vector2> poly)
        {
            var tris = new List<int>();
            var idx = new List<int>(poly.Count);
            for (int i = 0; i < poly.Count; i++) idx.Add(i);
            int guard = poly.Count * poly.Count;
            while (idx.Count > 3 && guard-- > 0)
            {
                bool clipped = false;
                for (int i = 0; i < idx.Count; i++)
                {
                    int i0 = idx[(i + idx.Count - 1) % idx.Count];
                    int i1 = idx[i];
                    int i2 = idx[(i + 1) % idx.Count];
                    if (Cross(poly[i0], poly[i1], poly[i2]) <= 1e-5f) continue;
                    bool hit = false;
                    for (int j = 0; j < idx.Count; j++)
                    {
                        int k = idx[j];
                        if (k == i0 || k == i1 || k == i2) continue;
                        if (InTri(poly[k], poly[i0], poly[i1], poly[i2]))
                        {
                            hit = true;
                            break;
                        }
                    }

                    if (hit) continue;
                    tris.Add(i0); tris.Add(i1); tris.Add(i2);
                    idx.RemoveAt(i);
                    clipped = true;
                    break;
                }

                if (!clipped) break;
            }

            if (idx.Count == 3)
            {
                tris.Add(idx[0]); tris.Add(idx[1]); tris.Add(idx[2]);
            }

            return tris;
        }

        static bool InTri(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
        {
            float c1 = Cross(a, b, p);
            float c2 = Cross(b, c, p);
            float c3 = Cross(c, a, p);
            bool neg = c1 < -1e-5f || c2 < -1e-5f || c3 < -1e-5f;
            bool pos = c1 > 1e-5f || c2 > 1e-5f || c3 > 1e-5f;
            return !(neg && pos);
        }

        static float Cross(Vector2 a, Vector2 b, Vector2 c)
        {
            return (b.x - a.x) * (c.y - a.y) - (b.y - a.y) * (c.x - a.x);
        }

        static float RingArea(Xz[] ring)
        {
            float area = 0f;
            for (int i = 0; i < ring.Length; i++)
            {
                Xz a = ring[i];
                Xz b = ring[(i + 1) % ring.Length];
                area += a.x * b.z - b.x * a.z;
            }

            return area * 0.5f;
        }

        static void Finish(GameObject go, Mesh mesh, Material material)
        {
            var filter = go.AddComponent<MeshFilter>();
            filter.sharedMesh = mesh;
            var renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }

        static Material MakeMat(Shader shader, string name, string resource, bool repeat)
        {
            var material = new Material(shader) { name = name };
            if (string.IsNullOrEmpty(resource)) return material;
            var texture = Resources.Load<Texture2D>(resource);
            if (texture == null) return material;
            texture.wrapMode = repeat ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
            material.mainTexture = texture;
            return material;
        }

        static string Unique(Transform parent, string name)
        {
            name = Clean(name);
            string candidate = name;
            int n = 2;
            while (parent.Find(candidate) != null)
            {
                candidate = name + " " + n;
                n++;
            }

            return candidate;
        }

        static string Clean(string name)
        {
            if (string.IsNullOrEmpty(name)) return "Casa";
            char[] bad = { '\\', '/', ':', '*', '?', '"', '<', '>', '|' };
            for (int i = 0; i < bad.Length; i++) name = name.Replace(bad[i], ' ');
            return name.Trim();
        }

        static string KindLabel(string kind)
        {
            if (kind == "steps") return "escalinata";
            if (kind == "footway" || kind == "path") return "sendero";
            if (kind == "service") return "servicio";
            if (kind == "track") return "camino";
            if (kind == "pedestrian") return "peatonal";
            return "via";
        }

        static bool StoneKind(string kind)
        {
            return kind == "steps" || kind == "footway" || kind == "path";
        }

        static bool Has(string name, string word)
        {
            return !string.IsNullOrEmpty(name) &&
                   name.IndexOf(word, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        static int Mod(int value, int count)
        {
            if (count <= 0) return 0;
            int m = value % count;
            return m < 0 ? m + count : m;
        }

        static void Encapsulate(
            ref bool boxed, ref float minX, ref float maxX, ref float minZ, ref float maxZ,
            float x, float z)
        {
            if (!boxed)
            {
                minX = maxX = x;
                minZ = maxZ = z;
                boxed = true;
                return;
            }

            if (x < minX) minX = x;
            if (x > maxX) maxX = x;
            if (z < minZ) minZ = z;
            if (z > maxZ) maxZ = z;
        }
    }
}
#endif
