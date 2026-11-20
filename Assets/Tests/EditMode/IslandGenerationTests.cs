// Test giai doan 1: sinh dao theo seed phai giong ban web va hop le de choi.
// Gia tri tham chieu tinh tu chinh code JS trong serene-isle.html (node).
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace SereneIsle.Tests
{
    public class IslandGenerationTests
    {
        static IslandData _d424242;
        static IslandData Island424242
        {
            get
            {
                if (_d424242 == null) { _d424242 = new IslandData(424242, 800); _d424242.Generate(); }
                return _d424242;
            }
        }

        [Test]
        public void Hash2_KhopBanWeb()
        {
            Assert.AreEqual(0.7700288979732499, IslandNoise.Hash2(0, 0, 424242), 1e-15);
            Assert.AreEqual(0.16900241192639862, IslandNoise.Hash2(123, -456, 424242), 1e-15);
            Assert.AreEqual(0.30562273000963563, IslandNoise.Hash2(-99999, 77777, 1), 1e-15);
        }

        [Test]
        public void Mulberry32_KhopBanWeb()
        {
            var r = new Mulberry32(424242 * 13 + 5);
            Assert.AreEqual(0.47649568622000515, r.Next(), 1e-15);
            Assert.AreEqual(0.583313568495214, r.Next(), 1e-15);
            Assert.AreEqual(0.22270174836739898, r.Next(), 1e-15);
        }

        [TestCase(1, 400, 86, 2)]
        [TestCase(424242, 400, 92, 2)]
        [TestCase(999999, 723, 400, 1)]
        public void ViTriThiTran_KhopBanWeb(int seed, int cx, int cz, int tdir)
        {
            var d = seed == 424242 ? Island424242 : Generate(seed);
            Assert.AreEqual(cx, d.CX, "CX");
            Assert.AreEqual(cz, d.CZ, "CZ");
            Assert.AreEqual(tdir, d.TDIR, "TDIR");
        }

        [Test]
        public void CungSeed_CungDao()
        {
            var a = Generate(2024, 300);
            var b = Generate(2024, 300);
            Assert.IsTrue(a.FH.SequenceEqual(b.FH), "do cao phai giong nhau");
            Assert.IsTrue(a.Bio.SequenceEqual(b.Bio), "biome phai giong nhau");
        }

        [Test]
        public void DaoCoDuBiome_VaBienBaoQuanh()
        {
            var d = Island424242;
            int n = d.N * d.N;
            var count = new int[11];
            int ocean = 0, fresh = 0;
            for (int k = 0; k < n; k++) { count[(int)d.Bio[k]]++; ocean += d.Ocean[k]; fresh += d.Fresh[k]; }
            for (int b = 0; b < 11; b++)
                Assert.Greater(count[b], 0, $"thieu biome {(Biome)b}");
            Assert.Greater(ocean, n * .3, "bien man phai bao quanh dao");
            Assert.Greater(fresh, 0, "phai co song/ho nuoc ngot");
            // 4 goc ban do la bien
            Assert.AreEqual(1, d.Ocean[d.Idx(0, 0)]);
            Assert.AreEqual(1, d.Ocean[d.Idx(d.N - 1, d.N - 1)]);
        }

        [Test]
        public void DiemCapBen_TrenCauTau_DiDuocVaoThiTran()
        {
            var d = Island424242;
            var p = d.ArrivalPoint(out float face);
            Assert.IsTrue(d.IsPier(p.x, p.z), "diem den phai tren cau tau");
            Assert.IsTrue(d.Walkable(p.x, p.z, p.y), "diem den phai dung duoc");
            // di thang ve tam thi tran
            float x = p.x, z = p.z, y = p.y;
            for (int s = 0; s < 400; s++)
            {
                float dx = d.CX - x, dz = d.CZ - z, l = Mathf.Sqrt(dx * dx + dz * dz);
                if (l < 1f) break;
                d.MoveEntity(ref x, ref z, dx / l * .06f, dz / l * .06f, .28f, y);
                y = d.GroundHeight(x, z);
            }
            Assert.Less(Vector2.Distance(new Vector2(x, z), new Vector2(d.CX, d.CZ)), 1.5f, "bi chan tren duong tu cau tau vao thi tran");
            Assert.AreEqual(Biome.Town, d.Bio[d.Idx(d.CX, d.CZ)]);
        }

        [Test]
        public void KhongDiXuongBienMan()
        {
            var d = Island424242;
            // o (0,0) la bien sau: khong dung duoc du dang o do cao nao
            Assert.IsFalse(d.Walkable(0, 0, 0));
            Assert.IsFalse(d.Walkable(-5, -5, 0), "ngoai ban do");
        }

        [Test]
        public void KhongLeoBacCaoHon042()
        {
            var d = Island424242;
            float h = d.GroundHeight(d.CX, d.CZ);
            Assert.IsTrue(d.Walkable(d.CX, d.CZ, h));
            Assert.IsFalse(d.Walkable(d.CX, d.CZ, h - .5f), "buoc len .5 phai bi chan");
        }

        static IslandData Generate(int seed, int size = 800)
        {
            var d = new IslandData(seed, size);
            d.Generate();
            return d;
        }
    }
}
