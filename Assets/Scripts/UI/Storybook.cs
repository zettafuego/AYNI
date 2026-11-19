using System;
using AYNI.Battle;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace AYNI.UI
{
    /// <summary>
    /// Cinema chrome for the laguna fight. Word buttons are green plates.
    /// The chosen one is a light leaf with dark type. Decorative images do not take clicks.
    /// </summary>
    public static class Storybook
    {
        public static readonly Color Jungle = new Color(0.027f, 0.078f, 0.047f, 0.55f);
        public static readonly Color JungleDeep = new Color(0.027f, 0.078f, 0.047f, 0.82f);
        public static readonly Color Leaf = new Color(0.122f, 0.420f, 0.227f, 1f);
        public static readonly Color Parchment = new Color(0.953f, 0.902f, 0.769f, 0.94f);
        public static readonly Color Ink = new Color(0.165f, 0.129f, 0.086f, 1f);
        public static readonly Color Gold = new Color(0.894f, 0.694f, 0.353f, 1f);
        public static readonly Color Vine = new Color(0.239f, 0.420f, 0.196f, 1f);
        public static readonly Color Soil = new Color(0.10f, 0.13f, 0.08f, 0.9f);
        public static readonly Color InkLine = new Color(0.07f, 0.06f, 0.09f, 1f);
        public static readonly Color Sticker = new Color(0.99f, 0.98f, 0.95f, 0.96f);
        public static readonly Color Spark = new Color(0.22f, 0.90f, 0.86f, 1f);
        public static readonly Color Fox = new Color(0.95f, 0.45f, 0.14f, 1f);
        public static readonly Color Sky = new Color(0.29f, 0.62f, 0.93f, 1f);
        public static readonly Color LeafInk = new Color(0.08f, 0.10f, 0.05f, 1f);
        public static readonly Color LeafHalo = new Color(0.98f, 0.99f, 0.88f, 1f);
        public static readonly Color Panel = new Color(0.045f, 0.12f, 0.075f, 0.96f);
        public static readonly Color Chalk = new Color(0.96f, 0.96f, 0.97f, 1f);
        public static readonly Color Mute = new Color(0.73f, 0.73f, 0.75f, 1f);
        public static readonly Color Play = new Color(0.89f, 0.09f, 0.14f, 1f);
        public static readonly Color Veil = new Color(0.012f, 0.045f, 0.028f, 0.93f);
        public static readonly Color VeilSoft = new Color(0.012f, 0.045f, 0.028f, 0.68f);
        public static readonly Color GreenRest = new Color(0.11f, 0.42f, 0.24f, 1f);
        public static readonly Color GreenHover = new Color(0.18f, 0.55f, 0.31f, 1f);
        public static readonly Color GreenPress = new Color(0.06f, 0.26f, 0.14f, 1f);
        public static readonly Color GreenChosen = new Color(0.73f, 0.90f, 0.50f, 1f);
        public static readonly Color GreenChosenHover = new Color(0.84f, 0.96f, 0.64f, 1f);
        public static readonly Color GreenInk = new Color(0.07f, 0.15f, 0.07f, 1f);
        public static readonly Color GreenChalk = new Color(0.97f, 0.98f, 0.92f, 1f);

        static Font displayFont;
        static Font bodyFont;
        static Font characterFont;
        static Font labelFont;
        static Sprite ornament;
        static Sprite leafArt;
        static Sprite stoneFrame;
        static Sprite goldFrame;
        static Sprite greenPlate;

        /// 172 px of the plaque border is 22 canvas units on the 1920×1080 scaler.
        const float FrameBorder = 172f;
        const float FramePixelsPerUnit = 782f;

        /// The book's own serif, shipped with the game so every machine reads the same.
        public static Font DisplayFont => displayFont ??=
            Loaded("Fonts/CrimsonText-SemiBold") ?? Pick("Palatino Linotype", "Constantia", "Book Antiqua", "Georgia");
        public static Font BodyFont => bodyFont ??=
            Loaded("Fonts/CrimsonText-Regular") ?? Pick("Palatino Linotype", "Constantia", "Book Antiqua", "Georgia");

        /// A small, clean face for names and credits. Not the book serif.
        public static Font LabelFont => labelFont ??=
            Loaded("Fonts/Lato-Bold") ?? DisplayFont;

        /// One family for the whole screen: the character page no longer mixes hands.
        public static Font CharacterFont => characterFont ??= DisplayFont;

        static Font Loaded(string path)
        {
            try
            {
                return Resources.Load<Font>(path);
            }
            catch (Exception)
            {
                return null;
            }
        }

        public static Image Plate(Transform parent, string name, Vector2 min, Vector2 max, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            var image = go.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        /// Short leaf rule under a screen title.
        public static void Rule(Transform parent, string name, Vector2 min, Vector2 max)
        {
            if (parent == null || parent.Find(name) != null) return;
            Plate(parent, name, min, max, GreenChosen);
        }

        public static void DressText(Text text, string content, Font font, int size, Color color, FontStyle style)
        {
            if (text == null) return;
            text.text = content;
            if (font != null) text.font = font;
            text.fontSize = size;
            text.color = color;
            text.fontStyle = style;
            text.alignment = TextAnchor.MiddleCenter;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            text.supportRichText = true;
        }

        public static void Anchors(Component graphic, Vector2 min, Vector2 max)
        {
            if (graphic == null) return;
            var rect = graphic.GetComponent<RectTransform>();
            if (rect == null) return;
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        public static void Corners(Transform parent)
        {
            if (parent == null || parent.Find("VineCorners") != null) return;

            var root = new GameObject("VineCorners");
            root.transform.SetParent(parent, false);
            var rect = root.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            Corner(root.transform, "TL", new Vector2(0f, 1f), new Vector2(16f, -16f), new Vector2(1f, 1f));
            Corner(root.transform, "TR", new Vector2(1f, 1f), new Vector2(-16f, -16f), new Vector2(-1f, 1f));
            Corner(root.transform, "BL", new Vector2(0f, 0f), new Vector2(16f, 16f), new Vector2(1f, -1f));
            Corner(root.transform, "BR", new Vector2(1f, 0f), new Vector2(-16f, 16f), new Vector2(-1f, -1f));
            root.transform.SetAsLastSibling();
        }

        public static void TintBar(Transform bar, Color fillColor)
        {
            if (bar == null) return;
            var background = bar.GetComponent<Image>();
            if (background != null) background.color = InkLine;
            var fill = bar.Find("Fill")?.GetComponent<Image>();
            if (fill != null) fill.color = fillColor;
        }

        public static void Paint(Image image, string resourcePath)
        {
            if (image == null) return;
            var sprite = BattleRuntimeAssets.LoadBackground(resourcePath);
            if (sprite == null) return;
            image.sprite = sprite;
            image.color = Color.white;
            image.type = Image.Type.Simple;
            image.preserveAspect = false;
        }

        public static Image Card(Transform parent, string name, Vector2 min, Vector2 max, Color fill)
        {
            return Plate(parent, name, min, max, fill);
        }

        public static Button StickerButton(Transform parent, string name, string label, Vector2 min, Vector2 max, UnityAction onClick, bool carved = true)
        {
            _ = carved;
            var face = Plate(parent, name, min, max, Color.white);
            face.raycastTarget = true;
            face.sprite = null;
            var button = face.gameObject.AddComponent<Button>();
            var nav = button.navigation;
            nav.mode = Navigation.Mode.None;
            button.navigation = nav;

            var go = new GameObject("Label");
            go.transform.SetParent(face.transform, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(12f, 4f);
            rect.offsetMax = new Vector2(-12f, -4f);
            var text = go.AddComponent<Text>();
            DressText(text, label, DisplayFont, 22, GreenChalk, FontStyle.Bold);
            LeafRead(text, 40);
            if (onClick != null) button.onClick.AddListener(onClick);
            face.gameObject.AddComponent<MenuButtonMotion>();
            LeafColors(button, false);
            return button;
        }

        public enum Marca
        {
            Planta,
            Inclinacion,
            Volver,
            Historia,
            Cerca,
            Lejos,
            Vida,
            Aura,
            Atractivo,
            Volar,
            Editar,
            Casa,
            Arbol,
            Banco,
            Farol,
            Flor,
            Quitar,
            Alforja,
        }

        public static Sprite Icono(Marca marca) => Glyph(marca);

        static Sprite[] marcas;
        static Sprite discoBrujula;
        static Sprite anilloBrujula;
        static Sprite agujaBrujula;

        /// <summary>
        /// Misma placa que el botón de palabras. El dibujo va dentro, sin texto.
        /// La historia y el menú siguen usando StickerButton.
        /// </summary>
        public static Button IconButton(Transform parent, string name, Marca marca, Vector2 min, Vector2 max, UnityAction onClick)
        {
            return IconButton(parent, name, Glyph(marca), min, max, onClick);
        }

        public static Button IconButton(Transform parent, string name, Sprite marca, Vector2 min, Vector2 max, UnityAction onClick)
        {
            var face = Plate(parent, name, min, max, Color.white);
            face.raycastTarget = true;
            face.sprite = null;
            var button = face.gameObject.AddComponent<Button>();
            var nav = button.navigation;
            nav.mode = Navigation.Mode.None;
            button.navigation = nav;

            var go = new GameObject("Marca");
            go.transform.SetParent(face.transform, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.22f, 0.22f);
            rect.anchorMax = new Vector2(0.78f, 0.78f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            var image = go.AddComponent<Image>();
            image.sprite = marca;
            image.color = GreenChalk;
            image.preserveAspect = true;
            image.raycastTarget = false;
            var ink = go.AddComponent<Outline>();
            ink.effectColor = new Color(0.10f, 0.06f, 0.03f, 0.95f);
            ink.effectDistance = new Vector2(1.4f, -1.4f);

            if (onClick != null) button.onClick.AddListener(onClick);
            face.gameObject.AddComponent<MenuButtonMotion>();
            LeafColors(button, false);
            return button;
        }

        /// <summary>
        /// Gold compass. The needle points to world north. A click faces the view north.
        /// It is a mark, not a word.
        /// </summary>
        public static void Brujula(Transform parent)
        {
            if (parent == null || parent.Find("Brujula") != null) return;

            var root = new GameObject("Brujula");
            root.transform.SetParent(parent, false);
            var rect = root.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.463f, 0.865f);
            rect.anchorMax = new Vector2(0.537f, 0.995f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var plate = root.AddComponent<Image>();
            plate.sprite = CompassDisc();
            plate.color = new Color(0.10f, 0.06f, 0.03f, 0.82f);
            plate.raycastTarget = true;
            plate.preserveAspect = true;

            var button = root.AddComponent<Button>();
            var nav = button.navigation;
            nav.mode = Navigation.Mode.None;
            button.navigation = nav;
            button.onClick.AddListener(CamaraTercera.Norte);
            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1f, 0.95f, 0.82f, 1f);
            colors.pressedColor = Gold;
            colors.fadeDuration = 0.05f;
            button.colors = colors;
            root.AddComponent<MenuButtonMotion>();

            var grupo = root.AddComponent<CanvasGroup>();
            Mark(root.transform, "Anillo", CompassRing());
            var aguja = Mark(root.transform, "Aguja", CompassNeedle());
            var brujula = root.AddComponent<BrujulaDelMapa>();
            brujula.Bind(aguja, grupo);
            BrujulaDelMapa.Mostrar = true;
        }

        static RectTransform Mark(Transform parent, string name, Sprite sprite)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.14f, 0.14f);
            rect.anchorMax = new Vector2(0.86f, 0.86f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            var image = go.AddComponent<Image>();
            image.sprite = sprite;
            image.color = Gold;
            image.preserveAspect = true;
            image.raycastTarget = false;
            var ink = go.AddComponent<Outline>();
            ink.effectColor = new Color(0.10f, 0.06f, 0.03f, 0.95f);
            ink.effectDistance = new Vector2(1.2f, -1.2f);
            return rect;
        }

        static Sprite CompassDisc()
        {
            if (discoBrujula != null) return discoBrujula;
            discoBrujula = BlankSprite(px => Disc(px, 64, 32, 32, 30));
            return discoBrujula;
        }

        static Sprite CompassRing()
        {
            if (anilloBrujula != null) return anilloBrujula;
            anilloBrujula = BlankSprite(px =>
            {
                Ring(px, 64, 32, 32, 26, 3);
                Stroke(px, 64, 32, 46, 32, 58, 2);
            });
            return anilloBrujula;
        }

        static Sprite CompassNeedle()
        {
            if (agujaBrujula != null) return agujaBrujula;
            agujaBrujula = BlankSprite(px =>
            {
                Stroke(px, 64, 32, 34, 32, 54, 3);
                Stroke(px, 64, 32, 54, 22, 44, 3);
                Stroke(px, 64, 32, 54, 42, 44, 3);
                Stroke(px, 64, 32, 16, 32, 30, 2);
                Disc(px, 64, 32, 32, 4);
            });
            return agujaBrujula;
        }

        static Sprite BlankSprite(System.Action<Color32[]> paint)
        {
            const int n = 64;
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
            tex.hideFlags = HideFlags.HideAndDontSave;
            tex.filterMode = FilterMode.Point;
            tex.wrapMode = TextureWrapMode.Clamp;
            var px = new Color32[n * n];
            paint(px);
            tex.SetPixels32(px);
            tex.Apply(false, true);
            var sprite = Sprite.Create(tex, new Rect(0f, 0f, n, n), new Vector2(0.5f, 0.5f), 100f);
            sprite.hideFlags = HideFlags.HideAndDontSave;
            return sprite;
        }

        static Sprite Glyph(Marca marca)
        {
            int index = (int)marca;
            if (marcas == null) marcas = new Sprite[24];
            if (index < 0 || index >= marcas.Length) index = 0;
            if (marcas[index] != null) return marcas[index];

            const int n = 64;
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
            tex.hideFlags = HideFlags.HideAndDontSave;
            tex.filterMode = FilterMode.Point;
            tex.wrapMode = TextureWrapMode.Clamp;
            var px = new Color32[n * n];
            switch (marca)
            {
                case Marca.Planta: PaintPlanta(px, n); break;
                case Marca.Inclinacion: PaintInclinacion(px, n); break;
                case Marca.Volver: PaintVolver(px, n); break;
                case Marca.Historia: PaintHistoria(px, n); break;
                case Marca.Cerca: PaintLupa(px, n, true); break;
                case Marca.Vida: PaintVida(px, n); break;
                case Marca.Aura: PaintAura(px, n); break;
                case Marca.Atractivo: PaintAtractivo(px, n); break;
                case Marca.Volar: PaintVolar(px, n); break;
                case Marca.Editar: PaintEditar(px, n); break;
                case Marca.Casa: PaintCasa(px, n); break;
                case Marca.Arbol: PaintArbol(px, n); break;
                case Marca.Banco: PaintBanco(px, n); break;
                case Marca.Farol: PaintFarol(px, n); break;
                case Marca.Flor: PaintFlor(px, n); break;
                case Marca.Quitar: PaintQuitar(px, n); break;
                case Marca.Alforja: PaintAlforja(px, n); break;
                default: PaintLupa(px, n, false); break;
            }

            tex.SetPixels32(px);
            tex.Apply(false, true);
            var sprite = Sprite.Create(tex, new Rect(0f, 0f, n, n), new Vector2(0.5f, 0.5f), 100f);
            sprite.hideFlags = HideFlags.HideAndDontSave;
            marcas[index] = sprite;
            return sprite;
        }

        static readonly Color32 Blanco = new Color32(255, 255, 255, 255);

        static void Plot(Color32[] px, int n, int x, int y)
        {
            if ((uint)x >= (uint)n || (uint)y >= (uint)n) return;
            px[y * n + x] = Blanco;
        }

        static void Disc(Color32[] px, int n, int x, int y, int radius)
        {
            int r2 = radius * radius;
            for (int dy = -radius; dy <= radius; dy++)
            for (int dx = -radius; dx <= radius; dx++)
                if (dx * dx + dy * dy <= r2) Plot(px, n, x + dx, y + dy);
        }

        static void Stroke(Color32[] px, int n, int x0, int y0, int x1, int y1, int radius)
        {
            int steps = Mathf.Max(Mathf.Abs(x1 - x0), Mathf.Abs(y1 - y0));
            if (steps < 1) steps = 1;
            for (int i = 0; i <= steps; i++)
            {
                float t = i / (float)steps;
                Disc(px, n,
                    Mathf.RoundToInt(Mathf.Lerp(x0, x1, t)),
                    Mathf.RoundToInt(Mathf.Lerp(y0, y1, t)),
                    radius);
            }
        }

        static void Ring(Color32[] px, int n, int cx, int cy, int radius, int thick)
        {
            int outer = radius * radius;
            int inner = Mathf.Max(0, radius - thick);
            int hole = inner * inner;
            for (int y = cy - radius; y <= cy + radius; y++)
            for (int x = cx - radius; x <= cx + radius; x++)
            {
                int d = (x - cx) * (x - cx) + (y - cy) * (y - cy);
                if (d <= outer && d >= hole) Plot(px, n, x, y);
            }
        }

        static void Frame(Color32[] px, int n, int x0, int y0, int x1, int y1, int radius)
        {
            Stroke(px, n, x0, y0, x1, y0, radius);
            Stroke(px, n, x0, y1, x1, y1, radius);
            Stroke(px, n, x0, y0, x0, y1, radius);
            Stroke(px, n, x1, y0, x1, y1, radius);
        }

        static void PaintPlanta(Color32[] px, int n)
        {
            Frame(px, n, 10, 10, 54, 54, 2);
            Stroke(px, n, 32, 14, 32, 50, 3);
            Stroke(px, n, 14, 32, 50, 32, 3);
            Disc(px, n, 32, 32, 5);
        }

        static void PaintInclinacion(Color32[] px, int n)
        {
            Stroke(px, n, 8, 40, 56, 40, 2);
            Stroke(px, n, 16, 40, 32, 56, 2);
            Stroke(px, n, 48, 40, 32, 56, 2);
            Stroke(px, n, 28, 36, 12, 8, 2);
            Stroke(px, n, 36, 36, 52, 8, 2);
            Stroke(px, n, 12, 8, 52, 8, 2);
        }

        static void PaintVolver(Color32[] px, int n)
        {
            Stroke(px, n, 18, 32, 50, 32, 3);
            Stroke(px, n, 18, 32, 34, 48, 3);
            Stroke(px, n, 18, 32, 34, 16, 3);
        }

        static void PaintEditar(Color32[] px, int n)
        {
            Stroke(px, n, 14, 12, 46, 44, 3);
            Stroke(px, n, 40, 50, 54, 36, 3);
            Stroke(px, n, 46, 44, 54, 36, 3);
        }

        static void PaintCasa(Color32[] px, int n)
        {
            Frame(px, n, 14, 12, 50, 36, 2);
            Stroke(px, n, 8, 36, 32, 54, 3);
            Stroke(px, n, 56, 36, 32, 54, 3);
            Frame(px, n, 26, 12, 38, 24, 2);
        }

        static void PaintArbol(Color32[] px, int n)
        {
            Stroke(px, n, 32, 8, 32, 28, 3);
            Disc(px, n, 32, 40, 14);
        }

        static void PaintBanco(Color32[] px, int n)
        {
            Stroke(px, n, 10, 24, 54, 24, 4);
            Stroke(px, n, 12, 28, 12, 46, 3);
            Stroke(px, n, 52, 28, 52, 46, 3);
            Stroke(px, n, 12, 44, 52, 44, 3);
        }

        static void PaintFarol(Color32[] px, int n)
        {
            Stroke(px, n, 32, 8, 32, 30, 3);
            Frame(px, n, 20, 30, 44, 50, 2);
            Disc(px, n, 32, 40, 4);
        }

        static void PaintFlor(Color32[] px, int n)
        {
            Stroke(px, n, 32, 8, 32, 30, 2);
            Disc(px, n, 32, 40, 8);
            Disc(px, n, 20, 34, 6);
            Disc(px, n, 44, 34, 6);
        }

        static void PaintQuitar(Color32[] px, int n)
        {
            Stroke(px, n, 16, 16, 48, 48, 4);
            Stroke(px, n, 48, 16, 16, 48, 4);
        }

        static void PaintAlforja(Color32[] px, int n)
        {
            Frame(px, n, 14, 10, 50, 38, 3);
            Stroke(px, n, 14, 38, 32, 54, 3);
            Stroke(px, n, 50, 38, 32, 54, 3);
            Stroke(px, n, 14, 26, 50, 26, 2);
            Stroke(px, n, 22, 38, 22, 56, 2);
            Stroke(px, n, 42, 38, 42, 56, 2);
        }

        public static void Pista(Button boton, string linea)
        {
            if (boton == null || string.IsNullOrEmpty(linea)) return;
            var go = new GameObject("Pista");
            go.transform.SetParent(boton.transform, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(-0.15f, -0.42f);
            rect.anchorMax = new Vector2(1.15f, -0.04f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            var text = go.AddComponent<Text>();
            DressText(text, linea, DisplayFont, 14, new Color(0.98f, 0.94f, 0.82f), FontStyle.Bold);
            text.alignment = TextAnchor.MiddleCenter;
            text.raycastTarget = false;
            text.enabled = false;
            var motion = boton.GetComponent<MenuButtonMotion>();
            if (motion != null) motion.SetHot(() => text.enabled = true);
            boton.gameObject.AddComponent<PistaFuera>().Texto = text;
        }

        sealed class PistaFuera : MonoBehaviour, IPointerExitHandler
        {
            public Text Texto;
            public void OnPointerExit(PointerEventData eventData)
            {
                if (Texto != null) Texto.enabled = false;
            }
        }

        static void PaintVolar(Color32[] px, int n)
        {
            Frame(px, n, 6, 18, 34, 46, 2);
            Ring(px, n, 20, 32, 7, 2);
            Stroke(px, n, 48, 12, 48, 52, 3);
            Stroke(px, n, 48, 52, 38, 40, 3);
            Stroke(px, n, 48, 52, 58, 40, 3);
            Stroke(px, n, 48, 12, 38, 24, 3);
            Stroke(px, n, 48, 12, 58, 24, 3);
        }

        static void PaintHistoria(Color32[] px, int n)
        {
            Frame(px, n, 8, 16, 30, 50, 2);
            Frame(px, n, 34, 16, 56, 50, 2);
            Stroke(px, n, 32, 14, 32, 52, 2);
            Stroke(px, n, 14, 40, 24, 40, 1);
            Stroke(px, n, 14, 32, 24, 32, 1);
            Stroke(px, n, 14, 24, 24, 24, 1);
            Stroke(px, n, 40, 40, 50, 40, 1);
            Stroke(px, n, 40, 32, 50, 32, 1);
            Stroke(px, n, 40, 24, 50, 24, 1);
        }

        static void PaintLupa(Color32[] px, int n, bool plus)
        {
            Ring(px, n, 26, 36, 14, 3);
            Stroke(px, n, 36, 26, 54, 8, 3);
            Stroke(px, n, 19, 36, 33, 36, 2);
            if (plus) Stroke(px, n, 26, 29, 26, 43, 2);
        }

        static void PaintVida(Color32[] px, int n)
        {
            Disc(px, n, 22, 40, 13);
            Disc(px, n, 42, 40, 13);
            for (int y = 12; y <= 34; y++)
            {
                float t = (34 - y) / 22f;
                int half = Mathf.RoundToInt(Mathf.Lerp(20f, 3f, t));
                for (int x = 32 - half; x <= 32 + half; x++)
                    Plot(px, n, x, y);
            }
        }

        static void PaintAura(Color32[] px, int n)
        {
            Ring(px, n, 32, 32, 18, 4);
            Disc(px, n, 32, 32, 6);
            Stroke(px, n, 32, 58, 32, 50, 2);
            Stroke(px, n, 32, 6, 32, 14, 2);
            Stroke(px, n, 6, 32, 14, 32, 2);
            Stroke(px, n, 58, 32, 50, 32, 2);
            Stroke(px, n, 14, 50, 20, 44, 2);
            Stroke(px, n, 50, 50, 44, 44, 2);
            Stroke(px, n, 14, 14, 20, 20, 2);
            Stroke(px, n, 50, 14, 44, 20, 2);
        }

        static void PaintAtractivo(Color32[] px, int n)
        {
            const int cx = 32;
            const int cy = 32;
            const int outer = 22;
            const int inner = 9;
            int prevX = 0;
            int prevY = 0;
            int firstX = 0;
            int firstY = 0;
            for (int i = 0; i < 10; i++)
            {
                float grados = 90f - i * 36f;
                float radio = (i % 2 == 0) ? outer : inner;
                float a = grados * Mathf.Deg2Rad;
                int x = cx + Mathf.RoundToInt(Mathf.Cos(a) * radio);
                int y = cy + Mathf.RoundToInt(Mathf.Sin(a) * radio);
                if (i == 0)
                {
                    firstX = x;
                    firstY = y;
                }
                else
                    Stroke(px, n, prevX, prevY, x, y, 2);
                prevX = x;
                prevY = y;
            }

            Stroke(px, n, prevX, prevY, firstX, firstY, 2);
            Disc(px, n, cx, cy, 4);
        }

        public static void WearLeaf(Image image, bool shadow)
        {
            if (image == null) return;
            image.sprite = null;
            image.type = Image.Type.Simple;
            image.preserveAspect = false;
            image.color = shadow ? new Color(0f, 0f, 0f, 0f) : Color.white;
            image.raycastTarget = !shadow;
        }

        public static void WearFrame(Image image, bool chosen)
        {
            if (image == null) return;
            var frame = Plaque(chosen);
            if (frame == null) return;
            image.sprite = frame;
            image.type = Image.Type.Sliced;
            image.preserveAspect = false;
            image.color = Color.white;
            image.raycastTarget = true;
            SeatPlaqueLabel(image.transform.Find("Label")?.GetComponent<Text>());
        }

        public static void LeafColors(Button button, bool chosen)
        {
            if (button == null) return;
            var face = button.GetComponent<Image>();
            var label = button.transform.Find("Label")?.GetComponent<Text>();
            var marca = button.transform.Find("Marca")?.GetComponent<Image>();
            bool plate = label != null || marca != null;
            if (plate) WearGreen(face);

            bool carved = !plate && face != null && face.sprite != null
                && (face.sprite == stoneFrame || face.sprite == goldFrame);
            var colors = button.colors;
            if (plate)
            {
                colors.normalColor = chosen ? GreenChosen : GreenRest;
                colors.highlightedColor = chosen ? GreenChosenHover : GreenHover;
                colors.pressedColor = chosen ? new Color(0.58f, 0.76f, 0.38f, 1f) : GreenPress;
                colors.selectedColor = colors.normalColor;
                colors.disabledColor = new Color(GreenRest.r, GreenRest.g, GreenRest.b, 0.45f);
            }
            else if (carved)
            {
                var frame = Plaque(chosen);
                if (frame != null) face.sprite = frame;
                face.type = Image.Type.Sliced;
                face.color = Color.white;
                colors.normalColor = Color.white;
                colors.highlightedColor = Color.white;
                colors.pressedColor = new Color(0.86f, 0.86f, 0.86f, 1f);
                colors.selectedColor = Color.white;
                colors.disabledColor = new Color(1f, 1f, 1f, 0.40f);
            }
            else
            {
                colors.normalColor = chosen ? GreenChosen : GreenRest;
                colors.highlightedColor = chosen ? GreenChosenHover : GreenHover;
                colors.pressedColor = chosen ? new Color(0.58f, 0.76f, 0.38f, 1f) : GreenPress;
                colors.selectedColor = colors.normalColor;
                colors.disabledColor = new Color(GreenRest.r, GreenRest.g, GreenRest.b, 0.45f);
            }

            colors.fadeDuration = 0.08f;
            colors.colorMultiplier = 1f;
            button.colors = colors;

            if (label != null)
            {
                label.color = chosen ? GreenInk : GreenChalk;
                var ink = label.GetComponent<Outline>();
                if (ink == null) ink = label.gameObject.AddComponent<Outline>();
                ink.enabled = true;
                ink.effectColor = chosen
                    ? new Color(1f, 1f, 1f, 0.28f)
                    : new Color(0.02f, 0.08f, 0.04f, 0.9f);
                ink.effectDistance = new Vector2(1.15f, -1.15f);

                var halo = label.transform.Find("Halo");
                if (halo != null) halo.gameObject.SetActive(false);
            }

            if (marca != null)
            {
                marca.color = chosen ? GreenInk : GreenChalk;
                var ink = marca.GetComponent<Outline>();
                if (ink == null) ink = marca.gameObject.AddComponent<Outline>();
                ink.enabled = true;
                ink.effectColor = chosen
                    ? new Color(1f, 1f, 1f, 0.35f)
                    : new Color(0.02f, 0.08f, 0.04f, 0.9f);
                ink.effectDistance = new Vector2(1.2f, -1.2f);
            }
        }

        /// White rounded plate. Button colors tint it green.
        public static void WearGreen(Image image)
        {
            if (image == null) return;
            var plate = GreenPlate();
            if (image.sprite != plate || image.type != Image.Type.Sliced)
            {
                image.sprite = plate;
                image.type = Image.Type.Sliced;
                image.preserveAspect = false;
                image.color = Color.white;
            }

            SeatWordLabel(image.transform.Find("Label")?.GetComponent<Text>());
        }

        static void SeatWordLabel(Text label)
        {
            if (label == null) return;
            var rect = label.rectTransform;
            bool plaque = Mathf.Abs(rect.anchorMin.y - 0.22f) < 0.02f
                && Mathf.Abs(rect.anchorMax.y - 0.78f) < 0.02f;
            bool unset = rect.anchorMin == Vector2.zero && rect.anchorMax == Vector2.one
                && rect.offsetMin == Vector2.zero && rect.offsetMax == Vector2.zero;
            if (!plaque && !unset) return;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(18f, 8f);
            rect.offsetMax = new Vector2(-18f, -8f);
        }

        static Sprite GreenPlate()
        {
            if (greenPlate != null) return greenPlate;

            const int n = 64;
            const int radius = 14;
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
            tex.hideFlags = HideFlags.HideAndDontSave;
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;
            for (int y = 0; y < n; y++)
            {
                for (int x = 0; x < n; x++)
                {
                    bool cornerX = x < radius || x >= n - radius;
                    bool cornerY = y < radius || y >= n - radius;
                    float alpha = 1f;
                    float rim = 0f;
                    if (cornerX && cornerY)
                    {
                        float cx = x < radius ? radius - 0.5f : n - radius - 0.5f;
                        float cy = y < radius ? radius - 0.5f : n - radius - 0.5f;
                        float dist = Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy));
                        alpha = dist <= radius - 1f ? 1f : Mathf.Clamp01(radius - dist);
                        rim = Mathf.Clamp01((dist - (radius - 2.4f)) / 2.4f);
                    }
                    else if (x < 2 || y < 2 || x >= n - 2 || y >= n - 2)
                    {
                        rim = 1f;
                    }

                    float shade = Mathf.Lerp(0.84f, 1f, y / (n - 1f));
                    shade = Mathf.Lerp(shade, shade * 0.7f, rim);
                    tex.SetPixel(x, y, new Color(shade, shade, shade, alpha));
                }
            }

            tex.Apply();
            greenPlate = Sprite.Create(
                tex,
                new Rect(0f, 0f, n, n),
                new Vector2(0.5f, 0.5f),
                100f,
                0,
                SpriteMeshType.FullRect,
                new Vector4(radius, radius, radius, radius));
            greenPlate.hideFlags = HideFlags.HideAndDontSave;
            return greenPlate;
        }

        static void SeatPlaqueLabel(Text label)
        {
            if (label == null) return;
            var rect = label.rectTransform;
            rect.anchorMin = new Vector2(0.06f, 0.22f);
            rect.anchorMax = new Vector2(0.94f, 0.78f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        static Sprite Plaque(bool chosen)
        {
            if (stoneFrame == null)
            {
                var stone = Resources.Load<Texture2D>("UI/boton_piedra");
                var bronze = Resources.Load<Texture2D>("UI/boton_oro");
                var border = new Vector4(FrameBorder, FrameBorder, FrameBorder, FrameBorder);
                if (stone != null) stoneFrame = Slice(stone, border);
                if (bronze != null) goldFrame = Slice(bronze, border);
            }

            if (chosen && goldFrame != null) return goldFrame;
            return stoneFrame;
        }

        static Sprite Slice(Texture2D tex, Vector4 border)
        {
            return Sprite.Create(
                tex,
                new Rect(0f, 0f, tex.width, tex.height),
                new Vector2(0.5f, 0.5f),
                FramePixelsPerUnit,
                0,
                SpriteMeshType.FullRect,
                border);
        }

        public static void LeafRead(Text text, int size)
        {
            if (text == null) return;
            text.font = DisplayFont;
            text.fontSize = size;
            text.color = Color.white;
            text.fontStyle = FontStyle.Bold;
            int floor = Mathf.Max(14, size - 16);
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = floor;
            text.resizeTextMaxSize = Mathf.Max(floor, size);
            var halo = text.transform.Find("Halo");
            if (halo != null) halo.gameObject.SetActive(false);
        }

        public static void Stamp(Text text, float distance = 1.6f)
        {
            if (text == null) return;
            var outline = text.GetComponent<Outline>();
            if (outline == null) outline = text.gameObject.AddComponent<Outline>();
            outline.effectColor = InkLine;
            outline.effectDistance = new Vector2(distance, -distance);
        }

        static void Corner(Transform parent, string name, Vector2 anchor, Vector2 pos, Vector2 flip)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = anchor;
            rect.sizeDelta = new Vector2(148f, 148f);
            rect.anchoredPosition = pos;
            var image = go.AddComponent<Image>();
            image.sprite = Ornament();
            image.color = Vine;
            image.raycastTarget = false;
            image.preserveAspect = true;
            go.transform.localScale = new Vector3(flip.x, flip.y, 1f);
        }

        static Font Pick(params string[] names)
        {
            string[] installed;
            try
            {
                installed = Font.GetOSInstalledFontNames();
            }
            catch (Exception)
            {
                installed = Array.Empty<string>();
            }

            if (installed != null)
            {
                foreach (var want in names)
                {
                    for (int i = 0; i < installed.Length; i++)
                    {
                        if (string.Equals(installed[i], want, StringComparison.OrdinalIgnoreCase))
                            return Font.CreateDynamicFontFromOSFont(installed[i], 48);
                    }
                }
            }

            return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }

        static Sprite LeafArt()
        {
            if (leafArt != null) return leafArt;

            const int w = 192;
            const int h = 96;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.hideFlags = HideFlags.HideAndDontSave;
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;
            var clear = new Color(0f, 0f, 0f, 0f);
            var fill = new Color(0.80f, 0.90f, 0.42f, 1f);
            var soft = new Color(0.92f, 0.96f, 0.62f, 1f);
            var vein = new Color(0.22f, 0.40f, 0.13f, 1f);
            var rim = new Color(0.16f, 0.32f, 0.09f, 1f);
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float nx = (x / (w - 1f)) * 2f - 1f;
                    float ny = (y / (h - 1f)) * 2f - 1f;
                    float along = (nx + 1f) * 0.5f;
                    bool stem = nx < -0.78f && nx > -0.98f && Mathf.Abs(ny) < 0.07f;
                    if (stem)
                    {
                        tex.SetPixel(x, y, vein);
                        continue;
                    }

                    float belly = Mathf.Sin(Mathf.Pow(Mathf.Clamp01(along), 0.62f) * Mathf.PI);
                    float half = 0.94f * Mathf.Pow(Mathf.Max(belly, 0f), 0.78f);
                    float d = Mathf.Abs(ny) / (half + 0.0001f);
                    if (d > 1f)
                    {
                        tex.SetPixel(x, y, clear);
                        continue;
                    }

                    float edge = Mathf.Clamp01((1f - d) / 0.16f);
                    Color body = Color.Lerp(soft, fill, Mathf.Clamp01(d * 1.35f));
                    if (Mathf.Abs(ny) * h < 2.1f && along > 0.1f && along < 0.9f)
                        body = vein;
                    else if (along > 0.16f && along < 0.78f && d > 0.28f && d < 0.82f)
                    {
                        float wave = Mathf.Abs(Mathf.Sin(along * 7f * Mathf.PI));
                        if (wave > 0.93f) body = Color.Lerp(body, vein, 0.8f);
                    }

                    body = Color.Lerp(rim, body, edge);
                    body.a = edge > 0.4f ? 1f : edge / 0.4f;
                    tex.SetPixel(x, y, body);
                }
            }

            tex.Apply();
            leafArt = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f);
            leafArt.hideFlags = HideFlags.HideAndDontSave;
            return leafArt;
        }

        static Sprite Ornament()
        {
            if (ornament != null) return ornament;

            const int n = 64;
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
            tex.hideFlags = HideFlags.HideAndDontSave;
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;
            var clear = new Color(1f, 1f, 1f, 0f);
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                    tex.SetPixel(x, y, clear);

            for (int i = 0; i < 46; i++)
            {
                float t = i / 45f;
                int x = Mathf.RoundToInt(Mathf.Lerp(8f, 48f, t * t));
                int y = Mathf.RoundToInt(Mathf.Lerp(56f, 14f, t));
                Stamp(tex, x, y, 2, 1f);
            }

            Blob(tex, 16, 48, 7, 4);
            Blob(tex, 30, 34, 6, 3);
            Blob(tex, 20, 22, 5, 3);
            tex.Apply();

            ornament = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0f, 1f), 64f);
            ornament.hideFlags = HideFlags.HideAndDontSave;
            return ornament;
        }

        static void Stamp(Texture2D tex, int cx, int cy, int radius, float alpha)
        {
            for (int y = -radius; y <= radius; y++)
                for (int x = -radius; x <= radius; x++)
                {
                    float d = Mathf.Sqrt(x * x + y * y);
                    if (d > radius) continue;
                    Plot(tex, cx + x, cy + y, alpha * (1f - d / (radius + 0.5f)));
                }
        }

        static void Blob(Texture2D tex, int cx, int cy, int rx, int ry)
        {
            for (int y = -ry; y <= ry; y++)
                for (int x = -rx; x <= rx; x++)
                {
                    float nx = x / (float)rx;
                    float ny = y / (float)ry;
                    float d = nx * nx + ny * ny;
                    if (d > 1f) continue;
                    Plot(tex, cx + x, cy + y, 1f - d);
                }
        }

        static void Plot(Texture2D tex, int x, int y, float alpha)
        {
            if (x < 0 || y < 0 || x >= tex.width || y >= tex.height) return;
            var color = tex.GetPixel(x, y);
            color.r = 1f;
            color.g = 1f;
            color.b = 1f;
            color.a = Mathf.Max(color.a, Mathf.Clamp01(alpha));
            tex.SetPixel(x, y, color);
        }

        static AudioSource clic;
        static AudioClip clicClip;

        /// <summary>
        /// One dry click. Mouse uses MenuButtonMotion. Keyboard confirms call this
        /// directly, so a mouse click does not play twice.
        /// </summary>
        public static void TocarBoton()
        {
            var src = Clic();
            if (src == null) return;
            src.pitch = UnityEngine.Random.Range(0.96f, 1.05f);
            src.PlayOneShot(clicClip, 0.55f);
        }

        static AudioSource Clic()
        {
            if (clic != null) return clic;
            var go = new GameObject("ClicDeBoton");
            go.hideFlags = HideFlags.HideAndDontSave;
            UnityEngine.Object.DontDestroyOnLoad(go);
            clic = go.AddComponent<AudioSource>();
            clic.playOnAwake = false;
            clic.loop = false;
            clic.spatialBlend = 0f;
            clicClip = HacerClic();
            clic.clip = clicClip;
            return clic;
        }

        static AudioClip HacerClic()
        {
            const int rate = 44100;
            const int n = 2800;
            var data = new float[n];
            var rng = new System.Random(17);
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)rate;
                float env = Mathf.Exp(-t * 38f);
                float body = Mathf.Sin(t * 640f * Mathf.PI * 2f);
                float tick = Mathf.Sin(t * 1840f * Mathf.PI * 2f);
                float grit = (float)(rng.NextDouble() * 2.0 - 1.0);
                float noise = t < 0.012f ? grit : grit * 0.15f;
                data[i] = (body * 0.45f + tick * 0.22f + noise * 0.18f) * env;
            }

            var clip = AudioClip.Create("clic-boton", n, 1, rate, false);
            clip.SetData(data, 0);
            clip.hideFlags = HideFlags.HideAndDontSave;
            return clip;
        }
    }
}
