// Giao dien cho viec lam (giai doan 3), gan vao HUD (UI Toolkit):
//  - dong nhac phim E, chu noi "+3 Go" bay len tu the gioi (ban web: floatText),
//  - hop xac nhan di ngu, bang che tao (ban web: openPanel('bench')),
//  - thanh luc quang can, tro choi keo ca (ban web: #power, #reel).
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace SereneIsle
{
    public class ActionUI : MonoBehaviour
    {
        public static ActionUI Instance { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { Instance = null; }

        VisualElement _layer, _prompt, _floats, _dialog, _dialogBody, _dialogButtons, _craft, _craftList, _power, _powerFill, _reel, _reelZone, _reelFish, _reelProg;
        Label _promptLabel, _dialogTitle, _dialogText, _reelName, _craftSub;
        readonly System.Collections.Generic.List<IconGlyph> _reelStars = new System.Collections.Generic.List<IconGlyph>();
        bool _built;

        struct FloatText { public Label label; public Vector3 world; public float t; }
        readonly List<FloatText> _floatTexts = new List<FloatText>();

        public bool DialogOpen => _dialog != null && _dialog.style.display == DisplayStyle.Flex;
        public string DialogTitle => _dialogTitle?.text;
        public string DialogText => _dialogText?.text;
        /// <summary>Nhan cac nut cua hop thoai dang mo (test).</summary>
        public System.Collections.Generic.List<string> DialogButtons()
        {
            var l = new System.Collections.Generic.List<string>();
            if (_dialogButtons != null) foreach (var c in _dialogButtons.Children()) if (c is Button b) l.Add(b.text);
            return l;
        }
        /// <summary>Bam nut theo nhan (test).</summary>
        public bool ClickDialog(string label)
        {
            if (_dialogButtons == null) return false;
            foreach (var (l, act) in _dialogActs)
                if (l == label)
                {
                    CloseDialog();
                    act?.Invoke();
                    return true;
                }
            return false;
        }
        readonly System.Collections.Generic.List<(string, System.Action)> _dialogActs = new System.Collections.Generic.List<(string, System.Action)>();
        public bool CraftOpen => _craft != null && _craft.style.display == DisplayStyle.Flex;

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
            if (ss) hud.Root.styleSheets.Add(ss);
            _layer = E("actions", "hud-root"); // hud-root: dung chung bien mau cua HUD
            hud.Root.Add(_layer);

            _floats = E("floats");
            _layer.Add(_floats);

            // bong thoai tren dau dan lang (ban web: say(), dong 1168)
            _bubble = E("bubble", "chip");
            _bubbleName = L("bubble-name");
            _bubbleText = L("bubble-text");
            _bubble.Add(_bubbleName); _bubble.Add(_bubbleText);
            _bubble.style.display = DisplayStyle.None;
            _floats.Add(_bubble);

            _prompt = E("prompt", "chip");
            _promptLabel = L(null);
            _prompt.Add(_promptLabel);
            _prompt.style.display = DisplayStyle.None;
            _layer.Add(_prompt);

            // thanh luc quang can
            _power = E("power", "chip");
            _power.Add(L("power-label", "Giữ để lấy lực quăng"));
            var track = E(null, "power-track");
            _powerFill = E("power-fill");
            track.Add(_powerFill);
            _power.Add(track);
            _power.style.display = DisplayStyle.None;
            _layer.Add(_power);

            // keo ca
            _reel = E("reel", "chip");
            var stars = E(null, "reel-stars");
            stars.style.flexDirection = FlexDirection.Row; stars.style.justifyContent = Justify.Center;
            stars.style.color = new Color(1f, .89f, .48f);
            for (int q = 0; q < 5; q++) { var st = new IconGlyph(IconGlyph.Kind.StarOutline, 14); st.style.marginLeft = st.style.marginRight = 1; _reelStars.Add(st); stars.Add(st); }
            _reel.Add(stars);
            _reelName = L("reel-name");
            _reel.Add(_reelName);
            var row = E(null, "reel-row");
            var bar = E("reel-bar");
            _reelZone = E("reel-zone");
            _reelFish = E("reel-fish");
            bar.Add(_reelZone); bar.Add(_reelFish);
            var progTrack = E("reel-prog-track");
            _reelProg = E("reel-prog");
            progTrack.Add(_reelProg);
            row.Add(bar); row.Add(progTrack);
            _reel.Add(row);
            _reel.Add(L("reel-hint", "Giữ F / chuột trái để kéo khung lên.\nGiữ cá trong khung cho đầy thanh."));
            _reel.style.display = DisplayStyle.None;
            _layer.Add(_reel);

            // hop thoai
            _dialog = E("dialog-overlay", null, true);
            var panel = E("dialog", null, true);
            _dialogTitle = L("panel-title");
            _dialogText = L("dialog-text");
            _dialogBody = E(null);
            _dialogButtons = E("dialog-buttons");
            panel.Add(_dialogTitle); panel.Add(_dialogText); panel.Add(_dialogBody); panel.Add(_dialogButtons);
            _dialog.Add(panel);
            _dialog.style.display = DisplayStyle.None;
            _layer.Add(_dialog);

            // ban che tao
            _craft = E("craft-overlay", null, true);
            var cp = E("craft", null, true);
            cp.Add(L("panel-title", "Bàn chế tạo"));
            _craftSub = L("panel-sub", "Gỗ, đá và quặng thành đồ dùng.");
            cp.Add(_craftSub);
            var scroll = new ScrollView(ScrollViewMode.Vertical) { name = "craft-scroll" };
            _craftList = scroll.contentContainer;
            cp.Add(scroll);
            var close = new Button(CloseCrafting) { text = "Đóng (Esc)" };
            close.AddToClassList("btn");
            cp.Add(close);
            _craft.Add(cp);
            _craft.RegisterCallback<PointerDownEvent>(e => { if (e.target == _craft) CloseCrafting(); });
            _craft.style.display = DisplayStyle.None;
            _layer.Add(_craft);
        }

        void Update()
        {
            if (!_built) Build();
            if (!_built) return;
            var s = S;
            UpdateFloats();
            UpdateBubble();
            if (!s) return;

            if (CraftOpen && GameInput.CancelPressed) CloseCrafting();
            else if (DialogOpen && GameInput.CancelPressed) CloseDialog();

            var ts = ToolSystem.Instance;
            var f = ts ? ts.Fishing : null;
            bool charge = f && f.Current == Fishing.State.Charge;
            _power.style.display = charge ? DisplayStyle.Flex : DisplayStyle.None;
            if (charge)
            {
                _powerFill.style.width = Length.Percent(f.Power * 100);
                _powerFill.style.backgroundColor = f.Power > .85f ? MaterialLibrary.Hex("#e8582f") : f.Power > .5f ? MaterialLibrary.Hex("#e8b02f") : MaterialLibrary.Hex("#6cbf3f");
            }
            bool reel = f && f.Current == Fishing.State.Reel;
            _reel.style.display = reel ? DisplayStyle.Flex : DisplayStyle.None;
            if (reel)
            {
                var R = f.Reel;
                int d = Mathf.Clamp(R.d, 0, 5);
                _reelName.text = Fishing.Rarity[d];
                for (int q = 0; q < _reelStars.Count; q++) _reelStars[q].Glyph = q < d ? IconGlyph.Kind.Star : IconGlyph.Kind.StarOutline;
                _reelZone.style.height = Length.Percent(R.zone * 100);
                _reelZone.style.bottom = Length.Percent((R.pos - R.zone / 2) * 100);
                _reelZone.EnableInClassList("reel-zone--on", R.inZone);
                _reelFish.style.bottom = Length.Percent(R.fy * 100);
                float pr = Mathf.Clamp01(R.prog);
                _reelProg.style.height = Length.Percent(pr * 100);
                _reelProg.style.backgroundColor = pr > .66f ? MaterialLibrary.Hex("#6cbf3f") : pr > .33f ? MaterialLibrary.Hex("#e8b02f") : MaterialLibrary.Hex("#e8582f");
            }
        }

        // ------------------------------------------------------------------ dong nhac
        string _promptText;
        public static void Prompt(string text)
        {
            var me = Instance;
            if (!me || !me._built || me._promptText == text) return;
            me._promptText = text;
            me._prompt.style.display = text != null ? DisplayStyle.Flex : DisplayStyle.None;
            if (text != null) me._promptLabel.text = text;
        }

        // ------------------------------------------------------------------ chu noi
        public static void Float(string text, Vector3 world) => Float(text, world, new Color(1, .96f, .82f));

        public static void Float(string text, Vector3 world, Color color)
        {
            var me = Instance;
            if (!me || !me._built) return;
            var l = L("float-text", text);
            l.style.color = color;
            me._floats.Add(l);
            me._floatTexts.Add(new FloatText { label = l, world = world, t = 0 });
            me.PlaceFloat(me._floatTexts[me._floatTexts.Count - 1]);
        }

        void UpdateFloats()
        {
            for (int i = _floatTexts.Count - 1; i >= 0; i--)
            {
                var f = _floatTexts[i];
                f.t += Time.deltaTime;
                if (f.t > 1.4f) { f.label.RemoveFromHierarchy(); _floatTexts.RemoveAt(i); continue; }
                f.world += Vector3.up * Time.deltaTime * .9f;
                f.label.style.opacity = 1 - Mathf.Clamp01((f.t - .9f) / .5f);
                _floatTexts[i] = f;
                PlaceFloat(f);
            }
        }

        void PlaceFloat(FloatText f)
        {
            var cam = Camera.main;
            if (!cam || f.label.panel == null) return;
            var vp = cam.WorldToViewportPoint(f.world);
            if (vp.z < 0) { f.label.style.display = DisplayStyle.None; return; }
            f.label.style.display = DisplayStyle.Flex;
            var size = f.label.panel.visualTree.layout.size;
            f.label.style.left = vp.x * size.x;
            f.label.style.top = (1 - vp.y) * size.y;
        }

        // ------------------------------------------------------------------ bong thoai
        VisualElement _bubble;
        Label _bubbleName, _bubbleText;
        Transform _bubbleTarget;
        float _bubbleT;

        /// <summary>Hien 1 cau tren dau nhan vat trong 'secs' giay (chi 1 bong cung luc, nhu ban web).</summary>
        public static void Bubble(Transform target, string text, float secs = 4)
        {
            var me = Instance;
            if (!me || !me._built || !target) return;
            me._bubbleTarget = target;
            me._bubbleT = secs;
            var npc = target.GetComponent<Npc>();
            me._bubbleName.text = npc && npc.def != null ? npc.def.name : "";
            me._bubbleName.style.display = me._bubbleName.text.Length > 0 ? DisplayStyle.Flex : DisplayStyle.None;
            me._bubbleText.text = text;
            me.UpdateBubble();
        }

        public static void ClearBubble()
        {
            var me = Instance;
            if (me) me._bubbleT = 0;
        }

        public string BubbleText => _bubbleT > 0 && _bubbleText != null ? _bubbleText.text : null;

        void UpdateBubble()
        {
            if (_bubble == null) return;
            _bubbleT -= Time.deltaTime;
            var cam = Camera.main;
            var s = S;
            bool show = _bubbleT > 0 && _bubbleTarget && _bubbleTarget.gameObject.activeInHierarchy && cam && _bubble.panel != null
                        && !(s && s.Player && Vector3.Distance(s.Player.transform.position, _bubbleTarget.position) > 9);
            if (show)
            {
                var vp = cam.WorldToViewportPoint(_bubbleTarget.position + Vector3.up * 1.75f);
                show = vp.z > 0;
                if (show)
                {
                    var size = _bubble.panel.visualTree.layout.size;
                    _bubble.style.left = vp.x * size.x;
                    _bubble.style.top = (1 - vp.y) * size.y;
                }
            }
            _bubble.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;
        }

        // ------------------------------------------------------------------ hop thoai
        public void ShowDialog(string title, string text, params (string label, System.Action act)[] buttons)
        {
            if (!_built) return;
            _dialogTitle.text = title;
            _dialogText.text = text;
            _dialogButtons.Clear();
            // nut dai (chon cau ke trong thu...) -> xep doc, rong het hop thoai
            bool stack = System.Array.Exists(buttons, b => b.label != null && b.label.Length > 18);
            _dialogButtons.EnableInClassList("stack", stack);
            _dialogActs.Clear();
            _dialogActs.AddRange(buttons);
            foreach (var (label, act) in buttons)
            {
                var b = new Button(() => { CloseDialog(); act?.Invoke(); }) { text = label };
                b.AddToClassList("btn");
                _dialogButtons.Add(b);
            }
            _dialog.style.display = DisplayStyle.Flex;
            S.UiOpen = true;
        }

        public void CloseDialog()
        {
            if (!DialogOpen) return;
            _dialog.style.display = DisplayStyle.None;
            if (S) S.UiOpen = false;
        }

        public void AskSleep()
        {
            var s = S;
            ShowDialog("Đi ngủ?", $"Ngủ tới 7 giờ sáng ngày {s.Clock.Day + 1}. Ngủ đủ giấc hồi đầy năng lượng. Game tự lưu khi ngủ.",
                ("Ngủ", () => s.Sleep()), ("Thôi", null));
        }

        // ------------------------------------------------------------------ che tao
        public void OpenCrafting()
        {
            if (!_built) return;
            RenderCrafting();
            _craft.style.display = DisplayStyle.Flex;
            S.UiOpen = true;
        }

        public void CloseCrafting()
        {
            if (!CraftOpen) return;
            _craft.style.display = DisplayStyle.None;
            if (S) S.UiOpen = false;
        }

        void RenderCrafting()
        {
            var s = S;
            _craftList.Clear();
            foreach (var r in Recipes.All)
            {
                var row = E(null, "craft-row");
                var icon = new ItemIcon();
                icon.Set(s.Items.Get(r.id));
                row.Add(icon);
                var text = E(null, "craft-text");
                bool have = Recipes.AlreadyHave(r, s);
                string state = have ? " · đã có" : (r.tool == null && r.bag == 0 ? $" · đang có {s.Inventory.Count(r.id)}" : "");
                text.Add(L("craft-name", $"{s.Items.NameOf(r.id)}{state}"));
                text.Add(L("craft-desc", r.desc));
                text.Add(L("craft-req", Recipes.ReqText(r, s.Inventory)));
                row.Add(text);
                var recipe = r;
                var btn = new Button(() =>
                {
                    var msg = Recipes.Craft(recipe, s);
                    if (msg != null) s.Say(msg);
                    RenderCrafting();
                }) { text = have ? "Đã có" : "Làm" };
                btn.AddToClassList("btn");
                btn.AddToClassList("craft-btn");
                btn.SetEnabled(Recipes.CanCraft(r, s));
                row.Add(btn);
                _craftList.Add(row);
            }
        }
    }
}
