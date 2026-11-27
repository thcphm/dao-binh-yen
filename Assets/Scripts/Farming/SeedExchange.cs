// Doi giong voi Co Sau Vuon (Docs/ThietKeGameplay.md muc 4.8): mang 3 hat giu giong (bat ky loai) doi 2 hat giong hiem
// cua thang. Moi thang doi 1 lan. Nut "Doi giống" them vao hoi thoai qua Village.Talking.
using System.Collections.Generic;
using UnityEngine;

namespace SereneIsle
{
    public class SeedExchange : MonoBehaviour
    {
        public static SeedExchange Instance { get; private set; }
        public const string Npc = "sau";

        public int LastMonth { get; private set; } = -1;

        GameSession S => GameSession.Instance;
        int Month => Mail.MonthOf(S.Clock.Day);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { Instance = null; }

        void Awake() { Instance = this; }
        void OnEnable() { Village.Talking += OnTalk; }
        void OnDisable() { Village.Talking -= OnTalk; }
        void OnDestroy() { if (Instance == this) Instance = null; }

        public string RareSeed => FarmCare.RareSeed(Month, CropDatabase.Instance.crops);

        /// <summary>Tong hat giu giong trong tui.</summary>
        public int SavedSeeds()
        {
            int n = 0;
            foreach (var id in S.Inventory.Order) if (id.StartsWith(FarmCare.SavedSeedPrefix)) n += S.Inventory.Count(id);
            return n;
        }

        public bool CanSwap => S && LastMonth < Month && SavedSeeds() >= FarmCare.SwapCost;

        void OnTalk(Village.TalkContext ctx)
        {
            if (ctx.npc.def.id != Npc || !S) return;
            if (LastMonth >= Month) return;
            string rare = S.Items.NameOf(RareSeed);
            if (SavedSeeds() >= FarmCare.SwapCost)
            {
                ctx.line = $"Con có hạt giữ giống hả? Đưa cô {FarmCare.SwapCost} hạt, cô đổi {FarmCare.SwapGive} gói {rare.ToLowerInvariant()}, giống hiếm tháng này đó.";
                ctx.buttons.Add(($"Đổi giống ({FarmCare.SwapCost} hạt giữ giống)", () => Swap()));
            }
        }

        /// <summary>Lay 3 hat giu giong (uu tien loai nhieu nhat), dua 2 hat giong hiem. Tra ve false neu khong doi duoc.</summary>
        public bool Swap()
        {
            var s = S;
            if (!CanSwap) return false;
            var own = new List<string>();
            foreach (var id in s.Inventory.Order) if (id.StartsWith(FarmCare.SavedSeedPrefix)) own.Add(id);
            own.Sort((a, b) => s.Inventory.Count(b).CompareTo(s.Inventory.Count(a)));
            int need = FarmCare.SwapCost;
            foreach (var id in own)
            {
                int take = Mathf.Min(need, s.Inventory.Count(id));
                s.Inventory.Take(id, take);
                need -= take;
                if (need == 0) break;
            }
            string rare = RareSeed;
            s.Give(rare, FarmCare.SwapGive, true);
            LastMonth = Month;
            s.Say($"Cô Sáu: \"Giống nhà con khỏe lắm, cô gieo thử coi. Còn đây {FarmCare.SwapGive} gói {s.Items.NameOf(rare).ToLowerInvariant()} cho con.\"");
            return true;
        }

        public void Restore(int lastMonth) { LastMonth = lastMonth; }
    }
}
