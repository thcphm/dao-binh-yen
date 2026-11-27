// Cay dau dong (Docs/ThietKeGameplay.md muc 4.4): vung dat suc song tu 75 moc 1 cay an trai dau dong (buoc day do).
// Dung truoc cay bam E de cat canh ghep (moi 7 ngay 1 lan), mang ve ghep len cay trong tu cay giong.
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SereneIsle
{
    public class MotherTrees : MonoBehaviour
    {
        public static MotherTrees Instance { get; private set; }

        public const int CutEveryDays = 7;
        const float Range = 1.9f;

        public sealed class Tree { public int region, k; public string crop; public GameObject go; }

        readonly Dictionary<int, Tree> _trees = new Dictionary<int, Tree>();   // id vung -> cay
        readonly Dictionary<int, int> _cut = new Dictionary<int, int>();       // id vung -> ngay cat canh gan nhat
        float _check;

        GameSession S => GameSession.Instance;
        static IslandData D => IslandTerrain.Instance ? IslandTerrain.Instance.Data : null;
        public IEnumerable<Tree> Trees => _trees.Values;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { Instance = null; }

        void Awake() { Instance = this; }
        void OnDestroy() { if (Instance == this) Instance = null; }

        void Update()
        {
            if ((_check -= Time.deltaTime) > 0) return;
            _check = 3;
            Refresh();
        }

        /// <summary>Moc cay dau dong o cac vung vua du suc song.</summary>
        public void Refresh()
        {
            var s = S; var d = D;
            if (!s || !s.Restoration || d == null || !IslandTerrain.Instance.IsReady) return;
            s.Restoration.VitalityAt(Vector3.zero);   // dam bao da tao mo hinh vung
            var m = s.Restoration.Model;
            if (m == null) return;
            foreach (var r in m.Regions)
                if (r.vitality >= FarmCare.MotherVitality && !_trees.ContainsKey(r.id)) Spawn(m, r, d);
        }

        static List<CropDefinition> TreeCrops()
        {
            var l = new List<CropDefinition>();
            foreach (var c in CropDatabase.Instance.crops) if (c && c.kind == CropKind.Tree) l.Add(c);
            l.Sort((a, b) => string.CompareOrdinal(a.id, b.id));
            return l;
        }

        void Spawn(RestorationModel m, RestorationModel.Region r, IslandData d)
        {
            int k = FindSpot(m, r, d);
            var crops = TreeCrops();
            if (k < 0 || crops.Count == 0) { _trees[r.id] = new Tree { region = r.id, k = -1 }; return; }
            var c = crops[(int)((uint)(r.id * 5 + d.Seed) % (uint)crops.Count)];
            var go = new GameObject($"MotherTree_{r.id}_{c.id}");
            go.transform.SetParent(transform, false);
            go.transform.position = new Vector3(k % d.N, d.HT[k], k / d.N);
            go.transform.localScale = Vector3.one * 1.35f;
            var body = new GameObject("tree");
            body.transform.SetParent(go.transform, false);
            body.AddComponent<MeshFilter>().sharedMesh = CropMeshes.Get(c, 3, 0);
            body.AddComponent<MeshRenderer>().sharedMaterial = MaterialLibrary.Objects(false);
            var b = new MeshBuilder();   // day do buoc quanh goc: cay dau dong
            b.Box("#d8402a", 0, .32f, 0, .2f, .05f, .2f);
            b.Box("#d8402a", .1f, .26f, .08f, .03f, .14f, .05f, .3f, 0, 0);
            var rib = new GameObject("ribbon");
            rib.transform.SetParent(go.transform, false);
            rib.AddComponent<MeshFilter>().sharedMesh = b.ToMesh("MotherRibbon");
            rib.AddComponent<MeshRenderer>().sharedMaterial = MaterialLibrary.Objects(false);
            d.Solid[k] = 1;
            var veg = IslandTerrain.Instance.Vegetation;
            if (veg) veg.HideGrass(k);
            _trees[r.id] = new Tree { region = r.id, k = k, crop = c.id, go = go };
            if (S && S.Active) S.Say($"{r.name}: một cây {c.displayName.ToLowerInvariant()} đầu dòng đã mọc. Ra cắt cành ghép nhé (E).");
        }

        static int FindSpot(RestorationModel m, RestorationModel.Region r, IslandData d)
        {
            int cn = m.CellsPerSide, cell = RestorationModel.Cell;
            int start = (int)((uint)(r.id * 7919 + d.Seed) % (uint)Math.Max(1, r.cells.Count));
            for (int t = 0; t < r.cells.Count; t++)
            {
                int c = r.cells[(start + t) % r.cells.Count];
                int ci = (c % cn) * cell - IslandData.M + cell / 2, cj = (c / cn) * cell - IslandData.M + cell / 2;
                for (int rad = 0; rad < cell / 2; rad++)
                    for (int dj = -rad; dj <= rad; dj++)
                        for (int di = -rad; di <= rad; di++)
                        {
                            if (Math.Max(Math.Abs(di), Math.Abs(dj)) != rad) continue;
                            int i = ci + di, j = cj + dj;
                            if (!d.InBounds(i, j)) continue;
                            int k = d.Idx(i, j);
                            if (Ok(d, k)) return k;
                        }
            }
            return -1;
        }

        static bool Ok(IslandData d, int k)
        {
            if (d.Level[k] == 0 || d.Solid[k] != 0 || d.Pier[k] != 0 || d.Fresh[k] != 0 || d.Bio[k] == Biome.Town) return false;
            if (d.HT[k] < TideClock.MaxLevel + .15f) return false;
            float dx = k % d.N - d.CX, dz = k / d.N - d.CZ;
            if (dx * dx + dz * dz < 26 * 26) return false;   // khong moc giua xom chai
            for (int dj = -1; dj <= 1; dj++)                   // chua cho dung quanh goc
                for (int di = -1; di <= 1; di++)
                {
                    int i = k % d.N + di, j = k / d.N + dj;
                    if (!d.InBounds(i, j) || d.Solid[d.Idx(i, j)] != 0 || d.Level[d.Idx(i, j)] == 0) return false;
                }
            var s = GameSession.Instance;
            if (s && s.Farm && s.Farm.PlotAt(k) != null) return false;
            if (Placeables.Instance && Placeables.Instance.At(k) != null) return false;
            return true;
        }

        // ------------------------------------------------------------------ phim E
        public Action NearbyAction(Vector3 pos, out string label)
        {
            label = null;
            var s = S; var d = D;
            if (!s || d == null) return null;
            foreach (var t in _trees.Values)
            {
                if (t.k < 0) continue;
                var at = new Vector2(t.k % d.N, t.k / d.N);
                if (Vector2.Distance(at, new Vector2(pos.x, pos.z)) > Range) continue;
                var c = CropDatabase.Instance.Get(t.crop);
                string name = c ? c.displayName.ToLowerInvariant() : t.crop;
                int wait = DaysLeft(t.region, s.Clock.Day);
                if (wait > 0) { label = $"Cây {name} đầu dòng · {wait} ngày nữa cắt cành được"; return () => { }; }
                label = $"E: Cắt cành ghép {name} đầu dòng";
                var tree = t;
                return () => Cut(tree);
            }
            return null;
        }

        public int DaysLeft(int region, int day)
            => _cut.TryGetValue(region, out var last) ? Math.Max(0, last + CutEveryDays - day) : 0;

        public bool Cut(Tree t)
        {
            var s = S;
            if (!s || t.k < 0 || DaysLeft(t.region, s.Clock.Day) > 0) return false;
            if (s.Body) s.Body.PlayAct("seed");
            _cut[t.region] = s.Clock.Day;
            int got = s.Give(FarmCare.ScionPrefix + t.crop, 2, true);
            s.Say($"Cắt được 2 {s.Items.NameOf(FarmCare.ScionPrefix + t.crop).ToLowerInvariant()}" + (got < 2 ? " (túi đầy, để lại dưới đất)." : ". Cầm cành, đứng trước cây ăn trái bấm chuột để ghép."));
            return true;
        }

        // ------------------------------------------------------------------ luu / tai
        public List<ItemCount> Snapshot()
        {
            var l = new List<ItemCount>();
            foreach (var kv in _cut) l.Add(new ItemCount { id = kv.Key.ToString(), n = kv.Value });
            return l;
        }

        public void Restore(List<ItemCount> list)
        {
            _cut.Clear();
            if (list != null) foreach (var c in list) if (int.TryParse(c.id, out int r)) _cut[r] = c.n;
        }
    }
}
