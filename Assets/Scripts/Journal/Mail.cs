// Thu ve dat lien (Docs/ThietKeGameplay.md muc 7.7): moi thang ghe thu cua Bac Chin cap cau tau (mung 2).
// Noi chuyen voi Bac Chin -> nhan thu nha (ke dan vi sao gia dinh nho nguoi choi ra dao) + qua (hat giong, cay giong, xu).
// Nguoi choi viet thu hoi am: chon 1 chuyen ke trong thang, gui kem 1 mon qua. Thang sau thu nha nhac lai chuyen do + cam on qua.
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SereneIsle
{
    public class Mail : MonoBehaviour
    {
        public static Mail Instance { get; private set; }

        public const int MailDay = 2; // mung 2 hang thang
        public const string Npc = "chin";

        public sealed class Letter
        {
            public string from, text;
            public (string id, int n)[] gifts;
            public int coins;
        }

        static Letter M(string from, string text, int coins, params (string, int)[] gifts) => new Letter { from = from, text = text, coins = coins, gifts = gifts };

        /// <summary>8 la thu nha (1 nam), ke dan chuyen ong noi va hon dao.</summary>
        public static readonly Letter[] Story =
        {
            M("Mẹ", "Con tới đảo bình an chưa? Ông nội cứ hỏi hoài: \"Thằng Bảy còn giữ hải đăng không?\". Mẹ gửi con ít hạt rau dễ trồng, gieo quanh lều mà ăn dần. Đừng làm quá sức nghen con.",
              0, ("seed_cai_be", 5), ("seed_hanh_la", 5)),
            M("Ông nội", "Nội viết thư này tay run run, con đọc ráng nghen. Hồi nhỏ nội lớn lên ở xóm chài đó, nhà sát cầu tàu. Nội với ông Bảy chèo ghe thúng ra rạn bắt cá suốt. Gửi con hạt cà rốt, hồi đó nhà mình trồng ngoài đất cát mà ngọt lắm.",
              0, ("seed_ca_rot", 5), ("seed_dau_phong", 4)),
            M("Ba", "Ba gửi con chút tiền mua đồ nghề. Nói thiệt, hồi con còn làm trên phố, tối nào ba mẹ cũng thấy đèn phòng con sáng tới khuya. Nội nói: \"Cho nó ra đảo đi, đảo chữa được bệnh đó\". Ba mẹ tin nội.",
              200),
            M("Ông nội", "Bốn chục năm trước có một trận bão lớn. Nước biển tràn vô vườn, cây xoài của ông cố trốc gốc, nhà sập một nửa. Cả nhà dọn vô đất liền từ đó. Nội gửi con cây xoài giống, trồng lại giùm nội một cây nghen.",
              0, ("seed_xoai", 1), ("canh_xoai", 1)),
            M("Mẹ", "Ông Bảy có gửi lời theo ghe Bác Chín, khen con siêng. Ông nội nghe xong cười cả buổi. Mẹ gửi con hạt sen, bà nội con hồi xưa có cái ao sen nhỏ sau nhà, mùa nào cũng thơm.",
              0, ("seed_sen", 2), ("seed_rau_thom", 3)),
            M("Ông nội", "Trận bão năm rồi, nội lo cho ông Bảy với cái đảo mà không đi được, chân yếu rồi. Nên nội mới nhờ con ra. Không phải để làm giàu, mà để coi đảo có xanh lại được không. Nội gửi cây quýt với cành quýt đầu dòng nhà mình, ghép vô cho trái ngọt, Tết có trái chưng.",
              0, ("seed_quyt", 1), ("canh_quyt", 1)),
            M("Ba", "Cả nhà đọc thư con mấy lần. Con viết vui hơn hồi trước nhiều. Ba gửi nếp với lá thơm, cuối năm con thử gói bánh chưng coi, nồi bánh đầu tiên trên đảo phải do con nấu.",
              150, ("seed_nep", 5), ("rau_thom", 2)),
            M("Ông nội", "Nội hỏi Bác Chín rồi, Tết này Bác chèo ghe đưa nội ra đảo một chuyến. Nội muốn ngồi ở cầu tàu với ông Bảy, coi con trồng cây, nghe sóng như hồi nhỏ. Con giữ đảo giùm nội nghen. Thương con.",
              500, ("oldcoin", 1)),
        };

        /// <summary>Sau khi het chuyen: thu thuong (xoay vong).</summary>
        public static readonly Letter[] Everyday =
        {
            M("Mẹ", "Nhà mình vẫn khỏe. Ông nội giờ sáng nào cũng ra hiên ngó về phía biển. Gửi con ít hạt giống mùa mới.", 0, ("seed_tomato", 4), ("seed_ot", 4)),
            M("Ba", "Ba gửi con chút tiền xài dần. Nhớ giữ sức, đừng ở ngoài trễ quá mà bị ngất nghen con.", 200),
            M("Ông nội", "Nội nhớ mùi gió biển. Con kể nội nghe thêm về con nước với mấy con cá con câu được nghen.", 0, ("seed_toi", 4)),
            M("Ông nội", "Nhà hàng xóm mới chiết được cành bưởi ngon, nội xin một nhánh gửi con. Ghép lên cây bưởi con trồng, hai mùa sau có trái ngọt như hồi xưa.", 0, ("canh_buoi", 1)),
            M("Mẹ", "Mẹ làm ít mứt gừng gửi theo ghe. Đêm lạnh ngồi bên lửa trại mà nhâm nhi.", 0, ("seed_gung", 3), ("berry", 4)),
        };

        static readonly Dictionary<string, string> Ack = new Dictionary<string, string>
        {
            ["fish"] = "Nghe con câu được cá, nội mừng lắm. Hồi nhỏ nội cũng thả câu ngay khúc cầu tàu đó.",
            ["farm"] = "Vườn ruộng con ra trái ra hạt, ba mẹ coi thư mà nghe thơm mùi đất.",
            ["island"] = "Con dọn đảo, trồng cây, nội nói vậy là đảo đang thở lại đó con.",
            ["people"] = "Con quen hết xóm rồi hả? Ông Bảy khó tính vậy chứ thương người lắm.",
            ["calm"] = "Con biết ngồi yên ngắm hoàng hôn rồi. Mẹ đọc tới đó mà rưng rưng.",
            ["sea"] = "Nội đọc tới khúc con ngắm sóng ở cầu tàu, nội nói nhớ cái mùi gió đó quá.",
            ["market"] = "Bày sạp chợ phiên mà bán được tiền, giỏi quá con. Nhớ đừng nói thách người ta nghen.",
        };

        public int Chapter { get; private set; }       // so thu nha da nhan
        public int LastMonth { get; private set; } = -1; // thang (tuyet doi) da nhan thu
        public int SentMonth { get; private set; } = -1; // thang da gui thu hoi am
        public string Topic { get; private set; }       // chuyen da ke trong thu gui
        public string Gift { get; private set; }        // qua da gui kem

        GameSession S => GameSession.Instance;
        bool _hooked;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { Instance = null; }

        void Awake() { Instance = this; }
        void OnEnable() { Village.Talking += OnTalk; }
        void OnDisable() { Village.Talking -= OnTalk; }
        void OnDestroy() { if (Instance == this) Instance = null; if (_hooked && S) S.DayStarted -= OnDayStarted; }

        public static int MonthOf(int day) => (day - 1) / LunarCalendar.DaysPerMonth;
        int Month => MonthOf(S.Clock.Day);

        /// <summary>Co thu nha cho nhan (tu mung 2 thang nay, chua nhan).</summary>
        public bool Pending => S && Month > LastMonth && S.Date.DayOfMonth >= MailDay;
        public bool CanSend => S && LastMonth == Month && SentMonth < Month;

        public static Letter LetterAt(int i) => i < Story.Length ? Story[i] : Everyday[(i - Story.Length) % Everyday.Length];

        void Update()
        {
            var s = S;
            if (!s || !s.Running || _hooked) return;
            s.DayStarted += OnDayStarted;
            _hooked = true;
        }

        void OnDayStarted(int day)
        {
            if (Pending) S.Say("Ghe thư của Bác Chín cập cầu tàu. Ghé Bác Chín nhận thư nhà nghen.");
        }

        void OnTalk(Village.TalkContext ctx)
        {
            if (ctx.npc.def.id != Npc) return;
            if (Pending)
            {
                ctx.line = "Có thư nhà nè con! Ghe thư mới cập bến, thư con nằm trên cùng đó.";
                ctx.buttons.Add(("Nhận thư nhà", Receive));
            }
            else if (CanSend) ctx.buttons.Add(("Gửi thư về nhà", Write));
        }

        // ------------------------------------------------------------------ nhan thu
        public void Receive()
        {
            var s = S;
            if (!Pending) return;
            var l = LetterAt(Chapter);
            var parts = new List<string>();
            if (Topic != null && Ack.TryGetValue(Topic, out var ack)) parts.Add(ack);
            var gd = Gift != null ? s.Items.Get(Gift) : null;
            if (gd) parts.Add($"Món {gd.displayName.ToLowerInvariant()} con gửi, cả nhà chia nhau, ai cũng khen.");
            parts.Add(l.text);
            var got = new List<string>();
            foreach (var (id, n) in l.gifts ?? Array.Empty<(string, int)>())
            {
                var d = s.Items.Get(id);
                if (!d) continue;
                s.Give(id, n, true);
                got.Add($"{n} {d.displayName.ToLowerInvariant()}");
            }
            int coins = l.coins + (gd ? 50 : 0);
            if (coins > 0) { s.Coins += coins; got.Add($"{coins} xu"); }
            if (got.Count > 0) parts.Add("Gửi kèm: " + string.Join(", ", got) + ".");
            Chapter++;
            LastMonth = Month;
            Topic = null; Gift = null;
            s.Stats.Calm = Mathf.Min(100, s.Stats.Calm + 3);
            Journal.Stat("mail");
            var ui = ActionUI.Instance;
            if (!ui) return;
            var buttons = new List<(string, Action)> { ("Cất thư", null) };
            if (CanSend) buttons.Insert(0, ("Viết thư hồi âm", Write));
            ui.ShowDialog($"Thư nhà · {l.from}", string.Join("\n\n", parts), buttons.ToArray());
        }

        // ------------------------------------------------------------------ gui thu
        /// <summary>Cac chuyen co the ke (toi da 3), theo nhung gi nguoi choi da lam.</summary>
        public List<(string id, string text)> Topics()
        {
            var s = S; var j = Journal.Instance;
            float St(string id) => j ? j.StatOf(id) : 0;
            var all = new List<(string, string)>();
            if (St("fish") > 0) all.Add(("fish", $"Con câu được {St("fish"):0} con cá rồi."));
            if (St("crop") > 0) all.Add(("farm", $"Vườn ruộng của con thu được {St("crop"):0} nông sản."));
            if (St("earn") > 0) all.Add(("market", $"Con bày sạp chợ phiên, bán được {St("earn"):0} xu."));
            if (s.Stats.Calm >= 25) all.Add(("calm", "Con tập ngồi yên ngắm hoàng hôn, thấy lòng nhẹ hẳn."));
            all.Add(("island", "Con dọn dẹp, trồng cây cho đảo xanh lại từng chút."));
            all.Add(("people", "Con quen hết xóm rồi: ông Bảy, bác Chín, cô Sáu, thím Tám."));
            all.Add(("sea", "Biển ở đây xanh lắm, chiều nào con cũng ra cầu tàu ngắm sóng."));
            return all.Take(3).ToList();
        }

        /// <summary>Qua co the gui: 3 mon dat nhat trong tui (khong phai dung cu, hat giong).</summary>
        public List<ItemDefinition> GiftOptions()
        {
            var s = S;
            return s.Inventory.Stacks().Select(kv => s.Items.Get(kv.Key))
                    .Where(d => d && !d.IsTool && d.category != ItemCategory.Seed && d.category != ItemCategory.Bag)
                    .OrderByDescending(d => d.price).Take(3).ToList();
        }

        public void Write()
        {
            var ui = ActionUI.Instance;
            if (!CanSend || !ui) return;
            var buttons = Topics().Select(t => (t.text, (Action)(() => ChooseGift(t.id)))).ToList();
            ui.ShowDialog("Viết thư về nhà", "Tháng này kể chuyện gì cho nhà nghe?", buttons.ToArray());
        }

        void ChooseGift(string topic)
        {
            var ui = ActionUI.Instance;
            var buttons = GiftOptions().Select(d => ($"Gửi {d.displayName}", (Action)(() => Send(topic, d.id)))).ToList();
            buttons.Add(("Không gửi quà", () => Send(topic, null)));
            ui.ShowDialog("Viết thư về nhà", "Gửi kèm món quà nhỏ theo ghe Bác Chín không?", buttons.ToArray());
        }

        public void Send(string topic, string gift)
        {
            var s = S;
            if (!CanSend) return;
            if (gift != null && s.Inventory.Count(gift) > 0) s.Inventory.Take(gift, 1);
            else gift = null;
            Topic = topic; Gift = gift;
            SentMonth = Month;
            s.Stats.Calm = Mathf.Min(100, s.Stats.Calm + 3);
            s.Say("Bác Chín cất thư vào túi áo: \"Tháng sau có thư hồi âm nghen con.\" (+Bình yên)");
        }

        // ------------------------------------------------------------------ luu / tai
        public void Restore(int chapter, int lastMonth, int sentMonth, string topic, string gift)
        {
            Chapter = Mathf.Max(0, chapter); LastMonth = lastMonth; SentMonth = sentMonth;
            Topic = string.IsNullOrEmpty(topic) ? null : topic;
            Gift = string.IsNullOrEmpty(gift) ? null : gift;
        }
    }
}
