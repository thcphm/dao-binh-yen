// Quan dao nho quanh dao chinh (Docs/ThietKeGameplay.md muc 7.2): 4 hon dao lam san, sinh SAU genTerrain va genObjects
// (khong doi dia hinh / vat the cua dao chinh -> cac test khop ban web giu nguyen).
//  - Hon Dua: bai cat + rung dua.  - Hon Da: da, quang, ruong co o dinh.
//  - Hon Rua: bai cat phang, rua con bo ra bien dem ram; co con cat noi vao bo, chi lo ra khi nuoc rong kiet ngay rong.
//  - Hon Yen: vach da cao, chim yen bay quanh.
// Thuan C# (chay tren thread phu cung luc sinh dao).
using System;
using System.Collections.Generic;
using static SereneIsle.IslandNoise;

namespace SereneIsle
{
    public enum IsletKind { Coconut, Rock, Turtle, Swift }

    public sealed class Islet
    {
        public string name;
        public IsletKind kind;
        public int ci, cj;            // tam (o)
        public float radius;
        public int land;              // so o dat lien
        public int barFrom = -1, barTo = -1;   // con cat (Hon Rua): o dau tren dao chinh, o cuoi sat hon dao
        public int featureTile = -1;  // ruong (Hon Da), bai rua (Hon Rua), vach yen (Hon Yen)
    }

    public static class Islets
    {
        public const float BarHeight = IslandData.SEA - .42f;   // nuoc rong kiet (SEA-.45) moi lo ra; nuoc trung binh sau .42 -> khong loi duoc

        static readonly (IsletKind kind, string name, float r, float peak)[] Kinds =
        {
            (IsletKind.Turtle, "Hòn Rùa", 6f, .32f), (IsletKind.Coconut, "Hòn Dừa", 7f, .55f),
            (IsletKind.Rock, "Hòn Đá", 5.5f, 1.5f), (IsletKind.Swift, "Hòn Yến", 5f, 2.6f),
        };

        /// <summary>Tao cac hon dao nho, ghi vao d.Islets; them cay / da vao objects (danh sach cua WorldObjects.Place).</summary>
        public static List<Islet> Carve(IslandData d, List<WorldObjects.Placement> objects)
        {
            int N = d.N;
            // ---- khoang cach toi dat lien (BFS nhieu nguon), nho o dat gan nhat ----
            var dist = new short[N * N];
            var src = new int[N * N];
            var q = new Queue<int>();
            for (int k = 0; k < N * N; k++)
            {
                if (d.Level[k] > 0) { dist[k] = 0; src[k] = k; q.Enqueue(k); }
                else dist[k] = -1;
            }
            int[] DX = { 1, -1, 0, 0 }, DZ = { 0, 0, 1, -1 };
            while (q.Count > 0)
            {
                int k = q.Dequeue(), i = k % N, j = k / N;
                if (dist[k] >= 90) continue;
                for (int n = 0; n < 4; n++)
                {
                    int x = i + DX[n], z = j + DZ[n];
                    if (x < 0 || z < 0 || x >= N || z >= N) continue;
                    int m = z * N + x;
                    if (dist[m] >= 0) continue;
                    dist[m] = (short)(dist[k] + 1); src[m] = src[k];
                    q.Enqueue(m);
                }
            }

            // ---- chon vi tri: xa bo 18-42 o, nuoc sau, cach nhau >= 70, cach thi tran >= 40 ----
            var cand = new List<(double score, int k)>();
            for (int j = 16; j < N - 16; j += 3)
                for (int i = 16; i < N - 16; i += 3)
                {
                    int k = j * N + i;
                    if (d.Ocean[k] == 0 || dist[k] < 18 || dist[k] > 42) continue;
                    if (IslandData.SEA - d.HT[k] < .8f) continue;
                    if (Hypot(i - d.CX, j - d.CZ) < 40) continue;
                    cand.Add((Hash2(i, j, d.Seed + 901), k));
                }
            cand.Sort((a, b) => a.score.CompareTo(b.score));
            var picked = new List<int>();
            foreach (var c in cand)
            {
                int ci = c.k % N, cj = c.k / N;
                bool ok = true;
                foreach (int p in picked) if (Hypot(ci - p % N, cj - p / N) < 70) { ok = false; break; }
                if (!ok) continue;
                picked.Add(c.k);
                if (picked.Count == Kinds.Length) break;
            }
            // hon gan thi tran nhat la Hon Rua (co con cat noi vao bo)
            picked.Sort((a, b) => Hypot(a % N - d.CX, a / N - d.CZ).CompareTo(Hypot(b % N - d.CX, b / N - d.CZ)));

            var result = new List<Islet>();
            for (int n = 0; n < picked.Count; n++)
            {
                var (kind, name, r, peak) = Kinds[n];
                int k = picked[n];
                var isl = new Islet { name = name, kind = kind, ci = k % N, cj = k / N, radius = r };
                Raise(d, isl, peak);
                if (kind == IsletKind.Turtle) Sandbar(d, isl, src[k]);
                Decorate(d, isl, objects);
                result.Add(isl);
            }
            d.Islets = result;
            return result;
        }

        /// <summary>Nang day bien thanh hon dao: dinh = SEA + .06 + peak, mep = them ngam SEA - .4, roi hoa vao day.</summary>
        static void Raise(IslandData d, Islet isl, float peak)
        {
            float r = isl.radius, R = r + 5;
            bool cliff = isl.kind == IsletKind.Rock || isl.kind == IsletKind.Swift;
            for (int j = (int)(isl.cj - R) - 1; j <= isl.cj + R + 1; j++)
                for (int i = (int)(isl.ci - R) - 1; i <= isl.ci + R + 1; i++)
                {
                    if (!d.InBounds(i, j)) continue;
                    double ang = Math.Atan2(j - isl.cj, i - isl.ci);
                    double wob = 1 + .22 * (Fbm(Math.Cos(ang) * 1.7 + isl.ci * .01, Math.Sin(ang) * 1.7 + isl.cj * .01, d.Seed + 77 + (int)isl.kind) - .5);
                    double t = Hypot(i - isl.ci, j - isl.cj) / (r * wob);
                    float shelf = IslandData.SEA - .4f;
                    int vn = d.VIdx(i, j);
                    float orig = d.FH[vn], h;
                    if (t <= 1)
                    {
                        double f = 1 - t;
                        double shape = cliff ? Smooth(0, .35, f) * (.75 + .25 * f) : Math.Pow(f, .55);
                        h = (float)(shelf + (peak + .46) * shape);
                    }
                    else
                    {
                        double u = Math.Min(1, (t - 1) / ((R / r) - 1));
                        h = (float)(shelf + (orig - shelf) * Smooth(0, 1, u));
                    }
                    if (h <= orig) continue;
                    d.FH[vn] = h;
                    SetTile(d, i, j, cliff);
                    if (d.Level[d.Idx(i, j)] > 0) isl.land++;
                }
        }

        static void SetTile(IslandData d, int i, int j, bool cliff)
        {
            int k = d.Idx(i, j);
            float h = d.FH[d.VIdx(i, j)];
            d.HT[k] = h;
            byte l = (byte)(h < IslandData.SEA + .04f ? 0 : h < IslandData.SEA + .45f ? 1 : h < 2.2f ? 2 : h < 3.5f ? 3 : 4);
            d.Level[k] = l;
            d.Bio[k] = l == 0 ? Biome.Sea : l == 1 ? Biome.Beach : cliff ? Biome.Alpine : Biome.Tropics;
            d.Ocean[k] = (byte)(l == 0 ? 1 : 0);
            d.Coast[k] = (byte)(l == 0 ? 1 : 2);
            d.Fresh[k] = 0; d.Dirt[k] = 0; d.Rough[k] = 0;
        }

        /// <summary>Con cat thang tu bo dao chinh toi hon dao, rong 3 o, cao SEA - .42.</summary>
        static void Sandbar(IslandData d, Islet isl, int landTile)
        {
            int N = d.N;
            float x0 = landTile % N, z0 = landTile / N, x1 = isl.ci, z1 = isl.cj;
            float len = (float)Hypot(x1 - x0, z1 - z0);
            if (len < 1) return;
            isl.barFrom = landTile;
            for (float s = 0; s <= len; s += .5f)
            {
                float x = x0 + (x1 - x0) * s / len, z = z0 + (z1 - z0) * s / len;
                for (int dz = -1; dz <= 1; dz++)
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        int i = (int)Math.Round(x) + dx, j = (int)Math.Round(z) + dz;
                        if (!d.InBounds(i, j)) continue;
                        int vn = d.VIdx(i, j);
                        if (d.FH[vn] >= BarHeight) continue;
                        d.FH[vn] = BarHeight;
                        SetTile(d, i, j, false);
                        isl.barTo = d.Idx(i, j);
                    }
            }
        }

        static void Decorate(IslandData d, Islet isl, List<WorldObjects.Placement> objects)
        {
            var rng = new Random(d.Seed * 31 + (int)isl.kind * 7919);
            var land = new List<int>();
            for (int j = (int)(isl.cj - isl.radius - 2); j <= isl.cj + isl.radius + 2; j++)
                for (int i = (int)(isl.ci - isl.radius - 2); i <= isl.ci + isl.radius + 2; i++)
                    if (d.InBounds(i, j) && d.Level[d.Idx(i, j)] > 0) land.Add(d.Idx(i, j));
            if (land.Count == 0) return;
            // diem dac biet: o cao nhat (ruong, vach yen) / o cat giua dao (bai rua)
            int top = land[0];
            foreach (int k in land) if (d.HT[k] > d.HT[top]) top = k;
            isl.featureTile = top;

            (WorldObjectType t, int n, float s0, float s1)[] plan = isl.kind switch
            {
                IsletKind.Coconut => new[] { (WorldObjectType.Palm, 9, .9f, 1.25f), (WorldObjectType.Bush, 3, .8f, 1f) },
                IsletKind.Rock => new[] { (WorldObjectType.Rock, 6, .9f, 1.4f), (WorldObjectType.Ore, 3, .9f, 1.1f), (WorldObjectType.RedRock, 2, 1f, 1.3f) },
                IsletKind.Swift => new[] { (WorldObjectType.Rock, 7, 1.4f, 2.1f), (WorldObjectType.Fern, 3, .9f, 1.1f) },
                _ => new[] { (WorldObjectType.ScrewPine, 2, .9f, 1.1f), (WorldObjectType.Reeds, 3, .8f, 1f) },
            };
            foreach (var (t, n, s0, s1) in plan)
                for (int c = 0, tries = 0; c < n && tries < 80; tries++)
                {
                    int k = land[rng.Next(land.Count)];
                    if (k == top || d.ObjType[k] != 0) continue;
                    if (t == WorldObjectType.Palm && d.Level[k] > 2) continue;
                    float sc = s0 + (float)rng.NextDouble() * (s1 - s0), ro = (float)(rng.NextDouble() * Math.PI * 2);
                    d.ObjType[k] = (byte)(t + 1); d.ObjScale[k] = sc; d.ObjRot[k] = ro; d.Solid[k] = 1;
                    objects?.Add(new WorldObjects.Placement { idx = k, type = t, rot = ro, scale = sc });
                    c++;
                }
        }
    }
}
