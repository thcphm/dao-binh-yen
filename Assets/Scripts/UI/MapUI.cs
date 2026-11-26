// Ban do (ban web: minimap dong 1664-1721, ban do lon M dong 1667-1707). Nen ban do tinh 1 lan tu mau dat (luong phu),
// lop dong (cay, nha, do dat, ruong) cap nhat quanh nguoi choi moi 4 giay.
//  - Ban do nho goc phai: cua so 120 o quanh nguoi choi, mui ten huong nhin, ghim, lang. Bam vao = mo ban do lon.
//  - Ban do lon (M): lan chuot zoom quanh con tro (1-8), keo de di chuyen, bam dup = ve cho nguoi choi,
//    chuot phai = dat / go ghim (toi da 12, 6 mau, co cot sang tren dao). Ten vung + suc song (Docs muc 5).
//    Cot phai: lich am, pha trang, con nuoc hom nay, cho phien, mua vu (Docs muc 11: giai doan 5).
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UIElements;
using static SereneIsle.MenuPanels;

namespace SereneIsle
{
    [Serializable]
    public class MapPin { public float x, z; public int c; }

    public class MapUI : MonoBehaviour
    {
        public static MapUI Instance { get; private set; }
        public const int MaxPins = 12, MiniWindow = 120;
        public static readonly string[] PinColors = { "#e8483a", "#2f8fe0", "#f0b429", "#8a4fd8", "#2fae6a", "#ee6fa8" };

        public readonly List<MapPin> Pins = new List<MapPin>();
        public bool BigOpen => _big != null && _big.parent != null;
        public bool Ready => _tex != null;
        public float Zoom => _z;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { Instance = null; }

        Texture2D _tex;
        Color32[] _base, _px;
        int _n;
        bool _building;
        float _dynT, _miniT;

        VisualElement _mini, _miniMarks, _big, _bigMap, _bigMarks, _side;
        Image _miniImg, _bigImg;
        Label _miniArrow, _bigInfo;
        float _z = 1, _cx, _cz, _size = 640;
        bool _drag; Vector2 _dragStart; float _dragCx, _dragCz;
        readonly List<(string name, Vector2 at, RestorationModel.Region r)> _regionLabels = new List<(string, Vector2, RestorationModel.Region)>();
        readonly List<GameObject> _beacons = new List<GameObject>();

        GameSession S => GameSession.Instance;
        static IslandData D => IslandTerrain.Instance ? IslandTerrain.Instance.Data : null;

        void Awake() { Instance = this; }
        void OnDestroy() { if (Instance == this) Instance = null; if (_tex) Destroy(_tex); }

        // ================================================================== nen ban do
        static readonly Color32 SeaC = new Color32(58, 182, 204, 255), FreshC = new Color32(78, 176, 196, 255),
            BuildC = new Color32(196, 90, 58, 255), SoilC = new Color32(138, 90, 52, 255), PathC = new Color32(189, 180, 163, 255), Deep = new Color32(29, 111, 134, 255);

        /// <summary>Mau nen tung o (thuan, chay o luong phu): mau dat nhu dia hinh, nuoc bien / nuoc ngot.</summary>
        public static Color32[] BuildBase(IslandData d)
        {
            int N = d.N, GS = d.GS, M = IslandData.M;
            var px = new Color32[N * N];
            Parallel.For(0, N, j =>
            {
                for (int i = 0; i < N; i++)
                {
                    int k = j * N + i;
                    if (d.Level[k] == 0)
                    {
                        px[k] = d.Fresh[k] != 0 ? FreshC : d.HT[k] < IslandData.SEA - 1.2f ? Deep : SeaC;
                        continue;
                    }
                    int u = i + M, v = j + M, idx = v * GS + u;
                    float h = d.FH[idx];
                    float hx = d.FH[v * GS + Math.Min(GS - 1, u + 1)] - d.FH[v * GS + Math.Max(0, u - 1)];
                    float hz = d.FH[Math.Min(GS - 1, v + 1) * GS + u] - d.FH[Math.Max(0, v - 1) * GS + u];
                    px[k] = d.GroundColor(i, j, h, Mathf.Sqrt(hx * hx + hz * hz) / 2f, idx);
                }
            });
            return px;
        }

        /// <summary>Lop dong: cay toi mau, nha / do da dat, ruong.</summary>
        Color32 Dyn(IslandData d, int k)
        {
            var c = _base[k];
            if (d.Level[k] == 0) return c;
            var pl = Placeables.Instance ? Placeables.Instance.At(k) : null;
            if (pl != null) return pl.type == "path" ? PathC : BuildC;
            if (S && S.Farm && S.Farm.PlotAt(k) != null) return SoilC;
            if (d.ObjType != null && d.ObjType[k] != 0) return new Color32((byte)(c.r * .6f), (byte)(c.g * .7f), (byte)(c.b * .55f), 255);
            if (d.Solid[k] != 0) return BuildC; // nha lang, leu, ban che tao
            return c;
        }

        async void StartBuild(IslandData d)
        {
            _building = true;
            var b = await Task.Run(() => BuildBase(d));
            if (this == null || D != d) { _building = false; return; }
            _base = b; _n = d.N;
            _px = new Color32[_n * _n];
            for (int k = 0; k < _px.Length; k++) _px[k] = Dyn(d, k);
            _tex = new Texture2D(_n, _n, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp, name = "IslandMap" };
            _tex.SetPixels32(_px);
            _tex.Apply(false);
            _building = false;
            BuildRegions(d);
            if (_miniImg != null) _miniImg.image = _tex;
            if (_bigImg != null) _bigImg.image = _tex;
        }

        /// <summary>Cap nhat lop dong quanh nguoi choi (r o).</summary>
        public void RefreshAround(Vector3 p, int r = 40)
        {
            var d = D;
            if (d == null || _tex == null) return;
            int i0 = Mathf.Max(0, (int)p.x - r), i1 = Mathf.Min(_n - 1, (int)p.x + r), j0 = Mathf.Max(0, (int)p.z - r), j1 = Mathf.Min(_n - 1, (int)p.z + r);
            bool dirty = false;
            for (int j = j0; j <= j1; j++)
                for (int i = i0; i <= i1; i++)
                {
                    int k = j * _n + i;
                    var c = Dyn(d, k);
                    if (!c.Equals(_px[k])) { _px[k] = c; dirty = true; }
                }
            if (dirty) { _tex.SetPixels32(_px); _tex.Apply(false); }
        }

        public Color32 PixelAt(int i, int j) => _px != null ? _px[j * _n + i] : default;

        void BuildRegions(IslandData d)
        {
            _regionLabels.Clear();
            var rs = S ? S.Restoration : null;
            var model = rs ? rs.Model : null;
            if (model == null) return;
            foreach (var r in model.Regions)
            {
                if (r.cells.Count == 0) continue;
                float sx = 0, sz = 0;
                foreach (var c in r.cells)
                {
                    sx += (c % model.CellsPerSide + .5f) * RestorationModel.Cell - IslandData.M;
                    sz += (c / model.CellsPerSide + .5f) * RestorationModel.Cell - IslandData.M;
                }
                _regionLabels.Add((r.name, new Vector2(sx / r.cells.Count, sz / r.cells.Count), r));
            }
        }

        // ================================================================== giao dien
        void BuildUi()
        {
            var hud = GameHud.Instance;
            if (!hud || hud.Root == null || _mini != null) return;
            var ss = Resources.Load<StyleSheet>("UI/Map");
            if (ss && !hud.Root.styleSheets.Contains(ss)) hud.Root.styleSheets.Add(ss);
            _mini = E("minimap", "chip", true);
            _miniImg = new Image { scaleMode = ScaleMode.StretchToFill, pickingMode = PickingMode.Ignore };
            _miniImg.AddToClassList("mini-img");
            _mini.Add(_miniImg);
            _miniMarks = E(null, "map-marks");
            _mini.Add(_miniMarks);
            _miniArrow = L("map-arrow", "▲");
            _mini.Add(_miniArrow);
            _mini.Add(L("mini-hint", "M"));
            _mini.RegisterCallback<ClickEvent>(_ => OpenBig());
            var hudLayer = hud.Root.Q("hud");
            (hudLayer ?? hud.Root).Add(_mini);
        }

        public void OpenBig()
        {
            var hud = GameHud.Instance;
            var s = S;
            if (!hud || !s || !s.Running || BigOpen || _tex == null) return;
            var mss = Resources.Load<StyleSheet>("UI/Menu");
            if (mss && !hud.Root.styleSheets.Contains(mss)) hud.Root.styleSheets.Add(mss);
            _big = Overlay(hud.Root, out var card, CloseBig, "bigmap-card");
            var head = E(null, "bm-head");
            head.Add(L("panel-title", "Bản đồ Đảo Bình Yên"));
            _bigInfo = L("bm-info");
            head.Add(_bigInfo);
            card.Add(head);
            var row = E(null, "bm-row");
            card.Add(row);

            _size = Mathf.Clamp(Mathf.Min(hud.Root.layout.width - 420, hud.Root.layout.height - 330), 300, 620);
            if (float.IsNaN(_size) || _size <= 0) _size = 560;
            card.style.width = _size + 300 + 14 + 46;
            _bigMap = E("bm-map", null, true);
            _bigMap.style.width = _size; _bigMap.style.height = _size;
            _bigImg = new Image { image = _tex, scaleMode = ScaleMode.StretchToFill, pickingMode = PickingMode.Ignore };
            _bigImg.style.position = Position.Absolute;
            _bigImg.style.left = 0; _bigImg.style.top = 0; _bigImg.style.right = 0; _bigImg.style.bottom = 0;
            _bigMap.Add(_bigImg);
            _bigMarks = E(null, "map-marks");
            _bigMap.Add(_bigMarks);
            row.Add(_bigMap);
            _side = E(null, "bm-side");
            row.Add(_side);

            var leg = E(null, "bm-legend");
            foreach (var (hex, name) in new[] { ("#ead59e", "Bãi biển"), ("#8db552", "Đồi cỏ"), ("#5a8a36", "Rừng"), ("#a9c95a", "Đồng bằng"), ("#c9673a", "Đất đỏ"),
                                                 ("#3e7a2c", "Rừng rậm"), ("#617a48", "Rừng đước"), ("#eef2f4", "Núi"), ("#4eb0c4", "Sông hồ"), ("#c45a3a", "Nhà, đồ đặt") })
            {
                var it = E(null, "leg-item");
                var sw = E(null, "leg-sw"); sw.style.backgroundColor = MaterialLibrary.Hex(hex);
                it.Add(sw); it.Add(L("leg-text", name));
                leg.Add(it);
            }
            card.Add(leg);
            card.Add(L("panel-hint", "Lăn chuột: phóng to · kéo: di chuyển · bấm đúp: về chỗ bạn · chuột phải: cắm / gỡ ghim · M hoặc Esc: đóng"));

            _bigMap.RegisterCallback<WheelEvent>(OnWheel);
            _bigMap.RegisterCallback<PointerDownEvent>(OnDown);
            _bigMap.RegisterCallback<PointerMoveEvent>(OnMove);
            _bigMap.RegisterCallback<PointerUpEvent>(OnUp);

            _z = 1; _cx = _n / 2f; _cz = _n / 2f;
            s.UiOpen = true;
            RenderSide();
            RenderBig();
        }

        public void CloseBig()
        {
            if (!BigOpen) return;
            _big.RemoveFromHierarchy();
            _big = null;
            if (S) S.UiOpen = false;
        }

        // ---- toa do ban do lon ----
        float Span => _n / _z;
        void Clamp()
        {
            float h = Span / 2;
            _cx = Mathf.Clamp(_cx, h, _n - h);
            _cz = Mathf.Clamp(_cz, h, _n - h);
        }
        Vector2 ToLocal(float x, float z) => new Vector2((x - (_cx - Span / 2)) / Span * _size, _size - (z - (_cz - Span / 2)) / Span * _size);
        Vector2 ToWorld(Vector2 l) => new Vector2(_cx - Span / 2 + l.x / _size * Span, _cz - Span / 2 + (_size - l.y) / _size * Span);

        public void ZoomAt(Vector2 local, float factor)
        {
            var w = ToWorld(local);
            _z = Mathf.Clamp(_z * factor, 1, 8);
            // giu diem duoi con tro dung yen
            _cx = w.x - (local.x / _size - .5f) * Span;
            _cz = w.y + (local.y / _size - .5f) * Span;
            Clamp();
            RenderBig();
        }

        void OnWheel(WheelEvent e) { ZoomAt(e.localMousePosition, e.delta.y < 0 ? 1.25f : .8f); e.StopPropagation(); }

        void OnDown(PointerDownEvent e)
        {
            if (e.button == 1) { TogglePin(ToWorld(e.localPosition)); e.StopPropagation(); return; }
            if (e.button != 0) return;
            if (e.clickCount >= 2)
            {
                var p = S.Player.transform.position;
                _cx = p.x; _cz = p.z; _z = Mathf.Clamp(_z * 2, 1, 8); Clamp(); RenderBig();
                return;
            }
            _drag = true; _dragStart = e.localPosition; _dragCx = _cx; _dragCz = _cz;
            _bigMap.CapturePointer(e.pointerId);
        }

        void OnMove(PointerMoveEvent e)
        {
            if (!_drag) return;
            var dlt = (Vector2)e.localPosition - _dragStart;
            _cx = _dragCx - dlt.x / _size * Span;
            _cz = _dragCz + dlt.y / _size * Span;
            Clamp();
            RenderBig();
        }

        void OnUp(PointerUpEvent e)
        {
            _drag = false;
            if (_bigMap.HasPointerCapture(e.pointerId)) _bigMap.ReleasePointer(e.pointerId);
        }

        /// <summary>Cam ghim tai (x, z) the gioi; bam trung ghim cu (ban kinh 14 px) thi go.</summary>
        public void TogglePin(Vector2 world)
        {
            float hit = 14f / _size * Span;
            int idx = Pins.FindIndex(p => Vector2.Distance(new Vector2(p.x, p.z), world) < hit);
            if (idx >= 0) Pins.RemoveAt(idx);
            else
            {
                if (Pins.Count >= MaxPins) { S?.Say($"Tối đa {MaxPins} ghim. Chuột phải vào ghim cũ để gỡ."); return; }
                int c = 0;
                for (; c < PinColors.Length; c++) if (!Pins.Exists(p => p.c == c)) break;
                Pins.Add(new MapPin { x = world.x, z = world.y, c = c % PinColors.Length });
            }
            RefreshBeacons();
            if (BigOpen) RenderBig();
        }

        void RenderBig()
        {
            var s = S; var d = D;
            if (!BigOpen || s == null || d == null) return;
            if (_regionLabels.Count == 0 && s.Restoration) { s.Restoration.VitalityAt(Vector3.zero); BuildRegions(d); }
            _bigImg.uv = new Rect((_cx - Span / 2) / _n, (_cz - Span / 2) / _n, Span / _n, Span / _n);
            _bigMarks.Clear();
            // ten vung + suc song
            foreach (var (name, at, r) in _regionLabels)
                {
                    if (_z < 1.5f && r.cells.Count < 4 && r.group != Biome.Town) continue; // thu nho: chi vung lon (ban web: zoom >= 1.5)
                    var l = ToLocal(at.x, at.y);
                    if (l.x < 0 || l.y < 0 || l.x > _size || l.y > _size) continue;
                    var lab = L("bm-region", $"{name}\n{Mathf.RoundToInt(r.vitality)}");
                    lab.style.left = l.x; lab.style.top = l.y;
                    lab.style.color = r.vitality >= 50 ? new Color(.85f, 1f, .7f) : r.vitality >= 25 ? new Color(1f, .96f, .8f) : new Color(1f, .8f, .7f);
                    _bigMarks.Add(lab);
                }
            // lang
            var town = ToLocal(d.CX, d.CZ);
            var tm = E(null, "bm-town"); tm.style.left = town.x; tm.style.top = town.y;
            _bigMarks.Add(tm);
            var tl = L("bm-town-label", "Làng"); tl.style.left = town.x; tl.style.top = town.y + 8;
            _bigMarks.Add(tl);
            // ghim
            for (int i = 0; i < Pins.Count; i++)
            {
                var p = Pins[i];
                var l = ToLocal(p.x, p.z);
                var pin = L("bm-pin", (i + 1).ToString());
                pin.style.left = l.x; pin.style.top = l.y;
                pin.style.backgroundColor = MaterialLibrary.Hex(PinColors[p.c]);
                _bigMarks.Add(pin);
            }
            // kho bau (dau X) + chai troi dat
            var bt = Bottles.Instance;
            if (bt && bt.Treasure >= 0) { var l = ToLocal(bt.Treasure % d.N, bt.Treasure / d.N); var x = L("bm-x", "X"); x.style.left = l.x; x.style.top = l.y; _bigMarks.Add(x); }
            if (bt && bt.Bottle >= 0) { var l = ToLocal(bt.Bottle % d.N, bt.Bottle / d.N); var b = E(null, "mini-bottle"); b.style.left = l.x; b.style.top = l.y; _bigMarks.Add(b); }
            // nguoi choi
            var pp = s.Player.transform.position;
            var me = ToLocal(pp.x, pp.z);
            var arrow = L("map-arrow big", "▲");
            arrow.style.left = me.x; arrow.style.top = me.y;
            arrow.style.rotate = new Rotate(Angle.Degrees(s.Player.Facing * Mathf.Rad2Deg));
            _bigMarks.Add(arrow);
            float dist = Vector2.Distance(new Vector2(pp.x, pp.z), new Vector2(d.CX, d.CZ));
            var region = s.Restoration ? s.Restoration.Current : null;
            _bigInfo.text = $"{(region != null ? region.name + " · " : "")}cách làng {Mathf.RoundToInt(dist)} ô · phóng {_z:0.#}x · {Pins.Count}/{MaxPins} ghim";
        }

        void RenderSide()
        {
            var s = S;
            _side.Clear();
            var date = s.Date;
            _side.Add(L("bm-h", "LỊCH ÂM"));
            _side.Add(L("bm-big", date.ShortLabel));
            _side.Add(L("bm-p", $"Mùa {LunarCalendar.SeasonName(date.Season)} · năm {date.Year} · tuần {date.Week}, ngày {date.DayOfWeek}/7"));
            double age = LunarCalendar.MoonAge(s.Clock.Day, s.Clock.Hour);
            _side.Add(L("bm-p", $"{LunarCalendar.MoonPhaseName(age)} · sáng {Mathf.RoundToInt((float)LunarCalendar.MoonIllumination(age) * 100)}%"));
            string fest = LunarCalendar.Festival(date);
            if (fest != null) _side.Add(L("bm-p bm-hl", $"Hôm nay: {fest}"));
            else
            {
                var next = LunarCalendar.NextFestival(s.Clock.Day, out int fd);
                if (next != null) _side.Add(L("bm-p", $"Lễ sắp tới: {next}, còn {fd} ngày."));
            }
            int toMarket = 0; while (!Market.IsMarketDay(s.Clock.Day + toMarket) && toMarket < 7) toMarket++;
            _side.Add(L("bm-p", toMarket == 0 ? "Hôm nay họp chợ phiên (7–17 giờ)." : $"Chợ phiên: còn {toMarket} ngày."));

            _side.Add(L("bm-h", "CON NƯỚC HÔM NAY"));
            double dayStart = TideClock.AbsHours(s.Clock.Day, 0), dayEnd = dayStart + 24;
            var events = new List<(double t, bool high)>();
            for (double t = TideClock.NextHigh(dayStart - .01); t < dayEnd; t = TideClock.NextHigh(t + .5)) events.Add((t, true));
            for (double t = TideClock.NextLow(dayStart - .01); t < dayEnd; t = TideClock.NextLow(t + .5)) events.Add((t, false));
            events.Sort((a, b) => a.t.CompareTo(b.t));
            foreach (var (t, high) in events)
                _side.Add(L("bm-p", $"{(high ? "Nước lớn" : "Nước ròng")} lúc {TideClock.ClockLabel(t)}"));
            float amp = TideClock.Amplitude(TideClock.AbsHours(s.Clock.Day, 12));
            _side.Add(L("bm-p bm-dim", amp > .32f ? "Con nước rong: bãi triều lộ rộng, ruộng thấp dễ nhiễm mặn." : amp < .2f ? "Con nước kém: nước lên xuống ít." : "Con nước vừa."));

            _side.Add(L("bm-h", $"MÙA VỤ {LunarCalendar.SeasonName(date.Season).ToUpperInvariant()}"));
            var crops = CropDatabase.Instance;
            if (crops)
            {
                var sow = new List<string>(); var fruit = new List<string>();
                foreach (var c in crops.crops)
                {
                    if (c == null || !c.InSeason(date.Season)) continue;
                    if (c.kind == CropKind.Tree) fruit.Add(c.displayName); else sow.Add(c.displayName);
                }
                _side.Add(L("bm-p", "Gieo: " + (sow.Count > 0 ? string.Join(", ", sow.GetRange(0, Mathf.Min(10, sow.Count))) + (sow.Count > 10 ? "…" : "") : "—")));
                if (fruit.Count > 0) _side.Add(L("bm-p", "Cây trái ra trái: " + string.Join(", ", fruit)));
            }
        }

        // ================================================================== cot sang tai ghim (ban web: pinBeacons)
        static Mesh _beam;
        readonly Dictionary<int, Material> _beamMats = new Dictionary<int, Material>();

        void RefreshBeacons()
        {
            foreach (var b in _beacons) if (b) Destroy(b);
            _beacons.Clear();
            var d = D;
            if (d == null) return;
            if (!_beam)
            {
                var src = LowPolyShapes.Cylinder(.18f, .18f, 1, 10);
                _beam = Instantiate(src);
                var col = new Color[_beam.vertexCount];
                for (int i = 0; i < col.Length; i++) col[i] = Color.white;
                _beam.colors = col;
            }
            foreach (var p in Pins)
            {
                if (!_beamMats.TryGetValue(p.c, out var m) || !m)
                {
                    m = new Material(Shader.Find("SereneIsle/Glow")) { name = "SI_PinBeam", renderQueue = 3000 };
                    var c = MaterialLibrary.Hex(PinColors[p.c]); c.a = .35f;
                    m.SetColor("_BaseColor", c);
                    _beamMats[p.c] = m;
                }
                var go = new GameObject("PinBeam");
                go.transform.SetParent(transform, false);
                go.transform.position = new Vector3(p.x, d.GroundHeight(p.x, p.z) + 20, p.z);
                go.transform.localScale = new Vector3(1, 40, 1);
                go.AddComponent<MeshFilter>().sharedMesh = _beam;
                var mr = go.AddComponent<MeshRenderer>();
                mr.sharedMaterial = m;
                mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                _beacons.Add(go);
            }
        }

        public void SetPins(List<MapPin> pins)
        {
            Pins.Clear();
            if (pins != null) foreach (var p in pins) if (p != null) Pins.Add(p);
            RefreshBeacons();
        }

        // ================================================================== moi khung
        void Update()
        {
            var s = S; var d = D;
            if (_mini == null) BuildUi();
            if (d != null && _tex == null && !_building) StartBuild(d);
            if (_mini == null) return;
            bool show = s && s.Running && _tex != null;
            _mini.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;
            if (!show) return;
            if (_beacons.Count != Pins.Count) RefreshBeacons();

            if (BigOpen)
            {
                if (GameInput.MapPressed || GameInput.CancelPressed) CloseBig();
                else RenderBig();
                return;
            }
            if (GameInput.MapPressed && s.Active) { OpenBig(); return; }

            var p = s.Player.transform.position;
            _dynT -= Time.deltaTime;
            if (_dynT <= 0) { _dynT = 4; RefreshAround(p); }
            _miniT -= Time.deltaTime;
            if (_miniT > 0) return;
            _miniT = .2f;
            float half = MiniWindow / 2f;
            float x0 = Mathf.Clamp(p.x - half, 0, _n - MiniWindow), z0 = Mathf.Clamp(p.z - half, 0, _n - MiniWindow);
            _miniImg.uv = new Rect(x0 / _n, z0 / _n, (float)MiniWindow / _n, (float)MiniWindow / _n);
            float W = _mini.layout.width > 0 ? _mini.layout.width - 6 : 160;
            Vector2 Loc(float x, float z) => new Vector2(3 + (x - x0) / MiniWindow * W, 3 + W - (z - z0) / MiniWindow * W);
            var me = Loc(p.x, p.z);
            _miniArrow.style.left = me.x; _miniArrow.style.top = me.y;
            _miniArrow.style.rotate = new Rotate(Angle.Degrees(s.Player.Facing * Mathf.Rad2Deg));
            _miniMarks.Clear();
            var town = Loc(d.CX, d.CZ);
            if (town.x > 0 && town.y > 0 && town.x < W && town.y < W) { var t = E(null, "mini-town"); t.style.left = town.x; t.style.top = town.y; _miniMarks.Add(t); }
            var bt = Bottles.Instance;
            if (bt && bt.Treasure >= 0)
            {
                var l = Loc(bt.Treasure % _n, bt.Treasure / _n);
                l.x = Mathf.Clamp(l.x, 6, W); l.y = Mathf.Clamp(l.y, 6, W);
                var x = L("bm-x small", "X"); x.style.left = l.x; x.style.top = l.y; _miniMarks.Add(x);
            }
            if (bt && bt.Bottle >= 0)
            {
                var l = Loc(bt.Bottle % _n, bt.Bottle / _n);
                if (l.x > 0 && l.y > 0 && l.x < W && l.y < W) { var b = E(null, "mini-bottle"); b.style.left = l.x; b.style.top = l.y; _miniMarks.Add(b); }
            }
            for (int i = 0; i < Pins.Count; i++)
            {
                var l = Loc(Pins[i].x, Pins[i].z);
                l.x = Mathf.Clamp(l.x, 4, W); l.y = Mathf.Clamp(l.y, 4, W);
                var pin = E(null, "mini-pin");
                pin.style.left = l.x; pin.style.top = l.y;
                pin.style.backgroundColor = MaterialLibrary.Hex(PinColors[Pins[i].c]);
                _miniMarks.Add(pin);
            }
        }
    }
}
