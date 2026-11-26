// Noi dung quan dao nho (Docs/ThietKeGameplay.md muc 7.2):
//  - Len hon dao lan dau: "Tim thay Hon ..." + 5 binh yen (luu trong file game).
//  - Hon Da: ruong o dinh (E mo 1 lan: xu + dong xu co + manh san ho).
//  - Hon Rua: dem ram (19h-23h) rua con bo ra bien; lan dau thay: khoanh khac binh yen +10.
//  - Hon Yen: chim yen bay vong quanh vach da.
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SereneIsle
{
    public class IsletSystem : MonoBehaviour
    {
        public static IsletSystem Instance { get; private set; }
        public IReadOnlyList<Islet> Islets => Data != null ? Data.Islets : (IReadOnlyList<Islet>)Array.Empty<Islet>();
        public readonly HashSet<string> Discovered = new HashSet<string>();
        public bool ChestOpened { get; set; }
        public bool TurtlesSeen { get; set; }
        public bool HatchingNow { get; private set; }

        GameObject _chest;
        readonly List<Transform> _birds = new List<Transform>();
        readonly List<Transform> _turtles = new List<Transform>();
        Islet _swift, _turtle, _rock;
        bool _built;
        float _hatchT;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { Instance = null; }

        GameSession S => GameSession.Instance;
        static IslandData Data => IslandTerrain.Instance ? IslandTerrain.Instance.Data : null;

        void Awake() { Instance = this; }
        void OnDestroy() { if (Instance == this) Instance = null; }

        public Islet Find(IsletKind k) { foreach (var i in Islets) if (i.kind == k) return i; return null; }

        public Islet At(Vector3 p)
        {
            foreach (var i in Islets)
                if (Vector2.Distance(new Vector2(p.x, p.z), new Vector2(i.ci, i.cj)) < i.radius + 1.5f) return i;
            return null;
        }

        void Build()
        {
            var d = Data;
            if (d == null || _built) return;
            _built = true;
            _rock = Find(IsletKind.Rock); _swift = Find(IsletKind.Swift); _turtle = Find(IsletKind.Turtle);
            if (_rock != null && _rock.featureTile >= 0)
            {
                _chest = new GameObject("IsletChest");
                _chest.transform.SetParent(transform, false);
                int k = _rock.featureTile;
                _chest.transform.SetPositionAndRotation(new Vector3(k % d.N, d.HT[k], k / d.N), Quaternion.Euler(0, 30, 0));
                _chest.AddComponent<MeshFilter>().sharedMesh = IsletMeshes.Chest;
                _chest.AddComponent<MeshRenderer>().sharedMaterial = MaterialLibrary.Objects(false);
                _chest.SetActive(!ChestOpened);
            }
            if (_swift != null)
                for (int n = 0; n < 9; n++)
                {
                    var b = new GameObject("Swift_" + n).transform;
                    b.SetParent(transform, false);
                    b.gameObject.AddComponent<MeshFilter>().sharedMesh = IsletMeshes.Bird;
                    b.gameObject.AddComponent<MeshRenderer>().sharedMaterial = MaterialLibrary.Objects(false);
                    _birds.Add(b);
                }
        }

        void Update()
        {
            var s = S; var d = Data;
            if (!s || d == null || !IslandTerrain.Instance.IsReady) return;
            Build();
            if (_chest) _chest.SetActive(!ChestOpened);
            AnimateBirds(d);
            if (!s.Running || !s.Player) return;
            var p = s.Player.transform.position;
            var here = At(p);
            if (here != null && d.WaterDepth(p.x, p.z) <= .05f && Discovered.Add(here.name))
            {
                s.Say($"Tìm thấy {here.name}!");
                s.Stats.Calm = Mathf.Min(100, s.Stats.Calm + 5);
            }
            UpdateTurtles(s, d, here);
        }

        void AnimateBirds(IslandData d)
        {
            if (_swift == null || _birds.Count == 0) return;
            float t = Time.time;
            float top = _swift.featureTile >= 0 ? d.HT[_swift.featureTile] : 2f;
            for (int n = 0; n < _birds.Count; n++)
            {
                float a = t * (.6f + n * .05f) + n * .7f, r = 3f + (n % 3) * 1.4f;
                var pos = new Vector3(_swift.ci + Mathf.Cos(a) * r, top + 1.2f + (n % 4) * .5f + Mathf.Sin(t * 2 + n) * .2f, _swift.cj + Mathf.Sin(a) * r);
                _birds[n].SetPositionAndRotation(pos, Quaternion.Euler(Mathf.Sin(t * 9 + n) * 25, -a * Mathf.Rad2Deg, 0));
            }
        }

        // ---------------------------------------------------------------- rua con dem ram
        /// <summary>Dem ram (ngay 15 am) tu 19h: rua con bo ra bien o Hon Rua.</summary>
        public static bool HatchTime(GameSession s) => s && s.Date.IsFullMoon && s.Clock.Hour >= 19;

        void UpdateTurtles(GameSession s, IslandData d, Islet here)
        {
            bool on = _turtle != null && HatchTime(s) && here == _turtle;
            if (on && !HatchingNow) { HatchingNow = true; _hatchT = 0; SpawnTurtles(d); }
            if (!on && HatchingNow) { HatchingNow = false; foreach (var t in _turtles) if (t) Destroy(t.gameObject); _turtles.Clear(); }
            if (!HatchingNow) return;
            _hatchT += Time.deltaTime;
            foreach (var t in _turtles)
            {
                if (!t) continue;
                var dir = (t.position - new Vector3(_turtle.ci, t.position.y, _turtle.cj)).normalized;
                var np = t.position + dir * Time.deltaTime * .35f;
                np.y = d.GroundHeight(np.x, np.z);
                if (d.WaterDepth(np.x, np.z) > .1f) { t.gameObject.SetActive(false); continue; }
                t.SetPositionAndRotation(np, Quaternion.LookRotation(dir));
            }
            if (!TurtlesSeen && _hatchT > 2f)
            {
                TurtlesSeen = true;
                s.Say("Khoảnh khắc bình yên: đàn rùa con bò ra biển dưới trăng rằm.");
                s.Stats.Calm = Mathf.Min(100, s.Stats.Calm + 10);
            }
        }

        void SpawnTurtles(IslandData d)
        {
            for (int n = 0; n < 8; n++)
            {
                float a = n * Mathf.PI * 2 / 8 + .3f, r = _turtle.radius * .35f;
                var t = new GameObject("Hatchling_" + n).transform;
                t.SetParent(transform, false);
                float x = _turtle.ci + Mathf.Cos(a) * r, z = _turtle.cj + Mathf.Sin(a) * r;
                t.position = new Vector3(x, d.GroundHeight(x, z), z);
                t.gameObject.AddComponent<MeshFilter>().sharedMesh = IsletMeshes.Turtle;
                t.gameObject.AddComponent<MeshRenderer>().sharedMaterial = MaterialLibrary.Objects(false);
                _turtles.Add(t);
            }
        }

        // ---------------------------------------------------------------- phim E: ruong Hon Da
        public Action NearbyAction(Vector3 p, out string label)
        {
            label = null;
            if (!_chest || ChestOpened || !_chest.activeSelf) return null;
            if (Vector2.Distance(new Vector2(p.x, p.z), new Vector2(_chest.transform.position.x, _chest.transform.position.z)) > 1.8f) return null;
            label = "E: Mở rương cũ";
            return OpenChest;
        }

        public void OpenChest()
        {
            var s = S;
            if (ChestOpened || !s) return;
            ChestOpened = true;
            s.Coins += 120;
            s.Give("oldcoin", 2, true); s.Give("manh_san_ho", 2, true);
            s.Say("Rương cũ của ghe đắm: 120 xu, 2 đồng xu cổ, 2 mảnh san hô.");
            if (_chest) _chest.SetActive(false);
        }
    }

    public static class IsletMeshes
    {
        static Mesh _chest, _bird, _turtle;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { _chest = _bird = _turtle = null; }

        public static Mesh Chest
        {
            get
            {
                if (_chest) return _chest;
                var b = new MeshBuilder();
                b.Box("#7a5434", 0, .22f, 0, .7f, .44f, .46f);
                b.Box("#8a6440", 0, .5f, 0, .72f, .14f, .48f);
                b.Box("#c8a040", 0, .36f, .24f, .12f, .14f, .03f);
                b.Box("#5a3a22", 0, .22f, 0, .74f, .06f, .5f);
                _chest = b.ToMesh("IsletChest");
                return _chest;
            }
        }

        public static Mesh Bird
        {
            get
            {
                if (_bird) return _bird;
                var b = new MeshBuilder();
                b.P(LowPolyShapes.Ico0, "#2a2a32", 0, 0, 0, .06f, .05f, .12f);
                b.Box("#2a2a32", -.12f, 0, 0, .22f, .01f, .07f, 0, 0, .25f);
                b.Box("#2a2a32", .12f, 0, 0, .22f, .01f, .07f, 0, 0, -.25f);
                _bird = b.ToMesh("Swift");
                return _bird;
            }
        }

        public static Mesh Turtle
        {
            get
            {
                if (_turtle) return _turtle;
                var b = new MeshBuilder();
                b.P(LowPolyShapes.Ico1, "#3f5a3a", 0, .03f, 0, .07f, .03f, .08f);
                b.P(LowPolyShapes.Ico0, "#5a7a4a", 0, .03f, .08f, .025f, .02f, .03f);
                foreach (var (x, z) in new[] { (-.06f, .04f), (.06f, .04f), (-.05f, -.05f), (.05f, -.05f) })
                    b.Box("#5a7a4a", x, .02f, z, .05f, .01f, .03f);
                _turtle = b.ToMesh("Hatchling");
                return _turtle;
            }
        }
    }
}
