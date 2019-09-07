using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GoE.Utils;
using GoE.Utils.Extensions;

using GoE.Utils.Algorithms;
using GoE.AdvRouting;

namespace GoE.GameLogic
{
    // TODO: utils is getting too big - separate to multiple files
    // TODO: replace all points in the software with more elaborate structure (e.g. Vectorf2D) to allow easier 
    // arithmetic

    public static class Utils
    {
        /// <summary>
        /// if val is NaN or infinite, return false
        /// </summary>
        /// <param name="val"></param>
        /// <returns></returns>
        public static bool isFinite(float val)
        {
            if (float.IsNaN(val) || float.IsInfinity(val))
                return false;
            return true;
        }
        public static bool isFinite(double val)
        {
            if (double.IsNaN(val) || double.IsInfinity(val))
                return false;
            return true;
        }
        public static bool areFloatsValid<T>(T val)
        {
            
            foreach (var f in typeof(T).GetProperties())
                if(f.PropertyType == typeof(float))
                {
                    if (!isFinite((float)f.GetValue(val)))
                        return false;
                }
                else if (f.ReflectedType == typeof(double))
                {
                    if (!isFinite((double)f.GetValue(val)))
                        return false;
                }
            return true;
        }

        /// <summary>
        /// a Dictionary whose keys are evaders (speeds up normal dictionary, by
        /// assuming all evaders have sequential ID values)
        /// </summary>
        /// <typeparam name="ValType"></typeparam>
        public class EvaderDictionary<ValueType> where ValueType : new()
        {
            public ValueType this[Evader e]
            {
                get
                {
                    return data[e.ID];
                }
                set
                {
                    data[e.ID] = value;
                }
            }
            public EvaderDictionary()
            {
                data = new ValueType[Evader.TotalAllocatedCount];
            }

            ValueType[] data;
        }

        
        public struct CapturedObservation
        {
            public Point where;
            public Evader who;
        }
        /// <summary>
        /// after evader ends its turn, it has an area of view. if in the following turn a pursuer goes through that view area, we create an observation, and the evader gets in in the start of his next round
        /// </summary>
        public struct PursuerPathObservation : IComparable<PursuerPathObservation>
        {
            public Evader observer;
            public Point observedFrom; // location of observer
            public List<Point> observedPursuerPath;  // a path that starts in pursuerPath.First and ends with pursuerPath.End was inside the evader's field of view
            public int round;

            public int CompareTo(PursuerPathObservation other)
            {
                return round.CompareTo(other.round);
            }

            public PursuerPathObservation(int Round, Point ObservedFrom, Evader Observer, int r_es, List<Point> PursuerPath)
            {
                this.observedFrom = ObservedFrom;
                this.round = Round;
                this.observer = Observer;
                this.observedPursuerPath = new List<Point>();
                foreach (Point p in PursuerPath)
                    if (p.manDist(observedFrom) <= r_es)
                        observedPursuerPath.Add(p);
                
            }
        }


        /// <summary>
        /// for each distinct target in the game, this vector contains data units from different 
        /// rounds that came from that target.
        /// NOTE: it is assumed most Add() operations are on the, Contains() operations may be slow 
        /// </summary>
        public class DataUnitVec
        {

            public struct Range
            {
                public bool isInRange(int r)
                {
                    return r <= maxRound && r >= minRound;
                }
                public int minRound;
                public int maxRound;
                public int Size { get {return maxRound - minRound + 1;}}
            }

            public Point Target { get; protected set; }

            /// <summary>
            /// assumes bigger.minRound <= smaller.maxRound <= bigger.maxRound and
            /// 
            /// </summary>
            /// <param name="smaller"></param>
            /// <param name="bigger"></param>
            /// <returns></returns>
            private int intersection(Range smaller, Range bigger)
            {
                return smaller.maxRound - Math.Max(bigger.minRound, smaller.minRound) + 1;
            }

            /// <summary>
            /// NOTE: assumes boths sets are not empty
            /// returns true if a unit not in 'otherSet' was found (and res was set to it)
            /// returns false otherwise
            /// </summary>
            /// <param name="otherSet"></param>
            /// <returns></returns>
            public bool getMinimalUnitNotInOtherSet(DataUnitVec otherSet, out DataUnit res)
            {
                // we now know there is SOME intersection
                var thisIter = roundRanges.First;
                var vIter = otherSet.roundRanges.First;

                // while the other range contains this range:
                while(thisIter.Value.minRound >= vIter.Value.minRound && thisIter.Value.maxRound <= vIter.Value.maxRound)
                {
                    thisIter = thisIter.Next;
                    if (thisIter == null)
                    {
                        res = DataUnit.NIL;
                        return false;
                    }
                    
                    if(thisIter.Value.minRound <= vIter.Value.maxRound && thisIter.Value.maxRound > vIter.Value.maxRound)
                    {
                        // this set contains otherSet's range's max value AND the following value    
                        res = new DataUnit { round = vIter.Value.maxRound + 1, sourceTarget = new Location(Target) };
                        return true;
                    }
                    if (vIter.Value.maxRound < thisIter.Value.minRound)
                    {
                        vIter = vIter.Next;
                        if(vIter == null)
                        {
                            res = new DataUnit { round = thisIter.Value.minRound, sourceTarget = new Location(Target) };
                            return true;
                        }
                    }
                }

                // if we get here, there is SOME intersection between these two ranges:
                if (thisIter.Value.minRound < vIter.Value.minRound)
                {
                    // this set contains otherSet's min value, AND the previous value    
                    res = new DataUnit { round = vIter.Value.minRound - 1, sourceTarget = new Location(Target) };
                    return true;
                }

                res = new DataUnit { round = vIter.Value.maxRound + 1, sourceTarget = new Location(Target) };
                return true;
            }

            public int Count { get; private set; }
            public int getIntersectionSize(DataUnitVec v)
            {
                int res = 0;

                if (roundRanges.Count == 0 || 
                    v.roundRanges.Count == 0 ||
                    roundRanges.First.Value.minRound > v.roundRanges.Last.Value.maxRound ||
                    v.roundRanges.First.Value.minRound > roundRanges.Last.Value.maxRound)
                {
                    return 0;
                }

                // we now know there is SOME intersection
                var thisIter = roundRanges.First;
                var vIter = v.roundRanges.First;

                do
                {
                    // make sure vIter and thisIter have some intersection:
                    while (vIter.Value.maxRound < thisIter.Value.minRound)
                    {
                        vIter = vIter.Next;
                        if (vIter == null)
                            return res;
                    }
                    
                    while (thisIter.Value.maxRound < vIter.Value.minRound)
                    {
                        thisIter = thisIter.Next;
                        if (thisIter == null)
                            return res;
                    }

                    if (thisIter.Value.maxRound > vIter.Value.maxRound)
                    {
                        res += intersection(vIter.Value, thisIter.Value);
                        vIter = vIter.Next;
                    } 
                    else if(thisIter.Value.maxRound < vIter.Value.maxRound)
                    {
                        res += intersection(thisIter.Value, vIter.Value);
                        thisIter = thisIter.Next;
                    }
                    else
                    {
                        res += thisIter.Value.maxRound - Math.Max(thisIter.Value.minRound, vIter.Value.minRound) + 1;
                        thisIter = thisIter.Next;
                        vIter = vIter.Next;
                    }
                    
                }
                while (thisIter != null && vIter != null);

                return res;
            }

            public List<DataUnit> ToList()
            {
                List<DataUnit> res = new List<DataUnit>();
                var valIter = this.roundRanges.First;
                while(valIter != null)
                {
                    for (int r = valIter.Value.minRound; r <= valIter.Value.maxRound; ++r)
                        res.Add(new DataUnit { sourceTarget = new Location(Target), round = r });
                    valIter = valIter.Next;
                }
                return res;
            }
            public DataUnitVec()
            {
                roundRanges = new LinkedList<Range>();
                Count = 0;
                
                // TODO: this is so ugly that its beatiful
                // It will cause bugs if getIntersectionSize() is used outside of Genetic algorithm
            }

            /// <summary>
            /// we act as if each data unit is stored in a different slot, and there are Count slots
            /// </summary>
            /// <param name="slotIdx"></param>
            /// <returns></returns>
            public DataUnit this[int slotIdx]
            {
                get
                {
                    var vIter = roundRanges.Last;
                    slotIdx = (Count - 1) - slotIdx ;
                    while(vIter.Value.Size < slotIdx)
                    {
                        slotIdx -= vIter.Value.Size;
                        vIter = vIter.Previous;
                    }
                    return new DataUnit{sourceTarget = new Location(Target), round = vIter.Value.maxRound - slotIdx};
                }
            }
            public DataUnitVec(Point target)
            {
                roundRanges = new LinkedList<Range>();
                Count = 0;
                Target = target;
            }
            
            public bool Add(DataUnit newU)
            {
                if (roundRanges.Count == 0)
                {
                    roundRanges.AddFirst(new Range { minRound = newU.round, maxRound = newU.round });
                    ++Count;
                    return true;
                }

                var last = roundRanges.Last.Value;
                if(last.maxRound <= (newU.round-1))
                {
                    // the most common option:
                    if (last.maxRound == (newU.round - 1))
                    {
                        roundRanges.Last.Value = new Range { maxRound = last.maxRound + 1, minRound = last.minRound };
                        ++Count;
                        return true;
                    }
                    roundRanges.AddLast(new Range{minRound = newU.round,maxRound = newU.round});
                    ++Count;
                    return true;
                }
                return addInner(newU.round);
            }
         
            public bool Contains(DataUnit du)
            {
                if (du == DataUnit.NIL)
                    return roundRanges.First.Value.maxRound == du.round;
                if (du == DataUnit.NOISE)
                    return roundRanges.First.Value.minRound == du.round;
                return Contains(du.round);
            }
            public bool Contains(int round)
            {
                var minRangeIt = roundRanges.Last;

                // find the first range that is not entirely > than 'round', if there is such a range
                while (minRangeIt.Previous != null && minRangeIt.Value.minRound > round)
                    minRangeIt = minRangeIt.Previous;

                return minRangeIt.Value.minRound >= round && minRangeIt.Value.maxRound <= round;
            }

            /// <summary>
            /// true if added
            /// </summary>
            /// <param name="round"></param>
            /// <returns></returns>
            private bool addInner(int round)
            {
                var minRangeIt = roundRanges.Last;
                
                // find the first range that is entirely <= than 'round', if there is such a range
                while (minRangeIt.Previous != null && minRangeIt.Value.maxRound > round)
                    minRangeIt = minRangeIt.Previous;

                Range minRange = minRangeIt.Value;

                if (minRange.maxRound == round)
                    return false;

                if (minRange.maxRound < round) // make sure the loop stopped only because minRangeIt.Previous == null
                {
                    var rangeItNext = minRangeIt.Next;
                    // we assume next is not null, since we cover this case in Add()
                    
                    if (minRange.maxRound == (round - 1))
                    {
                        // we enlarge the nearest range (and perhaps "glue" with the following range, if needed)
                        int newMax = round;
                        if(rangeItNext.Value.minRound == (round + 1))
                        {
                            newMax = rangeItNext.Value.maxRound;
                            roundRanges.Remove(rangeItNext);
                        }
                        minRangeIt.Value = new Range{minRound = minRange.minRound, maxRound = newMax};
                        ++Count;
                        return true;
                    }
                    minRange = rangeItNext.Value;
                    if (minRange.minRound > round) // check if we should add 'round' to the next (larger) range
                    {
                        if (minRange.minRound == (round + 1))
                        {
                            rangeItNext.Value = new Range { minRound = minRange.minRound - 1, maxRound = minRange.maxRound };
                            ++Count;
                            return true;
                        }
                        roundRanges.AddAfter(minRangeIt, new Range { minRound = round, maxRound = round });
                        ++Count;
                        return true;
                    }
                    return false;
                }
                
                // our smallest value is still larger than the new round :
                if(minRange.minRound == (round +1 ))
                {
                    minRangeIt.Value = new Range{minRound = minRange.minRound - 1, maxRound = minRange.maxRound};
                    ++Count;
                    return true;
                }
                if (minRange.minRound > round)
                {
                    roundRanges.AddFirst(new Range { minRound = round, maxRound = round });
                    ++Count;
                    return true;
                }
                return false;
            }

            public LinkedList<Range> roundRanges { get; private set; } // sorted in ascending values
            
        }


        public class Grid4Square
        {
            public Point TopLeft { get; set; }
            public int EdgeLen { get; set; }
            


            /// <summary>
            /// each side of the square has 'edgeLen'-1 points
            /// </summary>
            public Grid4Square(Point topLeft, int edgeLen)
            {
                TopLeft = topLeft;
                EdgeLen = edgeLen;
            }

            public bool isOnSquare(Point p)
            {
                int yDiff = p.Y - TopLeft.Y;
                int xDiff = p.X - TopLeft.X;

                if (xDiff == 0 || xDiff == EdgeLen)
                    return yDiff >= 0 && yDiff <= EdgeLen;
                if (yDiff == 0 || yDiff == EdgeLen)
                    return xDiff >= 0 && xDiff <= EdgeLen;
                return false;
            }

            public float getAngle(Point p)
            {
                int yDiff = p.Y - TopLeft.Y;
                int xDiff = p.X - TopLeft.X;

                if (xDiff == 0)
                    return 1 - ((float)(p.Y - TopLeft.Y))/EdgeLen;
                if (xDiff == EdgeLen)
                    return 2 + ((float)(p.Y - TopLeft.Y)) / EdgeLen;
                if (yDiff == 0)
                    return 1 + ((float)(p.X - TopLeft.X)) / EdgeLen;
                return 4 - ((float)(p.X - TopLeft.X)) / EdgeLen;
            }

            public Point getPointFromAngle(float a)
            {
                switch ((int)a)
                {
                    case 0: return TopLeft.addFRound(0, (1-a) * EdgeLen);
                    case 1: return TopLeft.addFRound((a - 1) * EdgeLen, 0);
                    case 2: return TopLeft.addFRound(EdgeLen, (a - 2) * EdgeLen);
                    default: return TopLeft.addFRound((4 - a) * EdgeLen, EdgeLen);
                }
            }
            public int PointCount
            {
                get { return 4 * (EdgeLen); }
            }
            public PointF Center { get { return new PointF(TopLeft.X + ((float)EdgeLen) / 2, TopLeft.Y + ((float)EdgeLen) / 2); } }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="center"></param>
            /// <param name="edgeLen"></param>
            /// <param name="currentPoint"></param>
            /// <param name="advanceCount">
            /// may be negaive. tells how many points to travel away from 'currentPoint', on the square's circumference
            /// </param>
            /// <returns></returns>
            public Point advancePointOnCircumference(Point currentPoint, int advanceCount)
            {
                float currentAngle = getAngle(currentPoint);
                return getPointFromAngle(MathEx.modf(currentAngle + ((float)(advanceCount)) / EdgeLen, 4));
            }

            // tells how many edges are needed for traversing clockwise from p1 to p2 on square's circumference
            // (may be negative)
            public int CWDistanceOnSquare(Point p1, Point p2)
            {
                return ((int)(Math.Round(EdgeLen * (getAngle(p2) - getAngle(p1)))) + PointCount) % PointCount;
            }
            public int CWDistanceOnSquare(int p1advances, int p2advances)
            {
                float p1 = 4 * ((float)p1advances) / PointCount;
                float p2 = 4 * ((float)p2advances) / PointCount;
                return ((int)(Math.Round(EdgeLen * (p2 - p1))) + PointCount) % PointCount;
            }
        }

        //public class Grid4Squaredep
        //{
        //    // sides corespond graphic view:
        //    // bottom/top - highest/lowest y
        //    // right/left - highest/lowest x
        //    private float bottom, top, right, left;
        //    private float EdgeLenHalf;
        //    private Point center;
        //    private PointF centerF;
        //    public int EdgeLen 
        //    {
        //        get
        //        {
        //            return (int)(bottom - top);
        //        }
        //        set
        //        {
        //            if (value % 2 == 0)
        //                throw new Exception("Grid4Square.EdgeLen must be odd");

        //            EdgeLenHalf = ((float)value)/2.0f;
        //            bottom = centerF.Y + EdgeLenHalf;
        //            right = centerF.X + EdgeLenHalf;
        //            top = centerF.Y - EdgeLenHalf;
        //            left = centerF.X - EdgeLenHalf;

        //        }
        //    }
        //    public Point Center 
        //    { 
        //        get
        //        {
        //            return center;
        //        }
        //        set
        //        {

        //            center = value;
        //            centerF = new PointF(center.X + 0.5f, center.Y + 0.5f);
        //            EdgeLen = (int)(EdgeLen);
        //        }
        //    }



        //    public Grid4Squaredep(Point SquareCenter, int EdgeLength)
        //    {
        //        center = SquareCenter;
        //        centerF = new PointF(center.X + 0.5f, center.Y + 0.5f);
        //        EdgeLen = EdgeLength;
        //    }

        //    public bool isOnSquare(Point p)
        //    {

        //        if (Math.Abs((float)p.X - centerF.X) == EdgeLenHalf)
        //            return (p.Y - top) < (EdgeLen - 1);
        //        if (Math.Abs((float)p.Y - centerF.Y) == EdgeLenHalf)
        //            return (p.X - left) < (EdgeLen - 1);
        //        return false;
        //    }
        //    public float getAngle(Point currentPoint)
        //    {
        //        if (currentPoint.X == left - 1)
        //            return (bottom - (currentPoint.Y)) / EdgeLen;
        //        if (currentPoint.X == right)
        //            return 2 - (bottom - (currentPoint.Y )) / EdgeLen;

        //        if (currentPoint.Y == bottom - 1)
        //            return 1 + (currentPoint.X  - left);

        //        return 4 - (currentPoint.X - left);

        //        //if (currentPoint.X == left)
        //        //    return 1 - (currentPoint.Y + 0.5f - (center.Y - (float)edgeLenRad)) / (EdgeLen); // [0,1]
        //        //if (currentPoint.Y == Center.Y - edgeLenRad)
        //        //    return 1 + (currentPoint.X + 0.5f - (center.X - (float)edgeLenRad)) / (EdgeLen); // (1,2]
        //        //if (currentPoint.X == Center.X + edgeLenRad)
        //        //    return 2 + ((center.Y + (float)edgeLenRad) - (currentPoint.Y + 0.5f)) / (EdgeLen); // (2,3]

        //        ////if (currentPoint.Y == center.Y + edgeLenRad)
        //        //return 4 - ((center.X - (float)edgeLenRad) - (currentPoint.X + 0.5f)) / (EdgeLen); // (3,4)

        //    }
        //    public Point getPointFromAngle(float a)
        //    {
        //        switch((int)a)
        //        {
        //            case 0: return new Point((int)(right), (int)(bottom - a * EdgeLenHalf));
        //            case 1: return new Point((int)(left + (a-1) * EdgeLenHalf), (int)(bottom));
        //            case 2: return new Point((int)(left), (int)(top + (a-2) * EdgeLenHalf));
        //            default : return new Point((int)(right - (a - 3) * EdgeLenHalf), (int)(bottom));
        //        }
        //    }


        //    /// <summary>
        //    /// 
        //    /// </summary>
        //    /// <param name="center"></param>
        //    /// <param name="edgeLen"></param>
        //    /// <param name="currentPoint"></param>
        //    /// <param name="advanceCount">
        //    /// may be negaive. tells how many points to travel away from 'currentPoint', on the square's circumference
        //    /// </param>
        //    /// <returns></returns>
        //    public Point advancePointOnCircumference(Point currentPoint, int advanceCount)
        //    {
        //        float currentAngle = getAngle(currentPoint);
        //        return getPointFromAngle(MathEx.modf(currentAngle + ((float)(advanceCount)) / EdgeLen, 4));
        //    }
        //}

           
        /// <summary>
        /// returns 4 points, 1 for each direction from p (+-1)
        /// </summary>
        /// <param name="p"></param>
        /// <returns></returns>
        public static List<PointF> getAdjacentManDist(PointF p)
        {
            List<PointF> res = new List<PointF>();
            res.Add(new PointF(p.X - 1, p.Y));
            res.Add(new PointF(p.X + 1, p.Y));
            res.Add(new PointF(p.X, p.Y - 1));
            res.Add(new PointF(p.X, p.Y + 1));

            return res;
        }
        public static Point? runAwayFrom(GridGameGraph g, Point currentLocation, PointF runningAwayFrom, Func<Point,bool> pointApprover)
        {
            
            var options = g.getAdjacentIDs(currentLocation);
            Point res = currentLocation;
            double dist = g.getMinDistance(currentLocation, runningAwayFrom) + 1;
            
            foreach (Point p in options)
                if (g.getMinDistance(p, runningAwayFrom) == dist && pointApprover(p))
                {
                    return p;
                }

            return null;
        }
        public static Point? increaseSquareRadius(GridGameGraph g, Point currentLocation, PointF runningAwayFrom, Func<Point, bool> pointApprover)
        {

            var options = g.getAdjacentIDs(currentLocation);
            PointF res = new PointF(currentLocation.X, currentLocation.Y);
            float rad = res.subtruct(runningAwayFrom).getAbsCoords().maxCoord() + 1;
            
            foreach (Point p in options)
                if ((new PointF(p.X,p.Y)).subtruct(runningAwayFrom).getAbsCoords().maxCoord() == rad && pointApprover(p))
                {
                    return p;
                }

            return null;
        }



        /// <summary>
        /// Assuming the evader moves 1 step from start to dest, this calculates which
        /// direction is on the shortest path, considering the weight of each transition is the probability for capture
        /// TODO: the alg. is not good! Instead of summing the capture probabilities, we need multiplication (the point is finding the path
        /// where the probability for surviving is maximized)
        /// </summary>
        /// <param name="startPoint"></param>
        /// <param name="destPoint"></param>
        /// <param name="graphWeights">
        /// contains the weight for each point in the grid between start and dest points (i.e. graphWeights[>startPoint.x,>startPoint.y] )
        /// </param>
        /// <returns></returns>
        public static Point getShortestPathDest(Point startPoint, Point destPoint, float[,] graphWeights)
        {
            // we start from dest point, and each time calculate the weight of entering 
            // an "increaseing triangle" that increases towards start point

            int width = Math.Abs(destPoint.X - startPoint.X);
            int height = Math.Abs(destPoint.Y - startPoint.Y);


            int xDir = (destPoint.X > startPoint.X) ? (1) : (-1);
            int yDir = (destPoint.Y > startPoint.Y) ? (1) : (-1);
            int iterationCount = (width) + (height);


            float[] currentWeights = new float[Math.Max(width+1, height)];
            float[] nextWeights = new float[Math.Max(width+1, height)];

            currentWeights[0] = 0;
            int prevLineLen = 1; // if it was 0, it would cause an index underflow on the first destDist iteration


            Point triPoint;
            Point triPointEnd = new Point(), triPointStart = new Point();
            for (int destDist = 0; destDist < iterationCount; ++destDist)
            {
                triPoint = destPoint.add(-xDir * destDist, 0);
                triPointEnd = destPoint.add(0, -yDir * destDist);
                int lineLen;
                if (destDist > width)
                {
                    triPoint.X = startPoint.X;
                    triPoint = triPoint.add(0, (destDist - width) * -yDir);
                }
                if (destDist > height)
                {
                    triPointEnd.Y = startPoint.Y;
                    triPointEnd = triPointEnd.add((destDist - height) * -xDir, 0);
                }
                triPointStart = triPoint;

                // counts all points in the line between triPoint and triPointEnd
                lineLen = Math.Min(Math.Abs(triPoint.X - triPointEnd.X),
                                         Math.Abs(triPoint.Y - triPointEnd.Y)) + 1;

                int firstMultiChoicePoint = 0,
                    lastMultiChoicePoint = lineLen - 1;

                int corespondingWeightPrevRoundDiff = 0;

                // if the triangle expands, the extreme points have only one traval option 
                if (lineLen > prevLineLen)
                {
                    nextWeights[0] = graphWeights[triPoint.X, triPoint.Y] + currentWeights[0];
                    nextWeights[lineLen - 1] = graphWeights[triPointEnd.X, triPointEnd.Y] + currentWeights[prevLineLen - 1];
                    ++firstMultiChoicePoint;
                    --lastMultiChoicePoint;
                    triPoint = triPoint.add(xDir, -yDir);
                    corespondingWeightPrevRoundDiff = -1;
                }
                else if (lineLen == prevLineLen)
                {
                    // if lens are equal, then the expending triangle intersected the grid rectngle on 1 direction. 
                    // Only the point on the first side that was hit has two travel options
                    if (triPoint.X == destPoint.X)
                    {
                        nextWeights[0] = graphWeights[triPoint.X, triPoint.Y] + currentWeights[0];
                        ++firstMultiChoicePoint;
                        triPoint = triPoint.add(xDir, -yDir);
                        corespondingWeightPrevRoundDiff = -1;
                    }
                    else
                    {
                        nextWeights[lineLen - 1] = graphWeights[triPointEnd.X, triPointEnd.Y] + currentWeights[prevLineLen - 1];
                        --lastMultiChoicePoint;
                        corespondingWeightPrevRoundDiff = 1;
                    }
                }
                else
                    corespondingWeightPrevRoundDiff = 1;


                for (int i = firstMultiChoicePoint; i <= lastMultiChoicePoint; ++i)
                {
                    nextWeights[i] = graphWeights[triPoint.X, triPoint.Y] +
                        Math.Min(currentWeights[i], currentWeights[i + corespondingWeightPrevRoundDiff]);
                    triPoint = triPoint.add(xDir, -yDir);
                }

                AlgorithmUtils.Swap(ref currentWeights, ref nextWeights);
                prevLineLen = lineLen;
            }

            if (currentWeights[0] < currentWeights[1])
                return triPointStart;
            else
                return triPointEnd;
        }

        /// <summary>
        /// if false evader had no data that isn't already in sink. 
        /// if true, then untransmittedUnit is set to the unit from earliest round which is not in sink
        /// </summary>
        /// <param name="unitsInSink"></param>
        /// <param name="s"></param>
        /// <param name="e"></param>
        /// <param name="untransmittedUnit"></param>
        /// <returns></returns>
        public static bool getUntransmittedDataUnit(GoE.GameLogic.Utils.DataUnitVec unitsInSink, GameState s, Evader e, out DataUnit untransmittedUnit)
        {
            var mem = s.M[s.MostUpdatedEvadersMemoryRound];
            DataUnitVec evMem;
            if (!mem.TryGetValue(e, out evMem))
            {
                untransmittedUnit = DataUnit.NIL;
                return false;
            }

            return evMem.getMinimalUnitNotInOtherSet(unitsInSink, out untransmittedUnit);
        }

        /// <summary>
        /// not really an angle, but we define a [0,4) CW "angles" around a point(0,0), where 0 is leftmost(x<) to the point, 1 is upmost(y<), 2 is rightmost(x>) and 3 is downmost(y>)
        /// </summary>
        /// <returns></returns>
        public static Point getGridPointByAngle(int radius, float anglef)
        {
            // NOTE: in some strange cases, Math.round() of exactly 0.5 or -0.5 behaves strangely (this actually happened!) and may 
            // return a point not in correct radius, if not fixed. the +0.00017 spares an if() , and probably will always work
            double angle = anglef + 0.0000017;

            if (angle <= 1)
                return new Point((int)Math.Round((angle - 1) * radius),
                                 (int)Math.Round(-angle * radius));
            else if (angle <= 2)
            {
                angle -= 1;
                return new Point((int)Math.Round(angle * radius),
                                 (int)Math.Round((angle - 1) * radius));
            }
            else if (angle <= 3)
            {
                angle -= 2;
                return new Point((int)Math.Round(((1 - angle) * radius)),
                                 (int)Math.Round((angle * radius)));
            }

            angle -= 3;
            return new Point((int)Math.Round(-angle * radius),
                             (int)Math.Round((1 - angle) * radius));
        }

        public static float getGridPointAngleDiff(Point p1, Point p2, out bool a1CWtoa2)
        {
            return getGridPointAngleDiff(getAngleOfGridPoint(p1), getAngleOfGridPoint(p2), out a1CWtoa2);
        }

        public enum GridCircleQuadrant : int
        {
            
            Q1 = 0,
            Q12 = 1,
            Q2 = 2,
            Q23 = 3,
            Q3 = 4,
            Q34 = 5,
            Q4 = 6,
            Q41 = 7
        }

        /// <summary>
        /// assumes angle is in [0,4)
        /// </summary>
        /// <param name="angle"></param>
        /// <returns></returns>
        public static GridCircleQuadrant getQuad(float angle)
        {
            if (angle < 2)
            {
                if(angle < 1)
                {
                    if(angle == 0)
                        return GridCircleQuadrant.Q41;
                    return GridCircleQuadrant.Q1;
                }
                if(angle == 1)
                    return GridCircleQuadrant.Q12;
                return GridCircleQuadrant.Q2;
            }
            if(angle < 3)
            {
                if(angle == 2)
                    return GridCircleQuadrant.Q23;
                return GridCircleQuadrant.Q3;
            }
            if(angle == 3)
                return GridCircleQuadrant.Q34;
            return GridCircleQuadrant.Q4;
        }
        public static GridCircleQuadrant getQuad(this Point p, Point center)
        {
            if(p.X < center.X)
            {
                if(p.Y < center.Y)
                    return GridCircleQuadrant.Q1;
                if(p.Y > center.Y)
                    return GridCircleQuadrant.Q4;
                
                return GridCircleQuadrant.Q41;
            }
            if(p.X > center.X)
            {
                if(p.Y < center.Y)
                    return GridCircleQuadrant.Q2;
                if(p.Y > center.Y)
                    return GridCircleQuadrant.Q3;
                
                return GridCircleQuadrant.Q23;
            }

           // x is equal
            if (p.Y < center.Y)
                return GridCircleQuadrant.Q12;
            return GridCircleQuadrant.Q34;
        }

        /// <summary>
        /// returns diff to the next diagonal point adjacent to 'current' which has a larger CW angle, and
        /// is on the circle around 'center' in radius rad
        /// </summary>
        /// <param name="current"></param>
        /// <param name="center"></param>
        /// <param name="rad"></param>
        /// <returns></returns>
        public static Point getNextDiagPointCWDiff(GridCircleQuadrant q)
        {
            switch(q)
            {
                case GridCircleQuadrant.Q41:
                case GridCircleQuadrant.Q1: return new Point(1, -1);
                case GridCircleQuadrant.Q12:
                case GridCircleQuadrant.Q2: return new Point(1, 1);
                case GridCircleQuadrant.Q23:
                case GridCircleQuadrant.Q3: return new Point(-1, 1);
                case GridCircleQuadrant.Q34:
                case GridCircleQuadrant.Q4: return new Point(-1, -1);
            }
            return new Point(0, 0);
        }

        /// <summary>
        /// similar to getNextDiagPointCW(), but returns a point truly adjacent (and not diagonal) to current point
        /// (point further from circle center is chosen)
        /// </summary>
        /// <param name="current"></param>
        /// <param name="center"></param>
        /// <param name="rad"></param>
        /// <returns></returns>
        public static Point getNextPointCWDiff(GridCircleQuadrant q)
        {
            switch (q)
            {
                case GridCircleQuadrant.Q41: 
                case GridCircleQuadrant.Q1: return new Point(0, -1);
                case GridCircleQuadrant.Q12:
                case GridCircleQuadrant.Q2: return new Point(1, 0);
                case GridCircleQuadrant.Q23:
                case GridCircleQuadrant.Q3: return new Point(0, 1);
                case GridCircleQuadrant.Q34:
                case GridCircleQuadrant.Q4: return new Point(-1, 0);
            }
            return new Point(0, 0);

        }

        /// <summary>
        /// returns diff to the next diagonal point adjacent to 'current' which has a larger CW angle, and
        /// is on the circle around 'center' in radius rad
        /// </summary>
        /// <param name="current"></param>
        /// <param name="center"></param>
        /// <param name="rad"></param>
        /// <returns></returns>
        public static Point getNextDiagPointCCWDiff(GridCircleQuadrant q)
        {
            switch (q)
            {
                case GridCircleQuadrant.Q1:
                case GridCircleQuadrant.Q12: return new Point(-1, 1);
                case GridCircleQuadrant.Q2:
                case GridCircleQuadrant.Q23: return new Point(-1, -1);
                case GridCircleQuadrant.Q3:
                case GridCircleQuadrant.Q34: return new Point(1, -1);
                case GridCircleQuadrant.Q4:
                case GridCircleQuadrant.Q41: return new Point(1, 1);
            }
            return new Point(0, 0);
        }

        /// <summary>
        /// similar to getNextDiagPointCW(), but returns a point truly adjacent (and not diagonal) to 'current'
        /// (point further from 'center' is chosen)
        /// </summary>
        /// <param name="current"></param>
        /// <param name="center"></param>
        /// <param name="rad"></param>
        /// <returns></returns>
        public static Point getNextPointCCWDiff(GridCircleQuadrant q)
        {
            switch (q)
            {
                case GridCircleQuadrant.Q1:
                case GridCircleQuadrant.Q12: return new Point(-1, 0);
                case GridCircleQuadrant.Q2:
                case GridCircleQuadrant.Q23: return new Point(0, -1);
                case GridCircleQuadrant.Q3:
                case GridCircleQuadrant.Q34: return new Point(1, 0);
                case GridCircleQuadrant.Q4:
                case GridCircleQuadrant.Q41: return new Point(0, 1);
            }
            return new Point(0, 0);

        }

        ///// <summary>
        ///// more efficient than two getNextDiagPoint() and getNextPoint() calls
        ///// </summary>
        ///// <param name="current"></param>
        ///// <param name="center"></param>
        ///// <param name="rad"></param>
        ///// <param name="nextAdj"></param>
        ///// <param name="nextDiag"></param>
        ///// <returns>
        ///// angle of 'nextDiag'
        ///// </returns>
        //public static float getNextPointsCW(Point current, Point center, int rad, out Point nextAdj, out Point nextDiag)
        //{
        //    if (center.X < current.X)
        //    {
        //        if (center.Y < current.Y) 
        //        {
        //            nextDiag = current.add(-1, 1);
        //            nextAdj = current.add(0, 1);
        //            return 3 - ((float)(nextAdj.X - center.X)) / rad;
        //        }
        //        else
        //        {
        //            nextDiag = current.add(1, 1);
        //            nextAdj = current.add(1, 0);
        //            return 1 + ((float)(nextAdj.X - center.X)) / rad;
        //        }
        //    }
        //    else if (center.X > current.X)
        //    {
        //        if (center.Y < current.Y)
        //        {
        //            nextDiag = current.add(-1, -1);
        //            nextAdj = current.add(-1, 0);
        //            float res = 3 + ((float)(center.X - nextAdj.X)) / rad;
        //            return (res < 4)?(res):(res-4);
        //        }
        //        else
        //        {
        //            nextDiag = current.add(1, -1);
        //            nextAdj = current.add(0, -1);
        //            return ((float)(center.X - nextAdj.X)) / rad;
        //        }

        //    }
            
        //    // if center.x = current.x
        //    if (center.Y > current.Y)
        //    {
        //        nextDiag = current.add(1, 1);
        //        nextAdj = current.add(1, 0);
        //        return 3.0f;
        //    }
            
        //    nextDiag = current.add(-1, -1);
        //    nextAdj = current.add(-1, 0);
        //    return 1.0f;
            
        //}

        public static Point advanceOnPath(GridGameGraph g, Point from, Point to, bool returnFirst, HashSet<Point> pointsToAvoid, out bool isBlocked)
        {
            var options = g.getAdjacentIDs(from);
            Point res = from;
            int dist = (int)g.getMinDistance(from, to) - 1;
            isBlocked = true;
            foreach (Point p in options)
                if (g.getMinDistance(p, to) == dist && !pointsToAvoid.Contains(p))
                {
                    isBlocked = false;
                    if (returnFirst)
                        return p;
                    returnFirst = true;
                    res = p;
                }

            return res;
        }
        /// <summary>
        /// retutns diff between current and a point with 'newRad' from center
        /// </summary>
        /// <param name="current"></param>
        /// <param name="center"></param>
        /// <param name="rad"></param>
        /// <param name="newRad"></param>
        /// <returns></returns>
        public static Point getOrthogonalPointDiff(Point current, Point center, int rad, int newRad)
        {

            return center.add((current.subtruct(center)).mult(((float)newRad) / rad)).subtruct(current);

            //int dirx = 0, diry = 0;
            //int addition = newRad - rad;
            //int diffX = current.X - center.X;
            //int diffY = current.Y - center.Y;
            
            //if (diffX != 0)
            //    dirx = addition * (Math.Abs(diffX) / diffX);
            //if(diffY != 0)
            //    diry = addition * (Math.Abs(diffY) / diffY);

            //return current.add(dirx, diry);
        }

        /// <summary>
        /// assumes start,end and circle are on the same line.
        /// creates a path (start exluded, end included unless it's equal to start) and adds it to the end of pathOUT
        /// </summary>
        /// <param name="pathOUT"></param>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <param name="circleCenter"></param>
        /// <param name="CW">
        /// points that connect the diagonal points in the new path may be either CW or CCW 
        /// </param>
        public static void addOrhotonalPath(List<Point> pathOUT, Point start, Point end, Point circleCenter, bool CW)
        {
            if (start == end)
                return;

            if(start.X == end.X)
            {
                int dir = (end.Y > start.Y) ? (1) : (-1);
                for (int y = start.Y + dir; y != end.Y; y += dir)
                    pathOUT.Add(new Point(start.X, y));
                pathOUT.Add(end);
                return;
            }
            if (start.Y == end.Y)
            {
                int dir = (end.X > start.X) ? (1) : (-1);
                for (int x = start.X + dir; x != end.X; x += dir)
                    pathOUT.Add(new Point(x, start.Y));
                pathOUT.Add(end);
                return;
            }

            int startRad = start.manDist(circleCenter);
            int endRad = end.manDist(circleCenter);

            
            int pathPointIdx = pathOUT.Count;
            pathOUT.Resize(pathOUT.Count + Math.Abs(endRad - startRad));
            

            //Point current;
            //Point last;
            
            int pathPointIdxDir;

            // the loop goes from nearer to center and to further to center. we check if the insertion
            // into the result path should be reversed
            if(startRad < endRad)
            {
                pathPointIdx = 0;
                pathPointIdxDir = 1;
                //current = start;
                //last = end;
                // note we don't add 'start' point

                pathOUT[pathOUT.Count-1] = end;
            }
            else
            {
                pathPointIdx = pathOUT.Count-1; // we start from the end 
                pathPointIdxDir = -1;
                //current = end;
               // last = start;
                
                pathOUT[pathPointIdx] = end;
                pathPointIdx += pathPointIdxDir; // we now add 'end' point, and before finishing, we remove the 'start' point that will be added to the pathOUT end
            }

            Point diagDiff;
            Point adjDiff;
            var q = start.getQuad(circleCenter);
            if (CW)
                adjDiff = getNextPointCWDiff(q);
            else
                adjDiff = getNextPointCCWDiff(q);

            if (startRad > endRad)
            {
                adjDiff.X *= -1;
                adjDiff.Y *= -1;

                diagDiff = getOrthogonalPointDiff(start, circleCenter, startRad, startRad + 1);
                diagDiff.X *= -1;
                diagDiff.Y *= -1;
                diagDiff.subtruct(adjDiff);
            }
            else
                diagDiff = getOrthogonalPointDiff(start, circleCenter, startRad, startRad + 1).subtruct(adjDiff);
                    // we always add diagDiff after we added adjDiff, so cancel it beforehand

            // the loop excludes end and start point and two last points
            while (start.manDist(end)>2)
            {
                start = start.add(adjDiff);
                pathOUT[pathPointIdx] = start;
                pathPointIdx += pathPointIdxDir;

                start = start.add(diagDiff);
                pathOUT[pathPointIdx] = start;
                pathPointIdx += pathPointIdxDir;
            }

            // end was already added, so the only missing node is the one adjacent to end
            start = start.add(adjDiff);
            pathOUT[pathPointIdx] = start;

        }

        /// <summary>
        /// assumes start.manDist(center) = end.manDist(center)
        /// creates a path (start exluded, end included unless equal to start) and adds it to the end of pathOUT
        /// </summary>
        /// <param name="pathOUT"></param>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <param name="circleCenter"></param>
        /// <param name="CW"></param>
        public static void addHorizontalPath(List<Point> pathOUT, Point start, Point end, Point circleCenter, bool CW)
        {
            if (start == end)
                return;
            int pathIdxDir = 1;
            //if(!CW)
            //{
                
            //    Point tmp = start;
            //    start = end;
            //    end = start;
            //}

            float rad = start.manDist(circleCenter);

            Point diagDiff;
            Point adjDiff = new Point();


            if(!CW)
            {
                var tmp = start;
                start = end;
                end = tmp;
            }

            float currentAngle = getAngleOfGridPoint(start.subtruct(circleCenter));
            float lastAngle = getAngleOfGridPoint(end.subtruct(circleCenter));
            float cwAngleDiff = Utils.getGridPointAngleDiffCW(currentAngle, lastAngle);
            //if(CW)
                //lastAngle = currentAngle + getGridPointAngleDiffCW(currentAngle, getAngleOfGridPoint(end.subtruct(circleCenter)));
            //else
              //  lastAngle = currentAngle + getGridPointAngleDiffCCW(currentAngle, getAngleOfGridPoint(end.subtruct(circleCenter)));

            int pathIdx = pathOUT.Count;

            pathOUT.Resize((int)(pathOUT.Count + Math.Round(2 * rad * cwAngleDiff)));// Math.round helps for inaccuracies. otherwise may get something like: resize((int)3.999) <=> resize(3)

            if (CW)
            {
                pathOUT[pathOUT.Count-1] = end;
            }
            else
            {
                pathOUT[pathOUT.Count - 1] = start; // we swapped start and end previously, so this is actually the original end
                pathIdx = pathOUT.Count - 2; // start writing from the end of the array
                pathIdxDir = -1;
            }

            float nextStop = 0;
            // the loop excludes the 2 last points, and start point 
            // note: lastAngle may be more than 4, and that's ok!
            float angleDiff = 1.0f / rad;
            while (Math.Abs(start.X - end.X) !=1 || Math.Abs(start.Y - end.Y) !=1)
            {
                float effAngle = MathEx.modf(currentAngle, 4.0f);
                GridCircleQuadrant q = getQuad(effAngle);
                adjDiff = getNextPointCWDiff(q);
                diagDiff = getNextDiagPointCWDiff(q).subtruct(adjDiff); // we add diagDiff after we also add adjDiff , so cancel it beforehand
                nextStop = (float)Math.Ceiling(currentAngle); // when currentAngle reaches next stop, we need to change the diffs
                
                do
                {
                    start = start.add(adjDiff);
                    pathOUT[pathIdx] = start;
                    pathIdx += pathIdxDir;
                    start = start.add(diagDiff);
                    pathOUT[pathIdx] = start;
                    pathIdx += pathIdxDir;
                    currentAngle += angleDiff;
                }
                while ((currentAngle + angleDiff) < nextStop && (Math.Abs(start.X - end.X) !=1 || Math.Abs(start.Y - end.Y) !=1));

                if (currentAngle + angleDiff > nextStop) // due to float inaccuracy, currentAngle is slightly smaller than next stop even though they should be equal
                    currentAngle = nextStop; 
            }
            
            // since we excluded the last 2 points but did insert 'end', only the last adjacent point is till missing:
            if (currentAngle >= nextStop)
            {
                float effAngle = MathEx.modf(currentAngle, 4.0f);
                GridCircleQuadrant q = getQuad(effAngle);
                adjDiff = getNextPointCWDiff(q);
            }

            start = start.add(adjDiff);
            pathOUT[pathIdx] = start; 


            //if (!CW)
              //  pathOUT.RemoveAt(pathOUT.Count - 1); // this is the original 'start' point which we want to exclude
           
        }
  
        /// <summary>
        /// expects two angles in [0,4], returns diff in [0,2]
        /// </summary>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <returns></returns>
        public static float getGridPointAngleDiff(float a1, float a2, out bool a1CWtoa2)
        {

            if(a1 > a2)
            {
                if (a1 - a2 > 2)
                {
                    a1CWtoa2 = false;
                    return 4 + a2 - a1;
                }
                a1CWtoa2 = true;
                return a1 - a2;
            }

            if (a2 - a1 > 2)
            {
                a1CWtoa2 = true;
                return 4 + a1 - a2;
            }

            a1CWtoa2 = false;
            return a2 - a1;    
        }
        public static float getGridPointAngleDiffCW(float a1, float a2)
        {
            bool isCCW;
            float diff = getGridPointAngleDiff(a1, a2, out isCCW);
            if (isCCW)
                return 4-diff;
            return diff;
        }
        public static float getGridPointAngleDiffCCW(float a1, float a2)
        {
            bool isCCW;
            float diff = getGridPointAngleDiff(a1, a2, out isCCW);
            if (!isCCW)
                return 4 - diff;
            return diff;
        }

        public struct SortedPointIdx
        {
            public int pointIndex;
            public float pointAngle;
        }
        public struct SortedPoint
        {
            public Point point;
            public float pointAngle;
        }      
        /// <summary>
        /// sorts the points according to angle (ascending i.e. CW)
        /// </summary>
        public static List<SortedPointIdx> sortPointIndicesByAngles(List<Point> points, Point center)
        {
            List<SortedPointIdx> res = new List<SortedPointIdx>();
            int i = 0;
            foreach(Point p in points)
                res.Add(new SortedPointIdx() { pointAngle = getAngleOfGridPoint(p.subtruct(center)), pointIndex = i++ });
            res.Sort(new Comparison<SortedPointIdx>((x, y) => x.pointAngle.CompareTo(y.pointAngle)));
            return res;
        }
        /// <summary>
        /// sorts the points according to angle (ascending)
        /// </summary>
        public static List<SortedPoint> sortPointsByAngles(List<Point> points, Point center)
        {
            List<SortedPoint> res = new List<SortedPoint>();
            int i = 0;
            foreach (Point p in points)
                res.Add(new SortedPoint() { pointAngle = getAngleOfGridPoint(p.subtruct(center)), point = p});
            res.Sort(new Comparison<SortedPoint>((x, y) => x.pointAngle.CompareTo(y.pointAngle)));
            return res;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="pointsSortedByAngles"></param>
        /// <param name="center"></param>
        /// <returns>
        /// average manhatten distance variance between every two following points
        /// </returns>
        public static float getAverageDistanceVariance(List<SortedPoint> pointsSortedByAngles, Point center)
        {
            float avgDist = 0;
            int rad = pointsSortedByAngles.First().point.manDist(center);
            float[] distances = new float[pointsSortedByAngles.Count - 1];
            for (int i = 1; i < pointsSortedByAngles.Count; ++i)
                distances[i-1] = getGridPointAngleDiffCW(pointsSortedByAngles[i - 1].pointAngle, pointsSortedByAngles[i].pointAngle);

            avgDist =
                (pointsSortedByAngles.Last().pointAngle - pointsSortedByAngles.First().pointAngle) / (pointsSortedByAngles.Count - 1);
            
            double distVar = 0;
            for (int i = 1; i < pointsSortedByAngles.Count; ++i)
                distVar += MathEx.Sqr(distances[i - 1] - avgDist);
            return rad * (float)(Math.Sqrt(distVar) / (pointsSortedByAngles.Count - 1));
        }
        /// <summary>
        /// rotates a point on the circle (relative to center) , where angle in [-2,2]
        /// </summary>
        /// <param name="p"></param>
        /// <param name="center"></param>
        /// <param name="angle"></param>
        /// <returns></returns>
        public static Point getRotated(Point p, Point center, float angle)
        {
            float currentAngle = getAngleOfGridPoint(p.subtruct(center));
            return center.add(getGridPointByAngle(p.manDist(center), angle + currentAngle));
        }

        /// <summary>
        /// coresponds getGridPointByAngle
        /// </summary>
        public static float getAngleOfGridPoint(Point p)
        {
            int rad = p.manDist(new Point(0,0));

            if(p.X >= 0)
            {
                if (p.Y >= 0)
                    return 2 + (float)(p.Y) / rad;
                return 1 + (float)(p.X) / rad;
            }
            if(p.Y >= 0)
                return (p.X == -rad)?(0): (3 - (float)(p.X) / rad); // make sure we return 0.0 instead of 4.0
            
            return 1 + (float)(p.X) / rad;
            
        }

        /// <summary>
        /// if a location points to a point, return it.
        /// otherwise, return (-1,-1)
        /// </summary>
        public static Point locationToPoint(Location l)
        {
            if (l.locationType == GameLogic.Location.Type.Node)
                return l.nodeLocation;
            return new Point(-1, -1);
        }
        
        /// <summary>
        /// uses locationToPoint() to cast a list of locations
        /// </summary>
        /// <param name="l"></param>
        /// <returns></returns>
        public static List<Point> locationsToPoints(List<Location> l)
        {
            List<Point> res = new List<Point>();
            foreach (Location loc in l)
                res.Add(locationToPoint(loc));
            return res;
        }

        public static List<Location> pointsToLocations(List<Point> l)
        {
            List<Location> res = new List<Location>();
            foreach (Point p in l)
                res.Add(new Location(p));
            return res;
        }

        public static float distance(this PointF p1, PointF p2)
        {
            return (float)Math.Sqrt(MathEx.PowInt(p1.X - p2.X, 2) + MathEx.PowInt(p1.Y - p2.Y, 2));
        }

        /// <summary>
        /// different method name for PointF and Point since in mono this creates ambiguity
        /// </summary>
        /// <param name="p1"></param>
        /// <param name="p2"></param>
        /// <returns></returns>
        public static float distance2F(this PointF p1, PointF p2)
        {
            return (float)MathEx.PowInt(p1.X - p2.X, 2) + (float)MathEx.PowInt(p1.Y - p2.Y, 2);
        }

        public static float distance2(this Point p1, Point p2)
        {
            return (float)MathEx.PowInt(p1.X - p2.X, 2) + (float)MathEx.PowInt(p1.Y - p2.Y, 2);
        }

        // FIXME: move many functions to template file, so we can also have float version of them
        public static Point addPoints(Point p1, Point p2)
        {
            return new Point(p1.X + p2.X, p1.Y + p2.Y);
        }
        public static Point subtructPoints(Point p1, Point p2)
        {
            return new Point(p1.X - p2.X, p1.Y - p2.Y);
        }

        /// different method name for PointF and Point since in mono this creates ambiguity
        public static PointF addF(this PointF p, float x, float y)
        {
            p.X += x; // PointF is a struct, so 'this' doesn't really change!
            p.Y += y;
            return p;
        }
        public static PointF mult(this PointF p, float s)
        {
            p.X *= s; // PointF is a struct, so 'this' doesn't really change!
            p.Y *= s;
            return p;
        }
        public static PointF add(this PointF p1, PointF p2)
        {
            p1.X += p2.X;// PointF is a struct, so 'this' doesn't really change!
            p1.Y += p2.Y;
            return p1;
        }
        public static Point add(this Point p1, Point p2)
        {
            p1.X += p2.X;// PointF is a struct, so 'this' doesn't really change!
            p1.Y += p2.Y;
            return p1;
        }
        public static Point mult(this Point p, int s)
        {
            p.X *= s;// PointF is a struct, so 'this' doesn't really change!
            p.Y *= s;
            return p;
        }

        /// <summary>
        /// Math.round() is used on resulted coordinates
        /// </summary>
        public static Point mult(this Point p, float s)
        {
            p.X = (int)Math.Round(p.X * s); // PointF is a struct, so 'this' doesn't really change!
            p.Y = (int)Math.Round(p.Y * s);
            return p;
        }
        public static Point add(this Point p, int x, int y)
        {
            p.X += x;// PointF is a struct, so 'this' doesn't really change!
            p.Y += y;
            return p;
        }
        public static Point addFTrunc(this Point p, float x, float y)
        {
            p.X += (int)x;// PointF is a struct, so 'this' doesn't really change!
            p.Y += (int)y;
            return p;
        }
        public static Point addFRound(this Point p, float x, float y)
        {
            p.X += (int)Math.Round(x);// PointF is a struct, so 'this' doesn't really change!
            p.Y += (int)Math.Round(y);
            return p;
        }

        public static PointF subtruct(this PointF p1, float x, float y)
        {
            p1.X -= x;// PointF is a struct, so 'this' doesn't really change!
            p1.Y -= y;
            return p1;
        }
        public static PointF subtruct(this PointF p1, PointF p2)
        {
            p1.X -= p2.X;// PointF is a struct, so 'this' doesn't really change!
            p1.Y -= p2.Y;
            return p1;
        }
        public static Point subtruct(this Point p1, Point p2)
        {
            p1.X -= p2.X;// PointF is a struct, so 'this' doesn't really change!
            p1.Y -= p2.Y;
            return p1;
        }
        public static Point subtruct(this Point p, int x, int y)
        {
            p.X -= x;// PointF is a struct, so 'this' doesn't really change!
            p.Y -= y;
            return p;
        }
        public static Point toPointTrunc(this PointF p)
        {
            return new Point((int)p.X, (int)p.Y);
        }
        public static Point toPointRound(this PointF p)
        {
            return new Point((int)Math.Round(p.X), (int)Math.Round(p.Y));
        }
        public static Point toPointRound(this PointF p, MidpointRounding r)
        {
            return new Point((int)Math.Round(p.X,r), (int)Math.Round(p.Y,r));
        }

        public static int manDist(this Point A, Point B)
        {
            return Math.Abs(A.X - B.X) + Math.Abs(A.Y - B.Y);
        }
        public static int manDist(this PointF A, PointF B)
        {
            return (int)(Math.Abs(A.X - B.X) + Math.Abs(A.Y - B.Y));
        }
        public static float manDistF(this PointF A, PointF B)
        {
            return Math.Abs(A.X - B.X) + Math.Abs(A.Y - B.Y);
        }

        public static float dotProduct(this PointF A, PointF B)
        {
            return A.X * B.X + A.Y * B.Y;
        }
        public static float dotProduct(this Point A, Point B)
        {
            return A.X * B.X + A.Y * B.Y;
        }
        public static float lenSqr(this Point p)
        {
            return p.X * p.X + p.Y * p.Y;
        }
        public static float lenSqr(this PointF p)
        {
            return p.X * p.X + p.Y * p.Y;
        }
        public static PointF getAbsCoords(this PointF p)
        {
            return new PointF(Math.Abs(p.X), Math.Abs(p.Y));
        }
        public static float maxCoord(this PointF p)
        {
            return Math.Max(p.X,p.Y);
        }
        public static float minCoord(this PointF p)
        {
            return Math.Min(p.X, p.Y);
        }

        public static List<Point> getGridPoints(bool skipFirst, bool skipLast, int r_p, int d, Point startPoint, int xDir, int yDir)
        {
            List<Point> res = new List<Point>();

            int iterCount = (int)Math.Ceiling(((float)d) / (r_p)) ;
            Point processedPoint = startPoint;

            if (!skipFirst)
                res.Add(processedPoint);
            if (skipLast)
                iterCount--;
            for (int i = 1; i < iterCount; ++i)
            {
                processedPoint = processedPoint.add(xDir * (r_p), yDir * (r_p));
                res.Add(processedPoint);
            }
            return res;
        }

        /// <summary>
        /// returns all points within a given radius (Point(0,0) is always the first in output list, if included)
        /// </summary>
        /// <param name="rad"></param>
        /// <returns></returns>
        public static List<Point> getAllPointsInArea(int rad, bool addCenterPoint = true, bool addQuarter1 = true, bool addQuarter2 = true, bool addQuarter3 = true, bool addQuarter4 = true)
        {
            List<Point> allPoints = new List<Point>(rad * (rad + 1) + 1);
            
            if (addCenterPoint)
                allPoints.Add(new Point(0, 0));

            if (addQuarter1)
                for(int d1 = -rad; d1 < 0; ++d1)
                    for (int d2 = -rad; d2 < 1; ++d2)
                    {
                        Point res = new Point(d1, d2);
                        if (res.X + res.Y < -rad)
                            res = new Point(-1 + (-rad - res.X), -rad - res.Y);
                        allPoints.Add(res);
                    }

            if (addQuarter2)
                for(int d1 = 0; d1 <= rad; ++d1)
                    for (int d2 = -rad; d2 < 0; ++d2)
                    {
                        Point res = new Point(d1, d2);
                        if (-res.Y + res.X > rad)
                            res = new Point(rad - res.X, -1 + (-rad - res.Y));
                        allPoints.Add(res);
                    }

            if (addQuarter3)
                for (int d1 = 1; d1 <= rad; ++d1)
                    for (int d2 = 0; d2 <= rad; ++d2)
                    {
                        Point res = new Point(d1, d2);
                        if (res.Y + res.X > rad)
                            res = new Point(1 + (rad - res.X), rad - res.Y);
                        allPoints.Add(res);
                    }

            if (addQuarter4)
                for (int d1 = -rad; d1 < 1; ++d1)
                    for (int d2 = 1; d2 <= rad; ++d2)
                    {
                        Point res = new Point(d1, d2);
                        if (res.Y - res.X > rad)
                            res = new Point((-rad - res.X), 1 + (rad - res.Y));
                        allPoints.Add(res);
                    }
            return allPoints;
        }

        /// <summary>
        /// returns a point in manhatten distance 1 to 'distance', in 'quarter'
        /// </summary>
        /// <param name="distance"></param>
        /// <param name="quarter"></param>
        /// value in [0,3] , which tells in which qurater the resulted point will be
        /// <param name="rand"></param>
        /// <returns></returns>
        public static Point getUniformRandomPointInManDistance(int distance, int quarter, Random rand)
        {
            switch (quarter)
            {
                case 0: // top left
                {
                    Point res = new Point(rand.Next(-distance, 0), rand.Next(-distance, 1));
                    if (res.X + res.Y < -distance)
                        res = new Point(-1 + (-distance - res.X), -distance - res.Y);
                    return res;
                }
                case 1:
                {
                    Point res = new Point(rand.Next(0, distance + 1), rand.Next(-distance, 0));
                    if (-res.Y + res.X > distance)
                        res = new Point(distance - res.X, -1 + (-distance - res.Y));
                    return res;
                }
                case 2:
                {
                    Point res = new Point(rand.Next(1, distance + 1), rand.Next(0, distance + 1));
                    if (res.Y + res.X > distance)
                        res = new Point(1 + (distance - res.X), distance - res.Y);
                    return res;
                }
                case 3:
                {
                    Point res = new Point(rand.Next(-distance, 1), rand.Next(1, distance + 1));
                    if (res.Y - res.X > distance)
                        res = new Point((-distance - res.X), 1 + (distance - res.Y));
                    return res;
                }
            }

            throw new Exception("getUniformRandomPointInManDistance(): unknown error");
        }
        /// <summary>
        /// returns a point in manhatten distance 0(or 1) to 'distance'
        /// </summary>
        /// <param name="distance"></param>
        /// <param name="rand"></param>
        /// <param name="includeZero">
        /// if true, the method returns a point in manhatten distance 0 to 'distance' away from point (0,0)
        /// </param>
        /// <returns></returns>
        public static Point getUniformRandomPointInManDistance(int distance, Random rand, bool includeZero = false)
        {
            // in order to achieve uniform division between the possible points, we get a random 
            // in a square distance X distance area, and if its outside the area of manhetten distance,
            // we map each point outside the area into a point inside the area (the amount of points inside and outside is exactly even, excluding center point)

            if (includeZero)
            {
                int PointCount = 2 * distance * (distance + 1) + 1;
                if (rand.Next(0, PointCount) == 0)
                    return new Point(0, 0); // if we enter the switch, all points are equally likely, but (0,0) is not an option, so we give it a chance here
            }
            return getUniformRandomPointInManDistance(distance, rand.Next(0, 4), rand);
            
        }



        public static int getGridGraphPointCount(int radius)
        {
            return 2 * radius * (radius + 1) + 1;
        }

        /// <summary>
        /// returns vertives of rectangle (clockwise)
        /// </summary>
        /// <param name="r"></param>
        /// <param name="vi">
        /// 0 to 3
        /// </param>
        /// <returns>
        /// 
        /// </returns>
        public static PointF getVertex(this RectangleF r, int vi)
        {
            switch(vi)
            {
                case 0: return new PointF(r.Left, r.Top);
                case 1: return new PointF(r.Right, r.Top);
                case 2: return new PointF(r.Right, r.Bottom);
                case 3: return new PointF(r.Left, r.Bottom);
            }
            throw new IndexOutOfRangeException();
        }
        public static PointF[] getVertices(this RectangleF r)
        {
            return new PointF[4] {
                new PointF(r.Left, r.Top),
                new PointF(r.Right, r.Top),
                new PointF(r.Right, r.Bottom),
                new PointF(r.Left, r.Bottom) };
        }
        public static RectangleF ensureInclusion(this RectangleF r, PointF p)
        {
            r.Y = Math.Min(r.Y, p.Y);
            r.Height = Math.Max(r.Height, p.Y - r.Y);
            
            r.X = Math.Min(r.X, p.X);
            r.Width = Math.Max(r.Width, p.X - r.X);
            return r;
        }
        
    }
}
