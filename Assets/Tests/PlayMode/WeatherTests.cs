// Giai doan 6a: thoi tiet trong game - troi xam, mua, bao (chop), cho nghi va dan lang o nha khi bao, go troi sau bao.
using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace SereneIsle.Tests
{
    public class WeatherTests
    {
        [TearDown]
        public void TearDown()
        {
            foreach (var go in Object.FindObjectsByType<GameObject>())
                if (go && go.transform.parent == null) Object.Destroy(go);
        }

        [UnityTest, Timeout(600000)]
        public IEnumerator Mua_Bao()
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
            var w = WeatherSystem.Instance;
            var env = s.Environment;
            var rig = Camera.main.GetComponent<OrbitCamera>();
            string dir = Phase3Tests.Dir();
            Assert.IsNotNull(w);
            s.Flags.Add(Village.MetBay);
            s.Clock.Minutes = 15 * 60;
            var c = IslandTerrain.Instance.Data.TownPoint(0, 1);
            s.Player.PlaceAt(new Vector3(c.x, 0, c.y), IslandTerrain.Instance.Data.TownAngle);
            rig.yaw = IslandTerrain.Instance.Data.TownAngle + Mathf.PI + .5f; rig.pitch = .3f; rig.distance = 12;

            // nang
            w.Override = WeatherKind.Sun; w.SnapVisuals();
            for (int i = 0; i < 20; i++) yield return null;
            Assert.AreEqual(0, env.overcast, .01f);
            yield return Phase3Tests.Shot(Path.Combine(dir, "p6_0_nang.png"));

            // mua
            w.Override = WeatherKind.Rain; w.SnapVisuals();
            float tw = Time.realtimeSinceStartup;
            while (Time.realtimeSinceStartup - tw < 2.5f) yield return null;
            Assert.IsTrue(w.IsRaining);
            Assert.Greater(env.overcast, .5f, "troi xam");
            Assert.Greater(Object.FindAnyObjectByType<RainFX>().ActiveDrops, 100, "co hat mua");
            yield return Phase3Tests.Shot(Path.Combine(dir, "p6_1_mua.png"));

            // bao: dan lang o nha, cho nghi
            s.Clock.Day = 7; // ngay cho
            w.Override = WeatherKind.Storm; w.SnapVisuals();
            tw = Time.realtimeSinceStartup;
            while (Time.realtimeSinceStartup - tw < 2.5f) yield return null;
            Assert.IsTrue(w.IsStorm);
            Assert.IsFalse(Market.Instance.CanSetUp, "bao thi cho nghi");
            foreach (var n in Village.Instance.Npcs) Assert.IsFalse(n.gameObject.activeSelf, n.def.id + " o trong nha khi bao");
            Assert.AreEqual(1f, env.wind, .01f, "gio manh");
            yield return Phase3Tests.Shot(Path.Combine(dir, "p6_2_bao.png"));
            w.FlashNow(); // chop
            yield return null;
            Assert.Greater(env.flash, .5f, "chop sang loa (ban web: chop bi applyDay xoa mat)");
            yield return Phase3Tests.Shot(Path.Combine(dir, "p6_3_chop.png"));

            // sang sau bao: go troi dat vao bai
            w.Override = null;
            int piles0 = Object.FindObjectsByType<ItemPile>().Length;
            int day = 3;
            for (int d = 1; d < 400; d++) if (WeatherSystem.IsStormDay(IslandTerrain.Instance.seed, d)) { day = d; break; }
            s.Clock.Day = day + 1;
            w.DayStartForTest(day + 1);
            Assert.Greater(Object.FindObjectsByType<ItemPile>().Length, piles0, "sau bao co go troi dat vao bai");
            s.Clock.Day = day - 2;
            StringAssert.Contains("hai bữa nữa có bão", w.RadioLine(), "radio bao truoc 2 ngay");
        }
    }
}
