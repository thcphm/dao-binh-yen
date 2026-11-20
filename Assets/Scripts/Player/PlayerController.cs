// Di chuyen nhan vat tren luoi do cao (port vong lap "player" trong frame() cua ban web).
// Khong dung CharacterController / Rigidbody: va cham theo luoi o giong ban goc (walkable + moveEntity),
// nen hanh vi (khong xuong bien, loi nuoc ngot nong, khong leo bac > .42, nhay qua vat thap) giu nguyen.
using UnityEngine;

namespace SereneIsle
{
    [RequireComponent(typeof(IslandCharacter))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Toc do (o/giay, giong ban web)")]
        public float walkSpeed = 3.6f;
        public float runSpeed = 5.6f;
        [Tooltip("He so toc do khi loi nuoc")] public float wadeFactor = .55f;
        public float turnRate = 14f;

        [Header("Nhay")]
        public float jumpVelocity = 7.4f;
        public float gravity = 22f;
        [Tooltip("Thoi gian ngoi lay da truoc khi bat len")] public float jumpPrep = .1f;
        public float radius = .28f;

        public OrbitCamera cameraRig;

        public bool Moving { get; private set; }
        public bool Sprinting { get; private set; }
        public bool Grounded => _ground;
        public float Speed => _speed;
        /// <summary>Huong mat (radian, 0 = +z).</summary>
        public float Facing => _rot;
        /// <summary>Khoa dieu khien (mo tui do, chuyen ngay...) - ban web: running && !uiOpen.</summary>
        public bool InputLocked { get; set; }
        /// <summary>He so toc do khi het nang luong (ban web: x0.6).</summary>
        public float SpeedMultiplier { get; set; } = 1f;

        IslandCharacter _body;
        float _x, _y, _z, _vy, _rot, _prep, _land, _landAmt, _walk, _speed, _time;
        bool _ground = true, _placed;

        IslandData Data => IslandTerrain.Instance ? IslandTerrain.Instance.Data : null;

        void Awake()
        {
            _body = GetComponent<IslandCharacter>();
        }

        /// <summary>Dat nhan vat len dao (ban web: arriveStart).</summary>
        public void PlaceAt(Vector3 pos, float facingRad)
        {
            _x = pos.x; _z = pos.z;
            var d = Data;
            _y = d != null ? d.GroundHeight(_x, _z) : pos.y;
            _vy = 0; _ground = true; _rot = facingRad;
            _placed = true;
            SyncTransform();
            if (cameraRig) cameraRig.SnapBehind(facingRad);
        }

        /// <summary>Quay mat (radian) - vd. quay lung ve camera khi quang can.</summary>
        public void Face(float facingRad) { _rot = facingRad; SyncTransform(); }

        public void PlaceAtArrival()
        {
            var d = Data;
            if (d == null) return;
            var p = d.ArrivalPoint(out float face);
            PlaceAt(p, face);
        }

        void Update()
        {
            var data = Data;
            if (data == null) return;
            if (!_placed) PlaceAtArrival();

            float dt = Mathf.Min(.05f, Time.deltaTime);
            _time += dt;

            // ---- di chuyen theo huong camera ----
            Vector2 input = InputLocked ? Vector2.zero : GameInput.Move;
            float ix = input.x, iz = input.y, il = Mathf.Sqrt(ix * ix + iz * iz);
            Moving = il > .1f;
            if (Moving)
            {
                float yaw = cameraRig ? cameraRig.Yaw : 0f;
                // toi = tu camera nhin ve nhan vat; phai = phai man hinh cua Unity (he tay trai)
                float fx = -Mathf.Sin(yaw), fz = -Mathf.Cos(yaw), rx = -Mathf.Cos(yaw), rz = Mathf.Sin(yaw);
                float mx = fx * iz + rx * ix, mz = fz * iz + rz * ix, ml = Mathf.Sqrt(mx * mx + mz * mz);
                mx /= ml; mz /= ml;
                Sprinting = GameInput.Run;
                float wd = data.TileHeight(_x, _z) < IslandData.SEA ? wadeFactor : 1f;
                float sp = (Sprinting ? runSpeed : walkSpeed) * Mathf.Min(1, il) * wd * SpeedMultiplier;
                data.MoveEntity(ref _x, ref _z, mx * sp * dt, mz * sp * dt, radius, _y);
                float target = Mathf.Atan2(mx, mz), dRot = Mathf.Atan2(Mathf.Sin(target - _rot), Mathf.Cos(target - _rot));
                _rot += dRot * Mathf.Min(1, dt * turnRate);
                _walk += dt * sp * 2.1f;
                _speed = sp;
            }
            else Sprinting = false;

            // ---- nhay ----
            if (!InputLocked && GameInput.JumpPressed && _ground && !(_prep > 0) && !(_land > .12f)) _prep = jumpPrep;
            if (_prep > 0)
            {
                _prep -= dt;
                if (_prep <= 0) { _prep = 0; _vy = jumpVelocity; _ground = false; _y += .02f; }
            }
            if (_land > 0) _land -= dt;

            // ---- trong luc + bam dat ----
            float gy = data.GroundHeight(_x, _z);
            _vy -= gravity * dt;
            _y += _vy * dt;
            if (_y <= gy)
            {
                if (!_ground && _vy < -3) { _land = .24f; _landAmt = Mathf.Min(1, .35f - _vy / 12f); }
                _y = gy; _vy = 0; _ground = true;
            }
            else if (_ground && _vy <= 0 && _y - gy < .3f) { _y = gy; _vy = 0; } // xuong doc: bam dat
            else _ground = false;

            SyncTransform();

            _body.Pose(dt, new PoseInput
            {
                walkPhase = _walk,
                amp = Moving ? Mathf.Min(1.6f, _speed / walkSpeed) : 0,
                air = !_ground,
                vy = _vy,
                prep = _prep > 0 ? 1 - _prep / jumpPrep : 0,
                land = _land > 0 ? Mathf.Sin(_land / .24f * Mathf.PI) * _landAmt : 0,
                time = _time,
                speed = Moving ? _speed : 0,
            });
        }

        void SyncTransform()
        {
            transform.SetPositionAndRotation(new Vector3(_x, _y, _z), Quaternion.Euler(0, _rot * Mathf.Rad2Deg, 0));
        }
    }
}
