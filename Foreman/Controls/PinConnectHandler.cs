namespace Foreman.Controls
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Data;
    using System.Windows.Documents;
    using System.Windows.Input;
    using System.Windows.Interactivity;
    using System.Windows.Media;
    using Extensions;

    internal class PinConnectHandler : Behavior<InteractiveCanvasView>
    {
        private enum DragState
        {
            None,
            Preparing,
            Dragging
        }

        private DragState state;
        private FrameworkElementAdorner connectorAdorner;
        private Point startPosition;
        private Pin sourcePin;
        private Pin targetPin;

        private bool IsActive => state != DragState.None;

        protected override void OnAttached()
        {
            base.OnAttached();
            AssociatedObject.PreviewMouseDown += OnMouseDown;
            AssociatedObject.MouseMove += OnMouseMove;
            AssociatedObject.MouseUp += OnMouseUp;
            AssociatedObject.LostMouseCapture += OnLostMouseCapture;
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();
            AssociatedObject.LostMouseCapture -= OnLostMouseCapture;
            AssociatedObject.MouseUp -= OnMouseUp;
            AssociatedObject.MouseMove -= OnMouseMove;
            AssociatedObject.PreviewMouseDown -= OnMouseDown;
        }

        private bool MatchesGesture(MouseButtonEventArgs args)
        {
            return args.ChangedButton == MouseButton.Left &&
                   Keyboard.Modifiers == ModifierKeys.None;
        }

        private void OnMouseDown(object sender, MouseButtonEventArgs args)
        {
            if (args.Handled || !MatchesGesture(args))
                return;

            var mousePos = args.GetPosition(AssociatedObject);
            var pin = FindPin(mousePos);
            if (pin != null) {
                PrepareDrag(pin);
                args.Handled = true;
            }
        }

        private void OnMouseMove(object sender, MouseEventArgs args)
        {
            if (!IsActive)
                return;

            var position = args.GetPosition(AssociatedObject);
            var currCanvasPos = AssociatedObject.PointToCanvas(position);
            if (state == DragState.Preparing) {
                var delta = (currCanvasPos - startPosition).Abs();
                if (delta.IsGreaterThanDragDistance())
                    StartDrag();
            }

            if (state == DragState.Dragging)
                Update(position, currCanvasPos);
        }

        private async void OnMouseUp(object sender, MouseButtonEventArgs args)
        {
            if (!IsActive)
                return;

            if (state != DragState.Dragging) {
                Reset();
                return;
            }

            await FinishDrag(args);
        }

        private void OnLostMouseCapture(object sender, MouseEventArgs args)
        {
            if (IsActive)
                Reset();
        }

        private Pin FindPin(Point point)
        {
            return AssociatedObject.HitTestDataContext<NodeElement, Pin>(point);
        }

        private void Reset(bool keepAdorner = false)
        {
            state = DragState.None;
            startPosition = new Point();
            sourcePin = null;
            if (targetPin != null) {
                targetPin.IsHighlighted = false;
                targetPin = null;
            }

            if (!keepAdorner) {
                var layer = AdornerLayer.GetAdornerLayer(AssociatedObject);
                if (connectorAdorner != null)
                    layer.Remove(connectorAdorner);
                connectorAdorner = null;
            }

            AssociatedObject.ReleaseMouseCapture();
        }

        private void PrepareDrag(Pin source)
        {
            if (state != DragState.None)
                Reset();

            sourcePin = source;
            startPosition = sourcePin.Hotspot;
            state = DragState.Preparing;
            if (!AssociatedObject.CaptureMouse())
                Reset();
        }

        private void StartDrag()
        {
            var brush = new SolidColorBrush(
                DataCache.IconAverageColor(sourcePin.Item.Icon));

            var connectorShape = new CurvedConnectorShape {
                Fill = brush,
                Direction = ConnectorShapeDirection.Upwards,
                Thickness = 3
            };
            connectorShape.SetBinding(UIElement.RenderTransformProperty, new Binding {
                Path = new PropertyPath(UIElement.RenderTransformProperty),
                Source = AssociatedObject.ItemsHost,
                Mode = BindingMode.OneWay
            });
            connectorShape.SetBinding(UIElement.RenderTransformOriginProperty, new Binding {
                Path = new PropertyPath(UIElement.RenderTransformOriginProperty),
                Source = AssociatedObject.ItemsHost,
                Mode = BindingMode.OneWay
            });
            connectorAdorner = new FrameworkElementAdorner(
                AssociatedObject, new Canvas { Children = { connectorShape } });

            var layer = AdornerLayer.GetAdornerLayer(AssociatedObject);
            layer.Add(connectorAdorner);

            state = DragState.Dragging;
        }

        private void Update(Point position, Point canvasPos)
        {
            var connector = (ConnectorShape)((Canvas)connectorAdorner.Child).Children[0];
            if (sourcePin.Kind == PinKind.Output)
                connector.Points = new PointCollection { startPosition, canvasPos };
            else
                connector.Points = new PointCollection { canvasPos, startPosition };

            Pin candidatePin = FindPin(position);

            if (targetPin != candidatePin && IsValidTarget(sourcePin, candidatePin)) {
                if (targetPin != null)
                    targetPin.IsHighlighted = false;

                targetPin = candidatePin;

                if (targetPin != null)
                    targetPin.IsHighlighted = true;
            }
        }

        private async Task FinishDrag(MouseButtonEventArgs args)
        {
            var viewModel = AssociatedObject.DataContext as ProductionGraphViewModel;
            if (viewModel == null)
                return;

            Pin source = sourcePin;
            Pin target = targetPin;
            if (target == null) {
                var position = args.GetPosition(AssociatedObject);
                var canvasPos = AssociatedObject.PointToCanvas(position);
                var screenPos = AssociatedObject.PointToScreen(position);
                try {
                    Reset(keepAdorner: true);
                    await viewModel.SuggestConnect(source, canvasPos, screenPos);
                } finally {
                    Reset();
                }
            } else {
                Reset();
                viewModel.Connect(source, target);
            }
        }

        private static bool IsValidTarget(Pin source, Pin target)
        {
            if (target == null)
                return true;
            if (source == target)
                return false;

            bool compatibleKinds =
                (source.Kind == PinKind.Input && target.Kind == PinKind.Output) ||
                (source.Kind == PinKind.Output && target.Kind == PinKind.Input);

            return compatibleKinds && source.Item == target.Item;
        }
    }
}
