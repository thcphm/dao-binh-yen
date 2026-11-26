// Cau ca (ban web dong 1326-1386): giu nut de lay luc -> tha quang -> phao noi cho ca -> ca dop moi ("!")
// -> bam giat can -> tro choi keo: giu nut de day khung len, giu ca nam trong khung cho day thanh.
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SereneIsle
{
    public class Fishing : MonoBehaviour
    {
        public enum State { Idle, Charge, Fly, Wait, Bite, Reel, Fail }

        public State Current { get; private set; } = State.Idle;
        public bool Busy => Current != State.Idle;
        public float Power { get; private set; }
        public ItemDefinition Fish { get; private set; }
        public string Habitat { get; private set; } = "sea";

        public static readonly Dictionary<string, string> HabitatNames = new Dictionary<string, string>
        {
            ["sea"] = "Biển", ["trop"] = "Biển nhiệt đới", ["swamp"] = "Đầm lầy", ["jungle"] = "Suối rừng mưa", ["mount"] = "Hồ núi", ["pond"] = "Ao hồ",
        };
        public static readonly string[] Rarity = { "", "Phổ biến", "Thường gặp", "Hiếm", "Rất hiếm", "Huyền thoại" };

        // tro choi keo (REEL)
        public struct ReelState { public float pos, vel, fy, fv, tgt, tt, prog, zone; public int d; public bool hold, inZone; public float spd; }
        public ReelState Reel;

        float _t, _dur, _nib, _land;
        int _k;
        bool _bait;
        Vector3 _from, _to;
        Transform _bobber;
        LineRenderer _line;
        System.Random _rng = new System.Random();

        GameSession S => GameSession.Instance;
        IslandData D => IslandTerrain.Instance ? IslandTerrain.Instance.Data : null;
        public int RodLevel = 1;

        void Awake()
        {
            // phao: dau do, than trang, can den
            var b = new MeshBuilder();
            b.P(ProceduralMeshes.Sphere, "#d83a2a", 0, .02f, 0, .18f, .12f, .18f);
            b.P(ProceduralMeshes.Sphere, "#f4f0e8", 0, -.03f, 0, .18f, .1f, .18f);
            b.Box("#2a2a2a", 0, .1f, 0, .012f, .09f, .012f);
            var go = new GameObject("Bobber");
            go.transform.SetParent(transform, false);
            go.AddComponent<MeshFilter>().sharedMesh = b.ToMesh("Bobber");
            go.AddComponent<MeshRenderer>().sharedMaterial = MaterialLibrary.Objects(false);
            _bobber = go.transform;
            go.SetActive(false);

            var lg = new GameObject("FishLine");
            lg.transform.SetParent(transform, false);
            _line = lg.AddComponent<LineRenderer>();
            _line.positionCount = 14;
            _line.widthMultiplier = .012f;
            _line.sharedMaterial = MaterialLibrary.Solid("#f4f4f4");
            _line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _line.generateLightingData = true; // shader WebLambert can phap tuyen
            _line.enabled = false;
        }

        // ------------------------------------------------------------------ nut bam
        /// <summary>rodDown: bam nut dung cu khi dang cam can.</summary>
        public void Press()
        {
            var s = S;
            switch (Current)
            {
                case State.Idle:
                    if (!s.Stats.UseEnergy(1)) return;
                    Current = State.Charge; Power = 0;
                    // quay lung ve camera (ban web: PL.rot = camYaw + PI)
                    var rig = Camera.main ? Camera.main.GetComponent<OrbitCamera>() : null;
                    if (rig) s.Player.Face(rig.Yaw + Mathf.PI);
                    break;
                case State.Bite: StartReel(); break;
                case State.Wait:
                    End(); s.Say("Giật cần sớm quá, cá hoảng bỏ đi.");
                    s.Body.PlayAct("rod");
                    break;
            }
        }

        public void Cancel() { if (Busy) End(); }

        /// <summary>Tha nut ngay voi luc cho truoc (0..1) - dung cho test / tay cam khong giu duoc.</summary>
        public void Release(float power)
        {
            if (Current != State.Charge) return;
            Power = Mathf.Clamp01(power);
            Cast();
        }

        void End()
        {
            Current = State.Idle;
            _bobber.gameObject.SetActive(false);
            _line.enabled = false;
            if (S && S.Body) S.Body.HoldPose = null;
        }

        // ------------------------------------------------------------------ moi khung
        void Update()
        {
            var s = S;
            if (!s || Current == State.Idle) return;
            float dt = Time.deltaTime, T = Time.time;
            if (s.Transitioning) { End(); return; }

            if (Current == State.Charge)
            {
                Power = Mathf.Min(1, Power + dt * .75f);
                if (!GameInput.UseHeld) Cast();
                return;
            }
            Vector3 tip = RodTip();
            float sag = .35f, jit = 0;
            switch (Current)
            {
                case State.Fly:
                    _t += dt;
                    if (_t < 0) { _from = tip; return; }
                    _bobber.gameObject.SetActive(true); _line.enabled = true;
                    float q = Mathf.Min(1, _t / _dur);
                    var p = Vector3.Lerp(_from, _to, q);
                    p.y += Mathf.Sin(q * Mathf.PI) * (1 + Vector3.Distance(_from, _to) * .2f);
                    _bobber.position = p; sag = .05f;
                    if (q >= 1) Land();
                    break;
                case State.Fail:
                    _t -= dt; if (_t <= 0) End();
                    break;
                case State.Wait:
                    _t -= dt; _nib -= dt;
                    float dip = 0;
                    if (_nib < .14f) dip = -.045f;
                    if (_nib < 0) _nib = .7f + (float)_rng.NextDouble() * 1.5f;
                    SetBobberY(IslandData.SEA + .03f + Mathf.Sin(T * 3) * .015f + dip);
                    if (_t <= 0)
                    {
                        Fish = PickFish(Habitat, _bait);
                        if (!Fish) { End(); s.Say("Chỗ này không có cá."); return; }
                        Current = State.Bite; _t = 1.05f - Fish.fishDifficulty * .08f;
                        GameAudio.Play(Sfx.Bite);
                        ActionUI.Float("!", _bobber.position + Vector3.up * .9f, new Color(1, .89f, .48f));
                    }
                    break;
                case State.Bite:
                    _t -= dt;
                    SetBobberY(IslandData.SEA - .09f + Mathf.Sin(T * 28) * .05f); sag = .02f; jit = .06f;
                    if (_t <= 0)
                    {
                        s.Say("Cá nhả mồi rồi…");
                        if (_rng.NextDouble() < .5) { Current = State.Wait; _t = 2 + (float)_rng.NextDouble() * 3; }
                        else End();
                    }
                    break;
                case State.Reel:
                    UpdateReel(dt, T);
                    if (Current != State.Reel) return;
                    SetBobberY(IslandData.SEA - .06f + Mathf.Sin(T * 20) * .04f); sag = .01f; jit = .03f;
                    break;
            }
            if (_line.enabled) DrawLine(tip, sag, jit, T);
        }

        void SetBobberY(float y) { var p = _bobber.position; p.y = y; _bobber.position = p; }

        Vector3 RodTip()
        {
            var s = S;
            var pl = s.Player.transform;
            var hand = s.Body && s.Body.Hand ? s.Body.Hand.position : pl.position + Vector3.up * .7f;
            float r = s.Player.Facing;
            return hand + new Vector3(Mathf.Sin(r), 0, Mathf.Cos(r)) * .75f + Vector3.up * .55f;
        }

        void Cast()
        {
            var s = S; var d = D;
            float dist = 1.6f + Power * 8.4f, r = s.Player.Facing;
            var pp = s.Player.transform.position;
            float x = pp.x + Mathf.Sin(r) * dist, z = pp.z + Mathf.Cos(r) * dist;
            int i = Mathf.FloorToInt(x + .5f), j = Mathf.FloorToInt(z + .5f);
            s.Body.PlayAct("rod");
            GameAudio.Play(Sfx.Swish);
            Current = State.Fly;
            _t = -CharacterActions.DurationOf("rod") * .45f;
            _dur = .3f + dist * .05f;
            bool inb = d != null && i >= 0 && j >= 0 && i < d.N && j < d.N;
            _k = inb ? j * d.N + i : -1;
            _land = _k >= 0 ? d.HeightAt(x, z) : IslandData.SEA - 1;
            _to = new Vector3(x, Mathf.Max(IslandData.SEA + .04f, _land + .05f), z);
            _from = RodTip();
        }

        void Land()
        {
            var s = S; var d = D;
            if (_k < 0 || d.Level[_k] != 0 || _land > IslandData.SEA + .02f)
            {
                s.Say("Mồi rơi trên bờ rồi. Quăng ra chỗ có nước nhé.");
                Current = State.Fail; _t = .7f;
                return;
            }
            Debris.Burst(new Vector3(_to.x, IslandData.SEA + .05f, _to.z), MaterialLibrary.Hex("#d8f2ff"), 8, 1.5f);
            GameAudio.Play(Sfx.Splash, .6f);
            Habitat = HabitatAt(d, _k);
            _bait = s.Inventory.Has("worm");
            if (_bait) s.Inventory.Take("worm");
            Current = State.Wait;
            _t = (RodLevel > 1 ? 1.5f : 2.5f) + (float)_rng.NextDouble() * 4 * (_bait ? .6f : 1);
            _nib = .6f + (float)_rng.NextDouble();
            s.Body.HoldPose = "Fishing_Idle";
            s.Say($"Vùng câu: {HabitatNames[Habitat]}{(_bait ? " · dùng mồi giun" : "")}");
        }

        /// <summary>habitatAt: bien (nong/lanh) hoac vung nuoc ngot theo biome gan nhat.</summary>
        public static string HabitatAt(IslandData d, int k)
        {
            int N = d.N, i = k % N, j = k / N;
            if (d.Ocean[k] != 0) return d.Temp(i, j) > .6 ? "trop" : "sea";
            for (int r = 1; r < 7; r++)
                for (int dj = -r; dj <= r; dj++)
                    for (int di = -r; di <= r; di++)
                    {
                        int x = i + di, z = j + dj;
                        if (x < 0 || z < 0 || x >= N || z >= N) continue;
                        var b = d.Bio[z * N + x];
                        if (b == Biome.Mangrove) return "swamp";
                        if (b == Biome.Tropics) return "jungle";
                        if (b == Biome.Alpine) return "mount";
                        if ((int)b > 1) return b == Biome.Forest && d.Temp(i, j) < .35 ? "mount" : "pond";
                    }
            return "pond";
        }

        /// <summary>pickFish: chon ca theo vung, ngay/dem, do hiem (ca hiem de gap hon khi co moi / can tot).</summary>
        public ItemDefinition PickFish(string hab, bool bait)
        {
            bool night = S.Clock.IsNight;
            var list = S.Items.items.Where(f => f && f.fishDifficulty > 0 && f.habitats != null && f.habitats.Contains(hab)
                                                && (string.IsNullOrEmpty(f.activeTime) || f.activeTime == "any" || (f.activeTime == "night") == night)).ToList();
            if (list.Count == 0) return null;
            float[] W = { 0, 10, 6, 3, 1.2f, .35f };
            float boost = (RodLevel > 1 ? 1.6f : 1) * (bait ? 1.5f : 1)
                          * (WeatherSystem.Instance && WeatherSystem.Instance.Today == WeatherKind.Rain ? 1.25f : 1)   // ban web: mua -> ca hiem x1.25
                          * (S.Stats.Calm >= 75 ? 1.2f : 1);                                                           // binh yen 75
            int craft = Crafts.Level(Crafts.Fishing); // nghe cau (Bac Chin Do)
            // cau muc dem tren ghe thung co den (BoatSystem), it muc dem trang tron
            bool lamp = BoatSystem.Instance && BoatSystem.Instance.LampOn;
            double moon = LunarCalendar.MoonAge(S.Clock.Day, S.Clock.Hour);
            var ws = list.Select(f => W[Mathf.Clamp(f.fishDifficulty, 0, 5)] * (f.fishDifficulty >= 3 ? boost : 1) * Crafts.RareWeight(craft, f.fishDifficulty)
                                      * BoatSystem.SquidBoost(f, lamp, moon)).ToList();
            float r = (float)_rng.NextDouble() * ws.Sum();
            for (int n = 0; n < list.Count; n++) { r -= ws[n]; if (r <= 0) return list[n]; }
            return list[0];
        }

        // ------------------------------------------------------------------ tro choi keo ca
        void StartReel()
        {
            Current = State.Reel;
            int d = Fish.fishDifficulty;
            float zone = Mathf.Max(.15f, .34f - d * .04f) + (RodLevel > 1 ? .08f : 0) + Crafts.ReelZoneBonus(Crafts.Level(Crafts.Fishing));
            Reel = new ReelState { pos = .2f, fy = .35f, tgt = .5f, prog = .3f, zone = zone, d = d, spd = .32f + d * .17f };
            S.Body.HoldPose = "Fishing_Reeling";
        }

        void UpdateReel(float dt, float T)
        {
            var R = Reel;
            R.hold = GameInput.UseHeld;
            R.vel += (R.hold ? 2.9f : -2.4f) * dt;
            R.vel *= Mathf.Pow(.25f, dt);
            R.pos += R.vel * dt;
            float lo = R.zone / 2, hi = 1 - R.zone / 2;
            if (R.pos < lo) { R.pos = lo; R.vel *= -.35f; }
            if (R.pos > hi) { R.pos = hi; R.vel = 0; }
            R.tt -= dt;
            if (R.tt <= 0)
            {
                R.tt = (1.7f - R.d * .24f) * (.45f + (float)_rng.NextDouble());
                R.tgt = .05f + (float)_rng.NextDouble() * .9f;
                if (R.d >= 4 && _rng.NextDouble() < .45) R.tgt = R.fy > .5f ? .05f : .95f;
            }
            float df = R.tgt - R.fy;
            R.fv += Mathf.Sign(df) * Mathf.Min(Mathf.Abs(df) * 6, 1) * R.spd * dt * 7;
            R.fv *= Mathf.Pow(.12f, dt);
            R.fy = Mathf.Clamp(R.fy + R.fv * dt + (R.d >= 3 ? Mathf.Sin(T * (7 + R.d * 3)) * .0025f * R.d : 0), .03f, .97f);
            R.inZone = Mathf.Abs(R.fy - R.pos) < R.zone / 2;
            R.prog += dt * (R.inZone ? .3f : -(.15f + R.d * .045f));
            Reel = R;
            if (R.prog >= 1) Catch();
            else if (R.prog <= 0) { End(); GameAudio.Play(Sfx.Err); S.Say("Cá vùng vẫy rồi thoát mất!"); }
        }

        void Catch()
        {
            var s = S; var f = Fish;
            End();
            s.Give(f.id, 1, true);
            GameAudio.Play(Sfx.Catch);
            s.Stats.Calm = Mathf.Min(100, s.Stats.Calm + 1); // ban web: moi con ca +1 binh yen
            bool first = s.Caught.Add(f.id);
            ActionUI.Float($"{f.displayName}!", s.Player.transform.position + Vector3.up * 2.2f, f.fishDifficulty >= 4 ? new Color(1, .81f, .29f) : new Color(1, .91f, .66f));
            s.Say($"Câu được {f.displayName} · {Rarity[Mathf.Clamp(f.fishDifficulty, 0, 5)]} · {f.price} xu");
            if (first) s.Say($"Mới trong sổ sưu tầm: {f.displayName}!");
            s.Body.PlayAct("rod");
        }

        void DrawLine(Vector3 tip, float sag, float jit, float T)
        {
            var b = _bobber.position;
            for (int n = 0; n < _line.positionCount; n++)
            {
                float q = n / (float)(_line.positionCount - 1), sn = Mathf.Sin(q * Mathf.PI);
                _line.SetPosition(n, new Vector3(
                    tip.x + (b.x - tip.x) * q + (jit > 0 ? Mathf.Sin(T * 40 + n) * jit * sn : 0),
                    tip.y + (b.y + .1f - tip.y) * q - sag * sn,
                    tip.z + (b.z - tip.z) * q + (jit > 0 ? Mathf.Cos(T * 37 + n) * jit * sn : 0)));
            }
        }
    }
}
