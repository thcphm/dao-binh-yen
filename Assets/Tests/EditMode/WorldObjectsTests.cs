// Cay/da tren dao phai giong ban web (so lieu lay tu genObjects goc chay bang node, seed 424242).
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace SereneIsle.Tests
{
    public class WorldObjectsTests
    {
        static IslandData _d;
        static List<WorldObjects.Placement> _list;

        static void Ensure()
        {
            if (_d != null) return;
            _d = new IslandData(424242, 800);
            _d.Generate();
            _list = WorldObjects.Place(_d);
        }

        [Test]
        public void SoLuongTungLoai_KhopBanWeb()
        {
            Ensure();
            var want = new Dictionary<WorldObjectType, int>
            {
                [WorldObjectType.Palm] = 2040, [WorldObjectType.Rock] = 3710, [WorldObjectType.JungleTree] = 4935, [WorldObjectType.Reeds] = 8700,
                [WorldObjectType.ScrewPine] = 205, [WorldObjectType.Bush] = 6526, [WorldObjectType.Fern] = 7101, [WorldObjectType.Mangrove] = 2800,
                [WorldObjectType.Acacia] = 989, [WorldObjectType.Spin] = 5538, [WorldObjectType.RedRock] = 2623, [WorldObjectType.Mound] = 1099,
                [WorldObjectType.Gum] = 13705, [WorldObjectType.Ore] = 2233, [WorldObjectType.Baobab] = 176, [WorldObjectType.Pine] = 3392,
            };
            Assert.AreEqual(65772, _list.Count);
            foreach (var kv in want)
                Assert.AreEqual(kv.Value, _list.Count(p => p.type == kv.Key), kv.Key.ToString());
        }

        [Test]
        public void ViTriGocXoayTyLe_KhopBanWeb()
        {
            Ensure();
            var first = new (int idx, WorldObjectType t, float rot, float sc)[]
            {
                (55536, WorldObjectType.Palm, 1.155549f, 1.010996f), (56265, WorldObjectType.Palm, 3.326584f, 1.099536f),
                (56329, WorldObjectType.Palm, 3.010355f, 1.09898f), (56332, WorldObjectType.Palm, 5.152891f, .91583f),
                (56337, WorldObjectType.Rock, 1.656581f, 1.097498f),
            };
            for (int i = 0; i < first.Length; i++)
            {
                Assert.AreEqual(first[i].idx, _list[i].idx, $"#{i} idx");
                Assert.AreEqual(first[i].t, _list[i].type, $"#{i} loai");
                Assert.AreEqual(first[i].rot, _list[i].rot, 1e-4, $"#{i} goc");
                Assert.AreEqual(first[i].sc, _list[i].scale, 1e-4, $"#{i} ty le");
            }
        }

        [Test]
        public void DuaChiMocOBoBienThat_VaKhongMocTrongThiTran()
        {
            Ensure();
            foreach (var p in _list)
            {
                int i = p.idx % _d.N, j = p.idx / _d.N;
                Assert.GreaterOrEqual(IslandNoise.Hypot(i - _d.CX, j - _d.CZ), 10.5, "khong co cay trong thi tran");
                if (p.type == WorldObjectType.Palm && _d.Bio[p.idx] == Biome.Beach)
                    Assert.IsTrue(_d.Coast[p.idx] != 0 && _d.Coast[p.idx] <= 7, "dua tren bai chi o bo bien man");
                Assert.AreEqual(1, _d.Solid[p.idx]);
            }
        }

        [Test]
        public void NhayQuaDaThap_KhongQuaCayTo()
        {
            Ensure();
            var rock = _list.First(p => p.type == WorldObjectType.Rock);
            var tree = _list.First(p => p.type == WorldObjectType.Gum);
            int ri = rock.idx % _d.N, rj = rock.idx / _d.N;
            float top = _d.SolidTop(rock.idx);
            Assert.AreEqual(_d.HT[rock.idx] + .75f * rock.scale, top, 1e-4);
            Assert.IsFalse(_d.Walkable(ri, rj, _d.HT[rock.idx]), "dung duoi dat thi bi da chan");
            Assert.IsTrue(_d.Walkable(ri, rj, top + .01f), "dang nhay cao hon dinh da thi qua duoc");
            Assert.Greater(_d.SolidTop(tree.idx), 1e8f, "cay to khong nhay qua");
        }

        [Test]
        public void HinhHoc_TatCaLoaiDungDuoc()
        {
            for (int t = 0; t < WorldObjects.Count; t++)
            {
                var type = (WorldObjectType)t;
                var m = WorldObjects.BuildMesh(type);
                Assert.Greater(m.vertexCount, 12, type.ToString());
                Assert.Greater(m.bounds.size.y, .2f, type.ToString());
                if (WorldObjects.Info(type).big)
                {
                    var lod = WorldObjects.BuildLod(type, m);
                    Assert.Less(lod.vertexCount, m.vertexCount, type + " LOD phai nhe hon");
                }
            }
        }
    }
}
