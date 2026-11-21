// Ve bieu tuong vector bang Painter2D (UI Toolkit) trong khung 40x40 giong viewBox SVG cua ban web.
// Dung cu: port tu ICON (dong 353-363). Do vat: hinh ve moi thay cho emoji (emoji khong hien on dinh tren WebGL).
using UnityEngine;
using UnityEngine.UIElements;

namespace SereneIsle
{
    public enum StatIcon { Heart, Drop, Bolt, Leaf, Sun, Moon }

    public static class IconPainter
    {
        static readonly Color Wood = Hex("#8a5a2e"), Steel = Hex("#c3ccd2"), SteelD = Hex("#4f5a63"), Ink = Hex("#3a2715"), White = Color.white;
        static Color Hex(string h) => MaterialLibrary.Hex(h);

        /// <summary>Ngu canh ve: doi toa do khung 40x40 sang pixel cua phan tu.</summary>
        sealed class Pen
        {
            public Painter2D p; public Vector2 o; public float k;
            public Vector2 V(float x, float y) => o + new Vector2(x, y) * k;

            public Pen M(float x, float y) { p.BeginPath(); p.MoveTo(V(x, y)); return this; }
            public Pen L(float x, float y) { p.LineTo(V(x, y)); return this; }
            public Pen Q(float cx, float cy, float x, float y) { p.QuadraticCurveTo(V(cx, cy), V(x, y)); return this; }
            public Pen C(float c1x, float c1y, float c2x, float c2y, float x, float y) { p.BezierCurveTo(V(c1x, c1y), V(c2x, c2y), V(x, y)); return this; }
            public Pen Z() { p.ClosePath(); return this; }
            public void Fill(Color c) { p.fillColor = c; p.Fill(); }
            public void Stroke(Color c, float w) { p.strokeColor = c; p.lineWidth = w * k; p.lineCap = LineCap.Round; p.lineJoin = LineJoin.Round; p.Stroke(); }
            public void FillStroke(Color f, Color s, float w) { Fill(f); Stroke(s, w); }

            public void Line(float x1, float y1, float x2, float y2, Color c, float w) { M(x1, y1).L(x2, y2); Stroke(c, w); }

            /// <summary>Elip bang 4 duong Bezier (khong dung Arc de tranh khac biet API).</summary>
            public Pen Ellipse(float cx, float cy, float rx, float ry)
            {
                const float K = .5523f;
                M(cx + rx, cy);
                C(cx + rx, cy + ry * K, cx + rx * K, cy + ry, cx, cy + ry);
                C(cx - rx * K, cy + ry, cx - rx, cy + ry * K, cx - rx, cy);
                C(cx - rx, cy - ry * K, cx - rx * K, cy - ry, cx, cy - ry);
                C(cx + rx * K, cy - ry, cx + rx, cy - ry * K, cx + rx, cy);
                return Z();
            }
            public Pen Circle(float cx, float cy, float r) => Ellipse(cx, cy, r, r);

            public Pen RoundRect(float x, float y, float w, float h, float r)
            {
                M(x + r, y).L(x + w - r, y).Q(x + w, y, x + w, y + r).L(x + w, y + h - r).Q(x + w, y + h, x + w - r, y + h)
                    .L(x + r, y + h).Q(x, y + h, x, y + h - r).L(x, y + r).Q(x, y, x + r, y);
                return Z();
            }

            public Pen Poly(params float[] xy)
            {
                M(xy[0], xy[1]);
                for (int i = 2; i < xy.Length; i += 2) L(xy[i], xy[i + 1]);
                return Z();
            }
        }

        static readonly Pen _pen = new Pen();

        static Color Dark(Color c, float f = .7f) => new Color(c.r * f, c.g * f, c.b * f, c.a);
        static Color Light(Color c, float f = .45f) => Color.Lerp(c, Color.white, f);
        static Color A(Color c, float a) => new Color(c.r, c.g, c.b, a);

        static Pen Begin(Painter2D p, Rect r)
        {
            _pen.p = p;
            _pen.k = Mathf.Min(r.width, r.height) / 40f;
            _pen.o = r.center - new Vector2(20, 20) * _pen.k;
            return _pen;
        }

        public static void Draw(Painter2D painter, Rect r, IconShape s, Color c1, Color c2)
        {
            if (s == IconShape.None || r.width <= 1) return;
            var g = Begin(painter, r);
            Color o1 = Dark(c1, .55f), o2 = Dark(c2, .55f);
            switch (s)
            {
                // ---------------- dung cu (port SVG) ----------------
                case IconShape.ToolShovel:
                    g.Line(9, 35, 25, 13, Wood, 4.5f); g.Line(5, 33, 13, 39, Wood, 4);
                    g.M(22, 15).L(27, 6).Q(33, 7, 34, 13).L(26, 18).Z().FillStroke(Steel, SteelD, 2); break;
                case IconShape.ToolAxe:
                    g.Line(11, 35, 27, 9, Wood, 4.5f);
                    g.M(21, 5).Q(33, 6, 34, 18).L(24, 16).Z().FillStroke(Steel, SteelD, 2); break;
                case IconShape.ToolPick:
                    g.Line(20, 36, 20, 11, Wood, 4.5f);
                    g.M(5, 15).Q(20, 2, 35, 15); g.Stroke(Hex("#7d8890"), 5); break;
                case IconShape.ToolHoe:
                    g.Line(9, 35, 27, 8, Wood, 4.5f);
                    g.Poly(23, 6, 35, 9, 32, 17, 25, 12).FillStroke(Steel, SteelD, 2); break;
                case IconShape.ToolCan:
                    g.M(12, 15).Q(18, 6, 24, 15); g.Stroke(Hex("#3f6f8f"), 3);
                    g.RoundRect(8, 15, 20, 17, 3).FillStroke(Hex("#6fa7c9"), Hex("#3f6f8f"), 2);
                    g.Line(27, 21, 36, 12, Hex("#3f6f8f"), 4); break;
                case IconShape.ToolSeed:
                    g.M(11, 16).Q(20, 10, 29, 16).L(32, 32).Q(20, 38, 8, 32).Z().FillStroke(Hex("#d8b27a"), Wood, 2);
                    g.M(20, 14).Q(19, 7, 13, 6).Q(14, 12, 20, 14).Q(21, 8, 27, 6).Q(26, 12, 20, 14).Z().Fill(Hex("#5fae3e")); break;
                case IconShape.ToolRod:
                    g.M(6, 35).Q(16, 6, 34, 5); g.Stroke(Wood, 3.5f);
                    g.Line(34, 5, 34, 26, Hex("#555555"), 1);
                    g.Circle(34, 28, 3.5f).FillStroke(Hex("#d83a2a"), White, 1.5f); break;
                case IconShape.ToolNet:
                    g.Line(6, 35, 19, 22, Wood, 4);
                    g.Circle(26, 14, 10).Fill(A(White, .6f));
                    var mesh = Hex("#9ab07a");
                    g.Line(19, 9, 33, 19, mesh, 1); g.Line(19, 19, 33, 9, mesh, 1); g.Line(26, 4, 26, 24, mesh, 1); g.Line(16, 14, 36, 14, mesh, 1);
                    g.Circle(26, 14, 10); g.Stroke(Hex("#5a7d3a"), 3); break;
                case IconShape.ToolBuild:
                    g.Line(10, 33, 25, 18, Wood, 4.5f);
                    // hinh chu nhat 16x9 tai (20,6) xoay 45 do quanh (28,10.5)
                    g.Poly(RotRect(20, 6, 16, 9, 28, 10.5f, 45)).FillStroke(Hex("#7d8890"), SteelD, 2); break;

                // ---------------- vat lieu ----------------
                case IconShape.Log:
                    g.RoundRect(5, 13, 27, 15, 5).FillStroke(c1, o1, 2);
                    g.Line(10, 18, 22, 18, o1, 1.4f); g.Line(12, 23, 26, 23, o1, 1.4f);
                    g.Ellipse(30, 20.5f, 5, 7.5f).FillStroke(c2, o1, 2);
                    g.Ellipse(30, 20.5f, 2.2f, 3.4f); g.Stroke(Dark(c2), 1.2f); break;
                case IconShape.Rock:
                    g.Poly(5, 30, 9, 17, 19, 10, 30, 13, 35, 27, 28, 34, 11, 34).FillStroke(c1, o2, 2);
                    g.Poly(11, 19, 19, 13, 25, 15, 17, 21).Fill(Light(c1, .35f)); break;
                case IconShape.Ore:
                    g.Poly(5, 30, 9, 17, 19, 10, 30, 13, 35, 27, 28, 34, 11, 34).FillStroke(c1, Dark(c1, .5f), 2);
                    g.Poly(13, 19, 17, 15, 21, 19, 17, 23).FillStroke(c2, Dark(c2), 1);
                    g.Poly(23, 24, 27, 20, 30, 24, 26, 28).FillStroke(c2, Dark(c2), 1);
                    g.Poly(13, 28, 15, 26, 17, 28, 15, 30).Fill(Light(c2)); break;
                case IconShape.Fiber:
                    g.M(20, 36).Q(18, 20, 10, 6); g.Stroke(c2, 3);
                    g.M(20, 36).Q(21, 18, 22, 4); g.Stroke(c1, 3);
                    g.M(20, 36).Q(24, 22, 32, 9); g.Stroke(c2, 3);
                    g.M(20, 36).Q(15, 26, 6, 20); g.Stroke(c1, 2.5f);
                    g.M(20, 36).Q(26, 28, 35, 23); g.Stroke(c1, 2.5f); break;

                // ---------------- do an ----------------
                case IconShape.Coconut:
                    g.Circle(20, 22, 13).FillStroke(c1, Dark(c1, .6f), 2);
                    g.M(12, 13).Q(16, 20, 13, 30); g.Stroke(Dark(c1, .75f), 1.4f);
                    g.M(26, 12).Q(23, 20, 27, 31); g.Stroke(Dark(c1, .75f), 1.4f);
                    g.Circle(16.5f, 17, 1.8f).Fill(Dark(c1, .4f)); g.Circle(23.5f, 17, 1.8f).Fill(Dark(c1, .4f)); g.Circle(20, 22, 1.8f).Fill(Dark(c1, .4f)); break;
                case IconShape.Bottle:
                    g.RoundRect(16, 4, 8, 5, 1.5f).Fill(c2);
                    g.M(17, 9).L(23, 9).L(23, 13).Q(29, 15, 29, 20).L(29, 33).Q(29, 36, 26, 36).L(14, 36).Q(11, 36, 11, 33).L(11, 20).Q(11, 15, 17, 13).Z()
                        .FillStroke(A(c1, .85f), c2, 2);
                    g.M(12, 24).Q(20, 22, 28, 24).L(28, 33).Q(28, 35, 26, 35).L(14, 35).Q(12, 35, 12, 33).Z().Fill(Hex("#3d9bd6"));
                    g.Line(15, 17, 15, 30, A(White, .7f), 1.8f); break;
                case IconShape.Berry:
                    g.M(20, 12).Q(14, 4, 7, 7).Q(12, 12, 20, 12).Z().Fill(c2);
                    g.M(20, 12).Q(27, 4, 33, 8).Q(27, 13, 20, 12).Z().Fill(c2);
                    foreach (var (x, y) in new[] { (13f, 23f), (26f, 21f), (19f, 30f) })
                    {
                        g.Circle(x, y, 7).FillStroke(c1, Dark(c1, .55f), 1.5f);
                        g.Circle(x - 2.2f, y - 2.4f, 1.8f).Fill(A(White, .7f));
                    }
                    break;
                case IconShape.Tomato:
                    g.Circle(20, 23, 12).FillStroke(c1, Dark(c1, .55f), 2);
                    g.Poly(20, 9, 23, 14, 29, 12, 25, 16, 27, 19, 20, 16, 13, 19, 15, 16, 11, 12, 17, 14).Fill(c2);
                    g.Circle(14, 21, 2.2f).Fill(A(White, .55f)); break;
                case IconShape.Wheat:
                    g.M(20, 37).Q(20, 20, 23, 5); g.Stroke(c2, 2.2f);
                    for (int i = 0; i < 5; i++)
                    {
                        float y = 8 + i * 5.2f;
                        g.Ellipse(16.5f + i * .3f, y + 2, 3, 4.6f).FillStroke(c1, c2, 1);
                        g.Ellipse(25.5f - i * .3f, y + 1, 3, 4.6f).FillStroke(c1, c2, 1);
                    }
                    break;
                case IconShape.Pumpkin:
                    g.Line(20, 12, 22, 5, c2, 3);
                    g.Ellipse(13, 24, 8, 10).FillStroke(c1, Dark(c1, .6f), 1.6f);
                    g.Ellipse(27, 24, 8, 10).FillStroke(c1, Dark(c1, .6f), 1.6f);
                    g.Ellipse(20, 24, 7, 11).FillStroke(Light(c1, .15f), Dark(c1, .6f), 1.6f); break;
                case IconShape.Melon:
                    g.Ellipse(20, 21, 15, 12).FillStroke(c1, Dark(c1, .55f), 2);
                    for (int i = -2; i <= 2; i++) { g.M(20 + i * 5, 9.5f).Q(20 + i * 7, 21, 20 + i * 5, 32.5f); g.Stroke(Dark(c1, .7f), 2); }
                    g.M(24, 26).L(36, 33).Q(32, 37, 26, 37).Z().FillStroke(c2, Hex("#5f8a3a"), 2); break;
                case IconShape.SeedBag:
                    g.M(11, 14).Q(20, 9, 29, 14).L(32, 32).Q(20, 38, 8, 32).Z().FillStroke(c1, Wood, 2);
                    g.Line(12, 16, 28, 16, Wood, 1.5f);
                    g.Ellipse(20, 25, 4.5f, 5.5f).FillStroke(c2, Dark(c2, .55f), 1.2f); break;
                case IconShape.Skewer:
                    g.Line(4, 36, 36, 4, Dark(c2), 2.4f);
                    // ca nghieng: ve than xoay bang da giac
                    g.Poly(RotPoly(new float[] { 8, 20, 14, 14, 26, 14, 30, 20, 26, 26, 14, 26 }, 20, 20, -45)).FillStroke(c1, Dark(c1, .55f), 1.5f);
                    g.Poly(RotPoly(new float[] { 30, 20, 36, 15, 36, 25 }, 20, 20, -45)).FillStroke(c1, Dark(c1, .55f), 1.5f);
                    g.Circle(13, 27, 1.4f).Fill(Ink); break;
                case IconShape.Pie:
                    g.M(4, 22).L(36, 22).Q(34, 34, 20, 34).Q(6, 34, 4, 22).Z().FillStroke(c2, Dark(c2), 1.8f);
                    g.Ellipse(20, 21, 16, 7).FillStroke(c1, Dark(c1, .6f), 1.8f);
                    for (int i = -2; i <= 2; i++) g.Line(20 + i * 5 - 3, 16, 20 + i * 5 + 3, 26, Dark(c1, .7f), 1.4f); break;
                case IconShape.Bowl:
                    g.M(13, 13).Q(10, 9, 13, 5); g.Stroke(A(White, .8f), 1.6f);
                    g.M(21, 13).Q(18, 9, 21, 5); g.Stroke(A(White, .8f), 1.6f);
                    g.M(4, 20).L(36, 20).Q(34, 34, 20, 34).Q(6, 34, 4, 20).Z().FillStroke(c2, Dark(c2, .55f), 2);
                    g.Ellipse(20, 20, 15, 4.5f).FillStroke(c1, Dark(c1, .6f), 1.4f); break;
                case IconShape.Bread:
                    g.M(4, 28).Q(4, 12, 20, 12).Q(36, 12, 36, 28).Q(36, 32, 32, 32).L(8, 32).Q(4, 32, 4, 28).Z().FillStroke(c1, Dark(c2, .7f), 2);
                    for (int i = 0; i < 3; i++) g.Line(12 + i * 7, 17, 15 + i * 7, 24, c2, 2); break;
                case IconShape.Egg:
                    g.M(20, 5).C(29, 5, 32, 22, 31, 26).C(30, 33, 25, 36, 20, 36).C(15, 36, 10, 33, 9, 26).C(8, 22, 11, 5, 20, 5).Z().FillStroke(c1, c2, 2);
                    g.Ellipse(15.5f, 16, 2.2f, 3.5f).Fill(A(White, .7f)); break;
                case IconShape.Omelet:
                    g.Circle(20, 21, 15).FillStroke(Hex("#5a5a5a"), Hex("#2a2a2a"), 2);
                    g.Line(33, 13, 39, 7, Hex("#2a2a2a"), 3.5f);
                    g.M(9, 20).Q(10, 10, 19, 11).Q(29, 9, 31, 19).Q(32, 30, 21, 31).Q(8, 31, 9, 20).Z().Fill(c2);
                    g.Circle(19, 20, 5.5f).FillStroke(c1, Dark(c1, .75f), 1); break;
                case IconShape.Milk:
                    g.M(15, 4).L(25, 4).L(25, 10).L(30, 16).L(30, 35).L(10, 35).L(10, 16).L(15, 10).Z().FillStroke(c1, Hex("#8a8a8a"), 2);
                    g.RoundRect(10, 19, 20, 9, 1).Fill(c2);
                    g.Line(13, 14, 13, 32, A(White, .8f), 1.6f); break;
                case IconShape.Cup:
                    g.Line(23, 13, 30, 2, c2, 2.5f);
                    g.M(9, 12).L(31, 12).L(28, 36).L(12, 36).Z().FillStroke(c1, Hex("#c8b89a"), 2);
                    g.M(10, 17).L(30, 17).L(28.5f, 35).L(11.5f, 35).Z().Fill(Hex("#f2ead2"));
                    g.M(6, 13).Q(9, 6, 15, 9).Q(11, 13, 6, 13).Z().Fill(c2); break;

                // ---------------- ca & sinh vat bien ----------------
                case IconShape.Fish:
                    g.Poly(27, 20, 37, 12, 35, 20, 37, 28).FillStroke(c1, o1, 1.8f);
                    g.Ellipse(17, 20, 13, 8).FillStroke(c1, o1, 2);
                    g.M(7, 22).Q(17, 28, 27, 22).Q(17, 25, 7, 22).Z().Fill(c2);
                    g.M(15, 12).Q(20, 6, 24, 13); g.Stroke(o1, 2);
                    g.Circle(10, 18, 2.2f).Fill(White); g.Circle(9.6f, 18, 1.1f).Fill(Ink); break;
                case IconShape.TropicalFish:
                    g.Poly(26, 20, 36, 10, 34, 20, 36, 30).FillStroke(c1, o1, 1.8f);
                    g.M(13, 9).Q(20, 2, 26, 9); g.Stroke(c2, 3);
                    g.Ellipse(17, 20, 12, 10).FillStroke(c1, o1, 2);
                    g.M(14, 11).Q(11, 20, 14, 29); g.Stroke(c2, 3);
                    g.M(21, 11).Q(18, 20, 21, 29); g.Stroke(c2, 3);
                    g.Circle(9.5f, 18, 2.2f).Fill(White); g.Circle(9.1f, 18, 1.1f).Fill(Ink); break;
                case IconShape.Puffer:
                    g.Poly(29, 21, 37, 15, 37, 27).FillStroke(c1, o1, 1.6f);
                    for (int i = 0; i < 12; i++)
                    {
                        float a = i / 12f * Mathf.PI * 2;
                        g.Line(18 + Mathf.Cos(a) * 11, 21 + Mathf.Sin(a) * 11, 18 + Mathf.Cos(a) * 15, 21 + Mathf.Sin(a) * 15, o2, 1.6f);
                    }
                    g.Circle(18, 21, 12).FillStroke(c1, o1, 2);
                    g.M(8, 25).Q(18, 33, 28, 25).Q(18, 30, 8, 25).Z().Fill(Light(c1, .5f));
                    g.Circle(12, 18, 2.6f).Fill(White); g.Circle(11.4f, 18, 1.3f).Fill(Ink); break;
                case IconShape.Crab:
                    for (int i = 0; i < 3; i++) { g.Line(10, 25 + i * 3, 3, 28 + i * 3, o1, 2); g.Line(30, 25 + i * 3, 37, 28 + i * 3, o1, 2); }
                    g.Line(13, 20, 8, 14, o1, 2.4f); g.Line(27, 20, 32, 14, o1, 2.4f);
                    g.M(4, 12).Q(6, 4, 12, 8).L(9, 12).Z().FillStroke(c1, o1, 1.6f);
                    g.M(36, 12).Q(34, 4, 28, 8).L(31, 12).Z().FillStroke(c1, o1, 1.6f);
                    g.Ellipse(20, 25, 12, 8).FillStroke(c1, o1, 2);
                    g.Circle(16, 17, 2).Fill(Ink); g.Circle(24, 17, 2).Fill(Ink); break;
                case IconShape.Squid:
                    for (int i = 0; i < 5; i++) { float x = 13 + i * 3.5f; g.M(x, 24).Q(x - 2, 31, x + 1, 38); g.Stroke(c2, 2); }
                    g.M(20, 2).Q(31, 12, 28, 26).L(12, 26).Q(9, 12, 20, 2).Z().FillStroke(c1, o2, 2);
                    g.Poly(12, 13, 6, 18, 12, 20).Fill(c1); g.Poly(28, 13, 34, 18, 28, 20).Fill(c1);
                    g.Circle(16.5f, 21, 1.8f).Fill(Ink); g.Circle(23.5f, 21, 1.8f).Fill(Ink); break;
                case IconShape.Octopus:
                    for (int i = 0; i < 4; i++)
                    {
                        float x = 9 + i * 7.3f;
                        g.M(x + 2, 20).Q(x - 3, 30, x + 2, 36); g.Stroke(c1, 4.5f);
                    }
                    g.Ellipse(20, 15, 12, 11).FillStroke(c1, o2, 2);
                    g.Circle(15.5f, 16, 2.5f).Fill(White); g.Circle(24.5f, 16, 2.5f).Fill(White);
                    g.Circle(15.5f, 16.5f, 1.2f).Fill(Ink); g.Circle(24.5f, 16.5f, 1.2f).Fill(Ink); break;
                case IconShape.Lobster:
                    g.Line(16, 10, 10, 2, o1, 1.2f); g.Line(24, 10, 30, 2, o1, 1.2f);
                    g.M(5, 10).Q(4, 3, 10, 5).L(12, 12).Z().FillStroke(c1, o1, 1.4f);
                    g.M(35, 10).Q(36, 3, 30, 5).L(28, 12).Z().FillStroke(c1, o1, 1.4f);
                    g.Line(11, 11, 15, 16, c1, 3); g.Line(29, 11, 25, 16, c1, 3);
                    g.Ellipse(20, 17, 6, 8).FillStroke(c1, o1, 1.8f);
                    for (int i = 0; i < 3; i++) g.Ellipse(20, 26 + i * 3.6f, 5 - i * .8f, 2.6f).FillStroke(c1, o1, 1.4f);
                    g.Poly(20, 35, 13, 39, 27, 39).FillStroke(c1, o1, 1.4f); break;
                case IconShape.Eel:
                    g.M(4, 32).C(12, 40, 16, 22, 22, 22).C(28, 22, 28, 10, 34, 9); g.Stroke(o1, 7.5f);
                    g.M(4, 32).C(12, 40, 16, 22, 22, 22).C(28, 22, 28, 10, 34, 9); g.Stroke(c1, 5.5f);
                    g.M(5, 33).C(12, 39, 15, 24, 22, 24); g.Stroke(c2, 1.4f);
                    g.Circle(33, 9, 1.4f).Fill(Ink); break;
                case IconShape.Dragon:
                    g.Poly(27, 20, 38, 9, 35, 20, 38, 31).FillStroke(c2, Dark(c2), 1.6f);
                    g.M(12, 13).Q(18, 4, 26, 12); g.Stroke(c2, 3);
                    g.Ellipse(17, 20, 13, 8).FillStroke(c1, o1, 2);
                    for (int i = 0; i < 3; i++) for (int j = 0; j < 2; j++) g.Circle(15 + i * 4.5f, 18 + j * 4, 1.8f).Fill(Light(c1, .3f));
                    g.M(5, 21).Q(1, 24, 3, 29); g.Stroke(o1, 1.2f);
                    g.Circle(9.5f, 18, 2).Fill(Ink); break;

                // ---------------- con trung ----------------
                case IconShape.Butterfly:
                    g.Ellipse(12, 14, 8, 9).FillStroke(c2, c1, 1.8f); g.Ellipse(28, 14, 8, 9).FillStroke(c2, c1, 1.8f);
                    g.Ellipse(13, 28, 6, 7).FillStroke(c2, c1, 1.8f); g.Ellipse(27, 28, 6, 7).FillStroke(c2, c1, 1.8f);
                    g.Circle(12, 14, 2.5f).Fill(A(White, .55f)); g.Circle(28, 14, 2.5f).Fill(A(White, .55f));
                    g.Ellipse(20, 21, 2.2f, 11).Fill(c1);
                    g.M(20, 10).Q(17, 4, 14, 3); g.Stroke(c1, 1.2f); g.M(20, 10).Q(23, 4, 26, 3); g.Stroke(c1, 1.2f); break;
                case IconShape.Beetle:
                    for (int i = 0; i < 3; i++) { g.Line(12, 18 + i * 6, 5, 15 + i * 7, c1, 1.6f); g.Line(28, 18 + i * 6, 35, 15 + i * 7, c1, 1.6f); }
                    g.Circle(20, 9, 5).Fill(c1);
                    g.Ellipse(20, 23, 10, 12).FillStroke(c2, c1, 2);
                    g.Line(20, 12, 20, 35, c1, 1.6f);
                    g.Ellipse(16, 19, 2, 4).Fill(A(White, .45f)); break;
                case IconShape.Cricket:
                    g.M(14, 22).L(7, 12).L(4, 34); g.Stroke(c2, 2);
                    g.M(26, 22).L(34, 12).L(37, 34); g.Stroke(c2, 2);
                    g.Ellipse(20, 23, 6, 13).FillStroke(c1, Dark(c1, .55f), 1.6f);
                    g.Circle(20, 9, 4.2f).Fill(Dark(c1, .8f));
                    g.M(18, 6).Q(13, 1, 6, 2); g.Stroke(c1, 1.2f); g.M(22, 6).Q(27, 1, 34, 2); g.Stroke(c1, 1.2f); break;
                case IconShape.Firefly:
                    g.Circle(20, 26, 13).Fill(A(c2, .3f)); g.Circle(20, 27, 7).Fill(A(c2, .85f));
                    g.Ellipse(13, 15, 6, 4).Fill(A(White, .6f)); g.Ellipse(27, 15, 6, 4).Fill(A(White, .6f));
                    g.Ellipse(20, 19, 4, 8).Fill(c1); g.Circle(20, 10, 3.5f).Fill(c1); break;
                case IconShape.Dragonfly:
                    g.Ellipse(11, 13, 9, 3.2f).FillStroke(A(c2, .85f), c1, 1.2f); g.Ellipse(29, 13, 9, 3.2f).FillStroke(A(c2, .85f), c1, 1.2f);
                    g.Ellipse(12, 19, 8, 2.8f).FillStroke(A(c2, .85f), c1, 1.2f); g.Ellipse(28, 19, 8, 2.8f).FillStroke(A(c2, .85f), c1, 1.2f);
                    g.M(20, 12).L(20, 37); g.Stroke(c1, 3);
                    g.Circle(20, 9, 3.6f).Fill(Dark(c1, .8f)); break;
                case IconShape.Bee:
                    g.Ellipse(13, 12, 7, 5).FillStroke(A(c2, .8f), Dark(c1, .5f), 1.2f); g.Ellipse(27, 12, 7, 5).FillStroke(A(c2, .8f), Dark(c1, .5f), 1.2f);
                    g.Ellipse(20, 23, 10, 12).FillStroke(c1, Dark(c1, .5f), 1.8f);
                    for (int i = 0; i < 3; i++) g.Line(11, 19 + i * 6, 29, 19 + i * 6, Ink, 2.4f);
                    g.Circle(20, 10, 4).Fill(Ink); break;
                case IconShape.Mantis:
                    g.M(20, 36).L(21, 16); g.Stroke(c1, 4);
                    g.M(21, 16).L(15, 10).L(11, 16); g.Stroke(c1, 2.2f); g.M(21, 16).L(27, 10).L(30, 15); g.Stroke(c1, 2.2f);
                    g.M(20, 30).L(11, 36); g.Stroke(Dark(c1, .7f), 1.6f); g.M(20, 30).L(29, 36); g.Stroke(Dark(c1, .7f), 1.6f);
                    g.Ellipse(20, 27, 4.5f, 8).Fill(A(c2, .9f));
                    g.M(17, 8).L(21, 4).L(25, 8).Z().Fill(c1); break;
                case IconShape.Cicada:
                    g.M(20, 13).L(6, 34).L(17, 30).Z().FillStroke(A(c2, .75f), Dark(c1, .6f), 1.2f);
                    g.M(20, 13).L(34, 34).L(23, 30).Z().FillStroke(A(c2, .75f), Dark(c1, .6f), 1.2f);
                    g.Ellipse(20, 20, 6, 11).FillStroke(c1, Dark(c1, .55f), 1.6f);
                    g.Circle(15, 10, 2.6f).Fill(Ink); g.Circle(25, 10, 2.6f).Fill(Ink); break;

                // ---------------- linh tinh ----------------
                case IconShape.Worm:
                    g.M(5, 28).C(10, 18, 16, 36, 22, 26).C(26, 18, 30, 30, 35, 22); g.Stroke(o1, 7);
                    g.M(5, 28).C(10, 18, 16, 36, 22, 26).C(26, 18, 30, 30, 35, 22); g.Stroke(c1, 5);
                    g.Circle(34, 21, 1.1f).Fill(Ink); break;
                case IconShape.Shell:
                    g.M(20, 35).L(4, 18).Q(6, 5, 20, 5).Q(34, 5, 36, 18).Z().FillStroke(c1, c2, 2);
                    for (int i = -2; i <= 2; i++) g.Line(20, 33, 20 + i * 6.5f, 7 + Mathf.Abs(i) * 2.5f, c2, 1.5f);
                    g.Poly(15, 35, 20, 31, 25, 35, 20, 38).Fill(c2); break;
                case IconShape.Coin:
                    g.Circle(20, 20, 15).FillStroke(c1, c2, 2.4f);
                    g.Circle(20, 20, 11); g.Stroke(Dark(c1, .8f), 1.4f);
                    g.Poly(16.5f, 16.5f, 23.5f, 16.5f, 23.5f, 23.5f, 16.5f, 23.5f).FillStroke(Light(c1, .6f), c2, 1.6f);
                    g.Circle(14, 13, 2).Fill(A(White, .6f)); break;
                case IconShape.Backpack:
                    g.M(14, 10).Q(14, 4, 20, 4).Q(26, 4, 26, 10); g.Stroke(c2, 2.4f);
                    g.RoundRect(8, 9, 24, 28, 7).FillStroke(c1, c2, 2);
                    g.M(8, 18).Q(20, 12, 32, 18).L(32, 21).Q(20, 16, 8, 21).Z().Fill(c2);
                    g.RoundRect(13, 25, 14, 9, 3).FillStroke(Light(c1, .2f), c2, 1.6f); break;
                case IconShape.Chick:
                    g.Circle(20, 24, 12).FillStroke(c1, Dark(c1, .7f), 1.8f);
                    g.Circle(20, 13, 8).FillStroke(c1, Dark(c1, .7f), 1.8f);
                    g.Poly(26, 13, 33, 15, 26, 17).Fill(c2);
                    g.Circle(22, 11, 1.6f).Fill(Ink);
                    g.M(10, 22).Q(6, 26, 11, 29); g.Stroke(Dark(c1, .8f), 1.6f); break;
                case IconShape.Goat:
                    g.M(13, 10).Q(8, 2, 3, 8); g.Stroke(c2, 3); g.M(27, 10).Q(32, 2, 37, 8); g.Stroke(c2, 3);
                    g.Ellipse(8, 15, 5, 2.5f).Fill(c1); g.Ellipse(32, 15, 5, 2.5f).Fill(c1);
                    g.M(12, 10).L(28, 10).Q(30, 22, 24, 33).L(16, 33).Q(10, 22, 12, 10).Z().FillStroke(c1, c2, 1.8f);
                    g.M(18, 33).L(22, 33).L(20, 38).Z().Fill(c1);
                    g.Circle(15.5f, 17, 1.8f).Fill(Ink); g.Circle(24.5f, 17, 1.8f).Fill(Ink); break;
                case IconShape.Cow:
                    g.Ellipse(6, 13, 5, 3).Fill(c2); g.Ellipse(34, 13, 5, 3).Fill(c2);
                    g.M(11, 8).Q(10, 3, 6, 4); g.Stroke(Hex("#e8e0cc"), 2.4f); g.M(29, 8).Q(30, 3, 34, 4); g.Stroke(Hex("#e8e0cc"), 2.4f);
                    g.RoundRect(9, 7, 22, 26, 9).FillStroke(c1, c2, 1.8f);
                    g.Ellipse(14, 13, 4, 3.5f).Fill(c2); g.Ellipse(27, 22, 3, 4).Fill(c2);
                    g.Ellipse(20, 30, 9, 6).FillStroke(Hex("#f2a8a8"), Dark(Hex("#f2a8a8"), .7f), 1.4f);
                    g.Circle(16.5f, 30, 1.3f).Fill(Ink); g.Circle(23.5f, 30, 1.3f).Fill(Ink);
                    g.Circle(15, 19, 1.8f).Fill(Ink); g.Circle(25, 19, 1.8f).Fill(Ink); break;

                // ---------------- do dat ----------------
                case IconShape.Campfire:
                    g.Line(6, 35, 34, 27, c2, 5); g.Line(6, 27, 34, 35, Dark(c2, .8f), 5);
                    g.M(20, 4).Q(31, 16, 28, 25).Q(25, 32, 20, 32).Q(12, 32, 12, 24).Q(12, 15, 20, 4).Z().Fill(c1);
                    g.M(20, 15).Q(25, 22, 24, 27).Q(22, 31, 20, 31).Q(16, 31, 16, 26).Q(16, 21, 20, 15).Z().Fill(Hex("#ffd34d")); break;
                case IconShape.Torch:
                    g.Line(20, 37, 20, 16, c2, 4.5f);
                    g.RoundRect(16, 14, 8, 5, 1.5f).Fill(Dark(c2));
                    g.M(20, 2).Q(28, 9, 25, 14).L(15, 14).Q(12, 9, 20, 2).Z().Fill(c1);
                    g.M(20, 7).Q(23, 11, 22, 14).L(18, 14).Q(17, 11, 20, 7).Z().Fill(White); break;
                case IconShape.Fence:
                    g.RoundRect(4, 14, 32, 4, 1).FillStroke(c1, c2, 1.4f); g.RoundRect(4, 25, 32, 4, 1).FillStroke(c1, c2, 1.4f);
                    foreach (float x in new[] { 6f, 18f, 30f }) g.Poly(x, 9, x + 2, 6, x + 4, 9, x + 4, 36, x, 36).FillStroke(c1, c2, 1.6f); break;
                case IconShape.PathTile:
                    g.Poly(20, 4, 37, 20, 20, 36, 3, 20).FillStroke(c2, Dark(c2), 1.6f);
                    g.Poly(20, 7, 27, 14, 20, 21, 13, 14).Fill(c1); g.Poly(28, 15, 34, 20, 27, 26, 21, 21).Fill(c1);
                    g.Poly(12, 15, 19, 21, 13, 27, 6, 20).Fill(c1); g.Poly(20, 22, 26, 27, 20, 33, 14, 28).Fill(c1); break;
                case IconShape.Sprinkler:
                    foreach (var (x, y) in new[] { (7f, 10f), (33f, 10f), (12f, 4f), (28f, 4f), (20f, 2f) }) g.Ellipse(x, y, 1.6f, 2.4f).Fill(c2);
                    g.RoundRect(17, 10, 6, 14, 1.5f).FillStroke(c1, Dark(c1, .6f), 1.4f);
                    g.Ellipse(20, 10, 6, 2.5f).FillStroke(Light(c1), Dark(c1, .6f), 1.2f);
                    g.M(9, 36).L(31, 36).L(27, 24).L(13, 24).Z().FillStroke(c1, Dark(c1, .6f), 1.6f); break;
                case IconShape.Scarecrow:
                    g.Line(20, 39, 20, 12, Wood, 3); g.Line(5, 20, 35, 20, Wood, 3);
                    g.M(10, 18).L(30, 18).L(27, 31).L(13, 31).Z().FillStroke(c2, Dark(c2), 1.6f);
                    g.Circle(20, 11, 6).FillStroke(c1, Dark(c1, .7f), 1.4f);
                    g.M(11, 7).L(29, 7).L(25, 4).L(15, 4).Z().Fill(Dark(c1, .7f));
                    g.Line(17.5f, 10, 18.5f, 11, Ink, 1.4f); g.Line(21.5f, 11, 22.5f, 10, Ink, 1.4f); break;

                // ---------------- giong cay 4 mua (c1: nong san, c2: la) ----------------
                case IconShape.LeafGreens:
                    g.M(20, 36).C(5, 30, 4, 12, 14, 7).C(19, 16, 21, 28, 20, 36).Z().FillStroke(Dark(c1, .85f), Dark(c1, .5f), 1.5f);
                    g.M(20, 36).C(35, 30, 36, 12, 26, 7).C(21, 16, 19, 28, 20, 36).Z().FillStroke(Dark(c1, .85f), Dark(c1, .5f), 1.5f);
                    g.M(20, 36).C(11, 26, 12, 8, 20, 4).C(28, 8, 29, 26, 20, 36).Z().FillStroke(c1, Dark(c1, .5f), 1.5f);
                    g.M(20, 34).Q(19, 20, 20, 8); g.Stroke(Light(c1, .5f), 1.4f); break;
                case IconShape.Root:
                    g.M(16, 12).Q(12, 4, 9, 6); g.Stroke(c2, 2.5f);
                    g.M(20, 12).Q(20, 3, 21, 2); g.Stroke(c2, 2.5f);
                    g.M(24, 12).Q(29, 4, 32, 7); g.Stroke(c2, 2.5f);
                    g.M(12, 12).Q(20, 9, 28, 12).Q(26, 26, 20, 38).Q(14, 26, 12, 12).Z().FillStroke(c1, Dark(c1, .55f), 1.8f);
                    g.Line(15, 18, 19, 18, Dark(c1, .6f), 1.2f); g.Line(21, 24, 24, 24, Dark(c1, .6f), 1.2f); g.Line(17, 29, 20, 29, Dark(c1, .6f), 1.2f); break;
                case IconShape.RootRound:
                    g.M(17, 13).Q(13, 4, 8, 6); g.Stroke(c2, 2.5f);
                    g.M(23, 13).Q(27, 4, 32, 6); g.Stroke(c2, 2.5f);
                    g.M(20, 13).Q(20, 5, 20, 3); g.Stroke(c2, 2.5f);
                    g.Line(20, 34, 21, 39, Dark(c1, .55f), 1.5f);
                    g.Ellipse(20, 24, 11, 10.5f).FillStroke(c1, Dark(c1, .55f), 1.8f);
                    g.Circle(16, 20, 2).Fill(A(White, .5f)); break;
                case IconShape.Bulb:
                    g.Line(20, 12, 18, 3, c2, 2.5f); g.Line(20, 12, 23, 3, c2, 2.5f);
                    g.M(20, 10).C(31, 16, 34, 33, 20, 35).C(6, 33, 9, 16, 20, 10).Z().FillStroke(c1, Dark(c1, .6f), 1.8f);
                    g.M(20, 12).Q(15, 24, 18, 34); g.Stroke(Dark(c1, .75f), 1.2f);
                    g.M(20, 12).Q(25, 24, 22, 34); g.Stroke(Dark(c1, .75f), 1.2f);
                    g.Line(15, 36, 25, 36, Dark(c1, .5f), 1.2f); break;
                case IconShape.Corn:
                    g.M(14, 36).Q(6, 22, 12, 8).Q(15, 22, 20, 36).Z().Fill(c2);
                    g.Ellipse(21, 19, 7, 15).FillStroke(c1, Dark(c1, .6f), 1.6f);
                    for (int i = 0; i < 5; i++) { float y = 9 + i * 4.5f; g.Line(16, y, 26, y, Dark(c1, .75f), 1); }
                    g.Line(21, 5, 21, 33, Dark(c1, .75f), 1);
                    g.M(28, 36).Q(35, 22, 29, 10).Q(26, 24, 22, 36).Z().Fill(Dark(c2, .85f)); break;
                case IconShape.Flower:
                    g.M(20, 38).Q(19, 28, 20, 21); g.Stroke(c2, 2.5f);
                    g.M(20, 31).Q(12, 26, 9, 30).Q(14, 33, 20, 31).Z().Fill(c2);
                    for (int i = 0; i < 6; i++)
                    {
                        float a = i * Mathf.PI / 3;
                        g.Circle(20 + Mathf.Cos(a) * 7, 14 + Mathf.Sin(a) * 7, 5).FillStroke(c1, Dark(c1, .6f), 1.2f);
                    }
                    g.Circle(20, 14, 4).Fill(Dark(c1, .55f)); break;
                case IconShape.Gourd:
                    g.Line(30, 9, 34, 4, c2, 2.5f);
                    g.M(7, 33).C(9, 17, 25, 6, 31, 8).C(36, 12, 22, 33, 7, 33).Z().FillStroke(c1, Dark(c1, .55f), 1.8f);
                    g.M(11, 30).Q(20, 21, 29, 11); g.Stroke(Light(c1, .35f), 1.4f); break;
                case IconShape.Chili:
                    g.M(26, 9).Q(28, 4, 33, 4); g.Stroke(c2, 2.5f);
                    g.M(22, 9).L(30, 10).Q(30, 24, 10, 37).Q(16, 24, 22, 9).Z().FillStroke(c1, Dark(c1, .55f), 1.6f);
                    g.M(23, 13).Q(22, 22, 16, 30); g.Stroke(A(White, .45f), 1.3f); break;
                case IconShape.Fruit:
                    g.Line(20, 12, 21, 5, Wood, 2);
                    g.M(21, 8).Q(27, 2, 34, 6).Q(28, 11, 21, 8).Z().FillStroke(c2, Dark(c2, .6f), 1.2f);
                    g.Ellipse(20, 23, 12, 12.5f).FillStroke(c1, Dark(c1, .55f), 1.8f);
                    g.Circle(15, 19, 2.4f).Fill(A(White, .5f)); break;
                case IconShape.Banana:
                    g.Line(29, 6, 32, 3, Wood, 2.5f);
                    g.M(29, 6).Q(34, 28, 8, 34).Q(10, 31, 12, 30).Q(28, 26, 26, 7).Z().FillStroke(c1, Dark(c1, .55f), 1.6f);
                    g.M(26, 7).Q(30, 26, 12, 33); g.Stroke(Dark(c1, .75f), 1.1f); break;
                case IconShape.Lotus:
                    g.Ellipse(20, 33, 15, 4).Fill(c2);
                    g.M(20, 30).Q(10, 22, 13, 10).Q(19, 18, 20, 30).Z().FillStroke(c1, Dark(c1, .6f), 1.3f);
                    g.M(20, 30).Q(30, 22, 27, 10).Q(21, 18, 20, 30).Z().FillStroke(c1, Dark(c1, .6f), 1.3f);
                    g.M(20, 30).Q(14, 18, 20, 6).Q(26, 18, 20, 30).Z().FillStroke(Light(c1, .25f), Dark(c1, .6f), 1.3f); break;

                default:
                    g.Circle(20, 20, 12).FillStroke(c1, o1, 2); break;
            }
        }

        public static void DrawStat(Painter2D painter, Rect r, StatIcon s)
        {
            var g = Begin(painter, r);
            switch (s)
            {
                case StatIcon.Heart:
                    g.M(20, 35).C(8, 27, 3, 20, 4, 13).C(5, 6, 15, 3, 20, 11).C(25, 3, 35, 6, 36, 13).C(37, 20, 32, 27, 20, 35).Z()
                        .FillStroke(Hex("#d8452f"), Hex("#8a2a1a"), 2.4f); break;
                case StatIcon.Drop:
                    g.M(20, 3).Q(33, 18, 32, 25).Q(31, 36, 20, 36).Q(9, 36, 8, 25).Q(7, 18, 20, 3).Z().FillStroke(Hex("#3d9bd6"), Hex("#1f5f8a"), 2.4f);
                    g.Ellipse(15, 25, 2.2f, 4).Fill(A(White, .7f)); break;
                case StatIcon.Bolt:
                    g.Poly(23, 2, 8, 22, 18, 22, 15, 38, 32, 16, 22, 16).FillStroke(Hex("#e8b02f"), Hex("#8a5f10"), 2.4f); break;
                case StatIcon.Leaf:
                    g.M(6, 34).Q(4, 8, 34, 5).Q(36, 33, 6, 34).Z().FillStroke(Hex("#5fb8a0"), Hex("#2f6f5a"), 2.4f);
                    g.M(8, 32).Q(18, 20, 30, 9); g.Stroke(Hex("#2f6f5a"), 1.8f); break;
                case StatIcon.Sun:
                    for (int i = 0; i < 8; i++)
                    {
                        float a = i / 8f * Mathf.PI * 2;
                        g.Line(20 + Mathf.Cos(a) * 13, 20 + Mathf.Sin(a) * 13, 20 + Mathf.Cos(a) * 18, 20 + Mathf.Sin(a) * 18, Hex("#e8913a"), 2.6f);
                    }
                    g.Circle(20, 20, 10).FillStroke(Hex("#ffc93c"), Hex("#e8913a"), 2); break;
                case StatIcon.Moon:
                    g.M(25, 4).Q(9, 7, 9, 21).Q(10, 35, 26, 36).Q(15, 30, 15, 20).Q(15, 9, 25, 4).Z().FillStroke(Hex("#ffe9b8"), Hex("#9fb2ff"), 2); break;
            }
        }

        static float[] RotRect(float x, float y, float w, float h, float cx, float cy, float deg)
            => RotPoly(new[] { x, y, x + w, y, x + w, y + h, x, y + h }, cx, cy, deg);

        static float[] RotPoly(float[] pts, float cx, float cy, float deg)
        {
            float a = deg * Mathf.Deg2Rad, c = Mathf.Cos(a), s = Mathf.Sin(a);
            var r = new float[pts.Length];
            for (int i = 0; i < pts.Length; i += 2)
            {
                float dx = pts[i] - cx, dy = pts[i + 1] - cy;
                r[i] = cx + dx * c - dy * s; r[i + 1] = cy + dx * s + dy * c;
            }
            return r;
        }
    }
}
