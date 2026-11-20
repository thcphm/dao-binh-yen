using System.Collections.Generic;
using UnityEngine;

namespace SereneIsle
{
    /// <summary>Mesh dung chung: primitive cua Unity (khong collider) + LatheGeometry kieu three.js.</summary>
    public static class ProceduralMeshes
    {
        static readonly Dictionary<PrimitiveType, Mesh> _prims = new Dictionary<PrimitiveType, Mesh>();

        public static Mesh Primitive(PrimitiveType type)
        {
            if (_prims.TryGetValue(type, out var m) && m) return m;
            var go = GameObject.CreatePrimitive(type);
            m = go.GetComponent<MeshFilter>().sharedMesh;
            Object.DestroyImmediate(go);
            _prims[type] = m;
            return m;
        }

        public static Mesh Sphere => Primitive(PrimitiveType.Sphere);     // ban kinh 0.5
        public static Mesh Capsule => Primitive(PrimitiveType.Capsule);   // cao 2, ban kinh 0.5, truc Y
        public static Mesh Cylinder => Primitive(PrimitiveType.Cylinder); // cao 2, ban kinh 0.5, truc Y

        /// <summary>
        /// Quay bien dang (r, y) quanh truc Y. Bien dang phai di theo chieu sao cho mat ngoai nam ben phai
        /// huong di (vd: tu duoi len tren cho mat ngoai huong ra xa truc).
        /// </summary>
        public static Mesh Lathe(IList<Vector2> profile, int segments, string name = "Lathe")
        {
            int P = profile.Count, W = segments + 1;
            // phap tuyen bien dang: xoay tiep tuyen -90 do trong mat phang (r,y)
            var pn = new Vector2[P];
            for (int k = 0; k < P; k++)
            {
                Vector2 t = Vector2.zero;
                if (k > 0) t += (profile[k] - profile[k - 1]).normalized;
                if (k < P - 1) t += (profile[k + 1] - profile[k]).normalized;
                pn[k] = new Vector2(t.y, -t.x).normalized;
            }
            var v = new Vector3[P * W];
            var n = new Vector3[P * W];
            for (int j = 0; j <= segments; j++)
            {
                float phi = j / (float)segments * Mathf.PI * 2f, s = Mathf.Sin(phi), c = Mathf.Cos(phi);
                for (int k = 0; k < P; k++)
                {
                    float r = Mathf.Max(1e-4f, profile[k].x);
                    v[j * P + k] = new Vector3(r * s, profile[k].y, r * c);
                    n[j * P + k] = new Vector3(pn[k].x * s, pn[k].y, pn[k].x * c).normalized;
                }
            }
            var tri = new List<int>(segments * (P - 1) * 6);
            for (int j = 0; j < segments; j++)
                for (int k = 0; k < P - 1; k++)
                {
                    int a = j * P + k, b = (j + 1) * P + k, c = j * P + k + 1, d = (j + 1) * P + k + 1;
                    tri.Add(a); tri.Add(b); tri.Add(c);
                    tri.Add(b); tri.Add(d); tri.Add(c);
                }
            var mesh = new Mesh { name = name };
            mesh.SetVertices(v);
            mesh.SetNormals(n);
            mesh.SetTriangles(tri, 0);
            mesh.RecalculateBounds();
            return mesh;
        }

        /// <summary>Lang tru deu n canh, truc Y, cao h, ban kinh r, phap tuyen phang (THREE.CylinderGeometry(r,r,h,n)).
        /// Dinh dau tien nam o +z giong three.js.</summary>
        public static Mesh Prism(int n, float r, float h, string name = "Prism")
        {
            var v = new List<Vector3>(); var nr = new List<Vector3>(); var t = new List<int>();
            float y0 = -h / 2, y1 = h / 2;
            Vector3 P(int i) { float a = i / (float)n * Mathf.PI * 2; return new Vector3(r * Mathf.Sin(a), 0, r * Mathf.Cos(a)); }
            for (int i = 0; i < n; i++)
            {
                Vector3 a = P(i), b = P(i + 1), nn = ((a + b) * .5f).normalized;
                int s = v.Count;
                v.Add(a + Vector3.up * y0); v.Add(b + Vector3.up * y0); v.Add(a + Vector3.up * y1); v.Add(b + Vector3.up * y1);
                for (int k = 0; k < 4; k++) nr.Add(nn);
                // nhin tu ngoai: b nam ben trai man hinh -> (a0, b0, a1), (b0, b1, a1) theo chieu kim dong ho
                t.Add(s); t.Add(s + 1); t.Add(s + 2); t.Add(s + 1); t.Add(s + 3); t.Add(s + 2);
            }
            foreach (float y in new[] { y0, y1 })
            {
                int c = v.Count; v.Add(new Vector3(0, y, 0)); nr.Add(y > 0 ? Vector3.up : Vector3.down);
                for (int i = 0; i <= n; i++) { v.Add(P(i) + Vector3.up * y); nr.Add(y > 0 ? Vector3.up : Vector3.down); }
                for (int i = 0; i < n; i++)
                {
                    if (y > 0) { t.Add(c); t.Add(c + 1 + i); t.Add(c + 2 + i); }
                    else { t.Add(c); t.Add(c + 2 + i); t.Add(c + 1 + i); }
                }
            }
            var m = new Mesh { name = name };
            m.SetVertices(v); m.SetNormals(nr); m.SetTriangles(t, 0); m.RecalculateBounds();
            return m;
        }

        /// <summary>Chop cau don vi (ban kinh 1) tu dinh xuong goc theta (radian), giong SphereGeometry(1,..,0,2PI,0,thetaLength).</summary>
        public static Mesh SphereCap(float thetaLength, int segments = 24, int rings = 12)
        {
            var prof = new List<Vector2>();
            for (int k = rings; k >= 0; k--) // tu mep duoi len dinh -> mat ngoai
            {
                float th = thetaLength * k / rings;
                prof.Add(new Vector2(Mathf.Sin(th), Mathf.Cos(th)));
            }
            return Lathe(prof, segments, "SphereCap");
        }
    }
}
