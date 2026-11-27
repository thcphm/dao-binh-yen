// Menu chinh (ban web: #title dong 241-259, MENU dong 1947 / 2208, #create dong 230-240, 'islands' / 'wipe' dong 1564-1565).
// Nen la chinh hon dao (o luu gan nhat) luc hoang hon, camera luon quanh lang (ban kinh 34, cao 16).
// Tao nhan vat: ten 1-16 ky tu, Nam / Nu, mau ao, quan, giay, da, toc, ao dai tay, quan short; xem truoc 3D tren dao.
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using static SereneIsle.MenuPanels;

namespace SereneIsle
{
    public class MainMenu : MonoBehaviour
    {
        public static MainMenu Instance { get; private set; }
        public int BackgroundSeed { get; set; }
        public bool IsOpen => _root != null && _root.parent != null;
        public string View { get; private set; } = "title";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { Instance = null; }

        VisualElement _root, _title, _motes, _overlay, _card;
        readonly List<(VisualElement e, float x, float d, float w, float s)> _moteList = new List<(VisualElement, float, float, float, float)>();
        float _a, _t;
        bool _built;

        // tao nhan vat
        int _createSlot;
        CharacterOutfit _outfit;
        string _name = "Minh";
        GameObject _preview;
        TextField _nameField;
        Label _err;

        GameSession S => GameSession.Instance;
        static GameBootstrap B => GameBootstrap.Instance;

        void Awake() { Instance = this; }
        void OnDestroy() { if (Instance == this) Instance = null; if (_preview) Destroy(_preview); }

        // ================================================================== dung
        void Build()
        {
            var hud = GameHud.Instance;
            if (!hud || hud.Root == null) return;
            _built = true;
            foreach (var n in new[] { "UI/Hud", "UI/Menu", "UI/Actions" })
            {
                var ss = Resources.Load<StyleSheet>(n);
                if (ss && !hud.Root.styleSheets.Contains(ss)) hud.Root.styleSheets.Add(ss);
            }
            _root = E("menu-root", "hud-root", true);
            hud.Root.Add(_root);

            var glow = E("title-glow");
            glow.style.backgroundImage = new StyleBackground(GlowTexture());
            _root.Add(glow);
            _motes = E("motes");
            _root.Add(_motes);
            // 22 dom dom (vi tri, thoi gian bay, tre, co - giong ban web)
            float[,] m = { { 0, 6, 0, 3 }, { 53, 13, 11, 4 }, { 6, 11, 10, 5 }, { 59, 9, 9, 6 }, { 12, 7, 8, 3 }, { 65, 14, 7, 4 }, { 18, 12, 6, 5 }, { 71, 10, 5, 6 },
                           { 24, 8, 4, 3 }, { 77, 6, 3, 4 }, { 30, 13, 2, 5 }, { 83, 11, 1, 6 }, { 36, 9, 0, 3 }, { 89, 7, 11, 4 }, { 42, 14, 10, 5 }, { 95, 12, 9, 6 },
                           { 48, 10, 8, 3 }, { 1, 8, 7, 4 }, { 54, 6, 6, 5 }, { 7, 13, 5, 6 }, { 60, 11, 4, 3 }, { 13, 9, 3, 4 } };
            for (int i = 0; i < m.GetLength(0); i++)
            {
                var e = E(null, "mote");
                e.style.width = e.style.height = m[i, 3] * 1.4f;
                _motes.Add(e);
                _moteList.Add((e, m[i, 0], m[i, 1], m[i, 2], m[i, 3]));
            }
            _title = E(null, "tcol");
            _root.Add(_title);
            var foot = E(null, "tfoot");
            foot.Add(L(null, "Thuc Pham · 2026"));
            foot.Add(L(null, "Phiên bản Unity · lưu trên máy"));
            _root.Add(foot);
            ShowTitle();
            if (S) S.UiOpen = true;
        }

        static Texture2D GlowTexture()
        {
            var t = new Texture2D(64, 1, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            for (int x = 0; x < 64; x++)
            {
                float a = Mathf.Pow(1 - x / 63f, 1.3f) * .62f;
                t.SetPixel(x, 0, new Color(.16f, .07f, .03f, a));
            }
            t.Apply();
            return t;
        }

        /// <summary>Ky tu bieu tuong cu -> bieu tuong ve (font khong co cac ky tu nay).</summary>
        static IconGlyph.Kind GlyphOf(string icon) => icon switch
        {
            "▶" => IconGlyph.Kind.Play, "✿" => IconGlyph.Kind.Flower, "◆" => IconGlyph.Kind.Diamond, "⚙" => IconGlyph.Kind.Gear,
            "✦" => IconGlyph.Kind.Sparkle, "♥" => IconGlyph.Kind.Heart, _ => IconGlyph.Kind.Close,
        };

        VisualElement MenuButton(string icon, string title, string sub, System.Action act, string cls = null)
        {
            var b = E(null, "mb" + (cls != null ? " " + cls : ""), true);
            var ic = E(null, "mb-icon");
            ic.Add(new IconGlyph(GlyphOf(icon), 20));
            b.Add(ic);
            var txt = E();
            txt.Add(L("mb-title", title));
            if (!string.IsNullOrEmpty(sub)) txt.Add(L("mb-sub", sub));
            b.Add(txt);
            b.RegisterCallback<ClickEvent>(_ => act());
            b.name = "mb-" + title;
            return b;
        }

        // ================================================================== man hinh tieu de
        public void ShowTitle()
        {
            View = "title";
            CloseOverlay();
            if (_preview) Destroy(_preview);
            _title.Clear();
            _title.style.display = DisplayStyle.Flex;
            _motes.style.display = DisplayStyle.Flex;
            _title.Add(L("tbadge", "Một hành trình chữa lành"));
            var name = E(); name.style.flexDirection = FlexDirection.Row;
            var dao = L("tname tname-dao", "Đảo");
            name.Add(dao); name.Add(L("tname", "Bình Yên"));
            _title.Add(name);
            _title.Add(L("ten", "SERENE  ISLE"));
            _title.Add(L("ttag", "Bỏ lại thành phố ồn ào và những deadline không hồi kết. Một hòn đảo đất đỏ, một ông lão, và nhịp sống chậm rãi đang chờ bạn."));

            int recent = SaveSystem.MostRecent();
            int count = 0;
            for (int i = 0; i < SaveSystem.Slots; i++) if (SaveSystem.Exists(i)) count++;
            if (recent >= 0)
            {
                var info = SaveSystem.Info(recent);
                _title.Add(MenuButton("▶", "Tiếp tục", $"{info.name} · {info.dateLabel}", () => B.Launch(recent, false, null, null), "main"));
            }
            _title.Add(MenuButton("✿", "Đảo mới", "Bắt đầu câu chuyện", NewIsland, recent >= 0 ? null : "main"));
            if (count > 0) _title.Add(MenuButton("◆", "Các hòn đảo", $"{count}/{SaveSystem.Slots} đảo", ShowIslands));
            _title.Add(MenuButton("⚙", "Cài đặt", null, () => Panel(c => Settings(c, ShowTitle))));
            _title.Add(MenuButton("✦", "Ghi công", null, () => Panel(c => Credits(c, ShowTitle))));
            _title.Add(MenuButton("♥", "Ủng hộ tác giả", null, () => Panel(c => Donate(c, ShowTitle)), "don"));
            if (!Application.isEditor && Application.platform != RuntimePlatform.WebGLPlayer) _title.Add(MenuButton("✕", "Thoát", null, Application.Quit));
        }

        void Panel(System.Action<VisualElement> fill, string cls = null)
        {
            CloseOverlay();
            _overlay = Overlay(_root, out _card, ShowTitle, cls);
            fill(_card);
        }

        void CloseOverlay()
        {
            _overlay?.RemoveFromHierarchy();
            _overlay = null;
        }

        int FreeSlot()
        {
            for (int i = 0; i < SaveSystem.Slots; i++) if (!SaveSystem.Exists(i)) return i;
            return -1;
        }

        void NewIsland()
        {
            int slot = FreeSlot();
            if (slot < 0) { ShowIslands(); S?.Say($"Đã đủ {SaveSystem.Slots} hòn đảo. Xoá bớt một đảo để tạo đảo mới."); return; }
            ShowCreate(slot);
        }

        // ================================================================== cac hon dao (o luu)
        public void ShowIslands()
        {
            View = "islands";
            Panel(card =>
            {
                card.Add(L("panel-title", "Các hòn đảo của bạn"));
                card.Add(L("panel-sub", $"Lưu được tối đa {SaveSystem.Slots} hòn đảo, mỗi đảo một bản đồ riêng."));
                for (int i = 0; i < SaveSystem.Slots; i++)
                {
                    var info = SaveSystem.Info(i);
                    int slot = i;
                    var row = E(null, "isl" + (info.exists ? "" : " empty"), true);
                    row.Add(L("isl-badge", info.exists ? (info.model == "Rogue" ? "Nữ" : "Nam") : "~"));
                    var txt = E(null, "isl-text");
                    if (info.exists)
                    {
                        txt.Add(L("isl-name", $"Đảo {i + 1} · {info.name}"));
                        txt.Add(L("isl-info", $"{info.dateLabel} · {info.coins:#,0} xu · chơi {info.playTime} · lưu lúc {info.savedAt}".Replace(',', '.')));
                        row.Add(txt);
                        row.Add(Btn("Chơi", () => B.Launch(slot, false, null, null)));
                        row.Add(Btn("Xoá", () => ConfirmDelete(slot, info), "danger"));
                    }
                    else
                    {
                        txt.Add(L("isl-name", "Ô trống"));
                        txt.Add(L("isl-info", $"Đảo {i + 1}"));
                        row.Add(txt);
                        row.Add(Btn("Tạo đảo", () => ShowCreate(slot)));
                    }
                    card.Add(row);
                }
                var r = E(null, "menu-buttons");
                r.Add(Btn("Quay lại", ShowTitle, "muted"));
                card.Add(r);
            }, "wide");
        }

        void ConfirmDelete(int slot, SlotInfo info)
        {
            Panel(card =>
            {
                card.Add(L("panel-title", "Xoá hòn đảo này?"));
                card.Add(L("panel-sub", $"Toàn bộ tiến trình của {info.name} ({info.dateLabel}, {info.coins} xu, nhà cửa, vật nuôi, bộ sưu tập) sẽ bị xoá khỏi máy. Không thể hoàn tác."));
                var r = E(null, "menu-buttons");
                r.Add(Btn("Giữ lại", ShowIslands));
                r.Add(Btn("Xoá vĩnh viễn", () => { SaveSystem.Delete(slot); ShowIslands(); }, "danger"));
                card.Add(r);
            });
        }

        // ================================================================== tao nhan vat
        public void ShowCreate(int slot)
        {
            View = "create";
            _createSlot = slot;
            CloseOverlay();
            _title.style.display = DisplayStyle.None;
            _motes.style.display = DisplayStyle.None;
            _outfit ??= CharacterOutfit.Male();
            _overlay = E(null, "hud-root", true);
            _overlay.style.position = Position.Absolute;
            _overlay.style.left = 0; _overlay.style.right = 0; _overlay.style.top = 0; _overlay.style.bottom = 0;
            _card = E(null, "menu-card create-card", true);
            _overlay.Add(_card);
            _root.Add(_overlay);
            RenderCreate();
            SpawnPreview();
        }

        static readonly Dictionary<string, string> DefaultName = new Dictionary<string, string> { ["Ranger"] = "Minh", ["Rogue"] = "Lan" };

        void RenderCreate()
        {
            var c = _card;
            c.Clear();
            c.Add(L("panel-title", "Bạn là ai?"));
            c.Add(L("panel-sub", "Chọn nhân vật và đặt tên trước khi lên đường ra đảo."));

            var seg = E(null, "seg");
            bool male = _outfit.model != "Rogue";
            seg.Add(Btn("Nam", () => SetModel(false), male ? null : "off"));
            seg.Add(Btn("Nữ", () => SetModel(true), male ? "off" : null));
            var segRow = E(null, "create-row"); segRow.Add(L("create-label", "Nhân vật")); segRow.Add(seg);
            c.Add(segRow);

            var nameRow = E(null, "create-row");
            nameRow.Add(L("create-label", "Tên"));
            _nameField = new TextField { value = _name, maxLength = 16 };
            _nameField.AddToClassList("name-field");
            _nameField.RegisterValueChangedCallback(e => { _name = e.newValue; if (_err != null) _err.style.display = DisplayStyle.None; });
            nameRow.Add(_nameField);
            c.Add(nameRow);
            _err = L("err", "Hãy nhập tên (1–16 ký tự).");
            _err.style.display = DisplayStyle.None;
            c.Add(_err);

            Swatches(c, "Áo", CharacterOutfit.ShirtColors, () => _outfit.shirt, v => _outfit.shirt = v);
            Swatches(c, "Quần", CharacterOutfit.PantsColors, () => _outfit.pants, v => _outfit.pants = v);
            Swatches(c, "Giày dép", CharacterOutfit.ShoeColors, () => _outfit.shoe, v => _outfit.shoe = v);
            Swatches(c, "Màu da", CharacterOutfit.SkinColors, () => _outfit.skin, v => _outfit.skin = v);
            Swatches(c, "Tóc", CharacterOutfit.HairColors, () => _outfit.hair, v => _outfit.hair = v);
            var tog = E(null, "seg");
            tog.Add(Btn("Áo dài tay", () => { _outfit.longSleeve = !_outfit.longSleeve; ApplyPreview(true); }, _outfit.longSleeve ? null : "off"));
            tog.Add(Btn("Quần short", () => { _outfit.shorts = !_outfit.shorts; ApplyPreview(true); }, _outfit.shorts ? null : "off"));
            var togRow = E(null, "create-row"); togRow.Add(L("create-label", "Kiểu")); togRow.Add(tog);
            c.Add(togRow);

            var r = E(null, "menu-buttons");
            r.Add(Btn("Quay lại", ShowTitle, "muted"));
            r.Add(Btn("Lên đường →", Go));
            c.Add(r);
        }

        void Swatches(VisualElement c, string label, string[] colors, System.Func<string> get, System.Action<string> set)
        {
            var row = E(null, "create-row");
            row.Add(L("create-label", label));
            foreach (var hex in colors)
            {
                var sw = E(null, "swatch" + (get() == hex ? " on" : ""), true);
                sw.style.backgroundColor = MaterialLibrary.Hex(hex);
                string h = hex;
                sw.RegisterCallback<ClickEvent>(_ => { set(h); ApplyPreview(true); });
                row.Add(sw);
            }
            c.Add(row);
        }

        public void SetModel(bool female)
        {
            string model = female ? "Rogue" : "Ranger";
            if (_outfit.model == model) return;
            bool defaultName = string.IsNullOrWhiteSpace(_name) || DefaultName.ContainsValue(_name);
            _outfit = female ? CharacterOutfit.Female() : CharacterOutfit.Male();
            if (defaultName) _name = DefaultName[model];
            SpawnPreview();
            RenderCreate();
        }

        public void SetOutfit(CharacterOutfit o) { _outfit = o.Clone(); SpawnPreview(); RenderCreate(); }

        void ApplyPreview(bool rerender)
        {
            if (_preview) _preview.GetComponent<IslandCharacter>().ApplyOutfit(_outfit);
            if (rerender) RenderCreate();
        }

        void SpawnPreview()
        {
            if (_preview) Destroy(_preview);
            var d = IslandTerrain.Instance ? IslandTerrain.Instance.Data : null;
            if (d == null) return;
            var go = new GameObject("CreatePreview");
            go.SetActive(false);
            var body = go.AddComponent<IslandCharacter>();
            body.outfit = _outfit.Clone();
            go.SetActive(true);
            var p = d.TownPoint(-1, -11); // bai co phia sau lang, nhin ra lang va bien
            go.transform.position = new Vector3(p.x, d.GroundHeight(p.x, p.y), p.y);
            _preview = go;
        }

        /// <summary>Ten hop le: bo ky tu &lt;&gt;&amp;", cat khoang trang, 1-16 ky tu.</summary>
        public static string CleanName(string n)
        {
            if (n == null) return "";
            n = n.Replace("<", "").Replace(">", "").Replace("&", "").Replace("\"", "").Trim();
            return n.Length > 16 ? n.Substring(0, 16) : n;
        }

        public void SetName(string n)
        {
            _name = n;
            if (_nameField != null) _nameField.SetValueWithoutNotify(n);
            if (_err != null) _err.style.display = DisplayStyle.None;
        }

        public void Go()
        {
            string n = CleanName(_name);
            if (n.Length == 0) { _err.style.display = DisplayStyle.Flex; return; }
            var outfit = _outfit.Clone();
            if (_preview) Destroy(_preview);
            int slot = _createSlot;
            Close();
            B.Launch(slot, true, outfit, n);
        }

        public void Close()
        {
            _root?.RemoveFromHierarchy();
            if (_preview) Destroy(_preview);
            if (S) S.UiOpen = false;
            enabled = false;
        }

        // ================================================================== moi khung
        void Update()
        {
            if (!_built) Build();
            if (!_built || !IsOpen) return;
            var s = S;
            var d = IslandTerrain.Instance && IslandTerrain.Instance.IsReady ? IslandTerrain.Instance.Data : null;
            float dt = Time.unscaledDeltaTime;
            _t += dt;
            if (s) s.Clock.Minutes = 17.55f * 60; // hoang hon (ban web: G.time = 17.55*60)

            // dom dom bay len
            foreach (var (e, x, dur, w, sz) in _moteList)
            {
                float k = Mathf.Repeat((_t + w) / dur, 1f);
                e.style.left = Length.Percent(x);
                e.style.top = Length.Percent(100 - k * 110);
                e.style.opacity = Mathf.Sin(k * Mathf.PI) * .85f;
                e.style.translate = new Translate(Mathf.Sin((_t + w) * 1.3f) * 14, 0);
            }

            var cam = Camera.main;
            if (!cam || d == null) return;
            if (View == "create" && _preview)
            {
                // xem truoc: nhan vat ben trai man hinh, camera truoc mat
                var p = _preview.transform.position;
                float face = d.TownAngle + Mathf.PI * .85f;
                var fwd = new Vector3(Mathf.Sin(face), 0, Mathf.Cos(face));
                // ben phai man hinh (camera nhin nguoc fwd) -> lech khung sang phai de nhan vat nam ben trai, khong bi bang che
                var right = new Vector3(-fwd.z, 0, fwd.x);
                _preview.transform.rotation = Quaternion.Euler(0, (face + .5f + Mathf.Sin(_t / 1.4f) * .6f) * Mathf.Rad2Deg, 0);
                var body = _preview.GetComponent<IslandCharacter>();
                body.Pose(dt, new PoseInput { time = _t });
                var target = p + Vector3.up * .75f + right * 1.35f;
                cam.transform.position = p + fwd * 4.2f + Vector3.up * 1.25f + right * 1.35f;
                cam.transform.LookAt(target);
                return;
            }
            // bay quanh lang (ban web: MENU.a += dt*.035, ban kinh 34 quanh townPt(0,8), cao 16 + sin(a*.7)*2)
            _a += dt * .035f;
            var c = d.TownPoint(0, 8);
            var center = new Vector3(c.x, 0, c.y);
            cam.transform.position = center + new Vector3(Mathf.Sin(_a) * 34, 16 + Mathf.Sin(_a * .7f) * 2, Mathf.Cos(_a) * 34);
            cam.transform.LookAt(center + Vector3.up * 1.2f);
        }
    }
}
