// Suc song vung trong game (Docs/ThietKeGameplay.md muc 5):
//  - rac dat bo moi sang (nhieu hon o vung kiet que), di qua la nhat: +1 suc song vung do;
//  - trong cay an trai song qua 3 ngay +3, chat cay -1 (goi tu FarmSystem / ToolSystem);
//  - vuot nguong 25/50/75/100 thi bao + cong binh yen; dia hinh vung kiet que phai mau (shader _Desaturate);
//  - doi vung thi hien ten vung + suc song.
using System.Collections.Generic;
using UnityEngine;

namespace SereneIsle
{
    public class RestorationSystem : MonoBehaviour
    {
        public int maxDebris = 40;
        public float debrisRadius = 60f;

        public RestorationModel Model { get; private set; }
        /// <summary>Vung nguoi choi dang dung (null: ngoai bien).</summary>
        public RestorationModel.Region Current { get; private set; }

        readonly List<GameObject> _debris = new List<GameObject>();
        readonly Dictionary<int, Renderer> _chunks = new Dictionary<int, Renderer>();
        MaterialPropertyBlock _mpb;
        float[] _pending;
        bool _tintDirty = true, _spawnedFirst;
        static readonly int IdDesat = Shader.PropertyToID("_Desaturate");
        static Mesh _bottle, _plank, _net;

        GameSession S => GameSession.Instance;

        void Start()
        {
            if (S) S.DayStarted += OnDayStarted;
        }

        void OnDestroy()
        {
            if (S) S.DayStarted -= OnDayStarted;
        }

        bool EnsureModel()
        {
            if (Model != null) return true;
            var t = IslandTerrain.Instance;
            if (!t || !t.IsReady) return false;
            Model = new RestorationModel(t.Data);
            if (_pending != null) { Model.Restore(_pending); _pending = null; }
            FindChunks(t);
            _tintDirty = true;
            return true;
        }

        void FindChunks(IslandTerrain t)
        {
            var root = t.transform.Find("TerrainChunks");
            if (!root) return;
            foreach (Transform ch in root)
            {
                var r = ch.GetComponent<Renderer>();
                if (!r) continue;
                var p = ch.localPosition;
                int u = Mathf.RoundToInt(p.x + IslandData.M) / RestorationModel.Cell, v = Mathf.RoundToInt(p.z + IslandData.M) / RestorationModel.Cell;
                _chunks[v * Model.CellsPerSide + u] = r;
            }
        }

        void Update()
        {
            var s = S;
            if (!s || !EnsureModel()) return;
            if (_tintDirty) ApplyTint();
            if (!s.Running || !s.Player) return;
            if (!_spawnedFirst) { _spawnedFirst = true; SpawnDebris(); }

            var pos = s.Player.transform.position;
            var reg = Model.RegionAt(pos.x, pos.z);
            Current = reg;   // HUD hien ten vung + suc song

            if (!s.Active) return;
            for (int n = _debris.Count - 1; n >= 0; n--)
            {
                var go = _debris[n];
                if (!go) { _debris.RemoveAt(n); continue; }
                var p = go.transform.position;
                if (Mathf.Abs(p.x - pos.x) > 1f || Mathf.Abs(p.z - pos.z) > 1f) continue;
                if (Vector2.Distance(new Vector2(p.x, p.z), new Vector2(pos.x, pos.z)) > .9f) continue;
                _debris.RemoveAt(n);
                Destroy(go);
                PickDebris(p);
            }
        }

        // ---------------------------------------------------------------- API cho he thong khac
        public float VitalityAt(Vector3 p) => EnsureModel() ? Model.RegionAt(p.x, p.z)?.vitality ?? 0 : 0;

        public int RegrowFactor(int k)
        {
            if (!EnsureModel()) return 1;
            var d = IslandTerrain.Instance.Data;
            var r = Model.RegionAt(k % d.N, k / d.N);
            return r != null ? RestorationModel.RegrowFactor(r.vitality) : 1;
        }

        /// <summary>Cong / tru suc song o vung chua o k. reason: hien len khi cong.</summary>
        public void Add(int k, float delta, string reason = null)
        {
            if (!EnsureModel()) return;
            var d = IslandTerrain.Instance.Data;
            var r = Model.RegionAt(k % d.N, k / d.N);
            if (r == null) return;
            int crossed = Model.Add(r, delta);
            _tintDirty = true;
            if (reason != null && delta > 0) S.Say($"+{delta:0} sức sống {r.name} ({reason})");
            if (crossed > 0) Celebrate(r, crossed);
        }

        void Celebrate(RestorationModel.Region r, int t)
        {
            string msg = t switch
            {
                25 => $"{r.name}: cỏ hoa đã mọc lại.",
                50 => $"{r.name}: chim, bướm, đom đóm đã quay về.",
                75 => $"{r.name} đủ sức sống để đón người dân mới.",
                _ => $"{r.name} đã hồi sinh hoàn toàn!",
            };
            S.Say(msg);
            S.Stats.Calm = Mathf.Min(100, S.Stats.Calm + 5);
        }

        // ---------------------------------------------------------------- rac dat bo
        void OnDayStarted(int day)
        {
            for (int n = _debris.Count - 1; n >= 0; n--)
            {
                var go = _debris[n];
                if (!go) { _debris.RemoveAt(n); continue; }
                if (S.Player && Vector3.Distance(go.transform.position, S.Player.transform.position) > debrisRadius * 2) { Destroy(go); _debris.RemoveAt(n); }
            }
            SpawnDebris();
        }

        void SpawnDebris()
        {
            var s = S;
            if (!s || !s.Player || !EnsureModel()) return;
            var d = IslandTerrain.Instance.Data;
            Vector3 c = s.Player.transform.position;
            int want = Mathf.Min(maxDebris - _debris.Count, 8 + Mathf.FloorToInt((100 - Model.IslandVitality) / 8));
            var rng = new System.Random(s.Clock.Day * 7919 + d.Seed);
            for (int t = 0; t < 6000 && want > 0; t++)
            {
                int i = Mathf.RoundToInt(c.x + (float)(rng.NextDouble() * 2 - 1) * debrisRadius);
                int j = Mathf.RoundToInt(c.z + (float)(rng.NextDouble() * 2 - 1) * debrisRadius);
                if (!d.InBounds(i, j)) continue;
                int k = d.Idx(i, j);
                if (d.Level[k] == 0 || d.Ocean[k] != 0 || d.Solid[k] != 0 || d.Pier[k] != 0 || d.Coast[k] < 2 || d.Coast[k] > 3) continue;
                if (d.HT[k] < IslandData.SEA + .06f) continue;
                var reg = Model.RegionAt(i, j);
                if (reg != null && rng.NextDouble() * 100 < reg.vitality) continue;   // vung da sach thi it rac
                _debris.Add(MakeDebris(new Vector3(i + (float)(rng.NextDouble() - .5) * .5f, d.HeightAt(i, j), j + (float)(rng.NextDouble() - .5) * .5f), rng));
                want--;
            }
        }

        GameObject MakeDebris(Vector3 pos, System.Random rng)
        {
            if (!_bottle)
            {
                var b = new MeshBuilder();
                b.Cylinder("#7fb8a0", 0, .1f, 0, .05f, .06f, .2f); b.Cylinder("#5a8a78", 0, .23f, 0, .025f, .03f, .06f);
                _bottle = b.ToMesh("Debris_Bottle");
                b = new MeshBuilder();
                b.Box("#9a8a70", 0, .03f, 0, .7f, .05f, .14f); b.Box("#7a6a50", .15f, .07f, .02f, .3f, .04f, .12f, 0, .4f, 0);
                _plank = b.ToMesh("Debris_Plank");
                b = new MeshBuilder();
                b.P(LowPolyShapes.Ico0, "#4f7a8a", 0, .07f, 0, .22f, .08f, .18f); b.P(LowPolyShapes.Ico0, "#d8a040", .12f, .05f, .05f, .06f, .05f, .06f);
                _net = b.ToMesh("Debris_Net");
            }
            var go = new GameObject("Debris");
            go.transform.SetParent(transform, false);
            double r = rng.NextDouble();
            var mesh = r < .4 ? _plank : r < .75 ? _bottle : _net;
            // chai nam nghieng tren cat
            go.transform.SetPositionAndRotation(pos + (mesh == _bottle ? Vector3.up * .06f : Vector3.zero),
                Quaternion.Euler(0, (float)rng.NextDouble() * 360, mesh == _bottle ? 80 : 0));
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial = MaterialLibrary.Objects(false);
            go.name = mesh == _plank ? "Debris_Plank" : mesh == _bottle ? "Debris_Bottle" : "Debris_Net";
            return go;
        }

        void PickDebris(Vector3 p)
        {
            var d = IslandTerrain.Instance.Data;
            int i = Mathf.RoundToInt(p.x), j = Mathf.RoundToInt(p.z);
            Add(d.Idx(Mathf.Clamp(i, 0, d.N - 1), Mathf.Clamp(j, 0, d.N - 1)), 1, "nhặt rác dạt bờ");
            if (Random.value < .25f) S.Give("wood", 1);
        }

        // ---------------------------------------------------------------- mau dia hinh
        void ApplyTint()
        {
            _tintDirty = false;
            _mpb ??= new MaterialPropertyBlock();
            foreach (var kv in _chunks)
            {
                if (!kv.Value) continue;
                var r = Model.RegionOfCell(kv.Key);
                kv.Value.GetPropertyBlock(_mpb);
                _mpb.SetFloat(IdDesat, r != null ? RestorationModel.Desaturation(r.vitality) : 0);
                kv.Value.SetPropertyBlock(_mpb);
            }
        }

        // ---------------------------------------------------------------- luu / tai
        public float[] Snapshot() => Model?.Snapshot();

        public void Restore(float[] vitality)
        {
            if (Model != null) { Model.Restore(vitality); _tintDirty = true; }
            else _pending = vitality;
        }
    }
}
