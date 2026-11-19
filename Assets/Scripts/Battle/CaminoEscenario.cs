using AYNI.Narrative;
using AYNI.UI;
using UnityEngine;
using UnityEngine.UI;

namespace AYNI.Battle
{
    /// <summary>
    /// After the lagoon rival falls, the same arena keeps going:
    /// the path, the mine mouth with its bell, the village, the dark thing, then the hat.
    /// </summary>
    public class CaminoEscenario : MonoBehaviour
    {
        public const float RoadEnd = 112f;
        public const float LagoonEdge = 28f;

        LagunaBattleManager battle;
        bool opened;
        bool finished;
        bool toldCave;
        bool toldPass;
        bool toldShadow;
        bool toldMeet;
        float meetTimer = -1f;
        float spark;
        SombraDelCerro sombra;
        Transform campesino;

        public static CaminoEscenario Ensure(LagunaBattleManager host)
        {
            var found = host.GetComponent<CaminoEscenario>();
            if (found == null) found = host.gameObject.AddComponent<CaminoEscenario>();
            found.battle = host;
            return found;
        }

        public bool HoldTheRoad()
        {
            if (finished || opened) return false;
            Open();
            return true;
        }

        void Open()
        {
            opened = true;
            battle.ArenaMaxX = BatanFiambre.GateX;
            MonteDressing.Ensure(battle.transform).ExtendRoad(battle.FloorY, LagoonEdge, RoadEnd + 16f);
            BatanFiambre.Place(battle);
            FigurasDelCamino.DressRoad(battle);
            DropRing();
            var cuy = FindFirstObjectByType<CuyNarrador>();
            var hero = FindFirstObjectByType<VizcachaFighter>();
            if (cuy != null && hero != null && cuy.gameObject.activeSelf)
            {
                cuy.WalkWith();
                cuy.transform.position = new Vector3(hero.PositionX - 1.7f, battle.FloorY, 0f);
            }
            battle.ShowRoadLine("EL SENDERO", LeyDelMonte.AfterSender, LeyDelMonte.Voice);
            PlaceDevil();
        }

        void Update()
        {
            if (!opened || finished || battle == null || !battle.IsPlaying || battle.IsGameOver) return;

            var player = FindFirstObjectByType<VizcachaFighter>();
            if (player == null || !player.IsAlive) return;

            KeepCuyNear(player.PositionX);

            if (!toldCave && player.PositionX > 68f)
            {
                toldCave = true;
                battle.ShowRoadLine("LA BOCA", LeyDelMonte.AfterCave, "La mina");
            }

            if (!toldPass && player.PositionX > 72f)
            {
                toldPass = true;
                PassCard();
            }

            if (!toldShadow && player.PositionX > 92f)
            {
                toldShadow = true;
                SpawnSombra();
                battle.ShowRoadLine("LA SOMBRA", LeyDelMonte.AfterShadow, LeyDelMonte.Voice);
            }

            if (campesino != null)
            {
                spark += Time.deltaTime;
                if (spark > 0.45f)
                {
                    spark = 0f;
                    LeafEffect.Spawn(campesino.position + Vector3.up * 1.15f, true);
                }

                if (!toldMeet && Mathf.Abs(player.PositionX - campesino.position.x) < 4.2f)
                {
                    toldMeet = true;
                    battle.ShowRoadLine("EL DEL SOMBRERO", LeyDelMonte.AfterHat, LeyDelMonte.Voice);
                    meetTimer = 3.8f;
                }
            }

            if (meetTimer < 0f) return;
            meetTimer -= Time.deltaTime;
            if (meetTimer <= 0f) Finish();
        }

        public void OnSombraDown()
        {
            if (finished || battle == null || battle.IsGameOver) return;
            sombra = null;
            battle.PresentFoe("El camino", 0f);
            battle.ShowRoadLine("EL PUEBLO", LeyDelMonte.AfterShadowDown, LeyDelMonte.Voice);
            SpawnCampesino();
        }

        void Finish()
        {
            if (finished) return;
            finished = true;
            meetTimer = -1f;
            battle.EndBattle(true);
        }

        void SpawnSombra()
        {
            if (sombra != null) return;
            var go = new GameObject("Sombra");
            sombra = go.AddComponent<SombraDelCerro>();
            sombra.Init(battle, this, battle.FloorY, 100f);
            battle.PresentFoe("La sombra", 100f);
        }

        void SpawnCampesino()
        {
            if (campesino != null) return;
            var go = new GameObject("DelSombrero");
            var ink = go.AddComponent<SpriteRenderer>();
            ink.sprite = Person();
            ink.sortingOrder = 11;
            go.transform.localScale = new Vector3(1.15f, 1.15f, 1f);
            go.transform.position = new Vector3(106f, battle.FloorY, 0f);
            campesino = go.transform;
        }

        void KeepCuyNear(float playerX)
        {
            var cuy = FindFirstObjectByType<CuyNarrador>();
            if (cuy == null || !cuy.gameObject.activeInHierarchy || cuy.Held) return;
            var pos = cuy.transform.position;
            float goal = playerX - 1.7f;
            float gap = Mathf.Abs(pos.x - goal);
            float speed = gap > 2.5f ? 12f : 4.2f;
            float next = Mathf.MoveTowards(pos.x, goal, speed * Time.deltaTime);
            bool moving = Mathf.Abs(next - pos.x) > 0.01f;
            cuy.transform.position = new Vector3(next, battle.FloorY, 0f);
            var anim = cuy.GetComponent<SpriteFrameAnimator>();
            if (anim != null && !anim.IsPlayingAction)
                anim.PlayLocomotion(moving, false);
            cuy.GetComponent<PasoSonido>()?.Mark(moving);
        }

        void PlaceDevil()
        {
            var foe = FindFirstObjectByType<ToroEnemyAI>();
            if (foe != null && foe.IsDevil)
                foe.gameObject.SetActive(false);

            var go = new GameObject("Diablito");
            var ink = go.AddComponent<SpriteRenderer>();
            ink.sortingOrder = 11;
            var anim = go.AddComponent<SpriteFrameAnimator>();
            BattleSprites.WearDevil(anim, true);
            go.transform.localScale = new Vector3(CastScale.Diablo, CastScale.Diablo, 1f);
            var shade = go.AddComponent<SombraEnElSuelo>();
            shade.Arm(battle.FloorY);
            var guide = go.AddComponent<DiabloDelCamino>();
            guide.Init(battle, battle.FloorY);
            GoldInTheMouth();
        }

        void GoldInTheMouth()
        {
            var sprite = Nugget();
            float[] heights = { 0.55f, 0.95f, 1.35f, 0.7f, 1.15f };
            for (int i = 0; i < heights.Length; i++)
            {
                var go = new GameObject("Oro");
                var ink = go.AddComponent<SpriteRenderer>();
                ink.sprite = sprite;
                ink.color = i % 2 == 0
                    ? new Color(1f, 0.82f, 0.22f, 1f)
                    : new Color(0.95f, 0.62f, 0.12f, 1f);
                ink.sortingOrder = 6;
                float side = i % 2 == 0 ? -0.18f : 0.22f;
                go.transform.position = new Vector3(DiabloDelCamino.CaveX + side, battle.FloorY + heights[i], 0f);
                go.transform.localScale = Vector3.one * (0.34f + (i % 3) * 0.08f);
            }
        }

        static Sprite Nugget()
        {
            const int s = 24;
            var tex = new Texture2D(s, s, TextureFormat.RGBA32, false);
            tex.hideFlags = HideFlags.HideAndDontSave;
            float c = (s - 1) * 0.5f;
            for (int y = 0; y < s; y++)
            for (int x = 0; x < s; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), new Vector2(c, c)) / c;
                tex.SetPixel(x, y, d < 0.82f ? Color.white : new Color(0f, 0f, 0f, 0f));
            }

            tex.Apply();
            var sprite = Sprite.Create(tex, new Rect(0f, 0f, s, s), new Vector2(0.5f, 0.5f), s);
            sprite.hideFlags = HideFlags.HideAndDontSave;
            return sprite;
        }

        void DropRing()
        {
            var foe = FindFirstObjectByType<ToroEnemyAI>();
            float x = foe != null ? foe.PositionX + 0.8f : 20f;
            var go = new GameObject("Anillo");
            var ink = go.AddComponent<SpriteRenderer>();
            ink.sprite = Ring();
            ink.sortingOrder = 13;
            ink.color = new Color(0.93f, 0.72f, 0.18f, 1f);
            go.transform.position = new Vector3(x, battle.FloorY + 0.22f, 0f);
            go.transform.localScale = Vector3.one * 0.55f;
        }

        void PassCard()
        {
            var canvas = battle.gameOverScreen != null ? battle.gameOverScreen.transform.parent : null;
            if (canvas == null || canvas.Find("Paso") != null) return;
            var plate = Storybook.Plate(canvas, "Paso", Vector2.zero, Vector2.one, new Color(0.05f, 0.06f, 0.08f, 0.94f));
            plate.transform.SetAsLastSibling();
            var go = new GameObject("PasoText");
            go.transform.SetParent(plate.transform, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.12f, 0.28f);
            rect.anchorMax = new Vector2(0.88f, 0.72f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            var text = go.AddComponent<Text>();
            Storybook.DressText(text, LeyDelMonte.AfterVillage, Storybook.DisplayFont, 36, Storybook.Sticker, FontStyle.Bold);
            Destroy(plate.gameObject, 1.25f);
        }

        static Sprite Ring()
        {
            const int s = 48;
            var tex = new Texture2D(s, s, TextureFormat.RGBA32, false);
            var clear = new Color(0f, 0f, 0f, 0f);
            float c = (s - 1) * 0.5f;
            for (int y = 0; y < s; y++)
            for (int x = 0; x < s; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), new Vector2(c, c));
                tex.SetPixel(x, y, d < 22f && d > 13f ? Color.white : clear);
            }
            tex.Apply();
            tex.filterMode = FilterMode.Point;
            return Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), s);
        }

        static Sprite Person()
        {
            const int w = 72;
            const int h = 112;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            var clear = new Color(0f, 0f, 0f, 0f);
            var skin = new Color(0.72f, 0.48f, 0.32f, 1f);
            var hat = new Color(0.18f, 0.13f, 0.10f, 1f);
            var poncho = new Color(0.70f, 0.22f, 0.16f, 1f);
            var band = new Color(0.86f, 0.62f, 0.18f, 1f);
            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
                tex.SetPixel(x, y, clear);

            Fill(tex, 30, 8, 42, 28, new Color(0.16f, 0.12f, 0.10f, 1f));
            Fill(tex, 18, 28, 54, 74, poncho);
            Fill(tex, 22, 64, 50, 70, band);
            Fill(tex, 28, 72, 44, 92, skin);
            Fill(tex, 16, 90, 56, 98, hat);
            Fill(tex, 26, 98, 46, 108, hat);
            Dot(tex, new Color(0.1f, 0.08f, 0.07f, 1f), 33, 82, 2);
            Dot(tex, new Color(0.1f, 0.08f, 0.07f, 1f), 41, 82, 2);

            tex.Apply();
            tex.filterMode = FilterMode.Point;
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0f), 78f);
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

    /// <summary>
    /// After the bull falls, the little devil walks to the mine mouth and points.
    /// </summary>
    public class DiabloDelCamino : MonoBehaviour
    {
        public const float CaveX = 63f;

        LagunaBattleManager battle;
        SpriteFrameAnimator anim;
        PasoSonido pasos;
        float floor;
        float x = 40f;
        bool told;

        public void Init(LagunaBattleManager host, float floorY)
        {
            battle = host;
            floor = floorY;
            anim = GetComponent<SpriteFrameAnimator>();
            pasos = gameObject.AddComponent<PasoSonido>();
            pasos.Pitch = 1.35f;
            transform.position = new Vector3(x, floor, 0f);
            anim?.SetFacing(true);
        }

        void Update()
        {
            if (battle == null || !battle.IsPlaying || battle.IsGameOver) return;

            float goal = CaveX - 3.2f;
            bool moving = x < goal - 0.04f;
            if (moving)
                x = Mathf.MoveTowards(x, goal, 1.7f * Time.deltaTime);
            transform.position = new Vector3(x, floor, 0f);
            if (anim != null && !anim.IsPlayingAction)
                anim.PlayLocomotion(moving, false);
            pasos?.Mark(moving);

            if (told || battle.Player == null) return;
            if (Mathf.Abs(battle.Player.PositionX - x) > 6.5f) return;
            told = true;
            battle.ShowRoadLine("EL DIABLITO", LeyDelMonte.DiabloCueva, "El diablito");
        }
    }
}
