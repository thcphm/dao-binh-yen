// Lich am, con nuoc, giong cay 4 mua, luat trong trot (Docs/ThietKeGameplay.md muc 2-4, 8).
using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace SereneIsle.Tests
{
    public class Phase3GameplayTests
    {
        // ------------------------------------------------------------ lich am
        [Test]
        public void Lich_28NgayMoiThang_2ThangMoiMua_224NgayMoiNam()
        {
            Assert.AreEqual(224, LunarCalendar.DaysPerYear);
            var d1 = LunarCalendar.Date(1);
            Assert.AreEqual((1, 1, 1, Season.Xuan), (d1.Year, d1.Month, d1.DayOfMonth, d1.Season));
            Assert.AreEqual("Mùng 1 tháng 1", d1.ShortLabel);
            var d29 = LunarCalendar.Date(29);
            Assert.AreEqual((2, 1, Season.Xuan), (d29.Month, d29.DayOfMonth, d29.Season));
            Assert.AreEqual(Season.Ha, LunarCalendar.Date(57).Season, "thang 3 la mua Ha");
            Assert.AreEqual(Season.Thu, LunarCalendar.Date(113).Season);
            Assert.AreEqual(Season.Dong, LunarCalendar.Date(169).Season);
            var y2 = LunarCalendar.Date(225);
            Assert.AreEqual((2, 1, 1), (y2.Year, y2.Month, y2.DayOfMonth));
            Assert.AreEqual("Rằm tháng 1", LunarCalendar.Date(15).ShortLabel);
            Assert.AreEqual(4, LunarCalendar.Date(22).Week);
        }

        [Test]
        public void Lich_LeHoi_VaPhienCho()
        {
            Assert.AreEqual("Tết", LunarCalendar.Festival(LunarCalendar.Date(1)));
            Assert.AreEqual("Rằm tháng giêng", LunarCalendar.Festival(LunarCalendar.Date(15)));
            Assert.AreEqual("Lễ cầu ngư", LunarCalendar.Festival(LunarCalendar.Date(2 * 28 + 15)));
            Assert.AreEqual("Trung thu", LunarCalendar.Festival(LunarCalendar.Date(5 * 28 + 15)));
            Assert.IsNull(LunarCalendar.Festival(LunarCalendar.Date(3)));
            var market = Enumerable.Range(1, 28).Where(d => LunarCalendar.Date(d).IsMarketDay).ToArray();
            CollectionAssert.AreEqual(new[] { 7, 14, 21, 28 }, market, "cho moi ngay thu 7");
            var big = market.Where(d => LunarCalendar.IsBigMarket(LunarCalendar.Date(d))).ToArray();
            CollectionAssert.AreEqual(new[] { 14, 28 }, big, "phien lon gan ram va mung 1");
            Assert.AreEqual("Trăng tròn", LunarCalendar.MoonPhaseName(LunarCalendar.MoonAge(15)));
            Assert.AreEqual("Trăng non", LunarCalendar.MoonPhaseName(LunarCalendar.MoonAge(1, 0)));
        }

        // ------------------------------------------------------------ con nuoc
        [Test]
        public void ConNuoc_RongQuanhMung1VaRam_KemQuanhMung8()
        {
            Assert.AreEqual(TideClock.SpringAmp, TideClock.Amplitude(TideClock.AbsHours(1, 0)), .01f);
            Assert.AreEqual(TideClock.SpringAmp, TideClock.Amplitude(TideClock.AbsHours(15, 0)), .01f);
            Assert.AreEqual(TideClock.NeapAmp, TideClock.Amplitude(TideClock.AbsHours(8, 0)), .01f);
            TideClock.DayRange(1, out float lo1, out float hi1);
            TideClock.DayRange(8, out float lo8, out float hi8);
            Assert.Greater(hi1 - lo1, 2 * (hi8 - lo8), "nuoc rong dao dong manh hon nuoc kem");
            Assert.LessOrEqual(hi1, TideClock.MaxLevel + 1e-4f);
        }

        [Test]
        public void ConNuoc_GioNuocLonLuiKhoang50PhutMoiNgay()
        {
            double h1 = TideClock.NextHigh(TideClock.AbsHours(1, 0));
            double h2 = TideClock.NextHigh(TideClock.AbsHours(2, 0));
            Assert.AreEqual(6.0, h1, 1e-6);
            Assert.AreEqual(50.4, (h2 - 24 - h1) * 60, 1, "lui ~50 phut");
            Assert.AreEqual(TideClock.SpringAmp, TideClock.Offset(h1), .01f, "dinh nuoc lon");
            Assert.IsTrue(TideClock.Rising(h1 - 1), "truoc nuoc lon la nuoc dang len");
            Assert.IsFalse(TideClock.Rising(h1 + 1));
            Assert.AreEqual(h1 + TideClock.Period / 2, TideClock.NextLow(h1), 1e-6);
        }

        [Test]
        public void ConNuoc_LoiBienNong_KhongDiDuocNuocSau()
        {
            var d = new IslandData(424242, 300);
            d.Generate();
            // tim o bien nong (sau 0.2 luc muc nuoc trung binh)
            int found = -1;
            for (int k = 0; k < d.N * d.N && found < 0; k++)
                if (d.Ocean[k] != 0 && d.Pier[k] == 0 && d.HT[k] < IslandData.SEA - .15f && d.HT[k] > IslandData.SEA - .25f) found = k;
            Assert.GreaterOrEqual(found, 0, "co o bien nong");
            float x = found % d.N, z = found / d.N, h = d.HeightAt(x, z);
            d.WaterLevel = IslandData.SEA;
            Assert.IsTrue(d.Walkable(x, z, h), "loi duoc khi nuoc trung binh");
            d.WaterLevel = IslandData.SEA + TideClock.SpringAmp;
            Assert.IsFalse(d.Walkable(x, z, h), "nuoc rong len thi qua sau");
            d.WaterLevel = IslandData.SEA - TideClock.SpringAmp;
            Assert.LessOrEqual(d.WaterDepth(x, z), 0f, "nuoc ròng kiet thi lo ra");

            // dai bai trieu: so o bien lo ra khi nuoc rong kiet
            int flats = 0;
            for (int k = 0; k < d.N * d.N; k++)
                if (d.Ocean[k] != 0 && d.HT[k] > IslandData.SEA - TideClock.SpringAmp + .06f && d.HT[k] < IslandData.SEA - .02f) flats++;
            Debug.Log($"[SereneIsle] bai trieu (dao 300x300): {flats} o lo ra khi nuoc rong kiet");
            Assert.Greater(flats, 50);

            // moi loai dat (tru dat vuon quanh leu) deu co cho cuoc duoc (tren muc nuoc rong)
            var count = new int[5];
            for (int j = 0; j < d.N; j++)
                for (int i = 0; i < d.N; i++)
                {
                    int k = d.Idx(i, j);
                    if (d.Solid[k] != 0 || d.Fresh[k] != 0 || d.HT[k] < TideClock.MaxLevel + .04f) continue;
                    count[(int)Soils.Classify(d, i, j, null)]++;
                }
            Debug.Log($"[SereneIsle] o cuoc duoc theo loai dat: cat pha {count[0]}, phu sa {count[1]}, dat do {count[2]}, dat lo {count[3]}, vuon {count[4]}");
            for (int t = 0; t < 4; t++) Assert.Greater(count[t], 100, Soils.Name((SoilType)t) + " qua it");
        }

        // ------------------------------------------------------------ giong cay
        static List<CropDefinition> Crops() => CropCatalogData.CreateAll();

        [Test]
        public void GiongCay_MoiMuaCoItNhat8Giong_HatGiongCoTrongItem()
        {
            var crops = Crops();
            Assert.AreEqual(crops.Count, crops.Select(c => c.id).Distinct().Count(), "id trung");
            foreach (Season s in Enum.GetValues(typeof(Season)))
            {
                int n = crops.Count(c => c.InSeason(s) && !c.needsPaddy && c.kind != CropKind.Tree);
                Assert.GreaterOrEqual(n, 8, $"mua {s} it giong");
            }
            var items = ItemCatalogData.CreateAll().ToDictionary(i => i.id);
            foreach (var c in crops)
            {
                Assert.IsTrue(items.ContainsKey(c.produce), "thieu nong san " + c.id);
                Assert.IsTrue(items.ContainsKey(c.SeedId), "thieu hat " + c.id);
                Assert.AreEqual(c.id, items[c.SeedId].seedOf);
                Assert.AreEqual(ItemCategory.Seed, items[c.SeedId].category);
                if (!c.perennial) Assert.LessOrEqual(c.growDays, LunarCalendar.DaysPerSeason, c.id + " lon khong kip trong mua");
            }
            Assert.IsTrue(crops.Any(c => c.id == "toi" && c.InSeason(Season.Dong)), "toi dao dac san mua Dong");
            Assert.IsTrue(crops.Any(c => c.id == "melon" && c.InSeason(Season.Ha) && c.InSeason(Season.Dong)), "dua hau Tet");
        }

        // ------------------------------------------------------------ luat trong trot
        static CropDefinition Get(string id) => Crops().First(c => c.id == id);

        [Test]
        public void TrongTrot_TuoiThiLon_QuenTuoi2NgayThiChet()
        {
            var c = Get("cai_be"); // 5 ngay, hop vuon / phu sa
            var p = new FarmPlot { soil = SoilType.Vuon };
            Assert.IsNull(FarmRules.CanPlant(p, c, Season.Xuan));
            Assert.IsNotNull(FarmRules.CanPlant(p, c, Season.Ha), "trai mua khong gieo duoc");
            FarmRules.Plant(p, c);
            for (int i = 0; i < 4; i++) { p.watered = true; FarmRules.NewDay(p, c, Season.Xuan); }
            Assert.AreEqual(4, p.growth);
            Assert.IsFalse(FarmRules.Harvestable(p, c));
            FarmRules.NewDay(p, c, Season.Xuan);
            Assert.IsTrue(p.Wilting, "quen tuoi 1 ngay: heo");
            Assert.IsFalse(p.dead);
            FarmRules.NewDay(p, c, Season.Xuan);
            Assert.IsTrue(p.dead, "quen tuoi 2 ngay: chet");
            Assert.AreEqual("khô héo", p.deadReason);
        }

        [Test]
        public void TrongTrot_ThuHoach_HangVaThuLai()
        {
            var rng = new System.Random(1);
            var c = Get("dua_leo"); // 10 ngay, thu lai moi 3 ngay
            var p = new FarmPlot { soil = SoilType.PhuSa };
            FarmRules.Plant(p, c);
            for (int i = 0; i < 10; i++) { p.watered = true; FarmRules.NewDay(p, c, Season.Xuan); }
            Assert.IsTrue(FarmRules.Harvestable(p, c));
            Assert.AreEqual(3, FarmRules.Stage(p, c));
            int n = FarmRules.Harvest(p, c, rng, out int q);
            Assert.AreEqual(2, q, "dung dat + tuoi deu = dac san");
            Assert.That(n, Is.InRange(c.yieldMin + 1, c.yieldMax + 1));
            Assert.AreEqual(c.id, p.crop, "thu lai: cay con");
            for (int i = 0; i < 3; i++) { p.watered = true; FarmRules.NewDay(p, c, Season.Xuan); }
            Assert.IsTrue(FarmRules.Harvestable(p, c), "3 ngay sau thu tiep");

            var once = Get("cu_cai");
            var p2 = new FarmPlot { soil = SoilType.DatDo }; // sai dat: lon nua toc do
            FarmRules.Plant(p2, once);
            for (int i = 0; i < 8; i++) { p2.watered = true; FarmRules.NewDay(p2, once, Season.Xuan); }
            Assert.AreEqual(4, p2.growth, .001f);
            for (int i = 0; i < 8; i++) { p2.watered = true; FarmRules.NewDay(p2, once, Season.Xuan); }
            FarmRules.Harvest(p2, once, rng, out int q2);
            Assert.AreEqual(1, q2, "sai dat = ngon");
            Assert.IsTrue(p2.Empty, "thu 1 lan thi het cay");
        }

        [Test]
        public void TrongTrot_SangMuaKhac_CayTraiMuaTan_CayAnTraiSongQua()
        {
            var c = Get("xa_lach");
            var p = new FarmPlot { soil = SoilType.Vuon };
            FarmRules.Plant(p, c);
            p.watered = true;
            Assert.IsTrue(FarmRules.NewDay(p, c, Season.Ha));
            Assert.AreEqual("trái mùa", p.deadReason);

            var t = Get("xoai"); // ra trai mua Ha, 28 ngay lon, 7 ngay mot dot trai
            var tp = new FarmPlot { soil = SoilType.DatDo };
            Assert.IsNull(FarmRules.CanPlant(tp, t, Season.Xuan), "cay giong trong moi mua");
            FarmRules.Plant(tp, t);
            for (int i = 0; i < 28; i++) { tp.watered = true; FarmRules.NewDay(tp, t, Season.Xuan); }
            Assert.IsTrue(FarmRules.Mature(tp, t));
            Assert.IsFalse(FarmRules.Harvestable(tp, t), "chua vao mua trai");
            for (int i = 0; i < 10; i++) FarmRules.NewDay(tp, t, Season.Xuan);
            Assert.IsFalse(tp.dead, "cay lon khong can tuoi");
            for (int i = 0; i < 7; i++) FarmRules.NewDay(tp, t, Season.Ha);
            Assert.IsTrue(FarmRules.Harvestable(tp, t), "mua Ha ra trai");
            FarmRules.Harvest(tp, t, new System.Random(2), out _);
            Assert.IsFalse(tp.hasFruit);
            Assert.AreEqual("xoai", tp.crop, "cay van con");
        }

        [Test]
        public void ConNuoc_BiKet_Mat30PhanTramTien_HongThucAnVaHat()
        {
            var db = ItemDatabase.CreateInMemory();
            var inv = new Inventory(db);
            inv.Give("bread", 2); inv.Give("seed_cai_be", 5); inv.Give("wood", 3);
            var msgs = TideRules.Rescue(inv, db, 100, out int left);
            Assert.AreEqual(70, left);
            Assert.AreEqual(0, inv.Count("bread"));
            Assert.AreEqual(0, inv.Count("seed_cai_be"));
            Assert.AreEqual(3, inv.Count("wood"), "go khong hong");
            Assert.IsTrue(msgs.Any(m => m.Contains("30 xu")));
        }
    }
}
