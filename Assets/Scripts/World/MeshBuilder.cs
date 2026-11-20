// Gop nhieu khoi (cube/cylinder...) thanh 1 mesh co mau dinh (ban web: merge([P(...), ...])).
// Mau la sRGB nhu ma hex ban web; dung voi MaterialLibrary.Terrain (shader WebLambert, _VertexColorWeight = 1).
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace SereneIsle
{
    public sealed class MeshBuilder
    {
        readonly List<Vector3> _v = new List<Vector3>();
        readonly List<Vector3> _n = new List<Vector3>();
        readonly List<Color> _c = new List<Color>();
        readonly List<int> _t = new List<int>();

        public int VertexCount => _v.Count;

        public void Add(Mesh src, Matrix4x4 m, Color srgb)
        {
            var nm = m.inverse.transpose;
            int b = _v.Count;
            var sv = src.vertices; var sn = src.normals; var st = src.triangles;
            for (int i = 0; i < sv.Length; i++)
            {
                _v.Add(m.MultiplyPoint3x4(sv[i]));
                _n.Add(nm.MultiplyVector(sn[i]).normalized);
                _c.Add(srgb);
            }
            // ma tran lat (scale am) thi dao chieu tam giac
            bool flip = m.determinant < 0;
            for (int i = 0; i < st.Length; i += 3)
            {
                _t.Add(b + st[i]);
                if (flip) { _t.Add(b + st[i + 2]); _t.Add(b + st[i + 1]); }
                else { _t.Add(b + st[i + 1]); _t.Add(b + st[i + 2]); }
            }
        }

        /// <summary>P(BX, mau, x,y,z, sx,sy,sz, rx,ry,rz) cua ban web: hop don vi.</summary>
        public void Box(string hex, float x, float y, float z, float sx, float sy, float sz, float rx = 0, float ry = 0, float rz = 0)
            => Add(ProceduralMeshes.Primitive(PrimitiveType.Cube), Matrix4x4.TRS(new Vector3(x, y, z), ChibiCharacter.EulerXYZ(rx, ry, rz), new Vector3(sx, sy, sz)), MaterialLibrary.Hex(hex));

        /// <summary>P(g, mau, x,y,z, sx,sy,sz, rx,ry,rz) cua ban web voi khoi bat ky (Euler thu tu XYZ).</summary>
        public void P(Mesh g, string hex, float x = 0, float y = 0, float z = 0, float sx = 1, float sy = 1, float sz = 1, float rx = 0, float ry = 0, float rz = 0)
            => Add(g, Matrix4x4.TRS(new Vector3(x, y, z), ChibiCharacter.EulerXYZ(rx, ry, rz), new Vector3(sx, sy, sz)), MaterialLibrary.Hex(hex));

        /// <summary>PM: khoi dai 'len' bat dau tu 'from' theo huong 'dir' (la dua, la cay).</summary>
        public void PM(Mesh g, string hex, Vector3 from, Vector3 dir, float sx, float sy, float len)
        {
            var rot = Quaternion.LookRotation(dir, Mathf.Abs(dir.y) > .99f ? Vector3.forward : Vector3.up);
            Add(g, Matrix4x4.TRS(from + dir * (len * .5f), rot, new Vector3(sx, sy, len)), MaterialLibrary.Hex(hex));
        }

        /// <summary>THREE.CylinderGeometry(rTop, rBottom, h) tai (x,y,z). Dung ban kinh trung binh.</summary>
        public void Cylinder(string hex, float x, float y, float z, float rTop, float rBottom, float h)
        {
            float r = (rTop + rBottom) * .5f;
            Add(ProceduralMeshes.Cylinder, Matrix4x4.TRS(new Vector3(x, y, z), Quaternion.identity, new Vector3(2 * r, h * .5f, 2 * r)), MaterialLibrary.Hex(hex));
        }

        public Mesh ToMesh(string name)
        {
            var m = new Mesh { name = name };
            if (_v.Count > 65535) m.indexFormat = IndexFormat.UInt32;
            m.SetVertices(_v); m.SetNormals(_n); m.SetColors(_c); m.SetTriangles(_t, 0);
            m.RecalculateBounds();
            return m;
        }
    }
}
