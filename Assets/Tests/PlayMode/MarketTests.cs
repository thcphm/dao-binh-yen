// Giai doan 4b: cho phien (bay sap, khach mua / tra gia / bo di, uy tin, cho tan) va hoc nghe (bai thu, len bac).
using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace SereneIsle.Tests
{
    public class MarketTests
    {
        [TearDown]
        public void TearDown()
        {
            System.Environment.SetEnvironmentVariable("SERENE_SAVE_DIR", null);
            foreach (var go in Object.FindObjectsByType<GameObject>())
                if (go && go.transform.parent == null) Object.Destroy(go);
        }

        [UnityTest, Timeout(600000)]
        public IEnumerator ChoPhien_HocNghe()
        {
            var boot = new GameObject("Bootstrap");
            boot.SetActive(false);
            var bs = boot.AddComponent<GameBootstrap>(); bs.seed = 424242; bs.saveSlot = -1;
            boot.SetActive(true);
            float t0 = Time.realtimeSinceStartup;
            while (!(IslandTerrain.Instance && IslandTerrain.Instance.IsReady))
            {
                if (Time.realtimeSinceStartup - t0 > 180) Assert.Fail("sinh dao qua 180s");
                yield return null;
            }
            for (int i = 0; i < 10; i++) yield return null;

            var s = GameSession.Instance;
            var v = Village.Instance;
            var m = Market.Instance;
            var d = IslandTerrain.Instance.Data;
            var rig = Camera.main.GetComponent<OrbitCamera>();
            string dir = Phase3Tests.Dir();
            s.Flags.Add(Village.MetBay);
            var cho = v.Building("cho");
            Assert.IsNotNull(cho, "co nha long cho");
            Assert.IsNotNull(m);

            // ---- ngay thuong: khong bay sap duoc ----
            s.Clock.Minutes = 9 * 60;
            Assert.IsFalse(m.CanSetUp);
            s.Inventory.BagLevel = 2;
            s.Give("cai_be", 10, true); s.Give("dua_leo", 6, true); s.Give("b_fly", 3, true);
            Assert.IsNotNull(m.Put("cai_be", 3), "ngay thuong khong bay duoc");

            // ---- ngay thu 7 ----
            s.Clock.Day = 7;
            Assert.IsTrue(m.IsOpen);
            Assert.IsNull(m.Put("cai_be", 5));
            Assert.IsNull(m.Put("dua_leo", 6));
            Assert.IsNull(m.Put("b_fly", 3));
            Assert.AreEqual(3, m.Stall.Count);
            Assert.AreEqual(5, s.Inventory.Count("cai_be"), "lay khoi tui");
            m.SetPct(1, 75);
            Assert.AreEqual(80, m.Stall[1].pct, "lam tron 10%");
            m.SetPct(1, 300);
            Assert.AreEqual(Market.MaxPct, m.Stall[1].pct);
            m.SetPct(1, 100);

            // trong sap: dung canh quay
            var right = new Vector3(Mathf.Cos(cho.facing), 0, -Mathf.Sin(cho.facing));
            s.Player.PlaceAt(cho.door + right * 2.4f, cho.facing + Mathf.PI);
            yield return null;
            Assert.IsTrue(m.Attended, "dung gan sap");

            // khach den mua (gia goc, khach thich nong san)
            int coins0 = s.Coins;
            var khach = System.Array.Find(Market.Types, t => t.id == "khach");
            var c = m.SpawnCustomer(khach);
            c.transform.position = cho.door;
            while (m.Stall.Exists(x => x.id != "cai_be")) m.TakeBack(m.Stall.FindIndex(x => x.id != "cai_be"));
            float rep0 = m.Reputation;
            var res = m.Serve(c);
            Assert.AreEqual(Market.Outcome.Bought, res, "gia goc thi khach mua");
            Assert.Greater(s.Coins, coins0);
            Assert.Greater(m.Reputation, rep0, "gia hop ly tang uy tin");
            Assert.Greater(m.SoldToday, 0);

            // gia 160%: khach tra gia (hop thoai) hoac bo di
            m.Stall[0].pct = 160;
            var bacu = System.Array.Find(Market.Types, t => t.id == "tre");
            var c2 = m.SpawnCustomer(bacu);
            res = m.Serve(c2);
            Assert.AreNotEqual(Market.Outcome.Bought, res, "tre con khong mua gia 160%");
            if (res == Market.Outcome.Haggle) { Assert.IsTrue(ActionUI.Instance.DialogOpen); ActionUI.Instance.CloseDialog(); }

            // khong trong sap thi khach khong mua
            s.Player.PlaceAt(cho.door + right * 12f, cho.facing);
            yield return null;
            Assert.AreEqual(Market.Outcome.NoStaff, m.Serve(m.SpawnCustomer(khach)));

            // ---- anh: cho dong khach ----
            while (m.Stall.Count > 0) m.TakeBack(0);
            Assert.IsNull(m.Put("dua_leo", 6)); Assert.IsNull(m.Put("b_fly", 3)); Assert.IsNull(m.Put("cai_be", 4));
            s.Player.PlaceAt(cho.door + right * 1.6f + new Vector3(Mathf.Sin(cho.facing), 0, Mathf.Cos(cho.facing)) * .5f, cho.facing + Mathf.PI);
            for (int i = 0; i < 4; i++) m.SpawnCustomer();
            rig.yaw = cho.facing + .5f; rig.pitch = .42f; rig.distance = 9f;
            float tw = Time.realtimeSinceStartup;
            while (Time.realtimeSinceStartup - tw < 6) yield return null;
            if (ActionUI.Instance.DialogOpen) ActionUI.Instance.CloseDialog();
            yield return Phase3Tests.Shot(Path.Combine(dir, "p4b_0_cho_phien.png"));
            MarketUI.Instance.Open();
            for (int i = 0; i < 5; i++) yield return null;
            Assert.IsTrue(s.UiOpen);
            yield return Phase3Tests.Shot(Path.Combine(dir, "p4b_1_bay_sap.png"));
            MarketUI.Instance.Close();

            // ---- luu: uy tin + sap ----
            string saveDir = Path.Combine(Path.GetTempPath(), "serene-test-saves");
            System.Environment.SetEnvironmentVariable("SERENE_SAVE_DIR", saveDir);
            Assert.IsTrue(SaveSystem.Save(s, 3));
            var data = SaveSystem.Load(3);
            File.Delete(SaveSystem.PathOf(3));
            Assert.AreEqual(m.Reputation, data.marketRep, .001f);
            Assert.AreEqual(m.Stall.Count, data.stall.Count);

            // ---- cho tan: do ve tui ----
            int left = 0; foreach (var x in m.Stall) left += x.n;
            int bag = s.Inventory.TotalItems;
            s.Clock.Minutes = 17 * 60 + 5;
            yield return null; yield return null;
            Assert.AreEqual(0, m.Stall.Count, "cho tan thi don sap");
            Assert.AreEqual(bag + left, s.Inventory.TotalItems, "do chua ban ve tui");

            // ---- hoc nghe: Co Sau, bai thu ----
            s.Clock.Day = 8; s.Clock.Minutes = 10 * 60;
            yield return null;
            Assert.AreEqual(0, Crafts.Level(Crafts.Garden));
            s.Give("fiber", 4, true); s.Give("cai_be", 5, true);
            Assert.IsTrue(Crafts.CanAdvance(Crafts.Get(Crafts.Garden), 0, s.Inventory, s.Items), "du vat lieu bac 1");
            var sau = v.Find("sau");
            s.Player.PlaceAt(sau.transform.position + new Vector3(Mathf.Sin(sau.Facing), 0, Mathf.Cos(sau.Facing)) * 1.5f, sau.Facing + Mathf.PI);
            ApprenticeUI.Instance.Open("sau");
            for (int i = 0; i < 5; i++) yield return null;
            yield return Phase3Tests.Shot(Path.Combine(dir, "p4b_2_hoc_nghe.png"));
            ApprenticeUI.Instance.StartTest(Crafts.Get(Crafts.Garden));
            Assert.IsTrue(ApprenticeUI.Instance.Testing);
            for (int i = 0; i < 20; i++) yield return null;
            yield return Phase3Tests.Shot(Path.Combine(dir, "p4b_3_bai_thu.png"));
            // truot 3 lan -> chua dat, khong mat vat lieu
            int crops = s.Inventory.Count("cai_be") + s.Inventory.Count("dua_leo");
            for (int k = 0; k < 3; k++) { var u = ApprenticeUI.Instance; u.SetNeedle(u.ZoneCenter > .5f ? .02f : .98f); u.Press(); yield return null; }
            Assert.IsFalse(ApprenticeUI.Instance.Testing, $"truot 3 lan thi het bai thu (mo {ApprenticeUI.Instance.IsOpen}, ui {s.UiOpen})");
            Assert.AreEqual(0, Crafts.Level(Crafts.Garden));
            Assert.AreEqual(crops, s.Inventory.Count("cai_be") + s.Inventory.Count("dua_leo"), "truot thi khong mat vat lieu");
            // trung 3 lan -> len bac 1
            ApprenticeUI.Instance.StartTest(Crafts.Get(Crafts.Garden));
            for (int k = 0; k < 3; k++)
            {
                var ui = ApprenticeUI.Instance;
                ui.SetNeedle(ui.ZoneCenter); // kim dung giua vung xanh
                string st = $"kim {ui.Needle:F3}, tam {ui.ZoneCenter:F3}, dang thu {ui.Testing}, mo {ui.IsOpen}, ui {s.UiOpen}";
                Assert.IsTrue(ui.Press(), $"trung lan {k + 1} ({st})");
            }
            Assert.AreEqual(1, Crafts.Level(Crafts.Garden), "dat bai thu -> bac 1");
            Assert.AreEqual(crops - 5, s.Inventory.Count("cai_be") + s.Inventory.Count("dua_leo"), "tru 5 nong san");
            ApprenticeUI.Instance.Close();
            Assert.IsFalse(s.UiOpen);
        }

        /// <summary>Cho kim vao (hoac ra khoi) vung xanh.</summary>
        static IEnumerator WaitNeedle(bool inside)
        {
            var ui = ApprenticeUI.Instance;
            var (zone, _) = Crafts.TestParams(Crafts.Level(Crafts.Garden) + 1);
            float t = Time.realtimeSinceStartup;
            while (Time.realtimeSinceStartup - t < 10)
            {
                bool now = Mathf.Abs(ui.Needle - ui.ZoneCenter) <= zone / 2 - .02f;
                bool far = Mathf.Abs(ui.Needle - ui.ZoneCenter) > zone / 2 + .02f;
                if (inside ? now : far) yield break;
                yield return null;
            }
            Assert.Fail("kim khong toi vi tri can");
        }
    }
}
