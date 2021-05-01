namespace Foreman.Controls
{
    using System.ComponentModel;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Controls.Primitives;
    using System.Windows.Input;

    public class InteractiveCanvasItem : ContentControl
    {
        private bool deferredSelect;
        private Point deferredSelectOrigin = new(double.PositiveInfinity, double.PositiveInfinity);

        private InteractiveCanvasView ParentCanvas =>
            ItemsControl.ItemsControlFromItemContainer(this) as InteractiveCanvasView;

        static InteractiveCanvasItem()
        {
            var forType = typeof(InteractiveCanvasItem);

            DefaultStyleKeyProperty.OverrideMetadata(
                forType, new FrameworkPropertyMetadata(forType));

            KeyboardNavigation.DirectionalNavigationProperty.OverrideMetadata(
                forType, new FrameworkPropertyMetadata(KeyboardNavigationMode.Once));
            KeyboardNavigation.TabNavigationProperty.OverrideMetadata(
                forType, new FrameworkPropertyMetadata(KeyboardNavigationMode.Local));
        }

        public static readonly DependencyProperty IsSelectedProperty =
            Selector.IsSelectedProperty.AddOwner(
                typeof(InteractiveCanvasItem),
                new FrameworkPropertyMetadata(
                    false,
                    FrameworkPropertyMetadataOptions.Journal |
                    FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                    OnIsSelectedChanged));

        public static readonly RoutedEvent SelectedEvent =
            Selector.SelectedEvent.AddOwner(typeof(InteractiveCanvasItem));

        public static readonly RoutedEvent UnselectedEvent =
            Selector.UnselectedEvent.AddOwner(typeof(InteractiveCanvasItem));

        [Bindable(true)]
        [Category("Appearance")]
        public bool IsSelected
        {
            get => (bool)GetValue(IsSelectedProperty);
            set => SetValue(IsSelectedProperty, value);
        }

        public bool IsDraggable => (DataContext as IInteractiveElement)?.IsDraggable ?? false;
        public bool IsSelectable => (DataContext as IInteractiveElement)?.IsSelectable ?? false;

        private static void OnIsSelectedChanged(
            DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var container = (InteractiveCanvasItem)d;
            bool newValue = (bool)e.NewValue;
            if (newValue)
                container.OnSelected(new RoutedEventArgs(Selector.SelectedEvent, container));
            else
                container.OnUnselected(new RoutedEventArgs(Selector.UnselectedEvent, container));
        }

        protected virtual void OnSelected(RoutedEventArgs e)
        {
            HandleIsSelectedChanged(true, e);
        }

        protected virtual void OnUnselected(RoutedEventArgs e)
        {
            HandleIsSelectedChanged(false, e);
        }

        private void HandleIsSelectedChanged(bool newValue, RoutedEventArgs e)
        {
            RaiseEvent(e);
        }

        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonDown(e);
            return;

            //if (IsSelected && Keyboard.Modifiers == ModifierKeys.None) {
            //    // Suppress the default selection behavior and defer it until
            //    // the next mouse up event.
            //    e.Handled = true;
            //    deferredSelect = true;
            //    deferredSelectOrigin = e.GetPosition(ParentCanvas);
            //    if (SelectorUtils.IsSelectable(this))
            //        Focus();
            //} else
            //    deferredSelect = false;

            //base.OnMouseLeftButtonDown(e);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (deferredSelect && e.GetPosition(ParentCanvas) != deferredSelectOrigin)
                deferredSelect = false;
        }

        protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
        {
            if (deferredSelect)
                ParentCanvas.HandleDeferredSelect(this);

            deferredSelectOrigin = new Point(double.PositiveInfinity, double.PositiveInfinity);
            deferredSelect = false;

            base.OnMouseLeftButtonUp(e);
        }
    }
}