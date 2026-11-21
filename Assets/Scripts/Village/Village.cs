// Lang chai (giai doan 4): dung nha (VillageBuilder), dan lang (NpcDefs), noi chuyen (phim E), cua hang (ShopUI),
// den hai dang ban dem, ghe thung Bac Chin noi theo con nuoc. Dan lang ve nha ngu ban dem (an di).
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SereneIsle
{
    public class Village : MonoBehaviour
    {
        public static Village Instance { get; private set; }

        public readonly List<VillageBuilding> Buildings = new List<VillageBuilding>();
        public readonly List<Npc> Npcs = new List<Npc>();
        public bool Built { get; private set; }
        public Transform Boat { get; private set; }
        public Light Beacon { get; private set; }

        public const float TalkRange = 2.1f;
        public const string MetBay = "met_bay";

        readonly System.Random _rng = new System.Random();
        readonly HashSet<string> _greeted = new HashSet<string>();
        int _greetDay = -1;
        float _callT;
        Npc _talking;
        Renderer _beaconGlow;

        GameSession S => GameSession.Instance;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { Instance = null; }

        void Awake()
        {
            Instance = this;
            if (!GetComponent<Market>()) gameObject.AddComponent<Market>();
        }
        void OnDestroy() { if (Instance == this) Instance = null; }

        // ------------------------------------------------------------------ hoc nghe (luu trong file)
        readonly Dictionary<string, int> _crafts = new Dictionary<string, int>();
        public int CraftLevel(string id) => _crafts.TryGetValue(id, out var l) ? l : 0;
        public void SetCraftLevel(string id, int level) => _crafts[id] = Mathf.Clamp(level, 0, Crafts.MaxLevel);
        public IReadOnlyDictionary<string, int> CraftLevels => _crafts;

        public VillageBuilding Building(string id) => Buildings.Find(b => b.id == id);
        public Npc Find(string id) => Npcs.Find(n => n.def.id == id);

        // ------------------------------------------------------------------ dung lang
        /// <summary>Goi khi dao sinh xong (truoc khi tai do da dat tu file luu).</summary>
        public void Build()
        {
            var it = IslandTerrain.Instance;
            if (Built || !it || it.Data == null) return;
            Built = true;
            var d = it.Data;
            Buildings.AddRange(VillageBuilder.Build(d, it.transform, it.Vegetation));

            foreach (var def in NpcDefs.All)
            {
                var b = Building(def.building);
                if (b == null) continue;
                Npcs.Add(Spawn(def, b));
            }

            // ghe thung Bac Chin canh cau tau
            Vector2 bp = d.TownPoint(-1.7f, 12.5f);
            var boat = new GameObject("BasketBoat");
            boat.transform.SetParent(transform, false);
            boat.transform.position = new Vector3(bp.x, d.WaterLevel, bp.y);
            boat.transform.rotation = Quaternion.Euler(0, d.TownAngle * Mathf.Rad2Deg + 30, 0);
            boat.AddComponent<MeshFilter>().sharedMesh = VillageBuilder.MeshOf("basketboat");
            boat.AddComponent<MeshRenderer>().sharedMaterial = MaterialLibrary.Objects(false);
            Boat = boat.transform;

            // den hai dang
            var lh = Building("lighthouse");
            if (lh != null)
            {
                var lg = new GameObject("Beacon");
                lg.transform.SetParent(lh.go.transform, false);
                lg.transform.localPosition = new Vector3(0, 4.25f, 0);
                Beacon = lg.AddComponent<Light>();
                Beacon.type = LightType.Point;
                Beacon.color = new Color(1f, .85f, .55f);
                Beacon.range = 11;
                Beacon.shadows = LightShadows.None;
                Beacon.intensity = 0;
                Beacon.enabled = false;
                var glow = new GameObject("BeaconGlow");
                glow.transform.SetParent(lg.transform, false);
                glow.transform.localScale = Vector3.one * .38f;
                glow.AddComponent<MeshFilter>().sharedMesh = GlowBall;
                _beaconGlow = glow.AddComponent<MeshRenderer>();
                _beaconGlow.sharedMaterial = GlowMat;
                _beaconGlow.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                _beaconGlow.enabled = false;
            }
            Debug.Log($"[SereneIsle] Lang: {Buildings.Count} nha, {Npcs.Count} dan lang");
        }

        Npc Spawn(NpcDef def, VillageBuilding b)
        {
            var go = new GameObject("NPC_" + def.id);
            go.SetActive(false);
            go.transform.SetParent(transform, false);
            var body = go.AddComponent<IslandCharacter>();
            body.outfit = def.outfit.Clone();
            var npc = go.AddComponent<Npc>();
            npc.def = def;
            npc.home = b.door;
            npc.homeFacing = b.facing;
            go.SetActive(true);
            if (def.hat) body.AttachToHead(HatMesh, MaterialLibrary.Objects(false));
            npc.PlaceHome();
            return npc;
        }

        // ------------------------------------------------------------------ moi khung
        void Update()
        {
            var s = S;
            if (!Built || !s) return;
            float hour = s.Clock.Hour;
            var d = IslandTerrain.Instance ? IslandTerrain.Instance.Data : null;

            // dan lang co mat theo gio
            foreach (var n in Npcs)
            {
                bool on = hour >= n.def.fromHour && hour < n.def.toHour;
                if (n.gameObject.activeSelf != on)
                {
                    n.gameObject.SetActive(on);
                    if (on) n.PlaceHome();
                }
            }

            // dang noi chuyen: chi khi hop thoai / cua hang con mo
            bool ui = (ActionUI.Instance && ActionUI.Instance.DialogOpen) || (ShopUI.Instance && ShopUI.Instance.IsOpen)
                      || (ApprenticeUI.Instance && ApprenticeUI.Instance.IsOpen);
            if (!ui && _talking) { _talking.Talking = false; _talking = null; }

            // ghe thung noi theo con nuoc
            if (Boat && d != null)
            {
                var bp = Boat.position;
                float ground = d.HeightAt(bp.x, bp.z);
                bp.y = Mathf.Max(ground, d.WaterLevel - .05f) + Mathf.Sin(Time.time * 1.3f) * .03f;
                Boat.position = bp;
            }

            // den hai dang
            if (Beacon)
            {
                float night = Placeables.NightFactor(hour);
                bool on = night > .02f;
                Beacon.enabled = on;
                _beaconGlow.enabled = on;
                if (on) Beacon.intensity = 2.6f * night;
            }

            Greet(s);
        }

        /// <summary>Chao khi nguoi choi di ngang (moi nguoi 1 lan / ngay); ong Bay goi lai neu chua gap.</summary>
        void Greet(GameSession s)
        {
            if (!s.Player || !s.Active) return;
            if (_greetDay != s.Clock.Day) { _greetDay = s.Clock.Day; _greeted.Clear(); }
            var p = s.Player.transform.position;
            _callT -= Time.deltaTime;
            foreach (var n in Npcs)
            {
                if (!n.gameObject.activeInHierarchy || n.Talking) continue;
                float dist = Flat(p, n.transform.position);
                if (n.def.id == "bay" && !s.Flags.Contains(MetBay))
                {
                    if (dist < 9 && _callT <= 0) { _callT = 9; ActionUI.Bubble(n.transform, "Ê con! Lại đây ông nói cái ni.", 4); }
                    continue;
                }
                if (dist < 4 && _greeted.Add(n.def.id))
                    ActionUI.Bubble(n.transform, Greeting(n.def, s.Clock.Hour), 3.5f);
            }
        }

        static string Greeting(NpcDef n, float hour)
        {
            string buoi = hour < 11 ? "sáng" : hour < 14 ? "trưa" : hour < 18 ? "chiều" : "tối";
            return n.id switch
            {
                "bay" => $"Chào con. Buổi {buoi} ni ra biển chưa?",
                "chin" => $"Chào con! Buổi {buoi} mát quá ha.",
                "sau" => $"Ghé coi hạt giống không con?",
                "tam" => $"Vô đây con, tiệm có đồ ăn nè.",
                _ => "Chào con!",
            };
        }

        // ------------------------------------------------------------------ noi chuyen
        public Npc Nearest(Vector3 pos, float range = TalkRange)
        {
            Npc best = null;
            float bd = range;
            foreach (var n in Npcs)
            {
                if (!n.gameObject.activeInHierarchy) continue;
                float dd = Flat(pos, n.transform.position);
                if (dd < bd) { bd = dd; best = n; }
            }
            return best;
        }

        /// <summary>Phim E gan dan lang (ToolSystem hoi truoc leu / ban che tao).</summary>
        public Action NearbyAction(Vector3 pos, out string label)
        {
            label = null;
            var n = Nearest(pos);
            if (n)
            {
                label = $"E: Nói chuyện với {n.def.name}";
                return () => Talk(n);
            }
            var cho = Building("cho");
            if (cho != null && (Flat(pos, cho.door) < 2.2f || Flat(pos, cho.center) < 2.4f))
            {
                var m = Market.Instance;
                label = m && m.CanSetUp ? "E: Bày sạp chợ phiên" : "E: Nhà lồng chợ";
                return OpenMarket;
            }
            return null;
        }

        void OpenMarket()
        {
            var m = Market.Instance;
            if (m && m.CanSetUp && MarketUI.Instance) { MarketUI.Instance.Open(); return; }
            var s = S;
            int wait = 1;
            while (!Market.IsMarketDay(s.Clock.Day + wait) && wait < 7) wait++;
            s.Say(wait == 1 ? "Ngày mai họp chợ phiên (thứ 7), từ 7 giờ đến 17 giờ. Chuẩn bị nông sản, cá, bọ mà bán nghen." :
                  $"Chợ phiên họp ngày thứ 7 mỗi tuần, còn {wait} ngày nữa.");
        }

        public void Talk(Npc n)
        {
            var s = S; var ui = ActionUI.Instance;
            if (!s || !ui || !n) return;
            _talking = n;
            n.Talking = true;
            ActionUI.ClearBubble();
            if (n.def.id == "bay" && !s.Flags.Contains(MetBay)) { Intro(n, 0); return; }
            var buttons = new List<(string, Action)>();
            if (n.def.shop != null)
            {
                var shop = n.def.shop;
                buttons.Add((shop == Shops.TapHoa ? "Mua bán" : "Mua hạt giống", () => OpenShop(n, shop)));
            }
            if (ApprenticeUI.Instance && System.Linq.Enumerable.Any(Crafts.TaughtBy(n.def.id)))
                buttons.Add(("Học nghề", () => { _talking = n; n.Talking = true; ApprenticeUI.Instance.Open(n.def.id); }));
            buttons.Add(("Tạm biệt", null));
            ui.ShowDialog(n.def.name, NpcDefs.Line(n.def, s, _rng), buttons.ToArray());
        }

        void OpenShop(Npc n, string shop)
        {
            if (!ShopUI.Instance) return;
            _talking = n;
            n.Talking = true;
            ShopUI.Instance.Open(shop);
        }

        void Intro(Npc n, int page)
        {
            var ui = ActionUI.Instance;
            var lines = NpcDefs.BayIntro;
            if (page >= lines.Length - 1)
            {
                ui.ShowDialog(n.def.name, lines[lines.Length - 1], ("Dạ, con cảm ơn ông", () => S.Flags.Add(MetBay)));
                S.Flags.Add(MetBay);
                return;
            }
            ui.ShowDialog(n.def.name, lines[page], ("Tiếp", () => Intro(n, page + 1)));
        }

        static float Flat(Vector3 a, Vector3 b) => Vector2.Distance(new Vector2(a.x, a.z), new Vector2(b.x, b.z));

        // ------------------------------------------------------------------ tai nguyen
        static Mesh _hat, _glowBall;
        static Material _glowMat;

        /// <summary>Non la: non tre vang nhat + vanh dam.</summary>
        public static Mesh HatMesh
        {
            get
            {
                if (_hat) return _hat;
                var b = new MeshBuilder();
                b.P(LowPolyShapes.Cone(.34f, .2f, 16), "#e6d3a0", 0, .1f, 0);
                b.P(LowPolyShapes.Cylinder(.345f, .345f, .012f, 16), "#c8b07a", 0, .006f, 0);
                b.P(LowPolyShapes.Cylinder(.25f, .25f, .01f, 16), "#d8c088", 0, .07f, 0);
                return _hat = b.ToMesh("NonLa");
            }
        }

        static Mesh GlowBall
        {
            get
            {
                if (_glowBall) return _glowBall;
                var m = Instantiate(LowPolyShapes.Sphere86);
                var c = new Color[m.vertexCount];
                for (int i = 0; i < c.Length; i++) c[i] = Color.white;
                m.colors = c;
                return _glowBall = m;
            }
        }

        static Material GlowMat
        {
            get
            {
                if (_glowMat) return _glowMat;
                _glowMat = new Material(Shader.Find("SereneIsle/Glow")) { name = "SI_BeaconGlow", renderQueue = 3000 };
                _glowMat.SetColor("_BaseColor", new Color(1f, .9f, .6f, .55f));
                _glowMat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.One);
                _glowMat.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.One);
                return _glowMat;
            }
        }
    }
}
