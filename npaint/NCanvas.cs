/* Курдюков Алексей, 21.08.2013 */
/*
	Правильнее было бы унаследовать все использованные объекты - 
	это позволит иметь специализированные редакторы объектов в них самих,
	проще будет с доработкой интерфейса (например с добавлением контрольных точек),
	легче будет добавлять новые объекты - но это потребует примерно столько же времени,
	сколько затрачено на разработку того, что есть	
*/
using System;
using System.IO;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using System.Windows;
using System.Windows.Shapes;
using System.Windows.Media;
using System.Windows.Input;
using System.Runtime.Serialization.Formatters.Binary;

namespace WpfApplication1
{
    class NCanvas
    {
        public enum NToolMode { None, Select, MultiSelect, Rect, Line, Move, Rotate, Delete, Scale, Pan };
        protected Canvas canvas;
        protected NToolMode toolMode;
        protected Point pointStart;
        protected Point pointEnd;
        protected Point pointInitial;
        protected double ConnectDistance;
        Color startColor;
        Color stopColor;
        int lineThickness;
        Shape activeShape;        
        List<Shape> shapes;//multiselect-only
        List<Point> pointsInitial;//multiselect-only
        int linePointIndex;

        public NCanvas(Canvas canvas)
        {
            toolMode = NToolMode.None;
            pointStart = new Point(0, 0);
            pointEnd = new Point(0, 0);
            lineThickness = 1;
            this.canvas = canvas;
            activeShape = null;
            ConnectDistance = 5;
        }

        public void SaveToFile(String fileName)
        {
            System.Xml.XmlDocument doc = new System.Xml.XmlDocument();
            System.Xml.XmlElement root = doc.CreateElement("NCanvas");
            foreach (Shape shape in canvas.Children)
            {
                SaveShape(root, shape);
            }
            doc.AppendChild(root);
            doc.Save(fileName);
        }

        protected void SaveShape(System.Xml.XmlElement root, Shape shape)
        {
            System.Xml.XmlElement xShape = root.OwnerDocument.CreateElement("Shape");
            root.AppendChild(xShape);
            AddAttribute(xShape, "Left", Canvas.GetLeft(shape).ToString());
            AddAttribute(xShape, "Top", Canvas.GetTop(shape).ToString());
            AddAttribute(xShape, "Width", shape.Width.ToString());
            AddAttribute(xShape, "Height", shape.Height.ToString());
            AddAttribute(xShape, "Angle", (shape.RenderTransform as RotateTransform).Angle.ToString());
            if (shape is Rectangle)
            {
                AddAttribute(xShape, "Type", "Rectangle");
                AddAttribute(xShape, "Color0", (shape.Fill as LinearGradientBrush).GradientStops[0].Color.ToString());
                AddAttribute(xShape, "Color1", (shape.Fill as LinearGradientBrush).GradientStops[1].Color.ToString());
            }
            else if (shape is Polyline)
            {
                AddAttribute(xShape, "Type", "Polyline");
                AddAttribute(xShape, "Color", (shape.Stroke as SolidColorBrush).Color.ToString());
                AddAttribute(xShape, "Thickness", shape.StrokeThickness.ToString());
                AddAttribute(xShape, "Points", (shape as Polyline).Points.ToString());
            }
            //don't need to save/load any unsupported types
        }

        protected void AddAttribute(System.Xml.XmlElement tag, String name, String val)
        {
            System.Xml.XmlAttribute attr = tag.OwnerDocument.CreateAttribute(name);
            attr.Value = val;
            tag.Attributes.Append(attr);
        }

        public void LoadFromFile(String fileName)
        {
            try //just stop on error
            {
                Reset();
                System.Xml.XmlDocument doc = new System.Xml.XmlDocument();
                doc.Load(fileName);
                System.Xml.XmlNodeList xShapes = doc.DocumentElement.SelectNodes("Shape");
                foreach (System.Xml.XmlElement tag in xShapes)
                {
                    LoadShape(tag);
                }
            }
            catch (Exception)
            {
            }
        }

        protected void LoadShape(System.Xml.XmlElement tag)
        {
            String type = tag.GetAttribute("Type");
            if (type == "Rectangle")
            {
                Shape shape;
                LinearGradientBrush brush = new LinearGradientBrush();
                brush.GradientStops.Add(new GradientStop((Color)ColorConverter.ConvertFromString(tag.GetAttribute("Color0")), 0.0));
                brush.GradientStops.Add(new GradientStop((Color)ColorConverter.ConvertFromString(tag.GetAttribute("Color1")), 1.0));
                shape = new Rectangle();
                shape.Stroke = new SolidColorBrush(Colors.Black);
                shape.StrokeThickness = 1;
                shape.Fill = brush;
                shape.Width = Convert.ToDouble(tag.GetAttribute("Width"));
                shape.Height = Convert.ToDouble(tag.GetAttribute("Height"));
                shape.RenderTransform = new RotateTransform(Convert.ToDouble(tag.GetAttribute("Angle")));
                shape.RenderTransformOrigin = new Point(0.5, 0.5);
                Canvas.SetLeft(shape, Convert.ToDouble(tag.GetAttribute("Left")));
                Canvas.SetTop(shape, Convert.ToDouble(tag.GetAttribute("Top")));
                canvas.Children.Add(shape);
            }
            else if (type == "Polyline")
            {
                Polyline shape = new Polyline();
                shape.Stroke = new SolidColorBrush((Color)ColorConverter.ConvertFromString(tag.GetAttribute("Color")));
                shape.StrokeThickness = Convert.ToDouble(tag.GetAttribute("Thickness"));
                //shape.Points = PointCollection.Parse(tag.GetAttribute("Points")); //didn't fly
                string[] strpointarray = tag.GetAttribute("Points").Split();
                for (int i = 0; i < strpointarray.Count();++i)
                {
                    string[] newitem = strpointarray[i].Split(';');
                    shape.Points.Add(new Point(Convert.ToDouble(newitem[0]), Convert.ToDouble(newitem[1])));
                }                
                shape.RenderTransform = new RotateTransform(Convert.ToDouble(tag.GetAttribute("Angle")));
                shape.RenderTransformOrigin = new Point(0.5, 0.5);
                Canvas.SetLeft(shape, Convert.ToDouble(tag.GetAttribute("Left")));
                Canvas.SetTop(shape, Convert.ToDouble(tag.GetAttribute("Top")));
                canvas.Children.Add(shape);
            }
        }

        public void Reset()
        {
            canvas.Children.Clear();
        }

        protected void DeactivateShape()
        {            
            linePointIndex = -1;
            if (activeShape == null && (shapes == null || shapes.Count == 0)) return;
            if (toolMode == NToolMode.MultiSelect)
            {
                canvas.Children.Remove(activeShape);
                activeShape = null;
                return;
            };
            if (activeShape != null) activeShape.StrokeDashArray = null;
            if (shapes != null && shapes.Count > 0)
            {
                foreach (Shape shape in shapes)
                    shape.StrokeDashArray = null;
                shapes.Clear();
            }
            activeShape = null;
        }

        public NToolMode ToolMode
        {
            get { return toolMode; }
            set
            {
                toolMode = value;
                DeactivateShape();
            }
        }

        public int LineThickness
        {
            get { return lineThickness; }
            set
            {
                lineThickness = value;
                if (activeShape != null && activeShape is Polyline) activeShape.StrokeThickness = lineThickness;
            }
        }

        public Color ColorStart
        {
            get { return startColor; }
            set
            {
                startColor = value;
                if (activeShape != null) (activeShape.Fill as LinearGradientBrush).GradientStops[0].Color = startColor;
            }
        }

        public Color ColorStop
        {
            get { return stopColor; }
            set
            {
                stopColor = value;
                if (activeShape != null) (activeShape.Fill as LinearGradientBrush).GradientStops[1].Color = stopColor;
            }
        }

        public void MouseDown(Point pt, bool multiselect, bool doubleclick)
        {
            if (multiselect) toolMode = NToolMode.MultiSelect;
            switch (toolMode)
            {
                case NToolMode.Rect: DoRect(pt); break;
                case NToolMode.Select: DoSelect(pt, doubleclick); break;
                case NToolMode.Pan: DoSelect(pt, doubleclick); break;
                case NToolMode.Delete: DoDelete(pt); break;
                case NToolMode.Scale: DoScale(pt); break;
                case NToolMode.Rotate: DoRotate(pt); break;
                case NToolMode.Line: DoLine(pt); break;
                case NToolMode.Move: pointStart = pt; break;
                case NToolMode.MultiSelect:
                    DoRect(pt);
                    activeShape.Fill = new SolidColorBrush();
                    DoubleCollection dc = new DoubleCollection(2);
                    dc.Add(5); dc.Add(2);
                    activeShape.StrokeDashArray = dc;
                    break;
            }
        }

        public void MouseUp(Point pt)
        {
            switch (toolMode)
            {
                case NToolMode.Pan: canvas.ReleaseMouseCapture(); break;
                case NToolMode.Rect: DoRect(pt); break;
                case NToolMode.MultiSelect: DoMultiSelect(pt); break;
                case NToolMode.Line: break;
                case NToolMode.Select:
                    if (activeShape is Polyline && linePointIndex > -1)
                    {
                        //control point move
                        foreach (Point point in (activeShape as Polyline).Points)
                        {
                            if (point != (activeShape as Polyline).Points[linePointIndex] && Distance(point, (activeShape as Polyline).Points[linePointIndex]) <= ConnectDistance)
                            {
                                (activeShape as Polyline).Points.RemoveAt(linePointIndex);
                                break;
                            }
                        }
                    }
                    DeactivateShape();
                    break;
                default: DeactivateShape(); break;
            }
        }

        protected void DoRect(Point pt)
        {
            if (activeShape == null)
            {
                pointStart = pt;
                pointEnd = pointStart;
                LinearGradientBrush brush = new LinearGradientBrush();
                brush.GradientStops.Add(new GradientStop(startColor, 0.0));
                brush.GradientStops.Add(new GradientStop(stopColor, 1.0));
                activeShape = new Rectangle();
                activeShape.Stroke = new SolidColorBrush(Colors.Black);
                activeShape.StrokeThickness = 1;
                activeShape.Fill = brush;
                activeShape.Width = 0;
                activeShape.Height = 0;
                activeShape.RenderTransform = new RotateTransform(0);
                activeShape.RenderTransformOrigin = new Point(0.5, 0.5);
                Canvas.SetLeft(activeShape, pointStart.X);
                Canvas.SetTop(activeShape, pointStart.Y);
                canvas.Children.Add(activeShape);
            }
            else
            {
                SetShapeSize(activeShape, pointStart, pointEnd);
                DeactivateShape();
            }
        }

        protected void DoLine(Point pt)
        {
            pointStart = pt;
            pointEnd = pointStart;
            if (activeShape == null)
            {
                pointStart = pt;
                pointEnd = pointStart;
                Polyline shape = new Polyline();
                shape.Stroke = new SolidColorBrush(ColorStart);
                shape.StrokeThickness = lineThickness;
                shape.Points = new PointCollection();
                shape.Points.Add(new Point(0, 0));
                shape.Points.Add(new Point(0, 0));
                shape.RenderTransform = new RotateTransform(0);
                shape.RenderTransformOrigin = new Point(0.5, 0.5);
                activeShape = shape;
                Canvas.SetLeft(activeShape, pointStart.X);
                Canvas.SetTop(activeShape, pointStart.Y);
                canvas.Children.Add(activeShape);
            }
            else
            {
                (activeShape as Polyline).Points.Add(new Point(pt.X - Canvas.GetLeft(activeShape), pt.Y - Canvas.GetTop(activeShape)));
            }
        }

        protected void SetShapeSize(Shape shape, Point pt1, Point pt2)
        {
            double left = Math.Min(pt1.X, pt2.X);
            double top = Math.Min(pt1.Y, pt2.Y);
            double width = Math.Abs(pt1.X - pt2.X);
            double height = Math.Abs(pt1.Y - pt2.Y);
            shape.Width = width;
            shape.Height = height;
            Canvas.SetLeft(shape, left);
            Canvas.SetTop(shape, top);
        }

        protected void SetShapePos(Shape shape, Point pt1, Point pt2, Point pointInitial)
        {
            if (shape is Rectangle || activeShape == null) //can move lines in multiselect mode
            {
                Canvas.SetLeft(shape, pointInitial.X + (pt2.X - pt1.X));
                Canvas.SetTop(shape, pointInitial.Y + (pt2.Y - pt1.Y));
            }
            else if (shape is Polyline)
            {
                Polyline line = (shape as Polyline);
                if (linePointIndex != -1)
                    line.Points[linePointIndex] = new Point(pt2.X - Canvas.GetLeft(shape), pt2.Y - Canvas.GetTop(shape));
                else
                    line.Points[line.Points.Count - 1] = new Point(pt2.X - Canvas.GetLeft(shape), pt2.Y - Canvas.GetTop(shape));
            }
        }

        protected void SetShapeRotation(Shape shape, Point pt1, Point pt2)
        {
            (shape.RenderTransform as RotateTransform).Angle -= (pt2.X - pt1.X) / 5 + (pt2.Y - pt1.Y) / 20 /* это для плавности */;
        }

        protected void DoPan(Point pointStart, Point pointEnd, Point pointInitial)
        {            
            (canvas.RenderTransform as TranslateTransform).X = pointInitial.X + (pointEnd.X - pointStart.X);
            (canvas.RenderTransform as TranslateTransform).Y = pointInitial.Y + (pointEnd.Y - pointStart.Y);
            Debug.WriteLine("" + pointInitial + " + (" + pointEnd +" - " +pointStart+")");
        }

        public void MouseMove(Point pt, MouseButtonState state)
        {
            if (activeShape == null && (shapes == null || shapes.Count == 0))
                if (state == MouseButtonState.Pressed && toolMode == NToolMode.Pan)
                {                    
                    pointEnd = canvas.TranslatePoint(pt, null);
                    DoPan(pointStart, pointEnd, pointInitial);
                }
                else return;
            switch (toolMode)
            {
                case NToolMode.Rect:
                    if (state == MouseButtonState.Pressed)
                    {
                        pointEnd = pt;
                        SetShapeSize(activeShape, pointStart, pointEnd);
                    }
                    break;
                case NToolMode.MultiSelect:
                    if (state == MouseButtonState.Pressed)
                    {
                        pointEnd = pt;
                        SetShapeSize(activeShape, pointStart, pointEnd);
                    }
                    break;
                case NToolMode.Scale:
                    if (state == MouseButtonState.Pressed)
                    {
                        pointEnd = pt;
                        SetShapeSize(activeShape, pointStart, pointEnd);
                    }
                    break;
                case NToolMode.Rotate:
                    if (state == MouseButtonState.Pressed)
                    {
                        pointEnd = pt;
                        SetShapeRotation(activeShape, pointStart, pointEnd);
                        pointStart = pointEnd;
                    }
                    break;
                case NToolMode.Line:
                    if (state == MouseButtonState.Released)
                    {
                        pointEnd = pt;
                        SetShapePos(activeShape, pointStart, pointEnd, pointInitial);
                    }
                    break;
                default:
                    if (state == MouseButtonState.Pressed)
                    {
                        pointEnd = pt;
                        if (activeShape != null)
                        {
                            SetShapePos(activeShape, pointStart, pointEnd, pointInitial);
                        }
                        else if (shapes != null)
                        {
                            //foreach (Shape shape in shapes)
                            for (int i = 0; i < shapes.Count; ++i)
                            {
                                SetShapePos(shapes[i], pointStart, pointEnd, pointsInitial[i]);
                            }
                        }
                    }
                    break;
            }
        }

        public void DoSelect(Point pt, bool doubleclick = false)
        {
            DeactivateShape();
            HitTestResult result = VisualTreeHelper.HitTest(canvas, pt);
            if (result != null && result.VisualHit is Shape)
            {
                activeShape = (result.VisualHit as Shape);
                DoubleCollection dc = new DoubleCollection(2);
                dc.Add(2); dc.Add(2);
                activeShape.StrokeDashArray = dc;
                pointInitial = new Point(Canvas.GetLeft(activeShape), Canvas.GetTop(activeShape));
                pointStart = pt;
                if (activeShape is Polyline)
                {//start polyline editing
                    linePointIndex = GetPolylinePointIndex(activeShape as Polyline, pt, doubleclick);
                }
            }
            else
            {
                toolMode = NToolMode.Pan;
                pointInitial = new Point((canvas.RenderTransform as TranslateTransform).X, (canvas.RenderTransform as TranslateTransform).Y);
                pointStart = canvas.TranslatePoint(pt, null);
                Debug.WriteLine("start pan: " + pointStart);
                canvas.CaptureMouse();
            }
        }

        protected int GetPolylinePointIndex(Polyline shape, Point pt, bool addNew)
        {
            Point ptInternal = new Point(pt.X - Canvas.GetLeft(shape), pt.Y - Canvas.GetTop(shape));
            if (addNew)
            {
                double dist0 = 9999999999999;//something big enough
                double dist;
                int index=-1;
                //find nearest line seg:
                for (int i = 1; i < shape.Points.Count; ++i)
                {
                    dist = Distance(ptInternal, shape.Points[i], shape.Points[i-1]);
                    if (dist<dist0)
                    {
                        dist0 = dist;
                        index = i;
                    }
                }
                if (index>-1)
                    shape.Points.Insert(index, ptInternal);
            } else {
                for (int i = 0; i < shape.Points.Count; ++i)
                {
                    if (Distance(ptInternal, shape.Points[i]) <= ConnectDistance)
                    {
                        return i;
                    }
                }
            }
            //nothing found
            return -1;
        }

        protected double Distance(Point pt1, Point pt2)
        {
            return Math.Sqrt((pt1.X - pt2.X) * (pt1.X - pt2.X) + (pt1.Y - pt2.Y) * (pt1.Y - pt2.Y));
        }

        protected double Distance(Point pt, Point pt0, Point pt1)
        {
            return Math.Abs(((pt0.Y - pt1.Y) * pt.X + (pt1.X - pt0.X) * pt.Y + (pt0.X * pt1.Y - pt1.X * pt0.Y)) /
                Distance(pt0, pt1));
        }

        public void DoMultiSelect(Point pt)
        {
            shapes = new List<Shape>();
            RectangleGeometry HitTestArea = new RectangleGeometry(new Rect(
                Canvas.GetLeft(activeShape), Canvas.GetTop(activeShape),
                activeShape.Width, activeShape.Height));
            VisualTreeHelper.HitTest(canvas, null,
                result =>
                {
                    var shape = result.VisualHit as Shape;

                    if (shape != null && shape != activeShape)
                    {
                        shapes.Add(shape);
                    }

                    return HitTestResultBehavior.Continue;
                },
                new GeometryHitTestParameters(HitTestArea));
            if (shapes.Count > 0)
            {
                DoubleCollection dc = new DoubleCollection(2);
                dc.Add(2); dc.Add(2);
                pointsInitial = new List<Point>();
                foreach (Shape shape in shapes)
                {
                    shape.StrokeDashArray = dc;
                    pointsInitial.Add(new Point(Canvas.GetLeft(shape), Canvas.GetTop(shape)));
                }
                pointStart = pt;
            }
            DeactivateShape();
            toolMode = NToolMode.Move;            
        }

        public void DoDelete(Point pt)
        {
            DoSelect(pt);
            if (activeShape != null)
            {
                canvas.Children.Remove(activeShape);
                activeShape = null;
            }
        }

        public void DoScale(Point pt)
        {
            DoSelect(pt);
            if (activeShape != null)
            {
                pointStart = new Point(Canvas.GetLeft(activeShape), Canvas.GetTop(activeShape));
            }
        }

        public void DoRotate(Point pt)
        {
            DoSelect(pt);
            if (activeShape != null)
            {
                pointStart = new Point(Canvas.GetLeft(activeShape), Canvas.GetTop(activeShape));
            }
        }
    }
}
