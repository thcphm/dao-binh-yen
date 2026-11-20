// Cau tau cua thi tran (ban web: buildPier, dong 1017-1025).
// Dung trong toa do thi tran (x = lx, z = lz huong ra bien) roi dat tai (CX, 0, CZ), xoay TownAngle quanh Y.
using UnityEngine;

namespace SereneIsle
{
    public static class PierBuilder
    {
        public static GameObject Build(IslandData d, Transform parent)
        {
            const float H = IslandData.PIER_H;
            var b = new MeshBuilder();
            string[] plank = { "#9a7448", "#8a6a40", "#a67e50" };

            // van san (tinh z bang double giong vong lap JS)
            for (double z = 7.6; z < 21.6; z += .3)
                b.Box(plank[(int)System.Math.Floor(z * 3.3 + .5) % 3], .5f, H, (float)z, 2.2f, .07f, .27f);
            // dam doc
            foreach (float x in new[] { -.55f, 1.55f }) b.Box("#6b4a2a", x, H - .08f, 14.6f, .12f, .12f, 14.2f);
            // canh phu ben phai
            for (double z = 10.6; z < 13.5; z += .3) b.Box("#9a7448", 2, H, (float)z, 1.1f, .07f, .27f);
            // coc + nap coc
            for (double z = 8; z <= 21; z += 2.2)
                foreach (float x in new[] { -.6f, 1.6f })
                {
                    if (x > 1 && z > 10.4 && z < 13.8) continue;
                    b.Cylinder("#5a3a22", x, H - 1.05f, (float)z, .09f, .1f, 2.6f);
                    b.Cylinder("#6b4a2a", x, H + .25f, (float)z, .1f, .1f, .5f);
                }
            // lan can day thung
            b.Box("#c8b27a", -.6f, H + .42f, 14.6f, .03f, .03f, 13.2f);
            b.Box("#c8b27a", 1.6f, H + .42f, 9.1f, .03f, .03f, 2.2f);
            b.Box("#c8b27a", 1.6f, H + .42f, 17.7f, .03f, .03f, 7.8f);
            // thung hang + thung go tron
            b.Box("#a07a4a", 1.15f, H + .22f, 11, .4f, .4f, .4f);
            b.Box("#a07a4a", 1.2f, H + .55f, 11.1f, .3f, .3f, .3f, 0, .5f, 0);
            b.Cylinder("#6b4a2a", -.15f, H + .28f, 12.4f, .2f, .2f, .5f);
            // cot treo den cuoi cau
            b.Box("#5a3a22", -.45f, H + .8f, 21, .1f, 1.6f, .1f);
            b.Box("#5a3a22", -.25f, H + 1.55f, 21, .45f, .06f, .06f);

            var go = new GameObject("Pier");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3(d.CX, 0, d.CZ);
            go.transform.localRotation = Quaternion.Euler(0, d.TownAngle * Mathf.Rad2Deg, 0);
            go.AddComponent<MeshFilter>().sharedMesh = b.ToMesh("PierMesh");
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = MaterialLibrary.Terrain;
            return go;
        }
    }
}
