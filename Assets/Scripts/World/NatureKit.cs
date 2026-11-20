// Mo hinh cay / da / bui Quaternius Stylized Nature MegaKit (CC0) thay cho mo hinh tu dung cua ban web.
// Tao boi menu "Serene Isle > Nhap cay da Quaternius" (Editor/NatureKitImporter.cs) -> Assets/Resources/NatureKit.asset.
// Luc chay: gop cac mesh trong FBX thanh 1 mesh (moi vat lieu 1 submesh), dua ve goc toa do o chan cay,
// chinh chieu cao cho bang mo hinh cu (de giu ti le voi nhan vat va luoi o), tao material WebLambert tuong ung.
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SereneIsle
{
    public class NatureKit : ScriptableObject
    {
        [Serializable]
        public class Surface
        {
            public string material;          // ten vat lieu trong FBX (vd. "Leaves_NormalTree")
            public Texture2D texture;
            public bool cutout;              // la cay: cat alpha + ve 2 mat
            public Color tint = Color.white; // nhan mau (sRGB)
        }

        [Serializable]
        public class TypeSet
        {
            public WorldObjectType type;
            public List<GameObject> models = new List<GameObject>();
            [Tooltip("Chieu cao so voi mo hinh cu cua loai nay")] public float heightScale = 1f;
            [Tooltip("Mau tan la cho LOD o xa (sRGB)")] public Color lodColor = Color.green;
            [Tooltip("Thay texture cho moi be mat (vd. da do dung texture da sa mac)")] public Texture2D textureOverride;
            [Tooltip("Mau nhan thay the (alpha = 0: khong dung)")] public Color tintOverride = Color.clear;
        }

        public List<Surface> surfaces = new List<Surface>();
        public List<TypeSet> types = new List<TypeSet>();

        static NatureKit _instance;
        public static NatureKit Instance
        {
            get
            {
                if (!_instance) _instance = Resources.Load<NatureKit>("NatureKit");
                return _instance;
            }
        }

        public TypeSet Get(WorldObjectType t)
        {
            foreach (var s in types) if (s.type == t && s.models.Count > 0) return s;
            return null;
        }

        public Surface GetSurface(string material)
        {
            foreach (var s in surfaces) if (s.material == material) return s;
            // Unity co the them hau to khi import (vd. "Leaves_NormalTree.001")
            foreach (var s in surfaces) if (material.StartsWith(s.material)) return s;
            return null;
        }

        // ------------------------------------------------------------------ dung mesh luc chay
        public sealed class Variant
        {
            public Mesh mesh;
            public Material[] materials;   // theo submesh
        }

        static readonly Dictionary<(Texture2D, bool, Color32, bool), Material> _mats = new Dictionary<(Texture2D, bool, Color32, bool), Material>();

        /// <summary>Material WebLambert cho 1 be mat (cache theo texture/cat alpha/mau/gio).</summary>
        public static Material MaterialFor(Surface s, bool sway)
        {
            var key = (s.texture, s.cutout, (Color32)s.tint, sway);
            if (_mats.TryGetValue(key, out var m) && m) return m;
            m = new Material(MaterialLibrary.Lambert) { name = "SI_Kit_" + s.material, enableInstancing = true };
            m.SetColor("_BaseColor", s.tint);
            m.SetTexture("_BaseMap", s.texture);
            m.SetFloat("_VertexColorWeight", 0);
            m.SetFloat("_OcclusionFade", 1);
            if (sway) { m.SetFloat("_Sway", 1); m.EnableKeyword("_SWAY"); }
            if (s.cutout)
            {
                m.SetFloat("_AlphaClip", 1); m.EnableKeyword("_ALPHATEST_ON");
                m.SetFloat("_Cutoff", .5f);
                m.SetFloat("_Cull", (float)UnityEngine.Rendering.CullMode.Off);
            }
            _mats[key] = m;
            return m;
        }

        /// <summary>
        /// Gop mo hinh thanh 1 mesh, chan cay o y = 0, cao 'targetHeight' o. Mesh trong FBX phai doc duoc (isReadable).
        /// </summary>
        public Variant Build(GameObject model, float targetHeight, bool sway, Texture2D textureOverride = null, Color tintOverride = default)
        {
            var root = model.transform;
            var byMat = new Dictionary<string, List<int>>();
            var order = new List<string>();
            var verts = new List<Vector3>(); var norms = new List<Vector3>(); var uvs = new List<Vector2>();
            foreach (var mf in model.GetComponentsInChildren<MeshFilter>(true))
            {
                var src = mf.sharedMesh;
                if (!src || !src.isReadable) continue;
                var mr = mf.GetComponent<MeshRenderer>();
                var mats = mr ? mr.sharedMaterials : new Material[0];
                var m = root.worldToLocalMatrix * mf.transform.localToWorldMatrix;
                var nm = m.inverse.transpose;
                int b = verts.Count;
                var sv = src.vertices; var sn = src.normals; var su = src.uv;
                for (int i = 0; i < sv.Length; i++)
                {
                    verts.Add(m.MultiplyPoint3x4(sv[i]));
                    norms.Add(sn.Length > i ? nm.MultiplyVector(sn[i]).normalized : Vector3.up);
                    uvs.Add(su.Length > i ? su[i] : Vector2.zero);
                }
                bool flip = m.determinant < 0;
                for (int s = 0; s < src.subMeshCount; s++)
                {
                    string name = s < mats.Length && mats[s] ? mats[s].name : "default";
                    if (!byMat.TryGetValue(name, out var list)) { byMat[name] = list = new List<int>(); order.Add(name); }
                    var tri = src.GetTriangles(s);
                    for (int t = 0; t < tri.Length; t += 3)
                    {
                        list.Add(b + tri[t]);
                        list.Add(b + (flip ? tri[t + 2] : tri[t + 1]));
                        list.Add(b + (flip ? tri[t + 1] : tri[t + 2]));
                    }
                }
            }
            if (verts.Count == 0) return null;

            // chuan hoa: chan o y = 0, cao targetHeight
            float minY = float.MaxValue, maxY = float.MinValue;
            foreach (var v in verts) { minY = Mathf.Min(minY, v.y); maxY = Mathf.Max(maxY, v.y); }
            float k = targetHeight / Mathf.Max(.01f, maxY - minY);
            for (int i = 0; i < verts.Count; i++) { var v = verts[i]; verts[i] = new Vector3(v.x * k, (v.y - minY) * k, v.z * k); }

            var mesh = new Mesh { name = "Kit_" + model.name };
            if (verts.Count > 65535) mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            mesh.SetVertices(verts); mesh.SetNormals(norms); mesh.SetUVs(0, uvs);
            mesh.subMeshCount = order.Count;
            var materials = new Material[order.Count];
            for (int s = 0; s < order.Count; s++)
            {
                mesh.SetTriangles(byMat[order[s]], s);
                var surf = GetSurface(order[s]) ?? new Surface { material = order[s] };
                if (textureOverride || tintOverride.a > 0)
                    surf = new Surface
                    {
                        material = surf.material + "_" + (textureOverride ? textureOverride.name : "tint"),
                        texture = textureOverride ? textureOverride : surf.texture, cutout = surf.cutout,
                        tint = tintOverride.a > 0 ? tintOverride : surf.tint,
                    };
                materials[s] = MaterialFor(surf, sway);
            }
            mesh.RecalculateBounds();
            return new Variant { mesh = mesh, materials = materials };
        }
    }
}
