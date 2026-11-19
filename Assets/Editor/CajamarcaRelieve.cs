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
    /// Aplica el relieve real (DEM) al plano de Cajamarca:
    ///  · el suelo, las calles y las plazas se drapean vértice a vértice;
    ///  · las casas se mantienen rígidas (una terraza por edificio) con la
    ///    base hundida en la ladera para que no queden flotando;
    ///  · se genera una malla de terreno real que sustituye el quad plano.
    /// Trabaja en el espacio local de las mallas del FBX: 1 unidad = 100 m,
    /// x = este, y = sur (Blender) y z = altura.
    /// </summary>
    public static class CajamarcaRelieve
    {
        public const string TerrainName = "Terreno";
        const float MeshScale = 100f;      // los hijos del plano están a escala 100
        const float TerrainTile = 18f;     // metros por repetición de textura

        // En el plano original PlanoSuelo estaba 0,3 m por debajo de las calles.
        // El terreno nuevo sustituye a ese quad: si se dibuja a la cota del DEM
        // queda coplanar con las calles (z-fighting) y, como la rejilla es de
        // 10 m, sus cuerdas tapan las calles en los tramos cóncavos. Se rebaja
        // 0,45 m en el casco: 0,3 del plano original + margen para la rejilla.
        const float CoreDrop = 0.45f;
        const float Plinth = 0.70f;        // muro enterrado bajo el terreno rebajado
        const float SkirtDepth = 0.90f;    // canto de aceras/calles (tapa el hueco)
        const float WeldGrid = 0.05f;      // tolerancia para unir vértices
        const float ClusterGap = 0.9f;     // une muro y techo del mismo edificio

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

            // El quad plano del suelo se sustituye por el terreno drapeado,
            // pero conserva su material de tierra para el casco urbano.
            Transform suelo = plan.transform.Find("PlanoSuelo");
            Material soilMaterial = null;
            if (suelo != null)
            {
                var sueloRenderer = suelo.GetComponent<Renderer>();
                if (sueloRenderer != null) soilMaterial = sueloRenderer.sharedMaterial;
            }

            RemoveNode(suelo);
            BuildTerrain(district, field, shader, soilMaterial);
            SnapDistrict(district, plan, field);
        }

        // ── Suelo, calles, plazas y agua ─────────────────────────────────────
        static void DrapeGround(Transform node, CajamarcaHeightField field, bool skirt)
        {
            if (node == null) return;
            var filter = node.GetComponent<MeshFilter>();
            if (filter == null || filter.sharedMesh == null) return;

            var mesh = UnityEngine.Object.Instantiate(filter.sharedMesh);
            mesh.name = filter.sharedMesh.name + "_relieve";
            var verts = mesh.vertices;
            for (int i = 0; i < verts.Length; i++)
            {
                Vector3 v = verts[i];
                float h = field.Sample(v.x * MeshScale, -v.y * MeshScale);
                v.z += h / MeshScale;
                verts[i] = v;
            }

            mesh.vertices = verts;
            // Las calles y plazas son superficies sin grosor y el terreno queda
            // 0,45 m más abajo: una falda vertical cierra el canto y evita que
            // se vea el hueco bajo la calzada desde los lados.
            if (skirt) AppendSkirt(mesh, SkirtDepth / MeshScale);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            filter.sharedMesh = mesh;
        }

        /// <summary>
        /// Añade una falda vertical a las aristas de borde de la malla. Las
        /// aristas se cuentan soldando por posición, para que las costuras de
        /// UV no se confundan con el borde real de la calzada.
        /// </summary>
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
            public Vector3[] verts;   // metros: x = este, y = arriba, z = norte
            public bool wall;
            public int[] global;      // índice global de cada vértice
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
            mesh.name = filter.sharedMesh.name + "_relieve";
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

        /// <summary>Muros y techos: una terraza rígida por edificio, base enterrada.</summary>
        static void DrapeRigid(List<Piece> pieces, CajamarcaHeightField field)
        {
            var positions = new List<Vector3>();
            var owner = new List<int>();      // índice global → pieza
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

            // Islas = piezas conexas tras soldar.
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

            // Unir muro + techo + alero del mismo edificio por cercanía.
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
                // La base del edificio es el mínimo de TODO el conjunto: soldar
                // por posición separa el anillo bajo del alto, así que el mínimo
                // de una isla suelta sería el alero, no el suelo.
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
                    piece.verts[IndexOf(piece, id)] = v;
                }
            }

            for (int p = 0; p < pieces.Count; p++) Write(pieces[p]);
        }

        static int IndexOf(Piece piece, int global)
        {
            // piece.global es ordenado: índice local = global - primero.
            return global - piece.global[0];
        }

        /// <summary>Puertas y portadas: cada hoja se apoya en su propio suelo.</summary>
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
                    Vector3[] verts = piece.verts;
                    foreach (int id in island.ids)
                    {
                        Vector3 v = verts[id];
                        v.y += offset;
                        verts[id] = v;
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

            public Vector3 Center => new Vector3((MinX + MaxX) * 0.5f, (MinY + MaxY) * 0.5f, (MinZ + MaxZ) * 0.5f);

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
                int x0 = Mathf.FloorToInt((island.MinX - ClusterGap) / cell);
                int x1 = Mathf.FloorToInt((island.MaxX + ClusterGap) / cell);
                int z0 = Mathf.FloorToInt((island.MinZ - ClusterGap) / cell);
                int z1 = Mathf.FloorToInt((island.MaxZ + ClusterGap) / cell);
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

        static long CellKey(int x, int z)
        {
            return ((long)x << 32) ^ (uint)z;
        }

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

        // ── Terreno ──────────────────────────────────────────────────────────
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

            // Dos sub-mallas con los mismos vértices: tierra del casco urbano
            // (el extinto PlanoSuelo) y pajonal de las laderas. El casco sigue
            // la mancha construida (calles ±813×±684) con una superelipse para
            // que el borde no se vea como un rectángulo de tierra.
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

            var mesh = new Mesh { name = "CajamarcaTerreno", indexFormat = IndexFormat.UInt32 };
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

            var materials = new Material[2];
            if (soilMaterial != null)
            {
                // Las UV del terreno van cada 18 m; el plano usaba 8 m.
                soilMaterial.mainTextureScale = new Vector2(TerrainTile / 8f, TerrainTile / 8f);
                materials[0] = soilMaterial;
            }

            var material = new Material(shader != null ? shader : Shader.Find("Standard"));
            material.name = "Terreno";
            var texture = Resources.Load<Texture2D>("Bosque/piso_cesped");
            if (texture != null)
            {
                texture.wrapMode = TextureWrapMode.Repeat;
                material.mainTexture = texture;
            }

            material.mainTextureScale = Vector2.one;
            material.color = Color.white;
            materials[1] = material;
            if (materials[0] == null) materials[0] = material;

            var renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterials = materials;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }

        static Color32 TerrainColor(float x, float z, float height)
        {
            // Tierra en el casco, pajonal en las laderas; el borde acompaña a
            // la superelipse de la sub-malla para que no se note la costura.
            float core = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(1f, 0.55f, CoreMetric(x, z)));

            Color earth = new Color(0.86f, 0.81f, 0.68f);
            Color grass = new Color(0.72f, 0.76f, 0.48f);
            Color high = new Color(0.80f, 0.75f, 0.55f);
            float h = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(90f, 260f, height));
            Color slope = Color.Lerp(grass, high, h);
            return Color.Lerp(slope, earth, core);
        }

        // ── Casco urbano (superelipse) ───────────────────────────────────────
        // Las calles llegan a ±813 × ±684; el casco deja ~55 m de holgura.
        const float CoreX = 870f;
        const float CoreZ = 745f;

        static bool Core(float x, float z) => CoreMetric(x, z) <= 1f;

        /// <summary>
        /// Rebaje del terreno dentro del casco urbano. Es pleno en toda la
        /// mancha de calles (métrica ≤ 0,85) y se funde a cero en el borde de
        /// la superelipse, en una franja de ~15 m, para no dejar un escalón
        /// visible en las laderas.
        /// </summary>
        public static float CoreDropAt(float x, float z)
        {
            float fade = Mathf.SmoothStep(
                0f, 1f, Mathf.InverseLerp(0.85f, 1f, CoreMetric(x, z)));
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

        // ── Objetos sueltos del distrito ─────────────────────────────────────
        public static void SnapDistrict(Transform district, GameObject plan, CajamarcaHeightField field)
        {
            foreach (Transform child in district)
            {
                if (child == null) continue;
                if (child.name == TerrainName || child.name == AyniBakedCajamarca.PlanName) continue;
                if (child.name.StartsWith("__", StringComparison.Ordinal)) continue;
                Vector3 p = child.localPosition;
                child.localPosition = new Vector3(p.x, field.Sample(p.x, p.z) + p.y, p.z);
            }

            // Césped, senderos y jardines nacen como losas planas. En la plaza
            // el valle ya sube varios metros, y hacia Santa Apolonia la subida
            // pasa de 0 a 76 m. Cada vértice se apoya en el DEM.
            // Esto va antes del retorno de los letreros: en el horneado todavía
            // no existe NombresCalles.
            DrapeSheets(district, field);

            Transform labels = plan.transform.Find("NombresCalles");
            if (labels == null) return;
            foreach (Transform label in labels)
            {
                if (label == null) continue;
                Vector3 p = label.localPosition;
                label.localPosition = new Vector3(p.x, field.Sample(p.x, p.z) + 0.22f, p.z);
            }
        }

        static readonly string[] SheetNames = { "Cesped", "Senderos", "Jardines" };
        const float SheetEdge = 8f;

        static void DrapeSheets(Transform district, CajamarcaHeightField field)
        {
            for (int i = 0; i < SheetNames.Length; i++)
            {
                Transform sheet = district.Find(SheetNames[i]);
                if (sheet == null) continue;
                var filters = sheet.GetComponentsInChildren<MeshFilter>(true);
                for (int f = 0; f < filters.Length; f++)
                    DrapeSheet(district, filters[f], field);
            }
        }

        static void DrapeSheet(Transform district, MeshFilter filter, CajamarcaHeightField field)
        {
            Mesh source = filter.sharedMesh;
            if (source == null || source.vertexCount == 0) return;

            var mesh = UnityEngine.Object.Instantiate(source);
            mesh.name = source.name;
            var local = mesh.vertices;
            var srcUv = mesh.uv;
            var srcColor = mesh.colors32;
            bool hasUv = srcUv != null && srcUv.Length == local.Length;
            bool hasColor = srcColor != null && srcColor.Length == local.Length;
            var tris = mesh.triangles;

            var verts = new List<Vector3>(local.Length);
            var clear = new List<float>(local.Length);
            var uvs = new List<Vector2>(local.Length);
            var colors = new List<Color32>(local.Length);
            for (int i = 0; i < local.Length; i++)
            {
                Vector3 at = district.InverseTransformPoint(filter.transform.TransformPoint(local[i]));
                verts.Add(at);
                clear.Add(local[i].y);
                uvs.Add(hasUv ? srcUv[i] : Vector2.zero);
                colors.Add(hasColor ? srcColor[i] : new Color32(255, 255, 255, 255));
            }

            var faces = new List<int>(tris);
            for (int pass = 0; pass < 8; pass++)
            {
                bool split = false;
                var next = new List<int>(faces.Count);
                for (int t = 0; t < faces.Count; t += 3)
                {
                    int a = faces[t];
                    int b = faces[t + 1];
                    int c = faces[t + 2];
                    float ab = Flat(verts[a], verts[b]);
                    float bc = Flat(verts[b], verts[c]);
                    float ca = Flat(verts[c], verts[a]);
                    if (ab <= SheetEdge && bc <= SheetEdge && ca <= SheetEdge)
                    {
                        next.Add(a);
                        next.Add(b);
                        next.Add(c);
                        continue;
                    }

                    split = true;
                    int s0 = a;
                    int s1 = b;
                    float longest = ab;
                    if (bc > longest) { longest = bc; s0 = b; s1 = c; }
                    if (ca > longest) { s0 = c; s1 = a; }
                    int mid = verts.Count;
                    verts.Add((verts[s0] + verts[s1]) * 0.5f);
                    clear.Add((clear[s0] + clear[s1]) * 0.5f);
                    uvs.Add((uvs[s0] + uvs[s1]) * 0.5f);
                    colors.Add(Color32.Lerp(colors[s0], colors[s1], 0.5f));

                    int other = a + b + c - s0 - s1;
                    next.Add(s0); next.Add(mid); next.Add(other);
                    next.Add(mid); next.Add(s1); next.Add(other);
                }

                faces = next;
                if (!split) break;
            }

            for (int i = 0; i < verts.Count; i++)
            {
                Vector3 v = verts[i];
                v.y = field.Sample(v.x, v.z) + clear[i];
                verts[i] = filter.transform.InverseTransformPoint(district.TransformPoint(v));
            }

            mesh.indexFormat = verts.Count > 65000
                ? IndexFormat.UInt32
                : IndexFormat.UInt16;
            mesh.SetVertices(verts);
            if (hasUv) mesh.SetUVs(0, uvs);
            if (hasColor) mesh.SetColors(colors);
            mesh.SetTriangles(faces, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            filter.sharedMesh = mesh;
        }

        static float Flat(Vector3 a, Vector3 b)
        {
            float dx = a.x - b.x;
            float dz = a.z - b.z;
            return Mathf.Sqrt(dx * dx + dz * dz);
        }

        static void RemoveNode(Transform node)
        {
            if (node != null) UnityEngine.Object.DestroyImmediate(node.gameObject);
        }
    }
}
#endif
