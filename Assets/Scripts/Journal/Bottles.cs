// Chai troi dat + ban do kho bau (ban web: spawnBottle / openBottle / digTreasure dong 1244-1268).
//  - Moi sang 50% co 1 chai tren bai bien sat bien (sau bao: chac chan co).
//  - Mo chai: 35% la ban do kho bau (dau X tren ban do nho / lon) neu chua co, con lai la 1 la thu (10 la, doc lan luot).
//  - Dao bang xeng dung cho (lech toi da 1 o): ruong nho - 300-900 xu, dong xu co, co the co quang.
using System;
using UnityEngine;

namespace SereneIsle
{
    public class Bottles : MonoBehaviour
    {
        public static Bottles Instance { get; private set; }

        public static readonly (string from, string text)[] Letters =
        {
            ("Mẹ", "Con ơi, mẹ thả chai này ở bến phà, chẳng biết có tới tay con không. Mẹ chỉ muốn nói: ăn uống đầy đủ, đừng thức khuya nữa nhé. Mẹ thương."),
            ("Huy (đồng nghiệp cũ)", "Ê, từ hôm ông nghỉ, phòng mình chẳng ai dám nói \"không\" với sếp nữa. Tôi đang tập. Cảm ơn ông đã làm gương. Mà ông sống nổi trên đảo thật à?"),
            ("Một người lạ", "Tôi không biết bạn là ai. Tôi chỉ muốn nói với ai đó rằng hôm nay tôi đã đi bộ một tiếng mà không cầm điện thoại. Cảm giác như được thở lại."),
            ("Vy (bạn thân)", "Mày đi mà không nói tiếng nào, tao giận lắm. Nhưng xem ảnh mày gửi về, tao lại ghen tị. Giữ chỗ cho tao một cái võng nhé!"),
            ("Sếp cũ", "Công ty vẫn chạy bình thường. Tôi nghĩ tôi nợ cậu một lời xin lỗi vì những cuộc họp lúc 7 giờ sáng. Chúc cậu tìm được thứ cậu cần."),
            ("Bé Na (đảo bên cạnh)", "Chào bạn! Em 9 tuổi. Em thả chai này để tìm bạn mới. Đảo em có một con rùa già tên là Ông Mập. Bạn có nuôi con gì không?"),
            ("Chính bạn, 5 năm trước", "Nếu một ngày nào đó mình đọc được thư này, mong là mình đã dám sống chậm lại. Nếu chưa, thì hãy bắt đầu từ hôm nay."),
            ("Một thuyền trưởng già", "Biển dạy tôi một điều: sóng nào rồi cũng lặng. Cứ giữ chặt mái chèo, con à."),
            ("Huy (lần 2)", "Tôi xin nghỉ phép một tuần lần đầu tiên sau 3 năm. Vợ tôi khóc vì vui. Nếu đảo ông còn chỗ, tháng sau tôi ghé chơi nhé."),
            ("Mẹ (lần 2)", "Mẹ thấy ảnh con đen nhẻm mà cười tươi. Mẹ yên tâm rồi. Lúc nào muốn về thì về, không thì cứ ở, miễn là con vui."),
        };

        /// <summary>O co chai (-1 = khong co).</summary>
        public int Bottle { get; private set; } = -1;
        /// <summary>O kho bau tren ban do (-1 = chua co ban do).</summary>
        public int Treasure { get; private set; } = -1;
        public int LettersRead { get; private set; }

        GameObject _bottleGo;
        readonly System.Random _rng = new System.Random();
        bool _hooked;
        int _spawnedDay = -1;

        GameSession S => GameSession.Instance;
        static IslandData D => IslandTerrain.Instance ? IslandTerrain.Instance.Data : null;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { Instance = null; }

        void Awake() { Instance = this; }
        void OnDestroy() { if (Instance == this) Instance = null; if (_hooked && S) S.DayStarted -= OnDayStarted; }

        void Update()
        {
            var s = S;
            if (!s || !s.Running) return;
            if (!_hooked) { s.DayStarted += OnDayStarted; _hooked = true; }
            if (Bottle >= 0 && !_bottleGo) Place();
            if (_bottleGo)
            {
                // lap lanh + nhap nho theo song
                var d = D;
                var p = _bottleGo.transform.position;
                p.y = d.HT[Bottle] + .04f + Mathf.Sin(Time.time * 2) * .02f;
                _bottleGo.transform.position = p;
                _bottleGo.transform.Rotate(0, Time.deltaTime * 20, 0, Space.World);
            }
        }

        void OnDayStarted(int day)
        {
            if (_spawnedDay == day) return;
            _spawnedDay = day;
            bool afterStorm = WeatherSystem.Instance && WeatherSystem.Instance.Of(day - 1) == WeatherKind.Storm;
            if (Bottle < 0 && (afterStorm || _rng.NextDouble() < .5)) Spawn();
        }

        /// <summary>Dat chai len bai bien sat bien (15-75 o tu lang).</summary>
        public bool Spawn()
        {
            var d = D;
            if (d == null) return false;
            for (int t = 0; t < 4000; t++)
            {
                double a = _rng.NextDouble() * Math.PI * 2, r = 15 + _rng.NextDouble() * 60;
                int i = (int)Math.Round(d.CX + Math.Cos(a) * r), j = (int)Math.Round(d.CZ + Math.Sin(a) * r);
                if (!d.InBounds(i, j)) continue;
                int k = d.Idx(i, j);
                if (d.Bio[k] != Biome.Beach || d.Solid[k] != 0 || d.Level[k] == 0) continue;
                if (Placeables.Instance && Placeables.Instance.At(k) != null) continue;
                bool sea = false;
                foreach (var (dx, dz) in new[] { (1, 0), (-1, 0), (0, 1), (0, -1) })
                    if (d.InBounds(i + dx, j + dz) && d.Ocean[d.Idx(i + dx, j + dz)] != 0) sea = true;
                if (!sea) continue;
                Bottle = k;
                Place();
                return true;
            }
            return false;
        }

        void Place()
        {
            if (_bottleGo) Destroy(_bottleGo);
            _bottleGo = null;
            var d = D;
            if (Bottle < 0 || d == null) return;
            var b = new MeshBuilder();
            b.P(LowPolyShapes.Cylinder(.07f, .08f, .26f, 8), "#7fc8b0", 0, .13f, 0, 1, 1, 1, 0, 0, Mathf.PI / 2.3f);
            b.P(LowPolyShapes.Cylinder(.03f, .03f, .08f, 6), "#a07a4a", .15f, .19f, 0, 1, 1, 1, 0, 0, Mathf.PI / 2.3f);
            b.Box("#f2e6c8", 0, .13f, 0, .14f, .05f, .05f);
            _bottleGo = new GameObject("Bottle");
            _bottleGo.transform.SetParent(transform, false);
            _bottleGo.transform.position = new Vector3(Bottle % d.N, d.HT[Bottle], Bottle / d.N);
            _bottleGo.AddComponent<MeshFilter>().sharedMesh = b.ToMesh("Bottle");
            _bottleGo.AddComponent<MeshRenderer>().sharedMaterial = MaterialLibrary.Objects(false);
            // lap lanh (ban web: sprite ✨)
            var spark = new GameObject("Sparkle");
            spark.transform.SetParent(_bottleGo.transform, false);
            spark.transform.localPosition = new Vector3(0, .45f, 0);
            spark.transform.localScale = Vector3.one * .12f;
            spark.AddComponent<MeshFilter>().sharedMesh = LowPolyShapes.Octa;
            var mr = spark.AddComponent<MeshRenderer>();
            mr.sharedMaterial = SparkMat;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        static Material _spark;
        static Material SparkMat
        {
            get
            {
                if (_spark) return _spark;
                _spark = new Material(MaterialLibrary.Lambert) { name = "SI_BottleSparkle" };
                _spark.SetColor("_BaseColor", new Color(3.2f, 3f, 1.6f, 1));
                _spark.SetFloat("_VertexColorWeight", 0);
                _spark.SetFloat("_FogScale", .3f);
                return _spark;
            }
        }

        // ------------------------------------------------------------------ phim E
        public Action NearbyAction(Vector3 pos, out string label)
        {
            label = null;
            var d = D;
            if (Bottle < 0 || d == null) return null;
            if (Vector2.Distance(new Vector2(pos.x, pos.z), new Vector2(Bottle % d.N, Bottle / d.N)) > 1.6f) return null;
            label = "E: Nhặt chai thủy tinh";
            return Open;
        }

        public void Open()
        {
            var s = S; var d = D;
            if (Bottle < 0) return;
            Bottle = -1;
            if (_bottleGo) Destroy(_bottleGo);
            Journal.Stat("letter");
            s.Stats.Calm = Mathf.Min(100, s.Stats.Calm + 2);
            if (s.Body) s.Body.PlayAct("pickup");
            var ui = ActionUI.Instance;
            if (Treasure < 0 && _rng.NextDouble() < .35 && MakeTreasure())
            {
                int ti = Treasure % d.N, tj = Treasure / d.N;
                var p = s.Player.transform.position;
                double ang = Math.Atan2(tj - p.z, ti - p.x);
                string[] dirs = { "Đông", "Đông Bắc", "Bắc", "Tây Bắc", "Tây", "Tây Nam", "Nam", "Đông Nam" }; // z tang = Bac
                string dir = dirs[(((int)Math.Round(ang / (Math.PI / 4)) % 8) + 8) % 8];
                int dist = Mathf.RoundToInt(Vector2.Distance(new Vector2(ti, tj), new Vector2(p.x, p.z)));
                ui?.ShowDialog("Bản đồ kho báu",
                    $"Trong chai là một tấm bản đồ cũ, vẽ dấu X ở phía {dir}, cách đây chừng {dist} bước.\n\nDấu X đã hiện trên bản đồ (phím M). Tới đúng chỗ, dùng xẻng đào lên nhé!", ("Cất bản đồ", null));
                return;
            }
            var (from, text) = Letters[LettersRead % Letters.Length];
            LettersRead++;
            ui?.ShowDialog(from, "Bạn mở nút chai, rút ra một lá thư đã hơi ố vàng…\n\n" + text, ("Cất thư", null));
        }

        bool MakeTreasure()
        {
            var d = D; var s = S;
            var p = s.Player.transform.position;
            for (int t = 0; t < 6000; t++)
            {
                int i = _rng.Next(4, d.N - 5), j = _rng.Next(4, d.N - 5), k = d.Idx(i, j);
                float dist = Vector2.Distance(new Vector2(i, j), new Vector2(p.x, p.z));
                if (dist < 40 || dist > 220 || d.Level[k] == 0 || d.Solid[k] != 0 || d.Bio[k] == Biome.Town || d.Pier[k] != 0 || d.Fresh[k] != 0) continue;
                if (d.HT[k] < TideClock.MaxLevel + .05f) continue;
                Treasure = k;
                return true;
            }
            return false;
        }

        /// <summary>Xeng dao o k: trung kho bau (lech toi da 1 o) thi mo ruong. Tra ve true neu da dao duoc.</summary>
        public bool TryDig(int k)
        {
            var d = D; var s = S;
            if (Treasure < 0 || d == null || k < 0) return false;
            int i = k % d.N, j = k / d.N, ti = Treasure % d.N, tj = Treasure / d.N;
            if (Math.Abs(i - ti) > 1 || Math.Abs(j - tj) > 1) return false;
            Treasure = -1;
            int coins = 300 + _rng.Next(601);
            s.Coins += coins;
            s.Give("oldcoin", 1 + _rng.Next(2), true);
            bool ore = _rng.NextDouble() < .5;
            if (ore) s.Give("ore", 2 + _rng.Next(3), true);
            Journal.Stat("treasure");
            s.Stats.Calm = Mathf.Min(100, s.Stats.Calm + 3);
            Debris.Burst(new Vector3(i, d.HT[k] + .5f, j), MaterialLibrary.Hex("#ffd34d"), 30, 5);
            ActionUI.Instance?.ShowDialog("Kho báu!", $"Xẻng chạm vào gỗ. Một chiếc rương nhỏ! Bên trong có {coins} xu, vài đồng xu cổ{(ore ? " và mấy viên quặng" : "")}.", ("Tuyệt quá", null));
            return true;
        }

        // ------------------------------------------------------------------ luu / tai
        public void Restore(int bottle, int treasure, int letters)
        {
            Bottle = bottle; Treasure = treasure; LettersRead = Mathf.Max(0, letters);
            if (_bottleGo) Destroy(_bottleGo);
        }
    }
}
