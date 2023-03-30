using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Sim80C51.Toolbox.Wpf
{
    /// <summary>
    /// A seven segments control
    /// Based on source from https://www.codeproject.com/Articles/1277331/Seven-Segment-and-Sixteen-Segment-Controls-for-WPF
    /// </summary>
    public class SevenSegments : UserControl
    {
        #region Dependency Properties

        protected event PropertyChangedCallback PropertyChanged = (sender, e) => { };

        public static readonly DependencyProperty PenColorProperty = DependencyProperty.Register("PenColor", typeof(Color), typeof(SevenSegments), new PropertyMetadata(Color.FromRgb(234, 234, 234), VisualChanged));
        public static readonly DependencyProperty SelectedPenColorProperty = DependencyProperty.Register("SelectedPenColor", typeof(Color), typeof(SevenSegments), new PropertyMetadata(Colors.Black, VisualChanged));
        public static readonly DependencyProperty FillBrushProperty = DependencyProperty.Register("FillBrush", typeof(Brush), typeof(SevenSegments), new PropertyMetadata(new SolidColorBrush(Color.FromRgb(248, 248, 248)), VisualChanged));
        public static readonly DependencyProperty PenThicknessProperty = DependencyProperty.Register("PenThickness", typeof(double), typeof(SevenSegments), new PropertyMetadata(1.0, VisualChanged));
        public static readonly DependencyProperty SelectedFillBrushProperty = DependencyProperty.Register("SelectedFillBrush", typeof(Brush), typeof(SevenSegments), new PropertyMetadata(new SolidColorBrush(Colors.Green), VisualChanged));
        public static readonly DependencyProperty ValueProperty = DependencyProperty.Register("Value", typeof(byte), typeof(SevenSegments), new PropertyMetadata((byte)0, VisualChanged));
        public static readonly DependencyProperty GapWidthProperty = DependencyProperty.Register("GapWidth", typeof(double), typeof(SevenSegments), new PropertyMetadata(3.0, VisualChanged));
        public static readonly DependencyProperty ShowDotProperty = DependencyProperty.Register("ShowDot", typeof(bool), typeof(SevenSegments), new PropertyMetadata(false, VisualChanged));
        public static readonly DependencyProperty TiltAngleProperty = DependencyProperty.Register("TiltAngle", typeof(double), typeof(SevenSegments), new PropertyMetadata(10.0, VisualChanged));
        public static readonly DependencyProperty RoundedCornersProperty = DependencyProperty.Register("RoundedCorners", typeof(bool), typeof(SevenSegments), new PropertyMetadata(false, VisualChanged));
        public static readonly DependencyProperty SelectedSegmentsProperty = DependencyProperty.Register("SelectedSegments", typeof(List<int>), typeof(SevenSegments), new PropertyMetadata(new List<int>(), VisualChanged));
        public static readonly DependencyProperty SegmentsBrushProperty = DependencyProperty.Register("SegmentsBrush", typeof(List<Tuple<int, Brush, Color>>), typeof(SevenSegments), new PropertyMetadata(new List<Tuple<int, Brush, Color>>(), VisualChanged));
        public static readonly DependencyProperty VertSegDividerProperty = DependencyProperty.Register("VertSegDivider", typeof(double), typeof(SevenSegments), new PropertyMetadata(5.0, VisualChanged));
        public static readonly DependencyProperty HorizSegDividerProperty = DependencyProperty.Register("HorizSegDivider", typeof(double), typeof(SevenSegments), new PropertyMetadata(9.0, VisualChanged));

        /// <summary>
        /// A list of selected segments set by user
        /// </summary>
        public List<int> SelectedSegments
        {
            get { return (List<int>)GetValue(SelectedSegmentsProperty); }
            set { SetValue(SelectedSegmentsProperty, value); }
        }

        /// <summary>
        /// A list of segments numbers, fill brushes and pen colors
        /// </summary>
        public List<Tuple<int, Brush, Color>> SegmentsBrush
        {
            get { return (List<Tuple<int, Brush, Color>>)GetValue(SegmentsBrushProperty); }
            set { SetValue(SegmentsBrushProperty, value); }
        }

        /// <summary>
        /// A brush for not selected elements
        /// </summary>
        public Brush FillBrush
        {
            get { return (Brush)GetValue(FillBrushProperty); }
            set { SetValue(FillBrushProperty, value); }
        }

        /// <summary>
        /// A brush for selected elements
        /// </summary>
        public Brush SelectedFillBrush
        {
            get { return (Brush)GetValue(SelectedFillBrushProperty); }
            set { SetValue(SelectedFillBrushProperty, value); }
        }

        /// <summary>
        /// A pen color for not selected elements
        /// </summary>
        public Color PenColor
        {
            get { return (Color)GetValue(PenColorProperty); }
            set { SetValue(PenColorProperty, value); }
        }

        /// <summary>
        /// A pen color for selected elements
        /// </summary>
        public Color SelectedPenColor
        {
            get { return (Color)GetValue(SelectedPenColorProperty); }
            set { SetValue(SelectedPenColorProperty, value); }
        }

        /// <summary>
        /// A pen thickness of elements
        /// </summary>
        public double PenThickness
        {
            get { return (double)GetValue(PenThicknessProperty); }
            set { SetValue(PenThicknessProperty, value); }
        }

        /// <summary>
        /// A value for segments control
        /// </summary>
        public byte Value
        {
            get { return (byte)GetValue(ValueProperty); }
            set { SetValue(ValueProperty, value); }
        }

        /// <summary>
        /// A gap between segments (in pixels)
        /// </summary>
        public double GapWidth
        {
            get { return (double)GetValue(GapWidthProperty); }
            set { SetValue(GapWidthProperty, value); }
        }


        /// <summary>
        /// Checks whether or not the corners are rounded 
        /// </summary>
        public bool RoundedCorners
        {
            get { return (bool)GetValue(RoundedCornersProperty); }
            set { SetValue(RoundedCornersProperty, value); }
        }


        /// <summary>
        /// A tilt angle (in degrees)
        /// </summary>
        public double TiltAngle
        {
            get { return (double)GetValue(TiltAngleProperty); }
            set { SetValue(TiltAngleProperty, value); }
        }


        /// <summary>
        /// Shows/Hides dot for segments control
        /// </summary>
        public bool ShowDot
        {
            get { return (bool)GetValue(ShowDotProperty); }
            set { SetValue(ShowDotProperty, value); }
        }

        /// <summary>
        /// A divider for vert. segments width
        /// </summary>
        public double VertSegDivider
        {
            get { return (double)GetValue(VertSegDividerProperty); }
            set { SetValue(VertSegDividerProperty, value); }
        }


        /// <summary>
        /// A divider for horiz. segments height
        /// </summary>
        public double HorizSegDivider
        {
            get { return (double)GetValue(HorizSegDividerProperty); }
            set { SetValue(HorizSegDividerProperty, value); }
        }


        private static void VisualChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            SevenSegments segments = (SevenSegments)sender;
            segments.PropertyChanged(sender, e);
        }
        #endregion

        #region Protected variables

        protected bool isPropertyCahnged = true;
        protected double startPointThickness;

        protected double vertRoundCoef = 0;
        protected double horizRoundCoef = 0;

        /// <summary>
        /// The width of the vert. segm
        /// </summary>
        protected double VertSegW { get; private set; }

        /// <summary>
        /// The width of the vert. segment's part
        /// </summary>
        protected double VertSegPartW { get; private set; }

        /// <summary>
        /// The height of the vert. segment's part
        /// </summary>
        protected double VertSegSmallPartH { get; private set; }

        /// <summary>
        /// The height of the horiz. segment
        /// </summary>
        protected double HorizSegH { get; private set; }

        /// <summary>
        /// The height of the horiz. segment's part
        /// </summary>
        protected double HorizSegSmallPartH { get; private set; }

        /// <summary>
        /// The width of the horiz. segment's part
        /// </summary>
        protected double HorizSegSmallPartW { get; private set; }

        /// <summary>
        /// The horizontal midlle point
        /// </summary>
        protected double MidPoint { get; private set; }

        /// <summary>
        /// The gap between segments
        /// </summary>
        protected double GapW { get; private set; }


        /// <summary>
        /// The diameter of the dot
        /// </summary>
        protected double DotDiameter { get; private set; }

        /// <summary>
        /// The diameter of the colon
        /// </summary>
        protected double ColonDiameter { get; private set; }

        /// <summary>
        /// The height depending on the decimal dot
        /// </summary>
        protected double VirtualWidth { get; private set; }

        /// <summary>
        /// The width depending on the decimal dot
        /// </summary>
        protected double VirtualHeight { get; private set; }


        /// <summary>
        /// The list of geometries to detect selected segments
        /// </summary>
        protected List<GeometryWithSegm> GeometryFigures;

        /// <summary>
        /// The width of the vert. segment's bottom part
        /// </summary>
        protected double VertSegBotPartW { get; private set; }

        /// <summary>
        /// Points collection for the left bottom segment
        /// </summary>
        protected PointCollection LeftBottomSegmPoints { get; private set; }

        /// <summary>
        /// Points collection for the left top segment
        /// </summary>
        protected PointCollection LeftTopSegmPoints { get; private set; }

        /// <summary>
        /// Points collection for the top segment
        /// </summary>
        protected PointCollection TopSegmPoints { get; set; }

        /// <summary>
        /// Points collection for the bottom segment
        /// </summary>
        protected PointCollection BottomSegmPoints { get; set; }

        /// <summary>
        /// Points collection for the middle segment
        /// </summary>
        protected PointCollection MiddleSegmPoints { get; set; }

        /// <summary>
        /// Points collection for the right top segment
        /// </summary>
        protected PointCollection RightTopSegmPoints { get; private set; }

        /// <summary>
        /// Points collection for the right bottom segment
        /// </summary>
        protected PointCollection RightBottomSegmPoints { get; private set; }

        protected double figureStartPointY;

        #endregion

        #region Constructor

        public SevenSegments()
        {
            PropertyChanged += OnPropertyChanged;
            vertRoundCoef = 5.5;
            horizRoundCoef = 15;
        }

        private void OnPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            SevenSegments segments = (SevenSegments)sender;
            isPropertyCahnged = true;

            segments.InvalidateVisual();
        }

        #endregion

        #region Drawing

        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);
            CalculateMeasures();

            AssignSegments();
            ClearSegmentsSelection();
            SetSegments();

            // Draws segments
            foreach (GeometryWithSegm entry in GeometryFigures)
            {

                if (SegmentsBrush.Any())
                {
                    Tuple<int, Brush, Color>? brush = SegmentsBrush.SingleOrDefault(s => s.Item1 == entry.SegmentNumber);
                    Pen figurePen = new(new SolidColorBrush(brush != null ? brush.Item3 : PenColor), PenThickness);
                    drawingContext.DrawGeometry(brush != null ? brush.Item2 : FillBrush, figurePen, entry.Geometry);

                }
                else
                {
                    Pen figurePen = new(new SolidColorBrush(entry.IsSelected ? SelectedPenColor : PenColor), PenThickness);
                    drawingContext.DrawGeometry(entry.IsSelected ? SelectedFillBrush : FillBrush, figurePen, entry.Geometry);
                }
            }

            // Draws decimal dot
            DrawDot(drawingContext);
        }

        /// <summary>
        /// Clear selected segments and value
        /// </summary>
        public void ClearSegments()
        {
            Value = 0;
            SelectedSegments = new List<int>();
            SegmentsBrush = new List<Tuple<int, Brush, Color>>();
        }

        /// <summary>
        /// Assigns a segment number to required path geometry. Order is important!
        /// </summary>
        protected virtual void AssignSegments()
        {
            GeometryFigures = new List<GeometryWithSegm>
            {
                new GeometryWithSegm(LeftBottomSegement(), 4),
                new GeometryWithSegm(LeftTopSegement(), 5),
                new GeometryWithSegm(RightTopSegement(), 1),
                new GeometryWithSegm(RightBottomSegement(), 2),
                new GeometryWithSegm(MiddleSegement(), 6),
                new GeometryWithSegm(TopSegement(), 0),
                new GeometryWithSegm(BottomSegement(), 3),
            };
        }

        /// <summary>
        /// Selects required segments
        /// </summary>
        protected void SetSegments()
        {
            if (SelectedSegments.Any())
            {
                for (int i = 0; i < SelectedSegments.Count; i++)
                {
                    GeometryFigures.Single(t => t.SegmentNumber == SelectedSegments[i]).IsSelected = true;
                }
            }
            else
            {
                ValueSegmentsSelection();
            }
        }


        /// <summary>
        /// Calculates required points and measures
        /// </summary>
        private void CalculateMeasures()
        {
            //Horiz. figure
            HorizSegH = ActualHeight / HorizSegDivider;
            HorizSegSmallPartH = HorizSegH / 4;

            //Vert. figure
            VertSegW = ActualWidth / VertSegDivider;
            VertSegPartW = VertSegW / 3.5;
            VertSegSmallPartH = VertSegW / 3.5;
            VertSegBotPartW = VertSegW / 2;

            HorizSegSmallPartW = VertSegW / 4;

            //The points calculation
            MidPoint = ActualHeight / 2;
            GapW = GapWidth;

            DotDiameter = HorizSegH;
            ColonDiameter = HorizSegH;

            VirtualHeight = ShowDot ? ActualHeight - DotDiameter / 1.5 : ActualHeight;
            VirtualWidth = ShowDot ? ActualWidth - DotDiameter / 1.5 : ActualWidth;

            figureStartPointY = VirtualHeight - (HorizSegSmallPartH + GapW + VertSegSmallPartH);
            startPointThickness = PenThickness / 2;
        }

        /// <summary>
        /// Selects segments depending on the value 
        /// </summary>
        protected virtual void ValueSegmentsSelection()
        {
            List<int> segments = new();
            for (int i = 0; i < 7; i++)
            {
                if ((Value & 1 << i) != 0)
                {
                    segments.Add(i);
                }
            }

            if (segments.Count > 0)
            {
                SelectSegments(segments.ToArray());
            }
        }


        /// <summary>
        /// Draws decimal dot separator
        /// </summary>
        protected void DrawDot(DrawingContext drawingContext)
        {
            if (ShowDot)
            {
                bool dotOn = (Value & 0x80) != 0;
                Pen dotPen = new(new SolidColorBrush(dotOn ? SelectedPenColor : PenColor), PenThickness);
                Point centerPoint = new(ActualWidth - DotDiameter / 2, ActualHeight - DotDiameter / 2);
                PathGeometry pathGeometry = CreateEllipseGeometry(centerPoint, DotDiameter / 2);
                drawingContext.DrawGeometry(dotOn ? SelectedFillBrush : FillBrush, dotPen, pathGeometry);
            }
        }

        private PathGeometry CreateEllipseGeometry(Point centerPoint, double diameter)
        {
            EllipseGeometry ellipseGeometry = new()
            {
                Center = centerPoint,
                RadiusX = diameter,
                RadiusY = diameter
            };

            PathGeometry pathGeometry = PathGeometry.CreateFromGeometry(ellipseGeometry);
            pathGeometry.Transform = new SkewTransform(-TiltAngle, 0, centerPoint.X, centerPoint.Y); ;
            return pathGeometry;
        }


        /// <summary>
        /// Sets required geometry figures as selected
        /// </summary>
        protected void SelectSegments(params int[] segmNumbers)
        {
            for (int i = 0; i < segmNumbers.Length; i++)
            {
                GeometryFigures.Single(t => t.SegmentNumber == segmNumbers[i]).IsSelected = true;
            }

        }

        /// <summary>
        /// Clears selection for all geometry figures 
        /// </summary>
        protected void ClearSegmentsSelection()
        {
            GeometryFigures.ForEach(c => c.IsSelected = false);
        }


        /// <summary>
        /// Draws custom path geometry
        /// </summary>
        protected static PathGeometry SegmentPathGeometry(Point startPoint, PolyLineSegment polyLineSegment)
        {
            PathGeometry pathGeometry = new();
            PathFigure pathFigure = new()
            {
                StartPoint = startPoint,
                IsClosed = true
            };
            pathGeometry.Figures.Add(pathFigure);
            pathFigure.Segments.Add(polyLineSegment);
            return pathGeometry;
        }


        /// <summary>
        /// Returns X-coord by the angle and height
        /// </summary>
        /// <param name="y">Y-coordinate to calculate height</param>
        protected double XByAngle(double y)
        {
            double h = figureStartPointY - y;
            return TanAngle() * h;
        }

        /// <summary>
        /// Returns tangent of the tilt angle in degrees
        /// </summary>
        protected double TanAngle()
        {
            return Math.Tan(TiltAngle * (Math.PI / 180.0));
        }

        /// <summary>
        /// Returns gap shift for the top and bottom segments
        /// </summary>
        private double GapShift()
        {
            return GapW * 0.75;
        }


        #endregion

        #region Points' locations

        /// <summary>
        /// Calulates points  for the left top segment
        /// </summary>
        /// <returns></returns>
        protected PointCollection GetLeftTopSegmPoints()
        {

            double intermPoint = VirtualHeight / 2 - HorizSegH / 2;
            double startTopY = HorizSegSmallPartH + GapW + VertSegSmallPartH + startPointThickness;
            double x1 = XByAngle(startTopY);

            double yBezier = (VirtualHeight - startPointThickness) / vertRoundCoef;
            double xBezier = RoundedCorners ? XByAngle(yBezier) : 0;

            // the bezier point
            Point bezPoint = RoundedCorners
                ? new Point(xBezier + startPointThickness, yBezier)
                : new Point(x1 + startPointThickness, HorizSegSmallPartH + startPointThickness + GapW + VertSegSmallPartH);

            startTopY = HorizSegSmallPartH + GapShift();
            double x2 = XByAngle(startTopY);

            startTopY = HorizSegH + GapW / 2;
            double x3 = XByAngle(startTopY);

            startTopY = intermPoint - GapW / 2;
            double x4 = XByAngle(startTopY - startPointThickness);

            startTopY = VirtualHeight / 2 - GapW / 2;
            double x5 = XByAngle(startTopY - startPointThickness);

            return new()
            {
                // three top points, starting from the left point
                new Point(x1 + startPointThickness, HorizSegSmallPartH + GapW + VertSegSmallPartH + startPointThickness),
                new Point(x2 + VertSegPartW + startPointThickness, HorizSegSmallPartH + startPointThickness + GapShift()),
                new Point(x3 + VertSegW + startPointThickness, HorizSegH + startPointThickness + GapW / 2),

                // three bottom points, starting from the right point
                new Point(x4 + VertSegW + startPointThickness, intermPoint - GapW / 2),
                new Point(x5 + VertSegBotPartW + startPointThickness, VirtualHeight / 2 - GapW / 2),
                new Point(x5 + startPointThickness, VirtualHeight / 2 - GapW / 2),

                // the point for rounded Bezier curve
                bezPoint
            };
        }

        /// <summary>
        /// Calulates points for the left bottom segment
        /// </summary>
        /// <returns></returns>
        protected PointCollection GetLeftBottomSegmPoints()
        {
            double startBottomY = VirtualHeight / 2 + HorizSegH / 2 + GapW / 2;
            double startBottomY2 = VirtualHeight - (HorizSegH + GapW / 2) - startPointThickness;

            double x1 = XByAngle(VirtualHeight / 2 + GapW / 2);
            double x = XByAngle(startBottomY);
            double x2 = XByAngle(startBottomY2);

            double yBezier = VirtualHeight - startPointThickness - VirtualHeight / vertRoundCoef;
            double xBezier = RoundedCorners ? XByAngle(yBezier) : 0;

            // the bezier point
            Point bezPoint = RoundedCorners
                ? new Point(xBezier + startPointThickness, yBezier)
                : new Point(startPointThickness, figureStartPointY - startPointThickness);

            return new()
            {
                // three top points, starting from left top point
                new Point(x1 + startPointThickness, VirtualHeight / 2 + GapW / 2),
                new Point(x1 + VertSegBotPartW + startPointThickness, VirtualHeight / 2 + GapW / 2),
                new Point(x + VertSegW + startPointThickness, startBottomY),

                // three bottom points, starting from right
                new Point(x2 + VertSegW + startPointThickness, startBottomY2),
                new Point(VertSegPartW + startPointThickness, VirtualHeight - startPointThickness - (HorizSegSmallPartH + GapShift())),
                new Point(startPointThickness, figureStartPointY - startPointThickness),

                // the point for rounded Bezier curve
                bezPoint
            };
        }

        /// <summary>
        /// Calulates points for the right bottom segment
        /// </summary>
        /// <returns></returns>
        protected PointCollection GetRightBottomSegmPoints()
        {
            return new PointCollection
            {
                // three top points, starting from the left point
                new Point(VirtualWidth - LeftTopSegmPoints[3].X, VirtualHeight - LeftTopSegmPoints[3].Y),
                new Point(VirtualWidth - LeftTopSegmPoints[4].X, VirtualHeight - LeftTopSegmPoints[4].Y),
                new Point(VirtualWidth - LeftTopSegmPoints[5].X, VirtualHeight - LeftTopSegmPoints[5].Y),

                // the point for rounded Bezier curve
                new Point(VirtualWidth - LeftTopSegmPoints[6].X, VirtualHeight - LeftTopSegmPoints[6].Y),

                // three bottom points, starting from the right point
                new Point(VirtualWidth - LeftTopSegmPoints[0].X, VirtualHeight - LeftTopSegmPoints[0].Y),
                new Point(VirtualWidth - LeftTopSegmPoints[1].X, VirtualHeight - LeftTopSegmPoints[1].Y),
                new Point(VirtualWidth - LeftTopSegmPoints[2].X, VirtualHeight - LeftTopSegmPoints[2].Y)
            };
        }


        /// <summary>
        /// Calulates points  for the right top segment
        /// </summary>
        protected PointCollection GetRightTopSegmPoints()
        {
            return new()
            {
                // three top points, starting from the left point
                new Point(VirtualWidth - LeftBottomSegmPoints[3].X, VirtualHeight - LeftBottomSegmPoints[3].Y),
                new Point(VirtualWidth - LeftBottomSegmPoints[4].X, VirtualHeight - LeftBottomSegmPoints[4].Y),
                new Point(VirtualWidth - LeftBottomSegmPoints[5].X, VirtualHeight - LeftBottomSegmPoints[5].Y),

                // the point for rounded Bezier curve
                new Point(VirtualWidth - LeftBottomSegmPoints[6].X, VirtualHeight - LeftBottomSegmPoints[6].Y),

                // three bottom points, starting from the right point
                new Point(VirtualWidth - LeftBottomSegmPoints[0].X, VirtualHeight - LeftBottomSegmPoints[0].Y),
                new Point(VirtualWidth - LeftBottomSegmPoints[1].X, VirtualHeight - LeftBottomSegmPoints[1].Y),
                new Point(VirtualWidth - LeftBottomSegmPoints[2].X, VirtualHeight - LeftBottomSegmPoints[2].Y)
            };
        }

        /// <summary>
        /// Calculates points collection for the middle segment
        /// </summary>
        /// <returns></returns>
        protected PointCollection GetMiddleSegmPoints()
        {
            double x = XByAngle(VirtualHeight / 2 + HorizSegH / 2) + (VertSegW + GapW);
            double x1 = XByAngle(VirtualHeight / 2) + VertSegBotPartW + GapW;
            double x2 = XByAngle(VirtualHeight / 2 - HorizSegH / 2) + VertSegW + GapW;

            return new()
            {
                // three left points, starting from the bottom point
                new Point(x, VirtualHeight / 2 + HorizSegH / 2),
                new Point(x1, VirtualHeight / 2),
                new Point(x2, VirtualHeight / 2 - HorizSegH / 2),

                // three right points, starting from the top point
                new Point(VirtualWidth - x, RightTopSegmPoints[6].Y + GapW / 2),
                new Point(VirtualWidth - x1, VirtualHeight / 2),
                new Point(VirtualWidth - x2, RightBottomSegmPoints[0].Y - GapW / 2)
            };
        }


        /// <summary>
        /// Calulates points for the top segment
        /// </summary>
        /// <returns></returns>
        protected PointCollection GetTopSegmPoints()
        {
            double topLeftX = LeftTopSegmPoints[1].X + HorizSegSmallPartW;
            double topRightX = RightTopSegmPoints[1].X - HorizSegSmallPartW;
            double coefRound = RoundedCorners ? VirtualWidth / horizRoundCoef : 0;

            return new()
            {
                // three left points, starting from the bottom point
                new Point(LeftTopSegmPoints[2].X + GapW, HorizSegH + startPointThickness),
                new Point(LeftTopSegmPoints[1].X + GapShift(), HorizSegSmallPartH + startPointThickness),
                new Point(topLeftX, startPointThickness),

                // two top Bezier points starting from the left point
                new Point(topLeftX + coefRound, startPointThickness),
                new Point(topRightX - coefRound, startPointThickness),

                // three right points, starting from the top left point
                new Point(topRightX, startPointThickness),
                new Point(RightTopSegmPoints[1].X - GapShift(), HorizSegSmallPartH + startPointThickness),
                new Point(RightTopSegmPoints[0].X - GapW, HorizSegH + startPointThickness)
            };
        }


        /// <summary>
        /// Calulates points for the bottom segment
        /// </summary>
        /// <returns></returns>
        protected PointCollection GetBottomSegmPoints()
        {
            double botLeftX = LeftBottomSegmPoints[4].X + HorizSegSmallPartW;
            double botRightX = RightBottomSegmPoints[5].X - HorizSegSmallPartW;
            double coefRound = RoundedCorners ? VirtualWidth / horizRoundCoef : 0;

            return new()
            {
                // three left points, starting from the bottom point
                new Point(botLeftX, VirtualHeight - startPointThickness),
                new Point(LeftBottomSegmPoints[4].X + GapShift(), VirtualHeight - HorizSegSmallPartH - startPointThickness),
                new Point(LeftBottomSegmPoints[3].X + GapW, VirtualHeight - HorizSegH - startPointThickness),

                // three right points, starting from the top left point
                new Point(RightBottomSegmPoints[6].X - GapW, VirtualHeight - HorizSegH - startPointThickness),
                new Point(RightBottomSegmPoints[5].X - GapShift(), VirtualHeight - HorizSegSmallPartH - startPointThickness),
                new Point(botRightX, VirtualHeight - startPointThickness),

                // two bottom Bezier points starting from the right point
                new Point(botRightX - coefRound, VirtualHeight - startPointThickness),
                new Point(botLeftX + coefRound, VirtualHeight - startPointThickness)
            };
        }


        #endregion

        #region Segments' geometries

        /// <summary>
        /// Right top segment drawing
        /// </summary>
        /// <returns></returns>
        protected PathGeometry RightTopSegement()
        {
            RightTopSegmPoints = GetRightTopSegmPoints();
            Point startPoint = RightTopSegmPoints[0];

            // The Bezier curve for rounded corners
            PointCollection pointsBezier = new()
            {
                RightTopSegmPoints[1],
                RightTopSegmPoints[2],
                RightTopSegmPoints[3]
            };

            PolyBezierSegment bez = new()
            {
                Points = new PointCollection(pointsBezier)
            };

            PathGeometry pathGeometry = new();
            PathFigure pathFigure = new()
            {
                StartPoint = startPoint,
                IsClosed = true
            };
            pathGeometry.Figures.Add(pathFigure);
            pathFigure.Segments.Add(new LineSegment(RightTopSegmPoints[0], true));
            pathFigure.Segments.Add(new LineSegment(RightTopSegmPoints[1], true));
            pathFigure.Segments.Add(bez);
            pathFigure.Segments.Add(new LineSegment(RightTopSegmPoints[4], true));
            pathFigure.Segments.Add(new LineSegment(RightTopSegmPoints[5], true));
            pathFigure.Segments.Add(new LineSegment(RightTopSegmPoints[6], true));

            return pathGeometry;
        }


        /// <summary>
        /// Middle segment drawing
        /// </summary>
        /// <returns></returns>
        protected PathGeometry MiddleSegement()
        {
            MiddleSegmPoints = GetMiddleSegmPoints();

            Point startPoint = MiddleSegmPoints[0];
            PolyLineSegment segment = new() { Points = MiddleSegmPoints };
            return SegmentPathGeometry(startPoint, segment);
        }


        /// <summary>
        /// Right bottom segment drawing
        /// </summary>
        /// <returns></returns>
        protected PathGeometry RightBottomSegement()
        {
            RightBottomSegmPoints = GetRightBottomSegmPoints();
            Point startPoint = RightBottomSegmPoints[0];

            // The Bezier curve for rounded corners
            PointCollection pointsBezier = new()
            {
                RightBottomSegmPoints[3],
                RightBottomSegmPoints[4],
                RightBottomSegmPoints[5]
            };

            PolyBezierSegment bez = new()
            {
                Points = new PointCollection(pointsBezier)
            };

            PathGeometry pathGeometry = new();
            PathFigure pathFigure = new()
            {
                StartPoint = startPoint,
                IsClosed = true
            };
            pathGeometry.Figures.Add(pathFigure);
            pathFigure.Segments.Add(new LineSegment(RightBottomSegmPoints[0], true));
            pathFigure.Segments.Add(new LineSegment(RightBottomSegmPoints[1], true));
            pathFigure.Segments.Add(new LineSegment(RightBottomSegmPoints[2], true));
            pathFigure.Segments.Add(new LineSegment(RightBottomSegmPoints[3], true));
            pathFigure.Segments.Add(bez);
            pathFigure.Segments.Add(new LineSegment(RightBottomSegmPoints[6], true));

            return pathGeometry;
        }


        /// <summary>
        /// Top segment drawing
        /// </summary>
        /// <returns></returns>
        protected PathGeometry TopSegement()
        {
            TopSegmPoints = GetTopSegmPoints();
            Point startPoint = TopSegmPoints[0];

            // The left Bezier curve for rounded corners
            PointCollection pointsBezierLeft = new()
            {
                TopSegmPoints[1], TopSegmPoints[2], TopSegmPoints[3]
            };

            PolyBezierSegment bezLeft = new()
            {
                Points = new PointCollection(pointsBezierLeft)
            };


            // The right Bezier curve for rounded corners
            PointCollection pointsBezierRight = new()
            {
                TopSegmPoints[4], TopSegmPoints[5], TopSegmPoints[6]
            };

            PolyBezierSegment bezRight = new()
            {
                Points = new PointCollection(pointsBezierRight)
            };

            PathGeometry pathGeometry = new();
            PathFigure pathFigure = new()
            {
                StartPoint = startPoint,
                IsClosed = true
            };

            pathGeometry.Figures.Add(pathFigure);
            pathFigure.Segments.Add(new LineSegment(TopSegmPoints[0], true));
            pathFigure.Segments.Add(new LineSegment(TopSegmPoints[1], true));
            pathFigure.Segments.Add(bezLeft);
            pathFigure.Segments.Add(new LineSegment(TopSegmPoints[3], true));
            pathFigure.Segments.Add(new LineSegment(TopSegmPoints[4], true));
            pathFigure.Segments.Add(bezRight);
            pathFigure.Segments.Add(new LineSegment(TopSegmPoints[6], true));
            pathFigure.Segments.Add(new LineSegment(TopSegmPoints[7], true));

            return pathGeometry;
        }



        /// <summary>
        /// Left top segment drawing
        /// </summary>
        /// <returns></returns>
        protected PathGeometry LeftTopSegement()
        {
            LeftTopSegmPoints = GetLeftTopSegmPoints();
            Point startPoint = LeftTopSegmPoints[6];

            // The Bezier curve for rounded corners
            PointCollection pointsBezier = new()
            {
                LeftTopSegmPoints[6],
                LeftTopSegmPoints[0],
                LeftTopSegmPoints[1]
            };

            PolyBezierSegment bez = new()
            {
                Points = new PointCollection(pointsBezier)
            };

            PathGeometry pathGeometry = new();
            PathFigure pathFigure = new()
            {
                StartPoint = startPoint,
                IsClosed = true
            };
            pathGeometry.Figures.Add(pathFigure);
            pathFigure.Segments.Add(bez);
            pathFigure.Segments.Add(new LineSegment(LeftTopSegmPoints[2], true));
            pathFigure.Segments.Add(new LineSegment(LeftTopSegmPoints[3], true));
            pathFigure.Segments.Add(new LineSegment(LeftTopSegmPoints[4], true));
            pathFigure.Segments.Add(new LineSegment(LeftTopSegmPoints[5], true));

            return pathGeometry;
        }

        /// <summary>
        /// Left Bottom segment drawing
        /// </summary>
        /// <returns></returns>
        protected PathGeometry LeftBottomSegement()
        {
            LeftBottomSegmPoints = GetLeftBottomSegmPoints();
            Point startPoint = LeftBottomSegmPoints[0];

            // The Bezier curve for rounded corners
            PointCollection pointsBezier = new()
            {
                LeftBottomSegmPoints[4],
                LeftBottomSegmPoints[5],
                LeftBottomSegmPoints[6]
            };

            PolyBezierSegment bez = new()
            {
                Points = new PointCollection(pointsBezier)
            };

            PathGeometry pathGeometry = new();
            PathFigure pathFigure = new()
            {
                StartPoint = startPoint,
                IsClosed = true
            };
            pathGeometry.Figures.Add(pathFigure);
            pathFigure.Segments.Add(new LineSegment(LeftBottomSegmPoints[0], true));
            pathFigure.Segments.Add(new LineSegment(LeftBottomSegmPoints[1], true));
            pathFigure.Segments.Add(new LineSegment(LeftBottomSegmPoints[2], true));
            pathFigure.Segments.Add(new LineSegment(LeftBottomSegmPoints[3], true));
            pathFigure.Segments.Add(new LineSegment(LeftBottomSegmPoints[4], true));
            pathFigure.Segments.Add(bez);

            return pathGeometry;
        }




        /// <summary>
        /// Bottom segment drawing
        /// </summary>
        protected PathGeometry BottomSegement()
        {
            BottomSegmPoints = GetBottomSegmPoints();
            Point startPoint = BottomSegmPoints[1];

            // The right Bezier curve for rounded corners
            PointCollection pointsBezierRight = new()
            {
                BottomSegmPoints[4], BottomSegmPoints[5], BottomSegmPoints[6]
            };

            PolyBezierSegment bezRight = new()
            {
                Points = new PointCollection(pointsBezierRight)
            };

            // The left Bezier curve for rounded corners
            PointCollection pointsBezierLeft = new()
            {
                BottomSegmPoints[7], BottomSegmPoints[0], BottomSegmPoints[1]
            };

            PolyBezierSegment bezLeft = new()
            {
                Points = new PointCollection(pointsBezierLeft)
            };

            PathGeometry pathGeometry = new();
            PathFigure pathFigure = new()
            {
                StartPoint = startPoint,
                IsClosed = true
            };
            pathGeometry.Figures.Add(pathFigure);

            pathFigure.Segments.Add(new LineSegment(BottomSegmPoints[1], true));
            pathFigure.Segments.Add(new LineSegment(BottomSegmPoints[2], true));
            pathFigure.Segments.Add(new LineSegment(BottomSegmPoints[3], true));
            pathFigure.Segments.Add(new LineSegment(BottomSegmPoints[4], true));
            pathFigure.Segments.Add(bezRight);
            pathFigure.Segments.Add(new LineSegment(BottomSegmPoints[6], true));
            pathFigure.Segments.Add(new LineSegment(BottomSegmPoints[7], true));
            pathFigure.Segments.Add(bezLeft);

            return pathGeometry;
        }

        /// <summary>
        /// The class to detect selected segment
        /// </summary>
        public class GeometryWithSegm
        {
            public PathGeometry Geometry { get; set; }
            public int SegmentNumber { get; set; }
            public bool IsSelected { get; set; }

            public GeometryWithSegm(PathGeometry geometry, int segm, bool isSelected = false)
            {
                Geometry = geometry;
                SegmentNumber = segm;
                IsSelected = isSelected;
            }
        }
        #endregion
    }
}
