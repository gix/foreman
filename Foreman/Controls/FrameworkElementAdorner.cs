namespace Foreman.Controls
{
    using System.Collections;
    using System.Windows;
    using System.Windows.Documents;
    using System.Windows.Media;

    public class FrameworkElementAdorner : Adorner
    {
        private readonly FrameworkElement root;
        private Point position;

        public FrameworkElementAdorner(UIElement adornedElement, FrameworkElement child)
            : base(adornedElement)
        {
            Child = child;
            root = new AdornerDecorator {
                IsHitTestVisible = false,
                Child = child
            };
            AddVisualChild(root);
            AddLogicalChild(root);
        }

        public FrameworkElement Child { get; }

        protected override IEnumerator LogicalChildren
        {
            get { yield return root; }
        }

        protected override int VisualChildrenCount => 1;

        public Point Position
        {
            get => position;
            set
            {
                position = value;
                var layer = Parent as AdornerLayer;
                layer?.Update(AdornedElement);
            }
        }

        protected override Visual GetVisualChild(int index)
        {
            return root;
        }

        protected override Size MeasureOverride(Size constraint)
        {
            var size = AdornedElement.RenderSize;
            root.Measure(size);
            return size;
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            var rect = new Rect(new Point(), AdornedElement.RenderSize);
            root.Arrange(rect);
            return AdornedElement.RenderSize;
        }
    }
}
