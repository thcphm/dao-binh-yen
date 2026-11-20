// Thanh nhanh 10 o (ban web: HOTBAR / G.hb / hbAssign / hbClear / keo tha doi cho).
// Moi o chua id dung cu (axe...) hoac id do an/uong, hoac "" (trong).
using System;

namespace SereneIsle
{
    public sealed class Hotbar
    {
        public const int Size = 10;

        /// <summary>Ban web HB_DEF: 9 dung cu + 1 o trong.</summary>
        public static string[] DefaultLayout() =>
            new[] { "axe", "shovel", "hoe", "can", "seed", "pick", "rod", "net", "build", "" };

        readonly string[] _slots = new string[Size];
        int _selected;

        public event Action Changed;

        public Hotbar() { Load(DefaultLayout()); }

        public void Load(string[] layout)
        {
            for (int i = 0; i < Size; i++) _slots[i] = layout != null && i < layout.Length && layout[i] != null ? layout[i] : "";
            _selected = Math.Max(0, Math.Min(Size - 1, _selected));
            Changed?.Invoke();
        }

        public string this[int i] => i >= 0 && i < Size ? _slots[i] : "";
        public string[] ToArray() => (string[])_slots.Clone();

        public int Selected
        {
            get => _selected;
            set { int v = ((value % Size) + Size) % Size; if (v != _selected) { _selected = v; Changed?.Invoke(); } }
        }
        public string SelectedId => _slots[_selected];

        public int IndexOf(string id)
        {
            if (string.IsNullOrEmpty(id)) return -1;
            return Array.IndexOf(_slots, id);
        }

        /// <summary>hbAssign: gan id vao o; neu id da o cho khac thi doi cho 2 o. O dang chon di theo mon dang cam.</summary>
        public void Assign(int slot, string id)
        {
            if (slot < 0 || slot >= Size) return;
            string cur = _slots[_selected];
            int old = IndexOf(id);
            if (old >= 0) _slots[old] = _slots[slot];
            _slots[slot] = id ?? "";
            int ns = IndexOf(cur);
            _selected = ns >= 0 ? ns : slot;
            Changed?.Invoke();
        }

        /// <summary>Keo tha giua 2 o tren thanh.</summary>
        public void Swap(int a, int b)
        {
            if (a == b || a < 0 || b < 0 || a >= Size || b >= Size) return;
            string cur = _slots[_selected];
            (_slots[a], _slots[b]) = (_slots[b], _slots[a]);
            int ns = IndexOf(cur);
            if (ns >= 0) _selected = ns;
            Changed?.Invoke();
        }

        /// <summary>Chuot phai: xoa o.</summary>
        public void Clear(int slot)
        {
            if (slot < 0 || slot >= Size || _slots[slot] == "") return;
            _slots[slot] = "";
            Changed?.Invoke();
        }
    }
}
