namespace Foreman.Extensions
{
    using System;
    using System.Diagnostics;

    public static class DoubleUtils
    {
        /// <summary>
        ///   Smallest <see cref="double"/> such that <c>1.0 + e != 1.0</c>.
        /// </summary>
        public const double MachineEpsilon = 2.2204460492503131E-16;

        public static bool IsFinite(this double value)
        {
            return !double.IsNaN(value) && !double.IsInfinity(value);
        }

        public static double GetFiniteValueOrDefault(this double value, double defaultValue = 0)
        {
            return value.IsFinite() ? value : defaultValue;
        }

        public static double Max(double value1, double value2, double value3)
        {
            return Math.Max(value1, Math.Max(value2, value3));
        }

        public static bool IsZero(this double value)
        {
            return Math.Abs(value) < 10.0 * MachineEpsilon;
        }

        public static bool AreClose(double value1, double value2)
        {
            if (value1 == value2)
                return true;

            // Computes (|v1-v2| / (|v1| + |v2| + 10.0)) < MachineEpsilon
            double tolerance = (Math.Abs(value1) + Math.Abs(value2) + 10.0) * MachineEpsilon;
            double diff = value1 - value2;
            return diff > -tolerance && diff < tolerance;
        }

        public static bool GreaterThan(this double value1, double value2)
        {
            return value1 > value2 && !AreClose(value1, value2);
        }

        public static bool GreaterThanOrClose(this double value1, double value2)
        {
            return value1 > value2 || AreClose(value1, value2);
        }

        public static bool LessThan(this double value1, double value2)
        {
            return value1 < value2 && !AreClose(value1, value2);
        }

        public static bool LessThanOrClose(this double value1, double value2)
        {
            return value1 < value2 || AreClose(value1, value2);
        }

        public static double Clamp(this double value, double min, double max)
        {
            Debug.Assert(max >= min);

            if (value < min)
                return min;
            if (value > max)
                return max;
            return value;
        }

        public static ulong ClampToUInt64(double value)
        {
            if (value < 0)
                return 0;
            if (value >= 1.8446744073709552E+19)
                return ulong.MaxValue;
            return (ulong)value;
        }
    }
}
