using UnityEngine;

namespace AYNI.Battle
{
    public class ToroEnemyAI : MonoBehaviour
    {
        [SerializeField] float moveSpeed = 2.5f;
        [SerializeField] float attackInterval = 2.5f;
        [SerializeField] float auraDamage = 15f;
        [SerializeField] float bodyDamage = 10f;
        [SerializeField] float keepDistance = 6f;
        [SerializeField] float retreatDistance = 2.5f;
        [SerializeField] float chaseMinX = -26f;
        [SerializeField] float chaseMaxX = 26f;

        public float Health { get; private set; } = 100f;
        public float MaxHealth => 100f;
        public bool IsAlive => Health > 0f;
        public float PositionX => transform.position.x;
        public string Grade => "Codicia";

        public const float RevealDistance = 11f;
        public const float SickAt = 40f;

        public bool IsPresent => present;
        public bool IsDevil => devil;

        SpriteFrameAnimator animator;
        SpriteRenderer body;
        VizcachaFighter player;
        LagunaBattleManager battle;
        AudioSource attackSound;
        PasoSonido pasos;

        float attackTimer;
        float hitCooldown;
        float floorY;
        bool facingLeft = true;
        float showTimer;
        float hopTime = -1f;
        float bodyScale = CastScale.Toro;
        bool nativeFacesRight;
        bool waitsAtEnd;
        bool present = true;
        bool devil;

        void Awake()
        {
            animator = GetComponent<SpriteFrameAnimator>();
            body = GetComponent<SpriteRenderer>();
            player = FindFirstObjectByType<VizcachaFighter>();
            battle = FindFirstObjectByType<LagunaBattleManager>();
            attackSound = GetComponent<AudioSource>();
            ApplyScale();
        }

        void ApplyScale()
        {
            transform.localScale = new Vector3(bodyScale, bodyScale, 1f);
        }

        public void Wear(FighterRoster.Option option)
        {
            nativeFacesRight = option.FacesRight;
            waitsAtEnd = option.Sheet == BattleCast.Toro;
            bodyScale = option.Scale;
            if (animator == null) animator = GetComponent<SpriteFrameAnimator>();
            BattleSprites.Apply(animator, option.Sheet);
            ApplyScale();
        }

        public void Init(float floor, float startX) => ResetForBattle(floor, startX);

        public void SetCry(AudioClip clip)
        {
            if (clip == null) return;
            if (attackSound == null) attackSound = GetComponent<AudioSource>();
            if (attackSound == null) attackSound = gameObject.AddComponent<AudioSource>();
            attackSound.clip = clip;
            attackSound.playOnAwake = false;
            attackSound.loop = false;
            attackSound.Stop();
        }

        public void ResetForBattle(float floor, float startX)
        {
            floorY = floor;
            Health = 100f;
            attackTimer = 0f;
            hitCooldown = 0f;
            present = !waitsAtEnd;
            transform.position = new Vector3(startX, floorY, 0f);
            if (body == null) body = GetComponent<SpriteRenderer>();
            if (body != null) body.enabled = present;
            Silence();
        }

        public void Appear() => Appear(true);

        public void Appear(bool grito)
        {
            if (present) return;
            present = true;
            if (body == null) body = GetComponent<SpriteRenderer>();
            if (body != null) body.enabled = true;
            battle?.ShowEnemyLife(true);
            battle?.OnEnemyHealthChanged(Health);
            if (!grito || attackSound == null || attackSound.clip == null) return;
            attackSound.loop = false;
            attackSound.volume = 0.75f;
            attackSound.Play();
        }

        public void Acotar(float minX, float maxX, bool jefe = true)
        {
            chaseMinX = minX;
            chaseMaxX = Mathf.Max(minX + 1f, maxX);
            if (!jefe) waitsAtEnd = false;
        }

        public static ToroEnemyAI Cercano(float x)
        {
            var todos = Object.FindObjectsByType<ToroEnemyAI>(FindObjectsInactive.Exclude);
            ToroEnemyAI mejor = null;
            float mejorDist = float.MaxValue;
            for (int i = 0; i < todos.Length; i++)
            {
                var uno = todos[i];
                if (uno == null || !uno.IsAlive || !uno.present) continue;
                float d = Mathf.Abs(uno.PositionX - x);
                if (d >= mejorDist) continue;
                mejorDist = d;
                mejor = uno;
            }

            return mejor;
        }

        public static bool HayAlgunoPresente()
        {
            var todos = Object.FindObjectsByType<ToroEnemyAI>(FindObjectsInactive.Exclude);
            for (int i = 0; i < todos.Length; i++)
            {
                if (todos[i] != null && todos[i].IsAlive && todos[i].present) return true;
            }

            return false;
        }

        static bool HayAlgunoVivo()
        {
            var todos = Object.FindObjectsByType<ToroEnemyAI>(FindObjectsInactive.Exclude);
            for (int i = 0; i < todos.Length; i++)
            {
                if (todos[i] != null && todos[i].IsAlive) return true;
            }

            return false;
        }

        void Update()
        {
            if (!present) return;
            if (battle != null && (!battle.IsPlaying || battle.IsGameOver || battle.CommandsOpen)) return;
            if (!IsAlive || player == null || !player.IsAlive) return;

            float dist = PositionX - player.PositionX;
            bool moving = false;

            if (dist > keepDistance)
            {
                transform.position += Vector3.left * moveSpeed * Time.deltaTime;
                facingLeft = true;
                moving = true;
            }
            else if (dist < retreatDistance)
            {
                transform.position += Vector3.right * moveSpeed * Time.deltaTime;
                facingLeft = false;
                moving = true;
            }

            var pos = transform.position;
            float leftLimit = Mathf.Max(chaseMinX, RioDeEntrada.Right + RioDeEntrada.FarBank);
            pos.x = Mathf.Clamp(pos.x, leftLimit, chaseMaxX);
            if (hopTime >= 0f)
            {
                hopTime += Time.deltaTime;
                float u = Mathf.Clamp01(hopTime / 0.5f);
                pos.y = floorY + Mathf.Sin(u * Mathf.PI) * (0.55f * bodyScale);
                if (u >= 1f) hopTime = -1f;
            }
            else
            {
                pos.y = floorY;
            }
            transform.position = pos;

            animator?.SetFacing(facingLeft != nativeFacesRight);
            showTimer += Time.deltaTime;
            if (showTimer > 5.5f && animator != null && !animator.IsPlayingAction && hopTime < 0f)
            {
                showTimer = 0f;
                hopTime = 0f;
                animator.PlayJump();
            }
            else if (animator != null && !animator.IsPlayingAction)
            {
                animator.PlayLocomotion(moving, hopTime >= 0f);
            }

            if (pasos == null) pasos = GetComponent<PasoSonido>();
            pasos?.Mark(moving && hopTime < 0f);

            attackTimer += Time.deltaTime;
            if (attackTimer >= attackInterval && player.PositionX < PositionX)
            {
                attackTimer = 0f;
                FireAura();
            }

            if (hitCooldown > 0f) hitCooldown -= Time.deltaTime;
        }

        void FireAura()
        {
            animator?.PlayAction();
            var go = new GameObject("AuraBall");
            go.transform.position = transform.position + Vector3.up * (0.35f * bodyScale);
            var proj = go.AddComponent<BattleProjectile>();
            proj.Init(ProjectileType.Aura, -15f, new Color(0.8f, 0.2f, 0.9f), 0.45f);

            if (!devil && attackSound != null && attackSound.clip != null)
                attackSound.Play();
        }

        public void TakeDamage(float amount)
        {
            if (!present || !IsAlive) return;
            Health = Mathf.Max(0f, Health - amount);
            hitCooldown = 0.4f;
            transform.position += Vector3.right * 0.5f;
            Shake();
            if (waitsAtEnd)
                MonedaDelToro.Spill(transform.position, floorY, amount, battle);
            if (waitsAtEnd && !devil && Health > 0f && Health <= SickAt)
                BecomeDevil();
            battle?.OnEnemyHealthChanged(Health);
            if (Health <= 0f)
            {
                Silence();
                animator?.PlayDeath();
                if (HayAlgunoVivo())
                {
                    var otro = Cercano(player != null ? player.PositionX : PositionX);
                    if (otro != null) battle?.OnEnemyHealthChanged(otro.Health);
                }
                else
                    battle?.EndBattle(true);
            }
            else
            {
                animator?.PlayHurt();
            }
        }

        public void OnBodyCollision(float playerX)
        {
            if (hitCooldown > 0f) return;
            TakeDamage(bodyDamage);
            hitCooldown = 0.8f;
        }

        public bool CheckHealHit(Vector3 projPos)
        {
            var bounds = new Bounds(
                transform.position + Vector3.up * (0.45f * bodyScale),
                new Vector3(1.5f * bodyScale, 1.8f * bodyScale, 0.1f));
            return bounds.Contains(projPos);
        }

        void Shake()
        {
            transform.position += (Vector3)Random.insideUnitCircle * 0.06f;
        }

        void Silence()
        {
            if (attackSound != null && attackSound.isPlaying)
                attackSound.Stop();
        }

        void BecomeDevil()
        {
            devil = true;
            nativeFacesRight = true;
            bodyScale = CastScale.Diablo;
            ApplyScale();
            if (animator == null) animator = GetComponent<SpriteFrameAnimator>();
            BattleSprites.WearDevil(animator, false);
            Silence();
            if (pasos == null) pasos = GetComponent<PasoSonido>();
            if (pasos != null) pasos.Pitch = 1.35f;
            var pos = transform.position;
            pos.y = floorY;
            transform.position = pos;
            battle?.ShowRoadLine("EL TORO", AYNI.Narrative.LeyDelMonte.ToroEnfermo, "El diablito");
            battle?.NameTheDevil(false);
        }
    }
}