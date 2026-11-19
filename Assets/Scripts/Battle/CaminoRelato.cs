using AYNI.Narrative;
using AYNI.UI;
using UnityEngine;
using UnityEngine.UI;

namespace AYNI.Battle
{
    /// <summary>
    /// Splash, then the cuy tells every arc. The mountain is the fight.
    /// </summary>
    public class CaminoRelato : MonoBehaviour
    {
        LagunaBattleManager battle;
        GameObject splash;
        GameObject keys;
        GameObject book;
        Text kicker;
        Text lugar;
        Text body;
        Text count;
        Text nextLabel;
        Button ir;
        LeyDelMonte.Page[] pages = System.Array.Empty<LeyDelMonte.Page>();
        int index;
        bool entersOnLast;
        bool mustFinish;
        float splashTime;
        Image splashMark;
        Image splashLogo;
        Text splashStudio;
        Text splashLema;
        CanvasGroup splashCard;
        CanvasGroup[] splashSlots = System.Array.Empty<CanvasGroup>();
        RectTransform[] splashSlotRects = System.Array.Empty<RectTransform>();
        Sprite[] splashLogos = System.Array.Empty<Sprite>();
        int splashLogoIndex = -1;
        GameObject nota;
        Text notaTitle;
        Text notaBody;
        bool notaOpen;
        bool queued;
        string queuedKicker;
        string queuedBody;

        public static bool Reading { get; private set; }

        public bool AteThisFrame { get; private set; }
        public bool HoldsTheGate =>
            (splash != null && splash.activeSelf)
            || (keys != null && keys.activeSelf)
            || (book != null && book.activeSelf);

        public static CaminoRelato Ensure(Transform canvas, LagunaBattleManager battle)
        {
            var relato = canvas.GetComponent<CaminoRelato>();
            if (relato == null) relato = canvas.gameObject.AddComponent<CaminoRelato>();
            relato.battle = battle;
            relato.Build(canvas);
            return relato;
        }

        public void OpenKeys()
        {
            if (keys == null) return;
            keys.SetActive(true);
            keys.transform.SetAsLastSibling();
        }

        public void Open(LeyDelMonte.Page[] source, bool enterMountain)
        {
            if (source == null || source.Length == 0 || book == null) return;
            pages = source;
            entersOnLast = enterMountain;
            index = 0;
            book.SetActive(true);
            book.transform.SetAsLastSibling();
            Show();
        }

        void Build(Transform canvas)
        {
            if (splash == null) BuildSplash(canvas);
            if (keys == null) BuildKeys(canvas);
            if (book == null) BuildBook(canvas);
            if (nota == null) BuildNota(canvas);
            if (splash != null) splash.transform.SetAsLastSibling();
        }

        void BuildSplash(Transform canvas)
        {
            var plate = Storybook.Plate(canvas, "Splash", Vector2.zero, Vector2.one, Color.black);
            plate.raycastTarget = true;
            splash = plate.gameObject;

            // First beat: the A on its own, like a stamp pressed on the dark.
            var mark = Storybook.Plate(splash.transform, "LogoMarca",
                new Vector2(0.40f, 0.46f), new Vector2(0.60f, 0.74f), Color.white);
            Storybook.Paint(mark, "Battle/Logos/Ayni_Icono_Oro");
            mark.preserveAspect = true;
            mark.raycastTarget = false;
            mark.color = new Color(1f, 1f, 1f, 0f);
            splashMark = mark;

            // Then the full lockup, cycling through the family of variants.
            var logo = Storybook.Plate(splash.transform, "Logo",
                new Vector2(0.18f, 0.575f), new Vector2(0.82f, 0.805f), Color.white);
            Storybook.Paint(logo, "Battle/Logos/Ayni_Logo_Blanco");
            logo.preserveAspect = true;
            logo.raycastTarget = false;
            if (logo.sprite == null)
            {
                Storybook.Paint(logo, "Battle/Pantallas/Waffles");
                logo.preserveAspect = true;
            }
            logo.color = new Color(1f, 1f, 1f, 0f);
            splashLogo = logo;

            splashLogos = new[]
            {
                BattleRuntimeAssets.LoadBackground("Battle/Logos/Ayni_Logo_Blanco"),
                BattleRuntimeAssets.LoadBackground("Battle/Logos/Ayni_Logo_Oro"),
                BattleRuntimeAssets.LoadBackground("Battle/Logos/Ayni_Logo_Verde"),
                BattleRuntimeAssets.LoadBackground("Battle/Logos/Ayni_Logo_Hueso"),
            };

            // The card: three heroes, each one framed, exactly as the story calls them.
            var lema = NewText(splash.transform, "Lema",
                new Vector2(0.26f, 0.510f), new Vector2(0.74f, 0.558f));
            Storybook.DressText(lema, "LA LEY DEL MONTE", Storybook.LabelFont, 17,
                new Color(Storybook.Gold.r, Storybook.Gold.g, Storybook.Gold.b, 0f), FontStyle.Bold);
            lema.alignment = TextAnchor.MiddleCenter;
            splashLema = lema;

            Storybook.Plate(splash.transform, "CartaSombra",
                new Vector2(0.215f, 0.214f), new Vector2(0.785f, 0.496f),
                new Color(0f, 0f, 0f, 0.55f));

            var frame = Storybook.Plate(splash.transform, "Carta",
                new Vector2(0.215f, 0.222f), new Vector2(0.785f, 0.504f), Storybook.Gold);
            frame.raycastTarget = false;

            var inner = Storybook.Plate(frame.transform, "CartaFondo",
                Vector2.zero, Vector2.one, Storybook.Panel);
            inner.rectTransform.offsetMin = new Vector2(6f, 6f);
            inner.rectTransform.offsetMax = new Vector2(-6f, -6f);

            splashCard = frame.gameObject.AddComponent<CanvasGroup>();
            splashCard.alpha = 0f;

            CharacterSheet[] sheets = { BattleCast.Vizcacha, BattleCast.Atoq, BattleCast.Conejo };
            string[] nombres = { "VIZCACHA", "ATOQ", "CONEJO" };
            float[] x0 = { 0.022f, 0.342f, 0.662f };
            const float slotW = 0.316f;

            splashSlots = new CanvasGroup[sheets.Length];
            splashSlotRects = new RectTransform[sheets.Length];
            for (int i = 0; i < sheets.Length; i++)
            {
                var slot = Storybook.Plate(frame.transform, "Ficha" + i,
                    new Vector2(x0[i], 0.13f), new Vector2(x0[i] + slotW, 0.97f),
                    new Color(Storybook.Gold.r, Storybook.Gold.g, Storybook.Gold.b, 0.55f));

                var relleno = Storybook.Plate(slot.transform, "Relleno", Vector2.zero, Vector2.one,
                    new Color(0.055f, 0.115f, 0.075f, 0.99f));
                relleno.rectTransform.offsetMin = new Vector2(2f, 2f);
                relleno.rectTransform.offsetMax = new Vector2(-2f, -2f);

                var retrato = Storybook.Plate(slot.transform, "Retrato",
                    new Vector2(0.04f, 0.20f), new Vector2(0.96f, 1.00f), Color.white);
                retrato.sprite = Portrait(sheets[i]);
                retrato.preserveAspect = true;
                retrato.raycastTarget = false;

                var nombre = NewText(slot.transform, "Nombre",
                    new Vector2(0.04f, 0.03f), new Vector2(0.96f, 0.26f));
                Storybook.DressText(nombre, nombres[i], Storybook.LabelFont, 20,
                    new Color(0.96f, 0.94f, 0.87f, 1f), FontStyle.Bold);
                nombre.alignment = TextAnchor.MiddleCenter;
                Storybook.Stamp(nombre, 1.1f);

                var group = slot.gameObject.AddComponent<CanvasGroup>();
                group.alpha = 0f;
                splashSlots[i] = group;
                splashSlotRects[i] = slot.rectTransform;
            }

            var studio = NewText(splash.transform, "Estudio",
                new Vector2(0.30f, 0.100f), new Vector2(0.70f, 0.158f));
            Storybook.DressText(studio, "WAFFLES  TECH", Storybook.LabelFont, 18,
                new Color(Storybook.Gold.r, Storybook.Gold.g, Storybook.Gold.b, 0f), FontStyle.Bold);
            studio.alignment = TextAnchor.MiddleCenter;
            splashStudio = studio;

            splash.transform.SetAsLastSibling();
        }

        static Sprite Portrait(CharacterSheet sheet)
        {
            if (sheet == null) return null;
            var clip = BattleRuntimeAssets.LoadClip(sheet.Clip("Quieto"));
            if (clip != null && clip.Length > 0) return clip[0];
            clip = BattleRuntimeAssets.LoadClip(sheet.Clip("Feliz"));
            if (clip != null && clip.Length > 0) return clip[0];
            return BattleRuntimeAssets.LoadBackground(sheet.WalkFolder);
        }

        void AnimateSplash(float t)
        {
            // The symbol lands first and leaves when the wordmark takes over.
            float markIn = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.08f, 0.40f, t));
            float markOut = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.56f, 0.76f, t));
            if (splashMark != null)
            {
                splashMark.color = new Color(1f, 1f, 1f, markIn * markOut);
                float scale = Mathf.Lerp(0.82f, 1f, markIn);
                splashMark.rectTransform.localScale = new Vector3(scale, scale, 1f);
            }

            float logoIn = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.58f, 0.88f, t));
            if (splashLogo != null)
            {
                if (splashLogos.Length > 0)
                {
                    int next = t < 0.98f ? 0 : t < 1.34f ? 1 : t < 1.70f ? 2 : t < 2.06f ? 3 : 0;
                    if (next != splashLogoIndex && splashLogos[next] != null)
                    {
                        splashLogoIndex = next;
                        splashLogo.sprite = splashLogos[next];
                        splashLogo.rectTransform.localScale = Vector3.one * 1.045f;
                    }
                }

                float settle = 1f - Mathf.Exp(-14f * Time.unscaledDeltaTime);
                splashLogo.rectTransform.localScale =
                    Vector3.Lerp(splashLogo.rectTransform.localScale, Vector3.one, settle);
                splashLogo.color = new Color(1f, 1f, 1f, logoIn);
            }

            if (splashLema != null)
            {
                float lemaIn = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.80f, 1.04f, t));
                splashLema.color = new Color(Storybook.Gold.r, Storybook.Gold.g, Storybook.Gold.b,
                    lemaIn * 0.9f);
            }

            if (splashCard != null)
            {
                float cardIn = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.86f, 1.16f, t));
                splashCard.alpha = cardIn;
                float cardScale = Mathf.Lerp(0.965f, 1f, cardIn);
                splashCard.transform.localScale = new Vector3(cardScale, cardScale, 1f);
            }

            for (int i = 0; i < splashSlots.Length; i++)
            {
                if (splashSlots[i] == null) continue;
                float start = 0.94f + i * 0.11f;
                float inSlot = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(start, start + 0.24f, t));
                splashSlots[i].alpha = inSlot;
                if (splashSlotRects[i] != null)
                    splashSlotRects[i].anchoredPosition = new Vector2(0f, Mathf.Lerp(-16f, 0f, inSlot));
            }

            if (splashStudio != null)
            {
                float studioIn = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(1.30f, 1.80f, t));
                splashStudio.color = new Color(Storybook.Gold.r, Storybook.Gold.g, Storybook.Gold.b,
                    studioIn * 0.92f);
            }
        }

        void BuildKeys(Transform canvas)
        {
            var dim = Storybook.Plate(canvas, "Teclado", Vector2.zero, Vector2.one, Storybook.Veil);
            dim.raycastTarget = true;
            keys = dim.gameObject;

            Storybook.Card(keys.transform, "Hoja", new Vector2(0.05f, 0.03f), new Vector2(0.95f, 0.97f), Storybook.Panel);

            var title = NewText(keys.transform, "Titulo", new Vector2(0.08f, 0.88f), new Vector2(0.92f, 0.95f));
            Storybook.DressText(title, LeyDelMonte.KeysTitle, Storybook.DisplayFont, 52, Color.white, FontStyle.Bold);
            Storybook.Rule(keys.transform, "Regla", new Vector2(0.08f, 0.862f), new Vector2(0.28f, 0.874f));

            var lead = NewText(keys.transform, "Aviso", new Vector2(0.08f, 0.76f), new Vector2(0.92f, 0.855f));
            Storybook.DressText(lead, LeyDelMonte.KeysLead, Storybook.DisplayFont, 32, Storybook.Play, FontStyle.Bold);

            var left = NewText(keys.transform, "Izquierda", new Vector2(0.08f, 0.20f), new Vector2(0.50f, 0.76f));
            Storybook.DressText(left, LeyDelMonte.KeysLeft, Storybook.BodyFont, 22, Storybook.Chalk, FontStyle.Normal);
            left.alignment = TextAnchor.UpperLeft;
            left.lineSpacing = 0.85f;

            var right = NewText(keys.transform, "Derecha", new Vector2(0.52f, 0.20f), new Vector2(0.92f, 0.76f));
            Storybook.DressText(right, LeyDelMonte.KeysRight, Storybook.BodyFont, 22, Storybook.Chalk, FontStyle.Normal);
            right.alignment = TextAnchor.UpperLeft;
            right.lineSpacing = 0.85f;

            Storybook.StickerButton(keys.transform, "Listo", LeyDelMonte.KeysReady,
                new Vector2(0.32f, 0.045f), new Vector2(0.68f, 0.16f), CloseKeys);
            keys.SetActive(false);
        }

        void BuildBook(Transform canvas)
        {
            var dim = Storybook.Plate(canvas, "Relato", Vector2.zero, Vector2.one, Storybook.Veil);
            dim.raycastTarget = true;
            book = dim.gameObject;

            Storybook.Card(book.transform, "Hoja", new Vector2(0.08f, 0.05f), new Vector2(0.92f, 0.95f), Storybook.Panel);
            kicker = NewText(book.transform, "Kicker", new Vector2(0.12f, 0.86f), new Vector2(0.88f, 0.93f));
            Storybook.Rule(book.transform, "Regla", new Vector2(0.12f, 0.842f), new Vector2(0.32f, 0.854f));
            lugar = NewText(book.transform, "Lugar", new Vector2(0.12f, 0.76f), new Vector2(0.88f, 0.836f));
            body = NewText(book.transform, "Cuerpo", new Vector2(0.12f, 0.24f), new Vector2(0.88f, 0.76f));
            count = NewText(book.transform, "Conteo", new Vector2(0.38f, 0.20f), new Vector2(0.62f, 0.28f));

            var back = Storybook.StickerButton(book.transform, "Atras", "ATRÁS",
                new Vector2(0.10f, 0.07f), new Vector2(0.32f, 0.17f), Back);
            ir = Storybook.StickerButton(book.transform, "Ir", "IR",
                new Vector2(0.35f, 0.07f), new Vector2(0.65f, 0.17f), Go);
            var next = Storybook.StickerButton(book.transform, "Siguiente", "SIGUIENTE",
                new Vector2(0.68f, 0.07f), new Vector2(0.90f, 0.17f), Next);
            nextLabel = next.transform.Find("Label")?.GetComponent<Text>();
            back.transform.SetAsLastSibling();
            ir.transform.SetAsLastSibling();
            next.transform.SetAsLastSibling();
            book.SetActive(false);
        }

        static Text NewText(Transform parent, string name, Vector2 min, Vector2 max)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return go.AddComponent<Text>();
        }

        void Show()
        {
            var page = pages[index];
            Storybook.DressText(kicker, page.Kicker, Storybook.DisplayFont, 28, Storybook.Play, FontStyle.Bold);
            string donde = LeyDelMonte.LugarLinea(page.Lugar);
            if (donde.Length == 0 && mustFinish)
                donde = "Léelo hasta el final. Después eliges por dónde caminar.";
            Storybook.DressText(lugar, donde, Storybook.DisplayFont, 20, Storybook.Gold, FontStyle.Bold);
            if (lugar != null) lugar.gameObject.SetActive(donde.Length > 0);
            Storybook.DressText(body, page.Body, Storybook.BodyFont, 24, Storybook.Chalk, FontStyle.Normal);
            if (body != null)
            {
                body.alignment = TextAnchor.UpperLeft;
                body.lineSpacing = 1.05f;
                body.resizeTextForBestFit = false;
            }

            bool last = index >= pages.Length - 1;
            string forward = last && entersOnLast ? "ENTRAR" : last ? "CERRAR" : "SIGUIENTE";
            if (nextLabel != null) nextLabel.text = forward;
            bool showIr = !mustFinish && !string.IsNullOrEmpty(page.Lugar) && !(last && entersOnLast);
            if (ir != null)
            {
                ir.gameObject.SetActive(showIr);
                var irLabel = ir.transform.Find("Label")?.GetComponent<Text>();
                if (irLabel != null) irLabel.text = LeyDelMonte.LugarIr(page.Lugar);
            }

            Storybook.DressText(count, (index + 1) + "  /  " + pages.Length,
                Storybook.DisplayFont, 18, Storybook.Mute, FontStyle.Bold);
        }

        void Go()
        {
            if (pages == null || index < 0 || index >= pages.Length) return;
            string lugarId = pages[index].Lugar;
            mustFinish = false;
            CloseBook();
            if (lugarId == "calles") CajamarcaCalles.Begin(battle);
            else if (lugarId == "valle") CajamarcaRealCapitulo.Begin(battle);
            else if (lugarId == "bosque") BosqueCapitulo.Begin(battle);
            else if (lugarId == "montana") MontanaDelPerol.Begin(battle);
        }

        void Next()
        {
            if (index < pages.Length - 1)
            {
                index++;
                Show();
                return;
            }

            CloseBook();
            if (entersOnLast) MontanaDelPerol.Begin(battle);
        }

        void Back()
        {
            if (index > 0)
            {
                index--;
                Show();
                return;
            }

            if (mustFinish) return;
            CloseBook();
        }

        void CloseBook()
        {
            if (book != null) book.SetActive(false);
            mustFinish = false;
            FlushNota();
        }

        void CloseSplash()
        {
            AteThisFrame = true;
            if (splash != null) splash.SetActive(false);
        }

        void CloseKeys()
        {
            AteThisFrame = true;
            if (keys != null) keys.SetActive(false);
            FlushNota();
        }

        public void Tell(string kicker, string body)
        {
            if (string.IsNullOrEmpty(body) || nota == null) return;
            if (notaOpen || (book != null && book.activeSelf) || (keys != null && keys.activeSelf))
            {
                queuedKicker = kicker;
                queuedBody = body;
                queued = true;
                return;
            }

            PresentNota(kicker, body);
        }

        void PresentNota(string kicker, string body)
        {
            Storybook.DressText(notaTitle, kicker, Storybook.DisplayFont, 26, Storybook.Play, FontStyle.Bold);
            Storybook.DressText(notaBody, body, Storybook.BodyFont, 24, Storybook.Chalk, FontStyle.Bold);
            if (notaBody != null)
            {
                notaBody.alignment = TextAnchor.UpperLeft;
                notaBody.resizeTextForBestFit = false;
            }

            nota.SetActive(true);
            nota.transform.SetAsLastSibling();
            notaOpen = true;
            Reading = true;
            Time.timeScale = 0f;
        }

        void DismissNota()
        {
            if (queued)
            {
                string kicker = queuedKicker;
                string body = queuedBody;
                queued = false;
                PresentNota(kicker, body);
                return;
            }

            notaOpen = false;
            Reading = false;
            if (nota != null) nota.SetActive(false);
            Time.timeScale = 1f;
        }

        void FlushNota()
        {
            if (!queued || notaOpen) return;
            string kicker = queuedKicker;
            string body = queuedBody;
            queued = false;
            PresentNota(kicker, body);
        }

        void BuildNota(Transform canvas)
        {
            var dim = Storybook.Plate(canvas, "NotaDelCuy", Vector2.zero, Vector2.one, Storybook.VeilSoft);
            dim.raycastTarget = true;
            nota = dim.gameObject;
            Storybook.Card(nota.transform, "Hoja", new Vector2(0.14f, 0.22f), new Vector2(0.86f, 0.78f), Storybook.Panel);
            notaTitle = NewText(nota.transform, "Titulo", new Vector2(0.18f, 0.64f), new Vector2(0.82f, 0.74f));
            notaBody = NewText(nota.transform, "Cuerpo", new Vector2(0.18f, 0.38f), new Vector2(0.82f, 0.64f));
            var who = NewText(nota.transform, "Voz", new Vector2(0.18f, 0.32f), new Vector2(0.82f, 0.38f));
            Storybook.DressText(who, "— El cuy", Storybook.DisplayFont, 20, Storybook.Gold, FontStyle.Bold);
            if (who != null) who.alignment = TextAnchor.MiddleLeft;
            Storybook.StickerButton(nota.transform, "Seguir", "SEGUIR",
                new Vector2(0.30f, 0.25f), new Vector2(0.70f, 0.33f), DismissNota);
            nota.SetActive(false);
        }

        void OnDisable()
        {
            if (!Reading) return;
            Reading = false;
            notaOpen = false;
            Time.timeScale = 1f;
        }

        void Update()
        {
            if (notaOpen)
            {
                if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)
                    || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Escape))
                {
                    AteThisFrame = true;
                    Storybook.TocarBoton();
                    DismissNota();
                }

                return;
            }

            if (splash != null && splash.activeSelf)
            {
                splashTime += Time.unscaledDeltaTime;
                AnimateSplash(splashTime);
                bool skip = splashTime > 0.45f && Input.anyKeyDown;
                if (splashTime > 3.45f || skip)
                    CloseSplash();
                return;
            }

            if (keys != null && keys.activeSelf)
            {
                if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)
                    || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Escape))
                {
                    Storybook.TocarBoton();
                    CloseKeys();
                }
                return;
            }

            if (book == null || !book.activeSelf) return;

            if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D)
                || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space))
            {
                AteThisFrame = true;
                Storybook.TocarBoton();
                Next();
            }
            else if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.Escape))
            {
                AteThisFrame = true;
                Storybook.TocarBoton();
                Back();
            }
        }

        void LateUpdate()
        {
            AteThisFrame = false;
        }
    }
}
