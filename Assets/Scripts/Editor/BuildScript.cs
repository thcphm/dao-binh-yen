// Dung ban choi: WebGL (GitHub Pages) va Windows. Menu "Serene Isle/Dung ban ..." hoac batchmode:
//   Unity.exe -batchmode -quit -projectPath <du an> -executeMethod SereneIsle.EditorTools.BuildScript.WebGL
// Ket qua o <du an>/Builds/<nen tang> (git bo qua). Bien moi truong SERENE_BUILD_DIR doi thu muc ra.
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace SereneIsle.EditorTools
{
    public static class BuildScript
    {
        public const string Product = "Đảo Bình Yên";
        public const string Company = "Thuc Pham";

        static string OutDir(string platform)
        {
            var root = Environment.GetEnvironmentVariable("SERENE_BUILD_DIR");
            if (string.IsNullOrEmpty(root)) root = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Builds"));
            return Path.Combine(root, platform);
        }

        static string[] Scenes => EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();

        static void Common()
        {
            PlayerSettings.companyName = Company;
            PlayerSettings.productName = Product;
            PlayerSettings.bundleVersion = "1.0.0";
            PlayerSettings.runInBackground = true;
        }

        [MenuItem("Serene Isle/Dựng bản WebGL")]
        public static void WebGL()
        {
            Common();
            // GitHub Pages khong gui header Content-Encoding cho .gz -> bat giai nen du phong trong trinh duyet
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
            PlayerSettings.WebGL.decompressionFallback = true;
            PlayerSettings.WebGL.dataCaching = true;
            PlayerSettings.WebGL.exceptionSupport = WebGLExceptionSupport.ExplicitlyThrownExceptionsOnly;
            PlayerSettings.WebGL.nameFilesAsHashes = false;
            PlayerSettings.WebGL.maximumMemorySize = 2048;
            PlayerSettings.WebGL.showDiagnostics = false;
            PlayerSettings.WebGL.template = "PROJECT:SereneIsle"; // Assets/WebGLTemplates/SereneIsle: toan man hinh, man tai hoang hon
            PlayerSettings.SetManagedStrippingLevel(UnityEditor.Build.NamedBuildTarget.WebGL, ManagedStrippingLevel.Low);
            Run(BuildTarget.WebGL, OutDir("WebGL"));
        }

        [MenuItem("Serene Isle/Dựng bản Windows")]
        public static void Windows()
        {
            Common();
            Run(BuildTarget.StandaloneWindows64, Path.Combine(OutDir("Windows"), "SereneIsle.exe"));
        }

        const string KeepPrefab = "Assets/Resources/Internal/KeepPhysics.prefab";

        /// <summary>Prefab co san cac collider trong Resources: Unity chi giu lop native (SphereCollider...) khi co asset dung toi,
        /// ma GameObject.CreatePrimitive (ProceduralMeshes) them collider luc chay -> thieu lop thi WebGL bao loi.</summary>
        static void EnsureKeepPrefab()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(KeepPrefab)) return;
            Directory.CreateDirectory(Path.GetDirectoryName(KeepPrefab));
            var go = new GameObject("KeepPhysics");
            go.AddComponent<BoxCollider>(); go.AddComponent<SphereCollider>(); go.AddComponent<CapsuleCollider>(); go.AddComponent<MeshCollider>();
            PrefabUtility.SaveAsPrefabAsset(go, KeepPrefab);
            UnityEngine.Object.DestroyImmediate(go);
            AssetDatabase.Refresh();
        }

        static void Run(BuildTarget target, string path)
        {
            EnsureKeepPrefab();
            AssetDatabase.SaveAssets();
            var opts = new BuildPlayerOptions { scenes = Scenes, locationPathName = path, target = target, options = BuildOptions.None };
            var t0 = DateTime.Now;
            var report = BuildPipeline.BuildPlayer(opts);
            var s = report.summary;
            Debug.Log($"[SereneIsle] Dung {target}: {s.result}, {s.totalSize / 1048576f:0.0} MB, {(DateTime.Now - t0).TotalSeconds:0}s, {s.totalErrors} loi -> {path}");
            foreach (var step in report.steps)
                foreach (var m in step.messages)
                    if (m.type == LogType.Error || m.type == LogType.Exception) Debug.LogError("[SereneIsle] " + m.content);
            if (Application.isBatchMode && s.result != BuildResult.Succeeded) EditorApplication.Exit(1);
        }
    }
}
