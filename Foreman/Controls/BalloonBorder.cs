namespace Foreman.Controls
{
    using System;
    using System.Runtime.InteropServices;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Media;
    using Extensions;

    public class BalloonBorder : Border
    {
        private StreamGeometry borderGeometryCache;
        private StreamGeometry backgroundGeometryCache;

        public static readonly DependencyProperty ArrowHeadLengthProperty =
            DependencyProperty.Register(
                nameof(ArrowHeadLength),
                typeof(double),
                typeof(BalloonBorder),
                new FrameworkPropertyMetadata(
                    7.0, FrameworkPropertyMetadataOptions.AffectsMeasure));

        public static readonly DependencyProperty ArrowHeadWidthProperty =
            DependencyProperty.Register(
                nameof(ArrowHeadWidth),
                typeof(double),
                typeof(BalloonBorder),
                new FrameworkPropertyMetadata(
                    10.0, FrameworkPropertyMetadataOptions.AffectsMeasure));

        public static readonly DependencyProperty ArrowDirectionProperty =
            DependencyProperty.Register(
                nameof(ArrowDirection),
                typeof(Direction),
                typeof(BalloonBorder),
                new FrameworkPropertyMetadata(
                    Direction.Left, FrameworkPropertyMetadataOptions.AffectsMeasure));

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

        protected override Size MeasureOverride(Size constraint)
        {
            UIElement child = Child;
            var desired = new Size();

            Thickness borders = BorderThickness;
            if (UseLayoutRounding) {
                DpiScale dpi = VisualTreeHelper.GetDpi(this);
                borders = LayoutUtils.RoundLayoutValue(borders, dpi);
            }

            // Compute the chrome size added by the various elements
            Size border = CollapseThickness(borders);
            Size padding = CollapseThickness(Padding);

            // If we have a child
            if (child != null) {
                // Combine into total decorating size
                var combined = new Size(
                    border.Width + padding.Width, border.Height + padding.Height);

                // Remove size of border only from child's reference size.
                var childConstraint = new Size(
                    Math.Max(0.0, constraint.Width - combined.Width),
                    Math.Max(0.0, constraint.Height - combined.Height));

                childConstraint = DeflateSize(childConstraint, ArrowDirection, ArrowHeadLength);

                child.Measure(childConstraint);
                Size childSize = child.DesiredSize;

                // Now use the returned size to drive our size, by adding back the margins, etc.
                desired.Width = childSize.Width + combined.Width;
                desired.Height = childSize.Height + combined.Height;
            } else {
                // Combine into total decorating size
                desired = new Size(border.Width + padding.Width, border.Height + padding.Height);
            }

            desired = InflateSize(desired, ArrowDirection, ArrowHeadLength);

            return desired;
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            Thickness borders = BorderThickness;
            if (UseLayoutRounding) {
                DpiScale dpi = VisualTreeHelper.GetDpi(this);
                borders = LayoutUtils.RoundLayoutValue(borders, dpi);
            }

            Rect boundRect = DeflateRect(new Rect(finalSize), ArrowDirection, ArrowHeadLength);
            Rect innerRect = DeflateRect(boundRect, borders);

            UIElement child = Child;
            if (child != null) {
                Rect childRect = DeflateRect(innerRect, Padding);
                child.Arrange(childRect);
            }

            CornerRadius radii = CornerRadius;
            var innerRadii = new Radii(radii, borders, false);

            StreamGeometry backgroundGeometry = null;

            bool hasBorder = !boundRect.Width.IsZero() && !boundRect.Height.IsZero();
            if (!innerRect.Width.IsZero() && !innerRect.Height.IsZero()) {
                backgroundGeometry = new StreamGeometry();

                using (var ctx = backgroundGeometry.Open()) {
                    GenerateGeometry(ctx, innerRect, innerRadii);
                    if (!hasBorder)
                        GenerateArrowHeadGeometry(ctx, innerRect);
                }

                backgroundGeometry.Freeze();
                backgroundGeometryCache = backgroundGeometry;
            } else {
                backgroundGeometryCache = null;
            }

            if (hasBorder) {
                var outerRadii = new Radii(radii, borders, true);
                var borderGeometry = new StreamGeometry();
                borderGeometry.FillRule = FillRule.Nonzero;

                using (var ctx = borderGeometry.Open()) {
                    GenerateGeometry(ctx, boundRect, outerRadii);
                    GenerateArrowHeadGeometry(ctx, innerRect);
                    if (backgroundGeometry != null)
                        GenerateGeometry(ctx, innerRect, innerRadii);
                }

                borderGeometry.Freeze();
                borderGeometryCache = borderGeometry;
            } else {
                borderGeometryCache = null;
            }

            return finalSize;
        }

        protected override void OnRender(DrawingContext dc)
        {
            Brush brush;
            StreamGeometry geometry;
            if ((geometry = borderGeometryCache) != null && (brush = BorderBrush) != null)
                dc.DrawGeometry(brush, null, geometry);

            if ((geometry = backgroundGeometryCache) != null && (brush = Background) != null)
                dc.DrawGeometry(brush, null, geometry);
        }

        private static Size DeflateSize(
            Size size, Direction arrowDirection, double arrowHeadLength)
        {
            switch (arrowDirection) {
                case Direction.Left:
                case Direction.Right:
                    size.Width -= arrowHeadLength;
                    break;
                case Direction.Up:
                case Direction.Down:
                    size.Height -= arrowHeadLength;
                    break;
            }

            return size;
        }

        private static Size InflateSize(
            Size size, Direction arrowDirection, double arrowHeadLength)
        {
            switch (arrowDirection) {
                case Direction.Left:
                case Direction.Right:
                    size.Width += arrowHeadLength;
                    break;
                case Direction.Up:
                case Direction.Down:
                    size.Height += arrowHeadLength;
                    break;
            }

            return size;
        }

        private static Rect DeflateRect(
            Rect rect, Direction arrowDirection, double arrowHeadLength)
        {
            switch (arrowDirection) {
                case Direction.Left:
                    arrowHeadLength = Math.Min(arrowHeadLength, rect.Width);
                    rect.X += arrowHeadLength;
                    rect.Width -= arrowHeadLength;
                    break;
                case Direction.Up:
                    arrowHeadLength = Math.Min(arrowHeadLength, rect.Height);
                    rect.Y += arrowHeadLength;
                    rect.Height -= arrowHeadLength;
                    break;
                case Direction.Right:
                    arrowHeadLength = Math.Min(arrowHeadLength, rect.Width);
                    rect.Width -= arrowHeadLength;
                    break;
                case Direction.Down:
                    arrowHeadLength = Math.Min(arrowHeadLength, rect.Height);
                    rect.Height -= arrowHeadLength;
                    break;
            }

            return rect;
        }

        private static void GenerateGeometry(StreamGeometryContext ctx, Rect rect, Radii radii)
        {
            //
            //  compute the coordinates of the key points
            //

            Point topLeft = new Point(radii.LeftTop, 0);
            Point topRight = new Point(rect.Width - radii.RightTop, 0);
            Point rightTop = new Point(rect.Width, radii.TopRight);
            Point rightBottom = new Point(rect.Width, rect.Height - radii.BottomRight);
            Point bottomRight = new Point(rect.Width - radii.RightBottom, rect.Height);
            Point bottomLeft = new Point(radii.LeftBottom, rect.Height);
            Point leftBottom = new Point(0, rect.Height - radii.BottomLeft);
            Point leftTop = new Point(0, radii.TopLeft);

            //
            //  check keypoints for overlap and resolve by partitioning radii according to
            //  the percentage of each one.
            //

            //  top edge is handled here
            if (topLeft.X > topRight.X) {
                double v = (radii.LeftTop) / (radii.LeftTop + radii.RightTop) * rect.Width;
                topLeft.X = v;
                topRight.X = v;
            }

            //  right edge
            if (rightTop.Y > rightBottom.Y) {
                double v = (radii.TopRight) / (radii.TopRight + radii.BottomRight) * rect.Height;
                rightTop.Y = v;
                rightBottom.Y = v;
            }

            //  bottom edge
            if (bottomRight.X < bottomLeft.X) {
                double v = (radii.LeftBottom) / (radii.LeftBottom + radii.RightBottom) * rect.Width;
                bottomRight.X = v;
                bottomLeft.X = v;
            }

            // left edge
            if (leftBottom.Y < leftTop.Y) {
                double v = (radii.TopLeft) / (radii.TopLeft + radii.BottomLeft) * rect.Height;
                leftBottom.Y = v;
                leftTop.Y = v;
            }

            //
            //  add on offsets
            //

            Vector offset = new Vector(rect.TopLeft.X, rect.TopLeft.Y);
            topLeft += offset;
            topRight += offset;
            rightTop += offset;
            rightBottom += offset;
            bottomRight += offset;
            bottomLeft += offset;
            leftBottom += offset;
            leftTop += offset;

            //
            //  create the border geometry
            //
            ctx.BeginFigure(topLeft, isFilled: true, isClosed: true);

            // Top line
            ctx.LineTo(topRight, isStroked: true, isSmoothJoin: false);

            // Upper-right corner
            double radiusX = rect.TopRight.X - topRight.X;
            double radiusY = rightTop.Y - rect.TopRight.Y;
            if (!radiusX.IsZero() || !radiusY.IsZero()) {
                ctx.ArcTo(
                    rightTop, new Size(radiusX, radiusY), 0, false,
                    SweepDirection.Clockwise, true, false);
            }

            // Right line
            ctx.LineTo(rightBottom, isStroked: true, isSmoothJoin: false);

            // Lower-right corner
            radiusX = rect.BottomRight.X - bottomRight.X;
            radiusY = rect.BottomRight.Y - rightBottom.Y;
            if (!radiusX.IsZero() || !radiusY.IsZero()) {
                ctx.ArcTo(
                    bottomRight, new Size(radiusX, radiusY), 0, false,
                    SweepDirection.Clockwise, true, false);
            }

            // Bottom line
            ctx.LineTo(bottomLeft, isStroked: true, isSmoothJoin: false);

            // Lower-left corner
            radiusX = bottomLeft.X - rect.BottomLeft.X;
            radiusY = rect.BottomLeft.Y - leftBottom.Y;
            if (!radiusX.IsZero() || !radiusY.IsZero()) {
                ctx.ArcTo(
                    leftBottom, new Size(radiusX, radiusY), 0, false,
                    SweepDirection.Clockwise, true, false);
            }

            // Left line
            ctx.LineTo(leftTop, isStroked: true, isSmoothJoin: false);

            // Upper-left corner
            radiusX = topLeft.X - rect.TopLeft.X;
            radiusY = leftTop.Y - rect.TopLeft.Y;
            if (!radiusX.IsZero() || !radiusY.IsZero()) {
                ctx.ArcTo(
                    topLeft, new Size(radiusX, radiusY), 0, false,
                    SweepDirection.Clockwise, true, false);
            }
        }

        private void GenerateArrowHeadGeometry(StreamGeometryContext ctx, Rect rect)
        {
            var center = rect.GetCenter();
            var length = ArrowHeadLength;
            var halfWidth = ArrowHeadWidth / 2;

            Point arrowTip;
            Point arrowPt1;
            Point arrowPt2;
            switch (ArrowDirection) {
                case Direction.Left:
                    arrowTip = new Point(rect.Left - length, center.Y);
                    arrowPt1 = new Point(arrowTip.X + length, arrowTip.Y - halfWidth);
                    arrowPt2 = new Point(arrowTip.X + length, arrowTip.Y + halfWidth);
                    break;
                case Direction.Up:
                    arrowTip = new Point(center.X, rect.Top - length);
                    arrowPt1 = new Point(arrowTip.X - halfWidth, arrowTip.Y + length);
                    arrowPt2 = new Point(arrowTip.X + halfWidth, arrowTip.Y + length);
                    break;
                case Direction.Right:
                    arrowTip = new Point(rect.Right + length, center.Y);
                    arrowPt1 = new Point(arrowTip.X - length, arrowTip.Y - halfWidth);
                    arrowPt2 = new Point(arrowTip.X - length, arrowTip.Y + halfWidth);
                    break;
                case Direction.Down:
                    arrowTip = new Point(center.X, rect.Bottom + length);
                    arrowPt1 = new Point(arrowTip.X - halfWidth, arrowTip.Y - length);
                    arrowPt2 = new Point(arrowTip.X + halfWidth, arrowTip.Y - length);
                    break;
                default:
                    return;
            }

            ctx.BeginFigure(arrowTip, isFilled: true, isClosed: true);
            ctx.LineTo(arrowPt1, isStroked: false, isSmoothJoin: false);
            ctx.LineTo(arrowPt2, isStroked: false, isSmoothJoin: false);
        }

        private static Size CollapseThickness(Thickness th)
        {
            return new Size(th.Left + th.Right, th.Top + th.Bottom);
        }

        private static Rect DeflateRect(Rect rect, Thickness thickness)
        {
            return new Rect(
                rect.Left + thickness.Left,
                rect.Top + thickness.Top,
                Math.Max(0.0, rect.Width - thickness.Left - thickness.Right),
                Math.Max(0.0, rect.Height - thickness.Top - thickness.Bottom));
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct Radii
        {
            public readonly double LeftTop;
            public readonly double TopLeft;
            public readonly double TopRight;
            public readonly double RightTop;
            public readonly double RightBottom;
            public readonly double BottomRight;
            public readonly double BottomLeft;
            public readonly double LeftBottom;

            public Radii(CornerRadius radii, Thickness borders, bool outer)
            {
                double left = 0.5 * borders.Left;
                double top = 0.5 * borders.Top;
                double right = 0.5 * borders.Right;
                double bottom = 0.5 * borders.Bottom;

                if (outer) {
                    if (radii.TopLeft.IsZero()) {
                        LeftTop = TopLeft = 0.0;
                    } else {
                        LeftTop = radii.TopLeft + left;
                        TopLeft = radii.TopLeft + top;
                    }
                    if (radii.TopRight.IsZero()) {
                        TopRight = RightTop = 0.0;
                    } else {
                        TopRight = radii.TopRight + top;
                        RightTop = radii.TopRight + right;
                    }
                    if (radii.BottomRight.IsZero()) {
                        RightBottom = BottomRight = 0.0;
                    } else {
                        RightBottom = radii.BottomRight + right;
                        BottomRight = radii.BottomRight + bottom;
                    }
                    if (radii.BottomLeft.IsZero()) {
                        BottomLeft = LeftBottom = 0.0;
                    } else {
                        BottomLeft = radii.BottomLeft + bottom;
                        LeftBottom = radii.BottomLeft + left;
                    }
                } else {
                    LeftTop = Math.Max(0.0, radii.TopLeft - left);
                    TopLeft = Math.Max(0.0, radii.TopLeft - top);
                    TopRight = Math.Max(0.0, radii.TopRight - top);
                    RightTop = Math.Max(0.0, radii.TopRight - right);
                    RightBottom = Math.Max(0.0, radii.BottomRight - right);
                    BottomRight = Math.Max(0.0, radii.BottomRight - bottom);
                    BottomLeft = Math.Max(0.0, radii.BottomLeft - bottom);
                    LeftBottom = Math.Max(0.0, radii.BottomLeft - left);
                }
            }
        }
    }
}
