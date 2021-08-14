namespace Foreman.Controls
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Controls.Primitives;
    using System.Windows.Input;
    using System.Windows.Media;
    using Extensions;

    /// <summary>
    ///   Defines an area within which you can explicitly position an infinite
    ///   number of child elements by using coordinates that are relative to the
    ///   <see cref="ZoomableCanvas"/> area.
    /// </summary>
    public class ZoomableCanvas : Canvas, IScrollInfo
    {
        static ZoomableCanvas()
        {
            RenderTransformProperty.OverrideMetadata(
                typeof(ZoomableCanvas), new FrameworkPropertyMetadata(null, CoerceRenderTransform));

            TopProperty.OverrideMetadata(
                typeof(ZoomableCanvas), new FrameworkPropertyMetadata(OnPositioningChanged));
            LeftProperty.OverrideMetadata(
                typeof(ZoomableCanvas), new FrameworkPropertyMetadata(OnPositioningChanged));
            BottomProperty.OverrideMetadata(
                typeof(ZoomableCanvas), new FrameworkPropertyMetadata(OnPositioningChanged));
            RightProperty.OverrideMetadata(
                typeof(ZoomableCanvas), new FrameworkPropertyMetadata(OnPositioningChanged));
            CanvasService.PositionProperty.OverrideMetadata(
                typeof(UIElement), new FrameworkPropertyMetadata(OnPositioningChanged));
        }

        public ZoomableCanvas()
        {
            CoerceValue(ScaleProperty);
            CoerceValue(OffsetProperty);
            CoerceValue(ActualViewboxProperty);
            CoerceValue(RenderTransformProperty);
        }

        #region ApplyTransformProperty

        /// <summary>
        ///   Identifies the <see cref="ApplyTransform"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ApplyTransformProperty =
            DependencyProperty.Register(
                nameof(ApplyTransform),
                typeof(bool),
                typeof(ZoomableCanvas),
                new FrameworkPropertyMetadata(
                    true, FrameworkPropertyMetadataOptions.AffectsArrange,
                    OnApplyTransformChanged));

        /// <summary>
        ///   Gets or sets whether to automatically apply a <see cref="ScaleTransform"/>
        ///   to the canvas.
        /// </summary>
        /// <value>
        ///   <see langword="true"/> or <see langword="false"/>. The default is
        ///   <see langword="true"/>.
        /// </value>
        /// <remarks>
        ///   The value of this dependency property is <see langword="true"/> by
        ///   default, meaning that the <see cref="UIElement.RenderTransform"/>
        ///   property will contain a <see cref="Transform"/> that scales the
        ///   canvas and its children automatically. This property can be set to
        ///   <see langword="false"/> prevent the automatic transform. This means
        ///   that children are responsible for changing their appearance when
        ///   the <see cref="Scale"/> property changes. Note that this property
        ///   does not affect the <b>placement</b> of the elements; the children
        ///   are automatically placed with the top-left corners of their elements
        ///   at the appropriate positions on the screen, regardless of the value
        ///   of <see cref="ApplyTransform"/>.
        ///   <para>
        ///     Children will usually do this by simply changing their
        ///     <see cref="P:Width"/> and <see cref="P:Height"/> to become larger
        ///     or smaller when the <see cref="Scale"/> property increases or
        ///     decreases. This is useful when pen widths are important, such as
        ///     an element surrounded with a <see cref="Border"/> with
        ///     <see cref="Border.BorderThickness"/> set to <c>1.0</c>. If
        ///     <see cref="ApplyTransform"/> is <see langword="true"/>, then as
        ///     <see cref="Scale"/> decreases the shape will be scaled down and
        ///     the border stroke will become thinner than one pixel, possibly
        ///     too thin to see even with sub-pixel rendering. This is also true
        ///     when drawing paths, edges of a graph, or any other element that
        ///     uses <see cref="Pen"/> to draw lines and strokes. In these cases
        ///     setting <see cref="ApplyTransform"/> to <see langword="false"/>
        ///     and setting the <see cref="System.Windows.Shapes.Shape"/> 's
        ///     <see cref="System.Windows.Shapes.Shape.Stretch"/> to
        ///     <see cref="F:Stretch.Fill"/> while binding its <see cref="P:Width"/>
        ///     and <see cref="P:Height"/> to a factor of <see cref="Scale"/>
        ///     will often provide a better effect.
        ///   </para>
        ///   <para>
        ///     Another reason to set this property to <see langword="false"/>
        ///     is when elements change their representation or visual state
        ///     based on the scale (also known as "semantic zoom"). For example,
        ///     imagine a canvas showing multiple thumbnails of spreadsheets and
        ///     the relationships between their formulas and values. When
        ///     <see cref="Scale"/> is set to <c>1.0</c> (the default value),
        ///     each spreadsheet element might be fully interactive, editable,
        ///     and showing all rows and columns. When zooming out, and
        ///     <see cref="Scale"/> gets small enough that there is not enough
        ///     room for each spreadsheet to show all of its rows and columns,
        ///     it may change its representation into a bar chart or pie chart
        ///     with axis values and a legend instead. When zooming even further
        ///     out, and <see cref="Scale"/> gets small enough that there is not
        ///     enough room for the axis and legend, it may simply remove the
        ///     axis and legend to make more room for the graphical portion of
        ///     the chart. Since the children of the canvas can be arbitary rich
        ///     UIElements, they can dynamically change their representation and
        ///     be interacted with at all levels of zoom. This is in sharp contrast
        ///     to multi-scale-image approaches such as Silverlight's Deep Zoom
        ///     since those scenarios are simply performing linear scale transformations
        ///     on pre-computed static bitmaps.
        ///   </para>
        /// </remarks>
        public bool ApplyTransform
        {
            get => (bool)GetValue(ApplyTransformProperty);
            set => SetValue(ApplyTransformProperty, value);
        }

        /// <summary>
        ///   Returns a transform applying the <see cref="Scale"/> and
        ///   <see cref="Offset"/> when <see cref="ApplyTransform"/> is set to
        ///   <see langword="true"/>.
        /// </summary>
        /// <param name="d">Dependency object whos value is being coerced.</param>
        /// <param name="value">The original uncoerced value.</param>
        /// <returns>
        ///   A new transform if <see cref="ApplyTransform"/> is set to <see langword="true"/>;
        ///   otherwise, <paramref name="value"/>.
        /// </returns>
        private static object CoerceRenderTransform(DependencyObject d, object value)
        {
            if (d is ZoomableCanvas { ApplyTransform: true }) {
                var transform = new TransformGroup();
                transform.Children.Add(new ScaleTransform());
                transform.Children.Add(new TranslateTransform());
                return transform;
            }

            return value;
        }

        /// <summary>
        ///   Handles the event that occurs when the value of the
        ///   <see cref="ApplyTransform"/> dependency property has changed.
        /// </summary>
        /// <param name="d">
        ///   The dependency object on which the dependency property has changed.
        /// </param>
        /// <param name="e">
        ///   The event args containing the old and new values of the dependency
        ///   property.
        /// </param>
        private static void OnApplyTransformChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            d.CoerceValue(RenderTransformProperty);
        }

        #endregion

        #region ActualViewboxProperty

        private static readonly DependencyPropertyKey ActualViewboxPropertyKey =
            DependencyProperty.RegisterReadOnly(
                nameof(ActualViewbox),
                typeof(Rect),
                typeof(ZoomableCanvas),
                new FrameworkPropertyMetadata(
                    Rect.Empty, OnActualViewboxChanged, CoerceActualViewbox));

        /// <summary>
        ///   Identifies the <see cref="ActualViewbox"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ActualViewboxProperty =
            ActualViewboxPropertyKey.DependencyProperty;

        /// <summary>
        ///   Gets a <see cref="Rect"/> representing the area of the canvas that
        ///   is currently being displayed by this panel.
        /// </summary>
        /// <value>
        ///   A <see cref="Rect"/> representing the area of the canvas that is
        ///   currently being displayed by this panel.
        /// </value>
        /// <remarks>
        ///   The value of this property is automatically computed based on the
        ///   <see cref="Scale"/>, <see cref="Offset"/>, and
        ///   <see cref="UIElement.RenderSize"/> of this panel. It is independent
        ///   (and usually different) from the <see cref="Viewbox"/> dependency
        ///   property.
        /// </remarks>
        /// <seealso cref="Viewbox"/>
        public Rect ActualViewbox => (Rect)GetValue(ActualViewboxProperty);

        public event EventHandler? ActualViewboxChanged;

        /// <summary>
        ///   Returns a <see cref="Rect"/> representing the area of the canvas
        ///   that is currently being displayed.
        /// </summary>
        /// <param name="d">Dependency object whos value is being coerced.</param>
        /// <param name="value">The original uncoerced value.</param>
        /// <returns>
        ///   A <see cref="Rect"/> representing the area of the canvas (in canvas
        ///   coordinates) that is being displayed by this panel.
        /// </returns>
        private static object CoerceActualViewbox(DependencyObject d, object value)
        {
            var canvas = (ZoomableCanvas)d;

            var offset = canvas.Offset;
            var scale = canvas.Scale;
            var renderSize = canvas.RenderSize;
            return new Rect(
                offset.X / scale,
                offset.Y / scale,
                renderSize.Width / scale,
                renderSize.Height / scale);
        }

        /// <summary>
        ///   Handles the event that occurs when the value of the
        ///   <see cref="ActualViewbox"/> dependency property has changed.
        /// </summary>
        /// <param name="d">
        ///   The dependency object on which the dependency property has changed.
        /// </param>
        /// <param name="e">
        ///   The event args containing the old and new values of the dependency
        ///   property.
        /// </param>
        private static void OnActualViewboxChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var canvas = (ZoomableCanvas)d;
            ((IScrollInfo)canvas).ScrollOwner?.InvalidateScrollInfo();
            canvas.ActualViewboxChanged?.Invoke(canvas, EventArgs.Empty);
        }

        #endregion

        #region ViewboxProperty

        /// <summary>
        ///   Identifies the <see cref="Viewbox"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ViewboxProperty =
            DependencyProperty.Register(
                nameof(Viewbox),
                typeof(Rect),
                typeof(ZoomableCanvas),
                new FrameworkPropertyMetadata(
                    Rect.Empty,
                    FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                    OnViewboxChanged),
                IsViewboxValid);

        /// <summary>
        ///   Gets or sets the portion of the canvas (in canvas coordinates)
        ///   that should be attempted to be displayed by this panel.
        /// </summary>
        /// <value>
        ///   A <see cref="Rect"/> specifying the portion of the canvas that
        ///   should be displayed by this panel, or <see cref="Rect.Empty"/>
        ///   when unspecified. The default value is <see cref="Rect.Empty"/>.
        /// </value>
        /// <remarks>
        ///   The area of the canvas shown by this panel can be controlled by
        ///   either setting <see cref="Scale"/> and <see cref="Offset"/>, or by
        ///   setting the <see cref="Viewbox"/>, <see cref="Stretch"/>, and
        ///   <see cref="StretchDirection"/> properties. When <see cref="Viewbox"/>
        ///   is set to anything other than <see cref="Rect.Empty"/>, the
        ///   <see cref="Scale"/> and <see cref="Offset"/> will be automatically
        ///   coerced to appropriate values according to the <see cref="Stretch"/>
        ///   and <see cref="StretchDirection"/> properties. Note that the
        ///   <see cref="Stretch"/> mode of <see cref="F:Stretch.Fill"/> is not
        ///   supported, so unless the aspect ratio of <see cref="Viewbox"/>
        ///   exactly matches the aspect ratio of <see cref="UIElement.RenderSize"/> the
        ///   actual area displayed will be more or less than <see cref="Viewbox"/>.
        ///   The exact area that is displayed can be determined by the
        ///   <see cref="ActualViewbox"/> property in this case.
        /// </remarks>
        /// <seealso cref="Stretch"/>
        /// <seealso cref="StretchDirection"/>
        /// <seealso cref="TileBrush.Viewbox"/>
        public Rect Viewbox
        {
            get => (Rect)GetValue(ViewboxProperty);
            set => SetValue(ViewboxProperty, value);
        }

        /// <summary>
        ///   Determines whether the value given is a valid value for the
        ///   <see cref="Viewbox"/> dependency property.
        /// </summary>
        /// <param name="value">
        ///   The potential value for the dependency property.
        /// </param>
        /// <returns>
        ///   <see langword="true"/> if the value is a valid value for the property; otherwise,
        ///   <see langword="false"/>.
        /// </returns>
        private static bool IsViewboxValid(object value)
        {
            var viewbox = (Rect)value;
            return viewbox.IsEmpty
                   || (viewbox.X.IsBetween(double.MinValue, double.MaxValue)
                       && viewbox.Y.IsBetween(double.MinValue, double.MaxValue)
                       && viewbox.Width.IsBetween(double.Epsilon, double.MaxValue)
                       && viewbox.Height.IsBetween(double.Epsilon, double.MaxValue));
        }

        /// <summary>
        ///   Handles the event that occurs when the value of the <see cref="Viewbox"/>
        ///   dependency property has changed.
        /// </summary>
        /// <param name="d">
        ///   The dependency object on which the dependency property has changed.
        /// </param>
        /// <param name="e">
        ///   The event args containing the old and new values of the dependency
        ///   property.
        /// </param>
        private static void OnViewboxChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            d.CoerceValue(ScaleProperty);
            d.CoerceValue(OffsetProperty);
        }

        #endregion

        #region StretchProperty

        /// <summary>
        ///   Identifies the <see cref="Stretch"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty StretchProperty =
            DependencyProperty.Register(
                nameof(Stretch),
                typeof(Stretch),
                typeof(ZoomableCanvas),
                new FrameworkPropertyMetadata(Stretch.Uniform, OnStretchChanged), IsStretchValid);

        /// <summary>
        ///   Gets or sets a value that specifies how the content of the canvas
        ///   is displayed when <see cref="Viewbox"/> is set.
        /// </summary>
        /// <value>
        ///   One of the <see cref="Stretch"/> values other than
        ///   <see cref="F:Stretch.Fill"/>. The default is <see cref="F:Stretch.Uniform"/>.
        /// </value>
        /// <remarks>
        ///   Please see the documentation of <see cref="TileBrush.Stretch"/>
        ///   for a detailed explanation of the effects of this property. The
        ///   <see cref="Stretch"/> mode of <see cref="F:Stretch.Fill"/> is not
        ///   supported, so unless the aspect ratio of <see cref="Viewbox"/>
        ///   exactly matches the aspect ratio of <see cref="UIElement.RenderSize"/> the
        ///   actual area displayed will be more or less than <see cref="Viewbox"/>.
        ///   The exact area that is displayed can be determined by the
        ///   <see cref="ActualViewbox"/> property in this case.
        /// </remarks>
        /// <seealso cref="TileBrush.Stretch"/>
        public Stretch Stretch
        {
            get => (Stretch)GetValue(StretchProperty);
            set => SetValue(StretchProperty, value);
        }

        /// <summary>
        ///   Determines whether the value given is a valid value for the
        ///   <see cref="Stretch"/> dependency property.
        /// </summary>
        /// <param name="value">
        ///   The potential value for the dependency property.
        /// </param>
        /// <returns>
        ///   <see langword="true"/> if the value is a valid value for the property; otherwise,
        ///   <see langword="false"/>.
        /// </returns>
        private static bool IsStretchValid(object value)
        {
            var stretch = (Stretch)value;
            return stretch == Stretch.None
                   || stretch == Stretch.Uniform
                   || stretch == Stretch.UniformToFill;
        }

        /// <summary>
        ///   Handles the event that occurs when the value of the <see cref="Stretch"/>
        ///   dependency property has changed.
        /// </summary>
        /// <param name="d">
        ///   The dependency object on which the dependency property has changed.
        /// </param>
        /// <param name="e">
        ///   The event args containing the old and new values of the dependency
        ///   property.
        /// </param>
        private static void OnStretchChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            d.CoerceValue(ScaleProperty);
            d.CoerceValue(OffsetProperty);
        }

        #endregion

        #region StretchDirectionProperty

        /// <summary>
        ///   Identifies the <see cref="StretchDirection"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty StretchDirectionProperty =
            DependencyProperty.Register(
                nameof(StretchDirection),
                typeof(StretchDirection),
                typeof(ZoomableCanvas),
                new FrameworkPropertyMetadata(StretchDirection.Both, OnStretchDirectionChanged),
                IsStretchDirectionValid);

        /// <summary>
        ///   Gets or sets how setting the <see cref="Viewbox"/> property can
        ///   affect the <see cref="Scale"/>.
        /// </summary>
        /// <value>
        ///   One of the <see cref="StretchDirection"/> values. The default is
        ///   <see cref="F:StretchDirection.Both"/>
        /// </value>
        /// <remarks>
        ///   When setting the <see cref="Viewbox"/> property, the <see cref="Scale"/>
        ///   and <see cref="Offset"/> properties are automatically coerced to
        ///   the appropriate values according to the <see cref="Stretch"/> and
        ///   <see cref="StretchDirection"/> properties, and any existing values
        ///   of <see cref="Scale"/> and <see cref="Offset"/> will be overridden.
        ///   However, when the value of <see cref="StretchDirection"/> is set
        ///   to anything other than <see cref="F:StretchDirection.Both"/>, then
        ///   the setting of the <see cref="Scale"/> property can limit the range
        ///   of the automatically computed value. The exact area that is displayed
        ///   can be determined by the <see cref="ActualViewbox"/> property in
        ///   this case.
        /// </remarks>
        public StretchDirection StretchDirection
        {
            get => (StretchDirection)GetValue(StretchDirectionProperty);
            set => SetValue(StretchDirectionProperty, value);
        }

        /// <summary>
        ///   Determines whether the value given is a valid value for the
        ///   <see cref="StretchDirection"/> dependency property.
        /// </summary>
        /// <param name="value">
        ///   The potential value for the dependency property.
        /// </param>
        /// <returns>
        ///   <see langword="true"/> if the value is a valid value for the property; otherwise,
        ///   <see langword="false"/>.
        /// </returns>
        private static bool IsStretchDirectionValid(object value)
        {
            var stretch = (StretchDirection)value;
            return stretch == StretchDirection.Both
                   || stretch == StretchDirection.UpOnly
                   || stretch == StretchDirection.DownOnly;
        }

        /// <summary>
        ///   Handles the event that occurs when the value of the
        ///   <see cref="StretchDirection"/> dependency property has changed.
        /// </summary>
        /// <param name="d">
        ///   The dependency object on which the dependency property has changed.
        /// </param>
        /// <param name="e">
        ///   The event args containing the old and new values of the dependency
        ///   property.
        /// </param>
        private static void OnStretchDirectionChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            d.CoerceValue(ScaleProperty);
            d.CoerceValue(OffsetProperty);
        }

        #endregion

        #region OffsetProperty

        /// <summary>
        ///   Identifies the <see cref="Offset"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty OffsetProperty =
            DependencyProperty.Register(
                nameof(Offset),
                typeof(Vector),
                typeof(ZoomableCanvas),
                new FrameworkPropertyMetadata(
                    new Vector(),
                    FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                    OnOffsetChanged,
                    CoerceOffset),
                IsOffsetValid);

        /// <summary>
        ///   Gets or sets (but see remarks) the top-left point of the area of
        ///   the canvas (in canvas coordinates) that is being displayed by this
        ///   panel.
        /// </summary>
        /// <value>
        ///   A <see cref="Point"/> on the canvas (in canvas coordinates). The
        ///   default is <c>(0,0)</c>.
        /// </value>
        /// <remarks>
        ///   This value controls the horizontal and vertical position of the
        ///   canvas children relative to this panel.
        ///   <para>
        ///     For example, consider a child element which has its
        ///     <see cref="Canvas.LeftProperty"/> and <see cref="Canvas.TopProperty"/>
        ///     set to <c>100</c> and <c>100</c>. Also assume that the value of
        ///     <see cref="Scale"/> is set to <c>1.0</c> (the default value).
        ///   </para>
        ///   <para>
        ///     By default, the value of <see cref="Offset"/> is <c>(0,0)</c> so
        ///     the element will be displayed at 100 units to the right and 100
        ///     units down from the top-left corner of this panel, exactly how
        ///     <see cref="Canvas"/> would display it. If the value of
        ///     <see cref="Offset"/> is set to <c>(20,40)</c> then the element
        ///     will be displayed 80 units to the right and 60 units down from
        ///     the top-left corner of this panel. In other words, it will have
        ///     appeared to "move" up by 20 units and left by 40 units.
        ///   </para>
        ///   <para>
        ///     If the value of <see cref="Offset"/> is set to <c>(100,100)</c>
        ///     then the top-left corner of the element will be displayed exactly
        ///     in the top-left corner of this panel. Note that this is true
        ///     regardless of the value of <see cref="Scale"/>!
        ///   </para>
        ///   <para>
        ///     If the value of <see cref="Offset"/> is set to <c>(110,120)</c>
        ///     then the top-left corner of the element will be displayed 10
        ///     units to the left and 20 units above this panel. In other words,
        ///     if <see cref="P:ClipToBounds"/> is set to <see langword="true"/>,
        ///     then the top-left corner of the element will not be visible.
        ///   </para>
        ///   <para>
        ///     The value of <see cref="Offset"/> can also be negative, so if
        ///     the value of <see cref="Offset"/> is set to <c>(-100,-100)</c>
        ///     then the element will be displayed at 200 pixels to the right
        ///     and 200 pixels down from the top-left corner of the panel.
        ///   </para>
        ///   <para>
        ///     When the <see cref="Viewbox"/> property is set to a non-
        ///     <see cref="Rect.Empty"/> value, the value of the <see cref="Offset"/>
        ///     property will be automatically computed to match the
        ///     <see cref="Viewbox"/>, <see cref="Stretch"/>, and
        ///     <see cref="StretchDirection"/> properties. The value of the
        ///     <see cref="Offset"/> property will contain the computed value
        ///     (via the WPF dependency property coersion mechanism), and any
        ///     attempts to set <see cref="Offset"/> to a different value will
        ///     be ignored until <see cref="Viewbox"/> is set to <see cref="Rect.Empty"/>
        ///     again.
        ///   </para>
        /// </remarks>
        /// <seealso cref="Viewbox"/>
        public Vector Offset
        {
            get => (Vector)GetValue(OffsetProperty);
            set => SetValue(OffsetProperty, value);
        }

        public event EventHandler? OffsetChanged;

        /// <summary>
        ///   Determines whether the value given is a valid value for the
        ///   <see cref="Offset"/> dependency property.
        /// </summary>
        /// <param name="value">
        ///   The potential value for the dependency property.
        /// </param>
        /// <returns>
        ///   <see langword="true"/> if the value is a valid value for the property; otherwise,
        ///   <see langword="false"/>.
        /// </returns>
        private static bool IsOffsetValid(object value)
        {
            var vector = (Vector)value;
            return vector.X.IsBetween(double.MinValue, double.MaxValue)
                   && vector.Y.IsBetween(double.MinValue, double.MaxValue);
        }

        /// <summary>
        ///   Returns a <see cref="Point"/> representing top-left point of the
        ///   canvas (in canvas coordinates) that is currently being displayed
        ///   after taking <see cref="Viewbox"/> into account.
        /// </summary>
        /// <param name="d">Dependency object whos value is being coerced.</param>
        /// <param name="value">The original uncoerced value.</param>
        /// <returns>
        ///   A <see cref="Point"/> representing top-left point of the canvas
        ///   (in canvas coordinates) that is currently being displayed.
        /// </returns>
        private static object CoerceOffset(DependencyObject d, object value)
        {
            var canvas = (ZoomableCanvas)d;
            var viewbox = canvas.Viewbox;
            if (!viewbox.IsEmpty) {
                var scale = canvas.Scale;
                var renderSize = canvas.RenderSize;
                value = new Vector((viewbox.X + viewbox.Width / 2) * scale - renderSize.Width / 2, (viewbox.Y + viewbox.Height / 2) * scale - renderSize.Height / 2);
            }

            return value;
        }

        /// <summary>
        ///   Handles the event that occurs when the value of the <see cref="Offset"/>
        ///   dependency property has changed.
        /// </summary>
        /// <param name="d">
        ///   The dependency object on which the dependency property has changed.
        /// </param>
        /// <param name="e">
        ///   The event args containing the old and new values of the dependency
        ///   property.
        /// </param>
        private static void OnOffsetChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            d.CoerceValue(ActualViewboxProperty);

            var canvas = (ZoomableCanvas)d;
            canvas.OffsetOverride((Vector)e.NewValue);
            canvas.OffsetChanged?.Invoke(canvas, EventArgs.Empty);
        }

        #endregion

        #region ScaleProperty

        /// <summary>
        ///   Identifies the <see cref="Scale"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ScaleProperty =
            DependencyProperty.Register(
                nameof(Scale),
                typeof(double),
                typeof(ZoomableCanvas),
                new FrameworkPropertyMetadata(
                    1.0,
                    FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                    OnScaleChanged,
                    CoerceScale),
                IsScaleValid);

        /// <summary>
        ///   Gets or sets (but see remarks) the scale at which the content of
        ///   the canvas is being displayed.
        /// </summary>
        /// <value>
        ///   A <see cref="double"/> between <see cref="double.Epsilon"/> and
        ///   <see cref="double.MaxValue"/>. The default value is <c>1.0</c>.
        /// </value>
        /// <remarks>
        ///   This value is what controls the zoom level of the canvas and the
        ///   amount of the <see cref="ScaleTransform"/> when
        ///   <see cref="ApplyTransform"/> is set to <see langword="true"/>.
        ///   When <see cref="ApplyTransform"/> is set to <see langword="false"/>,
        ///   this value still controls the positioning of the children (i.e.
        ///   elements are placed closer together when zoomed out and farther
        ///   apart when zoomed in), but the sizes of the children are unaffected.
        ///   <para>
        ///     For example, consider a child element which has its
        ///     <see cref="Canvas.LeftProperty"/> and <see cref="Canvas.TopProperty"/>
        ///     set to <c>100</c> and <c>100</c>, with a
        ///     <see cref="FrameworkElement.Width"/> of 50 and a
        ///     <see cref="FrameworkElement.Height"/> of 50. Also assume that
        ///     the value of <see cref="Offset"/> is set to <c>(0,0)</c> (the
        ///     default value).
        ///   </para>
        ///   <para>
        ///     By default, the value of <see cref="Scale"/> is <c>1.0</c> so
        ///     the element will be displayed at 100 units to the right and 100
        ///     units down from the top-left corner of this panel, exactly how
        ///     <see cref="Canvas"/> would display it. If the value of
        ///     <see cref="Scale"/> is set to <c>0.8</c> then the top-left corner
        ///     of the element will be displayed 80 units to the right and 80
        ///     units down from the top-left corner of this panel. If
        ///     <see cref="ApplyTransform"/> is set to <see langword="true"/>
        ///     (the default value), then the element will also be scaled down
        ///     (shrunk) to 80% of its normal size, so that the bottom-right of
        ///     the element will be 120 units to the right and 120 units down
        ///     from the top-left corner of this panel. If <see cref="ApplyTransform"/>
        ///     is set to <see langword="false"/>, then the element will remain
        ///     its original size, resulting in the bottom-right of the element
        ///     being 130 units to the right and 130 units down from the top-left
        ///     corner of this panel. In other words, it will simply have appeared
        ///     to "move" up by 20 units and left by 20 units without changing
        ///     its size. This is not normally what a user would expect when
        ///     "zooming out" (unless the element is some kind of floating label
        ///     above the canvas), so it is expected that the children of the
        ///     canvas will be responsible for changing their representation
        ///     appropriately when <see cref="ApplyTransform"/> is set to
        ///     <see langword="false"/>.
        ///   </para>
        ///   <para>
        ///     When the <see cref="Viewbox"/> property is set to a non-
        ///     <see cref="Rect.Empty"/> value, the value of the <see cref="Scale"/>
        ///     property will be automatically computed to match the
        ///     <see cref="Viewbox"/>, <see cref="Stretch"/>, and
        ///     <see cref="StretchDirection"/> properties. The value of the
        ///     <see cref="Scale"/> property will contain the computed value
        ///     (via the WPF dependency property coersion mechanism), but and
        ///     any attempts to set <see cref="Scale"/> to a different value
        ///     will be ignored (if <see cref="StretchDirection"/> is set to
        ///     <see cref="F:StretchDirection.Both"/>) until <see cref="Viewbox"/>
        ///     is set to <see cref="Rect.Empty"/> again.
        ///   </para>
        /// </remarks>
        /// <seealso cref="Viewbox"/>
        public double Scale
        {
            get => (double)GetValue(ScaleProperty);
            set => SetValue(ScaleProperty, value);
        }

        public event EventHandler? ScaleChanged;

        /// <summary>
        ///   Determines whether the value given is a valid value for the
        ///   <see cref="Scale"/> dependency property.
        /// </summary>
        /// <param name="value">
        ///   The potential value for the dependency property.
        /// </param>
        /// <returns>
        ///   <see langword="true"/> if the value is a valid value for the property; otherwise,
        ///   <see langword="false"/>.
        /// </returns>
        private static bool IsScaleValid(object value)
        {
            return ((double)value).IsBetween(double.Epsilon, double.MaxValue);
        }

        /// <summary>
        ///   Returns a <see cref="double"/> representing the scale of the content
        ///   that is currently being displayed after taking <see cref="Viewbox"/>,
        ///   <see cref="Stretch"/>, and <see cref="StretchDirection"/> into
        ///   account.
        /// </summary>
        /// <param name="d">Dependency object whos value is being coerced.</param>
        /// <param name="value">The original uncoerced value.</param>
        /// <returns>
        ///   A <see cref="double"/> representing scale of the content that is
        ///   currently being displayed.
        /// </returns>
        private static object CoerceScale(DependencyObject d, object value)
        {
            var scale = (double)value;

            var canvas = (ZoomableCanvas)d;
            var renderSize = canvas.RenderSize;
            if (renderSize.Width > 0 && renderSize.Height > 0) {
                var viewbox = canvas.Viewbox;
                if (!viewbox.IsEmpty) {
                    switch (canvas.Stretch) {
                        case Stretch.Uniform:
                            scale = Math.Min(
                                renderSize.Width / viewbox.Width,
                                renderSize.Height / viewbox.Height);
                            break;

                        case Stretch.UniformToFill:
                            scale = Math.Max(
                                renderSize.Width / viewbox.Width,
                                renderSize.Height / viewbox.Height);
                            break;
                    }

                    switch (canvas.StretchDirection) {
                        case StretchDirection.DownOnly:
                            scale = scale.AtMost((double)value);
                            break;

                        case StretchDirection.UpOnly:
                            scale = scale.AtLeast((double)value);
                            break;
                    }
                }
            }

            return scale;
        }

        /// <summary>
        ///   Handles the event that occurs when the value of the <see cref="Scale"/>
        ///   dependency property has changed.
        /// </summary>
        /// <param name="d">
        ///   The dependency object on which the dependency property has changed.
        /// </param>
        /// <param name="e">
        ///   The event args containing the old and new values of the dependency
        ///   property.
        /// </param>
        private static void OnScaleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            d.CoerceValue(ActualViewboxProperty);
            d.CoerceValue(OffsetProperty);

            var canvas = (ZoomableCanvas)d;
            canvas.ScaleOverride((double)e.NewValue);
            canvas.ScaleChanged?.Invoke(canvas, EventArgs.Empty);
        }

        #endregion

        /// <summary>
        ///   Refreshes our data when the <see cref="Panel.IsItemsHost"/> property
        ///   has changed.
        /// </summary>
        protected override void OnIsItemsHostChanged(bool oldIsItemsHost, bool newIsItemsHost)
        {
            OnItemsReset();
            base.OnIsItemsHostChanged(oldIsItemsHost, newIsItemsHost);
        }

        /// <summary>
        ///   Resets and initializes our spatial indices when the items source
        ///   has changed.
        /// </summary>
        private void OnItemsReset()
        {
            InvalidateExtent();
        }

        #region Arrange Logic

        /// <summary>
        ///   Updates the calculated <see cref="ActualViewbox"/> and the
        ///   <see cref="Scale"/> and <see cref="Offset"/> when the size changes.
        /// </summary>
        /// <param name="sizeInfo">Size information about the render size.</param>
        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        {
            CoerceValue(ScaleProperty);
            CoerceValue(OffsetProperty);
            CoerceValue(ActualViewboxProperty);

            base.OnRenderSizeChanged(sizeInfo);
        }

        /// <summary>
        ///   Invalidates the arrangement of canvases when their children's positions
        ///   change.
        /// </summary>
        /// <param name="d">Dependency object whos position has changed.</param>
        /// <param name="e">Event arguments related to the change.</param>
        private static void OnPositioningChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var parent = VisualTreeHelper.GetParent(d);
            if (parent is UIElement element)
                element.InvalidateArrange();
        }

        /// <summary>
        ///   Gets the applied scale transform if <see cref="ApplyTransform"/>
        ///   is set to <see langword="true"/>.
        /// </summary>
        private ScaleTransform? AppliedScaleTransform
        {
            get
            {
                if (ApplyTransform)
                    return (ScaleTransform)((TransformGroup)RenderTransform).Children[0];
                return null;
            }
        }

        /// <summary>
        ///   Gets the applied translate transform if <see cref="ApplyTransform"/>
        ///   is set to <see langword="true"/>.
        /// </summary>
        private TranslateTransform? AppliedTranslateTransform
        {
            get
            {
                if (ApplyTransform)
                    return (TranslateTransform)((TransformGroup)RenderTransform).Children[1];
                return null;
            }
        }

        /// <summary>
        ///   Scales the child elements of a <see cref="ZoomableCanvas"/> by
        ///   applying a transform if <see cref="ApplyTransform"/> is <see langword="true"/>,
        ///   or by calling <see cref="FrameworkElement.InvalidateArrange"/>
        ///   otherwise.
        /// </summary>
        /// <param name="scale">The new scale of the canvas.</param>
        protected virtual void ScaleOverride(double scale)
        {
            var appliedTransform = AppliedScaleTransform;
            if (appliedTransform != null) {
                appliedTransform.ScaleX = scale;
                appliedTransform.ScaleY = scale;
            } else {
                InvalidateArrange();
            }
        }

        /// <summary>
        ///   Offsets the child elements of a <see cref="ZoomableCanvas"/> by
        ///   applying a transform if <see cref="ApplyTransform"/> is <see langword="true"/>,
        ///   or by calling <see cref="FrameworkElement.InvalidateArrange"/>
        ///   otherwise.
        /// </summary>
        /// <param name="offset">The new offset of the canvas.</param>
        protected virtual void OffsetOverride(Vector offset)
        {
            var appliedTransform = AppliedTranslateTransform;
            if (appliedTransform != null) {
                appliedTransform.X = -offset.X;
                appliedTransform.Y = -offset.Y;
            } else {
                InvalidateArrange();
            }
        }

        /// <summary>
        ///   Measures the child elements of a <see cref="ZoomableCanvas"/> in
        ///   anticipation of arranging them during the <see cref="ArrangeOverride"/>
        ///   pass.
        /// </summary>
        /// <param name="availableSize">
        ///   An upper limit <see cref="Size"/> that should not be exceeded.
        /// </param>
        /// <returns>
        ///   A <see cref="Size"/> that represents the size that is required to
        ///   arrange child content.
        /// </returns>
        protected override Size MeasureOverride(Size availableSize)
        {
            var childConstraint = new Size(
                double.PositiveInfinity, double.PositiveInfinity);

            foreach (UIElement child in InternalChildren)
                child?.Measure(childConstraint);

            return new Size();
        }

        /// <summary>
        ///   Arranges the content of a <see cref="ZoomableCanvas"/> element.
        /// </summary>
        /// <param name="finalSize">
        ///   The size that this <see cref="ZoomableCanvas"/> element should use
        ///   to arrange its child elements.
        /// </param>
        /// <returns>
        ///   A <see cref="Size"/> that represents the arranged size of this
        ///   <see cref="ZoomableCanvas"/> element and its descendants.
        /// </returns>
        protected override Size ArrangeOverride(Size finalSize)
        {
            bool applyTransform = ApplyTransform;
            Vector offset = applyTransform ? new Vector() : Offset;
            double scale = applyTransform ? 1.0 : Scale;

            childrenExtent = Rect.Empty;

            foreach (UIElement child in InternalChildren) {
                if (child == null)
                    continue;

                var bounds = DetermineBounds(child);

                // Update the children extent for scrolling.
                childrenExtent.Union(bounds);

                // So far everything has been in canvas coordinates.
                // Here we adjust the result for the final call to Arrange.
                bounds.X *= scale;
                bounds.X -= offset.X;
                bounds.Y *= scale;
                bounds.Y -= offset.Y;
                bounds.Width *= scale;
                bounds.Height *= scale;

                // WPF Arrange will crash if the values are too large.
                bounds.X = bounds.X.AtLeast(float.MinValue / 2);
                bounds.Y = bounds.Y.AtLeast(float.MinValue / 2);
                bounds.Width = bounds.Width.AtMost(float.MaxValue);
                bounds.Height = bounds.Height.AtMost(float.MaxValue);

                child.Arrange(bounds);
            }

            InvalidateExtent();

            return finalSize;
        }

        private static Point DeterminePosition(UIElement element)
        {
            if (element.ReadLocalValue(CanvasService.PositionProperty) != DependencyProperty.UnsetValue)
                return CanvasService.GetPosition(element);
            return new Point(GetLeft(element), GetTop(element));
        }

        private static Rect DetermineBounds(UIElement element)
        {
            var position = DeterminePosition(element);
            var bounds = new Rect(
                position.X.GetFiniteValueOrDefault(),
                position.Y.GetFiniteValueOrDefault(),
                element.DesiredSize.Width,
                element.DesiredSize.Height);

            var horizontalAlignment = (HorizontalAlignment)element.GetValue(
                HorizontalAlignmentProperty);
            var verticalAlignment = (VerticalAlignment)element.GetValue(
                VerticalAlignmentProperty);

            switch (horizontalAlignment) {
                case HorizontalAlignment.Center:
                    bounds.X -= bounds.Size.Width / 2;
                    break;
                case HorizontalAlignment.Right:
                    bounds.X -= bounds.Size.Width;
                    break;
            }

            switch (verticalAlignment) {
                case VerticalAlignment.Center:
                    bounds.Y -= bounds.Size.Height / 2;
                    break;
                case VerticalAlignment.Bottom:
                    bounds.Y -= bounds.Size.Height;
                    break;
            }

            return bounds;
        }

        /// <summary>
        ///   Returns a clipping geometry that indicates the area that will be
        ///   clipped if the <see cref="UIElement.ClipToBounds"/> property is
        ///   set to <see langword="true"/>.
        /// </summary>
        /// <param name="layoutSlotSize">The available size of the element.</param>
        /// <returns>
        ///   A <see cref="Geometry"/> that represents the area that is clipped
        ///   if <see cref="UIElement.ClipToBounds"/> is <see langword="true"/>.
        /// </returns>
        protected override Geometry? GetLayoutClip(Size layoutSlotSize)
        {
            // ZoomableCanvas only clips to bounds if ClipToBounds is set, no automatic clipping.
            return ClipToBounds ? new RectangleGeometry(new Rect(RenderSize)) : null;
        }

        #endregion

        #region Utility Methods

        /// <summary>
        ///   Represents the extent of the instantiated UIElements calculated
        ///   during <see cref="ArrangeOverride"/>.
        /// </summary>
        private Rect childrenExtent = Rect.Empty;

        /// <summary>
        ///   Caches the calculated <see cref="Extent"/> based on the spatial
        ///   index and arranged children of the canvas until
        ///   <see cref="InvalidateExtent"/> is called.
        /// </summary>
        private Rect computedExtent = Rect.Empty;

        /// <summary>
        ///   Gets the extent of the populated area of the canvas (in canvas
        ///   coordinates).
        /// </summary>
        /// <remarks>
        ///   This property is also used to determine the range of the scroll
        ///   bars when the canvas is hosted within a <see cref="ScrollViewer"/>.
        /// </remarks>
        public virtual Rect Extent
        {
            get
            {
                if (computedExtent.IsEmpty) {
                    //computedExtent = Rect.Union(childrenExtent, Rect.Empty);
                    if (childrenExtent.IsEmpty)
                        computedExtent = childrenExtent;
                    else
                        computedExtent = new Rect(
                            childrenExtent.X - (childrenExtent.Width + ActualWidth) / 2,
                            childrenExtent.Y - (childrenExtent.Height + ActualHeight) / 2,
                            childrenExtent.Width * 2 + ActualWidth,
                            childrenExtent.Height * 2 + ActualHeight);
                }
                return computedExtent;
            }
        }

        /// <summary>
        ///   Re-computes the <see cref="Extent"/> of items in the canvas and
        ///   updates the parent scroll viewer if there is one.
        /// </summary>
        protected void InvalidateExtent()
        {
            computedExtent = Rect.Empty;
            var owner = ((IScrollInfo)this).ScrollOwner;
            owner?.InvalidateScrollInfo();
        }

        /// <summary>
        ///   Gets the current visual coordinates for a given <see cref="Point"/>
        ///   on this <see cref="ZoomableCanvas"/>.
        /// </summary>
        /// <param name="canvasPoint">
        ///   The <see cref="Point"/> in canvas coordinates.
        /// </param>
        /// <returns>
        ///   The current position of the canvas point on the screen relative to
        ///   the upper-left corner of this <see cref="ZoomableCanvas"/>.
        /// </returns>
        public Point PointFromCanvas(Point canvasPoint)
        {
            return (Point)(((Vector)canvasPoint * Scale) - Offset);
        }

        /// <summary>
        ///   Gets the point on the canvas that is currently represented by the
        ///   given <see cref="Point"/> on the screen.
        /// </summary>
        /// <param name="visualPoint">
        ///   The <see cref="Point"/> on the screen relative to the upper-left
        ///   corner of this <see cref="ZoomableCanvas"/>.
        /// </param>
        /// <returns>
        ///   The point on the canvas that corresponds to the given point on the
        ///   screen.
        /// </returns>
        public Point PointToCanvas(Point visualPoint)
        {
            return (Point)(((Vector)visualPoint + Offset) / Scale);
        }

        /// <summary>
        ///   Returns the the point on the canvas at which the mouse cursor is
        ///   currently located.
        /// </summary>
        public Point MousePosition
        {
            get
            {
                var position = Mouse.GetPosition(this);
                if (ApplyTransform)
                    return position;
                return PointToCanvas(position);
            }
        }

        #endregion

        #region IScrollInfo Implementation

        [SuppressMessage("Microsoft.Design", "CA1033:InterfaceMethodsShouldBeCallableByChildTypes")]
        ScrollViewer? IScrollInfo.ScrollOwner { get; set; }

        [SuppressMessage("Microsoft.Design", "CA1033:InterfaceMethodsShouldBeCallableByChildTypes")]
        bool IScrollInfo.CanHorizontallyScroll { get; set; }

        [SuppressMessage("Microsoft.Design", "CA1033:InterfaceMethodsShouldBeCallableByChildTypes")]
        bool IScrollInfo.CanVerticallyScroll { get; set; }

        [SuppressMessage("Microsoft.Design", "CA1033:InterfaceMethodsShouldBeCallableByChildTypes")]
        double IScrollInfo.ViewportWidth => ActualViewbox.Width * Scale;

        [SuppressMessage("Microsoft.Design", "CA1033:InterfaceMethodsShouldBeCallableByChildTypes")]
        double IScrollInfo.ViewportHeight => ActualViewbox.Height * Scale;

        [SuppressMessage("Microsoft.Design", "CA1033:InterfaceMethodsShouldBeCallableByChildTypes")]
        double IScrollInfo.ExtentWidth =>
            Math.Max(Math.Max(ActualViewbox.Right, Extent.Right) - Math.Min(ActualViewbox.Left, Extent.Left), 0.0) * Scale;

        [SuppressMessage("Microsoft.Design", "CA1033:InterfaceMethodsShouldBeCallableByChildTypes")]
        double IScrollInfo.ExtentHeight =>
            Math.Max(Math.Max(ActualViewbox.Bottom, Extent.Bottom) - Math.Min(ActualViewbox.Top, Extent.Top), 0.0) * Scale;

        [SuppressMessage("Microsoft.Design", "CA1033:InterfaceMethodsShouldBeCallableByChildTypes")]
        double IScrollInfo.HorizontalOffset => Math.Max(ActualViewbox.X - Extent.X, 0.0) * Scale;

        [SuppressMessage("Microsoft.Design", "CA1033:InterfaceMethodsShouldBeCallableByChildTypes")]
        double IScrollInfo.VerticalOffset => Math.Max(ActualViewbox.Y - Extent.Y, 0.0) * Scale;

        [SuppressMessage("Microsoft.Design", "CA1033:InterfaceMethodsShouldBeCallableByChildTypes")]
        void IScrollInfo.LineDown()
        {
            ((IScrollInfo)this).SetVerticalOffset(((IScrollInfo)this).VerticalOffset + 16);
        }

        [SuppressMessage("Microsoft.Design", "CA1033:InterfaceMethodsShouldBeCallableByChildTypes")]
        void IScrollInfo.LineLeft()
        {
            ((IScrollInfo)this).SetHorizontalOffset(((IScrollInfo)this).HorizontalOffset - 16);
        }

        [SuppressMessage("Microsoft.Design", "CA1033:InterfaceMethodsShouldBeCallableByChildTypes")]
        void IScrollInfo.LineRight()
        {
            ((IScrollInfo)this).SetHorizontalOffset(((IScrollInfo)this).HorizontalOffset + 16);
        }

        [SuppressMessage("Microsoft.Design", "CA1033:InterfaceMethodsShouldBeCallableByChildTypes")]
        void IScrollInfo.LineUp()
        {
            ((IScrollInfo)this).SetVerticalOffset(((IScrollInfo)this).VerticalOffset - 16);
        }

        [SuppressMessage("Microsoft.Design", "CA1033:InterfaceMethodsShouldBeCallableByChildTypes")]
        void IScrollInfo.MouseWheelDown()
        {
            ((IScrollInfo)this).SetVerticalOffset(((IScrollInfo)this).VerticalOffset + 48);
        }

        [SuppressMessage("Microsoft.Design", "CA1033:InterfaceMethodsShouldBeCallableByChildTypes")]
        void IScrollInfo.MouseWheelLeft()
        {
            ((IScrollInfo)this).SetHorizontalOffset(((IScrollInfo)this).HorizontalOffset - 48);
        }

        [SuppressMessage("Microsoft.Design", "CA1033:InterfaceMethodsShouldBeCallableByChildTypes")]
        void IScrollInfo.MouseWheelRight()
        {
            ((IScrollInfo)this).SetHorizontalOffset(((IScrollInfo)this).HorizontalOffset + 48);
        }

        [SuppressMessage("Microsoft.Design", "CA1033:InterfaceMethodsShouldBeCallableByChildTypes")]
        void IScrollInfo.MouseWheelUp()
        {
            ((IScrollInfo)this).SetVerticalOffset(((IScrollInfo)this).VerticalOffset - 48);
        }

        [SuppressMessage("Microsoft.Design", "CA1033:InterfaceMethodsShouldBeCallableByChildTypes")]
        void IScrollInfo.PageDown()
        {
            ((IScrollInfo)this).SetVerticalOffset(((IScrollInfo)this).VerticalOffset + ((IScrollInfo)this).ViewportHeight);
        }

        [SuppressMessage("Microsoft.Design", "CA1033:InterfaceMethodsShouldBeCallableByChildTypes")]
        void IScrollInfo.PageLeft()
        {
            ((IScrollInfo)this).SetHorizontalOffset(((IScrollInfo)this).HorizontalOffset - ((IScrollInfo)this).ViewportWidth);
        }

        [SuppressMessage("Microsoft.Design", "CA1033:InterfaceMethodsShouldBeCallableByChildTypes")]
        void IScrollInfo.PageRight()
        {
            ((IScrollInfo)this).SetHorizontalOffset(((IScrollInfo)this).HorizontalOffset + ((IScrollInfo)this).ViewportWidth);
        }

        [SuppressMessage("Microsoft.Design", "CA1033:InterfaceMethodsShouldBeCallableByChildTypes")]
        void IScrollInfo.PageUp()
        {
            ((IScrollInfo)this).SetVerticalOffset(((IScrollInfo)this).VerticalOffset - ((IScrollInfo)this).ViewportHeight);
        }

        [SuppressMessage("Microsoft.Design", "CA1033:InterfaceMethodsShouldBeCallableByChildTypes")]
        void IScrollInfo.SetHorizontalOffset(double offset)
        {
            var scrollInfo = (IScrollInfo)this;
            offset = offset.Clamp(0, scrollInfo.ExtentWidth - scrollInfo.ViewportWidth);

            var viewbox = Viewbox;
            if (viewbox.IsEmpty) {
                Offset += new Vector(offset - scrollInfo.HorizontalOffset, 0);
            } else {
                viewbox.X += (offset - scrollInfo.HorizontalOffset) / Scale;
                Viewbox = viewbox;
            }
        }

        [SuppressMessage("Microsoft.Design", "CA1033:InterfaceMethodsShouldBeCallableByChildTypes")]
        void IScrollInfo.SetVerticalOffset(double offset)
        {
            var scrollInfo = (IScrollInfo)this;
            offset = offset.Clamp(0, scrollInfo.ExtentHeight - scrollInfo.ViewportHeight);

            var viewbox = Viewbox;
            if (viewbox.IsEmpty) {
                Offset += new Vector(0, offset - scrollInfo.VerticalOffset);
            } else {
                viewbox.Y += (offset - scrollInfo.VerticalOffset) / Scale;
                Viewbox = viewbox;
            }
        }

        [SuppressMessage("Microsoft.Design", "CA1033:InterfaceMethodsShouldBeCallableByChildTypes")]
        Rect IScrollInfo.MakeVisible(Visual? visual, Rect rectangle)
        {
            if (rectangle.IsEmpty || visual == null || !IsAncestorOf(visual))
                return Rect.Empty;

            rectangle = visual.TransformToAncestor(this).TransformBounds(rectangle);
            rectangle = RenderTransform.TransformBounds(rectangle);

            var width = ((IScrollInfo)this).ViewportWidth;
            var height = ((IScrollInfo)this).ViewportHeight;
            var left = -rectangle.X;
            var top = -rectangle.Y;
            var right = left + width - rectangle.Width;
            var bottom = top + height - rectangle.Height;
            var deltaX = left > 0 && right > 0 ? Math.Min(left, right) : left < 0 && right < 0 ? Math.Max(left, right) : 0.0;
            var deltaY = top > 0 && bottom > 0 ? Math.Min(top, bottom) : top < 0 && bottom < 0 ? Math.Max(top, bottom) : 0.0;
            var delta = new Vector(deltaX, deltaY);

            Offset -= delta;

            rectangle.Location += delta;
            rectangle.Intersect(new Rect(0, 0, width, height));

            return rectangle;
        }

        #endregion
    }
}
