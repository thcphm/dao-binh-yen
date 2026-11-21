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
        readonly HashSet<int> _ditches = new HashSet<int>();
        HashSet<int> _wetDitches = new HashSet<int>();
        Transform _root;

        /// <summary>Phan bon dung truc tiep len o dang co cay: rong bien (bai trieu), ca com (phan ca).</summary>
        static readonly HashSet<string> Fertilizers = new HashSet<string> { "rong_bien", "f_anch" };
        public static bool IsFertilizer(ItemDefinition d) => d && Fertilizers.Contains(d.id);

        sealed class PlotView
        {
            public GameObject go;
            public MeshRenderer soil;
            public MeshFilter cropFilter;
            public MeshRenderer cropRenderer;
            public GameObject water, dike;
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

        // ---------------------------------------------------------------- muong, ruong nuoc
        static bool IsFresh(int k) { var d = Data; return d != null && d.Fresh[k] != 0; }

        /// <summary>O k ngap nuoc (ke muong co nuoc / nuoc ngot): trong lua, sen duoc va tu tuoi.</summary>
        public bool Flooded(int k) { var d = Data; return d != null && FarmWater.Flooded(k, d.N, _wetDitches, IsFresh); }
        public bool DitchWet(int k) => _wetDitches.Contains(k);

        void RecomputeWater()
        {
            var d = Data;
            if (d == null) return;
            _ditches.Clear();
            foreach (var p in _plots.Values) if (p.ditch) _ditches.Add(p.k);
            _wetDitches = FarmWater.WetDitches(_ditches, d.N, IsFresh);
            foreach (var k in _plots.Keys) Refresh(k);
        }

        /// <summary>Goi y tren HUD cho o phia truoc (ban web: hintFor).</summary>
        public string HintFor(ItemDefinition held)
        {
            int k = TargetTile();
            if (k < 0) return null;
            var p = PlotAt(k);
            if (p != null && p.ditch) return DitchWet(k) ? "Mương có nước" : "Mương khô: nối tới suối, ao hồ để có nước";
            var c = p != null ? Crops.Get(p.crop) : null;
            if (c != null)
            {
                if (p.dead) return $"{c.displayName} đã chết ({p.deadReason}). Cuốc lại để dọn.";
                if (FarmRules.Harvestable(p, c)) return $"Thu hoạch {c.displayName}";
                if (IsFertilizer(held) && !p.fertilized) return $"Bón {held.displayName.ToLowerInvariant()} cho {c.displayName}";
                string w = c.kind == CropKind.Tree && FarmRules.Mature(p, c)
                    ? (c.InSeason(CurrentSeason) ? "đang ra trái" : $"ra trái mùa {CropCatalogData.SeasonText(c.seasons)}")
                    : Flooded(k) ? "ruộng ngập nước" : p.watered ? "đã tưới" : p.dryDays > 0 ? "đang héo, cần tưới ngay" : "cần tưới";
                return $"{c.displayName}: {Mathf.FloorToInt(p.growth)}/{c.growDays} ngày · {w}" + (p.fertilized ? " · đã bón" : "");
            }
            if (p != null && p.saltDays > 0) return $"Đất mặn còn {p.saltDays} ngày";
            if (!held) return null;
            if (held.id == "hoe" && p == null) return CanTill(k, out _) ? $"Cuốc đất ({Soils.Name(SoilAt(k))}{(IsLowland(k) ? ", đất trũng" : "")})" : null;
            if (held.id == "hoe" && p != null && p.Empty && !p.dead && FarmWater.CanDig(k, Data.N, _ditches, IsFresh)) return "Cuốc mương dẫn nước";
            if (held.id == "shovel" && p != null && p.lowland && !p.dike) return $"Đắp bờ bao ngăn mặn ({FarmRules.DikeStones} đá)";
            if (held.id == "can") return IsWater(k) || (p != null && p.ditch && DitchWet(k)) ? "Đổ đầy bình tưới" : p != null && !p.watered ? $"Tưới nước ({CanWater} lượt)" : null;
            if (p != null && p.Empty && (held.category == ItemCategory.Seed || held.id == "seed")) return Flooded(k) ? "Gieo hạt (ruộng nước)" : "Gieo hạt";
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
                    if (held.id == "shovel" && p.lowland && !p.dike && !p.ditch) { BuildDike(k, p); return true; }
                    if (body) body.PlayAct(held.id);
                    RemovePlot(k);
                    if (p.ditch) RecomputeWater();
                    S.Say(p.ditch ? "Đã lấp mương." : p.Empty ? "Đã lấp ô đất." : "Đã nhổ cây trồng.");
                    return true;
            }
            if (IsFertilizer(held)) { Fertilize(k, p, held); return true; }
            if (held.category == ItemCategory.Seed) { Plant(k, p, held); return true; }
            return false;
        }

        bool IsLowland(int k) => Data.HT[k] < TideClock.MaxLevel + .04f;

        /// <summary>Dap bo bao quanh o dat trung (muc 4.6): ton 2 da, nuoc rong khong tran vao nua.</summary>
        void BuildDike(int k, FarmPlot p)
        {
            if (!S.Inventory.Has("stone", FarmRules.DikeStones)) { S.Say($"Cần {FarmRules.DikeStones} đá để đắp bờ bao ngăn mặn. Muốn lấp ô thì dùng cuốc chim."); return; }
            if (!S.Stats.UseEnergy(3)) { S.Say("Hết sức rồi."); return; }
            S.Inventory.Take("stone", FarmRules.DikeStones);
            if (S.Body) S.Body.PlayAct("shovel");
            p.dike = true;
            Refresh(k);
            S.Say("Đã đắp bờ bao: nước rong không tràn vào ô này nữa.");
        }

        void Fertilize(int k, FarmPlot p, ItemDefinition fert)
        {
            string err = FarmRules.CanFertilize(p);
            if (err != null) { S.Say(err); return; }
            if (!S.Inventory.Has(fert.id)) { S.Say($"Hết {fert.displayName}."); return; }
            S.Inventory.Take(fert.id);
            if (S.Body) S.Body.PlayAct("seed");
            p.fertilized = true;
            Refresh(k);
            S.Say($"Đã bón {fert.displayName.ToLowerInvariant()}: cây lớn nhanh hơn, nông sản ít nhất hạng ngon.");
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
            return true;   // dat trung (nuoc rong tran toi) van cuoc duoc, can dap bo bao de khoi chet man
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
                if (p.ditch) { S.Say("Đây là mương. Dùng xẻng để lấp."); return; }
                // cuoc lai o trong canh nguon nuoc ngot / muong: thanh muong dan nuoc (muc 4.3)
                if (p.Empty && FarmWater.CanDig(k, Data.N, _ditches, IsFresh))
                {
                    if (!S.Stats.UseEnergy(3)) { S.Say("Hết sức rồi."); return; }
                    if (body) body.PlayAct("hoe");
                    p.ditch = true; p.dike = false; p.fertilized = false;
                    RecomputeWater();
                    S.Say(DitchWet(k) ? "Đã cuốc mương: nước ngọt chảy vào. Ô ruộng sát mương sẽ ngập nước." : "Đã cuốc mương, nhưng chưa nối tới nguồn nước ngọt.");
                    return;
                }
                S.Say(p.Empty ? "Ô này đã cuốc rồi. Cuốc lại ô cạnh suối hoặc cạnh mương để làm mương." : "Ô này đang có cây."); return;
            }
            if (!CanTill(k, out var why)) { S.Say(why); return; }
            if (!S.Stats.UseEnergy(2)) { S.Say("Hết sức rồi, nghỉ ngơi hoặc ăn gì đó đã."); return; }
            if (body) body.PlayAct("hoe");
            var plot = new FarmPlot { k = k, soil = SoilAt(k), lowland = IsLowland(k) };
            _plots[k] = plot;
            HideGrass(k);
            Refresh(k);
            if (plot.lowland) S.Say($"Đất trũng: nước rong sẽ tràn vào làm cây chết mặn. Dùng xẻng và {FarmRules.DikeStones} đá để đắp bờ bao.");
        }

        void Water(int k, FarmPlot p)
        {
            var d = Data;
            var body = S.Body;
            if (IsWater(k) || (p != null && p.ditch && DitchWet(k)))
            {
                if (d.Fresh[k] == 0 && !(p != null && p.ditch)) { S.Say("Nước biển mặn, tưới vào cây sẽ héo. Lấy nước ở suối, ao hồ."); return; }
                if (body) body.PlayAct("can");
                CanWater = CanCapacity;
                S.Say($"Đã đổ đầy bình tưới ({CanCapacity} lượt).");
                return;
            }
            if (p == null || p.ditch) { S.Say("Tưới vào ô đất đã cuốc."); return; }
            if (p.watered) { S.Say("Ô này tưới rồi."); return; }
            if (Flooded(k)) { S.Say("Ruộng ngập nước, không cần tưới."); return; }
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
                if (FarmRules.CanPlant(new FarmPlot(), c, season, Flooded(k)) == null) { pick = it; break; }
            }
            if (!pick && !any) { S.Say("Không còn hạt giống nào trong túi."); return; }
            Plant(k, p, pick ? pick : any);
        }

        void Plant(int k, FarmPlot p, ItemDefinition seed)
        {
            var c = Crops.FromSeed(seed);
            if (!S.Inventory.Has(seed.id)) { S.Say($"Hết {seed.displayName}."); return; }
            string err = FarmRules.CanPlant(p, c, CurrentSeason, Flooded(k));
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
            n += Crafts.HarvestBonus(Crafts.Level(Crafts.Garden), _rng); // nghe lam vuon (Co Sau Vuon)
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
            int died = 0, offSeason = 0, salted = 0;
            var pl = Placeables.Instance;
            var d = Data;
            TideClock.DayRange(Mathf.Max(1, day - 1), out _, out float high);   // nuoc lon nhat hom qua
            foreach (var p in _plots.Values)
            {
                if (p.ditch) continue;
                var c = Crops.Get(p.crop);
                // voi tuoi / ruong ngap nuoc: tinh la da tuoi hom qua (ban web dong 1479)
                bool auto = (pl && pl.Sprinkled(p.k)) || Flooded(p.k);
                if (auto) p.watered = true;
                float before = p.growth;
                if (FarmRules.NewDay(p, c, date.Season)) { died++; if (p.deadReason == "trái mùa") offSeason++; }
                // nuoc rong hom qua tran vao dat trung chua dap bo bao: man tu hom nay (sau buoc rua man cua NewDay)
                if (d != null && FarmRules.SaltFlood(p, d.HT[p.k], high)) salted++;
                // trong cay an trai song qua 3 ngay: +3 suc song vung (muc 5)
                if (c != null && c.kind == CropKind.Tree && !p.dead && before < 3 && p.growth >= 3 && S.Restoration)
                    S.Restoration.Add(p.k, 3, $"cây {c.displayName.ToLowerInvariant()} bén rễ");
                if (auto && !p.dead) p.watered = true;         // va tuoi luon sang nay (dong 1482)
            }
            foreach (var k in _plots.Keys) Refresh(k);
            if (date.Season != prev.Season && day > 1) S.Say($"Sang mùa {LunarCalendar.SeasonName(date.Season)}.");
            if (offSeason > 0) S.Say($"{offSeason} cây trái mùa đã tàn.");
            if (salted > 0) S.Say($"Nước rong tràn vào {salted} ô đất trũng chưa đắp bờ bao: đất nhiễm mặn {FarmRules.SaltDays} ngày.");
            int dry = died - offSeason;   // o bi man da chet truoc khi qua ngay, khong tinh vao day
            if (dry > 0) S.Say($"{dry} cây chết khô vì quên tưới.");
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
            bool flooded = !p.ditch && Flooded(k);
            Color soilCol = Soils.Tilled(p.soil, p.watered || flooded || p.ditch);
            if (p.saltDays > 0) soilCol = Color.Lerp(soilCol, MaterialLibrary.Hex("#d8d2c4"), .55f);   // dat man: muoi trang
            v.soil.sharedMaterial = MaterialLibrary.Solid(soilCol);
            v.soil.transform.localPosition = new Vector3(0, p.ditch ? -.14f : 0, 0);
            // mat nuoc: muong co nuoc / ruong ngap nuoc
            bool showWater = (p.ditch && DitchWet(k)) || flooded;
            if (showWater && !v.water)
            {
                v.water = new GameObject("water");
                v.water.transform.SetParent(v.go.transform, false);
                v.water.AddComponent<MeshFilter>().sharedMesh = CropMeshes.WaterSheet;
                var wr = v.water.AddComponent<MeshRenderer>();
                wr.sharedMaterial = MaterialLibrary.Solid("#5aa6c4");
                wr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
            if (v.water)
            {
                v.water.SetActive(showWater);
                v.water.transform.localPosition = new Vector3(0, p.ditch ? -.05f : .025f, 0);
            }
            if (p.dike && !v.dike)
            {
                v.dike = new GameObject("dike");
                v.dike.transform.SetParent(v.go.transform, false);
                v.dike.AddComponent<MeshFilter>().sharedMesh = CropMeshes.DikeRim;
                v.dike.AddComponent<MeshRenderer>().sharedMaterial = MaterialLibrary.Solid("#7a6248");
            }
            if (v.dike) v.dike.SetActive(p.dike);
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
            foreach (var p in plots) { _plots[p.k] = p; HideGrass(p.k); }
            RecomputeWater();   // ve lai tat ca o (can biet muong nao co nuoc)
        }

        /// <summary>O dat da cuoc thi khong con co / hoa moc xuyen qua.</summary>
        static void HideGrass(int k)
        {
            var t = IslandTerrain.Instance;
            if (t && t.Vegetation) t.Vegetation.HideGrass(k);
        }
    }
}
