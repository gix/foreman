namespace Foreman.Extensions
{
    using System;
    using System.Windows;

    public static class LayoutUtils
    {
        public static double RoundLayoutValue(double value, double dpiScale)
        {
            // If DPI == 1, don't use DPI-aware rounding.
            if (DoubleUtils.AreClose(dpiScale, 1.0))
                return Math.Round(value);

            var newValue = Math.Round(value * dpiScale) / dpiScale;

            // If rounding produces a value unacceptable to layout
            // (NaN, Infinity or MaxValue), use the original value.
            if (!newValue.IsFinite() ||
                DoubleUtils.AreClose(newValue, double.MaxValue))
                return value;

            return newValue;
        }

        public static Thickness RoundLayoutValue(Thickness value, DpiScale dpi)
        {
            return new(
                RoundLayoutValue(value.Left, dpi.DpiScaleX),
                RoundLayoutValue(value.Top, dpi.DpiScaleY),
                RoundLayoutValue(value.Right, dpi.DpiScaleX),
                RoundLayoutValue(value.Bottom, dpi.DpiScaleY));
        }
    }
}
