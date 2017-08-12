namespace Foreman.Infrastructure
{
    using System;
    using System.Collections.ObjectModel;
    using System.Collections.Specialized;
    using System.ComponentModel;

    public delegate void ItemPropertyChangedHandler<in T>(T item, PropertyChangedEventArgs args);

    public class BindableCollection<T> : ObservableCollection<T>
        where T : INotifyPropertyChanged
    {
        public event ItemPropertyChangedHandler<T> ItemPropertyChanged;

        protected override void ClearItems()
        {
            foreach (var entry in this)
                AttachToChild(entry);
            base.ClearItems();
        }

        protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            base.OnCollectionChanged(e);
            if (e.OldItems != null) {
                foreach (T item in e.OldItems)
                    DetachFromChild(item);
            }
            if (e.NewItems != null) {
                foreach (T item in e.NewItems)
                    AttachToChild(item);
            }
        }

        protected virtual void AttachToChild(T item)
        {
            if (item != null)
                item.PropertyChanged += OnItemPropertyChanged;
        }

        protected virtual void DetachFromChild(T item)
        {
            if (item != null)
                item.PropertyChanged -= OnItemPropertyChanged;
        }

        private void OnItemPropertyChanged(
            object sender, PropertyChangedEventArgs args)
        {
            OnItemPropertyChanged((T)sender, args);
        }

        protected virtual void OnItemPropertyChanged(
            T item, PropertyChangedEventArgs args)
        {
            ItemPropertyChanged?.Invoke(item, args);
        }
    }
}