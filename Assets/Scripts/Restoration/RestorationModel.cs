// Suc song vung (Docs/ThietKeGameplay.md muc 5). Thuan C# de test EditMode.
// Dao chia thanh o 64x64 dinh (trung manh dia hinh). Moi o lay ten theo vung sinh thai chiem nhieu nhat
// + huong so voi tam dao ("Rung Duoc Tay Bac"); cac o cung ten gop thanh 1 vung. O co thi tran ten "Xom Chai".
using System;
using System.Collections.Generic;

namespace SereneIsle
{
    public sealed class RestorationModel
    {
        public const int Cell = IslandData.CHUNK;
        public static readonly int[] Thresholds = { 25, 50, 75, 100 };

        public sealed class Region
        {
            public int id;
            public string name;
            public Biome group;
            public float vitality;
            public int land;                 // so o dat lien
            public readonly List<int> cells = new List<int>();
        }

        public readonly List<Region> Regions = new List<Region>();
        readonly int[] _cellRegion;
        readonly int _cn, _n;

        public int CellsPerSide => _cn;

        public RestorationModel(IslandData d)
        {
            _n = d.N;
            _cn = (d.GS - 1 + Cell - 1) / Cell;
            _cellRegion = new int[_cn * _cn];
            for (int q = 0; q < _cellRegion.Length; q++) _cellRegion[q] = -1;

            var counts = new int[_cn * _cn, 11];
            var land = new int[_cn * _cn];
            for (int j = 0; j < d.N; j++)
                for (int i = 0; i < d.N; i++)
                {
                    int k = d.Idx(i, j);
                    if (d.Level[k] == 0) continue;
                    int c = CellOf(i, j);
                    counts[c, (int)d.Bio[k]]++;
                    land[c]++;
                }
            int townCell = CellOf(d.CX, d.CZ);
            var byName = new Dictionary<string, Region>();
            for (int c = 0; c < _cellRegion.Length; c++)
            {
                if (land[c] < Cell * Cell / 10) continue;      // it hon 10% dat lien: bo
                Biome best = Biome.Beach; int bn = -1;
                for (int b = 1; b < 11; b++) if (b != (int)Biome.Town && counts[c, b] > bn) { bn = counts[c, b]; best = (Biome)b; }
                string name = c == townCell ? "Xóm Chài" : $"{GroupName(best)} {Compass(d, c)}";
                if (!byName.TryGetValue(name, out var r))
                {
                    r = new Region { id = Regions.Count, name = name, group = c == townCell ? Biome.Town : best };
                    r.vitality = 20 + (int)((uint)(d.Seed * 31 + Hash(name)) % 21u);   // 20..40, co dinh theo seed
                    byName[name] = r;
                    Regions.Add(r);
                }
                r.cells.Add(c); r.land += land[c];
                _cellRegion[c] = r.id;
            }
        }

        static int Hash(string s) { unchecked { int h = 17; foreach (char ch in s) h = h * 31 + ch; return h; } }

        public static string GroupName(Biome b) => b switch
        {
            Biome.Beach => "Bãi Ghềnh", Biome.Mangrove => "Rừng Đước", Biome.Tropics => "Rừng Rậm", Biome.Forest => "Rừng Già",
            Biome.Bush => "Đồi Sim", Biome.Plains => "Đồng Cỏ", Biome.Savanna => "Gò Cát", Biome.Desert => "Đồi Đỏ",
            Biome.Alpine => "Núi Đá", Biome.Town => "Xóm Chài", _ => "Vùng",
        };

        /// <summary>Huong cua o so voi tam dao (z tang = phia Bac, x tang = phia Dong).</summary>
        string Compass(IslandData d, int c)
        {
            float cx = (c % _cn + .5f) * Cell - IslandData.M - d.N / 2f, cz = (c / _cn + .5f) * Cell - IslandData.M - d.N / 2f;
            if (Math.Abs(cx) < d.N * .14f && Math.Abs(cz) < d.N * .14f) return "giữa đảo";
            double a = Math.Atan2(cx, cz) * 180 / Math.PI;         // 0 = Bac, 90 = Dong
            int sector = (int)Math.Round(((a + 360) % 360) / 45) % 8;
            return new[] { "Bắc", "Đông Bắc", "Đông", "Đông Nam", "Nam", "Tây Nam", "Tây", "Tây Bắc" }[sector];
        }

        public int CellOf(int i, int j)
        {
            int u = Math.Max(0, Math.Min(_cn - 1, (i + IslandData.M) / Cell)), v = Math.Max(0, Math.Min(_cn - 1, (j + IslandData.M) / Cell));
            return v * _cn + u;
        }

        public Region RegionAt(float x, float z)
        {
            int i = (int)Math.Floor(x + .5), j = (int)Math.Floor(z + .5);
            if (i < 0 || j < 0 || i >= _n || j >= _n) return null;
            int r = _cellRegion[CellOf(i, j)];
            return r >= 0 ? Regions[r] : null;
        }

        public Region RegionOfCell(int c) => c >= 0 && c < _cellRegion.Length && _cellRegion[c] >= 0 ? Regions[_cellRegion[c]] : null;

        /// <summary>Cong / tru suc song. Tra ve nguong vua vuot len (25/50/75/100) hoac 0.</summary>
        public int Add(Region r, float delta)
        {
            if (r == null || delta == 0) return 0;
            float before = r.vitality;
            r.vitality = Math.Max(0, Math.Min(100, r.vitality + delta));
            int crossed = 0;
            foreach (int t in Thresholds) if (before < t && r.vitality >= t) crossed = t;
            return crossed;
        }

        /// <summary>Suc song ca dao: trung binh theo dien tich dat lien.</summary>
        public float IslandVitality
        {
            get
            {
                double s = 0; long w = 0;
                foreach (var r in Regions) { s += r.vitality * r.land; w += r.land; }
                return w > 0 ? (float)(s / w) : 0;
            }
        }

        /// <summary>Muc phai mau dia hinh: vung kiet que xam nau, tu 50 tro len tuoi han.</summary>
        public static float Desaturation(float vitality) => .45f * (1 - (float)IslandNoise.Smooth(0, 50, vitality));

        /// <summary>Cay chat moc lai cham gap doi o vung duoi 50.</summary>
        public static int RegrowFactor(float vitality) => vitality < 50 ? 2 : 1;

        public float[] Snapshot()
        {
            var a = new float[Regions.Count];
            for (int q = 0; q < a.Length; q++) a[q] = Regions[q].vitality;
            return a;
        }

        public void Restore(float[] a)
        {
            if (a == null || a.Length != Regions.Count) return;   // dao khac / phien ban khac: giu mac dinh
            for (int q = 0; q < a.Length; q++) Regions[q].vitality = Math.Max(0, Math.Min(100, a[q]));
        }
    }
}
