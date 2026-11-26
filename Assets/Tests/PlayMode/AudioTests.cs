// Giai doan 6e: nhac lofi (3 tam trang) ve san, hieu ung, am nen theo mua / lua trai / dem / bien.
// Xuat file WAV (thu muc anh chup) de nguoi dung nghe duyet.
using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace SereneIsle.Tests
{
    public class AudioTests
    {
        [TearDown]
        public void TearDown()
        {
            foreach (var go in Object.FindObjectsByType<GameObject>())
                if (go && go.transform.parent == null) Object.Destroy(go);
        }

        static IEnumerator WaitFor(System.Func<bool> ok, float secs, string what)
        {
            float t = Time.realtimeSinceStartup;
            while (!ok())
            {
                if (Time.realtimeSinceStartup - t > secs) Assert.Fail("qua lau: " + what);
                yield return null;
            }
        }

        [UnityTest, Timeout(600000)]
        public IEnumerator Nhac_AmNen_HieuUng()
        {
            var boot = new GameObject("Bootstrap");
            boot.SetActive(false);
            var bs = boot.AddComponent<GameBootstrap>(); bs.seed = 424242; bs.saveSlot = -1;
            boot.SetActive(true);
            yield return WaitFor(() => IslandTerrain.Instance && IslandTerrain.Instance.IsReady, 180, "sinh dao");
            var s = GameSession.Instance;
            var a = GameAudio.Instance;
            var w = WeatherSystem.Instance;
            Assert.IsNotNull(a, "co GameAudio");
            float t0 = Time.realtimeSinceStartup;
            yield return WaitFor(() => a.HasMusic("day") && a.HasMusic("night") && a.HasMusic("intro"), 60, "ve nhac");
            Debug.Log($"[SereneIsle] Ve xong 3 ban nhac sau {Time.realtimeSinceStartup - t0:0.0}s");
            Assert.AreEqual(System.Enum.GetValues(typeof(Sfx)).Length, a.SfxCount, "du hieu ung");

            // nhac ban ngay: dung do dai 8 o nhip 76 bpm, co tieng, khong vo (clip)
            var day = a.MusicClip("day");
            Assert.AreEqual(GameAudio.Bars * 16 * (60f / 76 / 4), day.length, .05f);
            var data = new float[day.samples];
            day.GetData(data, 0);
            float peak = 0; double rms = 0;
            foreach (var v in data) { peak = Mathf.Max(peak, Mathf.Abs(v)); rms += v * v; }
            rms = System.Math.Sqrt(rms / data.Length);
            Assert.Greater(rms, .01, "nhac co tieng");
            Assert.Less(peak, 1f, "khong vo tieng");

            // tam trang theo gio
            w.Override = WeatherKind.Sun; w.SnapVisuals();
            s.Clock.Minutes = 10 * 60;
            yield return WaitFor(() => a.CurrentMood == "day", 5, "nhac ngay");
            Assert.Greater(a.MusicVolume, 0f);
            s.Clock.Minutes = 21 * 60;
            yield return WaitFor(() => a.CurrentMood == "night", 5, "nhac dem");
            yield return WaitFor(() => a.AmbientVolume("crickets") > .02f, 6, "de keu ban dem");

            // mua: tieng mua to dan
            w.Override = WeatherKind.Rain; w.SnapVisuals();
            yield return WaitFor(() => a.AmbientVolume("rain") > .05f, 8, "tieng mua");
            w.Override = WeatherKind.Storm; w.SnapVisuals();
            yield return WaitFor(() => a.AmbientVolume("wind") > .05f, 8, "gio bao");
            w.Override = WeatherKind.Sun; w.SnapVisuals();

            // lua trai giua lang
            var d = IslandTerrain.Instance.Data;
            var c = d.TownPoint(0, 2);
            s.Player.PlaceAt(new Vector3(c.x, 0, c.y), 0);
            yield return WaitFor(() => a.AmbientVolume("fire") > .05f, 8, "tieng lua trai");

            // hieu ung goi duoc
            GameAudio.Play(Sfx.Coin);
            GameAudio.Play(Sfx.Catch);

            // xuat WAV de nghe duyet
            string dir = Phase3Tests.Dir();
            foreach (var m in new[] { "day", "night", "intro" }) WriteWav(Path.Combine(dir, $"p6e_nhac_{m}.wav"), a.MusicClip(m));
            foreach (Sfx x in System.Enum.GetValues(typeof(Sfx))) WriteWav(Path.Combine(dir, $"p6e_sfx_{x.ToString().ToLowerInvariant()}.wav"), a.SfxClip(x));
        }

        static void WriteWav(string path, AudioClip clip)
        {
            var f = new float[clip.samples * clip.channels];
            clip.GetData(f, 0);
            using (var w = new BinaryWriter(File.Create(path)))
            {
                int bytes = f.Length * 2;
                w.Write(System.Text.Encoding.ASCII.GetBytes("RIFF")); w.Write(36 + bytes);
                w.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt ")); w.Write(16); w.Write((short)1); w.Write((short)clip.channels);
                w.Write(clip.frequency); w.Write(clip.frequency * clip.channels * 2); w.Write((short)(clip.channels * 2)); w.Write((short)16);
                w.Write(System.Text.Encoding.ASCII.GetBytes("data")); w.Write(bytes);
                foreach (var v in f) w.Write((short)Mathf.Clamp(Mathf.RoundToInt(v * 32767), -32768, 32767));
            }
        }
    }
}
