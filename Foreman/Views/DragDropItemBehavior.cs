namespace Foreman.Views
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Windows;
    using System.Windows.Documents;
    using System.Windows.Media.Imaging;
    using Controls;
    using Extensions;
    using Microsoft.Xaml.Behaviors;

    public class DragDropItemBehavior : Behavior<InteractiveCanvasView>
    {
        private ContentAdorner dragAdorner;

        protected override void OnAttached()
        {
            base.OnAttached();
            AssociatedObject.DragEnter += OnDragEnter;
            AssociatedObject.DragOver += OnDragOver;
            AssociatedObject.DragLeave += OnDragLeave;
            AssociatedObject.Drop += OnDrop;
        }

        protected override void OnDetaching()
        {
            AssociatedObject.Drop += OnDrop;
            AssociatedObject.DragLeave += OnDragLeave;
            AssociatedObject.DragOver += OnDragOver;
            AssociatedObject.DragEnter += OnDragEnter;
            base.OnDetaching();
        }

        private void OnDragEnter(object sender, DragEventArgs e)
        {
            IEnumerable<BitmapSource> icons = null;
            if (e.Data.IsDataPresent<HashSet<Item>>())
                icons = e.Data.GetData<HashSet<Item>>().Select(x => x.Icon);
            else if (e.Data.IsDataPresent<HashSet<Recipe>>())
                icons = e.Data.GetData<HashSet<Recipe>>().Select(x => x.Icon);

            if (icons != null) {
                e.Effects = DragDropEffects.Copy;

                dragAdorner = new ContentAdorner(
                    AssociatedObject, new GhostElement(icons));

                var layer = AdornerLayer.GetAdornerLayer(AssociatedObject);
                layer.Add(dragAdorner);
            } else {
                e.Effects = DragDropEffects.None;
            }
        }

        private void OnDragOver(object sender, DragEventArgs e)
        {
            if (dragAdorner != null)
                dragAdorner.Position = e.GetPosition(AssociatedObject);
        }

        private void OnDragLeave(object sender, EventArgs e)
        {
            RemoveDragAdorner();
        }

        private async void OnDrop(object sender, DragEventArgs e)
        {
            var position = e.GetPosition(AssociatedObject);
            var canvasPos = AssociatedObject.PointToCanvas(position);
            var screenPos = AssociatedObject.PointToScreen(position);

            RemoveDragAdorner();
            await ((ProductionGraphViewModel)AssociatedObject.DataContext).OnDataDropped(
                e.Data, screenPos, canvasPos);
        }

        private void RemoveDragAdorner()
        {
            if (dragAdorner != null) {
                var layer = AdornerLayer.GetAdornerLayer(AssociatedObject);
                layer.Remove(dragAdorner);
                dragAdorner = null;
            }
        }
    }
}
