// Giai doan 4: cua hang lang (gia, hang theo mua, doi theo ngay) va du lieu dan lang.
using System.Linq;
using NUnit.Framework;

namespace SereneIsle.Tests
{
    public class VillageLogicTests
    {
        static ItemDatabase _items;
        static CropDatabase _crops;
        static ItemDatabase Items => _items ? _items : (_items = ItemDatabase.CreateInMemory());
        static CropDatabase Crops => _crops ? _crops : (_crops = CropDatabase.CreateInMemory());

        [Test]
        public void GiaMuaBan()
        {
            var bottle = Items.Get("bottle");
            Assert.AreEqual(10, Shops.BuyPrice(bottle), "1.6 x 6 lam tron len");
            Assert.AreEqual(3, Shops.SellPrice(bottle), "Thim Tam mua nua gia");
            var seed = Items.Get("seed_cai_be");
            Assert.AreEqual(seed.price, Shops.BuyPrice(seed), "hat giong ban dung gia hat");
            Assert.GreaterOrEqual(Shops.SellPrice(Items.Get("worm")), 1, "it nhat 1 xu");
            Assert.IsFalse(Shops.CanSell(Items.Get("campfire")), "khong ban do dat xuong dat");
            Assert.IsFalse(Shops.CanSell(Items.Get("axe")), "khong ban dung cu");
            Assert.IsTrue(Shops.CanSell(Items.Get("wood")));
        }

        [Test]
        public void HatGiong_DungMua_DoiTheoNgay()
        {
            for (int day = 1; day <= LunarCalendar.DaysPerYear; day += 9)
            {
                var season = LunarCalendar.Date(day).Season;
                var offers = Shops.Offers(Shops.HatGiong, day, Items, Crops);
                var annual = offers.Select(o => Crops.FromSeed(Items.Get(o.id))).Where(c => c.kind != CropKind.Tree).ToList();
                Assert.That(annual.Count, Is.InRange(5, 7), $"ngay {day}: 6-7 giong ngan ngay (it nhat 5 neu mua it giong)");
                foreach (var c in annual)
                {
                    Assert.IsTrue(c.InSeason(season), $"ngay {day}: {c.id} phai dung mua {season}");
                }
                Assert.AreEqual(offers.Count, offers.Select(o => o.id).Distinct().Count(), "khong trung");
                CollectionAssert.AreEqual(offers.Select(o => o.id), Shops.Offers(Shops.HatGiong, day, Items, Crops).Select(o => o.id), "co dinh trong ngay");
            }
            var a = Shops.Offers(Shops.HatGiong, 3, Items, Crops).Select(o => o.id).ToList();
            var b = Shops.Offers(Shops.HatGiong, 4, Items, Crops).Select(o => o.id).ToList();
            CollectionAssert.AreNotEqual(a, b, "doi hang moi ngay");
        }

        [Test]
        public void TapHoa()
        {
            var offers = Shops.Offers(Shops.TapHoa, 1, Items, Crops);
            CollectionAssert.Contains(offers.Select(o => o.id).ToList(), "bottle");
            Assert.IsTrue(Shops.Buys(Shops.TapHoa));
            Assert.IsFalse(Shops.Buys(Shops.HatGiong));
        }

        [Test]
        public void DanLang()
        {
            CollectionAssert.AreEquivalent(new[] { "bay", "chin", "sau", "tam" }, NpcDefs.All.Select(n => n.id));
            foreach (var n in NpcDefs.All)
            {
                Assert.IsNotEmpty(n.lines, n.id);
                Assert.IsTrue(OutfitBuilder.Supports(n.outfit.model), n.id);
                Assert.Less(n.fromHour, n.toHour);
            }
            Assert.AreEqual(Shops.TapHoa, NpcDefs.Get("tam").shop);
            Assert.AreEqual(Shops.HatGiong, NpcDefs.Get("sau").shop);
            Assert.Greater(NpcDefs.BayIntro.Length, 2);
        }
    }
}
