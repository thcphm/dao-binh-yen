// Ban che tao o thi tran (ban web: BLD.bench dong 1006, placeB('bench',3,3,'bench')).
// Ban web: ong Bay khieng ra trong phan huong dan (giai doan 4); tam thoi dat san tu dau.
using UnityEngine;

namespace SereneIsle
{
    public static class BenchBuilder
    {
        public const int OX = 3, OZ = 3;

        public static GameObject Build(IslandData d, Transform parent, out Vector3 center)
        {
            var b = new MeshBuilder();
            b.Box("#b07a45", 0, .62f, 0, 1, .1f, .62f);
            foreach (var (x, z) in new[] { (-.42f, -.24f), (.42f, -.24f), (-.42f, .24f), (.42f, .24f) }) b.Box("#7a5232", x, .3f, z, .08f, .6f, .08f);
            b.Box("#6d767c", -.3f, .75f, 0, .18f, .16f, .16f);   // de
            b.Box("#c3ccd2", .25f, .69f, .1f, .4f, .02f, .14f);  // luoi cua
            b.Box("#8a5a2e", .5f, .7f, .1f, .12f, .06f, .05f);   // can cua
            b.Box("#a8703e", .1f, .15f, 0, .8f, .06f, .5f);      // ke duoi

            Vector2 c = d.TownPoint(OX, OZ);
            int k = d.TownIdx(OX, OZ);
            float ground = 0;
            if (k >= 0 && k < d.N * d.N)
            {
                d.Solid[k] = 1;
                if (d.ObjType != null && d.ObjType[k] != 0) d.ObjType[k] = 0;
                ground = d.HT[k];
            }
            var go = new GameObject("CraftBench");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3(c.x, ground, c.y);
            go.transform.localRotation = Quaternion.Euler(0, d.TownAngle * Mathf.Rad2Deg, 0);
            go.AddComponent<MeshFilter>().sharedMesh = b.ToMesh("BenchMesh");
            go.AddComponent<MeshRenderer>().sharedMaterial = MaterialLibrary.Objects(false);
            center = new Vector3(c.x, ground, c.y);
            return go;
        }
    }
}
