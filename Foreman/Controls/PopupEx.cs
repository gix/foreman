namespace Foreman.Controls
{
    using System;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Reflection;
    using System.Security;
    using System.Security.Permissions;
    using System.Windows;
    using System.Windows.Controls.Primitives;
    using System.Windows.Interop;
    using System.Windows.Media;

    public class PopupEx : Popup
    {
        private static readonly object Mutex = new();
        private static PropertyInfo pshIsChildPopupProperty;
        private static MethodInfo pshConnectedToForegroundWindowMethod;
        private static FieldInfo pshWindowField;
        private static ConstructorInfo securityCriticalDataClassOfWindowCtor;
        private static HookUtils.MemoryPatch buildWindowHook;

        public static void InstallHook()
        {
            lock (Mutex) {
                if (buildWindowHook != null)
                    return;

                var nonPublicInstance = BindingFlags.Instance | BindingFlags.NonPublic;
                var nonPublicStatic = BindingFlags.Static | BindingFlags.NonPublic;

                var popupSecurityHelperType = typeof(Popup).GetNestedType(
                    "PopupSecurityHelper", BindingFlags.NonPublic);
                pshWindowField = popupSecurityHelperType.GetField("_window", nonPublicInstance);
                pshIsChildPopupProperty = popupSecurityHelperType.GetProperty("IsChildPopup", nonPublicInstance);
                pshConnectedToForegroundWindowMethod =
                    popupSecurityHelperType.GetMethod("ConnectedToForegroundWindow", nonPublicStatic);

                var windowsBase = typeof(DependencyObject).Assembly;
                securityCriticalDataClassOfWindowCtor =
                    windowsBase.GetType("MS.Internal.SecurityCriticalDataClass`1")
                        .MakeGenericType(typeof(HwndSource))
                        .GetConstructor(nonPublicInstance, null, new[] { typeof(HwndSource) }, null);

                var oldBuildWindow = popupSecurityHelperType.GetMethod(
                    "BuildWindow", BindingFlags.Instance | BindingFlags.NonPublic);
                var newBuildWindow = typeof(PopupSecurityHelper).GetMethod(
                    "BuildWindow", BindingFlags.Static | BindingFlags.NonPublic);

                buildWindowHook = HookUtils.HookMethod(oldBuildWindow, newBuildWindow);
            }
        }

        public static void UninstallHook()
        {
            lock (Mutex) {
                if (buildWindowHook == null)
                    return;

                buildWindowHook.Dispose();
                pshIsChildPopupProperty = null;
                pshConnectedToForegroundWindowMethod = null;
                pshWindowField = null;
                securityCriticalDataClassOfWindowCtor = null;
            }
        }

        public static readonly DependencyProperty HasSystemDropShadowProperty =
            DependencyProperty.Register(
                nameof(HasSystemDropShadow),
                typeof(bool),
                typeof(PopupEx),
                new FrameworkPropertyMetadata(false));

        public bool HasSystemDropShadow
        {
            get => (bool)GetValue(HasSystemDropShadowProperty);
            set => SetValue(HasSystemDropShadowProperty, value);
        }

        public virtual void CreateParams(ref HwndSourceParameters parameters)
        {
            if (HasSystemDropShadow)
                parameters.WindowClassStyle |= NativeMethods.CS_DROPSHADOW;
            else
                parameters.WindowClassStyle &= ~NativeMethods.CS_DROPSHADOW;
        }

        private static class PopupSecurityHelper
        {
            [SuppressMessage("CodeQuality", "IDE0051:Remove unused private members",
                Justification = "Used by reflection")]
            private static void BuildWindow(
                object psh, int x, int y, Visual placementTarget,
                bool transparent, HwndSourceHook hook, AutoResizedEventHandler handler,
                HwndDpiChangedEventHandler dpiChangedHandler)
            {
                bool isChildPopup = (bool)pshIsChildPopupProperty.GetValue(psh);

                Debug.Assert(!isChildPopup || (isChildPopup && !transparent), "Child popups cannot be transparent");
                transparent = transparent && !isChildPopup;

                Visual mainTreeVisual = placementTarget;
                if (isChildPopup) {
                    // If the popup is nested inside other popups, get out into the main tree
                    // before querying for the presentation source.
                    mainTreeVisual = FindMainTreeVisual(placementTarget);
                }

                // get visual's PresentationSource
                var hwndSource = GetPresentationSource(mainTreeVisual) as HwndSource;

                // get parent handle
                IntPtr parent = IntPtr.Zero;
                if (hwndSource != null)
                    parent = GetHandle(hwndSource);

                int classStyle = 0;
                int style = NativeMethods.WS_CLIPSIBLINGS;
                int styleEx = NativeMethods.WS_EX_TOOLWINDOW | NativeMethods.WS_EX_NOACTIVATE;

                if (isChildPopup) {
                    // The popup was created in an environment where it should
                    // be a child window, not a popup window.
                    style |= NativeMethods.WS_CHILD;
                } else {
                    style |= NativeMethods.WS_POPUP;
                    styleEx |= NativeMethods.WS_EX_TOPMOST;
                }

                // set window parameters
                var param = new HwndSourceParameters(string.Empty);
                param.WindowClassStyle = classStyle;
                param.WindowStyle = style;
                param.ExtendedWindowStyle = styleEx;
                param.SetPosition(x, y);

                (hook.Target as PopupEx)?.CreateParams(ref param);

                if (isChildPopup) {
                    if (parent != IntPtr.Zero)
                        param.ParentWindow = parent;
                    else
                        new UIPermission(UIPermissionWindow.AllWindows).Demand();
                } else {
                    param.UsesPerPixelOpacity = transparent;
                    if (parent != IntPtr.Zero && ConnectedToForegroundWindow(parent))
                        param.ParentWindow = parent;
                }

                // create popup's window object
                var newWindow = new HwndSource(param);

                new UIPermission(UIPermissionWindow.AllWindows).Assert(); //BlessedAssert
                try {
                    // add hook to the popup's window
                    newWindow.AddHook(hook);
                } finally {
                    CodeAccessPermission.RevertAssert();
                }

                // initialize the private critical window object
                pshWindowField.SetValue(psh, securityCriticalDataClassOfWindowCtor.Invoke(
                    new object[] { newWindow }));

                // Set background color
                var hwndTarget = newWindow.CompositionTarget;
                hwndTarget.BackgroundColor = transparent ? Colors.Transparent : Colors.Black;

                // add AddAutoResizedEventHandler event handler
                newWindow.AutoResized += handler;

                newWindow.DpiChanged += dpiChangedHandler;
            }

            private static bool ConnectedToForegroundWindow(IntPtr window)
            {
                return (bool)pshConnectedToForegroundWindowMethod.Invoke(null, new object[] { window });
            }

            [SecurityCritical]
            private static PresentationSource GetPresentationSource(Visual visual)
            {
                if (visual == null)
                    return null;
                return PresentationSource.FromVisual(visual);
            }

            [SecurityCritical]
            private static IntPtr GetHandle(HwndSource hwnd)
            {
                if (hwnd == null)
                    return IntPtr.Zero;
                return hwnd.Handle;
            }

            private static Visual FindMainTreeVisual(Visual placementTarget)
            {
                return placementTarget;
            }
        }

        private static class NativeMethods
        {
            public const int WS_POPUP = unchecked((int)0x80000000);
            public const int WS_CHILD = 0x40000000;
            public const int WS_CLIPSIBLINGS = 0x04000000;
            public const int WS_EX_TOPMOST = 0x00000008;
            public const int WS_EX_TOOLWINDOW = 0x00000080;
            public const int WS_EX_NOACTIVATE = 0x08000000;
            public const int CS_DROPSHADOW = 0x20000;
        }
    }
}