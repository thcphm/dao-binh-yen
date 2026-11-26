// Giai doan 6e: bo tong hop am thanh (dao dong, bo loc, nhieu, noi vong).
using NUnit.Framework;
using UnityEngine;

namespace SereneIsle.Tests
{
    public class AudioLogicTests
    {
        static float Rms(float[] b, int from, int to)
        {
            double s = 0;
            for (int i = from; i < to; i++) s += b[i] * b[i];
            return (float)System.Math.Sqrt(s / Mathf.Max(1, to - from));
        }

        [Test]
        public void Tone_TatDan()
        {
            Assert.AreEqual(440f, Synth.Mtof(69), .01f);
            Assert.AreEqual(880f, Synth.Mtof(81), .01f);
            var b = new float[Synth.Len(.5f)];
            Synth.Tone(b, 0, 440, .4f, Synth.Wave.Sine, .2f);
            float head = Rms(b, 0, Synth.Len(.05f)), tail = Rms(b, Synth.Len(.35f), Synth.Len(.4f));
            Assert.Greater(head, .05f, "co tieng");
            Assert.Less(tail, head * .1f, "tat dan theo ham mu");
            Assert.AreEqual(0f, Rms(b, Synth.Len(.42f), b.Length), 1e-6f, "het tieng sau d giay");
        }

        [Test]
        public void BoLoc_ThongThap()
        {
            float Gain(float freq)
            {
                var lp = new Synth.Biquad(Synth.Filter.Lowpass, 500, .7f);
                double s = 0;
                int n = Synth.Rate;
                for (int i = 0; i < n; i++)
                {
                    float y = lp.Process(Mathf.Sin(2 * Mathf.PI * freq * i / Synth.Rate));
                    if (i > n / 2) s += y * y;
                }
                return (float)System.Math.Sqrt(s / (n / 2)) / .7071f;
            }
            Assert.AreEqual(1f, Gain(100), .1f, "tan so thap di qua");
            Assert.Less(Gain(5000), .02f, "tan so cao bi chan");
        }

        [Test]
        public void NoiVong_KhongVap()
        {
            var src = new float[Synth.Len(3)];
            for (int i = 0; i < src.Length; i++) src[i] = Mathf.Sin(i * .37f) * (i / (float)src.Length);
            var o = Synth.LoopCrossfade(src, .5f);
            Assert.AreEqual(src.Length - Synth.Len(.5f), o.Length);
            float jump = Mathf.Abs(o[0] - o[o.Length - 1]);
            Assert.Less(jump, .4f, "dau va cuoi gan nhau (khong nghe tieng tach)");
            Synth.Normalize(o, .5f);
            float m = 0; foreach (var v in o) m = Mathf.Max(m, Mathf.Abs(v));
            Assert.AreEqual(.5f, m, 1e-4f);
        }
    }
}
