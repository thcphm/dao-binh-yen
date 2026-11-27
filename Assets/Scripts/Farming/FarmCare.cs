// Vuon ruong nang cao (Docs/ThietKeGameplay.md muc 4.4 - 4.6, 4.8), thuan C# de test EditMode:
//  - Xen canh: dau ke ben +1 hang; sa, hanh, toi, van tho quanh luong giam sau; mot loai lien nhau tren 12 o de sau benh.
//  - Sau benh: sau xanh, ray, oc buou. Vung suc song thap / doc canh de bi. Tri tu nhien: bat tay, nuoc toi ot, tro bep.
//  - Thien dich: vung suc song tu 50 co chim, ech, ong: sau giam mot nua, dua bi cay trai them 10% san luong.
//  - Giu giong: chua 1 cay chin cho gia 3 ngay lay hat; hat mang hang cay me; 3 vu lien tiep thanh giong nha (+1 hang).
//  - Ghep cay: cay an trai trong tu cay giong chi hang thuong; ghep canh tu cay dau dong thi cho dac san;
//    ghep khac loai ra giong lai co dac tinh moi (trai hai mua, chiu man, trai to).
//  - Luan canh lua tom: o dat trung co bo bao + cong. Mua Ha mo cong tha tom, mua Thu dong cong rua man cay lua chiu man.
using System;
using System.Collections.Generic;

namespace SereneIsle
{
    public enum Pest : byte { None = 0, SauXanh = 1, Ray = 2, OcBuou = 3 }

    public enum GraftTrait : byte { None = 0, TwoSeasons = 1, SaltTolerant = 2, BigFruit = 3 }

    /// <summary>Dong giong giu lai cua 1 loai cay (luu game).</summary>
    [Serializable]
    public sealed class Strain
    {
        public string crop;
        public int q;        // hang cay me lan lay hat gan nhat
        public int gen;      // so vu lien tiep giu giong
        public string name;  // ten giong nha (null = chua thanh giong nha)
    }

    public static class FarmCare
    {
        // ------------------------------------------------------------------ hang so
        public const int MonoLimit = 12;          // tren 12 o cung loai lien nhau: de sau benh
        public const int PestKillDays = 4;        // bi sau 4 ngay khong tri: chet
        public const float EnemyVitality = 50;    // thien dich xuat hien
        public const float MotherVitality = 75;   // cay dau dong xuat hien
        public const double EnemyYield = .1;      // thien dich: +10% san luong dua, bi, cay trai
        public const int SeedAgeDays = 3;         // chua giong: de gia 3 ngay
        public const int HomeStrainGens = 3;      // 3 vu lien tiep thanh giong nha
        public const int TolerantSalt = 4;        // lua chiu man cay duoc khi dat con man <= 4 ngay
        public const int ShrimpDays = 10;         // tom lon sau 10 ngay
        public const int ShelterRadius = 2;       // nha luoi che 5 x 5 o quanh cot giua
        public const int PestStartDay = 29;       // thang dau tien chua co sau (nguoi moi lam quen)
        public const string SavedSeedPrefix = "giong_", ScionPrefix = "canh_";

        public static readonly HashSet<string> Legumes = new HashSet<string> { "dau_que", "dau_phong" };
        public static readonly HashSet<string> Repellents = new HashSet<string> { "sa", "hanh_la", "hanh_tim", "toi", "van_tho" };

        static IEnumerable<int> Around8(int k, int n)
        {
            int i = k % n, j = k / n;
            for (int dj = -1; dj <= 1; dj++)
                for (int di = -1; di <= 1; di++)
                {
                    if (di == 0 && dj == 0) continue;
                    int x = i + di, z = j + dj;
                    if (x >= 0 && z >= 0 && x < n && z < n) yield return z * n + x;
                }
        }

        static IEnumerable<int> Around4(int k, int n)
        {
            int i = k % n, j = k / n;
            if (i > 0) yield return k - 1;
            if (i < n - 1) yield return k + 1;
            if (j > 0) yield return k - n;
            if (j < n - 1) yield return k + n;
        }

        static bool Alive(FarmPlot p) => p != null && !p.ditch && !p.Empty && !p.dead;

        // ------------------------------------------------------------------ xen canh
        /// <summary>Co cay dau song o 8 o xung quanh (khong tinh chinh no la dau).</summary>
        public static bool NearLegume(int k, int n, Func<int, FarmPlot> at)
        {
            var self = at(k);
            if (self == null || Legumes.Contains(self.crop)) return false;
            foreach (int q in Around8(k, n)) { var p = at(q); if (Alive(p) && Legumes.Contains(p.crop)) return true; }
            return false;
        }

        /// <summary>Co sa, hanh, toi, van tho trong 8 o xung quanh (hoac chinh no).</summary>
        public static bool NearRepellent(int k, int n, Func<int, FarmPlot> at)
        {
            var self = at(k);
            if (Alive(self) && Repellents.Contains(self.crop)) return true;
            foreach (int q in Around8(k, n)) { var p = at(q); if (Alive(p) && Repellents.Contains(p.crop)) return true; }
            return false;
        }

        /// <summary>So o cung loai cay song lien nhau (4 huong) tinh tu o k.</summary>
        public static int ClusterSize(int k, int n, Func<int, FarmPlot> at, int stopAt = 999)
        {
            var self = at(k);
            if (!Alive(self)) return 0;
            var seen = new HashSet<int> { k };
            var st = new Stack<int>();
            st.Push(k);
            while (st.Count > 0 && seen.Count < stopAt)
            {
                int c = st.Pop();
                foreach (int q in Around4(c, n))
                {
                    var p = at(q);
                    if (Alive(p) && p.crop == self.crop && seen.Add(q)) st.Push(q);
                }
            }
            return seen.Count;
        }

        // ------------------------------------------------------------------ sau benh, thien dich
        /// <summary>Xac suat moi ngay mot o bi sau.</summary>
        public static double PestChance(float vitality, bool mono, bool repel, bool sheltered)
        {
            double p = vitality < 25 ? .06 : vitality < 50 ? .04 : .02;
            if (vitality >= EnemyVitality) p *= .5;   // chim, ech an sau
            if (mono) p *= 3;
            if (repel) p *= .4;
            if (sheltered) p *= .5;
            return p;
        }

        public static bool CanGetPest(FarmPlot p, CropDefinition c)
            => Alive(p) && c != null && c.kind != CropKind.Tree && p.pest == Pest.None && !FarmRules.Mature(p, c);

        /// <summary>Loai sau theo dang cay: ruong nuoc co oc buou, rau la / day leo co sau xanh, con lai ray.</summary>
        public static Pest PestFor(CropDefinition c)
        {
            if (c.needsPaddy) return Pest.OcBuou;
            return c.kind == CropKind.Leafy || c.kind == CropKind.Vine || c.kind == CropKind.Bush || c.kind == CropKind.Ground ? Pest.SauXanh : Pest.Ray;
        }

        public static string PestName(Pest p) => p switch
        {
            Pest.SauXanh => "sâu xanh", Pest.Ray => "rầy", Pest.OcBuou => "ốc bươu", _ => "",
        };

        /// <summary>Sau xanh, oc buou bat tay duoc; ray phai phun nuoc toi ot hoac rac tro bep.</summary>
        public static bool HandPickable(Pest p) => p == Pest.SauXanh || p == Pest.OcBuou;

        public static bool Treats(string item, Pest p)
            => p != Pest.None && (item == "nuoc_toi_ot" || item == "tro_bep" || (item == null && HandPickable(p)));

        public static void Cure(FarmPlot p) { p.pest = Pest.None; p.pestDays = 0; }

        /// <summary>Thien dich tang san luong dua, bi va cay trai.</summary>
        public static bool PollinatorCrop(CropDefinition c)
            => c.kind == CropKind.Vine || c.kind == CropKind.Ground || c.kind == CropKind.Tree;

        // ------------------------------------------------------------------ giu giong
        public static bool SeedReady(FarmPlot p) => p.saved && p.seedAge >= SeedAgeDays;

        public static string CanSave(FarmPlot p, CropDefinition c)
        {
            if (!Alive(p) || c == null) return "Chừa giống ở ô đang có cây sống.";
            if (c.kind == CropKind.Tree) return "Cây ăn trái thì nhân giống bằng cành ghép.";
            if (p.saved) return "Cây này đang để già lấy giống.";
            if (!FarmRules.Mature(p, c)) return "Chờ cây chín rồi mới chừa làm giống.";
            return null;
        }

        /// <summary>Lay hat tu cay da gia: tra ve so hat va cap nhat dong giong. becameHome: vua du 3 vu thanh giong nha.</summary>
        public static int CollectSeeds(FarmPlot p, CropDefinition c, Strain strain, Random rng, out bool becameHome)
        {
            int q = FarmRules.Quality(p, c);
            bool chain = p.strainQ > 0;
            strain.crop = c.id;
            strain.q = q;
            strain.gen = chain ? Math.Max(1, p.strainGen + 1) : 1;
            becameHome = strain.name == null && strain.gen >= HomeStrainGens;
            int n = rng.Next(c.yieldMin + 1, c.yieldMax + 3);
            FarmRules.Clear(p);
            return n;
        }

        /// <summary>Gieo hat giu giong: o mang hang cay me va giong nha (neu da dat ten).</summary>
        public static void ApplyStrain(FarmPlot p, Strain strain)
        {
            if (strain == null) { p.strainQ = 1; p.strainGen = 0; return; }   // hat giu giong khong ro dong: hang thuong
            p.strainQ = strain.q + 1;
            p.strainGen = strain.gen;
            p.homeStrain = !string.IsNullOrEmpty(strain.name);
        }

        public static string[] StrainNames(string cropName, string player, string region)
        {
            var l = new List<string> { $"{cropName} Bình Yên" };
            if (!string.IsNullOrEmpty(player)) l.Add($"{cropName} nhà {player}");
            l.Add(!string.IsNullOrEmpty(region) ? $"{cropName} {region}" : $"{cropName} Hòn Đảo");
            return l.ToArray();
        }

        // ------------------------------------------------------------------ ghep cay
        public static string CanGraft(FarmPlot p, CropDefinition c, CropDefinition scion)
        {
            if (scion == null || scion.kind != CropKind.Tree) return "Cành ghép này không dùng được.";
            if (!Alive(p) || c == null || c.kind != CropKind.Tree) return "Ghép cành lên cây ăn trái đang sống.";
            if (p.graft != null) return "Cây này ghép rồi.";
            return null;
        }

        /// <summary>Ghep canh: cung loai = giong dau dong (dac san); khac loai = giong lai, dac tinh co dinh theo cap cay.</summary>
        public static GraftTrait Graft(FarmPlot p, CropDefinition c, CropDefinition scion)
        {
            p.graft = scion.id;
            if (scion.id == c.id) { p.trait = GraftTrait.None; p.extraSeasons = 0; return GraftTrait.None; }
            int h = 17;
            unchecked { foreach (char ch in c.id + "+" + scion.id) h = h * 31 + ch; }
            p.trait = (GraftTrait)(1 + (int)((uint)h % 3u));
            p.extraSeasons = p.trait == GraftTrait.TwoSeasons ? (int)scion.seasons : 0;
            return p.trait;
        }

        public static string TraitName(GraftTrait t) => t switch
        {
            GraftTrait.TwoSeasons => "ra trái hai mùa", GraftTrait.SaltTolerant => "chịu mặn", GraftTrait.BigFruit => "trái to", _ => "",
        };

        // ------------------------------------------------------------------ nguon giong (muc 4.8)
        public const float WildVitality = 50;     // vung da hoi sinh: chat bui co nhat duoc hat dai
        public const double WildChance = .3;
        public static readonly string[] WildSeeds = { "seed_rau_ma", "seed_ngai_cuu", "seed_rau_den", "seed_rau_thom" };
        public const int SwapCost = 3, SwapGive = 2;

        /// <summary>Phat bui co, lau, duong xi, dua dai o vung suc song tu 50: 30% roi 1 hat giong dai. null = khong.</summary>
        public static string WildSeed(WorldObjectType t, float vitality, Random rng)
        {
            bool plant = t == WorldObjectType.Spin || t == WorldObjectType.Reeds || t == WorldObjectType.Fern
                         || t == WorldObjectType.Bush || t == WorldObjectType.ScrewPine;
            if (!plant || vitality < WildVitality || rng.NextDouble() >= WildChance) return null;
            return WildSeeds[rng.Next(WildSeeds.Length)];
        }

        /// <summary>Giong hiem cua thang (Co Sau doi lay hat giu giong): co dinh theo thang, xoay vong cac giong khong phai cay an trai.</summary>
        public static string RareSeed(int month, IList<CropDefinition> crops)
        {
            var pool = new List<CropDefinition>();
            foreach (var c in crops) if (c && c.kind != CropKind.Tree && !c.wild) pool.Add(c);
            if (pool.Count == 0) return null;
            pool.Sort((a, b) => string.CompareOrdinal(a.id, b.id));
            return pool[(int)((uint)(month * 7 + 3) % (uint)pool.Count)].SeedId;
        }

        // ------------------------------------------------------------------ luan canh lua tom
        public static string CanBuildSluice(FarmPlot p)
        {
            if (p == null || p.ditch) return "Làm cống trên ô ruộng đất trũng đã đắp bờ bao.";
            if (!p.lowland) return "Cống chỉ cần ở ô đất trũng, nơi nước rong tràn tới.";
            if (!p.dike) return "Đắp bờ bao trước (xẻng + 2 đá), rồi mới làm cống.";
            if (p.sluice) return "Ô này có cống rồi.";
            return null;
        }

        /// <summary>Mo cong: nuoc lo vao, cay trong o chet (tru khi o trong).</summary>
        public static void OpenSluice(FarmPlot p)
        {
            if (!p.Empty && !p.dead) { p.dead = true; p.deadReason = "nước lợ"; p.hasFruit = false; }
            if (!p.Empty) FarmRules.Clear(p);
            p.sluiceOpen = true;
            p.saltDays = FarmRules.SaltDays;
        }

        /// <summary>Dong cong: bat dau rua man. Tom chua thu thi mat. Tra ve true neu mat tom.</summary>
        public static bool CloseSluice(FarmPlot p)
        {
            bool lost = p.shrimpAge > 0;
            p.sluiceOpen = false;
            p.shrimpAge = 0;
            p.saltDays = FarmRules.SaltDays;
            return lost;
        }

        public static string CanStock(FarmPlot p, Season s)
        {
            if (p == null || !p.sluiceOpen) return "Mở cống cho nước lợ vào ruộng rồi mới thả tôm.";
            if (p.shrimpAge > 0) return "Ruộng này đang nuôi tôm.";
            if (s != Season.Ha) return "Tôm giống thả vào mùa Hạ, lúc nước lợ ấm.";
            return null;
        }

        public static void Stock(FarmPlot p) { p.shrimpAge = 1; }

        public static bool ShrimpReady(FarmPlot p) => p.sluiceOpen && p.shrimpAge > ShrimpDays;

        public static int HarvestShrimp(FarmPlot p, Random rng)
        {
            p.shrimpAge = 0;
            p.shrimpBonus = true;
            return rng.Next(3, 6);
        }
    }
}
