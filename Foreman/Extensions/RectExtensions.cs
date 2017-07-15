namespace Foreman.Extensions
{
    using System;
    using System.Windows;

    /// <summary>Provides extension methods for <see cref="Rect"/>.</summary>
    public static class RectExtensions
    {
        /// <summary>Returns the center point of the <see cref="Rect"/>.</summary>
        /// <param name="rect">The rect to return the center point of.</param>
        /// <returns>
        ///   The center <see cref="Point"/> of the <paramref name="rect"/>.
        /// </returns>
        public static Point GetCenter(this Rect rect)
        {
            return new Point(rect.X + rect.Width / 2, rect.Y + rect.Height / 2);
        }

        /// <summary>
        ///   Returns whether the <see cref="Rect"/> defines a real area in space.
        /// </summary>
        /// <param name="rect">The rect to test.</param>
        /// <returns>
        ///   <see langword="true"/> if <paramref name="rect"/> defines an area
        ///   or point in finite space, which is not the case for
        ///   <see cref="Rect.Empty"/> or if any of the fields are
        ///   <see cref="double.NaN"/>; otherwise <see langword="false"/>.
        /// </returns>
        public static bool IsDefined(this Rect rect)
        {
            return rect.Width >= 0.0
                   && rect.Height >= 0.0
                   && rect.Left < double.PositiveInfinity
                   && rect.Top < double.PositiveInfinity
                   && (rect.Left > double.NegativeInfinity || double.IsPositiveInfinity(rect.Width))
                   && (rect.Top > double.NegativeInfinity || double.IsPositiveInfinity(rect.Height));
        }

        /// <summary>
        ///   Indicates whether the specified rectangle intersects with the current
        ///   rectangle, properly considering the empty rect and infinities.
        /// </summary>
        /// <param name="self">The current rectangle.</param>
        /// <param name="rect">The rectangle to check.</param>
        /// <returns>
        ///   <c>true</c> if the specified rectangle intersects with the current
        ///   rectangle; otherwise, <c>false</c>.
        /// </returns>
        public static bool Intersects(this Rect self, Rect rect)
        {
            return (self.IsEmpty || rect.IsEmpty)
                   || (double.IsPositiveInfinity(self.Width) || self.Right >= rect.Left)
                   && (double.IsPositiveInfinity(rect.Width) || rect.Right >= self.Left)
                   && (double.IsPositiveInfinity(self.Height) || self.Bottom >= rect.Top)
                   && (double.IsPositiveInfinity(rect.Height) || rect.Bottom >= self.Top);
        }
    }
}
