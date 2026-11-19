using UnityEngine;

namespace AYNI.Battle
{
    /// <summary>
    /// The cuy tells the road from the bank. He does not fight.
    /// </summary>
    public class CuyNarrador : MonoBehaviour
    {
        float baseY;
        float time;
        float goalX;
        bool follow;
        bool steppingOut;
        SpriteFrameAnimator animator;

        public bool Held { get; private set; }

        public void WalkWith()
        {
            follow = true;
        }

        /// Steps out from behind the batán and stays there while he explains the mill.
        public void ComeFrom(float fromX, float toX, float floor)
        {
            Held = true;
            steppingOut = true;
            follow = true;
            baseY = floor;
            goalX = toX;
            transform.position = new Vector3(fromX, floor, 0f);
            var ink = GetComponent<SpriteRenderer>();
            if (ink != null) ink.sortingOrder = 11;
            if (animator == null) animator = GetComponent<SpriteFrameAnimator>();
            animator?.SetFacing(false);
        }

        public void LetGo()
        {
            Held = false;
            steppingOut = false;
            var ink = GetComponent<SpriteRenderer>();
            if (ink != null) ink.sortingOrder = 8;
        }

        public void Init(float floor)
        {
            baseY = floor;
            animator = GetComponent<SpriteFrameAnimator>();
            transform.position = new Vector3(RioDeEntrada.SpawnX - 4.5f, baseY, 0f);
            animator?.SetFacing(true);
        }

        void Update()
        {
            if (steppingOut)
            {
                var pos = transform.position;
                float next = Mathf.MoveTowards(pos.x, goalX, 1.7f * Time.deltaTime);
                bool moving = Mathf.Abs(next - pos.x) > 0.001f;
                pos.x = next;
                pos.y = baseY;
                transform.position = pos;
                if (!moving)
                {
                    steppingOut = false;
                    var ink = GetComponent<SpriteRenderer>();
                    if (ink != null) ink.sortingOrder = 18;
                    animator?.SetFacing(true);
                }
                else if (pos.x < goalX + 0.45f)
                {
                    var ink = GetComponent<SpriteRenderer>();
                    if (ink != null) ink.sortingOrder = 18;
                }

                animator?.PlayLocomotion(moving, false);
                return;
            }

            if (follow || Held) return;
            time += Time.deltaTime;
            var idle = transform.position;
            idle.y = baseY + Mathf.Sin(time * 1.1f) * 0.05f;
            transform.position = idle;
            animator?.PlayLocomotion(false, false);
        }
    }
}
