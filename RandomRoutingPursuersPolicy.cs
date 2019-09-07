using GoE.GameLogic;

using GoE.UI;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GoE.Utils;
using GoE.GameLogic.EvolutionaryStrategy;
using GoE.AppConstants;
using GoE.Utils.Algorithms;
using GoE.AppConstants.Policies.Routing;

namespace GoE.Policies
{
    /// <summary>
    /// in this policy, patrollers repeatedly scan the graph to remove evaders.
    /// probably the best way to react evaders that are entirely random
    /// </summary>
    public class ScanningRoutingPursuersPolicy : AFrontsGridRoutingPursuersPolicy
    {
        protected FrontsGridRoutingGameParams param;
        protected LinkedList<Point> nextPointList;
        public override List<ArgEntry> policyInputKeys()
        {
            //get
            //{
                return ReflectionUtils.getStaticInstancesInClass<ArgEntry>(typeof(ScanningRoutingPursuers));
            //}
        }

        public override List<Point> getNextStep()
        {
            List<Point> res = new List<Point>();

            for (int pi = 0; pi < param.A_P.Count; ++pi)
            {
                res.Add(nextPointList.First.Value);
                nextPointList.AddLast(nextPointList.First.Value);
                nextPointList.RemoveFirst();
            }
            return res;
        }

        public override bool init(AGameGraph iG, FrontsGridRoutingGameParams prm, IPolicyGUIInputProvider pgui, Dictionary<string, string> policyParams)
        {
            this.param = prm;
            nextPointList = new LinkedList<Point>();
            GridGameGraph G = (GridGameGraph)iG;
            if (ScanningRoutingPursuers.DIAGONAL_SCAN.tryRead(policyParams) == "1")
            {
                for (int i = 0; i < G.WidthCellCount; ++i)
                    for (int j = 0; j <= Math.Min(i,G.HeightCellCount-1); ++j)
                        nextPointList.AddLast(new Point(i-j, j));

                for (int i = 1; i < G.HeightCellCount; ++i)
                    for (int j = (int)G.WidthCellCount - 1; j >= Math.Max((int)G.WidthCellCount - G.HeightCellCount+i,i); --j)
                        nextPointList.AddLast(new Point(j,i - (j - (int)G.WidthCellCount) - 1 ));
            }
            else
            {
                for (int i = 0; i < G.WidthCellCount; ++i)
                    for (int j = 0; j < G.HeightCellCount; ++j)
                        nextPointList.AddLast(new Point(i, j));
            }
            return true;
        }

        public override void setGameState(int currentRound, IEnumerable<Point> O_c, IEnumerable<GameLogic.Utils.CapturedObservation> O_d)
        {
            
        }
    }
    public class RandomRoutingPursuersPolicy : ScanningRoutingPursuersPolicy
    {
        protected ThreadSafeRandom myRand;
        public override bool init(AGameGraph iG, FrontsGridRoutingGameParams prm, IPolicyGUIInputProvider pgui, Dictionary<string, string> policyParams)
        {
            myRand = new ThreadSafeRandom();
            this.param = prm;
            List<Point> randomizedPointList = new List<Point>();

            GridGameGraph G = (GridGameGraph)iG;

            for (int i = 0; i < G.WidthCellCount; ++i)
                for (int j = 0; j < G.HeightCellCount; ++j)
                    randomizedPointList.Add(new Point(i, j));
            // after enough value swaps, the list is randomized:
            for (int i = 0; i < G.WidthCellCount * G.HeightCellCount; ++i)
            {
                int tmp1i = myRand.Next() % randomizedPointList.Count, tmp2i = myRand.Next() % randomizedPointList.Count;
                Point tmp;
                tmp = randomizedPointList[tmp1i];
                randomizedPointList[tmp1i] = randomizedPointList[tmp2i];
                randomizedPointList[tmp2i] = tmp;
            }

            nextPointList = new LinkedList<Point>();
            foreach (var p in randomizedPointList)
                nextPointList.AddLast(p);
            return true;
        }
    }

    public class ColumnProbabalisticRoutingPursuersPolicy : AFrontsGridRoutingPursuersPolicy
    {
        protected List<double> probPerColGroup;
        protected List<List<Point>> colGroups; // for each column group, contains all points
        
        protected double repeatSuccessProb;
        protected GridGameGraph graph;
        protected FrontsGridRoutingGameParams gameParams;
        protected List<int> columnIdxToColGroup;
        protected IEnumerable<GameLogic.Utils.CapturedObservation> succeededPoints;
        protected ThreadSafeRandom myRand;

        public override List<ArgEntry> policyInputKeys()
        {
            //get
            //{
                return ReflectionUtils.getStaticInstancesInClass<ArgEntry>(typeof(ColumnProbabalisticRoutingPursuers));
            //}
        }

        public override List<Point> getNextStep()
        {
            List<Point> res = new List<Point>();
            int pi = 0;

            foreach (var sp in succeededPoints)
            {
                if(myRand.NextDouble() <= repeatSuccessProb)
                {
                    ++pi;
                    int colGroup = columnIdxToColGroup[sp.where.X];
                    res.Add(colGroups[colGroup].chooseRandomItem(myRand.rand));
                }
            }

            for (; pi < gameParams.A_P.Count; ++pi)
            {
                int groupCol = 0;
                double groupProb = myRand.NextDouble();
                while (groupCol < probPerColGroup.Count && groupProb > probPerColGroup[groupCol])
                    groupProb -= probPerColGroup[groupCol++];
                if (groupCol == probPerColGroup.Count)
                    groupCol = probPerColGroup.Count - 1;
                res.Add(colGroups[groupCol].chooseRandomItem(myRand.rand));
            }

            return res;
        }

        public override bool init(AGameGraph G, FrontsGridRoutingGameParams prm, IPolicyGUIInputProvider pgui, Dictionary<string, string> policyParams)
        {
            graph = (GridGameGraph)G;
            gameParams = prm;
            myRand = new ThreadSafeRandom();
            repeatSuccessProb = double.Parse(
                ColumnProbabalisticRoutingPursuers.REPEAT_SUCCUSSFULL_COLUMN_HIT.tryRead(policyParams));
            
            var probStrings =
                ParsingUtils.separateCSV(ColumnProbabalisticRoutingPursuers.PROBABILITY_PER_COLUMN.tryRead(policyParams));
            probPerColGroup = probStrings.ConvertAll((s) => { return double.Parse(s); });
            
            colGroups = new List<List<Point>>();
            int colIdxStart = 0;
            columnIdxToColGroup = new List<int>();
            for (int colGroup = 0; colGroup < probStrings.Count; ++colGroup)
            {
                int colIdxEnd = (int)Math.Min(graph.WidthCellCount,
                    (int)Math.Round((1+colGroup) * ((float)(graph.WidthCellCount) / probStrings.Count)));

                colGroups.Add(new List<Point>());
                for (; colIdxStart < colIdxEnd; ++colIdxStart)
                {
                    columnIdxToColGroup.Add(colGroup);
                    for (int y = 0; y < graph.HeightCellCount; ++y)
                        colGroups.Last().Add(new Point(colIdxStart, y));
                }
                colIdxStart = colIdxEnd;
            }
            

            return true;
        }

        public override void setGameState(int currentRound, 
                                          IEnumerable<Point> O_c, 
                                          IEnumerable<GameLogic.Utils.CapturedObservation> O_d)
        {
            succeededPoints = O_d;
        }
    }
    
    public class RowDestroyingPursuerPolicy : AFrontsGridRoutingPursuersPolicy
    {
        void addIfLegal(List<Point> pointsList, Point p)
        {
            if (p.X < 0 || p.X >= graph.WidthCellCount)
                return;
            pointsList.Add(p);
        }
        GridGameGraph graph;
        FrontsGridRoutingGameParams param;
        List<Point> rightPoints, waitingRightPoints, leftPoints, waitingLeftPoints;
        double stopScanProb;
        double dualSearchProb;
        public override List<ArgEntry> policyInputKeys()
        {
            //get
            //{
                var res = new List<ArgEntry>();
                res.Add(AppConstants.Policies.Routing.RowDestroyingPursuerPolicy.STOP_SCAN_ONFAILURE_PROBABILITY);
                res.Add(AppConstants.Policies.Routing.RowDestroyingPursuerPolicy.DUAL_DIRECTION_SEACH_PROB);
                return res;

           // }
        }

        ThreadSafeRandom myRand;
       
        public override List<Point> getNextStep()
        {
            List<Point> res = new List<Point>();
            int pi = 0;


            List<Point> nextLeft = new List<Point>();
            List<Point> nextRight = new List<Point>();
            foreach (Point p in od)
            {
                if(rightPoints.Contains(p))
                {
                    rightPoints.Remove(p);
                    addIfLegal(nextRight, p.add(-1, 0));
                }
                else if(leftPoints.Contains(p))
                {
                    leftPoints.Remove(p);
                    addIfLegal(nextLeft,p.add(1, 0));
                }
                else
                {
                    // add to at least one direction
                    if (myRand.NextDouble() < dualSearchProb)
                    {
                        addIfLegal(nextLeft, p.add(1, 0));
                        addIfLegal(nextRight, p.add(-1, 0));
                    }
                    else
                    {
                        if(myRand.NextDouble() < 0.5)
                            addIfLegal(nextLeft, p.add(1, 0));
                        else
                            addIfLegal(nextRight, p.add(-1, 0));
                    }
                }
            }

            // with some probability, add a point even if it failed, anyway:
            foreach (Point failedPoint in leftPoints)
            {
                if(!od.Contains(failedPoint)) // if od contains it, we already added the point to nextLeft
                    if(myRand.NextDouble() > stopScanProb)
                        addIfLegal(nextLeft, failedPoint.add(1, 0));
            }
            foreach (Point failedPoint in rightPoints)
            {
                if (!od.Contains(failedPoint))
                    if (myRand.NextDouble() > stopScanProb)
                        addIfLegal(nextRight, failedPoint.add(-1, 0));
            }

            leftPoints = nextLeft;
            rightPoints = nextRight;
            if(leftPoints.Count + rightPoints.Count < param.A_P.Count)
            {
                int addToLeft = Math.Min(param.A_P.Count - (leftPoints.Count + rightPoints.Count), waitingLeftPoints.Count);
                for (int i = 0; i < addToLeft; ++i)
                {
                    leftPoints.Add(waitingLeftPoints.Last());
                    waitingLeftPoints.RemoveAt(waitingLeftPoints.Count() - 1);
                }
                if (leftPoints.Count + rightPoints.Count < param.A_P.Count)
                {
                    int addToRight = Math.Min(param.A_P.Count - (leftPoints.Count + rightPoints.Count), waitingRightPoints.Count);
                    for (int i = 0; i < addToRight; ++i)
                    {
                        waitingRightPoints.Add(waitingRightPoints.Last());
                        waitingRightPoints.RemoveAt(waitingRightPoints.Count() - 1);
                    }
                }
            }

            res.AddRange(leftPoints);
            res.AddRange(rightPoints);
            pi = res.Count();
            for (; pi < param.A_P.Count; ++pi)
            {
                Point p;

                do
                {
                    p = new Point((int)(myRand.Next() % graph.WidthCellCount),
                        (int)(myRand.Next() % graph.HeightCellCount));
                }
                while (res.Contains(p));
                res.Add(p);
            }

            return res;
        }

        public override bool init(AGameGraph G, FrontsGridRoutingGameParams prm, IPolicyGUIInputProvider pgui, Dictionary<string, string> policyParams)
        {
            this.graph = (GridGameGraph)G;
            this.param = prm;
            myRand = new ThreadSafeRandom();
            leftPoints = new List<Point>();
            rightPoints = new List<Point>();
            waitingRightPoints = new List<Point>();
            waitingLeftPoints = new List<Point>();
            stopScanProb = 
                double.Parse(AppConstants.Policies.Routing.RowDestroyingPursuerPolicy.STOP_SCAN_ONFAILURE_PROBABILITY.tryRead(policyParams));
            dualSearchProb =
                double.Parse(AppConstants.Policies.Routing.RowDestroyingPursuerPolicy.DUAL_DIRECTION_SEACH_PROB.tryRead(policyParams));
            return true;
        }

        List<Point> od;

        public override void setGameState(int currentRound, IEnumerable<Point> O_c, IEnumerable<GameLogic.Utils.CapturedObservation> O_d)
        {
            od = new  List<Point>( O_d.Select((ce) => { return ce.where; }));
        }
    }

    
}
