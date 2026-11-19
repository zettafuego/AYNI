using AYNI.UI;
using UnityEngine;
using UnityEngine.UI;

namespace AYNI.Battle
{
    /// <summary>
    /// The handheld command window stays off. It drew a dark box over the fight.
    /// The blows stay on the number keys when a foe is close.
    /// </summary>
    public class PeleaComandos : MonoBehaviour
    {
        public const int OptionCount = 4;
        const float Near = 14f;

        static readonly Color Frame = new Color(0.04f, 0.04f, 0.05f, 0.96f);
        static readonly Color Paper = new Color(0.08f, 0.08f, 0.09f, 0.96f);

        static readonly string[] Moves = { "PATADA", "EMBESTIDA", "CURAR", "OFRENDA", "COLETAZO" };
        static readonly string[] Blurbs =
        {
            "Patada. De cerca.",
            "Embestida. Alcanza más.",
            "Curar. El cuerpo vuelve.",
            "Ofrenda. Das un poco, y pegas menos.",
            "Coletazo. La cola alcanza.",
        };

        int MoveCount => FighterRoster.Current.Sheet == BattleCast.Vizcacha ? Moves.Length : Moves.Length - 1;

        LagunaBattleManager battle;
        VizcachaFighter player;
        GameObject root;
        Text message;
        Text cursor;
        GameObject mainPage;
        GameObject movePage;
        GameObject bagPage;
        GameObject ayniPage;
        Button[] mainButtons;
        Button[] moveButtons;
        Button[] bagButtons;
        Button[] ayniButtons;
        int page;
        int index;
        bool focused;
        bool holdLine;
        GameObject reservas;
        Button[] reservaBotones;
        Text[] reservaTexto;

        public static PeleaComandos Ensure(LagunaBattleManager battle)
        {
            if (battle == null || battle.startScreen == null) return null;
            var canvas = battle.startScreen.transform.parent;
            if (canvas == null) return null;

            var old = canvas.Find("Pelea");
            if (old != null) Object.Destroy(old.gameObject);

            var commands = canvas.GetComponent<PeleaComandos>();
            if (commands == null) commands = canvas.gameObject.AddComponent<PeleaComandos>();
            commands.battle = battle;
            commands.focused = false;
            battle.SetCommandsOpen(false);
            return commands;
        }

        void Build(Transform canvas)
        {
            if (root != null) return;

            var plate = Storybook.Plate(canvas, "Pelea", Vector2.zero, Vector2.one, new Color(0f, 0f, 0f, 0f));
            root = plate.gameObject;

            FramePair(root.transform, "Mensaje", new Vector2(0.02f, 0.02f), new Vector2(0.63f, 0.30f));
            message = AddText(root.transform.Find("MensajePapel"), "Linea",
                new Vector2(0.06f, 0.1f), new Vector2(0.94f, 0.9f), 20, Color.white);

            FramePair(root.transform, "Orden", new Vector2(0.648f, 0.02f), new Vector2(0.98f, 0.30f));
            var paper = root.transform.Find("OrdenPapel");

            mainPage = Page(paper, "Principal");
            mainButtons = new[]
            {
                Option(mainPage.transform, "Luchar", "LUCHAR", new Vector2(0.06f, 0.52f), new Vector2(0.48f, 0.92f), () => Choose(0, 0)),
                Option(mainPage.transform, "Mochila", "MOCHILA", new Vector2(0.52f, 0.52f), new Vector2(0.94f, 0.92f), () => Choose(0, 1)),
                Option(mainPage.transform, "Ayni", "AYNI", new Vector2(0.06f, 0.06f), new Vector2(0.48f, 0.46f), () => Choose(0, 2)),
                Option(mainPage.transform, "Huir", "HUIR", new Vector2(0.52f, 0.06f), new Vector2(0.94f, 0.46f), () => Choose(0, 3)),
            };

            movePage = Page(paper, "Golpes");
            moveButtons = new Button[Moves.Length];
            for (int i = 0; i < Moves.Length; i++)
            {
                int picked = i;
                moveButtons[i] = Option(movePage.transform, "Golpe" + i, Moves[i],
                    new Vector2(0.06f, 0.06f), new Vector2(0.94f, 0.22f),
                    () => Choose(1, picked));
            }
            FitMoves();

            bagPage = Page(paper, "Bolsa");
            bagButtons = new[]
            {
                Option(bagPage.transform, "Curar", "CURAR", new Vector2(0.08f, 0.52f), new Vector2(0.92f, 0.92f), () => Choose(2, 0)),
                Option(bagPage.transform, "Volver", "VOLVER", new Vector2(0.08f, 0.08f), new Vector2(0.92f, 0.46f), () => Choose(2, 1)),
            };

            ayniPage = Page(paper, "Ley");
            ayniButtons = new[]
            {
                Option(ayniPage.transform, "Volver", "VOLVER", new Vector2(0.12f, 0.28f), new Vector2(0.88f, 0.72f), () => Choose(3, 0)),
            };

            cursor = AddText(root.transform, "Cursor", Vector2.zero, Vector2.one, 18, Color.white);
            cursor.text = ">";
            cursor.alignment = TextAnchor.MiddleLeft;
            root.SetActive(false);
        }

        void Update()
        {
            if (root != null)
            {
                Destroy(root);
                root = null;
            }

            focused = false;
            battle?.SetCommandsOpen(false);
            if (player == null) player = FindFirstObjectByType<VizcachaFighter>();
            PintarReservas();
            if (!EnPelea()) return;

            for (int i = 0; i < MoveCount; i++)
            {
                if (!Digit(i)) continue;
                if ((i <= 1 || i == 4) && !CanShow()) return;
                player.CastGolpe(i);
                return;
            }
        }

        bool EnPelea()
        {
            if (battle == null || !battle.IsPlaying || battle.IsGameOver) return false;
            if (CaminoRelato.Reading) return false;
            if (CajamarcaCalles.IsOpen || BosqueCapitulo.IsOpen || CajamarcaRealCapitulo.IsOpen) return false;
            return player != null && player.IsAlive;
        }

        void AsegurarReservas()
        {
            if (reservas != null || battle == null || battle.startScreen == null) return;
            var canvas = battle.startScreen.transform.parent;
            if (canvas == null) return;

            var viejo = canvas.Find("Reservas");
            if (viejo != null) Destroy(viejo.gameObject);

            var placa = Storybook.Plate(canvas, "Reservas", Vector2.zero, Vector2.one, new Color(0f, 0f, 0f, 0f));
            reservas = placa.gameObject;
            reservaBotones = new Button[4];
            reservaTexto = new Text[4];
            string[] nombres = { "Vida", "Ayni", "Reputacion", "Codicia" };
            for (int i = 0; i < 4; i++)
            {
                float x0 = 0.02f + i * 0.245f;
                int cual = i;
                var boton = Storybook.StickerButton(
                    reservas.transform,
                    nombres[i],
                    nombres[i],
                    new Vector2(x0, 0.068f),
                    new Vector2(x0 + 0.23f, 0.145f),
                    () => UsarReserva(cual));
                reservaBotones[i] = boton;
                reservaTexto[i] = boton.GetComponentInChildren<Text>();
            }

            reservas.transform.SetAsLastSibling();
            reservas.SetActive(false);
        }

        void PintarReservas()
        {
            AsegurarReservas();
            if (reservas == null) return;
            bool mostrar = EnPelea();
            if (reservas.activeSelf != mostrar) reservas.SetActive(mostrar);
            if (!mostrar) return;

            int[] cuenta =
            {
                ReservaDelCamino.Vida,
                ReservaDelCamino.Ayni,
                ReservaDelCamino.Reputacion,
                ReservaDelCamino.Codicia,
            };
            string[] etiquetas = { "VIDA ", "AYNI ", "REPUTACIÓN ", "CODICIA " };
            for (int i = 0; i < 4; i++)
            {
                if (reservaTexto[i] != null)
                {
                    reservaTexto[i].text = etiquetas[i] + cuenta[i];
                    var tinta = reservaTexto[i].color;
                    tinta.a = cuenta[i] > 0 ? 1f : 0.4f;
                    reservaTexto[i].color = tinta;
                }

                if (reservaBotones[i] != null)
                    reservaBotones[i].interactable = cuenta[i] > 0;
            }
        }

        void UsarReserva(int cual)
        {
            if (player == null || !EnPelea()) return;
            if (cual == 0) player.CastGolpe(2);
            else if (cual == 1) player.CastGolpe(3);
            else if (cual == 2) player.CastReputacion();
            else player.CastCodicia();
        }

        bool CanShow()
        {
            if (!EnPelea()) return false;
            return Nearest(out _, out float dist) && dist < Near;
        }

        bool Nearest(out float foeX, out float dist)
        {
            foeX = 0f;
            dist = 999f;
            bool found = false;
            var toro = ToroEnemyAI.Cercano(player.PositionX);
            if (toro != null)
            {
                foeX = toro.PositionX;
                dist = Mathf.Abs(player.PositionX - foeX);
                found = true;
            }

            var sombra = FindFirstObjectByType<SombraDelCerro>();
            if (sombra != null && sombra.IsAlive)
            {
                float next = Mathf.Abs(player.PositionX - sombra.PositionX);
                if (!found || next < dist)
                {
                    foeX = sombra.PositionX;
                    dist = next;
                    found = true;
                }
            }

            return found;
        }

        void ReadIdle()
        {
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            {
                focused = true;
                Open(0);
                return;
            }

            for (int i = 0; i < MoveCount; i++)
            {
                if (!Digit(i)) continue;
                Cast(i);
                return;
            }
        }

        void ReadMenu()
        {
            var keys = ButtonsFor(page);
            int columns = page == 0 ? 2 : 1;
            int rows = Mathf.Max(1, keys.Length / columns);

            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Backspace))
            {
                if (page == 0) focused = false;
                else Open(0);
                return;
            }

            int previous = index;
            if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W))
                index = Step(index, 0, -1, columns, rows, keys.Length);
            else if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S))
                index = Step(index, 0, 1, columns, rows, keys.Length);
            else if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A))
                index = Step(index, -1, 0, columns, rows, keys.Length);
            else if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D))
                index = Step(index, 1, 0, columns, rows, keys.Length);
            if (index != previous) holdLine = false;
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetKeyDown(KeyCode.Space))
                Confirm();

            for (int i = 0; i < MoveCount; i++)
            {
                if (!Digit(i)) continue;
                Cast(i);
                return;
            }
        }

        static int Step(int current, int dx, int dy, int columns, int rows, int length)
        {
            int col = columns <= 1 ? 0 : current % columns;
            int row = columns <= 1 ? current : current / columns;
            col = Mathf.Clamp(col + dx, 0, Mathf.Max(0, columns - 1));
            row = Mathf.Clamp(row + dy, 0, Mathf.Max(0, rows - 1));
            int next = columns <= 1 ? row : row * columns + col;
            return Mathf.Clamp(next, 0, Mathf.Max(0, length - 1));
        }

        static bool Digit(int index)
        {
            if (index == 0) return Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1);
            if (index == 1) return Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2);
            if (index == 2) return Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Keypad3);
            if (index == 3) return Input.GetKeyDown(KeyCode.Alpha4) || Input.GetKeyDown(KeyCode.Keypad4);
            if (index == 4) return Input.GetKeyDown(KeyCode.Alpha5) || Input.GetKeyDown(KeyCode.Keypad5);
            return false;
        }

        void Choose(int pickedPage, int pickedIndex)
        {
            if (page != pickedPage) Open(pickedPage);
            index = pickedIndex;
            focused = true;
            Confirm();
        }

        void Confirm()
        {
            if (page == 0)
            {
                if (index == 0) Open(1);
                else if (index == 1) Open(2);
                else if (index == 2) Open(3);
                else Flee();
                return;
            }

            if (page == 1)
            {
                Cast(index);
                return;
            }

            if (page == 2 && index == 0)
            {
                Cast(2);
                return;
            }

            Open(0);
        }

        void Cast(int move)
        {
            if (player == null) return;
            bool landed = player.CastGolpe(move);
            if (!landed)
            {
                holdLine = true;
                if (message != null) message.text = "Todavía no. Falta aire.";
                return;
            }

            focused = false;
            Open(0);
            holdLine = false;
            if (message != null) message.text = Blurbs[Mathf.Clamp(move, 0, Blurbs.Length - 1)];
        }

        void Flee()
        {
            player?.StepBack();
            focused = false;
            Open(0);
            holdLine = false;
            if (message != null) message.text = "El cerro no suelta el paso.";
        }

        void Open(int next)
        {
            page = next;
            index = 0;
            holdLine = false;
            FitMoves();
            if (mainPage != null) mainPage.SetActive(page == 0);
            if (movePage != null) movePage.SetActive(page == 1);
            if (bagPage != null) bagPage.SetActive(page == 2);
            if (ayniPage != null) ayniPage.SetActive(page == 3);
            if (message == null) return;

            string who = FighterRoster.Current.Sheet.Name;
            if (page == 0)
            {
                string tail = MoveCount > 4 ? "\n5 coletazo" : "";
                message.text = "¿Qué hará " + who + "?\nENTER elige\n1 patada   2 embestida\n3 curar   4 ofrenda" + tail;
            }
            else if (page == 1)
                message.text = Blurbs[0];
            else if (page == 2)
                message.text = "No hay oro en la bolsa.\nCurar sigue aquí.";
            else
                message.text = "Ayni es lo que das.\nReputación vuelve.\nCodicia se queda si el oro gana.";
        }

        void Paint()
        {
            var keys = ButtonsFor(page);
            for (int i = 0; i < keys.Length; i++)
            {
                var image = keys[i] != null ? keys[i].GetComponent<Image>() : null;
                if (image != null)
                    image.color = i == index ? new Color(1.12f, 1.08f, 0.74f, 1f) : Color.white;
                var motion = keys[i] != null ? keys[i].GetComponent<MenuButtonMotion>() : null;
                if (motion != null) motion.Chosen = focused && i == index;
            }

            if (page == 1 && !holdLine && message != null && index >= 0 && index < Blurbs.Length)
                message.text = Blurbs[index];
        }

        void PlaceCursor()
        {
            var keys = ButtonsFor(page);
            if (cursor == null || keys.Length == 0) return;
            int slot = Mathf.Clamp(index, 0, keys.Length - 1);
            var button = keys[slot];
            if (button == null) return;

            cursor.transform.SetParent(button.transform, false);
            var rect = cursor.rectTransform;
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 0.5f);
            rect.sizeDelta = new Vector2(28f, 0f);
            rect.anchoredPosition = new Vector2(6f, 0f);
            float blink = Mathf.Abs(Mathf.Sin(Time.unscaledTime * 5.5f));
            cursor.color = new Color(1f, 1f, 1f, 0.35f + 0.65f * blink);
        }

        void FitMoves()
        {
            if (moveButtons == null) return;
            int count = MoveCount;
            float step = 0.92f / count;
            for (int i = 0; i < moveButtons.Length; i++)
            {
                if (moveButtons[i] == null) continue;
                bool show = i < count;
                moveButtons[i].gameObject.SetActive(show);
                if (!show) continue;
                var rect = moveButtons[i].GetComponent<RectTransform>();
                float top = 0.96f - i * step;
                rect.anchorMin = new Vector2(0.06f, top - (step - 0.02f));
                rect.anchorMax = new Vector2(0.94f, top);
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
            }
            if (index >= count) index = Mathf.Max(0, count - 1);
        }

        Button[] ButtonsFor(int which)
        {
            if (which == 1)
            {
                int count = Mathf.Min(MoveCount, moveButtons.Length);
                var shown = new Button[count];
                for (int i = 0; i < count; i++) shown[i] = moveButtons[i];
                return shown;
            }
            if (which == 2) return bagButtons;
            if (which == 3) return ayniButtons;
            return mainButtons;
        }

        static void FramePair(Transform parent, string name, Vector2 min, Vector2 max)
        {
            Storybook.Plate(parent, name + "Marco", min, max, Frame);
            var pad = 0.008f;
            Storybook.Plate(parent, name + "Papel",
                min + new Vector2(pad, pad),
                max - new Vector2(pad, pad),
                Paper);
        }

        static GameObject Page(Transform parent, string name)
        {
            var plate = Storybook.Plate(parent, name, Vector2.zero, Vector2.one, new Color(0f, 0f, 0f, 0f));
            return plate.gameObject;
        }

        static Button Option(Transform parent, string name, string label, Vector2 min, Vector2 max, UnityEngine.Events.UnityAction click)
        {
            var face = Storybook.Plate(parent, name, min, max, Color.white);
            face.raycastTarget = true;
            Storybook.WearLeaf(face, false);
            face.color = new Color(0.16f, 0.16f, 0.18f, 1f);
            var button = face.gameObject.AddComponent<Button>();
            button.transition = Selectable.Transition.None;
            var nav = button.navigation;
            nav.mode = Navigation.Mode.None;
            button.navigation = nav;

            var text = AddText(face.transform, "Label", Vector2.zero, Vector2.one, 20, Storybook.LeafInk);
            text.text = label;
            Storybook.LeafRead(text, 48);
            var pad = text.rectTransform;
            pad.offsetMin = new Vector2(28f, 2f);
            pad.offsetMax = new Vector2(-6f, -2f);

            if (click != null) button.onClick.AddListener(click);
            var motion = face.gameObject.AddComponent<MenuButtonMotion>();
            motion.Subtle = true;
            return button;
        }

        static Text AddText(Transform parent, string name, Vector2 min, Vector2 max, int size, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            var text = go.AddComponent<Text>();
            Storybook.DressText(text, string.Empty, Storybook.DisplayFont, size, color, FontStyle.Bold);
            text.alignment = TextAnchor.MiddleCenter;
            return text;
        }
    }

    /// <summary>
    /// Cargas que se juntan jugando. En cero el botón no hace nada.
    /// </summary>
    public static class ReservaDelCamino
    {
        public const int Tope = 9;

        public static int Vida { get; private set; }
        public static int Ayni { get; private set; }
        public static int Reputacion { get; private set; }
        public static int Codicia { get; private set; }

        static bool promesaPagada;

        public static void SumarVida(int n = 1) => Vida = Subir(Vida, n);
        public static void SumarAyni(int n = 1) => Ayni = Subir(Ayni, n);
        public static void SumarReputacion(int n = 1) => Reputacion = Subir(Reputacion, n);
        public static void SumarCodicia(int n = 1) => Codicia = Subir(Codicia, n);

        public static bool GastarVida() => Bajar(Vida, valor => Vida = valor);
        public static bool GastarAyni() => Bajar(Ayni, valor => Ayni = valor);
        public static bool GastarReputacion() => Bajar(Reputacion, valor => Reputacion = valor);
        public static bool GastarCodicia() => Bajar(Codicia, valor => Codicia = valor);

        public static bool PagarPromesa()
        {
            if (promesaPagada) return false;
            promesaPagada = true;
            SumarVida();
            SumarAyni();
            SumarReputacion();
            return true;
        }

        static int Subir(int actual, int n)
        {
            if (n <= 0) return actual;
            return Mathf.Min(Tope, actual + n);
        }

        static bool Bajar(int actual, System.Action<int> escribir)
        {
            if (actual <= 0) return false;
            escribir(actual - 1);
            return true;
        }
    }

    /// <summary>
    /// Lo que ya hiciste en esta partida. Sobrevive a salir de un capítulo
    /// y a recargar la escena. Se borra al cerrar Play.
    /// </summary>
    public static class PartidaDelCamino
    {
        public enum Oro
        {
            Ninguno,
            Guardado,
            Soltado,
        }

        public static float Ayni { get; private set; }
        public static float Reputacion { get; private set; }
        public static float Codicia { get; private set; }
        public static bool TieneCifras { get; private set; }
        public static bool PromesaHecha { get; private set; }
        public static bool RioCruzado { get; private set; }
        public static Oro EleccionOro { get; private set; }

        public static void AnotarCifras(float ayni, float reputacion, float codicia)
        {
            Ayni = Mathf.Clamp(ayni, 0f, 100f);
            Reputacion = Mathf.Clamp(reputacion, 0f, 100f);
            Codicia = Mathf.Clamp(codicia, 0f, 100f);
            TieneCifras = true;
        }

        public static void CumplirPromesa() => PromesaHecha = true;

        public static void CruzarRio() => RioCruzado = true;

        public static void GuardarOro()
        {
            if (EleccionOro == Oro.Soltado) return;
            EleccionOro = Oro.Guardado;
        }

        public static void SoltarOro() => EleccionOro = Oro.Soltado;
    }
}
