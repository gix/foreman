namespace Foreman
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.Reflection;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Controls.Primitives;
    using System.Windows.Input;
    using System.Windows.Media;
    using System.Windows.Threading;
    using Controls;
    using Expression = System.Linq.Expressions.Expression;

    public static class PopupUtils
    {
        public static CustomPopupPlacement[] LeftCenteredPlacement(
            Size popupSize, Size targetSize, Point offset)
        {
            var centerY = (targetSize.Height - popupSize.Height) / 2;
            var leftCenter = new Point(-popupSize.Width - offset.X, centerY + offset.Y);
            var rightCenter = new Point(targetSize.Width + offset.X, centerY + offset.Y);
            var leftPlacement = new CustomPopupPlacement(leftCenter, PopupPrimaryAxis.Vertical);
            var rightPlacement = new CustomPopupPlacement(rightCenter, PopupPrimaryAxis.Vertical);
            return new[] { leftPlacement, rightPlacement };
        }

        public static CustomPopupPlacement[] RightCenteredPlacement(
            Size popupSize, Size targetSize, Point offset)
        {
            var centerY = (targetSize.Height - popupSize.Height) / 2;
            var leftCenter = new Point(-popupSize.Width - offset.X, centerY + offset.Y);
            var rightCenter = new Point(targetSize.Width + offset.X, centerY + offset.Y);
            var leftPlacement = new CustomPopupPlacement(leftCenter, PopupPrimaryAxis.Vertical);
            var rightPlacement = new CustomPopupPlacement(rightCenter, PopupPrimaryAxis.Vertical);
            return new[] { rightPlacement, leftPlacement };
        }

        public static CustomPopupPlacement[] TopCenteredPlacement(
            Size popupSize, Size targetSize, Point offset)
        {
            var centerX = (targetSize.Width - popupSize.Width) / 2;
            var topCenter = new Point(centerX + offset.X, -popupSize.Height - offset.Y);
            var bottomCenter = new Point(centerX + offset.X, targetSize.Height + offset.Y);
            var topPlacement = new CustomPopupPlacement(topCenter, PopupPrimaryAxis.Horizontal);
            var bottomPlacement = new CustomPopupPlacement(bottomCenter, PopupPrimaryAxis.Horizontal);
            return new[] { topPlacement, bottomPlacement };
        }

        public static CustomPopupPlacement[] BottomCenteredPlacement(
            Size popupSize, Size targetSize, Point offset)
        {
            var centerX = (targetSize.Width - popupSize.Width) / 2;
            var topCenter = new Point(centerX + offset.X, -popupSize.Height - offset.Y);
            var bottomCenter = new Point(centerX + offset.X, targetSize.Height + offset.Y);
            var topPlacement = new CustomPopupPlacement(topCenter, PopupPrimaryAxis.Horizontal);
            var bottomPlacement = new CustomPopupPlacement(bottomCenter, PopupPrimaryAxis.Horizontal);
            return new[] { bottomPlacement, topPlacement };
        }

        [ThreadStatic]
        private static Stack<Popup> nestedPopups;

        private static Stack<Popup> NestedPopups =>
            nestedPopups ?? (nestedPopups = new Stack<Popup>());

        public static Popup CreatePopup(object content)
        {
            var popup = new PopupEx {
                AllowsTransparency = false,
                UseLayoutRounding = true,
                StaysOpen = false,
                HasSystemDropShadow = true,
                Child = WrapContent(content)
            };
            popup.Opened += OnPopupOpened;
            popup.Closed += OnPopupClosed;
            return popup;
        }

        private static void OnPopupOpened(object sender, EventArgs args)
        {
            var popup = (Popup)sender;
            NestedPopups.Push(popup);
            EstablishNestedPopupCapture(popup);
            FocusContent(popup);
        }

        private static void OnPopupClosed(object sender, EventArgs e)
        {
            NestedPopups.Pop();
            if (NestedPopups.Count > 0 && Mouse.Captured == null) {
                var popup = NestedPopups.Peek();
                popup.Dispatcher.InvokeAsync(
                    () => EstablishNestedPopupCapture(popup), DispatcherPriority.Input);
            }
        }

        private static void EstablishNestedPopupCapture(Popup popup)
        {
            var popupRoot = GetPopupRoot(popup);
            if (Mouse.Captured != popupRoot) {
                Mouse.Capture(popupRoot, CaptureMode.SubTree);
                PopupCacheValidSetter(popup, 1, true);
            }
        }

        private static UIElement WrapContent(object content)
        {
            var border = new Border {
                Child = new ContentPresenter {
                    Content = content
                },
                Background = Brushes.White,
                BorderBrush = Brushes.DarkGray,
                BorderThickness = new Thickness(1),
                Focusable = true
            };
            RenderOptions.SetClearTypeHint(border, ClearTypeHint.Enabled);
            TextOptions.SetTextFormattingMode(border, TextFormattingMode.Display);
            FocusManager.SetIsFocusScope(border, true);
            return border;
        }

        private static void FocusContent(Popup popup)
        {
            if (popup.Child is Border root &&
                root.Child is ContentPresenter presenter &&
                VisualTreeHelper.GetChildrenCount(presenter) > 0 &&
                VisualTreeHelper.GetChild(presenter, 0) is IInputElement ie) {
                FocusManager.SetFocusedElement(root, ie);
                root.Focus();
            }
        }

        private static FrameworkElement GetPopupRoot(Popup popup)
        {
            var d = VisualTreeHelper.GetParent(popup.Child);
            while (d != null) {
                var parent = VisualTreeHelper.GetParent(d);
                if (parent == null)
                    break;
                d = parent;
            }

            return d as FrameworkElement;
        }

        private static readonly Action<Popup, int, bool> PopupCacheValidSetter =
            CreatePopupCacheValidSetter();

        private static Action<Popup, int, bool> CreatePopupCacheValidSetter()
        {
            var cacheValidField =
                typeof(Popup).GetField("_cacheValid", BindingFlags.Instance | BindingFlags.NonPublic);

            if (cacheValidField == null || cacheValidField.FieldType != typeof(BitVector32))
                return (p, i, v) => { };

            var popup = Expression.Parameter(typeof(Popup));
            var flagParameter = Expression.Parameter(typeof(int));
            var value = Expression.Parameter(typeof(bool));
            var indexer = typeof(BitVector32).GetProperty("Item", new[] { typeof(int) });

            var lambda = Expression.Lambda<Action<Popup, int, bool>>(
                Expression.Assign(
                    Expression.MakeIndex(
                        Expression.Field(popup, cacheValidField),
                        indexer,
                        new[] { flagParameter }),
                    value),
                popup, flagParameter, value);
            return lambda.Compile();
        }
    }
}
