namespace Foreman.Units
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Extensions;

    public interface IQuantity : IComparable
    {
        double RawValue { get; }
    }

    public interface IQuantity<T>
       : IEquatable<T>, IComparable<T>, IQuantity
    {
    }

    public static class QuantityExtensions
    {
        public static T Average<T>(this IEnumerable<T> enumerable)
           where T : IQuantity
        {
            IEnumerable<double> values = enumerable.Select(x => x.RawValue);
            double average = values.Average();
            return (T)Activator.CreateInstance(typeof(T), average)!;
        }

        public static bool IsFinite(this IQuantity quantity)
        {
            return quantity.RawValue.IsFinite();
        }
    }
}
