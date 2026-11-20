// Cong thuc o ban che tao (ban web: RECIPES dong 325-337).
using System.Collections.Generic;
using System.Linq;

namespace SereneIsle
{
    public sealed class Recipe
    {
        public string id;          // do lam ra (id item)
        public string tool;        // lam ra dung cu (so huu)
        public int bag;            // nang cap tui len bac nay
        public (string id, int n)[] req;
        public string desc;
    }

    public static class Recipes
    {
        static Recipe R(string id, string desc, params (string, int)[] req) => new Recipe { id = id, desc = desc, req = req };

        public static readonly Recipe[] All =
        {
            new Recipe { id = "pick", tool = "pick", req = new[] { ("wood", 4), ("stone", 3) }, desc = "Đập đá to, lấy quặng" },
            new Recipe { id = "rod", tool = "rod", req = new[] { ("wood", 3), ("fiber", 2) }, desc = "Câu cá ở biển và ao" },
            new Recipe { id = "net", tool = "net", req = new[] { ("wood", 2), ("fiber", 3) }, desc = "Bắt côn trùng" },
            new Recipe { id = "bag1", bag = 1, req = new[] { ("fiber", 12), ("wood", 4) }, desc = "Túi đồ 8 → 16 ô" },
            new Recipe { id = "bag2", bag = 2, req = new[] { ("fiber", 20), ("shell", 6), ("ore", 4) }, desc = "Túi đồ 16 → 24 ô (cần Ba lô vải)" },
            R("campfire", "Nấu cá, toả sáng ban đêm", ("wood", 5), ("stone", 3)),
            R("torch", "Đèn nhỏ cắm đường", ("wood", 1), ("fiber", 1)),
            R("fence", "Rào ruộng cho gọn", ("wood", 3)),
            R("path", "Lát nền đá, đi lại đẹp", ("stone", 2)),
            R("scarecrow", "Đứng canh ruộng", ("wood", 4), ("fiber", 3)),
            R("sprinkler", "Mỗi sáng tưới 8 ô quanh nó", ("ore", 3), ("stone", 4)),
        };

        public static Recipe Get(string id) => All.FirstOrDefault(r => r.id == id);

        public static bool CanPay(Recipe r, Inventory inv) => r.req.All(q => inv.Count(q.id) >= q.n);

        /// <summary>Da co roi thi khong lam nua (dung cu da so huu, tui da dat bac).</summary>
        public static bool AlreadyHave(Recipe r, GameSession s)
            => (r.tool != null && s.OwnedTools.Contains(r.tool)) || (r.bag > 0 && s.Inventory.BagLevel >= r.bag);

        /// <summary>Tui bac 2 can co tui bac 1 truoc.</summary>
        public static bool Locked(Recipe r, GameSession s) => r.bag > 0 && s.Inventory.BagLevel != r.bag - 1;

        public static bool CanCraft(Recipe r, GameSession s) => CanPay(r, s.Inventory) && !AlreadyHave(r, s) && !Locked(r, s);

        /// <summary>Lam do: tru vat lieu, them do. Tra ve thong bao.</summary>
        public static string Craft(Recipe r, GameSession s)
        {
            if (!CanCraft(r, s)) return null;
            foreach (var (id, n) in r.req) s.Inventory.Take(id, n);
            var name = s.Items.NameOf(r.id);
            if (r.bag > 0) { s.Give(r.id, 1, true); return $"Đã làm {name}! Túi giờ chứa {s.Inventory.Capacity} ô."; }
            if (r.tool != null) { s.Give(r.tool, 1, true); return $"Đã làm {name}!"; }
            s.Give(r.id, 1, true);
            return $"Đã làm {name}.";
        }

        public static string ReqText(Recipe r, Inventory inv)
            => string.Join(" · ", r.req.Select(q => $"{ItemDatabase.Instance.NameOf(q.id)} {inv.Count(q.id)}/{q.n}"));
    }
}
