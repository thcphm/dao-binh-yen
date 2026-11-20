// Vuon ruong (giai doan 3): cuoc dat, gieo hat, tuoi nuoc, cay lon qua ngay, thu hoach. Port + mo rong ban web (useTool hoe/can/seed, harvest).
// Dung cu tac dong o phia truoc nhan vat (ban web: targetIdx = vi tri + huong mat * .85).
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SereneIsle
{
    public class FarmSystem : MonoBehaviour
    {
        public const int CanCapacity = 20;
        const float Reach = .85f;

        public int CanWater { get; set; }
        public IReadOnlyDictionary<int, FarmPlot> Plots => _plots;

        readonly Dictionary<int, FarmPlot> _plots = new Dictionary<int, FarmPlot>();
        readonly Dictionary<int, PlotView> _views = new Dictionary<int, PlotView>();
        readonly System.Random _rng = new System.Random();
        Transform _root;

        sealed class PlotView
        {
            public GameObject go;
            public MeshRenderer soil;
            public MeshFilter cropFilter;
            public MeshRenderer cropRenderer;
        }

        GameSession S => GameSession.Instance;
        static IslandData Data => IslandTerrain.Instance ? IslandTerrain.Instance.Data : null;
        static CropDatabase Crops => CropDatabase.Instance;
        Season CurrentSeason => LunarCalendar.Date(S ? S.Clock.Day : 1).Season;

        void Start()
        {
            if (S) S.DayStarted += OnDayStarted;
        }

        void OnDestroy()
        {
            if (S) S.DayStarted -= OnDayStarted;
        }

        // ---------------------------------------------------------------- muc tieu
        /// <summary>O phia truoc nhan vat (-1 neu ngoai dao).</summary>
        public int TargetTile()
        {
            var d = Data;
            if (d == null || !S || !S.Player) return -1;
            var p = S.Player.transform.position;
            float f = S.Player.Facing;
            int i = Mathf.FloorToInt(p.x + Mathf.Sin(f) * Reach + .5f), j = Mathf.FloorToInt(p.z + Mathf.Cos(f) * Reach + .5f);
            return d.InBounds(i, j) ? d.Idx(i, j) : -1;
        }

        public FarmPlot PlotAt(int k) => _plots.TryGetValue(k, out var p) ? p : null;

        /// <summary>Goi y tren HUD cho o phia truoc (ban web: hintFor).</summary>
        public string HintFor(ItemDefinition held)
        {
            int k = TargetTile();
            if (k < 0) return null;
            var p = PlotAt(k);
            var c = p != null ? Crops.Get(p.crop) : null;
            if (c != null)
            {
                if (p.dead) return $"{c.displayName} đã chết ({p.deadReason}). Cuốc lại để dọn.";
                if (FarmRules.Harvestable(p, c)) return $"Thu hoạch {c.displayName}";
                string w = c.kind == CropKind.Tree && FarmRules.Mature(p, c)
                    ? (c.InSeason(CurrentSeason) ? "đang ra trái" : $"ra trái mùa {CropCatalogData.SeasonText(c.seasons)}")
                    : p.watered ? "đã tưới" : p.dryDays > 0 ? "đang héo, cần tưới ngay" : "cần tưới";
                return $"{c.displayName}: {Mathf.FloorToInt(p.growth)}/{c.growDays} ngày · {w}";
            }
            if (!held) return null;
            if (held.id == "hoe" && p == null) return CanTill(k, out _) ? $"Cuốc đất ({Soils.Name(SoilAt(k))})" : null;
            if (held.id == "can") return IsWater(k) ? "Đổ đầy bình tưới" : p != null && !p.watered ? $"Tưới nước ({CanWater} lượt)" : null;
            if (p != null && p.Empty && (held.category == ItemCategory.Seed || held.id == "seed")) return "Gieo hạt";
            return null;
        }

        // ---------------------------------------------------------------- dung cu
        /// <summary>Dung mon dang cam len o phia truoc. Tra ve true neu da xu ly (ke ca bao loi).</summary>
        public bool TryUse(ItemDefinition held)
        {
            var d = Data;
            if (d == null || !held || !S) return false;
            int k = TargetTile();
            if (k < 0) return false;
            var p = PlotAt(k);
            var body = S.Body;
            if (body && body.Acting) return true;

            // thu hoach: tay cam gi cung duoc (tru cuoc chim, riu)
            if (p != null && !p.dead && held.id != "pick" && held.id != "axe")
            {
                var c = Crops.Get(p.crop);
                if (c != null && FarmRules.Harvestable(p, c)) { if (body) body.PlayAct("seed"); Harvest(k, p, c); return true; }
            }

            switch (held.id)
            {
                case "hoe": Till(k, p); return true;
                case "can": Water(k, p); return true;
                case "seed": PlantFromBag(k, p); return true;
                case "shovel":
                case "pick":
                    if (p == null) return false;            // de dung cu khac (dao dat, dap da) xu ly
                    if (body) body.PlayAct(held.id);
                    RemovePlot(k);
                    S.Say(p.Empty ? "Đã lấp ô đất." : "Đã nhổ cây trồng.");
                    return true;
            }
            if (held.category == ItemCategory.Seed) { Plant(k, p, held); return true; }
            return false;
        }

        public SoilType SoilAt(int k)
        {
            var d = Data;
            Vector3? home = S && S.HasHome ? S.HomePosition : (Vector3?)null;
            return Soils.Classify(d, k % d.N, k / d.N, home);
        }

        /// <summary>Cuoc duoc khong: tren can, khong vat can, khong cau tau, nuoc rong khong tran toi.</summary>
        public bool CanTill(int k, out string why)
        {
            var d = Data;
            why = null;
            if (d.Pier[k] != 0 || d.Solid[k] != 0) { why = "Không cuốc được ở đây."; return false; }
            if (Placeables.Instance && Placeables.Instance.At(k) != null) { why = "Ô này đã lát đá. Nhặt đá lên bằng xẻng trước."; return false; }
            if (d.Fresh[k] != 0 || d.HT[k] < IslandData.SEA + .04f) { why = "Dưới nước thì không cuốc được."; return false; }
            if (d.HT[k] < TideClock.MaxLevel + .04f) { why = "Nước rong tràn tới ô này, cây sẽ chết mặn. Cuốc chỗ cao hơn."; return false; }
            return true;
        }

        bool IsWater(int k)
        {
            var d = Data;
            if (d.Pier[k] != 0) return false;
            return d.WaterSurface(k) - d.HT[k] > .02f;
        }

        void Till(int k, FarmPlot p)
        {
            var body = S.Body;
            if (p != null)
            {
                if (p.dead) { if (body) body.PlayAct("hoe"); FarmRules.Clear(p); Refresh(k); S.Say("Đã dọn cây chết."); return; }
                S.Say(p.Empty ? "Ô này đã cuốc rồi." : "Ô này đang có cây."); return;
            }
            if (!CanTill(k, out var why)) { S.Say(why); return; }
            if (!S.Stats.UseEnergy(2)) { S.Say("Hết sức rồi, nghỉ ngơi hoặc ăn gì đó đã."); return; }
            if (body) body.PlayAct("hoe");
            var plot = new FarmPlot { k = k, soil = SoilAt(k) };
            _plots[k] = plot;
            Refresh(k);
        }

        void Water(int k, FarmPlot p)
        {
            var d = Data;
            var body = S.Body;
            if (IsWater(k))
            {
                if (d.Fresh[k] == 0) { S.Say("Nước biển mặn, tưới vào cây sẽ héo. Lấy nước ở suối, ao hồ."); return; }
                if (body) body.PlayAct("can");
                CanWater = CanCapacity;
                S.Say($"Đã đổ đầy bình tưới ({CanCapacity} lượt).");
                return;
            }
            if (p == null) { S.Say("Tưới vào ô đất đã cuốc."); return; }
            if (p.watered) { S.Say("Ô này tưới rồi."); return; }
            if (CanWater <= 0) { S.Say("Bình hết nước. Ra suối hoặc ao hồ để đổ đầy."); return; }
            if (!S.Stats.UseEnergy(1)) { S.Say("Hết sức rồi."); return; }
            if (body) body.PlayAct("can");
            CanWater--;
            p.watered = true;
            Refresh(k);
            if (CanWater == 0) S.Say("Bình tưới vừa hết nước.");
        }

        /// <summary>Tui hat giong (dung cu "seed"): gieo loai dung mua dau tien trong tui.</summary>
        void PlantFromBag(int k, FarmPlot p)
        {
            var season = CurrentSeason;
            ItemDefinition pick = null, any = null;
            foreach (var id in S.Inventory.Order)
            {
                var it = S.Items.Get(id);
                if (!it || it.category != ItemCategory.Seed) continue;
                var c = Crops.FromSeed(it);
                if (c == null) continue;
                any ??= it;
                if (FarmRules.CanPlant(new FarmPlot(), c, season) == null) { pick = it; break; }
            }
            if (!pick && !any) { S.Say("Không còn hạt giống nào trong túi."); return; }
            Plant(k, p, pick ? pick : any);
        }

        void Plant(int k, FarmPlot p, ItemDefinition seed)
        {
            var c = Crops.FromSeed(seed);
            if (!S.Inventory.Has(seed.id)) { S.Say($"Hết {seed.displayName}."); return; }
            string err = FarmRules.CanPlant(p, c, CurrentSeason);
            if (err != null) { S.Say(err); return; }
            S.Inventory.Take(seed.id);
            if (S.Body) S.Body.PlayAct("seed");
            FarmRules.Plant(p, c);
            Refresh(k);
            if (!FarmRules.SoilOk(p, c))
                S.Say($"{c.displayName} hợp {Soils.Names(c.soils)}; trồng trên {Soils.Name(p.soil).ToLowerInvariant()} sẽ lớn chậm.");
        }

        void Harvest(int k, FarmPlot p, CropDefinition c)
        {
            int n = FarmRules.Harvest(p, c, _rng, out int q);
            int got = S.Give(c.produce, n, true);
            S.Stats.Calm = Mathf.Min(100, S.Stats.Calm + .5f);
            S.Say($"Thu hoạch {n} {c.displayName} ({FarmRules.QualityNames[q]})" + (got < n ? ", túi đầy nên để lại một ít dưới đất." : "."));
            Refresh(k);
        }

        void RemovePlot(int k)
        {
            _plots.Remove(k);
            if (_views.TryGetValue(k, out var v)) { Destroy(v.go); _views.Remove(k); }
        }

        // ---------------------------------------------------------------- qua ngay
        void OnDayStarted(int day)
        {
            var date = LunarCalendar.Date(day);
            var prev = LunarCalendar.Date(Mathf.Max(1, day - 1));
            int died = 0, offSeason = 0;
            var pl = Placeables.Instance;
            foreach (var p in _plots.Values)
            {
                var c = Crops.Get(p.crop);
                bool auto = pl && pl.Sprinkled(p.k);          // voi tuoi: tinh la da tuoi hom qua (ban web dong 1479)
                if (auto) p.watered = true;
                if (FarmRules.NewDay(p, c, date.Season)) { died++; if (p.deadReason == "trái mùa") offSeason++; }
                if (auto && !p.dead) p.watered = true;         // va tuoi luon sang nay (dong 1482)
            }
            foreach (var k in _plots.Keys) Refresh(k);
            if (date.Season != prev.Season && day > 1) S.Say($"Sang mùa {LunarCalendar.SeasonName(date.Season)}.");
            if (offSeason > 0) S.Say($"{offSeason} cây trái mùa đã tàn.");
            if (died - offSeason > 0) S.Say($"{died - offSeason} cây chết khô vì quên tưới.");
        }

        // ---------------------------------------------------------------- ve
        void Refresh(int k)
        {
            var d = Data;
            if (!_plots.TryGetValue(k, out var p)) return;
            if (!_root) { _root = new GameObject("Farm").transform; _root.SetParent(transform, false); }
            if (!_views.TryGetValue(k, out var v))
            {
                v = new PlotView { go = new GameObject("Plot_" + k) };
                v.go.transform.SetParent(_root, false);
                v.go.transform.position = new Vector3(k % d.N, d.HT[k], k / d.N);
                var soil = new GameObject("soil");
                soil.transform.SetParent(v.go.transform, false);
                soil.AddComponent<MeshFilter>().sharedMesh = CropMeshes.Soil;
                v.soil = soil.AddComponent<MeshRenderer>();
                v.soil.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                var crop = new GameObject("crop");
                crop.transform.SetParent(v.go.transform, false);
                crop.transform.localPosition = new Vector3(0, .04f, 0);
                crop.transform.localRotation = Quaternion.Euler(0, (k * 37) % 360, 0);
                v.cropFilter = crop.AddComponent<MeshFilter>();
                v.cropRenderer = crop.AddComponent<MeshRenderer>();
                v.cropRenderer.sharedMaterial = MaterialLibrary.Objects(false);
                _views[k] = v;
            }
            v.soil.sharedMaterial = MaterialLibrary.Solid(Soils.Tilled(p.soil, p.watered));
            var c = Crops.Get(p.crop);
            if (c == null) { v.cropRenderer.enabled = false; return; }
            v.cropRenderer.enabled = true;
            v.cropFilter.sharedMesh = CropMeshes.Get(c, FarmRules.Stage(p, c), p.dead ? 2 : p.Wilting ? 1 : 0);
        }

        // ---------------------------------------------------------------- luu / tai (dung cho he thong luu game)
        public List<FarmPlot> Snapshot() => new List<FarmPlot>(_plots.Values);

        public void Restore(IEnumerable<FarmPlot> plots)
        {
            foreach (var k in new List<int>(_plots.Keys)) RemovePlot(k);
            foreach (var p in plots) { _plots[p.k] = p; Refresh(p.k); }
        }
    }
}
