using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using GoE.GameLogic;
using GoE.Utils;

namespace GoE
{



    public class GridGameGraph : GameGraph<Point>
    {
        public static Point ILLEGAL_NODE_ID { get { return new Point(-1, -1); }  }

        public bool isOnGrid(Point p)
        {
            return p.X >= 0 && p.Y >= 0 && p.X < widthCellCount && p.Y < heightCellCount;
        }
        /// <summary>
        /// utility function to get adjacent nodes in a grid.
        /// </summary>
        /// <param name="onlyGreater">
        /// if true, returns only up to 2 points - those with coordinates greater than p
        /// </param>
        /// <returns>
        /// returns up to 4 points, for each adjacent node in the drig (no coordinate is smaller than 0, or >= from grid size)
        /// </returns>
        public static List<Point> getAdjacentIDs(Point p, Size gridSize, bool onlyGreater)
        {
            List<Point> res = new List<Point>();
            if (onlyGreater && p.X > 0)
                res.Add(new Point(p.X - 1, p.Y));
            if ((p.X + 1) < gridSize.Width)
                res.Add(new Point(p.X + 1, p.Y));
            if (onlyGreater && p.Y > 0)
                res.Add(new Point(p.X, p.Y - 1));
            if ((p.Y + 1) < gridSize.Height)
                res.Add(new Point(p.X, p.Y + 1));

            return res;
        }
        public List<Point> getAdjacentIDs(Point p)
        {
            List<Point> res = new List<Point>();
            if (p.X > 0)
                res.Add(new Point(p.X - 1, p.Y));
            if ((p.X + 1) < widthCellCount)
                res.Add(new Point(p.X + 1, p.Y));
            if (p.Y > 0)
                res.Add(new Point(p.X, p.Y - 1));
            if ((p.Y + 1) < heightCellCount)
                res.Add(new Point(p.X, p.Y + 1));

            return res;
        }

        public GridGameGraph() 
        {
            widthCellCount = heightCellCount = 0;
        }
        public GridGameGraph(string graphFile)
        {
            base.deserialize(File.ReadAllLines(FileUtils.TryFindingFile(graphFile)));
        }
        public UInt32 WidthCellCount 
        {
            get {return widthCellCount;} 
        }
        public UInt32 HeightCellCount 
        { 
            get { return heightCellCount; } 
        }

        private enum LineDir
        {
            UpLeft, // -X-Y
            UpRight, // +X-Y
            DownLeft, // -X+Y
            DownRight // +X+Y
        }


        // serves getNodesInDistance()
        /// <summary>
        /// if line is a point, it will be added only 
        /// </summary>
        /// <param name="dest">
        /// where points will be added to
        /// </param>
        /// <param name="lineStartEnd">
        /// Item1 - line start
        /// Item2 - line end
        /// </param>
        /// <param name="includeEndPoint">
        /// include/exclude lineStartEnd.Item2
        /// </param>
        private void addLine(List<Point> dest, Tuple<Point,Point> lineStartEnd, bool includeEndPoint)
        {
            if (lineStartEnd == null)
                return;

            int xDiff = lineStartEnd.Item2.X - lineStartEnd.Item1.X;
            int yDiff = lineStartEnd.Item2.Y - lineStartEnd.Item1.Y;

            int y, x;

            y = lineStartEnd.Item1.Y;

            if (xDiff == 0)
            {
                if (yDiff != 0)
                    yDiff /= Math.Abs(yDiff);
                while (y != lineStartEnd.Item2.Y)
                {
                    dest.Add(new Point(lineStartEnd.Item1.X, y));
                    y += yDiff;
                }

                if (includeEndPoint)
                    dest.Add(lineStartEnd.Item2);
                return;
            }

            yDiff /= Math.Abs(xDiff);
            xDiff /= Math.Abs(xDiff);

            x = lineStartEnd.Item1.X;
            
            while(x != lineStartEnd.Item2.X)
            {
                dest.Add(new Point(x, y));
                x += xDiff;
                y += yDiff;
            }

            if (includeEndPoint)
                dest.Add(lineStartEnd.Item2);
        }

        // serves shearLineInGrid()
        int distToY(Point start, Point dir, int y)
        {
            return (y - start.Y) / dir.Y;
        }
        int distToX(Point start, Point dir, int x)
        {
            return (x - start.X) / dir.X;
        }        

        // serves getNodesInDistance()
        private Tuple<Point,Point> shearLineInGrid(Point start, int dist, LineDir d)
        {
            int minX = 0;// Math.Max(0, start.X);
            int minY = 0;// Math.Max(0, start.Y);
            int maxX = (int)widthCellCount-1;// Math.Min((int)widthCellCount-1, start.X + dist);
            int maxY = (int)heightCellCount-1; //Math.Min((int)heightCellCount-1, start.Y + dist);

            Point dir,end;
            int s = 0;
            switch(d)
            {
                case LineDir.DownRight:
                    dir = new Point(1, 1);
                    break;
                case LineDir.UpLeft:
                    dir = new Point(-1, -1);
                    break;
                case LineDir.DownLeft:
                    dir = new Point(-1, 1);
                    break;
                case LineDir.UpRight:
                    dir = new Point(1, -1);
                    break;
                default:
                    throw new Exception("bad enum LineDir value");
            }
            
            int x1,x2,y1,y2;

            if(dir.X > 0){
                x1 = minX; x2 = maxX;
            }
            else{
                x2 = minX; x1 = maxX;
            }
            if (dir.Y > 0) {
                y1 = minY; y2 = maxY;
            }
            else{
                y2 = minY; y1 = maxY;
            }

            s = Math.Max(s, Math.Max(distToX(start, dir, x1), distToY(start, dir, y1)));
            start = start.add(dir.mult(s));
            dist -= s;
            s = Math.Min(dist, Math.Min(distToX(start, dir, x2), distToY(start, dir, y2)));
            end = start.add(dir.mult(s));

            /*
            Point end;
            int cut;
            switch(dir)
            {
                case LineDir.UpLeft:
                    end = new Point(start.X - dist, start.Y - dist);
                    cut = Math.Max(0, Math.Max(start.X - maxX, start.Y - maxY));
                    start = start.add(cut, -cut);
                    cut = Math.Max(0, Math.Max(minX - end.X, minY - end.Y));
                    end = end.add(cut, cut);
                    break;
                case LineDir.UpRight:
                    end = new Point(start.X + dist, start.Y - dist);
                    cut = Math.Max(0, Math.Max(maxX - start.X, start.Y - maxY));
                    start = start.add(cut, cut);
                    cut = Math.Max(0, Math.Max(end.X - maxX, minY - end.Y));
                    end = end.add(cut, cut);
                    break;
                case LineDir.DownLeft:
                    end = new Point(start.X - dist, start.Y + dist);
                    cut = Math.Max(0, Math.Max(start.X - maxX, minY - start.Y));
                    start = start.add(cut, cut);
                    cut = Math.Max(0,Math.Max(minX - end.X, end.Y - maxY));
                    end = end.add(cut, cut);
                    break;
                case LineDir.DownRight:
                    end = new Point(start.X + dist, start.Y + dist);
                    cut = Math.Max(0, Math.Max(minX - start.X, minY - start.Y));
                    start = start.add(cut, cut);
                    cut = Math.Max(0, Math.Max(end.X - maxX, end.Y - maxY));
                    end = end.add(cut, cut);
                    break;
                default:
                    throw new Exception("bad enum LineDir value");
            }*/

            if (start.X < 0 || start.X > widthCellCount - 1 ||
                start.Y < 0 || start.Y > heightCellCount - 1 ||
                end.X < 0 || end.X > widthCellCount - 1 ||
                end.Y < 0 || end.Y > heightCellCount - 1)
                return null;
            return Tuple.Create(start, end);
        }


        public override List<Point> getNodesInDistance(Point origin, double distD)
        {
            int dist = (int)Math.Round(distD);
            List<Point> res = new List<Point>(4 * dist);
            
            Point b = new Point(origin.X, origin.Y - dist);
            Point r = new Point(origin.X + dist, origin.Y);
            Point l = new Point(origin.X - dist, origin.Y);
            Point t = new Point(origin.X, origin.Y + dist);
            // note: method still untested!
            addLine(res, shearLineInGrid(b, dist, LineDir.DownLeft),false);
            addLine(res, shearLineInGrid(l, dist, LineDir.DownRight), false);
            addLine(res, shearLineInGrid(t, dist, LineDir.UpRight), false);
            addLine(res, shearLineInGrid(r, dist, LineDir.UpLeft), false);
            return res;
        }

        public override List<Point> getNodesWithinDistance(Point origin, double maxDist)
        {
            List<Point> res = new List<Point>((int)(2 * ((int)Math.Round(maxDist)) * (maxDist + 1) + 1));
            getNodesWithinDistance(origin, maxDist, res);
            return res;
        }
        
        /// <summary>
        /// adds point to dest
        /// </summary>
        public virtual void getNodesWithinDistance(Point origin, double maxDist, HashSet<Point> dest)
        {
            getNodesWithinDistance(origin, maxDist, dest);
        }
        public void getNodesWithinDistance(Point origin, double maxDist, ICollection<Point> res)
        {
            int dist = (int)Math.Round(maxDist);
            //List<Point> res = new List<Point>((int)(2 * dist * (maxDist + 1) + 1));

            
            int minY = Math.Max(0, origin.Y - dist);
            int maxY = Math.Min((int)heightCellCount-1, origin.Y + dist);

            if (maxDist < 25) // not sure 25 is the magic number, but obviously the normal flow is inefficient for small values
            {
                for(int y = minY; y < origin.Y; ++y)
                {
                    int minX = origin.X - (y - origin.Y + dist);
                    int maxX = origin.X + (y - origin.Y + dist);
                    minX = Math.Max(minX,0);
                    maxX = Math.Min(maxX,(int)widthCellCount-1);
                    for(int x = minX; x <= maxX; ++x)
                        res.Add(new Point(x,y));
                }
                for(int y = origin.Y; y <= maxY; ++y)
                {
                    int minX = origin.X + y - origin.Y - dist;
                    int maxX = origin.X - y + origin.Y + dist;
                    minX = Math.Max(minX,0);
                    maxX = Math.Min(maxX,(int)widthCellCount-1);
                    for(int x = minX; x <= maxX; ++x)
                        res.Add(new Point(x,y));
                }
                return;
            } // if small maxDist value

            // TODO: apparently buggy for dist=38
            // efficient implementation : 

            List<Point> from = new List<Point>(2 * dist);
            List<Point> to = new List<Point>(2 * dist);
            
            Point b = new Point(origin.X, origin.Y - dist);
            //Point r = new Point(origin.X + dist, origin.Y);
            //Point l = new Point(origin.X - dist, origin.Y);
            Point t = new Point(origin.X, origin.Y + dist);

            Point l;

            addLine(from, shearLineInGrid(b, dist, LineDir.DownLeft), false);
            if (origin.X - dist < 0) // the lines were cut by the right side of the grid
            {
                if (from.Count == 0)
                    l = new Point(0, minY);
                else
                    l = from.Last().add(-1, 1);
                addLine(from, Tuple.Create(l, new Point(0, origin.Y)), false);
            }
            var lastLine = shearLineInGrid(t, dist, LineDir.UpLeft);
            if (lastLine != null)
            {
                if (origin.X - dist < 0)
                    addLine(from, Tuple.Create(new Point(0, origin.Y), lastLine.Item2), false);
                addLine(from, lastLine, true);
            }
            else
                addLine(from, Tuple.Create(new Point(0, origin.Y),
                                           new Point(0, maxY)), true);
            


            addLine(to, shearLineInGrid(b, dist, LineDir.DownRight), false);
            if (origin.X + dist > (int)widthCellCount - 1) // the lines were cut by the right side of the grid
            {
                if (to.Count == 0)
                    l = new Point((int)widthCellCount - 1, minY);
                else
                    l = to.Last().add(1, 1);
                addLine(to, Tuple.Create(l, new Point((int)widthCellCount - 1, origin.Y)), false);
            }
            lastLine = shearLineInGrid(t, dist, LineDir.UpRight);
            if (lastLine != null)
            {
                if (origin.X + dist > (int)widthCellCount - 1)
                    addLine(to, Tuple.Create(new Point((int)widthCellCount - 1, origin.Y), lastLine.Item2), false);
                addLine(to, lastLine, true);
            }
            else
                addLine(to, Tuple.Create(new Point((int)widthCellCount - 1, origin.Y),
                                         new Point((int)widthCellCount - 1, maxY)), true);
            
            for (int y = 0; y < from.Count; ++y )
                for (int x = from[y].X; x <= to[y].X; ++x)
                    res.Add(new Point(x, from[y].Y));
        }

        /// <summary>
        /// probably reduandent because it's a grid and every node in range is in the graph
        /// </summary>
        public override Dictionary<Point, Node<Point>> Nodes
        {
            get { return nodes; }
        }

        public override Dictionary<Point, Dictionary<Point, Double>> Edges
        {
            get { return edges; }
        }

        /// <summary>
        /// we don't allow explicit removal of nodes, but setting a node type to blocked is equivalent
        /// </summary>
        /// <param name="nodID"></param>
        /// <param name="newType"></param>
        public void setNodeType(Point nodeID, NodeType newType)
        {
            if (!nodes.ContainsKey(nodeID))
                return;
            if (nodes[nodeID].t != NodeType.Blocked && newType == NodeType.Blocked)
            {
                // remove all edges connected to nodeID
                List<Point> nodesToRemove = edges[nodeID].Keys.ToList();
                foreach (Point dest in nodesToRemove)
                {
                    Edges[nodeID].Remove(dest);
                    Edges[dest].Remove(nodeID);
                }
            }
            else if(nodes[nodeID].t == NodeType.Blocked && newType != NodeType.Blocked)
            {
                // recreate edges to nodeID
                List<Point> adj = getAdjacentIDs(nodeID, new Size((int)widthCellCount, (int)heightCellCount), false);
                foreach(Point p in adj)
                    edges[p][nodeID] = edges[nodeID][p] = 1;
            }

            nodes[nodeID].t = newType;
            isDirty = true;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="WidthCellCount"></param>
        /// <param name="HeightCellCount"></param>
        /// <param name="resetGrid">
        /// if true, grid is rebuilt - all nodes are of type normal and every possible edge (in the grid)
        /// is added.
        /// Otherwise, nodes keep their type, and blocked nodes still won't have edges to adjacent nodes
        /// </param>
        public void resizeGrid(UInt32 WidthCellCount, UInt32 HeightCellCount, bool resetGrid)
        {
            isDirty = true;
            if(resetGrid)
            {
                nodes = new Dictionary<Point, Node<Point>>();
                edges = new Dictionary<Point, Dictionary<Point, double>>();
                for(int x = 0; x < WidthCellCount; ++x)
                    for(int y = 0; y < HeightCellCount; ++y)
                    {
                        Point id = new Point(x,y);
                        nodes[id] = new Node<Point>(id, NodeType.Normal);
                        edges[id] = new Dictionary<Point,double>();
                    }
            }
            else
            {
                for (int x = 0; x < WidthCellCount; ++x)
                    for (int y = 0; y < HeightCellCount; ++y)
                    {
                        Point id = new Point(x, y);
                        if (!nodes.Keys.Contains(id))
                            nodes[id] = new Node<Point>(id, NodeType.Normal);
                        if (!edges.Keys.Contains(id))
                            edges[id] = new Dictionary<Point, double>();
                    }
            }

            for (int x = 0; x < WidthCellCount; ++x)
                for (int y = 0; y < HeightCellCount; ++y)
                {
                    Point id = new Point(x, y);
                    if (Nodes[id].t == NodeType.Blocked)
                        continue;
                        
                    List<Point> adjacent = 
                        getAdjacentIDs(id, new Size((int)WidthCellCount, (int)HeightCellCount), true);
                        
                    foreach (Point a in adjacent)
                    {
                        if (nodes[a].t == NodeType.Blocked)
                            continue;
                            
                        edges[id][a] = edges[a][id] = 1;
                    }
                }
            
            this.widthCellCount = WidthCellCount;
            this.heightCellCount = HeightCellCount;
        }
        public override double getMinDistance(Point n1, Point n2)
        {
            return Math.Abs(n1.X - n2.X) + Math.Abs(n1.Y - n2.Y);
        }
        public double getMinDistance(Point n1, PointF n2)
        {
            return Math.Abs(n1.X - n2.X) + Math.Abs(n1.Y - n2.Y);
        }

        protected override void deserializeEx(string[] remainingLines)
        {
            widthCellCount = heightCellCount = 0;
            foreach (var n in nodes)
            {
                widthCellCount = Math.Max(widthCellCount, (UInt32)n.Key.X);
                heightCellCount = Math.Max(heightCellCount, (UInt32)n.Key.Y);
            }
            ++widthCellCount; // width/height are 1 + max node ID
            ++heightCellCount;
        }
        protected override string serializeID(Point nid)
        {
            return nid.X.ToString() + "," + nid.Y.ToString();
        }

        protected override Point deserializeID(string seralizedNode)
        {
            Point res = new Point();
            int sep = seralizedNode.LastIndexOf(',');
            res.X = Int32.Parse(seralizedNode.Substring(0, sep));
            res.Y = Int32.Parse(seralizedNode.Substring(sep+1));
            return res;
        }
        override protected bool isGraphDirty()
        {
            bool wasDirty = isDirty;
            isDirty = false;
            return wasDirty;
        }
        private Dictionary<Point, Node<Point>> nodes = new Dictionary<Point,Node<Point>>();
        private Dictionary<Point, Dictionary<Point, Double>> edges = new Dictionary<Point, Dictionary<Point, double>>();
        private UInt32 widthCellCount, heightCellCount;
        private bool isDirty = true;
    }
}

