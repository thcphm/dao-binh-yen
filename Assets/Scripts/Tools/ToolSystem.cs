// Dung dung cu (ban web: useTool dong 1392-1434): riu chat cay, cuoc chim dap da, xeng dao dat, can cau.
// + tuong tac phim E: leu (di ngu), ban che tao. Cay da chat se moc lai sau vai ngay (thiet ke gameplay muc 5).
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SereneIsle
{
    [DefaultExecutionOrder(-40)]
    public class ToolSystem : MonoBehaviour
    {
        public static ToolSystem Instance { get; private set; }

        [Serializable]
        public struct Removed { public int idx; public int type; public int regrowDay; }

        /// <summary>Vat the da chat / dap, cho moc lai (luu vao file).</summary>
        public readonly List<Removed> RemovedObjects = new List<Removed>();
        /// <summary>Cap dung cu (ban web: G.lv) - nang cap o lo ren (giai doan 4).</summary>
        public readonly Dictionary<string, int> Levels = new Dictionary<string, int> { ["axe"] = 1, ["pick"] = 1, ["can"] = 1, ["rod"] = 1, ["net"] = 1 };

        public Fishing Fishing { get; private set; }
        /// <summary>Dang vung dung cu, chua toi luc trung (atImpact).</summary>
        public bool Busy => _busy;
        public Vector3 BenchPosition { get; set; }
        public bool HasBench { get; set; }

        readonly Dictionary<int, int> _hp = new Dictionary<int, int>();
        readonly Dictionary<int, int> _nuts = new Dictionary<int, int>();
        readonly System.Random _rng = new System.Random();
        TileCursor _cursor;
        bool _busy;

        GameSession S => GameSession.Instance;
        IslandData D => IslandTerrain.Instance ? IslandTerrain.Instance.Data : null;
        VegetationRenderer V => IslandTerrain.Instance ? IslandTerrain.Instance.Vegetation : null;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { Instance = null; }

        void Awake()
        {
            Instance = this;
            Fishing = gameObject.AddComponent<Fishing>();
            if (!Critters.Instance) gameObject.AddComponent<Critters>();
            _cursor = TileCursor.Create();
            _cursor.transform.SetParent(transform, false);
        }

        void OnDestroy() { if (Instance == this) Instance = null; }

        static readonly HashSet<string> TargetTools = new HashSet<string> { "axe", "pick", "shovel", "hoe", "can", "seed" };

        int Lv(string tool) => Levels.TryGetValue(tool, out var l) ? l : 1;

        // ------------------------------------------------------------------ moi khung
        void Update()
        {
            var s = S; var d = D;
            if (!s || d == null || !s.Running) { _cursor.Hide(); return; }
            Fishing.RodLevel = Lv("rod");
            if (Fishing.Busy && GameInput.CancelPressed) Fishing.Cancel();

            string tool = s.Hotbar.SelectedId;
            if (s.Body) s.Body.SetTool(tool != null && s.OwnedTools.Contains(tool) ? tool : null, tool != null ? Lv(tool) : 1);
            int k = TargetIdx();
            if (s.Active && !Fishing.Busy && k >= 0 && tool != null && TargetTools.Contains(tool) && s.OwnedTools.Contains(tool))
            {
                int i = k % d.N, j = k / d.N;
                _cursor.Show(new Vector3(i, d.HT[k], j), CanUseOn(tool, k));
            }
            else _cursor.Hide();

            // phim E: leu / ban che tao
            if (s.Active && !Fishing.Busy)
            {
                var act = NearbyAction(out string label);
                ActionUI.Prompt(act != null ? label : null);
                if (act != null && GameInput.InteractPressed) act();
            }
            else ActionUI.Prompt(null);
        }

        /// <summary>targetIdx: o ngay truoc mat (.85 o).</summary>
        public int TargetIdx()
        {
            var s = S; var d = D;
            if (!s || !s.Player || d == null) return -1;
            var p = s.Player.transform.position;
            float r = s.Player.Facing;
            int i = Mathf.FloorToInt(p.x + Mathf.Sin(r) * .85f + .5f), j = Mathf.FloorToInt(p.z + Mathf.Cos(r) * .85f + .5f);
            return i >= 0 && j >= 0 && i < d.N && j < d.N ? j * d.N + i : -1;
        }

        bool CanUseOn(string tool, int k)
        {
            var d = D;
            int o = d.ObjType[k];
            switch (tool)
            {
                case "axe": case "pick": return o != 0 && WorldObjects.Info((WorldObjectType)(o - 1)).tool == tool;
                case "shovel": return d.Level[k] != 0 && d.Solid[k] == 0;
                case "hoe": case "can": case "seed":
                    var s = S;
                    return s.Farm && s.Farm.HintFor(s.Items.Get(tool)) != null; // FarmSystem biet o nao cuoc/tuoi/gieo duoc
                default: return false;
            }
        }

        Action NearbyAction(out string label)
        {
            var s = S;
            var p = s.Player.transform.position;
            label = null;
            if (s.HasHome && Flat(p, s.HomePosition) < 2.2f) { label = "E: Đi ngủ"; return () => ActionUI.Instance.AskSleep(); }
            if (HasBench && Flat(p, BenchPosition) < 1.9f) { label = "E: Bàn chế tạo"; return () => ActionUI.Instance.OpenCrafting(); }
            return null;
        }

        static float Flat(Vector3 a, Vector3 b) => Vector2.Distance(new Vector2(a.x, a.z), new Vector2(b.x, b.z));

        // ------------------------------------------------------------------ dung dung cu
        /// <summary>Goi tu GameSession.UseSelected khi o dang chon la dung cu da co.</summary>
        public void Use(string tool)
        {
            var s = S; var d = D;
            if (d == null || _busy) return;
            if (tool == "rod") { Fishing.Press(); return; }
            if (s.Body && s.Body.Acting) return;
            int k = TargetIdx();
            switch (tool)
            {
                case "axe": case "pick": UseAxePick(tool, k); return;
                case "shovel": UseShovel(k); return;
                case "hoe": case "can": case "seed": s.Body.PlayAct(tool); return; // vuon ruong (FarmSystem) da xu ly truoc; den day = o khong hop le
                case "net": UseNet(); return;
                default: s.Say($"{s.Items.NameOf(tool)}: chưa dùng được."); return;
            }
        }

        IEnumerator AtImpact(string act, float at, Action f)
        {
            _busy = true;
            yield return new WaitForSeconds(CharacterActions.DurationOf(act) * at);
            _busy = false;
            f();
        }

        void UseAxePick(string tool, int k)
        {
            var s = S; var d = D;
            s.Body.PlayAct(tool);
            if (k < 0 || d.ObjType[k] == 0) return;
            var type = (WorldObjectType)(d.ObjType[k] - 1);
            var info = WorldObjects.Info(type);
            if (!s.Stats.UseEnergy(2)) return;
            if (info.tool != tool)
            {
                StartCoroutine(AtImpact(tool, .64f, () =>
                {
                    s.Say(tool == "axe" ? "Cần cuốc chim để đập đá." : "Cần rìu để chặt.");
                    V?.Shake(k, .2f);
                }));
                return;
            }
            StartCoroutine(AtImpact(tool, .64f, () => Hit(k, type, tool)));
        }

        /// <summary>Danh trung vat the: tru mau, rung, dua roi; het mau thi do va roi do.</summary>
        public void Hit(int k, WorldObjectType type, string tool)
        {
            var s = S; var d = D;
            if (d.ObjType[k] != (byte)(type + 1)) return;
            var info = WorldObjects.Info(type);
            int i = k % d.N, j = k / d.N;
            float y = d.HT[k];
            int hp = (_hp.TryGetValue(k, out var h) ? h : info.hp) - Lv(tool);
            _hp[k] = hp;
            V?.Shake(k);
            if (type == WorldObjectType.Palm)
            {
                int nuts = _nuts.TryGetValue(k, out var nn) ? nn : 3;
                int n = hp <= 0 ? nuts : (nuts > 0 && _rng.NextDouble() < .4 ? 1 : 0);
                for (int q = 0; q < n; q++)
                    FallingItem.Spawn("coconut", new Vector3(i + (float)(_rng.NextDouble() - .5) * .8f, y + 3.1f + q * .3f, j + (float)(_rng.NextDouble() - .5) * .8f));
                _nuts[k] = nuts - n;
            }
            var fx = MaterialLibrary.Hex(info.fx);
            Debris.Burst(new Vector3(i, y + (tool == "axe" ? .9f : .4f), j), fx, 6);
            if (hp > 0) return;

            // do / vo
            var drops = WorldObjects.Drops(type, _rng);
            RemoveObject(k, type);
            Debris.Burst(new Vector3(i, y + .8f, j), fx, 16, 4);
            float ty = s.Player.transform.position.y + 2;
            foreach (var (id, n) in drops)
            {
                s.Give(id, n, true);
                ActionUI.Float($"+{n} {s.Items.NameOf(id)}", new Vector3(i, ty, j));
                ty += .45f;
            }
        }

        /// <summary>Bo vat the khoi dao, hen ngay moc lai.</summary>
        public void RemoveObject(int k, WorldObjectType type, int regrowDay = -1)
        {
            var d = D;
            d.ObjType[k] = 0; d.Solid[k] = 0;
            V?.RemoveObject(k, type);
            _hp.Remove(k); _nuts.Remove(k);
            if (regrowDay < 0) regrowDay = S.Clock.Day + WorldObjects.RegrowDays(type, _rng);
            RemovedObjects.Add(new Removed { idx = k, type = (int)type, regrowDay = regrowDay });
        }

        /// <summary>Sang ngay moi: cay / da den han thi moc lai (neu o con trong va nguoi choi khong dung do).</summary>
        public int Regrow(int day)
        {
            var d = D; var s = S;
            if (d == null) return 0;
            int n = 0;
            for (int q = RemovedObjects.Count - 1; q >= 0; q--)
            {
                var r = RemovedObjects[q];
                if (r.regrowDay > day) continue;
                int i = r.idx % d.N, j = r.idx / d.N;
                if (d.ObjType[r.idx] != 0 || d.Solid[r.idx] != 0) { RemovedObjects.RemoveAt(q); continue; }
                if (s && s.Player)
                {
                    var p = s.Player.transform.position;
                    if (Mathf.Abs(p.x - i) < 1.2f && Mathf.Abs(p.z - j) < 1.2f) continue; // dang dung tren o: de hom sau
                }
                d.ObjType[r.idx] = (byte)(r.type + 1); d.Solid[r.idx] = 1;
                V?.AddObject(r.idx, (WorldObjectType)r.type, d.ObjRot[r.idx], d.ObjScale[r.idx]);
                RemovedObjects.RemoveAt(q);
                n++;
            }
            return n;
        }

        /// <summary>Tai game: bo lai cac vat the da chat (khong hen lai ngay).</summary>
        public void RestoreRemoved(IEnumerable<Removed> list)
        {
            var d = D;
            foreach (var r in list)
            {
                if (r.idx < 0 || r.idx >= d.ObjType.Length) continue;
                if (d.ObjType[r.idx] == (byte)(r.type + 1)) RemoveObject(r.idx, (WorldObjectType)r.type, r.regrowDay);
                else RemovedObjects.Add(r);
            }
        }

        /// <summary>Vot (ban web dong 1427-1429): bat con trung gan nhat phia truoc, tam 1.9 o (vot tot: 2.6).</summary>
        void UseNet()
        {
            var s = S;
            s.Body.PlayAct("net");
            if (!s.Stats.UseEnergy(1)) return;
            var c = Critters.Instance;
            if (!c) return;
            var best = c.Nearest(s.Player.transform.position, s.Player.Facing, Lv("net") > 1 ? 2.6f : 1.9f);
            if (best == null) return;
            string id = best.id;
            var at = best.Position;
            c.Remove(best);
            s.Give(id, 1, true);
            bool first = s.Caught.Add(id);
            var def = s.Items.Get(id);
            ActionUI.Float($"{def.displayName}!", s.Player.transform.position + Vector3.up * 2.2f, new Color(.78f, 1f, .6f));
            Debris.Burst(at, def.iconColor2, 5, 1.2f);
            s.Say($"Bắt được {def.displayName} ({def.price} xu)");
            if (first) s.Say($"Mới trong sổ sưu tầm: {def.displayName}!");
        }

        void UseShovel(int k)
        {
            var s = S; var d = D;
            s.Body.PlayAct("shovel");
            if (k < 0 || d.Level[k] == 0 || d.Solid[k] != 0) return;
            if (!s.Stats.UseEnergy(2)) return;
            StartCoroutine(AtImpact("shovel", .64f, () =>
            {
                int i = k % d.N, j = k / d.N;
                bool sandy = d.Bio[k] == Biome.Beach;
                Debris.Burst(new Vector3(i, d.HT[k] + .1f, j), MaterialLibrary.Hex(sandy ? "#e6d29a" : "#8a6a44"), 8, 2);
                double r = _rng.NextDouble();
                string got = null;
                if (r < (sandy ? .15 : .36)) got = "stone";
                else if (r < (sandy ? .45 : .5)) got = sandy ? "shell" : "worm";
                else if (r < .54) got = "oldcoin";
                var at = new Vector3(i, d.HT[k] + 1, j);
                if (got != null) { s.Give(got, 1, true); ActionUI.Float($"+1 {s.Items.NameOf(got)}", at); }
                else ActionUI.Float("Chỉ toàn đất…", at, new Color(.94f, .89f, .75f));
            }));
        }
    }
}
