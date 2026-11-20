// Tui do (ban web: G.inv + STACK/BAGS/slotsUsed/roomFor/give/take).
// Luu theo so luong moi loai; moi 99 cai chiem 1 o. Them gioi han khoi luong (dac ta ban Unity).
using System;
using System.Collections.Generic;

namespace SereneIsle
{
    public sealed class Inventory
    {
        public const int Stack = 99;

        public struct BagInfo
        {
            public string name; public int slots; public float maxWeight;
            public BagInfo(string n, int s, float w) { name = n; slots = s; maxWeight = w; }
        }

        /// <summary>Ban web: Tay khong 8 o, Ba lo vai 16, Ba lo da 24. Khoi luong toi da them moi.</summary>
        public static readonly BagInfo[] Bags =
        {
            new BagInfo("Tay không", 8, 25f),
            new BagInfo("Ba lô vải", 16, 40f),
            new BagInfo("Ba lô da", 24, 60f),
        };

        readonly IItemCatalog _catalog;
        readonly Dictionary<string, int> _counts = new Dictionary<string, int>();
        readonly List<string> _order = new List<string>(); // thu tu nhat duoc, de UI on dinh

        int _bagLevel;
        public int BagLevel
        {
            get => _bagLevel;
            set { _bagLevel = Math.Max(0, Math.Min(Bags.Length - 1, value)); Changed?.Invoke(); }
        }
        public BagInfo Bag => Bags[_bagLevel];
        public int Capacity => Bag.slots;
        public float MaxWeight => Bag.maxWeight;

        /// <summary>Goi moi khi tui thay doi (UI cap nhat).</summary>
        public event Action Changed;

        public Inventory(IItemCatalog catalog) { _catalog = catalog; }

        public IReadOnlyList<string> Order => _order;
        public int Count(string id) => id != null && _counts.TryGetValue(id, out var n) ? n : 0;
        public bool Has(string id, int n = 1) => Count(id) >= n;
        public int TotalItems { get { int s = 0; foreach (var v in _counts.Values) s += v; return s; } }

        float WeightOf(string id) => _catalog?.Get(id)?.weight ?? 0f;

        public float Weight
        {
            get { float w = 0; foreach (var kv in _counts) w += kv.Value * WeightOf(kv.Key); return w; }
        }

        /// <summary>So o dang dung (bo qua 1 loai neu can).</summary>
        public int SlotsUsed(string except = null)
        {
            int u = 0;
            foreach (var kv in _counts) if (kv.Key != except && kv.Value > 0) u += (kv.Value + Stack - 1) / Stack;
            return u;
        }

        /// <summary>Con nhet them duoc bao nhieu cai: min(theo o, theo khoi luong).</summary>
        public int RoomFor(string id)
        {
            int bySlots = Math.Max(0, (Capacity - SlotsUsed(id)) * Stack - Count(id));
            float w = WeightOf(id);
            if (w <= 0) return bySlots;
            int byWeight = Math.Max(0, (int)Math.Floor((MaxWeight - Weight) / w + 1e-4));
            return Math.Min(bySlots, byWeight);
        }

        /// <summary>Them toi da n cai, tra ve so cai da them (phan con lai nguoi goi tu xu ly: roi xuong dat).</summary>
        public int Give(string id, int n = 1)
        {
            if (string.IsNullOrEmpty(id) || n <= 0) return 0;
            int add = Math.Min(n, RoomFor(id));
            if (add <= 0) return 0;
            if (!_counts.ContainsKey(id)) { _counts[id] = 0; _order.Add(id); }
            _counts[id] += add;
            Changed?.Invoke();
            return add;
        }

        public bool Take(string id, int n = 1)
        {
            if (n <= 0 || !Has(id, n)) return false;
            _counts[id] -= n;
            if (_counts[id] <= 0) { _counts.Remove(id); _order.Remove(id); }
            Changed?.Invoke();
            return true;
        }

        /// <summary>Lam rong tui (ngat luc 23h). Tra ve tong so mon da mat.</summary>
        public int Clear()
        {
            int n = TotalItems;
            _counts.Clear(); _order.Clear();
            Changed?.Invoke();
            return n;
        }

        /// <summary>Danh sach o hien thi: moi phan tu = (id, so luong &lt;= 99).</summary>
        public List<KeyValuePair<string, int>> Stacks()
        {
            var list = new List<KeyValuePair<string, int>>();
            foreach (var id in _order)
            {
                int left = _counts[id];
                while (left > 0) { int s = Math.Min(Stack, left); list.Add(new KeyValuePair<string, int>(id, s)); left -= s; }
            }
            return list;
        }
    }
}
