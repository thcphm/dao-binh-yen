using UnityEngine;
using UnityEngine.UIElements;

namespace SereneIsle
{
    /// <summary>O ve bieu tuong item (vector Painter2D, hoac sprite neu item co).</summary>
    public class ItemIcon : VisualElement
    {
        IconShape _shape;
        Color _c1 = Color.white, _c2 = Color.white;

        public ItemIcon()
        {
            pickingMode = PickingMode.Ignore;
            AddToClassList("item-icon");
            generateVisualContent += OnGenerate;
        }

        public void Set(ItemDefinition it, bool gray = false)
        {
            if (it && it.sprite)
            {
                _shape = IconShape.None;
                style.backgroundImage = new StyleBackground(it.sprite);
                style.unityBackgroundImageTintColor = gray ? new Color(.6f, .6f, .6f, .6f) : Color.white;
            }
            else
            {
                style.backgroundImage = StyleKeyword.None;
                _shape = it ? it.icon : IconShape.None;
                _c1 = it ? it.iconColor : Color.white;
                _c2 = it ? it.iconColor2 : Color.white;
                if (gray) { _c1 = Gray(_c1); _c2 = Gray(_c2); }
            }
            MarkDirtyRepaint();
        }

        public void SetShape(IconShape s, Color c1, Color c2) { _shape = s; _c1 = c1; _c2 = c2; MarkDirtyRepaint(); }

        static Color Gray(Color c) { float l = c.r * .3f + c.g * .59f + c.b * .11f; return new Color(l, l, l, c.a); }

        void OnGenerate(MeshGenerationContext ctx)
        {
            if (_shape == IconShape.None) return;
            IconPainter.Draw(ctx.painter2D, contentRect, _shape, _c1, _c2);
        }
    }

    /// <summary>Bieu tuong thanh chi so (tim, giot nuoc, tia set, la, mat troi/trang).</summary>
    public class StatIconElement : VisualElement
    {
        StatIcon _icon;
        public StatIconElement(StatIcon icon)
        {
            _icon = icon;
            pickingMode = PickingMode.Ignore;
            AddToClassList("stat-icon");
            generateVisualContent += ctx => IconPainter.DrawStat(ctx.painter2D, contentRect, _icon);
        }
        public StatIcon Icon { get => _icon; set { if (_icon != value) { _icon = value; MarkDirtyRepaint(); } } }
    }
}
