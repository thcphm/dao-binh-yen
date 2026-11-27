// Giai doan 5: ten nhan vat, tom tat o luu, cai dat.
using NUnit.Framework;

namespace SereneIsle.Tests
{
    public class Phase5LogicTests
    {
        [Test]
        public void TenNhanVat()
        {
            Assert.AreEqual("Minh", MainMenu.CleanName("  Minh "));
            Assert.AreEqual("ab", MainMenu.CleanName("<a&b>\""));
            Assert.AreEqual("", MainMenu.CleanName(null));
            Assert.AreEqual(16, MainMenu.CleanName(new string('x', 30)).Length);
        }

        [Test]
        public void TomTatOLuu()
        {
            Assert.IsFalse(SlotInfo.From(2, null).exists);
            var d = new SaveData { day = 30, coins = 1234, playerName = "Lan", playSeconds = 3720, savedAt = "2026-11-21 10:00",
                                   outfit = CharacterOutfit.Female() };
            var i = SlotInfo.From(1, d);
            Assert.IsTrue(i.exists);
            Assert.AreEqual("Lan", i.name);
            StringAssert.Contains("Mùng 2 tháng 2", i.dateLabel, "ngay 30 = mung 2 thang 2");
            Assert.AreEqual("1 giờ 2 phút", i.playTime);
            Assert.AreEqual("Rogue", i.model);
            Assert.AreEqual("Bạn", SlotInfo.From(0, new SaveData()).name);
        }

        [Test]
        public void CaiDatMacDinh()
        {
            var s = new GameSettings();
            Assert.AreEqual(1, s.viewDistance);
            Assert.IsTrue(s.shadows && s.autosave);
            var json = UnityEngine.JsonUtility.ToJson(s);
            var back = UnityEngine.JsonUtility.FromJson<GameSettings>(json);
            Assert.AreEqual(s.music, back.music);
            Assert.AreEqual(3, GameSettings.ViewNames.Length);
        }
    }
}
