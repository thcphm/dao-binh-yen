// Hinh 3D low-poly cho cay trong theo dang cay va giai doan (mam, non, lon, chin), dung mau dinh (MaterialLibrary.Objects).
// Tam goc o (0,0,0) = mat dat giua o.
using System.Collections.Generic;
using UnityEngine;

namespace SereneIsle
{
    public static class CropMeshes
    {
        static readonly Dictionary<string, Mesh> _cache = new Dictionary<string, Mesh>();
        static readonly Color Stake = MaterialLibrary.Hex("#8a6a40"), Dead = MaterialLibrary.Hex("#8a6a3a"), Trunk = MaterialLibrary.Hex("#7a5434");

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { _cache.Clear(); }

        /// <summary>state: 0 binh thuong, 1 dang heo, 2 chet.</summary>
        public static Mesh Get(CropDefinition c, int stage, int state)
        {
            string key = $"{c.id}|{stage}|{state}";
            if (_cache.TryGetValue(key, out var m) && m) return m;
            m = Build(c, stage, state);
            m.name = "Crop_" + key;
            _cache[key] = m;
            return m;
        }

        static void P(MeshBuilder b, Mesh g, Color col, Vector3 pos, Vector3 scale, Vector3 euler = default)
            => b.Add(g, Matrix4x4.TRS(pos, Quaternion.Euler(euler), scale), col);

        static Mesh Build(CropDefinition c, int stage, int state)
        {
            var b = new MeshBuilder();
            Color leaf = c.leafColor, prod = c.produceColor;
            if (state == 1) leaf = Color.Lerp(leaf, MaterialLibrary.Hex("#c8a85a"), .5f);
            if (state == 2) { leaf = Dead; prod = Dead; stage = Mathf.Min(stage, 1); }
            float s = stage == 0 ? .35f : stage == 1 ? .6f : stage == 2 ? .85f : 1f;
            bool ripe = stage == 3 && state != 2;
            var oct = LowPolyShapes.Octa; var ico = LowPolyShapes.Ico0; var ico1 = LowPolyShapes.Ico1; var box = LowPolyShapes.Box;
            int seed = c.id.GetHashCode();
            float Jit(int i) => ((seed >> (i % 16)) & 7) / 7f - .5f;

            switch (c.kind)
            {
                case CropKind.Leafy:
                    for (int i = 0; i < 6; i++)
                    {
                        float a = i * 60 + Jit(i) * 20;
                        P(b, oct, leaf, Quaternion.Euler(0, a, 0) * new Vector3(0, .05f, .08f) * s, new Vector3(.08f, .03f, .16f) * s, new Vector3(-25, a, 0));
                    }
                    P(b, ico, ripe ? prod : leaf, new Vector3(0, .08f * s, 0), new Vector3(.09f, .1f, .09f) * s);
                    break;

                case CropKind.Root:
                case CropKind.Bulb:
                    for (int i = 0; i < 5; i++)
                    {
                        float a = i * 72;
                        var dir = Quaternion.Euler(c.kind == CropKind.Bulb ? 8 : 22, a, 0);
                        P(b, oct, leaf, dir * new Vector3(0, .14f, 0) * s, new Vector3(.03f, .16f, .05f) * s, dir.eulerAngles);
                    }
                    if (ripe || stage == 2) P(b, ico, prod, new Vector3(0, .03f, 0), new Vector3(.08f, .06f, .08f) * (ripe ? 1.2f : .8f));
                    break;

                case CropKind.Bush:
                    P(b, box, Stake, new Vector3(0, .15f * s, 0), new Vector3(.03f, .3f * s, .03f));
                    P(b, ico, leaf, new Vector3(0, .22f * s, 0), new Vector3(.17f, .14f, .17f) * s);
                    P(b, ico, leaf, new Vector3(.06f, .33f * s, -.03f), new Vector3(.12f, .1f, .12f) * s);
                    if (ripe) for (int i = 0; i < 5; i++)
                        {
                            var p = Quaternion.Euler(0, i * 72 + 20, 0) * new Vector3(0, 0, .15f);
                            P(b, ico, prod, p + new Vector3(0, .2f + (i % 2) * .08f, 0), Vector3.one * .045f);
                        }
                    break;

                case CropKind.Vine:
                    P(b, box, Stake, new Vector3(0, .45f, 0), new Vector3(.035f, .9f, .035f));
                    P(b, box, Stake, new Vector3(0, .84f, 0), new Vector3(.3f, .03f, .03f));
                    int nl = 2 + stage * 2;
                    for (int i = 0; i < nl; i++)
                    {
                        float y = .1f + i * .8f / 7f, a = i * 137;
                        P(b, ico, leaf, Quaternion.Euler(0, a, 0) * new Vector3(0, 0, .06f) + new Vector3(0, y, 0), new Vector3(.09f, .07f, .09f));
                    }
                    if (ripe) for (int i = 0; i < 3; i++)
                            P(b, ico, prod, Quaternion.Euler(0, i * 120, 0) * new Vector3(0, 0, .09f) + new Vector3(0, .35f + i * .14f, 0), new Vector3(.035f, .09f, .035f));
                    break;

                case CropKind.Ground:
                    for (int i = 0; i < 4; i++)
                    {
                        float a = i * 90 + 45;
                        P(b, ico, leaf, Quaternion.Euler(0, a, 0) * new Vector3(0, .06f, .16f) * s, new Vector3(.12f, .05f, .12f) * s);
                    }
                    if (stage >= 2)
                    {
                        bool melon = c.id == "melon";
                        float r = ripe ? .22f : .12f;
                        P(b, ico1, prod, new Vector3(.02f, r * .7f, -.02f), melon ? new Vector3(r, r * .9f, r * 1.25f) : new Vector3(r, r * .72f, r));
                    }
                    break;

                case CropKind.Grain:
                    for (int i = 0; i < 9; i++)
                    {
                        var dir = Quaternion.Euler(10 + Jit(i) * 14, i * 40, 0);
                        float h = .45f * s;
                        P(b, box, leaf, dir * new Vector3(0, h * .5f, 0) + new Vector3((i % 3 - 1) * .1f, 0, (i / 3 - 1) * .1f), new Vector3(.02f, h, .02f), dir.eulerAngles);
                        if (ripe) P(b, oct, prod, dir * new Vector3(0, h, 0) + new Vector3((i % 3 - 1) * .1f, 0, (i / 3 - 1) * .1f), new Vector3(.03f, .06f, .03f), dir.eulerAngles);
                    }
                    break;

                case CropKind.Corn:
                    P(b, box, leaf, new Vector3(0, .4f * s, 0), new Vector3(.04f, .8f * s, .04f));
                    for (int i = 0; i < 4; i++)
                        P(b, oct, leaf, Quaternion.Euler(0, i * 90, 0) * new Vector3(0, 0, .09f) + new Vector3(0, (.2f + i * .15f) * s, 0), new Vector3(.03f, .02f, .14f) * s, new Vector3(-20, i * 90, 0));
                    if (ripe) P(b, ico, prod, new Vector3(.05f, .5f, 0), new Vector3(.045f, .11f, .045f));
                    break;

                case CropKind.Flower:
                    P(b, box, leaf, new Vector3(0, .15f * s, 0), new Vector3(.025f, .3f * s, .025f));
                    for (int i = 0; i < 4; i++)
                        P(b, oct, leaf, Quaternion.Euler(0, i * 90, 0) * new Vector3(0, 0, .07f) + new Vector3(0, .06f * s, 0), new Vector3(.04f, .02f, .09f) * s, new Vector3(-20, i * 90, 0));
                    if (stage >= 2)
                        for (int i = 0; i < 3; i++)
                            P(b, ico1, ripe ? prod : leaf, Quaternion.Euler(0, i * 120, 0) * new Vector3(0, 0, .07f) + new Vector3(0, .3f * s + (i == 0 ? .05f : 0), 0), Vector3.one * (ripe ? .07f : .035f));
                    break;

                case CropKind.Aquatic:
                    for (int i = 0; i < 3; i++)
                        P(b, LowPolyShapes.Cylinder(1, 1, 1, 8), leaf, Quaternion.Euler(0, i * 120, 0) * new Vector3(0, 0, .13f) + new Vector3(0, .02f, 0), new Vector3(.14f, .01f, .14f) * Mathf.Max(.6f, s));
                    if (ripe) P(b, ico1, prod, new Vector3(0, .12f, 0), new Vector3(.08f, .07f, .08f));
                    break;

                case CropKind.Tree:
                    {
                        float ts = stage == 0 ? .3f : stage == 1 ? .55f : 1f;
                        P(b, LowPolyShapes.Cylinder(1, 1, 1, 6), state == 2 ? Dead : Trunk, new Vector3(0, .55f * ts, 0), new Vector3(.07f, 1.1f * ts, .07f));
                        Vector3 crown = new Vector3(0, 1.2f * ts, 0);
                        P(b, ico1, leaf, crown, new Vector3(.5f, .42f, .5f) * ts);
                        P(b, ico1, leaf, crown + new Vector3(.22f, -.12f, .1f) * ts, new Vector3(.32f, .28f, .32f) * ts);
                        P(b, ico1, leaf, crown + new Vector3(-.2f, -.1f, -.15f) * ts, new Vector3(.3f, .26f, .3f) * ts);
                        if (ripe) for (int i = 0; i < 7; i++)
                            {
                                var p = Quaternion.Euler(0, i * 51, 0) * new Vector3(0, 0, .42f);
                                P(b, ico, prod, crown + p + new Vector3(0, -.15f + (i % 3) * .1f, 0), Vector3.one * .07f);
                            }
                    }
                    break;
            }
            return b.ToMesh("crop");
        }

        static Mesh _water, _dike;

        /// <summary>Lop nuoc mong phu kin o (muong co nuoc / ruong ngap nuoc).</summary>
        public static Mesh WaterSheet
        {
            get
            {
                if (_water) return _water;
                var b = new MeshBuilder();
                P(b, LowPolyShapes.Box, Color.white, Vector3.zero, new Vector3(.94f, .02f, .94f));
                _water = b.ToMesh("WaterSheet");
                return _water;
            }
        }

        /// <summary>Bo bao: 4 go dat thap quanh o.</summary>
        public static Mesh DikeRim
        {
            get
            {
                if (_dike) return _dike;
                var b = new MeshBuilder();
                for (int s = 0; s < 4; s++)
                {
                    var rot = Quaternion.Euler(0, s * 90, 0);
                    b.Add(LowPolyShapes.Box, Matrix4x4.TRS(rot * new Vector3(0, .06f, .45f), rot, new Vector3(1f, .14f, .1f)), Color.white);
                }
                _dike = b.ToMesh("DikeRim");
                return _dike;
            }
        }

        static Mesh _soil;
        /// <summary>O dat da cuoc: khoi .94 x .22 x .94, mat tren cao hon mat dat .04 (ban web: soilIM).</summary>
        public static Mesh Soil
        {
            get
            {
                if (_soil) return _soil;
                var b = new MeshBuilder();
                P(b, LowPolyShapes.Box, Color.white, new Vector3(0, -.07f, 0), new Vector3(.94f, .22f, .94f));
                for (int i = 0; i < 3; i++) P(b, LowPolyShapes.Box, MaterialLibrary.Hex("#d8d0c4"), new Vector3(0, .045f, (i - 1) * .28f), new Vector3(.86f, .015f, .07f));
                _soil = b.ToMesh("Soil");
                return _soil;
            }
        }
    }
}
