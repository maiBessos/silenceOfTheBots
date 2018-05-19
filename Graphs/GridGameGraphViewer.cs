using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GoE
{
    public partial class GridGameGraphViewer : UserControl
    {
        public delegate void CellClickHandler(object sender, UInt32 cellX, UInt32 cellY, MouseEventArgs m);

        public GridGameGraphViewer()
        {
            InitializeComponent();
            heightCellCount = widthCellCount = 8;
            resizeGrid(8, 8, 32, false);
        }

        /// <summary>
        /// resets colored cells
        /// </summary>
        /// <param name="WidthCellCount"></param>
        /// <param name="HeightCellCount"></param>
        /// <param name="CellSize"></param>
        /// <returns> list of colored cells that are now outside of boundaries</returns>
        public List<Point> resizeGrid(UInt32 WidthCellCount, UInt32 HeightCellCount, UInt32 CellSize, bool keepColoredCells)
        {
            List<Point> pointsToRemove = new List<Point>();
            if (coloredCells != null)
                foreach (var v in coloredCells)
                {
                    if (v.Key.Y >= HeightCellCount ||
                        v.Key.Y >= WidthCellCount)
                    {
                        pointsToRemove.Add(v.Key);
                    }
                }

            if (!keepColoredCells)
                coloredCells = new Dictionary<Point, NodeDisplay>();
            else
            {
                // remove only cells outside grid
                foreach (var p in pointsToRemove)
                    coloredCells.Remove(p);
            }


            widthCellCount = WidthCellCount;
            heightCellCount = HeightCellCount;
            cellSize = CellSize;
            return pointsToRemove;
        }
                
        public UInt32 WidthCellCount 
        {
            get { return widthCellCount; } 
            set 
            {
                widthCellCount = value;
                resizeGrid(widthCellCount, heightCellCount, cellSize, true);
            } 
        }
        public UInt32 HeightCellCount 
        {
            get { return heightCellCount; } 
            set 
            {
                heightCellCount = value;
                resizeGrid(widthCellCount, heightCellCount, cellSize, true);
            }
        }
        public UInt32 CellSize
        {
            get { return cellSize; }
            set
            {
                cellSize = value;
                resizeGrid(widthCellCount, heightCellCount, cellSize, true);
            }
        }

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Dictionary<System.Drawing.Point, GoE.NodeDisplay> ColoredCells
        {
            get { return this.coloredCells; }
            set 
            {
                coloredCells = value;
            }
        }

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public List<MovementDisplay> ColoredMovement
        {
            get;
            set;
        }

        

        public event CellClickHandler CellClick;

        public Point getCellCoord(MouseEventArgs e)
        {
            return new Point(
                (int)((e.X + hScrollBar1.Value) / cellSize), (int)((e.Y + vScrollBar1.Value) / cellSize));
        }

        private void GridGraph_Paint(object sender, PaintEventArgs e)
        {
            vScrollBar1.Maximum = 9 + Math.Max(0, (int)(heightCellCount * cellSize + 2) - (e.ClipRectangle.Height));// - hScrollBar1.Height));
            hScrollBar1.Maximum = 9 + Math.Max(0, (int)(widthCellCount * cellSize + 2) - (e.ClipRectangle.Width));// - vScrollBar1.Width));
            //e.Graphics.DrawString(e.ClipRectangle.Width.ToString(), new System.Drawing.Font(FontFamily.GenericSerif, 8, FontStyle.Regular, GraphicsUnit.Pixel),new SolidBrush(Color.Black),new PointF(50,50));
            //e.Graphics.DrawString(hScrollBar1.Maximum.ToString(), new System.Drawing.Font(FontFamily.GenericSerif, 8, FontStyle.Regular, GraphicsUnit.Pixel), new SolidBrush(Color.Black), new PointF(50, 100));
            hScrollBar1.Invalidate();
            vScrollBar1.Invalidate();

            e.Graphics.TranslateTransform(1 + -hScrollBar1.Value,1 + -vScrollBar1.Value);
            
            if (coloredCells != null)
                foreach (var v in coloredCells)
                {
                    Rectangle rect = new Rectangle((int)(v.Key.X * cellSize), (int)(v.Key.Y * cellSize),
                                                   (int)cellSize, (int)cellSize);
                    Point rectCenter = rect.Location;
                    rectCenter.Offset(0, (int)cellSize / 4);
                    SolidBrush rectBrush = new SolidBrush(v.Value.c);                    
                    SolidBrush textBrush = new SolidBrush(GoE.Utils.GraphicUtils.mostDistantColor(v.Value.c));
                    e.Graphics.FillRectangle(rectBrush, rect);
                    e.Graphics.DrawString(v.Value.text, new System.Drawing.Font(FontFamily.GenericSerif, 8, FontStyle.Regular, GraphicsUnit.Pixel), textBrush, rectCenter);
                }
            if(ColoredMovement != null)
                foreach(var v in ColoredMovement)
                {
                    if (v.From == v.To)
                    {
                        Point lineStart = new Point((int)(v.From.X * cellSize + cellSize / 4), (int)(v.From.Y * cellSize + cellSize / 4));
                        Point lineEnd = new Point((int)(v.To.X * cellSize + 3 * cellSize / 4), (int)(v.To.Y * cellSize + 3 * cellSize / 4));
                        e.Graphics.DrawLine(new Pen(v.lineColor, 1), lineStart, lineEnd);
                    }
                    else
                    {
                        Point lineStart = new Point((int)(v.From.X * cellSize + cellSize / 2), (int)(v.From.Y * cellSize + cellSize / 2));
                        Point lineEnd = new Point((int)(v.To.X * cellSize + cellSize / 2), (int)(v.To.Y * cellSize + cellSize / 2));

                        e.Graphics.DrawLine(new Pen(v.lineColor, 1), lineStart, lineEnd);
                    }
                }

            for (UInt32 x = 0; x <= widthCellCount; ++x)
                e.Graphics.DrawLine(Pens.Black, new Point((int)(x * cellSize), 0), new Point((int)(x * cellSize), (int)(heightCellCount * cellSize)));
            for (UInt32 y = 0; y <= heightCellCount; ++y)
                e.Graphics.DrawLine(Pens.Black, new Point(0, (int)(y * cellSize)), new Point((int)(widthCellCount * cellSize), (int)(y * cellSize)));

        }

        private void GridGameGraphViewer_MouseClick(object sender, MouseEventArgs e)
        {
            if (CellClick != null)// && e.Button == System.Windows.Forms.MouseButtons.Right)
            {
                Point cell = getCellCoord(e);
                CellClick.Invoke(sender, (UInt32)cell.X, (UInt32)cell.Y, e);
            }
        }

        private void GridGameGraphViewer_Resize(object sender, EventArgs e)
        {
            vScrollBar1.Value = hScrollBar1.Value = 0;
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

        private void GridGameGraphViewer_Load(object sender, EventArgs e)
        {

        }

        private UInt32 cellSize;
        private UInt32 heightCellCount, widthCellCount;

        /// <summary>
        /// maps cell index (x,y) to display data
        /// </summary>
        private Dictionary<System.Drawing.Point, GoE.NodeDisplay> 
            coloredCells = new Dictionary<Point, NodeDisplay>();

        private void GridGameGraphViewer_MouseMove(object sender, MouseEventArgs e)
        {
            //vScrollBar1.Maximum = 9 + Math.Max(0, (int)(heightCellCount * cellSize + 2) - (this.Height));
            //hScrollBar1.Maximum = 9 + Math.Max(0, (int)(widthCellCount * cellSize + 2) - (this.Width));
        }

    }
}
