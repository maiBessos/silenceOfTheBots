using GoE.GameLogic;
using GoE.GameLogic.EvolutionaryStrategy;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GoE.Utils
{
    namespace Algorithms
    {
        /// <summary>
        /// generates an unlimited sequence of occurances with some constraints
        /// </summary>
        public class ConstrainedRandomSequence
        {
            private int occupiedPointIndex;
            private int pointInSequence;
            PatternRandomizer currentPattern;

            /// <summary>
            /// generates a repetitive pattern (using class PatternRandomizer) with occurancesCount events out of perExperimentsCount
            /// <param name="occurancesCount"></param>
            /// <param name="perExperimentsCount"></param>
            public ConstrainedRandomSequence(int occurancesCount, int perExperimentsCount)
            {
                currentPattern = new PatternRandomizer(perExperimentsCount, occurancesCount);
                currentPattern.Randomize(new ThreadSafeRandom(), true, -1, true);
                occupiedPointIndex = 0;
            }
            public ConstrainedRandomSequence(ConstrainedRandomSequence src)
            {
                occupiedPointIndex = src.occupiedPointIndex;
                pointInSequence = src.pointInSequence;
                currentPattern = new PatternRandomizer(src.currentPattern);
            }

            public bool checkOccourance()
            {
                //TODO: consider speeding this up by copying currentPattern.CurrentlyUsedPoints into a separate array with extra slot for final
                // false values
                return occupiedPointIndex < currentPattern.CurrentlyUsedPoints.Count &&
                    currentPattern.CurrentlyUsedPoints[occupiedPointIndex] == pointInSequence;
            }
            public void advance()
            {
                ++pointInSequence;
                if (pointInSequence >= currentPattern.totalPoints)
                {
                    pointInSequence = 0;
                    occupiedPointIndex = 0;
                    return;
                }

                // previous point was an occourance, so now we advance
                if (occupiedPointIndex < currentPattern.CurrentlyUsedPoints.Count &&
                    currentPattern.CurrentlyUsedPoints[occupiedPointIndex] == (pointInSequence - 1))
                    ++occupiedPointIndex;
            }

        }

        
        public class CoarsePointGrid
        {
            public Rectangle FullArea;
            public int AreaWidth { get; protected set; }
            public int AreaHeight { get; protected set; }
            public CoarsePointGrid(Rectangle fullArea, int areaWidth, int areaHeight)
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
        }
        /// <summary>
        /// a faster replacement for HashSet<Point>, optimized for small sets (less than 100) that are uniformly distributed
        /// </summary>
        public class PointSet
        {
            List<List<Point>> points;
            int minX, width;

            /// <summary>
            /// returns a raw structure that contains all the points in the set (some lists may be empty)
            /// </summary>
            public List<List<Point>> AllPoints { get { return points; } }
            
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
                    points[(p.X - minX) / width].Add(p);

            }
            public int Count { get; protected set; }
            public bool Contains(Point p)
            {
                int x = (p.X - minX);
                if (x < 0)
                    return false;

                int cellX = (p.X - minX) / width;
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
                int x = (p.X - minX);
                if (x < 0)
                    return false;

                int cellX = (p.X - minX) / width;
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
        public struct OptimizedObj<T>
        {
            public T data;
            public double value;

            //C'tor: since OptimizedObj is a struct and T has no guarentees, we can't define a c'tor that doesn't gets an initial T data

            public bool setIfValueIncreases(OptimizedObj<T> cmp)
            {
                if (value < cmp.value)
                {
                    value = cmp.value;
                    data = cmp.data;
                    return true;
                }
                return false;
            }
            public bool setIfValueIncreases(T Data, double Val)
            {
                if (this.value < Val)
                {
                    this.value = Val;
                    this.data = Data;
                    return true;
                }
                return false;
            }
            public bool setIfValueDecreases(OptimizedObj<T> cmp)
            {
                if (value > cmp.value)
                {
                    value = cmp.value;
                    data = cmp.data;
                    return true;
                }
                return false;
            }
            public bool setIfValueDecreases(T Data, double Val)
            {
                if (this.value > Val)
                {
                    this.value = Val;
                    this.data = Data;
                    return true;
                }
                return false;
            }
        }
        /// <summary>
        /// used for circular patrol - 
        /// given x points in which pursuers can be, only y are occupied each round.
        /// we want the y points to change each round (with zero intersection) - this is what this utility does
        /// </summary>
        public class PatternRandomizer
        {
            /// <summary>
            /// UNRANDOMIZED pattern is created -  
            /// Initializes the pattern for occupying the first 'occupiedPoints'
            /// </summary>
            /// <param name="len"></param>
            public PatternRandomizer(int Points, int OccupiedPoints)
            {

                AvailablePoints = new List<int>();
                CurrentlyUsedPoints = new List<int>();

                this.totalPoints = Points;
                this.occupiedPoints = OccupiedPoints;
                PreviouslyUsedPoints = null;

                for (int i = occupiedPoints; i < Points; ++i)
                    AvailablePoints.Add(i);
                for (int i = 0; i < occupiedPoints; ++i)
                    CurrentlyUsedPoints.Add(i);

            }
            public PatternRandomizer(PatternRandomizer src)
            {
                totalPoints = src.totalPoints;
                occupiedPoints = src.occupiedPoints;
                AvailablePoints = new List<int>(src.AvailablePoints);
                CurrentlyUsedPoints = new List<int>(src.CurrentlyUsedPoints);
                PreviouslyUsedPoints = new List<int>(src.PreviouslyUsedPoints);
            }
            /// <summary>
            /// 
            /// </summary>
            /// <param name="rand"></param>
            /// <param name="sortCurrentlyUsedPoints"></param>
            /// <param name="newOccupiedPoints">
            /// this tells how many should be occupied after invocation, out of the currently avilable points in the pattern
            /// </param>
            virtual public void Randomize(ThreadSafeRandom rand, bool sortCurrentlyUsedPoints = true, int newOccupiedPoints = -1, bool makeAllPointsAvailable = false)
            {
                if (newOccupiedPoints != -1)
                    occupiedPoints = newOccupiedPoints;

                PreviouslyUsedPoints = CurrentlyUsedPoints;
                CurrentlyUsedPoints = new List<int>(occupiedPoints);

                if (makeAllPointsAvailable)
                {
                    AvailablePoints = new List<int>(totalPoints);
                    for (int i = 0; i < totalPoints; ++i)
                        AvailablePoints.Add(i);
                }

                // generate the list of next points to occupy
                List<int> indices = new List<int>(occupiedPoints);
                for (int j = 0; j < occupiedPoints; ++j)
                    indices.Add(AvailablePoints.popRandomItem(rand.rand));

                // on next round, all points are avilable (until we exclude indices[] )
                AvailablePoints = new List<int>(totalPoints);
                for (int i = 0; i < totalPoints; ++i)
                    AvailablePoints.Add(i);

                for (int j = 0; j < occupiedPoints; ++j)
                {
                    int targetIndex = indices[j];
                    CurrentlyUsedPoints.Add(targetIndex);
                    AvailablePoints[targetIndex] = -1;
                }
                if (sortCurrentlyUsedPoints)
                    CurrentlyUsedPoints.Sort();

                for (int i = AvailablePoints.Count() - 1; i >= 0; --i)
                    if (AvailablePoints[i] == -1)
                    {
                        // we don't mind messing the order of the list, since it is used in random anyway
                        AvailablePoints[i] = AvailablePoints.Last();
                        AvailablePoints.RemoveAt(AvailablePoints.Count - 1);
                    }
            }

            public List<int> AvailablePoints { get; protected set; }
            public List<int> CurrentlyUsedPoints { get; protected set; }
            public List<int> PreviouslyUsedPoints { get; protected set; }
            public int totalPoints { get; protected set; }
            public int occupiedPoints { get; protected set; }
        }

        /// <summary>
        /// it is assumed that PatternCount <= OccupiedPoints/2, and on average no more than 1 point
        /// on each pattern
        /// </summary>
        public class MultiPatternRandomizer
        {

            public List<PatternRandomizer> PatternPerLayer { get; protected set; }

            /// <summary>
            /// OccupiedPoints < PatternsSize/2
            /// </summary>
            /// <param name="AvgPointsCount"></param>
            /// <param name="PatternsSize"></param>
            /// <param name="OccupiedPoints"></param>
            public MultiPatternRandomizer(List<float> AvgPointsCount, int PatternsSize, int OccupiedPoints)
            {
                this.avgPointsCount = AvgPointsCount;
                this.patternsSize = PatternsSize;
                this.occupiedPoints = OccupiedPoints;
                this.PatternPerLayer = new List<PatternRandomizer>(AvgPointsCount.Count);

                int remainingPoints = OccupiedPoints;
                for (int i = 0; i < AvgPointsCount.Count; ++i)
                {
                    int points = (int)Math.Ceiling(AvgPointsCount[i]);
                    points = Math.Max(remainingPoints, points);
                    PatternPerLayer.Add(new PatternRandomizer(PatternsSize, points));
                    remainingPoints -= points;
                }
            }
            public void Randomize(ThreadSafeRandom rand)
            {
                int remainingPoints = occupiedPoints;
                for (int i = 1; i < avgPointsCount.Count; ++i)
                {
                    int points = (int)Math.Floor(avgPointsCount[i]);
                    if (rand.NextDouble() < (avgPointsCount[i] - points))
                        ++points; // points is avgPointsCount[i] on average
                    points = Math.Min(remainingPoints, points);
                    PatternPerLayer[i].Randomize(rand, true, points);
                    remainingPoints -= points;
                }
                // remainingPoints is on average exactly the number we want

                PatternPerLayer.First().Randomize(rand, true, remainingPoints);
                // this is a little dirty:
                // in practice, the first layer has more avg points than the next one, and so forth, 
                // and actually the other layers represent fewer points than avgPointsCount[] tells.
                // since the first layer is the only layer that may sometime actually get significantly more points 
                // than it's avg, we want to make sure it practically can hold all these pooints
            }

            private List<float> avgPointsCount;
            private int patternsSize;
            private int occupiedPoints;

        }

        // NOTE: AreaPatternRandomizer is deprecated
        ///// <summary>
        ///// similar to PatternRandomizer, but allows choosing random points in an area instead of a line
        ///// </summary>
        //public class AreaPatternRandomizer
        //{
        //    /// <summary>
        //    /// encapsulates several patterns, each pattern for a separate Layer,
        //    /// where some amount of points are scattered in all patterns together
        //    /// </summary>
        //    /// <param name="LayerFactors">
        //    /// the smallest layer has factor 1 , and if a layer is essentialy x times as more points, it should have factor x
        //    /// </param>
        //    public AreaPatternRandomizer(List<float> LayerFactors, int PointsPerLayer, int OccupiedPoints)
        //    {
        //        this.PatternPerLayer = new List<PatternRandomizer>();
        //        this.layerFrequencies = new List<float>(LayerFactors.Count);
        //        this.layerFactors = new List<float>(LayerFactors);
        //        this.nextAvgPointsPerLayer = new List<float>(LayerFactors.Count);
        //        this.points = PointsPerLayer;
        //        this.totalOccupiedPoints = OccupiedPoints;
        //        this.frequencySum = 0;
        //        int remainingPoints = OccupiedPoints;

        //        for(int rI = 0; rI < layerFactors.Count - 1; ++rI)
        //        {
        //            int LayerPoints = (int)Math.Ceiling(layerFactors[rI]);
        //            remainingPoints -= LayerPoints;
        //            PatternPerLayer.Add(new PatternRandomizer(PointsPerLayer, LayerPoints));
        //            layerFrequencies.Add((layerFactors[rI] / (points - layerFactors[rI])));
        //            frequencySum += layerFrequencies.Last();
        //            nextAvgPointsPerLayer.Add(getNextAvgPointsPerLayer(LayerPoints, rI));
        //        }

        //        PatternPerLayer.Add(new PatternRandomizer(PointsPerLayer, remainingPoints));
        //        layerFrequencies.Add((layerFactors[layerFactors.Count - 1] / (points - layerFactors[layerFactors.Count - 1])));
        //        frequencySum += layerFrequencies.Last();
        //        nextAvgPointsPerLayer.Add(getNextAvgPointsPerLayer(remainingPoints, layerFactors.Count - 1));
        //        refreshTotalFactoredPoints();
        //    }

        //    public void refreshTotalFactoredPoints()
        //    {
        //        totalFactoredPoints = 0;
        //        for (int lI = 0; lI < PatternPerLayer.Count; ++lI)
        //            totalFactoredPoints += layerFactors[lI] *(points - PatternPerLayer[lI].occupiedPoints);

        //        if (totalFactoredPoints <= 0) // TOO remove
        //            while (true) ;
        //    }
        //    public void Randomize(ThreadSafeRandom rand, bool sortCurrentlyUsedPoints = true)
        //    {
        //        var pointCount = randPointsPerLayer(rand);
        //        for (int i = 0; i < PatternPerLayer.Count; ++i)
        //            PatternPerLayer[i].Randomize(rand, sortCurrentlyUsedPoints, pointCount[i]);
        //        refreshTotalFactoredPoints();
        //    }
        //    public List<PatternRandomizer> PatternPerLayer { get; protected set; }


        //    double totalFactoredPoints = 0;
        //    private List<int> randPointsPerLayer(ThreadSafeRandom rand)
        //    {
        //        List<int> res = Utils.AlgorithmUtils.getRepeatingValueList(0, this.PatternPerLayer.Count);

        //        //float avgOccupiedPointsPerLayer = ((float)this.totalOccupiedPoints) / PatternPerLayer.Count;


        //        for (int pI = 0; pI < totalOccupiedPoints; ++pI)
        //        {
        //            //double pointsSum = 0;
        //            double location = rand.NextDouble() * totalFactoredPoints;


        //            for (int lI = 0; lI < PatternPerLayer.Count; ++lI)
        //            {
        //               // float pointsToAddToLayer =
        //                       //(avgPointsPerLayer[lI] / (totalOccupiedPoints))  *
        //                       //(layerFrequencies[lI] / frequencySum )*
        //                       //nextAvgPointsPerLayer[lI] *
        //                      // (points - PatternPerLayer[lI].CurrentlyUsedPoints.Count - res[lI]);

        //                //float currentLayerPointsWeight =
        //                //    avgPointsPerLayer[lI] / (avgOccupiedPointsPerLayer);
        //                //if (location <= pointsToAddToLayer * currentLayerPointsWeight)

        //                double layerFactoredPoints = layerFactors[lI]* 
        //                    (points - PatternPerLayer[lI].CurrentlyUsedPoints.Count - res[lI]);
        //                if (location < layerFactoredPoints)
        //                {
        //                    totalFactoredPoints -= layerFactors[lI];
        //                    ++res[lI];

        //                    break;
        //                }

        //                location -= layerFactoredPoints;
        //            }


        //        }

        //        // at first I attempted a more elegant solution - since 
        //        // we just want a specific average amount of points in each layer,
        //        // we can just randomly assign each point to layers according to the total avilable points + 
        //        // 

        //        //int remainingPoints;

        //        //List<int> res = new List<int>(PatternPerLayer.Count);
        //        //do
        //        //{
        //        //    res.Clear();
        //        //    remainingPoints = totalOccupiedPoints;
        //        //    int i;
        //        //    for (i = 0; i < nextAvgPointsPerLayer.Count - 1; ++i)
        //        //    {
        //        //        int avilablePointsInLayer =
        //        //            (points - PatternPerLayer[i].CurrentlyUsedPoints.Count);

        //        //        // LayerPoints should be a random number between 0 and avilablePointsInLayer,
        //        //        // with average nextAvgPointsPerLayer[i]

        //        //        //int LayerPoints = (int)(nextAvgPointsPerLayer[i] * (rand.generateNormalDistNumber() + 1));
        //        //        int LayerPoints;
        //        //        double layerPointsFactor = rand.NextDouble();
        //        //        if (layerPointsFactor < 0.5)
        //        //            LayerPoints = (int)Math.Round( (layerPointsFactor / 0.5) * nextAvgPointsPerLayer[i]);
        //        //        else
        //        //            LayerPoints = (int)Math.Round(nextAvgPointsPerLayer[i] +
        //        //                ((layerPointsFactor - 0.5) / 0.5) * (avilablePointsInLayer-nextAvgPointsPerLayer[i]));

        //        //        LayerPoints = Math.Min(remainingPoints, LayerPoints);
        //        //        remainingPoints -= LayerPoints;

        //        //        res.Add(LayerPoints);
        //        //    }

        //        //    // we retry, until we dont have excessive points in the last layer
        //        //}
        //        //while (remainingPoints > (points - PatternPerLayer.Last().CurrentlyUsedPoints.Count));

        //        //res.Add(remainingPoints);
        //        //for (int i = 0; i < nextAvgPointsPerLayer.Count - 1; ++i)
        //        //    nextAvgPointsPerLayer[i] = getNextAvgPointsPerLayer(res[i], i);


        //            return res;
        //    }

        //    private float getNextAvgPointsPerLayer(int currentLayerPointCount, int layerIdx)
        //    {
        //        // if the Layer has avgPointsPerLayer[i] points in it now, we expect it to have 
        //        // avgPointsPerLayer[i] on the next round as well
        //        return layerFrequencies[layerIdx] * (points - currentLayerPointCount);
        //    }
        //    private float frequencySum;
        //    private List<float> layerFrequencies;
        //    private int points;
        //    private int totalOccupiedPoints;
        //    private List<float> layerFactors;
        //    private List<float> nextAvgPointsPerLayer;

        //    private List<float> pointsFactor; // the smallest layer has 1 
        //}
        // TODO: replace with a faster implementation
        class Dijkstra
        {
            public delegate int weights(int from, int to);

            // taken from http://www.codeproject.com/Articles/19919/Shortest-Path-Problem-Dijkstra-s-Algorithm
            private int rank = 0;
            private int[,] L;
            private int[] C;
            public int[] D;
            private int trank = 0;
            public Dijkstra(int paramRank, weights paramArray)
            {
                L = new int[paramRank, paramRank];
                C = new int[paramRank];
                D = new int[paramRank];
                rank = paramRank;
                for (int i = 0; i < rank; i++)
                {
                    for (int j = 0; j < rank; j++)
                    {
                        L[i, j] = paramArray(i, j);
                    }
                }

                for (int i = 0; i < rank; i++)
                {
                    C[i] = i;
                }
                C[0] = -1;
                for (int i = 1; i < rank; i++)
                    D[i] = L[0, i];
            }
            public void DijkstraSolving()
            {
                int minValue = Int32.MaxValue;
                int minNode = 0;
                for (int i = 0; i < rank; i++)
                {
                    if (C[i] == -1)
                        continue;
                    if (D[i] > 0 && D[i] < minValue)
                    {
                        minValue = D[i];
                        minNode = i;
                    }
                }
                C[minNode] = -1;
                for (int i = 0; i < rank; i++)
                {
                    if (L[minNode, i] < 0)
                        continue;
                    if (D[i] < 0)
                    {
                        D[i] = minValue + L[minNode, i];
                        continue;
                    }
                    if ((D[minNode] + L[minNode, i]) < D[i])
                        D[i] = minValue + L[minNode, i];
                }
            }
            public void Run()
            {
                for (trank = 1; trank < rank; trank++)
                {
                    DijkstraSolving();
                    Console.WriteLine("iteration" + trank);
                    for (int i = 0; i < rank; i++)
                        Console.Write(D[i] + " ");
                    Console.WriteLine("");
                    for (int i = 0; i < rank; i++)
                        Console.Write(C[i] + " ");
                    Console.WriteLine("");
                }
            }

        }
        public static class AlgorithmUtils
        {


            /// <summary>
            /// generates a list that tells for each slot how many items it should have,
            /// such that the sum of items in all slot is 'itemsToDivide'
            /// </summary>
            /// <param name="itemsToDivide"></param>
            /// <param name="slotCount"></param>
            /// <returns></returns>
            public static List<int> getRandomDvision(int itemsToDivide, int slotCount, Random rng)
            {
                // TODO: perhaps should be optimized
                List<int> res = getRepeatingValueList(0, slotCount);
                for (int i = 0; i < itemsToDivide; ++slotCount)
                    ++res[rng.Next(slotCount)];
                return res;
            }

            /// <summary>
            /// turns an array of weight values in [0,1]  into portions, which 
            /// are summed up to 1. each portion value can't be smaller than 'minimalPortion',
            /// but the values keep the same proportions they had (as closely as possible)
            /// </summary>
            /// <param name="itemsPerSlot"></param>
            /// <returns></returns>
            public static List<double> translateWeightsToPortions(List<double> weights, double minimalPortion)
            {
                List<double> res = new List<double>(weights);
                double maxWeight = 0, sumWeight = 0;
                for (int i = 0; i < weights.Count; ++i)
                {
                    maxWeight = Math.Max(maxWeight, weights[i]);
                    sumWeight += weights[i];
                }

                for (int i = 0; i < weights.Count; ++i)
                    res[i] = weights[i] / sumWeight;

                double excess = 0, unfixedSum = 0;
                for (int i = 0; i < weights.Count; ++i)
                    if (res[i] < minimalPortion)
                    {
                        excess += (minimalPortion - res[i]);
                        res[i] = minimalPortion;
                    }
                    else
                        unfixedSum += res[i];
                double fixedSum = 1 - excess / unfixedSum;
                for (int i = 0; i < weights.Count; ++i)
                    if (res[i] > minimalPortion)
                        res[i] *= fixedSum;

                //// TODO: remove below
                //double s = 0;
                //for (int i = 0; i < weights.Count; ++i)
                //    s += res[i];
                //if(Math.Abs(s-1) > 0.01)
                //{
                //    while (true) ;
                //}
                return res;
            }

            /// <summary>
            /// assumes all values in vals are in [0,1], and returns a vector of values with a sum of 1.
            /// Example: [0.5,0.5] -> [0.5,0.25,0.25] 
            /// </summary>
            /// <returns></returns>
            public static List<Double> getNormalizedValues(double[] vals, int startIdx = 0)
            {
                List<Double> res = new List<double>();
                double remaining = 1;
                for (int i = startIdx; i < vals.Count(); ++i)
                {
                    res.Add(remaining * vals[i]);
                    remaining -= res.Last();
                }
                res.Add(remaining);
                return res;
            }

            // opposite of getNormalizedValues()
            public static List<double> denormalizeValues(List<double> vals)
            {
                List<Double> res = new List<double>();
                res.Add(vals.First());
                for(int i = 1; i < vals.Count-1; ++i)
                {
                    if (res[i - 1] == 0)
                        res.Add(0); // if vals is (e.g.) of the form [1,0,0,0], then at some point res will get division by zero. this avoids it
                    else
                        res.Add(vals[i] / res[i - 1]);
                }
                //List<Double> res = new List<double>();
                //double remaining = vals.Last();

                //for (int i = vals.Count()-2; i >= 0; --i)
                //{
                //    res.Add(remaining / vals[i]);
                //    remaining += res.Last();
                //}
                //res.Add(remaining);
                return res;
            }

            public static void Swap<T>(ref T lhs, ref T rhs)
            {
                T temp;
                temp = lhs;
                lhs = rhs;
                rhs = temp;
            }


            /// <summary>
            /// chooses an items in random from the list and returns it (also, removes it from the list by switching the data with
            /// last item, then decreaseing list size i.e. O(1) ) 
            /// </summary>
            /// <typeparam name="T"></typeparam>
            /// <param name="items"></param>
            /// <param name="rand"></param>
            /// <returns></returns>
            public static T popRandomItem<T>(this List<T> items, Random rand)
            {
                int r = rand.Next(0, items.Count);
                T res = items[r];
                items[r] = items.Last();
                items.RemoveAt(items.Count - 1);
                return res;
            }
            public static T chooseRandomItem<T>(this List<T> items, Random rand)
            {
                int r = rand.Next(0, items.Count);
                T res = items[r];
                return res;
            }

            public static double Average(this List<int> vals)
            {
                double sum = 0;
                foreach (var v in vals)
                    sum += v;
                return sum / vals.Count;
            }

            public static double Average(this List<float> vals)
            {
                double sum = 0;
                foreach (var v in vals)
                    sum += v;
                return sum / vals.Count;
            }

            public static double Average(this List<double> vals)
            {
                double sum = 0;
                foreach (var v in vals)
                    sum += v;
                return sum / vals.Count;
            }

            
            /// <summary>
            /// assumes the type of the values in the dictionary is float, then returns a float summary of it
            /// </summary>
            /// <typeparam name="T"></typeparam>
            /// <param name="vals"></param>
            /// <returns></returns>
            public static DictionaryType Average<DictionaryType>(List<DictionaryType> vals, bool addStdDeviation = true, int distributionSegments = 20) where DictionaryType : Dictionary<string, string>, new()
            {
                // TODO: consider using linq for this and similar table operations
                DictionaryType res = new DictionaryType();
                
                foreach (var key in vals.First())
                {
                    float sum = 0;
                    foreach (var dic in vals)
                    {
                        float dicv = float.Parse(dic[key.Key]);
                        if (!MathEx.makeFinite(ref dicv))
                            continue;
                        sum += dicv;
                    }

                    res[key.Key] = (sum / vals.Count).ToString();
                }

                if (addStdDeviation)
                {
                    foreach (var key in vals.First())
                    {
                        double distSum = 0;
                        foreach (var dic in vals)
                        {
                            float dicv = float.Parse(dic[key.Key]);
                            float resv = float.Parse(res[key.Key]);

                            if (!MathEx.makeFinite(ref dicv) || !MathEx.makeFinite(ref resv))
                                continue;

                            distSum += Math.Pow(dicv - resv, 2);
                        }

                        double stddev = Math.Sqrt(distSum / vals.Count);
                        res[AppConstants.GameProcess.Statistics.STANDARD_DEVIATION_PREFIX + key.Key] =
                            stddev.ToString();

                        res[AppConstants.GameProcess.Statistics.CONF_INTERVAL + key.Key] =
                            (2.58f * (stddev/Math.Sqrt(vals.Count))).ToString();
                    }
                }

                if(distributionSegments > 0)
                {
                    foreach (var key in vals.First())
                    {
                        float segSize;
                        List<float> perSeg = AlgorithmUtils.getRepeatingValueList(0.0f, distributionSegments);
                        float min = float.MaxValue;
                        float max = -float.MaxValue;
                        foreach (var dic in vals)
                        {
                            float v = float.Parse(dic[key.Key]);
                            if (!MathEx.makeFinite(ref v))
                            {
                                max = min = 0;
                                break;
                            }

                            min = Math.Min(min, v);
                            max = Math.Max(max, v);
                        }
                        if (min == max)
                            continue;
                        segSize = (max - min) / distributionSegments;
                        foreach (var dic in vals)
                        {
                            float v = float.Parse(dic[key.Key]);
                            perSeg[(int)(Math.Min(perSeg.Count - 1, v / segSize))] += 1.0f / vals.Count;
                        }
                        
                        res[AppConstants.GameProcess.Statistics.DISTRIBUTION + "_" + key.Key] = ParsingUtils.makeCSV(perSeg, 0, false);
                        res[AppConstants.GameProcess.Statistics.MAX + "_" + key.Key] = max.ToString();
                    }
                }

                return res;
            }

            public static void Populate<T>(this T[] arr, T value)
            {
                for (int i = 0; i < arr.Length; i++)
                {
                    arr[i] = value;
                }
            }

            public static T[] getRepeatingValueArr<T>(T value, int len) where T : new()
            {
                T[] res = new T[len];
                for (int i = 0; i < len; ++i)
                    res[i] = value;
                return res;
            }

            /// <summary>
            /// copies the same instance multiple times into the newly constructed List (note - class objects will not be duplicated, only REFERENCES 
            /// are duplicated)
            /// </summary>
            /// <typeparam name="T"></typeparam>
            /// <param name="value"></param>
            /// <param name="len"></param>
            /// <returns></returns>
            public static List<T> getRepeatingValueList<T>(T value, int len)
            {
                List<T> res = new List<T>(len);
                for (int i = 0; i < len; ++i)
                    res.Add(value);
                return res;
            }

            /// <summary>
            /// clones the value several times.
            /// value itself will be optionally added to result 
            /// </summary>
            /// <typeparam name="T"></typeparam>
            /// <param name="value"></param>
            /// <param name="len"></param>
            /// <param name="addValueToList">
            /// if true, value will be added at the begining of the list
            /// </param>
            /// <returns></returns>
            public static List<T> getRepeatingClonedValueList<T>(T value, int len, bool addValueToList = false) where T : ICloneable
            {
                return getRepeatingClonedValueList<T, T>(value, len, addValueToList);
            }
            public static T getContainingCollection<T, K>(K item) where T : ICollection<K>, new()
            {
                T res = new T();
                res.Add(item);
                return res;
            }
            public static List<T> getRepeatingValueList<T>(int len, Func<T> valueGenerator)
            {
                List<T> res = new List<T>(len);
                for (int i = 0; i < len; ++i)
                    res.Add(valueGenerator());
                return res;
            }
            public static List<T> getRepeatingClonedStructList<T>(T value, int len, bool addValueToList = false) where T : struct
            {
                List<T> res = new List<T>(len);

                if (addValueToList)
                    res.Add((T)value);

                for (int i = 0; i < len; ++i)
                    res.Add(value);
                return res;
            }

            /// <summary>
            /// similar to getRepeatingClonedValueList<T>(), but also allows getting a List<> of type 'TypeTo'
            /// which value (of type TypeFrom) may be casted to
            /// </summary>
            public static List<TypeTo> getRepeatingClonedValueList<TypeTo, TypeFrom>(TypeFrom value, int len, bool addValueToList = false) where TypeFrom : ICloneable, TypeTo
            {
                List<TypeTo> res = new List<TypeTo>(len);

                if (addValueToList)
                    res.Add((TypeTo)value);

                for (int i = 0; i < len; ++i)
                    res.Add((TypeTo)value.Clone());
                return res;
            }

            public static List<T> getRepeatingValueList<T>(int len) where T : new()
            {
                List<T> res = new List<T>(len);
                for (int i = 0; i < len; ++i)
                    res.Add(new T());
                return res;
            }
        }
    }
}
