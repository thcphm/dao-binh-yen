// Luat lan bien (Docs/ThietKeGameplay.md muc 7.1 + muc 8). Thuan C# de test EditMode.
// Bac nghe lan (O Man): 1 kinh lan (dam nong), 2 chan vit (ran gan bo), 3 den lan (moi noi, lan dem).
// Do sau tinh bang don vi o cua dao (bien quanh dao sau toi ~1.6).
using System;
using System.Collections.Generic;

namespace SereneIsle
{
    public static class DiveRules
    {
        public const float SwimEnterDepth = .32f;    // nuoc sau hon muc nay thi boi (neu biet lan)
        public const float CoinLoss = .2f;           // het hoi: mat 20% tien + toan bo tui

        public static bool CanSwim(int level) => level >= 1;

        /// <summary>Do sau toi da lan xuong duoc.</summary>
        public static float MaxDepth(int level) => level >= 3 ? 99f : level == 2 ? 1.4f : level == 1 ? 1.0f : 0f;

        /// <summary>Thoi gian nin tho (giay that). Mua Dong nuoc lanh: giam 30%.</summary>
        public static float BreathSeconds(int level, Season s)
        {
            float b = level >= 3 ? 45 : level == 2 ? 30 : level == 1 ? 15 : 0;
            return s == Season.Dong ? b * .7f : b;
        }

        public static float SwimSpeed(int level, bool underwater)
            => underwater ? (level >= 2 ? 2.0f : 1.4f) : (level >= 2 ? 2.6f : 1.8f);

        /// <summary>Lan dem chi khi bac 3 (co den).</summary>
        public static bool CanDiveAtNight(int level) => level >= 3;

        /// <summary>Do o day bien theo do sau: nong (rong, oc, san ho) / sau (hai sam, bao ngu, trai ngoc).</summary>
        public static string PickLoot(float depth, Random rng)
        {
            (string id, float w)[] table = depth < .8f
                ? new[] { ("rong_sun", 4f), ("oc_huong", 3f), ("manh_san_ho", 2.5f), ("nhum", 2f) }
                : new[] { ("hai_sam", 3f), ("bao_ngu", 2f), ("nhum", 2f), ("trai_ngoc", 1.2f), ("manh_san_ho", 1f) };
            float total = 0;
            foreach (var t in table) total += t.w;
            double r = rng.NextDouble() * total;
            foreach (var t in table) { r -= t.w; if (r <= 0) return t.id; }
            return table[0].id;
        }

        /// <summary>Het hoi: mat toan bo do trong tui + 20% tien. Tra ve cac dong thong bao.</summary>
        public static List<string> Drown(Inventory inv, ref int coins)
        {
            var msgs = new List<string>();
            int n = inv.Clear();
            int lost = (int)Math.Floor(coins * CoinLoss);
            coins -= lost;
            if (n > 0) msgs.Add($"{n} món đồ trong túi chìm xuống đáy biển.");
            if (lost > 0) msgs.Add($"Mất {lost} xu tiền thuốc.");
            msgs.Add("Lặn thì canh hơi thở, nhấp nháy là phải nổi lên liền nghe.");
            return msgs;
        }
    }
}
