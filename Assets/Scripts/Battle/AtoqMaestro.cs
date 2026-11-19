using UnityEngine;

namespace AYNI.Battle
{
    /// <summary>
    /// The fox walks the left side of the monte. He teaches; he does not fight.
    /// </summary>
    public class AtoqMaestro : MonoBehaviour
    {
        SpriteFrameAnimator animator;
        LagunaBattleManager battle;
        PasoSonido pasos;
        float floorY = -2.5f;
        float left = -24f;
        float right = -12f;
        float x = -18f;
        float dir = 1f;
        float hopTime = -1f;
        float nextHop = 3.2f;
        float nextSwipe = 5.4f;

        public void Init(float floor, float minX, float maxX)
        {
            floorY = floor;
            left = minX;
            right = maxX;
            x = (minX + maxX) * 0.5f;
            transform.position = new Vector3(x, floorY, 0f);
        }

        void Awake()
        {
            animator = GetComponent<SpriteFrameAnimator>();
            battle = FindFirstObjectByType<LagunaBattleManager>();
        }

        void Update()
        {
            if (battle != null && battle.IsGameOver)
            {
                animator?.PlayLocomotion(false, false);
                if (pasos == null) pasos = GetComponent<PasoSonido>();
                pasos?.Mark(false);
                return;
            }

            float dt = Time.deltaTime;
            if (hopTime >= 0f)
            {
                hopTime += dt;
                float u = Mathf.Clamp01(hopTime / 0.55f);
                transform.position = new Vector3(x, floorY + Mathf.Sin(u * Mathf.PI) * 1.5f, 0f);
                if (u >= 1f) hopTime = -1f;
                if (pasos == null) pasos = GetComponent<PasoSonido>();
                pasos?.Mark(false);
                return;
            }

            x += dir * 2.4f * dt;
            if (x > right)
            {
                x = right;
                dir = -1f;
            }
            else if (x < left)
            {
                x = left;
                dir = 1f;
            }

            animator?.SetFacing(dir > 0f);
            transform.position = new Vector3(x, floorY, 0f);

            nextHop -= dt;
            nextSwipe -= dt;
            if (animator != null && !animator.IsPlayingAction && nextHop <= 0f)
            {
                nextHop = 4.8f;
                hopTime = 0f;
                animator.PlayJump();
                return;
            }

            if (animator != null && !animator.IsPlayingAction && nextSwipe <= 0f)
            {
                nextSwipe = 6.8f;
                animator.PlayAction();
            }

            animator?.PlayLocomotion(true, false);
            if (pasos == null) pasos = GetComponent<PasoSonido>();
            pasos?.Mark(true);
        }
    }
}
