// Bang giong cay 4 mua (Docs/ThietKeGameplay.md muc 4.2). Moi dong sinh ra: 1 CropDefinition, 1 item nong san, 1 item hat giong.
// Cay an trai (Tree): "hat giong" la cay giong, trong duoc moi mua, truong thanh sau growDays ngay roi ra trai dung mua.
// Gia, ngay lon la diem xuat phat de can chinh khi choi thu.
using System.Collections.Generic;
using UnityEngine;

namespace SereneIsle
{
    public static class CropCatalogData
    {
        public sealed class Row
        {
            public string id, name;
            public CropKind kind;
            public SeasonMask seasons;
            public int grow, regrow;
            public SoilMask soils;
            public int yMin, yMax;
            public int price, seedPrice;
            public float weight;
            public string leaf, prod;
            public int en, wt, hp;
            public bool paddy, trellis, perennial;
        }

        const SeasonMask X = SeasonMask.Xuan, H = SeasonMask.Ha, T = SeasonMask.Thu, D = SeasonMask.Dong, ALL = SeasonMask.All;
        const SoilMask CP = SoilMask.CatPha, PS = SoilMask.PhuSa, DD = SoilMask.DatDo, DL = SoilMask.DatLo, VU = SoilMask.Vuon, ANY = SoilMask.All;

        static List<Row> _rows;

        static Row C(string id, string name, CropKind kind, SeasonMask s, int grow, int regrow, SoilMask soils, int yMin, int yMax,
                     int price, int seedPrice, string leaf, string prod, float weight = .3f, int en = 0, int wt = 0, int hp = 0)
        {
            var r = new Row
            {
                id = id, name = name, kind = kind, seasons = s, grow = grow, regrow = regrow, soils = soils, yMin = yMin, yMax = yMax,
                price = price, seedPrice = seedPrice, leaf = leaf, prod = prod, weight = weight, en = en, wt = wt, hp = hp,
                perennial = kind == CropKind.Tree,
            };
            _rows.Add(r);
            return r;
        }

        /// <summary>Tat ca dong du lieu (tao 1 lan).</summary>
        public static IReadOnlyList<Row> Rows
        {
            get
            {
                if (_rows != null) return _rows;
                _rows = new List<Row>();
                const CropKind Lf = CropKind.Leafy, Rt = CropKind.Root, Bl = CropKind.Bulb, Bs = CropKind.Bush, Vn = CropKind.Vine,
                               Gr = CropKind.Ground, Gn = CropKind.Grain, Cn = CropKind.Corn, Fl = CropKind.Flower, Aq = CropKind.Aquatic, Tr = CropKind.Tree;

                // ---- Xuan (thang 1-2, mat, mua phun) ----
                C("cai_be", "Cải bẹ xanh", Lf, X, 5, 0, VU | PS, 1, 2, 18, 6, "#4f9e3a", "#7ac24e", .4f, en: 6);
                C("xa_lach", "Xà lách", Lf, X, 4, 0, VU, 1, 2, 15, 5, "#7cc05a", "#a8e07a", .2f, en: 5, wt: 3);
                C("hanh_la", "Hành lá", Lf, X, 6, 4, VU | PS, 2, 3, 8, 6, "#4f9e3a", "#6ab84a", .05f);
                C("cu_cai", "Củ cải trắng", Rt, X, 8, 0, CP | PS, 1, 1, 28, 8, "#5fae3e", "#f2efe6", .6f, en: 8);
                C("dua_leo", "Dưa leo", Vn, X, 10, 3, ANY, 2, 3, 16, 10, "#4f8a3a", "#4a8a32", .3f, en: 4, wt: 12).trellis = true;
                C("dau_que", "Đậu que", Vn, X, 9, 3, ANY, 2, 4, 12, 8, "#5fae3e", "#7ab84a", .1f).trellis = true;
                C("pumpkin", "Bí đỏ", Gr, X, 14, 0, DD | VU, 1, 1, 115, 15, "#5f8a3a", "#e8913a", 4f);
                C("nep", "Nếp", Gn, X, 24, 0, PS | DL, 3, 5, 20, 10, "#a8c84a", "#f0e6c8", .5f).paddy = true;
                C("lua", "Lúa", Gn, X | T, 24, 0, PS | DL, 4, 6, 18, 8, "#9ab84a", "#e0c060", .5f).paddy = true;
                C("buoi", "Bưởi", Tr, X, 28, 7, PS | VU, 2, 3, 40, 60, "#3f7a2a", "#d8e07a", 1.2f, en: 8, wt: 10);
                C("man", "Mận", Tr, X, 28, 6, PS | VU, 3, 5, 14, 45, "#4a8a3a", "#e05a6a", .1f, en: 4, wt: 8);

                // ---- Ha (thang 3-4, nang gat) ----
                C("melon", "Dưa hấu", Gr, H | D, 12, 0, CP, 1, 1, 170, 22, "#3f8a3a", "#3a7a32", 5f, en: 18, wt: 30, hp: 4);
                C("bap", "Bắp", Cn, H, 12, 0, DD | PS, 1, 2, 26, 8, "#6aa84a", "#f0d050", .4f, en: 10);
                C("dau_phong", "Đậu phộng", Rt, H, 14, 0, CP, 2, 4, 10, 6, "#6aae4a", "#d9b27a", .05f, en: 4);
                C("khoai_lang", "Khoai lang", Rt, H, 16, 0, CP | DD, 2, 3, 24, 8, "#5a9a3a", "#a8507a", .4f, en: 12);
                C("ot", "Ớt", Bs, H | T, 12, 4, VU | DD, 3, 5, 6, 6, "#4f8a3a", "#e0302a", .02f);
                C("ca_tim", "Cà tím", Bs, H, 12, 4, VU | PS, 2, 3, 14, 8, "#4f7a3a", "#6a3a8a", .25f);
                C("muop", "Mướp", Vn, H, 10, 3, ANY, 1, 2, 16, 8, "#5a9a3a", "#7aa84a", .4f).trellis = true;
                C("kho_qua", "Khổ qua", Vn, H, 10, 3, ANY, 1, 2, 18, 8, "#4f8a3a", "#8ac84a", .3f).trellis = true;
                C("rau_muong", "Rau muống", Lf, H, 5, 3, PS | DL, 2, 3, 8, 5, "#4f9e3a", "#6ab84a", .2f);
                C("sen", "Hoa sen", Aq, H, 20, 0, PS | DL, 2, 3, 45, 20, "#3f8a5a", "#f2a0b8", .2f).paddy = true;
                C("xoai", "Xoài", Tr, H, 28, 7, DD | CP | VU, 3, 4, 30, 70, "#3f7a2a", "#f0c040", .5f, en: 10, wt: 6);
                C("chom_chom", "Chôm chôm", Tr, H, 28, 6, PS | VU, 4, 6, 12, 60, "#4a8a3a", "#d8302a", .05f, en: 4, wt: 4);

                // ---- Thu (thang 5-6, mua mua bao) ----
                C("khoai_mon", "Khoai môn", Rt, T, 18, 0, PS | DL, 1, 2, 34, 10, "#3f8a4a", "#b8a0c8", .6f, en: 14);
                C("bi_dao", "Bí đao", Vn, T, 14, 0, ANY, 1, 1, 40, 10, "#4f8a3a", "#7ab07a", 2f).trellis = true;
                C("dau_bap", "Đậu bắp", Bs, T, 8, 3, VU, 2, 3, 10, 6, "#5a9a3a", "#7ac04a", .05f);
                C("rau_den", "Rau dền", Lf, T, 5, 0, VU | PS, 1, 2, 10, 4, "#6a8a3a", "#b03a4a", .2f);
                C("mong_toi", "Mồng tơi", Vn, T, 6, 3, VU, 2, 3, 9, 5, "#3f7a2a", "#4f8a3a", .2f).trellis = true;
                C("gung", "Gừng", Rt, T, 20, 0, DD, 1, 2, 38, 12, "#5a9a3a", "#d8b070", .2f);
                C("nghe", "Nghệ", Rt, T, 22, 0, DD, 1, 2, 40, 12, "#5a9a3a", "#f0a020", .2f);
                C("bong_sung", "Bông súng", Aq, T, 10, 4, PS | DL, 2, 3, 14, 8, "#3f8a5a", "#c870d8", .2f).paddy = true;
                C("mang_cau", "Mãng cầu", Tr, T, 28, 7, DD | VU, 2, 3, 28, 60, "#4a8a3a", "#9ac87a", .4f, en: 8);
                C("oi", "Ổi", Tr, T, 28, 6, ANY, 3, 4, 12, 45, "#4a8a3a", "#b8d870", .2f, en: 5, wt: 4);

                // ---- Dong (thang 7-8, kho, gio bac) ----
                C("toi", "Tỏi đảo", Bl, D, 24, 0, CP | DD, 2, 3, 60, 20, "#6aa84a", "#f2ece0", .05f);
                C("hanh_tim", "Hành tím", Bl, D, 18, 0, CP, 2, 4, 22, 10, "#5fae3e", "#a04a7a", .05f);
                C("cai_thao", "Cải thảo", Lf, D, 10, 0, VU, 1, 1, 30, 8, "#8ac86a", "#d8ecb0", 1f, en: 6);
                C("su_hao", "Su hào", Rt, D, 12, 0, VU, 1, 1, 26, 8, "#6aa84a", "#b8d890", .5f, en: 6);
                C("ca_rot", "Cà rốt", Rt, D, 12, 0, CP | VU, 1, 2, 22, 8, "#5fae3e", "#f08a2a", .15f, en: 8);
                C("tomato", "Cà chua", Bs, D, 14, 4, VU | PS, 2, 4, 32, 6, "#5fae3e", "#d8452f", .3f, en: 10, wt: 8);
                C("van_tho", "Hoa vạn thọ", Fl, D, 12, 0, VU, 2, 3, 20, 8, "#4f8a3a", "#f0a020", .05f);
                C("cuc", "Hoa cúc mâm xôi", Fl, D, 16, 0, VU, 1, 2, 45, 15, "#4f8a3a", "#f0d040", .3f);
                C("quyt", "Quýt", Tr, D, 28, 7, PS | VU, 4, 6, 14, 55, "#3f7a2a", "#f08a20", .1f, en: 5, wt: 6);

                // ---- Quanh nam ----
                C("rau_thom", "Rau thơm", Lf, ALL, 6, 3, VU, 1, 2, 8, 6, "#3f8a3a", "#5fae3e", .05f);
                C("sa", "Sả", Lf, ALL, 12, 6, ANY, 2, 3, 8, 6, "#8ab84a", "#c8d870", .1f).perennial = true;
                C("chuoi", "Chuối", Tr, ALL, 28, 7, PS | VU, 4, 8, 8, 40, "#5a9a3a", "#f0d050", .2f, en: 10);
                return _rows;
            }
        }

        public static CropDefinition CreateDefinition(Row r)
        {
            var c = ScriptableObject.CreateInstance<CropDefinition>();
            c.name = r.id;
            c.id = r.id; c.displayName = r.name; c.produce = r.id; c.kind = r.kind;
            c.seasons = r.seasons; c.perennial = r.perennial; c.growDays = r.grow; c.regrowDays = r.regrow;
            c.soils = r.soils; c.needsPaddy = r.paddy; c.trellis = r.trellis;
            c.yieldMin = r.yMin; c.yieldMax = r.yMax;
            c.leafColor = MaterialLibrary.Hex(r.leaf); c.produceColor = MaterialLibrary.Hex(r.prod);
            return c;
        }

        public static List<CropDefinition> CreateAll()
        {
            var list = new List<CropDefinition>();
            foreach (var r in Rows) list.Add(CreateDefinition(r));
            return list;
        }

        /// <summary>Bieu tuong tam cho tung dang cay (dung lai hinh ve co san, to mau theo cay).</summary>
        public static IconShape IconOf(Row r) => r.id switch
        {
            "tomato" => IconShape.Tomato, "pumpkin" => IconShape.Pumpkin, "melon" => IconShape.Melon,
            "ot" => IconShape.Chili, "chuoi" => IconShape.Banana, "chom_chom" => IconShape.Berry, "man" => IconShape.Berry,
            "cu_cai" or "su_hao" or "khoai_lang" or "khoai_mon" or "dau_phong" => IconShape.RootRound,
            "ca_tim" or "dau_bap" => IconShape.Gourd,
            "sa" => IconShape.Fiber,
            _ => r.kind switch
            {
                CropKind.Leafy => IconShape.LeafGreens, CropKind.Root => IconShape.Root, CropKind.Bulb => IconShape.Bulb,
                CropKind.Grain => IconShape.Wheat, CropKind.Corn => IconShape.Corn, CropKind.Vine => IconShape.Gourd,
                CropKind.Ground => IconShape.Pumpkin, CropKind.Flower => IconShape.Flower, CropKind.Aquatic => IconShape.Lotus,
                CropKind.Tree => IconShape.Fruit, _ => IconShape.Tomato,
            },
        };

        public static string SeasonText(SeasonMask m)
        {
            if (m == SeasonMask.All) return "quanh năm";
            var parts = new List<string>();
            for (int i = 0; i < 4; i++) if (((int)m & (1 << i)) != 0) parts.Add(LunarCalendar.SeasonName((Season)i));
            return string.Join(", ", parts);
        }
    }
}
