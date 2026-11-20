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

        public bool Empty => crop == null;
        public bool Wilting => crop != null && !dead && dryDays > 0;
    }

    public static class FarmRules
    {
        public const float OffSoilGrowth = .5f;
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

        public static bool Harvestable(FarmPlot p, CropDefinition c)
            => !p.dead && c != null && Mature(p, c) && (c.kind != CropKind.Tree || p.hasFruit);

        /// <summary>Hang khi thu hoach: 2 dac san (dung dat, chua tung quen tuoi), 1 ngon (mot trong hai), 0 thuong.</summary>
        public static int Quality(FarmPlot p, CropDefinition c)
        {
            bool soil = SoilOk(p, c), care = !p.everDry;
            return soil && care ? 2 : soil || care ? 1 : 0;
        }

        /// <summary>Kiem tra gieo duoc khong. Tra ve loi (tieng Viet) hoac null.</summary>
        public static string CanPlant(FarmPlot p, CropDefinition c, Season s)
        {
            if (c == null) return "Hạt này chưa trồng được.";
            if (p == null) return "Gieo hạt trên ô đất đã cuốc.";
            if (!p.Empty) return "Ô này đã có cây.";
            if (c.needsPaddy) return $"{c.displayName} cần ruộng nước (sẽ có ở giai đoạn sau).";
            if (!c.perennial && !c.InSeason(s))
                return $"{c.displayName} gieo mùa {CropCatalogData.SeasonText(c.seasons)}, bây giờ là mùa {LunarCalendar.SeasonName(s)}.";
            return null;
        }

        public static void Plant(FarmPlot p, CropDefinition c)
        {
            p.crop = c.id; p.growth = 0; p.dryDays = 0; p.everDry = false; p.dead = false; p.deadReason = null;
            p.fruitTimer = 0; p.hasFruit = false;
        }

        public static void Clear(FarmPlot p)
        {
            p.crop = null; p.growth = 0; p.dryDays = 0; p.everDry = false; p.dead = false; p.deadReason = null;
            p.fruitTimer = 0; p.hasFruit = false;
        }

        /// <summary>Qua 1 ngay. rained: troi mua (tu tuoi). Tra ve true neu cay vua chet.</summary>
        public static bool NewDay(FarmPlot p, CropDefinition c, Season s, bool rained = false)
        {
            bool wet = p.watered || rained;
            p.watered = false;
            if (p.Empty || p.dead || c == null) return false;

            if (!c.perennial && !c.InSeason(s)) return Kill(p, "trái mùa");

            float step = SoilOk(p, c) ? 1f : OffSoilGrowth;
            if (c.kind == CropKind.Tree && Mature(p, c))
            {
                // cay an trai da lon: khong can tuoi, ra trai dung mua
                if (c.InSeason(s))
                {
                    if (!p.hasFruit && ++p.fruitTimer >= Math.Max(1, c.regrowDays)) { p.hasFruit = true; p.fruitTimer = 0; }
                }
                else { p.hasFruit = false; p.fruitTimer = 0; }
                return false;
            }

            if (Mature(p, c)) return false;      // da chin, cho thu hoach
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
            int n = rng.Next(c.yieldMin, c.yieldMax + 1) + (quality == 2 ? 1 : 0);
            if (c.kind == CropKind.Tree) { p.hasFruit = false; p.fruitTimer = 0; }
            else if (c.regrowDays > 0) { p.growth = Math.Max(0, c.growDays - c.regrowDays); p.everDry = false; p.dryDays = 0; }
            else Clear(p);
            return n;
        }
    }
}
