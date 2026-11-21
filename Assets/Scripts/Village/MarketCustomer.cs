// Khach cho phien: di tu ria lang toi truoc sap, dung xem hang (Market.Serve), roi di ve va bien mat.
using UnityEngine;

namespace SereneIsle
{
    [RequireComponent(typeof(IslandCharacter))]
    public class MarketCustomer : MonoBehaviour
    {
        public enum Phase { Walk, Browse, Wait, Leave }

        public CustomerType Type { get; private set; }
        public Phase State { get; private set; }
        public Market.Outcome? Result { get; private set; }

        Vector3 _from, _spot, _target;
        float _t, _rot, _phase;
        IslandCharacter _body;
        const float Speed = 1.3f;

        static readonly string[] Shirts = { "#e46a4e", "#5b9bd5", "#f2c14e", "#7cb66b", "#ef8f7f", "#f4f1e8", "#8a6ab0", "#4f86a6" };
        static readonly string[] Pants = { "#2a2422", "#4a6a9c", "#6e7178", "#7a5a40", "#2f3640" };
        static readonly string[] Skins = { "#f8cfae", "#eab489", "#c98d63", "#b07a50" };
        static readonly string[] Hairs = { "#2a2422", "#4a2e22", "#d8d4cc", "#2a2422" };

        public static MarketCustomer Create(CustomerType type, Vector3 from, Vector3 spot, System.Random rng, Transform parent)
        {
            var go = new GameObject("Customer_" + type.id);
            go.SetActive(false);
            go.transform.SetParent(parent, false);
            var body = go.AddComponent<IslandCharacter>();
            bool female = rng.NextDouble() < .5;
            body.outfit = new CharacterOutfit
            {
                model = female ? "Rogue" : "Ranger",
                shirt = Shirts[rng.Next(Shirts.Length)], pants = Pants[rng.Next(Pants.Length)], shoe = "#5a3a22",
                skin = Skins[rng.Next(Skins.Length)], hair = type.id == "ba_cu" ? "#d8d4cc" : Hairs[rng.Next(Hairs.Length)],
                longSleeve = rng.NextDouble() < .5, shorts = type.id == "tre", noBelt = true,
            };
            if (type.id == "tre") body.height = 1.0f;
            var c = go.AddComponent<MarketCustomer>();
            c.Type = type;
            c._from = from; c._spot = spot; c._target = spot;
            go.SetActive(true);
            if (type.id == "ba_cu" || rng.NextDouble() < .35) body.AttachToHead(Village.HatMesh, MaterialLibrary.Objects(false));
            go.transform.position = from;
            var to = spot - from;
            c._rot = Mathf.Atan2(to.x, to.z);
            return c;
        }

        void Awake() { _body = GetComponent<IslandCharacter>(); }

        public void Say(string text) => ActionUI.Bubble(transform, text, 3f);

        public void Leave()
        {
            if (State == Phase.Leave) return;
            State = Phase.Leave;
            _target = _from;
        }

        void Update()
        {
            var m = Market.Instance;
            var s = GameSession.Instance;
            if (!m || !s) return;
            bool paused = !s.Active;          // hop thoai / tui do mo: dung lai
            float dt = paused ? 0 : Time.deltaTime;
            var p = transform.position;
            float speed = 0;

            switch (State)
            {
                case Phase.Walk:
                    if (MoveTo(ref p, _spot, dt)) { State = Phase.Browse; _t = 1.2f; }
                    else speed = Speed;
                    break;
                case Phase.Browse:
                    FaceStall(dt);
                    _t -= dt;
                    if (_t <= 0)
                    {
                        Result = m.Serve(this);
                        if (Result == Market.Outcome.Haggle) { State = Phase.Wait; _t = 12; }
                        else { State = Phase.Wait; _t = 1.6f; }
                    }
                    break;
                case Phase.Wait:
                    FaceStall(dt);
                    _t -= dt;
                    if (_t <= 0) Leave();
                    break;
                case Phase.Leave:
                    if (MoveTo(ref p, _target, dt)) { Destroy(gameObject); return; }
                    speed = Speed;
                    break;
            }
            var d = IslandTerrain.Instance ? IslandTerrain.Instance.Data : null;
            if (d != null) p.y = d.GroundHeight(p.x, p.z);
            transform.position = p;
            transform.rotation = Quaternion.Euler(0, _rot * Mathf.Rad2Deg, 0);
            _phase += dt * speed * 2.1f;
            _body.Pose(paused ? 0 : Time.deltaTime, new PoseInput { walkPhase = _phase, amp = speed > 0 ? 1 : 0, speed = speed, time = Time.time });
        }

        bool MoveTo(ref Vector3 p, Vector3 target, float dt)
        {
            var to = target - p; to.y = 0;
            float dist = to.magnitude;
            if (dist < .1f) return true;
            var dir = to / dist;
            p += dir * Mathf.Min(dist, Speed * dt);
            if (dt > 0) _rot = Mathf.LerpAngle(_rot * Mathf.Rad2Deg, Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg, Mathf.Min(1, dt * 8)) * Mathf.Deg2Rad;
            return false;
        }

        void FaceStall(float dt)
        {
            var pv = Market.Instance.Pavilion;
            if (pv == null) return;
            var to = pv.center - transform.position;
            if (dt > 0) _rot = Mathf.LerpAngle(_rot * Mathf.Rad2Deg, Mathf.Atan2(to.x, to.z) * Mathf.Rad2Deg, Mathf.Min(1, dt * 5)) * Mathf.Deg2Rad;
        }
    }
}
