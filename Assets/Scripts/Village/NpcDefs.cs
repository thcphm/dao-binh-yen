// Dan lang giai doan 4 (Docs/ThietKeGameplay.md muc 9): Ong Bay (mien Trung), Bac Chin Do va Co Sau Vuon (mien Tay),
// Thim Tam (mien Nam). Ngoai hinh = mo hinh KayKit + bo do casual doi mau; non la gan len dau.
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SereneIsle
{
    public sealed class NpcDef
    {
        public string id, name;
        public string building;          // id VillageBuilding o gan
        public string shop;              // Shops.TapHoa / HatGiong / null
        public CharacterOutfit outfit;
        public bool hat;                 // non la
        public float wander;             // ban kinh di dao quanh nha (0 = dung yen)
        public float fromHour = 6.5f, toHour = 20.5f;
        public string[] lines;
        public Func<GameSession, string> dynamicLine; // cau theo con nuoc / mua (co the null)
    }

    public static class NpcDefs
    {
        public static readonly NpcDef[] All =
        {
            new NpcDef
            {
                id = "bay", name = "Ông Bảy", building = "shack", wander = 0, fromHour = 6, toHour = 22,
                outfit = new CharacterOutfit { model = "Ranger", shirt = "#ece4d0", pants = "#4a4a52", shoe = "#5a3a22", skin = "#c98d63", hair = "#d8d4cc", longSleeve = true, noBelt = true },
                lines = new[]
                {
                    "Trận bão vừa rồi lớn lắm, cây ngã cả đồi. Từ từ mà dọn, đảo rồi cũng xanh lại.",
                    "Mi cần chi cứ hỏi ông. Ông ở cái nhà sàn bên cầu tàu ni.",
                    "Đêm mô trời trong, đứng trên cầu tàu ngó trăng là sướng nhứt.",
                    "Cứ từ từ thôi con, ở đảo ni chẳng ai hối ai.",
                    "Hải đăng ni ông giữ ba chục năm rồi. Đêm mô cũng phải thắp đèn cho ghe về.",
                },
                dynamicLine = s =>
                {
                    var w = WeatherSystem.Instance;
                    if (w && (w.DaysToStorm() is int ds && ds >= 0 && ds <= 2 || UnityEngine.Random.value < .35f)) return w.RadioLine();
                    if (Market.IsMarketDay(s.Clock.Day) && s.Clock.Hour < Market.CloseHour)
                        return Market.IsBig(s.Clock.Day) ? "Bữa ni phiên chợ lớn, khách đất liền ra đông lắm. Bày sạp ở nhà lồng chợ mà bán đi con."
                                                         : "Bữa ni họp chợ phiên ở nhà lồng chợ đó. Đem rau cá ra bán, được giá hơn bán cho thím Tám.";
                    if (Market.IsMarketDay(s.Clock.Day + 1)) return "Mai họp chợ phiên đó con, lo mà chuẩn bị đồ bán.";
                    double abs = TideClock.AbsHours(s.Clock.Day, s.Clock.Hour);
                    return TideClock.Rising(abs)
                        ? $"Nước đang lên, tới {TideClock.ClockLabel(TideClock.NextHigh(abs))} là lớn. Đừng ra cồn nghe con."
                        : $"Bữa ni nước ròng tới {TideClock.ClockLabel(TideClock.NextLow(abs))}, ra bãi mà lượm ốc, nhớ coi chừng nước lên nghe con.";
                },
            },
            new NpcDef
            {
                id = "chin", name = "Bác Chín Đò", building = "chinhut", shop = Shops.ConGiong, wander = 3.5f, fromHour = 5.5f, toHour = 20,
                outfit = new CharacterOutfit { model = "Ranger", shirt = "#4f86a6", pants = "#2f3640", shoe = "#5a3a22", skin = "#b07a50", hair = "#2a2422", shorts = true, noBelt = true },
                hat = true,
                lines = new[]
                {
                    "Kẹt trên cồn thì kêu bác nghen, bác chèo ghe ra rước.",
                    "Ghe thúng này bác đan hồi còn trai tráng đó, chắc lắm.",
                    "Nước lớn là cá vô gần bờ, quăng câu ngay đầu cầu là dính.",
                    "Mỗi tháng bác chạy ghe thư vô đất liền một chuyến. Ai gởi gì thì gởi nghen.",
                    "Muốn nuôi gà, nuôi trâu thì làm chuồng trước, bác chở giống từ đất liền ra cho.",
                    "Con trâu là đầu cơ nghiệp đó con. Có trâu thì có phân chuồng bón ruộng.",
                },
            },
            new NpcDef
            {
                id = "sau", name = "Cô Sáu Vườn", building = "sauvuon", shop = Shops.HatGiong, wander = 3f, fromHour = 6, toHour = 19.5f,
                outfit = new CharacterOutfit { model = "Rogue", shirt = "#7cb66b", pants = "#2a2422", shoe = "#8a5a3a", skin = "#eab489", hair = "#2a2422", noBelt = true },
                hat = true,
                lines = new[]
                {
                    "Đất gần đầm bị mặn rồi, trồng chỗ cao cao một chút nghen con.",
                    "Tưới nước mỗi ngày cây nó mới lớn. Quên một bữa là héo liền.",
                    "Đậu trồng xen với rau thì đất tốt lên đó.",
                    "Hạt giống cô đổi mỗi ngày, mùa nào thức nấy.",
                },
                dynamicLine = s =>
                {
                    var date = LunarCalendar.Date(s.Clock.Day);
                    var offers = Shops.Offers(Shops.HatGiong, s.Clock.Day, s.Items, CropDatabase.Instance);
                    foreach (var o in offers)
                    {
                        var c = CropDatabase.Instance ? CropDatabase.Instance.FromSeed(s.Items.Get(o.id)) : null;
                        if (c != null && c.kind != CropKind.Tree)
                            return $"Mùa {LunarCalendar.SeasonName(date.Season)} này trồng {c.displayName.ToLowerInvariant()} là hạp nhứt đó con!";
                    }
                    return null;
                },
            },
            new NpcDef
            {
                // O Man (mien Trung): day nghe lan bien, hay ngoi o ben cua Bac Chin
                id = "man", name = "O Mận", building = "chinhut", wander = 5f, fromHour = 6, toHour = 19,
                outfit = new CharacterOutfit { model = "Rogue", shirt = "#3f8aa8", pants = "#2a3a4a", shoe = "#2a2422", skin = "#c98d63", hair = "#1a1614", shorts = true, noBelt = true },
                lines = new[]
                {
                    "Lặn chi mà lâu rứa? Hụt hơi là chết đó nghe.",
                    "Đáy rạn ni hồi xưa san hô đẹp lắm, bão phá hết rồi. Cấy lại từ từ thì cá về.",
                    "Mùa đông nước lạnh, nín thở không được lâu mô.",
                    "Hải sâm, bào ngư nằm chỗ sâu. Muốn xuống đó thì phải học thêm với O.",
                    "Lượm nghêu, sò ở bãi rồi đem lại đây, O dạy cho cách lặn.",
                },
                dynamicLine = s =>
                {
                    int lv = Crafts.Level("lan");
                    if (lv == 0) return "Muốn lặn thì đem 3 nghêu với 3 sò huyết lại đây, O chỉ cho.";
                    if (s.Clock.IsNight && lv < 3) return "Tối rồi, không có đèn lặn thì đừng xuống nước nghe.";
                    return null;
                },
            },
            new NpcDef
            {
                id = "tam", name = "Thím Tám", building = "taphoa", shop = Shops.TapHoa, wander = 1.5f, fromHour = 6.5f, toHour = 20.5f,
                outfit = new CharacterOutfit { model = "Rogue", shirt = "#b05a8a", pants = "#2a2422", shoe = "#d9534f", skin = "#f8cfae", hair = "#2a2422", longSleeve = true, noBelt = true },
                lines = new[]
                {
                    "Có gì cứ đem lại thím mua, giá phải chăng thôi nghen.",
                    "Tiệm có nước, bánh nhân thịt, bánh mì nè con.",
                    "Đem ra chợ phiên bán thì được giá hơn, thím chỉ mua nửa giá thôi.",
                    "Ăn uống cho đàng hoàng, đói quá là xỉu đó nghen.",
                },
            },
        };

        public static NpcDef Get(string id) => Array.Find(All, n => n.id == id);

        /// <summary>Ong Bay ke chuyen lan dau gap (Docs muc 5: ke lai tran bao, chi cho dung leu, roi de nguoi choi tu do).</summary>
        public static readonly string[] BayIntro =
        {
            "Chào con! Ông là Bảy, giữ hải đăng ở cầu tàu ni đã ba chục năm.",
            "Trận bão vừa rồi quật tan nát cả đảo. Nhà cửa sập, ruộng vườn ngập mặn, cây ngã khắp đồi.",
            "Gia đình con nhờ con ra đây phụ dựng lại phải không? Lều của con ông dựng sẵn trong làng rồi đó.",
            "Cần mua đồ thì ghé tiệm Thím Tám, hạt giống thì có Cô Sáu Vườn. Bác Chín lái đò hay ngồi ngoài bến.",
            "Nhớ coi con nước, 11 giờ đêm phải về lều mà ngủ nghe. Thôi đi chơi đi con!",
        };

        /// <summary>Cau thoai ngau nhien (co the theo con nuoc / mua).</summary>
        public static string Line(NpcDef n, GameSession s, System.Random rng)
        {
            if (n.dynamicLine != null && rng.NextDouble() < .45)
            {
                var l = n.dynamicLine(s);
                if (!string.IsNullOrEmpty(l)) return l;
            }
            return n.lines[rng.Next(n.lines.Length)];
        }
    }
}
