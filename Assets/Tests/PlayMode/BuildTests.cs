// Giai doan 3: dat do che tao xuong dat (lua trai, duoc, hang rao, lat da, voi tuoi, bu nhin), nhat lai, voi tuoi, luu / tai.
using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace SereneIsle.Tests
{
    public class BuildTests
    {
        [TearDown]
        public void TearDown()
        {
            System.Environment.SetEnvironmentVariable("SERENE_SAVE_DIR", null);
            foreach (var go in Object.FindObjectsByType<GameObject>())
                if (go && go.transform.parent == null) Object.Destroy(go);
        }

        [UnityTest, Timeout(600000)]
        public IEnumerator DatDo_NhatLai_VoiTuoi_Luu()
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
            var ts = ToolSystem.Instance;
            var pl = Placeables.Instance;
            var d = IslandTerrain.Instance.Data;
            var rig = Camera.main.GetComponent<OrbitCamera>();
            string dir = Phase3Tests.Dir();
            Assert.IsNotNull(pl, "phai co Placeables");
            Assert.IsTrue(s.OwnedTools.Contains("build"), "co ban che tao thi co dung cu xay dung");
            var fire = pl.At(d.TownIdx(0, 3));
            Assert.IsNotNull(fire, "lua trai giua lang");
            Assert.AreEqual("campfire", fire.type);

            // ---- che tao ----
            s.Inventory.BagLevel = 2;
            s.Give("wood", 14, true); s.Give("stone", 10, true); s.Give("fiber", 4, true); s.Give("ore", 3, true); // tui 60 kg
            foreach (var id in new[] { "campfire", "torch", "fence", "fence", "path", "path", "sprinkler", "scarecrow", "pick" })
                Assert.IsNotNull(Recipes.Craft(Recipes.Get(id), s), "lam " + id);
            Assert.AreEqual(2, s.Inventory.Count("fence"));

            // ---- cho trong: 1 hang 7 o phia truoc + 2 hang phia sau cho voi tuoi ----
            Assert.IsTrue(FindOpen(d, s, out int x0, out int z0), "tim cho trong gan lang");
            string[] order = { "torch", "fence", "path", "sprinkler", "scarecrow", "campfire", "fence" };
            s.Hotbar.Selected = s.Hotbar.IndexOf("build");
            int n0 = pl.Count;
            for (int q = 0; q < order.Length; q++)
            {
                int k = d.Idx(x0 + q, z0);
                s.Player.PlaceAt(new Vector3(x0 + q, d.HT[d.Idx(x0 + q, z0 - 1)], z0 - 1), 0); // nhin +z
                yield return null;
                Assert.AreEqual(k, ts.TargetIdx(), "nhin vao o can dat");
                pl.Selected = order[q];
                int have = s.Inventory.Count(order[q]);
                s.UseSelected();
                Assert.IsNotNull(pl.At(k), $"dat {order[q]}");
                Assert.AreEqual(order[q], pl.At(k).type);
                Assert.AreEqual(have - 1, s.Inventory.Count(order[q]), "tru 1 trong tui");
                Assert.AreEqual(order[q] == "path" ? 0 : 1, d.Solid[k], "lat da di qua duoc, con lai la vat can");
                yield return Phase3Tests.WaitAct(s, "build");
            }
            Assert.AreEqual(n0 + order.Length, pl.Count);
            int fenceK = d.Idx(x0 + 1, z0), pathK = d.Idx(x0 + 2, z0), sprK = d.Idx(x0 + 3, z0), torchK = d.Idx(x0, z0);
            Assert.AreEqual(d.HT[fenceK] + .9f, d.SolidTop(fenceK), .001f, "nhay qua hang rao");
            Assert.IsFalse(pl.CanPlace(fenceK, "torch", out _), "o da co do");

            // dat lai vao o co do: khong mat do
            s.Player.PlaceAt(new Vector3(x0 + 1, d.HT[d.Idx(x0 + 1, z0 - 1)], z0 - 1), 0);
            yield return null;
            pl.Selected = "path";
            int path0 = s.Inventory.Count("path");
            s.UseSelected();
            Assert.AreEqual(path0, s.Inventory.Count("path"), "o co do roi thi khong dat");
            Assert.AreEqual("fence", pl.At(fenceK).type);

            // R doi mon: chi cac mon con trong tui
            pl.Selected = null;
            Assert.AreEqual("path", pl.CycleSelected(), "chi con lat da trong tui");

            // ---- cuoc ruong canh voi tuoi ----
            int plotK = d.Idx(x0 + 3, z0 + 1);
            s.Player.PlaceAt(new Vector3(x0 + 3, d.HT[d.Idx(x0 + 3, z0 + 2)], z0 + 2), Mathf.PI); // nhin -z
            s.Hotbar.Selected = s.Hotbar.IndexOf("hoe");
            s.Stats.Energy = 100;
            yield return null;
            s.UseSelected();
            Assert.IsNotNull(s.Farm.PlotAt(plotK), "cuoc o canh voi tuoi");
            Assert.IsTrue(pl.Sprinkled(plotK));
            yield return Phase3Tests.WaitAct(s, "hoe");
            // lat da: khong cuoc duoc
            Assert.IsFalse(s.Farm.CanTill(pathK, out _), "khong cuoc tren lat da");

            // ---- anh: ban ngay + ban dem (den lua trai, duoc) ----
            s.Player.PlaceAt(new Vector3(x0 + 3, d.HT[d.Idx(x0 + 3, z0 - 2)], z0 - 2), 0);
            rig.yaw = Mathf.PI * .85f; rig.pitch = .42f; rig.distance = 7.5f;
            s.Clock.Minutes = 16 * 60;
            for (int i = 0; i < 20; i++) yield return null;
            yield return Phase3Tests.Shot(Path.Combine(dir, "p3_7_dat_do.png"));
            s.Clock.Minutes = 21.5f * 60;
            for (int i = 0; i < 20; i++) yield return null;
            int lit = 0;
            foreach (var l in Object.FindObjectsByType<Light>()) if (l.type == LightType.Point && l.enabled && l.intensity > .1f) lit++;
            Assert.GreaterOrEqual(lit, 2, "ban dem lua trai + duoc phai sang");
            yield return Phase3Tests.Shot(Path.Combine(dir, "p3_8_dat_do_dem.png"));
            s.Clock.Minutes = 15 * 60;

            // ---- nhat lai: cuoc chim (hang rao), xeng (duoc) ----
            s.Player.PlaceAt(new Vector3(x0 + 1, d.HT[d.Idx(x0 + 1, z0 - 1)], z0 - 1), 0);
            s.Hotbar.Selected = s.Hotbar.IndexOf("pick");
            yield return null;
            int fence0 = s.Inventory.Count("fence");
            s.UseSelected();
            yield return Phase3Tests.WaitAct(s, "pick");
            Assert.IsNull(pl.At(fenceK), "cuoc chim nhat hang rao");
            Assert.AreEqual(0, d.Solid[fenceK]);
            Assert.AreEqual(fence0 + 1, s.Inventory.Count("fence"));
            s.Player.PlaceAt(new Vector3(x0, d.HT[d.Idx(x0, z0 - 1)], z0 - 1), 0);
            s.Hotbar.Selected = s.Hotbar.IndexOf("shovel");
            yield return null;
            s.UseSelected();
            yield return Phase3Tests.WaitAct(s, "shovel");
            Assert.IsNull(pl.At(torchK), "xeng nhat duoc");
            Assert.AreEqual(1, s.Inventory.Count("torch"));

            // ---- luu / tai ----
            string saveDir = Path.Combine(Path.GetTempPath(), "serene-test-saves");
            System.Environment.SetEnvironmentVariable("SERENE_SAVE_DIR", saveDir);
            Assert.IsTrue(SaveSystem.Save(s, 3));
            var data = SaveSystem.Load(3);
            File.Delete(SaveSystem.PathOf(3));
            Assert.AreEqual(pl.Count, data.placed.Count);
            int count = pl.Count;
            pl.Clear();
            Assert.AreEqual(0, d.Solid[sprK]);
            SaveSystem.ApplyWorld(data, s);
            Assert.AreEqual(count, pl.Count, "tai lai du do da dat");
            Assert.AreEqual("sprinkler", pl.At(sprK).type);
            Assert.AreEqual(1, d.Solid[sprK]);
            Assert.AreEqual(0, d.Solid[pathK]);

            // ---- qua dem: voi tuoi tuoi o ruong ----
            Assert.IsFalse(s.Farm.PlotAt(plotK).watered);
            int day = s.Clock.Day;
            s.Sleep();
            float tsl = Time.realtimeSinceStartup;
            while ((s.Transitioning || s.Clock.Day == day) && Time.realtimeSinceStartup - tsl < 10) yield return null;
            Assert.AreEqual(day + 1, s.Clock.Day);
            Assert.IsTrue(s.Farm.PlotAt(plotK).watered, "voi tuoi tu tuoi moi sang");
        }

        /// <summary>Tim 1 vung 9 x 4 o trong, phang, tren muc nuoc rong, gan lang.</summary>
        static bool FindOpen(IslandData d, GameSession s, out int x0, out int z0)
        {
            var c = d.TownPoint(0, -8);
            int cx = Mathf.RoundToInt(c.x), cz = Mathf.RoundToInt(c.y);
            for (int r = 0; r < 60; r++)
                for (int dz = -r; dz <= r; dz++)
                    for (int dx = -r; dx <= r; dx++)
                    {
                        if (Mathf.Max(Mathf.Abs(dx), Mathf.Abs(dz)) != r) continue;
                        x0 = cx + dx; z0 = cz + dz;
                        if (Open(d, s, x0, z0)) return true;
                    }
            x0 = z0 = -1;
            return false;
        }

        static bool Open(IslandData d, GameSession s, int x0, int z0)
        {
            if (!d.InBounds(x0 - 1, z0 - 3) || !d.InBounds(x0 + 8, z0 + 3)) return false;
            float h0 = d.HT[d.Idx(x0, z0)];
            for (int z = z0 - 3; z <= z0 + 3; z++)
                for (int x = x0 - 1; x <= x0 + 8; x++)
                {
                    int k = d.Idx(x, z);
                    if (d.Level[k] == 0 || d.Solid[k] != 0 || d.Fresh[k] != 0 || d.Pier[k] != 0) return false;
                    if (d.HT[k] < TideClock.MaxLevel + .1f || Mathf.Abs(d.HT[k] - h0) > .3f) return false;
                    if (Placeables.Instance.At(k) != null || s.Farm.PlotAt(k) != null) return false;
                }
            return true;
        }
    }
}
