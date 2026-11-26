// Mua (hat mua keo dai roi xien theo gio, di theo camera) + tia nuoc bat len mat dat quanh nguoi choi.
// Cuong do 0..1 do WeatherSystem dat (mua = .55, bao = 1).
using UnityEngine;

namespace SereneIsle
{
    public class RainFX : MonoBehaviour
    {
        [Range(0, 1)] public float intensity;
        public float maxDrops = 2600;

        ParticleSystem _rain, _splash;
        static Material _dropMat, _splashMat;

        public int ActiveDrops => _rain ? _rain.particleCount : 0;

        /// <summary>Xoa het hat mua dang roi (doi thoi tiet tuc thi: test, chuyen ngay).</summary>
        public void ClearNow() { if (_rain) _rain.Clear(true); if (_splash) _splash.Clear(true); }

        void Awake()
        {
            _rain = MakeSystem("Rain", true);
            _splash = MakeSystem("Splash", false);
        }

        ParticleSystem MakeSystem(string name, bool drops)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.loop = true;
            main.playOnAwake = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = drops ? 4000 : 600;
            var shape = ps.shape;
            shape.enabled = true;
            if (drops)
            {
                main.startLifetime = 1.1f;
                main.startSpeed = 0;
                main.startSize3D = true;
                main.startSizeX = .025f; main.startSizeY = .55f; main.startSizeZ = .025f;
                main.startColor = new Color(.78f, .86f, .95f, .42f);
                main.gravityModifier = 0;
                shape.shapeType = ParticleSystemShapeType.Box;
                shape.scale = new Vector3(40, 1, 40);
                var vel = ps.velocityOverLifetime;
                vel.enabled = true;
                vel.space = ParticleSystemSimulationSpace.World;
                vel.x = new ParticleSystem.MinMaxCurve(-2.5f);
                vel.y = new ParticleSystem.MinMaxCurve(-17f);
                vel.z = new ParticleSystem.MinMaxCurve(-1f);
            }
            else
            {
                main.startLifetime = .25f;
                main.startSpeed = new ParticleSystem.MinMaxCurve(.6f, 1.4f);
                main.startSize = new ParticleSystem.MinMaxCurve(.03f, .06f);
                main.startColor = new Color(.86f, .92f, 1f, .55f);
                main.gravityModifier = 1.2f;
                shape.shapeType = ParticleSystemShapeType.Hemisphere;
                shape.radius = .05f;
            }
            var em = ps.emission;
            em.rateOverTime = 0;
            var r = go.GetComponent<ParticleSystemRenderer>();
            r.renderMode = drops ? ParticleSystemRenderMode.Stretch : ParticleSystemRenderMode.Billboard;
            if (drops) { r.velocityScale = .045f; r.lengthScale = 1f; }
            r.sharedMaterial = drops ? DropMat : SplashMat;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            ps.Play();
            return ps;
        }

        static Material DropMat => _dropMat ? _dropMat : (_dropMat = Mat("SI_RainDrop"));
        static Material SplashMat => _splashMat ? _splashMat : (_splashMat = Mat("SI_RainSplash"));

        static Material Mat(string name)
        {
            var m = new Material(Shader.Find("SereneIsle/Glow")) { name = name, renderQueue = 3100 };
            m.SetColor("_BaseColor", Color.white);
            return m;
        }

        void LateUpdate()
        {
            var cam = Camera.main;
            if (!cam) return;
            // khoi mua tren dau camera, lech theo huong nhin cho hat mua nam trong khung
            var f = cam.transform.forward; f.y = 0;
            var center = cam.transform.position + f.normalized * 10 + Vector3.up * 11;
            _rain.transform.position = center;
            var em = _rain.emission;
            em.rateOverTime = maxDrops * intensity;
            // tia nuoc quanh nguoi choi
            var s = GameSession.Instance;
            var d = IslandTerrain.Instance ? IslandTerrain.Instance.Data : null;
            var sem = _splash.emission;
            sem.rateOverTime = 0;
            if (intensity > .05f && s && s.Player && d != null)
            {
                int n = Mathf.RoundToInt(intensity * 12);
                var pp = s.Player.transform.position;
                for (int i = 0; i < n; i++)
                {
                    float a = Random.value * Mathf.PI * 2, r = Random.value * 9;
                    var q = pp + new Vector3(Mathf.Sin(a) * r, 0, Mathf.Cos(a) * r);
                    q.y = Mathf.Max(d.GroundHeight(q.x, q.z), d.WaterLevel) + .02f;
                    var ep = new ParticleSystem.EmitParams { position = q, applyShapeToPosition = false };
                    _splash.Emit(ep, 1);
                }
            }
        }
    }
}
