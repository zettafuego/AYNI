using UnityEngine;

namespace AYNI.Battle
{
    public class VizcachaFighter : MonoBehaviour
    {
        [SerializeField] float moveSpeed = 10f;
        [SerializeField] float jumpForce = 20f;
        [SerializeField] float gravity = 32f;
        [SerializeField] int maxJumps = 2;
        [SerializeField] float kickRange = 3.5f;
        [SerializeField] float kickDamage = 12f;
        [SerializeField] float kickCooldown = 1f;
        [SerializeField] float healCooldown = 2f;
        [SerializeField] float healAmount = 15f;
        [SerializeField] float bodyDamage = 5f;
        [SerializeField] float bodyCollisionRange = 2.5f;

        public float Health { get; private set; } = 100f;
        public float MaxHealth => 100f;
        public bool IsAlive => Health > 0f;
        public bool IsAirborne => !isGrounded;
        public float PositionX => transform.position.x;

        SpriteFrameAnimator animator;
        LagunaBattleManager battle;
        SombraDelCerro sombra;

        float velocityY;
        bool isGrounded = true;
        int jumpsLeft;
        float floorY;
        BattlePropSpawner propSpawner;
        float kickTimer;
        float healTimer;
        float hitCooldown;
        float splashTimer;
        AudioSource voice;
        AudioClip cry;
        PasoSonido pasos;
        bool facingRight = true;
        bool nativeFacesRight = true;
        bool devil;
        float launchX;
        JungleCanopy canopy;

        void Awake()
        {
            animator = GetComponent<SpriteFrameAnimator>();
            battle = FindFirstObjectByType<LagunaBattleManager>();
            propSpawner = FindFirstObjectByType<BattlePropSpawner>();
        }

        public void Init(float floor) => ResetForBattle(floor);

        public void ResetForBattle(float floor)
        {
            floorY = floor;
            Health = 100f;
            velocityY = 0f;
            launchX = 0f;
            isGrounded = true;
            kickTimer = 0f;
            healTimer = 0f;
            hitCooldown = 0f;
            jumpsLeft = maxJumps;
            transform.position = new Vector3(RioDeEntrada.SpawnX, floorY, 0f);
        }

        void Update()
        {
            if (battle != null && (!battle.IsPlaying || battle.IsGameOver)) return;
            if (MinaDelMuqui.Ocupa) return;

            if (BatanFiambre.BlocksWalker(PositionX))
            {
                animator?.PlayLocomotion(false, false);
                return;
            }

            if (canopy == null) canopy = JungleCanopy.Instance;
            if (battle != null && battle.CommandsOpen)
            {
                if (canopy != null && canopy.Occupies(this))
                    canopy.Drive(this, false);
                return;
            }

            if (canopy != null && canopy.Occupies(this))
            {
                canopy.Drive(this);
                animator?.PlayLocomotion(false, true);
                battle?.OnPlayerMoved(PositionX);
                return;
            }

            float h = Input.GetAxisRaw("Horizontal");
            bool jump = Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow);
            bool kick = Input.GetKeyDown(KeyCode.F);
            bool heal = Input.GetKeyDown(KeyCode.R);

            if (jump && !isGrounded && canopy != null && canopy.TryGrab(this))
            {
                battle?.OnPlayerMoved(PositionX);
                return;
            }

            launchX = Mathf.MoveTowards(launchX, 0f, 18f * Time.deltaTime);
            bool moving = Mathf.Abs(h) > 0.01f || Mathf.Abs(launchX) > 0.4f;

            if (Mathf.Abs(h) > 0.01f)
                Face(h > 0f);

            transform.position += Vector3.right * (h * moveSpeed + launchX) * Time.deltaTime;

            if (jump && jumpsLeft > 0)
            {
                velocityY = jumpForce;
                isGrounded = false;
                jumpsLeft--;
                launchX *= 0.35f;
            }

            var position = transform.position;

            if (!isGrounded)
            {
                velocityY -= gravity * Time.deltaTime;
                position.y += velocityY * Time.deltaTime;

                float groundY = GetGroundHeight(position.x);
                if (position.y <= groundY && velocityY <= 0f)
                {
                    position.y = groundY;
                    velocityY = 0f;
                    launchX = 0f;
                    isGrounded = true;
                    jumpsLeft = maxJumps;
                }
            }
            else
            {
                float groundY = GetGroundHeight(position.x);
                if (position.y > groundY + 0.1f)
                    isGrounded = false;
                else
                    position.y = groundY;
            }

            float minX = battle != null ? battle.ArenaMinX : -9f;
            float maxX = battle != null ? battle.ArenaMaxX : 9f;
            position.x = Mathf.Clamp(position.x, minX, maxX);
            if (splashTimer > 0f) splashTimer -= Time.deltaTime;
            if (RioDeEntrada.Swallows(position.x, position.y, floorY))
            {
                position.x = RioDeEntrada.ReturnX;
                position.y = floorY;
                velocityY = 0f;
                launchX = 0f;
                isGrounded = true;
                jumpsLeft = maxJumps;
                if (splashTimer <= 0f)
                {
                    splashTimer = 1.2f;
                    animator?.PlayFall();
                    string who = FighterRoster.Current.Sheet.Name;
                    battle?.ShowRoadLine("EL RÍO", "El agua te devuelve a la orilla.", who);
                }
            }
            transform.position = position;

            if (kick && kickTimer <= 0f)
                DoKick();

            if (heal && healTimer <= 0f)
                SpawnHealBall();

            if (kickTimer > 0f) kickTimer -= Time.deltaTime;
            if (healTimer > 0f) healTimer -= Time.deltaTime;
            if (hitCooldown > 0f) hitCooldown -= Time.deltaTime;

            if (animator != null && !animator.IsPlayingAction)
            {
                animator.PlayLocomotion(moving, !isGrounded);
                if ((moving || !isGrounded) && Random.value < 0.02f)
                    LeafEffect.Spawn(transform.position, facingRight);
            }

            if (pasos == null) pasos = GetComponent<PasoSonido>();
            pasos?.Mark(moving && isGrounded);

            CheckBodyCollision();
            CheckCollectibles(position);
            battle?.OnPlayerMoved(PositionX);
        }

        float GetGroundHeight(float x)
        {
            float highest = RioDeEntrada.Over(x) ? floorY - RioDeEntrada.Pit : floorY;
            if (propSpawner == null) return highest;

            foreach (var plat in propSpawner.Platforms)
            {
                if (plat == null) continue;
                if (x >= plat.LeftX && x <= plat.RightX)
                    highest = Mathf.Max(highest, plat.TopY);
            }
            return highest;
        }

        void CheckCollectibles(Vector3 pos)
        {
            var items = FindObjectsByType<BattleCollectible>(FindObjectsSortMode.None);
            foreach (var item in items)
                item.TryCollect(pos + Vector3.up * 0.5f);
        }

        public bool CastGolpe(int index)
        {
            if (battle != null && (!battle.IsPlaying || battle.IsGameOver)) return false;
            if (index == 0) return CastKick();
            if (index == 1) return CastRush();
            if (index == 2) return CastHeal();
            if (index == 3) return CastOffering();
            if (index == 4) return CastTail();
            return false;
        }

        bool CastTail()
        {
            if (FighterRoster.Current.Sheet != BattleCast.Vizcacha) return false;
            if (kickTimer > 0f) return false;
            kickTimer = kickCooldown * 0.9f;
            animator?.PlaySpecial();
            Cry();
            TryHit(16f, 4.4f);
            return true;
        }

        public void SetCry(AudioClip clip)
        {
            cry = clip;
            if (voice == null)
            {
                voice = GetComponent<AudioSource>();
                if (voice == null) voice = gameObject.AddComponent<AudioSource>();
                voice.playOnAwake = false;
                voice.loop = false;
            }
        }

        void Cry()
        {
            if (voice == null || cry == null) return;
            voice.PlayOneShot(cry);
        }

        public void StepBack()
        {
            float foeX = FoeX(PositionX + 4f);
            float dir = PositionX <= foeX ? -1f : 1f;
            Face(dir > 0f);
            AcceptLaunch(dir * 11f, 6f);
        }

        bool CastKick()
        {
            if (kickTimer > 0f) return false;
            DoKick();
            return true;
        }

        bool CastRush()
        {
            if (kickTimer > 0f) return false;
            kickTimer = kickCooldown * 1.15f;
            animator?.PlayAction();
            Cry();
            float foeX = FoeX(PositionX + (facingRight ? 4f : -4f));
            float dir = foeX >= PositionX ? 1f : -1f;
            Face(dir > 0f);
            AcceptLaunch(dir * 14f, 6.5f);
            TryHit(18f, 6.2f);
            return true;
        }

        bool CastHeal()
        {
            if (healTimer > 0f) return false;
            if (!ReservaDelCamino.GastarVida()) return false;
            healTimer = healCooldown;
            Heal(healAmount);
            animator?.PlayJump();
            return true;
        }

        bool CastOffering()
        {
            if (healTimer > 0f) return false;
            if (!ReservaDelCamino.GastarAyni()) return false;
            healTimer = healCooldown;
            Heal(8f);
            battle?.NoteOfrenda();
            animator?.PlayAction();
            TryHit(7f, 5.2f);
            return true;
        }

        public bool CastReputacion()
        {
            if (battle != null && (!battle.IsPlaying || battle.IsGameOver)) return false;
            if (healTimer > 0f) return false;
            if (!ReservaDelCamino.GastarReputacion()) return false;
            healTimer = healCooldown;
            Heal(6f);
            battle?.NoteReputacion();
            animator?.PlayJump();
            return true;
        }

        public bool CastCodicia()
        {
            if (battle != null && (!battle.IsPlaying || battle.IsGameOver)) return false;
            if (kickTimer > 0f) return false;
            if (!ReservaDelCamino.GastarCodicia()) return false;
            kickTimer = kickCooldown;
            animator?.PlayAction();
            Cry();
            battle?.NoteCodicia();
            TryHit(20f, 6.5f);
            return true;
        }

        float FoeX(float fallback)
        {
            var foe = ToroEnemyAI.Cercano(PositionX);
            if (foe != null) return foe.PositionX;
            var bug = LivingBug();
            if (bug != null) return bug.PositionX;
            return fallback;
        }

        void DoKick()
        {
            kickTimer = kickCooldown;
            animator?.PlayAction();
            Cry();
            TryHit(kickDamage, kickRange);
        }

        void TryHit(float damage, float range)
        {
            var foe = ToroEnemyAI.Cercano(PositionX);
            if (foe != null)
            {
                HitFoe(foe.PositionX, foe.transform.position.y, () => foe.TakeDamage(damage), foe.transform.position, range);
                return;
            }

            var bug = LivingBug();
            if (bug == null) return;
            HitFoe(bug.PositionX, bug.transform.position.y, () => bug.TakeDamage(damage), bug.transform.position, range);
        }

        void HitFoe(float foeX, float foeY, System.Action hurt, Vector3 burst, float range)
        {
            float dist = Mathf.Abs(PositionX - foeX);
            float yDist = Mathf.Abs(transform.position.y - foeY);
            if (dist >= range || yDist >= range) return;
            hurt();
            LeafEffect.Spawn(burst, facingRight);
            LeafEffect.Spawn(burst + Vector3.up * 0.5f, facingRight);
        }

        void SpawnHealBall()
        {
            healTimer = healCooldown;
            float aimX;
            var foe = ToroEnemyAI.Cercano(PositionX);
            if (foe != null) aimX = foe.PositionX;
            else
            {
                var bug = LivingBug();
                if (bug == null) return;
                aimX = bug.PositionX;
            }

            var go = new GameObject("HealingBall");
            go.transform.position = transform.position + Vector3.up * 0.8f;
            var proj = go.AddComponent<BattleProjectile>();
            float dir = aimX >= PositionX ? 1f : -1f;
            proj.Init(ProjectileType.Healing, dir * 12f, new Color(1f, 0.6f, 0.1f), 0.35f);
        }

        void CheckBodyCollision()
        {
            if (hitCooldown > 0f) return;

            var foe = ToroEnemyAI.Cercano(PositionX);
            if (foe != null)
            {
                float dist = Mathf.Abs(PositionX - foe.PositionX);
                if (dist < bodyCollisionRange)
                {
                    TakeDamage(bodyDamage);
                    hitCooldown = 0.8f;
                    transform.position += Vector3.right * (PositionX < foe.PositionX ? -0.8f : 0.8f);
                    foe.OnBodyCollision(PositionX);
                }
                return;
            }

            var bug = LivingBug();
            if (bug == null) return;
            if (Mathf.Abs(PositionX - bug.PositionX) >= bodyCollisionRange) return;
            TakeDamage(bodyDamage);
            hitCooldown = 0.8f;
            transform.position += Vector3.right * (PositionX < bug.PositionX ? -0.8f : 0.8f);
            bug.OnBodyBump();
        }

        SombraDelCerro LivingBug()
        {
            if (sombra == null) sombra = FindFirstObjectByType<SombraDelCerro>();
            if (sombra == null || !sombra.IsAlive) return null;
            return sombra;
        }

        public void Wear(FighterRoster.Option option)
        {
            nativeFacesRight = option.FacesRight;
            if (animator == null) animator = GetComponent<SpriteFrameAnimator>();
            BattleSprites.Apply(animator, option.Sheet);
            transform.localScale = new Vector3(option.Scale, option.Scale, 1f);
            Face(true);
        }

        public void Face(bool right)
        {
            facingRight = right;
            animator?.SetFacing(right == nativeFacesRight);
        }

        public void AcceptLaunch(float vx, float vy)
        {
            launchX = vx;
            velocityY = vy;
            isGrounded = false;
            jumpsLeft = 1;
        }

        public void TakeDamage(float amount)
        {
            if (hitCooldown > 0f && amount < 10f) return;
            Health = Mathf.Max(0f, Health - amount);
            hitCooldown = 0.5f;
            Shake();
            if (!devil && Health > 0f && Health <= ToroEnemyAI.SickAt &&
                FighterRoster.Current.Sheet == BattleCast.Toro)
                BecomeDevil();
            battle?.OnPlayerHealthChanged(Health);
            if (Health <= 0f)
            {
                animator?.PlayDeath();
                battle?.EndBattle(false);
            }
            else
            {
                animator?.PlayHurt();
            }
        }

        void BecomeDevil()
        {
            devil = true;
            nativeFacesRight = true;
            if (animator == null) animator = GetComponent<SpriteFrameAnimator>();
            BattleSprites.WearDevil(animator, false);
            transform.localScale = new Vector3(CastScale.Diablo, CastScale.Diablo, 1f);
            Face(facingRight);
            if (pasos == null) pasos = GetComponent<PasoSonido>();
            if (pasos != null) pasos.Pitch = 1.35f;
            battle?.ShowRoadLine("EL TORO", AYNI.Narrative.LeyDelMonte.ToroEnfermo, "El diablito");
            battle?.NameTheDevil(true);
        }

        public void Heal(float amount)
        {
            Health = Mathf.Min(MaxHealth, Health + amount);
            battle?.OnPlayerHealthChanged(Health);
        }

        public bool CheckProjectileHit(Vector3 projPos)
        {
            float s = Mathf.Max(0.2f, Mathf.Abs(transform.localScale.y));
            var bounds = new Bounds(transform.position + Vector3.up * (0.5f * s), new Vector3(1.2f * s, 1.5f * s, 0.1f));
            return bounds.Contains(projPos);
        }

        void Shake()
        {
            transform.position += (Vector3)Random.insideUnitCircle * 0.08f;
        }

    }
}