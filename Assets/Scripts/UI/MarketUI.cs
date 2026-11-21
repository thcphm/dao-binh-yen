// Bang bay sap cho phien: cot trai = do trong tui (bay 1 / bay het), cot phai = cac o sap (gia -10% / +10%, cat lai).
// Dong bang roi dung gan sap: khach toi xem hang, mua, tra gia (hop thoai), hoac bo di.
using UnityEngine;
using UnityEngine.UIElements;

namespace SereneIsle
{
    public class MarketUI : MonoBehaviour
    {
        public static MarketUI Instance { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { Instance = null; }

        VisualElement _overlay, _bag, _stall;
        Label _title, _sub, _stallHead;
        bool _built;

        public bool IsOpen => _overlay != null && _overlay.style.display == DisplayStyle.Flex;

        GameSession S => GameSession.Instance;
        static Market M => Market.Instance;

        void Awake() { Instance = this; }
        void OnDestroy() { if (Instance == this) Instance = null; if (M) M.Changed -= OnChanged; }

        static VisualElement E(string name = null, string cls = null, bool pick = false)
        {
            var e = new VisualElement { name = name, pickingMode = pick ? PickingMode.Position : PickingMode.Ignore };
            if (cls != null) foreach (var c in cls.Split(' ')) e.AddToClassList(c);
            return e;
        }
        static Label L(string cls, string text = "") { var l = new Label(text) { pickingMode = PickingMode.Ignore }; if (cls != null) l.AddToClassList(cls); return l; }

        void Build()
        {
            var hud = GameHud.Instance;
            if (!hud || hud.Root == null) return;
            _built = true;
            var ss = Resources.Load<StyleSheet>("UI/Actions");
            if (ss && !hud.Root.styleSheets.Contains(ss)) hud.Root.styleSheets.Add(ss);
            _overlay = E("shop-overlay", "hud-root", true);
            var panel = E("shop", null, true);
            _title = L("panel-title", "Sạp chợ phiên của bạn");
            _sub = L("panel-sub");
            panel.Add(_title); panel.Add(_sub);
            var cols = E(null, "shop-cols");
            var left = E(null, "shop-col");
            left.Add(L("shop-head", "Trong túi"));
            var lsv = new ScrollView(ScrollViewMode.Vertical);
            _bag = lsv.contentContainer;
            left.Add(lsv);
            var right = E(null, "shop-col");
            _stallHead = L("shop-head");
            right.Add(_stallHead);
            var rsv = new ScrollView(ScrollViewMode.Vertical);
            _stall = rsv.contentContainer;
            right.Add(rsv);
            cols.Add(left); cols.Add(right);
            panel.Add(cols);
            panel.Add(L("panel-hint", "Giá 60–160% giá gốc. Giá hợp lý thì uy tín sạp tăng, khách ghé đông hơn; giá cao quá thì khách trả giá hoặc bỏ đi. Đóng bảng rồi đứng gần sạp để trông hàng."));
            var close = new Button(Close) { text = "Xong, trông sạp (Esc)" };
            close.AddToClassList("btn");
            panel.Add(close);
            _overlay.Add(panel);
            _overlay.style.display = DisplayStyle.None;
            hud.Root.Add(_overlay);
        }

        void OnChanged() { if (IsOpen) Render(); }

        public void Open()
        {
            if (!_built) Build();
            if (!_built || !S || !M) return;
            M.Changed -= OnChanged; M.Changed += OnChanged;
            _overlay.style.display = DisplayStyle.Flex;
            S.UiOpen = true;
            Render();
        }

        public void Close()
        {
            if (!IsOpen) return;
            _overlay.style.display = DisplayStyle.None;
            if (S) S.UiOpen = false;
        }

        void Update()
        {
            if (!_built) Build();
            if (IsOpen && GameInput.CancelPressed) Close();
        }

        void Render()
        {
            var s = S; var m = M;
            bool big = Market.IsBig(s.Clock.Day);
            _sub.text = $"{(big ? "Phiên chợ lớn · " : "")}Uy tín {m.Reputation:0}/100 · hôm nay bán {m.SoldToday} món, +{m.EarnedToday} xu"
                        + (m.IsOpen ? "" : $" · chợ mở lúc {Market.OpenHour:0} giờ");
            _stallHead.text = $"Trên sạp ({m.Stall.Count}/{m.Slots} ô)";

            _bag.Clear();
            int n = 0;
            foreach (var kv in s.Inventory.Stacks())
            {
                var d = s.Items.Get(kv.Key);
                if (!Shops.CanSell(d)) continue;
                string id = kv.Key;
                var row = Row(d, $"×{kv.Value} · giá gốc {d.price} xu");
                bool room = m.Stall.Exists(x => x.id == id) || m.Stall.Count < m.Slots;
                Btn(row, "Bày 1", room, () => Say(m.Put(id, 1)));
                Btn(row, "Bày hết", room, () => Say(m.Put(id, 999)));
                _bag.Add(row);
                n++;
            }
            if (n == 0) _bag.Add(L("shop-empty", "Túi chưa có gì bày bán được."));

            _stall.Clear();
            for (int i = 0; i < m.Stall.Count; i++)
            {
                var slot = m.Stall[i];
                var d = s.Items.Get(slot.id);
                int idx = i;
                var row = Row(d, $"×{slot.n} · {m.PriceOf(slot)} xu/cái ({slot.pct}%)");
                Btn(row, "−", slot.pct > Market.MinPct, () => m.SetPct(idx, slot.pct - 10));
                Btn(row, "+", slot.pct < Market.MaxPct, () => m.SetPct(idx, slot.pct + 10));
                Btn(row, "Cất", true, () => m.TakeBack(idx));
                _stall.Add(row);
            }
            if (m.Stall.Count == 0) _stall.Add(L("shop-empty", "Sạp trống. Bày nông sản, cá, bọ, đồ nhặt được lên đây."));
        }

        void Say(string msg) { if (!string.IsNullOrEmpty(msg)) S.Say(msg); Render(); }

        VisualElement Row(ItemDefinition d, string price)
        {
            var row = E(null, "shop-row", true);
            var icon = new ItemIcon();
            icon.Set(d);
            row.Add(icon);
            var text = E(null, "shop-text");
            text.Add(L("shop-name", d ? d.displayName : "?"));
            text.Add(L("shop-price", price));
            row.Add(text);
            return row;
        }

        static void Btn(VisualElement row, string label, bool enabled, System.Action act)
        {
            var b = new Button(act) { text = label };
            b.AddToClassList("btn");
            b.AddToClassList("shop-btn");
            b.SetEnabled(enabled);
            row.Add(b);
        }
    }
}
