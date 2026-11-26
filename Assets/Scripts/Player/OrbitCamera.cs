// Camera quay quanh nhan vat (ban web: camYaw / camPitch / camDist).
// Vi tri: P + (sin yaw, 0, cos yaw) * cos(pitch) * dist, cao = max(P.y + 1.1 + sin(pitch)*dist, dat + .5, SEA + .4).
using UnityEngine;

namespace SereneIsle
{
    [RequireComponent(typeof(Camera))]
    public class OrbitCamera : MonoBehaviour
    {
        public Transform target;

        [Header("Goc nhin (radian, giong ban web)")]
        public float yaw = Mathf.PI * .15f;
        /// <summary>Dang lan bien (DivingSystem): cho camera xuong duoi mat nuoc.</summary>
        public static bool AllowUnderwater;
        [Range(.06f, 1.35f)] public float pitch = .55f;
        [Range(3.5f, 24f)] public float distance = 9f;
        public float lookHeight = 1.1f;

        [Header("Do nhay")]
        [Tooltip("rad/pixel khi keo chuot phai/giua (web: .006 x sens/50)")]
        public float dragSensitivity = .006f;
        public float stickSensitivity = 2.6f;   // rad/giay
        public float keyRotateSpeed = 1.8f;     // Z / C
        public Vector2 distanceRange = new Vector2(3.5f, 24f);
        [Tooltip("Ban web: lan chuot doi o hotbar, can giu Ctrl/Alt de zoom")]
        public bool zoomNeedsModifier = true;

        bool _dragFromUi;

        public float Yaw => yaw;

        IslandData Data => IslandTerrain.Instance ? IslandTerrain.Instance.Data : null;

        void Reset()
        {
            var cam = GetComponent<Camera>();
            cam.fieldOfView = 42; cam.nearClipPlane = .1f; cam.farClipPlane = 320;
        }

        public void SnapBehind(float facingRad, float newPitch = .42f, float newDist = 9f)
        {
            yaw = facingRad + Mathf.PI;
            pitch = newPitch;
            distance = newDist;
            LateUpdate();
        }

        void LateUpdate()
        {
            if (!target) return;
            float dt = Mathf.Min(.05f, Time.unscaledDeltaTime);

            // Luu y: Unity dung he toa do tay trai -> chieu yaw nguoc ban web de keo phai = nhin sang phai nhu cu.
            // bat dau keo tren giao dien (vd chuot phai xoa o hotbar) thi khong xoay
            if (GameInput.OrbitPressed) _dragFromUi = GameHud.PointerOverUI;
            if (GameInput.OrbitHeld && !_dragFromUi && !GameHud.BlocksCamera)
            {
                Vector2 d = GameInput.MouseDelta;
                yaw += d.x * dragSensitivity;
                pitch -= d.y * dragSensitivity * .85f;
            }
            Vector2 st = GameInput.LookStick;
            yaw += st.x * stickSensitivity * dt;
            pitch -= st.y * stickSensitivity * .85f * dt;
            yaw -= GameInput.CameraRotateKeys * keyRotateSpeed * dt;
            pitch = Mathf.Clamp(pitch, .06f, 1.35f);

            // ban web: lan chuot = doi o hotbar; Ctrl/Alt + lan = zoom
            float steps = GameInput.ScrollSteps;
            if (steps != 0 && (GameInput.ZoomModifier || !zoomNeedsModifier)) distance -= steps;
            float zk = GameInput.ZoomKeySteps;
            if (zk != 0) distance -= zk * 1.5f;
            distance = Mathf.Clamp(distance, distanceRange.x, distanceRange.y);

            Vector3 p = target.position;
            float cp = Mathf.Cos(pitch) * distance;
            float cx = p.x + Mathf.Sin(yaw) * cp, cz = p.z + Mathf.Cos(yaw) * cp;
            float cy = p.y + lookHeight + Mathf.Sin(pitch) * distance;
            var data = Data;
            float lookY = p.y + lookHeight;
            if (data != null)
            {
                if (AllowUnderwater)
                {
                    // dang lan: camera ha xuong duoi mat nuoc, sat sau lung nguoi boi (khong chui xuong day)
                    lookY = p.y + .25f;
                    cy = Mathf.Max(data.HeightAt(cx, cz) + .3f, Mathf.Min(cy, data.WaterLevel - .12f));
                }
                else cy = Mathf.Max(cy, data.HeightAt(cx, cz) + .5f, data.WaterLevel + .4f);
            }

            transform.position = new Vector3(cx, cy, cz);
            transform.LookAt(new Vector3(p.x, lookY, p.z));

            // ban web FADE: uCam, uPl = nhan vat + .8, uR = 2.2 -> vat che nhan vat thua dan
            Shader.SetGlobalVector(idCam, transform.position);
            Shader.SetGlobalVector(idPlayer, new Vector4(p.x, p.y + .8f, p.z, 1));
            Shader.SetGlobalFloat(idFadeR, occlusionFadeRadius);
        }

        [Tooltip("Ban kinh vung lam mo vat che nhan vat (ban web uR = 2.2)")]
        public float occlusionFadeRadius = 2.2f;
        static readonly int idCam = Shader.PropertyToID("_SI_Cam"), idPlayer = Shader.PropertyToID("_SI_Player"), idFadeR = Shader.PropertyToID("_SI_FadeR");
    }
}
