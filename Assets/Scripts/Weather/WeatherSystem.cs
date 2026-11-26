// Thoi tiet (ban web: WEA / rollWeather dong 1205-1213; Docs/ThietKeGameplay.md muc 4.1, 7.4, 8).
//  - Moi ngay 1 kieu: Nang, Nhieu may, Mua, Giong bao. Ti le theo mua am lich (Xuan mua phun, Ha nang, Thu mua bao,
//    Dong kho). Tinh tu seed dao + so ngay -> khong can luu, tai game van dung.
//  - Bao: 1-2 con moi mua Thu (thang 5-6), co dinh theo seed + nam; radio ong Bay bao truoc 2 ngay.
//  - Mua / bao: troi xam, hat mua, gio lay cay; bao co chop. Sang hom mua: ruong duoc tuoi (FarmSystem.WaterAllByRain).
//    Sang sau dem bao: cay ngoai troi hu (FarmSystem.ApplyStorm), go troi dat vao bai. Bao: cho nghi, dan lang o nha.
using System;
using UnityEngine;

namespace SereneIsle
{
    public enum WeatherKind { Sun = 0, Cloud = 1, Rain = 2, Storm = 3 }

    public class WeatherSystem : MonoBehaviour
    {
        public static WeatherSystem Instance { get; private set; }

        public WeatherKind Today { get; private set; }
        public WeatherKind Tomorrow => Of(Day + 1);
        public bool IsRaining => Today == WeatherKind.Rain || Today == WeatherKind.Storm;
        public bool IsStorm => Today == WeatherKind.Storm;
        /// <summary>Cuong do mua hien tai 0..1 (mua tung con).</summary>
        public float RainNow { get; private set; }
        /// <summary>Test / su kien: ep thoi tiet hom nay (null = theo lich).</summary>
        public WeatherKind? Override { get; set; }

        public static readonly string[] Names = { "Nắng đẹp", "Nhiều mây", "Mưa", "Giông bão" };
        public static string NameOf(WeatherKind w) => Names[(int)w];

        RainFX _rain;
        float _overcast, _wind, _flashT = 6, _flash;
        int _day = -1;
        bool _hooked;
        readonly System.Random _rng = new System.Random();

        GameSession S => GameSession.Instance;
        int Seed => IslandTerrain.Instance ? IslandTerrain.Instance.seed : 0;
        int Day => S ? S.Clock.Day : 1;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { Instance = null; }

        void Awake()
        {
            Instance = this;
            _rain = new GameObject("Rain").AddComponent<RainFX>();
            _rain.transform.SetParent(transform, false);
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
            if (_hooked && S) S.DayStarted -= OnDayStarted;
        }

        // ------------------------------------------------------------------ lich thoi tiet (thuan, test duoc)
        static uint Hash(int a, int b)
        {
            unchecked
            {
                uint h = (uint)a * 0x9E3779B1u ^ (uint)b * 0x85EBCA77u;
                h ^= h >> 15; h *= 0x2C1B3C6Du; h ^= h >> 12; h *= 0x297A2D39u; h ^= h >> 15;
                return h;
            }
        }

        /// <summary>Ti le [nang, may, mua] theo mua (Docs muc 4.2: Xuan mua phun, Ha nang gat, Thu mua bao, Dong kho).</summary>
        public static float[] Odds(Season s) => s switch
        {
            Season.Xuan => new[] { .45f, .30f, .25f },
            Season.Ha => new[] { .62f, .20f, .18f },
            Season.Thu => new[] { .30f, .28f, .42f },
            _ => new[] { .55f, .35f, .10f },
        };

        /// <summary>Ngay bao trong nam (1-2 con, thang 5-6 = mua Thu), co dinh theo seed.</summary>
        public static bool IsStormDay(int seed, int day)
        {
            var d = LunarCalendar.Date(day);
            if (d.Season != Season.Thu) return false;
            uint h = Hash(seed, d.Year * 31 + 7);
            int count = 1 + (int)(h % 2);
            int thuStart = (d.Year - 1) * LunarCalendar.DaysPerYear + 2 * LunarCalendar.DaysPerSeason + 1; // mung 1 thang 5
            for (int i = 0; i < count; i++)
            {
                // tranh 4 ngay dau mua; 2 con cach nhau it nhat 10 ngay
                int off = 4 + (int)(Hash(seed + i * 977, d.Year) % (uint)(LunarCalendar.DaysPerSeason / count - 10)) + i * (LunarCalendar.DaysPerSeason / count);
                if (day == thuStart + off) return true;
            }
            return false;
        }

        public static WeatherKind Roll(int seed, int day)
        {
            if (IsStormDay(seed, day)) return WeatherKind.Storm;
            if (IsStormDay(seed, day - 1)) return WeatherKind.Rain;             // mua tan sau bao
            if (IsStormDay(seed, day + 1)) return WeatherKind.Cloud;            // may keo den truoc bao
            var o = Odds(LunarCalendar.Date(day).Season);
            float r = (Hash(seed, day) % 10000u) / 10000f;
            if (r < o[0]) return WeatherKind.Sun;
            if (r < o[0] + o[1]) return WeatherKind.Cloud;
            return WeatherKind.Rain;
        }

        public WeatherKind Of(int day) => day == Day && Override.HasValue ? Override.Value : Roll(Seed, day);

        /// <summary>So ngay toi con bao gan nhat (0 = hom nay), -1 neu khong co trong 7 ngay.</summary>
        public int DaysToStorm()
        {
            for (int i = 0; i <= 7; i++) if (Of(Day + i) == WeatherKind.Storm) return i;
            return -1;
        }

        /// <summary>Cau radio ong Bay (bao truoc 2 ngay; Docs muc 7.4).</summary>
        public string RadioLine()
        {
            int ds = DaysToStorm();
            if (ds == 0) return "Radio báo bão đang vô đảo. Ở trong nhà, đừng ra biển nghe con!";
            if (ds == 1) return "Radio báo mai bão vô. Thu hoạch sớm, buộc lều cho chắc, kéo ghe lên bờ nghe con.";
            if (ds == 2) return "Radio báo hai bữa nữa có bão. Lo mà chuẩn bị đi con.";
            return Tomorrow switch
            {
                WeatherKind.Rain => "Radio báo mai trời mưa. Khỏi tưới rau, mà nhớ mang áo mưa.",
                WeatherKind.Cloud => "Radio báo mai nhiều mây, mát trời, đi đâu cũng dễ chịu.",
                _ => "Radio báo mai trời nắng đẹp. Phơi muối, phơi cá là khô liền.",
            };
        }

        // ------------------------------------------------------------------ qua ngay
        void OnDayStarted(int day)
        {
            var s = S;
            Override = null;
            var today = Of(day);
            var yesterday = Of(day - 1);
            if (yesterday == WeatherKind.Storm) AfterStorm();
            if (today == WeatherKind.Rain || today == WeatherKind.Storm) RainWater();
            if (today == WeatherKind.Storm) s.Say("Giông bão! Chợ nghỉ, dân làng ở trong nhà. Đừng ra xa, về lều sớm nghe.");
            else if (Of(day + 1) == WeatherKind.Storm || Of(day + 2) == WeatherKind.Storm) s.Say("Radio ông Bảy: bão sắp vào đảo. Thu hoạch sớm, chuẩn bị đi!");
            else if (today == WeatherKind.Rain) s.Say("Trời mưa: ruộng vườn được tưới rồi.");
        }

        void RainWater()
        {
            var farm = S ? S.Farm : null;
            if (!farm) return;
            farm.WaterAllByRain();
        }

        void AfterStorm()
        {
            var s = S; var d = IslandTerrain.Instance ? IslandTerrain.Instance.Data : null;
            var farm = s ? s.Farm : null;
            var (killed, lost) = farm ? farm.ApplyStorm(_rng) : (0, 0);
            if (killed + lost == 0) s?.Say("Đêm qua bão quét qua đảo."); // ApplyStorm tu bao khi co cay hu
            // go troi dat vao bai bien quanh lang (Docs muc 7.4: sau bao bien dat vao go troi)
            if (d == null || !s.Player) return;
            int n = 0;
            for (int t = 0; t < 400 && n < 6; t++)
            {
                float a = (float)_rng.NextDouble() * Mathf.PI * 2, r = 8 + (float)_rng.NextDouble() * 30;
                float x = d.CX + Mathf.Sin(a) * r, z = d.CZ + Mathf.Cos(a) * r;
                int i = Mathf.RoundToInt(x), j = Mathf.RoundToInt(z);
                if (!d.InBounds(i, j)) continue;
                int k = d.Idx(i, j);
                if (d.Bio[k] != Biome.Beach || d.Solid[k] != 0 || d.WaterDepth(x, z) > 0) continue;
                ItemPile.Spawn("wood", 1 + _rng.Next(3), new Vector3(x, d.GroundHeight(x, z), z));
                n++;
            }
            if (n > 0) s.Say($"Sau bão, sóng đánh gỗ trôi dạt vào bãi ({n} đống). Ra nhặt đi!");
        }

        // ------------------------------------------------------------------ moi khung
        void Update()
        {
            var s = S;
            if (!s) return;
            if (!_hooked) { s.DayStarted += OnDayStarted; _hooked = true; }
            if (_day != s.Clock.Day) { _day = s.Clock.Day; }
            Today = Of(s.Clock.Day);

            // mua tung con trong ngay (mua dai, ngot ngat)
            float h = s.Clock.Hour;
            float baseRain = Today == WeatherKind.Storm ? 1f : Today == WeatherKind.Rain ? .55f : 0f;
            float shower = Today == WeatherKind.Storm ? 1 : Mathf.Clamp01(.55f + .45f * Mathf.Sin(h * 1.3f + Day * 2.1f) + .25f * Mathf.Sin(h * 3.7f));
            RainNow = baseRain * shower;
            float targetOver = Today switch { WeatherKind.Storm => 1f, WeatherKind.Rain => .55f + .3f * shower, WeatherKind.Cloud => .42f, _ => 0f };
            float targetWind = Today switch { WeatherKind.Storm => 1f, WeatherKind.Rain => .3f, WeatherKind.Cloud => .15f, _ => .05f };
            float dt = Time.deltaTime;
            _overcast = Mathf.MoveTowards(_overcast, targetOver, dt * .15f);
            _wind = Mathf.MoveTowards(_wind, targetWind, dt * .2f);

            // chop (bao)
            if (Today == WeatherKind.Storm && s.Active)
            {
                _flashT -= dt;
                if (_flashT <= 0)
                {
                    _flashT = 6 + (float)_rng.NextDouble() * 10;
                    _flash = 1;
                    Thunder?.Invoke();
                }
            }
            // gioi han dt: khung hinh giat (dt lon) khong xoa mat tia chop truoc khi kip hien
            _flash = Mathf.MoveTowards(_flash, 0, Mathf.Min(dt, .05f) * 4);

            var env = s.Environment;
            if (env) { env.overcast = _overcast; env.wind = _wind; env.flash = _flash; }
            _rain.intensity = s.Running ? RainNow : 0;
        }

        /// <summary>Tieng sam (giai doan 6d: am thanh).</summary>
        public static event Action Thunder;

        /// <summary>Test: dat ngay muc tieu ngay (bo qua chuyen tu tu).</summary>
        public void SnapVisuals()
        {
            var s = S;
            Today = Of(Day);
            _overcast = Today switch { WeatherKind.Storm => 1f, WeatherKind.Rain => .8f, WeatherKind.Cloud => .42f, _ => 0f };
            _wind = Today switch { WeatherKind.Storm => 1f, WeatherKind.Rain => .3f, WeatherKind.Cloud => .15f, _ => .05f };
            if (s && s.Environment) { s.Environment.overcast = _overcast; s.Environment.wind = _wind; }
            if (_rain && !IsRaining) { _rain.intensity = 0; _rain.ClearNow(); }
        }

        /// <summary>Danh 1 tia chop ngay (test / su kien).</summary>
        public void FlashNow() { _flash = 1; Thunder?.Invoke(); }

        /// <summary>Test: chay sang ngay moi.</summary>
        public void DayStartForTest(int day) => OnDayStarted(day);
    }
}
