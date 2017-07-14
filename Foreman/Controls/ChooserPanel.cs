namespace Foreman
{
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.Linq;
    using System.Windows.Forms;

    public partial class ChooserPanel : Form
    {
        public Action<ChooserControl> CallbackMethod { get; set; }

        private readonly List<ChooserControl> controls;

        private ChooserControl selectedControl;

        public ChooserControl SelectedControl
        {
            get => selectedControl;
            set
            {
                if (selectedControl != null) {
                    selectedControl.BackColor = Color.White;
                }
                selectedControl = value;
                if (value != null) {
                    selectedControl.BackColor = Color.FromArgb(0xFF, 0xAE, 0xC6, 0xCF);
                }
            }
        }

        public ChooserPanel(IEnumerable<ChooserControl> controls, ProductionGraphViewer parent)
        {
            InitializeComponent();
            this.controls = controls.ToList();
            FilterTextBox.Focus();
        }

        private void ChooserPanel_Load(object sender, EventArgs e)
        {
            flowLayoutPanel1.SuspendLayout();
            foreach (ChooserControl control in controls) {
                flowLayoutPanel1.Controls.Add(control);
                control.ParentPanel = this;
                control.Dock = DockStyle.Top;
                control.Width = Width;
                RegisterKeyEvents(control);
            }

            flowLayoutPanel1.ResumeLayout();

            UpdateControlWidth();

            var location = Location;
            var screen = Screen.FromPoint(location);
            var bottomY = location.Y + Height;

            if (bottomY > screen.WorkingArea.Bottom)
                location.Y -= bottomY - screen.WorkingArea.Bottom;

            if (Location != location)
                Location = location;
        }

        private void UpdateControlWidth()
        {
            if (flowLayoutPanel1.Controls[flowLayoutPanel1.Controls.Count - 1].Bottom > 600) {
                flowLayoutPanel1.Padding = new Padding(Padding.Left, Padding.Top,
                    SystemInformation.VerticalScrollBarWidth, Padding.Bottom);
            } else {
                flowLayoutPanel1.Padding = new Padding(Padding.Left, Padding.Top, Padding.Left, Padding.Bottom);
            }
        }

        public void Show(Control placementTarget, Direction direction, Action<ChooserControl> callback)
        {
            Show(ComputeLocation(placementTarget, direction), callback);
        }

        public void Show(Point location, Action<ChooserControl> callback)
        {
            Location = location;
            Show();
            CallbackMethod = callback;
        }

        private Point ComputeLocation(Control placementTarget, Direction direction)
        {
            var location = new Point();
            switch (direction) {
                case Direction.Left:
                    location = new Point(-Width, 0);
                    location = placementTarget.PointToScreen(location);
                    break;
                case Direction.Right:
                    location = new Point(placementTarget.Width, 0);
                    location = placementTarget.PointToScreen(location);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(direction), direction, null);
            }

            return location;
        }

        protected override void OnDeactivate(EventArgs e)
        {
            base.OnDeactivate(e);
            Hide();
        }

        private void RegisterKeyEvents(Control control)
        {
            control.KeyDown += ChooserPanel_KeyDown;

            foreach (Control subControl in control.Controls) {
                RegisterKeyEvents(subControl);
            }
        }

        public void ChooserPanel_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.KeyCode) {
                case Keys.Escape:
                    CallbackMethod(null);
                    Dispose();
                    break;
                case Keys.Down:
                    SelectedControl = controls[Math.Min(controls.IndexOf(selectedControl) + 1, controls.Count - 1)];
                    break;
                case Keys.Up:
                    SelectedControl = controls[Math.Max(controls.IndexOf(selectedControl) - 1, 0)];
                    break;
                case Keys.Enter:
                    CallbackMethod(SelectedControl);
                    Dispose();
                    break;
                default:
                    FilterTextBox.Focus();
                    SendKeys.Send(e.KeyCode.ToString());
                    break;
            }
        }

        private void ChooserPanel_MouseMove(object sender, MouseEventArgs e)
        {
            SelectedControl = null;
        }

        private void ChooserPanel_MouseLeave(object sender, EventArgs e)
        {
            SelectedControl = null;
        }

        private void FilterTextBox_TextChanged(object sender, EventArgs e)
        {
            SuspendLayout();
            foreach (ChooserControl control in flowLayoutPanel1.Controls) {
                if (control.FilterText.ToLower().Contains(FilterTextBox.Text.ToLower())) {
                    control.Visible = true;
                } else {
                    control.Visible = false;
                }
            }
            ResumeLayout(false);
        }

        private void ChooserPanel_Leave(object sender, EventArgs e)
        {
            Dispose();
        }

        private void FilterTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape || e.KeyCode == Keys.Up || e.KeyCode == Keys.Down || e.KeyCode == Keys.Enter) {
                ChooserPanel_KeyDown(sender, e);
                e.Handled = true;
            }
        }
    }
}
