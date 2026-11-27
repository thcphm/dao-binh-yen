// Giai doan 6f: duong chay cua WebGL (khong luong phu) cho ra hon dao giong het ban da luong.
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace SereneIsle.Tests
{
    public class WebGLPathTests
    {
        [TearDown]
        public void TearDown()
        {
            Par.Threads = true;
            foreach (var go in Object.FindObjectsByType<GameObject>())
                if (go && go.transform.parent == null) Object.Destroy(go);
        }

        static IEnumerator Gen(bool threads, System.Action<IslandTerrain> got)
        {
            Par.Threads = threads;
            var go = new GameObject("Island");
            go.SetActive(false);
            var t = go.AddComponent<IslandTerrain>();
            t.seed = 424242; t.size = 800;
            go.SetActive(true);
            float t0 = Time.realtimeSinceStartup;
            while (!t.IsReady)
            {
                if (Time.realtimeSinceStartup - t0 > 300) Assert.Fail("sinh dao qua 300s");
                yield return null;
            }
            got(t);
        }

        static long Hash(IslandData d)
        {
            long h = 17;
            for (int k = 0; k < d.N * d.N; k++)
                h = h * 31 + Mathf.RoundToInt(d.HT[k] * 1000) * 7 + d.Level[k] * 3 + (int)d.Bio[k] + d.ObjType[k] * 11 + d.Ocean[k];
            return h;
        }

        [UnityTest, Timeout(900000)]
        public IEnumerator KhongLuongPhu_GiongDaLuong()
        {
            IslandTerrain a = null, b = null;
            yield return Gen(true, t => a = t);
            long ha = Hash(a.Data); float sa = a.GenSeconds;
            Object.Destroy(a.gameObject);
            yield return null;
            yield return Gen(false, t => b = t);
            long hb = Hash(b.Data); float sb = b.GenSeconds;
            Debug.Log($"[SereneIsle] Sinh dao: da luong {sa:0.0}s, tuan tu (WebGL) {sb:0.0}s");
            Assert.AreEqual(ha, hb, "cung seed -> cung hon dao o ca hai che do");
            Assert.AreEqual(Mathf.Round(a.Progress * 100), 100);
        }
    }
}
