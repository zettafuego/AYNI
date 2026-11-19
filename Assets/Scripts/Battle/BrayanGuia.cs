using UnityEngine;

namespace AYNI.Battle
{
    /// <summary>
    /// Brayan stands with the others and offers the way. He does not fight.
    /// Every few seconds he walks into a rock and steps back.
    /// </summary>
    public class BrayanGuia : MonoBehaviour
    {
        float baseY;
        float homeX;
        float time;
        float nextBump;
        bool againstRock;
        SpriteFrameAnimator animator;
        PasoSonido pasos;

        public void Init(float floor)
        {
            baseY = floor;
            homeX = 20f;
            nextBump = 5.5f;
            animator = GetComponent<SpriteFrameAnimator>();
            transform.position = new Vector3(homeX, baseY, 0f);
            animator?.SetFacing(false);
        }

        void Update()
        {
            time += Time.deltaTime;
            var pos = transform.position;
            pos.y = baseY + Mathf.Sin(time * 1.3f) * 0.16f;

            if (!againstRock && time >= nextBump)
            {
                againstRock = true;
                animator?.PlayJump();
            }

            float target = againstRock ? homeX - 0.85f : homeX;
            float speed = againstRock ? 3.4f : 1.3f;
            float nextX = Mathf.MoveTowards(pos.x, target, speed * Time.deltaTime);
            bool stepping = Mathf.Abs(nextX - pos.x) > 0.001f;
            pos.x = nextX;
            if (pasos == null) pasos = GetComponent<PasoSonido>();
            pasos?.Mark(stepping);
            if (againstRock && pos.x <= homeX - 0.8f)
            {
                againstRock = false;
                nextBump = time + Random.Range(7f, 11f);
                animator?.PlayHurt();
            }
            else if (animator != null && !animator.IsPlayingAction)
            {
                animator.PlayLocomotion(againstRock, false);
            }

            transform.position = pos;
        }
    }
}
