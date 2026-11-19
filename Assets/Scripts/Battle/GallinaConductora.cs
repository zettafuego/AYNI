using UnityEngine;

namespace AYNI.Battle
{
    /// <summary>
    /// The painted hen drives back and forth. Her art faces left.
    /// </summary>
    public class GallinaConductora : MonoBehaviour
    {
        public float Left = -14f;
        public float Right = 14f;
        public float Speed = 4.2f;
        /// <summary>Eje de la calle en el plano XZ (x, z). Por defecto, el eje X.</summary>
        public Vector2 Axis = Vector2.right;
        /// <summary>Fase del traqueteo, para que varios carros no salten a la vez.</summary>
        public float Phase;
        public float Bob = 0.045f;
        public float BobRate = 16f;
        /// <summary>La calle sube y baja: el carro sigue la altura del DEM.</summary>
        public bool FollowGround;
        /// <summary>Sobra solo si el arte no apoya las ruedas en el pivote.</summary>
        public float GroundOffset;

        SpriteFrameAnimator anim;
        float dir = -1f;
        float baseY;
        float along;
        float side;

        void Awake()
        {
            Vector3 p = transform.localPosition;
            baseY = p.y;
            Vector2 axis = Axis.sqrMagnitude > 0.0001f ? Axis.normalized : Vector2.right;
            Axis = axis;
            along = p.x * axis.x + p.z * axis.y;
            side = -p.x * axis.y + p.z * axis.x;
            anim = GetComponent<SpriteFrameAnimator>();
            var frames = BattleRuntimeAssets.LoadClip("Calle/Gallina/Conducir");
            if (frames.Length > 0)
                anim.SetFrames(frames);
            anim.SetFacing(true);
            anim.PlayLocomotion(true, false);
        }

        /// <summary>
        /// Coloca el carro en una calle que puede estar girada y le fija el
        /// sentido inicial. Se llama despues de AddComponent (cuando Awake ya
        /// corrio) para que along/side correspondan al eje real.
        /// </summary>
        public void Configure(Vector3 local, Vector2 axis, bool rightward)
        {
            Axis = axis.sqrMagnitude > 0.0001f ? axis.normalized : Vector2.right;
            transform.localPosition = local;
            baseY = local.y;
            along = local.x * Axis.x + local.z * Axis.y;
            side = -local.x * Axis.y + local.z * Axis.x;
            dir = rightward ? 1f : -1f;
        }

        void Update()
        {
            if (anim == null) return;
            along += dir * Speed * Time.deltaTime;
            if (along <= Left)
            {
                along = Left;
                dir = 1f;
            }
            else if (along >= Right)
            {
                along = Right;
                dir = -1f;
            }

            float x = along * Axis.x - side * Axis.y;
            float z = along * Axis.y + side * Axis.x;

            var place = transform.localPosition;
            place.x = x;
            place.z = z;
            float floor = FollowGround ? SueloDeLaCalle.Altura(x, z) + GroundOffset : baseY;
            place.y = floor + Mathf.Sin(Time.time * BobRate + Phase) * Bob;
            transform.localPosition = place;

            // El arte mira a la izquierda: true deja el dibujo sin espejar.
            var cam = Camera.main;
            Vector3 right = cam != null ? cam.transform.right : Vector3.right;
            right.y = 0f;
            Vector3 travel = new Vector3(Axis.x * dir, 0f, Axis.y * dir);
            if (right.sqrMagnitude > 0.0001f)
                anim.SetFacing(Vector3.Dot(travel, right.normalized) < 0f);
            anim.PlayLocomotion(true, false);
        }
    }
}
