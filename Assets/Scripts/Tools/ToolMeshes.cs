// Mo hinh dung cu cam tay (ban web: toolMesh dong 936-948).
// Quy uoc ban web: nam tay o goc toa do, can chia ra truoc (+z), luoi o dau can, canh luoi huong -y.
using System.Collections.Generic;
using UnityEngine;

namespace SereneIsle
{
    public static class ToolMeshes
    {
        static readonly Dictionary<string, Mesh> _cache = new Dictionary<string, Mesh>();

        public static bool Has(string id) => id == "axe" || id == "pick" || id == "hoe" || id == "shovel" || id == "can" || id == "rod" || id == "net" || id == "seed" || id == "build";

        /// <summary>Goc nghieng cua dung cu trong tay (ban web: g.rotation.x = tilt).</summary>
        public static float Tilt(string id) => id switch { "can" => 0, "seed" => 0, "rod" => -.55f, "net" => -.2f, _ => .5f };

        public static Mesh Get(string id, int level = 1)
        {
            string key = id + level;
            if (_cache.TryGetValue(key, out var m) && m) return m;
            var b = new MeshBuilder();
            const string wood = "#8a5a2e";
            string steel = level > 1 ? "#d88a4a" : "#c3ccd2", dark = level > 1 ? "#9a5a2a" : "#7d8890";
            void B(float w, float h, float d, string c, float x, float y, float z, float rx = 0) => b.Box(c, x, y, z, w, h, d, rx);
            switch (id)
            {
                case "axe": B(.045f, .045f, .62f, wood, 0, 0, .22f); B(.035f, .2f, .15f, steel, 0, -.075f, .5f); B(.03f, .035f, .2f, steel, 0, -.18f, .5f); B(.05f, .06f, .07f, dark, 0, .04f, .5f); break;
                case "pick": B(.045f, .045f, .66f, wood, 0, 0, .24f); B(.05f, .5f, .06f, dark, 0, 0, .55f); B(.04f, .08f, .04f, steel, 0, .28f, .55f); B(.04f, .08f, .04f, steel, 0, -.28f, .55f); B(.06f, .07f, .08f, steel, 0, 0, .55f); break;
                case "hoe": B(.04f, .04f, .82f, wood, 0, 0, .3f); B(.18f, .15f, .025f, steel, 0, -.08f, .7f, .15f); B(.05f, .05f, .06f, dark, 0, 0, .7f); break;
                case "shovel": B(.04f, .04f, .82f, wood, 0, 0, .3f); B(.16f, .04f, .04f, wood, 0, 0, -.1f); B(.17f, .03f, .22f, steel, 0, -.01f, .78f, .12f); B(.05f, .05f, .06f, dark, 0, 0, .66f); break;
                case "can": B(.2f, .2f, .26f, "#6fa7c9", 0, -.12f, .1f); B(.05f, .05f, .24f, "#3f6f8f", 0, -.06f, .32f, -.5f); B(.04f, .1f, .04f, "#3f6f8f", 0, 0, -.02f); break;
                case "rod": B(.03f, .03f, 1.35f, wood, 0, 0, .6f); B(.06f, .06f, .08f, "#555555", 0, -.04f, .05f); break;
                case "net":
                    B(.035f, .035f, .75f, wood, 0, 0, .3f);
                    b.P(LowPolyShapes.Torus(.18f, .022f, 5, 14, Mathf.PI * 2), "#5a7d3a", 0, 0, .85f);
                    b.P(ProceduralMeshes.Sphere, "#eef2ea", 0, -.05f, .85f, .3f, .14f, .3f);
                    break;
                case "seed": B(.16f, .18f, .13f, "#d8b27a", 0, -.08f, .02f); break;
                case "build": B(.04f, .04f, .36f, wood, 0, 0, .13f); B(.06f, .18f, .08f, "#7d8890", 0, -.02f, .3f); break; // bua
                default: return null;
            }
            m = b.ToMesh("Tool_" + key);
            _cache[key] = m;
            return m;
        }
    }
}
