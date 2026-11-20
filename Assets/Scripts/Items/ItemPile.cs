// Dong do roi duoi dat khi tui day (ban web: dropPile / pileUpdate). Di qua (< 1 o) la nhat neu tui con cho.
using System.Collections.Generic;
using UnityEngine;

namespace SereneIsle
{
    public class ItemPile : MonoBehaviour
    {
        public string itemId;
        public int count;
        bool _wait = true;   // vua roi: phai di ra xa > 1.6 roi quay lai moi nhat
        Transform _icon;
        float _phase;

        static readonly List<ItemPile> _all = new List<ItemPile>();
        public static IReadOnlyList<ItemPile> All => _all;

        void OnEnable() { _all.Add(this); }
        void OnDisable() { _all.Remove(this); }

        public static ItemPile Spawn(string id, int n, Vector3 near)
        {
            // gop vao dong cung loai gan do
            foreach (var p in _all)
                if (p && p.itemId == id && Vector2.Distance(new Vector2(p.transform.position.x, p.transform.position.z), new Vector2(near.x, near.z)) < 1.2f)
                { p.count += n; p._wait = true; return p; }

            float a = Random.value * Mathf.PI * 2;
            float x = near.x + Mathf.Cos(a) * .6f, z = near.z + Mathf.Sin(a) * .6f;
            var data = IslandTerrain.Instance ? IslandTerrain.Instance.Data : null;
            float y = data != null ? data.GroundHeight(x, z) : near.y;

            var go = new GameObject("Pile_" + id);
            go.transform.position = new Vector3(x, y, z);
            var sack = new GameObject("sack");
            sack.transform.SetParent(go.transform, false);
            sack.transform.localPosition = new Vector3(0, .13f, 0);
            sack.transform.localScale = new Vector3(.4f, .3f, .4f);
            sack.AddComponent<MeshFilter>().sharedMesh = ProceduralMeshes.Sphere;
            sack.AddComponent<MeshRenderer>().sharedMaterial = MaterialLibrary.Solid("#b08a55");

            var def = ItemDatabase.Instance.Get(id);
            var icon = new GameObject("icon");
            icon.transform.SetParent(go.transform, false);
            icon.transform.localScale = Vector3.one * .2f;
            icon.AddComponent<MeshFilter>().sharedMesh = ProceduralMeshes.Sphere;
            icon.AddComponent<MeshRenderer>().sharedMaterial = MaterialLibrary.Solid(def ? def.iconColor : Color.white);

            var pile = go.AddComponent<ItemPile>();
            pile.itemId = id; pile.count = n; pile._icon = icon.transform; pile._phase = Random.value * 6f;
            return pile;
        }

        void Update()
        {
            if (_icon) _icon.localPosition = new Vector3(0, .55f + Mathf.Sin(Time.time * 3 + _phase) * .05f, 0);
            var s = GameSession.Instance;
            if (!s || !s.Player || !s.Active) return;
            Vector3 pp = s.Player.transform.position, me = transform.position;
            float d = Vector2.Distance(new Vector2(pp.x, pp.z), new Vector2(me.x, me.z));
            if (_wait) { if (d > 1.6f) _wait = false; return; }
            if (d >= 1f) return;
            int got = s.Inventory.Give(itemId, count);
            if (got > 0)
            {
                count -= got;
                s.Say($"+{got} {ItemDatabase.Instance.NameOf(itemId)}");
            }
            if (count <= 0) { Destroy(gameObject); return; }
            _wait = true;
            s.Say("Túi đầy, chưa nhặt hết được.");
        }
    }
}
