// Dam nuoi ran san ho (thay bao tang) + doi giong voi Co Sau: cay 3 san ho -> mo dam, tha ca bien / do bien (ca nuoc
// ngot khong tha), moc 5 loai co thuong, manh san ho trang tri, luu / tai. Chup anh de duyet.
using System.Collections;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace SereneIsle.Tests
{
    public class ReefPondTests
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

        [UnityTest, Timeout(600000)]
        public IEnumerator DamNuoi_DoiGiong()
        {
            var boot = new GameObject("Bootstrap");
            boot.SetActive(false);
            var bs = boot.AddComponent<GameBootstrap>(); bs.seed = 424242; bs.saveSlot = -1;
            boot.SetActive(true);
            yield return Until(() => IslandTerrain.Instance && IslandTerrain.Instance.IsReady, 180, "sinh dao");
            for (int i = 0; i < 10; i++) yield return null;
            var s = GameSession.Instance;
            var d = IslandTerrain.Instance.Data;
            var pond = ReefPond.Instance;
            var dive = DivingSystem.Instance;
            var rig = Camera.main.GetComponent<OrbitCamera>();
            string dir = Phase3Tests.Dir();
            Assert.IsNotNull(pond); Assert.IsNotNull(dive);
            if (WeatherSystem.Instance) { WeatherSystem.Instance.Override = WeatherKind.Sun; WeatherSystem.Instance.SnapVisuals(); }
            s.Inventory.BagLevel = 2;
            s.Clock.Minutes = 11 * 60;

            // ---- 3 san ho o ran nong gan bo, cach xom mot quang ----
            int center = -1; float best = 1e9f;
            for (int k = 0; k < d.N * d.N; k++)
            {
                if (d.Ocean[k] == 0 || d.Pier[k] != 0) continue;
                float depth = IslandData.SEA - d.HT[k];
                if (depth < .6f || depth > 1.3f) continue;
                int i = k % d.N, j = k / d.N;
                float dist = Vector2.Distance(new Vector2(i, j), new Vector2(d.CX, d.CZ));
                if (dist < 30 || dist > best) continue;
                // can bo dat trong 4 o de dung ngam
                bool shore = false;
                for (int r = 2; r <= 4 && !shore; r++)
                    for (int a = 0; a < 16 && !shore; a++)
                    {
                        int x = Mathf.RoundToInt(i + Mathf.Cos(a * Mathf.PI / 8) * r), z = Mathf.RoundToInt(j + Mathf.Sin(a * Mathf.PI / 8) * r);
                        shore = d.InBounds(x, z) && d.Level[d.Idx(x, z)] != 0 && d.HT[d.Idx(x, z)] > TideClock.MaxLevel + .05f && d.Solid[d.Idx(x, z)] == 0;
                    }
                if (shore) { best = dist; center = k; }
            }
            Assert.GreaterOrEqual(center, 0, "khong co ran nong gan bo");
            Assert.IsFalse(pond.TryOpen(), "chua du san ho");
            Assert.IsFalse(pond.Open);
            dive.RestoreCorals(new[] { center, center + 2, center + d.N });
            Assert.IsTrue(pond.TryOpen(), "du 3 san ho: mo dam");
            Assert.IsTrue(pond.Open);
            Assert.IsNotNull(GameObject.Find("ReefPond"));

            // ---- dung tren bo canh dam ----
            int ci = center % d.N, cj = center / d.N, land = -1; float ld = 1e9f;
            for (int dj = -5; dj <= 5; dj++)
                for (int di = -5; di <= 5; di++)
                {
                    int x = ci + di, z = cj + dj;
                    if (!d.InBounds(x, z)) continue;
                    int q = d.Idx(x, z);
                    if (d.Level[q] == 0 || d.HT[q] < TideClock.MaxLevel + .05f || d.Solid[q] != 0) continue;
                    float dd = di * di + dj * dj;
                    if (dd < ld) { ld = dd; land = q; }
                }
            Assert.GreaterOrEqual(land, 0);
            var lp = new Vector3(land % d.N, 0, land / d.N);
            s.Player.PlaceAt(new Vector3(lp.x, d.GroundHeight(lp.x, lp.z), lp.z), Mathf.Atan2(ci - lp.x, cj - lp.z));
            yield return null;
            var act = ToolSystem.Instance.NearbyAction(out string label);
            var pp = s.Player.transform.position;
            Assert.IsNotNull(pond.NearbyAction(pp, out string pl), $"dam: player {pp} center {pond.Center} land {lp}");
            Assert.IsNotNull(act, "E o dam nuoi: " + label);
            StringAssert.Contains("đầm", label);

            // ---- tha ca: 7 mon, ca tre nuoc ngot khong tha ----
            foreach (var id in new[] { "f_clown", "f_mack", "f_snap", "f_puffer", "f_parrot", "nhum", "sao_bien", "f_catf" }) s.Give(id, 1, true);
            s.Give("manh_san_ho", 3, true);
            int coins = s.Coins;
            Assert.AreEqual(7, pond.Releasable().Count);
            pond.Visit();
            Assert.AreEqual(7, pond.Count);
            Assert.IsTrue(pond.Has("f_clown")); Assert.IsFalse(pond.Has("f_catf"));
            Assert.AreEqual(1, s.Inventory.Count("f_catf"), "ca tre van trong tui");
            Assert.AreEqual(0, s.Inventory.Count("f_clown"));
            Assert.AreEqual(coins + ReefPond.GoalCoins[0], s.Coins, "moc 5 loai");
            Assert.IsTrue(ActionUI.Instance.DialogOpen);
            Assert.IsTrue(ActionUI.Instance.ClickDialog("Thả mảnh san hô"));
            Assert.AreEqual(1, pond.CoralDecor);
            pond.AddCoralDecor(); pond.AddCoralDecor();
            Assert.AreEqual(3, pond.CoralDecor);
            ActionUI.Instance.CloseDialog();
            s.Give("f_clown", 1, true);
            Assert.AreEqual(0, pond.Releasable().Count, "moi loai 1 con");

            rig.yaw = Mathf.Atan2(ci - lp.x, cj - lp.z) + Mathf.PI + .3f; rig.pitch = 1.0f; rig.distance = 7f;
            for (int i = 0; i < 30; i++) yield return null;
            yield return Phase3Tests.Shot(Path.Combine(dir, "p6i_1_dam_nuoi.png"));
            rig.pitch = .95f; rig.distance = 5f;

            for (int i = 0; i < 30; i++) yield return null;
            yield return Phase3Tests.Shot(Path.Combine(dir, "p6i_2_dam_nuoi_gan.png"));

            // ---- so tay: dong dam nuoi ----
            JournalUI.Instance.Open("book");
            for (int i = 0; i < 4; i++) yield return null;
            yield return Phase3Tests.Shot(Path.Combine(dir, "p6i_3_so_tay_dam.png"));
            JournalUI.Instance.Close();

            // ---- doi giong voi Co Sau ----
            var ex = SeedExchange.Instance;
            Assert.IsNotNull(ex);
            Assert.IsFalse(ex.CanSwap, "chua co hat giu giong");
            s.Give("giong_cai_be", 2, true); s.Give("giong_ot", 2, true);
            Assert.IsTrue(ex.CanSwap);
            string rare = ex.RareSeed;
            int had = s.Inventory.Count(rare);
            Assert.IsTrue(ex.Swap());
            Assert.AreEqual(had + FarmCare.SwapGive, s.Inventory.Count(rare));
            Assert.AreEqual(1, s.Inventory.Count("giong_cai_be") + s.Inventory.Count("giong_ot"), "lay 3 hat");
            Assert.IsFalse(ex.CanSwap, "moi thang 1 lan");

            // ---- luu / tai ----
            var back = JsonUtility.FromJson<SaveData>(JsonUtility.ToJson(SaveSystem.Capture(s)));
            pond.Restore(-1, null, 0);
            ex.Restore(-1);
            Assert.IsFalse(pond.Open);
            SaveSystem.ApplyState(back, s);
            Assert.IsTrue(pond.Open);
            Assert.AreEqual(center, pond.CenterK);
            Assert.AreEqual(7, pond.Count);
            Assert.AreEqual(3, pond.CoralDecor);
            Assert.IsFalse(ex.CanSwap);
        }
    }
}
