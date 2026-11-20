// Tao asset item (ScriptableObject) tu bang ItemCatalogData:
//   Assets/Data/Items/<id>.asset  +  Assets/Resources/ItemDatabase.asset
// Chay lai an toan: asset da co duoc cap nhat tai cho (giu GUID, giu sprite tu gan).
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace SereneIsle.EditorTools
{
    public static class ItemDatabaseBuilder
    {
        const string ItemDir = "Assets/Data/Items";
        const string DbPath = "Assets/Resources/ItemDatabase.asset";

        [MenuItem("Serene Isle/Tạo dữ liệu item (ScriptableObject)")]
        public static void Build()
        {
            Directory.CreateDirectory(ItemDir);
            Directory.CreateDirectory(Path.GetDirectoryName(DbPath));
            AssetDatabase.Refresh();

            var fresh = ItemCatalogData.CreateAll();
            var list = new List<ItemDefinition>();
            foreach (var src in fresh)
            {
                string path = $"{ItemDir}/{src.id}.asset";
                var existing = AssetDatabase.LoadAssetAtPath<ItemDefinition>(path);
                if (existing)
                {
                    var keepSprite = existing.sprite;
                    EditorUtility.CopySerialized(src, existing);
                    existing.name = src.id;
                    if (keepSprite) existing.sprite = keepSprite;
                    EditorUtility.SetDirty(existing);
                    list.Add(existing);
                    Object.DestroyImmediate(src);
                }
                else
                {
                    AssetDatabase.CreateAsset(src, path);
                    list.Add(src);
                }
            }

            var db = AssetDatabase.LoadAssetAtPath<ItemDatabase>(DbPath);
            if (!db)
            {
                db = ScriptableObject.CreateInstance<ItemDatabase>();
                AssetDatabase.CreateAsset(db, DbPath);
            }
            db.items = list;
            EditorUtility.SetDirty(db);
            AssetDatabase.SaveAssets();
            Debug.Log($"[SereneIsle] Da tao {list.Count} item trong {ItemDir} va {DbPath}");
            BuildCrops();
        }

        const string CropDir = "Assets/Data/Crops";
        const string CropDbPath = "Assets/Resources/CropDatabase.asset";

        /// <summary>Giong cay 4 mua: Assets/Data/Crops/&lt;id&gt;.asset + Assets/Resources/CropDatabase.asset.</summary>
        public static void BuildCrops()
        {
            Directory.CreateDirectory(CropDir);
            AssetDatabase.Refresh();
            var list = new List<CropDefinition>();
            foreach (var src in CropCatalogData.CreateAll())
            {
                string path = $"{CropDir}/{src.id}.asset";
                var existing = AssetDatabase.LoadAssetAtPath<CropDefinition>(path);
                if (existing)
                {
                    EditorUtility.CopySerialized(src, existing);
                    existing.name = src.id;
                    EditorUtility.SetDirty(existing);
                    list.Add(existing);
                    Object.DestroyImmediate(src);
                }
                else
                {
                    AssetDatabase.CreateAsset(src, path);
                    list.Add(src);
                }
            }
            var db = AssetDatabase.LoadAssetAtPath<CropDatabase>(CropDbPath);
            if (!db)
            {
                db = ScriptableObject.CreateInstance<CropDatabase>();
                AssetDatabase.CreateAsset(db, CropDbPath);
            }
            db.crops = list;
            EditorUtility.SetDirty(db);
            AssetDatabase.SaveAssets();
            Debug.Log($"[SereneIsle] Da tao {list.Count} giong cay trong {CropDir} va {CropDbPath}");
        }
    }
}
