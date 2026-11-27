// Dam nuoi o ran san ho da hoi sinh (Docs/ThietKeGameplay.md muc 10: thay "bao tang"). Cay du 3 manh san ho thi
// dung rao tre quanh cho san ho gan xom nhat. Boi / dung canh dam bam E: tha moi loai ca bien, oc, so... 1 con vao nuoi,
// ca boi vong trong rao. Moc 5 / 12 / 20 loai: thuong xu + suc song vung. Manh san ho tha vao thanh vuon san ho trong dam.
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SereneIsle
{
    public class ReefPond : MonoBehaviour
    {
        public static ReefPond Instance { get; private set; }

        public const int CoralsNeeded = 3;
        public const float Radius = 2.6f, Reach = 4.6f;
        public const int MaxCoralDecor = 8;
        public static readonly int[] Goals = { 5, 12, 20 };
        public static readonly int[] GoalCoins = { 150, 400, 900 };
        /// <summary>Do bien khong phai ca cau duoc nhung tha nuoi duoc.</summary>
        public static readonly HashSet<string> SeaCritters = new HashSet<string>
            { "so_huyet", "ngheu", "oc_buou", "sao_bien", "oc_huong", "nhum", "hai_sam", "bao_ngu", "trai_ngoc", "tom_su" };

        public int CenterK { get; private set; } = -1;
        public bool Open => CenterK >= 0;
        public int Count => _species.Count;
        public int CoralDecor { get; private set; }
        public bool Has(string id) => _species.Contains(id);
        public IEnumerable<string> Species => _species;

        readonly List<string> _species = new List<string>();
        readonly List<Swimmer> _swim = new List<Swimmer>();
        readonly List<GameObject> _decor = new List<GameObject>();
        GameObject _pen;
        float _check;

        sealed class Swimmer { public Transform t; public float r, speed, phase, y, bob; }

        GameSession S => GameSession.Instance;
        static IslandData D => IslandTerrain.Instance ? IslandTerrain.Instance.Data : null;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { Instance = null; }

        void Awake() { Instance = this; }
        void OnDestroy() { if (Instance == this) Instance = null; }

        /// <summary>Tha nuoi duoc: ca bien (song o bien, ran nhiet doi) va do bien. Ca nuoc ngot thi khong.</summary>
        public static bool Keepable(ItemDefinition d)
        {
            if (!d) return false;
            if (SeaCritters.Contains(d.id)) return true;
            return d.category == ItemCategory.Fish && d.habitats != null && d.habitats.Any(h => h == "sea" || h == "trop");
        }

        public static int TotalKeepable(ItemDatabase items) => items ? items.items.Count(Keepable) : 0;

        // ------------------------------------------------------------------ mo dam
        void Update()
        {
            if (!Open && (_check -= Time.deltaTime) <= 0)
            {
                _check = 2;
                TryOpen();
            }
            AnimateFish();
        }

        /// <summary>Du 3 san ho da cay: dung rao quanh san ho gan xom nhat.</summary>
        public bool TryOpen()
        {
            var dive = DivingSystem.Instance; var d = D;
            if (Open || !dive || d == null || dive.Corals.Count < CoralsNeeded) return false;
            int best = -1; float bd = float.MaxValue;
            foreach (int k in dive.Corals)
            {
                float dx = k % d.N - d.CX, dz = k / d.N - d.CZ, dist = dx * dx + dz * dz;
                if (dist < bd) { bd = dist; best = k; }
            }
            SetCenter(best);
            if (S && S.Active) S.Say("O Mận: \"Rạn chỗ con cấy san hô sống lại rồi. Cô rào đăng tre làm đầm nuôi, con thả cá vô mà ngắm (E ở đầm).\"");
            return true;
        }

        void SetCenter(int k)
        {
            CenterK = k;
            BuildPen();
            RebuildLife();
        }

        public Vector3 Center
        {
            get { var d = D; return d == null || !Open ? Vector3.zero : new Vector3(CenterK % d.N, d.WaterLevel, CenterK / d.N); }
        }

        // ------------------------------------------------------------------ phim E
        public Action NearbyAction(Vector3 pos, out string label)
        {
            label = null;
            if (!Open || !S) return null;
            var c = Center;
            if (Vector2.Distance(new Vector2(pos.x, pos.z), new Vector2(c.x, c.z)) > Reach) return null;
            int n = Releasable().Count;
            label = n > 0 ? $"E: Thả {n} loài mới vào đầm" : $"E: Ngắm đầm nuôi ({Count} loài)";
            return Visit;
        }

        /// <summary>Do trong tui tha duoc ma dam chua co (moi loai 1 con) + manh san ho trang tri.</summary>
        public List<ItemDefinition> Releasable()
        {
            var s = S;
            var l = new List<ItemDefinition>();
            if (!s) return l;
            foreach (var id in s.Inventory.Order)
            {
                var d = s.Items.Get(id);
                if (Keepable(d) && !Has(id) && s.Inventory.Count(id) > 0) l.Add(d);
            }
            return l;
        }

        public void Visit()
        {
            var s = S;
            var news = Release();
            int total = TotalKeepable(s.Items);
            string text;
            if (news.Count > 0)
                text = $"Đã thả: {string.Join(", ", news.Select(x => x.displayName.ToLowerInvariant()))}.\nĐầm giờ có {Count}/{total} loài.";
            else
                text = Count == 0
                    ? "Đầm còn trống. Câu cá biển hoặc lặn nhặt ốc, sò, nhum rồi mang ra đây thả (mỗi loài một con)."
                    : $"Đầm có {Count}/{total} loài. Cá nước ngọt không sống được ở đầm nước mặn.";
            int next = Array.Find(Goals, g => g > Count);
            if (next > 0) text += $"\nĐủ {next} loài sẽ có quà của O Mận.";
            if (s.Inventory.Has("manh_san_ho") && CoralDecor < MaxCoralDecor)
                text += "\nCó mảnh san hô trong túi: thả vào làm vườn san hô cho đầm.";
            var buttons = new List<(string, Action)>();
            if (s.Inventory.Has("manh_san_ho") && CoralDecor < MaxCoralDecor) buttons.Add(("Thả mảnh san hô", () => AddCoralDecor()));
            buttons.Add(("Ngắm thêm chút", null));
            if (ActionUI.Instance) ActionUI.Instance.ShowDialog("Đầm nuôi rạn san hô", text, buttons.ToArray());
        }

        /// <summary>Tha moi loai moi 1 con. Tra ve cac loai vua tha.</summary>
        public List<ItemDefinition> Release()
        {
            var s = S;
            var list = Releasable();
            int before = Count;
            foreach (var d in list)
            {
                if (!s.Inventory.Take(d.id)) continue;
                _species.Add(d.id);
                AddSwimmer(d);
                s.Stats.Calm = Mathf.Min(100, s.Stats.Calm + 2);
                Journal.Stat("pond");
            }
            for (int g = 0; g < Goals.Length; g++)
            {
                if (before >= Goals[g] || Count < Goals[g]) continue;
                s.Coins += GoalCoins[g];
                if (s.Restoration) s.Restoration.Add(CenterK, 5, "đầm nuôi");
                s.Say($"Đầm nuôi đủ {Goals[g]} loài! O Mận gửi {GoalCoins[g]} xu.");
            }
            return list;
        }

        public bool AddCoralDecor()
        {
            var s = S;
            if (CoralDecor >= MaxCoralDecor || !s.Inventory.Take("manh_san_ho")) return false;
            CoralDecor++;
            BuildCoral(CoralDecor - 1);
            s.Stats.Calm = Mathf.Min(100, s.Stats.Calm + 1);
            return true;
        }

        // ------------------------------------------------------------------ hinh ve
        void BuildPen()
        {
            if (_pen) Destroy(_pen);
            var d = D;
            if (d == null || !Open) return;
            _pen = new GameObject("ReefPond");
            _pen.transform.SetParent(transform, false);
            var c = new Vector3(CenterK % d.N, 0, CenterK / d.N);
            _pen.transform.position = c;
            var b = new MeshBuilder();
            const int posts = 18;
            float top = IslandData.SEA + TideClock.SpringAmp + .55f;   // tren muc nuoc lon nhat
            for (int i = 0; i < posts; i++)
            {
                float a = i * Mathf.PI * 2 / posts;
                float x = Mathf.Cos(a) * Radius, z = Mathf.Sin(a) * Radius;
                float floor = d.HeightAt(c.x + x, c.z + z) - .1f;
                float h = Mathf.Max(.4f, top - floor);
                b.Box("#b8985a", x, floor + h / 2, z, .07f, h, .07f);
                // day thung noi cot (2 tang)
                float a2 = (i + 1) * Mathf.PI * 2 / posts;
                float x2 = Mathf.Cos(a2) * Radius, z2 = Mathf.Sin(a2) * Radius, len = Vector2.Distance(new Vector2(x, z), new Vector2(x2, z2));
                float ry = -Mathf.Atan2(z2 - z, x2 - x);
                b.Box("#c8b080", (x + x2) / 2, top - .08f, (z + z2) / 2, len, .025f, .025f, 0, ry, 0);
                b.Box("#c8b080", (x + x2) / 2, top - .3f, (z + z2) / 2, len, .025f, .025f, 0, ry, 0);
            }
            // bang go nho tren cot phia xom
            b.Box("#8a5a2e", Radius + .05f, top + .12f, 0, .05f, .26f, .5f);
            b.Box("#e8d8a8", Radius + .08f, top + .12f, 0, .02f, .18f, .4f);
            var go = new GameObject("pen");
            go.transform.SetParent(_pen.transform, false);
            go.AddComponent<MeshFilter>().sharedMesh = b.ToMesh("ReefPondPen");
            go.AddComponent<MeshRenderer>().sharedMaterial = MaterialLibrary.Objects(false);
        }

        void RebuildLife()
        {
            foreach (var sw in _swim) if (sw.t) Destroy(sw.t.gameObject);
            _swim.Clear();
            foreach (var g in _decor) if (g) Destroy(g);
            _decor.Clear();
            if (!Open || !S) return;
            foreach (var id in _species) { var d = S.Items.Get(id); if (d) AddSwimmer(d); }
            for (int i = 0; i < CoralDecor; i++) BuildCoral(i);
        }

        void BuildCoral(int i)
        {
            var d = D;
            if (d == null || !_pen) return;
            float a = i * 2.4f + .7f, r = .7f + (i % 3) * .55f;
            float x = Mathf.Cos(a) * r, z = Mathf.Sin(a) * r;
            var c = _pen.transform.position;
            var go = new GameObject("PondCoral_" + i);
            go.transform.SetParent(_pen.transform, false);
            go.transform.position = new Vector3(c.x + x, d.HeightAt(c.x + x, c.z + z), c.z + z);
            go.transform.rotation = Quaternion.Euler(0, i * 67, 0);
            go.transform.localScale = Vector3.one * (.8f + (i % 2) * .3f);
            go.AddComponent<MeshFilter>().sharedMesh = SeabedMeshes.Coral;
            go.AddComponent<MeshRenderer>().sharedMaterial = MaterialLibrary.Objects(false);
            _decor.Add(go);
        }

        void AddSwimmer(ItemDefinition item)
        {
            if (!_pen) return;
            var d = D;
            bool critter = SeaCritters.Contains(item.id) && item.id != "tom_su";
            var go = new GameObject("Pond_" + item.id);
            go.transform.SetParent(_pen.transform, false);
            int h = item.id.GetHashCode();
            float phase = (h & 1023) / 1023f * Mathf.PI * 2;
            if (critter)
            {
                // oc, so, nhum, sao bien: nam duoi day
                float r = .5f + ((h >> 10) & 255) / 255f * (Radius - .9f);
                var c = _pen.transform.position;
                float x = Mathf.Cos(phase) * r, z = Mathf.Sin(phase) * r;
                go.transform.position = new Vector3(c.x + x, d.HeightAt(c.x + x, c.z + z), c.z + z);
                go.AddComponent<MeshFilter>().sharedMesh = SeabedMeshes.For(item.id);
                go.AddComponent<MeshRenderer>().sharedMaterial = MaterialLibrary.Objects(false);
                return;
            }
            go.AddComponent<MeshFilter>().sharedMesh = FishMesh(item);
            go.AddComponent<MeshRenderer>().sharedMaterial = MaterialLibrary.Objects(false);
            float size = Mathf.Clamp(1f + item.fishDifficulty * .15f, 1f, 1.7f);   // to de nhin qua mat nuoc
            go.transform.localScale = Vector3.one * size;
            _swim.Add(new Swimmer
            {
                t = go.transform, phase = phase,
                r = .6f + ((h >> 10) & 255) / 255f * (Radius - 1.1f),
                speed = (.35f + ((h >> 18) & 63) / 63f * .5f) * (((h >> 24) & 1) == 0 ? 1 : -1),
                y = .07f + ((h >> 4) & 63) / 63f * .12f, bob = phase * 1.7f,   // sat mat nuoc
            });
        }

        void AnimateFish()
        {
            var d = D;
            if (d == null || !_pen || _swim.Count == 0) return;
            var c = _pen.transform.position;
            float t = Time.time;
            foreach (var sw in _swim)
            {
                if (!sw.t) continue;
                float a = sw.phase + t * sw.speed / sw.r;
                float x = c.x + Mathf.Cos(a) * sw.r, z = c.z + Mathf.Sin(a) * sw.r;
                float floor = d.HeightAt(x, z) + .12f;
                float y = Mathf.Max(floor, d.WaterLevel - sw.y + Mathf.Sin(t * 1.3f + sw.bob) * .05f);
                sw.t.position = new Vector3(x, y, z);
                // huong boi: tiep tuyen voi vong tron
                var dir = new Vector3(-Mathf.Sin(a), 0, Mathf.Cos(a)) * Mathf.Sign(sw.speed);
                sw.t.rotation = Quaternion.LookRotation(dir) * Quaternion.Euler(0, Mathf.Sin(t * 6 + sw.bob) * 10, 0);
            }
        }

        static readonly Dictionary<string, Mesh> _fishMeshes = new Dictionary<string, Mesh>();

        /// <summary>Ca low-poly: than thoi 2 mau (lung / bung), duoi, vay lung. Huong +z.</summary>
        static Mesh FishMesh(ItemDefinition item)
        {
            if (_fishMeshes.TryGetValue(item.id, out var m) && m) return m;
            var b = new MeshBuilder();
            string back = "#" + ColorUtility.ToHtmlStringRGB(item.iconColor), belly = "#" + ColorUtility.ToHtmlStringRGB(item.iconColor2);
            bool flat = item.icon == IconShape.TropicalFish || item.icon == IconShape.Puffer;   // ca ran: than dep, cao
            float hh = flat ? .2f : .12f, w = flat ? .06f : .09f, len = flat ? .26f : .36f;
            b.Box(back, 0, hh * .25f, 0, w, hh * .55f, len);
            b.Box(belly, 0, -hh * .2f, .01f, w * .9f, hh * .45f, len * .9f);
            b.Box(belly, 0, 0, len * .5f, w * .7f, hh * .6f, .06f);                  // dau
            b.Box(back, 0, 0, -len * .58f, .02f, hh * 1.1f, .1f, .5f, 0, 0);        // duoi
            b.Box(back, 0, hh * .55f, -.02f, .015f, hh * .45f, len * .45f, -.3f, 0, 0); // vay lung
            b.Box("#1a1a1a", w * .52f, hh * .1f, len * .5f, .01f, .03f, .03f);    // mat
            b.Box("#1a1a1a", -w * .52f, hh * .1f, len * .5f, .01f, .03f, .03f);
            m = b.ToMesh("PondFish_" + item.id);
            _fishMeshes[item.id] = m;
            return m;
        }

        // ------------------------------------------------------------------ luu / tai
        public List<string> Snapshot() => new List<string>(_species);

        public void Restore(int center, List<string> species, int coralDecor)
        {
            _species.Clear();
            if (species != null) foreach (var id in species) if (!string.IsNullOrEmpty(id) && !_species.Contains(id)) _species.Add(id);
            CoralDecor = Mathf.Clamp(coralDecor, 0, MaxCoralDecor);
            if (_pen) { Destroy(_pen); _pen = null; }
            _swim.Clear(); _decor.Clear();
            CenterK = -1;
            if (center >= 0) SetCenter(center);
        }
    }
}
