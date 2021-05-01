namespace Foreman.Controls
{
    using System.Collections;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Documents;
    using System.Windows.Media;

    public class ContentAdorner : Adorner
    {
        private readonly FrameworkElement child;
        private Point position;

        public ContentAdorner(UIElement adornedElement, object content)
            : base(adornedElement)
        {
            Content = content;
            child = new ContentPresenter {
                IsHitTestVisible = false,
                Content = content,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top
            };
            AddVisualChild(child);
            AddLogicalChild(child);
        }

        public object Content { get; }

        protected override IEnumerator LogicalChildren
        {
            get
            {
                yield return child;
            }
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
            return child;
        }

        protected override Size MeasureOverride(Size constraint)
        {
            var size = AdornedElement.RenderSize;
            child.Measure(size);
            return size;
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            var rect = new Rect(new Point(), AdornedElement.RenderSize);
            child.Arrange(rect);
            return AdornedElement.RenderSize;
        }

        public override GeneralTransform GetDesiredTransform(GeneralTransform transform)
        {
            var pt = Position - (Vector)child.RenderSize / 2;

            var transformGroup = new GeneralTransformGroup();
            transformGroup.Children.Add(new TranslateTransform(pt.X, pt.Y));
            transformGroup.Children.Add(base.GetDesiredTransform(transform));
            return transformGroup;
        }
    }
}