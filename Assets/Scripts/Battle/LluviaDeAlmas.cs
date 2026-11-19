using System;
using System.Collections.Generic;
using UnityEngine;

namespace AYNI.Battle
{
    /// <summary>
    /// Lluvia sobre el paseo. Cada trueno suelta las tres almas de color,
    /// que bajan con su ciclo de Caminar y se apagan al tocar el suelo.
    /// </summary>
    public class LluviaDeAlmas : MonoBehaviour
    {
        const int DropCount = 48;
        const int GhostCap = 3;

        Transform follow;
        Func<float, float, float> groundY;
        float scaleMul = 0.35f;
        float nextBolt;
        float flash;
        AudioSource voice;
        SpriteRenderer bolt;
        readonly List<Transform> drops = new List<Transform>(DropCount);
        readonly List<float> speeds = new List<float>(DropCount);
        int ghosts;

        static Sprite streak;
        static Sprite puff;
        static Sprite white;
        static AudioClip rainBed;
        static AudioClip thunder;
        static LluviaDeAlmas viva;

        public static void Pausa(bool pausa)
        {
            if (viva != null) viva.gameObject.SetActive(!pausa);
        }

        public static void Begin(Transform parent, Transform follow, Func<float, float, float> groundY, float scaleMul)
        {
            if (parent == null) return;
            var go = new GameObject("Lluvia");
            go.transform.SetParent(parent, false);
            var rain = go.AddComponent<LluviaDeAlmas>();
            viva = rain;
            rain.follow = follow;
            rain.groundY = groundY ?? ((x, z) => 0.2f);
            rain.scaleMul = Mathf.Max(0.05f, scaleMul);
            rain.nextBolt = Time.time + UnityEngine.Random.Range(3.2f, 5.4f);
            rain.Build();
        }

        void Build()
        {
            EnsureArt();
            for (int i = 0; i < DropCount; i++)
            {
                var go = new GameObject("Gota");
                go.transform.SetParent(transform, false);
                var ink = go.AddComponent<SpriteRenderer>();
                ink.sprite = streak;
                ink.color = new Color(0.76f, 0.86f, 0.95f, UnityEngine.Random.Range(0.28f, 0.62f));
                ink.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                ink.receiveShadows = false;
                ink.sortingOrder = 16;
                drops.Add(go.transform);
                speeds.Add(UnityEngine.Random.Range(16f, 28f));
                PlaceDrop(i, true);
            }

            for (int i = 0; i < 5; i++)
            {
                var go = new GameObject("Nube");
                go.transform.SetParent(transform, false);
                var ink = go.AddComponent<SpriteRenderer>();
                ink.sprite = puff;
                ink.color = new Color(0.22f, 0.24f, 0.30f, 0.38f);
                ink.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                ink.receiveShadows = false;
                ink.sortingOrder = 4;
                float span = UnityEngine.Random.Range(14f, 26f);
                go.transform.localScale = new Vector3(span, span * 0.42f, 1f);
                var c = Center();
                go.transform.localPosition = c + new Vector3(
                    UnityEngine.Random.Range(-18f, 18f),
                    12f + i * 1.4f,
                    UnityEngine.Random.Range(-18f, 18f));
            }

            voice = gameObject.AddComponent<AudioSource>();
            voice.playOnAwake = false;
            voice.loop = true;
            voice.spatialBlend = 0f;
            voice.volume = 0.16f;
            voice.clip = rainBed;
            voice.Play();

            var cam = Camera.main;
            if (cam == null) return;
            var plate = new GameObject("Relampago");
            plate.transform.SetParent(cam.transform, false);
            bolt = plate.AddComponent<SpriteRenderer>();
            bolt.sprite = white;
            bolt.color = new Color(0.92f, 0.95f, 1f, 0f);
            bolt.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            bolt.receiveShadows = false;
            bolt.sortingOrder = 4000;
        }

        void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f) return;

            Vector3 center = Center();
            for (int i = 0; i < drops.Count; i++)
            {
                var drop = drops[i];
                if (drop == null) continue;
                Vector3 p = drop.localPosition;
                p.y -= speeds[i] * dt;
                float floor = Floor(p.x, p.z);
                Vector3 flat = p - center;
                flat.y = 0f;
                if (p.y <= floor || flat.sqrMagnitude > 32f * 32f)
                    PlaceDrop(i, false);
                else
                    drop.localPosition = p;
            }

            if (Time.time >= nextBolt)
            {
                nextBolt = Time.time + UnityEngine.Random.Range(6.5f, 12f);
                Strike(center);
            }

            if (flash > 0f)
            {
                flash -= dt * 3.4f;
                if (bolt != null)
                {
                    float a = Mathf.Clamp01(flash);
                    a = a * a * 0.78f;
                    var c = bolt.color;
                    c.a = a;
                    bolt.color = c;
                }
            }
        }

        void LateUpdate()
        {
            var cam = Camera.main;
            if (cam == null) return;
            var face = Face(cam);
            for (int i = 0; i < drops.Count; i++)
            {
                if (drops[i] != null) drops[i].rotation = face;
            }

            for (int i = 0; i < transform.childCount; i++)
            {
                var child = transform.GetChild(i);
                if (child.name == "Nube") child.rotation = face;
            }

            if (bolt == null) return;
            float dist = cam.orthographic ? 1f : 1.4f;
            float height = cam.orthographic
                ? cam.orthographicSize * 2.3f
                : 2f * dist * Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad) * 1.2f;
            float width = height * Mathf.Max(0.5f, cam.aspect);
            bolt.transform.localRotation = Quaternion.identity;
            bolt.transform.localPosition = new Vector3(0f, 0f, cam.orthographic ? cam.nearClipPlane + 0.3f : dist);
            bolt.transform.localScale = new Vector3(width, height, 1f);
        }

        void Strike(Vector3 center)
        {
            flash = 1f;
            if (voice != null && thunder != null)
                voice.PlayOneShot(thunder, 0.9f);

            var colors = new[] { BattleCast.AlmaOro, BattleCast.AlmaRosa, BattleCast.AlmaAzul };
            for (int i = 0; i < colors.Length; i++)
            {
                if (ghosts >= GhostCap) break;
                float ang = (i / 3f) * Mathf.PI * 2f + UnityEngine.Random.Range(-0.4f, 0.4f);
                float radius = UnityEngine.Random.Range(16f, 26f);
                float x = center.x + Mathf.Cos(ang) * radius;
                float z = center.z + Mathf.Sin(ang) * radius;
                float floor = Floor(x, z);
                float y = floor + UnityEngine.Random.Range(14f, 24f) + i * 2.5f;
                SpawnGhost(colors[i], new Vector3(x, y, z), floor);
            }
        }

        void SpawnGhost(CharacterSheet sheet, Vector3 at, float floor)
        {
            if (sheet == null) return;
            var go = new GameObject("Trueno" + sheet.Name);
            go.transform.SetParent(transform, false);
            var ink = go.AddComponent<SpriteRenderer>();
            ink.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            ink.receiveShadows = false;
            ink.sortingOrder = 26;
            var anim = go.AddComponent<SpriteFrameAnimator>();
            BattleSprites.Apply(anim, sheet);
            float scale = BattleCast.BodyScale(sheet) * scaleMul;
            go.transform.localScale = new Vector3(Mathf.Abs(scale), Mathf.Abs(scale), 1f);
            go.transform.localPosition = at;
            anim.SetFacing(true);
            anim.PlayWalk(true);
            var fall = go.AddComponent<AlmaQueCae>();
            fall.Arm(this, floor, UnityEngine.Random.Range(9f, 13f));
            ghosts++;
        }

        public void NoteGhostGone()
        {
            ghosts = Mathf.Max(0, ghosts - 1);
        }

        void PlaceDrop(int i, bool first)
        {
            var center = Center();
            float x = center.x + UnityEngine.Random.Range(-20f, 20f);
            float z = center.z + UnityEngine.Random.Range(-20f, 20f);
            float floor = Floor(x, z);
            float y = floor + (first ? UnityEngine.Random.Range(0.5f, 20f) : UnityEngine.Random.Range(12f, 22f));
            var drop = drops[i];
            drop.localPosition = new Vector3(x, y, z);
            float h = UnityEngine.Random.Range(0.42f, 1.05f);
            drop.localScale = new Vector3(0.035f, h, 1f);
        }

        Vector3 Center()
        {
            if (follow != null) return follow.localPosition;
            return Vector3.zero;
        }

        float Floor(float x, float z)
        {
            try
            {
                return groundY(x, z);
            }
            catch (Exception)
            {
                return 0.2f;
            }
        }

        void OnDestroy()
        {
            if (viva == this) viva = null;
            if (bolt != null) Destroy(bolt.gameObject);
            if (voice != null) voice.Stop();
        }

        static void EnsureArt()
        {
            if (streak == null) streak = MakeStreak();
            if (puff == null) puff = MakePuff();
            if (white == null) white = MakeWhite();
            if (rainBed == null) rainBed = MakeRain();
            if (thunder == null) thunder = MakeThunder();
        }

        static Sprite MakeStreak()
        {
            const int w = 4;
            const int h = 32;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.hideFlags = HideFlags.HideAndDontSave;
            for (int y = 0; y < h; y++)
            {
                float v = 1f - Mathf.Abs(y / (h - 1f) * 2f - 1f);
                byte a = (byte)Mathf.Clamp(v * v * 255f, 0f, 255f);
                for (int x = 0; x < w; x++)
                    tex.SetPixel(x, y, new Color32(255, 255, 255, a));
            }

            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), h);
        }

        static Sprite MakePuff()
        {
            const int n = 32;
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
            tex.hideFlags = HideFlags.HideAndDontSave;
            var clear = new Color32(255, 255, 255, 0);
            for (int y = 0; y < n; y++)
            {
                for (int x = 0; x < n; x++)
                {
                    float dx = (x + 0.5f) / n * 2f - 1f;
                    float dy = (y + 0.5f) / n * 2f - 1f;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    float a = Mathf.Clamp01(1f - d);
                    a = a * a;
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
                }
            }

            tex.Apply();
            tex.filterMode = FilterMode.Bilinear;
            return Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), n);
        }

        static Sprite MakeWhite()
        {
            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            tex.hideFlags = HideFlags.HideAndDontSave;
            var px = new Color32(255, 255, 255, 255);
            tex.SetPixels32(new[] { px, px, px, px });
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, 2, 2), new Vector2(0.5f, 0.5f), 2f);
        }

        static AudioClip MakeRain()
        {
            const int rate = 22050;
            int n = rate / 2;
            var data = new float[n];
            var rng = new System.Random(19);
            float hold = 0f;
            for (int i = 0; i < n; i++)
            {
                float grit = (float)(rng.NextDouble() * 2.0 - 1.0);
                hold = hold * 0.86f + grit * 0.14f;
                data[i] = hold * 0.55f;
            }

            var clip = AudioClip.Create("lluvia", n, 1, rate, false);
            clip.SetData(data, 0);
            clip.hideFlags = HideFlags.HideAndDontSave;
            return clip;
        }

        static AudioClip MakeThunder()
        {
            const int rate = 22050;
            int n = rate;
            var data = new float[n];
            var rng = new System.Random(41);
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)rate;
                float crack = (float)(rng.NextDouble() * 2.0 - 1.0) * Mathf.Exp(-t * 16f);
                float low = Mathf.Sin(t * 42f * Mathf.PI * 2f) * Mathf.Exp(-t * 1.5f);
                float mid = Mathf.Sin(t * 26f * Mathf.PI * 2f) * Mathf.Exp(-t * 2.2f);
                data[i] = Mathf.Clamp(crack * 0.8f + low * 0.38f + mid * 0.24f, -1f, 1f);
            }

            var clip = AudioClip.Create("trueno", n, 1, rate, false);
            clip.SetData(data, 0);
            clip.hideFlags = HideFlags.HideAndDontSave;
            return clip;
        }

        public static Quaternion Face(Camera cam)
        {
            // En PLANTA la cámara mira abajo: el dibujo se acuesta con ella.
            if (cam.transform.forward.y < -0.82f)
                return cam.transform.rotation;
            Vector3 look = cam.transform.forward;
            look.y = 0f;
            if (look.sqrMagnitude < 0.001f) look = Vector3.forward;
            return Quaternion.LookRotation(look.normalized, Vector3.up);
        }
    }

    /// <summary>Un alma de color que baja con el trueno y se hunde al tocar el suelo.</summary>
    [DefaultExecutionOrder(200)]
    public class AlmaQueCae : MonoBehaviour
    {
        LluviaDeAlmas rain;
        SpriteFrameAnimator anim;
        float floor;
        float speed;
        float life;
        bool down;
        bool told;

        public void Arm(LluviaDeAlmas owner, float ground, float fallSpeed)
        {
            rain = owner;
            floor = ground;
            speed = fallSpeed;
            anim = GetComponent<SpriteFrameAnimator>();
        }

        void Update()
        {
            if (anim != null) anim.PlayWalk(true);
            var p = transform.localPosition;
            if (!down)
            {
                p.y -= speed * Time.deltaTime;
                p.x += Mathf.Sin(Time.time * 2.4f + floor) * 0.65f * Time.deltaTime;
                if (p.y <= floor)
                {
                    p.y = floor;
                    down = true;
                    life = 2.6f;
                }
            }
            else
            {
                life -= Time.deltaTime;
                if (life > 1.15f)
                    p.y = floor + Mathf.Sin(Time.time * 3.1f) * 0.18f;
                else
                    p.y -= 2.4f * Time.deltaTime;

                if (life <= 0f || p.y < floor - 3f)
                {
                    if (!told && rain != null)
                    {
                        told = true;
                        rain.NoteGhostGone();
                    }

                    Destroy(gameObject);
                    return;
                }
            }

            transform.localPosition = p;
        }

        void LateUpdate()
        {
            var cam = Camera.main;
            if (cam == null) return;
            transform.rotation = LluviaDeAlmas.Face(cam);
        }

        void OnDestroy()
        {
            if (!told && rain != null) rain.NoteGhostGone();
        }
    }
}
