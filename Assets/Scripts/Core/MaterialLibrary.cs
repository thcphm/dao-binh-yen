using System.Collections.Generic;
using UnityEngine;

namespace SereneIsle
{
    /// <summary>Tao va cache material dung shader SereneIsle/* (nam trong Assets/Resources/Shaders).</summary>
    public static class MaterialLibrary
    {
        static Shader _lambert, _water;
        static Material _terrain;
        static readonly Dictionary<Color32, Material> _solid = new Dictionary<Color32, Material>();

        static Shader Find(ref Shader cache, string name)
        {
            if (cache) return cache;
            cache = Shader.Find(name);
            if (!cache)
            {
                Debug.LogWarning($"[SereneIsle] Khong tim thay shader '{name}', dung URP Lit (se mat mau dinh).");
                cache = Shader.Find("Universal Render Pipeline/Lit");
            }
            return cache;
        }

        public static Shader Lambert => Find(ref _lambert, "SereneIsle/WebLambert");
        public static Shader WaterShader => Find(ref _water, "SereneIsle/Water");

        /// <summary>Material dia hinh: dung mau dinh.</summary>
        public static Material Terrain
        {
            get
            {
                if (_terrain) return _terrain;
                _terrain = new Material(Lambert) { name = "SI_Terrain" };
                _terrain.SetColor("_BaseColor", Color.white);
                _terrain.SetFloat("_VertexColorWeight", 1f);
                return _terrain;
            }
        }

        static Material _obj, _veg, _grass;

        /// <summary>Vat the dung mau dinh (cay, da, nha...): thua khi che nhan vat; sway = lay theo gio (ban web objMat / vegMat).</summary>
        public static Material Objects(bool sway)
        {
            ref Material m = ref sway ? ref _veg : ref _obj;
            if (m) return m;
            m = new Material(Lambert) { name = sway ? "SI_Vegetation" : "SI_Objects", enableInstancing = true };
            m.SetColor("_BaseColor", Color.white);
            m.SetFloat("_VertexColorWeight", 1f);
            m.SetFloat("_OcclusionFade", 1f);
            if (sway) { m.SetFloat("_Sway", 1f); m.EnableKeyword("_SWAY"); }
            return m;
        }

        static Material _clouds;
        /// <summary>May: trang, chi chiu 1 phan suong mu de nhin thay tu xa.</summary>
        public static Material Clouds(float fogScale)
        {
            if (_clouds) return _clouds;
            _clouds = new Material(Lambert) { name = "SI_Clouds" };
            _clouds.SetColor("_BaseColor", Color.white);
            _clouds.SetFloat("_VertexColorWeight", 1f);
            _clouds.SetFloat("_FogScale", fogScale);
            return _clouds;
        }

        /// <summary>Co: lay theo gio, khong thua (qua thap de che nhan vat). Mau theo biome qua MaterialPropertyBlock.</summary>
        public static Material Grass()
        {
            if (_grass) return _grass;
            _grass = new Material(Lambert) { name = "SI_Grass", enableInstancing = true };
            _grass.SetColor("_BaseColor", Color.white);
            _grass.SetFloat("_VertexColorWeight", 1f);
            _grass.SetFloat("_Sway", 1f);
            _grass.EnableKeyword("_SWAY");
            return _grass;
        }

        /// <summary>Material mau don (nhan vat, vat the). Mau nhap theo sRGB nhu ma hex ban web.</summary>
        public static Material Solid(Color srgb)
        {
            Color32 key = srgb;
            if (_solid.TryGetValue(key, out var m) && m) return m;
            m = new Material(Lambert) { name = "SI_" + ColorUtility.ToHtmlStringRGB(srgb) };
            m.SetColor("_BaseColor", srgb);
            m.SetFloat("_VertexColorWeight", 0f);
            m.enableInstancing = true;
            _solid[key] = m;
            return m;
        }

        public static Material Solid(string hex) => Solid(Hex(hex));

        public static Material NewWater()
        {
            var m = new Material(WaterShader) { name = "SI_Water" };
            m.renderQueue = 3000;
            return m;
        }

        /// <summary>"#rrggbb" -> Color (sRGB). C# thuan, goi duoc trong static initializer / thread phu.</summary>
        public static Color Hex(string hex)
        {
            if (string.IsNullOrEmpty(hex)) return Color.magenta;
            hex = hex.TrimStart('#');
            if (hex.Length == 6 && int.TryParse(hex, System.Globalization.NumberStyles.HexNumber,
                                                System.Globalization.CultureInfo.InvariantCulture, out int v))
                return new Color(((v >> 16) & 255) / 255f, ((v >> 8) & 255) / 255f, (v & 255) / 255f, 1f);
            return Color.magenta;
        }
    }
}
