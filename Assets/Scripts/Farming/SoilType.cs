// Loai dat (Docs/ThietKeGameplay.md muc 4.1), suy ra tu vung sinh thai cua ban sinh dia hinh.
using System;
using UnityEngine;

namespace SereneIsle
{
    public enum SoilType : byte { CatPha = 0, PhuSa = 1, DatDo = 2, DatLo = 3, Vuon = 4 }

    [Flags]
    public enum SoilMask { None = 0, CatPha = 1, PhuSa = 2, DatDo = 4, DatLo = 8, Vuon = 16, All = 31 }

    public static class Soils
    {
        public static string Name(SoilType s) => s switch
        {
            SoilType.CatPha => "Cát pha", SoilType.PhuSa => "Phù sa", SoilType.DatDo => "Đất đỏ",
            SoilType.DatLo => "Đất lợ", _ => "Đất vườn",
        };

        public static SoilMask Mask(SoilType s) => (SoilMask)(1 << (int)s);
        public static bool In(SoilMask m, SoilType s) => (m & Mask(s)) != 0;

        public static string Names(SoilMask m)
        {
            if (m == SoilMask.All) return "mọi loại đất";
            var parts = new System.Collections.Generic.List<string>();
            for (int i = 0; i < 5; i++) if (((int)m & (1 << i)) != 0) parts.Add(Name((SoilType)i).ToLowerInvariant());
            return string.Join(", ", parts);
        }

        /// <summary>Mau dat da cuoc (sRGB): kho / uot.</summary>
        public static Color Tilled(SoilType s, bool wet)
        {
            string hex = s switch
            {
                SoilType.CatPha => wet ? "#9a7a4e" : "#c9a874",
                SoilType.DatDo => wet ? "#6a2f1c" : "#9a4a2a",
                SoilType.DatLo => wet ? "#4a4436" : "#6f6446",
                SoilType.PhuSa => wet ? "#4e3826" : "#7a5a3a",
                _ => wet ? "#5b3a22" : "#8a5a34",
            };
            return MaterialLibrary.Hex(hex);
        }

        /// <summary>Loai dat cua o (i, j). home: vi tri leu (vuon nha trong ban kinh 14 o).</summary>
        public static SoilType Classify(IslandData d, int i, int j, Vector3? home)
        {
            int k = d.Idx(i, j);
            var b = d.Bio[k];
            if (b == Biome.Town) return SoilType.Vuon;
            if (home.HasValue && Vector2.Distance(new Vector2(i, j), new Vector2(home.Value.x, home.Value.z)) < 14f) return SoilType.Vuon;
            if (b == Biome.Mangrove) return SoilType.DatLo;
            int vn = d.VIdx(i, j);
            // bai cat + go cat ven bien (toi 8 o tu mep nuoc man) + dong co kho
            if (b == Biome.Beach || (d.Coast[k] > 0 && d.Coast[k] <= 8 && d.VRF[vn] <= .08f) || b == Biome.Savanna) return SoilType.CatPha;
            if (d.VRF[vn] > .08f) return SoilType.PhuSa;
            if (b == Biome.Desert || b == Biome.Alpine || d.HT[k] > 2.2f || d.VMS[vn] > .1f) return SoilType.DatDo;
            return d.Moist(i, j) > .5 ? SoilType.PhuSa : SoilType.DatDo;
        }
    }
}
