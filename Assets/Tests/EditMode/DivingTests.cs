// Luat lan bien (Docs/ThietKeGameplay.md muc 7.1, 8).
using System.Linq;
using NUnit.Framework;

namespace SereneIsle.Tests
{
    public class DivingTests
    {
        [Test]
        public void BacNghe_DoSau_HoiTho()
        {
            Assert.IsFalse(DiveRules.CanSwim(0));
            Assert.IsTrue(DiveRules.CanSwim(1));
            Assert.Less(DiveRules.MaxDepth(1), DiveRules.MaxDepth(2));
            Assert.Less(DiveRules.MaxDepth(2), DiveRules.MaxDepth(3));
            Assert.AreEqual(15f, DiveRules.BreathSeconds(1, Season.Ha));
            Assert.AreEqual(30f, DiveRules.BreathSeconds(2, Season.Xuan));
            Assert.AreEqual(45f, DiveRules.BreathSeconds(3, Season.Thu));
            Assert.AreEqual(45f * .7f, DiveRules.BreathSeconds(3, Season.Dong), 1e-4f, "mua Dong nuoc lanh");
            Assert.IsFalse(DiveRules.CanDiveAtNight(2));
            Assert.IsTrue(DiveRules.CanDiveAtNight(3));
            Assert.Greater(DiveRules.SwimSpeed(2, false), DiveRules.SwimSpeed(1, false), "chan vit boi nhanh hon");
        }

        [Test]
        public void LanDuocKhong_TheoDoSauVaBac()
        {
            Assert.IsNotNull(DivingSystem.CanDive(1, .3f, null), "can qua");
            Assert.IsNull(DivingSystem.CanDive(1, .9f, null));
            StringAssert.Contains("chân vịt", DivingSystem.CanDive(1, 1.2f, null));
            Assert.IsNull(DivingSystem.CanDive(2, 1.2f, null));
            StringAssert.Contains("đèn lặn", DivingSystem.CanDive(2, 1.6f, null));
            Assert.IsNull(DivingSystem.CanDive(3, 1.6f, null));
        }

        [Test]
        public void DoDayBien_NongVaSau_CoTrongItem()
        {
            var items = ItemCatalogData.CreateAll().ToDictionary(i => i.id);
            var rng = new System.Random(3);
            var shallow = Enumerable.Range(0, 400).Select(_ => DiveRules.PickLoot(.6f, rng)).Distinct().ToList();
            var deep = Enumerable.Range(0, 400).Select(_ => DiveRules.PickLoot(1.2f, rng)).Distinct().ToList();
            CollectionAssert.Contains(shallow, "rong_sun");
            CollectionAssert.DoesNotContain(shallow, "bao_ngu", "bao ngu o cho sau");
            CollectionAssert.Contains(deep, "bao_ngu");
            CollectionAssert.Contains(deep, "trai_ngoc");
            foreach (var id in shallow.Concat(deep)) Assert.IsTrue(items.ContainsKey(id), id);
        }

        [Test]
        public void HetHoi_MatTuiVa20PhanTramTien_QuaNgay()
        {
            var db = ItemDatabase.CreateInMemory();
            var inv = new Inventory(db);
            inv.Give("wood", 5); inv.Give("hai_sam", 2);
            var st = new PlayerStats();
            int coins = 200;
            var msgs = DayRules.Apply(NewDayReason.Drowned, st, inv, ref coins);
            Assert.AreEqual(0, inv.TotalItems);
            Assert.AreEqual(160, coins);
            Assert.AreEqual(60, st.Energy, "ngat: nang luong 60");
            Assert.IsTrue(msgs.Any(m => m.Contains("40 xu")));
            StringAssert.Contains("O Mận", DayRules.Subtitle(NewDayReason.Drowned));
        }

        [Test]
        public void NgheLan_OMan_3Bac()
        {
            var c = Crafts.Get("lan");
            Assert.IsTrue(c.available);
            Assert.AreEqual("man", c.master);
            Assert.AreEqual(3, c.levels.Length);
            var items = ItemCatalogData.CreateAll().ToDictionary(i => i.id);
            foreach (var lv in c.levels) foreach (var r in lv.req) Assert.IsTrue(items.ContainsKey(r.id), r.id);
            Assert.IsNotNull(NpcDefs.Get("man"));
        }
    }
}
