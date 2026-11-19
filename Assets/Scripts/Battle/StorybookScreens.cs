using System;
using AYNI.Narrative;
using AYNI.UI;
using UnityEngine;
using UnityEngine.UI;

namespace AYNI.Battle
{
    /// <summary>
    /// Dresses the laguna canvas that already exists. Object names stay, so AutoFindUI keeps working.
    /// </summary>
    public class StorybookScreens : MonoBehaviour
    {
        GameObject banner;
        Text bannerText;
        int showing = -1;
        int pending = -1;
        int menuIndex;
        Button[] menuButtons;
        Button[] portadaButtons;
        Button[] historiaButtons;
        Text menuCursor;
        Text storyHint;
        Text musicaLabel;
        Text pantallaLabel;
        GameObject paginaBotones;
        GameObject paginaAjustes;
        GameObject paginaCreditos;
        GameObject paginaPersonaje;
        System.Action alElegirPersonaje;
        Hoja hoja = Hoja.Portada;
        LagunaBattleManager battle;
        CaminoRelato relato;

        enum Hoja { Portada, Historias, Personaje, Botones, Ajustes, Creditos }

        public bool IsShowing => banner != null && banner.activeSelf;
        public bool HoldsTheGate => relato != null && relato.HoldsTheGate;
        public bool AteThisFrame => relato != null && relato.AteThisFrame;

        public static StorybookScreens Dress(LagunaBattleManager battle)
        {
            if (battle == null || battle.startScreen == null) return null;

            var canvas = battle.startScreen.transform.parent;
            if (canvas == null) return null;

            var screens = canvas.GetComponent<StorybookScreens>();
            if (screens == null) screens = canvas.gameObject.AddComponent<StorybookScreens>();
            screens.Apply(battle, canvas);
            return screens;
        }

        public void ShowChapter(int index)
        {
            if (index < 0 || index >= LeyDelMonte.Chapters.Length) return;
            if (banner == null) return;
            if (banner.activeSelf)
            {
                if (index != showing) pending = index;
                return;
            }

            Present(index);
        }

        public void HideChapter()
        {
            pending = -1;
            CancelInvoke(nameof(HideBanner));
            HideBanner();
        }

        public void ShowRoad(string kicker, string line, string speaker, float seconds = 3.6f)
        {
            if (banner == null || bannerText == null) return;
            pending = -1;
            showing = -2;
            bannerText.text = kicker + "\n\n«" + line + "»\n— " + speaker;
            banner.SetActive(true);
            banner.transform.SetAsLastSibling();
            CancelInvoke(nameof(HideBanner));
            Invoke(nameof(HideBanner), seconds);
        }

        void Apply(LagunaBattleManager battle, Transform canvas)
        {
            this.battle = battle;
            DressPage(battle.startScreen, Storybook.Jungle, true);
            DressPage(battle.gameOverScreen, Storybook.JungleDeep, false);
            DressScores(canvas);
            DressHealth(battle);
            ShowLives(false);
            ShowScores(false);
            DressInstructions(battle.instructionsText);
            EnsureBanner(canvas);
            EnsureFinale(battle.gameOverScreen);
            relato = CaminoRelato.Ensure(canvas, battle);
            MostrarPortada();
        }

        public void ShowLives(bool on)
        {
            if (battle == null || battle.startScreen == null) return;
            var canvas = battle.startScreen.transform.parent;
            if (canvas == null) return;
            var player = canvas.Find("PlayerHealth_Panel");
            var enemy = canvas.Find("EnemyHealth_Panel");
            if (player != null) player.gameObject.SetActive(on);
            if (enemy != null) enemy.gameObject.SetActive(on);
        }

        public void ShowScores(bool on)
        {
            if (battle == null || battle.startScreen == null) return;
            var canvas = battle.startScreen.transform.parent;
            if (canvas == null) return;
            string[] names = { "AYNI", "Espiritualidad", "Respeto", "Dinero" };
            for (int i = 0; i < names.Length; i++)
            {
                var bar = canvas.Find(names[i]);
                if (bar != null) bar.gameObject.SetActive(on);
            }
        }

        public void OfferFinale(bool playerWon)
        {
            var page = battle != null ? battle.gameOverScreen : null;
            if (page == null) return;
            SetOn(page.transform, "ElFinal", playerWon);
            SetOn(page.transform, "ElFinalShadow", playerWon);
        }

        static void SetOn(Transform page, string childName, bool on)
        {
            var child = page.Find(childName);
            if (child != null) child.gameObject.SetActive(on);
        }

        void EnsureFinale(GameObject page)
        {
            if (page == null || page.transform.Find("ElFinal") != null) return;
            var button = Storybook.StickerButton(page.transform, "ElFinal", "EL FINAL",
                new Vector2(0.28f, 0.22f), new Vector2(0.72f, 0.36f),
                () => relato?.Open(LeyDelMonte.Ending, false));
            button.gameObject.SetActive(false);
            var shadow = page.transform.Find("ElFinalShadow");
            if (shadow != null) shadow.gameObject.SetActive(false);
        }

        void DressPage(GameObject page, Color wash, bool isOpening)
        {
            if (page == null) return;

            var image = page.GetComponent<Image>();
            Storybook.Paint(image, isOpening ? "Battle/Pantallas/Titulo" : "Battle/Pantallas/LagunaNoche");
            if (image != null && image.sprite == null)
                image.color = isOpening ? Storybook.Sky : wash;

            Hide(page.transform, "Pergamino");
            Hide(page.transform, "RuleTop");
            Hide(page.transform, "VineCorners");

            if (isOpening)
                DressOpening(page.transform);
            else
                DressEnding(page.transform);
        }

        static void Hide(Transform page, string childName)
        {
            var child = page.Find(childName);
            if (child != null) child.gameObject.SetActive(false);
        }

        void DressOpening(Transform page)
        {
            var title = page.Find("Title")?.GetComponent<Text>();
            Storybook.DressText(title, string.Empty, Storybook.DisplayFont, 8, Storybook.InkLine, FontStyle.Bold);

            var wash = page.Find("Cine");
            if (wash == null)
            {
                wash = Storybook.Plate(page, "Cine", Vector2.zero, new Vector2(0.42f, 1f),
                    new Color(0f, 0f, 0f, 0f)).transform;
            }
            else
            {
                Storybook.Anchors(wash, Vector2.zero, new Vector2(0.42f, 1f));
                var washImage = wash.GetComponent<Image>();
                if (washImage != null) washImage.color = new Color(0f, 0f, 0f, 0f);
            }

            var menuPanelMin = new Vector2(0.012f, 0.18f);
            var menuPanelMax = new Vector2(0.30f, 0.78f);
            var menuPanel = page.Find("MenuPanel");
            if (menuPanel == null)
            {
                Storybook.Plate(page, "MenuPanel", menuPanelMin, menuPanelMax,
                    new Color(0f, 0f, 0f, 0f));
            }
            else
            {
                Storybook.Anchors(menuPanel, menuPanelMin, menuPanelMax);
                var panelImage = menuPanel.GetComponent<Image>();
                if (panelImage != null) panelImage.color = new Color(0f, 0f, 0f, 0f);
            }

            var card = page.Find("CastCard");
            if (card != null) card.gameObject.SetActive(false);
            var cardShadow = page.Find("CastCardShadow");
            if (cardShadow != null) cardShadow.gameObject.SetActive(false);

            var story = page.Find("Story")?.GetComponent<Text>();
            if (story == null)
            {
                var go = new GameObject("Story");
                go.transform.SetParent(page, false);
                go.AddComponent<RectTransform>();
                story = go.AddComponent<Text>();
                story.raycastTarget = false;
            }

            Storybook.Anchors(story, new Vector2(0.016f, 0.090f), new Vector2(0.46f, 0.152f));
            Storybook.DressText(story, LeyDelMonte.PortadaHint,
                Storybook.LabelFont, 17, new Color(0.97f, 0.93f, 0.81f, 0.95f), FontStyle.Bold);
            story.alignment = TextAnchor.MiddleLeft;
            story.horizontalOverflow = HorizontalWrapMode.Overflow;
            story.verticalOverflow = VerticalWrapMode.Truncate;
            Storybook.Stamp(story, 1.3f);
            storyHint = story;

            var controls = page.Find("Controls")?.GetComponent<Text>();
            Storybook.DressText(controls, string.Empty, Storybook.BodyFont, 8, Storybook.InkLine, FontStyle.Bold);

            Fila(0, out var seguirMin, out var seguirMax);
            Fila(1, out var historiaMin, out var historiaMax);
            Fila(2, out var callesMin, out var callesMax);
            Fila(3, out var bosqueMin, out var bosqueMax);
            Fila(4, out var valleMin, out var valleMax);
            Fila(5, out var minaMin, out var minaMax);
            Fila(6, out var monteMin, out var monteMax);
            Fila(7, out var wordMin, out var wordMax);
            Fila(8, out var teclaMin, out var teclaMax);

            if (page.Find("ComenzarShadow") == null && page.Find("Comenzar") != null)
            {
                var made = Storybook.Plate(page, "ComenzarShadow",
                    monteMin + new Vector2(0.008f, -0.014f),
                    monteMax + new Vector2(0.008f, -0.014f),
                    Storybook.InkLine);
                var begin = page.Find("Comenzar");
                if (begin != null) made.transform.SetSiblingIndex(begin.GetSiblingIndex());
            }

            if (page.Find("Seguir") == null)
            {
                Storybook.StickerButton(page, "Seguir", LeyDelMonte.SeguirLabel, seguirMin, seguirMax,
                    () => EmpezarCamino());
            }

            if (page.Find("Historia") == null)
            {
                Storybook.StickerButton(page, "Historia", "LA HISTORIA", historiaMin, historiaMax,
                    () => relato?.Open(LeyDelMonte.Road, true));
            }

            if (page.Find("Calles") == null)
            {
                Storybook.StickerButton(page, "Calles", LeyDelMonte.StreetsLabel, callesMin, callesMax,
                    () => CajamarcaCalles.Begin(battle));
            }

            if (page.Find("Valle") == null)
            {
                Storybook.StickerButton(page, "Valle", LeyDelMonte.CajamarcaRealLabel, valleMin, valleMax,
                    () => CajamarcaRealCapitulo.Begin(battle));
            }

            if (page.Find("Bosque") == null)
            {
                Storybook.StickerButton(page, "Bosque", LeyDelMonte.ForestLabel, bosqueMin, bosqueMax,
                    () => BosqueCapitulo.Begin(battle));
            }

            if (page.Find("Palabras") == null)
            {
                Storybook.StickerButton(page, "Palabras", "PALABRAS", wordMin, wordMax,
                    () => PalabrasCartilla.Open(page));
            }

            if (page.Find("Mina") == null)
            {
                Storybook.StickerButton(page, "Mina", "II  ·  LA MINA", minaMin, minaMax,
                    () => MontanaDelPerol.Begin(battle));
            }

            var minaYa = page.Find("Mina")?.GetComponent<Button>();
            if (minaYa != null)
            {
                minaYa.onClick.RemoveAllListeners();
                minaYa.onClick.AddListener(() => MontanaDelPerol.Begin(battle));
            }

            if (page.Find("Teclado") == null)
            {
                Storybook.StickerButton(page, "Teclado", "EL TECLADO", teclaMin, teclaMax,
                    () => relato?.OpenKeys());
            }

            Place(page, "Seguir", seguirMin, seguirMax);
            Place(page, "Historia", historiaMin, historiaMax);
            Place(page, "Calles", callesMin, callesMax);
            Place(page, "Bosque", bosqueMin, bosqueMax);
            Place(page, "Valle", valleMin, valleMax);
            Place(page, "Mina", minaMin, minaMax);
            Place(page, "Comenzar", monteMin, monteMax);
            Place(page, "Palabras", wordMin, wordMax);
            Place(page, "Teclado", teclaMin, teclaMax);

            Storybook.WearLeaf(page.Find("ComenzarShadow")?.GetComponent<Image>(), true);
            NameButton(page, "Seguir", SeguirElCamino.Rotulo());
            NameButton(page, "Historia", "LA HISTORIA");
            NameButton(page, "Calles", "I  ·  SIETE NOCHES");
            NameButton(page, "Bosque", "I  ·  LA MENTIRA");
            NameButton(page, "Valle", "I  ·  SANTA APOLONIA");
            NameButton(page, "Mina", "II  ·  LA MINA");
            NameButton(page, "Comenzar", "III  ·  LA MONTAÑA");
            NameButton(page, "Palabras", "PALABRAS");
            NameButton(page, "Teclado", "EL TECLADO");

            EnsureCursor(page);
            BindMenu(page);
            DressRoster(page);
            PaintMenu();

            var cine = page.Find("Cine");
            if (cine != null) cine.SetAsFirstSibling();
            var panelBack = page.Find("MenuPanel");
            if (panelBack != null) panelBack.SetSiblingIndex(cine != null ? 1 : 0);
            if (story != null) story.transform.SetAsLastSibling();
            Lift(page, "Seguir");
            Lift(page, "Historia");
            Lift(page, "Calles");
            Lift(page, "Bosque");
            Lift(page, "Valle");
            Lift(page, "Mina");
            Lift(page, "Comenzar");
            Lift(page, "Palabras");
            Lift(page, "Teclado");
            var roster = page.Find("Roster");
            if (roster != null) roster.SetAsLastSibling();
            var pick = page.Find("Elige");
            if (pick != null) pick.SetAsLastSibling();

            RebindHistorias(page);
            ArmarPortada(page);
            ArmarPaginas(page);
            MostrarPortada();
        }

        void RebindHistorias(Transform page)
        {
            Rebind(page, "Seguir", EmpezarCamino);
            Rebind(page, "Historia", () => relato?.Open(LeyDelMonte.Road, true));
            Rebind(page, "Calles", () => { SeguirElCamino.Soltar(); PedirPersonaje(() => CajamarcaCalles.Begin(battle)); });
            Rebind(page, "Bosque", () => { SeguirElCamino.Soltar(); PedirPersonaje(() => BosqueCapitulo.Begin(battle)); });
            Rebind(page, "Valle", () => { SeguirElCamino.Soltar(); PedirPersonaje(() => CajamarcaRealCapitulo.Begin(battle)); });
            Rebind(page, "Mina", () => { SeguirElCamino.Soltar(); PedirPersonaje(() => MontanaDelPerol.Begin(battle)); });
            Rebind(page, "Comenzar", () => { SeguirElCamino.Soltar(); PedirPersonaje(() => MontanaDelPerol.Begin(battle)); });
            Rebind(page, "Palabras", () => PalabrasCartilla.Open(page));
            Rebind(page, "Teclado", () => relato?.OpenKeys());
        }

        static void Rebind(Transform page, string name, UnityEngine.Events.UnityAction action)
        {
            var button = page.Find(name)?.GetComponent<Button>();
            if (button == null || action == null) return;
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(action);
        }

        void ArmarPortada(Transform page)
        {
            string[] nombres = { "PortadaHistorias", "PortadaBotones", "PortadaAjustes", "PortadaCreditos" };
            string[] letras =
            {
                LeyDelMonte.IniciarHistorias,
                LeyDelMonte.BotonesTitulo,
                LeyDelMonte.AjustesTitulo,
                LeyDelMonte.CreditosTitulo,
            };
            UnityEngine.Events.UnityAction[] actos =
            {
                AbrirHistorias,
                AbrirBotones,
                AbrirAjustes,
                AbrirCreditos,
            };
            portadaButtons = new Button[nombres.Length];
            for (int i = 0; i < nombres.Length; i++)
            {
                FilaGrande(i, out var min, out var max);
                if (page.Find(nombres[i]) == null)
                    Storybook.StickerButton(page, nombres[i], letras[i], min, max, null);
                Place(page, nombres[i], min, max);
                Rebind(page, nombres[i], actos[i]);
                var label = page.Find(nombres[i] + "/Label")?.GetComponent<Text>();
                Storybook.LeafRead(label, 24);
                if (label != null) label.text = letras[i];
                portadaButtons[i] = page.Find(nombres[i])?.GetComponent<Button>();
            }

            if (page.Find("VolverHistorias") == null)
            {
                Storybook.StickerButton(page, "VolverHistorias", LeyDelMonte.VolverLabel,
                    new Vector2(0.014f, 0.22f), new Vector2(0.268f, 0.30f), null);
            }
            Place(page, "VolverHistorias", new Vector2(0.014f, 0.22f), new Vector2(0.268f, 0.30f));
            Rebind(page, "VolverHistorias", MostrarPortada);
            var volver = page.Find("VolverHistorias/Label")?.GetComponent<Text>();
            Storybook.LeafRead(volver, 22);
            if (volver != null) volver.text = LeyDelMonte.VolverLabel;

            historiaButtons = new[]
            {
                page.Find("Seguir")?.GetComponent<Button>(),
                page.Find("Historia")?.GetComponent<Button>(),
                page.Find("Calles")?.GetComponent<Button>(),
                page.Find("Bosque")?.GetComponent<Button>(),
                page.Find("Valle")?.GetComponent<Button>(),
                page.Find("Mina")?.GetComponent<Button>(),
                page.Find("Comenzar")?.GetComponent<Button>(),
                page.Find("Palabras")?.GetComponent<Button>(),
                page.Find("Teclado")?.GetComponent<Button>(),
                page.Find("VolverHistorias")?.GetComponent<Button>(),
            };
        }

        static void FilaGrande(int i, out Vector2 min, out Vector2 max)
        {
            const float alto = 0.11f;
            const float paso = 0.14f;
            float y1 = 0.78f - i * paso;
            min = new Vector2(0.014f, y1 - alto);
            max = new Vector2(0.30f, y1);
        }

        void ArmarPaginas(Transform page)
        {
            paginaBotones = HojaDeTexto(page, "PaginaBotones", LeyDelMonte.BotonesTitulo, LeyDelMonte.BotonesLead, LeyDelMonte.BotonesCuerpo);
            paginaCreditos = HojaDeTexto(page, "PaginaCreditos", LeyDelMonte.CreditosTitulo, string.Empty, LeyDelMonte.Credits);
            paginaAjustes = HojaDeAjustes(page);
            if (page.Find("PaginaPersonaje") == null)
            {
                var dim = Storybook.Plate(page, "PaginaPersonaje", Vector2.zero, Vector2.one, Storybook.Veil);
                dim.raycastTarget = true;
                Storybook.StickerButton(dim.transform, "AtrasPersonaje", LeyDelMonte.VolverLabel,
                    new Vector2(0.08f, 0.04f), new Vector2(0.32f, 0.14f), CancelarPersonaje);
                Storybook.StickerButton(dim.transform, "ListoPersonaje", LeyDelMonte.PersonajeListo,
                    new Vector2(0.68f, 0.04f), new Vector2(0.92f, 0.14f), ConfirmarPersonaje);
            }

            paginaPersonaje = page.Find("PaginaPersonaje")?.gameObject;
        }

        GameObject HojaDeTexto(Transform page, string name, string titulo, string lead, string cuerpo)
        {
            var existente = page.Find(name);
            if (existente != null)
            {
                if (string.IsNullOrEmpty(lead)) AbrirCuerpo(existente);
                return existente.gameObject;
            }
            var dim = Storybook.Plate(page, name, Vector2.zero, Vector2.one, Storybook.Veil);
            dim.raycastTarget = true;
            Storybook.Card(dim.transform, "Hoja", new Vector2(0.08f, 0.06f), new Vector2(0.92f, 0.94f), Storybook.Panel);
            var title = Texto(dim.transform, "Titulo", new Vector2(0.12f, 0.84f), new Vector2(0.88f, 0.93f));
            Storybook.DressText(title, titulo, Storybook.DisplayFont, 42, Color.white, FontStyle.Bold);
            Storybook.Rule(dim.transform, "Regla", new Vector2(0.12f, 0.822f), new Vector2(0.34f, 0.834f));
            var aviso = Texto(dim.transform, "Aviso", new Vector2(0.12f, 0.74f), new Vector2(0.88f, 0.815f));
            Storybook.DressText(aviso, lead, Storybook.DisplayFont, 26, Storybook.Play, FontStyle.Bold);
            var cuerpoTexto = Texto(dim.transform, "Cuerpo", new Vector2(0.12f, 0.20f), new Vector2(0.88f, 0.74f));
            if (string.IsNullOrEmpty(lead)) AbrirCuerpo(dim.transform);
            Storybook.DressText(cuerpoTexto, cuerpo, Storybook.BodyFont, 26, Storybook.Chalk, FontStyle.Normal);
            cuerpoTexto.alignment = TextAnchor.UpperLeft;
            cuerpoTexto.lineSpacing = 1.02f;
            cuerpoTexto.resizeTextForBestFit = true;
            cuerpoTexto.resizeTextMinSize = 18;
            cuerpoTexto.resizeTextMaxSize = 26;
            Storybook.StickerButton(dim.transform, "Volver", LeyDelMonte.VolverLabel,
                new Vector2(0.34f, 0.05f), new Vector2(0.66f, 0.16f), MostrarPortada);
            return dim.gameObject;
        }

        static void AbrirCuerpo(Transform hoja)
        {
            var aviso = hoja.Find("Aviso");
            if (aviso != null) aviso.gameObject.SetActive(false);
            var cuerpo = hoja.Find("Cuerpo");
            if (cuerpo != null)
                Storybook.Anchors(cuerpo, new Vector2(0.12f, 0.20f), new Vector2(0.88f, 0.81f));
        }

        GameObject HojaDeAjustes(Transform page)
        {
            var existente = page.Find("PaginaAjustes");
            if (existente != null)
            {
                musicaLabel = existente.Find("Musica/Label")?.GetComponent<Text>();
                pantallaLabel = existente.Find("Pantalla/Label")?.GetComponent<Text>();
                return existente.gameObject;
            }

            var dim = Storybook.Plate(page, "PaginaAjustes", Vector2.zero, Vector2.one, Storybook.Veil);
            dim.raycastTarget = true;
            Storybook.Card(dim.transform, "Hoja", new Vector2(0.16f, 0.16f), new Vector2(0.84f, 0.88f), Storybook.Panel);
            var title = Texto(dim.transform, "Titulo", new Vector2(0.2f, 0.74f), new Vector2(0.8f, 0.86f));
            Storybook.DressText(title, LeyDelMonte.AjustesTitulo, Storybook.DisplayFont, 42, Color.white, FontStyle.Bold);
            Storybook.Rule(dim.transform, "Regla", new Vector2(0.28f, 0.72f), new Vector2(0.72f, 0.734f));
            var musica = Storybook.StickerButton(dim.transform, "Musica", "MÚSICA: SÍ",
                new Vector2(0.24f, 0.54f), new Vector2(0.76f, 0.68f), AlternarMusica);
            var pantalla = Storybook.StickerButton(dim.transform, "Pantalla", "PANTALLA COMPLETA: NO",
                new Vector2(0.24f, 0.36f), new Vector2(0.76f, 0.50f), AlternarPantalla);
            musicaLabel = musica.transform.Find("Label")?.GetComponent<Text>();
            pantallaLabel = pantalla.transform.Find("Label")?.GetComponent<Text>();
            Storybook.LeafRead(musicaLabel, 26);
            Storybook.LeafRead(pantallaLabel, 26);
            Storybook.StickerButton(dim.transform, "Volver", LeyDelMonte.VolverLabel,
                new Vector2(0.34f, 0.20f), new Vector2(0.66f, 0.32f), MostrarPortada);
            return dim.gameObject;
        }

        static Text Texto(Transform parent, string name, Vector2 min, Vector2 max)
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

        void MostrarPortada()
        {
            hoja = Hoja.Portada;
            alElegirPersonaje = null;
            VerNombres(HistoriaNombres(), false);
            VerObjeto("VolverHistorias", false);
            VerPortada(true);
            VerReparto(false);
            CerrarHojas();
            var page = Pagina();
            if (page != null && portadaButtons != null)
            {
                for (int i = 0; i < portadaButtons.Length; i++)
                    if (portadaButtons[i] != null) portadaButtons[i].transform.SetAsLastSibling();
            }

            VerPista(true);
            if (storyHint != null)
            {
                storyHint.text = LeyDelMonte.PortadaHint;
                storyHint.transform.SetAsLastSibling();
            }

            UsarMenu(portadaButtons);
        }

        void AbrirHistorias()
        {
            hoja = Hoja.Historias;
            VerPortada(false);
            VerNombres(HistoriaNombres(), true);
            VerObjeto("VolverHistorias", true);
            VerReparto(false);
            CerrarHojas();
            LevantarHistorias();
            VerPista(true);
            if (storyHint != null) storyHint.text = LeyDelMonte.HistoriasHint;
            SeguirElCamino.Pintar(battle);
            UsarMenu(historiaButtons);
        }

        void AbrirBotones()
        {
            hoja = Hoja.Botones;
            VerPista(false);
            VerPortada(false);
            VerNombres(HistoriaNombres(), false);
            VerObjeto("VolverHistorias", false);
            VerReparto(false);
            CerrarHojas();
            if (paginaBotones != null)
            {
                paginaBotones.SetActive(true);
                paginaBotones.transform.SetAsLastSibling();
                UsarMenu(new[] { paginaBotones.transform.Find("Volver")?.GetComponent<Button>() });
            }
        }

        void AbrirCreditos()
        {
            hoja = Hoja.Creditos;
            VerPista(false);
            VerPortada(false);
            VerNombres(HistoriaNombres(), false);
            VerObjeto("VolverHistorias", false);
            VerReparto(false);
            CerrarHojas();
            if (paginaCreditos != null)
            {
                paginaCreditos.SetActive(true);
                paginaCreditos.transform.SetAsLastSibling();
                UsarMenu(new[] { paginaCreditos.transform.Find("Volver")?.GetComponent<Button>() });
            }
        }

        void AbrirAjustes()
        {
            hoja = Hoja.Ajustes;
            VerPista(false);
            VerPortada(false);
            VerNombres(HistoriaNombres(), false);
            VerObjeto("VolverHistorias", false);
            VerReparto(false);
            CerrarHojas();
            if (paginaAjustes == null) return;
            paginaAjustes.SetActive(true);
            paginaAjustes.transform.SetAsLastSibling();
            PintarAjustes();
            UsarMenu(new[]
            {
                paginaAjustes.transform.Find("Musica")?.GetComponent<Button>(),
                paginaAjustes.transform.Find("Pantalla")?.GetComponent<Button>(),
                paginaAjustes.transform.Find("Volver")?.GetComponent<Button>(),
            });
        }

        void PedirPersonaje(System.Action alElegir)
        {
            alElegirPersonaje = alElegir;
            hoja = Hoja.Personaje;
            VerPista(false);
            VerPortada(false);
            VerNombres(HistoriaNombres(), false);
            VerObjeto("VolverHistorias", false);
            CerrarHojas();
            VerReparto(true);
            if (paginaPersonaje != null) paginaPersonaje.transform.SetAsLastSibling();
            var page = Pagina();
            if (page != null)
            {
                page.Find("Ficha")?.SetAsLastSibling();
                page.Find("Roster")?.SetAsLastSibling();
                page.Find("PersonajePista")?.SetAsLastSibling();
                page.Find("Elige")?.SetAsLastSibling();
                PaintRoster(page);
            }

            menuButtons = System.Array.Empty<Button>();
            if (menuCursor != null) menuCursor.gameObject.SetActive(false);
        }

        void ConfirmarPersonaje()
        {
            var ir = alElegirPersonaje;
            alElegirPersonaje = null;
            hoja = Hoja.Historias;
            VerReparto(false);
            VerNombres(HistoriaNombres(), true);
            VerObjeto("VolverHistorias", true);
            ir?.Invoke();
        }

        void CancelarPersonaje()
        {
            alElegirPersonaje = null;
            AbrirHistorias();
        }

        void AlternarMusica()
        {
            Jornada.Musica = !Jornada.Musica;
            PintarAjustes();
        }

        void AlternarPantalla()
        {
            Screen.fullScreen = !Screen.fullScreen;
            PintarAjustes();
        }

        void PintarAjustes()
        {
            if (musicaLabel != null)
                musicaLabel.text = Jornada.Musica ? "MÚSICA: SÍ" : "MÚSICA: NO";
            if (pantallaLabel != null)
                pantallaLabel.text = Screen.fullScreen ? "PANTALLA COMPLETA: SÍ" : "PANTALLA COMPLETA: NO";
        }

        void UsarMenu(Button[] botones)
        {
            menuButtons = botones;
            menuIndex = 0;
            if (botones == null) return;
            for (int i = 0; i < botones.Length; i++)
            {
                int slot = i;
                var button = botones[i];
                if (button == null) continue;
                var motion = button.GetComponent<MenuButtonMotion>();
                if (motion == null) motion = button.gameObject.AddComponent<MenuButtonMotion>();
                motion.SetHot(() =>
                {
                    menuIndex = slot;
                    PaintMenu();
                });
            }

            PaintMenu();
        }

        void VerPista(bool on)
        {
            if (storyHint != null) storyHint.gameObject.SetActive(on);
        }

        void VerPortada(bool on)
        {
            if (portadaButtons == null) return;
            for (int i = 0; i < portadaButtons.Length; i++)
            {
                if (portadaButtons[i] != null) portadaButtons[i].gameObject.SetActive(on);
            }
        }

        void VerReparto(bool on)
        {
            VerObjeto("Roster", on);
            VerObjeto("Ficha", on);
            VerObjeto("Elige", on);
            VerObjeto("EligeFondo", on);
            VerObjeto("PersonajePista", on);
            if (paginaPersonaje != null) paginaPersonaje.SetActive(on);
        }

        void CerrarHojas()
        {
            if (paginaBotones != null) paginaBotones.SetActive(false);
            if (paginaAjustes != null) paginaAjustes.SetActive(false);
            if (paginaCreditos != null) paginaCreditos.SetActive(false);
        }

        void VerObjeto(string name, bool on)
        {
            var page = Pagina();
            if (page == null) return;
            var child = page.Find(name);
            if (child != null) child.gameObject.SetActive(on);
            var shadow = page.Find(name + "Shadow");
            if (shadow != null) shadow.gameObject.SetActive(on);
        }

        void VerNombres(string[] nombres, bool on)
        {
            for (int i = 0; i < nombres.Length; i++) VerObjeto(nombres[i], on);
        }

        void LevantarHistorias()
        {
            var page = Pagina();
            if (page == null) return;
            var nombres = HistoriaNombres();
            for (int i = 0; i < nombres.Length; i++) Lift(page, nombres[i]);
            Lift(page, "VolverHistorias");
            if (storyHint != null) storyHint.transform.SetAsLastSibling();
        }

        static string[] HistoriaNombres()
        {
            return new[] { "Seguir", "Historia", "Calles", "Bosque", "Valle", "Mina", "Comenzar", "Palabras", "Teclado" };
        }

        Transform Pagina()
        {
            return battle != null && battle.startScreen != null ? battle.startScreen.transform : null;
        }

        static void Fila(int i, out Vector2 min, out Vector2 max)
        {
            const float alto = 0.056f;
            const float paso = 0.064f;
            float y1 = 0.892f - i * paso;
            min = new Vector2(0.014f, y1 - alto);
            max = new Vector2(0.268f, y1);
        }

        static void Place(Transform page, string name, Vector2 min, Vector2 max)
        {
            var widget = page.Find(name);
            if (widget != null) Storybook.Anchors(widget, min, max);
            var shadow = page.Find(name + "Shadow");
            if (shadow != null)
                Storybook.Anchors(shadow, min + new Vector2(0.008f, -0.014f), max + new Vector2(0.008f, -0.014f));
        }

        static void Lift(Transform page, string name)
        {
            var shadow = page.Find(name + "Shadow");
            if (shadow != null) shadow.SetAsLastSibling();
            var widget = page.Find(name);
            if (widget != null) widget.SetAsLastSibling();
        }

        void EnsureCursor(Transform page)
        {
            var found = page.Find("MenuCursor");
            if (found == null)
            {
                var go = new GameObject("MenuCursor");
                go.transform.SetParent(page, false);
                go.AddComponent<RectTransform>();
                menuCursor = go.AddComponent<Text>();
                Storybook.DressText(menuCursor, ">", Storybook.DisplayFont, 40, Color.white, FontStyle.Bold);
            }
            else
            {
                menuCursor = found.GetComponent<Text>();
            }

            Storybook.Stamp(menuCursor, 1.6f);
        }

        void BindMenu(Transform page)
        {
            menuButtons = new[]
            {
                page.Find("Historia")?.GetComponent<Button>(),
                page.Find("Calles")?.GetComponent<Button>(),
                page.Find("Bosque")?.GetComponent<Button>(),
                page.Find("Valle")?.GetComponent<Button>(),
                page.Find("Mina")?.GetComponent<Button>(),
                page.Find("Comenzar")?.GetComponent<Button>(),
                page.Find("Palabras")?.GetComponent<Button>(),
                page.Find("Teclado")?.GetComponent<Button>(),
            };

            for (int i = 0; i < menuButtons.Length; i++)
            {
                int slot = i;
                var button = menuButtons[i];
                if (button == null) continue;
                var motion = button.GetComponent<MenuButtonMotion>();
                if (motion == null) motion = button.gameObject.AddComponent<MenuButtonMotion>();
                motion.SetHot(() =>
                {
                    menuIndex = slot;
                    PaintMenu();
                });
                var label = button.transform.Find("Label")?.GetComponent<Text>();
                Storybook.LeafRead(label, 20);
            }
        }

        void PaintMenu()
        {
            if (menuButtons == null || menuButtons.Length == 0) return;
            for (int i = 0; i < menuButtons.Length; i++)
            {
                var button = menuButtons[i];
                if (button == null) continue;
                bool chosen = i == menuIndex;
                Storybook.LeafColors(button, chosen);
                var motion = button.GetComponent<MenuButtonMotion>();
                if (motion != null) motion.Chosen = chosen;
            }

            int slot = Mathf.Clamp(menuIndex, 0, menuButtons.Length - 1);
            var selected = menuButtons[slot];
            if (menuCursor == null || selected == null) return;
            menuCursor.gameObject.SetActive(true);
            menuCursor.transform.SetParent(selected.transform, false);
            var rect = menuCursor.rectTransform;
            rect.anchorMin = new Vector2(0f, 0.5f);
            rect.anchorMax = new Vector2(0f, 0.5f);
            rect.pivot = new Vector2(1f, 0.5f);
            rect.sizeDelta = new Vector2(48f, 52f);
            rect.anchoredPosition = new Vector2(-2f, 0f);
            menuCursor.color = Storybook.GreenChalk;
            menuCursor.transform.SetAsLastSibling();
        }

        void DressRoster(Transform page)
        {
            var row = page.Find("Roster");
            int count = FighterRoster.Choices.Length;
            int buttons = 0;
            if (row != null)
            {
                for (int i = 0; i < row.childCount; i++)
                    if (row.GetChild(i).name.StartsWith("Who")) buttons++;
            }

            if (row == null || buttons != count)
            {
                if (row != null)
                {
                    row.gameObject.name = "RosterOld";
                    Destroy(row.gameObject);
                }

                var root = new GameObject("Roster");
                root.transform.SetParent(page, false);
                var rect = root.AddComponent<RectTransform>();
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
                row = root.transform;

                for (int i = 0; i < count; i++)
                {
                    int picked = i;
                    RosterCell(count, i, out Vector2 min, out Vector2 max);
                    var choice = Storybook.StickerButton(row, "Who" + i, FighterRoster.Choices[i].Label,
                        min, max,
                        () =>
                        {
                            FighterRoster.Select(picked);
                            PaintRoster(page);
                        },
                        false);
                    var caption = choice.transform.Find("Label")?.GetComponent<Text>();
                    if (caption != null)
                    {
                        caption.alignment = TextAnchor.LowerCenter;
                        var labelRect = caption.rectTransform;
                        labelRect.anchorMin = new Vector2(0.04f, 0.02f);
                        labelRect.anchorMax = new Vector2(0.96f, 0.32f);
                        labelRect.offsetMin = Vector2.zero;
                        labelRect.offsetMax = Vector2.zero;
                        Storybook.LeafRead(caption, 24);
                        caption.resizeTextMinSize = 10;
                        caption.resizeTextMaxSize = 22;
                        var halo = caption.transform.Find("Halo")?.GetComponent<Text>();
                        if (halo != null)
                        {
                            halo.resizeTextMinSize = 10;
                            halo.resizeTextMaxSize = 22;
                        }
                    }

                    // The drawing sits in its own well so the card reads as a card.
                    Storybook.Plate(choice.transform, "Pozo",
                        new Vector2(0.055f, 0.298f), new Vector2(0.945f, 0.945f),
                        new Color(0.025f, 0.065f, 0.045f, 0.48f));
                    Storybook.Plate(choice.transform, "Filete",
                        new Vector2(0.10f, 0.291f), new Vector2(0.90f, 0.300f),
                        new Color(Storybook.Gold.r, Storybook.Gold.g, Storybook.Gold.b, 0.50f));

                    var card = choice.gameObject.AddComponent<RosterCard>();
                    card.Bind(FighterRoster.Choices[i].Sheet);
                    var motion = choice.GetComponent<MenuButtonMotion>();
                    if (motion != null) motion.Subtle = true;
                }
            }

            SeatRoster(row);
            DressPickTitle(page);
            DressFicha(page);
            PaintRoster(page);
        }

        static void NameButton(Transform page, string name, string label)
        {
            var text = page.Find(name + "/Label")?.GetComponent<Text>();
            if (text == null) return;
            Storybook.DressText(text, label, Storybook.BodyFont, 26, Storybook.Gold, FontStyle.Bold);
            Storybook.LeafRead(text, 20);
            text.alignment = TextAnchor.MiddleCenter;
        }

        /// <summary>Grilla de fichas: cuatro columnas y las filas que hagan falta.</summary>
        static void RosterCell(int count, int index, out Vector2 min, out Vector2 max)
        {
            int cols = Mathf.Clamp(FighterRoster.RosterColumns, 1, Mathf.Max(1, count));
            int rows = Mathf.CeilToInt(count / (float)cols);
            const float left = 0.16f;
            const float right = 0.84f;
            const float bottom = 0.33f;
            const float top = 0.74f;
            float gapX = cols <= 3 ? 0.02f : 0.012f;
            float gapY = rows <= 1 ? 0f : 0.012f;
            float width = (right - left - gapX * (cols - 1)) / cols;
            float height = (top - bottom - gapY * (rows - 1)) / rows;
            int row = index / cols;
            int col = index % cols;
            float x = left + col * (width + gapX);
            float y = top - row * (height + gapY);
            min = new Vector2(x, y - height);
            max = new Vector2(x + width, y);
        }

        static void SeatRoster(Transform row)
        {
            if (row == null) return;
            int count = FighterRoster.Choices.Length;
            for (int i = 0; i < count; i++)
            {
                var who = row.Find("Who" + i);
                if (who == null) continue;
                RosterCell(count, i, out Vector2 min, out Vector2 max);
                Storybook.Anchors(who, min, max);
            }
        }

        void DressFicha(Transform page)
        {
            var card = page.Find("Ficha");
            if (card == null)
            {
                var face = Storybook.Card(page, "Ficha", new Vector2(0.16f, 0.135f), new Vector2(0.84f, 0.320f),
                new Color(0.018f, 0.055f, 0.038f, 0.96f));
                face.raycastTarget = false;
                card = face.transform;
                Line(card, "Kicker", new Vector2(0.035f, 0.80f), new Vector2(0.55f, 0.975f), 15, Storybook.Gold);
                Line(card, "Nombre", new Vector2(0.035f, 0.47f), new Vector2(0.52f, 0.81f), 30, Color.white);
                Line(card, "Oficio", new Vector2(0.52f, 0.45f), new Vector2(0.965f, 0.79f), 19, Storybook.Gold);
                Line(card, "Cuerpo", new Vector2(0.035f, 0.22f), new Vector2(0.965f, 0.50f), 22, Storybook.Chalk);
                Line(card, "Hace", new Vector2(0.035f, 0.02f), new Vector2(0.965f, 0.21f), 14, Storybook.Mute);
            }
            else
            {
                Storybook.Anchors(card, new Vector2(0.16f, 0.135f), new Vector2(0.84f, 0.320f));
                var image = card.GetComponent<Image>();
                if (image != null) image.color = new Color(0.018f, 0.055f, 0.038f, 0.96f);
                Sit(card, "Kicker", new Vector2(0.035f, 0.80f), new Vector2(0.55f, 0.975f));
                Sit(card, "Nombre", new Vector2(0.035f, 0.47f), new Vector2(0.52f, 0.81f));
                Sit(card, "Oficio", new Vector2(0.52f, 0.45f), new Vector2(0.965f, 0.79f));
                Sit(card, "Cuerpo", new Vector2(0.035f, 0.22f), new Vector2(0.965f, 0.50f));
                Sit(card, "Hace", new Vector2(0.035f, 0.02f), new Vector2(0.965f, 0.21f));
                PaintLine(card, "Kicker", Storybook.Gold);
                PaintLine(card, "Nombre", Color.white);
                PaintLine(card, "Oficio", Storybook.Gold);
                PaintLine(card, "Cuerpo", Storybook.Chalk);
                PaintLine(card, "Hace", Storybook.Mute);
            }

            var fichaShadow = page.Find("FichaShadow");
            if (fichaShadow != null) fichaShadow.gameObject.SetActive(false);
            BordesFicha(card);
            card.SetAsLastSibling();
        }

        static void Sit(Transform card, string child, Vector2 min, Vector2 max)
        {
            var text = card.Find(child)?.GetComponent<Text>();
            if (text != null) Storybook.Anchors(text, min, max);
        }

        static void BordesFicha(Transform card)
        {
            if (card.Find("Filete") == null)
                Storybook.Plate(card, "Filete", new Vector2(0.03f, 0.985f), new Vector2(0.97f, 0.995f),
                    new Color(Storybook.Gold.r, Storybook.Gold.g, Storybook.Gold.b, 0.65f));
        }

        static Text Line(Transform parent, string name, Vector2 min, Vector2 max, int size, Color color)
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
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = Mathf.Max(9, size - 8);
            text.resizeTextMaxSize = size;
            return text;
        }

        void DressPickTitle(Transform page)
        {
            var existing = page.Find("Elige");
            Text text;
            if (existing == null)
            {
                var go = new GameObject("Elige");
                go.transform.SetParent(page, false);
                go.AddComponent<RectTransform>();
                text = go.AddComponent<Text>();
                text.raycastTarget = false;
            }
            else
            {
                text = existing.GetComponent<Text>();
            }

            var fondo = page.Find("EligeFondo");
            if (fondo == null)
            {
                var plate = Storybook.Plate(page, "EligeFondo",
                    new Vector2(0.085f, 0.768f), new Vector2(0.915f, 1f),
                    new Color(0.010f, 0.036f, 0.024f, 1f));
                plate.raycastTarget = false;
                Storybook.Plate(plate.transform, "FileteBajo",
                    new Vector2(0f, 0f), new Vector2(1f, 0.012f),
                    new Color(Storybook.Gold.r, Storybook.Gold.g, Storybook.Gold.b, 0.75f)).raycastTarget = false;
            }
            else
            {
                Storybook.Anchors(fondo, new Vector2(0.085f, 0.768f), new Vector2(0.915f, 1f));
            }

            Storybook.Anchors(text, new Vector2(0.12f, 0.848f), new Vector2(0.88f, 0.962f));
            Storybook.DressText(text, LeyDelMonte.PersonajeAviso, Storybook.CharacterFont, 46,
                new Color(0.98f, 0.95f, 0.88f, 1f), FontStyle.Normal);
            text.alignment = TextAnchor.MiddleCenter;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = 22;
            text.resizeTextMaxSize = 46;
            Storybook.Stamp(text, 1.6f);
            text.transform.SetAsLastSibling();

            var pista = page.Find("PersonajePista")?.GetComponent<Text>();
            if (pista == null)
            {
                var go = new GameObject("PersonajePista");
                go.transform.SetParent(page, false);
                go.AddComponent<RectTransform>();
                pista = go.AddComponent<Text>();
                pista.raycastTarget = false;
            }

            Storybook.Anchors(pista, new Vector2(0.16f, 0.798f), new Vector2(0.84f, 0.846f));
            Storybook.DressText(pista, LeyDelMonte.PersonajePista, Storybook.LabelFont, 18,
                new Color(0.96f, 0.90f, 0.76f, 0.95f), FontStyle.Bold);
            pista.alignment = TextAnchor.MiddleCenter;
            pista.raycastTarget = false;
            Storybook.Stamp(pista, 1.1f);
        }

        void PaintRoster(Transform page)
        {
            var row = page.Find("Roster");
            if (row == null) return;
            for (int i = 0; i < FighterRoster.Choices.Length; i++)
            {
                var face = row.Find("Who" + i)?.GetComponent<Image>();
                var button = face != null ? face.GetComponent<Button>() : null;
                if (button == null) continue;
                bool chosen = i == FighterRoster.Index;
                Storybook.LeafColors(button, chosen);
                var caption = button.transform.Find("Label")?.GetComponent<Text>();
                if (caption != null)
                {
                    caption.font = Storybook.LabelFont;
                    caption.fontStyle = FontStyle.Bold;
                    caption.fontSize = 22;
                    caption.resizeTextMaxSize = 22;
                    caption.color = chosen ? Storybook.GreenInk : Storybook.GreenChalk;
                    var ink = caption.GetComponent<Outline>();
                    if (ink != null) ink.enabled = false;
                }

                var card = button.GetComponent<RosterCard>();
                if (card != null) card.Chosen = chosen;
                var motion = button.GetComponent<MenuButtonMotion>();
                if (motion != null) motion.Chosen = chosen;
            }

            PaintFicha(page);
        }

        void PaintFicha(Transform page)
        {
            var card = page.Find("Ficha");
            if (card == null) return;
            var ficha = FighterRoster.Card(FighterRoster.Current);
            Write(card, "Kicker", "FICHA");
            Write(card, "Nombre", ficha.Nombre);
            Write(card, "Oficio", ficha.Oficio);
            Write(card, "Cuerpo", ficha.Cuerpo);
            Write(card, "Hace", ficha.Hace);
            VestirLetraFicha(card);
            card.SetAsLastSibling();
        }

        static void VestirLetraFicha(Transform card)
        {
            var nombre = card.Find("Nombre")?.GetComponent<Text>();
            if (nombre != null)
            {
                nombre.font = Storybook.CharacterFont;
                nombre.fontStyle = FontStyle.Bold;
                nombre.fontSize = 30;
                nombre.resizeTextMaxSize = 30;
                nombre.color = new Color(0.97f, 0.94f, 0.86f, 1f);
                nombre.alignment = TextAnchor.MiddleLeft;
            }

            var oficio = card.Find("Oficio")?.GetComponent<Text>();
            if (oficio != null)
            {
                oficio.font = Storybook.LabelFont;
                oficio.fontStyle = FontStyle.Bold;
                oficio.fontSize = 19;
                oficio.resizeTextMaxSize = 19;
                oficio.color = Storybook.Gold;
                oficio.alignment = TextAnchor.MiddleRight;
            }

            var kicker = card.Find("Kicker")?.GetComponent<Text>();
            if (kicker != null)
            {
                kicker.font = Storybook.LabelFont;
                kicker.fontStyle = FontStyle.Bold;
                kicker.fontSize = 14;
                kicker.resizeTextMaxSize = 14;
                kicker.color = new Color(Storybook.Gold.r, Storybook.Gold.g, Storybook.Gold.b, 0.92f);
                kicker.alignment = TextAnchor.MiddleLeft;
            }

            var cuerpo = card.Find("Cuerpo")?.GetComponent<Text>();
            if (cuerpo != null)
            {
                cuerpo.font = Storybook.BodyFont;
                cuerpo.fontStyle = FontStyle.Normal;
                cuerpo.fontSize = 22;
                cuerpo.resizeTextMaxSize = 22;
                cuerpo.color = new Color(0.93f, 0.91f, 0.84f, 1f);
                cuerpo.alignment = TextAnchor.MiddleLeft;
            }

            var hace = card.Find("Hace")?.GetComponent<Text>();
            if (hace != null)
            {
                hace.font = Storybook.LabelFont;
                hace.fontStyle = FontStyle.Normal;
                hace.fontSize = 14;
                hace.resizeTextMaxSize = 14;
                hace.color = new Color(0.80f, 0.83f, 0.78f, 0.95f);
                hace.alignment = TextAnchor.MiddleLeft;
            }
        }

        static void Write(Transform card, string child, string value)
        {
            var text = card.Find(child)?.GetComponent<Text>();
            if (text == null) return;
            text.text = value;
        }

        static void PaintLine(Transform card, string child, Color color)
        {
            var text = card.Find(child)?.GetComponent<Text>();
            if (text != null) text.color = color;
        }

        void EmpezarCamino()
        {
            if (SeguirElCamino.Terminado())
            {
                battle?.Contar("EL CAMINO", LeyDelMonte.CaminoListo);
                SeguirElCamino.Pintar(battle);
                return;
            }

            PedirPersonaje(() =>
            {
                SeguirElCamino.Empezar(battle);
                SeguirElCamino.Abrir();
            });
        }

        void Update()
        {
            SeguirElCamino.Tick();
            if (CajamarcaCalles.IsOpen || BosqueCapitulo.IsOpen || CajamarcaRealCapitulo.IsOpen || MontanaDelPerol.IsOpen) return;
            if (battle == null || battle.IsPlaying || battle.IsGameOver || HoldsTheGate || AteThisFrame) return;
            if (battle.startScreen == null || !battle.startScreen.activeSelf) return;

            if (PalabrasCartilla.IsOpen)
            {
                if (Input.GetKeyDown(KeyCode.Escape))
                {
                    Storybook.TocarBoton();
                    PalabrasCartilla.Close();
                }
                else if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S) ||
                         Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.E) ||
                         Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space))
                {
                    Storybook.TocarBoton();
                    PalabrasCartilla.Next();
                }
                else if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W) ||
                         Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.Q))
                {
                    Storybook.TocarBoton();
                    PalabrasCartilla.Prev();
                }
                return;
            }

            if (hoja == Hoja.Personaje)
            {
                LeerPersonaje();
                return;
            }

            if ((hoja == Hoja.Historias || hoja == Hoja.Botones || hoja == Hoja.Ajustes || hoja == Hoja.Creditos)
                && Input.GetKeyDown(KeyCode.Escape))
            {
                Storybook.TocarBoton();
                MostrarPortada();
                return;
            }

            if (menuCursor != null)
            {
                float blink = 0.45f + 0.55f * Mathf.Abs(Mathf.Sin(Time.unscaledTime * 4.2f));
                var chalk = Storybook.GreenChalk;
                menuCursor.color = new Color(chalk.r, chalk.g, chalk.b, blink);
            }

            if (menuButtons != null && menuButtons.Length > 0)
            {
                if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W))
                {
                    menuIndex = (menuIndex + menuButtons.Length - 1) % menuButtons.Length;
                    PaintMenu();
                    return;
                }

                if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S))
                {
                    menuIndex = (menuIndex + 1) % menuButtons.Length;
                    PaintMenu();
                    return;
                }

                if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetKeyDown(KeyCode.Space))
                {
                    var chosen = menuButtons[Mathf.Clamp(menuIndex, 0, menuButtons.Length - 1)];
                    if (chosen != null)
                    {
                        Storybook.TocarBoton();
                        chosen.onClick.Invoke();
                    }
                }
            }
        }

        void LeerPersonaje()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                Storybook.TocarBoton();
                CancelarPersonaje();
                return;
            }

            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetKeyDown(KeyCode.Space))
            {
                Storybook.TocarBoton();
                ConfirmarPersonaje();
                return;
            }

            int count = FighterRoster.Choices.Length;
            int cols = Mathf.Clamp(FighterRoster.RosterColumns, 1, Mathf.Max(1, count));
            int next = FighterRoster.Index;
            if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.Q) || Input.GetKeyDown(KeyCode.A))
                next--;
            else if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.D))
                next++;
            else if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W))
                next -= cols;
            else if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S))
                next += cols;
            else
                return;

            FighterRoster.Select(next);
            if (battle.startScreen != null) PaintRoster(battle.startScreen.transform);
        }

        void DressEnding(Transform page)
        {
            var endCard = page.Find("EndCard");
            if (endCard == null)
                Storybook.Card(page, "EndCard", new Vector2(0.16f, 0.24f), new Vector2(0.84f, 0.76f), Storybook.Panel);
            else
            {
                var endImage = endCard.GetComponent<Image>();
                if (endImage != null) endImage.color = Storybook.Panel;
            }

            var text = page.Find("GOText")?.GetComponent<Text>();
            Storybook.Anchors(text, new Vector2(0.2f, 0.38f), new Vector2(0.8f, 0.72f));
            if (text == null) return;
            text.font = Storybook.DisplayFont;
            text.fontSize = 34;
            text.color = Color.white;
            text.alignment = TextAnchor.MiddleCenter;
            text.raycastTarget = false;
            text.transform.SetAsLastSibling();
        }

        void DressScores(Transform canvas)
        {
            RestyleScore(canvas, "AYNI", "AYNI", Storybook.Spark);
            RestyleScore(canvas, "Espiritualidad", "ESPÍRITU", Storybook.Gold);
            RestyleScore(canvas, "Respeto", "REPUTACIÓN", Storybook.Vine);
            RestyleScore(canvas, "Dinero", "CODICIA", Storybook.Fox);
        }

        void RestyleScore(Transform canvas, string objectName, string caption, Color fill)
        {
            var root = canvas.Find(objectName);
            if (root == null) return;
            var label = root.Find("Lbl")?.GetComponent<Text>();
            if (label != null)
            {
                label.text = caption;
                label.font = Storybook.DisplayFont;
                label.fontSize = 13;
                label.fontStyle = FontStyle.Bold;
                label.color = Color.white;
                Storybook.Stamp(label, 1.2f);
                label.alignment = TextAnchor.MiddleLeft;
                label.raycastTarget = false;
            }

            Storybook.TintBar(root.Find("Bar"), fill);
        }

        void DressHealth(LagunaBattleManager battle)
        {
            StyleName(battle.playerNameText, TextAnchor.MiddleLeft);
            StyleName(battle.enemyNameText, TextAnchor.MiddleRight);
            if (battle.playerHealthBar != null) Storybook.TintBar(battle.playerHealthBar.transform, Storybook.Spark);
            if (battle.enemyHealthBar != null) Storybook.TintBar(battle.enemyHealthBar.transform, Storybook.Fox);
        }

        static void StyleName(Text text, TextAnchor anchor)
        {
            if (text == null) return;
            text.font = Storybook.DisplayFont;
            text.fontSize = 18;
            text.color = Color.white;
            text.alignment = anchor;
            text.raycastTarget = false;
            Storybook.Stamp(text);
        }

        void DressInstructions(Text text)
        {
            if (text == null) return;
            Storybook.DressText(text,
                "Salta el río. Junta vida, ayni, reputación y codicia: los botones de abajo los gastan. 1 patada  2 embestida  3 vida  4 ayni. Vizcacha: 5 coletazo.",
                Storybook.DisplayFont, 18, Color.white, FontStyle.Bold);
            text.alignment = TextAnchor.LowerCenter;
            Storybook.Stamp(text);
        }

        void EnsureBanner(Transform canvas)
        {
            if (banner == null)
            {
                var plate = Storybook.Plate(canvas, "ChapterBanner", new Vector2(0.18f, 0.36f), new Vector2(0.82f, 0.7f), Storybook.Panel);
                banner = plate.gameObject;

                var go = new GameObject("ChapterText");
                go.transform.SetParent(banner.transform, false);
                var rect = go.AddComponent<RectTransform>();
                rect.anchorMin = new Vector2(0.08f, 0.08f);
                rect.anchorMax = new Vector2(0.92f, 0.92f);
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
                bannerText = go.AddComponent<Text>();
                Storybook.DressText(bannerText, string.Empty, Storybook.DisplayFont, 28, Color.white, FontStyle.Bold);
                banner.SetActive(false);
            }

            var plateImage = banner.GetComponent<Image>();
            if (plateImage != null) plateImage.color = Storybook.Panel;
            if (bannerText != null) bannerText.color = Color.white;
            var shade = banner.transform.Find("Shade");
            if (shade != null) Destroy(shade.gameObject);
        }

        void Present(int index)
        {
            var chapter = LeyDelMonte.Chapters[index];
            showing = index;
            bannerText.text =
                "CAPÍTULO " + chapter.Roman + "\n" +
                chapter.Title + "\n\n«" +
                chapter.Law + "»\n— " + chapter.Speaker;
            banner.SetActive(true);
            banner.transform.SetAsLastSibling();
            CancelInvoke(nameof(HideBanner));
            Invoke(nameof(HideBanner), 7f);
        }

        void HideBanner()
        {
            if (banner != null) banner.SetActive(false);
            showing = -1;
            if (pending < 0) return;

            int next = pending;
            pending = -1;
            Present(next);
        }
    }

    /// <summary>
    /// El camino del folleto: promesa, río, lámpara y oro.
    /// Un capítulo se cierra en este frame y el siguiente se abre en el próximo,
    /// porque OnDestroy devuelve la cámara al final del frame.
    /// </summary>
    static class SeguirElCamino
    {
        public static bool Activo { get; private set; }
        static float espera;
        static bool cerrar;
        static LagunaBattleManager host;

        public static bool Terminado()
        {
            return PartidaDelCamino.PromesaHecha
                && PartidaDelCamino.RioCruzado
                && PartidaDelCamino.EleccionOro != PartidaDelCamino.Oro.Ninguno;
        }

        public static string Rotulo()
        {
            if (!PartidaDelCamino.PromesaHecha) return LeyDelMonte.SeguirLabel;
            if (!PartidaDelCamino.RioCruzado) return "SEGUIR · EL RÍO";
            if (PartidaDelCamino.EleccionOro == PartidaDelCamino.Oro.Ninguno) return "SEGUIR · EL ORO";
            return "EL CAMINO";
        }

        public static void Empezar(LagunaBattleManager battle)
        {
            host = battle;
            Activo = true;
            espera = 0f;
            cerrar = false;
        }

        public static void Soltar()
        {
            Activo = false;
            espera = 0f;
            cerrar = false;
        }

        public static void Tramo()
        {
            if (!Activo || espera > 0f || cerrar) return;
            espera = 2.2f;
        }

        public static void Tick()
        {
            if (!Activo) return;
            Pintar(host);
            if (espera > 0f)
            {
                espera -= Time.unscaledDeltaTime;
                if (espera > 0f) return;
                cerrar = true;
            }

            if (cerrar)
            {
                if (CajamarcaCalles.IsOpen || BosqueCapitulo.IsOpen || MontanaDelPerol.IsOpen)
                {
                    CajamarcaCalles.CloseCurrent();
                    BosqueCapitulo.CloseCurrent();
                    MontanaDelPerol.CloseCurrent();
                    return;
                }

                cerrar = false;
                Abrir();
                return;
            }

            if (!CajamarcaCalles.IsOpen && !BosqueCapitulo.IsOpen && !MontanaDelPerol.IsOpen && Terminado())
            {
                Activo = false;
                host?.Contar("EL CAMINO", LeyDelMonte.CaminoListo);
                Pintar(host);
            }
        }

        public static void Abrir()
        {
            if (host == null) return;
            if (!PartidaDelCamino.PromesaHecha)
            {
                CajamarcaCalles.Begin(host);
                return;
            }

            if (!PartidaDelCamino.RioCruzado)
            {
                BosqueCapitulo.Begin(host);
                return;
            }

            if (PartidaDelCamino.EleccionOro == PartidaDelCamino.Oro.Ninguno)
            {
                MontanaDelPerol.Begin(host);
                return;
            }

            Activo = false;
            host.Contar("EL CAMINO", LeyDelMonte.CaminoListo);
            Pintar(host);
        }

        public static void Pintar(LagunaBattleManager battle)
        {
            var label = battle?.startScreen?.transform.Find("Seguir/Label")?.GetComponent<UnityEngine.UI.Text>();
            if (label != null) label.text = Rotulo();
        }
    }
}
