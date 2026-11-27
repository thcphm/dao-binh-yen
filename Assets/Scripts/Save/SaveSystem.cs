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
        // san ho da cay duoi day bien (DivingSystem) - chi so o
        public List<int> corals = new List<int>();
        // quan dao nho (IsletSystem): hon da tim thay, ruong Hon Da, da thay rua con
        public List<string> islets = new List<string>();
        public bool isletChest, turtlesSeen;
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
        // giai doan 5: ten, thoi gian choi
        public string playerName;
        public float playSeconds;
        public List<MapPin> pins = new List<MapPin>();
        // giai doan 6: so tay, chai thu, kho bau
        public List<ItemCount> stats = new List<ItemCount>();
        public List<ItemCount> miles = new List<ItemCount>();
        public int journalPages = 1, letters, bottle = -1, treasure = -1;
        // giai doan 6d: thu dat lien
        public int mailChapter, mailMonth = -1, mailSent = -1;
        public string mailTopic, mailGift;
        // khoanh khac binh yen (id, ngay gap)
        public List<ItemCount> moments = new List<ItemCount>();
        // vuon ruong nang cao: dong giong giu lai, ngay cat canh cay dau dong (id vung -> ngay)
        public List<Strain> strains = new List<Strain>();
        public List<ItemCount> motherCuts = new List<ItemCount>();
    }

    /// <summary>Tom tat 1 o luu cho man hinh chon o (giai doan 5).</summary>
    public sealed class SlotInfo
    {
        public int slot;
        public bool exists;
        public string name, dateLabel, savedAt, playTime, model;
        public int day, coins;

        public static SlotInfo From(int slot, SaveData d)
        {
            var i = new SlotInfo { slot = slot, exists = d != null };
            if (d == null) return i;
            i.day = d.day; i.coins = d.coins; i.savedAt = d.savedAt;
            i.name = string.IsNullOrEmpty(d.playerName) ? "Bạn" : d.playerName;
            var date = LunarCalendar.Date(d.day);
            i.dateLabel = $"{LunarCalendar.SeasonName(date.Season)} · {date.ShortLabel} · năm {date.Year}";
            int min = Mathf.FloorToInt(d.playSeconds / 60);
            i.playTime = min >= 60 ? $"{min / 60} giờ {min % 60} phút" : $"{min} phút";
            i.model = d.outfit != null ? d.outfit.model : null;
            return i;
        }
    }

    public static class SaveSystem
    {
        public const int Slots = 4;

#if UNITY_WEBGL && !UNITY_EDITOR
        [System.Runtime.InteropServices.DllImport("__Internal")] static extern void SereneSyncFS();
#endif
        /// <summary>WebGL: day file vua ghi xuong IndexedDB (Plugins/WebGL/SereneSync.jslib). Noi khac: khong lam gi.</summary>
        public static void Flush()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            try { SereneSyncFS(); } catch (Exception e) { Debug.LogWarning("[SereneIsle] syncfs: " + e.Message); }
#endif
        }

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

        public static SlotInfo Info(int slot) => SlotInfo.From(slot, Load(slot));

        /// <summary>O luu moi nhat (theo thoi gian ghi file), -1 neu chua co.</summary>
        public static int MostRecent()
        {
            int best = -1; DateTime t = DateTime.MinValue;
            for (int i = 0; i < Slots; i++)
            {
                if (!Exists(i)) continue;
                var w = File.GetLastWriteTimeUtc(PathOf(i));
                if (w > t) { t = w; best = i; }
            }
            return best;
        }

        /// <summary>Xoa o luu (nguoi choi da xac nhan o man hinh chon o).</summary>
        public static void Delete(int slot)
        {
            try { if (Exists(slot)) File.Delete(PathOf(slot)); Flush(); }
            catch (Exception e) { Debug.LogWarning("[SereneIsle] Khong xoa duoc o luu: " + e.Message); }
        }

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
                kitGiven = s.KitGiven,
            };
            foreach (var kv in s.Inventory.Stacks()) d.inventory.Add(new ItemCount { id = kv.Key, n = kv.Value });
            d.ownedTools.AddRange(s.OwnedTools);
            d.caught.AddRange(s.Caught);
            d.flags.AddRange(s.Flags);
            if (Village.Instance) foreach (var kv in Village.Instance.CraftLevels) d.crafts.Add(new ItemCount { id = kv.Key, n = kv.Value });
            if (Market.Instance) { d.marketRep = Market.Instance.Reputation; d.stall = Market.Instance.Snapshot(); }
            var an = Animals.Instance;
            if (an) { d.pet = an.Pet; d.farmAnimals.AddRange(an.Farm); d.seenAnimals.AddRange(an.Seen); }
            d.playerName = s.PlayerName; d.playSeconds = s.PlaySeconds;
            if (MapUI.Instance) d.pins.AddRange(MapUI.Instance.Pins);
            if (Journal.Instance) { d.stats = Journal.Instance.SnapshotStats(); d.miles = Journal.Instance.SnapshotMiles(); d.journalPages = Journal.Instance.PagesUnlocked; }
            if (Bottles.Instance) { d.bottle = Bottles.Instance.Bottle; d.treasure = Bottles.Instance.Treasure; d.letters = Bottles.Instance.LettersRead; }
            if (Moments.Instance) d.moments = Moments.Instance.Snapshot();
            if (s.Farm) d.strains = s.Farm.StrainSnapshot();
            if (MotherTrees.Instance) d.motherCuts = MotherTrees.Instance.Snapshot();
            var mail = Mail.Instance;
            if (mail) { d.mailChapter = mail.Chapter; d.mailMonth = mail.LastMonth; d.mailSent = mail.SentMonth; d.mailTopic = mail.Topic; d.mailGift = mail.Gift; }
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
            if (DivingSystem.Instance) d.corals = DivingSystem.Instance.CoralSnapshot();
            var isl = IsletSystem.Instance;
            if (isl) { d.islets = new List<string>(isl.Discovered); d.isletChest = isl.ChestOpened; d.turtlesSeen = isl.TurtlesSeen; }
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
                Flush();
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
            if (!string.IsNullOrEmpty(d.playerName)) s.PlayerName = d.playerName;
            s.PlaySeconds = d.playSeconds;
            if (MapUI.Instance) MapUI.Instance.SetPins(d.pins);
            if (Journal.Instance) Journal.Instance.Restore(d.stats, d.miles, d.journalPages);
            if (Bottles.Instance) Bottles.Instance.Restore(d.bottle, d.treasure, d.letters);
            if (Moments.Instance) Moments.Instance.Restore(d.moments);
            if (s.Farm) s.Farm.RestoreStrains(d.strains);
            if (MotherTrees.Instance) MotherTrees.Instance.Restore(d.motherCuts);
            if (Mail.Instance) Mail.Instance.Restore(d.mailChapter, d.mailMonth, d.mailSent, d.mailTopic, d.mailGift);
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
                foreach (var p in d.farm) { if (string.IsNullOrEmpty(p.crop)) p.crop = null; if (string.IsNullOrEmpty(p.deadReason)) p.deadReason = null; if (string.IsNullOrEmpty(p.graft)) p.graft = null; }
                s.Farm.Restore(d.farm);
            }
            if (s.Restoration && d.vitality != null && d.vitality.Length > 0) s.Restoration.Restore(d.vitality);
            if (DivingSystem.Instance) DivingSystem.Instance.RestoreCorals(d.corals);
            var isl = IsletSystem.Instance;
            if (isl)
            {
                isl.Discovered.Clear();
                if (d.islets != null) foreach (var n in d.islets) isl.Discovered.Add(n);
                isl.ChestOpened = d.isletChest; isl.TurtlesSeen = d.turtlesSeen;
            }
            if (s.Player) s.Player.PlaceAt(new Vector3(d.px, 0, d.pz), d.facing);
        }
    }
}
