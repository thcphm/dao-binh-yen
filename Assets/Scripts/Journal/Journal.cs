// Nhat ky (ban web: MILES / stat dong 1215-1221, JOURNAL / journalCheck dong 1226-1234, calmUpdate dong 1235-1239):
//  - Cot moc: 12 loai x 3 bac -> thuong xu + binh yen (ban web thuong diem giay phep; ban nay da thay bang hoc nghe).
//    "Nha suu tam" dem so loai trong So sinh vat (thay bao tang; ban web bac III khong the dat vi chi co 36 hien vat).
//  - Trang nhat ky mo theo binh yen (0, 15, 30 ... 100).
//  - Binh yen: dung yen ngam binh minh / hoang hon (+3), ngam sao dem quang (+3), moi sang -2, binh yen 100: +10 mau moi sang.
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SereneIsle
{
    public sealed class Milestone { public string id, name, desc; public int[] t; }

    public class Journal : MonoBehaviour
    {
        public static Journal Instance { get; private set; }

        public static readonly Milestone[] Miles =
        {
            new Milestone { id = "wood", name = "Tiều phu", desc = "Thu gỗ", t = new[] { 50, 250, 1000 } },
            new Milestone { id = "mine", name = "Thợ đá", desc = "Thu đá, quặng", t = new[] { 40, 200, 800 } },
            new Milestone { id = "fish", name = "Ngư dân", desc = "Câu được cá", t = new[] { 10, 60, 200 } },
            new Milestone { id = "bug", name = "Nhà côn trùng học", desc = "Bắt côn trùng", t = new[] { 10, 40, 120 } },
            new Milestone { id = "crop", name = "Nhà nông", desc = "Thu hoạch nông sản", t = new[] { 20, 120, 500 } },
            new Milestone { id = "ranch", name = "Chủ trại", desc = "Trứng, sữa, phân chuồng thu được", t = new[] { 5, 40, 150 } },
            new Milestone { id = "earn", name = "Thương lái", desc = "Xu kiếm từ bán hàng", t = new[] { 500, 5000, 25000 } },
            new Milestone { id = "cook", name = "Đầu bếp", desc = "Món đã nấu", t = new[] { 5, 30, 100 } },
            new Milestone { id = "walk", name = "Lữ khách", desc = "Mét đã đi", t = new[] { 2000, 15000, 60000 } },
            new Milestone { id = "book", name = "Nhà sưu tầm", desc = "Loài trong sổ sinh vật", t = new[] { 5, 20, 45 } },
            new Milestone { id = "letter", name = "Người nhặt thư", desc = "Chai trôi dạt đã nhặt", t = new[] { 3, 10, 25 } },
            new Milestone { id = "treasure", name = "Thợ săn kho báu", desc = "Kho báu đã đào", t = new[] { 1, 5, 15 } },
        };
        public static readonly int[] RewardCoins = { 100, 300, 800 };

        public static readonly (int calm, string title, string text)[] Pages =
        {
            (0, "Ngày đầu", "Lần đầu tiên sau mười bốn tháng, mình tắt chuông báo thức. Ở đây chẳng ai cần mình trả lời email lúc nửa đêm."),
            (15, "Tiếng sóng", "Mình ngồi nghe sóng vỗ cả buổi chiều mà không thấy có lỗi. Hoá ra \"không làm gì\" cũng là một việc cần học."),
            (30, "Bàn tay", "Tay mình chai sạn vì cầm rìu, nhưng đầu thì nhẹ hẳn. Mệt kiểu này ngủ ngon hơn mệt kiểu văn phòng nhiều."),
            (45, "Ông Bảy", "Ông Bảy bảo: \"Cây không lớn nhanh hơn vì cháu nhìn nó.\" Mình cười, rồi nghĩ về những deadline đã từng dí mình."),
            (60, "Bạn nhỏ", "Có một sinh vật nhỏ chờ mình mỗi sáng. Được ai đó cần đến theo cách dịu dàng như vậy, thật lạ."),
            (75, "Ngôi sao", "Đêm nay trời trong. Mình đếm được cả chòm sao hình con cá. Ở thành phố, mình chỉ đếm được số tin nhắn chưa đọc."),
            (90, "Ở lại", "Mẹ hỏi bao giờ mình về. Mình chưa biết. Nhưng lần đầu tiên, câu trả lời \"chưa biết\" không làm mình sợ."),
            (100, "Bình yên", "Mình đã tìm lại được nhịp thở của chính mình. Hòn đảo không chữa lành mình, nó chỉ cho mình đủ yên tĩnh để tự lành."),
        };

        public static readonly (int level, string perk)[] Perks =
        {
            (25, "Bước chân nhẹ: đi nhanh hơn 5%."),
            (50, "Thong thả: năng lượng hao chậm hơn 20%."),
            (75, "Tĩnh lặng: cá hiếm cắn câu nhiều hơn 20%."),
            (100, "Bình yên trọn vẹn: mỗi sáng hồi thêm 10 máu."),
        };

        readonly Dictionary<string, float> _stats = new Dictionary<string, float>();
        readonly Dictionary<string, int> _miles = new Dictionary<string, int>();
        public int PagesUnlocked { get; private set; } = 1;
        float _idle, _walkAcc;
        int _dawnDone = -1, _starDone = -1;
        Vector3 _lastPos;
        bool _hooked, _hasLast;

        GameSession S => GameSession.Instance;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { Instance = null; }

        void Awake() { Instance = this; }
        void OnDestroy() { if (Instance == this) Instance = null; if (_hooked && S) S.DayStarted -= OnDayStarted; }

        // ------------------------------------------------------------------ thong ke + cot moc
        public float StatOf(string id) => _stats.TryGetValue(id, out var v) ? v : 0;
        public int MileLevel(string id) => _miles.TryGetValue(id, out var v) ? v : 0;

        /// <summary>Cong thong ke (an toan khi chua co Journal).</summary>
        public static void Stat(string id, float n = 1) { if (Instance) Instance.Add(id, n); }

        /// <summary>Thong ke theo mon vua nhan (ban web: STAT_OF trong give()).</summary>
        public static void OnGive(ItemDefinition d, int n)
        {
            if (!Instance || !d || n <= 0) return;
            string id = d.id == "wood" ? "wood" : d.id == "stone" || d.id == "ore" ? "mine"
                      : d.category == ItemCategory.Fish ? "fish" : d.category == ItemCategory.Bug ? "bug" : d.category == ItemCategory.Crop ? "crop"
                      : d.id == "egg" || d.id == "milk" || d.id == "goatmilk" || d.id == "phan_chuong" ? "ranch" : null;
            if (id != null) Instance.Add(id, n);
        }

        void Add(string id, float n)
        {
            _stats[id] = StatOf(id) + n;
            var m = Array.Find(Miles, x => x.id == id);
            if (m == null) return;
            int lv = MileLevel(id);
            while (lv < 3 && _stats[id] >= m.t[lv])
            {
                var s = S;
                if (s)
                {
                    s.Coins += RewardCoins[lv];
                    s.Stats.Calm = Mathf.Min(100, s.Stats.Calm + 3);
                    s.Say($"Cột mốc: {m.name} {new[] { "I", "II", "III" }[lv]}! +{RewardCoins[lv]} xu, +3 bình yên");
                }
                lv++;
            }
            _miles[id] = lv;
        }

        // ------------------------------------------------------------------ so sinh vat (thay bao tang)
        public sealed class BookEntry { public string id, name, where; public bool known; public int price; public ItemDefinition item; public string kind; }

        public List<BookEntry> Book()
        {
            var s = S;
            var list = new List<BookEntry>();
            if (!s) return list;
            foreach (var it in s.Items.items)
            {
                if (!it || (it.category != ItemCategory.Fish && it.category != ItemCategory.Bug)) continue;
                string when = it.activeTime == "night" ? "ban đêm" : it.activeTime == "day" ? "ban ngày" : "cả ngày";
                list.Add(new BookEntry { id = it.id, name = it.displayName, item = it, price = it.price, known = s.Caught.Contains(it.id),
                                         kind = it.category == ItemCategory.Fish ? "Cá" : "Côn trùng", where = when });
            }
            var an = Animals.Instance;
            foreach (var a in AnimalDefs.All)
                if (a.role == AnimalRole.Wild)
                    list.Add(new BookEntry { id = "w_" + a.id, name = a.name, known = an && an.Seen.Contains(a.id), kind = "Thú hoang", where = string.Join(", ", Array.ConvertAll(a.biomes, b => RestorationModel.GroupName(b))) });
            // ca truoc, con trung, roi thu hoang (OrderBy giu thu tu trong nhom)
            return list.OrderBy(e => e.kind == "Cá" ? 0 : e.kind == "Côn trùng" ? 1 : 2).ToList();
        }

        public int BookKnown()
        {
            var s = S;
            int n = 0;
            if (s) foreach (var id in s.Caught) n++;
            if (Animals.Instance) n += Animals.Instance.Seen.Count;
            return n;
        }

        // ------------------------------------------------------------------ binh yen
        void Update()
        {
            var s = S;
            if (!s || !s.Running) return;
            if (!_hooked) { s.DayStarted += OnDayStarted; _hooked = true; }
            // trang nhat ky moi
            while (PagesUnlocked < Pages.Length && s.Stats.Calm >= Pages[PagesUnlocked].calm)
            {
                s.Say($"Nhật ký mới: \"{Pages[PagesUnlocked].title}\" (bấm J)");
                PagesUnlocked++;
            }
            // so sinh vat -> cot moc nha suu tam
            int book = BookKnown();
            if (book > StatOf("book")) Add("book", book - StatOf("book"));
            if (!s.Active) return;

            // quang duong (met)
            var p = s.Player.transform.position;
            if (_hasLast) { float dd = Vector2.Distance(new Vector2(p.x, p.z), new Vector2(_lastPos.x, _lastPos.z)); if (dd < 2) _walkAcc += dd; }
            _lastPos = p; _hasLast = true;
            if (_walkAcc >= 10) { Add("walk", Mathf.Floor(_walkAcc)); _walkAcc -= Mathf.Floor(_walkAcc); }

            // dung yen ngam binh minh / hoang hon / sao
            _idle = s.Player.Moving ? 0 : _idle + Time.deltaTime;
            float h = s.Clock.Hour;
            int day = s.Clock.Day;
            if (_idle > 8 && ((h >= 5.5f && h < 7.5f) || (h >= 17.3f && h < 19f)) && _dawnDone != day * 2 + (h > 12 ? 1 : 0))
            {
                _dawnDone = day * 2 + (h > 12 ? 1 : 0);
                s.Stats.Calm = Mathf.Min(100, s.Stats.Calm + 3);
                s.Say(h < 12 ? "Bạn đứng ngắm bình minh. Lòng nhẹ hẳn. (+Bình yên)" : "Hoàng hôn đỏ rực trên biển. (+Bình yên)");
            }
            bool clear = !(WeatherSystem.Instance && (WeatherSystem.Instance.IsRaining || WeatherSystem.Instance.Today == WeatherKind.Cloud));
            if (_idle > 10 && (h >= 20 || h < 4) && clear && _starDone != day)
            {
                _starDone = day;
                s.Stats.Calm = Mathf.Min(100, s.Stats.Calm + 3);
                s.Say("Trời đầy sao. Bạn tìm thấy chòm sao hình con cá. (+Bình yên)");
            }
        }

        void OnDayStarted(int day)
        {
            var s = S;
            s.Stats.Calm = Mathf.Max(0, s.Stats.Calm - 2);
            if (s.Stats.Calm >= 100) s.Stats.Hp = Mathf.Min(100, s.Stats.Hp + 10);
        }

        // ------------------------------------------------------------------ luu / tai
        public List<ItemCount> SnapshotStats()
        {
            var l = new List<ItemCount>();
            foreach (var kv in _stats) l.Add(new ItemCount { id = kv.Key, n = Mathf.FloorToInt(kv.Value) });
            return l;
        }

        public List<ItemCount> SnapshotMiles()
        {
            var l = new List<ItemCount>();
            foreach (var kv in _miles) l.Add(new ItemCount { id = kv.Key, n = kv.Value });
            return l;
        }

        public void Restore(List<ItemCount> stats, List<ItemCount> miles, int pages)
        {
            _stats.Clear(); _miles.Clear();
            if (stats != null) foreach (var c in stats) _stats[c.id] = c.n;
            if (miles != null) foreach (var c in miles) _miles[c.id] = c.n;
            PagesUnlocked = Mathf.Clamp(pages, 1, Pages.Length);
        }
    }
}
