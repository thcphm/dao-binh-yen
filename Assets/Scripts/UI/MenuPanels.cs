// Cac bang dung chung cho menu chinh va menu tam dung: cai dat, ghi cong, ung ho (ban web: settingsHTML dong 1510,
// credits dong 1553, donate dong 1566).
using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace SereneIsle
{
    public static class MenuPanels
    {
        public const string GitHubUrl = "https://github.com/thcphm/serene-isle";

        public static VisualElement E(string name = null, string cls = null, bool pick = false)
        {
            var e = new VisualElement { name = name, pickingMode = pick ? PickingMode.Position : PickingMode.Ignore };
            if (cls != null) foreach (var c in cls.Split(' ')) e.AddToClassList(c);
            return e;
        }

        public static Label L(string cls, string text = "")
        {
            var l = new Label(text) { pickingMode = PickingMode.Ignore };
            if (cls != null) foreach (var c in cls.Split(' ')) l.AddToClassList(c);
            return l;
        }

        public static Button Btn(string text, Action act, string cls = null)
        {
            var b = new Button(act) { text = text };
            b.AddToClassList("btn");
            if (cls != null) foreach (var c in cls.Split(' ')) b.AddToClassList(c);
            return b;
        }

        /// <summary>Lop phu toan man hinh + the giua (bam ra ngoai thi dong).</summary>
        public static VisualElement Overlay(VisualElement parent, out VisualElement card, Action onBackdrop, string cardCls = null)
        {
            var ov = E(null, "menu-overlay hud-root", true);
            card = E(null, "menu-card" + (cardCls != null ? " " + cardCls : ""), true);
            ov.Add(card);
            ov.RegisterCallback<PointerDownEvent>(e => { if (e.target == ov) onBackdrop?.Invoke(); });
            parent.Add(ov);
            return ov;
        }

        // ------------------------------------------------------------------ cai dat
        static string _setTab = "sound";

        /// <summary>Cai dat chia 4 tab nhu ban web (Am thanh / Hinh anh / Dieu khien / Huong dan).
        /// exitToMenu: mo tu trong game thi co them nut luu va thoat ra menu chinh.</summary>
        public static void Settings(VisualElement card, Action done, Action exitToMenu = null)
        {
            var s = GameSettings.Current;
            card.Clear();
            card.Add(L("panel-title", "Cài đặt"));
            var tabs = E(null, "set-tabs");
            foreach (var (id, name) in new[] { ("sound", "Âm thanh"), ("gfx", "Hình ảnh"), ("ctrl", "Điều khiển"), ("help", "Hướng dẫn") })
            {
                string tid = id;
                tabs.Add(Btn(name, () => { _setTab = tid; Settings(card, done, exitToMenu); }, _setTab == id ? null : "off"));
            }
            card.Add(tabs);
            var scroll = new ScrollView(ScrollViewMode.Vertical) { horizontalScrollerVisibility = ScrollerVisibility.Hidden };
            scroll.style.flexShrink = 1;
            var c = scroll.contentContainer;
            card.Add(scroll);
            void Redraw() => Settings(card, done, exitToMenu);

            if (_setTab == "sound")
            {
                AddSlider(c, "Nhạc nền", s.music, v => { s.music = v; GameSettings.Set(s); });
                AddSlider(c, "Hiệu ứng âm thanh", s.sfx, v => { s.sfx = v; GameSettings.Set(s); });
            }
            else if (_setTab == "help") HelpTab(c);
            else if (_setTab == "ctrl")
            {
                AddSlider(c, "Độ nhạy chuột", Mathf.InverseLerp(.3f, 2.5f, s.mouseSensitivity), v => s.mouseSensitivity = Mathf.Lerp(.3f, 2.5f, v));
                AddToggle(c, "Kéo chuột đảo chiều", s.invertDrag, v => s.invertDrag = v);
                AddToggle(c, "Tự lưu mỗi sáng", s.autosave, v => s.autosave = v);
            }
            else GfxTab(c, s, Redraw);

            var row = E(null, "menu-buttons");
            row.Add(Btn("Khôi phục mặc định", () => { GameSettings.Set(new GameSettings()); Redraw(); }, "muted"));
            if (exitToMenu != null) row.Add(Btn("Lưu và thoát ra menu", () => { GameSettings.Set(s); exitToMenu(); }, "muted"));
            row.Add(Btn("Xong", () => { GameSettings.Set(s); done?.Invoke(); }));
            card.Add(row);
        }

        /// <summary>Bang phim (ban web: settingsHTML tab "help") + loi dan song sot.</summary>
        static void HelpTab(VisualElement c)
        {
            foreach (var (keys, what) in new[]
            {
                ("W A S D", "Di chuyển"), ("Shift", "Chạy nhanh (tốn sức gấp đôi)"), ("Space", "Nhảy"),
                ("Chuột trái · F", "Dùng đồ đang cầm (giữ để làm liên tục), ăn / uống"),
                ("E", "Nói chuyện, thu hoạch, ngủ, bàn chế tạo, nấu ăn"),
                ("1–9 · 0 · Lăn chuột", "Đổi ô trên thanh nhanh"), ("R", "Đổi đồ đặt khi cầm Xây dựng"),
                ("Cần câu: giữ F", "Lấy lực, thả để quăng"),
                ("I", "Túi đồ"), ("J", "Sổ tay: sinh vật, cột mốc, nhật ký, thư nhà"), ("V", "Ngồi nghỉ, ngắm cảnh (thời gian trôi nhanh, tăng Bình yên)"),
                ("M", "Bản đồ toàn đảo (lăn chuột phóng to, kéo để di chuyển)"),
                ("Kéo chuột phải", "Xoay camera"), ("Ctrl + lăn chuột", "Phóng to / thu nhỏ"), ("Esc", "Tạm dừng, cài đặt"),
            })
            {
                var r = E(null, "help-row");
                r.Add(L("help-key", keys));
                r.Add(L("help-what", what));
                c.Add(r);
            }
            c.Add(L("panel-sub", "Ngày bắt đầu 7 giờ. 23 giờ phải về tới lều, không thì ngất và mất hết đồ trong túi. Nhớ ăn uống đủ: tim (máu), giọt nước (nước), tia sét (sức)."));
        }

        static void GfxTab(VisualElement c, GameSettings s, Action redraw)
        {
            var vrow = E(null, "set-row");
            vrow.Add(L("set-label", "Tầm nhìn"));
            var seg = E(null, "seg");
            for (int i = 0; i < GameSettings.ViewNames.Length; i++)
            {
                int idx = i;
                var b = Btn(GameSettings.ViewNames[i], null, s.viewDistance == i ? null : "off");
                b.clicked += () => { s.viewDistance = idx; redraw(); };
                seg.Add(b);
            }
            vrow.Add(seg);
            c.Add(vrow);
            var trow = E(null, "set-row");
            trow.Add(L("set-label", "Cỡ chữ"));
            var tseg = E(null, "seg");
            tseg.AddToClassList("seg-tight");   // 4 nut vua mot hang
            for (int i = 0; i < GameSettings.TextSizeNames.Length; i++)
            {
                int idx = i;
                var b = Btn(GameSettings.TextSizeNames[i], null, s.textSize == i ? null : "off");
                b.clicked += () => { s.textSize = idx; GameSettings.Set(s); redraw(); };   // ap dung ngay de xem truoc
                tseg.Add(b);
            }
            trow.Add(tseg);
            c.Add(trow);
            AddToggle(c, "Bóng đổ", s.shadows, v => s.shadows = v);
            AddToggle(c, "Giảm độ phân giải (máy yếu)", s.renderScale < .99f, v => s.renderScale = v ? .75f : 1f);
        }

        static void AddSlider(VisualElement c, string label, float value01, Action<float> set)
        {
            var row = E(null, "set-row");
            row.Add(L("set-label", label));
            var sl = new Slider(0, 1) { value = Mathf.Clamp01(value01) };
            var val = L("set-value", $"{Mathf.RoundToInt(value01 * 100)}%");
            sl.RegisterValueChangedCallback(e => { set(e.newValue); val.text = $"{Mathf.RoundToInt(e.newValue * 100)}%"; });
            row.Add(sl);
            row.Add(val);
            c.Add(row);
        }

        static void AddToggle(VisualElement c, string label, bool value, Action<bool> set)
        {
            var row = E(null, "set-row");
            row.Add(L("set-label", label));
            var t = new Toggle { value = value };
            t.RegisterValueChangedCallback(e => set(e.newValue));
            row.Add(t);
            c.Add(row);
        }

        // ------------------------------------------------------------------ ghi cong
        public static void Credits(VisualElement card, Action done, bool licenses = false)
        {
            card.Clear();
            var scroll = new ScrollView(ScrollViewMode.Vertical);
            scroll.style.flexShrink = 1;
            var c = scroll.contentContainer;
            card.Add(scroll);
            c.Add(L("cr-logo", "Đảo Bình Yên"));
            c.Add(L("cr-logo-sub", "SERENE ISLE"));
            void Sec(string h, string p) { c.Add(L("cr-h", h)); c.Add(L("cr-p", p)); }
            if (!licenses)
            {
                Sec("Tạo bởi Thuc Pham", "Ý tưởng, thiết kế, lập trình và đồ hoạ.");
                Sec("Phiên bản Unity", "Làm lại từ bản web bằng Unity 6 (URP).");
                Sec("Mô hình 3D", "Nhân vật: KayKit Adventurers và Character Animations của Kay Lousberg.\nCây, đá, cỏ: Stylized Nature MegaKit của Quaternius.\nThú lớn: Ultimate Animated Animal Pack của Quaternius.");
                Sec("Âm nhạc", "Lofi tạo theo thủ tục (sẽ có ở bản Unity sau).");
                Sec("Cảm ơn đặc biệt", "Gia đình và bạn bè\nNhững người chơi thử\nMọi người ủng hộ tác giả\nVà bạn, người đã ghé đảo");
                c.Add(L("cr-note", "Game được làm ở Việt Nam, với tình yêu dành cho những làng quê yên bình, những buổi chiều không vội và những người đang cần một chỗ để nghỉ ngơi. Mong bạn tìm được chút bình yên ở đây."));
            }
            else
            {
                Sec("Đảo Bình Yên", $"Mã nguồn bản web công khai trên GitHub: thcphm/serene-isle (giấy phép MIT).");
                Sec("KayKit (Kay Lousberg)", "Adventurers 1.0, Character Animations 1.1 · giấy phép CC0 1.0 (phạm vi công cộng).");
                Sec("Quaternius", "Stylized Nature MegaKit, Ultimate Animated Animal Pack · giấy phép CC0 1.0 (phạm vi công cộng).");
                Sec("Unity", "Unity 6, Universal Render Pipeline, Input System, UI Toolkit · theo điều khoản của Unity Technologies.");
                c.Add(L("cr-note", "Tài sản CC0 được dùng tự do, không bắt buộc ghi công; ghi ở đây để cảm ơn các tác giả."));
            }
            var row = E(null, "menu-buttons");
            row.Add(Btn(licenses ? "Ghi công" : "Giấy phép", () => Credits(card, done, !licenses), "muted"));
            row.Add(Btn("Đóng", done));
            card.Add(row);
        }

        // ------------------------------------------------------------------ ung ho
        public static void Donate(VisualElement card, Action done)
        {
            card.Clear();
            card.Add(L("panel-title", "Ủng hộ tác giả"));
            card.Add(L("panel-sub", "Nếu bạn thích Đảo Bình Yên, một ly cà phê sẽ giúp game có thêm nhiều mùa mới."));
            var qr = E("donate-qr");
            var tex = Resources.Load<Texture2D>("UI/donate-qr");
            if (tex) qr.style.backgroundImage = new StyleBackground(tex);
            card.Add(qr);
            card.Add(L("don-text", "Mở app ngân hàng bất kỳ, quét mã VietQR để ủng hộ Thuc Pham."));
            var thanks = IconGlyph.Row(IconGlyph.Kind.Heart, "Mỗi sự ủng hộ, dù nhỏ, đều là động lực rất lớn. Cảm ơn bạn!", "don-text");
            thanks.style.flexDirection = FlexDirection.RowReverse; thanks.style.justifyContent = Justify.FlexEnd; // tim o cuoi cau
            ((VisualElement)thanks[0]).style.marginLeft = 5; ((VisualElement)thanks[0]).style.marginRight = 0;
            ((VisualElement)thanks[0]).style.color = new Color(.85f, .3f, .3f);
            card.Add(thanks);
            var row = E(null, "menu-buttons");
            row.Add(Btn("Tặng sao trên GitHub", () => Application.OpenURL(GitHubUrl), "muted"));
            row.Add(Btn("Đóng", done));
            card.Add(row);
        }
    }
}
