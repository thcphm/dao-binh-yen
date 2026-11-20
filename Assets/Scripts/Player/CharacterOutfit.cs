// Trang phuc casual dung tren mo hinh KayKit (port tu trang duyet nhan vat, Review/character-viewer).
// KayKit to mau bang 1 anh bang mau 8 cot x 4 hang, moi o la 1 chat lieu (da, ao, that lung...).
// Ta phan loai tung tam giac theo (phan co the, o mau, xuong chinh) roi:
//   - chuyen UV sang o mau moi o hang 3 (ao, quan, giay...) -> doi mau chi can ve lai o do,
//   - hoac bo tam giac (tui deo), hoac giu nguyen (mat, toc...).
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SereneIsle
{
    [Serializable]
    public class CharacterOutfit
    {
        public string model = "Ranger";
        public string shirt = "#5b9bd5";
        public string pants = "#c8ad7d";
        public string shoe = "#f6f6f2";
        public string skin = "#eab489";
        public string hair = "#4a2e22";
        public bool longSleeve;
        public bool shorts;
        public bool noBelt;

        public static CharacterOutfit Male() => new CharacterOutfit();
        public static CharacterOutfit Female() => new CharacterOutfit
        {
            model = "Rogue", shirt = "#f2c14e", pants = "#4a6a9c", shoe = "#d9534f", skin = "#f8cfae", hair = "#a95f3d", shorts = true,
        };

        public CharacterOutfit Clone() => (CharacterOutfit)MemberwiseClone();

        /// <summary>Bang mau goi y (giong trang duyet).</summary>
        public static readonly string[] ShirtColors = { "#f2c14e", "#f4f1e8", "#5b9bd5", "#ef8f7f", "#7cb66b", "#e46a4e" };
        public static readonly string[] PantsColors = { "#4a6a9c", "#c8ad7d", "#6e7178", "#ece8dc", "#7a5a40" };
        public static readonly string[] ShoeColors = { "#f6f6f2", "#d9534f", "#2f4068", "#f0c040" };
        public static readonly string[] SkinColors = { "#f8cfae", "#eab489", "#c98d63", "#8f5c3d" };
        public static readonly string[] HairColors = { "#a95f3d", "#4a2e22", "#2a2422", "#e0aa4e", "#6f8a4f" };
    }

    public static class OutfitBuilder
    {
        // o moi tren hang 3 (khong dung o ban goc); da dung lai o (0,0). [hang, cot], hang tinh tu tren xuong.
        public enum Slot { Keep, Del, Shirt, Trim, Pants, Waist, Sock, Shoe, Sole, Hair, Skin }
        static readonly Dictionary<Slot, Vector2Int> Cell = new Dictionary<Slot, Vector2Int>
        {
            { Slot.Shirt, new Vector2Int(3, 0) }, { Slot.Trim, new Vector2Int(3, 1) }, { Slot.Pants, new Vector2Int(3, 2) },
            { Slot.Waist, new Vector2Int(3, 3) }, { Slot.Sock, new Vector2Int(3, 4) }, { Slot.Shoe, new Vector2Int(3, 5) },
            { Slot.Sole, new Vector2Int(3, 6) }, { Slot.Hair, new Vector2Int(3, 7) }, { Slot.Skin, new Vector2Int(0, 0) },
        };

        // vai tro cua tung o mau (hang,cot) theo nhan vat - doc tu anh bang mau va phan bo tam giac
        static readonly Dictionary<string, Dictionary<string, string>> Roles = new Dictionary<string, Dictionary<string, string>>
        {
            ["Rogue"] = new Dictionary<string, string>
            {
                ["0,0"] = "skin", ["0,1"] = "hair", ["1,0"] = "tunic", ["1,1"] = "trim", ["0,5"] = "belt", ["0,6"] = "pouch",
                ["0,3"] = "metal", ["1,7"] = "pants", ["2,3"] = "boots", ["2,7"] = "glove",
            },
            ["Ranger"] = new Dictionary<string, string>
            {
                ["0,0"] = "skin", ["0,1"] = "hair", ["0,5"] = "tunic", ["0,6"] = "tunic", ["1,0"] = "trim", ["0,7"] = "belt",
                ["1,1"] = "pouch", ["0,3"] = "metal", ["1,7"] = "pants", ["2,3"] = "boots", ["2,7"] = "glove",
            },
        };

        public static bool Supports(string model) => Roles.ContainsKey(model);

        /// <summary>"Rogue_ArmLeft" -> "ArmLeft".</summary>
        public static string PartOf(string rendererName)
        {
            int i = rendererName.LastIndexOf('_');
            return i >= 0 ? rendererName.Substring(i + 1) : rendererName;
        }

        public static bool IsBasePart(string part)
            => part == "ArmLeft" || part == "ArmRight" || part == "Body" || part == "Head" || part == "LegLeft" || part == "LegRight";

        static Slot Classify(string part, string role, string bone, bool sole, CharacterOutfit o)
        {
            if (part == "Head") return role == "hair" ? Slot.Hair : Slot.Keep;
            if (role == "skin") return Slot.Keep;
            if (part.StartsWith("Arm"))
            {
                if (bone.StartsWith("upperarm")) return Slot.Shirt;
                if (role == "glove" || role == "metal") return Slot.Skin;
                if (bone.StartsWith("lowerarm") && o.longSleeve) return role == "trim" ? Slot.Trim : Slot.Shirt;
                return Slot.Skin;
            }
            if (part == "Body")
            {
                switch (role)
                {
                    case "tunic": return Slot.Shirt;
                    case "trim": return Slot.Trim;
                    case "pants": return Slot.Pants;
                    // that lung + khoa la mot phan cua than (xoa se thung) -> to mau ao; tui deo thi go han
                    case "belt": case "metal": return o.noBelt ? Slot.Shirt : Slot.Waist;
                    case "pouch": return o.noBelt ? Slot.Del : Slot.Waist;
                    default: return Slot.Shirt;
                }
            }
            if (part.StartsWith("Leg"))
            {
                if (bone.StartsWith("upperleg")) return Slot.Pants;
                if (bone.StartsWith("lowerleg")) return o.shorts ? Slot.Sock : Slot.Pants;
                return sole ? Slot.Sole : Slot.Shoe;
            }
            return Slot.Keep;
        }

        /// <summary>
        /// Tao mesh casual tu mesh goc (phai doc duoc). 'up' = vi tri dinh trong khong gian mo hinh (de tim de giay).
        /// UV cua FBX trong Unity: v = 0 o duoi anh, nen hang (tinh tu tren) = floor((1 - v) * 4).
        /// </summary>
        public static Mesh BuildMesh(Mesh src, Transform[] bones, string model, string part, Func<Vector3, float> modelHeight, CharacterOutfit o)
        {
            var roles = Roles[model];
            var v = src.vertices; var n = src.normals; var uv = src.uv; var bw = src.boneWeights; var tris = src.triangles;
            float minY = float.MaxValue, maxY = float.MinValue;
            var hy = new float[v.Length];
            for (int i = 0; i < v.Length; i++) { hy[i] = modelHeight(v[i]); minY = Mathf.Min(minY, hy[i]); maxY = Mathf.Max(maxY, hy[i]); }
            float soleY = minY + (maxY - minY) * .085f;

            var nv = new List<Vector3>(tris.Length); var nn = new List<Vector3>(tris.Length);
            var nuv = new List<Vector2>(tris.Length); var nbw = new List<BoneWeight>(tris.Length); var nt = new List<int>(tris.Length);
            for (int t = 0; t < tris.Length; t += 3)
            {
                int a = tris[t], b = tris[t + 1], c = tris[t + 2];
                float u = (uv[a].x + uv[b].x + uv[c].x) / 3f, vt = 1f - (uv[a].y + uv[b].y + uv[c].y) / 3f;
                int col = Mathf.Clamp((int)(u * 8), 0, 7), row = Mathf.Clamp((int)(vt * 4), 0, 3);
                roles.TryGetValue(row + "," + col, out string role);
                string ba = BoneName(bones, bw[a]), bb = BoneName(bones, bw[b]), bc = BoneName(bones, bw[c]);
                string bone = bb == bc ? bb : ba;
                bool sole = Mathf.Max(hy[a], Mathf.Max(hy[b], hy[c])) < soleY;
                var slot = Classify(part, role ?? "other", bone, sole, o);
                if (slot == Slot.Del) continue;
                foreach (int k in new[] { a, b, c })
                {
                    var q = uv[k];
                    if (slot != Slot.Keep)
                    {
                        var cell = Cell[slot];
                        float ru = Mathf.Clamp(q.x * 8 - col, .04f, .96f), rv = Mathf.Clamp((1f - q.y) * 4 - row, .04f, .96f);
                        q = new Vector2((cell.y + ru) / 8f, 1f - (cell.x + rv) / 4f);
                    }
                    nt.Add(nv.Count);
                    nv.Add(v[k]); nn.Add(n[k]); nuv.Add(q); nbw.Add(bw[k]);
                }
            }
            var m = new Mesh { name = src.name + "_casual" };
            if (nv.Count > 65535) m.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            m.SetVertices(nv); m.SetNormals(nn); m.SetUVs(0, nuv);
            m.boneWeights = nbw.ToArray();
            m.bindposes = src.bindposes;
            m.SetTriangles(nt, 0);
            m.bounds = src.bounds;
            return m;
        }

        static string BoneName(Transform[] bones, BoneWeight w)
        {
            // Unity sap trong so giam dan: boneIndex0 la xuong chinh
            int i = w.boneIndex0;
            return i >= 0 && i < bones.Length && bones[i] ? bones[i].name : "";
        }

        // ------------------------------------------------------------------ bang mau
        /// <summary>Tao texture bang mau rieng (copy anh goc, ve lai hang 3 + o da).</summary>
        public static Texture2D NewPalette(Texture2D src)
        {
            var tex = new Texture2D(src.width, src.height, TextureFormat.RGBA32, true, false)
            {
                name = src.name + "_casual", filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp,
            };
            tex.SetPixels32(src.GetPixels32());
            return tex;
        }

        public static void Paint(Texture2D tex, Texture2D src, CharacterOutfit o)
        {
            PaintCell(tex, Cell[Slot.Skin], o.skin, 1.06f, .9f);
            PaintCell(tex, Cell[Slot.Shirt], o.shirt);
            PaintCell(tex, Cell[Slot.Trim], Mix(o.shirt, Color.white, .45f));
            PaintCell(tex, Cell[Slot.Pants], o.pants);
            PaintCell(tex, Cell[Slot.Waist], Mix(o.pants, Color.black, .3f));
            PaintCell(tex, Cell[Slot.Sock], "#f4f3ee", 1.05f, .8f);
            PaintCell(tex, Cell[Slot.Shoe], o.shoe);
            PaintCell(tex, Cell[Slot.Sole], o.shoe == "#f6f6f2" ? "#c9ccd2" : "#f6f6f2", 1.05f, .85f);
            PaintCell(tex, Cell[Slot.Hair], o.hair, 1.2f, .62f);
            tex.Apply(true);
        }

        static string Mix(string hex, Color to, float t) => "#" + ColorUtility.ToHtmlStringRGB(Color.Lerp(MaterialLibrary.Hex(hex), to, t));

        static float Shade(float c, float k) => k >= 1 ? c + (1 - c) * (k - 1) : c * k;

        /// <summary>To 1 o bang dai mau doc: tren sang (top), duoi toi (bot) - giong o goc cua KayKit.</summary>
        static void PaintCell(Texture2D tex, Vector2Int cell, string hex, float top = 1.14f, float bot = .7f)
        {
            int cw = tex.width / 8, ch = tex.height / 4;
            int x0 = cell.y * cw, y0 = (3 - cell.x) * ch; // Unity: y = 0 o duoi anh
            var c = MaterialLibrary.Hex(hex);
            var px = new Color32[cw * ch];
            for (int y = 0; y < ch; y++)
            {
                float t = 1f - (y + .5f) / ch; // 0 o dinh o, 1 o day o
                float k = Mathf.Lerp(top, bot, t);
                Color32 col = new Color(Mathf.Clamp01(Shade(c.r, k)), Mathf.Clamp01(Shade(c.g, k)), Mathf.Clamp01(Shade(c.b, k)), 1);
                for (int x = 0; x < cw; x++) px[y * cw + x] = col;
            }
            tex.SetPixels32(x0, y0, cw, ch, px);
        }
    }
}
