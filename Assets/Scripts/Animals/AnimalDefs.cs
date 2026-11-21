// Cac loai thu (giai doan 4c). Thu lon dung mo hinh Quaternius co hoat canh (to mau lai cho hop dong que Viet Nam);
// thu nho dung khoi low-poly dung bang code (port ban web: hopGeo, monkeyGeo, fowlGeo, duckGeo, turtleGeo, crabGeo,
// goatGeo, catGeo dong 1114-1140). Tranh dong vat vung Uc (Docs muc 1).
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SereneIsle
{
    public enum AnimalRole { Wild, Farm, Pet }

    public sealed class AnimalDef
    {
        public string id, name;
        public AnimalRole role;
        public string quaternius;                 // ten mo hinh trong AnimalLibrary (null = khoi dung bang code)
        public Dictionary<string, string> colors; // ten vat lieu -> mau (mo hinh Quaternius)
        public float height = 1;                  // chieu cao muc tieu (o)
        public float speed = 1.4f;                // toc do di (o/giay)
        public float flee;                        // so nguoi choi khi lai gan (o); 0 = khong so
        public Biome[] biomes = new Biome[0];
        public bool hop;                          // nhay coc (tho, khi)
        public bool nocturnal;
        // vat nuoi
        public string pen, product, verb, feed;   // feed: "@crop" / "fiber"
        public int feedN = 1, productN = 1, price;
        public string calf;                       // item giong (a_chick...)
    }

    public static class AnimalDefs
    {
        static readonly Dictionary<string, string> EyesQ = new Dictionary<string, string>
        {
            ["Eye_Black"] = "#1a1a1a", ["Eye_White"] = "#f4f2ee", ["Eye_Lighter"] = "#3a3030",
            ["Eyes_Black"] = "#1a1a1a", ["Eyes_White"] = "#f4f2ee", ["Eyes_Pupil"] = "#1a1a1a",
        };

        static Dictionary<string, string> Q(params (string, string)[] c)
        {
            var d = new Dictionary<string, string>(EyesQ);
            foreach (var (k, v) in c) d[k] = v;
            return d;
        }

        public static readonly AnimalDef[] All =
        {
            // ---------------- thu hoang (ban web: WILD dong 1141) ----------------
            new AnimalDef { id = "tho", name = "Thỏ rừng", role = AnimalRole.Wild, height = .48f, speed = 4.5f, flee = 4, hop = true,
                            biomes = new[] { Biome.Bush, Biome.Savanna, Biome.Plains } },
            new AnimalDef { id = "huou", name = "Hươu sao", role = AnimalRole.Wild, quaternius = "Deer", height = 1.15f, speed = 5.5f, flee = 7,
                            biomes = new[] { Biome.Forest, Biome.Alpine, Biome.Plains },
                            colors = Q(("Main", "#a8683a"), ("Main_Dark", "#7a4a2a"), ("Main_Light", "#efe2c8"), ("Muzzle", "#3a2a22"), ("Hooves", "#3a2a22")) },
            new AnimalDef { id = "khi", name = "Khỉ đuôi dài", role = AnimalRole.Wild, height = .6f, speed = 4, flee = 3.5f, hop = true, biomes = new[] { Biome.Tropics } },
            new AnimalDef { id = "garung", name = "Gà rừng", role = AnimalRole.Wild, height = .48f, speed = 3.8f, flee = 4,
                            biomes = new[] { Biome.Bush, Biome.Tropics, Biome.Savanna } },
            new AnimalDef { id = "vit", name = "Vịt trời", role = AnimalRole.Wild, height = .32f, speed = 2.6f, flee = 4, biomes = new[] { Biome.Mangrove } },
            new AnimalDef { id = "rua", name = "Rùa biển", role = AnimalRole.Wild, height = .25f, speed = .7f, flee = 0, biomes = new[] { Biome.Beach } },
            new AnimalDef { id = "cua", name = "Cua đá", role = AnimalRole.Wild, height = .18f, speed = 2.4f, flee = 2.5f, biomes = new[] { Biome.Beach } },
            new AnimalDef { id = "denui", name = "Dê núi", role = AnimalRole.Wild, height = .8f, speed = 4.2f, flee = 5, biomes = new[] { Biome.Alpine, Biome.Desert } },

            // ---------------- vat nuoi (ban web: FARM dong 1822; them trau) ----------------
            new AnimalDef { id = "ga", name = "Gà", role = AnimalRole.Farm, height = .48f, speed = 1.6f, pen = "chuong_ga", calf = "a_chick", price = 120,
                            product = "egg", verb = "Nhặt trứng", feed = "@crop", feedN = 1 },
            new AnimalDef { id = "de", name = "Dê", role = AnimalRole.Farm, height = .8f, speed = 1.4f, pen = "chuong_trau", calf = "a_goat", price = 400,
                            product = "goatmilk", verb = "Vắt sữa dê", feed = "fiber", feedN = 2 },
            new AnimalDef { id = "bo", name = "Bò", role = AnimalRole.Farm, quaternius = "Cow", height = 1.2f, speed = 1.1f, pen = "chuong_trau", calf = "a_cow", price = 700,
                            product = "milk", verb = "Vắt sữa bò", feed = "fiber", feedN = 3,
                            colors = Q(("Main", "#c89050"), ("Main_Light", "#ecd0a0"), ("Muzzle", "#e0b098"), ("Horns", "#e0d6c0"), ("Hooves", "#3a2a22")) },
            new AnimalDef { id = "trau", name = "Trâu", role = AnimalRole.Farm, quaternius = "Bull", height = 1.25f, speed = 1.0f, pen = "chuong_trau", calf = "a_trau", price = 900,
                            product = "phan_chuong", productN = 2, verb = "Hốt phân chuồng", feed = "fiber", feedN = 3,
                            colors = Q(("Main", "#4c4c54"), ("Main_Light", "#6a6a72"), ("Muzzle", "#3a3a40"), ("Horns", "#cfc6b4"), ("Hooves", "#2a2a2a")) },

            // ---------------- thu cung ----------------
            new AnimalDef { id = "cho", name = "Chó", role = AnimalRole.Pet, quaternius = "ShibaInu", height = .55f, speed = 4.2f,
                            colors = Q(("Main", "#d29a50"), ("Main_Light", "#f2e2c4"), ("Black", "#2a2420")) },
            new AnimalDef { id = "meo", name = "Mèo", role = AnimalRole.Pet, height = .45f, speed = 4.2f },
        };

        public static AnimalDef Get(string id) => Array.Find(All, a => a.id == id);
        public static AnimalDef ByCalf(string item) => Array.Find(All, a => a.calf == item);

        public static int PenCapacity(string pen) => pen == "chuong_ga" ? 4 : pen == "chuong_trau" ? 2 : 0;
        public static string PenName(string pen) => pen == "chuong_ga" ? "Chuồng gà" : "Chuồng trâu bò";

        // ------------------------------------------------------------------ khoi dung bang code
        static readonly Dictionary<string, Mesh> _meshes = new Dictionary<string, Mesh>();

        public static Mesh MeshOf(string id)
        {
            if (_meshes.TryGetValue(id, out var m) && m) return m;
            var b = new MeshBuilder();
            var SP = LowPolyShapes.Sphere86;
            void Legs4(string c, float x, float z, float y, float h, float r = .028f)
            {
                foreach (var (a, q) in new[] { (-x, -z), (x, -z), (-x, z), (x, z) }) b.P(LowPolyShapes.Cylinder(r, r * .8f, h, 5), c, a, y, q);
            }
            void Eyes(float y, float z, float x = .05f, float s = .016f)
            {
                b.P(SP, "#1a1a1a", -x, y, z, s, s * 1.1f, s * .8f);
                b.P(SP, "#1a1a1a", x, y, z, s, s * 1.1f, s * .8f);
            }
            void Fowl(string body, string head, string tail)
            {
                b.P(SP, body, 0, .24f, 0, .13f, .12f, .17f);
                b.P(SP, tail, 0, .32f, -.17f, .04f, .13f, .11f, -.6f, 0, 0);
                b.P(SP, head, 0, .38f, .13f, .065f, .07f, .065f);
                b.P(SP, "#d83a2a", 0, .46f, .13f, .015f, .03f, .04f);
                b.P(LowPolyShapes.Cone(.018f, .05f, 4), "#e8b040", 0, .37f, .2f, 1, 1, 1, Mathf.PI / 2, 0, 0);
                b.P(SP, "#d83a2a", 0, .32f, .17f, .015f, .025f, .012f);
                Eyes(.4f, .17f, .035f, .01f);
                b.P(LowPolyShapes.Cylinder(.01f, .01f, .14f, 4), "#e8b040", -.04f, .07f, 0);
                b.P(LowPolyShapes.Cylinder(.01f, .01f, .14f, 4), "#e8b040", .04f, .07f, 0);
            }
            void Goat(string c)
            {
                b.P(SP, c, 0, .42f, 0, .15f, .15f, .27f);
                Legs4("#8a7a6a", .08f, .16f, .18f, .36f, .025f);
                b.P(SP, c, 0, .62f, .27f, .09f, .09f, .12f);
                b.P(SP, "#e8e0d0", 0, .56f, .36f, .035f, .04f, .03f);
                Eyes(.66f, .33f, .045f, .012f);
                b.P(LowPolyShapes.Cone(.02f, .14f, 5), "#6a5a4a", -.04f, .75f, .23f, 1, 1, 1, -.5f, 0, 0);
                b.P(LowPolyShapes.Cone(.02f, .14f, 5), "#6a5a4a", .04f, .75f, .23f, 1, 1, 1, -.5f, 0, 0);
                b.P(SP, c, -.09f, .65f, .24f, .04f, .02f, .03f);
                b.P(SP, c, .09f, .65f, .24f, .04f, .02f, .03f);
                b.P(SP, "#ece4d4", 0, .5f, .33f, .025f, .05f, .02f);
                b.P(SP, c, 0, .47f, -.27f, .03f, .05f, .03f);
            }
            switch (id)
            {
                case "tho":
                    b.P(SP, "#b8a898", 0, .17f, 0, .17f, .15f, .24f);
                    b.P(SP, "#c4b6a6", 0, .28f, .17f, .11f, .1f, .11f);
                    b.P(SP, "#a89888", -.045f, .45f, .15f, .025f, .09f, .016f, -.2f, 0, -.12f);
                    b.P(SP, "#a89888", .045f, .45f, .15f, .025f, .09f, .016f, -.2f, 0, .12f);
                    b.P(SP, "#f4eee4", 0, .2f, -.24f, .06f, .06f, .06f);
                    b.P(SP, "#1a1a1a", -.05f, .31f, .26f, .014f, .016f, .012f);
                    b.P(SP, "#1a1a1a", .05f, .31f, .26f, .014f, .016f, .012f);
                    b.P(SP, "#e8a0a0", 0, .27f, .28f, .016f, .012f, .012f);
                    b.P(SP, "#a89888", -.08f, .05f, .08f, .05f, .04f, .08f);
                    b.P(SP, "#a89888", .08f, .05f, .08f, .05f, .04f, .08f);
                    break;
                case "khi":
                    b.P(SP, "#7a5a3a", 0, .3f, 0, .13f, .16f, .12f);
                    b.P(SP, "#7a5a3a", 0, .52f, .04f, .11f, .1f, .1f);
                    b.P(SP, "#e0b090", 0, .5f, .12f, .07f, .06f, .03f);
                    Eyes(.53f, .14f, .035f, .013f);
                    b.P(SP, "#e0b090", -.11f, .53f, .03f, .035f, .035f, .02f);
                    b.P(SP, "#e0b090", .11f, .53f, .03f, .035f, .035f, .02f);
                    Legs4("#6a4a2e", .07f, .05f, .12f, .24f, .03f);
                    b.P(LowPolyShapes.Torus(.14f, .018f, 4, 10, Mathf.PI * 1.3f), "#6a4a2e", 0, .32f, -.16f, 1, 1, 1, 0, Mathf.PI / 2, 0);
                    break;
                case "garung": Fowl("#b3412a", "#c8582e", "#1f4a3a"); break;
                case "ga": Fowl("#f2ede2", "#f2ede2", "#e8e0d0"); break;
                case "vit":
                    b.P(SP, "#8a6a4a", 0, .14f, 0, .13f, .09f, .18f);
                    b.P(SP, "#2f6a3a", 0, .27f, .13f, .06f, .06f, .06f);
                    b.P(SP, "#e88a2a", 0, .25f, .2f, .03f, .012f, .04f);
                    Eyes(.29f, .17f, .03f, .01f);
                    b.P(SP, "#f2ede2", 0, .2f, .11f, .05f, .015f, .03f);
                    break;
                case "rua":
                    b.P(LowPolyShapes.Dodeca, "#5a6a3a", 0, .12f, 0, .3f, .12f, .36f);
                    b.P(LowPolyShapes.Dodeca, "#6f7f48", 0, .18f, 0, .22f, .08f, .27f);
                    b.P(SP, "#8a9a6a", 0, .1f, .4f, .08f, .07f, .09f);
                    Eyes(.13f, .47f, .035f, .012f);
                    foreach (var (a, q) in new[] { (-1, 1), (1, 1), (-1, -1), (1, -1) })
                        b.P(SP, "#8a9a6a", a * .26f, .05f, q * .22f, .12f, .025f, .06f, 0, a * q * .5f, 0);
                    break;
                case "cua":
                    b.P(SP, "#d0502e", 0, .1f, 0, .16f, .07f, .12f);
                    b.P(SP, "#e0603a", -.2f, .1f, .13f, .06f, .05f, .05f);
                    b.P(SP, "#e0603a", .2f, .1f, .13f, .06f, .05f, .05f);
                    b.P(SP, "#1a1a1a", -.04f, .17f, .1f, .015f, .03f, .015f);
                    b.P(SP, "#1a1a1a", .04f, .17f, .1f, .015f, .03f, .015f);
                    foreach (var a in new[] { -1, 1 })
                        foreach (var z in new[] { -.06f, 0, .06f })
                            b.Box("#b8402a", a * .17f, .05f, z, .12f, .02f, .02f, 0, 0, a * .4f);
                    break;
                case "denui": Goat("#d8d0c0"); break;
                case "de": Goat("#efe8da"); break;
                case "meo":
                    b.P(SP, "#c8a070", 0, .22f, 0, .1f, .1f, .2f);
                    Legs4("#b89060", .05f, .11f, .08f, .16f, .022f);
                    b.P(SP, "#c8a070", 0, .38f, .19f, .1f, .09f, .09f);
                    b.P(SP, "#f0e6d8", 0, .35f, .26f, .045f, .035f, .03f);
                    b.P(SP, "#e8909a", 0, .37f, .28f, .012f, .01f, .01f);
                    Eyes(.4f, .26f, .04f, .014f);
                    b.P(LowPolyShapes.Cone(.035f, .07f, 3), "#b89060", -.055f, .47f, .18f);
                    b.P(LowPolyShapes.Cone(.035f, .07f, 3), "#b89060", .055f, .47f, .18f);
                    b.P(LowPolyShapes.Torus(.13f, .017f, 4, 10, Mathf.PI), "#b89060", 0, .33f, -.2f, 1, 1, 1, 0, Mathf.PI / 2, 0);
                    foreach (var (x, y, z) in new[] { (-.05f, .25f, .02f), (.06f, .27f, -.08f), (0, .3f, -.12f) })
                        b.P(SP, "#9a7448", x, y, z, .04f, .03f, .05f);           // van meo muop
                    break;
                default:
                    b.P(SP, "#b8a898", 0, .2f, 0, .2f, .2f, .3f);
                    break;
            }
            m = b.ToMesh("Animal_" + id);
            _meshes[id] = m;
            return m;
        }
    }
}
