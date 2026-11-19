using System.Collections.Generic;
using AYNI.Narrative;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace AYNI.Battle
{
    public class LagunaBattleManager : MonoBehaviour
    {
        [Header("Arena")]
        public float ArenaMinX = -28f;
        public float ArenaMaxX = 28f;
        public float FloorY = -2.5f;

        [Header("Audio")]
        public AudioClip bgMusic;
        public AudioClip toroAttackSound;

        [Header("UI")]
        public GameObject startScreen;
        public GameObject gameOverScreen;
        public Text gameOverText;
        public Slider playerHealthBar;
        public Slider enemyHealthBar;
        public Text playerNameText;
        public Text enemyNameText;
        public Slider ayniBar;
        public Slider espiritualidadBar;
        public Slider respetoBar;
        public Slider dineroBar;
        public Text instructionsText;

        [Header("Parallax")]
        public ParallaxLayer[] parallaxLayers;

        public bool IsPlaying { get; private set; }
        public bool IsGameOver { get; private set; }
        public bool IsNightMode { get; private set; }
        public bool CommandsOpen { get; private set; }
        public VizcachaFighter Player => player;

        VizcachaFighter player;
        ToroEnemyAI enemy;
        AyniBakedWorld bakedWorld;
        bool HasBakedWorld => bakedWorld != null;
        AudioSource musicSource;
        BattleParallaxSystem parallaxSystem;
        BattlePropSpawner propSpawner;
        StorybookScreens screens;
        CaminoEscenario camino;
        readonly List<BattleProjectile> projectiles = new();
        GameObject nightOverlay;
        bool toldCodicia;
        bool rewarded;

        float scoreAyni = 100f;
        float scoreEsp = 75f;
        float scoreResp = 80f;
        float scoreDin = 50f;
        float ambientSpawnTimer;

        void Awake()
        {
            if (PartidaDelCamino.TieneCifras)
            {
                scoreAyni = PartidaDelCamino.Ayni;
                scoreResp = PartidaDelCamino.Reputacion;
                scoreDin = PartidaDelCamino.Codicia;
            }

            player = FindFirstObjectByType<VizcachaFighter>();
            enemy = FindFirstObjectByType<ToroEnemyAI>();
            bakedWorld = FindFirstObjectByType<AyniBakedWorld>();

            // Los capítulos viven en la misma escena que la laguna. Sus raíces
            // se quedan visibles en el editor, pero al jugar arrancan ocultas
            // y solo se activan cuando se abre el capítulo correspondiente.
            HideChapterDistricts();

            if (bgMusic == null)
                bgMusic = BattleRuntimeAssets.LoadAudio("Battle/Audio/musica Laguna del Perol");
            if (toroAttackSound == null)
                toroAttackSound = BattleRuntimeAssets.LoadAudio("Battle/Audio/toro");

            musicSource = gameObject.AddComponent<AudioSource>();
            musicSource.clip = bgMusic;
            musicSource.loop = true;
            musicSource.volume = 0.4f;

            if (enemy != null)
            {
                attackSound = enemy.GetComponent<AudioSource>();
                if (attackSound == null)
                    attackSound = enemy.gameObject.AddComponent<AudioSource>();
                attackSound.clip = toroAttackSound;
                attackSound.playOnAwake = false;
                attackSound.loop = false;
                attackSound.Stop();
            }

            CreateNightOverlay();
            UpdateScoreboard();
            EnsureEventSystem();
            AutoFindUI();

            if (startScreen != null) startScreen.SetActive(true);
            if (gameOverScreen != null) gameOverScreen.SetActive(false);
            if (instructionsText != null) instructionsText.gameObject.SetActive(false);

            IsPlaying = false;
        }

        AudioSource attackSound;

        void Start()
        {
            EnsureFighterSprites();
            if (HasBakedWorld) SetupBakedEnvironment();
            else SetupEnvironment();
            WireStartButton();

            var canopy = JungleCanopy.Ensure(FloorY, ArenaMinX, ArenaMaxX);
            canopy.FirstGrab -= OnFirstVine;
            canopy.FirstGrab += OnFirstVine;
            screens = StorybookScreens.Dress(this);
            PeleaComandos.Ensure(this);
            camino = CaminoEscenario.Ensure(this);
            Jornada.Ensure(this);
            FigurasDelCamino.Ensure(this);
            OficiosDelCamino.Preparar(this);

            var cam = Camera.main;
            if (cam != null)
            {
                if (cam.GetComponent<BattleCameraFollow>() == null)
                    cam.gameObject.AddComponent<BattleCameraFollow>();
                cam.orthographicSize = 6f;
                cam.backgroundColor = new Color(0.04f, 0.11f, 0.07f);
            }
        }

        void OnDestroy()
        {
            if (JungleCanopy.Instance != null)
                JungleCanopy.Instance.FirstGrab -= OnFirstVine;
        }

        void OnFirstVine()
        {
            screens?.ShowChapter(1);
        }

        void SetupEnvironment()
        {
            parallaxSystem = GetComponent<BattleParallaxSystem>();
            if (parallaxSystem == null)
                parallaxSystem = gameObject.AddComponent<BattleParallaxSystem>();
            parallaxSystem.Build(FloorY, ArenaMaxX);

            propSpawner = GetComponent<BattlePropSpawner>();
            if (propSpawner == null)
                propSpawner = gameObject.AddComponent<BattlePropSpawner>();

            MonteDressing.Ensure(transform).BuildField(FloorY, ArenaMinX, ArenaMaxX);
            RioDeEntrada.Ensure(transform, FloorY);
            LagunaDelPerol.Pintar(transform, FloorY, ArenaMinX, ArenaMaxX);
            MinaDelMuqui.Preparar(this);
            parallaxLayers = parallaxSystem.Layers;
        }

        /// <summary>
        /// La escena ya trae el mundo construido y editado a mano.
        /// Solo conectamos los sistemas; no generamos ni sobrescribimos nada.
        /// </summary>
        static void HideChapterDistricts()
        {
            var cities = FindObjectsByType<AyniBakedCajamarca>(FindObjectsInactive.Include);
            for (int i = 0; i < cities.Length; i++)
                if (cities[i] != null) cities[i].gameObject.SetActive(false);

            var realCities = FindObjectsByType<AyniBakedCajamarcaReal>(FindObjectsInactive.Include);
            for (int i = 0; i < realCities.Length; i++)
                if (realCities[i] != null) realCities[i].gameObject.SetActive(false);

            var groves = FindObjectsByType<AyniBakedBosque>(FindObjectsInactive.Include);
            for (int i = 0; i < groves.Length; i++)
                if (groves[i] != null) groves[i].gameObject.SetActive(false);
        }

        void SetupBakedEnvironment()
        {
            parallaxSystem = GetComponent<BattleParallaxSystem>();
            if (parallaxSystem != null)
                parallaxLayers = parallaxSystem.Layers;

            propSpawner = GetComponent<BattlePropSpawner>();
            if (propSpawner == null)
                propSpawner = gameObject.AddComponent<BattlePropSpawner>();
            propSpawner.RegisterBakedPlatforms();
            LagunaDelPerol.Pintar(transform, FloorY, ArenaMinX, ArenaMaxX);
            MinaDelMuqui.Preparar(this);
        }

        void EnsureFighterSprites()
        {
            BattleSprites.Apply(player != null ? player.GetComponent<SpriteFrameAnimator>() : null, BattleCast.Vizcacha);
            if (player != null)
            {
                player.transform.localScale = new Vector3(CastScale.Vizcacha, CastScale.Vizcacha, 1f);
                var ink = player.GetComponent<SpriteRenderer>();
                if (ink != null && ink.sortingOrder < 12) ink.sortingOrder = 14;
            }
            BattleSprites.Apply(enemy != null ? enemy.GetComponent<SpriteFrameAnimator>() : null, BattleCast.Toro);
            if (enemy != null)
            {
                var ink = enemy.GetComponent<SpriteRenderer>();
                if (ink != null) ink.sortingOrder = 12;
            }
            SpawnAtoq();
            SpawnBrayan();
            SpawnCuy();
        }

        void SpawnAtoq()
        {
            if (FindFirstObjectByType<AtoqMaestro>() != null) return;

            var go = new GameObject("Atoq");
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sortingOrder = 7;
            var anim = go.AddComponent<SpriteFrameAnimator>();
            BattleSprites.Apply(anim, BattleCast.Atoq);
            go.transform.localScale = new Vector3(CastScale.Atoq, CastScale.Atoq, 1f);
            var maestro = go.AddComponent<AtoqMaestro>();
            maestro.Init(FloorY, -24f, RioDeEntrada.Left - 2.2f);
        }

        void SpawnBrayan()
        {
            if (FindFirstObjectByType<BrayanGuia>() != null) return;

            var go = new GameObject("Brayan");
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sortingOrder = 6;
            var anim = go.AddComponent<SpriteFrameAnimator>();
            BattleSprites.Apply(anim, BattleCast.Brayan);
            go.transform.localScale = new Vector3(CastScale.Brayan, CastScale.Brayan, 1f);
            var guia = go.AddComponent<BrayanGuia>();
            guia.Init(FloorY);
        }

        void SpawnCuy()
        {
            if (FindFirstObjectByType<CuyNarrador>() != null) return;
            if (BattleRuntimeAssets.LoadClip(BattleCast.Cuy.WalkFolder).Length == 0) return;

            var go = new GameObject("Cuy");
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sortingOrder = 8;
            var anim = go.AddComponent<SpriteFrameAnimator>();
            BattleSprites.Apply(anim, BattleCast.Cuy);
            go.transform.localScale = new Vector3(CastScale.Cuy, CastScale.Cuy, 1f);
            var narrador = go.AddComponent<CuyNarrador>();
            narrador.Init(FloorY);
        }

        void ShowCompanions(CharacterSheet hero)
        {
            SetCompanion<AtoqMaestro>(hero != BattleCast.Atoq);
            SetCompanion<BrayanGuia>(hero != BattleCast.Brayan);
            SetCompanion<CuyNarrador>(hero != BattleCast.Cuy);
        }

        void SetCompanion<T>(bool visible) where T : UnityEngine.Component
        {
            var companion = FindFirstObjectByType<T>();
            if (companion != null) companion.gameObject.SetActive(visible);
        }

        public void TasteAuraBerry()
        {
            scoreEsp = Mathf.Min(100f, scoreEsp + 8f);
            ReservaDelCamino.SumarVida();
            player?.Heal(4f);
            UpdateScoreboard();
        }

        public void NoteBatan()
        {
            scoreEsp = Mathf.Min(100f, scoreEsp + 6f);
            UpdateScoreboard();
        }

        public float Aura => scoreEsp;
        public float Dinero => scoreDin;

        public void SumarAura() => SumarAura(5f);

        public void SumarAura(float cantidad)
        {
            if (cantidad <= 0f) return;
            scoreEsp = Mathf.Min(100f, scoreEsp + cantidad);
            UpdateScoreboard();
        }

        public bool Cobrar(float cantidad)
        {
            if (cantidad <= 0f || scoreDin < cantidad) return false;
            scoreDin -= cantidad;
            UpdateScoreboard();
            return true;
        }

        public void Multar(float cantidad)
        {
            if (cantidad <= 0f) return;
            scoreDin = Mathf.Max(0f, scoreDin - cantidad);
            UpdateScoreboard();
        }

        public void OnCollectibleGathered(CollectibleType type, bool suma = false)
        {
            switch (type)
            {
                case CollectibleType.SpiritOrb:
                    scoreEsp = Mathf.Min(100f, scoreEsp + 3f);
                    if (suma) ReservaDelCamino.SumarVida();
                    break;
                case CollectibleType.CocaLeaf:
                    scoreAyni = Mathf.Min(100f, scoreAyni + 4f);
                    scoreResp = Mathf.Min(100f, scoreResp + 2f);
                    if (suma)
                    {
                        ReservaDelCamino.SumarAyni();
                        ReservaDelCamino.SumarReputacion();
                    }
                    break;
                case CollectibleType.GoldenFlower:
                    scoreDin = Mathf.Min(100f, scoreDin + 5f);
                    if (suma) ReservaDelCamino.SumarCodicia();
                    break;
            }
            UpdateScoreboard();
            LeafEffect.Spawn(player != null ? player.transform.position : Vector3.zero, true);
        }

        void Update()
        {
            if (!IsPlaying && !IsGameOver && startScreen != null && startScreen.activeSelf)
                return;

            if (IsGameOver && Input.GetKeyDown(KeyCode.R))
            {
                if (screens != null && screens.HoldsTheGate) return;
                RestartBattle();
                return;
            }

            if (!IsPlaying || IsGameOver) return;

            if (enemy != null && !enemy.IsPresent && player != null &&
                player.PositionX >= enemy.PositionX - ToroEnemyAI.RevealDistance)
                enemy.Appear();

            if (player != null)
            {
                var cerca = ToroEnemyAI.Cercano(player.PositionX);
                if (cerca != null) OnEnemyHealthChanged(cerca.Health);
            }

            if (!toldCodicia && player != null && enemy != null && enemy.IsPresent && enemy.IsAlive &&
                Mathf.Abs(player.PositionX - enemy.PositionX) < 8f &&
                (screens == null || !screens.IsShowing))
            {
                toldCodicia = true;
                screens?.ShowChapter(2);
            }

            UpdateProjectiles();
            SpawnAmbientObjects();
        }

        void SpawnAmbientObjects()
        {
            ambientSpawnTimer += Time.deltaTime;
            if (ambientSpawnTimer < 2.5f || player == null) return;
            ambientSpawnTimer = 0f;

            float x = player.PositionX + Random.Range(-12f, 12f);
            x = Mathf.Clamp(x, ArenaMinX, ArenaMaxX);
            float y = FloorY + Random.Range(0.5f, 4f);

            LeafEffect.Spawn(new Vector3(x, y, 0f), Random.value > 0.5f);
        }

        public void StartBattle()
        {
            IsPlaying = true;
            IsGameOver = false;
            PalabrasCartilla.Close();

            if (startScreen != null) startScreen.SetActive(false);
            screens?.ShowLives(true);
            screens?.ShowScores(true);
            if (gameOverScreen != null) gameOverScreen.SetActive(false);
            if (instructionsText != null)
            {
                instructionsText.gameObject.SetActive(true);
                CancelInvoke(nameof(HideInstructions));
                Invoke(nameof(HideInstructions), 4f);
            }

            Jornada.BeginPlay();

            // Si el mundo está horneado, las plataformas y coleccionables ya están
            // en la escena: no se regeneran para no borrar el trabajo de edición.
            if (!HasBakedWorld)
                propSpawner?.SpawnAll(FloorY, ArenaMinX, ArenaMaxX);

            var hero = FighterRoster.Current;
            var rival = FighterRoster.Rival();
            player?.Wear(hero);
            enemy?.Wear(rival);
            player?.SetCry(CastVoice.For(hero.Sheet));
            enemy?.SetCry(CastVoice.For(rival.Sheet));
            ShowCompanions(hero.Sheet);

            player?.ResetForBattle(FloorY);
            enemy?.ResetForBattle(FloorY, ArenaMaxX - 6f);
            DispersarEnemigos();
            SembrarReservas();
            DressPresence();
            if (!ToroEnemyAI.HayAlgunoPresente())
                ShowEnemyLife(false);

            if (playerNameText != null) playerNameText.text = hero.Sheet.Name + " (Tú)";
            if (enemyNameText != null)
                enemyNameText.text = rival.Sheet == BattleCast.Toro ? "Toro — Codicia" : rival.Sheet.Name;

            OnPlayerHealthChanged(100f);
            OnEnemyHealthChanged(100f);
            Contar("CAPÍTULO III · LA MONTAÑA", LeyDelMonte.MontanaEntra);

            Debug.Log("[AYNI] Capítulo I. La liana se toma en el aire.");
        }

        void EnsureEventSystem()
        {
            if (FindFirstObjectByType<EventSystem>() != null) return;
            var es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<StandaloneInputModule>();
        }

        void AutoFindUI()
        {
            var canvas = FindFirstObjectByType<Canvas>();
            if (canvas == null) return;

            startScreen ??= canvas.transform.Find("StartScreen")?.gameObject;
            gameOverScreen ??= canvas.transform.Find("GameOver")?.gameObject;
            gameOverText ??= gameOverScreen?.transform.Find("GOText")?.GetComponent<Text>();
            instructionsText ??= canvas.transform.Find("Instructions")?.GetComponent<Text>();
            playerHealthBar ??= canvas.transform.Find("PlayerHealth_Panel/Bar")?.GetComponent<Slider>();
            enemyHealthBar ??= canvas.transform.Find("EnemyHealth_Panel/Bar")?.GetComponent<Slider>();
            playerNameText ??= canvas.transform.Find("PlayerHealth_Panel/Name")?.GetComponent<Text>();
            enemyNameText ??= canvas.transform.Find("EnemyHealth_Panel/Name")?.GetComponent<Text>();
            ayniBar ??= canvas.transform.Find("AYNI/Bar")?.GetComponent<Slider>();
            espiritualidadBar ??= canvas.transform.Find("Espiritualidad/Bar")?.GetComponent<Slider>();
            respetoBar ??= canvas.transform.Find("Respeto/Bar")?.GetComponent<Slider>();
            dineroBar ??= canvas.transform.Find("Dinero/Bar")?.GetComponent<Slider>();
        }

        void WireStartButton()
        {
            if (startScreen == null) return;
            var btn = startScreen.transform.Find("Comenzar")?.GetComponent<Button>();
            if (btn == null) return;

            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() => MontanaDelPerol.Begin(this));

            var label = btn.transform.Find("Label")?.GetComponent<Text>();
            if (label != null) label.raycastTarget = false;
        }

        void HideInstructions()
        {
            if (instructionsText != null)
                instructionsText.gameObject.SetActive(false);
        }

        public void OnPlayerMoved(float playerX)
        {
            float painted = Mathf.Min(playerX, CaminoEscenario.LagoonEdge);
            parallaxSystem?.FollowPlayer(painted);
            if (parallaxLayers == null) return;
            foreach (var layer in parallaxLayers)
                layer?.FollowPlayer(painted);
        }

        public void Contar(string kicker, string cuerpo)
        {
            if (startScreen == null) return;
            startScreen.transform.parent?.GetComponent<CaminoRelato>()?.Tell(kicker, cuerpo);
        }

        public void AbrirHistoria()
        {
            if (startScreen == null) return;
            startScreen.transform.parent?.GetComponent<CaminoRelato>()?.Open(LeyDelMonte.Road, false);
        }

        public void ShowRoadLine(string kicker, string line, string speaker, float seconds = 6.5f)
        {
            screens?.ShowRoad(kicker, line, speaker, seconds);
        }

        public void NameTheDevil(bool thePlayer)
        {
            if (thePlayer)
            {
                if (playerNameText != null) playerNameText.text = "Diablito (Tú)";
                return;
            }

            if (enemyNameText != null) enemyNameText.text = "Diablito";
        }

        public void ShowEnemyLife(bool on)
        {
            if (enemyHealthBar == null) return;
            var panel = enemyHealthBar.transform.parent;
            if (panel != null) panel.gameObject.SetActive(on);
        }

        public void NoteMoneda()
        {
            scoreDin = Mathf.Min(100f, scoreDin + 2f);
            UpdateScoreboard();
        }

        void DressPresence()
        {
            DressOne(player, FighterRoster.Current.Sheet);
            DressOne(enemy, FighterRoster.Rival().Sheet);
            DressOne(FindFirstObjectByType<AtoqMaestro>(), BattleCast.Atoq);
            DressOne(FindFirstObjectByType<CuyNarrador>(), BattleCast.Cuy);
            DressOne(FindFirstObjectByType<BrayanGuia>(), BattleCast.Brayan);
        }

        void DressOne(Component who, CharacterSheet sheet)
        {
            if (who == null || sheet == null) return;
            var sombra = who.GetComponent<SombraEnElSuelo>();
            if (sombra == null) sombra = who.gameObject.AddComponent<SombraEnElSuelo>();
            sombra.Arm(FloorY);
            var paso = who.GetComponent<PasoSonido>();
            if (paso == null) paso = who.gameObject.AddComponent<PasoSonido>();
            paso.Pitch = PasoSonido.For(sheet);
        }

        public void PresentFoe(string name, float health)
        {
            if (enemyNameText != null) enemyNameText.text = name;
            OnEnemyHealthChanged(health);
        }

        public void SetCommandsOpen(bool open) => CommandsOpen = open;

        public void NoteOfrenda()
        {
            scoreEsp = Mathf.Min(100f, scoreEsp + 4f);
            scoreAyni = Mathf.Min(100f, scoreAyni + 3f);
            scoreDin = Mathf.Max(0f, scoreDin - 2f);
            UpdateScoreboard();
        }

        public void NoteReputacion()
        {
            scoreResp = Mathf.Min(100f, scoreResp + 8f);
            scoreAyni = Mathf.Min(100f, scoreAyni + 2f);
            UpdateScoreboard();
        }

        public void NoteCodicia()
        {
            scoreDin = Mathf.Min(100f, scoreDin + 8f);
            scoreAyni = Mathf.Max(0f, scoreAyni - 4f);
            UpdateScoreboard();
        }

        public void DevolverOro()
        {
            scoreDin = Mathf.Max(0f, scoreDin - 8f);
            scoreAyni = Mathf.Min(100f, scoreAyni + 4f);
            scoreResp = Mathf.Min(100f, scoreResp + 6f);
            UpdateScoreboard();
        }

        bool enemigosDispersos;
        bool reservasSembradas;

        void DispersarEnemigos()
        {
            if (enemigosDispersos || enemy == null) return;
            enemigosDispersos = true;
            float[] puestos = { 0f, 9f };
            enemy.Acotar(15.5f, ArenaMaxX - 2f);
            var rival = FighterRoster.Rival();
            for (int i = 0; i < puestos.Length; i++)
            {
                var copia = Instantiate(enemy.gameObject, enemy.transform.parent);
                copia.name = "EnemigoOrilla" + (i + 1);
                var otro = copia.GetComponent<ToroEnemyAI>();
                otro.Wear(rival);
                otro.ResetForBattle(FloorY, puestos[i]);
                otro.Acotar(puestos[i] - 3.8f, puestos[i] + 3.8f, false);
                otro.Appear(false);
                DressOne(otro, rival.Sheet);
            }
        }

        void SembrarReservas()
        {
            if (reservasSembradas) return;
            reservasSembradas = true;
            Semilla(CollectibleType.SpiritOrb, -1.5f);
            Semilla(CollectibleType.CocaLeaf, 7f);
            Semilla(CollectibleType.GoldenFlower, 17f);
        }

        void Semilla(CollectibleType tipo, float x)
        {
            var go = new GameObject("Reserva " + tipo);
            go.transform.SetParent(transform, false);
            go.AddComponent<BattleCollectible>().Init(tipo, new Vector3(x, FloorY + 1.15f, 0f), true);
        }

        public void OnPlayerHealthChanged(float health)
        {
            if (playerHealthBar != null)
                playerHealthBar.value = health / 100f;
            UpdateHealthColor(playerHealthBar, health);
        }

        public void OnEnemyHealthChanged(float health)
        {
            if (enemyHealthBar != null)
                enemyHealthBar.value = health / 100f;
            UpdateHealthColor(enemyHealthBar, health);
        }

        void UpdateProjectiles()
        {
            projectiles.RemoveAll(p => p == null);

            var all = FindObjectsByType<BattleProjectile>(FindObjectsSortMode.None);
            foreach (var proj in all)
            {
                if (proj.Type == ProjectileType.Aura && player != null && player.IsAlive)
                {
                    if (player.CheckProjectileHit(proj.transform.position))
                    {
                        player.TakeDamage(15f);
                        proj.Deactivate();
                    }
                }
                else if (proj.Type == ProjectileType.Healing)
                {
                    bool caught = enemy != null && enemy.IsAlive && enemy.CheckHealHit(proj.transform.position);
                    if (!caught)
                    {
                        var sombra = FindFirstObjectByType<SombraDelCerro>();
                        caught = sombra != null && sombra.IsAlive && sombra.CheckHealHit(proj.transform.position);
                    }
                    if (caught)
                    {
                        player?.Heal(15f);
                        proj.Deactivate();
                    }
                }
            }
        }

        public void EndBattle(bool playerWon)
        {
            if (playerWon && camino != null && camino.HoldTheRoad())
            {
                Reward(true);
                return;
            }

            IsGameOver = true;
            screens?.HideChapter();
            if (gameOverScreen != null) gameOverScreen.SetActive(true);
            screens?.OfferFinale(playerWon);
            if (gameOverText != null)
            {
                gameOverText.text = playerWon
                    ? "«" + LeyDelMonte.WinLine + "»\n— " + LeyDelMonte.Voice + "\n\nPulsa R para volver"
                    : LeyDelMonte.LossLine + "\n\nPulsa R para volver";
            }

            Reward(playerWon);
        }

        void Reward(bool playerWon)
        {
            if (playerWon)
            {
                if (rewarded) return;
                rewarded = true;
                scoreAyni = Mathf.Min(100f, scoreAyni + 10f);
                scoreResp = Mathf.Min(100f, scoreResp + 15f);
                scoreDin = Mathf.Max(0f, scoreDin - 12f);
            }
            else
            {
                scoreAyni = Mathf.Max(0f, scoreAyni - 20f);
                scoreEsp = Mathf.Max(0f, scoreEsp - 15f);
                scoreDin = Mathf.Min(100f, scoreDin + 18f);
            }
            UpdateScoreboard();
        }

        public void RestartBattle()
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
        }

        public void NoteNight(bool night) => IsNightMode = night;

        void ToggleNightMode()
        {
            IsNightMode = !IsNightMode;
            if (nightOverlay != null)
                nightOverlay.SetActive(IsNightMode);
        }

        void CreateNightOverlay()
        {
            nightOverlay = new GameObject("NightOverlay");
            var sr = nightOverlay.AddComponent<SpriteRenderer>();
            sr.color = new Color(0f, 0f, 0f, 0.75f);
            sr.sortingOrder = 100;
            var tex = new Texture2D(1, 1);
            tex.SetPixel(0, 0, Color.white);
            tex.Apply();
            sr.sprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
            nightOverlay.transform.localScale = new Vector3(40f, 25f, 1f);
            nightOverlay.transform.position = new Vector3(0f, 2f, 1f);
            nightOverlay.SetActive(false);
        }

        void UpdateScoreboard()
        {
            PartidaDelCamino.AnotarCifras(scoreAyni, scoreResp, scoreDin);
            if (ayniBar != null) ayniBar.value = scoreAyni / 100f;
            if (espiritualidadBar != null) espiritualidadBar.value = scoreEsp / 100f;
            if (respetoBar != null) respetoBar.value = scoreResp / 100f;
            if (dineroBar != null) dineroBar.value = scoreDin / 100f;
        }

        static void UpdateHealthColor(Slider bar, float health)
        {
            if (bar == null) return;
            var fill = bar.fillRect?.GetComponent<Image>();
            if (fill == null) return;
            if (health > 50f) fill.color = new Color(0.24f, 0.55f, 0.28f);
            else if (health > 20f) fill.color = new Color(0.89f, 0.69f, 0.35f);
            else fill.color = new Color(0.70f, 0.24f, 0.16f);
        }
    }
}