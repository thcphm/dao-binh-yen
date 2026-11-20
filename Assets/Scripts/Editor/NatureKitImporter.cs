// Cai dat import cho mo hinh Quaternius Stylized Nature MegaKit (CC0) va tao Assets/Resources/NatureKit.asset.
// Chay: menu "Serene Isle > Nhap cay da Quaternius", hoac batchmode -executeMethod SereneIsle.EditorTools.NatureKitImporter.Import
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace SereneIsle.EditorTools
{
    public static class NatureKitImporter
    {
        const string Root = "Assets/ThirdParty/Quaternius/StylizedNature";
        const string KitPath = "Assets/Resources/NatureKit.asset";

        // ten vat lieu trong FBX -> (texture, la cat alpha, nhan mau)
        static readonly (string mat, string tex, bool cutout, string tint)[] Surfaces =
        {
            ("Bark_NormalTree", "Bark_NormalTree", false, "#ffffff"),
            ("Bark_TwistedTree", "Bark_TwistedTree", false, "#ffffff"),
            ("Leaves_NormalTree", "Leaves_NormalTree_C", true, "#ffffff"),
            ("Leaves_Pine", "Leaf_Pine_C", true, "#ffffff"),
            ("Leaves_TwistedTree", "Leaves_TwistedTree", true, "#5e9c3a"), // ban _C la mau la thu (do) -> dung ban trang nhuom xanh
            ("Leaves", "Leaves", true, "#6fae45"),      // la trang -> nhuom xanh (ban Source dung shader rieng)
            ("Flowers", "Flowers", true, "#ffffff"),
            ("Grass", "Grass", false, "#c9c27a"),        // say: xanh vang
            ("Rocks", "Rocks_Diffuse", false, "#ffffff"),
        };

        // loai vat the -> mo hinh, ti le chieu cao so voi mo hinh cu
        static readonly (WorldObjectType type, string[] models, float height, string overrideTex, string tint)[] Map =
        {
            (WorldObjectType.Gum, Range("CommonTree_", 5), 1.25f, null, null),
            (WorldObjectType.Pine, Range("Pine_", 5), 1.2f, null, null),
            (WorldObjectType.JungleTree, Range("TwistedTree_", 5), 1.15f, null, null),
            (WorldObjectType.Bush, new[] { "Bush_Common", "Bush_Common_Flowers" }, 1.05f, null, null),
            (WorldObjectType.Fern, new[] { "Fern_1" }, 1.1f, null, null),
            (WorldObjectType.Reeds, new[] { "Grass_Wispy_Tall" }, 1f, null, null),
            (WorldObjectType.Rock, Range("Rock_Medium_", 3), 1f, null, null),
            // da do: texture da sa mac (vang cat) nhuom do gach cho hop dat do
            (WorldObjectType.RedRock, Range("Rock_Medium_", 3), 1f, "Rocks_Desert_Diffuse", "#e0805a"),
        };

        static string[] Range(string prefix, int n) => Enumerable.Range(1, n).Select(i => prefix + i).ToArray();

        [MenuItem("Serene Isle/Nhập cây đá Quaternius")]
        public static void Import()
        {
            // ---- texture: toi da 1024 (WebGL), la cay giu do phu alpha qua cac mip ----
            foreach (var f in Directory.GetFiles($"{Root}/Textures", "*.png"))
            {
                var p = f.Replace('\\', '/');
                if (!(AssetImporter.GetAtPath(p) is TextureImporter ti)) continue;
                bool leaf = Surfaces.Any(s => s.cutout && s.tex == Path.GetFileNameWithoutExtension(p));
                ti.maxTextureSize = 1024;
                ti.sRGBTexture = true;
                ti.mipmapEnabled = true;
                ti.alphaIsTransparency = leaf;
                ti.mipMapsPreserveCoverage = leaf;
                ti.alphaTestReferenceValue = .5f;
                ti.wrapMode = leaf ? TextureWrapMode.Clamp : TextureWrapMode.Repeat;
                ti.SaveAndReimport();
            }

            // ---- mo hinh: mesh doc duoc (gop luc chay), giu ten vat lieu ----
            foreach (var f in Directory.GetFiles($"{Root}/Models", "*.fbx"))
            {
                if (!(AssetImporter.GetAtPath(f.Replace('\\', '/')) is ModelImporter mi)) continue;
                mi.isReadable = true;
                mi.animationType = ModelImporterAnimationType.None;
                mi.importAnimation = false;
                mi.importCameras = false; mi.importLights = false; mi.importBlendShapes = false;
                mi.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
                mi.materialLocation = ModelImporterMaterialLocation.InPrefab;
                mi.SaveAndReimport();
            }

            var kit = AssetDatabase.LoadAssetAtPath<NatureKit>(KitPath);
            if (!kit)
            {
                Directory.CreateDirectory("Assets/Resources");
                kit = ScriptableObject.CreateInstance<NatureKit>();
                AssetDatabase.CreateAsset(kit, KitPath);
            }
            kit.surfaces.Clear();
            foreach (var s in Surfaces)
                kit.surfaces.Add(new NatureKit.Surface
                {
                    material = s.mat, texture = Tex(s.tex), cutout = s.cutout, tint = MaterialLibrary.Hex(s.tint),
                });

            kit.types.Clear();
            var matNames = new HashSet<string>();
            foreach (var (type, models, height, overrideTex, tint) in Map)
            {
                var set = new NatureKit.TypeSet
                {
                    type = type, heightScale = height, textureOverride = overrideTex != null ? Tex(overrideTex) : null,
                    tintOverride = tint != null ? MaterialLibrary.Hex(tint) : Color.clear,
                };
                foreach (var m in models)
                {
                    var go = AssetDatabase.LoadAssetAtPath<GameObject>($"{Root}/Models/{m}.fbx");
                    if (!go) { Debug.LogError("[SereneIsle] Thieu mo hinh " + m); continue; }
                    set.models.Add(go);
                    foreach (var r in go.GetComponentsInChildren<MeshRenderer>(true))
                        foreach (var mat in r.sharedMaterials) if (mat) matNames.Add(mat.name);
                }
                set.lodColor = LeafColor(kit, set);
                kit.types.Add(set);
            }
            var unknown = matNames.Where(n => kit.GetSurface(n) == null).ToArray();
            Debug.Log($"[SereneIsle] NatureKit: {kit.types.Count} loai, {kit.types.Sum(t => t.models.Count)} mo hinh; vat lieu: {string.Join(", ", matNames)}" +
                      (unknown.Length > 0 ? " | CHUA GAN: " + string.Join(", ", unknown) : ""));

            EditorUtility.SetDirty(kit);
            AssetDatabase.SaveAssets();
        }

        static Texture2D Tex(string name) => AssetDatabase.LoadAssetAtPath<Texture2D>($"{Root}/Textures/{name}.png");

        /// <summary>Mau trung binh cua la (pixel alpha > .5) x mau nhan -> tan la LOD o xa.</summary>
        static Color LeafColor(NatureKit kit, NatureKit.TypeSet set)
        {
            foreach (var go in set.models)
                foreach (var r in go.GetComponentsInChildren<MeshRenderer>(true))
                    foreach (var mat in r.sharedMaterials)
                    {
                        if (!mat) continue;
                        var s = kit.GetSurface(mat.name);
                        if (s == null || !s.cutout || !s.texture) continue;
                        var t = new Texture2D(2, 2);
                        t.LoadImage(File.ReadAllBytes(AssetDatabase.GetAssetPath(s.texture)));
                        var px = t.GetPixels32();
                        double cr = 0, cg = 0, cb = 0; int n = 0;
                        for (int i = 0; i < px.Length; i += 7)
                            if (px[i].a > 128) { cr += px[i].r; cg += px[i].g; cb += px[i].b; n++; }
                        Object.DestroyImmediate(t);
                        if (n == 0) continue;
                        return new Color((float)(cr / n / 255) * s.tint.r, (float)(cg / n / 255) * s.tint.g, (float)(cb / n / 255) * s.tint.b, 1);
                    }
            return MaterialLibrary.Hex(WorldObjects.Info(set.type).fx);
        }
    }
}
