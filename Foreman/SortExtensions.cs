namespace Foreman
{
    using System;
    using System.Collections.Generic;

    public static class SortExtensions
    {
        public static List<T> SortBy<T, TResult>(
            this List<T> list, Func<T, TResult> selector) where TResult : IComparable<TResult>
        {
            if (list == null)
                throw new ArgumentNullException(nameof(list));
            if (selector == null)
                throw new ArgumentNullException(nameof(selector));
            list.Sort((x, y) => selector(x).CompareTo(selector(y)));
            return list;
        }

        public static IList<T> StableSortBy<T, TResult>(
            this IList<T> list, Func<T, TResult> selector) where TResult : IComparable<TResult>
        {
            if (list == null)
                throw new ArgumentNullException(nameof(list));
            if (selector == null)
                throw new ArgumentNullException(nameof(selector));
            return list.StableSort(new SelectorComparer<T, TResult>(selector));
        }

        public static IList<T> StableSort<T>(this IList<T> list)
        {
            if (list == null)
                throw new ArgumentNullException(nameof(list));
            return UncheckedMergeSort(list, 0, list.Count, Comparer<T>.Default);
        }

        public static IList<T> StableSort<T>(this IList<T> list, IComparer<T> comparer)
        {
            if (list == null)
                throw new ArgumentNullException(nameof(list));
            if (comparer == null)
                throw new ArgumentNullException(nameof(comparer));
            return UncheckedMergeSort(list, 0, list.Count, comparer);
        }

        public static IList<T> StableSort<T>(
            this IList<T> list, int index, int count, IComparer<T> comparer)
        {
            if (list == null)
                throw new ArgumentNullException(nameof(list));
            if (comparer == null)
                throw new ArgumentNullException(nameof(comparer));
            return UncheckedMergeSort(list, index, count, comparer);
        }

        public static IList<T> MergeSort<T>(this IList<T> list)
        {
            if (list == null)
                throw new ArgumentNullException(nameof(list));
            return UncheckedMergeSort(list, 0, list.Count, Comparer<T>.Default);
        }

        public static IList<T> MergeSort<T>(this IList<T> list, IComparer<T> comparer)
        {
            if (list == null)
                throw new ArgumentNullException(nameof(list));
            if (comparer == null)
                throw new ArgumentNullException(nameof(comparer));
            return UncheckedMergeSort(list, 0, list.Count, comparer);
        }

        public static IList<T> MergeSort<T>(
            this IList<T> list, int index, int count, IComparer<T> comparer)
        {
            if (list == null)
                throw new ArgumentNullException(nameof(list));
            if (comparer == null)
                throw new ArgumentNullException(nameof(comparer));
            if (index < 0)
                throw new ArgumentOutOfRangeException(nameof(comparer));
            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(comparer));
            if (list.Count - index >= count)
                throw new ArgumentException(
                    "Range denoted by index and count is out of bounds.", nameof(comparer));
            return UncheckedMergeSort(list, index, count, comparer);
        }

        private static IList<T> UncheckedMergeSort<T>(
            this IList<T> list, int index, int count, IComparer<T> comparer)
        {
            if (count == 0)
                return list;

            List<T> sorted = Foreman.MergeSort.Sort(list, index, index + count, comparer);
            for (int i = 0; i < sorted.Count; ++i)
                list[i] = sorted[i];
            return list;
        }
    }
}
