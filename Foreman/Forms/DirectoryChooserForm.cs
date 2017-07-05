namespace Foreman
{
    using System;
    using System.IO;
    using System.Windows.Forms;

    public partial class DirectoryChooserForm : Form
    {
        public string SelectedPath { get; private set; }

        public DirectoryChooserForm(string defaultDirectory)
        {
            SelectedPath = defaultDirectory;
            InitializeComponent();

            DirTextBox.Text = SelectedPath;
        }

        private void BrowseButton_Click(object sender, EventArgs e)
        {
            using (FolderBrowserDialog dialog = new FolderBrowserDialog()) {
                if (Directory.Exists(SelectedPath)) {
                    dialog.SelectedPath = SelectedPath;
                }
                var result = dialog.ShowDialog();

                if (result == DialogResult.OK) {
                    DirTextBox.Text = dialog.SelectedPath;
                    SelectedPath = dialog.SelectedPath;
                }
            }
        }

        private void OKButton_Click(object sender, EventArgs e)
        {
            if (!Directory.Exists(SelectedPath)) {
                MessageBox.Show("That directory doesn't seem to exist");
            } else {
                DialogResult = DialogResult.OK;
                Close();
            }
        }

        private void DirTextBox_TextChanged(object sender, EventArgs e)
        {
            SelectedPath = DirTextBox.Text;
        }
    }
}
