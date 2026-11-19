using UnityEngine;

namespace AYNI.Battle
{
    /// <summary>
    /// Da vida a los frames: respiracion, paso, salto, golpe y muerte.
    /// Trabaja sobre escala, giro y color del mismo objeto, sin moverlo: los
    /// controladores siguen siendo duenos de la posicion. Las almas reciben
    /// ademas un aura y estelas espectrales.
    /// </summary>
    [RequireComponent(typeof(SpriteFrameAnimator))]
    [RequireComponent(typeof(SpriteRenderer))]
    public class SpriteMotion : MonoBehaviour
    {
        [SerializeField] float breathe = 0.014f;
        [SerializeField] float sway = 0.45f;

        const float AttackTime = 0.40f;
        const float HurtTime = 0.26f;
        const float JumpTime = 0.14f;
        const float LandTime = 0.18f;
        const float DeathTime = 0.85f;
        const float SpecialTime = 0.55f;

        SpriteFrameAnimator anim;
        SpriteRenderer body;

        Vector3 baseScale;
        Vector3 appliedScale;
        Quaternion baseRotation;
        Quaternion appliedRotation;
        Color baseColor;
        Color appliedColor;
        bool ready;
        bool facingRight = true;

        float phase;
        float walkPhase;
        float attackAt = -1f;
        float hurtAt = -1f;
        float jumpAt = -1f;
        float landAt = -1f;
        float specialAt = -1f;
        float deathAt = -1f;
        float trailAt;
        bool wasAirborne;
        bool ethereal;

        SpriteRenderer aura;
        Color auraColor = Color.white;
        static Sprite glowSprite;

        void Awake()
        {
            anim = GetComponent<SpriteFrameAnimator>();
            body = GetComponent<SpriteRenderer>();
            phase = Random.value * Mathf.PI * 2f;
            Capture(true);
        }

        void LateUpdate()
        {
            if (anim == null) return;
            Capture(false);
            if (!ready) return;

            float dt = Time.deltaTime > 0.0001f ? Time.deltaTime : 1f / 60f;
            TickTimers(dt);
            Pose(dt);
            DressAura();
            DropTrails(dt);
        }

        public void SetFacing(bool right)
        {
            facingRight = right;
            if (!ready) Capture(true);
            baseScale.x = Mathf.Abs(baseScale.x) * (right ? 1f : -1f);
        }

        public void SetEthereal(bool on, Color color)
        {
            ethereal = on;
            auraColor = color;
            if (on)
                EnsureAura();
            else if (aura != null)
                aura.enabled = false;
        }

        public void OnLocomotion(bool moving, bool inAir)
        {
            if (wasAirborne && !inAir) landAt = 0f;
            wasAirborne = inAir;
        }

        public void OnJump()
        {
            jumpAt = 0f;
            wasAirborne = true;
        }

        public void OnAttack()
        {
            attackAt = 0f;
            trailAt = 0f;
        }

        public void OnHurt()
        {
            hurtAt = 0f;
        }

        public void OnSpecial()
        {
            specialAt = 0f;
            trailAt = 0f;
        }

        public void OnDeath()
        {
            deathAt = 0f;
        }

        public void ResetPose()
        {
            attackAt = hurtAt = jumpAt = landAt = specialAt = deathAt = -1f;
            trailAt = 0f;
            wasAirborne = false;
        }

        // ------------------------------------------------------------ lectura

        void Capture(bool force)
        {
            if (body == null) return;

            Vector3 scale = transform.localScale;
            Quaternion rotation = transform.localRotation;
            Color color = body.color;

            if (force)
            {
                baseScale = scale;
                appliedScale = scale;
                baseRotation = rotation;
                appliedRotation = rotation;
                baseColor = color;
                appliedColor = color;
                ready = true;
                return;
            }

            if ((scale - appliedScale).sqrMagnitude > 1e-8f)
            {
                baseScale = scale;
                baseScale.x = Mathf.Abs(baseScale.x) * (facingRight ? 1f : -1f);
                appliedScale = scale;
            }

            if (Quaternion.Angle(rotation, appliedRotation) > 0.05f)
            {
                baseRotation = rotation;
                appliedRotation = rotation;
            }

            if (!Same(color, appliedColor))
            {
                baseColor = color;
                appliedColor = color;
            }
        }

        // ------------------------------------------------------------- pose

        void TickTimers(float dt)
        {
            if (attackAt >= 0f && (attackAt += dt) > AttackTime) attackAt = -1f;
            if (hurtAt >= 0f && (hurtAt += dt) > HurtTime) hurtAt = -1f;
            if (jumpAt >= 0f && (jumpAt += dt) > JumpTime) jumpAt = -1f;
            if (landAt >= 0f && (landAt += dt) > LandTime) landAt = -1f;
            if (specialAt >= 0f && (specialAt += dt) > SpecialTime) specialAt = -1f;
            if (deathAt >= 0f) deathAt += dt;
        }

        void Pose(float dt)
        {
            float sx = 1f;
            float sy = 1f;
            float roll = 0f;
            float flash = 0f;
            float alpha = 1f;
            Color tint = baseColor;
            float time = Time.time;

            bool busy = anim.IsPlayingAction || anim.IsDead;
            bool idle = !anim.IsMoving && !anim.IsAirborne && !anim.IsSwimming && !busy;

            if (idle)
            {
                float wave = Mathf.Sin(time * (ethereal ? 1.25f : 1.9f) + phase);
                float amount = ethereal ? breathe * 2.6f : breathe;
                sy *= 1f + wave * amount;
                sx *= 1f - wave * amount * 0.55f;
                roll += wave * (ethereal ? sway * 2.1f : sway);
            }

            if (anim.IsMoving && !anim.IsAirborne)
            {
                walkPhase += dt * (ethereal ? 9f : 11f);
                float step = Mathf.Sin(walkPhase);
                sy *= 1f + step * 0.018f;
                sx *= 1f - step * 0.013f;
                roll += step * (ethereal ? 1.9f : 1.0f);
            }

            if (anim.IsSwimming)
            {
                roll += Mathf.Sin(time * 2.7f + phase) * 3.4f;
                sy *= 1f + Mathf.Sin(time * 3.2f + phase) * 0.022f;
            }

            if (jumpAt >= 0f)
            {
                float k = Mathf.Sin(Mathf.Clamp01(jumpAt / JumpTime) * Mathf.PI);
                sy *= 1f + 0.10f * k;
                sx *= 1f - 0.06f * k;
                roll += 4.5f * k;
            }

            if (anim.IsAirborne && jumpAt < 0f)
            {
                sy *= 1.03f;
                sx *= 0.98f;
                roll += Mathf.Sin(time * 4.2f + phase) * 1.4f;
            }

            if (landAt >= 0f)
            {
                float k = Mathf.Sin(Mathf.Clamp01(landAt / LandTime) * Mathf.PI);
                sy *= 1f - 0.13f * k;
                sx *= 1f + 0.09f * k;
            }

            if (attackAt >= 0f)
            {
                float t = attackAt / AttackTime;
                if (t < 0.22f)
                {
                    float k = t / 0.22f;
                    roll -= 5.5f * k;
                    sy *= 1f - 0.05f * k;
                    sx *= 1f + 0.03f * k;
                }
                else if (t < 0.55f)
                {
                    float k = (t - 0.22f) / 0.33f;
                    float hit = Mathf.Sin(k * Mathf.PI);
                    roll += Mathf.Lerp(-5.5f, 10.5f, k);
                    sy *= 1f + 0.07f * hit;
                    sx *= 1f - 0.05f * hit;
                    flash = 0.22f * hit;
                }
                else
                {
                    float k = (t - 0.55f) / 0.45f;
                    roll += Mathf.Lerp(10.5f, 0f, k);
                }
            }

            if (hurtAt >= 0f)
            {
                float k = Mathf.Sin(Mathf.Clamp01(hurtAt / HurtTime) * Mathf.PI);
                roll -= 7.5f * k;
                sx *= 1f + 0.05f * k;
                sy *= 1f - 0.06f * k;
                flash = Mathf.Max(flash, Mathf.Clamp01(1f - hurtAt / 0.13f) * 0.75f);
            }

            if (specialAt >= 0f)
            {
                float t = Mathf.Clamp01(specialAt / SpecialTime);
                float pulse = Mathf.Sin(t * Mathf.PI * 2f);
                sx *= 1f + 0.05f * pulse;
                sy *= 1f - 0.035f * pulse;
                roll += 3.2f * pulse;
                flash = Mathf.Max(flash, Mathf.Sin(t * Mathf.PI) * 0.22f);
            }

            if (deathAt >= 0f)
            {
                float t = Mathf.Clamp01(deathAt / DeathTime);
                roll += 6.5f * t;
                sy *= 1f - 0.12f * t;
                alpha *= 1f - 0.22f * t;
                tint = Color.Lerp(baseColor, new Color(0.74f, 0.88f, 1f, baseColor.a), 0.32f * t);
            }

            if (ethereal && !anim.IsDead)
                flash = Mathf.Max(flash, 0.035f + 0.02f * Mathf.Sin(time * 1.6f + phase));

            var targetScale = new Vector3(baseScale.x * sx, baseScale.y * sy, baseScale.z);
            transform.localScale = targetScale;
            appliedScale = targetScale;

            var targetRotation = baseRotation * Quaternion.Euler(0f, 0f, roll);
            transform.localRotation = targetRotation;
            appliedRotation = targetRotation;

            if (body != null)
            {
                Color final = Color.Lerp(tint, Color.white, Mathf.Clamp01(flash));
                final.a = Mathf.Clamp01(tint.a * alpha);
                body.color = final;
                appliedColor = final;
            }
        }

        // ------------------------------------------------------------ estelas

        void DropTrails(float dt)
        {
            bool attacking = attackAt >= 0f && attackAt < AttackTime * 0.62f;
            bool casting = specialAt >= 0f && specialAt < SpecialTime * 0.8f;
            if (!attacking && !casting) return;

            trailAt += dt;
            if (trailAt < 0.065f) return;
            trailAt = 0f;
            SpawnTrail(attacking ? 0.34f : 0.26f);
        }

        void SpawnTrail(float strength)
        {
            if (body == null || body.sprite == null) return;

            var go = new GameObject("Estela");
            Vector3 side = transform.right * (-0.16f * (facingRight ? 1f : -1f));
            go.transform.position = transform.position + side + Vector3.up * 0.03f;
            go.transform.rotation = transform.rotation;

            Color color = ethereal ? Color.Lerp(baseColor, auraColor, 0.55f) : baseColor;
            color.a = strength;

            var ink = go.AddComponent<SpriteRenderer>();
            ink.sprite = body.sprite;
            ink.color = color;
            ink.sortingOrder = body.sortingOrder - 3;
            go.transform.localScale = transform.lossyScale * 0.97f;
            go.AddComponent<AfterImage>().Arm(color);
        }

        // -------------------------------------------------------------- aura

        void DressAura()
        {
            if (aura == null || body == null) return;
            if (!ethereal || anim.IsDead || !body.enabled || body.sprite == null)
            {
                aura.enabled = false;
                return;
            }

            aura.enabled = true;
            aura.sortingOrder = body.sortingOrder - 2;
            float pulse = 0.5f + 0.5f * Mathf.Sin(Time.time * 1.7f + phase);
            Color color = auraColor;
            color.a = 0.10f + 0.10f * pulse;
            aura.color = color;
            aura.transform.localPosition = new Vector3(0f, 1.62f + pulse * 0.12f, 0.06f);
            aura.transform.localScale = Vector3.one * (3.3f + pulse * 0.28f);
        }

        void EnsureAura()
        {
            if (aura != null || body == null) return;

            var go = new GameObject("Aura");
            go.transform.SetParent(transform, false);
            aura = go.AddComponent<SpriteRenderer>();
            aura.sprite = GlowSprite();
            aura.color = new Color(auraColor.r, auraColor.g, auraColor.b, 0.15f);
            aura.sortingOrder = body.sortingOrder - 2;
            aura.transform.localPosition = new Vector3(0f, 1.62f, 0.06f);
            aura.transform.localScale = Vector3.one * 3.3f;
        }

        static Sprite GlowSprite()
        {
            if (glowSprite != null) return glowSprite;

            const int size = 64;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.hideFlags = HideFlags.HideAndDontSave;
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dx = (x + 0.5f) / size * 2f - 1f;
                float dy = (y + 0.5f) / size * 2f - 1f;
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                float a = Mathf.Clamp01(1f - d);
                a *= a;
                texture.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }

            texture.Apply();
            glowSprite = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
            glowSprite.hideFlags = HideFlags.HideAndDontSave;
            return glowSprite;
        }

        static bool Same(Color a, Color b)
        {
            return Mathf.Abs(a.r - b.r) + Mathf.Abs(a.g - b.g) + Mathf.Abs(a.b - b.b) + Mathf.Abs(a.a - b.a) < 0.002f;
        }
    }

    /// <summary>Una copia del dibujo que se desvanece sola.</summary>
    public class AfterImage : MonoBehaviour
    {
        SpriteRenderer ink;
        Color color;
        float age;
        const float Life = 0.24f;

        public void Arm(Color tint)
        {
            color = tint;
            ink = GetComponent<SpriteRenderer>();
        }

        void Update()
        {
            age += Time.deltaTime;
            float t = Mathf.Clamp01(age / Life);
            if (ink != null)
            {
                Color c = color;
                c.a *= 1f - t;
                ink.color = c;
            }

            if (t >= 1f) Destroy(gameObject);
        }
    }
}
