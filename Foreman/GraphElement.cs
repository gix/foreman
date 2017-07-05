namespace Foreman
{
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.Windows.Forms;

    public abstract class GraphElement : IDisposable
    {
        public HashSet<GraphElement> SubElements { get; }
        public virtual Point Location { get; set; }

        public virtual int X
        {
            get => Location.X;
            set => Location = new Point(value, Location.Y);
        }

        public virtual int Y
        {
            get => Location.Y;
            set => Location = new Point(Location.X, value);
        }

        public virtual Point Size { get; set; }

        public virtual int Width
        {
            get => Size.X;
            set => Size = new Point(value, Size.Y);
        }

        public virtual int Height
        {
            get => Size.Y;
            set => Size = new Point(Size.X, value);
        }

        public Rectangle Bounds
        {
            get => new Rectangle(X, Y, Width, Height);
            set
            {
                X = value.X;
                Y = value.Y;
                Width = value.Width;
                Height = value.Height;
            }
        }

        public ProductionGraphViewer Parent { get; }

        public GraphElement(ProductionGraphViewer parent)
        {
            Parent = parent;
            Parent.Elements.Add(this);
            SubElements = new HashSet<GraphElement>();
        }

        public virtual bool ContainsPoint(Point point)
        {
            return false;
        }

        public virtual void Paint(Graphics graphics)
        {
            foreach (GraphElement element in SubElements) {
                graphics.TranslateTransform(element.X, element.Y);
                element.Paint(graphics);
                graphics.TranslateTransform(-element.X, -element.Y);
            }
        }

        public virtual void MouseMoved(Point location)
        {
        }

        public virtual void MouseDown(Point location, MouseButtons button)
        {
        }

        public virtual void MouseUp(Point location, MouseButtons button)
        {
        }

        public virtual void Dragged(Point location)
        {
        }

        public virtual void Dispose()
        {
            Parent.Elements.Remove(this);
            Parent.Invalidate();
        }
    }
}
