using System;
using UnityEngine;

namespace AYNI.Battle
{
    /// <summary>
    /// Four lianas over the laguna. Angle 0 hangs straight down; positive angle swings toward +X.
    /// Sprite rotation Z uses the same sign, so the bob moves with the pendulum.
    /// </summary>
    public class JungleCanopy : MonoBehaviour
    {
        public static JungleCanopy Instance { get; private set; }

        public event Action FirstGrab;

        const float Gravity = 20f;
        const float Torque = 7f;
        const float MaxAngle = 1.15f;
        const float GrabRadius = 1.75f;
        const float PlayAnchor = 6f;
        const float PlayLength = 3.2f;

        Vine[] vines;
        Vine held;
        float hangDrop = 2f;
        bool grabbedOnce;
        bool waitRelease;
        VizcachaFighter rider;
        float minX = -28f;
        float maxX = 28f;

        class Vine
        {
            public Transform Rope;
            public float X;
            public float AnchorY;
            public float Length;
            public float Angle;
            public float AngleVel;
            public bool Grabbable;
        }

        public static JungleCanopy Ensure(float floorY, float arenaMinX, float arenaMaxX)
        {
            var existing = FindFirstObjectByType<JungleCanopy>();
            if (existing != null) return existing;

            var go = new GameObject("JungleCanopy");
            var canopy = go.AddComponent<JungleCanopy>();
            canopy.minX = arenaMinX;
            canopy.maxX = arenaMaxX;
            canopy.Build(floorY);
            return canopy;
        }

        void Awake()
        {
            Instance = this;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public bool Occupies(VizcachaFighter fighter) => rider == fighter && held != null;

        public bool TryGrab(VizcachaFighter fighter)
        {
            if (fighter == null || vines == null || rider != null) return false;

            hangDrop = HandHeight(fighter);
            Vector3 hands = fighter.transform.position + Vector3.up * hangDrop;

            Vine best = null;
            float bestDist = GrabRadius;
            foreach (var vine in vines)
            {
                if (!vine.Grabbable) continue;
                float dist = Vector2.Distance(hands, Bob(vine));
                if (dist < bestDist)
                {
                    bestDist = dist;
                    best = vine;
                }
            }

            if (best == null) return false;

            float dx = hands.x - best.X;
            best.Angle = Mathf.Clamp(Mathf.Asin(Mathf.Clamp(dx / best.Length, -0.99f, 0.99f)), -MaxAngle, MaxAngle);
            best.AngleVel = 0f;
            ApplyPose(best);

            held = best;
            rider = fighter;
            waitRelease = true;
            Place(fighter);

            if (!grabbedOnce)
            {
                grabbedOnce = true;
                FirstGrab?.Invoke();
            }

            return true;
        }

        public void Drive(VizcachaFighter fighter, bool steer = true)
        {
            if (held == null || fighter == null) return;

            float h = steer ? Input.GetAxisRaw("Horizontal") : 0f;
            if (Mathf.Abs(h) > 0.01f)
                fighter.Face(h > 0f);

            float dt = Time.deltaTime;
            float length = held.Length;
            held.AngleVel += ((-Gravity / length) * Mathf.Sin(held.Angle) + h * Torque) * dt;
            held.AngleVel *= Mathf.Exp(-0.22f * dt);
            held.Angle += held.AngleVel * dt;

            if (held.Angle > MaxAngle)
            {
                held.Angle = MaxAngle;
                held.AngleVel = Mathf.Min(0f, held.AngleVel);
            }
            else if (held.Angle < -MaxAngle)
            {
                held.Angle = -MaxAngle;
                held.AngleVel = Mathf.Max(0f, held.AngleVel);
            }

            ApplyPose(held);
            Place(fighter);

            if (!steer) return;

            bool heldKey = Input.GetKey(KeyCode.Space) || Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow);
            bool pressed = Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow);
            if (waitRelease)
            {
                if (!heldKey) waitRelease = false;
            }
            else if (pressed)
            {
                Release(fighter);
            }
        }

        void Update()
        {
            if (vines == null) return;

            float time = Time.time;
            for (int i = 0; i < vines.Length; i++)
            {
                var vine = vines[i];
                if (vine == held || vine.Rope == null) continue;
                float idle = Mathf.Sin(time * 0.65f + i * 0.9f) * (vine.Grabbable ? 0.07f : 0.12f);
                vine.Angle = Mathf.Lerp(vine.Angle, idle, 1f - Mathf.Exp(-2.2f * Time.deltaTime));
                vine.AngleVel = 0f;
                ApplyPose(vine);
            }
        }

        void Release(VizcachaFighter fighter)
        {
            if (held == null) return;

            float angle = held.Angle;
            float speed = held.AngleVel;
            // Held at the end of the arc, angular speed is ~0. Still throw toward the laguna.
            if (Mathf.Abs(speed) < 0.35f)
            {
                float gained = Mathf.Sqrt(2f * Gravity * (1f - Mathf.Cos(angle)) / Mathf.Max(0.5f, held.Length));
                speed = -Mathf.Sign(Mathf.Abs(angle) < 0.05f ? 1f : angle) * Mathf.Max(1.15f, gained);
            }

            float length = held.Length;
            float vx = Mathf.Clamp(Mathf.Cos(angle) * speed * length, -16f, 16f);
            float vy = Mathf.Sin(angle) * speed * length + 7f;
            vy = Mathf.Clamp(vy, 5f, 18f);

            held = null;
            rider = null;
            waitRelease = false;
            fighter.AcceptLaunch(vx, vy);
        }

        void Build(float floorY)
        {
            var sprite = RopeSprite();
            float[] playX = { -2f, 6f, 14f, 22f };
            float[] dressX = { -22f, -12f, 10f, 18f, 27f };
            vines = new Vine[playX.Length + dressX.Length];

            int index = 0;
            foreach (float x in playX)
                vines[index++] = MakeVine(sprite, x, floorY + PlayAnchor, PlayLength, true, 0.34f);
            foreach (float x in dressX)
                vines[index++] = MakeVine(sprite, x, floorY + 7.5f, 1.35f, false, 0.2f);
        }

        Vine MakeVine(Sprite sprite, float x, float anchorY, float length, bool grabbable, float worldThickness)
        {
            x = Mathf.Clamp(x, minX + 1f, maxX - 1f);
            const float spriteWidth = 8f / 32f;

            var root = new GameObject(grabbable ? "Liana" : "Rama");
            root.transform.SetParent(transform, false);
            root.transform.position = new Vector3(x, anchorY, 0f);

            var rope = new GameObject("Cuerda");
            rope.transform.SetParent(root.transform, false);
            var renderer = rope.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = grabbable
                ? new Color(0.29f, 0.48f, 0.22f, 1f)
                : new Color(0.18f, 0.34f, 0.16f, 0.85f);
            renderer.sortingOrder = grabbable ? 7 : 4;
            rope.transform.localScale = new Vector3(worldThickness / spriteWidth, length, 1f);

            if (grabbable)
            {
                var leaf = new GameObject("Hoja");
                leaf.transform.SetParent(rope.transform, false);
                leaf.transform.localPosition = new Vector3(0f, -1f, 0f);
                leaf.transform.localScale = new Vector3(0.9f / worldThickness, 0.42f / length, 1f);
                var leafRenderer = leaf.AddComponent<SpriteRenderer>();
                leafRenderer.sprite = sprite;
                leafRenderer.color = new Color(0.45f, 0.68f, 0.28f, 1f);
                leafRenderer.sortingOrder = 8;
            }

            return new Vine
            {
                Rope = rope.transform,
                X = x,
                AnchorY = anchorY,
                Length = length,
                Grabbable = grabbable
            };
        }

        void Place(VizcachaFighter fighter)
        {
            Vector3 bob = Bob(held);
            var position = fighter.transform.position;
            position.x = bob.x;
            position.y = bob.y - hangDrop;
            fighter.transform.position = position;
        }

        static Vector3 Bob(Vine vine)
        {
            return new Vector3(
                vine.X + Mathf.Sin(vine.Angle) * vine.Length,
                vine.AnchorY - Mathf.Cos(vine.Angle) * vine.Length,
                0f);
        }

        static void ApplyPose(Vine vine)
        {
            if (vine.Rope == null) return;
            vine.Rope.localRotation = Quaternion.Euler(0f, 0f, vine.Angle * Mathf.Rad2Deg);
        }

        static float HandHeight(VizcachaFighter fighter)
        {
            var renderer = fighter.GetComponent<SpriteRenderer>();
            if (renderer == null || renderer.sprite == null) return 2f;
            return Mathf.Clamp(renderer.bounds.size.y * 0.46f, 1.2f, 2.6f);
        }

        static Sprite RopeSprite()
        {
            var tex = new Texture2D(8, 32, TextureFormat.RGBA32, false);
            tex.hideFlags = HideFlags.HideAndDontSave;
            tex.wrapMode = TextureWrapMode.Clamp;
            for (int y = 0; y < 32; y++)
                for (int x = 0; x < 8; x++)
                {
                    float edge = (x == 0 || x == 7) ? 0.45f : 1f;
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, edge));
                }
            tex.Apply();
            var sprite = Sprite.Create(tex, new Rect(0, 0, 8, 32), new Vector2(0.5f, 1f), 32f);
            sprite.hideFlags = HideFlags.HideAndDontSave;
            return sprite;
        }
    }
}
