// Smoke test giai doan 1: dung scene bang GameBootstrap nhu khi bam Play, cho dao sinh xong,
// kiem tra nhan vat/camera, va chup anh tu camera game ra <project>/Temp/phase1/ de xem bang mat.
using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace SereneIsle.Tests
{
    public class Phase1SmokeTests
    {
        GameObject _boot;

        [TearDown]
        public void TearDown()
        {
            foreach (var go in Object.FindObjectsByType<GameObject>())
                if (go && go.transform.parent == null) Object.Destroy(go);
        }

        [UnityTest]
        public IEnumerator DaoSinhXong_NhanVatDungTrenCauTau_ChupAnh()
        {
            _boot = new GameObject("Bootstrap");
            _boot.SetActive(false);
            var b = _boot.AddComponent<GameBootstrap>();
            b.seed = 424242;
            b.islandSize = 800;
            _boot.SetActive(true);

            float t0 = Time.realtimeSinceStartup;
            while (!(IslandTerrain.Instance && IslandTerrain.Instance.IsReady))
            {
                if (Time.realtimeSinceStartup - t0 > 180) Assert.Fail("sinh dao qua 180s");
                yield return null;
            }
            float genSeconds = Time.realtimeSinceStartup - t0;
            for (int i = 0; i < 10; i++) yield return null;

            var data = IslandTerrain.Instance.Data;
            Assert.AreEqual(400, data.CX); Assert.AreEqual(92, data.CZ);

            var player = b.Player;
            Assert.IsNotNull(player);
            Vector3 p = player.transform.position;
            Assert.IsTrue(data.IsPier(p.x, p.z), $"nhan vat phai dung tren cau tau, dang o {p}");
            Assert.AreEqual(data.GroundHeight(p.x, p.z), p.y, .01f, "nhan vat phai cham mat cau tau");
            Assert.IsTrue(player.Grounded);

            var renderers = player.GetComponentsInChildren<SkinnedMeshRenderer>();
            Assert.GreaterOrEqual(renderers.Length, 6, "nhan vat KayKit: than, dau, 2 tay, 2 chan (skinned)");
            foreach (var r in renderers) Assert.AreEqual("SereneIsle/WebLambert", r.sharedMaterial.shader.name);

            var chunks = IslandTerrain.Instance.GetComponentsInChildren<MeshRenderer>();
            Assert.Greater(chunks.Length, 190, "phai co ~196 manh dia hinh + nuoc + day bien");

            var cam = Camera.main;
            Assert.IsNotNull(cam);
            Assert.Less(Vector3.Distance(cam.transform.position, p), 12f, "camera phai o gan nhan vat");

            // Temp/ cua project bi Unity xoa khi dong -> cho phep chi dinh thu muc khac
            string dir = System.Environment.GetEnvironmentVariable("SERENE_SHOT_DIR");
            if (string.IsNullOrEmpty(dir)) dir = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Logs", "phase1"));
            Directory.CreateDirectory(dir);
            yield return Shot(cam, Path.Combine(dir, "1_cap_ben.png"));

            // nhin tu tren cao xuong thi tran va bo bien
            var rig = cam.GetComponent<OrbitCamera>();
            rig.pitch = 1.0f; rig.distance = 24f; rig.yaw += .6f;
            for (int i = 0; i < 3; i++) yield return null;
            yield return Shot(cam, Path.Combine(dir, "2_toan_canh.png"));

            Debug.Log($"[Phase1Smoke] sinh dao {genSeconds:F1}s, anh o {dir}");
        }

        static IEnumerator Shot(Camera cam, string path)
        {
            var rt = new RenderTexture(1280, 720, 24, RenderTextureFormat.ARGB32);
            var prev = cam.targetTexture;
            cam.targetTexture = rt;
            for (int i = 0; i < 3; i++) yield return null;
            var old = RenderTexture.active;
            RenderTexture.active = rt;
            var tex = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
            tex.Apply();
            RenderTexture.active = old;
            cam.targetTexture = prev;
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.Destroy(tex);
            rt.Release();
            Object.Destroy(rt);
        }
    }
}
