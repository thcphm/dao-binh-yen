// Bo tong hop am thanh don gian (thay WebAudio cua ban web, dong 1172-1180 va 1951-1979).
// Moi am thanh duoc "ve" san thanh mang float roi nap vao AudioClip (AudioClip.SetData chay ca tren WebGL,
// con OnAudioFilterRead thi khong) -> khong ton CPU luc choi.
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SereneIsle
{
    public static class Synth
    {
        public const int Rate = 22050;

        public enum Wave { Sine, Square, Triangle, Saw }
        public enum Filter { Lowpass, Highpass, Bandpass }

        /// <summary>Bo loc bac 2 (RBJ cookbook), giong BiquadFilterNode.</summary>
        public sealed class Biquad
        {
            float _b0, _b1, _b2, _a1, _a2, _x1, _x2, _y1, _y2;

            public Biquad(Filter type, float freq, float q)
            {
                freq = Mathf.Clamp(freq, 10, Rate * .45f);
                double w = 2 * Math.PI * freq / Rate, cs = Math.Cos(w), sn = Math.Sin(w), alpha = sn / (2 * Math.Max(.05, q));
                double b0, b1, b2, a0 = 1 + alpha, a1 = -2 * cs, a2 = 1 - alpha;
                switch (type)
                {
                    case Filter.Lowpass: b0 = (1 - cs) / 2; b1 = 1 - cs; b2 = (1 - cs) / 2; break;
                    case Filter.Highpass: b0 = (1 + cs) / 2; b1 = -(1 + cs); b2 = (1 + cs) / 2; break;
                    default: b0 = alpha; b1 = 0; b2 = -alpha; break; // bandpass (dinh 0 dB)
                }
                _b0 = (float)(b0 / a0); _b1 = (float)(b1 / a0); _b2 = (float)(b2 / a0); _a1 = (float)(a1 / a0); _a2 = (float)(a2 / a0);
            }

            public float Process(float x)
            {
                float y = _b0 * x + _b1 * _x1 + _b2 * _x2 - _a1 * _y1 - _a2 * _y2;
                _x2 = _x1; _x1 = x; _y2 = _y1; _y1 = y;
                return y;
            }
        }

        public static int Len(float secs) => Mathf.Max(1, Mathf.CeilToInt(secs * Rate));

        static float Osc(Wave w, double phase)
        {
            double p = phase - Math.Floor(phase);
            switch (w)
            {
                case Wave.Square: return p < .5 ? 1 : -1;
                case Wave.Triangle: return (float)(p < .5 ? 4 * p - 1 : 3 - 4 * p);
                case Wave.Saw: return (float)(2 * p - 1);
                default: return (float)Math.Sin(2 * Math.PI * p);
            }
        }

        /// <summary>Ghi them vao buf tai vi tri (giay); wrap = vong lai dau mang (nhac lap khong vap).</summary>
        static void Put(float[] buf, int i, float v, bool wrap)
        {
            if (i < 0) return;
            if (i >= buf.Length) { if (!wrap) return; i %= buf.Length; }
            buf[i] += v;
        }

        /// <summary>tone() ban web: dao dong f (truot mu toi f2), am luong v giam mu ve 0.0008 trong d giay.</summary>
        public static void Tone(float[] buf, float at, float f, float d, Wave w = Wave.Sine, float v = .12f, float f2 = 0, bool wrap = false)
        {
            int s0 = Mathf.RoundToInt(at * Rate), n = Len(d);
            double phase = 0;
            float decay = Mathf.Log(.0008f / v);
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)n;
                double freq = f2 > 0 ? f * Math.Pow(f2 / f, t) : f;
                phase += freq / Rate;
                Put(buf, s0 + i, Osc(w, phase) * v * Mathf.Exp(decay * t), wrap);
            }
        }

        static float[] _noise;
        /// <summary>1 giay nhieu trang co dinh (seed) dung chung.</summary>
        public static float[] NoiseBuf
        {
            get
            {
                if (_noise != null) return _noise;
                var r = new System.Random(1234);
                _noise = new float[Rate];
                for (int i = 0; i < _noise.Length; i++) _noise[i] = (float)(r.NextDouble() * 2 - 1);
                return _noise;
            }
        }

        /// <summary>noise() ban web: nhieu qua bo loc, am luong v giam mu trong d giay.</summary>
        public static void Noise(float[] buf, float at, float d, float v, float fq, float q = 1, Filter type = Filter.Bandpass, bool wrap = false, int offset = 0, float end = .0008f)
        {
            int s0 = Mathf.RoundToInt(at * Rate), n = Len(d);
            var bq = new Biquad(type, fq, q);
            var nb = NoiseBuf;
            float decay = Mathf.Log(end / v);
            for (int i = 0; i < n; i++)
                Put(buf, s0 + i, bq.Process(nb[(offset + i) % nb.Length]) * v * Mathf.Exp(decay * i / n), wrap);
        }

        /// <summary>mNote ban web: len 0->vol trong .02s, mu ve vol*.35 o 40%, mu ve .0005 o cuoi.</summary>
        public static void Note(float[] buf, float at, float f, float dur, float vol, Wave w = Wave.Sine, float detuneCents = 0, bool wrap = false)
        {
            int s0 = Mathf.RoundToInt(at * Rate), n = Len(dur);
            double freq = f * Math.Pow(2, detuneCents / 1200.0), phase = 0;
            int atk = Mathf.Min(Len(.02f), n - 2), mid = Mathf.Clamp((int)(n * .4f), atk + 1, n - 1);
            float k1 = Mathf.Log(.35f), k2 = Mathf.Log(.0005f / (vol * .35f));
            for (int i = 0; i < n; i++)
            {
                float g;
                if (i < atk) g = vol * i / (float)atk;
                else if (i < mid) g = vol * Mathf.Exp(k1 * (i - atk) / (float)(mid - atk));
                else g = vol * .35f * Mathf.Exp(k2 * (i - mid) / (float)(n - mid));
                phase += freq / Rate;
                Put(buf, s0 + i, Osc(w, phase) * g, wrap);
            }
        }

        public static float Mtof(float m) => 440f * Mathf.Pow(2, (m - 69) / 12f);

        public static AudioClip ToClip(string name, float[] data, bool loop = false)
        {
            var c = AudioClip.Create(name, data.Length, 1, Rate, false);
            c.SetData(data, 0);
            return c;
        }

        /// <summary>Lam mem cho noi vong: tron 'fade' giay cuoi vao dau mang.</summary>
        public static float[] LoopCrossfade(float[] src, float fade)
        {
            int f = Mathf.Min(Len(fade), src.Length / 3), n = src.Length - f;
            var o = new float[n];
            Array.Copy(src, o, n);
            for (int i = 0; i < f; i++)
            {
                float t = i / (float)f;
                o[i] = src[i] * t + src[n + i] * (1 - t);
            }
            return o;
        }

        public static void Normalize(float[] buf, float peak)
        {
            float m = 1e-6f;
            foreach (var v in buf) m = Mathf.Max(m, Mathf.Abs(v));
            float k = peak / m;
            for (int i = 0; i < buf.Length; i++) buf[i] *= k;
        }
    }
}
