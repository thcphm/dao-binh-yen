// Bang mua ban (ban web: openPanel('shop') dong 1528-1533, 1596-1600). Cot trai: hang cua tiem; cot phai: do trong tui de ban
// (chi tiem tap hoa thu mua, 50% gia). Dung thoi gian khi mo.
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace SereneIsle
{
    public class ShopUI : MonoBehaviour
    {
        public static ShopUI Instance { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { Instance = null; }

        VisualElement _overlay, _buyList, _sellList, _sellCol;
        Label _title, _sub;
        bool _built;
        string _shop;

        public bool IsOpen => _overlay != null && _overlay.style.display == DisplayStyle.Flex;
        public string Current => IsOpen ? _shop : null;

        GameSession S => GameSession.Instance;

        void Awake() { Instance = this; }
        void OnDestroy() { if (Instance == this) Instance = null; }

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
            _title = L("panel-title");
            _sub = L("panel-sub");
            panel.Add(_title); panel.Add(_sub);
            var cols = E(null, "shop-cols");
            var buyCol = E(null, "shop-col");
            buyCol.Add(L("shop-head", "Tiệm bán"));
            var bs = new ScrollView(ScrollViewMode.Vertical);
            _buyList = bs.contentContainer;
            buyCol.Add(bs);
            _sellCol = E(null, "shop-col");
            _sellCol.Add(L("shop-head", "Bạn bán (nửa giá gốc)"));
            var ssv = new ScrollView(ScrollViewMode.Vertical);
            _sellList = ssv.contentContainer;
            _sellCol.Add(ssv);
            cols.Add(buyCol); cols.Add(_sellCol);
            panel.Add(cols);
            var close = new Button(Close) { text = "Đóng (Esc)" };
            close.AddToClassList("btn");
            panel.Add(close);
            _overlay.Add(panel);
            _overlay.RegisterCallback<PointerDownEvent>(e => { if (e.target == _overlay) Close(); });
            _overlay.style.display = DisplayStyle.None;
            hud.Root.Add(_overlay);
        }

        void Update()
        {
            if (!_built) Build();
            if (IsOpen && GameInput.CancelPressed) Close();
        }

        public void Open(string shop)
        {
            if (!_built) Build();
            if (!_built || !S) return;
            _shop = shop;
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

        // ------------------------------------------------------------------ ve
        void Render()
        {
            var s = S;
            _title.text = Shops.Title(_shop);
            _sub.text = $"Bạn có {s.Coins:#,0} xu · túi {s.Inventory.SlotsUsed()}/{s.Inventory.Capacity} ô · {s.Inventory.Weight:0.#}/{s.Inventory.MaxWeight:0} kg".Replace(',', '.');

            _buyList.Clear();
            foreach (var o in Shops.Offers(_shop, s.Clock.Day, s.Items, CropDatabase.Instance, Crafts.Level(Crafts.Garden)))
            {
                var d = s.Items.Get(o.id);
                if (!d) continue;
                var offer = o;
                var row = Row(d, $"{o.price} xu", d.description);
                Btn(row, "Mua 1", s.Coins >= o.price && s.Inventory.RoomFor(o.id) > 0, () => Do(Shops.Buy(s, offer, 1, out _)));
                Btn(row, "Mua 5", s.Coins >= o.price * 5 && s.Inventory.RoomFor(o.id) >= 5, () => Do(Shops.Buy(s, offer, 5, out _)));
                _buyList.Add(row);
            }

            bool buys = Shops.Buys(_shop);
            _sellCol.style.display = buys ? DisplayStyle.Flex : DisplayStyle.None;
            _sellList.Clear();
            if (!buys) return;
            int n = 0;
            foreach (var kv in s.Inventory.Stacks())
            {
                var d = s.Items.Get(kv.Key);
                if (!Shops.CanSell(d)) continue;
                string id = kv.Key;
                int price = Shops.SellPrice(d);
                var row = Row(d, $"×{kv.Value} · {price} xu/cái", null);
                Btn(row, "Bán 1", true, () => Do(Shops.Sell(s, id, 1, out _)));
                Btn(row, "Bán hết", true, () => Do(Shops.Sell(s, id, -1, out _)));
                _sellList.Add(row);
                n++;
            }
            if (n == 0) _sellList.Add(L("shop-empty", "Túi chưa có gì bán được. Chặt cây, câu cá, bắt bọ rồi mang lại đây."));
        }

        void Do(string msg)
        {
            if (!string.IsNullOrEmpty(msg)) S.Say(msg);
            Render();
        }

        VisualElement Row(ItemDefinition d, string price, string tip)
        {
            var row = E(null, "shop-row", true);
            var icon = new ItemIcon();
            icon.Set(d);
            row.Add(icon);
            var text = E(null, "shop-text");
            text.Add(L("shop-name", d.displayName));
            text.Add(L("shop-price", price));
            row.Add(text);
            if (!string.IsNullOrEmpty(tip)) row.tooltip = tip;
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
