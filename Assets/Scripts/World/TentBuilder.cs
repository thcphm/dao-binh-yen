// Leu cua nguoi choi (ban web: BLD.tent dong 999, placeB('tent',-7,1,'bed')).
// Tam leu = townPt(ox + (w-1)/2, oz + (d-1)/2), xoay theo huong bien. 2x2 o bi chan.
using UnityEngine;

namespace SereneIsle
{
    public static class TentBuilder
    {
        public const int OX = -7, OZ = 1, W = 2, D = 2;

        /// <param name="home">diem dung truoc cua leu (noi tinh day)</param>
        public static GameObject Build(IslandData d, Transform parent, out Vector3 home, out float facing)
        {
            var b = new MeshBuilder();
            var prism = ProceduralMeshes.Prism(3, 1, 1, "TentPrism");
            float rx = -Mathf.PI / 2;
            // than leu: lang tru tam giac nam ngang (CylinderGeometry(.95,.95,1.9,3) xoay -90 do quanh X)
            b.Add(prism, Matrix4x4.TRS(new Vector3(0, .475f, 0), ChibiCharacter.EulerXYZ(rx, 0, 0), new Vector3(.95f, 1.9f, .95f)), MaterialLibrary.Hex("#d9c38f"));
            // cua
            b.Add(prism, Matrix4x4.TRS(new Vector3(0, .25f, .95f), ChibiCharacter.EulerXYZ(rx, 0, 0), new Vector3(.5f, .06f, .5f)), MaterialLibrary.Hex("#5a4630"));
            b.Box("#8a6a4a", 0, .6f, 1.05f, .05f, 1.2f, .05f);
            b.Box("#b7a06a", 0, .02f, 1.2f, 1.4f, .04f, .6f);

            Vector2 c = d.TownPoint(OX + (W - 1) / 2f, OZ + (D - 1) / 2f);
            float ground = 0;
            for (int dz = 0; dz < D; dz++)
                for (int dx = 0; dx < W; dx++)
                {
                    int k = d.TownIdx(OX + dx, OZ + dz);
                    if (k < 0 || k >= d.N * d.N) continue;
                    d.Solid[k] = 1;
                    ground = Mathf.Max(ground, d.HT[k]);
                }

            var go = new GameObject("Tent");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3(c.x, ground, c.y);
            go.transform.localRotation = Quaternion.Euler(0, d.TownAngle * Mathf.Rad2Deg, 0);
            go.AddComponent<MeshFilter>().sharedMesh = b.ToMesh("TentMesh");
            go.AddComponent<MeshRenderer>().sharedMaterial = MaterialLibrary.Objects(false);

            // dung truoc cua (cua huong +z cua leu = huong bien), quay mat vao thi tran
            Vector2 front = d.TownPoint(OX + (W - 1) / 2f, OZ + (D - 1) / 2f + 2f);
            home = new Vector3(front.x, d.GroundHeight(front.x, front.y), front.y);
            facing = Mathf.Atan2(d.CX - front.x, d.CZ - front.y);
            return go;
        }
    }
}
