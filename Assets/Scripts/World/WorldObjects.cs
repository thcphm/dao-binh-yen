// Cay, da, bui... tren dao (ban web: *Geo dong 452-499, OT dong 500-517, OBJH dong 1779, genObjects dong 692-701, lodGeo dong 525-527).
// Thu tu enum giu dung thu tu OT cua ban web.
using System.Collections.Generic;
using UnityEngine;

namespace SereneIsle
{
    public enum WorldObjectType
    {
        Gum, Palm, Spin, Rock, RedRock, Ore, Bush, Pine, JungleTree, Fern, Reeds, Acacia, Baobab, ScrewPine, Mangrove, Mound,
    }

    public struct WorldObjectInfo
    {
        public string name; public int hp; public string tool; public string fx;
        public float jump;   // > 0: nhay qua duoc (OBJH)
        public bool big;     // cay lon: co LOD o xa (BIGT)
        public bool sway;    // lay theo gio (vegMat: dung cu 'axe')
    }

    public static class WorldObjects
    {
        public static readonly int Count = 16;

        static readonly WorldObjectInfo[] _info =
        {
            new WorldObjectInfo { name = "Cây bàng", hp = 4, tool = "axe", fx = "#7fa24a", big = true, sway = true },
            new WorldObjectInfo { name = "Cây dừa", hp = 3, tool = "axe", fx = "#5c9a3a", big = true, sway = true },
            new WorldObjectInfo { name = "Bụi cỏ gai", hp = 1, tool = "axe", fx = "#c4b453", jump = .55f, sway = true },
            new WorldObjectInfo { name = "Tảng đá", hp = 3, tool = "pick", fx = "#a29f97", jump = .75f },
            new WorldObjectInfo { name = "Đá đỏ", hp = 4, tool = "pick", fx = "#b85c34", jump = .95f },
            new WorldObjectInfo { name = "Mỏ quặng đồng", hp = 5, tool = "pick", fx = "#e88a35", jump = .85f },
            new WorldObjectInfo { name = "Bụi quả mọng", hp = 2, tool = "axe", fx = "#5d8c3c", jump = .75f, sway = true },
            new WorldObjectInfo { name = "Cây thông", hp = 4, tool = "axe", fx = "#33633a", big = true, sway = true },
            new WorldObjectInfo { name = "Cây rừng mưa", hp = 5, tool = "axe", fx = "#3c7d32", big = true, sway = true },
            new WorldObjectInfo { name = "Dương xỉ", hp = 1, tool = "axe", fx = "#4f9a3a", jump = .65f, sway = true },
            new WorldObjectInfo { name = "Bụi sậy", hp = 1, tool = "axe", fx = "#8a9a4a", jump = .9f, sway = true },
            new WorldObjectInfo { name = "Cây keo", hp = 3, tool = "axe", fx = "#7f9440", big = true, sway = true },
            new WorldObjectInfo { name = "Cây bao báp", hp = 5, tool = "axe", fx = "#7f9a46", big = true, sway = true },
            new WorldObjectInfo { name = "Cây dứa dại", hp = 2, tool = "axe", fx = "#8aa848", big = true, sway = true },
            new WorldObjectInfo { name = "Cây đước", hp = 4, tool = "axe", fx = "#3f6e35", big = true, sway = true },
            new WorldObjectInfo { name = "Gò đá ong", hp = 3, tool = "pick", fx = "#b5653a", jump = 1.1f },
        };

        public static WorldObjectInfo Info(WorldObjectType t) => _info[(int)t];

        /// <summary>Do roi khi chat do / dap vo (ban web: OT[type].drops, dong 501-516). Chua co giay phep mo -> bo phan thuong mo.</summary>
        public static List<(string id, int n)> Drops(WorldObjectType t, System.Random r)
        {
            int ri(int a, int b) => r.Next(a, b + 1);
            var d = new List<(string, int)>();
            switch (t)
            {
                case WorldObjectType.Gum: case WorldObjectType.Pine: d.Add(("wood", ri(3, 5))); break;
                case WorldObjectType.Palm: d.Add(("wood", ri(2, 3))); break;
                case WorldObjectType.Spin: case WorldObjectType.Reeds: d.Add(("fiber", ri(1, 3))); break;
                case WorldObjectType.Fern: d.Add(("fiber", ri(1, 2))); break;
                case WorldObjectType.Rock: d.Add(("stone", ri(2, 4))); break;
                case WorldObjectType.RedRock: d.Add(("stone", ri(3, 5))); if (r.NextDouble() < .25) d.Add(("ore", 1)); break;
                case WorldObjectType.Ore: d.Add(("ore", ri(1, 3))); d.Add(("stone", 1)); break;
                case WorldObjectType.Bush: d.Add(("wood", 1)); break;
                case WorldObjectType.JungleTree: case WorldObjectType.Baobab: d.Add(("wood", ri(4, 6))); break;
                case WorldObjectType.Acacia: d.Add(("wood", ri(2, 4))); break;
                case WorldObjectType.ScrewPine: d.Add(("fiber", ri(2, 3))); d.Add(("wood", 1)); break;
                case WorldObjectType.Mangrove: d.Add(("wood", ri(3, 4))); break;
                case WorldObjectType.Mound: d.Add(("stone", ri(2, 3))); if (r.NextDouble() < .2) d.Add(("ore", 1)); break;
            }
            return d;
        }

        /// <summary>
        /// So ngay de vat the moc lai (thiet ke gameplay muc 5: cay 3-7 ngay; mo da tu lap lai moi thang = 28 ngay).
        /// </summary>
        public static int RegrowDays(WorldObjectType t, System.Random r) => Info(t).tool == "pick" ? 28 : r.Next(3, 8);
        public static float JumpHeight(WorldObjectType t) => _info[(int)t].jump;

        // ================================================================== hinh hoc
        static Mesh IC => LowPolyShapes.Ico0;
        static Mesh IC1 => LowPolyShapes.Ico1;
        static Mesh DO => LowPolyShapes.Dodeca;
        static Mesh OC => LowPolyShapes.Octa;
        static Mesh SP => LowPolyShapes.Sphere86;
        static Mesh BX => LowPolyShapes.Box;
        static Mesh Cyl(float rt, float rb, float h, int n) => LowPolyShapes.Cylinder(rt, rb, h, n);
        static Mesh Cone(float r, float h, int n) => LowPolyShapes.Cone(r, h, n);

        public static Mesh BuildMesh(WorldObjectType t)
        {
            var b = new MeshBuilder();
            switch (t)
            {
                case WorldObjectType.Gum: Gum(b); break;
                case WorldObjectType.Palm: Palm(b); break;
                case WorldObjectType.Spin:
                    b.P(IC, "#9e9a44", 0, .12f, 0, .5f, .24f, .5f);
                    for (int k = 0; k < 11; k++)
                    {
                        float a = k * 2.39f, r = .12f + (k % 3) * .1f;
                        b.P(Cone(.06f, .7f, 4), k % 2 == 1 ? "#c4b453" : "#a9a648", Mathf.Cos(a) * r, .38f, Mathf.Sin(a) * r, 1, 1, 1, Mathf.Sin(a) * .5f, 0, -Mathf.Cos(a) * .5f);
                    }
                    break;
                case WorldObjectType.Rock: Rock(b, "#a29f97", "#8b8a83", 1); break;
                case WorldObjectType.RedRock: Rock(b, "#b85c34", "#a24e2c", 1.25f); break;
                case WorldObjectType.Ore:
                    b.P(DO, "#8e8a86", 0, .26f, 0, .55f, .48f, .5f, 0, .4f, 0);
                    b.P(DO, "#7d7975", -.3f, .12f, .25f, .25f, .2f, .24f);
                    foreach (var (x, y, z, c) in new[] { (.3f, .4f, .1f, "#e88a35"), (-.2f, .5f, .2f, "#4cb7a5"), (.05f, .62f, -.2f, "#e88a35"), (-.35f, .3f, -.15f, "#e88a35"), (.25f, .2f, .35f, "#4cb7a5") })
                        b.P(OC, c, x, y, z, .11f, .13f, .11f, .3f, .5f, 0);
                    break;
                case WorldObjectType.Bush:
                    b.P(IC, "#4f7d34", 0, .42f, 0, .58f, .46f, .58f);
                    b.P(IC, "#5d8c3c", .3f, .36f, .18f, .4f, .34f, .4f);
                    b.P(IC, "#476f2f", -.28f, .32f, -.12f, .38f, .3f, .38f);
                    // qua mong (berryGeo) - giai doan 3 se tach rieng de hai
                    for (int k = 0; k < 10; k++)
                    {
                        float a = k * 2.4f, y = .3f + (k % 3) * .16f, r = .5f - (y - .3f) * .5f;
                        b.P(SP, "#d32c3e", Mathf.Cos(a) * r, y, Mathf.Sin(a) * r, .07f, .07f, .07f);
                    }
                    break;
                case WorldObjectType.Pine:
                    b.P(Cyl(.11f, .16f, 1.3f, 6), "#6b4a30", 0, .65f, 0);
                    b.P(Cone(.95f, 1.25f, 7), "#2f5a35", 0, 1.35f, 0);
                    b.P(Cone(.72f, 1.05f, 7), "#33633a", 0, 2.0f, 0);
                    b.P(Cone(.46f, .85f, 7), "#3a6e40", 0, 2.6f, 0);
                    break;
                case WorldObjectType.JungleTree:
                    b.P(Cyl(.15f, .26f, 3.3f, 7), "#7a6a50", 0, 1.65f, 0);
                    for (int k = 0; k < 4; k++) { float a = k * 1.57f + .3f; b.P(BX, "#6f5e46", Mathf.Cos(a) * .22f, .3f, Mathf.Sin(a) * .22f, .08f, .6f, .42f, 0, -a, 0); }
                    b.P(IC, "#2f6b2a", 0, 3.5f, 0, 1.5f, .8f, 1.5f);
                    b.P(IC, "#3c7d32", .85f, 3.2f, .3f, 1.05f, .6f, 1.05f);
                    b.P(IC, "#2a5e26", -.75f, 3.25f, -.4f, .95f, .55f, .95f);
                    b.P(IC, "#357530", .1f, 3.95f, -.2f, .9f, .5f, .9f);
                    for (int k = 0; k < 5; k++) { float a = k * 1.3f; b.P(BX, "#4f8a3a", Mathf.Cos(a) * 1.1f, 2.6f, Mathf.Sin(a) * 1.1f, .03f, 1.2f, .03f); }
                    break;
                case WorldObjectType.Fern:
                    for (int k = 0; k < 8; k++) { float a = k / 8f * 6.28f; b.P(BX, k % 2 == 1 ? "#4f9a3a" : "#3f8a32", Mathf.Cos(a) * .38f, .28f, -Mathf.Sin(a) * .38f, .85f, .02f, .17f, 0, a, .55f); }
                    break;
                case WorldObjectType.Reeds:
                    for (int k = 0; k < 10; k++)
                    {
                        float a = k * 2.4f, r = .05f + (k % 3) * .09f, h = .8f + (k % 4) * .15f, x = Mathf.Cos(a) * r, z = Mathf.Sin(a) * r;
                        b.P(BX, k % 2 == 1 ? "#8a9a4a" : "#7a8f40", x, h / 2, z, .03f, h, .03f, Mathf.Sin(a) * .1f, 0, Mathf.Cos(a) * .1f);
                        if (k % 3 == 0) b.P(BX, "#6a4a2a", x, h - .05f, z, .05f, .16f, .05f);
                    }
                    break;
                case WorldObjectType.Acacia:
                    b.P(Cyl(.07f, .12f, 1.7f, 6), "#6b5038", 0, .85f, 0, 1, 1, 1, 0, 0, .1f);
                    b.P(Cyl(.05f, .07f, .9f, 5), "#6b5038", .35f, 1.85f, 0, 1, 1, 1, 0, 0, -.6f);
                    b.P(Cyl(.05f, .07f, .9f, 5), "#6b5038", -.25f, 1.85f, .1f, 1, 1, 1, .2f, 0, .6f);
                    b.P(IC, "#7f9440", .1f, 2.35f, 0, 1.6f, .28f, 1.35f);
                    b.P(IC, "#8ea24a", .6f, 2.25f, .3f, 1, .22f, .9f);
                    b.P(IC, "#76893a", -.6f, 2.28f, -.2f, .9f, .2f, .8f);
                    break;
                case WorldObjectType.Baobab:
                    b.P(LowPolyShapes.Lathe(new[] { new Vector2(.2f, 0), new Vector2(.34f, .25f), new Vector2(.44f, .7f), new Vector2(.38f, 1.1f), new Vector2(.22f, 1.55f), new Vector2(.16f, 1.75f) }, 9), "#b49a7a", 0, 0, 0);
                    b.P(Cyl(.04f, .07f, .7f, 5), "#8a7258", .2f, 1.95f, 0, 1, 1, 1, 0, 0, -.7f);
                    b.P(Cyl(.04f, .07f, .7f, 5), "#8a7258", -.2f, 1.95f, .05f, 1, 1, 1, 0, 0, .7f);
                    b.P(Cyl(.04f, .06f, .6f, 5), "#8a7258", 0, 2.0f, -.2f, 1, 1, 1, .7f, 0, 0);
                    b.P(IC, "#7f9a46", .45f, 2.25f, 0, .45f, .22f, .4f);
                    b.P(IC, "#8aa64e", -.45f, 2.25f, .1f, .42f, .2f, .38f);
                    b.P(IC, "#76913f", 0, 2.3f, -.4f, .4f, .2f, .36f);
                    b.P(IC, "#86a24a", 0, 2.42f, .1f, .5f, .24f, .45f);
                    break;
                case WorldObjectType.ScrewPine:
                    b.P(Cyl(.07f, .1f, 1.6f, 6), "#8a7a5a", 0, 1.0f, 0, 1, 1, 1, 0, 0, .08f);
                    for (int k = 0; k < 5; k++) { float a = k * 1.26f; b.P(Cyl(.025f, .035f, .9f, 4), "#7a6a4a", Mathf.Cos(a) * .22f, .38f, Mathf.Sin(a) * .22f, 1, 1, 1, Mathf.Sin(a) * .45f, 0, -Mathf.Cos(a) * .45f); }
                    for (int k = 0; k < 16; k++)
                    {
                        float a = k * .785f + (k > 7 ? .4f : 0), tt = k > 7 ? .6f : 1.1f;
                        b.P(Cone(.06f, 1.0f, 3), k % 2 == 1 ? "#5f8f3a" : "#6fa044", Mathf.Cos(a) * .3f + .08f, 1.95f, Mathf.Sin(a) * .3f, 1, 1, .35f, Mathf.Sin(a) * tt, 0, -Mathf.Cos(a) * tt);
                    }
                    b.P(SP, "#d8a43a", .15f, 1.75f, .1f, .12f, .14f, .12f);
                    break;
                case WorldObjectType.Mangrove:
                    b.P(Cyl(.1f, .13f, 1.4f, 7), "#5a4630", 0, 1.15f, 0);
                    for (int k = 0; k < 6; k++) { float a = k * 1.05f; b.P(LowPolyShapes.Torus(.32f, .035f, 4, 8, Mathf.PI), "#5a4630", Mathf.Cos(a) * .3f, .05f, Mathf.Sin(a) * .3f, 1, 1.4f, 1, 0, -a + Mathf.PI / 2, 0); }
                    b.P(IC, "#3f6e35", 0, 2.0f, 0, 1.0f, .42f, .95f);
                    b.P(IC, "#4a7a3c", .45f, 1.85f, .2f, .6f, .3f, .55f);
                    b.P(IC, "#376230", -.4f, 1.9f, -.3f, .6f, .3f, .55f);
                    break;
                case WorldObjectType.Mound:
                    b.P(Cone(.36f, 1.15f, 7), "#b5653a", 0, .57f, 0);
                    b.P(Cone(.18f, .6f, 6), "#a85a32", .22f, .3f, .1f);
                    b.P(Cone(.14f, .45f, 6), "#c0703f", -.2f, .22f, -.12f);
                    b.P(DO, "#a85a32", 0, .08f, 0, .42f, .12f, .42f);
                    break;
            }
            return b.ToMesh("Obj_" + t);
        }

        static void Rock(MeshBuilder b, string c1, string c2, float s)
        {
            b.P(DO, c1, 0, .2f * s, 0, .5f * s, .4f * s, .46f * s, 0, .4f, 0);
            b.P(DO, c2, .34f * s, .1f * s, .22f * s, .24f * s, .2f * s, .22f * s, .3f, 0, 0);
        }

        static void Gum(MeshBuilder b)
        {
            b.P(Cyl(.13f, .2f, 1.5f, 7), "#6b4a32", 0, .75f, 0);
            b.P(Cyl(.09f, .13f, 1.3f, 7), "#5e4029", .08f, 2.05f, 0, 1, 1, 1, 0, 0, -.12f);
            foreach (var (y, a) in new[] { (.5f, .3f), (1.0f, 2.2f), (1.6f, 4.1f), (2.2f, 1.2f) })
                b.P(BX, "#c2b8a6", Mathf.Cos(a) * .14f, y, Mathf.Sin(a) * .14f, .08f, .22f, .05f, 0, -a, 0);
            b.P(Cyl(.04f, .08f, 1.1f, 5), "#ddd4c2", .42f, 2.35f, .05f, 1, 1, 1, 0, 0, -.85f);
            b.P(Cyl(.04f, .07f, 1, 5), "#ddd4c2", -.34f, 2.3f, .15f, 1, 1, 1, .3f, 0, .8f);
            b.P(Cyl(.03f, .06f, .8f, 5), "#ddd4c2", 0, 2.5f, -.4f, 1, 1, 1, -.7f, 0, 0);
            var r = new Mulberry32(41);
            string[] C = { "#5f8738", "#6f9a40", "#7fa94a", "#68903c", "#557d32", "#86ae54" };
            for (int k = 0; k < 13; k++)
            {
                float a = (float)r.Next() * 6.28f, rr = Mathf.Sqrt((float)r.Next()) * .95f, y = 2.75f + ((float)r.Next() - .35f) * .75f, s = .42f + (float)r.Next() * .32f;
                float ry = (float)r.Next() * 6;
                b.P(IC1, C[k % 6], Mathf.Cos(a) * rr * 1.1f, y + (1 - rr) * .25f, Mathf.Sin(a) * rr * 1.1f, s * 1.15f, s * .8f, s * 1.15f, 0, ry, 0);
            }
        }

        static void Palm(MeshBuilder b)
        {
            float x = 0, y = 0;
            for (int k = 0; k < 7; k++)
            {
                float h = .48f, lean = .05f * k;
                b.P(Cyl(.095f - k * .004f, .12f - k * .004f, h, 7), k % 2 == 1 ? "#9a7449" : "#ad8658", x, y + h / 2, 0, 1, 1, 1, 0, 0, -lean);
                b.P(Cyl(.125f - k * .004f, .125f - k * .004f, .05f, 7), "#7a5a38", x, y + .02f, 0, 1, 1, 1, 0, 0, -lean);
                x += Mathf.Sin(lean) * h; y += Mathf.Cos(lean) * h;
            }
            var top = new Vector3(x, y, 0);
            var r = new Mulberry32(9);
            string[] LC = { "#4f9a3a", "#5aa846", "#3f8a30" };
            for (int f = 0; f < 9; f++)
            {
                float yaw = f / 9f * Mathf.PI * 2 + (float)r.Next() * .3f;
                var hz = new Vector3(Mathf.Sin(yaw), 0, Mathf.Cos(yaw));
                var side = new Vector3(Mathf.Cos(yaw), 0, -Mathf.Sin(yaw));
                var pos = top;
                float pitch = .55f + (float)r.Next() * .2f;
                for (int sg = 0; sg < 6; sg++)
                {
                    const float L = .42f;
                    var dir = (hz * Mathf.Cos(pitch) + new Vector3(0, Mathf.Sin(pitch), 0)).normalized;
                    b.PM(BX, "#5a7a30", pos, dir, .035f, .025f, L);
                    var mid = pos + dir * (L * .5f);
                    float ll = .62f * (1 - sg / 7f);
                    foreach (int sd in new[] { -1, 1 })
                    {
                        var ld = (side * sd + new Vector3(0, -.5f, 0) + dir * .45f).normalized;
                        b.PM(BX, LC[(sg + f) % 3], mid, ld, .17f, .02f, ll * 1.2f);
                    }
                    pos += dir * L;
                    pitch -= .33f;
                }
            }
            for (int k = 0; k < 4; k++) { float a = k * 1.6f; b.P(SP, k % 2 == 1 ? "#6b5a2a" : "#7a8a3a", x + Mathf.Cos(a) * .14f, y - .15f, Mathf.Sin(a) * .14f, .11f, .12f, .11f); }
        }

        /// <summary>lodGeo: than tru + tan da giac theo kich thuoc that cua cay (cho cay o xa).</summary>
        public static Mesh BuildLod(WorldObjectType t, Mesh full, Color? leafColor = null)
        {
            var bb = full.bounds;
            float hy = bb.max.y, w = Mathf.Max(bb.size.x, bb.size.z) / 2, cx = bb.center.x, cz = bb.center.z;
            string tc = t == WorldObjectType.Gum ? "#5e4029" : t == WorldObjectType.Baobab ? "#b49a7a" : t == WorldObjectType.ScrewPine ? "#7a6a4a" : "#6b5038";
            var cc = leafColor ?? MaterialLibrary.Hex(Info(t).fx);
            var b = new MeshBuilder();
            b.P(Cyl(.09f, .14f, hy * .75f, 5), tc, cx * .3f, hy * .37f, cz * .3f);
            b.Add(IC, Matrix4x4.TRS(new Vector3(cx, hy * .8f, cz), Quaternion.identity, new Vector3(w * .85f, hy * .24f, w * .85f)), cc);
            b.Add(IC, Matrix4x4.TRS(new Vector3(cx + w * .25f, hy * .7f, cz - w * .2f), Quaternion.identity, new Vector3(w * .5f, hy * .16f, w * .5f)), cc * .85f);
            return b.ToMesh("Lod_" + t);
        }

        // ================================================================== dat vat the (genObjects)
        public struct Placement { public int idx; public WorldObjectType type; public float rot, scale; }

        static readonly (WorldObjectType t, double p)[] BANK = { (WorldObjectType.Reeds, .14), (WorldObjectType.Bush, .03), (WorldObjectType.Rock, .01) };
        static readonly (WorldObjectType t, double p)[] DRYBANK = { (WorldObjectType.Spin, .05), (WorldObjectType.Rock, .02) };
        static readonly Dictionary<Biome, (WorldObjectType t, double p)[]> T = new Dictionary<Biome, (WorldObjectType, double)[]>
        {
            [Biome.Beach] = new[] { (WorldObjectType.Palm, .05), (WorldObjectType.ScrewPine, .02), (WorldObjectType.Rock, .015) },
            [Biome.Bush] = new[] { (WorldObjectType.Gum, .07), (WorldObjectType.Bush, .03), (WorldObjectType.Rock, .03), (WorldObjectType.Ore, .006) },
            [Biome.Forest] = new[] { (WorldObjectType.Gum, .26), (WorldObjectType.Bush, .05), (WorldObjectType.Fern, .04), (WorldObjectType.Rock, .02) },
            [Biome.Desert] = new[] { (WorldObjectType.Spin, .08), (WorldObjectType.RedRock, .05), (WorldObjectType.Ore, .025), (WorldObjectType.Mound, .012) },
            [Biome.Tropics] = new[] { (WorldObjectType.JungleTree, .14), (WorldObjectType.Fern, .18), (WorldObjectType.Palm, .05), (WorldObjectType.Bush, .06) },
            [Biome.Mangrove] = new[] { (WorldObjectType.Mangrove, .07), (WorldObjectType.Reeds, .2), (WorldObjectType.JungleTree, .02), (WorldObjectType.Bush, .02) },
            [Biome.Alpine] = new[] { (WorldObjectType.Pine, .18), (WorldObjectType.Rock, .05), (WorldObjectType.Ore, .025), (WorldObjectType.Bush, .02) },
            [Biome.Savanna] = new[] { (WorldObjectType.Acacia, .035), (WorldObjectType.Baobab, .008), (WorldObjectType.Spin, .05), (WorldObjectType.Mound, .02), (WorldObjectType.Rock, .01) },
            [Biome.Plains] = new[] { (WorldObjectType.Bush, .014), (WorldObjectType.Gum, .006), (WorldObjectType.Acacia, .004), (WorldObjectType.Spin, .01), (WorldObjectType.Rock, .006) },
        };

        /// <summary>Dat vat the len dao, danh dau o bi chan. Cung SEED -> cung vi tri nhu ban web.</summary>
        public static List<Placement> Place(IslandData d)
        {
            int N = d.N;
            d.ObjType = new byte[N * N];
            d.ObjScale = new float[N * N];
            d.ObjRot = new float[N * N];
            var list = new List<Placement>();
            var rand = new Mulberry32(unchecked(d.Seed * 7 + 3));
            var tb = new List<(WorldObjectType t, double p)>(8);
            for (int k = 0; k < N * N; k++)
            {
                var b = d.Bio[k];
                if (!T.TryGetValue(b, out var baseTable)) continue;
                int i = k % N, j = k / N;
                if (IslandNoise.Hypot(i - d.CX, j - d.CZ) < 10.5) continue;
                d.ToTownLocal(i, j, out var lx, out var lz);
                if (lz > 0 && lz < 24 && System.Math.Abs(lx - .5) < 3) continue;

                double r = rand.Next();
                // ban web (dong 699): tb = goc (hoac BANK/DRYBANK); roi chen [rock,ore] len dau neu nui cao;
                // roi chen [ore,redrock] len dau neu dat go ghe -> thu tu cuoi: [ore,redrock] [rock,ore] goc
                tb.Clear();
                if (d.Rough[k] != 0 && (b == Biome.Desert || b == Biome.Savanna || b == Biome.Bush)) { tb.Add((WorldObjectType.Ore, .03)); tb.Add((WorldObjectType.RedRock, .03)); }
                if (d.Level[k] == 4 && b != Biome.Alpine) { tb.Add((WorldObjectType.Rock, .05)); tb.Add((WorldObjectType.Ore, .02)); }
                var main = b == Biome.Beach && !(d.Coast[k] != 0 && d.Coast[k] <= 7) ? (d.Moist(i, j) < .4 ? DRYBANK : BANK) : baseTable;
                tb.AddRange(main);
                float ro = (float)(rand.Next() * 6.28), sc = (float)(.85 + rand.Next() * .35);
                foreach (var (t, p) in tb)
                {
                    if (r < p)
                    {
                        if (d.ObjType[k] == 0)
                        {
                            d.ObjType[k] = (byte)(t + 1); d.ObjScale[k] = sc; d.ObjRot[k] = ro; d.Solid[k] = 1;
                            list.Add(new Placement { idx = k, type = t, rot = ro, scale = sc });
                        }
                        break;
                    }
                    r -= p;
                }
            }
            return list;
        }
    }
}
