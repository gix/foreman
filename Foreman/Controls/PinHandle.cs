namespace Foreman.Controls
{
    using System;
    using System.Windows;
    using System.Windows.Controls;
    using Extensions;

    public class PinHandle : Control
    {
        static PinHandle()
        {
            Type forType = typeof(PinHandle);
            DefaultStyleKeyProperty.OverrideMetadata(
                forType, new FrameworkPropertyMetadata(forType));
        }

        public PinHandle()
        {
            Focusable = false;
            LayoutUpdated += (s, e) => RecomputeHotspot();
        }

        public static readonly DependencyProperty HotspotProperty =
            DependencyProperty.Register(
                nameof(Hotspot), typeof(Point), typeof(PinHandle));

        public Point Hotspot
        {
            get => (Point)GetValue(HotspotProperty);
            set => SetValue(HotspotProperty, value);
        }

        private void RecomputeHotspot()
        {
            var parent = this.FindAncestor<Canvas>();
            if (parent == null || !parent.IsAncestorOf(this))
                return;

            var centerPoint = new Point(ActualWidth / 2, ActualHeight / 2);

            // Transform the center point so that it is relative to the parent
            // control.
            Hotspot = TransformToAncestor(parent).Transform(centerPoint);
        }
    }
}
