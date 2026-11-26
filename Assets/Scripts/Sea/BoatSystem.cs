// Ghe thung (Docs/ThietKeGameplay.md muc 7.2, 7.3): muon ghe cua Bac Chin o ben.
//  - E canh ghe: len ghe; cheo tren moi mat nuoc (ca cho sau khong biet lan); E canh bo: len bo.
//  - Ban dem tren ghe thap den: muc keo lai gan (cau muc dem - Fishing.PickFish nhan SquidBoost).
//  - Sang ngay moi ghe tu ve ben.
using System;
using UnityEngine;

namespace SereneIsle
{
    public class BoatSystem : MonoBehaviour
    {
        public float paddleSpeed = 3f;

        public static BoatSystem Instance { get; private set; }
        public bool OnBoat { get; private set; }
        public Transform Boat => Village.Instance ? Village.Instance.Boat : null;
        /// <summary>Den tren ghe dang sang (dem + dang tren ghe).</summary>
        public bool LampOn => OnBoat && S && S.Clock.IsNight;

        Vector3 _home; Quaternion _homeRot; bool _homeSet;
        float _walk, _time;
        Light _lamp;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { Instance = null; }

        GameSession S => GameSession.Instance;
        static IslandData Data => IslandTerrain.Instance ? IslandTerrain.Instance.Data : null;

        void Awake() { Instance = this; }
        void Start() { if (S) S.DayStarted += OnDayStarted; }
        void OnDestroy() { if (S) S.DayStarted -= OnDayStarted; if (Instance == this) Instance = null; }

        void OnDayStarted(int day)
        {
            if (OnBoat) Leave(null);
            ReturnHome();
        }

        void ReturnHome()
        {
            var b = Boat;
            if (!b || !_homeSet) return;
            b.SetPositionAndRotation(_home, _homeRot);
        }

        void Update()
        {
            var b = Boat; var d = Data;
            if (!b || d == null) return;
            if (!_homeSet) { _home = b.position; _homeRot = b.rotation; _homeSet = true; }
            if (!OnBoat)
            {
                // ghe neo: noi theo con nuoc
                var p = b.position; p.y = d.WaterLevel + Mathf.Sin(Time.time * 1.3f) * .02f; b.position = p;
            }
            UpdateLamp(b);
        }

        void UpdateLamp(Transform b)
        {
            bool on = LampOn;
            if (on && !_lamp)
            {
                var go = new GameObject("BoatLamp");
                go.transform.SetParent(b, false);
                go.transform.localPosition = new Vector3(.35f, .9f, 0);
                _lamp = go.AddComponent<Light>();
                _lamp.type = LightType.Point; _lamp.color = new Color(1f, .82f, .5f); _lamp.range = 6; _lamp.intensity = 1.4f;
                _lamp.shadows = LightShadows.None;
            }
            if (_lamp) _lamp.enabled = on;
        }

        // ---------------------------------------------------------------- phim E
        public Action NearbyAction(Vector3 p, out string label)
        {
            label = null;
            var b = Boat; var d = Data; var s = S;
            if (!b || d == null || !s) return null;
            if (OnBoat)
            {
                if (LandingSpot(out var land)) { label = "E: Lên bờ"; return () => Leave(land); }
                return null;
            }
            if (Vector2.Distance(new Vector2(p.x, p.z), new Vector2(b.position.x, b.position.z)) < 2f)
            {
                label = "E: Lên ghe thúng (mượn của Bác Chín)";
                return Board;
            }
            return null;
        }

        public void Board()
        {
            var s = S; var b = Boat;
            if (!s || !b || OnBoat) return;
            OnBoat = true;
            s.Player.Drive(new Vector3(b.position.x, Data.WaterLevel + .05f, b.position.z), s.Player.Facing, false, 0);
            if (s.Body) s.Body.HoldPose = "Sit_Floor_Idle";
            s.Say("Đã lên ghe. Dùng WASD để chèo, E sát bờ để lên bờ." + (s.Clock.IsNight ? " Đèn đã thắp: mực kéo lại gần." : ""));
        }

        /// <summary>Len bo tai o dat (null: dua ve leu, vd. sang ngay moi).</summary>
        public void Leave(Vector3? land)
        {
            var s = S;
            OnBoat = false;
            if (s && s.Body) s.Body.HoldPose = null;
            if (land.HasValue && s) s.Player.PlaceAt(land.Value, s.Player.Facing);
        }

        /// <summary>O dat di duoc trong tam 1.6 o phia truoc / hai ben.</summary>
        bool LandingSpot(out Vector3 spot)
        {
            spot = default;
            var s = S; var d = Data;
            var p = s.Player.transform.position;
            float f = s.Player.Facing;
            foreach (float da in new[] { 0f, .6f, -.6f, 1.2f, -1.2f })
                for (float r = .8f; r <= 1.8f; r += .5f)
                {
                    float x = p.x + Mathf.Sin(f + da) * r, z = p.z + Mathf.Cos(f + da) * r;
                    if (d.WaterDepth(x, z) > .05f) continue;
                    float h = d.GroundHeight(x, z);
                    if (!d.Walkable(x, z, h)) continue;
                    spot = new Vector3(x, h, z);
                    return true;
                }
            return false;
        }

        // ---------------------------------------------------------------- cheo ghe (goi tu DivingSystem.Handle)
        public bool Handle(PlayerController pc, float dt)
        {
            if (!OnBoat) return false;
            var d = Data; var b = Boat;
            if (d == null || !b) { OnBoat = false; return false; }
            _time += dt;
            var pos = pc.transform.position;
            float rot = pc.Facing;
            Vector2 input = pc.InputLocked ? Vector2.zero : GameInput.Move;
            float il = Mathf.Min(1, input.magnitude);
            bool moving = il > .1f;
            float x = pos.x, z = pos.z;
            if (moving)
            {
                float yaw = pc.cameraRig ? pc.cameraRig.Yaw : 0f;
                float fx = -Mathf.Sin(yaw), fz = -Mathf.Cos(yaw), rx = -Mathf.Cos(yaw), rz = Mathf.Sin(yaw);
                float mx = fx * input.y + rx * input.x, mz = fz * input.y + rz * input.x, ml = Mathf.Sqrt(mx * mx + mz * mz);
                mx /= ml; mz /= ml;
                float nx = x + mx * paddleSpeed * il * dt, nz = z + mz * paddleSpeed * il * dt;
                if (nx > 1 && nz > 1 && nx < d.N - 2 && nz < d.N - 2 && d.WaterDepth(nx, nz) > .15f) { x = nx; z = nz; }
                float target = Mathf.Atan2(mx, mz), dr = Mathf.Atan2(Mathf.Sin(target - rot), Mathf.Cos(target - rot));
                rot += dr * Mathf.Min(1, dt * 5f);
                _walk += dt * 3f;
            }
            float bob = Mathf.Sin(_time * 1.6f) * .03f;
            float y = d.WaterLevel + bob;
            b.SetPositionAndRotation(new Vector3(x, y, z), Quaternion.Euler(Mathf.Sin(_time * 1.1f) * 3f, rot * Mathf.Rad2Deg, Mathf.Sin(_time * 1.4f) * 3f));
            pc.Drive(new Vector3(x, y + .05f, z), rot, false, 0);   // ngoi trong ghe (khong phat hoat anh di bo)
            if (S && S.Body) S.Body.Pose(dt, new PoseInput { walkPhase = _walk, amp = 0, time = _time });
            return true;
        }

        // ---------------------------------------------------------------- cau muc dem
        /// <summary>He so cau muc: tren ghe ban dem co den x4; dem khong trang (mung 1) x1.5, trang tron (ram) x0.5.</summary>
        public static float SquidBoost(ItemDefinition fish, bool lamp, double moonAge)
        {
            if (!fish || !IsSquid(fish.id)) return 1f;
            float moon = (float)(1.5 - LunarCalendar.MoonIllumination(moonAge));   // tron 0.5, non 1.5
            return (lamp ? 4f : 1f) * moon;
        }

        public static bool IsSquid(string id) => id == "f_squid" || id == "f_muc_la" || id == "f_muc_nang";
    }
}
