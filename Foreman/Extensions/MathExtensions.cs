namespace Foreman.Extensions
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Windows;

    public static class MathExtensions
    {
        #region Approximation

        /// <summary>Returns the larger of two 8-bit unsigned integers.</summary>
        /// <param name="a">
        ///   The first of two 8-bit unsigned integers to compare.
        /// </param>
        /// <param name="b">
        ///   The second of two 8-bit unsigned integers to compare.
        /// </param>
        /// <returns>Parameter a or b, whichever is larger.</returns>
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
        public static byte AtLeast(this byte a, byte b)
        {
            return Math.Max(a, b);
        }

        /// <summary>Returns the larger of two decimal numbers.</summary>
        /// <param name="a">
        ///   The first of two <see cref="T:System.Decimal"/> numbers to compare.
        /// </param>
        /// <param name="b">
        ///   The second of two <see cref="T:System.Decimal"/> numbers to compare.
        /// </param>
        /// <returns>Parameter a or b, whichever is larger.</returns>
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
        public static decimal AtLeast(this decimal a, decimal b)
        {
            return Math.Max(a, b);
        }

        /// <summary>
        ///   Returns the larger of two double-precision floating-point numbers.
        /// </summary>
        /// <param name="a">
        ///   The first of two double-precision floating-point numbers to compare.
        /// </param>
        /// <param name="b">
        ///   The second of two double-precision floating-point numbers to compare.
        /// </param>
        /// <returns>
        ///   Parameter a or b, whichever is larger. If a, b, or both a and b
        ///   are equal to <see cref="F:System.Double.NaN"/>,
        ///   <see cref="F:System.Double.NaN"/> is returned.
        /// </returns>
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
        public static double AtLeast(this double a, double b)
        {
            return Math.Max(a, b);
        }

        /// <summary>
        ///   Returns the larger of two single-precision floating-point numbers.
        /// </summary>
        /// <param name="a">
        ///   The first of two single-precision floating-point numbers to compare.
        /// </param>
        /// <param name="b">
        ///   The second of two single-precision floating-point numbers to compare.
        /// </param>
        /// <returns>
        ///   Parameter a or b, whichever is larger. If a, or b, or both a and b
        ///   are equal to <see cref="F:System.Single.NaN"/>,
        ///   <see cref="F:System.Single.NaN"/> is returned.
        /// </returns>
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
        public static float AtLeast(this float a, float b)
        {
            return Math.Max(a, b);
        }

        /// <summary>Returns the larger of two 16-bit signed integers.</summary>
        /// <param name="a">
        ///   The first of two 16-bit signed integers to compare.
        /// </param>
        /// <param name="b">
        ///   The second of two 16-bit signed integers to compare.
        /// </param>
        /// <returns>Parameter a or b, whichever is larger.</returns>
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
        public static short AtLeast(this short a, short b)
        {
            return Math.Max(a, b);
        }

        /// <summary>Returns the larger of two 32-bit signed integers.</summary>
        /// <param name="a">
        ///   The first of two 32-bit signed integers to compare.
        /// </param>
        /// <param name="b">
        ///   The second of two 32-bit signed integers to compare.
        /// </param>
        /// <returns>Parameter a or b, whichever is larger.</returns>
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
        public static int AtLeast(this int a, int b)
        {
            return Math.Max(a, b);
        }

        /// <summary>Returns the larger of two 64-bit signed integers.</summary>
        /// <param name="a">
        ///   The first of two 64-bit signed integers to compare.
        /// </param>
        /// <param name="b">
        ///   The second of two 64-bit signed integers to compare.
        /// </param>
        /// <returns>Parameter a or b, whichever is larger.</returns>
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
        public static long AtLeast(this long a, long b)
        {
            return Math.Max(a, b);
        }

        /// <summary>Returns the larger of two 8-bit signed integers.</summary>
        /// <param name="a">
        ///   The first of two 8-bit unsigned integers to compare.
        /// </param>
        /// <param name="b">
        ///   The second of two 8-bit unsigned integers to compare.
        /// </param>
        /// <returns>Parameter a or b, whichever is larger.</returns>
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
        public static sbyte AtLeast(this sbyte a, sbyte b)
        {
            return Math.Max(a, b);
        }

        /// <summary>Returns the larger of two 16-bit unsigned integers.</summary>
        /// <param name="a">
        ///   The first of two 16-bit unsigned integers to compare.
        /// </param>
        /// <param name="b">
        ///   The second of two 16-bit unsigned integers to compare.
        /// </param>
        /// <returns>Parameter a or b, whichever is larger.</returns>
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
        public static ushort AtLeast(this ushort a, ushort b)
        {
            return Math.Max(a, b);
        }

        /// <summary>Returns the larger of two 32-bit unsigned integers.</summary>
        /// <param name="a">
        ///   The first of two 32-bit unsigned integers to compare.
        /// </param>
        /// <param name="b">
        ///   The second of two 32-bit unsigned integers to compare.
        /// </param>
        /// <returns>Parameter a or b, whichever is larger.</returns>
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
        public static uint AtLeast(this uint a, uint b)
        {
            return Math.Max(a, b);
        }

        /// <summary>Returns the larger of two 64-bit unsigned integers.</summary>
        /// <param name="a">
        ///   The first of two 64-bit unsigned integers to compare.
        /// </param>
        /// <param name="b">
        ///   The second of two 64-bit unsigned integers to compare.
        /// </param>
        /// <returns>Parameter a or b, whichever is larger.</returns>
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
        public static ulong AtLeast(this ulong a, ulong b)
        {
            return Math.Max(a, b);
        }

        /// <summary>Returns the smaller of two 8-bit unsigned integers.</summary>
        /// <param name="a">
        ///   The first of two 8-bit unsigned integers to compare.
        /// </param>
        /// <param name="b">
        ///   The second of two 8-bit unsigned integers to compare.
        /// </param>
        /// <returns>Parameter a or b, whichever is smaller.</returns>
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
        public static byte AtMost(this byte a, byte b)
        {
            return Math.Min(a, b);
        }

        /// <summary>Returns the smaller of two decimal numbers.</summary>
        /// <param name="a">
        ///   The first of two <see cref="T:System.Decimal"/> numbers to compare.
        /// </param>
        /// <param name="b">
        ///   The second of two <see cref="T:System.Decimal"/> numbers to compare.
        /// </param>
        /// <returns>Parameter a or b, whichever is smaller.</returns>
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
        public static decimal AtMost(this decimal a, decimal b)
        {
            return Math.Min(a, b);
        }

        /// <summary>
        ///   Returns the smaller of two double-precision floating-point numbers.
        /// </summary>
        /// <param name="a">
        ///   The first of two double-precision floating-point numbers to compare.
        /// </param>
        /// <param name="b">
        ///   The second of two double-precision floating-point numbers to compare.
        /// </param>
        /// <returns>
        ///   Parameter a or b, whichever is smaller. If a, b, or both a and b
        ///   are equal to <see cref="F:System.Double.NaN"/>,
        ///   <see cref="F:System.Double.NaN"/> is returned.
        /// </returns>
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
        public static double AtMost(this double a, double b)
        {
            return Math.Min(a, b);
        }

        /// <summary>
        ///   Returns the smaller of two single-precision floating-point numbers.
        /// </summary>
        /// <param name="a">
        ///   The first of two single-precision floating-point numbers to compare.
        /// </param>
        /// <param name="b">
        ///   The second of two single-precision floating-point numbers to compare.
        /// </param>
        /// <returns>
        ///   Parameter a or b, whichever is smaller. If a, b, or both a and b
        ///   are equal to <see cref="F:System.Single.NaN"/>,
        ///   <see cref="F:System.Single.NaN"/> is returned.
        /// </returns>
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
        public static float AtMost(this float a, float b)
        {
            return Math.Min(a, b);
        }

        /// <summary>Returns the smaller of two 16-bit signed integers.</summary>
        /// <param name="a">
        ///   The first of two 16-bit signed integers to compare.
        /// </param>
        /// <param name="b">
        ///   The second of two 16-bit signed integers to compare.
        /// </param>
        /// <returns>Parameter a or b, whichever is smaller.</returns>
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
        public static short AtMost(this short a, short b)
        {
            return Math.Min(a, b);
        }

        /// <summary>Returns the smaller of two 32-bit signed integers.</summary>
        /// <param name="a">
        ///   The first of two 32-bit signed integers to compare.
        /// </param>
        /// <param name="b">
        ///   The second of two 32-bit signed integers to compare.
        /// </param>
        /// <returns>Parameter a or b, whichever is smaller.</returns>
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
        public static int AtMost(this int a, int b)
        {
            return Math.Min(a, b);
        }

        /// <summary>Returns the smaller of two 64-bit signed integers.</summary>
        /// <param name="a">
        ///   The first of two 64-bit signed integers to compare.
        /// </param>
        /// <param name="b">
        ///   The second of two 64-bit signed integers to compare.
        /// </param>
        /// <returns>Parameter a or b, whichever is smaller.</returns>
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
        public static long AtMost(this long a, long b)
        {
            return Math.Min(a, b);
        }

        /// <summary>Returns the smaller of two 8-bit signed integers.</summary>
        /// <param name="a">
        ///   The first of two 8-bit signed integers to compare.
        /// </param>
        /// <param name="b">
        ///   The second of two 8-bit signed integers to compare.
        /// </param>
        /// <returns>Parameter a or b, whichever is smaller.</returns>
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
        public static sbyte AtMost(this sbyte a, sbyte b)
        {
            return Math.Min(a, b);
        }

        /// <summary>Returns the smaller of two 16-bit unsigned integers.</summary>
        /// <param name="a">
        ///   The first of two 16-bit unsigned integers to compare.
        /// </param>
        /// <param name="b">
        ///   The second of two 16-bit unsigned integers to compare.
        /// </param>
        /// <returns>Parameter a or b, whichever is smaller.</returns>
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
        public static ushort AtMost(this ushort a, ushort b)
        {
            return Math.Min(a, b);
        }

        /// <summary>Returns the smaller of two 32-bit unsigned integers.</summary>
        /// <param name="a">
        ///   The first of two 32-bit unsigned integers to compare.
        /// </param>
        /// <param name="b">
        ///   The second of two 32-bit unsigned integers to compare.
        /// </param>
        /// <returns>Parameter a or b, whichever is smaller.</returns>
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
        public static uint AtMost(this uint a, uint b)
        {
            return Math.Min(a, b);
        }

        /// <summary>Returns the smaller of two 64-bit unsigned integers.</summary>
        /// <param name="a">
        ///   The first of two 64-bit unsigned integers to compare.
        /// </param>
        /// <param name="b">
        ///   The second of two 64-bit unsigned integers to compare.
        /// </param>
        /// <returns>Parameter a or b, whichever is smaller.</returns>
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
        public static ulong AtMost(this ulong a, ulong b)
        {
            return Math.Min(a, b);
        }

        /// <summary>
        ///   Returns the value constrained inclusively between two 8-bit unsigned
        ///   integers.
        /// </summary>
        /// <param name="value">The value to restrict between a and b.</param>
        /// <param name="a">
        ///   The first of two 8-bit unsigned integers to compare.
        /// </param>
        /// <param name="b">
        ///   The second of two 8-bit unsigned integers to compare.
        /// </param>
        /// <returns>A value between a and b inclusively.</returns>
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
        public static byte Clamp(this byte value, byte a, byte b)
        {
            return a < b ? Math.Min(Math.Max(value, a), b) : Math.Max(Math.Min(value, a), b);
        }

        /// <summary>
        ///   Returns the value constrained inclusively between two decimal numbers.
        /// </summary>
        /// <param name="value">The value to restrict between a and b.</param>
        /// <param name="a">
        ///   The first of two <see cref="T:System.Decimal"/> numbers to compare.
        /// </param>
        /// <param name="b">
        ///   The second of two <see cref="T:System.Decimal"/> numbers to compare.
        /// </param>
        /// <returns>A value between a and b inclusively.</returns>
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
        public static decimal Clamp(this decimal value, decimal a, decimal b)
        {
            return a < b ? Math.Min(Math.Max(value, a), b) : Math.Max(Math.Min(value, a), b);
        }

        /// <summary>
        ///   Returns the value constrained inclusively between two double-precision
        ///   floating-point numbers.
        /// </summary>
        /// <param name="value">The value to restrict between a and b.</param>
        /// <param name="a">
        ///   The first of two double-precision floating-point numbers to compare.
        /// </param>
        /// <param name="b">
        ///   The second of two double-precision floating-point numbers to compare.
        /// </param>
        /// <returns>
        ///   A value between a and b inclusively. If a, b, or both a and b are
        ///   equal to <see cref="double.NaN"/>, <see cref="double.NaN"/> is
        ///   returned.
        /// </returns>
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
        public static double SafeClamp(this double value, double a, double b)
        {
            return a < b ? Math.Min(Math.Max(value, a), b) : Math.Max(Math.Min(value, a), b);
        }

        /// <summary>
        ///   Returns the value constrained inclusively between two single-precision
        ///   floating-point numbers.
        /// </summary>
        /// <param name="value">The value to restrict between a and b.</param>
        /// <param name="a">
        ///   The first of two single-precision floating-point numbers to compare.
        /// </param>
        /// <param name="b">
        ///   The second of two single-precision floating-point numbers to compare.
        /// </param>
        /// <returns>
        ///   A value between a and b inclusively. If a, b, or both a and b are
        ///   equal to <see cref="float.NaN"/>, <see cref="float.NaN"/> is returned.
        /// </returns>
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
        public static float SafeClamp(this float value, float a, float b)
        {
            return a < b ? Math.Min(Math.Max(value, a), b) : Math.Max(Math.Min(value, a), b);
        }

        /// <summary>
        ///   Returns the value constrained inclusively between two 16-bit signed
        ///   integers.
        /// </summary>
        /// <param name="value">The value to restrict between a and b.</param>
        /// <param name="a">
        ///   The first of two 16-bit signed integers to compare.
        /// </param>
        /// <param name="b">
        ///   The second of two 16-bit signed integers to compare.
        /// </param>
        /// <returns>A value between a and b inclusively.</returns>
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
        public static short Clamp(this short value, short a, short b)
        {
            return a < b ? Math.Min(Math.Max(value, a), b) : Math.Max(Math.Min(value, a), b);
        }

        /// <summary>
        ///   Returns the value constrained inclusively between two 32-bit signed
        ///   integers.
        /// </summary>
        /// <param name="value">The value to restrict between a and b.</param>
        /// <param name="a">
        ///   The first of two 32-bit signed integers to compare.
        /// </param>
        /// <param name="b">
        ///   The second of two 32-bit signed integers to compare.
        /// </param>
        /// <returns>A value between a and b inclusively.</returns>
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
        public static int Clamp(this int value, int a, int b)
        {
            return a < b ? Math.Min(Math.Max(value, a), b) : Math.Max(Math.Min(value, a), b);
        }

        /// <summary>
        ///   Returns the value constrained inclusively between two 64-bit signed
        ///   integers.
        /// </summary>
        /// <param name="value">The value to restrict between a and b.</param>
        /// <param name="a">
        ///   The first of two 64-bit signed integers to compare.
        /// </param>
        /// <param name="b">
        ///   The second of two 64-bit signed integers to compare.
        /// </param>
        /// <returns>A value between a and b inclusively.</returns>
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
        public static long Clamp(this long value, long a, long b)
        {
            return a < b ? Math.Min(Math.Max(value, a), b) : Math.Max(Math.Min(value, a), b);
        }

        /// <summary>
        ///   Returns the value constrained inclusively between two 8-bit signed
        ///   integers.
        /// </summary>
        /// <param name="value">The value to restrict between a and b.</param>
        /// <param name="a">
        ///   The first of two 8-bit signed integers to compare.
        /// </param>
        /// <param name="b">
        ///   The second of two 8-bit signed integers to compare.
        /// </param>
        /// <returns>A value between a and b inclusively.</returns>
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
        public static sbyte Clamp(this sbyte value, sbyte a, sbyte b)
        {
            return a < b ? Math.Min(Math.Max(value, a), b) : Math.Max(Math.Min(value, a), b);
        }

        /// <summary>
        ///   Returns the value constrained inclusively between two 16-bit unsigned
        ///   integers.
        /// </summary>
        /// <param name="value">The value to restrict between a and b.</param>
        /// <param name="a">
        ///   The first of two 16-bit unsigned integers to compare.
        /// </param>
        /// <param name="b">
        ///   The second of two 16-bit unsigned integers to compare.
        /// </param>
        /// <returns>A value between a and b inclusively.</returns>
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
        public static ushort Clamp(this ushort value, ushort a, ushort b)
        {
            return a < b ? Math.Min(Math.Max(value, a), b) : Math.Max(Math.Min(value, a), b);
        }

        /// <summary>
        ///   Returns the value constrained inclusively between two 32-bit unsigned
        ///   integers.
        /// </summary>
        /// <param name="value">The value to restrict between a and b.</param>
        /// <param name="a">
        ///   The first of two 32-bit unsigned integers to compare.
        /// </param>
        /// <param name="b">
        ///   The second of two 32-bit unsigned integers to compare.
        /// </param>
        /// <returns>A value between a and b inclusively.</returns>
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
        public static uint Clamp(this uint value, uint a, uint b)
        {
            return a < b ? Math.Min(Math.Max(value, a), b) : Math.Max(Math.Min(value, a), b);
        }

        /// <summary>
        ///   Returns the value constrained inclusively between two 64-bit unsigned
        ///   integers.
        /// </summary>
        /// <param name="value">The value to restrict between a and b.</param>
        /// <param name="a">
        ///   The first of two 64-bit unsigned integers to compare.
        /// </param>
        /// <param name="b">
        ///   The second of two 64-bit unsigned integers to compare.
        /// </param>
        /// <returns>A value between a and b inclusively.</returns>
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
        public static ulong Clamp(this ulong value, ulong a, ulong b)
        {
            return a < b ? Math.Min(Math.Max(value, a), b) : Math.Max(Math.Min(value, a), b);
        }

        #endregion

        #region Comparison

        /// <summary>
        ///   Determines whether an 8-bit signed integer is inclusively between
        ///   two values.
        /// </summary>
        /// <param name="value">An 8-bit signed integer to compare.</param>
        /// <param name="a">The first bound to compare value against.</param>
        /// <param name="b">The second bound to compare value against.</param>
        /// <returns>
        ///   A <see cref="bool"/> indicating whether <paramref name="value"/>
        ///   is inclusively between <paramref name="a"/> and <paramref name="b"/>.
        /// </returns>
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
        public static bool IsBetween(this sbyte value, sbyte a, sbyte b)
        {
            return a < b ? a <= value && value <= b : b <= value && value <= a;
        }

        /// <summary>
        ///   Determines whether an 8-bit unsigned integer is inclusively between
        ///   two values.
        /// </summary>
        /// <param name="value">An 8-bit unsigned integer to compare.</param>
        /// <param name="a">The first bound to compare value against.</param>
        /// <param name="b">The second bound to compare value against.</param>
        /// <returns>
        ///   A <see cref="bool"/> indicating whether <paramref name="value"/>
        ///   is inclusively between <paramref name="a"/> and <paramref name="b"/>.
        /// </returns>
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
        public static bool IsBetween(this byte value, byte a, byte b)
        {
            return a < b ? a <= value && value <= b : b <= value && value <= a;
        }

        /// <summary>
        ///   Determines whether a 16-bit signed integer is inclusively between
        ///   two values.
        /// </summary>
        /// <param name="value">A 16-bit signed integer to compare.</param>
        /// <param name="a">The first bound to compare value against.</param>
        /// <param name="b">The second bound to compare value against.</param>
        /// <returns>
        ///   A <see cref="bool"/> indicating whether <paramref name="value"/>
        ///   is inclusively between <paramref name="a"/> and <paramref name="b"/>.
        /// </returns>
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
        public static bool IsBetween(this short value, short a, short b)
        {
            return a < b ? a <= value && value <= b : b <= value && value <= a;
        }

        /// <summary>
        ///   Determines whether a 16-bit unsigned integer is inclusively between
        ///   two values.
        /// </summary>
        /// <param name="value">A 16-bit unsigned integer to compare.</param>
        /// <param name="a">The first bound to compare value against.</param>
        /// <param name="b">The second bound to compare value against.</param>
        /// <returns>
        ///   A <see cref="bool"/> indicating whether <paramref name="value"/>
        ///   is inclusively between <paramref name="a"/> and <paramref name="b"/>.
        /// </returns>
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
        public static bool IsBetween(this ushort value, ushort a, ushort b)
        {
            return a < b ? a <= value && value <= b : b <= value && value <= a;
        }

        /// <summary>
        ///   Determines whether a 32-bit signed integer is inclusively between
        ///   two values.
        /// </summary>
        /// <param name="value">A 32-bit signed integer to compare.</param>
        /// <param name="a">The first bound to compare value against.</param>
        /// <param name="b">The second bound to compare value against.</param>
        /// <returns>
        ///   A <see cref="bool"/> indicating whether <paramref name="value"/>
        ///   is inclusively between <paramref name="a"/> and <paramref name="b"/>.
        /// </returns>
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
        public static bool IsBetween(this int value, int a, int b)
        {
            return a < b ? a <= value && value <= b : b <= value && value <= a;
        }

        /// <summary>
        ///   Determines whether a 32-bit unsigned integer is inclusively between
        ///   two values.
        /// </summary>
        /// <param name="value">A 32-bit unsigned integer to compare.</param>
        /// <param name="a">The first bound to compare value against.</param>
        /// <param name="b">The second bound to compare value against.</param>
        /// <returns>
        ///   A <see cref="bool"/> indicating whether <paramref name="value"/>
        ///   is inclusively between <paramref name="a"/> and <paramref name="b"/>.
        /// </returns>
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
        public static bool IsBetween(this uint value, uint a, uint b)
        {
            return a < b ? a <= value && value <= b : b <= value && value <= a;
        }

        /// <summary>
        ///   Determines whether a 64-bit signed integer is inclusively between
        ///   two values.
        /// </summary>
        /// <param name="value">A 64-bit signed integer to compare.</param>
        /// <param name="a">The first bound to compare value against.</param>
        /// <param name="b">The second bound to compare value against.</param>
        /// <returns>
        ///   A <see cref="bool"/> indicating whether <paramref name="value"/>
        ///   is inclusively between <paramref name="a"/> and <paramref name="b"/>.
        /// </returns>
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
        public static bool IsBetween(this long value, long a, long b)
        {
            return a < b ? a <= value && value <= b : b <= value && value <= a;
        }

        /// <summary>
        ///   Determines whether a 64-bit unsigned integer is inclusively between
        ///   two values.
        /// </summary>
        /// <param name="value">A 64-bit unsigned integer to compare.</param>
        /// <param name="a">The first bound to compare value against.</param>
        /// <param name="b">The second bound to compare value against.</param>
        /// <returns>
        ///   A <see cref="bool"/> indicating whether <paramref name="value"/>
        ///   is inclusively between <paramref name="a"/> and <paramref name="b"/>.
        /// </returns>
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
        public static bool IsBetween(this ulong value, ulong a, ulong b)
        {
            return a < b ? a <= value && value <= b : b <= value && value <= a;
        }

        /// <summary>
        ///   Determines whether a single-precision floating-point value is inclusively
        ///   between two values.
        /// </summary>
        /// <param name="value">
        ///   A double-precision floating-point value to compare.
        /// </param>
        /// <param name="a">The first bound to compare value against.</param>
        /// <param name="b">The second bound to compare value against.</param>
        /// <returns>
        ///   A <see cref="bool"/> indicating whether <paramref name="value"/>
        ///   is inclusively between <paramref name="a"/> and <paramref name="b"/>.
        /// </returns>
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
        public static bool IsBetween(this float value, float a, float b)
        {
            return a < b ? a <= value && value <= b : b <= value && value <= a;
        }

        /// <summary>
        ///   Determines whether a double-precision floating-point value is inclusively
        ///   between two values.
        /// </summary>
        /// <param name="value">
        ///   A double-precision floating-point value to compare.
        /// </param>
        /// <param name="a">The first bound to compare value against.</param>
        /// <param name="b">The second bound to compare value against.</param>
        /// <returns>
        ///   A <see cref="bool"/> indicating whether <paramref name="value"/>
        ///   is inclusively between <paramref name="a"/> and <paramref name="b"/>.
        /// </returns>
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
        public static bool IsBetween(this double value, double a, double b)
        {
            return a < b ? a <= value && value <= b : b <= value && value <= a;
        }

        /// <summary>
        ///   Determines whether a decimal value is inclusively between two values.
        /// </summary>
        /// <param name="value">A decimal value to compare.</param>
        /// <param name="a">The first bound to compare value against.</param>
        /// <param name="b">The second bound to compare value against.</param>
        /// <returns>
        ///   A <see cref="bool"/> indicating whether <paramref name="value"/>
        ///   is inclusively between <paramref name="a"/> and <paramref name="b"/>.
        /// </returns>
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
        public static bool IsBetween(this decimal value, decimal a, decimal b)
        {
            return a < b ? a <= value && value <= b : b <= value && value <= a;
        }

        /// <summary>
        ///   Returns a value indicating whether the specified number is not a
        ///   number (<see cref="float.NaN"/>).
        /// </summary>
        /// <param name="value">A single-precision floating-point number.</param>
        /// <returns>
        ///   <see langword="true"/> if value evaluates to not a number
        ///   (<see cref="float.NaN"/>); otherwise, <see langword="false"/>.
        /// </returns>
        /// <filterpriority>1</filterpriority>
        public static bool IsNaN(this float value)
        {
            return float.IsNaN(value);
        }

        /// <summary>
        ///   Returns a value indicating whether the specified number is not a
        ///   number (<see cref="double.NaN"/>).
        /// </summary>
        /// <param name="value">A double-precision floating-point number.</param>
        /// <returns>
        ///   <see langword="true"/> if value evaluates to <see cref="double.NaN"/>;
        ///   otherwise, <see langword="false"/>.
        /// </returns>
        /// <filterpriority>1</filterpriority>
        public static bool IsNaN(this double value)
        {
            return double.IsNaN(value);
        }

        #endregion

        public static Point ComputeCentroid(this IEnumerable<Point> points)
        {
            Point p;
            int count = 0;
            foreach (Point point in points) {
                p.X += point.X;
                p.Y += point.Y;
                ++count;
            }

            if (count != 0) {
                p.X /= count;
                p.Y /= count;
            }

            return p;
        }
    }
}
