namespace Foreman.Views
{
    using System;
    using System.IO;
    using System.Windows;

    public partial class DirectoryChooserDialog
    {
        public DirectoryChooserDialog(string defaultDirectory, string title)
        {
            InitializeComponent();

            Title = title;
            SelectedPath = defaultDirectory;
            DirTextBox.Text = SelectedPath;
        }

        public string SelectedPath { get; private set; }

        private void BrowseButton_Click(object sender, EventArgs e)
        {
            using var dialog = new System.Windows.Forms.FolderBrowserDialog();
            if (Directory.Exists(SelectedPath)) {
                dialog.SelectedPath = SelectedPath;
            }
            var result = dialog.ShowDialog();

            if (result == System.Windows.Forms.DialogResult.OK) {
                DirTextBox.Text = dialog.SelectedPath;
                SelectedPath = dialog.SelectedPath;
            }
        }

        private void OKButton_Click(object sender, EventArgs e)
        {
            if (!Directory.Exists(SelectedPath)) {
                MessageBox.Show("That directory doesn't seem to exist");
            } else {
                DialogResult = true;
                Close();
            }
        }

        private void DirTextBox_TextChanged(object sender, EventArgs e)
        {
            SelectedPath = DirTextBox.Text;
        }
    }
}
