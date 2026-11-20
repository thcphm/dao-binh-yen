// Lich am cua dao (Docs/ThietKeGameplay.md muc 2): 7 ngay/tuan, 28 ngay/thang, 2 thang/mua, 4 mua/nam = 224 ngay.
// Moi ngay game la 1 ngay am. Mung 1 trang non, ram (ngay 15) trang tron. Ngay 1 cua van choi = mung 1 thang 1, Xuan, nam 1.
// Thuan C#, khong phu thuoc Unity -> test EditMode duoc.
using System;

namespace SereneIsle
{
    public enum Season { Xuan = 0, Ha = 1, Thu = 2, Dong = 3 }

    [Flags]
    public enum SeasonMask { None = 0, Xuan = 1, Ha = 2, Thu = 4, Dong = 8, All = 15 }

    public readonly struct LunarDate
    {
        public readonly int Day;        // ngay tuyet doi, tu 1
        public readonly int Year;       // tu 1
        public readonly int Month;      // 1..8
        public readonly int DayOfMonth; // 1..28
        public readonly int DayOfWeek;  // 1..7 (ngay 7 = chu phien)
        public readonly int Week;       // 1..4 trong thang

        public LunarDate(int day)
        {
            Day = Math.Max(1, day);
            int d0 = Day - 1;
            Year = d0 / LunarCalendar.DaysPerYear + 1;
            int dy = d0 % LunarCalendar.DaysPerYear;
            Month = dy / LunarCalendar.DaysPerMonth + 1;
            DayOfMonth = dy % LunarCalendar.DaysPerMonth + 1;
            DayOfWeek = d0 % 7 + 1;
            Week = (DayOfMonth - 1) / 7 + 1;
        }

        public Season Season => (Season)((Month - 1) / LunarCalendar.MonthsPerSeason);
        /// <summary>Thang thu may trong mua (1 hoac 2).</summary>
        public int MonthOfSeason => (Month - 1) % LunarCalendar.MonthsPerSeason + 1;
        public bool IsMarketDay => DayOfWeek == 7;
        public bool IsFullMoon => DayOfMonth == 15;
        public bool IsNewMoon => DayOfMonth == 1;

        public string DayName => DayOfMonth <= 10 ? $"Mùng {DayOfMonth}" : DayOfMonth == 15 ? "Rằm" : $"Ngày {DayOfMonth}";
        /// <summary>Vd: "Mùng 3 tháng 1".</summary>
        public string ShortLabel => $"{DayName} tháng {Month}";
        /// <summary>Vd: "Mùng 3 tháng 1 · Xuân · Năm 1".</summary>
        public string LongLabel => $"{ShortLabel} · {LunarCalendar.SeasonName(Season)} · Năm {Year}";
        public override string ToString() => LongLabel;
    }

    public static class LunarCalendar
    {
        public const int DaysPerWeek = 7;
        public const int DaysPerMonth = 28;
        public const int MonthsPerSeason = 2;
        public const int SeasonsPerYear = 4;
        public const int MonthsPerYear = MonthsPerSeason * SeasonsPerYear;   // 8
        public const int DaysPerSeason = DaysPerMonth * MonthsPerSeason;     // 56
        public const int DaysPerYear = DaysPerSeason * SeasonsPerYear;       // 224
        public const int FullMoonDay = 15;

        public static LunarDate Date(int day) => new LunarDate(day);

        public static string SeasonName(Season s) => s switch
        {
            Season.Xuan => "Xuân", Season.Ha => "Hạ", Season.Thu => "Thu", _ => "Đông",
        };

        public static SeasonMask Mask(Season s) => (SeasonMask)(1 << (int)s);
        public static bool In(SeasonMask m, Season s) => (m & Mask(s)) != 0;

        /// <summary>Tuoi trang 0..1 (0 = trang non mung 1, .5 = trang tron ngay ram). hour: gio trong ngay.</summary>
        public static double MoonAge(int day, double hour = 12)
        {
            var d = new LunarDate(day);
            return ((d.DayOfMonth - 1) + hour / 24.0) / DaysPerMonth;
        }

        /// <summary>Do sang mat trang 0..1.</summary>
        public static double MoonIllumination(double age) => .5 - .5 * Math.Cos(2 * Math.PI * age);

        public static string MoonPhaseName(double age)
        {
            double a = age - Math.Floor(age);
            if (a < .035 || a > .965) return "Trăng non";
            if (a < .215) return "Trăng lưỡi liềm";
            if (a < .285) return "Trăng thượng huyền";
            if (a < .465) return "Trăng khuyết đầu tháng";
            if (a < .535) return "Trăng tròn";
            if (a < .715) return "Trăng khuyết cuối tháng";
            if (a < .785) return "Trăng hạ huyền";
            return "Trăng lưỡi liềm cuối tháng";
        }

        /// <summary>Le hoi theo lich am (muc 7.5). null neu khong co.</summary>
        public static string Festival(LunarDate d)
        {
            if (d.Month == 1 && d.DayOfMonth == 1) return "Tết";
            if (d.Month == 1 && d.DayOfMonth == FullMoonDay) return "Rằm tháng giêng";
            if (d.Month == 3 && d.DayOfMonth == FullMoonDay) return "Lễ cầu ngư";
            if (d.Month == 6 && d.DayOfMonth == FullMoonDay) return "Trung thu";
            return null;
        }

        /// <summary>Phien cho lon: phien thu 7 roi vao (hoac gan nhat voi) mung 1 hay ram.</summary>
        public static bool IsBigMarket(LunarDate d)
        {
            if (!d.IsMarketDay) return false;
            int a = Math.Abs(d.DayOfMonth - FullMoonDay), b = Math.Min(d.DayOfMonth - 1, DaysPerMonth + 1 - d.DayOfMonth);
            return Math.Min(a, b) <= 3;
        }
    }
}
