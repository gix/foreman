namespace Foreman.Extensions
{
    using System;
    using System.Windows;

    public static class PointUtils
    {
        public static bool IsFinite(this Point pt)
        {
            return
                !double.IsNaN(pt.X) && !double.IsNaN(pt.Y) &&
                !double.IsInfinity(pt.X) && !double.IsInfinity(pt.Y);
        }

        public static Point Abs(Point pt)
        {
            return new(Math.Abs(pt.X), Math.Abs(pt.Y));
        }

        public static Point Max(Point p1, Point p2)
        {
            return new(Math.Max(p1.X, p2.X), Math.Max(p1.Y, p2.Y));
        }

        public static Point Min(Point p1, Point p2)
        {
            return new(Math.Min(p1.X, p2.X), Math.Min(p1.Y, p2.Y));
        }

        public static Point MidPoint(Point p1, Point p2)
        {
            return new((p1.X + p2.X) / 2, (p1.Y + p2.Y) / 2);
        }

        public static Point RoundToNearest(this Point point, double multiple)
        {
            return new(
                Math.Round(point.X / multiple) * multiple,
                Math.Round(point.Y / multiple) * multiple);
        }
    }
}
