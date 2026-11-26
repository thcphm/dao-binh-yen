// Smoke test lich am + con nuoc + vuon ruong trong game that (seed 424242). Chup anh vao SERENE_SHOT_DIR.
using System.Collections;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace SereneIsle.Tests
{
    public class Phase3GameplaySmokeTests
    {
        [TearDown]
        public void TearDown()
        {
            foreach (var go in Object.FindObjectsByType<GameObject>())
                if (go && go.transform.parent == null) Object.Destroy(go);
        }

        static string ShotDir()
        {
            string dir = System.Environment.GetEnvironmentVariable("SERENE_SHOT_DIR");
            if (string.IsNullOrEmpty(dir)) dir = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Logs", "shots"));
            Directory.CreateDirectory(dir);
            return dir;
        }

        static IEnumerator Boot()
        {
            var boot = new GameObject("Bootstrap");
            boot.SetActive(false);
            var b = boot.AddComponent<GameBootstrap>();
            b.seed = 424242; b.saveSlot = -1;
            boot.SetActive(true);
            float t0 = Time.realtimeSinceStartup;
            while (!(IslandTerrain.Instance && IslandTerrain.Instance.IsReady))
            {
                if (Time.realtimeSinceStartup - t0 > 180) Assert.Fail("sinh dao qua 180s");
                yield return null;
            }
            for (int i = 0; i < 10; i++) yield return null;
        }

        static IEnumerator WaitAct(GameSession s)
        {
            float t = Time.realtimeSinceStartup;
            yield return null;
            while (s.Body && s.Body.Acting && Time.realtimeSinceStartup - t < 3) yield return null;
        }

        static IEnumerator Sleep(GameSession s)
        {
            s.StartCoroutine(s.NewDay(NewDayReason.Sleep));
            yield return null;
            float t = Time.realtimeSinceStartup;
            while (s.Transitioning && Time.realtimeSinceStartup - t < 10) yield return null;
        }

        static void Face(GameSession s, Vector3 from, int k)
        {
            var d = IslandTerrain.Instance.Data;
            float tx = k % d.N, tz = k / d.N;
            s.Player.PlaceAt(from, Mathf.Atan2(tx - from.x, tz - from.z));
        }

        [UnityTest, Timeout(600000)]
        public IEnumerator VuonRuong_CuocTuoiGieo_QuaNgay_ThuHoach()
        {
            yield return Boot();
            var s = GameSession.Instance;
            var d = IslandTerrain.Instance.Data;
            var farm = s.Farm;
            Assert.IsNotNull(farm);
            Assert.IsTrue(s.OwnedTools.Contains("hoe") && s.OwnedTools.Contains("can") && s.OwnedTools.Contains("seed"));
            Assert.AreEqual("Mùng 1 tháng 1", s.Date.ShortLabel);

            // o cuoc duoc gan leu, dung cach 1 o phia nam
            int target = -1; Vector3 stand = default;
            Vector3 home = s.HomePosition;
            for (int r = 3; r < 12 && target < 0; r++)
                for (int a = 0; a < 16 && target < 0; a++)
                {
                    int i = Mathf.RoundToInt(home.x + Mathf.Cos(a * .3927f) * r), j = Mathf.RoundToInt(home.z + Mathf.Sin(a * .3927f) * r);
                    if (!d.InBounds(i, j) || !d.InBounds(i, j - 1)) continue;
                    int k = d.Idx(i, j), ks = d.Idx(i, j - 1);
                    if (!farm.CanTill(k, out _) || d.Solid[ks] != 0 || !d.Walkable(i, j - 1, d.HT[ks])) continue;
                    if (Mathf.Abs(d.HT[k] - d.HT[ks]) > .3f) continue;
                    target = k; stand = new Vector3(i, d.HT[ks], j - 1);
                }
            Assert.GreaterOrEqual(target, 0, "khong tim duoc o dat cuoc gan leu");
            Face(s, stand, target);
            yield return null;
            Assert.AreEqual(target, farm.TargetTile());

            s.SelectSlot(s.Hotbar.IndexOf("hoe")); s.UseSelected(); yield return WaitAct(s);
            var p = farm.PlotAt(target);
            Assert.IsNotNull(p, "cuoc xong phai co o dat");
            Assert.AreEqual(SoilType.Vuon, p.soil, "gan leu la dat vuon");

            s.SelectSlot(s.Hotbar.IndexOf("can")); s.UseSelected(); yield return WaitAct(s);
            Assert.IsTrue(p.watered);
            Assert.AreEqual(FarmSystem.CanCapacity - 1, farm.CanWater);

            s.SelectSlot(s.Hotbar.IndexOf("seed")); s.UseSelected(); yield return WaitAct(s);
            Assert.AreEqual("cai_be", p.crop, "tui hat gieo loai dung mua dau tien");
            Assert.AreEqual(5, s.Inventory.Count("seed_cai_be"));

            // 5 ngay tuoi deu -> chin
            for (int day = 0; day < 5; day++)
            {
                p.watered = true;
                yield return Sleep(s);
            }
            Assert.AreEqual(6, s.Clock.Day);
            var c = CropDatabase.Instance.Get("cai_be");
            Assert.IsTrue(FarmRules.Harvestable(p, c), $"sau 5 ngay phai chin (growth {p.growth})");
            Face(s, stand, target);
            yield return null;
            s.Inventory.Take("stone", s.Inventory.Count("stone")); // tui khoi dau day 8 o: chua 1 o cho rau
            s.UseSelected(); yield return WaitAct(s);
            Assert.Greater(s.Inventory.Count("cai_be"), 0, "thu hoach vao tui");
            Assert.IsTrue(p.Empty);

            // vuon mau nhieu loai cay (dung cho anh duyet)
            // tim 1 khu 4x4 o cuoc duoc gan leu nhat
            var demo = new List<FarmPlot>();
            string[] ids = { "cai_be", "xa_lach", "dua_leo", "dau_que", "pumpkin", "cu_cai", "hanh_la", "melon", "tomato", "ot", "bap", "toi", "van_tho", "khoai_lang", "buoi", "xoai" };
            int bx = -1, bz = -1; float bd = 1e9f;
            for (int z0 = (int)home.z - 20; z0 <= home.z + 20; z0++)
                for (int x0 = (int)home.x - 20; x0 <= home.x + 20; x0++)
                {
                    bool ok = true;
                    for (int q = 0; q < 16 && ok; q++)
                    {
                        int i = x0 + q % 4, j = z0 + q / 4;
                        ok = d.InBounds(i, j) && farm.CanTill(d.Idx(i, j), out _) && d.Idx(i, j) != target && Mathf.Abs(d.HT[d.Idx(i, j)] - d.HT[d.Idx(x0, z0)]) < .5f;
                    }
                    float dist = Vector2.Distance(new Vector2(x0 + 1.5f, z0 + 1.5f), new Vector2(home.x, home.z));
                    if (ok && dist > 3 && dist < bd) { bd = dist; bx = x0; bz = z0; }
                }
            Assert.GreaterOrEqual(bx, 0, "khong co khu dat 4x4 gan leu");
            for (int q = 0; q < 16; q++)
            {
                int k = d.Idx(bx + q % 4, bz + q / 4);
                var cc = CropDatabase.Instance.Get(ids[q]);
                var fp = new FarmPlot { k = k, soil = farm.SoilAt(k), crop = cc.id, growth = cc.growDays, hasFruit = true, watered = q % 3 == 0 };
                if (q % 5 == 4) fp.growth = cc.growDays * .5f;
                demo.Add(fp);
            }
            farm.Restore(demo);
            var rig = Camera.main.GetComponent<OrbitCamera>();
            float cx = bx + 1.5f, cz = bz - 1.2f;
            s.Player.PlaceAt(new Vector3(cx, d.GroundHeight(cx, cz), cz), 0);
            rig.yaw = Mathf.PI; rig.pitch = .62f; rig.distance = 7.5f;
            s.Clock.Minutes = 9 * 60;
            yield return new WaitForSecondsRealtime(1.5f); // cho man chuyen ngay tan
            for (int i = 0; i < 20; i++) yield return null;
            yield return GameplayShots.Shot(Path.Combine(ShotDir(), "p3_1_vuon.png"));
        }

        [UnityTest, Timeout(600000)]
        public IEnumerator ConNuoc_BaiTrieu_NuocLenBiKet()
        {
            yield return Boot();
            var s = GameSession.Instance;
            var d = IslandTerrain.Instance.Data;

            // ngay 1 (nuoc rong): nuoc lon 6:00, can luc 12:13
            s.Clock.Minutes = 12 * 60 + 13;
            yield return null; yield return null;
            Assert.AreEqual(IslandData.SEA - TideClock.SpringAmp, d.WaterLevel, .02f, "nuoc ròng kiet");
            Assert.AreEqual(d.WaterLevel, IslandTerrain.Instance.WaterPlane.position.y, .001f, "mat nuoc di theo con nuoc");

            // o bai trieu gan thi tran nhat
            int flat = -1; float best = 1e9f;
            for (int k = 0; k < d.N * d.N; k++)
            {
                if (d.Ocean[k] == 0 || d.Pier[k] != 0) continue;
                float h = d.HT[k];
                if (h < IslandData.SEA - .3f || h > IslandData.SEA - .18f) continue;
                float dist = Vector2.Distance(new Vector2(k % d.N, k / d.N), new Vector2(d.CX, d.CZ));
                if (dist < best) { best = dist; flat = k; }
            }
            Assert.GreaterOrEqual(flat, 0, "khong co bai trieu");
            var at = new Vector3(flat % d.N, 0, flat / d.N);
            at.y = d.GroundHeight(at.x, at.z);
            Assert.IsTrue(d.Walkable(at.x, at.z, at.y), "nuoc ròng: di bo tren bai duoc");
            s.Player.PlaceAt(at, 0);

            // rai do bai trieu quanh nguoi choi (lan sau khi nuoc rong lai)
            int before = ItemPile.All.Count;
            s.Clock.Minutes = 9 * 60; yield return null;
            s.Clock.Minutes = 12 * 60 + 13; yield return null; yield return null;
            Assert.Greater(ItemPile.All.Count, before, "nuoc ròng thi co do bai trieu");

            var rig = Camera.main.GetComponent<OrbitCamera>();
            rig.pitch = .5f; rig.distance = 12f;
            for (int i = 0; i < 20; i++) yield return null;
            yield return GameplayShots.Shot(Path.Combine(ShotDir(), "p3_2_nuoc_rong.png"));

            // nuoc lon (18:25): bai ngap, nguoi choi bi ket -> Bac Chin don, mat 30% tien, hong do an
            s.Coins = 100;
            int bread = s.Inventory.Count("bread");
            Assert.Greater(bread + s.Inventory.Count("berry"), 0);
            s.Clock.Minutes = 18 * 60 + 25;
            yield return null; yield return null;
            Assert.AreEqual(IslandData.SEA + TideClock.SpringAmp, d.WaterLevel, .03f, "nuoc lon dinh");
            yield return GameplayShots.Shot(Path.Combine(ShotDir(), "p3_3_nuoc_lon.png"));
            float t = Time.realtimeSinceStartup;
            while (!s.Transitioning && Time.realtimeSinceStartup - t < 2) yield return null;
            Assert.IsTrue(s.Transitioning, "bi ket thi chuyen canh cuu");
            while (s.Transitioning && Time.realtimeSinceStartup - t < 10) yield return null;
            Assert.AreEqual(70, s.Coins);
            Assert.AreEqual(0, s.Inventory.Count("bread"));
            Assert.AreEqual(0, s.Inventory.Count("berry"));
            Assert.Greater(s.Inventory.Count("wood"), 0, "go van con");
            Assert.IsTrue(d.IsPier(s.Player.transform.position.x, s.Player.transform.position.z), "ve cau tau");
        }

        [UnityTest, Timeout(600000)]
        public IEnumerator SucSong_RacDatBo_PhaiMau_BieuTuongCay()
        {
            yield return Boot();
            var s = GameSession.Instance;
            var d = IslandTerrain.Instance.Data;
            var rs = s.Restoration;
            Assert.IsNotNull(rs);
            for (int i = 0; i < 5; i++) yield return null;
            Assert.IsNotNull(rs.Model, "co du lieu vung");
            Assert.IsNotNull(rs.Current, "dung tren cau tau thi o 1 vung");

            // rac dat bo quanh nguoi choi
            var debris = new List<GameObject>();
            foreach (Transform t in rs.transform) if (t.name.StartsWith("Debris_")) debris.Add(t.gameObject);
            Assert.Greater(debris.Count, 0, "co rac dat bo");

            // di toi rac -> +1 suc song vung do
            var dg = debris[0];
            var p = dg.transform.position;
            var reg = rs.Model.RegionAt(p.x, p.z);
            float before = reg.vitality;
            s.Player.PlaceAt(new Vector3(p.x, 0, p.z), 0);
            for (int i = 0; i < 5; i++) yield return null;
            Assert.IsTrue(dg == null, "nhat rac thi rac bien mat");
            Assert.AreEqual(before + 1, reg.vitality, .001f);

            var rig = Camera.main.GetComponent<OrbitCamera>();
            rig.pitch = .55f; rig.distance = 10f;
            s.Clock.Minutes = 9 * 60;
            for (int i = 0; i < 20; i++) yield return null;
            yield return GameplayShots.Shot(Path.Combine(ShotDir(), "p3_4_rac_dat_bo.png"));

            // vung kiet que: dia hinh phai mau; len 50 thi tuoi han
            reg.vitality = 10; rs.Add(d.Idx(Mathf.RoundToInt(p.x), Mathf.RoundToInt(p.z)), 0.001f);
            yield return null;
            var chunk = FindChunkRenderer(reg, rs);
            Assert.IsNotNull(chunk);
            var mpb = new MaterialPropertyBlock();
            chunk.GetPropertyBlock(mpb);
            Assert.Greater(mpb.GetFloat("_Desaturate"), .2f);
            yield return GameplayShots.Shot(Path.Combine(ShotDir(), "p3_5_vung_kiet_que.png"));
            rs.Add(d.Idx(Mathf.RoundToInt(p.x), Mathf.RoundToInt(p.z)), 50);
            yield return null;
            chunk.GetPropertyBlock(mpb);
            Assert.AreEqual(0, mpb.GetFloat("_Desaturate"), .001f);
            yield return GameplayShots.Shot(Path.Combine(ShotDir(), "p3_6_vung_hoi_sinh.png"));

            // bieu tuong rieng cho giong cay (anh tui do)
            s.Inventory.Clear();
            s.Inventory.BagLevel = 2;
            foreach (var id in new[] { "cai_be", "cu_cai", "ca_rot", "toi", "bap", "van_tho", "dua_leo", "ot", "xoai", "chuoi", "sen", "lua",
                                       "seed_toi", "seed_xoai", "melon", "pumpkin", "tomato", "khoai_lang" })
                s.Inventory.Give(id, 3);
            GameHud.Instance.SetInventory(true);
            for (int i = 0; i < 10; i++) yield return null;
            yield return GameplayShots.Shot(Path.Combine(ShotDir(), "p3_7_bieu_tuong_cay.png"));
            GameHud.Instance.SetInventory(false);
        }

        [UnityTest, Timeout(600000)]
        public IEnumerator RuongNuoc_Muong_BoBao_BonPhan()
        {
            yield return Boot();
            var s = GameSession.Instance;
            var d = IslandTerrain.Instance.Data;
            var farm = s.Farm;
            int[] DX = { 1, -1, 0, 0 }, DZ = { 0, 0, 1, -1 };
            bool Land(int i, int j) => d.InBounds(i, j) && farm.CanTill(d.Idx(i, j), out _) && d.HT[d.Idx(i, j)] >= TideClock.MaxLevel + .04f;
            bool Stand(int i, int j) => d.InBounds(i, j) && d.Solid[d.Idx(i, j)] == 0 && d.Walkable(i, j, d.HT[d.Idx(i, j)]);

            // suoi / ao gan thi tran nhat co 2 o dat lien thang hang (A sat nuoc, B sau A)
            int fa = -1, fb = -1, sa = -1, sb = -1; float best = 1e9f;
            for (int k = 0; k < d.N * d.N; k++)
            {
                if (d.Fresh[k] == 0) continue;
                int i = k % d.N, j = k / d.N;
                float dist = Vector2.Distance(new Vector2(i, j), new Vector2(d.CX, d.CZ));
                if (dist >= best) continue;
                for (int q = 0; q < 4; q++)
                {
                    int ai = i + DX[q], aj = j + DZ[q], bi = ai + DX[q], bj = aj + DZ[q];
                    int pi = DZ[q], pj = DX[q];                         // huong vuong goc de dung
                    if (!Land(ai, aj) || !Land(bi, bj) || !Stand(ai + pi, aj + pj) || !Stand(bi + pi, bj + pj)) continue;
                    if (Mathf.Abs(d.HT[d.Idx(ai, aj)] - d.HT[d.Idx(bi, bj)]) > .3f) continue;
                    best = dist; fa = d.Idx(ai, aj); fb = d.Idx(bi, bj); sa = d.Idx(ai + pi, aj + pj); sb = d.Idx(bi + pi, bj + pj);
                    break;
                }
            }
            Assert.GreaterOrEqual(fa, 0, "khong tim duoc cho lam ruong canh suoi");
            Vector3 P(int k) => new Vector3(k % d.N, d.HT[k], k / d.N);

            s.Inventory.Clear(); s.Inventory.BagLevel = 2;
            s.Give("seed_lua", 3, true); s.Give("rong_bien", 2, true); s.Give("stone", 4, true); s.Give("seed_cai_be", 4, true);
            var hoe = s.Items.Get("hoe");
            // A: cuoc 2 lan -> muong co nuoc
            Face(s, P(sa), fa); yield return null;
            Assert.IsTrue(farm.TryUse(hoe)); yield return WaitAct(s);
            Assert.IsTrue(farm.TryUse(hoe)); yield return WaitAct(s);
            Assert.IsTrue(farm.PlotAt(fa).ditch, "cuoc lai o canh suoi thanh muong");
            Assert.IsTrue(farm.DitchWet(fa), "muong noi suoi co nuoc");
            // B: o ruong sat muong -> ngap nuoc, gieo lua duoc
            Face(s, P(sb), fb); yield return null;
            Assert.IsTrue(farm.TryUse(hoe)); yield return WaitAct(s);
            Assert.IsTrue(farm.Flooded(fb), "o sat muong ngap nuoc");
            Assert.IsTrue(farm.TryUse(s.Items.Get("seed_lua"))); yield return WaitAct(s);
            var pb = farm.PlotAt(fb);
            Assert.AreEqual("lua", pb.crop);
            Assert.IsTrue(farm.TryUse(s.Items.Get("rong_bien"))); yield return WaitAct(s);
            Assert.IsTrue(pb.fertilized);
            Assert.AreEqual(1, s.Inventory.Count("rong_bien"));

            // dat trung ven bien: 2 o canh nhau, 1 o dap bo bao
            int la = -1, lb = -1; best = 1e9f;
            for (int k = 0; k < d.N * d.N; k++)
            {
                int i = k % d.N, j = k / d.N;
                if (i + 1 >= d.N || j < 1) continue;
                int k2 = d.Idx(i + 1, j);
                bool Low(int q) => farm.CanTill(q, out _) && d.HT[q] > IslandData.SEA + .08f && d.HT[q] < TideClock.MaxLevel - .05f && d.Solid[q] == 0;
                if (!Low(k) || !Low(k2) || !Stand(i, j - 1) || !Stand(i + 1, j - 1)) continue;
                float dist = Vector2.Distance(new Vector2(i, j), new Vector2(d.CX, d.CZ));
                if (dist < best) { best = dist; la = k; lb = k2; }
            }
            Assert.GreaterOrEqual(la, 0, "khong co dat trung");
            foreach (int k in new[] { la, lb })
            {
                Face(s, P(k - d.N), k); yield return null;
                Assert.IsTrue(farm.TryUse(hoe)); yield return WaitAct(s);
                Assert.IsTrue(farm.PlotAt(k).lowland, "dat trung");
                Assert.IsTrue(farm.TryUse(s.Items.Get("seed_cai_be"))); yield return WaitAct(s);
            }
            Face(s, P(lb - d.N), lb); yield return null;
            int stones = s.Inventory.Count("stone");
            Assert.IsTrue(farm.TryUse(s.Items.Get("shovel"))); yield return WaitAct(s);
            Assert.IsTrue(farm.PlotAt(lb).dike, "dap bo bao");
            Assert.AreEqual(stones - FarmRules.DikeStones, s.Inventory.Count("stone"));
            farm.PlotAt(la).watered = farm.PlotAt(lb).watered = true;

            // anh truoc khi ngu
            var rig = Camera.main.GetComponent<OrbitCamera>();
            s.Player.PlaceAt(P(sb), 0);
            rig.pitch = .6f; rig.distance = 6f;
            s.Clock.Minutes = 9 * 60;
            for (int i = 0; i < 20; i++) yield return null;
            yield return GameplayShots.Shot(Path.Combine(ShotDir(), "p3_8_ruong_nuoc.png"));

            // qua ngay 1 -> 2 (nuoc rong): lua tu tuoi (+1.5 vi bon phan), o trung khong bo bao chet man
            yield return Sleep(s);
            var lua = CropDatabase.Instance.Get("lua"); var cai = CropDatabase.Instance.Get("cai_be");
            Assert.AreEqual((FarmRules.SoilOk(pb, lua) ? 1f : .5f) + FarmRules.FertilizerBonus, pb.growth, .001f, "ruong ngap nuoc tu tuoi + bon phan");
            Assert.IsFalse(pb.Wilting);
            Assert.IsTrue(farm.PlotAt(la).dead, "dat trung khong bo bao: nuoc rong tran vao");
            Assert.AreEqual("nhiễm mặn", farm.PlotAt(la).deadReason);
            Assert.AreEqual(FarmRules.SaltDays, farm.PlotAt(la).saltDays);
            Assert.IsFalse(farm.PlotAt(lb).dead, "co bo bao thi song");
            Assert.AreEqual(FarmRules.SoilOk(farm.PlotAt(lb), cai) ? 1f : .5f, farm.PlotAt(lb).growth, .001f);

            Face(s, P(la - d.N), la); yield return null;
            s.Player.PlaceAt(P(la - d.N + 1), Mathf.PI * .25f);
            rig.yaw = Mathf.PI; rig.pitch = .6f; rig.distance = 6f;
            for (int i = 0; i < 20; i++) yield return null;
            yield return GameplayShots.Shot(Path.Combine(ShotDir(), "p3_9_dat_trung_bo_bao.png"));
        }

        [UnityTest, Timeout(600000)]
        public IEnumerator LanBien_Boi_Lan_NhatDo_CaySanHo_HetHoi()
        {
            yield return Boot();
            var s = GameSession.Instance;
            var d = IslandTerrain.Instance.Data;
            var dive = DivingSystem.Instance;
            Assert.IsNotNull(dive);
            Village.Instance.SetCraftLevel("lan", 2);

            // cho nuoc sau .6-1.1 gan thi tran nhat
            int spot = -1; float best = 1e9f;
            for (int k = 0; k < d.N * d.N; k++)
            {
                if (d.Ocean[k] == 0 || d.Pier[k] != 0) continue;
                float depth = IslandData.SEA - d.HT[k];
                if (depth < .7f || depth > 1.1f) continue;
                float dist = Vector2.Distance(new Vector2(k % d.N, k / d.N), new Vector2(d.CX, d.CZ));
                if (dist < best) { best = dist; spot = k; }
            }
            Assert.GreaterOrEqual(spot, 0, "khong co cho lan");
            s.Clock.Minutes = 10 * 60;
            s.Player.PlaceAt(new Vector3(spot % d.N, 0, spot / d.N), 0);
            for (int i = 0; i < 60; i++) yield return null;
            Assert.IsTrue(dive.Swimming, "nuoc sau thi boi");
            Assert.IsFalse(dive.Underwater);
            var sp = s.Player.transform.position;
            float expectY = Mathf.Max(d.WaterLevel - .95f, d.HeightAt(sp.x, sp.z) + .2f);   // noi tren mat nuoc, cho can thi cham day
            Assert.AreEqual(expectY, sp.y, .1f, "noi tren mat nuoc");
            Assert.IsFalse(s.Transitioning, "dang boi khong bi Bac Chin cuu");
            var rig = Camera.main.GetComponent<OrbitCamera>();
            rig.pitch = .35f; rig.distance = 6f;
            for (int i = 0; i < 10; i++) yield return null;
            yield return GameplayShots.Shot(Path.Combine(ShotDir(), "p6_1_boi.png"));

            Assert.IsTrue(dive.ToggleDive(), "lan xuong");
            Assert.IsTrue(dive.Underwater);
            Assert.Greater(dive.LootCount, 0, "co do day bien");
            rig.pitch = .15f; rig.distance = 4f;
            for (int i = 0; i < 90; i++) yield return null;
            Assert.Less(Camera.main.transform.position.y, d.WaterLevel, "camera xuong duoi nuoc");
            Assert.Less(dive.Breath, dive.MaxBreath, "hoi tho giam");

            // toi do gan nhat roi nhat
            Vector3 lp = Vector3.zero; float bd = 1e9f;
            foreach (var q in dive.LootPositions) { float dd = Vector3.Distance(q, s.Player.transform.position); if (dd < bd) { bd = dd; lp = q; } }
            s.Player.PlaceAt(new Vector3(lp.x + .3f, 0, lp.z), 0);
            for (int i = 0; i < 40; i++) yield return null;
            Assert.IsTrue(dive.Underwater, "van duoi nuoc");
            int before = s.Inventory.TotalItems, loot = dive.LootCount;
            yield return GameplayShots.Shot(Path.Combine(ShotDir(), "p6_2_duoi_nuoc.png"));
            Assert.IsTrue(dive.TryUse());
            Assert.AreEqual(before + 1, s.Inventory.TotalItems, "nhat duoc 1 mon");
            Assert.AreEqual(loot - 1, dive.LootCount);

            // cay san ho: +5 suc song vung
            s.Inventory.BagLevel = 2;
            s.Give("manh_san_ho", 2, true);
            s.Hotbar.Assign(6, "manh_san_ho"); s.Hotbar.Selected = 6;
            var pos = s.Player.transform.position;
            var reg = s.Restoration.Model.RegionAt(pos.x, pos.z);
            float vit = reg != null ? reg.vitality : 0;
            Assert.IsTrue(dive.TryUse());
            Assert.AreEqual(1, dive.Corals.Count);
            if (reg != null) Assert.AreEqual(vit + 5, reg.vitality, .01f);
            Assert.AreEqual(1, s.Inventory.Count("manh_san_ho"));
            for (int i = 0; i < 5; i++) yield return null;
            yield return GameplayShots.Shot(Path.Combine(ShotDir(), "p6_3_cay_san_ho.png"));

            // het hoi: ngat, mat tui + 20% xu, sang ngay moi
            s.Coins = 100;
            dive.Breath = .02f;
            float t = Time.realtimeSinceStartup;
            while (!s.Transitioning && Time.realtimeSinceStartup - t < 3) yield return null;
            Assert.IsTrue(s.Transitioning, "het hoi thi ngat");
            while (s.Transitioning && Time.realtimeSinceStartup - t < 12) yield return null;
            Assert.AreEqual(2, s.Clock.Day);
            Assert.AreEqual(0, s.Inventory.TotalItems, "do trong tui chim mat");
            Assert.AreEqual(80, s.Coins);
            for (int i = 0; i < 5; i++) yield return null;
            Assert.IsFalse(dive.Swimming, "tinh day o leu");
        }

        [UnityTest, Timeout(600000)]
        public IEnumerator GheThung_CheoRaBien_LenBo_CauMucDem_TrauCay()
        {
            yield return Boot();
            var s = GameSession.Instance;
            var d = IslandTerrain.Instance.Data;
            var boats = BoatSystem.Instance;
            Assert.IsNotNull(boats);
            for (int i = 0; i < 5; i++) yield return null;
            var boat = boats.Boat;
            Assert.IsNotNull(boat, "co ghe cua Bac Chin");

            // len ghe
            s.Player.PlaceAt(boat.position + new Vector3(.6f, 0, .6f), 0);
            yield return null;
            Assert.IsNotNull(boats.NearbyAction(s.Player.transform.position, out string label));
            StringAssert.Contains("Lên ghe", label);
            boats.Board();
            Assert.IsTrue(boats.OnBoat);
            for (int i = 0; i < 5; i++) yield return null;
            Assert.AreEqual(boat.position.x, s.Player.transform.position.x, .05f, "ngoi trong ghe");

            // cheo ra cho nuoc sau (dich chuyen thang de khoi gia lap phim)
            int deep = -1; float best = 1e9f;
            for (int k = 0; k < d.N * d.N; k++)
            {
                if (d.Ocean[k] == 0 || IslandData.SEA - d.HT[k] < 1.2f) continue;
                float dist = Vector2.Distance(new Vector2(k % d.N, k / d.N), new Vector2(d.CX, d.CZ));
                if (dist < best) { best = dist; deep = k; }
            }
            s.Player.Drive(new Vector3(deep % d.N, d.WaterLevel, deep / d.N), 0, false, 0);
            for (int i = 0; i < 10; i++) yield return null;
            Assert.IsTrue(boats.OnBoat);
            Assert.IsFalse(s.Transitioning, "tren ghe khong bi ket nuoc");
            Assert.IsFalse(DivingSystem.Instance.Swimming, "tren ghe khong boi");
            Assert.AreEqual(d.WaterLevel, boat.position.y, .06f, "ghe noi tren mat nuoc");

            // dem: thap den, muc nhieu gap ~4 (trang non nhieu hon trang tron)
            s.Clock.Minutes = 21 * 60;
            for (int i = 0; i < 10; i++) yield return null;
            Assert.IsTrue(boats.LampOn);
            var squid = s.Items.Get("f_muc_la");
            Assert.IsNotNull(squid);
            Assert.AreEqual(4f * 1.5f, BoatSystem.SquidBoost(squid, true, 0), .01f, "dem khong trang");
            Assert.AreEqual(4f * .5f, BoatSystem.SquidBoost(squid, true, .5), .01f, "trang tron");
            Assert.AreEqual(1f, BoatSystem.SquidBoost(s.Items.Get("f_tuna"), true, 0), "ca khac khong doi");
            var rig = Camera.main.GetComponent<OrbitCamera>();
            rig.pitch = .4f; rig.distance = 6f;
            for (int i = 0; i < 20; i++) yield return null;
            yield return GameplayShots.Shot(Path.Combine(ShotDir(), "p6_4_ghe_dem.png"));

            // sang ngay moi: ghe ve ben, nguoi ve leu
            s.Clock.Minutes = 10 * 60;
            yield return Sleep(s);
            Assert.IsFalse(boats.OnBoat);

            // trau cay: co trau trong chuong thi cuoc 1 lan duoc 4 o
            var farm = s.Farm;
            Assert.IsFalse(FarmSystem.HasBuffalo());
            Vector3 home = s.HomePosition; int start = -1;
            for (int z = (int)home.z - 15; z <= home.z + 15 && start < 0; z++)
                for (int x = (int)home.x - 15; x <= home.x + 15 && start < 0; x++)
                {
                    bool ok = d.InBounds(x, z - 1) && d.InBounds(x, z + 3) && d.Solid[d.Idx(x, z - 1)] == 0 && d.Walkable(x, z - 1, d.HT[d.Idx(x, z - 1)]);
                    for (int q = 0; q < 4 && ok; q++) ok = farm.CanTill(d.Idx(x, z + q), out _) && farm.PlotAt(d.Idx(x, z + q)) == null;
                    if (ok) start = d.Idx(x, z);
                }
            Assert.GreaterOrEqual(start, 0, "khong co 4 o dat lien nhau");
            Face(s, new Vector3(start % d.N, d.HT[start - d.N], start / d.N - 1), start);
            int before = farm.Plots.Count;
            // them trau vao danh sach chi trong luc cuoc (khong de he vat nuoi xu ly chuong gia)
            var trau = new FarmAnimal { type = "trau", pen = 0 };
            Animals.Instance.Farm.Add(trau);
            Assert.IsTrue(FarmSystem.HasBuffalo());
            bool used = farm.TryUse(s.Items.Get("hoe"));
            Animals.Instance.Farm.Remove(trau);
            Assert.IsTrue(used); yield return WaitAct(s);
            Assert.AreEqual(before + 4, farm.Plots.Count, "trau cay 4 o");
        }

        static Renderer FindChunkRenderer(RestorationModel.Region reg, RestorationSystem rs)
        {
            var root = IslandTerrain.Instance.transform.Find("TerrainChunks");
            foreach (Transform ch in root)
            {
                var pos = ch.localPosition;
                int u = Mathf.RoundToInt(pos.x + IslandData.M) / RestorationModel.Cell, v = Mathf.RoundToInt(pos.z + IslandData.M) / RestorationModel.Cell;
                if (reg.cells.Contains(v * rs.Model.CellsPerSide + u)) return ch.GetComponent<Renderer>();
            }
            return null;
        }
    }
}
