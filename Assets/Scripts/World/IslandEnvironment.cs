// Troi, suong mu, mat troi, den ban cau theo gio trong ngay (ban web: DAYK + applyDay).
// Day cac gia tri sRGB vao shader SereneIsle/* qua bien toan cuc de mau giong ban web.
using UnityEngine;
using UnityEngine.Rendering;

namespace SereneIsle
{
    public class IslandEnvironment : MonoBehaviour
    {
        [Header("Thoi gian")]
        [Tooltip("Gio hien tai (ban web bat dau 14:00).")]
        [Range(5f, 26.9f)] public float hour = 14f;
        [Tooltip("Cho dong ho chay (ban web: 2 phut game / giay).")]
        public bool advanceTime = true;
        public float gameMinutesPerSecond = 2f;
        [Tooltip("Ngay chay 7:00 -> 23:00 roi quay lai 7:00 (chua co ngu / ngat).")]
        public Vector2 dayRange = new Vector2(7f, 23f);

        [Header("Suong mu / den")]
        public float fogNear = 45f;
        public float fogFar = 150f;
        public Color hemiGround = new Color(0x8a / 255f, 0x6a / 255f, 0x3a / 255f);
        [Tooltip("Mat troi bam theo nhan vat (de bong do o gan).")]
        public Transform followTarget;

        public Light Sun { get; private set; }

        // [gio, mau troi, mau nang, cuong do nang, cuong do ambient]
        struct Key { public float h; public Color sky, sun; public float sunI, ambI; }
        static Key K(float h, string sky, string sun, float si, float ai)
            => new Key { h = h, sky = MaterialLibrary.Hex(sky), sun = MaterialLibrary.Hex(sun), sunI = si, ambI = ai };
        static readonly Key[] DAYK =
        {
            K(6, "#f4ad78", "#ffb37a", .55f, .48f), K(8, "#8fd3ea", "#fff2d6", .85f, .5f), K(12, "#78c6ec", "#ffffff", .92f, .54f),
            K(16, "#8ccfe6", "#fff0c8", .85f, .52f), K(18, "#f29a62", "#ff9a55", .75f, .52f), K(19.5f, "#7a4f86", "#ff7a5a", .35f, .4f),
            K(21, "#1d2747", "#9fb2ff", .3f, .32f), K(27, "#121a33", "#9fb2ff", .26f, .3f),
        };

        static readonly int idSky = Shader.PropertyToID("_SI_HemiSky"), idGround = Shader.PropertyToID("_SI_HemiGround"),
                            idFogC = Shader.PropertyToID("_SI_FogColor"), idFogP = Shader.PropertyToID("_SI_FogParams"),
                            idSkyTop = Shader.PropertyToID("_SI_SkyTop"), idSunDisc = Shader.PropertyToID("_SI_SunDisc");

        void Awake()
        {
            Sun = FindSun();
            if (!Sun)
            {
                var go = new GameObject("Sun");
                go.transform.SetParent(transform, false);
                Sun = go.AddComponent<Light>();
                Sun.type = LightType.Directional;
            }
            Sun.shadows = LightShadows.Soft;
            Sun.shadowBias = .02f;
            Sun.shadowNormalBias = .3f;
            RenderSettings.sun = Sun;
            RenderSettings.fog = false; // suong mu tu tinh trong shader (smoothstep nhu three.js)
            Apply();
        }

        static Light FindSun()
        {
            if (RenderSettings.sun) return RenderSettings.sun;
            foreach (var l in FindObjectsByType<Light>())
                if (l.type == LightType.Directional) return l;
            return null;
        }

        void Update()
        {
            if (advanceTime)
            {
                hour += Time.deltaTime * gameMinutesPerSecond / 60f;
                if (hour >= dayRange.y) hour = dayRange.x;
            }
            Apply();
        }

        void OnValidate()
        {
            if (Application.isPlaying && Sun) Apply();
        }

        /// <summary>Gui mau sRGB nguyen ban toi GPU: trong Linear color space Unity doi mau den gamma->linear, nen dao truoc.</summary>
        static Color RawForLight(Color srgb) => QualitySettings.activeColorSpace == ColorSpace.Linear ? srgb.gamma : srgb;

        public void Apply()
        {
            float h = Mathf.Min(26.9f, hour);
            Key a = DAYK[0], b = DAYK[DAYK.Length - 1];
            for (int k = 0; k < DAYK.Length - 1; k++)
                if (h >= DAYK[k].h && h < DAYK[k + 1].h) { a = DAYK[k]; b = DAYK[k + 1]; break; }
            float t = (h - a.h) / (b.h - a.h == 0 ? 1 : b.h - a.h);
            Color sky = Color.LerpUnclamped(a.sky, b.sky, t);
            Color sunC = Color.LerpUnclamped(a.sun, b.sun, t);
            float sunI = a.sunI + (b.sunI - a.sunI) * t, ambI = a.ambI + (b.ambI - a.ambI) * t;
            Color hemiSky = Color.Lerp(sky, Color.white, .5f);

            // huong mat troi
            Vector3 d = h < 19.6f
                ? new Vector3(Mathf.Cos((h - 5.5f) / 14f * Mathf.PI) * .8f, Mathf.Max(.3f, Mathf.Sin((h - 5.5f) / 14f * Mathf.PI)), .45f)
                : new Vector3(-.35f, .85f, .35f);
            d.Normalize();

            if (Sun)
            {
                Sun.color = RawForLight(sunC);
                Sun.intensity = sunI;
                Sun.transform.rotation = Quaternion.LookRotation(-d);
                if (followTarget) Sun.transform.position = followTarget.position + d * 50f;
            }

            Shader.SetGlobalVector(idSky, new Vector4(hemiSky.r * ambI, hemiSky.g * ambI, hemiSky.b * ambI, 1));
            Shader.SetGlobalVector(idGround, new Vector4(hemiGround.r * ambI, hemiGround.g * ambI, hemiGround.b * ambI, 1));
            Shader.SetGlobalVector(idFogC, new Vector4(sky.r, sky.g, sky.b, 1));
            Shader.SetGlobalVector(idFogP, new Vector4(fogNear, fogFar, 1, 0));
            // vom troi: dinh troi dam hon chan troi; dia mat troi tat sau 19h36 (mat troi da lan)
            var top = new Color(sky.r * .62f, sky.g * .78f, Mathf.Min(1, sky.b * 1.02f));
            Shader.SetGlobalVector(idSkyTop, new Vector4(top.r, top.g, top.b, 1));
            float sunUp = h < 19.6f ? Mathf.Clamp01((h - 5.6f) / .6f) : 0;
            Shader.SetGlobalVector(idSunDisc, new Vector4(Mathf.Lerp(sunC.r, 1, .5f), Mathf.Lerp(sunC.g, 1, .5f), Mathf.Lerp(sunC.b, 1, .5f), sunUp));

            // cho shader khac (URP Lit...) cung co ambient tuong tu
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = hemiSky * ambI;
            RenderSettings.ambientEquatorColor = Color.Lerp(hemiSky, hemiGround, .5f) * ambI;
            RenderSettings.ambientGroundColor = hemiGround * ambI;

            var cam = Camera.main;
            if (cam)
            {
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = sky;
            }
        }
    }
}
