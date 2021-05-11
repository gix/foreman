namespace Foreman.Controls
{
    using System;
    using System.Diagnostics;
    using System.Windows;
    using System.Windows.Media;
    using System.Windows.Shapes;
    using Extensions;

    public enum ConnectorShapeDirection
    {
        Horizontal,
        Vertical,
        Upwards,
        Downwards,
        Leftwards,
        Rightwards
    }

    public abstract class ConnectorShape : Shape
    {
        /// <summary>
        ///   Cached pen based on <see cref="Thickness"/> to widen the line
        ///   geometry.
        /// </summary>
        private Pen wideningPen = new(Brushes.Black, 2.0);

        protected Pen WideningPen => wideningPen;

        public static readonly DependencyProperty DirectionProperty =
            DependencyProperty.Register(
                nameof(Direction),
                typeof(ConnectorShapeDirection),
                typeof(ConnectorShape),
                new FrameworkPropertyMetadata(
                    ConnectorShapeDirection.Horizontal,
                    FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty ArrowHeadVisibilityProperty =
            DependencyProperty.Register(
                nameof(ArrowHeadVisibility),
                typeof(Visibility),
                typeof(ConnectorShape),
                new FrameworkPropertyMetadata(
                    Visibility.Visible, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty ArrowHeadLengthProperty =
            DependencyProperty.Register(
                nameof(ArrowHeadLength),
                typeof(double),
                typeof(ConnectorShape),
                new FrameworkPropertyMetadata(7.0, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty ArrowHeadWidthProperty =
            DependencyProperty.Register(
                nameof(ArrowHeadWidth),
                typeof(double),
                typeof(ConnectorShape),
                new FrameworkPropertyMetadata(10.0, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty ThicknessProperty =
            DependencyProperty.Register(
                nameof(Thickness),
                typeof(double),
                typeof(ConnectorShape),
                new FrameworkPropertyMetadata(
                    2.0, FrameworkPropertyMetadataOptions.AffectsRender, OnThicknessChanged));

        private static void OnThicknessChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var source = (ConnectorShape)d;
            source.wideningPen = new Pen(Brushes.Black, (double)e.NewValue);
        }

        public static readonly DependencyProperty PointsProperty =
            DependencyProperty.Register(
                nameof(Points),
                typeof(PointCollection),
                typeof(ConnectorShape),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

        /// <summary>Gets or sets the connector direction.</summary>
        public ConnectorShapeDirection Direction
        {
            get => (ConnectorShapeDirection)GetValue(DirectionProperty);
            set => SetValue(DirectionProperty, value);
        }

        /// <summary>Gets or sets the visibility of the arrow head.</summary>
        public Visibility ArrowHeadVisibility
        {
            get => (Visibility)GetValue(ArrowHeadVisibilityProperty);
            set => SetValue(ArrowHeadVisibilityProperty, value);
        }

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

        /// <summary>Gets or sets the thickness of the connector line.</summary>
        public double Thickness
        {
            get => (double)GetValue(ThicknessProperty);
            set => SetValue(ThicknessProperty, value);
        }

        /// <summary>
        ///   Gets or sets the intermediate points that make up the line between
        ///   the start and the end.
        /// </summary>
        public PointCollection Points
        {
            get => (PointCollection)GetValue(PointsProperty);
            set => SetValue(PointsProperty, value);
        }

        protected override Geometry DefiningGeometry
        {
            get
            {
                if (Points == null || Points.Count < 2)
                    return new GeometryGroup();

                var lineGeometry = GenerateLineGeometry();
                if (ArrowHeadVisibility != Visibility.Visible)
                    return lineGeometry;

                var arrowHeadGeometry = GenerateArrowHeadGeometry();
                return new CombinedGeometry(lineGeometry, arrowHeadGeometry);
            }
        }

        protected abstract Geometry GenerateLineGeometry();
        protected abstract Geometry GenerateArrowHeadGeometry();

        protected Geometry GenerateArrowHeadGeometry(Point midPoint, Vector direction)
        {
            var tip = midPoint + direction * ArrowHeadLength / 2;
            var basePoint = tip - direction * ArrowHeadLength;
            var crossDir = new Vector(-direction.Y, direction.X);

            var pt2 = basePoint - crossDir * (ArrowHeadWidth / 2);
            var pt3 = basePoint + crossDir * (ArrowHeadWidth / 2);

            var arrowHead = new PathFigure {
                IsClosed = true,
                IsFilled = true,
                StartPoint = tip,
                Segments = {
                    new LineSegment(pt2, true),
                    new LineSegment(pt3, true)
                }
            };

            var geometry = new PathGeometry();
            geometry.Figures.Add(arrowHead);

            return geometry;
        }
    }

    public class AngledConnectorShape : ConnectorShape
    {
        /// <summary>Generate the shapes geometry.</summary>
        protected override Geometry GenerateLineGeometry()
        {
            Debug.Assert(Points.Count >= 2);

            var geometry = new PathGeometry();

            Point startPoint, endPoint, midPoint;
            if (Points.Count == 2) {
                startPoint = Points[0];
                endPoint = Points[1];
                midPoint = PointUtils.MidPoint(startPoint, endPoint);
            } else {
                startPoint = Points[0];
                midPoint = Points[1];
                endPoint = Points[2];
            }

            var cornerPoint1 = new Point(midPoint.X, startPoint.Y);
            var cornerPoint2 = new Point(midPoint.X, endPoint.Y);

            var fig = new PathFigure {
                IsClosed = false,
                IsFilled = false,
                StartPoint = startPoint
            };
            fig.Segments.Add(new LineSegment(cornerPoint1, true));
            fig.Segments.Add(new LineSegment(cornerPoint2, true));
            fig.Segments.Add(new LineSegment(endPoint, true));
            geometry.Figures.Add(fig);

            return geometry.GetWidenedPathGeometry(WideningPen);
        }

        protected override Geometry GenerateArrowHeadGeometry()
        {
            Point startPoint, endPoint, midPoint;
            if (Points.Count == 2) {
                startPoint = Points[0];
                endPoint = Points[1];
                midPoint = PointUtils.MidPoint(startPoint, endPoint);
            } else {
                startPoint = Points[0];
                midPoint = Points[1];
                endPoint = Points[2];
            }

            var horizontal = Math.Abs(endPoint.Y - startPoint.Y) <= ArrowHeadWidth / 2;

            var direction = endPoint - startPoint;
            if (horizontal)
                direction.Y = 0;
            else
                direction.X = 0;
            direction.Normalize();

            return GenerateArrowHeadGeometry(midPoint, direction);
        }
    }

    public class StraightConnectorShape : ConnectorShape
    {
        protected override Geometry GenerateLineGeometry()
        {
            Point startPoint = Points[0];
            Point endPoint = Points[Points.Count - 1];

            var geometry = new LineGeometry(startPoint, endPoint);
            return geometry.GetWidenedPathGeometry(WideningPen);
        }

        protected override Geometry GenerateArrowHeadGeometry()
        {
            Point startPoint = Points[0];
            Point endPoint = Points[Points.Count - 1];
            Point midPoint = PointUtils.MidPoint(startPoint, endPoint);

            var direction = endPoint - startPoint;
            direction.Normalize();

            return GenerateArrowHeadGeometry(midPoint, direction);
        }
    }

    public class CurvedConnectorShape : ConnectorShape
    {
        protected override Geometry GenerateLineGeometry()
        {
            var geometry = new PathGeometry();

            GetControlPoints(out Point p0, out Point p1, out Point p2, out Point p3);

            var fig = new PathFigure {
                IsClosed = false,
                IsFilled = false,
                StartPoint = p0
            };
            fig.Segments.Add(new BezierSegment(p1, p2, p3, true));
            geometry.Figures.Add(fig);

            return geometry.GetWidenedPathGeometry(WideningPen);
        }

        protected override Geometry GenerateArrowHeadGeometry()
        {
            GetControlPoints(out Point p0, out Point p1, out Point p2, out Point p3);

            // Compute the mid point of the bezier segment.
            //
            // P(t)   = (1-t)^3 * P0 + 3t(1-t)^2 * P1 + 3t^2 (1-t) * P2 + t^3 * P3
            // P(0.5) = 0.125 * P0 + 0.375 * P1 + 0.375 * P2 + 0.125 * P3
            var midPointX = 0.125 * p0.X + 0.375 * p1.X + 0.375 * p2.X + 0.125 * p3.X;
            var midPointY = 0.125 * p0.Y + 0.375 * p1.Y + 0.375 * p2.Y + 0.125 * p3.Y;
            var midPoint = new Point(midPointX, midPointY);

            // Compute the gradient at the mid point.
            //
            // P'(t)   = -3(1-t)^2 * P0 + 3(1-t)^2 * P1 - 6t(1-t) * P1 - 3t^2 * P2 + 6t(1-t) * P2 + 3t^2 * P3
            // P'(0.5) = -0.75*P0 - 0.75*P1 + 0.75*P2 + 0.75*P3
            var gradientX = 0.75 * (-p0.X - p1.X + p2.X + p3.X);
            var gradientY = 0.75 * (-p0.Y - p1.Y + p2.Y + p3.Y);

            var direction = new Vector(gradientX, gradientY);
            direction.Normalize();

            return GenerateArrowHeadGeometry(midPoint, direction);
        }

        private void GetControlPoints(
            out Point p0, out Point p1, out Point p2, out Point p3)
        {
            double length = Math.Abs((Points[1] - Points[0]).Length);
            double handleOffset = Math.Min(50, length / 2);

            if (Points.Count == 2) {
                Point startPoint = Points[0];
                Point endPoint = Points[1];

                p0 = startPoint;
                if (Direction == ConnectorShapeDirection.Horizontal) {
                    double midPointX = startPoint.X + (endPoint.X - startPoint.X) / 2;
                    p1 = new Point(midPointX, startPoint.Y);
                    p2 = new Point(midPointX, endPoint.Y);
                } else if (Direction == ConnectorShapeDirection.Leftwards) {
                    double midPointX = startPoint.X + (endPoint.X - startPoint.X) / 2;
                    p1 = new Point(Math.Min(startPoint.Y - handleOffset, midPointX), startPoint.Y);
                    p2 = new Point(Math.Max(endPoint.Y + handleOffset, midPointX), endPoint.Y);
                } else if (Direction == ConnectorShapeDirection.Rightwards) {
                    double midPointX = startPoint.X + (endPoint.X - startPoint.X) / 2;
                    p1 = new Point(Math.Max(startPoint.Y + handleOffset, midPointX), startPoint.Y);
                    p2 = new Point(Math.Min(endPoint.Y - handleOffset, midPointX), endPoint.Y);
                } else if (Direction == ConnectorShapeDirection.Vertical) {
                    double midPointY = startPoint.Y + (endPoint.Y - startPoint.Y) / 2;
                    p1 = new Point(startPoint.X, midPointY);
                    p2 = new Point(endPoint.X, midPointY);
                } else if (Direction == ConnectorShapeDirection.Upwards) {
                    double midPointY = startPoint.Y + (endPoint.Y - startPoint.Y) / 2;
                    p1 = new Point(startPoint.X, Math.Min(startPoint.Y - handleOffset, midPointY));
                    p2 = new Point(endPoint.X, Math.Max(endPoint.Y + handleOffset, midPointY));
                } else if (Direction == ConnectorShapeDirection.Downwards) {
                    double midPointY = startPoint.Y + (endPoint.Y - startPoint.Y) / 2;
                    p1 = new Point(startPoint.X, Math.Max(startPoint.Y + handleOffset, midPointY));
                    p2 = new Point(endPoint.X, Math.Min(endPoint.Y - handleOffset, midPointY));
                } else {
                    throw new NotImplementedException();
                }
                p3 = endPoint;
            } else if (Points.Count == 3) {
                Point startPoint = Points[0];
                Point endPoint = Points[1];
                Point midPoint = Points[2];

                p0 = startPoint;
                if (Direction == ConnectorShapeDirection.Horizontal) {
                    p1 = new Point(midPoint.X, startPoint.Y);
                    p2 = new Point(midPoint.X, endPoint.Y);
                } else if (Direction == ConnectorShapeDirection.Leftwards) {
                    p1 = new Point(Math.Min(startPoint.Y - handleOffset, midPoint.X), startPoint.Y);
                    p2 = new Point(Math.Max(endPoint.Y + handleOffset, midPoint.X), endPoint.Y);
                } else if (Direction == ConnectorShapeDirection.Rightwards) {
                    p1 = new Point(Math.Max(startPoint.Y + handleOffset, midPoint.X), startPoint.Y);
                    p2 = new Point(Math.Min(endPoint.Y - handleOffset, midPoint.X), endPoint.Y);
                } else if (Direction == ConnectorShapeDirection.Vertical) {
                    p1 = new Point(startPoint.X, midPoint.Y);
                    p2 = new Point(endPoint.X, midPoint.Y);
                } else if (Direction == ConnectorShapeDirection.Upwards) {
                    p1 = new Point(startPoint.X, Math.Min(startPoint.Y - handleOffset, midPoint.Y));
                    p2 = new Point(endPoint.X, Math.Max(endPoint.Y + handleOffset, midPoint.Y));
                } else if (Direction == ConnectorShapeDirection.Downwards) {
                    p1 = new Point(startPoint.X, Math.Max(startPoint.Y + handleOffset, midPoint.Y));
                    p2 = new Point(endPoint.X, Math.Min(endPoint.Y - handleOffset, midPoint.Y));
                } else {
                    throw new NotImplementedException();
                }
                p3 = endPoint;
            } else if (Points.Count == 4) {
                p0 = Points[0];
                p1 = Points[1];
                p2 = Points[2];
                p3 = Points[3];
            } else {
                throw new InvalidOperationException("Invalid number of points.");
            }
        }
    }
}
