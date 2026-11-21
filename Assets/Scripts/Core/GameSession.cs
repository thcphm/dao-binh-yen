// Trang thai 1 van choi: dong ho, chi so, tui, hotbar, xu, dung cu da co (ban web: G + frame()).
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SereneIsle
{
    [DefaultExecutionOrder(-50)]
    public class GameSession : MonoBehaviour
    {
        public static GameSession Instance { get; private set; }

        [Tooltip("Cho vai mon de thu (giai doan 2 chua co cach kiem do).")]
        public bool starterKit = true;

        public PlayerStats Stats { get; } = new PlayerStats();
        public Hotbar Hotbar { get; } = new Hotbar();
        public DayClock Clock { get; } = new DayClock();
        public Inventory Inventory { get; private set; }
        public ItemDatabase Items => ItemDatabase.Instance;
        public int Coins { get; set; } = 30;
        public readonly HashSet<string> OwnedTools = new HashSet<string>();
        /// <summary>Ca / bo da tung bat (so suu tam - ban web: G.caught).</summary>
        public readonly HashSet<string> Caught = new HashSet<string>();
        /// <summary>Co danh dau tien trinh (da gap ong Bay...) - ban web: G.flags.</summary>
        public readonly HashSet<string> Flags = new HashSet<string>();
        /// <summary>O luu tu dong (moi sang ngay moi). &lt; 0 = khong luu (test).</summary>
        public int SaveSlot { get; set; } = -1;

        public PlayerController Player { get; set; }
        public IslandCharacter Body { get; set; }
        public IslandEnvironment Environment { get; set; }

        public Vector3 HomePosition { get; private set; }
        public float HomeFacing { get; private set; }
        public bool HasHome { get; private set; }
        const float HomeRadius = 4.5f;

        /// <summary>Dang mo bang (tui do...) - ban web: uiOpen. Dung thoi gian va nhan vat.</summary>
        public bool UiOpen { get; set; }
        public bool Transitioning { get; private set; }
        public bool Running => Player && IslandTerrain.Instance && IslandTerrain.Instance.IsReady;
        public bool Active => Running && !UiOpen && !Transitioning;

        public event Action<string> Toast;
        public event Action<int, string> FadeShown;
        public event Action FadeHidden;
        /// <summary>Sau khi sang ngay moi (truoc khi man hinh sang lai). Tham so: so ngay.</summary>
        public event Action<int> DayStarted;

        /// <summary>Vuon ruong, con nuoc (giai doan 3, Docs/ThietKeGameplay.md).</summary>
        public FarmSystem Farm { get; private set; }
        public TideSystem Tide { get; private set; }
        /// <summary>Suc song vung (Docs/ThietKeGameplay.md muc 5).</summary>
        public RestorationSystem Restoration { get; private set; }
        public LunarDate Date => LunarCalendar.Date(Clock.Day);

        bool _warn22, _warn225, _kitGiven;
        float _fullToastTime = -10;

        void Awake()
        {
            Instance = this;
            Inventory = new Inventory(Items);
            Stats.Warning += m => Toast?.Invoke(m);
            Farm = gameObject.AddComponent<FarmSystem>();
            Tide = gameObject.AddComponent<TideSystem>();
            Restoration = gameObject.AddComponent<RestorationSystem>();
        }

        void OnDestroy() { if (Instance == this) Instance = null; }

        /// <summary>Thoat game giua chung: luu lai (ban web luu thuong xuyen vao localStorage).</summary>
        void OnApplicationQuit() { if (Running && !Transitioning) SaveNow(); }

        public void SetHome(Vector3 pos, float facing) { HomePosition = pos; HomeFacing = facing; HasHome = true; }

        public void Say(string msg) => Toast?.Invoke(msg);

        void Update()
        {
            var tools = ToolSystem.Instance;
            if (Player) Player.InputLocked = !Active || (tools && tools.Fishing && tools.Fishing.Busy);
            if (!Running) return;
            if (starterKit && !_kitGiven) GiveStarterKit();

            if (Active)
            {
                float dt = Mathf.Min(.05f, Time.deltaTime);
                float gameMinutes = dt * DayClock.MinutesPerSecond;
                Clock.Advance(gameMinutes);
                Stats.Tick(dt, gameMinutes / 60f, Player.Sprinting && Player.Moving);
                Player.SpeedMultiplier = Stats.Energy <= 0 ? .6f : 1f;

                if (Clock.Minutes >= 22 * 60 && !_warn22) { _warn22 = true; Say("22 giờ rồi. 23 giờ phải về tới lều, không thì ngất và mất hết đồ trong túi!"); }
                if (Clock.Minutes >= 22.5f * 60 && !_warn225) { _warn225 = true; Say("Còn 30 phút nữa là 23 giờ. Về lều ngay!"); }

                if (Clock.PastDayEnd)
                {
                    bool home = HasHome && Vector2.Distance(new Vector2(Player.transform.position.x, Player.transform.position.z),
                                                            new Vector2(HomePosition.x, HomePosition.z)) < HomeRadius;
                    StartCoroutine(NewDay(home ? NewDayReason.Sleep : NewDayReason.Late));
                }
                else if (Stats.Dead) StartCoroutine(NewDay(NewDayReason.Fainted));
                else HandleInput();
            }
            if (Environment) Environment.hour = Clock.Hour;
        }

        // ---------------------------------------------------------------- dieu khien
        void HandleInput()
        {
            int s = GameInput.SlotPressed;
            if (s >= 0) SelectSlot(s);
            int cyc = GameInput.SlotCycle;
            if (cyc != 0) CycleSlot(cyc);
            float wheel = GameInput.ScrollSteps;
            if (wheel != 0 && !GameInput.ZoomModifier && !GameHud.PointerOverUI) CycleSlot(wheel > 0 ? -1 : 1);

            bool use = GameInput.UsePressed;
            if (use && GameInput.UsePressedByMouse && GameHud.PointerOverUI) use = false;
            if (use) UseSelected();
        }

        bool SlotUsable(string id)
        {
            if (string.IsNullOrEmpty(id)) return false;
            var d = Items.Get(id);
            return d && (d.IsTool ? OwnedTools.Contains(id) : true);
        }

        /// <summary>Lan chuot: chi dung lai o cac o da co (ban web: hbOwn).</summary>
        public void CycleSlot(int dir)
        {
            int t = Hotbar.Selected;
            for (int q = 0; q < Hotbar.Size; q++)
            {
                t = (t + dir + Hotbar.Size) % Hotbar.Size;
                if (SlotUsable(Hotbar[t])) { Hotbar.Selected = t; return; }
            }
        }

        /// <summary>selectTool: chon o; bam lai o do an dang chon = an.</summary>
        public void SelectSlot(int s)
        {
            string id = Hotbar[s];
            if (string.IsNullOrEmpty(id)) { Say("Ô trống. Mở Túi đồ (I) để gán đồ ăn, nước uống vào."); return; }
            var d = Items.Get(id);
            if (!d) return;
            if (!SlotUsable(id)) { Say($"Chưa có {d.displayName}."); return; }
            if (!d.IsTool && Hotbar.Selected == s) { UseSelected(); return; }
            Hotbar.Selected = s;
        }

        public void UseSelected()
        {
            string id = Hotbar.SelectedId;
            if (string.IsNullOrEmpty(id)) return;
            var d = Items.Get(id);
            if (!d) return;
            if (d.IsTool)
            {
                if (!OwnedTools.Contains(id)) Say($"Chưa có {d.displayName}. Làm ở bàn chế tạo trong làng.");
                else if (Farm && Farm.TryUse(d)) { }                         // vuon ruong: cuoc, tuoi, gieo, thu hoach
                else if (ToolSystem.Instance) ToolSystem.Instance.Use(id);   // riu, cuoc chim, xeng, can cau, vot
                return;
            }
            if ((d.category == ItemCategory.Seed || FarmSystem.IsFertilizer(d)) && Farm && Farm.TryUse(d)) return;   // gieo hat, bon phan
            if (d.IsFood) Consume(id);
        }

        // ---------------------------------------------------------------- do dac
        /// <summary>give(): them vao tui; dung cu -> so huu; ba lo -> nang cap tui; du thi roi xuong dat.</summary>
        public int Give(string id, int n = 1, bool silent = false)
        {
            var d = Items.Get(id);
            if (!d || n <= 0) return 0;
            if (d.IsTool) { OwnedTools.Add(id); if (!silent) Say($"Có {d.displayName}!"); Hotbar.Load(Hotbar.ToArray()); return n; }
            if (d.category == ItemCategory.Bag) { Inventory.BagLevel = Math.Max(Inventory.BagLevel, d.bagLevel); if (!silent) Say($"Có {d.displayName}: túi {Inventory.Capacity} ô, {Inventory.MaxWeight:0} kg."); return n; }
            int add = Inventory.Give(id, n);
            if (add > 0 && !silent) Say($"+{add} {d.displayName}");
            if (n > add)
            {
                Vector3 at = Player ? Player.transform.position : Vector3.zero;
                ItemPile.Spawn(id, n - add, at);
                if (Time.realtimeSinceStartup - _fullToastTime > 2.5f)
                {
                    _fullToastTime = Time.realtimeSinceStartup;
                    Say($"Túi đầy ({Inventory.Capacity} ô, {Inventory.MaxWeight:0} kg)! {n - add} {d.displayName} để lại dưới đất. Làm ba lô để mang nhiều hơn.");
                }
            }
            return add;
        }

        public static string EatText(ItemDefinition d)
        {
            var parts = new List<string>();
            if (d.eatHp > 0) parts.Add($"+{d.eatHp} máu");
            if (d.eatWater > 0) parts.Add($"+{d.eatWater} nước");
            if (d.eatEnergy > 0) parts.Add($"+{d.eatEnergy} năng lượng");
            return string.Join(", ", parts);
        }

        /// <summary>consume(): an / uong, co hoat anh.</summary>
        public bool Consume(string id)
        {
            var d = Items.Get(id);
            if (!d || !d.IsFood || !Running || Transitioning) return false;
            if (Body && Body.Acting) return false;
            if (!Inventory.Has(id)) { Say($"Hết {d.displayName}."); return false; }
            Inventory.Take(id);
            Stats.Consume(d);
            if (Body) Body.PlayAct(d.IsDrink ? "drink" : "eat", d);
            Say($"{(d.IsDrink ? "Uống" : "Ăn")} {d.displayName}: {EatText(d)}");
            return true;
        }

        /// <summary>Da tai van choi: khong phat do khoi dau nua.</summary>
        public void MarkKitGiven() => _kitGiven = true;

        void GiveStarterKit()
        {
            _kitGiven = true;
            // tui ban dau chi 8 o: de trong 2 o cho do nhat duoc (chat cay, dao dat) -> bo dua, banh mi
            Give("bottle", 2, true); Give("berry", 6, true);
            Give("wood", 6, true); Give("stone", 4, true);
            // ban web: ong Bay trao riu va xeng trong phan huong dan (giai doan 4) - tam thoi co san
            Give("axe", 1, true); Give("shovel", 1, true);
            Hotbar.Assign(9, "bottle");
            Hotbar.Selected = 9;
            // vuon ruong (thu nghiem giai doan 3): cuoc, binh tuoi, tui hat + hat giong mua Xuan
            Give("hoe", 1, true); Give("can", 1, true); Give("seed", 1, true);
            Give("seed_cai_be", 6, true); Give("seed_dua_leo", 3, true);
            if (Farm) Farm.CanWater = FarmSystem.CanCapacity;
            Say("Đồ khởi đầu: rìu, xẻng, cuốc, bình tưới, hạt giống, nước, quả mọng. Bấm 1 cầm rìu, F để chặt cây.");
        }

        /// <summary>Di ngu o leu (sau khi xac nhan): sang ngay moi, tu luu.</summary>
        public void Sleep()
        {
            if (!Transitioning) StartCoroutine(NewDay(NewDayReason.Sleep));
        }

        public bool SaveNow()
        {
            if (SaveSlot < 0) return false;
            return SaveSystem.Save(this, SaveSlot);
        }

        /// <summary>Toi man hinh, chay apply (tra ve cac dong thong bao), roi sang lai. Dung cho cuu nan, chuyen canh.</summary>
        public IEnumerator Cutscene(string subtitle, Func<List<string>> apply)
        {
            if (Transitioning) yield break;
            Transitioning = true;
            FadeShown?.Invoke(Clock.Day, subtitle);
            yield return new WaitForSecondsRealtime(1.3f);
            var msgs = apply?.Invoke() ?? new List<string>();
            yield return new WaitForSecondsRealtime(1.2f);
            FadeHidden?.Invoke();
            Transitioning = false;
            yield return new WaitForSecondsRealtime(.6f);
            foreach (var m in msgs) Say(m);
        }

        // ---------------------------------------------------------------- qua ngay
        public IEnumerator NewDay(NewDayReason reason)
        {
            if (Transitioning) yield break;
            Transitioning = true;
            FadeShown?.Invoke(Clock.Day + 1, DayRules.Subtitle(reason));
            yield return new WaitForSecondsRealtime(1.3f);

            int coins = Coins;
            var msgs = DayRules.Apply(reason, Stats, Inventory, ref coins);
            Coins = coins;
            Clock.StartNewDay();
            _warn22 = _warn225 = false;
            DayStarted?.Invoke(Clock.Day);
            var date = Date;
            string fest = LunarCalendar.Festival(date);
            if (fest != null) msgs.Add($"Hôm nay là {fest}!");
            if (date.IsMarketDay) msgs.Add(LunarCalendar.IsBigMarket(date) ? "Hôm nay có phiên chợ lớn." : "Hôm nay có phiên chợ.");
            if (HasHome && Player) Player.PlaceAt(HomePosition, HomeFacing);
            if (Environment) Environment.hour = Clock.Hour;
            if (ToolSystem.Instance) ToolSystem.Instance.Regrow(Clock.Day);
            if (SaveNow()) msgs.Add("Đã lưu game."); // sau DayStarted: luu ca vuon ruong da qua ngay

            yield return new WaitForSecondsRealtime(1.6f);
            FadeHidden?.Invoke();
            Transitioning = false;
            yield return new WaitForSecondsRealtime(.6f);
            foreach (var m in msgs) Say(m);
        }
    }
}
