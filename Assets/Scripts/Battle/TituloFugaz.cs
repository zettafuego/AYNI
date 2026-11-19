using UnityEngine;

namespace AYNI.Battle
{
    /// <summary>
    /// A chapter title or a status line. It stays up for a few seconds, then fades.
    /// Buttons are not attached to this.
    /// </summary>
    public class TituloFugaz : MonoBehaviour
    {
        CanvasGroup group;
        float left;
        bool gone;

        public static TituloFugaz Begin(GameObject target, float seconds = 4.2f)
        {
            if (target == null) return null;
            var group = target.GetComponent<CanvasGroup>();
            if (group == null) group = target.AddComponent<CanvasGroup>();
            group.alpha = 1f;
            group.blocksRaycasts = false;
            group.interactable = false;
            var fade = target.GetComponent<TituloFugaz>();
            if (fade == null) fade = target.AddComponent<TituloFugaz>();
            fade.group = group;
            fade.left = seconds;
            fade.gone = false;
            return fade;
        }

        public void ShowAgain(float seconds = 4.2f)
        {
            left = seconds;
            gone = false;
            if (group != null) group.alpha = 1f;
        }

        void Update()
        {
            if (gone || group == null) return;
            left -= Time.deltaTime;
            if (left > 0f) return;
            group.alpha = Mathf.MoveTowards(group.alpha, 0f, Time.deltaTime / 0.7f);
            if (group.alpha <= 0.01f)
            {
                group.alpha = 0f;
                gone = true;
            }
        }
    }
}
