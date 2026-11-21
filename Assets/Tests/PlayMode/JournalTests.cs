// Giai doan 6b/6c: nau an o lua trai, ngoi suoi lua (binh yen), so tay (so sinh vat, cot moc, nhat ky),
// chai thu tren bai bien, ban do kho bau + dao xeng, luu/tai. Chup anh de duyet.
using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace SereneIsle.Tests
{
    public class JournalTests
    {
        [TearDown]
        public void TearDown()
        {
            foreach (var go in Object.FindObjectsByType<GameObject>())
                if (go && go.transform.parent == null) Object.Destroy(go);
        }

        static IEnumerator Wait(float secs)
        {
            float t = Time.realtimeSinceStartup;
            while (Time.realtimeSinceStartup - t < secs) yield return null;
        }

        [UnityTest, Timeout(600000)]
        public IEnumerator NauAn_SoTay_ChaiThu()
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
            var d = IslandTerrain.Instance.Data;
            var rig = Camera.main.GetComponent<OrbitCamera>();
            var j = Journal.Instance;
            var b = Bottles.Instance;
            string dir = Phase3Tests.Dir();
            Assert.IsNotNull(j); Assert.IsNotNull(b);
            if (WeatherSystem.Instance) { WeatherSystem.Instance.Override = WeatherKind.Sun; WeatherSystem.Instance.SnapVisuals(); }
            s.Inventory.BagLevel = 2;

            // ---- nau an ben lua trai giua lang ----
            s.Clock.Minutes = 19 * 60;
            var c = d.TownPoint(0, 2);
            s.Player.PlaceAt(new Vector3(c.x, 0, c.y), d.TownAngle);
            rig.yaw = d.TownAngle + Mathf.PI + .6f; rig.pitch = .35f; rig.distance = 7;
            yield return null;
            Assert.IsTrue(s.NearFire(3), "dung canh lua trai lang");
            var act = ToolSystem.Instance.NearbyAction(out string label);
            Assert.IsNotNull(act, "phim E o lua trai");
            StringAssert.Contains("Nấu", label);
            s.Inventory.Give("lua", 2); s.Inventory.Give("tomato", 2); s.Inventory.Give("egg", 2);
            var fish = s.Items.items.Find(x => x.category == ItemCategory.Fish);
            s.Inventory.Give(fish.id, 1);
            act();
            for (int i = 0; i < 5; i++) yield return null;
            Assert.IsTrue(CookUI.Instance.IsOpen, "mo bang nau an");
            yield return Phase3Tests.Shot(Path.Combine(dir, "p6_4_nau_an.png"));
            float calm0 = s.Stats.Calm;
            Assert.IsTrue(Cooking.Cook(Cooking.Get("com"), s.Inventory, s.Items));
            Journal.Stat("cook");
            Assert.AreEqual(1, s.Inventory.Count("com"));
            Assert.AreEqual(1f, j.StatOf("cook"), .01f);
            CookUI.Instance.Close();
            Assert.IsFalse(s.UiOpen);

            // ---- ngoi suoi lua: thoi gian nhanh, binh yen + nang luong ----
            s.Stats.Energy = 40;
            float m0 = s.Clock.Minutes;
            s.Sit();
            Assert.IsTrue(s.Sitting);
            yield return Wait(5.6f);
            Assert.Greater(s.Stats.Calm, calm0 + .5f, "ngoi canh lua hoi binh yen");
            Assert.Greater(s.Stats.Energy, 40f, "hoi nang luong");
            Assert.Greater(s.Clock.Minutes - m0, 30f, "thoi gian troi nhanh khi ngoi");
            yield return Phase3Tests.Shot(Path.Combine(dir, "p6_5_ngoi_suoi.png"));
            s.Stand();
            Assert.IsFalse(s.Sitting);

            // ---- cot moc: 50 go -> moc Tieu phu I (+100 xu) ----
            int coins0 = s.Coins;
            s.Give("wood", 50, true);
            Assert.AreEqual(1, j.MileLevel("wood"), "moc Tieu phu I");
            Assert.AreEqual(coins0 + Journal.RewardCoins[0], s.Coins, "thuong 100 xu");

            // ---- so tay (J): 4 trang ----
            s.Caught.Add(fish.id);
            s.Stats.Calm = 55;
            for (int i = 0; i < 3; i++) yield return null;
            Assert.GreaterOrEqual(j.PagesUnlocked, 4, "binh yen 55 mo them trang nhat ky");
            Assert.GreaterOrEqual(j.BookKnown(), 1);
            s.Clock.Minutes = 15 * 60;
            foreach (var tab in new[] { "book", "mile", "diary", "calm" })
            {
                JournalUI.Instance.Open(tab);
                for (int i = 0; i < 4; i++) yield return null;
                Assert.IsTrue(JournalUI.Instance.IsOpen);
                Assert.AreEqual(tab, JournalUI.Instance.Tab);
                yield return Phase3Tests.Shot(Path.Combine(dir, $"p6_6_so_tay_{tab}.png"));
            }
            JournalUI.Instance.Close();
            Assert.IsFalse(s.UiOpen);

            // ---- chai thu tren bai bien ----
            Assert.IsTrue(b.Spawn(), "tim duoc bai bien sat song");
            int k = b.Bottle;
            Assert.GreaterOrEqual(k, 0);
            Assert.AreEqual(Biome.Beach, d.Bio[k]);
            var bp = new Vector3(k % d.N, 0, k / d.N);
            s.Player.PlaceAt(bp + new Vector3(.8f, 0, 0), 0);
            rig.yaw = .35f; rig.pitch = .45f; rig.distance = 4; // camera o phia +z: chai ben trai nhan vat
            yield return Wait(.5f);
            Assert.IsNotNull(GameObject.Find("Bottle"), "co chai tren bai");
            act = ToolSystem.Instance.NearbyAction(out label);
            Assert.IsNotNull(act);
            StringAssert.Contains("chai", label);
            yield return Phase3Tests.Shot(Path.Combine(dir, "p6_7_chai_thu.png"));
            int letters0 = b.LettersRead, treasure0 = b.Treasure;
            act();
            for (int i = 0; i < 3; i++) yield return null;
            Assert.AreEqual(-1, b.Bottle, "da nhat chai");
            Assert.IsNull(GameObject.Find("Bottle"));
            Assert.IsTrue(ActionUI.Instance.DialogOpen, "hien thu hoac ban do");
            Assert.IsTrue(b.LettersRead == letters0 + 1 || b.Treasure >= 0, "thu hoac ban do kho bau");
            Assert.AreEqual(1f, j.StatOf("letter"), .01f);
            yield return Phase3Tests.Shot(Path.Combine(dir, "p6_8_thu.png"));
            ActionUI.Instance.CloseDialog();

            // ---- kho bau: dau X tren ban do, dao bang xeng ----
            if (b.Treasure < 0) b.Restore(-1, FindLand(d, bp), b.LettersRead);
            int tk = b.Treasure;
            Assert.GreaterOrEqual(tk, 0);
            MapUI.Instance.OpenBig();
            for (int i = 0; i < 5; i++) yield return null;
            Assert.IsNotNull(GameHud.Instance.Root.Q(className: "bm-x"), "dau X tren ban do lon");
            yield return Phase3Tests.Shot(Path.Combine(dir, "p6_9_ban_do_kho_bau.png"));
            MapUI.Instance.CloseBig();
            Assert.IsFalse(b.TryDig(d.Idx(tk % d.N + 5, tk / d.N)), "dao lech cho khong ra");
            coins0 = s.Coins;
            Assert.IsTrue(b.TryDig(tk), "dao trung kho bau");
            Assert.GreaterOrEqual(s.Coins - coins0, 300);
            Assert.GreaterOrEqual(s.Inventory.Count("oldcoin"), 1);
            Assert.AreEqual(-1, b.Treasure);
            Assert.AreEqual(1f, j.StatOf("treasure"), .01f);
            ActionUI.Instance.CloseDialog();

            // ---- luu / tai ----
            b.Restore(k, 1234, 3);
            var save = SaveSystem.Capture(s);
            var json = JsonUtility.ToJson(save);
            var back = JsonUtility.FromJson<SaveData>(json);
            j.Restore(null, null, 1);
            b.Restore(-1, -1, 0);
            SaveSystem.ApplyState(back, s);
            Assert.AreEqual(1, j.MileLevel("wood"));
            Assert.AreEqual(1f, j.StatOf("cook"), .01f);
            Assert.GreaterOrEqual(j.PagesUnlocked, 4);
            Assert.AreEqual(k, b.Bottle);
            Assert.AreEqual(1234, b.Treasure);
            Assert.AreEqual(3, b.LettersRead);
        }

        static int FindLand(IslandData d, Vector3 near)
        {
            for (int r = 90; r < 220; r += 5)
                for (int a = 0; a < 24; a++)
                {
                    float ang = a * Mathf.PI / 12;
                    int i = Mathf.RoundToInt(near.x + Mathf.Cos(ang) * r), z = Mathf.RoundToInt(near.z + Mathf.Sin(ang) * r);
                    if (!d.InBounds(i, z)) continue;
                    int k = d.Idx(i, z);
                    if (d.Level[k] != 0 && d.Solid[k] == 0 && d.Bio[k] != Biome.Town) return k;
                }
            return -1;
        }
    }
}
