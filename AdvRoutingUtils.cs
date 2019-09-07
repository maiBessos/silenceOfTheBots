using GoE.GameLogic;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static GoE.GameLogic.Utils;

namespace GoE.AdvRouting
{
#if MONO
    static class MonoUtils
    {
        public static float distance2F(this PointF p1, PointF p2)
        {
            return (float)Math.Pow(p1.X - p2.X, 2) + (float)Math.Pow(p1.Y - p2.Y, 2);
        }
        public static PointF addF(this PointF p, float x, float y)
        {
            p.X += x;
            p.Y += y;
            return p;
        }
        public static PointF subtruct(this PointF p1, float x, float y)
        {
            p1.X -= x;
            p1.Y -= y;
            return p1;
        }
    public static PointF add(this PointF p1, PointF p2)
        {
            p1.X += p2.X;
            p1.Y += p2.Y;
            return p1;
        }
    }
#endif

    class SquareRegion //: IComparable<SquareRegion>
    {
        public PointF centerP;
        public float edgeLen;

        public PointF v1 { get { return centerP.addF(-edgeLen / 2, -edgeLen / 2); } }
        public PointF v2 { get { return centerP.addF(edgeLen / 2, -edgeLen / 2); } }
        public PointF v3 { get { return centerP.addF(edgeLen / 2, edgeLen / 2); } }
        public PointF v4 { get { return centerP.addF(-edgeLen / 2, edgeLen / 2); } }

        public List<Tuple<PointF, bool>> squareWallPoints = new List<Tuple<PointF, bool>>(); // for each point on this square's border, this tells which points also belong to inner child squares(bool=false) and which aren't (bool=true)

        //public int CompareTo(SquareRegion other)
        //{
        //    // we never generate different sqcycles with the same point, so it may be used to compare
        //    //int v = centerP.X.CompareTo(other.centerP.X);
        //    //return v != 0 ? v : (centerP.Y.CompareTo(other.centerP.Y));
        //    return centerP.X.CompareTo(other.centerP.X) * 2 + centerP.Y.CompareTo(other.centerP.Y);
        //}
        public override bool Equals(object obj)
        {
            return centerP.Equals(((SquareRegion)obj).centerP);
        }
        public override int GetHashCode()
        {
            return centerP.GetHashCode();
        }
    }

    struct WatchedPoint : IComparable<WatchedPoint>
    {
        public PointF p;
        public int visitsNumber; //how many times this point was visited

        public int CompareTo(WatchedPoint other)
        {
            return visitsNumber.CompareTo(other.visitsNumber) * 4 +
                p.X.CompareTo(other.p.X) * 2 +
                p.Y.CompareTo(other.p.Y);
        }
    }
    
    static class Utils
    {
        
        /// <summary>
        /// assuming points on the boundary of 's' are already covered, this function generates 
        /// points that cover it's internal region
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public static List<PointF> coverInnerRegion(SquareRegion s)
        {
            List<PointF> res = new List<PointF>();

            //float sqrt15 = (float)Math.Sqrt(1.5);
            PointF centerAxisPoint = s.centerP.subtruct(0, s.edgeLen/2);

            int lineNum = 0;

            while (true)
            {
                PointF currp = centerAxisPoint;
                if (lineNum % 2 == 1)
                    currp.X += sqrt3 / 2;

                // begin with Y of 
                do
                {
                    res.Add(currp);
                    currp.X += sqrt3;
                }
                while (currp.X < s.centerP.X+s.edgeLen / 2);

                currp.Y = centerAxisPoint.Y;
                do
                {
                    res.Add(currp);
                    currp.X -= sqrt3;
                }
                while (currp.X > s.centerP.X - s.edgeLen / 2);


                if (centerAxisPoint.Y >= s.centerP.Y + s.edgeLen / 2)
                    break;
                
                centerAxisPoint.Y += 1.5f; // when moving the prev. circle center sqrt3 / 2 on one axis and 1.5 on another axis, distance is sqrt(3)
                ++lineNum;
            }
            return res;
        }
        /// <summary>
        /// generates centers of points that when visited, every neighbor of p (with distance 1.0) will be covered
        /// </summary>
        /// <param name="p"></param>
        /// <returns></returns>
        public static List<PointF> getCoveringNeighbors(PointF t)
        {

            return new List<PointF>()
                { new PointF(sqrt3 / 2, 1.5f).add(t) ,
                  new PointF(-sqrt3 / 2, 1.5f).add(t) ,
                  new PointF(sqrt3 / 2, -1.5f).add(t) ,
                  new PointF(-sqrt3 / 2, -1.5f).add(t) ,
                  new PointF(sqrt3, 0).add(t) ,
                  new PointF(-sqrt3, 0).add(t) };
        }
        const float sqrt3 = 1.73205080f;

        /// <summary>
        /// generates centrs of cicrles with radius 1.0 that cover a circle at center (0,0) with radius 'rad' 
        /// </summary>
        /// <param name="rad"></param>
        /// <returns></returns>
        public static List<PointF> getCoveringPoints(float rad)
        {

            HashSet<Point> antiDupe = new HashSet<Point>(); // fixme remove
           
            List<PointF> res = new List<PointF>();
            
            //float sqrt15 = (float)Math.Sqrt(1.5);
            PointF diameterPoint = new PointF(0, -rad);

            int lineNum = 0;

            while (true)
            {
                PointF currp = diameterPoint;
                float startX = currp.X;
                if (lineNum % 2 == 1)
                {
                    currp.X += sqrt3 / 2;
                    startX += sqrt3 / 2;
                }
                
                do
                {
                    res.Add(currp);
                    currp.X += sqrt3;
                }
                while (currp.distance2F(new PointF(0, 0)) < rad * rad);

                currp.Y = diameterPoint.Y;
                currp.X = startX - sqrt3;
                do
                {
                    res.Add(currp);
                    currp.X -= sqrt3;
                }
                while (currp.distance2F(new PointF(0, 0)) < rad * rad);


                if (diameterPoint.Y >= rad)
                    break;
                
                diameterPoint.Y += 1.5f; // when moving the prev. circle center sqrt3 / 2 on one axis and 1.5 on another axis, distance is sqrt(3)
                ++lineNum;
            }

            return res;
        }
        public static List<PointF> verticalWall(float edgeLen, PointF startP)
        {
            float sqrt3 = (float)Math.Sqrt(3);
            List<PointF> res = new List<PointF>();
            PointF tmpPoint = startP;
            while (tmpPoint.Y < startP.Y + edgeLen)
            {
                res.Add(tmpPoint);
                tmpPoint.Y += sqrt3;
            }
            tmpPoint.Y = startP.Y + edgeLen;
            res.Add(tmpPoint);
            return res;
        }
       
        public static List<PointF> horizontalWall(float edgeLen, PointF startP, float jumpSize = 0)
        {
            if(jumpSize == 0)
                jumpSize = (float)Math.Sqrt(3);
            List<PointF> res = new List<PointF>();
            PointF tmpPoint = startP;
            while (tmpPoint.X < startP.X + edgeLen)
            {
                res.Add(tmpPoint);
                tmpPoint.X += jumpSize;
            }
            tmpPoint.X = startP.X + edgeLen;
            res.Add(tmpPoint);
            return res;
        }

        public static List<PointF> squareWall(SquareRegion r)
        {
            List<PointF> res = horizontalWall(r.edgeLen, new PointF(r.centerP.X - r.edgeLen / 2, r.centerP.Y - r.edgeLen / 2));
            res.AddRange(horizontalWall(r.edgeLen, new PointF(r.centerP.X - r.edgeLen / 2, r.centerP.Y + r.edgeLen / 2)));
            res.AddRange(verticalWall(r.edgeLen, new PointF(r.centerP.X - r.edgeLen / 2, r.centerP.Y - r.edgeLen / 2)));
            res.AddRange(verticalWall(r.edgeLen, new PointF(r.centerP.X + r.edgeLen / 2, r.centerP.Y - r.edgeLen / 2)));
            return res;
        }

        public static bool isPointOnSquare(SquareRegion sq, PointF p)
        {
            if (Math.Abs(p.X - (sq.centerP.X - sq.edgeLen / 2)) < 0.001 ||
                Math.Abs(p.X - (sq.centerP.X + sq.edgeLen / 2)) < 0.001)
                return p.Y >= sq.centerP.Y - sq.edgeLen / 2 - 0.001 &&
                       p.Y <= sq.centerP.Y + sq.edgeLen / 2 + 0.001;

            if (Math.Abs(p.Y - (sq.centerP.Y - sq.edgeLen / 2)) < 0.001 ||
                Math.Abs(p.Y - (sq.centerP.Y + sq.edgeLen / 2)) < 0.001)
                return p.X >= sq.centerP.X - sq.edgeLen / 2 - 0.001 &&
                       p.X <= sq.centerP.X + sq.edgeLen / 2 + 0.001;

            return false;

            //SegmentF s1 = new SegmentF() { start = sq.v1, end = sq.v2 };
            //SegmentF s2 = new SegmentF() { start = sq.v2, end = sq.v3 };
            //SegmentF s3 = new SegmentF() { start = sq.v3, end = sq.v4 };
            //SegmentF s4 = new SegmentF() { start = sq.v4, end = sq.v1 };
            //if(s1.distance())
        }

        public struct AddedPoints
        {
            public List<PointF> newPoints;
            public List<PointF> parentPoints;
        }

        /// <summary>
        /// same as squareWall(r), but omits points that are already fully covered by squareWall(parentSquare)
        /// </summary>
        /// <param name="r"></param>
        /// <param name="parentSquare"></param>
        /// <param name="markUsedInParent">
        /// if true, return value will also include 'parentPoints', and these same points will be marked 'false' in 'parentSquare.squareWallPoints'
        /// </param>
        /// <returns></returns>
        public static AddedPoints squareWall(SquareRegion r, SquareRegion parentSquare, bool markUsedInParent = false)
        {
            AddedPoints res = new AddedPoints();
            res.newPoints = new List<PointF>();
            if(markUsedInParent)
                res.parentPoints = new List<PointF>();

            int child_dx = (r.centerP.X < parentSquare.centerP.X) ? (-1) : (1);
            int child_dy = (r.centerP.Y < parentSquare.centerP.Y) ? (-1) : (1);

            //foreach (var p in allPoints)
            //    if (isPointOnSquare(parentSquare, p))
            //        res.parentPoints.Add(p);
            if (markUsedInParent) // for each point "moved" from parent to child, we mark that the parent no longer owns it. note that several children may take the same point
                for (int i = 0; i < parentSquare.squareWallPoints.Count; ++i)
                    if (isPointOnSquare(parentSquare, parentSquare.squareWallPoints[i].Item1))
                    {
                        res.parentPoints.Add(parentSquare.squareWallPoints[i].Item1);
                        parentSquare.squareWallPoints[i] = new Tuple<PointF, bool>(parentSquare.squareWallPoints[i].Item1, false);
                    }

            if (child_dx == -1)
                res.newPoints.AddRange(verticalWall(r.edgeLen, new PointF(r.centerP.X + r.edgeLen / 2, r.centerP.Y - r.edgeLen / 2)));
            else
                res.newPoints.AddRange(verticalWall(r.edgeLen, new PointF(r.centerP.X - r.edgeLen / 2, r.centerP.Y - r.edgeLen / 2)));
            
            if(child_dy == -1)
                res.newPoints.AddRange(horizontalWall(r.edgeLen, new PointF(r.centerP.X - r.edgeLen / 2, r.centerP.Y + r.edgeLen / 2)));
            else
                res.newPoints.AddRange(horizontalWall(r.edgeLen, new PointF(r.centerP.X - r.edgeLen / 2, r.centerP.Y - r.edgeLen / 2)));
            
            return res;
        }

        public static PointF nextCenter(float edgeLen, PointF prevCenter, int dx, int dy)
        {
            return new PointF(prevCenter.X + dx * edgeLen / 4, prevCenter.Y + dy * edgeLen / 4);
        }
        public static List<PointF> nextCenters(SquareRegion r)
        {
            List<PointF> res = new List<PointF>();
            res.Add(nextCenter(r.edgeLen, r.centerP, -1, -1));
            res.Add(nextCenter(r.edgeLen, r.centerP, 1, -1));
            res.Add(nextCenter(r.edgeLen, r.centerP, -1, 1));
            res.Add(nextCenter(r.edgeLen, r.centerP, 1, 1));
            return res;
        }

        /// <summary>
        /// returns the sub-region square that contains 'pointOnBorder'
        /// </summary>
        /// <param name="r"></param>
        /// <param name="pointOnBorder"></param>
        /// <returns></returns>
        public static SquareRegion getIncludingDividedSquare(SquareRegion r, PointF pointOnBorder)
        {
            int including_dx = (pointOnBorder.X < r.centerP.X) ? (-1) : (1);
            int including_dy = (pointOnBorder.Y < r.centerP.Y) ? (-1) : (1);
            return new SquareRegion() { centerP = nextCenter(r.edgeLen, r.centerP, including_dx, including_dy), edgeLen = r.edgeLen / 2 };
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="r"></param>
        /// <param name="pointOnBorder"></param>
        /// <param name="includingSquareRegionIdx">
        /// coresponds return value. tells region that includes 'pointOnBorder'
        /// </param>
        /// <returns></returns>
        public static List<SquareRegion> splitRegion(SquareRegion r)
        {
            List<SquareRegion> res = new List<SquareRegion>();
            List<PointF> centers = nextCenters(r);
            foreach (var c in centers)
                res.Add(new SquareRegion() { centerP = c, edgeLen = r.edgeLen / 2 });

            return res;
        }

       
    }

}
