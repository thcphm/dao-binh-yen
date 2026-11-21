// Giai doan 6b/6c: cong thuc nau an (nguyen lieu, ca bat ky lay re truoc, sua de truoc), cot moc so tay, thu trong chai.
using System.Linq;
using NUnit.Framework;

namespace SereneIsle.Tests
{
    public class CookingLogicTests
    {
        static ItemDatabase _items;
        static ItemDatabase Items => _items ? _items : (_items = ItemDatabase.CreateInMemory());

        [Test]
        public void CongThuc_DuMon()
        {
            Assert.AreEqual(14, Cooking.All.Length);
            Assert.AreEqual(Cooking.All.Length, Cooking.All.Select(r => r.id).Distinct().Count(), "khong trung id");
            foreach (var r in Cooking.All)
            {
                var d = Items.Get(r.id);
                Assert.IsTrue(d, "thieu mon an " + r.id);
                Assert.AreEqual(ItemCategory.Food, d.category, r.id + " la do an");
                Assert.IsTrue(d.IsFood, r.id + " phai an duoc");
                foreach (var q in r.req)
                    Assert.IsTrue(q.id.StartsWith("@") || Items.Get(q.id), $"{r.id}: thieu nguyen lieu {q.id}");
            }
        }

        [Test]
        public void Nau_TruNguyenLieu_CaReTruoc()
        {
            var inv = new Inventory(Items) { BagLevel = 2 };
            var fish = Items.items.Where(d => d.category == ItemCategory.Fish).OrderBy(d => d.price).ToList();
            Assert.GreaterOrEqual(fish.Count, 2);
            var cheap = fish[0]; var dear = fish[fish.Count - 1];
            var kho = Cooking.Get("ca_kho");
            Assert.IsFalse(Cooking.CanCook(kho, inv, Items));
            inv.Give(dear.id, 1); inv.Give(cheap.id, 1); inv.Give("ot", 1);
            Assert.AreEqual(2, Cooking.Have(inv, Items, "@fish"));
            Assert.IsTrue(Cooking.CanCook(kho, inv, Items));
            StringAssert.Contains("cá bất kỳ (2)", Cooking.ReqText(kho, inv, Items));
            Assert.IsTrue(Cooking.Cook(kho, inv, Items));
            Assert.AreEqual(1, inv.Count("ca_kho"));
            Assert.AreEqual(0, inv.Count(cheap.id), "dung ca re truoc");
            Assert.AreEqual(1, inv.Count(dear.id), "giu ca dat");
            Assert.AreEqual(0, inv.Count("ot"));
            Assert.IsFalse(Cooking.Cook(kho, inv, Items), "het ot");
        }

        [Test]
        public void Nau_SuaDeTruoc()
        {
            var inv = new Inventory(Items) { BagLevel = 2 };
            inv.Give("pumpkin", 1); inv.Give("milk", 1); inv.Give("goatmilk", 1);
            Assert.IsTrue(Cooking.Cook(Cooking.Get("stew"), inv, Items));
            Assert.AreEqual(1, inv.Count("milk"));
            Assert.AreEqual(0, inv.Count("goatmilk"), "sua de dung truoc");
            Assert.AreEqual(1, inv.Count("stew"));
        }

        [Test]
        public void SoTay_DuLieu()
        {
            Assert.AreEqual(12, Journal.Miles.Length);
            foreach (var m in Journal.Miles)
            {
                Assert.AreEqual(3, m.t.Length, m.id);
                Assert.IsTrue(m.t[0] < m.t[1] && m.t[1] < m.t[2], m.id + " moc tang dan");
            }
            Assert.AreEqual(8, Journal.Pages.Length, "8 trang nhat ky");
            Assert.AreEqual(0, Journal.Pages[0].calm, "trang dau mo san");
            for (int i = 1; i < Journal.Pages.Length; i++) Assert.Greater(Journal.Pages[i].calm, Journal.Pages[i - 1].calm);
            Assert.AreEqual(4, Journal.Perks.Length);
            Assert.AreEqual(10, Bottles.Letters.Length, "10 la thu trong chai");
        }
    }
}
