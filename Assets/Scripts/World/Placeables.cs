// Do che tao dat xuong dat (ban web: PLACEABLES, placeMesh / addPlaced / removePlaced dong 1071-1089, canPlace dong 1434).
// Lua trai, duoc: lua co hoat anh + den diem ban dem. Voi tuoi: moi sang tuoi 8 o quanh no. Lat da: di duoc, khong cuoc duoc.
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SereneIsle
{
    [Serializable]
    public class PlacedItem { public int k; public string type; public float rot; }

    public class Placeables : MonoBehaviour
    {
        public static Placeables Instance { get; private set; }

        /// <summary>Thu tu R doi mon (ban web: PLACEABLES).</summary>
        public static readonly string[] Types = { "campfire", "torch", "fence", "path", "sprinkler", "scarecrow", "chuong_ga", "chuong_trau", "nha_luoi" };

        /// <summary>O k nam duoi nha luoi (5 x 5 quanh cot giua, muc 4.8).</summary>
        public bool Sheltered(int k)
        {
            var d = D;
            if (d == null) return false;
            int i = k % d.N, j = k / d.N, r = FarmCare.ShelterRadius;
            for (int dj = -r; dj <= r; dj++)
                for (int di = -r; di <= r; di++)
                    if (d.InBounds(i + di, j + dj) && _items.TryGetValue(d.Idx(i + di, j + dj), out var v) && v.item.type == "nha_luoi") return true;
            return false;
        }

        /// <summary>Do cao de nhay qua (ban web: PLH). Bu nhin = khong nhay qua duoc.</summary>
        public static float JumpTop(string type) => type switch
        {
            "fence" => .9f, "campfire" => .55f, "torch" => 1.05f, "path" => 0, "sprinkler" => .45f, _ => 9f, // bu nhin, chuong: khong nhay qua
        };

        /// <summary>So den diem bat cung luc (gan nguoi choi nhat). URP Forward: 4 den / vat the.</summary>
        public int maxLights = 4;

        /// <summary>Mon dang chon de dat (ban web: G.buildSel).</summary>
        public string Selected { get; set; }
        public int Count => _items.Count;
        public event Action Changed;

        sealed class View
        {
            public PlacedItem item;
            public GameObject go;
            public Flame flame;
            public Light light;
            public float lightBase;
            public Transform head;
        }

        readonly Dictionary<int, View> _items = new Dictionary<int, View>();
        Transform _root;

        GameSession S => GameSession.Instance;
        static IslandData D => IslandTerrain.Instance ? IslandTerrain.Instance.Data : null;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { Instance = null; }

        void Awake() { Instance = this; }
        void OnDestroy() { if (Instance == this) Instance = null; }

        public PlacedItem At(int k) => k >= 0 && _items.TryGetValue(k, out var v) ? v.item : null;
        public IEnumerable<PlacedItem> All() { foreach (var v in _items.Values) yield return v.item; }

        /// <summary>Voi tuoi tu dong co phu o k khong (3x3 quanh voi).</summary>
        public bool Sprinkled(int k)
        {
            var d = D;
            if (d == null) return false;
            int i = k % d.N, j = k / d.N;
            for (int dj = -1; dj <= 1; dj++)
                for (int di = -1; di <= 1; di++)
                {
                    if (!d.InBounds(i + di, j + dj)) continue;
                    if (_items.TryGetValue(d.Idx(i + di, j + dj), out var v) && v.item.type == "sprinkler") return true;
                }
            return false;
        }

        /// <summary>Co mon 'type' trong ban kinh r o quanh vi tri (ban web: placedNear).</summary>
        public bool Near(string type, Vector3 pos, int r)
        {
            var d = D;
            if (d == null) return false;
            int pi = Mathf.RoundToInt(pos.x), pj = Mathf.RoundToInt(pos.z);
            for (int dj = -r; dj <= r; dj++)
                for (int di = -r; di <= r; di++)
                    if (d.InBounds(pi + di, pj + dj) && _items.TryGetValue(d.Idx(pi + di, pj + dj), out var v) && v.item.type == type) return true;
            return false;
        }

        // ---------------------------------------------------------------- chon mon
        public bool Owns(string id) => S && S.Inventory.Has(id);

        /// <summary>Chon mon dau tien con trong tui (hoac null).</summary>
        public string SelectFirstOwned()
        {
            Selected = null;
            foreach (var t in Types) if (Owns(t)) { Selected = t; break; }
            return Selected;
        }

        /// <summary>R: doi sang mon tiep theo co trong tui (ban web dong 1658).</summary>
        public string CycleSelected()
        {
            var own = new List<string>();
            foreach (var t in Types) if (Owns(t)) own.Add(t);
            if (own.Count == 0) { Selected = null; return null; }
            int at = Selected != null ? own.IndexOf(Selected) : -1;
            Selected = own[(at + 1) % own.Count];
            return Selected;
        }

        // ---------------------------------------------------------------- dat / nhat
        /// <summary>canPlace: tren can, khong vat can, khong o ruong, khong cho dang dung (tru lat da), nuoc rong khong toi.</summary>
        public bool CanPlace(int k, string id, out string why)
        {
            var d = D;
            why = "Không đặt được ở ô này.";
            if (d == null || k < 0 || string.IsNullOrEmpty(id)) return false;
            if (d.Level[k] == 0 || d.Pier[k] != 0 || d.Fresh[k] != 0) return false;
            if (d.Solid[k] != 0 || _items.ContainsKey(k)) return false;
            if (S && S.Farm && S.Farm.PlotAt(k) != null) { why = "Ô này là ruộng. Lấp đất bằng xẻng trước đã."; return false; }
            if (d.HT[k] < TideClock.MaxLevel + .04f) { why = "Nước rong sẽ tràn tới đây. Chọn chỗ cao hơn."; return false; }
            if (id == "nha_luoi")
            {
                // khung 5 x 5: khong de len nha, sap, cay, do da dat
                int i0 = k % d.N, j0 = k / d.N, r = FarmCare.ShelterRadius;
                for (int dj = -r; dj <= r; dj++)
                    for (int di = -r; di <= r; di++)
                    {
                        int i = i0 + di, j = j0 + dj;
                        if (!d.InBounds(i, j)) return false;
                        int q = d.Idx(i, j);
                        if (d.Level[q] == 0 || d.Pier[q] != 0 || d.Solid[q] != 0 || _items.ContainsKey(q))
                        { why = "Nhà lưới cần khoảng đất trống 5 × 5 ô (không vướng nhà, cây, đồ đã đặt)."; return false; }
                    }
            }
            if (id != "path" && S && S.Player)
            {
                var p = S.Player.transform.position;
                if (Mathf.RoundToInt(p.x) == k % d.N && Mathf.RoundToInt(p.z) == k / d.N) { why = "Lùi ra một chút rồi đặt."; return false; }
            }
            why = null;
            return true;
        }

        public PlacedItem Add(string type, int k, float rot = 0)
        {
            var d = D;
            if (d == null || k < 0 || _items.ContainsKey(k)) return null;
            if (!_root) { _root = new GameObject("Placed").transform; _root.SetParent(transform, false); }
            var item = new PlacedItem { k = k, type = type, rot = rot };
            var v = new View { item = item };
            v.go = BuildView(type, out v.flame, out v.head);
            v.go.name = $"{type}_{k}";
            v.go.transform.SetParent(_root, false);
            v.go.transform.SetPositionAndRotation(new Vector3(k % d.N, d.HT[k], k / d.N), Quaternion.Euler(0, rot * Mathf.Rad2Deg, 0));
            if (v.flame != null)
            {
                var lg = new GameObject("light");
                lg.transform.SetParent(v.go.transform, false);
                lg.transform.localPosition = new Vector3(0, type == "torch" ? 1.25f : .6f, 0);
                v.light = lg.AddComponent<Light>();
                v.light.type = LightType.Point;
                v.light.color = new Color(1f, .62f, .32f);
                v.light.range = type == "torch" ? 5.5f : 8f;
                v.light.shadows = LightShadows.None;
                v.light.renderMode = LightRenderMode.ForcePixel;
                v.lightBase = type == "torch" ? 1.4f : 2.4f;
                v.light.intensity = 0;
                v.light.enabled = false;
            }
            _items[k] = v;
            if (type != "path") d.Solid[k] = 1;
            d.PlacedTop[k] = JumpTop(type);
            if (type == "path") d.PlacedTop.Remove(k);
            IslandTerrain.Instance.Vegetation?.HideGrass(k);
            Changed?.Invoke();
            return item;
        }

        public void Remove(int k)
        {
            var d = D;
            if (!_items.TryGetValue(k, out var v)) return;
            _items.Remove(k);
            if (v.go) Destroy(v.go);
            if (d != null) { d.Solid[k] = 0; d.PlacedTop.Remove(k); }
            Changed?.Invoke();
        }

        public void Clear()
        {
            foreach (var k in new List<int>(_items.Keys)) Remove(k);
        }

        public List<PlacedItem> Snapshot()
        {
            var l = new List<PlacedItem>();
            foreach (var v in _items.Values) l.Add(new PlacedItem { k = v.item.k, type = v.item.type, rot = v.item.rot });
            return l;
        }

        public void Restore(IEnumerable<PlacedItem> list)
        {
            Clear();
            var d = D;
            if (list == null || d == null) return;
            foreach (var p in list)
                if (p != null && p.k >= 0 && p.k < d.N * d.N && Array.IndexOf(Types, p.type) >= 0 && d.Solid[p.k] == 0) Add(p.type, p.k, p.rot);
            EnsureTownFire();
        }

        /// <summary>Lua trai giua lang (ban web dong 1863: addPlaced('campfire', townIdx(0,3)) neu file luu chua co).</summary>
        public void EnsureTownFire()
        {
            var d = D;
            if (d == null) return;
            int k = d.TownIdx(0, 3);
            if (k < 0 || k >= d.N * d.N || _items.ContainsKey(k) || d.Level[k] == 0) return;
            if (d.ObjType != null && d.ObjType[k] != 0) return;
            if (d.Solid[k] != 0) return;
            Add("campfire", k);
        }

        // ---------------------------------------------------------------- moi khung
        void Update()
        {
            float dt = Time.deltaTime, T = Time.time;
            float night = S ? NightFactor(S.Clock.Hour) : 0;
            Vector3 pp = S && S.Player ? S.Player.transform.position : Vector3.zero;

            // chi bat vai den gan nhat (ban web: lightSrc sap xep theo khoang cach, 3 PointLight)
            _lit.Clear();
            foreach (var v in _items.Values)
            {
                v.flame?.Update(dt, T);
                if (v.head) v.head.Rotate(0, dt * 2 * Mathf.Rad2Deg, 0, Space.Self);
                if (v.light) _lit.Add(v);
            }
            if (_lit.Count > 1) _lit.Sort((a, b) => (a.go.transform.position - pp).sqrMagnitude.CompareTo((b.go.transform.position - pp).sqrMagnitude));
            for (int q = 0; q < _lit.Count; q++)
            {
                var v = _lit[q];
                bool on = night > .02f && q < maxLights && (v.go.transform.position - pp).sqrMagnitude < 45 * 45;
                v.light.enabled = on;
                if (on) v.light.intensity = v.lightBase * night * (1 + Mathf.Sin(T * 9 + v.item.k) * .06f + Mathf.Sin(T * 23 + v.item.k * .7f) * .04f);
            }
        }

        readonly List<View> _lit = new List<View>();

        /// <summary>Do toi 0..1 theo gio (gio > 24 = sau nua dem).</summary>
        public static float NightFactor(float h)
        {
            if (h >= 24) h -= 24;
            if (h < 5) return 1;
            if (h < 6.5f) return 1 - (h - 5) / 1.5f;
            if (h < 18) return 0;
            if (h < 19.8f) return (h - 18) / 1.8f;
            return 1;
        }

        // ---------------------------------------------------------------- mo hinh (ban web: placeMesh)
        static readonly Dictionary<string, Mesh> _meshes = new Dictionary<string, Mesh>();

        static Mesh MeshOf(string type)
        {
            if (_meshes.TryGetValue(type, out var m) && m) return m;
            var b = new MeshBuilder();
            switch (type)
            {
                case "campfire":
                    for (int k = 0; k < 8; k++)
                    {
                        float a = k / 8f * 6.28f;
                        b.P(LowPolyShapes.Dodeca, "#8e8a86", Mathf.Cos(a) * .36f, .07f, Mathf.Sin(a) * .36f, .12f, .09f, .12f, 0, a, 0);
                    }
                    for (int k = 0; k < 5; k++)
                    {
                        float a = k / 5f * 6.28f + .3f, c = Mathf.Cos(a), s2 = Mathf.Sin(a);
                        b.P(LowPolyShapes.Cylinder(.04f, .05f, .5f, 6), k % 2 == 1 ? "#5a3a22" : "#4a2e1a", c * .13f, .18f, s2 * .13f, 1, 1, 1, -s2 * .55f, 0, c * .55f);
                        b.P(LowPolyShapes.Cylinder(.041f, .041f, .06f, 6), "#1e1410", c * .03f, .4f, s2 * .03f, 1, 1, 1, -s2 * .55f, 0, c * .55f);
                    }
                    b.P(LowPolyShapes.Cylinder(.05f, .05f, .55f, 6), "#6b4423", .05f, .05f, -.2f, 1, 1, 1, 0, .4f, 1.57f);
                    b.P(LowPolyShapes.Dodeca, "#2a1c14", 0, .02f, 0, .24f, .04f, .24f);
                    break;
                case "torch":
                    b.Box("#7a5232", 0, .5f, 0, .07f, 1, .07f);
                    b.Box("#4a3220", 0, 1.02f, 0, .12f, .08f, .12f);
                    break;
                case "fence":
                    b.Box("#8a6038", -.45f, .4f, 0, .1f, .8f, .1f);
                    b.Box("#8a6038", .45f, .4f, 0, .1f, .8f, .1f);
                    b.Box("#a8703e", 0, .55f, 0, 1, .08f, .05f);
                    b.Box("#a8703e", 0, .3f, 0, 1, .08f, .05f);
                    break;
                case "path":
                    b.Box("#a29f97", -.22f, .02f, -.2f, .42f, .05f, .4f, 0, .2f, 0);
                    b.Box("#9a978f", .22f, .02f, -.18f, .38f, .05f, .42f, 0, -.15f, 0);
                    b.Box("#aaa79f", -.18f, .02f, .24f, .44f, .05f, .36f, 0, .1f, 0);
                    b.Box("#97948c", .24f, .02f, .22f, .36f, .05f, .4f, 0, -.3f, 0);
                    break;
                case "sprinkler":
                    b.P(LowPolyShapes.Cylinder(.16f, .2f, .12f, 8), "#7d8890", 0, .06f, 0);
                    b.P(LowPolyShapes.Cylinder(.04f, .04f, .35f, 6), "#c3ccd2", 0, .25f, 0);
                    break;
                case "sprinkler_head":
                    b.Box("#d88a4a", 0, 0, 0, .5f, .05f, .06f);
                    b.Box("#d88a4a", 0, 0, 0, .06f, .05f, .5f);
                    b.P(LowPolyShapes.Sphere86, "#3d9bd6", 0, .04f, 0, .07f, .07f, .07f);
                    break;
                case "scarecrow":
                    b.Box("#7a5232", 0, .75f, 0, .08f, 1.5f, .08f);
                    b.Box("#7a5232", 0, 1.1f, 0, 1, .07f, .07f);
                    b.Box("#4f86a6", 0, 1.05f, 0, .5f, .5f, .25f);
                    b.P(LowPolyShapes.Ico0, "#e6c25a", 0, 1.5f, 0, .2f, .22f, .2f);
                    b.P(LowPolyShapes.Cylinder(.3f, .3f, .03f, 10), "#9a6534", 0, 1.66f, 0);
                    b.P(LowPolyShapes.Cylinder(.15f, .17f, .14f, 10), "#9a6534", 0, 1.74f, 0);
                    b.Box("#e6c25a", -.52f, 1.0f, 0, .1f, .18f, .1f);
                    b.Box("#e6c25a", .52f, 1.0f, 0, .1f, .18f, .1f);
                    break;
                case "chuong_ga":
                    // chuong ga san: 4 chan, san tre, vach nan, mai la, cau thang ga
                    foreach (var (x, z) in new[] { (-.38f, -.32f), (.38f, -.32f), (-.38f, .32f), (.38f, .32f) }) b.Box("#7a5232", x, .35f, z, .06f, .7f, .06f);
                    b.Box("#a8803a", 0, .42f, 0, .86f, .05f, .74f);
                    for (int k = 0; k < 6; k++) b.Box(k % 2 == 1 ? "#c8a06a" : "#b8904a", -.36f + k * .144f, .66f, -.34f, .12f, .44f, .04f);
                    b.Box("#c8a06a", -.42f, .66f, 0, .04f, .44f, .7f);
                    b.Box("#c8a06a", .42f, .66f, 0, .04f, .44f, .7f);
                    b.Box("#c8a06a", -.22f, .66f, .35f, .4f, .44f, .04f);
                    b.Box("#3a2a1a", .2f, .62f, .36f, .22f, .26f, .02f);                        // cua chuong
                    b.Add(ProceduralMeshes.Prism(3, 1, 1, "TentPrism"), Matrix4x4.TRS(new Vector3(0, 1.02f, 0), Quaternion.Euler(0, 90, 0) * Quaternion.Euler(-90, 0, 0), new Vector3(.6f, 1.05f, .32f)), MaterialLibrary.Hex("#b5523a"));
                    b.Box("#8a6038", .2f, .22f, .62f, .16f, .04f, .55f, -.75f, 0, 0);            // cau thang
                    b.P(LowPolyShapes.Cylinder(.12f, .1f, .08f, 8), "#8a6a40", -.25f, .04f, .55f); // mang an
                    break;
                case "chuong_trau":
                    // mai che (chuong trau bo): 4 cot, mai la doc, mang co, rom
                    foreach (var (x, z) in new[] { (-.45f, -.4f), (.45f, -.4f), (-.45f, .4f), (.45f, .4f) }) b.Box("#6b4a2a", x, .75f, z, .08f, 1.5f, .08f);
                    b.Box("#b89a5a", 0, 1.55f, 0, 1.15f, .06f, 1.05f, .18f, 0, 0);
                    b.Box("#a8884a", 0, 1.6f, -.1f, 1.2f, .04f, .7f, .18f, 0, 0);
                    b.Box("#7a5232", 0, .28f, -.38f, .9f, .22f, .22f);                             // mang co
                    b.Box("#9ab85a", 0, .4f, -.38f, .82f, .05f, .16f);
                    b.Box("#d8c070", -.2f, .08f, .1f, .5f, .16f, .4f, 0, .3f, 0);                   // rom
                    b.Box("#e0c878", .25f, .07f, .18f, .4f, .14f, .3f, 0, -.2f, 0);
                    b.P(LowPolyShapes.Cylinder(.14f, .14f, .3f, 10), "#5a8aa8", .38f, .15f, -.05f); // thung nuoc
                    break;
                case "nha_luoi":
                {
                    // nha luoi (muc 4.8): khung tre 5 x 5 o, mai luoi trang dan thua (cot giua + 4 cot goc)
                    const float R = 2.45f, H = 2.2f;
                    foreach (var (x, z) in new[] { (-R, -R), (R, -R), (-R, R), (R, R), (0f, 0f) })
                        b.Box("#a8884a", x, H / 2, z, .09f, H, .09f);
                    foreach (var z in new[] { -R, 0f, R }) b.Box("#9a7a3a", 0, H, z, R * 2 + .1f, .07f, .07f);
                    foreach (var x in new[] { -R, 0f, R }) b.Box("#9a7a3a", x, H, 0, .07f, .07f, R * 2 + .1f);
                    for (float t = -R + .35f; t < R - .1f; t += .35f)
                    {
                        b.Box("#eef0ec", 0, H + .04f, t, R * 2, .015f, .02f);
                        b.Box("#eef0ec", t, H + .045f, 0, .02f, .015f, R * 2);
                    }
                    break;
                }
                default:
                    b.Box("#c9a46e", 0, .2f, 0, .4f, .4f, .4f);
                    break;
            }
            m = b.ToMesh("Placed_" + type);
            _meshes[type] = m;
            return m;
        }

        static GameObject BuildView(string type, out Flame flame, out Transform head)
        {
            flame = null; head = null;
            var go = new GameObject(type);
            go.AddComponent<MeshFilter>().sharedMesh = MeshOf(type);
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = MaterialLibrary.Objects(false);
            if (type == "path") mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            if (type == "campfire") { flame = new Flame(go.transform, 1.05f, false); flame.root.localPosition = new Vector3(0, .04f, 0); }
            else if (type == "torch") { flame = new Flame(go.transform, .42f, true); flame.root.localPosition = new Vector3(0, 1.06f, 0); }
            else if (type == "sprinkler")
            {
                var h = new GameObject("head");
                h.transform.SetParent(go.transform, false);
                h.transform.localPosition = new Vector3(0, .44f, 0);
                h.AddComponent<MeshFilter>().sharedMesh = MeshOf("sprinkler_head");
                h.AddComponent<MeshRenderer>().sharedMaterial = MaterialLibrary.Objects(false);
                head = h.transform;
            }
            return go;
        }
    }
}
