// Giai doan 6d: thu dat lien (ghe thu Bac Chin mung 2) + le hoi am lich (trang tri, li xi, hoa dang, cau ngu,
// trung thu, cung com moi). Chup anh de duyet.
using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace SereneIsle.Tests
{
    public class FestivalTests
    {
        [TearDown]
        public void TearDown()
        {
            foreach (var go in Object.FindObjectsByType<GameObject>())
                if (go && go.transform.parent == null) Object.Destroy(go);
        }

        static IEnumerator Frames(int n) { for (int i = 0; i < n; i++) yield return null; }

        static int Day(int month, int dom) => (month - 1) * LunarCalendar.DaysPerMonth + dom;

        /// <summary>Doi ngay (khong qua man ngu), cho le hoi dung lai trang tri.</summary>
        static IEnumerator GoTo(GameSession s, int day, float hour)
        {
            s.Clock.Day = day; s.Clock.Minutes = hour * 60;
            var w = WeatherSystem.Instance;
            w.Override = WeatherKind.Sun; // anh le hoi: troi quang
            yield return Frames(2);
            w.SnapVisuals();
            yield return Frames(2);
        }

        /// <summary>Dat nhan vat truoc nha, camera nhin vao cua.</summary>
        static void Frame(GameSession s, OrbitCamera rig, VillageBuilding bd, float dist, float yawOff = .35f, float back = 3.2f)
        {
            var fwd = new Vector3(Mathf.Sin(bd.facing), 0, Mathf.Cos(bd.facing));
            s.Player.PlaceAt(bd.door + fwd * back, bd.facing + Mathf.PI);
            rig.yaw = bd.facing + yawOff; rig.pitch = .3f; rig.distance = dist;
        }

        static string Talk(Village v, string id)
        {
            var n = v.Find(id);
            Assert.IsNotNull(n, id);
            Assert.IsTrue(n.gameObject.activeInHierarchy, id + " co mat");
            v.Talk(n);
            Assert.IsTrue(ActionUI.Instance.DialogOpen);
            return ActionUI.Instance.DialogText;
        }

        [UnityTest, Timeout(600000)]
        public IEnumerator ThuNha_LeHoi()
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
            yield return Frames(10);
            var s = GameSession.Instance;
            var v = Village.Instance;
            var f = Festivals.Instance;
            var mail = Mail.Instance;
            var ui = ActionUI.Instance;
            var rig = Camera.main.GetComponent<OrbitCamera>();
            string dir = Phase3Tests.Dir();
            Assert.IsNotNull(f); Assert.IsNotNull(mail);
            var w = WeatherSystem.Instance;
            w.Override = WeatherKind.Sun; w.SnapVisuals();
            s.Flags.Add(Village.MetBay);
            s.Inventory.BagLevel = 2;

            // ---- Tet: trang tri + li xi ----
            yield return GoTo(s, Day(1, 1), 10);
            Assert.AreEqual(Festivals.Tet, f.Today);
            Assert.IsNotNull(f.Decor, "co trang tri Tet");
            int coins0 = s.Coins;
            Frame(s, rig, v.Building("chinhut"), 3, .6f, 1.2f);
            yield return Frames(3);
            StringAssert.Contains("lì xì", Talk(v, "chin"));
            ui.CloseDialog();
            Assert.AreEqual(coins0 + 40, s.Coins, "Bac Chin li xi 40 xu");
            Talk(v, "chin"); ui.CloseDialog();
            Assert.AreEqual(coins0 + 40, s.Coins, "li xi 1 lan");
            yield return GoTo(s, Day(1, 1), 19.3f);
            Frame(s, rig, v.Building("taphoa"), 9);
            yield return Frames(5);
            yield return Phase3Tests.Shot(Path.Combine(dir, "p6d_0_tet.png"));
            Frame(s, rig, v.Building("chinhut"), 5.5f, .9f, 2.5f);
            rig.pitch = .18f;
            yield return Frames(5);
            yield return Phase3Tests.Shot(Path.Combine(dir, "p6d_0b_tet_gan.png"));

            // ---- thu nha: mung 2 ----
            yield return GoTo(s, Day(1, 2), 9);
            Assert.IsNull(f.Today);
            Assert.IsNull(f.Decor, "het Tet thi cat trang tri");
            Assert.IsTrue(mail.Pending, "mung 2 co thu");
            Frame(s, rig, v.Building("chinhut"), 3, .6f, 1.2f);
            yield return Frames(3);
            Talk(v, "chin");
            CollectionAssert.Contains(ui.DialogButtons(), "Nhận thư nhà");
            Assert.IsTrue(ui.ClickDialog("Nhận thư nhà"));
            yield return Frames(2);
            Assert.AreEqual(1, mail.Chapter);
            Assert.IsFalse(mail.Pending);
            Assert.GreaterOrEqual(s.Inventory.Count("seed_cai_be"), 5, "qua hat giong");
            StringAssert.StartsWith("Thư nhà", ui.DialogTitle);
            yield return Phase3Tests.Shot(Path.Combine(dir, "p6d_1_thu_nha.png"));
            Assert.IsTrue(ui.ClickDialog("Viết thư hồi âm"));
            yield return Frames(2);
            Assert.AreEqual(3, ui.DialogButtons().Count, "3 chuyen de ke");
            yield return Phase3Tests.Shot(Path.Combine(dir, "p6d_2_viet_thu.png"));
            ui.CloseDialog();
            s.Give("melon", 1, true);
            mail.Send("people", "melon");
            Assert.IsFalse(mail.CanSend, "moi thang gui 1 lan");
            Assert.AreEqual(0, s.Inventory.Count("melon"), "qua da gui");
            yield return GoTo(s, Day(2, 2), 9);
            Assert.IsTrue(mail.Pending, "thang sau co thu");
            mail.Receive();
            StringAssert.Contains("Ông Bảy khó tính", ui.DialogText, "thu nhac chuyen da ke");
            StringAssert.Contains("dưa hấu", ui.DialogText, "cam on qua");
            ui.CloseDialog();

            // ---- Ram thang gieng: hoa dang ----
            yield return GoTo(s, Day(1, 15), 10);
            Assert.AreEqual(Festivals.RamGieng, f.Today);
            Assert.Greater(f.Decor.GetComponentsInChildren<FloatingLantern>().Length, 5, "hoa dang troi quanh lang");
            var bay = v.Find("bay");
            Frame(s, rig, v.Building("shack"), 3, .6f, 1.2f);
            yield return Frames(3);
            Talk(v, "bay"); ui.CloseDialog();
            Assert.AreEqual(3, s.Inventory.Count("hoa_dang"), "ong Bay cho 3 hoa dang");
            yield return GoTo(s, Day(1, 15), 20);
            var water = FindShore(IslandTerrain.Instance.Data, s);
            s.Player.PlaceAt(water, 0);
            yield return Frames(3);
            var act = ToolSystem.Instance.NearbyAction(out string label);
            Assert.IsNotNull(act, "mep nuoc, troi toi, co hoa dang");
            StringAssert.Contains("hoa đăng", label);
            float calm0 = s.Stats.Calm;
            act();
            Assert.AreEqual(2, s.Inventory.Count("hoa_dang"));
            Assert.AreEqual(1, f.Released);
            Assert.AreEqual(calm0 + 3, s.Stats.Calm, .01f);
            var lan = f.Decor.GetComponentsInChildren<FloatingLantern>();
            var c = IslandTerrain.Instance.Data.TownPoint(0, 0);
            var near = lan[0].transform.position;
            foreach (var l in lan) if (Vector3.Distance(l.transform.position, s.Player.transform.position) < Vector3.Distance(near, s.Player.transform.position)) near = l.transform.position;
            rig.yaw = Mathf.Atan2(s.Player.transform.position.x - near.x, s.Player.transform.position.z - near.z); rig.pitch = .35f; rig.distance = 7;
            yield return Frames(30);
            yield return Phase3Tests.Shot(Path.Combine(dir, "p6d_3_hoa_dang.png"));

            // ---- Cung com moi ----
            yield return GoTo(s, Day(3, 10), 10);
            Assert.AreEqual(Festivals.ComMoi, f.Today);
            s.Give("com", 3, true);
            Frame(s, rig, v.Building("shack"), 3, .6f, 1.2f);
            yield return Frames(3);
            Talk(v, "bay");
            CollectionAssert.Contains(ui.DialogButtons(), "Mời cả làng cơm mới (3 cơm trắng)");
            calm0 = s.Stats.Calm;
            Assert.IsTrue(ui.ClickDialog("Mời cả làng cơm mới (3 cơm trắng)"));
            Assert.AreEqual(0, s.Inventory.Count("com"));
            Assert.AreEqual(Mathf.Min(100, calm0 + 10), s.Stats.Calm, .01f);
            ui.CloseDialog();
            var fire = IslandTerrain.Instance.Data.TownPoint(0, 2);
            s.Player.PlaceAt(new Vector3(fire.x, 0, fire.y), 0);
            rig.yaw = IslandTerrain.Instance.Data.TownAngle + Mathf.PI + .9f; rig.pitch = .45f; rig.distance = 6;
            yield return GoTo(s, Day(3, 10), 17.6f);
            yield return Phase3Tests.Shot(Path.Combine(dir, "p6d_4_com_moi.png"));

            // ---- Cau ngu: cho hop, co duoi ca ----
            yield return GoTo(s, Day(3, 15), 9);
            Assert.AreEqual(Festivals.CauNgu, f.Today);
            Assert.IsTrue(Market.Instance.CanSetUp, "cau ngu hop cho");
            Frame(s, rig, v.Building("cho"), 9, -.75f, 4.5f);
            yield return Frames(5);
            yield return Phase3Tests.Shot(Path.Combine(dir, "p6d_5_cau_ngu.png"));

            // ---- Trung thu: den ong sao, banh, long den dan lang ----
            yield return GoTo(s, Day(6, 15), 10);
            Assert.AreEqual(Festivals.TrungThu, f.Today);
            Frame(s, rig, v.Building("taphoa"), 3, .6f, 1.2f);
            yield return Frames(3);
            Talk(v, "tam"); ui.CloseDialog();
            Assert.AreEqual(2, s.Inventory.Count("banh_trung_thu"));
            yield return GoTo(s, Day(6, 15), 19.5f);
            Frame(s, rig, v.Building("taphoa"), 8);
            yield return Frames(5);
            var tam = v.Find("tam");
            Assert.IsNotNull(tam.transform.Find("LongDen"));
            Assert.IsTrue(tam.transform.Find("LongDen").gameObject.activeSelf, "toi thi cam long den");
            yield return Phase3Tests.Shot(Path.Combine(dir, "p6d_6_trung_thu.png"));

            // ---- so tay: tab thu nha; luu / tai ----
            JournalUI.Instance.Open("mail");
            yield return Frames(4);
            yield return Phase3Tests.Shot(Path.Combine(dir, "p6d_7_so_tay_thu.png"));
            JournalUI.Instance.Close();
            var back = JsonUtility.FromJson<SaveData>(JsonUtility.ToJson(SaveSystem.Capture(s)));
            mail.Restore(0, -1, -1, null, null);
            SaveSystem.ApplyState(back, s);
            Assert.AreEqual(2, mail.Chapter);
            Assert.AreEqual(Mail.MonthOf(Day(2, 2)), mail.LastMonth);
            Assert.IsTrue(s.Flags.Contains("fest1_lixi_chin"), "co li xi luu trong flags");
        }

        /// <summary>O dat sat nuoc gan lang (de tha hoa dang).</summary>
        static Vector3 FindShore(IslandData d, GameSession s)
        {
            var c = d.TownPoint(0, 0);
            for (int r = 4; r < 40; r++)
                for (int a = 0; a < 32; a++)
                {
                    float ang = a * Mathf.PI / 16;
                    int i = Mathf.RoundToInt(c.x + Mathf.Cos(ang) * r), j = Mathf.RoundToInt(c.y + Mathf.Sin(ang) * r);
                    if (!d.InBounds(i, j)) continue;
                    int k = d.Idx(i, j);
                    if (d.Level[k] == 0 || d.Solid[k] != 0 || d.Pier[k] != 0) continue;
                    foreach (var (dx, dz) in new[] { (1, 0), (-1, 0), (0, 1), (0, -1), (2, 0), (-2, 0), (0, 2), (0, -2) })
                        if (d.InBounds(i + dx, j + dz) && d.Level[d.Idx(i + dx, j + dz)] == 0 && d.WaterDepth(i + dx, j + dz) > .3f) return new Vector3(i, 0, j);
                }
            Assert.Fail("khong tim duoc bo nuoc");
            return default;
        }
    }
}
