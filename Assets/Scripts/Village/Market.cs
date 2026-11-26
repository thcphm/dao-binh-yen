// Cho phien (Docs/ThietKeGameplay.md muc 6): hop ngay thu 7 moi tuan, 7:00-17:00 o nha long cho giua lang.
// Nguoi choi bay sap (6 o, them o khi uy tin cao), dat gia 60-160% gia goc. Khach co so thich rieng; gia cao qua thi
// khach tra gia (nguoi choi chon bot hay giu gia). Ban gia hop ly thi uy tin sap tang. Phien lon (gan mung 1 / ram) dong khach hon.
// Phai dung gan sap (trong sap) thi moi ban duoc. Cho tan: do chua ban cat lai vao tui.
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SereneIsle
{
    [Serializable]
    public class StallSlot { public string id; public int n; public int pct = 100; }

    public sealed class CustomerType
    {
        public string id, name;
        public float crop, fish, bug, food, other; // muc ua thich 0..1
        public float wealth;                      // tra them
        public string[] buyLines, haggleLines, leaveLines;
        public float Pref(ItemDefinition d) => d.category switch
        {
            ItemCategory.Crop => crop, ItemCategory.Fish => fish, ItemCategory.Bug => bug, ItemCategory.Food => food, _ => other,
        };
    }

    public class Market : MonoBehaviour
    {
        public static Market Instance { get; private set; }

        public const float OpenHour = 7, CloseHour = 17;
        public const int MinPct = 60, MaxPct = 160, BaseSlots = 6;
        public const float AttendRange = 7f;

        public readonly List<StallSlot> Stall = new List<StallSlot>();
        public float Reputation { get; set; }
        public int Slots => BaseSlots + (Reputation >= 30 ? 2 : 0) + (Reputation >= 80 ? 2 : 0);
        public int SoldToday { get; private set; }
        public int EarnedToday { get; private set; }
        public int CustomerCount => _customers.Count;
        public event Action Changed;

        public static readonly CustomerType[] Types =
        {
            new CustomerType { id = "ba_cu", name = "Bà cụ", fish = 1, food = .9f, crop = .5f, bug = .1f, other = .3f,
                buyLines = new[] { "Cá tươi quá, bà lấy nghen.", "Đồ con bán ngon dữ." },
                haggleLines = new[] { "Bớt cho bà chút đi con, {0} xu được không?", "Mắc quá con ơi, {0} xu nghen?" },
                leaveLines = new[] { "Thôi, bà đi chỗ khác coi." } },
            new CustomerType { id = "tre", name = "Đám trẻ", bug = 1, crop = .5f, food = .6f, fish = .2f, other = .2f, wealth = -.1f,
                buyLines = new[] { "Con bọ đẹp quá! Em mua!", "Hay quá, mua liền!" },
                haggleLines = new[] { "Em chỉ có {0} xu à, bán không anh chị?" },
                leaveLines = new[] { "Mắc quá, tụi em hổng đủ tiền." } },
            new CustomerType { id = "khach", name = "Khách đất liền", crop = 1, fish = .7f, food = .4f, bug = .4f, other = .5f, wealth = .15f,
                buyLines = new[] { "Đặc sản đảo đây rồi, mua làm quà!", "Hàng đẹp, tôi lấy." },
                haggleLines = new[] { "{0} xu thì tôi lấy liền.", "Bớt chút đi, {0} xu nhé?" },
                leaveLines = new[] { "Để tôi dạo thêm vòng nữa." } },
            new CustomerType { id = "lang", name = "Người làng", crop = .6f, fish = .6f, food = .6f, bug = .3f, other = .6f,
                buyLines = new[] { "Lấy cho cô mấy cái.", "Được, mua ủng hộ con." },
                haggleLines = new[] { "{0} xu được không con?" },
                leaveLines = new[] { "Thôi để bữa khác." } },
        };

        readonly List<MarketCustomer> _customers = new List<MarketCustomer>();
        readonly System.Random _rng = new System.Random();
        float _spawnT = 5;
        int _openDay = -1;
        GameObject _goods;
        MarketCustomer _haggling;

        GameSession S => GameSession.Instance;
        static IslandData D => IslandTerrain.Instance ? IslandTerrain.Instance.Data : null;
        public VillageBuilding Pavilion => Village.Instance ? Village.Instance.Building("cho") : null;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { Instance = null; }

        void Awake() { Instance = this; }
        void OnDestroy() { if (Instance == this) Instance = null; }

        // ------------------------------------------------------------------ lich
        /// <summary>Le cau ngu (ram thang 3): chu phien lon nhat nam, hop bat ke thu may (muc 7.5).</summary>
        public static bool IsCauNgu(int day) { var d = LunarCalendar.Date(day); return d.Month == 3 && d.DayOfMonth == LunarCalendar.FullMoonDay; }
        public static bool IsMarketDay(int day) => LunarCalendar.Date(day).IsMarketDay || IsCauNgu(day);
        public static bool IsBig(int day) => LunarCalendar.IsBigMarket(LunarCalendar.Date(day)) || IsCauNgu(day);
        public bool IsOpen => S && IsMarketDay(S.Clock.Day) && S.Clock.Hour >= OpenHour && S.Clock.Hour < CloseHour && !Storm;
        /// <summary>Bay sap duoc: ngay cho, truoc gio tan, khong bao.</summary>
        public bool CanSetUp => S && IsMarketDay(S.Clock.Day) && S.Clock.Hour < CloseHour && !Storm;
        static bool Storm => WeatherSystem.Instance && WeatherSystem.Instance.IsStorm;

        public bool Attended
        {
            get
            {
                var p = Pavilion;
                return p != null && S && S.Player && Flat(S.Player.transform.position, p.center) < AttendRange;
            }
        }

        // ------------------------------------------------------------------ sap
        public static int PriceOf(ItemDefinition d, int pct) => d ? Mathf.Max(1, Mathf.RoundToInt(d.price * pct / 100f)) : 0;
        public int PriceOf(StallSlot s) => PriceOf(S.Items.Get(s.id), s.pct);

        /// <summary>Bay n mon tu tui len sap (gop vao o cung loai). Tra ve thong bao loi hoac null.</summary>
        public string Put(string id, int n)
        {
            var s = S;
            if (!CanSetUp) return "Hôm nay không họp chợ. Chợ phiên họp ngày thứ 7, từ 7 giờ đến 17 giờ.";
            var d = s.Items.Get(id);
            if (!Shops.CanSell(d)) return "Món này không bày bán được.";
            n = Mathf.Min(n, s.Inventory.Count(id));
            if (n <= 0) return null;
            var slot = Stall.Find(x => x.id == id);
            if (slot == null)
            {
                if (Stall.Count >= Slots) return $"Sạp chỉ có {Slots} ô. Uy tín cao thì được thêm ô.";
                slot = new StallSlot { id = id, n = 0, pct = 100 };
                Stall.Add(slot);
            }
            s.Inventory.Take(id, n);
            slot.n += n;
            Notify();
            return null;
        }

        /// <summary>Cat 1 o sap ve tui.</summary>
        public void TakeBack(int idx)
        {
            if (idx < 0 || idx >= Stall.Count) return;
            var slot = Stall[idx];
            Stall.RemoveAt(idx);
            ReturnToBag(slot);
            Notify();
        }

        public void SetPct(int idx, int pct)
        {
            if (idx < 0 || idx >= Stall.Count) return;
            Stall[idx].pct = Mathf.Clamp(Mathf.RoundToInt(pct / 10f) * 10, MinPct, MaxPct);
            Notify();
        }

        void ReturnToBag(StallSlot slot)
        {
            int got = S.Inventory.Give(slot.id, slot.n);
            if (got < slot.n && S.Player) ItemPile.Spawn(slot.id, slot.n - got, S.Player.transform.position);
        }

        /// <summary>Cho tan (hoac sang ngay moi): do chua ban ve tui.</summary>
        public void CloseStall(string reason)
        {
            int n = 0;
            foreach (var slot in Stall) { n += slot.n; ReturnToBag(slot); }
            Stall.Clear();
            foreach (var c in _customers) if (c) c.Leave();
            if (n > 0 && S) S.Say($"{reason} {n} món chưa bán đã cất lại vào túi.");
            Notify();
        }

        void Notify() { RefreshGoods(); Changed?.Invoke(); }

        // ------------------------------------------------------------------ moi khung
        void Update()
        {
            var s = S;
            if (!s || !s.Running || Pavilion == null) return;
            bool open = IsOpen;
            if (open && _openDay != s.Clock.Day) { _openDay = s.Clock.Day; SoldToday = 0; EarnedToday = 0; }
            if (!CanSetUp && Stall.Count > 0) CloseStall("Chợ tan.");
            if (!_goods && Stall.Count > 0) RefreshGoods(); // tai game truoc khi lang dung xong
            if (_goods) _goods.SetActive(Stall.Count > 0);

            _customers.RemoveAll(c => !c);
            if (!open || !s.Active) return;

            // khach den: phien lon / uy tin cao thi dong hon; sap trong thi it khach ghe
            _spawnT -= Time.deltaTime;
            int max = IsCauNgu(s.Clock.Day) ? 7 : IsBig(s.Clock.Day) ? 5 : 3;
            if (_spawnT <= 0 && _customers.Count < max)
            {
                float interval = (IsCauNgu(s.Clock.Day) ? 5f : IsBig(s.Clock.Day) ? 7f : 11f) * Mathf.Lerp(1f, .6f, Reputation / 100f);
                if (Stall.Count == 0) interval *= 2.5f;
                _spawnT = interval * (.7f + (float)_rng.NextDouble() * .6f);
                SpawnCustomer();
            }
        }

        public MarketCustomer SpawnCustomer(CustomerType type = null)
        {
            var d = D; var p = Pavilion;
            if (d == null || p == null) return null;
            Vector3 from = p.door;
            for (int t = 0; t < 20; t++)
            {
                float a = (float)_rng.NextDouble() * Mathf.PI * 2;
                var q = p.center + new Vector3(Mathf.Sin(a), 0, Mathf.Cos(a)) * 15;
                int i = Mathf.RoundToInt(q.x), j = Mathf.RoundToInt(q.z);
                if (!d.InBounds(i, j)) continue;
                int k = d.Idx(i, j);
                if (d.Level[k] == 0 || d.Solid[k] != 0 || d.WaterDepth(q.x, q.z) > 0) continue;
                from = q; break;
            }
            type ??= Types[_rng.Next(Types.Length)];
            var c = MarketCustomer.Create(type, from, SpotFor(_customers.Count), _rng, transform);
            _customers.Add(c);
            return c;
        }

        /// <summary>Cho dung truoc sap (xep hang ngang truoc quay).</summary>
        Vector3 SpotFor(int i)
        {
            var p = Pavilion;
            var right = new Vector3(Mathf.Cos(p.facing), 0, -Mathf.Sin(p.facing));
            float off = ((i % 5) - 2) * .85f;
            var q = p.door + right * off;
            var d = D;
            q.y = d != null ? d.GroundHeight(q.x, q.z) : q.y;
            return q;
        }

        // ------------------------------------------------------------------ mua ban
        public enum Outcome { Bought, Haggle, Left, NoStaff, Empty }

        /// <summary>Gia toi da (ti le gia goc) khach chap nhan voi mon nay.</summary>
        public float WillingRatio(CustomerType t, ItemDefinition d, float noise)
            => .95f + .3f * t.Pref(d) + t.wealth + Reputation / 100f * .2f + (S && IsBig(S.Clock.Day) ? .1f : 0)
               + (S && IsCauNgu(S.Clock.Day) && d.category == ItemCategory.Fish ? .15f : 0) + noise; // cau ngu: ca duoc gia

        /// <summary>Khach toi quay: chon mon, mua / tra gia / bo di.</summary>
        public Outcome Serve(MarketCustomer c)
        {
            var s = S;
            if (!Attended) { c.Say("Sạp không ai trông hả ta..."); return Outcome.NoStaff; }
            if (Stall.Count == 0) { c.Say("Sạp chưa bày gì hết."); return Outcome.Empty; }

            // chon mon theo so thich
            float total = 0;
            var ws = new float[Stall.Count];
            for (int i = 0; i < Stall.Count; i++) { ws[i] = .05f + c.Type.Pref(s.Items.Get(Stall[i].id)); total += ws[i]; }
            float r = (float)_rng.NextDouble() * total;
            int idx = 0;
            for (; idx < Stall.Count - 1; idx++) { r -= ws[idx]; if (r <= 0) break; }
            var slot = Stall[idx];
            var d = s.Items.Get(slot.id);
            float ratio = slot.pct / 100f;
            float willing = WillingRatio(c.Type, d, ((float)_rng.NextDouble() - .5f) * .16f);
            int k = Mathf.Min(slot.n, 1 + _rng.Next(3));

            if (ratio <= willing)
            {
                Sell(c, slot, k, slot.pct, ratio <= 1.05f ? 1f : ratio <= 1.25f ? .5f : .2f);
                c.Say(Pick(c.Type.buyLines));
                return Outcome.Bought;
            }
            if (ratio <= willing + .3f)
            {
                int offer = Mathf.Clamp(Mathf.FloorToInt(willing * 10) * 10, MinPct, MaxPct);
                if (offer >= slot.pct) offer = slot.pct - 10;
                if (offer < MinPct) { c.Say(Pick(c.Type.leaveLines)); Reputation = Mathf.Max(0, Reputation - .2f); return Outcome.Left; }
                AskHaggle(c, slot, k, offer);
                return Outcome.Haggle;
            }
            c.Say(Pick(c.Type.leaveLines));
            Reputation = Mathf.Max(0, Reputation - .2f);
            return Outcome.Left;
        }

        void AskHaggle(MarketCustomer c, StallSlot slot, int k, int offerPct)
        {
            var s = S;
            var d = s.Items.Get(slot.id);
            int price = PriceOf(d, offerPct);
            string line = string.Format(Pick(c.Type.haggleLines), price);
            _haggling = c;
            void Accept()
            {
                if (Stall.Contains(slot) && slot.n > 0) { Sell(c, slot, Mathf.Min(k, slot.n), offerPct, .3f); c.Say("Cảm ơn con nghen!"); }
                c.Leave(); _haggling = null;
            }
            void Refuse()
            {
                Reputation = Mathf.Max(0, Reputation - .5f);
                c.Say(Pick(c.Type.leaveLines));
                c.Leave(); _haggling = null;
            }
            var ui = ActionUI.Instance;
            if (!ui) { Refuse(); return; }
            ui.ShowDialog($"{c.Type.name} trả giá",
                $"{line}\n\n{k} {d.displayName}: bạn ra giá {PriceOf(d, slot.pct)} xu/cái ({slot.pct}%), khách trả {price} xu/cái ({offerPct}%).",
                ($"Bán {price} xu", (Action)Accept), ("Giữ giá", (Action)Refuse));
        }

        /// <summary>Chi dung cho test: tra loi tra gia ma khong qua hop thoai.</summary>
        public bool HagglePending => _haggling;

        void Sell(MarketCustomer c, StallSlot slot, int k, int pct, float rep)
        {
            var s = S;
            var d = s.Items.Get(slot.id);
            int price = PriceOf(d, pct) * k;
            s.Coins += price;
            GameAudio.Play(Sfx.Coin);
            Journal.Stat("earn", price);
            slot.n -= k;
            if (slot.n <= 0) Stall.Remove(slot);
            SoldToday += k; EarnedToday += price;
            Reputation = Mathf.Min(100, Reputation + rep);
            var p = Pavilion;
            if (p != null) ActionUI.Float($"+{price} xu", p.center + Vector3.up * 1.6f, new Color(1f, .86f, .35f));
            Notify();
        }

        string Pick(string[] a) => a[_rng.Next(a.Length)];
        static float Flat(Vector3 a, Vector3 b) => Vector2.Distance(new Vector2(a.x, a.z), new Vector2(b.x, b.z));

        // ------------------------------------------------------------------ hang bay tren quay
        static Mesh _basket;

        void RefreshGoods()
        {
            var p = Pavilion;
            if (p == null) return;
            if (_goods) Destroy(_goods);
            _goods = new GameObject("Goods");
            _goods.transform.SetParent(p.go.transform, false);
            if (!_basket)
            {
                var b = new MeshBuilder();
                b.P(LowPolyShapes.Cylinder(.17f, .13f, .12f, 10), "#8a6a40", 0, .06f, 0);
                b.P(LowPolyShapes.Cylinder(.18f, .18f, .02f, 10), "#6b4a2a", 0, .12f, 0);
                _basket = b.ToMesh("MarketBasket");
            }
            for (int i = 0; i < Stall.Count && i < 10; i++)
            {
                var d = S.Items.Get(Stall[i].id);
                float x = -1.05f + (i % 5) * .52f, z = i < 5 ? .62f : -.55f;
                var g = new GameObject("slot" + i);
                g.transform.SetParent(_goods.transform, false);
                g.transform.localPosition = new Vector3(x, .41f, z);
                g.AddComponent<MeshFilter>().sharedMesh = _basket;
                g.AddComponent<MeshRenderer>().sharedMaterial = MaterialLibrary.Objects(false);
                var mat = MaterialLibrary.Solid(d ? d.iconColor : Color.white);
                int n = Mathf.Clamp(Stall[i].n, 1, 3);
                for (int q = 0; q < n; q++)
                {
                    var ball = new GameObject("item");
                    ball.transform.SetParent(g.transform, false);
                    ball.transform.localPosition = new Vector3((q - (n - 1) / 2f) * .09f, .16f + (q == 1 ? .04f : 0), 0);
                    ball.transform.localScale = Vector3.one * .11f;
                    ball.AddComponent<MeshFilter>().sharedMesh = ProceduralMeshes.Sphere;
                    ball.AddComponent<MeshRenderer>().sharedMaterial = mat;
                }
            }
            _goods.SetActive(Stall.Count > 0);
        }

        // ------------------------------------------------------------------ luu / tai
        public List<StallSlot> Snapshot()
        {
            var l = new List<StallSlot>();
            foreach (var x in Stall) l.Add(new StallSlot { id = x.id, n = x.n, pct = x.pct });
            return l;
        }

        public void Restore(List<StallSlot> list, float rep)
        {
            Reputation = Mathf.Clamp(rep, 0, 100);
            Stall.Clear();
            if (list != null) foreach (var x in list) if (x != null && !string.IsNullOrEmpty(x.id) && x.n > 0) Stall.Add(x);
            Notify();
        }
    }
}
