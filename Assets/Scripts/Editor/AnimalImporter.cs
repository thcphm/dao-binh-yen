// Cai dat import cho thu Quaternius (CC0) va tao Assets/Resources/AnimalLibrary.asset.
// Chay: menu "Serene Isle > Nhap thu Quaternius", hoac batchmode -executeMethod SereneIsle.EditorTools.AnimalImporter.Import
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace SereneIsle.EditorTools
{
    public static class AnimalImporter
    {
        const string Root = "Assets/ThirdParty/Quaternius/Animals";
        const string LibPath = "Assets/Resources/AnimalLibrary.asset";
        static readonly string[] Models = { "Bull", "Cow", "Deer", "ShibaInu" };

        [MenuItem("Serene Isle/Nhập thú Quaternius")]
        public static void Import()
        {
            foreach (var m in Models)
            {
                string p = $"{Root}/{m}.fbx";
                if (!(AssetImporter.GetAtPath(p) is ModelImporter mi)) { Debug.LogError("Thieu " + p); continue; }
                mi.animationType = ModelImporterAnimationType.Generic;
                mi.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
                mi.importAnimation = true;
                mi.isReadable = true;
                // giu ten vat lieu (Main, Horns, Eye_White...) de to mau lai luc chay
                mi.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
                mi.materialLocation = ModelImporterMaterialLocation.InPrefab;
                mi.importBlendShapes = false; mi.importCameras = false; mi.importLights = false;
                var clips = mi.defaultClipAnimations;
                foreach (var c in clips)
                {
                    string n = c.name.Contains("|") ? c.name.Substring(c.name.LastIndexOf('|') + 1) : c.name;
                    c.name = n;
                    c.loopTime = n.StartsWith("Idle") || n == "Walk" || n == "Gallop" || n == "Eating";
                }
                mi.clipAnimations = clips;
                mi.SaveAndReimport();
            }

            var lib = AssetDatabase.LoadAssetAtPath<AnimalLibrary>(LibPath);
            if (!lib)
            {
                lib = ScriptableObject.CreateInstance<AnimalLibrary>();
                AssetDatabase.CreateAsset(lib, LibPath);
            }
            lib.models.Clear();
            foreach (var m in Models)
            {
                string p = $"{Root}/{m}.fbx";
                var all = AssetDatabase.LoadAllAssetsAtPath(p);
                var model = new AnimalLibrary.Model
                {
                    id = m,
                    prefab = AssetDatabase.LoadAssetAtPath<GameObject>(p),
                    avatar = all.OfType<Avatar>().FirstOrDefault(),
                    clips = all.OfType<AnimationClip>().Where(c => !c.name.StartsWith("__preview__")).ToList(),
                };
                lib.models.Add(model);
                Debug.Log($"[SereneIsle] Thu {m}: {model.clips.Count} dong tac ({string.Join(", ", model.clips.Select(c => c.name))})");
            }
            EditorUtility.SetDirty(lib);
            AssetDatabase.SaveAssets();
        }
    }
}
