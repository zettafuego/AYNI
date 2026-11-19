using UnityEngine;

namespace AYNI.Battle
{
    /// <summary>
    /// A short step while a character walks the mountain path.
    /// </summary>
    public class PasoSonido : MonoBehaviour
    {
        public float Pitch = 1.15f;

        AudioSource src;
        float wait;
        static AudioClip step;

        public static float For(CharacterSheet sheet)
        {
            if (sheet == BattleCast.Toro) return 0.64f;
            if (sheet == BattleCast.Cuy) return 1.5f;
            if (sheet == BattleCast.Brayan) return 1.34f;
            if (sheet == BattleCast.Atoq) return 1.02f;
            return 1.22f;
        }

        public void Mark(bool moving)
        {
            if (!moving)
            {
                wait = 0f;
                return;
            }

            if (wait > 0f) return;
            wait = Pitch < 0.8f ? 0.46f : 0.32f;
            Ensure();
            src.pitch = Pitch;
            src.PlayOneShot(Clip(), 0.42f);
        }

        void Update()
        {
            if (wait > 0f) wait -= Time.deltaTime;
        }

        void Ensure()
        {
            if (src != null) return;
            src = gameObject.AddComponent<AudioSource>();
            src.playOnAwake = false;
            src.loop = false;
            src.spatialBlend = 0f;
        }

        static AudioClip Clip()
        {
            if (step != null) return step;
            const int rate = 22050;
            const int n = 1600;
            var data = new float[n];
            var rng = new System.Random(23);
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)n;
                float env = Mathf.Exp(-t * 16f);
                float body = Mathf.Sin(t * 78f * Mathf.PI * 2f);
                float grit = (float)(rng.NextDouble() * 2.0 - 1.0);
                data[i] = (body * 0.62f + grit * 0.22f) * env;
            }

            step = AudioClip.Create("paso", n, 1, rate, false);
            step.SetData(data, 0);
            step.hideFlags = HideFlags.HideAndDontSave;
            return step;
        }
    }

    /// <summary>
    /// The current sprite, flattened on the path, under the feet.
    /// It stays on the ground when the body leaves it.
    /// </summary>
    public class SombraEnElSuelo : MonoBehaviour
    {
        SpriteRenderer body;
        SpriteRenderer shade;
        float floor;
        bool armed;

        public void Arm(float floorY)
        {
            floor = floorY;
            body = GetComponent<SpriteRenderer>();
            if (armed) return;
            armed = true;
            var go = new GameObject("Sombra");
            shade = go.AddComponent<SpriteRenderer>();
            shade.sortingOrder = body != null ? Mathf.Max(1, body.sortingOrder - 1) : 1;
            shade.enabled = false;
        }

        void LateUpdate()
        {
            if (shade == null || body == null) return;
            if (!isActiveAndEnabled || !body.enabled || body.sprite == null)
            {
                shade.enabled = false;
                return;
            }

            shade.enabled = true;
            shade.sprite = body.sprite;
            var scale = transform.lossyScale;
            float lift = Mathf.Max(0f, transform.position.y - floor);
            float falloff = Mathf.Clamp01(lift / 4.2f);
            float squash = Mathf.Lerp(0.26f, 0.1f, falloff);
            float lean = 0.42f + lift * 0.32f;
            float worldH = body.sprite.bounds.size.y * Mathf.Abs(scale.y) * squash;
            shade.color = new Color(0.02f, 0.04f, 0.03f, Mathf.Lerp(0.46f, 0.12f, falloff));
            shade.transform.position = new Vector3(transform.position.x + lean, floor + worldH * 0.5f, 0.05f);
            shade.transform.localScale = new Vector3(scale.x, Mathf.Abs(scale.y) * squash, 1f);
        }

        void OnDisable()
        {
            if (shade != null) shade.enabled = false;
        }

        void OnDestroy()
        {
            if (shade != null) Destroy(shade.gameObject);
        }
    }

    /// <summary>
    /// Gold that leaves the toro when his power drops.
    /// </summary>
    public class MonedaDelToro : MonoBehaviour
    {
        Vector3 vel;
        float floor;
        float life;
        bool landed;
        LagunaBattleManager battle;
        static Sprite coin;

        public static void Spill(Vector3 from, float floorY, float amount, LagunaBattleManager battle)
        {
            int n = Mathf.Clamp(Mathf.RoundToInt(amount / 6f), 1, 4);
            for (int i = 0; i < n; i++)
            {
                var go = new GameObject("Moneda");
                var piece = go.AddComponent<MonedaDelToro>();
                float dir = Random.value < 0.5f ? -1f : 1f;
                piece.vel = new Vector3(dir * Random.Range(2.4f, 5.6f), Random.Range(4.2f, 7.2f), 0f);
                piece.floor = floorY;
                piece.battle = battle;
                go.transform.position = from + Vector3.up * 0.85f + Vector3.right * Random.Range(-0.2f, 0.2f);
                var ink = go.AddComponent<SpriteRenderer>();
                ink.sprite = Coin();
                ink.sortingOrder = 15;
                ink.color = new Color(1f, 0.92f, 0.45f, 1f);
                go.transform.localScale = Vector3.one * Random.Range(0.42f, 0.58f);
            }
        }

        void Update()
        {
            life += Time.deltaTime;
            if (!landed)
            {
                vel.y -= 22f * Time.deltaTime;
                transform.position += vel * Time.deltaTime;
                transform.Rotate(0f, 0f, vel.x * -8f * Time.deltaTime);
                if (transform.position.y <= floor + 0.16f && vel.y <= 0f)
                {
                    var at = transform.position;
                    at.y = floor + 0.16f;
                    transform.position = at;
                    vel = Vector3.zero;
                    landed = true;
                }
            }
            else
            {
                var at = transform.position;
                at.y = floor + 0.16f + Mathf.Sin((Time.time + life) * 4f) * 0.04f;
                transform.position = at;
            }

            var hero = battle != null ? battle.Player : null;
            if (life > 0.4f && hero != null && hero.IsAlive &&
                Vector3.Distance(hero.transform.position, transform.position) < 1.15f)
            {
                battle.NoteMoneda();
                Destroy(gameObject);
                return;
            }

            if (life > 8f) Destroy(gameObject);
        }

        static Sprite Coin()
        {
            if (coin != null) return coin;
            const int s = 32;
            var tex = new Texture2D(s, s, TextureFormat.RGBA32, false);
            tex.hideFlags = HideFlags.HideAndDontSave;
            float c = (s - 1) * 0.5f;
            for (int y = 0; y < s; y++)
            for (int x = 0; x < s; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), new Vector2(c, c)) / c;
                Color px = Color.clear;
                if (d < 0.92f)
                {
                    px = d > 0.72f
                        ? new Color(0.55f, 0.36f, 0.08f, 1f)
                        : new Color(0.93f, 0.74f, 0.18f, 1f);
                    if (d < 0.28f) px = new Color(0.98f, 0.88f, 0.42f, 1f);
                }

                tex.SetPixel(x, y, px);
            }

            tex.Apply();
            coin = Sprite.Create(tex, new Rect(0f, 0f, s, s), new Vector2(0.5f, 0.5f), s);
            coin.hideFlags = HideFlags.HideAndDontSave;
            return coin;
        }
    }
}
