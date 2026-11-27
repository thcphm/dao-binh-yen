// Giai doan 4c: du lieu thu (loai, vung, vat nuoi, gia giong) va cua hang con giong.
using System.Linq;
using NUnit.Framework;

namespace SereneIsle.Tests
{
    public class AnimalLogicTests
    {
        static ItemDatabase _items;
        static ItemDatabase Items => _items ? _items : (_items = ItemDatabase.CreateInMemory());

        [Test]
        public void DuLieuThu()
        {
            var wild = AnimalDefs.All.Where(a => a.role == AnimalRole.Wild).Select(a => a.id).ToList();
            CollectionAssert.AreEquivalent(new[] { "tho", "huou", "khi", "garung", "vit", "rua", "cua", "denui" }, wild, "8 loai ban web");
            foreach (var a in AnimalDefs.All.Where(a => a.role == AnimalRole.Wild))
                Assert.IsNotEmpty(a.biomes, a.id);
            foreach (var a in AnimalDefs.All.Where(a => a.role == AnimalRole.Farm))
            {
                Assert.IsTrue(Items.Get(a.calf), "item giong " + a.calf);
                Assert.IsTrue(Items.Get(a.product), "san pham " + a.product);
                Assert.IsTrue(Items.Get(a.pen), "chuong " + a.pen);
                Assert.AreEqual(ItemCategory.Placeable, Items.Get(a.pen).category);
                Assert.Greater(AnimalDefs.PenCapacity(a.pen), 0);
                Assert.Greater(a.price, 0);
                Assert.AreEqual(a, AnimalDefs.ByCalf(a.calf));
            }
            Assert.IsNotNull(AnimalDefs.Get("cho"));
            Assert.IsNotNull(AnimalDefs.Get("meo"));
            foreach (var a in AnimalDefs.All.Where(a => a.quaternius == null))
                Assert.Greater(AnimalDefs.MeshOf(a.id).vertexCount, 20, "khoi dung bang code " + a.id);
        }

        [Test]
        public void ChuongVaCongThuc()
        {
            CollectionAssert.Contains(Placeables.Types, "chuong_ga");
            CollectionAssert.Contains(Placeables.Types, "chuong_trau");
            Assert.IsNotNull(Recipes.Get("chuong_ga"));
            Assert.IsNotNull(Recipes.Get("chuong_trau"));
            Assert.AreEqual(4, AnimalDefs.PenCapacity("chuong_ga"));
        }

        [Test]
        public void CuaHangConGiong()
        {
            var offers = Shops.Offers(Shops.ConGiong, 1, Items, null);
            CollectionAssert.AreEquivalent(new[] { "a_chick", "a_goat", "a_cow", "a_trau", "tom_giong" }, offers.Select(o => o.id));
            Assert.IsFalse(Shops.Buys(Shops.ConGiong));
            Assert.AreEqual(Shops.ConGiong, NpcDefs.Get("chin").shop);
        }
    }
}
