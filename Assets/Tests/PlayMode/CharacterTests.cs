// Nhan vat KayKit trong game: dung, di, chay, nhay, dong tac + 2 nhan vat dung canh nhau. Chup anh de duyet.
using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace SereneIsle.Tests
{
    public class CharacterTests
    {
        [TearDown]
        public void TearDown()
        {
            Time.captureFramerate = 0;
            foreach (var go in Object.FindObjectsByType<GameObject>())
                if (go && go.transform.parent == null) Object.Destroy(go);
        }

        [UnityTest, Timeout(600000)] // may 8 GB, co the dang chay Unity khac
        public IEnumerator NhanVat_DiChay_DongTac()
        {
            Assert.IsNotNull(CharacterLibrary.Instance, "phai co Resources/CharacterLibrary");
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

            var s = GameSession.Instance;
            var body = s.Body;
            Assert.IsNotNull(body.Anim, "phai co bo phat animation");
            Assert.IsNotNull(body.Hand);
            s.Player.enabled = false; // tu dieu khien tu the
            Time.captureFramerate = 60; // moi khung = 1/60 giay game -> animation chay dung nhip khi chup
            var rig = Camera.main.GetComponent<OrbitCamera>();
            float face = s.Player.Facing;
            string dir = Dir();

            // giu tu the trong 'frames' khung, moi khung coi nhu 1/60 giay (batchmode chay nhanh hon thoi gian that)
            IEnumerator Hold(PoseInput p, int frames)
            {
                for (int i = 0; i < frames; i++) { p.time += 1 / 60f; body.Pose(1 / 60f, p); yield return null; }
            }

            rig.yaw = face; rig.pitch = .2f; rig.distance = 3.2f;
            yield return Hold(new PoseInput(), 40);
            Assert.AreEqual("Idle_A", body.Anim.Current);
            yield return Shot(Path.Combine(dir, "c_1_dung.png"));

            rig.yaw = face + Mathf.PI * .5f;
            yield return Hold(new PoseInput { amp = 1, speed = 3.6f }, 25);
            Assert.AreEqual("Walking_A", body.Anim.Current);
            yield return Shot(Path.Combine(dir, "c_2_di.png"));

            yield return Hold(new PoseInput { amp = 1.55f, speed = 5.6f }, 25);
            Assert.AreEqual("Running_A", body.Anim.Current);
            yield return Shot(Path.Combine(dir, "c_3_chay.png"));

            yield return Hold(new PoseInput { air = true, vy = 2 }, 20);
            yield return Shot(Path.Combine(dir, "c_4_nhay.png"));

            rig.yaw = face + .5f;
            body.PlayAct("drink", s.Items.Get("bottle"));
            Assert.AreEqual("drink", body.CurrentAct);
            yield return Hold(new PoseInput(), 25);
            yield return Shot(Path.Combine(dir, "c_5_uong.png"));
            yield return Hold(new PoseInput(), 60);

            body.PlayAct("axe");
            yield return Hold(new PoseInput(), 12);
            yield return Shot(Path.Combine(dir, "c_6_chat_cay.png"));
            yield return Hold(new PoseInput(), 60);

            // nhan vat nu dung canh
            var other = new GameObject("NPC");
            other.SetActive(false);
            other.AddComponent<IslandCharacter>().outfit = CharacterOutfit.Female();
            other.SetActive(true);
            // dung truoc mat nguoi choi tren cau, quay mat ve phia nguoi choi
            var p0 = s.Player.transform.position;
            var fwd = new Vector3(Mathf.Sin(face), 0, Mathf.Cos(face));
            var np = p0 + fwd * 1.3f;
            np.y = IslandTerrain.Instance.Data.GroundHeight(np.x, np.z);
            other.transform.SetPositionAndRotation(np, Quaternion.Euler(0, face * Mathf.Rad2Deg + 150, 0));
            var ob = other.GetComponent<IslandCharacter>();
            rig.yaw = face + 1.35f; rig.distance = 3.6f; rig.pitch = .18f;
            for (int i = 0; i < 90; i++) { body.Pose(1 / 60f, new PoseInput()); ob.Pose(1 / 60f, new PoseInput()); yield return null; }
            yield return Shot(Path.Combine(dir, "c_7_hai_nhan_vat.png"));

            // doi do: ao dai tay, quan short, bo that lung
            var o = ob.outfit.Clone(); o.longSleeve = true; o.shorts = false; o.noBelt = true; o.shirt = "#7cb66b";
            ob.ApplyOutfit(o);
            yield return null;
            yield return Shot(Path.Combine(dir, "c_8_doi_do.png"));
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
