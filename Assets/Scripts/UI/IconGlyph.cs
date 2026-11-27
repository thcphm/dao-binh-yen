// Bieu tuong ve bang Painter2D (thay ky tu ✓ ✗ ✕ ✿ ⚙ ✦ ♥ ★ ☆ ▶ ◆): font Baloo 2 khong co cac ky tu nay
// va WebGL khong co font he thong de thay the. Mau = mau chu (color) cua phan tu.
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace SereneIsle
{
    public class IconGlyph : VisualElement
    {
        public enum Kind { Check, Cross, Close, Flower, Gear, Sparkle, Heart, Star, StarOutline, Play, Diamond }

        Kind _kind;
        public Kind Glyph { get => _kind; set { _kind = value; MarkDirtyRepaint(); } }

        public IconGlyph(Kind kind, float size = 18)
        {
            _kind = kind;
            AddToClassList("icon-glyph");
            style.width = size; style.height = size;
            style.flexShrink = 0;
            pickingMode = PickingMode.Ignore;
            generateVisualContent += Draw;
        }

        /// <summary>Hang: bieu tuong + chu (thay "✓ Muc 25").</summary>
        public static VisualElement Row(Kind kind, string text, string labelClass, float size = 14)
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;
            var ic = new IconGlyph(kind, size);
            ic.style.marginRight = 5;
            row.Add(ic);
            var l = new Label(text);
            if (labelClass != null) foreach (var c in labelClass.Split(' ')) l.AddToClassList(c);
            row.Add(l);
            return row;
        }

        void Draw(MeshGenerationContext mgc)
        {
            float w = contentRect.width, h = contentRect.height;
            if (w <= 0 || h <= 0) return;
            float s = Mathf.Min(w, h), cx = w / 2, cy = h / 2, r = s / 2;
            var p = mgc.painter2D;
            var col = resolvedStyle.color;
            p.fillColor = col; p.strokeColor = col;
            p.lineJoin = LineJoin.Round; p.lineCap = LineCap.Round;
            Vector2 P(float x, float y) => new Vector2(cx + x * r, cy + y * r); // toa do -1..1, y huong xuong
            switch (_kind)
            {
                case Kind.Check:
                    p.lineWidth = s * .16f;
                    p.BeginPath(); p.MoveTo(P(-.7f, .05f)); p.LineTo(P(-.2f, .55f)); p.LineTo(P(.75f, -.55f)); p.Stroke();
                    break;
                case Kind.Cross:
                case Kind.Close:
                    p.lineWidth = s * (_kind == Kind.Close ? .14f : .16f);
                    p.BeginPath(); p.MoveTo(P(-.6f, -.6f)); p.LineTo(P(.6f, .6f)); p.Stroke();
                    p.BeginPath(); p.MoveTo(P(.6f, -.6f)); p.LineTo(P(-.6f, .6f)); p.Stroke();
                    break;
                case Kind.Play:
                    Poly(p, P(-.55f, -.7f), P(.75f, 0), P(-.55f, .7f));
                    break;
                case Kind.Diamond:
                    Poly(p, P(0, -.85f), P(.7f, 0), P(0, .85f), P(-.7f, 0));
                    break;
                case Kind.Star:
                case Kind.StarOutline:
                {
                    var pts = new List<Vector2>();
                    for (int i = 0; i < 10; i++)
                    {
                        float a = -Mathf.PI / 2 + i * Mathf.PI / 5, rr = i % 2 == 0 ? .95f : .42f;
                        pts.Add(P(Mathf.Cos(a) * rr, Mathf.Sin(a) * rr + .06f));
                    }
                    if (_kind == Kind.Star) Poly(p, pts.ToArray());
                    else { p.lineWidth = s * .09f; p.BeginPath(); p.MoveTo(pts[0]); for (int i = 1; i < pts.Count; i++) p.LineTo(pts[i]); p.ClosePath(); p.Stroke(); }
                    break;
                }
                case Kind.Sparkle:
                    Poly(p, P(0, -.95f), P(.2f, -.2f), P(.95f, 0), P(.2f, .2f), P(0, .95f), P(-.2f, .2f), P(-.95f, 0), P(-.2f, -.2f));
                    break;
                case Kind.Heart:
                    p.BeginPath();
                    p.MoveTo(P(0, .8f));
                    p.BezierCurveTo(P(-.35f, .5f), P(-.95f, .1f), P(-.9f, -.35f));
                    p.BezierCurveTo(P(-.85f, -.85f), P(-.2f, -.95f), P(0, -.45f));
                    p.BezierCurveTo(P(.2f, -.95f), P(.85f, -.85f), P(.9f, -.35f));
                    p.BezierCurveTo(P(.95f, .1f), P(.35f, .5f), P(0, .8f));
                    p.ClosePath(); p.Fill();
                    break;
                case Kind.Flower:
                    for (int i = 0; i < 5; i++)
                    {
                        float a = -Mathf.PI / 2 + i * Mathf.PI * 2 / 5;
                        Circle(p, P(Mathf.Cos(a) * .5f, Mathf.Sin(a) * .5f), r * .36f);
                    }
                    Circle(p, P(0, 0), r * .28f);
                    break;
                case Kind.Gear:
                {
                    p.lineWidth = s * .2f;
                    for (int i = 0; i < 8; i++)
                    {
                        float a = i * Mathf.PI / 4;
                        p.BeginPath(); p.MoveTo(P(Mathf.Cos(a) * .55f, Mathf.Sin(a) * .55f)); p.LineTo(P(Mathf.Cos(a) * .9f, Mathf.Sin(a) * .9f)); p.Stroke();
                    }
                    p.lineWidth = s * .2f;
                    p.BeginPath(); p.Arc(P(0, 0), r * .5f, 0, 360); p.Stroke();
                    break;
                }
            }
        }

        static void Poly(Painter2D p, params Vector2[] pts)
        {
            p.BeginPath(); p.MoveTo(pts[0]);
            for (int i = 1; i < pts.Length; i++) p.LineTo(pts[i]);
            p.ClosePath(); p.Fill();
        }

        static void Circle(Painter2D p, Vector2 c, float rad)
        {
            p.BeginPath(); p.Arc(c, rad, 0, 360); p.ClosePath(); p.Fill();
        }
    }
}
