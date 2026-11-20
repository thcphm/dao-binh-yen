// Chup camera + HUD (UI Toolkit ve rieng vao texture trong suot roi ghep len) - dung chung cho test giai doan 3.
using System.Collections;
using System.IO;
using UnityEngine;

namespace SereneIsle.Tests
{
    public static class GameplayShots
    {
        public static IEnumerator Shot(string path)
        {
            const int W = 1280, H = 720;
            var cam = Camera.main;
            var hud = GameHud.Instance;
            var rtCam = new RenderTexture(W, H, 24, RenderTextureFormat.ARGB32);
            var rtUi = new RenderTexture(W, H, 24, RenderTextureFormat.ARGB32);
            var prevCam = cam.targetTexture;
            cam.targetTexture = rtCam;
            hud.Panel.targetTexture = rtUi;
            hud.Panel.clearColor = true;
            hud.Panel.colorClearValue = new Color(0, 0, 0, 0);
            for (int i = 0; i < 4; i++) yield return null;

            var a = Read(rtCam); var u = Read(rtUi);
            var px = a.GetPixels(); var ui = u.GetPixels();
            for (int i = 0; i < px.Length; i++)
            {
                float al = ui[i].a;
                if (al <= 0) continue;
                px[i] = new Color(ui[i].r + px[i].r * (1 - al), ui[i].g + px[i].g * (1 - al), ui[i].b + px[i].b * (1 - al), 1);
            }
            a.SetPixels(px); a.Apply();
            File.WriteAllBytes(path, a.EncodeToPNG());

            cam.targetTexture = prevCam;
            hud.Panel.targetTexture = null;
            Object.Destroy(a); Object.Destroy(u);
            rtCam.Release(); rtUi.Release(); Object.Destroy(rtCam); Object.Destroy(rtUi);
        }

        static Texture2D Read(RenderTexture rt)
        {
            var old = RenderTexture.active;
            RenderTexture.active = rt;
            var t = new Texture2D(rt.width, rt.height, TextureFormat.RGBA32, false);
            t.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
            t.Apply();
            RenderTexture.active = old;
            return t;
        }
    }
}
