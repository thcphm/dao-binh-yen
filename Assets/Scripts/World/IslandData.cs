// Port phan "terrain" cua serene-isle.html: rawH, chooseTown, initMtns, genTerrain, groundColor, hAt, gH, walkable, moveEntity.
// Thuan C# (khong goi API Unity tru struct Color/Vector), an toan de chay tren thread phu.
//
// He toa do giu nguyen ban web: o (i,j) nam tai x=i, z=j (don vi = 1 o), y = do cao.
// Luoi do cao FH co vien bien M o quanh dao: dinh (u,v) <-> the gioi (u-M, v-M).
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using static SereneIsle.IslandNoise;

namespace SereneIsle
{
    /// <summary>Ma biome giong mang BIO cua ban web.</summary>
    public enum Biome : byte
    {
        Sea = 0, Beach = 1, Bush = 2, Forest = 3, Desert = 4, Town = 5,
        Tropics = 6, Mangrove = 7, Alpine = 8, Savanna = 9, Plains = 10
    }

    /// <summary>Gia tri phu cua rawH (ban web luu vao bien toan cuc FEAT).</summary>
    public struct TerrainFeat
    {
        public double rf, lk, pl, ms, mm; // song, ho, dong bang, mesa, nui
    }

    public sealed class IslandData
    {
        // ---- hang so (giong ban web) ----
        public const float SEA = 0.32f;
        public const float TOWN_H = 0.95f;
        public const float PIER_H = 0.78f;
        public const int M = 24;            // vien bien quanh dao
        public const int CHUNK = 64;        // TCH: so o moi manh dia hinh

        public readonly int N;              // kich thuoc dao (o)
        public readonly int GS;             // so dinh luoi moi canh = N + 2M + 1
        public readonly int Seed;

        // ---- luoi dinh (GS*GS) ----
        public readonly float[] FH;         // do cao
        public readonly float[] VRF;        // max(song, ho)
        public readonly float[] VPL;        // dong bang
        public readonly float[] VMS;        // mesa

        // ---- luoi o (N*N) ----
        public readonly float[] HT;         // do cao tam o
        public readonly byte[] Level;       // H: 0 nuoc, 1 bai, 2..4 dat cao dan
        public readonly Biome[] Bio;
        public readonly byte[] Dirt, Rough, Pier, Ocean, Fresh, Coast;
        public readonly byte[] Solid;       // vat can (cay, da, nha...) - de cho cac buoc sau

        // ---- thi tran ----
        public int CX, CZ, TDIR;

        struct Mtn { public double x, z, r; }
        readonly List<Mtn> _mtns = new List<Mtn>();

        public IslandData(int seed, int size = 800)
        {
            Seed = seed;
            N = size;
            GS = N + 2 * M + 1;
            FH = new float[GS * GS]; VRF = new float[GS * GS]; VPL = new float[GS * GS]; VMS = new float[GS * GS];
            int nn = N * N;
            HT = new float[nn]; Level = new byte[nn]; Bio = new Biome[nn];
            Dirt = new byte[nn]; Rough = new byte[nn]; Pier = new byte[nn];
            Ocean = new byte[nn]; Fresh = new byte[nn]; Coast = new byte[nn]; Solid = new byte[nn];
            CX = CZ = N >> 1;
        }

        // ================= tien ich chi so =================
        public int Idx(int i, int j) => j * N + i;
        public bool InBounds(int i, int j) => i >= 0 && j >= 0 && i < N && j < N;
        /// <summary>Chi so dinh luoi FH ung voi o (i,j).</summary>
        public int VIdx(int i, int j) => (j + M) * GS + (i + M);
        static int RoundJs(double v) => (int)Math.Floor(v + 0.5); // Math.round cua JS

        // ---- xoay bo cuc thi tran theo huong bien (TDIR 0:+z 1:+x 2:-z 3:-x) ----
        public float TownAngle => new[] { 0f, Mathf.PI / 2, Mathf.PI, -Mathf.PI / 2 }[TDIR];
        void TR(double x, double z, out double a, out double b)
        {
            switch (TDIR) { case 0: a = x; b = z; break; case 1: a = z; b = -x; break; case 2: a = -x; b = -z; break; default: a = -z; b = x; break; }
        }
        void INV(double dx, double dz, out double a, out double b)
        {
            switch (TDIR) { case 0: a = dx; b = dz; break; case 1: a = -dz; b = dx; break; case 2: a = -dx; b = -dz; break; default: a = dz; b = -dx; break; }
        }
        public Vector2 TownPoint(double x, double z) { TR(x, z, out var a, out var b); return new Vector2((float)(CX + a), (float)(CZ + b)); }
        /// <summary>Doi toa do the gioi sang toa do bo cuc thi tran (lx, lz; lz duong = huong bien).</summary>
        public void ToTownLocal(double x, double z, out double lx, out double lz) => INV(x - CX, z - CZ, out lx, out lz);
        public int TownIdx(int x, int z) { TR(x, z, out var a, out var b); return Idx(CX + (int)a, CZ + (int)b); }

        // ================= cac truong khi hau =================
        public double Moist(double x, double z) => Clamp01((Fbm(x * .02 + 50, z * .02 + 50, Seed + 99) - .5) * 1.7 + .5);
        public double Temp(double x, double z) => Clamp01((1 - z / N) * .85 + (Fbm(x * .015, z * .015, Seed + 55) - .5) * .5 + .08);
        double PlainsF(double x, double z) => Smooth(.56, .64, Fbm(x * .007 + 20, z * .007 - 9, Seed + 410));
        double MesaF(double x, double z) => Smooth(.37, .31, Moist(x, z)) * Smooth(.5, .6, Fbm(x * .016, z * .016, Seed + 420));

        double MtnMask(double x, double z)
        {
            double m = 0;
            foreach (var t in _mtns) m = Math.Max(m, Smooth(t.r, t.r * .3, Hypot(x - t.x, z - t.z)));
            return m;
        }

        // ================= ham do cao goc =================
        public double RawH(double x, double z, bool noTown, out TerrainFeat f)
        {
            double half = N / 2.0;
            double nx = (x - half + .5) / half, nz = (z - half + .5) / half, d = Hypot(nx, nz);
            double cont = Fbm(x * .004, z * .004, Seed + 77);
            double e = Fbm(x * .03, z * .03, Seed) * .4 + Fbm(x * .011, z * .011, Seed + 3) * .3 + (cont - .5) * .55 + .6 - d * d * .9
                     + (Fbm(x * .13, z * .13, Seed + 5) - .5) * .05
                     - Smooth(.78, .95, Math.Max(Math.Abs(nx), Math.Abs(nz))) * .7;
            double h, rf = 0, lk = 0, pl = 0, ms = 0, mm = 0;
            if (e < .3) h = SEA - .12 - (.3 - e) * 5;
            else
            {
                h = SEA + .06 + 2.5 * (1 - Math.Exp(-(e - .3) * 2.8));
                double inl = Smooth(.31, .42, e);
                // doi thoai
                h += (Fbm(x * .022, z * .022, Seed + 41) - .45) * 1.3 * inl;
                // day nui: ridged noise
                double rid = 1 - Math.Abs(2 * Fbm(x * .018, z * .018, Seed + 31) - 1);
                mm = MtnMask(x, z);
                h += (.35 + .65 * rid * rid * rid) * mm * 7.5 * Smooth(.33, .46, e);
                h += (VNoise(x * .5, z * .5, Seed + 8) - .5) * .08;
                // dong bang: phang, thoang
                pl = PlainsF(x, z) * (1 - mm) * inl;
                if (pl > 0) h = h * (1 - pl * .85) + (SEA + .95 + (VNoise(x * .08, z * .08, Seed + 13) - .5) * .25) * pl * .85;
                // cao nguyen bac thang (mesa) voi vach da
                ms = MesaF(x, z) * (1 - mm) * inl;
                if (ms > 0)
                {
                    double hb = h + ms * 1.6, st = .75, t = hb / st, fl = Math.Floor(t), fr = t - fl;
                    h = h * (1 - ms) + ((fl + Smooth(.72, .92, fr)) * st) * ms;
                }
                // dam lay
                double wf = Smooth(.66, .76, Moist(x, z)) * Smooth(1.9, 1.1, h);
                if (wf > 0) h = h * (1 - wf * .85) + (SEA + (VNoise(x * .25, z * .25, Seed + 12) - .42) * .32) * wf * .85;
                // ho nuoc ngot (billabong)
                lk = Smooth(.7, .77, Fbm(x * .017, z * .017, Seed + 300)) * (1 - mm) * Smooth(.35, .45, e) * (1 - ms);
                if (lk > 0) h = h * (1 - lk) + (SEA - .75) * lk;
                // song uon luon, loi qua duoc
                double wx = (Fbm(x * .01, z * .01, Seed + 230) - .5) * 60;
                double rv = Math.Abs(Fbm((x + wx) * .0045, (z - wx) * .0045, Seed + 200) * 2 - 1);
                double rv2 = Math.Abs(Fbm((x - wx) * .006, (z + wx) * .006, Seed + 260) * 2 - 1);
                rf = Math.Max(Smooth(.045, .018, rv), Smooth(.03, .012, rv2) * .9) * Smooth(.31, .34, e) * (1 - mm * .7);
                if (!noTown) rf *= Smooth(24, 40, Hypot(x - CX, z - CZ));
                if (rf > 0) h = h * (1 - rf) + Math.Min(h, SEA - .3) * rf;
            }
            f.rf = rf; f.lk = lk; f.pl = pl; f.ms = ms; f.mm = mm;
            h = Math.Max(h, -1.6);
            if (noTown) return h;
            INV(x - CX, z - CZ, out var lx, out var lz);
            double dd = Hypot(lx, lz) + Math.Max(0, lz - 4) * 1.4, w = Smooth(17, 8, dd);
            return h * (1 - w) + TOWN_H * w;
        }

        // ================= chon cho dat thi tran (sat bo bien) =================
        void ChooseTown()
        {
            var r = new Mulberry32(unchecked(Seed * 3 + 1));
            int[][] D = { new[] { 0, 1 }, new[] { 1, 0 }, new[] { 0, -1 }, new[] { -1, 0 } };
            // Ban web: [0,1,2,3].sort(()=>r()-.5) -> mo phong dung thu tu goi comparator cua V8 (Chrome/Node)
            int[] ord = { 0, 1, 2, 3 };
            V8RandomSort(ord, r);
            int C0 = N >> 1;
            bool IsSea(int x, int z) { double h = RawH(x, z, true, out var f); return h < SEA - .05 && f.rf < .05 && f.lk < .05; }
            foreach (int di in ord)
            {
                int dx = D[di][0], dz = D[di][1], shore = 0;
                for (int t = 20; t < C0 - 12; t++)
                {
                    if (!IsSea(C0 + dx * t, C0 + dz * t)) continue;
                    bool ok = true;
                    for (int u = 1; u <= 10; u++) if (!IsSea(C0 + dx * (t + u), C0 + dz * (t + u))) { ok = false; break; }
                    if (ok) { shore = t; break; }
                }
                if (shore >= 40 && shore <= C0 - 50)
                {
                    CX = RoundJs(C0 + dx * (shore - 12)); CZ = RoundJs(C0 + dz * (shore - 12));
                    if (RawH(CX, CZ, true, out _) > SEA + .15) { TDIR = di; return; }
                }
            }
            CX = C0; CZ = C0; TDIR = 0;
        }

        /// <summary>
        /// Array.prototype.sort cua V8 (TimSort: CountAndMakeRun + BinaryInsertionSort, mang ngan &lt; 64)
        /// voi comparator (a,b) =&gt; r() - .5 (bo qua a, b). Chi thu tu goi r() la quan trong.
        /// </summary>
        public static void V8RandomSort(int[] a, Mulberry32 r)
        {
            int n = a.Length;
            if (n < 2) return;
            double Cmp() => r.Next() - .5;
            // CountAndMakeRun(0, n)
            int run = 2;
            bool desc = Cmp() < 0;
            for (int idx = 2; idx < n; idx++)
            {
                double o = Cmp();
                if (desc ? o >= 0 : o < 0) break;
                run++;
            }
            if (desc) Array.Reverse(a, 0, run);
            // BinaryInsertionSort(0, run, n) vi minRun = n
            for (int start = run; start < n; start++)
            {
                int left = 0, right = start, pivot = a[start];
                while (left < right)
                {
                    int mid = left + ((right - left) >> 1);
                    if (Cmp() < 0) right = mid; else left = mid + 1;
                }
                for (int p = start; p > left; p--) a[p] = a[p - 1];
                a[left] = pivot;
            }
        }

        void InitMtns()
        {
            var r = new Mulberry32(unchecked(Seed * 13 + 5));
            _mtns.Clear();
            for (int k = 0; k < 9; k++)
            {
                double a = r.Next() * 6.28 + k * .7, d = (.15 + r.Next() * .6) * N / 2;
                double x = N / 2.0 + Math.Cos(a) * d, z = N / 2.0 + Math.Sin(a) * d;
                if (Hypot(x - CX, z - CZ) < 70) continue; // ban web: bo qua TRUOC khi goi r() lay ban kinh
                _mtns.Add(new Mtn { x = x, z = z, r = 18 + r.Next() * 24 });
            }
        }

        // ================= sinh toan bo dia hinh =================
        /// <param name="progress">0..1, co the goi tu thread phu.</param>
        public void Generate(Action<float> progress = null)
        {
            ChooseTown();
            InitMtns();

            Array.Clear(Pier, 0, Pier.Length);
            for (int lx = 0; lx <= 1; lx++)
                for (int lz = 8; lz <= 21; lz++) { int k = TownIdx(lx, lz); if (k >= 0 && k < N * N) Pier[k] = 1; }
            for (int lz = 11; lz <= 13; lz++) { int k = TownIdx(2, lz); if (k >= 0 && k < N * N) Pier[k] = 1; }

            // luoi dinh: song song theo hang
            int done = 0;
            Par.For(0, GS, v =>
            {
                for (int u = 0; u < GS; u++)
                {
                    int n = v * GS + u;
                    FH[n] = (float)RawH(u - M, v - M, false, out var f);
                    VRF[n] = (float)Math.Max(f.rf, f.lk);
                    VPL[n] = (float)f.pl;
                    VMS[n] = (float)f.ms;
                }
                int c = System.Threading.Interlocked.Increment(ref done);
                if (progress != null && (c & 31) == 0) progress(.85f * c / GS);
            });

            // phan loai o
            Par.For(0, N, j =>
            {
                for (int i = 0; i < N; i++)
                {
                    int k = Idx(i, j), vn = VIdx(i, j);
                    float h = FH[vn]; HT[k] = h;
                    byte l = (byte)(h < SEA + .04 ? 0 : h < SEA + .45 ? 1 : h < 2.2 ? 2 : h < 3.5 ? 3 : 4);
                    double m = Moist(i, j), tp = Temp(i, j);
                    Level[k] = l;
                    double rv = VRF[vn], pl = VPL[vn];
                    Biome b =
                        l == 0 ? Biome.Sea :
                        l == 1 ? ((m > .7 || (rv > .15 && tp > .55)) ? Biome.Mangrove : Biome.Beach) :
                        h > 4.3 ? Biome.Alpine :
                        (tp > .64 && m > .52) ? Biome.Tropics :
                        (m > .66 && h < 1.6) ? Biome.Mangrove :
                        (pl > .5 && m > .34 && m < .62) ? Biome.Plains :
                        m < .33 ? Biome.Desert :
                        m < .43 ? Biome.Savanna :
                        m > .54 ? (tp < .3 ? Biome.Alpine : Biome.Forest) :
                        Biome.Bush;
                    Bio[k] = b;
                    Rough[k] = (byte)(((b == Biome.Bush || b == Biome.Desert || b == Biome.Tropics || b == Biome.Savanna)
                                       && Fbm(i * .045, j * .045, Seed + 400) > .68) ? 1 : 0);
                    double d = Hypot(i - CX, j - CZ);
                    INV(i - CX, j - CZ, out var lx, out var lz);
                    if (d < 8.6 && l > 0)
                    {
                        Bio[k] = Biome.Town; Level[k] = 2;
                        if (d < 2.6 || (lx >= 0 && lx <= 1 && lz > 0 && lz < 9) || (Math.Abs(lz) < 1 && Math.Abs(lx) < 7)) Dirt[k] = 1;
                    }
                }
            });
            progress?.Invoke(.9f);

            // bien man: loang tu goc (0,0), khong di qua song ho
            Array.Clear(Ocean, 0, Ocean.Length);
            var stack = new Stack<int>();
            Ocean[0] = 1; stack.Push(0);
            int[] DX = { 1, -1, 0, 0 }, DZ = { 0, 0, 1, -1 };
            while (stack.Count > 0)
            {
                int k = stack.Pop(), i = k % N, j = k / N;
                for (int q = 0; q < 4; q++)
                {
                    int x = i + DX[q], y = j + DZ[q];
                    if (!InBounds(x, y)) continue;
                    int n = Idx(x, y);
                    if (Ocean[n] == 0 && Level[n] == 0 && !(VRF[VIdx(x, y)] > .35f)) { Ocean[n] = 1; stack.Push(n); }
                }
            }
            for (int k = 0; k < N * N; k++) Fresh[k] = (byte)(Level[k] == 0 && Ocean[k] == 0 ? 1 : 0);

            // khoang cach toi bien man (1 = bien, 2..8 = bo bien that)
            Array.Clear(Coast, 0, Coast.Length);
            var front = new List<int>();
            for (int k = 0; k < N * N; k++) if (Ocean[k] != 0) { Coast[k] = 1; front.Add(k); }
            for (byte d = 2; d <= 8; d++)
            {
                var next = new List<int>();
                foreach (int k in front)
                {
                    int i = k % N, j = k / N;
                    for (int q = 0; q < 4; q++)
                    {
                        int x = i + DX[q], y = j + DZ[q];
                        if (!InBounds(x, y)) continue;
                        int n = Idx(x, y);
                        if (Coast[n] == 0) { Coast[n] = d; next.Add(n); }
                    }
                }
                front = next;
            }
            progress?.Invoke(1f);
        }

        // ================= truy van do cao =================
        /// <summary>hAt: noi suy song tuyen tren luoi FH.</summary>
        public float HeightAt(float x, float z)
        {
            float u = Mathf.Clamp(x + M, 0, GS - 1.001f), v = Mathf.Clamp(z + M, 0, GS - 1.001f);
            int i = (int)u, j = (int)v;
            float fu = u - i, fv = v - j;
            float a = FH[j * GS + i], b = FH[j * GS + i + 1], c = FH[(j + 1) * GS + i], d = FH[(j + 1) * GS + i + 1];
            return a + (b - a) * fu + (c - a) * fv + (a - b - c + d) * fu * fv;
        }

        /// <summary>gH: do cao mat dat co tinh ca san cau tau.</summary>
        public float GroundHeight(float x, float z)
        {
            int i = RoundJs(x), j = RoundJs(z);
            float h = HeightAt(x, z);
            return InBounds(i, j) && Pier[Idx(i, j)] != 0 ? Mathf.Max(h, PIER_H) : h;
        }

        public bool IsPier(float x, float z)
        {
            int i = RoundJs(x), j = RoundJs(z);
            return InBounds(i, j) && Pier[Idx(i, j)] != 0;
        }

        public float TileHeight(float x, float z)
        {
            int i = RoundJs(x), j = RoundJs(z);
            return InBounds(i, j) ? HT[Idx(i, j)] : -1.6f;
        }

        // ---- vat the tren o (cay, da...) - ban web: objAt ----
        /// <summary>0 = trong, nguoc lai = (int)WorldObjectType + 1.</summary>
        public byte[] ObjType;
        /// <summary>Quan dao nho (Islets.Carve, sinh sau dao chinh). Rong neu chua tao.</summary>
        public List<Islet> Islets = new List<Islet>();
        public float[] ObjScale;
        public float[] ObjRot;

        /// <summary>Do cao dinh vat can tai o k (ban web: solidTop). Cay to = khong nhay qua duoc.</summary>
        public float SolidTop(int k)
        {
            if (ObjType != null && ObjType[k] != 0)
            {
                float h = WorldObjects.JumpHeight((WorldObjectType)(ObjType[k] - 1));
                return h > 0 ? HT[k] + h * ObjScale[k] : 1e9f;
            }
            if (PlacedTop.TryGetValue(k, out var t)) return HT[k] + t;
            return 1e9f;
        }

        /// <summary>Do cao do nguoi choi dat (hang rao, lua trai...) de nhay qua (ban web: PLH).</summary>
        public readonly Dictionary<int, float> PlacedTop = new Dictionary<int, float>();

        // ================= con nuoc =================
        /// <summary>Muc nuoc bien hien tai (con nuoc). Ao, ho, song ngot giu muc SEA.</summary>
        public float WaterLevel = SEA;
        /// <summary>Nuoc bien sau hon muc nay thi khong loi tiep duoc.</summary>
        public const float MaxWadeDepth = .4f;

        /// <summary>Mat nuoc tai o k: bien theo con nuoc, nuoc ngot co dinh.</summary>
        public float WaterSurface(int k) => Fresh[k] != 0 ? SEA : WaterLevel;

        /// <summary>Do sau nuoc tai (x, z) (&lt;= 0 la tren can).</summary>
        public float WaterDepth(float x, float z)
        {
            int i = RoundJs(x), j = RoundJs(z);
            if (!InBounds(i, j)) return 9f;
            int k = Idx(i, j);
            if (Pier[k] != 0) return 0f;
            return WaterSurface(k) - HeightAt(x, z);
        }

        /// <summary>walkable: khong xuong nuoc sau, khong leo bac cao hon .42.
        /// Ban web khong cho xuong bien; ban nay cho loi bien nong (do sau &lt; .4) vi bai bien thay doi theo con nuoc.</summary>
        public bool Walkable(float x, float z, float fromY)
        {
            int i = RoundJs(x), j = RoundJs(z);
            if (!InBounds(i, j)) return false;
            int k = Idx(i, j);
            if (Solid[k] != 0 && !(fromY >= SolidTop(k))) return false;
            float h = GroundHeight(x, z);
            if (Pier[k] == 0)
            {
                if (Fresh[k] != 0) { if (h < SEA + .04f && !(h > SEA - .4f)) return false; }
                else if (WaterLevel - h > MaxWadeDepth) return false;
            }
            return h - fromY <= .42f;
        }

        /// <summary>moveEntity: tach truc X/Z, kiem 3 diem mep ban kinh r.</summary>
        public void MoveEntity(ref float px, ref float pz, float dx, float dz, float r, float fromY)
        {
            bool blockX = false, blockZ = false;
            if (dx != 0)
            {
                float nx = px + dx, ex = nx + Mathf.Sign(dx) * r;
                if (Walkable(ex, pz, fromY) && Walkable(ex, pz + r * .7f, fromY) && Walkable(ex, pz - r * .7f, fromY)) px = nx;
                else blockX = true;
            }
            if (dz != 0)
            {
                float nz = pz + dz, ez = nz + Mathf.Sign(dz) * r;
                if (Walkable(px, ez, fromY) && Walkable(px + r * .7f, ez, fromY) && Walkable(px - r * .7f, ez, fromY)) pz = nz;
                else blockZ = true;
            }
            // truot qua goc: di gan thang vao vat can 1 o (lua trai, cot, cay) ma ben canh con trong -> day ngang de vong qua
            // thay vi dung im (ban web khong co, de ket o giua duong)
            if (blockZ && Mathf.Abs(dx) < Mathf.Abs(dz) * .35f) SlideAround(ref px, pz, dz, r, fromY, true);
            else if (blockX && Mathf.Abs(dz) < Mathf.Abs(dx) * .35f) SlideAround(ref pz, px, dx, r, fromY, false);
        }

        /// <summary>Bi chan theo truc chinh: tim ben (trai / phai, trong 0.9 o) ma tu do di tiep duoc, dich ngang ve phia do.</summary>
        void SlideAround(ref float side, float along, float d, float r, float fromY, bool alongZ)
        {
            float step = Mathf.Abs(d), ahead = along + d + Mathf.Sign(d) * r;
            bool Free(float s) => alongZ ? Walkable(s, ahead, fromY) && Walkable(s, along, fromY)
                                         : Walkable(ahead, s, fromY) && Walkable(along, s, fromY);
            for (float off = .3f; off <= .9f; off += .3f)
                foreach (float sgn in new[] { 1f, -1f })
                {
                    if (!Free(side + sgn * off)) continue;
                    float ns = side + sgn * step, edge = ns + sgn * r;
                    if (alongZ ? Walkable(edge, along, fromY) : Walkable(along, edge, fromY)) side = ns;
                    return;
                }
        }

        /// <summary>Diem den khi tau cap ben (findArrival): o tren cau tau, huong vao thi tran.</summary>
        public Vector3 ArrivalPoint(out float facingRad)
        {
            int land = TownIdx(0, 20);
            int i = land % N, j = land / N;
            facingRad = Mathf.Atan2(CX - i, CZ - j);
            return new Vector3(i, GroundHeight(i, j), j);
        }

        // ================= mau mat dat (groundColor) - tra ve mau sRGB =================
        static Color Hex(int rgb) => new Color(((rgb >> 16) & 255) / 255f, ((rgb >> 8) & 255) / 255f, (rgb & 255) / 255f, 1f);
        static readonly Color
            gSandW = Hex(0xd8c690), gDeep = Hex(0x1c6676), gSand = Hex(0xead59e), gBush = Hex(0x8db552),
            gForest = Hex(0x5a8a36), gPlains = Hex(0xa9c95a), gRough = Hex(0x7a4e34), gMesa1 = Hex(0xd98a4a), gMesa2 = Hex(0x9a4a2e),
            gRedsand = Hex(0xd9925e), gMud = Hex(0x6f6446), gPinef = Hex(0x4a7040), gDesert = Hex(0xc9673a), gRock = Hex(0x9a958a),
            gRedrock = Hex(0xa4553a), gDirt = Hex(0xc39d68), gTown = Hex(0x9dbd5a), gJungle = Hex(0x3e7a2c), gWet = Hex(0x617a48),
            gSnow = Hex(0xeef2f4), gSav = Hex(0xc9b862);

        static Color L(Color a, Color b, double t) => Color.LerpUnclamped(a, b, (float)t);

        public Color GroundColor(double x, double z, double h, double slope, int vn)
        {
            double rv = vn >= 0 ? VRF[vn] : 0, pl = vn >= 0 ? VPL[vn] : 0, ms = vn >= 0 ? VMS[vn] : 0;
            if (h < SEA) return L(gSandW, gDeep, Math.Min(1, (SEA - h) / 1.3));
            double m = Moist(x, z), tp = Temp(x, z), td = Hypot(x - CX, z - CZ);
            Color o = gDesert;
            o = L(o, gSav, Smooth(.31, .36, m)); o = L(o, gBush, Smooth(.41, .47, m)); o = L(o, gForest, Smooth(.52, .6, m));
            o = L(o, gJungle, Smooth(.6, .7, tp) * Smooth(.48, .56, m));
            o = L(o, gPinef, Smooth(.33, .25, tp) * Smooth(.52, .6, m));
            o = L(o, gPlains, Smooth(.4, .7, pl) * Smooth(.33, .38, m) * Smooth(.64, .58, m));
            if (ms > .05) { double band = .5 + .5 * Math.Sin(h * 8.4); o = L(o, band > .5 ? gMesa1 : gMesa2, ms * (.35 + .4 * Smooth(.4, 1.2, slope))); }
            if (Fbm(x * .045, z * .045, Seed + 400) > .68 && m < .5 && tp < .64 && h < 3.3) o = L(o, gRough, .55);
            o = L(o, gMud, Smooth(.05, .25, rv) * Smooth(SEA + 1.2, SEA + .2, h) * (m > .4 ? 1 : .4));
            double ww = Smooth(.63, .7, m) * Smooth(1.8, 1.2, h); o = L(o, gWet, ww * .85);
            o = L(o, gTown, Smooth(11, 8, td));
            o = L(o, m < .42 ? gRedrock : gRock, Math.Max(Smooth(3.3, 4.1, h), Smooth(.8, 1.4, slope)));
            o = L(o, gSnow, Smooth(4.3, 4.9, h) * (1 - Smooth(1.2, 1.8, slope) * .6));
            o = L(o, m < .4 ? gRedsand : gSand, Smooth(SEA + .6, SEA + .3, h) * (1 - ww) * (1 - Smooth(.1, .3, rv)));
            INV(x - CX, z - CZ, out var lx, out var lz);
            double dd = td < 9 ? Math.Max(Math.Max(Smooth(3, 2.3, td), lz > 0 ? Smooth(1.3, .8, Math.Abs(lx - .5)) : 0),
                                          Smooth(.9, .4, Math.Abs(lz)) * Smooth(7.5, 6, Math.Abs(lx))) : 0;
            o = L(o, gDirt, dd);
            float k = (float)(.93 + VNoise(x * .7, z * .7, Seed + 3) * .14);
            return new Color(o.r * k, o.g * k, o.b * k, 1f);
        }
    }
}
