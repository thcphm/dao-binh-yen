// Con nuoc (Docs/ThietKeGameplay.md muc 3): ban nhat trieu, chu ky 12h25', bien do theo tuan trang.
// Muc nuoc = SEA + A(trang) * cos(2pi * (t - 6h) / 12.42h). Gio nuoc lon tu lui ~50 phut moi ngay.
// Nuoc rong (bien do lon) quanh mung 1 va ram, nuoc kem quanh mung 8 va 22.
using System;

namespace SereneIsle
{
    public static class TideClock
    {
        public const double Period = 12.42;          // gio
        public const double FirstHigh = 6.0;         // nuoc lon dau tien: 6:00 ngay 1
        public const float SpringAmp = .45f;         // bien do nuoc rong
        public const float NeapAmp = .15f;           // bien do nuoc kem

        /// <summary>Gio tuyet doi tinh tu 0:00 ngay 1.</summary>
        public static double AbsHours(int day, double hour) => (Math.Max(1, day) - 1) * 24.0 + hour;

        /// <summary>Bien do (don vi do cao) theo tuoi trang: lon nhat luc trang non / tron.</summary>
        public static float Amplitude(double absHours)
        {
            double age = absHours / 24.0 / LunarCalendar.DaysPerMonth;
            double spring = Math.Abs(Math.Cos(2 * Math.PI * age));
            return (float)(NeapAmp + (SpringAmp - NeapAmp) * spring);
        }

        /// <summary>Do lech so voi muc bien trung binh (SEA).</summary>
        public static float Offset(double absHours)
            => Amplitude(absHours) * (float)Math.Cos(2 * Math.PI * (absHours - FirstHigh) / Period);

        public static float Offset(int day, double hour) => Offset(AbsHours(day, hour));

        public static float Level(int day, double hour) => IslandData.SEA + Offset(day, hour);

        /// <summary>Nuoc dang len?</summary>
        public static bool Rising(double absHours) => Math.Sin(2 * Math.PI * (absHours - FirstHigh) / Period) < 0;

        /// <summary>Thoi diem (gio tuyet doi) nuoc lon / rong ke tiep sau absHours.</summary>
        public static double NextHigh(double absHours) => NextPhase(absHours, 0);
        public static double NextLow(double absHours) => NextPhase(absHours, .5);

        static double NextPhase(double absHours, double frac)
        {
            double c = (absHours - FirstHigh) / Period - frac;
            double n = Math.Floor(c) + 1;
            return FirstHigh + (n + frac) * Period;
        }

        /// <summary>Muc nuoc cao / thap nhat trong 1 ngay (de biet o nao bi ngap, o nao lo ra).</summary>
        public static void DayRange(int day, out float low, out float high)
        {
            low = float.MaxValue; high = float.MinValue;
            for (int m = 0; m < 24 * 60; m += 10)
            {
                float l = Level(day, m / 60.0);
                if (l < low) low = l;
                if (l > high) high = l;
            }
        }

        /// <summary>Muc nuoc cao nhat co the (nuoc rong) - o thap hon muc nay ma khong co bo bao thi bi man tran.</summary>
        public static float MaxLevel => IslandData.SEA + SpringAmp;

        /// <summary>"15:40" tu gio tuyet doi.</summary>
        public static string ClockLabel(double absHours)
        {
            double h = absHours % 24;
            int m = (int)Math.Round(h * 60) % (24 * 60);
            return $"{m / 60:00}:{m % 60:00}";
        }
    }
}
