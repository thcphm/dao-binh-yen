// Mo dau van moi (port ban web: SLIDES / introStart, dong 2155-2185): 5 canh ke vi sao nhan vat ra dao.
// 3 canh dau ve 2D (thanh pho mua, van phong dem, can ho), canh 4 nhin dao tu tren cao qua khung dien thoai,
// canh 5 ghe tien vao cau tau luc binh minh. Chu chay tung ky tu; bam chuot / Space / E / Enter: hien het chu hoac
// sang canh; Esc hoac nut "Bo qua": vao game ngay.
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace SereneIsle
{
    public class IntroUI : MonoBehaviour
    {
        public static IntroUI Instance { get; private set; }
        public static bool Playing => Instance && Instance._on;

        sealed class Slide { public string label, text, world; public Action<VisualElement> paint; }

        static readonly Slide[] Slides =
        {
            new Slide { label = "Thành phố · 6:47", paint = PaintCity,
                text = "Chuông báo thức reo lần thứ ba. Ngoài kia mưa vẫn rơi, xe cộ vẫn kẹt cứng như mọi ngày." },
            new Slide { label = "Văn phòng · 23:47", paint = PaintOffice,
                text = "Email, deadline, họp hành. Mười bốn tháng liền chưa nghỉ một ngày. Sếp vẫn nói: \"Cố thêm chút nữa thôi.\"" },
            new Slide { label = "Căn hộ · 1:20 sáng", paint = PaintRoom,
                text = "Về tới nhà chỉ để ngủ. Sáng mai lại như vậy. Cậu không còn nhớ lần cuối mình thật sự thấy vui là khi nào." },
            new Slide { label = "Một tấm ảnh", world = "aerial",
                text = "Một đêm mất ngủ, cậu lướt thấy tấm ảnh một hòn đảo xa: đất đỏ, biển xanh, rừng dừa bạt ngàn. Nghe nói ở đó chỉ có một ông lão sống." },
            new Slide { label = "Trên biển · bình minh", world = "boat",
                text = "Sáng hôm sau, cậu nộp đơn nghỉ việc, bán gần hết đồ đạc, mua một vé tàu ra đảo. Lần đầu tiên sau nhiều năm, lòng cậu thấy nhẹ tênh." },
        };

        bool _on;
        int _i;
        float _t, _shown;
        string _text;
        Action _done;
        VisualElement _root, _scene, _black, _phone;
        Label _label, _textLabel, _hint;
        sealed class Drop { public VisualElement el; public float speed, y; }
        readonly List<Drop> _rain = new List<Drop>();
        OrbitCamera _rig;
        Transform _boat;
        Vector3 _boatFrom, _boatDir;
        float _savedMinutes;
        string _pronoun = "cậu";

        GameSession S => GameSession.Instance;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { Instance = null; }

        void Awake() { Instance = this; }
        void OnDestroy() { if (Instance == this) Instance = null; }

        /// <summary>Chay mo dau roi goi done. female: doi "cau" thanh "co".</summary>
        public void Play(bool female, Action done)
        {
            var hud = GameHud.Instance;
            if (!hud || hud.Root == null) { done?.Invoke(); return; }
            _done = done;
            _pronoun = female ? "cô" : "cậu";
            _on = true;
            if (S) S.UiOpen = true;
            var cam = Camera.main;
            _rig = cam ? cam.GetComponent<OrbitCamera>() : null;
            if (_rig) _rig.enabled = false;
            if (S) _savedMinutes = S.Clock.Minutes;
            if (S && S.Body) S.Body.gameObject.SetActive(false);
            Build(hud.Root);
            ShowSlide(0);
        }

        // ------------------------------------------------------------------ giao dien
        void Build(VisualElement parent)
        {
            _root = new VisualElement { name = "intro", pickingMode = PickingMode.Position };
            _root.AddToClassList("intro");
            _scene = new VisualElement { pickingMode = PickingMode.Ignore };
            _scene.AddToClassList("intro-scene");
            _root.Add(_scene);
            // canh 4: khung dien thoai o giua, 4 dai toi xung quanh (UITK khong co box-shadow nhu ban web)
            _phone = new VisualElement { pickingMode = PickingMode.Ignore };
            _phone.style.position = Position.Absolute;
            _phone.style.left = 0; _phone.style.top = 0; _phone.style.right = 0; _phone.style.bottom = 0;
            _phone.style.flexDirection = FlexDirection.Row;
            VisualElement Dim() { var e = new VisualElement { pickingMode = PickingMode.Ignore }; e.style.flexGrow = 1; e.style.backgroundColor = new Color(.024f, .03f, .047f, .88f); return e; }
            _phone.Add(Dim());
            var col = new VisualElement { pickingMode = PickingMode.Ignore }; col.style.width = 300;
            col.Add(Dim());
            var frame = new VisualElement { pickingMode = PickingMode.Ignore };
            frame.AddToClassList("intro-phone");
            col.Add(frame);
            col.Add(Dim());
            _phone.Add(col);
            _phone.Add(Dim());
            _root.Add(_phone);
            var top = new VisualElement(); top.AddToClassList("intro-bar"); top.AddToClassList("top"); _root.Add(top);
            var bot = new VisualElement(); bot.AddToClassList("intro-bar"); bot.AddToClassList("bot"); _root.Add(bot);
            _black = new VisualElement { pickingMode = PickingMode.Ignore };
            _black.AddToClassList("intro-black");
            _root.Add(_black);
            var cap = new VisualElement(); cap.AddToClassList("intro-cap");
            _label = new Label(); _label.AddToClassList("intro-label");
            _textLabel = new Label(); _textLabel.AddToClassList("intro-text");
            cap.Add(_label); cap.Add(_textLabel);
            _root.Add(cap);
            _hint = new Label("Bấm để tiếp tục ▸"); _hint.AddToClassList("intro-hint");
            _root.Add(_hint);
            var skip = new Button(End) { text = "Bỏ qua" };
            skip.AddToClassList("intro-skip");
            _root.Add(skip);
            _root.RegisterCallback<PointerDownEvent>(e => { if (e.target != skip) Next(); });
            var ss = Resources.Load<StyleSheet>("UI/Intro");
            if (ss) _root.styleSheets.Add(ss);
            parent.Add(_root);
        }

        void ShowSlide(int i)
        {
            _i = i; _t = 0; _shown = 0;
            var s = Slides[i];
            _text = s.text.Replace("cậu", _pronoun).Replace("Cậu", char.ToUpper(_pronoun[0]) + _pronoun.Substring(1));
            _label.text = s.label;
            _textLabel.text = "";
            _scene.Clear(); _rain.Clear();
            s.paint?.Invoke(_scene);
            if (s.paint == PaintCity) AddRain(70);
            _scene.style.display = s.world == null ? DisplayStyle.Flex : DisplayStyle.None;
            _phone.style.display = s.world == "aerial" ? DisplayStyle.Flex : DisplayStyle.None;
            Aerial(s.world == "aerial");
            if (s.world == "aerial" && S) S.Clock.Minutes = 16.4f * 60;
            if (s.world == "boat") StartBoat();
        }

        void Next()
        {
            if (!_on) return;
            if (_shown < _text.Length) { _shown = _text.Length; _textLabel.text = _text; return; }
            GameAudio.Play(Sfx.Pop, .5f);
            if (_i < Slides.Length - 1) ShowSlide(_i + 1);
            else End();
        }

        public void End()
        {
            if (!_on) return;
            _on = false;
            if (_boat) Destroy(_boat.gameObject);
            Aerial(false);
            _root?.RemoveFromHierarchy();
            if (S)
            {
                S.Clock.Minutes = _savedMinutes;
                if (S.Body) S.Body.gameObject.SetActive(true);
                S.UiOpen = false;
            }
            if (_rig) _rig.enabled = true;
            _done?.Invoke();
        }

        void Update()
        {
            if (!_on) return;
            float dt = Time.unscaledDeltaTime;
            _t += dt;
            _black.style.opacity = Mathf.Max(0, 1 - _t / 1.1f);
            if (_t > .6f && _shown < _text.Length)
            {
                _shown = Mathf.Min(_text.Length, _shown + dt * 42);
                _textLabel.text = _text.Substring(0, (int)_shown);
            }
            _hint.style.opacity = _shown >= _text.Length ? 1 : 0;
            var kb = Keyboard.current;
            if (kb != null)
            {
                if (kb.escapeKey.wasPressedThisFrame) { End(); return; }
                if (kb.spaceKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame || kb.eKey.wasPressedThisFrame) Next();
            }
            // mua roi
            float h = _scene.layout.height > 0 ? _scene.layout.height : 900;
            foreach (var r in _rain)
            {
                r.y = (r.y + r.speed * dt) % (h + 40);
                r.el.style.top = r.y - 40;
            }
            UpdateWorld(dt);
        }

        // ------------------------------------------------------------------ canh 3D: dao tu tren cao, ghe vao bo
        void UpdateWorld(float dt)
        {
            var cam = Camera.main;
            var d = IslandTerrain.Instance ? IslandTerrain.Instance.Data : null;
            if (!cam || d == null) return;
            var w = Slides[_i].world;
            if (w == "aerial")
            {
                float a = .6f + _t * .05f;
                var c = new Vector3(d.N / 2f, 0, d.N / 2f);
                cam.transform.position = c + new Vector3(Mathf.Sin(a) * d.N * .42f, d.N * .3f, Mathf.Cos(a) * d.N * .42f);
                cam.transform.LookAt(c + Vector3.up * 2);
            }
            else if (w == "boat" && _boat)
            {
                var p = _boatFrom + _boatDir * Mathf.Min(_t * 2.2f, 52);
                p.y = d.WaterLevel - .05f + Mathf.Sin(_t * 1.5f) * .06f;
                _boat.position = p;
                _boat.rotation = Quaternion.LookRotation(_boatDir) * Quaternion.Euler(0, 0, Mathf.Sin(_t * 1.1f) * 2.3f);
                var side = new Vector3(_boatDir.z, 0, -_boatDir.x);
                cam.transform.position = p - _boatDir * 4.5f + side * 4f + Vector3.up * 2.6f;
                cam.transform.LookAt(p + _boatDir * 6 + Vector3.up * 1.2f);
            }
        }

        IslandEnvironment _env; float _fogN, _fogF, _far; bool _wide;

        /// <summary>Canh nhin dao tu tren cao: tam noi suong mu + tam ve xa (ban web: fog 260..700).</summary>
        void Aerial(bool on)
        {
            var cam = Camera.main;
            if (on && !_wide)
            {
                _env = FindAnyObjectByType<IslandEnvironment>();
                if (_env) { _fogN = _env.fogNear; _fogF = _env.fogFar; _env.fogNear = 260; _env.fogFar = 760; }
                if (cam) { _far = cam.farClipPlane; cam.farClipPlane = 1400; }
                _wide = true;
            }
            else if (!on && _wide)
            {
                if (_env) { _env.fogNear = _fogN; _env.fogFar = _fogF; }
                if (cam) cam.farClipPlane = _far;
                _wide = false;
            }
        }

        void StartBoat()
        {
            var d = IslandTerrain.Instance ? IslandTerrain.Instance.Data : null;
            if (d == null) return;
            if (S) S.Clock.Minutes = 6.6f * 60;
            var land = d.ArrivalPoint(out float facing);
            var into = new Vector3(Mathf.Sin(facing), 0, Mathf.Cos(facing));   // huong tu cau tau vao lang
            _boatDir = into;
            _boatFrom = land - into * 58;
            var src = Village.Instance ? Village.Instance.Boat : null;
            if (src) _boat = Instantiate(src.gameObject).transform;
            else
            {
                var b = new MeshBuilder();
                b.Box("#8a5a2e", 0, .15f, 0, 1.1f, .3f, 2.4f);
                b.Box("#6b4423", 0, .32f, 0, 1f, .06f, .2f);
                var go = new GameObject("IntroBoat");
                go.AddComponent<MeshFilter>().sharedMesh = b.ToMesh("IntroBoat");
                go.AddComponent<MeshRenderer>().sharedMaterial = MaterialLibrary.Objects(false);
                _boat = go.transform;
            }
            _boat.name = "IntroBoat";
        }

        // ------------------------------------------------------------------ canh 2D
        static VisualElement Box(VisualElement p, float l, float t, float w, float h, string hex, float radius = 0)
        {
            var e = new VisualElement { pickingMode = PickingMode.Ignore };
            e.style.position = Position.Absolute;
            e.style.left = Length.Percent(l); e.style.top = Length.Percent(t);
            e.style.width = Length.Percent(w); e.style.height = Length.Percent(h);
            e.style.backgroundColor = MaterialLibrary.Hex(hex);
            if (radius > 0) { e.style.borderTopLeftRadius = e.style.borderTopRightRadius = e.style.borderBottomLeftRadius = e.style.borderBottomRightRadius = radius; }
            p.Add(e);
            return e;
        }

        static void Sky(VisualElement p, string top, string bottom)
        {
            for (int i = 0; i < 10; i++) Box(p, 0, i * 10, 100, 10.5f, "#" + ColorUtility.ToHtmlStringRGB(Color.Lerp(MaterialLibrary.Hex(top), MaterialLibrary.Hex(bottom), i / 9f)));
        }

        static void PaintCity(VisualElement p)
        {
            Sky(p, "#3a4656", "#6a7686");
            var rng = new System.Random(4);
            float x = -2;
            while (x < 100)
            {
                float w = 6 + (float)rng.NextDouble() * 9, h = 30 + (float)rng.NextDouble() * 45;
                string[] tone = { "#2a3240", "#323b4a", "#262d38" };
                Box(p, x, 100 - h, w, h, tone[rng.Next(3)]);
                for (float wy = 100 - h + 3; wy < 94; wy += 5)
                    for (float wx = x + 1.2f; wx < x + w - 1.5f; wx += 2.6f)
                        if (rng.NextDouble() < .35) Box(p, wx, wy, 1.2f, 2.2f, rng.NextDouble() < .7 ? "#e8c070" : "#8aa0b8");
                x += w + .6f;
            }
            Box(p, 0, 93, 100, 7, "#1e232b");
            for (int i = 0; i < 9; i++) Box(p, 4 + i * 11.5f, 95.5f, 5, 1.2f, i % 2 == 0 ? "#d84a3a" : "#f0d080", 2);   // den xe ket
        }

        static void PaintOffice(VisualElement p)
        {
            Sky(p, "#141820", "#1e2430");
            for (int i = 0; i < 4; i++) Box(p, 8 + i * 22, 12, 16, 34, "#0e1a2c");          // cua kinh, thanh pho dem
            for (int i = 0; i < 30; i++) Box(p, 9 + (i * 37 % 86), 16 + (i * 13 % 26), .7f, 1.2f, "#c8a050");
            Box(p, 0, 62, 100, 38, "#2a2420");                                                  // ban
            Box(p, 36, 30, 28, 26, "#0c0f14", 6);                                               // man hinh
            Box(p, 37.5f, 32, 25, 22, "#7aa8d8", 4).style.opacity = .85f;
            for (int i = 0; i < 6; i++) Box(p, 40, 35 + i * 3, 10 + (i * 7 % 12), 1.2f, "#e8f0f8");
            Box(p, 47, 56, 6, 6, "#0c0f14");
            Box(p, 18, 64, 12, 7, "#e8e0d0", 2); Box(p, 70, 63, 8, 9, "#8a5a3a", 3);           // giay to, ly ca phe
            var clock = new Label("23:47");
            clock.style.position = Position.Absolute; clock.style.left = Length.Percent(78); clock.style.top = Length.Percent(18);
            clock.style.fontSize = 34; clock.style.color = MaterialLibrary.Hex("#e86a4a");
            p.Add(clock);
        }

        static void PaintRoom(VisualElement p)
        {
            Sky(p, "#0e1424", "#18203a");
            Box(p, 60, 14, 26, 34, "#22305a", 4);                                               // cua so
            Box(p, 75, 19, 5, 7, "#f0ecd0", 30);                                                // trang
            Box(p, 72.5f, 14, 1.2f, 34, "#0e1424"); Box(p, 60, 30, 26, 1.2f, "#0e1424");
            Box(p, 0, 66, 100, 34, "#1a1e2c");                                                  // giuong
            Box(p, 8, 58, 60, 14, "#2e3650", 6);
            Box(p, 12, 54, 16, 8, "#e8e4f0", 8);                                                // goi
            var glow = Box(p, 36, 46, 16, 16, "#9ab8ff", 60); glow.style.opacity = .35f;       // anh sang dien thoai
            Box(p, 42, 52, 4, 6, "#bcd4ff", 2);
            Box(p, 86, 52, 6, 10, "#ffb070", 3).style.opacity = .7f;                            // den ngu
        }

        void AddRain(int n)
        {
            var rng = new System.Random(9);
            for (int i = 0; i < n; i++)
            {
                var e = new VisualElement { pickingMode = PickingMode.Ignore };
                e.style.position = Position.Absolute;
                float x = (float)rng.NextDouble() * 100;
                e.style.left = Length.Percent(x);
                e.style.width = 1.5f; e.style.height = 26;
                e.style.backgroundColor = new Color(.75f, .82f, .9f, .45f);
                e.style.rotate = new Rotate(Angle.Degrees(12));
                _scene.Add(e);
                _rain.Add(new Drop { el = e, speed = 520 + (float)rng.NextDouble() * 260, y = (float)rng.NextDouble() * 900 });
            }
        }
    }
}
