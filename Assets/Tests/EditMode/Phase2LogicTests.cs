// Test giai doan 2: tui do, hotbar, chi so, luat qua ngay - doi chieu voi luat ban web.
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace SereneIsle.Tests
{
    public class Phase2LogicTests
    {
        static ItemDatabase _db;
        static ItemDatabase DB => _db ? _db : (_db = ItemDatabase.CreateInMemory());

        // ------------------------------------------------------------ du lieu item
        [Test]
        public void DuLieuItem_DuVaKhongTrung()
        {
            var ids = DB.items.Select(i => i.id).ToList();
            Assert.AreEqual(ids.Count, ids.Distinct().Count(), "id trung");
            Assert.AreEqual(9, DB.items.Count(i => i.IsTool), "9 dung cu");
            Assert.AreEqual(31, DB.items.Count(i => i.category == ItemCategory.Fish), "30 ca FISH + f_trout");
            Assert.AreEqual(6, DB.items.Count(i => i.category == ItemCategory.Bug));
            foreach (var it in DB.items)
            {
                Assert.IsFalse(string.IsNullOrEmpty(it.displayName), it.id);
                Assert.AreNotEqual(IconShape.None, it.icon, it.id + " thieu bieu tuong");
                if (it.GoesInBag) Assert.Greater(it.weight, 0, it.id + " thieu khoi luong");
            }
            Assert.AreEqual("Quả dừa", DB.Get("coconut").displayName);
            Assert.AreEqual(22, DB.Get("coconut").eatWater);
            Assert.IsTrue(DB.Get("bottle").IsDrink);
            Assert.IsFalse(DB.Get("coconut").IsDrink);
        }

        // ------------------------------------------------------------ tui do
        [Test]
        public void Tui_Chong99_Va8O()
        {
            var inv = new Inventory(DB);
            Assert.AreEqual(8, inv.Capacity);
            Assert.AreEqual(150, inv.Give("fiber", 150)); // 0.1 kg x 150 = 15 kg
            Assert.AreEqual(2, inv.SlotsUsed(), "150 cai = 2 o");
            Assert.AreEqual(new[] { 99, 51 }, inv.Stacks().Select(s => s.Value).ToArray());
            Assert.IsTrue(inv.Take("fiber", 51));
            Assert.AreEqual(1, inv.SlotsUsed());
        }

        [Test]
        public void Tui_GioiHanO()
        {
            var inv = new Inventory(DB);
            string[] light = { "berry", "worm", "shell", "oldcoin", "seed_tomato", "seed_wheat", "seed_pumpkin", "seed_melon" };
            foreach (var id in light) Assert.AreEqual(1, inv.Give(id, 1), id);
            Assert.AreEqual(8, inv.SlotsUsed());
            Assert.AreEqual(0, inv.Give("fiber", 1), "het o thi khong nhan loai moi");
            Assert.AreEqual(98, inv.RoomFor("berry"), "van them duoc vao chong dang co");
        }

        [Test]
        public void Tui_GioiHanKhoiLuong_VaBaLo()
        {
            var inv = new Inventory(DB);
            Assert.AreEqual(25f, inv.MaxWeight);
            // da 1.5 kg -> toi da 16 vien (24 kg)
            Assert.AreEqual(16, inv.Give("stone", 40));
            Assert.AreEqual(0, inv.RoomFor("pumpkin"), "bi ngo 4 kg khong vua 1 kg con lai");
            inv.BagLevel = 1;
            Assert.AreEqual(16, inv.Capacity);
            Assert.AreEqual(40f, inv.MaxWeight);
            Assert.AreEqual(10, inv.Give("stone", 40), "40 kg - 24 kg = 16 kg -> 10 vien");
        }

        // ------------------------------------------------------------ hotbar
        [Test]
        public void Hotbar_MacDinhVaGanDoiCho()
        {
            var hb = new Hotbar();
            Assert.AreEqual("axe", hb[0]);
            Assert.AreEqual("", hb[9]);
            hb.Selected = 2; // dang cam cuoc dat
            hb.Assign(9, "bottle");
            Assert.AreEqual("bottle", hb[9]);
            Assert.AreEqual(2, hb.Selected, "o dang chon di theo mon dang cam");
            hb.Assign(0, "bottle"); // keo chai nuoc tu o 9 sang o 0: doi cho voi riu
            Assert.AreEqual("bottle", hb[0]);
            Assert.AreEqual("axe", hb[9]);
            hb.Swap(0, 2);
            Assert.AreEqual("hoe", hb[0]);
            Assert.AreEqual(0, hb.Selected, "cuoc dat dang chon -> chon theo sang o 0");
            hb.Clear(0);
            Assert.AreEqual("", hb[0]);
        }

        // ------------------------------------------------------------ chi so
        [Test]
        public void NangLuong_7hDen12h_KhongAn_Con50()
        {
            var st = new PlayerStats();
            // 5 gio game = 150 giay that, buoc 0.05 s
            for (int i = 0; i < 3000; i++) st.Tick(.05f, .05f * 2 / 60f, false);
            Assert.AreEqual(50f, st.Energy, .5f);
            Assert.AreEqual(100 - 25f, st.Water, .5f, "nuoc -5/gio");
            Assert.AreEqual(100 - 5f, st.Hp, .5f, "mau -1/gio");
        }

        [Test]
        public void Chay_TonGapDoi_Va80PhanTramHoiLaiSauKhiNghi()
        {
            var st = new PlayerStats();
            for (int i = 0; i < 600; i++) st.Tick(.05f, .05f * 2 / 60f, true); // 30 s chay = 1 gio game
            Assert.AreEqual(80f, st.Energy, .3f, "chay: -20% / gio");
            Assert.AreEqual(8f, st.Recover, .3f, "80% phan ton them khi chay");
            // nghi 3 giay: hoi lai gan het phan Recover (12%/s), con bi tru 10%/gio binh thuong
            for (int i = 0; i < 60; i++) st.Tick(.05f, .05f * 2 / 60f, false);
            Assert.Less(st.Recover, .01f);
            Assert.AreEqual(80f + 8f - .1f * 100f * (3 * 2 / 60f), st.Energy, .3f);
        }

        [Test]
        public void DungDungCu_TonNangLuong_HoiLaiSau1Giay()
        {
            var st = new PlayerStats();
            Assert.IsTrue(st.UseEnergy(10));
            Assert.AreEqual(90f, st.Energy, .01f);
            Assert.AreEqual(8f, st.Recover, .01f);
            st.Tick(.9f, 0, false);
            Assert.AreEqual(90f, st.Energy, .01f, "chua du 1 giay nghi");
            for (int i = 0; i < 40; i++) st.Tick(.05f, 0, false);
            Assert.AreEqual(98f, st.Energy, .01f, "hoi 80% = 8");
            st.Energy = 3;
            var warn = new List<string>();
            st.Warning += warn.Add;
            Assert.IsFalse(st.UseEnergy(5));
            Assert.That(warn, Has.Some.Contains("Quá mệt"));
        }

        [Test]
        public void HetNuocVaNangLuong_MatMauNhanh()
        {
            var st = new PlayerStats { Water = 0, Energy = 0 };
            st.Tick(0, 1, false);
            Assert.AreEqual(100 - 13f, st.Hp, .01f);
        }

        [Test]
        public void AnUong_CongChiSo()
        {
            var st = new PlayerStats { Water = 50, Energy = 50, Hp = 95 };
            st.Consume(DB.Get("melon"));
            Assert.AreEqual(80, st.Water); Assert.AreEqual(68, st.Energy); Assert.AreEqual(99, st.Hp);
            st.Consume(DB.Get("hotpot"));
            Assert.AreEqual(100, st.Hp, "khong vuot 100");
        }

        // ------------------------------------------------------------ qua ngay
        [Test]
        public void QuaNgay_VeLeuKip()
        {
            var st = new PlayerStats { Hp = 60, Water = 50, Energy = 10 };
            var inv = new Inventory(DB); inv.Give("wood", 5);
            int coins = 100;
            var msgs = DayRules.Apply(NewDayReason.Sleep, st, inv, ref coins);
            Assert.AreEqual(100, st.Energy); Assert.AreEqual(90, st.Hp); Assert.AreEqual(35, st.Water);
            Assert.AreEqual(5, inv.Count("wood")); Assert.AreEqual(100, coins);
            Assert.IsEmpty(msgs);
        }

        [Test]
        public void QuaNgay_23hChuaVeLeu_MatHetDo()
        {
            var st = new PlayerStats { Hp = 30, Water = 10, Energy = 5, Calm = 40 };
            var inv = new Inventory(DB); inv.Give("wood", 5); inv.Give("berry", 3);
            int coins = 100;
            var msgs = DayRules.Apply(NewDayReason.Late, st, inv, ref coins);
            Assert.AreEqual(0, inv.TotalItems);
            Assert.AreEqual(60, st.Energy); Assert.AreEqual(50, st.Hp); Assert.AreEqual(40, st.Water);
            Assert.AreEqual(25, st.Calm, "binh yen -15");
            Assert.AreEqual(100, coins);
            Assert.That(msgs, Has.Some.Contains("8 món"));
        }

        [Test]
        public void QuaNgay_NgatVihetMau_Mat10PhanTramXu()
        {
            var st = new PlayerStats { Hp = 0 };
            var inv = new Inventory(DB); inv.Give("wood", 5);
            int coins = 2500;
            DayRules.Apply(NewDayReason.Fainted, st, inv, ref coins);
            Assert.AreEqual(2400, coins, "toi da 100 xu");
            Assert.AreEqual(5, inv.Count("wood"), "khong mat do");
            Assert.AreEqual(50, st.Hp);
        }

        [Test]
        public void DongHo_7hDen23h()
        {
            var c = new DayClock();
            Assert.AreEqual("14:00", c.TimeLabel);
            c.Advance(9 * 60);
            Assert.IsTrue(c.PastDayEnd);
            c.StartNewDay();
            Assert.AreEqual(2, c.Day);
            Assert.AreEqual("07:00", c.TimeLabel);
        }

        // ------------------------------------------------------------ dong tac
        [Test]
        public void DongTac_AnUong_CoDuKhungHinh()
        {
            var r = new float[CharacterActions.Keys.Length];
            foreach (var act in new[] { "eat", "drink", "axe", "pick", "hoe", "shovel", "can", "seed", "rod", "net", "sit", "build" })
            {
                Assert.IsTrue(CharacterActions.Has(act), act);
                CharacterActions.Sample(act, .5f, r);
            }
            CharacterActions.Sample("drink", .6f, r);
            Assert.AreEqual(-2.25f, r[System.Array.IndexOf(CharacterActions.Keys, "shR")], .1f, "tay phai dua chai len mieng");
            Assert.AreEqual(1.25f, CharacterActions.DurationOf("drink"));
        }
    }
}
