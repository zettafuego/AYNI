#if UNITY_EDITOR
using System.IO;
using System.Linq;
using AYNI.Battle;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace AYNI.Editor
{
    public static class LagunaBattleBootstrap
    {
        const string ScenePath = "Assets/Scenes/AYNI_LagunaPelea.unity";
        const string BattleRoot = "Assets/Battle";

        [MenuItem("AYNI/Crear Escena Pelea - Laguna del Perol")]
        public static void CreateBattleScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            SetupCamera();
            var floor = -2.5f;

            const string castRoot = "Assets/Resources/Battle/Personajes";
            var player = CreateFighter("Vizcacha",
                castRoot + "/Vizcacha/Caminar", castRoot + "/Vizcacha/Ataque",
                1.2f, 10, typeof(VizcachaFighter));

            var enemy = CreateFighter("Toro",
                castRoot + "/Toro/Caminar", castRoot + "/Toro/Ataque",
                2.5f, 5, typeof(ToroEnemyAI));

            CreateGround(floor);

            var canvas = CreateBattleUI();
            EnsureEventSystem();
            var manager = CreateManager(canvas);

            player.GetComponent<VizcachaFighter>();
            enemy.GetComponent<ToroEnemyAI>();

            Selection.activeGameObject = manager;

            EnsureDir("Assets/Scenes");
            EditorSceneManager.SaveScene(scene, ScenePath);

            var scenes = new EditorBuildSettingsScene[]
            {
                new EditorBuildSettingsScene(ScenePath, true)
            };
            var existing = EditorBuildSettings.scenes.ToList();
            if (!existing.Any(s => s.path == ScenePath))
            {
                existing.Add(scenes[0]);
                EditorBuildSettings.scenes = existing.ToArray();
            }

            AssetDatabase.Refresh();
            Debug.Log("[AYNI] Escena de pelea creada: " + ScenePath);
            EditorUtility.DisplayDialog("AYNI — La ley del monte",
                "Escena de pelea creada.\n\nPulsa Play y luego ENTRAR AL MONTE.\n\n" +
                "A D caminar | Espacio saltar y soltar la liana | F patada | R curar | N noche",
                "Entrar");
        }

        static void SetupCamera()
        {
            var camGo = new GameObject("Main Camera");
            var cam = camGo.AddComponent<Camera>();
            cam.tag = "MainCamera";
            cam.orthographic = true;
            cam.orthographicSize = 6f;
            cam.backgroundColor = new Color(0.04f, 0.11f, 0.07f);
            cam.transform.position = new Vector3(0f, 1f, -10f);
            camGo.AddComponent<AudioListener>();
            camGo.AddComponent<BattleCameraFollow>();
        }

        static void CreateGround(float y)
        {
            var ground = new GameObject("Suelo");
            ground.transform.position = new Vector3(0f, y - 0.6f, 2f);
            ground.transform.localScale = new Vector3(60f, 0.3f, 1f);
            var sr = ground.AddComponent<SpriteRenderer>();
            sr.color = new Color(0.03f, 0.06f, 0.19f);
            var tex = new Texture2D(1, 1);
            tex.SetPixel(0, 0, Color.white);
            tex.Apply();
            sr.sprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f));
            sr.sortingOrder = 3;
        }

        static GameObject CreateFighter(string name, string walkFolder, string actionFolder,
            float scale, int sortOrder, System.Type component)
        {
            var go = new GameObject(name);
            go.transform.position = new Vector3(name == "Vizcacha" ? -6f : 6f, -2f, 0f);
            go.transform.localScale = Vector3.one * scale;

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sortingOrder = sortOrder;

            var anim = go.AddComponent<SpriteFrameAnimator>();
            SetField(anim, "walkFrames", LoadClip(walkFolder));
            if (!string.IsNullOrEmpty(actionFolder))
                SetField(anim, "actionFrames", LoadClip(actionFolder));

            go.AddComponent(component);
            go.AddComponent<FighterSpriteSetup>();
            return go;
        }

        static void EnsureEventSystem()
        {
            if (Object.FindFirstObjectByType<EventSystem>() != null) return;
            var es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<StandaloneInputModule>();
        }

        static GameObject CreateManager(GameObject canvas)
        {
            var go = new GameObject("_LagunaBattleManager");
            var mgr = go.AddComponent<LagunaBattleManager>();
            go.AddComponent<BattleParallaxSystem>();
            go.AddComponent<BattlePropSpawner>();

            var bgMusic = LoadAudio(BattleRoot + "/Audio/musica Laguna del Perol.mp3");
            var toroSnd = LoadAudio(BattleRoot + "/Audio/toro.mp3");

            SetField(mgr, "bgMusic", bgMusic);
            SetField(mgr, "toroAttackSound", toroSnd);
            SetField(mgr, "FloorY", -2.5f);
            SetField(mgr, "ArenaMinX", -28f);
            SetField(mgr, "ArenaMaxX", 28f);

            WireUI(mgr, canvas);
            return go;
        }

        static GameObject CreateBattleUI()
        {
            var canvasGo = new GameObject("BattleCanvas");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGo.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasGo.GetComponent<CanvasScaler>().referenceResolution = new Vector2(1920, 1080);
            canvasGo.AddComponent<GraphicRaycaster>();

            // Start screen
            var start = CreatePanel(canvasGo.transform, "StartScreen", Vector2.zero, Vector2.one,
                new Color(0.027f, 0.078f, 0.047f, 0.55f));
            CreateText(start.transform, "Title", "AYNI\nLA LEY DEL MONTE", 42, TextAnchor.MiddleCenter,
                new Vector2(0.2f, 0.62f), new Vector2(0.8f, 0.84f), new Color(0.165f, 0.129f, 0.086f), FontStyle.Bold);
            CreateText(start.transform, "Story",
                "Capítulo I     EL CACHORRO\n\n" +
                "«El monte no es un camino. Es una familia.»\n\n— El Atoq",
                22, TextAnchor.MiddleCenter, new Vector2(0.2f, 0.32f), new Vector2(0.8f, 0.58f),
                new Color(0.165f, 0.129f, 0.086f), FontStyle.Normal);
            CreateText(start.transform, "Controls",
                "A D caminar  ·  Espacio saltar y soltar la liana  ·  F patada  ·  R curar",
                16, TextAnchor.MiddleCenter, new Vector2(0.18f, 0.2f), new Vector2(0.82f, 0.31f),
                new Color(0.165f, 0.129f, 0.086f), FontStyle.Italic);

            var begin = CreateButton(start.transform, "Comenzar", new Vector2(0.35f, 0.08f), new Vector2(0.65f, 0.18f));
            var beginLabel = begin.transform.Find("Label")?.GetComponent<Text>();
            if (beginLabel != null)
            {
                beginLabel.text = "ENTRAR AL MONTE";
                beginLabel.color = new Color(0.165f, 0.129f, 0.086f);
            }
            var beginImage = begin.GetComponent<Image>();
            if (beginImage != null) beginImage.color = new Color(0.894f, 0.694f, 0.353f);

            // VS health bars
            CreateHealthBar(canvasGo.transform, "PlayerHealth", true);
            CreateHealthBar(canvasGo.transform, "EnemyHealth", false);

            CreateScoreSlider(canvasGo.transform, "AYNI", new Vector2(0.02f, 0.88f));
            CreateScoreSlider(canvasGo.transform, "Espiritualidad", new Vector2(0.02f, 0.82f));
            CreateScoreSlider(canvasGo.transform, "Respeto", new Vector2(0.02f, 0.76f));
            CreateScoreSlider(canvasGo.transform, "Dinero", new Vector2(0.02f, 0.70f));
            var fruto = canvasGo.transform.Find("Dinero/Lbl")?.GetComponent<Text>();
            if (fruto != null) fruto.text = "FRUTO";

            // Game over
            var gameOver = CreatePanel(canvasGo.transform, "GameOver", Vector2.zero, Vector2.one,
                new Color(0f, 0f, 0f, 0.85f));
            gameOver.SetActive(false);
            var goText = CreateText(gameOver.transform, "GOText", "", 36, TextAnchor.MiddleCenter,
                new Vector2(0.1f, 0.3f), new Vector2(0.9f, 0.7f), Color.white, FontStyle.Bold);

            CreateText(canvasGo.transform, "Instructions",
                "Espacio en el aire, junto a la liana, para colgarte. Otra vez para soltarte.",
                15, TextAnchor.LowerCenter, new Vector2(0.1f, 0.01f), new Vector2(0.9f, 0.06f),
                new Color(0.953f, 0.902f, 0.769f), FontStyle.Italic);

            canvasGo.SetActive(true);
            return canvasGo;
        }

        static void WireUI(LagunaBattleManager mgr, GameObject canvas)
        {
            var start = canvas.transform.Find("StartScreen")?.gameObject;
            var gameOver = canvas.transform.Find("GameOver")?.gameObject;
            var goText = gameOver?.transform.Find("GOText")?.GetComponent<Text>();
            var instr = canvas.transform.Find("Instructions")?.GetComponent<Text>();

            SetField(mgr, "startScreen", start);
            SetField(mgr, "gameOverScreen", gameOver);
            SetField(mgr, "gameOverText", goText);
            SetField(mgr, "instructionsText", instr);

            SetField(mgr, "playerHealthBar", canvas.transform.Find("PlayerHealth_Panel/Bar")?.GetComponent<Slider>());
            SetField(mgr, "enemyHealthBar", canvas.transform.Find("EnemyHealth_Panel/Bar")?.GetComponent<Slider>());
            SetField(mgr, "playerNameText", canvas.transform.Find("PlayerHealth_Panel/Name")?.GetComponent<Text>());
            SetField(mgr, "enemyNameText", canvas.transform.Find("EnemyHealth_Panel/Name")?.GetComponent<Text>());
            SetField(mgr, "ayniBar", canvas.transform.Find("AYNI/Bar")?.GetComponent<Slider>());
            SetField(mgr, "espiritualidadBar", canvas.transform.Find("Espiritualidad/Bar")?.GetComponent<Slider>());
            SetField(mgr, "respetoBar", canvas.transform.Find("Respeto/Bar")?.GetComponent<Slider>());
            SetField(mgr, "dineroBar", canvas.transform.Find("Dinero/Bar")?.GetComponent<Slider>());

            var startBtn = start?.transform.Find("Comenzar")?.GetComponent<Button>();
            if (startBtn != null)
            {
                startBtn.onClick.RemoveAllListeners();
                startBtn.onClick.AddListener(mgr.StartBattle);
            }
        }

        static GameObject CreatePanel(Transform parent, string name, Vector2 min, Vector2 max, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            go.AddComponent<Image>().color = color;
            return go;
        }

        static Text CreateText(Transform parent, string name, string content, int size,
            TextAnchor align, Vector2 anchorMin, Vector2 anchorMax, Color color, FontStyle style)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            var t = go.AddComponent<Text>();
            t.text = content;
            t.fontSize = size;
            t.alignment = align;
            t.color = color;
            t.fontStyle = style;
            t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            return t;
        }

        static Button CreateButton(Transform parent, string label, Vector2 anchorMin, Vector2 anchorMax)
        {
            var go = new GameObject(label);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            go.AddComponent<Image>().color = new Color(0.2f, 0.55f, 0.3f);
            var btn = go.AddComponent<Button>();
            var labelText = CreateText(go.transform, "Label", label, 22, TextAnchor.MiddleCenter,
                Vector2.zero, Vector2.one, Color.white, FontStyle.Bold);
            labelText.raycastTarget = false;
            return btn;
        }

        static (Slider slider, Text nameText) CreateHealthBar(Transform parent, string id, bool left)
        {
            var panel = new GameObject(id + "_Panel");
            panel.transform.SetParent(parent, false);
            var rect = panel.AddComponent<RectTransform>();
            rect.anchorMin = left ? new Vector2(0.03f, 0.9f) : new Vector2(0.72f, 0.9f);
            rect.anchorMax = left ? new Vector2(0.35f, 0.97f) : new Vector2(0.97f, 0.97f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var nameText = CreateText(panel.transform, "Name",
                left ? "Vizcacha (Tú)" : "Toro — Codicia",
                18, left ? TextAnchor.UpperLeft : TextAnchor.UpperRight,
                new Vector2(0f, 0.45f), Vector2.one, Color.white, FontStyle.Bold);

            var slider = CreateSlider(panel.transform, "Bar", new Vector2(0f, 0f), new Vector2(1f, 0.4f));
            slider.value = 1f;
            return (slider, nameText);
        }

        static Slider CreateScoreSlider(Transform parent, string label, Vector2 anchorPos)
        {
            var go = new GameObject(label);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = anchorPos;
            rect.anchorMax = anchorPos + new Vector2(0.25f, 0.04f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            CreateText(go.transform, "Lbl", label, 11, TextAnchor.MiddleLeft,
                Vector2.zero, new Vector2(0.4f, 1f), Color.white, FontStyle.Normal);

            return CreateSlider(go.transform, "Bar", new Vector2(0.42f, 0.1f), new Vector2(1f, 0.9f));
        }

        static Slider CreateSlider(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            go.AddComponent<Image>().color = new Color(0.15f, 0.15f, 0.15f);

            var fillArea = new GameObject("Fill");
            fillArea.transform.SetParent(go.transform, false);
            var fillRect = fillArea.AddComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = new Vector2(2, 2);
            fillRect.offsetMax = new Vector2(-2, -2);
            fillArea.AddComponent<Image>().color = Color.green;

            var slider = go.AddComponent<Slider>();
            slider.fillRect = fillRect;
            slider.value = 0.75f;
            return slider;
        }

        static Sprite LoadSprite(string assetPath)
        {
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
            if (tex == null) return null;
            return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0f), 100f);
        }

        static Sprite[] LoadClip(string folder)
        {
            var sprites = new System.Collections.Generic.List<Sprite>();
            if (string.IsNullOrEmpty(folder) || !Directory.Exists(folder)) return sprites.ToArray();

            var files = Directory.GetFiles(folder, "*.png");
            System.Array.Sort(files, (a, b) =>
                BattleRuntimeAssets.FrameNumber(Path.GetFileNameWithoutExtension(a))
                    .CompareTo(BattleRuntimeAssets.FrameNumber(Path.GetFileNameWithoutExtension(b))));

            foreach (var file in files)
            {
                var assetPath = file.Replace('\\', '/');
                int idx = assetPath.IndexOf("Assets/");
                if (idx >= 0) assetPath = assetPath.Substring(idx);
                var spr = LoadSprite(assetPath);
                if (spr != null) sprites.Add(spr);
            }
            return sprites.ToArray();
        }

        static AudioClip LoadAudio(string path)
        {
            return AssetDatabase.LoadAssetAtPath<AudioClip>(path);
        }

        static void SetField(object target, string field, object value)
        {
            var type = target.GetType();
            while (type != null)
            {
                var f = type.GetField(field,
                    System.Reflection.BindingFlags.Instance |
                    System.Reflection.BindingFlags.NonPublic |
                    System.Reflection.BindingFlags.Public);
                if (f != null) { f.SetValue(target, value); return; }
                type = type.BaseType;
            }
        }

        static void EnsureDir(string path)
        {
            if (!Directory.Exists(path)) Directory.CreateDirectory(path);
        }
    }
}
#endif