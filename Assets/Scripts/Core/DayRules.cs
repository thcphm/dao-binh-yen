// Luat sang ngay moi (ban web: newDay, dong 1473-1478).
using System;
using System.Collections.Generic;

namespace SereneIsle
{
    public enum NewDayReason
    {
        Sleep,   // ve leu kip truoc 23h
        Late,    // 23h chua ve leu: ngat, mat het do trong tui (dung cu van con)
        Fainted, // het mau: ngat, mat 10% xu tien thuoc (toi da 100)
    }

    public static class DayRules
    {
        public static string Subtitle(NewDayReason r) => r switch
        {
            NewDayReason.Fainted => "Bạn kiệt sức ngất đi… Ông Bảy tìm thấy và đưa bạn về nghỉ.",
            NewDayReason.Late => "23 giờ mà chưa về lều, bạn ngất giữa đường… Ông Bảy dìu bạn về, nhưng đồ trong túi rơi mất hết.",
            _ => "Một ngày mới trên đảo",
        };

        /// <summary>Ap dung luat len chi so / tui / xu. Tra ve cac dong thong bao sau khi tinh day.</summary>
        public static List<string> Apply(NewDayReason reason, PlayerStats st, Inventory inv, ref int coins, float homeBonus = 0)
        {
            var msgs = new List<string>();
            bool passOut = reason != NewDayReason.Sleep;
            st.Calm = Math.Max(0, Math.Min(100, st.Calm + (passOut ? -15 : -2)));

            if (reason == NewDayReason.Late)
            {
                int n = inv.Clear();
                if (n > 0) msgs.Add($"Mất hết {n} món đồ trong túi. Dụng cụ vẫn còn.");
            }

            if (passOut)
            {
                int lost = reason == NewDayReason.Fainted ? Math.Min(100, (int)Math.Floor(coins * .1)) : 0;
                coins -= lost;
                st.Energy = 60;
                st.Hp = Math.Max(st.Hp, 50);
                st.Water = Math.Max(st.Water, 40);
                if (lost > 0) msgs.Add($"Mất {lost} xu tiền thuốc.");
            }
            else
            {
                st.Energy = 100 + homeBonus;
                st.Hp = Math.Min(100, st.Hp + 30);
                st.Water = Math.Max(0, st.Water - 15);
            }
            st.ClearRecover();
            return msgs;
        }
    }
}
