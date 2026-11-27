// Vuon ruong nang cao (Docs/ThietKeGameplay.md muc 4.4 - 4.6, 4.8): xen canh, sau benh + thien dich, giu giong,
// ghep cay + giong lai, luan canh lua tom, nha luoi.
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace SereneIsle.Tests
{
    public class FarmCareTests
    {
        static readonly List<CropDefinition> All = CropCatalogData.CreateAll();
        static CropDefinition Crop(string id) => All.First(c => c.id == id);

        static FarmPlot Grown(string crop, SoilType soil = SoilType.Vuon)
        {
            var p = new FarmPlot { soil = soil };
            FarmRules.Plant(p, Crop(crop));
            return p;
        }

        [Test]
        public void XenCanh_DauKeBen_SaHanhGiamSau_DocCanhDeSau()
        {
            const int n = 6;
            var map = new Dictionary<int, FarmPlot>();
            FarmPlot At(int k) => map.TryGetValue(k, out var p) ? p : null;
            map[7] = Grown("tomato"); map[7].k = 7;
            Assert.IsFalse(FarmCare.NearLegume(7, n, At));
            map[14] = Grown("dau_que"); map[14].k = 14;                 // cheo goc (2,2) canh (1,1)
            Assert.IsTrue(FarmCare.NearLegume(7, n, At), "dau ke ben");
            Assert.IsFalse(FarmCare.NearLegume(14, n, At), "dau khong tu tinh cho no");
            map[14].dead = true;
            Assert.IsFalse(FarmCare.NearLegume(7, n, At), "dau chet khong tinh");

            Assert.IsFalse(FarmCare.NearRepellent(7, n, At));
            map[8] = Grown("sa"); map[8].k = 8;
            Assert.IsTrue(FarmCare.NearRepellent(7, n, At), "sa quanh luong");

            // 13 o ca chua lien nhau = doc canh
            var mono = new Dictionary<int, FarmPlot>();
            for (int k = 0; k < 13; k++) mono[k] = Grown("tomato");
            int size = FarmCare.ClusterSize(0, 20, q => mono.TryGetValue(q, out var p) ? p : null, FarmCare.MonoLimit + 1);
            Assert.Greater(size, FarmCare.MonoLimit);

            double basic = FarmCare.PestChance(30, false, false, false);
            Assert.Greater(FarmCare.PestChance(30, true, false, false), basic * 2.9, "doc canh x3");
            Assert.Less(FarmCare.PestChance(30, false, true, false), basic * .5, "xen sa hanh giam sau");
            Assert.Less(FarmCare.PestChance(60, false, false, false), FarmCare.PestChance(40, false, false, false) * .6, "thien dich o vung 50+");
            Assert.Less(FarmCare.PestChance(30, false, false, true), basic, "nha luoi it sau");
        }

        [Test]
        public void HangNongSan_XenDau_SauBenh_CayAnTraiChuaGhep()
        {
            var c = Crop("tomato");
            var p = Grown("tomato"); p.everDry = true;                    // dung dat, quen tuoi: ngon
            Assert.AreEqual(1, FarmRules.Quality(p, c));
            p.nearLegume = true;
            Assert.AreEqual(2, FarmRules.Quality(p, c), "xen dau +1");
            p.pest = Pest.SauXanh;
            Assert.AreEqual(1, FarmRules.Quality(p, c), "dang bi sau -1");

            var xoai = Crop("xoai");
            var t = Grown("xoai", SoilType.DatDo);
            Assert.AreEqual(0, FarmRules.Quality(t, xoai), "cay giong chua ghep: hang thuong");
            Assert.IsNull(FarmCare.CanGraft(t, xoai, xoai));
            Assert.AreEqual(GraftTrait.None, FarmCare.Graft(t, xoai, xoai));
            Assert.AreEqual(2, FarmRules.Quality(t, xoai), "ghep canh dau dong: dac san");
            Assert.IsNotNull(FarmCare.CanGraft(t, xoai, xoai), "ghep roi");
        }

        [Test]
        public void SauBenh_CayDungLon_KhongTriThiChet_BatTayHoacPhun()
        {
            var c = Crop("cai_be");
            var p = Grown("cai_be");
            Assert.IsTrue(FarmCare.CanGetPest(p, c));
            Assert.AreEqual(Pest.SauXanh, FarmCare.PestFor(c));
            Assert.AreEqual(Pest.OcBuou, FarmCare.PestFor(Crop("lua")));
            Assert.AreEqual(Pest.Ray, FarmCare.PestFor(Crop("ca_rot")));
            p.pest = Pest.SauXanh;
            p.watered = true;
            FarmRules.NewDay(p, c, Season.Xuan);
            Assert.AreEqual(0f, p.growth, "bi sau thi khong lon");
            Assert.IsTrue(FarmCare.Treats(null, Pest.SauXanh), "sau xanh bat tay");
            Assert.IsFalse(FarmCare.Treats(null, Pest.Ray), "ray phai phun");
            Assert.IsTrue(FarmCare.Treats("nuoc_toi_ot", Pest.Ray));
            for (int d = 0; d < FarmCare.PestKillDays; d++) { p.watered = true; FarmRules.NewDay(p, c, Season.Xuan); }
            Assert.IsTrue(p.dead);
            Assert.AreEqual("sâu bệnh", p.deadReason);

            var q = Grown("cai_be"); q.pest = Pest.SauXanh; q.pestDays = 2;
            FarmCare.Cure(q);
            q.watered = true;
            FarmRules.NewDay(q, c, Season.Xuan);
            Assert.AreEqual(1f, q.growth, "tri xong lon lai");
            Assert.IsFalse(FarmCare.CanGetPest(new FarmPlot { crop = "xoai" }, Crop("xoai")), "cay an trai khong bi sau");
        }

        [Test]
        public void GiuGiong_BaVuLienTiep_ThanhGiongNha()
        {
            var c = Crop("cai_be");
            var rng = new System.Random(5);
            var strain = new Strain();
            bool home = false;
            for (int gen = 1; gen <= 3; gen++)
            {
                var p = Grown("cai_be");
                if (gen > 1) FarmCare.ApplyStrain(p, strain);
                Assert.IsNotNull(FarmCare.CanSave(p, c), "chua chin chua chua giong duoc");
                p.growth = c.growDays;
                Assert.IsNull(FarmCare.CanSave(p, c));
                p.saved = true;
                Assert.IsFalse(FarmRules.Harvestable(p, c), "dang de gia");
                for (int d = 0; d < FarmCare.SeedAgeDays; d++) FarmRules.NewDay(p, c, Season.Xuan, true);
                Assert.IsTrue(FarmCare.SeedReady(p));
                Assert.IsTrue(FarmRules.Harvestable(p, c));
                int n = FarmCare.CollectSeeds(p, c, strain, rng, out home);
                Assert.GreaterOrEqual(n, 2);
                Assert.IsTrue(p.Empty, "lay hat xong cay tan");
                Assert.AreEqual(gen, strain.gen);
                Assert.AreEqual(gen == 3, home, "vu thu 3 thanh giong nha");
            }
            strain.name = FarmCare.StrainNames(c.displayName, "Út", "Đồi Sim")[0];
            var child = Grown("cai_be"); child.everDry = true; child.soil = SoilType.DatDo;   // sai dat + quen tuoi: thuong
            Assert.AreEqual(0, FarmRules.Quality(child, c));
            FarmCare.ApplyStrain(child, strain);
            Assert.IsTrue(child.homeStrain);
            Assert.AreEqual(2, FarmRules.Quality(child, c), "hang cay me (dac san) + giong nha");
            Assert.IsNotNull(FarmCare.CanSave(Grown("xoai"), Crop("xoai")), "cay an trai nhan giong bang canh ghep");
        }

        [Test]
        public void GhepKhacLoai_GiongLai_CoDacTinh()
        {
            var traits = new HashSet<GraftTrait>();
            var trees = All.Where(c => c.kind == CropKind.Tree).ToList();
            foreach (var a in trees)
                foreach (var b in trees)
                {
                    if (a == b) continue;
                    var p = Grown(a.id);
                    var t = FarmCare.Graft(p, a, b);
                    Assert.AreNotEqual(GraftTrait.None, t);
                    Assert.AreEqual(t, FarmCare.Graft(Grown(a.id), a, b), "co dinh theo cap cay");
                    traits.Add(t);
                    if (t == GraftTrait.TwoSeasons) Assert.AreEqual((int)b.seasons, p.extraSeasons);
                }
            Assert.AreEqual(3, traits.Count, "du 3 dac tinh");

            // xoai (Ha) lai trai hai mua: ra trai ca mua cua canh ghep
            var xoai = Crop("xoai");
            foreach (var b in trees.Where(t => t != xoai))
            {
                var p = Grown("xoai", SoilType.DatDo);
                if (FarmCare.Graft(p, xoai, b) != GraftTrait.TwoSeasons) continue;
                var other = (Season)System.Array.FindIndex(new[] { Season.Xuan, Season.Ha, Season.Thu, Season.Dong }, s => b.InSeason(s) && !xoai.InSeason(s));
                if ((int)other < 0) continue;
                Assert.IsTrue(FarmRules.FruitSeason(p, xoai, other));
                Assert.IsFalse(xoai.InSeason(other));
                break;
            }
            // chiu man: nuoc rong tran vao khong chet
            var salt = new FarmPlot { lowland = true, crop = "xoai", trait = GraftTrait.SaltTolerant };
            Assert.IsTrue(FarmRules.SaltFlood(salt, 0, 1));
            Assert.IsFalse(salt.dead);
            // trai to: +1 trai
            var big = Grown("xoai", SoilType.DatDo); big.growth = xoai.growDays; big.hasFruit = true; big.graft = "chuoi"; big.trait = GraftTrait.BigFruit;
            var plain = Grown("xoai", SoilType.DatDo); plain.growth = xoai.growDays; plain.hasFruit = true; plain.graft = "chuoi";
            int nb = FarmRules.Harvest(big, xoai, new System.Random(1), out _), np = FarmRules.Harvest(plain, xoai, new System.Random(1), out _);
            Assert.AreEqual(np + 1, nb);
        }

        [Test]
        public void LuanCanhLuaTom_MoCongNuoiTom_DongCongCayLuaChiuMan()
        {
            var lua = Crop("lua_man");
            Assert.IsTrue(lua.saltTolerant && lua.needsPaddy && lua.InSeason(Season.Thu));
            var p = new FarmPlot { soil = SoilType.DatLo, lowland = true };
            StringAssert.Contains("bờ bao", FarmCare.CanBuildSluice(p));
            p.dike = true;
            Assert.IsNull(FarmCare.CanBuildSluice(p));
            p.sluice = true;
            FarmRules.Plant(p, Crop("rau_muong"));
            FarmCare.OpenSluice(p);
            Assert.IsTrue(p.Empty, "nuoc lo tran vao, cay trong o tan");
            Assert.IsNotNull(FarmRules.CanPlant(p, lua, Season.Ha, true), "cong mo khong cay duoc");
            StringAssert.Contains("mùa Hạ", FarmCare.CanStock(p, Season.Xuan));
            Assert.IsNull(FarmCare.CanStock(p, Season.Ha));
            FarmCare.Stock(p);
            for (int d = 0; d < FarmCare.ShrimpDays; d++) { Assert.IsFalse(FarmCare.ShrimpReady(p)); FarmRules.NewDay(p, null, Season.Ha); }
            Assert.IsTrue(FarmCare.ShrimpReady(p));
            Assert.AreEqual(FarmRules.SaltDays, p.saltDays, "cong mo: dat giu man");
            int n = FarmCare.HarvestShrimp(p, new System.Random(2));
            Assert.That(n, Is.InRange(3, 5));
            Assert.IsFalse(FarmCare.CloseSluice(p), "thu tom roi moi dong: khong mat");
            Assert.AreEqual(FarmRules.SaltDays, p.saltDays);
            var thuong = Crop("lua");
            Assert.IsNotNull(FarmRules.CanPlant(p, lua, Season.Thu, true), "dat con man 7 ngay");
            FarmRules.NewDay(p, null, Season.Thu, true);
            FarmRules.NewDay(p, null, Season.Thu, true);   // tuoi nuoc ngot: rua nhanh gap doi -> con 3
            Assert.AreEqual(3, p.saltDays);
            Assert.IsNull(FarmRules.CanPlant(p, lua, Season.Thu, true), "lua chiu man cay duoc");
            Assert.IsNotNull(FarmRules.CanPlant(p, thuong, Season.Thu, true), "lua thuong chua cay duoc");
            FarmRules.Plant(p, lua);
            Assert.IsTrue(p.fertilized, "bun tom bon cho vu lua");
            p.dike = false;
            Assert.IsTrue(FarmRules.SaltFlood(p, 0, 1, lua.saltTolerant));
            Assert.IsFalse(p.dead, "lua chiu man khong chet man");

            // dong cong khi tom chua lon: mat tom
            var q = new FarmPlot { lowland = true, dike = true, sluice = true };
            FarmCare.OpenSluice(q); FarmCare.Stock(q);
            Assert.IsTrue(FarmCare.CloseSluice(q));
        }

        [Test]
        public void NhaLuoi_GieoTraiMua_KhongChetTraiMua()
        {
            var cai = Crop("cai_be");   // mua Xuan
            var p = new FarmPlot { soil = SoilType.Vuon };
            Assert.IsNotNull(FarmRules.CanPlant(p, cai, Season.Ha));
            Assert.IsNull(FarmRules.CanPlant(p, cai, Season.Ha, false, true));
            FarmRules.Plant(p, cai);
            p.watered = true;
            Assert.IsFalse(FarmRules.NewDay(p, cai, Season.Ha, false, true));
            Assert.AreEqual(1f, p.growth);
            Assert.IsTrue(FarmRules.NewDay(p, cai, Season.Ha), "ngoai nha luoi: chet trai mua");
        }

        [Test]
        public void VatPhamMoi_CoTrongKho()
        {
            var items = ItemCatalogData.CreateAll();
            foreach (var id in new[] { "tro_bep", "nuoc_toi_ot", "cong", "tom_giong", "tom_su", "nha_luoi", "canh_xoai", "giong_cai_be", "seed_lua_man", "lua_man" })
                Assert.IsTrue(items.Any(i => i.id == id), id);
            Assert.AreEqual("cai_be", items.First(i => i.id == "giong_cai_be").seedOf);
            Assert.IsFalse(items.Any(i => i.id == "giong_xoai"), "cay an trai khong co hat giu giong");
            foreach (var r in Recipes.All) foreach (var (id, _) in r.req) Assert.IsTrue(items.Any(i => i.id == id), "nguyen lieu " + id);
            Assert.AreEqual(3, Recipes.Get("nha_luoi").craftLevel);
            Assert.IsTrue(System.Array.IndexOf(Placeables.Types, "nha_luoi") >= 0);
        }
    }
}
