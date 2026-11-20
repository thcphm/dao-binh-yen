// Dung mesh dia hinh tu IslandData (ban web: buildTerrain + water + floor).
// - Sinh du lieu + tinh dinh/mau/phap tuyen tren thread phu (Task), chi tao Mesh tren main thread.
// - Chia manh 64x64 o, moi manh 1 Mesh voi vertex color (sRGB, shader SereneIsle/WebLambert).
using System;
using System.Collections;
using System.Threading.Tasks;
using UnityEngine;

namespace SereneIsle
{
    public class IslandTerrain : MonoBehaviour
    {
        public static IslandTerrain Instance { get; private set; }

        [Header("Sinh dao")]
        [Tooltip("SEED giong ban web. <0 = ngau nhien moi lan chay.")]
        public int seed = -1;
        [Tooltip("Kich thuoc dao (o). Ban web: 800.")]
        [Range(200, 1000)] public int size = 800;

        [Header("Hien thi")]
        public bool castShadows = true;
        [Tooltip("Them MeshCollider cho tung manh (de raycast chuot / vat ly sau nay).")]
        public bool addColliders = false;
        public bool buildWater = true;

        public IslandData Data { get; private set; }
        public bool IsReady { get; private set; }
        public float Progress { get; private set; }
        public event Action<IslandData> Ready;

        sealed class ChunkData
        {
            public int u0, v0, nu, nv;
            public Vector3[] pos, nor;
            public Color[] col;
            public int[] tri;
        }

        void Awake()
        {
            Instance = this;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        IEnumerator Start()
        {
            int s = seed >= 0 ? seed : UnityEngine.Random.Range(0, 1000000);
            seed = s;
            var data = new IslandData(s, size);
            ChunkData[] chunks = null;
            float t0 = Time.realtimeSinceStartup;

            var task = Task.Run(() =>
            {
                data.Generate(p => Progress = p * .7f);
                chunks = BuildChunkData(data, p => Progress = .7f + p * .25f);
            });
            while (!task.IsCompleted) yield return null;
            if (task.IsFaulted)
            {
                Debug.LogException(task.Exception);
                yield break;
            }
            Data = data;

            // tao Mesh tren main thread, rai ra vai frame
            var mat = MaterialLibrary.Terrain;
            var root = new GameObject("TerrainChunks").transform;
            root.SetParent(transform, false);
            int n = 0;
            foreach (var c in chunks)
            {
                CreateChunk(c, root, mat);
                if (++n % 16 == 0) { Progress = .95f + .05f * n / chunks.Length; yield return null; }
            }
            if (buildWater) BuildWaterAndFloor(data);
            PierBuilder.Build(data, transform);
            TentBuilder.Build(data, transform, out var home, out var homeFacing);
            if (GameSession.Instance) GameSession.Instance.SetHome(home, homeFacing);

            Debug.Log($"[SereneIsle] Dao seed={s}, {size}x{size} o, {chunks.Length} manh, thi tran ({data.CX},{data.CZ}) huong {data.TDIR}, " +
                      $"{(Time.realtimeSinceStartup - t0):F2}s");
            Progress = 1;
            IsReady = true;
            Ready?.Invoke(data);
        }

        static ChunkData[] BuildChunkData(IslandData d, Action<float> progress)
        {
            int GS = d.GS, TCH = IslandData.CHUNK, TCN = Mathf.CeilToInt((GS - 1) / (float)TCH), M = IslandData.M;
            var FH = d.FH;
            var result = new ChunkData[TCN * TCN];
            int done = 0;
            Parallel.For(0, TCN * TCN, ci =>
            {
                int cu = ci % TCN, cv = ci / TCN;
                int u0 = cu * TCH, v0 = cv * TCH, u1 = Math.Min(GS - 1, u0 + TCH), v1 = Math.Min(GS - 1, v0 + TCH);
                int nu = u1 - u0, nv = v1 - v0, W = nu + 1, count = W * (nv + 1);
                var c = new ChunkData { u0 = u0, v0 = v0, nu = nu, nv = nv, pos = new Vector3[count], nor = new Vector3[count], col = new Color[count], tri = new int[nu * nv * 6] };
                for (int b = 0; b <= nv; b++)
                    for (int a = 0; a <= nu; a++)
                    {
                        int q = b * W + a, u = u0 + a, v = v0 + b, idx = v * GS + u;
                        float h = FH[idx];
                        // toa do local cua manh: goc manh o (u0-M, v0-M)
                        c.pos[q] = new Vector3(a, h, b);
                        float hx = FH[v * GS + Math.Min(GS - 1, u + 1)] - FH[v * GS + Math.Max(0, u - 1)];
                        float hz = FH[Math.Min(GS - 1, v + 1) * GS + u] - FH[Math.Max(0, v - 1) * GS + u];
                        float slope = Mathf.Sqrt(hx * hx + hz * hz) / 2f;
                        c.nor[q] = new Vector3(-hx, 2f, -hz).normalized;
                        c.col[q] = d.GroundColor(u - M, v - M, h, slope, idx);
                    }
                int t = 0;
                for (int b = 0; b < nv; b++)
                    for (int a = 0; a < nu; a++)
                    {
                        int q = b * W + a;
                        // chieu kim dong ho nhin tu tren xuong (mat truoc cua Unity)
                        c.tri[t++] = q; c.tri[t++] = q + W; c.tri[t++] = q + 1;
                        c.tri[t++] = q + 1; c.tri[t++] = q + W; c.tri[t++] = q + W + 1;
                    }
                result[ci] = c;
                int k = System.Threading.Interlocked.Increment(ref done);
                progress?.Invoke((float)k / result.Length);
            });
            return result;
        }

        void CreateChunk(ChunkData c, Transform parent, Material mat)
        {
            var mesh = new Mesh { name = $"Terrain_{c.u0}_{c.v0}" };
            mesh.vertices = c.pos;
            mesh.normals = c.nor;
            mesh.colors = c.col;
            mesh.triangles = c.tri;
            mesh.RecalculateBounds();

            var go = new GameObject(mesh.name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3(c.u0 - IslandData.M, 0, c.v0 - IslandData.M);
            go.isStatic = true;
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = mat;
            mr.shadowCastingMode = castShadows ? UnityEngine.Rendering.ShadowCastingMode.On : UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = true;
            if (addColliders) go.AddComponent<MeshCollider>().sharedMesh = mesh;
        }

        void BuildWaterAndFloor(IslandData d)
        {
            float N = d.N;
            // mat nuoc: PlaneGeometry(N+240, N+240, 150, 150) tai y = SEA
            var water = new GameObject("Water");
            water.transform.SetParent(transform, false);
            water.transform.localPosition = new Vector3(N / 2f, IslandData.SEA, N / 2f);
            water.AddComponent<MeshFilter>().sharedMesh = MakePlane(N + 240, 150, "WaterPlane");
            var wr = water.AddComponent<MeshRenderer>();
            wr.sharedMaterial = MaterialLibrary.NewWater();
            wr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            wr.receiveShadows = false;

            // day bien phang mau #1c6676 o y = -1.65
            var floor = new GameObject("SeaFloor");
            floor.transform.SetParent(transform, false);
            floor.transform.localPosition = new Vector3(N / 2f, -1.65f, N / 2f);
            floor.AddComponent<MeshFilter>().sharedMesh = MakePlane(N + 900, 1, "FloorPlane");
            var fr = floor.AddComponent<MeshRenderer>();
            fr.sharedMaterial = MaterialLibrary.Solid("#1c6676");
            fr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        static Mesh MakePlane(float size, int seg, string name)
        {
            int W = seg + 1;
            var v = new Vector3[W * W];
            var nrm = new Vector3[W * W];
            var tri = new int[seg * seg * 6];
            for (int j = 0; j <= seg; j++)
                for (int i = 0; i <= seg; i++)
                {
                    v[j * W + i] = new Vector3((i / (float)seg - .5f) * size, 0, (j / (float)seg - .5f) * size);
                    nrm[j * W + i] = Vector3.up;
                }
            int t = 0;
            for (int j = 0; j < seg; j++)
                for (int i = 0; i < seg; i++)
                {
                    int q = j * W + i;
                    tri[t++] = q; tri[t++] = q + W; tri[t++] = q + 1;
                    tri[t++] = q + 1; tri[t++] = q + W; tri[t++] = q + W + 1;
                }
            var m = new Mesh { name = name };
            if (v.Length > 65535) m.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            m.vertices = v; m.normals = nrm; m.triangles = tri;
            m.RecalculateBounds();
            // bien mat nuoc co song: noi rong bounds de khong bi cull sai
            var bnd = m.bounds; bnd.Expand(new Vector3(0, 1, 0)); m.bounds = bnd;
            return m;
        }

        void OnGUI()
        {
            if (IsReady) return;
            var r = new Rect(Screen.width / 2f - 160, Screen.height / 2f - 14, 320, 28);
            GUI.Box(r, $"Đang nặn núi đồi, đào sông hồ… {Progress * 100f:F0}%");
        }
    }
}
