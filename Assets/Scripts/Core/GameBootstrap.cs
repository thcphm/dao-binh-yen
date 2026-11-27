// Dat 1 component nay vao scene trong la chay duoc: tu tao moi truong, dia hinh, camera, nhan vat.
// (Hoac dung menu "Serene Isle > Tao scene dao" de sinh san scene.)
// Giai doan 5: co menu chinh (saveSlot >= 0 va mainMenu = true). Menu chay tren chinh hon dao cua o luu gan nhat
// (camera bay quanh luc hoang hon); vao game tai cho neu cung dao, khac dao thi nap lai scene voi GameLaunch.
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace SereneIsle
{
    /// <summary>Yeu cau vao game (giu qua lan nap lai scene).</summary>
    public static class GameLaunch
    {
        public static bool Pending;
        public static int Slot;
        public static bool NewGame;
        public static int Seed = -1;
        public static CharacterOutfit Outfit;
        public static string Name;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { Pending = false; Outfit = null; Name = null; Seed = -1; }
    }

    [DefaultExecutionOrder(-100)]
    public class GameBootstrap : MonoBehaviour
    {
        public static GameBootstrap Instance { get; private set; }

        [Header("Dao")]
        [Tooltip("<0 = ngau nhien moi lan chay")] public int seed = -1;
        [Range(200, 1000)] public int islandSize = 800;

        [Header("Nhan vat")]
        public bool female;

        [Header("Render")]
        [Tooltip("Tat post-processing tren camera (tone mapping se lam lech mau so voi ban web).")]
        public bool disablePostProcessing = true;

        [Header("Luu game")]
        [Tooltip("O luu (0-3). < 0 = khong tai, khong luu, khong menu (test).")]
        public int saveSlot = 0;
        [Tooltip("Mo game thi tai lai o luu neu co (khi khong co menu chinh).")]
        public bool loadSave = true;
        [Tooltip("Hien menu chinh truoc khi choi (giai doan 5).")]
        public bool mainMenu = true;

        [Header("Thu nghiem")]
        [Tooltip("Cho vai mon do de thu tui do / an uong (giai doan 2).")]
        public bool starterKit = true;

        public GameSession Session { get; private set; }
        public GameHud Hud { get; private set; }
        public IslandTerrain Terrain { get; private set; }
        public PlayerController Player { get; private set; }
        public OrbitCamera CameraRig { get; private set; }
        public IslandEnvironment Environment { get; private set; }
        public bool InMenu { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { Instance = null; }

        void OnDestroy() { if (Instance == this) Instance = null; }

        void Awake()
        {
            Instance = this;
            if (!(GraphicsSettings.currentRenderPipeline is UniversalRenderPipelineAsset))
                Debug.LogWarning("[SereneIsle] Project chua dung URP: shader SereneIsle/* se khong hien. Gan URP Asset trong Project Settings > Graphics.");

            // ---- yeu cau vao game tu menu (sau khi nap lai scene) ----
            bool launch = GameLaunch.Pending;
            if (launch) { saveSlot = GameLaunch.Slot; GameLaunch.Pending = false; }
            InMenu = !launch && mainMenu && saveSlot >= 0;

            SaveData save = null;
            if (launch) save = GameLaunch.NewGame ? null : SaveSystem.Load(saveSlot);
            else if (InMenu) { int recent = SaveSystem.MostRecent(); save = recent >= 0 ? SaveSystem.Load(recent) : null; }
            else if (saveSlot >= 0 && loadSave) save = SaveSystem.Load(saveSlot);

            // ---- moi truong ----
            Environment = FindAnyObjectByType<IslandEnvironment>();
            if (!Environment) Environment = new GameObject("Environment").AddComponent<IslandEnvironment>();
            Environment.advanceTime = false; // gio do GameSession dieu khien
            if (!Environment.GetComponent<SkyRenderer>()) Environment.gameObject.AddComponent<SkyRenderer>();

            // ---- van choi (phai co truoc dia hinh de nhan vi tri leu) ----
            Session = FindAnyObjectByType<GameSession>();
            if (!Session) Session = new GameObject("GameSession").AddComponent<GameSession>();
            Session.starterKit = starterKit;
            Session.SaveSlot = InMenu ? -1 : saveSlot;
            if (!ToolSystem.Instance) Session.gameObject.AddComponent<ToolSystem>();

            // ---- dia hinh (Start chay sau Awake -> kip gan seed/size) ----
            Terrain = FindAnyObjectByType<IslandTerrain>();
            if (!Terrain)
            {
                Terrain = new GameObject("Island").AddComponent<IslandTerrain>();
                int s0 = save != null ? save.seed : launch && GameLaunch.Seed >= 0 ? GameLaunch.Seed : seed;
                Terrain.seed = s0;
                Terrain.size = save != null ? save.size : islandSize;
            }

            // ---- camera ----
            var cam = Camera.main;
            if (!cam)
            {
                var go = new GameObject("Main Camera") { tag = "MainCamera" };
                cam = go.AddComponent<Camera>();
                go.AddComponent<AudioListener>();
            }
            cam.fieldOfView = 42; cam.nearClipPlane = .1f; cam.farClipPlane = 320;
            if (disablePostProcessing)
            {
                var acd = cam.GetUniversalAdditionalCameraData();
                if (acd) acd.renderPostProcessing = false;
            }
            CameraRig = cam.GetComponent<OrbitCamera>();
            if (!CameraRig) CameraRig = cam.gameObject.AddComponent<OrbitCamera>();
            Session.Environment = Environment;

            // ---- giao dien ----
            Hud = FindAnyObjectByType<GameHud>();
            if (!Hud) Hud = new GameObject("HUD").AddComponent<GameHud>();
            if (!Hud.GetComponent<ActionUI>()) Hud.gameObject.AddComponent<ActionUI>();
            if (!Hud.GetComponent<ShopUI>()) Hud.gameObject.AddComponent<ShopUI>();
            if (!Hud.GetComponent<ApprenticeUI>()) Hud.gameObject.AddComponent<ApprenticeUI>();
            if (!Hud.GetComponent<MarketUI>()) Hud.gameObject.AddComponent<MarketUI>();
            if (!Hud.GetComponent<PauseMenu>()) Hud.gameObject.AddComponent<PauseMenu>();
            if (!Hud.GetComponent<MapUI>()) Hud.gameObject.AddComponent<MapUI>();
            if (!Hud.GetComponent<CookUI>()) Hud.gameObject.AddComponent<CookUI>();
            if (!Hud.GetComponent<JournalUI>()) Hud.gameObject.AddComponent<JournalUI>();

            // ---- lang chai: dung truoc khi tai do da dat tu file luu (nha chiem cho truoc) ----
            if (!Animals.Instance) Session.gameObject.AddComponent<Animals>();
            if (!WeatherSystem.Instance) Session.gameObject.AddComponent<WeatherSystem>();
            if (!Journal.Instance) Session.gameObject.AddComponent<Journal>();
            if (!Bottles.Instance) Session.gameObject.AddComponent<Bottles>();
            if (!Mail.Instance) Session.gameObject.AddComponent<Mail>();
            if (!Festivals.Instance) Session.gameObject.AddComponent<Festivals>();
            if (!Moments.Instance) Session.gameObject.AddComponent<Moments>();
            if (!MotherTrees.Instance) Session.gameObject.AddComponent<MotherTrees>();
            if (!GameAudio.Instance) cam.gameObject.AddComponent<GameAudio>(); // nhac + am nen + hieu ung (giai doan 6e)
            var village = Village.Instance ? Village.Instance : Session.gameObject.AddComponent<Village>();
            if (Terrain.IsReady) village.Build();
            else Terrain.Ready += _ => village.Build();

            if (Terrain.IsReady) GameSettings.Current.Apply();
            else Terrain.Ready += _ => GameSettings.Current.Apply();

            if (InMenu)
            {
                // menu chinh: chua co nhan vat; camera bay quanh dao luc hoang hon
                CameraRig.enabled = false;
                var menu = Hud.GetComponent<MainMenu>();
                if (!menu) menu = Hud.gameObject.AddComponent<MainMenu>();
                menu.BackgroundSeed = Terrain.seed;
                return;
            }

            var outfit = launch && GameLaunch.NewGame && GameLaunch.Outfit != null ? GameLaunch.Outfit : null;
            StartPlaying(saveSlot, save, outfit, launch && GameLaunch.NewGame ? GameLaunch.Name : null);
        }

        /// <summary>Tao nhan vat va vao game tren hon dao hien tai (tu Awake hoac tu menu chinh).</summary>
        public void StartPlaying(int slot, SaveData save, CharacterOutfit newOutfit, string newName)
        {
            InMenu = false;
            Session.SaveSlot = slot;
            CameraRig.enabled = true;
            if (MainMenu.Instance && MainMenu.Instance.IsOpen) MainMenu.Instance.Close();

            Player = FindAnyObjectByType<PlayerController>();
            if (!Player)
            {
                var pgo = new GameObject("Player");
                pgo.SetActive(false);
                var body = pgo.AddComponent<IslandCharacter>();
                body.outfit = newOutfit != null ? newOutfit
                            : save?.outfit != null && !string.IsNullOrEmpty(save.outfit.model) ? save.outfit
                            : female ? CharacterOutfit.Female() : CharacterOutfit.Male();
                Player = pgo.AddComponent<PlayerController>();
                pgo.SetActive(true);
            }
            Player.cameraRig = CameraRig;
            CameraRig.target = Player.transform;
            Environment.followTarget = Player.transform;
            Session.Player = Player;
            Session.Body = Player.GetComponent<IslandCharacter>();
            if (!string.IsNullOrEmpty(newName)) Session.PlayerName = newName;

            if (save != null)
            {
                SaveSystem.ApplyState(save, Session);
                // file luu loi cu (ghi ngay luc vao dao, truoc khi phat do) khong co dung cu nao -> phat lai do khoi dau
                if (save.kitGiven && save.ownedTools != null && save.ownedTools.Count > 0) Session.MarkKitGiven();
                Debug.Log($"[SereneIsle] Tai o luu {slot}: ngay {save.day}, luu luc {save.savedAt}");
            }
            else if (newOutfit != null || !string.IsNullOrEmpty(newName))
            {
                // van moi tu menu: bat dau 7 gio sang ngay 1
                Session.Clock.Day = 1; Session.Clock.Minutes = DayClock.NewGameTime;
            }
            void Place()
            {
                if (save != null) SaveSystem.ApplyWorld(save, Session);
                else Player.PlaceAtArrival();
                if (save == null && slot >= 0) { Session.EnsureStarterKit(); Session.SaveNow(); } // tao o luu ngay de menu thay (da co do khoi dau)
            }
            if (Terrain.IsReady) Place();
            else Terrain.Ready += _ => Place();
        }

        /// <summary>Vao 1 o luu (menu chinh). Cung dao thi vao tai cho, khac dao thi nap lai scene.</summary>
        public void Launch(int slot, bool newGame, CharacterOutfit outfit, string name)
        {
            var save = newGame ? null : SaveSystem.Load(slot);
            // dao moi: neu dang xem dao cua 1 o luu khac thi sinh dao moi (seed ngau nhien), moi dao 1 ban do rieng
            bool freshBackground = SaveSystem.MostRecent() < 0;
            int targetSeed = save != null ? save.seed : freshBackground ? Terrain.seed : Random.Range(0, 1000000);
            if (newGame && freshBackground && Terrain.IsReady && !Session.Running)
            {
                StartPlaying(slot, null, outfit, name);
                return;
            }
            if (save != null && save.seed == Terrain.seed && Terrain.IsReady && !Session.Running)
            {
                StartPlaying(slot, save, null, null);
                return;
            }
            GameLaunch.Pending = true; GameLaunch.Slot = slot; GameLaunch.NewGame = newGame;
            GameLaunch.Seed = targetSeed; GameLaunch.Outfit = outfit; GameLaunch.Name = name;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex >= 0 ? SceneManager.GetActiveScene().buildIndex : 0);
        }

        /// <summary>Ve menu chinh (tu menu tam dung): luu roi nap lai scene.</summary>
        public void BackToMenu()
        {
            if (Session && Session.Running && Session.SaveSlot >= 0) Session.SaveNow();
            GameLaunch.Pending = false;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex >= 0 ? SceneManager.GetActiveScene().buildIndex : 0);
        }
    }
}
