namespace Foreman.Controls
{
    using System;
    using System.ComponentModel;
    using System.Windows;
    using System.Windows.Controls;
    using Extensions;

    public static class CanvasService
    {
        static CanvasService()
        {
            //Canvas.TopProperty.OverrideMetadata(
            //    typeof(CanvasService), new FrameworkPropertyMetadata(OnPositioningChanged));
            //Canvas.LeftProperty.OverrideMetadata(
            //    typeof(CanvasService), new FrameworkPropertyMetadata(OnPositioningChanged));
            //Canvas.BottomProperty.OverrideMetadata(
            //    typeof(CanvasService), new FrameworkPropertyMetadata(OnPositioningChanged));
            //Canvas.RightProperty.OverrideMetadata(
            //    typeof(CanvasService), new FrameworkPropertyMetadata(OnPositioningChanged));
        }

        private static void OnPositioningChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var element = (UIElement)d;
            double x = Canvas.GetLeft(element);
            double y = Canvas.GetTop(element);
            if (x.IsFinite() && y.IsFinite())
                SetPosition(element, new Point(x, y));
        }

        public static readonly DependencyProperty PositionProperty =
            DependencyProperty.RegisterAttached(
                "Position",
                typeof(Point),
                typeof(CanvasService),
                new FrameworkPropertyMetadata(new Point(), OnPositionChanged),
                ValidatePosition);

        private static void OnPositionChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            //var position = (Point)e.NewValue;
            //Canvas.SetLeft((UIElement)d, position.X);
            //Canvas.SetTop((UIElement)d, position.Y);
        }

        private static bool ValidatePosition(object value)
        {
            var point = (Point)value;
            return !double.IsInfinity(point.X) && !double.IsInfinity(point.Y);
        }

        [TypeConverter(typeof(PositionConverter))]
        [AttachedPropertyBrowsableForChildren]
        public static Point GetPosition(UIElement element)
        {
            if (element == null)
                throw new ArgumentNullException(nameof(element));
            return (Point)element.GetValue(PositionProperty);
        }

        public static void SetPosition(UIElement element, Point position)
        {
            if (element == null)
                throw new ArgumentNullException(nameof(element));
            element.SetValue(PositionProperty, position);
        }
    }
}
