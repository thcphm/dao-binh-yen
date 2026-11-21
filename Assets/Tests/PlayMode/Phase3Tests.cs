// Giai doan 3: chat cay, moc lai, che tao, dap da, dao dat, cau ca, luu / tai, di ngu. Chup anh co HUD.
using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace SereneIsle.Tests
{
    public class Phase3Tests
    {
        [TearDown]
        public void TearDown()
        {
            System.Environment.SetEnvironmentVariable("SERENE_SAVE_DIR", null);
            foreach (var go in Object.FindObjectsByType<GameObject>())
                if (go && go.transform.parent == null) Object.Destroy(go);
        }

        [UnityTest, Timeout(600000)] // may 8 GB, co the dang chay Unity khac
        public IEnumerator ChatCay_CheTao_CauCa_Luu()
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
            var d = IslandTerrain.Instance.Data;
            var rig = Camera.main.GetComponent<OrbitCamera>();
            string dir = Dir();
            Assert.IsNotNull(ts, "phai co ToolSystem");
            Assert.IsTrue(s.OwnedTools.Contains("axe") && s.OwnedTools.Contains("shovel"), "co san riu, xeng");
            Assert.IsTrue(ts.HasBench, "phai co ban che tao");

            // ---- chat 1 cay ----
            Assert.IsTrue(FindTarget(d, WorldObjectType.Gum, out int tree, out Vector3 stand, out float face), "tim cay bang");
            s.Player.PlaceAt(stand, face);
            rig.yaw = face + 2.2f; rig.pitch = .45f; rig.distance = 7;
            s.Hotbar.Selected = s.Hotbar.IndexOf("axe");
            // can canh cam riu (kiem huong dung cu trong tay)
            rig.yaw = face + Mathf.PI * .5f; rig.pitch = .15f; rig.distance = 3;
            for (int i = 0; i < 30; i++) yield return null;
            yield return Shot(Path.Combine(dir, "p3_0_cam_riu.png"));
            rig.yaw = face + 2.2f; rig.pitch = .45f; rig.distance = 7;
            int wood0 = s.Inventory.Count("wood");
            var treeType = (WorldObjectType)(d.ObjType[tree] - 1);
            for (int hit = 0; hit < 10 && d.ObjType[tree] != 0; hit++)
            {
                Assert.AreEqual(tree, ts.TargetIdx(), "dung truoc mat cay");
                s.Stats.Energy = 100;
                s.UseSelected();
                yield return WaitAct(s, "axe");
                if (hit == 1) yield return Shot(Path.Combine(dir, "p3_1_chat_cay.png"));
            }
            Assert.AreEqual(0, d.ObjType[tree], "cay phai do");
            Assert.AreEqual(0, d.Solid[tree], "o cay thanh trong");
            Assert.Greater(s.Inventory.Count("wood"), wood0, "phai co them go");
            Assert.AreEqual(1, ts.RemovedObjects.Count);
            yield return new WaitForSeconds(.5f);
            yield return Shot(Path.Combine(dir, "p3_2_cay_do.png"));

            // ---- cay moc lai sau vai ngay ----
            s.Player.PlaceAt(stand + (stand - new Vector3(tree % d.N, 0, tree / d.N)).normalized * 2, face);
            // 3-7 ngay, gap doi neu vung chua hoi sinh (suc song < 50) -> toi da 14 ngay
            Assert.AreEqual(1, ts.Regrow(s.Clock.Day + 15), "cay phai moc lai");
            Assert.AreEqual((byte)(treeType + 1), d.ObjType[tree]);
            s.Player.PlaceAt(stand, face);

            // ---- che tao: cuoc chim, can cau ----
            s.Inventory.BagLevel = 2; // tui da: 24 o, 60 kg - de nhet du vat lieu cho test (tui dau chi 25 kg)
            s.Give("wood", 10, true); s.Give("stone", 6, true); s.Give("fiber", 6, true);
            s.Player.PlaceAt(ts.BenchPosition + new Vector3(1.2f, 0, 0), Mathf.Atan2(-1.2f, 0));
            for (int i = 0; i < 5; i++) yield return null;
            ActionUI.Instance.OpenCrafting();
            Assert.IsTrue(s.UiOpen, "mo ban che tao thi dung thoi gian");
            yield return Shot(Path.Combine(dir, "p3_3_ban_che_tao.png"));
            Assert.IsNotNull(Recipes.Craft(Recipes.Get("pick"), s));
            Assert.IsNotNull(Recipes.Craft(Recipes.Get("rod"), s));
            Assert.IsTrue(s.OwnedTools.Contains("pick") && s.OwnedTools.Contains("rod"));
            Assert.IsNull(Recipes.Craft(Recipes.Get("pick"), s), "da co cuoc chim thi khong lam nua");
            ActionUI.Instance.CloseCrafting();
            Assert.IsFalse(s.UiOpen);

            // ---- dap da ----
            Assert.IsTrue(FindTarget(d, WorldObjectType.Rock, out int rock, out stand, out face), "tim tang da");
            s.Player.PlaceAt(stand, face);
            s.Hotbar.Selected = s.Hotbar.IndexOf("pick");
            int stone0 = s.Inventory.Count("stone");
            for (int hit = 0; hit < 10 && d.ObjType[rock] != 0; hit++)
            {
                s.Stats.Energy = 100;
                s.UseSelected();
                yield return WaitAct(s, "pick");
            }
            Assert.AreEqual(0, d.ObjType[rock], "da phai vo");
            Assert.Greater(s.Inventory.Count("stone"), stone0);

            // ---- dao dat ----
            s.Player.PlaceAt(stand, face); // o da vua vo: trong
            s.Hotbar.Selected = s.Hotbar.IndexOf("shovel");
            s.Stats.Energy = 50;
            s.Stats.ClearRecover(); // bo phan nang luong dang hoi tu cac lan chat truoc
            s.UseSelected();
            int dk = ts.TargetIdx();
            Assert.AreEqual(48, s.Stats.Energy, .01f, $"dao dat ton 2 nang luong (o {dk} / da {rock}, level {d.Level[dk]}, solid {d.Solid[dk]}, " +
                                                     $"dang dien {s.Body.Acting} {s.Body.CurrentAct}, chon {s.Hotbar.SelectedId})");
            yield return WaitAct(s, "shovel");

            // ---- con trung + vot ----
            var bugs = Critters.Instance;
            Assert.IsNotNull(bugs, "phai co Critters");
            bugs.spawnEnabled = false;
            bugs.Clear();
            Critters.Bug natural = null;
            for (int i = 0; i < 20 && natural == null; i++) natural = bugs.Spawn();
            Assert.IsNotNull(natural, "con trung phai tu sinh quanh nguoi choi (ban ngay)");
            Assert.AreEqual(ItemCategory.Bug, s.Items.Get(natural.id).category);
            Assert.IsNotNull(Recipes.Craft(Recipes.Get("net"), s), "lam vot");
            s.Hotbar.Selected = s.Hotbar.IndexOf("net");
            var pp = s.Player.transform.position;
            var fwd = new Vector3(Mathf.Sin(face), 0, Mathf.Cos(face));
            var side = new Vector3(fwd.z, 0, -fwd.x);
            // nhieu loai khac nhau quanh nguoi choi (bo bo duoi dat tu bam mat dat)
            foreach (var (id, off) in new[] {
                         ("b_fly", fwd * 1.2f + Vector3.up * 1f), ("b_dragonfly", fwd * 1.8f + side * .9f + Vector3.up * 1.1f),
                         ("b_birdwing", fwd * 2.2f - side * 1.2f + Vector3.up * 1.3f), ("b_bee", fwd * 1.4f - side * .6f + Vector3.up * .7f),
                         ("b_grasshopper", fwd * 1.3f + side * 1.1f), ("b_ladybug", fwd * .9f - side * 1f),
                         ("b_mantis", fwd * 1.9f + side * .2f), ("b_stag", fwd * 2.4f - side * .4f) })
                bugs.SpawnAt(id, pp + off);
            rig.yaw = face + Mathf.PI + .5f; rig.pitch = .35f; rig.distance = 3.6f;
            for (int i = 0; i < 20; i++) yield return null;
            yield return Shot(Path.Combine(dir, "p3_6_con_trung.png"));
            // dua con buom ra ngay truoc mat roi vung vot
            var fly = bugs.Bugs.Find(b => b.id == "b_fly");
            // vong bay: x = hx + sin(.7t)*1.4..., z = hz + cos(.5t)*1.4 -> t = 0 thi lech z +1.4
            fly.hx = pp.x + fwd.x * 1.1f; fly.hz = pp.z + fwd.z * 1.1f - 1.4f; fly.t = 0;
            yield return null;
            int bugs0 = bugs.Bugs.Count;
            s.Stats.Energy = 100;
            s.UseSelected();
            Assert.AreEqual(bugs0 - 1, bugs.Bugs.Count, "vot phai bat duoc 1 con");
            Assert.IsTrue(s.Caught.Count > 0);
            yield return WaitAct(s, "net");
            bugs.Clear();

            // ---- cau ca o dau cau tau ----
            var arrive = d.ArrivalPoint(out float toTown);
            s.Player.PlaceAt(arrive, toTown + Mathf.PI);
            s.Hotbar.Selected = s.Hotbar.IndexOf("rod");
            rig.yaw = toTown + Mathf.PI + Mathf.PI; rig.pitch = .35f; rig.distance = 8;
            for (int i = 0; i < 10; i++) yield return null;
            ts.Fishing.Press();
            Assert.AreEqual(Fishing.State.Charge, ts.Fishing.Current);
            ts.Fishing.Release(.45f); // quang ~5 o, qua khoi dau cau
            float tf = Time.realtimeSinceStartup;
            while (ts.Fishing.Current != Fishing.State.Bite && Time.realtimeSinceStartup - tf < 15)
            {
                Assert.AreNotEqual(Fishing.State.Idle, ts.Fishing.Current, "khong duoc huy giua chung (moi phai roi xuong nuoc)");
                if (ts.Fishing.Current == Fishing.State.Wait && Time.realtimeSinceStartup - tf > 3 && !File.Exists(Path.Combine(dir, "p3_4_cau_ca.png")))
                    yield return Shot(Path.Combine(dir, "p3_4_cau_ca.png"));
                yield return null;
            }
            Assert.AreEqual(Fishing.State.Bite, ts.Fishing.Current, "ca phai can cau");
            Assert.IsNotNull(ts.Fishing.Fish);
            ts.Fishing.Press();
            Assert.AreEqual(Fishing.State.Reel, ts.Fishing.Current);
            for (int i = 0; i < 20; i++) yield return null;
            yield return Shot(Path.Combine(dir, "p3_5_keo_ca.png"));
            ts.Fishing.Cancel();
            Assert.IsFalse(ts.Fishing.Busy);

            // ---- luu / tai ----
            string saveDir = Path.Combine(Path.GetTempPath(), "serene-test-saves");
            System.Environment.SetEnvironmentVariable("SERENE_SAVE_DIR", saveDir);
            Assert.IsTrue(SaveSystem.Save(s, 3));
            var data = SaveSystem.Load(3);
            Assert.IsNotNull(data);
            Assert.AreEqual(s.Clock.Day, data.day);
            Assert.AreEqual(s.Inventory.Count("wood"), data.inventory.Find(c => c.id == "wood").n);
            Assert.AreEqual(ts.RemovedObjects.Count, data.removed.Count);
            CollectionAssert.Contains(data.ownedTools, "rod");
            Assert.AreEqual(424242, data.seed);
            File.Delete(SaveSystem.PathOf(3));

            // ---- di ngu ----
            int day = s.Clock.Day;
            s.Stats.Energy = 20;
            s.Sleep();
            float tsl = Time.realtimeSinceStartup;
            while ((s.Transitioning || s.Clock.Day == day) && Time.realtimeSinceStartup - tsl < 10) yield return null;
            Assert.AreEqual(day + 1, s.Clock.Day);
            Assert.AreEqual(100, s.Stats.Energy, .01f);
            Assert.AreEqual(7 * 60, s.Clock.Minutes, 1f);
        }

        /// <summary>Tim vat the loai 'type' gan thi tran co o trong ben canh de dung chat.</summary>
        internal static bool FindTarget(IslandData d, WorldObjectType type, out int target, out Vector3 stand, out float face)
        {
            byte code = (byte)(type + 1);
            target = -1; stand = default; face = 0;
            float best = float.MaxValue;
            int[] dx = { 1, -1, 0, 0 }, dz = { 0, 0, 1, -1 };
            for (int k = 0; k < d.N * d.N; k++)
            {
                if (d.ObjType[k] != code) continue;
                int i = k % d.N, j = k / d.N;
                float dist = Mathf.Abs(i - d.CX) + Mathf.Abs(j - d.CZ);
                if (dist >= best) continue;
                for (int q = 0; q < 4; q++)
                {
                    int x = i + dx[q], z = j + dz[q];
                    if (x < 1 || z < 1 || x >= d.N - 1 || z >= d.N - 1) continue;
                    int n = z * d.N + x;
                    if (d.Solid[n] != 0 || d.Level[n] == 0 || Mathf.Abs(d.HT[n] - d.HT[k]) > .3f) continue;
                    best = dist; target = k;
                    stand = new Vector3(x, d.HT[n], z);
                    face = Mathf.Atan2(i - x, j - z);
                    break;
                }
            }
            return target >= 0;
        }

        /// <summary>Cho het dong tac (may tai nang: bo dem dong tac bi gioi han 0.05 s / khung nen cham hon gio that).</summary>
        internal static IEnumerator WaitAct(GameSession s, string act)
        {
            yield return new WaitForSeconds(CharacterActions.DurationOf(act) + .1f);
            float t = Time.realtimeSinceStartup;
            while ((s.Body.Acting || ToolSystem.Instance.Busy) && Time.realtimeSinceStartup - t < 5) yield return null;
        }

        internal static string Dir()
        {
            string dir = System.Environment.GetEnvironmentVariable("SERENE_SHOT_DIR");
            if (string.IsNullOrEmpty(dir)) dir = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Logs", "shots"));
            Directory.CreateDirectory(dir);
            return dir;
        }

        /// <summary>Chup camera + HUD (UI Toolkit ve vao RenderTexture rieng roi chong len).</summary>
        internal static IEnumerator Shot(string path)
        {
            const int W = 1280, H = 720;
            var cam = Camera.main;
            var hud = GameHud.Instance;
            var rtCam = new RenderTexture(W, H, 24, RenderTextureFormat.ARGB32);
            var rtUi = new RenderTexture(W, H, 24, RenderTextureFormat.ARGB32);
            var prevCam = cam.targetTexture;
            cam.targetTexture = rtCam;
            hud.Panel.targetTexture = rtUi;
            hud.Panel.clearColor = true;
            hud.Panel.colorClearValue = new Color(0, 0, 0, 0);
            for (int i = 0; i < 4; i++) yield return null;
            var a = Read(rtCam); var u = Read(rtUi);
            var px = a.GetPixels(); var ui = u.GetPixels();
            for (int i = 0; i < px.Length; i++)
            {
                float al = ui[i].a;
                if (al <= 0) continue;
                px[i] = new Color(ui[i].r + px[i].r * (1 - al), ui[i].g + px[i].g * (1 - al), ui[i].b + px[i].b * (1 - al), 1);
            }
            a.SetPixels(px); a.Apply();
            File.WriteAllBytes(path, a.EncodeToPNG());
            cam.targetTexture = prevCam;
            hud.Panel.targetTexture = null;
            Object.Destroy(a); Object.Destroy(u);
            rtCam.Release(); rtUi.Release(); Object.Destroy(rtCam); Object.Destroy(rtUi);
        }

        static Texture2D Read(RenderTexture rt)
        {
            var old = RenderTexture.active;
            RenderTexture.active = rt;
            var t = new Texture2D(rt.width, rt.height, TextureFormat.RGBA32, false);
            t.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
            t.Apply();
            RenderTexture.active = old;
            return t;
        }
    }
}
