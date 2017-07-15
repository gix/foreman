namespace Foreman.Extensions
{
    using System;
    using System.Runtime.InteropServices;
    using System.Windows;
    using System.Windows.Interop;

    public static class WindowExtensions
    {
        private const int GWL_EXSTYLE = -20;
        private const int WS_EX_DLGMODALFRAME = 0x0001;
        private const int SWP_NOSIZE = 0x0001;
        private const int SWP_NOMOVE = 0x0002;
        private const int SWP_NOZORDER = 0x0004;
        private const int SWP_FRAMECHANGED = 0x0020;
        private const uint WM_SETICON = 0x0080;
        private const uint RDW_INVALIDATE = 0x0001;
        private const uint RDW_FRAME = 0x0400;

        public static IntPtr GetWindowLong(HandleRef hWnd, int nIndex)
        {
            if (IntPtr.Size == 4)
                return (IntPtr)GetWindowLongPtr32(hWnd, nIndex);
            return GetWindowLongPtr64(hWnd, nIndex);
        }

        public static IntPtr SetWindowLong(HandleRef hWnd, int nIndex, IntPtr dwNewLong)
        {
            if (IntPtr.Size == 4)
                return (IntPtr)SetWindowLongPtr32(hWnd, nIndex, dwNewLong.ToInt32());
            return SetWindowLongPtr64(hWnd, nIndex, dwNewLong);
        }

        [DllImport("user32", CharSet = CharSet.Auto, EntryPoint = "GetWindowLong")]
        private static extern int GetWindowLongPtr32(HandleRef hWnd, int nIndex);

        [DllImport("user32", CharSet = CharSet.Auto, EntryPoint = "GetWindowLongPtr")]
        private static extern IntPtr GetWindowLongPtr64(HandleRef hWnd, int nIndex);

        [DllImport("user32", CharSet = CharSet.Auto, EntryPoint = "SetWindowLong")]
        private static extern int SetWindowLongPtr32(HandleRef hWnd, int nIndex, int dwNewLong);

        [DllImport("user32", CharSet = CharSet.Auto, EntryPoint = "SetWindowLongPtr")]
        private static extern IntPtr SetWindowLongPtr64(HandleRef hWnd, int nIndex, IntPtr dwNewLong);

        [DllImport("user32", CharSet = CharSet.Auto, ExactSpelling = true)]
        private static extern bool SetWindowPos(
            HandleRef hwnd, IntPtr hwndInsertAfter, int x, int y, int width,
            int height, uint flags);

        [DllImport("user32", CharSet = CharSet.Auto, ExactSpelling = true)]
        private static extern bool RedrawWindow(
            HandleRef hwnd, IntPtr lprcUpdate, IntPtr hrgnUpdate, uint flags);

        [DllImport("user32", CharSet = CharSet.Unicode)]
        private static extern IntPtr SendMessage(
            HandleRef hwnd, uint msg, UIntPtr wParam, IntPtr lParam);

        private static void UpdateWindowIcon(Window window, bool showIcon)
        {
            var wih = new WindowInteropHelper(window);
            wih.EnsureHandle();
            if (wih.Handle == IntPtr.Zero)
                return;

            var hwnd = new HandleRef(window, new WindowInteropHelper(window).Handle);

            // Change the extended window style to not show a window icon
            var extendedStyle = (int)GetWindowLong(hwnd, GWL_EXSTYLE);

            if (showIcon)
                extendedStyle &= ~WS_EX_DLGMODALFRAME;
            else
                extendedStyle |= WS_EX_DLGMODALFRAME;

            SetWindowLong(hwnd, GWL_EXSTYLE, (IntPtr)(extendedStyle | WS_EX_DLGMODALFRAME));

            // Update the window's non-client area to reflect the changes
            SetWindowPos(hwnd, IntPtr.Zero, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE |
                                                        SWP_NOZORDER | SWP_FRAMECHANGED);

            if (!showIcon && window.Icon == null) {
                SendMessage(hwnd, WM_SETICON, (UIntPtr)0, (IntPtr)0);
                SendMessage(hwnd, WM_SETICON, (UIntPtr)1, (IntPtr)0);
            }

            RedrawWindow(hwnd, IntPtr.Zero, IntPtr.Zero, RDW_INVALIDATE | RDW_FRAME);
        }

        public static readonly DependencyProperty ShowIconProperty =
            DependencyProperty.RegisterAttached(
                "ShowIcon",
                typeof(bool),
                typeof(WindowExtensions),
                new PropertyMetadata(true, OnShowIconChanged));

        private static void OnShowIconChanged(
            DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is Window window)
                UpdateWindowIcon(window, (bool)e.NewValue);
        }

        public static bool GetShowIcon(Window d)
        {
            return (bool)d.GetValue(ShowIconProperty);
        }

        public static void SetShowIcon(Window d, bool value)
        {
            d.SetValue(ShowIconProperty, value);
        }
    }
}
