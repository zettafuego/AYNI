using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace AYNI.UI
{
    /// <summary>
    /// A still button. Hover and press change the scale by a small amount.
    /// A click plays once here. Keyboard confirms call Storybook.TocarBoton themselves,
    /// because onClick.Invoke does not raise this pointer event.
    /// </summary>
    public class MenuButtonMotion : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler, IPointerClickHandler
    {
        public bool Chosen;
        public bool Subtle;

        Action onHot;
        Vector2 basePos;
        Vector3 baseScale;
        bool hasBase;
        bool hot;
        bool pressed;

        public void SetHot(Action hotAction) => onHot = hotAction;

        public void OnPointerEnter(PointerEventData eventData)
        {
            hot = true;
            onHot?.Invoke();
        }

        public void OnPointerExit(PointerEventData eventData) => hot = false;

        public void OnPointerDown(PointerEventData eventData) => pressed = true;

        public void OnPointerUp(PointerEventData eventData) => pressed = false;

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left) return;
            var button = GetComponent<Button>();
            if (button != null && !button.interactable) return;
            Storybook.TocarBoton();
        }

        void Update()
        {
            var rect = (RectTransform)transform;
            if (!hasBase)
            {
                basePos = rect.anchoredPosition;
                baseScale = transform.localScale;
                if (baseScale.sqrMagnitude < 0.01f) baseScale = Vector3.one;
                hasBase = true;
            }

            float pulse = Chosen ? 1.05f : 1f;
            if (hot) pulse = 1.08f;
            if (pressed) pulse = 0.94f;
            float t = 1f - Mathf.Exp(-18f * Time.unscaledDeltaTime);
            transform.localScale = Vector3.Lerp(transform.localScale, baseScale * pulse, t);
            rect.anchoredPosition = basePos;
        }
    }
}
