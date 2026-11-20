// Port 1:1 cac ham random/noise tu serene-isle.html (mulberry32, hash2, vnoise, fbm, smooth).
// Dung double + phep nhan int32 tran so (unchecked) de cung SEED cho ket qua giong ban web.
using System;

namespace SereneIsle
{
    public static class IslandNoise
    {
        /// <summary>hash2(x,y,s) -> [0,1]. JS: Math.imul + >>> .</summary>
        public static double Hash2(int x, int y, int s)
        {
            unchecked
            {
                int h = x * 374761393 + y * 668265263 + s * 1442695041;
                h = (h ^ (int)((uint)h >> 13)) * 1274126177;
                h ^= (int)((uint)h >> 16);
                return (uint)h / 4294967295.0;
            }
        }

        /// <summary>Value noise noi suy smoothstep.</summary>
        public static double VNoise(double x, double y, int s)
        {
            double fx = Math.Floor(x), fy = Math.Floor(y);
            int xi = (int)fx, yi = (int)fy;
            double xf = x - fx, yf = y - fy;
            double u = xf * xf * (3 - 2 * xf), v = yf * yf * (3 - 2 * yf);
            double a = Hash2(xi, yi, s), b = Hash2(xi + 1, yi, s);
            double c = Hash2(xi, yi + 1, s), d = Hash2(xi + 1, yi + 1, s);
            return a + (b - a) * u + (c - a) * v + (a - b - c + d) * u * v;
        }

        /// <summary>fbm 4 octave, chuan hoa ve [0,1].</summary>
        public static double Fbm(double x, double y, int s)
        {
            double t = 0, amp = .5, f = 1, n = 0;
            for (int o = 0; o < 4; o++)
            {
                t += VNoise(x * f, y * f, s + o * 17) * amp;
                n += amp; amp *= .5; f *= 2;
            }
            return t / n;
        }

        /// <summary>smooth(a,b,x): smoothstep cho phep a&gt;b (dao chieu) nhu ban web.</summary>
        public static double Smooth(double a, double b, double x)
        {
            double t = (x - a) / (b - a);
            if (t < 0) t = 0; else if (t > 1) t = 1;
            return t * t * (3 - 2 * t);
        }

        public static double Clamp01(double x) => x < 0 ? 0 : x > 1 ? 1 : x;

        public static double Hypot(double a, double b) => Math.Sqrt(a * a + b * b);
    }

    /// <summary>mulberry32 PRNG (giong ban web).</summary>
    public sealed class Mulberry32
    {
        int _a;
        public Mulberry32(int seed) { _a = seed; }

        public double Next()
        {
            unchecked
            {
                _a = _a + 0x6D2B79F5;
                int a = _a;
                int t = (a ^ (int)((uint)a >> 15)) * (1 | a);
                t = (t + (t ^ (int)((uint)t >> 7)) * (61 | t)) ^ t;
                return (uint)(t ^ (int)((uint)t >> 14)) / 4294967296.0;
            }
        }
    }
}
