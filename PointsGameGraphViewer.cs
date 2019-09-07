using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using GoE.GameLogic;
using GoE.Utils.Algorithms;
using System.Drawing.Drawing2D;

namespace GoE
{
    public struct SegmentView
    {
        public List<string> textLines { get; set; }
        public Color c1 { get; set; }
        public Color c2 { get; set; }
        public SegmentF seg { get; set; }
    }
    public struct NodeView 
    {
        public enum NodeShape
        {
            Circle = 0,
            Diamond = 1
        }
        public override int GetHashCode()
        {
            string txtN = (text==null)?"" : text;

            int hash = center.GetHashCode() ^ txtN.GetHashCode();
            for(int i = 0; i < Rads.Count;++i)
                hash ^= Rads[i].GetHashCode() ^ Fill[i].GetHashCode() ^ circleColor[i].GetHashCode();

            return hash;
        }
        public override bool Equals(object obj)
        {
            NodeView v = (NodeView)obj;
            return center == v.center && 
                    text == v.text &&
                    Rads.SequenceEqual(v.Rads) && 
                    Fill.SequenceEqual(v.Fill) && 
                    circleColor.SequenceEqual(v.circleColor);
        }

        public PointF center;
        public string text; // deprecated(use textLines instead) TODO:consider removing
        public List<string> textLines;

        public List<float> Rads { get; set; } // sorted from largest to smallest
        public List<bool> Fill { get; set; }
        public List<Color> circleColor { get; set; }

        public List<NodeShape> shapes { get; set; } // if uninitalized, default is circle
    }

    public partial class PointsGameGraphViewer : UserControl
    {
        public PointsGameGraphViewer()
        {
            InitializeComponent();

            nodes = new List<NodeView>();
            segments = new List<SegmentView>();
        }

        //public float Radius { get; set; }
        private void DoNothing_MouseWheel(object sender, EventArgs e)
        {
            HandledMouseEventArgs ee = (HandledMouseEventArgs)e;
            ee.Handled = true;
        }
        public void setScrollers(Point newVals)
        {
            vScrollBar1.Value = Math.Max(Math.Min(newVals.Y,vScrollBar1.Maximum), vScrollBar1.Minimum);
            hScrollBar1.Value = Math.Max(Math.Min(newVals.X, hScrollBar1.Maximum), hScrollBar1.Minimum);

        }
        public Point getScollersMaxVals()
        {
            refreshScrollers();
            return new Point(hScrollBar1.Maximum, vScrollBar1.Maximum);
        }
        public Point getScollersVals()
        {
            return new Point(hScrollBar1.Value, vScrollBar1.Value);
        }

        float zoom = 1;
        public float Zoom
        {
            get { return zoom;  }
            set
            {
                //PointF prevScollMax = getScollersMaxVals();
                PointF scrollVals = getScollersVals();
                float prevZoom = zoom;
                zoom = value;
                refreshScrollers();
                setScrollers(scrollVals.mult(zoom / prevZoom).toPointRound());
                //setScrollers(scrollVals.add(drawClipRect.Size.Width, drawClipRect.Size.Height).
                //    mult(zoom / prevZoom).toPointRound().add(-drawClipRect.Size.Width, -drawClipRect.Size.Height));
            }
        }

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public void resetElements(List<NodeView> newNodes = null,
            List<SegmentView> newSegs = null)
        {
            PointF elementPoint;

            if (newSegs == null)
                newSegs = new List<SegmentView>();
            if (newNodes == null)
                newNodes = new List<NodeView>();

            if (newNodes.Count == 0 && newSegs.Count ==0)
            {
                elementsWorld = new RectangleF(0, 0, 1, 1);
                nodes = new List<NodeView>();
                segments = new List<SegmentView>();
                return;
            }

            if (newNodes.Count == 0)
            {
                newNodes = new List<NodeView>();
                elementPoint = newSegs.First().seg.start;
            }
            else
                elementPoint = newNodes.First().center;
            
            nodes = new List<NodeView>(newNodes);
            segments = new List<SegmentView>(newSegs);

            elementsWorld = new RectangleF(elementPoint, new SizeF(0,0));
            foreach (var v in nodes)
            {
                elementsWorld = elementsWorld.ensureInclusion( new PointF(v.Rads.Last(), v.Rads.Last()).add(v.center));
                elementsWorld = elementsWorld.ensureInclusion(new PointF(-v.Rads.Last(), -v.Rads.Last()).add(v.center));
            }
            foreach(var s in segments)
            {
                elementsWorld = elementsWorld.ensureInclusion(s.seg.start);
                elementsWorld = elementsWorld.ensureInclusion(s.seg.end);
            }
        }

        public void setWorldRect(RectangleF World)
        {
            world = World;
        }

        RectangleF world = new RectangleF(0,0,0,0);
        RectangleF elementsWorld = new RectangleF(0,0,0,0); // used to set v/h scroll bars
        Rectangle  drawClipRect = new Rectangle(0,0,0,0);
        List<NodeView> nodes;
        List<SegmentView> segments;

        public Color RadiusColor { get; set; }

        private RectangleF getCurrentWorldRect()
        {
            RectangleF currentWorld = world;
            var nodesWorldVerts =
                new PointF[4] { new PointF(elementsWorld.Left, elementsWorld.Top), new PointF(elementsWorld.Right, elementsWorld.Top), new PointF(elementsWorld.Right, elementsWorld.Bottom), new PointF(elementsWorld.Left, elementsWorld.Bottom) };
            foreach (var v in nodesWorldVerts)
                currentWorld = currentWorld.ensureInclusion(v);
            return currentWorld;
        }

        private void refreshScrollers()
        {
            vScrollBar1.Maximum = Math.Max(0, (int)Math.Ceiling(Zoom * (getCurrentWorldRect().Height)) - drawClipRect.Height) + 9;
            hScrollBar1.Maximum = Math.Max(0, (int)Math.Ceiling(Zoom * (getCurrentWorldRect().Width)) - drawClipRect.Width) + 9;
        }
        private void GridGraph_Paint(object sender, PaintEventArgs e)
        {
            drawClipRect = e.ClipRectangle;
           

            //vScrollBar1.Maximum = 9 + Math.Max(0, (int)(Radius * 2) - (e.ClipRectangle.Height));
            //hScrollBar1.Maximum = 9 + Math.Max(0, (int)(Radius * 2) - (e.ClipRectangle.Width));

            //vScrollBar1.Minimum = (int)world.Y;
            //hScrollBar1.Minimum = (int)world.X;
            refreshScrollers();
            
            hScrollBar1.Invalidate();
            vScrollBar1.Invalidate();

            e.Graphics.TranslateTransform(1 + -hScrollBar1.Value - getCurrentWorldRect().X * zoom, 1 + -vScrollBar1.Value - getCurrentWorldRect().Y * zoom);

            // we do manual zoom since node centers are float values instead of int and we want to mitigate that
            //e.Graphics.ScaleTransform(zoom, zoom); 

            SolidBrush textBrush = new SolidBrush(Color.Black);
            if (nodes != null)
                foreach (var v in nodes)
                {
                    for (int i = 0; i < v.Rads.Count; ++i)
                    {
                        Rectangle rect = new Rectangle((int)(Zoom *(v.center.X - v.Rads[i])), (int)(Zoom * (v.center.Y - v.Rads[i])),
                                                       (int)(Zoom*v.Rads[i] * 2), (int)(Zoom*v.Rads[i] * 2));
                        SolidBrush rectBrush = new SolidBrush(v.circleColor[i]);
                        //SolidBrush textBrush = new SolidBrush(GoE.Utils.GraphicUtils.mostDistantColor(v.circleColor[i]));
                        
                        if (v.shapes == null || v.shapes.Count <= i || v.shapes[i] == NodeView.NodeShape.Circle)
                        {
                            if (v.Fill[i])
                                e.Graphics.FillEllipse(rectBrush, rect);
                            else
                                e.Graphics.DrawEllipse(new Pen(rectBrush), rect);
                        }
                        else 
                        {
                            PointF[] poly = new PointF[4];
                            poly[0] = new PointF(rect.X + (float)rect.Width / 2, rect.Y);
                            poly[1] = new PointF(rect.X + rect.Width,rect.Y + (float)rect.Height/ 2);
                            poly[2] = new PointF(rect.X + (float)rect.Width / 2, rect.Y + rect.Height);
                            poly[3] = new PointF(rect.X , rect.Y + (float)rect.Height/2);

                            if (v.Fill[i])
                                e.Graphics.FillPolygon(rectBrush, poly);
                            else
                                e.Graphics.DrawPolygon(new Pen(rectBrush), poly);
                        }

                        if (v.textLines == null || v.textLines.Count == 0)
                            e.Graphics.DrawString(v.text, new System.Drawing.Font(FontFamily.GenericSerif, 14, FontStyle.Regular, GraphicsUnit.Pixel), textBrush, v.center.mult(zoom));
                        else
                        {
                            float centerY = 0;
                            foreach (string strLine in v.textLines)
                            {
                                e.Graphics.DrawString(strLine, new System.Drawing.Font(FontFamily.GenericSerif, 14, FontStyle.Bold, GraphicsUnit.Pixel), textBrush, v.center.mult(zoom).addF(0, centerY));
                                centerY += 15;       
                            }
                        }

                    }
                }
            if(segments != null)
            {
                foreach(var s in segments)
                {
                    PointF segStart = s.seg.start.mult(Zoom);
                    PointF segEnd = s.seg.end.mult(Zoom);
                    PointF segCenter = s.seg.getPoint(0.33f).mult(Zoom); // 0.33 instead of 0.5, so if there are two opposite edges between same points, labels won't collide
                    
                    Pen p = new Pen(new LinearGradientBrush(segStart,segEnd,s.c1,s.c2));
                    e.Graphics.DrawLine(p, segStart, segEnd);
                    float centerY = 0;
                    foreach (string strLine in s.textLines)
                    {
                        e.Graphics.DrawString(strLine, new System.Drawing.Font(FontFamily.GenericSerif, 14, FontStyle.Bold, GraphicsUnit.Pixel), textBrush, segCenter.addF(0, centerY));
                        centerY += 15;
                    }
                }
            }
            
        }
        
        private void PointsGameGraphViewer_Resize(object sender, EventArgs e)
        {
            vScrollBar1.Value = vScrollBar1.Maximum / 2;
            hScrollBar1.Value = hScrollBar1.Maximum / 2;
            this.Invalidate();
        }
       

        private void vScrollBar1_Scroll(object sender, ScrollEventArgs e)
        {
            this.Invalidate();
        }

        private void hScrollBar1_Scroll(object sender, ScrollEventArgs e)
        {
            this.Invalidate();
        }

    
      
    }
}
