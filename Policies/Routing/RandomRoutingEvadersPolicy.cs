using AForge.Genetic;
using GoE.AppConstants;
using GoE.AppConstants.Policies.Routing;
using GoE.GameLogic;
using GoE.GameLogic.EvolutionaryStrategy;
using GoE.UI;
using GoE.Utils;
using GoE.Utils.Algorithms;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoE.Policies
{
    public class RandomRoutingEvadersPolicy : AFrontsGridRoutingEvadersPolicy
    {
        int remainingEvadersToPlace;
        List<Point> unoccupiedPoints = new List<Point>();
        GridGameGraph graph;
        FrontsGridRoutingGameParams param;
        ThreadSafeRandom myRand;
        IEnumerable<GameLogic.Utils.CapturedObservation> deadEvaders;
        public override CommunicationGraph communicate()
        {
            return new CommunicationGraph();
        }

        HashSet<Point> allEvaders = new HashSet<Point>();
        public override List<Point> getNextStep()
        {
            

            while(remainingEvadersToPlace > 0 && unoccupiedPoints.Count > 0)
            {
                --remainingEvadersToPlace;
                int pi = myRand.Next() % unoccupiedPoints.Count;
                var p = unoccupiedPoints[pi];
                unoccupiedPoints[pi] = unoccupiedPoints.Last();
                unoccupiedPoints.RemoveAt(unoccupiedPoints.Count - 1);
                allEvaders.Add(p);
            }
            foreach (var p in deadEvaders)
                unoccupiedPoints.Add(p.where); // next round, we may add another evader there (but not this round-since a pursuer is still there!)

            return allEvaders.ToList();
        }

        public override bool init(AGameGraph G, FrontsGridRoutingGameParams prm, APursuersPolicy initializedPursuers, IPolicyGUIInputProvider pgui, Dictionary<string, string> policyParams = null)
        {
            this.myRand = new ThreadSafeRandom();
            this.graph = (GridGameGraph)G;
            this.param = prm;
            unoccupiedPoints = new List<Point>();
            for (int i = 0; i < graph.WidthCellCount; ++i)
                for (int j = 0; j < graph.HeightCellCount; ++j)
                    unoccupiedPoints.Add(new Point(i, j));
            return true;
        }

        public override void setGameState(int currentRound, 
                                          IEnumerable<GameLogic.Utils.CapturedObservation> O_d, 
                                          AgentGrid<Evader> currentEvaders, float MaxEvadersToPlace,
                                          List<Point> CurrentPatrollerLocations)
        {
            this.deadEvaders = O_d;
            this.remainingEvadersToPlace = (int)MaxEvadersToPlace;
            foreach (var p in O_d)
                allEvaders.Remove(p.where);
        }
    }
    public class BiasedRandomRoutingEvadersPolicy : AFrontsGridRoutingEvadersPolicy
    {
        protected double increaseRowProb, increaseColProb, decreaseKilledProb;
        protected int remainingEvadersToPlace;
        protected Dictionary<Point, double> unoccupiedPoints = new Dictionary<Point, double>();
        protected GridGameGraph graph;
        protected FrontsGridRoutingGameParams param;
        protected ThreadSafeRandom myRand;
        protected IEnumerable<GameLogic.Utils.CapturedObservation> deadEvaders;
        double currentTotalSum = 1.0; // helps dealing with accumulated inaccuracies

        public override List<ArgEntry> policyInputKeys
        {
            get
            {
                return ReflectionUtils.getStaticInstancesInClass<ArgEntry>(typeof(BiasedRandomRoutingEvaders));
            }
        }
        public override CommunicationGraph communicate()
        {
            return new CommunicationGraph();
        }
        

        /// <summary>
        /// finds the nearest unoccupied points in the same row/column as startPoint
        /// </summary>
        /// <param name="startPoint"></param>
        /// <param name="inRow"></param>
        /// <param name="inCol"></param>
        /// <returns></returns>
        protected List<Point> getUnoccupied(Point startPoint, bool inRow,bool inCol)
        {
            List<Point> res = new List<Point>();

            double tmp;
            if (inRow)
            {
                Point left = startPoint.add(-1, 0);
                Point right = startPoint.add(1, 0);
                while (left.X >= 0 && !unoccupiedPoints.TryGetValue(left, out tmp))
                    left = left.add(-1, 0);
                while (right.X < graph.WidthCellCount && !unoccupiedPoints.TryGetValue(right, out tmp))
                    right = right.add(1, 0);

                if (left.X >= 0)
                    res.Add(left);
                if (right.X < graph.WidthCellCount)
                    res.Add(right);
            }
            if (inCol)
            {
                Point up = startPoint.add(0, 1);
                Point down = startPoint.add(0, -1);
                while (down.Y >= 0 && !unoccupiedPoints.TryGetValue(down, out tmp))
                    down = down.add(0, -1);
                while (up.Y < graph.HeightCellCount && !unoccupiedPoints.TryGetValue(up, out tmp))
                    up = up.add(0, 1);

                if (up.Y < graph.HeightCellCount)
                    res.Add(up);
                
                if (down.Y >= 0)
                    res.Add(down);
            }

            return res;
        }
        KeyValuePair<Point,double> chooseUnoccupiedPoint()
        {
            double prob = myRand.NextDouble() * currentTotalSum;
            foreach (var pi in unoccupiedPoints)
            {
                prob -= pi.Value;
                if (prob <= 0)
                {
                    return pi;
                }
            }
            return unoccupiedPoints.Last();
        }

        // makes sure the total probability sum is 1.0
        void normalizeUnoccupiedProbabilities()
        {
            currentTotalSum = 0;

            var unoccupiedPointsList = unoccupiedPoints.ToList();

            foreach (var v in unoccupiedPointsList)
                currentTotalSum += v.Value;
            
            foreach (var v in unoccupiedPointsList)
                unoccupiedPoints[v.Key] *= 1.0 / currentTotalSum;
            currentTotalSum = 1.0;
        }

        HashSet<Point> allEvaders = new HashSet<Point>();
        public override List<Point> getNextStep()
        {
            

            while (remainingEvadersToPlace > 0 && unoccupiedPoints.Count > 0)
            {
                --remainingEvadersToPlace;

                var unoccupiedPoint = chooseUnoccupiedPoint();
                double prob = unoccupiedPoint.Value;
                Point p = unoccupiedPoint.Key;
                
                unoccupiedPoints.Remove(p);
                allEvaders.Add(p);
                
                if (myRand.NextDouble() <= increaseRowProb) // with some probability, increase parobabilities of nearby points
                {
                    var pointsToIncrease = getUnoccupied(p, true, myRand.NextDouble() <= increaseColProb); // with some probability, increase probabilities of column
                    foreach (Point cp in pointsToIncrease)
                        unoccupiedPoints[cp] += prob / pointsToIncrease.Count;

                   if(pointsToIncrease.Count > 0)
                        currentTotalSum -= prob;
                }
                

                //if (left.X >= 0 && right.X < graph.WidthCellCount)
                //{
                //    unoccupiedPoints[left] += prob / 2;
                //    unoccupiedPoints[right] += prob / 2;
                //}
                //else if (left.X >= 0)
                //    unoccupiedPoints[left] += prob;
                //else if (right.X < graph.WidthCellCount)
                //    unoccupiedPoints[right] += prob;
                //else


                //if (probDivision > 0 ||)
                //{
                //    double prevSum = currentTotalSum;
                //    currentTotalSum = 0;
                //    // refresh currentTotalSum
                //    // all values now sum up to currentTotalSum-prob, instead of 1. we fix this by "streching" all values
                //    var unoccupiedPointsList = unoccupiedPoints.Keys.ToList();
                //    foreach (var v in unoccupiedPointsList)
                //        currentTotalSum += (unoccupiedPoints[v] *= 1.0 / (prevSum - prob));
                //}
            }


            //if (deadEvaders.Count() > 0)
            //{
            //    double avgProb = 1.0 / ((double)unoccupiedPoints.Count + deadEvaders.Count()); // avgProb after normalization
            //    double newTotalProb = currentTotalSum + avgProb * deadEvaders.Count();

            //    currentTotalSum = 0; // refresh currentTotalSum

            //    double multVal = 1.0 / newTotalProb;
            //    var unoccupiedPointsList = unoccupiedPoints.Keys.ToList();
            //    // in a moment, all values will sum up to newTotalProb, instead of 1. we fix this by "streching" all values
            //    foreach (var v in unoccupiedPointsList)
            //        currentTotalSum += (unoccupiedPoints[v] *= multVal);                    
            //    foreach (var p in deadEvaders)
            //        unoccupiedPoints[p.where] = avgProb;
            //    currentTotalSum += avgProb * deadEvaders.Count();
            //}

            List<Point> adjacentToDeadPoints = null;
            if (decreaseKilledProb >= myRand.NextDouble())
                adjacentToDeadPoints = new List<Point>();

            if (deadEvaders.Count() > 0)
            {
                if (adjacentToDeadPoints != null)
                {
                    foreach (var p in deadEvaders)
                        adjacentToDeadPoints.AddRange(getUnoccupied(p.where,true,true));
                    PointSet ps = new PointSet(adjacentToDeadPoints);
                    ps.removeDupliacates();

                    foreach (var p in adjacentToDeadPoints)
                    {
                        double lostProb = unoccupiedPoints[p];
                        unoccupiedPoints[p] /= 2;
                        currentTotalSum -= (lostProb - unoccupiedPoints[p]);
                    }
                }

                double avgProb = currentTotalSum / ((double)unoccupiedPoints.Count + deadEvaders.Count()); // after normalization, each of the newly unoccupied points will have 1/pointCount prob
                foreach (var p in deadEvaders)
                    unoccupiedPoints[p.where] = avgProb;
            }

            if (Math.Abs(currentTotalSum - 1.0) > 0.0001)
                normalizeUnoccupiedProbabilities();

            return allEvaders.ToList();
        }

        public override bool init(AGameGraph G, FrontsGridRoutingGameParams prm, APursuersPolicy initializedPursuers, IPolicyGUIInputProvider pgui, Dictionary<string, string> policyParams = null)
        {
            this.allEvaders = new HashSet<Point>();
            this.myRand = new ThreadSafeRandom();
            this.graph = (GridGameGraph)G;
            this.param = prm;
            unoccupiedPoints = new Dictionary<Point, double>();
            double uniformProb = 1.0/((double)graph.WidthCellCount * graph.HeightCellCount);
            for (int i = 0; i < graph.WidthCellCount; ++i)
                for (int j = 0; j < graph.HeightCellCount; ++j)
                    unoccupiedPoints[new Point(i, j)] = uniformProb;
            currentTotalSum = uniformProb * graph.WidthCellCount * graph.HeightCellCount;

            decreaseKilledProb = double.Parse(BiasedRandomRoutingEvaders.DECREASE_AROUND_CAPTURED.tryRead(policyParams)); 
            increaseRowProb = double.Parse(BiasedRandomRoutingEvaders.INCREASE_ON_ROW.tryRead(policyParams));
            increaseColProb = double.Parse(BiasedRandomRoutingEvaders.INCREASE_ON_COLUMN.tryRead(policyParams));
            return true;
        }

        public override void setGameState(int currentRound,
                                          IEnumerable<GameLogic.Utils.CapturedObservation> O_d,
                                          AgentGrid<Evader> currentEvaders, float MaxEvadersToPlace,
                                          List<Point> CurrentPatrollerLocations)
        {
            this.deadEvaders = O_d;
            this.remainingEvadersToPlace = (int)MaxEvadersToPlace;

            foreach (var p in O_d)
                allEvaders.Remove(p.where);
        }
    }
    public class GeneticWindowFunctionEvadersPolicy : AFrontsGridRoutingEvadersPolicy
    {
        public enum PointType : int
        {
            OccupiedByEvader = 0, // add/mult by constant
            OccupiedByPatroller = 1, // add/mult by constant
            Unoccupied = 2, // add/mult by that point's energy multiplied by constant
            Blank = 3,  // (out of graph) - add/mult by constant

            Count
        }
        /// <summary>
        /// serves WindowFunctionChromosome. 
        /// The chromsome chooses to what extent the window around a point affects the probability of choosing that point.
        /// The window affects the point by recognizing 4 points types (see enum PointType).
        /// The window's value is determined by summing then multiplying several values around its center point, where each point contributes either addition or multiplication with some constant
        /// </summary>
        public struct WindowFunction
        {
            public static WindowFunction RowBiased
            {
                get
                {
                    var res = new WindowFunction();
                    res.addOpsOnly = 1;
                    res.WindowSize = 5;
                    res.WindowFunctionInfluence = 0.5;
                    res.opPerPointTypeAndLocation = 0;
                    res.constPerPoint = Utils.Algorithms.AlgorithmUtils.getRepeatingValueList(0.0, getOpCount(res.WindowSize));
                    res.setConstant(PointType.OccupiedByEvader, 0, 1);
                    res.setConstant(PointType.OccupiedByEvader, 1, 1);
                    res.setConstant(PointType.OccupiedByEvader, 2, 1);

                    res.setConstant(PointType.Unoccupied, 0, 1);
                    res.setConstant(PointType.Unoccupied, 1, 1);
                    res.setConstant(PointType.Unoccupied, 2, 1);
                    return res;
                }
            }

            public int addOpsOnly; // if 1, all ops are add (no multiply)
            public int WindowSize;
            public ulong opPerPointTypeAndLocation; // bit array. for each op, 0 means addition, 1 means multiplication
            public List<double> constPerPoint;
            public double WindowFunctionInfluence;

            public double getConstant(PointType t, int pointLocation)
            {
                return constPerPoint[getOpIdx(t, pointLocation)];
            }
            public void setConstant(PointType t, int pointLocation, double constantVal)
            {
                constPerPoint[getOpIdx(t, pointLocation)] = constantVal;
            }
            public int getOp(PointType t, int pointLocation)
            {
                if (addOpsOnly == 1)
                    return 0;
                return (opPerPointTypeAndLocation & (1ul << getOpIdx(t, pointLocation))) == 0 ? (0) : (1);
            }
            public void setOp(PointType t, int pointLocation, int op)
            {
                if (op == 1)
                    opPerPointTypeAndLocation |= (1ul << getOpIdx(t, pointLocation));
                else
                    opPerPointTypeAndLocation &= ulong.MaxValue - (1ul << getOpIdx(t, pointLocation));
            }

            /// <summary>
            /// </summary>
            /// <param name="t"></param>
            /// <param name="pointLocation">
            /// 0 to 4, or 0 to 8 (depending on 'WindowSize')
            /// </param>
            /// <returns></returns>
            public static int getOpIdx(PointType t, int pointLocation)
            {
                return (int)PointType.Count * pointLocation + (int)t;
            }
            public static int getOpCount(int winSize)
            {
                return (int)PointType.Count * winSize;
            }
            public override string ToString()
            {
                return addOpsOnly.ToString() + "|" +
                    WindowSize.ToString() + "|" +
                    WindowFunctionInfluence.ToString() + "|" +
                    opPerPointTypeAndLocation.ToString() + "|" +
                    ParsingUtils.makeCSV(constPerPoint, 0, false);
            }
            public static WindowFunction FromString(string serialization)
            {
                WindowFunction res;
                var vals = serialization.Split(new char[] { '|' });
                res.addOpsOnly = int.Parse(vals[0]);
                res.WindowSize = int.Parse(vals[1]);
                res.WindowFunctionInfluence = double.Parse(vals[2]);
                res.opPerPointTypeAndLocation = ulong.Parse(vals[3]);
                res.constPerPoint = ParsingUtils.separateCSV(vals[4]).ConvertAll((s) => {return  double.Parse(s); });
                return res;
            }
        }
        public class WindowFunctionChromosome : IChromosome
        {
            public float mutationRate { get; protected set; }
            public double maxWindowFuncInfluence { get; protected set; }
            public MemberBinaryChromosome opPerPointTypeAndLocation; // for each op, 0 means addition, 1 means multiplication
            public MemberDoubleArrayChromosome vals; // 1 value for WindowFunctionInfluence, and the rest for ConstValuePerPoint

            public double WindowFunctionInfluence
            {
                get
                {
                    return vals[0];
                }
                protected set
                {
                    vals[0] = value;
                }
            }
            //public double ConstValuePerPoint(PointType t, int pointLocation) // for each op, tells what constant to use
            //{
            //    return vals[1 + WindowFunction.getOpIdx(t, pointLocation)];
            //}
    
            /// <summary>
            /// either 5 or 9 (amount of surrounding points)
            /// </summary>
            public int WindowSize
            {
                get;
                protected set;
            }
            
            public WindowFunction ToWindowFunction()
            {
                var res = new WindowFunction();

                res.constPerPoint = new List<double>();
                for (int i = 1; i < vals.count(); ++i)
                {
                    res.constPerPoint.Add(vals[i]);
                }
                res.WindowSize = WindowSize;
                res.WindowFunctionInfluence = WindowFunctionInfluence;
                res.opPerPointTypeAndLocation = opPerPointTypeAndLocation.Value;
                res.addOpsOnly = 0; // by default, multiplication is also used
                return res;
            }
            public void FromWindowFunction(WindowFunction src)
            {
                vals = new MemberDoubleArrayChromosome(src.constPerPoint.Count() + 1,100.0f,mutationRate);
                for (int i = 0; i < vals.count(); ++i)
                {
                    vals[i + 1] = src.constPerPoint[i];
                }
                WindowFunctionInfluence = src.WindowFunctionInfluence;
                opPerPointTypeAndLocation.Value = src.opPerPointTypeAndLocation;

            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="windowSize">
            /// either 5 or 9
            /// </param>
            /// <param name="mutationRate">
            /// 0 to 1
            /// </param>
            /// <param name="maxWindowFuncInfluence">
            /// 0 to 1, where 0 means window function is entirely ignored, and 1 means its the only thing that matters.
            /// </param>
            public WindowFunctionChromosome(int windowSize = 5, float MutationRate = 0.1f, double MaxWindowFuncInfluence = 0.9)
            {
                WindowSize = windowSize;
                Fitness = 0;
                this.mutationRate = MutationRate;
                this.maxWindowFuncInfluence = MaxWindowFuncInfluence;

                opPerPointTypeAndLocation = new MemberBinaryChromosome(WindowFunction.getOpCount(windowSize));
                vals = new MemberDoubleArrayChromosome(WindowFunction.getOpCount(windowSize) + 1, MutationRate, 0.1);
                WindowFunctionInfluence = Math.Min(WindowFunctionInfluence, maxWindowFuncInfluence);
                
            }
            
            public WindowFunctionChromosome(WindowFunctionChromosome src)
            {
                this.WindowSize = src.WindowSize;
                Fitness = src.Fitness;
                opPerPointTypeAndLocation = (MemberBinaryChromosome)src.opPerPointTypeAndLocation.Clone();
                vals = (MemberDoubleArrayChromosome)src.vals.Clone();
            }

            #region IChromosome implementation
            public double Fitness
            {
                get;
                protected set;
            }
            public IChromosome Clone()
            {
                return new WindowFunctionChromosome(this);
            }

            public int CompareTo(object obj)
            {
                return ((WindowFunctionChromosome)obj).Fitness.CompareTo(Fitness);
            }

            public IChromosome CreateNew()
            {
                return new WindowFunctionChromosome(WindowSize,mutationRate,maxWindowFuncInfluence);
            }

            public void Crossover(IChromosome pair)
            {
                WindowFunctionChromosome cp = (WindowFunctionChromosome)pair;
                opPerPointTypeAndLocation.Crossover(cp.opPerPointTypeAndLocation);
                vals.Crossover(cp.vals);

            }

            public void Evaluate(IFitnessFunction function)
            {
                Fitness = function.Evaluate(this);
            }

            public void Generate()
            {
                Fitness = 0;
                opPerPointTypeAndLocation = new MemberBinaryChromosome(WindowFunction.getOpCount(WindowSize));
                vals = new MemberDoubleArrayChromosome(WindowFunction.getOpCount(WindowSize) + 1, mutationRate, 0.1);
                WindowFunctionInfluence = Math.Min(WindowFunctionInfluence, maxWindowFuncInfluence);
            }

            public void Mutate()
            {
                vals.Mutate();
                // TODO: this is not a good implementation, as binary chromsome mutation always mutates 1 value. that is, the same value may be mutated several times
                int mutationsCount = (int)Math.Ceiling(mutationRate * WindowFunction.getOpCount(WindowSize));
                for(int i = 0; i < mutationsCount; ++i)
                    opPerPointTypeAndLocation.Mutate();

                WindowFunctionInfluence = Math.Min(WindowFunctionInfluence, maxWindowFuncInfluence);
            }

            #endregion

        }
        private struct PointState
        {
            public PointType t;
            public double energy
            {
                get;
                set;
                //get
                //{
                //    return inner;
                //}
                //set
                //{
                //    inner = value;
                //    if (double.IsInfinity(inner) || double.IsNaN(inner) || 
                //        (inner >= double.MaxValue/2) || (inner <= double.MinValue/2))
                //    {
                //        int a = 0;
                //    }
                //}
            }

            //private double inner;
        }
        private PointState[,] latestState; // graphState[x,y] tells the state of point in x,y. 
        private float maxEvadersToPlace;
        private WindowFunction func;
        private GridGameGraph graph;
        private FrontsGridRoutingGameParams param;
        public override List<ArgEntry> policyInputKeys
        {
            get
            {
                List<ArgEntry> res = new List<ArgEntry>();
                res.Add(AppConstants.Policies.Routing.GeneticWindowFunctionEvadersPolicy.WINDOW_CHROMOSOME);
                return res;

            }
        }
        public override CommunicationGraph communicate()
        {
            return new CommunicationGraph();
        }

        HashSet<Point> allEvaders = new HashSet<Point>();
        ThreadSafeRandom myRand;
        public override List<Point> getNextStep()
        {
            PointState[,] nextStates = new PointState[graph.WidthCellCount, graph.HeightCellCount];

            double maxEnergy = double.MinValue;
            double minEnergy = double.MaxValue;
            for (int x = 0; x < graph.WidthCellCount; ++x)
                for (int y = 0; y < graph.HeightCellCount; ++y)
                {
                    if (latestState[x, y].t == PointType.OccupiedByEvader) // if occupied by patroller, in next state its unoccupied
                    {
                        nextStates[x, y] = new PointState() { t = PointType.OccupiedByEvader, energy = 0 };
                        continue; // x,y has an uncaptured evader. no energy is associated with this point
                    }

                    double e = getEnergy(new Point(x, y));
                    maxEnergy = Math.Max(maxEnergy, e);
                    minEnergy = Math.Min(minEnergy, e);
                    nextStates[x, y] = new PointState() { t = PointType.Unoccupied, energy = e};
                }

            double minChance = 1;
            if(maxEnergy - minEnergy > 0.00001)
                minChance = (maxEnergy - minEnergy) * (1 - func.WindowFunctionInfluence); // allows balancing the window function's weight

            double totalChance = 0;
            double[,] chancePerPoint = new double[graph.WidthCellCount, graph.HeightCellCount];
            for (int x = 0; x < graph.WidthCellCount; ++x)
                for (int y = 0; y < graph.HeightCellCount; ++y)
                {
                    if (nextStates[x, y].t == PointType.Unoccupied)
                    {
                        nextStates[x, y].energy += minEnergy + minChance; // minimal value of all points is now minChance
                        nextStates[x, y].energy /= (maxEnergy + minEnergy + minChance); // maximal energy is now 1
                    }
                    else
                        nextStates[x, y].energy = 0;

                    totalChance += nextStates[x, y].energy;
                }

            

            for(int i = 0; i < maxEvadersToPlace; ++i)
            {
                double val = myRand.NextDouble() * totalChance;

                for (int x = 0; x < graph.WidthCellCount; ++x)
                    for (int y = 0; y < graph.HeightCellCount; ++y)
                    {
                        val -= nextStates[x, y].energy;
                        if(val <= 0)
                        {
                            nextStates[x, y].energy = 0;
                            nextStates[x, y].t = PointType.OccupiedByEvader;
                            allEvaders.Add(new Point(x, y));

                            x = (int)graph.WidthCellCount;
                            y = (int)graph.HeightCellCount;
                            break;
                        }
                    }
                
            }

            latestState = nextStates;
            return allEvaders.ToList();
        }

     
        public override bool init(AGameGraph G, FrontsGridRoutingGameParams prm, APursuersPolicy initializedPursuers, IPolicyGUIInputProvider pgui, Dictionary<string, string> policyParams = null)
        {
            this.allEvaders = new HashSet<Point>();
            myRand = new ThreadSafeRandom();
            func =
                WindowFunction.FromString(AppConstants.Policies.Routing.GeneticWindowFunctionEvadersPolicy.WINDOW_CHROMOSOME.tryRead(policyParams));
            graph = (GridGameGraph)G;
            param = prm;
            latestState =  new PointState[graph.WidthCellCount, graph.HeightCellCount];
            for (int x = 0; x < graph.WidthCellCount; ++x)
                for (int y = 0; y < graph.HeightCellCount; ++y)
                    latestState[x, y] = new PointState() { energy = 0, t = PointType.Unoccupied };
            return true;
        }
        
        public override void setGameState(int currentRound, IEnumerable<GameLogic.Utils.CapturedObservation> O_d, AgentGrid<Evader> CurrentEvaders, float MaxEvadersToPlace, List<Point> CurrentPatrollerLocations)
        {
            maxEvadersToPlace = MaxEvadersToPlace;
            foreach (var p in CurrentPatrollerLocations)
                latestState[p.X, p.Y].t = PointType.OccupiedByPatroller;
            foreach (var p in O_d)
                allEvaders.Remove(p.where);
        }

        private PointState getState(Point p)
        {
            if (!graph.isOnGrid(p))
                return new PointState() { t = PointType.Blank };
            return latestState[p.X, p.Y];
        }
        private double getEnergy(Point p)
        {
            List<PointState> window = new List<PointState>();
            if (func.WindowSize == 5)
            {
                window.Add(getState(GameLogic.Utils.add(p, -1, 0)));
                window.Add(getState(p));
                window.Add(getState(GameLogic.Utils.add(p, 1, 0)));
                window.Add(getState(GameLogic.Utils.add(p, 0, -1)));
                window.Add(getState(GameLogic.Utils.add(p, 0, 1)));
            }
            double sum = 0, mult = 1;
            for (int i = 0; i < window.Count; ++i)
            {
                double energy = 1;
                if (window[i].t == PointType.Unoccupied)
                    energy = window[i].energy;

                double constVal = func.getConstant(window[i].t, i) * energy;
                //if(double.IsNaN(constVal) || double.IsInfinity(constVal) || double.IsNaN(energy) || double.IsInfinity(energy)
                //    || (constVal >= double.MaxValue / 2) || (constVal <= double.MinValue / 2) ||
                //    (energy >= double.MaxValue / 2) || (energy <= double.MinValue / 2))
                //{
                //    int a = 0;
                //}
                if (func.getOp(window[i].t, i) == 0)
                    sum += constVal;
                else
                    mult *= constVal;
            }
            //if (double.IsNaN(sum) || double.IsInfinity(sum) || double.IsNaN(mult) || double.IsInfinity(mult) ||
            //    double.IsNaN(sum * mult) || double.IsInfinity(sum * mult) ||
            //    (sum * mult >= double.MaxValue / 2) || (sum * mult <= double.MinValue / 2))
            //{
            //    int a = 0;
            //}


            return sum * mult;
        }
    }

    /// <summary>
    /// chooses a column/row randomly, where the weight of each row/column depends on how many evaders
    /// are already there
    /// </summary>
    public class WeightedColumnRowRandomRoutingEvadersPolicy : AFrontsGridRoutingEvadersPolicy
    {
        public const double MINIMAL_PENALTY = 0.01;

        protected class PointState : IComparable<PointState>
        {
            const int maxWidth = 1000; // FIXME: dirty. using a static variable according to graph may be unsafe for parallelization
            public PointState(Point location, double Penalty)
            {
                p = location;
                penalty = Penalty;
            }
            public Point p;
            public double penalty;
            public override bool Equals(object obj)
            {
                PointState pobj = (PointState)obj;
                return pobj.p == p;
            }
            public override int GetHashCode()
            {
                return p.GetHashCode();
            }
            public int CompareTo(PointState other)
            {
                return p.X.CompareTo(other.p.X) * maxWidth + p.Y.CompareTo(other.p.Y);
            }
        }

        protected double capturedPenalty, penaltyDiscount;
        protected double colProb;
        protected int remainingEvadersToPlace;

        protected double energyPowerFactor;
        protected int totalUnoccupiedPoints;
        protected List<List<PointState>> unoccupiedByRow; // todo: change to some sorted tree. SortedSet doesn't have PopRandomItem, so it won't improve perforamnce
        protected List<List<PointState>> unoccupiedByColumn;
        

        protected GridGameGraph graph;
        protected FrontsGridRoutingGameParams param;
        protected ThreadSafeRandom myRand;
        
        public override List<ArgEntry> policyInputKeys
        {
            get
            {
                return ReflectionUtils.getStaticInstancesInClass<ArgEntry>(typeof(WeightedColumnRowRandomRoutingEvaders));
            }
        }
        public override CommunicationGraph communicate()
        {
            return new CommunicationGraph();
        }

        private HashSet<Point> allEvaders = new HashSet<Point>();
        public override List<Point> getNextStep()
        {
            

            while (remainingEvadersToPlace > 0 && totalUnoccupiedPoints > 0)
            {
                --remainingEvadersToPlace;
                --totalUnoccupiedPoints;

                #region update penalty
                for (int y = 0; y < graph.HeightCellCount; ++y)
                    foreach (var n in unoccupiedByRow[y])
                    {
                        n.penalty *= penaltyDiscount;
                        if (n.penalty < MINIMAL_PENALTY)
                            n.penalty = 0;
                    }
                for (int x = 0; x < graph.WidthCellCount; ++x)
                    foreach (var n in unoccupiedByColumn[x])
                    {
                        n.penalty *= penaltyDiscount;
                        if (n.penalty < MINIMAL_PENALTY)
                            n.penalty = 0;
                    }
                #endregion

                if (myRand.NextDouble() > colProb)
                {
                    #region select row
                    double totalEnergy = 0;
                    List<double> energyPerRow = new List<double>();
                    // evaluate each row
                    for(int y = 0; y < graph.HeightCellCount; ++y)
                    {
                        
                        if (unoccupiedByRow[y].Count == 0)
                        {
                            // no options anyway
                            energyPerRow.Add(0);
                            continue;
                        }
                        
                        energyPerRow.Add(graph.WidthCellCount - unoccupiedByRow[y].Count); // more occupied points - more chance to choose this
                        foreach (var n in unoccupiedByRow[y])
                            energyPerRow[energyPerRow.Count-1] -= n.penalty; // reduce chance of row according to accumulated penalties

                        // scale the roulette:
                        energyPerRow[energyPerRow.Count - 1] = Math.Pow(energyPerRow[energyPerRow.Count - 1], energyPowerFactor);
                        totalEnergy += energyPerRow.Last();
                    }

                    double choice = myRand.NextDouble() * totalEnergy - energyPerRow.First();
                    int testedRow = 0;
                    int chosenRow = 0;
                    while(choice >= 0 && testedRow < energyPerRow.Count-1)
                    {
                        ++testedRow;
                        if (unoccupiedByRow[testedRow].Count != 0)
                            chosenRow = testedRow;
                        choice -= energyPerRow[testedRow];
                    }

                    Point toAdd = unoccupiedByRow[chosenRow].popRandomItem(myRand.rand).p;
                    allEvaders.Add(toAdd);
                    unoccupiedByColumn[toAdd.X].Remove(new PointState(toAdd, 0));
                    #endregion
                }
                else
                {
                    #region select column
                    double totalEnergy = 0;
                    List<double> energyPerCol = new List<double>();
                    // evaluate each column
                    for (int x = 0; x < graph.WidthCellCount; ++x)
                    {
                        if (unoccupiedByColumn[x].Count == 0)
                        {
                            // no options anyway
                            energyPerCol.Add(0);
                            continue;
                        }

                        energyPerCol.Add(graph.HeightCellCount - unoccupiedByColumn[x].Count); // more occupied points - more chance to choose this
                        foreach (var n in unoccupiedByColumn[x])
                            energyPerCol[energyPerCol.Count - 1] -= n.penalty; // reduce chance of col according to accumulated penalties

                        // scale the roulette:
                        energyPerCol[energyPerCol.Count - 1] = Math.Pow(energyPerCol[energyPerCol.Count - 1], energyPowerFactor);
                        totalEnergy += energyPerCol.Last();
                    }

                    double choice = myRand.NextDouble() * totalEnergy - energyPerCol.First();
                    int chosenCol = 0, testedCol = 0 ;
                    while (choice >= 0 && chosenCol < energyPerCol.Count-1)
                    {
                        ++testedCol;
                        if (unoccupiedByRow[testedCol].Count != 0)
                            chosenCol = testedCol;
                        choice -= energyPerCol[testedCol];
                    }
                    Point toAdd = unoccupiedByColumn[chosenCol].popRandomItem(myRand.rand).p;
                    allEvaders.Add(toAdd);
                    unoccupiedByRow[toAdd.Y].Remove(new PointState(toAdd, 0));
                    #endregion
                }
                
            }
            
            return allEvaders.ToList();
        }

        public override bool init(AGameGraph G, FrontsGridRoutingGameParams prm, APursuersPolicy initializedPursuers, IPolicyGUIInputProvider pgui, Dictionary<string, string> policyParams = null)
        {
            this.myRand = new ThreadSafeRandom();
            this.graph = (GridGameGraph)G;
            this.param = prm;
            totalUnoccupiedPoints = graph.Nodes.Count;

            this.energyPowerFactor = double.Parse(WeightedColumnRowRandomRoutingEvaders.ENERGY_POWER_FACTOR.tryRead(policyParams));

            this.penaltyDiscount =
                double.Parse(WeightedColumnRowRandomRoutingEvaders.PURSUERS_HIT_PENALTY_DISCOUNT_FACTOR.tryRead(policyParams));

            this.capturedPenalty =
                graph.WidthCellCount *
                double.Parse(WeightedColumnRowRandomRoutingEvaders.PURSUERS_HIT_INITIAL_PENALTY_FACTOR.tryRead(policyParams));
            colProb =
                double.Parse(WeightedColumnRowRandomRoutingEvaders.ADD_BY_COLUM_PROB.tryRead(policyParams));

            unoccupiedByColumn = new List<List<PointState>>();
            for (int x = 0; x < graph.WidthCellCount; ++x)
            {
                unoccupiedByColumn.Add(new List<PointState>());
                for (int y = 0; y < graph.HeightCellCount; ++y)
                    unoccupiedByColumn.Last().Add(new PointState(new Point(x, y), 0));
            }
            unoccupiedByRow = new List<List<PointState>>();
            for (int y = 0; y < graph.HeightCellCount; ++y)
            {
                unoccupiedByRow.Add(new List<PointState>());
                for (int x = 0; x < graph.WidthCellCount; ++x)
                    unoccupiedByRow.Last().Add(new PointState(new Point(x, y), 0));
            }
            return true;
        }

        public override void setGameState(int currentRound,
                                          IEnumerable<GameLogic.Utils.CapturedObservation> O_d,
                                          AgentGrid<Evader> currentEvaders, float MaxEvadersToPlace,
                                          List<Point> CurrentPatrollerLocations)
        {
            foreach(var co in O_d)
            {
                unoccupiedByRow[co.where.Y].Add(new PointState(co.where,this.capturedPenalty));
                unoccupiedByColumn[co.where.X].Add(new PointState(co.where, this.capturedPenalty));
                ++totalUnoccupiedPoints;
                allEvaders.Remove(co.where);
            }
            this.remainingEvadersToPlace = (int)MaxEvadersToPlace;

        }
    }
    
}
