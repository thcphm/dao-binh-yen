// Ruong nuoc, muong, dat trung + bo bao, bon phan (Docs/ThietKeGameplay.md muc 4.3 - 4.6).
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace SereneIsle.Tests
{
    public class PaddyTests
    {
        static CropDefinition Crop(string id) => CropCatalogData.CreateAll().First(c => c.id == id);

        [Test]
        public void Muong_NoiToiNuocNgot_MoiCoNuoc_ORuongCanhMuongNgap()
        {
            const int n = 5;                                  // luoi 5x5, o (0,0) la suoi
            bool Fresh(int k) => k == 0;
            var ditches = new HashSet<int> { 1, 2, 20 };      // (1,0),(2,0) noi voi suoi; (0,4) le loi
            var wet = FarmWater.WetDitches(ditches, n, Fresh);
            CollectionAssert.AreEquivalent(new[] { 1, 2 }, wet);
            Assert.IsTrue(FarmWater.Flooded(7, n, wet, Fresh), "(2,1) sat muong co nuoc");
            Assert.IsTrue(FarmWater.Flooded(5, n, wet, Fresh), "(0,1) sat suoi");
            Assert.IsFalse(FarmWater.Flooded(21, n, wet, Fresh), "(1,4) sat muong kho thi khong ngap");
            Assert.IsFalse(FarmWater.Flooded(24, n, wet, Fresh));
            Assert.IsTrue(FarmWater.CanDig(3, n, ditches, Fresh), "noi dai muong");
            Assert.IsFalse(FarmWater.CanDig(12, n, ditches, Fresh), "giua dong, khong ke nuoc");
            // muong khong qua mep luoi: (4,0) va (0,1) khong ke nhau
            Assert.IsFalse(FarmWater.Flooded(4, n, new HashSet<int> { 5 }, k => false));
        }

        [Test]
        public void LuaVaSen_CanRuongNgapNuoc()
        {
            var lua = Crop("lua");
            var p = new FarmPlot { soil = SoilType.PhuSa };
            StringAssert.Contains("ruộng ngập nước", FarmRules.CanPlant(p, lua, Season.Xuan, false));
            Assert.IsNull(FarmRules.CanPlant(p, lua, Season.Xuan, true));
            Assert.IsNotNull(FarmRules.CanPlant(new FarmPlot { ditch = true }, Crop("cai_be"), Season.Xuan, true), "khong gieo tren muong");
        }

        [Test]
        public void DatTrung_NuocRongTran_ChetMan_BoBaoThiAnToan()
        {
            var c = Crop("cai_be");
            var p = new FarmPlot { soil = SoilType.CatPha, lowland = true };
            FarmRules.Plant(p, c);
            Assert.IsFalse(FarmRules.SaltFlood(p, .7f, .6f), "nuoc chua toi");
            Assert.IsTrue(FarmRules.SaltFlood(p, .7f, .75f));
            Assert.IsTrue(p.dead); Assert.AreEqual("nhiễm mặn", p.deadReason);
            Assert.AreEqual(FarmRules.SaltDays, p.saltDays);

            FarmRules.Clear(p);
            StringAssert.Contains("mặn", FarmRules.CanPlant(p, c, Season.Xuan));
            p.watered = true; FarmRules.NewDay(p, null, Season.Xuan);
            Assert.AreEqual(FarmRules.SaltDays - 2, p.saltDays, "tuoi nuoc ngot rua man nhanh gap doi");
            FarmRules.NewDay(p, null, Season.Xuan);
            Assert.AreEqual(FarmRules.SaltDays - 3, p.saltDays);

            var safe = new FarmPlot { soil = SoilType.CatPha, lowland = true, dike = true };
            FarmRules.Plant(safe, c);
            Assert.IsFalse(FarmRules.SaltFlood(safe, .7f, .9f), "co bo bao");
            Assert.IsFalse(FarmRules.SaltFlood(new FarmPlot { lowland = false }, .7f, .9f), "dat cao");
        }

        [Test]
        public void BonPhan_LonNhanhHon_ItNhatHangNgon()
        {
            var c = Crop("cu_cai");                           // 8 ngay, hop cat pha / phu sa
            var p = new FarmPlot { soil = SoilType.DatDo };   // sai dat: 0.5 ngay moi ngay
            Assert.IsNotNull(FarmRules.CanFertilize(p), "chua co cay");
            FarmRules.Plant(p, c);
            Assert.IsNull(FarmRules.CanFertilize(p));
            p.fertilized = true;
            Assert.IsNotNull(FarmRules.CanFertilize(p), "bon roi");
            p.watered = true; FarmRules.NewDay(p, c, Season.Xuan);
            Assert.AreEqual(1f, p.growth, 1e-4f, "0.5 + 0.5 phan");
            FarmRules.NewDay(p, c, Season.Xuan);                // quen tuoi 1 ngay
            Assert.AreEqual(1, FarmRules.Quality(p, c), "sai dat + quen tuoi nhung co bon phan: ngon");
            p.fertilized = false;
            Assert.AreEqual(0, FarmRules.Quality(p, c));
            FarmRules.Clear(p);
            Assert.IsFalse(p.fertilized);
        }
    }
}
