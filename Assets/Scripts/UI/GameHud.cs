// HUD trong game bang UI Toolkit (ban web: #hud, #clock, #stats, #coins, #toasts, #hotbar, #help, #panel tui do, #fade).
// Dung hoan toan bang code + Resources/UI/Hud.uss, khong can UXML.
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace SereneIsle
{
    [DefaultExecutionOrder(100)]
    public class GameHud : MonoBehaviour
    {
        public static GameHud Instance { get; private set; }
        /// <summary>Chuot dang tren 1 phan tu giao dien (de khong dung do / xoay camera nham).</summary>
        public static bool PointerOverUI => Instance && Instance._pointerOver;
        /// <summary>Dang mo bang -> camera khong xoay.</summary>
        public static bool BlocksCamera => Instance && Instance.InventoryOpen;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { Instance = null; }

        public PanelSettings Panel { get; private set; }
        public VisualElement Root => _root;
        public bool InventoryOpen { get; private set; }

        UIDocument _doc;
        VisualElement _root, _hud;
        Label _dayLabel, _timeLabel, _coinLabel, _panelSub, _itemInfo, _fadeTitle, _fadeSub;
        StatIconElement _sun;
        VisualElement _hpFill, _waterFill, _energyFill, _calmFill, _energyRec, _weightFill;
        VisualElement _toasts, _hotbar, _overlay, _grid, _toolsRow, _fade, _ghost, _help;
        ItemIcon _ghostIcon;
        SlotView[] _slots;
        bool _pointerOver, _dirtyHotbar = true, _dirtyPanel = true;
        GameSession _hooked;

        GameSession S => GameSession.Instance;
        ItemDatabase DB => ItemDatabase.Instance;

        sealed class SlotView
        {
            public VisualElement root; public ItemIcon icon; public Label key, label, count;
        }

        // ================================================================== dung giao dien
        void Awake()
        {
            Instance = this;
            Build();
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
            Unhook();
            if (Panel) Destroy(Panel);
        }

        static Label L(string name, string cls = null, string text = "")
        {
            var l = new Label(text) { name = name, pickingMode = PickingMode.Ignore };
            if (cls != null) l.AddToClassList(cls);
            return l;
        }

        static VisualElement E(string name = null, string cls = null, bool pick = false)
        {
            var e = new VisualElement { name = name, pickingMode = pick ? PickingMode.Position : PickingMode.Ignore };
            if (cls != null) foreach (var c in cls.Split(' ')) e.AddToClassList(c);
            return e;
        }

        void Build()
        {
            Panel = ScriptableObject.CreateInstance<PanelSettings>();
            Panel.name = "SereneHudPanel";
            Panel.themeStyleSheet = Resources.Load<ThemeStyleSheet>("UI/SereneTheme");
            Panel.scaleMode = PanelScaleMode.ScaleWithScreenSize;
            Panel.referenceResolution = new Vector2Int(1600, 900);
            Panel.screenMatchMode = PanelScreenMatchMode.MatchWidthOrHeight;
            Panel.match = .5f;
            Panel.sortingOrder = 10;

            var go = new GameObject("HudDocument");
            go.SetActive(false);
            go.transform.SetParent(transform, false);
            _doc = go.AddComponent<UIDocument>();
            _doc.panelSettings = Panel;
            go.SetActive(true);

            _root = _doc.rootVisualElement;
            _root.pickingMode = PickingMode.Ignore;
            var ss = Resources.Load<StyleSheet>("UI/Hud");
            if (ss) _root.styleSheets.Add(ss);
            _hud = E("hud", "hud-root");
            _root.Add(_hud);

            // ---- dong ho + chi so ----
            var clock = E("clock", "chip", true);
            var row = E(null, "clock-row");
            _dayLabel = L("day-label");
            _sun = new StatIconElement(StatIcon.Sun) { name = "sun-dot" };
            row.Add(_dayLabel); row.Add(_sun);
            clock.Add(row);
            _timeLabel = L("time-label", null, "14:00");
            clock.Add(_timeLabel);
            _hpFill = StatRow(clock, StatIcon.Heart, "hp-fill", out _);
            _waterFill = StatRow(clock, StatIcon.Drop, "water-fill", out _);
            _energyFill = StatRow(clock, StatIcon.Bolt, "energy-fill", out var enBar);
            _energyRec = E(null, "bar-recover");
            enBar.Add(_energyRec);
            _calmFill = StatRow(clock, StatIcon.Leaf, "calm-fill", out _);
            _hud.Add(clock);

            // ---- xu ----
            var coins = E("coins", "chip", true);
            var coinIcon = new ItemIcon { name = "coin-icon" };
            coinIcon.SetShape(IconShape.Coin, MaterialLibrary.Hex("#ffd34d"), MaterialLibrary.Hex("#9a6510"));
            coins.Add(coinIcon);
            _coinLabel = L("coin-label", null, "0");
            coins.Add(_coinLabel);
            _hud.Add(coins);

            // ---- thong bao ----
            _toasts = E("toasts");
            _hud.Add(_toasts);

            // ---- thanh nhanh ----
            _hotbar = E("hotbar");
            _slots = new SlotView[Hotbar.Size];
            for (int i = 0; i < Hotbar.Size; i++)
            {
                int idx = i;
                var v = new SlotView { root = E("slot" + i, "slot", true) };
                v.key = L(null, "slot-key", i == 9 ? "0" : (i + 1).ToString());
                v.icon = new ItemIcon();
                v.label = L(null, "slot-label");
                v.count = L(null, "slot-count");
                v.root.Add(v.icon); v.root.Add(v.label); v.root.Add(v.key); v.root.Add(v.count);
                v.root.RegisterCallback<PointerDownEvent>(e => OnSlotDown(idx, e));
                v.root.RegisterCallback<PointerMoveEvent>(OnDragMove);
                v.root.RegisterCallback<PointerUpEvent>(OnDragUp);
                _slots[i] = v;
                _hotbar.Add(v.root);
            }
            _hud.Add(_hotbar);

            // ---- huong dan ----
            _help = E("help", "chip", true);
            _help.Add(L(null, null, "WASD đi · Shift chạy · Space nhảy"));
            _help.Add(L(null, null, "1–9, 0 hoặc lăn chuột: đổi ô · F / chuột trái: dùng"));
            _help.Add(L(null, null, "I: túi đồ · kéo chuột phải: xoay · Ctrl+lăn: zoom"));
            _help.Add(L(null, null, "H: ẩn hướng dẫn"));
            _hud.Add(_help);

            BuildInventoryPanel();

            // ---- bong keo tha ----
            _ghost = E("drag-ghost");
            _ghostIcon = new ItemIcon();
            _ghost.Add(_ghostIcon);
            _ghost.style.display = DisplayStyle.None;
            _hud.Add(_ghost);

            // ---- chuyen ngay ----
            _fade = E("fade");
            _fadeTitle = L("fade-title");
            _fadeSub = L("fade-sub");
            _fade.Add(_fadeTitle); _fade.Add(_fadeSub);
            _hud.Add(_fade);
        }

        VisualElement StatRow(VisualElement parent, StatIcon icon, string fillName, out VisualElement bar)
        {
            var row = E(null, "stat");
            row.Add(new StatIconElement(icon));
            bar = E(null, "bar");
            var fill = E(fillName, "bar-fill");
            bar.Add(fill);
            row.Add(bar);
            parent.Add(row);
            return fill;
        }

        void BuildInventoryPanel()
        {
            _overlay = E("overlay", null, true);
            _overlay.style.display = DisplayStyle.None;
            var panel = E("panel", null, true);
            panel.Add(L(null, "panel-title", "Túi đồ"));
            _panelSub = L(null, "panel-sub");
            panel.Add(_panelSub);
            var wbar = E(null, "weight-bar");
            _weightFill = E("weight-fill");
            wbar.Add(_weightFill);
            panel.Add(wbar);
            panel.Add(L(null, "panel-section", "ĐỒ TRONG TÚI"));
            _grid = E(null, "grid");
            panel.Add(_grid);
            panel.Add(L(null, "panel-section", "DỤNG CỤ"));
            _toolsRow = E(null, "grid");
            panel.Add(_toolsRow);
            _itemInfo = L("item-info", null, "Rê chuột lên một món để xem.");
            panel.Add(_itemInfo);
            panel.Add(L(null, "panel-hint", "Kéo đồ xuống thanh nhanh để gán vào ô. Nhấp đồ ăn, nước uống để dùng. Chuột phải lên ô ở thanh nhanh để xoá."));
            var close = new Button(() => SetInventory(false)) { text = "Đóng (I)" };
            close.AddToClassList("btn");
            panel.Add(close);
            _overlay.Add(panel);
            // bam ra ngoai bang de dong
            _overlay.RegisterCallback<PointerDownEvent>(e => { if (e.target == _overlay) SetInventory(false); });
            _hud.Add(_overlay);
        }

        // ================================================================== ket noi session
        void Hook()
        {
            Unhook();
            _hooked = S;
            if (!_hooked) return;
            _hooked.Toast += ShowToast;
            _hooked.FadeShown += ShowFade;
            _hooked.FadeHidden += HideFade;
            _hooked.Hotbar.Changed += MarkDirty;
            _hooked.Inventory.Changed += MarkDirty;
            MarkDirty();
        }

        void Unhook()
        {
            if (!_hooked) { _hooked = null; return; }
            _hooked.Toast -= ShowToast;
            _hooked.FadeShown -= ShowFade;
            _hooked.FadeHidden -= HideFade;
            _hooked.Hotbar.Changed -= MarkDirty;
            _hooked.Inventory.Changed -= MarkDirty;
            _hooked = null;
        }

        void MarkDirty() { _dirtyHotbar = true; _dirtyPanel = true; }

        // ================================================================== cap nhat
        void Update()
        {
            if (_hooked != S) Hook();
            UpdatePointerOver();
            var s = S;
            bool show = s && s.Running;
            _hud.style.display = show || (s && s.Transitioning) ? DisplayStyle.Flex : DisplayStyle.None;
            if (!show) return;

            // phim
            if (!s.Transitioning)
            {
                if (GameInput.InventoryPressed) SetInventory(!InventoryOpen);
                else if (InventoryOpen && GameInput.CancelPressed) SetInventory(false);
            }
            var kb = Keyboard.current;
            if (kb != null && kb.hKey.wasPressedThisFrame)
                _help.style.display = _help.style.display == DisplayStyle.None ? DisplayStyle.Flex : DisplayStyle.None;

            // dong ho + chi so
            _dayLabel.text = $"NGÀY {s.Clock.Day}";
            _timeLabel.text = s.Clock.TimeLabel;
            _sun.Icon = s.Clock.IsNight ? StatIcon.Moon : StatIcon.Sun;
            var st = s.Stats;
            float en = Mathf.Clamp01(st.Energy / st.EnergyMax) * 100f;
            SetBar(_hpFill, st.Hp, true);
            SetBar(_waterFill, st.Water, true);
            SetBar(_energyFill, en, true);
            SetBar(_calmFill, st.Calm, false);
            _energyRec.style.left = Length.Percent(en);
            _energyRec.style.width = Length.Percent(Mathf.Min(100 - en, st.Recover / st.EnergyMax * 100f));
            _coinLabel.text = s.Coins.ToString("#,0", CultureInfo.InvariantCulture).Replace(',', '.');

            if (_dirtyHotbar) { _dirtyHotbar = false; RenderHotbar(); }
            if (InventoryOpen && _dirtyPanel) { _dirtyPanel = false; RenderPanel(); }
        }

        static void SetBar(VisualElement fill, float pct, bool blinkLow)
        {
            fill.style.width = Length.Percent(Mathf.Clamp(pct, 0, 100));
            fill.style.opacity = blinkLow && pct < 20 ? .5f + .5f * Mathf.Abs(Mathf.Sin(Time.unscaledTime * 4f)) : 1f;
        }

        void UpdatePointerOver()
        {
            _pointerOver = false;
            var mouse = Mouse.current;
            var panel = _root?.panel;
            if (mouse == null || panel == null) return;
            Vector2 sp = mouse.position.ReadValue();
            sp.y = Screen.height - sp.y;
            var pp = RuntimePanelUtils.ScreenToPanel(panel, sp);
            _pointerOver = panel.Pick(pp) != null;
        }

        // ================================================================== thanh nhanh
        void RenderHotbar()
        {
            var s = S;
            for (int i = 0; i < Hotbar.Size; i++)
            {
                var v = _slots[i];
                string id = s.Hotbar[i];
                var d = DB.Get(id);
                v.root.RemoveFromClassList("slot--on"); v.root.RemoveFromClassList("slot--lock"); v.root.RemoveFromClassList("slot--out");
                v.count.text = "";
                if (!d) { v.icon.Set(null); v.label.text = "Trống"; continue; }
                bool locked = d.IsTool && !s.OwnedTools.Contains(id);
                v.icon.Set(d, locked);
                v.label.text = locked ? "Chưa có" : d.displayName;
                if (locked) v.root.AddToClassList("slot--lock");
                if (!d.IsTool)
                {
                    int n = s.Inventory.Count(id);
                    v.count.text = n.ToString();
                    if (n <= 0) v.root.AddToClassList("slot--out");
                }
                if (s.Hotbar.Selected == i && !locked) v.root.AddToClassList("slot--on");
            }
        }

        // ================================================================== tui do
        public void SetInventory(bool open)
        {
            var s = S;
            if (!s) return;
            InventoryOpen = open;
            s.UiOpen = open;
            _overlay.style.display = open ? DisplayStyle.Flex : DisplayStyle.None;
            if (open) { _dirtyPanel = false; RenderPanel(); }
            CancelDrag();
        }

        void RenderPanel()
        {
            var s = S;
            var inv = s.Inventory;
            _panelSub.text = $"{inv.Bag.name} · {inv.SlotsUsed()}/{inv.Capacity} ô · {inv.Weight:0.#}/{inv.MaxWeight:0} kg";
            _weightFill.style.width = Length.Percent(Mathf.Clamp01(inv.Weight / inv.MaxWeight) * 100f);

            _grid.Clear();
            var stacks = inv.Stacks();
            for (int i = 0; i < inv.Capacity; i++)
            {
                if (i < stacks.Count) _grid.Add(MakeCell(stacks[i].Key, stacks[i].Value, false));
                else _grid.Add(E(null, "cell cell--empty"));
            }

            _toolsRow.Clear();
            foreach (var tid in Hotbar.DefaultLayout())
            {
                if (string.IsNullOrEmpty(tid)) continue;
                _toolsRow.Add(MakeCell(tid, 0, !s.OwnedTools.Contains(tid)));
            }
        }

        VisualElement MakeCell(string id, int count, bool locked)
        {
            var d = DB.Get(id);
            var cell = E(null, locked ? "cell cell--lock" : "cell", true);
            var icon = new ItemIcon();
            icon.Set(d, locked);
            cell.Add(icon);
            if (count > 0) cell.Add(L(null, "cell-count", count.ToString()));
            cell.RegisterCallback<PointerEnterEvent>(_ => _itemInfo.text = Describe(d, locked));
            if (!locked)
            {
                cell.RegisterCallback<PointerDownEvent>(e => BeginDrag(cell, e, -1, id));
                cell.RegisterCallback<PointerMoveEvent>(OnDragMove);
                cell.RegisterCallback<PointerUpEvent>(OnDragUp);
            }
            return cell;
        }

        static string Describe(ItemDefinition d, bool locked)
        {
            if (!d) return "";
            if (d.IsTool) return locked ? $"{d.displayName} · chưa có" : $"{d.displayName} · dụng cụ (kéo xuống thanh nhanh)";
            var parts = new List<string> { d.displayName };
            if (d.IsFood) parts.Add(GameSession.EatText(d));
            parts.Add($"{d.weight:0.##} kg");
            if (d.price > 0) parts.Add($"bán {d.price} xu");
            return string.Join(" · ", parts);
        }

        // ================================================================== keo tha (ban web: DND)
        sealed class Drag
        {
            public VisualElement src; public int pointerId; public Vector2 start; public bool on;
            public int fromSlot; public string itemId; // fromSlot >= 0: keo tu thanh nhanh, nguoc lai keo tu tui
        }
        Drag _drag;
        int _dragTarget = -1;

        void OnSlotDown(int slot, PointerDownEvent e)
        {
            var s = S;
            if (!s) return;
            if (e.button == 1) { s.Hotbar.Clear(slot); e.StopPropagation(); return; }
            BeginDrag(_slots[slot].root, e, slot, s.Hotbar[slot]);
        }

        void BeginDrag(VisualElement src, PointerDownEvent e, int fromSlot, string id)
        {
            if (e.button != 0) return;
            _drag = new Drag { src = src, pointerId = e.pointerId, start = e.position, fromSlot = fromSlot, itemId = id };
            src.CapturePointer(e.pointerId);
            e.StopPropagation();
        }

        void OnDragMove(PointerMoveEvent e)
        {
            if (_drag == null || e.pointerId != _drag.pointerId) return;
            Vector2 p = e.position;
            if (!_drag.on)
            {
                if (Vector2.Distance(p, _drag.start) < 7 || string.IsNullOrEmpty(_drag.itemId)) return;
                _drag.on = true;
                _ghostIcon.Set(DB.Get(_drag.itemId));
                _ghost.style.display = DisplayStyle.Flex;
            }
            _ghost.style.left = p.x - 29;
            _ghost.style.top = p.y - 29;
            int t = SlotAt(p);
            if (t != _dragTarget)
            {
                if (_dragTarget >= 0) _slots[_dragTarget].root.RemoveFromClassList("slot--target");
                _dragTarget = t;
                if (t >= 0) _slots[t].root.AddToClassList("slot--target");
            }
        }

        void OnDragUp(PointerUpEvent e)
        {
            var d = _drag;
            if (d == null || e.pointerId != d.pointerId) return;
            if (d.src.HasPointerCapture(e.pointerId)) d.src.ReleasePointer(e.pointerId);
            var s = S;
            int to = SlotAt(e.position);
            CancelDrag();
            if (!s) return;
            if (!d.on)
            {
                // nhap chuot
                if (d.fromSlot >= 0) s.SelectSlot(d.fromSlot);
                else { var def = DB.Get(d.itemId); if (def && def.IsFood) s.Consume(d.itemId); }
                return;
            }
            if (to < 0) return;
            if (d.fromSlot >= 0) s.Hotbar.Swap(d.fromSlot, to);
            else s.Hotbar.Assign(to, d.itemId);
        }

        void CancelDrag()
        {
            if (_dragTarget >= 0) _slots[_dragTarget].root.RemoveFromClassList("slot--target");
            _dragTarget = -1;
            _ghost.style.display = DisplayStyle.None;
            if (_drag != null && _drag.src.HasPointerCapture(_drag.pointerId)) _drag.src.ReleasePointer(_drag.pointerId);
            _drag = null;
        }

        int SlotAt(Vector2 panelPos)
        {
            for (int i = 0; i < _slots.Length; i++) if (_slots[i].root.worldBound.Contains(panelPos)) return i;
            return -1;
        }

        // ================================================================== thong bao + chuyen ngay
        void ShowToast(string msg)
        {
            var t = L(null, "chip", msg);
            t.AddToClassList("toast");
            t.AddToClassList("toast--in");
            _toasts.Add(t);
            while (_toasts.childCount > 5) _toasts.RemoveAt(0);
            t.schedule.Execute(() => t.RemoveFromClassList("toast--in")).StartingIn(20);
            t.schedule.Execute(() => t.RemoveFromHierarchy()).StartingIn(3200);
        }

        void ShowFade(int day, string sub)
        {
            _fadeTitle.text = $"Ngày {day}";
            _fadeSub.text = sub;
            _fade.pickingMode = PickingMode.Position;
            _fade.AddToClassList("fade--on");
            if (InventoryOpen) SetInventory(false);
        }

        void HideFade()
        {
            _fade.RemoveFromClassList("fade--on");
            _fade.pickingMode = PickingMode.Ignore;
        }
    }
}
