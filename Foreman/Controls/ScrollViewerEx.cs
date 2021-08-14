namespace Foreman.Controls
{
    using System.Windows.Controls;
    using System.Windows.Input;

    public class ScrollViewerEx : ScrollViewer
    {
        public bool HandleMouseWheel { get; set; }

        protected override void OnMouseWheel(MouseWheelEventArgs e)
        {
            if (HandleMouseWheel)
                base.OnMouseWheel(e);
            e.Handled = false;
        }
    }
}
