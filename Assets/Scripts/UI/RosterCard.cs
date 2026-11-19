using System.Collections.Generic;
using AYNI.Battle;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace AYNI.UI
{
    /// <summary>
    /// A roster sticker. The chosen character plays that sheet's own happy clip.
    /// </summary>
    public class RosterCard : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public bool Chosen;

        Image portrait;
        Sprite[] frames;
        Sprite rest;
        float timer;
        int frame;
        bool hot;
        bool measured;
        SpriteMetrics.Measure measure;

        public void Bind(CharacterSheet sheet)
        {
            if (sheet == null) return;
            measured = SpriteMetrics.TryGetSheet(sheet, out measure);
            var idle = BattleRuntimeAssets.LoadClip(sheet.Clip("Quieto"));
            var feliz = BattleRuntimeAssets.LoadClip(sheet.Clip("Feliz"));
            rest = idle != null && idle.Length > 0 ? idle[0] : null;
            if (feliz != null && feliz.Length > 0)
            {
                var clip = new List<Sprite>();
                if (rest != null) clip.Add(rest);
                clip.AddRange(feliz);
                if (idle != null && idle.Length > 1) clip.Add(idle[1]);
                frames = clip.ToArray();
            }
            else
            {
                frames = BattleRuntimeAssets.LoadClip(sheet.Clip("Salto"));
                if (frames == null || frames.Length == 0)
                    frames = BattleRuntimeAssets.LoadClip(sheet.WalkFolder);
            }
            if ((frames == null || frames.Length == 0) && rest != null)
                frames = new[] { rest };
            if (rest == null && frames != null && frames.Length > 0)
                rest = frames[0];

            var go = new GameObject("Portrait");
            go.transform.SetParent(transform, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.36f);
            rect.anchorMax = new Vector2(0.5f, 0.36f);
            rect.pivot = new Vector2(0.5f, 0f);
            portrait = go.AddComponent<Image>();
            portrait.preserveAspect = true;
            portrait.raycastTarget = false;
            portrait.sprite = rest != null ? rest : (frames != null && frames.Length > 0 ? frames[0] : null);
            Fit(portrait.sprite);
        }

        public void OnPointerEnter(PointerEventData eventData) => hot = true;

        public void OnPointerExit(PointerEventData eventData) => hot = false;

        void Update()
        {
            if (portrait == null || frames == null || frames.Length == 0) return;
            bool live = Chosen || hot;
            portrait.rectTransform.localScale = Vector3.one;
            portrait.color = Chosen ? Color.white : new Color(1f, 1f, 1f, hot ? 1f : 0.72f);
            if (!live)
            {
                portrait.sprite = rest != null ? rest : frames[0];
                frame = 0;
            }
            else
            {
                timer += Time.unscaledDeltaTime;
                if (timer >= 0.14f)
                {
                    timer = 0f;
                    frame = (frame + 1) % frames.Length;
                    portrait.sprite = frames[frame];
                }
            }

            Fit(portrait.sprite);
        }

        /// Every drawing fills the same height, whatever its pixel size.
        void Fit(Sprite sprite)
        {
            if (portrait == null || sprite == null) return;
            var parent = (RectTransform)transform;
            float boxH = parent.rect.height * 0.60f;
            float boxW = parent.rect.width * 0.92f;
            if (boxH < 4f || boxW < 4f) return;

            float canvasH = Mathf.Max(1f, sprite.rect.height);
            float canvasW = Mathf.Max(1f, sprite.rect.width);
            float aspect = canvasW / canvasH;
            float imageH = boxH;
            float imageW = imageH * aspect;

            if (measured)
            {
                // El cuerpo visible llena la caja; el lienzo de aire queda fuera.
                imageH = boxH / Mathf.Max(0.05f, measure.HeightRatio);
                imageW = imageH * aspect;
                float visibleW = imageW * measure.WidthRatio;
                if (visibleW > boxW)
                {
                    float k = boxW / visibleW;
                    imageH *= k;
                    imageW *= k;
                }
            }
            else if (imageW > boxW)
            {
                imageW = boxW;
                imageH = imageW / aspect;
            }

            var rect = portrait.rectTransform;
            rect.anchorMin = new Vector2(0.5f, 0.36f);
            rect.anchorMax = new Vector2(0.5f, 0.36f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.sizeDelta = new Vector2(imageW, imageH);
            // Los pies del dibujo caen sobre la misma línea que las otras fichas.
            float drop = measured ? (1f - measure.FeetFromTop) * imageH : 0f;
            rect.anchoredPosition = new Vector2(0f, -drop);
        }
    }
}
