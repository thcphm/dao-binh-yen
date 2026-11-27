// Vuon ruong nang cao trong game (Docs muc 4.4 - 4.6, 4.8): sau benh + bat sau + phun toi ot, xen dau, chua giong lay hat,
// nha luoi (gieo trai mua, che bao), ghep canh, cong + ruong tom + lua chiu man, cay dau dong. Chup anh de duyet.
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace SereneIsle.Tests
{
    public class FarmCarePlayTests
    {
        [TearDown]
        public void TearDown()
        {
            foreach (var go in Object.FindObjectsByType<GameObject>())
                if (go && go.transform.parent == null) Object.Destroy(go);
        }

        static IEnumerator Until(System.Func<bool> ok, float secs, string what)
        {
            float t = Time.realtimeSinceStartup;
            while (!ok())
            {
                if (Time.realtimeSinceStartup - t > secs) Assert.Fail("qua lau: " + what);
                yield return null;
            }
        }

        static IEnumerator WaitAct(GameSession s) { yield return null; while (s.Body && s.Body.Acting) yield return null; }

        /// <summary>Dung o phia nam o k, mat huong +z (TargetTile = k).</summary>
        static void Face(GameSession s, IslandData d, int k)
        {
            float x = k % d.N, z = k / d.N - 1;
            s.Player.PlaceAt(new Vector3(x, d.GroundHeight(x, z), z), 0);
        }

        static void PassDay(GameSession s, FarmSystem farm)
        {
            s.Clock.Day++;
            typeof(FarmSystem).GetMethod("OnDayStarted", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(farm, new object[] { s.Clock.Day });
        }

        static FarmPlot Plot(FarmSystem farm, int k, string crop, float grow = 1)
        {
            var c = CropDatabase.Instance.Get(crop);
            var p = new FarmPlot { k = k, soil = farm.SoilAt(k) };
            FarmRules.Plant(p, c);
            p.growth = c.growDays * grow;
            p.hasFruit = c.kind == CropKind.Tree && grow >= 1;
            p.watered = true;
            return p;
        }

        [UnityTest, Timeout(600000)]
        public IEnumerator VuonRuongNangCao()
        {
            var boot = new GameObject("Bootstrap");
            boot.SetActive(false);
            var bs = boot.AddComponent<GameBootstrap>(); bs.seed = 424242; bs.saveSlot = -1;
            boot.SetActive(true);
            yield return Until(() => IslandTerrain.Instance && IslandTerrain.Instance.IsReady, 180, "sinh dao");
            for (int i = 0; i < 10; i++) yield return null;
            var s = GameSession.Instance;
            var d = IslandTerrain.Instance.Data;
            var farm = s.Farm;
            var rig = Camera.main.GetComponent<OrbitCamera>();
            string dir = Phase3Tests.Dir();
            if (WeatherSystem.Instance) { WeatherSystem.Instance.Override = WeatherKind.Sun; WeatherSystem.Instance.SnapVisuals(); }
            s.Inventory.BagLevel = 2;
            s.Clock.Day = 57;                       // mua Ha (thang 3): tha tom duoc
            s.Clock.Minutes = 9 * 60;
            Assert.AreEqual(Season.Ha, s.Date.Season);

            // ---- khu 9 x 5 o cuoc duoc gan leu ----
            var home = s.HasHome ? s.HomePosition : s.Player.transform.position;
            int bx = -1, bz = -1; float bd = 1e9f;
            for (int z0 = 1; z0 < d.N - 6; z0++)
                for (int x0 = 1; x0 < d.N - 10; x0++)
                {
                    bool ok = true;
                    for (int q = 0; q < 45 && ok; q++)
                    {
                        int i = x0 + q % 9, j = z0 + q / 9;
                        ok = d.InBounds(i, j) && farm.CanTill(d.Idx(i, j), out _) && farm.PlotAt(d.Idx(i, j)) == null
                             && Mathf.Abs(d.HT[d.Idx(i, j)] - d.HT[d.Idx(x0, z0)]) < .6f && d.HT[d.Idx(i, j)] > TideClock.MaxLevel + .1f
                             && d.Bio[d.Idx(i, j)] != Biome.Town && d.Solid[d.Idx(i, j)] == 0;
                    }
                    for (int i = x0; i <= x0 + 3 && ok; i++) ok = d.Walkable(i, z0 - 1, d.HT[d.Idx(i, z0)]);
                    float dist = Vector2.Distance(new Vector2(x0 + 4, z0 + 2), new Vector2(home.x, home.z));
                    float town = Vector2.Distance(new Vector2(x0 + 4, z0 + 2), new Vector2(d.CX, d.CZ));
                    if (ok && dist > 4 && town > 34 && dist < bd) { bd = dist; bx = x0; bz = z0; }
                }
            Assert.GreaterOrEqual(bx, 0, "khong co khu dat 9 x 5");
            int K(int di, int dj) => d.Idx(bx + di, bz + dj);

            // ---- trai: sau benh, xen dau, chua giong (cot 0..3) ----
            var plots = new List<FarmPlot>();
            for (int i = 0; i < 4; i++) { var p = Plot(farm, K(i, 0), "ot", .5f); if (i < 2) p.pest = Pest.Ray; plots.Add(p); }          // ot bi ray
            for (int i = 0; i < 4; i++) { var p = Plot(farm, K(i, 1), "rau_muong", .6f); if (i == 1 || i == 2) p.pest = Pest.SauXanh; plots.Add(p); }
            plots.Add(Plot(farm, K(0, 2), "dau_phong", .8f));
            plots.Add(Plot(farm, K(1, 2), "ca_tim", .7f));
            plots.Add(Plot(farm, K(2, 2), "khoai_lang", 1));
            var saved = Plot(farm, K(3, 2), "bap", 1); saved.saved = true; saved.seedAge = 1; plots.Add(saved);
            for (int i = 0; i < 4; i++) plots.Add(Plot(farm, K(i, 3), "kho_qua", .9f));
            // ---- phai: nha luoi 5 x 5 (cot 4..8), cay mua Xuan / Dong trong mua Ha ----
            string[] off = { "cai_be", "xa_lach", "cai_thao", "tomato", "ca_rot", "su_hao", "cu_cai", "dau_que" };
            int ci = 6, cj = 2, n = 0;
            for (int dj = -2; dj <= 2; dj++)
                for (int di = -2; di <= 2; di++)
                {
                    if (di == 0 && dj == 0) continue;
                    plots.Add(Plot(farm, K(ci + di, cj + dj), off[n++ % off.Length], .55f + (n % 4) * .15f));
                }
            farm.Restore(plots);
            Assert.IsTrue(Placeables.Instance.CanPlace(K(ci, cj), "nha_luoi", out string why), why);
            Assert.IsNotNull(Placeables.Instance.Add("nha_luoi", K(ci, cj)));
            Assert.IsTrue(Placeables.Instance.Sheltered(K(ci + 2, cj - 2)));
            Assert.IsFalse(Placeables.Instance.Sheltered(K(3, 0)));
            PassDay(s, farm); s.Clock.Day--;         // tinh xen dau, (sau benh moi ngau nhien)
            foreach (var p in farm.Plots.Values) p.watered = true;
            Assert.IsTrue(farm.PlotAt(K(1, 2)).nearLegume, "ca tim ke dau phong");
            Assert.IsFalse(farm.PlotAt(K(0, 2)).dead, "cay trai mua trong nha luoi khong chet");
            Assert.IsFalse(farm.PlotAt(K(ci + 1, cj)).dead);

            float cx = bx + 4f, cz = bz - 2.2f;
            s.Player.PlaceAt(new Vector3(cx, d.GroundHeight(cx, cz), cz), 0);
            rig.yaw = Mathf.PI; rig.pitch = .7f; rig.distance = 10f;
            for (int i = 0; i < 20; i++) yield return null;
            yield return Phase3Tests.Shot(Path.Combine(dir, "p6h_1_vuon_nang_cao.png"));

            // can canh: sau + coc chua giong
            float px = bx + 1.5f, pz = bz - 1.3f;
            s.Player.PlaceAt(new Vector3(px, d.GroundHeight(px, pz), pz), 0);
            rig.yaw = Mathf.PI + .3f; rig.pitch = .55f; rig.distance = 4.6f;
            for (int i = 0; i < 10; i++) yield return null;
            yield return Phase3Tests.Shot(Path.Combine(dir, "p6h_2_sau_va_giong.png"));

            // ---- bat sau bang tay (E), phun nuoc toi ot ----
            Face(s, d, K(1, 1)); yield return null;
            var act = farm.NearbyAction(out string label);
            Assert.IsNotNull(act); StringAssert.Contains("Bắt sâu xanh", label);
            act(); yield return WaitAct(s);
            Assert.AreEqual(Pest.None, farm.PlotAt(K(1, 1)).pest);
            Assert.AreEqual(Pest.SauXanh, farm.PlotAt(K(2, 1)).pest, "o khac van con sau");
            Face(s, d, K(0, 0)); yield return null;
            Assert.IsNull(farm.NearbyAction(out _), "ray khong bat tay duoc");
            s.Give("nuoc_toi_ot", 1, true);
            Assert.IsTrue(farm.TryUse(s.Items.Get("nuoc_toi_ot"))); yield return WaitAct(s);
            Assert.AreEqual(Pest.None, farm.PlotAt(K(0, 0)).pest);
            Assert.AreEqual(Pest.None, farm.PlotAt(K(1, 0)).pest, "phun 9 o");
            Assert.AreEqual(0, s.Inventory.Count("nuoc_toi_ot"));

            // ---- chua giong: khoai lang chin -> E -> de gia 3 ngay -> thu hat ----
            Face(s, d, K(2, 2)); yield return null;
            act = farm.NearbyAction(out label);
            StringAssert.Contains("Chừa", label);
            act();
            Assert.IsTrue(farm.PlotAt(K(2, 2)).saved);
            for (int i = 0; i < FarmCare.SeedAgeDays; i++) { foreach (var p in farm.Plots.Values) p.watered = true; PassDay(s, farm); }
            Assert.IsTrue(FarmCare.SeedReady(farm.PlotAt(K(2, 2))));
            Assert.IsTrue(farm.TryUse(s.Items.Get("hoe"))); yield return WaitAct(s);
            Assert.Greater(s.Inventory.Count("giong_khoai_lang"), 1, "thu hat giu giong");
            Assert.AreEqual(1, farm.StrainOf("khoai_lang").gen);
            Assert.IsTrue(farm.PlotAt(K(2, 2)).Empty);
            Assert.IsTrue(farm.TryUse(s.Items.Get("giong_khoai_lang"))); yield return WaitAct(s);
            Assert.Greater(farm.PlotAt(K(2, 2)).strainQ, 0, "cay con mang hang cay me");

            // ---- ghep canh: trong xoai trong nha luoi roi ghep canh dau dong ----
            var x = farm.PlotAt(K(ci, cj + 1));
            FarmRules.Clear(x); FarmRules.Plant(x, CropDatabase.Instance.Get("xoai")); x.growth = 10;
            s.Give("canh_quyt", 1, true);
            // phia nam o (ci, cj+1) la cot nha luoi (o vat can): dung phia bac, quay mat ve -z
            float fx = bx + ci, fz = bz + cj + 2;
            s.Player.PlaceAt(new Vector3(fx, d.GroundHeight(fx, fz), fz), Mathf.PI);
            yield return null;
            Assert.AreEqual(K(ci, cj + 1), farm.TargetTile());
            Assert.IsTrue(farm.TryUse(s.Items.Get("canh_quyt"))); yield return WaitAct(s);
            Assert.AreEqual("quyt", x.graft);
            Assert.AreNotEqual(GraftTrait.None, x.trait, "xoai lai quyt");

            // ---- nha luoi che bao ----
            farm.ApplyStorm(new System.Random(1));
            foreach (var p in farm.Plots.Values)
                if (Placeables.Instance.Sheltered(p.k)) Assert.IsFalse(p.dead && p.deadReason == "bão", "nha luoi che bao");

            // ---- cong + ruong tom (dat trung ven bien) ----
            int la = -1, lb = -1; float best = 1e9f;
            for (int k = 0; k < d.N * d.N; k++)
            {
                int i = k % d.N, j = k / d.N;
                if (i + 1 >= d.N || j < 2) continue;
                int k2 = k + 1;
                bool Low(int q) => farm.CanTill(q, out _) && farm.PlotAt(q) == null && d.HT[q] > IslandData.SEA + .08f && d.HT[q] < TideClock.MaxLevel - .05f && d.Solid[q] == 0;
                if (!Low(k) || !Low(k2) || !d.Walkable(i, j - 1, d.HT[k]) || !d.Walkable(i + 1, j - 1, d.HT[k2])) continue;
                float dist = Vector2.Distance(new Vector2(i, j), new Vector2(d.CX, d.CZ));
                if (dist < 34) continue;   // xa xom (NPC dung che)
                if (dist < best) { best = dist; la = k; lb = k2; }
            }
            Assert.GreaterOrEqual(la, 0, "khong co dat trung");
            var low = new List<FarmPlot>(farm.Snapshot());
            foreach (int k in new[] { la, lb }) low.Add(new FarmPlot { k = k, soil = farm.SoilAt(k), lowland = true, dike = true });
            farm.Restore(low);
            s.Give("cong", 2, true);
            foreach (int k in new[] { la, lb })
            {
                Face(s, d, k); yield return null;
                Assert.IsTrue(farm.TryUse(s.Items.Get("cong"))); yield return WaitAct(s);
                Assert.IsTrue(farm.PlotAt(k).sluice, "dat cong");
            }
            Face(s, d, la); yield return null;
            act = farm.NearbyAction(out label);
            StringAssert.Contains("Mở cống", label);
            act(); yield return WaitAct(s);
            Assert.IsTrue(farm.PlotAt(la).sluiceOpen);
            s.Give("tom_giong", 1, true);
            Assert.IsTrue(farm.TryUse(s.Items.Get("tom_giong"))); yield return WaitAct(s);
            Assert.AreEqual(1, farm.PlotAt(la).shrimpAge);
            // o ben: lua chiu man dang lon
            var lp = farm.PlotAt(lb);
            lp.saltDays = 2;
            Assert.IsNull(FarmRules.CanPlant(lp, CropDatabase.Instance.Get("lua_man"), Season.Thu, true));
            FarmRules.Plant(lp, CropDatabase.Instance.Get("lua_man")); lp.growth = 14;
            farm.Restore(farm.Snapshot());
            float sx = (la % d.N) + .5f, sz = la / d.N - 1.4f;
            s.Player.PlaceAt(new Vector3(sx, d.GroundHeight(sx, sz), sz), 0);
            rig.yaw = Mathf.PI + .3f; rig.pitch = .75f; rig.distance = 5f;
            for (int i = 0; i < 15; i++) yield return null;
            yield return Phase3Tests.Shot(Path.Combine(dir, "p6h_3_ruong_tom.png"));
            for (int i = 0; i < FarmCare.ShrimpDays; i++) PassDay(s, farm);
            Face(s, d, la); yield return null;
            act = farm.NearbyAction(out label);
            StringAssert.Contains("Thu tôm", label);
            act();
            Assert.That(s.Inventory.Count("tom_su"), Is.InRange(3, 5));
            Assert.IsTrue(farm.PlotAt(la).shrimpBonus);

            // ---- cay dau dong: vung du suc song 75 ----
            var pos = new Vector3(bx + 4, 0, bz - 3);
            var reg = s.Restoration.Model.RegionAt(pos.x, pos.z);
            Assert.IsNotNull(reg);
            reg.vitality = 80;
            MotherTrees.Instance.Refresh();
            MotherTrees.Tree tree = null;
            foreach (var t in MotherTrees.Instance.Trees) if (t.region == reg.id) tree = t;
            Assert.IsNotNull(tree); Assert.GreaterOrEqual(tree.k, 0, "co cay dau dong");
            float tx = tree.k % d.N, tz = tree.k / d.N - 1.3f;
            s.Player.PlaceAt(new Vector3(tx, d.GroundHeight(tx, tz), tz), 0);
            rig.yaw = Mathf.PI + .4f; rig.pitch = .4f; rig.distance = 5.5f;
            for (int i = 0; i < 10; i++) yield return null;
            yield return Phase3Tests.Shot(Path.Combine(dir, "p6h_4_cay_dau_dong.png"));
            var cut = MotherTrees.Instance.NearbyAction(s.Player.transform.position, out label);
            StringAssert.Contains("Cắt cành", label);
            cut();
            Assert.AreEqual(2, s.Inventory.Count("canh_" + tree.crop));
            MotherTrees.Instance.NearbyAction(s.Player.transform.position, out label);
            StringAssert.Contains("ngày nữa", label);

            // ---- luu / tai ----
            var back = JsonUtility.FromJson<SaveData>(JsonUtility.ToJson(SaveSystem.Capture(s)));
            farm.RestoreStrains(null);
            MotherTrees.Instance.Restore(null);
            SaveSystem.ApplyState(back, s);
            Assert.AreEqual(1, farm.StrainOf("khoai_lang").gen);
            Assert.Greater(MotherTrees.Instance.DaysLeft(tree.region, s.Clock.Day), 0);
            Assert.AreEqual("quyt", farm.PlotAt(K(ci, cj + 1)).graft);
            Assert.IsNull(farm.PlotAt(K(0, 0)).graft, "chuoi rong doc lai thanh null");
            Assert.IsTrue(farm.PlotAt(lb).sluice);
        }
    }
}
