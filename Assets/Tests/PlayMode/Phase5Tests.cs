// Giai doan 5: menu chinh (dao luc hoang hon), cai dat, ghi cong, ung ho, tao nhan vat -> vao game tai cho,
// o luu, ban do nho + ban do lon (zoom, ghim), menu tam dung. Chup anh de duyet.
using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace SereneIsle.Tests
{
    public class Phase5Tests
    {
        string _dir;

        [SetUp]
        public void SetUp()
        {
            _dir = Path.Combine(Path.GetTempPath(), "serene-test-menu");
            if (Directory.Exists(_dir)) foreach (var f in Directory.GetFiles(_dir)) File.Delete(f);
            System.Environment.SetEnvironmentVariable("SERENE_SAVE_DIR", _dir);
            GameSettings.Reset();
        }

        [TearDown]
        public void TearDown()
        {
            System.Environment.SetEnvironmentVariable("SERENE_SAVE_DIR", null);
            GameSettings.Reset();
            foreach (var go in Object.FindObjectsByType<GameObject>())
                if (go && go.transform.parent == null) Object.Destroy(go);
        }

        [UnityTest, Timeout(600000)]
        public IEnumerator Menu_TaoNhanVat_BanDo()
        {
            var boot = new GameObject("Bootstrap");
            boot.SetActive(false);
            var bs = boot.AddComponent<GameBootstrap>(); bs.seed = 424242; bs.saveSlot = 0; bs.mainMenu = true;
            boot.SetActive(true);
            float t0 = Time.realtimeSinceStartup;
            while (!(IslandTerrain.Instance && IslandTerrain.Instance.IsReady))
            {
                if (Time.realtimeSinceStartup - t0 > 180) Assert.Fail("sinh dao qua 180s");
                yield return null;
            }
            for (int i = 0; i < 20; i++) yield return null;
            string dir = Phase3Tests.Dir();
            var s = GameSession.Instance;
            var menu = MainMenu.Instance;

            // ---- menu chinh ----
            Assert.IsTrue(bs.InMenu, "co menu chinh");
            Assert.IsNotNull(menu);
            Assert.IsTrue(menu.IsOpen);
            Assert.IsFalse(s.Running, "chua co nhan vat");
            Assert.IsNull(Object.FindAnyObjectByType<PlayerController>());
            Assert.AreEqual(17.55f, s.Clock.Hour, .01f, "hoang hon");
            Assert.AreEqual(-1, SaveSystem.MostRecent());
            float tw = Time.realtimeSinceStartup;
            while (Time.realtimeSinceStartup - tw < 2) yield return null;
            yield return Phase3Tests.Shot(Path.Combine(dir, "p5_0_menu.png"));

            // ---- cai dat ----
            var root = GameHud.Instance.Root;
            ClickMenu(root, "mb-Cài đặt");
            for (int i = 0; i < 5; i++) yield return null;
            yield return Phase3Tests.Shot(Path.Combine(dir, "p5_1_cai_dat.png"));
            var set = GameSettings.Current;
            set.viewDistance = 0; set.mouseSensitivity = 1.5f;
            GameSettings.Set(set);
            Assert.IsTrue(File.Exists(GameSettings.PathOf), "luu settings.json");
            GameSettings.Reset();
            Assert.AreEqual(0, GameSettings.Current.viewDistance, "doc lai cai dat");
            Assert.AreEqual(100f, IslandTerrain.Instance.Vegetation.farDistance, .1f, "tam nhin gan");
            GameSettings.Set(new GameSettings());

            // ---- ghi cong, ung ho ----
            menu.ShowTitle();
            ClickMenu(root, "mb-Ghi công");
            for (int i = 0; i < 5; i++) yield return null;
            yield return Phase3Tests.Shot(Path.Combine(dir, "p5_2_ghi_cong.png"));
            menu.ShowTitle();
            ClickMenu(root, "mb-Ủng hộ tác giả");
            for (int i = 0; i < 5; i++) yield return null;
            yield return Phase3Tests.Shot(Path.Combine(dir, "p5_3_ung_ho.png"));

            // ---- tao nhan vat ----
            menu.ShowTitle();
            menu.ShowCreate(0);
            menu.SetModel(true);
            var o = CharacterOutfit.Female(); o.shirt = "#7cb66b"; o.longSleeve = true; o.shorts = false;
            menu.SetOutfit(o);
            menu.SetName("   ");
            menu.Go();
            Assert.IsTrue(menu.IsOpen, "ten rong thi khong vao game");
            Assert.AreEqual("Lan", MainMenu.CleanName(" <Lan> "));
            Assert.AreEqual(16, MainMenu.CleanName("12345678901234567890").Length);
            menu.SetName("Lan");
            tw = Time.realtimeSinceStartup;
            while (Time.realtimeSinceStartup - tw < 2) yield return null;
            yield return Phase3Tests.Shot(Path.Combine(dir, "p5_4_tao_nhan_vat.png"));
            menu.Go();
            for (int i = 0; i < 10; i++) yield return null;
            Assert.IsFalse(menu.IsOpen, "vao game thi dong menu");
            Assert.IsFalse(bs.InMenu);
            Assert.IsTrue(s.Running, "da co nhan vat");
            Assert.AreEqual("Lan", s.PlayerName);
            Assert.AreEqual("Rogue", s.Body.outfit.model);
            Assert.AreEqual("#7cb66b", s.Body.outfit.shirt);
            Assert.AreEqual(0, s.SaveSlot);
            Assert.IsTrue(SaveSystem.Exists(0), "van moi tao o luu ngay");
            var info = SaveSystem.Info(0);
            Assert.AreEqual("Lan", info.name);
            Assert.AreEqual("Rogue", info.model);
            Assert.IsFalse(s.UiOpen);

            // ---- ban do ----
            var map = MapUI.Instance;
            tw = Time.realtimeSinceStartup;
            while (!map.Ready && Time.realtimeSinceStartup - tw < 20) yield return null;
            Assert.IsTrue(map.Ready, "nen ban do");
            var d = IslandTerrain.Instance.Data;
            var sea = map.PixelAt(2, 2);
            Assert.Greater(sea.b, sea.r, "goc dao la bien");
            for (int i = 0; i < 15; i++) yield return null;
            yield return Phase3Tests.Shot(Path.Combine(dir, "p5_5_ban_do_nho.png"));
            map.OpenBig();
            Assert.IsTrue(map.BigOpen);
            Assert.IsTrue(s.UiOpen, "mo ban do thi dung thoi gian");
            map.TogglePin(new Vector2(d.CX + 30, d.CZ - 20));
            map.TogglePin(new Vector2(d.CX - 40, d.CZ + 25));
            Assert.AreEqual(2, map.Pins.Count);
            map.TogglePin(new Vector2(d.CX + 30.5f, d.CZ - 20.5f));
            Assert.AreEqual(1, map.Pins.Count, "chuot phai vao ghim cu thi go");
            map.TogglePin(new Vector2(d.CX + 30, d.CZ - 20));
            for (int i = 0; i < 5; i++) yield return null;
            yield return Phase3Tests.Shot(Path.Combine(dir, "p5_6_ban_do_lon.png"));
            map.ZoomAt(new Vector2(200, 200), 1.25f);
            Assert.AreEqual(1.25f, map.Zoom, .001f);
            map.CloseBig();
            Assert.IsFalse(s.UiOpen);

            // ---- luu ghim ----
            Assert.IsTrue(SaveSystem.Save(s, 0));
            var data = SaveSystem.Load(0);
            Assert.AreEqual(2, data.pins.Count);
            Assert.AreEqual("Lan", data.playerName);

            // ---- tam dung ----
            PauseMenu.Instance.Open();
            Assert.IsTrue(PauseMenu.Instance.IsOpen);
            Assert.IsTrue(s.UiOpen);
            for (int i = 0; i < 5; i++) yield return null;
            yield return Phase3Tests.Shot(Path.Combine(dir, "p5_7_tam_dung.png"));
            PauseMenu.Instance.Close();
            Assert.IsFalse(s.UiOpen);
        }

        static void ClickMenu(UnityEngine.UIElements.VisualElement root, string name)
        {
            var b = root.Q(name);
            Assert.IsNotNull(b, "nut " + name);
            using (var e = UnityEngine.UIElements.ClickEvent.GetPooled())
            {
                e.target = b;
                b.SendEvent(e);
            }
        }
    }
}
