// Am thanh cua game: nhac lofi tu tong hop (ban web dong 1951-1979: hop am Rhodes, bass, trong swing nhe, tieng dia than),
// hieu ung (SFX ban web dong 1177-1180) va am nen (song bien, mua, gio bao, sam, lua trai, de dem).
// Tat ca "ve" san thanh AudioClip luc khoi dong (nhac ve dan tung o nhip de khong giat).
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static SereneIsle.Synth;

namespace SereneIsle
{
    public enum Sfx { Chop, Mine, Fell, Coin, Splash, Bite, Catch, Till, Water, Pop, Err, Swish, Build, Thunder }

    public class GameAudio : MonoBehaviour
    {
        public static GameAudio Instance { get; private set; }

        /// <summary>Choi 1 hieu ung (an toan khi chua co GameAudio, vd trong test logic).</summary>
        public static void Play(Sfx s, float vol = 1) { if (Instance) Instance.PlayOne(s, vol); }

        // ---- nhac: 3 tam trang nhu ban web ----
        sealed class Mood { public float bpm; public int[][] ch; public int[] bass; public bool drums; public float sparkle; }
        static readonly Dictionary<string, Mood> Moods = new Dictionary<string, Mood>
        {
            ["day"] = new Mood { bpm = 76, drums = true, sparkle = .16f, bass = new[] { 41, 40, 38, 36 },
                ch = new[] { new[] { 53, 57, 60, 64, 67 }, new[] { 52, 55, 59, 62 }, new[] { 50, 53, 57, 60, 64 }, new[] { 48, 52, 55, 59, 62 } } },
            ["night"] = new Mood { bpm = 64, drums = true, sparkle = .16f, bass = new[] { 33, 29, 36, 31 },
                ch = new[] { new[] { 45, 52, 55, 60, 64 }, new[] { 41, 48, 52, 57, 60 }, new[] { 48, 52, 55, 59, 64 }, new[] { 43, 50, 53, 57, 62 } } },
            ["intro"] = new Mood { bpm = 58, drums = false, sparkle = .12f, bass = new[] { 33, 29, 26, 28 },
                ch = new[] { new[] { 45, 48, 52, 55 }, new[] { 41, 45, 48, 52 }, new[] { 38, 45, 48, 53 }, new[] { 40, 44, 47, 50 } } },
        };
        public const int Bars = 8;
        const float MusicGain = .42f;

        readonly Dictionary<Sfx, AudioClip> _sfx = new Dictionary<Sfx, AudioClip>();
        readonly Dictionary<string, AudioClip> _music = new Dictionary<string, AudioClip>();
        AudioSource _one, _musA, _musB, _waves, _rain, _wind, _fire, _crickets;
        string _playing;
        float _xfade = 1, _fireV, _wavesV, _probeT;
        bool _ready;

        public bool Ready => _ready;
        public string CurrentMood => _playing;
        public bool HasMusic(string mood) => _music.ContainsKey(mood);
        public int SfxCount => _sfx.Count;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { Instance = null; }

        void Awake()
        {
            Instance = this;
            _one = Src(false);
            _musA = Src(true); _musB = Src(true);
            _waves = Src(true); _rain = Src(true); _wind = Src(true); _fire = Src(true); _crickets = Src(true);
        }

        void OnEnable() { WeatherSystem.Thunder += OnThunder; }
        void OnDisable() { WeatherSystem.Thunder -= OnThunder; }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
            foreach (var c in _sfx.Values) if (c) Destroy(c);
            foreach (var c in _music.Values) if (c) Destroy(c);
            foreach (var s in new[] { _waves, _rain, _wind, _fire, _crickets }) if (s && s.clip) Destroy(s.clip);
        }

        AudioSource Src(bool loop)
        {
            var s = gameObject.AddComponent<AudioSource>();
            s.playOnAwake = false; s.loop = loop; s.spatialBlend = 0; s.volume = 0;
            return s;
        }

        IEnumerator Start()
        {
            BuildSfx();
            yield return null;
            Ambient(_waves, "Waves", Waves());
            Ambient(_rain, "Rain", Rain());
            yield return null;
            Ambient(_wind, "Wind", Wind());
            Ambient(_fire, "Fire", Fire());
            Ambient(_crickets, "Crickets", Crickets());
            _ready = true;
            // nhac: ve tam trang dang can truoc, cac cai con lai sau
            string first = WantMood();
            yield return RenderMusic(first);
            foreach (var m in Moods.Keys) if (m != first) yield return RenderMusic(m);
        }

        static void Ambient(AudioSource s, string name, float[] data)
        {
            s.clip = ToClip(name, data, true);
            s.Play();
        }

        // ------------------------------------------------------------------ hieu ung
        void BuildSfx()
        {
            void Make(Sfx id, float len, Action<float[]> draw)
            {
                var b = new float[Len(len)];
                draw(b);
                _sfx[id] = ToClip("SFX_" + id, b);
            }
            Make(Sfx.Chop, .14f, b => { Noise(b, 0, .12f, .35f, 500); Tone(b, 0, 150, .08f, Wave.Square, .05f, 90); });
            Make(Sfx.Mine, .1f, b => { Tone(b, 0, 1100, .07f, Wave.Square, .05f, 700); Noise(b, 0, .08f, .2f, 2600, 3); });
            Make(Sfx.Fell, .52f, b => Noise(b, 0, .5f, .3f, 300));
            Make(Sfx.Coin, .23f, b => { Tone(b, 0, 988, .07f, Wave.Square, .05f); Tone(b, .07f, 1319, .14f, Wave.Square, .05f); });
            Make(Sfx.Splash, .37f, b => Noise(b, 0, .35f, .25f, 1400));
            Make(Sfx.Bite, .21f, b => { Tone(b, 0, 700, .08f, Wave.Triangle, .18f, 950); Tone(b, .11f, 700, .08f, Wave.Triangle, .18f, 950); });
            Make(Sfx.Catch, .36f, b => { float[] f = { 523, 659, 784, 1046 }; for (int i = 0; i < 4; i++) Tone(b, i * .07f, f[i], .13f, Wave.Triangle, .12f); });
            Make(Sfx.Till, .14f, b => Noise(b, 0, .12f, .25f, 350));
            Make(Sfx.Water, .32f, b => Noise(b, 0, .3f, .12f, 3200, .6f));
            Make(Sfx.Pop, .1f, b => Tone(b, 0, 520, .08f, Wave.Sine, .14f, 900));
            Make(Sfx.Err, .16f, b => Tone(b, 0, 220, .14f, Wave.Square, .04f, 160));
            Make(Sfx.Swish, .14f, b => Noise(b, 0, .12f, .12f, 1800, .8f));
            Make(Sfx.Build, .18f, b => { Tone(b, 0, 300, .06f, Wave.Square, .06f); Tone(b, .08f, 450, .08f, Wave.Square, .06f); });
            Make(Sfx.Thunder, 4.2f, b =>
            {
                Noise(b, 0, .35f, .5f, 900, .6f);                                       // tieng nut
                Noise(b, .05f, 4f, .9f, 110, .7f, Filter.Lowpass, false, 4000, .002f);  // rung ram
                Noise(b, .6f, 2.6f, .5f, 70, .7f, Filter.Lowpass, false, 9000, .002f);
            });
        }

        void PlayOne(Sfx s, float vol)
        {
            if (!_sfx.TryGetValue(s, out var c)) return;
            _one.PlayOneShot(c, Mathf.Clamp01(vol) * GameSettings.Current.sfx);
        }

        void OnThunder()
        {
            if (_sfx.ContainsKey(Sfx.Thunder)) StartCoroutine(Delayed(Sfx.Thunder, UnityEngine.Random.Range(.3f, 1.4f), 1));
        }

        IEnumerator Delayed(Sfx s, float t, float vol)
        {
            yield return new WaitForSeconds(t);
            PlayOne(s, vol);
        }

        // ------------------------------------------------------------------ am nen
        static float[] Waves()
        {
            const float L = 8;
            var b = new float[Len(L + 1)];
            var lp = new Biquad(Filter.Lowpass, 520, .6f);
            var r = new System.Random(7);
            float brown = 0;
            for (int i = 0; i < b.Length; i++)
            {
                float t = i / (float)Rate;
                brown = brown * .985f + (float)(r.NextDouble() * 2 - 1) * .15f;
                float swell = .45f + .35f * Mathf.Sin(2 * Mathf.PI * t / L) + .2f * Mathf.Sin(2 * Mathf.PI * 2 * t / L + 1.3f);
                b[i] = lp.Process(brown) * Mathf.Max(.08f, swell);
            }
            var o = LoopCrossfade(b, 1);
            Normalize(o, .5f);
            return o;
        }

        static float[] Rain()
        {
            var b = new float[Len(5)];
            var hp = new Biquad(Filter.Highpass, 1100, .7f);
            var lp = new Biquad(Filter.Lowpass, 7000, .7f);
            var r = new System.Random(11);
            for (int i = 0; i < b.Length; i++) b[i] = lp.Process(hp.Process((float)(r.NextDouble() * 2 - 1))) * .35f;
            for (int k = 0; k < 140; k++) Noise(b, (float)r.NextDouble() * 4.9f, .03f, .25f + (float)r.NextDouble() * .2f, 1800 + (float)r.NextDouble() * 2500, 4, Filter.Bandpass, false, r.Next(20000));
            var o = LoopCrossfade(b, 1);
            Normalize(o, .5f);
            return o;
        }

        static float[] Wind()
        {
            const float L = 6;
            var b = new float[Len(L + 1)];
            var lp = new Biquad(Filter.Lowpass, 380, .9f);
            var r = new System.Random(13);
            for (int i = 0; i < b.Length; i++)
            {
                float t = i / (float)Rate;
                float gust = .5f + .35f * Mathf.Sin(2 * Mathf.PI * t / L) + .15f * Mathf.Sin(2 * Mathf.PI * 3 * t / L);
                b[i] = lp.Process((float)(r.NextDouble() * 2 - 1)) * gust;
            }
            var o = LoopCrossfade(b, 1);
            Normalize(o, .5f);
            return o;
        }

        static float[] Fire()
        {
            var b = new float[Len(5)];
            var lp = new Biquad(Filter.Lowpass, 260, .7f);
            var r = new System.Random(17);
            for (int i = 0; i < b.Length; i++) b[i] = lp.Process((float)(r.NextDouble() * 2 - 1)) * .5f;
            for (int k = 0; k < 70; k++)
                Noise(b, (float)r.NextDouble() * 4.9f, .02f + (float)r.NextDouble() * .04f, .3f + (float)r.NextDouble() * .5f, 1400 + (float)r.NextDouble() * 3000, 2, Filter.Bandpass, false, r.Next(20000));
            var o = LoopCrossfade(b, 1);
            Normalize(o, .5f);
            return o;
        }

        static float[] Crickets()
        {
            const float L = 4;
            var b = new float[Len(L)];
            var r = new System.Random(19);
            foreach (var (f, start, gap) in new[] { (4400f, 0f, .62f), (4750f, .21f, .8f) })
                for (float t = start; t < L - .2f; t += gap * (.9f + (float)r.NextDouble() * .2f))
                    for (int p = 0; p < 3; p++) Tone(b, t + p * .045f, f, .03f, Wave.Sine, .12f, 0, true);
            return b;
        }

        // ------------------------------------------------------------------ nhac
        string WantMood()
        {
            if (MainMenu.Instance && MainMenu.Instance.IsOpen) return "intro";
            var s = GameSession.Instance;
            if (!s) return "day";
            float h = s.Clock.Hour;
            return h >= 19.5f || h < 5.5f ? "night" : "day";
        }

        /// <summary>Ve 8 o nhip (2 vong hop am) cua 1 tam trang, nhac lap vong lien mach.</summary>
        IEnumerator RenderMusic(string name)
        {
            if (_music.ContainsKey(name)) yield break;
            var m = Moods[name];
            float sd = 60f / m.bpm / 4;
            var buf = new float[Len(sd * 16 * Bars)];
            var r = new System.Random(name.GetHashCode() & 0xffff);
            float Rnd() => (float)r.NextDouble();
            for (int bar = 0; bar < Bars; bar++)
            {
                var ch = m.ch[bar % 4];
                for (int st = 0; st < 16; st++)
                {
                    float t = (bar * 16 + st) * sd + (st % 2 == 1 ? sd * .18f : 0);
                    if (st == 0) for (int n = 0; n < ch.Length; n++) Key(buf, ch[n], t + n * .018f, sd * 15, .05f);
                    if (st == 10 && Rnd() < .6f) for (int n = 1; n < ch.Length; n++) Key(buf, ch[n] + (n == 1 ? 12 : 0), t + (n - 1) * .015f, sd * 5, .028f);
                    if (st == 0 || st == 10 || (st == 7 && Rnd() < .4f)) Note(buf, t, Mtof(m.bass[bar % 4]), sd * (st == 0 ? 7 : 4), .12f, Wave.Sine, 0, true);
                    if (m.drums)
                    {
                        if (st == 0 || st == 10 || (st == 7 && bar % 2 == 1)) Kick(buf, t);
                        if (st == 4 || st == 12) Noise(buf, t, .16f, .08f, 1600, 1, Filter.Bandpass, true, r.Next(10000));
                        if (st % 2 == 0) Noise(buf, t, .035f, st % 4 != 0 ? .018f : .03f, 7000, 1, Filter.Highpass, true, r.Next(10000));
                    }
                    if (st % 2 == 0 && Rnd() < m.sparkle) Note(buf, t, Mtof(ch[r.Next(ch.Length)] + 24), sd * 3, .032f, Wave.Triangle, 0, true);
                }
                yield return null; // trai viec ve nhac ra nhieu khung hinh
            }
            // loc thong thap 2500 Hz (chay 2 vong de bo loc on dinh qua cho noi)
            var lp = new Biquad(Filter.Lowpass, 2500, .3f);
            for (int i = buf.Length - Len(.5f); i < buf.Length; i++) lp.Process(buf[i]);
            for (int i = 0; i < buf.Length; i++) buf[i] = lp.Process(buf[i]);
            // tieng dia than (vinyl) lap trong nen
            float v = 0;
            for (int i = 0; i < buf.Length; i++)
            {
                v = v * .97f + (Rnd() * 2 - 1) * .03f;
                buf[i] += (v * .25f + (Rnd() < .0006f ? (Rnd() * 2 - 1) * .5f : 0)) * .35f;
            }
            float peak = 0;
            foreach (var x in buf) peak = Mathf.Max(peak, Mathf.Abs(x));
            if (peak > .9f) Normalize(buf, .9f); // tranh vo tieng khi nhieu not chong len nhau
            _music[name] = ToClip("Lofi_" + name, buf, true);
        }

        static void Key(float[] b, int m, float t, float dur, float vol)
        {
            float f = Mtof(m);
            Note(b, t, f, dur, vol, Wave.Sine, -4, true);
            Note(b, t, f, dur * .8f, vol * .5f, Wave.Triangle, 5, true);
            Note(b, t, f * 2, dur * .3f, vol * .1f, Wave.Sine, 0, true);
        }

        /// <summary>mKick: 120 Hz truot xuong 42 Hz trong .22s, am luong .45 -> .001 trong .3s.</summary>
        static void Kick(float[] b, float at)
        {
            int s0 = Mathf.RoundToInt(at * Rate), n = Len(.3f), nf = Len(.22f);
            double phase = 0;
            for (int i = 0; i < n; i++)
            {
                double f = i < nf ? 120 * Math.Pow(42 / 120.0, i / (double)nf) : 42;
                phase += f / Rate;
                float g = .45f * Mathf.Exp(Mathf.Log(.001f / .45f) * i / n);
                b[(s0 + i) % b.Length] += (float)Math.Sin(2 * Math.PI * phase) * g;
            }
        }

        // ------------------------------------------------------------------ moi khung
        void Update()
        {
            var set = GameSettings.Current;
            var s = GameSession.Instance;
            var w = WeatherSystem.Instance;

            // nhac: doi tam trang thi chuyen dan 3 giay
            string want = WantMood();
            if (want != _playing && _music.TryGetValue(want, out var clip))
            {
                (_musA, _musB) = (_musB, _musA); // A = moi, B = cu
                _musA.clip = clip; _musA.volume = 0; _musA.Play();
                _playing = want; _xfade = 0;
            }
            _xfade = Mathf.MoveTowards(_xfade, 1, Time.unscaledDeltaTime / 3f);
            float mv = MusicGain * set.music;
            _musA.volume = mv * _xfade;
            _musB.volume = mv * (1 - _xfade);
            if (_xfade >= 1 && _musB.isPlaying) _musB.Stop();

            if (!_ready) return;
            float amb = set.sfx;
            bool playing = s && s.Running;
            float night = s ? Placeables.NightFactor(s.Clock.Hour) : 0;
            float rain = w ? w.RainNow : 0;
            float wind = s && s.Environment ? s.Environment.wind : 0;

            // song: gan bien thi to (do 16 huong quanh nguoi choi moi .5 giay)
            _probeT -= Time.unscaledDeltaTime;
            if (_probeT <= 0) { _probeT = .5f; _wavesV = OceanNearby(); }
            float menuWaves = MainMenu.Instance && MainMenu.Instance.IsOpen ? .6f : 0;
            Fade(_waves, amb * .5f * Mathf.Max(menuWaves, playing ? _wavesV : 0));
            Fade(_rain, amb * .55f * rain);
            Fade(_wind, amb * .45f * Mathf.Clamp01((wind - .2f) / .8f));
            _fireV = Mathf.MoveTowards(_fireV, playing && s.NearFire(3) ? 1 : 0, Time.unscaledDeltaTime * .8f);
            Fade(_fire, amb * .45f * _fireV);
            Fade(_crickets, amb * .18f * (playing ? night * (1 - rain) : 0));
        }

        static void Fade(AudioSource src, float target)
            => src.volume = Mathf.MoveTowards(src.volume, target, Time.unscaledDeltaTime * .5f);

        float OceanNearby()
        {
            var s = GameSession.Instance;
            var d = IslandTerrain.Instance ? IslandTerrain.Instance.Data : null;
            if (!s || !s.Player || d == null) return 0;
            var p = s.Player.transform.position;
            int hit = 0, tot = 0;
            foreach (float r in new[] { 5f, 11f, 20f })
                for (int a = 0; a < 16; a++)
                {
                    float ang = a * Mathf.PI / 8;
                    int i = Mathf.RoundToInt(p.x + Mathf.Cos(ang) * r), j = Mathf.RoundToInt(p.z + Mathf.Sin(ang) * r);
                    tot++;
                    if (!d.InBounds(i, j) || d.Ocean[d.Idx(i, j)] != 0) hit += r < 6 ? 3 : r < 12 ? 2 : 1;
                }
            return Mathf.Clamp01(hit / (tot * .9f));
        }

        /// <summary>Test: tra ve clip nhac da ve (null neu chua).</summary>
        public AudioClip MusicClip(string mood) => _music.TryGetValue(mood, out var c) ? c : null;
        public AudioClip SfxClip(Sfx s) => _sfx.TryGetValue(s, out var c) ? c : null;
        public float AmbientVolume(string which) => which switch
        {
            "waves" => _waves.volume, "rain" => _rain.volume, "wind" => _wind.volume, "fire" => _fire.volume, "crickets" => _crickets.volume, _ => 0,
        };
        public float MusicVolume => _musA.volume + _musB.volume;
    }
}
