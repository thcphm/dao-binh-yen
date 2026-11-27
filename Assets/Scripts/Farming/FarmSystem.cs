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

        /// <summary>Phan bon dung truc tiep len o dang co cay: rong bien (bai trieu), ca com (phan ca), phan chuong (vat nuoi).</summary>
        static readonly HashSet<string> Fertilizers = new HashSet<string> { "rong_bien", "f_anch", "phan_chuong", "tro_bep" };
        public static bool IsFertilizer(ItemDefinition d) => d && Fertilizers.Contains(d.id);

        // dong giong giu lai (muc 4.4): crop -> the he, hang, ten giong nha
        readonly Dictionary<string, Strain> _strains = new Dictionary<string, Strain>();
        public Strain StrainOf(string crop) => crop != null && _strains.TryGetValue(crop, out var s) ? s : null;

        sealed class PlotView
        {
            public GameObject go;
            public MeshRenderer soil;
            public MeshFilter cropFilter;
            public MeshRenderer cropRenderer;
            public GameObject water, dike, pest, stake, gate, plank, shrimp;
            public MeshRenderer waterRenderer;
        }

        static GameObject Part(Transform parent, ref GameObject slot, string name, Mesh mesh)
        {
            if (slot) return slot;
            slot = new GameObject(name);
            slot.transform.SetParent(parent, false);
            slot.AddComponent<MeshFilter>().sharedMesh = mesh;
            slot.AddComponent<MeshRenderer>().sharedMaterial = MaterialLibrary.Objects(false);
            return slot;
        }

        static void Show(GameObject go, bool on) { if (go) go.SetActive(on); }

        bool Sheltered(int k) => Placeables.Instance && Placeables.Instance.Sheltered(k);

        float VitalityAt(int k)
        {
            var d = Data;
            return S && S.Restoration && d != null ? S.Restoration.VitalityAt(new Vector3(k % d.N, 0, k / d.N)) : 0;
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
            if (p != null && p.sluiceOpen)
                return FarmCare.ShrimpReady(p) ? "Tôm đã lớn: E để thu tôm"
                     : p.shrimpAge > 0 ? $"Ruộng nuôi tôm: {p.shrimpAge - 1}/{FarmCare.ShrimpDays} ngày · E đóng cống sẽ mất tôm"
                     : held && held.id == "tom_giong" ? "Thả tôm giống" : "Cống mở, ruộng ngập nước lợ · thả tôm giống mùa Hạ · E đóng cống";
            var c = p != null ? Crops.Get(p.crop) : null;
            if (c != null)
            {
                if (p.dead) return $"{c.displayName} đã chết ({p.deadReason}). Cuốc lại để dọn.";
                if (p.pest != Pest.None)
                    return $"{c.displayName} bị {FarmCare.PestName(p.pest)} ({p.pestDays}/{FarmCare.PestKillDays} ngày) · "
                         + (FarmCare.HandPickable(p.pest) ? "E bắt tay, hoặc " : "") + "dùng nước tỏi ớt, tro bếp";
                if (p.saved && !FarmCare.SeedReady(p)) return $"{c.displayName} để già lấy giống: {p.seedAge}/{FarmCare.SeedAgeDays} ngày";
                if (p.saved) return $"Thu hạt giống {c.displayName}";
                if (held && held.id.StartsWith(FarmCare.ScionPrefix) && c.kind == CropKind.Tree && p.graft == null) return $"Ghép {held.displayName.ToLowerInvariant()} lên {c.displayName}";
                if (FarmRules.Harvestable(p, c)) return $"Thu hoạch {c.displayName}" + (c.kind != CropKind.Tree ? " · E: chừa làm giống" : "");
                if (IsFertilizer(held) && !p.fertilized) return $"Bón {held.displayName.ToLowerInvariant()} cho {c.displayName}";
                string w = c.kind == CropKind.Tree && FarmRules.Mature(p, c)
                    ? (c.InSeason(CurrentSeason) ? "đang ra trái" : $"ra trái mùa {CropCatalogData.SeasonText(c.seasons)}")
                    : Flooded(k) ? "ruộng ngập nước" : p.watered ? "đã tưới" : p.dryDays > 0 ? "đang héo, cần tưới ngay" : "cần tưới";
                return $"{CropLabel(p, c)}: {Mathf.FloorToInt(p.growth)}/{c.growDays} ngày · {w}" + (p.fertilized ? " · đã bón" : "")
                       + (p.nearLegume ? " · xen đậu" : "") + (Sheltered(k) ? " · nhà lưới" : "");
            }
            if (p != null && held && held.id == "cong") return FarmCare.CanBuildSluice(p) ?? "Đặt cống chặn nước";
            if (p != null && p.sluice && p.Empty) return "Cống đóng · E mở cống cho nước lợ vào nuôi tôm (mùa Hạ)";
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
                if (FarmCare.ShrimpReady(p)) { if (body) body.PlayAct("seed"); HarvestShrimp(k, p); return true; }
                var c = Crops.Get(p.crop);
                bool grafting = held.id.StartsWith(FarmCare.ScionPrefix) && c != null && c.kind == CropKind.Tree && p.graft == null;
                if (c != null && !grafting && FarmRules.Harvestable(p, c)) { if (body) body.PlayAct("seed"); Harvest(k, p, c); return true; }
            }

            // vuon ruong nang cao: canh ghep, cong, tom giong, tri sau
            if (held.id.StartsWith(FarmCare.ScionPrefix)) { Graft(k, p, held); return true; }
            if (held.id == "cong") { BuildSluice(k, p); return true; }
            if (held.id == "tom_giong") { StockShrimp(k, p); return true; }
            if (held.id == "nuoc_toi_ot" || (held.id == "tro_bep" && p != null && p.pest != Pest.None)) { Treat(k, held.id); return true; }

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

        // ---------------------------------------------------------------- vuon ruong nang cao (FarmCare)
        string CropLabel(FarmPlot p, CropDefinition c)
        {
            if (c.kind == CropKind.Tree && p.graft != null)
            {
                if (p.graft == c.id) return c.displayName + " ghép";
                var sc = Crops.Get(p.graft);
                return $"{c.displayName} lai {(sc ? sc.displayName.ToLowerInvariant() : p.graft)} ({FarmCare.TraitName(p.trait)})";
            }
            var st = p.homeStrain ? StrainOf(c.id) : null;
            return st != null && st.name != null ? st.name : c.displayName;
        }

        /// <summary>Phim E o o ruong phia truoc: thu tom, bat sau, chua giong, mo / dong cong. null neu khong co gi.</summary>
        public Action NearbyAction(out string label)
        {
            label = null;
            int k = TargetTile();
            var p = k >= 0 ? PlotAt(k) : null;
            if (p == null || p.ditch || !S) return null;
            if (FarmCare.ShrimpReady(p)) { label = "E: Thu tôm"; return () => HarvestShrimp(k, p); }
            var c = Crops.Get(p.crop);
            if (c != null && !p.dead && p.pest != Pest.None && FarmCare.HandPickable(p.pest))
            {
                label = $"E: Bắt {FarmCare.PestName(p.pest)}";
                return () => Treat(k, null);
            }
            if (c != null && FarmCare.CanSave(p, c) == null)
            {
                label = "E: Chừa cây làm giống";
                return () => SaveSeedPlant(k, p, c);
            }
            if (p.sluice && (p.Empty || p.dead || p.sluiceOpen))
            {
                label = p.sluiceOpen ? "E: Đóng cống" : "E: Mở cống";
                return () => ToggleSluice(k, p);
            }
            return null;
        }

        void SaveSeedPlant(int k, FarmPlot p, CropDefinition c)
        {
            string err = FarmCare.CanSave(p, c);
            if (err != null) { S.Say(err); return; }
            p.saved = true; p.seedAge = 0;
            Refresh(k);
            S.Say($"Đã cắm cọc chừa {c.displayName} làm giống: để già {FarmCare.SeedAgeDays} ngày rồi thu hạt.");
        }

        /// <summary>Tri sau: tay khong (bat sau xanh, oc buou) 1 o; tro bep 1 o; nuoc toi ot 9 o quanh o phia truoc.</summary>
        void Treat(int k, string item)
        {
            var d = Data;
            var p = PlotAt(k);
            if (item == null)
            {
                if (p == null || !FarmCare.HandPickable(p.pest)) { S.Say("Rầy nhỏ quá, bắt tay không xuể: phun nước tỏi ớt hoặc rắc tro bếp."); return; }
                if (!S.Stats.UseEnergy(2)) { S.Say("Hết sức rồi."); return; }
                if (S.Body) S.Body.PlayAct("seed");
                string what = FarmCare.PestName(p.pest);
                FarmCare.Cure(p);
                Refresh(k);
                S.Say($"Đã bắt hết {what}.");
                return;
            }
            if (!S.Inventory.Has(item)) { S.Say($"Hết {S.Items.NameOf(item)}."); return; }
            var targets = new List<int>();
            if (item == "nuoc_toi_ot")
            {
                int i0 = k % d.N, j0 = k / d.N;
                for (int dj = -1; dj <= 1; dj++)
                    for (int di = -1; di <= 1; di++)
                        if (d.InBounds(i0 + di, j0 + dj)) targets.Add(d.Idx(i0 + di, j0 + dj));
            }
            else targets.Add(k);
            int cured = 0;
            foreach (int q in targets) { var t = PlotAt(q); if (t != null && t.pest != Pest.None) { FarmCare.Cure(t); Refresh(q); cured++; } }
            if (cured == 0) { S.Say("Quanh đây không có cây nào bị sâu."); return; }
            S.Inventory.Take(item);
            if (S.Body) S.Body.PlayAct("can");
            S.Say(item == "nuoc_toi_ot" ? $"Đã phun nước tỏi ớt: {cured} ô hết sâu." : "Đã rắc tro bếp: hết sâu.");
        }

        void Graft(int k, FarmPlot p, ItemDefinition scionItem)
        {
            var c = p != null ? Crops.Get(p.crop) : null;
            var sc = Crops.Get(scionItem.id.Substring(FarmCare.ScionPrefix.Length));
            string err = FarmCare.CanGraft(p, c, sc);
            if (err != null) { S.Say(err); return; }
            if (!S.Stats.UseEnergy(2)) { S.Say("Hết sức rồi."); return; }
            S.Inventory.Take(scionItem.id);
            if (S.Body) S.Body.PlayAct("seed");
            var t = FarmCare.Graft(p, c, sc);
            Refresh(k);
            S.Say(t == GraftTrait.None
                ? $"Đã ghép cành đầu dòng lên cây {c.displayName.ToLowerInvariant()}: trái sẽ đạt hạng đặc sản nếu chăm kỹ."
                : $"Ghép {sc.displayName.ToLowerInvariant()} lên {c.displayName.ToLowerInvariant()}: ra giống lai {FarmCare.TraitName(t)}!");
        }

        void BuildSluice(int k, FarmPlot p)
        {
            string err = FarmCare.CanBuildSluice(p);
            if (err != null) { S.Say(err); return; }
            if (!S.Stats.UseEnergy(3)) { S.Say("Hết sức rồi."); return; }
            S.Inventory.Take("cong");
            if (S.Body) S.Body.PlayAct("shovel");
            p.sluice = true;
            Refresh(k);
            S.Say("Đã đặt cống. Mùa Hạ mở cống (E) cho nước lợ vào nuôi tôm; mùa Thu đóng cống, rửa mặn rồi cấy lúa chịu mặn.");
        }

        void ToggleSluice(int k, FarmPlot p)
        {
            if (S.Body) S.Body.PlayAct("shovel");
            if (p.sluiceOpen)
            {
                bool lost = FarmCare.CloseSluice(p);
                S.Say(lost ? "Đóng cống khi tôm chưa lớn: tôm chết hết."
                           : $"Đã đóng cống. Đất mặn {FarmRules.SaltDays} ngày, tưới nước ngọt để rửa nhanh; lúa chịu mặn cấy được khi còn {FarmCare.TolerantSalt} ngày.");
            }
            else
            {
                bool hadCrop = !p.Empty && !p.dead;
                FarmCare.OpenSluice(p);
                S.Say((hadCrop ? "Nước lợ tràn vào làm chết cây trong ô. " : "") + "Đã mở cống: ruộng thành đầm nước lợ, thả tôm giống vào mùa Hạ.");
            }
            Refresh(k);
        }

        void StockShrimp(int k, FarmPlot p)
        {
            string err = FarmCare.CanStock(p, CurrentSeason);
            if (err != null) { S.Say(err); return; }
            S.Inventory.Take("tom_giong");
            if (S.Body) S.Body.PlayAct("seed");
            FarmCare.Stock(p);
            Refresh(k);
            S.Say($"Đã thả tôm giống: {FarmCare.ShrimpDays} ngày nữa thu.");
        }

        void HarvestShrimp(int k, FarmPlot p)
        {
            int n = FarmCare.HarvestShrimp(p, _rng);
            int got = S.Give("tom_su", n, true);
            Refresh(k);
            S.Say($"Thu {n} tôm sú" + (got < n ? ", túi đầy nên để lại một ít." : ". Bùn đáy ruộng tôm sẽ bón luôn cho vụ lúa sau."));
        }

        void CollectSeeds(int k, FarmPlot p, CropDefinition c)
        {
            if (!_strains.TryGetValue(c.id, out var st)) _strains[c.id] = st = new Strain { crop = c.id };
            int n = FarmCare.CollectSeeds(p, c, st, _rng, out bool home);
            int got = S.Give(FarmCare.SavedSeedPrefix + c.id, n, true);
            Refresh(k);
            S.Say($"Thu {n} hạt giữ giống {c.displayName.ToLowerInvariant()} (cây mẹ hạng {FarmRules.QualityNames[st.q]}, vụ thứ {st.gen})"
                  + (got < n ? ", túi đầy nên để lại một ít." : "."));
            if (home) AskStrainName(st, c);
        }

        void AskStrainName(Strain st, CropDefinition c)
        {
            string region = null;
            if (S.Restoration && S.Restoration.Model != null && S.Player)
            {
                var pp = S.Player.transform.position;
                region = S.Restoration.Model.RegionAt(pp.x, pp.z)?.name;
            }
            var names = FarmCare.StrainNames(c.displayName, S.PlayerName, region);
            st.name = names[0];   // mac dinh neu dong hop thoai
            if (!ActionUI.Instance) return;
            var buttons = new (string, Action)[names.Length];
            for (int i = 0; i < names.Length; i++) { string nm = names[i]; buttons[i] = (nm, () => { st.name = nm; S.Say($"Giống nhà \"{nm}\" đã vào sổ: cây gieo từ hạt này lên một hạng."); }); }
            ActionUI.Instance.ShowDialog("Giống nhà",
                $"Giữ giống {c.displayName.ToLowerInvariant()} được {st.gen} vụ liền. Giống đã quen đất đảo: đặt tên cho giống nhà nhé.", buttons);
        }

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
            bool low = TillOne(k);
            // trau cay (muc 4.3): co trau trong chuong thi moi lan cuoc lam them 3 o thang hang phia truoc (nhanh gap 4)
            int extra = 0;
            if (HasBuffalo())
            {
                var d = Data;
                float f = S.Player.Facing;
                int i0 = k % d.N, j0 = k / d.N, di = Mathf.RoundToInt(Mathf.Sin(f)), dj = Mathf.RoundToInt(Mathf.Cos(f));
                if (di == 0 && dj == 0) dj = 1;
                for (int q = 1; q <= 3; q++)
                {
                    int i = i0 + di * q, j = j0 + dj * q;
                    if (!d.InBounds(i, j)) break;
                    int kk = d.Idx(i, j);
                    if (_plots.ContainsKey(kk) || !CanTill(kk, out _)) break;
                    low |= TillOne(kk); extra++;
                }
                if (extra > 0) S.Say($"Trâu cày thêm {extra} ô.");
            }
            if (low) S.Say($"Đất trũng: nước rong sẽ tràn vào làm cây chết mặn. Dùng xẻng và {FarmRules.DikeStones} đá để đắp bờ bao.");
        }

        bool TillOne(int k)
        {
            var plot = new FarmPlot { k = k, soil = SoilAt(k), lowland = IsLowland(k) };
            _plots[k] = plot;
            HideGrass(k);
            Refresh(k);
            return plot.lowland;
        }

        /// <summary>Co trau o trong chuong (Animals, giai doan 4c).</summary>
        public static bool HasBuffalo() => Animals.Instance && Animals.Instance.Farm.Exists(a => a.type == "trau" && a.pen >= 0);

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
                if (FarmRules.CanPlant(new FarmPlot(), c, season, Flooded(k), Sheltered(k)) == null) { pick = it; break; }
            }
            if (!pick && !any) { S.Say("Không còn hạt giống nào trong túi."); return; }
            Plant(k, p, pick ? pick : any);
        }

        void Plant(int k, FarmPlot p, ItemDefinition seed)
        {
            var c = Crops.FromSeed(seed);
            if (!S.Inventory.Has(seed.id)) { S.Say($"Hết {seed.displayName}."); return; }
            string err = FarmRules.CanPlant(p, c, CurrentSeason, Flooded(k), Sheltered(k));
            if (err != null) { S.Say(err); return; }
            S.Inventory.Take(seed.id);
            if (S.Body) S.Body.PlayAct("seed");
            FarmRules.Plant(p, c);
            if (seed.id.StartsWith(FarmCare.SavedSeedPrefix)) FarmCare.ApplyStrain(p, StrainOf(c.id));
            p.nearLegume = FarmCare.NearLegume(k, Data.N, PlotAt);
            Refresh(k);
            if (!FarmRules.SoilOk(p, c))
                S.Say($"{c.displayName} hợp {Soils.Names(c.soils)}; trồng trên {Soils.Name(p.soil).ToLowerInvariant()} sẽ lớn chậm.");
        }

        void Harvest(int k, FarmPlot p, CropDefinition c)
        {
            if (p.saved) { CollectSeeds(k, p, c); return; }
            string label = CropLabel(p, c);
            int n = FarmRules.Harvest(p, c, _rng, out int q);
            n += Crafts.HarvestBonus(Crafts.Level(Crafts.Garden), _rng); // nghe lam vuon (Co Sau Vuon)
            // thien dich (muc 4.5): vung suc song tu 50 co ong thu phan, dua bi cay trai them 10%
            bool bees = FarmCare.PollinatorCrop(c) && VitalityAt(k) >= FarmCare.EnemyVitality && _rng.NextDouble() < FarmCare.EnemyYield * n;
            if (bees) n++;
            int got = S.Give(c.produce, n, true);
            S.Stats.Calm = Mathf.Min(100, S.Stats.Calm + .5f);
            S.Say($"Thu hoạch {n} {label} ({FarmRules.QualityNames[q]})" + (bees ? ", ong thụ phấn được thêm 1" : "")
                  + (got < n ? ", túi đầy nên để lại một ít dưới đất." : "."));
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
            int died = 0, offSeason = 0, salted = 0, pestDead = 0;
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
                bool pestBefore = p.pest != Pest.None;
                if (FarmRules.NewDay(p, c, date.Season, false, Sheltered(p.k)))
                {
                    died++;
                    if (p.deadReason == "trái mùa") offSeason++;
                    else if (pestBefore) pestDead++;
                }
                // nuoc rong hom qua tran vao dat trung chua dap bo bao: man tu hom nay (sau buoc rua man cua NewDay)
                if (d != null && FarmRules.SaltFlood(p, d.HT[p.k], high, c != null && c.saltTolerant)) salted++;
                // trong cay an trai song qua 3 ngay: +3 suc song vung (muc 5)
                if (c != null && c.kind == CropKind.Tree && !p.dead && before < 3 && p.growth >= 3 && S.Restoration)
                    S.Restoration.Add(p.k, 3, $"cây {c.displayName.ToLowerInvariant()} bén rễ");
                if (auto && !p.dead) p.watered = true;         // va tuoi luon sang nay (dong 1482)
            }
            int newPests = d != null ? SpreadPests(d, day >= FarmCare.PestStartDay) : 0;
            foreach (var k in _plots.Keys) Refresh(k);
            if (newPests > 0) S.Say($"Sâu bệnh xuất hiện ở {newPests} ô ruộng. Bắt tay, phun nước tỏi ớt hoặc rắc tro bếp trong {FarmCare.PestKillDays} ngày.");
            if (pestDead > 0) S.Say($"{pestDead} cây chết vì sâu bệnh.");
            if (date.Season != prev.Season && day > 1) S.Say($"Sang mùa {LunarCalendar.SeasonName(date.Season)}.");
            if (offSeason > 0) S.Say($"{offSeason} cây trái mùa đã tàn.");
            if (salted > 0) S.Say($"Nước rong tràn vào {salted} ô đất trũng chưa đắp bờ bao: đất nhiễm mặn {FarmRules.SaltDays} ngày.");
            int dry = died - offSeason - pestDead;   // o bi man da chet truoc khi qua ngay, khong tinh vao day
            if (dry > 0) S.Say($"{dry} cây chết khô vì quên tưới.");
        }

        /// <summary>Sau benh moi sang (muc 4.5): o dang bi sau lay sang o cung loai ben canh (25%), o khoe bi sau theo
        /// suc song vung, doc canh, xen canh, nha luoi. Cap nhat co xen dau. Tra ve so o moi bi sau.</summary>
        int SpreadPests(IslandData d, bool allow)
        {
            var hit = new List<(int k, Pest pest)>();
            foreach (var p in _plots.Values)
            {
                p.nearLegume = FarmCare.NearLegume(p.k, d.N, PlotAt);
                var c = Crops.Get(p.crop);
                if (!allow) continue;
                if (p.pest != Pest.None && !p.dead)
                {
                    foreach (int q in new[] { p.k - 1, p.k + 1, p.k - d.N, p.k + d.N })
                    {
                        var t = PlotAt(q);
                        if (t != null && t.crop == p.crop && FarmCare.CanGetPest(t, c) && _rng.NextDouble() < .25) hit.Add((q, p.pest));
                    }
                    continue;
                }
                if (!FarmCare.CanGetPest(p, c)) continue;
                bool mono = FarmCare.ClusterSize(p.k, d.N, PlotAt, FarmCare.MonoLimit + 1) > FarmCare.MonoLimit;
                double ch = FarmCare.PestChance(VitalityAt(p.k), mono, FarmCare.NearRepellent(p.k, d.N, PlotAt), Sheltered(p.k));
                if (_rng.NextDouble() < ch) hit.Add((p.k, FarmCare.PestFor(c)));
            }
            int n = 0;
            foreach (var (k, pest) in hit)
            {
                var t = PlotAt(k);
                if (t == null || t.pest != Pest.None) continue;
                t.pest = pest; t.pestDays = 0; n++;
            }
            return n;
        }

        // ---------------------------------------------------------------- thoi tiet (goi tu he thoi tiet, giai doan 6)
        /// <summary>Troi mua: moi o ruong (tru muong) tinh la da tuoi hom nay. Tra ve so o duoc tuoi.</summary>
        public int WaterAllByRain()
        {
            int n = 0;
            foreach (var p in _plots.Values)
            {
                if (p.ditch || p.watered) continue;
                p.watered = true; n++;
            }
            foreach (var k in _plots.Keys) Refresh(k);
            return n;
        }

        /// <summary>Bao quet qua (Docs/ThietKeGameplay.md muc 8): cay ngan ngay hu 70%, cay an trai rung trai.
        /// Tra ve so cay chet va so cay rung trai; tu bao len HUD.</summary>
        public (int killed, int fruitLost) ApplyStorm(System.Random rng)
        {
            int killed = 0, lost = 0;
            foreach (var p in _plots.Values)
            {
                if (Sheltered(p.k)) continue;   // nha luoi che bao (muc 4.8)
                int r = FarmRules.StormDamage(p, Crops.Get(p.crop), rng);
                if (r == 1) killed++; else if (r == 2) lost++;
            }
            foreach (var k in _plots.Keys) Refresh(k);
            if (S && (killed > 0 || lost > 0))
                S.Say($"Bão quét qua vườn: {killed} cây hư" + (lost > 0 ? $", {lost} cây ăn trái rụng hết trái." : "."));
            return (killed, lost);
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
            bool showWater = (p.ditch && DitchWet(k)) || flooded || p.sluiceOpen;
            if (showWater && !v.water)
            {
                v.water = new GameObject("water");
                v.water.transform.SetParent(v.go.transform, false);
                v.water.AddComponent<MeshFilter>().sharedMesh = CropMeshes.WaterSheet;
                v.waterRenderer = v.water.AddComponent<MeshRenderer>();
                v.waterRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
            if (v.water)
            {
                v.water.SetActive(showWater);
                v.waterRenderer.sharedMaterial = MaterialLibrary.Solid(p.sluiceOpen ? "#6a9a8a" : "#5aa6c4");   // nuoc lo xanh duc
                v.water.transform.localPosition = new Vector3(0, p.ditch ? -.05f : p.sluiceOpen ? .06f : .025f, 0);
            }
            // cong chan nuoc (tam van nang len khi mo) + ruong tom
            if (p.sluice)
            {
                Part(v.go.transform, ref v.gate, "sluice", CropMeshes.SluiceGate);
                Part(v.go.transform, ref v.plank, "plank", CropMeshes.SluicePlank);
                v.plank.transform.localPosition = new Vector3(0, p.sluiceOpen ? .22f : 0, -.46f);
            }
            Show(v.gate, p.sluice); Show(v.plank, p.sluice);
            if (p.shrimpAge > 0) Part(v.go.transform, ref v.shrimp, "shrimp", CropMeshes.Shrimp);
            Show(v.shrimp, p.shrimpAge > 0);
            if (p.dike && !v.dike)
            {
                v.dike = new GameObject("dike");
                v.dike.transform.SetParent(v.go.transform, false);
                v.dike.AddComponent<MeshFilter>().sharedMesh = CropMeshes.DikeRim;
                v.dike.AddComponent<MeshRenderer>().sharedMaterial = MaterialLibrary.Solid("#7a6248");
            }
            if (v.dike) v.dike.SetActive(p.dike);
            var c = Crops.Get(p.crop);
            bool pest = c != null && !p.dead && p.pest != Pest.None;
            if (pest) Part(v.go.transform, ref v.pest, "pest", CropMeshes.PestMarker);
            Show(v.pest, pest);
            bool stake = c != null && !p.dead && p.saved;
            if (stake) Part(v.go.transform, ref v.stake, "stake", CropMeshes.SeedStake);
            Show(v.stake, stake);
            if (c == null) { v.cropRenderer.enabled = false; return; }
            v.cropRenderer.enabled = true;
            v.cropFilter.sharedMesh = CropMeshes.Get(c, FarmRules.Stage(p, c), p.dead ? 2 : p.Wilting || pest ? 1 : 0);
        }

        // ---------------------------------------------------------------- luu / tai (dung cho he thong luu game)
        public List<FarmPlot> Snapshot() => new List<FarmPlot>(_plots.Values);

        public List<Strain> StrainSnapshot() => new List<Strain>(_strains.Values);

        public void RestoreStrains(IEnumerable<Strain> list)
        {
            _strains.Clear();
            if (list == null) return;
            foreach (var s in list)
            {
                if (s == null || string.IsNullOrEmpty(s.crop)) continue;
                if (string.IsNullOrEmpty(s.name)) s.name = null;
                _strains[s.crop] = s;
            }
        }

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
