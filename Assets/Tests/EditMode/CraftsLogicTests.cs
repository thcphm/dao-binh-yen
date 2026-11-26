// Giai doan 4b: hoc nghe (vat lieu, hieu ung, bai thu) va gia cho phien.
using System.Linq;
using NUnit.Framework;

namespace SereneIsle.Tests
{
    public class CraftsLogicTests
    {
        static ItemDatabase _items;
        static ItemDatabase Items => _items ? _items : (_items = ItemDatabase.CreateInMemory());

        [Test]
        public void DuLieuNghe()
        {
            var taught = Crafts.All.Where(c => c.available).Select(c => c.id).ToList();
            CollectionAssert.AreEquivalent(new[] { Crafts.Garden, Crafts.Fishing, "lan" }, taught, "4b: vuon + cau; giai doan 6 them lan bien (O Man)");
            foreach (var c in Crafts.All.Where(c => c.available))
            {
                Assert.AreEqual(Crafts.MaxLevel, c.levels.Length, c.id);
                Assert.IsNotNull(NpcDefs.Get(c.master), $"tho ca {c.master} phai la dan lang");
                foreach (var lv in c.levels)
                    foreach (var r in lv.req)
                        Assert.IsTrue(r.id.StartsWith("@") || Items.Get(r.id), $"{c.id}: thieu mon {r.id}");
            }
            Assert.AreEqual(8, Crafts.All.Length, "7 nghe trong thiet ke + nghe cau");
            CollectionAssert.AreEquivalent(new[] { Crafts.Garden }, Crafts.TaughtBy("sau").Select(c => c.id));
        }

        [Test]
        public void VatLieu_DuLoai_LayReTruoc()
        {
            var inv = new Inventory(Items) { BagLevel = 2 };
            var garden = Crafts.Get(Crafts.Garden);
            Assert.IsFalse(Crafts.CanAdvance(garden, 0, inv, Items));
            inv.Give("cai_be", 3); inv.Give("dua_leo", 2); inv.Give("fiber", 4);
            Assert.IsTrue(Crafts.CanAdvance(garden, 0, inv, Items), "5 nong san bat ky + 4 co gai");
            Assert.IsFalse(Crafts.CanAdvance(garden, 1, inv, Items), "bac 2 can 12 nong san 3 loai");
            Crafts.Consume(garden.levels[0], inv, Items);
            Assert.AreEqual(0, inv.Count("cai_be") + inv.Count("dua_leo"));
            Assert.AreEqual(0, inv.Count("fiber"));

            // so loai khac nhau
            inv.Give("cai_be", 20); inv.Give("wood", 10);
            var r = garden.levels[1].req[0];
            Assert.IsFalse(Crafts.Meets(inv, Items, r), "20 cai be nhung chi 1 loai");
            inv.Give("dua_leo", 1); inv.Give("xa_lach", 1);
            Assert.IsTrue(Crafts.Meets(inv, Items, r));
            Crafts.Consume(garden.levels[1], inv, Items);
            Assert.AreEqual(10, inv.Count("cai_be"), "lay 12 = 1 dua leo + 1 xa lach + 10 cai be");
        }

        [Test]
        public void CaHiem_LayTruoc()
        {
            var inv = new Inventory(Items) { BagLevel = 2 };
            var fish = Items.items.Where(i => i.category == ItemCategory.Fish).OrderBy(i => i.fishDifficulty).ToList();
            var rare = fish.Last(f => f.fishDifficulty >= 4);
            var commons = fish.Where(f => f.fishDifficulty <= 2).Take(4).ToList();
            foreach (var f in commons) inv.Give(f.id, 3);
            var lv3 = Crafts.Get(Crafts.Fishing).levels[2];
            Assert.IsFalse(Crafts.Meets(inv, Items, lv3.req[1]), "chua co ca hiem");
            inv.Give(rare.id, 1);
            Assert.IsTrue(lv3.req.All(q => Crafts.Meets(inv, Items, q)));
            Crafts.Consume(lv3, inv, Items);
            Assert.AreEqual(0, inv.Count(rare.id), "ca hiem bi lay cho yeu cau ca hiem");
            Assert.AreEqual(0, inv.TotalItems, "12 ca thuong + 1 ca hiem");
        }

        [Test]
        public void HieuUng()
        {
            var rng = new System.Random(1);
            Assert.AreEqual(0, Enumerable.Range(0, 200).Sum(_ => Crafts.HarvestBonus(0, rng)));
            int l1 = Enumerable.Range(0, 2000).Sum(_ => Crafts.HarvestBonus(1, rng));
            int l3 = Enumerable.Range(0, 2000).Sum(_ => Crafts.HarvestBonus(3, rng));
            Assert.That(l1, Is.InRange(400, 600), "25%");
            Assert.That(l3, Is.InRange(1100, 1300), "60%");
            Assert.AreEqual(0, Crafts.ReelZoneBonus(0));
            Assert.Greater(Crafts.ReelZoneBonus(3), Crafts.ReelZoneBonus(1));
            Assert.AreEqual(1, Crafts.RareWeight(1, 4));
            Assert.AreEqual(1.4f, Crafts.RareWeight(2, 4), 1e-4);
            Assert.AreEqual(2.8f, Crafts.RareWeight(3, 5), 1e-4);
            var (z1, s1) = Crafts.TestParams(1);
            var (z3, s3) = Crafts.TestParams(3);
            Assert.Greater(z1, z3, "bac cao vung xanh hep hon");
            Assert.Less(s1, s3, "bac cao kim nhanh hon");
        }

        [Test]
        public void SapHatGiong_TheoBacNghe()
        {
            var crops = CropDatabase.CreateInMemory();
            for (int day = 1; day < 60; day += 7)
            {
                var l0 = Shops.Offers(Shops.HatGiong, day, Items, crops, 0);
                var l2 = Shops.Offers(Shops.HatGiong, day, Items, crops, 2);
                var l3 = Shops.Offers(Shops.HatGiong, day, Items, crops, 3);
                Assert.Greater(l2.Count, l0.Count, "bac 2 ban them");
                Assert.GreaterOrEqual(l3.Count, l2.Count - 2);
                var a = l0.First(); var b = l2.First(o => o.id == a.id);
                Assert.Less(b.price, a.price + 1, "bac 2 re hon");
            }
        }

        [Test]
        public void GiaChoPhien()
        {
            var melon = Items.Get("bottle");
            Assert.AreEqual(6, Market.PriceOf(melon, 100));
            Assert.AreEqual(10, Market.PriceOf(melon, 160));
            Assert.AreEqual(4, Market.PriceOf(melon, 60));
            Assert.IsTrue(Market.IsMarketDay(7));
            Assert.IsFalse(Market.IsMarketDay(8));
            Assert.IsTrue(Market.IsMarketDay(14));
        }
    }
}
