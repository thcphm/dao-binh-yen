// Ve cay / da / co / hoa bang GPU instancing (Graphics.RenderMeshInstanced), chia manh 32x32 o.
// Ban web: streamObjs (tier: <40 day du + bong, <64 day du, < suong mu: LOD cho cay to), buildTufts / hoa (< 110).
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace SereneIsle
{
    public class VegetationRenderer : MonoBehaviour
    {
        [Header("Khoang cach (o)")]
        public float shadowDistance = 40;
        public float fullDistance = 64;
        public float farDistance = 150;
        public float grassDistance = 110;

        const int CH = 32;        // OCH / GCH cua ban web
        const int MaxBatch = 1023;

        sealed class Entry
        {
            public string name;
            public Mesh mesh;
            public int submesh;       // mo hinh Quaternius: than (vo cay) va la la 2 submesh
            public int variant;       // bien the mo hinh (CommonTree_1..5...)
            public Material mat;
            public MaterialPropertyBlock props;
            public bool lodOnly;      // chi ve o tier 1 (LOD cay to)
            public bool grass;        // co/hoa: hien theo grassDistance, khong bong
            public List<Matrix4x4>[] chunks;
            public Matrix4x4[] near = new Matrix4x4[0], mid = new Matrix4x4[0];
            public int nearCount, midCount;
        }

        readonly List<Entry> _entries = new List<Entry>();
        IslandData _d;
        int _cn;
        Vector2 _lastCam = new Vector2(1e9f, 1e9f);
        Bounds _bounds;
        byte[] _tier;   // 0 an, 1 LOD, 2 day du, 3 day du + bong
        bool[] _grassOn;

        [Tooltip("Dung mo hinh Quaternius (Resources/NatureKit) cho cac loai co san")]
        public bool useNatureKit = true;

        /// <summary>So loai cay/da dang dung mo hinh Quaternius.</summary>
        public int KitTypes { get; private set; }
        public int VisibleInstances { get; private set; }
        public int TotalObjects { get; private set; }
        public int TotalGrass { get; private set; }

        // ================================================================== khoi tao
        public void Init(IslandData d, List<WorldObjects.Placement> objects)
        {
            _d = d;
            _cn = Mathf.CeilToInt(d.N / (float)CH);
            _tier = new byte[_cn * _cn];
            _grassOn = new bool[_cn * _cn];
            _bounds = new Bounds(new Vector3(d.N / 2f, 0, d.N / 2f), new Vector3(d.N + 100, 40, d.N + 100));

            var objMat = MaterialLibrary.Objects(false);
            var vegMat = MaterialLibrary.Objects(true);

            // ---- cay, da: mo hinh Quaternius neu co (NatureKit), con lai dung mo hinh tu dung cua ban web ----
            var kit = useNatureKit ? NatureKit.Instance : null;
            var full = new List<Entry>[WorldObjects.Count];   // [loai] -> cac entry cua tung bien the (moi submesh 1 entry)
            var variantOf = new int[WorldObjects.Count];      // so bien the
            var lod = new Entry[WorldObjects.Count];
            KitTypes = 0;
            for (int t = 0; t < WorldObjects.Count; t++)
            {
                var type = (WorldObjectType)t;
                var info = WorldObjects.Info(type);
                var mesh = WorldObjects.BuildMesh(type);
                full[t] = new List<Entry>();
                var set = kit ? kit.Get(type) : null;
                Mesh lodSource = mesh;
                Color? lodColor = null;
                if (set != null)
                {
                    float h = mesh.bounds.max.y * set.heightScale;
                    for (int v = 0; v < set.models.Count; v++)
                    {
                        var built = kit.Build(set.models[v], h, info.sway, set.textureOverride, set.tintOverride);
                        if (built == null) continue;
                        for (int s = 0; s < built.mesh.subMeshCount; s++)
                        {
                            var e = NewEntry($"{type}_{v}_{s}", built.mesh, built.materials[s], null);
                            e.submesh = s; e.variant = variantOf[t];
                            full[t].Add(e);
                        }
                        if (v == 0) lodSource = built.mesh;
                        variantOf[t]++;
                    }
                    lodColor = set.lodColor;
                    if (variantOf[t] > 0) KitTypes++;
                }
                if (full[t].Count == 0)
                {
                    variantOf[t] = 1;
                    full[t].Add(NewEntry(type.ToString(), mesh, info.sway ? vegMat : objMat, null));
                }
                if (info.big) { lod[t] = NewEntry(type + "_lod", WorldObjects.BuildLod(type, lodSource, lodColor), vegMat, null); lod[t].lodOnly = true; }
            }
            _full = full; _lod = lod; _variantOf = variantOf;
            foreach (var p in objects) AddObject(p.idx, p.type, p.rot, p.scale, false);
            TotalObjects = objects.Count;

            BuildGrass();
            Refresh(true);
        }

        // ================================================================== them / bot / rung (chat cay, dap da, moc lai)
        List<Entry>[] _full;
        Entry[] _lod;
        int[] _variantOf;
        readonly Dictionary<int, float> _shakes = new Dictionary<int, float>();
        bool _dirty;

        int ChunkOf(int idx) { int i = idx % _d.N, j = idx / _d.N; return (j / CH) * _cn + i / CH; }

        Matrix4x4 MatrixOf(int idx, float rot, float scale, float tilt = 0, float tiltDir = 0)
        {
            int i = idx % _d.N, j = idx / _d.N;
            var q = Quaternion.Euler(0, rot * Mathf.Rad2Deg, 0);
            if (tilt != 0) q = Quaternion.AngleAxis(tilt, new Vector3(Mathf.Cos(tiltDir), 0, Mathf.Sin(tiltDir))) * q;
            return Matrix4x4.TRS(new Vector3(i, _d.HT[idx], j), q, Vector3.one * scale);
        }

        IEnumerable<Entry> EntriesOf(int idx, WorldObjectType type)
        {
            int t = (int)type;
            int v = _variantOf[t] > 1 ? Mathf.Min(_variantOf[t] - 1, (int)(IslandNoise.Hash2(idx, 41, _d.Seed) * _variantOf[t])) : 0;
            foreach (var e in _full[t]) if (e.variant == v) yield return e;
            if (_lod[t] != null) yield return _lod[t];
        }

        /// <summary>Them vat the (luc khoi tao, khi cay moc lai, khi tai game).</summary>
        public void AddObject(int idx, WorldObjectType type, float rot, float scale, bool refresh = true)
        {
            var m = MatrixOf(idx, rot, scale);
            int c = ChunkOf(idx);
            foreach (var e in EntriesOf(idx, type)) e.chunks[c].Add(m);
            if (refresh) _dirty = true;
        }

        /// <summary>Bo vat the khoi o idx (chat do, dap vo).</summary>
        public void RemoveObject(int idx, WorldObjectType type)
        {
            int c = ChunkOf(idx), i = idx % _d.N, j = idx / _d.N;
            foreach (var e in EntriesOf(idx, type))
                e.chunks[c].RemoveAll(m => m.m03 == i && m.m23 == j);
            _shakes.Remove(idx);
            _dirty = true;
        }

        /// <summary>Rung vat the khi bi chat / dap (ban web: o.shake).</summary>
        public void Shake(int idx, float time = .25f) => _shakes[idx] = time;

        void UpdateShakes(float dt)
        {
            if (_shakes.Count == 0) return;
            var keys = new List<int>(_shakes.Keys);
            foreach (int idx in keys)
            {
                if (_d.ObjType[idx] == 0) { _shakes.Remove(idx); continue; }
                var type = (WorldObjectType)(_d.ObjType[idx] - 1);
                float t = _shakes[idx] - dt;
                float tilt = t > 0 ? Mathf.Sin(t * 60) * 5f * (t / .25f) : 0;
                var m = MatrixOf(idx, _d.ObjRot[idx], _d.ObjScale[idx], tilt, _d.ObjRot[idx]);
                int c = ChunkOf(idx), i = idx % _d.N, j = idx / _d.N;
                foreach (var e in EntriesOf(idx, type))
                {
                    var list = e.chunks[c];
                    for (int n = 0; n < list.Count; n++) if (list[n].m03 == i && list[n].m23 == j) list[n] = m;
                }
                if (t <= 0) _shakes.Remove(idx); else _shakes[idx] = t;
                _dirty = true;
            }
        }

        Entry NewEntry(string name, Mesh mesh, Material mat, MaterialPropertyBlock props)
        {
            var e = new Entry { name = name, mesh = mesh, mat = mat, props = props, chunks = new List<Matrix4x4>[_cn * _cn] };
            for (int c = 0; c < e.chunks.Length; c++) e.chunks[c] = new List<Matrix4x4>();
            _entries.Add(e);
            return e;
        }

        /// <summary>Co (buildTufts) va hoa - ty le theo biome, vi tri tu hash2 nen co dinh theo seed.</summary>
        void BuildGrass()
        {
            var d = _d; int N = d.N, seed = d.Seed;
            // tuft: 4 non 3 canh mau trang, nhan mau theo biome
            var tb = new MeshBuilder();
            for (int k = 0; k < 4; k++)
                tb.P(LowPolyShapes.Cone(.045f, .36f, 3), "#ffffff", Mathf.Cos(k * 1.7f) * .09f, .16f, Mathf.Sin(k * 1.7f) * .09f, 1, 1, 1, Mathf.Sin(k) * .35f, 0, Mathf.Cos(k) * .35f);
            var tuftMesh = tb.ToMesh("Tuft");
            var fb = new MeshBuilder();
            fb.P(LowPolyShapes.Box, "#5f9a3a", 0, .13f, 0, .02f, .26f, .02f);
            fb.P(LowPolyShapes.Ico0, "#ffffff", 0, .28f, 0, .075f, .06f, .075f);
            var flowerMesh = fb.ToMesh("Flower");

            var grassMat = MaterialLibrary.Grass();
            var flowerMat = MaterialLibrary.Objects(false);
            var tuftEntries = new Dictionary<int, Entry>();
            string[] fc = { "#ffe066", "#ffffff", "#e7a3f0", "#ff9aa8" };
            var flowerEntries = new Entry[4];
            for (int q = 0; q < 4; q++)
            {
                var mp = new MaterialPropertyBlock(); mp.SetColor("_BaseColor", MaterialLibrary.Hex(fc[q]));
                flowerEntries[q] = NewEntry("flower" + q, flowerMesh, flowerMat, mp);
                flowerEntries[q].grass = true;
            }
            float[] levels = { .933f, 1f, 1.067f };

            for (int k = 0; k < N * N; k++)
            {
                if ((d.ObjType != null && d.ObjType[k] != 0) || d.Dirt[k] != 0 || d.Solid[k] != 0) continue;
                var b = d.Bio[k];
                double pr = b switch
                {
                    Biome.Bush => .6, Biome.Forest => .65, Biome.Town => .4, Biome.Desert => .12, Biome.Beach => .06, Biome.Tropics => .7,
                    Biome.Mangrove => .55, Biome.Alpine => .3, Biome.Savanna => .55, Biome.Plains => .75, _ => 0,
                };
                int i = k % N, j = k / N, c = (j / CH) * _cn + i / CH;
                if (IslandNoise.Hash2(k, 11, seed) < pr)
                {
                    double h5 = IslandNoise.Hash2(k, 5, seed);
                    int lv = h5 < .333 ? 0 : h5 < .667 ? 1 : 2;
                    int key = (int)b * 3 + lv;
                    if (!tuftEntries.TryGetValue(key, out var e))
                    {
                        var col = MaterialLibrary.Hex(TuftColor(b)) * levels[lv]; col.a = 1;
                        var mp = new MaterialPropertyBlock(); mp.SetColor("_BaseColor", col);
                        e = NewEntry($"tuft_{b}_{lv}", tuftMesh, grassMat, mp);
                        e.grass = true;
                        tuftEntries[key] = e;
                    }
                    float x = i + (float)(IslandNoise.Hash2(k, 7, seed) - .5) * .6f, z = j + (float)(IslandNoise.Hash2(k, 8, seed) - .5) * .6f;
                    float sc = (float)(.8 + IslandNoise.Hash2(k, 10, seed) * .6) * (b == Biome.Plains ? 1.35f : 1f);
                    e.chunks[c].Add(Matrix4x4.TRS(new Vector3(x, d.HeightAt(x, z) - .02f, z), Quaternion.Euler(0, (float)IslandNoise.Hash2(k, 9, seed) * 6 * Mathf.Rad2Deg, 0), Vector3.one * sc));
                    TotalGrass++;
                }
                bool flowerBiome = b == Biome.Bush || b == Biome.Town || b == Biome.Savanna || b == Biome.Alpine || b == Biome.Plains;
                if (flowerBiome && IslandNoise.Hash2(k, 29, seed) < (b == Biome.Plains ? .14 : .07))
                {
                    float x = i + (float)(IslandNoise.Hash2(k, 17, seed) - .5) * .5f, z = j + (float)(IslandNoise.Hash2(k, 18, seed) - .5) * .5f;
                    flowerEntries[(k * 13) % 4].chunks[c].Add(Matrix4x4.Translate(new Vector3(x, d.HeightAt(x, z) - .02f, z)));
                }
            }
        }

        static string TuftColor(Biome b) => b switch
        {
            Biome.Desert => "#c9a75a", Biome.Beach => "#cdbb6a", Biome.Forest => "#5f8c35", Biome.Tropics => "#3f7a2e",
            Biome.Mangrove => "#7a8f4a", Biome.Alpine => "#6f8a5a", Biome.Savanna => "#cbb865", Biome.Plains => "#b8d465", _ => "#a4c85e",
        };

        // ================================================================== moi frame
        void LateUpdate()
        {
            if (_d == null) return;
            var cam = Camera.main;
            if (!cam) return;
            UpdateShakes(Time.deltaTime);
            Refresh(_dirty);
            _dirty = false;
            Draw();
        }

        /// <summary>Tinh lai tier tung manh khi camera di > 4 o (ban web: worldVis).</summary>
        void Refresh(bool force)
        {
            var cam = Camera.main;
            if (!cam) return;
            var cp = new Vector2(cam.transform.position.x, cam.transform.position.z);
            if (!force && Vector2.Distance(cp, _lastCam) < 4) return;
            _lastCam = cp;
            for (int cj = 0; cj < _cn; cj++)
                for (int ci = 0; ci < _cn; ci++)
                {
                    float dx = Mathf.Max(ci * CH - cp.x, 0, cp.x - (ci + 1) * CH), dz = Mathf.Max(cj * CH - cp.y, 0, cp.y - (cj + 1) * CH);
                    float dd = Mathf.Sqrt(dx * dx + dz * dz);
                    int c = cj * _cn + ci;
                    _tier[c] = (byte)(dd < shadowDistance ? 3 : dd < fullDistance ? 2 : dd < farDistance ? 1 : 0);
                    _grassOn[c] = dd < grassDistance;
                }
            foreach (var e in _entries)
            {
                e.nearCount = 0; e.midCount = 0;
                for (int c = 0; c < e.chunks.Length; c++)
                {
                    var list = e.chunks[c];
                    if (list.Count == 0) continue;
                    int t = _tier[c];
                    if (e.grass) { if (_grassOn[c]) Append(ref e.mid, ref e.midCount, list); }
                    else if (e.lodOnly) { if (t == 1) Append(ref e.mid, ref e.midCount, list); }
                    else if (t == 3) Append(ref e.near, ref e.nearCount, list);
                    else if (t == 2) Append(ref e.mid, ref e.midCount, list);
                }
            }
        }

        static void Append(ref Matrix4x4[] buf, ref int count, List<Matrix4x4> src)
        {
            if (buf.Length < count + src.Count) System.Array.Resize(ref buf, Mathf.Max(count + src.Count, buf.Length * 2));
            src.CopyTo(buf, count);
            count += src.Count;
        }

        void Draw()
        {
            int vis = 0;
            foreach (var e in _entries)
            {
                if (e.nearCount > 0) { DrawBatches(e, e.near, e.nearCount, ShadowCastingMode.On); vis += e.nearCount; }
                if (e.midCount > 0) { DrawBatches(e, e.mid, e.midCount, ShadowCastingMode.Off); vis += e.midCount; }
            }
            VisibleInstances = vis;
        }

        void DrawBatches(Entry e, Matrix4x4[] buf, int count, ShadowCastingMode shadows)
        {
            var rp = new RenderParams(e.mat)
            {
                shadowCastingMode = shadows,
                receiveShadows = true,
                worldBounds = _bounds,
                matProps = e.props,
            };
            for (int s = 0; s < count; s += MaxBatch)
                Graphics.RenderMeshInstanced(rp, e.mesh, e.submesh, buf, Mathf.Min(MaxBatch, count - s), s);
        }
    }
}
