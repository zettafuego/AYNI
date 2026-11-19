using AYNI.Narrative;
using AYNI.UI;
using UnityEngine;
using UnityEngine.UI;

namespace AYNI.Battle
{
    /// <summary>
    /// Counts real play and turns the day into night.
    /// An original rock instrumental loops as the bed while a chapter or the fight is open.
    /// The title does not count.
    /// </summary>
    public class Jornada : MonoBehaviour
    {
        public const float NightAfter = 300f;

        static Jornada current;

        LagunaBattleManager battle;
        float played;
        float shade;
        bool previewNight;
        bool forcedDay;
        bool musicOn;
        bool booted;
        static bool musica = true;

        public static bool Musica
        {
            get => musica;
            set
            {
                musica = value;
                if (current == null) return;
                current.musicOn = current.EnJuego() && musica;
            }
        }
        AudioSource dayBed;
        AudioSource nightBed;
        SpriteRenderer veil;

        public static float Shade => current != null ? current.shade : 0f;

        float tema;
        static bool camaCallada;

        /// <summary>Mientras Cállate y Baila se oye, el rock de fondo espera.</summary>
        public static void SilenciarCama(bool callar) => camaCallada = callar;

        /// <summary>Baja el fondo cuando una canción suena al lado del héroe.</summary>
        public static void MarcarTema(bool cerca)
        {
            if (current == null) return;
            current.tema = Mathf.MoveTowards(current.tema, cerca ? 1f : 0f, Time.unscaledDeltaTime * 1.6f);
        }

        public static void Ensure(LagunaBattleManager host)
        {
            if (host == null) return;
            var found = host.GetComponent<Jornada>();
            if (found == null) found = host.gameObject.AddComponent<Jornada>();
            found.battle = host;
            found.Boot();
        }

        public static void BeginPlay()
        {
            if (current == null) return;
            current.musicOn = musica;
        }

        /// <summary>Las calles abren de día para que se lea el color de la plaza.</summary>
        public static void Amanecer()
        {
            if (current == null) return;
            current.played = 0f;
            current.shade = 0f;
            current.forcedDay = false;
            current.previewNight = false;
        }

        bool EnJuego()
        {
            bool chapter = CajamarcaCalles.IsOpen || BosqueCapitulo.IsOpen || CajamarcaRealCapitulo.IsOpen;
            bool fight = battle != null && battle.IsPlaying && !battle.IsGameOver;
            return fight || chapter;
        }

        void Awake()
        {
            current = this;
        }

        void OnDestroy()
        {
            if (current == this) current = null;
            camaCallada = false;
            if (veil != null) Destroy(veil.gameObject);
        }

        void Boot()
        {
            if (booted) return;
            booted = true;
            var song = RockDelJuego();
            dayBed = MakeBed(song);
            nightBed = MakeBed(song);
            veil = MakeVeil();
        }

        void Update()
        {
            if (battle == null) return;
            bool chapter = CajamarcaCalles.IsOpen || BosqueCapitulo.IsOpen || CajamarcaRealCapitulo.IsOpen;
            bool fight = battle.IsPlaying && !battle.IsGameOver;
            bool live = fight || chapter;
            if (live) played += Time.unscaledDeltaTime;
            musicOn = live && musica;

            if (live && Input.GetKeyDown(KeyCode.N))
            {
                if (played < NightAfter) previewNight = !previewNight;
                else forcedDay = !forcedDay;
            }

            bool wantNight = played >= NightAfter ? !forcedDay : previewNight;
            float step = Time.unscaledDeltaTime / 7f;
            shade = Mathf.MoveTowards(shade, wantNight ? 1f : 0f, step);
            battle.NoteNight(shade > 0.45f);
            Mix();
        }

        void LateUpdate()
        {
            PlaceVeil();
            if (BosqueCapitulo.IsOpen || CajamarcaCalles.IsOpen || CajamarcaRealCapitulo.IsOpen || MontanaDelPerol.IsOpen) return;
            if (battle != null && battle.IsPlaying)
                TintAir(new Color(0.72f, 0.74f, 0.68f), new Color(0.72f, 0.74f, 0.68f));
        }

        public static void TintSky(Camera cam, Color day)
        {
            if (cam == null) return;
            cam.backgroundColor = Color.Lerp(day, new Color(0.03f, 0.05f, 0.12f), Shade);
        }

        public static void TintAir(Color dayFog, Color dayAmbient)
        {
            RenderSettings.fogColor = Color.Lerp(dayFog, new Color(0.05f, 0.07f, 0.12f), Shade);
            RenderSettings.ambientLight = Color.Lerp(dayAmbient, new Color(0.12f, 0.14f, 0.22f), Shade);
        }

        void Mix()
        {
            if (dayBed == null && nightBed == null) return;
            if (!musicOn || camaCallada)
            {
                if (dayBed != null && dayBed.isPlaying) dayBed.Pause();
                if (nightBed != null && nightBed.isPlaying) nightBed.Pause();
                return;
            }

            bool same = dayBed != null && nightBed != null && dayBed.clip != null && dayBed.clip == nightBed.clip;
            const float bed = 0.5f;
            float duck = 1f - tema * 0.82f;
            float dayVolume = (same ? bed : bed * (1f - shade)) * duck;
            float nightVolume = same ? 0f : bed * shade * duck;
            SetBed(dayBed, dayVolume);
            SetBed(nightBed, nightVolume);
        }

        static void SetBed(AudioSource bed, float volume)
        {
            if (bed == null || bed.clip == null) return;
            if (volume <= 0.001f)
            {
                bed.volume = 0f;
                return;
            }

            if (!bed.isPlaying) bed.Play();
            bed.volume = volume;
        }

        // Rock propio del juego: 128, ocho compases, Am G F E. Sin letra.
        static AudioClip RockDelJuego()
        {
            const int rate = 22050;
            const float bpm = 128f;
            const float beat = 60f / bpm;
            const float bar = beat * 4f;
            const int bars = 8;
            int n = Mathf.RoundToInt(rate * bar * bars);
            var data = new float[n];
            float[] raiz = { 110f, 98f, 87.31f, 82.41f };
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)rate;
                int compas = Mathf.Min(bars - 1, Mathf.FloorToInt(t / bar));
                float raizHz = raiz[compas % 4];
                float quinta = raizHz * 1.5f;
                float octava = raizHz * 2f;
                float enPulso = t / beat;
                int pulso = Mathf.FloorToInt(enPulso);
                float uPulso = enPulso - pulso;
                float enCorchea = t / (beat * 0.5f);
                int corchea = Mathf.FloorToInt(enCorchea);
                float u = enCorchea - corchea;

                float puerta = u < 0.08f ? u / 0.08f : Mathf.Exp(-(u - 0.08f) * 4.4f);
                float guitarra = Mathf.Sin(Mathf.PI * 2f * raizHz * t)
                    + 0.72f * Mathf.Sin(Mathf.PI * 2f * quinta * t)
                    + 0.46f * Mathf.Sin(Mathf.PI * 2f * octava * t);
                guitarra = guitarra / (1f + Mathf.Abs(guitarra));
                float acento = (corchea % 2 == 0) ? 1f : 0.62f;
                if (compas >= 4 && (corchea % 2 == 1)) acento = 0.9f;
                guitarra *= puerta * acento * 0.46f;

                float bajoPuerta = Mathf.Exp(-uPulso * 2.4f);
                float bajo = Mathf.Sin(Mathf.PI * 2f * (raizHz * 0.5f) * t) * 0.62f
                    + Mathf.Sin(Mathf.PI * 2f * raizHz * t) * 0.22f;
                bajo *= bajoPuerta;

                float bombo = 0f;
                if (uPulso < 0.22f)
                {
                    float k = uPulso / 0.22f;
                    float f = Mathf.Lerp(150f, 46f, k);
                    bombo = Mathf.Sin(Mathf.PI * 2f * f * t) * (1f - k) * (1f - k);
                }

                float caja = 0f;
                int enCompas = pulso % 4;
                bool golpeCaja = enCompas == 1 || enCompas == 3;
                float cajaU = uPulso;
                if (compas == bars - 1 && enCompas == 3 && uPulso >= 0.5f && uPulso < 0.68f)
                {
                    golpeCaja = true;
                    cajaU = uPulso - 0.5f;
                }

                if (golpeCaja && cajaU < 0.16f)
                {
                    float s = 1f - cajaU / 0.16f;
                    int hash = (i * 1103515245 + 12345) & 0x7fffffff;
                    float ruido = hash / 2147483647f * 2f - 1f;
                    caja = ruido * s + Mathf.Sin(Mathf.PI * 2f * 188f * t) * s * 0.35f;
                }

                float charles = 0f;
                if (u < 0.07f)
                {
                    float h = 1f - u / 0.07f;
                    int hash = ((i + 17) * 1103515245 + 12345) & 0x7fffffff;
                    float ruido = hash / 2147483647f * 2f - 1f;
                    float abierto = (corchea % 2 == 0) ? 0.45f : 0.22f;
                    charles = ruido * h * abierto;
                }

                data[i] = guitarra + bajo * 0.72f + bombo * 0.5f + caja * 0.28f + charles * 0.16f;
            }

            float pico = 0.001f;
            for (int i = 0; i < n; i++)
            {
                float a = Mathf.Abs(data[i]);
                if (a > pico) pico = a;
            }

            float ganancia = 0.78f / pico;
            for (int i = 0; i < n; i++)
                data[i] = Mathf.Clamp(data[i] * ganancia, -1f, 1f);

            var clip = AudioClip.Create("rock-del-juego", n, 1, rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        AudioSource MakeBed(AudioClip clip)
        {
            var source = gameObject.AddComponent<AudioSource>();
            source.clip = clip;
            source.loop = true;
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            source.volume = 0f;
            source.dopplerLevel = 0f;
            return source;
        }

        SpriteRenderer MakeVeil()
        {
            var go = new GameObject("VeloDeNoche");
            var ink = go.AddComponent<SpriteRenderer>();
            var tex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            tex.hideFlags = HideFlags.HideAndDontSave;
            tex.SetPixel(0, 0, Color.white);
            tex.Apply();
            ink.sprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
            ink.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            ink.receiveShadows = false;
            ink.sortingOrder = 120;
            ink.enabled = false;
            return ink;
        }

        void PlaceVeil()
        {
            if (veil == null) return;
            var cam = Camera.main;
            bool show = shade > 0.02f && battle != null &&
                        (battle.IsPlaying || CajamarcaCalles.IsOpen || BosqueCapitulo.IsOpen || CajamarcaRealCapitulo.IsOpen);
            veil.enabled = show && cam != null;
            if (!veil.enabled) return;

            veil.transform.SetParent(cam.transform, false);
            float dist;
            float height;
            if (cam.orthographic)
            {
                dist = cam.nearClipPlane + 0.5f;
                height = cam.orthographicSize * 2.4f;
            }
            else
            {
                dist = Mathf.Max(0.35f, cam.nearClipPlane + 0.25f);
                height = 2f * dist * Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad) * 1.2f;
            }

            float width = height * Mathf.Max(0.5f, cam.aspect) * 1.15f;
            veil.transform.localPosition = new Vector3(0f, 0f, dist);
            veil.transform.localRotation = Quaternion.identity;
            veil.transform.localScale = new Vector3(width, height, 1f);
            veil.color = new Color(0.02f, 0.04f, 0.12f, 0.50f * shade);
        }
    }

    /// <summary>
    /// The parchment card that teaches the words, one at a time.
    /// </summary>
    public static class PalabrasCartilla
    {
        static GameObject root;
        static Text kind;
        static Text word;
        static Text meaning;
        static int index;

        public static bool IsOpen => root != null && root.activeSelf;

        public static void Open(Transform page)
        {
            if (page == null || LeyDelMonte.Palabras.Length == 0) return;
            if (root == null) Build(page);
            root.SetActive(true);
            root.transform.SetAsLastSibling();
            Show();
        }

        public static void Close()
        {
            if (root != null) root.SetActive(false);
        }

        public static void Next()
        {
            if (LeyDelMonte.Palabras.Length == 0) return;
            index = (index + 1) % LeyDelMonte.Palabras.Length;
            Show();
        }

        public static void Prev()
        {
            int count = LeyDelMonte.Palabras.Length;
            if (count == 0) return;
            index = (index + count - 1) % count;
            Show();
        }

        static void Show()
        {
            if (kind == null || word == null || meaning == null) return;
            var palabra = LeyDelMonte.Palabras[index];
            kind.text = palabra.Clase.ToUpperInvariant() + "   ·   " + (index + 1) + " / " + LeyDelMonte.Palabras.Length;
            word.text = palabra.Voz;
            meaning.text = palabra.Dice;
            Storybook.LeafRead(word, 42);
        }

        static void Build(Transform page)
        {
            var dim = Storybook.Plate(page, "PalabrasCartilla", Vector2.zero, Vector2.one, new Color(0.02f, 0.02f, 0.025f, 0.88f));
            dim.raycastTarget = true;
            root = dim.gameObject;

            var card = Storybook.Plate(dim.transform, "Ficha", new Vector2(0.22f, 0.16f), new Vector2(0.78f, 0.84f), Storybook.Panel);
            card.raycastTarget = false;

            kind = Add(card.transform, "Clase", new Vector2(0.08f, 0.78f), new Vector2(0.92f, 0.92f), 22, Storybook.Play);
            word = Add(card.transform, "Voz", new Vector2(0.08f, 0.50f), new Vector2(0.92f, 0.78f), 42, Color.white);
            meaning = Add(card.transform, "Dice", new Vector2(0.1f, 0.28f), new Vector2(0.9f, 0.52f), 26, Storybook.Chalk);
            var hint = Add(card.transform, "Aviso", new Vector2(0.08f, 0.22f), new Vector2(0.92f, 0.30f), 18, Storybook.Mute);
            hint.text = "W S otra palabra. Esc cierra.";

            Storybook.StickerButton(card.transform, "Otra", "OTRA",
                new Vector2(0.06f, 0.04f), new Vector2(0.46f, 0.22f), Next);
            Storybook.StickerButton(card.transform, "Cerrar", "CERRAR",
                new Vector2(0.54f, 0.04f), new Vector2(0.94f, 0.22f), Close);
        }

        static Text Add(Transform parent, string name, Vector2 min, Vector2 max, int size, Color color)
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
            return text;
        }
    }

    /// <summary>
    /// At the batán the camera comes in close. Slide the chungo from side to side.
    /// The bar fills, then the road opens. It is not a fail window.
    /// </summary>
    public class BatanFiambre : MonoBehaviour
    {
        public const float SpotX = 32f;
        public const float GateX = 35f;
        const float Need = 3.2f;
        const float SlideLeft = -0.55f;
        const float SlideRight = 0.62f;

        static BatanFiambre current;

        LagunaBattleManager battle;
        Transform chungo;
        Transform barFill;
        SpriteRenderer meal;
        SpriteRenderer[] luces;
        Color[] luzColor;
        float grind;
        float slide = 0.12f;
        bool done;
        bool told;

        public static bool BlocksWalker(float x)
        {
            return current != null && !current.done && Mathf.Abs(x - SpotX) < 3.2f;
        }

        public static void Place(LagunaBattleManager host)
        {
            if (host == null || host.GetComponentInChildren<BatanFiambre>(true) != null) return;
            var go = new GameObject("BatanFiambre");
            go.transform.SetParent(host.transform, false);
            var mill = go.AddComponent<BatanFiambre>();
            current = mill;
            mill.battle = host;
            mill.Build();
        }

        void OnDestroy()
        {
            if (current == this) current = null;
            var eye = Camera.main != null ? Camera.main.GetComponent<BattleCameraFollow>() : null;
            eye?.Release();
        }

        void Build()
        {
            var root = new GameObject("Batan");
            root.transform.SetParent(transform, false);
            root.transform.position = new Vector3(SpotX, battle.FloorY, 0f);

            var stone = SpriteOf(root.transform, "Piedra", Stone(), 12);
            stone.transform.localPosition = new Vector3(0f, 0.16f, 0f);
            stone.transform.localScale = new Vector3(1.7f, 0.7f, 1f);

            chungo = SpriteOf(root.transform, "Chungo", HandStone(), 13);
            chungo.localPosition = new Vector3(0.12f, 0.48f, -0.02f);
            chungo.localScale = Vector3.one * 0.72f;

            var cloth = SpriteOf(root.transform, "Fiambre", Bundle(), 12);
            cloth.localPosition = new Vector3(-0.95f, 0.22f, 0f);
            meal = cloth.GetComponent<SpriteRenderer>();

            luces = new SpriteRenderer[3];
            luzColor = new[]
            {
                new Color(0.45f, 1f, 0.72f, 1f),
                new Color(0.72f, 0.38f, 1f, 1f),
                new Color(1f, 0.86f, 0.42f, 1f),
            };
            Vector3[] luzAt =
            {
                new Vector3(-0.95f, 0.78f, -0.06f),
                new Vector3(0.15f, 1.05f, -0.06f),
                new Vector3(0.82f, 0.62f, -0.06f),
            };
            for (int i = 0; i < luces.Length; i++)
            {
                var luz = SpriteOf(root.transform, "LuzAura" + i, GlowOrb(), 17);
                luz.localPosition = luzAt[i];
                luz.localScale = Vector3.one * 0.28f;
                luces[i] = luz.GetComponent<SpriteRenderer>();
            }

            var back = SpriteOf(root.transform, "Barra", White(), 15);
            back.localPosition = new Vector3(0f, 1.55f, -0.04f);
            back.localScale = new Vector3(1.7f, 0.16f, 1f);
            back.GetComponent<SpriteRenderer>().color = new Color(0.10f, 0.08f, 0.05f, 0.9f);

            barFill = SpriteOf(root.transform, "Lleno", WhiteLeft(), 16);
            barFill.localPosition = new Vector3(-0.8f, 1.55f, -0.05f);
            barFill.localScale = new Vector3(0.02f, 0.1f, 1f);
            barFill.GetComponent<SpriteRenderer>().color = new Color(0.55f, 0.72f, 0.22f, 1f);
        }

        void Update()
        {
            if (done || battle == null || !battle.IsPlaying || battle.IsGameOver) return;
            var player = battle.Player;
            if (player == null || !player.IsAlive) return;

            bool near = Mathf.Abs(player.PositionX - SpotX) < 3.2f;
            if (near && !told)
            {
                told = true;
                var eye = Camera.main != null ? Camera.main.GetComponent<BattleCameraFollow>() : null;
                eye?.Focus(SpotX, battle.FloorY + 0.62f, 1.8f);
                var cuy = FindFirstObjectByType<CuyNarrador>();
                if (cuy != null && cuy.gameObject.activeInHierarchy)
                    cuy.ComeFrom(SpotX, SpotX - 1.65f, battle.FloorY);
                battle.ShowRoadLine("EL BATÁN", LeyDelMonte.BatanAsk, LeyDelMonte.Voice, 14f);
            }

            if (near)
            {
                float dir = Input.GetAxisRaw("Horizontal");
                if (Mathf.Abs(dir) > 0.01f)
                {
                    float next = Mathf.Clamp(slide + dir * 1.35f * Time.deltaTime, SlideLeft, SlideRight);
                    grind = Mathf.Min(Need, grind + Mathf.Abs(next - slide));
                    slide = next;
                }
            }

            if (chungo != null)
            {
                chungo.localPosition = new Vector3(slide, 0.48f, -0.02f);
                chungo.localRotation = Quaternion.Euler(0f, 0f, -slide * 22f);
            }

            if (barFill != null)
                barFill.localScale = new Vector3(Mathf.Max(0.02f, 1.6f * (grind / Need)), 0.1f, 1f);

            PaintMill();
            if (grind >= Need) Finish();
        }

        void PaintMill()
        {
            float g = done ? 1f : Mathf.Clamp01(grind / Need);
            if (meal != null)
                meal.color = Color.Lerp(new Color(0.55f, 0.42f, 0.32f, 1f), new Color(1f, 0.72f, 0.28f, 1f), g);
            if (luces == null) return;
            for (int i = 0; i < luces.Length; i++)
            {
                if (luces[i] == null) continue;
                float pulse = 0.82f + 0.18f * Mathf.Sin(Time.time * 3.2f + i);
                float size = Mathf.Lerp(0.22f, 0.95f, g) * pulse;
                luces[i].transform.localScale = Vector3.one * size;
                var ink = luzColor[i];
                ink.a = Mathf.Lerp(0.12f, 0.95f, g);
                luces[i].color = ink;
            }
        }

        void Finish()
        {
            done = true;
            var eye = Camera.main != null ? Camera.main.GetComponent<BattleCameraFollow>() : null;
            eye?.Release();
            battle.ArenaMaxX = CaminoEscenario.RoadEnd;
            battle.ShowRoadLine("EL BATÁN", LeyDelMonte.BatanDone, LeyDelMonte.Voice, 6f);
            battle.NoteBatan();
            PaintMill();
            if (chungo != null) chungo.localRotation = Quaternion.identity;
            var cuy = FindFirstObjectByType<CuyNarrador>();
            if (cuy != null) cuy.LetGo();
            LeafEffect.Spawn(new Vector3(SpotX, battle.FloorY + 0.8f, 0f), true);
        }

        static Transform SpriteOf(Transform parent, string name, Sprite sprite, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var ink = go.AddComponent<SpriteRenderer>();
            ink.sprite = sprite;
            ink.sortingOrder = order;
            return go.transform;
        }

        static Sprite GlowOrb()
        {
            const int s = 32;
            var tex = new Texture2D(s, s, TextureFormat.RGBA32, false);
            tex.hideFlags = HideFlags.HideAndDontSave;
            float c = (s - 1) * 0.5f;
            for (int y = 0; y < s; y++)
            for (int x = 0; x < s; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), new Vector2(c, c)) / c;
                float a = Mathf.Clamp01(1f - d);
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a * a));
            }

            tex.Apply();
            tex.filterMode = FilterMode.Bilinear;
            var sprite = Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), 32f);
            sprite.hideFlags = HideFlags.HideAndDontSave;
            return sprite;
        }

        static Sprite White()
        {
            return Block(8, 8, new Vector2(0.5f, 0.5f), Color.white);
        }

        static Sprite WhiteLeft()
        {
            return Block(8, 8, new Vector2(0f, 0.5f), Color.white);
        }

        static Sprite Block(int w, int h, Vector2 pivot, Color color)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.hideFlags = HideFlags.HideAndDontSave;
            var pixels = new Color[w * h];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = color;
            tex.SetPixels(pixels);
            tex.Apply();
            tex.filterMode = FilterMode.Point;
            var sprite = Sprite.Create(tex, new Rect(0, 0, w, h), pivot, 8f);
            sprite.hideFlags = HideFlags.HideAndDontSave;
            return sprite;
        }

        static Sprite Stone()
        {
            const int w = 96;
            const int h = 40;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.hideFlags = HideFlags.HideAndDontSave;
            var clear = new Color(0f, 0f, 0f, 0f);
            var rock = new Color(0.55f, 0.54f, 0.50f, 1f);
            var dark = new Color(0.32f, 0.31f, 0.28f, 1f);
            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                float nx = (x / (w - 1f)) * 2f - 1f;
                float ny = (y / (h - 1f)) * 2f - 1f;
                float d = nx * nx * 0.85f + ny * ny * 1.3f;
                if (d > 1f) tex.SetPixel(x, y, clear);
                else if (y < 8 || d > 0.82f) tex.SetPixel(x, y, dark);
                else tex.SetPixel(x, y, rock);
            }

            for (int x = 18; x < 78; x++) tex.SetPixel(x, 18 + (x / 17) % 3, dark);
            tex.Apply();
            tex.filterMode = FilterMode.Point;
            var sprite = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.15f), 64f);
            sprite.hideFlags = HideFlags.HideAndDontSave;
            return sprite;
        }

        static Sprite HandStone()
        {
            const int s = 36;
            var tex = new Texture2D(s, s, TextureFormat.RGBA32, false);
            tex.hideFlags = HideFlags.HideAndDontSave;
            var clear = new Color(0f, 0f, 0f, 0f);
            var rock = new Color(0.40f, 0.36f, 0.30f, 1f);
            var light = new Color(0.68f, 0.62f, 0.50f, 1f);
            float c = (s - 1) * 0.5f;
            for (int y = 0; y < s; y++)
            for (int x = 0; x < s; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), new Vector2(c, c - 1f));
                if (d > 15f) tex.SetPixel(x, y, clear);
                else tex.SetPixel(x, y, y > 20 ? light : rock);
            }

            tex.Apply();
            tex.filterMode = FilterMode.Point;
            var sprite = Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.2f), 48f);
            sprite.hideFlags = HideFlags.HideAndDontSave;
            return sprite;
        }

        static Sprite Bundle()
        {
            const int w = 40;
            const int h = 28;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.hideFlags = HideFlags.HideAndDontSave;
            var clear = new Color(0f, 0f, 0f, 0f);
            var cloth = new Color(0.70f, 0.28f, 0.16f, 1f);
            var knot = new Color(0.86f, 0.72f, 0.28f, 1f);
            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                bool bag = x > 4 && x < 36 && y > 3 && y < 22;
                bool tie = x > 16 && x < 24 && y > 14 && y < 26;
                if (tie) tex.SetPixel(x, y, knot);
                else if (bag) tex.SetPixel(x, y, cloth);
                else tex.SetPixel(x, y, clear);
            }

            tex.Apply();
            tex.filterMode = FilterMode.Point;
            var sprite = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.1f), 48f);
            sprite.hideFlags = HideFlags.HideAndDontSave;
            return sprite;
        }
    }
}
