namespace Foreman.Views
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Windows;
    using System.Windows.Interop;
    using System.Windows.Media;
    using System.Windows.Media.Imaging;
    using System.Windows.Threading;
    using Extensions;
    using Microsoft.Win32;

    public partial class ImageExportDialog
    {
        private readonly ProductionGraphViewModel sourceModel;

        public ImageExportDialog(ProductionGraphViewModel sourceModel)
        {
            InitializeComponent();
            this.sourceModel = sourceModel;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            var dialog = new SaveFileDialog();
            dialog.AddExtension = true;
            dialog.Filter = "PNG files (*.png)|*.png";
            dialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            dialog.FileName = "Foreman Production Flowchart.png";
            dialog.ValidateNames = true;
            dialog.OverwritePrompt = true;
            var result = dialog.ShowDialog();

            if (result == true) {
                fileTextBox.Text = dialog.FileName;
            }
        }

        private void ExportButton_Click(object sender, EventArgs e)
        {
            int scale = 1;
            if (Scale2xCheckBox.IsChecked == true) {
                scale = 2;
            } else if (Scale3xCheckBox.IsChecked == true) {
                scale = 3;
            }

            BitmapSource bitmap = RenderGraph(sourceModel, scale);

            if (!Directory.Exists(Path.GetDirectoryName(fileTextBox.Text))) {
                MessageBox.Show("Directory doesn't exist!");
            } else {
                try {
                    SaveAsPng(bitmap, fileTextBox.Text);
                    Close();
                } catch (Exception exception) {
                    MessageBox.Show("Error saving image: " + exception.Message);
                }
            }
        }

        private BitmapSource RenderGraph(ProductionGraphViewModel graph, double scale)
        {
            double oldScale = graph.Scale;
            Vector oldOffset = graph.Offset;
            graph.Scale = scale;
            graph.Offset = new Vector();

            BitmapSource bitmap;
            try {
                var graphViewer = CreateViewer(graph, scale, new Thickness(10));

                bitmap = RenderElement(graphViewer);

                graphViewer.DataContext = null;
            } finally {
                graph.Scale = oldScale;
                graph.Offset = oldOffset;
            }

            return bitmap;
        }

        private ProductionGraphViewer CreateViewer(
            ProductionGraphViewModel graph, double scale, Thickness margin)
        {
            var viewer = new ProductionGraphViewer();

            RenderOptions.SetBitmapScalingMode(viewer, BitmapScalingMode.Fant);
            TextOptions.SetTextFormattingMode(viewer, TextFormattingMode.Display);
            viewer.SnapsToDevicePixels = true;
            viewer.UseLayoutRounding = true;

            viewer.DataContext = graph;
            if (!TransparencyCheckBox.IsChecked == true)
                viewer.Background = Brushes.White;

            viewer.UpdateLayout();
            viewer.Dispatcher.Flush(DispatcherPriority.Render);

            Rect bounds = Rect.Empty;
            foreach (var node in graph.Elements.OfType<NodeElement>())
                bounds.Union(new Rect(node.Position - (Vector)node.RenderSize / 2, node.RenderSize));

            bounds.X *= scale;
            bounds.Y *= scale;
            bounds.X -= margin.Left;
            bounds.Y -= margin.Top;
            bounds.Width *= scale;
            bounds.Height *= scale;
            bounds.Width += margin.Left + margin.Right;
            bounds.Height += margin.Top + margin.Bottom;

            graph.Offset = new Vector(bounds.X, bounds.Y);
            viewer.Width = bounds.Width;
            viewer.Height = bounds.Height;
            viewer.UpdateLayout();

            return viewer;
        }

        private static void SaveAsPng(BitmapSource bitmap, string path)
        {
            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bitmap));
            using var output = File.Create(path);
            encoder.Save(output);
        }

        private static BitmapSource RenderElement(FrameworkElement element)
        {
            var rootVisual = VisualTreeHelper.GetParent(element) == null ? element : null;

            using var source = new HwndSource(new HwndSourceParameters()) {
                RootVisual = rootVisual
            };

            element.Dispatcher.Flush(DispatcherPriority.Render);

            var width = (int)element.ActualWidth;
            var height = (int)element.ActualHeight;
            var rtb = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
            rtb.Render(element);
            return rtb;
        }
    }
}
