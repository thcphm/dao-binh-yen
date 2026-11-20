// Hieu ung nho khi lam viec: manh vun ban ra (ban web: burst), qua roi xuong roi bay vao tui (fallItem),
// khung o muc tieu truoc mat (cursor).
using System.Collections.Generic;
using UnityEngine;

namespace SereneIsle
{
    /// <summary>Manh vun hinh hop nho, bay ra roi roi xuong, nho dan (khong can he hat).</summary>
    public class Debris : MonoBehaviour
    {
        struct Bit { public Transform t; public Vector3 v; public float life, size; }
        readonly List<Bit> _bits = new List<Bit>();
        static Debris _inst;

        static Debris Inst
        {
            get
            {
                if (!_inst) _inst = new GameObject("Debris").AddComponent<Debris>();
                return _inst;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { _inst = null; }

        /// <summary>burst(x,y,z,mau,so manh,toc do)</summary>
        public static void Burst(Vector3 at, Color color, int n = 8, float speed = 2.5f)
        {
            var d = Inst;
            var mat = MaterialLibrary.Solid(color);
            for (int q = 0; q < n; q++)
            {
                var go = new GameObject("bit");
                go.transform.SetParent(d.transform, false);
                go.transform.position = at + Random.insideUnitSphere * .15f;
                go.transform.rotation = Random.rotation;
                float s = Random.Range(.05f, .11f);
                go.transform.localScale = Vector3.one * s;
                go.AddComponent<MeshFilter>().sharedMesh = ProceduralMeshes.Primitive(PrimitiveType.Cube);
                var mr = go.AddComponent<MeshRenderer>();
                mr.sharedMaterial = mat;
                mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                var dir = Random.insideUnitSphere; dir.y = Mathf.Abs(dir.y) + .6f;
                d._bits.Add(new Bit { t = go.transform, v = dir.normalized * speed * Random.Range(.5f, 1f), life = Random.Range(.45f, .75f), size = s });
            }
        }

        void Update()
        {
            float dt = Time.deltaTime;
            for (int i = _bits.Count - 1; i >= 0; i--)
            {
                var b = _bits[i];
                b.life -= dt;
                if (b.life <= 0 || !b.t) { if (b.t) Destroy(b.t.gameObject); _bits.RemoveAt(i); continue; }
                b.v.y -= 9 * dt;
                b.t.position += b.v * dt;
                b.t.Rotate(400 * dt, 300 * dt, 0);
                b.t.localScale = Vector3.one * b.size * Mathf.Clamp01(b.life / .3f);
                _bits[i] = b;
            }
        }
    }

    /// <summary>Qua roi tu tren cay xuong, nay, roi bay ve nguoi choi va vao tui (ban web: fallItem/fallUpdate).</summary>
    public class FallingItem : MonoBehaviour
    {
        public string itemId;
        float _vy, _ground, _landed = -1;

        public static void Spawn(string id, Vector3 from)
        {
            var go = new GameObject("Fall_" + id);
            go.transform.position = from;
            var m = new GameObject("nut");
            m.transform.SetParent(go.transform, false);
            m.transform.localScale = new Vector3(.26f, .28f, .26f);
            m.AddComponent<MeshFilter>().sharedMesh = ProceduralMeshes.Sphere;
            var def = ItemDatabase.Instance.Get(id);
            m.AddComponent<MeshRenderer>().sharedMaterial = MaterialLibrary.Solid(def ? def.iconColor : Color.white);
            var f = go.AddComponent<FallingItem>();
            f.itemId = id;
            var d = IslandTerrain.Instance ? IslandTerrain.Instance.Data : null;
            f._ground = (d != null ? d.GroundHeight(from.x, from.z) : 0) + .13f;
        }

        void Update()
        {
            float dt = Time.deltaTime;
            var s = GameSession.Instance;
            if (_landed < 0)
            {
                _vy -= 16 * dt;
                var p = transform.position; p.y += _vy * dt;
                transform.Rotate(dt * 340, 0, 0);
                if (p.y <= _ground)
                {
                    p.y = _ground;
                    if (Mathf.Abs(_vy) > 3) _vy *= -.35f; else _landed = 0;
                }
                transform.position = p;
                return;
            }
            _landed += dt;
            if (_landed > .45f && s && s.Player)
            {
                var target = s.Player.transform.position + Vector3.up * .8f;
                transform.position = Vector3.Lerp(transform.position, target, Mathf.Min(1, dt * 10));
                if (_landed > .75f)
                {
                    s.Give(itemId, 1, true);
                    ActionUI.Float($"+1 {s.Items.NameOf(itemId)}", s.Player.transform.position + Vector3.up * 2f);
                    Destroy(gameObject);
                }
            }
        }
    }

    /// <summary>Khung o muc tieu (ban web: cursor) - vang khi dung duoc, do khi khong.</summary>
    public class TileCursor : MonoBehaviour
    {
        MeshRenderer _mr;
        Material _ok, _bad;

        public static TileCursor Create()
        {
            var go = new GameObject("TileCursor");
            var c = go.AddComponent<TileCursor>();
            var b = new MeshBuilder();
            const float w = .05f, h = .04f;
            b.Box("#ffffff", 0, 0, -.5f, 1 + w, h, w); b.Box("#ffffff", 0, 0, .5f, 1 + w, h, w);
            b.Box("#ffffff", -.5f, 0, 0, w, h, 1 + w); b.Box("#ffffff", .5f, 0, 0, w, h, 1 + w);
            go.AddComponent<MeshFilter>().sharedMesh = b.ToMesh("CursorFrame");
            c._mr = go.AddComponent<MeshRenderer>();
            c._mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            c._ok = MaterialLibrary.Solid("#fff2a0");
            c._bad = MaterialLibrary.Solid("#ff7a6a");
            go.SetActive(false);
            return c;
        }

        public void Show(Vector3 at, bool ok)
        {
            gameObject.SetActive(true);
            transform.position = at + Vector3.up * .05f;
            _mr.sharedMaterial = ok ? _ok : _bad;
        }

        public void Hide() { if (gameObject.activeSelf) gameObject.SetActive(false); }
    }
}
