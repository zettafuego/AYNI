using UnityEngine;
using UnityEngine.UI;

namespace AYNI.UI
{
    /// <summary>
    /// The needle's Z is the active yaw. Positive yaw puts north on the left of the screen,
    /// and a positive UI Z turn swings the needle the same way.
    /// </summary>
    public class BrujulaDelMapa : MonoBehaviour
    {
        public static float Rumbo;
        public static bool Mostrar = true;

        RectTransform aguja;
        CanvasGroup grupo;

        public void Bind(RectTransform aguja, CanvasGroup grupo)
        {
            this.aguja = aguja;
            this.grupo = grupo;
        }

        void LateUpdate()
        {
            if (grupo != null)
            {
                grupo.alpha = Mostrar ? 1f : 0f;
                grupo.blocksRaycasts = Mostrar;
                grupo.interactable = Mostrar;
            }

            if (aguja == null) return;
            aguja.localEulerAngles = new Vector3(0f, 0f, Rumbo);
        }
    }
}
