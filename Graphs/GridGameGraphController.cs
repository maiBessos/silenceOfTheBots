using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GoE.Utils;
namespace GoE
{
    public class GridGameGraphController
    {
        public GridGameGraphController(GoE.GridGameGraphViewer view, GridGameGraph graph)
        {
            this.Graph = graph;
            this.View = view;

            View.resizeGrid((uint)Graph.WidthCellCount, (uint)Graph.HeightCellCount, 32, false);
            foreach (var n in Graph.Nodes)
                setNodeType(n.Key, n.Value.t);
            View.Invalidate();

        }
        public void resetGraph(GridGameGraph newGraph)
        {
            this.Graph = newGraph;
            resize(new Size((int)newGraph.WidthCellCount, (int)newGraph.HeightCellCount), true);
            
            foreach (var n in newGraph.Nodes)
                setNodeType(n.Key, n.Value.t);
            
            View.Invalidate();
            
        }

        public void setMovement(Point from, List<Point> to)
        {
            if (View.ColoredMovement == null)
                View.ColoredMovement = new List<MovementDisplay>();
            View.ColoredMovement.Clear();
            View.ColoredMovement.Add(new MovementDisplay() { lineColor = Color.Black, From = from, To = to[0] });
            
            for (int pi = 1; pi < to.Count; ++pi)
                View.ColoredMovement.Add(new MovementDisplay() { lineColor = Color.Green, From = to[pi-1], To = to[pi] });

            View.Invalidate();
        }
        public void setMovement(Point from, Point to)
        {
            var tmpList = new List<Point>();
            tmpList.Add(to);
            setMovement(from, tmpList);
        }

        

        public void clearColors()
        {
            nodeColor.Clear();
        }


        public void setColors(Dictionary<Point, GoE.NodeDisplay> colors)
        {
            foreach(var v in colors)
            {
                nodeColor[v.Key] = v.Value.c;
                nodeText[v.Key] = v.Value.text;
            }
            View.ColoredCells = colors;
            View.Invalidate();
        }
        public void setColors(List<Point> pursuers, 
                              List<Point> evaders, 
                              List<Point> destinationPoints, 
                              List<Point> policyInput, 
                              List<Point> additionalMarks,
                              List<Tuple<Color, List<Point>>> coloredLists = null)
        {
            List<Point> blocked = Graph.getNodesByType(NodeType.Blocked);

            // reset nodeColor:
            

            foreach (Point p in blocked)
                nodeColor[p] = Color.Black;

            foreach (Point p in destinationPoints)
            {
                if (nodeColor.Keys.Contains(p) && nodeColor[p] != Color.Gray)
                    nodeColor[p] = GraphicUtils.colorMixer(nodeColor[p], Color.Black);
                else
                    nodeColor[p] = Color.Gray;
            }

            foreach (Point p in policyInput)
            {
                if (nodeColor.Keys.Contains(p))
                    nodeColor[p] = GraphicUtils.colorMixer(nodeColor[p], Color.Yellow);
                else
                    nodeColor[p] = Color.Yellow;
            }
            foreach (Point p in additionalMarks)
            {
                if (nodeColor.Keys.Contains(p))
                    nodeColor[p] = GraphicUtils.colorMixer(nodeColor[p], Color.Green);
                else
                    nodeColor[p] = Color.Green;
            }
            foreach (Point p in pursuers)
            {
                if (nodeColor.Keys.Contains(p))
                    nodeColor[p] = GraphicUtils.colorMixer(nodeColor[p], Color.Red);
                else
                    nodeColor[p] = Color.Red;
            }
            foreach (Point p in evaders)
            {
                if (nodeColor.Keys.Contains(p))
                    nodeColor[p] = GraphicUtils.colorMixer(nodeColor[p], Color.Blue);
                else
                    nodeColor[p] = Color.Blue;
            }

            if (coloredLists != null)
            {
                foreach (var l in coloredLists)
                {
                    foreach (var p in l.Item2)
                    {
                        if (nodeColor.Keys.Contains(p))
                            nodeColor[p] = GraphicUtils.colorMixer(nodeColor[p], l.Item1);
                        else
                            nodeColor[p] = l.Item1;
                    }
                }
            }

            // reset View.ColoredCells's colors :
            foreach (var c in nodeColor)
            {
                if (!View.ColoredCells.Keys.Contains(c.Key))
                    View.ColoredCells.Add(c.Key, new NodeDisplay(c.Value, ""));
                else
                    View.ColoredCells[c.Key].c = c.Value;
            }
            foreach(var c in View.ColoredCells)
            {
                if (!nodeColor.Keys.Contains(c.Key))
                    c.Value.c = Color.Transparent;
            }

          
            View.Invalidate();
        }
        public void setNodeType(Point nodeID, NodeType t)
        {
            Graph.setNodeType(nodeID, t);

            if (t == NodeType.Normal)
            {
                View.ColoredCells.Remove(nodeID);
                return;
            }

            switch(t)
            {
                case NodeType.Sink:
                    nodeText[nodeID] = "S";
                    break;
                case NodeType.Target:
                    nodeText[nodeID] = "T:\n" + nodeID.X.ToString() + "," + nodeID.Y.ToString();
                    break;
                case NodeType.Blocked:
                    nodeColor[nodeID] = Color.Black;
                    break;
            }

            if (!nodeColor.Keys.Contains(nodeID))
                nodeColor[nodeID] = Color.Transparent;
            if (!nodeText.Keys.Contains(nodeID))
                nodeText[nodeID] = "";
            View.ColoredCells[nodeID]= new NodeDisplay(nodeColor[nodeID], nodeText[nodeID]);
            View.Invalidate();

            
        }
        public void resize(Size newSize, bool reset)
        {
            View.resizeGrid((uint)newSize.Width, (uint)newSize.Height, 32, !reset);
            Graph.resizeGrid((uint)newSize.Width, (uint)newSize.Height, reset);
        }

        public GoE.GridGameGraphViewer View { get; set; }
        public GridGameGraph Graph { get; private set; }
        private Dictionary<Point, string> nodeText = new Dictionary<Point, string>();
        private Dictionary<Point, Color> nodeColor = new Dictionary<Point, Color>();
    }
}
