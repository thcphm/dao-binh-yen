// Vom troi (theo camera) + may low-poly troi cham tren dao.
using System.Collections.Generic;
using UnityEngine;

namespace SereneIsle
{
    public class SkyRenderer : MonoBehaviour
    {
        [Header("May")]
        public int cloudCount = 34;
        public float cloudHeightMin = 48, cloudHeightMax = 70;
        public float windSpeed = 1.2f;
        [Range(0, 1)] public float cloudFog = .35f;

        Transform _dome;
        readonly List<Transform> _clouds = new List<Transform>();
        float _span, _x0;

        void Start()
        {
            // vom troi: cau mat trong, ban kinh 280 (< far clip 320), luon dat tai camera
            var shader = Shader.Find("SereneIsle/Sky");
            if (shader)
            {
                var dome = new GameObject("SkyDome");
                dome.transform.SetParent(transform, false);
                dome.transform.localScale = Vector3.one * 560f; // sphere Unity ban kinh .5 -> 280
                dome.AddComponent<MeshFilter>().sharedMesh = ProceduralMeshes.Sphere;
                var mr = dome.AddComponent<MeshRenderer>();
                mr.sharedMaterial = new Material(shader) { name = "SI_Sky" };
                mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                mr.receiveShadows = false;
                _dome = dome.transform;
            }
            var t = IslandTerrain.Instance;
            if (t && t.IsReady) BuildClouds(t.Data.N);
            else if (t) t.Ready += d => BuildClouds(d.N);
        }

        void BuildClouds(int n)
        {
            var mat = MaterialLibrary.Clouds(cloudFog);
            var rnd = new System.Random(12345);
            float R() => (float)rnd.NextDouble();
            _span = n + 400; _x0 = -200;
            for (int c = 0; c < cloudCount; c++)
            {
                var b = new MeshBuilder();
                int puffs = 4 + (int)(R() * 4);
                float len = 6 + R() * 10;
                for (int p = 0; p < puffs; p++)
                {
                    float px = (p / (float)(puffs - 1) - .5f) * len, r = 2.2f + R() * 2.6f * (1 - Mathf.Abs(px) / len);
                    b.Add(LowPolyShapes.Ico1, Matrix4x4.TRS(new Vector3(px, R() * 1.2f, (R() - .5f) * 3f), Quaternion.Euler(0, R() * 360, 0),
                        new Vector3(r * 1.3f, r * .7f, r)), Color.white);
                }
                var go = new GameObject("Cloud" + c);
                go.transform.SetParent(transform, false);
                go.transform.position = new Vector3(_x0 + R() * _span, cloudHeightMin + R() * (cloudHeightMax - cloudHeightMin), -200 + R() * (n + 400));
                go.transform.rotation = Quaternion.Euler(0, (R() - .5f) * 30, 0);
                go.AddComponent<MeshFilter>().sharedMesh = b.ToMesh("CloudMesh");
                var mr = go.AddComponent<MeshRenderer>();
                mr.sharedMaterial = mat;
                mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                mr.receiveShadows = false;
                _clouds.Add(go.transform);
            }
        }

        void LateUpdate()
        {
            var cam = Camera.main;
            if (_dome && cam) _dome.position = cam.transform.position;
            float dx = windSpeed * Time.deltaTime;
            foreach (var c in _clouds)
            {
                var p = c.position;
                p.x += dx;
                if (p.x > _x0 + _span) p.x -= _span;
                c.position = p;
            }
        }
    }
}
