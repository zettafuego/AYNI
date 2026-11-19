using System.Collections.Generic;
using AYNI.Narrative;
using UnityEngine;

namespace AYNI.Battle
{
    /// <summary>
    /// Juan de Oso, the Yacumama, skulls in the mora, and the one who carries souls.
    /// They are drawings. They do not fight.
    /// </summary>
    public class FigurasDelCamino : MonoBehaviour
    {
        static readonly Vector2[] ZarzaAt =
        {
            new Vector2(-58f, 38f),
            new Vector2(55f, 44f),
            new Vector2(-60f, 70f),
            new Vector2(58f, 84f),
            new Vector2(8f, 34f),
            new Vector2(-22f, 92f),
        };

        static FigurasDelCamino current;

        LagunaBattleManager battle;
        Transform juan;
        Transform serpent;
        Transform demon;
        Transform town;
        Transform grove;
        readonly List<Transform> brambles = new List<Transform>();
        readonly List<Transform> segments = new List<Transform>();
        readonly List<Transform> souls = new List<Transform>();
        bool toldJuan;
        bool toldYacu;
        bool toldZarza;
        bool toldDemon;
        bool booted;

        public static void Ensure(LagunaBattleManager host)
        {
            if (host == null) return;
            var found = host.GetComponent<FigurasDelCamino>();
            if (found == null) found = host.gameObject.AddComponent<FigurasDelCamino>();
            found.battle = host;
            found.Boot();
        }

        public static void DressRoad(LagunaBattleManager host)
        {
            Ensure(host);
            if (current == null || current.juan != null) return;
            current.juan = current.Figure("JuanDeOso", Person(), new Vector3(70f, host.FloorY, 0f), 12, 1.15f);
            current.juan.SetParent(current.transform, true);
        }

        public static void DressGrove(Transform grove)
        {
            if (current == null || grove == null) return;
            current.grove = grove;
            current.ClearGrove();
            current.BuildSerpent(grove);
            current.BuildBrambles(grove);
        }

        public static void DressTown(Transform district)
        {
            if (current == null) return;
            current.town = district;
        }

        public static void TouchGrove(Vector3 local)
        {
            if (current == null) return;
            current.GroveLine(local);
        }

        void Awake()
        {
            current = this;
        }

        void OnDestroy()
        {
            if (current == this) current = null;
        }

        void Boot()
        {
            if (booted) return;
            booted = true;
            demon = Figure("ElQueLlevaAlmas", Demon(), new Vector3(0f, -40f, 0f), 12, 1.1f);
            for (int i = 0; i < 3; i++)
            {
                var soul = Figure("Alma" + i, Soul(), Vector3.zero, 13, 0.45f);
                soul.SetParent(demon, false);
                souls.Add(soul);
            }

            demon.SetParent(transform, true);
            demon.gameObject.SetActive(false);
        }

        void Update()
        {
            if (battle == null) return;
            IdleJuan();
            SwaySerpent();
            if (BosqueCapitulo.IsOpen)
            {
                for (int i = 0; i < brambles.Count; i++)
                    Face(brambles[i]);
            }

            PlaceDemon();
            NearJuan();
        }

        void IdleJuan()
        {
            if (juan == null) return;
            float breath = 1f + Mathf.Sin(Time.time * 1.5f) * 0.02f;
            juan.localScale = new Vector3(1.15f * breath, 1.15f * (2f - breath), 1f);
        }

        void NearJuan()
        {
            if (toldJuan || juan == null || !battle.IsPlaying) return;
            var player = battle.Player;
            if (player == null) return;
            if (Mathf.Abs(player.PositionX - juan.position.x) > 4.5f) return;
            toldJuan = true;
            battle.ShowRoadLine("JUAN DE OSO", LeyDelMonte.JuanLine, "Juan de Oso");
        }

        void SwaySerpent()
        {
            if (serpent == null) return;
            for (int i = 0; i < segments.Count; i++)
            {
                var part = segments[i];
                if (part == null) continue;
                float along = segments.Count <= 1 ? 0f : i / (segments.Count - 1f);
                float x = Mathf.Lerp(-8f, 8f, along);
                float y = Mathf.Sin(Time.time * 1.25f + i * 0.65f) * 0.16f;
                float z = Mathf.Sin(Time.time * 0.8f + i * 0.9f) * 0.28f;
                part.localPosition = new Vector3(x, y, z);
                Face(part);
            }
        }

        void GroveLine(Vector3 local)
        {
            if (battle == null) return;
            if (!toldYacu && local.z > 14f && local.z < 28f)
            {
                toldYacu = true;
                battle.ShowRoadLine("LA YACUMAMA", LeyDelMonte.YacumamaLine, "El río");
            }

            if (toldZarza) return;
            for (int i = 0; i < ZarzaAt.Length; i++)
            {
                float dx = local.x - ZarzaAt[i].x;
                float dz = local.z - ZarzaAt[i].y;
                if (dx * dx + dz * dz > 18f) continue;
                toldZarza = true;
                battle.ShowRoadLine("ZARZAS DE MORA", LeyDelMonte.ZarzaLine, LeyDelMonte.Voice);
                return;
            }
        }

        void PlaceDemon()
        {
            if (demon == null) return;
            bool live = battle.IsPlaying || CajamarcaCalles.IsOpen || BosqueCapitulo.IsOpen || CajamarcaRealCapitulo.IsOpen;
            bool night = live && Jornada.Shade > 0.55f;
            if (demon.gameObject.activeSelf != night) demon.gameObject.SetActive(night);
            if (!night) return;

            if (BosqueCapitulo.IsOpen && grove != null)
            {
                demon.position = grove.TransformPoint(new Vector3(6f, 0f, 48f));
                demon.localScale = Vector3.one * 1.7f;
            }
            else if (CajamarcaCalles.IsOpen && town != null)
            {
                demon.position = town.TransformPoint(new Vector3(10f, 0f, 4f));
                demon.localScale = Vector3.one * 1.5f;
            }
            else
            {
                var player = battle.Player;
                float x = player != null ? player.PositionX + 6.5f : 8f;
                x = Mathf.Clamp(x, battle.ArenaMinX + 4f, battle.ArenaMaxX - 1.5f);
                demon.position = new Vector3(x, battle.FloorY, 0f);
                demon.localScale = Vector3.one * 1.15f;
            }

            Face(demon);
            for (int i = 0; i < souls.Count; i++)
            {
                if (souls[i] == null) continue;
                float y = 1.15f + Mathf.Sin(Time.time * 2.2f + i * 1.3f) * 0.16f;
                float x = -0.22f + i * 0.22f;
                souls[i].localPosition = new Vector3(x, y, -0.05f);
            }

            if (!toldDemon)
            {
                toldDemon = true;
                battle.ShowRoadLine("EL QUE LLEVA ALMAS", LeyDelMonte.DemonioLine, "La noche");
            }
        }

        void ClearGrove()
        {
            if (serpent != null) Destroy(serpent.gameObject);
            serpent = null;
            segments.Clear();
            for (int i = 0; i < brambles.Count; i++)
                if (brambles[i] != null) Destroy(brambles[i].gameObject);
            brambles.Clear();
            toldYacu = false;
            toldZarza = false;
        }

        void BuildSerpent(Transform parent)
        {
            var root = new GameObject("Yacumama");
            root.transform.SetParent(parent, false);
            root.transform.localPosition = new Vector3(0f, 0.72f, 20.8f);
            serpent = root.transform;
            var body = Oval(new Color(0.07f, 0.38f, 0.24f, 1f), new Color(0.78f, 0.66f, 0.22f, 1f));
            var head = Head();
            for (int i = 0; i < 9; i++)
            {
                bool last = i == 8;
                var part = Figure(last ? "Cabeza" : "Cuerpo" + i, last ? head : body, Vector3.zero, 16, last ? 1.35f : 1f);
                part.SetParent(serpent, false);
                segments.Add(part);
            }
        }

        void BuildBrambles(Transform parent)
        {
            var art = Bramble();
            for (int i = 0; i < ZarzaAt.Length; i++)
            {
                var spot = new Vector3(ZarzaAt[i].x, 0.15f, ZarzaAt[i].y);
                var bush = Figure("Zarza" + i, art, parent.TransformPoint(spot), 12, 2.1f);
                bush.SetParent(parent, true);
                brambles.Add(bush);
                Face(bush);
            }
        }

        static void Face(Transform body)
        {
            var cam = Camera.main;
            if (body == null || cam == null) return;
            bool side = cam.orthographic && !BosqueCapitulo.IsOpen && !CajamarcaCalles.IsOpen && !CajamarcaRealCapitulo.IsOpen;
            if (side)
            {
                body.rotation = Quaternion.identity;
                return;
            }

            Vector3 flat = cam.transform.position - body.position;
            flat.y = 0f;
            if (flat.sqrMagnitude < 0.0001f) return;
            body.rotation = Quaternion.LookRotation(flat.normalized, Vector3.up);
        }

        Transform Figure(string name, Sprite sprite, Vector3 world, int order, float scale)
        {
            var go = new GameObject(name);
            var ink = go.AddComponent<SpriteRenderer>();
            ink.sprite = sprite;
            ink.sortingOrder = order;
            go.transform.position = world;
            go.transform.localScale = Vector3.one * scale;
            return go.transform;
        }

        static Sprite Person()
        {
            const int w = 72;
            const int h = 128;
            var tex = Blank(w, h);
            var fur = new Color(0.46f, 0.28f, 0.14f, 1f);
            var dark = new Color(0.26f, 0.14f, 0.08f, 1f);
            var snout = new Color(0.70f, 0.50f, 0.32f, 1f);
            var cloth = new Color(0.62f, 0.16f, 0.14f, 1f);
            var band = new Color(0.86f, 0.68f, 0.22f, 1f);
            Fill(tex, 28, 0, 34, 28, dark);
            Fill(tex, 40, 0, 46, 28, dark);
            Fill(tex, 18, 26, 54, 78, fur);
            Fill(tex, 20, 64, 52, 72, cloth);
            Fill(tex, 22, 70, 50, 76, band);
            Fill(tex, 16, 78, 56, 112, fur);
            Dot(tex, dark, 22, 104, 8);
            Dot(tex, dark, 50, 104, 8);
            Fill(tex, 28, 86, 44, 100, snout);
            Dot(tex, new Color(0.08f, 0.06f, 0.05f, 1f), 30, 98, 2);
            Dot(tex, new Color(0.08f, 0.06f, 0.05f, 1f), 42, 98, 2);
            Dot(tex, new Color(0.12f, 0.07f, 0.06f, 1f), 36, 90, 2);
            return Bake(tex, new Vector2(0.5f, 0f), 64f);
        }

        static Sprite Demon()
        {
            const int w = 56;
            const int h = 140;
            var tex = Blank(w, h);
            var cloak = new Color(0.06f, 0.05f, 0.08f, 1f);
            var horn = new Color(0.62f, 0.58f, 0.50f, 1f);
            var bone = new Color(0.78f, 0.76f, 0.68f, 1f);
            Fill(tex, 16, 0, 40, 96, cloak);
            Fill(tex, 8, 70, 18, 84, bone);
            Fill(tex, 38, 70, 48, 84, bone);
            Fill(tex, 18, 96, 38, 118, cloak);
            Fill(tex, 10, 112, 18, 132, horn);
            Fill(tex, 38, 112, 46, 132, horn);
            Dot(tex, new Color(0.85f, 0.18f, 0.12f, 1f), 24, 108, 2);
            Dot(tex, new Color(0.85f, 0.18f, 0.12f, 1f), 34, 108, 2);
            return Bake(tex, new Vector2(0.5f, 0f), 64f);
        }

        static Sprite Head()
        {
            var tex = Blank(56, 40);
            var hide = new Color(0.05f, 0.32f, 0.20f, 1f);
            var belly = new Color(0.84f, 0.72f, 0.28f, 1f);
            var eye = new Color(0.93f, 0.82f, 0.28f, 1f);
            Dot(tex, hide, 26, 18, 16);
            Fill(tex, 34, 14, 52, 24, belly);
            Dot(tex, eye, 18, 24, 3);
            Dot(tex, new Color(0.08f, 0.06f, 0.04f, 1f), 18, 24, 1);
            return Bake(tex, new Vector2(0.35f, 0.45f), 32f);
        }

        static Sprite Oval(Color hide, Color belly)
        {
            var tex = Blank(48, 28);
            float cx = 23f;
            float cy = 13f;
            for (int y = 0; y < tex.height; y++)
            for (int x = 0; x < tex.width; x++)
            {
                float dx = (x - cx) / 20f;
                float dy = (y - cy) / 11f;
                if (dx * dx + dy * dy > 1f) continue;
                tex.SetPixel(x, y, y < 12 ? belly : hide);
            }

            return Bake(tex, new Vector2(0.5f, 0.5f), 32f);
        }

        static Sprite Soul()
        {
            var tex = Blank(16, 16);
            Dot(tex, new Color(0.98f, 0.90f, 0.45f, 0.95f), 8, 8, 6);
            Dot(tex, new Color(1f, 0.98f, 0.85f, 1f), 8, 8, 3);
            return Bake(tex, new Vector2(0.5f, 0.5f), 16f);
        }

        static Sprite Bramble()
        {
            const int s = 96;
            var tex = Blank(s, s);
            var cane = new Color(0.10f, 0.16f, 0.07f, 1f);
            var leaf = new Color(0.16f, 0.28f, 0.10f, 1f);
            var mora = new Color(0.40f, 0.08f, 0.32f, 1f);
            for (int i = 0; i < 14; i++)
            {
                int x0 = 12 + (i * 17) % 70;
                int y0 = 10 + (i * 13) % 48;
                int x1 = x0 + 18 - (i % 5) * 4;
                int y1 = y0 + 16 + (i % 3) * 6;
                Line(tex, x0, y0, x1, y1, i % 2 == 0 ? cane : leaf);
            }

            Dot(tex, mora, 30, 34, 4);
            Dot(tex, mora, 62, 28, 4);
            Dot(tex, mora, 48, 48, 3);
            Dot(tex, mora, 24, 52, 3);
            Skull(tex, 48, 40);
            return Bake(tex, new Vector2(0.5f, 0.08f), 48f);
        }

        static void Skull(Texture2D tex, int cx, int cy)
        {
            var bone = new Color(0.93f, 0.91f, 0.84f, 1f);
            var hole = new Color(0.05f, 0.04f, 0.04f, 1f);
            Dot(tex, bone, cx, cy + 6, 10);
            Fill(tex, cx - 8, cy - 8, cx + 8, cy + 2, bone);
            Dot(tex, hole, cx - 4, cy + 6, 2);
            Dot(tex, hole, cx + 4, cy + 6, 2);
            Fill(tex, cx - 1, cy + 1, cx + 1, cy + 4, hole);
            for (int x = cx - 6; x <= cx + 6; x += 3)
                Fill(tex, x, cy - 6, x + 1, cy - 3, hole);
        }

        static void Line(Texture2D tex, int x0, int y0, int x1, int y1, Color color)
        {
            int steps = Mathf.Max(Mathf.Abs(x1 - x0), Mathf.Abs(y1 - y0));
            if (steps < 1) steps = 1;
            for (int i = 0; i <= steps; i++)
            {
                float t = i / (float)steps;
                int x = Mathf.RoundToInt(Mathf.Lerp(x0, x1, t));
                int y = Mathf.RoundToInt(Mathf.Lerp(y0, y1, t));
                Dot(tex, color, x, y, 2);
            }
        }

        static Texture2D Blank(int w, int h)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.hideFlags = HideFlags.HideAndDontSave;
            var clear = new Color(0f, 0f, 0f, 0f);
            var pixels = new Color[w * h];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = clear;
            tex.SetPixels(pixels);
            return tex;
        }

        static Sprite Bake(Texture2D tex, Vector2 pivot, float ppu)
        {
            tex.Apply();
            tex.filterMode = FilterMode.Point;
            var sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), pivot, ppu);
            sprite.hideFlags = HideFlags.HideAndDontSave;
            return sprite;
        }

        static void Fill(Texture2D tex, int x0, int y0, int x1, int y1, Color color)
        {
            for (int y = y0; y <= y1; y++)
            for (int x = x0; x <= x1; x++)
            {
                if (x < 0 || y < 0 || x >= tex.width || y >= tex.height) continue;
                tex.SetPixel(x, y, color);
            }
        }

        static void Dot(Texture2D tex, Color color, int cx, int cy, int r)
        {
            for (int y = cy - r; y <= cy + r; y++)
            for (int x = cx - r; x <= cx + r; x++)
            {
                if (x < 0 || y < 0 || x >= tex.width || y >= tex.height) continue;
                if ((x - cx) * (x - cx) + (y - cy) * (y - cy) <= r * r)
                    tex.SetPixel(x, y, color);
            }
        }
    }
}
