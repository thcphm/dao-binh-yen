// Canh vat Quaternius tren dao: chup anh tung vung (rung cay tan, rung thong, rung mua, da, da do) de duyet.
using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace SereneIsle.Tests
{
    public class SceneryTests
    {
        [TearDown]
        public void TearDown()
        {
            foreach (var go in Object.FindObjectsByType<GameObject>())
                if (go && go.transform.parent == null) Object.Destroy(go);
        }

        [UnityTest, Timeout(600000)] // may 8 GB, co the dang chay Unity khac
        public IEnumerator CanhVat_Quaternius()
        {
            Assert.IsNotNull(NatureKit.Instance, "phai co Resources/NatureKit");
            var boot = new GameObject("Bootstrap");
            boot.SetActive(false);
            var bs = boot.AddComponent<GameBootstrap>(); bs.seed = 424242; bs.saveSlot = -1;
            boot.SetActive(true);
            float t0 = Time.realtimeSinceStartup;
            while (!(IslandTerrain.Instance && IslandTerrain.Instance.IsReady))
            {
                if (Time.realtimeSinceStartup - t0 > 180) Assert.Fail("sinh dao qua 180s");
                yield return null;
            }
            for (int i = 0; i < 10; i++) yield return null;

            var veg = IslandTerrain.Instance.Vegetation;
            Assert.GreaterOrEqual(veg.KitTypes, 6, "it nhat 6 loai dung mo hinh Quaternius");
            var d = IslandTerrain.Instance.Data;
            var s = GameSession.Instance;
            var rig = Camera.main.GetComponent<OrbitCamera>();
            string dir = Dir();

            var shots = new[]
            {
                (WorldObjectType.Gum, "s_1_rung_cay_tan.png"), (WorldObjectType.Pine, "s_2_rung_thong.png"),
                (WorldObjectType.JungleTree, "s_3_rung_mua.png"), (WorldObjectType.Rock, "s_4_da.png"),
                (WorldObjectType.RedRock, "s_5_da_do.png"), (WorldObjectType.Bush, "s_6_bui.png"),
            };
            foreach (var (type, file) in shots)
            {
                if (!FindCluster(d, type, out int ti, out int tj)) { Debug.LogWarning("Khong tim thay " + type); continue; }
                // dung cach cum vat the 6 o, quay mat vao cum; camera sau lung
                Vector2 best = default; bool ok = false;
                for (int a = 0; a < 16 && !ok; a++)
                {
                    float ang = a * Mathf.PI / 8;
                    int x = Mathf.RoundToInt(ti + Mathf.Cos(ang) * 9), z = Mathf.RoundToInt(tj + Mathf.Sin(ang) * 9);
                    if (x < 0 || z < 0 || x >= d.N || z >= d.N) continue;
                    int k = z * d.N + x;
                    if (d.ObjType[k] == 0 && d.Solid[k] == 0 && d.HT[k] > IslandData.SEA) { best = new Vector2(x, z); ok = true; }
                }
                if (!ok) continue;
                float dx = ti - best.x, dz = tj - best.y;
                float face = Mathf.Atan2(dx, dz);
                s.Player.PlaceAt(new Vector3(best.x, 0, best.y), face);
                rig.yaw = Mathf.Atan2(-dx, -dz); rig.pitch = .55f; rig.distance = 9f; // camera mac dinh khi choi
                for (int i = 0; i < 20; i++) yield return null;
                yield return Shot(Path.Combine(dir, file));
            }

            // nhin tu tren cao
            rig.pitch = .9f; rig.distance = 40f;
            for (int i = 0; i < 20; i++) yield return null;
            yield return Shot(Path.Combine(dir, "s_7_tu_tren_cao.png"));
        }

        /// <summary>O co nhieu vat the loai 'type' nhat trong cua so 9x9 (lay mau thua).</summary>
        static bool FindCluster(IslandData d, WorldObjectType type, out int bi, out int bj)
        {
            byte code = (byte)(type + 1);
            int best = 0; bi = bj = 0;
            for (int j = 8; j < d.N - 8; j += 5)
                for (int i = 8; i < d.N - 8; i += 5)
                {
                    if (d.ObjType[j * d.N + i] != code) continue;
                    int n = 0;
                    for (int b = -4; b <= 4; b++)
                        for (int a = -4; a <= 4; a++)
                            if (d.ObjType[(j + b) * d.N + i + a] == code) n++;
                    if (n > best) { best = n; bi = i; bj = j; }
                }
            return best > 0;
        }

        static string Dir()
        {
            string dir = System.Environment.GetEnvironmentVariable("SERENE_SHOT_DIR");
            if (string.IsNullOrEmpty(dir)) dir = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Logs", "shots"));
            Directory.CreateDirectory(dir);
            return dir;
        }

        static IEnumerator Shot(string path)
        {
            var cam = Camera.main;
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
