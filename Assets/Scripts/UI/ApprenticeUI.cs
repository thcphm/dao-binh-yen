// Bang hoc nghe voi tho ca: xem bac, vat lieu can cho bac sau, lam bai thu tay nghe (kim chay qua lai,
// bam F / Space khi kim nam trong vung xanh: trung 3 lan truoc khi truot 3 lan). Dat thi tru vat lieu, len bac.
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace SereneIsle
{
    public class ApprenticeUI : MonoBehaviour
    {
        public static ApprenticeUI Instance { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { Instance = null; }

        ScrollView _scroll;
        VisualElement _overlay, _list, _test, _track, _zone, _needle;
        Label _title, _sub, _testTitle, _testHint, _score;
        bool _built;
        string _npc;

        // bai thu
        CraftDef _craft;
        float _t, _grace, _zoneCenter, _zoneW, _speed, _flash;
        int _hits, _misses;
        readonly System.Random _rng = new System.Random();

        public bool IsOpen => _overlay != null && _overlay.style.display == DisplayStyle.Flex;
        public bool Testing => IsOpen && _craft != null;
        /// <summary>Vi tri kim 0..1 (test doc).</summary>
        public float Needle { get; private set; }
        public float ZoneCenter => _zoneCenter;

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
            var panel = E("shop", "apprentice", true);
            _title = L("panel-title");
            _sub = L("panel-sub");
            panel.Add(_title); panel.Add(_sub);
            var sv = _scroll = new ScrollView(ScrollViewMode.Vertical);
            sv.AddToClassList("apprentice-scroll");
            _list = sv.contentContainer;
            panel.Add(sv);

            // bai thu
            _test = E(null, "test");
            _testTitle = L("craft-name");
            _testHint = L("craft-desc", "Bấm F hoặc Space khi kim nằm trong vùng xanh. Trúng 3 lần là đạt, trượt 3 lần là chưa đạt.");
            _track = E("test-track");
            _zone = E("test-zone");
            _needle = E("test-needle");
            _track.Add(_zone); _track.Add(_needle);
            _score = L("test-score");
            _test.Add(_testTitle); _test.Add(_testHint); _test.Add(_track); _test.Add(_score);
            _test.style.display = DisplayStyle.None;
            panel.Add(_test);

            var close = new Button(Close) { text = "Đóng (Esc)" };
            close.AddToClassList("btn");
            panel.Add(close);
            _overlay.Add(panel);
            _overlay.style.display = DisplayStyle.None;
            hud.Root.Add(_overlay);
        }

        public void Open(string npc)
        {
            if (!_built) Build();
            if (!_built || !S) return;
            _npc = npc;
            _craft = null;
            _overlay.style.display = DisplayStyle.Flex;
            S.UiOpen = true;
            Render(null);
        }

        public void Close()
        {
            if (!IsOpen) return;
            _craft = null;
            _overlay.style.display = DisplayStyle.None;
            if (S) S.UiOpen = false;
        }

        void Render(string message)
        {
            var s = S;
            var def = NpcDefs.Get(_npc);
            _title.text = $"Học nghề với {def?.name ?? _npc}";
            _sub.text = message ?? "Mang đủ vật liệu rồi làm bài thử tay nghề để lên bậc.";
            _scroll.style.display = DisplayStyle.Flex;
            _test.style.display = DisplayStyle.None;
            _list.Clear();
            foreach (var c in Crafts.TaughtBy(_npc))
            {
                int lv = Crafts.Level(c.id);
                var card = E(null, "craft-card");
                card.Add(L("craft-name", $"{c.name} · bậc {lv}/{c.levels.Length}"));
                for (int i = 0; i < lv && i < c.levels.Length; i++) card.Add(L("req ok", $"✓ Bậc {i + 1}: {c.levels[i].unlock}"));
                if (lv >= c.levels.Length) { card.Add(L("craft-desc", "Đã thành thợ cả nghề này!")); _list.Add(card); continue; }
                var next = c.levels[lv];
                card.Add(L("craft-desc", $"Bậc {lv + 1}: {next.unlock}"));
                foreach (var r in next.req)
                {
                    var (have, kinds) = Crafts.Count(s.Inventory, s.Items, r);
                    bool ok = Crafts.Meets(s.Inventory, s.Items, r);
                    string k = r.kinds > 1 ? $", {kinds}/{r.kinds} loại" : "";
                    card.Add(L(ok ? "req ok" : "req no", $"{(ok ? "✓" : "✗")} {Crafts.ReqText(r, s.Items)} (đang có {have}{k})"));
                }
                var craft = c;
                var btn = new Button(() => StartTest(craft)) { text = "Làm bài thử tay nghề" };
                btn.AddToClassList("btn");
                btn.SetEnabled(Crafts.CanAdvance(c, lv, s.Inventory, s.Items));
                card.Add(btn);
                _list.Add(card);
            }
        }

        // ------------------------------------------------------------------ bai thu
        public void StartTest(CraftDef c)
        {
            _craft = c;
            int next = Crafts.Level(c.id) + 1;
            (_zoneW, _speed) = Crafts.TestParams(next);
            _hits = _misses = 0;
            _t = (float)_rng.NextDouble();
            _grace = .4f;
            NewZone();
            _testTitle.text = $"Bài thử {c.name} bậc {next}";
            _scroll.style.display = DisplayStyle.None;
            _test.style.display = DisplayStyle.Flex;
            UpdateTestUi();
        }

        void NewZone()
        {
            _zoneCenter = Mathf.Lerp(_zoneW / 2 + .05f, 1 - _zoneW / 2 - .05f, (float)_rng.NextDouble());
            _zone.style.left = Length.Percent((_zoneCenter - _zoneW / 2) * 100);
            _zone.style.width = Length.Percent(_zoneW * 100);
        }

        /// <summary>Bam: trung neu kim trong vung xanh. Tra ve true neu trung.</summary>
        public bool Press()
        {
            if (_craft == null) return false;
            bool hit = Mathf.Abs(Needle - _zoneCenter) <= _zoneW / 2;
            if (hit) { _hits++; NewZone(); } else _misses++;
            _flash = .25f;
            _zone.EnableInClassList("test-zone--hit", hit);
            _zone.EnableInClassList("test-zone--miss", !hit);
            UpdateTestUi();
            if (_hits >= Crafts.TestHits) Finish(true);
            else if (_misses >= Crafts.TestMisses) Finish(false);
            return hit;
        }

        void Finish(bool pass)
        {
            var s = S; var c = _craft;
            _craft = null;
            if (pass)
            {
                int lv = Crafts.Level(c.id);
                Crafts.Consume(c.levels[lv], s.Inventory, s.Items);
                Village.Instance.SetCraftLevel(c.id, lv + 1);
                string msg = $"Đạt! {c.name} lên bậc {lv + 1}: {c.levels[lv].unlock}";
                s.Say(msg);
                Render(msg);
            }
            else
            {
                s.Stats.Energy = Mathf.Max(0, s.Stats.Energy - 5);
                Render("Chưa đạt. Luyện thêm rồi thử lại nghen (vật liệu vẫn còn, mất 5 năng lượng).");
            }
        }

        void UpdateTestUi()
        {
            _score.text = $"Trúng {_hits}/{Crafts.TestHits} · trượt {_misses}/{Crafts.TestMisses}";
        }

        void Update()
        {
            if (!_built) Build();
            if (!IsOpen) return;
            if (GameInput.CancelPressed) { if (_craft != null) { _craft = null; Render(null); } else Close(); return; }
            if (_craft == null) return;
            float dt = Time.unscaledDeltaTime;
            _t += dt * _speed;
            Needle = Mathf.PingPong(_t, 1f);
            _needle.style.left = Length.Percent(Needle * 100);
            if (_flash > 0 && (_flash -= dt) <= 0) { _zone.RemoveFromClassList("test-zone--hit"); _zone.RemoveFromClassList("test-zone--miss"); }
            if (_grace > 0) { _grace -= dt; return; }
            if (GameInput.UsePressed || GameInput.JumpPressed) Press();
        }
    }
}
