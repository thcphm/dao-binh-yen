// Dinh nghia 1 mon do (ban web: ITEMS / FISH / BUGS / TOOLS). Moi mon la 1 asset ScriptableObject.
using UnityEngine;

namespace SereneIsle
{
    public enum ItemCategory { Material, Food, Fish, Bug, Crop, Seed, Tool, Bag, Placeable, Animal, Misc }

    /// <summary>Hinh ve vector cua bieu tuong (ve bang Painter2D, chay duoc tren WebGL - thay cho emoji ban web).</summary>
    public enum IconShape
    {
        None, Log, Rock, Ore, Fiber, Coconut, Bottle, Berry,
        Fish, TropicalFish, Puffer, Crab, Squid, Octopus, Lobster, Eel, Dragon,
        Butterfly, Beetle, Cricket, Firefly,
        Tomato, Wheat, Pumpkin, Melon, SeedBag,
        Skewer, Pie, Bowl, Bread, Egg, Omelet, Milk, Cup,
        Worm, Shell, Coin, Backpack, Chick, Goat, Cow,
        Campfire, Torch, Fence, PathTile, Sprinkler, Scarecrow,
        ToolShovel, ToolAxe, ToolPick, ToolHoe, ToolCan, ToolSeed, ToolRod, ToolNet, ToolBuild,
        // them sau (giu thu tu cu vi asset luu so thu tu)
        Dragonfly, Bee, Mantis, Cicada,
        // giong cay 4 mua
        LeafGreens, Root, RootRound, Bulb, Corn, Flower, Gourd, Chili, Fruit, Banana, Lotus,
    }

    [CreateAssetMenu(menuName = "Serene Isle/Item", fileName = "Item")]
    public class ItemDefinition : ScriptableObject
    {
        [Tooltip("Ma noi bo, giong ban web (vd: wood, coconut, f_tuna)")]
        public string id;
        public string displayName;
        public ItemCategory category;
        [Tooltip("Gia ban (xu)")] public int price;
        [Tooltip("Khoi luong 1 cai (kg)")] public float weight;

        [Header("An / uong")]
        public int eatHp;
        public int eatWater;
        public int eatEnergy;

        [Header("Bieu tuong")]
        public IconShape icon;
        public Color iconColor = Color.white;
        public Color iconColor2 = Color.white;
        [Tooltip("Tuy chon: neu co sprite thi dung thay cho hinh ve")]
        public Sprite sprite;

        [Header("Khac")]
        [Tooltip("Hat giong: id cay se moc")] public string seedOf;
        [Tooltip("Ba lo: cap tui sau khi che tao (1, 2)")] public int bagLevel;
        [Tooltip("Ca: do kho 1-5")] public int fishDifficulty;
        [Tooltip("Ca/bo: noi song (sea, trop, swamp, pond, jungle, mount / ma biome)")] public string[] habitats;
        [Tooltip("Ca/bo: day, night, any")] public string activeTime;
        [TextArea] public string description;

        public bool IsFood => eatHp > 0 || eatWater > 0 || eatEnergy > 0;
        /// <summary>Ban web: co nuoc ma khong co nang luong -> "uong".</summary>
        public bool IsDrink => eatWater > 0 && eatEnergy == 0;
        public bool IsTool => category == ItemCategory.Tool;
        /// <summary>Do cho vao tui (dung cu, ba lo, giong vat nuoi thi khong).</summary>
        public bool GoesInBag => category != ItemCategory.Tool && category != ItemCategory.Bag && category != ItemCategory.Animal;
    }
}
