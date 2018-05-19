using GoE.Policies;
using GoE.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GoE.AppConstants;
using GoE.GameLogic;
using GoE.UI;
using System.Drawing;
using GoE.GameLogic.EvolutionaryStrategy;
using GoE.Utils.Algorithms;
using GoE.AppConstants.Algorithms;
using AForge.Genetic;
using GoE.AppConstants.GameProcess;
using GoE.Utils.Extensions;
using GoE.Utils.Genetic;
using GoE.GameLogic.EvolutionaryStrategy.EvaderSide;
using GoE.AppConstants.GameLogic;
using System.Runtime.Serialization;
using System.IO;
using System.Threading;

namespace GoE.AdvRouting
{
    
    /// <summary>
    /// base class for AdvRoutingRoutersPolicy
    /// </summary>
    abstract class AdvRoutingRouterPolicyBase : IEvadersPolicy
    {
        //public const int SinkIdx = 0;
        
        protected List<PointF> network;
        protected Dictionary<PointF, int> locationToIdx; // initialized after generateNetwork() call

        protected AdvRoutingGameParams param;
        protected IPolicyGUIInputProvider gui;
        protected ThreadSafeRandom rand;

        public PointF SourceRouter { get; private set; } // used only if param.SingleSourceRouter is true

        /// <summary>
        /// initialized after first generateNetwork() call
        /// </summary>
        public PointF sink { get; private set; }

        public virtual bool GaveUp
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="capturedPoints">
        /// tells which points were captured
        /// </param>
        /// <param name="reward">
        /// tells how many routers are connected to the sink and transmit data
        /// </param>
        /// <returns></returns>
        public abstract List<int> nextTransmittingPoints(List<PointF> capturedPoints, out int reward);

        public virtual void gameFinished()
        {

        }

        /// <summary>
        /// called in the same thread of following nextTransmittingPoints() and getInitialNetwork() calls
        /// </summary>
        /// <param name="G"></param>
        /// <param name="prm"></param>
        /// <param name="initializedPursuers"></param>
        /// <param name="pgui"></param>
        /// <param name="policyParams"></param>
        /// <returns></returns>
        public bool init(AGameGraph G, IGameParams prm, APursuersPolicy initializedPursuers, IPolicyGUIInputProvider pgui, Dictionary<string, string> policyParams)
        {
            rand = new ThreadSafeRandom();
            param = (AdvRoutingGameParams)prm;
            gui = pgui;
            return initEx(G, initializedPursuers, policyParams);
        }
        protected abstract bool initEx(AGameGraph G, APursuersPolicy initializedPursuers, Dictionary<string, string> policyParams);

        public abstract List<ArgEntry> policyInputKeys { get; }

        /// <summary>
        /// </summary>
        /// <returns>
        /// after called, this.SourceRouter and this.Sink should be usable
        /// </returns>
        public List<PointF> getInitialNetwork()
        {

            if (param.A_E.Count == 0)
                return new List<PointF>();//throw new Exception("Invalid parameter: 0 routers");


            PointF srouter, dest;
            if(!generateNetwork(out network, out srouter, out dest))
                return new List<PointF>();
            SourceRouter = srouter;
            sink = dest;

            locationToIdx = new Dictionary<PointF, int>();
            for (int n = 0; n < network.Count; ++n)
                locationToIdx[network[n]] = n;
            return network;
        }

        /// <summary>
        /// outputs network, and initializes SourceRouter
        /// After generateNetwork() is called,
       ///  locationToIdx is initialized, and network is kept as member (under the same name)
        /// </summary>
        /// <param name="network"></param>
        protected abstract bool generateNetwork(out List<PointF> network, out PointF sourceRouter, out PointF sink);
    }

    /// <summary>
    /// creates a network composed of a number of 'segments', where each segment has a number of parallel routes which start and end at a single end point (and optionally a number of parallel false  routes)
    /// </summary>
    class AdvRoutingSegmentedRouteRouterPolicy : AdvRoutingRouterPolicyBase
    {
        public AdvRoutingSegmentedRouteRouterPolicy() { }

        protected class RouteSegment
        {
            public int lastTransmittedReal = 0;
            public List<List<int>> realParallelRoutes;
            public List<List<int>> fakeParallelRoutes;
            public List<int> outputNodes, inputNodes;
            // regardless of the selected realParallelRoutes , outputnodes and inputNodes will always transmit from the previous real route to the next real route
            // (initial route is connected to sink node)
        }
        
        public bool TransmitContinuously { get; protected set; }
 
        public override List<ArgEntry> policyInputKeys
        {
            get
            {
                return ReflectionUtils.getStaticInstancesInClass<ArgEntry>(typeof(AppConstants.Policies.AdvRoutingSegmentedRouteRouterPolicy));
            }
        }
        
        /// <summary>
        /// returns all points in the network. first point is the sink
        /// </summary>
        protected override bool initEx(AGameGraph G, APursuersPolicy initializedPursuers, Dictionary<string, string> policyParams)
        {

            TransmitContinuously =
                AppConstants.Policies.AdvRoutingSegmentedRouteRouterPolicy.TRANSMIT_CONTINUOUSLY.tryRead(policyParams) == "1";

            StrategyCode = AppConstants.Policies.AdvRoutingSegmentedRouteRouterPolicy.STRATEGY_CODE.tryRead(policyParams);

            
            //StrategyCode  = new AdvRoutingSegmentedStrategyCodeChromosome().CreateNew().ToString();
            //fixme remove:
            //StrategyCode = "1,8,1,-1,0,1,55,0.777777777777778,-1,0,4,52,0.111111111111111,-1,0,3,96,0.888888888888889,-1,0,4,";
            //AppSettings.WriteLogLine("fighting! tid:" + Thread.CurrentThread.ManagedThreadId.ToString() + StrategyCode);//fixme remove
            

            assumeExhaustive = "1" == AppConstants.Policies.AdvRoutingSegmentedRouteRouterPolicy.ASSUME_EXHAUSTIVE_SEARCH.tryRead(policyParams);
            assumeContinuousRecAlg = "1" == AppConstants.Policies.AdvRoutingSegmentedRouteRouterPolicy.ASSUME_CONTINUOUS_SEARCH.tryRead(policyParams);
            assumeArbitraryRecAlg = "1" == AppConstants.Policies.AdvRoutingSegmentedRouteRouterPolicy.ASSUME_ARBITRARY_SEARCH.tryRead(policyParams);

            return true;
        }

        const int SinkIdx = 0;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="capturedPoints"></param>
        /// <param name="reward"></param>
        /// <returns>
        /// corresponds points returned in getInitialNetwork();
        /// </returns>
        public override List<int> nextTransmittingPoints(List<PointF> capturedPoints, out int reward)
        {
            reward = 0;
            List<int> transmitting = new List<int>(network.Count);
            foreach (var s in routeSegments)
            {
                if (s.fakeParallelRoutes.Count > 0)
                {
                    var f = s.fakeParallelRoutes.chooseRandomItem(rand.rand);

                    //foreach (var v in f)
                    //  transmitting.Add(v);
                    //transmitting.UnionWith(f);
                    transmitting.AddRange(f);

                  reward += f.Count;
                }

                if (s.realParallelRoutes.Count > 0)
                {
                    //var r = s.realParallelRoutes.chooseRandomItem(rand.rand);
                    var r = s.realParallelRoutes[s.lastTransmittedReal]; // by transmitting in round robin, we maximize the time between transmissions from the same route
                    int lastTransmittedRealIncrease = 1;// + rand.Next() % 2;
                    s.lastTransmittedReal = (s.lastTransmittedReal + lastTransmittedRealIncrease) % s.realParallelRoutes.Count;

                    //foreach (var v in r)
                    //  transmitting.Add(v);
                    //transmitting.UnionWith(r);
                    transmitting.AddRange(r);
                    reward += r.Count;
                }

                //foreach (var v in s.inputNodes)
                //  transmitting.Add(v);
                //transmitting.UnionWith(s.inputNodes);
                transmitting.AddRange(s.inputNodes);
                reward += s.inputNodes.Count;
                //foreach (var v in s.outputNodes)
                //  transmitting.Add(v);
                //transmitting.UnionWith(s.outputNodes);
                transmitting.AddRange(s.outputNodes);
                reward += s.outputNodes.Count; // FIXME ELIMINATION: afteer nodes are destroyed check connectivity to sink, and calculate reward
            }

            //foreach (var dr in continuouslyTransmittingRouters)
            //  transmitting.Add(dr);
            //transmitting.UnionWith(continuouslyTransmittingRouters);
            transmitting.AddRange(continuouslyTransmittingRouters);
            transmitting.Add(SinkIdx);
            return transmitting;
        }

        /// <summary>
        /// the folowing affect StrategyCode, if StrategyCode is ""
        /// </summary>
        private bool assumeContinuousRecAlg,
                      assumeArbitraryRecAlg,
                      assumeExhaustive; // if sink may always be detected, assume that the exhaustive search "grows". otherwise random

        // StrategyCode Format :
        // For disconnected routers, Average distance from any non-random router 0 to 1,
        // ***for each segment:
        // # of total parallel routes in segment, 
        // Ratio of routers participating in segment  0 to 1,
        // Ratio of how many of the segment routes them are fake,
        // Ratio of how many of the segment routers are allocated to fakse 0 to 1 )
        // # for how many fake routes per each real route (spread one after the other). If 0, fakes are shuffled. if this val is x ~=0, and fake routes = -1, this sets fake routes to x*realroutes, and each route is of length 1!
        //
        // *** ratios are multiplied by router#. All ratios of router# must sum to <1. 
        // ***remaining routers will be randomly scattered disconnected routers (with distance coresponding to first value in the format).
        // ***values are separated by comma
        // ***only numeric values are parsed and letters ignored, so the value may contain comments
        // FIXME: consider adding a curve (e.g. x axis always growing, but y direction changes with each segment. each segment is connected to previous segment 
        // from one of the extremes)
        public string StrategyCode { get; set; }


        //private void addSegment(int totalSegmentRoutes, int totalSegmentRouters, int fakeRoutes, int fakeRouters)
        //{
        //     = (int)valsF[segI];
        //     = (int)Math.Round(valsF[segI + 1] * param.A_E.Count);

        //    = (int)Math.Round(totalSegmentRoutes * valsF[segI + 2]);
        //     = (int)Math.Round(valsF[segI + 3] * totalSegmentRouters); // each fake route has at least 1 router

        //}

        
        protected override bool generateNetwork(out List<PointF> network, out PointF sourceRouter, out PointF sinkRouter)
        {
            network = new List<PointF>(); //AlgorithmUtils.getRepeatingValueList(new PointF(0, 0), param.A_E.Count);
            //allRouterIndicies = new HashSet<int>();
            //survivingRouters = new HashSet<PointF>();
            float avgDisconnectedDistFromConnected;
            //avgFalseConnectedPathLen;

            //int totalDisconnectedRouters;
            //totalFalseConnectedRouter;

            List<Tuple<int, int>> routersPerSegment = new List<Tuple<int, int>>(); //item1: routes count. item2: max total routers dedicated to these routes

            //List<string> vals = ParsingUtils.separateCSV(StrategyCode);
            //List<float> valsF = new List<float>();
            //foreach (var v in vals)
            //{
            //    string cs = ParsingUtils.filterOutNonNumericValues(v);
            //    if (cs.Length > 0)
            //        valsF.Add(float.Parse(cs));
            //}
            List<float> valsF = ParsingUtils.extractValues(StrategyCode);


            // note that if two nodes have the same location, this causes bugs. we ensure random nodes don't
            // fall exactly on another node
            avgDisconnectedDistFromConnected = valsF[0] * param.A_E.Count;
            avgDisconnectedDistFromConnected = Math.Max(avgDisconnectedDistFromConnected, 0.001f);


            routeSegments = new List<RouteSegment>();
            //int remainedRouters = param.A_E.Count - totalDisconnectedRouters;// - totalFalseConnectedRouter;
            //allRouterIndicies.Add(SinkIdx);
            
            network.Add(new PointF(rand.Next(-param.A_E.Count, 2 * param.A_E.Count), rand.Next(-param.A_E.Count, 2 * param.A_E.Count))); 
            //network.Add(new PointF(0, 0)); // sink is in (0,0)

            //survivingRouters.Add(network[SinkIdx]);

            const int sinkIdx = 0;
            int segIdx = 0;
            for (int valsI = 1; valsI < valsF.Count; valsI += 5)
            {
                int totalSegmentRoutes = (int)valsF[valsI];
                int totalSegmentRouters = (int)Math.Round(valsF[valsI + 1] * param.A_E.Count);

                totalSegmentRouters = Math.Min(totalSegmentRouters, param.A_E.Count - network.Count); // make sure we don't use more routers than what we have

                int fakeRoutes;
                if (valsF[valsI + 2] == -1)
                    fakeRoutes = -1;
                else
                    fakeRoutes = (int)Math.Round(totalSegmentRoutes * valsF[valsI + 2]);
                int fakeRouters = (int)Math.Round(valsF[valsI + 3] * totalSegmentRouters); // each fake route has at least 1 router
                int fakesPerReal = ((int)valsF[valsI + 4]);

                if (totalSegmentRoutes <= 0)
                    continue;
                addSegment(totalSegmentRoutes, totalSegmentRouters, fakeRoutes, fakeRouters, segIdx++, fakesPerReal);
            }


            continuouslyTransmittingRouters = new List<int>();
            List<PointF> connectedRouters = new List<PointF>(network);

            
            sourceRouter = network.Last();
            

            sinkRouter = network[sinkIdx];

            // sanity check (the case where there are no segments at all and routers are all random is forbidden)
            if (network.Count == 1)
                return false; // illegal chromosome

            // note that if two nodes have the same location, this causes bugs. we ensure random nodes don't
            // fall exactly on another node
            while (network.Count < param.A_E.Count)
            {
                PointF disconnectedRouter = connectedRouters.chooseRandomItem(rand.rand);
                float x = -1 + 2 * (float)rand.NextDouble();
                float y = (float)Math.Sqrt(1 - x * x) * (-1 + 2 * rand.Next(2));
                float distanceMult = 0.001f + (float)(2 * rand.NextDouble()); // can never be 0

                //survivingRouters.Add(disconnectedRouter.
                //    add(x * avgDisconnectedDistFromConnected * distanceMult, 
                //        y * avgDisconnectedDistFromConnected * distanceMult));

                network.Add(disconnectedRouter.
                    addF(x * avgDisconnectedDistFromConnected * distanceMult,
                        y * avgDisconnectedDistFromConnected * distanceMult));

                continuouslyTransmittingRouters.Add(network.Count - 1);
            }
            return true;
        }


        ///// <summary>
        ///// since the first segment is connected to the sink, we want the sink to be in the center.
        ///// Additionally, we want to minimize the part of the first layer in the first segment that transmits
        ///// each turn
        ///// </summary>
        //private void addFirstSegment(int totalSegmentRoutes,
        //    int totalSegmentRouters,
        //    int fakeRoutes,
        //    int fakeRouters,
        //    int segI)
        //{

        //}

        // FIXME: this turned to be very cumbersome. consdier changing the format
        private void addSegment(int totalSegmentRoutes,
            int totalSegmentRouters,
            int fakeRoutes,
            int fakeRouters,
            int segI,
            int fakesPerReal)
        {

            if (2 * totalSegmentRoutes > totalSegmentRouters)
                totalSegmentRoutes = totalSegmentRouters / 2;

            if (totalSegmentRouters < 2 || totalSegmentRoutes < 1)
                return;

            // fake routes have at least 2 nodes - at the begining of segment and at the end of segment.
            // make sure we have enough routes
            if (fakeRoutes > 0)
                fakeRoutes = Math.Min(fakeRoutes, fakeRouters / 2);

            int realRoutes = totalSegmentRoutes - fakeRoutes;
            int realRouters = totalSegmentRouters - fakeRouters;

            // as the format specifies, if fakeRoutes == -1 and fakesPerReal != 0,
            // we re-set fake routes so fakeRoutes = fakesPerReal * realRoutes
            if (fakeRoutes == -1 && fakesPerReal != 0)
            {
                fakeRoutes = totalSegmentRoutes - (int)Math.Ceiling(((float)totalSegmentRoutes) / (fakesPerReal + 1));
                realRoutes = totalSegmentRoutes - fakeRoutes;
                fakeRouters = fakeRoutes * 2;
                realRouters = totalSegmentRouters - fakeRouters;
            }

            if (realRoutes > realRouters / 2)
            {
                realRoutes = realRouters / 2;
                totalSegmentRoutes = realRoutes + fakeRoutes;
            }

            PointF routeStartPoint;
            if (segI == 0)
                routeStartPoint = network[SinkIdx];
            else
                routeStartPoint = network[routeSegments.Last().outputNodes.Last()];

            routeSegments.Add(new RouteSegment());
            routeSegments.Last().realParallelRoutes = new List<List<int>>();
            routeSegments.Last().fakeParallelRoutes = new List<List<int>>();
            routeSegments.Last().outputNodes = new List<int>();
            routeSegments.Last().inputNodes = new List<int>();

            int realRouteLength = Math.Max(2, realRouters / realRoutes); // each route has at least 1 start and 1 end node
            float avgFakeRouteLength = Math.Min(realRouteLength, ((float)fakeRouters) / fakeRoutes);

            // make sure varaibles don't cause an impossible situation
            if (realRouteLength * realRoutes > realRouters)
                fakeRoutes = 0;

            List<bool> fakeRoutePatternLst;
            if (fakesPerReal != 0)
            {
                int realsToAdd = 1;
                int addedFakes = 0, addedReals = 0;
                fakeRoutePatternLst = new List<bool>(totalSegmentRoutes);
                while (addedFakes < fakeRoutes && addedReals < realRoutes)
                {
                    realsToAdd = Math.Min(realsToAdd, realRoutes - addedReals);
                    fakesPerReal = Math.Min(fakesPerReal, fakeRoutes - addedFakes);
                    fakeRoutePatternLst.AddRange(AlgorithmUtils.getRepeatingValueList(false, realsToAdd));
                    fakeRoutePatternLst.AddRange(AlgorithmUtils.getRepeatingValueList(true, fakesPerReal));
                    addedFakes += fakesPerReal;
                    addedReals += realsToAdd;
                }
                fakeRoutePatternLst.AddRange(AlgorithmUtils.getRepeatingValueList(false, realRoutes - addedReals));
                fakeRoutePatternLst.AddRange(AlgorithmUtils.getRepeatingValueList(true, fakeRoutes - addedFakes));
            }
            else
            {
                fakeRoutePatternLst = AlgorithmUtils.getRepeatingValueList(false, totalSegmentRoutes);
                PatternRandomizer fakeRoutePattern = new PatternRandomizer(totalSegmentRoutes, fakeRoutes);
                fakeRoutePattern.Randomize(rand, false, -1, true);
                foreach (var v in fakeRoutePattern.CurrentlyUsedPoints)
                    fakeRoutePatternLst[v] = true;
            }

            int remainingFakeRoutesNodes = fakeRouters;
            int remainingFakeRoutes = fakeRoutes;
            for (int routeIdx = 0; routeIdx < totalSegmentRoutes; ++routeIdx)
            {
                addSegmentRoute(routeStartPoint,
                    realRouteLength,
                    avgFakeRouteLength,
                    fakeRoutePatternLst,
                    remainingFakeRoutesNodes,
                    remainingFakeRoutes, routeIdx);
            }

        }

        private void addSegmentRoute(
            PointF routeStartPoint,
            int realRouteLength,
            float avgFakeRouteLength,
            List<bool> isFakeRoute,
            int remainingFakeRoutesNodes,
            int remainingFakeRoutes,
            int routeIdx)
        {
            routeStartPoint = routeStartPoint.addF(0, routeIdx + 1);

            int routeLen;
            if (isFakeRoute[routeIdx])
            {
                routeSegments.Last().fakeParallelRoutes.Add(new List<int>());
                // make a fake route
                int randRouteLen = (int)Math.Round(rand.NextDouble() * (2 * avgFakeRouteLength - 2)); // -2 since at least 2 nodes are used
                routeLen = 2 + new int[] { remainingFakeRoutesNodes - remainingFakeRoutes * 2, realRouteLength - 2, randRouteLen }.Min();
            }
            else
            {
                routeLen = realRouteLength; // includes first and last
                routeSegments.Last().realParallelRoutes.Add(new List<int>());
            }

            // run from 1 to (routeLen - 1), since first and last node is always in input/output nodes
            routeSegments.Last().inputNodes.Add(network.Count);
            network.Add(routeStartPoint);
            for (int ni = 1; ni < routeLen - 1; ++ni)
            {
                if (isFakeRoute[routeIdx])
                    routeSegments.Last().fakeParallelRoutes.Last().Add(network.Count);
                else
                    routeSegments.Last().realParallelRoutes.Last().Add(network.Count);

                network.Add(routeStartPoint.addF(realRouteLength - ni - 1, 0));
            }
            routeSegments.Last().outputNodes.Add(network.Count);
            network.Add(routeStartPoint.addF(realRouteLength - 1, 0));

        }

        protected List<int> continuouslyTransmittingRouters;
        protected List<RouteSegment> routeSegments;
    }


    /// <summary>
    /// this router algorithm allows minimizing the continuous transmission recursive pursuers algorithm
    /// and against uniform graph search.
    /// since these pursuers alg. has no counter limit, this algorithm makes sure the counter are high for dead end paths, then transmit
    /// from another path.
    /// - transmit exclusively into false direction - reveal half path for some time (typically >Nlog^4(N))
    /// - transmit exclusively into false direction  - reveal remaining path for some time (typically >Nlog^2(N)
    /// - transmit also into real direction
    /// </summary>
    class AdvRoutingContInterKillerRouterPolicy : AdvRoutingRouterPolicyBase
    {
        public override List<ArgEntry> policyInputKeys
        {
            get
            {
                return ReflectionUtils.getStaticInstancesInClass<ArgEntry>(typeof(AppConstants.Policies.AdvRoutingContInterKillerRouterPolicy));
            }
        }
        protected override bool initEx(AGameGraph G, APursuersPolicy initializedPursuers, Dictionary<string, string> policyParams)
        {
            falseTransmissionFirstHalf = int.Parse(
                AppConstants.Policies.AdvRoutingContInterKillerRouterPolicy.FALSE_TRANSMISSIONS_FIRST_HALF.tryRead(policyParams));
            falseTransmissionFirstHalf = (int)(
                param.A_E.Count * MathEx.PowInt(Math.Log(param.A_E.Count, 2), falseTransmissionFirstHalf));

            falseTransmissionSecondHalf = int.Parse(
                AppConstants.Policies.AdvRoutingContInterKillerRouterPolicy.FALSE_TRANSMISSIONS_SECOND_HALF.tryRead(policyParams));
            falseTransmissionSecondHalf = (int)(
                param.A_E.Count * MathEx.PowInt(Math.Log(param.A_E.Count, 2), falseTransmissionSecondHalf));
            
            return true;
        }
        public override List<int> nextTransmittingPoints(List<PointF> capturedPoints, out int reward)
        {
            HashSet<int> transmitting = new HashSet<int>();
            ++timeStep;

            if (timeStep <= falseTransmissionFirstHalf)
            {
                reward = 0;
                return fakeFirstHalf;
            }
            if (timeStep <= falseTransmissionFirstHalf + falseTransmissionSecondHalf)
            {
                reward = 0;
                return allFake;
            }

            reward = param.A_E.Count - 1;
            return all;
        }
        
        protected override bool generateNetwork(out List<PointF> network, out PointF sourceRouter, out PointF sinkRouter)
        {
            network = new List<PointF>(); //AlgorithmUtils.getRepeatingValueList(new PointF(0, 0), param.A_E.Count);

            // add sink:
            network.Add(new PointF(0, 0));
            sinkRouter = network.First();

            // real route is composed of only 3 nodes
            for (int i = 0; i < 3; ++i)
                network.Add(network[network.Count - 1].addF(1, 0));
            sourceRouter = network.Last();

            realRoutePoints = new List<int>();
            realRoutePoints.Add(1);
            realRoutePoints.Add(2);
            realRoutePoints.Add(3);
            
            // first half of fake route:
            fakeFirstHalf = new List<int>();
            for (int i = 0; i < param.A_E.Count/2; ++i)
            {
                network.Add(network[network.Count - 1].addF(1, 0));
                fakeFirstHalf.Add(network.Count-1);
            }

            // first half of fake route:
            fakeSecondHalf = new List<int>();
            while(network.Count < param.A_E.Count)
            {
                network.Add(network[network.Count - 1].addF(1, 0));
                fakeSecondHalf.Add(network.Count - 1);
            }

            allFake = new List<int>();
            all = new List<int>(realRoutePoints);
            foreach (var v in fakeFirstHalf.Union(fakeSecondHalf))
            {
                all.Add(v);
                allFake.Add(v);
            }

            return true;
        }

        int timeStep = 0;
        int falseTransmissionFirstHalf, falseTransmissionSecondHalf;
        List<int> realRoutePoints, fakeFirstHalf, fakeSecondHalf, allFake, all;
    }

    /// <summary>
    /// this router algorithm allows minimizing the semi-continuous transmission pursuers algorithm.
    /// pursuers have a counter limit, and are optimized for a specific (randomly chosen) rate.
    /// - parameters: maximal time steps to transfer, randomize/get parameter for real data transmission rate 
    /// (avg. distance between following real transmissions)
    /// TODO: separate Segmented policy from it's transmission policy, and make this a derived specific segmented network
    /// </summary>
    class AdvRoutingAssumedRateInterKillerRouterPolicy : AdvRoutingRouterPolicyBase
    {
        public override List<ArgEntry> policyInputKeys
        {
            get
            {
                return ReflectionUtils.getStaticInstancesInClass<ArgEntry>(typeof(AppConstants.Policies.AdvRoutingAssumedRateInterKillerRouterPolicy));
            }
        }
        protected override bool initEx(AGameGraph G, APursuersPolicy initializedPursuers, Dictionary<string, string> policyParams)
        {
            timeStep = 0;
            
            int arbitraryAlgRuntime = (int)(param.A_E.Count * MathEx.PowInt(Math.Log(param.A_E.Count, 2), 2));
            // after ~'arbitraryAlgRuntime' data units are transferred, the game ends anyway

            float timeSpanExp = float.Parse(
                AppConstants.Policies.AdvRoutingAssumedRateInterKillerRouterPolicy.TOTAL_TIME_SPAN.tryRead(policyParams));

            float realRate = float.Parse(
                AppConstants.Policies.AdvRoutingAssumedRateInterKillerRouterPolicy.REAL_TRANSMISSION_RATE.tryRead(policyParams));

            totalTimeSpan = (int)Math.Pow(param.A_E.Count, timeSpanExp);
            
            float DesiredAvgTransmissionDiff = (int) Math.Max(1,(realRate * (totalTimeSpan / arbitraryAlgRuntime)));

            // start at a point that allows transmitting all data with the desired rate before game ends:
            int initialTimeStep = rand.Next(0, (int)(totalTimeSpan - DesiredAvgTransmissionDiff * arbitraryAlgRuntime));
            transmit = new List<bool>(Enumerable.Repeat(false, totalTimeSpan));
            transmit[initialTimeStep] = true;

            // select additional 'arbitraryAlgRuntime-1' data units to transfer, in the next 'arbitraryAlgRuntime * DesiredAvgTransmissionDiff'
            // time steps. any pattern will have an average rate of 'DesiredAvgTransmissionDiff'
            PatternRandomizer ptrn = new PatternRandomizer((int)(arbitraryAlgRuntime * DesiredAvgTransmissionDiff), arbitraryAlgRuntime-1);
            ptrn.Randomize(rand, true, -1, true);

            foreach (var p in ptrn.CurrentlyUsedPoints)
                transmit[p] = true;

            restartTimeStep = Math.Max(0, (int)(initialTimeStep - DesiredAvgTransmissionDiff));
            return true;
        }
        public override List<int> nextTransmittingPoints(List<PointF> capturedPoints, out int reward)
        {
            List<int> transmitting = new List<int>();

            if (timeStep >= transmit.Count)
                timeStep = restartTimeStep;

            if (transmit[timeStep++])
            {
                reward = param.A_E.Count - 1;
                return allNodes;
            }

            reward = 0;
            return new List<int>();
        }

        protected override bool generateNetwork(out List<PointF> network, out PointF sourceRouter, out PointF sinkRouter)
        {
            network = new List<PointF>(); //AlgorithmUtils.getRepeatingValueList(new PointF(0, 0), param.A_E.Count);

            // add sink:
            network.Add(new PointF(0, 0));
            sinkRouter = network.First();

            // real route is composed of only 3 nodes
            for (int i = 0; i < param.A_E.Count-1; ++i)
                network.Add(network[network.Count - 1].addF(1, 0));
            sourceRouter = network.Last();
            
            allNodes = new List<int>(Enumerable.Range(0,network.Count));
            return true;
        }

        int restartTimeStep; // if the alg. restarts the pattern, this tells from where
        int totalTimeSpan;
        int timeStep = 0;
        List<bool> transmit;
        List<int> allNodes;
    }

    [Serializable()]
    class AdvRoutingSegmentedStrategyCodeChromosome : ACompositeChromosome, ISerializable
    {
        // For disconnected routers, Average distance from any non-random router 0 to 1,
        // ***for each segment:
        // # of total parallel routes in segment, 
        // Ratio of routers participating in segment  0 to 1,
        // Ratio of how many of the segment routes them are fake,
        // Ratio of how many of the segment routers are allocated to fakse 0 to 1 )
        // # for how many fake routes per each real route (spread one after the other). If 0, fakes are shuffled. if this val is x ~=0, and fake routes = -1, this sets fake routes to x*realroutes, and each route is of length 1!
        //
        // *** ratios are multiplied by router#. All ratios of router# must sum to <1. 

        // we always use spread routes (fakeroutes = -1)
        public const int SegCount = 4;
        public const float MinimalRoutersRatioPerSeg = 0.1f;

        // doubles indices:
        public const int IAvgDisconnectedRouterDistance = 0;
        public const int IRoutersFactor = 1;
        // shorts indices:
        public const int IRouteCount = 0;
        public const int IDistanceBetweenRealRoutes = SegCount;
        
        public const int shortsCount = SegCount * 2;
        public const int doublesCount = 1 + (SegCount);

        const double MutationProb = 0.2;
        static public ushort[] MaxShorts()
        {
            return Enumerable.Repeat((ushort)100, SegCount). // max routes: 99 (min 1)
                Concat(Enumerable.Repeat((ushort)4, SegCount)). // max distance: 5 (min 1)
                ToArray();
        }
        static public double[] MaxDoubles()
        {
            return Enumerable.Repeat(1.0, doublesCount).ToArray();
        }

        //Deserialization constructor.
        public AdvRoutingSegmentedStrategyCodeChromosome(SerializationInfo info, StreamingContext ctxt)
            : base(shortsCount, MaxShorts(), doublesCount, MaxDoubles(), (int)Math.Ceiling(1.0/MinimalRoutersRatioPerSeg), MutationProb, null)
        {
            doubles[IAvgDisconnectedRouterDistance] = (double)info.GetValue("AvgDisconnectedRouterDistance", typeof(double));

            for (int segI = 0; segI < SegCount; ++segI)
            {
                shorts[IRouteCount + segI] = (ushort)info.GetValue("RouteCount" + segI.ToString(), typeof(ushort));
                doubles[IRoutersFactor + segI] = (double)info.GetValue("RoutersFactor" + segI.ToString(), typeof(double));
                shorts[IDistanceBetweenRealRoutes + segI] = (ushort)info.GetValue("DistanceBetweenRealRoutes" + segI.ToString(), typeof(ushort));
                
            }
        }

        //Serialization function.
        public void GetObjectData(SerializationInfo info, StreamingContext ctxt)
        {
            info.AddValue("AvgDisconnectedRouterDistance", doubles[IAvgDisconnectedRouterDistance]);
            for (int segI = 0; segI < SegCount; ++segI)
            {
                info.AddValue("RouteCount" + segI.ToString(), shorts[IRouteCount + segI]);
                info.AddValue("RoutersFactor" + segI.ToString(), doubles[IRoutersFactor + segI]);
                info.AddValue("DistanceBetweenRealRoutes" + segI.ToString(), shorts[IDistanceBetweenRealRoutes + segI]);
            }
        }
        public override string ToString()
        {
            string res = doubles[IAvgDisconnectedRouterDistance].ToString() + ",";

            //List<double> routersRatios = doubles.getNormalizedValues(IRoutersFactor);

            //double addedRatio = 0;
            for (int segI = 0; segI < SegCount; segI++)
            {
                // ignore segments with very few routers
                if (doubles[IRoutersFactor+segI] < MinimalRoutersRatioPerSeg)
                {
                    //addedRatio += routersRatios[segI]; // don't waste routers away
                    res += "0,0,0,0,0,";
                    continue;
                }

                res += (shorts[IRouteCount + segI] + 1).ToString() + ","; // +1 since min. value is 1, and not 0
                res += (doubles[IRoutersFactor+segI]).ToString() + ",";

                res += "-1,"; // fake routes
                res += "0,"; // fake routers
                res += (shorts[IDistanceBetweenRealRoutes + segI] + 1).ToString() + ","; // +1 since min. value is 1, and not 0

                //addedRatio = 0;
            }
            return res;
        }
        
        public AdvRoutingSegmentedStrategyCodeChromosome(string chromosomeString = "") :
            base(shortsCount, MaxShorts(), doublesCount, MaxDoubles(), (int)Math.Ceiling(1.0/MinimalRoutersRatioPerSeg), MutationProb, null)
        {
            if (chromosomeString == "")
                return;
            
            var vals = ParsingUtils.extractValues(chromosomeString);
            int valIdx = 0;
            doubles[IAvgDisconnectedRouterDistance] = vals[valIdx++];
            List<double> normalizedRouterRatios = new List<double>();
            for (int segI = 0; segI < SegCount; segI++)
            {
                if (vals.Count <= valIdx)
                    break; // when serializing a chromosome, it doesn't necessarily have all SegCount segments
                shorts[IRouteCount + segI] = (ushort)(vals[valIdx++] - 1);
                //normalizedRouterRatios.Add(vals[valIdx++]); // we can't directly write into doubles, since chromosome data is denormalized
                doubles[IRoutersFactor + segI] = vals[valIdx++];
                valIdx++; // skip fake routes
                valIdx++; // skip fake routers
                shorts[IDistanceBetweenRealRoutes + segI] = (ushort)(vals[valIdx++] - 1);
            }

            
            //// denormalize values, to represent as a chromosome
            //var usableRatios = AlgorithmUtils.denormalizeValues(normalizedRouterRatios);
            //for (int i = 0; i < usableRatios.Count; ++i)
            //    doubles[IRoutersFactor + i] = usableRatios[i];

        }
        protected override void ensureLegalVals()
        {
            base.ensureLegalVals();
            for(int i =0; i < SegCount; ++i)
                if(doubles[IRoutersFactor+i]>= MinimalRoutersRatioPerSeg)
                    return;
            doubles[IRoutersFactor] = MinimalRoutersRatioPerSeg;
        }
        public AdvRoutingSegmentedStrategyCodeChromosome(AdvRoutingSegmentedStrategyCodeChromosome src) : base(src)
        {
            
        }

        public override IChromosome Clone()
        {
            return new AdvRoutingSegmentedStrategyCodeChromosome(this);
        }

        public override IChromosome CreateNew()
        {
            var res = new AdvRoutingSegmentedStrategyCodeChromosome();
            res.RandomizeCompositeVals();
            return res;
        }
    }

    class AdvRoutingSegmentedRouteRouterPolicyOptimizer : APolicyOptimizer, IFitnessFunction
    {
        protected class OptimizationResult : GameResult
        {
            // we need explicitly parameterless ctor so it can be used as generic type
            public OptimizationResult()
            : this(float.PositiveInfinity, 0)
            { }

            public OptimizationResult(float totalReward,
                                     int capturedEvaders,
                                     int generationsCount = -1)
            : base(totalReward, capturedEvaders)
            {
                GenerationsCount = generationsCount;
            }

            public int GenerationsCount
            {
                get
                {
                    return Int32.Parse(this[OutputFields.GENERATION_COUNT]);
                }
                set
                {
                    this[OutputFields.GENERATION_COUNT] = value.ToString();
                }
            }
        }

        public override List<ArgEntry> optimizationInputKeys
        {
            get
            {
                var keys = ReflectionUtils.getStaticInstancesInClass<ArgEntry>(typeof(AppConstants.Algorithms.AdvRoutingSegmentedRouteRouterPolicyOptimizer));
                keys.Add(AppConstants.Algorithms.Optimizers.CHROMOSOME_COUNT);
                keys.Add(AppConstants.Algorithms.Optimizers.GENERATIONS_COUNT);
                keys.Add(AppConstants.Algorithms.Optimizers.TIME_LIMIT_SEC);
                keys.Add(AppConstants.Algorithms.Optimizers.INJECTED_INITIAL_CHROMOSOME);

                return keys;
            }
        }

        public override GameResult optimizationOutput
        {
            get;
            protected set;
        }

        public override List<string> optimizationOutputKeys
        {
            get
            {
                var keys = new List<string>();
                keys.Add(AppConstants.Policies.AdvRoutingSegmentedRouteRouterPolicy.STRATEGY_CODE.key);
                return keys;
            }
        }

        Dictionary<string, double> rewardPerChromosome = new Dictionary<string, double>();


        private void processInnter(ParallelOptions opt)
        {
            AdvRoutingSegmentedStrategyCodeChromosome initialChromosome = new AdvRoutingSegmentedStrategyCodeChromosome();

            try
            {
                // if no chromosome to inject was specific, an exception will be thrown
                initialChromosome = new AdvRoutingSegmentedStrategyCodeChromosome(
                        Optimizers.INJECTED_INITIAL_CHROMOSOME.tryRead(policyInput));
            }
            catch (Exception) { }

            MultiThreadEvaluationPopulation<AdvRoutingSegmentedStrategyCodeChromosome> pop =
                new MultiThreadEvaluationPopulation<AdvRoutingSegmentedStrategyCodeChromosome>(
                    int.Parse(Optimizers.CHROMOSOME_COUNT.tryRead(policyInput)),
                    initialChromosome,
                    this,
                    new RankSelection(),
                    new CustomThreadPool(opt.MaxDegreeOfParallelism),
                    false);

            pop.RandomSelectionPortion = 0.1;

            int genCount = int.Parse(Optimizers.GENERATIONS_COUNT.tryRead(policyInput));
            int logGenCount = Math.Max(1, Math.Min(30, genCount / 10));
            int gen;
            DateTime startTime = DateTime.Now;


            //pop = MultiThreadEvaluationPopulation<AdvRoutingSegmentedStrategyCodeChromosome>.desrializePopulation(
            //    new List<string>(File.ReadAllLines("allc.txt")),
            //    new MultiThreadEvaluationPopulation<AdvRoutingSegmentedStrategyCodeChromosome>.ChromosomeGenerator((string d) => new AdvRoutingSegmentedStrategyCodeChromosome(d)),
            //    initialChromosome,
            //        this,
            //        new RankSelection(),
            //        new CustomThreadPool(opt.MaxDegreeOfParallelism));
            for (gen = 0; gen < genCount; ++gen)
            {
                //fixme remove
                //List<string> allclines = null; ;
                //while (allclines == null)
                //{
                //    try
                //    {
                //        allclines = new List<string>(File.ReadAllLines("allc.txt"));
                //    }
                //    catch (Exception) { }
                //}

                //pop = MultiThreadEvaluationPopulation<AdvRoutingSegmentedStrategyCodeChromosome>.desrializePopulation(
                //allclines,
                //new MultiThreadEvaluationPopulation<AdvRoutingSegmentedStrategyCodeChromosome>.ChromosomeGenerator((string d) => new AdvRoutingSegmentedStrategyCodeChromosome(d)),
                //    this,
                //    new RankSelection(),
                //    new CustomThreadPool(opt.MaxDegreeOfParallelism));


                if ((DateTime.Now - startTime).TotalSeconds >= timeLimitSec)
                {
                    this.writeLog("time out. exact run time:" + (DateTime.Now - startTime).TotalSeconds.ToString());
                    this.writeLog("best reward:" + pop.BestChromosome.Fitness.ToString() + " || " +
                       pop.BestChromosome.ToString());
                    break;
                }

                //seralize pop:
                //var popLines = pop.serializePopulation();
                //Task asyncWrite = new Task(() =>
                //{
                //    try
                //    {
                //        File.WriteAllLines("allc.txt", popLines);
                //    }
                //    catch (Exception) { }
                //});
                //asyncWrite.Start();


                pop.runEpochParallel();
                //if (gen % logGenCount == 0)
                //{

                //    this.writeLog("best reward:" + pop.BestChromosome.Fitness.ToString() + " || " +
                //        pop.BestChromosome.ToString());
                //}

                for(int ci = 0; ci< pop.Size; ++ci)
                {
                    rewardPerChromosome[pop[ci].ToString()] = pop[ci].Fitness;
                }
            }

            optimizationOutput = new OptimizationResult((float)pop.BestChromosome.Fitness, 0, gen);
            optimizationOutput[AppConstants.Policies.AdvRoutingSegmentedRouteRouterPolicy.STRATEGY_CODE.key] =
                pop.BestChromosome.ToString();
            //optimizationOutput[AppConstants.Policies.Routing.GeneticWindowFunctionEvadersPolicy.WINDOW_CHROMOSOME.key] =
            //    ((GeneticWindowFunctionEvadersPolicy.WindowFunctionChromosome)pop.BestChromosome).ToWindowFunction().ToString();

            List<Dictionary<string, string>> allEntries = new List<Dictionary<string, string>>();
            foreach (var entry in rewardPerChromosome)
            {
                allEntries.Add(new Dictionary<string, string>());
                allEntries.Last()[AppConstants.Policies.AdvRoutingSegmentedRouteRouterPolicy.STRATEGY_CODE.key] = entry.Key;
                allEntries.Last()[AppConstants.GameProcess.OutputFields.PRESENTABLE_UTILITY] = entry.Value.ToString();
            }

            
            AppSettings.SaveToDB(policyInput, allEntries);
        }
        public override void process(ParallelOptions opt = null)
        {
            Exceptions.ConditionalTryCatch(() => { processInnter(opt); },
                (Exception ex) => { AppSettings.handleGameException(ex); });
        }

        protected override void initEx()
        {
            evalMethod = AppConstants.Algorithms.AdvRoutingSegmentedRouteRouterPolicyOptimizer.HEURISTIC.tryRead(policyInput);
            expectedTimeRepetitions = int.Parse(AppConstants.Algorithms.AdvRoutingSegmentedRouteRouterPolicyOptimizer.EXPECTED_RUNTIME_REPETETION_COUNT.tryRead(policyInput));
            repetitions = int.Parse(AppConstants.Algorithms.AdvRoutingSegmentedRouteRouterPolicyOptimizer.REPETETION_COUNT.tryRead(policyInput));
            heuristicSteps = float.Parse(AppConstants.Algorithms.AdvRoutingSegmentedRouteRouterPolicyOptimizer.HEURISTIC_TIME_STEPS_FACTOR.tryRead(policyInput));
            rewardPenalty = float.Parse(AdvRoutingGameParamsValueNames.SINK_FOUND_REWARD_PENALTY.tryRead(policyInput));
            timeLimitSec = int.Parse(AppConstants.Algorithms.Optimizers.TIME_LIMIT_SEC.tryRead(policyInput));
        }

        int timeLimitSec;
        float rewardPenalty;
        int repetitions, expectedTimeRepetitions;
        string evalMethod;
        float heuristicSteps;

        /// <summary>
        /// calculates when routers should have stopped, and how much reward they would have got if they did so
        /// </summary>
        /// <param name=""></param>
        /// <returns></returns>
        private double calculateBestHaltingPointResult(List<ProcessGameResult> results)
        {
            List<int> captureTimes = new List<int>();
            foreach (var r in results)
                captureTimes.Add((int)r.utilityPerEvader);
            captureTimes.Sort();
            float expectedReward = 0;
            float bestReward = -float.MaxValue;
            for (int spi = 0; spi < captureTimes.Count; ++spi)
            {
                float probToLose = ((float)spi) / captureTimes.Count;
                // if routers would have stopped after (captureTimes[spi] - 1) time steps, the would :
                // with probability probToLose, they would have failed like one of the previous sample, and would get on average expectedReward and suffer penalty
                // with prob. (1-probToLose), they would win captureTimes[spi] reward, since they stopped just before getting captured
                bestReward = Math.Max(bestReward, probToLose * (expectedReward - rewardPenalty) + (1 - probToLose) * (captureTimes[spi] - 1));

                expectedReward += captureTimes[spi] * (1.0f / captureTimes.Count);
            }
            return bestReward;
        }

        private double EvaluateNewStrategy(string strategyCode)
        {
            policyInput[AppConstants.Policies.AdvRoutingSegmentedRouteRouterPolicy.STRATEGY_CODE.key] =
                strategyCode;
            
            PerformanceEstimation est = new PerformanceEstimation(
                AppArgumentKeys.EVADER_POLICY.tryRead(policyInput),
                AppArgumentKeys.PURSUER_POLICY.tryRead(policyInput),
                igameParams,
                gameGraph);

            List<ProcessGameResult> results;

            if (rewardPenalty > 0 || evalMethod == "none")
            {
                results = est.estimatePerformance(new AdvRoutingGameProcess(), repetitions, policyInput);
                if (rewardPenalty == 0)
                    return double.Parse(AlgorithmUtils.Average(results)[OutputFields.PRESENTABLE_UTILITY]);
                // else:

                // given the penalty, search the best time for routers to stop, and estimate the reward they would have received
                return calculateBestHaltingPointResult(results);

            }

            // before invoking heuristic, estimate how many iterations are needed for the game to end normally
            int HeuristicRuntime;
            if (expectedTimeRepetitions > 0)
            {
                // use actual runs to estimate
                results = est.estimatePerformance(new AdvRoutingGameProcess(), expectedTimeRepetitions, policyInput);
                float expTime = float.Parse(AlgorithmUtils.Average(results)[OutputFields.ROUNDS_COUNT]);
                HeuristicRuntime = (int)(expTime * heuristicSteps);
            }
            else
            {
                // except for arbitrary run algorithm, this should be good enough for most algorithms
                HeuristicRuntime = int.Parse(policyInput[AppConstants.GameLogic.GameParamsValueNames.EVADERS_COUNT.key]);
                HeuristicRuntime = (int)((4 * HeuristicRuntime * Math.Log(HeuristicRuntime, 2)) * heuristicSteps);
            }

            results = est.estimatePerformance(new AdvRoutingGameProcess(), HeuristicRuntime, policyInput);

            if (evalMethod == "minSinkDist")
                return double.Parse(AlgorithmUtils.Average(results)[AdvRoutingGameProcess.MinDistanceToSinkHName]);

            //else if (evalMethod == "avgSinkDist")
            return double.Parse(AlgorithmUtils.Average(results)[AdvRoutingGameProcess.AvgDistancesToSinkHName]);
        }
        public double Evaluate(IChromosome chromosome)
        {
            AdvRoutingSegmentedStrategyCodeChromosome c =
               (AdvRoutingSegmentedStrategyCodeChromosome)chromosome;

            string strategyCode = c.ToString();

            if (rewardPerChromosome.ContainsKey(strategyCode))
                return rewardPerChromosome[strategyCode];

            double reward = EvaluateNewStrategy(strategyCode);
            
            return reward;
        }
    }

    class AdvRoutingEnsureLowerBoundPolicy : AdvRoutingRouterPolicyBase
    {
        public override List<ArgEntry> policyInputKeys
        {
            get
            {
                return new List<ArgEntry>(new ArgEntry[]{ AppConstants.Policies.AdvRoutingEnsureLowerBoundPolicy.FAKE_ROUTES });
            }
        }

        Dictionary<Point, int> toSink = null, toRandom = null, toFake = null;


        /// <summary>
        /// same as nextTransmittingPoints(), but also updates toSink,toRandom,toFake and adds gui markings
        /// </summary>
        /// <param name="capturedPoints"></param>
        /// <param name="reward"></param>
        /// <returns></returns>
        private List<int> nextTransmittingPointsWGUI(List<PointF> capturedPoints, out int reward)
        {
            if (toSink == null)
            {
                toSink = new Dictionary<Point, int>();
                toRandom = new Dictionary<Point, int>();
                toFake = new Dictionary<Point, int>();

                for (int x = 0; x < sqrtN; ++x)
                    for (int y = 0; y < sqrtN; ++y)
                    {
                        toSink[new Point(x, y)] = 0;
                        toRandom[new Point(x, y)] = 0;
                        toFake[new Point(x, y)] = 0;
                    }
            }

            List<int> res = new List<int>();

            PointF realRouteMidPoint = selectRandomPoint();
            List<int> tmp = new List<int>();

            ++t;
            addTransmittingRouterLocations(SourceRouter, realRouteMidPoint, res);
            foreach (var p in tmp)
            {
                Point pl = new Point(p / sqrtN, p % sqrtN);
                toRandom[pl] = toRandom[pl]+1;
            }

            
            addTransmittingRouterLocations(realRouteMidPoint, sink, tmp);
            foreach (var p in tmp)
            {
                Point pl = new Point(p / sqrtN, p % sqrtN);
                toSink[pl] = toSink[pl] + 1;
            }
            res.AddRange(tmp);

            foreach (var fakeSink in fakeSinks)
            {
                PointF fakeRouteMidPoint = selectRandomPoint();
                tmp = new List<int>();
                addTransmittingRouterLocations(SourceRouter, fakeRouteMidPoint, tmp);
                res.AddRange(tmp);
                foreach (var p in tmp)
                {
                    Point pl = new Point(p / sqrtN, p % sqrtN);
                    toRandom[pl] = toRandom[pl] + 1;
                }

                tmp = new List<int>();
                addTransmittingRouterLocations(fakeRouteMidPoint, fakeSink, tmp);
                foreach (var p in tmp)
                {
                    Point pl = new Point(p / sqrtN, p % sqrtN);
                    toFake[pl] = toFake[pl] + 1;
                }
                res.AddRange(tmp);
            }

            
            Dictionary<string, List<PointF>> marks = new Dictionary<string, List<PointF>>();
            const int DIVISION = 25;
            const double maxVal = 0.25;
            
            for (int i = 0; i < DIVISION; ++i)
            {
                marks["to sink >=" + (i * (maxVal / DIVISION)).ToString()] = new List<PointF>();
                marks["to fake >=" + (i * (maxVal / DIVISION)).ToString()] = new List<PointF>();
                marks["to random >=" + (i * (maxVal / DIVISION)).ToString()] = new List<PointF>();
                
            }
            foreach (var p in toSink)
            {
                int i = Math.Min(DIVISION - 1, (int)(((double)p.Value) / ((maxVal / DIVISION) * t)));
                marks["to sink >=" + (i * (maxVal / DIVISION)).ToString()].Add(p.Key);
            }
            foreach (var p in toFake)
            {
                int i = Math.Min(DIVISION - 1, (int)(((double)p.Value) / ((maxVal / DIVISION) * t)));
                marks["to fake >=" + (i * (maxVal / DIVISION)).ToString()].Add(p.Key);
            }
            foreach (var p in toRandom)
            {
                int i = Math.Min(DIVISION - 1, (int)(((double)p.Value) / ((maxVal / DIVISION) * t)));
                marks["to random >=" + (i * (maxVal / DIVISION)).ToString()].Add(p.Key);
            }
            List<string> emptyLists = new List<string>();
            foreach (var s in marks)
                if (s.Value.Count == 0)
                    emptyLists.Add(s.Key);
            foreach (var s in emptyLists)
                marks.Remove(s);
            gui.markLocations(marks);

            reward = 1;
            return res;
        }
        public override List<int> nextTransmittingPoints(List<PointF> capturedPoints, out int reward)
        {
//            #if DEBUG
            if (gui.hasBoardGUI())
                return nextTransmittingPointsWGUI(capturedPoints, out reward);
  //          #endif

            List<int> res = new List<int>();

            PointF realRouteMidPoint = selectRandomPoint();
            
            ++t;
            addTransmittingRouterLocations(SourceRouter, realRouteMidPoint, res);
            addTransmittingRouterLocations(realRouteMidPoint, sink, res);

            foreach(var fakeSink in fakeSinks)
            {
                PointF fakeRouteMidPoint = selectRandomPoint();
                addTransmittingRouterLocations(SourceRouter, fakeRouteMidPoint, res);
                addTransmittingRouterLocations(fakeRouteMidPoint, fakeSink, res);
            }
            
            reward = 1;
            return res;
        }

        protected override bool generateNetwork(out List<PointF> network, out PointF sourceRouter, out PointF sinkRouter)
        {
            network = new List<PointF>();
            fakeSinks = new HashSet<PointF>();
            sourceRouter = selectRandomPoint();
            do
            {
                sinkRouter = selectRandomPoint();
            } while (sinkRouter == sourceRouter);
            
            for (int x = 0; x < sqrtN; ++x)
                for (int y = 0; y < sqrtN; ++y)
                    network.Add(new PointF(x, y));

            for (int f = 0; f < fakeRouteCount;++f)
            {
                PointF newf;
                do
                {
                    newf = selectRandomPoint();
                }
                while (newf == sinkRouter || fakeSinks.Contains(newf));
                fakeSinks.Add(newf);
            }        


            return true;
        }

        int fakeRouteCount;
        protected override bool initEx(AGameGraph G, APursuersPolicy initializedPursuers, Dictionary<string, string> policyParams)
        {
            sqrtN = (int)Math.Sqrt(param.A_E.Count);
            latestSelectedPoints = new int[sqrtN,sqrtN];
            for (int x = 0; x < sqrtN; ++x)
                for (int y = 0; y < sqrtN; ++y)
                    latestSelectedPoints[x, y] = 0;

            
            fakeRouteCount = int.Parse(
                AppConstants.Policies.AdvRoutingEnsureLowerBoundPolicy.FAKE_ROUTES.tryRead(policyParams));


            return true;
        }

        private PointF selectRandomPoint()
        {
            return new PointF(rand.Next(sqrtN), rand.Next(sqrtN));
        }

        /// <summary>
        /// added points are marked with latestSelectedPoints[x,y]=t
        /// </summary>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <param name="transmittingRouters"></param>
        private void addTransmittingRouterLocations(PointF from, PointF to, List<int> transmittingRouters)
        {
            Point dest = new Point((int)to.X, (int)to.Y);
            Point current = new Point((int)from.X, (int)from.Y);
            int xDir = to.X > from.X ? 1 : -1;
            int yDir = to.Y > from.Y ? 1 : -1;

            while (current != dest)
            {
                if (latestSelectedPoints[current.X, current.Y] < t)
                    transmittingRouters.Add(current.X * sqrtN + current.Y);
                latestSelectedPoints[current.X, current.Y] = t;

                // advance either on x or on y
                if ( (dest.X - (current.X + xDir)) / xDir <
                     (dest.Y - (current.Y + yDir)) / yDir) // if xDist < yDist
                    current = current.add(0, yDir);
                else
                    current = current.add(xDir,0);
            }

            if (latestSelectedPoints[dest.X, dest.Y] < t)
                transmittingRouters.Add(dest.X * sqrtN + dest.Y);
            latestSelectedPoints[dest.X, dest.Y] = t;
        }

        int[,] latestSelectedPoints; // most udpated time (t) of when the point was selected
        int t = 1; // time step
        int sqrtN;
        HashSet<PointF> fakeSinks;
        
    }
}
