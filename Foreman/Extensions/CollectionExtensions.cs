namespace Foreman.Extensions
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public static class CollectionExtensions
    {
        public static void AddRange<T>(this ICollection<T> list, IEnumerable<T> items)
        {
            if (list is List<T> l)
                l.AddRange(list);
            else {
                foreach (var item in items)
                    list.Add(item);
            }
        }

        public static HashSet<T> ToSet<T>(this IEnumerable<T> enumerable)
        {
            return new(enumerable);
        }

        public static HashSet<TValue> ToSet<T, TValue>(
            this IEnumerable<T> enumerable, Func<T, TValue> selector)
        {
            return new(enumerable.Select(selector));
        }

        public static int RemoveWhere<T>(this ICollection<T> collection, Predicate<T> match)
        {
            if (collection == null)
                throw new ArgumentNullException(nameof(collection));
            if (match == null)
                throw new ArgumentNullException(nameof(match));

            if (collection.IsReadOnly)
                throw new NotSupportedException("Collection is read-only.");

            int removed = 0;

            switch (collection) {
                case HashSet<T> set:
                    return set.RemoveWhere(match);
                case SortedSet<T> sortedSet:
                    return sortedSet.RemoveWhere(match);
                case List<T> listT:
                    return listT.RemoveAll(match);

                case IList<T> list:
                    for (int i = list.Count - 1; i >= 0; --i) {
                        if (match(list[i])) {
                            list.RemoveAt(i);
                            ++removed;
                        }
                    }
                    return removed;

                default:
                    var items = collection.Where(x => match(x)).ToList();
                    foreach (T item in items) {
                        if (collection.Remove(item))
                            ++removed;
                    }

                    return removed;
            }
        }

        public static TValue GetValueOrDefault<TKey, TValue>(
            this IDictionary<TKey, TValue> dictionary, TKey key, TValue defaultValue = default)
        {
            if (dictionary.TryGetValue(key, out TValue value))
                return value;
            return defaultValue;
        }

        public static TValue GetValueOrDefault<TKey, TValue>(
            this IDictionary<TKey, TValue> dictionary, TKey key,
            Func<TKey, TValue> defaultValueFactory)
        {
            if (dictionary.TryGetValue(key, out TValue value))
                return value;
            return defaultValueFactory(key);
        }

        public static TValue GetOrAdd<TKey, TValue>(
            this IDictionary<TKey, TValue> dictionary, TKey key, TValue value)
        {
            if (dictionary.TryGetValue(key, out TValue existingValue))
                return existingValue;
            dictionary.Add(key, value);
            return value;
        }

        public static TValue GetOrAdd<TKey, TValue>(
            this IDictionary<TKey, TValue> dictionary, TKey key,
            Func<TKey, TValue> valueFactory)
        {
            if (dictionary.TryGetValue(key, out TValue existingValue))
                return existingValue;
            TValue value = valueFactory(key);
            dictionary.Add(key, value);
            return value;
        }
    }
}
