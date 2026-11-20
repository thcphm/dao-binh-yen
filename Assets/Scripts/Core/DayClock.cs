// Dong ho trong game (ban web: G.time tinh bang phut, DAY_START 7:00, DAY_END 23:00, 2 phut game / giay).
using UnityEngine;

namespace SereneIsle
{
    public sealed class DayClock
    {
        public const float DayStart = 7 * 60;
        public const float DayEnd = 23 * 60;
        public const float MinutesPerSecond = 2;
        /// <summary>Ban web: game moi bat dau luc 14:00 (G.time = 840).</summary>
        public const float NewGameTime = 14 * 60;

        public int Day = 1;
        public float Minutes = NewGameTime;

        public float Hour => Minutes / 60f;
        public bool PastDayEnd => Minutes >= DayEnd;
        public bool IsNight { get { float h = Hour; return h >= 20 || h < 5.5f; } }

        public string TimeLabel
        {
            get
            {
                int m = Mathf.FloorToInt(Minutes);
                return $"{(m / 60) % 24:00}:{m % 60:00}";
            }
        }

        public void Advance(float gameMinutes) { Minutes += gameMinutes; }

        public void StartNewDay() { Day++; Minutes = DayStart; }
    }
}
