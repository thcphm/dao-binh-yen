// Giai doan 4c: thu hoang (8 loai, so, thuan bang qua mong), thu cung (thung giay ngay 3, di theo, vuot ve),
// vat nuoi (chuong, mua giong, cho an, thu san pham), luu / tai. Chup anh de duyet.
using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace SereneIsle.Tests
{
    public class AnimalsTests
    {
        [TearDown]
        public void TearDown()
        {
            System.Environment.SetEnvironmentVariable("SERENE_SAVE_DIR", null);
            foreach (var go in Object.FindObjectsByType<GameObject>())
                if (go && go.transform.parent == null) Object.Destroy(go);
        }

        [UnityTest, Timeout(600000)]
        public IEnumerator ThuHoang_ThuCung_VatNuoi()
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
            var an = Animals.Instance;
            var d = IslandTerrain.Instance.Data;
            var rig = Camera.main.GetComponent<OrbitCamera>();
            string dir = Phase3Tests.Dir();
            Assert.IsNotNull(an, "phai co Animals");
            Assert.IsNotNull(AnimalLibrary.Instance, "phai co AnimalLibrary (menu Nhap thu Quaternius)");
            s.Flags.Add(Village.MetBay);
            s.Clock.Minutes = 15 * 60;

            // ---- thu hoang: tha du 8 loai quanh 1 bai co ----
            an.spawnWild = false;
            an.ClearWild();
            Assert.IsTrue(FindOpen(d, s.HomePosition + new Vector3(0, 0, -14), 6, out var center), "bai trong");
            s.Player.PlaceAt(center, 0);
            int n = 0;
            foreach (var def in AnimalDefs.All)
            {
                if (def.role != AnimalRole.Wild) continue;
                float a = n++ * Mathf.PI * 2 / 8;
                var p = center + new Vector3(Mathf.Sin(a), 0, Mathf.Cos(a)) * 3.2f;
                p.y = d.GroundHeight(p.x, p.z);
                var body = an.SpawnWild(def, p);
                if (def.quaternius != null) Assert.IsTrue(body.Animated, def.id + " co hoat canh");
            }
            Assert.AreEqual(8, an.WildCount);
            rig.yaw = .6f; rig.pitch = .75f; rig.distance = 11;
            for (int i = 0; i < 40; i++) yield return null;
            Assert.GreaterOrEqual(an.Seen.Count, 6, "lan dau thay ghi vao so");
            yield return Phase3Tests.Shot(Path.Combine(dir, "p4c_0_thu_hoang.png"));
            // can canh huou sao
            var deer = System.Array.Find(Object.FindObjectsByType<AnimalBody>(), x => x.Def.id == "huou");
            var toDeer = deer.transform.position - center; toDeer.y = 0;
            s.Player.PlaceAt(center - toDeer.normalized * .5f, Mathf.Atan2(toDeer.x, toDeer.z));
            rig.yaw = Mathf.Atan2(toDeer.x, toDeer.z) + Mathf.PI + .9f; rig.pitch = .22f; rig.distance = 6.5f;
            for (int i = 0; i < 30; i++) yield return null;
            yield return Phase3Tests.Shot(Path.Combine(dir, "p4c_3_huou.png"));
            s.Player.PlaceAt(center, 0);

            // cho an qua mong
            s.Give("berry", 3, true);
            var rabbit = System.Array.Find(Object.FindObjectsByType<AnimalBody>(), x => x.Def.id == "tho");
            s.Player.PlaceAt(rabbit.transform.position - new Vector3(0, 0, .9f), 0);
            yield return null;
            s.Player.PlaceAt(rabbit.transform.position - new Vector3(0, 0, .9f), 0); // tho co the vua nhich
            var act = an.NearbyAction(s.Player.transform.position, 0, out var label);
            Assert.IsNotNull(act, "nhin vao tho");
            StringAssert.Contains("quả mọng", label);
            int berries = s.Inventory.Count("berry");
            act();
            Assert.AreEqual(berries - 1, s.Inventory.Count("berry"));
            an.ClearWild();

            // ---- thu cung: ngay 3 co thung giay ----
            s.Clock.Day = 3;
            s.Player.PlaceAt(s.HomePosition, s.HomeFacing);
            for (int i = 0; i < 3; i++) yield return null;
            Assert.IsNotNull(an.PetBox, "ngay 3 co thung giay truoc leu");
            var boxPos = an.PetBox.transform.position;
            var face = Mathf.Atan2(boxPos.x - s.HomePosition.x, boxPos.z - s.HomePosition.z);
            s.Player.PlaceAt(boxPos - new Vector3(Mathf.Sin(face), 0, Mathf.Cos(face)) * 1.0f, face);
            yield return null;
            Assert.IsNotNull(an.NearbyAction(s.Player.transform.position, s.Player.Facing, out label));
            StringAssert.Contains("thùng giấy", label);
            an.Adopt("cho", "Vàng");
            Assert.IsNull(an.PetBox);
            Assert.IsNotNull(an.PetBody);
            Assert.IsTrue(an.PetBody.Animated, "cun Quaternius co hoat canh");
            // di xa -> cun chay theo
            s.Player.PlaceAt(center, 0);
            float tw = Time.realtimeSinceStartup;
            while (Time.realtimeSinceStartup - tw < 4) yield return null;
            Assert.Less(Vector3.Distance(an.PetBody.transform.position, s.Player.transform.position), 6f, "cun di theo");
            an.PetThePet();
            Assert.AreEqual(8, an.Pet.bond);
            an.PetThePet();
            Assert.AreEqual(8, an.Pet.bond, "moi ngay 1 lan");
            rig.yaw = 2.4f; rig.pitch = .3f; rig.distance = 4.5f;
            for (int i = 0; i < 60; i++) yield return null;
            yield return Phase3Tests.Shot(Path.Combine(dir, "p4c_1_thu_cung.png"));

            // ---- vat nuoi ----
            var pl = Placeables.Instance;
            Assert.IsTrue(FindPenSpots(d, pl, center, out int kGa, out int kTrau), "cho dat chuong");
            pl.Add("chuong_ga", kGa);
            pl.Add("chuong_trau", kTrau);
            s.Coins = 5000;
            foreach (var id in new[] { "a_chick", "a_chick", "a_trau", "a_cow" })
            {
                var msg = Shops.Buy(s, new ShopOffer { id = id, price = AnimalDefs.ByCalf(id).price }, 1, out bool ok);
                Assert.IsTrue(ok, msg);
            }
            Assert.AreEqual(5000 - 120 * 2 - 900 - 700, s.Coins);
            Assert.AreEqual(4, an.Farm.Count);
            Shops.Buy(s, new ShopOffer { id = "a_goat", price = 400 }, 1, out bool full);
            Assert.IsFalse(full, "chuong trau bo chi 2 con");
            for (int i = 0; i < 5; i++) yield return null;

            // cho ga an (1 nong san) -> sang mai co trung
            s.Inventory.BagLevel = 2; // tui dau 8 o da day -> trung roi xuong dat
            s.Give("cai_be", 3, true); s.Give("fiber", 6, true);
            var ga = an.Farm.Find(a => a.type == "ga");
            var gaBody = System.Array.Find(Object.FindObjectsByType<AnimalBody>(), b => b.Def.id == "ga");
            var gp = gaBody.transform.position;
            s.Player.PlaceAt(gp - new Vector3(0, 0, .9f), 0);
            yield return null;
            gaBody.transform.position = gp; // dung yen cho chac
            act = an.NearbyAction(s.Player.transform.position, 0, out label);
            Assert.IsNotNull(act, "nhin vao con ga: " + label);
            if (label.Contains("Cho")) act();
            foreach (var a in an.Farm) a.fed = true;
            an.DailyForTest(4);
            Assert.AreEqual(1, ga.ready, "an hom qua -> co trung");
            Assert.AreEqual(2, an.Farm.Find(a => a.type == "trau").ready, "trau cho 2 phan chuong");
            int eggs = s.Inventory.Count("egg");
            ga.ready = 1;
            s.Player.PlaceAt(gaBody.transform.position - new Vector3(0, 0, .9f), 0);
            yield return null;
            act = an.NearbyAction(s.Player.transform.position, 0, out label);
            if (act != null && label.Contains("trứng")) { act(); Assert.AreEqual(eggs + 1, s.Inventory.Count("egg")); }

            // anh chuong trai
            // nguoi choi dung phia truoc chuong trau (huong +z cua chuong), camera sau lung nhin vao chuong
            var pv = new Vector3(kTrau % d.N, d.HT[kTrau], kTrau / d.N);
            var stand = pv + new Vector3(2.2f, 0, 4.2f);
            var look = pv + new Vector3(0, 0, 1.4f) - stand; look.y = 0;
            s.Player.PlaceAt(stand, Mathf.Atan2(look.x, look.z));
            rig.yaw = Mathf.Atan2(-look.x, -look.z) + .35f; rig.pitch = .32f; rig.distance = 6.5f;
            tw = Time.realtimeSinceStartup;
            while (Time.realtimeSinceStartup - tw < 3) yield return null;
            yield return Phase3Tests.Shot(Path.Combine(dir, "p4c_2_vat_nuoi.png"));
            var pg = new Vector3(kGa % d.N, d.HT[kGa], kGa / d.N);
            var stand2 = pg + new Vector3(-1.8f, 0, 3.8f);
            var look2 = pg + new Vector3(0, 0, 1.4f) - stand2; look2.y = 0;
            s.Player.PlaceAt(stand2, Mathf.Atan2(look2.x, look2.z));
            rig.yaw = Mathf.Atan2(-look2.x, -look2.z) - .35f; rig.pitch = .3f; rig.distance = 5.5f;
            tw = Time.realtimeSinceStartup;
            while (Time.realtimeSinceStartup - tw < 2) yield return null;
            yield return Phase3Tests.Shot(Path.Combine(dir, "p4c_4_chuong_ga.png"));

            // ---- luu / tai ----
            string saveDir = Path.Combine(Path.GetTempPath(), "serene-test-saves");
            System.Environment.SetEnvironmentVariable("SERENE_SAVE_DIR", saveDir);
            Assert.IsTrue(SaveSystem.Save(s, 3));
            var data = SaveSystem.Load(3);
            File.Delete(SaveSystem.PathOf(3));
            Assert.AreEqual("Vàng", data.pet.name);
            Assert.AreEqual(4, data.farmAnimals.Count);
            an.Restore(null, null, null);
            Assert.IsNull(an.Pet);
            an.Restore(data.pet, data.farmAnimals, data.seenAnimals);
            Assert.AreEqual("cho", an.Pet.type);
            Assert.AreEqual(4, an.Farm.Count);
            Assert.GreaterOrEqual(an.Seen.Count, 6);
            yield return null; yield return null;
            Assert.IsNotNull(an.PetBody, "tai lai thi cun hien lai");
        }

        static Vector3 FindBody(Animals an, string id)
        {
            var b = System.Array.Find(Object.FindObjectsByType<AnimalBody>(), x => x.Def.id == id);
            Assert.IsNotNull(b, "co " + id);
            return b.transform.position;
        }

        /// <summary>Bai dat trong (khong vat can, khong nuoc) ban kinh r quanh 1 diem gan from.</summary>
        static bool FindOpen(IslandData d, Vector3 from, int r, out Vector3 center)
        {
            for (int rad = 0; rad < 50; rad++)
                for (int dz = -rad; dz <= rad; dz++)
                    for (int dx = -rad; dx <= rad; dx++)
                    {
                        if (Mathf.Max(Mathf.Abs(dx), Mathf.Abs(dz)) != rad) continue;
                        int ci = Mathf.RoundToInt(from.x) + dx, cj = Mathf.RoundToInt(from.z) + dz;
                        bool ok = d.InBounds(ci - r, cj - r) && d.InBounds(ci + r, cj + r);
                        for (int j = cj - r; ok && j <= cj + r; j++)
                            for (int i = ci - r; ok && i <= ci + r; i++)
                            {
                                int k = d.Idx(i, j);
                                if (d.Level[k] == 0 || d.WaterDepth(i, j) > 0 || d.Pier[k] != 0) ok = false;
                                else if (d.Solid[k] != 0 && d.ObjType[k] == 0) ok = false;
                            }
                        if (!ok) continue;
                        for (int j = cj - r; j <= cj + r; j++)
                            for (int i = ci - r; i <= ci + r; i++)
                            {
                                int k = d.Idx(i, j);
                                if (d.ObjType[k] != 0) ToolSystem.Instance.RemoveObject(k, (WorldObjectType)(d.ObjType[k] - 1), 9999);
                            }
                        center = new Vector3(ci, d.HT[d.Idx(ci, cj)], cj);
                        return true;
                    }
            center = default;
            return false;
        }

        static bool FindPenSpots(IslandData d, Placeables pl, Vector3 c, out int a, out int b)
        {
            a = b = -1;
            for (int dz = -4; dz <= 4 && b < 0; dz++)
                for (int dx = -4; dx <= 4; dx++)
                {
                    int k = d.Idx(Mathf.RoundToInt(c.x) + dx, Mathf.RoundToInt(c.z) + dz);
                    if (Mathf.Abs(dx) < 2 && Mathf.Abs(dz) < 2) continue;
                    if (!pl.CanPlace(k, "chuong_ga", out _)) continue;
                    if (a < 0) a = k;
                    else if (Mathf.Abs(k % d.N - a % d.N) > 3) { b = k; break; }
                }
            return a >= 0 && b >= 0;
        }
    }
}
