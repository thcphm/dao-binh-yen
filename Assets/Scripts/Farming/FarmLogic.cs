// Luat trong trot (Docs/ThietKeGameplay.md muc 4 + muc 8), thuan C# de test EditMode.
//  - Tuoi trong ngay thi cay lon 1 ngay (sai loai dat: 0.5 ngay).
//  - Quen tuoi: ngay dau cay heo, ngay thu hai cay chet. Cay da chin thi khong heo.
//  - Sang mua khac: cay thuong trai mua chet. Cay lau nam song qua, chi ra trai dung mua.
//  - Hang nong san: thuong / ngon / dac san theo dung loai dat va tuoi deu.
using System;
using System.Collections.Generic;

namespace SereneIsle
{
    [Serializable]
    public sealed class FarmPlot
    {
        public int k;                 // chi so o
        public SoilType soil;
        public bool watered;          // da tuoi hom nay
        public string crop;           // null = dat trong
        public float growth;          // so ngay da lon
        public int dryDays;           // so ngay lien quen tuoi
        public bool everDry;          // tung bi quen tuoi trong vu nay
        public bool dead;
        public string deadReason;
        public int fruitTimer;        // cay an trai: dem ngay ra trai
        public bool hasFruit;
        // ruong nuoc, dat trung, phan bon (muc 4.3 - 4.6)
        public bool ditch;            // o nay la muong dan nuoc (khong trong cay)
        public bool lowland;          // dat trung: nuoc rong co the tran toi
        public bool dike;             // da dap bo bao ngan man
        public int saltDays;          // con man bao nhieu ngay nua
        public bool fertilized;       // da bon phan vu nay
        // vuon ruong nang cao (muc 4.4 - 4.6, FarmCare)
        public Pest pest;             // sau benh dang co
        public int pestDays;          // so ngay bi sau
        public bool nearLegume;       // ke cay dau (xen canh): +1 hang
        public bool saved;            // chua cay nay lam giong (de gia lay hat)
        public int seedAge;           // so ngay da gia (tu khi chin + chua giong)
        public int strainQ;           // gieo tu hat giu giong: hang cay me + 1 (0 = hat mua)
        public int strainGen;         // the he giong (gieo tu hat giu giong)
        public bool homeStrain;       // giong nha da dat ten: +1 hang
        public string graft;          // cay an trai: canh ghep (id cay), null = chua ghep
        public GraftTrait trait;      // ghep khac loai: dac tinh moi
        public int extraSeasons;      // ghep ra trai hai mua: mua cua canh ghep (SeasonMask)
        public bool sluice;           // co cong chan nuoc (luan canh lua tom)
        public bool sluiceOpen;       // cong dang mo: ruong thanh dam nuoc lo nuoi tom
        public int shrimpAge;         // 0 = chua tha tom, 1.. = so ngay + 1
        public bool shrimpBonus;      // vua nuoi tom xong: vu lua sau tu duoc bon phan

        public bool Empty => crop == null;
        public bool Wilting => crop != null && !dead && dryDays > 0;
    }

    public static class FarmRules
    {
        public const float OffSoilGrowth = .5f;
        public const float FertilizerBonus = .5f;    // bon phan: lon them nua ngay moi ngay tuoi
        public const int SaltDays = 7;               // dat nhiem man 7 ngay (rua bang nuoc ngot thi nhanh gap doi)
        public const int DikeStones = 2;
        public static readonly string[] QualityNames = { "thường", "ngon", "đặc sản" };

        public static bool SoilOk(FarmPlot p, CropDefinition c) => Soils.In(c.soils, p.soil);

        public static bool Mature(FarmPlot p, CropDefinition c) => p.growth >= c.growDays;

        /// <summary>0: mam, 1: non, 2: lon, 3: chin (cay an trai: 3 = co trai).</summary>
        public static int Stage(FarmPlot p, CropDefinition c)
        {
            if (Mature(p, c)) return c.kind == CropKind.Tree && !p.hasFruit ? 2 : 3;
            float f = c.growDays > 0 ? p.growth / c.growDays : 1;
            return f < .34f ? 0 : f < .67f ? 1 : 2;
        }

        /// <summary>Thu hoach duoc: cay chin (cay an trai: co trai); cay chua giong thi phai gia du ngay moi lay hat.</summary>
        public static bool Harvestable(FarmPlot p, CropDefinition c)
            => !p.dead && c != null && Mature(p, c) && (c.kind != CropKind.Tree || p.hasFruit) && (!p.saved || FarmCare.SeedReady(p));

        /// <summary>Hang khi thu hoach: 2 dac san (dung dat, chua tung quen tuoi), 1 ngon (mot trong hai), 0 thuong.
        /// Them (muc 4.4, 4.5): bon phan it nhat ngon, ke cay dau +1, hat giu giong mang hang cay me, giong nha +1,
        /// dang bi sau -1; cay an trai chua ghep chi cho hang thuong.</summary>
        public static int Quality(FarmPlot p, CropDefinition c)
        {
            bool soil = SoilOk(p, c), care = !p.everDry;
            int q = soil && care ? 2 : soil || care ? 1 : 0;
            if (p.fertilized) q = Math.Max(q, 1);   // bon phan: it nhat hang ngon
            if (p.nearLegume) q++;
            if (p.strainQ > 0) q = Math.Max(q, p.strainQ - 1);
            if (p.homeStrain) q++;
            if (p.pest != Pest.None) q--;
            if (c.kind == CropKind.Tree && p.graft == null) q = 0;
            return Math.Max(0, Math.Min(2, q));
        }

        /// <summary>Kiem tra gieo duoc khong. flooded: o ke muong / nguon nuoc ngot (ruong nuoc).
        /// sheltered: trong nha luoi (gieo trai mua duoc). Tra ve loi hoac null.</summary>
        public static string CanPlant(FarmPlot p, CropDefinition c, Season s, bool flooded = false, bool sheltered = false)
        {
            if (c == null) return "Hạt này chưa trồng được.";
            if (p == null) return "Gieo hạt trên ô đất đã cuốc.";
            if (p.ditch) return "Đây là mương nước, gieo ở ô ruộng bên cạnh.";
            if (p.sluiceOpen) return "Cống đang mở, ruộng đang ngập nước lợ. Đóng cống rồi rửa mặn mới cấy được.";
            if (!p.Empty) return "Ô này đã có cây.";
            if (p.saltDays > 0 && !(c.saltTolerant && p.saltDays <= FarmCare.TolerantSalt))
                return $"Đất còn mặn {p.saltDays} ngày. Tưới nước ngọt để rửa mặn nhanh hơn."
                       + (c.saltTolerant ? "" : $" Lúa chịu mặn cấy được khi còn {FarmCare.TolerantSalt} ngày.");
            if (c.needsPaddy && !flooded) return $"{c.displayName} cần ruộng ngập nước: cuốc mương dẫn nước ngọt tới sát ô này.";
            if (!c.perennial && !c.InSeason(s) && !sheltered)
                return $"{c.displayName} gieo mùa {CropCatalogData.SeasonText(c.seasons)}, bây giờ là mùa {LunarCalendar.SeasonName(s)}.";
            return null;
        }

        public static void Plant(FarmPlot p, CropDefinition c)
        {
            Reset(p);
            p.crop = c.id;
            if (p.shrimpBonus) { p.fertilized = true; p.shrimpBonus = false; }   // dat sau vu tom: tu bon
        }

        public static void Clear(FarmPlot p) => Reset(p);

        static void Reset(FarmPlot p)
        {
            p.crop = null; p.growth = 0; p.dryDays = 0; p.everDry = false; p.dead = false; p.deadReason = null;
            p.fruitTimer = 0; p.hasFruit = false; p.fertilized = false;
            p.pest = Pest.None; p.pestDays = 0; p.saved = false; p.seedAge = 0;
            p.strainQ = 0; p.strainGen = 0; p.homeStrain = false;
            p.graft = null; p.trait = GraftTrait.None; p.extraSeasons = 0;
        }

        /// <summary>Cay ra trai mua s (tinh ca mua cua canh ghep "hai mua").</summary>
        public static bool FruitSeason(FarmPlot p, CropDefinition c, Season s)
            => c.InSeason(s) || (p.extraSeasons != 0 && LunarCalendar.In((SeasonMask)p.extraSeasons, s));

        /// <summary>Qua 1 ngay. rained: troi mua (tu tuoi). sheltered: trong nha luoi (khong chet trai mua).
        /// Tra ve true neu cay vua chet.</summary>
        public static bool NewDay(FarmPlot p, CropDefinition c, Season s, bool rained = false, bool sheltered = false)
        {
            bool wet = p.watered || rained;
            p.watered = false;
            // cong mo: ruong la dam nuoc lo, tom lon dan, dat giu man
            if (p.sluiceOpen)
            {
                p.saltDays = SaltDays;
                if (p.shrimpAge > 0) p.shrimpAge++;
                return false;
            }
            // dat man nhat dan; tuoi nuoc ngot thi rua nhanh gap doi
            if (p.saltDays > 0) p.saltDays = Math.Max(0, p.saltDays - (wet ? 2 : 1));
            if (p.Empty || p.dead || c == null) return false;

            if (!c.perennial && !c.InSeason(s) && !sheltered) return Kill(p, "trái mùa");

            // sau benh (muc 4.5): cay dung lon, de lau thi chet
            if (p.pest != Pest.None && ++p.pestDays >= FarmCare.PestKillDays) return Kill(p, "sâu bệnh");

            float step = (SoilOk(p, c) ? 1f : OffSoilGrowth) + (p.fertilized ? FertilizerBonus : 0);
            if (p.pest != Pest.None) step = 0;
            if (c.kind == CropKind.Tree && Mature(p, c))
            {
                // cay an trai da lon: khong can tuoi, ra trai dung mua
                if (FruitSeason(p, c, s))
                {
                    if (!p.hasFruit && ++p.fruitTimer >= Math.Max(1, c.regrowDays)) { p.hasFruit = true; p.fruitTimer = 0; }
                }
                else { p.hasFruit = false; p.fruitTimer = 0; }
                return false;
            }

            if (Mature(p, c))
            {
                if (p.saved) p.seedAge++;        // cay chua giong: gia dan de lay hat
                return false;                    // da chin, cho thu hoach
            }
            if (wet)
            {
                p.growth = Math.Min(c.growDays, p.growth + step);
                p.dryDays = 0;
                return false;
            }
            p.everDry = true;
            if (++p.dryDays >= 2) return Kill(p, "khô héo");
            return false;
        }

        static bool Kill(FarmPlot p, string why) { p.dead = true; p.deadReason = why; p.hasFruit = false; return true; }

        /// <summary>Thu hoach: tra ve so luong; cap nhat o (thu lai / het cay).</summary>
        public static int Harvest(FarmPlot p, CropDefinition c, Random rng, out int quality)
        {
            quality = Quality(p, c);
            int n = rng.Next(c.yieldMin, c.yieldMax + 1) + (quality == 2 ? 1 : 0) + (p.trait == GraftTrait.BigFruit ? 1 : 0);
            if (c.kind == CropKind.Tree) { p.hasFruit = false; p.fruitTimer = 0; }
            else if (c.regrowDays > 0) { p.growth = Math.Max(0, c.growDays - c.regrowDays); p.everDry = false; p.dryDays = 0; }
            else Clear(p);
            return n;
        }

        // ------------------------------------------------------------ dat trung, nuoc man (muc 4.6)
        /// <summary>Muc nuoc cao nhat trong ngay tran toi o dat trung chua dap bo bao: cay chet man, dat man 7 ngay.
        /// tolerant: cay chiu man (lua chiu man, cay ghep chiu man) khong chet, dat van man. Tra ve true neu o vua bi man.</summary>
        public static bool SaltFlood(FarmPlot p, float tileHeight, float dayHigh, bool tolerant = false)
        {
            if (!p.lowland || p.dike || p.ditch || p.sluiceOpen || dayHigh < tileHeight - .02f) return false;
            p.saltDays = SaltDays;
            if (!p.Empty && !p.dead && !tolerant && p.trait != GraftTrait.SaltTolerant) Kill(p, "nhiễm mặn");
            return true;
        }

        public const double StormKillChance = .7;

        /// <summary>Bao (muc 8): cay ngan ngay ngoai troi hu 70%, cay an trai rung het trai cua mua.
        /// Tra ve 1 neu cay chet, 2 neu cay an trai rung trai, 0 neu khong sao.</summary>
        public static int StormDamage(FarmPlot p, CropDefinition c, Random rng)
        {
            if (p.ditch || p.Empty || p.dead || c == null) return 0;
            if (c.kind == CropKind.Tree)
            {
                if (!p.hasFruit) return 0;
                p.hasFruit = false; p.fruitTimer = 0;
                return 2;
            }
            if (rng.NextDouble() >= StormKillChance) return 0;
            Kill(p, "bão");
            return 1;
        }

        /// <summary>Bon phan: chi khi o dang co cay song va chua bon vu nay.</summary>
        public static string CanFertilize(FarmPlot p)
        {
            if (p == null || p.ditch) return "Bón phân cho ô đang có cây.";
            if (p.Empty || p.dead) return "Ô này chưa có cây để bón.";
            if (p.fertilized) return "Ô này bón phân rồi.";
            return null;
        }
    }

    /// <summary>Mang muong dan nuoc ngot (muc 4.3): muong co nuoc khi noi lien (4 huong) toi o nuoc ngot;
    /// o ruong canh muong co nuoc hoac canh nuoc ngot thi ngap nuoc (trong lua, sen duoc va tu tuoi moi ngay).</summary>
    public static class FarmWater
    {
        static IEnumerable<int> Neighbors(int k, int n)
        {
            int i = k % n, j = k / n;
            if (i > 0) yield return k - 1;
            if (i < n - 1) yield return k + 1;
            if (j > 0) yield return k - n;
            if (j < n - 1) yield return k + n;
        }

        /// <summary>Cac o muong dang co nuoc. isFresh(k): o k la nuoc ngot (song, ao, ho).</summary>
        public static HashSet<int> WetDitches(ICollection<int> ditches, int n, Func<int, bool> isFresh)
        {
            var wet = new HashSet<int>();
            var stack = new Stack<int>();
            foreach (int k in ditches)
            {
                if (wet.Contains(k)) continue;
                bool source = false;
                foreach (int q in Neighbors(k, n)) if (isFresh(q)) { source = true; break; }
                if (!source) continue;
                wet.Add(k); stack.Push(k);
                while (stack.Count > 0)
                {
                    int c = stack.Pop();
                    foreach (int q in Neighbors(c, n))
                        if (ditches.Contains(q) && wet.Add(q)) stack.Push(q);
                }
            }
            return wet;
        }

        /// <summary>O k ngap nuoc: ke muong co nuoc hoac ke nuoc ngot.</summary>
        public static bool Flooded(int k, int n, ICollection<int> wetDitches, Func<int, bool> isFresh)
        {
            foreach (int q in Neighbors(k, n)) if (wetDitches.Contains(q) || isFresh(q)) return true;
            return false;
        }

        /// <summary>Dao muong duoc o k: ke nuoc ngot hoac ke muong khac.</summary>
        public static bool CanDig(int k, int n, ICollection<int> ditches, Func<int, bool> isFresh)
        {
            foreach (int q in Neighbors(k, n)) if (ditches.Contains(q) || isFresh(q)) return true;
            return false;
        }
    }
}
