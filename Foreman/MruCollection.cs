namespace Foreman
{
    using System.Collections;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Collections.Specialized;

    public class MruCollection<T> : IReadOnlyList<T>, INotifyCollectionChanged
    {
        private readonly ObservableCollection<T> items = new();
        private int capacity = 10;

        public event NotifyCollectionChangedEventHandler CollectionChanged
        {
            add => items.CollectionChanged += value;
            remove => items.CollectionChanged -= value;
        }

        public int Capacity
        {
            get => capacity;
            set
            {
                capacity = value;
                while (items.Count > capacity)
                    items.RemoveAt(items.Count - 1);
            }
        }

        public int Count => items.Count;

        public T this[int index] => items[index];

        public IEnumerator<T> GetEnumerator()
        {
            return items.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)items).GetEnumerator();
        }

        public void Add(T item)
        {
            if (items.Count > 0 && EqualityComparer<T>.Default.Equals(items[0], item))
                return;

            items.Remove(item);
            if (items.Count == Capacity)
                items.RemoveAt(items.Count - 1);
            items.Insert(0, item);
        }
    }
}