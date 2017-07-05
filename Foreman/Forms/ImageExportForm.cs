namespace Foreman
{
    using System;
    using System.Drawing;
    using System.Drawing.Drawing2D;
    using System.Drawing.Imaging;
    using System.IO;
    using System.Windows.Forms;

    public partial class ImageExportForm : Form
    {
        private readonly ProductionGraphViewer graphViewer;

        public ImageExportForm(ProductionGraphViewer graphViewer)
        {
            InitializeComponent();
            this.graphViewer = graphViewer;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            using (SaveFileDialog dialog = new SaveFileDialog()) {
                dialog.AddExtension = true;
                dialog.Filter = "PNG files (*.png)|*.png";
                dialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                dialog.FileName = "Foreman Production Flowchart.png";
                dialog.ValidateNames = true;
                dialog.OverwritePrompt = true;
                var result = dialog.ShowDialog();

                if (result == DialogResult.OK) {
                    fileTextBox.Text = dialog.FileName;
                }
            }
        }

        private void ExportButton_Click(object sender, EventArgs e)
        {
            int scale = 1;
            if (Scale2xCheckBox.Checked) {
                scale = 2;
            } else if (Scale3xCheckBox.Checked) {
                scale = 3;
            }

            Bitmap image = new Bitmap(graphViewer.GraphBounds.Width * scale, graphViewer.GraphBounds.Height * scale);
            using (Graphics graphics = Graphics.FromImage(image)) {
                graphics.ScaleTransform(scale, scale);
                graphics.TranslateTransform(-graphViewer.GraphBounds.X, -graphViewer.GraphBounds.Y);
                graphics.SmoothingMode = SmoothingMode.HighQuality;

                if (!TransparencyCheckBox.Checked) {
                    graphics.Clear(Color.White);
                }

                graphViewer.Paint(graphics);

                if (!Directory.Exists(Path.GetDirectoryName(fileTextBox.Text))) {
                    MessageBox.Show("Directory doesn't exist!");
                } else {
                    try {
                        image.Save(fileTextBox.Text, ImageFormat.Png);
                        Close();
                    } catch (Exception exception) {
                        MessageBox.Show("Error saving image: " + exception.Message);
                    }
                }
            }
        }
    }
}
