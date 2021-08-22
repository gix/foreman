namespace Foreman.Controls
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.Design;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Controls.Primitives;
    using System.Windows.Data;
    using System.Windows.Documents;
    using System.Windows.Input;
    using System.Windows.Media;
    using Extensions;

    public interface IInteractiveElement
    {
        bool IsSelectable { get; }
        bool IsSelected { get; set; }
        bool IsDraggable { get; }
    }

    public interface IPlacedElement
    {
        Point Position { get; set; }
        double RenderWidth { get; set; }
        double RenderHeight { get; set; }
    }

    public interface IContextElement
    {
        void HandleRightClick(UIElement container);
    }

    public interface IInteractiveCanvasViewModel
    {
        void Select(object item);
        void Unselect(object item);
        void UnselectAll();
        void DeleteSelected();
        void CopySelected();
        List<object>? Paste();
    }

    public class InteractiveCanvasView : ItemsControl
    {
        private const double ZoomStrength = 1.1;
        private const double SnapDistance = 10;

        private readonly PanHandler panHandler;
        private readonly DragHandler dragHandler;
        private readonly RectangleSelectionHandler selectionHandler;
        private MouseGestureHandler? gestureHandler;

        private ZoomableCanvas? canvas;
        private InteractiveCanvasItem? frontItem;

        static InteractiveCanvasView()
        {
            Type forType = typeof(InteractiveCanvasView);
            DefaultStyleKeyProperty.OverrideMetadata(
                forType, new FrameworkPropertyMetadata(forType));
        }

        public InteractiveCanvasView()
        {
            panHandler = new PanHandler(this);
            dragHandler = new DragHandler(this);
            selectionHandler = new RectangleSelectionHandler(this);
        }

        public static readonly DependencyProperty ScaleProperty =
            ZoomableCanvas.ScaleProperty.AddOwner(typeof(InteractiveCanvasView));

        public static readonly DependencyProperty OffsetProperty =
            ZoomableCanvas.OffsetProperty.AddOwner(typeof(InteractiveCanvasView));

        public static readonly DependencyProperty ViewboxProperty =
            ZoomableCanvas.ViewboxProperty.AddOwner(typeof(InteractiveCanvasView));

        public static readonly DependencyProperty ActualViewboxProperty =
            ZoomableCanvas.ActualViewboxProperty.AddOwner(typeof(InteractiveCanvasView));

        public double Scale
        {
            get => (double)GetValue(ScaleProperty);
            set => SetValue(ScaleProperty, value);
        }

        public Vector Offset
        {
            get => (Vector)GetValue(OffsetProperty);
            set => SetValue(OffsetProperty, value);
        }

        public Rect Viewbox
        {
            get => (Rect)GetValue(ViewboxProperty);
            set => SetValue(ViewboxProperty, value);
        }

        public Rect ActualViewbox => (Rect)GetValue(ActualViewboxProperty);

        public Panel? ItemsHost => canvas;

        private IInteractiveCanvasViewModel? ViewModel => DataContext as IInteractiveCanvasViewModel;

        private List<InteractiveCanvasItem> SelectedItems { get; } =
            new();

        private IEnumerable<UIElement> SelectedDraggables
        {
            get
            {
                foreach (var item in SelectedItems) {
                    if (item.IsDraggable)
                        yield return item;
                }
            }
        }

        protected override DependencyObject GetContainerForItemOverride()
        {
            return new InteractiveCanvasItem();
        }

        protected override bool IsItemItsOwnContainerOverride(object item)
        {
            return item is InteractiveCanvasItem;
        }

        protected override void PrepareContainerForItemOverride(
            DependencyObject element, object item)
        {
            base.PrepareContainerForItemOverride(element, item);

            if (item is IPlacedElement) {
                BindingOperations.SetBinding(
                    element,
                    CanvasService.PositionProperty,
                    new Binding(nameof(IPlacedElement.Position)) {
                        Mode = BindingMode.TwoWay
                    });

                var bindings = PushBindings.GetBindings(element);
                bindings.Add(new PushBinding {
                    Source = item,
                    Path = new PropertyPath(nameof(IPlacedElement.RenderWidth)),
                    TargetDependencyProperty = ActualWidthProperty
                });
                bindings.Add(new PushBinding {
                    Source = item,
                    Path = new PropertyPath(nameof(IPlacedElement.RenderHeight)),
                    TargetDependencyProperty = ActualHeightProperty
                });
            }

            if (item is IInteractiveElement) {
                BindingOperations.SetBinding(
                    element,
                    Selector.IsSelectedProperty,
                    new Binding(nameof(IInteractiveElement.IsSelected)) {
                        Mode = BindingMode.TwoWay
                    });
            }
        }

        protected override void ClearContainerForItemOverride(
            DependencyObject element, object item)
        {
            if (item is IPlacedElement) {
                BindingOperations.ClearBinding(element, CanvasService.PositionProperty);
                PushBindings.GetBindings(element).Clear();
            }

            base.ClearContainerForItemOverride(element, item);
        }

        internal void HandleDeferredSelect(InteractiveCanvasItem clickedItem)
        {
            clickedItem.IsSelected = false;
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            if (canvas != null)
                canvas.ScaleChanged -= OnCanvasScaleChanged;

            canvas = GetTemplateChild("PART_Panel") as ZoomableCanvas;

            if (canvas != null)
                canvas.ScaleChanged += OnCanvasScaleChanged;
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            switch (e.Key) {
                case Key.Escape:
                    gestureHandler?.Reset();
                    break;

                case Key.Delete:
                    ViewModel?.DeleteSelected();
                    break;

                case Key.C:
                    if (Keyboard.Modifiers == ModifierKeys.Control)
                        ViewModel?.CopySelected();
                    break;

                case Key.V:
                    if (Keyboard.Modifiers == ModifierKeys.Control)
                        OnPaste();
                    break;
            }
        }

        private void OnPaste()
        {
            List<object>? itemsToSelect = ViewModel?.Paste();
            if (itemsToSelect == null || itemsToSelect.Count == 0)
                return;

            UnselectAll();
            foreach (var item in itemsToSelect) {
                if (ItemContainerGenerator.ContainerFromItem(item) is InteractiveCanvasItem container) {
                    Select(container);
                    BringToFront(container);
                }
            }
        }

        protected override void OnMouseDown(MouseButtonEventArgs args)
        {
            Focus();
            base.OnMouseDown(args);

            if (args.Handled)
                return;

            Point mousePos = args.GetPosition(this);
            var item = HitTestItem(mousePos);

            if (args.ChangedButton == MouseButton.Left) {
                switch (Keyboard.Modifiers) {
                    case ModifierKeys.None:
                        if (item == null) {
                            UnselectAll();
                        } else if (!item.IsSelected && item.IsSelectable) {
                            UnselectAll();
                            Select(item);
                            BringToFront(item);
                        }

                        if (item == null) {
                            panHandler.Start(args);
                            gestureHandler = panHandler;
                        } else if (item.IsDraggable) {
                            dragHandler.Start(mousePos, SelectedDraggables, item.IsSelected ? item : null);
                            gestureHandler = dragHandler;
                        }

                        break;

                    case ModifierKeys.Control:
                        if (item != null)
                            ToggleSelect(item);
                        else {
                            selectionHandler.Start(mousePos);
                            gestureHandler = selectionHandler;
                        }
                        break;
                }
            } else if (args.ChangedButton == MouseButton.Middle) {
                if (Keyboard.Modifiers == ModifierKeys.None) {
                    panHandler.Start(args);
                    gestureHandler = panHandler;
                }
            } else if (args.ChangedButton == MouseButton.Right) {
                if (Keyboard.Modifiers == ModifierKeys.None) {
                    if (item is not { IsSelected: true }) {
                        UnselectAll();
                        if (item != null)
                            Select(item);
                    }
                }
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            gestureHandler?.HandleMouseMove(e);
        }

        protected override void OnMouseUp(MouseButtonEventArgs e)
        {
            base.OnMouseUp(e);
            gestureHandler?.HandleMouseUp(e);

            if (e.Handled)
                return;

            if (e.ChangedButton == MouseButton.Right && Keyboard.Modifiers == ModifierKeys.None) {
                var container = HitTestItem(e.GetPosition(this));
                if (container?.DataContext is IContextElement ce)
                    ce.HandleRightClick(container);
            }
        }

        protected override void OnLostMouseCapture(MouseEventArgs e)
        {
            base.OnLostMouseCapture(e);
            gestureHandler?.HandleLostMouseCapture();
        }

        protected override void OnMouseWheel(MouseWheelEventArgs args)
        {
            base.OnMouseWheel(args);
            PerformZoom(args);
        }

        private void PerformZoom(MouseWheelEventArgs args)
        {
            double delta = (double)args.Delta / Mouse.MouseWheelDeltaForOneLine;
            double factor = Math.Pow(ZoomStrength, Math.Abs(delta));
            var position = args.GetPosition(this);

            if (args.Delta > 0)
                ZoomIn(factor, position);
            else
                ZoomOut(factor, position);
        }

        private void ZoomIn(double factor, Point center)
        {
            Point canvasPoint = canvas!.PointToCanvas(center);

            canvas.Scale = ClampScale(canvas.Scale * factor);

            Point newCenter = canvas.PointFromCanvas(canvasPoint);
            canvas.Offset += newCenter - center;
        }

        private void ZoomOut(double factor, Point center)
        {
            Point canvasPoint = canvas!.PointToCanvas(center);

            canvas.Scale = ClampScale(canvas.Scale / factor);

            Point newCenter = canvas.PointFromCanvas(canvasPoint);
            canvas.Offset += newCenter - center;
        }

        private static double ClampScale(double value)
        {
            value = value.Clamp(0.01, 5);
            if (DoubleUtils.AreClose(value, 1))
                value = 1;
            return value;
        }

        private void OnCanvasScaleChanged(object? sender, EventArgs args)
        {
            var formattingMode = DoubleUtils.AreClose(canvas!.Scale, 1) ?
                TextFormattingMode.Display : TextFormattingMode.Ideal;
            TextOptions.SetTextFormattingMode(canvas, formattingMode);
        }

        private void Select(InteractiveCanvasItem element)
        {
            element.IsSelected = true;
            SelectedItems.Add(element);
            if (element.DataContext != null)
                ViewModel?.Select(element.DataContext);
        }

        public void UnselectAll()
        {
            foreach (var item in SelectedItems)
                item.IsSelected = false;
            SelectedItems.Clear();
            ViewModel?.UnselectAll();
        }

        private void ToggleSelect(InteractiveCanvasItem element)
        {
            element.IsSelected = !element.IsSelected;
            if (element.IsSelected) {
                SelectedItems.Add(element);
                ViewModel?.Select(element.DataContext);
            } else {
                SelectedItems.Remove(element);
                ViewModel?.Unselect(element.DataContext);
            }
        }

        public Point PointToCanvas(Point visualPoint)
        {
            return canvas!.PointToCanvas(visualPoint);
        }

        public Point PointFromCanvas(Point visualPoint)
        {
            return canvas!.PointFromCanvas(visualPoint);
        }

        private InteractiveCanvasItem? HitTestItem(Point point)
        {
            foreach (var obj in this.HitTestDrawn<DependencyObject>(point)) {
                var item = obj.FindAncestor<InteractiveCanvasItem>();
                if (item != null)
                    return item;
            }

            return null;
        }

        private void BringToFront(InteractiveCanvasItem item)
        {
            frontItem?.ClearValue(Panel.ZIndexProperty);
            Panel.SetZIndex(item, 1);
            frontItem = item;
        }

        private abstract class MouseGestureHandler
        {
            public virtual bool IsActive { get; protected set; }

            public void HandleMouseMove(MouseEventArgs args)
            {
                if (!IsActive)
                    return;

                OnMouseMove(args);
            }

            public void HandleMouseUp(MouseButtonEventArgs args)
            {
                if (!IsActive)
                    return;

                OnMouseUp(args);
            }

            public virtual void HandleLostMouseCapture()
            {
                Reset();
            }

            public abstract void Reset();

            protected abstract void OnMouseMove(MouseEventArgs args);
            protected abstract void OnMouseUp(MouseButtonEventArgs args);
        }

        private class PanHandler : MouseGestureHandler
        {
            private readonly InteractiveCanvasView view;
            private Point startPosition;
            private Vector startOffset;

            public PanHandler(InteractiveCanvasView view)
            {
                this.view = view;
            }

            public void Start(MouseButtonEventArgs args)
            {
                startPosition = args.GetPosition(view);
                startOffset = view.canvas!.Offset;
                IsActive = view.CaptureMouse();
            }

            protected override void OnMouseMove(MouseEventArgs args)
            {
                Vector delta = args.GetPosition(view) - startPosition;
                view.canvas!.Offset = startOffset - delta;
            }

            protected override void OnMouseUp(MouseButtonEventArgs args)
            {
                if (args.ChangedButton != MouseButton.Left &&
                    args.ChangedButton != MouseButton.Middle)
                    return;

                Vector delta = args.GetPosition(view) - startPosition;
                view.canvas!.Offset = startOffset - delta;

                view.ReleaseMouseCapture();
            }

            public override void Reset()
            {
                IsActive = false;
                startPosition = new Point();
                startOffset = new Vector();
            }
        }

        private class DragHandler : MouseGestureHandler
        {
            private readonly InteractiveCanvasView view;
            private readonly List<Entry> draggedElements = new();

            private InteractiveCanvasItem? deferredSelectElement;
            private Point startPosition;

            public DragHandler(InteractiveCanvasView view)
            {
                this.view = view;
            }

            public void Start(
                Point startPos, IEnumerable<UIElement> elements,
                InteractiveCanvasItem? selectedElement)
            {
                IsActive = true;
                startPosition = startPos;
                deferredSelectElement = selectedElement;

                draggedElements.Clear();
                foreach (var element in elements)
                    draggedElements.Add(new Entry(element));

                if (!view.CaptureMouse())
                    Reset();
            }

            protected override void OnMouseMove(MouseEventArgs args)
            {
                if (!view.IsMouseCaptured && !view.CaptureMouse()) {
                    Reset();
                    return;
                }

                deferredSelectElement = null;

                Vector delta = args.GetPosition(view) - startPosition;
                delta /= view.canvas!.Scale;
                bool snapPosition = Keyboard.Modifiers == ModifierKeys.Control;

                foreach (var entry in draggedElements)
                    entry.Translate(delta, snapPosition);
            }

            protected override void OnMouseUp(MouseButtonEventArgs args)
            {
                if (args.ChangedButton != MouseButton.Left)
                    return;

                if (deferredSelectElement != null) {
                    view.UnselectAll();
                    view.Select(deferredSelectElement);
                    deferredSelectElement = null;
                }

                draggedElements.Clear();
                view.ReleaseMouseCapture();
            }

            public override void Reset()
            {
                foreach (var element in draggedElements)
                    element.ResetPosition();

                IsActive = false;
                draggedElements.Clear();
            }

            private readonly struct Entry
            {
                private readonly UIElement element;
                private readonly Point startingPosition;

                public Entry(UIElement element)
                {
                    this.element = element;
                    startingPosition = CanvasService.GetPosition(element);
                }

                public void Translate(Vector delta, bool snapPosition = false)
                {
                    Point newPosition = startingPosition + delta;
                    if (snapPosition)
                        newPosition = newPosition.RoundToNearest(SnapDistance);
                    CanvasService.SetPosition(element, newPosition);
                }

                public void ResetPosition()
                {
                    CanvasService.SetPosition(element, startingPosition);
                }
            }
        }

        private class RectangleSelectionHandler : MouseGestureHandler
        {
            private readonly InteractiveCanvasView view;
            private Point startPosition;
            private SelectionAdorner? adorner;

            public RectangleSelectionHandler(InteractiveCanvasView view)
            {
                this.view = view;
            }

            public void Start(Point mousePos)
            {
                if (IsActive)
                    throw new InvalidOperationException();

                IsActive = true;
                startPosition = mousePos;

                adorner = new SelectionAdorner(view);
                var layer = AdornerLayer.GetAdornerLayer(view)!;
                layer.Add(adorner);
                if (!view.CaptureMouse())
                    Reset();
            }

            private class SelectionAdorner : Adorner
            {
                public SelectionAdorner(UIElement adornedElement)
                    : base(adornedElement)
                {
                    UseLayoutRounding = true;
                    SnapsToDevicePixels = true;
                }

                private Rect bounds;

                public Rect Bounds
                {
                    get => bounds;
                    set
                    {
                        bounds = value;
                        InvalidateVisual();
                    }
                }

                protected override void OnRender(DrawingContext drawingContext)
                {
                    Rect rect = bounds;
                    if (rect.IsEmpty)
                        return;

                    drawingContext.DrawRectangle(
                        new SolidColorBrush(Color.FromArgb(0x66, 0x33, 0x99, 0xFF)),
                        new Pen(new SolidColorBrush(Color.FromArgb(0xFF, 0x33, 0x99, 0xFF)), 1),
                        new Rect(rect.Location + new Vector(0.5, 0.5), rect.Size));
                }
            }

            public override void Reset()
            {
                IsActive = false;
                startPosition = new Point();

                if (adorner != null)
                    AdornerLayer.GetAdornerLayer(view)!.Remove(adorner);
            }

            protected override void OnMouseMove(MouseEventArgs args)
            {
                var currPos = args.GetPosition(view);
                adorner!.Bounds = new Rect(startPosition, currPos);
            }

            protected override void OnMouseUp(MouseButtonEventArgs args)
            {
                if (args.ChangedButton != MouseButton.Left)
                    return;

                var endPos = args.GetPosition(view);
                var bounds = new Rect(startPosition, endPos);

                foreach (var obj in view.HitTestAll(bounds)) {
                    var element = obj.FindAncestor<InteractiveCanvasItem>();
                    if (element is { IsDraggable: true })
                        view.Select(element);
                }

                view.ReleaseMouseCapture();
            }
        }
    }
}
