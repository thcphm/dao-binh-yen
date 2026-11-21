// Nhan vat nguoi choi / dan dao: mo hinh KayKit (CC0) mac do casual + animation KayKit phat bang Playables.
// Thay ChibiCharacter (khoi rieng le) - giu cung giao dien: Pose(), PlayAct(), Acting, CurrentAct, Hand.
using System.Collections.Generic;
using UnityEngine;

namespace SereneIsle
{
    public class IslandCharacter : MonoBehaviour
    {
        public CharacterOutfit outfit = CharacterOutfit.Male();
        [Tooltip("Chieu cao nhan vat (o). Ban web ~1.3")]
        public float height = 1.3f;
        [Tooltip("Xoay them mo hinh quanh Y (do) neu FBX khong nhin ve +Z")]
        public float modelYaw;

        public Transform Hand { get; private set; }   // diem gan dung cu (handslot.r)
        public Transform Model { get; private set; }
        public CharacterAnimator Anim { get; private set; }
        public float Scale { get; private set; } = 1;

        // ---- dong tac (eat, drink, axe...) ----
        static readonly Dictionary<string, string> ActClips = new Dictionary<string, string>
        {
            ["eat"] = "Use_Item", ["drink"] = "Use_Item", ["axe"] = "Chopping", ["pick"] = "Pickaxing", ["shovel"] = "Digging",
            ["hoe"] = "Digging", ["can"] = "Use_Item", ["seed"] = "PickUp", ["rod"] = "Fishing_Cast", ["net"] = "Throw",
            ["pickup"] = "PickUp", ["wave"] = "Waving", ["cheer"] = "Cheering", ["hammer"] = "Hammering", ["build"] = "PickUp", ["faint"] = "Death_A",
        };

        string _act;
        float _swing, _swingDur;
        GameObject _held;
        bool _built;

        readonly List<SkinnedMeshRenderer> _renderers = new List<SkinnedMeshRenderer>();
        readonly List<Mesh> _srcMeshes = new List<Mesh>();
        readonly List<Mesh> _ownMeshes = new List<Mesh>();
        Texture2D _palette, _srcPalette;
        Material _mat;
        Transform[] _rootBones;

        public bool Acting => _swing > 0;
        /// <summary>Tu the giu nguyen khi dung yen (vd. "Fishing_Idle" luc cho ca). null = dung binh thuong.</summary>
        public string HoldPose { get; set; }
        public string CurrentAct => _swing > 0 ? _act : null;

        void Awake()
        {
            if (!_built) Build();
        }

        void OnDestroy()
        {
            Anim?.Destroy();
            foreach (var m in _ownMeshes) if (m) Destroy(m);
            if (_palette) Destroy(_palette);
            if (_mat) Destroy(_mat);
        }

        // ================================================================== dung hinh
        void Build()
        {
            _built = true;
            var lib = CharacterLibrary.Instance;
            var entry = lib ? lib.GetModel(outfit.model) : null;
            if (entry == null || !entry.prefab)
            {
                Debug.LogError("[SereneIsle] Chua co CharacterLibrary (chay menu Serene Isle > Nhap nhan vat KayKit).");
                return;
            }

            var holder = new GameObject("Model").transform;
            holder.SetParent(transform, false);
            holder.localRotation = Quaternion.Euler(0, modelYaw, 0);
            var inst = Instantiate(entry.prefab, holder, false);
            inst.name = entry.id;
            Model = inst.transform;

            var animator = inst.GetComponent<Animator>();
            if (!animator) animator = inst.AddComponent<Animator>();
            animator.avatar = entry.avatar;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.runtimeAnimatorController = null;

            _srcPalette = entry.palette;
            inst.GetComponentsInChildren(true, _renderers);
            foreach (var r in _renderers) _srcMeshes.Add(r.sharedMesh);

            // ti le: dinh dau cao 'height' o
            float top = 0;
            foreach (var r in _renderers)
            {
                if (!OutfitBuilder.IsBasePart(OutfitBuilder.PartOf(r.name))) continue;
                var mesh = r.sharedMesh;
                if (!mesh || !mesh.isReadable) continue;
                foreach (var v in mesh.vertices) top = Mathf.Max(top, ModelHeight(r, v));
            }
            Scale = top > 0 ? height / top : 1;
            holder.localScale = Vector3.one * Scale;

            Hand = FindDeep(Model, "handslot.r");
            ApplyOutfit(outfit);

            Anim = new CharacterAnimator(animator, "Char_" + name);
            Play("Idle_A", 0);
        }

        float ModelHeight(SkinnedMeshRenderer r, Vector3 v) => Model.InverseTransformPoint(r.transform.TransformPoint(v)).y;

        static Transform FindDeep(Transform t, string n)
        {
            if (t.name == n) return t;
            foreach (Transform c in t)
            {
                var r = FindDeep(c, n);
                if (r) return r;
            }
            return null;
        }

        /// <summary>Mac bo do moi (doi kieu -> dung lai mesh; doi mau -> ve lai bang mau).</summary>
        public void ApplyOutfit(CharacterOutfit o)
        {
            outfit = o;
            if (!Model) return;
            bool casual = OutfitBuilder.Supports(o.model) && _srcPalette && _srcPalette.isReadable;
            if (casual)
            {
                if (!_palette) _palette = OutfitBuilder.NewPalette(_srcPalette);
                OutfitBuilder.Paint(_palette, _srcPalette, o);
                if (!_mat) _mat = MaterialLibrary.Textured(_palette, "SI_Char_" + o.model);
            }
            else if (!_mat) _mat = MaterialLibrary.Textured(_srcPalette, "SI_Char_" + o.model);

            foreach (var m in _ownMeshes) if (m) Destroy(m);
            _ownMeshes.Clear();
            for (int i = 0; i < _renderers.Count; i++)
            {
                var r = _renderers[i];
                string part = OutfitBuilder.PartOf(r.name);
                bool basePart = OutfitBuilder.IsBasePart(part);
                r.enabled = basePart || !casual; // do casual: bo ao choang, ong ten...
                r.sharedMaterial = _mat;
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
                r.updateWhenOffscreen = true;
                var src = _srcMeshes[i];
                if (casual && basePart && src && src.isReadable)
                {
                    var rr = r;
                    var mesh = OutfitBuilder.BuildMesh(src, r.bones, o.model, part, v => ModelHeight(rr, v), o);
                    _ownMeshes.Add(mesh);
                    r.sharedMesh = mesh;
                }
                else r.sharedMesh = src;
            }
        }

        /// <summary>Gan 1 vat (non la...) len dinh dau, theo xuong dau. sink: thut xuong tu dinh dau (o).</summary>
        public Transform AttachToHead(Mesh mesh, Material mat, float sink = .08f)
        {
            if (!Model) { Debug.LogWarning("[SereneIsle] AttachToHead: chua co mo hinh"); return null; }
            var head = FindDeep(Model, "head") ?? FindDeep(Model, "Head");
            if (!head) { Debug.LogWarning("[SereneIsle] AttachToHead: khong thay xuong dau"); return null; }
            float top = 0;
            foreach (var r in _renderers)
            {
                if (OutfitBuilder.PartOf(r.name) != "Head" || !r.sharedMesh || !r.sharedMesh.isReadable) continue;
                foreach (var v in r.sharedMesh.vertices) top = Mathf.Max(top, ModelHeight(r, v));
            }
            if (top <= 0) { Debug.LogWarning($"[SereneIsle] AttachToHead: khong do duoc dinh dau ({_renderers.Count} renderer)"); return null; }
            float worldTop = Model.TransformPoint(new Vector3(0, top, 0)).y;
            var go = new GameObject("hat");
            go.transform.position = new Vector3(head.position.x, worldTop - sink, head.position.z);
            go.transform.rotation = transform.rotation;
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = mat;
            go.transform.SetParent(head, true);
            return go.transform;
        }

        /// <summary>Cac mon doi mau duoc (giu ten cu cho HUD/test): shirt, pants, shoes, skin, hair.</summary>
        public IEnumerable<string> ClothingSlots => new[] { "shirt", "pants", "shoes", "skin", "hair" };

        public void SetClothingColor(string slot, string hex)
        {
            switch (slot)
            {
                case "shirt": outfit.shirt = hex; break;
                case "pants": outfit.pants = hex; break;
                case "shoes": outfit.shoe = hex; break;
                case "skin": outfit.skin = hex; break;
                case "hair": outfit.hair = hex; break;
                default: return;
            }
            if (_palette && _srcPalette) OutfitBuilder.Paint(_palette, _srcPalette, outfit);
        }

        // ================================================================== dong tac
        void Play(string clip, float fade, float speed = 1, bool restart = false)
        {
            var c = CharacterLibrary.Instance ? CharacterLibrary.Instance.GetClip(clip) : null;
            if (c != null) Anim?.Play(c.clip, fade, speed, restart);
        }

        /// <summary>Dien 1 dong tac (eat, drink, axe, pick...) trong CharacterActions.DurationOf(act) giay.</summary>
        public void PlayAct(string act, ItemDefinition held = null)
        {
            _act = act;
            _swingDur = _swing = CharacterActions.DurationOf(act);
            ShowHeld(held);
            if (!ActClips.TryGetValue(act, out var clipName)) clipName = "Interact";
            var c = CharacterLibrary.Instance ? CharacterLibrary.Instance.GetClip(clipName) : null;
            float speed = c != null && _swingDur > 0 ? Mathf.Clamp(c.clip.length / _swingDur, .6f, 2.5f) : 1;
            Play(clipName, .12f, speed, true);
        }

        // ---- dung cu cam tay (ban web: refreshToolMesh) ----
        [Tooltip("Xoay dung cu so voi handslot.r cua KayKit (do). Ban web: can huong +z, luoi -y.")]
        public Vector3 toolAlign = new Vector3(-90, 0, 0);
        GameObject _tool;
        string _toolId;
        int _toolLevel;

        /// <summary>Cam dung cu dang chon tren thanh nhanh (null = tay khong). Goi moi khung, chi dung lai khi doi.</summary>
        public void SetTool(string id, int level = 1)
        {
            if (!ToolMeshes.Has(id ?? "")) id = null;
            if (id == _toolId && level == _toolLevel) { if (_tool) _tool.SetActive(!_held); return; }
            _toolId = id; _toolLevel = level;
            if (_tool) Destroy(_tool);
            _tool = null;
            if (id == null || !Hand) return;
            _tool = new GameObject("tool_" + id);
            _tool.transform.SetParent(Hand, false);
            _tool.transform.localRotation = Quaternion.Euler(toolAlign) * Quaternion.Euler(ToolMeshes.Tilt(id) * Mathf.Rad2Deg, 0, 0);
            _tool.transform.localScale = Vector3.one * (.85f / Mathf.Max(.01f, Scale));
            _tool.AddComponent<MeshFilter>().sharedMesh = ToolMeshes.Get(id, level);
            var mr = _tool.AddComponent<MeshRenderer>();
            mr.sharedMaterial = MaterialLibrary.Objects(false);
            _tool.SetActive(!_held);
        }

        void ShowHeld(ItemDefinition it)
        {
            if (_held) Destroy(_held);
            _held = null;
            if (!it || !Hand) return;
            _held = new GameObject("held_" + it.id);
            _held.transform.SetParent(Hand, false);
            // handslot o ti le goc cua mo hinh -> chia lai de do vat co kich thuoc that (o)
            float inv = 1f / Mathf.Max(.01f, Scale);
            var mesh = it.IsDrink ? ProceduralMeshes.Cylinder : ProceduralMeshes.Sphere;
            _held.transform.localScale = (it.IsDrink ? new Vector3(.09f, .08f, .09f) : Vector3.one * .15f) * inv;
            _held.AddComponent<MeshFilter>().sharedMesh = mesh;
            var mr = _held.AddComponent<MeshRenderer>();
            mr.sharedMaterial = MaterialLibrary.Solid(it.iconColor);
        }

        /// <summary>Cap nhat tu the moi khung (goi tu PlayerController).</summary>
        public void Pose(float dt, PoseInput s)
        {
            if (Anim == null) return;
            if (_swing > 0)
            {
                _swing -= dt;
                if (_swing <= 0) { _swing = 0; ShowHeld(null); }
                else { Anim.Update(dt); return; }
            }

            if (HoldPose != null && !s.air && s.amp < .05f) Play(HoldPose, .2f);
            else if (s.prep > 0) Play("Jump_Start", .05f, 3f);
            else if (s.air) Play("Jump_Idle", .12f);
            else if (s.land > .05f) Play("Jump_Land", .06f, 2f);
            else if (s.amp < .05f) Play("Idle_A", .25f);
            else
            {
                string clip = s.amp > 1.25f ? "Running_A" : "Walking_A";
                // nhip buoc giong ban web (pha chan tang dt * toc do * 2.1 rad): so chu ky/giay = v * 2.1 / 2pi.
                // Nhan vat chibi buoc ngan nen van truot nhe - giong ban web.
                var c = CharacterLibrary.Instance.GetClip(clip);
                float speed = c != null ? Mathf.Clamp(s.speed * 2.1f / (2 * Mathf.PI) * c.clip.length, .6f, 2.4f) : 1;
                Play(clip, .18f, speed);
            }
            Anim.Update(dt);
        }
    }
}
