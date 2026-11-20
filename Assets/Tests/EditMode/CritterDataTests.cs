// Du lieu con trung: moi vung dat co loai ban ngay va ban dem, kieu di chuyen hop le.
using System.Linq;
using NUnit.Framework;

namespace SereneIsle.Tests
{
    public class CritterDataTests
    {
        [Test]
        public void MoiVung_CoConTrung_NgayVaDem()
        {
            var bugs = ItemCatalogData.CreateAll().Where(i => i.category == ItemCategory.Bug).ToList();
            Assert.GreaterOrEqual(bugs.Count, 24, "it nhat 24 loai");
            Assert.AreEqual(bugs.Count, bugs.Select(b => b.id).Distinct().Count(), "ma loai khong trung");
            for (int bio = 1; bio <= 10; bio++)
            {
                string code = bio.ToString();
                bool Lives(ItemDefinition b) => b.habitats.Contains(code);
                Assert.IsTrue(bugs.Any(b => Lives(b) && b.activeTime != "night"), $"vung {(Biome)bio} phai co con trung ban ngay");
                Assert.IsTrue(bugs.Any(b => Lives(b) && b.activeTime != "day"), $"vung {(Biome)bio} phai co con trung ban dem");
            }
            foreach (var id in new[] { "b_dragonfly", "b_grasshopper", "b_locust", "b_fieldcricket", "b_molecricket", "b_cicada", "b_giantbug" })
                Assert.IsTrue(bugs.Any(b => b.id == id), "thieu " + id);
            Assert.AreEqual(Critters.Kind.Dart, Critters.KindOf("b_dragonfly"));
            Assert.AreEqual(Critters.Kind.Hop, Critters.KindOf("b_grasshopper"));
            Assert.AreEqual(Critters.Kind.Perch, Critters.KindOf("b_cicada"));
            Assert.AreEqual(Critters.Kind.Glow, Critters.KindOf("b_firefly"));
        }
    }
}
