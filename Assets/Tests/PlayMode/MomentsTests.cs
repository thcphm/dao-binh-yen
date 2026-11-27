// Khoanh khac binh yen (muc 7.6): hoang hon tren cau tau, ben lua trai nghe mua, dom dom rung duoc -> +10 binh yen,
// anh chup vao so tay, luu / tai. Chup anh tab so tay de duyet.
using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace SereneIsle.Tests
{
    public class MomentsTests
    {
        string _dir;

        [SetUp]
        public void SetUp()
        {
            _dir = Path.Combine(Path.GetTempPath(), "serene-test-moments");
            if (Directory.Exists(_dir)) Directory.Delete(_dir, true);
            System.Environment.SetEnvironmentVariable("SERENE_SAVE_DIR", _dir);
        }

        [TearDown]
        public void TearDown()
        {
            System.Environment.SetEnvironmentVariable("SERENE_SAVE_DIR", null);
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
        public IEnumerator KhoanhKhac_AnhChup()
        {
            var boot = new GameObject("Bootstrap");
            boot.SetActive(false);
            var bs = boot.AddComponent<GameBootstrap>(); bs.seed = 424242; bs.saveSlot = -1;
            boot.SetActive(true);
            yield return Until(() => IslandTerrain.Instance && IslandTerrain.Instance.IsReady, 180, "sinh dao");
            for (int i = 0; i < 10; i++) yield return null;
            var s = GameSession.Instance;
            var m = Moments.Instance;
            var d = IslandTerrain.Instance.Data;
            var w = WeatherSystem.Instance;
            var rig = Camera.main.GetComponent<OrbitCamera>();
            string dir = Phase3Tests.Dir();
            Assert.IsNotNull(m);
            s.Flags.Add(Village.MetBay);
            w.Override = WeatherKind.Sun; w.SnapVisuals();

            // ---- hoang hon tren cau tau ----
            s.Clock.Day = 3; s.Clock.Minutes = 17.6f * 60;
            var pier = d.TownPoint(0, 18);
            s.Player.PlaceAt(new Vector3(pier.x, 0, pier.y), d.TownAngle + Mathf.PI);
            Assert.AreNotEqual(0, d.Pier[d.Idx(Mathf.RoundToInt(pier.x), Mathf.RoundToInt(pier.y))], "dung tren cau tau");
            rig.yaw = d.TownAngle + .4f; rig.pitch = .25f; rig.distance = 7;
            float calm0 = s.Stats.Calm;
            yield return Until(() => m.Has("hoang_hon_cau_tau"), 15, "khoanh khac hoang hon");
            Assert.AreEqual(Mathf.Min(100, calm0 + Moments.CalmReward), s.Stats.Calm, 1.01f, "+10 binh yen");
            yield return Until(() => File.Exists(m.PhotoPath("hoang_hon_cau_tau")), 5, "anh chup");
            var tex = m.LoadPhoto("hoang_hon_cau_tau");
            Assert.IsNotNull(tex);
            Assert.AreEqual(Moments.PhotoW, tex.width);
            Object.Destroy(tex);
            Assert.IsFalse(m.Try("hoang_hon_cau_tau"), "moi khoanh khac chi 1 lan");

            // ---- ben lua trai nghe mua ----
            w.Override = WeatherKind.Rain; w.SnapVisuals();
            s.Clock.Minutes = 20 * 60;
            var fire = d.TownPoint(0, 2);
            s.Player.PlaceAt(new Vector3(fire.x, 0, fire.y), d.TownAngle);
            rig.yaw = d.TownAngle + Mathf.PI + .6f; rig.pitch = .3f; rig.distance = 6;
            yield return null;
            s.Sit();
            yield return Until(() => m.Has("lua_trai_mua"), 8, "lua trai nghe mua");
            s.Stand();
            yield return Until(() => File.Exists(m.PhotoPath("lua_trai_mua")), 5, "anh lua trai");

            // ---- dom dom rung duoc ----
            w.Override = WeatherKind.Sun; w.SnapVisuals();
            s.Clock.Minutes = 21 * 60;
            int mk = -1;
            for (int k = 0; k < d.N * d.N && mk < 0; k++) if (d.Bio[k] == Biome.Mangrove && d.Level[k] != 0 && d.Solid[k] == 0) mk = k;
            Assert.GreaterOrEqual(mk, 0, "dao co rung duoc");
            s.Player.PlaceAt(new Vector3(mk % d.N, 0, mk / d.N), 0);
            yield return Until(() => m.Has("dom_dom"), 8, "dom dom");

            // ---- so tay: tab khoanh khac ----
            s.Clock.Minutes = 15 * 60;
            yield return null;
            JournalUI.Instance.Open("moment");
            for (int i = 0; i < 5; i++) yield return null;
            Assert.AreEqual("moment", JournalUI.Instance.Tab);
            yield return Phase3Tests.Shot(Path.Combine(dir, "p6g_khoanh_khac.png"));
            JournalUI.Instance.Close();

            // ---- luu / tai ----
            Assert.AreEqual(3, m.Count);
            var back = JsonUtility.FromJson<SaveData>(JsonUtility.ToJson(SaveSystem.Capture(s)));
            m.Restore(null);
            Assert.AreEqual(0, m.Count);
            SaveSystem.ApplyState(back, s);
            Assert.AreEqual(3, m.Count);
            Assert.AreEqual(3, m.DayOf("hoang_hon_cau_tau"));
        }
    }
}
