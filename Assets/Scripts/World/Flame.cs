// Lua (ban web: flame / updateFire dong 1039-1070): nhieu luoi lua hinh giot nuoc (vang -> cam -> do),
// loi sang cong, tan lua bay, khoi, than hong va quang sang tren mat dat. Ban nho (small) cho duoc.
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace SereneIsle
{
    public sealed class Flame
    {
        public readonly Transform root;
        readonly float _s;

        sealed class Tongue { public Transform t; public float w, h, ph, f1, f2; }
        sealed class Particle { public Transform t; public Renderer r; public float time, life, vx, vz, vy; }

        readonly List<Tongue> _tongues = new List<Tongue>();
        readonly List<Particle> _embers = new List<Particle>();
        readonly List<Particle> _smoke = new List<Particle>();
        readonly Renderer _bed;
        readonly Transform _glow;
        readonly MaterialPropertyBlock _mpb = new MaterialPropertyBlock();

        static Mesh _tongueMesh, _coreMesh, _cube, _ball, _quad;
        static Material _outer, _core, _ember, _bedMat, _glowMat, _smokeMat;

        static readonly int BaseColor = Shader.PropertyToID("_BaseColor");

        public Flame(Transform parent, float s, bool small)
        {
            Init();
            _s = s;
            var g = new GameObject("flame");
            root = g.transform;
            root.SetParent(parent, false);

            int n = small ? 3 : 6;
            for (int k = 0; k < n; k++)
            {
                float a = k / (float)n * 6.28f + Random.value, r = k == 0 ? 0 : (.05f + Random.value * .06f) * s;
                var t = Part("tongue", _tongueMesh, _outer, new Vector3(Mathf.Cos(a) * r, 0, Mathf.Sin(a) * r));
                float w = (k == 0 ? 1.25f : .7f + Random.value * .35f) * s, h = (k == 0 ? 1.35f : .7f + Random.value * .5f) * s;
                t.localScale = new Vector3(w, h, w);
                _tongues.Add(new Tongue { t = t, w = w, h = h, ph = Random.value * 9, f1 = 7 + Random.value * 5, f2 = 13 + Random.value * 7 });
            }
            var core = Part("core", _coreMesh, _core, Vector3.zero);
            core.localScale = new Vector3(.55f * s, .7f * s, .55f * s);
            _tongues.Add(new Tongue { t = core, w = .55f * s, h = .7f * s, ph = 1, f1 = 11, f2 = 19 });

            if (!small)
            {
                var bed = Part("bed", _ball, _bedMat, Vector3.zero);
                bed.localScale = new Vector3(.2f * s, .035f * s, .2f * s);
                _bed = bed.GetComponent<Renderer>();
                _glow = Part("glow", _quad, _glowMat, new Vector3(0, -.03f, 0));
                _glow.localScale = Vector3.one * 1.9f * s;
                for (int k = 0; k < 5; k++)
                {
                    var m = Part("smoke", _ball, _smokeMat, Vector3.zero);
                    m.localScale = Vector3.zero;
                    _smoke.Add(new Particle { t = m, r = m.GetComponent<Renderer>(), time = Random.value * 3, life = 2.6f + Random.value });
                }
            }
            for (int k = 0; k < (small ? 4 : 12); k++)
            {
                var m = Part("ember", _cube, _ember, Vector3.zero);
                m.localScale = Vector3.zero;
                _embers.Add(new Particle { t = m, time = Random.value * 1.5f, life = 1, vy = 1 });
            }
        }

        Transform Part(string name, Mesh mesh, Material mat, Vector3 pos)
        {
            var go = new GameObject(name);
            go.transform.SetParent(root, false);
            go.transform.localPosition = pos;
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = mat;
            mr.shadowCastingMode = ShadowCastingMode.Off;
            mr.receiveShadows = false;
            return go.transform;
        }

        public void Update(float dt, float T)
        {
            float s = _s;
            foreach (var t in _tongues)
            {
                float n = Mathf.Sin(T * t.f1 + t.ph) * .5f + Mathf.Sin(T * t.f2 + t.ph * 2.3f) * .5f;
                t.t.localScale = new Vector3(t.w * (1 + n * .12f), t.h * (1 + n * .3f + Mathf.Sin(T * 27 + t.ph) * .06f), t.w * (1 - n * .1f));
                t.t.localRotation = ChibiCharacter.EulerXYZ(Mathf.Cos(T * 2.6f + t.ph * 1.7f) * .16f, 0, Mathf.Sin(T * 3.1f + t.ph) * .2f);
            }
            if (_bed)
            {
                float q = .5f + .5f * Mathf.Sin(T * 3.3f);
                _mpb.SetColor(BaseColor, new Color(1, .35f + .15f * q, .1f));
                _bed.SetPropertyBlock(_mpb);
                _glow.localScale = Vector3.one * (1.8f + Mathf.Sin(T * 9) * .08f + Mathf.Sin(T * 23) * .05f) * s;
            }
            foreach (var e in _embers)
            {
                e.time += dt;
                if (e.time > e.life)
                {
                    e.time = 0; e.life = .7f + Random.value * 1.1f;
                    float a = Random.value * 6.28f, r = Random.value * .1f * s;
                    e.t.localPosition = new Vector3(Mathf.Cos(a) * r, .12f * s, Mathf.Sin(a) * r);
                    e.vx = (Random.value - .5f) * .35f; e.vz = (Random.value - .5f) * .35f; e.vy = (.7f + Random.value * .9f) * s;
                }
                float k = e.time / e.life;
                e.t.localPosition += new Vector3((e.vx + Mathf.Sin(T * 4 + e.life * 9) * .25f) * dt, e.vy * dt, e.vz * dt);
                e.t.localScale = Vector3.one * (.028f * s * (1 - k));
                e.t.Rotate(0, dt * 5 * Mathf.Rad2Deg, 0, Space.Self);
            }
            foreach (var m in _smoke)
            {
                m.time += dt;
                if (m.time > m.life) { m.time = 0; m.t.localPosition = new Vector3((Random.value - .5f) * .1f, .5f * s, (Random.value - .5f) * .1f); }
                float k = m.time / m.life;
                m.t.localPosition += new Vector3(dt * (.12f + Mathf.Sin(T + m.life) * .08f), dt * .55f, 0);
                m.t.localScale = Vector3.one * ((.08f + k * .32f) * s);
                var c = new Color(.54f, .52f, .5f, .32f * Mathf.Sin(Mathf.PI * Mathf.Min(1, k * 1.2f)) * (1 - k));
                _mpb.SetColor(BaseColor, c);
                m.r.SetPropertyBlock(_mpb);
            }
        }

        // ---------------------------------------------------------------- tai nguyen dung chung
        static void Init()
        {
            if (_tongueMesh) return;
            var prof = new[] { new Vector2(0, 0), new Vector2(.07f, .03f), new Vector2(.1f, .1f), new Vector2(.096f, .18f), new Vector2(.072f, .28f), new Vector2(.038f, .38f), new Vector2(0, .47f) };
            for (int i = 0; i < prof.Length; i++) prof[i].x = Mathf.Max(.001f, prof[i].x);
            var lathe = LowPolyShapes.Lathe(prof, 9);
            _tongueMesh = Object.Instantiate(lathe);
            _tongueMesh.name = "FlameTongue";
            Color A = MaterialLibrary.Hex("#fff6b0"), B = MaterialLibrary.Hex("#ffa62a"), R = MaterialLibrary.Hex("#e5441a");
            var v = _tongueMesh.vertices;
            var cols = new Color[v.Length];
            for (int i = 0; i < v.Length; i++)
            {
                float y = v[i].y / .47f;
                cols[i] = Color.Lerp(Color.Lerp(A, B, Mathf.Min(1, y * 1.8f)), R, Mathf.Max(0, (y - .5f) / .5f));
            }
            _tongueMesh.colors = cols;
            _coreMesh = White(lathe, "FlameCore");
            _cube = White(ProceduralMeshes.Primitive(PrimitiveType.Cube), "EmberCube");
            _ball = White(LowPolyShapes.Sphere86, "FireBall");
            _quad = GlowQuad();

            var sh = Shader.Find("SereneIsle/Glow");
            Material M(string name, Color c, bool add, bool radial = false)
            {
                var m = new Material(sh) { name = name, renderQueue = 3000 };
                m.SetColor(BaseColor, c);
                m.SetFloat("_SrcBlend", add ? (float)BlendMode.One : (float)BlendMode.SrcAlpha);
                m.SetFloat("_DstBlend", add ? (float)BlendMode.One : (float)BlendMode.OneMinusSrcAlpha);
                m.SetFloat("_Radial", radial ? 1 : 0);
                return m;
            }
            _outer = M("SI_FireOuter", new Color(1, 1, 1, .9f), false);
            _core = M("SI_FireCore", WithA(MaterialLibrary.Hex("#fff2a0"), .85f), true);
            _ember = M("SI_Ember", MaterialLibrary.Hex("#ffb040"), true);
            _bedMat = M("SI_FireBed", MaterialLibrary.Hex("#ff6a20"), false);
            _glowMat = M("SI_FireGlow", WithA(new Color(1f, .67f, .27f), .85f), true, true);
            _smokeMat = M("SI_Smoke", new Color(.54f, .52f, .5f, 0), false);
            _outer.renderQueue = 3001; _core.renderQueue = 3002;
        }

        static Color WithA(Color c, float a) { c.a = a; return c; }

        static Mesh White(Mesh src, string name)
        {
            var m = Object.Instantiate(src);
            m.name = name;
            var c = new Color[m.vertexCount];
            for (int i = 0; i < c.Length; i++) c[i] = Color.white;
            m.colors = c;
            return m;
        }

        /// <summary>Tam vuong nam ngang, UV 0..1 (shader ve quang tron).</summary>
        static Mesh GlowQuad()
        {
            var m = new Mesh { name = "FireGlowQuad" };
            m.vertices = new[] { new Vector3(-.5f, 0, -.5f), new Vector3(.5f, 0, -.5f), new Vector3(-.5f, 0, .5f), new Vector3(.5f, 0, .5f) };
            m.uv = new[] { new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 1), new Vector2(1, 1) };
            m.colors = new[] { Color.white, Color.white, Color.white, Color.white };
            m.triangles = new[] { 0, 2, 1, 1, 2, 3 };
            m.RecalculateNormals();
            m.RecalculateBounds();
            return m;
        }
    }
}
