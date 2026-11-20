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
