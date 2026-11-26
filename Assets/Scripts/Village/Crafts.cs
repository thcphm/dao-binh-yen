// Hoc nghe voi tho ca (Docs/ThietKeGameplay.md muc 6, thay giay phep cua ban web - LICS dong 1222):
// moi nghe 3 bac; mang du vat lieu va qua bai thu tay nghe thi len bac. Giai doan 4b co 2 tho ca o lang tu dau:
// Co Sau Vuon (lam vuon) va Bac Chin Do (nghe cau). Cac nghe khac co du lieu, tho ca ve lang khi vung hoi sinh (sau).
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SereneIsle
{
    /// <summary>Yeu cau vat lieu. id thuong = mon cu the; "@crop" / "@fish" = bat ky nong san / ca;
    /// kinds = so loai khac nhau toi thieu; minDifficulty = ca kho toi thieu (ca hiem).</summary>
    public struct CraftReq
    {
        public string id; public int n; public int kinds; public int minDifficulty;
        public CraftReq(string id, int n, int kinds = 0, int minDifficulty = 0) { this.id = id; this.n = n; this.kinds = kinds; this.minDifficulty = minDifficulty; }
    }

    public sealed class CraftLevel
    {
        public CraftReq[] req;
        public string unlock;   // mo ta mo khoa
    }

    public sealed class CraftDef
    {
        public string id, name, master, masterName;
        public bool available;   // tho ca da o lang
        public CraftLevel[] levels;
    }

    public static class Crafts
    {
        public const string Garden = "vuon", Fishing = "cau";
        public const int MaxLevel = 3;

        public static readonly CraftDef[] All =
        {
            new CraftDef
            {
                id = Garden, name = "Làm vườn", master = "sau", masterName = "Cô Sáu Vườn", available = true,
                levels = new[]
                {
                    new CraftLevel { req = new[] { new CraftReq("@crop", 5), new CraftReq("fiber", 4) },
                                     unlock = "Thu hoạch: 25% được thêm 1 trái." },
                    new CraftLevel { req = new[] { new CraftReq("@crop", 12, kinds: 3), new CraftReq("wood", 10) },
                                     unlock = "Sạp Cô Sáu bán thêm 2 giống và cây giống ăn trái, hạt giống rẻ 20%." },
                    new CraftLevel { req = new[] { new CraftReq("@crop", 25, kinds: 5), new CraftReq("sprinkler", 1) },
                                     unlock = "Thu hoạch: 60% được thêm 1 trái. Cô Sáu bán đủ mọi giống đúng mùa." },
                },
            },
            new CraftDef
            {
                id = Fishing, name = "Nghề câu", master = "chin", masterName = "Bác Chín Đò", available = true,
                levels = new[]
                {
                    new CraftLevel { req = new[] { new CraftReq("@fish", 3), new CraftReq("worm", 3) },
                                     unlock = "Kéo cá: khung xanh rộng hơn." },
                    new CraftLevel { req = new[] { new CraftReq("@fish", 8, kinds: 3) },
                                     unlock = "Cá hiếm cắn câu nhiều hơn 40%." },
                    new CraftLevel { req = new[] { new CraftReq("@fish", 12, kinds: 4), new CraftReq("@fish", 1, minDifficulty: 4) },
                                     unlock = "Cá huyền thoại cắn câu gấp đôi, kéo cá dễ hơn nữa." },
                },
            },
            new CraftDef
            {
                // nghe lan (giai doan 6, Docs muc 7.1): DivingSystem doc cap nay
                id = "lan", name = "Lặn biển", master = "man", masterName = "O Mận", available = true,
                levels = new[]
                {
                    new CraftLevel { req = new[] { new CraftReq("ngheu", 3), new CraftReq("so_huyet", 3) },
                                     unlock = "Kính lặn: bơi ra chỗ nước sâu, lặn đầm nông (sâu tới 1 sải), nín thở 15 giây." },
                    new CraftLevel { req = new[] { new CraftReq("rong_sun", 4), new CraftReq("oc_huong", 3), new CraftReq("nhum", 2) },
                                     unlock = "Chân vịt: bơi nhanh hơn, lặn rạn gần bờ (sâu tới 1,4 sải), nín thở 30 giây." },
                    new CraftLevel { req = new[] { new CraftReq("hai_sam", 3), new CraftReq("bao_ngu", 2), new CraftReq("manh_san_ho", 3) },
                                     unlock = "Đèn lặn: lặn mọi chỗ sâu, lặn được ban đêm, nín thở 45 giây." },
                },
            },
            Later("luoi", "Đan lưới", "tu", "Bà Tư Lưới"),
            Later("muoi", "Làm muối", "nam", "Chú Năm Diêm"),
            Later("mam", "Làm nước mắm", "ba", "Dì Ba Mắm"),
            Later("gom", "Gốm", "hai", "Ông Hai Lò"),
            Later("ghe", "Đóng ghe", "ut", "Chú Út Ghe"),
        };

        static CraftDef Later(string id, string name, string master, string masterName)
            => new CraftDef { id = id, name = name, master = master, masterName = masterName, available = false, levels = new CraftLevel[0] };

        public static CraftDef Get(string id) => Array.Find(All, c => c.id == id);
        public static IEnumerable<CraftDef> TaughtBy(string npc) => All.Where(c => c.master == npc && c.available);

        // ------------------------------------------------------------------ cap hien tai (luu trong Village)
        public static int Level(string craft) => Village.Instance ? Village.Instance.CraftLevel(craft) : 0;

        // ------------------------------------------------------------------ hieu ung
        /// <summary>Lam vuon: so trai thu them khi thu hoach.</summary>
        public static int HarvestBonus(int gardenLevel, System.Random rng)
        {
            float p = gardenLevel >= 3 ? .6f : gardenLevel >= 1 ? .25f : 0;
            return p > 0 && rng.NextDouble() < p ? 1 : 0;
        }

        /// <summary>Nghe cau: khung keo ca rong them.</summary>
        public static float ReelZoneBonus(int fishLevel) => fishLevel >= 3 ? .1f : fishLevel >= 1 ? .06f : 0;

        /// <summary>Nghe cau: nhan trong so ca hiem (do kho 3+) va ca huyen thoai (5).</summary>
        public static float RareWeight(int fishLevel, int difficulty)
        {
            float w = 1;
            if (fishLevel >= 2 && difficulty >= 3) w *= 1.4f;
            if (fishLevel >= 3 && difficulty >= 5) w *= 2f;
            return w;
        }

        // ------------------------------------------------------------------ vat lieu
        static bool Matches(ItemDefinition d, CraftReq r)
        {
            if (!d) return false;
            if (r.id == "@crop") return d.category == ItemCategory.Crop;
            if (r.id == "@fish") return d.category == ItemCategory.Fish && d.fishDifficulty >= r.minDifficulty;
            return d.id == r.id;
        }

        /// <summary>Dang co bao nhieu (va bao nhieu loai) cho 1 yeu cau.</summary>
        public static (int have, int kinds) Count(Inventory inv, ItemDatabase items, CraftReq r)
        {
            int have = 0, kinds = 0;
            foreach (var kv in inv.Stacks())
                if (Matches(items.Get(kv.Key), r)) { have += kv.Value; kinds++; }
            return (have, kinds);
        }

        public static bool Meets(Inventory inv, ItemDatabase items, CraftReq r)
        {
            var (have, kinds) = Count(inv, items, r);
            return have >= r.n && kinds >= r.kinds;
        }

        public static bool CanAdvance(CraftDef c, int level, Inventory inv, ItemDatabase items)
            => c != null && c.available && level < c.levels.Length && c.levels[level].req.All(r => Meets(inv, items, r));

        public static string ReqText(CraftReq r, ItemDatabase items)
        {
            string what = r.id == "@crop" ? "nông sản bất kỳ" : r.id == "@fish" ? (r.minDifficulty >= 4 ? "cá hiếm (4 sao trở lên)" : "cá bất kỳ") : items.NameOf(r.id);
            return $"{r.n} {what}" + (r.kinds > 1 ? $" ({r.kinds} loại khác nhau)" : "");
        }

        /// <summary>Lay vat lieu (mon re truoc; giu du so loai). Goi sau khi CanAdvance = true.</summary>
        public static void Consume(CraftLevel lv, Inventory inv, ItemDatabase items)
        {
            // yeu cau cu the truoc, roi den yeu cau kho (ca hiem), roi chung chung
            foreach (var r in lv.req.OrderBy(q => q.id.StartsWith("@") ? (q.minDifficulty > 0 ? 1 : 2) : 0))
            {
                var stacks = inv.Stacks().Where(kv => Matches(items.Get(kv.Key), r))
                                .OrderBy(kv => items.Get(kv.Key).price).ToList();
                int left = r.n;
                // lay 1 cai moi loai truoc cho du so loai (khong lam loai nao het de giu lai cho lan sau)
                foreach (var kv in stacks)
                {
                    if (left <= 0) break;
                    inv.Take(kv.Key, 1); left--;
                }
                foreach (var kv in stacks)
                {
                    if (left <= 0) break;
                    int k = Math.Min(left, inv.Count(kv.Key));
                    if (k > 0) { inv.Take(kv.Key, k); left -= k; }
                }
            }
        }

        // ------------------------------------------------------------------ bai thu tay nghe
        /// <summary>Do kho bai thu: be rong vung xanh (0..1) va toc do kim (vong/giay) theo bac sap len.</summary>
        public static (float zone, float speed) TestParams(int nextLevel)
            => nextLevel switch { 1 => (.26f, .55f), 2 => (.19f, .75f), _ => (.13f, .95f) };

        public const int TestHits = 3, TestMisses = 3;
    }
}
