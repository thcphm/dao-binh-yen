// Nha cua trong lang (ban web: BLD + houseGeo dong 961-1015, placeB / freeSpot / addBuilding dong 1027-1035).
// Toa do thi tran: x = lx, z = lz huong ra bien (cau tau o lx 0..1, lz 8..21). Moi nha xoay theo huong bien,
// cua nha nhin ra bien (+z). Nha tren dat tim cho trong gan nhat (freeSpot); nha san ong Bay co dinh canh cau tau.
using System.Collections.Generic;
using UnityEngine;

namespace SereneIsle
{
    public sealed class VillageBuilding
    {
        public string id;          // shack, lighthouse, taphoa, sauvuon, chinhut
        public string name;
        public GameObject go;
        public Vector3 center;     // tam nen (the gioi)
        public Vector3 door;       // diem dung truoc cua
        public float facing;       // huong nhin ra cua (rad, quanh Y, 0 = +z)
        public readonly List<int> tiles = new List<int>();
    }

    public static class VillageBuilder
    {
        struct Spec { public string id, name; public int ox, oz, w, d; }

        // vi tri mong muon (toa do thi tran) - lech khoi leu (-7,1), ban che tao (3,3), lua trai (0,3)
        static readonly Spec[] Land =
        {
            new Spec { id = "taphoa", name = "Tiệm tạp hóa Thím Tám", ox = 4, oz = -5, w = 2, d = 2 },
            new Spec { id = "cho", name = "Nhà lồng chợ phiên", ox = -2, oz = -4, w = 3, d = 2 },
            new Spec { id = "sauvuon", name = "Nhà Cô Sáu Vườn", ox = -8, oz = -6, w = 2, d = 2 },
            new Spec { id = "chinhut", name = "Chòi Bác Chín Đò", ox = -4, oz = 7, w = 2, d = 2 },
            new Spec { id = "lighthouse", name = "Hải đăng", ox = 4, oz = 8, w = 1, d = 1 },
        };

        public static List<VillageBuilding> Build(IslandData d, Transform parent, VegetationRenderer veg)
        {
            var list = new List<VillageBuilding>();
            var root = new GameObject("Village").transform;
            root.SetParent(parent, false);
            var reserved = new HashSet<int> { d.TownIdx(0, 3) }; // lua trai giua lang

            list.Add(BuildShack(d, root, veg));
            foreach (var sp in Land)
            {
                var (ox, oz) = FreeSpot(d, sp, reserved);
                var b = new VillageBuilding { id = sp.id, name = sp.name };
                Vector2 c = d.TownPoint(ox + (sp.w - 1) / 2f, oz + (sp.d - 1) / 2f);
                float ground = float.MinValue;
                for (int a = 0; a < sp.w; a++)
                    for (int q = 0; q < sp.d; q++)
                    {
                        int k = d.TownIdx(ox + a, oz + q);
                        if (k < 0 || k >= d.N * d.N) continue;
                        Occupy(d, k, veg);
                        b.tiles.Add(k);
                        ground = Mathf.Max(ground, d.HT[k]);
                    }
                if (ground == float.MinValue) ground = d.GroundHeight(c.x, c.y);
                b.center = new Vector3(c.x, ground, c.y);
                b.go = new GameObject(sp.id);
                b.go.transform.SetParent(root, false);
                b.go.transform.localPosition = b.center;
                b.go.transform.localRotation = Quaternion.Euler(0, d.TownAngle * Mathf.Rad2Deg, 0);
                b.go.AddComponent<MeshFilter>().sharedMesh = MeshOf(sp.id);
                b.go.AddComponent<MeshRenderer>().sharedMaterial = MaterialLibrary.Objects(false);
                Vector2 front = d.TownPoint(ox + (sp.w - 1) / 2f, oz + (sp.d - 1) / 2f + (sp.w > 1 ? 1.9f : 1.2f));
                b.door = new Vector3(front.x, d.GroundHeight(front.x, front.y), front.y);
                b.facing = d.TownAngle;
                foreach (var k in b.tiles) reserved.Add(k);
                list.Add(b);
            }
            return list;
        }

        static void Occupy(IslandData d, int k, VegetationRenderer veg)
        {
            if (d.ObjType != null && d.ObjType[k] != 0)
            {
                veg?.RemoveObject(k, (WorldObjectType)(d.ObjType[k] - 1));
                d.ObjType[k] = 0;
            }
            d.Solid[k] = 1;
            veg?.HideGrass(k);
        }

        /// <summary>fpFree: tren can (khong cau tau, nuoc ngot, vat can da xay), tren muc nuoc rong.</summary>
        static bool Free(IslandData d, Spec sp, int ox, int oz, HashSet<int> reserved)
        {
            for (int a = 0; a < sp.w; a++)
                for (int q = 0; q < sp.d; q++)
                {
                    int k = d.TownIdx(ox + a, oz + q);
                    if (k < 0 || k >= d.N * d.N || reserved.Contains(k)) return false;
                    if (d.Level[k] == 0 || d.Pier[k] != 0 || d.Fresh[k] != 0) return false;
                    if (d.Solid[k] != 0 && !(d.ObjType != null && d.ObjType[k] != 0)) return false; // cay/da thi don di duoc
                    if (d.HT[k] < TideClock.MaxLevel + .08f) return false;
                }
            return true;
        }

        static (int, int) FreeSpot(IslandData d, Spec sp, HashSet<int> reserved)
        {
            if (Free(d, sp, sp.ox, sp.oz, reserved)) return (sp.ox, sp.oz);
            for (int r = 1; r < 10; r++)
                for (int dz = -r; dz <= r; dz++)
                    for (int dx = -r; dx <= r; dx++)
                    {
                        if (Mathf.Max(Mathf.Abs(dx), Mathf.Abs(dz)) != r) continue;
                        if (Free(d, sp, sp.ox + dx, sp.oz + dz, reserved)) return (sp.ox + dx, sp.oz + dz);
                    }
            return (sp.ox, sp.oz);
        }

        // ------------------------------------------------------------------ nha san ong Bay (ban web dong 968-998)
        static VillageBuilding BuildShack(IslandData d, Transform root, VegetationRenderer veg)
        {
            const int OX = 3, OZ = 11, W = 3, D = 3;
            var b = new VillageBuilding { id = "shack", name = "Nhà sàn của ông Bảy" };
            for (int a = 0; a < W; a++)
                for (int q = 0; q < D; q++)
                {
                    int k = d.TownIdx(OX + a, OZ + q);
                    if (k < 0 || k >= d.N * d.N) continue;
                    Occupy(d, k, veg);
                    b.tiles.Add(k);
                }
            Vector2 c = d.TownPoint(OX + 1, OZ + 1);
            b.center = new Vector3(c.x, IslandData.PIER_H, c.y);
            b.go = new GameObject("shack");
            b.go.transform.SetParent(root, false);
            b.go.transform.localPosition = b.center;
            b.go.transform.localRotation = Quaternion.Euler(0, (d.TownAngle - Mathf.PI / 2) * Mathf.Rad2Deg, 0);
            b.go.AddComponent<MeshFilter>().sharedMesh = MeshOf("shack");
            b.go.AddComponent<MeshRenderer>().sharedMaterial = MaterialLibrary.Objects(false);
            // ong Bay dung tren cau tau truoc nha (ban web: townPt(.5, 10.2))
            Vector2 p = d.TownPoint(.5f, 10.2f);
            b.door = new Vector3(p.x, IslandData.PIER_H, p.y);
            b.facing = d.TownAngle + Mathf.PI; // nhin vao lang
            return b;
        }

        // ------------------------------------------------------------------ mo hinh
        static readonly Dictionary<string, Mesh> _meshes = new Dictionary<string, Mesh>();

        public static Mesh MeshOf(string id)
        {
            if (_meshes.TryGetValue(id, out var m) && m) return m;
            var b = new MeshBuilder();
            switch (id)
            {
                case "shack": Shack(b); break;
                case "lighthouse": Lighthouse(b); break;
                case "taphoa": TapHoa(b); break;
                case "sauvuon": SauVuon(b); break;
                case "chinhut": ChinHut(b); break;
                case "basketboat": BasketBoat(b); break;
                case "cho": Pavilion(b); break;
                default: House(b, "#efe0bd", "#a9b6bb", "#7a4a2a"); break;
            }
            m = b.ToMesh("Village_" + id);
            _meshes[id] = m;
            return m;
        }

        static Mesh Prism => ProceduralMeshes.Prism(3, 1, 1, "TentPrism");

        /// <summary>houseGeo(wall, roof, door): than nha 1.8 x 1.7, mai lang tru, cua, 2 cua so, hien, ong khoi.</summary>
        static void House(MeshBuilder b, string wall, string roof, string door, bool porchRoof = true, bool chimney = true)
        {
            b.Box(wall, 0, .65f, 0, 1.8f, 1.3f, 1.7f);
            // mai: lang tru tam giac nam ngang doc chieu rong nha (truc X), dinh huong len.
            // (Euler XYZ cua ban web lam lang tru nam doc truc Z, dinh chia sang ngang -> doi sang Ry(90) * Rx(-90).)
            b.Add(Prism, Matrix4x4.TRS(new Vector3(0, 1.9f, 0), Quaternion.Euler(0, 90, 0) * Quaternion.Euler(-90, 0, 0), new Vector3(1.2f, 2.1f, 1.2f)), MaterialLibrary.Hex(roof));
            b.Box(door, 0, .5f, .86f, .4f, .8f, .05f);
            b.Box("#9fd3e0", -.55f, .75f, .86f, .32f, .3f, .04f);
            b.Box("#9fd3e0", .55f, .75f, .86f, .32f, .3f, .04f);
            b.Box("#7a5a3a", -.55f, .75f, .87f, .38f, .04f, .03f);
            if (porchRoof) b.Box(roof, 0, 1.32f, 1.18f, 2.0f, .06f, .7f, .18f, 0, 0);
            b.Box("#8a6a4a", -.92f, .65f, 1.48f, .08f, 1.3f, .08f);
            b.Box("#8a6a4a", .92f, .65f, 1.48f, .08f, 1.3f, .08f);
            b.Box("#a08a6a", 0, .04f, 1.2f, 2, .08f, .8f);
            if (chimney) b.Box("#9a4a3a", .55f, 2.05f, -.3f, .24f, .6f, .24f);
        }

        /// <summary>Tiem tap hoa: tuong vang, mai ngoi do, mai hien soc do trang, bang hieu, bao gao, ro trai cay.</summary>
        static void TapHoa(MeshBuilder b)
        {
            House(b, "#f0d39a", "#b5523a", "#3f6f5a", porchRoof: false);
            for (int k = 0; k < 6; k++) b.Box(k % 2 == 1 ? "#f6e7c8" : "#d8452f", -.83f + k * .333f, 1.36f, 1.2f, .34f, .05f, .8f, .2f, 0, 0);
            b.Box("#2f5a8a", 0, 1.52f, .88f, 1.1f, .26f, .05f);              // bang hieu
            for (int k = 0; k < 5; k++) b.Box("#f6f0e0", -.36f + k * .18f, 1.52f, .91f, .1f, .12f, .01f);
            // quay hang truoc hien
            b.Box("#a8703e", .5f, .32f, 1.35f, .7f, .6f, .4f);
            b.Box("#c4925a", .5f, .64f, 1.35f, .78f, .05f, .46f);
            b.P(LowPolyShapes.Cylinder(.12f, .14f, .3f, 8), "#e8dcc0", -.55f, .19f, 1.3f);   // bao gao
            b.P(LowPolyShapes.Cylinder(.12f, .14f, .3f, 8), "#e8dcc0", -.3f, .19f, 1.42f);
            b.P(LowPolyShapes.Cylinder(.2f, .14f, .1f, 8), "#8a6a40", .35f, .72f, 1.35f);    // ro trai cay
            foreach (var (x, z, c) in new[] { (.3f, 1.32f, "#e04a2a"), (.4f, 1.38f, "#f2b02a"), (.33f, 1.42f, "#5a9a3a") })
                b.P(LowPolyShapes.Sphere86, c, x, .8f, z, .07f, .07f, .07f);
            b.Box("#8fd3f0", .7f, .76f, 1.35f, .08f, .2f, .08f);                               // chai nuoc
            b.Box("#8fd3f0", .78f, .76f, 1.3f, .08f, .2f, .08f);
        }

        /// <summary>Nha Co Sau Vuon: vach trang, mai la (vang rom), sap hat giong co mai soc xanh, chau cay.</summary>
        static void SauVuon(MeshBuilder b)
        {
            House(b, "#ece4cc", "#b89a5a", "#6b4a2a", chimney: false);
            // sap hat giong ben phai hien
            const float sx = 1.55f, sz = 1.1f;
            b.Box("#a8703e", sx, .35f, sz, .5f, .7f, .9f);
            b.Box("#c4925a", sx, .72f, sz, .56f, .05f, .96f);
            for (int k = 0; k < 4; k++) b.Box(k % 2 == 1 ? "#f6f0e0" : "#5f9a3a", sx, 1.25f, sz - .36f + k * .24f, .7f, .04f, .24f, 0, 0, .15f);
            foreach (var (x, z) in new[] { (sx - .3f, sz - .4f), (sx + .3f, sz - .4f), (sx - .3f, sz + .4f), (sx + .3f, sz + .4f) })
                b.Box("#7a5232", x, .6f, z, .05f, 1.2f, .05f);
            foreach (var (z, c) in new[] { (sz - .3f, "#d8b27a"), (sz - .05f, "#e0b84a"), (sz + .2f, "#c8a070"), (sz + .4f, "#b0d070") })
                b.Box(c, sx, .8f, z, .16f, .12f, .14f);                                         // tui hat
            // chau cay truoc nha
            foreach (var x in new[] { -.75f, -.45f })
            {
                b.P(LowPolyShapes.Cylinder(.12f, .09f, .18f, 8), "#b5643a", x, .17f, 1.45f);
                b.P(LowPolyShapes.Sphere86, "#4f9a3a", x, .36f, 1.45f, .16f, .14f, .16f);
            }
            b.P(LowPolyShapes.Sphere86, "#e04a6a", -.45f, .48f, 1.45f, .05f, .05f, .05f);
        }

        /// <summary>Choi Bac Chin: vach tre, mai la, mai cheo dua vao vach, luoi phoi.</summary>
        static void ChinHut(MeshBuilder b)
        {
            House(b, "#c8a86a", "#9a7a44", "#5a3a22", chimney: false);
            for (int k = 0; k < 8; k++) b.Box(k % 2 == 1 ? "#b8985a" : "#d0b072", -.79f + k * .225f, .65f, .855f, .03f, 1.3f, .02f); // nep tre
            b.Box("#7a5232", 1.0f, .8f, 1.0f, .06f, 1.6f, .06f, 0, 0, .25f);                       // mai cheo
            b.Box("#8a6038", 1.2f, .08f, 1.0f, .16f, .3f, .04f, 0, 0, .25f);
            for (int k = 0; k < 5; k++) b.Box("#2f4a3a", -1.0f, .4f + k * .16f, -.1f, .02f, .02f, 1.2f); // luoi
            for (int k = 0; k < 6; k++) b.Box("#2f4a3a", -1.0f, .72f, -.6f + k * .2f, .02f, .66f, .02f);
            b.P(LowPolyShapes.Cylinder(.2f, .18f, .4f, 10), "#6b4a2a", -.6f, .22f, 1.45f);       // lu nuoc
        }

        /// <summary>Nha long cho: nen gach, 6 cot go, mai ngoi do, quay truoc (khach dung ngoai) + ban sau, bang hieu.</summary>
        static void Pavilion(MeshBuilder b)
        {
            b.Box("#b8a888", 0, .04f, 0, 3.0f, .08f, 2.0f);
            foreach (var x in new[] { -1.4f, 0, 1.4f })
                foreach (var z in new[] { -.85f, .85f })
                    b.Box("#7a5232", x, 1.1f, z, .12f, 2.2f, .12f);
            b.Box("#6b4a2a", 0, 2.2f, .85f, 3.0f, .1f, .1f);
            b.Box("#6b4a2a", 0, 2.2f, -.85f, 3.0f, .1f, .1f);
            // mai: lang tru doc truc X (scale: x = nua sau, y = dai, z = cao dinh)
            b.Add(Prism, Matrix4x4.TRS(new Vector3(0, 2.6f, 0), Quaternion.Euler(0, 90, 0) * Quaternion.Euler(-90, 0, 0), new Vector3(1.45f, 3.5f, .8f)), MaterialLibrary.Hex("#b5523a"));
            b.Box("#8a3a2a", 0, 3.0f, 0, 3.55f, .08f, .12f);                                 // nok
            // quay truoc + ban sau
            b.Box("#a8703e", 0, .38f, .62f, 2.6f, .06f, .5f);
            b.Box("#8a5a30", 0, .2f, .86f, 2.6f, .36f, .04f);
            b.Box("#a8703e", 0, .38f, -.55f, 2.6f, .06f, .45f);
            foreach (var x in new[] { -1.2f, 1.2f })
            {
                b.Box("#7a5232", x, .19f, .45f, .06f, .38f, .06f);
                b.Box("#7a5232", x, .19f, -.55f, .06f, .38f, .06f);
            }
            // bang hieu "CHO PHIEN" (vach chu gia) + den long do
            b.Box("#c8402a", 0, 1.95f, .93f, 1.2f, .32f, .05f);
            for (int k = 0; k < 6; k++) b.Box("#f6e7c8", -.4f + k * .16f, 1.95f, .96f, .09f, .14f, .01f);
            foreach (var x in new[] { -.95f, .95f })
            {
                b.Box("#3a2a1a", x, 2.05f, .95f, .02f, .2f, .02f);
                b.P(LowPolyShapes.Sphere86, "#e0402a", x, 1.85f, .95f, .12f, .15f, .12f);
            }
        }

        /// <summary>Ghe thung: thung tron dan tre + vanh + mai cheo (noi theo con nuoc).</summary>
        static void BasketBoat(MeshBuilder b)
        {
            b.P(LowPolyShapes.Sphere86, "#6b5434", 0, .12f, 0, .78f, .3f, .78f);
            b.P(LowPolyShapes.Cylinder(.8f, .8f, .06f, 16), "#3a2a1a", 0, .28f, 0);
            b.P(LowPolyShapes.Cylinder(.74f, .74f, .02f, 16), "#8a6a40", 0, .3f, 0);
            b.Box("#7a5232", .2f, .38f, 0, 1.5f, .05f, .07f, 0, .4f, .15f);
            b.Box("#8a6038", .9f, .3f, .38f, .3f, .03f, .14f, 0, .4f, .15f);
        }

        /// <summary>Hai dang: thap tron soc trang do, ban cong, phong den, mai non do.</summary>
        static void Lighthouse(MeshBuilder b)
        {
            b.P(LowPolyShapes.Cylinder(.62f, .7f, .3f, 12), "#8e8a86", 0, .15f, 0);
            for (int k = 0; k < 6; k++)
            {
                float r0 = .56f - k * .045f, r1 = .56f - (k + 1) * .045f;
                b.P(LowPolyShapes.Cylinder(r1, r0, .6f, 12), k % 2 == 0 ? "#f4f1e8" : "#c8402a", 0, .6f + k * .6f, 0);
            }
            b.P(LowPolyShapes.Cylinder(.55f, .55f, .08f, 12), "#3a3a3a", 0, 3.94f, 0);          // ban cong
            for (int k = 0; k < 12; k++)
            {
                float a = k / 12f * Mathf.PI * 2;
                b.Box("#3a3a3a", Mathf.Cos(a) * .52f, 4.12f, Mathf.Sin(a) * .52f, .03f, .3f, .03f);
            }
            b.P(LowPolyShapes.Cylinder(.3f, .3f, .5f, 10), "#fff2b0", 0, 4.25f, 0);             // phong den
            b.P(LowPolyShapes.Cone(.42f, .4f, 12), "#c8402a", 0, 4.7f, 0);
            b.Box("#5a3a22", 0, .5f, .55f, .3f, .6f, .05f);                                        // cua
        }

        static void Shack(MeshBuilder b)
        {
            string[] W = { "#6a9494", "#5a8282", "#7aa0a0", "#638c8c" };
            string[] plank = { "#9a7448", "#8a6a40", "#a67e50" };
            foreach (var x in new[] { -1.55f, 0, 1.55f })
                foreach (var z in new[] { -1.55f, 0, 1.55f })
                    b.P(LowPolyShapes.Cylinder(.1f, .12f, 2.8f, 7), "#4a3220", x, -1.35f, z);
            for (double z = -1.65; z <= 1.75; z += .26)
                b.Box(plank[((int)System.Math.Round(z * 4 + 9) % 3 + 3) % 3], 0, 0, (float)z, 3.5f, .08f, .24f);
            for (int k = 0; k < 11; k++)
            {
                float x = -1.25f + k * .25f;
                string c = k == 3 || k == 8 ? "#a67e50" : W[k % 4];
                b.Box(c, x, .95f, 1.0f, .24f, 1.8f, .05f);
                b.Box(W[(k + 1) % 4], x, .95f, -1.3f, .24f, 1.8f, .05f);
            }
            for (int k = 0; k < 10; k++)
            {
                float z = -1.2f + k * .24f;
                b.Box(W[k % 4], -1.3f, .95f, z, .05f, 1.8f, .23f);
                b.Box(k == 5 ? "#a67e50" : W[(k + 2) % 4], 1.3f, .95f, z, .05f, 1.8f, .23f);
            }
            b.Box("#5f8a8a", 0, .95f, -.15f, 2.55f, 1.8f, 2.25f);
            // mai ton, mieng va, nok, ong khoi, pin mat troi, ang-ten
            b.Box("#8f9590", -.72f, 2.18f, -.15f, 1.7f, .06f, 2.9f, 0, 0, .52f);
            b.Box("#868c86", .72f, 2.18f, -.15f, 1.7f, .06f, 2.9f, 0, 0, -.52f);
            b.Box("#9a5a3a", .6f, 2.24f, .5f, .6f, .07f, .7f, 0, 0, -.52f);
            b.Box("#6a6e6a", 0, 2.6f, -.15f, .12f, .1f, 2.95f);
            b.P(LowPolyShapes.Cylinder(.08f, .08f, .9f, 8), "#4a4a4a", .75f, 2.65f, -.8f);
            b.P(LowPolyShapes.Cylinder(.12f, .12f, .08f, 8), "#3a3a3a", .75f, 3.12f, -.8f);
            b.Box("#1e2c4a", -.62f, 2.32f, -.6f, .9f, .05f, 1.0f, 0, 0, .52f);
            b.Box("#c8ccd0", -.62f, 2.3f, -.6f, .95f, .03f, 1.05f, 0, 0, .52f);
            b.Box("#8a8a8a", -.2f, 2.9f, -1.0f, .03f, 1.2f, .03f);
            b.Box("#8a8a8a", -.2f, 3.35f, -1.0f, .5f, .03f, .03f);
            // hien truoc, cua, cua so co rem
            b.Box("#8a8f88", 0, 1.92f, 1.55f, 3.4f, .05f, 1.25f, .22f, 0, 0);
            foreach (var x in new[] { -1.5f, 1.5f }) b.Box("#6b4a2a", x, .95f, 2.05f, .1f, 1.9f, .1f);
            b.Box("#3e5a5a", 0, .62f, 1.04f, .6f, 1.15f, .06f);
            b.Box("#f2d79a", 0, .95f, 1.08f, .26f, .22f, .02f);
            b.Box("#c9a46e", .2f, .6f, 1.09f, .06f, .06f, .03f);
            foreach (var x in new[] { -.85f, .85f })
            {
                b.Box("#f2d79a", x, 1.05f, 1.04f, .5f, .42f, .03f);
                b.Box("#5a3a22", x, 1.05f, 1.06f, .58f, .06f, .04f);
                b.Box("#5a3a22", x, 1.05f, 1.06f, .06f, .5f, .04f);
                b.Box("#c94a3a", x - .17f, 1.08f, 1.07f, .12f, .38f, .02f);
                b.Box("#c94a3a", x + .17f, 1.08f, 1.07f, .12f, .38f, .02f);
                b.Box("#8a6a40", x, .8f, 1.12f, .6f, .06f, .14f);
                b.P(LowPolyShapes.Sphere86, "#4f8a3a", x - .15f, .88f, 1.14f, .1f, .08f, .08f);
                b.P(LowPolyShapes.Sphere86, "#d85a7a", x + .12f, .88f, 1.14f, .07f, .06f, .06f);
            }
            foreach (var z in new[] { -.6f, .4f })
            {
                b.Box("#f2d79a", -1.33f, 1.1f, z, .03f, .4f, .45f);
                b.Box("#f2d79a", 1.33f, 1.1f, z, .03f, .4f, .45f);
            }
            // ghe, den treo, phao, luoi, cui, bon nuoc, day phoi
            b.Box("#8a5a30", -.95f, .32f, 1.6f, .45f, .06f, .42f);
            b.Box("#8a5a30", -.95f, .6f, 1.4f, .45f, .55f, .06f, -.2f, 0, 0);
            b.Box("#3a2a1a", 1.0f, 1.7f, 1.9f, .02f, .3f, .02f);
            b.Box("#ffd27a", 1.0f, 1.5f, 1.9f, .12f, .16f, .12f);
            b.P(LowPolyShapes.Sphere86, "#e0502e", 1.32f, .75f, 1.95f, .11f, .14f, .11f);
            b.P(LowPolyShapes.Sphere86, "#f2ede2", 1.32f, .55f, 1.95f, .11f, .08f, .11f);
            b.P(LowPolyShapes.Sphere86, "#e8c23a", -1.6f, .6f, .6f, .1f, .13f, .1f);
            b.P(LowPolyShapes.Sphere86, "#3a7ad0", -1.6f, .4f, .9f, .1f, .13f, .1f);
            for (int k = 0; k < 6; k++)
            {
                b.Box("#2f4a3a", 1.34f, .5f + k * .18f, -.5f, .02f, .02f, 1.4f);
                b.Box("#2f4a3a", 1.34f, .95f, -1.15f + k * .26f, .02f, 1.0f, .02f);
            }
            b.Box("#6a5a3a", 1.45f, .2f, 1.2f, .5f, .35f, .4f);
            b.Box("#4a3a2a", 1.45f, .2f, 1.2f, .52f, .03f, .42f);
            b.Box("#6a5a3a", 1.5f, .55f, 1.2f, .45f, .3f, .35f, 0, .3f, 0);
            for (int k = 0; k < 4; k++)
                for (int q = 0; q < 3; q++)
                    b.P(LowPolyShapes.Cylinder(.06f, .06f, .6f, 6), k % 2 == 1 ? "#7a5232" : "#8a6038", -1.15f + q * .13f, .14f + k * .12f, -1.5f, 1, 1, 1, 0, 0, Mathf.PI / 2);
            b.P(LowPolyShapes.Cylinder(.35f, .35f, .8f, 12), "#5a8aa8", 1.05f, 1.0f, -1.6f);
            b.P(LowPolyShapes.Cylinder(.36f, .36f, .06f, 12), "#3a5a6a", 1.05f, 1.42f, -1.6f);
            b.Box("#4a3220", 1.05f, .3f, -1.6f, .6f, .6f, .6f);
            b.P(LowPolyShapes.Cylinder(.22f, .2f, .55f, 10), "#6b4a2a", -1.5f, .3f, -.1f);
            b.P(LowPolyShapes.Cylinder(.23f, .23f, .04f, 10), "#3a3a3a", -1.5f, .44f, -.1f);
            b.Box("#5a3a22", -1.65f, .9f, -1.6f, .05f, 1.8f, .05f);
            b.Box("#5a3a22", -1.65f, .9f, 1.6f, .05f, 1.8f, .05f);
            b.Box("#d8d0c0", -1.65f, 1.75f, 0, .01f, .01f, 3.2f);
            b.Box("#c94a3a", -1.65f, 1.55f, -.6f, .02f, .35f, .3f);
            b.Box("#3a6ab0", -1.65f, 1.5f, -.1f, .02f, .45f, .35f);
            b.Box("#e8e0cc", -1.65f, 1.58f, .4f, .02f, .3f, .3f);
            // xuong con buoc duoi chan + bac thang xuong nuoc
            b.Box("#a85a3a", 2.1f, -.55f, .4f, .7f, .25f, 1.6f, 0, .2f, 0);
            b.Box("#e8e0cc", 2.1f, -.45f, .4f, .72f, .05f, 1.62f, 0, .2f, 0);
            for (int k = 0; k < 4; k++) b.Box("#8a6a40", -1.9f, -.15f - k * .25f, 1.2f, .3f, .05f, .5f);
        }
    }
}
