// Dinh nghia 1 giong cay (Docs/ThietKeGameplay.md muc 4.2). Moi giong la 1 asset ScriptableObject.
using UnityEngine;

namespace SereneIsle
{
    /// <summary>Dang cay, quyet dinh hinh ve 3D.</summary>
    public enum CropKind { Leafy, Root, Bulb, Bush, Vine, Ground, Grain, Corn, Flower, Aquatic, Tree }

    [CreateAssetMenu(menuName = "Serene Isle/Crop", fileName = "Crop")]
    public class CropDefinition : ScriptableObject
    {
        [Tooltip("Ma cay (vd: cai_be). Hat giong: seed_<id>")]
        public string id;
        public string displayName;
        [Tooltip("Mon thu hoach duoc (id item). Thuong trung voi id cay.")]
        public string produce;
        public CropKind kind;

        [Header("Mua vu")]
        [Tooltip("Mua nay mam / song duoc. Cay thuong sang mua khac thi chet; cay lau nam chi ra trai dung mua")]
        public SeasonMask seasons;
        [Tooltip("Cay lau nam (cay an trai, sa...): song qua cac mua")]
        public bool perennial;
        [Tooltip("So ngay tuoi nuoc de lon (cay an trai: de truong thanh)")]
        public int growDays;
        [Tooltip("Thu lai sau bao nhieu ngay (0 = thu 1 lan roi het)")]
        public int regrowDays;

        [Header("Dat, nuoc")]
        public SoilMask soils = SoilMask.All;
        [Tooltip("Can ruong nuoc (lua, sen...) - mo o giai doan 4")]
        public bool needsPaddy;
        [Tooltip("Can gian tre (dua leo, muop...)")]
        public bool trellis;

        [Header("Thu hoach")]
        public int yieldMin = 1;
        public int yieldMax = 1;

        [Header("Mau ve")]
        public Color leafColor = Color.green;
        public Color produceColor = Color.red;

        public string SeedId => "seed_" + id;
        public bool InSeason(Season s) => LunarCalendar.In(seasons, s);
    }
}
