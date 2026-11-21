// Than thu: mo hinh Quaternius (to mau lai, phat Idle / Walk / Gallop / Eating bang Playables) hoac khoi dung bang code
// (nhun, nhay coc nhu ban web). Ai dieu khien (thu hoang, vat nuoi, thu cung) chi goi Move() moi khung.
using System.Collections.Generic;
using UnityEngine;

namespace SereneIsle
{
    public class AnimalBody : MonoBehaviour
    {
        public AnimalDef Def { get; private set; }
        public Transform Visual { get; private set; }
        public bool Animated => _anim != null;
        public string Pose { get; private set; }

        CharacterAnimator _anim;
        AnimalLibrary.Model _model;
        float _hop, _sit;
        static readonly Dictionary<string, Material> _mats = new Dictionary<string, Material>();

        public static AnimalBody Create(AnimalDef def, Transform parent, Vector3 pos, float rot)
        {
            var go = new GameObject("Animal_" + def.id);
            go.transform.SetParent(parent, false);
            go.transform.SetPositionAndRotation(pos, Quaternion.Euler(0, rot * Mathf.Rad2Deg, 0));
            var body = go.AddComponent<AnimalBody>();
            body.Build(def);
            return body;
        }

        void Build(AnimalDef def)
        {
            Def = def;
            var lib = AnimalLibrary.Instance;
            _model = def.quaternius != null && lib ? lib.Get(def.quaternius) : null;
            if (_model != null && _model.prefab)
            {
                var holder = new GameObject("Model").transform;
                holder.SetParent(transform, false);
                var inst = Instantiate(_model.prefab, holder, false);
                inst.name = _model.id;
                // ti le theo chieu cao
                var rs = inst.GetComponentsInChildren<SkinnedMeshRenderer>(true);
                float top = 0;
                foreach (var r in rs)
                {
                    if (!r.sharedMesh || !r.sharedMesh.isReadable) continue;
                    foreach (var v in r.sharedMesh.vertices) top = Mathf.Max(top, inst.transform.InverseTransformPoint(r.transform.TransformPoint(v)).y);
                }
                holder.localScale = Vector3.one * (top > 0 ? def.height / top : 1);
                foreach (var r in rs)
                {
                    var src = r.sharedMaterials;
                    var mats = new Material[src.Length];
                    for (int i = 0; i < src.Length; i++) mats[i] = Recolor(def, src[i] ? src[i].name : "Main");
                    r.sharedMaterials = mats;
                    r.updateWhenOffscreen = true;
                    r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
                }
                var animator = inst.GetComponent<Animator>();
                if (!animator) animator = inst.AddComponent<Animator>();
                animator.avatar = _model.avatar;
                animator.applyRootMotion = false;
                animator.cullingMode = AnimatorCullingMode.CullCompletely;
                animator.runtimeAnimatorController = null;
                _anim = new CharacterAnimator(animator, "Animal_" + def.id);
                Visual = holder;
                Play("Idle", 0);
            }
            else
            {
                var v = new GameObject("Mesh");
                v.transform.SetParent(transform, false);
                v.AddComponent<MeshFilter>().sharedMesh = AnimalDefs.MeshOf(def.id);
                v.AddComponent<MeshRenderer>().sharedMaterial = MaterialLibrary.Objects(false);
                Visual = v.transform;
            }
        }

        /// <summary>Vat lieu WebLambert theo ten vat lieu goc ("Main.001" -> "Main").</summary>
        static Material Recolor(AnimalDef def, string matName)
        {
            string key = matName.Replace(" (Instance)", "");
            int dot = key.IndexOf('.');
            if (dot > 0) key = key.Substring(0, dot);
            string hex = def.colors != null && def.colors.TryGetValue(key, out var h) ? h : "#8a7a6a";
            if (_mats.TryGetValue(hex, out var m) && m) return m;
            m = new Material(MaterialLibrary.Lambert) { name = "SI_Animal_" + hex.TrimStart('#') };
            m.SetColor("_BaseColor", MaterialLibrary.Hex(hex));
            m.SetFloat("_VertexColorWeight", 0);
            _mats[hex] = m;
            return m;
        }

        void OnDestroy() { _anim?.Destroy(); }

        void Play(string clip, float fade, float speed = 1)
        {
            if (_anim == null) return;
            var c = AnimalLibrary.Clip(_model, clip);
            if (!c && clip == "Idle_Headlow") c = AnimalLibrary.Clip(_model, "Idle_2_HeadLow");
            if (!c) c = AnimalLibrary.Clip(_model, "Idle");
            if (c) { _anim.Play(c, fade, speed); Pose = clip; }
        }

        /// <summary>Cap nhat moi khung. speed: toc do that (o/giay); idle: tu the dung yen (Idle, Eating, Idle_Headlow, Sit).</summary>
        public void Move(float speed, float dt, string idle = "Idle")
        {
            if (_anim != null)
            {
                if (speed > Def.speed * .6f && speed > 1.8f) Play("Gallop", .15f, Mathf.Clamp(speed / (Def.speed * .9f), .7f, 1.6f));
                else if (speed > .05f) Play("Walk", .2f, Mathf.Clamp(speed / Mathf.Max(.5f, Def.speed * .45f), .6f, 1.8f));
                else Play(idle == "Sit" ? "Idle_2" : idle, .3f);
                _anim.Update(dt);
                return;
            }
            // khoi dung bang code: nhun / nhay coc, ngoi xuong
            if (speed > .05f) { _hop += dt * (Def.hop ? 10 : 7) * Mathf.Clamp(speed / Mathf.Max(.5f, Def.speed * .5f), .6f, 1.8f); _sit = 0; }
            else { _hop = 0; _sit = Mathf.MoveTowards(_sit, idle == "Sit" ? 1 : 0, dt * 3); }
            float lift = Mathf.Abs(Mathf.Sin(_hop)) * (Def.hop ? .12f : .03f);
            Visual.localPosition = new Vector3(0, lift - _sit * .05f, 0);
            Visual.localRotation = Quaternion.Euler(-_sit * 14f + (idle == "Eating" && speed < .05f ? 12f : 0), 0, 0);
        }
    }
}
