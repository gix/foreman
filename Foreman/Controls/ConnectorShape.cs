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
        protected Pen WideningPen { get; private set; } = new(Brushes.Black, 2.0);

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
            source.WideningPen = new Pen(Brushes.Black, (double)e.NewValue);
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
        public PointCollection? Points
        {
            get => (PointCollection?)GetValue(PointsProperty);
            set => SetValue(PointsProperty, value);
        }

        protected override Geometry DefiningGeometry
        {
            get
            {
                if (Points == null || Points.Count < 2)
                    return new GeometryGroup();

                Geometry lineGeometry = GenerateLineGeometry(Points);
                if (ArrowHeadVisibility != Visibility.Visible)
                    return lineGeometry;

                Geometry arrowHeadGeometry = GenerateArrowHeadGeometry(Points);
                return new CombinedGeometry(lineGeometry, arrowHeadGeometry);
            }
        }

        protected abstract Geometry GenerateLineGeometry(PointCollection points);
        protected abstract Geometry GenerateArrowHeadGeometry(PointCollection points);

        protected Geometry GenerateArrowHeadGeometry(Point midPoint, Vector direction)
        {
            Point tip = midPoint + (direction * ArrowHeadLength / 2);
            Point basePoint = tip - (direction * ArrowHeadLength);
            var crossDir = new Vector(-direction.Y, direction.X);

            Point pt2 = basePoint - (crossDir * (ArrowHeadWidth / 2));
            Point pt3 = basePoint + (crossDir * (ArrowHeadWidth / 2));

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
        protected override Geometry GenerateLineGeometry(PointCollection points)
        {
            Debug.Assert(points.Count >= 2);

            var geometry = new PathGeometry();

            Point startPoint, endPoint, midPoint;
            if (points.Count == 2) {
                startPoint = points[0];
                endPoint = points[1];
                midPoint = PointUtils.MidPoint(startPoint, endPoint);
            } else {
                startPoint = points[0];
                midPoint = points[1];
                endPoint = points[2];
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

        protected override Geometry GenerateArrowHeadGeometry(PointCollection points)
        {
            Point startPoint, endPoint, midPoint;
            if (points.Count == 2) {
                startPoint = points[0];
                endPoint = points[1];
                midPoint = PointUtils.MidPoint(startPoint, endPoint);
            } else {
                startPoint = points[0];
                midPoint = points[1];
                endPoint = points[2];
            }

            bool horizontal = Math.Abs(endPoint.Y - startPoint.Y) <= ArrowHeadWidth / 2;

            Vector direction = endPoint - startPoint;
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
        protected override Geometry GenerateLineGeometry(PointCollection points)
        {
            Point startPoint = points[0];
            Point endPoint = points[^1];

            var geometry = new LineGeometry(startPoint, endPoint);
            return geometry.GetWidenedPathGeometry(WideningPen);
        }

        protected override Geometry GenerateArrowHeadGeometry(PointCollection points)
        {
            Point startPoint = points[0];
            Point endPoint = points[^1];
            Point midPoint = PointUtils.MidPoint(startPoint, endPoint);

            Vector direction = endPoint - startPoint;
            direction.Normalize();

            return GenerateArrowHeadGeometry(midPoint, direction);
        }
    }

    public class CurvedConnectorShape : ConnectorShape
    {
        protected override Geometry GenerateLineGeometry(PointCollection points)
        {
            var geometry = new PathGeometry();

            GetControlPoints(points, out Point p0, out Point p1, out Point p2, out Point p3);

            var fig = new PathFigure {
                IsClosed = false,
                IsFilled = false,
                StartPoint = p0
            };
            fig.Segments.Add(new BezierSegment(p1, p2, p3, true));
            geometry.Figures.Add(fig);

            return geometry.GetWidenedPathGeometry(WideningPen);
        }

        protected override Geometry GenerateArrowHeadGeometry(PointCollection points)
        {
            GetControlPoints(points, out Point p0, out Point p1, out Point p2, out Point p3);

            // Compute the mid point of the bezier segment.
            //
            // P(t)   = (1-t)^3 * P0 + 3t(1-t)^2 * P1 + 3t^2 (1-t) * P2 + t^3 * P3
            // P(0.5) = 0.125 * P0 + 0.375 * P1 + 0.375 * P2 + 0.125 * P3
            var midPointX = (0.125 * p0.X) + (0.375 * p1.X) + (0.375 * p2.X) + (0.125 * p3.X);
            var midPointY = (0.125 * p0.Y) + (0.375 * p1.Y) + (0.375 * p2.Y) + (0.125 * p3.Y);
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
            PointCollection points, out Point p0, out Point p1, out Point p2, out Point p3)
        {
            double length = Math.Abs((points[1] - points[0]).Length);
            double handleOffset = Math.Min(50, length / 2);

            if (points.Count == 2) {
                Point startPoint = points[0];
                Point endPoint = points[1];

                p0 = startPoint;
                switch (Direction) {
                    case ConnectorShapeDirection.Horizontal: {
                        double midPointX = startPoint.X + ((endPoint.X - startPoint.X) / 2);
                        p1 = new Point(midPointX, startPoint.Y);
                        p2 = new Point(midPointX, endPoint.Y);
                        break;
                    }
                    case ConnectorShapeDirection.Leftwards: {
                        double midPointX = startPoint.X + ((endPoint.X - startPoint.X) / 2);
                        p1 = new Point(Math.Min(startPoint.Y - handleOffset, midPointX), startPoint.Y);
                        p2 = new Point(Math.Max(endPoint.Y + handleOffset, midPointX), endPoint.Y);
                        break;
                    }
                    case ConnectorShapeDirection.Rightwards: {
                        double midPointX = startPoint.X + ((endPoint.X - startPoint.X) / 2);
                        p1 = new Point(Math.Max(startPoint.Y + handleOffset, midPointX), startPoint.Y);
                        p2 = new Point(Math.Min(endPoint.Y - handleOffset, midPointX), endPoint.Y);
                        break;
                    }
                    case ConnectorShapeDirection.Vertical: {
                        double midPointY = startPoint.Y + ((endPoint.Y - startPoint.Y) / 2);
                        p1 = new Point(startPoint.X, midPointY);
                        p2 = new Point(endPoint.X, midPointY);
                        break;
                    }
                    case ConnectorShapeDirection.Upwards: {
                        double midPointY = startPoint.Y + ((endPoint.Y - startPoint.Y) / 2);
                        p1 = new Point(startPoint.X, Math.Min(startPoint.Y - handleOffset, midPointY));
                        p2 = new Point(endPoint.X, Math.Max(endPoint.Y + handleOffset, midPointY));
                        break;
                    }
                    case ConnectorShapeDirection.Downwards: {
                        double midPointY = startPoint.Y + ((endPoint.Y - startPoint.Y) / 2);
                        p1 = new Point(startPoint.X, Math.Max(startPoint.Y + handleOffset, midPointY));
                        p2 = new Point(endPoint.X, Math.Min(endPoint.Y - handleOffset, midPointY));
                        break;
                    }
                    default:
                        throw new ArgumentOutOfRangeException(
                            nameof(Direction), Direction, "Unknown connector direction");
                }
                p3 = endPoint;
            } else if (points.Count == 3) {
                Point startPoint = points[0];
                Point endPoint = points[1];
                Point midPoint = points[2];

                p0 = startPoint;
                switch (Direction) {
                    case ConnectorShapeDirection.Horizontal:
                        p1 = new Point(midPoint.X, startPoint.Y);
                        p2 = new Point(midPoint.X, endPoint.Y);
                        break;
                    case ConnectorShapeDirection.Leftwards:
                        p1 = new Point(Math.Min(startPoint.Y - handleOffset, midPoint.X), startPoint.Y);
                        p2 = new Point(Math.Max(endPoint.Y + handleOffset, midPoint.X), endPoint.Y);
                        break;
                    case ConnectorShapeDirection.Rightwards:
                        p1 = new Point(Math.Max(startPoint.Y + handleOffset, midPoint.X), startPoint.Y);
                        p2 = new Point(Math.Min(endPoint.Y - handleOffset, midPoint.X), endPoint.Y);
                        break;
                    case ConnectorShapeDirection.Vertical:
                        p1 = new Point(startPoint.X, midPoint.Y);
                        p2 = new Point(endPoint.X, midPoint.Y);
                        break;
                    case ConnectorShapeDirection.Upwards:
                        p1 = new Point(startPoint.X, Math.Min(startPoint.Y - handleOffset, midPoint.Y));
                        p2 = new Point(endPoint.X, Math.Max(endPoint.Y + handleOffset, midPoint.Y));
                        break;
                    case ConnectorShapeDirection.Downwards:
                        p1 = new Point(startPoint.X, Math.Max(startPoint.Y + handleOffset, midPoint.Y));
                        p2 = new Point(endPoint.X, Math.Min(endPoint.Y - handleOffset, midPoint.Y));
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(
                            nameof(Direction), Direction, "Unknown connector direction");
                }
                p3 = endPoint;
            } else if (points.Count == 4) {
                p0 = points[0];
                p1 = points[1];
                p2 = points[2];
                p3 = points[3];
            } else {
                throw new InvalidOperationException("Invalid number of points.");
            }
        }
    }
}
