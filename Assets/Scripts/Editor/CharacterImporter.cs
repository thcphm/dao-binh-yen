// Cai dat import cho mo hinh + animation KayKit (CC0) va tao Assets/Resources/CharacterLibrary.asset.
// Chay: menu "Serene Isle > Nhap nhan vat KayKit", hoac batchmode -executeMethod SereneIsle.EditorTools.CharacterImporter.Import
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace SereneIsle.EditorTools
{
    public static class CharacterImporter
    {
        const string Root = "Assets/ThirdParty/KayKit";
        const string LibPath = "Assets/Resources/CharacterLibrary.asset";
        static readonly string[] Models = { "Rogue", "Ranger" };

        [MenuItem("Serene Isle/Nhập nhân vật KayKit")]
        public static void Import()
        {
            // ---- bang mau: doc duoc (de ve lai o casual), khong nen ----
            foreach (var m in Models)
            {
                string p = $"{Root}/Characters/{m.ToLower()}_texture.png";
                if (AssetImporter.GetAtPath(p) is TextureImporter ti)
                {
                    ti.isReadable = true; ti.sRGBTexture = true; ti.mipmapEnabled = true;
                    ti.textureCompression = TextureImporterCompression.Uncompressed;
                    ti.filterMode = FilterMode.Bilinear; ti.wrapMode = TextureWrapMode.Clamp;
                    ti.SaveAndReimport();
                }
            }

            // ---- nhan vat: rig Generic, mesh doc duoc, khong tao material ----
            foreach (var m in Models)
            {
                if (!(AssetImporter.GetAtPath($"{Root}/Characters/{m}.fbx") is ModelImporter mi)) { Debug.LogError("Thieu " + m); continue; }
                mi.animationType = ModelImporterAnimationType.Generic;
                mi.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
                mi.importAnimation = false;
                mi.isReadable = true;
                mi.materialImportMode = ModelImporterMaterialImportMode.None;
                mi.importBlendShapes = false; mi.importCameras = false; mi.importLights = false;
                mi.SaveAndReimport();
            }

            // ---- animation: lap lai cho dong tac lien tuc ----
            var animFiles = Directory.GetFiles($"{Root}/Animations", "*.fbx").Select(f => f.Replace('\\', '/')).ToArray();
            foreach (var f in animFiles)
            {
                if (!(AssetImporter.GetAtPath(f) is ModelImporter mi)) continue;
                mi.animationType = ModelImporterAnimationType.Generic;
                mi.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
                mi.importAnimation = true;
                mi.materialImportMode = ModelImporterMaterialImportMode.None;
                mi.importBlendShapes = false; mi.importCameras = false; mi.importLights = false;
                var clips = mi.defaultClipAnimations.Where(c => c.name != "T-Pose").ToArray();
                foreach (var c in clips) c.loopTime = IsLoop(c.name);
                mi.clipAnimations = clips;
                mi.SaveAndReimport();
            }

            // ---- thu vien ----
            var lib = AssetDatabase.LoadAssetAtPath<CharacterLibrary>(LibPath);
            if (!lib)
            {
                Directory.CreateDirectory("Assets/Resources");
                lib = ScriptableObject.CreateInstance<CharacterLibrary>();
                AssetDatabase.CreateAsset(lib, LibPath);
            }
            lib.models.Clear();
            foreach (var m in Models)
            {
                string fbx = $"{Root}/Characters/{m}.fbx";
                lib.models.Add(new CharacterLibrary.Model
                {
                    id = m,
                    prefab = AssetDatabase.LoadAssetAtPath<GameObject>(fbx),
                    avatar = AssetDatabase.LoadAllAssetsAtPath(fbx).OfType<Avatar>().FirstOrDefault(),
                    palette = AssetDatabase.LoadAssetAtPath<Texture2D>($"{Root}/Characters/{m.ToLower()}_texture.png"),
                });
            }

            lib.clips.Clear();
            var probe = Object.Instantiate(lib.models[0].prefab);
            try
            {
                var paths = new HashSet<string>();
                CollectPaths(probe.transform, "", paths);
                int missing = 0, total = 0;
                foreach (var f in animFiles)
                    foreach (var clip in AssetDatabase.LoadAllAssetsAtPath(f).OfType<AnimationClip>())
                    {
                        if (clip.name.StartsWith("__preview__")) continue;
                        foreach (var b in AnimationUtility.GetCurveBindings(clip))
                        {
                            total++;
                            if (!paths.Contains(b.path)) missing++;
                        }
                        float gs = clip.name.StartsWith("Walking") || clip.name.StartsWith("Running") ? GroundSpeed(probe, clip) : 0;
                        lib.clips.Add(new CharacterLibrary.Clip { clip = clip, groundSpeed = gs });
                        if (gs > 0) Debug.Log($"[SereneIsle] {clip.name}: {clip.length:F2}s, toc do buoc chan {gs:F2}/s");
                    }
                Debug.Log($"[SereneIsle] Nhan vat: {lib.models.Count} mo hinh, {lib.clips.Count} dong tac; duong dan xuong khop {total - missing}/{total}" +
                          $" (avatar {(lib.models[0].avatar ? "co" : "KHONG")}, cao {Height(probe):F2})");
            }
            finally { Object.DestroyImmediate(probe); }

            EditorUtility.SetDirty(lib);
            AssetDatabase.SaveAssets();
        }

        static bool IsLoop(string n)
            => n.Contains("Idle") || n.EndsWith("ing") || n.StartsWith("Walking") || n.StartsWith("Running") || n.StartsWith("Holding")
               || n == "Push_Ups" || n == "Sit_Ups";

        static void CollectPaths(Transform t, string prefix, HashSet<string> paths)
        {
            paths.Add(prefix);
            foreach (Transform c in t) CollectPaths(c, prefix.Length == 0 ? c.name : prefix + "/" + c.name, paths);
        }

        static Transform Find(Transform t, string n)
        {
            if (t.name == n) return t;
            foreach (Transform c in t) { var r = Find(c, n); if (r) return r; }
            return null;
        }

        static float Height(GameObject go)
        {
            float top = 0;
            foreach (var r in go.GetComponentsInChildren<SkinnedMeshRenderer>())
                if (r.sharedMesh && r.sharedMesh.isReadable)
                    foreach (var v in r.sharedMesh.vertices) top = Mathf.Max(top, go.transform.InverseTransformPoint(r.transform.TransformPoint(v)).y);
            return top;
        }

        /// <summary>
        /// Toc do "buoc chan" cua dong tac di/chay (o ti le goc): ban chan tru truot ve sau 1 doan R trong ~nua chu ky.
        /// </summary>
        static float GroundSpeed(GameObject go, AnimationClip clip)
        {
            var fl = Find(go.transform, "foot.l");
            if (!fl) return 0;
            const int N = 48;
            var pts = new List<Vector3>();
            for (int i = 0; i < N; i++)
            {
                clip.SampleAnimation(go, clip.length * i / N);
                pts.Add(go.transform.InverseTransformPoint(fl.position));
            }
            float rx = pts.Max(p => p.x) - pts.Min(p => p.x), rz = pts.Max(p => p.z) - pts.Min(p => p.z);
            float r = Mathf.Max(rx, rz);
            return clip.length > 0 ? r / (clip.length * .5f) : 0;
        }
    }
}
