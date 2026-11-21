// Nau an o lua trai (ban web: COOK dong 1269-1272, cookDo; ngoi suoi lua dong 1239-1243).
// Ban web 7 mon + them mon Viet tu nong san 4 mua (com, xoi, rau muong xao toi, canh cai, ca kho to, khoai nuong,
// banh chung). "@fish" = ca bat ky (dung ca re truoc), "@milk" = sua bo hoac sua de (dung sua de truoc).
using System;
using System.Collections.Generic;
using System.Linq;

namespace SereneIsle
{
    public sealed class CookRecipe
    {
        public string id;
        public (string id, int n)[] req;
        public string note;
    }

    public static class Cooking
    {
        static CookRecipe R(string id, string note, params (string, int)[] req) => new CookRecipe { id = id, note = note, req = req };

        public static readonly CookRecipe[] All =
        {
            R("fishcook", "Xiên cá nướng trên than hồng", ("@fish", 1)),
            R("com", "Nấu nồi cơm gạo mới", ("lua", 2)),
            R("xoi", "Đồ xôi nếp dẻo thơm", ("nep", 2)),
            R("rau_xao", "Xào lửa lớn cho giòn", ("rau_muong", 2), ("toi", 1)),
            R("canh_cai", "Nấu canh cho mát", ("cai_be", 2)),
            R("ca_kho", "Kho tộ với ớt cho đậm", ("@fish", 1), ("ot", 1)),
            R("khoai_nuong", "Vùi khoai vào tro nóng", ("khoai_lang", 1)),
            R("soup", "Súp cà chua nóng", ("tomato", 2)),
            R("omelet", "Chiên trứng thơm phức", ("egg", 2)),
            R("smoothie", "Sinh tố dừa mát lạnh", ("coconut", 1), ("berry", 2)),
            R("bread", "Bánh mì nướng giòn", ("wheat", 3)),
            R("hotpot", "Lẩu cá chua đậm đà", ("@fish", 2), ("tomato", 2)),
            R("stew", "Bí đỏ hầm sữa", ("pumpkin", 1), ("@milk", 1)),
            R("banh_chung", "Gói bánh chưng cho Tết", ("nep", 3), ("rau_thom", 1)),
        };

        public static CookRecipe Get(string id) => Array.Find(All, r => r.id == id);

        static bool Matches(ItemDefinition d, string token)
        {
            if (!d) return false;
            if (token == "@fish") return d.category == ItemCategory.Fish;
            if (token == "@milk") return d.id == "milk" || d.id == "goatmilk";
            return d.id == token;
        }

        public static int Have(Inventory inv, ItemDatabase items, string token)
        {
            int n = 0;
            foreach (var kv in inv.Stacks()) if (Matches(items.Get(kv.Key), token)) n += kv.Value;
            return n;
        }

        public static bool CanCook(CookRecipe r, Inventory inv, ItemDatabase items)
            => r != null && r.req.All(q => Have(inv, items, q.id) >= q.n);

        public static string ReqText(CookRecipe r, Inventory inv, ItemDatabase items)
            => string.Join(" + ", r.req.Select(q =>
            {
                string name = q.id == "@fish" ? "cá bất kỳ" : q.id == "@milk" ? "sữa" : items.NameOf(q.id);
                return $"{q.n} {name} ({Have(inv, items, q.id)})";
            }));

        /// <summary>Nau 1 mon: tru nguyen lieu (ca re / sua de truoc), them mon vao tui. Tra ve false neu thieu.</summary>
        public static bool Cook(CookRecipe r, Inventory inv, ItemDatabase items)
        {
            if (!CanCook(r, inv, items)) return false;
            foreach (var (token, n) in r.req)
            {
                int left = n;
                var stacks = inv.Stacks().Where(kv => Matches(items.Get(kv.Key), token))
                                .OrderBy(kv => kv.Key == "goatmilk" ? 0 : 1).ThenBy(kv => items.Get(kv.Key).price).ToList();
                foreach (var kv in stacks)
                {
                    if (left <= 0) break;
                    int k = Math.Min(left, inv.Count(kv.Key));
                    inv.Take(kv.Key, k);
                    left -= k;
                }
            }
            inv.Give(r.id, 1);
            return true;
        }
    }
}
