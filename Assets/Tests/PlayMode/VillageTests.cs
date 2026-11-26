// Giai doan 4a: lang chai - nha, 4 dan lang, noi chuyen, cua hang, den hai dang. Chup anh de duyet.
using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace SereneIsle.Tests
{
    public class VillageTests
    {
        [TearDown]
        public void TearDown()
        {
            foreach (var go in Object.FindObjectsByType<GameObject>())
                if (go && go.transform.parent == null) Object.Destroy(go);
        }

        [UnityTest, Timeout(600000)]
        public IEnumerator Lang_DanLang_CuaHang()
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
            var v = Village.Instance;
            var d = IslandTerrain.Instance.Data;
            var rig = Camera.main.GetComponent<OrbitCamera>();
            string dir = Phase3Tests.Dir();
            Assert.IsNotNull(v, "phai co Village");
            Assert.IsTrue(v.Built);
            foreach (var id in new[] { "shack", "lighthouse", "taphoa", "sauvuon", "chinhut" })
            {
                var b = v.Building(id);
                Assert.IsNotNull(b, "nha " + id);
                Assert.IsNotEmpty(b.tiles);
                foreach (var k in b.tiles) Assert.AreEqual(1, d.Solid[k], $"{id}: o nha la vat can");
            }
            Assert.AreEqual(NpcDefs.All.Length, v.Npcs.Count, "du dan lang (4 + O Man tho lan)");
            Assert.IsNotNull(Placeables.Instance.At(d.TownIdx(0, 3)), "lua trai giua lang van co");

            // ban ngay: ai cung co mat
            s.Clock.Minutes = 10 * 60;
            yield return null; yield return null;
            foreach (var n in v.Npcs) Assert.IsTrue(n.gameObject.activeSelf, n.def.id + " co mat ban ngay");
            foreach (var n in v.Npcs)
                Assert.Greater(n.GetComponentsInChildren<SkinnedMeshRenderer>().Length, 0, n.def.id + " co mo hinh");

            // ---- anh tong the lang ----
            var bay = v.Find("bay");
            var tamShop = v.Building("taphoa");
            var mid = d.TownPoint(-1, 0);
            s.Player.PlaceAt(new Vector3(mid.x, 0, mid.y), d.TownAngle);
            rig.yaw = d.TownAngle + Mathf.PI + .55f; rig.pitch = .78f; rig.distance = 26;
            for (int i = 0; i < 30; i++) yield return null;
            yield return Phase3Tests.Shot(Path.Combine(dir, "p4_0_lang.png"));

            // ---- ong Bay: lan dau gap ke chuyen ----
            s.Player.PlaceAt(bay.transform.position + Fwd(bay.Facing) * 1.6f, bay.Facing + Mathf.PI);
            for (int i = 0; i < 5; i++) yield return null;
            Assert.AreEqual(bay, v.Nearest(s.Player.transform.position), "gan ong Bay");
            Assert.IsFalse(s.Flags.Contains(Village.MetBay));
            v.Talk(bay);
            Assert.IsTrue(ActionUI.Instance.DialogOpen, "mo hop thoai");
            Assert.IsTrue(s.UiOpen);
            rig.yaw = bay.Facing + .6f; rig.pitch = .25f; rig.distance = 4.5f;
            for (int i = 0; i < 30; i++) yield return null;
            Assert.IsTrue(bay.Talking);
            yield return Phase3Tests.Shot(Path.Combine(dir, "p4_1_ong_bay.png"));
            ActionUI.Instance.CloseDialog();
            yield return null;
            Assert.IsFalse(bay.Talking, "dong hop thoai thi het noi chuyen");

            // ---- tung nguoi: anh can canh ----
            int shot = 2;
            foreach (var id in new[] { "chin", "sau", "tam" })
            {
                var n = v.Find(id);
                var b = v.Building(n.def.building);
                // nguoi choi dung canh nguoi do truoc cua; camera phia ngoai (huong cua) nhin vao nha
                var right = new Vector3(Mathf.Cos(b.facing), 0, -Mathf.Sin(b.facing));
                s.Player.PlaceAt(b.door + right * 1.4f + Fwd(b.facing) * .6f, b.facing + Mathf.PI);
                n.PlaceHome();
                rig.yaw = b.facing + .45f; rig.pitch = .32f; rig.distance = 6f;
                for (int i = 0; i < 40; i++) yield return null;
                yield return Phase3Tests.Shot(Path.Combine(dir, $"p4_{shot++}_{id}.png"));
                var hat = System.Array.Find(n.GetComponentsInChildren<Transform>(true), t => t.name == "hat");
                if (n.def.hat) Assert.IsNotNull(hat, id + " doi non la");
            }

            // ---- tiem tap hoa: ban go, mua nuoc ----
            var tam = v.Find("tam");
            s.Player.PlaceAt(tam.transform.position + Fwd(tamShop.facing) * 1.2f, tamShop.facing + Mathf.PI);
            for (int i = 0; i < 3; i++) yield return null;
            Assert.IsNotNull(v.NearbyAction(s.Player.transform.position, out var label), "phim E gan Thim Tam");
            StringAssert.Contains("Thím Tám", label);
            s.Give("wood", 4, true);
            int coins0 = s.Coins, wood0 = s.Inventory.Count("wood");
            ShopUI.Instance.Open(Shops.TapHoa);
            Assert.IsTrue(ShopUI.Instance.IsOpen && s.UiOpen);
            for (int i = 0; i < 10; i++) yield return null;
            yield return Phase3Tests.Shot(Path.Combine(dir, $"p4_{shot++}_cua_hang.png"));
            Assert.IsNotNull(Shops.Sell(s, "wood", -1, out bool sold));
            Assert.IsTrue(sold);
            Assert.AreEqual(0, s.Inventory.Count("wood"));
            Assert.AreEqual(coins0 + wood0 * 2, s.Coins, "go 4 xu -> thim mua 2 xu");
            int bottles = s.Inventory.Count("bottle");
            Shops.Buy(s, new ShopOffer { id = "bottle", price = 10 }, 1, out bool bought);
            Assert.IsTrue(bought);
            Assert.AreEqual(bottles + 1, s.Inventory.Count("bottle"));
            s.Coins = 0;
            Shops.Buy(s, new ShopOffer { id = "bottle", price = 10 }, 1, out bought);
            Assert.IsFalse(bought, "het tien thi khong mua duoc");
            ShopUI.Instance.Close();
            Assert.IsFalse(s.UiOpen);

            // ---- sap hat giong ----
            s.Coins = 500;
            ShopUI.Instance.Open(Shops.HatGiong);
            for (int i = 0; i < 10; i++) yield return null;
            yield return Phase3Tests.Shot(Path.Combine(dir, $"p4_{shot++}_hat_giong.png"));
            var offers = Shops.Offers(Shops.HatGiong, s.Clock.Day, s.Items, CropDatabase.Instance);
            Assert.GreaterOrEqual(offers.Count, 6);
            ShopUI.Instance.Close();

            // ---- ban dem: dan lang ve nha, hai dang sang ----
            s.Player.PlaceAt(bay.home + Fwd(bay.homeFacing) * 3f, bay.homeFacing + Mathf.PI);
            s.Clock.Minutes = 21 * 60 + 30;
            rig.yaw = d.TownAngle + Mathf.PI * .85f; rig.pitch = .4f; rig.distance = 14;
            for (int i = 0; i < 30; i++) yield return null;
            Assert.IsFalse(v.Find("tam").gameObject.activeSelf, "Thim Tam ve nha ban dem");
            Assert.IsTrue(v.Find("bay").gameObject.activeSelf, "ong Bay thuc toi 22 gio");
            Assert.IsTrue(v.Beacon && v.Beacon.enabled, "hai dang sang ban dem");
            yield return Phase3Tests.Shot(Path.Combine(dir, $"p4_{shot++}_dem.png"));
        }

        static Vector3 Fwd(float rad) => new Vector3(Mathf.Sin(rad), 0, Mathf.Cos(rad));
    }
}
