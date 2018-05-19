

using GoE.Utils.Algorithms;
using System;
using System.Drawing;
using System.Collections.Generic;
using GoE.GameLogic;
using System.Linq;

namespace GoE.Utils
{
    namespace PointDataStructs
    {

				/*
		public class CoarsePointGrid
        {
            public Rectangle FullArea;
            public int AreaWidth { get; protected set; }
            public int AreaHeight { get; protected set; }
            public  CoarsePointGrid(Rectangle fullArea, int areaWidth, int areaHeight)
            {
                FullArea = fullArea;
                AreaWidth = areaWidth;
                AreaHeight = areaHeight;
                Areas = new List<Point>[(int)Math.Ceiling(((float)fullArea.Width) / areaWidth), (int)Math.Ceiling(((float)FullArea.Height / areaHeight))];
                for (int x = 0; x < fullArea.Width; x += AreaWidth)
                    for (int y = 0; y < fullArea.Height; y += areaHeight)
                        Areas[x/ areaWidth, y/ areaHeight] = new List<Point>();
            }

            public void removePoint(Point p)
            {
                var area = Areas[p.X / AreaWidth, p.Y / AreaHeight];
                for (int i = 0; i < area.Count;++i)
                    if (area[i] == p)
                    {
                        area[i] = area.Last();
                        area.RemoveAt(area.Count-1);
                        break;
                    }
            }
            public void addPoint(Point p)
            {
                Areas[p.X / AreaWidth, p.Y / AreaHeight].Add(p);
            }
            /// <summary>
            /// retruns points in the grid with man distance <= 'dist'
            /// </summary>
            /// <param name="from"></param>
            /// <returns></returns>
            public List<Point> findPointsWithinManDistance(Point from, int dist)
            {
                List<Point> res = new List<Point>();
                int minX = Math.Max(0,(from.X - dist) / AreaWidth);
                int maxX = Math.Min((from.X + dist) / AreaWidth, Areas.GetLength(0)-1);
                int minY = Math.Max(0,(from.Y - dist) / AreaHeight);
                int maxY = Math.Min((from.Y + dist) / AreaHeight, Areas.GetLength(1)-1);
                for (int x = minX; x <= maxX; ++x)
                    for (int y = minY; y <= maxY; ++y)
                        foreach (Point cmp in Areas[x, y])
                            if (cmp.manDist(from) <= dist)
                                res.Add(cmp);
                return res;
            }
            List<Point>[,] Areas;
        }*/

		public class PointSet
        {
            List<List<Point>> points;
            int width;
			int minX;

            /// <summary>
            /// returns a raw structure that contains all the points in the set (some lists may be empty)
            /// </summary>
            public List<List<Point>> AllPoints { get { return points; } }
            
			public List<Point> findNearest(Point p, float radius)
            {
				List<Point> res = new List<Point>();
                
				float radius2 = radius*radius;
                int minCellX = Math.Max(0,(int)((p.X - radius - minX) / width));
				minCellX = Math.Min(minCellX, points.Count);
                int maxCellX = Math.Max(0,(int)((p.X + radius - minX) / width));
				maxCellX = Math.Min(maxCellX, points.Count);
				for(int cellX = minCellX; cellX <= maxCellX; ++cellX)
                {
					Exceptions.ConditionalTryCatch<Exception>(() =>
					{
						List<Point> relevantList = points[cellX];
						foreach(var cp in relevantList)
							if(cp.distance2(p) <= radius2)
								res.Add(cp);
					});
				}

				return res;
            }
            public void removeDupliacates()
            {
                Count = 0;
                for (int i = 0; i < points.Count; ++i)
                {
                    List<Point> prevList = points[i];

                    if (prevList.Count > 0)
                    {
                        prevList.Sort(new Comparison<Point>((p1, p2) => p1.Y.CompareTo(p2.Y)));
                        List<Point> newPointsList = new List<Point>();
                        newPointsList.Add(prevList[0]);
                        for (int j = 1; j < prevList.Count; ++j)
                            if (prevList[j] != prevList[j - 1])
                                newPointsList.Add(prevList[j]);

                        points[i] = newPointsList;
                        Count += newPointsList.Count;
                    }
                }
            }
            public PointSet(IEnumerable<Point> allPoints)
            {
                Count = allPoints.Count();
                if (Count == 0)
                {
                    minX = int.MaxValue;
                    width = 0;
                    points = new List<List<Point>>();
                    return;
                }

                minX = int.MaxValue;
                int maxX = int.MinValue;
                foreach (Point p in allPoints)
                {
                    minX = Math.Min(minX, p.X);
                    maxX = Math.Max(maxX, p.X);
                }

                int xCellCount = (int)Math.Ceiling(Math.Sqrt(allPoints.Count()));
                int yCellCount = allPoints.Count() / xCellCount;

                this.points = new List<List<Point>>(xCellCount);
                for (int i = 0; i < xCellCount; ++i)
                    this.points.Add(new List<Point>(yCellCount));

                width = (int)Math.Ceiling(Math.Max(1, (maxX + 1.0f - minX) / xCellCount));

                foreach (Point p in allPoints)
                    points[(int)((p.X - minX) / width)].Add(p);

            }
            public int Count { get; protected set; }
            public bool Contains(Point p)
            {
                int x = (int)(p.X - minX);
                if (x < 0)
                    return false;

                int cellX = (int)((p.X - minX) / width);
                if (cellX >= points.Count)
                    return false;

                List<Point> relevantList = points[cellX];
                for (int i = 0; i < relevantList.Count; ++i)
                    if (relevantList[i] == p)
                        return true;
                return false;
            }
            
            public bool Remove(Point p)
            {
                int x = (int)(p.X - minX);
                if (x < 0)
                    return false;

                int cellX = (int)((p.X - minX) / width);
                if (cellX >= points.Count)
                    return false;

                List<Point> relevantList = points[cellX];
                for (int i = 0; i < relevantList.Count; ++i)
                    if (relevantList[i] == p)
                    {
                        relevantList[i] = relevantList.Last();
                        relevantList.RemoveAt(relevantList.Count - 1);
                        --Count;
                        return true;
                    }
                return false;
            }
        }
				/*
		public class CoarsePointFGrid
        {
            public Rectangle FullArea;
            public int AreaWidth { get; protected set; }
            public int AreaHeight { get; protected set; }
            public  CoarsePointFGrid(Rectangle fullArea, int areaWidth, int areaHeight)
            {
                FullArea = fullArea;
                AreaWidth = areaWidth;
                AreaHeight = areaHeight;
                Areas = new List<PointF>[(int)Math.Ceiling(((float)fullArea.Width) / areaWidth), (int)Math.Ceiling(((float)FullArea.Height / areaHeight))];
                for (int x = 0; x < fullArea.Width; x += AreaWidth)
                    for (int y = 0; y < fullArea.Height; y += areaHeight)
                        Areas[x/ areaWidth, y/ areaHeight] = new List<PointF>();
            }

            public void removePoint(PointF p)
            {
                var area = Areas[p.X / AreaWidth, p.Y / AreaHeight];
                for (int i = 0; i < area.Count;++i)
                    if (area[i] == p)
                    {
                        area[i] = area.Last();
                        area.RemoveAt(area.Count-1);
                        break;
                    }
            }
            public void addPoint(PointF p)
            {
                Areas[p.X / AreaWidth, p.Y / AreaHeight].Add(p);
            }
            /// <summary>
            /// retruns points in the grid with man distance <= 'dist'
            /// </summary>
            /// <param name="from"></param>
            /// <returns></returns>
            public List<PointF> findPointsWithinManDistance(PointF from, int dist)
            {
                List<PointF> res = new List<PointF>();
                int minX = Math.Max(0,(from.X - dist) / AreaWidth);
                int maxX = Math.Min((from.X + dist) / AreaWidth, Areas.GetLength(0)-1);
                int minY = Math.Max(0,(from.Y - dist) / AreaHeight);
                int maxY = Math.Min((from.Y + dist) / AreaHeight, Areas.GetLength(1)-1);
                for (int x = minX; x <= maxX; ++x)
                    for (int y = minY; y <= maxY; ++y)
                        foreach (PointF cmp in Areas[x, y])
                            if (cmp.manDist(from) <= dist)
                                res.Add(cmp);
                return res;
            }
            List<PointF>[,] Areas;
        }*/

		public class PointFSet
        {
            List<List<PointF>> points;
            float width;
			float minX;

            /// <summary>
            /// returns a raw structure that contains all the points in the set (some lists may be empty)
            /// </summary>
            public List<List<PointF>> AllPoints { get { return points; } }
            
			public List<PointF> findNearest(PointF p, float radius)
            {
				List<PointF> res = new List<PointF>();
                
				float radius2 = radius*radius;
                int minCellX = Math.Max(0,(int)((p.X - radius - minX) / width));
				minCellX = Math.Min(minCellX, points.Count-1);
                int maxCellX = Math.Max(0,(int)((p.X + radius - minX) / width));
				maxCellX = Math.Min(maxCellX, points.Count-1);
				for(int cellX = minCellX; cellX <= maxCellX; ++cellX)
                {
					Exceptions.ConditionalTryCatch<Exception>(() =>
					{
						List<PointF> relevantList = points[cellX];
						foreach(var cp in relevantList)
							if(cp.distance2F(p) <= radius2)
								res.Add(cp);
					});
				}

				return res;
            }
            public void removeDupliacates()
            {
                Count = 0;
                for (int i = 0; i < points.Count; ++i)
                {
                    List<PointF> prevList = points[i];

                    if (prevList.Count > 0)
                    {
                        prevList.Sort(new Comparison<PointF>((p1, p2) => p1.Y.CompareTo(p2.Y)));
                        List<PointF> newPointsList = new List<PointF>();
                        newPointsList.Add(prevList[0]);
                        for (int j = 1; j < prevList.Count; ++j)
                            if (prevList[j] != prevList[j - 1])
                                newPointsList.Add(prevList[j]);

                        points[i] = newPointsList;
                        Count += newPointsList.Count;
                    }
                }
            }
            public PointFSet(IEnumerable<PointF> allPoints)
            {
                Count = allPoints.Count();
                if (Count == 0)
                {
                    minX = float.MaxValue;
                    width = 0;
                    points = new List<List<PointF>>();
                    return;
                }

                minX = float.MaxValue;
                float maxX = float.MinValue;
                foreach (PointF p in allPoints)
                {
                    minX = Math.Min(minX, p.X);
                    maxX = Math.Max(maxX, p.X);
                }

                int xCellCount = (int)Math.Ceiling(Math.Sqrt(allPoints.Count()));
                int yCellCount = allPoints.Count() / xCellCount;

                this.points = new List<List<PointF>>(xCellCount);
                for (int i = 0; i < xCellCount; ++i)
                    this.points.Add(new List<PointF>(yCellCount));

                width = (float)Math.Ceiling(Math.Max(1, (maxX + 1.0f - minX) / xCellCount));

                foreach (PointF p in allPoints)
                    points[(int)((p.X - minX) / width)].Add(p);

            }
            public int Count { get; protected set; }
            public bool Contains(PointF p)
            {
                int x = (int)(p.X - minX);
                if (x < 0)
                    return false;

                int cellX = (int)((p.X - minX) / width);
                if (cellX >= points.Count)
                    return false;

                List<PointF> relevantList = points[cellX];
                for (int i = 0; i < relevantList.Count; ++i)
                    if (relevantList[i] == p)
                        return true;
                return false;
            }
            
            public bool Remove(PointF p)
            {
                int x = (int)(p.X - minX);
                if (x < 0)
                    return false;

                int cellX = (int)((p.X - minX) / width);
                if (cellX >= points.Count)
                    return false;

                List<PointF> relevantList = points[cellX];
                for (int i = 0; i < relevantList.Count; ++i)
                    if (relevantList[i] == p)
                    {
                        relevantList[i] = relevantList.Last();
                        relevantList.RemoveAt(relevantList.Count - 1);
                        --Count;
                        return true;
                    }
                return false;
            }
        }
		 // foreach PointType in types
	}
}