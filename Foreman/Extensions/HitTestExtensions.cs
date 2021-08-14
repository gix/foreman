namespace Foreman.Extensions
{
    using System.Collections.Generic;
    using System.Windows;
    using System.Windows.Media;

    public static class HitTestExtensions
    {
        public static List<T> HitTestDrawn<T>(this Visual reference, Point point)
            where T : class
        {
            var result = new List<T>();

            HitTestResultBehavior Callback(HitTestResult r)
            {
                if (r.VisualHit is T value)
                    result.Add(value);
                return HitTestResultBehavior.Continue;
            }

            var parameters = new PointHitTestParameters(point);
            VisualTreeHelper.HitTest(reference, null, Callback, parameters);
            return result;
        }

        public static List<T> HitTestAll<T>(this Visual reference, Point point)
            where T : class
        {
            var result = new List<T>();

            HitTestFilterBehavior Filter(DependencyObject d)
            {
                if (d is T value)
                    result.Add(value);
                return HitTestFilterBehavior.Continue;
            }

            static HitTestResultBehavior Callback(HitTestResult r)
            {
                return HitTestResultBehavior.Continue;
            }

            var parameters = new PointHitTestParameters(point);
            VisualTreeHelper.HitTest(reference, Filter, Callback, parameters);
            result.Reverse();
            return result;
        }

        public static T? HitTestAllSingle<T>(this Visual reference, Point point)
            where T : class
        {
            T? result = null;

            HitTestFilterBehavior Filter(DependencyObject d)
            {
                if (d is T value)
                    result = value;
                return HitTestFilterBehavior.Continue;
            }

            HitTestResultBehavior Callback(HitTestResult r)
            {
                return HitTestResultBehavior.Continue;
            }

            var parameters = new PointHitTestParameters(point);
            VisualTreeHelper.HitTest(reference, Filter, Callback, parameters);
            return result;
        }

        public static List<DependencyObject> HitTestAll(
            this Visual reference, Rect rect)
        {
            var result = new List<DependencyObject>();

            HitTestResultBehavior Callback(HitTestResult r)
            {
                result.Add(r.VisualHit);
                return HitTestResultBehavior.Continue;
            }

            var geometry = new RectangleGeometry(rect);
            var parameters = new GeometryHitTestParameters(geometry);
            VisualTreeHelper.HitTest(reference, null, Callback, parameters);
            return result;
        }

        public static TDataContext? HitTestDataContext<TRootDataContext, TDataContext>(
            this Visual reference, Point point)
            where TRootDataContext : class
            where TDataContext : class
        {
            foreach (var fe in reference.HitTestDrawn<FrameworkElement>(point)) {
                if (fe.DataContext is TDataContext dc)
                    return dc;
                if (fe.DataContext is TRootDataContext)
                    break;
            }
            return null;
        }
    }
}
