// Giai doan 6a: lich thoi tiet (co dinh theo seed + ngay), bao mua Thu, ti le theo mua.
using System.Linq;
using NUnit.Framework;

namespace SereneIsle.Tests
{
    public class WeatherLogicTests
    {
        [Test]
        public void CoDinhTheoSeed()
        {
            for (int day = 1; day < 300; day++)
                Assert.AreEqual(WeatherSystem.Roll(424242, day), WeatherSystem.Roll(424242, day), "cung seed cung ngay -> cung thoi tiet");
            int diff = Enumerable.Range(1, 200).Count(d => WeatherSystem.Roll(1, d) != WeatherSystem.Roll(2, d));
            Assert.Greater(diff, 40, "dao khac thoi tiet khac");
        }

        [Test]
        public void BaoMuaThu()
        {
            foreach (int seed in new[] { 1, 7, 424242, 999 })
                for (int year = 1; year <= 3; year++)
                {
                    int start = (year - 1) * LunarCalendar.DaysPerYear + 1;
                    var storms = Enumerable.Range(start, LunarCalendar.DaysPerYear).Where(d => WeatherSystem.IsStormDay(seed, d)).ToList();
                    Assert.That(storms.Count, Is.InRange(1, 2), $"seed {seed} nam {year}: 1-2 con bao");
                    foreach (var d in storms)
                    {
                        Assert.AreEqual(Season.Thu, LunarCalendar.Date(d).Season, "bao chi mua Thu");
                        Assert.AreEqual(WeatherKind.Storm, WeatherSystem.Roll(seed, d));
                        Assert.AreEqual(WeatherKind.Rain, WeatherSystem.Roll(seed, d + 1), "mua tan sau bao");
                        Assert.AreEqual(WeatherKind.Cloud, WeatherSystem.Roll(seed, d - 1), "may keo den truoc bao");
                    }
                }
        }

        [Test]
        public void TiLeTheoMua()
        {
            int Rain(Season s)
            {
                int n = 0, total = 0;
                for (int d = 1; d < LunarCalendar.DaysPerYear * 8; d++)
                {
                    if (LunarCalendar.Date(d).Season != s) continue;
                    total++;
                    var w = WeatherSystem.Roll(31337, d);
                    if (w == WeatherKind.Rain || w == WeatherKind.Storm) n++;
                }
                return n * 100 / total;
            }
            Assert.Greater(Rain(Season.Thu), Rain(Season.Ha), "Thu mua nhieu hon Ha");
            Assert.Greater(Rain(Season.Xuan), Rain(Season.Dong), "Xuan mua phun nhieu hon Dong kho");
            foreach (Season s in System.Enum.GetValues(typeof(Season)))
                Assert.AreEqual(1f, WeatherSystem.Odds(s).Sum(), .001f);
        }
    }
}
