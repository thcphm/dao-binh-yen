// Bang du lieu item port tu serene-isle.html (ITEMS dong 286-307, FISH dong 312-322, BUGS dong 323-324, TOOLS dong 347).
// Ten, gia, hieu ung an uong giu nguyen ban web. Khoi luong (kg) va mau bieu tuong la them moi cho ban Unity.
// Dung boi: editor (tao asset trong Assets/Data/Items) va du phong luc chay neu thieu asset.
using System.Collections.Generic;
using UnityEngine;

namespace SereneIsle
{
    public static class ItemCatalogData
    {
        static List<ItemDefinition> _list;

        static ItemDefinition R(string id, string name, ItemCategory cat, int price, float w, IconShape icon, string c1, string c2,
                                int hp = 0, int wt = 0, int en = 0)
        {
            var d = ScriptableObject.CreateInstance<ItemDefinition>();
            d.name = id;
            d.id = id; d.displayName = name; d.category = cat; d.price = price; d.weight = w;
            d.icon = icon; d.iconColor = MaterialLibrary.Hex(c1); d.iconColor2 = MaterialLibrary.Hex(c2);
            d.eatHp = hp; d.eatWater = wt; d.eatEnergy = en;
            _list.Add(d);
            return d;
        }

        static void Fish(string id, string name, int price, int diff, string hab, string when, IconShape icon, string c1, string c2)
        {
            var d = R(id, name, ItemCategory.Fish, price, .2f + diff * .6f, icon, c1, c2);
            d.fishDifficulty = diff; d.habitats = hab.Split(','); d.activeTime = when;
        }

        static void Bug(string id, string name, int price, string when, string bio, IconShape icon, string body, string wing)
        {
            var d = R(id, name, ItemCategory.Bug, price, .02f, icon, body, wing);
            d.activeTime = when; d.habitats = bio.Split(',');
        }

        /// <summary>Tao toan bo item (ScriptableObject trong bo nho).</summary>
        public static List<ItemDefinition> CreateAll()
        {
            _list = new List<ItemDefinition>();
            const ItemCategory T = ItemCategory.Tool, M = ItemCategory.Material, F = ItemCategory.Food, C = ItemCategory.Crop,
                               S = ItemCategory.Seed, B = ItemCategory.Bag, A = ItemCategory.Animal, P = ItemCategory.Placeable, X = ItemCategory.Misc;

            // ---- dung cu (TOOLS) - khong nam trong tui ----
            R("axe", "Rìu", T, 0, 0, IconShape.ToolAxe, "#c3ccd2", "#8a5a2e");
            R("shovel", "Xẻng", T, 0, 0, IconShape.ToolShovel, "#c3ccd2", "#8a5a2e");
            R("hoe", "Cuốc đất", T, 0, 0, IconShape.ToolHoe, "#c3ccd2", "#8a5a2e");
            R("can", "Bình tưới", T, 0, 0, IconShape.ToolCan, "#6fa7c9", "#3f6f8f");
            R("seed", "Hạt giống", T, 0, 0, IconShape.ToolSeed, "#d8b27a", "#5fae3e");
            R("pick", "Cuốc chim", T, 0, 0, IconShape.ToolPick, "#7d8890", "#8a5a2e");
            R("rod", "Cần câu", T, 0, 0, IconShape.ToolRod, "#8a5a2e", "#d83a2a");
            R("net", "Vợt", T, 0, 0, IconShape.ToolNet, "#5a7d3a", "#8a5a2e");
            R("build", "Xây dựng", T, 0, 0, IconShape.ToolBuild, "#7d8890", "#8a5a2e");

            // ---- vat lieu ----
            R("wood", "Gỗ", M, 4, 1f, IconShape.Log, "#8a5a2e", "#d9b27a");
            R("stone", "Đá", M, 3, 1.5f, IconShape.Rock, "#9a958a", "#6f6a60");
            R("ore", "Quặng đồng", M, 14, 2f, IconShape.Ore, "#7d7468", "#e0892f");
            R("fiber", "Cỏ gai", M, 3, .1f, IconShape.Fiber, "#7ab04a", "#3f7a2a");

            // ---- do an / uong ----
            R("coconut", "Quả dừa", F, 12, .8f, IconShape.Coconut, "#7a4e2a", "#f6f0e0", wt: 22, en: 8);
            R("bottle", "Chai nước", F, 6, .6f, IconShape.Bottle, "#8fd3f0", "#3f6f8f", wt: 40);
            R("berry", "Quả mọng", F, 6, .05f, IconShape.Berry, "#c8304a", "#5fae3e", en: 8, wt: 4);
            R("fishcook", "Cá nướng", F, 30, .5f, IconShape.Skewer, "#c98a4a", "#8a5a2e", en: 35, hp: 10);
            R("pie", "Bánh nhân thịt", F, 20, .4f, IconShape.Pie, "#d9a05a", "#8a5a2e", en: 45, hp: 15);
            R("egg", "Trứng gà", F, 22, .06f, IconShape.Egg, "#f6ecd8", "#c8b08a", en: 8);
            R("goatmilk", "Sữa dê", F, 55, 1f, IconShape.Milk, "#f6f3ea", "#8fb0c8", wt: 15, en: 10);
            R("milk", "Sữa bò", F, 75, 1f, IconShape.Milk, "#ffffff", "#3d9bd6", wt: 20, en: 12);
            R("soup", "Súp cà chua", F, 70, .6f, IconShape.Bowl, "#d8452f", "#c8a070", en: 25, wt: 12, hp: 5);
            R("bread", "Bánh mì", F, 65, .4f, IconShape.Bread, "#d9a05a", "#a8703a", en: 32);
            R("omelet", "Trứng chiên", F, 60, .3f, IconShape.Omelet, "#ffd34d", "#f6f0e0", en: 25, hp: 8);
            R("stew", "Bí ngô hầm sữa", F, 260, .8f, IconShape.Bowl, "#e8913a", "#c8a070", en: 50, hp: 20, wt: 10);
            R("hotpot", "Lẩu cá chua", F, 220, 1.2f, IconShape.Bowl, "#d8452f", "#7d8890", en: 60, hp: 25, wt: 20);
            R("smoothie", "Sinh tố dừa", F, 45, .5f, IconShape.Cup, "#f6f0e0", "#5fae3e", wt: 45, en: 10);

            // ---- nong san + hat giong ----
            R("tomato", "Cà chua bụi", C, 32, .3f, IconShape.Tomato, "#d8452f", "#5fae3e", en: 10, wt: 8);
            R("wheat", "Lúa mì", C, 20, .3f, IconShape.Wheat, "#e0b84a", "#a8862a");
            R("pumpkin", "Bí ngô", C, 115, 4f, IconShape.Pumpkin, "#e8913a", "#5f8a3a");
            R("melon", "Dưa hấu", C, 170, 5f, IconShape.Melon, "#3f8a3a", "#d8452f", en: 18, wt: 30, hp: 4);
            R("seed_tomato", "Hạt cà chua", S, 6, .02f, IconShape.SeedBag, "#d8b27a", "#d8452f").seedOf = "tomato";
            R("seed_wheat", "Hạt lúa mì", S, 4, .02f, IconShape.SeedBag, "#d8b27a", "#e0b84a").seedOf = "wheat";
            R("seed_pumpkin", "Hạt bí ngô", S, 15, .02f, IconShape.SeedBag, "#d8b27a", "#e8913a").seedOf = "pumpkin";
            R("seed_melon", "Hạt dưa hấu", S, 22, .02f, IconShape.SeedBag, "#d8b27a", "#3f8a3a").seedOf = "melon";

            // ---- linh tinh ----
            R("worm", "Giun đất", X, 3, .02f, IconShape.Worm, "#d98a8a", "#a85a5a");
            R("shell", "Vỏ sò", X, 8, .05f, IconShape.Shell, "#f2d7c4", "#d9a58a");
            R("oldcoin", "Đồng xu cổ", X, 60, .02f, IconShape.Coin, "#e0a21f", "#9a6510");

            // ---- ba lo, vat nuoi (khong vao tui) ----
            R("bag1", "Ba lô vải", B, 0, 0, IconShape.Backpack, "#c9a46a", "#8a6a40").bagLevel = 1;
            R("bag2", "Ba lô da", B, 0, 0, IconShape.Backpack, "#8a5a2e", "#5a3a22").bagLevel = 2;
            R("a_chick", "Gà con", A, 0, 0, IconShape.Chick, "#ffe066", "#e8913a");
            R("a_goat", "Dê con", A, 0, 0, IconShape.Goat, "#f2ede2", "#8a7a6a");
            R("a_cow", "Bê con", A, 0, 0, IconShape.Cow, "#f6f0e0", "#3a2715");

            // ---- do dat duoc ----
            R("campfire", "Lửa trại", P, 20, 2f, IconShape.Campfire, "#e8913a", "#8a5a2e");
            R("torch", "Đuốc", P, 5, .3f, IconShape.Torch, "#ffc93c", "#8a5a2e");
            R("fence", "Hàng rào", P, 6, 1.5f, IconShape.Fence, "#a67e50", "#6b4a2a");
            R("path", "Lát đá", P, 4, 2f, IconShape.PathTile, "#bdb4a3", "#8a8478");
            R("sprinkler", "Vòi tưới tự động", P, 40, 1.5f, IconShape.Sprinkler, "#7d8890", "#3d9bd6");
            R("scarecrow", "Bù nhìn", P, 15, 2f, IconShape.Scarecrow, "#e0b84a", "#3f7fb0");

            // ---- con trung (BUGS: mau than / canh, gio, biome) ----
            Bug("b_fly", "Bướm xanh", 20, "day", "2,5,6,9,10", IconShape.Butterfly, "#2b5fa8", "#5bb6f0");
            Bug("b_beetle", "Bọ hung", 30, "day", "3,6,8", IconShape.Beetle, "#3b2a1c", "#6a4a2a");
            Bug("b_cricket", "Dế bụi", 24, "day", "1,4,9,7,10", IconShape.Cricket, "#6f8a2a", "#a3b84a");
            Bug("b_firefly", "Đom đóm", 45, "night", "2,3,5,7,6", IconShape.Firefly, "#2d2a10", "#e8ff7a");
            Bug("b_moth", "Bướm đêm", 36, "night", "2,3,4,8,9", IconShape.Butterfly, "#7a6a55", "#d9c7a3");
            Bug("b_jewel", "Bọ rùa ngọc", 160, "any", "3,4,6", IconShape.Beetle, "#1f8f6a", "#3fe0b0");

            // ---- ca (FISH: gia, do kho, noi song, gio) ----
            Fish("f_trout", "Cá mú san hô", 90, 3, "trop", "any", IconShape.TropicalFish, "#e86a4a", "#ffd0a0");
            Fish("f_anch", "Cá cơm", 8, 1, "sea,trop", "any", IconShape.Fish, "#a8b8c0", "#6f8a98");
            Fish("f_mack", "Cá thu", 22, 2, "sea", "any", IconShape.Fish, "#3f6f8f", "#a8c0d0");
            Fish("f_snap", "Cá hồng", 30, 2, "sea,trop", "day", IconShape.TropicalFish, "#e8706a", "#f6c0a0");
            Fish("f_crab", "Cua biển", 35, 2, "sea,trop", "any", IconShape.Crab, "#d8452f", "#8a2a1a");
            Fish("f_squid", "Mực ống", 42, 3, "sea,trop", "night", IconShape.Squid, "#f2b8b0", "#c87a70");
            Fish("f_octo", "Bạch tuộc", 65, 3, "sea", "any", IconShape.Octopus, "#c8504a", "#8a2a2a");
            Fish("f_grouper", "Cá mú", 58, 3, "sea,trop", "any", IconShape.Fish, "#8a6a4a", "#c8a070");
            Fish("f_lobster", "Tôm hùm", 140, 4, "sea,trop", "night", IconShape.Lobster, "#c8402a", "#8a2a1a");
            Fish("f_sword", "Cá kiếm", 190, 4, "sea", "day", IconShape.Fish, "#4a6a8a", "#c0d0e0");
            Fish("f_tuna", "Cá ngừ vây xanh", 300, 5, "sea", "any", IconShape.Fish, "#2a4a7a", "#e0e8f0");
            Fish("f_clown", "Cá hề", 25, 1, "trop", "day", IconShape.TropicalFish, "#f08a2a", "#ffffff");
            Fish("f_puffer", "Cá nóc", 45, 2, "trop", "any", IconShape.Puffer, "#e0c060", "#8a7a4a");
            Fish("f_parrot", "Cá mó xanh", 72, 3, "trop", "day", IconShape.TropicalFish, "#3fc0a0", "#e86aa0");
            Fish("f_ray", "Cá đuối", 125, 4, "trop", "night", IconShape.Fish, "#6a7a8a", "#c0c8d0");
            Fish("f_gold", "Cá vàng huyền thoại", 420, 5, "trop", "night", IconShape.Puffer, "#ffd34d", "#e8913a");
            Fish("f_catf", "Cá trê", 20, 1, "swamp,pond", "any", IconShape.Fish, "#5a5040", "#8a7a60");
            Fish("f_snake", "Cá lóc", 32, 2, "swamp", "any", IconShape.Fish, "#4a5a3a", "#8a9a6a");
            Fish("f_eel", "Lươn", 48, 3, "swamp", "night", IconShape.Eel, "#8a6a2a", "#c0a060");
            Fish("f_bigeel", "Cá chình đầm", 115, 4, "swamp", "night", IconShape.Eel, "#4a4a3a", "#8a8a6a");
            Fish("f_carp", "Cá chép đỏ", 30, 2, "jungle,pond", "any", IconShape.Fish, "#e8603a", "#ffd0a0");
            Fish("f_discus", "Cá dĩa", 58, 3, "jungle", "day", IconShape.TropicalFish, "#3fa0e0", "#ffd34d");
            Fish("f_fang", "Cá răng đao", 85, 3, "jungle", "night", IconShape.Fish, "#7a8a9a", "#d8452f");
            Fish("f_arowana", "Cá rồng", 270, 5, "jungle", "day", IconShape.Dragon, "#e0a21f", "#d8452f");
            Fish("f_rtrout", "Cá hồi vân", 45, 2, "mount", "any", IconShape.Fish, "#8a9a6a", "#e88aa0");
            Fish("f_salmon", "Cá hồi suối", 78, 3, "mount", "day", IconShape.Fish, "#c87a6a", "#f2b8a0");
            Fish("f_sturgeon", "Cá tầm", 175, 4, "mount", "any", IconShape.Fish, "#5a6a7a", "#a0b0c0");
            Fish("f_ice", "Cá băng huyền thoại", 330, 5, "mount", "night", IconShape.Fish, "#c0e8f8", "#6fc0e8");
            Fish("f_perch", "Cá rô bạc", 15, 1, "pond,swamp", "any", IconShape.Fish, "#b0b8a0", "#6f7a5a");
            Fish("f_crucian", "Cá diếc", 12, 1, "pond", "any", IconShape.Fish, "#c0b080", "#8a7a4a");
            Fish("f_barra", "Cá chẽm", 46, 3, "pond,sea", "any", IconShape.Fish, "#a0a8b0", "#5a6a7a");

            var r = _list; _list = null;
            return r;
        }
    }
}
