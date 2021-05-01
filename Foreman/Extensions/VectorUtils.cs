namespace Foreman.Extensions
{
    using System;
    using System.Windows;

    public static class VectorUtils
    {
        public static Vector Abs(this Vector vector)
        {
            return new(Math.Abs(vector.X), Math.Abs(vector.Y));
        }

        public static Vector Max(Vector v1, Vector v2)
        {
            return new(Math.Max(v1.X, v2.X), Math.Max(v1.Y, v2.Y));
        }

        public static Vector Min(Vector v1, Vector v2)
        {
            return new(Math.Min(v1.X, v2.X), Math.Min(v1.Y, v2.Y));
        }

        public static bool IsGreaterThanDragDistance(this Vector vector)
        {
            return
                vector.X.GreaterThan(SystemParameters.MinimumHorizontalDragDistance) ||
                vector.Y.GreaterThan(SystemParameters.MinimumVerticalDragDistance);
        }
    }
}
