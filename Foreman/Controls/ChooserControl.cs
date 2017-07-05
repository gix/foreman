namespace Foreman
{
    using System.Drawing;
    using System.Windows.Forms;

    public class ChooserControl : UserControl
    {
        public string DisplayText { get; }
        public string FilterText { get; }

        public ChooserPanel ParentPanel { get; set; }

        public ChooserControl(string text, string filterText)
        {
            DisplayText = text;
            FilterText = filterText;
        }

        protected void RegisterMouseEvents(Control control)
        {
            control.MouseMove += MouseMoved;
            control.MouseClick += MouseClicked;

            foreach (Control subControl in control.Controls) {
                RegisterMouseEvents(subControl);
            }
        }

        private void MouseMoved(object sender, MouseEventArgs e)
        {
            ParentPanel.SelectedControl = this;
        }

        private void MouseClicked(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left) {
                ParentPanel.CallbackMethod.Invoke(this);
                ParentPanel.Dispose();
            }
        }

        private void InitializeComponent()
        {
            SuspendLayout();
            //
            // ChooserControl
            //
            AutoSize = true;
            AutoSizeMode = AutoSizeMode.GrowAndShrink;
            Name = "ChooserControl";
            Size = new Size(0, 0);
            ResumeLayout(false);
        }
    }
}
