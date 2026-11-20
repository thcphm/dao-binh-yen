// Danh sach giong cay. Asset nam o Assets/Resources/CropDatabase.asset (tao bang menu Serene Isle > Tao du lieu item);
// chua co asset thi dung bang du lieu trong code (CropCatalogData).
using System.Collections.Generic;
using UnityEngine;

namespace SereneIsle
{
    [CreateAssetMenu(menuName = "Serene Isle/Crop Database", fileName = "CropDatabase")]
    public class CropDatabase : ScriptableObject
    {
        public List<CropDefinition> crops = new List<CropDefinition>();

        Dictionary<string, CropDefinition> _map;

        void OnEnable() { _map = null; }

        public CropDefinition Get(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            if (_map == null)
            {
                _map = new Dictionary<string, CropDefinition>();
                foreach (var c in crops) if (c && !string.IsNullOrEmpty(c.id)) _map[c.id] = c;
            }
            return _map.TryGetValue(id, out var d) ? d : null;
        }

        /// <summary>Cay moc tu hat giong (item.seedOf).</summary>
        public CropDefinition FromSeed(ItemDefinition seed) => seed ? Get(seed.seedOf) : null;

        static CropDatabase _instance;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { _instance = null; }

        public static CropDatabase Instance
        {
            get
            {
                if (_instance) return _instance;
                _instance = Resources.Load<CropDatabase>("CropDatabase");
                if (!_instance) _instance = CreateInMemory();
                return _instance;
            }
        }

        public static CropDatabase CreateInMemory()
        {
            var db = CreateInstance<CropDatabase>();
            db.name = "CropDatabase (in-memory)";
            db.crops = CropCatalogData.CreateAll();
            return db;
        }
    }
}
