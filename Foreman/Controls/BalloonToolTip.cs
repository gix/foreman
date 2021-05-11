namespace Foreman.Controls
{
    using System;
    using System.Reflection;
    using System.Runtime.InteropServices;
    using System.Security;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Controls.Primitives;
    using System.Windows.Data;
    using System.Windows.Input;
    using System.Windows.Media;
    using System.Windows.Media.Media3D;
    using Extensions;
    using Expression = System.Linq.Expressions.Expression;

    public class BalloonToolTip : ToolTip
    {
        static BalloonToolTip()
        {
            var forType = typeof(BalloonToolTip);
            DefaultStyleKeyProperty.OverrideMetadata(
                forType, new FrameworkPropertyMetadata(forType));
        }

        public BalloonToolTip()
        {
            Loaded += OnLoaded;
            UpdatePlacement();
        }

        public static readonly DependencyProperty ArrowHeadLengthProperty =
            DependencyProperty.Register(
                nameof(ArrowHeadLength),
                typeof(double),
                typeof(BalloonToolTip),
                new FrameworkPropertyMetadata(7.0));

        public static readonly DependencyProperty ArrowHeadWidthProperty =
            DependencyProperty.Register(
                nameof(ArrowHeadWidth),
                typeof(double),
                typeof(BalloonToolTip),
                new FrameworkPropertyMetadata(10.0));

        public static readonly DependencyProperty ArrowDirectionProperty =
            DependencyProperty.Register(
                nameof(ArrowDirection),
                typeof(Direction),
                typeof(BalloonToolTip),
                new FrameworkPropertyMetadata(Direction.Left, OnArrowDirectionChanged));

        private Popup popup;

        /// <summary>Gets or sets the angle (in degrees) of the arrow head.</summary>
        public double ArrowHeadLength
        {
            get => (double)GetValue(ArrowHeadLengthProperty);
            set => SetValue(ArrowHeadLengthProperty, value);
        }

        /// <summary>Gets or sets the width of the arrow head.</summary>
        public double ArrowHeadWidth
        {
            get => (double)GetValue(ArrowHeadWidthProperty);
            set => SetValue(ArrowHeadWidthProperty, value);
        }

        /// <summary>Gets or sets the width of the arrow head.</summary>
        public Direction ArrowDirection
        {
            get => (Direction)GetValue(ArrowDirectionProperty);
            set => SetValue(ArrowDirectionProperty, value);
        }

        private static void OnArrowDirectionChanged(
            DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((BalloonToolTip)d).UpdatePlacement();
        }

        private void UpdatePlacement()
        {
            Placement = PlacementMode.Custom;
            CustomPopupPlacementCallback = PlacePopup;
        }

        private CustomPopupPlacement[] PlacePopup(Size popupsize, Size targetsize, Point offset)
        {
            switch (ArrowDirection) {
                case Direction.Up:
                    return PopupUtils.BottomCenteredPlacement(popupsize, targetsize, offset);
                case Direction.Down:
                    return PopupUtils.TopCenteredPlacement(popupsize, targetsize, offset);
                case Direction.Left:
                    return PopupUtils.RightCenteredPlacement(popupsize, targetsize, offset);
                case Direction.Right:
                    return PopupUtils.LeftCenteredPlacement(popupsize, targetsize, offset);
                default:
                    return new CustomPopupPlacement[0];
            }

        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            FindAndHookPopup(this, ref popup);
        }

        private static void FindAndHookPopup(DependencyObject element, ref Popup popup)
        {
            if (popup != null)
                return;

            DependencyObject visualRoot = element.FindVisualRoot();
            if (visualRoot != null) {
                popup = LogicalTreeHelper.GetParent(visualRoot) as Popup;
                if (popup != null) {
                    popup.Opened += OnPopupOpenedOrClosed;
                    popup.Closed += OnPopupOpenedOrClosed;
                    UpdatePopupAnimation(popup);
                }
            }
        }

        private static void OnPopupOpenedOrClosed(object sender, EventArgs e)
        {
            UpdatePopupAnimation((Popup)sender);
        }

        private static void UpdatePopupAnimation(Popup popup)
        {
            popup.PopupAnimation = PopupAnimation.None;
        }
    }

    public sealed class BalloonToolTipEventArgs : RoutedEventArgs
    {
        internal BalloonToolTipEventArgs(bool opening)
        {
            if (opening)
                RoutedEvent = BalloonToolTipService.BalloonToolTipOpeningEvent;
            else
                RoutedEvent = BalloonToolTipService.BalloonToolTipClosingEvent;
        }

        protected override void InvokeEventHandler(Delegate genericHandler, object genericTarget)
        {
            ((BalloonToolTipEventHandler)genericHandler)(genericTarget, this);
        }
    }

    public delegate void BalloonToolTipEventHandler(object sender, BalloonToolTipEventArgs e);

    internal sealed class FindBalloonEventArgs : RoutedEventArgs
    {
        public FindBalloonEventArgs()
        {
            RoutedEvent = BalloonToolTipService.FindBalloonToolTipEvent;
        }

        public bool KeepCurrentActive { get; set; }

        public DependencyObject TargetElement { get; set; }

        protected override void InvokeEventHandler(
            Delegate genericHandler, object genericTarget)
        {
            var handler = (FindBallonEventHandler)genericHandler;
            handler(genericTarget, this);
        }
    }

    internal delegate void FindBallonEventHandler(object sender, FindBalloonEventArgs e);

    public sealed class BalloonToolTipService
    {
        private readonly Func<InputEventArgs, bool> isRawMouseDeactivate;

        private WeakReference lastChecked;
        private WeakReference lastMouseDirectlyOver;
        private WeakReference lastMouseOverWithToolTip;

        private BalloonToolTip currentToolTip;
        private bool ownsToolTip;

        [SecurityCritical]
        private BalloonToolTipService()
        {
            InputManager.Current.PostProcessInput += OnPostProcessInput;
            isRawMouseDeactivate = CreateRawMouseDeactivateDelegate();
        }

        public static readonly RoutedEvent BalloonToolTipOpeningEvent =
            EventManager.RegisterRoutedEvent(
                "BalloonToolTipOpening",
                RoutingStrategy.Direct,
                typeof(BalloonToolTipEventHandler),
                typeof(BalloonToolTipService));

        public static readonly RoutedEvent BalloonToolTipClosingEvent =
            EventManager.RegisterRoutedEvent(
                "BalloonToolTipClosing",
                RoutingStrategy.Direct,
                typeof(BalloonToolTipEventHandler),
                typeof(BalloonToolTipService));

        internal static readonly RoutedEvent FindBalloonToolTipEvent =
            EventManager.RegisterRoutedEvent(
                "FindBalloonToolTip",
                RoutingStrategy.Bubble,
                typeof(FindBallonEventHandler),
                typeof(BalloonToolTipService));

        internal static readonly DependencyProperty ServiceOwnedProperty =
            DependencyProperty.RegisterAttached(
                "ServiceOwned", typeof(bool), typeof(BalloonToolTipService),
                new FrameworkPropertyMetadata(false));

        internal static readonly DependencyProperty OwnerProperty =
            (DependencyProperty)typeof(ToolTip).Assembly
                .GetType("System.Windows.Controls.PopupControlService")
                .GetField("OwnerProperty", BindingFlags.Static | BindingFlags.NonPublic)
                .GetValue(null);

        /// <summary>
        ///   Identifies the <see cref="BalloonToolTip"/> read-only attached
        ///   dependency property.
        /// </summary>
        public static readonly DependencyProperty BalloonToolTipProperty =
            DependencyProperty.RegisterAttached(
                "BalloonToolTip",
                typeof(object),
                typeof(BalloonToolTipService),
                new FrameworkPropertyMetadata(OnBalloonToolTipChanged));

        private static void OnBalloonToolTipChanged(
            DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
        }

        private DependencyObject LastChecked
        {
            get
            {
                if (lastChecked != null) {
                    var target = (DependencyObject)lastChecked.Target;
                    if (target != null)
                        return target;
                    lastChecked = null;
                }
                return null;
            }
            set
            {
                if (value == null)
                    lastChecked = null;
                else if (lastChecked == null)
                    lastChecked = new WeakReference(value);
                else
                    lastChecked.Target = value;
            }
        }

        public static BalloonToolTipService Current { get; } = new();

        private IInputElement LastMouseDirectlyOver
        {
            get
            {
                if (lastMouseDirectlyOver != null) {
                    var target = (IInputElement)lastMouseDirectlyOver.Target;
                    if (target != null)
                        return target;
                    lastMouseDirectlyOver = null;
                }
                return null;
            }
            set
            {
                if (value == null)
                    lastMouseDirectlyOver = null;
                else if (lastMouseDirectlyOver == null)
                    lastMouseDirectlyOver = new WeakReference(value);
                else
                    lastMouseDirectlyOver.Target = value;
            }
        }

        private DependencyObject LastMouseOverWithToolTip
        {
            get
            {
                if (lastMouseOverWithToolTip != null) {
                    var target = (DependencyObject)lastMouseOverWithToolTip.Target;
                    if (target != null)
                        return target;
                    lastMouseOverWithToolTip = null;
                }
                return null;
            }
            set
            {
                if (value == null)
                    lastMouseOverWithToolTip = null;
                else if (lastMouseOverWithToolTip == null)
                    lastMouseOverWithToolTip = new WeakReference(value);
                else
                    lastMouseOverWithToolTip.Target = value;
            }
        }

        static BalloonToolTipService()
        {
            EventManager.RegisterClassHandler(
                typeof(UIElement), FindBalloonToolTipEvent,
                new FindBallonEventHandler(OnFindBalloonToolTip));
            EventManager.RegisterClassHandler(
                typeof(ContentElement), FindBalloonToolTipEvent,
                new FindBallonEventHandler(OnFindBalloonToolTip));
            EventManager.RegisterClassHandler(
                typeof(UIElement3D), FindBalloonToolTipEvent,
                new FindBallonEventHandler(OnFindBalloonToolTip));
        }

        public static object GetBalloonToolTip(DependencyObject d)
        {
            return d.GetValue(BalloonToolTipProperty);
        }

        public static void SetBalloonToolTip(DependencyObject d, object value)
        {
            d.SetValue(BalloonToolTipProperty, value);
        }

        [SecurityCritical]
        private void OnPostProcessInput(object sender, ProcessInputEventArgs e)
        {
            if (e.StagingItem.Input.RoutedEvent == Mouse.MouseMoveEvent) {
                IInputElement directlyOver = Mouse.PrimaryDevice.DirectlyOver;
                if (directlyOver != null)
                    OnMouseMove(directlyOver);
            } else if (isRawMouseDeactivate(e.StagingItem.Input)) {
                if (LastMouseDirectlyOver != null) {
                    LastMouseDirectlyOver = null;
                    if (LastMouseOverWithToolTip != null) {
                        RaiseToolTipClosingEvent(true);
                        if (GetCapture() == IntPtr.Zero)
                            LastMouseOverWithToolTip = null;
                    }
                }
            }
        }

        private void OnMouseMove(IInputElement directlyOver)
        {
            if (directlyOver != LastMouseDirectlyOver) {
                LastMouseDirectlyOver = directlyOver;
                if (directlyOver != LastMouseOverWithToolTip)
                    InspectElementForToolTip(directlyOver as DependencyObject);
            }
        }

        private void InspectElementForToolTip(DependencyObject obj)
        {
            DependencyObject origObj = obj;
            if (LocateNearestToolTip(ref obj)) {
                if (obj != null) {
                    if (LastMouseOverWithToolTip != null)
                        RaiseToolTipClosingEvent(true);
                    LastChecked = origObj;
                    LastMouseOverWithToolTip = obj;
                    RaiseToolTipOpeningEvent();
                }
            } else {
                RaiseToolTipClosingEvent(true);
                LastMouseOverWithToolTip = null;
            }
        }

        private bool LocateNearestToolTip(ref DependencyObject o)
        {
            if (o is IInputElement element) {
                var args = new FindBalloonEventArgs();
                element.RaiseEvent(args);
                if (args.TargetElement != null) {
                    o = args.TargetElement;
                    return true;
                }
                if (args.KeepCurrentActive) {
                    o = null;
                    return true;
                }
            }
            return false;
        }

        private static void OnFindBalloonToolTip(object sender, FindBalloonEventArgs e)
        {
            if (e.TargetElement == null && sender is DependencyObject o) {
                if (Current.StopLookingForToolTip(o)) {
                    e.Handled = true;
                    e.KeepCurrentActive = true;
                } else if (BalloonToolTipIsEnabled(o)) {
                    e.TargetElement = o;
                    e.Handled = true;
                }
            }
        }

        private bool StopLookingForToolTip(DependencyObject o)
        {
            return
                o == LastChecked ||
                o == LastMouseOverWithToolTip ||
                o == currentToolTip ||
                WithinCurrentToolTip(o);
        }

        private bool WithinCurrentToolTip(DependencyObject o)
        {
            if (currentToolTip == null)
                return false;

            DependencyObject obj = o as Visual;
            if (obj == null) {
                if (o is ContentElement ce)
                    obj = FindContentElementParent(ce);
                else
                    obj = o as Visual3D;
            }

            if (obj == null)
                return false;

            return
                (obj is Visual visual && visual.IsDescendantOf(currentToolTip)) ||
                (obj is Visual3D visual3D && visual3D.IsDescendantOf(currentToolTip));
        }

        private static DependencyObject FindContentElementParent(ContentElement ce)
        {
            DependencyObject parent = ce;
            while (parent != null) {
                if (parent is Visual visual)
                    return visual;
                if (parent is Visual3D visual3D)
                    return visual3D;
                ce = parent as ContentElement;
                if (ce == null)
                    return null;

                parent = ContentOperations.GetParent(ce);
                if (parent == null && ce is FrameworkContentElement element)
                    parent = element.Parent;
            }

            return null;
        }

        private static bool BalloonToolTipIsEnabled(DependencyObject obj)
        {
            return
                GetBalloonToolTip(obj) != null
                && ToolTipService.GetIsEnabled(obj)
                && (IsElementEnabled(obj) || ToolTipService.GetShowOnDisabled(obj));
        }

        private static bool IsElementEnabled(DependencyObject obj)
        {
            switch (obj) {
                case UIElement element:
                    return element.IsEnabled;
                case ContentElement contentElement:
                    return contentElement.IsEnabled;
                case UIElement3D element3D:
                    return element3D.IsEnabled;
                default:
                    return true;
            }
        }

        private void RaiseToolTipOpeningEvent()
        {
            DependencyObject obj = LastMouseOverWithToolTip;
            if (obj == null)
                return;

            bool show = true;
            if (obj is IInputElement element) {
                var e = new BalloonToolTipEventArgs(true);
                element.RaiseEvent(e);
                show = !e.Handled;
            }

            if (!show)
                return;

            if (GetBalloonToolTip(obj) is BalloonToolTip toolTip) {
                currentToolTip = toolTip;
                ownsToolTip = false;
            } else if (currentToolTip == null || !ownsToolTip) {
                var binding = new Binding {
                    Path = new PropertyPath(ToolTipService.ToolTipProperty),
                    Source = obj,
                    Mode = BindingMode.OneWay
                };
                currentToolTip = new BalloonToolTip();
                currentToolTip.SetValue(ServiceOwnedProperty, true);
                currentToolTip.SetBinding(ContentControl.ContentProperty, binding);
                ownsToolTip = true;
            }

            if (!currentToolTip.StaysOpen)
                throw new NotSupportedException("ToolTip.StaysOpen=false is not allowed");

            currentToolTip.SetValue(OwnerProperty, obj);
            currentToolTip.Closed += OnToolTipClosed;
            currentToolTip.IsOpen = true;
        }

        private void RaiseToolTipClosingEvent(bool reset)
        {
            if (reset)
                LastChecked = null;

            DependencyObject o = LastMouseOverWithToolTip;
            if (o != null && currentToolTip != null) {
                bool isOpen = currentToolTip.IsOpen;
                try {
                    if (isOpen && o is IInputElement element)
                        element.RaiseEvent(new BalloonToolTipEventArgs(false));
                } finally {
                    if (isOpen) {
                        currentToolTip.IsOpen = false;
                    } else {
                        currentToolTip.ClearValue(OwnerProperty);
                        if (ownsToolTip)
                            BindingOperations.ClearBinding(currentToolTip, ContentControl.ContentProperty);
                    }
                    currentToolTip = null;
                }
            }
        }

        private void OnToolTipClosed(object sender, EventArgs e)
        {
            var source = (BalloonToolTip)sender;
            source.Closed -= OnToolTipClosed;
            source.ClearValue(OwnerProperty);
            if ((bool)source.GetValue(ServiceOwnedProperty))
                BindingOperations.ClearBinding(source, ContentControl.ContentProperty);
        }

        [DllImport("user32", CharSet = CharSet.Auto)]
        private static extern IntPtr GetCapture();

        private static Func<InputEventArgs, bool> CreateRawMouseDeactivateDelegate()
        {
            var presentationCore = typeof(PresentationSource).Assembly;
            var rawMouseActionsType = presentationCore.GetType("System.Windows.Input.RawMouseActions");
            var rawMouseActionsDeactivate = rawMouseActionsType.GetField("Deactivate").GetValue("null");
            var inputReportEventArgsType = presentationCore.GetType("System.Windows.Input.InputReportEventArgs");
            var routedEventProperty = inputReportEventArgsType.GetProperty("RoutedEvent");
            var reportProperty = inputReportEventArgsType.GetProperty("Report");
            var rawMouseInputReportType = presentationCore.GetType("System.Windows.Input.RawMouseInputReport");
            var typeProperty = rawMouseInputReportType.GetProperty("Type");
            var actionsProperty = rawMouseInputReportType.GetProperty("Actions");
            var inputManagerInputReportEventField = typeof(InputManager).GetField(
                "InputReportEvent", BindingFlags.Static | BindingFlags.NonPublic);

            if (reportProperty == null ||
                routedEventProperty == null ||
                typeProperty == null ||
                actionsProperty == null ||
                inputManagerInputReportEventField == null)
                throw new InvalidOperationException();

            var inputReportEvent = (RoutedEvent)inputManagerInputReportEventField.GetValue(null);
            var inputEventArgs = Expression.Parameter(typeof(InputEventArgs));
            var input = Expression.Variable(inputReportEventArgsType);

            var rawMouseActionsDeactivateExpr = Expression.Convert(
                Expression.Constant(rawMouseActionsDeactivate),
                Enum.GetUnderlyingType(rawMouseActionsType));

            // !input.Handled && input.Report.Type == InputType.Mouse &&
            // (((RawMouseInputReport)input.Report).Actions & RawMouseActions.Deactivate)
            //     == RawMouseActions.Deactivate
            var isRawMouseDeactivateReport = Expression.AndAlso(
                Expression.AndAlso(
                    Expression.Not(
                        Expression.Property(input, nameof(InputEventArgs.Handled))),
                    Expression.Equal(
                        Expression.Property(
                            Expression.Property(input, reportProperty),
                            typeProperty),
                        Expression.Constant(InputType.Mouse))),
                Expression.Equal(
                    Expression.And(
                        Expression.Convert(
                            Expression.Property(
                                Expression.Convert(
                                    Expression.Property(input, reportProperty),
                                    rawMouseInputReportType),
                                actionsProperty),
                            Enum.GetUnderlyingType(rawMouseActionsType)),
                        rawMouseActionsDeactivateExpr),
                    rawMouseActionsDeactivateExpr));

            var returnLabel = Expression.Label(typeof(bool));

            // if (e.StagingItem.Input.RoutedEvent == InputManager.InputReportEvent) {
            //     var input = (InputReportEventArgs)e.StagingItem.Input;
            //     if (!input.Handled && input.Report.Type == InputType.Mouse) {
            //         if ((((RawMouseInputReport)input.Report).Actions & RawMouseActions.Deactivate) == RawMouseActions.Deactivate) {
            //             return true;
            //         }
            //     }
            // }
            // return false;
            return Expression.Lambda<Func<InputEventArgs, bool>>(
                Expression.Block(
                    new[] { input },
                    Expression.IfThen(
                        Expression.Equal(
                            Expression.Property(inputEventArgs, nameof(InputEventArgs.RoutedEvent)),
                            Expression.Constant(inputReportEvent)),
                        Expression.Block(
                            Expression.Assign(
                                input,
                                Expression.Convert(inputEventArgs, inputReportEventArgsType)),
                            Expression.IfThen(
                                isRawMouseDeactivateReport,
                                Expression.Return(returnLabel, Expression.Constant(true))))),
                    Expression.Return(returnLabel, Expression.Constant(false)),
                    Expression.Label(returnLabel, Expression.Constant(false))),
                inputEventArgs).Compile();
        }
    }
}
