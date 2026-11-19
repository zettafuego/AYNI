using UnityEngine;

namespace AYNI.Battle
{
    /// <summary>
    /// Plays Quieto, Caminar, Salto, Ataque, Golpe and Caida.
    /// Locomotion advances when the fighter calls PlayLocomotion.
    /// Ataque, Golpe and Caida advance on their own and hold the last frame of a fall.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class SpriteFrameAnimator : MonoBehaviour
    {
        [SerializeField] float idleRate = 6f;
        [SerializeField] float walkRate = 11f;
        [SerializeField] float airRate = 9f;
        [SerializeField] float swimRate = 9f;
        [SerializeField] float locomoteRate = 10f;
        [SerializeField] float actionRate = 14f;
        float prisa = 1f;

        public void Prisa(bool correr) => prisa = correr ? 1.55f : 1f;

        // Golpes por segundo del ciclo quieto. La batería no se queda en un solo dibujo.
        public void Compas(float pasos) => idleRate = Mathf.Clamp(pasos, 1f, 16f);

        enum Mode { Loop, Once, Hold }

        SpriteRenderer spriteRenderer;
        SpriteMotion motion;
        Sprite[] quieto;
        Sprite[] caminar;
        Sprite[] caminarFrente;
        Sprite[] caminarDetras;
        Sprite[] salto;
        Sprite[] ataque;
        Sprite[] golpe;
        Sprite[] caida;
        Sprite[] nadar;
        Sprite[] especial;

        Sprite[] current;
        Sprite[] loopClip;
        Mode mode = Mode.Loop;
        float timer;
        int frameIndex;
        float ghostTimer;
        bool moving;
        bool airborne;
        bool swimming;
        bool dead;
        bool ethereal;
        float faceYaw;

        public bool IsPlayingAction => mode != Mode.Loop || ghostTimer > 0f;
        public bool IsMoving => moving;
        public bool IsAirborne => airborne;
        public bool IsSwimming => swimming;
        public bool IsDead => dead;
        public bool Ethereal => ethereal;

        void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            EnsureMotion();
            ApplyLoop(First(quieto, caminar));
        }

        public void SetClips(Sprite[] idle, Sprite[] walk, Sprite[] jump, Sprite[] attack, Sprite[] hurt, Sprite[] death)
        {
            if (Has(idle)) quieto = idle;
            if (Has(walk)) caminar = walk;
            if (Has(jump)) salto = jump;
            if (Has(attack)) ataque = attack;
            if (Has(hurt)) golpe = hurt;
            if (Has(death)) caida = death;
            if (!Has(quieto) && Has(caminar))
                quieto = new[] { caminar[0] };
            mode = Mode.Loop;
            ghostTimer = 0f;
            dead = false;
            moving = false;
            airborne = false;
            swimming = false;
            motion?.ResetPose();
            ApplyLoop(First(quieto, caminar));
        }

        public void SetFrames(Sprite[] walk, Sprite[] action = null)
        {
            if (Has(walk))
            {
                caminar = walk;
                if (!Has(quieto))
                    quieto = new[] { walk[0] };
            }
            if (Has(action))
                ataque = action;
            mode = Mode.Loop;
            ApplyLoop(First(quieto, caminar));
        }

        public void SetAxialWalks(Sprite[] front, Sprite[] back)
        {
            if (Has(front)) caminarFrente = front;
            if (Has(back)) caminarDetras = back;
        }

        public void PlayWalk(bool moving) => PlayLocomotion(moving, false);

        // Cada cuerpo entra al ciclo en otro cuadro, para que no pisen juntos.
        public void Desfasar(int cuadros)
        {
            if (!Has(current) || spriteRenderer == null) return;
            int n = current.Length;
            if (n <= 1) return;
            int k = cuadros % n;
            if (k < 0) k += n;
            frameIndex = k;
            timer = 0f;
            Show(current[k]);
        }

        public void PlayLocomotion(bool moving, bool airborne, bool backward = false)
        {
            this.moving = moving;
            this.airborne = airborne;
            swimming = false;
            motion?.OnLocomotion(moving, airborne);

            if (mode != Mode.Loop) return;

            Sprite[] next = airborne && Has(salto)
                ? salto
                : moving && Has(caminar) ? caminar : First(quieto, caminar);

            if (!Has(next)) return;

            if (next != loopClip)
            {
                ApplyLoop(next);
                if (next.Length <= 1) return;
            }

            bool reverse = backward && moving && !airborne && next == caminar && next.Length > 1;
            Advance(next, RateFor(next, airborne) * prisa, Mode.Loop, reverse ? -1 : 1);
        }

        /// <summary>
        /// 3D ground step. The body keeps the last move direction.
        /// The camera angle picks the sheet: back, front, or the side cycle.
        /// A top-down camera keeps the side cycle.
        /// </summary>
        public void PlayStep(Vector3 move, bool moving)
        {
            if (FlatCamera())
            {
                PlayFlat(move, moving);
                return;
            }

            if (moving && move.sqrMagnitude > 0.0001f)
                faceYaw = Mathf.Atan2(move.x, move.z) * Mathf.Rad2Deg;

            Present(Mathf.DeltaAngle(faceYaw, ViewYaw()), moving);
        }

        void Present(float delta, bool moving)
        {
            if (mode != Mode.Loop) return;

            float abs = Mathf.Abs(delta);
            if (abs < 55f && Has(caminarDetras))
            {
                SetFacing(true);
                if (moving) PlayAxial(caminarDetras);
                else ShowStill(caminarDetras[0]);
                return;
            }

            if (abs > 125f && Has(caminarFrente))
            {
                SetFacing(true);
                if (moving) PlayAxial(caminarFrente);
                else ShowStill(caminarFrente[0]);
                return;
            }

            if (abs >= 55f && abs <= 125f)
                SetFacing(delta < 0f);

            if (!moving)
            {
                PlayLocomotion(false, false);
                return;
            }

            PlayLocomotion(true, false, abs > 125f && !Has(caminarFrente));
        }

        void PlayFlat(Vector3 move, bool moving)
        {
            if (!moving || move.sqrMagnitude < 0.0001f)
            {
                PlayLocomotion(false, false);
                return;
            }

            var cam = Camera.main;
            Vector3 right = Vector3.right;
            Vector3 forward = Vector3.forward;
            if (cam != null)
            {
                right = cam.transform.right;
                forward = cam.transform.forward;
                right.y = 0f;
                forward.y = 0f;
            }

            float side = 0f;
            float ahead = 0f;
            if (right.sqrMagnitude > 0.0001f)
                side = Vector3.Dot(move, right.normalized);
            if (forward.sqrMagnitude > 0.0001f)
                ahead = Vector3.Dot(move, forward.normalized);

            bool along = Mathf.Abs(ahead) > Mathf.Abs(side) * 0.75f;
            if (along && ahead >= 0f && Has(caminarDetras))
            {
                SetFacing(true);
                PlayAxial(caminarDetras);
                return;
            }

            if (along && ahead < 0f && Has(caminarFrente))
            {
                SetFacing(true);
                PlayAxial(caminarFrente);
                return;
            }

            if (Mathf.Abs(side) > 0.12f)
                SetFacing(side > 0f);
            PlayLocomotion(true, false, ahead < -0.2f && !Has(caminarFrente));
        }

        static bool FlatCamera()
        {
            var cam = Camera.main;
            if (cam == null) return false;
            Vector3 forward = cam.transform.forward;
            forward.y = 0f;
            return forward.sqrMagnitude < 0.04f;
        }

        static float ViewYaw()
        {
            if (CamaraTercera.UseYaw) return CamaraTercera.VistaYaw;
            var cam = Camera.main;
            if (cam == null) return 0f;
            Vector3 forward = cam.transform.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.0001f) return CamaraTercera.VistaYaw;
            return Mathf.Atan2(forward.x, forward.z) * Mathf.Rad2Deg;
        }

        void ShowStill(Sprite sprite)
        {
            moving = false;
            airborne = false;
            swimming = false;
            motion?.OnLocomotion(false, false);
            loopClip = null;
            mode = Mode.Loop;
            Show(sprite);
        }

        void PlayAxial(Sprite[] clip)
        {
            moving = true;
            airborne = false;
            swimming = false;
            motion?.OnLocomotion(true, false);
            if (mode != Mode.Loop) return;
            if (!Has(clip)) return;
            if (clip != loopClip)
            {
                ApplyLoop(clip);
                if (clip.Length <= 1) return;
            }

            // Frente and detras are short posed cycles. Keep one cycle near half a second
            // so two steps do not strobe at the side-walk rate.
            float rate = clip.Length / 0.52f;
            if (rate < 4f) rate = 4f;
            rate *= prisa;
            float tope = walkRate * prisa;
            if (rate > tope) rate = tope;
            Advance(clip, rate, Mode.Loop, 1);
        }

        public void PlayAction()
        {
            if (Has(ataque))
                PlayOnce(ataque, false);
            else
                PlayPhantom(0.42f);
            motion?.OnAttack();
        }

        public void PlayJump()
        {
            airborne = true;
            motion?.OnJump();
            PlayOnce(Has(salto) ? salto : caminar, true);
        }

        public void PlayHurt()
        {
            if (Has(golpe))
                PlayOnce(golpe, true);
            else
                PlayPhantom(0.26f);
            motion?.OnHurt();
        }

        public void SetSwim(Sprite[] frames)
        {
            if (Has(frames)) nadar = frames;
        }

        public void SetSpecial(Sprite[] frames)
        {
            if (Has(frames)) especial = frames;
        }

        public void SetEthereal(bool on, Color aura)
        {
            ethereal = on;
            EnsureMotion();
            motion?.SetEthereal(on, aura);
        }

        public void SetEthereal(bool on) => SetEthereal(on, Color.white);

        public void PlaySpecial()
        {
            if (Has(especial))
                PlayOnce(especial, false);
            else if (Has(ataque))
                PlayOnce(ataque, false);
            else
                PlayPhantom(0.46f);
            motion?.OnSpecial();
        }

        public void PlayFall()
        {
            PlayOnce(Has(caida) ? caida : salto, false);
            motion?.OnHurt();
        }

        public void PlaySwim()
        {
            swimming = true;
            moving = true;
            airborne = false;
            motion?.OnLocomotion(true, false);

            var clip = Has(nadar) ? nadar : Has(salto) ? salto : caminar;
            if (!Has(clip)) return;
            if (mode != Mode.Loop || loopClip != clip)
            {
                mode = Mode.Loop;
                ApplyLoop(clip);
            }

            Advance(clip, swimRate, Mode.Loop, 1);
        }

        public void PlayDeath()
        {
            dead = true;
            moving = false;
            airborne = false;
            motion?.OnDeath();
            if (!Has(caida))
            {
                PlayPhantom(0.5f);
                return;
            }

            mode = Mode.Hold;
            current = caida;
            loopClip = null;
            frameIndex = 0;
            timer = 0f;
            Show(caida[0]);
        }

        void PlayPhantom(float seconds)
        {
            ghostTimer = Mathf.Max(ghostTimer, seconds);
        }

        void PlayOnce(Sprite[] clip, bool restart)
        {
            if (!Has(clip) || mode == Mode.Hold) return;
            if (!restart && mode == Mode.Once && current == clip) return;

            mode = Mode.Once;
            current = clip;
            loopClip = null;
            frameIndex = 0;
            timer = 0f;
            Show(clip[0]);
        }

        void Update()
        {
            if (ghostTimer > 0f)
                ghostTimer = Mathf.Max(0f, ghostTimer - Time.deltaTime);

            if (mode == Mode.Loop || !Has(current)) return;
            Advance(current, actionRate, mode, 1);
        }

        float RateFor(Sprite[] clip, bool inAir)
        {
            if (inAir && clip == salto) return airRate;
            if (clip == caminar) return walkRate;
            if (clip == quieto) return idleRate;
            return locomoteRate;
        }

        void Advance(Sprite[] clip, float rate, Mode owner, int step)
        {
            if (!Has(clip) || spriteRenderer == null) return;

            timer += Time.deltaTime;
            float hold = clip.Length == 1 ? 0.28f : 1f / Mathf.Max(1f, rate);
            if (timer < hold) return;

            timer = 0f;
            frameIndex += step;

            if (owner == Mode.Hold)
            {
                frameIndex = Mathf.Clamp(frameIndex, 0, clip.Length - 1);
                Show(clip[frameIndex]);
                return;
            }

            if (owner == Mode.Once && frameIndex >= clip.Length)
            {
                mode = Mode.Loop;
                loopClip = null;
                ApplyLoop(First(quieto, caminar));
                return;
            }

            if (frameIndex >= clip.Length) frameIndex = 0;
            if (frameIndex < 0) frameIndex = clip.Length - 1;
            Show(clip[frameIndex]);
        }

        void ApplyLoop(Sprite[] clip)
        {
            loopClip = clip;
            current = clip;
            frameIndex = 0;
            timer = 0f;
            if (Has(clip))
                Show(clip[0]);
        }

        void Show(Sprite sprite)
        {
            if (spriteRenderer == null)
                spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer != null && sprite != null)
                spriteRenderer.sprite = sprite;
        }

        void EnsureMotion()
        {
            if (motion == null) motion = GetComponent<SpriteMotion>();
            if (motion == null) motion = gameObject.AddComponent<SpriteMotion>();
        }

        public void SetFacing(bool faceRight)
        {
            EnsureMotion();
            if (motion != null)
            {
                motion.SetFacing(faceRight);
                return;
            }

            var scale = transform.localScale;
            float size = Mathf.Abs(scale.x);
            if (size < 0.01f) size = 1f;
            scale.x = size * (faceRight ? 1f : -1f);
            transform.localScale = scale;
        }

        static bool Has(Sprite[] clip) => clip != null && clip.Length > 0;

        static Sprite[] First(Sprite[] preferred, Sprite[] fallback) => Has(preferred) ? preferred : fallback;
    }
}
