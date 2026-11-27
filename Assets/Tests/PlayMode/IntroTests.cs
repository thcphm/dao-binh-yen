// Mo dau van moi (5 canh nhu ban web): chay het canh, chup anh tung canh, bo qua bang Esc / nut.
using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace SereneIsle.Tests
{
    public class IntroTests
    {
        [TearDown]
        public void TearDown()
        {
            foreach (var go in Object.FindObjectsByType<GameObject>())
                if (go && go.transform.parent == null) Object.Destroy(go);
        }

        [UnityTest, Timeout(600000)]
        public IEnumerator MoDau_5Canh()
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
            var intro = IntroUI.Instance;
            Assert.IsNotNull(intro);
            float minutes = s.Clock.Minutes;
            bool done = false;
            intro.Play(false, () => done = true);
            Assert.IsTrue(IntroUI.Playing);
            Assert.IsTrue(s.UiOpen, "khoa dieu khien khi xem mo dau");
            string dir = Phase3Tests.Dir();
            for (int k = 0; k < 5; k++)
            {
                float w = Time.realtimeSinceStartup;
                while (Time.realtimeSinceStartup - w < 4.5f) yield return null;   // chu chay het, man den tan
                yield return Phase3Tests.Shot(Path.Combine(dir, $"p7_intro_{k + 1}.png"));
                intro.SendMessage("Next");
                yield return null;
            }
            Assert.IsTrue(done, "het 5 canh thi vao game");
            Assert.IsFalse(IntroUI.Playing);
            Assert.IsFalse(s.UiOpen);
            Assert.AreEqual(minutes, s.Clock.Minutes, 1f, "tra lai gio (sai so 1 khung hinh)");
            Assert.IsTrue(s.Body.gameObject.activeSelf);
            Assert.IsNull(GameObject.Find("IntroBoat"));

            // bo qua giua chung
            done = false;
            intro.Play(true, () => done = true);
            yield return null;
            intro.End();
            Assert.IsTrue(done);
            Assert.IsFalse(s.UiOpen);
        }
    }
}
