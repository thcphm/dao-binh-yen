// Cua hang trong lang (Docs/ThietKeGameplay.md muc 6, 4.8):
//  - Tiem tap hoa Thim Tam: ban nuoc, banh; thu mua moi mon hang ngay voi 50% gia goc.
//  - Sap hat giong Co Sau Vuon: 6-8 loai dung mua, doi moi ngay (theo ngay am, co dinh trong ngay).
// Thuan logic (khong UI) -> test duoc.
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SereneIsle
{
    public struct ShopOffer
    {
        public string id;
        public int price;
    }

    public static class Shops
    {
        public const string TapHoa = "taphoa", HatGiong = "hatgiong", ConGiong = "giong";

        /// <summary>Hang tap hoa co dinh (ban web: shopBuyList - bottle, pie...).</summary>
        static readonly string[] GroceryStock = { "bottle", "coconut", "pie", "bread" };

        public static string Title(string shop) => shop == HatGiong ? "Sạp hạt giống · Cô Sáu Vườn"
                                                 : shop == ConGiong ? "Con giống chở từ đất liền · Bác Chín Đò" : "Tiệm tạp hóa · Thím Tám";

        /// <summary>Gia mua vao cua nguoi choi. Hat giong: dung gia hat; con lai: 1.6 x gia goc.</summary>
        public static int BuyPrice(ItemDefinition d)
        {
            if (!d) return 0;
            if (d.category == ItemCategory.Seed) return Mathf.Max(1, d.price);
            return Mathf.Max(1, Mathf.CeilToInt(d.price * 1.6f));
        }

        /// <summary>Thim Tam thu mua: 50% gia goc (lam tron xuong, it nhat 1 xu).</summary>
        public static int SellPrice(ItemDefinition d) => d && d.price > 0 ? Mathf.Max(1, d.price / 2) : 0;

        /// <summary>Do ban duoc cho tiem: do trong tui co gia (khong ban do dat xuong dat da che tao).</summary>
        public static bool CanSell(ItemDefinition d) => d && d.GoesInBag && d.price > 0 && d.category != ItemCategory.Placeable;

        public static bool Buys(string shop) => shop == TapHoa;

        /// <summary>Danh sach hang ban hom nay.</summary>
        /// <param name="gardenLevel">bac nghe lam vuon: 2 = them 2 giong + cay giong, re 20%; 3 = du moi giong dung mua.</param>
        public static List<ShopOffer> Offers(string shop, int day, ItemDatabase items, CropDatabase crops, int gardenLevel = 0)
        {
            var list = new List<ShopOffer>();
            if (shop == TapHoa)
            {
                foreach (var id in GroceryStock)
                {
                    var d = items ? items.Get(id) : null;
                    if (d) list.Add(new ShopOffer { id = id, price = BuyPrice(d) });
                }
                return list;
            }
            if (shop == ConGiong)
            {
                foreach (var a in AnimalDefs.All)
                    if (a.role == AnimalRole.Farm && items && items.Get(a.calf)) list.Add(new ShopOffer { id = a.calf, price = a.price });
                return list;
            }
            if (shop != HatGiong || !crops || !items) return list;

            var season = LunarCalendar.Date(day).Season;
            var annual = new List<CropDefinition>();
            var trees = new List<CropDefinition>();
            foreach (var c in crops.crops)
            {
                if (c == null || !items.Get(c.SeedId)) continue; // ke ca lua, nep, sen, sung (ruong nuoc da co)
                if (c.kind == CropKind.Tree) trees.Add(c);
                else if (c.InSeason(season)) annual.Add(c);
            }
            // tron theo ngay (co dinh trong ngay, doi moi sang)
            var rng = new System.Random(day * 7919 + 31);
            Shuffle(annual, rng);
            Shuffle(trees, rng);
            int n = gardenLevel >= 3 ? annual.Count : Math.Min(annual.Count, 6 + day % 2 + (gardenLevel >= 2 ? 2 : 0));
            float mul = gardenLevel >= 2 ? .8f : 1f;
            for (int i = 0; i < n; i++) list.Add(Offer(items, annual[i].SeedId, mul));
            int nt = Math.Min(trees.Count, gardenLevel >= 2 ? 3 : 1);   // cay giong an trai
            for (int i = 0; i < nt; i++) list.Add(Offer(items, trees[i].SeedId, mul));
            return list;
        }

        static ShopOffer Offer(ItemDatabase items, string id, float mul = 1)
            => new ShopOffer { id = id, price = Mathf.Max(1, Mathf.RoundToInt(BuyPrice(items.Get(id)) * mul)) };

        static void Shuffle<T>(List<T> l, System.Random r)
        {
            for (int i = l.Count - 1; i > 0; i--)
            {
                int j = r.Next(i + 1);
                (l[i], l[j]) = (l[j], l[i]);
            }
        }

        /// <summary>Mua n mon. Tra ve thong bao (null = khong mua duoc gi).</summary>
        public static string Buy(GameSession s, ShopOffer o, int n, out bool ok)
        {
            ok = false;
            var d = s.Items.Get(o.id);
            if (!d || n <= 0) return null;
            if (d.category == ItemCategory.Animal)
            {
                // con giong: ve thang chuong, khong vao tui
                if (s.Coins < o.price) return $"Không đủ tiền mua {d.displayName} ({o.price} xu).";
                if (!Animals.Instance) return null;
                var msg = Animals.Instance.BuyFarm(o.id, out ok);
                if (ok) s.Coins -= o.price;
                return msg;
            }
            int afford = o.price > 0 ? s.Coins / o.price : n;
            int room = s.Inventory.RoomFor(o.id);
            int k = Math.Min(n, Math.Min(afford, room));
            if (afford <= 0) return $"Không đủ tiền mua {d.displayName} ({o.price} xu).";
            if (room <= 0) return "Túi đầy rồi, bán bớt hoặc cất đồ đã.";
            s.Coins -= k * o.price;
            GameAudio.Play(Sfx.Coin);
            s.Inventory.Give(o.id, k);
            ok = true;
            return $"Mua {k} {d.displayName}: -{k * o.price} xu.";
        }

        /// <summary>Ban n mon cho tiem tap hoa (n &lt; 0 = ban het).</summary>
        public static string Sell(GameSession s, string id, int n, out bool ok)
        {
            ok = false;
            var d = s.Items.Get(id);
            if (!CanSell(d)) return null;
            int have = s.Inventory.Count(id);
            int k = n < 0 ? have : Math.Min(n, have);
            if (k <= 0) return null;
            int got = k * SellPrice(d);
            s.Inventory.Take(id, k);
            s.Coins += got;
            GameAudio.Play(Sfx.Coin);
            Journal.Stat("earn", got);
            ok = true;
            return $"Bán {k} {d.displayName}: +{got} xu.";
        }
    }
}
