// Boi va lan bien (Docs/ThietKeGameplay.md muc 7.1): hoc nghe lan voi O Man.
//  - Nuoc sau hon muc loi: boi tren mat nuoc (can nghe lan bac 1). Space: lan xuong / noi len.
//  - Duoi nuoc: thanh hoi tho giam dan, noi len thi hoi lai. Het hoi: ngat (mat tui + 20% xu, het ngay).
//  - F duoi nuoc: nhat do day bien gan nhat; cam manh san ho thi cay san ho (+5 suc song vung).
//  - Camera xuong duoi nuoc, suong xanh luc.
using System.Collections.Generic;
using UnityEngine;

namespace SereneIsle
{
    public class DivingSystem : MonoBehaviour
    {
        public const string Craft = "lan";
        public int lootPerDay = 24;
        public float lootRadius = 35f;

        public static DivingSystem Instance { get; private set; }
        public bool Swimming { get; private set; }
        public bool Underwater { get; private set; }
        public float Breath { get; set; }
        public float MaxBreath { get; private set; } = 15;
        public float BreathPct => MaxBreath > 0 ? Breath / MaxBreath * 100f : 0;
        public IReadOnlyCollection<int> Corals => _corals;

        readonly List<(GameObject go, string id)> _loot = new List<(GameObject, string)>();
        readonly HashSet<int> _corals = new HashSet<int>();
        readonly Dictionary<int, GameObject> _coralViews = new Dictionary<int, GameObject>();
        int _lootDay = -1;
        float _walk, _time, _rot, _tilt;
        bool _drowning;
        static readonly int IdFogC = Shader.PropertyToID("_SI_FogColor"), IdFogP = Shader.PropertyToID("_SI_FogParams");

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { Instance = null; PlayerController.MovementOverride = null; OrbitCamera.AllowUnderwater = false; }

        GameSession S => GameSession.Instance;
        static IslandData Data => IslandTerrain.Instance ? IslandTerrain.Instance.Data : null;
        public static int Level => Crafts.Level(Craft);

        void Awake()
        {
            Instance = this;
            PlayerController.MovementOverride = Handle;
        }

        void Start() { if (S) S.DayStarted += OnDayStarted; }

        void OnDestroy()
        {
            if (S) S.DayStarted -= OnDayStarted;
            if (Instance == this) { Instance = null; PlayerController.MovementOverride = null; OrbitCamera.AllowUnderwater = false; }
        }

        void OnDayStarted(int day)
        {
            Swimming = Underwater = false;
            OrbitCamera.AllowUnderwater = false;
            _drowning = false;
            ClearLoot();
        }

        // ---------------------------------------------------------------- di chuyen (thay PlayerController khi boi)
        bool Handle(PlayerController pc, float dt)
        {
            var s = S; var d = Data;
            if (!s || d == null || !s.Running) return false;
            if (BoatSystem.Instance && BoatSystem.Instance.Handle(pc, dt)) { StopSwim(); return true; }   // dang cheo ghe
            var pos = pc.transform.position;
            float depth = d.WaterDepth(pos.x, pos.z);
            int level = Level;

            if (!Swimming)
            {
                if (_drowning || !DiveRules.CanSwim(level) || depth <= DiveRules.SwimEnterDepth) return false;
                StartSwim(s, level, pc.Facing);
            }
            // bi dua len bo (ngu, cuu, dich chuyen): thoi boi
            if (depth < .2f) { StopSwim(); return false; }
            _time += dt;

            // ---- huong boi theo camera ----
            Vector2 input = pc.InputLocked ? Vector2.zero : GameInput.Move;
            float il = Mathf.Min(1, input.magnitude);
            bool moving = il > .1f;
            float sp = DiveRules.SwimSpeed(level, Underwater) * pc.SpeedMultiplier;
            float x = pos.x, z = pos.z;
            if (moving)
            {
                float yaw = pc.cameraRig ? pc.cameraRig.Yaw : 0f;
                float fx = -Mathf.Sin(yaw), fz = -Mathf.Cos(yaw), rx = -Mathf.Cos(yaw), rz = Mathf.Sin(yaw);
                float mx = fx * input.y + rx * input.x, mz = fz * input.y + rz * input.x, ml = Mathf.Sqrt(mx * mx + mz * mz);
                mx /= ml; mz /= ml;
                float nx = x + mx * sp * il * dt, nz = z + mz * sp * il * dt;
                float nd = d.WaterDepth(nx, nz);
                bool inside = nx > 1 && nz > 1 && nx < d.N - 2 && nz < d.N - 2;
                if (inside && !(Underwater && nd > DiveRules.MaxDepth(level)) && !(Underwater && nd < .3f)) { x = nx; z = nz; }
                float target = Mathf.Atan2(mx, mz), dRot = Mathf.Atan2(Mathf.Sin(target - _rot), Mathf.Cos(target - _rot));
                _rot += dRot * Mathf.Min(1, dt * 8f);
                _walk += dt * sp * 1.6f;
            }

            // ---- lan / noi ----
            if (!pc.InputLocked && GameInput.JumpPressed) ToggleDive();
            OrbitCamera.AllowUnderwater = Underwater;

            // ---- hoi tho ----
            if (Underwater)
            {
                Breath -= dt;
                s.Stats.UseEnergy(dt * .4f);
                if (Breath <= 0) { Breath = 0; Drown(s); return true; }
            }
            else Breath = Mathf.Min(MaxBreath, Breath + dt * MaxBreath / 3f);

            // ---- do cao: noi tren mat nuoc / sat day ----
            // duoi nuoc: nam ngang boi sat day (bien quanh dao nong, dung thang thi dau con tren mat nuoc)
            float surfaceY = d.WaterLevel - .95f, floorY = d.HeightAt(x, z) + .2f;
            float targetY = Underwater ? floorY + .05f : Mathf.Max(surfaceY, floorY);
            float y = Mathf.MoveTowards(pos.y, targetY, dt * 2.2f);
            pc.Drive(new Vector3(x, y, z), _rot, moving, moving ? sp : 0);
            _tilt = Mathf.MoveTowards(_tilt, Underwater ? 78f : 0f, dt * 160f);
            if (_tilt > .1f) pc.transform.rotation = Quaternion.Euler(0, _rot * Mathf.Rad2Deg, 0) * Quaternion.Euler(_tilt, 0, 0);
            if (s.Body) s.Body.Pose(dt, new PoseInput
            {
                walkPhase = _walk, amp = moving ? .55f : 0, air = Underwater, vy = 0, time = _time, speed = moving ? sp : 0,
            });
            return true;
        }

        /// <summary>Lan xuong / noi len (Space khi dang boi). Tra ve true neu doi trang thai.</summary>
        public bool ToggleDive()
        {
            var s = S; var d = Data;
            if (!Swimming || !s || d == null) return false;
            if (Underwater) { Underwater = false; return true; }
            var p = s.Player.transform.position;
            string why = CanDive(Level, d.WaterDepth(p.x, p.z), s);
            if (why != null) { s.Say(why); return false; }
            Underwater = true;
            SpawnLootIfNeeded(s, d, Level);
            return true;
        }

        void StartSwim(GameSession s, int level, float facing)
        {
            Swimming = true; Underwater = false;
            MaxBreath = DiveRules.BreathSeconds(level, s.Date.Season);
            Breath = MaxBreath;
            _rot = facing;
        }

        void StopSwim()
        {
            if (_tilt > 0 && S && S.Player) S.Player.Face(S.Player.Facing);   // dung thang lai
            _tilt = 0;
            Swimming = Underwater = false;
            OrbitCamera.AllowUnderwater = false;
        }

        /// <summary>Lan duoc khong (null = duoc). Kiem tra do sau theo bac nghe, ban dem can den.</summary>
        public static string CanDive(int level, float depth, GameSession s)
        {
            if (depth < .45f) return "Nước cạn quá, chưa lặn được.";
            if (depth > DiveRules.MaxDepth(level))
                return level >= 2 ? "Chỗ này sâu quá, cần đèn lặn (nghề lặn bậc 3)." : "Chỗ này sâu quá, cần chân vịt (nghề lặn bậc 2).";
            if (s && s.Clock.IsNight && !DiveRules.CanDiveAtNight(level)) return "Trời tối, cần đèn lặn (nghề lặn bậc 3) mới lặn đêm được.";
            return null;
        }

        void Drown(GameSession s)
        {
            if (_drowning) return;
            _drowning = true;
            StopSwim();
            s.StartCoroutine(s.NewDay(NewDayReason.Drowned));
        }

        // ---------------------------------------------------------------- F duoi nuoc: nhat do / cay san ho
        /// <summary>Goi tu GameSession.UseSelected. Tra ve true neu dang duoi nuoc (da xu ly).</summary>
        public bool TryUse()
        {
            var s = S; var d = Data;
            if (!Underwater || !s || d == null) return false;
            var p = s.Player.transform.position;
            if (s.Hotbar.SelectedId == "manh_san_ho") { PlantCoral(s, d, p); return true; }

            int best = -1; float bd = 1.4f;
            for (int n = 0; n < _loot.Count; n++)
            {
                if (!_loot[n].go) continue;
                var q = _loot[n].go.transform.position;
                float dist = Vector2.Distance(new Vector2(q.x, q.z), new Vector2(p.x, p.z));
                if (dist < bd) { bd = dist; best = n; }
            }
            if (best < 0) { s.Say("Quanh đây không có gì. Bơi tới chỗ có rong, vỏ ốc dưới đáy."); return true; }
            var (go, id) = _loot[best];
            _loot.RemoveAt(best);
            Destroy(go);
            s.Give(id, 1);
            if (s.Body) s.Body.PlayAct("seed");
            return true;
        }

        void PlantCoral(GameSession s, IslandData d, Vector3 p)
        {
            int i = Mathf.RoundToInt(p.x), j = Mathf.RoundToInt(p.z), k = d.Idx(i, j);
            if (_corals.Contains(k)) { s.Say("Chỗ này đã cấy san hô rồi, bơi qua chỗ khác."); return; }
            if (!s.Inventory.Take("manh_san_ho")) { s.Say("Hết mảnh san hô."); return; }
            AddCoral(k);
            if (s.Body) s.Body.PlayAct("seed");
            if (s.Restoration) s.Restoration.Add(k, 5, "cấy san hô");
        }

        void AddCoral(int k)
        {
            var d = Data;
            if (d == null || !_corals.Add(k)) return;
            var go = new GameObject("Coral_" + k);
            go.transform.SetParent(transform, false);
            go.transform.SetPositionAndRotation(new Vector3(k % d.N, d.HT[k], k / d.N), Quaternion.Euler(0, (k * 53) % 360, 0));
            go.AddComponent<MeshFilter>().sharedMesh = SeabedMeshes.Coral;
            go.AddComponent<MeshRenderer>().sharedMaterial = MaterialLibrary.Objects(false);
            _coralViews[k] = go;
        }

        // ---------------------------------------------------------------- do day bien
        void SpawnLootIfNeeded(GameSession s, IslandData d, int level)
        {
            if (_lootDay == s.Clock.Day && _loot.Count > 0) return;
            ClearLoot();
            _lootDay = s.Clock.Day;
            var rng = new System.Random(s.Clock.Day * 104729 + d.Seed);
            var c = s.Player.transform.position;
            float maxD = Mathf.Min(DiveRules.MaxDepth(level), 3f);
            for (int t = 0; t < 5000 && _loot.Count < lootPerDay; t++)
            {
                int i = Mathf.RoundToInt(c.x + (float)(rng.NextDouble() * 2 - 1) * lootRadius);
                int j = Mathf.RoundToInt(c.z + (float)(rng.NextDouble() * 2 - 1) * lootRadius);
                if (!d.InBounds(i, j)) continue;
                int k = d.Idx(i, j);
                if (d.Ocean[k] == 0 || d.Pier[k] != 0) continue;
                float depth = IslandData.SEA - d.HT[k];
                if (depth < .45f || depth > maxD) continue;
                string id = DiveRules.PickLoot(depth, rng);
                var go = new GameObject("Seabed_" + id);
                go.transform.SetParent(transform, false);
                go.transform.SetPositionAndRotation(new Vector3(i + (float)(rng.NextDouble() - .5) * .6f, d.HeightAt(i, j), j + (float)(rng.NextDouble() - .5) * .6f),
                                                    Quaternion.Euler(0, (float)rng.NextDouble() * 360, 0));
                go.AddComponent<MeshFilter>().sharedMesh = SeabedMeshes.For(id);
                go.AddComponent<MeshRenderer>().sharedMaterial = MaterialLibrary.Objects(false);
                _loot.Add((go, id));
            }
        }

        void ClearLoot()
        {
            foreach (var l in _loot) if (l.go) Destroy(l.go);
            _loot.Clear();
        }

        /// <summary>Goi y tren HUD khi dang boi.</summary>
        public string Hint
        {
            get
            {
                if (!Swimming) return null;
                if (!Underwater) return "Space: lặn xuống";
                return S && S.Hotbar.SelectedId == "manh_san_ho" ? "F: cấy san hô · Space: nổi lên" : "F: nhặt đồ dưới đáy · Space: nổi lên";
            }
        }

        public int LootCount => _loot.Count;
        public IEnumerable<Vector3> LootPositions { get { foreach (var l in _loot) if (l.go) yield return l.go.transform.position; } }

        // ---------------------------------------------------------------- canh duoi nuoc
        void LateUpdate()
        {
            var cam = Camera.main;
            var d = Data;
            if (!cam || d == null || !Swimming) return;
            if (cam.transform.position.y >= d.WaterLevel) return;
            var deep = new Color(.12f, .42f, .46f);
            Shader.SetGlobalVector(IdFogC, new Vector4(deep.r, deep.g, deep.b, 1));
            Shader.SetGlobalVector(IdFogP, new Vector4(.5f, 13f, 1, 0));
            cam.backgroundColor = deep;
        }

        // ---------------------------------------------------------------- luu / tai
        public List<int> CoralSnapshot() => new List<int>(_corals);

        public void RestoreCorals(IEnumerable<int> list)
        {
            foreach (var go in _coralViews.Values) if (go) Destroy(go);
            _coralViews.Clear(); _corals.Clear();
            if (list != null) foreach (int k in list) AddCoral(k);
        }
    }

    /// <summary>Hinh do day bien va san ho (mau dinh).</summary>
    public static class SeabedMeshes
    {
        static readonly Dictionary<string, Mesh> _cache = new Dictionary<string, Mesh>();
        static Mesh _coral;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { _cache.Clear(); _coral = null; }

        public static Mesh For(string id)
        {
            if (_cache.TryGetValue(id, out var m) && m) return m;
            var b = new MeshBuilder();
            switch (id)
            {
                case "rong_sun":
                    for (int q = 0; q < 5; q++) b.PM(LowPolyShapes.Octa, "#c8a070", new Vector3((q - 2) * .05f, 0, (q % 2) * .05f), Quaternion.Euler(-10 + q * 5, q * 70, 0) * Vector3.up, .03f, .02f, .35f);
                    break;
                case "nhum":
                    b.P(LowPolyShapes.Ico0, "#3a2a4a", 0, .07f, 0, .09f, .07f, .09f);
                    for (int q = 0; q < 8; q++) b.PM(LowPolyShapes.Octa, "#2a1a3a", new Vector3(0, .07f, 0), Quaternion.Euler(-30 + (q % 2) * 50, q * 45, 0) * Vector3.forward, .01f, .01f, .16f);
                    break;
                case "hai_sam":
                    b.P(LowPolyShapes.Ico1, "#5a3a2a", 0, .04f, 0, .07f, .05f, .2f);
                    break;
                case "bao_ngu":
                    b.P(LowPolyShapes.Ico1, "#8a9a7a", 0, .03f, 0, .12f, .04f, .09f);
                    break;
                case "trai_ngoc":
                    b.P(LowPolyShapes.Ico1, "#4a4a5a", 0, .04f, 0, .11f, .05f, .11f);
                    b.P(LowPolyShapes.Ico0, "#f0ecf8", .02f, .07f, 0, .025f, .025f, .025f);
                    break;
                case "manh_san_ho":
                    for (int q = 0; q < 3; q++) b.PM(LowPolyShapes.Octa, "#f08a8a", Vector3.zero, Quaternion.Euler(-20, q * 120, 0) * Vector3.up, .03f, .03f, .18f);
                    break;
                default:   // oc huong
                    b.P(LowPolyShapes.Cone(1, 1, 6), "#e8c8a0", 0, .06f, 0, .06f, .12f, .06f, 1.4f, 0, 0);
                    break;
            }
            m = b.ToMesh("Seabed_" + id);
            _cache[id] = m;
            return m;
        }

        public static Mesh Coral
        {
            get
            {
                if (_coral) return _coral;
                var b = new MeshBuilder();
                string[] cols = { "#f08a8a", "#f0b05a", "#c870d8", "#5ac8b0" };
                for (int q = 0; q < 7; q++)
                {
                    var dir = Quaternion.Euler(-12 - (q % 3) * 10, q * 51, 0) * Vector3.up;
                    b.PM(LowPolyShapes.Octa, cols[q % 4], new Vector3((q % 3 - 1) * .1f, 0, (q % 2) * .1f - .05f), dir, .05f, .05f, .35f + (q % 3) * .08f);
                }
                b.P(LowPolyShapes.Ico0, "#e8d8c0", 0, .05f, 0, .22f, .07f, .2f);
                _coral = b.ToMesh("Coral");
                return _coral;
            }
        }
    }
}
