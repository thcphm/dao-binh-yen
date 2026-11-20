// Khoi hinh co ban giong three.js r128 (dung lai cho cay, da, nha):
//   BoxGeometry(1,1,1), IcosahedronGeometry(1,0|1), DodecahedronGeometry(1,0), OctahedronGeometry(1,0),
//   SphereGeometry(1,w,h), CylinderGeometry(rt,rb,h,n), ConeGeometry(r,h,n), TorusGeometry(R,r,rs,ts,arc).
// Chieu tam giac tu sua theo phap tuyen (mat truoc huong ra ngoai) nen khong lo sai winding giua 2 he toa do.
using System.Collections.Generic;
using UnityEngine;

namespace SereneIsle
{
    public static class LowPolyShapes
    {
        static readonly Dictionary<string, Mesh> _cache = new Dictionary<string, Mesh>();

        static Mesh Cached(string key, System.Func<Mesh> make)
        {
            if (_cache.TryGetValue(key, out var m) && m) return m;
            m = make(); m.name = key; _cache[key] = m;
            return m;
        }

        public static Mesh Box => Cached("Box", () => ProceduralMeshes.Primitive(PrimitiveType.Cube));
        public static Mesh Ico0 => Cached("Ico0", () => Icosahedron(0));
        public static Mesh Ico1 => Cached("Ico1", () => Icosahedron(1));
        public static Mesh Dodeca => Cached("Dodeca", Dodecahedron);
        public static Mesh Octa => Cached("Octa", Octahedron);
        public static Mesh Sphere86 => Cached("Sphere86", () => Sphere(8, 6));
        public static Mesh Cylinder(float rt, float rb, float h, int n) => Cached($"Cyl_{rt}_{rb}_{h}_{n}", () => MakeCylinder(rt, rb, h, n));
        public static Mesh Cone(float r, float h, int n) => Cached($"Cone_{r}_{h}_{n}", () => MakeCylinder(0, r, h, n));
        public static Mesh Torus(float R, float r, int rs, int ts, float arc) => Cached($"Torus_{R}_{r}_{rs}_{ts}_{arc}", () => MakeTorus(R, r, rs, ts, arc));

        // ---------------------------------------------------------------- xay dung chung
        sealed class Builder
        {
            public readonly List<Vector3> v = new List<Vector3>();
            public readonly List<Vector3> n = new List<Vector3>();
            public readonly List<int> t = new List<int>();

            /// <summary>Them tam giac; dao chieu neu mat truoc khong cung huong voi 'outward'.</summary>
            public void Tri(Vector3 a, Vector3 b, Vector3 c, Vector3 na, Vector3 nb, Vector3 nc, Vector3 outward)
            {
                Vector3 fn = Vector3.Cross(b - a, c - a);
                if (Vector3.Dot(fn, outward) < 0) { (b, c) = (c, b); (nb, nc) = (nc, nb); }
                int s = v.Count;
                v.Add(a); v.Add(b); v.Add(c); n.Add(na); n.Add(nb); n.Add(nc);
                t.Add(s); t.Add(s + 1); t.Add(s + 2);
            }

            public void FlatTri(Vector3 a, Vector3 b, Vector3 c, Vector3 outward)
            {
                Vector3 fn = Vector3.Cross(b - a, c - a);
                if (Vector3.Dot(fn, outward) < 0) { (b, c) = (c, b); fn = -fn; }
                fn.Normalize();
                Tri(a, b, c, fn, fn, fn, fn);
            }

            public Mesh ToMesh()
            {
                var m = new Mesh();
                m.SetVertices(v); m.SetNormals(n); m.SetTriangles(t, 0); m.RecalculateBounds();
                return m;
            }
        }

        // ---------------------------------------------------------------- da dien
        static Mesh Icosahedron(int detail)
        {
            float p = (1 + Mathf.Sqrt(5)) / 2;
            var V = new[]
            {
                new Vector3(-1, p, 0), new Vector3(1, p, 0), new Vector3(-1, -p, 0), new Vector3(1, -p, 0),
                new Vector3(0, -1, p), new Vector3(0, 1, p), new Vector3(0, -1, -p), new Vector3(0, 1, -p),
                new Vector3(p, 0, -1), new Vector3(p, 0, 1), new Vector3(-p, 0, -1), new Vector3(-p, 0, 1),
            };
            int[] F = { 0,11,5, 0,5,1, 0,1,7, 0,7,10, 0,10,11, 1,5,9, 5,11,4, 11,10,2, 10,7,6, 7,1,8,
                        3,9,4, 3,4,2, 3,2,6, 3,6,8, 3,8,9, 4,9,5, 2,4,11, 6,2,10, 8,6,7, 9,8,1 };
            var tris = new List<Vector3[]>();
            for (int i = 0; i < F.Length; i += 3) tris.Add(new[] { V[F[i]].normalized, V[F[i + 1]].normalized, V[F[i + 2]].normalized });
            for (int d = 0; d < detail; d++)
            {
                var next = new List<Vector3[]>();
                foreach (var tr in tris)
                {
                    Vector3 a = tr[0], b = tr[1], c = tr[2];
                    Vector3 ab = ((a + b) * .5f).normalized, bc = ((b + c) * .5f).normalized, ca = ((c + a) * .5f).normalized;
                    next.Add(new[] { a, ab, ca }); next.Add(new[] { ab, b, bc }); next.Add(new[] { ca, bc, c }); next.Add(new[] { ab, bc, ca });
                }
                tris = next;
            }
            var bld = new Builder();
            foreach (var tr in tris)
            {
                Vector3 ctr = (tr[0] + tr[1] + tr[2]) / 3f;
                if (detail == 0) bld.FlatTri(tr[0], tr[1], tr[2], ctr);
                else bld.Tri(tr[0], tr[1], tr[2], tr[0], tr[1], tr[2], ctr); // ban kinh 1 -> phap tuyen = vi tri
            }
            return bld.ToMesh();
        }

        static Mesh Octahedron()
        {
            var V = new[] { Vector3.right, Vector3.left, Vector3.up, Vector3.down, Vector3.forward, Vector3.back };
            int[] F = { 0,2,4, 0,4,3, 0,3,5, 0,5,2, 1,2,5, 1,5,3, 1,3,4, 1,4,2 };
            var bld = new Builder();
            for (int i = 0; i < F.Length; i += 3) bld.FlatTri(V[F[i]], V[F[i + 1]], V[F[i + 2]], V[F[i]] + V[F[i + 1]] + V[F[i + 2]]);
            return bld.ToMesh();
        }

        static Mesh Dodecahedron()
        {
            float t = (1 + Mathf.Sqrt(5)) / 2, r = 1 / t;
            var V = new[]
            {
                new Vector3(-1,-1,-1), new Vector3(-1,-1,1), new Vector3(-1,1,-1), new Vector3(-1,1,1),
                new Vector3(1,-1,-1), new Vector3(1,-1,1), new Vector3(1,1,-1), new Vector3(1,1,1),
                new Vector3(0,-r,-t), new Vector3(0,-r,t), new Vector3(0,r,-t), new Vector3(0,r,t),
                new Vector3(-r,-t,0), new Vector3(-r,t,0), new Vector3(r,-t,0), new Vector3(r,t,0),
                new Vector3(-t,0,-r), new Vector3(t,0,-r), new Vector3(-t,0,r), new Vector3(t,0,r),
            };
            // bang mat cua three.js DodecahedronGeometry (36 tam giac)
            int[] F = { 3,11,7, 3,7,15, 3,15,13, 7,19,17, 7,17,6, 7,6,15, 17,4,8, 17,8,10, 17,10,6, 8,0,16, 8,16,2, 8,2,10,
                        0,12,1, 0,1,18, 0,18,16, 6,10,2, 6,2,13, 6,13,15, 2,16,18, 2,18,3, 2,3,13, 18,1,9, 18,9,11, 18,11,3,
                        4,14,12, 4,12,0, 4,0,8, 11,9,5, 11,5,19, 11,19,7, 19,5,14, 19,14,4, 19,4,17, 1,12,14, 1,14,5, 1,5,9 };
            var bld = new Builder();
            for (int i = 0; i < F.Length; i += 3)
            {
                Vector3 a = V[F[i]].normalized, b = V[F[i + 1]].normalized, c = V[F[i + 2]].normalized;
                bld.FlatTri(a, b, c, a + b + c);
            }
            return bld.ToMesh();
        }

        static Mesh Sphere(int w, int h)
        {
            var bld = new Builder();
            Vector3 P(int x, int y)
            {
                float u = x / (float)w * Mathf.PI * 2, vv = y / (float)h * Mathf.PI;
                return new Vector3(-Mathf.Cos(u) * Mathf.Sin(vv), Mathf.Cos(vv), Mathf.Sin(u) * Mathf.Sin(vv));
            }
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    Vector3 a = P(x, y), b = P(x + 1, y), c = P(x, y + 1), d = P(x + 1, y + 1);
                    Vector3 ctr = (a + b + c + d) * .25f;
                    if (y > 0) bld.Tri(a, b, c, a, b, c, ctr);
                    if (y < h - 1) bld.Tri(b, d, c, b, d, c, ctr);
                }
            return bld.ToMesh();
        }

        // ---------------------------------------------------------------- tru / non
        static Mesh MakeCylinder(float rt, float rb, float h, int n)
        {
            var bld = new Builder();
            float slope = (rb - rt) / h;
            Vector3 Ring(int i, float r, float y) { float a = i / (float)n * Mathf.PI * 2; return new Vector3(r * Mathf.Sin(a), y, r * Mathf.Cos(a)); }
            Vector3 Nrm(int i) { float a = i / (float)n * Mathf.PI * 2; return new Vector3(Mathf.Sin(a), slope, Mathf.Cos(a)).normalized; }
            float y0 = -h / 2, y1 = h / 2;
            for (int i = 0; i < n; i++)
            {
                Vector3 b0 = Ring(i, rb, y0), b1 = Ring(i + 1, rb, y0), t0 = Ring(i, rt, y1), t1 = Ring(i + 1, rt, y1);
                Vector3 n0 = Nrm(i), n1 = Nrm(i + 1), outw = n0 + n1;
                if (rb > 0) bld.Tri(b0, b1, t0, n0, n1, rt > 0 ? n0 : (n0 + n1).normalized, outw);
                if (rt > 0) bld.Tri(b1, t1, t0, n1, n1, n0, outw);
                if (rb > 0) bld.FlatTri(new Vector3(0, y0, 0), b1, b0, Vector3.down);
                if (rt > 0) bld.FlatTri(new Vector3(0, y1, 0), t0, t1, Vector3.up);
            }
            return bld.ToMesh();
        }

        static Mesh MakeTorus(float R, float r, int rs, int ts, float arc)
        {
            var bld = new Builder();
            Vector3 P(int j, int i, out Vector3 nn)
            {
                float u = i / (float)ts * arc, v = j / (float)rs * Mathf.PI * 2;
                var c = new Vector3(R * Mathf.Cos(u), R * Mathf.Sin(u), 0);
                var p = new Vector3((R + r * Mathf.Cos(v)) * Mathf.Cos(u), (R + r * Mathf.Cos(v)) * Mathf.Sin(u), r * Mathf.Sin(v));
                nn = (p - c).normalized;
                return p;
            }
            for (int j = 0; j < rs; j++)
                for (int i = 0; i < ts; i++)
                {
                    var a = P(j, i, out var na); var b = P(j + 1, i, out var nb); var c = P(j, i + 1, out var nc); var d = P(j + 1, i + 1, out var nd);
                    bld.Tri(a, b, c, na, nb, nc, na + nb + nc);
                    bld.Tri(b, d, c, nb, nd, nc, nb + nd + nc);
                }
            return bld.ToMesh();
        }

        /// <summary>LatheGeometry: bien dang (r,y) quay quanh Y, phap tuyen tron.</summary>
        public static Mesh Lathe(IList<Vector2> prof, int seg)
        {
            var bld = new Builder();
            Vector3 P(int k, int j) { float a = j / (float)seg * Mathf.PI * 2; return new Vector3(prof[k].x * Mathf.Sin(a), prof[k].y, prof[k].x * Mathf.Cos(a)); }
            for (int k = 0; k < prof.Count - 1; k++)
                for (int j = 0; j < seg; j++)
                {
                    Vector3 a = P(k, j), b = P(k, j + 1), c = P(k + 1, j), d = P(k + 1, j + 1);
                    Vector3 outw = new Vector3(a.x + b.x + c.x + d.x, 0, a.z + b.z + c.z + d.z);
                    Vector3 na = new Vector3(a.x, 0, a.z).normalized, nb = new Vector3(b.x, 0, b.z).normalized;
                    Vector3 nc = new Vector3(c.x, 0, c.z).normalized, nd = new Vector3(d.x, 0, d.z).normalized;
                    bld.Tri(a, b, c, na, nb, nc, outw);
                    bld.Tri(b, d, c, nb, nd, nc, outw);
                }
            return bld.ToMesh();
        }
    }
}
