// Dat 1 component nay vao scene trong la chay duoc: tu tao moi truong, dia hinh, camera, nhan vat.
// (Hoac dung menu "Serene Isle > Tao scene dao" de sinh san scene.)
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace SereneIsle
{
    [DefaultExecutionOrder(-100)]
    public class GameBootstrap : MonoBehaviour
    {
        [Header("Dao")]
        [Tooltip("<0 = ngau nhien moi lan chay")] public int seed = -1;
        [Range(200, 1000)] public int islandSize = 800;

        [Header("Nhan vat")]
        public bool female;

        [Header("Render")]
        [Tooltip("Tat post-processing tren camera (tone mapping se lam lech mau so voi ban web).")]
        public bool disablePostProcessing = true;

        [Header("Luu game")]
        [Tooltip("O luu (0-3). < 0 = khong tai, khong luu (test).")]
        public int saveSlot = 0;
        [Tooltip("Mo game thi tai lai o luu neu co (menu chinh o giai doan 5).")]
        public bool loadSave = true;

        [Header("Thu nghiem")]
        [Tooltip("Cho vai mon do de thu tui do / an uong (giai doan 2).")]
        public bool starterKit = true;

        public GameSession Session { get; private set; }
        public GameHud Hud { get; private set; }
        public IslandTerrain Terrain { get; private set; }
        public PlayerController Player { get; private set; }
        public OrbitCamera CameraRig { get; private set; }

        void Awake()
        {
            if (!(GraphicsSettings.currentRenderPipeline is UniversalRenderPipelineAsset))
                Debug.LogWarning("[SereneIsle] Project chua dung URP: shader SereneIsle/* se khong hien. Gan URP Asset trong Project Settings > Graphics.");

            // ---- moi truong ----
            var env = FindAnyObjectByType<IslandEnvironment>();
            if (!env) env = new GameObject("Environment").AddComponent<IslandEnvironment>();
            env.advanceTime = false; // gio do GameSession dieu khien
            if (!env.GetComponent<SkyRenderer>()) env.gameObject.AddComponent<SkyRenderer>();

            // ---- van choi (phai co truoc dia hinh de nhan vi tri leu) ----
            Session = FindAnyObjectByType<GameSession>();
            if (!Session) Session = new GameObject("GameSession").AddComponent<GameSession>();
            Session.starterKit = starterKit;
            Session.SaveSlot = saveSlot;
            if (!ToolSystem.Instance) Session.gameObject.AddComponent<ToolSystem>();
            var save = saveSlot >= 0 && loadSave ? SaveSystem.Load(saveSlot) : null;

            // ---- dia hinh (Start chay sau Awake -> kip gan seed/size) ----
            Terrain = FindAnyObjectByType<IslandTerrain>();
            if (!Terrain)
            {
                Terrain = new GameObject("Island").AddComponent<IslandTerrain>();
                Terrain.seed = save != null ? save.seed : seed;
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

            // ---- nhan vat: tao o trang thai tat de kip dat ngoai hinh truoc Awake ----
            Player = FindAnyObjectByType<PlayerController>();
            if (!Player)
            {
                var pgo = new GameObject("Player");
                pgo.SetActive(false);
                var body = pgo.AddComponent<IslandCharacter>();
                body.outfit = save?.outfit != null && !string.IsNullOrEmpty(save.outfit.model) ? save.outfit
                            : female ? CharacterOutfit.Female() : CharacterOutfit.Male();
                Player = pgo.AddComponent<PlayerController>();
                pgo.SetActive(true);
            }
            Player.cameraRig = CameraRig;
            CameraRig.target = Player.transform;
            env.followTarget = Player.transform;
            Session.Player = Player;
            Session.Body = Player.GetComponent<IslandCharacter>();
            Session.Environment = env;

            // ---- giao dien ----
            Hud = FindAnyObjectByType<GameHud>();
            if (!Hud) Hud = new GameObject("HUD").AddComponent<GameHud>();
            if (!Hud.GetComponent<ActionUI>()) Hud.gameObject.AddComponent<ActionUI>();

            if (save != null)
            {
                SaveSystem.ApplyState(save, Session);
                Session.MarkKitGiven();
                Debug.Log($"[SereneIsle] Tai o luu {saveSlot}: ngay {save.day}, luu luc {save.savedAt}");
            }
            void Place()
            {
                if (save != null) SaveSystem.ApplyWorld(save, Session);
                else Player.PlaceAtArrival();
            }
            if (Terrain.IsReady) Place();
            else Terrain.Ready += _ => Place();
        }
    }
}
