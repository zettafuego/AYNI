#if UNITY_EDITOR
using System.IO;
using AYNI.Core;
using AYNI.Narrative;
using AYNI.Player;
using AYNI.UI;
using AYNI.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace AYNI.Editor
{
    public static class AyniSceneBootstrap
    {
        const string ScenePath = "Assets/Scenes/AYNI_Prototipo.unity";

        [MenuItem("AYNI/Crear Escena Prototipo")]
        public static void CreatePrototypeScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            CreateEnvironment();
            var player = CreatePlayer();
            CreateSystems();
            var canvas = CreateUI();
            WireUI(canvas);
            CreateWorldObjects();

            EnsureDirectory("Assets/Scenes");
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.Refresh();

            Debug.Log("[AYNI] Escena prototipo creada en " + ScenePath);
            EditorUtility.DisplayDialog(
                "AYNI — Cultura Jugable",
                "Prototipo creado.\n\n" +
                "Pulsa Play para jugar.\n" +
                "El Cuy Sabio te guiará desde el inicio.\n\n" +
                "WASD = mover | E = ofrenda | 1-4 = formas animales",
                "¡Ayni!");
        }

        static void CreateEnvironment()
        {
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Pachamama_Suelo";
            ground.transform.localScale = new Vector3(4f, 1f, 4f);
            ground.GetComponent<Renderer>().sharedMaterial = MakeColorMat("Mat_Suelo", new Color(0.25f, 0.5f, 0.2f));

            var sun = GameObject.Find("Directional Light");
            if (sun != null)
            {
                sun.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
                var light = sun.GetComponent<Light>();
                light.color = new Color(1f, 0.92f, 0.8f);
                light.intensity = 1.2f;
            }

            var manantial = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            manantial.name = "Manantial_Sagrado";
            manantial.transform.position = new Vector3(0f, 0.5f, 8f);
            manantial.transform.localScale = new Vector3(1.5f, 0.3f, 1.5f);
            manantial.GetComponent<Renderer>().sharedMaterial = MakeColorMat("Mat_Agua", new Color(0.2f, 0.6f, 0.9f, 0.8f));

            var altar = GameObject.CreatePrimitive(PrimitiveType.Cube);
            altar.name = "Altar_Condor";
            altar.transform.position = new Vector3(12f, 0.75f, -5f);
            altar.transform.localScale = new Vector3(2f, 1.5f, 2f);
            altar.GetComponent<Renderer>().sharedMaterial = MakeColorMat("Mat_Piedra", new Color(0.45f, 0.42f, 0.38f));

            var mountain = GameObject.CreatePrimitive(PrimitiveType.Cube);
            mountain.name = "Apu_Montana";
            mountain.transform.position = new Vector3(-20f, 5f, -15f);
            mountain.transform.localScale = new Vector3(15f, 10f, 8f);
            mountain.GetComponent<Renderer>().sharedMaterial = MakeColorMat("Mat_Montana", new Color(0.35f, 0.3f, 0.45f));
        }

        static GameObject CreatePlayer()
        {
            var player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            player.name = "Jugador";
            player.tag = "Player";
            player.transform.position = new Vector3(0f, 1f, -3f);

            Object.DestroyImmediate(player.GetComponent<CapsuleCollider>());
            var cc = player.AddComponent<CharacterController>();
            cc.height = 2f;
            cc.radius = 0.4f;

            player.AddComponent<PlayerController>();
            player.AddComponent<AnimalTransformationController>();

            var camGo = new GameObject("CamaraJugador");
            camGo.transform.SetParent(player.transform);
            camGo.transform.localPosition = new Vector3(0f, 2.5f, -5f);
            camGo.transform.localRotation = Quaternion.Euler(20f, 0f, 0f);
            var cam = camGo.AddComponent<Camera>();
            cam.tag = "MainCamera";
            Object.DestroyImmediate(GameObject.Find("Main Camera"));

            return player;
        }

        static void CreateSystems()
        {
            var systems = new GameObject("_AYNI_Systems");
            systems.AddComponent<GameManager>();
            systems.AddComponent<AyniBalanceSystem>();
            systems.AddComponent<NarratorSystem>();
            systems.AddComponent<MoralChoiceSystem>();
            systems.AddComponent<StorySequence>();
        }

        static GameObject CreateUI()
        {
            var canvasGo = new GameObject("AYNI_Canvas");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGo.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasGo.AddComponent<GraphicRaycaster>();

            var dialoguePanel = CreatePanel(canvasGo.transform, "DialoguePanel",
                new Vector2(0.05f, 0.02f), new Vector2(0.95f, 0.22f),
                new Color(0.05f, 0.08f, 0.12f, 0.85f));

            var speaker = CreateText(dialoguePanel.transform, "SpeakerText", "Cuy Sabio",
                22, TextAnchor.UpperLeft, new Vector2(10, -10), new Vector2(0, 1), new Vector2(1, 1),
                new Vector2(0, 1), new Vector2(-10, -40));
            speaker.fontStyle = FontStyle.Bold;
            speaker.color = new Color(0.9f, 0.75f, 0.3f);

            var dialogue = CreateText(dialoguePanel.transform, "DialogueText", "",
                18, TextAnchor.UpperLeft, new Vector2(10, 10), new Vector2(0, 0), new Vector2(1, 1),
                new Vector2(0, 0), new Vector2(-10, -10));
            dialogue.color = Color.white;

            var choicePanel = CreatePanel(canvasGo.transform, "ChoicePanel",
                new Vector2(0.15f, 0.25f), new Vector2(0.85f, 0.75f),
                new Color(0.08f, 0.1f, 0.15f, 0.92f));

            CreateText(choicePanel.transform, "ChoiceTitle", "Decisión moral",
                24, TextAnchor.UpperCenter, Vector2.zero, new Vector2(0, 0.85f), new Vector2(1, 1),
                new Vector2(0.5f, 1), new Vector2(0, -10)).fontStyle = FontStyle.Bold;

            var prompt = CreateText(choicePanel.transform, "PromptText", "",
                20, TextAnchor.UpperCenter, Vector2.zero, new Vector2(0.05f, 0.55f), new Vector2(0.95f, 0.85f),
                new Vector2(0.5f, 1), Vector2.zero);

            var context = CreateText(choicePanel.transform, "ContextText", "",
                16, TextAnchor.UpperCenter, Vector2.zero, new Vector2(0.05f, 0.4f), new Vector2(0.95f, 0.55f),
                new Vector2(0.5f, 1), Vector2.zero);
            context.fontStyle = FontStyle.Italic;
            context.color = new Color(0.75f, 0.8f, 0.85f);

            var buttons = new Button[3];
            var labels = new Text[3];
            for (int i = 0; i < 3; i++)
            {
                float yMin = 0.28f - i * 0.1f;
                var btnGo = CreatePanel(choicePanel.transform, $"Option{i}", 
                    new Vector2(0.1f, yMin - 0.08f), new Vector2(0.9f, yMin),
                    new Color(0.15f, 0.35f, 0.25f, 1f));
                buttons[i] = btnGo.AddComponent<Button>();
                var colors = buttons[i].colors;
                colors.highlightedColor = new Color(0.25f, 0.55f, 0.35f);
                buttons[i].colors = colors;

                labels[i] = CreateText(btnGo.transform, "Label", $"Opción {i + 1}",
                    16, TextAnchor.MiddleCenter, Vector2.zero, Vector2.zero, Vector2.one,
                    new Vector2(0.5f, 0.5f), Vector2.zero);
            }

            var hudPanel = CreatePanel(canvasGo.transform, "HUDPanel",
                new Vector2(0.02f, 0.78f), new Vector2(0.35f, 0.98f),
                new Color(0.05f, 0.08f, 0.1f, 0.7f));

            CreateText(hudPanel.transform, "Title", "AYNI",
                28, TextAnchor.UpperLeft, new Vector2(10, -5), new Vector2(0, 1), new Vector2(1, 1),
                new Vector2(0, 1), new Vector2(0, -35)).fontStyle = FontStyle.Bold;

            var formText = CreateText(hudPanel.transform, "FormText", "Joven — Equilibrio",
                14, TextAnchor.UpperLeft, new Vector2(10, -40), new Vector2(0, 1), new Vector2(1, 1),
                new Vector2(0, 1), new Vector2(0, -60));

            var stateText = CreateText(hudPanel.transform, "StateText", "",
                12, TextAnchor.UpperLeft, new Vector2(10, -65), new Vector2(0, 1), new Vector2(1, 1),
                new Vector2(0, 1), new Vector2(0, -85));

            var recipBar = CreateSlider(hudPanel.transform, "Reciprocidad", new Vector2(10, -95), "Ayni");
            var equiBar = CreateSlider(hudPanel.transform, "Equilibrio", new Vector2(10, -120), "Equilibrio");
            var pachaBar = CreateSlider(hudPanel.transform, "Pachamama", new Vector2(10, -145), "Pachamama");

            var controls = CreateText(hudPanel.transform, "Controls", "",
                11, TextAnchor.LowerLeft, new Vector2(10, 5), new Vector2(0, 0), new Vector2(1, 0.15f),
                new Vector2(0, 0), Vector2.zero);

            canvasGo.AddComponent<DialogueUI>();
            var choiceUI = canvasGo.AddComponent<ChoiceUI>();
            var hud = canvasGo.AddComponent<AyniHUD>();

            SetPrivateField(canvasGo.GetComponent<DialogueUI>(), "panel", dialoguePanel);
            SetPrivateField(canvasGo.GetComponent<DialogueUI>(), "speakerText", speaker);
            SetPrivateField(canvasGo.GetComponent<DialogueUI>(), "dialogueText", dialogue);

            SetPrivateField(choiceUI, "panel", choicePanel);
            SetPrivateField(choiceUI, "promptText", prompt);
            SetPrivateField(choiceUI, "contextText", context);
            SetPrivateField(choiceUI, "optionButtons", buttons);
            SetPrivateField(choiceUI, "optionLabels", labels);

            SetPrivateField(hud, "reciprocidadBar", recipBar);
            SetPrivateField(hud, "equilibrioBar", equiBar);
            SetPrivateField(hud, "pachamamaBar", pachaBar);
            SetPrivateField(hud, "formText", formText);
            SetPrivateField(hud, "stateText", stateText);
            SetPrivateField(hud, "controlsText", controls);

            return canvasGo;
        }

        static void WireUI(GameObject canvas) { }

        static void CreateWorldObjects()
        {
            var offering = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            offering.name = "Ofrenda_Coca";
            offering.transform.position = new Vector3(-5f, 0.5f, 3f);
            offering.transform.localScale = Vector3.one * 0.6f;
            offering.GetComponent<Renderer>().sharedMaterial = MakeColorMat("Mat_Ofrenda", new Color(0.3f, 0.7f, 0.3f));
            var col = offering.GetComponent<SphereCollider>();
            col.isTrigger = true;
            offering.AddComponent<PachamamaOffering>();

            var zone = new GameObject("Zona_Manantial");
            zone.transform.position = new Vector3(0f, 1f, 8f);
            var box = zone.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.size = new Vector3(6f, 3f, 6f);
            var explore = zone.AddComponent<ExplorationZone>();
            SetPrivateField(explore, "zoneName", "Manantial Sagrado");
            SetPrivateField(explore, "discoveryLine",
                "El manantial canta bajito. Aquí empezó tu desequilibrio, mijo.");

            var altarZone = new GameObject("Zona_Altar");
            altarZone.transform.position = new Vector3(12f, 1f, -5f);
            var altarBox = altarZone.AddComponent<BoxCollider>();
            altarBox.isTrigger = true;
            altarBox.size = new Vector3(5f, 3f, 5f);
            var altarExplore = altarZone.AddComponent<ExplorationZone>();
            SetPrivateField(altarExplore, "zoneName", "Altar del Cóndor");
            SetPrivateField(altarExplore, "discoveryLine",
                "Piedras antiguas, plumas en el viento. El cóndor te observa desde lo alto.");
        }

        static GameObject CreatePanel(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            var img = go.AddComponent<Image>();
            img.color = color;
            return go;
        }

        static Text CreateText(Transform parent, string name, string content, int size,
            TextAnchor align, Vector2 offsetMin, Vector2 anchorMin, Vector2 anchorMax,
            Vector2 pivot, Vector2 offsetMax)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
            var text = go.AddComponent<Text>();
            text.text = content;
            text.fontSize = size;
            text.alignment = align;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            return text;
        }

        static Slider CreateSlider(Transform parent, string name, Vector2 offset, string label)
        {
            var labelGo = new GameObject(name + "_Label");
            labelGo.transform.SetParent(parent, false);
            var labelRect = labelGo.AddComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0, 1);
            labelRect.anchorMax = new Vector2(1, 1);
            labelRect.pivot = new Vector2(0, 1);
            labelRect.anchoredPosition = offset;
            labelRect.sizeDelta = new Vector2(-20, 18);
            var labelText = labelGo.AddComponent<Text>();
            labelText.text = label;
            labelText.fontSize = 11;
            labelText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 1);
            rect.anchorMax = new Vector2(1, 1);
            rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = offset + new Vector2(0, -16);
            rect.sizeDelta = new Vector2(-20, 12);

            var bg = go.AddComponent<Image>();
            bg.color = new Color(0.2f, 0.2f, 0.2f);

            var fillArea = new GameObject("Fill Area");
            fillArea.transform.SetParent(go.transform, false);
            var fillRect = fillArea.AddComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = new Vector2(2, 2);
            fillRect.offsetMax = new Vector2(-2, -2);

            var fill = new GameObject("Fill");
            fill.transform.SetParent(fillArea.transform, false);
            var fillImage = fill.AddComponent<Image>();
            fillImage.color = new Color(0.3f, 0.75f, 0.4f);
            var fillR = fill.GetComponent<RectTransform>();
            fillR.anchorMin = Vector2.zero;
            fillR.anchorMax = Vector2.one;
            fillR.sizeDelta = Vector2.zero;

            var slider = go.AddComponent<Slider>();
            slider.fillRect = fillR;
            slider.value = 0.5f;
            return slider;
        }

        static Material MakeColorMat(string name, Color color)
        {
            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            mat.color = color;
            mat.name = name;
            return mat;
        }

        static void SetPrivateField(object target, string field, object value)
        {
            var type = target.GetType();
            while (type != null)
            {
                var f = type.GetField(field,
                    System.Reflection.BindingFlags.Instance |
                    System.Reflection.BindingFlags.NonPublic |
                    System.Reflection.BindingFlags.Public);
                if (f != null)
                {
                    f.SetValue(target, value);
                    return;
                }
                type = type.BaseType;
            }
        }

        static void EnsureDirectory(string path)
        {
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
        }
    }
}
#endif