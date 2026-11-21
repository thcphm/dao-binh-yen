// Luu / tai van choi ra JSON (ban web: serialize / loadSave, dong 1848-1851).
// Thu muc: bien moi truong SERENE_SAVE_DIR; trong Editor = <project>/Saves (o F:, git bo qua);
// ban build = Application.persistentDataPath/saves (WebGL: IndexedDB cua trinh duyet).
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace SereneIsle
{
    [Serializable]
    public struct ItemCount { public string id; public int n; }

    [Serializable]
    public class SaveData
    {
        public const int Version = 1;
        public int v = Version;
        public string savedAt;
        public int seed, size;
        public int day; public float minutes; public int coins;
        public float hp, water, energy, calm;
        public List<ItemCount> inventory = new List<ItemCount>();
        public int bagLevel;
        public string[] hotbar;
        public int selected;
        public List<string> ownedTools = new List<string>();
        public List<ItemCount> toolLevels = new List<ItemCount>();
        public List<string> caught = new List<string>();
        public float px, pz, facing;
        public List<ToolSystem.Removed> removed = new List<ToolSystem.Removed>();
        public CharacterOutfit outfit;
        public bool kitGiven;
        // vuon ruong (FarmSystem)
        public List<FarmPlot> farm = new List<FarmPlot>();
        public int canWater;
        // suc song tung vung (RestorationSystem), theo thu tu vung cua dao
        public float[] vitality;
        // do che tao da dat xuong dat (ban web: sv.pl)
        public List<PlacedItem> placed = new List<PlacedItem>();
        public string buildSel;
        public List<string> flags = new List<string>();
        // hoc nghe + cho phien (giai doan 4b)
        public List<ItemCount> crafts = new List<ItemCount>();
        public float marketRep;
        public List<StallSlot> stall = new List<StallSlot>();
        // thu (giai doan 4c)
        public PetData pet;
        public List<FarmAnimal> farmAnimals = new List<FarmAnimal>();
        public List<string> seenAnimals = new List<string>();
    }

    public static class SaveSystem
    {
        public const int Slots = 4;

        public static string Dir
        {
            get
            {
                var env = Environment.GetEnvironmentVariable("SERENE_SAVE_DIR");
                if (!string.IsNullOrEmpty(env)) return env;
                return Application.isEditor ? Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Saves"))
                                            : Path.Combine(Application.persistentDataPath, "saves");
            }
        }

        public static string PathOf(int slot) => Path.Combine(Dir, $"slot{slot}.json");
        public static bool Exists(int slot) => File.Exists(PathOf(slot));

        public static SaveData Capture(GameSession s)
        {
            var d = new SaveData
            {
                savedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm"),
                seed = IslandTerrain.Instance ? IslandTerrain.Instance.seed : -1,
                size = IslandTerrain.Instance ? IslandTerrain.Instance.size : 800,
                day = s.Clock.Day, minutes = s.Clock.Minutes, coins = s.Coins,
                hp = s.Stats.Hp, water = s.Stats.Water, energy = s.Stats.Energy, calm = s.Stats.Calm,
                bagLevel = s.Inventory.BagLevel,
                hotbar = s.Hotbar.ToArray(), selected = s.Hotbar.Selected,
                kitGiven = true,
            };
            foreach (var kv in s.Inventory.Stacks()) d.inventory.Add(new ItemCount { id = kv.Key, n = kv.Value });
            d.ownedTools.AddRange(s.OwnedTools);
            d.caught.AddRange(s.Caught);
            d.flags.AddRange(s.Flags);
            if (Village.Instance) foreach (var kv in Village.Instance.CraftLevels) d.crafts.Add(new ItemCount { id = kv.Key, n = kv.Value });
            if (Market.Instance) { d.marketRep = Market.Instance.Reputation; d.stall = Market.Instance.Snapshot(); }
            var an = Animals.Instance;
            if (an) { d.pet = an.Pet; d.farmAnimals.AddRange(an.Farm); d.seenAnimals.AddRange(an.Seen); }
            var ts = ToolSystem.Instance;
            if (ts)
            {
                foreach (var kv in ts.Levels) d.toolLevels.Add(new ItemCount { id = kv.Key, n = kv.Value });
                d.removed.AddRange(ts.RemovedObjects);
            }
            if (s.Player)
            {
                var p = s.Player.transform.position;
                d.px = p.x; d.pz = p.z; d.facing = s.Player.Facing;
            }
            if (s.Body) d.outfit = s.Body.outfit;
            if (s.Farm) { d.farm = s.Farm.Snapshot(); d.canWater = s.Farm.CanWater; }
            var pl = Placeables.Instance;
            if (pl) { d.placed = pl.Snapshot(); d.buildSel = pl.Selected; }
            if (s.Restoration) d.vitality = s.Restoration.Snapshot();
            return d;
        }

        public static bool Save(GameSession s, int slot)
        {
            try
            {
                Directory.CreateDirectory(Dir);
                var json = JsonUtility.ToJson(Capture(s), true);
                var path = PathOf(slot);
                File.WriteAllText(path + ".tmp", json);
                if (File.Exists(path)) File.Delete(path);
                File.Move(path + ".tmp", path);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogWarning("[SereneIsle] Khong luu duoc: " + e.Message);
                return false;
            }
        }

        public static SaveData Load(int slot)
        {
            try
            {
                if (!Exists(slot)) return null;
                var d = JsonUtility.FromJson<SaveData>(File.ReadAllText(PathOf(slot)));
                return d != null && d.v == SaveData.Version ? d : null;
            }
            catch (Exception e)
            {
                Debug.LogWarning("[SereneIsle] File luu hong: " + e.Message);
                return null;
            }
        }

        /// <summary>Ap dung phan khong phu thuoc dia hinh (goi truoc khi dao sinh xong).</summary>
        public static void ApplyState(SaveData d, GameSession s)
        {
            s.Clock.Day = d.day; s.Clock.Minutes = d.minutes; s.Coins = d.coins;
            s.Stats.Hp = d.hp; s.Stats.Water = d.water; s.Stats.Energy = d.energy; s.Stats.Calm = d.calm;
            s.Inventory.Clear();
            s.Inventory.BagLevel = d.bagLevel;
            foreach (var c in d.inventory) s.Inventory.Give(c.id, c.n);
            s.OwnedTools.Clear();
            foreach (var t in d.ownedTools) s.OwnedTools.Add(t);
            s.Caught.Clear();
            foreach (var c in d.caught) s.Caught.Add(c);
            s.Flags.Clear();
            if (d.flags != null) foreach (var f in d.flags) s.Flags.Add(f);
            if (Village.Instance && d.crafts != null) foreach (var c in d.crafts) Village.Instance.SetCraftLevel(c.id, c.n);
            if (Market.Instance) Market.Instance.Restore(d.stall, d.marketRep);
            if (Animals.Instance) Animals.Instance.Restore(d.pet, d.farmAnimals, d.seenAnimals);
            if (d.hotbar != null && d.hotbar.Length == Hotbar.Size) s.Hotbar.Load(d.hotbar);
            s.Hotbar.Selected = Mathf.Clamp(d.selected, 0, Hotbar.Size - 1);
            var ts = ToolSystem.Instance;
            if (ts) foreach (var c in d.toolLevels) ts.Levels[c.id] = c.n;
            if (s.Farm) s.Farm.CanWater = d.canWater;
        }

        /// <summary>Ap dung phan can dia hinh (vat the da chat, vi tri nguoi choi) - goi khi dao san sang.</summary>
        public static void ApplyWorld(SaveData d, GameSession s)
        {
            var ts = ToolSystem.Instance;
            if (ts) ts.RestoreRemoved(d.removed);
            var pl = Placeables.Instance;
            if (pl) { pl.Restore(d.placed); pl.Selected = string.IsNullOrEmpty(d.buildSel) ? null : d.buildSel; }
            if (s.Farm && d.farm != null)
            {
                // JsonUtility ghi null thanh "" -> tra lai null (o dat trong)
                foreach (var p in d.farm) { if (string.IsNullOrEmpty(p.crop)) p.crop = null; if (string.IsNullOrEmpty(p.deadReason)) p.deadReason = null; }
                s.Farm.Restore(d.farm);
            }
            if (s.Restoration && d.vitality != null && d.vitality.Length > 0) s.Restoration.Restore(d.vitality);
            if (s.Player) s.Player.PlaceAt(new Vector3(d.px, 0, d.pz), d.facing);
        }
    }
}
