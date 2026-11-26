// Le hoi theo lich am (Docs/ThietKeGameplay.md muc 7.5). Moi le: trang tri lang + viec rieng.
//  - Tet (mung 1 thang 1): den long do, chau mai vang truoc nha; noi chuyen voi dan lang -> li xi.
//  - Ram thang gieng: hoa dang troi tren mat nuoc quanh lang; ong Bay cho 3 hoa dang, toi ra mep nuoc tha (E).
//  - Le cau ngu (ram thang 3): co duoi ca quanh nha long cho; cho hop bat ke thu may, dong khach, ca duoc gia (Market).
//  - Trung thu (ram thang 6): den ong sao truoc nha, dan lang cam long den buoi toi; thim Tam cho banh trung thu.
//  - Cung com moi (mung 10 thang 3): mam com canh lua trai; mang 3 com trang moi lang (noi voi ong Bay).
// Viec da lam trong nam luu bang co GameSession.Flags ("fest{nam}_..."), khong can truong luu rieng.
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SereneIsle
{
    public class Festivals : MonoBehaviour
    {
        public static Festivals Instance { get; private set; }

        public const string Tet = "Tết", RamGieng = "Rằm tháng giêng", CauNgu = "Lễ cầu ngư", TrungThu = "Trung thu", ComMoi = "Cúng cơm mới";

        static readonly Dictionary<string, int> LiXi = new Dictionary<string, int> { ["bay"] = 50, ["chin"] = 40, ["sau"] = 30, ["tam"] = 30 };
        static readonly string[] Houses = { "shack", "taphoa", "sauvuon", "chinhut" };

        GameObject _decor;
        int _decorDay = -1;
        string _decorFest;
        readonly List<(Material m, Color c)> _glows = new List<(Material, Color)>();
        readonly Dictionary<Color, Material> _glowByColor = new Dictionary<Color, Material>();
        readonly List<GameObject> _npcLanterns = new List<GameObject>();
        readonly System.Random _rng = new System.Random();

        GameSession S => GameSession.Instance;
        static IslandData D => IslandTerrain.Instance ? IslandTerrain.Instance.Data : null;

        /// <summary>Le hom nay (null neu khong co).</summary>
        public string Today => S ? LunarCalendar.Festival(S.Date) : null;
        public GameObject Decor => _decor;
        public int Released { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { Instance = null; }

        void Awake() { Instance = this; }
        void OnEnable() { Village.Talking += OnTalk; }
        void OnDisable() { Village.Talking -= OnTalk; }
        void OnDestroy()
        {
            if (Instance == this) Instance = null;
            foreach (var m in _glowByColor.Values) if (m) Destroy(m);
        }

        string Key(string what) => $"fest{S.Date.Year}_{what}";
        bool Done(string what) => S.Flags.Contains(Key(what));
        void Mark(string what) => S.Flags.Add(Key(what));

        // ------------------------------------------------------------------ moi khung
        void Update()
        {
            var s = S; var v = Village.Instance;
            if (!s || !v || !v.Built || D == null) return;
            if (_decorDay != s.Clock.Day) Rebuild();

            // den sang dan khi troi toi
            float night = Placeables.NightFactor(s.Clock.Hour);
            float k = .12f + .88f * night;
            foreach (var (m, c) in _glows) m.SetColor("_BaseColor", new Color(c.r, c.g, c.b, c.a * k));
            bool lanternsOn = night > .3f;
            foreach (var l in _npcLanterns) if (l && l.activeSelf != lanternsOn) l.SetActive(lanternsOn);
        }

        /// <summary>Dung lai trang tri cho le hom nay (goi khi qua ngay; test goi truc tiep).</summary>
        public void Rebuild()
        {
            var s = S; var d = D; var v = Village.Instance;
            if (_decor) Destroy(_decor);
            foreach (var l in _npcLanterns) if (l) Destroy(l);
            _npcLanterns.Clear();
            _decor = null;
            Released = 0;
            _decorDay = s.Clock.Day;
            _decorFest = Today;
            if (_decorFest == null || d == null || v == null) return;

            _decor = new GameObject("Festival · " + _decorFest);
            _decor.transform.SetParent(transform, false);
            var b = new MeshBuilder();
            switch (_decorFest)
            {
                case Tet:
                    foreach (var h in Houses)
                    {
                        var bd = v.Building(h);
                        if (bd == null) continue;
                        Frame(bd, out var fwd, out var right);
                        foreach (int side in new[] { -1, 1 })
                            HangingLantern(b, bd.door + right * (1.2f * side) - fwd * .2f, -right * side, false, side < 0);
                        MaiTree(b, bd.door + right * 1.9f + fwd * .3f);
                    }
                    break;
                case TrungThu:
                    foreach (var h in Houses)
                    {
                        var bd = v.Building(h);
                        if (bd == null) continue;
                        Frame(bd, out var fwd, out var right);
                        foreach (int side in new[] { -1, 1 })
                            HangingLantern(b, bd.door + right * (1.2f * side) - fwd * .2f, -right * side, true, side < 0);
                    }
                    foreach (var n in v.Npcs) NpcLantern(n);
                    break;
                case CauNgu:
                {
                    var cho = v.Building("cho");
                    if (cho != null) Bunting(b, cho, 2.7f, 2.0f);
                    var shack = v.Building("shack");
                    if (shack != null) Bunting(b, shack, 1.9f, 1.6f);
                    break;
                }
                case ComMoi:
                {
                    var p = d.TownPoint(1.6, 3.7);
                    RiceTray(b, new Vector3(p.x, d.GroundHeight(p.x, p.y), p.y), d.TownAngle);
                    break;
                }
                case RamGieng:
                    SpawnFloatingLanterns(18);
                    break;
            }
            if (b.VertexCount > 0)
            {
                var go = new GameObject("Decor");
                go.transform.SetParent(_decor.transform, false);
                go.AddComponent<MeshFilter>().sharedMesh = b.ToMesh("FestivalDecor");
                go.AddComponent<MeshRenderer>().sharedMaterial = MaterialLibrary.Objects(false);
            }
        }

        static void Frame(VillageBuilding bd, out Vector3 fwd, out Vector3 right)
        {
            fwd = new Vector3(Mathf.Sin(bd.facing), 0, Mathf.Cos(bd.facing));
            right = new Vector3(Mathf.Cos(bd.facing), 0, -Mathf.Sin(bd.facing));
        }

        static float Ground(Vector3 p) => D.GroundHeight(p.x, p.z);

        static void Add(MeshBuilder b, Mesh m, Vector3 pos, Quaternion rot, Vector3 scale, string hex)
            => b.Add(m, Matrix4x4.TRS(pos, rot, scale), MaterialLibrary.Hex(hex));

        // ------------------------------------------------------------------ do trang tri
        /// <summary>Cot tre + canh tay, treo den long do (Tet) hoac den ong sao (Trung thu).</summary>
        void HangingLantern(MeshBuilder b, Vector3 at, Vector3 armDir, bool star, bool alt)
        {
            at.y = Ground(at);
            const float h = 2.35f;
            Add(b, LowPolyShapes.Cylinder(.035f, .05f, h, 6), at + Vector3.up * (h * .5f), Quaternion.identity, Vector3.one, "#8a6a3a");
            var top = at + Vector3.up * (h - .05f);
            var arm = armDir.normalized * .45f;
            Add(b, LowPolyShapes.Box, top + arm * .5f, Quaternion.LookRotation(arm), new Vector3(.04f, .04f, .45f), "#8a6a3a");
            var lp = top + arm + Vector3.down * .38f;
            Add(b, LowPolyShapes.Box, top + arm + Vector3.down * .1f, Quaternion.identity, new Vector3(.01f, .2f, .01f), "#3a2a1a");
            if (!star)
            {
                Add(b, LowPolyShapes.Sphere86, lp, Quaternion.identity, new Vector3(.42f, .36f, .42f), "#e0342a");
                Add(b, LowPolyShapes.Cylinder(.11f, .11f, .05f, 8), lp + Vector3.up * .19f, Quaternion.identity, Vector3.one, "#f2c14a");
                Add(b, LowPolyShapes.Cylinder(.11f, .11f, .05f, 8), lp + Vector3.down * .19f, Quaternion.identity, Vector3.one, "#f2c14a");
                Add(b, LowPolyShapes.Box, lp + Vector3.down * .32f, Quaternion.identity, new Vector3(.035f, .2f, .035f), "#e0342a");
                Halo(lp, new Color(1f, .4f, .2f, .6f), 1.5f);
            }
            else
            {
                string col = alt ? "#f2c14a" : "#e0342a";
                var face = Quaternion.LookRotation(Vector3.Cross(armDir.normalized, Vector3.up)); // mat sao nhin ra truoc nha
                Add(b, StarMesh, lp, face, Vector3.one * 1.35f, col);
                Add(b, LowPolyShapes.Box, lp + Vector3.down * .3f, Quaternion.identity, new Vector3(.03f, .16f, .03f), "#f2c14a");
                Halo(lp, alt ? new Color(1f, .8f, .3f, .6f) : new Color(1f, .45f, .25f, .6f), 1.5f);
            }
        }

        static Mesh _star;
        /// <summary>Ngoi sao 5 canh day .06 trong mat XY (den ong sao Trung thu).</summary>
        static Mesh StarMesh
        {
            get
            {
                if (_star) return _star;
                const float ro = .2f, ri = .085f, h = .03f;
                var rim = new Vector3[10];
                for (int i = 0; i < 10; i++)
                {
                    float a = Mathf.PI / 2 + i * Mathf.PI / 5, r = i % 2 == 0 ? ro : ri;
                    rim[i] = new Vector3(Mathf.Cos(a) * r, Mathf.Sin(a) * r, 0);
                }
                var v = new List<Vector3>(); var t = new List<int>();
                void Tri(Vector3 a, Vector3 b2, Vector3 c) { int k = v.Count; v.Add(a); v.Add(b2); v.Add(c); t.Add(k); t.Add(k + 1); t.Add(k + 2); }
                var fz = new Vector3(0, 0, -h); var bz = new Vector3(0, 0, h);
                for (int i = 0; i < 10; i++)
                {
                    var p0 = rim[i]; var p1 = rim[(i + 1) % 10];
                    Tri(fz, p1 + fz, p0 + fz);           // mat truoc (-z)
                    Tri(bz, p0 + bz, p1 + bz);           // mat sau (+z)
                    Tri(p0 + fz, p1 + fz, p1 + bz);      // canh
                    Tri(p0 + fz, p1 + bz, p0 + bz);
                }
                var m = new Mesh { name = "StarLantern" };
                m.SetVertices(v); m.SetTriangles(t, 0);
                m.RecalculateNormals(); m.RecalculateBounds();
                return _star = m;
            }
        }

        /// <summary>Chau mai vang (Tet).</summary>
        static void MaiTree(MeshBuilder b, Vector3 at)
        {
            at.y = Ground(at);
            Add(b, LowPolyShapes.Cylinder(.24f, .18f, .32f, 10), at + Vector3.up * .16f, Quaternion.identity, Vector3.one, "#b0603a");
            Add(b, LowPolyShapes.Cylinder(.04f, .06f, .9f, 6), at + Vector3.up * .75f, Quaternion.Euler(0, 0, 6), Vector3.one, "#5a3a22");
            var rng = new System.Random(Mathf.RoundToInt(at.x * 31 + at.z * 17));
            for (int i = 0; i < 11; i++)
            {
                float a = (float)rng.NextDouble() * Mathf.PI * 2, r = .12f + (float)rng.NextDouble() * .32f;
                var p = at + new Vector3(Mathf.Cos(a) * r, 1.05f + (float)rng.NextDouble() * .45f, Mathf.Sin(a) * r);
                float s = .12f + (float)rng.NextDouble() * .08f;
                Add(b, LowPolyShapes.Sphere86, p, Quaternion.identity, Vector3.one * s, i % 4 == 0 ? "#f7d23a" : "#f2b81e");
            }
        }

        /// <summary>Day co duoi ca 4 canh quanh nha (cau ngu).</summary>
        static void Bunting(MeshBuilder b, VillageBuilding bd, float hx, float hz)
        {
            Frame(bd, out var fwd, out var right);
            var c = bd.center;
            var corners = new[] { c + right * hx + fwd * hz, c - right * hx + fwd * hz, c - right * hx - fwd * hz, c + right * hx - fwd * hz };
            const float h = 2.7f;
            var tops = new Vector3[4];
            for (int i = 0; i < 4; i++)
            {
                var p = corners[i]; p.y = Ground(p);
                Add(b, LowPolyShapes.Cylinder(.035f, .05f, h, 6), p + Vector3.up * (h * .5f), Quaternion.identity, Vector3.one, "#8a6a3a");
                tops[i] = p + Vector3.up * (h - .05f);
            }
            string[] cols = { "#e0342a", "#f2c14a", "#2f7fc4", "#4f9e3a", "#f28a3a" };
            int ci = 0;
            for (int i = 0; i < 4; i++)
            {
                Vector3 a = tops[i], e = tops[(i + 1) % 4];
                int n = Mathf.Max(3, Mathf.RoundToInt(Vector3.Distance(a, e) / .45f));
                for (int f = 0; f <= n; f++)
                {
                    float t = f / (float)n;
                    var p = Vector3.Lerp(a, e, t) + Vector3.down * (Mathf.Sin(t * Mathf.PI) * .35f);
                    if (f < n)
                    {
                        var q = Vector3.Lerp(a, e, (f + 1f) / n) + Vector3.down * (Mathf.Sin((f + 1f) / n * Mathf.PI) * .35f);
                        Add(b, LowPolyShapes.Box, (p + q) * .5f, Quaternion.LookRotation(q - p), new Vector3(.012f, .012f, Vector3.Distance(p, q)), "#3a2a1a");
                    }
                    if (f == 0 || f == n) continue;
                    var dir = (e - a).normalized;
                    var rot = Quaternion.LookRotation(Vector3.Cross(dir, Vector3.up)) * Quaternion.Euler(0, 0, 45);
                    Add(b, LowPolyShapes.Box, p + Vector3.down * .12f, rot, new Vector3(.27f, .27f, .015f), cols[ci++ % cols.Length]);
                }
            }
        }

        /// <summary>Mam com moi canh lua trai: ban thap, 5 chen com, dia trai cay.</summary>
        static void RiceTray(MeshBuilder b, Vector3 at, float angle)
        {
            var rot = Quaternion.Euler(0, angle * Mathf.Rad2Deg, 0);
            Add(b, LowPolyShapes.Box, at + Vector3.up * .28f, rot, new Vector3(1.3f, .06f, .8f), "#8a5a2e");
            foreach (var (x, z) in new[] { (-.55f, -.32f), (.55f, -.32f), (-.55f, .32f), (.55f, .32f) })
                Add(b, LowPolyShapes.Box, at + rot * new Vector3(x, .13f, z), rot, new Vector3(.06f, .26f, .06f), "#6a4423");
            for (int i = 0; i < 5; i++)
            {
                var p = at + rot * new Vector3(-.44f + i * .22f, .36f, -.15f);
                Add(b, LowPolyShapes.Cylinder(.08f, .05f, .08f, 10), p, Quaternion.identity, Vector3.one, "#f2efe6");
                Add(b, LowPolyShapes.Sphere86, p + Vector3.up * .05f, Quaternion.identity, new Vector3(.14f, .07f, .14f), "#fbf8f0");
            }
            var plate = at + rot * new Vector3(.25f, .33f, .2f);
            Add(b, LowPolyShapes.Cylinder(.2f, .16f, .03f, 12), plate, Quaternion.identity, Vector3.one, "#d8c8a0");
            Add(b, LowPolyShapes.Sphere86, plate + new Vector3(-.06f, .07f, 0), Quaternion.identity, Vector3.one * .12f, "#f28a3a");
            Add(b, LowPolyShapes.Sphere86, plate + new Vector3(.07f, .07f, .03f), Quaternion.identity, Vector3.one * .11f, "#7ac24e");
            var ins = at + rot * new Vector3(-.3f, .33f, .22f);
            Add(b, LowPolyShapes.Cylinder(.06f, .05f, .08f, 8), ins, Quaternion.identity, Vector3.one, "#b0603a");
            for (int i = 0; i < 3; i++) Add(b, LowPolyShapes.Box, ins + new Vector3((i - 1) * .025f, .15f, 0), Quaternion.Euler(0, 0, (i - 1) * 8), new Vector3(.008f, .24f, .008f), "#c84a2a");
        }

        /// <summary>Long den cam tay cho dan lang (Trung thu, chi sang buoi toi).</summary>
        void NpcLantern(Npc n)
        {
            var go = new GameObject("LongDen");
            go.transform.SetParent(n.transform, false);
            go.transform.localPosition = new Vector3(.32f, .55f, .18f);
            var b = new MeshBuilder();
            b.Box("#8a6a3a", 0, .3f, 0, .025f, .6f, .025f);
            Add(b, StarMesh, new Vector3(0, .62f, .06f), Quaternion.identity, Vector3.one * .8f, n.def.id == "tam" || n.def.id == "chin" ? "#e0342a" : "#f2c14a");
            go.AddComponent<MeshFilter>().sharedMesh = b.ToMesh("LongDen");
            go.AddComponent<MeshRenderer>().sharedMaterial = MaterialLibrary.Objects(false);
            var halo = MakeHalo(go.transform, new Color(1f, .7f, .3f, .6f), .9f);
            halo.transform.localPosition = new Vector3(0, .62f, .06f);
            go.SetActive(false);
            _npcLanterns.Add(go);
        }

        // ------------------------------------------------------------------ hoa dang
        static Mesh _lotus;
        static Mesh LotusMesh
        {
            get
            {
                if (_lotus) return _lotus;
                var b = new MeshBuilder();
                Add(b, LowPolyShapes.Sphere86, new Vector3(0, .03f, 0), Quaternion.identity, new Vector3(.34f, .07f, .34f), "#7ac24e");
                for (int i = 0; i < 8; i++)
                {
                    var r = Quaternion.Euler(0, i * 45, 0);
                    Add(b, LowPolyShapes.Sphere86, r * new Vector3(0, .09f, .1f), r * Quaternion.Euler(-35, 0, 0), new Vector3(.09f, .05f, .16f), i % 2 == 0 ? "#f28aa8" : "#f7b8cc");
                }
                Add(b, LowPolyShapes.Cylinder(.03f, .03f, .08f, 8), new Vector3(0, .14f, 0), Quaternion.identity, Vector3.one, "#fff3c8");
                return _lotus = b.ToMesh("HoaDang");
            }
        }

        static bool IsWater(IslandData d, int i, int j)
        {
            if (!d.InBounds(i, j)) return false;
            int k = d.Idx(i, j);
            return (d.Ocean[k] != 0 || d.Fresh[k] != 0) && d.WaterDepth(i, j) > .15f;
        }

        void SpawnFloatingLantern(Vector3 at, Vector2 vel)
        {
            var d = D;
            var go = new GameObject("HoaDang");
            go.transform.SetParent(_decor.transform, false);
            go.transform.position = at;
            go.AddComponent<MeshFilter>().sharedMesh = LotusMesh;
            go.AddComponent<MeshRenderer>().sharedMaterial = MaterialLibrary.Objects(false);
            var halo = MakeHalo(go.transform, new Color(1f, .75f, .4f, .6f), 1.2f);
            halo.transform.localPosition = new Vector3(0, .2f, 0);
            var f = go.AddComponent<FloatingLantern>();
            f.vel = vel;
            f.phase = (float)_rng.NextDouble() * 6;
        }

        void SpawnFloatingLanterns(int count)
        {
            var d = D;
            var c = d.TownPoint(0, 0);
            for (int t = 0, n = 0; t < 3000 && n < count; t++)
            {
                double a = _rng.NextDouble() * Math.PI * 2, r = 6 + _rng.NextDouble() * 26;
                int i = (int)Math.Round(c.x + Math.Cos(a) * r), j = (int)Math.Round(c.y + Math.Sin(a) * r);
                if (!IsWater(d, i, j)) continue;
                float ang = (float)(_rng.NextDouble() * Math.PI * 2);
                SpawnFloatingLantern(new Vector3(i, d.WaterSurface(d.Idx(i, j)), j), new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * .12f);
                n++;
            }
        }

        /// <summary>Phim E: co hoa dang, troi toi, dung o mep nuoc -> tha.</summary>
        public Action NearbyAction(Vector3 pos, out string label)
        {
            label = null;
            var s = S; var d = D;
            if (!s || d == null || s.Inventory.Count("hoa_dang") <= 0) return null;
            if (Placeables.NightFactor(s.Clock.Hour) < .4f) return null;
            if (!NearestWater(pos, out var w)) return null;
            label = "E: Thả hoa đăng";
            return () => Release(w);
        }

        static bool NearestWater(Vector3 pos, out Vector3 w)
        {
            var d = D;
            w = default;
            float best = 2.6f;
            int pi = Mathf.RoundToInt(pos.x), pj = Mathf.RoundToInt(pos.z);
            for (int dj = -3; dj <= 3; dj++)
                for (int di = -3; di <= 3; di++)
                {
                    int i = pi + di, j = pj + dj;
                    if (!IsWater(d, i, j)) continue;
                    float dd = Vector2.Distance(new Vector2(i, j), new Vector2(pos.x, pos.z));
                    if (dd < best) { best = dd; w = new Vector3(i, d.WaterSurface(d.Idx(i, j)), j); }
                }
            return best < 2.6f;
        }

        public void Release(Vector3 water)
        {
            var s = S;
            if (s.Inventory.Count("hoa_dang") <= 0) return;
            s.Inventory.Take("hoa_dang", 1);
            if (!_decor) { _decor = new GameObject("Festival · hoa đăng"); _decor.transform.SetParent(transform, false); }
            var away = new Vector2(water.x - s.Player.transform.position.x, water.z - s.Player.transform.position.z).normalized;
            SpawnFloatingLantern(water, away * .25f);
            Released++;
            bool fest = Today == RamGieng;
            s.Stats.Calm = Mathf.Min(100, s.Stats.Calm + (fest ? 3 : 1));
            if (s.Body) s.Body.PlayAct("pickup");
            s.Say(fest ? "Hoa đăng trôi ra xa, lung linh trên mặt nước. Bạn thầm ước một điều. (+Bình yên)" : "Hoa đăng trôi lặng lẽ trên mặt nước. (+Bình yên)");
        }

        // ------------------------------------------------------------------ hoi thoai
        void OnTalk(Village.TalkContext ctx)
        {
            var s = S;
            string fest = Today, id = ctx.npc.def.id, name = ctx.npc.def.name;
            if (fest == null) return;
            switch (fest)
            {
                case Tet:
                    ctx.line = id switch
                    {
                        "bay" => "Chúc mừng năm mới! Năm nay đảo mình xanh hơn, biển êm hơn nghen con.",
                        "chin" => "Năm mới an khang nghen con! Ghe bác chở đầy hoa với dưa hấu cho xóm rồi đó.",
                        "sau" => "Chúc con năm mới vườn tược tốt tươi, trồng cây gì cũng sai trái!",
                        _ => "Năm mới phát tài nghen con! Ghé tiệm thím có bánh chưng, mứt gừng đó.",
                    };
                    if (LiXi.TryGetValue(id, out int amt) && !Done("lixi_" + id))
                    {
                        Mark("lixi_" + id);
                        s.Coins += amt;
                        ctx.line += $"\n\n{name} lì xì cho bạn một bao đỏ: {amt} xu.";
                        s.Say($"+{amt} xu lì xì");
                    }
                    break;
                case RamGieng:
                    if (id == "bay" && !Done("hoadang"))
                    {
                        Mark("hoadang");
                        s.Give("hoa_dang", 3, true);
                        ctx.line = "Rằm tháng giêng rồi. Ông cho con ba cái hoa đăng, tối ra mép nước thả, cầu cho đảo bình yên cả năm. (+3 Hoa đăng)";
                    }
                    else ctx.line = "Tối nay rằm, cả xóm ra mép nước thả hoa đăng. Đẹp lắm con.";
                    break;
                case CauNgu:
                    ctx.line = id switch
                    {
                        "bay" => "Lễ cầu ngư, xóm mình cúng cá Ông cho biển êm, ghe về đầy khoang.",
                        "chin" => "Bữa nay chợ lớn nhất năm, khách đất liền ra đông nghẹt. Có cá thì đem bán, được giá lắm!",
                        "sau" => "Đầu mùa cá rồi. Con bày sạp ở nhà lồng chợ đi, nay họp cả ngày.",
                        _ => "Cá bữa nay bán được giá nghen con. Khách đất liền mê đặc sản đảo lắm.",
                    };
                    break;
                case TrungThu:
                    if (id == "tam" && !Done("banhtt"))
                    {
                        Mark("banhtt");
                        s.Give("banh_trung_thu", 2, true);
                        ctx.line = "Trung thu vui nghen con! Thím cho con hai cái bánh trung thu, tối ngắm trăng mà ăn. (+2 Bánh trung thu)";
                    }
                    else ctx.line = "Tối nay rước đèn trung thu, trăng rằm tháng sáu tròn vành vạnh. Ra xóm chơi nghen.";
                    break;
                case ComMoi:
                    if (id == "bay")
                    {
                        if (Done("commoi")) ctx.line = "Nồi cơm mới của con thơm thiệt. Cả xóm nhắc hoài.";
                        else if (s.Inventory.Count("com") >= 3)
                        {
                            ctx.line = "Lúa đông xuân vô bồ rồi. Con có cơm gạo mới thì mời cả làng, cúng ông bà luôn.";
                            ctx.buttons.Add(("Mời cả làng cơm mới (3 cơm trắng)", OfferRice));
                        }
                        else ctx.line = "Bữa nay cúng cơm mới. Con nấu ba chén cơm trắng ở lửa trại rồi mang tới đây, ông dọn mâm mời cả làng.";
                    }
                    else ctx.line = "Nghe nói nay cúng cơm mới, có ai nấu cơm gạo mới mời làng hông ta?";
                    break;
            }
        }

        public void OfferRice()
        {
            var s = S;
            if (Done("commoi") || s.Inventory.Count("com") < 3) return;
            s.Inventory.Take("com", 3);
            Mark("commoi");
            s.Stats.Calm = Mathf.Min(100, s.Stats.Calm + 10);
            s.Coins += 150;
            s.Give("seed_lua", 5, true);
            s.Give("seed_nep", 3, true);
            ActionUI.Instance?.ShowDialog("Cúng cơm mới",
                "Cả xóm ngồi quanh mâm cơm bên lửa trại. Ông Bảy thắp nén nhang, khấn ông bà. Cơm gạo mới dẻo thơm, ai cũng xin thêm chén nữa.\n\nDân làng góp quà cảm ơn: 150 xu, 5 hạt lúa, 3 hạt nếp. (+10 Bình yên)",
                ("Ấm lòng quá", null));
        }

        // ------------------------------------------------------------------ hao quang
        static Mesh _glowMesh;
        /// <summary>Tam vuong UV 0..1 mau trang: shader Glow _Radial = 1 ve quang tron mem.</summary>
        static Mesh GlowMesh
        {
            get
            {
                if (_glowMesh) return _glowMesh;
                var m = Instantiate(ProceduralMeshes.Primitive(PrimitiveType.Quad));
                var c = new Color[m.vertexCount];
                for (int i = 0; i < c.Length; i++) c[i] = Color.white;
                m.colors = c;
                return _glowMesh = m;
            }
        }

        Material GlowMat(Color c)
        {
            if (_glowByColor.TryGetValue(c, out var m) && m) return m;
            m = new Material(Shader.Find("SereneIsle/Glow")) { name = "SI_FestivalGlow", renderQueue = 3000 };
            m.SetColor("_BaseColor", c);
            m.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.One);
            m.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.One);
            m.SetFloat("_Radial", 1);
            _glowByColor[c] = m;
            _glows.Add((m, c));
            return m;
        }

        readonly List<Transform> _halos = new List<Transform>();

        void LateUpdate()
        {
            var cam = Camera.main;
            if (!cam) return;
            _halos.RemoveAll(t => !t);
            foreach (var t in _halos) t.rotation = cam.transform.rotation;
        }

        GameObject MakeHalo(Transform parent, Color c, float size)
        {
            var go = new GameObject("Halo");
            go.transform.SetParent(parent, false);
            go.transform.localScale = Vector3.one * size;
            go.AddComponent<MeshFilter>().sharedMesh = GlowMesh;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = GlowMat(c);
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;
            _halos.Add(go.transform);
            return go;
        }

        void Halo(Vector3 at, Color c, float size)
        {
            var h = MakeHalo(_decor.transform, c, size);
            h.transform.position = at;
        }
    }

    /// <summary>Hoa dang troi: nhap nho theo song, troi cham, quay dau khi gap bo.</summary>
    public class FloatingLantern : MonoBehaviour
    {
        public Vector2 vel;
        public float phase;

        void Update()
        {
            var d = IslandTerrain.Instance ? IslandTerrain.Instance.Data : null;
            if (d == null) return;
            float dt = Time.deltaTime;
            var p = transform.position;
            var next = new Vector3(p.x + vel.x * dt, p.y, p.z + vel.y * dt);
            int i = Mathf.RoundToInt(next.x), j = Mathf.RoundToInt(next.z);
            if (!d.InBounds(i, j) || d.WaterDepth(next.x, next.z) < .12f) { vel = -vel; next = p; i = Mathf.RoundToInt(p.x); j = Mathf.RoundToInt(p.z); }
            if (!d.InBounds(i, j)) return;
            next.y = d.WaterSurface(d.Idx(i, j)) + Mathf.Sin(Time.time * 1.6f + phase) * .03f;
            transform.position = next;
            transform.rotation = Quaternion.Euler(Mathf.Sin(Time.time * 1.3f + phase) * 4, Time.time * 8 + phase * 40, 0);
        }
    }
}
