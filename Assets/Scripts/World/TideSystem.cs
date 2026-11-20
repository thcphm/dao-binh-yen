// Con nuoc trong game (Docs/ThietKeGameplay.md muc 3):
//  - cap nhat muc nuoc bien (IslandData.WaterLevel) va mat nuoc theo dong ho + tuan trang;
//  - nuoc rong: rai do bai trieu (so, ngheu, oc, sao bien, rong bien) quanh nguoi choi o dai dat vua lo ra;
//  - nuoc len ma nguoi choi bi ket (nuoc sau qua muc loi duoc): Bac Chin cheo ghe ra don, phat nang (muc 8).
using System.Collections.Generic;
using UnityEngine;

namespace SereneIsle
{
    public class TideSystem : MonoBehaviour
    {
        [Tooltip("Nuoc sau hon muc nay tai cho dung thi bi xem la ket")]
        public float strandDepth = .6f;
        [Tooltip("So dong do bai trieu toi da moi lan nuoc rong")]
        public int forageCount = 18;
        public float forageRadius = 45f;

        public float Level { get; private set; } = IslandData.SEA;
        public bool Rising { get; private set; }

        static readonly (string id, float w)[] Forage =
        {
            ("so_huyet", 3), ("ngheu", 4), ("oc_buou", 4), ("rong_bien", 3), ("sao_bien", 1), ("f_crab", 1.5f),
        };

        readonly List<ItemPile> _forage = new List<ItemPile>();
        bool _wasLow;
        bool _rescuing;

        GameSession S => GameSession.Instance;

        void Update()
        {
            var s = S;
            var terrain = IslandTerrain.Instance;
            if (!s || !terrain || !terrain.IsReady) return;
            var d = terrain.Data;

            double abs = TideClock.AbsHours(s.Clock.Day, s.Clock.Hour);
            Level = IslandData.SEA + TideClock.Offset(abs);
            Rising = TideClock.Rising(abs);
            d.WaterLevel = Level;
            if (terrain.WaterPlane)
            {
                var wp = terrain.WaterPlane.localPosition;
                terrain.WaterPlane.localPosition = new Vector3(wp.x, Level, wp.z);
            }

            // nuoc rong: rai do bai trieu 1 lan moi chu ky
            bool low = TideClock.Offset(abs) < -TideClock.NeapAmp * .8f;
            if (low && !_wasLow) SpawnForage(d);
            _wasLow = low;
            CleanSubmerged(d);

            if (s.Active && s.Player && !_rescuing && d.WaterDepth(s.Player.transform.position.x, s.Player.transform.position.z) > strandDepth)
                StartCoroutine(Rescue());
        }

        /// <summary>Rai do o dai dat nam giua muc nuoc hien tai va muc trung binh (vua lo ra khi nuoc rong).</summary>
        void SpawnForage(IslandData d)
        {
            var s = S;
            if (!s || !s.Player) return;
            Vector3 c = s.Player.transform.position;
            float total = 0;
            foreach (var f in Forage) total += f.w;
            int placed = 0;
            for (int t = 0; t < 4000 && placed < forageCount; t++)
            {
                int i = Mathf.RoundToInt(c.x + Random.Range(-forageRadius, forageRadius)), j = Mathf.RoundToInt(c.z + Random.Range(-forageRadius, forageRadius));
                if (!d.InBounds(i, j)) continue;
                int k = d.Idx(i, j);
                float h = d.HT[k];
                if (d.Ocean[k] == 0 || d.Pier[k] != 0 || h < Level + .06f || h > IslandData.SEA - .02f) continue;
                float r = Random.value * total;
                string id = Forage[0].id;
                foreach (var f in Forage) { r -= f.w; if (r <= 0) { id = f.id; break; } }
                var pile = ItemPile.Spawn(id, 1, new Vector3(i, h, j));
                if (!_forage.Contains(pile)) _forage.Add(pile);
                placed++;
            }
        }

        /// <summary>Do bai trieu bi nuoc len ngap thi troi mat.</summary>
        void CleanSubmerged(IslandData d)
        {
            for (int n = _forage.Count - 1; n >= 0; n--)
            {
                var p = _forage[n];
                if (!p) { _forage.RemoveAt(n); continue; }
                var pos = p.transform.position;
                if (d.WaterDepth(pos.x, pos.z) > .15f) { Destroy(p.gameObject); _forage.RemoveAt(n); }
            }
        }

        System.Collections.IEnumerator Rescue()
        {
            _rescuing = true;
            var s = S;
            var d = IslandTerrain.Instance.Data;
            yield return s.Cutscene(
                "Nước lên ngập lối về… Bác Chín Đò chèo ghe ra đón bạn vào cầu tàu.",
                () =>
                {
                    var msgs = TideRules.Rescue(s.Inventory, s.Items, s.Coins, out int left);
                    s.Coins = left;
                    var p = d.ArrivalPoint(out float f);
                    s.Player.PlaceAt(p, f);
                    return msgs;
                });
            _rescuing = false;
        }
    }

    /// <summary>Hinh phat khi bi ket nuoc len (muc 8): mat 30% tien, moi thuc an va hat giong trong tui bi uot hong.</summary>
    public static class TideRules
    {
        public const float CoinLoss = .3f;

        public static List<string> Rescue(Inventory inv, IItemCatalog items, int coins, out int coinsLeft)
        {
            var msgs = new List<string>();
            int lost = Mathf.FloorToInt(coins * CoinLoss);
            coinsLeft = coins - lost;
            if (lost > 0) msgs.Add($"Trả Bác Chín {lost} xu tiền ghe.");
            int ruined = 0;
            foreach (var id in new List<string>(inv.Order))
            {
                var it = items.Get(id);
                if (!it || !(it.IsFood || it.category == ItemCategory.Seed)) continue;
                int n = inv.Count(id);
                if (inv.Take(id, n)) ruined += n;
            }
            if (ruined > 0) msgs.Add($"{ruined} món thức ăn và hạt giống bị ướt hỏng.");
            msgs.Add("Lần sau nhớ coi giờ nước.");
            return msgs;
        }
    }
}
