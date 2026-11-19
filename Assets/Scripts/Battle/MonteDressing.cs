using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace AYNI.Battle
{
    /// <summary>
    /// Dirt slab, ichu, reeds, bushes, rocks, cantuta and one apacheta.
    /// One mesh sits behind the fighters. Short tufts draw in front of their feet.
    /// Vertex alpha is wind weight, not transparency. The camera stays side-on.
    /// </summary>
    public class MonteDressing : MonoBehaviour
    {
        static readonly Color SoilDeep = new Color(0.16f, 0.10f, 0.07f);
        static readonly Color Soil = new Color(0.28f, 0.16f, 0.09f);
        static readonly Color Ochre = new Color(0.45f, 0.28f, 0.14f);
        static readonly Color Sod = new Color(0.22f, 0.32f, 0.11f);
        static readonly Color Turf = new Color(0.35f, 0.55f, 0.18f);
        static readonly Color IchuDry = new Color(0.72f, 0.68f, 0.32f);
        static readonly Color IchuTip = new Color(0.88f, 0.84f, 0.52f);
        static readonly Color IchuGreen = new Color(0.40f, 0.55f, 0.18f);
        static readonly Color IchuGreenTip = new Color(0.62f, 0.70f, 0.30f);
        static readonly Color Reed = new Color(0.22f, 0.42f, 0.28f);
        static readonly Color ReedTip = new Color(0.55f, 0.62f, 0.30f);
        static readonly Color Bush = new Color(0.16f, 0.38f, 0.14f);
        static readonly Color BushRim = new Color(0.34f, 0.58f, 0.22f);
        static readonly Color Rock = new Color(0.42f, 0.40f, 0.38f);
        static readonly Color RockDark = new Color(0.26f, 0.24f, 0.22f);
        static readonly Color CantutaRed = new Color(0.85f, 0.15f, 0.28f);
        static readonly Color CantutaGold = new Color(0.93f, 0.72f, 0.18f);
        static readonly Color Stem = new Color(0.28f, 0.36f, 0.16f);

        const int Behind = 5;
        const int InFront = 16;

        Material mat;

        public static MonteDressing Ensure(Transform host)
        {
            var found = host.GetComponent<MonteDressing>();
            if (found == null) found = host.gameObject.AddComponent<MonteDressing>();
            return found;
        }

        public static void Dress(BattlePlatform platform)
        {
            if (platform == null) return;
            var host = FindFirstObjectByType<MonteDressing>();
            if (host == null)
            {
                var go = new GameObject("MonteDressing");
                host = go.AddComponent<MonteDressing>();
            }
            host.DressPlatform(platform);
        }

        public void BuildField(float floorY, float minX, float maxX)
        {
            ClearChildren();
            var rng = new System.Random(710);
            var field = new Loom();
            var front = new Loom();

            Suelo(field, rng, floorY, minX - 14f, RioDeEntrada.Left);
            Suelo(field, rng, floorY, RioDeEntrada.Right, maxX + 14f);
            Scatter(rng, 48, minX, maxX, x => { if (RioDeEntrada.Blocks(x, 0.4f)) return; Ichu(field, rng, x, floorY, false); });
            Scatter(rng, 16, minX, maxX, x => { if (RioDeEntrada.Blocks(x, 0.4f)) return; Junco(field, rng, x, floorY); });
            Scatter(rng, 10, minX, maxX, x => { if (RioDeEntrada.Blocks(x, 0.8f)) return; Arbusto(field, rng, x, floorY); });
            Scatter(rng, 18, minX, maxX, x => { if (RioDeEntrada.Blocks(x, 0.8f)) return; Roca(field, rng, x, floorY, Range(rng, 0.28f, 0.7f), Range(rng, 0.22f, 0.55f), Range(rng, 0.2f, 1.2f)); });
            Roca(field, rng, minX + 1.1f, floorY, 1.05f, 0.85f, 0.8f);
            Roca(field, rng, maxX - 1.1f, floorY, 1.15f, 0.95f, 0.9f);
            Scatter(rng, 12, minX, maxX, x => { if (RioDeEntrada.Blocks(x, 0.4f)) return; Cantuta(field, rng, x, floorY); });
            Apacheta(field, rng, 2.4f, floorY);
            Scatter(rng, 22, minX, maxX, x => { if (RioDeEntrada.Blocks(x, 0.3f)) return; Ichu(front, rng, x, floorY, true); });

            Attach(transform, "SueloYMonte", field, Behind, 0f);
            Attach(transform, "IchuDelante", front, InFront, 0f);
        }

        public void ExtendRoad(float floorY, float fromX, float toX)
        {
            var rng = new System.Random(711);
            var field = new Loom();
            var front = new Loom();

            Cerro(field, 44f, floorY);
            Boca(field, rng, 63f, floorY);
            Suelo(field, rng, floorY, fromX, toX);
            Casa(field, 80f, floorY, 2.5f, 1.85f);
            Casa(field, 97.5f, floorY, 2.15f, 1.6f);
            PaintedHouse(floorY);
            PaintedHen(floorY);
            Scatter(rng, 14, fromX, 54f, x => Ichu(field, rng, x, floorY, false));
            Scatter(rng, 8, 74f, toX - 8f, x => Cantuta(field, rng, x, floorY));
            Scatter(rng, 12, fromX, toX - 4f, x => Roca(field, rng, x, floorY, Range(rng, 0.18f, 0.42f), Range(rng, 0.12f, 0.26f), 0.35f));
            Scatter(rng, 18, fromX, toX - 4f, x => Ichu(front, rng, x, floorY, true));

            Attach(transform, "Camino", field, Behind, 0.2f);
            Attach(transform, "CaminoDelante", front, InFront, 0.2f);
        }

        public void DressPlatform(BattlePlatform platform)
        {
            var sr = platform.GetComponent<SpriteRenderer>();
            if (sr != null) sr.enabled = false;

            var old = platform.transform.Find("MonteDePiedra");
            if (old != null) Destroy(old.gameObject);

            var root = new GameObject("MonteDePiedra");
            root.transform.SetParent(platform.transform, false);

            var rng = new System.Random(Mathf.RoundToInt(platform.transform.position.x * 10f) + 3);
            float phase = platform.transform.position.x * 0.37f;

            var stone = new Loom();
            Ledge(stone, rng);
            Attach(root.transform, "Piedra", stone, Behind, phase);

            var grass = new Loom();
            int blades = 3 + rng.Next(3);
            for (int i = 0; i < blades; i++)
            {
                bool dry = rng.NextDouble() > 0.4;
                Blade(grass, Range(rng, -0.36f, 0.36f), 0.5f, 0f,
                    Range(rng, 0.22f, 0.4f), Range(rng, -0.05f, 0.05f), 0.018f,
                    Tint(rng, dry ? IchuDry : IchuGreen, 0.08f),
                    Tint(rng, dry ? IchuTip : IchuGreenTip, 0.08f));
            }
            Attach(root.transform, "Ichu", grass, InFront, phase);
        }

        void ClearChildren()
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                var child = transform.GetChild(i);
                if (child.name == "SueloYMonte" || child.name == "IchuDelante")
                    Destroy(child.gameObject);
            }
        }

        void Attach(Transform parent, string meshName, Loom loom, int order, float phase)
        {
            if (loom.Empty) return;
            var go = new GameObject(meshName);
            go.transform.SetParent(parent, false);
            var filter = go.AddComponent<MeshFilter>();
            filter.sharedMesh = loom.Bake(meshName);
            var renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = SharedMat();
            renderer.sortingOrder = order;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            if (Mathf.Abs(phase) > 0.001f)
            {
                var block = new MaterialPropertyBlock();
                block.SetFloat("_Phase", phase);
                renderer.SetPropertyBlock(block);
            }
        }

        Material SharedMat()
        {
            if (mat != null) return mat;
            var shader = Shader.Find("AYNI/Monte");
            if (shader == null)
            {
                Debug.LogWarning("[AYNI] No está el shader AYNI/Monte. El monte queda sin viento.");
                shader = Shader.Find("Sprites/Default") ?? Shader.Find("Standard") ?? Shader.Find("Unlit/Texture");
            }
            mat = new Material(shader) { name = "Monte" };
            if (mat.HasProperty("_Wind")) mat.SetFloat("_Wind", 0.28f);
            if (mat.HasProperty("_Speed")) mat.SetFloat("_Speed", 1.7f);
            if (shader != null && (shader.name == "Sprites/Default" || shader.name == "Standard"))
                mat.mainTexture = Texture2D.whiteTexture;
            return mat;
        }

        void Suelo(Loom loom, System.Random rng, float floorY, float x0, float x1)
        {
            var xs = new List<float>();
            for (float x = x0; x < x1 - 0.2f; x += 1.05f) xs.Add(x);
            xs.Add(x1);

            var lip = new float[xs.Count];
            for (int i = 0; i < xs.Count; i++)
                lip[i] = floorY + (float)rng.NextDouble() * 0.07f;

            var nDeep = new Vector3(0.05f, -0.35f, -1f);
            var nSoil = new Vector3(0f, 0.05f, -1f);
            var nTurf = new Vector3(0f, 0.85f, -0.55f);

            for (int i = 0; i < xs.Count - 1; i++)
            {
                float a = xs[i];
                float b = xs[i + 1];
                Strata(floorY, a, out float deepA, out float rockA, out float ochreA, out float soilA);
                Strata(floorY, b, out float deepB, out float rockB, out float ochreB, out float soilB);
                Band(loom, a, b, deepA, deepB, rockA, rockB, SoilDeep, Soil, nDeep);
                Band(loom, a, b, rockA, rockB, ochreA, ochreB, Soil, Ochre, nSoil);
                Band(loom, a, b, ochreA, ochreB, soilA, soilB, Ochre, Sod, nSoil);
                Band(loom, a, b, soilA, soilB, lip[i], lip[i + 1], Sod, Turf, nTurf);

                if (rng.NextDouble() > 0.45)
                {
                    float px = (a + b) * 0.5f;
                    float py = floorY - Range(rng, 0.45f, 2.4f);
                    Roca(loom, rng, px, py, Range(rng, 0.12f, 0.28f), Range(rng, 0.08f, 0.18f), 0.15f);
                }
            }

            for (float x = x0; x < x1; x += 0.36f)
            {
                bool dry = rng.NextDouble() > 0.55;
                Blade(loom, x + Range(rng, -0.05f, 0.05f), floorY, Range(rng, 0.15f, 1.1f),
                    Range(rng, 0.16f, 0.42f), Range(rng, -0.18f, 0.18f), 0.028f,
                    Tint(rng, dry ? IchuDry : IchuGreen, 0.1f),
                    Tint(rng, dry ? IchuTip : IchuGreenTip, 0.08f));
            }
        }

        static void Strata(float floorY, float x, out float deep, out float rock, out float ochre, out float soil)
        {
            deep = floorY - 3.55f;
            rock = floorY - 2.15f + Mathf.Sin(x * 0.27f) * 0.1f;
            ochre = floorY - 0.82f + Mathf.Sin(x * 0.19f + 0.8f) * 0.07f;
            soil = floorY - 0.16f + Mathf.Sin(x * 0.41f) * 0.025f;
        }

        void Ledge(Loom loom, System.Random rng)
        {
            const int cols = 5;
            var nStone = new Vector3(0f, 0.1f, -1f);
            var nTurf = new Vector3(0f, 0.9f, -0.4f);
            for (int i = 0; i < cols; i++)
            {
                float a = -0.5f + i / (float)cols;
                float b = a + 1f / cols;
                float lipA = 0.5f + (float)rng.NextDouble() * 0.08f;
                float lipB = 0.5f + (float)rng.NextDouble() * 0.08f;
                Band(loom, a, b, -0.5f, -0.5f, -0.02f, -0.02f, RockDark, Rock, nStone);
                Band(loom, a, b, -0.02f, -0.02f, 0.30f, 0.30f, Rock, Lighten(Rock, 0.08f), nStone);
                Band(loom, a, b, 0.30f, 0.30f, lipA, lipB, Sod, Turf, nTurf);
            }
        }

        void Band(Loom loom, float x0, float x1, float y0a, float y0b, float y1a, float y1b, Color bottom, Color top, Vector3 normal)
        {
            var c0 = Ink(bottom, 0f);
            var c1 = Ink(top, 0f);
            int a = loom.Add(new Vector3(x0, y0a, 0f), normal, c0);
            int b = loom.Add(new Vector3(x1, y0b, 0f), normal, c0);
            int c = loom.Add(new Vector3(x1, y1b, 0f), normal, c1);
            int d = loom.Add(new Vector3(x0, y1a, 0f), normal, c1);
            loom.Quad(a, b, c, d);
        }

        void Ichu(Loom loom, System.Random rng, float x, float floorY, bool front)
        {
            int n = front ? 4 + rng.Next(2) : 6 + rng.Next(4);
            float z = front ? Range(rng, -1.05f, -0.35f) : Range(rng, 0.35f, 1.9f);
            bool dry = rng.NextDouble() > 0.42;
            var root = Tint(rng, dry ? IchuDry : IchuGreen, 0.1f);
            var tip = Tint(rng, dry ? IchuTip : IchuGreenTip, 0.08f);
            for (int i = 0; i < n; i++)
            {
                float h = front ? Range(rng, 0.1f, 0.2f) : Range(rng, 0.48f, 1.15f);
                Blade(loom, x + Range(rng, -0.16f, 0.16f), floorY, z + i * 0.03f, h,
                    Range(rng, -0.32f, 0.32f), front ? 0.018f : Range(rng, 0.026f, 0.048f), root, tip);
            }
        }

        void Junco(Loom loom, System.Random rng, float x, float floorY)
        {
            int n = 5 + rng.Next(3);
            float z = Range(rng, 0.4f, 1.7f);
            var root = Tint(rng, Reed, 0.1f);
            var tip = Tint(rng, ReedTip, 0.08f);
            for (int i = 0; i < n; i++)
            {
                Blade(loom, x + Range(rng, -0.16f, 0.16f), floorY, z + i * 0.04f,
                    Range(rng, 1.2f, 2.2f), Range(rng, -0.2f, 0.2f), Range(rng, 0.016f, 0.028f), root, tip);
            }
        }

        void Arbusto(Loom loom, System.Random rng, float x, float floorY)
        {
            float z = Range(rng, 0.45f, 1.6f);
            int puffs = 3 + rng.Next(3);
            for (int i = 0; i < puffs; i++)
            {
                float rx = Range(rng, 0.22f, 0.5f);
                float ry = Range(rng, 0.16f, 0.36f);
                var center = new Vector3(x + Range(rng, -0.22f, 0.22f), floorY + ry * 0.72f, z + i * 0.02f);
                Fan(loom, rng, center, rx, ry, Tint(rng, Bush, 0.08f), Tint(rng, BushRim, 0.1f));
            }
        }

        void Cantuta(Loom loom, System.Random rng, float x, float floorY)
        {
            float z = Range(rng, 0.3f, 1.4f);
            float h = Range(rng, 0.7f, 1.15f);
            float lean = Range(rng, -0.12f, 0.12f);
            Blade(loom, x, floorY, z, h, lean, 0.016f, Stem, IchuGreen);
            var top = new Vector3(x + lean, floorY + h, z);
            bool gold = rng.NextDouble() > 0.72;
            var petal = gold ? CantutaGold : CantutaRed;
            int petals = 5;
            for (int i = 0; i < petals; i++)
            {
                float ang = (-0.9f + 1.8f * i / (petals - 1)) + Range(rng, -0.08f, 0.08f);
                var tip = top + new Vector3(Mathf.Sin(ang) * 0.16f, Mathf.Cos(ang) * 0.14f, 0f);
                Petal(loom, top, tip, 0.045f, Tint(rng, petal, 0.06f));
            }
        }

        void Apacheta(Loom loom, System.Random rng, float x, float floorY)
        {
            float y = floorY;
            float w = 0.72f;
            float h = 0.16f;
            for (int i = 0; i < 4; i++)
            {
                Roca(loom, rng, x + Range(rng, -0.04f, 0.04f), y, w, h, 0.55f);
                y += h * 0.72f;
                w *= 0.72f;
                h *= 0.92f;
            }
        }

        void Cerro(Loom loom, float x, float floorY)
        {
            var rng = new System.Random(44);
            FanRock(loom, rng, x, floorY, 6.4f, 5.1f, 0.9f, new Color(0.17f, 0.15f, 0.17f));
            FanRock(loom, rng, x + 3.4f, floorY, 3.2f, 2.9f, 0.8f, new Color(0.26f, 0.22f, 0.24f));
            FanRock(loom, rng, x - 2.6f, floorY, 2.5f, 2.1f, 0.75f, new Color(0.21f, 0.19f, 0.21f));
        }

        void Boca(Loom loom, System.Random rng, float x, float floorY)
        {
            FanRock(loom, rng, x, floorY, 3.5f, 3.2f, 0.55f, new Color(0.11f, 0.09f, 0.11f));
            FanRock(loom, rng, x, floorY + 0.35f, 1.35f, 1.65f, 0.4f, new Color(0.04f, 0.03f, 0.05f));
            FanRock(loom, rng, x + 0.05f, floorY + 1.62f, 0.2f, 0.36f, 0.32f, CantutaGold);
        }

        void Casa(Loom loom, float x, float floorY, float w, float h)
        {
            var wall = new Color(0.62f, 0.38f, 0.22f);
            var wallDark = new Color(0.40f, 0.22f, 0.13f);
            var roof = new Color(0.48f, 0.16f, 0.13f);
            var door = new Color(0.14f, 0.09f, 0.07f);
            float left = x - w * 0.5f;
            float right = x + w * 0.5f;
            var face = new Vector3(0f, 0.25f, -1f);
            Band(loom, left, right, floorY, floorY, floorY + h, floorY + h, wallDark, wall, face);
            Band(loom, left - 0.16f, right + 0.16f, floorY + h * 0.92f, floorY + h * 0.92f, floorY + h + 0.42f, floorY + h + 0.5f, roof, Lighten(roof, 0.1f), face);
            float dw = Mathf.Max(0.28f, w * 0.18f);
            Band(loom, x - dw, x + dw, floorY, floorY, floorY + h * 0.48f, floorY + h * 0.48f, door, door, face);
        }

        void PaintedHouse(float floorY)
        {
            var tex = Resources.Load<Texture2D>("Calle/casa_adobe");
            if (tex == null) return;
            var sprite = Sprite.Create(tex, new Rect(0f, 0f, tex.width, tex.height), new Vector2(0.5f, 0f), 100f);
            var go = new GameObject("CasaAdobe");
            go.transform.SetParent(transform, false);
            go.transform.localPosition = new Vector3(89f, floorY, -0.5f);
            float native = tex.height / 100f;
            float scale = native > 0.01f ? 3.6f / native : 0.34f;
            go.transform.localScale = new Vector3(scale, scale, 1f);
            var ink = go.AddComponent<SpriteRenderer>();
            ink.sprite = sprite;
            ink.sortingOrder = 6;
        }

        void PaintedHen(float floorY)
        {
            var frames = BattleRuntimeAssets.LoadClip("Calle/Gallina/Conducir");
            if (frames.Length == 0) return;
            var car = new GameObject("GallinaConductora");
            car.transform.SetParent(transform, false);
            car.transform.localPosition = new Vector3(78f, floorY, -0.8f);
            float height = frames[0].rect.height / frames[0].pixelsPerUnit;
            float scale = height > 0.01f ? 3.4f / height : 0.72f;
            car.transform.localScale = new Vector3(scale, scale, 1f);
            var ink = car.AddComponent<SpriteRenderer>();
            ink.sortingOrder = 12;
            car.AddComponent<SpriteFrameAnimator>();
            var drive = car.AddComponent<GallinaConductora>();
            drive.Left = 74f;
            drive.Right = 106f;
            drive.Speed = 2.4f;
        }

        void Roca(Loom loom, System.Random rng, float x, float baseY, float w, float h, float z)
        {
            FanRock(loom, rng, x + 0.05f, baseY - 0.04f, w * 1.04f, h, z + 0.02f, RockDark);
            FanRock(loom, rng, x, baseY, w, h, z, Rock);
        }

        void FanRock(Loom loom, System.Random rng, float x, float baseY, float w, float h, float z, Color body)
        {
            int sides = 6 + rng.Next(3);
            var hub = new Vector3(x, baseY + h * 0.46f, z);
            int hubI = loom.Add(hub, new Vector3(0f, 0.45f, -1f), Ink(Lighten(body, 0.1f), 0f));
            int first = -1;
            int prev = -1;
            for (int i = 0; i < sides; i++)
            {
                float ang = Mathf.PI * 2f * i / sides;
                float rise = (Mathf.Sin(ang) + 1f) * 0.5f;
                float px = x + Mathf.Cos(ang) * w * Range(rng, 0.82f, 1.12f);
                float py = baseY + rise * h * Range(rng, 0.88f, 1.08f);
                var col = Ink(Lighten(body, Mathf.Lerp(-0.1f, 0.14f, rise)), 0f);
                var normal = new Vector3(Mathf.Cos(ang) * 0.4f, Mathf.Lerp(-0.2f, 0.8f, rise), -1f);
                int vi = loom.Add(new Vector3(px, py, z), normal, col);
                if (i == 0) first = vi;
                else loom.Tri(hubI, prev, vi);
                prev = vi;
            }
            loom.Tri(hubI, prev, first);
        }

        void Fan(Loom loom, System.Random rng, Vector3 center, float rx, float ry, Color mid, Color rim)
        {
            int sides = 8;
            int hub = loom.Add(center, new Vector3(0f, 0.3f, -1f), Ink(mid, 0.04f));
            int first = -1;
            int prev = -1;
            for (int i = 0; i < sides; i++)
            {
                float ang = Mathf.PI * 2f * i / sides + Range(rng, -0.08f, 0.08f);
                float j = Range(rng, 0.85f, 1.12f);
                var p = center + new Vector3(Mathf.Cos(ang) * rx * j, Mathf.Sin(ang) * ry * j, 0f);
                var normal = new Vector3(Mathf.Cos(ang) * 0.5f, Mathf.Sin(ang) * 0.5f, -1f);
                int vi = loom.Add(p, normal, Ink(rim, 0.7f));
                if (i == 0) first = vi;
                else loom.Tri(hub, prev, vi);
                prev = vi;
            }
            loom.Tri(hub, prev, first);
        }

        void Petal(Loom loom, Vector3 root, Vector3 tip, float flare, Color color)
        {
            var mid = (root + tip) * 0.5f;
            var side = new Vector3(-(tip.y - root.y), tip.x - root.x, 0f);
            if (side.sqrMagnitude < 0.0001f) side = Vector3.right;
            side = side.normalized * flare;
            int b = loom.Add(root, new Vector3(0f, 0.2f, -1f), Ink(color, 0.1f));
            int l = loom.Add(mid + side, new Vector3(-0.4f, 0.4f, -1f), Ink(color, 0.65f));
            int r = loom.Add(mid - side, new Vector3(0.4f, 0.4f, -1f), Ink(color, 0.65f));
            int t = loom.Add(tip, new Vector3(0f, 0.6f, -1f), Ink(Lighten(color, 0.12f), 1f));
            loom.Tri(b, l, t);
            loom.Tri(b, t, r);
        }

        void Blade(Loom loom, float x, float y, float z, float height, float lean, float width, Color root, Color tip)
        {
            const int segs = 3;
            float strength = Mathf.Clamp(height / 1.15f, 0.18f, 1f);
            int prevL = 0;
            int prevR = 0;
            for (int s = 0; s <= segs; s++)
            {
                float t = s / (float)segs;
                float yy = y + height * t;
                float xx = x + lean * t * t;
                float w = Mathf.Lerp(width, width * 0.15f, t);
                var col = Color.Lerp(root, tip, t);
                col.a = t * t * strength;
                var leftN = new Vector3(-0.55f, 0.2f, -1f);
                var rightN = new Vector3(0.55f, 0.2f, -1f);
                int l = loom.Add(new Vector3(xx - w, yy, z), leftN, col);
                int r = loom.Add(new Vector3(xx + w, yy, z), rightN, col);
                if (s > 0) loom.Quad(prevL, prevR, r, l);
                prevL = l;
                prevR = r;
            }
        }

        static void Scatter(System.Random rng, int count, float minX, float maxX, Action<float> place)
        {
            if (count <= 0) return;
            float span = (maxX - minX) / count;
            for (int i = 0; i < count; i++)
            {
                float x = minX + span * (i + 0.18f + (float)rng.NextDouble() * 0.64f);
                place(x);
            }
        }

        static float Range(System.Random rng, float a, float b) => a + (float)rng.NextDouble() * (b - a);

        static Color Tint(System.Random rng, Color color, float amount)
        {
            float k = 1f + Range(rng, -amount, amount);
            return new Color(
                Mathf.Clamp01(color.r * k),
                Mathf.Clamp01(color.g * k),
                Mathf.Clamp01(color.b * k),
                color.a);
        }

        static Color Lighten(Color c, float amount)
        {
            return new Color(
                Mathf.Clamp01(c.r + amount),
                Mathf.Clamp01(c.g + amount),
                Mathf.Clamp01(c.b + amount),
                c.a);
        }

        static Color Ink(Color rgb, float wind)
        {
            rgb.a = wind;
            return rgb;
        }

        sealed class Loom
        {
            readonly List<Vector3> verts = new List<Vector3>();
            readonly List<Vector3> normals = new List<Vector3>();
            readonly List<Color> colors = new List<Color>();
            readonly List<int> tris = new List<int>();

            public bool Empty => verts.Count == 0;

            public int Add(Vector3 p, Vector3 normal, Color color)
            {
                verts.Add(p);
                normals.Add(normal.sqrMagnitude > 0.0001f ? normal.normalized : Vector3.back);
                colors.Add(color);
                return verts.Count - 1;
            }

            public void Tri(int a, int b, int c)
            {
                tris.Add(a);
                tris.Add(b);
                tris.Add(c);
            }

            public void Quad(int a, int b, int c, int d)
            {
                Tri(a, b, c);
                Tri(a, c, d);
            }

            public Mesh Bake(string meshName)
            {
                var mesh = new Mesh { name = meshName };
                if (verts.Count > 65000) mesh.indexFormat = IndexFormat.UInt32;
                mesh.SetVertices(verts);
                mesh.SetNormals(normals);
                mesh.SetColors(colors);
                mesh.SetTriangles(tris, 0);
                mesh.RecalculateBounds();
                var bounds = mesh.bounds;
                bounds.Expand(0.65f);
                mesh.bounds = bounds;
                return mesh;
            }
        }
    }
}
