using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GoE
{
    public partial class frmGridGraphEditor : Form
    {
        private GridGameGraphController gridController = null;
        private bool dirty = false; // turned on if loaded graph was modified
        private GridGameGraph Graph { get; set; }
        private NodeType lastDrawType = NodeType.Target; // updated (only) in drawNode()
        private Point currentlyEditedCell = new Point(-1, -1);

        public frmGridGraphEditor()
        {
            InitializeComponent();
        }

        
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string GraphPath { get; private set; }

        private void drawNode(NodeType t)
        {
            dirty = true;
            lastDrawType = t;
            gridController.setNodeType(currentlyEditedCell, t);
            gridController.View.Invalidate();
            currentlyEditedCell.X = currentlyEditedCell.Y = -1;
        }
        private void resizeGridToolStripMenuItem_Click(object sender, EventArgs e)
        {
            drawNode(NodeType.Target);
        }
        private void setSinkMenuItem_Click(object sender, EventArgs e)
        {
            drawNode(NodeType.Sink);
        }
        private void setBlockedMenuItem_Click(object sender, EventArgs e)
        {
            drawNode(NodeType.Blocked);
        }
        private void setNormalToolStripMenuItem_Click(object sender, EventArgs e)
        {
            drawNode(NodeType.Normal);
        }

        private void gridGraphView_CellClick(object sender, uint cellX, uint cellY, MouseEventArgs m)
        {
            switch(m.Button)
            {
                case System.Windows.Forms.MouseButtons.Right:
                    currentlyEditedCell.X = (int)cellX;
                    currentlyEditedCell.Y = (int)cellY;
                    mouseMenuGridGraphEditor.Show((Control)(sender), m.Location);
                    break;
                case System.Windows.Forms.MouseButtons.Left:
                    currentlyEditedCell.X = (int)cellX;
                    currentlyEditedCell.Y = (int)cellY;
                    drawNode(lastDrawType);
                    break;
            }
        }
        private void frmGridGraphEditor_load(object sender, EventArgs e)
        {
            Graph = new GridGameGraph();
            gridController = new GridGameGraphController(gridGraphView, Graph);
            gridController.resize(new Size(8, 8), true);
        }


        private void btnSave_Click(object sender, EventArgs e)
        {
            SaveFileDialog d = new SaveFileDialog();
            
            d.Filter = "Graph Files (*.ggrp)|*.ggrp";
            d.InitialDirectory = GoE.AppConstants.PathLocations.GRAPH_FILES_FOLDER;
            try
            {
                if (d.ShowDialog() != System.Windows.Forms.DialogResult.OK)
                    return;

                string[] serialization = Graph.serialize();
                File.WriteAllLines(d.FileName, serialization);
                GraphPath = d.FileName;
                dirty = false;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void btnLoad_Click(object sender, EventArgs e)
        {
            OpenFileDialog d = new OpenFileDialog();
            d.Filter = "Graph Files (*.ggrp)|*.ggrp";
            try
            {
                if (d.ShowDialog() != System.Windows.Forms.DialogResult.OK)
                    return;

                string[] serialization = File.ReadAllLines(d.FileName);
                Graph = new GridGameGraph();
                Graph.deserialize(serialization);
                gridController.resetGraph(Graph);
                gridGraphView.Invalidate();
                GraphPath = d.FileName;
                dirty = false;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void btnResize_Click(object sender, EventArgs e)
        {
            try
            {
                List<string> res = InputBox.ShowDialog(new string[2] { "width cell count", "height cell count" }, "Set grid size");
                //UInt32 cellsize = gridGameGraphViewer1.CellSize;

                //List<Point> pointsToRemove = 
                //    gridGameGraphViewer1.resizeGrid(UInt32.Parse(res[0]), UInt32.Parse(res[1]), cellsize, true);

                //graph.resizeGrid(UInt32.Parse(res[0]), UInt32.Parse(res[1]), false);

                gridController.resize(new Size(Int32.Parse(res[0]), Int32.Parse(res[1])), false);
                gridController.View.Invalidate();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void btnDone_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void frmGridGraphEditor_FormClosing(object sender, FormClosingEventArgs e)
        {
            if(dirty)
            {
                switch(MessageBox.Show("Do you want to save changes?", "Close graph editor", MessageBoxButtons.YesNoCancel))
                {
                    case System.Windows.Forms.DialogResult.Yes:
                        btnSave_Click(sender, e); break;
                    case System.Windows.Forms.DialogResult.No:
                        break;
                    default:
                        e.Cancel = true;
                        break;
                }
            }
        }

        private void btnSinks_Click(object sender, EventArgs e)
        {
            try
            {
                dirty = true;

                List<string> res = InputBox.
                    ShowDialog(new string[] { "Target X", "Target Y", "Min Sink Distance", "Max Sink Distance"}, "Surround Target With Sinks");
                Point center = new Point(Int32.Parse(res[0]), Int32.Parse(res[1]));
                int minDist = Int32.Parse(res[2]);
                int maxDist = Int32.Parse(res[3]);
                
                HashSet<Point> excluded = new HashSet<Point>();
                List<Point> excludedTemp = new List<Point>();
                if (minDist > 0)
                    excludedTemp  = Graph.getNodesWithinDistance(center, minDist - 1);

                foreach(Point p in excludedTemp)
                    excluded.Add(p);
                List<Point> relevant = Graph.getNodesWithinDistance(center,maxDist);
                
                foreach(Point p in relevant)
                    if(!excluded.Contains(p))
                    {
                        currentlyEditedCell = p;
                        gridController.setNodeType(currentlyEditedCell,NodeType.Sink);
                    }

                currentlyEditedCell = center;
                gridController.setNodeType(currentlyEditedCell, NodeType.Target);
                
            }
            catch (Exception) { }

            gridController.View.Invalidate();
        }

        private void gridGraphView_MouseMove(object sender, MouseEventArgs e)
        {
            Point cell = gridGraphView.getCellCoord(e);
            lblCoord.Text = cell.X.ToString() + "," + cell.Y.ToString();
        }

 

    }
}
