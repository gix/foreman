namespace Foreman
{
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.Linq;

    public class GhostNodeElement : GraphElement
    {
        public HashSet<Item> Items { get; set; } = new HashSet<Item>();
        public HashSet<Recipe> Recipes { get; set; } = new HashSet<Recipe>();

        private const int iconSize = 32;

        private readonly List<Point> offsetOrder = new List<Point> {
            new Point(0, 0),
            new Point(0, 35),
            new Point(0, -35),
            new Point(-35, 0),
            new Point(35, 0),
            new Point(-35, -35),
            new Point(35, 35),
            new Point(-35, 35),
            new Point(35, -35)
        };

        public GhostNodeElement(ProductionGraphViewer parent) : base(parent)
        {
            Width = 96;
            Height = 96;
        }

        public override void Paint(Graphics graphics)
        {
            int i = 0;

            List<Bitmap> icons = new List<Bitmap>();
            if (Items.Any())
                icons.AddRange(Items.Select(x => x.Icon));
            else
                icons.AddRange(Recipes.Select(x => x.Icon));

            foreach (Bitmap icon in icons) {
                if (i >= offsetOrder.Count) {
                    break;
                }
                Point position = Point.Subtract(offsetOrder[i], new Size(iconSize / 2, iconSize / 2));
                int scale = Convert.ToInt32(iconSize / Parent.ViewScale);
                graphics.DrawImage(icon ?? DataCache.UnknownIcon, position.X, position.Y, scale, scale);
                i++;
            }

            base.Paint(graphics);
        }

        public override void Dispose()
        {
            if (Parent.GhostDragElement == this) {
                Parent.GhostDragElement = null;
            }
            base.Dispose();
        }
    }
}
