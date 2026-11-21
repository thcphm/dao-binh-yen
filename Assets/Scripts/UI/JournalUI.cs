// So tay (phim J; ban web: panel 'journal' dong 1546-1552). The: So sinh vat (thay bao tang), Cot moc, Nhat ky, Binh yen.
using UnityEngine;
using UnityEngine.UIElements;
using static SereneIsle.MenuPanels;

namespace SereneIsle
{
    public class JournalUI : MonoBehaviour
    {
        public static JournalUI Instance { get; private set; }
        public bool IsOpen => _overlay != null && _overlay.parent != null;
        public string Tab { get; private set; } = "book";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { Instance = null; }

        VisualElement _overlay, _card;
        GameSession S => GameSession.Instance;
        static Journal J => Journal.Instance;

        void Awake() { Instance = this; }
        void OnDestroy() { if (Instance == this) Instance = null; }

        void Update()
        {
            var s = S;
            if (!s || !s.Running) return;
            if (IsOpen) { if (GameInput.CancelPressed || GameInput.JournalPressed) Close(); return; }
            if (GameInput.JournalPressed && s.Active) Open("book");
        }

        public void Open(string tab)
        {
            var hud = GameHud.Instance;
            if (!hud || hud.Root == null || !J) return;
            foreach (var n in new[] { "UI/Menu", "UI/Actions", "UI/Journal" })
            {
                var ss = Resources.Load<StyleSheet>(n);
                if (ss && !hud.Root.styleSheets.Contains(ss)) hud.Root.styleSheets.Add(ss);
            }
            if (!IsOpen)
            {
                _overlay = Overlay(hud.Root, out _card, Close, "wide journal");
                S.UiOpen = true;
            }
            Tab = tab;
            Render();
        }

        public void Close()
        {
            if (!IsOpen) return;
            _overlay.RemoveFromHierarchy();
            _overlay = null;
            if (S) S.UiOpen = false;
        }

        void Render()
        {
            var s = S; var j = J;
            _card.Clear();
            var head = E(null, "j-head");
            head.Add(L("panel-title", $"Sổ tay của {s.PlayerName}"));
            head.Add(L("j-calm", $"Bình yên {Mathf.RoundToInt(s.Stats.Calm)}/100 · {LunarCalendar.Date(s.Clock.Day).LongLabel}"));
            _card.Add(head);
            var tabs = E(null, "seg j-tabs");
            foreach (var (id, name) in new[] { ("book", "Sổ sinh vật"), ("mile", "Cột mốc"), ("diary", "Nhật ký"), ("calm", "Bình yên") })
            {
                string t = id;
                tabs.Add(Btn(name, () => Open(t), Tab == id ? null : "off"));
            }
            _card.Add(tabs);
            var scroll = new ScrollView(ScrollViewMode.Vertical) { horizontalScrollerVisibility = ScrollerVisibility.Hidden };
            scroll.style.flexShrink = 1;
            scroll.style.height = 470; // co dinh: doi tab khong lam the nhay kich thuoc
            _card.Add(scroll);
            var c = scroll.contentContainer;
            switch (Tab)
            {
                case "book": Book(c); break;
                case "mile": Miles(c); break;
                case "diary": Diary(c); break;
                default: Calm(c); break;
            }
            var foot = E(null, "menu-buttons");
            foot.Add(Btn("Đóng (J)", Close));
            _card.Add(foot);
        }

        void Book(VisualElement c)
        {
            var list = J.Book();
            int known = 0;
            foreach (var e in list) if (e.known) known++;
            c.Add(L("panel-sub", $"Đã ghi {known}/{list.Count} loài. Câu cá, bắt bọ, gặp thú hoang để điền sổ. Loài lạ hiện dấu ?"));
            string kind = null;
            VisualElement grid = null;
            foreach (var e in list)
            {
                if (e.kind != kind)
                {
                    kind = e.kind;
                    c.Add(L("j-section", kind.ToUpperInvariant()));
                    grid = E(null, "j-grid");
                    c.Add(grid);
                }
                var cell = E(null, "j-cell" + (e.known ? "" : " j-unknown"), true);
                if (e.item)
                {
                    var icon = new ItemIcon();
                    icon.Set(e.item, !e.known);
                    cell.Add(icon);
                }
                else cell.Add(L("j-animal", e.known ? "✿" : "?"));
                cell.Add(L("j-name", e.known ? e.name : "???"));
                cell.Add(L("j-info", e.known ? (e.item ? $"{e.price} xu · {e.where}" : e.where) : ""));
                grid.Add(cell);
            }
        }

        void Miles(VisualElement c)
        {
            c.Add(L("panel-sub", "Mỗi bậc cột mốc thưởng xu và bình yên."));
            foreach (var m in Journal.Miles)
            {
                int lv = J.MileLevel(m.id);
                float v = J.StatOf(m.id);
                int next = lv < 3 ? m.t[lv] : m.t[2];
                var row = E(null, "craft-row");
                var text = E(null, "craft-text");
                text.Add(L("craft-name", $"{m.name} {(lv > 0 ? new[] { "I", "II", "III" }[lv - 1] : "")}"));
                text.Add(L("craft-desc", lv >= 3 ? $"{m.desc}: {Mathf.FloorToInt(v):#,0} · đã hoàn thành" : $"{m.desc}: {Mathf.FloorToInt(v):#,0}/{next:#,0} · thưởng {Journal.RewardCoins[lv]} xu"));
                row.Add(text);
                var bar = E(null, "j-bar");
                var fill = E(null, "j-bar-fill");
                fill.style.width = Length.Percent(Mathf.Clamp01(v / next) * 100);
                bar.Add(fill);
                row.Add(bar);
                c.Add(row);
            }
        }

        void Diary(VisualElement c)
        {
            int n = J.PagesUnlocked;
            for (int i = 0; i < Journal.Pages.Length; i++)
            {
                var (calm, title, text) = Journal.Pages[i];
                var page = E(null, "j-page" + (i < n ? "" : " j-locked"));
                page.Add(L("craft-name", i < n ? title : $"Trang khoá · bình yên {calm}"));
                if (i < n) page.Add(L("j-text", text));
                c.Add(page);
            }
            if (Bottles.Instance) c.Add(L("j-section", $"ĐÃ ĐỌC {Bottles.Instance.LettersRead} LÁ THƯ TRONG CHAI"));
        }

        void Calm(VisualElement c)
        {
            var s = S;
            c.Add(L("j-text", "Bình yên tăng khi bạn sống chậm: ngắm bình minh, hoàng hôn, sao đêm; ngồi sưởi lửa (phím V); câu cá; nấu ăn; vuốt ve thú cưng, vật nuôi; cho thú hoang ăn; mở chai thư; tìm kho báu. Mỗi sáng bình yên giảm 2."));
            foreach (var (lv, perk) in Journal.Perks)
            {
                bool on = s.Stats.Calm >= lv;
                var row = E(null, "j-page" + (on ? "" : " j-locked"));
                row.Add(L("craft-name", $"{(on ? "✓" : "·")} Mức {lv}"));
                row.Add(L("j-text", perk));
                c.Add(row);
            }
        }
    }
}
