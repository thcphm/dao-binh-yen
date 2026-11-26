// Giai doan 6d: lich le hoi (5 le), le sap toi, cho cau ngu, du lieu thu nha (qua co that trong kho do).
using NUnit.Framework;

namespace SereneIsle.Tests
{
    public class FestivalLogicTests
    {
        static ItemDatabase _items;
        static ItemDatabase Items => _items ? _items : (_items = ItemDatabase.CreateInMemory());

        static int Day(int month, int dom, int year = 1) => (year - 1) * LunarCalendar.DaysPerYear + (month - 1) * LunarCalendar.DaysPerMonth + dom;

        [Test]
        public void LichLeHoi()
        {
            Assert.AreEqual(Festivals.Tet, LunarCalendar.Festival(LunarCalendar.Date(Day(1, 1))));
            Assert.AreEqual(Festivals.RamGieng, LunarCalendar.Festival(LunarCalendar.Date(Day(1, 15))));
            Assert.AreEqual(Festivals.ComMoi, LunarCalendar.Festival(LunarCalendar.Date(Day(3, 10))));
            Assert.AreEqual(Festivals.CauNgu, LunarCalendar.Festival(LunarCalendar.Date(Day(3, 15))));
            Assert.AreEqual(Festivals.TrungThu, LunarCalendar.Festival(LunarCalendar.Date(Day(6, 15))));
            Assert.AreEqual(Festivals.Tet, LunarCalendar.Festival(LunarCalendar.Date(Day(1, 1, 2))), "Tet nam 2");
            int n = 0;
            for (int d = 1; d <= LunarCalendar.DaysPerYear; d++) if (LunarCalendar.Festival(LunarCalendar.Date(d)) != null) n++;
            Assert.AreEqual(5, n, "5 le moi nam");

            Assert.AreEqual(Festivals.RamGieng, LunarCalendar.NextFestival(2, out int k));
            Assert.AreEqual(13, k);
            Assert.AreEqual(Festivals.Tet, LunarCalendar.NextFestival(Day(1, 1), out k));
            Assert.AreEqual(0, k);
            Assert.AreEqual(Festivals.Tet, LunarCalendar.NextFestival(Day(6, 16), out k), "sau trung thu la Tet nam sau");
            Assert.AreEqual(Day(1, 1, 2) - Day(6, 16), k);
        }

        [Test]
        public void ChoCauNgu()
        {
            int d = Day(3, 15);
            Assert.AreNotEqual(7, LunarCalendar.Date(d).DayOfWeek, "ram thang 3 khong roi vao thu 7");
            Assert.IsTrue(Market.IsMarketDay(d), "cau ngu: hop cho bat ke thu may");
            Assert.IsTrue(Market.IsBig(d));
            Assert.IsFalse(Market.IsMarketDay(d + 1));
        }

        [Test]
        public void ThuNha_DuLieu()
        {
            Assert.AreEqual(8, Mail.Story.Length, "8 thu ke chuyen (1 nam)");
            foreach (var l in Mail.Story)
            {
                Assert.IsNotEmpty(l.text);
                Assert.IsTrue(l.coins > 0 || (l.gifts != null && l.gifts.Length > 0), "thu nao cung co qua");
            }
            foreach (var l in Mail.Story) if (l.gifts != null) foreach (var (id, n) in l.gifts) Assert.IsTrue(Items.Get(id), "qua khong co trong kho do: " + id);
            foreach (var l in Mail.Everyday) if (l.gifts != null) foreach (var (id, n) in l.gifts) Assert.IsTrue(Items.Get(id), "qua khong co trong kho do: " + id);
            Assert.AreSame(Mail.Story[0], Mail.LetterAt(0));
            Assert.AreSame(Mail.Everyday[1], Mail.LetterAt(Mail.Story.Length + 1));
            Assert.AreEqual(0, Mail.MonthOf(1));
            Assert.AreEqual(0, Mail.MonthOf(28));
            Assert.AreEqual(1, Mail.MonthOf(29));
            Assert.IsTrue(Items.Get("hoa_dang"));
            Assert.IsTrue(Items.Get("banh_trung_thu").IsFood);
        }
    }
}
