using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SereneIsle.EditorTools
{
    public static class SereneIsleMenu
    {
        const string ScenePath = "Assets/Scenes/SereneIsle.unity";

        [MenuItem("Serene Isle/Tạo scene đảo (SereneIsle.unity)")]
        public static void CreateScene()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            new GameObject("Bootstrap").AddComponent<GameBootstrap>();

            if (!AssetDatabase.IsValidFolder("Assets/Scenes")) AssetDatabase.CreateFolder("Assets", "Scenes");
            EditorSceneManager.SaveScene(scene, ScenePath);

            // scene cua game dung dau; bo cac muc tro toi scene khong con ton tai
            var list = new System.Collections.Generic.List<EditorBuildSettingsScene> { new EditorBuildSettingsScene(ScenePath, true) };
            foreach (var s in EditorBuildSettings.scenes)
                if (s.path != ScenePath && System.IO.File.Exists(s.path)) list.Add(s);
            EditorBuildSettings.scenes = list.ToArray();
            Debug.Log($"[SereneIsle] Da tao {ScenePath}. Bam Play de sinh dao.");
        }

        /// <summary>Mo scene dao va vao Play. Dung tu dong lenh: -executeMethod SereneIsle.EditorTools.SereneIsleMenu.OpenAndPlay</summary>
        [MenuItem("Serene Isle/Mở đảo và chơi %#p")]
        public static void OpenAndPlay()
        {
            if (EditorApplication.isPlaying) return;
            if (!System.IO.File.Exists(ScenePath)) CreateScene();
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            EditorApplication.EnterPlaymode();
        }
    }
}
