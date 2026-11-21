// Thu tren dao (giai doan 4c, ban web dong 1141-1146, 1807-1844, 1899-1909):
//  - Thu hoang: sinh quanh nguoi choi theo vung sinh thai (vung hoi sinh cang cao cang dong), di dao, so thi chay;
//    cho an qua mong thi dung yen (thuan); lan dau thay ghi vao so (+binh yen).
//  - Thu cung: ngay 3 co thung giay truoc leu (cun hoac meo), dat ten, di theo, vuot ve moi ngay (+gan bo, +binh yen).
//  - Vat nuoi: mua giong o Bac Chin Do, nuoi trong chuong (do che tao dat xuong dat); cho an thi sang hom sau co
//    trung / sua / phan chuong; vuot ve tang tinh cam -> co ngay duoc gap doi.
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SereneIsle
{
    [Serializable]
    public class PetData { public string type; public string name; public int bond; public int petDay; }

    [Serializable]
    public class FarmAnimal { public string type; public int pen = -1; public bool fed; public int ready; public int love; public int petDay; }

    public class Animals : MonoBehaviour
    {
        public static Animals Instance { get; private set; }

        public int maxWild = 16;
        public float wildRadius = 40, despawnRadius = 58;
        public const int PetBoxDay = 3;
        public const float Reach = 1.3f;

        sealed class Wild
        {
            public AnimalDef def; public AnimalBody body;
            public Vector3 home, target; public bool moving; public float wait, rot, tameT;
        }

        sealed class Tame
        {
            public FarmAnimal data; public AnimalBody body;
            public Vector3 target; public bool moving; public float wait, rot;
        }

        readonly List<Wild> _wild = new List<Wild>();
        readonly List<Tame> _farm = new List<Tame>();
        public readonly List<FarmAnimal> Farm = new List<FarmAnimal>();
        public readonly HashSet<string> Seen = new HashSet<string>();
        public PetData Pet { get; private set; }
        public AnimalBody PetBody { get; private set; }
        public GameObject PetBox { get; private set; }
        public int WildCount => _wild.Count;
        public bool spawnWild = true;

        float _spawnT, _petIdle, _petRot, _heart;
        readonly System.Random _rng = new System.Random();
        Transform _root;
        bool _hooked;

        GameSession S => GameSession.Instance;
        static IslandData D => IslandTerrain.Instance ? IslandTerrain.Instance.Data : null;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { Instance = null; }

        void Awake() { Instance = this; }
        void OnDestroy() { if (Instance == this) Instance = null; if (_hooked && S) S.DayStarted -= OnDayStarted; }

        Transform Root
        {
            get
            {
                if (!_root) { _root = new GameObject("Animals").transform; _root.SetParent(transform, false); }
                return _root;
            }
        }

        // ================================================================== moi khung
        void Update()
        {
            var s = S; var d = D;
            if (!s || d == null || !s.Running || !s.Player) return;
            if (!_hooked) { s.DayStarted += OnDayStarted; _hooked = true; }
            float dt = s.Active ? Time.deltaTime : 0;
            var pp = s.Player.transform.position;

            UpdateWild(d, s, pp, dt);
            UpdatePet(d, s, pp, dt);
            UpdateFarm(d, s, dt);
        }

        static float Flat(Vector3 a, Vector3 b) => Vector2.Distance(new Vector2(a.x, a.z), new Vector2(b.x, b.z));
        static float Ground(IslandData d, Vector3 p) => d.GroundHeight(p.x, p.z);

        /// <summary>Di 1 buoc ve dich (tranh nuoc sau, vat can). Tra ve false neu bi chan.</summary>
        static bool Step(IslandData d, ref Vector3 p, Vector3 dir, float step, bool wade)
        {
            var n = p + dir * step;
            if (!d.Walkable(n.x, n.z, p.y)) return false;
            if (d.WaterDepth(n.x, n.z) > (wade ? .25f : 0)) return false;
            p = n;
            return true;
        }

        // ================================================================== thu hoang
        void UpdateWild(IslandData d, GameSession s, Vector3 pp, float dt)
        {
            // sinh / xoa theo khoang cach
            for (int i = _wild.Count - 1; i >= 0; i--)
                if (!_wild[i].body || Flat(_wild[i].body.transform.position, pp) > despawnRadius) { if (_wild[i].body) Destroy(_wild[i].body.gameObject); _wild.RemoveAt(i); }
            _spawnT -= Time.deltaTime;
            if (spawnWild && _spawnT <= 0)
            {
                _spawnT = .4f;
                float vit = s.Restoration ? s.Restoration.VitalityAt(pp) : 40;
                int target = Mathf.RoundToInt(maxWild * Mathf.Clamp(.45f + vit / 100f, .45f, 1.2f));
                if (_wild.Count < target) TrySpawnWild(d, pp);
            }

            foreach (var w in _wild)
            {
                var t = w.body.transform;
                var p = t.position;
                float dist = Flat(p, pp), speed = 0;
                if (w.tameT > 0) w.tameT -= dt;
                // chi so khi nguoi choi chay (Shift) toi gan; di bo cham rai thi lai gan cho an duoc
                bool scared = w.def.flee > 0 && w.tameT <= 0 && dist < w.def.flee && s.Player.Sprinting && s.Player.Moving;
                if (scared)
                {
                    var away = p - pp; away.y = 0;
                    if (away.sqrMagnitude < .01f) away = Vector3.forward;
                    away.Normalize();
                    w.target = p + away * 6; w.moving = true; w.wait = 0;
                }
                if (!w.moving)
                {
                    w.wait -= dt;
                    if (w.wait <= 0)
                    {
                        float a = (float)_rng.NextDouble() * Mathf.PI * 2, r = (float)_rng.NextDouble() * 5;
                        w.target = w.home + new Vector3(Mathf.Sin(a) * r, 0, Mathf.Cos(a) * r);
                        w.moving = true;
                    }
                }
                else
                {
                    var to = w.target - p; to.y = 0;
                    float len = to.magnitude;
                    float sp = w.def.speed * (scared ? 1 : .45f);
                    if (len < .2f) { w.moving = false; w.wait = 2 + (float)_rng.NextDouble() * 5; }
                    else if (dt > 0)
                    {
                        var dir = to / len;
                        if (Step(d, ref p, dir, Mathf.Min(len, sp * dt), w.def.id == "rua" || w.def.id == "cua" || w.def.id == "vit"))
                        {
                            speed = sp;
                            w.rot = Mathf.LerpAngle(w.rot * Mathf.Rad2Deg, Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg, Mathf.Min(1, dt * 8)) * Mathf.Deg2Rad;
                        }
                        else { w.moving = false; w.wait = 1; }
                    }
                }
                p.y = Ground(d, p);
                t.SetPositionAndRotation(p, Quaternion.Euler(0, w.rot * Mathf.Rad2Deg, 0));
                w.body.Move(speed, dt, w.tameT > 0 || (w.wait > 3 && w.body.Animated) ? "Eating" : "Idle");

                // lan dau thay
                if (dist < 6 && Seen.Add(w.def.id))
                {
                    s.Say($"Lần đầu thấy {w.def.name} trên đảo! Đã ghi vào sổ.");
                    s.Stats.Calm = Mathf.Min(100, s.Stats.Calm + 2);
                }
            }
        }

        bool TrySpawnWild(IslandData d, Vector3 pp)
        {
            for (int tries = 0; tries < 12; tries++)
            {
                float a = (float)_rng.NextDouble() * Mathf.PI * 2, r = 22 + (float)_rng.NextDouble() * (wildRadius - 22);
                var q = pp + new Vector3(Mathf.Sin(a) * r, 0, Mathf.Cos(a) * r);
                int i = Mathf.RoundToInt(q.x), j = Mathf.RoundToInt(q.z);
                if (!d.InBounds(i, j)) continue;
                int k = d.Idx(i, j);
                if (d.Level[k] == 0 || d.Solid[k] != 0 || d.WaterDepth(q.x, q.z) > 0) continue;
                if (Vector2.Distance(new Vector2(i, j), new Vector2(d.CX, d.CZ)) < 14) continue; // khong vao lang
                var list = new List<AnimalDef>();
                foreach (var def in AnimalDefs.All)
                    if (def.role == AnimalRole.Wild && Array.IndexOf(def.biomes, d.Bio[k]) >= 0) list.Add(def);
                if (list.Count == 0) continue;
                var pick = list[_rng.Next(list.Count)];
                if (pick.quaternius != null && _wild.FindAll(x => x.def == pick).Count >= 3) continue; // huou: toi da 3 con co hoat canh
                SpawnWild(pick, new Vector3(q.x, d.GroundHeight(q.x, q.z), q.z));
                return true;
            }
            return false;
        }

        /// <summary>Tha 1 con thu hoang tai vi tri (test / su kien).</summary>
        public AnimalBody SpawnWild(AnimalDef def, Vector3 pos)
        {
            float rot = (float)_rng.NextDouble() * Mathf.PI * 2;
            var body = AnimalBody.Create(def, Root, pos, rot);
            _wild.Add(new Wild { def = def, body = body, home = pos, rot = rot, wait = (float)_rng.NextDouble() * 3 });
            return body;
        }

        public void ClearWild()
        {
            foreach (var w in _wild) if (w.body) Destroy(w.body.gameObject);
            _wild.Clear();
        }

        // ================================================================== thu cung
        void UpdatePet(IslandData d, GameSession s, Vector3 pp, float dt)
        {
            if (Pet == null)
            {
                if (!PetBox && s.Clock.Day >= PetBoxDay && s.HasHome) SpawnPetBox(d, s);
                return;
            }
            if (!PetBody) SpawnPetBody(d, pp + new Vector3(1, 0, 1));
            var t = PetBody.transform;
            var p = t.position;
            float dist = Flat(p, pp), speed = 0;
            if (dist > 22) { p = pp - s.Player.transform.forward; dist = 1; }
            if (dist > 2.2f && dt > 0)
            {
                float sp = Mathf.Min(dist * 2, s.Player.Sprinting ? 6 : 4.2f) * (dist > 4 ? 1.4f : 1);
                var dir = pp - p; dir.y = 0; dir.Normalize();
                if (!Step(d, ref p, dir, sp * dt, true)) p += dir * sp * dt; // thu cung di xuyen vat can nho cho khoi ket
                speed = sp;
                _petRot = Mathf.Atan2(dir.x, dir.z);
                _petIdle = 0;
            }
            else
            {
                _petIdle += dt;
                if (dist > .6f) _petRot = Mathf.LerpAngle(_petRot * Mathf.Rad2Deg, Mathf.Atan2(pp.x - p.x, pp.z - p.z) * Mathf.Rad2Deg, Mathf.Min(1, dt * 3)) * Mathf.Deg2Rad;
            }
            p.y = Ground(d, p);
            t.SetPositionAndRotation(p, Quaternion.Euler(0, _petRot * Mathf.Rad2Deg, 0));
            PetBody.Move(speed, dt, _petIdle > 3 ? "Sit" : "Idle");
            if (_heart > 0) _heart -= dt;
        }

        void SpawnPetBody(IslandData d, Vector3 at)
        {
            var def = AnimalDefs.Get(Pet.type) ?? AnimalDefs.Get("cho");
            at.y = d.GroundHeight(at.x, at.z);
            PetBody = AnimalBody.Create(def, Root, at, 0);
            PetBody.name = "Pet_" + Pet.name;
        }

        void SpawnPetBox(IslandData d, GameSession s)
        {
            var right = new Vector3(Mathf.Cos(s.HomeFacing), 0, -Mathf.Sin(s.HomeFacing));
            var p = s.HomePosition + right * 1.8f;
            p.y = d.GroundHeight(p.x, p.z);
            var b = new MeshBuilder();
            b.Box("#c9a46e", 0, .18f, 0, .5f, .36f, .4f);
            b.Box("#b08a55", 0, .37f, .18f, .5f, .02f, .2f, -.6f, 0, 0);
            b.Box("#b08a55", 0, .37f, -.18f, .5f, .02f, .2f, .6f, 0, 0);
            b.Box("#8a6a40", -.1f, .2f, .205f, .12f, .02f, .01f);
            PetBox = new GameObject("PetBox");
            PetBox.transform.SetParent(Root, false);
            PetBox.transform.position = p;
            PetBox.AddComponent<MeshFilter>().sharedMesh = b.ToMesh("PetBox");
            PetBox.AddComponent<MeshRenderer>().sharedMaterial = MaterialLibrary.Objects(false);
        }

        void OpenPetBox()
        {
            var ui = ActionUI.Instance;
            ui.ShowDialog("Thùng giấy",
                "Trong thùng giấy có hai con vật nhỏ đang run rẩy: một con cún vàng và một con mèo mướp.\n\nÔng Bảy: \"Chắc ai trên ghe hàng bỏ lại. Con nhận nuôi một đứa đi, đứa kia ông giữ ở nhà sàn cho có bạn.\"",
                ("Nuôi cún", () => AskPetName("cho")), ("Nuôi mèo", () => AskPetName("meo")), ("Để sau", null));
        }

        void AskPetName(string type)
        {
            var names = type == "cho" ? new[] { "Vàng", "Mực", "Lu" } : new[] { "Mướp", "Miu", "Mun" };
            ActionUI.Instance.ShowDialog(type == "cho" ? "Đặt tên cho cún" : "Đặt tên cho mèo", "Gọi bé là gì đây?",
                (names[0], () => Adopt(type, names[0])), (names[1], () => Adopt(type, names[1])), (names[2], () => Adopt(type, names[2])));
        }

        public void Adopt(string type, string name)
        {
            Pet = new PetData { type = type, name = name, bond = 0, petDay = 0 };
            if (PetBox) Destroy(PetBox);
            PetBox = null;
            var s = S; var d = D;
            if (d != null && s && s.Player) SpawnPetBody(d, s.Player.transform.position + s.Player.transform.right);
            s?.Say($"{name} đã về nhà với bạn! Lại gần bấm E để vuốt ve mỗi ngày.");
        }

        public void PetThePet()
        {
            var s = S;
            if (Pet == null || !PetBody) return;
            bool first = Pet.petDay != s.Clock.Day;
            if (first)
            {
                Pet.petDay = s.Clock.Day;
                Pet.bond = Mathf.Min(100, Pet.bond + 8);
                s.Stats.Calm = Mathf.Min(100, s.Stats.Calm + 4);
            }
            _heart = 1.6f;
            Debris.Burst(PetBody.transform.position + Vector3.up * .6f, MaterialLibrary.Hex("#ff7a8a"), 8, 2);
            string[] lines = Pet.type == "cho" ? new[] { "Gâu gâu!", "*vẫy đuôi rối rít*", "*liếm tay bạn*" } : new[] { "Meo~", "*dụi đầu vào tay bạn*", "*rừ rừ*" };
            ActionUI.Bubble(PetBody.transform, $"{Pet.name}: {lines[_rng.Next(lines.Length)]}", 2.5f);
            if (first) s.Say($"{Pet.name} vui lắm (gắn bó {Pet.bond}/100, +4 bình yên).");
        }

        // ================================================================== vat nuoi
        public int CountIn(int pen) { int n = 0; foreach (var a in Farm) if (a.pen == pen) n++; return n; }

        /// <summary>Mua 1 con giong (item a_*). Tra ve thong bao; ok = da mua.</summary>
        public string BuyFarm(string calfItem, out bool ok)
        {
            ok = false;
            var def = AnimalDefs.ByCalf(calfItem);
            var pl = Placeables.Instance;
            if (def == null || !pl) return null;
            int best = -1;
            foreach (var p in pl.All())
                if (p.type == def.pen && CountIn(p.k) < AnimalDefs.PenCapacity(def.pen)) { best = p.k; break; }
            if (best < 0)
            {
                bool any = false;
                foreach (var p in pl.All()) if (p.type == def.pen) any = true;
                return any ? $"{AnimalDefs.PenName(def.pen)} đã đầy ({AnimalDefs.PenCapacity(def.pen)} con). Làm thêm chuồng ở bàn chế tạo."
                           : $"Cần đặt {AnimalDefs.PenName(def.pen)} trước (làm ở bàn chế tạo).";
            }
            var a = new FarmAnimal { type = def.id, pen = best };
            Farm.Add(a);
            SpawnFarmBody(a);
            ok = true;
            return $"Một con {def.name.ToLowerInvariant()} đã về {AnimalDefs.PenName(def.pen).ToLowerInvariant()}! Cho ăn {(def.feed == "fiber" ? $"{def.feedN} cỏ gai" : "1 nông sản")} mỗi ngày.";
        }

        Vector3 PenSpot(int pen)
        {
            var d = D;
            if (d == null || pen < 0) return Vector3.zero;
            int i = pen % d.N, j = pen / d.N;
            return new Vector3(i, d.HT[pen], j + 1.4f);
        }

        void SpawnFarmBody(FarmAnimal a)
        {
            var d = D;
            var def = AnimalDefs.Get(a.type);
            if (d == null || def == null) return;
            var p = PenSpot(a.pen) + new Vector3(((float)_rng.NextDouble() - .5f) * 2, 0, (float)_rng.NextDouble());
            p.y = d.GroundHeight(p.x, p.z);
            var body = AnimalBody.Create(def, Root, p, (float)_rng.NextDouble() * 6.28f);
            _farm.Add(new Tame { data = a, body = body, wait = (float)_rng.NextDouble() * 3 });
        }

        void UpdateFarm(IslandData d, GameSession s, float dt)
        {
            // view cho moi con (sau khi tai game)
            if (_farm.Count != Farm.Count)
            {
                foreach (var t in _farm) if (t.body) Destroy(t.body.gameObject);
                _farm.Clear();
                foreach (var a in Farm) SpawnFarmBody(a);
            }
            var pl = Placeables.Instance;
            bool night = s.Clock.IsNight;
            foreach (var t in _farm)
            {
                // chuong bi nhat len: tim chuong khac cung loai con cho
                if (pl && (t.data.pen < 0 || pl.At(t.data.pen) == null))
                {
                    var def0 = AnimalDefs.Get(t.data.type);
                    foreach (var p in pl.All())
                        if (p.type == def0.pen && CountIn(p.k) < AnimalDefs.PenCapacity(def0.pen)) { t.data.pen = p.k; break; }
                }
                var tr = t.body.transform;
                var pos = tr.position;
                var home = PenSpot(t.data.pen);
                if (t.data.pen < 0) home = pos;
                float speed = 0;
                if (!t.moving)
                {
                    t.wait -= dt;
                    if (t.wait <= 0)
                    {
                        float a = (float)_rng.NextDouble() * Mathf.PI * 2, r = night ? .6f : (float)_rng.NextDouble() * 3.2f;
                        t.target = home + new Vector3(Mathf.Cos(a) * r, 0, Mathf.Sin(a) * r);
                        t.moving = true;
                    }
                }
                else if (dt > 0)
                {
                    var to = t.target - pos; to.y = 0;
                    float len = to.magnitude;
                    if (len < .2f) { t.moving = false; t.wait = 2 + (float)_rng.NextDouble() * 5; }
                    else
                    {
                        var dir = to / len;
                        if (Step(d, ref pos, dir, Mathf.Min(len, t.body.Def.speed * dt), false))
                        {
                            speed = t.body.Def.speed;
                            t.rot = Mathf.LerpAngle(t.rot * Mathf.Rad2Deg, Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg, Mathf.Min(1, dt * 8)) * Mathf.Deg2Rad;
                        }
                        else { t.moving = false; t.wait = 1; }
                    }
                }
                pos.y = Ground(d, pos);
                tr.SetPositionAndRotation(pos, Quaternion.Euler(0, t.rot * Mathf.Rad2Deg, 0));
                t.body.Move(speed, dt, t.data.fed && !t.moving ? "Eating" : night ? "Idle_Headlow" : "Idle");
            }
        }

        /// <summary>Sang ngay moi (ban web: farmDaily): da an hom qua thi co san pham; tinh cam cao co the gap doi.</summary>
        void OnDayStarted(int day)
        {
            foreach (var a in Farm)
            {
                var def = AnimalDefs.Get(a.type);
                if (def == null) continue;
                a.ready = a.fed ? def.productN : 0;
                if (a.fed && a.love >= 60 && _rng.NextDouble() < .35) a.ready += def.productN;
                a.fed = false;
            }
            // thu cung ve canh nguoi choi (da ve leu)
            if (PetBody && S && S.Player) PetBody.transform.position = S.Player.transform.position + S.Player.transform.right;
            ClearWild();
        }

        /// <summary>Chi dung cho test: chay sang ngay moi cho vat nuoi.</summary>
        public void DailyForTest(int day) => OnDayStarted(day);

        string FeedItem(AnimalDef def)
        {
            var s = S;
            if (def.feed == "fiber") return s.Inventory.Has("fiber", def.feedN) ? "fiber" : null;
            foreach (var kv in s.Inventory.Stacks())
            {
                var it = s.Items.Get(kv.Key);
                if (it && it.category == ItemCategory.Crop) return kv.Key;
            }
            return null;
        }

        // ================================================================== phim E
        /// <summary>Viec lam voi con thu truoc mat (ban web: animalNear / animalInteract).</summary>
        public Action NearbyAction(Vector3 pos, float facing, out string label)
        {
            label = null;
            var s = S;
            var front = pos + new Vector3(Mathf.Sin(facing), 0, Mathf.Cos(facing)) * .9f;
            if (PetBox && Flat(PetBox.transform.position, front) < 1.4f) { label = "E: Mở thùng giấy"; return OpenPetBox; }
            object best = null; float bd = Reach;
            if (PetBody && Flat(PetBody.transform.position, front) < bd) { bd = Flat(PetBody.transform.position, front); best = PetBody; }
            foreach (var t in _farm) { float dd = Flat(t.body.transform.position, front); if (dd < bd) { bd = dd; best = t; } }
            foreach (var w in _wild) { float dd = Flat(w.body.transform.position, front); if (dd < bd) { bd = dd; best = w; } }
            if (best == null) return null;

            if (best is AnimalBody) { label = $"E: Vuốt ve {Pet.name}"; return PetThePet; }
            if (best is Tame tm)
            {
                var a = tm.data; var def = AnimalDefs.Get(a.type);
                if (a.ready > 0)
                {
                    label = $"E: {def.verb}";
                    return () =>
                    {
                        int n = a.ready; a.ready = 0;
                        s.Give(def.product, n);
                        Debris.Burst(tm.body.transform.position + Vector3.up * .5f, MaterialLibrary.Hex("#fff3c8"), 8, 2);
                    };
                }
                string food = !a.fed ? FeedItem(def) : null;
                if (food != null)
                {
                    label = $"E: Cho {def.name.ToLowerInvariant()} ăn ({(food == "fiber" ? $"{def.feedN} cỏ gai" : $"1 {s.Items.NameOf(food).ToLowerInvariant()}")})";
                    return () =>
                    {
                        s.Inventory.Take(food, food == "fiber" ? def.feedN : 1);
                        a.fed = true;
                        s.Say($"{def.name} ăn ngon lành. Sáng mai sẽ có {s.Items.NameOf(def.product).ToLowerInvariant()}.");
                    };
                }
                label = a.fed ? $"E: Vuốt ve {def.name.ToLowerInvariant()}" : $"{def.name} đói (cần {(def.feed == "fiber" ? $"{def.feedN} cỏ gai" : "1 nông sản")})";
                return () =>
                {
                    if (a.petDay != s.Clock.Day) { a.petDay = s.Clock.Day; a.love = Mathf.Min(100, a.love + 10); s.Stats.Calm = Mathf.Min(100, s.Stats.Calm + 2); }
                    Debris.Burst(tm.body.transform.position + Vector3.up * .6f, MaterialLibrary.Hex("#ff7a8a"), 6, 2);
                };
            }
            var w0 = (Wild)best;
            if (s.Inventory.Has("berry"))
            {
                label = $"E: Cho {w0.def.name.ToLowerInvariant()} ăn quả mọng";
                return () =>
                {
                    s.Inventory.Take("berry");
                    w0.tameT = 8; w0.moving = false; w0.wait = 8;
                    s.Stats.Calm = Mathf.Min(100, s.Stats.Calm + 3);
                    Debris.Burst(w0.body.transform.position + Vector3.up * .5f, MaterialLibrary.Hex("#ff7a8a"), 6, 2);
                    s.Say($"{w0.def.name} ăn quả mọng rồi ngoan ngoãn đứng yên.");
                };
            }
            label = $"E: Ngắm {w0.def.name.ToLowerInvariant()}";
            return () => { s.Stats.Calm = Mathf.Min(100, s.Stats.Calm + 1); s.Say(w0.def.name); };
        }

        // ================================================================== luu / tai
        public void Restore(PetData pet, List<FarmAnimal> farm, List<string> seen)
        {
            Pet = pet != null && !string.IsNullOrEmpty(pet.type) ? pet : null;
            if (PetBody) Destroy(PetBody.gameObject);
            PetBody = null;
            Farm.Clear();
            if (farm != null) foreach (var a in farm) if (a != null && AnimalDefs.Get(a.type) != null) Farm.Add(a);
            Seen.Clear();
            if (seen != null) foreach (var x in seen) Seen.Add(x);
        }
    }
}
