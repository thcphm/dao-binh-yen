// Nhan vat chibi dung bang khung xuong Transform (ban web: makeHuman + poseHuman).
// Ban web dung SkinnedMesh lien khoi; o day ghep cac khoi cau/vien nang gan vao tung xuong,
// cung vi tri xuong, cung bang mau, va port nguyen cong thuc tu the di / chay / nhay / tiep dat / chop mat.
//
// Luu y quy uoc xoay: ma tran Rx/Ry/Rz cua three.js va Unity trung nhau ve so hoc, chi khac thu tu Euler
// (three 'XYZ' = Rx*Ry*Rz) -> dung EulerXYZ() o duoi.
using System.Collections.Generic;
using UnityEngine;

namespace SereneIsle
{
    [System.Serializable]
    public class CharacterLook
    {
        public string skin = "#f0c098";
        public string shirt = "#3f7fb0";
        public string pants = "#6b4a2e";
        public string shoe = "#4a3020";
        public string hair = "#6a4326";
        public string hat = "#9a6534";   // de trong = khong doi mu
        public string band = "#3a2715";
        public string tie = "#d8566a";
        public bool female;

        // LOOKS.m / LOOKS.f cua ban web
        public static CharacterLook Male() => new CharacterLook();
        public static CharacterLook Female() => new CharacterLook
        {
            shirt = "#e07a8a", pants = "#4a5a8a", skin = "#f3c6a0", hat = "#e8d29a", band = "#d8566a", hair = "#5a3420", female = true
        };
    }

    /// <summary>Thong so tu the moi frame (giong tham so s cua poseHuman).</summary>
    public struct PoseInput
    {
        public float walkPhase;  // p
        public float amp;        // 0 dung yen, 1 di bo, ~1.55 chay
        public bool air;
        public float vy;
        public float prep;       // 0..1 ngoi lay da truoc khi nhay
        public float land;       // 0..1 nhun khi tiep dat
        public float time;       // T
        public bool old;
    }

    public class ChibiCharacter : MonoBehaviour
    {
        public CharacterLook look = CharacterLook.Male();

        // ---- xuong ----
        Transform _root, _hips, _spine, _head, _eyes;
        Transform _shL, _shR, _elL, _elR, _hnL, _hnR, _thL, _thR, _knL, _knR, _ftL, _ftR;
        public Transform Hand { get; private set; } // diem gan dung cu (tay phai)
        const float HipBase = .44f;
        readonly Dictionary<Transform, Vector3> _rest = new Dictionary<Transform, Vector3>();

        // ---- tu the ----
        enum K { hnR, thL, thR, knL, knR, ftL, ftR, shL, shR, elL, elR, azL, azR, hipY, hipR, spR, lean, hdX, hdY, COUNT }
        const int KC = (int)K.COUNT;
        readonly float[] _P = new float[KC];
        bool _poseInit;
        float _blink;

        bool _built;

        void Awake()
        {
            if (!_built) Build();
        }

        public void Rebuild(CharacterLook newLook)
        {
            look = newLook;
            if (_root) Destroy(_root.gameObject);
            _rest.Clear();
            _clothes.Clear();
            _poseInit = false;
            Build();
        }

        // ================================================================== dung hinh
        Transform Bone(string n, Transform parent, float x, float y, float z)
        {
            var t = new GameObject(n).transform;
            var p = new Vector3(x, y, z);
            _rest[t] = p;
            t.SetParent(parent ? parent : transform, false);
            t.localPosition = parent ? p - _rest[parent] : p;
            return t;
        }

        /// <summary>Gan 1 khoi mesh vao xuong, vi tri tinh theo toa do tuyet doi luc dung thang (nhu ban web).</summary>
        Transform Part(Transform bone, Mesh mesh, string color, Vector3 absPos, Quaternion rot, Vector3 scale, string name = "part")
        {
            var go = new GameObject(name);
            go.transform.SetParent(bone, false);
            go.transform.localPosition = absPos - _rest[bone];
            go.transform.localRotation = rot;
            go.transform.localScale = scale;
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = MaterialLibrary.Solid(color);
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            if (_layer != null)
            {
                if (!_clothes.TryGetValue(_layer, out var list)) _clothes[_layer] = list = new List<ClothPart>();
                list.Add(new ClothPart { renderer = mr, tint = _tint });
                go.name = _layer + "_" + name;
            }
            return go.transform;
        }

        // ---- lop quan ao (dac ta: quan ao la lop rieng, khong dinh vao da) ----
        struct ClothPart { public Renderer renderer; public bool tint; }
        readonly Dictionary<string, List<ClothPart>> _clothes = new Dictionary<string, List<ClothPart>>();
        string _layer;   // dang dung mon nao (null = than nguoi)
        bool _tint;      // phan nay lay mau chinh cua mon do (that lung, de giay thi khong)

        /// <summary>Cac mon dang mac: shirt, pants, shoes, hat.</summary>
        public IEnumerable<string> ClothingSlots => _clothes.Keys;

        /// <summary>Doi mau 1 mon (ma hex sRGB). Chi doi phan mau chinh, giu chi tiet (that lung, de giay...).</summary>
        public void SetClothingColor(string slot, string hex)
        {
            if (!_clothes.TryGetValue(slot, out var list)) return;
            var m = MaterialLibrary.Solid(hex);
            foreach (var p in list) if (p.tint && p.renderer) p.renderer.sharedMaterial = m;
        }

        /// <summary>Mac / coi 1 mon. Coi ra thi lo than nguoi ben duoi (khong bi thung).</summary>
        public void SetClothingVisible(string slot, bool visible)
        {
            if (!_clothes.TryGetValue(slot, out var list)) return;
            foreach (var p in list) if (p.renderer) p.renderer.enabled = visible;
        }

        public bool IsWearing(string slot) => _clothes.TryGetValue(slot, out var l) && l.Count > 0 && l[0].renderer && l[0].renderer.enabled;

        void Wear(string slot, bool tint = true) { _layer = slot; _tint = tint; }
        void Skin() { _layer = null; }

        /// <summary>Elip: r = ban kinh 3 truc (SphereGeometry(1) scale r trong ban web).</summary>
        Transform Ellipsoid(Transform bone, string color, float x, float y, float z, float rx, float ry, float rz,
                            float ex = 0, float ey = 0, float ez = 0)
            => Part(bone, ProceduralMeshes.Sphere, color, new Vector3(x, y, z), EulerXYZ(ex, ey, ez), new Vector3(rx, ry, rz) * 2f, "ellipsoid");

        /// <summary>Vien nang noi 2 diem a-b ban kinh r.</summary>
        Transform Capsule(Transform bone, string color, Vector3 a, Vector3 b, float r)
        {
            Vector3 d = b - a;
            float len = d.magnitude;
            var rot = len > 1e-5f ? Quaternion.FromToRotation(Vector3.up, d / len) : Quaternion.identity;
            return Part(bone, ProceduralMeshes.Capsule, color, (a + b) * .5f, rot, new Vector3(r * 2f, (len + 2f * r) * .5f, r * 2f), "capsule");
        }

        Transform Cylinder(Transform bone, string color, Vector3 center, float r, float h)
            => Part(bone, ProceduralMeshes.Cylinder, color, center, Quaternion.identity, new Vector3(r * 2f, h * .5f, r * 2f), "cylinder");

        /// <summary>Euler theo thu tu 'XYZ' cua three.js (radian).</summary>
        public static Quaternion EulerXYZ(float x, float y, float z)
            => Quaternion.AngleAxis(x * Mathf.Rad2Deg, Vector3.right)
             * Quaternion.AngleAxis(y * Mathf.Rad2Deg, Vector3.up)
             * Quaternion.AngleAxis(z * Mathf.Rad2Deg, Vector3.forward);

        static string Mul(string hex, float k)
        {
            var c = MaterialLibrary.Hex(hex);
            return "#" + ColorUtility.ToHtmlStringRGB(new Color(c.r * k, c.g * k, c.b * k));
        }

        static string Mix(string a, string b, float t)
            => "#" + ColorUtility.ToHtmlStringRGB(Color.Lerp(MaterialLibrary.Hex(a), MaterialLibrary.Hex(b), t));

        void Build()
        {
            _built = true;
            var o = look;
            string skin = o.skin, shirt = o.shirt, pants = o.pants, shoe = string.IsNullOrEmpty(o.shoe) ? "#4a3020" : o.shoe;
            string hair = string.IsNullOrEmpty(o.hair) ? "#6a4326" : o.hair, sock = "#efe8da";
            string skinD = Mul(skin, .88f), blush = Mix(skin, "#ff7a6a", .5f);

            // ---- khung xuong (vi tri tuyet doi giong makeHuman) ----
            _root = Bone("root", null, 0, 0, 0);
            _hips = Bone("hips", _root, 0, .44f, 0);
            _spine = Bone("spine", _hips, 0, .52f, 0);
            _head = Bone("head", _spine, 0, .84f, 0);
            _eyes = Bone("eyes", _head, 0, 1.07f, .25f);
            for (int side = -1; side <= 1; side += 2)
            {
                float s = side;
                var sh = Bone(side < 0 ? "shL" : "shR", _spine, s * .26f, .72f, 0);
                var el = Bone(side < 0 ? "elL" : "elR", sh, s * .33f, .6f, 0);
                var hn = Bone(side < 0 ? "hnL" : "hnR", el, s * .37f, .49f, 0);
                var th = Bone(side < 0 ? "thL" : "thR", _hips, s * .115f, .42f, 0);
                var kn = Bone(side < 0 ? "knL" : "knR", th, s * .115f, .235f, 0);
                var ft = Bone(side < 0 ? "ftL" : "ftR", kn, s * .115f, .09f, 0);
                if (side < 0) { _shL = sh; _elL = el; _hnL = hn; _thL = th; _knL = kn; _ftL = ft; }
                else { _shR = sh; _elR = el; _hnR = hn; _thR = th; _knR = kn; _ftR = ft; }

                // ---- than nguoi (da): tay, chan, ban chan tran - luon day du ----
                Vector3 a0 = new Vector3(s * .26f, .72f, 0), a1 = new Vector3(s * .33f, .6f, 0), a2 = new Vector3(s * .37f, .49f, 0);
                Skin();
                Capsule(sh, skin, a0, a1, .062f);
                Capsule(el, skin, a1, a2, .054f);
                Ellipsoid(hn, skin, s * .378f, .45f, 0, .062f, .066f, .062f);
                Capsule(th, skin, new Vector3(s * .115f, .43f, 0), new Vector3(s * .115f, .25f, 0), .08f);
                Capsule(kn, skin, new Vector3(s * .115f, .25f, 0), new Vector3(s * .115f, .1f, 0), .066f);
                Ellipsoid(ft, skin, s * .115f, .045f, .03f, .066f, .046f, .105f);

                // ---- quan ao (lop rieng phu ngoai) ----
                Wear("shirt");
                Capsule(sh, shirt, a0, Vector3.Lerp(a0, a1, .55f), .074f);          // ong tay ao
                Wear("pants");
                Capsule(th, pants, new Vector3(s * .115f, .43f, 0), new Vector3(s * .115f, .28f, 0), .09f); // ong quan
                Wear("shoes", false);
                Cylinder(kn, sock, new Vector3(s * .115f, .125f, 0), .07f, .06f);
                Wear("shoes");
                Ellipsoid(ft, shoe, s * .115f, .052f, .035f, .088f, .062f, .132f);
                Wear("shoes", false);
                Ellipsoid(ft, "#2e1e14", s * .115f, .01f, .035f, .09f, .018f, .136f);
                Skin();
            }

            // ---- than nguoi (da) ----
            Capsule(_hips, skin, new Vector3(0, .36f, 0), new Vector3(0, .47f, 0), .168f);
            Capsule(_spine, skin, new Vector3(0, .5f, 0), new Vector3(0, .76f, 0), .188f);
            // ---- quan ao ----
            Wear("pants");
            Capsule(_hips, pants, new Vector3(0, .36f, 0), new Vector3(0, .47f, 0), .18f);
            Wear("pants", false);
            Cylinder(_hips, "#5a3d22", new Vector3(0, .49f, 0), .212f, .034f); // that lung (rong hon ao de khong chong mat)
            Wear("shirt");
            Capsule(_spine, shirt, new Vector3(0, .5f, 0), new Vector3(0, .76f, 0), .2f);
            Skin();

            // ---- dau (elip .3/.285/.28 tai H0) ----
            const float H0 = 1.06f;
            Ellipsoid(_head, skin, 0, H0, 0, .3f, .285f, .28f);
            for (int side = -1; side <= 1; side += 2)
            {
                float s = side;
                Ellipsoid(_head, skinD, s * .29f, H0 - .01f, 0, .05f, .068f, .038f);       // tai
                Decal(_eyes, s * .1f, .01f, .036f, .05f, .02f, "#2a1a12");                  // mat
                Decal(_eyes, s * .1f + .012f, .032f, .011f, .014f, .006f, "#ffffff", 0, .009f); // dom sang
                Decal(_head, s * .1f, .088f, .05f, .012f, .012f, hair, -s * .12f);           // long may
                Decal(_head, s * .172f, -.07f, .046f, .026f, .006f, blush);                  // ma hong
            }
            Decal(_head, 0, -.035f, .03f, .026f, .03f, skinD);       // mui
            Decal(_head, 0, -.098f, .04f, .012f, .012f, "#8a3a2e");  // mieng (ban web: nua hinh xuyen)

            // toc: chop cau nghieng ra sau + bui sau gay
            Part(_head, ProceduralMeshes.SphereCap(Mathf.PI * .56f), hair, new Vector3(0, H0 + .02f, -.015f), EulerXYZ(-.32f, 0, 0),
                 new Vector3(.31f, .3f, .3f), "hairCap");
            Ellipsoid(_head, hair, 0, H0 - .02f, -.12f, .29f, .25f, .2f);
            if (o.female)
            {
                Ellipsoid(_head, hair, 0, H0 - .1f, -.12f, .305f, .29f, .2f, .18f);
                for (int side = -1; side <= 1; side += 2)
                    Ellipsoid(_head, hair, side * .255f, H0 - .1f, .05f, .07f, .19f, .1f, 0, 0, side * .12f);
                Ellipsoid(_head, string.IsNullOrEmpty(o.tie) ? "#d8566a" : o.tie, 0, H0 - .06f, -.27f, .07f, .06f, .06f);
                for (int k = 0; k < 4; k++)
                    Ellipsoid(_head, hair, 0, H0 - .13f - k * .075f, -.3f - k * .012f, .085f - k * .012f, .075f, .075f - k * .01f, .2f);
                Ellipsoid(_head, hair, 0, H0 + .16f, .17f, .2f, .06f, .1f, .5f);
            }
            if (string.IsNullOrEmpty(o.hat))
            {
                Ellipsoid(_head, hair, -.12f, H0 + .2f, .17f, .11f, .06f, .08f, .4f, 0, .3f);
                Ellipsoid(_head, hair, .05f, H0 + .22f, .18f, .1f, .06f, .08f, .5f, 0, -.2f);
            }
            else BuildHat(o, H0);

            // diem cam dung cu
            Hand = new GameObject("hand").transform;
            Hand.SetParent(_hnR, false);
            Hand.localPosition = new Vector3(0, -.05f, .01f);
            _rest[Hand] = _rest[_hnR] + Hand.localPosition;

            _blink = 2 + Random.value * 3;
        }

        void BuildHat(CharacterLook o, float H0)
        {
            // ban web: LatheGeometry trong ma tran hm = (0, H0+.18, 0), xoay x -.06, ti le 1.7
            var hatRoot = new GameObject("hat").transform;
            hatRoot.SetParent(_head, false);
            hatRoot.localPosition = new Vector3(0, H0 + .18f, 0) - _rest[_head];
            hatRoot.localRotation = EulerXYZ(-.06f, 0, 0);
            hatRoot.localScale = Vector3.one * 1.7f;
            _rest[hatRoot] = new Vector3(0, H0 + .18f, 0);

            var crown = new[] { new Vector2(.137f, -.005f), new Vector2(.13f, .06f), new Vector2(.112f, .11f), new Vector2(.07f, .132f), new Vector2(0, .135f) };
            // vanh mu: di mat duoi ra ngoai roi mat tren vao trong -> ca 2 mat huong ra ngoai
            var brim = new[] { new Vector2(.12f, -.012f), new Vector2(.2f, -.018f), new Vector2(.27f, -.042f), new Vector2(.285f, -.044f),
                               new Vector2(.268f, -.028f), new Vector2(.2f, -.004f), new Vector2(.12f, .004f) };
            Wear("hat");
            HatPart(hatRoot, ProceduralMeshes.Lathe(crown, 20, "HatCrown"), o.hat, Vector3.zero, Vector3.one);
            HatPart(hatRoot, ProceduralMeshes.Lathe(brim, 28, "HatBrim"), o.hat, Vector3.zero, Vector3.one);
            Wear("hat", false);
            HatPart(hatRoot, ProceduralMeshes.Cylinder, string.IsNullOrEmpty(o.band) ? "#3a2715" : o.band,
                    new Vector3(0, .018f, 0), new Vector3(.272f, .015f, .272f));
            Skin();
        }

        void HatPart(Transform parent, Mesh mesh, string color, Vector3 localPos, Vector3 scale)
        {
            var go = new GameObject("hatPart");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = scale;
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = MaterialLibrary.Solid(color);
            if (_layer != null)
            {
                if (!_clothes.TryGetValue(_layer, out var list)) _clothes[_layer] = list = new List<ClothPart>();
                list.Add(new ClothPart { renderer = mr, tint = _tint });
            }
        }

        /// <summary>Chi tiet dan sat mat dau (ban web: decal tren elip .3/.285/.28).</summary>
        void Decal(Transform bone, float x, float y, float sx, float sy, float th, string color, float roll = 0, float lift = 0)
        {
            const float H0 = 1.06f;
            float z = .28f * Mathf.Sqrt(Mathf.Max(0, 1 - (x / .3f) * (x / .3f) - (y / .285f) * (y / .285f)));
            var p = new Vector3(x, H0 + y, z);
            var n = new Vector3(x / .09f, y / .081225f, z / .0784f).normalized;
            var q = Quaternion.FromToRotation(Vector3.forward, n);
            if (roll != 0) q *= Quaternion.AngleAxis(roll * Mathf.Rad2Deg, Vector3.forward);
            p += n * (lift - th * .5f);
            Part(bone, ProceduralMeshes.Sphere, color, p, q, new Vector3(sx, sy, th) * 2f, "decal");
        }

        // ================================================================== tu the
        static float Smooth(float a, float b, float x) => (float)IslandNoise.Smooth(a, b, x);

        static void Mix(float[] a, float[] b, float t, float[] dst)
        {
            for (int i = 0; i < KC; i++) dst[i] = a[i] + (b[i] - a[i]) * t;
        }

        readonly float[] _W = new float[KC], _R = new float[KC], _T = new float[KC], _J = new float[KC], _U = new float[KC], _D = new float[KC], _L = new float[KC], _A = new float[KC];

        // ---- dong tac dang dien (ban web: PL.swing / PL.swingDur / PL.act) ----
        string _act;
        float _swing, _swingDur = .4f;
        GameObject _held;

        public bool Acting => _swing > 0;
        public string CurrentAct => _swing > 0 ? _act : null;

        /// <summary>startAct: dien 1 dong tac theo bang ACTS (eat, drink, axe, pick...).</summary>
        public void PlayAct(string act, ItemDefinition held = null)
        {
            _act = act;
            _swingDur = _swing = CharacterActions.DurationOf(act);
            ShowHeld(held);
        }

        /// <summary>Hien mon do tren tay phai (ban web: emoji sprite gan vao player.hand).</summary>
        void ShowHeld(ItemDefinition it)
        {
            if (_held) Destroy(_held);
            _held = null;
            if (!it || !Hand) return;
            _held = new GameObject("held_" + it.id);
            _held.transform.SetParent(Hand, false);
            _held.transform.localPosition = new Vector3(0, .04f, .1f);
            var mesh = it.IsDrink ? ProceduralMeshes.Cylinder : ProceduralMeshes.Sphere;
            _held.transform.localScale = it.IsDrink ? new Vector3(.09f, .08f, .09f) : Vector3.one * .15f;
            _held.AddComponent<MeshFilter>().sharedMesh = mesh;
            _held.AddComponent<MeshRenderer>().sharedMaterial = MaterialLibrary.Solid(it.iconColor);
        }

        static void Set(float[] d, float hnR, float thL, float thR, float knL, float knR, float ftL, float ftR, float shL, float shR,
                        float elL, float elR, float azL, float azR, float hipY, float hipR, float spR, float lean, float hdX, float hdY)
        {
            d[0] = hnR; d[1] = thL; d[2] = thR; d[3] = knL; d[4] = knR; d[5] = ftL; d[6] = ftR; d[7] = shL; d[8] = shR;
            d[9] = elL; d[10] = elR; d[11] = azL; d[12] = azR; d[13] = hipY; d[14] = hipR; d[15] = spR; d[16] = lean; d[17] = hdX; d[18] = hdY;
        }

        /// <summary>Port poseHuman (phan di/chay/nhay/nhun; dong tac dung cu se them sau).</summary>
        public void Pose(float dt, PoseInput s)
        {
            if (!_built) return;
            float amp = s.amp, p = s.walkPhase, sn = Mathf.Sin(p), cs = Mathf.Cos(p), T = s.time;
            float a = Mathf.Min(1, amp), run = Smooth(1.05f, 1.5f, amp), idle = 1 - a;

            // di bo
            Set(_W, 0, -sn * .6f * a, sn * .6f * a,
                .06f + a * (.12f + .8f * Mathf.Max(0, cs)), .06f + a * (.12f + .8f * Mathf.Max(0, -cs)), 0, 0,
                sn * .5f * a + Mathf.Sin(T * 1.3f) * .03f * idle, -sn * .5f * a + Mathf.Sin(T * 1.3f + 1) * .03f * idle,
                -(.16f + .45f * a * Mathf.Max(0, -sn)), -(.16f + .45f * a * Mathf.Max(0, sn)),
                -.02f * Mathf.Sin(T * 2.2f) * idle, .02f * Mathf.Sin(T * 2.2f) * idle,
                -.008f * a + .016f * a * (.5f + .5f * Mathf.Cos(2 * p)), sn * .12f * a, -sn * .2f * a,
                .04f * a + Mathf.Sin(T * 2.2f) * .012f * idle, 0, Mathf.Sin(T * .5f) * .15f * idle);
            // chay: sai chan dai, goi nhac cao, tay gap 90 do, nguoi do toi
            Set(_R, 0, -sn * .95f + .1f, sn * .95f + .1f,
                .25f + 1.55f * Mathf.Max(0, cs) + .35f * Mathf.Max(0, -sn), .25f + 1.55f * Mathf.Max(0, -cs) + .35f * Mathf.Max(0, sn),
                .25f * Mathf.Max(0, -cs), .25f * Mathf.Max(0, cs), sn * .9f - .15f, -sn * .9f - .15f,
                -1.45f - .2f * Mathf.Max(0, -sn), -1.45f - .2f * Mathf.Max(0, sn), .12f, -.12f,
                -.02f + .03f * Mathf.Abs(Mathf.Cos(p)), sn * .18f, -sn * .3f, .26f, 0, 0);
            var t = _T;
            Mix(_W, _R, run, t);
            t[(int)K.hdY] += -t[(int)K.spR] * .55f;
            t[(int)K.hdX] = -t[(int)K.lean] * .65f + Mathf.Sin(T * .8f) * .03f * idle;
            if (s.old) { t[(int)K.lean] += .16f; t[(int)K.hdX] -= .12f; t[(int)K.knL] += .08f; t[(int)K.knR] += .08f; t[(int)K.shL] += .15f; t[(int)K.shR] += .15f; }

            // nhay: bat len co chan, roi xuong duoi chan
            if (s.air)
            {
                float f = Smooth(3.5f, -3f, s.vy);
                Set(_U, t[0], -1.0f, .15f, 1.5f, .95f, .35f, .45f, -2.3f, -2.3f, -.35f, -.35f, -.35f, .35f, 0, 0, 0, .12f, -.12f, 0);
                Set(_D, t[0], -.45f, -.05f, .55f, .35f, .15f, .1f, -.7f, -.7f, -.5f, -.5f, -1.0f, 1.0f, 0, 0, 0, .02f, .08f, 0);
                Mix(_U, _D, f, _J);
                if (run > .3f)
                {
                    Set(_L, t[0], -1.1f, .75f, .75f, 1.1f, .2f, .4f, .8f, -1.6f, -1.2f, -.9f, -.3f, .3f, 0, -.15f, .25f, .22f, -.1f, -.1f);
                    Mix(_J, _L, (1 - f * .6f) * run, _J);
                }
                for (int i = 1; i < KC; i++) t[i] = _J[i]; // hnR giu nguyen
            }

            // ngoi lay da / nhun khi tiep dat
            float cr = Mathf.Max(s.prep, s.land);
            if (cr > 0)
            {
                float c = cr;
                t[(int)K.thL] += -.75f * c; t[(int)K.thR] += -.75f * c; t[(int)K.knL] += 1.35f * c; t[(int)K.knR] += 1.35f * c;
                t[(int)K.hipY] += -.07f * c; t[(int)K.lean] += .3f * c; t[(int)K.hdX] -= .15f * c;
                if (s.prep > 0) { t[(int)K.shL] = .7f * c; t[(int)K.shR] = .7f * c; t[(int)K.elL] = t[(int)K.elR] = -.3f; }
                else { t[(int)K.shL] = -.5f * c; t[(int)K.shR] = -.5f * c; t[(int)K.azL] = -.5f * c; t[(int)K.azR] = .5f * c; }
            }

            bool fast = s.air || cr > 0;
            float k = Mathf.Min(1, dt * (fast ? 22 : 12));
            if (!_poseInit) { System.Array.Copy(t, _P, KC); _poseInit = true; }
            for (int i = 0; i < KC; i++) _P[i] += (t[i] - _P[i]) * k;

            // dong tac (an, uong, dung cu...): tron len tu the, nha dan o 20% cuoi
            if (_swing > 0)
            {
                _swing = Mathf.Max(0, _swing - dt);
                if (CharacterActions.Has(_act))
                {
                    float q = 1 - _swing / _swingDur;
                    CharacterActions.Sample(_act, q, _A);
                    float w = q < .8f ? 1 : 1 - (q - .8f) / .2f;
                    for (int i = 0; i < KC; i++) if (!float.IsNaN(_A[i])) _P[i] = _P[i] * (1 - w) + _A[i] * w;
                }
                if (_swing <= 0) { _act = null; ShowHeld(null); }
            }

            Apply();

            // chop mat
            _blink -= dt;
            if (_blink < 0)
            {
                _eyes.localScale = new Vector3(1, .12f, 1);
                if (_blink < -.11f) _blink = 2.2f + Random.value * 3.5f;
            }
            else _eyes.localScale = Vector3.one;
        }

        void Apply()
        {
            var P = _P;
            float D = Mathf.Rad2Deg;
            _thL.localRotation = Quaternion.Euler(P[(int)K.thL] * D, 0, 0);
            _thR.localRotation = Quaternion.Euler(P[(int)K.thR] * D, 0, 0);
            _knL.localRotation = Quaternion.Euler(P[(int)K.knL] * D, 0, 0);
            _knR.localRotation = Quaternion.Euler(P[(int)K.knR] * D, 0, 0);
            _ftL.localRotation = Quaternion.Euler((-(P[(int)K.thL] + P[(int)K.knL]) * .9f + P[(int)K.ftL]) * D, 0, 0);
            _ftR.localRotation = Quaternion.Euler((-(P[(int)K.thR] + P[(int)K.knR]) * .9f + P[(int)K.ftR]) * D, 0, 0);
            _shL.localRotation = EulerXYZ(P[(int)K.shL], 0, P[(int)K.azL]);
            _shR.localRotation = EulerXYZ(P[(int)K.shR], 0, P[(int)K.azR]);
            _elL.localRotation = Quaternion.Euler(P[(int)K.elL] * D, 0, 0);
            _elR.localRotation = Quaternion.Euler(P[(int)K.elR] * D, 0, 0);
            _hnR.localRotation = Quaternion.Euler(P[(int)K.hnR] * D, 0, 0);
            var hp = _hips.localPosition; hp.y = HipBase + P[(int)K.hipY]; _hips.localPosition = hp;
            _hips.localRotation = Quaternion.Euler(0, P[(int)K.hipR] * D, 0);
            _spine.localRotation = EulerXYZ(P[(int)K.lean], P[(int)K.spR], 0);
            _head.localRotation = EulerXYZ(P[(int)K.hdX], P[(int)K.hdY], 0);
        }
    }
}
