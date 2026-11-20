// Con trung bay quanh nguoi choi, bat bang vot (ban web: BUGS dong 323, spawnBug dong 1149, cap nhat dong 1912-1917).
// - Toi da 11 con, sinh trong vong 8-22 o quanh nguoi choi theo vung (biome) va ngay/dem (du lieu: ItemCatalogData).
// - 24 loai, nhieu loai Viet Nam (chuon chuon, chau chau, cao cao, de men, de trui, ve sau, ca cuong...), moi kieu co
//   dang va cach di rieng: bay chap chon (buom, ong), luot roi lao (chuon chuon), bung nhay (chau chau, de),
//   bo (bo canh cung, bo ngua), dau than cay (ve sau), phat sang (dom dom).
// - Chay (Shift) lai gan thi chung bay / nhay xa. Song 60-120 giay hoac bien mat khi nguoi choi di xa > 32 o.
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SereneIsle
{
    public class Critters : MonoBehaviour
    {
        public static Critters Instance { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { Instance = null; }

        public const int MaxBugs = 11;
        public const int RarePrice = 100;

        public enum Kind { Flutter, Dart, Hop, Crawl, Perch, Glow }

        public static Kind KindOf(string id) => id switch
        {
            "b_firefly" => Kind.Glow,
            "b_dragonfly" or "b_damsel" => Kind.Dart,
            "b_grasshopper" or "b_locust" or "b_cricket" or "b_sandcricket" => Kind.Hop,
            "b_beetle" or "b_ladybug" or "b_shieldbug" or "b_stag" or "b_rhino" or "b_mantis" or "b_fieldcricket" or "b_molecricket" or "b_giantbug" => Kind.Crawl,
            "b_cicada" => Kind.Perch,
            _ => Kind.Flutter,
        };

        public sealed class Bug
        {
            public string id;
            public Kind kind;
            public Transform g, w1, w2, w3, w4, glow;
            public float hx, hz, hy, t, life, next, jump;
            public Vector3 from, to;
            public Vector3 Position => g.position;
        }

        public readonly List<Bug> Bugs = new List<Bug>();
        public bool spawnEnabled = true;
        readonly System.Random _rng = new System.Random();
        static Mesh _box, _ball;
        Material _glowMat;

        GameSession S => GameSession.Instance;
        IslandData D => IslandTerrain.Instance ? IslandTerrain.Instance.Data : null;

        void Awake()
        {
            Instance = this;
            if (!_ball) _ball = ProceduralMeshes.Sphere;
            if (!_box) _box = ProceduralMeshes.Primitive(PrimitiveType.Cube);
            // dom dom: sang hon han anh sang ban dem
            _glowMat = new Material(MaterialLibrary.Lambert) { name = "SI_FireflyGlow" };
            _glowMat.SetColor("_BaseColor", new Color(4f, 4.4f, 1.8f, 1));
            _glowMat.SetFloat("_VertexColorWeight", 0);
            _glowMat.SetFloat("_FogScale", .3f);
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
            if (_glowMat) Destroy(_glowMat);
        }

        float R() => (float)_rng.NextDouble();
        float R(float a, float b) => a + (b - a) * R();

        // ------------------------------------------------------------------ sinh
        /// <summary>spawnBug: tim o dat quanh nguoi choi co loai con trung hop vung / gio.</summary>
        public Bug Spawn()
        {
            var s = S; var d = D;
            if (!s || !s.Player || d == null) return null;
            bool night = s.Clock.IsNight;
            var all = s.Items.items.Where(it => it && it.category == ItemCategory.Bug).ToList();
            var pp = s.Player.transform.position;
            for (int t = 0; t < 30; t++)
            {
                float a = R() * 6.28f, r = 8 + R() * 14;
                int i = Mathf.RoundToInt(pp.x + Mathf.Cos(a) * r), j = Mathf.RoundToInt(pp.z + Mathf.Sin(a) * r);
                if (i < 1 || j < 1 || i >= d.N - 1 || j >= d.N - 1) continue;
                int k = j * d.N + i;
                if (d.Level[k] == 0 || d.Solid[k] != 0) continue;
                string bio = ((int)d.Bio[k]).ToString();
                var list = all.Where(b => (b.activeTime == "any" || (b.activeTime == "night") == night) && b.habitats != null && b.habitats.Contains(bio)).ToList();
                if (list.Count == 0) continue;
                var pick = list[_rng.Next(list.Count)];
                // loai hiem (gia >= 100 xu): 1/5 co hoi, con lai doi sang loai thuong
                if (pick.price >= RarePrice && R() > .2f)
                {
                    var common = list.Where(b => b.price < RarePrice).ToList();
                    if (common.Count == 0) continue;
                    pick = common[_rng.Next(common.Count)];
                }
                var kind = KindOf(pick.id);
                Vector3 pos;
                if (kind == Kind.Perch)
                {
                    // ve sau: bam than cay to canh do
                    if (!TreeNear(d, i, j, out int tk)) continue;
                    int ti = tk % d.N, tj = tk / d.N;
                    var dir = new Vector3(i - ti, 0, j - tj).normalized;
                    pos = new Vector3(ti, d.HT[tk] + R(1.1f, 1.8f), tj) + dir * .22f;
                }
                else
                {
                    float h = kind switch { Kind.Crawl => .05f, Kind.Hop => .05f, Kind.Dart => R(.6f, 1.4f), _ => R(.8f, 1.6f) };
                    pos = new Vector3(i, d.HT[k] + h, j);
                }
                return SpawnAt(pick.id, pos);
            }
            return null;
        }

        static bool TreeNear(IslandData d, int i, int j, out int k)
        {
            for (int dj = -1; dj <= 1; dj++)
                for (int di = -1; di <= 1; di++)
                {
                    k = (j + dj) * d.N + i + di;
                    int o = d.ObjType[k];
                    if (o != 0 && WorldObjects.Info((WorldObjectType)(o - 1)).big) return true;
                }
            k = -1;
            return false;
        }

        /// <summary>Tha 1 con tai vi tri (dung khi sinh va trong test).</summary>
        public Bug SpawnAt(string id, Vector3 pos)
        {
            var def = ItemDatabase.Instance.Get(id);
            if (!def) return null;
            var kind = KindOf(id);
            var g = new GameObject("Bug_" + id).transform;
            g.SetParent(transform, false);
            g.position = pos;
            var b = new Bug { id = id, kind = kind, g = g, hx = pos.x, hz = pos.z, hy = pos.y, t = R() * 10, life = R(60, 120), next = R(.5f, 2f), from = pos, to = pos };
            BuildBody(b, def);
            Bugs.Add(b);
            return b;
        }

        /// <summary>Dang 3D theo kieu (hop dai + cau, canh mong). Kich thuoc that ~ 0.2-0.5 o de con nhin thay.</summary>
        void BuildBody(Bug b, ItemDefinition def)
        {
            var g = b.g;
            var c1 = MaterialLibrary.Solid(def.iconColor);
            var c2 = MaterialLibrary.Solid(def.iconColor2);
            Transform Wing(Vector3 pos, Vector3 size, Material m)
            {
                var p = new GameObject("w").transform; p.SetParent(g, false); p.localPosition = pos;
                Part(p, _box, m, new Vector3(Mathf.Sign(pos.x) * size.x / 2, 0, 0), size);
                return p;
            }
            switch (b.kind)
            {
                case Kind.Glow:
                    Part(g, _ball, _glowMat, Vector3.zero, Vector3.one * .14f);
                    b.glow = Part(g, _ball, _glowMat, Vector3.zero, Vector3.one * .3f);
                    b.w1 = Wing(new Vector3(.03f, .03f, 0), new Vector3(.12f, .006f, .08f), c1);
                    b.w2 = Wing(new Vector3(-.03f, .03f, 0), new Vector3(.12f, .006f, .08f), c1);
                    break;
                case Kind.Dart: // chuon chuon: than dai, 4 canh hep
                    Part(g, _box, c1, new Vector3(0, 0, -.1f), new Vector3(.05f, .05f, .42f));
                    Part(g, _ball, c1, new Vector3(0, 0, .14f), new Vector3(.11f, .1f, .11f));
                    b.w1 = Wing(new Vector3(.02f, .03f, .07f), new Vector3(.3f, .006f, .07f), c2);
                    b.w2 = Wing(new Vector3(-.02f, .03f, .07f), new Vector3(.3f, .006f, .07f), c2);
                    b.w3 = Wing(new Vector3(.02f, .03f, 0), new Vector3(.27f, .006f, .065f), c2);
                    b.w4 = Wing(new Vector3(-.02f, .03f, 0), new Vector3(.27f, .006f, .065f), c2);
                    break;
                case Kind.Hop: // chau chau, cao cao, de: than dai, canh xep tren lung, chan sau gap
                    Part(g, _ball, c1, new Vector3(0, .06f, 0), new Vector3(.09f, .09f, .28f));
                    Part(g, _ball, c1, new Vector3(0, .08f, .14f), new Vector3(.08f, .09f, .09f));
                    Part(g, _box, c2, new Vector3(0, .1f, -.03f), new Vector3(.07f, .025f, .24f));
                    foreach (float sx in new[] { -1f, 1f })
                    {
                        var leg = Part(g, _box, c1, new Vector3(sx * .06f, .08f, -.08f), new Vector3(.025f, .025f, .2f));
                        leg.localRotation = Quaternion.Euler(-30, 0, 0);
                    }
                    break;
                case Kind.Crawl:
                    if (b.id == "b_mantis")
                    {
                        // bo ngua: than manh dung xien, cang truoc gap
                        var body = Part(g, _box, c1, new Vector3(0, .1f, 0), new Vector3(.05f, .05f, .3f)); body.localRotation = Quaternion.Euler(-25, 0, 0);
                        Part(g, _ball, c1, new Vector3(0, .2f, .16f), new Vector3(.08f, .07f, .07f));
                        Part(g, _box, c2, new Vector3(0, .12f, -.03f), new Vector3(.08f, .02f, .2f)).localRotation = Quaternion.Euler(-25, 0, 0);
                        foreach (float sx in new[] { -1f, 1f }) Part(g, _box, c1, new Vector3(sx * .04f, .14f, .14f), new Vector3(.025f, .12f, .025f)).localRotation = Quaternion.Euler(30, 0, 0);
                        break;
                    }
                    bool flat = b.id == "b_giantbug" || b.id == "b_shieldbug";
                    float sz = b.id == "b_ladybug" ? .7f : b.id == "b_stag" || b.id == "b_rhino" || b.id == "b_giantbug" ? 1.35f : 1f;
                    Part(g, _ball, c1, new Vector3(0, .05f * sz, 0), new Vector3(.14f, .08f, .2f) * sz);
                    Part(g, _ball, c2, new Vector3(0, .08f * sz, -.01f), new Vector3(.16f, flat ? .06f : .1f, .18f) * sz);
                    Part(g, _ball, c1, new Vector3(0, .06f * sz, .11f * sz), new Vector3(.08f, .06f, .07f) * sz);
                    if (b.id == "b_ladybug") foreach (var (x, z) in new[] { (.04f, .02f), (-.04f, .02f), (.03f, -.05f), (-.03f, -.05f) })
                            Part(g, _ball, MaterialLibrary.Solid("#1a1a1a"), new Vector3(x, .12f * sz, z), Vector3.one * .03f);
                    if (b.id == "b_stag") foreach (float sx in new[] { -1f, 1f })
                            Part(g, _box, c1, new Vector3(sx * .04f, .08f, .25f), new Vector3(.025f, .025f, .14f)).localRotation = Quaternion.Euler(0, -sx * 20, 0);
                    if (b.id == "b_rhino") Part(g, _box, c1, new Vector3(0, .14f, .2f), new Vector3(.03f, .03f, .14f)).localRotation = Quaternion.Euler(-40, 0, 0);
                    break;
                case Kind.Perch: // ve sau: than bau, canh trong xep sau lung
                    Part(g, _ball, c1, Vector3.zero, new Vector3(.12f, .1f, .24f));
                    Part(g, _box, MaterialLibrary.Solid("#1a1a1a"), new Vector3(0, .02f, .1f), new Vector3(.12f, .03f, .03f));
                    b.w1 = Wing(new Vector3(.02f, .04f, -.02f), new Vector3(.08f, .006f, .26f), c2);
                    b.w2 = Wing(new Vector3(-.02f, .04f, -.02f), new Vector3(.08f, .006f, .26f), c2);
                    break;
                default: // buom, buom dem, ong
                    bool bee = b.id == "b_bee";
                    float ws = b.id == "b_birdwing" ? 1.5f : bee ? .5f : 1f;
                    if (bee)
                    {
                        Part(g, _ball, c1, Vector3.zero, new Vector3(.13f, .12f, .19f));
                        Part(g, _box, MaterialLibrary.Solid("#2a2016"), new Vector3(0, 0, -.02f), new Vector3(.135f, .125f, .03f));
                        Part(g, _box, MaterialLibrary.Solid("#2a2016"), new Vector3(0, 0, .04f), new Vector3(.125f, .115f, .025f));
                    }
                    else Part(g, _ball, c1, Vector3.zero, new Vector3(.07f, .07f, .22f));
                    b.w1 = Wing(new Vector3(.02f, .01f, 0), new Vector3(.26f, .006f, .2f) * ws, c2);
                    b.w2 = Wing(new Vector3(-.02f, .01f, 0), new Vector3(.26f, .006f, .2f) * ws, c2);
                    break;
            }
        }

        static Transform Part(Transform parent, Mesh mesh, Material mat, Vector3 pos, Vector3 scale)
        {
            var go = new GameObject("p");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localScale = scale;
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = mat;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return go.transform;
        }

        public void Remove(Bug b)
        {
            if (b.g) Destroy(b.g.gameObject);
            Bugs.Remove(b);
        }

        public void Clear() { foreach (var b in Bugs.ToArray()) Remove(b); }

        /// <summary>Con trung gan nhat trong tam vot, o phia truoc mat (ban web: useTool 'net').</summary>
        public Bug Nearest(Vector3 from, float facing, float reach)
        {
            float fx = Mathf.Sin(facing), fz = Mathf.Cos(facing), bd = 99;
            Bug best = null;
            foreach (var b in Bugs)
            {
                float dx = b.g.position.x - from.x, dz = b.g.position.z - from.z, dd = Mathf.Sqrt(dx * dx + dz * dz);
                if (dd < reach && (dx * fx + dz * fz) / (dd > 0 ? dd : 1) > .2f && dd < bd) { bd = dd; best = b; }
            }
            return best;
        }

        // ------------------------------------------------------------------ moi khung
        void Update()
        {
            var s = S; var d = D;
            if (!s || !s.Player || d == null) return;
            float dt = Time.deltaTime;
            if (s.Transitioning) { Clear(); return; }
            if (spawnEnabled && s.Active && Bugs.Count < MaxBugs && R() < dt * 2) Spawn();

            var pl = s.Player.transform.position;
            bool scare = s.Player.Moving && s.Player.Sprinting;
            for (int n = Bugs.Count - 1; n >= 0; n--)
            {
                var b = Bugs[n];
                b.t += dt; b.life -= dt;
                var p = b.g.position;
                float dp = Mathf.Sqrt((p.x - pl.x) * (p.x - pl.x) + (p.z - pl.z) * (p.z - pl.z));
                if (b.life <= 0 || dp > 32) { Remove(b); continue; }
                bool scared = dp < 1.4f && scare;
                switch (b.kind)
                {
                    case Kind.Crawl: Crawl(b, d); break;
                    case Kind.Hop: Hop(b, d, dt, scared, pl); break;
                    case Kind.Dart: Dart(b, d, dt, scared, pl); break;
                    case Kind.Perch: Perch(b); break;
                    default: Flutter(b, scared, pl); break;
                }
            }
        }

        static float Ground(IslandData d, float x, float z) => d.HeightAt(x, z);

        void Crawl(Bug b, IslandData d)
        {
            var p = new Vector3(b.hx + Mathf.Sin(b.t * .6f) * .8f, 0, b.hz + Mathf.Cos(b.t * .45f) * .8f);
            p.y = Ground(d, p.x, p.z) + .02f;
            var dir = p - b.g.position; dir.y = 0;
            if (dir.sqrMagnitude > 1e-6f) b.g.rotation = Quaternion.LookRotation(dir);
            b.g.position = p;
        }

        /// <summary>Chau chau / de: dung yen roi bung nhay 0.6-1.2 o theo duong cong.</summary>
        void Hop(Bug b, IslandData d, float dt, bool scared, Vector3 pl)
        {
            if (b.jump > 0)
            {
                b.jump -= dt;
                float q = 1 - Mathf.Clamp01(b.jump / .42f);
                var p = Vector3.Lerp(b.from, b.to, q);
                p.y = Mathf.Lerp(b.from.y, b.to.y, q) + Mathf.Sin(q * Mathf.PI) * .38f;
                b.g.position = p;
                return;
            }
            b.next -= dt;
            if (b.next > 0 && !scared) return;
            b.next = R(1f, 3f);
            float a = scared ? Mathf.Atan2(b.g.position.x - pl.x, b.g.position.z - pl.z) + R(-.6f, .6f) : R(0, 6.28f);
            // khong nhay xa khoi cho cu qua 2.5 o
            var home = new Vector2(b.hx, b.hz);
            var to = new Vector3(b.g.position.x + Mathf.Sin(a) * R(.6f, 1.2f), 0, b.g.position.z + Mathf.Cos(a) * R(.6f, 1.2f));
            if (Vector2.Distance(new Vector2(to.x, to.z), home) > 2.5f && !scared) to = new Vector3(b.hx, 0, b.hz);
            to.y = Ground(d, to.x, to.z) + .02f;
            b.from = b.g.position; b.to = to; b.jump = .42f;
            var dir = to - b.from; dir.y = 0;
            if (dir.sqrMagnitude > 1e-6f) b.g.rotation = Quaternion.LookRotation(dir);
        }

        /// <summary>Chuon chuon: luot dung yen, roi lao nhanh toi diem moi; canh rung nhanh.</summary>
        void Dart(Bug b, IslandData d, float dt, bool scared, Vector3 pl)
        {
            float f = Mathf.Sin(b.t * 60) * 18;
            b.w1.localRotation = Quaternion.Euler(0, 0, f); b.w2.localRotation = Quaternion.Euler(0, 0, -f);
            b.w3.localRotation = Quaternion.Euler(0, 0, -f); b.w4.localRotation = Quaternion.Euler(0, 0, f);
            b.next -= dt;
            if (b.next <= 0 || scared)
            {
                b.next = R(1.2f, 3f);
                float a = R(0, 6.28f), r = R(1f, 2.5f);
                var to = new Vector3(b.hx + Mathf.Sin(a) * r, 0, b.hz + Mathf.Cos(a) * r);
                if (scared) to = b.g.position + (b.g.position - pl).normalized * 3;
                to.y = Ground(d, to.x, to.z) + R(.6f, 1.4f);
                b.from = b.g.position; b.to = to;
                var dir = to - b.from; dir.y = 0;
                if (dir.sqrMagnitude > 1e-6f) b.g.rotation = Quaternion.LookRotation(dir);
            }
            var p = Vector3.Lerp(b.g.position, b.to, Mathf.Min(1, dt * 5));
            p.y += Mathf.Sin(b.t * 4) * .002f;
            b.g.position = p;
        }

        void Perch(Bug b)
        {
            float f = Mathf.Sin(b.t * 70) * 3;
            b.w1.localRotation = Quaternion.Euler(0, 0, f); b.w2.localRotation = Quaternion.Euler(0, 0, -f);
            b.g.rotation = Quaternion.Euler(-70, (b.hx * 37 + b.hz * 11) % 360, 0); // bam doc than cay
        }

        /// <summary>Buom, ong, dom dom: vong bay cham (ban web).</summary>
        void Flutter(Bug b, bool scared, Vector3 pl)
        {
            bool fl = b.kind == Kind.Glow, bee = b.id == "b_bee";
            float sp = bee ? 1.8f : 1;
            var p = new Vector3(b.hx + Mathf.Sin(b.t * .7f * sp) * 1.4f + Mathf.Sin(b.t * 2.1f) * .3f, b.hy + Mathf.Sin(b.t * (fl ? 1.2f : 3)) * .25f, b.hz + Mathf.Cos(b.t * .5f * sp) * 1.4f);
            float f = Mathf.Sin(b.t * (fl ? 20 : bee ? 60 : 24)) * (bee ? 30 : 52);
            b.w1.localRotation = Quaternion.Euler(0, 0, f);
            b.w2.localRotation = Quaternion.Euler(0, 0, -f);
            b.g.rotation = Quaternion.Euler(0, (b.t * .7f * sp + Mathf.PI / 2) * Mathf.Rad2Deg, 0);
            if (fl && b.glow) b.glow.localScale = Vector3.one * (.22f + Mathf.Abs(Mathf.Sin(b.t * 2)) * .14f);
            b.g.position = p;
            if (scared) { b.hx += (p.x - pl.x) * .04f; b.hz += (p.z - pl.z) * .04f; }
        }
    }
}
