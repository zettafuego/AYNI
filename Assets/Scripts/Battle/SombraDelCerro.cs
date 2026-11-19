using UnityEngine;

namespace AYNI.Battle
{
    /// <summary>
    /// The dark thing on the road out of the lagoon. Its poses are drawn in code.
    /// It is not a filmed cycle, and it is not another animal from the cast.
    /// </summary>
    public class SombraDelCerro : MonoBehaviour
    {
        public const float MaxHealth = 70f;

        public float Health { get; private set; } = MaxHealth;
        public bool IsAlive => Health > 0f;
        public float PositionX => transform.position.x;

        LagunaBattleManager battle;
        CaminoEscenario camino;
        VizcachaFighter player;
        SpriteFrameAnimator animator;
        float floorY;
        float attackTimer;
        float bumpCooldown;
        bool facingLeft = true;

        public void Init(LagunaBattleManager battle, CaminoEscenario camino, float floor, float x)
        {
            this.battle = battle;
            this.camino = camino;
            floorY = floor;
            player = FindFirstObjectByType<VizcachaFighter>();

            var ink = gameObject.AddComponent<SpriteRenderer>();
            ink.sortingOrder = 12;
            animator = gameObject.AddComponent<SpriteFrameAnimator>();
            var idle = new[] { Pose(0), Pose(1) };
            animator.SetClips(idle, idle, new[] { Pose(2) }, new[] { Pose(2) }, new[] { Pose(3) }, new[] { Pose(4) });
            transform.localScale = new Vector3(1.55f, 1.55f, 1f);
            transform.position = new Vector3(x, floorY, 0f);
            animator.SetFacing(true);
        }

        void Update()
        {
            if (!IsAlive || battle == null || !battle.IsPlaying || battle.IsGameOver || battle.CommandsOpen) return;
            if (player == null || !player.IsAlive) return;

            if (bumpCooldown > 0f) bumpCooldown -= Time.deltaTime;

            float dist = PositionX - player.PositionX;
            bool moving = false;
            if (dist > 2.1f && dist < 18f)
            {
                transform.position += Vector3.left * 2.3f * Time.deltaTime;
                facingLeft = true;
                moving = true;
            }
            else if (dist < -1.4f && dist > -18f)
            {
                transform.position += Vector3.right * 2.3f * Time.deltaTime;
                facingLeft = false;
                moving = true;
            }

            var pos = transform.position;
            pos.x = Mathf.Clamp(pos.x, 86f, 110f);
            pos.y = floorY;
            transform.position = pos;

            animator?.SetFacing(facingLeft);
            if (animator != null && !animator.IsPlayingAction)
                animator.PlayLocomotion(moving, false);

            attackTimer += Time.deltaTime;
            if (attackTimer < 2.15f || Mathf.Abs(dist) > 3.1f) return;
            attackTimer = 0f;
            animator?.PlayAction();
            player.TakeDamage(8f);
        }

        public void TakeDamage(float amount)
        {
            if (!IsAlive) return;
            Health = Mathf.Max(0f, Health - amount);
            battle?.OnEnemyHealthChanged(Health / MaxHealth * 100f);
            if (Health <= 0f)
            {
                animator?.PlayDeath();
                camino?.OnSombraDown();
            }
            else
            {
                animator?.PlayHurt();
            }
        }

        public void OnBodyBump()
        {
            if (bumpCooldown > 0f || !IsAlive) return;
            bumpCooldown = 0.8f;
            TakeDamage(10f);
        }

        public bool CheckHealHit(Vector3 projPos)
        {
            var bounds = new Bounds(transform.position + Vector3.up * 0.45f, new Vector3(1.8f, 1.1f, 0.1f));
            return bounds.Contains(projPos);
        }

        static Sprite Pose(int pose)
        {
            const int w = 96;
            const int h = 72;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            var clear = new Color(0f, 0f, 0f, 0f);
            var shell = new Color(0.07f, 0.05f, 0.04f, 1f);
            var belly = new Color(0.16f, 0.11f, 0.08f, 1f);
            var horn = new Color(0.75f, 0.58f, 0.18f, 1f);
            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
                tex.SetPixel(x, y, clear);

            float bob = pose == 1 ? 3f : 0f;
            float cy = (pose == 4 ? 16f : 30f) + bob;
            float rx = pose == 4 ? 34f : 28f;
            float ry = pose == 4 ? 9f : pose == 2 ? 13f : 16f;
            float cx = pose == 2 ? 50f : 58f;
            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                if (Inside(x, y, cx, cy, rx, ry))
                    tex.SetPixel(x, y, y < cy ? belly : shell);
            }

            float headX = cx - rx + 6f;
            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                if (Inside(x, y, headX, cy + 2f, 11f, pose == 4 ? 7f : 11f))
                    tex.SetPixel(x, y, shell);
            }

            if (pose != 4)
            {
                Stamp(tex, horn, (int)headX - 10, (int)cy + 8, (int)headX - 2, (int)cy + 16);
                Stamp(tex, horn, (int)headX - 8, (int)cy + 4, (int)headX - 1, (int)cy + 10);
                int eye = pose == 3 ? 2 : 3;
                Dot(tex, new Color(0.9f, 0.82f, 0.45f, 1f), (int)headX - 2, (int)cy + 4, eye);
            }

            int ground = 3;
            Leg(tex, shell, (int)(cx - 8), (int)(cy - ry + 4), pose == 2 ? 28 : 34, ground);
            Leg(tex, shell, (int)(cx + 2), (int)(cy - ry + 4), pose == 2 ? 46 : 52, ground);
            Leg(tex, shell, (int)(cx + 14), (int)(cy - ry + 2), pose == 2 ? 62 : 70, ground);

            tex.Apply();
            tex.filterMode = FilterMode.Point;
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0f), 72f);
        }

        static bool Inside(int x, int y, float cx, float cy, float rx, float ry)
        {
            if (rx < 0.1f || ry < 0.1f) return false;
            float dx = (x - cx) / rx;
            float dy = (y - cy) / ry;
            return dx * dx + dy * dy <= 1f;
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

        static void Stamp(Texture2D tex, Color color, int x0, int y0, int x1, int y1)
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

        static void Leg(Texture2D tex, Color color, int x0, int y0, int x1, int y1)
        {
            Stamp(tex, color, x0, y0, x1, y1);
        }
    }
}
