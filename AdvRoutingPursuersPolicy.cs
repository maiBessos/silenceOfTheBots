using GoE.Policies;
using GoE.Utils;
using GoE.Utils.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GoE.AppConstants;
using GoE.GameLogic;
using GoE.UI;
using System.Drawing;
using GoE.Utils.Algorithms;
using GoE.GameLogic.EvolutionaryStrategy;
using GoE.Utils.Extensions;
using static GoE.AdvRouting.Utils;
using System.Collections;
using GoE.AppConstants.Policies;
using System.Threading;
using static GoE.Utils.GraphAlgorithms.FindShortestPath;

namespace GoE.AdvRouting
{
    public class PointEqComparer : IEqualityComparer<PointF>
    {
        const float FLOAT_SENSITIVITY = 100;
        const float EPSILON = 0.01f;
        public bool Equals(PointF a, PointF b)
        {
            return Math.Abs(a.X - b.X) + Math.Abs(a.Y - b.Y) < EPSILON;
        }

        public int GetHashCode(PointF obj)
        {
            return (int)((obj.X + obj.Y) * FLOAT_SENSITIVITY);
        }
    }
    //public class PointComparer : IComparer<Tuple<PointF, int>>
    //{
    //    int randMask;
    //    float floatPrecision;
    //    public PointComparer(int RandMask, int FloatPrecision = 1000)
    //    {
    //        floatPrecision = FloatPrecision;
    //        randMask = RandMask;
    //    }
    //    private int intHash(float v)
    //    {
    //        int iv = (int)(v * floatPrecision) ^ randMask;
    //        return (iv % 2 == 1)? -iv: iv;
    //    }

    //    //int routersCount;
    //    //public PointComparer(int RoutersCount)
    //    //{
    //    //    routersCount = RoutersCount;
    //    //}
    //    public int Compare(Tuple<PointF, int> a, Tuple<PointF, int> b)
    //    {
    //        // if main compared value is equal, use a hash on locations and 
    //        // on compared value(we must hash the compared value to avoid repetitive patterns - the software got stuck because of this!)
    //        return a.Item2.CompareTo(b.Item2) * 4 +
    //            intHash(a.Item2+a.Item1.X).CompareTo(intHash(a.Item2 + b.Item1.X)) * 2 +
    //            intHash(a.Item2 + a.Item1.Y).CompareTo(intHash(a.Item2 + b.Item1.Y));

    //    }
    //}

    public class DistancePointComparer : IComparer<PointF>
    {
        public PointF center;
        public DistancePointComparer(PointF c)
        {
            center = c;
        }

        public int Compare(PointF x, PointF y)
        {
            return -x.distance2F(center).CompareTo(y.distance2F(center));
        }
    }

    public abstract class AdvRoutingPursuersPolicyBase : APursuersPolicy
    {
        /// <summary>
        /// utility used by some algs
        /// </summary>
        /// <returns></returns>
        protected PointF popMinAndIncrease(Random rand, Dictionary<PointF, int> pointVisitations, int maxVal = int.MaxValue)
        {
            //List<Tuple<PointF, int>> minPoints = new List<Tuple<PointF, int>>();
            List<PointF> minPoints = new List<PointF>(pointVisitations.Count/10);
            int minVal = int.MaxValue;
            
            // loop deos two things at once:  1)find Min value 2)collect all points with that value
            foreach(var v in pointVisitations)
            {
                //int newVal = v.Value;
                if (v.Value < minVal)
                {
                    minVal = v.Value;
                    //minPoints.Clear();
                    minPoints = new List<PointF>(pointVisitations.Count / 10);
                    minPoints.Add(v.Key);
                }
                else if (v.Value == minVal)
                    minPoints.Add(v.Key);
            }
            // select a random item, and remove it from pointVisitations
            var selectedPoint = minPoints.chooseRandomItem(rand);

            // increase point's value
            ++pointVisitations[selectedPoint];
            //new Tuple<PointF, int>(selectedPoint.Item1, Math.Min(maxVal, minVal + 1));

            //fixme remove pi
            //pi.addIfExists(selectedPoint, 1, 1);
            //if (pi[selectedPoint] != pointVisitations[selectedPoint])
            //{
            //    while (true) ;
            //}

            return selectedPoint;
        }
        //Dictionary<PointF, int> pi = new Dictionary<PointF, int>();

        public override List<ArgEntry> policyInputKeys()
        {
            return new List<ArgEntry>();
        }



        public override APolicyOptimizer constructTheoreticalOptimizer()
        {
            return null;
        }

        public override void gameFinished()
        {
            
        }


        //enum AlgCode : int
        //{
        //    Exhaustive = 0,
        //    NaiveGraphSearch = 1,
        //    UniformVisitGraphSearch = 2,
        //    ArbitrarySimpleRec = 3,
        //    ArbitraryWithCounters = 4,
        //    ContinuousSimple = 5,
        //    ContinuousWithTravel = 6
        //}

        //AlgCode activeAlgorithm;

        //public Dictionary<string, string> myparams; // fixme remove 
        public override bool init(AGameGraph G, IGameParams prm, IPolicyGUIInputProvider pgui, Dictionary<string, string> policyParams)
        {
            param = (AdvRoutingGameParams)prm;
            gui = pgui;

            //myparams = policyParams;
            return initEx(policyParams);
            
            //else if (AppConstants.Policies.AdvRoutingPursuersPolicy.ARBITRARY_SEARCH.tryRead(policyParams) == "1")
            //    activeAlgorithm = AlgCode.ArbitrarySimpleRec;
            //else if (AppConstants.Policies.AdvRoutingPursuersPolicy.CONTINUOUS_SEARCH.tryRead(policyParams) == "1")
            //    activeAlgorithm = AlgCode.ContinuousSimple;
            //else if (AppConstants.Policies.AdvRoutingPursuersPolicy.EXHAUSTIVE_SEARCH.tryRead(policyParams) == "1")
            //    activeAlgorithm = AlgCode.Exhaustive;
            ////else if (AppConstants.Policies.AdvRoutingPursuersPolicy.ARBITRARY_SMART_SEARCH.tryRead(policyParams) == "1")
            //  //  activeAlgorithm = AlgCode.ArbitraryWithCounters; // similar to simple continuous, but also ensures the minimal counter for all points is always 0
            //else if (AppConstants.Policies.AdvRoutingPursuersPolicy.UNIFORM_VISIT_GRAPH_SEARCH.tryRead(policyParams) == "1")
            //    activeAlgorithm = AlgCode.UniformVisitGraphSearch;
            //else if (AppConstants.Policies.AdvRoutingPursuersPolicy.SMART_CONTINUOUS_SEARCH.tryRead(policyParams) == "1")
            //    activeAlgorithm = AlgCode.ContinuousWithTravel;
            
            //return true;
        }

        /// <summary>
        /// called from Base class's init(), after gui and param are set
        /// </summary>
        /// <param name="policyParams"></param>
        /// <returns></returns>
        public abstract bool initEx(Dictionary<string, string> policyParams);

        /// <summary>
        /// location of a router which is necessarily connected to sink i.e. with distance <= N from sink
        /// called after init() and before the first getNextLocation() call
        /// </summary>
        /// <param name=""></param>
        public abstract void setInitialDetectedRouter(PointF InitRouter);

        //public void setInitialDetectedRouter(PointF InitRouter)
        //{
        //    initRouter = InitRouter;

        //    switch (activeAlgorithm)
        //    {
        //        case AlgCode.ArbitrarySimpleRec:
        //            initRecursiveAlg(false); break;
        //        case AlgCode.ContinuousSimple:
        //            initRecursiveAlg(true); break;
        //    }


        //}

        protected PointF initRouter; // to be initialized on concrete setInitialDetectedRouter()
        protected List<PointF> o_c;

     

        /// <summary>
        /// 
        /// </summary>
        /// <param name="O_C">
        /// if 
        /// </param>
        /// <returns>
        /// item1:next location to visit, 
        /// item2:true if the router at that point should be eliminated
        /// </returns>
        public abstract Tuple<PointF, bool> getNextLocation(List<PointF> O_C);
        //public Tuple<PointF,bool> getNextLocation(List<PointF> O_C)
        //{
        //    this.o_c = O_C;

        //    switch(activeAlgorithm)
        //    {
        //        case AlgCode.ArbitrarySimpleRec:
        //            return Tuple.Create(getNextRecursive(false), false);
        //        case AlgCode.ContinuousSimple:
        //            return Tuple.Create(getNextRecursive(true), false);
        //    }
        //    return null;
        //}

        // FIXME fix
        protected ThreadSafeRandom rand = new ThreadSafeRandom();// new DebugTSRandom(0);
        protected AdvRoutingGameParams param;
        protected IPolicyGUIInputProvider gui;
    }
    public class AdvRoutingPursuersPolicyExhaustiveSearch : AdvRoutingPursuersPolicyBase
    {
        List<List<PointF>> coveringPointCentersByDistance; //coveringPointCentersByDistance[0]lists nearest points to initRouter, coveringPointCentersByDistance[1] lists slightly further points and so forth
        List<PointF> coveringPointCenters; // serves exhaustive

        public override Tuple<PointF,bool> getNextLocation(List<PointF> O_C)
        { 
            this.o_c = O_C;

            if (param.sinkAlwaysDetectable)
            {


                //PointF res = coveringPointCenters.Last();
                //coveringPointCenters.RemoveAt(coveringPointCenters.Count - 1);
                //return Tuple.Create(res,false); //  fixme: ok if xhaustive works alone, but if combined with another algorithm, it really needs to be random 
                //                                // as the following line does:
                
                return Tuple.Create(coveringPointCenters.popRandomItem(rand.rand),false);

            }
            return Tuple.Create(coveringPointCenters[rand.Next(0, coveringPointCenters.Count)],false);
        }
        public override void setInitialDetectedRouter(PointF InitRouter)
        {
            initRouter = InitRouter;
            var tmpcoverage = Utils.getCoveringPoints(param.A_E.Count);

            //HashSet<Point> antiDupe = new HashSet<Point>();
            //foreach(var cp in tmpcoverage)
            //{
            //    Point newp = new Point((int)(cp.X * 100),(int)(cp.Y * 100));
            //    if(antiDupe.Contains(newp))
            //    {

            //    }
            //    antiDupe.Add(newp);
            //}



            PatternRandomizer p = new PatternRandomizer(tmpcoverage.Count, tmpcoverage.Count);
            p.Randomize(rand, false, -1, true);
            coveringPointCenters = new List<PointF>();
            for (int i = 0; i < p.CurrentlyUsedPoints.Count; ++i)
                coveringPointCenters.Add(tmpcoverage[p.CurrentlyUsedPoints[i]]);
            
            if (param.sinkAlwaysDetectable)
            {
                //coveringPointCenters.Sort(new DistancePointComparer(new PointF(0, 0)));
            }

            // translate exhaustive search coverage by 'initRouter':
            for (int i = 0; i < coveringPointCenters.Count; ++i)
                coveringPointCenters[i] = coveringPointCenters[i].add(initRouter);

            // for some reason, the randomness has a significant bias, so we shuffle coveringPointCenters
            //coveringPointCenters = coveringPointCenters.moveAndShuffle(rand.rand);
        }
        public override bool initEx(Dictionary<string, string> policyParams) { return true; }
    }
    public class AdvRoutingPursuersPolicyNaiveGraphSearch : AdvRoutingPursuersPolicyBase
    {
        List<PointF> detectedPoints; // serves naive graph search
        HashSet<PointF> detectedPointsHash; // serves naive graph search and uniform graph search
        
        public override Tuple<PointF, bool> getNextLocation(List<PointF> O_C)
        {
            
            foreach (var p in O_C)
                if (!detectedPointsHash.Contains(p))
                {
                    detectedPoints.Add(p);
                    detectedPointsHash.Add(p);
                }

            PointF randPInGraph = detectedPoints[rand.Next(0, detectedPoints.Count)];
            if (param.accurateInterception)
                return Tuple.Create(randPInGraph,false); // all 'detectedPoints' are router locations. if the router selected transmits to another router, we advance

            var rpn = Utils.getCoveringNeighbors(randPInGraph);
            return Tuple.Create(rpn[rand.Next(0, rpn.Count)],false);

            
        }
        public override void setInitialDetectedRouter(PointF InitRouter)
        {
            initRouter = InitRouter;
            detectedPoints = new List<PointF>();
            detectedPointsHash = new HashSet<PointF>();
            detectedPointsHash.Add(initRouter);
            detectedPoints.Add(initRouter);
        }
        public override bool initEx(Dictionary<string, string> policyParams) { return true; }
    }

    public class AdvRoutingPursuersPolicyUniformGraphSearch : AdvRoutingPursuersPolicyBase
    {
        private int orderRandomizer = 0; 

        public override Tuple<PointF, bool> getNextLocation(List<PointF> O_C)
        {

            this.o_c = O_C;

            foreach (var p in o_c)
            {
                if (!detectedPointsHash.Add(p))
                    continue;

                if (param.accurateInterception)
                {
                    //pointVisitations.Add(new Tuple<PointF, int>(p, 0));
                    //pointVisitations[p] = 0;
                    if (!pointVisitations.ContainsKey(p))
                        pointVisitations[p] = 0;
                }
                else
                    addNeighborsVisitations(p); // adds to pointVisitations several surrounding points
            }
            
            // once every some time times we randomize the sorter, to make sure the pattern doesn't corelate with router's route (yes, this actually happened!!!)
            //if (orderRandomizer >= 5)//pointVisitations.Count)
            //{
            //    orderRandomizer = 0;
            //    SortedSet<Tuple<PointF, int>> newpointVisitations = new SortedSet<Tuple<PointF, int>>(new PointComparer(rand.Next() * rand.Next(), 1000));
            //    foreach (var p in pointVisitations)
            //        newpointVisitations.Add(p);
            //    pointVisitations = newpointVisitations;
            //}
            //++orderRandomizer;


            //var m = pointVisitations.Min;
            //pointVisitations.Remove(m);
            //pointVisitations.Add(Tuple.Create(m.Item1, m.Item2 + 1));
            //return Tuple.Create(m.Item1,false);


             
            if (gui.hasBoardGUI()) // fixme uncomment
            {
                var marks = new Dictionary<string, List<PointF>>();

                //List<PointF> minVisited = new List<PointF>();
                //var maxVisited = new List<PointF>();
                var allPoints = new List<PointF>();
                foreach (var v in pointVisitations)
                {
                    allPoints.Add(v.Key);
                //    if (v.Item2 == pointVisitations.Min.Item2)
                //        minVisited.Add(v.Item1);
                //    if (v.Item2 == pointVisitations.Max.Item2)
                //        maxVisited.Add(v.Item1);
                }
                //marks["min.visited pts"] = minVisited;
                //marks["max.visited pts"] = maxVisited;
                
                //marks["allPointsToVisit"]= allPoints; // FIXME I removed this because it interferred with movie recording
                gui.markLocations(marks);   
            }

            // find minimal points, then pop a random item

            //return Tuple.Create(pointVisitations.popMinAndIncrease(),false);
            return Tuple.Create(popMinAndIncrease(rand.rand,pointVisitations), false);
        }
        public override void setInitialDetectedRouter(PointF InitRouter)
        {
            initRouter = InitRouter;
            detectedPointsHash = new HashSet<PointF>();
            detectedPointsHash.Add(initRouter);
            //pointVisitations = new SortedSet<Tuple<PointF, int>>(new PointComparer(rand.Next() * rand.Next(), 1000));
            //pointVisitations = new List<Tuple<PointF, int>>();
            pointVisitations = new Dictionary<PointF, int>(new PointEqComparer());
            //pointVisitations.Add(Tuple.Create(initRouter, 0));
            pointVisitations[initRouter] = 0;

            if (!param.accurateInterception)
                addNeighborsVisitations(initRouter);

        }

        
        public override bool initEx(Dictionary<string, string> policyParams)
        {
            return true;
        }

        private void addNeighborsVisitations(PointF p)
        {
            var neighbors = Utils.getCoveringNeighbors(p);
            foreach (var n in neighbors)
                //pointVisitations.Add(new Tuple<PointF, int>(n, 0));
                if (!pointVisitations.ContainsKey(n))
                    pointVisitations[n] = 0;
        }

        // selects a random item from pointVisitations, with minimal (int) value,
        // then increases its value by 1
        
    
        Dictionary<PointF, int> pointVisitations; // tells how many times each point was visited
        //SortedSet<Tuple<PointF, int>> pointVisitations;// tells how many times each point was visited
        HashSet<PointF> detectedPointsHash; // serves naive graph search and uniform graph search
    }

    /// <summary>
    /// implements both algorithm for arbitrary transmission simple recursive search AND
    /// continuous transmission recursive search (same, but with visitation counter per point)
    /// </summary>
    public class AdvRoutingPursuersPolicySimpleRecursiveSearch : AdvRoutingPursuersPolicyBase
    {

        // TODO: consider implementing a heuristic that does secondary sort for points to visit. 
        // the nearer a point is to a location where an interception already occured, it gets selected
        // before others with same visitation count
        //Dictionary<PointF, float> heuristicPreference = new Dictionary<PointF, float>();
        GoE.Utils.PointDataStructs.PointFSet detectedPointsSet = new GoE.Utils.PointDataStructs.PointFSet(new List<PointF>());
        HashSet<PointF> allDetectedPoints = new HashSet<PointF>();

        Dictionary<PointF, int> continuousTransmissionPointVisitations;
        //SortedSet<Tuple<PointF, int>> continuousTransmissionPointVisitations;// tells how many times each point was visited

        HashSet<PointF> W_PHash;
        List<PointF> W_P;
        Dictionary<SquareRegion, SquareRegion> allSquareWalls; // MUCH easier than maintaining all square walls per points, and impacts performance only upon detection. should have been a hashset, but in hashset you can't search and change items
        HashSet<SquareRegion> dividedSquares; // tells which squares in allSquareWalls were already divided
        List<PointF> exhaustiveSearchPoints = new List<PointF>();
        HashSet<SquareRegion> exahsutivlySearched = new HashSet<SquareRegion>();
        int maxEdgeLen = 2; // SquareRegions with smaller edge length will get exhaustive search
        int maxCounter;
        public override List<ArgEntry> policyInputKeys()
        {
            return ReflectionUtils.getStaticInstancesInClass<ArgEntry>(typeof(AppConstants.Policies.AdvRoutingPursuersPolicySimpleRecursiveSearchParams));            
        }


        public override Tuple<PointF, bool> getNextLocation(List<PointF> O_C)
        {
            //bool newDetectedPoint = false;
            //foreach (var c in O_C)
            //    newDetectedPoint |= allDetectedPoints.Add(c);
            
            this.o_c = O_C;
            float N = param.A_E.Count;

            // if we are directly searching for the sink using an exhaustive search, this takes precendence:
            if(exhaustiveSearchPoints.Count > 0)
            {
                var res = exhaustiveSearchPoints.Last();
                exhaustiveSearchPoints.RemoveAt(exhaustiveSearchPoints.Count - 1);
                return Tuple.Create(res, false);
            }

            if (o_c.Count > 0)
            {
                var squaresToDivide = new List<SquareRegion>();
                foreach (var s in allSquareWalls)
                    if (isPointOnSquare(s.Key, o_c.First()) && !dividedSquares.Contains(s.Key))
                        squaresToDivide.Add(s.Key);
                foreach (var parentSqw in squaresToDivide)
                {
                    if (parentSqw.edgeLen > maxEdgeLen)
                    {
                        var G = Utils.nextCenters(parentSqw);
                        dividedSquares.Add(parentSqw);
                        foreach (var g in G)
                        {
                            var newSqw = new SquareRegion() { edgeLen = parentSqw.edgeLen / 2, centerP = g };
                            var L = Utils.squareWall(newSqw, parentSqw, assumeContinuous);
                            allSquareWalls[newSqw] = newSqw;

                            foreach (var p in L.newPoints)
                                if (p.distance(initRouter) <= N)
                                    if (W_PHash.Add(p))
                                    {
                                        W_P.Add(p);
                                        if (assumeContinuous)
                                            continuousTransmissionPointVisitations[p] = 0;
                                    }

                            //reset counters of parent squares (happens only if parentSqw is a newly added square)
                            if (assumeContinuous)
                            {
                                foreach (var p in L.parentPoints)
                                    if (W_PHash.Contains(p))
                                        continuousTransmissionPointVisitations[p] = 0;
                            }
                        }
                    }
                    else
                    {
                        //if (parentSqw.edgeLen <= 2)
                        //{
                            // to do exhaustive search we only need one more point
                            if (W_PHash.Add(parentSqw.centerP))
                            {
                                W_P.Add(parentSqw.centerP);
                                if (assumeContinuous)
                                    exhaustiveSearchPoints.Add(parentSqw.centerP);
                                    //continuousTransmissionPointVisitations.Add(new Tuple<PointF, int>(parentSqw.centerP, 0));
                            }
                        //}
                        //else
                        //{
                        //    if (!exahsutivlySearched.Contains(parentSqw))
                        //    {
                        //        exhaustiveSearchPoints.AddRange(Utils.coverInnerRegion(parentSqw));
                        //        exahsutivlySearched.Add(parentSqw);
                        //    }
                        //}
                    }
                }
            }

            if (gui.hasBoardGUI())
            {
                var marks = new Dictionary<string, List<PointF>>();
                marks["W_P"] = W_P;
                marks["Exhaustive"] = exhaustiveSearchPoints;
                gui.markLocations(marks);
            }

            // FIXME remove
            //step++;
            //if (step > 20000000)
            //{
            //    AppSettings.WriteLogLine("tid:" + Thread.CurrentThread.ManagedThreadId.ToString()+ " frrr200000  " + initoffset.X.ToString() + "," + initoffset.Y.ToString() + "|||" + myparams[AppConstants.Policies.AdvRoutingSegmentedRouteRouterPolicy.STRATEGY_CODE.key]);
            //    AppSettings.WriteLogLine("enetring while(True)");
            //    while (true) ;
            //}
            //// fixme remove
            //if (continuousTransmissionPointVisitations == null)
            //{
            //    AppSettings.WriteLogLine("tid:" + Thread.CurrentThread.ManagedThreadId.ToString() + " null continuousTransmissionPointVisitations  " + initoffset.X.ToString() + "," + initoffset.Y.ToString() + "|||" + myparams[AppConstants.Policies.AdvRoutingSegmentedRouteRouterPolicy.STRATEGY_CODE.key]);
            //}
            //bool fail = false;
            //if (continuousTransmissionPointVisitations.Count == 0)
            //{
            //    fail = true;
            //    AppSettings.WriteLogLine("tid:" + Thread.CurrentThread.ManagedThreadId.ToString() + " empty continuousTransmissionPointVisitations  " + initoffset.X.ToString() + "," + initoffset.Y.ToString() + "|||" + myparams[AppConstants.Policies.AdvRoutingSegmentedRouteRouterPolicy.STRATEGY_CODE.key]);
            //}
            //if (W_P.Count == 0)
            //{
            //    fail = true;
            //    AppSettings.WriteLogLine("empty W_P" + initoffset.X.ToString() + "," + initoffset.Y.ToString() + "|||" + myparams[AppConstants.Policies.AdvRoutingSegmentedRouteRouterPolicy.STRATEGY_CODE.key]);
            //}
            //if (fail)
            //{
            //    AppSettings.WriteLogLine(System.Threading.Thread.CurrentThread.ManagedThreadId.ToString() + "| W_P.=");
            //    foreach (var p in W_P)
            //    {
            //        AppSettings.WriteLogLine(System.Threading.Thread.CurrentThread.ManagedThreadId.ToString() + "|" + p.X.ToString() + "," + p.Y.ToString());
            //    }
            //    while (true) ;
            //}
            
            if (!assumeContinuous)
                return Tuple.Create(W_P.chooseRandomItem(rand.rand),false);

            return Tuple.Create(popMinAndIncrease(rand.rand,continuousTransmissionPointVisitations,maxCounter), false);
        }

        //int step = 0; // fixme remove 


        //PointF initoffset; // FIXME remove
        /// <summary>
        /// populates sqWallsToVisit such that for at least
        /// one square cycle in L sqWallsToVisit it holds that dest is in (In(L) \cup Range(L))) and
        /// initRouter is in Out(L)
        /// </summary>
        public override void setInitialDetectedRouter(PointF InitRouter)
        {
            initRouter = InitRouter;

            if (assumeContinuous)
            {
                //continuousTransmissionPointVisitations = new SortedSet<Tuple<PointF, int>>(new PointComparer(rand.Next() * rand.Next(), 1000));
                //continuousTransmissionPointVisitations = new List<Tuple<PointF, int>>();
                continuousTransmissionPointVisitations = new Dictionary<PointF, int>(new PointEqComparer());
            }

            dividedSquares = new HashSet<SquareRegion>();

            float N = param.A_E.Count;
            PointF offset = // begin with a random offset in the including square 
                initRouter.addF(
                    (float)((-1 + 2 * rand.NextDouble()) * N),
                    (float)((-1 + 2 * rand.NextDouble()) * N));

            //offset = new PointF(6.596564f, 40.95478f);
            //initoffset = offset; 

            float eLen = 4 * N;

            //sqWallsToVisit = new Dictionary<PointF, List<SquareRegion>>();
            allSquareWalls = new Dictionary<SquareRegion, SquareRegion>();
            var childSqw = new SquareRegion() { centerP = offset, edgeLen = eLen };
            allSquareWalls[childSqw] = childSqw;
            W_P = new List<PointF>();
            W_PHash = new HashSet<PointF>();
            bool isTopSqWall = true;
            while (eLen > maxEdgeLen) 
            {
                var currentSqWall = new SquareRegion() { centerP = offset, edgeLen = eLen };
                var G = Utils.nextCenters(currentSqWall);
                offset = Utils.getIncludingDividedSquare(currentSqWall, initRouter).centerP;
                foreach (var g in G)
                {
                    childSqw = new SquareRegion() { centerP = g, edgeLen = eLen / 2 };
                    allSquareWalls[childSqw] = childSqw;
                    AddedPoints L;
                    if (isTopSqWall)
                    {
                        L = new AddedPoints();
                        L.parentPoints = new List<PointF>();
                        L.newPoints = Utils.squareWall(childSqw);

                        if (assumeContinuous)
                            foreach (var v in L.newPoints)
                                childSqw.squareWallPoints.Add(Tuple.Create(v, true));
                    }
                    else
                    {
                        L = Utils.squareWall(childSqw, allSquareWalls[currentSqWall], assumeContinuous);
                        if (assumeContinuous)
                        {
                            foreach (var v in L.newPoints)
                                childSqw.squareWallPoints.Add(Tuple.Create(v, true));
                            foreach (var v in L.parentPoints)
                                childSqw.squareWallPoints.Add(Tuple.Create(v, true));
                        }
                    }

                    foreach (var p in L.newPoints)
                        if (p.distance(initRouter) <= N)
                            if (W_PHash.Add(p))
                            {
                                W_P.Add(p);
                                if (assumeContinuous)
                                    //continuousTransmissionPointVisitations.Add(new Tuple<PointF, int>(p, 0));
                                    //continuousTransmissionPointVisitations[p] = 0;
                                    continuousTransmissionPointVisitations[p] = 0;
                            }
                }
                isTopSqWall = false;
                eLen = eLen / 2;
            }
            
            if (W_PHash.Add(offset)) // offset is always updated to be the center of the smallest cycle
            {
                W_P.Add(offset);
                if (assumeContinuous)
                    //continuousTransmissionPointVisitations.Add(new Tuple<PointF, int>(offset, 0));
                    //continuousTransmissionPointVisitations[offset] = 0;
                    continuousTransmissionPointVisitations[offset] = 0;
            }

        }

        bool assumeContinuous;
        public override bool initEx(Dictionary<string, string> policyParams)
        {
            //string maxEdgeLenStr = AdvRoutingPursuersPolicySimpleRecursiveSearchParams.MAX_EDGELEN_FOR_EXHAUSTIVE.tryRead(policyParams);
            //if (maxEdgeLenStr == AdvRoutingPursuersPolicySimpleRecursiveSearchParams.MAX_EDGELEN_FOR_EXHAUSTIVE.val)
            //    maxEdgeLen = (int)Math.Log(param.A_E.Count, 2);
            //else
            //    maxEdgeLen = (int)float.Parse(maxEdgeLenStr);

            string transmissionConstraint =
                AdvRoutingPursuersPolicySimpleRecursiveSearchParams.ASSUME_TRANSMISSION_CONSTRAINT.tryRead(policyParams);
            switch(transmissionConstraint)
            {
                case "non":
                    assumeContinuous = false; break;
                case "cont":
                    assumeContinuous = true; break;
                default:
                    assumeContinuous = param.forceContinuousTransmission; break;
            }
                
            maxCounter = int.Parse(AdvRoutingPursuersPolicySimpleRecursiveSearchParams.MAX_COUNTER.tryRead(policyParams));
            
            return true;
        }
    }

    /// <summary>
    /// graph search only. designed for finding a way to handle a square of sqrtN X sqrtN routers
    /// in O(N) (or, hopefully, proving this is impossible)
    /// </summary>
    public class AdvRoutingPursuersPolicySqrtNXSqrtNKiller: AdvRoutingPursuersPolicyBase
    {
        private bool resetCondition()
        {
            return timeSteps > 2 * param.A_E.Count;
        }
        /// <summary>
        /// visits points adjacent to previously successfull search points.
        /// with probability 0.5, select a point furthest from init router
        /// with probability (0.5)^2, select the next to furthest, with prob. (0.5)^3 the next furthest and so on
        /// </summary>
        /// <param name="O_C"></param>
        /// <returns></returns>
        public override Tuple<PointF, bool> getNextLocation(List<PointF> O_C)
        {
            ++timeSteps;
            if (resetCondition())
            {
                reset();
                // since the parent of O_C doesn't exist in cleared data, we just ignore it:
                return Tuple.Create(fromGrid(pointsToVisitByDistance[0].chooseRandomItem(rand.rand)), false);
            }
            
            if (O_C.Count > 0)
            {
                bool refreshGraph = false; // if adding O_C.first() just created a cycle, we need to
                                           // refresh the distances from init router
                Point truncOC = toGrid(O_C.First());
                int visitedDist = pointsDistances[truncOC];
                maxDist = Math.Max(maxDist, visitedDist + 1);

                // add new points to visit around the successfull search
                foreach (var n in getCoveringNeighbors(O_C.First()))
                {
                    // don't override previously added points
                    if (pointsDistances[toGrid(n)] != UNINITIALIZED_DISTANCE)
                        continue;

                    // newly added point is a direct continuation of O_C.First()
                    var newP = toGrid(n);
                    pendingPoints[truncOC].Add(newP);
                    pointsDistances[newP] = visitedDist + 1;
                    allSearches.Add(newP);
                    pendingPoints[newP] = new List<Point>(); // newp is not successfull yet, so the list is empty

                    // if the added point is actually closer to init router than we expect (at laest 2 paths to reach newP),
                    // we'll have to refresh the distances in the graph
                    foreach (var nn in getCoveringNeighbors(n))
                        if (pointsDistances[toGrid(nn)] < visitedDist)
                        {
                            refreshGraph = true;
                            break;
                        }
                }
                
                // use dijakstra
                if(refreshGraph)
                {
                    PointDictionary<int> newGraph = constructPointsDictionary();
                    GraphAlgorithms.FindShortestPath.getAllDistances(allSearches, toGrid(initRouter), getAdjacentPoints,newGraph);
                    pointsDistances = newGraph;
                    redreshPointsToVisitByDistance();
                }
            }
            
            int dist = 0;
            // select distance to visit, with exponential distribution (dist = 0 means furthest from init router)
            while (rand.Next() % 2 == 0)
                ++dist;
            dist = Math.Min(dist, pointsToVisitByDistance.Count - 1);
            while (pointsToVisitByDistance[dist].Count == 0)
                dist = (dist + 1) % pointsToVisitByDistance.Count;

            if(gui.hasBoardGUI())
            {
                Dictionary<string, List<PointF>> markings = new Dictionary<string, List<PointF>>();
                for(int d = 0; d < maxDist; ++d)
                {
                    markings[d.ToString()] = new List<PointF>();
                    foreach (var dp in pointsToVisitByDistance[d])
                        markings[d.ToString()].Add(fromGrid(dp));
                }
                gui.markLocations(markings);
            }
            return Tuple.Create(fromGrid(pointsToVisitByDistance[dist].chooseRandomItem(rand.rand)), false);   
        }

        private void reset()
        {
            timeSteps = 0;
            sqrtN = (int)Math.Sqrt(param.A_E.Count);
            Point trunInitRouter = toGrid(initRouter);


            pointsDistances = constructPointsDictionary();
            pointsDistances[trunInitRouter] = 0;

            pendingPoints = new Dictionary<Point, List<Point>>();
            pendingPoints[trunInitRouter] = new List<Point>();

            allSearches = new List<Point>();
            allSearches.Add(trunInitRouter);
            foreach (var v in getCoveringNeighbors(initRouter))
            {
                var truncV = toGrid(v);
                pointsDistances[truncV] = 1;
                pendingPoints[trunInitRouter].Add(truncV);
                pendingPoints[truncV] = new List<Point>(); // truncV is not a successfull search yet, so the list is empty
                allSearches.Add(truncV);
            }
            maxDist = 1;

            redreshPointsToVisitByDistance();

        }
        public override void setInitialDetectedRouter(PointF InitRouter)
        {
            
            initRouter = InitRouter;
            reset();
        }

        /// <summary>
        /// Rebuilds pointsToVisitByDistance, according to pointsToVisitByDistance.
        /// Assumes maxDist and allSearches are updated.
        /// </summary>
        private void redreshPointsToVisitByDistance()
        {
            pointsToVisitByDistance = new List<List<PointF>>();
            for (int d = 0; d <= maxDist; ++d)
                 pointsToVisitByDistance.Add(new List<PointF>());                    
            
            foreach (var v in allSearches)
                if(pendingPoints[v].Count == 0) //make sure the point is not a successfull search point yet
                    pointsToVisitByDistance[maxDist-pointsDistances[v]].Add(v); // sets furthest points at idx 0
        }
        public override bool initEx(Dictionary<string, string> policyParams) { return true; }

        private PointF fromGrid(PointF gridPoint)
        {
            //float diff = (gridPoint.Y % 2) * SQRT3 / 2;
            //return new PointF( (gridPoint.X -3 + (1 - gridPoint.Y % 2) ) * SQRT3 - diff,  (gridPoint.Y - 1) * SQRT3);
            return pointMapper[gridPoint.toPointTrunc()];
        }

        Dictionary<Point, PointF> pointMapper = new Dictionary<Point, PointF>(); // fixme: dirty fix
        private Point toGrid(PointF searchPoint)
        {
            var p = searchPoint.toPointRound().add(4,4);
            pointMapper[p] = searchPoint;
            return p;
            //int yGrid = 1 + (int)Math.Round(searchPoint.Y / SQRT3, MidpointRounding.AwayFromZero);
            //return new Point(3+(int)Math.Round(searchPoint.X / SQRT3,MidpointRounding.AwayFromZero) - (1-yGrid %2), yGrid);
        }
        private List<Point> getAdjacentPoints(Point v)
        {
            // used by dijakstra algorithm. the algorithm works for directed graphs, and we use this 
            // to prevent impossible routes that go through several still-unsuccessfull points
            return pendingPoints[v];

            //List<Point> res = new List<Point>();
            //foreach (var diff in ADJACENT_POINTS)
            //{
            //if (pointsDistances[v.add(diff)] )
            //  res.Add(v.add(diff));
            //}
            //return res;
        }
        private PointDictionary<int> constructPointsDictionary()
        {
            return new PointDictionary<int>(sqrtN + 8, sqrtN + 8, -1); // we use trunc() on search points before accessing this structure
        }

        private int timeSteps;
        private int maxDist;
        private int sqrtN;
        private List<List<PointF>> pointsToVisitByDistance; // at index 0, furthest points from init router
        private PointDictionary<int> pointsDistances; // for all points (successfull and still-unsuccessfull)
        private List<Point> allSearches; // simple list of all points
        private Dictionary<Point, List<Point>> pendingPoints; // for each successfull search point, lists the following search points. empty lists for still-unsuccessfull points                                                              

        const float SQRT3 = 1.73205080f;
        private const int UNINITIALIZED_DISTANCE = -1;
        //private static Point[] ADJACENT_POINTS = new Point[4] { new Point(-1, -1), new Point(1, -1), new Point(-1, 1), new Point(1, 1) };
    }


    /// <summary>
    /// same as naive graph search, but filters out previously successfull points points
    /// </summary>
    public class AdvRoutingPursuersPolicyFrontGraphSearch : AdvRoutingPursuersPolicyBase
    {
        List<PointF> detectedPoints; // serves naive graph search
        HashSet<PointF> detectedPointsHash; // serves naive graph search and uniform graph search
        public override Tuple<PointF, bool> getNextLocation(List<PointF> O_C)
        {

            foreach (var p in O_C)
            {
                foreach (var pn in getCoveringNeighbors(p))
                {
                    if(detectedPointsHash.Add(pn))
                        detectedPoints.Add(pn);
                }
                detectedPoints.Remove(p); // no reason to choose the same point again
            }

            if(gui.hasBoardGUI())
            {
                Dictionary<string, List<PointF>> marks = new Dictionary<string, List<PointF>>();
                marks["front"] = detectedPoints;
                gui.markLocations(marks);
            }

            //PointF randPInGraph = detectedPoints[rand.Next(0, detectedPoints.Count)];
            //var rpn = Utils.getCoveringNeighbors(randPInGraph);
            //return Tuple.Create(rpn[rand.Next(0, rpn.Count)], false);
            return Tuple.Create(detectedPoints.chooseRandomItem(rand.rand),false);
        }
        // just accurate enough for sqrt3 grid
        class CrudePointCompare : IEqualityComparer<PointF>
        {
            public bool Equals(PointF a, PointF b)
            {
                Point ai = new Point((int)(a.X * 10), (int)(a.Y * 10));
                Point bi = new Point((int)(b.X * 10), (int)(b.Y * 10));
                return ai == bi;
            }

            public int GetHashCode(PointF obj)
            {
                return new Point((int)(obj.X * 10), (int)(obj.Y * 10)).GetHashCode();
            }
        }
        public override void setInitialDetectedRouter(PointF InitRouter)
        {
            initRouter = InitRouter;
            detectedPoints = new List<PointF>();
            detectedPointsHash = new HashSet<PointF>(new CrudePointCompare());

            foreach (var pn in getCoveringNeighbors(initRouter))
            {
                if (detectedPointsHash.Add(pn))
                    detectedPoints.Add(pn);
            }
        }
        public override bool initEx(Dictionary<string, string> policyParams) { return true; }
    }
}
