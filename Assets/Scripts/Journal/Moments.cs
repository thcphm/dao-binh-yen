// Khoanh khac binh yen (Docs/ThietKeGameplay.md muc 7.6): bo suu tap nhung luc nho, gap lan dau thi tu "chup" 1 tam anh
// vao so tay (anh may quay, khong co giao dien) va +10 binh yen. Anh luu canh file luu: <Dir>/photos/s<o>_<id>.jpg.
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Rendering;

namespace SereneIsle
{
    public class Moments : MonoBehaviour
    {
        public static Moments Instance { get; private set; }

        public sealed class Moment { public string id, name, hint; }

        static Moment M(string id, string name, string hint) => new Moment { id = id, name = name, hint = hint };

        public static readonly Moment[] All =
        {
            M("binh_minh", "Bình minh đầu tiên", "Đứng yên lúc mặt trời mọc"),
            M("hoang_hon_cau_tau", "Hoàng hôn trên cầu tàu", "Chiều tà, đứng trên cầu tàu"),
            M("lua_trai_mua", "Bên lửa trại nghe mưa", "Ngồi sưởi lửa trong cơn mưa"),
            M("dom_dom", "Đom đóm rừng đước", "Đêm quang, đi vào rừng đước"),
            M("trang_ram", "Trăng rằm nước lớn", "Đêm rằm, lúc nước lên cao"),
            M("lua_tro_bong", "Gió qua ruộng lúa trổ bông", "Đứng bên ruộng lúa sắp gặt"),
            M("rua_con", "Rùa con bò ra biển", "Ghé hòn đảo của rùa"),
            M("ca_vay_quanh", "Đàn cá vây quanh", "Lặn xuống rạn san hô"),
            M("hoa_dang", "Hoa đăng trôi", "Thả hoa đăng đêm rằm tháng giêng"),
            M("thu_nha", "Lá thư đầu tiên", "Nhận thư nhà từ ghe Bác Chín"),
        };

        public const int CalmReward = 10;
        public const int PhotoW = 384, PhotoH = 216;

        readonly Dictionary<string, int> _got = new Dictionary<string, int>(); // id -> ngay gap
        float _idle, _under;
        bool _capturing;

        GameSession S => GameSession.Instance;
        static IslandData D => IslandTerrain.Instance ? IslandTerrain.Instance.Data : null;

        public bool Has(string id) => _got.ContainsKey(id);
        public int DayOf(string id) => _got.TryGetValue(id, out var d) ? d : 0;
        public int Count => _got.Count;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { Instance = null; }

        void Awake() { Instance = this; }
        void OnDestroy() { if (Instance == this) Instance = null; }

        // ------------------------------------------------------------------ kiem tra moi khung
        void Update()
        {
            var s = S; var d = D;
            if (!s || !s.Active || !s.Player || d == null) return;
            var p = s.Player.transform.position;
            int i = Mathf.RoundToInt(p.x), j = Mathf.RoundToInt(p.z);
            if (!d.InBounds(i, j)) return;
            int k = d.Idx(i, j);
            float h = s.Clock.Hour;
            _idle = s.Player.Moving ? 0 : _idle + Time.deltaTime;
            var w = WeatherSystem.Instance;
            bool rain = w && w.IsRaining;
            bool clear = !(w && (w.IsRaining || w.Today == WeatherKind.Cloud));
            bool night = h >= 19.5f || h < 4.5f;

            if (_idle > 5 && h >= 5.5f && h < 7f) Try("binh_minh");
            if (_idle > 4 && h >= 17.3f && h < 19f && d.Pier[k] != 0) Try("hoang_hon_cau_tau");
            if (s.Sitting && rain && s.NearFire(3)) Try("lua_trai_mua");
            if (night && clear && d.Bio[k] == Biome.Mangrove) Try("dom_dom");
            if (night && clear && s.Date.IsFullMoon && TideClock.Level(s.Clock.Day, h) >= IslandData.SEA + TideClock.SpringAmp * .6f) Try("trang_ram");
            if (h >= 6 && h < 18 && NearRipeRice(d, i, j)) Try("lua_tro_bong");
            if (IsletSystem.Instance && IsletSystem.Instance.TurtlesSeen) Try("rua_con");
            var dive = DivingSystem.Instance;
            _under = dive && dive.Underwater ? _under + Time.deltaTime : 0;
            if (_under > 4) Try("ca_vay_quanh");
            if (Festivals.Instance && Festivals.Instance.Released > 0 && Festivals.Instance.Today == Festivals.RamGieng) Try("hoa_dang");
            if (Mail.Instance && Mail.Instance.Chapter > 0) Try("thu_nha");
        }

        static bool NearRipeRice(IslandData d, int i, int j)
        {
            var farm = GameSession.Instance ? GameSession.Instance.Farm : null;
            if (!farm) return false;
            for (int dj = -3; dj <= 3; dj++)
                for (int di = -3; di <= 3; di++)
                {
                    if (!d.InBounds(i + di, j + dj)) continue;
                    var pl = farm.PlotAt(d.Idx(i + di, j + dj));
                    if (pl == null || pl.dead || (pl.crop != "lua" && pl.crop != "nep")) continue;
                    var c = CropDatabase.Instance ? CropDatabase.Instance.Get(pl.crop) : null;
                    if (c && FarmRules.Stage(pl, c) >= 2) return true;
                }
            return false;
        }

        /// <summary>Gap khoanh khac (lan dau): ghi lai, +10 binh yen, chup anh.</summary>
        public bool Try(string id)
        {
            var s = S;
            if (!s || Has(id) || _capturing) return false;
            var m = Array.Find(All, x => x.id == id);
            if (m == null) return false;
            _got[id] = s.Clock.Day;
            s.Stats.Calm = Mathf.Min(100, s.Stats.Calm + CalmReward);
            s.Say($"Khoảnh khắc: \"{m.name}\" đã vào sổ tay (+{CalmReward} bình yên, bấm J)");
            Journal.Stat("moment");
            StartCoroutine(Capture(id));
            return true;
        }

        // ------------------------------------------------------------------ anh
        public static string PhotoDir => Path.Combine(SaveSystem.Dir, "photos");
        public string PhotoPath(string id) => Path.Combine(PhotoDir, $"s{(S ? S.SaveSlot : -1)}_{id}.jpg");

        IEnumerator Capture(string id)
        {
            _capturing = true;
            yield return null; // (WaitForEndOfFrame khong chay trong batchmode; render request khong can doi cuoi khung)
            try
            {
                var cam = Camera.main;
                if (cam)
                {
                    var bytes = Shoot(cam);
                    Directory.CreateDirectory(PhotoDir);
                    File.WriteAllBytes(PhotoPath(id), bytes);
                    SaveSystem.Flush();
                }
            }
            catch (Exception e) { Debug.LogWarning("[SereneIsle] Khong chup duoc anh khoanh khac: " + e.Message); }
            _capturing = false;
        }

        /// <summary>Ve may quay vao RenderTexture nho (khong co lop giao dien UI Toolkit) roi nen JPG.</summary>
        public static byte[] Shoot(Camera cam)
        {
            var rt = RenderTexture.GetTemporary(PhotoW, PhotoH, 24, RenderTextureFormat.ARGB32);
            var req = new RenderPipeline.StandardRequest { destination = rt };
            if (RenderPipeline.SupportsRenderRequest(cam, req)) RenderPipeline.SubmitRenderRequest(cam, req);
            else { var old = cam.targetTexture; cam.targetTexture = rt; cam.Render(); cam.targetTexture = old; }
            var prev = RenderTexture.active;
            RenderTexture.active = rt;
            var tex = new Texture2D(PhotoW, PhotoH, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, PhotoW, PhotoH), 0, 0);
            tex.Apply(false);
            RenderTexture.active = prev;
            RenderTexture.ReleaseTemporary(rt);
            var jpg = tex.EncodeToJPG(82);
            Destroy(tex);
            return jpg;
        }

        /// <summary>Doc anh da chup (null neu chua co). Nguoi goi tu huy texture.</summary>
        public Texture2D LoadPhoto(string id)
        {
            var path = PhotoPath(id);
            if (!File.Exists(path)) return null;
            var t = new Texture2D(2, 2, TextureFormat.RGB24, false) { name = "Moment_" + id };
            return t.LoadImage(File.ReadAllBytes(path)) ? t : null;
        }

        // ------------------------------------------------------------------ luu / tai
        public List<ItemCount> Snapshot()
        {
            var l = new List<ItemCount>();
            foreach (var kv in _got) l.Add(new ItemCount { id = kv.Key, n = kv.Value });
            return l;
        }

        public void Restore(List<ItemCount> list)
        {
            _got.Clear();
            if (list != null) foreach (var c in list) if (!string.IsNullOrEmpty(c.id)) _got[c.id] = c.n;
        }
    }
}
