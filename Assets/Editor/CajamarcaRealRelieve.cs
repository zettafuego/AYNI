#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using AYNI.Battle;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace AYNI.Editor
{
    /// <summary>
    /// Aplica la topografía fidedigna del valle de Cajamarca con relieve DEM,
    /// Cerro Santa Apolonia en su ubicación real, mirador, capilla, silla del inca,
    /// escalinatas de piedra y la paleta de iluminación violeta.
    /// </summary>
    public static class CajamarcaRealRelieve
    {
        public const string TerrainName = AyniBakedCajamarcaReal.TerrainName;
        const float MeshScale = 100f;
        const float TerrainTile = 18f;
        const float CoreDrop = 0.42f;
        const float Plinth = 0.70f;
        const float SkirtDepth = 0.90f;
        const float WeldGrid = 0.05f;
        const float ClusterGap = 0.9f;

        // Paleta de iluminación solicitada por el usuario
        public static readonly Color ColorHighlight = new Color(0.886f, 0.808f, 0.965f); // #E2CEF6
        public static readonly Color ColorOrchid    = new Color(0.851f, 0.502f, 0.976f); // #D980F9
        public static readonly Color ColorPurple    = new Color(0.686f, 0.314f, 0.898f); // #AF50E5
        public static readonly Color ColorViolet    = new Color(0.541f, 0.043f, 0.824f); // #8A0BD2
        public static readonly Color ColorShadow    = new Color(0.357f, 0.024f, 0.447f); // #5B0672
        public static readonly Color SkyBlue        = new Color(0.12f, 0.44f, 0.86f);    // Cielo azul andino

        static readonly string[] GroundNodes =
        {
            "PlanoCalles", "PlanoBanda", "PlanoPlazas", "PlanoAgua",
        };

        static readonly string[] WallNodes = { "PlanoMuros", "PlanoPiedra" };
        static readonly string[] RoofNodes = { "PlanoTechos" };
        static readonly string[] DoorNodes =
        {
            "PlanoPuertas", "PlanoPortadaCatedral", "PlanoPortadaFrancisco",
        };

        public static void Apply(
            Transform district, GameObject plan, CajamarcaHeightField field, Shader shader)
        {
            if (district == null) throw new ArgumentNullException(nameof(district));
            if (plan == null) throw new ArgumentNullException(nameof(plan));
            if (field == null) throw new ArgumentNullException(nameof(field));

            for (int i = 0; i < GroundNodes.Length; i++)
                DrapeGround(plan.transform.Find(GroundNodes[i]), field, GroundNodes[i] != "PlanoAgua");

            DrapeBuildings(plan, field);

            Transform suelo = plan.transform.Find("PlanoSuelo");
            Material soilMaterial = null;
            if (suelo != null)
            {
                var sueloRenderer = suelo.GetComponent<Renderer>();
                if (sueloRenderer != null) soilMaterial = sueloRenderer.sharedMaterial;
            }

            if (suelo != null) UnityEngine.Object.DestroyImmediate(suelo.gameObject);

            BuildTerrain(district, field, shader, soilMaterial);
            BuildSantaApolonia(district, field, shader);
            SnapDistrict(district, plan, field);
        }

        // ── Suelo, calles, plazas y agua ─────────────────────────────────────
        static void DrapeGround(Transform node, CajamarcaHeightField field, bool skirt)
        {
            if (node == null) return;
            var filter = node.GetComponent<MeshFilter>();
            if (filter == null || filter.sharedMesh == null) return;

            var mesh = UnityEngine.Object.Instantiate(filter.sharedMesh);
            mesh.name = filter.sharedMesh.name + "_real_relieve";
            var verts = mesh.vertices;
            for (int i = 0; i < verts.Length; i++)
            {
                Vector3 v = verts[i];
                float h = field.Sample(v.x * MeshScale, -v.y * MeshScale);
                v.z += h / MeshScale;
                verts[i] = v;
            }

            mesh.vertices = verts;
            if (skirt) AppendSkirt(mesh, SkirtDepth / MeshScale);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            filter.sharedMesh = mesh;
        }

        static void AppendSkirt(Mesh mesh, float depth)
        {
            var verts = mesh.vertices;
            var uv = mesh.uv;
            var colors = mesh.colors32;
            var tris = mesh.triangles;
            if (verts.Length == 0 || tris.Length == 0 || depth <= 0f) return;

            var canon = new Dictionary<(int, int, int), int>(verts.Length);
            var rep = new int[verts.Length];
            for (int i = 0; i < verts.Length; i++)
            {
                Vector3 v = verts[i];
                var key = (Mathf.RoundToInt(v.x * 10000f),
                           Mathf.RoundToInt(v.y * 10000f),
                           Mathf.RoundToInt(v.z * 10000f));
                if (canon.TryGetValue(key, out int first)) rep[i] = first;
                else
                {
                    canon[key] = i;
                    rep[i] = i;
                }
            }

            var edges = new Dictionary<long, int>();
            for (int i = 0; i < tris.Length; i += 3)
            {
                CountEdge(edges, rep[tris[i]], rep[tris[i + 1]]);
                CountEdge(edges, rep[tris[i + 1]], rep[tris[i + 2]]);
                CountEdge(edges, rep[tris[i + 2]], rep[tris[i]]);
            }

            var vlist = new List<Vector3>(verts);
            var uvlist = new List<Vector2>(uv.Length == verts.Length ? uv : new Vector2[verts.Length]);
            var clist = colors.Length == verts.Length ? new List<Color32>(colors) : null;
            var skirt = new List<int>();

            foreach (var pair in edges)
            {
                if (pair.Value != 1) continue;
                int a = (int)(pair.Key >> 32);
                int b = (int)(pair.Key & 0xFFFFFFFFL);
                Vector3 pa = verts[a];
                Vector3 pb = verts[b];
                Vector3 down = new Vector3(0f, 0f, -depth);

                int i0 = vlist.Count;
                vlist.Add(pa);
                vlist.Add(pb);
                vlist.Add(pb + down);
                vlist.Add(pa + down);

                Vector2 ua = uvlist[a];
                Vector2 ub = uvlist[b];
                uvlist.Add(ua);
                uvlist.Add(ub);
                uvlist.Add(ub);
                uvlist.Add(ua);

                if (clist != null)
                {
                    Color32 ca = clist[a];
                    Color32 cb = clist[b];
                    clist.Add(ca);
                    clist.Add(cb);
                    clist.Add(cb);
                    clist.Add(ca);
                }

                skirt.Add(i0); skirt.Add(i0 + 1); skirt.Add(i0 + 2);
                skirt.Add(i0); skirt.Add(i0 + 2); skirt.Add(i0 + 3);
            }

            if (skirt.Count == 0) return;

            var sub0 = new List<int>(mesh.GetTriangles(0));
            sub0.AddRange(skirt);
            mesh.SetVertices(vlist);
            if (uvlist.Count == vlist.Count) mesh.SetUVs(0, uvlist);
            if (clist != null && clist.Count == vlist.Count) mesh.SetColors(clist);
            mesh.SetTriangles(sub0, 0);
        }

        static void CountEdge(Dictionary<long, int> edges, int a, int b)
        {
            int lo = Mathf.Min(a, b);
            int hi = Mathf.Max(a, b);
            long key = ((long)lo << 32) | (uint)hi;
            edges.TryGetValue(key, out int count);
            edges[key] = count + 1;
        }

        // ── Edificios ────────────────────────────────────────────────────────
        sealed class Piece
        {
            public MeshFilter filter;
            public Mesh mesh;
            public Vector3[] verts;
            public bool wall;
            public int[] global;
        }

        static void DrapeBuildings(GameObject plan, CajamarcaHeightField field)
        {
            var pieces = new List<Piece>();
            for (int i = 0; i < WallNodes.Length; i++)
                AddPiece(plan, WallNodes[i], true, pieces);
            for (int i = 0; i < RoofNodes.Length; i++)
                AddPiece(plan, RoofNodes[i], false, pieces);

            if (pieces.Count > 0) DrapeRigid(pieces, field);

            var doors = new List<Piece>();
            for (int i = 0; i < DoorNodes.Length; i++)
                AddPiece(plan, DoorNodes[i], false, doors);
            if (doors.Count > 0) DrapeDoors(doors, field);
        }

        static void AddPiece(GameObject plan, string name, bool wall, List<Piece> pieces)
        {
            Transform node = plan.transform.Find(name);
            if (node == null) return;
            var filter = node.GetComponent<MeshFilter>();
            if (filter == null || filter.sharedMesh == null) return;

            var mesh = UnityEngine.Object.Instantiate(filter.sharedMesh);
            mesh.name = filter.sharedMesh.name + "_real_relieve";
            var raw = mesh.vertices;
            var verts = new Vector3[raw.Length];
            for (int i = 0; i < raw.Length; i++)
                verts[i] = new Vector3(raw[i].x * MeshScale, raw[i].z * MeshScale, -raw[i].y * MeshScale);

            pieces.Add(new Piece
            {
                filter = filter,
                mesh = mesh,
                verts = verts,
                wall = wall,
                global = new int[raw.Length],
            });
        }

        static void DrapeRigid(List<Piece> pieces, CajamarcaHeightField field)
        {
            var positions = new List<Vector3>();
            var owner = new List<int>();
            var weld = new Dictionary<long, int>();

            for (int p = 0; p < pieces.Count; p++)
            {
                var piece = pieces[p];
                for (int i = 0; i < piece.verts.Length; i++)
                {
                    Vector3 v = piece.verts[i];
                    int id = positions.Count;
                    positions.Add(v);
                    owner.Add(p);
                    piece.global[i] = id;

                    long key = WeldKey(v);
                    if (weld.ContainsKey(key)) weld[key] = Math.Min(weld[key], id);
                    else weld[key] = id;
                }
            }

            var uf = new UnionFind(positions.Count);
            foreach (var piece in pieces)
            {
                for (int i = 0; i < piece.verts.Length; i++)
                {
                    long key = WeldKey(piece.verts[i]);
                    if (weld.TryGetValue(key, out int other))
                        uf.Union(piece.global[i], other);
                }
            }

            var islands = new Dictionary<int, Island>();
            for (int i = 0; i < positions.Count; i++)
            {
                int root = uf.Find(i);
                if (!islands.TryGetValue(root, out var island))
                {
                    island = new Island();
                    islands[root] = island;
                }

                island.ids.Add(i);
                island.Grow(positions[i]);
            }

            ClusterIslands(islands, out var islandList, out var clusterOfIsland);

            var clusters = new Dictionary<int, Island>();
            for (int i = 0; i < islandList.Count; i++)
            {
                int cluster = clusterOfIsland[i];
                if (!clusters.TryGetValue(cluster, out var group))
                {
                    group = new Island();
                    clusters[cluster] = group;
                }

                group.Merge(islandList[i]);
            }

            var offsetOf = new Dictionary<int, float>();
            var floorOf = new Dictionary<int, float>();
            foreach (var pair in clusters)
            {
                offsetOf[pair.Key] = Offset(field, pair.Value);
                floorOf[pair.Key] = pair.Value.MinY;
            }

            for (int i = 0; i < islandList.Count; i++)
            {
                Island island = islandList[i];
                int cluster = clusterOfIsland[i];
                float offset = offsetOf[cluster];
                float floor = floorOf[cluster];
                foreach (int id in island.ids)
                {
                    Piece piece = pieces[owner[id]];
                    Vector3 v = positions[id];
                    if (piece.wall && v.y <= floor + 0.65f)
                        v.y = field.Sample(v.x, v.z) - Plinth;
                    else
                        v.y += offset;
                    positions[id] = v;
                    piece.verts[id - piece.global[0]] = v;
                }
            }

            for (int p = 0; p < pieces.Count; p++) Write(pieces[p]);
        }

        static void DrapeDoors(List<Piece> pieces, CajamarcaHeightField field)
        {
            foreach (var piece in pieces)
            {
                var uf = new UnionFind(piece.verts.Length);
                var weld = new Dictionary<long, int>();
                for (int i = 0; i < piece.verts.Length; i++)
                {
                    long key = WeldKey(piece.verts[i]);
                    if (weld.TryGetValue(key, out int other)) uf.Union(i, other);
                    else weld[key] = i;
                }

                var boxes = new Dictionary<int, Island>();
                for (int i = 0; i < piece.verts.Length; i++)
                {
                    int root = uf.Find(i);
                    if (!boxes.TryGetValue(root, out var island))
                    {
                        island = new Island();
                        boxes[root] = island;
                    }

                    island.ids.Add(i);
                    island.Grow(piece.verts[i]);
                }

                foreach (var island in boxes.Values)
                {
                    float offset = Offset(field, island);
                    for (int k = 0; k < island.ids.Count; k++)
                    {
                        int id = island.ids[k];
                        Vector3 v = piece.verts[id];
                        v.y += offset;
                        piece.verts[id] = v;
                    }
                }

                Write(piece);
            }
        }

        static void Write(Piece piece)
        {
            var raw = new Vector3[piece.verts.Length];
            for (int i = 0; i < raw.Length; i++)
            {
                Vector3 v = piece.verts[i];
                raw[i] = new Vector3(v.x / MeshScale, -v.z / MeshScale, v.y / MeshScale);
            }

            piece.mesh.vertices = raw;
            piece.mesh.RecalculateNormals();
            piece.mesh.RecalculateBounds();
            piece.filter.sharedMesh = piece.mesh;
        }

        sealed class Island
        {
            public readonly List<int> ids = new List<int>();
            public float MinX = float.MaxValue, MaxX = float.MinValue;
            public float MinZ = float.MaxValue, MaxZ = float.MinValue;
            public float MinY = float.MaxValue, MaxY = float.MinValue;

            public void Grow(Vector3 v)
            {
                if (v.x < MinX) MinX = v.x;
                if (v.x > MaxX) MaxX = v.x;
                if (v.z < MinZ) MinZ = v.z;
                if (v.z > MaxZ) MaxZ = v.z;
                if (v.y < MinY) MinY = v.y;
                if (v.y > MaxY) MaxY = v.y;
            }

            public void Merge(Island other)
            {
                if (other.MinX < MinX) MinX = other.MinX;
                if (other.MaxX > MaxX) MaxX = other.MaxX;
                if (other.MinZ < MinZ) MinZ = other.MinZ;
                if (other.MaxZ > MaxZ) MaxZ = other.MaxZ;
                if (other.MinY < MinY) MinY = other.MinY;
                if (other.MaxY > MaxY) MaxY = other.MaxY;
            }
        }

        static void ClusterIslands(
            Dictionary<int, Island> islands, out List<Island> list, out Dictionary<int, int> clusterOfIsland)
        {
            list = new List<Island>(islands.Values);
            var uf = new UnionFind(list.Count);
            const float cell = 8f;
            var grid = new Dictionary<long, List<int>>();

            for (int i = 0; i < list.Count; i++)
            {
                Island island = list[i];
                int x0 = Mathf.FloorToInt(island.MinX / cell);
                int x1 = Mathf.FloorToInt(island.MaxX / cell);
                int z0 = Mathf.FloorToInt(island.MinZ / cell);
                int z1 = Mathf.FloorToInt(island.MaxZ / cell);
                for (int x = x0; x <= x1; x++)
                {
                    for (int z = z0; z <= z1; z++)
                    {
                        long key = CellKey(x, z);
                        if (!grid.TryGetValue(key, out var bucket))
                        {
                            bucket = new List<int>();
                            grid[key] = bucket;
                        }

                        if (!bucket.Contains(i)) bucket.Add(i);
                    }
                }
            }

            for (int i = 0; i < list.Count; i++)
            {
                Island a = list[i];
                int x0 = Mathf.FloorToInt((a.MinX - ClusterGap) / cell);
                int x1 = Mathf.FloorToInt((a.MaxX + ClusterGap) / cell);
                int z0 = Mathf.FloorToInt((a.MinZ - ClusterGap) / cell);
                int z1 = Mathf.FloorToInt((a.MaxZ + ClusterGap) / cell);
                for (int x = x0; x <= x1; x++)
                {
                    for (int z = z0; z <= z1; z++)
                    {
                        if (!grid.TryGetValue(CellKey(x, z), out var bucket)) continue;
                        for (int k = 0; k < bucket.Count; k++)
                        {
                            int j = bucket[k];
                            if (j <= i) continue;
                            Island b = list[j];
                            if (a.MinX - ClusterGap > b.MaxX || b.MinX - ClusterGap > a.MaxX) continue;
                            if (a.MinZ - ClusterGap > b.MaxZ || b.MinZ - ClusterGap > a.MaxZ) continue;
                            uf.Union(i, j);
                        }
                    }
                }
            }

            clusterOfIsland = new Dictionary<int, int>();
            for (int i = 0; i < list.Count; i++) clusterOfIsland[i] = uf.Find(i);
        }

        static float Offset(CajamarcaHeightField field, Island island)
        {
            float[] samples =
            {
                field.Sample((island.MinX + island.MaxX) * 0.5f, (island.MinZ + island.MaxZ) * 0.5f),
                field.Sample(Mathf.Lerp(island.MinX, island.MaxX, 0.25f), Mathf.Lerp(island.MinZ, island.MaxZ, 0.25f)),
                field.Sample(Mathf.Lerp(island.MinX, island.MaxX, 0.75f), Mathf.Lerp(island.MinZ, island.MaxZ, 0.75f)),
                field.Sample(Mathf.Lerp(island.MinX, island.MaxX, 0.25f), Mathf.Lerp(island.MinZ, island.MaxZ, 0.75f)),
                field.Sample(Mathf.Lerp(island.MinX, island.MaxX, 0.75f), Mathf.Lerp(island.MinZ, island.MaxZ, 0.25f)),
            };
            Array.Sort(samples);
            return samples[samples.Length / 2];
        }

        static long WeldKey(Vector3 v)
        {
            long x = (long)Mathf.Round(v.x / WeldGrid) + 200000L;
            long y = (long)Mathf.Round(v.y / WeldGrid) + 200000L;
            long z = (long)Mathf.Round(v.z / WeldGrid) + 200000L;
            return (x << 40) | (y << 20) | z;
        }

        static long CellKey(int x, int z) => ((long)x << 32) ^ (uint)z;

        sealed class UnionFind
        {
            readonly int[] parent;
            public UnionFind(int count)
            {
                parent = new int[count];
                for (int i = 0; i < count; i++) parent[i] = i;
            }
            public int Find(int i)
            {
                while (parent[i] != i)
                {
                    parent[i] = parent[parent[i]];
                    i = parent[i];
                }
                return i;
            }
            public void Union(int a, int b)
            {
                int ra = Find(a);
                int rb = Find(b);
                if (ra != rb) parent[rb] = ra;
            }
        }

        // ── Terreno Real del Valle ───────────────────────────────────────────
        static void BuildTerrain(
            Transform district, CajamarcaHeightField field, Shader shader, Material soilMaterial)
        {
            int n = field.Count;
            float extent = field.Extent;
            float step = field.Step;

            var verts = new Vector3[n * n];
            var uv = new Vector2[n * n];
            var colors = new Color32[n * n];

            for (int j = 0; j < n; j++)
            {
                float z = -extent + j * step;
                for (int i = 0; i < n; i++)
                {
                    float x = -extent + i * step;
                    float h = field.Sample(x, z) - CoreDropAt(x, z);
                    int k = j * n + i;
                    verts[k] = new Vector3(x, h, z);
                    uv[k] = new Vector2(x / TerrainTile, z / TerrainTile);
                    colors[k] = TerrainColor(x, z, h);
                }
            }

            var city = new List<int>((n - 1) * (n - 1) * 3);
            var hills = new List<int>((n - 1) * (n - 1) * 3);

            for (int j = 0; j < n - 1; j++)
            {
                for (int i = 0; i < n - 1; i++)
                {
                    int a = j * n + i;
                    int b = a + 1;
                    int c = a + n;
                    int d = c + 1;
                    float cx = (verts[a].x + verts[b].x + verts[c].x + verts[d].x) * 0.25f;
                    float cz = (verts[a].z + verts[b].z + verts[c].z + verts[d].z) * 0.25f;
                    var list = Core(cx, cz) ? city : hills;
                    list.Add(a); list.Add(c); list.Add(b);
                    list.Add(b); list.Add(c); list.Add(d);
                }
            }

            var mesh = new Mesh { name = "CajamarcaRealTerreno", indexFormat = IndexFormat.UInt32 };
            mesh.vertices = verts;
            mesh.uv = uv;
            mesh.colors32 = colors;
            mesh.subMeshCount = 2;
            mesh.SetTriangles(city, 0);
            mesh.SetTriangles(hills, 1);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            var go = new GameObject(TerrainName);
            go.transform.SetParent(district, false);
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = Vector3.one;

            var filter = go.AddComponent<MeshFilter>();
            filter.sharedMesh = mesh;

            var realShader = Shader.Find("AYNI/CajamarcaReal") ?? shader ?? Shader.Find("Standard");

            var materials = new Material[2];
            var cityMat = new Material(realShader);
            cityMat.name = "TerrenoCiudad_Mat";
            if (soilMaterial != null && soilMaterial.mainTexture != null)
                cityMat.mainTexture = soilMaterial.mainTexture;
            else
                cityMat.mainTexture = Resources.Load<Texture2D>("Calle/piso_tierra");
            cityMat.mainTextureScale = new Vector2(TerrainTile / 8f, TerrainTile / 8f);
            materials[0] = cityMat;

            var hillMat = new Material(realShader);
            hillMat.name = "TerrenoLaderas_Mat";
            var texture = Resources.Load<Texture2D>("Bosque/piso_cesped");
            if (texture != null)
            {
                texture.wrapMode = TextureWrapMode.Repeat;
                hillMat.mainTexture = texture;
            }
            hillMat.mainTextureScale = Vector2.one;
            materials[1] = hillMat;

            var renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterials = materials;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }

        static Color32 TerrainColor(float x, float z, float height)
        {
            float core = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(1f, 0.55f, CoreMetric(x, z)));

            // Tinte de tierra del casco
            Color earth = new Color(0.84f, 0.78f, 0.72f);
            // Verde andino con suave reflejo lavanda de la paleta
            Color grass = new Color(0.68f, 0.72f, 0.56f);
            // Crestas altas rocosas que toman el violeta de la atmósfera
            Color ridge = Color.Lerp(new Color(0.78f, 0.72f, 0.65f), ColorOrchid, 0.22f);

            float h = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(25f, 180f, height));
            Color slope = Color.Lerp(grass, ridge, h);

            // Reflejo violeta en las cumbres del sur y oeste (Santa Apolonia y Cumbe Mayo)
            if (x < -100f && z < -100f)
            {
                float apoloniaProximity = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(30f, 80f, height));
                slope = Color.Lerp(slope, ColorPurple, apoloniaProximity * 0.18f);
            }

            return Color.Lerp(slope, earth, core);
        }

        const float CoreX = 870f;
        const float CoreZ = 745f;
        static bool Core(float x, float z) => CoreMetric(x, z) <= 1f;

        public static float CoreDropAt(float x, float z)
        {
            float fade = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.85f, 1f, CoreMetric(x, z)));
            return CoreDrop * (1f - fade);
        }

        static float CoreMetric(float x, float z)
        {
            float nx = Mathf.Abs(x) / CoreX;
            float nz = Mathf.Abs(z) / CoreZ;
            float a = nx * nx * nx;
            float b = nz * nz * nz;
            a *= a;
            b *= b;
            return a * a + b * b;
        }

        // ── Cerro Santa Apolonia, Mirador, Capilla, Silla del Inca y Escalinatas ─
        static void BuildSantaApolonia(Transform district, CajamarcaHeightField field, Shader shader)
        {
            var realShader = Shader.Find("AYNI/CajamarcaReal") ?? shader ?? Shader.Find("Standard");
            var root = new GameObject(AyniBakedCajamarcaReal.SantaApoloniaName);
            root.transform.SetParent(district, false);

            // Mirador de Santa Apolonia, medido contra el DEM:
            // -7.16047, -78.51910 → 184 m al oeste y 386 m al sur de la plaza.
            // La cumbre queda a +76 m. De la plaza hasta ahí la calle sube.
            float summitX = -184f;
            float summitZ = -386f;
            float summitY = field.Sample(summitX, summitZ);

            // Materiales de piedra y caliza
            var stoneMat = new Material(realShader) { name = "SantaApolonia_Piedra" };
            var stoneTex = Resources.Load<Texture2D>("Calle/piedra");
            if (stoneTex != null) stoneMat.mainTexture = stoneTex;

            var chapelMat = new Material(realShader) { name = "SantaApolonia_Capilla" };
            var adobeTex = Resources.Load<Texture2D>("Calle/muro_adobe");
            if (adobeTex != null) chapelMat.mainTexture = adobeTex;
            chapelMat.color = new Color(0.96f, 0.94f, 0.98f); // Blanco colonial con tinte lila

            var roofMat = new Material(realShader) { name = "SantaApolonia_Teja" };
            var roofTex = Resources.Load<Texture2D>("Calle/teja");
            if (roofTex != null) roofMat.mainTexture = roofTex;

            // 1. Terraza Mirador (Plataforma semicircular / poligonal sobre la cumbre)
            BuildMiradorTerrace(root.transform, summitX, summitY, summitZ, stoneMat);

            // 2. Capilla de Santa Apolonia (Virgen de Fátima)
            BuildCapilla(root.transform, summitX + 6f, summitY + 0.35f, summitZ + 12f, chapelMat, roofMat, stoneMat);

            // 3. Silla del Inca (Trono ceremonial tallado en la roca viva)
            BuildSillaDelInca(root.transform, summitX - 16f, summitY + 0.2f, summitZ + 4f, stoneMat);

            // 4. Escalinatas de Santa Apolonia que bajan hacia Jirón Dos de Mayo
            // El pie de la escalinata está donde la pendiente ya se siente,
            // camino a la plaza por Jirón Dos de Mayo.
            BuildEscalinatas(root.transform, field, summitX, summitZ, -90f, -190f, stoneMat);

            // 5. Faroles con luz violeta
            BuildFaroles(root.transform, summitX, summitY, summitZ);
        }

        static void BuildMiradorTerrace(Transform parent, float cx, float cy, float cz, Material mat)
        {
            var go = new GameObject(AyniBakedCajamarcaReal.MiradorName);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3(cx, cy, cz);

            var mesh = new Mesh { name = "MiradorPlataforma" };
            var verts = new List<Vector3>();
            var uvs = new List<Vector2>();
            var tris = new List<int>();

            // Terraza principal octogonal de 28m de diámetro
            int segments = 16;
            float radius = 14f;
            verts.Add(new Vector3(0f, 0.25f, 0f));
            uvs.Add(new Vector2(0.5f, 0.5f));

            for (int i = 0; i <= segments; i++)
            {
                float ang = (i / (float)segments) * Mathf.PI * 2f;
                float x = Mathf.Cos(ang) * radius;
                float z = Mathf.Sin(ang) * radius;
                verts.Add(new Vector3(x, 0.25f, z));
                uvs.Add(new Vector2(x / 4f, z / 4f));
            }

            for (int i = 1; i <= segments; i++)
            {
                tris.Add(0);
                tris.Add(i + 1);
                tris.Add(i);
            }

            // Muro pretil / barandilla de piedra alrededor de la terraza
            for (int i = 1; i <= segments; i++)
            {
                Vector3 p0 = verts[i];
                Vector3 p1 = verts[i + 1];
                Vector3 up = Vector3.up * 0.95f;

                int baseIdx = verts.Count;
                verts.Add(p0);
                verts.Add(p1);
                verts.Add(p1 + up);
                verts.Add(p0 + up);

                uvs.Add(new Vector2(0f, 0f));
                uvs.Add(new Vector2(1f, 0f));
                uvs.Add(new Vector2(1f, 1f));
                uvs.Add(new Vector2(0f, 1f));

                tris.Add(baseIdx); tris.Add(baseIdx + 2); tris.Add(baseIdx + 1);
                tris.Add(baseIdx); tris.Add(baseIdx + 3); tris.Add(baseIdx + 2);
            }

            mesh.SetVertices(verts);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            var mf = go.AddComponent<MeshFilter>();
            mf.sharedMesh = mesh;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = mat;
            mr.shadowCastingMode = ShadowCastingMode.Off;
        }

        static void BuildCapilla(
            Transform parent, float cx, float cy, float cz, Material whiteMat, Material roofMat, Material stoneMat)
        {
            var go = new GameObject(AyniBakedCajamarcaReal.CapillaName);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3(cx, cy, cz);

            // Nave de la capilla
            float w = 7.5f, len = 12.0f, h = 5.2f;
            var nave = GameObject.CreatePrimitive(PrimitiveType.Cube);
            nave.name = "Nave";
            nave.transform.SetParent(go.transform, false);
            nave.transform.localPosition = new Vector3(0f, h * 0.5f, 0f);
            nave.transform.localScale = new Vector3(w, h, len);
            nave.GetComponent<Renderer>().sharedMaterial = whiteMat;
            UnityEngine.Object.DestroyImmediate(nave.GetComponent<Collider>());

            // Techo a dos aguas
            var techo = new GameObject("Techo");
            techo.transform.SetParent(go.transform, false);
            techo.transform.localPosition = new Vector3(0f, h, 0f);
            var roofMesh = CreateGableRoof(w + 1.2f, len + 1.4f, 2.4f);
            var rmf = techo.AddComponent<MeshFilter>();
            rmf.sharedMesh = roofMesh;
            var rmr = techo.AddComponent<MeshRenderer>();
            rmr.sharedMaterial = roofMat;
            rmr.shadowCastingMode = ShadowCastingMode.Off;

            // Campanario / Espadaña colonial frontal con arco
            var espada = GameObject.CreatePrimitive(PrimitiveType.Cube);
            espada.name = "Campanario";
            espada.transform.SetParent(go.transform, false);
            espada.transform.localPosition = new Vector3(0f, h + 2.1f, len * 0.5f - 0.2f);
            espada.transform.localScale = new Vector3(3.2f, 3.8f, 0.6f);
            espada.GetComponent<Renderer>().sharedMaterial = whiteMat;
            UnityEngine.Object.DestroyImmediate(espada.GetComponent<Collider>());

            // Cruz en lo alto de la capilla
            var cruzV = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cruzV.name = "CruzVertical";
            cruzV.transform.SetParent(go.transform, false);
            cruzV.transform.localPosition = new Vector3(0f, h + 4.6f, len * 0.5f - 0.2f);
            cruzV.transform.localScale = new Vector3(0.24f, 1.6f, 0.24f);
            cruzV.GetComponent<Renderer>().sharedMaterial = stoneMat;
            UnityEngine.Object.DestroyImmediate(cruzV.GetComponent<Collider>());

            var cruzH = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cruzH.name = "CruzHorizontal";
            cruzH.transform.SetParent(go.transform, false);
            cruzH.transform.localPosition = new Vector3(0f, h + 4.9f, len * 0.5f - 0.2f);
            cruzH.transform.localScale = new Vector3(1.1f, 0.24f, 0.24f);
            cruzH.GetComponent<Renderer>().sharedMaterial = stoneMat;
            UnityEngine.Object.DestroyImmediate(cruzH.GetComponent<Collider>());

            // Puerta principal de madera
            var puerta = GameObject.CreatePrimitive(PrimitiveType.Cube);
            puerta.name = "PuertaCapilla";
            puerta.transform.SetParent(go.transform, false);
            puerta.transform.localPosition = new Vector3(0f, 1.6f, len * 0.5f + 0.05f);
            puerta.transform.localScale = new Vector3(2.2f, 3.2f, 0.2f);
            var doorMat = new Material(whiteMat) { color = new Color(0.28f, 0.16f, 0.10f) };
            puerta.GetComponent<Renderer>().sharedMaterial = doorMat;
            UnityEngine.Object.DestroyImmediate(puerta.GetComponent<Collider>());
        }

        static void BuildSillaDelInca(Transform parent, float cx, float cy, float cz, Material stoneMat)
        {
            var go = new GameObject("SillaDelInca");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3(cx, cy, cz);
            go.transform.localRotation = Quaternion.Euler(0f, 45f, 0f);

            // Altar escalonado ceremonial inca
            float[] stepH = { 0.45f, 0.90f, 1.35f, 1.80f };
            float[] stepW = { 4.6f, 3.6f, 2.6f, 1.8f };
            for (int i = 0; i < stepH.Length; i++)
            {
                var block = GameObject.CreatePrimitive(PrimitiveType.Cube);
                block.name = "Grada_" + i;
                block.transform.SetParent(go.transform, false);
                block.transform.localPosition = new Vector3(0f, stepH[i] * 0.5f, i * 0.5f);
                block.transform.localScale = new Vector3(stepW[i], stepH[i], 1.2f);
                block.GetComponent<Renderer>().sharedMaterial = stoneMat;
                UnityEngine.Object.DestroyImmediate(block.GetComponent<Collider>());
            }

            // Asiento tallado en la parte superior
            var seat = GameObject.CreatePrimitive(PrimitiveType.Cube);
            seat.name = "Trono";
            seat.transform.SetParent(go.transform, false);
            seat.transform.localPosition = new Vector3(0f, 2.3f, 2.0f);
            seat.transform.localScale = new Vector3(1.6f, 1.2f, 1.0f);
            seat.GetComponent<Renderer>().sharedMaterial = stoneMat;
            UnityEngine.Object.DestroyImmediate(seat.GetComponent<Collider>());
        }

        static void BuildEscalinatas(
            Transform parent, CajamarcaHeightField field,
            float topX, float topZ, float botX, float botZ, Material stoneMat)
        {
            var go = new GameObject(AyniBakedCajamarcaReal.StairsName);
            go.transform.SetParent(parent, false);

            int stepCount = 52;
            float stairWidth = 4.2f;

            var mesh = new Mesh { name = "MallaEscalinatas" };
            var verts = new List<Vector3>();
            var uvs = new List<Vector2>();
            var tris = new List<int>();

            Vector3 lastTop = Vector3.zero;
            for (int i = 0; i < stepCount; i++)
            {
                float t0 = (float)i / stepCount;
                float t1 = (float)(i + 1) / stepCount;

                float x0 = Mathf.Lerp(botX, topX, t0);
                float z0 = Mathf.Lerp(botZ, topZ, t0);
                float y0 = field.Sample(x0, z0) + 0.15f;

                float x1 = Mathf.Lerp(botX, topX, t1);
                float z1 = Mathf.Lerp(botZ, topZ, t1);
                float y1 = field.Sample(x1, z1) + 0.15f;

                Vector2 dir = new Vector2(x1 - x0, z1 - z0).normalized;
                Vector2 side = new Vector2(-dir.y, dir.x) * (stairWidth * 0.5f);

                Vector3 pA = new Vector3(x0 - side.x, y0, z0 - side.y);
                Vector3 pB = new Vector3(x0 + side.x, y0, z0 + side.y);
                Vector3 pC = new Vector3(x1 + side.x, y1, z1 + side.y);
                Vector3 pD = new Vector3(x1 - side.x, y1, z1 - side.y);

                int baseIdx = verts.Count;
                verts.Add(pA); verts.Add(pB); verts.Add(pC); verts.Add(pD);
                uvs.Add(new Vector2(0f, 0f));
                uvs.Add(new Vector2(1f, 0f));
                uvs.Add(new Vector2(1f, 1f));
                uvs.Add(new Vector2(0f, 1f));

                tris.Add(baseIdx); tris.Add(baseIdx + 2); tris.Add(baseIdx + 1);
                tris.Add(baseIdx); tris.Add(baseIdx + 3); tris.Add(baseIdx + 2);
            }

            mesh.SetVertices(verts);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            var mf = go.AddComponent<MeshFilter>();
            mf.sharedMesh = mesh;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = stoneMat;
            mr.shadowCastingMode = ShadowCastingMode.Off;
        }

        static void BuildFaroles(Transform parent, float cx, float cy, float cz)
        {
            var farolesRoot = new GameObject("FarolesVioleta");
            farolesRoot.transform.SetParent(parent, false);

            Vector3[] spots =
            {
                new Vector3(cx + 12f, cy + 0.8f, cz),
                new Vector3(cx - 12f, cy + 0.8f, cz),
                new Vector3(cx, cy + 0.8f, cz + 12f),
                new Vector3(cx, cy + 0.8f, cz - 12f),
                new Vector3(cx + 8f, cy + 0.8f, cz + 8f),
                new Vector3(cx - 8f, cy + 0.8f, cz - 8f),
            };

            for (int i = 0; i < spots.Length; i++)
            {
                var f = new GameObject("Farol_" + i);
                f.transform.SetParent(farolesRoot.transform, false);
                f.transform.localPosition = spots[i];

                var light = f.AddComponent<Light>();
                light.type = LightType.Point;
                light.color = ColorOrchid;
                light.range = 16f;
                light.intensity = 2.2f;
                light.shadows = LightShadows.None;
            }
        }

        static Mesh CreateGableRoof(float width, float length, float height)
        {
            var mesh = new Mesh { name = "TechoDosAguas" };
            float hw = width * 0.5f;
            float hl = length * 0.5f;

            var v = new Vector3[]
            {
                new Vector3(-hw, 0f, -hl),
                new Vector3( hw, 0f, -hl),
                new Vector3( 0f, height, -hl),

                new Vector3(-hw, 0f,  hl),
                new Vector3( hw, 0f,  hl),
                new Vector3( 0f, height,  hl),
            };

            var tris = new int[]
            {
                0, 2, 1, // front gable
                3, 4, 5, // back gable
                0, 3, 5,  0, 5, 2, // left slope
                1, 2, 5,  1, 5, 4, // right slope
            };

            var uv = new Vector2[]
            {
                new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 1f),
                new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 1f),
            };

            mesh.vertices = v;
            mesh.triangles = tris;
            mesh.uv = uv;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        public static void SnapDistrict(Transform district, GameObject plan, CajamarcaHeightField field)
        {
            foreach (Transform child in district)
            {
                if (child == null) continue;
                if (child.name == TerrainName || child.name == AyniBakedCajamarcaReal.PlanName) continue;
                if (child.name == AyniBakedCajamarcaReal.SantaApoloniaName) continue;
                if (child.name.StartsWith("__", StringComparison.Ordinal)) continue;
                Vector3 p = child.localPosition;
                child.localPosition = new Vector3(p.x, field.Sample(p.x, p.z) + p.y, p.z);
            }

            Transform labels = plan.transform.Find("NombresCalles");
            if (labels == null) return;
            foreach (Transform label in labels)
            {
                if (label == null) continue;
                Vector3 p = label.localPosition;
                label.localPosition = new Vector3(p.x, field.Sample(p.x, p.z) + 0.22f, p.z);
            }
        }
    }
}
#endif
