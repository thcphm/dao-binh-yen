// Dan lang di dao quanh nha (ban web: cap nhat npcs dong 1899-1902): dung 2-6 giay, chon diem quanh nha (+-ban kinh),
// di 1.4 o/giay; bi chan thi bo. Nguoi dung yen (ong Bay) quay mat ve nguoi choi khi lai gan < 5 o.
using UnityEngine;

namespace SereneIsle
{
    [RequireComponent(typeof(IslandCharacter))]
    public class Npc : MonoBehaviour
    {
        public NpcDef def;
        public Vector3 home;
        public float homeFacing;
        public const float WalkSpeed = 1.4f;

        public IslandCharacter Body { get; private set; }
        /// <summary>Dang noi chuyen: dung lai, quay ve nguoi choi.</summary>
        public bool Talking { get; set; }
        public float Facing => _rot;

        float _rot, _wait = 1, _walkPhase;
        Vector3 _target;
        bool _hasTarget;
        System.Random _rng;

        static IslandData D => IslandTerrain.Instance ? IslandTerrain.Instance.Data : null;
        GameSession S => GameSession.Instance;

        void Awake()
        {
            Body = GetComponent<IslandCharacter>();
            _rng = new System.Random(name.GetHashCode() ^ System.Environment.TickCount);
        }

        public void PlaceHome()
        {
            var d = D;
            float y = d != null ? d.GroundHeight(home.x, home.z) : home.y;
            transform.position = new Vector3(home.x, y, home.z);
            _rot = homeFacing;
            transform.rotation = Quaternion.Euler(0, _rot * Mathf.Rad2Deg, 0);
            _hasTarget = false;
            _wait = 1 + (float)_rng.NextDouble() * 3;
        }

        void Update()
        {
            var d = D;
            if (d == null || Body == null) return;
            float dt = Time.deltaTime;
            var p = transform.position;
            float speed = 0;
            Vector3? look = null;
            var pl = S && S.Player ? S.Player.transform.position : (Vector3?)null;
            float toPlayer = pl.HasValue ? Flat(p, pl.Value) : 99;

            if (Talking || (def != null && def.wander <= 0 && toPlayer < 5)) look = pl;
            else if (def != null && def.wander > 0)
            {
                if (!_hasTarget)
                {
                    _wait -= dt;
                    if (_wait <= 0)
                    {
                        float a = (float)_rng.NextDouble() * Mathf.PI * 2, r = (float)_rng.NextDouble() * def.wander;
                        _target = home + new Vector3(Mathf.Sin(a) * r, 0, Mathf.Cos(a) * r);
                        _hasTarget = true;
                    }
                }
                else
                {
                    var to = _target - p; to.y = 0;
                    float dist = to.magnitude;
                    if (dist < .15f) { _hasTarget = false; _wait = 2 + (float)_rng.NextDouble() * 4; }
                    else if (toPlayer < .9f) { /* nhuong duong nguoi choi */ }
                    else
                    {
                        var dir = to / dist;
                        float step = Mathf.Min(dist, WalkSpeed * dt);
                        var next = p + dir * step;
                        if (d.Walkable(next.x, next.z, p.y) && d.WaterDepth(next.x, next.z) <= 0)
                        {
                            p = next;
                            speed = WalkSpeed;
                            _rot = Mathf.LerpAngle(_rot * Mathf.Rad2Deg, Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg, Mathf.Min(1, dt * 10)) * Mathf.Deg2Rad;
                        }
                        else { _hasTarget = false; _wait = 1; }
                    }
                }
            }
            else if (def != null && def.wander <= 0)
                _rot = Mathf.LerpAngle(_rot * Mathf.Rad2Deg, homeFacing * Mathf.Rad2Deg, Mathf.Min(1, dt * 3)) * Mathf.Deg2Rad;

            if (look.HasValue)
            {
                var to = look.Value - p;
                if (to.sqrMagnitude > .01f)
                    _rot = Mathf.LerpAngle(_rot * Mathf.Rad2Deg, Mathf.Atan2(to.x, to.z) * Mathf.Rad2Deg, Mathf.Min(1, dt * 6)) * Mathf.Deg2Rad;
            }

            p.y = d.GroundHeight(p.x, p.z);
            transform.position = p;
            transform.rotation = Quaternion.Euler(0, _rot * Mathf.Rad2Deg, 0);
            _walkPhase += dt * speed * 2.1f;
            Body.Pose(dt, new PoseInput { walkPhase = _walkPhase, amp = speed > 0 ? 1 : 0, speed = speed, time = Time.time });
        }

        static float Flat(Vector3 a, Vector3 b) => Vector2.Distance(new Vector2(a.x, a.z), new Vector2(b.x, b.z));
    }
}
