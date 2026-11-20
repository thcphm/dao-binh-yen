// Smoke test giai doan 2: HUD, hotbar, an uong, tui do, qua ngay luc 23h. Chup anh co ca HUD.
using System.Collections;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace SereneIsle.Tests
{
    public class Phase2SmokeTests
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

        [UnityTest]
        public IEnumerator HUD_AnUong_TuiDo_QuaNgay()
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

            var s = GameSession.Instance;
            Assert.IsNotNull(s, "phai co GameSession");
            Assert.IsTrue(s.HasHome, "phai co leu");
            Assert.IsNotNull(GameHud.Instance, "phai co HUD");
            Assert.AreEqual(2, s.Inventory.Count("bottle"), "do khoi dau");
            Assert.AreEqual("bottle", s.Hotbar[9]);
            Assert.AreEqual(9, s.Hotbar.Selected);

            // uong nuoc qua hotbar
            s.Stats.Water = 40;
            s.UseSelected();
            Assert.AreEqual(1, s.Inventory.Count("bottle"));
            Assert.AreEqual(80, s.Stats.Water, .5f);
            Assert.AreEqual("drink", s.Body.CurrentAct);
            for (int i = 0; i < 20; i++) yield return null;

            string dir = ShotDir();
            yield return Shot(Path.Combine(dir, "p2_1_hud.png"));

            // can nhan vat + lop quan ao rieng
            var rig = Camera.main.GetComponent<OrbitCamera>();
            float y0 = rig.yaw, p0 = rig.pitch, d0 = rig.distance;
            rig.yaw = s.Player.Facing; rig.pitch = .22f; rig.distance = 3.6f; // nhin chinh dien
            for (int i = 0; i < 40; i++) yield return null;
            yield return Shot(Path.Combine(dir, "p2_5_nhan_vat.png"));
            var body = s.Body;
            CollectionAssert.IsSubsetOf(new[] { "shirt", "pants", "shoes", "hair" }, new List<string>(body.ClothingSlots));
            Assert.IsNotNull(body.Hand, "phai co diem cam do (handslot.r)");
            var o0 = body.outfit.Clone();
            var o = body.outfit.Clone();
            o.shirt = "#e46a4e"; o.shorts = !o.shorts; o.longSleeve = !o.longSleeve;
            body.ApplyOutfit(o);
            yield return Shot(Path.Combine(dir, "p2_6_doi_ao.png"));
            body.ApplyOutfit(o0);

            // chan troi luc 17h: vom troi, mat troi, may
            s.Clock.Minutes = 17 * 60;
            rig.yaw = s.Player.Facing + Mathf.PI * .8f; rig.pitch = .07f; rig.distance = 9f;
            for (int i = 0; i < 20; i++) yield return null;
            yield return Shot(Path.Combine(dir, "p2_7_bau_troi.png"));
            s.Clock.Minutes = 14 * 60;
            rig.yaw = y0; rig.pitch = p0; rig.distance = d0;
            for (int i = 0; i < 10; i++) yield return null;

            // mo tui do: thoi gian dung lai
            GameHud.Instance.SetInventory(true);
            float m0 = s.Clock.Minutes;
            for (int i = 0; i < 30; i++) yield return null;
            Assert.AreEqual(m0, s.Clock.Minutes, .001f, "mo tui thi dung dong ho");
            Assert.IsTrue(s.Player.InputLocked);
            yield return Shot(Path.Combine(dir, "p2_2_tui_do.png"));
            GameHud.Instance.SetInventory(false);
            yield return null;

            // 23h ma dang o cau tau (xa leu) -> ngat, mat het do, ve leu
            s.Stats.Water = 100;
            s.Clock.Minutes = DayClock.DayEnd - .01f;
            yield return null; yield return null;
            Assert.IsTrue(s.Transitioning, "phai bat dau chuyen ngay");
            yield return Shot(Path.Combine(dir, "p2_3_chuyen_ngay.png"));
            float t1 = Time.realtimeSinceStartup;
            while (s.Transitioning && Time.realtimeSinceStartup - t1 < 10) yield return null;
            Assert.AreEqual(2, s.Clock.Day);
            Assert.AreEqual("07:00", s.Clock.TimeLabel.Substring(0, 5));
            Assert.AreEqual(0, s.Inventory.TotalItems, "23h chua ve leu -> mat het do");
            Assert.AreEqual(60, s.Stats.Energy, 1f);
            Assert.Less(Vector3.Distance(s.Player.transform.position, s.HomePosition), .5f, "duoc dua ve leu");
            yield return new WaitForSecondsRealtime(1.3f); // cho man chuyen ngay tan het (.9s)
            yield return Shot(Path.Combine(dir, "p2_4_sang_ngay_2.png"));
        }

        /// <summary>Chup camera + HUD (UI Toolkit ve rieng vao texture trong suot roi ghep len).</summary>
        static IEnumerator Shot(string path)
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
                // UI Toolkit ghi mau da nhan alpha (premultiplied)
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
