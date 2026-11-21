// Suc song vung (Docs/ThietKeGameplay.md muc 5).
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace SereneIsle.Tests
{
    public class RestorationTests
    {
        static IslandData _d;
        static IslandData D
        {
            get
            {
                if (_d == null) { _d = new IslandData(424242, 300); _d.Generate(); }
                return _d;
            }
        }

        [Test]
        public void Vung_CoTen_KhongTrung_CoXomChai_SucSong20Den40()
        {
            var m = new RestorationModel(D);
            Debug.Log("[SereneIsle] vung: " + string.Join(", ", m.Regions.Select(r => $"{r.name} ({r.land} o, {r.vitality:0})")));
            Assert.GreaterOrEqual(m.Regions.Count, 3);
            Assert.AreEqual(m.Regions.Count, m.Regions.Select(r => r.name).Distinct().Count(), "ten vung trung");
            Assert.IsTrue(m.Regions.Any(r => r.name == "Xóm Chài"));
            Assert.AreEqual("Xóm Chài", m.RegionAt(D.CX, D.CZ).name, "thi tran o Xom Chai");
            foreach (var r in m.Regions) Assert.That(r.vitality, Is.InRange(20f, 40f), r.name);
            Assert.IsNull(m.RegionAt(0, 0), "goc ban do la bien");
            Assert.IsNull(m.RegionAt(-5, 3), "ngoai ban do");
            // cung seed -> cung ket qua
            var m2 = new RestorationModel(D);
            CollectionAssert.AreEqual(m.Snapshot(), m2.Snapshot());
        }

        [Test]
        public void SucSong_VuotNguong_PhaiMau_MocLai()
        {
            var m = new RestorationModel(D);
            var r = m.Regions[0];
            r.vitality = 22;
            Assert.AreEqual(0, m.Add(r, 2));
            Assert.AreEqual(25, m.Add(r, 3), "vuot 25");
            Assert.AreEqual(0, m.Add(r, -10));
            Assert.AreEqual(50, m.Add(r, 40), "nhay qua 25 va 50: bao nguong cao nhat");
            m.Add(r, 500);
            Assert.AreEqual(100, r.vitality, "toi da 100");
            m.Add(r, -500);
            Assert.AreEqual(0, r.vitality, "toi thieu 0");

            Assert.AreEqual(.45f, RestorationModel.Desaturation(0), 1e-4f);
            Assert.AreEqual(0f, RestorationModel.Desaturation(50), 1e-4f);
            Assert.Greater(RestorationModel.Desaturation(20), RestorationModel.Desaturation(40));
            Assert.AreEqual(2, RestorationModel.RegrowFactor(30));
            Assert.AreEqual(1, RestorationModel.RegrowFactor(60));
        }

        [Test]
        public void SucSong_LuuVaTai()
        {
            var m = new RestorationModel(D);
            foreach (var r in m.Regions) m.Add(r, 7);
            var snap = m.Snapshot();
            float avg = m.IslandVitality;
            var m2 = new RestorationModel(D);
            m2.Restore(snap);
            CollectionAssert.AreEqual(snap, m2.Snapshot());
            Assert.AreEqual(avg, m2.IslandVitality, 1e-4f);
            m2.Restore(new float[1]);          // sai so vung (dao khac): bo qua
            CollectionAssert.AreEqual(snap, m2.Snapshot());
        }
    }
}
