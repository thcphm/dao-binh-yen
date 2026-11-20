// Danh sach tat ca item. Asset nam o Assets/Resources/ItemDatabase.asset (tao bang menu Serene Isle > Tao du lieu item).
using System.Collections.Generic;
using UnityEngine;

namespace SereneIsle
{
    public interface IItemCatalog
    {
        ItemDefinition Get(string id);
    }

    [CreateAssetMenu(menuName = "Serene Isle/Item Database", fileName = "ItemDatabase")]
    public class ItemDatabase : ScriptableObject, IItemCatalog
    {
        public List<ItemDefinition> items = new List<ItemDefinition>();

        Dictionary<string, ItemDefinition> _map;

        void OnEnable() { _map = null; }

        public ItemDefinition Get(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            if (_map == null)
            {
                _map = new Dictionary<string, ItemDefinition>();
                foreach (var it in items) if (it && !string.IsNullOrEmpty(it.id)) _map[it.id] = it;
            }
            return _map.TryGetValue(id, out var d) ? d : null;
        }

        public string NameOf(string id) => Get(id)?.displayName ?? id;

        static ItemDatabase _instance;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { _instance = null; }

        /// <summary>Asset trong Resources; neu chua tao thi dung bang du lieu trong code.</summary>
        public static ItemDatabase Instance
        {
            get
            {
                if (_instance) return _instance;
                _instance = Resources.Load<ItemDatabase>("ItemDatabase");
                if (!_instance) _instance = CreateInMemory();
                return _instance;
            }
        }

        public static ItemDatabase CreateInMemory()
        {
            var db = CreateInstance<ItemDatabase>();
            db.name = "ItemDatabase (in-memory)";
            db.items = ItemCatalogData.CreateAll();
            return db;
        }
    }
}
