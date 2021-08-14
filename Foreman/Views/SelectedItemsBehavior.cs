namespace Foreman
{
    using System.Collections;
    using System.Collections.Specialized;
    using System.Windows;
    using System.Windows.Controls;
    using Microsoft.Xaml.Behaviors;

    public class SelectedItemsBehavior : Behavior<ListBox>
    {
        public static readonly DependencyProperty SelectedItemsProperty =
            DependencyProperty.Register(
                nameof(SelectedItems),
                typeof(IList),
                typeof(SelectedItemsBehavior),
                new FrameworkPropertyMetadata(null, OnSelectedItemsChanged));

        public IList? SelectedItems
        {
            get => (IList?)GetValue(SelectedItemsProperty);
            set => SetValue(SelectedItemsProperty, value);
        }

        protected override void OnAttached()
        {
            base.OnAttached();
            AssociatedObject.SelectionChanged += OnSelectionChanged;
            AttachToList(SelectedItems);
        }

        protected override void OnDetaching()
        {
            AssociatedObject.SelectionChanged -= OnSelectionChanged;
            base.OnDetaching();
        }

        private void OnSelectionChanged(object sender, SelectionChangedEventArgs args)
        {
            var list = SelectedItems;
            if (list == null)
                return;

            if (args.RemovedItems != null)
                foreach (var item in args.RemovedItems)
                    list.Remove(item);
            if (args.AddedItems != null)
                foreach (var item in args.AddedItems)
                    list.Add(item);
        }

        private static void OnSelectedItemsChanged(
            DependencyObject d, DependencyPropertyChangedEventArgs args)
        {
            var source = (SelectedItemsBehavior)d;
            source.OnSelectedItemsChanged((IList)args.OldValue, (IList)args.NewValue);
        }

        private void OnSelectedItemsChanged(IList oldList, IList newList)
        {
            if (AssociatedObject == null)
                return;

            DetachFromList(oldList);
            AttachToList(newList);
        }

        private void OnCollectionChanged(
            object? sender, NotifyCollectionChangedEventArgs args)
        {
            if (args.OldItems != null) {
                foreach (var item in args.OldItems)
                    AssociatedObject.SelectedItems.Remove(item);
            }

            if (args.NewItems != null) {
                foreach (var item in args.NewItems)
                    AssociatedObject.SelectedItems.Add(item);
            }
        }

        private void DetachFromList(IList list)
        {
            if (list is INotifyCollectionChanged ncc)
                ncc.CollectionChanged -= OnCollectionChanged;

            AssociatedObject.SelectedItems.Clear();
        }

        private void AttachToList(IList? list)
        {
            if (list is INotifyCollectionChanged ncc)
                ncc.CollectionChanged += OnCollectionChanged;

            if (list != null) {
                foreach (var item in list)
                    AssociatedObject.SelectedItems.Add(item);
            }
        }
    }
}
