// Cai dat (giai doan 5): luu settings.json canh file luu (SaveSystem.Dir) - khong dung PlayerPrefs (registry o o C:).
// Am thanh luu san cho giai doan 6 (nhac lofi, tieng dong).
using System;
using System.IO;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace SereneIsle
{
    [Serializable]
    public class GameSettings
    {
        public float music = .7f, sfx = .8f;
        public float mouseSensitivity = 1f;     // nhan voi toc do xoay camera keo chuot
        public bool invertDrag;                 // keo chuot dao chieu
        public int viewDistance = 1;            // 0 gan, 1 vua, 2 xa
        public bool shadows = true;
        public float renderScale = 1f;          // .75 = nhe may, 1 = net
        public bool showHelp = true;
        public bool autosave = true;

        public static readonly string[] ViewNames = { "Gần", "Vừa", "Xa" };

        static GameSettings _current;
        public static GameSettings Current => _current ??= Load();
        public static event Action Changed;

        public static string PathOf => System.IO.Path.Combine(SaveSystem.Dir, "settings.json");

        public static GameSettings Load()
        {
            try
            {
                if (File.Exists(PathOf)) return JsonUtility.FromJson<GameSettings>(File.ReadAllText(PathOf)) ?? new GameSettings();
            }
            catch (Exception e) { Debug.LogWarning("[SereneIsle] settings.json hong: " + e.Message); }
            return new GameSettings();
        }

        public void Save()
        {
            try
            {
                Directory.CreateDirectory(SaveSystem.Dir);
                File.WriteAllText(PathOf, JsonUtility.ToJson(this, true));
            }
            catch (Exception e) { Debug.LogWarning("[SereneIsle] Khong luu duoc cai dat: " + e.Message); }
        }

        /// <summary>Ap dung vao camera, cay co, bong, do phan giai.</summary>
        public void Apply()
        {
            var cam = Camera.main;
            var rig = cam ? cam.GetComponent<OrbitCamera>() : null;
            if (rig)
            {
                rig.dragSensitivity = .006f * Mathf.Clamp(mouseSensitivity, .3f, 2.5f) * (invertDrag ? -1 : 1);
                rig.stickSensitivity = 2.6f * Mathf.Clamp(mouseSensitivity, .3f, 2.5f);
            }
            float[] far = { 100, 150, 220 }, grass = { 70, 110, 150 }, camFar = { 220, 320, 420 };
            int v = Mathf.Clamp(viewDistance, 0, 2);
            var veg = IslandTerrain.Instance ? IslandTerrain.Instance.Vegetation : null;
            if (veg) { veg.farDistance = far[v]; veg.grassDistance = grass[v]; }
            if (cam) cam.farClipPlane = camFar[v];
            var env = UnityEngine.Object.FindAnyObjectByType<IslandEnvironment>();
            if (env) { env.fogFar = 150 * (v == 0 ? .8f : v == 2 ? 1.3f : 1f); }
            foreach (var l in UnityEngine.Object.FindObjectsByType<Light>())
                if (l.type == LightType.Directional) l.shadows = shadows ? LightShadows.Soft : LightShadows.None;
#if !UNITY_EDITOR
            // trong Editor doi asset URP se ghi de file .asset -> chi ap dung o ban build
            if (UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline is UniversalRenderPipelineAsset urp)
                urp.renderScale = Mathf.Clamp(renderScale, .5f, 1f);
#endif
            Changed?.Invoke();
        }

        public static void Set(GameSettings s)
        {
            _current = s;
            s.Save();
            s.Apply();
        }

        /// <summary>Test: bo bo nho dem.</summary>
        public static void Reset() => _current = null;
    }
}
