// Quan dao nho (Docs/ThietKeGameplay.md muc 7.2).
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace SereneIsle.Tests
{
    public class IsletTests
    {
        static IslandData _d;
        static List<WorldObjects.Placement> _objs;
        static int _objsBefore;
        static List<Islet> _isl;

        static void Ensure()
        {
            if (_d != null) return;
            _d = new IslandData(424242, 800);
            _d.Generate();
            _objs = WorldObjects.Place(_d);
            _objsBefore = _objs.Count;
            _isl = Islets.Carve(_d, _objs);
            Debug.Log("[SereneIsle] dao nho: " + string.Join(", ", _isl.Select(i => $"{i.name} ({i.ci},{i.cj}) {i.land} o")));
        }

        [Test]
        public void BonHonDao_CoDatLien_CachRoiDaoChinh()
        {
            Ensure();
            var d = _d;
            Assert.AreEqual(4, _isl.Count);
            CollectionAssert.AllItemsAreUnique(_isl.Select(i => i.name));
            Assert.AreSame(_isl, d.Islets);
            foreach (var isl in _isl)
            {
                Assert.GreaterOrEqual(isl.land, 15, isl.name + " qua nho");
                int c = d.Idx(isl.ci, isl.cj);
                Assert.Greater(d.Level[c], 0, isl.name + ": tam la dat lien");
                Assert.AreEqual(0, d.Ocean[c]);
                // khong noi bang dat lien toi thi tran (chi ra bang ghe hoac con cat luc nuoc rong)
                var seen = new HashSet<int> { c };
                var stack = new Stack<int>(); stack.Push(c);
                int[] DX = { 1, -1, 0, 0 }, DZ = { 0, 0, 1, -1 };
                while (stack.Count > 0 && seen.Count < 3000)
                {
                    int k = stack.Pop();
                    for (int n = 0; n < 4; n++)
                    {
                        int x = k % d.N + DX[n], z = k / d.N + DZ[n];
                        if (!d.InBounds(x, z)) continue;
                        int m = d.Idx(x, z);
                        if (d.Level[m] > 0 && seen.Add(m)) stack.Push(m);
                    }
                }
                Assert.Less(seen.Count, 3000, isl.name + " dinh lien dao chinh");
                Assert.IsFalse(seen.Contains(d.Idx(d.CX, d.CZ)));
            }
            for (int a = 0; a < _isl.Count; a++)
                for (int b = a + 1; b < _isl.Count; b++)
                    Assert.GreaterOrEqual(Vector2.Distance(new Vector2(_isl[a].ci, _isl[a].cj), new Vector2(_isl[b].ci, _isl[b].cj)), 70f);
        }

        [Test]
        public void HonRua_ConCat_ChiLoKhiNuocRongKiet()
        {
            Ensure();
            var d = _d;
            var t = _isl.First(i => i.kind == IsletKind.Turtle);
            Assert.GreaterOrEqual(t.barFrom, 0, "co con cat");
            Assert.GreaterOrEqual(t.barTo, 0);
            // doc theo con cat: lien mach (khong cho nao sau hon BarHeight), co doan duoc nang len dung BarHeight
            float x0 = t.barFrom % d.N, z0 = t.barFrom / d.N, x = 0, z = 0;
            bool raised = false;
            for (float s = 0; s <= 1; s += .01f)
            {
                float px = Mathf.Lerp(x0, t.ci, s), pz = Mathf.Lerp(z0, t.cj, s);
                int k = d.Idx(Mathf.RoundToInt(px), Mathf.RoundToInt(pz));
                Assert.GreaterOrEqual(d.HT[k], Islets.BarHeight - .001f, $"con cat dut tai ({px:0},{pz:0})");
                if (!raised && Mathf.Abs(d.HT[k] - Islets.BarHeight) < .001f) { raised = true; x = Mathf.RoundToInt(px); z = Mathf.RoundToInt(pz); }
            }
            Assert.IsTrue(raised, "co doan con cat duoc dap len");
            d.WaterLevel = IslandData.SEA - TideClock.SpringAmp;
            Assert.IsTrue(d.Walkable(x, z, d.HeightAt(x, z)), "nuoc rong kiet: di bo duoc");
            d.WaterLevel = IslandData.SEA;
            Assert.IsFalse(d.Walkable(x, z, d.HeightAt(x, z)), "nuoc trung binh: qua sau");
            d.WaterLevel = IslandData.SEA;
        }

        [Test]
        public void CayDa_TrenHonDao_DaoChinhGiuNguyen()
        {
            Ensure();
            var d = _d;
            Assert.Greater(_objs.Count, _objsBefore, "them cay / da tren hon dao");
            var added = _objs.Skip(_objsBefore).ToList();
            foreach (var p in added)
            {
                Assert.Greater(d.Level[p.idx], 0, "vat the tren dat");
                Assert.AreEqual((byte)(p.type + 1), d.ObjType[p.idx]);
                Assert.IsTrue(_isl.Any(i => Vector2.Distance(new Vector2(p.idx % d.N, p.idx / d.N), new Vector2(i.ci, i.cj)) <= i.radius + 3));
            }
            var coco = _isl.First(i => i.kind == IsletKind.Coconut);
            Assert.IsTrue(added.Any(p => p.type == WorldObjectType.Palm && Vector2.Distance(new Vector2(p.idx % d.N, p.idx / d.N), new Vector2(coco.ci, coco.cj)) < 12), "Hon Dua co dua");
            Assert.GreaterOrEqual(_isl.First(i => i.kind == IsletKind.Rock).featureTile, 0, "Hon Da co cho dat ruong");
        }
    }
}
