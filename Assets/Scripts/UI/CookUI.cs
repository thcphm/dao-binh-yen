// Bang nau an o lua trai (ban web: openPanel('cook') dong 1543-1545). Moi mon: nguyen lieu (dang co), tac dung, nut Nau.
// Nut "Ngoi suoi lua": ngoi xuong canh lua (thoi gian troi nhanh, hoi binh yen + nang luong).
using UnityEngine;
using UnityEngine.UIElements;
using static SereneIsle.MenuPanels;

namespace SereneIsle
{
    public class CookUI : MonoBehaviour
    {
        public static CookUI Instance { get; private set; }
        public bool IsOpen => _overlay != null && _overlay.parent != null;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { Instance = null; }

        VisualElement _overlay, _card;
        GameSession S => GameSession.Instance;

        void Awake() { Instance = this; }
        void OnDestroy() { if (Instance == this) Instance = null; }

        void Update()
        {
            if (IsOpen && GameInput.CancelPressed) Close();
        }

        public void Open()
        {
            var hud = GameHud.Instance;
            if (!hud || hud.Root == null || IsOpen) return;
            foreach (var n in new[] { "UI/Menu", "UI/Actions" })
            {
                var ss = Resources.Load<StyleSheet>(n);
                if (ss && !hud.Root.styleSheets.Contains(ss)) hud.Root.styleSheets.Add(ss);
            }
            _overlay = Overlay(hud.Root, out _card, Close, "wide");
            S.UiOpen = true;
            Render(null);
        }

        public void Close()
        {
            if (!IsOpen) return;
            _overlay.RemoveFromHierarchy();
            _overlay = null;
            if (S) S.UiOpen = false;
        }

        void Render(string msg)
        {
            var s = S;
            _card.Clear();
            _card.Add(L("panel-title", "Nấu ăn bên lửa trại"));
            _card.Add(L("panel-sub", msg ?? "Món nấu chín no lâu hơn đồ sống. Nấu xong món mới cũng thấy lòng bình yên hơn."));
            var scroll = new ScrollView(ScrollViewMode.Vertical) { horizontalScrollerVisibility = ScrollerVisibility.Hidden };
            scroll.style.flexShrink = 1;
            scroll.style.maxHeight = 470;
            _card.Add(scroll);
            foreach (var r in Cooking.All)
            {
                var d = s.Items.Get(r.id);
                if (!d) continue;
                var row = E(null, "craft-row", true);
                var icon = new ItemIcon();
                icon.Set(d);
                row.Add(icon);
                var text = E(null, "craft-text");
                text.Add(L("craft-name", $"{d.displayName} · {GameSession.EatText(d)}"));
                text.Add(L("craft-desc", r.note));
                text.Add(L("craft-req", Cooking.ReqText(r, s.Inventory, s.Items)));
                row.Add(text);
                var recipe = r;
                var btn = Btn("Nấu", () => Cook(recipe), "craft-btn");
                btn.SetEnabled(Cooking.CanCook(r, s.Inventory, s.Items));
                row.Add(btn);
                scroll.Add(row);
            }
            var foot = E(null, "menu-buttons");
            foot.Add(Btn("Ngồi sưởi lửa", () => { Close(); s.Sit(); }, "muted"));
            foot.Add(Btn("Đóng (Esc)", Close));
            _card.Add(foot);
        }

        void Cook(CookRecipe r)
        {
            var s = S;
            var d = s.Items.Get(r.id);
            if (!Cooking.Cook(r, s.Inventory, s.Items)) { Render("Thiếu nguyên liệu."); return; }
            if (s.Inventory.Count(r.id) == 0 && s.Player) ItemPile.Spawn(r.id, 1, s.Player.transform.position); // tui day
            s.Stats.Calm = Mathf.Min(100, s.Stats.Calm + 1);
            if (s.Body) s.Body.PlayAct("pickup");
            Journal.Stat("cook");
            Render($"Đã nấu {d.displayName}! (+1 bình yên)");
        }
    }
}
